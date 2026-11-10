using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// Steps 3, 4 and 5 of the analysis pipeline, for one file: file-scoped symbol collection,
/// recognition with the anchor tested against the zone mask, and classification by minimum over
/// candidate levels.
///
/// It emits no finding. Findings acquire identity, a fingerprint and a global ordinal in a later
/// stage, over the full ordered set once every file has been recognized; what leaves here is a value,
/// returned to the caller. The one thing it does emit is a file the match bound stops, reported as a
/// skipped item through <see cref="EmissionPoint.Emit"/>.
///
/// Rules run in a fixed order, sorted by pattern class identifier, so the sequence of matches - and
/// therefore which rules have run when the bound trips - does not depend on the enumeration order of
/// the frozen rule set.
/// </summary>
public sealed class RecognitionEngine
{
	private readonly EmissionPoint _emissionPoint;
	private readonly MatchBoundWatch _matchBoundWatch;

	/// <summary>
	/// Reset once per file in <see cref="Recognize"/>, alongside <see cref="MatchBoundWatch.BeginFile"/>.
	/// A malformed match can recur across a rule's own matches and across rules, but the skipped item is
	/// counted once per file, like <see cref="SkippedItemCategory.MatchBoundExceeded"/>.
	/// </summary>
	private bool _malformedMatchAlreadyReportedForThisFile;

	/// <summary>
	/// One interpreted <see cref="Regex"/> per rule, built lazily on first need and kept for this engine
	/// instance's lifetime - one analysis run. Keyed by <see cref="DetectionRule"/> reference, which is
	/// stable for the run. <see cref="InterpretedExpressionFor"/> is this cache's only reader and writer.
	/// No concurrency primitive guards it, because the engine never runs two files, or two rules of the
	/// same file, concurrently.
	/// </summary>
	private readonly Dictionary<DetectionRule, Regex> _interpretedExpressionByRule = [];

	/// <summary>
	/// DIAGNOSTIC ONLY, READ BY NOTHING THIS ENGINE DOES. The
	/// calibration harness (<c>Tests.Calibration.MatchBoundCalibration</c>) needs the exact total
	/// this run charged against <see cref="MatchBoundWatch"/>'s budget - symbol table, demotion
	/// signals and every rule together - to calibrate the bound from the same quantity the bound
	/// itself enforces, rather than from an independent re-summing of a subset of it. Exposed as a
	/// plain <see cref="TimeSpan"/> so the calibration project needs no visibility into the internal
	/// <see cref="MatchBoundWatch"/> type. Overwritten on every call to <see cref="Recognize"/>,
	/// exactly like the watch's own accumulator it mirrors.
	/// </summary>
	public TimeSpan LastFileMatchingTimeForDiagnostics { get; private set; }

	/// <summary>
	/// How many files this engine has run flow-signal evaluation on - every file holding a
	/// surviving High or Medium construct, whether or not its findings are later admitted under the
	/// global findings cap, which is applied after recognition. Cumulative over this engine's own
	/// lifetime, which is one analysis run.
	/// </summary>
	public int FilesEvaluatedForFlowSignals { get; private set; }

	/// <summary>
	/// How many of those evaluations timed out. A trip keeps the file's findings, leaves its
	/// evidence <see cref="FlowEvidence.None"/> and does not skip the file.
	/// </summary>
	public int FlowSignalTripCount { get; private set; }

	/// <summary>The engine as the pipeline builds it, on the system clock.</summary>
	public RecognitionEngine(EmissionPoint emissionPoint)
		: this(emissionPoint, TimeProvider.System)
	{
	}

	/// <param name="emissionPoint">The one door, for the match bound's skipped item.</param>
	/// <param name="timeProvider">The clock the match bound is measured against.</param>
	public RecognitionEngine(EmissionPoint emissionPoint, TimeProvider timeProvider)
	{
		ArgumentNullException.ThrowIfNull(emissionPoint);
		ArgumentNullException.ThrowIfNull(timeProvider);

		_emissionPoint = emissionPoint;
		_matchBoundWatch = new MatchBoundWatch(timeProvider);
	}

