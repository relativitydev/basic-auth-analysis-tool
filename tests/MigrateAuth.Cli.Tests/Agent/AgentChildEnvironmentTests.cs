using System.Diagnostics;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The environment of the child differs from the inherited one in two ways only: every search path entry
/// inside the repository is removed (an empty or relative entry means the working directory, so it is resolved
/// against it first), and on Windows the variable that stops the current directory being searched for programs
/// is set. Entries that stay are kept exactly as written.
/// </summary>
public sealed class AgentChildEnvironmentTests
{
	private const string _COMMAND_NAME = "claude";
	private const string _UNRELATED_VARIABLE = "AGENT_CHILD_ENVIRONMENT_TEST_UNRELATED";
	private const string _UNRELATED_VALUE = "kept as it was";
	private const string _INSIDE_FOLDER_NAME = "tools";
	private const string _LINK_NAME = "linked-into-repository";
	private const string _SIBLING_FOLDER_NAME = "sibling-of-the-repository";
	private const string _PARENT_ENTRY = "..";
	private const string _NULL_CHARACTER_ENTRY = "bad\0entry";

	[Fact]
	public void EntriesInsideTheRepositoryAreRemovedAndTheRestKeepTheirOrderAndText()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder outside = TemporaryFolder.Create();
		string inside = Directory.CreateDirectory(Path.Combine(repository.Path, _INSIDE_FOLDER_NAME)).FullName;
		string[] entries = [outside.Path, repository.Path, inside, $"{outside.Path}{Path.DirectorySeparatorChar}"];

		IReadOnlyList<string> kept = AgentChildEnvironment.KeepEntriesOutsideRepository(entries, repository.Path);

