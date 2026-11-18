using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `Python` ANALYZER'S TWELVE FLOW SIGNALS, THROUGH THE REAL EMBEDDED ANALYZER, THE REAL LEXICAL SCANNER AND THE REAL RECOGNITION ENGINE, AND THE FLOW A FINDING RENDERS UNDER THEM THROUGH THE REAL FOUR-STAGE PIPELINE.
///
/// Each signal is read ALONE (an analyzer that declares the one signal and everything else the shipped analyzer declares), so a fixture proves the signal it names and not whichever other signal happened to fire; the
/// whole analyzer is read beside it for the evidence a file takes. Every source is invented from the catalog's own prose, with placeholder names, and carries no fragment, path or third-party name from any repository.
/// </summary>
public sealed class PythonFlowSignalTests
{
	private const string _NEWLINE = "\n";

	/// <summary>A High construct that sends no credential-named value and no literal credential, so the file is evaluated for flow and nothing else fires.</summary>
	private const string _HIGH_CONSTRUCT = "response = requests.get(url, headers={\"Authorization\": \"Basic \" + encoded_pair})";

	private const string _INTERACTIVE_FILE_NAME = "interactive.py";
	private const string _FIXED_FILE_NAME = "fixed.py";
	private const string _PLAIN_FILE_NAME = "plain.py";
	private const string _BOTH_KINDS_FILE_NAME = "both_kinds.py";
	private const string _LITERAL_ONLY_FILE_NAME = "literal_only.py";
	private const string _CREDENTIAL_FILE_NAME = "credential.py";

	private const string _AUTHORIZATION_CODE_GRANT = "\"grant_type\": \"authorization_code\"";
	private const string _CLIENT_CREDENTIALS_GRANT = "\"grant_type\": \"client_credentials\"";
	private const string _BEARER_USE_SITE = "f\"Bearer {access_token}\"";
	private const string _PLACED_SECRET = "a-literal-secret";
	private const string _WARNING_PREFIX = "Warning:";
	private const string _TRIPLE_QUOTE = "'''";
	private const string _COMMAND_LINE_GROUP = "cli";
	private const string _WEB_SERVER_GROUP = "web-server";

	private const int _SIGNAL_COUNT = 12;
	private const int _INTERACTIVE_SIGNAL_COUNT = 8;
	private const int _FIXED_SIGNAL_COUNT = 4;
	private const int _SIGNALS_PER_INTERACTIVE_GROUP = 4;

