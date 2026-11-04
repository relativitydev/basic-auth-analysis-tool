using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE LEXICAL BUDGET: A CUMULATIVE, PER-FILE, PER-STAGE WATCH OVER THE THREE
/// ANALYZER-DECLARED EXPRESSIONS THAT RUN OUTSIDE THE RECOGNITION ENGINE'S MATCH BOUND.
///
/// EVERY TRIP IS FORCED WITH A CLOCK THE TEST CONTROLS, and never by waiting: an
/// <see cref="AdvancingTimeProvider"/> whose step is a chosen fraction of the budget turns "how
/// much time did matching take" into "how many match attempts were charged", which the scanner's own
/// structure decides and which is identical on every machine. The one route a clock cannot force - a
/// single expression that exceeds its own regex timeout - is driven with a genuinely catastrophic
/// expression and a one-millisecond timeout under a clock that never moves, so that only the timeout
/// can be what trips the budget.
///
/// THE FIXTURE ANALYZER DECLARES ALL THREE EXPRESSIONS, which is why the tests run over it.
/// </summary>
public sealed class LexicalBudgetTests
{
	private const int _ONE_MILLISECOND_TIMEOUT = 1;
	private const int _CATASTROPHIC_REPETITION_COUNT = 60;
	private const int _STEPS_TO_SPEND_THE_BUDGET_ON_THE_SECOND_ATTEMPT = 2;
	private const int _ATTEMPTS_THAT_SPEND_THE_BUDGET_IN_A_PIPELINE = 25;
	private const int _HOLES_IN_THE_PIPELINE_HERE_DOCUMENT = 40;
	private const double _FRACTION_OF_THE_BUDGET_A_SINGLE_ATTEMPT_MAY_SPEND = 0.6;

	private const string _CATASTROPHIC_PATTERN = "(?:a|aa)+$";
	private const string _HERE_DOCUMENT_TERMINATOR_GROUP = "terminator";
	private const string _HERE_DOCUMENT_OPENING = "<<";
	private const string _CONSTANT_DECLARATION_PATTERN = @"^\s*const\s+(?<name>\w+)";
	private const string _PROFILE_NAME = "budget-probe";
	private const string _QUOTE = "\"";
	private const string _ARITHMETIC_OPENING = "((";
	private const string _ARITHMETIC_SOURCE = "echo a\n(( 1 + 2 ))\necho b\n";
	private const string _UNCLOSED_ARITHMETIC_GROUP = "(( x ";
	private const int _UNCLOSED_GROUPS_ON_THE_LINE = 50;
	private const string _IDENTIFIER_SIGIL = "$";

	private const string _EXEMPLAR_LANGUAGE_NAME = "ExemplarOne";
	private const string _COMMENT_LINE = "-- a comment before anything anchored\n";
	private const string _HERE_DOCUMENT_OPENING_LINE = "<<EOT\n";
	private const string _HERE_DOCUMENT_BODY_WITH_A_HOLE = "body $name here\n";
	private const string _HERE_DOCUMENT_CLOSING_LINE = "EOT\n";
	private const string _PLAIN_HERE_DOCUMENT = "<<EOT\nplain body\nEOT\n";
	private const string _EXEMPLAR_CONSTRUCT = "NewExemplarCredentials";
	private const string _CREDENTIAL_BEFORE_THE_TRIP = "NewExemplarCredentials(alpha, \"one\")\n";
	private const string _CREDENTIAL_AFTER_THE_TRIP = "NewExemplarCredentials(beta, \"two\")\n";
	private const string _CREDENTIAL_IN_ANOTHER_FILE = "NewExemplarCredentials(gamma, \"three\")\n";
	private const string _TRIPPING_FILE_NAME = "tripping.cs";
	private const string _UNAFFECTED_FILE_NAME = "unaffected.cs";
	private const string _EXTENSIONLESS_FILE_NAME = "build-script";
	private const string _FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE = "#!/usr/bin/env exemplar\n";
	private const string _SECOND_FILE_CONTENT_LINE = "NewExemplarCredentials(delta, \"four\")\n";
	private const string _UNRECOGNIZED_LANGUAGE_NAME = "unrecognized";

