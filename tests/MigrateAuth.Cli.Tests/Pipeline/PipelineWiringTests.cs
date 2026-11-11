using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE FOUR STAGES END TO END, PLUS THE ONE THAT MUST NOT HAVE CHANGED.
///
/// The acceptance condition of the whole language-agnostic engine is "a repository with zero candidate
/// files across all eight languages completes all four stages and produces a valid report stating
/// zero findings, with exit code 0". It is the first test here.
/// </summary>
public sealed class PipelineWiringTests
{
	private const string _ORDINARY_FILE_NAME = "Ordinary.cs";
	private const string _CREDENTIAL_FILE_NAME = "Credentials.cs";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";

	private static readonly string _credentialContent =
		$"class Credentials\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, secret);\n}}\n";

	/// <summary>
	/// THE ACCEPTANCE CONDITION. A repository holding a source file with nothing to find - so the walk
	/// enumerates, the inventory counts, and preselection admits nothing - completes all four
	/// stages and produces both artifacts stating zero findings.
	/// </summary>
	[Fact]
	public void AZeroCandidateRepositoryCompletesEveryStageAndReportsZeroFindings()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		ImmutableArray<StageRunOutcome> outcomes = harness.RunTheWholeWorkflow();

		Assert.All(outcomes, outcome => Assert.Equal(StageRunOutcome.Completed, outcome));
		Assert.Empty(harness.State.CandidateRelativePaths);
		Assert.Empty(harness.State.Findings);
		Assert.Equal(2, harness.WrittenArtifactNames().Length);
		Assert.Contains(
			"Findings reported: 0 of 0",
			harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The emptiest form of that condition: no files at all. The four stages still complete and the report is
	/// still valid, which is the case every count-driven line in the pipeline has to survive.
	/// </summary>
	[Fact]
	public void AnEmptyRepositoryCompletesEveryStageAndProducesAValidArtifact()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		ImmutableArray<StageRunOutcome> outcomes = harness.RunTheWholeWorkflow();

		Assert.All(outcomes, outcome => Assert.Equal(StageRunOutcome.Completed, outcome));

		using JsonDocument artifact = JsonDocument.Parse(
			harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));

