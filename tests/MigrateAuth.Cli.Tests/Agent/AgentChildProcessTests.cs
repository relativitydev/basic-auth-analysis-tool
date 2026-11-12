using System.Diagnostics;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The one child process of a launch: how its start information is built for a direct program and for a
/// Windows script wrapper (inspected on every platform, without starting anything), and how a real fake is
/// started, waited for, ended by a test budget and left behind. Every process started here is a fake copied
/// into the test's own temporary folder and reached through the injected search path.
/// </summary>
public sealed class AgentChildProcessTests
{
	private const string _COMMAND_NAME = "claude";
	private const string _FAKE_NAME_STEM = "claude";
	private const string _COMMAND_INTERPRETER = "cmd.exe";
	private const string _PROMPT_ARGUMENT = "a prompt with spaces";
	private const int _EXIT_CODE = 21;
	private const int _BUDGET_SECONDS = 5;
	private const char _CONTROL_CHARACTER = '\u0007';
	private const string _ODD_DESTINATION_NAME = "odd & name %x 'y'";

	private static AgentResolution ResolutionFor(string executablePath, bool isWrapper)
	{
		return new AgentResolution
		{
			WasFound = true,
			ExecutablePath = executablePath,
			IsWrapperScript = isWrapper,
			EntryPosition = 1,
			EntryCount = 1,
			MatchInsideRepositoryWasIgnored = false
		};
	}

	[Fact]
	public void ADirectLaunchIsAnArgumentListWithoutAShell()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		string program = Path.Combine(repository.Path, "..", "tool.exe");

		ProcessStartInfo startInfo = AgentChildProcess.CreateStartInfo(
			ResolutionFor(program, isWrapper: false),
			["-i", _PROMPT_ARGUMENT],
			repository.Path,
			[]);

