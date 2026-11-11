using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Agent;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// AN AGENT THAT IS NOT FOUND, THROUGH THE REAL PROCESS: the pack is still written, the report still gets its
/// section, the not-found line says what was searched, and nothing is offered or started.
/// </summary>
public sealed class AiAssistNotFoundProcessTests
{
	public static TheoryData<AgentEnvironment> Environments => new()
	{
		AgentEnvironment.ClaudeCode,
		AgentEnvironment.OpenAiCodex,
		AgentEnvironment.GitHubCopilotCli
	};

	[Theory]
	[MemberData(nameof(Environments))]
	public void AnEmptySearchPathWritesThePackPrintsTheNotFoundLineAndOffersNothing(AgentEnvironment environment)
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, AiAssistProcessSupport.SelectionNumberOf(environment), AiAssistProcessSupport.ExitCommand));

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Contains(AiAssistProcessSupport.SearchingLine(environment), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Contains(AiAssistProcessSupport.NotFoundLine(environment), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.NO_TERMINAL, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.Confirmation(environment), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Equal([$"{AgentEnvironmentCatalog.Describe(environment).PackFileNameStem}.md"], AiAssistProcessSupport.PackNames(destination));
		Assert.Empty(AiAssistProcessSupport.LaunchFolders(destination));
	}

	[Fact]
	public void AnAgentOfAnotherEnvironmentDoesNotCountAsFoundAndIsNeverStarted()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();
		using FakeAgentFolder fakes = new();

		AiAssistProcessSupport.WriteRepository(repository);
		fakes.Install(AgentEnvironmentCatalog.Describe(AgentEnvironment.OpenAiCodex).CommandName, new FakeAgentBehavior());

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, "1", AiAssistProcessSupport.ExitCommand),
			fakes.FolderPath);

		Assert.Contains(AiAssistProcessSupport.NotFoundLine(AgentEnvironment.ClaudeCode), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.False(AiAssistProcessSupport.FakeWasStarted(fakes, AgentEnvironment.OpenAiCodex));
		Assert.Single(AiAssistProcessSupport.PackNames(destination));
	}
}
