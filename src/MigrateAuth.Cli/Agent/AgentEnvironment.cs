using System.Collections.Immutable;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// The AI agent environments the tool can write a prompt pack for and launch. A closed enumeration: every
/// switch over it is exhaustive, so a fourth environment cannot be added without the compiler naming every
/// place that must answer for it. The values are not the numbers the user types; see
/// <see cref="AgentEnvironmentDescriptor.SelectionNumber"/>.
/// </summary>
public enum AgentEnvironment
{
	/// <summary>Claude Code.</summary>
	ClaudeCode = 0,

	/// <summary>OpenAI Codex, or a Codex-style agent.</summary>
	OpenAiCodex = 1,

	/// <summary>GitHub Copilot CLI.</summary>
	GitHubCopilotCli = 2
}

/// <summary>
/// The static facts of one environment. Every value is a compiled constant: none is read from input, from
/// the repository or from configuration, which is what lets the pack stem be used as a file name without
/// validating it as one. The per-environment console and pack TEXT is not here; it lives with the texts,
/// keyed by <see cref="AgentEnvironment"/>.
/// </summary>
public sealed record AgentEnvironmentDescriptor
{
	/// <summary>The environment this describes.</summary>
	public required AgentEnvironment Environment { get; init; }

	/// <summary>The number the user types in the selection list, starting at one.</summary>
	public required int SelectionNumber { get; init; }

	/// <summary>The name used wherever the environment is named in text.</summary>
	public required string DisplayName { get; init; }

	/// <summary>The names the selection accepts, compared ignoring case after trimming.</summary>
	public required ImmutableArray<string> AcceptedNames { get; init; }

	/// <summary>The company the user's account is with.</summary>
	public required string ProviderName { get; init; }

	/// <summary>The command the launcher resolves; the command name only, never a path.</summary>
	public required string CommandName { get; init; }

	/// <summary>The prompt pack's file name without extension and without a date.</summary>
	public required string PackFileNameStem { get; init; }
}
