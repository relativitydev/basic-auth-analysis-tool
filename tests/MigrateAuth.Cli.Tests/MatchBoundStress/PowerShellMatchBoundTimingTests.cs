using System.Collections.Immutable;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE MATCH-BOUND TIMING PROCEDURE OVER THE `PowerShell` SHAPES, run through exactly the procedure the `CSharp` shapes are run through
/// (<see cref="MatchBoundTimingHarness"/>): the worst benign per-file analyzer-expression time, read back from a real engine run on the system clock.
/// The bound is set over BOTH languages - the larger worst governs - so this table and <see cref="MatchBoundTimingTests"/>'s are read together, one run
/// of each per operating system, and the timing table states which shape on which system produced the figure.
///
/// WHAT THIS ASSERTS. That no expression's own timeout fired; that no flow evaluation was cut short, and that the flow signals were evaluated on every
/// flow shape, so a shape that never reaches them cannot pass for one that does; that no detection rule's enumeration stalls; and that the worst
/// per-file time stays under the bound in force. The multiple and the value derived from the figures belong to the timing table, not to this test.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class PowerShellMatchBoundTimingTests
{
	/// <summary>When set, names the file the timing table is also written to, so a run leaves a record outside the test log.</summary>
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_POWERSHELL";

	private readonly ITestOutputHelper _output;

	public PowerShellMatchBoundTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void NoTerminatingBenignPowerShellShapeReachesTheMatchBoundAndTheFiguresAreRecorded()
	{
		PowerShellRecognitionHarness harness = new();
		MatchBoundTimingTarget target = MatchBoundTimingHarness.ForPowerShell(harness);
		bool tableRequested = MatchBoundTimingHarness.TimingTableIsRequested(REPORT_PATH_VARIABLE_NAME);
		ImmutableArray<MatchBoundShapeTiming> timings =
		[
			.. PowerShellMatchBoundStressSet.Build().Select(shape => MatchBoundTimingHarness.Measure(target, shape, tableRequested))
		];

		string report = MatchBoundTimingHarness.Describe(
			timings,
			MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE,
			MatchBoundTimingTests.ROUNDING_STEP_IN_MILLISECONDS);
		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);
		WriteTheStressSetWhereRequested();

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Equal(0, timing.FlowTrips));
		Assert.All(
			timings.Where(timing => timing.ShapeName.StartsWith(PowerShellMatchBoundStressSet.FLOW_SHAPE_NAME_PREFIX, StringComparison.Ordinal)),
			timing => Assert.Equal(1, timing.FlowEvaluations));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingRules));
		Assert.All(
			timings,
			timing => Assert.True(
				timing.WorstFileMilliseconds < RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS,
				timing.ShapeName));
	}

	private static void WriteTheStressSetWhereRequested()
	{
		string? stressFolder = Environment.GetEnvironmentVariable(MatchBoundTimingTests.STRESS_FOLDER_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(stressFolder))
		{
			foreach (MatchBoundStressShape shape in PowerShellMatchBoundStressSet.Build())
			{
				string path = Path.Combine(stressFolder, PowerShellMatchBoundStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
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
