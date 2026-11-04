using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;

using MigrateAuth.WorstCase;

using Xunit;

namespace MigrateAuth.Cli.Tests.WorstCase;

/// <summary>
/// OVER A GENERATED REPOSITORY.
///
/// A small worst-case tree - the heavy files that exhaust the global findings cap, then the files
/// that come after it - is generated into the harness's own folder and run through the real embedded
/// analyzer. Flow signals are evaluated per file before the cap is applied, so the count the run
/// records (<see cref="FlowEvaluationDiagnostics.FilesEvaluated"/>) must equal the number of files
/// holding a High construct, and it must exceed the number of files that contributed a retained
/// finding: the difference is the files encountered after the cap was reached.
///
/// The assertions compare counts and paths derived from the generator's own plan. Nothing here reads a
/// clock, so the test cannot be timing sensitive.
/// </summary>
public sealed class WorstCaseFlowEvaluationTests
{
	private const int _FILES_AFTER_THE_HEAVY_FILES = 34;
	private const int _SEED = 7;

	[Fact]
	public void SignalsAreEvaluatedOnFilesEncounteredAfterTheGlobalCapWasReached()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		WorstCaseTreeOptions seeded = new() { Seed = _SEED, OutputRoot = harness.RepositoryRootPath, FileCount = 0 };
		WorstCaseTreeOptions options = seeded with { FileCount = seeded.HeavyFileCount + _FILES_AFTER_THE_HEAVY_FILES };
		List<WorstCaseFilePlan> plans = Enumerable.Range(0, options.FileCount).Select(index => WorstCaseTreePlanner.PlanFile(options, index)).ToList();

		WorstCaseTreeGenerator.Generate(options);
		harness.RunAndCommit(CommandId.Scan);
		StageRunOutcome outcome = harness.Run(CommandId.Analyze);

		HashSet<string> pathsWithARetainedFinding = harness.State.Findings.Select(finding => finding.Path.Value).ToHashSet();
		List<WorstCaseFilePlan> filesHoldingHighConstructs = plans
			.Where(plan => plan.Kind is WorstCaseFileKind.Heavy or WorstCaseFileKind.HighTail)
			.ToList();
		List<WorstCaseFilePlan> filesPastTheCap = filesHoldingHighConstructs
			.Where(plan => !pathsWithARetainedFinding.Contains(plan.RelativePath))
			.ToList();

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.True(harness.State.FindingsCapOutcome.GlobalCapTripped);
		Assert.Equal(WorstCaseTreeOptions.PRODUCT_GLOBAL_FINDINGS_CAP, harness.State.Findings.Length);
		Assert.Contains(filesPastTheCap, plan => plan.Kind == WorstCaseFileKind.HighTail);
		Assert.Equal(filesHoldingHighConstructs.Count, harness.State.FlowEvaluationDiagnostics.FilesEvaluated);
		Assert.True(harness.State.FlowEvaluationDiagnostics.FilesEvaluated > pathsWithARetainedFinding.Count);
		Assert.Equal(
			harness.State.FlowEvaluationDiagnostics.FilesEvaluated - pathsWithARetainedFinding.Count,
			filesPastTheCap.Count);
	}
}
