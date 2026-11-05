using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.MatchBoundStress;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// THE THROUGHPUT FLOOR OF THE WALK VOLUME PROOF. The same tree as <see cref="WalkVolumeTests"/>, seeded large, walked twice and measured; the assertion is against the
/// requirement's floor of files a minute and never against the design's reference figure.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class WalkVolumeTimingTests
{
	private const int _FOLDER_COUNT = 60;
	private const int _FILES_PER_FOLDER = 50;
	private const int _ANOMALY_INTERVAL = 700;
	private const int _SEEDED_OVER_LONG_LINES = (_FOLDER_COUNT * _FILES_PER_FOLDER - 1) / _ANOMALY_INTERVAL;
	private const int _REQUIRED_FILES_PER_MINUTE = 5000;
	private const int _SECONDS_PER_MINUTE = 60;
	private const string _THROUGHPUT_FORMAT =
		"{0}: {1} files in {2:F3} s = {3:N0} files/min (skipped files {4}, unlistable directories {5})";

	private readonly ITestOutputHelper _output;

	public WalkVolumeTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// Two passes over the same tree. The first meets it as the walk would on arrival and the
	/// second meets it with the filesystem's own caches already warm - which is the only sense in
	/// which a test on an unprivileged process can say "cold", and it is stated that way rather
	/// than claimed as a cold-cache measurement, because dropping the operating system's cache
	/// needs rights this process does not have.
	///
	/// Each pass gets its own ledger, so the second reconciles against what was seeded rather
	/// than against twice it.
	/// </summary>
	[Fact]
	public void AFewThousandFilesWalkAtSpeedWithTheAnomaliesStillReconciling()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();
		int seededFiles = SeedLargeTree(fixture);

		Measure(fixture, "first pass", seededFiles);

		SkippedItemLedger warmLedger = Measure(fixture, "second pass, caches warm", seededFiles);

		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.UnreadableFile).Count);
		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.UnlistableDirectory).Count);
		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.LinkNotFollowed).Count);
		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.Undecodable).Count);
		Assert.Equal(1, warmLedger.TallyOf(SkippedItemCategory.OversizeFile).Count);
		Assert.Equal(_SEEDED_OVER_LONG_LINES + 1, warmLedger.TallyOf(SkippedItemCategory.OverLongLine).Count);
		Assert.Equal(0, warmLedger.TallyOf(SkippedItemCategory.PathTooLong).Count);
	}

	private SkippedItemLedger Measure(RepositoryFixture fixture, string label, int seededFiles)
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

		Assert.True(filesSeen >= seededFiles);
		Assert.True(filesPerMinute > _REQUIRED_FILES_PER_MINUTE);

		return ledger;
	}

	private static int SeedLargeTree(RepositoryFixture fixture)
	{
		int seeded = 0;

		for (int folder = 0; folder < _FOLDER_COUNT; folder++)
		{
			for (int file = 0; file < _FILES_PER_FOLDER; file++)
			{
				SeedOneFile(fixture, folder, file);
				seeded++;
			}
		}

		return seeded;
	}

	/// <summary>
	/// Most files are ordinary and every seven-hundredth is not, so the anomalies are spread
	/// through the tree rather than gathered where a walk would meet them first.
	/// </summary>
	private static void SeedOneFile(RepositoryFixture fixture, int folder, int file)
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
