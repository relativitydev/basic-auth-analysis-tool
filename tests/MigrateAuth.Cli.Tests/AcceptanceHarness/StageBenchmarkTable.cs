using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>One stage's measured cost, and what it was measured over.</summary>
internal sealed record StageTiming
{
	/// <summary>The stage's own name, in the user's vocabulary.</summary>
	internal required string StageName { get; init; }

	/// <summary>How long its single <c>Run</c> call took, wall clock.</summary>
	internal required TimeSpan Elapsed { get; init; }

	/// <summary>How many items it worked through, which differs per stage and is stated per stage.</summary>
	internal required int ItemCount { get; init; }

	/// <summary>What those items were. A file, a candidate and a finding are not comparable units.</summary>
	internal required string ItemLabel { get; init; }
}

/// <summary>One shape of repository, and the four timings measured over it.</summary>
internal sealed record BenchmarkShape
{
	/// <summary>How the shape is named in the table.</summary>
	internal required string Name { get; init; }

	/// <summary>What was planted, and why that shape was chosen.</summary>
	internal required string Description { get; init; }

	/// <summary>The four stages in run order.</summary>
	internal required ImmutableArray<StageTiming> Timings { get; init; }
}

/// <summary>
/// THE PER-STAGE DURATION DECOMPOSITION, RENDERED SO A HUMAN CAN READ IT.
///
/// It is not a reference benchmark, which wants a fixed reference rig and a full volume band. It
/// measures the four stage boundaries on a tree sized so the numbers mean something and the suite
/// still finishes in seconds, and it says so in its own header.
///
/// THE MACHINE IS DECLARED FROM WHAT THE RUNNING PROCESS CAN VOUCH FOR: the processor count, the
/// architecture, the operating system, the framework, the build configuration and the memory the
/// runtime sees. The processor model is not reachable from this target framework without an extra
/// dependency, so whoever records a figure records it beside the table.
/// </summary>
internal static class StageBenchmarkTable
{
	/// <summary>The file the rendered table is left in, under the git-ignored artifacts folder.</summary>
	internal const string OUTPUT_FILE_NAME = "per-stage-benchmarks.md";

	/// <summary>The git-ignored folder the publish output already uses, so nothing reaches a commit.</summary>
	internal const string OUTPUT_FOLDER_NAME = "artifacts";

	/// <summary>The marker that identifies the repository root when walking up from the test binary.</summary>
	internal const string REPOSITORY_ROOT_MARKER_FILE_NAME = "migrate-auth.sln";

	private const string _TITLE = "# Per-stage durations";
	private const string _HEADER_ROW = "| Stage | Worked over | Duration (ms) | Share of the four |";
	private const string _SEPARATOR_ROW = "|---|---|---:|---:|";
	/// <summary>
	/// Three decimals, because <c>suggest</c> over a small finding set is a fraction of a millisecond
	/// and a row reading 0.0 invites the reader to think it was not measured.
	/// </summary>
	private const string _ROW_FORMAT = "| {0} | {1:N0} {2} | {3:N3} | {4:F1} % |";

	private const string _TOTAL_ROW_FORMAT = "| **four stages** | | **{0:N3}** | 100.0 % |";
	private const string _SHAPE_HEADING_FORMAT = "## {0}";
	private const string _BUILD_CONFIGURATION =
#if DEBUG
		"Debug";
#else
		"Release";
#endif

	private const int _BYTES_PER_GIBIBYTE = 1024 * 1024 * 1024;
	private const double _PERCENT = 100d;

	/// <summary>
	/// The whole document, in Markdown: what was measured on, then one table per shape.
	/// </summary>
	internal static string Render(IEnumerable<BenchmarkShape> shapes, DateTimeOffset measuredAt)
	{
		StringBuilder document = new();

		document.AppendLine(_TITLE);
		document.AppendLine();
		document.AppendLine(DescribeTheRun(measuredAt));

		foreach (BenchmarkShape shape in shapes)
		{
			AppendShape(document, shape);
		}

		return document.ToString();
	}

