using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE ENGINE-MEASURED COST GATE OF THE SHIPPED `TypeScript` ANALYZER, on the pattern of <see cref="JavaScriptShippedCostGateTimingTests"/>: each stress shape of <see cref="TypeScriptShippedCostGateStressSet"/> (the shapes of
/// <see cref="TypeScriptMatchBoundStressSet"/> and the shapes the shipped expressions add) runs through the registered analyzer and the engine's own file accumulator is read back, so the figure is exactly what a real scan
/// charges against the match bound. The per-expression gate (<see cref="TypeScriptMatchBoundTimingTests"/>) times test-local copies of the expressions; this one times what ships, the shared `JavaScript` expressions that
/// carry the generic-argument group and the tail refusal among them.
///
/// THE 975 ms FIGURE IS AN AIM, NOT A GATE, printed beside the worst shape. What is asserted is what must hold whatever the aim: no expression
/// times out, no enumeration stalls, no flow evaluation trips, and every shape's worst per-file figure stays under the MATCH BOUND in force.
///
/// A `TypeScript` file has no first-line rule, so nothing is charged to the lexical budget here; the budget's own measurement is <see cref="TypeScriptLexicalBudgetTimingTests"/>. The extension of each shape selects the
/// file form (`.ts`, `.tsx`, `.mts`, `.cts`, `.d.ts`), so every file form the analyzer reads is gated.
///
/// Timing-sensitive: it asserts on wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class TypeScriptShippedCostGateTimingTests
{
	internal const string ENGINE_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_TYPESCRIPT_ENGINE";

	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER_TYPESCRIPT_ENGINE";

	/// <summary>When set to a comma-separated list of shape-name fragments, only the shapes whose name holds one of them are timed: a reshape probe, never the gate (the gate runs with it unset).</summary>
	internal const string SHAPE_FILTER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_SHAPES_TYPESCRIPT_ENGINE";

	/// <summary>On a build with the JIT optimizer disabled the generated expressions run several times slower than in the product: ten times the bound would still catch an expression that lost its bound.</summary>
	internal const double UNOPTIMIZED_BUILD_CEILING_MILLISECONDS = MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE * RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS;

	private const char _SHAPE_FILTER_SEPARATOR = ',';

	private readonly ITestOutputHelper _output;

	public TypeScriptShippedCostGateTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoTerminatingBenignTypeScriptShapeReachesTheMatchBoundOnTheShippedAnalyzerAndTheFiguresAreRecorded()
	{
		TypeScriptRecognitionHarness harness = new();
		MatchBoundTimingTarget target = ForTypeScript(harness);
		bool tableRequested = MatchBoundTimingHarness.TimingTableIsRequested(ENGINE_REPORT_PATH_VARIABLE_NAME);
		ImmutableArray<MatchBoundStressShape> shapes = TypeScriptShippedCostGateStressSet.Build();
		ImmutableArray<MatchBoundShapeTiming> timings =
		[
			.. SelectTheShapesToTime(shapes).Select(shape => MatchBoundTimingHarness.Measure(target, shape, tableRequested))
		];
		string report = MatchBoundTimingHarness.Describe(
			timings,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS);
		double ceiling = JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS
			: UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;

		_output.WriteLine(report);
		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Aim (not a gate): worst benign per-file figure about {0:F0} ms; gate: under the match bound of {1:F0} ms.",
			JavaScriptMatchBoundTimingTests.AIM_WORST_FILE_MILLISECONDS,
			ceiling));
		WriteWhereRequested(ENGINE_REPORT_PATH_VARIABLE_NAME, report);
		WriteTheStressSetWhereRequested(shapes);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Equal(0, timing.FlowTrips));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingRules));
		Assert.All(
			timings,
			timing => Assert.True(timing.WorstFileMilliseconds < ceiling, $"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against {ceiling:F0} ms"));
	}

	/// <summary>The shipped `TypeScript` analyzer as a timing target: its shapes stand where <see cref="TypeScriptMatchBoundStressSet.RelativePathOf"/> puts them, under the extension each names.</summary>
	internal static MatchBoundTimingTarget ForTypeScript(TypeScriptRecognitionHarness harness)
	{
		return new MatchBoundTimingTarget
		{
			Analyzer = harness.Analyzer,
			EmissionPoint = harness.EmissionPoint,
			ContextFor = (text, path) => harness.ContextFor(text, path),
			RelativePathOf = TypeScriptShippedCostGateStressSet.RelativePathOf
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
				string path = Path.Combine(stressFolder, TypeScriptShippedCostGateStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
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
