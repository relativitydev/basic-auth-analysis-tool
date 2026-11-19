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
/// THE `Go` ANALYZER'S ELEVEN FLOW SIGNALS, THROUGH THE REAL EMBEDDED ANALYZER, THE REAL LEXICAL SCANNER AND THE REAL RECOGNITION ENGINE, AND THE FLOW A FINDING
/// RENDERS UNDER THEM THROUGH THE REAL FOUR-STAGE PIPELINE.
///
/// Each signal is read ALONE (an analyzer that declares the one signal and everything else the shipped analyzer declares), so a fixture proves the signal it names and
/// not whichever other signal happened to fire; the whole analyzer is read beside it for the evidence a file takes. Every source is invented from the catalog's own prose,
/// with placeholder names, and carries no fragment, path or third-party name from any repository. The literal-credential warning of the console is read here too, through
/// the real pipeline.
/// </summary>
public sealed class GoFlowSignalTests
{
	private const int _FINDING_SEQUENTIAL_INDEX = 1;
	private const int _FINDING_LINE = 1;
	private const string _LITERAL_CREDENTIAL_PATTERN_CLASS = "GO-001";
	private const string _NEWLINE = "\n";

	/// <summary>A High construct that sends no credential-named assignment and no literal credential, so the file is evaluated for flow and nothing else fires.</summary>
	private const string _HIGH_CONSTRUCT = "request.SetBasicAuth(userName, value)";

	private const string _INTERACTIVE_FILE_NAME = "Interactive.go";
	private const string _FIXED_FILE_NAME = "Fixed.go";
	private const string _PLAIN_FILE_NAME = "Plain.go";
	private const string _BOTH_KINDS_FILE_NAME = "BothKinds.go";
	private const string _LITERAL_ONLY_FILE_NAME = "LiteralOnly.go";
	private const string _CREDENTIAL_FILE_NAME = "Credential.go";

	private const string _AUTHORIZATION_CODE_GRANT = "\"authorization_code\"";
	private const string _CLIENT_CREDENTIALS_GRANT = "\"client_credentials\"";
	private const string _CODE_USE_SITE = "\"Bearer \"+accessToken";
	private const string _GO_TOKEN_ACQUISITION = "func SignInFromCommandLine(";
	private const string _PLACED_SECRET = "a-literal-secret";
	private const string _WARNING_PREFIX = "Warning:";
	private const string _UNDEMOTED_PATH = "src/client.go";

	private const int _SIGNAL_COUNT = 11;
	private const int _INTERACTIVE_SIGNAL_COUNT = 6;
	private const int _FIXED_SIGNAL_COUNT = 5;

