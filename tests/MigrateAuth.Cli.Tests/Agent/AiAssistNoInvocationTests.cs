using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// NO AGENT IS EVER STARTED, AND NO CHECK STARTS ONE: with fakes installed for all three commands, the record folder stays
/// empty in a generate-only run (no terminal), a not-found run, a declined run and a cancelled run. Checking availability
/// and eligibility is a file-system question only, and the fakes prove it by never having run.
/// </summary>
public sealed class AiAssistNoInvocationTests
{
	private static void InstallAllThree(AiAssistHarness harness)
	{
		foreach (AgentEnvironment environment in Enum.GetValues<AgentEnvironment>())
		{
			harness.InstallFake(environment, new FakeAgentBehavior());
		}
	}

	[Theory]
	[InlineData("1")]
	[InlineData("2")]
	[InlineData("3")]
	public void AGenerateOnlyRunWithNoTerminalStartsNoAgent(string selection)
	{
		using AiAssistHarness harness = new([selection, "y"]);

		InstallAllThree(harness);
		harness.IsTerminal = false;
		harness.Run();

		harness.AssertNothingWasLaunched();
		Assert.Contains(AiAssistTexts.NO_TERMINAL, harness.Transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void ANotFoundRunStartsNoAgent()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.OpenAiCodex);
		harness.InstallFake(AgentEnvironment.GitHubCopilotCli);
		harness.Run();

		harness.AssertNothingWasLaunched();
		Assert.Contains($"ai-assist: {AiAssistHarness.ClaudeCommandName} was not found on the PATH. The prompt pack was written and can be run later or elsewhere; nothing was launched.", harness.Transcript, StringComparison.Ordinal);
		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
	}

	[Theory]
	[InlineData("")]
	[InlineData("n")]
	public void ADeclinedRunStartsNoAgent(string answer)
	{
		using AiAssistHarness harness = new(["2", answer]);

		InstallAllThree(harness);
		harness.Run();

		harness.AssertNothingWasLaunched();
	}

	[Fact]
	public void ACancelledSelectionStartsNoAgent()
	{
		using AiAssistHarness harness = new([string.Empty]);

		InstallAllThree(harness);
		harness.Run();

		harness.AssertNothingWasLaunched();
		Assert.Empty(harness.PackFiles());
	}

	[Fact]
	public void TheAvailabilityCheckReadsTheSearchPathAndNeverStartsAnything()
	{
		using AiAssistHarness harness = new(["3", "n"]);

		InstallAllThree(harness);
		harness.Run();

		Assert.True(harness.SearchPath.ReadCount >= 1);
		harness.AssertNothingWasLaunched();
	}
}
