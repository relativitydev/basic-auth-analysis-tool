using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// STEPS 3, 4 AND 5 OF THE ANALYSIS PIPELINE, FOR ONE FILE: file-scoped symbol collection,
/// recognition with the anchor tested against the mask, and classification by minimum over
/// candidate levels.
///
/// IT EMITS NO FINDING, AND THAT IS THE BOUNDARY OF THIS STAGE. Findings acquire identity, a
/// fingerprint and a global ordinal in the stage after this one, and those are assigned over the
/// FULL ordered set once every file has been recognized - so a per-file component that emitted
/// findings would be emitting them before the only thing that can number them exists. What leaves
/// here is a value, returned to the caller.
///
/// THE ONE THING IT DOES EMIT IS A HOLE IN ITS OWN COVERAGE, which is the opposite case: the
/// match bound stops a file, every file it stops must be counted in its own skipped
/// category and reported like every other, and a count that waited for a later stage to notice would
/// be a count nobody produced. It goes through <see cref="EmissionPoint.Emit"/> for the same
/// reason the scanner's lexical anomaly does - there is no second reporting path for a hole.
///
/// RULES ARE RUN IN A FIXED ORDER, AND THAT IS DETERMINISM RATHER THAN TIDINESS. The contract hands the
/// rule set over as a <see cref="System.Collections.Frozen.FrozenDictionary{TKey, TValue}"/>
/// precisely so that an analyzer's ordering cannot survive into the engine, and the enumeration
/// order of one is not specified. Ordering by pattern class identifier before iterating is what
/// makes the sequence of matches - and therefore which rules have run when the bound trips - a
/// property of the catalog's names rather than of a hash seed.
/// </summary>
public sealed class RecognitionEngine
{
	private readonly EmissionPoint _emissionPoint;
	private readonly MatchBoundWatch _matchBoundWatch;

	/// <summary>
	/// Reset once per file in <see cref="Recognize"/>, alongside <see cref="MatchBoundWatch.BeginFile"/>.
	/// A malformed match is a per-RULE event that can recur - the same defect can repeat across a
	/// rule's own further matches, and a second rule in the same catalog can carry the identical
	/// shape - but the skipped item is a per-FILE count, exactly like
	/// <see cref="SkippedItemCategory.MatchBoundExceeded"/>'s own single emission for a file the
	/// bound stops regardless of how many rules never ran.
	/// </summary>
	private bool _malformedMatchAlreadyReportedForThisFile;

	/// <summary>
	/// ONE INTERPRETED <see cref="Regex"/> PER RULE, BUILT LAZILY ON FIRST NEED AND
	/// KEPT FOR THIS ENGINE INSTANCE'S OWN LIFETIME - ONE ANALYSIS RUN, PER this type's own
	/// construction contract; nothing recycles a <see cref="RecognitionEngine"/> across files or
	/// across runs. Keyed by <see cref="DetectionRule"/> REFERENCE rather than by pattern class
	/// identifier: the analyzer contract hands the catalog's rules over already constructed and the analyzer holds
	/// them for the run, so reference identity is stable for as long as this dictionary is, and a
	/// second key derived from the rule's own content would buy nothing a reference does not
	/// already give for free. <see cref="InterpretedExpressionFor"/> is this cache's only reader and
	/// writer. No concurrency primitive guards it, because nothing else in this engine runs two
	/// files - or two rules of the same file - concurrently (the product introduces no
	/// parallelism).
	/// </summary>
	private readonly Dictionary<DetectionRule, Regex> _interpretedExpressionByRule = [];