		Assert.Equal([outside.Path, $"{outside.Path}{Path.DirectorySeparatorChar}"], kept);
	}

	[Fact]
	public void AnEmptyOrRelativeEntryMeansTheWorkingDirectoryAndIsRemovedWhenItLandsInsideTheRepository()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		Directory.CreateDirectory(Path.Combine(repository.Path, _INSIDE_FOLDER_NAME));
		string[] entries = ["", " ", ".", _INSIDE_FOLDER_NAME, Path.Combine(".", _INSIDE_FOLDER_NAME), Path.Combine(_INSIDE_FOLDER_NAME, "..", _INSIDE_FOLDER_NAME)];

		Assert.Empty(AgentChildEnvironment.KeepEntriesOutsideRepository(entries, repository.Path));
	}

	[Fact]
	public void ARelativeEntryThatLandsOutsideTheRepositoryIsKeptAsWritten()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();

		IReadOnlyList<string> kept = AgentChildEnvironment.KeepEntriesOutsideRepository([_PARENT_ENTRY], repository.Path);

		Assert.Equal([_PARENT_ENTRY], kept);
	}

	[Fact]
	public void ASiblingFolderWhoseNameStartsLikeTheRepositoryIsNotInsideIt()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		string sibling = Directory.CreateDirectory($"{repository.Path}-{_SIBLING_FOLDER_NAME}").FullName;

		try
		{
			Assert.Equal([sibling], AgentChildEnvironment.KeepEntriesOutsideRepository([sibling], repository.Path));
		}
		finally
		{
			Directory.Delete(sibling, recursive: true);
		}
	}

	[Fact]
	public void AnEntryThatCannotBeReadAsAPathIsRemoved()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder outside = TemporaryFolder.Create();

		Assert.Equal(
			[outside.Path],
			AgentChildEnvironment.KeepEntriesOutsideRepository([_NULL_CHARACTER_ENTRY, outside.Path], repository.Path));
	}

	[Fact]
	public void AnEntryThatIsALinkIntoTheRepositoryIsRemoved()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder outside = TemporaryFolder.Create();
		string linkPath = Path.Combine(outside.Path, _LINK_NAME);

		if (TestAgentResolver.TryCreateDirectoryLink(linkPath, repository.Path))
		{
			Assert.Empty(AgentChildEnvironment.KeepEntriesOutsideRepository([linkPath], repository.Path));
		}
	}

	[Fact]
	public void ApplyingTheEnvironmentChangesTheSearchPathAndNothingElseButTheWindowsVariable()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder outside = TemporaryFolder.Create();
		ProcessStartInfo baseline = new("unused");
		ProcessStartInfo startInfo = new("unused");

		baseline.Environment.Remove(AgentChildEnvironment.NO_CURRENT_DIRECTORY_VARIABLE);
		startInfo.Environment.Remove(AgentChildEnvironment.NO_CURRENT_DIRECTORY_VARIABLE);
		baseline.Environment[_UNRELATED_VARIABLE] = _UNRELATED_VALUE;
		startInfo.Environment[_UNRELATED_VARIABLE] = _UNRELATED_VALUE;
		AgentChildEnvironment.Apply(startInfo, [repository.Path, outside.Path], repository.Path);

		string[] changedNames =
		[
			.. startInfo.Environment.Keys
				.Union(baseline.Environment.Keys, StringComparer.OrdinalIgnoreCase)
				.Where(name => !string.Equals(startInfo.Environment[name], baseline.Environment.TryGetValue(name, out string? before) ? before : null, StringComparison.Ordinal))
				.Select(name => name.ToUpperInvariant())
				.Order(StringComparer.Ordinal)
		];
		string[] expected = OperatingSystem.IsWindows()
			? [AgentChildEnvironment.NO_CURRENT_DIRECTORY_VARIABLE.ToUpperInvariant(), AgentChildEnvironment.SEARCH_PATH_VARIABLE]
			: [AgentChildEnvironment.SEARCH_PATH_VARIABLE];

		Assert.Equal(outside.Path, startInfo.Environment[AgentChildEnvironment.SEARCH_PATH_VARIABLE]);
		Assert.Equal(_UNRELATED_VALUE, startInfo.Environment[_UNRELATED_VARIABLE]);
		Assert.Equal(expected.Order(StringComparer.Ordinal), changedNames);
	}

	[Fact]
	public void TheWindowsVariableIsSetThereAndNeverElsewhere()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		ProcessStartInfo startInfo = new("unused");

		startInfo.Environment.Remove(AgentChildEnvironment.NO_CURRENT_DIRECTORY_VARIABLE);
		AgentChildEnvironment.Apply(startInfo, [], repository.Path);

		Assert.Equal(
			OperatingSystem.IsWindows() ? AgentChildEnvironment.NO_CURRENT_DIRECTORY_VALUE : null,
			startInfo.Environment.TryGetValue(AgentChildEnvironment.NO_CURRENT_DIRECTORY_VARIABLE, out string? value) ? value : null);
	}

	[Fact]
	public void TheLaunchedFakeSeesTheFilteredSearchPathAndTheWindowsVariable()
	{
		using LauncherScenario scenario = new();
		string insideEntry = Directory.CreateDirectory(Path.Combine(scenario.RepositoryPath, _INSIDE_FOLDER_NAME)).FullName;
		string executable = scenario.Fakes.Install(
			_COMMAND_NAME,
			new FakeAgentBehavior
			{
				WatchedEnvironmentNames = [AgentChildEnvironment.SEARCH_PATH_VARIABLE, AgentChildEnvironment.NO_CURRENT_DIRECTORY_VARIABLE]
			});
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode, null, insideEntry, string.Empty);

		Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(scenario.WriteLaunchCopy()));

		FakeAgentRecord record = FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}");

		Assert.Equal(scenario.Fakes.FolderPath, record.WatchedEnvironment[AgentChildEnvironment.SEARCH_PATH_VARIABLE]);
		Assert.Equal(
			OperatingSystem.IsWindows() ? AgentChildEnvironment.NO_CURRENT_DIRECTORY_VALUE : null,
			record.WatchedEnvironment[AgentChildEnvironment.NO_CURRENT_DIRECTORY_VARIABLE]);
	}
}
