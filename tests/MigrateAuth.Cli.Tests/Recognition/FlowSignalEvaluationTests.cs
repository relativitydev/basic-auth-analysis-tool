using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Findings;
using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// FLOW-SIGNAL EVALUATION AT THE ENGINE, OVER A FIXTURE ANALYZER
/// THAT DECLARES SIGNALS UNDER TESTS.
///
/// The reference analyzer is wrapped, not edited: <see cref="FlowSignalFixtureAnalyzer"/> replaces its
/// signals and leaves everything else its own, so each test states the one thing it varies. The mask
/// is the real scanner's, because half of what is tested here is what a signal does with zones.
/// </summary>
public sealed class FlowSignalEvaluationTests
{
	private const string _CREDENTIAL_CONSTRUCTION = "let credentials = NewExemplarCredentials(operatorName, \"hunter2\")";
	private const string _COMMENTED_CREDENTIAL_CONSTRUCTION = "-- let credentials = NewExemplarCredentials(operatorName, \"hunter2\")";
	private const string _LINE_COMMENT_PREFIX = "-- ";
	private const string _BLOCK_COMMENT_OPENING = "<# ";
	private const string _BLOCK_COMMENT_CLOSING = " #>";
	private const string _NEWLINE = "\n";

	/// <summary>Only literals that are not the credential itself: a string body carrying a token.</summary>
	private const string _STRING_WITH_INTERACTIVE_TOKEN_FORMAT = "let banner = \"{0}\"";

	private const string _QUOTED_DELIMITER_LINE = "let banner = \"DelimiterTail\"";
	private const string _CODE_CALL_FORMAT = "let action = {0}()";
	private const string _CATASTROPHIC_PATTERN = @"^(?<construct>(\w+\s?)*)$";
	private const string _INPUT_THAT_CANNOT_MATCH = "the quick brown fox jumps over the lazy dog and keeps running well past the point of reason!";
	private const string _TIMING_OUT_SIGNAL_NAME = "fixture-timing-out";
	private const string _EMBEDDED_CSHARP_LANGUAGE_NAME = "CSharp";
	private const int _EMBEDDED_CSHARP_FLOW_SIGNAL_COUNT = 17;
	private const string _CATALOG_VERSION_WITH_THE_CSHARP_DETECTION_SHAPES = "1.31.0";
	private const string _SOURCE_RELATIVE_PATH = "source/module.ex1";
	private const int _FIRST_SIGNAL_ATTEMPTS = 2;
	private const int _ONE_MILLISECOND = 1;

	private static readonly TimeSpan _theFullBound =
		TimeSpan.FromMilliseconds(RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS);

	// ---- (a) no signal fires inside a comment ----

	[Theory]
	[InlineData(FlowSignalZone.CodeOnly)]
	[InlineData(FlowSignalZone.CodeOrStringBody)]
	public void NoSignalFiresInsideALineComment(FlowSignalZone zone)
	{
		string source = SourceOf(_CREDENTIAL_CONSTRUCTION, _LINE_COMMENT_PREFIX + FlowSignalFixtures.INTERACTIVE_TOKEN);

		RecognitionResult result = Recognize(source, [InteractiveSignalIn(zone)]).Result;

		Assert.NotEmpty(result.Constructs);
		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.False(result.FlowEvaluationTripped);
	}