	/// <summary>The declared signals in order, each with its kind, its zone and the application group of an interactive one (a fixed signal has none).</summary>
	private static readonly ImmutableArray<(string Name, FlowSignalKind Kind, FlowSignalZone Zone, string Group)> _declaredTable =
	[
		("GetpassCall", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, _COMMAND_LINE_GROUP),
		("InputPasswordPrompt", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, _COMMAND_LINE_GROUP),
		("WebBrowserOpenCall", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, _COMMAND_LINE_GROUP),
		("LoopbackRedirectReceiver", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, _COMMAND_LINE_GROUP),
		("RequestFormCredentialRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, _WEB_SERVER_GROUP),
		("FastApiCredentialForm", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, _WEB_SERVER_GROUP),
		("DjangoAuthenticateOrLogin", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, _WEB_SERVER_GROUP),
		("FrameworkSessionUse", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, _WEB_SERVER_GROUP),
		("EnvironmentCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, string.Empty),
		("ConfigFileCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, string.Empty),
		("NetrcCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, string.Empty),
		("CredentialNamedLiteralAssignment", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, string.Empty)
	];

	/// <summary>One or more fixtures per signal: the signal's name, the evidence it gives and a source in which it reads.</summary>
	private static readonly ImmutableArray<(string Signal, FlowEvidence Evidence, string Source)> _signalFixtures =
	[
		("GetpassCall", FlowEvidence.Interactive, "answer = getpass.getpass(\"Enter: \")"),
		("GetpassCall", FlowEvidence.Interactive, "from getpass import getpass\nanswer = getpass()"),
		("InputPasswordPrompt", FlowEvidence.Interactive, "answer = input(\"Enter your password: \")"),
		("InputPasswordPrompt", FlowEvidence.Interactive, "answer = input('Enter the password: ')"),
		("WebBrowserOpenCall", FlowEvidence.Interactive, "webbrowser.open(authorize_address)"),
		("WebBrowserOpenCall", FlowEvidence.Interactive, "webbrowser.open_new_tab(authorize_address)"),
		("LoopbackRedirectReceiver", FlowEvidence.Interactive, "server = http.server.HTTPServer((\"127.0.0.1\", 8123), handler)\nquery = parse_qs(address.query)\nreturned_code = query.get(\"code\", [None])[0]"),
		("RequestFormCredentialRead", FlowEvidence.Interactive, "typed = request.form[\"password\"]"),
		("RequestFormCredentialRead", FlowEvidence.Interactive, "typed = request.POST.get(\"password\")"),
		("FastApiCredentialForm", FlowEvidence.Interactive, "def sign_in(password: str = Form(...)):\n\tpass"),
		("FastApiCredentialForm", FlowEvidence.Interactive, "def sign_in(form: OAuth2PasswordRequestForm = Depends()):\n\tpass"),
		("DjangoAuthenticateOrLogin", FlowEvidence.Interactive, "user = authenticate(request, username=name, password=typed)"),
		("DjangoAuthenticateOrLogin", FlowEvidence.Interactive, "login(request, user)"),
		("FrameworkSessionUse", FlowEvidence.Interactive, "request.session[\"visitor\"] = value"),
		("FrameworkSessionUse", FlowEvidence.Interactive, "from flask import Flask, session"),
		("EnvironmentCredentialRead", FlowEvidence.Fixed, "value = os.environ[\"SERVICE_PASSWORD\"]"),
		("EnvironmentCredentialRead", FlowEvidence.Fixed, "value = os.getenv(\"SERVICE_TOKEN\")"),
		("ConfigFileCredentialRead", FlowEvidence.Fixed, "value = config[\"service\"][\"password\"]"),
		("ConfigFileCredentialRead", FlowEvidence.Fixed, "value = settings.SERVICE_PASSWORD"),
		("NetrcCredentialRead", FlowEvidence.Fixed, "login, account, stored = netrc.netrc().authenticators(host)"),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "service_password = \"a-literal-secret\"")
	];

	public static TheoryData<string, FlowEvidence, string> SignalFixtures()
	{
		TheoryData<string, FlowEvidence, string> data = [];

		foreach ((string signal, FlowEvidence evidence, string source) in _signalFixtures)
		{
			data.Add(signal, evidence, source);
		}

		return data;
	}

	// ------------------------------------------------------------------------------------------
	// The declaration
	// ------------------------------------------------------------------------------------------

	/// <summary>Eight interactive signals and four fixed-credential ones, each with the kind and the zone the catalog declares, and both kinds exist.</summary>
	[Fact]
	public void TheDeclaredSignalsCarryTheCatalogsKindAndZoneEachAndBothKindsExist()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(
			_declaredTable.Select(row => (row.Name, row.Kind, row.Zone)).ToArray(),
			signals.Select(signal => (signal.Name, signal.Kind, signal.Zone)).ToArray());
		Assert.Equal(_SIGNAL_COUNT, signals.Length);
		Assert.Equal(_INTERACTIVE_SIGNAL_COUNT, signals.Count(signal => signal.Kind == FlowSignalKind.InteractiveUser));
		Assert.Equal(_FIXED_SIGNAL_COUNT, signals.Count(signal => signal.Kind == FlowSignalKind.FixedCredential));
	}

	/// <summary>
	/// The application groups: four interactive signals read a command-line or desktop tool and four a server-side web application, and the groups are the catalog's, so every application type has a signal. The group
	/// is not a member of the contract: the declaration holds no member beyond the name, the kind, the zone and the expression.
	/// </summary>
	[Fact]
	public void EveryInteractiveApplicationGroupHasItsFourSignalsAndTheContractCarriesNoEnvironmentMember()
	{
		Assert.Equal(_SIGNALS_PER_INTERACTIVE_GROUP, _declaredTable.Count(row => row.Group == _COMMAND_LINE_GROUP));
		Assert.Equal(_SIGNALS_PER_INTERACTIVE_GROUP, _declaredTable.Count(row => row.Group == _WEB_SERVER_GROUP));
		Assert.All(_declaredTable.Where(row => row.Kind == FlowSignalKind.FixedCredential), row => Assert.Equal(string.Empty, row.Group));

		string[] declaredMembers = [.. typeof(FlowSignal).GetProperties().Select(property => property.Name).Order(StringComparer.Ordinal)];

		Assert.Equal(["Dialects", "ExcludedDialects", "Expression", "Kind", "Name", "VariableUse", "Zone"], declaredMembers);
	}

	[Fact]
	public void EverySignalExpressionDeclaresThePolicyMatchTimeoutAndTheAnalyzerFitsTheContract()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.All(signals, signal => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, signal.Expression.MatchTimeout));
		Assert.Empty(AnalyzerContractValidator.Validate(ReadAnalyzer()));
	}

