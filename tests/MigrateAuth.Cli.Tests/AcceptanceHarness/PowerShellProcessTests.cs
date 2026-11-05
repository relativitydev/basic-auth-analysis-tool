using System.Text.Json;

using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// A `PowerShell` repository through a REAL PROCESS, from scan to report: the three kinds of file the analyzer owns - a script, a module, and an
/// extensionless file whose first line names the interpreter - are assessed, found, attributed to the language by name and written to the artifact.
/// The scripts are invented placeholders.
/// </summary>
public sealed class PowerShellProcessTests
{
	private const string _LANGUAGE_NAME = "PowerShell";
	private const string _SCRIPT_PATH = "src/Upload.ps1";
	private const string _MODULE_PATH = "src/Client.psm1";
	private const string _SHEBANG_PATH = "tools/run-upload";
	private const string _ORDINARY_PATH = "src/Notes.ps1";
	private const string _EXPLICIT_SCHEME_PATTERN_CLASS = "PS-003";
	private const string _HEADER_HASHTABLE_PATTERN_CLASS = "PS-004";
	private const string _HIGH_CONFIDENCE_NAME = "High";
	private const string _MEDIUM_CONFIDENCE_NAME = "Medium";
	private const string _CLIENT_CREDENTIALS_FLOW_NAME = "ClientCredentials";
	private const string _AUTHORIZATION_CODE_FLOW_NAME = "AuthorizationCode";

	private const string _SCRIPT_CONTENT =
		"$credential = Get-Credential -Message 'Sign in'\r\nInvoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic\r\n";

	private const string _MODULE_CONTENT =
		"$secret = $env:SERVICE_PASSWORD\nfunction Send-Request {\n\tInvoke-RestMethod -Uri $uri -Headers @{ Authorization = \"Basic $encoded\" }\n}\n";

	private const string _SHEBANG_CONTENT =
		"#!/usr/bin/env pwsh\nInvoke-WebRequest -Uri $uri -Credential $credential -Authentication Basic\n";

	private const string _ORDINARY_CONTENT = "Write-Host 'nothing about authentication here'\n";

	[Fact]
	public void AScriptAModuleAndAShebangFileAreAssessedFoundAndAttributedToPowerShellInARealProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_SCRIPT_PATH, _SCRIPT_CONTENT);
		repository.WriteText(_MODULE_PATH, _MODULE_CONTENT);
		repository.WriteText(_SHEBANG_PATH, _SHEBANG_CONTENT);
		repository.WriteText(_ORDINARY_PATH, _ORDINARY_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		ArtifactDocument artifact = ReadArtifact(destination.Path);

		Assert.Equal(
			new[] { _SHEBANG_PATH, _SCRIPT_PATH, _MODULE_PATH }.Order(StringComparer.Ordinal),
			artifact.Findings.Select(finding => finding.FilePath).Order(StringComparer.Ordinal));
		Assert.All(artifact.Findings, finding => Assert.Equal(_LANGUAGE_NAME, finding.Language));

		ArtifactFindingDocument script = artifact.Findings.Single(finding => finding.FilePath == _SCRIPT_PATH);
		ArtifactFindingDocument module = artifact.Findings.Single(finding => finding.FilePath == _MODULE_PATH);
		ArtifactFindingDocument shebang = artifact.Findings.Single(finding => finding.FilePath == _SHEBANG_PATH);

		Assert.Equal(_EXPLICIT_SCHEME_PATTERN_CLASS, script.PatternClass);
		Assert.Equal(_HIGH_CONFIDENCE_NAME, script.Confidence);
		Assert.Equal(_AUTHORIZATION_CODE_FLOW_NAME, script.Flow);
		Assert.Equal(_HEADER_HASHTABLE_PATTERN_CLASS, module.PatternClass);
		Assert.Equal(_MEDIUM_CONFIDENCE_NAME, module.Confidence);
		Assert.Equal(_CLIENT_CREDENTIALS_FLOW_NAME, module.Flow);
		Assert.Equal(_EXPLICIT_SCHEME_PATTERN_CLASS, shebang.PatternClass);
		Assert.Equal(_HIGH_CONFIDENCE_NAME, shebang.Confidence);
		Assert.NotNull(script.Recommendation);
		Assert.NotNull(module.Recommendation);
	}

	[Fact]
	public void ThePowerShellFilesAreNoLongerCountedAsAFileInALanguageTheReleaseDoesNotAssess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_ORDINARY_PATH, _ORDINARY_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);

		string[] powerShellRows = [.. outcome.StandardOutput.Split('\n').Where(line => line.Contains(_LANGUAGE_NAME, StringComparison.Ordinal))];

		Assert.NotEmpty(powerShellRows);
		Assert.All(powerShellRows, row => Assert.DoesNotContain(ScanStage.UNASSESSED_ROW_SUFFIX.Trim(), row, StringComparison.Ordinal));
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