	/// <summary>
	/// Every construct this file yields, classified, subsumption applied, in the total finding order.
	/// Empty where the file holds none, and equally empty where the match bound stopped it; that case is
	/// reported as a skipped item rather than signalled in the return value.
	/// </summary>
	public RecognitionResult Recognize(FileRecognitionContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		_matchBoundWatch.BeginFile();
		_malformedMatchAlreadyReportedForThisFile = false;

		List<ClassifiedMatch> classifiedMatches = [];
		RecognitionResult result;

		if (TryRecognizeEveryRule(context, classifiedMatches, out RecognitionResult recognized))
		{
			result = recognized;
		}
		else
		{
			// The partial set is discarded rather than kept: which rules had already run before the bound
			// tripped depends on machine speed, so a kept remainder would differ between two runs of the same
			// input on two machines while looking complete.
			result = new RecognitionResult { Constructs = [] };
			ReportTheFileAsStoppedByTheMatchBound(context.Path);
		}

		LastFileMatchingTimeForDiagnostics = _matchBoundWatch.AccumulatedMatchingTime;

		return result;
	}

	/// <summary>
	/// Runs the whole rule set, or stops the file. False means the match bound was reached - by
	/// this component's own cooperative timer or by a match timeout the analyzer's own expression
	/// carried - and in both cases nothing about that event travels any further than this method.
	/// </summary>
	private bool TryRecognizeEveryRule(
		FileRecognitionContext context,
		List<ClassifiedMatch> classifiedMatches,
		out RecognitionResult recognized)
	{
		bool completed = false;
		recognized = new RecognitionResult { Constructs = [] };

		try
		{
			completed = RecognizeEveryRule(context, classifiedMatches);

			if (completed)
			{
				recognized = ReduceAndEvaluateFlow(context, classifiedMatches);
			}
		}
		catch (RegexMatchTimeoutException)
		{
			// CAPTURED LOCALLY AND NEVER RE-THROWN, NEVER LOGGED, NEVER SERIALIZED. This platform's
			// timeout exception exposes the COMPLETE INPUT in one of its properties, so its message
			// and even its type carry the whole analyzed file towards whatever reads them - which
			// the data invariant forbids absolutely. The exception object is not bound to a
			// variable for that reason: there is nothing named here that a later edit could pass to
			// a logger. What the run learns is a count, through the skipped-item category.
			completed = false;
		}

		return completed;
	}

	/// <summary>
	/// The file's two pre-rule phases, each bounded, then the rule loop.
	/// <see cref="FileSymbolTable.TryCollect"/> and <see cref="FileDemotionSignals.TryEvaluate"/> report
	/// whether they completed within the file's budget, as <see cref="RecognizeOneRule"/> does for a rule,
	/// and a trip in either one skips the rule loop and discards the file exactly as a rule's own trip
	/// would - through the single <c>completed</c> value this method returns.
	/// </summary>
	private bool RecognizeEveryRule(FileRecognitionContext context, List<ClassifiedMatch> classifiedMatches)
	{
		bool completed = FileSymbolTable.TryCollect(
			context.Analyzer.LexicalProfile,
			context.NormalizedText,
			context.Mask,
			_matchBoundWatch,
			out FileSymbolTable symbols);

		FileDemotion demotion = FileDemotion.None;

		if (completed)
		{
			completed = FileDemotionSignals.TryEvaluate(context, _matchBoundWatch, out demotion);
		}

		if (completed)
		{
			completed = RecognizeEveryDetectionRule(context, symbols, demotion, classifiedMatches);
		}

		return completed;
	}

	/// <summary>
	/// Subsumption first, then the flow signals, on the same <see cref="MatchBoundWatch"/> and
	/// only when a surviving construct is High or Medium. The findings cap is applied later, in
	/// composition, so this runs whether or not the file's findings are admitted under it.
	///
	/// A flow-signal timeout - the watch's own bound or an expression's own match timeout - is caught
	/// here, inside the flow phase alone, so the constructs the file already produced are kept. Partial
	/// signal evidence is discarded, the file's evidence is <see cref="FlowEvidence.None"/> with a trip
	/// mark, the trip is counted, and the file is not skipped.
	/// </summary>
	private RecognitionResult ReduceAndEvaluateFlow(FileRecognitionContext context, List<ClassifiedMatch> classifiedMatches)
	{
		ImmutableArray<RecognizedConstruct> constructs =
			SubsumptionReduction.Reduce(context.Analyzer.SubsumptionRelation, classifiedMatches);

		FlowEvidence evidence = FlowEvidence.None;
		bool tripped = false;

		if (FlowSignalEvaluation.IsRequiredFor(constructs))
		{
			FilesEvaluatedForFlowSignals++;
			tripped = !TryEvaluateFlowSignals(context, out evidence);

			if (tripped)
			{
				FlowSignalTripCount++;
			}
		}

		return new RecognitionResult { Constructs = constructs, FlowEvidence = evidence, FlowEvaluationTripped = tripped };
	}

