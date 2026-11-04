using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE FOUR STAGE BOUNDARIES, TIMED ONE AT A TIME.
///
/// What is timed is <c>ScanStage</c>, <c>AnalyzeStage</c>, <c>SuggestStage</c> and
/// <c>ReportStage</c>, composed exactly as <c>Program</c> composes them, each behind one
/// <c>Run</c> call, on the clock the product ships with.
///
/// TWO SHAPES, BECAUSE ONE NUMBER PER STAGE WOULD BE A LIE ABOUT THE OTHER SHAPE. Throughput depends
/// on match density and not only on file count, and the four stages divide the work completely
/// differently at the two ends. A repository where 2 % of files are candidates is what ordinary
/// repositories look like in practice. A repository where every file is a candidate is the worst
/// case the scan tests already generate. Both are reported and neither is presented as the figure.
///
/// THE SIZE IS 3,000 FILES AND THAT IS A CHOICE ABOUT THE SUITE, NOT ABOUT THE PRODUCT. It is large
/// enough that a stage's duration is signal rather than scheduling noise, and small enough to run on
/// every build.
///
/// WHAT IS ASSERTED IS THE REQUIRED FLOOR AND NOTHING ELSE. A test that compared this machine against a
/// recorded figure would fail whenever a laptop was busy, and a test that fails for that reason gets
/// deleted. The measured table is reported; the assertion is the requirement.
/// </summary>
public sealed class StageTimingTests
{
	private const int _FOLDER_COUNT = 60;
	private const int _FILES_PER_FOLDER = 50;
	private const int _GENERATED_FILE_COUNT = _FOLDER_COUNT * _FILES_PER_FOLDER;

	/// <summary>One file in fifty carries a construct: 2 %.</summary>
	private const int _TYPICAL_ONE_CANDIDATE_IN = 50;

	/// <summary>Every file carries one, which is the worst case.</summary>
	private const int _WORST_CASE_ONE_CANDIDATE_IN = 1;

	/// <summary>The required floor. The assertion is against this and never against a measured figure.</summary>
	private const int _REQUIRED_FILES_PER_MINUTE = 5000;

	private const int _SECONDS_PER_MINUTE = 60;
	private const string _FILES_LABEL = "files";
	private const string _CANDIDATES_LABEL = "candidate files";
	private const string _FINDINGS_LABEL = "findings";
	private const string _SOURCE_FILE_NAME_FORMAT = "module-{0:D3}/source/file-{1:D3}.cs";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";
	private const string _WRITTEN_TO_MESSAGE_FORMAT = "The table above was also written to {0}.";
	private const string _NOT_WRITTEN_MESSAGE =
		"The repository root was not found above the test binary, so the table was not written to a file.";

	private const string _TYPICAL_SHAPE_NAME = "Typical shape: one file in fifty is a candidate";
	private const string _WORST_CASE_SHAPE_NAME = "Worst case: every file is a candidate";

	private static readonly string _typicalShapeDescription = string.Format(
		CultureInfo.InvariantCulture,
		"{0:N0} files, of which {1:N0} carry a construct. Measurements on an ordinary repository found 1.9 % candidates; this is 2 %.",
		_GENERATED_FILE_COUNT,
		_GENERATED_FILE_COUNT / _TYPICAL_ONE_CANDIDATE_IN);

	private static readonly string _worstCaseShapeDescription = string.Format(
		CultureInfo.InvariantCulture,
		"{0:N0} files, every one of them a candidate carrying one construct. This is the worst case for the one term the engine does not cap.",
		_GENERATED_FILE_COUNT);

	private const string _WARM_UP_FILE_NAME = "warm-up/Candidate.cs";

	private static readonly ImmutableArray<CommandId> _theFourStagesInRunOrder =
	[
		CommandId.Scan,
		CommandId.Analyze,
		CommandId.Suggest,
		CommandId.Report
	];

	private static readonly string _candidateContent =
		$"public sealed class Candidate\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, secret);\n}}\n";

	private readonly ITestOutputHelper _output;

	public StageTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// The measurement, the table, and the one requirement the numbers have to clear. Both shapes are
	/// measured in one test because the table is one document: reporting half of it from a run that
	/// produced the other half elsewhere would be two tables presented as one.
	/// </summary>
	[Fact]
	public void TheFourStagesAreTimedSeparatelyOverBothShapes()
	{
		WarmUpEveryStage();

		TimingShape typicalShape = MeasureOneShape(
			_TYPICAL_SHAPE_NAME,
			_typicalShapeDescription,
			_TYPICAL_ONE_CANDIDATE_IN);

		TimingShape worstCaseShape = MeasureOneShape(
			_WORST_CASE_SHAPE_NAME,
			_worstCaseShapeDescription,
			_WORST_CASE_ONE_CANDIDATE_IN);

		ReportTheTable([typicalShape, worstCaseShape]);

		AssertTheFloorIsCleared(typicalShape);
		AssertTheFloorIsCleared(worstCaseShape);
	}

