using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Findings;
using MigrateAuth.Cli.Tests.Pipeline;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE FINDINGS CAP, AT THE LARGEST SUPPORTED SIZE, THROUGH THE REAL ANALYSIS PATH.
///
/// This is the volume proof the resource ceilings call for: not the cap's arithmetic, which
/// <c>FindingsCapTests</c> already covers over synthetic construct sets, but the claim that a real
/// repository large enough to trip both caps actually does, that the truncation counter it produces
/// matches what was generated, and that the retained set this run keeps in memory stays bounded -
/// which is the whole reason a cap exists in the first place.
///
/// FORTY FILES, EACH OFFERING MORE MATCHES THAN THE PER-FILE CAP, TOGETHER MORE THAN THE GLOBAL
/// CAP. Every file individually trips the per-file cap - three thousand matches against a cap of
/// two thousand - so the first twenty-five files alone exhaust the fifty-thousand global budget
/// and the remaining fifteen contribute nothing at all. Both counters are therefore exercised by
/// one generated tree, and the shape is stated as arithmetic below rather than asserted against a
/// number typed twice.
///
/// WHAT IS MEASURED IS THE RETAINED SET, NOT THE WHOLE PROCESS. <c>GC.GetTotalMemory(true)</c> is
/// sampled immediately before the Analyze stage runs and immediately after, both after a forced
/// collection, so the delta approximates what this one run added to the managed heap and keeps
/// reachable. The
/// bound asserted is generous on purpose: a tight assertion against a
/// number the runtime is free to move by allocator and GC generation would fail on a busy machine
/// for a reason that has nothing to do with the cap.
/// </summary>
[Collection(SessionStateVolumeProofTests.COLLECTION_NAME)]
public sealed class FindingsCapVolumeProofTests : IClassFixture<VolumeProofMemoryRelease>
{
	private const int _FILE_COUNT = 40;
	private const int _MATCHES_OFFERED_PER_FILE = 3_000;
	private const int _TOTAL_RECOGNIZED_COUNT = _FILE_COUNT * _MATCHES_OFFERED_PER_FILE;

	/// <summary>
	/// Every one of the forty files exceeds the per-file cap on its own - three thousand offered
	/// against a cap of two thousand - so every file contributes to this count.
	/// </summary>
	private const int _EXPECTED_PER_FILE_CAP_TRIPPED_FILE_COUNT = _FILE_COUNT;

	private const int _EXPECTED_PER_FILE_OMITTED_COUNT =
		_FILE_COUNT * (_MATCHES_OFFERED_PER_FILE - FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP);

	/// <summary>How many of the forty files the global budget admits in full, at the per-file cap each.</summary>
	private const int _FITTING_FILE_COUNT =
		FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP / FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP;

	private const int _EXPECTED_RETAINED_COUNT = FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP;

	private const int _EXPECTED_GLOBAL_OMITTED_COUNT =
		(_FILE_COUNT - _FITTING_FILE_COUNT) * FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP;

	/// <summary>
	/// The bound on the retained memory delta: generous on purpose, and well inside the one-gigabyte
	/// memory limit.
	/// </summary>
	private const long _MAXIMUM_RETAINED_MEMORY_DELTA_IN_BYTES = 300L * 1024 * 1024;

	private const string _SOURCE_FILE_NAME_FORMAT = "module-{0:D3}/source/file.cs";

	private const int _ORDERED_ADMISSION_FILE_COUNT = 60;
	private const int _ORDERED_ADMISSION_CONSTRUCTS_PER_FILE = FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP;
	private const int _ORDERED_ADMISSION_LINE_COUNT = 100;
	private const int _ORDERED_ADMISSION_LINE_LENGTH = 24;
	private const int _ORDERED_ADMISSION_SEED = 20260928;
	private const string _ORDERED_ADMISSION_FILE_NAME_FORMAT = "module-{0:D3}/source/file.ex1";

	private const int _ORDERING_PATH_COUNT = 500_000;
	private const int _ORDERING_PATH_LENGTH = 150;
	private const char _ORDERING_PATH_FILLER = 'p';
	private const string _ORDERING_PATH_FORMAT = "module-{0:D6}/";
	private const string _ORDERING_PATH_EXTENSION = ".ex1";
	private const int _ORDERING_SPOT_CHECK_STRIDE = 1_000;

	/// <summary>Coprime to <see cref="_ORDERING_PATH_COUNT"/>, so multiplying by it permutes the indices.</summary>
	private const long _ORDERING_PERMUTATION_MULTIPLIER = 104_729;

	private const double _BYTES_PER_MEBIBYTE = 1024.0 * 1024.0;
	private const long _ONE_GIGABYTE_IN_BYTES = 1024L * 1024 * 1024;