	private bool TryEvaluateFlowSignals(FileRecognitionContext context, out FlowEvidence evidence)
	{
		bool completed;

		try
		{
			completed = FlowSignalEvaluation.TryEvaluate(context, _matchBoundWatch, out evidence);
		}
		catch (RegexMatchTimeoutException)
		{
			completed = false;
			evidence = FlowEvidence.None;
		}

		return completed;
	}

	private bool RecognizeEveryDetectionRule(
		FileRecognitionContext context,
		FileSymbolTable symbols,
		FileDemotion demotion,
		List<ClassifiedMatch> classifiedMatches)
	{
		bool completed = true;

		foreach (string patternClassIdentifier in PatternClassesInEngineOrder(context.Analyzer))
		{
			DetectionRule rule = context.Analyzer.DetectionRulesByPatternClass[patternClassIdentifier];

			if (!RecognizeOneRule(
				new RuleRecognitionState
				{
					Context = context,
					PatternClassIdentifier = patternClassIdentifier,
					Rule = rule,
					Symbols = symbols,
					Demotion = demotion
				},
				classifiedMatches))
			{
				completed = false;
				break;
			}
		}

		return completed;
	}

	/// <summary>
	/// One rule over the whole text. The bound is polled before the rule starts and again between
	/// successive matches; only the <c>Match</c> and <c>NextMatch</c> calls themselves are timed, through
	/// <see cref="TimedRegexEvaluation.Match(MatchBoundWatch, Regex, string)"/> and
	/// <see cref="TimedRegexEvaluation.NextMatch"/>.
	///
	/// Every match is checked by the malformed-match guard before it is classified: the
	/// <c>[GeneratedRegex]</c> source generator can return a "successful" <see cref="Match"/> that does
	/// not advance past the one before it - see <see cref="AdvancesPastThePreviousMatch"/> and
	/// <see cref="MatchStartsAtOrAfterTheSearchOrigin"/>. A match that advances is handed to
	/// <see cref="TryClassifyOrResumePastCorruption"/>, which re-evaluates through the interpreter any
	/// match whose anchor group lies outside the match's own span (see
	/// <see cref="InterpretedCrossCheckConfirmsTheMatch"/>).
	///
	/// A match that fails to advance, or that the cross-check rejects, does not stop the rule for the rest
	/// of the file: <see cref="TryResumePastAMalformedMatch"/> resumes from the search origin - the
	/// previous well-formed match's end (<c>Index + Length</c>), or one past its start when that match was
	/// empty. The file is counted under <see cref="SkippedItemCategory.MalformedMatch"/> exactly once,
	/// through <see cref="ReportTheFileAsHoldingAMalformedMatch"/>, however many times this loop resumes.
	/// </summary>
	private bool RecognizeOneRule(RuleRecognitionState state, List<ClassifiedMatch> classifiedMatches)
	{
		Regex expression = state.Rule.Expression;
		string normalizedText = state.Context.NormalizedText;

		bool completed = !_matchBoundWatch.HasBeenExceeded();
		Match match = completed ? TimedRegexEvaluation.Match(_matchBoundWatch, expression, normalizedText) : Match.Empty;
		Match? previousMatch = null;
		int searchOrigin = 0;

		while (completed && match.Success)
		{
			bool advances = AdvancesPastThePreviousMatch(previousMatch, match)
				&& MatchStartsAtOrAfterTheSearchOrigin(searchOrigin, match);

			if (!advances)
			{
				completed = TryResumePastAMalformedMatch(
					state, expression, normalizedText, match, ref searchOrigin, out match);
				previousMatch = null;

				continue;
			}

			completed = TryClassifyOrResumePastCorruption(
				state, expression, normalizedText, match, ref searchOrigin, classifiedMatches, out bool matchWasResumed, out Match resumedMatch);

			if (matchWasResumed)
			{
				match = resumedMatch;
				previousMatch = null;

				continue;
			}

			previousMatch = match;
			searchOrigin = match.Index + Math.Max(match.Length, 1);
			completed = completed && !_matchBoundWatch.HasBeenExceeded();
			match = completed ? TimedRegexEvaluation.NextMatch(_matchBoundWatch, match) : Match.Empty;
		}

		return completed;
	}

