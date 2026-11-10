using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// A FOLDER NAMED <c>bin</c> IS BUILD OUTPUT, AND STAYS EXCLUDED, ONLY WHEN ITS PARENT HOLDS A
/// COMPILED PROJECT FILE; EVERY OTHER <c>bin</c> IS SCANNED LIKE ANY OTHER FOLDER.
/// Every fixture here uses generic names; the decision rests on file names alone.
/// </summary>
public sealed class BinFolderWalkTests
{
	private const string _SCRIPT_CONTENT = "echo ready\n";
	private const string _PROJECT_CONTENT = "<Project />\n";
	private const string _GENERATED_CONTENT = "generated\n";

	[Theory]
	[InlineData("App.csproj")]
	[InlineData("App.vbproj")]
	[InlineData("App.fsproj")]
	public void ABinFolderBesideACompiledProjectFileIsExcluded(string projectFileName)
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText($"app/{projectFileName}", _PROJECT_CONTENT);
		fixture.WriteText("app/bin/output.dll.txt", _GENERATED_CONTENT);
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);

		List<string> walked = [.. walk.Enumerate().Select(file => file.Path.Value)];

		Assert.Equal([$"app/{projectFileName}"], walked);
		Assert.Equal(["app/bin"], walk.ExcludedFolders().Paths.Select(path => path.Value));
		Assert.Equal(1, walk.ExcludedFolders().OccurrenceCount);
	}

	[Fact]
	public void ABinFolderWithNoProjectFileInItsParentIsScannedAndItsScriptIsReached()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText("tools/bin/deploy.ps1", _SCRIPT_CONTENT);
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);

		List<string> walked = [.. walk.Enumerate().Select(file => file.Path.Value)];

		Assert.Equal(["tools/bin/deploy.ps1"], walked);
		Assert.Equal(0, walk.ExcludedFolders().OccurrenceCount);
	}

	[Fact]
	public void ABinFolderAtTheRootOfARepositoryWithoutAProjectFileIsScanned()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText("bin/run.sh", _SCRIPT_CONTENT);

		Assert.Equal(["bin/run.sh"], fixture.WalkedPaths());
	}

	/// <summary>
	/// The project file sits at an ancestor, not at the folder that holds the <c>bin</c>: the rule
	/// looks at the parent only.
	/// </summary>
	[Fact]
	public void ANestedBinWhoseProjectFileIsOnlyAtAnAncestorIsScanned()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText("app/App.csproj", _PROJECT_CONTENT);
		fixture.WriteText("app/tools/bin/deploy.ps1", _SCRIPT_CONTENT);

		List<string> walked = fixture.WalkedPaths();

		Assert.Contains("app/tools/bin/deploy.ps1", walked);
		Assert.Contains("app/App.csproj", walked);
	}

	[Fact]
	public void OnlyTheBinBesideTheProjectFileIsExcludedWhenTwoExistInOneTree()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText("app/App.csproj", _PROJECT_CONTENT);
		fixture.WriteText("app/bin/output.txt", _GENERATED_CONTENT);
		fixture.WriteText("scripts/bin/deploy.ps1", _SCRIPT_CONTENT);

		List<string> walked = fixture.WalkedPaths();

		Assert.Equal(["app/App.csproj", "scripts/bin/deploy.ps1"], walked);
	}

	/// <summary>
	/// The folder name is compared ordinally and case-sensitively on every platform, as every other
	/// name on the list is, so <c>BIN</c> is not build output.
	/// </summary>
	[Fact]
	public void AnUppercaseBinFolderIsNotExcludedEvenBesideAProjectFile()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText("app/App.csproj", _PROJECT_CONTENT);
		fixture.WriteText("app/BIN/deploy.ps1", _SCRIPT_CONTENT);

		Assert.Contains("app/BIN/deploy.ps1", fixture.WalkedPaths());
	}

	/// <summary>
	/// The project extension is compared ordinally and case-insensitively, as language extensions
	/// are, so the answer does not depend on the platform.
	/// </summary>
	[Fact]
	public void AProjectFileWithAnUppercaseExtensionStillMakesTheBinBuildOutput()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText("app/App.CSPROJ", _PROJECT_CONTENT);
		fixture.WriteText("app/bin/output.txt", _GENERATED_CONTENT);

		Assert.Equal(["app/App.CSPROJ"], fixture.WalkedPaths());
	}

	[Fact]
	public void AFolderThatMerelyCarriesAProjectNameIsNotAProjectFile()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.CreateFolder("app/Fake.csproj");
		fixture.WriteText("app/bin/deploy.ps1", _SCRIPT_CONTENT);

		Assert.Contains("app/bin/deploy.ps1", fixture.WalkedPaths());
	}

	[Fact]
	public void AProjectFileOfAnotherExtensionDoesNotExcludeTheBin()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText("app/App.csproj.user", _PROJECT_CONTENT);
		fixture.WriteText("app/bin/deploy.ps1", _SCRIPT_CONTENT);

		Assert.Contains("app/bin/deploy.ps1", fixture.WalkedPaths());
	}

	/// <summary>
	/// The decision is made from names alone: a project file the tool is denied the right to open
	/// still makes its sibling <c>bin</c> build output, and no denial is recorded for it, because it
	/// is never opened.
	/// </summary>
	[Fact]
	public void TheDecisionOpensNoFileSoAnUnreadableProjectFileStillExcludesTheBin()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		string projectPath = fixture.WriteText("app/App.csproj", _PROJECT_CONTENT);

		fixture.WriteText("app/bin/output.txt", _GENERATED_CONTENT);
		fixture.Deny(projectPath);
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);

		List<string> walked = [.. walk.Enumerate().Select(file => file.Path.Value)];

		Assert.Equal(["app/App.csproj"], walked);
		Assert.Equal(1, walk.ExcludedFolders().OccurrenceCount);
		Assert.False(fixture.Ledger.RecordedAnything);
	}

	[Fact]
	public void TheNameAloneNoLongerExcludesBin()
	{
		Assert.False(WalkExclusions.IsExcluded("bin"));
		Assert.Contains("bin", WalkExclusions.EXCLUDED_DIRECTORY_NAMES);
	}
}
