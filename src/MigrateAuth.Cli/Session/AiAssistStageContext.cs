using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// What the <c>ai-assist</c> stage is built from: one record, every member required, the pattern the report
/// stage's context follows. Everything the stage can reach is named here, and each seam is a plain
/// constructor-time value, set once where the session is composed.
///
/// THE TWO SEAMS OF THE CONTEXT ARE <see cref="LauncherFor"/> and <see cref="IsInteractiveTerminal"/>; the launcher
/// it builds is the only reader of the search path. None is chosen by an argument, an environment variable or a
/// configuration key; the shipped values are built in the one composition root and tests supply their own.
/// </summary>
public sealed record AiAssistStageContext
{
	/// <summary>The console the stage reads its two answers from and writes everything to.</summary>
	public required SessionConsole Console { get; init; }

	/// <summary>The assessed repository, which is the agent's working directory and which no output may reach.</summary>
	public required RepositoryFolderPath Repository { get; init; }

	/// <summary>The documentation destination: where the pack and the launch folder are written.</summary>
	public required DocumentationDestinationFolderPath Destination { get; init; }

	/// <summary>The embedded catalog, which decides which route each finding takes.</summary>
	public required LanguageCatalog Catalog { get; init; }

	/// <summary>The report's minimum confidence level: the pack lists the findings the report lists.</summary>
	public required ConfidenceLevel MinimumConfidenceLevel { get; init; }

	/// <summary>The writer of the pack, over the destination.</summary>
	public required AtomicArtifactWriter ArtifactWriter { get; init; }

	/// <summary>The record of the report this process wrote, which names the report in the pack and gates the append.</summary>
	public required ReportWriteHolder ReportHolder { get; init; }

	/// <summary>Turns an environment into its launcher.</summary>
	public required Func<AgentEnvironment, IExternalAgentLauncher> LauncherFor { get; init; }

	/// <summary>Whether input and output are both an interactive terminal.</summary>
	public required Func<bool> IsInteractiveTerminal { get; init; }
}
