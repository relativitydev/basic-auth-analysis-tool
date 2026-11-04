using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE ENGINE-MEASURED COST GATE OF THE SHIPPED `Shell` ANALYZER, taken the way the `CSharp`, `PowerShell` and `VBNet` figures are: each stress shape runs through the registered
/// analyzer (the shipped profile, executed strings included) and the engine's own file accumulator is read back, so the figure is exactly what a real scan charges against the match bound.
/// The draft gate (<see cref="ShellMatchBoundTimingTests"/>) timed test-local expressions; this one times what ships.
///
/// Timing-sensitive: it asserts on wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class ShellShippedCostGateTimingTests
{
	internal const string ENGINE_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_SHELL_ENGINE";

	internal const string LEXICAL_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT_SHELL_SHIPPED";

	private const int _SCAN_WARM_UP_ITERATIONS = 1;

	private const int _SCAN_MEASURED_ITERATIONS = 3;

	private readonly ITestOutputHelper _output;

	public ShellShippedCostGateTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoTerminatingBenignShellShapeReachesTheMatchBoundAndTheWorstFileStaysUnderTheTargetOnTheShippedAnalyzer()
	{
		ShellRecognitionHarness harness = new();
		MatchBoundTimingTarget target = MatchBoundTimingHarness.ForShell(harness);
		bool tableRequested = MatchBoundTimingHarness.TimingTableIsRequested(ENGINE_REPORT_PATH_VARIABLE_NAME);
		ImmutableArray<MatchBoundShapeTiming> timings =
		[
			.. ShellMatchBoundStressSet.Build().Select(shape => MatchBoundTimingHarness.Measure(target, shape, tableRequested))
		];
		string report = MatchBoundTimingHarness.Describe(
			timings,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS);
		double ceiling = ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? ShellMatchBoundTimingTests.TARGET_WORST_FILE_MILLISECONDS
			: ShellMatchBoundTimingTests.UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;

		_output.WriteLine(report);
		WriteWhereRequested(ENGINE_REPORT_PATH_VARIABLE_NAME, report);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Equal(0, timing.FlowTrips));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingRules));
		Assert.All(
			timings,
			timing => Assert.True(timing.WorstFileMilliseconds < ceiling, $"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against {ceiling:F0} ms"));
	}

	[Fact]
	public void NoShellShapeSpendsTheLexicalBudgetOnTheShippedProfileAndTheFirstLineAndTheRuleOfTheBudgetHold()
	{
		ILanguageAnalyzer analyzer = ShellRegisteredExpressions.Analyzer;
		ImmutableArray<(string Name, double Charged, double Arithmetic, double WholeScan, bool Abandoned, bool Tripped)> scans =
		[
			.. ShellMatchBoundStressSet.Build().Select(shape => TimeTheScan(analyzer.LexicalProfile, shape))
		];
		Regex firstLineExpression = analyzer.LanguageIdentification.FirstLineDetectionExpression!;
		ImmutableArray<(string Name, double Milliseconds)> firstLines =
		[
			.. ShellFirstLineStressSet.Build().Select(shape => (shape.Name, ShellLexicalBudgetMeasurements.TimeTheFirstLineExpression(firstLineExpression, shape.Line)))
		];
		double worstCharged = scans.Max(scan => scan.Charged);
		double worstFirstLine = firstLines.Max(firstLine => firstLine.Milliseconds);
		double figure = Math.Max(worstCharged, worstFirstLine);
		double roundedBudget = Math.Ceiling(figure * MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE / MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS) * MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS;
		double ceiling = ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS
			: LexicalBudgetTimingTests.SHELL_UNOPTIMIZED_CEILING_MILLISECONDS;
		string report = Describe(scans, firstLines, figure, roundedBudget);

		_output.WriteLine(report);
		WriteWhereRequested(LEXICAL_REPORT_PATH_VARIABLE_NAME, report);

		Assert.All(scans, scan => Assert.True(scan.Charged < ceiling, $"{scan.Name}: {scan.Charged:F4} ms charged against {ceiling:F1} ms"));
		Assert.All(scans, scan => Assert.False(scan.Tripped, scan.Name));
		Assert.All(firstLines, firstLine => Assert.True(firstLine.Milliseconds < ceiling, $"{firstLine.Name}: {firstLine.Milliseconds:F4} ms against {ceiling:F1} ms"));

		if (ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized())
		{
			Assert.True(
				roundedBudget <= LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS,
				$"{MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE:F0} times {figure:F4} ms rounds up to {roundedBudget:F0} ms, above the budget of {LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS} ms");
		}
	}

	private static (string Name, double Charged, double Arithmetic, double WholeScan, bool Abandoned, bool Tripped) TimeTheScan(LexicalProfile profile, MatchBoundStressShape shape)
	{
		ChargeRecordingTimeProvider clock = new();
		LexicalScanner scanner = new(profile, clock);
		double charged = 0.0;
		double arithmetic = 0.0;
		double wholeScan = 0.0;
		bool abandoned = false;
		bool tripped = false;

		for (int iteration = 0; iteration < _SCAN_WARM_UP_ITERATIONS + _SCAN_MEASURED_ITERATIONS; iteration++)
		{
			clock.Reset();

			Stopwatch stopwatch = Stopwatch.StartNew();
			ZoneMask mask = scanner.Scan(shape.Text);

			stopwatch.Stop();
			abandoned |= mask.WasAbandonedMidFile;
			tripped |= mask.AnomalyReason is LexicalAnomalyReason.LexicalBudgetExceeded;

			if (iteration >= _SCAN_WARM_UP_ITERATIONS)
			{
				charged = Math.Max(charged, clock.ChargedMilliseconds);
				arithmetic = Math.Max(arithmetic, scanner.ChargeOfTheLastScan.ArithmeticReadAhead.TotalMilliseconds);
				wholeScan = Math.Max(wholeScan, stopwatch.Elapsed.TotalMilliseconds);
			}
		}

		return (shape.Name, charged, arithmetic, wholeScan, abandoned, tripped);
	}

	private static string Describe(
		ImmutableArray<(string Name, double Charged, double Arithmetic, double WholeScan, bool Abandoned, bool Tripped)> scans,
		ImmutableArray<(string Name, double Milliseconds)> firstLines,
		double figure,
		double roundedBudget)
	{
		StringBuilder report = new();
		var worst = scans.OrderByDescending(scan => scan.Charged).First();
		var worstFirstLine = firstLines.OrderByDescending(firstLine => firstLine.Milliseconds).First();

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Lexical budget of the shipped Shell profile (executed strings included): {0} warm-up and {1} measured iterations per shape, the worst reported; build {2}",
			_SCAN_WARM_UP_ITERATIONS,
			_SCAN_MEASURED_ITERATIONS,
			ShellMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug)"));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Environment: {0}; {1}; {2} logical processors; runtime {3}",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version));
		report.AppendLine("shape\tcharged_ms\tarithmetic_charged_ms\twhole_scan_ms\tabandoned\ttripped");

		foreach (var scan in scans)
		{
			report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1:F4}\t{2:F4}\t{3:F1}\t{4}\t{5}", scan.Name, scan.Charged, scan.Arithmetic, scan.WholeScan, scan.Abandoned, scan.Tripped));
		}

		foreach (var firstLine in firstLines)
		{
			report.AppendLine(string.Format(CultureInfo.InvariantCulture, "first-line\t{0}\t{1:F4}", firstLine.Name, firstLine.Milliseconds));
		}

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Worst charged per file {0:F4} ms on {1}; worst first line {2:F4} ms on {3}; figure {4:F4} ms; the rule gives {5:F0} ms; budget in force {6} ms",
			worst.Charged,
			worst.Name,
			worstFirstLine.Milliseconds,
			worstFirstLine.Name,
			figure,
			roundedBudget,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));

		return report.ToString();
	}

	private static void WriteWhereRequested(string variableName, string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(variableName);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}
}