	/// <summary>
	/// Resolves an advancing match. If its anchor group does not participate, or participates inside the
	/// match's own span, it is classified straight away. Otherwise <see
	/// cref="InterpretedCrossCheckConfirmsTheMatch"/> decides: confirmed, and it is classified like any
	/// other match; not confirmed, and <paramref name="match"/> is treated like a non-advancing one -
	/// resumed through <see cref="TryResumePastAMalformedMatch"/>, which records the malformed-match
	/// category. <paramref name="matchWasResumed"/> is <see langword="true"/> only in that second case,
	/// telling the caller that <paramref name="resumedMatch"/> - not <paramref name="match"/> - is the
	/// next value to examine. Returns <see langword="false"/> only when the match bound trips while this
	/// method is in control, which the caller reads like any other incomplete rule.
	/// </summary>
	private bool TryClassifyOrResumePastCorruption(
		RuleRecognitionState state,
		Regex expression,
		string normalizedText,
		Match match,
		ref int searchOrigin,
		List<ClassifiedMatch> classifiedMatches,
		out bool matchWasResumed,
		out Match resumedMatch)
	{
		matchWasResumed = false;
		resumedMatch = Match.Empty;

		bool completed = true;
		bool crossCheckRejectedTheMatch = false;

		if (AnchorGroupParticipatesButLiesOutsideTheMatchSpan(state.Rule, match, out string? anchorGroupName))
		{
			completed = !_matchBoundWatch.HasBeenExceeded();

			bool confirmedLegitimate = completed
				&& InterpretedCrossCheckConfirmsTheMatch(state.Rule, normalizedText, searchOrigin, match, anchorGroupName);

			crossCheckRejectedTheMatch = completed && !confirmedLegitimate;
		}

		if (crossCheckRejectedTheMatch)
		{
			completed = TryResumePastAMalformedMatch(
				state, expression, normalizedText, match, ref searchOrigin, out resumedMatch);
			matchWasResumed = true;
		}
		else if (completed && TryClassify(state, match, out ClassifiedMatch classifiedMatch))
		{
			classifiedMatches.Add(classifiedMatch);
		}

		return completed;
	}

	/// <summary>
	/// Resumes one stuck match. Records the malformed-match category once per file, then tries
	/// <c>Match(text, searchOrigin)</c> at the current search origin itself before moving on. Only a
	/// candidate that is itself untrustworthy (<see cref="IsAlsoStuck"/>) advances <paramref
	/// name="searchOrigin"/> by one and tries again; the method returns as soon as a trustworthy
	/// candidate is found, the file's match bound trips, or the text is exhausted. It is called for a
	/// non-advancing match and for a match that <see cref="TryClassifyOrResumePastCorruption"/>'s
	/// interpreted cross-check rejects.
	/// </summary>
	private bool TryResumePastAMalformedMatch(
		RuleRecognitionState state,
		Regex expression,
		string normalizedText,
		Match stuckMatch,
		ref int searchOrigin,
		out Match resumedMatch)
	{
		ReportTheFileAsHoldingAMalformedMatch(state.Context.Path);

		bool completed = !_matchBoundWatch.HasBeenExceeded();
		Match candidate = completed
			? TimedRegexEvaluation.Match(_matchBoundWatch, expression, normalizedText, searchOrigin)
			: Match.Empty;
		Match previousAttempt = stuckMatch;

		while (completed && IsAlsoStuck(searchOrigin, previousAttempt, candidate))
		{
			previousAttempt = candidate;
			searchOrigin++;

			completed = !_matchBoundWatch.HasBeenExceeded();
			bool searchSpaceExhausted = searchOrigin > normalizedText.Length;
			candidate = completed && !searchSpaceExhausted
				? TimedRegexEvaluation.Match(_matchBoundWatch, expression, normalizedText, searchOrigin)
				: Match.Empty;
		}

		resumedMatch = candidate;

		return completed;
	}

	/// <summary>
	/// WHETHER A RESUME CANDIDATE IS ITSELF UNTRUSTWORTHY, ON THE TWO SIGNATURES THIS METHOD CAN
	/// JUDGE WITHOUT CONTEXT. A candidate that failed to match at all is not "stuck" - it is the
	/// ordinary end of this rule's enumeration, and the caller's own loop ends on it. Otherwise it is
	/// untrustworthy if it starts before the position it was asked to start from (<see
	/// cref="MatchStartsAtOrAfterTheSearchOrigin"/>'s own question), or if it reproduces the
	/// immediately preceding stuck attempt's own span verbatim. A candidate accepted here is still
	/// re-examined by <see cref="TryClassifyOrResumePastCorruption"/>'s interpreted cross-check.
	/// </summary>
	private static bool IsAlsoStuck(int searchOrigin, Match previousAttempt, Match candidate)
	{
		bool repeatsThePreviousAttempt = previousAttempt.Success
			&& candidate.Index == previousAttempt.Index
			&& candidate.Length == previousAttempt.Length;

		return candidate.Success
			&& (!MatchStartsAtOrAfterTheSearchOrigin(searchOrigin, candidate) || repeatsThePreviousAttempt);
	}

