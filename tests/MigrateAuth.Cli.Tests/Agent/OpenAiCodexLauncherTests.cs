using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The OpenAI Codex launcher against its fake. DOCUMENTATION STAMP: the launcher was built from the vendor's
/// published command reference, titled below, at the address below, as read on the date below. The page shows
/// no literal example of a positional prompt, so the shape here is the documented description (an optional
/// text instruction that opens the interactive session) and is unverified against the real tool. No vendor
/// address is a product constant; this class is the only place the stamp is carried.
/// </summary>
public sealed class OpenAiCodexLauncherTests
{
	private const string _DOCUMENTATION_TITLE = "Developer commands (CLI)";
	private const string _DOCUMENTATION_URL = "https://learn.chatgpt.com/docs/developer-commands?surface=cli";
	private const string _DOCUMENTATION_ACCESS_DATE = "2026-10-28";
	private const string _COMMAND_NAME = "codex";
	private const int _FAKE_EXIT_CODE = 9;

	/// <summary>
	/// The flags and subcommands the documentation lists that a launch must never carry: bypassing
	/// approvals and the sandbox, choosing the approval policy or the sandbox, the full-automatic mode (its
	/// existence is unverified, so it is denied anyway), configuration overrides, profiles, added
	/// directories, and the subcommands that run without a session.
	/// </summary>
	private static readonly string[] _forbiddenArguments =
	[
		"--dangerously-bypass-approvals-and-sandbox",
		"--yolo",
		"--ask-for-approval",
		"-a",
		"--sandbox",
		"-s",
		"--approve-for-me",
		"--not-so-yolo",
		"--full-auto",
		"-c",
		"--config",
		"--profile",
		"-p",
		"--add-dir",
		"exec",
		"e"
	];

	[Fact]
	public void ThisClassCarriesItsDocumentationStamp()
	{
		LauncherVectorChecks.AssertStamp(_DOCUMENTATION_TITLE, _DOCUMENTATION_URL, _DOCUMENTATION_ACCESS_DATE);
	}

	[Fact]
	public void TheVectorIsTheOnePositionalPromptAndNothingElse()
	{
		LauncherVectorChecks.AssertDocumentedVector(AgentEnvironment.OpenAiCodex, [], _forbiddenArguments);
	}

	[Fact]
	public void TheLauncherResolvesTheCommandNamedCodex()
	{
		Assert.Equal(_COMMAND_NAME, AgentEnvironmentCatalog.Describe(AgentEnvironment.OpenAiCodex).CommandName);
	}

	[Fact]
	public void TheLaunchReportsTheExitCodeTheFakeEndedWith()
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior { ExitCode = _FAKE_EXIT_CODE });

		AgentTerminationStatus status = scenario.CreateLauncher(AgentEnvironment.OpenAiCodex).Launch(scenario.WriteLaunchCopy());

		Assert.Equal(AgentTerminationStatus.ExitedWithCode(_FAKE_EXIT_CODE), status);
	}
}
