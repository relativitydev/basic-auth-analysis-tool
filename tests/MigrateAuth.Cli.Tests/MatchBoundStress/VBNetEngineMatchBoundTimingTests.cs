using System.Collections.Immutable;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE ENGINE-MEASURED FIGURES OF THE `VBNet` STRESS SET: each shape runs through the real analyzer and the
/// engine's own file accumulator is read back, so the figure is exactly what a real scan charges against <see cref="RecognitionEnginePolicy.MATCH_BOUND"/>.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class VBNetEngineMatchBoundTimingTests
{
	/// <summary>When set, names the file the timing table is also written to, so a run leaves a record outside the test log.</summary>
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_VBNET_ENGINE";

	private readonly ITestOutputHelper _output;

	public VBNetEngineMatchBoundTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoTerminatingBenignVBNetShapeReachesTheMatchBoundAndTheFiguresAreRecorded()
	{
		VBNetRecognitionHarness harness = new();
		MatchBoundTimingTarget target = MatchBoundTimingHarness.ForVBNet(harness);
		ImmutableArray<MatchBoundShapeTiming> timings =
		[
			.. VBNetMatchBoundStressSet.Build().Select(shape => MatchBoundTimingHarness.Measure(target, shape))
		];

		string report = MatchBoundTimingHarness.Describe(
			timings,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS);
		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Equal(0, timing.FlowTrips));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingRules));
		Assert.All(
			timings,
			timing => Assert.True(
				timing.WorstFileMilliseconds < RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS,
				timing.ShapeName));
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
