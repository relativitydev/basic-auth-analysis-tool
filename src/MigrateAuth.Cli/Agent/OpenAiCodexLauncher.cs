using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// Launches OpenAI Codex. Its documented interactive form takes the prompt as the one positional argument;
/// the subcommand that runs without a session is never used.
/// </summary>
public sealed class OpenAiCodexLauncher(
	IAgentSearchPathProvider searchPathProvider,
	RepositoryFolderPath repository,
	TimeSpan? testBudget)
	: ExternalAgentLauncherBase(AgentEnvironment.OpenAiCodex, searchPathProvider, repository, testBudget)
{
	/// <inheritdoc />
	protected override IReadOnlyList<string> ArrangeArguments(string promptArgument)
	{
		return [promptArgument];
	}
}