	/// <summary>
	/// THE FIRST OF THE TWO THINGS "ADVANCES" MEANS. A well-formed enumeration always starts its next
	/// match at or after the previous one's end - and strictly after the previous one's own start when
	/// the previous match was empty, which is the platform's own rule for making an empty match advance
	/// rather than repeat forever. There is no previous match to compare the first one against, so the
	/// first match of a rule always passes this condition by definition - which is also true of the first
	/// match tried after a resume, since <see cref="RecognizeOneRule"/> resets the comparison there on
	/// purpose. See <see cref="MatchStartsAtOrAfterTheSearchOrigin"/> for whether a resumed match can be
	/// trusted at all. A match failing this condition is resumed past by <see cref="RecognizeOneRule"/>
	/// rather than enumerated further with <c>NextMatch</c>.
	/// </summary>
	private static bool AdvancesPastThePreviousMatch(Match? previousMatch, Match match)
	{
		bool advances = true;

		if (previousMatch is not null)
		{
			bool previousMatchWasEmpty = previousMatch.Length == 0;
			int previousMatchEnd = previousMatch.Index + previousMatch.Length;

			advances = previousMatchWasEmpty ? match.Index > previousMatch.Index : match.Index >= previousMatchEnd;
		}

		return advances;
	}

	/// <summary>
	/// THE SECOND OF THE TWO THINGS "ADVANCES" MEANS. <see cref="AdvancesPastThePreviousMatch"/> answers
	/// "did this match progress past the one immediately before it", and after a resume there is no real
	/// predecessor to ask that about - <see cref="RecognizeOneRule"/> resets it to null there, because a
	/// resumed match is not the continuation of a chain <c>NextMatch</c> built. Left unchecked, a null
	/// predecessor would let every resumed attempt pass as if it were a rule's first match.
	///
	/// The source-generated matcher, asked to search from a position <c>N</c> past a stuck point, can
	/// return a match whose own <see cref="Match.Index"/> is BEFORE <c>N</c>. This condition catches that:
	/// the resumed match's own index must be at or after the position it was asked to start searching
	/// from, or it is treated exactly like a failure to advance and resumed again through
	/// <see cref="TryResumePastAMalformedMatch"/>, past a search origin that method tracks independently of
	/// anything the matcher reports, so the position strictly increases.
	/// </summary>
	private static bool MatchStartsAtOrAfterTheSearchOrigin(int searchOrigin, Match match)
	{
		return match.Index >= searchOrigin;
	}

	/// <summary>
	/// The trigger for the interpreted cross-check: a match whose participating anchor group sits outside
	/// the match's own span, at any length. A rule may name a capture group inside a lookaround:
	/// <c>x(?=(?&lt;construct&gt;yz))</c> and <c>(?&lt;=(?&lt;construct&gt;Foo)\s*)\(</c> are both
	/// correct matches whose anchor group is outside the match's own span. This method answers only
	/// whether that shape is present; <see cref="InterpretedCrossCheckConfirmsTheMatch"/> tells a
	/// legitimate lookaround apart from the source-generator defect. A group that did not participate
	/// (<see cref="Group.Success"/> false) is not this shape: <see cref="TryClassify"/> already treats a
	/// non-participating anchor as not recognized.
	/// </summary>
	private static bool AnchorGroupParticipatesButLiesOutsideTheMatchSpan(
		DetectionRule rule, Match match, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? anchorGroupName)
	{
		anchorGroupName = rule.AnchorPosition.CaptureGroupName;

		bool outsideSpan = false;

		if (anchorGroupName is string name)
		{
			Group anchorGroup = match.Groups[name];

			outsideSpan = anchorGroup.Success && !AnchorGroupIsContainedInTheMatch(anchorGroup, match);
		}

		return outsideSpan;
	}

	/// <summary>Pure geometry: whether a participating anchor group's captured extent falls inside the match's own span.</summary>
	private static bool AnchorGroupIsContainedInTheMatch(Group anchorGroup, Match match)
	{
		return anchorGroup.Index >= match.Index && anchorGroup.Index + anchorGroup.Length <= match.Index + match.Length;
	}

