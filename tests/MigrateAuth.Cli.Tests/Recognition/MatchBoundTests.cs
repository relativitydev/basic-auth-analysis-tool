using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE MATCH BOUND: WHAT IT STOPS, WHAT IT REPORTS, AND WHAT IT MUST NEVER SAY.
///
/// The bound is a declared EXCEPTION TO DETERMINISM - a wall-clock limit makes the finding set
/// depend on the machine - and it is accepted only on the condition that every file it stops is
/// counted in its own skipped category and reported like every other, so that the exception is
/// visible rather than silent. Both halves are tested here, and so is a third: this platform's
/// timeout exception exposes THE COMPLETE INPUT in one of its properties, so neither its message
/// nor its type may reach a log, an exception that escapes, or any other sink.
///
/// TWO ROUTES REACH THE SAME OUTCOME AND BOTH ARE EXERCISED. The engine's own cooperative timer,
/// which is what bounds an expression the analyzer handed over without a timeout of its own; and
/// the platform's <see cref="RegexMatchTimeoutException"/>, which arrives where an analyzer's
/// expression carries one. A run that handled only the first would fall over the day a catalog
/// shipped an expression built with a timeout.
///
/// NOTHING HERE WAITS FOR A CLOCK. The cooperative route is driven by a provider that advances on
/// every read, so the trip is decided by the engine's own structure rather than by how busy the
/// machine is; the platform route is driven by catastrophic backtracking against a one-millisecond
/// timeout, which is a property of the input and not of the hardware.
///
/// THE WATCH ACCUMULATES ANALYZER-EXPRESSION TIME
/// ONLY, NOT WALL-CLOCK TIME SINCE THE FILE OPENED. <see cref="MigrateAuth.Cli.Recognition.MatchBoundWatch"/>
/// reads the clock exactly twice per call to <c>Match</c>, <c>NextMatch</c> or <c>IsMatch</c> - once
/// to open the attempt, once to close it, through the shared door
/// <see cref="MigrateAuth.Cli.Recognition.TimedRegexEvaluation"/> - and adds only that difference to
/// the file's accumulator; nothing between attempts, including the malformed-match guard and
/// classification, reads the clock at all.
///
/// THE PROOF OF THAT HAS TO BE ABLE TO FAIL.
/// <see cref="TimeThatPassesBetweenMatchAttemptsIsNotChargedToTheBound"/> and its converse
/// <see cref="TimeThatPassesInsideASingleMatchAttemptIsChargedToTheBound"/> use
/// <see cref="PairedReadingTimeProvider"/> (in <c>RecognitionHarness.cs</c>) to separate time within one
/// <c>Begin</c>/<c>End</c> pair from time between one pair and the next.
/// </summary>
public sealed class MatchBoundTests
{
	/// <summary>
	/// The full bound, spent by a single match attempt. <see cref="MigrateAuth.Cli.Recognition.MatchBoundWatch"/>
	/// reads the clock once to open a match attempt and once to close it, so a provider that advances
	/// by this much per read charges exactly one bound's worth of matching time to whichever attempt
	/// is timed - tripping the watch at the very next poll, deterministically and regardless of which
	/// rule that attempt belongs to.
	/// </summary>
	private static readonly TimeSpan _theFullBoundPerMatchAttempt =
		TimeSpan.FromMilliseconds(RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS);

	private const string _A_FILE_WITH_SEVERAL_RECOGNIZABLE_CONSTRUCTS = """
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		let other = NewExemplarCredentials(serviceName, "hunter3")
		""";

	private const string _PROBE_RELATIVE_PATH = "source/pathological.ex1";

	private const string _PROBE_PROFILE_NAME = "MatchBoundProbe";
	private const string _PROBE_LANGUAGE_NAME = "MatchBoundProbeLanguage";
	private const string _PROBE_FILE_EXTENSION = ".probe";
	private const string _PROBE_PATTERN_CLASS = "PRB-001";
	private const string _PROBE_ANCHOR_CAPTURE_GROUP = "construct";
	private const string _PROBE_CONSTANT_DECLARATION_PATTERN = @"^\s*const\s+(?<name>\w+)\s*=\s*""(?<value>[^""]*)""";
	private const string _PROBE_PRESELECTION_ANCHOR = "probe";

