using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Pipeline;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// FLOW-SIGNAL EVALUATION AT THE LARGEST SUPPORTED SIZE, THROUGH THE REAL ANALYSIS PATH, PAST THE GLOBAL
/// FINDINGS CAP.
///
/// Forty files each hold more constructs than the per-file cap, so the first twenty-five exhaust the
/// fifty-thousand global budget and the remaining fifteen contribute no finding at all. Signals are
/// evaluated per file BEFORE the cap is applied - the cap belongs to composition, which runs after
/// recognition - so the count of files evaluated must equal the number of files holding a High or
/// Medium construct, the fifteen included. Five further files hold Low constructs only and must not
/// be evaluated. The fixture analyzer declares one signal of each kind.
///
/// Each file carries a signal token chosen by its index (an interactive one, a fixed one, or none),
/// so the evidence copied onto the retained findings can be checked against what was generated.
/// </summary>
[Collection(SessionStateVolumeProofTests.COLLECTION_NAME)]
public sealed class FlowSignalVolumeProofTests : IClassFixture<VolumeProofMemoryRelease>
{
	private const int _FILE_COUNT = 40;
	private const int _MATCHES_OFFERED_PER_FILE = 3_000;
	private const int _LOW_ONLY_FILE_COUNT = 5;
	private const int _KIND_CYCLE_LENGTH = 3;
	private const int _INTERACTIVE_CYCLE_POSITION = 0;
	private const int _FIXED_CYCLE_POSITION = 1;

	private const string _SOURCE_FILE_NAME_FORMAT = "module-{0:D3}/source/file.cs";
	private const string _LOW_ONLY_FILE_NAME_FORMAT = "zz-lowonly-{0:D3}/source/file.cs";
	private const string _MODULE_FOLDER_PREFIX = "module-";
	private const string _PATH_SEPARATOR = "/";
	private const string _CALL_FORMAT = "\t\t{0}();";

	private readonly ITestOutputHelper _output;

	public FlowSignalVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void EveryFileHoldingAHighOrMediumConstructIsEvaluatedIncludingThoseAfterTheGlobalCap()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheSystemClockWithFlowSignals(
			[FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly]);

		PlantTree(harness);
		harness.RunAndCommit(CommandId.Scan);

		Stopwatch stopwatch = Stopwatch.StartNew();
		StageRunOutcome outcome = harness.Run(CommandId.Analyze);
		stopwatch.Stop();

		long peakWorkingSet = Process.GetCurrentProcess().PeakWorkingSet64;
		FlowEvaluationFigures figures = ReadTheFigures(harness);

