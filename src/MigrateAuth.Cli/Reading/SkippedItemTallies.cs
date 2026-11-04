using System.Collections.Immutable;

namespace MigrateAuth.Cli.Reading;

/// <summary>
/// THE OPERATIONS A FULL SET OF ELEVEN TALLIES SUPPORTS ONCE MORE THAN ONE STAGE CAN
/// PRODUCE ONE.
///
/// <see cref="Session.SessionState"/> holds two such sets - Scan's own and Analysis's own -
/// because <see cref="SkippedItemLedger"/> is cleared and re-accumulated once per stage rather than
/// once per session. Report never reads either set alone: it reads the two together, per category,
/// which is what <see cref="Merge"/> exists to do soundly rather than have <c>ReportStage</c>
/// improvise it.
/// </summary>
public static class SkippedItemTallies
{
	/// <summary>
	/// All eleven categories, each recording nothing. What a stage that has not yet run this session
	/// contributes to a merge, and what <see cref="Session.SessionState"/> starts a fresh session
	/// with - on the same terms <see cref="Reporting.ScanSummary.Empty"/> states for Section 2.
	/// </summary>
	public static ImmutableArray<SkippedItemTally> Empty { get; } =
	[
		.. SkippedItemCategories.ALL.Select(category => new SkippedItemTally
		{
			Category = category,
			Unit = SkippedItemCategories.UnitOf(category),
			Count = 0,
			EnumeratedPaths = []
		})
	];

	/// <summary>
	/// Two stages' tallies, combined per category: the counts add, and the enumerated paths are
	/// Scan's own followed by Analysis's own, truncated to <see cref="ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY"/>
	/// exactly as one ledger's own accumulation already truncates within a single run.
	///
	/// THE RESULT REPRODUCES A SINGLE, UNSPLIT LEDGER'S OWN TALLIES for an ordinary
	/// scan-then-analyze run, which is the property this type exists to hold: a resumed report's
	/// bytes must not move relative to a report a single process produced without ever persisting
	/// and reloading. That is exactly what a single ledger accumulating first Scan's items and then
	/// Analysis's, in that order, without ever clearing between them, would have produced.
	/// </summary>
	public static ImmutableArray<SkippedItemTally> Merge(
		ImmutableArray<SkippedItemTally> scanTallies,
		ImmutableArray<SkippedItemTally> analysisTallies)
	{
		Dictionary<SkippedItemCategory, SkippedItemTally> scanByCategory = scanTallies.ToDictionary(tally => tally.Category);
		Dictionary<SkippedItemCategory, SkippedItemTally> analysisByCategory = analysisTallies.ToDictionary(tally => tally.Category);

		return [.. SkippedItemCategories.ALL.Select(category => MergeOneCategory(category, scanByCategory, analysisByCategory))];
	}

	private static SkippedItemTally MergeOneCategory(
		SkippedItemCategory category,
		Dictionary<SkippedItemCategory, SkippedItemTally> scanByCategory,
		Dictionary<SkippedItemCategory, SkippedItemTally> analysisByCategory)
	{
		SkippedItemTally scanTally = scanByCategory.TryGetValue(category, out SkippedItemTally? scanFound) ? scanFound : EmptyTallyOf(category);
		SkippedItemTally analysisTally = analysisByCategory.TryGetValue(category, out SkippedItemTally? analysisFound)
			? analysisFound
			: EmptyTallyOf(category);

		ImmutableArray<Emission.RepositoryRelativePath> combinedPaths =
		[
			.. scanTally.EnumeratedPaths
				.Concat(analysisTally.EnumeratedPaths)
				.Take(ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY)
		];

		return new SkippedItemTally
		{
			Category = category,
			Unit = SkippedItemCategories.UnitOf(category),
			Count = scanTally.Count + analysisTally.Count,
			EnumeratedPaths = combinedPaths
		};
	}

	private static SkippedItemTally EmptyTallyOf(SkippedItemCategory category)
	{
		return new SkippedItemTally
		{
			Category = category,
			Unit = SkippedItemCategories.UnitOf(category),
			Count = 0,
			EnumeratedPaths = []
		};
	}
}
