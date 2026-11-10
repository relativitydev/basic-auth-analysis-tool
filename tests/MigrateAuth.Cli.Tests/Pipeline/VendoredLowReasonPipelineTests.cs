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
/// A FINDING DEMOTED BY A VENDORED FOLDER NAMES VENDORED CODE AS ITS REASON, AND A FINDING DEMOTED BY
/// ANYTHING ELSE KEEPS THE GENERIC ONE - through the real pipeline over the embedded catalog, in the
/// Markdown report and in the data artifact.
///
/// The same construct is planted five times so the only thing that differs between the findings is the
/// folder it sits in, and one of them sits in a vendored folder that is also a test folder.
/// </summary>
public sealed class VendoredLowReasonPipelineTests
{
	private const string _VENDORED_FILE = "vendor/Library.cs";
	private const string _THIRD_PARTY_FILE = "third-party/Library.cs";
	private const string _VENDORED_AND_TEST_FILE = "vendor/tests/Library.cs";
	private const string _DEPLOYMENT_FILE = "Deployment/Library.cs";
	private const string _OWN_CODE_FILE = "src/Library.cs";
	private const string _VENDORED_VALUE = "demotedVendoredFile";
	private const string _GENERIC_VALUE = "demotedFile";

	private const string _content =
		"class HighFindingFixture\n{\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	[Fact]
	public void AFindingInAVendoredFolderStatesVendoredCodeAndNothingElse()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		ImmutableArray<Finding> findings = harness.State.Findings;

		Assert.Equal(LowConfidenceReasons.DemotedVendoredFile, FindingIn(findings, _VENDORED_FILE).LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedVendoredFile, FindingIn(findings, _THIRD_PARTY_FILE).LowReasons);
		Assert.Equal(LowConfidenceReasons.DemotedFile, FindingIn(findings, _DEPLOYMENT_FILE).LowReasons);
		Assert.Equal(LowConfidenceReasons.None, FindingIn(findings, _OWN_CODE_FILE).LowReasons);
	}

	[Fact]
	public void AVendoredFolderThatIsAlsoATestFolderStatesBothReasons()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		Finding finding = FindingIn(harness.State.Findings, _VENDORED_AND_TEST_FILE);

		Assert.Equal(LowConfidenceReasons.DemotedFile | LowConfidenceReasons.DemotedVendoredFile, finding.LowReasons);
	}

	[Fact]
	public void TheMarkdownReportNamesVendoredCodeForAVendoredFindingAndTheGenericReasonForADemotedFile()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport).ReplaceLineEndings("\n");
		string vendoredLine = ReasonLine(LowConfidenceReasons.DemotedVendoredFile);
		string genericLine = ReasonLine(LowConfidenceReasons.DemotedFile);
		string bothLine = ReasonLine(LowConfidenceReasons.DemotedFile | LowConfidenceReasons.DemotedVendoredFile);

		Assert.Equal(2, CountOf(report, vendoredLine + "\n"));
		Assert.Equal(1, CountOf(report, genericLine + "\n"));
		Assert.Equal(1, CountOf(report, bothLine + "\n"));
		Assert.DoesNotContain(
			"vendor",
			LowReasonVocabulary.SentenceOf(LowConfidenceReasons.DemotedFile),
			StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void TheDataArtifactCarriesTheVendoredValueApartFromTheGenericOne()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		ArtifactDocument artifact = ReadArtifact(harness);

		Assert.Equal([_VENDORED_VALUE], ArtifactFindingIn(artifact, _VENDORED_FILE).LowReasons!);
		Assert.Equal([_VENDORED_VALUE], ArtifactFindingIn(artifact, _THIRD_PARTY_FILE).LowReasons!);
		Assert.Equal([_GENERIC_VALUE], ArtifactFindingIn(artifact, _DEPLOYMENT_FILE).LowReasons!);
		Assert.Equal([_GENERIC_VALUE, _VENDORED_VALUE], ArtifactFindingIn(artifact, _VENDORED_AND_TEST_FILE).LowReasons!);
		Assert.Null(ArtifactFindingIn(artifact, _OWN_CODE_FILE).LowReasons);
	}

	[Fact]
	public void TheVendoredReasonSurvivesAResume()
	{
		using PipelineHarness harness = RunTheWorkflowOverThePlantedTree();

		SessionStateLoad reloaded = harness.ReloadState();

		Assert.True(reloaded.WasResumed);
		Assert.Equal(
			LowConfidenceReasons.DemotedVendoredFile,
			FindingIn(reloaded.State.Findings, _VENDORED_FILE).LowReasons);
	}

	private static PipelineHarness RunTheWorkflowOverThePlantedTree()
	{
		PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.WriteSourceFile(_VENDORED_FILE, _content);
		harness.WriteSourceFile(_THIRD_PARTY_FILE, _content);
		harness.WriteSourceFile(_VENDORED_AND_TEST_FILE, _content);
		harness.WriteSourceFile(_DEPLOYMENT_FILE, _content);
		harness.WriteSourceFile(_OWN_CODE_FILE, _content);
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