	/// <summary>
	/// DIAGNOSTIC ONLY, READ BY NOTHING THIS ENGINE DOES: the exact total this run charged against
	/// <see cref="MatchBoundWatch"/>'s budget - symbol table, demotion signals and every rule together -
	/// for a timing harness. Exposed as a plain <see cref="TimeSpan"/> so the harness needs no visibility
	/// into the internal <see cref="MatchBoundWatch"/> type. Overwritten on every call to
	/// <see cref="Recognize"/>, exactly like the watch's own accumulator it mirrors.
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
	/// Every construct this file yields, classified, subsumption applied, in the total ordering. Empty
	/// where the file holds none - and equally empty where the match bound stopped it, which is why
	/// the skipped item is emitted rather than signalled in the return value: a caller that had to
	/// tell the two apart from a result would be a caller that could forget to.
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
			// The match-bound exception is stated at the level of the FILE, and the partial set is discarded
			// rather than kept: WHICH rules had already run before the bound tripped is itself
			// machine-speed-dependent, so a kept remainder would be a finding set that differs
			// between two runs of the same input on two machines while looking complete.
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
	/// THE FILE'S TWO PRE-RULE PHASES, EACH BOUNDED, THEN THE RULE LOOP.
	///
	/// Symbol-table collection and demotion-signal evaluation each run an analyzer-declared expression
	/// over the whole file, exactly like a detection rule, so both are charged to the match bound.
	/// <see cref="FileSymbolTable.TryCollect"/> and <see cref="FileDemotionSignals.TryEvaluate"/> report
	/// whether they completed within the file's budget on the same terms <see cref="RecognizeOneRule"/>
	/// reports for a rule, and a trip in either one skips the rule loop entirely and discards the file
	/// exactly as a rule's own trip would - through the single <c>completed</c> value this method returns.
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
	/// A flow-signal timeout - the watch's own bound or an expression's own match timeout - is
	/// caught HERE, inside the flow phase alone, so the constructs the file already produced are kept.
	/// The exception object is not bound to a variable for the reason <see cref="TryRecognizeEveryRule"/>
	/// states. Partial signal evidence is discarded, the file's evidence is
	/// <see cref="FlowEvidence.None"/> with a trip mark, the trip is counted, and the file is not
	/// skipped. This narrows the machine-speed exception from findings to flow decisions.
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
	/// successive matches, which is every point at which this component is in control - inside a
	/// single call to <c>Match</c> nothing outside the matching loop can look at a clock. Only
	/// <c>Match</c> and <c>NextMatch</c> themselves are timed, through <see cref="TimedRegexEvaluation"/>;
	/// the malformed-match guard and classification that follow a match run outside that bracket.
	///
	/// EVERY MATCH IS ALSO CHECKED AGAINST THE MALFORMED-MATCH GUARD BEFORE IT IS CLASSIFIED. The
	/// <c>[GeneratedRegex]</c> source generator can return a "successful" <see cref="Match"/> that does
	/// not advance past the one before it - see <see cref="AdvancesPastThePreviousMatch"/> and
	/// <see cref="MatchStartsAtOrAfterTheSearchOrigin"/> for the two things "advances" means.
	///
	/// AN ANCHOR GROUP OUTSIDE THE MATCH'S OWN SPAN IS RESOLVED BY AN INTERPRETED CROSS-CHECK.
	/// <see cref="TryClassifyOrResumePastCorruption"/> is what a match that DOES advance is handed to
	/// next. Whenever its anchor group participates but sits outside the match's own span, at any
	/// length, the SAME rule is re-evaluated through the interpreter (<c>new Regex(pattern)</c> rather
	/// than <c>[GeneratedRegex]</c>), from the exact position the generator's own attempt started from.
	/// A match the interpreter reproduces exactly (Index, Length, and the anchor group's own
	/// Index/Length) is legitimate and classified normally - this is what a lookaround anchor looks like.
	/// A match the interpreter does NOT reproduce is corrupted regardless of its length, and is resumed
	/// past exactly like a non-advancing match.
	///
	/// A MATCH THAT FAILS TO ADVANCE, OR THAT THE CROSS-CHECK REJECTS, DOES NOT STOP THE RULE FOR THE
	/// REST OF THE FILE - BOTH CONVERGE ON THE SAME RESUME. THE SEARCH ORIGIN IS DEFINED PRECISELY: the
	/// previous well-formed match's own end (<c>Index + Length</c>), or one past its own start when that
	/// match was empty - exactly the position <see cref="AdvancesPastThePreviousMatch"/> requires the
	/// NEXT ordinary match to reach, and exactly the position the cross-check re-evaluates from.
	/// <see cref="TryResumePastAMalformedMatch"/> first attempts
	/// <see cref="TimedRegexEvaluation.Match(MatchBoundWatch, Regex, string, int)"/> AT <c>searchOrigin</c>
	/// itself, a different call than the <c>NextMatch</c> that produced the stuck result. Only when that
	/// attempt is ITSELF untrustworthy - starting before <c>searchOrigin</c> or reproducing the
	/// immediately preceding stuck attempt's own span - does the search origin advance by one and try
	/// again, strictly increasing on every such step, which bounds this to at most the text's own length
	/// in resume attempts; a candidate that clears both is handed back to THIS method's own caller, which
	/// re-examines it through the same cross-check before trusting it. The file is counted under
	/// <see cref="SkippedItemCategory.MalformedMatch"/> exactly once, through
	/// <see cref="ReportTheFileAsHoldingAMalformedMatch"/>, no matter how many times this loop resumes or
	/// how many times the cross-check rejects a match.
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
	/// AN ADVANCING MATCH'S OWN RESOLUTION. If its anchor group does not participate, or participates
	/// INSIDE its own span, it is classified straight away. Otherwise <see
	/// cref="InterpretedCrossCheckConfirmsTheMatch"/> decides: confirmed, and it is classified exactly
	/// like any other match (a genuine lookaround, zero-length or not); not confirmed, and <paramref
	/// name="match"/> is treated exactly like a non-advancing one - resumed through <see
	/// cref="TryResumePastAMalformedMatch"/>, which itself records the malformed-match category. <paramref
	/// name="matchWasResumed"/> is <see langword="true"/> only in that second case, telling the caller
	/// that <paramref name="resumedMatch"/> - not <paramref name="match"/> - is the next value to
	/// examine, with the comparison to the previous match reset exactly as a non-advancing resume
	/// resets it. Returns <see langword="false"/> only when the match bound trips while this
	/// method is itself in control - before the cross-check's own attempt, or during the resume it may
	/// call into - which the caller reads exactly like any other incomplete rule.
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
	/// ONE STUCK MATCH, RESUMED. Records the malformed-match category once (idempotent per file, regardless of how
	/// many times this method itself is called), then tries <c>Match(text, searchOrigin)</c> AT the
	/// current search origin itself - a fresh attempt on a different call than the <c>NextMatch</c> that
	/// produced <paramref name="stuckMatch"/> - before conceding the position and moving on. Only a
	/// candidate that is ITSELF untrustworthy (<see cref="IsAlsoStuck"/>) advances <paramref
	/// name="searchOrigin"/> by one and tries again; <paramref name="searchOrigin"/> therefore strictly
	/// increases on every such step and this method returns as soon as a trustworthy candidate is found,
	/// the file's match bound trips, or the text is exhausted. It is reached from two places - a
	/// non-advancing match and a match <see cref="TryClassifyOrResumePastCorruption"/>'s own interpreted
	/// cross-check rejects - and both report the same category through the same idempotent call.
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
	/// THE TRIGGER FOR THE INTERPRETED CROSS-CHECK: a match's own participating anchor group sits outside
	/// its own span, AT ANY LENGTH. <see cref="DetectionRule.AnchorPosition"/> is the construct position,
	/// and a rule may name a capture group that sits inside a lookaround: <c>x(?=(?&lt;construct&gt;yz))</c>
	/// and <c>(?&lt;=(?&lt;construct&gt;Foo)\s*)\(</c> are both ordinary, correctly-functioning matches
	/// whose anchor group is legitimately outside the match's own span. This method answers only
	/// WHETHER that shape is present, never whether it is legitimate;
	/// <see cref="InterpretedCrossCheckConfirmsTheMatch"/> tells a legitimate lookaround apart from a
	/// corrupted generated match. A group that did not participate (<see cref="Group.Success"/> false) is
	/// not this shape: <see cref="TryClassify"/> already treats a non-participating anchor as "not
	/// recognized".
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
	/// ONE INTERPRETED <see cref="Regex"/> PER RULE, BUILT LAZILY AND CACHED IN
	/// <see cref="_interpretedExpressionByRule"/> - see that field's own doc comment for the cache's
	/// lifetime and why no concurrency primitive guards it. Built from the generated instance's own
	/// <see cref="Regex.ToString"/>, <see cref="Regex.Options"/> and <see cref="Regex.MatchTimeout"/>,
	/// never from a second copy of the pattern text: the catalog's contract hands the
	/// expression over already constructed, and this is the one place in the engine allowed to read it
	/// back apart, precisely because the interpreted reading is what this cross-check needs and the
	/// generated one cannot supply. <see cref="Regex.Options"/> READ BACK FROM A GENERATED RULE IS
	/// <c>CultureInvariant | ExplicitCapture</c>, PLUS <see cref="RegexOptions.IgnoreCase"/> WHERE THE
	/// LANGUAGE IS CASE-INSENSITIVE (<c>IgnoreCase | CultureInvariant</c> rules are legal, and
	/// the cross-check copies <see cref="Regex.Options"/> as it finds them) - in particular never <see cref="RegexOptions.Compiled"/>, which a source-
	/// generated regex has no reason to carry and which would otherwise need stripping before reaching
	/// an interpreter constructed at run time under Native AOT (a compiled interpreter uses
	/// <c>Reflection.Emit</c>, which Native AOT does not support). Nothing strips options here because
	/// there is, today, nothing to strip; a future rule declaring <c>RegexOptions.Compiled</c> on its
	/// own <c>[GeneratedRegex]</c> attribute - meaningless there, since the source generator already
	/// compiles at build time - would need this method revisited before it reached production, which is
	/// recorded here rather than guarded against silently.
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
	/// The confidence composition, and the two ways a match yields no finding at all. Neither of them is a
	/// Low finding: an anchor that does not resolve was never recognized, and a demonstrated
	/// negative scheme verdict means the construct is NOT AN INSTANCE OF THE CLASS - that is placed
	/// in recognition rather than in classification, which is what keeps "every finding
	/// receives exactly one level" true. A THIRD, NARROWER WAY: after a lexical budget trip the match
	/// or its anchor lies where <see cref="ZoneMask.IsAnalyzableAt"/> is false, and no finding is emitted there.
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
	/// THE LITERAL-CREDENTIAL SIGNAL, COMPUTED FROM THE ZONE AND NEVER FROM THE VALUE. The
	/// engine establishes that a credential is embedded in source without reading it, which is what
	/// makes the signal compatible with never reporting a secret's value.
	///
	/// IT IS COMPUTED HERE AND NOWHERE ELSE, because here is the only place the MATCH still exists.
	/// The credential position is a named capture group of the analyzer contract, and only a match turns
	/// a name into an offset; the stage that builds findings holds the text and the mask but not the
	/// match, so a signal computed there would mean running the expression a second time.
	///
	/// IT READS THE WHOLE CAPTURE EXTENT, DELIMITER-AGNOSTIC, RATHER THAN ONE OFFSET. The
	/// signal is true only where EVERY position across the credential group's captured extent is
	/// trustworthy per <see cref="ZoneMask.IsTrustworthyAt"/> and is either
	/// <see cref="LexicalZone.StringBody"/> or <see cref="LexicalZone.Delimiter"/>, AND at least one
	/// of those positions is <see cref="LexicalZone.StringBody"/>. A capture that opens on its own
	/// delimiter therefore signals true - the delimiter is still part of the literal - while a
	/// capture spanning a concatenation (`user + ":" + password`) signals false, because the
	/// operator and the identifiers it joins are <see cref="LexicalZone.Code"/> positions inside the
	/// same extent.
	///
	/// EVERY WAY OF NOT KNOWING IS FALSE RATHER THAN AN ERROR, and there are six: the rule
	/// declares no credential position (the contract makes the field optional and "none" the honest answer
	/// for a class where no argument is the credential), the declared group did not participate in
	/// this match, the group's captured extent is empty, some position in that extent is one the
	/// scanner does not vouch for, some position in that extent is <see cref="LexicalZone.Code"/>,
	/// or every position is a delimiter with no <see cref="LexicalZone.StringBody"/> position at
	/// all - an empty literal's quotes carry no body to report on. None of the six is evidence that
	/// a credential was written into the source, so none of them may report that it was.
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
	/// THE FOUR-CLAUSE RULE, OVER THE GROUP'S WHOLE CAPTURED EXTENT rather than one offset.
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
	/// THE CONFIDENCE TABLE, AND NOTHING ELSE. Every signal PROPOSES; the set is complete before anything
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
	/// THE MALFORMED-MATCH CATEGORY, EMITTED AT MOST ONCE PER FILE. The event is deterministic -
	/// the same expression against the same text on the same runtime produces the same corrupted
	/// match every time, unlike the match bound's own wall-clock exception - so nothing here
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
