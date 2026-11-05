using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// the gating and the cascade, over the state alone. The shell's own tests
/// cover what the user sees; these cover what is true.
/// </summary>
public sealed class SessionStateTests
{
	private const string _REPOSITORY_ROOT_LABEL = "example-repository";
	private const string _ATTRIBUTED_LANGUAGE_NAME = "ExemplarOne";

	[Fact]
	public void FreshSessionHasNothingCompleted()
	{
		SessionState state = BuildFreshState();

		Assert.Empty(state.CompletedStagesInRunOrder());
	}

	[Fact]
	public void ScanIsNotGatedInAFreshSession()
	{
		SessionState state = BuildFreshState();

		Assert.False(state.TryFindUnmetPredecessor(CommandId.Scan, out CommandDescriptor? predecessor));
		Assert.Null(predecessor);
	}

	[Theory]
	[InlineData(CommandId.Analyze, CommandId.Scan)]
	[InlineData(CommandId.Suggest, CommandId.Scan)]
	[InlineData(CommandId.Report, CommandId.Scan)]
	[InlineData(CommandId.AiAssist, CommandId.Scan)]
	public void StageIsGatedOnTheFirstIncompleteStageOfAFreshSession(CommandId stage, CommandId expectedPredecessor)
	{
		SessionState state = BuildFreshState();

		bool gated = state.TryFindUnmetPredecessor(stage, out CommandDescriptor? predecessor);

		Assert.True(gated);
		Assert.Equal(expectedPredecessor, predecessor!.Id);
	}

	[Theory]
	[InlineData(CommandId.Report, CommandId.Analyze)]
	[InlineData(CommandId.Suggest, CommandId.Analyze)]
	[InlineData(CommandId.AiAssist, CommandId.Analyze)]
	public void StageIsGatedOnTheFirstIncompleteStageAfterScanAlone(CommandId stage, CommandId expectedStage)
	{
		SessionState state = BuildFreshState();
		state.MarkCompleted(CommandId.Scan);

		bool gated = state.TryFindUnmetPredecessor(stage, out CommandDescriptor? unmetStage);

		Assert.True(gated);
		Assert.Equal(expectedStage, unmetStage!.Id);
	}

	[Fact]
	public void CompletingThePredecessorOpensTheStage()
	{
		SessionState state = BuildFreshState();

		state.MarkCompleted(CommandId.Scan);

		Assert.False(state.TryFindUnmetPredecessor(CommandId.Analyze, out _));
	}

	[Fact]
	public void RerunningAStageInvalidatesTheStagesDownstreamOfIt()
	{
		SessionState state = BuildStateCompletedThrough(CommandId.Report);

		ImmutableArray<CommandDescriptor> invalidatedStages = state.MarkCompleted(CommandId.Analyze);

		Assert.Equal([CommandId.Suggest, CommandId.Report], invalidatedStages.Select(stage => stage.Id));
	}

	[Fact]
	public void RerunningAStageLeavesTheStagesUpstreamOfItAlone()
	{
		SessionState state = BuildStateCompletedThrough(CommandId.Report);

		state.MarkCompleted(CommandId.Analyze);

		Assert.True(state.IsCompleted(CommandId.Scan));
		Assert.True(state.IsCompleted(CommandId.Analyze));
	}

	[Fact]
	public void RerunningAStageNamesOnlyTheDownstreamStagesThatHadCompleted()
	{
		SessionState state = BuildStateCompletedThrough(CommandId.Analyze);

		ImmutableArray<CommandDescriptor> invalidatedStages = state.MarkCompleted(CommandId.Scan);

		Assert.Equal([CommandId.Analyze], invalidatedStages.Select(stage => stage.Id));
	}

	[Fact]
	public void RerunningTheLastStageInvalidatesNothing()
	{
		SessionState state = BuildStateCompletedThrough(CommandId.AiAssist);

		ImmutableArray<CommandDescriptor> invalidatedStages = state.MarkCompleted(CommandId.AiAssist);

		Assert.Empty(invalidatedStages);
	}

	/// <summary>
	/// An interrupted stage leaves nothing behind, and it does not invalidate what came
	/// before it.
	/// </summary>
	[Fact]
	public void InterruptingAStageLeavesTheSessionIntact()
	{
		SessionState state = BuildStateCompletedThrough(CommandId.Analyze);

		state.MarkNotCompleted(CommandId.Analyze);

		Assert.True(state.IsCompleted(CommandId.Scan));
		Assert.False(state.IsCompleted(CommandId.Analyze));
	}

	[Fact]
	public void CompletedStagesAreReportedInRunOrder()
	{
		SessionState state = BuildFreshState();

		state.MarkCompleted(CommandId.Scan);
		state.MarkCompleted(CommandId.Analyze);

		Assert.Equal([CommandId.Scan, CommandId.Analyze], state.CompletedStagesInRunOrder().Select(stage => stage.Id));
	}

