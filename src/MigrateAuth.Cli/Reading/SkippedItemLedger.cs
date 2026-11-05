using System.Collections.Immutable;

using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Reading;

/// <summary>
/// One category's holes: how many, and up to a hundred of them by name.
///
/// The enumeration is bounded and the count never is. A single unreadable subtree can produce
/// hundreds of thousands of paths in a large walk - so the list is truncated and the truncation
/// is declared, while the count stays whole. A reader scanning skipped paths is looking for the
/// SHAPE of the hole, and a hundred shows that as well as a thousand does; the count is what
/// carries completeness.
/// </summary>
public sealed record SkippedItemTally
{
	/// <summary>Which of the eleven.</summary>
	public required SkippedItemCategory Category { get; init; }

	/// <summary>
	/// What <see cref="Count"/> counts. Directories for one category and files for the other
	/// ten, and the report may never add the two together.
	/// </summary>
	public required SkippedItemUnit Unit { get; init; }

	/// <summary>The full count. Never truncated.</summary>
	public required int Count { get; init; }

	/// <summary>At most a hundred, in the order they were observed.</summary>
	public required ImmutableArray<RepositoryRelativePath> EnumeratedPaths { get; init; }

	/// <summary>
	/// How many are not in <see cref="EnumeratedPaths"/>. It counts the ones past the hundredth
	/// and also the ones that could not be named at all - a path the platform refuses to express
	/// relative to the root is still a hole, and it is counted rather than dropped.
	/// </summary>
	public int OmittedPathCount => Count - EnumeratedPaths.Length;
}

/// <summary>
/// WHERE SKIPPED ITEMS ACCUMULATE UNTIL A REPORT EXISTS TO READ THEM.
///
/// The walk produces the skip data; the report that renders it comes later. This holds it in
/// between as a sink on the emission point rather than as a return value threaded out of the walk:
/// every skip then goes through <see cref="EmissionPoint.Emit"/> like everything else that leaves
/// this process, so the walk has no second path to output, and a skip observed deep inside a
/// directory-by-directory traversal does not have to be carried back up through the iterator to be
/// counted.
///
/// IT IS BOUNDED. Eleven categories, a hundred paths each, and integers - eleven hundred paths is
/// its ceiling no matter how large the repository or how much of it was unreadable. Nothing here
/// grows with the file count.
///
/// IT IS NOT A DESTINATION AND RENDERS NOTHING. It stores the typed path rather than text, so
/// no escaping happens here; it declares the destination that escapes nothing, and the sink
/// that eventually writes these paths escapes them for wherever it is writing.
///
/// THE ELEVEN CATEGORIES ARE NOT GUARANTEED DISJOINT PER FILE, AND A TALLY IS NOT A CENSUS OF
/// DISTINCT FILES. This is the component that sums them: a file can be recorded under
/// <see cref="SkippedItemCategory.MalformedMatch"/>
/// on one rule and separately under <see cref="SkippedItemCategory.MatchBoundExceeded"/> when a
/// different rule for the same file genuinely times out, since neither guards against the other.
/// <see cref="SkippedItemCategory.MalformedMatch"/> is a further departure from what "skipped"
/// suggests: unlike every other category here, a file counted under it is not necessarily empty
/// of findings - it drops one match or resumes past one stuck position, and keeps the rest. This
/// ledger's own sums (<see cref="SkippedFiles"/>, <see cref="Tallies"/>) do not attempt to
/// deduplicate across categories, and nothing downstream should read them as if they did.
/// </summary>
public sealed class SkippedItemLedger : IEmissionSink
{
	private readonly Dictionary<SkippedItemCategory, CategoryLedger> _ledgers;

	public SkippedItemLedger()
	{
		_ledgers = [];

		foreach (SkippedItemCategory category in SkippedItemCategories.ALL)
		{
			_ledgers[category] = new CategoryLedger();
		}
	}

	/// <inheritdoc />
	public EmissionDestination Destination => EmissionDestination.StructuredData;