	/// <summary>
	/// The documented catastrophic form: nested quantifiers over an input that cannot match. The
	/// engine has to explore an exponential number of ways to divide the run of word characters
	/// before it can conclude that the trailing character defeats the anchor.
	/// </summary>
	private const string _CATASTROPHIC_PATTERN = @"^(?<construct>(\w+\s?)*)$";

	private const string _INPUT_THAT_CANNOT_MATCH =
		"the quick brown fox jumps over the lazy dog and keeps running well past the point of reason!";

	private static readonly TimeSpan _anExpressionLevelTimeout = TimeSpan.FromMilliseconds(1);

	[Fact]
	public void TheBoundStopsTheFileAndReportsItUnderItsOwnSkippedCategory()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized = harness.RecognizeOn(
			new AdvancingTimeProvider(_theFullBoundPerMatchAttempt),
			_A_FILE_WITH_SEVERAL_RECOGNIZABLE_CONSTRUCTS);

		EmittedFact skip = Assert.Single(harness.SkippedItems());
		Assert.Empty(recognized);
		Assert.Equal(SkippedItemCategory.MatchBoundExceeded, skip.SkippedItem);
		Assert.Equal(harness.PathOf(RecognitionHarness.UNDEMOTED_RELATIVE_PATH), skip.Path);
	}

	/// <summary>
	/// The partial set is DISCARDED rather than kept. Which rules had already run when the bound
	/// tripped is machine-speed-dependent, so a kept remainder would be a finding set that differs
	/// between two runs of the same input while looking complete - and the determinism requirement grades on exactly that.
	/// </summary>
	[Fact]
	public void TheFileTheBoundStopsYieldsNothingEvenWhereItHadAlreadyYieldedSomething()
	{
		RecognitionHarness unstoppedRun = new();

		ImmutableArray<RecognizedConstruct> withoutTheBound =
			unstoppedRun.Recognize(_A_FILE_WITH_SEVERAL_RECOGNIZABLE_CONSTRUCTS);

		RecognitionHarness stoppedRun = new();

		ImmutableArray<RecognizedConstruct> withTheBound = stoppedRun.RecognizeOn(
			new AdvancingTimeProvider(_theFullBoundPerMatchAttempt),
			_A_FILE_WITH_SEVERAL_RECOGNIZABLE_CONSTRUCTS);

		Assert.NotEmpty(withoutTheBound);
		Assert.Empty(withTheBound);
	}

	/// <summary>
	/// The bound is an exception and must behave like one. A clock that does not move produces no
	/// skip at all, which is what keeps the skipped-item count a measurement of holes rather than
	/// a constant.
	/// </summary>
	[Fact]
	public void AFileThatFinishesInsideTheBoundIsNotReportedAsSkipped()
	{
		RecognitionHarness harness = new();

		ImmutableArray<RecognizedConstruct> recognized =
			harness.Recognize(_A_FILE_WITH_SEVERAL_RECOGNIZABLE_CONSTRUCTS);

		Assert.NotEmpty(recognized);
		Assert.Empty(harness.SkippedItems());
	}

	/// <summary>
	/// THE POINT OF THE ACCUMULATING WATCH, MADE OBSERVABLE.
	/// <see cref="PairedReadingTimeProvider"/> returns the SAME value for both reads of one match
	/// attempt (so every attempt's own <c>Begin</c>-to-<c>End</c> elapsed time is exactly zero) and
	/// advances the ABSOLUTE clock only between one attempt's <c>End</c> and the next attempt's
	/// <c>Begin</c> - a large jump that nothing under the watch ever reads, because
	/// <see cref="MigrateAuth.Cli.Recognition.MatchBoundWatch.HasBeenExceeded"/> is a pure comparison
	/// against the already-accumulated total and takes no clock reading of its own. However many match
	/// attempts the malformed-match guard, classification, subsumption and the symbol-table and
	/// demotion-signal phases brought under the bracket
	/// (<see cref="MigrateAuth.Cli.Recognition.TimedRegexEvaluation"/>) cause, each one still reads
	/// as zero elapsed, so the file finishes and nothing is reported as skipped. A watch that measured
	/// wall-clock time since the file opened would see the jump at its next poll and trip.
	/// </summary>
	[Fact]
	public void TimeThatPassesBetweenMatchAttemptsIsNotChargedToTheBound()
	{
		RecognitionHarness harness = new();
		TimeSpan wellPastTheBoundBetweenAttempts =
			TimeSpan.FromMilliseconds(RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS * 10.0);

		ImmutableArray<RecognizedConstruct> recognized = harness.RecognizeOn(
			new PairedReadingTimeProvider(wellPastTheBoundBetweenAttempts),
			_A_FILE_WITH_SEVERAL_RECOGNIZABLE_CONSTRUCTS);

		Assert.NotEmpty(recognized);
		Assert.Empty(harness.SkippedItems());
	}

	/// <summary>
	/// THE CONVERSE OF <see cref="TimeThatPassesBetweenMatchAttemptsIsNotChargedToTheBound"/>, using
	/// the same <see cref="PairedReadingTimeProvider"/> so the pairing is explicit: here the jump sits
	/// WITHIN one match attempt's own <c>Begin</c>-to-<c>End</c> pair rather than between two attempts
	/// - the one place the watch DOES read the clock - so the very first attempt already
	/// reports the full bound as elapsed and the file is stopped and counted. This is also proven,
	/// independently, by <see cref="TheBoundStopsTheFileAndReportsItUnderItsOwnSkippedCategory"/>'s
	/// use of the simpler <see cref="AdvancingTimeProvider"/>; this test exists to name the converse
	/// pairing explicitly rather than to add a third route to the same outcome.
	/// </summary>
	[Fact]
	public void TimeThatPassesInsideASingleMatchAttemptIsChargedToTheBound()
	{
		RecognitionHarness harness = new();
		TimeSpan theFullBoundWithinTheFirstAttempt =
			TimeSpan.FromMilliseconds(RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS);

		ImmutableArray<RecognizedConstruct> recognized = harness.RecognizeOn(
			new PairedReadingTimeProvider(TimeSpan.Zero, theFullBoundWithinTheFirstAttempt),
			_A_FILE_WITH_SEVERAL_RECOGNIZABLE_CONSTRUCTS);

		EmittedFact skip = Assert.Single(harness.SkippedItems());
		Assert.Empty(recognized);
		Assert.Equal(SkippedItemCategory.MatchBoundExceeded, skip.SkippedItem);
	}

	/// <summary>
	/// The platform's own route, on a real pathological input. What is asserted is not only that
	/// the file is stopped and counted, but that NOTHING of the exception survives the boundary:
	/// the run completes normally, and neither the console nor anything the emission point carried
	/// mentions the exception's type - whose message would carry the entire analyzed file.
	/// </summary>
	[Fact]
	public void APlatformMatchTimeoutIsCapturedLocallyAndLeaksNothingAboutTheFile()
	{
		RecognitionHarness harness = new();
		RecognitionEngine engine = new(harness.EmissionPoint);
		FileRecognitionContext context = BuildPathologicalContext(harness);
		StringWriter capturedOutput = new();
		StringWriter capturedError = new();
		TextWriter previousOutput = Console.Out;
		TextWriter previousError = Console.Error;

		ImmutableArray<RecognizedConstruct> recognized;

		try
		{
			Console.SetOut(capturedOutput);
			Console.SetError(capturedError);

			recognized = engine.Recognize(context).Constructs;
		}
		finally
		{
			Console.SetOut(previousOutput);
			Console.SetError(previousError);
		}

		EmittedFact skip = Assert.Single(harness.SkippedItems());
		Assert.Empty(recognized);
		Assert.Equal(SkippedItemCategory.MatchBoundExceeded, skip.SkippedItem);
		AssertNothingAboutTheTimeoutEscaped(harness, capturedOutput, capturedError);
	}

	/// <summary>
	/// The match bound ships as a named constant with a positive value rather than without one.
	/// </summary>
	[Fact]
	public void TheBoundIsANamedConstantWithAPositiveValue()
	{
		Assert.True(RecognitionEnginePolicy.MATCH_BOUND > TimeSpan.Zero);
		Assert.Equal(
			TimeSpan.FromMilliseconds(RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS),
			RecognitionEnginePolicy.MATCH_BOUND);
	}

	private static void AssertNothingAboutTheTimeoutEscaped(
		RecognitionHarness harness,
		StringWriter capturedOutput,
		StringWriter capturedError)
	{
		string everythingWritten = string.Concat(capturedOutput.ToString(), capturedError.ToString());

		Assert.DoesNotContain(nameof(RegexMatchTimeoutException), everythingWritten, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(nameof(TimeoutException), everythingWritten, StringComparison.OrdinalIgnoreCase);
		Assert.All(harness.Sink.Received, fact => Assert.False(fact.Rule.IsDeclared));
	}

	/// <summary>
	/// A probe analyzer whose expression carries a one-millisecond timeout of its own, which is
	/// the case the engine may not rebuild around: expressions are handed over already
	/// constructed, and a <c>Regex</c> fixes its timeout in its constructor.
	/// </summary>
	private static FileRecognitionContext BuildPathologicalContext(RecognitionHarness harness)
	{
		LexicalProfile profile = BuildProbeProfile();
		ZoneMask mask = new LexicalScanner(profile).Scan(_INPUT_THAT_CANNOT_MATCH);

		return new FileRecognitionContext
		{
			Analyzer = BuildProbeAnalyzer(profile),
			NormalizedText = _INPUT_THAT_CANNOT_MATCH,
			Mask = mask,
			Path = harness.PathOf(_PROBE_RELATIVE_PATH)
		};
	}

	private static ILanguageAnalyzer BuildProbeAnalyzer(LexicalProfile profile)
	{
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LexicalProfile).Returns(profile);
		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(BuildProbeIdentification());
		analyzer.SetupGet(supplied => supplied.DetectionRulesByPatternClass).Returns(BuildProbeDetectionRules());
		analyzer.SetupGet(supplied => supplied.DemotionSignals).Returns(LanguageShapedDemotionSignals.NoneDeclared);
		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation)
			.Returns(BuildProbeCandidateSelection());
		analyzer.SetupGet(supplied => supplied.SubsumptionRelation).Returns(FrozenSet<SubsumptionRule>.Empty);

		return analyzer.Object;
	}

	private static LexicalProfile BuildProbeProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROBE_PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_PROBE_CONSTANT_DECLARATION_PATTERN, RegexOptions.CultureInvariant),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = []
		};

		return profile;
	}

	private static LanguageIdentification BuildProbeIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _PROBE_FILE_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _PROBE_LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = DeriveProbeLanguageName
		};

		return identification;
	}

	private static string DeriveProbeLanguageName(string fileExtension, string? firstLine)
	{
		return _PROBE_LANGUAGE_NAME;
	}

	private static FrozenDictionary<string, DetectionRule> BuildProbeDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			[_PROBE_PATTERN_CLASS] = new DetectionRule
			{
				Expression = new Regex(_CATASTROPHIC_PATTERN, RegexOptions.CultureInvariant, _anExpressionLevelTimeout),
				AnchorPosition = MatchAnchor.AtCaptureGroup(_PROBE_ANCHOR_CAPTURE_GROUP),
				BaseLevel = ConfidenceLevel.High,
				SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
				SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
				DestinationCertainty = DestinationCertainty.Established,
				DisplayName = "Match bound probe pattern class"
			}
		};

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static CandidateSelectionAndRecommendation BuildProbeCandidateSelection()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = new[] { new PreselectionAnchor { Text = _PROBE_PRESELECTION_ANCHOR } }.ToFrozenSet(),
			TemplateProvider = ProvideNoTemplate,
			FlowSignals = []
		};

		return element;
	}

	private static RecommendationTemplate? ProvideNoTemplate(string patternClassIdentifier, CredentialFlow flow)
	{
		return null;
	}
}