	[Fact]
	public void OnlyAWorkflowCommandHasACompletionState()
	{
		SessionState state = BuildFreshState();

		Assert.Throws<ArgumentOutOfRangeException>(() => state.MarkCompleted(CommandId.Help));
		Assert.Throws<ArgumentOutOfRangeException>(() => state.MarkNotCompleted(CommandId.Clear));
	}

	[Fact]
	public void AFreshSessionHasNoCandidateSet()
	{
		Assert.Empty(BuildFreshState().CandidateRelativePaths);
	}

	[Fact]
	public void TheCandidateSetScanProducedIsRecorded()
	{
		SessionState state = BuildFreshState();

		RecordCandidates(state, "src/first.ex1", "src/second.ex1");

		Assert.Equal(["src/first.ex1", "src/second.ex1"], state.CandidateRelativePaths);
	}

	/// <summary>
	/// Resumption granularity is the stage, so a re-run of Scan produces the candidate set
	/// of that run and never the union of two. Appending would double the set on every re-scan of
	/// an unchanged repository.
	/// </summary>
	[Fact]
	public void RecordingTheCandidateSetReplacesTheOneBeforeIt()
	{
		SessionState state = BuildFreshState();

		RecordCandidates(state, "src/first.ex1");
		RecordCandidates(state, "src/second.ex1");

		Assert.Equal(["src/second.ex1"], state.CandidateRelativePaths);
	}

	[Fact]
	public void AScanThatFoundNothingClearsTheCandidateSet()
	{
		SessionState state = BuildFreshState();

		RecordCandidates(state, "src/first.ex1");
		RecordCandidates(state);

		Assert.Empty(state.CandidateRelativePaths);
		Assert.Empty(state.CandidateLanguageNames);
	}

	/// <summary>The attribution is recorded beside the candidate it belongs to and replaced with it.</summary>
	[Fact]
	public void EachCandidateIsRecordedWithTheAttributionScanGaveIt()
	{
		SessionState state = BuildFreshState();

		state.RecordScanOutcome(
			["src/first.ex1", "src/second.ex2"],
			["ExemplarOne", "ExemplarTwo"],
			ScanSummary.Empty,
			SkippedItemTallies.Empty,
			ImmutableArray<AssessedLanguage>.Empty);

		Assert.Equal(["ExemplarOne", "ExemplarTwo"], state.CandidateLanguageNames);

		RecordCandidates(state, "src/third.ex1");

		Assert.Equal([_ATTRIBUTED_LANGUAGE_NAME], state.CandidateLanguageNames);
	}

	/// <summary>A candidate without an attribution, or an attribution without a candidate, is not a scan outcome.</summary>
	[Fact]
	public void ACandidateSetWhoseAttributionDoesNotMatchItsLengthIsRefused()
	{
		SessionState state = BuildFreshState();

		Assert.Throws<ArgumentException>(() => state.RecordScanOutcome(
			["src/first.ex1", "src/second.ex1"],
			[_ATTRIBUTED_LANGUAGE_NAME],
			ScanSummary.Empty,
			SkippedItemTallies.Empty,
			ImmutableArray<AssessedLanguage>.Empty));
	}

	/// <summary>
	/// <c>SessionState.RecordScanOutcome</c>'s candidate-set half, isolated from the scan summary and
	/// the skipped-item tallies it also records - what these tests are about is the
	/// candidate set's own replace-not-append behaviour, unrelated to either of the other two.
	/// </summary>
	private static void RecordCandidates(SessionState state, params string[] candidateRelativePaths)
	{
		state.RecordScanOutcome(
			candidateRelativePaths,
			[.. candidateRelativePaths.Select(_ => _ATTRIBUTED_LANGUAGE_NAME)],
			ScanSummary.Empty,
			SkippedItemTallies.Empty,
			ImmutableArray<AssessedLanguage>.Empty);
	}

	private static SessionState BuildFreshState()
	{
		return SessionState.Fresh(LanguageCatalog.CurrentVersion, _REPOSITORY_ROOT_LABEL);
	}

	private static SessionState BuildStateCompletedThrough(CommandId lastCompletedStage)
	{
		SessionState state = BuildFreshState();
		int lastOrder = CommandRegistry.Describe(lastCompletedStage).OrderWithinGroup;

		foreach (CommandDescriptor stage in CommandRegistry.WorkflowCommandsInRunOrder)
		{
			if (stage.OrderWithinGroup <= lastOrder)
			{
				state.MarkCompleted(stage.Id);
			}
		}

		return state;
	}
}
