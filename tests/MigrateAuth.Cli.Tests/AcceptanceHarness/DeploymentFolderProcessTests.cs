using System.Text.Json;

using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE DEPLOYMENT-FOLDER DEMOTION THROUGH THE REAL, BUILT PROCESS: a <c>CSharp</c> High construct under <c>Deployment/</c> is reported
/// Low with the reason <c>demotedDeploymentFolder</c>, and the same construct elsewhere stays High.
///
/// ONLY THE <c>CSharp</c> HALF CAN RUN HERE. The built process loads the embedded catalog, which registers no
/// other language yet, and a test cannot inject an analyzer into it. The non-<c>CSharp</c> outcome (High under
/// <c>Deployment/</c>) is proven in process by <c>Pipeline.DeploymentFolderPerLanguagePipelineTests</c> and,
/// at the recognition level, by <c>Recognition.DemotionSignalTests</c>.
/// </summary>
public sealed class DeploymentFolderProcessTests
{
	private const string _DEPLOYMENT_FILE_PATH = "Deployment/Library.cs";
	private const string _OWN_CODE_FILE_PATH = "src/Library.cs";
	private const string _HIGH_CONFIDENCE_NAME = "High";
	private const string _LOW_CONFIDENCE_NAME = "Low";
	private const string _DEMOTED_DEPLOYMENT_FOLDER_REASON = "demotedDeploymentFolder";

	private const string _CONTENT =
		"class HighFindingFixture\n{\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	[Fact]
	public void ACSharpHighConstructUnderDeploymentIsLowWithTheDemotedDeploymentFolderReasonInARealProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_DEPLOYMENT_FILE_PATH, _CONTENT);
		repository.WriteText(_OWN_CODE_FILE_PATH, _CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		ArtifactDocument artifact = ReadArtifact(destination.Path);
		ArtifactFindingDocument demoted = artifact.Findings.Single(finding => finding.FilePath == _DEPLOYMENT_FILE_PATH);
		ArtifactFindingDocument own = artifact.Findings.Single(finding => finding.FilePath == _OWN_CODE_FILE_PATH);

		Assert.Equal(_LOW_CONFIDENCE_NAME, demoted.Confidence);
		Assert.Equal([_DEMOTED_DEPLOYMENT_FOLDER_REASON], demoted.LowReasons!);
		Assert.Equal(_HIGH_CONFIDENCE_NAME, own.Confidence);
		Assert.Null(own.LowReasons);
	}

	private static ArtifactDocument ReadArtifact(string destinationFolderPath)
	{
		ArtifactDocument? document = JsonSerializer.Deserialize(
			ProducedArtifacts.TextOfTheOnly(destinationFolderPath, OutputArtifactKind.StructuredDataArtifact),
			ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);

		return document;
	}
}