	/// <summary>
	/// THE INTERPRETED CROSS-CHECK ITSELF. This method re-runs the SAME rule through the interpreter
	/// (<c>new Regex(pattern)</c>, built once per rule by <see cref="InterpretedExpressionFor"/>), FROM THE
	/// SAME ATTEMPT START POSITION the generator's own call used, and asks whether the two engines agree.
	///
	/// <paramref name="attemptStartPosition"/> IS THE CALLER'S OWN <c>searchOrigin</c>, NEVER <paramref
	/// name="generatedMatch"/>'S OWN <see cref="Match.Index"/>. <see cref="RecognizeOneRule"/>'s own local
	/// <c>searchOrigin</c> is, at the moment a match is handed to
	/// <see cref="TryClassifyOrResumePastCorruption"/>, EXACTLY the position the generator's own call
	/// attempted from: 0 for the rule's first <c>Match</c>; the previous well-formed match's own end (or
	/// one past its start when empty) for a match produced by <c>NextMatch</c>; and the exact
	/// <c>startAt</c> <see cref="TryResumePastAMalformedMatch"/> passed for a resumed candidate.
	/// <paramref name="generatedMatch"/>'s own <see cref="Match.Index"/> is exactly the value a corrupted
	/// match can get wrong, while <c>searchOrigin</c> is derived independently of anything the matcher
	/// reported.
	///
	/// CONFIRMED ONLY ON AN EXACT MATCH: the interpreted attempt must succeed, at the identical
	/// <see cref="Match.Index"/> and <see cref="Match.Length"/>, with its own <paramref
	/// name="anchorGroupName"/> group participating at the identical <see cref="Group.Index"/> and
	/// <see cref="Group.Length"/>. Anything else - including the interpreted call finding no match at
	/// all, or a match at a different position or extent - means the generated match is the corrupted
	/// one, not a legitimate lookaround. Timed through the same <see cref="TimedRegexEvaluation"/> door
	/// every other analyzer-declared expression evaluation uses, so this attempt spends the file's own
	/// match-bound budget exactly like a detection rule's own <c>Match</c> call.
	/// </summary>
	private bool InterpretedCrossCheckConfirmsTheMatch(
		DetectionRule rule, string normalizedText, int attemptStartPosition, Match generatedMatch, string anchorGroupName)
	{
		Regex interpretedExpression = InterpretedExpressionFor(rule);
		Match interpretedMatch = TimedRegexEvaluation.Match(_matchBoundWatch, interpretedExpression, normalizedText, attemptStartPosition);

		Group generatedAnchorGroup = generatedMatch.Groups[anchorGroupName];
		Group interpretedAnchorGroup = interpretedMatch.Groups[anchorGroupName];

		return interpretedMatch.Success
			&& interpretedMatch.Index == generatedMatch.Index
			&& interpretedMatch.Length == generatedMatch.Length
			&& interpretedAnchorGroup.Success
			&& interpretedAnchorGroup.Index == generatedAnchorGroup.Index
			&& interpretedAnchorGroup.Length == generatedAnchorGroup.Length;
	}

	/// <summary>
	/// One interpreted <see cref="Regex"/> per rule, built lazily and cached in
	/// <see cref="_interpretedExpressionByRule"/>. Built from the generated instance's own
	/// <see cref="Regex.ToString"/>, <see cref="Regex.Options"/> and <see cref="Regex.MatchTimeout"/>,
	/// never from a second copy of the pattern text. The options of every generated rule are
	/// <c>CultureInvariant | ExplicitCapture</c>; a rule carrying <see cref="RegexOptions.Compiled"/>
	/// would need that option stripped here, because a compiled interpreter uses <c>Reflection.Emit</c>,
	/// which Native AOT does not support.
	/// </summary>
	private Regex InterpretedExpressionFor(DetectionRule rule)
	{
		if (!_interpretedExpressionByRule.TryGetValue(rule, out Regex? interpretedExpression))
		{
			interpretedExpression = new Regex(rule.Expression.ToString(), rule.Expression.Options, rule.Expression.MatchTimeout);
			_interpretedExpressionByRule[rule] = interpretedExpression;
		}

		return interpretedExpression;
	}

