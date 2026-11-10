using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.MatchBoundStress;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// THE THROUGHPUT FLOOR OF THE VOLUME PROOF. These are the tests of <see cref="ScanVolumeTests"/>'s tree that measure the stage and assert the required floor of files a minute.
/// The assertion is against the required floor and never against a reference figure.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class ScanVolumeTimingTests
{
	private const int _REQUIRED_FILES_PER_MINUTE = 5000;
	private const int _SECONDS_PER_MINUTE = 60;
	private const string _FIRST_LINE_CANDIDATE_CONTENT = "#!/usr/bin/env exemplar\nvar credentials = NewExemplarCredentials(user, secret);\n";
	private const string _THROUGHPUT_FORMAT =
		"{0}: {1} files in {2:F3} s = {3:N0} files/min (candidates {4}, lines {5})";

	private readonly ITestOutputHelper _output;

	public ScanVolumeTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// EVERY FILE A CANDIDATE. Nothing drops one, nothing truncates the list, and the count the
	/// stage reports is the count that was created - which is what "uncapped, deliberately" has to
	/// mean to be worth stating.
	/// </summary>
	[Fact]
	public void ATreeInWhichEveryFileIsACandidateKeepsEveryCandidate()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		ScanVolumeTests.CreateTree(harness, everyFileCarriesAnAnchor: true);

		InventoryResult inventory = Measure(harness, "every file a candidate");

		Assert.Equal(ScanVolumeTests.TREE_FILE_COUNT, inventory.Inventory.TotalFileCount);
		Assert.Equal(ScanVolumeTests.TREE_FILE_COUNT, inventory.Inventory.TotalCandidateFileCount);
		Assert.Equal(ScanVolumeTests.TREE_FILE_COUNT, inventory.CandidateRelativePaths.Count);
		Assert.Equal(ScanVolumeTests.TREE_FILE_COUNT, inventory.CandidateRelativePaths.Distinct(StringComparer.Ordinal).Count());
	}

	/// <summary>
	/// The same tree with no anchor in it: the same inventory, and no candidates at all. It is the
	/// other end of the same term, and it is the zero-candidate requirement at volume - a repository
	/// with zero candidate files is a normal outcome and not an error.
	/// </summary>
	[Fact]
	public void TheSameTreeWithNoAnchorProducesNoCandidates()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		ScanVolumeTests.CreateTree(harness, everyFileCarriesAnAnchor: false);

		InventoryResult inventory = Measure(harness, "no file a candidate");

		Assert.Equal(ScanVolumeTests.TREE_FILE_COUNT, inventory.Inventory.TotalFileCount);
		Assert.Empty(inventory.CandidateRelativePaths);
	}

	/// <summary>
	/// The lexical budget charges first-line detection for every file with no extension, so this
	/// is the volume at which its clock reads are paid: every file is extensionless and is attributed
	/// by its first line. Under the system clock none of them may trip the budget, which is the proof
	/// that the watch costs a clock read per file and never a false fallback.
	/// </summary>
	[Fact]
	public void ATreeOfExtensionlessFilesIsAttributedByFirstLineAtVolumeWithoutASingleFallback()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		CreateFirstLineTree(harness);

		InventoryResult inventory = Measure(harness, "every file attributed by first line");

		Assert.Equal(ScanVolumeTests.TREE_FILE_COUNT, inventory.Inventory.TotalCandidateFileCount);
		Assert.Equal(0, inventory.Inventory.FirstLineFallbackCount);
	}

	private static void CreateFirstLineTree(ScanHarness harness)
	{
		for (int folder = 0; folder < ScanVolumeTests.FOLDER_COUNT; folder++)
		{
			for (int file = 0; file < ScanVolumeTests.FILES_PER_FOLDER; file++)
			{
				harness.Fixture.WriteText(
					Path.Combine($"module-{folder:D3}", "scripts", $"script-{file:D3}"),
					_FIRST_LINE_CANDIDATE_CONTENT);
			}
		}
	}

	/// <summary>
	/// Both passes, timed together, because the two of them are what the required floor is about: the
	/// user waits for the stage and not for one of its halves.
	/// </summary>
	private InventoryResult Measure(ScanHarness harness, string label)
	{
		RepositoryInventoryScan scan = harness.BuildScan();
		Stopwatch stopwatch = Stopwatch.StartNew();

		EnumerationResult enumeration = scan.Enumerate(harness.BuildProgress(null), CancellationToken.None);
		InventoryResult inventory = scan.ReadInventory(
			enumeration.FilePaths,
			harness.BuildProgress(enumeration.FilePaths.Count),
			CancellationToken.None);

		stopwatch.Stop();

		double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
		double filesPerMinute = enumeration.FilePaths.Count / Math.Max(elapsedSeconds, double.Epsilon) * _SECONDS_PER_MINUTE;

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			_THROUGHPUT_FORMAT,
			label,
			enumeration.FilePaths.Count,
			elapsedSeconds,
			filesPerMinute,
			inventory.CandidateRelativePaths.Count,
			inventory.Inventory.TotalLineCount));

		Assert.True(filesPerMinute > _REQUIRED_FILES_PER_MINUTE);

		return inventory;
	}
}
