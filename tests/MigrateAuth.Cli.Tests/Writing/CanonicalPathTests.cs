using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// Canonicalization is what the boundary's comparison rests on, so the things it has to absorb
/// before the comparison runs are asserted here rather than left to the comparison to survive.
/// </summary>
public sealed class CanonicalPathTests
{
	private const string _LONG_FOLDER_NAME = "A Long Folder Name";
	private const string _EIGHT_DOT_THREE_FOLDER_NAME = "ALONGF~1";
	private const string _NESTED_FOLDER_NAME = "nested";
	private const string _PARENT_SEGMENT = "..";

	[Fact]
	public void SegmentsOfAnAbsolutePathLeadWithItsRoot()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();

		string[] segments = CanonicalPath.SplitIntoSegments(folder.Path);

		Assert.NotEmpty(segments);
		Assert.Equal(Path.GetFileName(folder.Path), segments[^1]);
		Assert.DoesNotContain(string.Empty, segments);
	}

	[Fact]
	public void TrailingAndDoubledSeparatorsChangeNothing()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();

		string awkwardPath = $"{folder.Path}{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}";

		Assert.Equal(CanonicalPath.SplitIntoSegments(folder.Path), CanonicalPath.SplitIntoSegments(awkwardPath));
	}

	[Fact]
	public void ATraversalIsCollapsedBeforeAnythingIsCompared()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();

		string traversedPath = Path.Combine(folder.Path, _NESTED_FOLDER_NAME, _PARENT_SEGMENT);

		Assert.Equal(CanonicalPath.Resolve(folder.Path), CanonicalPath.Resolve(traversedPath));
	}

	/// <summary>
	/// Pre-flight runs against a destination that is known and does not exist yet, so resolution
	/// has to answer for one rather than throw.
	/// </summary>
	[Fact]
	public void APathThatDoesNotExistKeepsItsAbsoluteForm()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();

		string absentPath = Path.Combine(folder.Path, "not-created-yet", "deeper");

		Assert.Equal(absentPath, CanonicalPath.Resolve(absentPath));
	}

	[Fact]
	public void AnExistingFolderResolvesToTheNameTheFilesystemHolds()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();

		string longPath = Path.Combine(folder.Path, _LONG_FOLDER_NAME);

		Directory.CreateDirectory(longPath);

		Assert.Equal(longPath, CanonicalPath.Resolve(longPath));
	}

	/// <summary>
	/// A short name is one of the things a string-prefix comparison gets wrong, and it
	/// is the one that arrives without anybody arranging it - the same folder handed out in two
	/// spellings by two sources. Where the volume generates short names, resolution expands
	/// them, so the two spellings meet as one canonical path.
	///
	/// Where the volume does not generate them there is no second spelling and nothing to
	/// reconcile, which is what the other branch asserts.
	/// </summary>
	[Fact]
	public void TwoSpellingsOfOneFolderResolveToOneCanonicalPath()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();

		string longPath = Path.Combine(folder.Path, _LONG_FOLDER_NAME);
		string eightDotThreePath = Path.Combine(folder.Path, _EIGHT_DOT_THREE_FOLDER_NAME);

		Directory.CreateDirectory(longPath);

		string secondSpelling = Directory.Exists(eightDotThreePath) ? eightDotThreePath : longPath;

		Assert.Equal(CanonicalPath.Resolve(longPath), CanonicalPath.Resolve(secondSpelling));
	}
}
