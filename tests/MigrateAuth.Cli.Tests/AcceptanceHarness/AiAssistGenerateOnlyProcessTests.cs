using System.Text;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Agent;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// GENERATE-ONLY, THROUGH THE REAL PROCESS: with redirected input and output the stage writes the pack, adds its
/// section to the report this process wrote, finds the agent the child's search path holds, says that no launch is
/// offered, and starts nothing.
/// </summary>
public sealed class AiAssistGenerateOnlyProcessTests
{
	[Fact]
	public void ThePackIsWrittenTheSectionAppendedAndTheFoundAgentIsNeverStarted()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder control = TemporaryFolder.Create();
		using TemporaryFolder destination = TemporaryFolder.Create();
		using FakeAgentFolder fakes = new();

		AiAssistProcessSupport.WriteRepository(repository);
		fakes.Install(AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).CommandName, new FakeAgentBehavior());
		AiAssistProcessSupport.Run(repository, control, AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.ExitCommand));

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, "1", AiAssistProcessSupport.ExitCommand),
			fakes.FolderPath);

		string[] packs = AiAssistProcessSupport.PackNames(destination);
		byte[] controlReport = AiAssistProcessSupport.ReportBytesWithoutRunDate(control);
		byte[] report = AiAssistProcessSupport.ReportBytesWithoutRunDate(destination);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Single(packs);
		Assert.Contains(AiAssistProcessSupport.SearchingLine(AgentEnvironment.ClaudeCode), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Contains(AiAssistTexts.NO_TERMINAL, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.Confirmation(AgentEnvironment.ClaudeCode), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.False(AiAssistProcessSupport.FakeWasStarted(fakes, AgentEnvironment.ClaudeCode));
		Assert.Empty(AiAssistProcessSupport.LaunchFolders(destination));
		Assert.True(report.AsSpan().StartsWith(controlReport), "The report keeps every byte it had; the section follows them.");

		string appended = Encoding.UTF8.GetString(AiAssistProcessSupport.ReportBytes(destination), controlReport.Length, report.Length - controlReport.Length);

		Assert.Contains(AiAssistTexts.REVIEW_LABEL, appended, StringComparison.Ordinal);
		Assert.Contains(packs[0], appended, StringComparison.Ordinal);
		Assert.Equal(AiAssistProcessSupport.DataBytes(control), AiAssistProcessSupport.DataBytes(destination));
		AiAssistProcessSupport.AssertNoAbsolutePath(outcome, repository.RootPath, fakes.FolderPath);
	}

	[Fact]
	public void AFoundAgentOfAnotherEnvironmentIsNotStartedEitherAndNoAbsolutePathIsPrinted()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();
		using FakeAgentFolder fakes = new();

		AiAssistProcessSupport.WriteRepository(repository);
		fakes.Install(AgentEnvironmentCatalog.Describe(AgentEnvironment.OpenAiCodex).CommandName, new FakeAgentBehavior());

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, "2", AiAssistProcessSupport.ExitCommand),
			fakes.FolderPath);

		Assert.Contains(AiAssistTexts.NO_TERMINAL, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.False(AiAssistProcessSupport.FakeWasStarted(fakes, AgentEnvironment.OpenAiCodex));
		AiAssistProcessSupport.AssertNoAbsolutePath(outcome, repository.RootPath, fakes.FolderPath);
	}
}