	/// <summary>
	/// The level composition, and the ways a match yields no finding at all. Neither of the first two is
	/// a Low finding: an anchor that does not resolve was never recognized, and a demonstrated negative
	/// scheme verdict means the construct is not an instance of the class. A third, narrower way: after
	/// a lexical budget trip the match or its anchor lies where <see cref="ZoneMask.IsAnalyzableAt"/> is
	/// false, and no finding is emitted there.
	/// </summary>
	private static bool TryClassify(RuleRecognitionState state, Match match, out ClassifiedMatch classifiedMatch)
	{
		classifiedMatch = default;

		bool anchorResolved =
			state.Rule.AnchorPosition.TryResolveOffset(match, out int anchorOffset)
			&& anchorOffset >= 0
			&& anchorOffset < state.Context.Mask.Length
			&& state.Context.Mask.IsAnalyzableAt(match.Index)
			&& state.Context.Mask.IsAnalyzableAt(anchorOffset);

		SymbolicVerdictResponse response = anchorResolved
			? ReadTheSymbolicVerdict(state, match)
			: SymbolicVerdictResponse.NoEffect;

		bool recognized = anchorResolved && response is not SymbolicVerdictResponse.SuppressesFinding;

		if (recognized)
		{
			LexicalZone anchorZone = state.Context.Mask.ZoneAt(anchorOffset);
			ConfidenceLevel level = ComposeLevel(state, anchorZone, response);

			classifiedMatch = new ClassifiedMatch
			{
				PatternClassIdentifier = state.PatternClassIdentifier,
				MatchIndex = match.Index,
				MatchLength = match.Length,
				AnchorOffset = anchorOffset,
				Level = level,
				LiteralCredentialSignal = ReadTheLiteralCredentialSignal(state, match),
				LowReasons = ReadTheLowReasons(state, anchorZone, level)
			};
		}

		return recognized;
	}

	/// <summary>
	/// The literal-credential signal, computed from the zone and never from the value: the engine
	/// establishes that a credential is embedded in source without reading it. It is computed here
	/// because only here does the match still exist; the credential position is a named capture group.
	///
	/// The signal is true only where every position across the credential group's captured extent is
	/// trustworthy per <see cref="ZoneMask.IsTrustworthyAt"/> and is either
	/// <see cref="LexicalZone.StringBody"/> or <see cref="LexicalZone.Delimiter"/>, and at least one
	/// of those positions is <see cref="LexicalZone.StringBody"/>. A capture that opens on its own
	/// delimiter therefore signals true, while a capture spanning a concatenation
	/// (`user + ":" + password`) signals false, because the operator and the identifiers it joins are
	/// <see cref="LexicalZone.Code"/> positions inside the same extent.
	///
	/// Every way of not knowing is false rather than an error: the rule declares no credential position,
	/// the declared group did not participate in this match, the group's captured extent is empty, some
	/// position in that extent is one the scanner does not vouch for, some position is
	/// <see cref="LexicalZone.Code"/>, or every position is a delimiter with no
	/// <see cref="LexicalZone.StringBody"/> position at all.
	/// </summary>
	private static bool ReadTheLiteralCredentialSignal(RuleRecognitionState state, Match match)
	{
		bool signal = false;

		if (state.Rule.CredentialPosition.CaptureGroupName is string credentialGroupName)
		{
			Group credentialGroup = match.Groups[credentialGroupName];

			if (credentialGroup.Success && credentialGroup.Length > 0)
			{
				signal = IsWhollyLiteral(credentialGroup, state.Context.Mask);
			}
		}

		return signal;
	}

	/// <summary>
	/// The four-clause rule, over the group's whole captured extent rather than one offset.
	/// Every position must be trustworthy and either <see cref="LexicalZone.StringBody"/> or
	/// <see cref="LexicalZone.Delimiter"/>, and at least one position must be
	/// <see cref="LexicalZone.StringBody"/> - a capture holding only delimiters, such as an empty
	/// literal's quotes, carries no body to report on.
	/// </summary>
	private static bool IsWhollyLiteral(Group group, ZoneMask mask)
	{
		bool everyPositionQualifies = true;
		bool anyPositionIsStringBody = false;

		for (int position = group.Index; position < group.Index + group.Length; position++)
		{
			bool trustworthy = mask.IsTrustworthyAt(position);
			LexicalZone zone = mask.ZoneAt(position);
			bool qualifies = trustworthy && zone is LexicalZone.StringBody or LexicalZone.Delimiter;

			anyPositionIsStringBody = anyPositionIsStringBody || (trustworthy && zone is LexicalZone.StringBody);
			everyPositionQualifies = everyPositionQualifies && qualifies;
		}

		return everyPositionQualifies && anyPositionIsStringBody;
	}

	private static SymbolicVerdictResponse ReadTheSymbolicVerdict(RuleRecognitionState state, Match match)
	{
		SymbolicVerdictInterpretation interpretation = state.Rule.SymbolicVerdictInterpretation;
		SymbolicVerdictResponse response = SymbolicVerdictResponse.NoEffect;

		if (interpretation.IsSubjectToSymbolicResolution)
		{
			SymbolicVerdict verdict = SchemeArgumentResolution.Resolve(
				interpretation,
				match,
				state.Context.NormalizedText,
				state.Context.Mask,
				state.Symbols);

			response = SchemeArgumentResolution.ResponseTo(interpretation, verdict);
		}

		return response;
	}

