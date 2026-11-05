using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reporting;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE REPORT AT THE CAP, WITH THE FINDINGS SUMMARY TABLE IN IT.
///
/// The global findings cap is 50,000, so 50,000 findings is the largest report the tool can write. The
/// summary table adds one row per finding ahead of the per-finding sections, and the claim proved here is
/// the one the streaming design makes: the table is written row by row from the same
/// filtered sequence section 4 walks, so a report with the table still holds nothing and stays inside
/// the memory limit every other volume proof in this suite is held to - one gigabyte, sampled
/// as the peak working set of the process (<see cref="PeakWorkingSetSampler"/>).
///
/// THERE IS NO NUMERIC BOUND ON THE SIZE OF A REPORT FILE to hold this to: the memory
/// the writer needs is bounded and not the length of what it writes, and the length is what a person's disk and a
/// reader's patience decide. The size of the report and of the table inside it are printed with the
/// test output so that the figure is on the record.
///
/// RUN ALONE, like the other volume proofs, because the working set is process-wide.
/// </summary>
[Collection(SessionStateVolumeProofTests.COLLECTION_NAME)]
public sealed class ReportSummaryVolumeProofTests : IClassFixture<VolumeProofMemoryRelease>
{
	private const int _FINDING_COUNT = 50_000;
	private const long _MEMORY_LIMIT_IN_BYTES = 1024L * 1024 * 1024;
	private const double _BYTES_PER_MEBIBYTE = 1024.0 * 1024.0;
	private const string _PATH_FORMAT = "module-{0:D5}/source/file.cs";
	private const string _SUMMARY_ROW_PREFIX = "| ";
	private const int _FIRST_DIGIT_POSITION = 2;
	private const int _HIGH_CYCLE = 0;
	private const int _MEDIUM_CYCLE = 1;
	private const int _CYCLE_LENGTH = 3;

	private readonly ITestOutputHelper _output;

	public ReportSummaryVolumeProofTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void AReportOfTheFullFiftyThousandFindingsWithTheSummaryTableListsEveryOneAndStaysUnderOneGigabyte()
	{
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, _FINDING_COUNT);

		ReportContent content = ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			BuildFindings());
		CountingTextWriter writer = new();

		PeakWorkingSetSampler.ReleaseUnusedMemory();
		PeakWorkingSetSampler sampler = PeakWorkingSetSampler.Start();

		MarkdownReportRenderer.Render(writer, content, new UnwiredGenericRecommendationProvider());

		long peakWorkingSet = sampler.StopAndReadPeakInBytes();

		ReportFigures(writer, peakWorkingSet);

		Assert.Equal(_FINDING_COUNT, writer.SummaryRowCount);
		Assert.Equal(ThresholdFilter.CountReported(content.Findings, ConfidenceLevel.Low), writer.SummaryRowCount);
		Assert.True(peakWorkingSet < _MEMORY_LIMIT_IN_BYTES, "Rendering the report passed 1 GB of working set.");
	}

	private static ImmutableArray<Finding> BuildFindings()
	{
		return
		[
			.. Enumerable.Range(1, _FINDING_COUNT).Select(index => (index % _CYCLE_LENGTH) switch
			{
				_HIGH_CYCLE => BuildFinding(index, ConfidenceLevel.High, CredentialFlow.ClientCredentials, FlowDecision.Default),
				_MEDIUM_CYCLE => BuildFinding(index, ConfidenceLevel.Medium, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal),
				_ => BuildFinding(index, ConfidenceLevel.Low, null, null)
			})
		];
	}

	private static Finding BuildFinding(int index, ConfidenceLevel level, CredentialFlow? flow, FlowDecision? decision)
	{
		return ReportHarness.FindingAt(
			index,
			string.Format(CultureInfo.InvariantCulture, _PATH_FORMAT, index),
			index,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			level,
			flow: flow,
			flowDecision: decision,
			lowReasons: level == ConfidenceLevel.Low ? LowConfidenceReasons.CommentOrStringLiteral : LowConfidenceReasons.None);
	}

	private void ReportFigures(CountingTextWriter writer, long peakWorkingSet)
	{
		_output.WriteLine("Report volume proof with the findings summary table (largest supported size)");
		_output.WriteLine(FormattedLine("Findings reported", _FINDING_COUNT));
		_output.WriteLine(FormattedLine("Summary table rows", writer.SummaryRowCount));
		_output.WriteLine(FormattedLine("Markdown report size (bytes)", writer.TotalBytes));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Markdown report size (MiB): {0:F2}", writer.TotalBytes / _BYTES_PER_MEBIBYTE));
		_output.WriteLine(FormattedLine("Summary table size (bytes)", writer.SummaryBytes));
		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Summary table size (MiB): {0:F2}", writer.SummaryBytes / _BYTES_PER_MEBIBYTE));
		_output.WriteLine(FormattedLine("Peak working set while rendering (bytes)", peakWorkingSet));
		_output.WriteLine(FormattedLine("Limit (1 GB taken as 1 GiB, bytes)", _MEMORY_LIMIT_IN_BYTES));
	}

	private static string FormattedLine(string label, long value)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}: {1:N0}", label, value);
	}

	/// <summary>
	/// A sink that keeps nothing: it counts the bytes the report is made of, the bytes of the summary
	/// ahead of section 1, and the summary's rows. A writer that kept the text would measure the
	/// accumulation the renderer is built to avoid.
	/// </summary>
	private sealed class CountingTextWriter : TextWriter
	{
		private bool _summaryEnded;

		public override Encoding Encoding => Encoding.UTF8;

		internal long TotalBytes { get; private set; }

		internal long SummaryBytes { get; private set; }

		internal int SummaryRowCount { get; private set; }

		public override void Write(char value)
		{
			TotalBytes += Encoding.UTF8.GetByteCount(value.ToString());
		}

		public override void Write(string? value)
		{
			TotalBytes += value is null ? 0 : Encoding.UTF8.GetByteCount(value);
		}

		public override void WriteLine(string? value)
		{
			if (!_summaryEnded)
			{
				CountSummaryLine(value);
			}

			Write(value);
			Write(NewLine);
		}

		private void CountSummaryLine(string? line)
		{
			if (string.Equals(line, MarkdownReportRenderer.COVERAGE_HEADING, StringComparison.Ordinal))
			{
				_summaryEnded = true;
				SummaryBytes = TotalBytes;
			}
			else if (line is not null
				&& line.StartsWith(_SUMMARY_ROW_PREFIX, StringComparison.Ordinal)
				&& char.IsAsciiDigit(line[_FIRST_DIGIT_POSITION]))
			{
				SummaryRowCount++;
			}
		}
	}
}
