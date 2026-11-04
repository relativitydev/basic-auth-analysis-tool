using System.Diagnostics;
using System.Globalization;

using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// THE VOLUME PROOF FOR THE TERM THAT GROWS WITH THE REPOSITORY.
///
/// The candidate set is the one thing in this design that grows with the repository
/// and it is deliberately NOT capped: capping it means silently declining to analyze
/// files, which is a worse failure than the memory it saves. A claim like that is only worth
/// anything if the uncapped case has been run, so this builds a tree in which EVERY file carries
/// an anchor and reconciles the candidate count against the number of files created.
///
/// The worst case is this one: every file, 100 %.
///
/// IT ALSO MEASURES, and the number it prints is this machine's. The assertion is against the required
/// floor of 5,000 files a minute and never against a reference figure, because a test that fails
/// when a laptop is busy is a test that gets deleted.
/// </summary>
public sealed class ScanVolumeTests
{
	private const int _FOLDER_COUNT = 60;
	private const int _FILES_PER_FOLDER = 50;
	private const int _TREE_FILE_COUNT = _FOLDER_COUNT * _FILES_PER_FOLDER;
	private const int _REQUIRED_FILES_PER_MINUTE = 5000;
	private const int _SECONDS_PER_MINUTE = 60;
	private const string _CANDIDATE_CONTENT = "var credentials = NewExemplarCredentials(user, secret);\n";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";
	private const string _FIRST_LINE_CANDIDATE_CONTENT = "#!/usr/bin/env exemplar\nvar credentials = NewExemplarCredentials(user, secret);\n";
	private const string _REPOSITORY_ROOT_LABEL = "volume-repository";
	private const string _THROUGHPUT_FORMAT =
		"{0}: {1} files in {2:F3} s = {3:N0} files/min (candidates {4}, lines {5})";

	private readonly ITestOutputHelper _output;

	public ScanVolumeTests(ITestOutputHelper output)
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

		CreateTree(harness, everyFileCarriesAnAnchor: true);

		InventoryResult inventory = Measure(harness, "every file a candidate");

		Assert.Equal(_TREE_FILE_COUNT, inventory.Inventory.TotalFileCount);
		Assert.Equal(_TREE_FILE_COUNT, inventory.Inventory.TotalCandidateFileCount);
		Assert.Equal(_TREE_FILE_COUNT, inventory.CandidateRelativePaths.Count);
		Assert.Equal(_TREE_FILE_COUNT, inventory.CandidateRelativePaths.Distinct(StringComparer.Ordinal).Count());
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

		CreateTree(harness, everyFileCarriesAnAnchor: false);

		InventoryResult inventory = Measure(harness, "no file a candidate");

		Assert.Equal(_TREE_FILE_COUNT, inventory.Inventory.TotalFileCount);
		Assert.Empty(inventory.CandidateRelativePaths);
	}

	/// <summary>
	/// The whole stage at volume, through the state it writes to. Half a million paths is the
	/// worst case this term is declared against; three thousand is what a test can create in a
	/// second, and what it establishes is that nothing between the walk and the state loses or
	/// duplicates one.
	/// </summary>
	[Fact]
	public void TheStageCarriesEveryCandidateThroughToTheSessionState()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		CreateTree(harness, everyFileCarriesAnAnchor: true);

		SessionState state = SessionState.Fresh(harness.Catalog.Version, _REPOSITORY_ROOT_LABEL);
		StageRunOutcome outcome = harness.BuildStage().Run(state, CancellationToken.None);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Equal(_TREE_FILE_COUNT, state.CandidateRelativePaths.Count);
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

		Assert.Equal(_TREE_FILE_COUNT, inventory.Inventory.TotalCandidateFileCount);
		Assert.Equal(0, inventory.Inventory.FirstLineFallbackCount);
	}

	private static void CreateFirstLineTree(ScanHarness harness)
	{
		for (int folder = 0; folder < _FOLDER_COUNT; folder++)
		{
			for (int file = 0; file < _FILES_PER_FOLDER; file++)
			{
				harness.Fixture.WriteText(
					Path.Combine($"module-{folder:D3}", "scripts", $"script-{file:D3}"),
					_FIRST_LINE_CANDIDATE_CONTENT);
			}
		}
	}

	private static void CreateTree(ScanHarness harness, bool everyFileCarriesAnAnchor)
	{
		string content = everyFileCarriesAnAnchor ? _CANDIDATE_CONTENT : _ORDINARY_CONTENT;

		for (int folder = 0; folder < _FOLDER_COUNT; folder++)
		{
			for (int file = 0; file < _FILES_PER_FOLDER; file++)
			{
				harness.Fixture.WriteText(
					Path.Combine($"module-{folder:D3}", "source", $"file-{file:D3}.ex1"),
					content);
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
