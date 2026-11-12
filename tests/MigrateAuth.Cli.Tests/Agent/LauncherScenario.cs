using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// Everything a launcher test needs and nothing it should not have: a repository folder, a documentation
/// destination, a folder of fake agents, and the search path the launcher is given, which is the fake folder
/// and any folders the test names. No search path here is the machine's own, and every launcher a scenario
/// makes is first checked by the guard of the test resolver, so a command found outside the test's temporary
/// folders fails the test before anything is started.
/// </summary>
internal sealed class LauncherScenario : IDisposable
{
	private const string _DESTINATION_FOLDER_NAME = "docs";
	private const string _PACK_FILE_NAME = "ai-assist-pack.md";
	private const string _PACK_CONTENT = "pack";

	private readonly TemporaryFolder _repository = TemporaryFolder.Create();
	private readonly TemporaryFolder _destinationParent = TemporaryFolder.Create();

	internal LauncherScenario(string destinationFolderName = _DESTINATION_FOLDER_NAME)
	{
		DestinationPath = Path.Combine(_destinationParent.Path, destinationFolderName);
		Directory.CreateDirectory(DestinationPath);
	}

	/// <summary>The folder of fake agents.</summary>
	internal FakeAgentFolder Fakes { get; } = new();

	/// <summary>The canonical path of the repository, which is the working directory of a launch.</summary>
	internal string RepositoryPath => _repository.Path;

	/// <summary>The documentation destination.</summary>
	internal string DestinationPath { get; }

	/// <summary>The repository as the launcher takes it.</summary>
	internal RepositoryFolderPath Repository => RepositoryFolderPath.FromSuppliedText(_repository.Path);

	/// <summary>Creates a launcher over the fake folder, plus any extra search path entries, after the guard.</summary>
	internal IExternalAgentLauncher CreateLauncher(
		AgentEnvironment environment,
		TimeSpan? testBudget = null,
		params string[] extraEntries)
	{
		TestAgentSearchPathProvider provider = new([Fakes.FolderPath, .. extraEntries]);

		return CreateLauncher(environment, provider, testBudget);
	}

	/// <summary>Creates a launcher over a provider the test keeps hold of, after the guard.</summary>
	internal IExternalAgentLauncher CreateLauncher(
		AgentEnvironment environment,
		TestAgentSearchPathProvider provider,
		TimeSpan? testBudget = null)
	{
		string commandName = AgentEnvironmentCatalog.Describe(environment).CommandName;
		TestAgentSearchPathProvider guardProvider = new(provider.PeekEntries());

		TestAgentResolver.Resolve(guardProvider, _repository.Path, commandName);

		return AgentLauncherSwitch.Create(environment, provider, Repository, testBudget);
	}

	/// <summary>Creates a launch folder under the destination, writes a pack copy in it and returns its path.</summary>
	internal PromptPackFilePath WriteLaunchCopy()
	{
		return WritePackIn(LaunchCopyLayout.NewFolder(DestinationPath));
	}

	/// <summary>Names the pack a folder would hold, without touching the file system.</summary>
	internal static PromptPackFilePath DeclarePackIn(string folderPath)
	{
		ArtifactWriteResult written = new()
		{
			Wrote = true,
			FileNameWritten = _PACK_FILE_NAME,
			Refusal = ArtifactWriteRefusal.None,
			RefusalMessage = string.Empty
		};

		Assert.True(
			PromptPackFilePath.TryForWrittenPack(
				DocumentationDestinationFolderPath.FromSuppliedText(folderPath),
				written,
				out PromptPackFilePath pack));

		return pack;
	}

	private static PromptPackFilePath WritePackIn(string folderPath)
	{
		File.WriteAllText(Path.Combine(folderPath, _PACK_FILE_NAME), _PACK_CONTENT);

		return DeclarePackIn(folderPath);
	}

	public void Dispose()
	{
		Fakes.Dispose();
		_repository.Dispose();
		_destinationParent.Dispose();
	}
}
