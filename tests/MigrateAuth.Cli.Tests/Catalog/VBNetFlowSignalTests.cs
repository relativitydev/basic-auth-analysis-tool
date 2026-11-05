using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `VBNet` ANALYZER'S TWENTY FLOW SIGNALS, THROUGH THE REAL EMBEDDED ANALYZER, THE REAL LEXICAL SCANNER AND THE REAL RECOGNITION ENGINE, AND THE
/// FLOW A FINDING RENDERS UNDER THEM THROUGH THE REAL FOUR-STAGE PIPELINE.
///
/// Every source is invented from the catalog's own prose, with placeholder names.
/// One fixture per signal, one near-miss per exclusion the catalog states, the literal rule, the mixed-evidence limit, and the credential-literal warning
/// of the console surface.
/// </summary>
public sealed class VBNetFlowSignalTests
{
	private const string _HIGH_CONSTRUCT = "Dim credentials As New UsernamePasswordCredentials(userName, password)";
	private const string _NEWLINE = "\n";

	private const string _INTERACTIVE_FILE_NAME = "Interactive.vb";
	private const string _FIXED_FILE_NAME = "Fixed.vb";
	private const string _PLAIN_FILE_NAME = "Plain.vb";
	private const string _BOTH_KINDS_FILE_NAME = "BothKinds.vb";
	private const string _LITERAL_ONLY_FILE_NAME = "LiteralOnly.vb";
	private const string _CREDENTIAL_FILE_NAME = "Credential.vb";

	private const string _AUTHORIZATION_CODE_GRANT = "grant_type=authorization_code";
	private const string _CLIENT_CREDENTIALS_GRANT = "grant_type=client_credentials";
	private const string _CODE_USE_SITE = "BearerTokenCredentials(accessToken)";
	private const string _VBNET_TOKEN_ACQUISITION = "listener.Prefixes.Add";
	private const string _PLACED_SECRET = "a-literal-secret";
	private const string _WARNING_PREFIX = "Warning:";

