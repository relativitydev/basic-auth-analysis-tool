using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE DEMOTION KINDS PER LANGUAGE, THROUGH THE REAL ANALYZERS AND THE REAL PIPELINE.
///
/// The same High construct is placed in files whose only difference is the demotion kind their name, folder
/// or content declares. A file whose signals are all of the Test kind reports Medium, keeps a flow and a
/// recommendation, and is marked as test code. A file with any other kind reports Low with that kind's reason
/// and no flow; Test beside another kind is Low and is still marked as test code, at every level.
///
/// <c>PowerShell</c> declares no mock file-name signal, so its mock is a mock folder.
/// </summary>
public sealed class DemotionKindFixturesPerLanguagePipelineTests
{
	private const string _RETIRED_GENERAL_REASON_VALUE = "demotedFile";

	private const string _CSHARP_CONSTRUCT =
		"class Fixture\n{\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	private const string _CSHARP_FRAMEWORK_MARKER_CONTENT =
		"class Fixture\n{\n\t[Fact]\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	private const string _POWERSHELL_CONSTRUCT = "Invoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic\n";

	private const string _POWERSHELL_FRAMEWORK_MARKER_CONTENT =
		"Describe 'the client' {\n\tIt 'sends the header' {\n\t\tInvoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic | Should -Not -BeNullOrEmpty\n\t}\n}\n";

	private const string _VBNET_CONSTRUCT =
		"Module Fixture\n\tSub Build()\n\t\tDim credentials As New UsernamePasswordCredentials(options.UserName, options.Password)\n\tEnd Sub\nEnd Module\n";

	private const string _VBNET_FRAMEWORK_MARKER_CONTENT =
		"Module Fixture\n\t<TestMethod>\n\tSub Build()\n\t\tDim credentials As New UsernamePasswordCredentials(options.UserName, options.Password)\n\tEnd Sub\nEnd Module\n";

	// ------------------------------------------------------------------------------------------
	// CSharp
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CSharpTestFolderTestFileNameAndTestFrameworkMarkerReportMediumWithFlowAndRecommendation()
	{
		AssertMediumWithFlowAndRecommendationAndTestCode(
			("tests/Client.cs", _CSHARP_CONSTRUCT),
			("src/Client.Tests.cs", _CSHARP_CONSTRUCT),
			("src/Checks.cs", _CSHARP_FRAMEWORK_MARKER_CONTENT));
	}