	private readonly ITestOutputHelper _output;

	public FindingsCapVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void FortyOverCapFilesTripBothCapsWithBoundedRetainedMemory()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheSystemClock();

		PlantTree(harness);

		harness.RunAndCommit(CommandId.Scan);

		ForceFullCollection();
		long memoryBeforeAnalyze = GC.GetTotalMemory(false);

		StageRunOutcome outcome = harness.Run(CommandId.Analyze);

		ForceFullCollection();
		long memoryAfterAnalyze = GC.GetTotalMemory(false);
		long retainedMemoryDelta = Math.Max(0, memoryAfterAnalyze - memoryBeforeAnalyze);

		ReportFigures(memoryBeforeAnalyze, memoryAfterAnalyze, retainedMemoryDelta);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		AssertTheCountersMatchWhatWasGenerated(harness);
		Assert.True(
			retainedMemoryDelta < _MAXIMUM_RETAINED_MEMORY_DELTA_IN_BYTES,
			$"Retained memory delta {retainedMemoryDelta:N0} bytes exceeded the stated bound of {_MAXIMUM_RETAINED_MEMORY_DELTA_IN_BYTES:N0} bytes.");
	}

	/// <summary>
	/// KEY-ORDERED ADMISSION AT VOLUME. Sixty files of two thousand constructs each - 120,000 findings offered against
	/// a global cap of 50,000 - are composed in key order and in a shuffled arrival order. The
	/// admitted set is the key-ordered prefix in both, with the same numbering and the same counters,
	/// and the time each composition took is recorded.
	/// </summary>
	[Fact]
	public void KeyOrderedAdmissionOverMoreThanFiftyThousandFindingsIsTheSameInAnyArrivalOrder()
	{
		FindingHarness harness = new();
		RecognizedFile[] files = [.. Enumerable.Range(0, _ORDERED_ADMISSION_FILE_COUNT).Select(index => OrderedAdmissionFile(harness, index))];
		Random random = new(_ORDERED_ADMISSION_SEED);
		RecognizedFile[] shuffled = [.. files.OrderBy(_ => random.Next())];

		Stopwatch inOrderWatch = Stopwatch.StartNew();
		ComposedFindingSet inOrder = FindingComposition.ComposeWithCapOutcome(files);
		inOrderWatch.Stop();

		Stopwatch shuffledWatch = Stopwatch.StartNew();
		ComposedFindingSet fromShuffled = FindingComposition.ComposeWithCapOutcome(shuffled);
		shuffledWatch.Stop();

		_output.WriteLine("Key-ordered admission volume proof");
		_output.WriteLine(FormattedLine("Findings offered", _ORDERED_ADMISSION_FILE_COUNT * _ORDERED_ADMISSION_CONSTRUCTS_PER_FILE));
		_output.WriteLine(FormattedLine("Findings admitted", inOrder.Findings.Length));
		_output.WriteLine(FormattedLine("Composition in key order (ms)", inOrderWatch.ElapsedMilliseconds));
		_output.WriteLine(FormattedLine("Composition in shuffled order (ms)", shuffledWatch.ElapsedMilliseconds));

		Assert.True(_ORDERED_ADMISSION_FILE_COUNT * _ORDERED_ADMISSION_CONSTRUCTS_PER_FILE > FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP);
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, inOrder.Findings.Length);
		Assert.Equal(FindingHarness.FingerprintsOf(inOrder.Findings), FindingHarness.FingerprintsOf(fromShuffled.Findings));
		Assert.Equal(FindingHarness.IdentifiersOf(inOrder.Findings), FindingHarness.IdentifiersOf(fromShuffled.Findings));
		Assert.Equal(inOrder.CapOutcome, fromShuffled.CapOutcome);
		Assert.True(inOrder.CapOutcome.GlobalCapTripped);
	}

	/// <summary>
	/// THE WORST CASE FOR THE KEY: 500,000 paths of 150 characters. They are
	/// sorted under the order key with its raw-ordinal tie-break, and the same half-million files -
	/// one construct each, offered in a permuted order - go through global-cap admission, which must
	/// keep only the 50,000 smallest keys. Duration and peak working set are recorded for each.
	/// No path is normalized, so what is measured is the comparison and the bounded admission. Each
	/// runs, and checks its result, in a child process that has done nothing else
	/// (<see cref="VolumeProofChildProcess"/>).
	/// </summary>
	[Fact]
	public void HalfAMillionLongPathsSortAndAdmitUnderTheKeyWithinOneGigabyte()
	{
		OperationFigures sorting = VolumeProofChildProcess.Run(VolumeProofChildProcess.ORDERING_SORT_OPERATION);
		OperationFigures admission = VolumeProofChildProcess.Run(VolumeProofChildProcess.ORDERING_ADMISSION_OPERATION);

		_output.WriteLine("Ordering proof, 500,000 paths of 150 characters");
		_output.WriteLine(FormattedFigures("Sort under the order key with tie-break", sorting));
		_output.WriteLine(FormattedFigures("Global-cap admission over 500,000 files in permuted order", admission));

		Assert.True(sorting.PeakWorkingSetInBytes < _ONE_GIGABYTE_IN_BYTES);
		Assert.True(admission.PeakWorkingSetInBytes < _ONE_GIGABYTE_IN_BYTES);
	}

	private static RecognizedFile OrderedAdmissionFile(FindingHarness harness, int index)
	{
		RecognizedConstruct[] constructs =
		[
			.. Enumerable.Range(0, _ORDERED_ADMISSION_CONSTRUCTS_PER_FILE)
				.Select(offset => FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, offset))
		];

		return harness.FileOf(
			string.Format(CultureInfo.InvariantCulture, _ORDERED_ADMISSION_FILE_NAME_FORMAT, index),
			FindingHarness.FillerText(_ORDERED_ADMISSION_LINE_COUNT, _ORDERED_ADMISSION_LINE_LENGTH),
			constructs);
	}

	/// <summary>The sort, as the child process runs it.</summary>
	internal static OperationFigures SortHalfAMillionPaths(string[] arguments)
	{
		return MeasureSortingOfHalfAMillionPaths(new FindingHarness());
	}

	/// <summary>The admission, as the child process runs it.</summary>
	internal static OperationFigures AdmitHalfAMillionFiles(string[] arguments)
	{
		return MeasureAdmissionOfHalfAMillionFiles(new FindingHarness());
	}

	private static OperationFigures MeasureSortingOfHalfAMillionPaths(FindingHarness harness)
	{
		FindingOrderKey[] keys = [.. Enumerable.Range(0, _ORDERING_PATH_COUNT).Select(index => OrderKeyOf(harness, PermutedIndex(index)))];

		PeakWorkingSetSampler.ReleaseUnusedMemory();

		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();
		long workingSetBefore = Environment.WorkingSet;
		Stopwatch stopwatch = Stopwatch.StartNew();

		Array.Sort(keys);

		stopwatch.Stop();

		OperationFigures figures = new(stopwatch.Elapsed, sampler.StopAndReadPeakInBytes(), workingSetBefore);

		Assert.Equal(_ORDERING_PATH_COUNT, keys.Length);
		Assert.Equal(PathTextOf(0), keys[0].RawPath);
		Assert.Equal(PathTextOf(_ORDERING_PATH_COUNT - 1), keys[^1].RawPath);
		Assert.All(
			Enumerable.Range(1, keys.Length - 1).Where(index => index % _ORDERING_SPOT_CHECK_STRIDE == 0),
			index => Assert.True(keys[index - 1].CompareTo(keys[index]) < 0));

		return figures;
	}

	private static OperationFigures MeasureAdmissionOfHalfAMillionFiles(FindingHarness harness)
	{
		string text = FindingHarness.FillerText(_ORDERED_ADMISSION_LINE_COUNT, _ORDERED_ADMISSION_LINE_LENGTH);
		RecognizedConstruct construct = FindingHarness.ConstructAt(FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS, anchorOffset: 0);

		PeakWorkingSetSampler.ReleaseUnusedMemory();

		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();
		long workingSetBefore = Environment.WorkingSet;
		Stopwatch stopwatch = Stopwatch.StartNew();

		ComposedFindingSet composed = FindingComposition.ComposeWithCapOutcome(
			Enumerable.Range(0, _ORDERING_PATH_COUNT).Select(index => harness.FileOf(PathTextOf(PermutedIndex(index)), text, construct)));

		stopwatch.Stop();

		OperationFigures figures = new(stopwatch.Elapsed, sampler.StopAndReadPeakInBytes(), workingSetBefore);

		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, composed.Findings.Length);
		Assert.Equal(_ORDERING_PATH_COUNT, composed.CapOutcome.TotalRecognizedCount);
		Assert.Equal(_ORDERING_PATH_COUNT - FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, composed.CapOutcome.GlobalOmittedCount);
		Assert.Equal(PathTextOf(0), composed.Findings[0].Path.Value);
		Assert.Equal(PathTextOf(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP - 1), composed.Findings[^1].Path.Value);

		return figures;
	}

	private static FindingOrderKey OrderKeyOf(FindingHarness harness, int pathNumber)
	{
		MigrateAuth.Cli.Emission.RepositoryRelativePath path = harness.PathOf(PathTextOf(pathNumber));

		return new FindingOrderKey
		{
			Path = NormalizedPathKey.From(path),
			Line = 1,
			Column = 1,
			PatternClassIdentifier = FindingHarness.CREDENTIAL_TYPE_PATTERN_CLASS,
			RawPath = path.Value
		};
	}

	private static int PermutedIndex(int index)
	{
		return (int)(index * _ORDERING_PERMUTATION_MULTIPLIER % _ORDERING_PATH_COUNT);
	}

	/// <summary>A path of exactly 150 characters whose leading number decides its place in the order.</summary>
	private static string PathTextOf(int pathNumber)
	{
		string head = string.Format(CultureInfo.InvariantCulture, _ORDERING_PATH_FORMAT, pathNumber);
		string filler = new(_ORDERING_PATH_FILLER, _ORDERING_PATH_LENGTH - head.Length - _ORDERING_PATH_EXTENSION.Length);

		return head + filler + _ORDERING_PATH_EXTENSION;
	}

	private static string FormattedFigures(string operation, OperationFigures figures)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			"{0}: duration {1:F0} ms, peak working set {2:N0} bytes ({3:F1} MiB)",
			operation,
			figures.Duration.TotalMilliseconds,
			figures.PeakWorkingSetInBytes,
			figures.PeakWorkingSetInBytes / _BYTES_PER_MEBIBYTE);
	}

	private static void AssertTheCountersMatchWhatWasGenerated(PipelineHarness harness)
	{
		Assert.Equal(_EXPECTED_RETAINED_COUNT, harness.State.Findings.Length);
		Assert.True(harness.State.FindingsCapOutcome.IsTruncated);
		Assert.Equal(_TOTAL_RECOGNIZED_COUNT, harness.State.FindingsCapOutcome.TotalRecognizedCount);
		Assert.Equal(_EXPECTED_RETAINED_COUNT, harness.State.FindingsCapOutcome.RetainedCount);
		Assert.Equal(_EXPECTED_PER_FILE_CAP_TRIPPED_FILE_COUNT, harness.State.FindingsCapOutcome.PerFileCapTrippedFileCount);
		Assert.Equal(_EXPECTED_PER_FILE_OMITTED_COUNT, harness.State.FindingsCapOutcome.PerFileOmittedCount);
		Assert.True(harness.State.FindingsCapOutcome.GlobalCapTripped);
		Assert.Equal(_EXPECTED_GLOBAL_OMITTED_COUNT, harness.State.FindingsCapOutcome.GlobalOmittedCount);

		// >= 100,000 findings attempted is the volume proof's own floor, independent of the exact
		// shape generated above.
		Assert.True(_TOTAL_RECOGNIZED_COUNT >= 100_000);
	}

	private static void ForceFullCollection()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private void ReportFigures(long memoryBeforeAnalyze, long memoryAfterAnalyze, long retainedMemoryDelta)
	{
		_output.WriteLine("Findings-cap volume proof");
		_output.WriteLine(FormattedLine("Findings attempted (total recognized)", _TOTAL_RECOGNIZED_COUNT));
		_output.WriteLine(FormattedLine("Findings retained", _EXPECTED_RETAINED_COUNT));
		_output.WriteLine(FormattedLine("Per-file cap tripped file count", _EXPECTED_PER_FILE_CAP_TRIPPED_FILE_COUNT));
		_output.WriteLine(FormattedLine("Per-file cap omitted count", _EXPECTED_PER_FILE_OMITTED_COUNT));
		_output.WriteLine(FormattedLine("Global cap tripped", 1));
		_output.WriteLine(FormattedLine("Global cap omitted count", _EXPECTED_GLOBAL_OMITTED_COUNT));
		_output.WriteLine(FormattedLine("Managed memory before Analyze (bytes)", memoryBeforeAnalyze));
		_output.WriteLine(FormattedLine("Managed memory after Analyze (bytes)", memoryAfterAnalyze));
		_output.WriteLine(FormattedLine("Retained memory delta (bytes)", retainedMemoryDelta));
		_output.WriteLine(FormattedLine("Stated bound (bytes)", _MAXIMUM_RETAINED_MEMORY_DELTA_IN_BYTES));
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
				ManyUntemplatedConstructCalls(_MATCHES_OFFERED_PER_FILE));
		}
	}

	/// <summary>One file carrying the supplied number of untemplated-construct calls, one per line.</summary>
	private static string ManyUntemplatedConstructCalls(int count)
	{
		StringBuilder builder = new();

		builder.AppendLine("public sealed class ManyConstructs");
		builder.AppendLine("{");
		builder.AppendLine("\tvoid Build()");
		builder.AppendLine("\t{");

		for (int index = 0; index < count; index++)
		{
			builder.AppendLine($"\t\t{PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();");
		}

		builder.AppendLine("\t}");
		builder.AppendLine("}");

		return builder.ToString();
	}
}