	/// <summary>
	/// Files that were skipped, across the ten file-counted categories. It is deliberately NOT
	/// a total of everything skipped: <see cref="DirectoriesOfUnknownContents"/> is a different
	/// unit and there is no property here that adds the two, because the sum would be false
	/// reassurance.
	/// </summary>
	public int SkippedFiles => SumOf(SkippedItemUnit.Files);

	/// <summary>
	/// Directories whose contents were never enumerable. The hole each one represents is of
	/// UNKNOWN size - reporting it as one skipped item is accurate about the directory and says
	/// nothing at all about how many files it held, which is the honest position.
	/// </summary>
	public int DirectoriesOfUnknownContents => SumOf(SkippedItemUnit.Directories);

	/// <summary>Whether anything was skipped at all, in either unit.</summary>
	public bool RecordedAnything => SkippedFiles > 0 || DirectoriesOfUnknownContents > 0;

	/// <summary>
	/// Resets every category to nothing recorded, so that this ledger can be reused across
	/// stage runs without the second run's counts adding onto the first's.
	///
	/// EACH WORKFLOW STAGE THAT EMITS THROUGH THIS LEDGER CALLS IT AT THE START OF ITS OWN RUN, and
	/// snapshots <see cref="Tallies"/> into <see cref="Session.SessionState"/> at completion - Scan
	/// for its own tallies, Analyze for its own. Without a clear, a second run in the SAME process
	/// would double-count everything the first run already recorded, since nothing else ever removes
	/// an entry from a <see cref="CategoryLedger"/>.
	/// </summary>
	public void Clear()
	{
		foreach (CategoryLedger ledger in _ledgers.Values)
		{
			ledger.Clear();
		}
	}

	/// <inheritdoc />
	public void Receive(EmittedFact fact)
	{
		ArgumentNullException.ThrowIfNull(fact);

		if (fact.Category is EmissionCategory.SkippedItem
			&& fact.SkippedItem is SkippedItemCategory category
			&& _ledgers.TryGetValue(category, out CategoryLedger? ledger))
		{
			ledger.Record(fact.Path);
		}
	}

	/// <summary>One category's tally.</summary>
	public SkippedItemTally TallyOf(SkippedItemCategory category)
	{
		return _ledgers.TryGetValue(category, out CategoryLedger? ledger)
			? ledger.ToTally(category)
			: EmptyTally(category);
	}

	/// <summary>
	/// All eleven, in the closed list's own order, including the ones that recorded nothing. A
	/// category with a count of zero is information - it is what lets a report say that no file
	/// was undecodable rather than leave the reader to infer it from an absence.
	/// </summary>
	public ImmutableArray<SkippedItemTally> Tallies()
	{
		return [.. SkippedItemCategories.ALL.Select(TallyOf)];
	}

	private static SkippedItemTally EmptyTally(SkippedItemCategory category)
	{
		return new SkippedItemTally
		{
			Category = category,
			Unit = SkippedItemCategories.UnitOf(category),
			Count = 0,
			EnumeratedPaths = []
		};
	}

	private int SumOf(SkippedItemUnit unit)
	{
		return SkippedItemCategories.ALL
			.Where(category => SkippedItemCategories.UnitOf(category) == unit)
			.Sum(category => _ledgers[category].Count);
	}

	/// <summary>
	/// The count and the bounded path list for one category. The list stops growing at the
	/// ceiling while the count does not.
	/// </summary>
	private sealed class CategoryLedger
	{
		private readonly List<RepositoryRelativePath> _enumeratedPaths = [];

		internal int Count { get; private set; }

		internal void Clear()
		{
			Count = 0;
			_enumeratedPaths.Clear();
		}

		internal void Record(RepositoryRelativePath path)
		{
			Count++;

			if (path.IsDeclared && _enumeratedPaths.Count < ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY)
			{
				_enumeratedPaths.Add(path);
			}
		}

		internal SkippedItemTally ToTally(SkippedItemCategory category)
		{
			return new SkippedItemTally
			{
				Category = category,
				Unit = SkippedItemCategories.UnitOf(category),
				Count = Count,
				EnumeratedPaths = [.. _enumeratedPaths]
			};
		}
	}
}