	[Fact]
	public void CSharpMockReportsLowAndTestPlusMockReportsLowStillMarkedAsTestCode()
	{
		AssertLowWithReason(LowConfidenceReasons.DemotedMockFile, false, ("src/FakeClient.cs", _CSHARP_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedMockFile, true, ("tests/FakeClient.cs", _CSHARP_CONSTRUCT));
	}

	[Fact]
	public void CSharpSampleVendoredGeneratedAndDeploymentReportLowWithTheirOwnReasonAndNoTestCode()
	{
		AssertLowWithReason(LowConfidenceReasons.DemotedSampleFile, false, ("src/SampleClient.cs", _CSHARP_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedVendoredFile, false, ("vendor/Client.cs", _CSHARP_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedGeneratedFile, false, ("src/Client.g.cs", _CSHARP_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedDeploymentFolder, false, ("Deployment/Client.cs", _CSHARP_CONSTRUCT));
	}

	// ------------------------------------------------------------------------------------------
	// PowerShell
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void PowerShellTestFolderTestFileNameAndPesterMarkerReportMediumWithFlowAndRecommendation()
	{
		AssertMediumWithFlowAndRecommendationAndTestCode(
			("tests/Client.ps1", _POWERSHELL_CONSTRUCT),
			("src/Client.Tests.ps1", _POWERSHELL_CONSTRUCT),
			("src/Checks.ps1", _POWERSHELL_FRAMEWORK_MARKER_CONTENT));
	}

	[Fact]
	public void PowerShellMockFolderReportsLowAndTestPlusMockReportsLowStillMarkedAsTestCode()
	{
		AssertLowWithReason(LowConfidenceReasons.DemotedMockFile, false, ("mocks/Client.ps1", _POWERSHELL_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedMockFile, true, ("tests/mocks/Client.ps1", _POWERSHELL_CONSTRUCT));
	}

	[Fact]
	public void PowerShellSampleVendoredAndGeneratedReportLowWithTheirOwnReasonAndNoTestCode()
	{
		AssertLowWithReason(LowConfidenceReasons.DemotedSampleFile, false, ("samples/Client.ps1", _POWERSHELL_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedVendoredFile, false, ("vendor/Client.ps1", _POWERSHELL_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedGeneratedFile, false, ("src/Client.generated.ps1", _POWERSHELL_CONSTRUCT));
	}

	/// <summary>PowerShell has no mock file-name signal, so a mock-named test file outside a mock folder is Medium.</summary>
	[Fact]
	public void PowerShellMockNamedTestFileOutsideAMockFolderReportsMedium()
	{
		AssertMediumWithFlowAndRecommendationAndTestCode(("tests/FakeClient.ps1", _POWERSHELL_CONSTRUCT));
	}

	// ------------------------------------------------------------------------------------------
	// VBNet
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void VBNetTestFolderTestFileNameAndTestFrameworkMarkerReportMediumWithFlowAndRecommendation()
	{
		AssertMediumWithFlowAndRecommendationAndTestCode(
			("tests/Client.vb", _VBNET_CONSTRUCT),
			("src/Client.Tests.vb", _VBNET_CONSTRUCT),
			("src/Checks.vb", _VBNET_FRAMEWORK_MARKER_CONTENT));
	}

	[Fact]
	public void VBNetMockReportsLowAndTestPlusMockReportsLowStillMarkedAsTestCode()
	{
		AssertLowWithReason(LowConfidenceReasons.DemotedMockFile, false, ("src/FakeClient.vb", _VBNET_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedMockFile, true, ("tests/FakeClient.vb", _VBNET_CONSTRUCT));
	}

	[Fact]
	public void VBNetSampleVendoredAndGeneratedReportLowWithTheirOwnReasonAndNoTestCode()
	{
		AssertLowWithReason(LowConfidenceReasons.DemotedSampleFile, false, ("src/SampleClient.vb", _VBNET_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedVendoredFile, false, ("vendor/Client.vb", _VBNET_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedGeneratedFile, false, ("src/Client.Designer.vb", _VBNET_CONSTRUCT));
		AssertLowWithReason(LowConfidenceReasons.DemotedGeneratedFile, false, ("Service References/Client.vb", _VBNET_CONSTRUCT));
	}

	// ------------------------------------------------------------------------------------------
	// All languages: no finding ever carries the reserved general reason
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void NoFindingOfAnyLanguageCarriesTheGeneralDemotedFileReason()
	{
		(string Path, string Content)[] files =
		[
			("src/FakeClient.cs", _CSHARP_CONSTRUCT),
			("vendor/Client.cs", _CSHARP_CONSTRUCT),
			("Deployment/Client.cs", _CSHARP_CONSTRUCT),
			("tests/Client.cs", _CSHARP_CONSTRUCT),
			("mocks/Client.ps1", _POWERSHELL_CONSTRUCT),
			("src/Client.generated.ps1", _POWERSHELL_CONSTRUCT),
			("src/SampleClient.vb", _VBNET_CONSTRUCT),
			("tests/FakeClient.vb", _VBNET_CONSTRUCT)
		];

		using PipelineHarness harness = RunTheWorkflowOver(files);

		Assert.Equal(files.Length, harness.State.Findings.Length);
		Assert.All(
			harness.State.Findings,
			finding => Assert.False(finding.LowReasons.HasFlag(LowConfidenceReasons.DemotedFile), finding.Path.Value));
		Assert.All(
			ReadArtifact(harness).Findings,
			finding => Assert.DoesNotContain(_RETIRED_GENERAL_REASON_VALUE, finding.LowReasons ?? [], StringComparer.Ordinal));
	}

	private static void AssertMediumWithFlowAndRecommendationAndTestCode(params (string Path, string Content)[] files)
	{
		using PipelineHarness harness = RunTheWorkflowOver(files);
		ArtifactDocument artifact = ReadArtifact(harness);

		foreach ((string path, _) in files)
		{
			Finding finding = FindingIn(harness.State.Findings, path);
			ArtifactFindingDocument documented = ArtifactFindingIn(artifact, path);

			Assert.True(finding.Level == ConfidenceLevel.Medium, $"{path}: level {finding.Level}");
			Assert.True(finding.TestCode, path);
			Assert.Equal(LowConfidenceReasons.None, finding.LowReasons);
			Assert.NotNull(finding.Flow);
			Assert.NotNull(documented.Flow);
			Assert.False(string.IsNullOrWhiteSpace(documented.Recommendation), path);
			Assert.Null(documented.LowReasons);
			Assert.True(documented.TestCode, path);
		}
	}

	private static void AssertLowWithReason(
		LowConfidenceReasons expectedReason,
		bool expectedTestCode,
		params (string Path, string Content)[] files)
	{
		using PipelineHarness harness = RunTheWorkflowOver(files);
		ArtifactDocument artifact = ReadArtifact(harness);

		foreach ((string path, _) in files)
		{
			Finding finding = FindingIn(harness.State.Findings, path);
			ArtifactFindingDocument documented = ArtifactFindingIn(artifact, path);

			Assert.True(finding.Level == ConfidenceLevel.Low, $"{path}: level {finding.Level}");
			Assert.Equal(expectedReason, finding.LowReasons);
			Assert.Equal(expectedTestCode, finding.TestCode);
			Assert.Null(finding.Flow);
			Assert.Null(documented.Recommendation);
			Assert.Equal(expectedTestCode, documented.TestCode);
		}
	}

	private static PipelineHarness RunTheWorkflowOver(IEnumerable<(string Path, string Content)> files)
	{
		PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		foreach ((string path, string content) in files)
		{
			harness.WriteSourceFile(path, content);
		}

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
