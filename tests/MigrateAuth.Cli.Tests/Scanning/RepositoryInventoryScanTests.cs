using System.Text;

using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// The two passes: what each one does, what neither does twice, and what the second one sees
/// that a second walk would not.
/// </summary>
public sealed class RepositoryInventoryScanTests
{
	private const string _RECOGNIZED_FILE_NAME = "credentials.ex1";
	private const string _SECOND_RECOGNIZED_FILE_NAME = "headers.ex1";
	private const string _TWO_BYTE_FILE_NAME = "two-byte.ex1";
	private const string _UNRECOGNIZED_FILE_NAME = "notes.txt";
	private const string _OVERSIZE_FILE_NAME = "generated.ex1";
	private const string _CANDIDATE_CONTENT = "var credentials = NewExemplarCredentials(user, secret);\n";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";
	private const string _MIXED_LINE_ENDING_CONTENT = "first\r\nsecond\rthird\n";
	private const string _TWO_BYTE_CONTENT = "headers = @{ AUTHORIZATION = 'Basic ' }\n";
	private const int _EXPECTED_LINES_OF_THE_MIXED_FILE = 3;
	private const int _OVERSIZE_MARGIN_IN_BYTES = 1024;

	/// <summary>
	/// THE ORDERING GUARANTEE AT ITS SOURCE. The enumeration pass opens nothing, so none of the
	/// four categories only the reader can raise appears after it - over a tree that contains one
	/// instance of each. The advance warning fires from this pass's count, so a pass that opened
	/// files would put the warning after the work it warns about.
	/// </summary>
	[Fact]
	public void TheEnumerationPassOpensNothing()
	{
		using ScanHarness harness = ScanHarness.CreateWithEveryCondition();

		EnumerationResult enumeration = harness.BuildScan().Enumerate(harness.BuildProgress(null), CancellationToken.None);

		Assert.NotEmpty(enumeration.FilePaths);
		Assert.Equal(0, harness.Fixture.Ledger.TallyOf(SkippedItemCategory.UnreadableFile).Count);
		Assert.Equal(0, harness.Fixture.Ledger.TallyOf(SkippedItemCategory.Undecodable).Count);
		Assert.Equal(0, harness.Fixture.Ledger.TallyOf(SkippedItemCategory.OversizeFile).Count);
		Assert.Equal(0, harness.Fixture.Ledger.TallyOf(SkippedItemCategory.OverLongLine).Count);
	}

