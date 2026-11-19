using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE ENGINE-MEASURED COST GATE OF THE SHIPPED `Python` ANALYZER, on the pattern of <see cref="TypeScriptShippedCostGateTimingTests"/>: each stress shape of <see cref="PythonShippedCostGateStressSet"/> runs through the registered
/// analyzer and the engine's own file accumulator is read back, so the figure is exactly what a real scan charges against the match bound. It times what ships, not a test-local copy of the expressions.
///
/// THE 975 ms FIGURE IS AN AIM, NOT A GATE. What is asserted is what must hold whatever the aim: no expression times out, no enumeration stalls, no flow evaluation trips, and every shape's worst per-file figure stays under the
/// MATCH BOUND in force. A `Python` file has a first-line rule, but it is charged to the lexical budget and not here: its own measurement is the first-line stress set and the lexical-budget timing of the `Python` profile.
///
/// THREE PARTS BY SHAPE FAMILY, with the same assertions: the argument-window shapes, the rest of the set (the shapes the catalog names, the window families and the per-file worst shapes) and the flow-signal shapes, which
/// also assert that the engine evaluated the flow signals once per shape (each opens with one High construct; the engine evaluates them only over a file that holds one). Together they
/// time exactly the shapes of <see cref="PythonShippedCostGateStressSet.Build"/>, which a test with no clock pins. Each part writes its own report, where the report variable points with the part's suffix before the extension.
///
/// Timing-sensitive: it asserts on wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing". It is WRITTEN AND COMPILED in the set that registers the analyzer and
/// RUN only in a separate timing window, never in the ordinary suite.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class PythonShippedCostGateTimingTests
{
	internal const string ENGINE_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_PYTHON_ENGINE";

	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER_PYTHON_ENGINE";

	/// <summary>When set to a comma-separated list of shape-name fragments, only the shapes whose name holds one of them are timed: a reshape probe, never the gate (the gate runs with it unset).</summary>
	internal const string SHAPE_FILTER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_SHAPES_PYTHON_ENGINE";

	/// <summary>On a build with the JIT optimizer disabled the generated expressions run several times slower than in the product: ten times the bound would still catch an expression that lost its bound.</summary>
	internal const double UNOPTIMIZED_BUILD_CEILING_MILLISECONDS = MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE * RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS;

	/// <summary>The suffix put before the extension of the report path of the part that times the argument-window shapes.</summary>
	internal const string ARGUMENT_WINDOW_REPORT_SUFFIX = "-argument-window";

	/// <summary>The suffix of the part that times every other shape of the set.</summary>
	internal const string REMAINING_SHAPES_REPORT_SUFFIX = "-remaining-shapes";

	/// <summary>The suffix of the part that times the flow-signal expressions: its shapes each open with one High construct, so the engine evaluates the flow signals inside the same accumulator.</summary>
	internal const string FLOW_SIGNALS_REPORT_SUFFIX = "-flow-signals";

	private const char _SHAPE_FILTER_SEPARATOR = ',';

	private readonly ITestOutputHelper _output;

	public PythonShippedCostGateTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoTerminatingBenignPythonArgumentWindowShapeReachesTheMatchBoundOnTheShippedAnalyzerAndTheFiguresAreRecorded()
	{
		RunThePart(PythonShippedCostGateStressSet.BuildTheArgumentWindowShapes(), ARGUMENT_WINDOW_REPORT_SUFFIX, "the argument-window shapes");
	}

	[Fact]
	public void NoTerminatingBenignPythonShapeOfTheRemainingFamiliesReachesTheMatchBoundOnTheShippedAnalyzerAndTheFiguresAreRecorded()
	{
		RunThePart(PythonShippedCostGateStressSet.BuildTheRemainingShapes(), REMAINING_SHAPES_REPORT_SUFFIX, "the remaining shapes");
	}

	[Fact]
	public void NoTerminatingBenignPythonFlowSignalShapeReachesTheMatchBoundOnTheShippedAnalyzerAndTheFiguresAreRecorded()
	{
		RunThePart(PythonShippedCostGateStressSet.BuildTheFlowSignalShapes(), FLOW_SIGNALS_REPORT_SUFFIX, "the flow-signal shapes", flowSignalsMustBeEvaluated: true);
	}

	private void RunThePart(ImmutableArray<MatchBoundStressShape> shapes, string reportSuffix, string partName, bool flowSignalsMustBeEvaluated = false)
	{
		PythonRecognitionHarness harness = new();
		MatchBoundTimingTarget target = ForPython(harness);
		bool tableRequested = MatchBoundTimingHarness.TimingTableIsRequested(ENGINE_REPORT_PATH_VARIABLE_NAME);
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
			"# Part of the Python shipped cost gate: {0} ({1} shapes); the gate's worst figure is the largest of its three parts{2}{3}",
			partName,
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

		if (flowSignalsMustBeEvaluated)
		{
			Assert.All(timings, timing => Assert.Equal(1, timing.FlowEvaluations));
		}

		Assert.All(
			timings,
			timing => Assert.True(timing.WorstFileMilliseconds < ceiling, $"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against {ceiling:F0} ms"));
	}

	/// <summary>The shipped `Python` analyzer as a timing target: its shapes stand where <see cref="PythonShippedCostGateStressSet.RelativePathOf"/> puts them, under the extension each names.</summary>
	internal static MatchBoundTimingTarget ForPython(PythonRecognitionHarness harness)
	{
		return new MatchBoundTimingTarget
		{
			Analyzer = harness.Analyzer,
			EmissionPoint = harness.EmissionPoint,
			ContextFor = (text, path) => harness.ContextFor(text, path),
			RelativePathOf = PythonShippedCostGateStressSet.RelativePathOf
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

	private static void WriteTheStressSetWhereRequested(ImmutableArray<MatchBoundStressShape> shapes)
	{
		string? stressFolder = Environment.GetEnvironmentVariable(STRESS_FOLDER_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(stressFolder))
		{
			foreach (MatchBoundStressShape shape in shapes)
			{
				string path = Path.Combine(stressFolder, PythonShippedCostGateStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
	}

	/// <summary>Writes the part's report where the variable points, with the part's suffix before the extension, so the two parts never overwrite one another.</summary>
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
