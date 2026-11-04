using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Emission;

/// <summary>
/// The only form of a path the product emits: relative to the repository root, never absolute,
/// never the root itself, and with portable separators.
/// </summary>
public sealed class RepositoryRelativePathTests
{
	private const string _PORTABLE_PATH = "src/Client/HttpFactory.cs";
	private const string _PARENT_SEGMENT = "..";

	[Fact]
	public void APathInsideTheRepositoryIsAcceptedWithPortableSeparators()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);
		string nativePath = System.IO.Path.Combine("src", "Client", "HttpFactory.cs");

		bool accepted = RepositoryRelativePath.TryFromScannedPath(repository, nativePath, out RepositoryRelativePath path);

		Assert.True(accepted);
		Assert.True(path.IsDeclared);
		Assert.Equal(_PORTABLE_PATH, path.Value);
	}

	[Fact]
	public void AnAbsolutePathInsideTheRepositoryIsEmittedRelativeToIt()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);
		string absolutePath = System.IO.Path.Combine(repository.CanonicalPath, "src", "Client", "HttpFactory.cs");

		bool accepted = RepositoryRelativePath.TryFromScannedPath(repository, absolutePath, out RepositoryRelativePath path);

		Assert.True(accepted);
		Assert.Equal(_PORTABLE_PATH, path.Value);
		Assert.False(System.IO.Path.IsPathRooted(path.Value));
	}

	[Fact]
	public void ATraversalOutOfTheRepositoryIsRefused()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);
		string traversal = System.IO.Path.Combine(_PARENT_SEGMENT, "elsewhere", "file.cs");

		bool accepted = RepositoryRelativePath.TryFromScannedPath(repository, traversal, out RepositoryRelativePath path);

		Assert.False(accepted);
		Assert.False(path.IsDeclared);
	}

	[Fact]
	public void AnAbsolutePathOutsideTheRepositoryIsRefused()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder otherFolder = TemporaryFolder.Create();
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		string outsidePath = System.IO.Path.Combine(otherFolder.Path, "file.cs");

		bool accepted = RepositoryRelativePath.TryFromScannedPath(repository, outsidePath, out RepositoryRelativePath path);

		Assert.False(accepted);
		Assert.Equal(default, path);
	}

	[Fact]
	public void TheRepositoryRootItselfIsRefused()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);

		bool accepted = RepositoryRelativePath.TryFromScannedPath(repository, repository.CanonicalPath, out RepositoryRelativePath path);

		Assert.False(accepted);
		Assert.False(path.IsDeclared);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void TextThatIsNotAPathIsRefused(string? text)
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);

		bool accepted = RepositoryRelativePath.TryFromScannedPath(repository, text!, out RepositoryRelativePath path);

		Assert.False(accepted);
		Assert.False(path.IsDeclared);
	}

	[Fact]
	public void TheDefaultInstanceNamesNoFile()
	{
		RepositoryRelativePath path = default;

		Assert.False(path.IsDeclared);
		Assert.Null(path.Value);
	}
}