	/// <summary>
	/// THE TREE IS WALKED ONCE AND BOTH PASSES RUN OVER THE LIST, proved by the one observable
	/// difference between a list and a second walk: a file deleted between the passes. Over a list,
	/// the inventory pass tries to open it and records an unreadable file. Over a second walk, it
	/// would simply not be there, and the count would silently be one lower - which is the
	/// disagreement the design refuses to allow about a repository being edited during a scan.
	/// </summary>
	[Fact]
	public void TheSecondPassRunsOverTheListRatherThanOverTheTreeAgain()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		string absolutePath = harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);
		harness.Fixture.WriteText(_SECOND_RECOGNIZED_FILE_NAME, _ORDINARY_CONTENT);

		RepositoryInventoryScan scan = harness.BuildScan();
		EnumerationResult enumeration = scan.Enumerate(harness.BuildProgress(null), CancellationToken.None);

		File.Delete(absolutePath);

		InventoryResult inventory = scan.ReadInventory(
			enumeration.FilePaths,
			harness.BuildProgress(enumeration.FilePaths.Count),
			CancellationToken.None);

		Assert.Equal(2, enumeration.FilePaths.Count);
		Assert.Equal(2, inventory.Inventory.TotalFileCount);
		Assert.Equal(1, harness.Fixture.Ledger.TallyOf(SkippedItemCategory.UnreadableFile).Count);
		Assert.Empty(inventory.CandidateRelativePaths);
	}

	[Fact]
	public void AFileCarryingAnAnchorBecomesACandidate()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);
		harness.Fixture.WriteText(_SECOND_RECOGNIZED_FILE_NAME, _ORDINARY_CONTENT);

		InventoryResult inventory = RunBothPasses(harness);

		Assert.Equal([_RECOGNIZED_FILE_NAME], inventory.CandidateRelativePaths);
		Assert.Equal(1, inventory.Inventory.TotalCandidateFileCount);
		Assert.Equal(2, inventory.Inventory.TotalFileCount);
	}

	/// <summary>
	/// PRESELECTION OVER DECODED TEXT, THROUGH THE WHOLE PIPELINE. The file is two bytes per
	/// character with no byte order mark - the case a preselection over the byte buffer misses
	/// silently - and it carries the anchor in the other casing, so the anchor's own case rule is
	/// exercised at the same time.
	/// </summary>
	[Fact]
	public void ATwoByteEncodedFileBecomesACandidateThroughTheRealPipeline()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		harness.Fixture.WriteBytes(
			_TWO_BYTE_FILE_NAME,
			new UnicodeEncoding(bigEndian: false, byteOrderMark: false).GetBytes(_TWO_BYTE_CONTENT));

		InventoryResult inventory = RunBothPasses(harness);

		Assert.Equal([_TWO_BYTE_FILE_NAME], inventory.CandidateRelativePaths);
	}

	/// <summary>
	/// Detection: a file no analyzer claims is counted in the inventory and excluded from
	/// candidate selection - even when its text would have matched an anchor, because there are no
	/// anchors to match it against.
	/// </summary>
	[Fact]
	public void AnUnrecognizedFileIsCountedAndNeverACandidate()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_UNRECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);

		InventoryResult inventory = RunBothPasses(harness);

		Assert.Empty(inventory.CandidateRelativePaths);
		Assert.Equal(1, inventory.Inventory.TotalFileCount);
		Assert.Equal(
			LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME,
			Assert.Single(inventory.Inventory.RowsInReportOrder()).LanguageName);
	}

	/// <summary>
	/// The line count is the normalized one, which is the same rule a finding's line number will
	/// derive from. Three line endings of three different shapes produce three lines, on every
	/// platform and whatever the version control client did to the file on checkout.
	/// </summary>
	[Fact]
	public void TheLineCountComesFromTheNormalizedText()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _MIXED_LINE_ENDING_CONTENT);

		InventoryResult inventory = RunBothPasses(harness);

		Assert.Equal(_EXPECTED_LINES_OF_THE_MIXED_FILE, inventory.Inventory.TotalLineCount);
	}

	/// <summary>
	/// The resource ceilings are consumed rather than re-checked: the file comes back skipped, the
	/// category is already counted by the machinery that owns it, and the inventory records a file
	/// with no lines instead of pretending it was never there.
	/// </summary>
	[Fact]
	public void AFileOverTheSizeCeilingIsInventoriedWithNoLines()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		harness.Fixture.WriteBytes(
			_OVERSIZE_FILE_NAME,
			new byte[ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES + _OVERSIZE_MARGIN_IN_BYTES]);

		InventoryResult inventory = RunBothPasses(harness);

		Assert.Equal(1, inventory.Inventory.TotalFileCount);
		Assert.Equal(0, inventory.Inventory.TotalLineCount);
		Assert.Empty(inventory.CandidateRelativePaths);
		Assert.Equal(1, harness.Fixture.Ledger.TallyOf(SkippedItemCategory.OversizeFile).Count);
	}

	[Fact]
	public void AnInterruptedInventoryPassSaysSo()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);

		using CancellationTokenSource cancellation = new();
		RepositoryInventoryScan scan = harness.BuildScan();
		EnumerationResult enumeration = scan.Enumerate(harness.BuildProgress(null), CancellationToken.None);

		cancellation.Cancel();

		InventoryResult inventory = scan.ReadInventory(
			enumeration.FilePaths,
			harness.BuildProgress(enumeration.FilePaths.Count),
			cancellation.Token);

		Assert.True(inventory.WasCancelled);
		Assert.Equal(0, inventory.Inventory.TotalFileCount);
	}

	[Fact]
	public void AnInterruptedEnumerationPassSaysSo()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);

		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();

		EnumerationResult enumeration = harness.BuildScan()
			.Enumerate(harness.BuildProgress(null), cancellation.Token);

		Assert.True(enumeration.WasCancelled);
	}

	/// <summary>
	/// Every candidate is one of the files the enumeration reached, expressed the same way. The
	/// candidate set is persisted and later read back by a stage that has to open those paths, so
	/// a candidate that is not an enumerated path is a candidate nobody can find.
	/// </summary>
	[Fact]
	public void EveryCandidateIsOneOfTheEnumeratedPaths()
	{
		using ScanHarness harness = ScanHarness.CreateWithEveryCondition();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);
		harness.Fixture.WriteText(Path.Combine("nested", _SECOND_RECOGNIZED_FILE_NAME), _CANDIDATE_CONTENT);

		RepositoryInventoryScan scan = harness.BuildScan();
		EnumerationResult enumeration = scan.Enumerate(harness.BuildProgress(null), CancellationToken.None);
		InventoryResult inventory = scan.ReadInventory(
			enumeration.FilePaths,
			harness.BuildProgress(enumeration.FilePaths.Count),
			CancellationToken.None);

		IReadOnlyCollection<string> enumeratedPaths =
			[.. enumeration.FilePaths.Select(path => path.Value)];

		Assert.Equal(3, inventory.CandidateRelativePaths.Count);
		Assert.Contains(Reading.RepositoryFixture.TWO_BYTE_FILE_NAME, inventory.CandidateRelativePaths);
		Assert.All(inventory.CandidateRelativePaths, candidate => Assert.Contains(candidate, enumeratedPaths));
	}

	/// <summary>
	/// The candidate set is ordered ordinally rather than left in the platform's walk
	/// order, so two identical runs persist an identical set - and a later cap over candidates
	/// drops the same ones on every machine.
	/// </summary>
	[Fact]
	public void TheCandidateSetIsOrderedOrdinally()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		harness.Fixture.WriteText(Path.Combine("zeta", _RECOGNIZED_FILE_NAME), _CANDIDATE_CONTENT);
		harness.Fixture.WriteText(Path.Combine("alpha", _RECOGNIZED_FILE_NAME), _CANDIDATE_CONTENT);
		harness.Fixture.WriteText(Path.Combine("mid", _RECOGNIZED_FILE_NAME), _CANDIDATE_CONTENT);

		InventoryResult inventory = RunBothPasses(harness);

		Assert.Equal(
			[
				$"alpha/{_RECOGNIZED_FILE_NAME}",
				$"mid/{_RECOGNIZED_FILE_NAME}",
				$"zeta/{_RECOGNIZED_FILE_NAME}"
			],
			inventory.CandidateRelativePaths);
	}

	private static InventoryResult RunBothPasses(ScanHarness harness)
	{
		RepositoryInventoryScan scan = harness.BuildScan();
		EnumerationResult enumeration = scan.Enumerate(harness.BuildProgress(null), CancellationToken.None);

		return scan.ReadInventory(
			enumeration.FilePaths,
			harness.BuildProgress(enumeration.FilePaths.Count),
			CancellationToken.None);
	}
}
