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
/// THE `JavaScript` ANALYZER'S SIXTEEN FLOW SIGNALS, THROUGH THE REAL EMBEDDED ANALYZER, THE REAL LEXICAL SCANNER AND THE REAL RECOGNITION ENGINE, AND THE FLOW A
/// FINDING RENDERS UNDER THEM THROUGH THE REAL FOUR-STAGE PIPELINE.
///
/// Each signal is read ALONE (an analyzer that declares the one signal and everything else the shipped analyzer declares), so a fixture proves the signal it
/// names and not whichever other signal happened to fire; the whole analyzer is read beside it for the evidence a file takes. Every source is invented from the
/// catalog's own prose, with placeholder names, and carries no fragment, path or third-party name from any repository.
/// </summary>
public sealed class JavaScriptFlowSignalTests
{
	private const string _NEWLINE = "\n";

	/// <summary>A High construct that sends no credential-named literal and carries no flow evidence of its own, so the file is evaluated for flow and nothing else fires.</summary>
	private const string _HIGH_CONSTRUCT = "await fetch(url, { headers: { Authorization: 'Basic ' + encoded } });";

	private const string _INTERACTIVE_FILE_NAME = "Interactive.js";
	private const string _FIXED_FILE_NAME = "Fixed.js";
	private const string _PLAIN_FILE_NAME = "Plain.js";
	private const string _BOTH_KINDS_FILE_NAME = "BothKinds.js";
	private const string _LITERAL_ONLY_FILE_NAME = "LiteralOnly.js";
	private const string _CREDENTIAL_FILE_NAME = "Credential.js";

	private const string _AUTHORIZATION_CODE_GRANT = "grant_type=authorization_code";
	private const string _CLIENT_CREDENTIALS_GRANT = "grant_type=client_credentials";
	private const string _CODE_USE_SITE = "Bearer ${accessToken}";
	private const string _CODE_BROWSER_VARIANT = "**Variant 1 — Browser with a server-side component**";
	private const string _CODE_COMMAND_LINE_VARIANT = "**Variant 3 — Node.js command line or desktop**";
	private const string _CLIENT_CREDENTIALS_PROCESS_VARIANT = "**Variant: a Node.js process acting as itself**";
	private const string _PLACED_SECRET = "exampleSecretValue";
	private const string _WARNING_PREFIX = "Warning:";

	private const string _NODE_DIALECT = "node";
	private const string _SCRIPT_DIALECT = "script";
	private const string _NODE_SHEBANG = "#!/usr/bin/env node\n";
	private const string _NODEJS_SHEBANG = "#!/usr/bin/nodejs\n";
	private const string _OTHER_SHEBANG = "#!/usr/bin/env deno\n";
	private const string _PLAIN_PATH = "src/client.js";
	private const string _COMMONJS_PATH = "src/client.cjs";
	private const string _MODULE_PATH = "src/client.mjs";
	private const string _JSX_PATH = "src/client.jsx";
	private const string _UPPER_CASE_COMMONJS_PATH = "src/client.CJS";

	private const int _SIGNAL_COUNT = 16;
	private const int _INTERACTIVE_SIGNAL_COUNT = 12;
	private const int _FIXED_SIGNAL_COUNT = 4;
	private const int _SIGNALS_THAT_EXCLUDE_THE_NODE_DIALECT = 7;

