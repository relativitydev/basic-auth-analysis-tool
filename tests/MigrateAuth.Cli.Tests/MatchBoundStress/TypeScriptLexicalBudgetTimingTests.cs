using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE LEXICAL BUDGET'S TIMING CHECK FOR `TypeScript`, on the pattern of the `Shell` lexical-budget gate in <see cref="LexicalBudgetTimingTests"/> and of <see cref="ShellLexicalBudgetMeasurements"/>.
///
/// WHAT IS MEASURED. The lexical budget bounds the analyzer-declared expressions evaluated outside the recognition engine's match bound: a here-document opening, an interpolation identifier extent and a first-line
/// detection expression. The shipped `TypeScript` profile declares none of the three (the analyzer declares four extensions and no first-line rule, and the profile composes the `JavaScript` quote rules, none of which
/// carries an extent or a captured terminator), so the charge of a file is expected to be nothing. What is timed is therefore the scan itself, over every shape of <see cref="TypeScriptShippedCostGateStressSet"/> with the shipped
/// profile: the time the budget is charged (through <see cref="ChargeRecordingTimeProvider"/>), the whole scan for context, and the backward predicate of the regular-expression opening (the `TypeScript` predicate and the
/// `JavaScript` predicate), which the budget does not charge, called at every slash, as its own row.
///
/// WHAT THIS ASSERTS, AND WHAT IT DOES NOT. That no shape trips the budget (that the profile declares no expression the budget would charge is asserted in the normal suite, by <see cref="TypeScriptShippedStressSetShapeTests"/>); that the charged figure of every shape stays under the budget in force; that
/// the rule that set the budget (ten times the worst benign charged figure, rounded up to the next 250 ms, within the budget in force) holds on the optimized build; and that the predicate's worst per-file time stays under
/// the aim. It does not assert the safety multiple, which is a statement about a slower machine.
///
/// Timing-sensitive: its name carries "Timing" and it runs alone in the <c>TimingSensitiveStressCollection</c> collection.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class TypeScriptLexicalBudgetTimingTests
{
	/// <summary>When set, names the file the timing table is also written to, so a run leaves a record outside the test log.</summary>
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT_TYPESCRIPT";

	private static readonly Lazy<ImmutableArray<TypeScriptLexicalTiming>> _scanTimings =
		new(() => TypeScriptLexicalBudgetMeasurements.MeasureEveryShape(TypeScriptShippedCostGateStressSet.Build()));

	private readonly ITestOutputHelper _output;

	public TypeScriptLexicalBudgetTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoTypeScriptShapeSpendsTheLexicalBudgetAndTheFiguresAreRecorded()
	{
		ImmutableArray<TypeScriptLexicalTiming> scans = _scanTimings.Value;
		string report = Describe(scans);

		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);

		double ceiling = TheCeiling();

		Assert.All(scans, scan => Assert.False(scan.TrippedTheBudget, scan.ShapeName));
		Assert.All(scans, scan => Assert.True(scan.ChargedMilliseconds < ceiling, $"{scan.ShapeName}: {scan.ChargedMilliseconds:F4} ms charged against {ceiling:F1} ms"));
	}

	/// <summary>
	/// THE RULE THAT SET THE BUDGET HOLDS ON THE OPTIMIZED BUILD OF EVERY SYSTEM THE TEST RUNS ON: ten times the worst benign charged figure of a file, rounded up to the next 250 ms, is within the budget in force, so the
	/// figure the rule states is the figure enforced. The unoptimized build's figures are not the product's, and the rule is reported there but not asserted.
	/// </summary>
	[Fact]
	public void TheRuleOfTheLexicalBudgetHoldsOnTheWorstBenignChargeOfAnyTypeScriptShape()
	{
		ImmutableArray<TypeScriptLexicalTiming> scans = _scanTimings.Value;
		double figure = scans.Max(scan => scan.ChargedMilliseconds);
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

	/// <summary>The backward predicate of the regular-expression opening is not charged to the budget; nothing else bounds it, so its worst benign per-file cost is its own row, held to the aim.</summary>
	[Fact]
	public void TheTypeScriptRegularExpressionOpeningPredicateWorstBenignCostIsRecordedAsItsOwnRow()
	{
		ImmutableArray<TypeScriptLexicalTiming> scans = _scanTimings.Value;
		TypeScriptLexicalTiming worst = scans.OrderByDescending(scan => scan.PredicateMilliseconds).First();
		double ceiling = JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? JavaScriptMatchBoundTimingTests.AIM_WORST_FILE_MILLISECONDS
			: TypeScriptShippedCostGateTimingTests.UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"OpeningContextPredicate (regular-expression opening, slash-predicate row), own row, not charged: worst benign per-file {0:F4} ms on {1} ({2} calls); ceiling {3:F1} ms",
			worst.PredicateMilliseconds,
			worst.ShapeName,
			worst.PredicateCalls,
			ceiling));

		Assert.All(scans, scan => Assert.True(scan.PredicateMilliseconds < ceiling, $"{scan.ShapeName}: {scan.PredicateMilliseconds:F4} ms against {ceiling:F1} ms"));
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

	private static string Describe(ImmutableArray<TypeScriptLexicalTiming> scans)
	{
		StringBuilder report = new();

		AppendTheHeader(report);
		report.AppendLine("shape\tcharacters\tcharged_ms\twhole_scan_ms\tpredicate_ms\tpredicate_calls\tabandoned\ttripped");

		foreach (TypeScriptLexicalTiming scan in scans)
		{
			report.AppendLine(string.Format(
				CultureInfo.InvariantCulture,
				"{0}\t{1}\t{2:F4}\t{3:F1}\t{4:F4}\t{5}\t{6}\t{7}",
				scan.ShapeName,
				scan.SizeInCharacters,
				scan.ChargedMilliseconds,
				scan.WholeScanMilliseconds,
				scan.PredicateMilliseconds,
				scan.PredicateCalls,
				scan.Abandoned,
				scan.TrippedTheBudget));
		}

		AppendTheSummary(report, scans);

		return report.ToString();
	}

	private static void AppendTheHeader(StringBuilder report)
	{
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Lexical budget of the shipped TypeScript profile: {0} warm-up and {1} measured iterations per shape, the worst reported; build {2}",
			TypeScriptLexicalBudgetMeasurements.WARM_UP_ITERATIONS,
			TypeScriptLexicalBudgetMeasurements.MEASURED_ITERATIONS,
			JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's"));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Environment: {0}; {1}; {2} logical processors; runtime {3}",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version));
	}

	private static void AppendTheSummary(StringBuilder report, ImmutableArray<TypeScriptLexicalTiming> scans)
	{
		TypeScriptLexicalTiming worstCharged = scans.OrderByDescending(scan => scan.ChargedMilliseconds).First();
		TypeScriptLexicalTiming worstScan = scans.OrderByDescending(scan => scan.WholeScanMilliseconds).First();
		TypeScriptLexicalTiming worstPredicate = scans.OrderByDescending(scan => scan.PredicateMilliseconds).First();
		double figure = worstCharged.ChargedMilliseconds;

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Worst charged per file {0:F4} ms on {1}; slowest whole scan {2:F1} ms on {3}; slowest predicate {4:F4} ms on {5} ({6} calls)",
			figure,
			worstCharged.ShapeName,
			worstScan.WholeScanMilliseconds,
			worstScan.ShapeName,
			worstPredicate.PredicateMilliseconds,
			worstPredicate.ShapeName,
			worstPredicate.PredicateCalls));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"TypeScript lexical-budget figure: worst benign charged per file {0:F4} ms; x{1} = {2:F4} ms; rounded up to a multiple of {3} ms = {4:F0} ms; budget in force = {5} ms",
			figure,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			figure * MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS,
			RoundedBudgetOf(figure),
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Rule of the budget (ten times the worst benign figure, rounded up to the next 250 ms, within the budget in force): {0}",
			RoundedBudgetOf(figure) <= LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS ? "holds" : "does not hold"));
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