	[Fact]
	public void EverySignalOfTheTableHasAtLeastOneFixture()
	{
		Assert.Equal(
			_declaredTable.Select(row => row.Name).Order(StringComparer.Ordinal),
			_signalFixtures.Select(fixture => fixture.Signal).Distinct().Order(StringComparer.Ordinal));
	}

	/// <summary>No signal narrows its dialects and none tracks a variable: a `Python` file is read in one dialect and the signals read the text they match.</summary>
	[Fact]
	public void NoSignalDeclaresADialectOrTracksAVariable()
	{
		Assert.All(ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals, signal =>
		{
			Assert.True(signal.Dialects.IsDefaultOrEmpty);
			Assert.True(signal.ExcludedDialects.IsDefaultOrEmpty);
			Assert.Null(signal.VariableUse);
		});
	}

	/// <summary>The construct every fixture is appended to is a High finding that gives no flow evidence of its own.</summary>
	[Fact]
	public void TheHighConstructIsHighAndGivesNoFlowEvidenceOfItsOwn()
	{
		RecognitionResult result = new PythonRecognitionHarness().RecognizeFully($"{_HIGH_CONSTRUCT}{_NEWLINE}");

		Assert.Contains(result.Constructs, construct => construct.Level == ConfidenceLevel.High);
		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	// ------------------------------------------------------------------------------------------
	// Each signal fires in its zone, and not outside it
	// ------------------------------------------------------------------------------------------

	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalGivesItsEvidenceOnItsOwnFixtureWhenItIsReadAlone(string signal, FlowEvidence expected, string source)
	{
		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(source));

		Assert.True(expected == alone, $"{signal}: expected {expected}, got {alone}.");
	}

	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void TheWholeAnalyzerGivesTheFixturesEvidenceToo(string signal, FlowEvidence expected, string source)
	{
		RecognitionResult result = new PythonRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.True(expected == result.FlowEvidence, $"{signal}: expected {expected}, got {result.FlowEvidence}.");
	}

