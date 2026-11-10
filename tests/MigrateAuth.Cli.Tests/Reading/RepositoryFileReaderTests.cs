using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// READING ONE FILE, AND THE FIVE WAYS IT DOES NOT HAPPEN.
///
/// The fixture is reconciled as a whole in <see cref="WalkVolumeTests"/>; these are the
/// individual conditions, each planted on its own so that a failure names which one moved.
/// </summary>
public sealed class RepositoryFileReaderTests
{
	private const string _FILE_NAME = "subject.cs";

	[Fact]
	public void AnOrdinaryFileIsReadDecodedAndMeasured()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText(_FILE_NAME, RepositoryFixture.CLEAN_FILE_CONTENT);

		FileReadResult result = Assert.Single(fixture.WalkAndRead());

		Assert.True(result.WasRead);
		Assert.Equal(RepositoryFixture.CLEAN_FILE_CONTENT, result.NormalizedText);
		Assert.Equal(DetectedEncoding.Utf8, result.Encoding);
		Assert.Equal(3, result.Measurement.LineCount);
		Assert.False(fixture.Ledger.RecordedAnything);
	}

	/// <summary>
	/// The two-byte file comes back as the text that was written, which is the half of the
	/// acceptance criterion a detection-only test would miss: a file detected as two-byte and
	/// then decoded wrongly still matches nothing.
	/// </summary>
	[Fact]
	public void AMarklessTwoByteFileComesBackAsItsOwnText()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		List<FileReadResult> results = fixture.WalkAndRead();
		FileReadResult twoByte = Assert.Single(
			results,
			result => result.Encoding is DetectedEncoding.Utf16LittleEndian);

		Assert.True(twoByte.WasRead);
		Assert.Equal(RepositoryFixture.TWO_BYTE_FILE_CONTENT, twoByte.NormalizedText);
		Assert.DoesNotContain(
			fixture.Ledger.TallyOf(SkippedItemCategory.Undecodable).EnumeratedPaths,
			path => string.Equals(path.Value, RepositoryFixture.TWO_BYTE_FILE_NAME, StringComparison.Ordinal));
	}

	[Fact]
	public void AFilePastTheSizeCeilingIsNotReadAndIsCounted()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteBytes(_FILE_NAME, new byte[ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES + 1]);

		FileReadResult result = Assert.Single(fixture.WalkAndRead());

		Assert.Equal(SkippedItemCategory.OversizeFile, result.SkippedAs);
		Assert.Equal(string.Empty, result.NormalizedText);
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.OversizeFile).Count);
	}

	/// <summary>
	/// The ceiling is inclusive: a file of exactly the maximum size is not refused for its size.
	/// This one is refused for what is IN it - a file of nothing but null bytes is dense in nulls
	/// of no single parity, which is the binary shape - and that is the point of the assertion:
	/// the size check let it through.
	/// </summary>
	[Fact]
	public void AFileExactlyAtTheSizeCeilingIsNotRefusedForItsSize()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteBytes(_FILE_NAME, new byte[ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES]);

		FileReadResult result = Assert.Single(fixture.WalkAndRead());

		Assert.NotEqual(SkippedItemCategory.OversizeFile, result.SkippedAs);
		Assert.Equal(SkippedItemCategory.Undecodable, result.SkippedAs);
	}

	/// <summary>
	/// The whole file is skipped, not the offending line, and the ceiling is breached near the
	/// END of the file - the case a check that stopped early would miss.
	/// </summary>
	[Fact]
	public void AFileWithOneOverLongLineIsSkippedWholeAndCounted()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		_ = fixture.WalkAndRead();

		SkippedItemTally tally = fixture.Ledger.TallyOf(SkippedItemCategory.OverLongLine);

		Assert.Equal(1, tally.Count);
		Assert.Equal([RepositoryFixture.OVER_LONG_LINE_FILE_NAME], tally.EnumeratedPaths.Select(path => path.Value));
	}

	[Fact]
	public void ABinaryFileIsCountedUndecodableRatherThanReadAsGarbage()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		_ = fixture.WalkAndRead();

		SkippedItemTally tally = fixture.Ledger.TallyOf(SkippedItemCategory.Undecodable);

		Assert.Equal(1, tally.Count);
		Assert.Equal([RepositoryFixture.BINARY_FILE_NAME], tally.EnumeratedPaths.Select(path => path.Value));
	}

	[Fact]
	public void LineEndingsAreNormalizedBeforeTheTextIsHandedOn()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText(_FILE_NAME, "first\r\nsecond\rthird\n");

		FileReadResult result = Assert.Single(fixture.WalkAndRead());

		Assert.Equal("first\nsecond\nthird\n", result.NormalizedText);
		Assert.Equal(3, result.Measurement.LineCount);
	}

	[Fact]
	public void AFileThatVanishedBetweenTheWalkAndTheReadIsCountedUnreadable()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		string absolutePath = fixture.WriteText(_FILE_NAME, RepositoryFixture.CLEAN_FILE_CONTENT);
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);
		RepositoryFileReader reader = new(fixture.EmissionPoint);
		List<WalkedFile> walked = [.. walk.Enumerate()];

		File.Delete(absolutePath);

		FileReadResult result = reader.Read(walked[0]);

		Assert.Equal(SkippedItemCategory.UnreadableFile, result.SkippedAs);
		Assert.Equal(0, fixture.Ledger.TallyOf(SkippedItemCategory.MissingCandidate).Count);
	}

	[Fact]
	public void AReaderRefusesToBeBuiltWithoutAnEmissionPoint()
	{
		Assert.Throws<ArgumentNullException>(() => new RepositoryFileReader(null!));
	}
}
