using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE LEXICAL BUDGET'S TIMING CHECK FOR `Python`, on the pattern of <see cref="LexicalBudgetTimingTests"/> and <see cref="TypeScriptLexicalBudgetTimingTests"/>. The profile measured is the SHIPPED profile
/// (the lexical profile of the registered `Python` analyzer, 84 quote rules), on an identifier-dense 2 MiB file with 10,000-character lines and on the shapes of <see cref="PythonLexicalBudgetMeasurements.BuildShapes"/>; the shipped
/// first-line expression, which language attribution charges to the budget, is timed on the first lines of <see cref="PythonLexicalBudgetMeasurements.BuildFirstLineShapes"/>, which hold the shapes of <see cref="PythonFirstLineStressSet"/>.
///
/// WHAT THIS ASSERTS, AND WHAT IT DOES NOT: the pass rule of <see cref="LexicalBudgetTimingTests"/> and of the `TypeScript` gate. That no shape trips the budget; that the charged figure of every shape, and the time of every first line,
/// stays under the budget in force; that every first line is read as the catalog says (the accepted forms match, the others do not); and that the rule that set the budget (ten times the worst benign charge, rounded up to the next 250 ms,
/// within the budget in force) holds on the optimized build. The charge is the time the budget is charged for a file: the scan's charge plus the first-line expression's time (a file is charged both, conservatively). It does not assert the
/// safety multiple, which is a statement about a slower machine. THE WHOLE-SCAN FIGURE is the cost the lexical-budget decision names (84 rules and about 140 index candidates on the hottest buckets); the budget does not charge it, so it is recorded and the same rule is
/// printed over it, never asserted: a figure that the rule rejects is the trigger of a reformulation, and the budget is not raised for it.
///
/// Timing-sensitive: its name carries "Timing" and it runs alone in the <c>TimingSensitiveStressCollection</c> collection. It is written and compiled here, and run only in a separate timing run.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class PythonLexicalBudgetTimingTests
{
	/// <summary>When set, names the file the timing table is also written to, so a run leaves a record outside the test log.</summary>
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT_PYTHON";

	private static readonly Lazy<ImmutableArray<PythonLexicalTiming>> _scanTimings =
		new(() => PythonLexicalBudgetMeasurements.MeasureEveryShape(PythonLexicalBudgetMeasurements.BuildShapes()));

	private static readonly Lazy<ImmutableArray<PythonFirstLineTiming>> _firstLineTimings =
		new(() => PythonLexicalBudgetMeasurements.MeasureEveryFirstLine(PythonLexicalBudgetMeasurements.BuildFirstLineShapes()));

	private readonly ITestOutputHelper _output;

	public PythonLexicalBudgetTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoPythonShapeOrFirstLineSpendsTheLexicalBudgetAndTheFiguresAreRecorded()
	{
		ImmutableArray<PythonLexicalTiming> scans = _scanTimings.Value;
		ImmutableArray<PythonFirstLineTiming> firstLines = _firstLineTimings.Value;
		string report = DescribeTheRun(scans, firstLines);

		_output.WriteLine(report);
		PythonTimingReportWriter.WriteWhereRequested(REPORT_PATH_VARIABLE_NAME, report);

		double ceiling = TheCeiling();

		Assert.All(firstLines, firstLine => Assert.Equal(firstLine.Accepted, firstLine.Matched));
		Assert.All(firstLines, firstLine => Assert.True(firstLine.Milliseconds < ceiling, $"{firstLine.Name}: {firstLine.Milliseconds:F4} ms against {ceiling:F1} ms"));
		Assert.All(scans, scan => Assert.False(scan.TrippedTheBudget, scan.ShapeName));
		Assert.All(scans, scan => Assert.True(scan.ChargedMilliseconds < ceiling, $"{scan.ShapeName}: {scan.ChargedMilliseconds:F4} ms charged against {ceiling:F1} ms"));
	}

	/// <summary>
	/// THE RULE THAT SET THE BUDGET HOLDS ON THE OPTIMIZED BUILD OF EVERY SYSTEM THE TEST RUNS ON: ten times the worst benign charge of a file, rounded up to the next 250 ms, is within the budget in force, so the
	/// figure the rule states is the figure enforced. The unoptimized build's figures are not the product's, and the rule is reported there but not asserted.
	/// </summary>
	[Fact]
	public void TheRuleOfTheLexicalBudgetHoldsOnTheWorstBenignChargeOfAnyPythonShape()
	{
		double figure = TheRuleFigure(_scanTimings.Value, _firstLineTimings.Value);
		double roundedBudget = RoundedBudgetOf(figure);

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Worst benign charged per file {0:F4} ms; the rule gives {1:F0} ms; budget in force {2} ms",
			figure,
			roundedBudget,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));

		if (JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized())
		{
			Assert.True(
				roundedBudget <= LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS,
				$"{MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE:F0} times {figure:F4} ms rounds up to {roundedBudget:F0} ms, above the budget of {LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS} ms");
		}
	}

	/// <summary>The whole-scan cost, recorded and never asserted: the whole scan of the identifier-dense 2 MiB file, with the same rule printed over it. A figure the rule rejects is the trigger of a reformulation.</summary>
	[Fact]
	public void TheWholeScanOfTheIdentifierDenseFileIsRecordedWithTheRulePrintedOverIt()
	{
		PythonLexicalTiming identifierDense = _scanTimings.Value.Single(scan => scan.ShapeName == PythonLexicalBudgetMeasurements.IDENTIFIER_DENSE_SHAPE_NAME);
		double roundedBudget = RoundedBudgetOf(identifierDense.WholeScanMilliseconds);

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Whole-scan cost, recorded, not asserted: whole scan of the identifier-dense 2 MiB file {0:F1} ms ({1} characters); the rule over it gives {2:F0} ms against the budget of {3} ms: {4}",
			identifierDense.WholeScanMilliseconds,
			identifierDense.SizeInCharacters,
			roundedBudget,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS,
			roundedBudget <= LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS ? "holds" : "does not hold (the trigger)"));

		Assert.True(identifierDense.SizeInCharacters > 0);
	}

	private static double TheRuleFigure(ImmutableArray<PythonLexicalTiming> scans, ImmutableArray<PythonFirstLineTiming> firstLines)
	{
		return scans.Max(scan => scan.ChargedMilliseconds) + firstLines.Max(firstLine => firstLine.Milliseconds);
	}

	private static double RoundedBudgetOf(double figure)
	{
		return Math.Ceiling(figure * MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE / MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS) * MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS;
	}

	private static double TheCeiling()
	{
		return JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS
			: LexicalBudgetTimingTests.SHELL_UNOPTIMIZED_CEILING_MILLISECONDS;
	}

	private static string DescribeTheRun(ImmutableArray<PythonLexicalTiming> scans, ImmutableArray<PythonFirstLineTiming> firstLines)
	{
		double figure = TheRuleFigure(scans, firstLines);
		string build = JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's";

		return PythonLexicalBudgetMeasurements.Describe(scans, firstLines, figure, RoundedBudgetOf(figure), build);
	}
}