	private const string _SOURCE_FOLDER_NAME = "src";
	private const string _SOLUTION_FILE_PATTERN = "*.sln";
	private const string _SOURCE_FILE_PATTERN = "*.cs";
	private const string _BUILD_OUTPUT_FOLDER_SEGMENT_BIN = "bin";
	private const string _BUILD_OUTPUT_FOLDER_SEGMENT_OBJ = "obj";
	private const string _COMMENT_LINE_PREFIX = "//";
	private const string _MARKDOWN_FALLBACK_LINE_FRAGMENT = "first-line detection exceeded its budget";
	private const string _JSON_FALLBACK_FIELD_FRAGMENT = "\"firstLineFallbackFiles\":1";
	private const string _JSON_NO_FALLBACK_FIELD_FRAGMENT = "\"firstLineFallbackFiles\":0";
	private const string _ANALYZE_STAGE_SOURCE_PATH = "Pipeline/AnalyzeStage.cs";
	private const string _LANGUAGE_ATTRIBUTION_TYPE_NAME = "LanguageAttribution";

	private const string _EVALUATION_CALL_PATTERN = @"(?<receiver>[\w\.\(\)!]*)\s*\.(?:Match|Matches|IsMatch|EnumerateMatches|NextMatch)\s*\(";
	private const string _RECEIVER_GROUP = "receiver";

	/// <summary>The two files that ARE the doors: every other evaluation of an analyzer-declared expression goes through one of them.</summary>
	private static readonly string[] _doorFileNames = ["TimedRegexEvaluation.cs", "LexicalBudgetWatch.cs"];

	/// <summary>
	/// The receivers of an evaluation call that reach a door. A call whose receiver is not one of these
	/// and that sits outside a door file must be an engine-own expression, and is named in
	/// <see cref="_engineOwnEvaluations"/>.
	/// </summary>
	private static readonly string[] _doorReceivers = ["TimedRegexEvaluation", "_budgetWatch"];

	/// <summary>
	/// Expressions the engine itself declares - never an analyzer's - and therefore outside both
	/// budgets by construction: a source file and the receiver text of its one evaluation call.
	/// </summary>
	private static readonly (string FileName, string Receiver)[] _engineOwnEvaluations =
	[
		("RuleIdentifier.cs", "CatalogIdentifierShape()")
	];

	/// <summary>
	/// Every Regex-typed member of the language analyzer contract, by declaring type and name. The structural
	/// test below is only as complete as this list, so a test asserts the list is the contract's
	/// whole set: an expression added to the contract fails there until it has been considered.
	/// </summary>
	private static readonly string[] _analyzerDeclaredExpressionMembers =
	[
		"CapturedTerminator.OpeningExpression",
		"ContentMarker.Expression",
		"DetectionRule.Expression",
		"FlowSignal.Expression",
		"LanguageIdentification.FirstLineDetectionExpression",
		"LanguageShapedDemotionSignals.GenerationMarkerExpressions",
		"LexicalProfile.ConstantDeclarationExpression",
		"QuoteRule.InterpolationIdentifierExtent"
	];

	[Fact]
	public void AClockThatNeverMovesNeverTripsTheBudget()
	{
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile(), new FrozenTimeProvider());
		string source = BuildHereDocumentSource();

