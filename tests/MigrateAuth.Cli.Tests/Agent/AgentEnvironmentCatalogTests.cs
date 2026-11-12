using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The static environment descriptors: three of them in selection order, with the number, names, provider,
/// command and pack stem each is declared with, every stem a bare file name, and an exhaustive answer for every enumeration value.
/// </summary>
public sealed class AgentEnvironmentCatalogTests
{
	private const int _ENVIRONMENT_COUNT = 3;

	public static TheoryData<AgentEnvironment, int, string, string, string, string> ExpectedDescriptors => new()
	{
		{ AgentEnvironment.ClaudeCode, 1, "Claude Code", "Anthropic", "claude", "ai-assist-claude-code" },
		{ AgentEnvironment.OpenAiCodex, 2, "OpenAI Codex", "OpenAI", "codex", "ai-assist-openai-codex" },
		{ AgentEnvironment.GitHubCopilotCli, 3, "GitHub Copilot CLI", "GitHub", "copilot", "ai-assist-copilot-cli" }
	};

	[Fact]
	public void TheEnumerationHasExactlyTheThreeEnvironments()
	{
		Assert.Equal(_ENVIRONMENT_COUNT, Enum.GetValues<AgentEnvironment>().Length);
		Assert.Equal(_ENVIRONMENT_COUNT, AgentEnvironmentCatalog.All.Length);
	}

	[Fact]
	public void TheDescriptorsAreListedInSelectionOrderWithConsecutiveNumbersFromOne()
	{
		Assert.Equal(
			[AgentEnvironment.ClaudeCode, AgentEnvironment.OpenAiCodex, AgentEnvironment.GitHubCopilotCli],
			AgentEnvironmentCatalog.All.Select(descriptor => descriptor.Environment));
		Assert.Equal([1, 2, 3], AgentEnvironmentCatalog.All.Select(descriptor => descriptor.SelectionNumber));
	}

	[Theory]
	[MemberData(nameof(ExpectedDescriptors))]
	public void EachDescriptorCarriesItsDeclaredFacts(
		AgentEnvironment environment,
		int number,
		string displayName,
		string provider,
		string command,
		string stem)
	{
		AgentEnvironmentDescriptor descriptor = AgentEnvironmentCatalog.Describe(environment);

		Assert.Equal(environment, descriptor.Environment);
		Assert.Equal(number, descriptor.SelectionNumber);
		Assert.Equal(displayName, descriptor.DisplayName);
		Assert.Equal(provider, descriptor.ProviderName);
		Assert.Equal(command, descriptor.CommandName);
		Assert.Equal(stem, descriptor.PackFileNameStem);
		Assert.Same(descriptor, AgentEnvironmentCatalog.All.Single(candidate => candidate.Environment == environment));
	}

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode, "claude code", "claude")]
	[InlineData(AgentEnvironment.OpenAiCodex, "openai codex", "codex")]
	[InlineData(AgentEnvironment.GitHubCopilotCli, "github copilot cli", "copilot")]
	public void EachDescriptorAcceptsItsNamesAsDeclared(AgentEnvironment environment, string longName, string shortName)
	{
		Assert.Equal([longName, shortName], AgentEnvironmentCatalog.Describe(environment).AcceptedNames.ToArray());
	}

	[Fact]
	public void TheStemConstantsAreTheDeclaredLiteralsAndTheDescriptorsUseThem()
	{
		Assert.Equal("ai-assist-claude-code", AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM);
		Assert.Equal("ai-assist-openai-codex", AgentEnvironmentCatalog.OPENAI_CODEX_PACK_STEM);
		Assert.Equal("ai-assist-copilot-cli", AgentEnvironmentCatalog.COPILOT_CLI_PACK_STEM);
		Assert.Equal(
			[
				AgentEnvironmentCatalog.CLAUDE_CODE_PACK_STEM,
				AgentEnvironmentCatalog.OPENAI_CODEX_PACK_STEM,
				AgentEnvironmentCatalog.COPILOT_CLI_PACK_STEM
			],
			AgentEnvironmentCatalog.All.Select(descriptor => descriptor.PackFileNameStem));
	}

	[Fact]
	public void EveryStemIsADistinctBareFileNameWithoutADateOrAnExtension()
	{
		char[] forbidden = [.. Path.GetInvalidFileNameChars(), '/', '\\', '.'];

		List<string> stems = [.. AgentEnvironmentCatalog.All.Select(descriptor => descriptor.PackFileNameStem)];

		Assert.Equal(stems.Count, stems.Distinct(StringComparer.Ordinal).Count());
		Assert.All(stems, stem => Assert.True(stem.IndexOfAny(forbidden) < 0, stem));
		Assert.All(stems, stem => Assert.DoesNotMatch(@"\d", stem));
	}

	[Fact]
	public void EveryNameAndNumberIsDistinctAcrossTheEnvironments()
	{
		IEnumerable<string> names = AgentEnvironmentCatalog.All.SelectMany(descriptor => descriptor.AcceptedNames);

		Assert.Equal(names.Count(), names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.Equal(
			AgentEnvironmentCatalog.All.Length,
			AgentEnvironmentCatalog.All.Select(descriptor => descriptor.CommandName).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void AnEnvironmentValueThatIsNotDeclaredIsAProgrammingError()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => AgentEnvironmentCatalog.Describe((AgentEnvironment)_ENVIRONMENT_COUNT));
	}
}
