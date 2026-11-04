using System.Collections.Immutable;

using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Reading;

/// <summary>
/// THE FILES THAT WERE READ AS A LEGACY CODE PAGE, AS A COUNT AND A BOUNDED LIST.
///
/// A file read as Windows-1252 was decoded by the last step of the ladder, which is a guess that no
/// byte of the file confirms. The guess is therefore never silent: the report and the data artifact
/// say how many files it applied to and name them, so that a reader who finds an odd character in a
/// finding's line can see the file was read that way without opening the source of the tool.
///
/// Per file, bounded: a per-file record is refused because the one term that grows with the
/// repository must not be retained, so the whole count is kept and the names are cut to
/// <see cref="ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY"/>, ordered ordinally, on the
/// terms an excluded folder is listed. They are not skipped items: the file was read.
/// </summary>
public sealed record LegacyEncodedFileSummary
{
	/// <summary>A scan that read no file as a legacy code page, and the summary of a session that has not scanned.</summary>
	public static LegacyEncodedFileSummary None { get; } = new() { OccurrenceCount = 0, Paths = [] };

	/// <summary>How many files were read as a legacy code page. Never truncated.</summary>
	public required int OccurrenceCount { get; init; }

	/// <summary>At most <see cref="ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY"/>, ordered ordinally.</summary>
	public required ImmutableArray<RepositoryRelativePath> Paths { get; init; }

	/// <summary>How many are counted and not listed.</summary>
	public int OmittedPathCount => OccurrenceCount - Paths.Length;

	/// <summary>
	/// Value equality over the count and the listed paths in order. A record compares an
	/// <see cref="ImmutableArray{T}"/> by the array it wraps, which would make two summaries of the
	/// same files unequal whenever they were built separately.
	/// </summary>
	public bool Equals(LegacyEncodedFileSummary? other)
	{
		return other is not null
			&& OccurrenceCount == other.OccurrenceCount
			&& Paths.AsSpan().SequenceEqual(other.Paths.AsSpan());
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(OccurrenceCount, Paths.IsDefaultOrEmpty ? 0 : Paths.Length);
	}
}

/// <summary>
/// The running count of files read as a legacy code page, which keeps only the first
/// <see cref="ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY"/> names in ordinal order, so that
/// a repository of half a million such files holds a hundred names and not half a million.
/// </summary>
public sealed class LegacyEncodedFileTally
{
	private readonly SortedSet<RepositoryRelativePath> _namedPaths = new(PathOrdinalComparer.Instance);

	private int _occurrenceCount;

	/// <summary>Counts one file, and names it if it is among the first by ordinal order.</summary>
	public void Record(RepositoryRelativePath path)
	{
		_occurrenceCount++;
		_namedPaths.Add(path);

		if (_namedPaths.Count > ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY && _namedPaths.Max is RepositoryRelativePath last)
		{
			_namedPaths.Remove(last);
		}
	}

	/// <summary>The count so far and the names kept, as a value.</summary>
	public LegacyEncodedFileSummary ToSummary()
	{
		return new LegacyEncodedFileSummary { OccurrenceCount = _occurrenceCount, Paths = [.. _namedPaths] };
	}

	private sealed class PathOrdinalComparer : IComparer<RepositoryRelativePath>
	{
		internal static PathOrdinalComparer Instance { get; } = new();

		public int Compare(RepositoryRelativePath left, RepositoryRelativePath right)
		{
			return string.CompareOrdinal(left.Value, right.Value);
		}
	}
}