		Assert.Equal(
			0,
			artifact.RootElement.GetProperty(StructuredDataArtifactRenderer.FINDINGS_FIELD_NAME).GetArrayLength());
	}

	/// <summary>
	/// The report's five sections are present in a report over a repository that produced nothing, which
	/// is what "present when empty" means. The section headings are
	/// asserted here only as evidence that the stage rendered a whole document rather than a
	/// truncated one.
	/// </summary>
	[Fact]
	public void TheReportOverAnEmptyRunCarriesAllFiveSections()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.RunTheWholeWorkflow();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Contains(MarkdownReportRenderer.COVERAGE_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.SCAN_SUMMARY_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.SKIPPED_ITEMS_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.FINDINGS_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.PROVENANCE_HEADING, report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The third part of the data invariant surviving a process restart, which is what persisting it was for. The
	/// state is reloaded through the store exactly as a new process would read it, and the finding
	/// comes back with every term the engine assigned, the flow evidence, flow and decision
	/// included.
	/// </summary>
	[Fact]
	public void FindingsSurviveAReloadOfTheSessionState()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		SessionStateLoad reloaded = harness.ReloadState();

		Assert.True(reloaded.WasResumed);

		Finding original = Assert.Single(harness.State.Findings);
		Finding restored = Assert.Single(reloaded.State.Findings);

		Assert.Equal(original, restored);
	}

	/// <summary>
	/// A session resumed after the process that scanned and analyzed has exited reads back the inventory, the
	/// skipped-item tallies and the findings-cap outcome exactly as the still-running process holds them.
	/// </summary>
	[Fact]
	public void AResumedSessionRecoversTheScanSummaryAndTheSkippedItemTallies()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		// The running process holds real numbers, which is what a continuous session reports.
		Assert.Equal(2, harness.State.ScanOutcome.FileCount);

		string persisted = harness.ReadWrittenStateDocument();

		Assert.Contains("\"findings\"", persisted, StringComparison.Ordinal);
		Assert.Contains("\"candidateRelativePaths\"", persisted, StringComparison.Ordinal);
		Assert.Contains("\"scanSummary\"", persisted, StringComparison.Ordinal);
		Assert.Contains("\"scanSkippedItems\"", persisted, StringComparison.Ordinal);
		Assert.Contains("\"analysisSkippedItems\"", persisted, StringComparison.Ordinal);
		Assert.Contains("\"findingsCapOutcome\"", persisted, StringComparison.Ordinal);

		// So a process that only ever loads the state - never having scanned or analyzed anything
		// itself - reads back the same numbers the process that produced them held.
		SessionStateLoad reloaded = harness.ReloadState();

		Assert.True(reloaded.WasResumed);
		Assert.NotEmpty(reloaded.State.Findings);
		Assert.Equal(harness.State.ScanOutcome.FileCount, reloaded.State.ScanOutcome.FileCount);
		Assert.Equal(harness.State.ScanOutcome.CandidateFileCount, reloaded.State.ScanOutcome.CandidateFileCount);
		Assert.Equal(harness.State.FindingsCapOutcome, reloaded.State.FindingsCapOutcome);

		// Compared by (category, count) pairs rather than by the records themselves: ImmutableArray's
		// own equality is by the underlying array's identity, not its contents, so two independently
		// built empty EnumeratedPaths arrays can differ that way even where every count agrees.
		Assert.Equal(
			harness.State.MergedSkippedItems().Select(tally => (tally.Category, tally.Count)),
			reloaded.State.MergedSkippedItems().Select(tally => (tally.Category, tally.Count)));
	}

	/// <summary>
	/// The invalidation cascade reaching the new stages: re-running <c>scan</c> invalidates the three
	/// downstream stages, so a report cannot be produced from findings the current scan did not
	/// produce. The shell owns the cascade - what is checked is that the four stages sit in the
	/// chain it walks.
	/// </summary>
	[Fact]
	public void ReRunningTheScanInvalidatesTheThreeStagesThisSetWired()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		ImmutableArray<CommandDescriptor> invalidated = harness.State.MarkCompleted(CommandId.Scan);

		Assert.Equal(
			[CommandId.Analyze, CommandId.Suggest, CommandId.Report],
			invalidated.Select(stage => stage.Id));
	}

	/// <summary>
	/// <c>ai-assist</c> is a workflow command like the four above it, and the runner at the bottom of the chain is
	/// the one that handles it: it prints its own line, and it reports <see cref="StageRunOutcome.SelfReported"/>
	/// rather than <see cref="StageRunOutcome.Completed"/>, so the shell commits nothing. With no input to read, the
	/// selection ends and says that nothing was written.
	/// </summary>
	[Fact]
	public void AiAssistResolvesToTheAiAssistRunnerAndReportsItselfAsSelfReported()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.RunTheWholeWorkflow();

		Assert.Equal(StageRunOutcome.SelfReported, harness.Run(CommandId.AiAssist));
		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// The four wired stages must NOT print the ai-assist runner's lines. It is the complement of the test above
	/// and it is what would catch a decorator wired to the wrong command identity.
	/// </summary>
	[Fact]
	public void NoWiredStagePrintsAnAiAssistLine()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		Assert.DoesNotContain(AiAssistTexts.CONSOLE_LINE_PREFIX, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.SELECTION_HEADING, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// The harness builds the report writer as the product does: with the report holder, which a report write
	/// fills with the name and the length of the report it wrote. The test-only variable that drops the holder is read
	/// by the harness alone; with it set to the off value the holder is absent and nothing records the report.
	/// </summary>
	[Fact]
	public void TheHarnessBuildsTheReportWriterWithTheHolderUnlessTheTestVariableTurnsItOff()
	{
		string? original = Environment.GetEnvironmentVariable(PipelineHarness.REPORT_HOLDER_VARIABLE_NAME);

		try
		{
			Environment.SetEnvironmentVariable(PipelineHarness.REPORT_HOLDER_VARIABLE_NAME, null);

			using (PipelineHarness withHolder = PipelineHarness.Create())
			{
				withHolder.RunTheWholeWorkflow();

				Assert.NotNull(withHolder.ReportHolder);
				Assert.Equal(
					withHolder.WrittenArtifactNames().First(name => name.EndsWith(".md", StringComparison.Ordinal)),
					withHolder.ReportHolder!.Current!.FileName);
			}

			Environment.SetEnvironmentVariable(PipelineHarness.REPORT_HOLDER_VARIABLE_NAME, PipelineHarness.REPORT_HOLDER_OFF);

			using PipelineHarness withoutHolder = PipelineHarness.Create();
			withoutHolder.RunTheWholeWorkflow();

			Assert.Null(withoutHolder.ReportHolder);
		}
		finally
		{
			Environment.SetEnvironmentVariable(PipelineHarness.REPORT_HOLDER_VARIABLE_NAME, original);
		}
	}
}
