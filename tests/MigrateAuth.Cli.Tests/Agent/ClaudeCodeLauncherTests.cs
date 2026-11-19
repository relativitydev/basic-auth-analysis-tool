using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The Claude Code launcher against its fake. DOCUMENTATION STAMP: the launcher was built from the vendor's
/// published command-line reference, titled below, at the address below, as read on the date below. No
/// vendor address is a product constant; this class is the only place the stamp is carried.
/// </summary>
public sealed class ClaudeCodeLauncherTests
{
	private const string _DOCUMENTATION_TITLE = "CLI reference";
	private const string _DOCUMENTATION_URL = "https://code.claude.com/docs/en/cli-reference";
	private const string _DOCUMENTATION_ACCESS_DATE = "2026-10-28";
	private const string _COMMAND_NAME = "claude";
	private const int _FAKE_EXIT_CODE = 7;

	/// <summary>
	/// The flags the documentation lists that a launch must never carry: every way to skip, grant or
	/// change permissions, to run without a session, to add directories, settings, servers, plugins, agents or
	/// system prompts, or to start remote control. The permission-mode switch is forbidden in every value.
	/// </summary>
	private static readonly string[] _forbiddenArguments =
	[
		"--dangerously-skip-permissions",
		"--allow-dangerously-skip-permissions",
		"--permission-mode",
		"--allowedTools",
		"--allowed-tools",
		"--settings",
		"-p",
		"--print",
		"--permission-prompts",
		"--bare",
		"--dangerously-load-development-channels",
		"--add-dir",
		"--mcp-config",
		"--strict-mcp-config",
		"--plugin-dir",
		"--agents",
		"--append-system-prompt",
		"--append-system-prompt-file",
		"--system-prompt",
		"--system-prompt-file",
		"--remote-control",
		"--rc"
	];

	[Fact]
	public void ThisClassCarriesItsDocumentationStamp()
	{
		LauncherVectorChecks.AssertStamp(_DOCUMENTATION_TITLE, _DOCUMENTATION_URL, _DOCUMENTATION_ACCESS_DATE);
	}

	[Fact]
	public void TheVectorIsTheOnePositionalPromptAndNothingElse()
	{
		LauncherVectorChecks.AssertDocumentedVector(AgentEnvironment.ClaudeCode, [], _forbiddenArguments);
	}

	[Fact]
	public void TheLauncherResolvesTheCommandNamedClaude()
	{
		Assert.Equal(_COMMAND_NAME, AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).CommandName);
	}

	[Fact]
	public void TheLaunchReportsTheExitCodeTheFakeEndedWith()
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior { ExitCode = _FAKE_EXIT_CODE });

		AgentTerminationStatus status = scenario.CreateLauncher(AgentEnvironment.ClaudeCode).Launch(scenario.WriteLaunchCopy());

		Assert.Equal(AgentTerminationStatus.ExitedWithCode(_FAKE_EXIT_CODE), status);
	}

	[Fact]
	public void ALaunchOfAnAgentThatIsGoneBeforeTheLaunchKeepsTheNotFoundStatus()
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior());
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);

		Assert.Equal(AgentAvailability.Available, launcher.CheckAvailability());
		File.Delete(Path.Combine(scenario.Fakes.FolderPath, FakeAgentFolder.ExecutableName(_COMMAND_NAME)));

		Assert.Equal(AgentTerminationStatus.NotFound(), launcher.Launch(scenario.WriteLaunchCopy()));
	}
}
