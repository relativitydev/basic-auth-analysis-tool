using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE STAGE'S PLACE IN THE ORDER AND ITS DESCRIPTION, THROUGH THE REAL PROCESS: entered before the report it gives the
/// ordinary out-of-order message and changes nothing; entered after the report it runs, and may run again; the
/// persisted state is the same bytes before and after; and the help listing describes what the stage does.
/// </summary>
public sealed class AiAssistGatingProcessTests
{
	private const string _SECTION_HEADING_PREFIX = "## Supplemental: AI-assisted review";

	[Fact]
	public void TheStageEnteredBeforeTheReportGivesTheOrdinaryMessageWritesNothingAndLeavesTheStateAsItWas()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);
		AiAssistProcessSupport.Run(
			repository,
			destination,
			[.. AiAssistProcessSupport.FourStages[..3], AiAssistProcessSupport.ExitCommand]);

		byte[] stateBefore = AiAssistProcessSupport.StateBytes(destination, repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			[AiAssistProcessSupport.AiAssistCommand, AiAssistProcessSupport.ExitCommand]);

		Assert.Contains(CommandRegistry.FormatGatingMessage(CommandId.Report), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.SELECTION_HEADING, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Empty(AiAssistProcessSupport.PackNames(destination));
		Assert.Equal(stateBefore, AiAssistProcessSupport.StateBytes(destination, repository));
	}

	[Fact]
	public void TheStageEnteredInTheSameSessionBeforeTheReportIsGatedToo()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			[.. AiAssistProcessSupport.FourStages[..3], AiAssistProcessSupport.AiAssistCommand, AiAssistProcessSupport.ExitCommand]);

		Assert.Contains(CommandRegistry.FormatGatingMessage(CommandId.Report), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Empty(AiAssistProcessSupport.PackNames(destination));
	}

	[Fact]
	public void AfterTheReportTheStageRunsAndMayRunAgainAndTheStateIsTheSameBytes()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);
		AiAssistProcessSupport.Run(repository, destination, AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.ExitCommand));

		byte[] stateBefore = AiAssistProcessSupport.StateBytes(destination, repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			[
				AiAssistProcessSupport.AiAssistCommand,
				AiAssistProcessSupport.SelectionNumberOf(AgentEnvironment.ClaudeCode),
				AiAssistProcessSupport.AiAssistCommand,
				AiAssistProcessSupport.SelectionNumberOf(AgentEnvironment.OpenAiCodex),
				AiAssistProcessSupport.ExitCommand
			]);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Equal(2, AiAssistProcessSupport.CountSelectionPrompts(outcome.StandardOutput));
		Assert.Equal(
			[
				$"{AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).PackFileNameStem}.md",
				$"{AgentEnvironmentCatalog.Describe(AgentEnvironment.OpenAiCodex).PackFileNameStem}.md"
			],
			AiAssistProcessSupport.PackNames(destination).Order(StringComparer.Ordinal));
		Assert.Equal(stateBefore, AiAssistProcessSupport.StateBytes(destination, repository));
	}

	[Fact]
	public void ARunAfterTheReportOfTheSameSessionAppendsOneSectionPerRunInOrder()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(
				AiAssistProcessSupport.AiAssistCommand,
				AiAssistProcessSupport.SelectionNumberOf(AgentEnvironment.ClaudeCode),
				AiAssistProcessSupport.AiAssistCommand,
				AiAssistProcessSupport.SelectionNumberOf(AgentEnvironment.GitHubCopilotCli),
				AiAssistProcessSupport.ExitCommand));

		string report = System.Text.Encoding.UTF8.GetString(AiAssistProcessSupport.ReportBytes(destination));
		int first = report.IndexOf(_SECTION_HEADING_PREFIX, StringComparison.Ordinal);
		int second = report.IndexOf(_SECTION_HEADING_PREFIX, first + 1, StringComparison.Ordinal);

		Assert.True(first >= 0 && second > first, "Two sections follow the report, the second after the first.");
		Assert.Equal(2, AiAssistProcessSupport.CountOccurrences(report, _SECTION_HEADING_PREFIX));
		Assert.Contains(AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).DisplayName, report[first..second], StringComparison.Ordinal);
		Assert.Contains(AgentEnvironmentCatalog.Describe(AgentEnvironment.GitHubCopilotCli).DisplayName, report[second..], StringComparison.Ordinal);
	}

	[Fact]
	public void TheHelpListingDescribesWhatTheStageDoesAndNoLongerSaysItIsNotAvailable()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			[CommandRegistry.Describe(CommandId.Help).Name, AiAssistProcessSupport.ExitCommand]);

		Assert.Contains(CommandRegistry.Describe(CommandId.AiAssist).Description, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain("Not available", outcome.StandardOutput, StringComparison.Ordinal);
	}
}
