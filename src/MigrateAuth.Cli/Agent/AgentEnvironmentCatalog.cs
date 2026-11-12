using System.Collections.Immutable;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// The one declaration of the three environments' static descriptors. Nothing here is computed or read:
/// the numbers, names, providers, commands and pack stems are compiled constants, listed in selection order.
/// </summary>
public static class AgentEnvironmentCatalog
{
	/// <summary>The pack stem of <see cref="AgentEnvironment.ClaudeCode"/>.</summary>
	public const string CLAUDE_CODE_PACK_STEM = "ai-assist-claude-code";

	/// <summary>The pack stem of <see cref="AgentEnvironment.OpenAiCodex"/>.</summary>
	public const string OPENAI_CODEX_PACK_STEM = "ai-assist-openai-codex";

	/// <summary>The pack stem of <see cref="AgentEnvironment.GitHubCopilotCli"/>.</summary>
	public const string COPILOT_CLI_PACK_STEM = "ai-assist-copilot-cli";

	private static readonly AgentEnvironmentDescriptor _claudeCode = new()
	{
		Environment = AgentEnvironment.ClaudeCode,
		SelectionNumber = 1,
		DisplayName = "Claude Code",
		AcceptedNames = ["claude code", "claude"],
		ProviderName = "Anthropic",
		CommandName = "claude",
		PackFileNameStem = CLAUDE_CODE_PACK_STEM
	};

	private static readonly AgentEnvironmentDescriptor _openAiCodex = new()
	{
		Environment = AgentEnvironment.OpenAiCodex,
		SelectionNumber = 2,
		DisplayName = "OpenAI Codex",
		AcceptedNames = ["openai codex", "codex"],
		ProviderName = "OpenAI",
		CommandName = "codex",
		PackFileNameStem = OPENAI_CODEX_PACK_STEM
	};

	private static readonly AgentEnvironmentDescriptor _gitHubCopilotCli = new()
	{
		Environment = AgentEnvironment.GitHubCopilotCli,
		SelectionNumber = 3,
		DisplayName = "GitHub Copilot CLI",
		AcceptedNames = ["github copilot cli", "copilot"],
		ProviderName = "GitHub",
		CommandName = "copilot",
		PackFileNameStem = COPILOT_CLI_PACK_STEM
	};

	/// <summary>The descriptors in the order the selection list shows them.</summary>
	public static ImmutableArray<AgentEnvironmentDescriptor> All { get; } =
		[_claudeCode, _openAiCodex, _gitHubCopilotCli];

	/// <summary>The descriptor of <paramref name="environment"/>. An undefined value is a programming error.</summary>
	public static AgentEnvironmentDescriptor Describe(AgentEnvironment environment)
	{
		AgentEnvironmentDescriptor descriptor = environment switch
		{
			AgentEnvironment.ClaudeCode => _claudeCode,
			AgentEnvironment.OpenAiCodex => _openAiCodex,
			AgentEnvironment.GitHubCopilotCli => _gitHubCopilotCli,
			_ => throw new ArgumentOutOfRangeException(nameof(environment))
		};

		return descriptor;
	}
}
