using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `PowerShell` ANALYZER'S NINE FLOW SIGNALS, THROUGH THE REAL EMBEDDED ANALYZER, THE REAL LEXICAL SCANNER AND THE REAL RECOGNITION ENGINE, AND THE
/// FLOW A FINDING RENDERS UNDER THEM THROUGH THE REAL FOUR-STAGE PIPELINE.
///
/// Every script is invented from the catalog's own prose, with placeholder names, and carries no fragment, path or third-party name from any
/// repository.
/// </summary>
public sealed class PowerShellFlowSignalTests
{
	private const string _HIGH_CONSTRUCT = "Invoke-RestMethod -Uri $uri -Credential $credential -Authentication Basic";
	private const string _NEWLINE = "\n";

	private const string _INTERACTIVE_FILE_NAME = "Interactive.ps1";
	private const string _FIXED_FILE_NAME = "Fixed.ps1";
	private const string _PLAIN_FILE_NAME = "Plain.ps1";
	private const string _BOTH_KINDS_FILE_NAME = "BothKinds.ps1";

	private const string _AUTHORIZATION_CODE_GRANT = "grant_type=authorization_code";
	private const string _CLIENT_CREDENTIALS_GRANT = "grant_type=client_credentials";

	private static readonly ImmutableArray<(string Name, FlowSignalKind Kind, FlowSignalZone Zone)> _declaredTable =
	[
		("GetCredentialCall", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("ReadHostSecureInput", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("PromptForCredentialCall", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("ReadHostCredentialPromptText", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("DesktopPasswordControl", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("EnvironmentCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("ImportClixmlRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("SecretStoreRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("PlainTextSecureString", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly)
	];

	private const string _INTERACTIVE_FILE_CONTENT = "$credential = Get-Credential -Message 'Sign in'\n" + _HIGH_CONSTRUCT + "\n";

	private const string _FIXED_FILE_CONTENT = "$secret = $env:SERVICE_SECRET\n" + _HIGH_CONSTRUCT + "\n";

	private const string _PLAIN_FILE_CONTENT = _HIGH_CONSTRUCT + "\n";

	private const string _BOTH_KINDS_FILE_CONTENT =
		"$credential = $null\n"
		+ "if ($env:SERVICE_PASSWORD) { $credential = New-Object System.Management.Automation.PSCredential('x', $secure) }\n"
		+ "if (-not $credential) { $credential = Get-Credential }\n"
		+ _HIGH_CONSTRUCT + "\n";

	/// <summary>At least one signal of each kind, and the table of signals, each with its kind and its zone, exactly as the catalog declares it.</summary>
	[Fact]
	public void TheDeclaredSignalsCarryTheCatalogsKindAndZoneEachAndAtLeastOneOfEachKindExists()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(
			_declaredTable.Select(row => (row.Name, row.Kind, row.Zone)).ToArray(),
			signals.Select(signal => (signal.Name, signal.Kind, signal.Zone)).ToArray());
		Assert.Contains(signals, signal => signal.Kind == FlowSignalKind.InteractiveUser);
		Assert.Contains(signals, signal => signal.Kind == FlowSignalKind.FixedCredential);
	}

	/// <summary>Every signal declares the policy match timeout, as the engine's bounded evaluation requires.</summary>
	[Fact]
	public void EverySignalExpressionDeclaresAMatchTimeout()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.All(signals, signal => Assert.NotEqual(System.Text.RegularExpressions.Regex.InfiniteMatchTimeout, signal.Expression.MatchTimeout));
	}

	/// <summary>A CodeOnly signal written in a comment or a string body is not evidence; the prompt-text signal is read in a string body but not in a comment.</summary>
	[Theory]
	[InlineData("# $credential = Get-Credential", FlowEvidence.None)]
	[InlineData("<# $env:SERVICE_PASSWORD #>", FlowEvidence.None)]
	[InlineData("$note = 'run Import-Clixml on the saved file'", FlowEvidence.None)]
	[InlineData("# $u = Read-Host 'Enter your password'", FlowEvidence.None)]
	[InlineData("$u = Read-Host 'Enter your password'", FlowEvidence.Interactive)]
	public void ASignalIsReadInItsZoneAndNotInAnotherOne(string signalSource, FlowEvidence expected)
	{
		RecognitionResult result = new PowerShellRecognitionHarness().RecognizeFully(signalSource + _NEWLINE + _HIGH_CONSTRUCT + _NEWLINE);

		Assert.Equal(expected, result.FlowEvidence);
	}

	/// <summary>The credential prompt that is directly a remoting-family cmdlet's credential argument authenticates to Windows and says nothing about the flow.</summary>
	[Theory]
	[InlineData("Invoke-Command -ComputerName $server -Credential (Get-Credential) -ScriptBlock { hostname }")]
	[InlineData("Invoke-Command -ComputerName $server -Credential:(Get-Credential) -ScriptBlock { hostname }")]
	[InlineData("Enter-PSSession -ComputerName $server -Credential $(Get-Credential)")]
	[InlineData("$session = New-PSSession -ComputerName $server -Cred (Get-Credential)")]
	public void ARemotingCmdletsDirectCredentialPromptIsNotAnInteractiveSignal(string remotingSource)
	{
		RecognitionResult result = new PowerShellRecognitionHarness().RecognizeFully(remotingSource + _NEWLINE + _HIGH_CONSTRUCT + _NEWLINE);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	/// <summary>The exclusion is the direct form only: a prompt used for something other than a remoting argument still counts.</summary>
	[Theory]
	[InlineData("$credential = Get-Credential\nInvoke-Command -ComputerName $server -Credential $credential -ScriptBlock { hostname }")]
	[InlineData("Invoke-Command -ComputerName $server -ScriptBlock { hostname }\n$credential = Get-Credential")]
	public void AnIndirectCredentialPromptNextToARemotingCallIsStillAnInteractiveSignal(string source)
	{
		RecognitionResult result = new PowerShellRecognitionHarness().RecognizeFully(source + _NEWLINE + _HIGH_CONSTRUCT + _NEWLINE);

		Assert.Equal(FlowEvidence.Interactive, result.FlowEvidence);
	}

	/// <summary>An interactive signal renders Authorization Code, with its note and its token acquisition; no signal renders the default note.</summary>
	[Fact]
	public void AnInteractiveSignalRendersAuthorizationCodeAndNoSignalRendersTheDefaultNote()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_INTERACTIVE_FILE_NAME, _INTERACTIVE_FILE_CONTENT);
		harness.WriteSourceFile(_FIXED_FILE_NAME, _FIXED_FILE_CONTENT);
		harness.WriteSourceFile(_PLAIN_FILE_NAME, _PLAIN_FILE_CONTENT);

		harness.RunTheWholeWorkflow();

		ArtifactDocument document = ReadArtifact(harness);
		ArtifactFindingDocument interactive = FindingOf(document, _INTERACTIVE_FILE_NAME);
		ArtifactFindingDocument fixedCredential = FindingOf(document, _FIXED_FILE_NAME);
		ArtifactFindingDocument plain = FindingOf(document, _PLAIN_FILE_NAME);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), interactive.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), interactive.FlowDecision);
		Assert.Equal(FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE, interactive.FlowNote);
		Assert.Contains(_AUTHORIZATION_CODE_GRANT, interactive.Recommendation, StringComparison.Ordinal);
		Assert.DoesNotContain(_CLIENT_CREDENTIALS_GRANT, interactive.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), fixedCredential.Flow);
		Assert.Equal(nameof(FlowDecision.FixedSignal), fixedCredential.FlowDecision);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, fixedCredential.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), plain.Flow);
		Assert.Equal(nameof(FlowDecision.Default), plain.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, plain.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, plain.Recommendation, StringComparison.Ordinal);
	}

	/// <summary>
	/// KNOWN LIMIT. A file that holds both an interactive signal and a fixed-credential signal, the "stored or environment credential, else
	/// prompt" idiom, is assigned Authorization Code, because the shared rule lets an interactive signal win. The flow expected for that idiom is
	/// Client Credentials, so the advice differs from the expected flow; the mismatch is disclosed in the limits, and this test pins it so
	/// that a change to the rule cannot move it unseen.
	/// </summary>
	[Fact]
	public void AFileWithBothKindsOfEvidenceIsAssignedAuthorizationCodeAndThatMismatchWithTheExpectedFlowIsAnAcceptedLimit()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_BOTH_KINDS_FILE_NAME, _BOTH_KINDS_FILE_CONTENT);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _BOTH_KINDS_FILE_NAME);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), finding.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), finding.FlowDecision);
	}

	private static ArtifactDocument ReadArtifact(PipelineHarness harness)
	{
		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		return JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument)!;
	}

	private static ArtifactFindingDocument FindingOf(ArtifactDocument document, string fileName)
	{
		return document.Findings.Single(finding => finding.FilePath == fileName);
	}

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		return PowerShellRenderedTexts.Analyzer();
	}
}
