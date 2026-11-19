using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// Launches GitHub Copilot CLI. Its documented way to start an interactive session that runs an initial
/// prompt is the interactive switch followed by the prompt; a positional prompt is not documented and the
/// programmatic switch is never used.
/// </summary>
public sealed class GitHubCopilotCliLauncher(
	IAgentSearchPathProvider searchPathProvider,
	RepositoryFolderPath repository,
	TimeSpan? testBudget)
	: ExternalAgentLauncherBase(AgentEnvironment.GitHubCopilotCli, searchPathProvider, repository, testBudget)
{
	/// <summary>The switch that starts an interactive session and runs the prompt that follows it.</summary>
	public const string INTERACTIVE_SWITCH = "-i";

	/// <inheritdoc />
	protected override IReadOnlyList<string> ArrangeArguments(string promptArgument)
	{
		return [INTERACTIVE_SWITCH, promptArgument];
	}
}
