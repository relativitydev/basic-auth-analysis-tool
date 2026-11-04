using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// The stage as the session runs it: what it prints, what it puts on the state, how it ends, and
/// what it leaves alone.
/// </summary>
public sealed class ScanStageTests
{
	private const string _RECOGNIZED_FILE_NAME = "credentials.ex1";
	private const string _ORDINARY_FILE_NAME = "ordinary.ex1";
	private const string _CANDIDATE_CONTENT = "var credentials = NewExemplarCredentials(user, secret);\n";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";

	[Fact]
	public void AScanOverACleanTreeCompletes()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		StageRunOutcome outcome = harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Equal(StageRunOutcome.Completed, outcome);
	}

	/// <summary>
	/// The candidate set is an output of Scan that reaches the session state, which is what
	/// gives the shell something to persist and the analysis something to consume.
	/// </summary>
	[Fact]
	public void TheCandidateSetReachesTheSessionState()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);
		harness.Fixture.WriteText(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Equal([_RECOGNIZED_FILE_NAME], harness.State.CandidateRelativePaths);
	}

	/// <summary>
	/// A second run replaces the set rather than appending to it. Re-running <c>scan</c> on an
	/// unchanged repository is the routine thing the cascade makes people do.
	/// </summary>
	[Fact]
	public void ASecondScanReplacesTheCandidateSet()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);

		ScanStage stage = harness.BuildStage();

		stage.Run(harness.State, CancellationToken.None);
		stage.Run(harness.State, CancellationToken.None);

		Assert.Equal([_RECOGNIZED_FILE_NAME], harness.State.CandidateRelativePaths);
	}

	[Fact]
	public void TheInventoryAndTheCandidateCountAreReported()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		string transcript = harness.Output.ToString();

		Assert.Contains(ScanStage.INVENTORY_HEADER_MESSAGE, transcript, StringComparison.Ordinal);
		Assert.Contains("ExemplarOne", transcript, StringComparison.Ordinal);
		Assert.Contains("Candidate files: 1 of 1.", transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// The skipped count, in the scan summary. The two numbers are reported apart,
	/// because files are counted for eight categories and directories for the ninth, and a sum of
	/// the two would mean nothing.
	/// </summary>
	[Fact]
	public void TheSkippedCountIsReportedInTheSummary()
	{
		using ScanHarness harness = ScanHarness.CreateWithEveryCondition();

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Contains(
			"Skipped files: 5. Directories whose contents could not be listed: 1.",
			harness.Output.ToString(),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The ledger is cleared at the start of every run, so a second Scan over an unchanged
	/// tree reports the same counts rather than the sum of two runs - without the clear, this would
	/// read 10 and 2 rather than 5 and 1.
	/// </summary>
	[Fact]
	public void ASecondScanDoesNotDoubleTheSkippedCount()
	{
		using ScanHarness harness = ScanHarness.CreateWithEveryCondition();
		ScanStage stage = harness.BuildStage();

		stage.Run(harness.State, CancellationToken.None);
		stage.Run(harness.State, CancellationToken.None);

		Assert.Contains(
			"Skipped files: 5. Directories whose contents could not be listed: 1.",
			harness.Output.ToString(),
			StringComparison.Ordinal);
		Assert.Equal(5, harness.State.ScanSkippedItems.Sum(tally => tally.Unit == SkippedItemUnit.Files ? tally.Count : 0));
		Assert.Equal(1, harness.State.ScanSkippedItems.Sum(tally => tally.Unit == SkippedItemUnit.Directories ? tally.Count : 0));
	}

	/// <summary>
	/// The emptiest case of the zero-candidate requirement: a repository with no files completes rather than failing,
	/// and says what it found rather than printing a table with nothing in it.
	/// </summary>
	[Fact]
	public void AnEmptyRepositoryCompletesAndSaysSo()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		StageRunOutcome outcome = harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Contains(ScanStage.NO_FILES_MESSAGE, harness.Output.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain(ScanStage.INVENTORY_HEADER_MESSAGE, harness.Output.ToString(), StringComparison.Ordinal);
		Assert.Empty(harness.State.CandidateRelativePaths);
	}

	/// <summary>
	/// An interrupted stage leaves nothing partial behind. The state's candidate set is
	/// untouched, so a cancelled scan does not half-replace the set an earlier one produced.
	/// </summary>
	[Fact]
	public void AnInterruptedScanLeavesTheCandidateSetAlone()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_RECOGNIZED_FILE_NAME, _CANDIDATE_CONTENT);

		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();

		StageRunOutcome outcome = harness.BuildStage().Run(harness.State, cancellation.Token);

		Assert.Equal(StageRunOutcome.Cancelled, outcome);
		Assert.Empty(harness.State.CandidateRelativePaths);
	}

	/// <summary>
	/// THE ORDERING CRITERION, AT UNIT SCALE. The tree holds four files the reader cannot read, so
	/// its refusal count becomes non-zero the moment the inventory pass opens its first files. The
	/// line naming the total is written while that count is still zero - which is what "before
	/// scanning begins" means, said as an observation rather than as a line number in this file.
	/// </summary>
	[Fact]
	public void TheTotalIsAnnouncedBeforeAnyFileIsOpened()
	{
		using ScanHarness harness = ScanHarness.CreateWithEveryCondition();

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.True(harness.Output.WasWrittenBeforeAnyFileWasOpened("files enumerated"));
		Assert.True(harness.Output.FilesTheReaderRefused > 0);
	}

	/// <summary>
	/// The four unwired stages are not this stage's, and the way they are left alone is that they are
	/// executed by their own runner. The mock stands in for that runner and
	/// records that it was the one asked.
	/// </summary>
	[Theory]
	[InlineData(CommandId.Analyze)]
	[InlineData(CommandId.Suggest)]
	[InlineData(CommandId.Report)]
	[InlineData(CommandId.AiAssist)]
	public void EveryStageButScanStillRunsThroughTheRunnerItAlwaysDid(CommandId workflowCommand)
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		Mock<IWorkflowStageRunner> runnerForEveryOtherStage = new(MockBehavior.Strict);

		runnerForEveryOtherStage
			.Setup(runner => runner.Run(workflowCommand, harness.State, It.IsAny<CancellationToken>()))
			.Returns(StageRunOutcome.Completed);

		ScanWiredStageRunner wiredRunner = new(harness.BuildStage(), runnerForEveryOtherStage.Object);

		StageRunOutcome outcome = wiredRunner.Run(workflowCommand, harness.State, CancellationToken.None);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		runnerForEveryOtherStage.VerifyAll();
	}

	/// <summary>
	/// And the unwired runner is never asked to run <c>scan</c>, which is the other half of the
	/// same guarantee: the message it prints must not appear alongside a real inventory.
	/// </summary>
	[Fact]
	public void ScanIsTheOneStageTheWiredRunnerKeepsForItself()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		Mock<IWorkflowStageRunner> runnerForEveryOtherStage = new(MockBehavior.Strict);
		ScanWiredStageRunner wiredRunner = new(harness.BuildStage(), runnerForEveryOtherStage.Object);

		StageRunOutcome outcome = wiredRunner.Run(CommandId.Scan, harness.State, CancellationToken.None);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		runnerForEveryOtherStage.Verify(
			runner => runner.Run(It.IsAny<CommandId>(), It.IsAny<SessionState>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	/// <summary>
	/// The messages this stage writes name the stage, and the name is the registry's. Two places
	/// holding the same word is fine as long as something fails when they stop agreeing.
	/// </summary>
	[Fact]
	public void TheStageNamesItselfAsTheRegistryNamesIt()
	{
		string registeredName = CommandRegistry.Describe(CommandId.Scan).Name;

		Assert.StartsWith($"{registeredName}:", ScanStage.ENUMERATION_STARTED_MESSAGE, StringComparison.Ordinal);
		Assert.StartsWith($"{registeredName}:", ScanStage.NO_FILES_MESSAGE, StringComparison.Ordinal);
		Assert.StartsWith(
			$"{registeredName}:",
			ScanStage.ENUMERATION_COMPLETED_MESSAGE_FORMAT,
			StringComparison.Ordinal);
	}
}
