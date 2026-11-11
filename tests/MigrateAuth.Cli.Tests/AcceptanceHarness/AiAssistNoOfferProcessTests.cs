using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Agent;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE NO-OFFER CASES A REAL PROCESS CAN REACH. A process started by a test has redirected input and output, so the
/// terminal case is always reachable, and a command found only inside the repository can be arranged with a file in
/// the repository. The cases that need a terminal first (an unsafe command line, a control character, a wrapper from a
/// non-local repository) are driven in process, where the terminal predicate is forced true.
/// </summary>
public sealed class AiAssistNoOfferProcessTests
{
	[Fact]
	public void RedirectedInputAndOutputWithholdTheOfferWithItsFixedLineAndTheFakeIsNotStarted()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();
		using FakeAgentFolder fakes = new();

		AiAssistProcessSupport.WriteRepository(repository);
		fakes.Install(AgentEnvironmentCatalog.Describe(AgentEnvironment.GitHubCopilotCli).CommandName, new FakeAgentBehavior());

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, "3", AiAssistProcessSupport.ExitCommand),
			fakes.FolderPath);

		Assert.Contains(AiAssistTexts.NO_TERMINAL, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.STARTED_DIRECTLY, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.False(AiAssistProcessSupport.FakeWasStarted(fakes, AgentEnvironment.GitHubCopilotCli));
		Assert.Single(AiAssistProcessSupport.PackNames(destination));
		Assert.Empty(AiAssistProcessSupport.LaunchFolders(destination));
	}

	[Fact]
	public void ACommandFoundOnlyInsideTheRepositoryIsIgnoredWithItsFixedLineAndThePackIsWritten()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		string insideRepository = repository.CreateFolder(AiAssistHarness.REPOSITORY_TOOLS_FOLDER);

		TestAgentResolver.WriteCommandFile(
			insideRepository,
			FakeAgentFolder.ExecutableName(AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).CommandName));

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, "1", AiAssistProcessSupport.ExitCommand),
			insideRepository);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Contains(AiAssistProcessSupport.MatchInsideRepositoryLine(AgentEnvironment.ClaudeCode), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.NO_TERMINAL, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Single(AiAssistProcessSupport.PackNames(destination));
		Assert.Empty(AiAssistProcessSupport.LaunchFolders(destination));
		AiAssistProcessSupport.AssertNoAbsolutePath(outcome, repository.RootPath, insideRepository);
	}
}
