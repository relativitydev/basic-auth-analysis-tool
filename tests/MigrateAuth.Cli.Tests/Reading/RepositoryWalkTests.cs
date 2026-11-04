using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// THE WALK'S DENIAL AND UNLISTABLE-DIRECTORY BEHAVIOUR.
///
/// A denied file or directory never aborts the walk and never disappears silently; an unlistable
/// directory is reported as ONE DIRECTORY OF UNKNOWN CONTENTS and never as a file count; and an
/// equivalent permission condition produces the same message shape, skip behaviour and
/// skipped-item count on every platform.
///
/// The product discovers a denial by ATTEMPTING the operation, so nothing in it knows what a
/// permission model is, and the same code path runs on all three platforms. The cause of a denial,
/// the running identity, and access-time side effects all differ between platforms and are not
/// equalized.
/// </summary>
public sealed class RepositoryWalkTests
{
	/// <summary>The limit long-path awareness exists to get past, quoted here to be exceeded.</summary>
	private const int _HISTORICAL_PATH_LIMIT = 260;

	private const int _DEEP_PATH_SEGMENT_COUNT = 12;
	private const int _UNREACHABLE_FILE_COUNT = 4;
	private const int _STREAMED_FOLDER_COUNT = 50;
	private const int _EXCLUSION_LIST_LENGTH = 8;

	[Fact]
	public void ADeniedDirectoryDoesNotStopTheWalkReachingTheRestOfTheTree()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		List<string> walked = fixture.WalkedPaths();