	private static readonly ImmutableArray<(string Name, FlowSignalKind Kind, FlowSignalZone Zone)> _declaredTable =
	[
		("ConsolePasswordPromptText", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("ConsoleReadLineIntoCredentialVariable", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("ConsoleMaskedKeyRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("InputBoxCredentialPrompt", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("WpfPasswordBoxRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("WinFormsPasswordMasking", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("PasswordSignInCall", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("WebFormsLoginHandlerBinding", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("PasswordDataTypeAttribute", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("HtmlPasswordInputMarkup", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("PostedFormCredentialFieldRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("ConfigurationIndexerCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("ConfigurationManagerCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("MySettingsCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("PlatformInstanceSettingCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOrStringBody),
		("EnvironmentVariableCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("SecretClientRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("RelativitySecretStoreRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("ProtectedDataUnprotectRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("UserSecretsConfigurationSource", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly)
	];

	/// <summary>One fixture per signal: the signal's name, the evidence it gives and a source in which it reads.</summary>
	private static readonly ImmutableArray<(string Signal, FlowEvidence Evidence, string Source)> _signalFixtures =
	[
		("ConsolePasswordPromptText", FlowEvidence.Interactive, "Console.Write(\"Enter your password: \")"),
		("ConsoleReadLineIntoCredentialVariable", FlowEvidence.Interactive, "Dim password As String = Console.ReadLine()"),
		("ConsoleMaskedKeyRead", FlowEvidence.Interactive, "Dim pressed = Console.ReadKey(True)"),
		("InputBoxCredentialPrompt", FlowEvidence.Interactive, "Dim secret As String = InputBox(\"Enter your password\")"),
		("WpfPasswordBoxRead", FlowEvidence.Interactive, "Dim box As PasswordBox\nDim typed = box.Password"),
		("WinFormsPasswordMasking", FlowEvidence.Interactive, "passwordField.UseSystemPasswordChar = True"),
		("PasswordSignInCall", FlowEvidence.Interactive, "Dim result = Await SignInManager.PasswordSignInAsync(userName, password, False, False)"),
		("WebFormsLoginHandlerBinding", FlowEvidence.Interactive, "Protected Sub SignIn_Authenticate(sender As Object, e As EventArgs) Handles SignIn.Authenticate\nEnd Sub"),
		("PasswordDataTypeAttribute", FlowEvidence.Interactive, "<DataType(DataType.Password)>\nPublic Property Entry As String"),
		("HtmlPasswordInputMarkup", FlowEvidence.Interactive, "Dim markup As String = \"<input type=\"\"password\"\" name=\"\"entry\"\" />\""),
		("PostedFormCredentialFieldRead", FlowEvidence.Interactive, "Dim typed = Request.Form(\"password\")"),
		("ConfigurationIndexerCredentialRead", FlowEvidence.Fixed, "Dim stored = configuration(\"Section:ApiSecret\")"),
		("ConfigurationManagerCredentialRead", FlowEvidence.Fixed, "Dim stored = ConfigurationManager.AppSettings(\"ApiPassword\")"),
		("MySettingsCredentialRead", FlowEvidence.Fixed, "Dim stored = My.Settings.ApiPassword"),
		("PlatformInstanceSettingCredentialRead", FlowEvidence.Fixed, "Dim stored = settings.GetString(\"Section\", \"ApiPassword\")"),
		("EnvironmentVariableCredentialRead", FlowEvidence.Fixed, "Dim stored = Environment.GetEnvironmentVariable(\"SERVICE_SECRET\")"),
		("SecretClientRead", FlowEvidence.Fixed, "Dim vault As SecretClient\nDim stored = vault.GetSecretAsync(\"name\")"),
		("RelativitySecretStoreRead", FlowEvidence.Fixed, "Dim stored = Helper.GetSecretStore().Get(path)"),
		("ProtectedDataUnprotectRead", FlowEvidence.Fixed, "Dim stored = ProtectedData.Unprotect(bytes, Nothing, DataProtectionScope.CurrentUser)"),
		("UserSecretsConfigurationSource", FlowEvidence.Fixed, "builder.AddUserSecrets(Of Startup)()")
	];

	private static readonly string _interactiveFileContent = "Dim password As String = Console.ReadLine()\n" + _HIGH_CONSTRUCT + "\n";

	private static readonly string _fixedFileContent = "Dim stored = My.Settings.ApiPassword\n" + _HIGH_CONSTRUCT + "\n";

	private static readonly string _bothKindsFileContent =
		"Dim stored = Environment.GetEnvironmentVariable(\"SERVICE_PASSWORD\")\n"
		+ "If stored Is Nothing Then stored = InputBox(\"Enter your password\")\n"
		+ _HIGH_CONSTRUCT + "\n";

	private static readonly string _literalOnlyFileContent = "Dim credentials As New UsernamePasswordCredentials(\"someone\", \"" + _PLACED_SECRET + "\")\n";

	public static TheoryData<string, FlowEvidence, string> SignalFixtures()
	{
		TheoryData<string, FlowEvidence, string> data = [];

		foreach ((string signal, FlowEvidence evidence, string source) in _signalFixtures)
		{
			data.Add(signal, evidence, source);
		}

		return data;
	}

	/// <summary>Eleven interactive signals and nine fixed-credential ones, each with the kind and the zone the catalog declares.</summary>
	[Fact]
	public void TheDeclaredSignalsCarryTheCatalogsKindAndZoneEachAndBothKindsExist()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(
			_declaredTable.Select(row => (row.Name, row.Kind, row.Zone)).ToArray(),
			signals.Select(signal => (signal.Name, signal.Kind, signal.Zone)).ToArray());
		Assert.Equal(11, signals.Count(signal => signal.Kind == FlowSignalKind.InteractiveUser));
		Assert.Equal(9, signals.Count(signal => signal.Kind == FlowSignalKind.FixedCredential));
	}

	[Fact]
	public void EverySignalExpressionDeclaresAMatchTimeout()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.All(signals, signal => Assert.NotEqual(System.Text.RegularExpressions.Regex.InfiniteMatchTimeout, signal.Expression.MatchTimeout));
	}

	[Fact]
	public void EverySignalOfTheTableHasOneFixture()
	{
		Assert.Equal(
			_declaredTable.Select(row => row.Name).Order(StringComparer.Ordinal),
			_signalFixtures.Select(fixture => fixture.Signal).Order(StringComparer.Ordinal));
	}

	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalGivesItsEvidenceOnItsOwnFixture(string signal, FlowEvidence expected, string source)
	{
		RecognitionResult result = new VBNetRecognitionHarness().RecognizeFully(source + _NEWLINE + _HIGH_CONSTRUCT + _NEWLINE);

		Assert.True(expected == result.FlowEvidence, $"{signal}: expected {expected}, got {result.FlowEvidence}.");
	}

	/// <summary>A signal commented out is not evidence, in either zone kind.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalInACommentIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		string commentedOut = string.Join(_NEWLINE, source.Split(_NEWLINE).Select(line => "' " + line));

		RecognitionResult result = new VBNetRecognitionHarness().RecognizeFully(commentedOut + _NEWLINE + _HIGH_CONSTRUCT + _NEWLINE);

		Assert.True(result.FlowEvidence == FlowEvidence.None, $"{signal} read inside a comment: {result.FlowEvidence}. {unused}");
	}

	/// <summary>A code-only signal quoted inside a string is not evidence; the string-body signals are read there by design.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ACodeOnlySignalInAStringBodyIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		FlowSignal declared = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signal);
		string quoted = "Dim note As String = \"" + source.Replace("\"", "\"\"").Replace(_NEWLINE, " ") + "\"";

		RecognitionResult result = new VBNetRecognitionHarness().RecognizeFully(quoted + _NEWLINE + _HIGH_CONSTRUCT + _NEWLINE);

		if (declared.Zone == FlowSignalZone.CodeOnly)
		{
			Assert.True(result.FlowEvidence == FlowEvidence.None, $"{signal} read inside a string: {result.FlowEvidence}. {unused}");
		}
	}

	/// <summary>Near-misses: each excludes what the catalog says it does not recognize.</summary>
	[Theory]
	[InlineData("Console.Write(\"Press any key to continue\")")]
	[InlineData("Dim name As String = Console.ReadLine()")]
	[InlineData("Dim pressed = Console.ReadKey()")]
	[InlineData("Dim pressed = Console.ReadKey(False)")]
	[InlineData("Dim answer As String = InputBox(\"Enter the quantity\")")]
	[InlineData("Dim typed = Request.QueryString(\"password\")")]
	[InlineData("Dim typed = Session(\"password\")")]
	[InlineData("Dim typed = Request.Headers(\"token\")")]
	[InlineData("Dim stored = configuration(\"Section:Timeout\")")]
	[InlineData("Dim stored = My.Settings.Timeout")]
	[InlineData("Dim stored = Environment.GetEnvironmentVariable(\"PATH\")")]
	[InlineData("Dim stored = ConfigurationManager.AppSettings(\"Timeout\")")]
	[InlineData("<DataType(DataType.EmailAddress)>\nPublic Property Entry As String")]
	[InlineData("Dim markup As String = \"<input type=\"\"text\"\" name=\"\"entry\"\" />\"")]
	[InlineData("passwordField.UseSystemPasswordChar = False")]
	public void ANearMissCarriesNoFlowEvidence(string source)
	{
		RecognitionResult result = new VBNetRecognitionHarness().RecognizeFully(source + _NEWLINE + _HIGH_CONSTRUCT + _NEWLINE);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	/// <summary>
	/// THE LITERAL RULE. A credential written into source is not flow evidence: the engine's literal-credential signal stays a security property.
	/// A file whose only credential is a literal decides the default, and the literal signal is still raised.
	/// </summary>
	[Fact]
	public void ALiteralCredentialIsNotFlowEvidenceAndItsFileDecidesTheDefault()
	{
		RecognitionResult result = new VBNetRecognitionHarness().RecognizeFully(_literalOnlyFileContent);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.Contains(result.Constructs, construct => construct.LiteralCredentialSignal);

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_LITERAL_ONLY_FILE_NAME, _literalOnlyFileContent);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _LITERAL_ONLY_FILE_NAME);

		Assert.Equal(nameof(FlowDecision.Default), finding.FlowDecision);
		Assert.Equal(nameof(CredentialFlow.ClientCredentials), finding.Flow);
	}

	/// <summary>An interactive signal renders Authorization Code, with its note and its token acquisition; no signal renders the default note.</summary>
	[Fact]
	public void AnInteractiveSignalRendersAuthorizationCodeAndNoSignalRendersTheDefaultNote()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_INTERACTIVE_FILE_NAME, _interactiveFileContent);
		harness.WriteSourceFile(_FIXED_FILE_NAME, _fixedFileContent);
		harness.WriteSourceFile(_PLAIN_FILE_NAME, _HIGH_CONSTRUCT + "\n");

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
		Assert.Contains(_CODE_USE_SITE, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_VBNET_TOKEN_ACQUISITION, interactive.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), fixedCredential.Flow);
		Assert.Equal(nameof(FlowDecision.FixedSignal), fixedCredential.FlowDecision);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, fixedCredential.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), plain.Flow);
		Assert.Equal(nameof(FlowDecision.Default), plain.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, plain.FlowNote);
	}

	/// <summary>
	/// THE FIXED-CREDENTIAL-DECIDED NOTE, BESIDE THE NO-SIGNAL DEFAULT NOTE. A finding whose flow was decided by a fixed-credential signal carries the shared
	/// engine's fixed-signal note, which keeps the default note's reach (nothing has measured how often such a signal fires wrongly) and says that a person-facing
	/// integration should use Authorization Code instead; a finding with neither signal carries the default note. Both are Client Credentials, both name the other flow.
	/// </summary>
	[Fact]
	public void AFindingDecidedByAFixedCredentialSignalCarriesTheFixedSignalNoteBesideTheNeitherSignalDefaultNote()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_FIXED_FILE_NAME, _fixedFileContent);
		harness.WriteSourceFile(_PLAIN_FILE_NAME, _HIGH_CONSTRUCT + _NEWLINE);

		harness.RunTheWholeWorkflow();

		ArtifactDocument document = ReadArtifact(harness);
		ArtifactFindingDocument decidedByFixedSignal = FindingOf(document, _FIXED_FILE_NAME);
		ArtifactFindingDocument withNeitherSignal = FindingOf(document, _PLAIN_FILE_NAME);

		Assert.Equal(nameof(FlowDecision.FixedSignal), decidedByFixedSignal.FlowDecision);
		Assert.Equal(FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE, decidedByFixedSignal.FlowNote);
		Assert.Equal(nameof(FlowDecision.Default), withNeitherSignal.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, withNeitherSignal.FlowNote);
		Assert.NotEqual(decidedByFixedSignal.FlowNote, withNeitherSignal.FlowNote);
		Assert.Equal(nameof(CredentialFlow.ClientCredentials), decidedByFixedSignal.Flow);
		Assert.Equal(nameof(CredentialFlow.ClientCredentials), withNeitherSignal.Flow);
		Assert.Contains("Authorization Code", decidedByFixedSignal.FlowNote, StringComparison.Ordinal);
		Assert.Contains("Authorization Code", withNeitherSignal.FlowNote, StringComparison.Ordinal);
	}

	/// <summary>
	/// KNOWN LIMIT. A file that holds both an interactive and a fixed-credential signal is assigned Authorization Code, because the shared rule lets an
	/// interactive signal win. The mismatch with the expected flow is disclosed; this test pins it so that no change moves it unseen.
	/// </summary>
	[Fact]
	public void AFileWithBothKindsOfEvidenceIsAssignedAuthorizationCodeAndThatMismatchIsAnAcceptedLimit()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_BOTH_KINDS_FILE_NAME, _bothKindsFileContent);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _BOTH_KINDS_FILE_NAME);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), finding.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), finding.FlowDecision);
	}

	/// <summary>
	/// THE CONSOLE WARNING THROUGH THE REAL PIPELINE. A literal credential in a VBNet source produces a warning line naming the finding, its pattern class and the
	/// Secret Management pointer, and never the placed secret itself anywhere in the transcript.
	/// </summary>
	[Fact]
	public void ALiteralCredentialFindingProducesAWarningNamingPartBAndNeverPrintsTheValue()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _literalOnlyFileContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Contains(_WARNING_PREFIX, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(finding.Identifier, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains("VB-001", harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_PLACED_SECRET, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The complement: a credential held in a variable produces no warning line at all.</summary>
	[Fact]
	public void AVariableCredentialFindingProducesNoWarning()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _HIGH_CONSTRUCT + "\n");

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Single(harness.State.Findings);
		Assert.DoesNotContain(_WARNING_PREFIX, harness.Transcript, StringComparison.Ordinal);
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
		return VBNetRenderedTexts.Analyzer();
	}
}