	/// <summary>A signal commented out is not evidence, in either zone kind.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalInACommentIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		string commentedOut = string.Join(_NEWLINE, source.Split(_NEWLINE).Select(line => $"# {line}"));

		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(commentedOut));

		Assert.True(alone == FlowEvidence.None, $"{signal} read inside a comment: {alone}. {unused}");
	}

	/// <summary>A code-only signal quoted inside a triple-quoted string is not evidence. The string-body signals are read there by design, and a fixture that holds the quote itself is not wrapped.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ACodeOnlySignalInAStringBodyIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		FlowSignal declared = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signal);

		if (declared.Zone == FlowSignalZone.CodeOnly && !source.Contains(_TRIPLE_QUOTE, StringComparison.Ordinal))
		{
			string quoted = $"note = {_TRIPLE_QUOTE}{source}{_TRIPLE_QUOTE}";

			FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(quoted));

			Assert.True(alone == FlowEvidence.None, $"{signal} read inside a string: {alone}. {unused}");
		}
	}

	/// <summary>The string-body signals do read their wording inside a string: that is the zone they declare.</summary>
	[Theory]
	[InlineData("InputPasswordPrompt", "answer = input(\"Enter your password: \")")]
	[InlineData("LoopbackRedirectReceiver", "server = http.server.HTTPServer((\"127.0.0.1\", 8123), handler)\nquery = parse_qs(address.query)\nreturned_code = query.get(\"code\", [None])[0]")]
	public void AStringBodySignalReadsItsWordingInsideAString(string signal, string source)
	{
		string quoted = $"note = {_TRIPLE_QUOTE}{source}{_TRIPLE_QUOTE}";

		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(quoted));

		Assert.NotEqual(FlowEvidence.None, alone);
	}

	// ------------------------------------------------------------------------------------------
	// Near-misses
	// ------------------------------------------------------------------------------------------

	/// <summary>THE FLOW NEAR-MISSES OF THE CATALOG: forms that resemble a signal and are kept out of it, each carrying no flow evidence.</summary>
	[Theory]
	[InlineData("user_name = getpass.getuser()")]
	[InlineData("name = input(\"Name: \")")]
	[InlineData("answer = ui.input(\"Enter your password: \")")]
	[InlineData("def input(prompt):\n\treturn prompt\nanswer = input(\"Enter your password: \")")]
	[InlineData("server = http.server.HTTPServer((\"127.0.0.1\", 8123), handler)")]
	[InlineData("server = http.server.HTTPServer((\"0.0.0.0\", 8123), handler)\nquery = parse_qs(address.query)\nreturned_code = query.get(\"code\", [None])[0]")]
	[InlineData("header_value = request.authorization")]
	[InlineData("session = requests.Session()\nsession.headers[\"Accept\"] = \"application/json\"")]
	[InlineData("password_prompt = \"Password: \"")]
	[InlineData("service_password = \"<password>\"")]
	[InlineData("value = os.environ[\"HOME\"]")]
	[InlineData("value = config[\"service\"][\"timeout\"]")]
	public void ANearMissCarriesNoFlowEvidence(string source)
	{
		RecognitionResult result = new PythonRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	/// <summary>The declared misreads that lose a signal and never a finding: the file reads the default.</summary>
	[Theory]
	[InlineData("from os import environ\nvalue = environ[\"SERVICE_PASSWORD\"]")]
	[InlineData("from webbrowser import open\nopen(authorize_address)")]
	[InlineData("server = http.server.HTTPServer((\"\", 8123), handler)\nquery = parse_qs(address.query)\nreturned_code = query.get(\"code\", [None])[0]")]
	[InlineData("def sign_in(request):\n\ttyped = self.request.POST[\"password\"]")]
	public void ADeclaredMisreadLosesTheSignalAndTheFileReadsTheDefault(string source)
	{
		RecognitionResult result = new PythonRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	/// <summary>Evidence is one value per file and an interactive signal wins over a fixed one: the mixed idiom is read as interactive.</summary>
	[Fact]
	public void AnInteractiveSignalWinsOverAFixedSignalInTheSameFile()
	{
		string source = WithTheHighConstruct("value = os.environ[\"SERVICE_PASSWORD\"]\nanswer = getpass.getpass(\"Enter: \")");

		Assert.Equal(FlowEvidence.Interactive, new PythonRecognitionHarness().RecognizeFully(source).FlowEvidence);
	}

	// ------------------------------------------------------------------------------------------
	// The literal rule
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE LITERAL RULE. A credential written into source is not flow evidence: the engine's literal-credential signal stays a security property. A file whose only credential is a literal decides the default, and the
	/// literal signal is still raised.
	/// </summary>
	[Fact]
	public void ALiteralCredentialIsNotFlowEvidenceAndItsFileDecidesTheDefault()
	{
		string source = LiteralOnlyContent();
		RecognitionResult result = new PythonRecognitionHarness().RecognizeFully(source);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.Contains(result.Constructs, construct => construct.LiteralCredentialSignal);

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_LITERAL_ONLY_FILE_NAME, source);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _LITERAL_ONLY_FILE_NAME);

		Assert.Equal(nameof(FlowDecision.Default), finding.FlowDecision);
		Assert.Equal(nameof(CredentialFlow.ClientCredentials), finding.Flow);
	}

	// ------------------------------------------------------------------------------------------
	// The flow a finding renders
	// ------------------------------------------------------------------------------------------

	/// <summary>An interactive signal renders Authorization Code, with its note, its Code use site and its token acquisition; no signal renders Client Credentials with the default note.</summary>
	[Fact]
	public void AnInteractiveSignalRendersAuthorizationCodeAndNoSignalRendersClientCredentialsWithTheDefaultNote()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_INTERACTIVE_FILE_NAME, $"answer = getpass.getpass(\"Enter: \"){_NEWLINE}{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_FIXED_FILE_NAME, $"value = os.environ[\"SERVICE_PASSWORD\"]{_NEWLINE}{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_PLAIN_FILE_NAME, $"{_HIGH_CONSTRUCT}{_NEWLINE}");

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
		Assert.Contains(_BEARER_USE_SITE, interactive.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), fixedCredential.Flow);
		Assert.Equal(nameof(FlowDecision.FixedSignal), fixedCredential.FlowDecision);
		Assert.Equal(FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE, fixedCredential.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, fixedCredential.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), plain.Flow);
		Assert.Equal(nameof(FlowDecision.Default), plain.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, plain.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, plain.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_BEARER_USE_SITE, plain.Recommendation, StringComparison.Ordinal);
	}

	/// <summary>
	/// ACCEPTED LIMIT (the mixed idiom). A file that holds both an interactive and a fixed-credential signal is assigned Authorization Code, because the shared rule lets an interactive signal win. The mismatch with the
	/// expected flow is accepted and disclosed; this test pins it so that no change moves it unseen.
	/// </summary>
	[Fact]
	public void AFileWithBothKindsOfEvidenceIsAssignedAuthorizationCodeAndThatMismatchIsAnAcceptedLimit()
	{
		string source =
			"stored = os.environ.get(\"SERVICE_PASSWORD\")\n"
			+ "if stored is None:\n"
			+ "\tstored = getpass.getpass(\"Enter: \")\n"
			+ $"{_HIGH_CONSTRUCT}\n";

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_BOTH_KINDS_FILE_NAME, source);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _BOTH_KINDS_FILE_NAME);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), finding.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), finding.FlowDecision);
	}

	// ------------------------------------------------------------------------------------------
	// The literal-credential warning
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE CONSOLE WARNING THROUGH THE REAL PIPELINE. A literal credential in a `Python` source produces a warning line naming the finding, its pattern class and the Secret Management pointer (Part B), and never the placed
	/// secret itself anywhere in the transcript.
	/// </summary>
	[Theory]
	[InlineData("auth = HTTPBasicAuth(\"user\", \"a-literal-secret\")\n", "PY-001")]
	[InlineData("response = requests.get(url, auth=(\"user\", \"a-literal-secret\"))\n", "PY-002")]
	public void ALiteralCredentialFindingProducesAWarningNamingPartBAndNeverPrintsTheValue(string source, string patternClass)
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, source);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Contains(_WARNING_PREFIX, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(finding.Identifier, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(patternClass, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_PLACED_SECRET, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The recommendation of a literal-credential `Python` finding carries the security sentence that names Part B, beside the migration text and not inside it.</summary>
	[Fact]
	public void ALiteralCredentialPythonFindingRendersTheSecuritySentenceThatNamesPartB()
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_CREDENTIAL_FILE_NAME,
			1,
			"PY-001",
			ConfidenceLevel.High,
			literalCredentialSignal: true,
			language: AssessedLanguage.Python,
			flow: CredentialFlow.ClientCredentials,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(PythonRenderedTexts.Catalog(), new GenericRecommendationProvider());

		RenderedRecommendation rendered = renderer.Render(finding, MigrateAuth.Cli.Emission.EmissionDestination.StructuredData);

		Assert.Equal(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, rendered.SecurityNote);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, rendered.SecurityNote, StringComparison.Ordinal);
		Assert.DoesNotContain(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, RecommendationComposer.ComposeArtifactText(rendered)!, StringComparison.Ordinal);
	}

	/// <summary>The complement: a credential held in a variable, and a placeholder, produce no warning line at all.</summary>
	[Theory]
	[InlineData("auth = HTTPBasicAuth(user_name, secret_value)\n")]
	[InlineData("auth = HTTPBasicAuth(\"user\", \"<password>\")\n")]
	[InlineData("response = requests.get(url, headers={\"Authorization\": \"Basic \" + encoded_pair})\n")]
	public void AVariableCredentialFindingProducesNoWarning(string source)
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, source);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Assert.Single(harness.State.Findings);
		Assert.DoesNotContain(_WARNING_PREFIX, harness.Transcript, StringComparison.Ordinal);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static string LiteralOnlyContent()
	{
		return $"auth = HTTPBasicAuth(\"user\", \"{_PLACED_SECRET}\"){_NEWLINE}";
	}

	private static string WithTheHighConstruct(string source)
	{
		return $"{source}{_NEWLINE}{_HIGH_CONSTRUCT}{_NEWLINE}";
	}

	/// <summary>The evidence one signal gives alone: the shipped analyzer, narrowed to that signal, on a file the shipped scanner and engine read.</summary>
	private static FlowEvidence EvidenceOfSignalAlone(string signalName, string source)
	{
		PythonRecognitionHarness harness = new();
		FlowSignal signal = harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signalName);
		FileRecognitionContext context = harness.ContextFor(source);
		FileRecognitionContext alone = context with { Analyzer = new SingleSignalPythonAnalyzer(harness.Analyzer, signal) };
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		return engine.Recognize(alone).FlowEvidence;
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
		return PythonRenderedTexts.Analyzer();
	}

	/// <summary>The shipped analyzer with every flow signal but one removed: the same profile, rules and demotion, so one signal can be read on its own.</summary>
	private sealed class SingleSignalPythonAnalyzer : ILanguageAnalyzer
	{
		private readonly ILanguageAnalyzer _inner;

		internal SingleSignalPythonAnalyzer(ILanguageAnalyzer inner, FlowSignal signal)
		{
			_inner = inner;
			CandidateSelectionAndRecommendation = new CandidateSelectionAndRecommendation
			{
				PreselectionAnchors = inner.CandidateSelectionAndRecommendation.PreselectionAnchors,
				TemplateProvider = inner.CandidateSelectionAndRecommendation.TemplateProvider,
				FlowSignals = [signal],
				LanguageFallbackTextByFlow = inner.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow,
				FileDialects = inner.CandidateSelectionAndRecommendation.FileDialects,
				WrapperTracking = inner.CandidateSelectionAndRecommendation.WrapperTracking
			};
		}

		public LexicalProfile LexicalProfile => _inner.LexicalProfile;

		public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

		public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass => _inner.DetectionRulesByPatternClass;

		public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

		public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; }

		public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;

		public ConstructPrecedence? ConstructPrecedence => _inner.ConstructPrecedence;
	}
}