		Assert.Equal(program, startInfo.FileName);
		Assert.Equal(["-i", _PROMPT_ARGUMENT], startInfo.ArgumentList);
		Assert.Equal(string.Empty, startInfo.Arguments);
		Assert.False(startInfo.UseShellExecute);
		Assert.Equal(repository.Path, startInfo.WorkingDirectory);
	}

	[Fact]
	public void AWrapperLaunchIsTheCommandInterpreterFromTheSystemFolderWithOneConstructedLine()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		string wrapper = Path.Combine(repository.Path, "..", "tool.cmd");

		ProcessStartInfo startInfo = AgentChildProcess.CreateStartInfo(
			ResolutionFor(wrapper, isWrapper: true),
			[_PROMPT_ARGUMENT],
			repository.Path,
			[]);

		Assert.Equal(Path.Combine(Environment.SystemDirectory, _COMMAND_INTERPRETER), startInfo.FileName);
		Assert.Empty(startInfo.ArgumentList);
		Assert.Equal(AgentCommandLineSafety.BuildWrapperCommandLine(wrapper, [_PROMPT_ARGUMENT]), startInfo.Arguments);
		Assert.StartsWith("/d /s /c ", startInfo.Arguments, StringComparison.Ordinal);
		Assert.False(startInfo.UseShellExecute);
		Assert.Equal(repository.Path, startInfo.WorkingDirectory);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void NothingIsRedirectedSoTheChildInheritsTheTerminal(bool isWrapper)
	{
		using TemporaryFolder repository = TemporaryFolder.Create();

		ProcessStartInfo startInfo = AgentChildProcess.CreateStartInfo(
			ResolutionFor(Path.Combine(repository.Path, "..", "tool.exe"), isWrapper),
			[_PROMPT_ARGUMENT],
			repository.Path,
			[]);

		Assert.False(startInfo.RedirectStandardInput);
		Assert.False(startInfo.RedirectStandardOutput);
		Assert.False(startInfo.RedirectStandardError);
		Assert.False(startInfo.CreateNoWindow);
	}

	[Fact]
	public void ADirectLaunchOfTheFakeReportsItsExitCodeAndStartsInTheRepository()
	{
		using LauncherScenario scenario = new();
		string executable = scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior { ExitCode = _EXIT_CODE });
		AgentResolution resolution = TestAgentResolver.Resolve([scenario.Fakes.FolderPath], scenario.RepositoryPath, _COMMAND_NAME);
		ProcessStartInfo startInfo = AgentChildProcess.CreateStartInfo(resolution, [_PROMPT_ARGUMENT], scenario.RepositoryPath, [scenario.Fakes.FolderPath]);

		AgentChildResult result = AgentChildProcess.Run(startInfo, testBudget: null);
		FakeAgentRecord record = FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}");

		Assert.True(result.Started);
		Assert.False(result.TimedOut);
		Assert.Equal(_EXIT_CODE, result.ExitCode);
		Assert.Equal(scenario.RepositoryPath, record.WorkingDirectory);
		Assert.Equal([_PROMPT_ARGUMENT], record.Arguments);
	}

	[Fact]
	public void AProgramThatDoesNotExistIsAResultAndNotAnException()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		ProcessStartInfo startInfo = AgentChildProcess.CreateStartInfo(
			ResolutionFor(Path.Combine(repository.Path, FakeAgentFolder.ExecutableName($"absent-{Guid.NewGuid():N}")), isWrapper: false),
			[],
			repository.Path,
			[]);

		AgentChildResult result = AgentChildProcess.Run(startInfo, testBudget: null);

		Assert.Equal(AgentChildResult.NotStarted, result);
		Assert.False(result.Started);
	}

	[Fact]
	public void ALaunchWhoseProcessCannotBeStartedKeepsTheNotFoundStatus()
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(_FAKE_NAME_STEM, new FakeAgentBehavior());
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);
		PromptPackFilePath copy = scenario.WriteLaunchCopy();

		Assert.Equal(AgentAvailability.Available, launcher.CheckAvailability());
		File.Delete(Path.Combine(scenario.Fakes.FolderPath, FakeAgentFolder.ExecutableName(_FAKE_NAME_STEM)));

		Assert.Equal(AgentTerminationStatus.NotFound(), launcher.Launch(copy));
	}

	[Fact]
	public void ATestBudgetThatRunsOutKillsTheProcessAndLeavesNoSurvivor()
	{
		using LauncherScenario scenario = new();
		string executable = scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior { Ending = FakeAgentEnding.Hang });
		AgentResolution resolution = TestAgentResolver.Resolve([scenario.Fakes.FolderPath], scenario.RepositoryPath, _COMMAND_NAME);
		ProcessStartInfo startInfo = AgentChildProcess.CreateStartInfo(resolution, [], scenario.RepositoryPath, [scenario.Fakes.FolderPath]);

		AgentChildResult result = AgentChildProcess.Run(startInfo, TimeSpan.FromSeconds(_BUDGET_SECONDS));

		Assert.True(result.Started);
		Assert.True(result.TimedOut);
		Assert.False(FakeAgentFolder.IsAnyRunningFrom(executable), "The fake agent survived the end of its test budget.");
	}

	[Fact]
	public void ADirectLaunchIgnoresTheWrapperAllowListBecauseNoInterpreterReadsTheLine()
	{
		using LauncherScenario scenario = new(_ODD_DESTINATION_NAME);
		string executable = scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior());
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);
		PromptPackFilePath copy = scenario.WriteLaunchCopy();

		Assert.False(AgentCommandLineSafety.IsAllowedText(copy.AbsolutePath));
		Assert.Equal(AgentLaunchEligibility.Launchable, launcher.CheckLaunchable(copy));
		Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(copy));
		Assert.Equal(
			[ExternalAgentLauncherBase.BuildPromptArgument(copy.AbsolutePath)],
			FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}").Arguments);
	}

	[Fact]
	public void AControlCharacterInTheCopyPathWithholdsTheOfferOnEveryPlatform()
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior());
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);
		string folderWithControlCharacter = Path.Combine(scenario.DestinationPath, $"docs{_CONTROL_CHARACTER}bell");

		Assert.Equal(
			AgentLaunchEligibility.PathContainsControlCharacter,
			launcher.CheckLaunchable(LauncherScenario.DeclarePackIn(folderWithControlCharacter)));
	}
}