	private static readonly ImmutableArray<(string Name, FlowSignalKind Kind, FlowSignalZone Zone)> _declaredTable =
	[
		("TerminalPasswordRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("StdinCredentialPromptRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("FmtScanCredentialVariable", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("LoginFormPasswordRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("HtmlPasswordInputMarkup", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("LoginRouteRegistration", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("EnvironmentCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("ConfigReceiverCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("SecretManagerClientCall", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("CredentialNamedLiteralAssignment", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("SecretFileRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly)
	];

	/// <summary>One or more fixtures per signal that reads: the signal's name, the evidence it gives and a source in which it reads.</summary>
	private static readonly ImmutableArray<(string Signal, FlowEvidence Evidence, string Source)> _signalFixtures =
	[
		("TerminalPasswordRead", FlowEvidence.Interactive, "password, err := term.ReadPassword(int(os.Stdin.Fd()))"),
		("TerminalPasswordRead", FlowEvidence.Interactive, "password, err := terminal.ReadPassword(fd)"),
		("TerminalPasswordRead", FlowEvidence.Interactive, "password, err := ReadPassword(int(syscall.Stdin))"),
		("TerminalPasswordRead", FlowEvidence.Interactive, "password, err := term.ReadPassword(descriptor)"),
		("StdinCredentialPromptRead", FlowEvidence.Interactive, "reader := bufio.NewReader(os.Stdin)\nfmt.Print(\"Password: \")"),
		("StdinCredentialPromptRead", FlowEvidence.Interactive, "fmt.Print(\"Enter your password: \")\nscanner := bufio.NewScanner(os.Stdin)"),
		("StdinCredentialPromptRead", FlowEvidence.Interactive, "fmt.Print(`Passphrase`)\nline, err := reader.ReadString('\\n')\nreader = bufio.NewReader(os.Stdin)"),
		("FmtScanCredentialVariable", FlowEvidence.Interactive, "fmt.Scanln(&password)"),
		("FmtScanCredentialVariable", FlowEvidence.Interactive, "fmt.Scanf(\"%s\", &apiToken)"),
		("FmtScanCredentialVariable", FlowEvidence.Interactive, "fmt.Scan(&userPass)"),
		("FmtScanCredentialVariable", FlowEvidence.Interactive, "fmt.Fscan(os.Stdin, &secret)"),
		("FmtScanCredentialVariable", FlowEvidence.Interactive, "fmt.Fscanln(os.Stdin, &password)"),
		("LoginFormPasswordRead", FlowEvidence.Interactive, "password := request.FormValue(\"password\")"),
		("LoginFormPasswordRead", FlowEvidence.Interactive, "password := request.PostFormValue(\"passwd\")"),
		("LoginFormPasswordRead", FlowEvidence.Interactive, "password := request.PostForm.Get(\"password\")"),
		("LoginFormPasswordRead", FlowEvidence.Interactive, "password := request.Form.Get(\"user_password\")"),
		("LoginFormPasswordRead", FlowEvidence.Interactive, "password := request.Form[\"password\"]"),
		("LoginFormPasswordRead", FlowEvidence.Interactive, "password := request.PostForm[\"password\"]"),
		("HtmlPasswordInputMarkup", FlowEvidence.Interactive, "const page = `<form><input type=\"password\" name=\"pw\"></form>`"),
		("HtmlPasswordInputMarkup", FlowEvidence.Interactive, "page := \"<input name=\\\"pw\\\" type=\\\"password\\\">\""),
		("HtmlPasswordInputMarkup", FlowEvidence.Interactive, "page := `<INPUT TYPE='password'>`"),
		("LoginRouteRegistration", FlowEvidence.Interactive, "http.HandleFunc(\"/login\", loginHandler)"),
		("LoginRouteRegistration", FlowEvidence.Interactive, "mux.Handle(\"POST /login\", handler)"),
		("LoginRouteRegistration", FlowEvidence.Interactive, "mux.HandleFunc(\"/account/sign-in\", handler)"),
		("LoginRouteRegistration", FlowEvidence.Interactive, "mux.HandleFunc(\"/logon/{next}\", handler)"),
		("EnvironmentCredentialRead", FlowEvidence.Fixed, "secret := os.Getenv(\"SERVICE_PASSWORD\")"),
		("EnvironmentCredentialRead", FlowEvidence.Fixed, "value, found := os.LookupEnv(\"DB_PASS\")"),
		("EnvironmentCredentialRead", FlowEvidence.Fixed, "value := os.Getenv(\"dbPass\")"),
		("ConfigReceiverCredentialRead", FlowEvidence.Fixed, "secret := config.ClientSecret"),
		("ConfigReceiverCredentialRead", FlowEvidence.Fixed, "password := cfg.Password"),
		("ConfigReceiverCredentialRead", FlowEvidence.Fixed, "password := config.Get(\"db.password\")"),
		("ConfigReceiverCredentialRead", FlowEvidence.Fixed, "secret := cfg.Account.Secret"),
		("ConfigReceiverCredentialRead", FlowEvidence.Fixed, "creds := svc.Settings[account]"),
		("SecretManagerClientCall", FlowEvidence.Fixed, "value, err := client.GetSecretValue(ctx, input)"),
		("SecretManagerClientCall", FlowEvidence.Fixed, "response, err := client.AccessSecretVersion(ctx, request)"),
		("SecretManagerClientCall", FlowEvidence.Fixed, "value, err := store.GetSecret(name)"),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "const apiPassword = \"exampleSecretValue\""),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "options := Options{Password: \"exampleSecret\"}"),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "clientSecret := `exampleSecret`"),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "var token string = \"exampleTokenValue\""),
		("SecretFileRead", FlowEvidence.Fixed, "data, err := os.ReadFile(\"/run/secrets/database\")"),
		("SecretFileRead", FlowEvidence.Fixed, "data, err := ReadFile(\"secret.txt\")"),
		("SecretFileRead", FlowEvidence.Fixed, "data, err := os.ReadFile(`token.json`)")
	];

	/// <summary>The flow near-misses of the catalog and the near-misses of each signal: the signal's name and a source in which it must not read.</summary>
	private static readonly ImmutableArray<(string Signal, string Source)> _nearMissFixtures =
	[
		("TerminalPasswordRead", "password, err := ReadPassword(path)"),
		("TerminalPasswordRead", "func ReadPassword(fd int) ([]byte, error) {"),
		("TerminalPasswordRead", "func (store *Store) ReadPassword(fd int) ([]byte, error) {"),
		("StdinCredentialPromptRead", "scanner := bufio.NewScanner(os.Stdin)\nfor scanner.Scan() {\n\tlines = append(lines, scanner.Text())\n}"),
		("StdinCredentialPromptRead", "fmt.Print(\"Password: \")\nvalue := os.Args[1]"),
		("StdinCredentialPromptRead", "reader := bufio.NewReader(os.Stdin)\nfmt.Print(\"Name: \")"),
		("FmtScanCredentialVariable", "fmt.Scanln(&count)"),
		("FmtScanCredentialVariable", "fmt.Sscanf(text, \"%d\", &password)"),
		("FmtScanCredentialVariable", "fmt.Fscan(file, &password)"),
		("FmtScanCredentialVariable", "fmt.Scanln(&bypass)"),
		("LoginFormPasswordRead", "name := request.FormValue(\"username\")"),
		("LoginFormPasswordRead", "target := request.FormValue(\"redirect\")"),
		("LoginFormPasswordRead", "password := request.URL.Query().Get(\"password\")"),
		("LoginFormPasswordRead", "password := values.Get(\"password\")"),
		("HtmlPasswordInputMarkup", "page := `<input type=\"text\" name=\"password-hint\">`"),
		("HtmlPasswordInputMarkup", "page := `<input type=\"text\"><label type=\"password\"></label>`"),
		("HtmlPasswordInputMarkup", "page := `<label>password</label>`"),
		("LoginRouteRegistration", "http.HandleFunc(\"/healthz\", health)"),
		("LoginRouteRegistration", "http.HandleFunc(\"/api/login-events\", events)"),
		("LoginRouteRegistration", "http.HandleFunc(\"/logout\", logout)"),
		("EnvironmentCredentialRead", "port := os.Getenv(\"PORT\")"),
		("EnvironmentCredentialRead", "home := os.Getenv(\"HOME\")"),
		("EnvironmentCredentialRead", "path := os.Getenv(\"GOPATH\")"),
		("EnvironmentCredentialRead", "bypass := os.Getenv(\"BYPASS\")"),
		("ConfigReceiverCredentialRead", "name := cfg.Account.Name"),
		("ConfigReceiverCredentialRead", "name := cfg.Region.Zone.Secret"),
		("ConfigReceiverCredentialRead", "port := svc.Settings[account]"),
		("ConfigReceiverCredentialRead", "creds := svc.Settings"),
		("ConfigReceiverCredentialRead", "creds := other[tenant]"),
		("ConfigReceiverCredentialRead", "port := config.Port"),
		("ConfigReceiverCredentialRead", "port := config.Get(\"port\")"),
		("SecretManagerClientCall", "values, err := store.GetSecrets(name)"),
		("SecretManagerClientCall", "func (store *Store) GetSecret(name string) (string, error) {"),
		("CredentialNamedLiteralAssignment", "passwordLabel = \"Password\""),
		("CredentialNamedLiteralAssignment", "tokenEnvName = \"SERVICE_TOKEN\""),
		("CredentialNamedLiteralAssignment", "password := \"\""),
		("CredentialNamedLiteralAssignment", "password := os.Getenv(\"SERVICE_PASSWORD\")"),
		("CredentialNamedLiteralAssignment", "retries := \"three\""),
		("SecretFileRead", "data, err := os.ReadFile(\"config.json\")"),
		("SecretFileRead", "data, err := os.ReadFile(templatePath)"),
		("SecretFileRead", "data, err := os.WriteFile(\"secret.txt\", body, 0o600)")
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

	public static TheoryData<string, string> NearMissFixtures()
	{
		TheoryData<string, string> data = [];

		foreach ((string signal, string source) in _nearMissFixtures)
		{
			data.Add(signal, source);
		}

		return data;
	}

	// ------------------------------------------------------------------------------------------
	// The declaration
	// ------------------------------------------------------------------------------------------

	/// <summary>Six interactive signals and five fixed-credential ones, each with the kind and the zone the catalog declares, and both kinds exist.</summary>
	[Fact]
	public void TheDeclaredSignalsCarryTheCatalogsKindAndZoneEachAndBothKindsExist()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(_SIGNAL_COUNT, signals.Length);
		Assert.Equal(
			_declaredTable.Select(row => (row.Name, row.Kind, row.Zone)).ToArray(),
			signals.Select(signal => (signal.Name, signal.Kind, signal.Zone)).ToArray());
		Assert.Equal(_INTERACTIVE_SIGNAL_COUNT, signals.Count(signal => signal.Kind == FlowSignalKind.InteractiveUser));
		Assert.Equal(_FIXED_SIGNAL_COUNT, signals.Count(signal => signal.Kind == FlowSignalKind.FixedCredential));
	}

	/// <summary>`Go` declares no dialect and no variable tracking, so no signal carries any.</summary>
	[Fact]
	public void NoSignalDeclaresADialectOrAVariableUse()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.All(signals, signal =>
		{
			Assert.Empty(signal.Dialects);
			Assert.Empty(signal.ExcludedDialects);
			Assert.Null(signal.VariableUse);
		});
	}

	/// <summary>Every signal declares the policy match timeout, as the engine's bounded evaluation requires, and an <c>anchor</c> group.</summary>
	[Fact]
	public void EverySignalExpressionDeclaresAMatchTimeoutAndAnAnchorGroup()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.All(signals, signal =>
		{
			Assert.NotEqual(System.Text.RegularExpressions.Regex.InfiniteMatchTimeout, signal.Expression.MatchTimeout);
			Assert.Contains(FlowSignal.AnchorGroupName, signal.Expression.GetGroupNames());
		});
	}

	/// <summary>The shipped table and the fixtures cover every declared signal at least once, in the reading direction and in the refusing one.</summary>
	[Fact]
	public void EveryDeclaredSignalHasAFixtureThatReadsAndOneThatDoesNot()
	{
		Assert.All(_declaredTable, row =>
		{
			Assert.Contains(_signalFixtures, fixture => fixture.Signal == row.Name);
			Assert.Contains(_nearMissFixtures, fixture => fixture.Signal == row.Name);
		});
		Assert.All(_signalFixtures, fixture => Assert.Contains(_declaredTable, row => row.Name == fixture.Signal && FlowEvidenceOf(row.Kind) == fixture.Evidence));
	}

	// ------------------------------------------------------------------------------------------
	// What each signal reads, alone
	// ------------------------------------------------------------------------------------------

	/// <summary>Each signal, alone, gives its own kind of evidence on the source the catalog describes for it.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalAloneGivesItsKindOfEvidenceOnItsOwnSource(string signalName, FlowEvidence expected, string source)
	{
		Assert.Equal(expected, EvidenceOfSignalAlone(signalName, WithTheHighConstruct(source)));
	}

	/// <summary>Each signal, alone, reads nothing on the near-misses the catalog names for it.</summary>
	[Theory]
	[MemberData(nameof(NearMissFixtures))]
	public void ASignalAloneGivesNoEvidenceOnItsNearMiss(string signalName, string source)
	{
		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signalName, WithTheHighConstruct(source)));
	}

	/// <summary>A CodeOnly signal written in a comment or a string body is not evidence; the two signals read in a string body are read there and not in a comment.</summary>
	[Theory]
	[InlineData("// password, err := term.ReadPassword(int(os.Stdin.Fd()))", FlowEvidence.None)]
	[InlineData("/* value := os.Getenv(\"SERVICE_PASSWORD\") */", FlowEvidence.None)]
	[InlineData("note := `os.Getenv(\"SERVICE_PASSWORD\")`", FlowEvidence.None)]
	[InlineData("value := os.Getenv(\"SERVICE_PASSWORD\")", FlowEvidence.Fixed)]
	[InlineData("// page := `<input type=\"password\">`", FlowEvidence.None)]
	[InlineData("page := `<input type=\"password\">`", FlowEvidence.Interactive)]
	[InlineData("// reader := bufio.NewReader(os.Stdin)\nfmt.Print(\"Password: \")", FlowEvidence.None)]
	[InlineData("fmt.Print(\"Password: \")\nreader := bufio.NewReader(os.Stdin)", FlowEvidence.Interactive)]
	public void ASignalIsReadInItsZoneAndNotInAnotherOne(string signalSource, FlowEvidence expected)
	{
		RecognitionResult result = new GoRecognitionHarness().RecognizeFully(WithTheHighConstruct(signalSource));

		Assert.Equal(expected, result.FlowEvidence);
	}

	/// <summary>A server marker alone is not a signal: serving, reading an inbound Basic header or reading the arguments says nothing about whose identity the calls use.</summary>
	[Theory]
	[InlineData("log.Fatal(http.ListenAndServe(address, nil))")]
	[InlineData("mux := http.NewServeMux()")]
	[InlineData("http.HandleFunc(\"/orders\", orders)")]
	[InlineData("user, pass, ok := r.BasicAuth()")]
	[InlineData("target := os.Args[1]")]
	public void AServerMarkerAloneIsNotAFlowSignal(string source)
	{
		RecognitionResult result = new GoRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	/// <summary>The credential words: a whole-token word and a camel-case boundary read, a word inside another word does not.</summary>
	[Theory]
	[InlineData("value := os.Getenv(\"DB_PASS\")", FlowEvidence.Fixed)]
	[InlineData("value := os.Getenv(\"dbPass\")", FlowEvidence.Fixed)]
	[InlineData("value := os.Getenv(\"APP_TOKEN\")", FlowEvidence.Fixed)]
	[InlineData("value := os.Getenv(\"BYPASS\")", FlowEvidence.None)]
	[InlineData("value := os.Getenv(\"COMPASS_POINT\")", FlowEvidence.None)]
	[InlineData("value := os.Getenv(\"dbPwd\")", FlowEvidence.None)]
	public void TheCredentialWordsMatchAsTheCatalogStatesThem(string source, FlowEvidence expected)
	{
		RecognitionResult result = new GoRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(expected, result.FlowEvidence);
	}

	// ------------------------------------------------------------------------------------------
	// The base rule and the literal rule
	// ------------------------------------------------------------------------------------------

	/// <summary>A file that holds both kinds of evidence is read as interactive: the base rule lets an interactive signal win.</summary>
	[Fact]
	public void InteractiveWinsWhenAFileHoldsBothKindsOfEvidence()
	{
		RecognitionResult result = new GoRecognitionHarness().RecognizeFully(WithTheHighConstruct(MixedIdiom()));

		Assert.Equal(FlowEvidence.Interactive, result.FlowEvidence);
	}

	/// <summary>
	/// THE LITERAL RULE. A credential written into source is not flow evidence: the engine's literal-credential signal stays a security property. A file whose only credential is
	/// a literal decides the default, and the literal signal is still raised.
	/// </summary>
	[Fact]
	public void ALiteralCredentialIsNotFlowEvidenceAndItsFileDecidesTheDefault()
	{
		string source = LiteralOnlyContent();
		RecognitionResult result = new GoRecognitionHarness().RecognizeFully(source);

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
		harness.WriteSourceFile(_INTERACTIVE_FILE_NAME, $"password := r.FormValue(\"password\")\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_FIXED_FILE_NAME, $"secret := os.Getenv(\"SERVICE_PASSWORD\")\n{_HIGH_CONSTRUCT}{_NEWLINE}");
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
		Assert.Contains(_CODE_USE_SITE, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_GO_TOKEN_ACQUISITION, interactive.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), fixedCredential.Flow);
		Assert.Equal(nameof(FlowDecision.FixedSignal), fixedCredential.FlowDecision);
		Assert.Equal(FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE, fixedCredential.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, fixedCredential.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), plain.Flow);
		Assert.Equal(nameof(FlowDecision.Default), plain.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, plain.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, plain.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_CODE_USE_SITE, plain.Recommendation, StringComparison.Ordinal);
	}

	/// <summary>
	/// ACCEPTED LIMIT (the mixed idiom). A file that holds both an interactive and a fixed-credential signal is assigned Authorization Code, because the shared rule lets an
	/// interactive signal win. The mismatch with the expected flow is accepted and disclosed; this test pins it so that no change moves it unseen.
	/// </summary>
	[Fact]
	public void AFileWithBothKindsOfEvidenceIsAssignedAuthorizationCodeAndThatMismatchIsAnAcceptedLimit()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_BOTH_KINDS_FILE_NAME, WithTheHighConstruct(MixedIdiom()));

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _BOTH_KINDS_FILE_NAME);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), finding.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), finding.FlowDecision);
	}

	// ------------------------------------------------------------------------------------------
	// The literal-credential warning
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE CONSOLE WARNING THROUGH THE REAL PIPELINE. A literal credential in a `Go` source produces a warning line naming the finding, its pattern class and the Secret
	/// Management pointer (Part B), and never the placed secret itself anywhere in the transcript.
	/// </summary>
	[Fact]
	public void ALiteralCredentialFindingProducesAWarningNamingPartBAndNeverPrintsTheValue()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, LiteralOnlyContent());

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Contains(_WARNING_PREFIX, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(finding.Identifier, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(_LITERAL_CREDENTIAL_PATTERN_CLASS, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_PLACED_SECRET, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The recommendation of a literal-credential `Go` finding carries the security sentence that names Part B, beside the migration text and not inside it.</summary>
	[Fact]
	public void ALiteralCredentialGoFindingRendersTheSecuritySentenceThatNamesPartB()
	{
		Finding finding = ReportHarness.FindingAt(
			_FINDING_SEQUENTIAL_INDEX,
			_CREDENTIAL_FILE_NAME,
			_FINDING_LINE,
			_LITERAL_CREDENTIAL_PATTERN_CLASS,
			ConfidenceLevel.High,
			literalCredentialSignal: true,
			language: AssessedLanguage.Go,
			flow: CredentialFlow.ClientCredentials,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(GoRenderedTexts.Catalog(), new GenericRecommendationProvider());

		RenderedRecommendation rendered = renderer.Render(finding, MigrateAuth.Cli.Emission.EmissionDestination.StructuredData);

		Assert.Equal(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, rendered.SecurityNote);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, rendered.SecurityNote, StringComparison.Ordinal);
		Assert.DoesNotContain(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, RecommendationComposer.ComposeArtifactText(rendered)!, StringComparison.Ordinal);
	}

	/// <summary>The complement: a credential held in a variable produces no warning line at all.</summary>
	[Fact]
	public void AVariableCredentialFindingProducesNoWarning()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, $"{_HIGH_CONSTRUCT}{_NEWLINE}");

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
		return $"request.SetBasicAuth(userName, \"{_PLACED_SECRET}\"){_NEWLINE}";
	}

	/// <summary>The file that reads a credential from the environment and, when it is unset, asks for one at the terminal.</summary>
	private static string MixedIdiom()
	{
		return "value := os.Getenv(\"SERVICE_PASSWORD\")\nif value == \"\" {\n\tfmt.Scanln(&password)\n}";
	}

	private static FlowEvidence FlowEvidenceOf(FlowSignalKind kind)
	{
		return kind == FlowSignalKind.InteractiveUser ? FlowEvidence.Interactive : FlowEvidence.Fixed;
	}

	private static string WithTheHighConstruct(string source)
	{
		return $"{source}{_NEWLINE}{_HIGH_CONSTRUCT}{_NEWLINE}";
	}

	/// <summary>The evidence one signal gives alone: the shipped analyzer, narrowed to that signal, on a file the shipped scanner and engine read.</summary>
	private static FlowEvidence EvidenceOfSignalAlone(string signalName, string source, string relativePath = _UNDEMOTED_PATH)
	{
		GoRecognitionHarness harness = new();
		FlowSignal signal = harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signalName);
		FileRecognitionContext context = harness.ContextFor(source, relativePath);
		FileRecognitionContext alone = context with { Analyzer = new SingleSignalGoAnalyzer(harness.Analyzer, signal) };
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
		return GoRenderedTexts.Analyzer();
	}

	/// <summary>The shipped analyzer with every flow signal but one removed: the same profile, rules and demotion, so one signal can be read on its own.</summary>
	private sealed class SingleSignalGoAnalyzer : ILanguageAnalyzer
	{
		private readonly ILanguageAnalyzer _inner;

		internal SingleSignalGoAnalyzer(ILanguageAnalyzer inner, FlowSignal signal)
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
