using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// WHY A LOW FINDING IS LOW, AND WHICH FOLDERS THE WALK DID NOT ENTER, THROUGH THE REAL PIPELINE:
/// recognition records the reason where the level is composed, composition carries it, the report and
/// the artifact print it, and the session state keeps it across a resume.
///
/// The three planted files each hold one construct that reads Low for a different single reason, and one
/// more holds a High construct that must state none, so a reason can only be read off the signal that
/// actually proposed it.
/// </summary>
public sealed class LowReasonAndExcludedFolderPipelineTests
{
	private const string _CLASS_LOW_FILE = "src/ClassLow.cs";
	private const string _COMMENT_FILE = "src/Commented.cs";
	private const string _DEMOTED_FILE = "Deployment/Demoted.cs";
	private const string _HIGH_FILE = "src/High.cs";
	private const string _EXCLUDED_FILE = "node_modules/generated.cs";
	private const string _NESTED_EXCLUDED_FILE = "src/bin/generated.cs";

	private static readonly string _classLowContent =
		$"class A\n{{\n\tvoid B() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();\n}}\n";

	private static readonly string _commentedContent =
		$"class A\n{{\n\t// {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, credential);\n}}\n";

	private static readonly string _codeContent =
		$"class A\n{{\n\tvoid B() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, credential);\n}}\n";

	[Fact]
	public void EachLowFindingStatesTheOneReasonThatMadeItLow()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		ImmutableArray<Finding> findings = harness.State.Findings;

		Assert.Equal(LowConfidenceReasons.PatternClass, FindingIn(findings, _CLASS_LOW_FILE).LowReasons);
		Assert.Equal(LowConfidenceReasons.CommentOrStringLiteral, FindingIn(findings, _COMMENT_FILE).LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedFile, FindingIn(findings, _DEMOTED_FILE).LowReasons);
		Assert.Equal(LowConfidenceReasons.None, FindingIn(findings, _HIGH_FILE).LowReasons);
	}

	[Fact]
	public void TheReasonsAreInTheMarkdownReportAndTheDataArtifact()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		ArtifactDocument artifact = ReadArtifact(harness);

		Assert.Equal(3, report.Split("- Low because: ", StringSplitOptions.None).Length - 1);
		Assert.Equal(["patternClass"], ArtifactFindingIn(artifact, _CLASS_LOW_FILE).LowReasons!);
		Assert.Equal(["commentOrStringLiteral"], ArtifactFindingIn(artifact, _COMMENT_FILE).LowReasons!);
		Assert.Equal(["demotedFile"], ArtifactFindingIn(artifact, _DEMOTED_FILE).LowReasons!);
		Assert.Null(ArtifactFindingIn(artifact, _HIGH_FILE).LowReasons);
	}

	[Fact]
	public void TheReasonsAndTheExcludedFoldersSurviveAResume()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		SessionStateLoad reloaded = harness.ReloadState();

		Assert.True(reloaded.WasResumed);
		Assert.Equal(harness.State.Findings.Select(finding => finding.LowReasons), reloaded.State.Findings.Select(finding => finding.LowReasons));
		Assert.Equal(harness.State.ScanOutcome.ExcludedFolders.OccurrenceCount, reloaded.State.ScanOutcome.ExcludedFolders.OccurrenceCount);
		Assert.Equal(
			harness.State.ScanOutcome.ExcludedFolders.Paths.Select(path => path.Value),
			reloaded.State.ScanOutcome.ExcludedFolders.Paths.Select(path => path.Value));
	}

	[Fact]
	public void TheExcludedFoldersAreInTheReportAndTheArtifactAndNotAmongTheSkippedItems()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		ArtifactExcludedFoldersDocument excluded = ReadArtifact(harness).ScanSummary.ExcludedFolders;

		Assert.Equal(2, excluded.Count);
		Assert.Equal(["node_modules", "src/bin"], excluded.Paths);
		Assert.Equal(0, excluded.OmittedPathCount);
		Assert.Contains("Folders excluded from the walk and not read: 2.", report, StringComparison.Ordinal);
		Assert.Contains("- node\\_modules", report, StringComparison.Ordinal);
		Assert.Contains("Skipped files: 0.", report, StringComparison.Ordinal);
	}

	private static PipelineHarness RunTheWorkflowOverThePlantedTree()
	{
		PipelineHarness harness = PipelineHarness.Create();

		harness.WriteSourceFile(_CLASS_LOW_FILE, _classLowContent);
		harness.WriteSourceFile(_COMMENT_FILE, _commentedContent);
		harness.WriteSourceFile(_DEMOTED_FILE, _codeContent);
		harness.WriteSourceFile(_HIGH_FILE, _codeContent);
		harness.WriteSourceFile(_EXCLUDED_FILE, _codeContent);
		harness.WriteSourceFile(_NESTED_EXCLUDED_FILE, _codeContent);
		harness.RunTheWholeWorkflow();

		return harness;
	}

	private static Finding FindingIn(ImmutableArray<Finding> findings, string relativePath)
	{
		return findings.Single(finding => finding.Path.Value == relativePath);
	}

	private static ArtifactFindingDocument ArtifactFindingIn(ArtifactDocument artifact, string relativePath)
	{
		return artifact.Findings.Single(finding => finding.FilePath == relativePath);
	}

	private static ArtifactDocument ReadArtifact(PipelineHarness harness)
	{
		ArtifactDocument? document = JsonSerializer.Deserialize(
			harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact),
			ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);

		return document;
	}
}