	/// <summary>
	/// ONE DISCARDED RUN OVER ONE FILE, BEFORE ANYTHING IS TIMED.
	///
	/// Without it the first shape measured pays the just-in-time compilation of every path the
	/// second then finds already compiled, and the two shapes are no longer comparable to each other.
	/// It is a warm-up rather than a repeat-and-average because what this table is for is the
	/// RELATIVE weight of the four boundaries.
	/// </summary>
	private static void WarmUpEveryStage()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheSystemClock();

		harness.WriteSourceFile(_WARM_UP_FILE_NAME, _candidateContent);

		foreach (CommandId stage in _theFourStagesInRunOrder)
		{
			harness.Run(stage);
			harness.State.MarkCompleted(stage);
		}
	}

	/// <summary>
	/// One shape, generated and run once. The harness is the pipeline set's own composition - the same
	/// decorator chain <c>Program</c> builds - taken over the system clock, so the per-item clock
	/// read, the progress cadence and the match bound all cost what they cost in a session.
	/// </summary>
	private static TimingShape MeasureOneShape(string name, string description, int oneCandidateIn)
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheSystemClock();

		GenerateTree(harness, oneCandidateIn);

		ImmutableArray<StageTiming> timings =
		[
			Measure(harness, CommandId.Scan, _FILES_LABEL, () => harness.State.ScanOutcome.FileCount),
			Measure(harness, CommandId.Analyze, _CANDIDATES_LABEL, () => harness.State.CandidateRelativePaths.Count),
			Measure(harness, CommandId.Suggest, _FINDINGS_LABEL, () => harness.State.Findings.Length),
			Measure(harness, CommandId.Report, _FINDINGS_LABEL, () => harness.State.Findings.Length)
		];

		// The timing table must not report four durations over a repository that produced nothing,
		// which is the way a broken fixture would present as a very fast engine.
		Assert.Equal(_GENERATED_FILE_COUNT, harness.State.ScanOutcome.FileCount);
		Assert.Equal(_GENERATED_FILE_COUNT / oneCandidateIn, harness.State.Findings.Length);

		return new TimingShape
		{
			Name = name,
			Description = description,
			Timings = timings
		};
	}

	/// <summary>
	/// One stage, timed around its single <c>Run</c> call. What is NOT inside the stopwatch is the
	/// shell's own work after a stage completes - marking it and persisting the session state - which
	/// belongs to the session rather than to the boundary between stages.
	/// </summary>
	private static StageTiming Measure(
		PipelineHarness harness,
		CommandId stage,
		string itemLabel,
		Func<int> countItemsWorkedOver)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		StageRunOutcome outcome = harness.Run(stage);

		stopwatch.Stop();

		Assert.Equal(StageRunOutcome.Completed, outcome);

		harness.State.MarkCompleted(stage);

		return new StageTiming
		{
			StageName = CommandRegistry.Describe(stage).Name,
			Elapsed = stopwatch.Elapsed,
			ItemCount = countItemsWorkedOver(),
			ItemLabel = itemLabel
		};
	}

	private static void GenerateTree(PipelineHarness harness, int oneCandidateIn)
	{
		int generatedCount = 0;

		for (int folder = 0; folder < _FOLDER_COUNT; folder++)
		{
			for (int file = 0; file < _FILES_PER_FOLDER; file++)
			{
				bool carriesAConstruct = generatedCount % oneCandidateIn == 0;

				harness.WriteSourceFile(
					string.Format(CultureInfo.InvariantCulture, _SOURCE_FILE_NAME_FORMAT, folder, file),
					carriesAConstruct ? _candidateContent : _ORDINARY_CONTENT);

				generatedCount++;
			}
		}
	}

	/// <summary>
	/// The required floor, applied to the four stages together rather than to any one of them. The
	/// requirement is about what the user waits for, and the user waits for the workflow.
	/// </summary>
	private static void AssertTheFloorIsCleared(TimingShape shape)
	{
		double elapsedSeconds = shape.Timings.Sum(timing => timing.Elapsed.TotalSeconds);
		double filesPerMinute = _GENERATED_FILE_COUNT / Math.Max(elapsedSeconds, double.Epsilon) * _SECONDS_PER_MINUTE;

		Assert.True(filesPerMinute > _REQUIRED_FILES_PER_MINUTE);
	}

	private void ReportTheTable(ImmutableArray<TimingShape> shapes)
	{
		string document = StageTimingTable.Render(shapes, DateTimeOffset.Now);
		string? writtenPath = StageTimingTable.TryWrite(document);

		_output.WriteLine(document);
		_output.WriteLine(writtenPath is null
			? _NOT_WRITTEN_MESSAGE
			: string.Format(CultureInfo.InvariantCulture, _WRITTEN_TO_MESSAGE_FORMAT, writtenPath));
	}
}
