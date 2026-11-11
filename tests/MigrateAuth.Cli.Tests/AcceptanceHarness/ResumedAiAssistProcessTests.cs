using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// A RESUMED SESSION, ACROSS TWO REAL PROCESSES. The first runs the four stages and ends; the second resumes from the
/// state the first persisted. The stage passes its gate, writes its pack, appends nothing to a report this process
/// did not write and says so, and leaves every artifact and the state as they were; after <c>analyze</c> re-runs, the
/// stage is gated again.
/// </summary>
public sealed class ResumedAiAssistProcessTests
{
	[Fact]
	public void AResumedSessionPassesTheGateWritesThePackAppendsNothingAndSaysSo()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);
		AiAssistProcessSupport.Run(repository, destination, AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.ExitCommand));

		byte[] reportBefore = AiAssistProcessSupport.ReportBytes(destination);
		byte[] dataBefore = AiAssistProcessSupport.DataBytes(destination);
		byte[] stateBefore = AiAssistProcessSupport.StateBytes(destination, repository);

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			[AiAssistProcessSupport.AiAssistCommand, "1", AiAssistProcessSupport.ExitCommand]);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Contains(AiAssistTexts.REPORT_NOT_WRITTEN_BY_THIS_RUN, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Single(AiAssistProcessSupport.PackNames(destination));
		Assert.Equal(reportBefore, AiAssistProcessSupport.ReportBytes(destination));
		Assert.Equal(dataBefore, AiAssistProcessSupport.DataBytes(destination));
		Assert.Equal(stateBefore, AiAssistProcessSupport.StateBytes(destination, repository));
	}

	[Fact]
	public void AfterAnalyzeRunsAgainInTheResumedSessionTheStageIsGatedAgain()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		AiAssistProcessSupport.WriteRepository(repository);
		AiAssistProcessSupport.Run(repository, destination, AiAssistProcessSupport.FourStagesThen(AiAssistProcessSupport.ExitCommand));

		CliProcessOutcome outcome = AiAssistProcessSupport.Run(
			repository,
			destination,
			[
				AiAssistProcessSupport.AiAssistCommand,
				string.Empty,
				CommandRegistry.Describe(CommandId.Analyze).Name,
				AiAssistProcessSupport.AiAssistCommand,
				AiAssistProcessSupport.ExitCommand
			]);

		// Analyze cleared every stage after it, so the first one the gate names is suggest.
		string gatingMessage = CommandRegistry.FormatGatingMessage(CommandId.Suggest);

		Assert.Equal(1, AiAssistProcessSupport.CountOccurrences(outcome.StandardOutput, gatingMessage));
		Assert.Equal(1, AiAssistProcessSupport.CountOccurrences(outcome.StandardOutput, AiAssistTexts.SELECTION_HEADING));
	}
}