	/// <summary>
	/// Leaves the document beside the publish output, where the repository already ignores
	/// everything. It returns the path it wrote, or null where the repository root is not above the
	/// test binary - a test must not fail because someone ran it from a copied output folder.
	/// </summary>
	internal static string? TryWrite(string document)
	{
		string? repositoryRootPath = FindRepositoryRoot();
		string? writtenPath = null;

		if (repositoryRootPath is not null)
		{
			string outputFolderPath = Path.Combine(repositoryRootPath, OUTPUT_FOLDER_NAME);

			Directory.CreateDirectory(outputFolderPath);
			writtenPath = Path.Combine(outputFolderPath, OUTPUT_FILE_NAME);

			File.WriteAllText(writtenPath, document);
		}

		return writtenPath;
	}

	/// <summary>Everything about this run a reader needs before believing any number in it.</summary>
	internal static string DescribeTheRun(DateTimeOffset measuredAt)
	{
		double availableMemoryInGibibytes =
			(double)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / _BYTES_PER_GIBIBYTE;

		StringBuilder description = new();

		description.AppendLine(CultureInfo.InvariantCulture, $"- Measured: {measuredAt:yyyy-MM-dd HH:mm:ss zzz}");
		description.AppendLine(CultureInfo.InvariantCulture, $"- Operating system: {RuntimeInformation.OSDescription}");
		description.AppendLine(CultureInfo.InvariantCulture, $"- Architecture: {RuntimeInformation.ProcessArchitecture}");
		description.AppendLine(CultureInfo.InvariantCulture, $"- Logical processors: {Environment.ProcessorCount}");
		description.AppendLine(
			CultureInfo.InvariantCulture,
			$"- Memory visible to the runtime: {availableMemoryInGibibytes:F1} GiB");
		description.AppendLine(CultureInfo.InvariantCulture, $"- Framework: {RuntimeInformation.FrameworkDescription}");
		description.AppendLine(CultureInfo.InvariantCulture, $"- Build configuration: {_BUILD_CONFIGURATION}");
		description.AppendLine("- Single thread, warm file cache, local NVMe storage, JIT rather than Native AOT.");
		description.AppendLine(
			"- One discarded run precedes the measurement, so no shape carries the other's just-in-time cost.");
		description.AppendLine(
			"- The processor model is not reachable from this target framework and is recorded out of band.");

		return description.ToString();
	}

	private static void AppendShape(StringBuilder document, BenchmarkShape shape)
	{
		double totalMilliseconds = shape.Timings.Sum(timing => timing.Elapsed.TotalMilliseconds);

		document.AppendLine(string.Format(CultureInfo.InvariantCulture, _SHAPE_HEADING_FORMAT, shape.Name));
		document.AppendLine();
		document.AppendLine(shape.Description);
		document.AppendLine();
		document.AppendLine(_HEADER_ROW);
		document.AppendLine(_SEPARATOR_ROW);

		foreach (StageTiming timing in shape.Timings)
		{
			document.AppendLine(RenderRow(timing, totalMilliseconds));
		}

		document.AppendLine(string.Format(CultureInfo.InvariantCulture, _TOTAL_ROW_FORMAT, totalMilliseconds));
		document.AppendLine();
	}

	private static string RenderRow(StageTiming timing, double totalMilliseconds)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			_ROW_FORMAT,
			timing.StageName,
			timing.ItemCount,
			timing.ItemLabel,
			timing.Elapsed.TotalMilliseconds,
			timing.Elapsed.TotalMilliseconds / Math.Max(totalMilliseconds, double.Epsilon) * _PERCENT);
	}

	private static string? FindRepositoryRoot()
	{
		DirectoryInfo? directory = new(AppContext.BaseDirectory);

		while (directory is not null
			&& !File.Exists(Path.Combine(directory.FullName, REPOSITORY_ROOT_MARKER_FILE_NAME)))
		{
			directory = directory.Parent;
		}

		return directory?.FullName;
	}
}