	[Theory]
	[InlineData(FlowSignalZone.CodeOnly)]
	[InlineData(FlowSignalZone.CodeOrStringBody)]
	public void NoSignalFiresInsideABlockComment(FlowSignalZone zone)
	{
		string commented = _BLOCK_COMMENT_OPENING + FlowSignalFixtures.INTERACTIVE_TOKEN + _BLOCK_COMMENT_CLOSING;
		string source = SourceOf(_CREDENTIAL_CONSTRUCTION, commented);

		RecognitionResult result = Recognize(source, [InteractiveSignalIn(zone)]).Result;

		Assert.NotEmpty(result.Constructs);
		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	// ---- (b) zones ----

	[Fact]
	public void ACodeOnlySignalFiresOnACodePosition()
	{
		string source = SourceOf(_CREDENTIAL_CONSTRUCTION, string.Format(_CODE_CALL_FORMAT, FlowSignalFixtures.INTERACTIVE_TOKEN));

		RecognitionResult result = Recognize(source, [FlowSignalFixtures.InteractiveCodeOnly]).Result;

		Assert.Equal(FlowEvidence.Interactive, result.FlowEvidence);
	}

	[Fact]
	public void ACodeOnlySignalDoesNotFireInsideAStringBodyButCodeOrStringBodyDoes()
	{
		string source = SourceOf(
			_CREDENTIAL_CONSTRUCTION,
			string.Format(_STRING_WITH_INTERACTIVE_TOKEN_FORMAT, FlowSignalFixtures.INTERACTIVE_TOKEN));

		RecognitionResult codeOnly = Recognize(source, [FlowSignalFixtures.InteractiveCodeOnly]).Result;
		RecognitionResult codeOrStringBody = Recognize(source, [FlowSignalFixtures.InteractiveCodeOrStringBody]).Result;

		Assert.Equal(FlowEvidence.None, codeOnly.FlowEvidence);
		Assert.Equal(FlowEvidence.Interactive, codeOrStringBody.FlowEvidence);
	}

	[Fact]
	public void ACodeOnlySignalDoesNotFireOnADelimiterPositionButCodeOrStringBodyDoes()
	{
		string source = SourceOf(_CREDENTIAL_CONSTRUCTION, _QUOTED_DELIMITER_LINE);
		int delimiterPosition = source.IndexOf(_QUOTED_DELIMITER_LINE, StringComparison.Ordinal)
			+ _QUOTED_DELIMITER_LINE.IndexOf('"');
		RecognitionHarness harness = new();

		FileRecognitionContext context = harness.ContextFor(source);
		RecognitionResult codeOnly = Recognize(source, [FlowSignalFixtures.DelimiterAnchoredCodeOnly]).Result;
		RecognitionResult codeOrStringBody = Recognize(source, [FlowSignalFixtures.DelimiterAnchoredCodeOrStringBody]).Result;

		Assert.Equal(LexicalZone.Delimiter, context.Mask.ZoneAt(delimiterPosition));
		Assert.Equal(FlowEvidence.None, codeOnly.FlowEvidence);
		Assert.Equal(FlowEvidence.Fixed, codeOrStringBody.FlowEvidence);
	}

	// ---- (c) a truncated mask ----

	[Fact]
	public void ASignalAnchoredPastATruncatedMasksValidExtentDoesNotFire()
	{
		string source = SourceOf(_CREDENTIAL_CONSTRUCTION, _BLOCK_COMMENT_OPENING + FlowSignalFixtures.INTERACTIVE_TOKEN);
		int tokenPosition = source.IndexOf(FlowSignalFixtures.INTERACTIVE_TOKEN, StringComparison.Ordinal);
		FileRecognitionContext context = new RecognitionHarness().ContextFor(source);

		Assert.False(context.Mask.IsCompletelyValid);
		Assert.True(tokenPosition >= context.Mask.ValidThroughPosition);
		Assert.False(context.Mask.IsTrustworthyAt(tokenPosition));

		RecognitionResult codeOrStringBody = Recognize(source, [FlowSignalFixtures.InteractiveCodeOrStringBody]).Result;
		RecognitionResult codeOnly = Recognize(source, [FlowSignalFixtures.InteractiveCodeOnly]).Result;

		Assert.NotEmpty(codeOrStringBody.Constructs);
		Assert.Equal(FlowEvidence.None, codeOrStringBody.FlowEvidence);
		Assert.Equal(FlowEvidence.None, codeOnly.FlowEvidence);
	}

	// ---- (d) a signal timeout ----

	[Fact]
	public void AFlowSignalTimeoutOnTheBoundKeepsTheConstructsLeavesEvidenceNoneAndCountsTheTrip()
	{
		string source = SourceOf(_CREDENTIAL_CONSTRUCTION, string.Format(_CODE_CALL_FORMAT, FlowSignalFixtures.INTERACTIVE_TOKEN));
		ImmutableArray<FlowSignal> signals = [FlowSignalFixtures.InteractiveCodeOnly];
		RecognitionHarness controlHarness = new();
		RecognitionResult untripped = new RecognitionEngine(controlHarness.EmissionPoint, new FrozenTimeProvider())
			.Recognize(ContextWith(controlHarness, source, signals));

		long readsBeforeTheFlowPhase = CountClockReads(source, []);
		RecognitionHarness harness = new();
		RecognitionEngine engine = new(harness.EmissionPoint, new ChargesTheBoundAfterProvider(readsBeforeTheFlowPhase, _theFullBound));

		RecognitionResult tripped = engine.Recognize(ContextWith(harness, source, signals));

		Assert.Equal(FlowEvidence.Interactive, untripped.FlowEvidence);
		Assert.NotEmpty(tripped.Constructs);
		Assert.True(untripped.Constructs.SequenceEqual(tripped.Constructs));
		Assert.Equal(FlowEvidence.None, tripped.FlowEvidence);
		Assert.True(tripped.FlowEvaluationTripped);
		Assert.Equal(1, engine.FlowSignalTripCount);
		Assert.Equal(1, engine.FilesEvaluatedForFlowSignals);
		Assert.Empty(harness.SkippedItems());
	}

	/// <summary>
	/// The platform's own route, on a deliberately non-policy expression: a pathological pattern that
	/// carries a one-millisecond timeout of its own. The exception is captured in the flow phase alone.
	/// </summary>
	[Fact]
	public void AFlowSignalsOwnMatchTimeoutKeepsTheConstructsAndDoesNotSkipTheFile()
	{
		FlowSignal timingOut = new()
		{
			Name = _TIMING_OUT_SIGNAL_NAME,
			Kind = FlowSignalKind.InteractiveUser,
			Zone = FlowSignalZone.CodeOnly,
			Expression = new Regex(_CATASTROPHIC_PATTERN, RegexOptions.None, TimeSpan.FromMilliseconds(_ONE_MILLISECOND))
		};
		string source = SourceOf(_INPUT_THAT_CANNOT_MATCH, _CREDENTIAL_CONSTRUCTION);
		RecognitionHarness harness = new();
		RecognitionEngine engine = new(harness.EmissionPoint);

		RecognitionResult result = engine.Recognize(ContextWith(harness, source, [timingOut]));

		Assert.NotEmpty(result.Constructs);
		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.True(result.FlowEvaluationTripped);
		Assert.Equal(1, engine.FlowSignalTripCount);
		Assert.Empty(harness.SkippedItems());
	}

	// ---- precedence, order-independence, gating ----

	[Fact]
	public void AnInteractiveSignalWinsOverAFixedOne()
	{
		string source = SourceOf(
			_CREDENTIAL_CONSTRUCTION,
			string.Format(_CODE_CALL_FORMAT, FlowSignalFixtures.FIXED_TOKEN),
			string.Format(_CODE_CALL_FORMAT, FlowSignalFixtures.INTERACTIVE_TOKEN));

		RecognitionResult fixedFirst = Recognize(
			source,
			[FlowSignalFixtures.FixedCodeOnly, FlowSignalFixtures.InteractiveCodeOnly]).Result;
		RecognitionResult interactiveFirst = Recognize(
			source,
			[FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly]).Result;

		Assert.Equal(FlowEvidence.Interactive, fixedFirst.FlowEvidence);
		Assert.Equal(FlowEvidence.Interactive, interactiveFirst.FlowEvidence);
	}

	[Fact]
	public void AFixedSignalAloneYieldsFixedEvidence()
	{
		string source = SourceOf(_CREDENTIAL_CONSTRUCTION, string.Format(_CODE_CALL_FORMAT, FlowSignalFixtures.FIXED_TOKEN));

		RecognitionResult result = Recognize(
			source,
			[FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly]).Result;

		Assert.Equal(FlowEvidence.Fixed, result.FlowEvidence);
	}

	/// <summary>
	/// No short circuit between signals: with the first signal already firing, the second is still
	/// evaluated, which shows as its own two clock reads on the bound's watch.
	/// </summary>
	[Fact]
	public void EverySignalIsEvaluatedEvenWhenAnEarlierOneAlreadyFired()
	{
		string source = SourceOf(
			_CREDENTIAL_CONSTRUCTION,
			string.Format(_CODE_CALL_FORMAT, FlowSignalFixtures.INTERACTIVE_TOKEN));

		long readsWithNoSignal = CountClockReads(source, []);
		long readsWithOneSignal = CountClockReads(source, [FlowSignalFixtures.InteractiveCodeOnly]);
		long readsWithTwoSignals = CountClockReads(
			source,
			[FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly]);

		Assert.Equal(readsWithNoSignal + _FIRST_SIGNAL_ATTEMPTS, readsWithOneSignal);
		Assert.Equal(readsWithOneSignal + _FIRST_SIGNAL_ATTEMPTS, readsWithTwoSignals);
	}

	[Fact]
	public void AFileHoldingOnlyLowConstructsIsNotEvaluated()
	{
		string source = SourceOf(
			_COMMENTED_CREDENTIAL_CONSTRUCTION,
			string.Format(_CODE_CALL_FORMAT, FlowSignalFixtures.INTERACTIVE_TOKEN));

		(RecognitionResult result, RecognitionEngine engine) = Recognize(source, [FlowSignalFixtures.InteractiveCodeOnly]);

		Assert.NotEmpty(result.Constructs);
		Assert.All(result.Constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.Equal(0, engine.FilesEvaluatedForFlowSignals);
	}

	[Fact]
	public void AFileHoldingAHighConstructIsCountedAsEvaluatedWhetherOrNotAnySignalFired()
	{
		(RecognitionResult result, RecognitionEngine engine) = Recognize(
			SourceOf(_CREDENTIAL_CONSTRUCTION),
			[FlowSignalFixtures.InteractiveCodeOnly]);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.Equal(1, engine.FilesEvaluatedForFlowSignals);
		Assert.Equal(0, engine.FlowSignalTripCount);
	}

	// ---- A literal credential is not flow evidence ----

	[Fact]
	public void ALiteralCredentialIsNotFlowEvidence()
	{
		RecognitionResult result = Recognize(SourceOf(_CREDENTIAL_CONSTRUCTION), [FlowSignalFixtures.FixedCodeOnly]).Result;

		RecognizedConstruct construct = Assert.Single(result.Constructs);
		Assert.True(construct.LiteralCredentialSignal);
		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	// ---- Evidence is carried onto High and Medium findings only ----

	[Fact]
	public void CompositionCopiesTheFilesEvidenceOntoHighAndMediumFindingsAndNoneOntoLow()
	{
		FindingHarness harness = new();
		RecognizedFile file = harness.FileOf(
			_SOURCE_RELATIVE_PATH,
			FindingHarness.FillerText(lineCount: 4, lineLength: 20),
			FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, 0, ConfidenceLevel.High),
			FindingHarness.ConstructAt(FindingHarness.HAND_ASSEMBLED_HEADER_PATTERN_CLASS, 21, ConfidenceLevel.Medium),
			FindingHarness.ConstructAt(FindingHarness.LEGACY_CLIENT_PATTERN_CLASS, 42, ConfidenceLevel.Low)) with
		{
			FlowEvidence = FlowEvidence.Interactive
		};

		ImmutableArray<Finding> findings = FindingComposition.Compose([file]);

		Assert.Equal(3, findings.Length);
		Assert.Equal(FlowEvidence.Interactive, findings.Single(finding => finding.Level == ConfidenceLevel.High).FlowEvidence);
		Assert.Equal(FlowEvidence.Interactive, findings.Single(finding => finding.Level == ConfidenceLevel.Medium).FlowEvidence);
		Assert.Null(findings.Single(finding => finding.Level == ConfidenceLevel.Low).FlowEvidence);
	}

	// ---- the embedded analyzer ----

	[Fact]
	public void TheEmbeddedCSharpAnalyzerDeclaresItsFlowSignalsAndValidatesClean()
	{
		ILanguageAnalyzer analyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog()
			.AnalyzersByLanguageName[_EMBEDDED_CSHARP_LANGUAGE_NAME];

		Assert.Equal(_EMBEDDED_CSHARP_FLOW_SIGNAL_COUNT, analyzer.CandidateSelectionAndRecommendation.FlowSignals.Length);
		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	[Fact]
	public void TheCatalogVersionMovedAgainWhenTheCSharpDetectionShapesChanged()
	{
		Assert.Equal(_CATALOG_VERSION_WITH_THE_CSHARP_DETECTION_SHAPES, LanguageCatalog.CurrentVersion.DisplayValue);
	}

	// ---- helpers ----

	private static FlowSignal InteractiveSignalIn(FlowSignalZone zone)
	{
		return zone is FlowSignalZone.CodeOnly
			? FlowSignalFixtures.InteractiveCodeOnly
			: FlowSignalFixtures.InteractiveCodeOrStringBody;
	}

	private static string SourceOf(params string[] lines)
	{
		return string.Join(_NEWLINE, lines);
	}

	private static FileRecognitionContext ContextWith(RecognitionHarness harness, string source, ImmutableArray<FlowSignal> signals)
	{
		return harness.ContextFor(source) with
		{
			Analyzer = new FlowSignalFixtureAnalyzer(harness.Analyzer, signals)
		};
	}

	private static (RecognitionResult Result, RecognitionEngine Engine) Recognize(string source, ImmutableArray<FlowSignal> signals)
	{
		RecognitionHarness harness = new();
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		return (engine.Recognize(ContextWith(harness, source, signals)), engine);
	}

	private static long CountClockReads(string source, ImmutableArray<FlowSignal> signals)
	{
		RecognitionHarness harness = new();
		CountingTimeProvider clock = new();

		new RecognitionEngine(harness.EmissionPoint, clock).Recognize(ContextWith(harness, source, signals));

		return clock.Reads;
	}
}

/// <summary>A clock that does not move and counts how often it was read, so a test can count match attempts.</summary>
internal sealed class CountingTimeProvider : TimeProvider
{
	public long Reads { get; private set; }

	public override long TimestampFrequency => TimeSpan.TicksPerSecond;

	public override long GetTimestamp()
	{
		Reads++;

		return 0;
	}
}

/// <summary>
/// A clock that reads zero for the first reads and then moves by a fixed step on every read. Given
/// the number of reads recognition makes before the flow phase, it charges the bound to the flow
/// phase alone and leaves every earlier phase free.
/// </summary>
internal sealed class ChargesTheBoundAfterProvider : TimeProvider
{
	private readonly long _freeReads;
	private readonly long _stepInTicks;

	private long _reads;
	private long _timestamp;

	internal ChargesTheBoundAfterProvider(long freeReads, TimeSpan step)
	{
		_freeReads = freeReads;
		_stepInTicks = step.Ticks;
	}

	public override long TimestampFrequency => TimeSpan.TicksPerSecond;

	public override long GetTimestamp()
	{
		long current = _timestamp;

		_reads++;

		if (_reads > _freeReads)
		{
			_timestamp += _stepInTicks;
		}

		return current;
	}
}
