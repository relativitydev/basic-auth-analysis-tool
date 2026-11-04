using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// THE CLOSED LIST, THE TWO UNITS THAT MUST NOT BE ADDED, AND THE BOUND ON THE ENUMERATION.
/// </summary>
public sealed class SkippedItemLedgerTests
{
	private const int _PAST_THE_BOUND = ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY + 37;
	private const int _CLOSED_LIST_LENGTH = 11;

	/// <summary>
	/// The list is closed at eleven categories, <see cref="SkippedItemCategory.MatchBoundExceeded"/> and
	/// <see cref="SkippedItemCategory.MalformedMatch"/> among them - the same engine raising both for the same
	/// reason, a rule's own match that cannot be trusted.
	/// </summary>
	[Fact]
	public void TheEnumerationIsExactlyElevenAndTheyAreTheElevenTheDesignNames()
	{
		Assert.Equal(_CLOSED_LIST_LENGTH, SkippedItemCategories.ALL.Length);
		Assert.Equal(
			[
				SkippedItemCategory.UnreadableFile,
				SkippedItemCategory.UnlistableDirectory,
				SkippedItemCategory.LinkNotFollowed,
				SkippedItemCategory.Undecodable,
				SkippedItemCategory.OversizeFile,
				SkippedItemCategory.OverLongLine,
				SkippedItemCategory.PathTooLong,
				SkippedItemCategory.LexicalAnomaly,
				SkippedItemCategory.MatchBoundExceeded,
				SkippedItemCategory.MalformedMatch,
				SkippedItemCategory.MissingCandidate
			],
			SkippedItemCategories.ALL);
	}

	/// <summary>
	/// Eleven and a twelfth value that is the absence of a category rather than one of them. If a
	/// later change adds a real twelfth, this is the test that says so out loud.
	/// </summary>
	[Fact]
	public void NothingHasAddedATwelfthCategory()
	{
		Assert.Equal(SkippedItemCategories.ALL.Length + 1, Enum.GetValues<SkippedItemCategory>().Length);
		Assert.Contains(SkippedItemCategory.None, Enum.GetValues<SkippedItemCategory>());
	}

	[Fact]
	public void OnlyTheUnlistableDirectoryCountsDirectories()
	{
		foreach (SkippedItemCategory category in SkippedItemCategories.ALL)
		{
			Assert.Equal(
				category is SkippedItemCategory.UnlistableDirectory ? SkippedItemUnit.Directories : SkippedItemUnit.Files,
				SkippedItemCategories.UnitOf(category));
		}
	}

	/// <summary>
	/// The two totals are reported apart and there is no property that adds them. A summary that
	/// said "three items skipped" where one of them held four thousand unreachable files would
	/// understate the hole by three orders of magnitude.
	/// </summary>
	[Fact]
	public void TheTwoUnitsAreCountedApart()
	{
		SkippedItemLedger ledger = new();
		EmissionPoint emissionPoint = new([ledger]);

		emissionPoint.Emit(Skip(SkippedItemCategory.UnreadableFile));
		emissionPoint.Emit(Skip(SkippedItemCategory.OversizeFile));
		emissionPoint.Emit(Skip(SkippedItemCategory.UnlistableDirectory));

		Assert.Equal(2, ledger.SkippedFiles);
		Assert.Equal(1, ledger.DirectoriesOfUnknownContents);
		Assert.DoesNotContain(
			typeof(SkippedItemLedger).GetProperties(),
			property => property.Name.Contains("Total", StringComparison.Ordinal));
	}

	[Fact]
	public void TheCountIsNeverTruncatedAndTheEnumerationAlwaysIs()
	{
		SkippedItemLedger ledger = new();
		EmissionPoint emissionPoint = new([ledger]);

		for (int index = 0; index < _PAST_THE_BOUND; index++)
		{
			emissionPoint.Emit(Skip(SkippedItemCategory.UnreadableFile, $"folder/file-{index}.cs"));
		}

		SkippedItemTally tally = ledger.TallyOf(SkippedItemCategory.UnreadableFile);

		Assert.Equal(_PAST_THE_BOUND, tally.Count);
		Assert.Equal(ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY, tally.EnumeratedPaths.Length);
		Assert.Equal(_PAST_THE_BOUND - ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY, tally.OmittedPathCount);
	}

	[Fact]
	public void EveryCategoryIsTalliedIncludingTheOnesThatRecordedNothing()
	{
		SkippedItemLedger ledger = new();

		Assert.Equal(SkippedItemCategories.ALL, ledger.Tallies().Select(tally => tally.Category));
		Assert.All(ledger.Tallies(), tally => Assert.Equal(0, tally.Count));
		Assert.False(ledger.RecordedAnything);
	}

	[Fact]
	public void ASkippedItemWithNoReasonIsRefusedAtTheEmissionPoint()
	{
		EmissionPoint emissionPoint = new([]);

		Assert.Throws<ArgumentException>(
			() => emissionPoint.Emit(new EmittedFact { Category = EmissionCategory.SkippedItem }));
		Assert.Throws<ArgumentException>(
			() => emissionPoint.Emit(new EmittedFact
			{
				Category = EmissionCategory.SkippedItem,
				SkippedItem = SkippedItemCategory.None
			}));
	}

	[Fact]
	public void AReasonOnAnythingThatIsNotASkippedItemIsRefused()
	{
		EmissionPoint emissionPoint = new([]);

		Assert.Throws<ArgumentException>(
			() => emissionPoint.Emit(new EmittedFact
			{
				Category = EmissionCategory.Finding,
				SkippedItem = SkippedItemCategory.UnreadableFile
			}));
	}

	[Fact]
	public void TheLedgerIgnoresFactsThatAreNotSkippedItems()
	{
		SkippedItemLedger ledger = new();
		EmissionPoint emissionPoint = new([ledger]);

		emissionPoint.Emit(new EmittedFact { Category = EmissionCategory.Coverage });

		Assert.False(ledger.RecordedAnything);
	}

	/// <summary>
	/// It stores the typed path and renders nothing, so it declares the destination that escapes
	/// nothing. Escaping belongs to whichever sink eventually writes these paths somewhere.
	/// </summary>
	[Fact]
	public void TheLedgerIsNotARenderingDestination()
	{
		Assert.Equal(EmissionDestination.StructuredData, new SkippedItemLedger().Destination);
	}

	private static EmittedFact Skip(SkippedItemCategory category, string relativePath = "")
	{
		RepositoryRelativePath path = default;

		if (relativePath.Length > 0)
		{
			RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(Path.GetTempPath());

			_ = RepositoryRelativePath.TryFromScannedPath(repository, Path.Combine(repository.CanonicalPath, relativePath), out path);
		}

		return new EmittedFact
		{
			Category = EmissionCategory.SkippedItem,
			SkippedItem = category,
			Path = path
		};
	}
}
