using System.Globalization;

namespace MigrateAuth.WorstCase;

/// <summary>
/// Writes the worst-case tree, one file at a time in index order, holding nothing but the file being
/// written, so the run's own memory never depends on the tree's size. The same options give a
/// byte-identical tree.
/// </summary>
public static class WorstCaseTreeGenerator
{
	private const string _PROGRESS_FORMAT = "Wrote {0:N0} of {1:N0} files";
	private const int _MINIMUM_OVERSIZE_SLOT_SPACING = 2;

	/// <summary>Writes the tree and returns what was written.</summary>
	/// <exception cref="ArgumentException">The target lies inside a repository, or the options cannot describe a tree.</exception>
	/// <exception cref="IOException">The target exists and is not empty.</exception>
	public static WorstCaseTreeSummary Generate(WorstCaseTreeOptions options)
	{
		string root = Path.GetFullPath(options.OutputRoot);

		ValidateOptions(options);
		ValidateTarget(root);
		Directory.CreateDirectory(root);

		Dictionary<WorstCaseFileKind, long> filesByKind = [];
		long bytesWritten = 0;
		string? currentFolder = null;

		for (long index = 0; index < options.FileCount; index++)
		{
			WorstCaseFilePlan plan = WorstCaseTreePlanner.PlanFile(options, index);
			string fullPath = Path.Combine(root, plan.RelativePath.Replace('/', Path.DirectorySeparatorChar));
			string folder = Path.GetDirectoryName(fullPath)!;

			if (folder != currentFolder)
			{
				Directory.CreateDirectory(folder);
				currentFolder = folder;
			}

			byte[] bytes = WorstCaseFileContent.BytesOf(options, plan);

			File.WriteAllBytes(fullPath, bytes);
			bytesWritten += bytes.Length;
			filesByKind[plan.Kind] = filesByKind.GetValueOrDefault(plan.Kind) + 1;
			ReportProgressWhenDue(options, index + 1);
		}

		return new WorstCaseTreeSummary { FilesWritten = options.FileCount, BytesWritten = bytesWritten, FilesByKind = filesByKind };
	}

	private static void ReportProgressWhenDue(WorstCaseTreeOptions options, long filesWritten)
	{
		if (filesWritten % options.ProgressBlockSize == 0)
		{
			options.ReportProgress?.Invoke(string.Format(CultureInfo.InvariantCulture, _PROGRESS_FORMAT, filesWritten, options.FileCount));
		}
	}

	private static void ValidateOptions(WorstCaseTreeOptions options)
	{
		if (options.PerFileFindingsCap < 1 || options.GlobalFindingsCap < 1 || options.ProgressBlockSize < 1)
		{
			throw new ArgumentException("Both findings caps and the progress block size must be positive.", nameof(options));
		}

		if (options.FileCount <= options.HeavyFileCount)
		{
			throw new ArgumentException("The tree must hold more files than the heavy files that lead it.", nameof(options));
		}

		bool oversizeFits = options.OversizeFileCount == 0
			|| (options.OversizeFileCount > 0
				&& (options.FileCount - options.HeavyFileCount) / options.OversizeFileCount >= _MINIMUM_OVERSIZE_SLOT_SPACING);

		if (!oversizeFits)
		{
			throw new ArgumentException("The oversize files must fit, at least two slots apart, after the heavy files.", nameof(options));
		}
	}

	private static void ValidateTarget(string root)
	{
		if (RepositoryBoundary.IsInsideRepository(root))
		{
			throw new ArgumentException("The target lies inside a repository; the generated tree must be written outside it.", nameof(root));
		}

		if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
		{
			throw new IOException("The target folder is not empty.");
		}
	}
}
