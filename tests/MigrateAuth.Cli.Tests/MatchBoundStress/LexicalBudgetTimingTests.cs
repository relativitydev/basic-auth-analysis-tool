using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE LEXICAL BUDGET'S TIMING CHECK, run as part of the suite so the property it guarantees is re-checked on every run and the table it produces can be
/// regenerated on demand.
///
/// WHAT IS MEASURED. The lexical budget bounds the three analyzer-declared expressions evaluated outside the recognition engine's match bound: a
/// here-document opening and an interpolation identifier extent (charged in the lexical scan) and a first-line detection expression (charged in
/// language attribution). The `PowerShell` catalog declares one of the three, the first-line detection, so what is timed is the whole of one file's
/// first-line expression's own evaluation, which is the quantity the per-file budget is charged for - on every first line of
/// <see cref="PowerShellFirstLineStressSet"/>, and beside it the whole of the file's attribution, the engine's own shebang table included, as context. The lexical scan of the match-bound stress files is timed beside it
/// as context: nothing in it is charged to the budget for this language, and the test asserts that, so that a later catalog declaring a second expression
/// fails here and brings its own shapes into this check.
///
/// WHAT THIS ASSERTS, AND WHAT IT DOES NOT. That the language declares exactly the one expression and declares the budget as its timeout; that no first
/// line trips the budget or an expression's own timeout; that a shebang naming the interpreter is attributed to the language and no other shape is; and that
/// the worst per-file time stays under the budget in force. It does not assert the safety multiple, which is a statement about a slower machine.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class LexicalBudgetTimingTests
{
	/// <summary>When set, names the file the timing table is also written to, so a run leaves a record outside the test log.</summary>
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT";

	internal const int WARM_UP_ITERATIONS = 3;
	internal const int MEASURED_ITERATIONS = 200;
	internal const int SCAN_MEASURED_ITERATIONS = 3;

	private const string _LANGUAGE_NAME = "PowerShell";
	private const string _VALID_SHEBANG_SHAPE_PREFIX = "valid-shebang";
	private const string _REPOSITORY_ROOT = "lexical-budget-timing-repository";
	private const string _FIRST_LINE_PATH_FORMAT = "stress/first-line-{0}";

	private readonly ITestOutputHelper _output;

	/// <summary>What the timing check measured for one first line.</summary>
	private sealed record FirstLineTiming(
		string Name,
		double WorstExpressionMilliseconds,
		double WorstAttributionMilliseconds,
		bool Tripped,
		string Language);

	public LexicalBudgetTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void ThePowerShellCatalogDeclaresOnlyTheFirstLineExpressionAmongTheThreeTheBudgetCovers()
	{
		ILanguageAnalyzer analyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME];

		Assert.All(analyzer.LexicalProfile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.InterpolationIdentifierExtent));
		Assert.All(analyzer.LexicalProfile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.CapturedTerminator));
		Assert.NotNull(analyzer.LanguageIdentification.FirstLineDetectionExpression);
		Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, analyzer.LanguageIdentification.FirstLineDetectionExpression!.MatchTimeout);
	}

	[Fact]
	public void NoFirstLineAtTheLineCeilingTripsTheBudgetAndTheFiguresAreRecorded()
	{
		ILanguageAnalyzer analyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME];
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(Path.Combine(Path.GetTempPath(), _REPOSITORY_ROOT));
		ImmutableArray<FirstLineStressShape> shapes = PowerShellFirstLineStressSet.Build();
		List<FirstLineTiming> timings = [];

		foreach (FirstLineStressShape shape in shapes)
		{
			Assert.Equal(PowerShellFirstLineStressSet.LINE_LENGTH_IN_CHARACTERS, shape.Line.Length);
			Assert.True(RepositoryRelativePath.TryFromScannedPath(
				repository,
				string.Format(CultureInfo.InvariantCulture, _FIRST_LINE_PATH_FORMAT, shape.Name),
				out RepositoryRelativePath path));
			timings.Add(TimeTheAttribution(attribution, analyzer.LanguageIdentification.FirstLineDetectionExpression!, path, shape));
		}

		(string ScanShape, double ScanMilliseconds) worstScan = TimeTheLexicalScanOfTheStressFiles();
		string report = Describe(timings, worstScan);

		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);

		Assert.All(timings, timing => Assert.False(timing.Tripped, timing.Name));
		Assert.All(
			timings,
			timing => Assert.Equal(
				timing.Name.StartsWith(_VALID_SHEBANG_SHAPE_PREFIX, StringComparison.Ordinal),
				timing.Language == _LANGUAGE_NAME));
		Assert.All(timings, timing => Assert.True(timing.WorstAttributionMilliseconds < LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS, timing.Name));
		Assert.All(timings, timing => Assert.True(timing.WorstExpressionMilliseconds < LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS, timing.Name));
	}

	/// <summary>
	/// The Shell ceiling the gate asserts for the openings, the arithmetic read-aheads and the first line: the budget in force, which no benign file may come near spending. On a build with the JIT optimizer disabled the generated expressions
	/// run several times slower than in the product, so the ceiling there is ten times the budget, which still catches an expression that has lost its bound. What the rule that set
	/// the budget asks (ten times the worst benign figure, rounded up to the next 250 ms, must not exceed the budget) is asserted on the optimized build by
	/// <see cref="TheRuleOfTheLexicalBudgetHoldsOnTheWorstBenignChargeIncludingTheArithmeticReadAhead"/>.
	/// </summary>
	internal const double SHELL_UNOPTIMIZED_CEILING_MILLISECONDS = MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE * LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS;

	internal const string SHELL_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT_SHELL";

	private const string _CONTIGUOUS_MATCH_ANCHOR = "\\G";

	private const int _SHELL_LEXICAL_EXPRESSION_COUNT = 3;

	private static readonly Lazy<ImmutableArray<ShellLexicalTiming>> _shellScanTimings =
		new(() => ShellLexicalBudgetMeasurements.MeasureEveryShape(ShellMatchBoundStressSet.Build()));

	[Fact]
	public void TheShellDraftDeclaresTheThreeExpressionsTheLexicalBudgetCoversWithTheBudgetAsTheirTimeoutAndTheOpeningsAnchoredWithTheContiguousMatchAnchor()
	{
		Assert.Equal(_SHELL_LEXICAL_EXPRESSION_COUNT, ShellDraftExpressions.LexicalExpressions.Length);
		Assert.All(ShellDraftExpressions.LexicalExpressions, entry => Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, entry.Expression.MatchTimeout));
		Assert.StartsWith(_CONTIGUOUS_MATCH_ANCHOR, ShellDraftExpressions.EX_SH_01, StringComparison.Ordinal);
		Assert.StartsWith(_CONTIGUOUS_MATCH_ANCHOR, ShellDraftExpressions.EX_SH_02, StringComparison.Ordinal);
	}

	[Fact]
	public void NoShellHereDocumentOpeningOrFirstLineSpendsTheLexicalBudgetAndTheFiguresAreRecorded()
	{
		ImmutableArray<ShellLexicalTiming> scans = _shellScanTimings.Value;
		ImmutableArray<(string Name, double Milliseconds, bool Matched)> firstLines =
		[
			.. ShellFirstLineStressSet.Build().Select(shape => (
				shape.Name,
				ShellLexicalBudgetMeasurements.TimeTheFirstLineExpression(ShellDraftExpressions.Ex04(), shape.Line),
				ShellDraftExpressions.Ex04().IsMatch(shape.Line)))
		];
		string report = DescribeShell(scans, firstLines);

		_output.WriteLine(report);
		WriteTheShellReportWhereRequested(report);

		double ceiling = TheShellCeiling();

		Assert.All(firstLines, firstLine => Assert.Equal(firstLine.Name.StartsWith(ShellFirstLineStressSet.VALID_SHAPE_NAME_PREFIX, StringComparison.Ordinal), firstLine.Matched));
		Assert.All(firstLines, firstLine => Assert.True(firstLine.Milliseconds < ceiling, $"{firstLine.Name}: {firstLine.Milliseconds:F4} ms against {ceiling:F1} ms"));
		Assert.All(scans, scan => Assert.True(scan.ChargedMilliseconds < ceiling, $"{scan.ShapeName}: {scan.ChargedMilliseconds:F4} ms charged against {ceiling:F1} ms"));
		Assert.All(scans, scan => Assert.False(scan.TrippedTheBudget, scan.ShapeName));
	}

	/// <summary>
	/// THE RULE THAT SET THE BUDGET HOLDS ON THE OPTIMIZED BUILD OF EVERY SYSTEM THE TEST RUNS ON: ten times the worst benign charged figure of a file (the openings, the arithmetic read-aheads and the first-line expression),
	/// rounded up to the next 250 ms, is within the budget in force, so the figure the rule states is the figure enforced. The worst figure is the whole charge a file can be put through, measured with the
	/// profile that reads executed strings as well as with the one that does not. The unoptimized build's figures are not the product's, and the rule is reported there but not asserted.
	/// </summary>
	[Fact]
	public void TheRuleOfTheLexicalBudgetHoldsOnTheWorstBenignChargeIncludingTheArithmeticReadAhead()
	{
		ImmutableArray<ShellLexicalTiming> scans = _shellScanTimings.Value;
		double worstFirstLine = ShellFirstLineStressSet.Build().Max(shape => ShellLexicalBudgetMeasurements.TimeTheFirstLineExpression(ShellDraftExpressions.Ex04(), shape.Line));
		double figure = Math.Max(scans.Max(scan => scan.ChargedMilliseconds), worstFirstLine);
		double roundedBudget = RoundedBudgetOf(figure);

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Worst benign charged per file (openings, arithmetic read-aheads, first line) {0:F4} ms; the rule gives {1:F0} ms; budget in force {2} ms",
			figure,
			roundedBudget,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));

		if (ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized())
		{
			Assert.True(
				roundedBudget <= LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS,
				$"Ten times {figure:F4} ms rounds up to {roundedBudget:F0} ms, above the budget of {LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS} ms");
		}
	}

	private static double RoundedBudgetOf(double figure)
	{
		return Math.Ceiling(figure * MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE / MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS) * MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS;
	}

	/// <summary>
	/// The arithmetic-context opening is charged to the lexical budget: its evaluation is the read-ahead that answers whether a double parenthesis is closed as arithmetic, made once for every run it passes and never once per opening.
	/// Its own row is the worst share of the charge it takes in a file, and with the openings it is held to the same ceiling as the charge as a whole. The here-document opening's own context predicate, which the budget does not charge, is held to the per-file design target.
	/// </summary>
	[Fact]
	public void TheShellArithmeticContextOpeningIsChargedToTheBudgetAndItsWorstBenignCostIsRecordedAsItsOwnRow()
	{
		ImmutableArray<ShellLexicalTiming> scans = _shellScanTimings.Value;
		ShellLexicalTiming worst = scans.OrderByDescending(scan => scan.ArithmeticChargedMilliseconds).First();
		ShellLexicalTiming worstPredicate = scans.OrderByDescending(scan => scan.PredicateMilliseconds).First();
		double predicateCeiling = ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? ShellMatchBoundTimingTests.TARGET_WORST_FILE_MILLISECONDS
			: ShellMatchBoundTimingTests.UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Arithmetic-context opening (read-ahead), charged to the budget, own row: worst benign per-file {0:F4} ms on {1}; budget {2} ms",
			worst.ArithmeticChargedMilliseconds,
			worst.ShapeName,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));
		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"OpeningContextPredicate (here-document opening), own row, not charged: worst benign per-file {0:F4} ms on {1} ({2} calls); ceiling {3:F1} ms",
			worstPredicate.PredicateMilliseconds,
			worstPredicate.ShapeName,
			worstPredicate.PredicateCalls,
			predicateCeiling));

		Assert.All(scans, scan => Assert.True(scan.ArithmeticChargedMilliseconds <= scan.ChargedMilliseconds, scan.ShapeName));
		Assert.All(scans, scan => Assert.True(scan.PredicateMilliseconds < predicateCeiling, $"{scan.ShapeName}: {scan.PredicateMilliseconds:F4} ms against {predicateCeiling:F1} ms"));
	}

	[Fact]
	public void TheShellLineCommentOpeningPredicateWorstBenignCostIsRecordedAsItsOwnRow()
	{
		ImmutableArray<ShellLexicalTiming> scans = _shellScanTimings.Value;
		ShellLexicalTiming worst = scans.OrderByDescending(scan => scan.CommentPredicateMilliseconds).First();
		double ceiling = ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? ShellMatchBoundTimingTests.TARGET_WORST_FILE_MILLISECONDS
			: ShellMatchBoundTimingTests.UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"OpeningContextPredicate (line comment, PR-SH-01), own row: worst benign per-file {0:F4} ms on {1} ({2} calls); ceiling {3:F1} ms",
			worst.CommentPredicateMilliseconds,
			worst.ShapeName,
			worst.CommentPredicateCalls,
			ceiling));

		Assert.All(scans, scan => Assert.True(scan.CommentPredicateMilliseconds < ceiling, $"{scan.ShapeName}: {scan.CommentPredicateMilliseconds:F4} ms against {ceiling:F1} ms"));
	}

	private static double TheShellCeiling()
	{
		return ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS
			: SHELL_UNOPTIMIZED_CEILING_MILLISECONDS;
	}

	private static string DescribeShell(
		ImmutableArray<ShellLexicalTiming> scans,
		ImmutableArray<(string Name, double Milliseconds, bool Matched)> firstLines)
	{
		StringBuilder report = new();

		AppendTheShellHeaderLines(report);
		AppendTheShellShapeRows(report, scans);
		AppendTheShellFirstLineRows(report, firstLines);
		AppendTheShellFigureAndRuleLines(report, scans, firstLines);

		return report.ToString();
	}

	private static void AppendTheShellHeaderLines(StringBuilder report)
	{
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Lexical budget of the Shell draft: {0} warm-up and {1} measured iterations per shape, the worst reported; build {2}",
			ShellLexicalBudgetMeasurements.WARM_UP_ITERATIONS,
			ShellLexicalBudgetMeasurements.MEASURED_ITERATIONS,
			ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's"));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Environment: {0}; {1}; {2} logical processors; runtime {3}",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version));
		report.AppendLine("shape\tcharacters\tcharged_ms\tarithmetic_charged_ms\twhole_scan_ms\tpredicate_ms\tpredicate_calls\tcomment_predicate_ms\tcomment_predicate_calls\tabandoned\ttripped");
	}

	private static void AppendTheShellShapeRows(
		StringBuilder report,
		ImmutableArray<ShellLexicalTiming> scans)
	{
		foreach (ShellLexicalTiming scan in scans)
		{
			report.AppendLine(string.Format(
				CultureInfo.InvariantCulture,
				"{0}\t{1}\t{2:F4}\t{3:F4}\t{4:F1}\t{5:F4}\t{6}\t{7:F4}\t{8}\t{9}\t{10}",
				scan.ShapeName,
				scan.SizeInCharacters,
				scan.ChargedMilliseconds,
				scan.ArithmeticChargedMilliseconds,
				scan.WholeScanMilliseconds,
				scan.PredicateMilliseconds,
				scan.PredicateCalls,
				scan.CommentPredicateMilliseconds,
				scan.CommentPredicateCalls,
				scan.Abandoned,
				scan.TrippedTheBudget));
		}
	}

	private static void AppendTheShellFirstLineRows(
		StringBuilder report,
		ImmutableArray<(string Name, double Milliseconds, bool Matched)> firstLines)
	{
		foreach ((string name, double milliseconds, bool matched) in firstLines)
		{
			report.AppendLine(string.Format(CultureInfo.InvariantCulture, "first line {0}: {1} characters, worst expression {2:F4} ms, matched {3}", name, ShellFirstLineStressSet.LINE_LENGTH_IN_CHARACTERS, milliseconds, matched));
		}
	}

	private static void AppendTheShellFigureAndRuleLines(
		StringBuilder report,
		ImmutableArray<ShellLexicalTiming> scans,
		ImmutableArray<(string Name, double Milliseconds, bool Matched)> firstLines)
	{
		ShellLexicalTiming worstCharged = scans.OrderByDescending(scan => scan.ChargedMilliseconds).First();
		ShellLexicalTiming worstPredicate = scans.OrderByDescending(scan => scan.PredicateMilliseconds).First();
		ShellLexicalTiming worstArithmetic = scans.OrderByDescending(scan => scan.ArithmeticChargedMilliseconds).First();
		ShellLexicalTiming worstCommentPredicate = scans.OrderByDescending(scan => scan.CommentPredicateMilliseconds).First();
		(string Name, double Milliseconds, bool Matched) worstFirstLine = firstLines.OrderByDescending(firstLine => firstLine.Milliseconds).First();
		double figure = Math.Max(worstCharged.ChargedMilliseconds, worstFirstLine.Milliseconds);
		double roundedBudget = RoundedBudgetOf(figure);

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Worst benign charged per file, openings and arithmetic read-aheads (EX-SH-01, EX-SH-02, the arithmetic context): {0:F4} ms on {1}; first-line expression (EX-SH-04): {2:F4} ms on {3}",
			worstCharged.ChargedMilliseconds,
			worstCharged.ShapeName,
			worstFirstLine.Milliseconds,
			worstFirstLine.Name));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Shell lexical-budget figure: worst benign per-file {0:F4} ms; x{1} = {2:F4} ms; rounded up to a multiple of {3} ms = {4:F0} ms; budget in force = {5} ms",
			figure,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			figure * MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS,
			roundedBudget,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Rule of the budget (ten times the worst benign figure, rounded up to the next 250 ms, within the budget in force): {0}",
			roundedBudget <= LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS ? "holds" : "does not hold"));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Arithmetic-context opening (read-ahead), own row, charged to the budget: worst benign per-file {0:F4} ms on {1}; OpeningContextPredicate (here-document opening), own row, not charged: {2:F4} ms on {3} ({4} calls)",
			worstArithmetic.ArithmeticChargedMilliseconds,
			worstArithmetic.ShapeName,
			worstPredicate.PredicateMilliseconds,
			worstPredicate.ShapeName,
			worstPredicate.PredicateCalls));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"OpeningContextPredicate (line comment, PR-SH-01), own row, not charged to the budget: worst benign per-file {0:F4} ms on {1} ({2} calls)",
			worstCommentPredicate.CommentPredicateMilliseconds,
			worstCommentPredicate.ShapeName,
			worstCommentPredicate.CommentPredicateCalls));
	}

	private static void WriteTheShellReportWhereRequested(string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(SHELL_REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}

	private static FirstLineTiming TimeTheAttribution(
		LanguageAttribution attribution,
		Regex firstLineExpression,
		RepositoryRelativePath path,
		FirstLineStressShape shape)
	{
		double worstExpression = 0;
		double worstAttribution = 0;
		AttributedLanguage result = default;

		for (int iteration = 0; iteration < WARM_UP_ITERATIONS + MEASURED_ITERATIONS; iteration++)
		{
			Stopwatch expressionStopwatch = Stopwatch.StartNew();

			firstLineExpression.IsMatch(shape.Line);
			expressionStopwatch.Stop();

			Stopwatch attributionStopwatch = Stopwatch.StartNew();

			result = attribution.Attribute(path, shape.Line);
			attributionStopwatch.Stop();

			bool measured = iteration >= WARM_UP_ITERATIONS;

			worstExpression = measured ? Math.Max(worstExpression, expressionStopwatch.Elapsed.TotalMilliseconds) : worstExpression;
			worstAttribution = measured ? Math.Max(worstAttribution, attributionStopwatch.Elapsed.TotalMilliseconds) : worstAttribution;
		}

		return new FirstLineTiming(shape.Name, worstExpression, worstAttribution, result.FirstLineBudgetExceeded, result.LanguageName);
	}

	/// <summary>The worst whole-scan time of the match-bound stress files, context only: no expression in this language is charged to the budget.</summary>
	private static (string Name, double Milliseconds) TimeTheLexicalScanOfTheStressFiles()
	{
		PowerShellRecognitionHarness harness = new();
		string worstName = string.Empty;
		double worst = 0;

		foreach (MatchBoundStressShape shape in PowerShellMatchBoundStressSet.Build())
		{
			string path = PowerShellMatchBoundStressSet.RelativePathOf(shape);

			harness.ContextFor(shape.Text, path);

			for (int iteration = 0; iteration < SCAN_MEASURED_ITERATIONS; iteration++)
			{
				Stopwatch stopwatch = Stopwatch.StartNew();

				harness.ContextFor(shape.Text, path);
				stopwatch.Stop();

				if (stopwatch.Elapsed.TotalMilliseconds > worst)
				{
					worst = stopwatch.Elapsed.TotalMilliseconds;
					worstName = shape.Name;
				}
			}
		}

		return (worstName, worst);
	}

	private static string Describe(
		List<FirstLineTiming> timings,
		(string ScanShape, double ScanMilliseconds) worstScan)
	{
		StringBuilder report = new();
		double worstBenign = timings.Max(timing => timing.WorstExpressionMilliseconds);
		double derived = Math.Ceiling(worstBenign * MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE / MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS)
			* MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS;

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Lexical-budget timing: {0} warm-up and {1} measured evaluations per first line; worst measured evaluation reported",
			WARM_UP_ITERATIONS,
			MEASURED_ITERATIONS));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Environment: {0}; {1}; {2} logical processors; runtime {3}",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version));

		foreach (FirstLineTiming timing in timings)
		{
			report.AppendLine(string.Format(
				CultureInfo.InvariantCulture,
				"first line {0}: {1} characters, worst per-file first-line expression {2:F4} ms, whole attribution (context) {3:F4} ms, budget tripped {4}, attributed to {5}",
				timing.Name,
				PowerShellFirstLineStressSet.LINE_LENGTH_IN_CHARACTERS,
				timing.WorstExpressionMilliseconds,
				timing.WorstAttributionMilliseconds,
				timing.Tripped,
				timing.Language));
		}

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Context, not charged to the budget (no lexical-scan expression is declared): worst lexical scan of a match-bound stress file {0:F1} ms on {1}",
			worstScan.ScanMilliseconds,
			worstScan.ScanShape));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Worst benign per-file lexical time (the first-line expression's own evaluation) over the first-line shapes: {0:F4} ms; x{1} = {2:F4} ms; rounded up to a multiple of {3} ms = {4:F0} ms; budget in force = {5} ms",
			worstBenign,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			worstBenign * MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS,
			derived,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));

		return report.ToString();
	}

	private static void WriteTheReportWhereRequested(string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}
}
