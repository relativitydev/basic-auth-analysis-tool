using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE LEXICAL BUDGET'S TIMING CHECK FOR `Go`, on the pattern of <see cref="PythonLexicalBudgetTimingTests"/>. The profile measured is the SHIPPED profile (the lexical profile of the registered `Go` analyzer: three quote
/// rules, two comment rules, no predicate), on an identifier-dense 2 MiB file with 10,000-character lines and on the shapes of <see cref="GoLexicalBudgetMeasurements.BuildShapes"/>. A `Go` file has no first-line rule, so
/// attribution charges the budget nothing beyond the scan.
///
/// WHAT THIS ASSERTS, AND WHAT IT DOES NOT: the pass rule of <see cref="LexicalBudgetTimingTests"/>, ABSOLUTE AGAINST THE BUDGET IN FORCE. That no shape trips the budget; that the charged figure of every shape stays under the
/// budget in force; and that the rule that set the budget (ten times the worst benign charge, rounded up to the next 250 ms, within the budget in force) holds on the optimized build. It does not assert the safety multiple,
/// which is a statement about a slower machine. THE WHOLE-SCAN FIGURE is recorded and the same rule is printed over it, never asserted: a figure that the rule rejects is the trigger of a reformulation, and
/// THE BUDGET IS NEVER RAISED BY THIS TEST.
///
/// Timing-sensitive: its name carries "Timing" and it runs alone in the <c>TimingSensitiveStressCollection</c> collection. It is written and compiled here, and run only in a separate timing run.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class GoLexicalBudgetTimingTests
{
	/// <summary>When set, names the file the timing table is also written to, so a run leaves a record outside the test log.</summary>
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT_GO";

	private static readonly Lazy<ImmutableArray<GoShippedLexicalTiming>> _scanTimings =
		new(() => GoLexicalBudgetMeasurements.MeasureEveryShape(GoLexicalBudgetMeasurements.BuildShapes()));

	private readonly ITestOutputHelper _output;

	public GoLexicalBudgetTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoGoShapeSpendsTheLexicalBudgetAndTheFiguresAreRecorded()
	{
		ImmutableArray<GoShippedLexicalTiming> scans = _scanTimings.Value;
		string report = DescribeTheRun(scans);

		_output.WriteLine(report);
		PythonTimingReportWriter.WriteWhereRequested(REPORT_PATH_VARIABLE_NAME, report);

		double ceiling = TheCeiling();

		Assert.All(scans, scan => Assert.False(scan.TrippedTheBudget, scan.ShapeName));
		Assert.All(scans, scan => Assert.True(scan.ChargedMilliseconds < ceiling, $"{scan.ShapeName}: {scan.ChargedMilliseconds:F4} ms charged against {ceiling:F1} ms"));
	}

	/// <summary>
	/// THE RULE THAT SET THE BUDGET HOLDS ON THE OPTIMIZED BUILD OF EVERY SYSTEM THE TEST RUNS ON: ten times the worst benign charge of a file, rounded up to the next 250 ms, is within the budget in force, so the figure the rule
	/// states is the figure enforced. The unoptimized build's figures are not the product's, and the rule is reported there but not asserted.
	/// </summary>
	[Fact]
	public void TheRuleOfTheLexicalBudgetHoldsOnTheWorstBenignChargeOfAnyGoShape()
	{
		double figure = TheRuleFigure(_scanTimings.Value);
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
		GoShippedLexicalTiming identifierDense = _scanTimings.Value.Single(scan => scan.ShapeName == GoLexicalBudgetMeasurements.IDENTIFIER_DENSE_SHAPE_NAME);
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

	private static double TheRuleFigure(ImmutableArray<GoShippedLexicalTiming> scans)
	{
		return scans.Max(scan => scan.ChargedMilliseconds);
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

	private static string DescribeTheRun(ImmutableArray<GoShippedLexicalTiming> scans)
	{
		double figure = TheRuleFigure(scans);
		string build = JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's";

		return GoLexicalBudgetMeasurements.Describe(scans, figure, RoundedBudgetOf(figure), build);
	}
}