	/// <summary>The pinned declaration: every flow signal with the kind and the zone the catalog states, in the order the analyzer declares them.</summary>
	private static readonly ImmutableArray<(string Name, FlowSignalKind Kind, FlowSignalZone Zone)> _declaredTable =
	[
		("DomPasswordFieldRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("FormPasswordPropertyRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("BrowserPromptCall", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("BrowserStorageRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("XhrFiveArgumentOpen", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("HtmlPasswordInputMarkup", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("RequestBodyPasswordForwarded", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("NodeMutedPasswordPrompt", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("PromptLibraryPasswordType", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("PromptWording", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		("ReadlineInterfaceQuestion", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("MutedWritableOutput", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("ProcessEnvCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("ConfigLibraryCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("SecretManagerClientCall", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("CredentialNamedLiteralAssignment", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly)
	];

	/// <summary>The signals whose evidence is the browser's, or markup or prompt wording that a server file may merely hold: they are refused in a file that declares itself <c>node</c>.</summary>
	private static readonly ImmutableArray<string> _signalsThatExcludeTheNodeDialect =
	[
		"BrowserPromptCall", "BrowserStorageRead", "DomPasswordFieldRead", "FormPasswordPropertyRead", "HtmlPasswordInputMarkup", "PromptWording", "XhrFiveArgumentOpen"
	];

	/// <summary>One or more fixtures per signal: the signal's name, the evidence it gives and a source in which it reads.</summary>
	private static readonly ImmutableArray<(string Signal, FlowEvidence Evidence, string Source)> _signalFixtures =
	[
		("DomPasswordFieldRead", FlowEvidence.Interactive, "const typed = document.getElementById('password').value;"),
		("DomPasswordFieldRead", FlowEvidence.Interactive, "const typed = document.querySelector('input[type=password]').value;"),
		("FormPasswordPropertyRead", FlowEvidence.Interactive, "const typed = form.elements.password.value;"),
		("FormPasswordPropertyRead", FlowEvidence.Interactive, "const typed = new FormData(form).get('password');"),
		("FormPasswordPropertyRead", FlowEvidence.Interactive, "const typed = form.elements.namedItem('password').value;"),
		("BrowserPromptCall", FlowEvidence.Interactive, "const typed = window.prompt('Enter the value');"),
		("BrowserPromptCall", FlowEvidence.Interactive, "const typed = prompt('Enter your password');"),
		("BrowserStorageRead", FlowEvidence.Interactive, "const stored = localStorage.getItem('password');"),
		("XhrFiveArgumentOpen", FlowEvidence.Interactive, "request.open('GET', url, true, 'exampleUser', 'exampleSecret');"),
		("HtmlPasswordInputMarkup", FlowEvidence.Interactive, "const markup = '<input type=\"password\" name=\"entry\">';"),
		("HtmlPasswordInputMarkup", FlowEvidence.Interactive, "const markup = `<input type=\"password\" name=\"entry\">`;"),
		("RequestBodyPasswordForwarded", FlowEvidence.Interactive, "const typed = req.body.password;"),
		("RequestBodyPasswordForwarded", FlowEvidence.Interactive, "const { userName, password } = req.body;"),
		("RequestBodyPasswordForwarded", FlowEvidence.Interactive, "const params = new URLSearchParams(body);\nconst typed = params.get('password');"),
		("NodeMutedPasswordPrompt", FlowEvidence.Interactive, "readlineInterface._writeToOutput = function () {};"),
		("NodeMutedPasswordPrompt", FlowEvidence.Interactive, "const passwordBuffer = [];\nprocess.stdin.setRawMode(true);"),
		("PromptLibraryPasswordType", FlowEvidence.Interactive, "const answers = await ask({ type: 'password', name: 'entry' });"),
		("PromptWording", FlowEvidence.Interactive, "const answer = await readlineInterface.question('Enter your password: ');"),
		("PromptWording", FlowEvidence.Interactive, "const answer = prompt('Enter the passphrase');"),
		("ReadlineInterfaceQuestion", FlowEvidence.Interactive, "const reader = readline.createInterface({ input, output });\nconst answer = await reader.question('Name: ');"),
		("MutedWritableOutput", FlowEvidence.Interactive, "const sink = new Writable({ write(chunk, encoding, callback) { if (!muted) { output.write(chunk, encoding); } callback(); } });"),
		("ProcessEnvCredentialRead", FlowEvidence.Fixed, "const entry = process.env.API_PASSWORD;"),
		("ProcessEnvCredentialRead", FlowEvidence.Fixed, "const entry = process.env['API_TOKEN'];"),
		("ConfigLibraryCredentialRead", FlowEvidence.Fixed, "const entry = config.get('apiPassword');"),
		("ConfigLibraryCredentialRead", FlowEvidence.Fixed, "const entry = settings.apiSecret;"),
		("SecretManagerClientCall", FlowEvidence.Fixed, "const entry = await client.getSecretValue({ SecretId: name });"),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "const apiPassword = 'exampleValue';"),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "const options = { password: 'exampleValue' };"),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "const apiSecret = `exampleValue`;")
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

	/// <summary>The pinned constant: twelve interactive signals and four fixed-credential ones, each with the kind and the zone the catalog declares, and both kinds exist.</summary>
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

	/// <summary>
	/// The dialects of the declaration: the browser-document signals and the two that read markup and prompt wording exclude <c>node</c>, and no signal uses an include
	/// list, because an include list would silence the Node.js prompt idioms in a plain file. No signal tracks a variable.
	/// </summary>
	[Fact]
	public void TheDialectDeclarationsAreTheOnesTheCatalogStates()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(
			_signalsThatExcludeTheNodeDialect,
			signals.Where(signal => !signal.ExcludedDialects.IsDefaultOrEmpty).Select(signal => signal.Name).Order(StringComparer.Ordinal));
		Assert.Equal(_SIGNALS_THAT_EXCLUDE_THE_NODE_DIALECT, signals.Count(signal => signal.ExcludedDialects.Contains(_NODE_DIALECT)));
		Assert.All(signals, signal => Assert.True(signal.Dialects.IsDefaultOrEmpty));
		Assert.All(signals, signal => Assert.Null(signal.VariableUse));
	}

	/// <summary>The dialect of a file: <c>script</c> by default, <c>node</c> for a Node.js shebang and for <c>.cjs</c>, and nothing for <c>.jsx</c> or <c>.mjs</c>.</summary>
	[Fact]
	public void TheFileDialectsAreScriptByDefaultAndNodeForTheShebangAndTheCommonJsExtensionOnly()
	{
		FileDialects dialects = ReadAnalyzer().CandidateSelectionAndRecommendation.FileDialects!;

		Assert.Equal(_SCRIPT_DIALECT, dialects.DefaultDialect);
		Assert.Equal(["node", "nodejs"], dialects.ShebangInterpreters.Keys.Order(StringComparer.Ordinal));
		Assert.All(dialects.ShebangInterpreters.Values, dialect => Assert.Equal(_NODE_DIALECT, dialect));
		Assert.Equal([".cjs"], dialects.ExtensionDialects.Keys.ToArray());
		Assert.Equal(_NODE_DIALECT, dialects.ExtensionDialects[".cjs"]);
		Assert.DoesNotContain(".jsx", dialects.ExtensionDialects.Keys);
		Assert.DoesNotContain(".mjs", dialects.ExtensionDialects.Keys);
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
		RecognitionResult result = new JavaScriptRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.True(expected == result.FlowEvidence, $"{signal}: expected {expected}, got {result.FlowEvidence}.");
	}

	/// <summary>A signal commented out is not evidence, in either zone kind.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalInACommentIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		string commentedOut = string.Join(_NEWLINE, source.Split(_NEWLINE).Select(line => $"// {line}"));

		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(commentedOut));

