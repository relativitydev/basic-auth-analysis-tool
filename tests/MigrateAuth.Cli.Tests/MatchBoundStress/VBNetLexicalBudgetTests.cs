using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE LEXICAL BUDGET AND THE `VBNet` CATALOG. The lexical budget bounds the three analyzer-declared expressions evaluated outside the recognition engine's match bound: a
/// here-document opening and an interpolation identifier extent (charged in the lexical scan) and a first-line detection expression (charged in language attribution).
/// The `VBNet` catalog declares none of the three: its files are attributed by extension alone, and its quote rules carry neither a captured terminator nor an interpolation
/// extent. So no `VBNet` expression is charged to the lexical budget and none can spend it. This test asserts exactly that, so that a later revision declaring one fails here and brings
/// its own shapes into a timing check; and it records, as context only, the worst whole lexical scan of the `VBNet` stress files.
/// </summary>
public sealed class VBNetLexicalBudgetTests
{
	/// <summary>When set, names the file the figures are also written to, so a run leaves a record outside the test log.</summary>
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT_VBNET";

	internal const int SCAN_MEASURED_ITERATIONS = 3;

	private const string _LANGUAGE_NAME = "VBNet";

	private readonly ITestOutputHelper _output;

	public VBNetLexicalBudgetTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheVBNetCatalogDeclaresNoneOfTheThreeExpressionsTheLexicalBudgetCoversAndTheScanOfTheStressFilesIsRecorded()
	{
		ILanguageAnalyzer analyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME];

		Assert.All(analyzer.LexicalProfile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.InterpolationIdentifierExtent));
		Assert.All(analyzer.LexicalProfile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.CapturedTerminator));
		Assert.Null(analyzer.LanguageIdentification.FirstLineDetectionExpression);

		(string worstName, double worstMilliseconds) = TimeTheLexicalScanOfTheStressFiles();
		string report = string.Format(
			CultureInfo.InvariantCulture,
			"Lexical budget, VBNet: expressions declared among the three the budget covers: 0 (budget {0:F0} ms spent by none). Whole lexical scan of the stress files, context only: worst {1:F1} ms on {2}.",
			LexicalEnginePolicy.LEXICAL_BUDGET.TotalMilliseconds,
			worstMilliseconds,
			worstName);

		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);
	}

	private static (string Name, double Milliseconds) TimeTheLexicalScanOfTheStressFiles()
	{
		VBNetRecognitionHarness harness = new();
		string worstName = string.Empty;
		double worst = 0;

		foreach (MatchBoundStressShape shape in VBNetMatchBoundStressSet.Build())
		{
			string path = VBNetMatchBoundStressSet.RelativePathOf(shape);

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

	private static void WriteTheReportWhereRequested(string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}
}
