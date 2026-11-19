using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// A Windows script wrapper, launched through the command interpreter with a fake behind it. Pack paths with
/// spaces launch; a path with a character outside the allow-list is never launched through a wrapper; a wrapper
/// that calls a bare helper runs the helper found on the search path outside the repository and never one the
/// repository placed in the working directory or in a search path entry of its own; and a repository that is
/// not on a local drive letter withholds a wrapper launch while a direct program is unaffected.
///
/// Every test body is a no-op on a platform other than Windows: there is no script wrapper there.
/// </summary>
public sealed class WindowsWrapperLaunchTests
{
	private const string _HELPER_NAME = "helper";
	private const string _HELPER_FILE_NAME = "helper.exe";
	private const string _WRAPPER_FILE_NAME = "claude.cmd";
	private const string _WRAPPER_CALLING_A_BARE_HELPER = "@helper\r\n@exit /b %ERRORLEVEL%\r\n";
	private const string _WRAPPER_CALLING_ITS_TARGET = "@\"%~dp0claude-target.exe\" %*\r\n@exit /b %ERRORLEVEL%\r\n";
	private const string _TARGET_FILE_NAME = "claude-target.exe";
	private const string _FOLDER_WITH_SPACES = "pack folder with spaces";
	private const string _TOOLS_FOLDER_WITH_SPACES = "agent tools with spaces";
	private const string _REPOSITORY_TOOLS_FOLDER = "tools";
	private const int _OVER_LENGTH_COMPONENT = 150;
	private const string _NON_LOCAL_PREFIX = @"\\localhost\";
	private const char _ADMIN_SHARE_MARKER = '$';
	private const int _DRIVE_PREFIX_LENGTH = 2;
	private const int _DRIVE_LETTER_INDEX = 0;
	private const int _DRIVE_COLON_INDEX = 1;
	private const char _DRIVE_COLON = ':';

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode, "claude", new string[0])]
	[InlineData(AgentEnvironment.OpenAiCodex, "codex", new string[0])]
	[InlineData(AgentEnvironment.GitHubCopilotCli, "copilot", new[] { "-i" })]
	public void APackPathWithSpacesLaunchesThroughTheWrapperWithTheDocumentedVector(
		AgentEnvironment environment,
		string commandName,
		string[] leadingArguments)
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new(_FOLDER_WITH_SPACES);
			PromptPackFilePath copy = scenario.WriteLaunchCopy();
			string target = scenario.Fakes.InstallCmdOnly(commandName, new FakeAgentBehavior { PackPath = copy.AbsolutePath });
			IExternalAgentLauncher launcher = scenario.CreateLauncher(environment);

			Assert.Equal(AgentAvailability.Available, launcher.CheckAvailability());
			Assert.Equal(AgentLaunchEligibility.Launchable, launcher.CheckLaunchable(copy));
			Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(copy));

			FakeAgentRecord record = FakeAgentRecord.ReadFrom($"{target}{FakeAgentRecord.FILE_SUFFIX}");

			Assert.Equal([.. leadingArguments, ExternalAgentLauncherBase.BuildPromptArgument(copy.AbsolutePath)], record.Arguments);
			Assert.Equal(scenario.RepositoryPath, record.WorkingDirectory);
			Assert.True(record.PackExists);
		}
	}

	[Fact]
	public void AWrapperPathWithSpacesLaunches()
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new();
			string toolsFolder = Directory.CreateDirectory(Path.Combine(scenario.Fakes.FolderPath, _TOOLS_FOLDER_WITH_SPACES)).FullName;
			string target = Path.Combine(toolsFolder, _TARGET_FILE_NAME);

			File.Copy(FakeAgentFolder.PublishedFakePath(), target);
			new FakeAgentBehavior().WriteFor(target);
			File.WriteAllText(Path.Combine(toolsFolder, _WRAPPER_FILE_NAME), _WRAPPER_CALLING_ITS_TARGET);

			IExternalAgentLauncher launcher = AgentLauncherSwitch.Create(
				AgentEnvironment.ClaudeCode,
				new TestAgentSearchPathProvider([toolsFolder]),
				scenario.Repository,
				testBudget: null);
			PromptPackFilePath copy = scenario.WriteLaunchCopy();

			Assert.Equal(AgentLaunchEligibility.Launchable, launcher.CheckLaunchable(copy));
			Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(copy));
			Assert.Equal(
				[ExternalAgentLauncherBase.BuildPromptArgument(copy.AbsolutePath)],
				FakeAgentRecord.ReadFrom($"{target}{FakeAgentRecord.FILE_SUFFIX}").Arguments);
		}
	}

	[Theory]
	[InlineData("%")]
	[InlineData("&")]
	[InlineData("^")]
	[InlineData("!")]
	[InlineData("(")]
	[InlineData(")")]
	[InlineData(",")]
	[InlineData(";")]
	[InlineData("=")]
	[InlineData("'")]
	[InlineData("\uFF05")]
	[InlineData("\uFF06")]
	[InlineData("\u02BA")]
	public void ACopyPathWithACharacterOutsideTheListIsNotLaunchedThroughAWrapper(string character)
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new($"docs{character}x");
			PromptPackFilePath copy = scenario.WriteLaunchCopy();
			string target = scenario.Fakes.InstallCmdOnly("claude", new FakeAgentBehavior());
			IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);

			Assert.Equal(AgentAvailability.Available, launcher.CheckAvailability());
			Assert.Equal(AgentLaunchEligibility.UnsafeCommandLine, launcher.CheckLaunchable(copy));
			Assert.Equal(AgentTerminationStatus.NotFound(), launcher.Launch(copy));
			Assert.False(File.Exists($"{target}{FakeAgentRecord.FILE_SUFFIX}"));
		}
	}

	[Fact]
	public void AWrapperPathWithACharacterOutsideTheListIsNotLaunchedEither()
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new();
			string toolsFolder = Directory.CreateDirectory(Path.Combine(scenario.Fakes.FolderPath, "tools&more")).FullName;
			string target = Path.Combine(toolsFolder, _TARGET_FILE_NAME);

			File.Copy(FakeAgentFolder.PublishedFakePath(), target);
			new FakeAgentBehavior().WriteFor(target);
			File.WriteAllText(Path.Combine(toolsFolder, _WRAPPER_FILE_NAME), _WRAPPER_CALLING_ITS_TARGET);

			IExternalAgentLauncher launcher = AgentLauncherSwitch.Create(
				AgentEnvironment.ClaudeCode,
				new TestAgentSearchPathProvider([toolsFolder]),
				scenario.Repository,
				testBudget: null);
			PromptPackFilePath copy = scenario.WriteLaunchCopy();

			Assert.Equal(AgentLaunchEligibility.UnsafeCommandLine, launcher.CheckLaunchable(copy));
			Assert.Equal(AgentTerminationStatus.NotFound(), launcher.Launch(copy));
			Assert.False(File.Exists($"{target}{FakeAgentRecord.FILE_SUFFIX}"));
		}
	}

	[Fact]
	public void ABareHelperIsNeverTakenFromTheWorkingDirectory()
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new();
			string goodHelper = InstallBareHelperWrapper(scenario);
			string hostileHelper = InstallHelper(scenario.RepositoryPath);
			IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);

			Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(scenario.WriteLaunchCopy()));
			Assert.True(File.Exists($"{goodHelper}{FakeAgentRecord.FILE_SUFFIX}"), "The helper on the search path outside the repository did not run.");
			Assert.False(File.Exists($"{hostileHelper}{FakeAgentRecord.FILE_SUFFIX}"), "The helper in the working directory ran.");
		}
	}

	[Fact]
	public void ABareHelperIsNeverTakenFromASearchPathEntryInsideTheRepository()
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new();
			string goodHelper = InstallBareHelperWrapper(scenario);
			string repositoryTools = Directory.CreateDirectory(Path.Combine(scenario.RepositoryPath, _REPOSITORY_TOOLS_FOLDER)).FullName;
			string hostileHelper = InstallHelper(repositoryTools);
			IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode, null, repositoryTools);

			Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(scenario.WriteLaunchCopy()));
			Assert.True(File.Exists($"{goodHelper}{FakeAgentRecord.FILE_SUFFIX}"), "The helper on the search path outside the repository did not run.");
			Assert.False(File.Exists($"{hostileHelper}{FakeAgentRecord.FILE_SUFFIX}"), "The helper in the repository's own search path entry ran.");
		}
	}

	[Fact]
	public void AWorkingDirectoryThatIsNotALocalDriveLetterWithholdsAWrapperLaunch()
	{
		if (OperatingSystem.IsWindows() && TryBuildNonLocalRepository(out string nonLocalRepository))
		{
			using LauncherScenario scenario = new();
			PromptPackFilePath copy = scenario.WriteLaunchCopy();
			string target = scenario.Fakes.InstallCmdOnly("claude", new FakeAgentBehavior());
			IExternalAgentLauncher launcher = LauncherOver(scenario, nonLocalRepository);

			Assert.Equal(AgentAvailability.Available, launcher.CheckAvailability());
			Assert.Equal(AgentLaunchEligibility.WrapperNeedsLocalRepository, launcher.CheckLaunchable(copy));
			Assert.Equal(AgentTerminationStatus.NotFound(), launcher.Launch(copy));
			Assert.False(File.Exists($"{target}{FakeAgentRecord.FILE_SUFFIX}"));
		}
	}

	[Fact]
	public void ADirectProgramIsNotWithheldFromANonLocalWorkingDirectory()
	{
		if (OperatingSystem.IsWindows() && TryBuildNonLocalRepository(out string nonLocalRepository))
		{
			using LauncherScenario scenario = new();

			scenario.Fakes.Install("claude", new FakeAgentBehavior());

			Assert.Equal(
				AgentLaunchEligibility.Launchable,
				LauncherOver(scenario, nonLocalRepository).CheckLaunchable(scenario.WriteLaunchCopy()));
		}
	}

	[Fact]
	public void AnOverLongWorkingDirectoryWithholdsAWrapperLaunch()
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new();
			string overLong = Path.Combine(scenario.RepositoryPath, new string('r', _OVER_LENGTH_COMPONENT), new string('s', _OVER_LENGTH_COMPONENT));

			scenario.Fakes.InstallCmdOnly("claude", new FakeAgentBehavior());

			Assert.Equal(
				AgentLaunchEligibility.WrapperNeedsLocalRepository,
				LauncherOver(scenario, overLong).CheckLaunchable(scenario.WriteLaunchCopy()));
		}
	}

	private static IExternalAgentLauncher LauncherOver(LauncherScenario scenario, string repositoryText)
	{
		TestAgentSearchPathProvider provider = new([scenario.Fakes.FolderPath]);

		TestAgentResolver.Resolve(new TestAgentSearchPathProvider(provider.PeekEntries()), scenario.RepositoryPath, "claude");

		return AgentLauncherSwitch.Create(
			AgentEnvironment.ClaudeCode,
			provider,
			RepositoryFolderPath.FromSuppliedText(repositoryText),
			testBudget: null);
	}

	private static string InstallBareHelperWrapper(LauncherScenario scenario)
	{
		File.WriteAllText(Path.Combine(scenario.Fakes.FolderPath, _WRAPPER_FILE_NAME), _WRAPPER_CALLING_A_BARE_HELPER);

		return InstallHelper(scenario.Fakes.FolderPath);
	}

	private static string InstallHelper(string folderPath)
	{
		string helperPath = Path.Combine(folderPath, _HELPER_FILE_NAME);

		File.Copy(FakeAgentFolder.PublishedFakePath(), helperPath);
		new FakeAgentBehavior().WriteFor(helperPath);

		Assert.Equal(_HELPER_NAME, Path.GetFileNameWithoutExtension(helperPath));

		return helperPath;
	}

	/// <summary>
	/// The temporary folder written as a network path through the administrative share of the local machine.
	/// Nothing is opened through it: the launcher only reads the text of the working directory.
	/// </summary>
	private static bool TryBuildNonLocalRepository(out string nonLocalRepository)
	{
		string path = TemporaryFolder.CanonicalRoot;
		bool built = path.Length > _DRIVE_PREFIX_LENGTH && char.IsAsciiLetter(path[_DRIVE_LETTER_INDEX]) && path[_DRIVE_COLON_INDEX] == _DRIVE_COLON;

		nonLocalRepository = built
			? $"{_NON_LOCAL_PREFIX}{path[_DRIVE_LETTER_INDEX]}{_ADMIN_SHARE_MARKER}{path[_DRIVE_PREFIX_LENGTH..]}"
			: string.Empty;

		return built;
	}
}
