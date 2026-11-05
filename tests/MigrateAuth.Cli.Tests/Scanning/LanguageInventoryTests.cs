using System.Collections.Immutable;

using MigrateAuth.Cli.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// the aggregate, over the three numbers a file is allowed to contribute.
/// </summary>
public sealed class LanguageInventoryTests
{
	private const string _FIRST_LANGUAGE = "ExemplarOne";
	private const string _SECOND_LANGUAGE = "ExemplarTwo";
	private const double _PERCENTAGE_TOLERANCE = 0.0001;

	[Fact]
	public void AnEmptyInventoryHasNoRowsAndNoTotals()
	{
		LanguageInventory inventory = new();

		Assert.Empty(inventory.RowsInReportOrder());
		Assert.Equal(0, inventory.TotalFileCount);
		Assert.Equal(0, inventory.TotalLineCount);
		Assert.Equal(0, inventory.TotalCandidateFileCount);
	}

	[Fact]
	public void FilesOfOneLanguageAccumulateIntoOneRow()
	{
		LanguageInventory inventory = new();

		inventory.RecordFile(_FIRST_LANGUAGE, lineCount: 10, isCandidate: true);
		inventory.RecordFile(_FIRST_LANGUAGE, lineCount: 5, isCandidate: false);

		LanguageInventoryRow row = Assert.Single(inventory.RowsInReportOrder());

		Assert.Equal(2, row.FileCount);
		Assert.Equal(15, row.LineCount);
		Assert.Equal(1, row.CandidateFileCount);
	}

	/// <summary>
	/// The shares add up to a hundred because the denominator is the enumerated file count, which
	/// every file contributes to whether or not it could be read.
	/// </summary>
	[Fact]
	public void TheSharesOfEveryRowAddUpToAHundred()
	{
		LanguageInventory inventory = new();

		inventory.RecordFile(_FIRST_LANGUAGE, lineCount: 10, isCandidate: false);
		inventory.RecordFile(_SECOND_LANGUAGE, lineCount: 0, isCandidate: false);
		inventory.RecordFile(LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, lineCount: 3, isCandidate: false);

		double total = inventory.RowsInReportOrder().Sum(row => row.ShareOfFilesPercentage);

		Assert.Equal(100.0, total, _PERCENTAGE_TOLERANCE);
	}

	/// <summary>
	/// A file that could not be read still holds its place in the inventory: it contributes a file
	/// and no lines. Dropping it would make the shares describe the readable part of a repository
	/// while being presented as describing the repository.
	/// </summary>
	[Fact]
	public void AFileWithNoLinesStillCountsAsAFile()
	{
		LanguageInventory inventory = new();

		inventory.RecordFile(_FIRST_LANGUAGE, lineCount: 0, isCandidate: false);

		Assert.Equal(1, inventory.TotalFileCount);
		Assert.Equal(0, inventory.TotalLineCount);
	}

	/// <summary>
	/// The same repository produces the same output everywhere, so the rows are ordered
	/// ordinally rather than by whatever a hash table hands back.
	/// </summary>
	[Fact]
	public void RowsAreOrderedOrdinallyByLanguageName()
	{
		LanguageInventory inventory = new();

		inventory.RecordFile(LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, lineCount: 1, isCandidate: false);
		inventory.RecordFile(_SECOND_LANGUAGE, lineCount: 1, isCandidate: false);
		inventory.RecordFile(_FIRST_LANGUAGE, lineCount: 1, isCandidate: false);

		ImmutableArray<LanguageInventoryRow> rows = inventory.RowsInReportOrder();

		Assert.Equal(
			[_FIRST_LANGUAGE, _SECOND_LANGUAGE, LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME],
			rows.Select(row => row.LanguageName));
	}

	[Fact]
	public void TheCandidateTotalIsDistinctFromTheFileTotal()
	{
		LanguageInventory inventory = new();

		inventory.RecordFile(_FIRST_LANGUAGE, lineCount: 1, isCandidate: true);
		inventory.RecordFile(_FIRST_LANGUAGE, lineCount: 1, isCandidate: false);
		inventory.RecordFile(_SECOND_LANGUAGE, lineCount: 1, isCandidate: false);

		Assert.Equal(3, inventory.TotalFileCount);
		Assert.Equal(1, inventory.TotalCandidateFileCount);
	}

	[Fact]
	public void ANegativeLineCountIsRefused()
	{
		LanguageInventory inventory = new();

		Assert.Throws<ArgumentOutOfRangeException>(
			() => inventory.RecordFile(_FIRST_LANGUAGE, lineCount: -1, isCandidate: false));
	}
}
