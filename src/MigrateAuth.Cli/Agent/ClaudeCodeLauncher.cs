using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// Launches Claude Code. Its documented way to start an interactive session with an initial prompt is the
/// prompt as the one positional argument; the headless form is a separate switch that is never used.
/// </summary>
public sealed class ClaudeCodeLauncher(
	IAgentSearchPathProvider searchPathProvider,
	RepositoryFolderPath repository,
	TimeSpan? testBudget)
	: ExternalAgentLauncherBase(AgentEnvironment.ClaudeCode, searchPathProvider, repository, testBudget)
{
	/// <inheritdoc />
	protected override IReadOnlyList<string> ArrangeArguments(string promptArgument)
	{
		return [promptArgument];
	}
}
