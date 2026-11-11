using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// The one place an environment becomes a launcher: an exhaustive switch over the enumeration, with every
/// implementation named statically. Nothing is created by reflection, which would not survive ahead-of-time
/// compilation.
/// </summary>
public static class AgentLauncherSwitch
{
	/// <summary>
	/// Creates the launcher for <paramref name="environment"/>. A null <paramref name="testBudget"/> waits for
	/// the agent without limit, which is the shipped behavior; a budget exists for tests, and when it runs out the
	/// process tree is killed.
	/// </summary>
	public static IExternalAgentLauncher Create(
		AgentEnvironment environment,
		IAgentSearchPathProvider searchPathProvider,
		RepositoryFolderPath repository,
		TimeSpan? testBudget)
	{
		IExternalAgentLauncher launcher = environment switch
		{
			AgentEnvironment.ClaudeCode => new ClaudeCodeLauncher(searchPathProvider, repository, testBudget),
			AgentEnvironment.OpenAiCodex => new OpenAiCodexLauncher(searchPathProvider, repository, testBudget),
			AgentEnvironment.GitHubCopilotCli => new GitHubCopilotCliLauncher(searchPathProvider, repository, testBudget),
			_ => throw new ArgumentOutOfRangeException(nameof(environment))
		};

		return launcher;
	}
}