		Assert.True(alone == FlowEvidence.None, $"{signal} read inside a comment: {alone}. {unused}");
	}

	/// <summary>A signal commented out in a block comment is not evidence either.</summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ASignalInABlockCommentIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct($"/* {source} */"));

		Assert.True(alone == FlowEvidence.None, $"{signal} read inside a block comment: {alone}. {unused}");
	}

	/// <summary>
	/// A code-only signal quoted inside a template body is not evidence. The two string-body signals are read there by design, and a fixture that holds a backtick of its
	/// own is not wrapped.
	/// </summary>
	[Theory]
	[MemberData(nameof(SignalFixtures))]
	public void ACodeOnlySignalInAStringBodyIsNotEvidence(string signal, FlowEvidence unused, string source)
	{
		FlowSignal declared = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signal);

		if (declared.Zone == FlowSignalZone.CodeOnly && !source.Contains('`'))
		{
			string quoted = $"const note = `{source}`;";

			FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(quoted));

			Assert.True(alone == FlowEvidence.None, $"{signal} read inside a string: {alone}. {unused}");
		}
	}

	/// <summary>The string-body signals do read the markup and the wording inside a string: that is the zone they declare.</summary>
	[Theory]
	[InlineData("HtmlPasswordInputMarkup", "const markup = '<input type=\"password\">';")]
	[InlineData("HtmlPasswordInputMarkup", "const markup = `<form><input type=password></form>`;")]
	[InlineData("PromptWording", "const answer = await rl.question('Enter your password: ');")]
	[InlineData("PromptWording", "const answer = prompt(\"Enter the passphrase\");")]
	public void AStringBodySignalReadsItsWordingInsideAString(string signal, string source)
	{
		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.Interactive, alone);
	}

	// ------------------------------------------------------------------------------------------
	// The dialects of a file
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The browser-document signals and the two string signals give no evidence in a file that declares itself <c>node</c> (a Node.js shebang or <c>.cjs</c>), and give it
	/// in a plain <c>.js</c> file, in <c>.mjs</c> and in <c>.jsx</c>, which have no dialect of their own.
	/// </summary>
	[Theory]
	[InlineData("DomPasswordFieldRead", "const typed = document.getElementById('password').value;")]
	[InlineData("FormPasswordPropertyRead", "const typed = form.elements.password.value;")]
	[InlineData("BrowserPromptCall", "const typed = window.prompt('Enter the value');")]
	[InlineData("BrowserStorageRead", "const stored = localStorage.getItem('password');")]
	[InlineData("XhrFiveArgumentOpen", "request.open('GET', url, true, 'exampleUser', 'exampleSecret');")]
	[InlineData("HtmlPasswordInputMarkup", "const markup = '<input type=\"password\">';")]
	[InlineData("PromptWording", "const answer = await rl.question('Enter your password: ');")]
	public void AnExcludedSignalIsEvidenceExceptInAFileThatDeclaresItselfNode(string signal, string source)
	{
		string body = WithTheHighConstruct(source);

		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, _NODE_SHEBANG + body, _PLAIN_PATH));
		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, _NODEJS_SHEBANG + body, _PLAIN_PATH));
		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, body, _COMMONJS_PATH));
		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, body, _UPPER_CASE_COMMONJS_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, body, _PLAIN_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, body, _MODULE_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, body, _JSX_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, _OTHER_SHEBANG + body, _PLAIN_PATH));
	}

	/// <summary>The signals that carry no exclusion read in a Node.js file as in any other: a terminal prompt, a request body and the fixed-credential reads.</summary>
	[Theory]
	[InlineData("RequestBodyPasswordForwarded", "const typed = req.body.password;", FlowEvidence.Interactive)]
	[InlineData("NodeMutedPasswordPrompt", "readlineInterface._writeToOutput = function () {};", FlowEvidence.Interactive)]
	[InlineData("PromptLibraryPasswordType", "const answers = await ask({ type: 'password', name: 'entry' });", FlowEvidence.Interactive)]
	[InlineData("ReadlineInterfaceQuestion", "const reader = readline.createInterface({ input, output });\nconst answer = await reader.question('Name: ');", FlowEvidence.Interactive)]
	[InlineData("ProcessEnvCredentialRead", "const entry = process.env.API_PASSWORD;", FlowEvidence.Fixed)]
	[InlineData("CredentialNamedLiteralAssignment", "const apiPassword = 'exampleValue';", FlowEvidence.Fixed)]
	public void ASignalWithNoExclusionIsEvidenceInAFileThatDeclaresItselfNodeToo(string signal, string source, FlowEvidence expected)
	{
		string body = WithTheHighConstruct(source);

		Assert.Equal(expected, EvidenceOfSignalAlone(signal, _NODE_SHEBANG + body, _PLAIN_PATH));
		Assert.Equal(expected, EvidenceOfSignalAlone(signal, body, _COMMONJS_PATH));
		Assert.Equal(expected, EvidenceOfSignalAlone(signal, body, _PLAIN_PATH));
	}

	// ------------------------------------------------------------------------------------------
	// Near-misses
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE FLOW NEAR-MISSES OF THE CATALOG, read alone: a Fetch mode that names a credential option, a variable that holds a record named <c>document</c>, a <c>prompt</c> defined in
	/// the file, a label that carries a credential word, a validation message, a request-body member that is not a credential, a query read, a storage read of a non-credential
	/// key, a configuration read of a port and an environment read of the mode.
	/// </summary>
	[Theory]
	[InlineData("CredentialNamedLiteralAssignment", "await fetch(url, { credentials: 'include' });")]
	[InlineData("CredentialNamedLiteralAssignment", "const credentials = \"same-origin\";")]
	[InlineData("CredentialNamedLiteralAssignment", "const passwordLabel = 'Password';")]
	[InlineData("CredentialNamedLiteralAssignment", "const fieldMessage = 'Password';")]
	[InlineData("CredentialNamedLiteralAssignment", "const bypass = 'enabled';")]
	[InlineData("CredentialNamedLiteralAssignment", "const apiPassword = process.env.ENTRY;")]
	[InlineData("DomPasswordFieldRead", "const document = await store.read(id);\nconst typed = document.password;")]
	[InlineData("FormPasswordPropertyRead", "const document = await store.read(id);\nconst typed = document.password;")]
	[InlineData("FormPasswordPropertyRead", "const typed = form.elements.namedItem('password');")]
	[InlineData("BrowserPromptCall", "function prompt(message) {\n\treturn message;\n}\nconst typed = prompt('Enter your password');")]
	[InlineData("BrowserPromptCall", "const typed = ui.prompt('Enter your password');")]
	[InlineData("BrowserStorageRead", "const stored = localStorage.getItem('theme');")]
	[InlineData("BrowserStorageRead", "const stored = sessionStorage.getItem('sessionToken');")]
	[InlineData("XhrFiveArgumentOpen", "request.open('GET', url, true);")]
	[InlineData("XhrFiveArgumentOpen", "window.open('https://host.example', '_blank', 'noopener', 'a', 'b');")]
	[InlineData("HtmlPasswordInputMarkup", "const markup = '<input type=\"text\" name=\"entry\">';")]
	[InlineData("RequestBodyPasswordForwarded", "const typed = req.body.userName;")]
	[InlineData("RequestBodyPasswordForwarded", "const typed = req.query.token;")]
	[InlineData("RequestBodyPasswordForwarded", "const params = new URLSearchParams(body);\nconst typed = params.get('userName');")]
	[InlineData("RequestBodyPasswordForwarded", "const params = new URLSearchParams(location.search);\nconst typed = params.get('password');")]
	[InlineData("NodeMutedPasswordPrompt", "process.stdin.setRawMode(true);")]
	[InlineData("PromptLibraryPasswordType", "input.type = 'text';")]
	[InlineData("PromptLibraryPasswordType", "const answers = await ask({ type: 'text', name: 'entry' });")]
	[InlineData("PromptWording", "throw new Error('Password must have eight characters');")]
	[InlineData("PromptWording", "response.status(400).send('Password too short');")]
	[InlineData("ReadlineInterfaceQuestion", "const answer = await helper.question('Name: ');")]
	[InlineData("ReadlineInterfaceQuestion", "const reader = readline.createInterface({ input, output });")]
	[InlineData("MutedWritableOutput", "const sink = new Writable({ write(chunk, encoding, callback) { output.write(chunk, encoding); callback(); } });")]
	[InlineData("MutedWritableOutput", "const sink = new Writable({ write(chunk, encoding, callback) { callback(); } });\nconst mutable = true;")]
	[InlineData("ProcessEnvCredentialRead", "const entry = process.env.NODE_ENV;")]
	[InlineData("ProcessEnvCredentialRead", "const entry = process.env.PORT;")]
	[InlineData("ConfigLibraryCredentialRead", "const entry = config.get('port');")]
	[InlineData("SecretManagerClientCall", "const entry = await client.getEntry({ name });")]
	public void ANearMissCarriesNoEvidenceFromTheSignalItResembles(string signal, string source)
	{
		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(source));

		Assert.True(alone == FlowEvidence.None, $"{signal} read a near-miss: {alone}.");
	}

	/// <summary>A server file that renders a page, in a plain file, reads as interactive: the declared cost of a dialect that separates only a file that declares itself.</summary>
	[Fact]
	public void AServerFileThatRendersMarkupReadsAsInteractiveWhenItDoesNotDeclareItselfNodeAndAsNothingWhenItDoes()
	{
		string page = WithTheHighConstruct("const page = '<form><input type=\"password\" name=\"entry\"></form>';");

		Assert.Equal(FlowEvidence.Interactive, new JavaScriptRecognitionHarness().RecognizeFully(page, _PLAIN_PATH).FlowEvidence);
		Assert.Equal(FlowEvidence.None, new JavaScriptRecognitionHarness().RecognizeFully(_NODE_SHEBANG + page, _PLAIN_PATH).FlowEvidence);
		Assert.Equal(FlowEvidence.None, new JavaScriptRecognitionHarness().RecognizeFully(page, _COMMONJS_PATH).FlowEvidence);
	}

	/// <summary>Server markers alone are not evidence: a Node.js login handler acts for a user, but nothing in the markers says so.</summary>
	[Theory]
	[InlineData("const http = require('http');\nmodule.exports = { start };")]
	[InlineData("import http from 'node:http';\nconst root = __dirname;")]
	public void ServerMarkersAloneAreNotFlowEvidence(string source)
	{
		RecognitionResult result = new JavaScriptRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	/// <summary>The interactive signal of a file wins over the fixed one, and a fixed signal alone decides Client Credentials.</summary>
	[Theory]
	[InlineData("const typed = window.prompt('Enter the value');\nconst entry = process.env.API_PASSWORD;", FlowEvidence.Interactive)]
	[InlineData("const entry = process.env.API_PASSWORD;", FlowEvidence.Fixed)]
	public void AnInteractiveSignalWinsOverAFixedOneInTheSameFile(string source, FlowEvidence expected)
	{
		RecognitionResult result = new JavaScriptRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(expected, result.FlowEvidence);
	}

	/// <summary>
	/// THE LITERAL RULE. A credential written into source is not flow evidence: the engine's literal-credential signal stays a security property. A file whose
	/// only credential is a literal, with no credential-named assignment, decides the default, and the literal signal is still raised.
	/// </summary>
	[Fact]
	public void ALiteralCredentialIsNotFlowEvidenceAndItsFileDecidesTheDefault()
	{
		string source = LiteralOnlyContent();
		RecognitionResult result = new JavaScriptRecognitionHarness().RecognizeFully(source);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.Contains(result.Constructs, construct => construct.LiteralCredentialSignal);

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_LITERAL_ONLY_FILE_NAME, source);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _LITERAL_ONLY_FILE_NAME, "JS-005");

		Assert.Equal(nameof(FlowDecision.Default), finding.FlowDecision);
		Assert.Equal(nameof(CredentialFlow.ClientCredentials), finding.Flow);
	}

	// ------------------------------------------------------------------------------------------
	// The flow a finding renders
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// An interactive signal renders Authorization Code, with its note, its Code use site and the three labelled variants; a fixed signal renders Client Credentials with its
	/// note; no signal renders Client Credentials with the default note.
	/// </summary>
	[Fact]
	public void AnInteractiveSignalRendersAuthorizationCodeAndNoSignalRendersClientCredentialsWithTheDefaultNote()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_INTERACTIVE_FILE_NAME, $"const typed = window.prompt('Enter your value');\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_FIXED_FILE_NAME, $"const entry = process.env.API_PASSWORD;\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_PLAIN_FILE_NAME, $"{_HIGH_CONSTRUCT}{_NEWLINE}");

		harness.RunTheWholeWorkflow();

		ArtifactDocument document = ReadArtifact(harness);
		ArtifactFindingDocument interactive = FindingOf(document, _INTERACTIVE_FILE_NAME, "JS-004");
		ArtifactFindingDocument fixedCredential = FindingOf(document, _FIXED_FILE_NAME, "JS-004");
		ArtifactFindingDocument plain = FindingOf(document, _PLAIN_FILE_NAME, "JS-004");

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), interactive.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), interactive.FlowDecision);
		Assert.Equal(FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE, interactive.FlowNote);
		Assert.Contains(_AUTHORIZATION_CODE_GRANT, interactive.Recommendation, StringComparison.Ordinal);
		Assert.DoesNotContain(_CLIENT_CREDENTIALS_GRANT, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_CODE_USE_SITE, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_CODE_BROWSER_VARIANT, interactive.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_CODE_COMMAND_LINE_VARIANT, interactive.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), fixedCredential.Flow);
		Assert.Equal(nameof(FlowDecision.FixedSignal), fixedCredential.FlowDecision);
		Assert.Equal(FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE, fixedCredential.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, fixedCredential.Recommendation, StringComparison.Ordinal);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), plain.Flow);
		Assert.Equal(nameof(FlowDecision.Default), plain.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, plain.FlowNote);
		Assert.Contains(_CLIENT_CREDENTIALS_GRANT, plain.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_CODE_USE_SITE, plain.Recommendation, StringComparison.Ordinal);
		Assert.Contains(_CLIENT_CREDENTIALS_PROCESS_VARIANT, plain.Recommendation, StringComparison.Ordinal);
	}

	/// <summary>
	/// KNOWN LIMIT (the mixed idiom). A file that holds both an interactive and a fixed-credential signal is assigned Authorization Code, because the shared
	/// rule lets an interactive signal win. This test pins it so that no change moves it unseen.
	/// </summary>
	[Fact]
	public void AFileWithBothKindsOfEvidenceIsAssignedAuthorizationCodeAndThatMismatchIsAnAcceptedLimit()
	{
		string source =
			"let entry = process.env.API_PASSWORD;\n"
			+ "if (entry === undefined) {\n"
			+ "\tentry = await readlineInterface.question('Enter your password: ');\n"
			+ "}\n"
			+ _HIGH_CONSTRUCT + _NEWLINE;

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_BOTH_KINDS_FILE_NAME, source);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _BOTH_KINDS_FILE_NAME, "JS-004");

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), finding.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), finding.FlowDecision);
	}

	// ------------------------------------------------------------------------------------------
	// The literal-credential warning
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// THE CONSOLE WARNING THROUGH THE REAL PIPELINE. A literal credential in a `JavaScript` source produces a warning line naming the finding, its pattern class and
	/// the Secret Management pointer (Part B), and never the placed secret itself anywhere in the transcript.
	/// </summary>
	[Fact]
	public void ALiteralCredentialFindingProducesAWarningNamingPartBAndNeverPrintsTheValue()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, $"await axios.get(url, {{ auth: {{ username: userName, password: '{_PLACED_SECRET}' }} }});{_NEWLINE}");

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);

		Assert.Contains(_WARNING_PREFIX, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(finding.Identifier, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains("JS-001", harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(SecretManagementPointer.PART_B_NAME, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_PLACED_SECRET, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The recommendation of a literal-credential `JavaScript` finding carries the security sentence that names Part B, beside the migration text and not inside it.</summary>
	[Fact]
	public void ALiteralCredentialJavaScriptFindingRendersTheSecuritySentenceThatNamesPartB()
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_CREDENTIAL_FILE_NAME,
			1,
			"JS-001",
			ConfidenceLevel.High,
			literalCredentialSignal: true,
			language: AssessedLanguage.JavaScript,
			flow: CredentialFlow.ClientCredentials,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(JavaScriptRenderedTexts.Catalog(), new GenericRecommendationProvider());

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
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, $"await axios.get(url, {{ auth: {{ username: userName, password: secret }} }});{_NEWLINE}");

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
		return $"const header = 'Basic ' + btoa('exampleUser:{_PLACED_SECRET}');\n";
	}

	private static string WithTheHighConstruct(string source)
	{
		return $"{source}{_NEWLINE}{_HIGH_CONSTRUCT}{_NEWLINE}";
	}

	/// <summary>The evidence one signal gives alone: the shipped analyzer, narrowed to that signal, on a file the shipped scanner and engine read.</summary>
	private static FlowEvidence EvidenceOfSignalAlone(string signalName, string source, string relativePath = JavaScriptRecognitionHarness.UNDEMOTED_RELATIVE_PATH)
	{
		JavaScriptRecognitionHarness harness = new();
		FlowSignal signal = harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signalName);
		FileRecognitionContext context = harness.ContextFor(source, relativePath);
		FileRecognitionContext alone = context with { Analyzer = new SingleSignalJavaScriptAnalyzer(harness.Analyzer, signal) };
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		return engine.Recognize(alone).FlowEvidence;
	}

	private static ArtifactDocument ReadArtifact(PipelineHarness harness)
	{
		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		return JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument)!;
	}

	private static ArtifactFindingDocument FindingOf(ArtifactDocument document, string fileName, string patternClass)
	{
		return document.Findings.Single(finding => finding.FilePath == fileName && finding.PatternClass == patternClass);
	}

	private static ILanguageAnalyzer ReadAnalyzer()
	{
		return JavaScriptRegisteredExpressions.Analyzer;
	}

	/// <summary>The shipped analyzer with every flow signal but one removed: the same profile, rules, demotion and dialects, so one signal can be read on its own.</summary>
	private sealed class SingleSignalJavaScriptAnalyzer : ILanguageAnalyzer
	{
		private readonly ILanguageAnalyzer _inner;

		internal SingleSignalJavaScriptAnalyzer(ILanguageAnalyzer inner, FlowSignal signal)
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
