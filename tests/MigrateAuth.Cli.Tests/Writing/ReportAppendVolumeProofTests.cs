using System.Globalization;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.AcceptanceHarness;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// THE REPLACEMENT AND THE PACK AT THE SIZE OF A REPORT PRODUCED AT THE FINDINGS CAP.
///
/// The global findings cap is 50,000, and a report of that many findings is about 37 MB. The claim proved here is the one
/// the streaming design makes: <see cref="AtomicArtifactWriter.AppendToReport"/> copies that report through a fixed buffer into
/// the temporary and hashes it on the way, and <see cref="AtomicArtifactWriter.WritePromptPackStreaming"/> writes a body of the
/// same order as it is produced, so neither holds the file or the body and both stay inside the memory limit every
/// other volume proof in this suite is held to: one gigabyte, sampled as the peak working set. The report and the body are
/// synthetic and of the order of the real ones; the content of the real pack at the cap belongs to the pack content tests.
/// Each operation runs in the existing volume-proof child process (<see cref="VolumeProofChildProcess"/>), so the working
/// set is the operation's and not what earlier tests left resident. NO TIME IS ASSERTED: there is no time bound here,
/// and the child reports a zero duration.
///
/// RUN ALONE, like the other volume proofs, because the working set is process-wide and the child process shares the machine.
/// </summary>
[Collection(SessionStateVolumeProofTests.COLLECTION_NAME)]
public sealed class ReportAppendVolumeProofTests : IClassFixture<VolumeProofMemoryRelease>
{
	internal const string APPEND_OPERATION = "report-append";
	internal const string PACK_STREAMING_OPERATION = "pack-streaming";

	private const int _FINDING_COUNT = 50_000;
	private const long _MEMORY_LIMIT_IN_BYTES = 1024L * 1024 * 1024;
	private const long _MINIMUM_REPORT_BYTES = 37_000_000;
	private const int _ENTRY_FILLER_LENGTH = 740;
	private const int _APPENDED_SECTION_ENTRIES = 1_000;
	private const double _BYTES_PER_MEBIBYTE = 1024.0 * 1024.0;
	private const string _ENTRY_HEADING_FORMAT = "### Finding {0:D5}\n";
	private const string _ENTRY_FILLER_CHARACTER = "x";
	private const string _SECTION_HEADING = "## Appended section\n";

	private static readonly DateOnly _artifactDate = new(2026, 9, 18);

	private readonly ITestOutputHelper _output;

	public ReportAppendVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void AppendingToAReportAtTheFindingsCapStreamsAndStaysUnderOneGigabyte()
	{
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, _FINDING_COUNT);

		(OperationFigures append, IReadOnlyList<string> details) = VolumeProofChildProcess.RunAndReadDetails(APPEND_OPERATION);

		ReportFigures("Report append volume proof (findings cap)", details, append);

