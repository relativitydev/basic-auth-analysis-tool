using System.Collections.Immutable;

using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Reading;

/// <summary>
/// THE FOLDERS THE WALK CHOSE NOT TO ENTER, AS A COUNT AND A BOUNDED LIST.
///
/// An excluded folder is a policy of the tool and not a hole it could not avoid, so it is kept
/// apart from the skipped items and never added to their counts. What it needs is to be visible:
/// a reader who finds no finding under a folder named like a build output has to be able to see
/// that the folder was never read, without opening the source of the tool.
///
/// THE COUNT IS WHOLE AND THE LIST IS BOUNDED, on the same terms and at the same ceiling as
/// <see cref="SkippedItemTally"/>: a repository may hold thousands of such folders and the shape of
/// the exclusion is visible in a hundred of them. The list is ordered ordinally by path so that the
/// same repository gives the same list on every platform, whatever order its filesystem enumerates in.
/// </summary>
public sealed record ExcludedFolderSummary
{
	/// <summary>A walk that excluded nothing, and the summary of a session that has not scanned.</summary>
	public static ExcludedFolderSummary None { get; } = new() { OccurrenceCount = 0, Paths = [] };

	/// <summary>How many excluded folders the walk met. Never truncated.</summary>
	public required int OccurrenceCount { get; init; }

	/// <summary>At most <see cref="ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY"/>, ordered ordinally.</summary>
	public required ImmutableArray<RepositoryRelativePath> Paths { get; init; }

	/// <summary>
	/// Value equality over the count and the listed paths in order. A record compares an
	/// <see cref="ImmutableArray{T}"/> by the array it wraps, which would make two summaries of the same
	/// folders unequal whenever they were built separately.
	/// </summary>
	public bool Equals(ExcludedFolderSummary? other)
	{
		return other is not null
			&& OccurrenceCount == other.OccurrenceCount
			&& Paths.AsSpan().SequenceEqual(other.Paths.AsSpan());
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(OccurrenceCount, Paths.IsDefaultOrEmpty ? 0 : Paths.Length);
	}

	/// <summary>How many are counted and not listed, including a folder whose path could not be expressed relative to the root.</summary>
	public int OmittedPathCount => OccurrenceCount - Paths.Length;

	/// <summary>
	/// The summary of a walk: the whole count, and the named folders ordered and cut to the ceiling.
	/// A folder that could not be named counts and is not listed.
	/// </summary>
	public static ExcludedFolderSummary From(int occurrenceCount, IEnumerable<RepositoryRelativePath> namedFolders)
	{
		ArgumentNullException.ThrowIfNull(namedFolders);

		return new ExcludedFolderSummary
		{
			OccurrenceCount = occurrenceCount,
			Paths =
			[
				.. namedFolders
					.OrderBy(path => path.Value, StringComparer.Ordinal)
					.Take(ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY)
			]
		};
	}
}
