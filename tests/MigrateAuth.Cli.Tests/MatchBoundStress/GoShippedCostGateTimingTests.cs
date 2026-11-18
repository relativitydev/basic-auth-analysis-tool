using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE ENGINE-MEASURED COST GATE OF THE SHIPPED `Go` ANALYZER, on the pattern of <see cref="JavaScriptShippedCostGateTimingTests"/> and <see cref="TypeScriptShippedCostGateTimingTests"/>: each stress shape of
/// <see cref="GoShippedCostGateStressSet"/> runs through the registered analyzer and the engine's own file accumulator is read back, so the figure is exactly what a real scan charges against the match bound. The shapes
/// include the argument window of every class that reads one, at its bound, past it and never closed; the worst per-file shapes at the reader's ceiling; the long-line files; the definition windows; and the same shapes
/// under a vendored folder and a test-data folder, because a vendored tree is walked and read like any other.
///
/// THE 975 ms FIGURE IS AN AIM, NOT A GATE (the aim of every language since `PowerShell` has been missed and is printed beside the worst shape). What is asserted is what must hold whatever the aim: no expression
/// times out, no enumeration stalls, no flow evaluation trips, and every shape's worst per-file figure stays under the MATCH BOUND in force.
///
/// FOUR PARTS BY SHAPE FAMILY, one test each, with the same assertions; together they time exactly the shapes of <see cref="GoShippedCostGateStressSet.Build"/> (a non-timing test pins that). Each part writes its own report,
/// where the report variable points, with the part's suffix before the extension. The fourth part times the eleven flow-signal expressions: each of its shapes opens with one High construct, so the engine evaluates the flow signals inside the same accumulator. A `Go` file has no first-line rule and no predicate, so the lexical budget has a test of its own over every shape on the shipped profile.
///
/// THESE TESTS ARE WRITTEN AND COMPILED, AND ARE RUN ONLY IN A SEPARATE TIMING RUN, on Windows and on Linux, with no other load on the machine.
///
/// Timing-sensitive: it asserts on wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class GoShippedCostGateTimingTests
{
	internal const string ENGINE_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_GO_ENGINE";

	internal const string LEXICAL_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT_GO_SHIPPED";

	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER_GO_ENGINE";

	/// <summary>When set to a comma-separated list of shape-name fragments, only the shapes whose name holds one of them are timed: a reshape probe, never the gate (the gate runs with it unset).</summary>
	internal const string SHAPE_FILTER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_SHAPES_GO_ENGINE";

	/// <summary>On a build with the JIT optimizer disabled the generated expressions run several times slower than in the product: ten times the bound would still catch an expression that lost its bound.</summary>
	internal const double UNOPTIMIZED_BUILD_CEILING_MILLISECONDS = MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE * RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS;

	/// <summary>The suffix put before the extension of the report path of the part that times the typical and the worst files.</summary>
	internal const string TYPICAL_AND_WORST_REPORT_SUFFIX = "-typical-and-worst";

	/// <summary>The suffix of the part that times the argument windows and the long lines.</summary>
	internal const string WINDOWS_AND_LONG_LINES_REPORT_SUFFIX = "-windows-and-long-lines";

	/// <summary>The suffix of the part that times the definition windows and the demoted folders.</summary>
	internal const string DEFINITIONS_AND_DEMOTED_FOLDERS_REPORT_SUFFIX = "-definitions-and-demoted-folders";

	/// <summary>The suffix of the part that times the flow-signal expressions.</summary>
	internal const string FLOW_SIGNALS_REPORT_SUFFIX = "-flow-signals";

	private const int _SCAN_WARM_UP_ITERATIONS = 1;

	private const int _SCAN_MEASURED_ITERATIONS = 3;

	private const char _SHAPE_FILTER_SEPARATOR = ',';

	private readonly ITestOutputHelper _output;

	public GoShippedCostGateTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoTerminatingBenignGoTypicalOrWorstFileShapeReachesTheMatchBoundOnTheShippedAnalyzerAndTheFiguresAreRecorded()
	{
		RunThePart(GoShippedCostGateStressSet.ShippedShapeFamily.TypicalAndWorstFiles, TYPICAL_AND_WORST_REPORT_SUFFIX);
	}

	[Fact]
	public void NoTerminatingBenignGoArgumentWindowOrLongLineShapeReachesTheMatchBoundOnTheShippedAnalyzerAndTheFiguresAreRecorded()
	{
		RunThePart(GoShippedCostGateStressSet.ShippedShapeFamily.ArgumentWindowsAndLongLines, WINDOWS_AND_LONG_LINES_REPORT_SUFFIX);
	}

	[Fact]
	public void NoTerminatingBenignGoDefinitionWindowOrDemotedFolderShapeReachesTheMatchBoundOnTheShippedAnalyzerAndTheFiguresAreRecorded()
	{
		RunThePart(GoShippedCostGateStressSet.ShippedShapeFamily.DefinitionWindowsAndDemotedFolders, DEFINITIONS_AND_DEMOTED_FOLDERS_REPORT_SUFFIX);
	}

	[Fact]
	public void NoTerminatingBenignGoFlowSignalShapeReachesTheMatchBoundOnTheShippedAnalyzerAndTheFiguresAreRecorded()
	{
		RunThePart(GoShippedCostGateStressSet.ShippedShapeFamily.FlowSignals, FLOW_SIGNALS_REPORT_SUFFIX);
	}

	[Fact]
	public void NoGoShapeSpendsTheLexicalBudgetOnTheShippedProfile()
	{
		ILanguageAnalyzer analyzer = GoRegisteredExpressions.Analyzer;
		ImmutableArray<(string Name, double Charged, double WholeScan, bool Abandoned, bool Tripped)> scans =
		[
			.. SelectTheShapesToTime(GoShippedCostGateStressSet.Build()).Select(shape => TimeTheScan(analyzer.LexicalProfile, shape))
		];
		double ceiling = JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS
			: LexicalBudgetTimingTests.SHELL_UNOPTIMIZED_CEILING_MILLISECONDS;
		string report = DescribeTheScans(scans, ceiling);

		_output.WriteLine(report);
		WriteWhereRequested(LEXICAL_REPORT_PATH_VARIABLE_NAME, report, string.Empty);

		Assert.All(scans, scan => Assert.False(scan.Tripped, scan.Name));
		Assert.All(scans, scan => Assert.True(scan.Charged < ceiling, $"{scan.Name}: {scan.Charged:F4} ms charged against {ceiling:F1} ms"));
	}

	private void RunThePart(GoShippedCostGateStressSet.ShippedShapeFamily family, string reportSuffix)
	{
		GoRecognitionHarness harness = new();
		MatchBoundTimingTarget target = ForGo(harness);
		bool tableRequested = MatchBoundTimingHarness.TimingTableIsRequested(ENGINE_REPORT_PATH_VARIABLE_NAME);
		ImmutableArray<MatchBoundStressShape> shapes = GoShippedCostGateStressSet.BuildTheFamily(family);
		ImmutableArray<MatchBoundShapeTiming> timings =
		[
			.. SelectTheShapesToTime(shapes).Select(shape => MatchBoundTimingHarness.Measure(target, shape, tableRequested))
		];
		string table = MatchBoundTimingHarness.Describe(
			timings,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS);
		string report = string.Format(
			CultureInfo.InvariantCulture,
			"# Part of the Go shipped cost gate: {0} ({1} shapes); the gate's worst figure is the largest of its four parts{2}{3}",
			family,
			shapes.Length,
			Environment.NewLine,
			table);
		double ceiling = JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS
			: UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;

		_output.WriteLine(report);
		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Aim (not a gate): worst benign per-file figure about {0:F0} ms; gate: under the match bound of {1:F0} ms.",
			JavaScriptMatchBoundTimingTests.AIM_WORST_FILE_MILLISECONDS,
			ceiling));
		WriteWhereRequested(ENGINE_REPORT_PATH_VARIABLE_NAME, report, reportSuffix);
		WriteTheStressSetWhereRequested(shapes);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Equal(0, timing.FlowTrips));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingRules));

		if (family == GoShippedCostGateStressSet.ShippedShapeFamily.FlowSignals)
		{
			Assert.All(timings, timing => Assert.Equal(1, timing.FlowEvaluations));
		}

		Assert.All(
			timings,
			timing => Assert.True(timing.WorstFileMilliseconds < ceiling, $"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against {ceiling:F0} ms"));
	}

	/// <summary>The shipped `Go` analyzer as a timing target: its shapes stand where <see cref="GoShippedCostGateStressSet.RelativePathOf"/> puts them, so a vendored shape is demoted as a vendored file.</summary>
	internal static MatchBoundTimingTarget ForGo(GoRecognitionHarness harness)
	{
		return new MatchBoundTimingTarget
		{
			Analyzer = harness.Analyzer,
			EmissionPoint = harness.EmissionPoint,
			ContextFor = (text, path) => harness.ContextFor(text, path),
			RelativePathOf = GoShippedCostGateStressSet.RelativePathOf
		};
	}

	private static IEnumerable<MatchBoundStressShape> SelectTheShapesToTime(ImmutableArray<MatchBoundStressShape> shapes)
	{
		string? filter = Environment.GetEnvironmentVariable(SHAPE_FILTER_VARIABLE_NAME);
		string[] fragments = string.IsNullOrWhiteSpace(filter) ? [] : filter.Split(_SHAPE_FILTER_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		return fragments.Length == 0
			? shapes
			: shapes.Where(shape => fragments.Any(fragment => shape.Name.Contains(fragment, StringComparison.Ordinal)));
	}

	private static (string Name, double Charged, double WholeScan, bool Abandoned, bool Tripped) TimeTheScan(LexicalProfile profile, MatchBoundStressShape shape)
	{
		ChargeRecordingTimeProvider clock = new();
		LexicalScanner scanner = new(profile, clock);
		double charged = 0.0;
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
				wholeScan = Math.Max(wholeScan, stopwatch.Elapsed.TotalMilliseconds);
			}
		}

		return (shape.Name, charged, wholeScan, abandoned, tripped);
	}

	private static string DescribeTheScans(ImmutableArray<(string Name, double Charged, double WholeScan, bool Abandoned, bool Tripped)> scans, double ceiling)
	{
		StringBuilder report = new();
		(string Name, double Charged, double WholeScan, bool Abandoned, bool Tripped) worst = scans.OrderByDescending(scan => scan.Charged).First();

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Lexical budget of the shipped Go profile: {0} warm-up and {1} measured iterations per shape, the worst reported; build {2}; budget in force {3:F1} ms",
			_SCAN_WARM_UP_ITERATIONS,
			_SCAN_MEASURED_ITERATIONS,
			JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug)",
			ceiling));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Environment: {0}; {1}; {2} logical processors; runtime {3}",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version));
		report.AppendLine("shape\tcharged_ms\twhole_scan_ms\tabandoned\ttripped");

		foreach ((string Name, double Charged, double WholeScan, bool Abandoned, bool Tripped) scan in scans)
		{
			report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1:F4}\t{2:F1}\t{3}\t{4}", scan.Name, scan.Charged, scan.WholeScan, scan.Abandoned, scan.Tripped));
		}

		report.AppendLine(string.Format(CultureInfo.InvariantCulture, "Worst charged per file {0:F4} ms on {1}", worst.Charged, worst.Name));

		return report.ToString();
	}

	private static void WriteTheStressSetWhereRequested(ImmutableArray<MatchBoundStressShape> shapes)
	{
		string? stressFolder = Environment.GetEnvironmentVariable(STRESS_FOLDER_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(stressFolder))
		{
			foreach (MatchBoundStressShape shape in shapes)
			{
				string path = Path.Combine(stressFolder, GoShippedCostGateStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
	}

	/// <summary>Writes the report where the variable points, with the part's suffix before the extension, so the parts never overwrite one another.</summary>
	private static void WriteWhereRequested(string variableName, string report, string reportSuffix)
	{
		string? reportPath = Environment.GetEnvironmentVariable(variableName);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			string suffixedFileName = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", Path.GetFileNameWithoutExtension(reportPath), reportSuffix, Path.GetExtension(reportPath));
			string suffixedPath = Path.Combine(Path.GetDirectoryName(reportPath) ?? string.Empty, suffixedFileName);

			File.WriteAllText(suffixedPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}
}
