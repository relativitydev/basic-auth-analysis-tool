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