	/// <summary>
	/// The level table, and nothing else. Every signal proposes; the set is complete before anything
	/// is computed from it; the minimum is taken once. There is no level variable here for a later
	/// edit to lower a second time.
	/// </summary>
	private static ConfidenceLevel ComposeLevel(
		RuleRecognitionState state,
		LexicalZone anchorZone,
		SymbolicVerdictResponse response)
	{
		CandidateLevelSet candidates = new();

		candidates.Propose(state.Rule.BaseLevel);
		candidates.ProposeWhen(
			state.Rule.SelfSufficiency is SelfSufficiency.RequiresCallPathToBeEstablished,
			ConfidenceLevel.Medium);
		candidates.ProposeWhen(
			anchorZone is LexicalZone.Comment or LexicalZone.StringBody,
			ConfidenceLevel.Low);
		candidates.ProposeWhen(
			response is SymbolicVerdictResponse.ProposesMediumLevel,
			ConfidenceLevel.Medium);
		candidates.ProposeWhen(state.Demotion.IsDemoted, ConfidenceLevel.Low);

		return candidates.Minimum();
	}

	/// <summary>
	/// WHICH OF THE SIGNALS <see cref="ComposeLevel"/> READS PROPOSED LOW, RECORDED SO THE REPORT CAN SAY
	/// WHY. It restates no rule: each condition below is the one <see cref="ComposeLevel"/> proposes Low
	/// on, and nothing here changes the level. The set is empty when the assigned level is above Low,
	/// because then no signal proposed Low.
	/// </summary>
	private static LowConfidenceReasons ReadTheLowReasons(
		RuleRecognitionState state,
		LexicalZone anchorZone,
		ConfidenceLevel assignedLevel)
	{
		LowConfidenceReasons reasons = LowConfidenceReasons.None;

		if (assignedLevel == ConfidenceLevel.Low)
		{
			reasons |= state.Rule.BaseLevel == ConfidenceLevel.Low ? LowConfidenceReasons.PatternClass : LowConfidenceReasons.None;
			reasons |= anchorZone is LexicalZone.Comment or LexicalZone.StringBody
				? LowConfidenceReasons.CommentOrStringLiteral
				: LowConfidenceReasons.None;
			reasons |= state.Demotion.FiresOtherwise ? LowConfidenceReasons.DemotedFile : LowConfidenceReasons.None;
			reasons |= state.Demotion.FiresOnVendoredFolder ? LowConfidenceReasons.DemotedVendoredFile : LowConfidenceReasons.None;
		}

		return reasons;
	}

	private static ImmutableArray<string> PatternClassesInEngineOrder(ILanguageAnalyzer analyzer)
	{
		return
		[
			.. analyzer.DetectionRulesByPatternClass.Keys.OrderBy(identifier => identifier, StringComparer.Ordinal)
		];
	}

	private void ReportTheFileAsStoppedByTheMatchBound(RepositoryRelativePath path)
	{
		_emissionPoint.Emit(new EmittedFact
		{
			Category = EmissionCategory.SkippedItem,
			SkippedItem = SkippedItemCategory.MatchBoundExceeded,
			Path = path
		});
	}

	/// <summary>
	/// The malformed-match skipped-item category, emitted at most once per file. The event is
	/// deterministic - the same expression against the same text on the same runtime produces the same
	/// corrupted match every time, unlike the match bound's own wall-clock exception - so nothing here
	/// discards the rest of the file's findings the way <see cref="ReportTheFileAsStoppedByTheMatchBound"/>
	/// does. It only records that the hole exists, once, no matter how many matches or how many
	/// rules in this file hit it.
	/// </summary>
	private void ReportTheFileAsHoldingAMalformedMatch(RepositoryRelativePath path)
	{
		if (!_malformedMatchAlreadyReportedForThisFile)
		{
			_malformedMatchAlreadyReportedForThisFile = true;

			_emissionPoint.Emit(new EmittedFact
			{
				Category = EmissionCategory.SkippedItem,
				SkippedItem = SkippedItemCategory.MalformedMatch,
				Path = path
			});
		}
	}

	/// <summary>
	/// Everything one rule's recognition needs, in one value. It exists so that the classification
	/// methods take a state rather than six parameters each, and it is a private nested type
	/// because nothing outside this engine has any business holding one.
	/// </summary>
	private sealed record RuleRecognitionState
	{
		internal required FileRecognitionContext Context { get; init; }

		internal required string PatternClassIdentifier { get; init; }

		internal required DetectionRule Rule { get; init; }

		internal required FileSymbolTable Symbols { get; init; }

		internal required FileDemotion Demotion { get; init; }
	}
}
