using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE COST NOTICE IS IN THE PACK, with every element, for each environment: its own account with the named provider and
/// possible charges, billed by the provider and not by this tool, outside the engagement's contract, growing with the
/// repository and the number of findings, with the count of entries.
/// </summary>
public sealed class PromptPackNoticeTests
{
	public static TheoryData<AgentEnvironment, string, string> Environments => new()
	{
		{ AgentEnvironment.ClaudeCode, "Claude Code", "Anthropic" },
		{ AgentEnvironment.OpenAiCodex, "OpenAI Codex", "OpenAI" },
		{ AgentEnvironment.GitHubCopilotCli, "GitHub Copilot CLI", "GitHub" }
	};

	[Theory]
	[MemberData(nameof(Environments))]
	public void EveryElementOfTheCostNoticeIsInThePack(AgentEnvironment environment, string agent, string provider)
	{
		string pack = PromptPackFixture.Body(environment, PromptPackFixture.Scope());

		Assert.Contains($"Launching the review runs {agent} under your own subscription or account with {provider}", pack, StringComparison.Ordinal);
		Assert.Contains($"and {provider} may charge you for it", pack, StringComparison.Ordinal);
		Assert.Contains($"Those charges are billed by {provider}; they are not charges for this tool", pack, StringComparison.Ordinal);
		Assert.Contains("they are outside this engagement's contract", pack, StringComparison.Ordinal);
		Assert.Contains("The cost grows with the size of the repository and with the number of findings under review", pack, StringComparison.Ordinal);
		Assert.Contains("this pack lists 6 entries", pack, StringComparison.Ordinal);
	}

	[Fact]
	public void TheCountIsGroupedTheSameOnEveryMachine()
	{
		Assert.Contains("this pack lists 50,000 entries", AiAssistTexts.CostNotice(AgentEnvironment.ClaudeCode, 50000), StringComparison.Ordinal);
	}

	[Fact]
	public void TheNoticeIsTheSameFunctionOnTheConsoleAndInThePack()
	{
		foreach (AgentEnvironment environment in Enum.GetValues<AgentEnvironment>())
		{
			Assert.Contains(AiAssistTexts.CostNotice(environment, 6), PromptPackFixture.Body(environment, PromptPackFixture.Scope()), StringComparison.Ordinal);
		}
	}

	[Fact]
	public void TheLaunchSentenceIsTheLaunchersOwnConstant()
	{
		Assert.Same(ExternalAgentLauncherBase.LAUNCH_SENTENCE, AiAssistTexts.LAUNCH_SENTENCE);
	}

	[Theory]
	[MemberData(nameof(Environments))]
	public void TheRepositoryAccessStatementHoldsItsElementsAndIsTrueWithoutAPermissionFlag(AgentEnvironment environment, string agent, string provider)
	{
		string statement = AiAssistTexts.RepositoryAccessStatement(environment);

		Assert.Contains($"{agent} is started in the repository folder", statement, StringComparison.Ordinal);
		Assert.Contains($"What the agent reads is sent to {provider}", statement, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.PermissionsSentence(environment), statement, StringComparison.Ordinal);
		Assert.Contains(AiAssistTexts.AccessSentence(environment), statement, StringComparison.Ordinal);
		Assert.Contains("Generating the pack alone sends nothing", statement, StringComparison.Ordinal);
		Assert.DoesNotContain("--", statement, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Environments))]
	public void TheAgentSettingsNoticeSaysTheAgentRunsWithItsOwnSettingsAndHowToChangeThemBeforeConfirming(AgentEnvironment environment, string agent, string provider)
	{
		string[] points = AiAssistTexts.AgentSettingsPoints(environment);

		Assert.NotEmpty(provider);
		Assert.Equal($"{agent} runs the review with its own current settings, including its permission mode. This tool does not change them.", points[0]);
		Assert.Equal(AiAssistTexts.PermissionsSentence(environment), points[1]);
		Assert.Equal($"To change them, answer No, adjust them in {agent}, and run ai-assist again.", points[^1]);
		Assert.Equal(AiAssistTexts.AGENT_SETTINGS_TITLE, AiAssistTexts.AgentSettingsLines(environment)[0]);
	}
}
