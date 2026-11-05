using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// THE VOLUME PROOF: a tree large enough to be a genuine walk, with the anomalies scattered
/// through it, reconciled item by item against what was seeded.
///
/// The eight-condition fixture proves each condition exists. This proves they still behave when
/// they are not the only thing in the tree - that a denial in one directory does not cost the
/// files in another, that the counts are exact rather than approximately right, and that the
/// walk holds nothing it does not need while it does so.
///
/// THE MEASUREMENT lives in WalkVolumeTimingTests, which prints this machine's number and asserts
/// against the requirement's floor of files a minute.
/// </summary>
public sealed class WalkVolumeTests
{
	private const int _READ_FILES_IN_THE_FIXTURE = 4;
	private const int _SKIPPED_FILES_IN_THE_FIXTURE = 5;

	/// <summary>
	/// Every seeded condition, reconciled. The numbers on the right are what the fixture put
	/// there, one of each, and the two zeroes are the categories the scan declares and does not
	/// raise.
	/// </summary>
	[Fact]
	public void TheSeededFixtureReconcilesItemByItem()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		List<FileReadResult> results = fixture.WalkAndRead();

		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.UnreadableFile).Count);
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.UnlistableDirectory).Count);
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.LinkNotFollowed).Count);
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.Undecodable).Count);
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.OversizeFile).Count);
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.OverLongLine).Count);
		Assert.Equal(0, fixture.Ledger.TallyOf(SkippedItemCategory.PathTooLong).Count);
		Assert.Equal(0, fixture.Ledger.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
		Assert.Equal(0, fixture.Ledger.TallyOf(SkippedItemCategory.MissingCandidate).Count);

		Assert.Equal(_SKIPPED_FILES_IN_THE_FIXTURE, fixture.Ledger.SkippedFiles);
		Assert.Equal(1, fixture.Ledger.DirectoriesOfUnknownContents);
		Assert.Equal(_READ_FILES_IN_THE_FIXTURE, results.Count(result => result.WasRead));
	}

	/// <summary>
	/// The files that were read are exactly the ones nothing was wrong with, named rather than
	/// counted - a count alone would pass if the walk read the wrong four files.
	/// </summary>
	[Fact]
	public void TheFilesThatSurviveAreTheOnesWithNothingWrongWithThem()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);
		RepositoryFileReader reader = new(fixture.EmissionPoint);

		List<string> read =
		[
			.. walk.Enumerate()
				.Where(file => reader.Read(file).WasRead)
				.Select(file => file.Path.Value)
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal(
			[
				RepositoryFixture.CARRIAGE_RETURN_FILE_NAME,
				RepositoryFixture.CLEAN_FILE_NAME,
				$"{RepositoryFixture.TARGET_DIRECTORY_NAME}/{RepositoryFixture.TARGET_FILE_NAME}",
				RepositoryFixture.TWO_BYTE_FILE_NAME
			],
			read);
	}
}
