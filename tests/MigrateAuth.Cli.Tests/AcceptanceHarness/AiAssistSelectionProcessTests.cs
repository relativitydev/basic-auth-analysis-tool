using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE SELECTION, THROUGH THE REAL PROCESS: the numbers and the accepted names, one re-ask, a cancel on a second
/// invalid input, an empty line or the end of input, the out-of-scope answer, and no echo of what was typed.
///
/// Every run here has redirected input, so no launch is ever offered; the child's search path is an empty folder.
/// </summary>
public sealed class AiAssistSelectionProcessTests
{
	public static TheoryData<AgentEnvironment> Environments => new()
	{
		AgentEnvironment.ClaudeCode,
		AgentEnvironment.OpenAiCodex,
		AgentEnvironment.GitHubCopilotCli
	};

	[Theory]
	[MemberData(nameof(Environments))]
	public void TheNumberOfAnEnvironmentSelectsItAndItsPackIsWritten(AgentEnvironment environment)
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, AiAssistProcessSupport.SelectionNumberOf(environment), AiAssistProcessSupport.ExitCommand));

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Contains(AiAssistTexts.SELECTION_HEADING, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Contains(AiAssistTexts.SelectionOption(environment), outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Equal([$"{AgentEnvironmentCatalog.Describe(environment).PackFileNameStem}.md"], AiAssistProcessSupport.PackNames(destination));
		Assert.DoesNotContain(AiAssistTexts.SELECTION_REASK, outcome.StandardOutput, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Environments))]
	public void TheAcceptedNameOfAnEnvironmentSelectsIt(AgentEnvironment environment)
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(
				AiAssistProcessSupport.AiAssistCommand,
				AgentEnvironmentCatalog.Describe(environment).AcceptedNames[0],
				AiAssistProcessSupport.ExitCommand));

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Equal([$"{AgentEnvironmentCatalog.Describe(environment).PackFileNameStem}.md"], AiAssistProcessSupport.PackNames(destination));
	}

	[Fact]
	public void AnInvalidInputPrintsTheReaskOnceAndAValidSecondInputSelects()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, AiAssistProcessSupport.HOSTILE_ANSWER, "2", AiAssistProcessSupport.ExitCommand));

		Assert.Equal(1, AiAssistProcessSupport.CountOccurrences(outcome.StandardOutput, AiAssistTexts.SELECTION_REASK));
		Assert.Equal([$"{AgentEnvironmentCatalog.Describe(AgentEnvironment.OpenAiCodex).PackFileNameStem}.md"], AiAssistProcessSupport.PackNames(destination));
		Assert.DoesNotContain(AiAssistProcessSupport.HOSTILE_ANSWER, outcome.StandardOutput, StringComparison.Ordinal);
	}

	[Fact]
	public void ASecondInvalidInputCancelsAndLeavesTheReportAndTheDataArtifactAsTheyWere()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder control = TemporaryFolder.Create();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);
		AiAssistProcessSupport.Run(repository, control, AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.ExitCommand));

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(
				AiAssistProcessSupport.AiAssistCommand,
				AiAssistProcessSupport.HOSTILE_ANSWER,
				AiAssistProcessSupport.HOSTILE_ANSWER,
				AiAssistProcessSupport.ExitCommand));

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Empty(AiAssistProcessSupport.PackNames(destination));
		Assert.Equal(AiAssistProcessSupport.ReportBytes(control), AiAssistProcessSupport.ReportBytes(destination));
		Assert.Equal(AiAssistProcessSupport.DataBytes(control), AiAssistProcessSupport.DataBytes(destination));
		Assert.DoesNotContain(AiAssistProcessSupport.HOSTILE_ANSWER, outcome.StandardOutput, StringComparison.Ordinal);
	}

	[Fact]
	public void AnEmptyLineCancelsWithoutAnyReask()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, string.Empty, AiAssistProcessSupport.ExitCommand));

		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.SELECTION_REASK, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Empty(AiAssistProcessSupport.PackNames(destination));
	}

	[Fact]
	public void TheEndOfInputCancelsAndTheSessionEndsWithExitCodeZero()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand));

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Empty(AiAssistProcessSupport.PackNames(destination));
	}

	[Fact]
	public void TheOutOfScopeNameGetsItsOwnLineAndNoPackIsWritten()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.AiAssistCommand, AiAssistTexts.CURSOR_NAME, string.Empty, AiAssistProcessSupport.ExitCommand));

		Assert.Contains(AiAssistTexts.SELECTION_CURSOR_NOT_SUPPORTED, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Empty(AiAssistProcessSupport.PackNames(destination));
	}
}
