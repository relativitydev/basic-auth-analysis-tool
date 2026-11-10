using System.Globalization;

namespace MigrateAuth.WorstCase;

/// <summary>One planned file: where it goes and what it is for.</summary>
public readonly record struct WorstCaseFilePlan(long Index, string RelativePath, WorstCaseFileKind Kind);

/// <summary>
/// The shape of the worst-case tree, as a pure function of the options and a file index.
///
/// Layout: three folder levels and a file, a hundred files to a leaf folder and a hundred leaf
/// folders to a middle folder, every name zero-padded to a fixed width. Fixed-width names make the
/// ordinal path order equal the index order at any file count, which is what places the heavy files
/// first in the order the run admits findings in.
///
/// Mix, in basis points of the files after the heavy ones (a file's kind is decided by a seeded hash
/// of its index): 5% files with a few High constructs, 1% undecodable, 0.5% with an over-long line,
/// 10% of a type no analyzer claims, the rest ordinary C#. The first file after the heavy ones always
/// holds a High construct, so a tree larger than the heavy files always has one past the global cap.
/// </summary>
public static class WorstCaseTreePlanner
{
	internal const int FILES_PER_LEAF_FOLDER = 100;
	internal const int LEAF_FOLDERS_PER_MIDDLE_FOLDER = 100;
	internal const int BASIS_POINT_DENOMINATOR = 10_000;
	internal const int STREAM_KIND = 1;

	private const int _HIGH_TAIL_THRESHOLD = 500;
	private const int _UNDECODABLE_THRESHOLD = 600;
	private const int _OVER_LONG_LINE_THRESHOLD = 650;
	private const int _NON_CANDIDATE_THRESHOLD = 1_650;
	private const string _TOP_FOLDER_FORMAT = "t{0:D3}";
	private const string _MIDDLE_FOLDER_FORMAT = "m{0:D2}";
	private const string _LEAF_FOLDER_FORMAT = "s{0:D2}";
	private const string _FILE_FORMAT = "f{0:D7}{1}";
	private const string _PATH_SEPARATOR = "/";
	private const string _CANDIDATE_EXTENSION = ".cs";
	private const int _NON_CANDIDATE_EXTENSION_COUNT = 4;
	private const int _STREAM_EXTENSION = 2;

	private static readonly string[] _NonCandidateExtensions = [".txt", ".md", ".json", ".xml"];

	/// <summary>The plan for one file of a tree described by the options.</summary>
	public static WorstCaseFilePlan PlanFile(WorstCaseTreeOptions options, long index)
	{
		WorstCaseFileKind kind = KindOf(options, index);
		string extension = kind == WorstCaseFileKind.NonCandidate
			? _NonCandidateExtensions[DeterministicRandom.Below(_NON_CANDIDATE_EXTENSION_COUNT, options.Seed, index, _STREAM_EXTENSION)]
			: _CANDIDATE_EXTENSION;

		return new WorstCaseFilePlan(index, RelativePathOf(index, extension), kind);
	}

	/// <summary>The kind of the file at an index.</summary>
	public static WorstCaseFileKind KindOf(WorstCaseTreeOptions options, long index)
	{
		long afterHeavy = index - options.HeavyFileCount;
		WorstCaseFileKind kind;

		if (afterHeavy < 0)
		{
			kind = WorstCaseFileKind.Heavy;
		}
		else if (IsOversizeSlot(options, afterHeavy))
		{
			kind = WorstCaseFileKind.Oversize;
		}
		else if (afterHeavy == 0)
		{
			kind = WorstCaseFileKind.HighTail;
		}
		else
		{
			kind = KindByBasisPoints(DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, STREAM_KIND));
		}

		return kind;
	}

	private static WorstCaseFileKind KindByBasisPoints(int basisPoints)
	{
		return basisPoints switch
		{
			< _HIGH_TAIL_THRESHOLD => WorstCaseFileKind.HighTail,
			< _UNDECODABLE_THRESHOLD => WorstCaseFileKind.Undecodable,
			< _OVER_LONG_LINE_THRESHOLD => WorstCaseFileKind.OverLongLine,
			< _NON_CANDIDATE_THRESHOLD => WorstCaseFileKind.NonCandidate,
			_ => WorstCaseFileKind.Filler
		};
	}

	/// <summary>Oversize files sit at evenly spaced slots after the heavy files, never the first one, so their count is exact.</summary>
	private static bool IsOversizeSlot(WorstCaseTreeOptions options, long afterHeavy)
	{
		bool isSlot = false;

		if (options.OversizeFileCount > 0)
		{
			long stride = (options.FileCount - options.HeavyFileCount) / options.OversizeFileCount;

			isSlot = stride > 1
				&& afterHeavy / stride < options.OversizeFileCount
				&& afterHeavy % stride == stride / 2;
		}

		return isSlot;
	}

	private static string RelativePathOf(long index, string extension)
	{
		long leaf = index / FILES_PER_LEAF_FOLDER;
		long middle = leaf / LEAF_FOLDERS_PER_MIDDLE_FOLDER;
		long top = middle / LEAF_FOLDERS_PER_MIDDLE_FOLDER;

		return string.Join(
			_PATH_SEPARATOR,
			string.Format(CultureInfo.InvariantCulture, _TOP_FOLDER_FORMAT, top),
			string.Format(CultureInfo.InvariantCulture, _MIDDLE_FOLDER_FORMAT, middle % LEAF_FOLDERS_PER_MIDDLE_FOLDER),
			string.Format(CultureInfo.InvariantCulture, _LEAF_FOLDER_FORMAT, leaf % LEAF_FOLDERS_PER_MIDDLE_FOLDER),
			string.Format(CultureInfo.InvariantCulture, _FILE_FORMAT, index, extension));
	}
}