		Assert.True(append.PeakWorkingSetInBytes < _MEMORY_LIMIT_IN_BYTES, "Appending to the report passed 1 GB of working set.");
	}

	[Fact]
	public void WritingAPromptPackBodyOfTheOrderOfAReportAtTheFindingsCapStreamsAndStaysUnderOneGigabyte()
	{
		(OperationFigures pack, IReadOnlyList<string> details) = VolumeProofChildProcess.RunAndReadDetails(PACK_STREAMING_OPERATION);

		ReportFigures("Prompt pack streaming volume proof (findings cap)", details, pack);

		Assert.True(pack.PeakWorkingSetInBytes < _MEMORY_LIMIT_IN_BYTES, "Writing the prompt pack passed 1 GB of working set.");
	}

	/// <summary>The append, as the child process runs it: the report is written first, then the append alone is measured.</summary>
	internal static OperationFigures AppendToTheLargestReport(string[] arguments)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ReportWriteHolder holder = new();
		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder, holder);
		ArtifactWriteResult written = writer.WriteShareableArtifactStreaming(
			OutputArtifactKind.MarkdownReport,
			_artifactDate,
			sink => WriteEntries(sink, _FINDING_COUNT));
		string largestPath = destinationFolder.Combine(written.FileNameWritten);
		long lengthBefore = new FileInfo(largestPath).Length;

		Assert.True(lengthBefore >= _MINIMUM_REPORT_BYTES);
		Assert.Equal(lengthBefore, holder.Current!.Length);

		PeakWorkingSetSampler.ReleaseUnusedMemory();

		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();
		long workingSetBefore = Environment.WorkingSet;

		ReportAppendResult result = writer.AppendToReport(
			holder,
			sink =>
			{
				sink.Write(_SECTION_HEADING);
				WriteEntries(sink, _APPENDED_SECTION_ENTRIES);
			});

		OperationFigures figures = new(TimeSpan.Zero, sampler.StopAndReadPeakInBytes(), workingSetBefore);
		long lengthAfter = new FileInfo(largestPath).Length;

		Assert.True(result.Appended);
		Assert.True(lengthAfter > lengthBefore);
		Assert.Equal(lengthAfter, holder.Current.Length);

		Console.WriteLine(FormattedLine("Report size before the append (bytes)", lengthBefore));
		Console.WriteLine(FormattedLine("Report size after the append (bytes)", lengthAfter));
		Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Report size before the append (MiB): {0:F2}", lengthBefore / _BYTES_PER_MEBIBYTE));

		return figures;
	}

	/// <summary>The pack write, as the child process runs it: a synthetic body of the order of the largest report, written as it is produced.</summary>
	internal static OperationFigures WriteTheLargestPackBody(string[] arguments)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		AtomicArtifactWriter writer = ReportAppendFixture.BuildWriter(repositoryFolder, destinationFolder);

		PeakWorkingSetSampler.ReleaseUnusedMemory();

		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();
		long workingSetBefore = Environment.WorkingSet;

		ArtifactWriteResult result = writer.WritePromptPackStreaming(
			AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM,
			sink => WriteEntries(sink, _FINDING_COUNT));

		OperationFigures figures = new(TimeSpan.Zero, sampler.StopAndReadPeakInBytes(), workingSetBefore);
		long length = new FileInfo(destinationFolder.Combine(result.FileNameWritten)).Length;

		Assert.True(result.Wrote);
		Assert.True(length >= _MINIMUM_REPORT_BYTES);

		Console.WriteLine(FormattedLine("Prompt pack size (bytes)", length));
		Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Prompt pack size (MiB): {0:F2}", length / _BYTES_PER_MEBIBYTE));

		return figures;
	}

	private static void WriteEntries(TextWriter sink, int count)
	{
		string filler = new(_ENTRY_FILLER_CHARACTER[0], _ENTRY_FILLER_LENGTH);

		for (int index = 1; index <= count; index++)
		{
			sink.Write(string.Format(CultureInfo.InvariantCulture, _ENTRY_HEADING_FORMAT, index));
			sink.Write(filler);
			sink.Write('\n');
		}
	}

	private void ReportFigures(string title, IReadOnlyList<string> details, OperationFigures figures)
	{
		_output.WriteLine(title);
		_output.WriteLine(FormattedLine("Findings at the cap", _FINDING_COUNT));

		foreach (string detail in details)
		{
			_output.WriteLine(detail);
		}

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			"Operation (child process): peak working set {0:N0} bytes ({1:F1} MiB), working set before {2:N0} bytes",
			figures.PeakWorkingSetInBytes,
			figures.PeakWorkingSetInBytes / _BYTES_PER_MEBIBYTE,
			figures.WorkingSetBeforeInBytes));
		_output.WriteLine(FormattedLine("Limit (1 GB taken as 1 GiB, bytes)", _MEMORY_LIMIT_IN_BYTES));
	}

	private static string FormattedLine(string label, long value)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}: {1:N0}", label, value);
	}
}
