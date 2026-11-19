using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// On Linux every launch is direct: the resolved file is started by its absolute path, with the arguments
/// in an argument list, in the repository, whether it is a program or a script with an interpreter line. No
/// wrapper rule applies, so the wrapper allow-list and the drive-letter rule never withhold a launch there. The
/// search path of the child holds no entry inside the repository, so a script that calls a helper by its bare
/// name gets the helper outside the repository.
///
/// Every test body is a no-op on a platform other than Linux.
/// </summary>
public sealed class LinuxDirectLaunchTests
{
	private const string _COMMAND_NAME = "claude";
	private const string _SCRIPT_HEADER = "#!/bin/sh\n";
	private const string _ARGUMENT_RECORD_SUFFIX = ".args";
	private const string _DIRECTORY_RECORD_SUFFIX = ".cwd";
	private const string _HELPER_NAME = "helper";
	private const string _REPOSITORY_TOOLS_FOLDER = "tools";
	private const string _ODD_DESTINATION_NAME = "odd & name %x 'y'";
	private const UnixFileMode _EXECUTABLE_MODE =
		UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
	private const UnixFileMode _NOT_EXECUTABLE_MODE = UnixFileMode.UserRead | UnixFileMode.UserWrite;

	[Fact]
	public void AScriptAgentIsStartedDirectlyWithItsArgumentsIntactInTheRepository()
	{
		if (OperatingSystem.IsLinux())
		{
			using LauncherScenario scenario = new(_ODD_DESTINATION_NAME);
			string script = WriteScript(
				scenario.Fakes.FolderPath,
				_COMMAND_NAME,
				$"printf '%s\\n' \"$@\" > \"$0{_ARGUMENT_RECORD_SUFFIX}\"\npwd > \"$0{_DIRECTORY_RECORD_SUFFIX}\"\n");
			PromptPackFilePath copy = scenario.WriteLaunchCopy();
			IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);

			Assert.Equal(AgentLaunchEligibility.Launchable, launcher.CheckLaunchable(copy));
			Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(copy));
			Assert.Equal(
				[ExternalAgentLauncherBase.BuildPromptArgument(copy.AbsolutePath)],
				File.ReadAllLines($"{script}{_ARGUMENT_RECORD_SUFFIX}"));
			Assert.Equal([scenario.RepositoryPath], File.ReadAllLines($"{script}{_DIRECTORY_RECORD_SUFFIX}"));
		}
	}

	[Fact]
	public void AFakeProgramIsStartedDirectlyWithTheDocumentedVector()
	{
		if (OperatingSystem.IsLinux())
		{
			LauncherVectorChecks.AssertDocumentedVector(AgentEnvironment.GitHubCopilotCli, ["-i"], []);
		}
	}

	[Fact]
	public void AFileWithoutAnExecuteBitIsNotAnAgentAndIsNotLaunched()
	{
		if (OperatingSystem.IsLinux())
		{
			using LauncherScenario scenario = new();
			string executable = scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior());

			File.SetUnixFileMode(executable, _NOT_EXECUTABLE_MODE);
			IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);

			Assert.Equal(AgentAvailability.NotFound, launcher.CheckAvailability());
			Assert.Equal(AgentTerminationStatus.NotFound(), launcher.Launch(scenario.WriteLaunchCopy()));
			Assert.False(File.Exists($"{executable}{FakeAgentRecord.FILE_SUFFIX}"));
		}
	}

	[Fact]
	public void NoWrapperRuleWithholdsALaunchOnLinux()
	{
		if (OperatingSystem.IsLinux())
		{
			using LauncherScenario scenario = new(_ODD_DESTINATION_NAME);

			scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior());

			Assert.False(WrapperWorkingDirectoryRule.AllowsWrapperLaunch(scenario.RepositoryPath));
			Assert.Equal(
				AgentLaunchEligibility.Launchable,
				scenario.CreateLauncher(AgentEnvironment.ClaudeCode).CheckLaunchable(scenario.WriteLaunchCopy()));
		}
	}

	[Fact]
	public void ABareHelperIsNeverTakenFromASearchPathEntryInsideTheRepository()
	{
		if (OperatingSystem.IsLinux())
		{
			using LauncherScenario scenario = new();
			string goodHelper = InstallHelper(scenario.Fakes.FolderPath);
			string repositoryTools = Directory.CreateDirectory(Path.Combine(scenario.RepositoryPath, _REPOSITORY_TOOLS_FOLDER)).FullName;
			string hostileHelper = InstallHelper(repositoryTools);

			WriteScript(scenario.Fakes.FolderPath, _COMMAND_NAME, $"{_HELPER_NAME}\n");
			IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode, null, repositoryTools);

			Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(scenario.WriteLaunchCopy()));
			Assert.True(File.Exists($"{goodHelper}{FakeAgentRecord.FILE_SUFFIX}"), "The helper on the search path outside the repository did not run.");
			Assert.False(File.Exists($"{hostileHelper}{FakeAgentRecord.FILE_SUFFIX}"), "The helper in the repository's own search path entry ran.");
		}
	}

	private static string WriteScript(string folderPath, string name, string body)
	{
		string path = Path.Combine(folderPath, name);

		File.WriteAllText(path, $"{_SCRIPT_HEADER}{body}");

		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(path, _EXECUTABLE_MODE);
		}

		return path;
	}

	private static string InstallHelper(string folderPath)
	{
		string helperPath = Path.Combine(folderPath, _HELPER_NAME);

		File.Copy(FakeAgentFolder.PublishedFakePath(), helperPath);
		new FakeAgentBehavior().WriteFor(helperPath);

		return helperPath;
	}
}