		ReportFigures(figures, stopwatch.Elapsed, peakWorkingSet);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.True(harness.State.FindingsCapOutcome.GlobalCapTripped);
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, harness.State.Findings.Length);
		Assert.Equal(_FILE_COUNT, figures.FilesEvaluated);
		Assert.True(figures.FilesEvaluated > figures.FilesWithAnAdmittedFinding);
		Assert.Equal(0, figures.SignalTripCount);
		AssertTheRetainedFindingsCarryWhatWasGenerated(harness.State.Findings);
	}

	private static FlowEvaluationFigures ReadTheFigures(PipelineHarness harness)
	{
		int filesWithAnAdmittedFinding = harness.State.Findings.Select(finding => finding.Path).Distinct().Count();

		return new FlowEvaluationFigures
		{
			FilesEvaluated = harness.State.FlowEvaluationDiagnostics.FilesEvaluated,
			SignalTripCount = harness.State.FlowEvaluationDiagnostics.SignalTripCount,
			FilesWithAnAdmittedFinding = filesWithAnAdmittedFinding
		};
	}

	private static void AssertTheRetainedFindingsCarryWhatWasGenerated(ImmutableArray<Finding> findings)
	{
		Assert.All(findings, finding => Assert.Equal(ExpectedEvidenceOf(finding), finding.FlowEvidence));
		Assert.Contains(findings, finding => finding.FlowEvidence == FlowEvidence.Interactive);
		Assert.Contains(findings, finding => finding.FlowEvidence == FlowEvidence.Fixed);
		Assert.Contains(findings, finding => finding.FlowEvidence == FlowEvidence.None);
	}

	private static FlowEvidence ExpectedEvidenceOf(Finding finding)
	{
		string path = finding.Path.Value;
		string folder = path[..path.IndexOf(_PATH_SEPARATOR, StringComparison.Ordinal)];
		int fileIndex = int.Parse(folder[_MODULE_FOLDER_PREFIX.Length..], CultureInfo.InvariantCulture);
		int cyclePosition = fileIndex % _KIND_CYCLE_LENGTH;

		return cyclePosition switch
		{
			_INTERACTIVE_CYCLE_POSITION => FlowEvidence.Interactive,
			_FIXED_CYCLE_POSITION => FlowEvidence.Fixed,
			_ => FlowEvidence.None
		};
	}

	private void ReportFigures(FlowEvaluationFigures figures, TimeSpan duration, long peakWorkingSet)
	{
		_output.WriteLine("Flow-signal evaluation volume proof (largest supported size)");
		_output.WriteLine(FormattedLine("Files analyzed", _FILE_COUNT + _LOW_ONLY_FILE_COUNT));
		_output.WriteLine(FormattedLine("Files holding a High or Medium construct", _FILE_COUNT));
		_output.WriteLine(FormattedLine("Files evaluated (counter)", figures.FilesEvaluated));
		_output.WriteLine(FormattedLine("Files with an admitted finding", figures.FilesWithAnAdmittedFinding));
		_output.WriteLine(FormattedLine("Flow-signal trip count", figures.SignalTripCount));
		_output.WriteLine(FormattedLine("Analyze duration (milliseconds)", (long)duration.TotalMilliseconds));
		_output.WriteLine(FormattedLine("Peak working set of the test process (bytes)", peakWorkingSet));
	}

	private static string FormattedLine(string label, long value)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}: {1:N0}", label, value);
	}

	private static void PlantTree(PipelineHarness harness)
	{
		for (int fileIndex = 0; fileIndex < _FILE_COUNT; fileIndex++)
		{
			harness.WriteSourceFile(
				string.Format(CultureInfo.InvariantCulture, _SOURCE_FILE_NAME_FORMAT, fileIndex),
				ManyHighConstructCalls(fileIndex));
		}

		for (int fileIndex = 0; fileIndex < _LOW_ONLY_FILE_COUNT; fileIndex++)
		{
			harness.WriteSourceFile(
				string.Format(CultureInfo.InvariantCulture, _LOW_ONLY_FILE_NAME_FORMAT, fileIndex),
				OneLowConstructCallWithAnInteractiveToken());
		}
	}

	/// <summary>One file of untemplated High constructs, carrying the signal token its index selects.</summary>
	private static string ManyHighConstructCalls(int fileIndex)
	{
		StringBuilder builder = new();

		builder.AppendLine("public sealed class ManyConstructs");
		builder.AppendLine("{");
		builder.AppendLine("\tvoid Build()");
		builder.AppendLine("\t{");

		switch (fileIndex % _KIND_CYCLE_LENGTH)
		{
			case _INTERACTIVE_CYCLE_POSITION:
				builder.AppendLine(string.Format(CultureInfo.InvariantCulture, _CALL_FORMAT, FlowSignalFixtures.INTERACTIVE_TOKEN));
				break;
			case _FIXED_CYCLE_POSITION:
				builder.AppendLine(string.Format(CultureInfo.InvariantCulture, _CALL_FORMAT, FlowSignalFixtures.FIXED_TOKEN));
				break;
			default:
				break;
		}

		for (int index = 0; index < _MATCHES_OFFERED_PER_FILE; index++)
		{
			builder.AppendLine(string.Format(CultureInfo.InvariantCulture, _CALL_FORMAT, PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT));
		}

		builder.AppendLine("\t}");
		builder.AppendLine("}");

		return builder.ToString();
	}

	/// <summary>A file whose only construct is Low, so no flow decision is made for it whatever tokens it holds.</summary>
	private static string OneLowConstructCallWithAnInteractiveToken()
	{
		StringBuilder builder = new();

		builder.AppendLine("public sealed class OnlyLow");
		builder.AppendLine("{");
		builder.AppendLine("\tvoid Build()");
		builder.AppendLine("\t{");
		builder.AppendLine(string.Format(CultureInfo.InvariantCulture, _CALL_FORMAT, FlowSignalFixtures.INTERACTIVE_TOKEN));
		builder.AppendLine(string.Format(CultureInfo.InvariantCulture, _CALL_FORMAT, PipelineLanguageAnalyzer.LOW_CONSTRUCT));
		builder.AppendLine("\t}");
		builder.AppendLine("}");

		return builder.ToString();
	}

	private sealed record FlowEvaluationFigures
	{
		internal required int FilesEvaluated { get; init; }

		internal required int SignalTripCount { get; init; }

		internal required int FilesWithAnAdmittedFinding { get; init; }
	}
}
