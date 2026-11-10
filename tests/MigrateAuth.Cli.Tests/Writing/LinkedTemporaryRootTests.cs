using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// A temporary folder that is reached through a directory link, reproduced on any platform.
///
/// macOS hands out a temporary folder beneath a folder that is itself a link, so the spelling the
/// platform returns and the spelling the product reports (the product resolves links along the whole
/// path) are different strings for one folder. Windows and Linux do not do that by default, so the
/// condition is created here with a real link: the fixtures are rooted at a path that goes
/// through it, and what is asserted is that every path a test builds from them is spelled the way
/// the product spells it.
/// </summary>
public sealed class LinkedTemporaryRootTests
{
	private const string _LINK_NAME = "linked-temporary-root";
	private const string _NESTED_FILE_PATH = "src/Client.cs";
	private const string _NESTED_FILE_CONTENT = "class Client { }\n";

	[Fact]
	public void TheLinkedSpellingOfAFolderResolvesToTheFolderTheLinkPointsAt()
	{
		using TemporaryFolder target = TemporaryFolder.Create();
		using TemporaryFolder host = TemporaryFolder.Create();

		string linkPath = Path.Combine(host.Path, _LINK_NAME);

		DirectoryLink.Create(linkPath, target.Path);

		string throughTheLink = Path.Combine(linkPath, "child");

		Assert.NotEqual(Path.Combine(target.Path, "child"), throughTheLink);
		Assert.Equal(Path.Combine(target.Path, "child"), CanonicalPath.Resolve(throughTheLink));
	}

	[Fact]
	public void AFolderCreatedThroughALinkIsHeldUnderItsCanonicalPath()
	{
		using TemporaryFolder target = TemporaryFolder.Create();
		using TemporaryFolder host = TemporaryFolder.Create();

		string linkPath = Path.Combine(host.Path, _LINK_NAME);

		DirectoryLink.Create(linkPath, target.Path);

		using TemporaryFolder folder = TemporaryFolder.CreateUnder(linkPath);

		Assert.Equal(target.Path, Path.GetDirectoryName(folder.Path));
		Assert.Equal(CanonicalPath.Resolve(folder.Path), folder.Path);
		Assert.True(Directory.Exists(folder.Path));
	}

	[Fact]
	public void APathBuiltFromAFixtureRootedThroughALinkIsAcceptedAndRelativized()
	{
		using TemporaryFolder target = TemporaryFolder.Create();
		using TemporaryFolder host = TemporaryFolder.Create();

		string linkPath = Path.Combine(host.Path, _LINK_NAME);

		DirectoryLink.Create(linkPath, target.Path);

		using RepositoryFixture fixture = RepositoryFixture.CreateEmptyUnder(linkPath);

		string absolutePath = fixture.WriteText(_NESTED_FILE_PATH, _NESTED_FILE_CONTENT);

		bool accepted = RepositoryRelativePath.TryFromScannedPath(
			fixture.Repository,
			absolutePath,
			out RepositoryRelativePath relativePath);

		Assert.Equal(fixture.Repository.CanonicalPath, fixture.RootPath);
		Assert.Equal(target.Path, Path.GetDirectoryName(fixture.RootPath));
		Assert.True(accepted);
		Assert.Equal(_NESTED_FILE_PATH, relativePath.Value);
		Assert.True(WriteBoundary.PermitsReading(fixture.Repository, absolutePath));
	}
}
