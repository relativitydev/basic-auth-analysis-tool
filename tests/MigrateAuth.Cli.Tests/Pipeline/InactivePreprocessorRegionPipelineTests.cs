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
/// A FINDING INSIDE A CONDITIONAL-COMPILATION BLOCK THAT THE FILE'S OWN SYMBOLS SWITCH OFF IS REPORTED AT LOW, NAMES THAT
/// AS ITS REASON, AND THE REASON SITS AFTER THE FILE-SCOPED ONES IN THE DECLARED ORDER - through the real pipeline over the
/// embedded catalog, in the Markdown report, the data artifact and a resumed session. The shape is a credential-carrying
/// statement inside a block guarded by a symbol the file never defines. Every name is invented.
/// </summary>
public sealed class InactivePreprocessorRegionPipelineTests
{
	private const string _INACTIVE_FILE = "src/Inactive.cs";
	private const string _LIVE_FILE = "src/Live.cs";
	private const string _MAYBE_FILE = "src/Maybe.cs";
	private const string _INACTIVE_VENDORED_FILE = "vendor/Inactive.cs";
	private const string _INACTIVE_VALUE = "inactivePreprocessorRegion";
	private const string _VENDORED_VALUE = "demotedVendoredFile";
	private const string _STATEMENT = "\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n";
	private const string _INACTIVE_CONTENT =
		$"class InactiveFixture\n{{\n\tvoid Build()\n\t{{\n#if LEGACY_TRANSPORT_NOT_DEFINED_HERE\n{_STATEMENT}#endif\n\t}}\n}}\n";
	private const string _LIVE_CONTENT = $"class LiveFixture\n{{\n\tvoid Build()\n\t{{\n{_STATEMENT}\t}}\n}}\n";
	private const string _MAYBE_CONTENT =
		$"class MaybeFixture\n{{\n\tvoid Build()\n\t{{\n#if DEBUG\n{_STATEMENT}#endif\n\t}}\n}}\n";

	[Fact]
	public void AFindingInASwitchedOffBlockReadsLowWithTheInactiveReasonAndALiveOrMaybeOneDoesNot()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePreparedTree();

		ImmutableArray<Finding> findings = harness.State.Findings;
		Finding inactive = FindingIn(findings, _INACTIVE_FILE);

		Assert.Equal(ConfidenceLevel.Low, inactive.Level);
		Assert.Equal(LowConfidenceReasons.InactivePreprocessorRegion, inactive.LowReasons);
		Assert.Equal(ConfidenceLevel.High, FindingIn(findings, _LIVE_FILE).Level);
		Assert.Equal(LowConfidenceReasons.None, FindingIn(findings, _LIVE_FILE).LowReasons);
		Assert.Equal(ConfidenceLevel.High, FindingIn(findings, _MAYBE_FILE).Level);
		Assert.Equal(LowConfidenceReasons.None, FindingIn(findings, _MAYBE_FILE).LowReasons);
	}

	[Fact]
	public void TheInactiveReasonFollowsTheFileScopedOnesInTheDeclaredOrder()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePreparedTree();

		Finding finding = FindingIn(harness.State.Findings, _INACTIVE_VENDORED_FILE);

		Assert.Equal(ConfidenceLevel.Low, finding.Level);
		Assert.Equal(LowConfidenceReasons.DemotedVendoredFile | LowConfidenceReasons.InactivePreprocessorRegion, finding.LowReasons);
		Assert.Equal([_VENDORED_VALUE, _INACTIVE_VALUE], LowReasonVocabulary.ValuesOf(finding.LowReasons));
	}

	[Fact]
	public void TheMarkdownReportStatesTheInactiveSentenceAfterTheVendoredOne()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePreparedTree();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport).ReplaceLineEndings("\n");
		string inactiveLine = ReasonLine(LowConfidenceReasons.InactivePreprocessorRegion);
		string vendoredAndInactiveLine = ReasonLine(LowConfidenceReasons.DemotedVendoredFile | LowConfidenceReasons.InactivePreprocessorRegion);

		Assert.Equal(1, CountOf(report, $"{inactiveLine}\n"));
		Assert.Equal(1, CountOf(report, $"{vendoredAndInactiveLine}\n"));
		Assert.True(
			vendoredAndInactiveLine.IndexOf("vendor or third-party", StringComparison.Ordinal)
				< vendoredAndInactiveLine.IndexOf("conditional-compilation", StringComparison.Ordinal));
	}

	[Fact]
	public void TheDataArtifactCarriesTheInactiveValueInDeclaredOrderAndNoneForALiveFinding()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePreparedTree();

		ArtifactDocument artifact = ReadArtifact(harness);

		Assert.Equal([_INACTIVE_VALUE], ArtifactFindingIn(artifact, _INACTIVE_FILE).LowReasons!);
		Assert.Equal([_VENDORED_VALUE, _INACTIVE_VALUE], ArtifactFindingIn(artifact, _INACTIVE_VENDORED_FILE).LowReasons!);
		Assert.Null(ArtifactFindingIn(artifact, _LIVE_FILE).LowReasons);
		Assert.Null(ArtifactFindingIn(artifact, _MAYBE_FILE).LowReasons);
	}

	[Fact]
	public void TheInactiveReasonSurvivesAResume()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePreparedTree();

		SessionStateLoad reloaded = harness.ReloadState();

		Assert.True(reloaded.WasResumed);
		Assert.Equal(
			LowConfidenceReasons.InactivePreprocessorRegion,
			FindingIn(reloaded.State.Findings, _INACTIVE_FILE).LowReasons);
	}

	private static PipelineHarness RunTheWorkflowOverThePreparedTree()
	{
		PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.WriteSourceFile(_INACTIVE_FILE, _INACTIVE_CONTENT);
		harness.WriteSourceFile(_LIVE_FILE, _LIVE_CONTENT);
		harness.WriteSourceFile(_MAYBE_FILE, _MAYBE_CONTENT);
		harness.WriteSourceFile(_INACTIVE_VENDORED_FILE, _INACTIVE_CONTENT);
		harness.RunTheWholeWorkflow();

		return harness;
	}

	private static string ReasonLine(LowConfidenceReasons reasons)
	{
		return $"- {LowReasonVocabulary.REASON_LABEL}: {LowReasonVocabulary.SentenceOf(reasons)}";
	}

	private static int CountOf(string text, string fragment)
	{
		return text.Split(fragment, StringSplitOptions.None).Length - 1;
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
