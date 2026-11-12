using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The GitHub Copilot CLI launcher against its fake. DOCUMENTATION STAMP: the launcher was built from the
/// vendor's published command reference, titled below, at the address below, as read on the date below. The
/// documentation describes the interactive switch as starting an interactive session that runs the prompt that
/// follows it, and documents no positional prompt, so the switch is passed and the positional form is not. No
/// vendor address is a product constant; this class is the only place the stamp is carried.
/// </summary>
public sealed class GitHubCopilotCliLauncherTests
{
	private const string _DOCUMENTATION_TITLE = "Command-line commands";
	private const string _DOCUMENTATION_URL = "https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference";
	private const string _DOCUMENTATION_ACCESS_DATE = "2026-10-28";
	private const string _COMMAND_NAME = "copilot";
	private const string _INTERACTIVE_SWITCH = "-i";
	private const int _FAKE_EXIT_CODE = 11;

	/// <summary>
	/// The flags the documentation lists that a launch must never carry: every allow-all form, tool and address
	/// allowances, the autonomous mode, the mode switch, folder trust, the programmatic prompt, added
	/// directories, extra servers and plugins, and the tightening flags, because a command-line permission choice
	/// overrides the user's own in either direction.
	/// </summary>
	private static readonly string[] _forbiddenArguments =
	[
		"--allow-all",
		"--yolo",
		"--allow-all-tools",
		"--allow-all-paths",
		"--allow-all-urls",
		"--allow-tool",
		"--allow-url",
		"--autopilot",
		"--mode",
		"--trust-folder",
		"-p",
		"--prompt",
		"--add-dir",
		"--additional-mcp-config",
		"--plugin-dir",
		"--disallow-temp-dir",
		"--deny-tool",
		"--sandbox",
		"--no-custom-instructions"
	];

	[Fact]
	public void ThisClassCarriesItsDocumentationStamp()
	{
		LauncherVectorChecks.AssertStamp(_DOCUMENTATION_TITLE, _DOCUMENTATION_URL, _DOCUMENTATION_ACCESS_DATE);
	}

	[Fact]
	public void TheVectorIsTheInteractiveSwitchThenTheOnePromptArgument()
	{
		LauncherVectorChecks.AssertDocumentedVector(AgentEnvironment.GitHubCopilotCli, [_INTERACTIVE_SWITCH], _forbiddenArguments);
	}

	[Fact]
	public void TheInteractiveSwitchIsTheLaunchersOwnConstant()
	{
		Assert.Equal(_INTERACTIVE_SWITCH, GitHubCopilotCliLauncher.INTERACTIVE_SWITCH);
	}

	[Fact]
	public void TheLauncherResolvesTheCommandNamedCopilot()
	{
		Assert.Equal(_COMMAND_NAME, AgentEnvironmentCatalog.Describe(AgentEnvironment.GitHubCopilotCli).CommandName);
	}

	[Fact]
	public void TheLaunchReportsTheExitCodeTheFakeEndedWith()
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior { ExitCode = _FAKE_EXIT_CODE });

		AgentTerminationStatus status = scenario.CreateLauncher(AgentEnvironment.GitHubCopilotCli).Launch(scenario.WriteLaunchCopy());

		Assert.Equal(AgentTerminationStatus.ExitedWithCode(_FAKE_EXIT_CODE), status);
	}
}