		Assert.Contains(RepositoryFixture.CLEAN_FILE_NAME, walked);
		Assert.Contains($"{RepositoryFixture.TARGET_DIRECTORY_NAME}/{RepositoryFixture.TARGET_FILE_NAME}", walked);
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.UnlistableDirectory).Count);
	}

	/// <summary>
	/// The hole is one directory, and the four files inside it are NOT four skipped files. A
	/// summary that collapsed the two would understate an unreadable subtree by its own size and
	/// read as a near-complete scan.
	/// </summary>
	[Fact]
	public void AnUnlistableDirectoryCountsOneDirectoryAndNoFiles()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		string deniedPath = fixture.CreateFolder("unreadable");

		for (int index = 0; index < _UNREACHABLE_FILE_COUNT; index++)
		{
			fixture.WriteText(Path.Combine("unreadable", $"hidden-{index}.cs"), RepositoryFixture.CLEAN_FILE_CONTENT);
		}

		fixture.Deny(deniedPath);
		_ = fixture.WalkedPaths();

		SkippedItemTally tally = fixture.Ledger.TallyOf(SkippedItemCategory.UnlistableDirectory);

		Assert.Equal(1, tally.Count);
		Assert.Equal(SkippedItemUnit.Directories, tally.Unit);
		Assert.Equal(1, fixture.Ledger.DirectoriesOfUnknownContents);
		Assert.Equal(0, fixture.Ledger.SkippedFiles);
	}

	[Fact]
	public void ADeniedFileIsCountedWithItsRelativePathAndNothingElse()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		_ = fixture.WalkAndRead();

		SkippedItemTally tally = fixture.Ledger.TallyOf(SkippedItemCategory.UnreadableFile);

		Assert.Equal(1, tally.Count);
		Assert.Equal(SkippedItemUnit.Files, tally.Unit);
		Assert.Equal([RepositoryFixture.DENIED_FILE_NAME], tally.EnumeratedPaths.Select(path => path.Value));
	}

	/// <summary>
	/// The path travels relative to the root, with portable separators, and no framework
	/// exception text travels with it - which on this platform embeds the absolute path by
	/// construction. The assertion is that the emitted path is not the absolute one and does not
	/// contain the root.
	/// </summary>
	[Fact]
	public void NoSkippedItemCarriesAnAbsolutePath()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		_ = fixture.WalkAndRead();

		IEnumerable<string> paths = fixture.Ledger.Tallies()
			.SelectMany(tally => tally.EnumeratedPaths)
			.Select(path => path.Value);

		Assert.NotEmpty(paths);
		Assert.All(paths, path => Assert.False(Path.IsPathRooted(path)));
		Assert.All(paths, path => Assert.DoesNotContain(fixture.RootPath, path, StringComparison.OrdinalIgnoreCase));
		Assert.All(paths, path => Assert.DoesNotContain("\\", path, StringComparison.Ordinal));
	}

	[Fact]
	public void ADirectoryLinkIsCountedAndNotDescendedInto()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		List<string> walked = fixture.WalkedPaths();

		string targetFilePath = $"{RepositoryFixture.TARGET_DIRECTORY_NAME}/{RepositoryFixture.TARGET_FILE_NAME}";

		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.LinkNotFollowed).Count);
		Assert.DoesNotContain(
			walked,
			path => path.StartsWith($"{RepositoryFixture.LINKED_DIRECTORY_NAME}/", StringComparison.Ordinal));
		Assert.Equal(1, walked.Count(path => string.Equals(path, targetFilePath, StringComparison.Ordinal)));
	}

	/// <summary>
	/// A link pointing at its own parent is the case that would make a recursive walk run until
	/// the path length stops it. The walk terminates because it never descends into a link at
	/// all, rather than because it recognizes the cycle.
	/// </summary>
	[Fact]
	public void ALinkPointingAtAnAncestorDoesNotMakeTheWalkRunForever()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText(Path.Combine("inner", RepositoryFixture.CLEAN_FILE_NAME), RepositoryFixture.CLEAN_FILE_CONTENT);
		DirectoryLink.Create(Path.Combine(fixture.RootPath, "inner", "back-to-the-root"), fixture.RootPath);

		List<string> walked = fixture.WalkedPaths();

		Assert.Equal([$"inner/{RepositoryFixture.CLEAN_FILE_NAME}"], walked);
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.LinkNotFollowed).Count);
	}

	[Fact]
	public void AnExcludedDirectoryIsNotWalkedAndIsNotASkippedItem()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();

		List<string> walked = fixture.WalkedPaths();

		Assert.DoesNotContain(
			walked,
			path => path.StartsWith($"{RepositoryFixture.EXCLUDED_DIRECTORY_NAME}/", StringComparison.Ordinal));
		Assert.All(
			fixture.Ledger.Tallies().SelectMany(tally => tally.EnumeratedPaths),
			path => Assert.DoesNotContain(RepositoryFixture.EXCLUDED_DIRECTORY_NAME, path.Value, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("bin")]
	[InlineData("obj")]
	[InlineData(".git")]
	[InlineData(".vs")]
	[InlineData("node_modules")]
	[InlineData("dist")]
	[InlineData("build")]
	[InlineData("packages")]
	public void EveryNameOnTheExclusionListIsExcluded(string directoryName)
	{
		Assert.True(WalkExclusions.IsExcluded(directoryName));
	}

	[Theory]
	[InlineData("vendor")]
	[InlineData(".venv")]
	[InlineData("venv")]
	[InlineData("__pycache__")]
	[InlineData("target")]
	public void AFormerExtensionNameIsNoLongerExcluded(string directoryName)
	{
		Assert.False(WalkExclusions.IsExcluded(directoryName));
	}

	[Theory]
	[InlineData("vendor")]
	[InlineData(".venv")]
	[InlineData("venv")]
	[InlineData("__pycache__")]
	[InlineData("target")]
	public void AFileUnderAFormerExtensionNameIsWalked(string directoryName)
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		string relativePath = $"{directoryName}/{RepositoryFixture.CLEAN_FILE_NAME}";

		fixture.WriteText(relativePath, RepositoryFixture.CLEAN_FILE_CONTENT);

		Assert.Contains(relativePath, fixture.WalkedPaths());
	}

	/// <summary>
	/// The one name whose ABSENCE is the decision. In a client repository this folder holds
	/// deployment scripts - PowerShell and Shell, two of the eight languages, and precisely where
	/// the shell forms of basic authentication concentrate - so excluding it by name would
	/// produce silent zero recall on that material.
	/// </summary>
	[Fact]
	public void DeploymentIsNotExcluded()
	{
		Assert.False(WalkExclusions.IsExcluded("Deployment"));
		Assert.False(WalkExclusions.IsExcluded("deployment"));
	}

	[Fact]
	public void TheExclusionListIsExactlyTheRequirementsEightNames()
	{
		Assert.Equal(_EXCLUSION_LIST_LENGTH, WalkExclusions.EXCLUDED_DIRECTORY_NAMES.Length);
		Assert.Equal(
			WalkExclusions.EXCLUDED_DIRECTORY_NAMES.Length,
			WalkExclusions.EXCLUDED_DIRECTORY_NAMES.Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void ARepositoryWithNothingInItProducesNoFilesAndNoSkippedItems()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		Assert.Empty(fixture.WalkedPaths());
		Assert.False(fixture.Ledger.RecordedAnything);
	}

	/// <summary>
	/// A path far past the platform's historical limit is ENUMERATED AND READ rather than
	/// skipped: a deep file must not be analyzed on one platform and silently missing on another.
	/// The binary declares long-path awareness in its manifest for this reason, and this test is what
	/// would notice if the declaration were dropped and the framework's own handling were not enough
	/// on its own.
	/// </summary>
	[Fact]
	public void ADeepPathIsWalkedAndReadRatherThanSkipped()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		string deepRelativePath = BuildDeepRelativePath();

		fixture.WriteText(deepRelativePath, RepositoryFixture.CLEAN_FILE_CONTENT);

		List<FileReadResult> results = fixture.WalkAndRead();

		Assert.True(fixture.RootPath.Length + deepRelativePath.Length + 1 > _HISTORICAL_PATH_LIMIT);
		Assert.Single(results);
		Assert.True(results[0].WasRead);
		Assert.Equal(0, fixture.Ledger.TallyOf(SkippedItemCategory.PathTooLong).Count);
	}

	/// <summary>
	/// The walk yields files as it finds them rather than building the tree first. The assertion
	/// is on the shape of the sequence, because the consequence at volume - half a million files
	/// never held at once - is not directly observable in a test.
	/// </summary>
	[Fact]
	public void TheWalkYieldsWithoutVisitingTheWholeTreeFirst()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		for (int index = 0; index < _STREAMED_FOLDER_COUNT; index++)
		{
			fixture.WriteText(Path.Combine($"folder-{index}", "file.cs"), RepositoryFixture.CLEAN_FILE_CONTENT);
		}

		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);
		int visited = 0;

		foreach (WalkedFile file in walk.Enumerate())
		{
			visited++;

			if (visited == 1)
			{
				Assert.True(file.Path.IsDeclared);
			}
		}

		Assert.Equal(_STREAMED_FOLDER_COUNT, visited);
	}

	[Fact]
	public void AWalkRefusesToBeBuiltWithoutAnEmissionPoint()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		Assert.Throws<ArgumentNullException>(() => new RepositoryWalk(fixture.Repository, null!));
	}

	/// <summary>
	/// Two walks of the same unchanged tree reach the same set of files. It is the set that the
	/// determinism requirement fixes; the ORDER of the output is imposed where output is produced,
	/// not by the walk.
	/// </summary>
	[Fact]
	public void TwoWalksOfTheSameTreeReachTheSameFiles()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateWithEveryCondition();
		EmissionPoint silent = new([]);
		RepositoryWalk firstWalk = new(fixture.Repository, silent);
		RepositoryWalk secondWalk = new(fixture.Repository, silent);

		List<string> first = [.. firstWalk.Enumerate().Select(file => file.Path.Value).Order(StringComparer.Ordinal)];
		List<string> second = [.. secondWalk.Enumerate().Select(file => file.Path.Value).Order(StringComparer.Ordinal)];

		Assert.Equal(first, second);
	}

	private static string BuildDeepRelativePath()
	{
		string[] segments = [.. Enumerable.Range(0, _DEEP_PATH_SEGMENT_COUNT).Select(index => $"deep-segment-{index:D2}-padding")];

		return Path.Combine([.. segments, RepositoryFixture.CLEAN_FILE_NAME]);
	}

	/// <summary>
	/// The repository root itself being unlistable is the one skipped item that cannot carry a
	/// path, because the root is emitted as a label and never as a path. It is counted anyway:
	/// the count is the part that must not lie.
	/// </summary>
	[Fact]
	public void AnUnlistableRootIsCountedWithoutAPath()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText(RepositoryFixture.CLEAN_FILE_NAME, RepositoryFixture.CLEAN_FILE_CONTENT);
		fixture.Deny(fixture.RootPath);

		Assert.Empty(fixture.WalkedPaths());

		SkippedItemTally tally = fixture.Ledger.TallyOf(SkippedItemCategory.UnlistableDirectory);

		Assert.Equal(1, tally.Count);
		Assert.Empty(tally.EnumeratedPaths);
		Assert.Equal(1, tally.OmittedPathCount);
	}

	[Fact]
	public void AWalkOfAFolderThatIsNotThereIsOneUnlistableDirectoryAndNoException()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		RepositoryFolderPath absent = RepositoryFolderPath.FromSuppliedText(
			Path.Combine(fixture.RootPath, "never-created"));
		RepositoryWalk walk = new(absent, fixture.EmissionPoint);

		Assert.Empty(walk.Enumerate());
		Assert.Equal(1, fixture.Ledger.TallyOf(SkippedItemCategory.UnlistableDirectory).Count);
	}
}
