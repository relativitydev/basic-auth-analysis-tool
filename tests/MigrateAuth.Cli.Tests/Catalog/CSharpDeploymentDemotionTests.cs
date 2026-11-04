using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `Deployment` FOLDER AS A `CSharp` LANGUAGE-SHAPED FOLDER SIGNAL, THROUGH THE REAL EMBEDDED ANALYZER.
///
/// This suite confirms the signal is declared once, by this analyzer, as a whole-segment, case-insensitive demotion, and that a
/// folder which only contains the word and a file called <c>Deployment.cs</c> are not demoted. The same High construct is
/// recognized under different paths, so each level read is a measurement of the path alone.
/// </summary>
public sealed class CSharpDeploymentDemotionTests
{
	private const string _HIGH_CONSTRUCT = "var result = client.WithBasicAuth(operatorName, accessKey);";
	private const string _DEPLOYMENT_FOLDER_NAME = "Deployment";

	[Fact]
	public void TheAnalyzerDeclaresTheDeploymentFolderSignalOnceAsAWholeSegmentCaseInsensitiveDemotion()
	{
		ImmutableArray<FolderNameSignal> declared =
		[
			.. new CSharpRecognitionHarness().Analyzer.DemotionSignals.FolderNameSignals
				.Where(signal => string.Equals(signal.FolderName, _DEPLOYMENT_FOLDER_NAME, StringComparison.OrdinalIgnoreCase))
		];

		FolderNameSignal signal = Assert.Single(declared);

		Assert.Equal(FolderNameMatch.ExactName, signal.Match);
		Assert.Equal(CaseSensitivity.Insensitive, signal.Sensitivity);
		Assert.False(signal.MarksVendoredCode);
	}

	[Fact]
	public void NoFileNameSignalIsDeclaredForTheDeploymentWord()
	{
		Assert.DoesNotContain(
			new CSharpRecognitionHarness().Analyzer.DemotionSignals.FileNameSignals,
			signal => signal.Text.Contains(_DEPLOYMENT_FOLDER_NAME, StringComparison.OrdinalIgnoreCase));
	}

	[Theory]
	[InlineData("Deployment/Client.cs")]
	[InlineData("deployment/Client.cs")]
	[InlineData("DEPLOYMENT/Client.cs")]
	[InlineData("src/Deployment/Client.cs")]
	[InlineData("src/tools/Deployment/nested/Client.cs")]
	public void AWholeSegmentDeploymentFolderDemotesInAnyCasing(string relativePath)
	{
		Assert.Equal(ConfidenceLevel.Low, LevelAt(relativePath));
	}

	[Theory]
	[InlineData("DeploymentTools/Client.cs")]
	[InlineData("src/DeploymentTools/Client.cs")]
	[InlineData("src/Deployments/Client.cs")]
	[InlineData("src/PreDeployment/Client.cs")]
	[InlineData("src/Deployment.cs")]
	[InlineData("src/Deployment.Client.cs")]
	public void AFolderThatOnlyContainsTheWordAndAFileNamedAfterItAreNotDemoted(string relativePath)
	{
		Assert.Equal(ConfidenceLevel.High, LevelAt(relativePath));
	}

	private static ConfidenceLevel LevelAt(string relativePath)
	{
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(_HIGH_CONSTRUCT, relativePath));

		return construct.Level;
	}
}
