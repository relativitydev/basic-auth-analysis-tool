using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// THE VOLUME PROOF: a tree large enough to be a genuine walk, with the anomalies scattered
/// through it, reconciled item by item against what was planted.
///
/// The eight-condition fixture proves each condition exists. This proves they still behave when
/// they are not the only thing in the tree - that a denial in one directory does not cost the
/// files in another, that the counts are exact rather than approximately right, and that the
/// walk holds nothing it does not need while it does so.
///
/// IT ALSO MEASURES, and the number it prints is this machine's. The assertion is against the
/// requirement's floor, because a test that fails when a machine is busy proves nothing about the
/// product.
/// </summary>
public sealed class WalkVolumeTests
{
	private const int _FOLDER_COUNT = 60;
	private const int _FILES_PER_FOLDER = 50;
	private const int _ANOMALY_INTERVAL = 700;
	private const int _PLANTED_OVER_LONG_LINES = (_FOLDER_COUNT * _FILES_PER_FOLDER - 1) / _ANOMALY_INTERVAL;
	private const int _READ_FILES_IN_THE_FIXTURE = 4;
	private const int _SKIPPED_FILES_IN_THE_FIXTURE = 5;
	private const int _REQUIRED_FILES_PER_MINUTE = 5000;
	private const int _SECONDS_PER_MINUTE = 60;
	private const string _THROUGHPUT_FORMAT =
		"{0}: {1} files in {2:F3} s = {3:N0} files/min (skipped files {4}, unlistable directories {5})";

	private readonly ITestOutputHelper _output;

	public WalkVolumeTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// Every planted condition, reconciled. The numbers on the right are what the fixture put
	/// there, one of each, and the two zeroes are the categories the scan declares and does not
	/// raise.
	/// </summary>
	[Fact]
	public void ThePlantedFixtureReconcilesItemByItem()
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

	/// <summary>
	/// Two passes over the same tree. The first meets it as the walk would on arrival and the
	/// second meets it with the filesystem's own caches already warm - which is the only sense in
	/// which a test on an unprivileged process can say "cold", and it is stated that way rather
	/// than claimed as a cold-cache measurement, because dropping the operating system's cache
	/// needs rights this process does not have.
	///
	/// Each pass gets its own ledger, so the second reconciles against what was planted rather
	/// than against twice it.
	/// </summary>
	[Fact]
	public void AFewThousandFilesWalkAtSpeedWithTheAnomaliesStillReconciling()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();
		int plantedFiles = PlantLargeTree(fixture);

		Measure(fixture, "first pass", plantedFiles);

		SkippedItemLedger warmLedger = Measure(fixture, "second pass, caches warm", plantedFiles);

		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.UnreadableFile).Count);
		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.UnlistableDirectory).Count);
		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.LinkNotFollowed).Count);
		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.Undecodable).Count);
		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.OversizeFile).Count);
		Assert.Equal(_PLANTED_OVER_LONG_LINES + 1, warmLedger.TallyOf(SkippedItemCategory.OverLongLine).Count);
		Assert.Equal(0, warmLedger.TallyOf(SkippedItemCategory.PathTooLong).Count);
	}

	private SkippedItemLedger Measure(RepositoryFixture fixture, string label, int plantedFiles)
	{
		SkippedItemLedger ledger = new();
		EmissionPoint emissionPoint = new([ledger]);
		RepositoryWalk walk = new(fixture.Repository, emissionPoint);
		RepositoryFileReader reader = new(emissionPoint);
		Stopwatch stopwatch = Stopwatch.StartNew();
		int filesSeen = 0;

		foreach (WalkedFile file in walk.Enumerate())
		{
			_ = reader.Read(file);
			filesSeen++;
		}

		stopwatch.Stop();

		double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
		double filesPerMinute = filesSeen / Math.Max(elapsedSeconds, double.Epsilon) * _SECONDS_PER_MINUTE;

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			_THROUGHPUT_FORMAT,
			label,
			filesSeen,
			elapsedSeconds,
			filesPerMinute,
			ledger.SkippedFiles,
			ledger.DirectoriesOfUnknownContents));

		Assert.True(filesSeen >= plantedFiles);
		Assert.True(filesPerMinute > _REQUIRED_FILES_PER_MINUTE);

		return ledger;
	}

	private static int PlantLargeTree(RepositoryFixture fixture)
	{
		int planted = 0;

		for (int folder = 0; folder < _FOLDER_COUNT; folder++)
		{
			for (int file = 0; file < _FILES_PER_FOLDER; file++)
			{
				PlantOneFile(fixture, folder, file);
				planted++;
			}
		}

		return planted;
	}

	/// <summary>
	/// Most files are ordinary and every seven-hundredth is not, so the anomalies are spread
	/// through the tree rather than gathered where a walk would meet them first.
	/// </summary>
	private static void PlantOneFile(RepositoryFixture fixture, int folder, int file)
	{
		string relativePath = Path.Combine($"module-{folder:D3}", "source", $"file-{file:D3}.cs");
		int ordinal = (folder * _FILES_PER_FOLDER) + file;

		if (ordinal > 0 && ordinal % _ANOMALY_INTERVAL == 0)
		{
			fixture.WriteText(relativePath, new string('a', ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS + 1));
		}
		else
		{
			fixture.WriteText(relativePath, RepositoryFixture.CLEAN_FILE_CONTENT);
		}
	}
}