		ZoneMask mask = scanner.Scan(source);

		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
	}

	/// <summary>
	/// The budget runs out on the SECOND charged attempt - the interpolation identifier inside the
	/// here-document's body - so the scan stops with the here-document half read. The mask is sound up
	/// to the opening of the outermost construct being read, which is the here-document's own opening,
	/// and the rest is filled in the demoting direction.
	/// </summary>
	[Fact]
	public void ABudgetSpentPartwayAbandonsTheScanAtTheOutermostConstructBeingRead()
	{
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile(), BuildClockSpendingTheBudgetOnTheSecondAttempt());
		string source = BuildHereDocumentSource();
		int hereDocumentStart = source.IndexOf(_HERE_DOCUMENT_OPENING, StringComparison.Ordinal);

		ZoneMask mask = scanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(hereDocumentStart, mask.ValidThroughPosition);
		ZoneAssertions.SpansZone(mask, source, _COMMENT_LINE.TrimEnd('\n'), LexicalZone.Comment);
		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(source.Length - 1));
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	[Fact]
	public void ATripOnTheHereDocumentOpeningItselfStopsBeforeTheHereDocument()
	{
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile(), new AdvancingTimeProvider(LexicalEnginePolicy.LEXICAL_BUDGET));
		string source = BuildHereDocumentSource();

		ZoneMask mask = scanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		Assert.Equal(source.IndexOf(_HERE_DOCUMENT_OPENING, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>
	/// The arithmetic opening's read-ahead is charged to the budget like an expression search is: a read-ahead that spends it
	/// abandons the scan at the construct being read, which is the double parenthesis, and the rest is filled in the demoting direction.
	/// </summary>
	[Fact]
	public void AnArithmeticReadAheadThatSpendsTheBudgetAbandonsTheScanAtTheDoubleParenthesis()
	{
		LexicalScanner scanner = new(ShellShapedProfile.Build(), new AdvancingTimeProvider(LexicalEnginePolicy.LEXICAL_BUDGET));
		string source = _ARITHMETIC_SOURCE;

		ZoneMask mask = scanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		Assert.True(mask.ValidThroughPosition <= source.IndexOf(_ARITHMETIC_OPENING, StringComparison.Ordinal));
		Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, scanner.ChargeOfTheLastScan.ArithmeticReadAhead);
		Assert.Equal(TimeSpan.Zero, scanner.ChargeOfTheLastScan.Expressions);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	/// <summary>A clock that never moves charges the read-ahead nothing, so no file is abandoned for it.</summary>
	[Fact]
	public void AnArithmeticReadAheadUnderAClockThatNeverMovesNeverTripsTheBudget()
	{
		LexicalScanner scanner = new(ShellShapedProfile.Build(), new FrozenTimeProvider());

		ZoneMask mask = scanner.Scan(_ARITHMETIC_SOURCE);

		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.Equal(TimeSpan.Zero, scanner.ChargeOfTheLastScan.Total);
	}

	/// <summary>
	/// One read-ahead answers every run it passes, so a line of many double parentheses that never close is charged ONCE
	/// and not once per run: with a clock whose step is half the budget the file is not abandoned, and the whole charge is that one step.
	/// </summary>
	[Fact]
	public void ManyDoubleParenthesesPassedByOneReadAheadAreChargedOnce()
	{
		TimeSpan step = LexicalEnginePolicy.LEXICAL_BUDGET / _STEPS_TO_SPEND_THE_BUDGET_ON_THE_SECOND_ATTEMPT;
		LexicalScanner scanner = new(ShellShapedProfile.Build(), new AdvancingTimeProvider(step));
		string source = $"{string.Concat(Enumerable.Repeat(_UNCLOSED_ARITHMETIC_GROUP, _UNCLOSED_GROUPS_ON_THE_LINE))}\n";

		ZoneMask mask = scanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.Equal(step, scanner.ChargeOfTheLastScan.ArithmeticReadAhead);
	}

	/// <summary>The budget is one per file: a second file starts from zero however much the first one spent.</summary>
	[Fact]
	public void TheBudgetIsPerFileAndNotPerScanner()
	{
		TimeSpan step = LexicalEnginePolicy.LEXICAL_BUDGET * _FRACTION_OF_THE_BUDGET_A_SINGLE_ATTEMPT_MAY_SPEND;
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile(), new AdvancingTimeProvider(step));

		ZoneMask first = scanner.Scan(_PLAIN_HERE_DOCUMENT);
		ZoneMask second = scanner.Scan(_PLAIN_HERE_DOCUMENT);

		Assert.True(first.IsCompletelyValid);
		Assert.True(second.IsCompletelyValid);
	}

	[Fact]
	public void AHereDocumentOpeningThatExceedsItsOwnTimeoutTripsTheBudget()
	{
		LexicalScanner scanner = new(BuildProfileWithACatastrophicHereDocumentOpening(), new FrozenTimeProvider());
		string source = $"first = 1\n{_HERE_DOCUMENT_OPENING}{BuildCatastrophicText()}\nlast = 2\n";

		ZoneMask mask = scanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		Assert.Equal(source.IndexOf(_HERE_DOCUMENT_OPENING, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	[Fact]
	public void AnIdentifierExtentThatExceedsItsOwnTimeoutTripsTheBudget()
	{
		LexicalScanner scanner = new(BuildProfileWithACatastrophicIdentifierExtent(), new FrozenTimeProvider());
		string source = $"first = 1\n{_QUOTE}{_IDENTIFIER_SIGIL}{BuildCatastrophicText()}{_QUOTE}\nlast = 2\n";

		ZoneMask mask = scanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		Assert.Equal(source.IndexOf(_QUOTE, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>
	/// A trip reaches the same category every other abandoned scan does, exactly once per file, through
	/// the one emission door - and a file that did not trip reports nothing.
	/// </summary>
	[Fact]
	public void ATripIsReportedOnceAsALexicalAnomaly()
	{
		SkippedItemLedger ledger = new();
		FileLexicalScanner scanner = new(
			new EmissionPoint([ledger]),
			BuildClockSpendingTheBudgetOnTheSecondAttempt());

		ZoneMask mask = scanner.Scan(ReferenceProfileScanner.Profile(), BuildHereDocumentSource(), default);

		Assert.Equal(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		Assert.Equal(1, ledger.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
		Assert.Equal(1, ledger.SkippedFiles);
	}

	/// <summary>
	/// A trip during Analyze reports the file once under the lexical-anomaly category and
	/// counts nothing under the match bound. What the scanner established before the trip is kept: the
	/// construct written before the here-document is a finding at the level the mask proves. Nothing
	/// at or after the trip position is emitted at any level, because the text past it was never read
	/// under the watch. The session completes with every other file still analyzed.
	/// </summary>
	[Fact]
	public void ATripDuringAnalyzeKeepsTheValidPrefixReportsTheFileOnceAndTheSessionCompletes()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverCatalog(
			new LanguageCatalog(LanguageCatalog.CurrentVersion, [new BudgetDeclaringPipelineAnalyzer()]),
			BuildClockSpendingTheBudgetAfterManyAttempts());

		harness.WriteSourceFile(_TRIPPING_FILE_NAME, ForThePipelineAnalyzer(BuildHereDocumentSourceAroundCredentials()));
		harness.WriteSourceFile(_UNAFFECTED_FILE_NAME, ForThePipelineAnalyzer(_CREDENTIAL_IN_ANOTHER_FILE));

		harness.RunAndCommit(CommandId.Scan);
		StageRunOutcome outcome = harness.Run(CommandId.Analyze);

		ImmutableArray<Finding> tripped = [.. harness.State.Findings.Where(finding => finding.Path.Value == _TRIPPING_FILE_NAME)];
		ImmutableArray<Finding> unaffected = [.. harness.State.Findings.Where(finding => finding.Path.Value == _UNAFFECTED_FILE_NAME)];

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Equal(1, harness.SkippedItems.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
		Assert.Equal(0, harness.SkippedItems.TallyOf(SkippedItemCategory.MatchBoundExceeded).Count);
		Assert.Equal(1, harness.SkippedItems.SkippedFiles);
		Assert.Equal(ConfidenceLevel.High, Assert.Single(unaffected).Level);
		Finding beforeTheTrip = Assert.Single(tripped);
		Assert.Equal(1, beforeTheTrip.Line);
		Assert.Equal(ConfidenceLevel.High, beforeTheTrip.Level);
	}

	/// <summary>
	/// At engine level, the construct before the here-document is recognized at the level
	/// the mask proves, and there is not one construct at or after the trip position, at any level.
	/// </summary>
	[Fact]
	public void ATripEmitsTheConstructsOfTheValidPrefixAndNoneAtOrAfterTheTripPosition()
	{
		RecognitionHarness harness = new();
		string source = TextNormalization.Normalize(BuildHereDocumentSourceAroundCredentials());
		FileLexicalScanner scanner = new(harness.EmissionPoint, BuildClockSpendingTheBudgetOnTheSecondAttempt());
		ZoneMask mask = scanner.Scan(harness.Analyzer.LexicalProfile, source, harness.PathOf(_TRIPPING_FILE_NAME));

		ImmutableArray<RecognizedConstruct> constructs = RecognizeOver(harness, source, mask);

		Assert.Equal(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		RecognizedConstruct kept = Assert.Single(constructs);
		Assert.Equal(0, kept.AnchorOffset);
		Assert.Equal(ConfidenceLevel.High, kept.Level);
		Assert.True(source.IndexOf(_CREDENTIAL_AFTER_THE_TRIP.TrimEnd('\n'), StringComparison.Ordinal) >= mask.ValidThroughPosition);
	}

	/// <summary>
	/// THE GUARD THAT PINS THE SCOPE: an unterminated literal still leaves its tail DEMOTED, not
	/// removed. Only a budget trip leaves the tail unanalyzed.
	/// </summary>
	[Fact]
	public void AnUnterminatedLiteralStillDemotesItsTailInsteadOfRemovingIt()
	{
		RecognitionHarness harness = new();
		string source = TextNormalization.Normalize("```never closed\n" +_CREDENTIAL_AFTER_THE_TRIP);
		ZoneMask mask = new FileLexicalScanner(harness.EmissionPoint).Scan(
			harness.Analyzer.LexicalProfile,
			source,
			harness.PathOf(_TRIPPING_FILE_NAME));

		ImmutableArray<RecognizedConstruct> constructs = RecognizeOver(harness, source, mask);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		RecognizedConstruct demoted = Assert.Single(constructs);
		Assert.True(demoted.AnchorOffset >= mask.ValidThroughPosition);
		Assert.NotEqual(ConfidenceLevel.High, demoted.Level);
	}

	private static ImmutableArray<RecognizedConstruct> RecognizeOver(RecognitionHarness harness, string source, ZoneMask mask)
	{
		return new RecognitionEngine(harness.EmissionPoint, new FrozenTimeProvider())
			.Recognize(new FileRecognitionContext
			{
				Analyzer = harness.Analyzer,
				NormalizedText = source,
				Mask = mask,
				Path = harness.PathOf(_TRIPPING_FILE_NAME)
			})
			.Constructs;
	}

	/// <summary>
	/// A file whose first line spends the budget is attributed by its extension instead, the
	/// fallback is counted in the inventory, persisted with the scan summary, and reported in both the
	/// Markdown report and the structured artifact, and the session still completes.
	/// </summary>
	[Fact]
	public void AFirstLineThatSpendsTheBudgetFallsBackToTheExtensionAndIsCountedAndReported()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverCatalog(
			ExemplarFixtureCatalog.Build(),
			new AdvancingTimeProvider(LexicalEnginePolicy.LEXICAL_BUDGET));

		harness.WriteSourceFile(_EXTENSIONLESS_FILE_NAME, _FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE + _SECOND_FILE_CONTENT_LINE);

		ImmutableArray<StageRunOutcome> outcomes = harness.RunTheWholeWorkflow();

		Assert.All(outcomes, outcome => Assert.Equal(StageRunOutcome.Completed, outcome));
		Assert.Equal(1, harness.State.ScanOutcome.FirstLineFallbackCount);
		Assert.Contains(harness.State.ScanOutcome.Rows, row => row.LanguageName == _UNRECOGNIZED_LANGUAGE_NAME);
		Assert.DoesNotContain(harness.State.CandidateRelativePaths, path => path == _EXTENSIONLESS_FILE_NAME);
		Assert.Contains(_MARKDOWN_FALLBACK_LINE_FRAGMENT, harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport));
		Assert.Contains(_JSON_FALLBACK_FIELD_FRAGMENT, harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		Assert.Equal(1, harness.ReloadState().State.ScanOutcome.FirstLineFallbackCount);
	}

	/// <summary>The control for the test above: with no trip the first line attributes the file and nothing is counted or said.</summary>
	[Fact]
	public void AFirstLineWithinTheBudgetAttributesTheFileAndReportsNoFallback()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverCatalog(ExemplarFixtureCatalog.Build(), new FrozenTimeProvider());

		harness.WriteSourceFile(_EXTENSIONLESS_FILE_NAME, _FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE + _SECOND_FILE_CONTENT_LINE);

		harness.RunTheWholeWorkflow();

		Assert.Equal(0, harness.State.ScanOutcome.FirstLineFallbackCount);
		Assert.Contains(harness.State.ScanOutcome.Rows, row => row.LanguageName == _EXEMPLAR_LANGUAGE_NAME);
		Assert.Contains(_EXTENSIONLESS_FILE_NAME, harness.State.CandidateRelativePaths);
		Assert.DoesNotContain(_MARKDOWN_FALLBACK_LINE_FRAGMENT, harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport));
		Assert.Contains(_JSON_NO_FALLBACK_FIELD_FRAGMENT, harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
	}

	/// <summary>
	/// The first-line site runs in Scan only: Analyze reuses the attribution Scan persisted, so
	/// no file can be given one language by Scan's budget and another by Analyze's. The lexical scan is
	/// the only thing Analyze charges.
	/// </summary>
	[Fact]
	public void AnalyzeNeverAttributesALanguageAndSoNeverChargesAFirstLine()
	{
		string analyzeStageSource = File.ReadAllText(Path.Combine(
			LocateRepositoryRoot(),
			_SOURCE_FOLDER_NAME,
			"MigrateAuth.Cli",
			_ANALYZE_STAGE_SOURCE_PATH));

		Assert.DoesNotContain(_LANGUAGE_ATTRIBUTION_TYPE_NAME, analyzeStageSource, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE STRUCTURAL GUARANTEE: no analyzer-declared expression is evaluated in <c>src/</c> outside
	/// one of the two budgets.
	///
	/// HOW IT IS ROBUST. It does not try to type-check receivers, which text cannot do. It inverts the
	/// question: every call to one of the regular-expression evaluation methods in <c>src/</c> must
	/// either sit inside one of the two door files, reach a door through its receiver, or be named in
	/// an explicit list of engine-own expressions. A new evaluation site therefore fails here whatever
	/// it is called on, until someone routes it through a door or says in this file why it is exempt.
	/// The companion test below ties the exempt list to the contract: it asserts the set of
	/// Regex-typed contract members is exactly the one this scan was written against.
	/// </summary>
	[Fact]
	public void NoAnalyzerDeclaredExpressionIsEvaluatedOutsideABudget()
	{
		string sourceFolder = Path.Combine(LocateRepositoryRoot(), _SOURCE_FOLDER_NAME);
		Regex evaluationCall = new(_EVALUATION_CALL_PATTERN, RegexOptions.CultureInvariant);

		string[] unbudgetedCallSites =
		[
			.. Directory
				.EnumerateFiles(sourceFolder, _SOURCE_FILE_PATTERN, SearchOption.AllDirectories)
				.Where(path => !IsBuildOutput(path))
				.Where(path => !_doorFileNames.Contains(Path.GetFileName(path), StringComparer.Ordinal))
				.SelectMany(path => FindUnbudgetedEvaluations(path, evaluationCall))
		];

		Assert.Empty(unbudgetedCallSites);
	}

	[Fact]
	public void TheStructuralScanCoversEveryRegexMemberOfTheAnalyzerContract()
	{
		Type[] contractTypes =
		[
			typeof(CapturedTerminator),
			typeof(ContentMarker),
			typeof(DetectionRule),
			typeof(FlowSignal),
			typeof(LanguageIdentification),
			typeof(LanguageShapedDemotionSignals),
			typeof(LexicalProfile),
			typeof(QuoteRule)
		];

		string[] declaredMembers =
		[
			.. contractTypes
				.SelectMany(type => type
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(property => CarriesARegularExpression(property.PropertyType))
					.Select(property => $"{type.Name}.{property.Name}"))
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal(_analyzerDeclaredExpressionMembers, declaredMembers);
	}

	private static IEnumerable<string> FindUnbudgetedEvaluations(string path, Regex evaluationCall)
	{
		string fileName = Path.GetFileName(path);

		foreach (string line in File.ReadLines(path))
		{
			if (line.TrimStart().StartsWith(_COMMENT_LINE_PREFIX, StringComparison.Ordinal))
			{
				continue;
			}

			foreach (Match call in evaluationCall.Matches(line))
			{
				string receiver = call.Groups[_RECEIVER_GROUP].Value;

				if (!ReachesADoorOrIsEngineOwn(fileName, receiver))
				{
					yield return $"{fileName}: {line.Trim()}";
				}
			}
		}
	}

	private static bool ReachesADoorOrIsEngineOwn(string fileName, string receiver)
	{
		bool reachesADoor = _doorReceivers.Contains(receiver, StringComparer.Ordinal);
		bool isEngineOwn = _engineOwnEvaluations.Contains((fileName, receiver));

		return reachesADoor || isEngineOwn;
	}

	private static bool CarriesARegularExpression(Type propertyType)
	{
		return propertyType == typeof(Regex)
			|| propertyType == typeof(Regex[])
			|| (propertyType.IsGenericType && propertyType.GetGenericArguments().Contains(typeof(Regex)));
	}

	private static bool IsBuildOutput(string path)
	{
		string[] segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		return segments.Contains(_BUILD_OUTPUT_FOLDER_SEGMENT_BIN) || segments.Contains(_BUILD_OUTPUT_FOLDER_SEGMENT_OBJ);
	}

	private static string LocateRepositoryRoot()
	{
		DirectoryInfo? candidate = new(AppContext.BaseDirectory);

		while (candidate is not null && candidate.GetFiles(_SOLUTION_FILE_PATTERN).Length == 0)
		{
			candidate = candidate.Parent;
		}

		return candidate?.FullName
			?? throw new DirectoryNotFoundException("No folder holding a solution file sits above the test assembly.");
	}

	/// <summary>
	/// A clock whose step is half the budget: the first charged attempt spends half of it and the
	/// second spends the rest, which is the smallest way to force a trip PARTWAY through a file.
	/// </summary>
	private static AdvancingTimeProvider BuildClockSpendingTheBudgetOnTheSecondAttempt()
	{
		return new AdvancingTimeProvider(LexicalEnginePolicy.LEXICAL_BUDGET / _STEPS_TO_SPEND_THE_BUDGET_ON_THE_SECOND_ATTEMPT);
	}

	/// <summary>
	/// A clock whose step is a small fraction of the budget, so that a file needs many charged attempts
	/// to spend it. A pipeline test needs this because the recognition engine reads the same clock
	/// under its own, much larger, match bound: a step big enough to trip the lexical budget in two
	/// attempts would trip that bound as well and drop the file.
	/// </summary>
	private static AdvancingTimeProvider BuildClockSpendingTheBudgetAfterManyAttempts()
	{
		return new AdvancingTimeProvider(LexicalEnginePolicy.LEXICAL_BUDGET / _ATTEMPTS_THAT_SPEND_THE_BUDGET_IN_A_PIPELINE);
	}

	private static string BuildHereDocumentSource()
	{
		return _COMMENT_LINE
			+ _HERE_DOCUMENT_OPENING_LINE
			+ _HERE_DOCUMENT_BODY_WITH_A_HOLE
			+ _HERE_DOCUMENT_CLOSING_LINE
			+ "let after = 1\n";
	}

	/// <summary>A credential on line one, the here-document that spends the budget, then a second credential after it.</summary>
	private static string BuildHereDocumentSourceAroundCredentials()
	{
		return _CREDENTIAL_BEFORE_THE_TRIP
			+ _HERE_DOCUMENT_OPENING_LINE
			+ string.Concat(Enumerable.Repeat(_HERE_DOCUMENT_BODY_WITH_A_HOLE, _HOLES_IN_THE_PIPELINE_HERE_DOCUMENT))
			+ _HERE_DOCUMENT_CLOSING_LINE
			+ _CREDENTIAL_AFTER_THE_TRIP;
	}

	/// <summary>The same source written against the construct the pipeline analyzer recognizes instead of the fixture analyzer's.</summary>
	private static string ForThePipelineAnalyzer(string exemplarSource)
	{
		return exemplarSource.Replace(_EXEMPLAR_CONSTRUCT, PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT, StringComparison.Ordinal);
	}

	private static string BuildCatastrophicText()
	{
		return new string('a', _CATASTROPHIC_REPETITION_COUNT) + "!";
	}

	private static Regex BuildCatastrophicExpression()
	{
		return new Regex(_CATASTROPHIC_PATTERN, RegexOptions.None, TimeSpan.FromMilliseconds(_ONE_MILLISECOND_TIMEOUT));
	}

	private static LexicalProfile BuildProfileWithACatastrophicHereDocumentOpening()
	{
		return BuildProfile(new QuoteRule
		{
			Opening = _HERE_DOCUMENT_OPENING,
			Closing = string.Empty,
			NewlinePolicy = NewlinePolicy.Allowed,
			CapturedTerminator = new CapturedTerminator
			{
				OpeningExpression = new Regex(
					$"<<(?<{_HERE_DOCUMENT_TERMINATOR_GROUP}>{_CATASTROPHIC_PATTERN})",
					RegexOptions.None,
					TimeSpan.FromMilliseconds(_ONE_MILLISECOND_TIMEOUT)),
				TerminatorCaptureGroupName = _HERE_DOCUMENT_TERMINATOR_GROUP
			}
		});
	}

	private static LexicalProfile BuildProfileWithACatastrophicIdentifierExtent()
	{
		return BuildProfile(new QuoteRule
		{
			Opening = _QUOTE,
			Closing = _QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			InterpolationOpening = _IDENTIFIER_SIGIL,
			InterpolationIdentifierExtent = BuildCatastrophicExpression()
		});
	}

	private static LexicalProfile BuildProfile(QuoteRule quoteRule)
	{
		return new LexicalProfile
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_CONSTANT_DECLARATION_PATTERN, RegexOptions.None, RecognitionEnginePolicy.MATCH_BOUND),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = [quoteRule]
		};
	}

	/// <summary>
	/// THE PIPELINE ANALYZER, WITH THE THREE EXPRESSIONS ADDED. It declares a real language name, which
	/// Analyze needs before it will scan a file at all (a finding must carry one of the eight), and it
	/// otherwise delegates every member to the pipeline analyzer unchanged. The lexical profile gains
	/// the fixture's here-document rule and the language identification its first-line expression, so
	/// the expressions come from the one place that declares them with the lexical budget as timeout.
	/// </summary>
	private sealed class BudgetDeclaringPipelineAnalyzer : ILanguageAnalyzer
	{
		private readonly PipelineLanguageAnalyzer _inner = new();

		public BudgetDeclaringPipelineAnalyzer()
		{
			LexicalProfile = BuildProfileWithAHereDocument(_inner.LexicalProfile);
			LanguageIdentification = BuildIdentificationWithAFirstLineExpression(_inner.LanguageIdentification);
		}

		public LexicalProfile LexicalProfile { get; }

		public LanguageIdentification LanguageIdentification { get; }

		public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass => _inner.DetectionRulesByPatternClass;

		public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

		public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation => _inner.CandidateSelectionAndRecommendation;

		public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;

		private static LexicalProfile BuildProfileWithAHereDocument(LexicalProfile inner)
		{
			return new LexicalProfile
			{
				ProfileName = inner.ProfileName,
				ConstantDeclarationExpression = inner.ConstantDeclarationExpression,
				KeywordCaseSensitivity = inner.KeywordCaseSensitivity,
				DelimiterCaseSensitivity = inner.DelimiterCaseSensitivity,
				LineCommentRules = inner.LineCommentRulesInEngineOrder,
				BlockCommentRules = inner.BlockCommentRulesInEngineOrder,
				QuoteRules =
				[
					.. inner.QuoteRulesInEngineOrder,
					new QuoteRule
					{
						Opening = _HERE_DOCUMENT_OPENING,
						Closing = string.Empty,
						NewlinePolicy = NewlinePolicy.Allowed,
						OpeningAnchoring = PositionalAnchoring.EndOfLine,
						ClosingAnchoring = PositionalAnchoring.StartOfLine,
						InterpolationOpening = _IDENTIFIER_SIGIL,
						InterpolationIdentifierExtent = ExemplarPatternExpressions.InterpolatedIdentifier(),
						CapturedTerminator = new CapturedTerminator
						{
							OpeningExpression = ExemplarPatternExpressions.HereDocumentOpening(),
							TerminatorCaptureGroupName = _HERE_DOCUMENT_TERMINATOR_GROUP
						}
					}
				]
			};
		}

		private static LanguageIdentification BuildIdentificationWithAFirstLineExpression(LanguageIdentification inner)
		{
			return new LanguageIdentification
			{
				FileExtensions = inner.FileExtensions,
				DeclaredLanguageNames = inner.DeclaredLanguageNames,
				FirstLineDetectionExpression = ExemplarPatternExpressions.FirstLineDetection(),
				LanguageNameDerivation = inner.LanguageNameDerivation,
				DeclaredUnsupportedCoverage = inner.DeclaredUnsupportedCoverage
			};
		}
	}
}
