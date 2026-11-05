using System.Collections.Frozen;
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
/// THE `TypeScript` ANALYZER'S TWENTY FLOW SIGNALS, THROUGH THE REAL EMBEDDED ANALYZER, THE REAL LEXICAL SCANNER AND THE REAL RECOGNITION ENGINE, AND THE FLOW A FINDING
/// RENDERS UNDER THEM THROUGH THE REAL FOUR-STAGE PIPELINE.
///
/// The sixteen signals of `JavaScript` are read on `TypeScript` text as they stand, and four signals exist for `TypeScript` alone. Each signal is read ALONE (an analyzer that
/// declares the one signal and everything else the shipped analyzer declares), so a fixture proves the signal it names and not whichever other signal happened to fire; the
/// whole analyzer is read beside it for the evidence a file takes. Every source is invented from the catalog's own prose, with placeholder names, and carries no fragment, path
/// or third-party name from any repository.
/// </summary>
public sealed class TypeScriptFlowSignalTests
{
	private const string _NEWLINE = "\n";

	/// <summary>A High construct that sends no credential-named literal and carries no flow evidence of its own, so the file is evaluated for flow and nothing else fires.</summary>
	private const string _HIGH_CONSTRUCT = "await fetch(url, { headers: { Authorization: 'Basic ' + encoded } });";

	private const string _ANGULAR_FILE_NAME = "Component.ts";
	private const string _NEST_FILE_NAME = "Controller.ts";
	private const string _NEST_CONFIGURATION_FILE_NAME = "Settings.ts";
	private const string _TYPED_LITERAL_FILE_NAME = "TypedLiteral.ts";
	private const string _PLAIN_FILE_NAME = "Plain.ts";
	private const string _BOTH_KINDS_FILE_NAME = "BothKinds.ts";
	private const string _LITERAL_ONLY_FILE_NAME = "LiteralOnly.ts";

	private const string _NODE_DIALECT = "node";
	private const string _SCRIPT_DIALECT = "script";
	private const string _NODEJS_INTERPRETER = "nodejs";
	private const string _TS_NODE_INTERPRETER = "ts-node";
	private const string _TSX_INTERPRETER = "tsx";
	private const string _HEADER_PATTERN_CLASS = "JS-004";
	private const string _LITERAL_PATTERN_CLASS = "JS-005";
	private const string _NODE_SHEBANG = "#!/usr/bin/env node\n";
	private const string _NODEJS_SHEBANG = "#!/usr/bin/nodejs\n";
	private const string _TS_NODE_SHEBANG = "#!/usr/bin/env ts-node\n";
	private const string _TSX_SHEBANG = "#!/usr/bin/env tsx\n";
	private const string _OTHER_SHEBANG = "#!/usr/bin/env deno\n";
	private const string _PLAIN_PATH = "src/client.ts";
	private const string _COMMONJS_PATH = "src/client.cts";
	private const string _UPPER_CASE_COMMONJS_PATH = "src/client.CTS";
	private const string _COMMONJS_DECLARATION_PATH = "types/client.d.cts";
	private const string _MODULE_PATH = "src/client.mts";
	private const string _JSX_PATH = "src/client.tsx";

	private const string _AUTHORIZATION_CODE_GRANT = "grant_type=authorization_code";
	private const string _CLIENT_CREDENTIALS_GRANT = "grant_type=client_credentials";

	private const int _SIGNAL_COUNT = 20;
	private const int _INTERACTIVE_SIGNAL_COUNT = 14;
	private const int _FIXED_SIGNAL_COUNT = 6;
	private const int _INHERITED_SIGNAL_COUNT = 16;
	private const int _SIGNALS_THAT_EXCLUDE_THE_NODE_DIALECT = 8;

	/// <summary>The pinned declaration: every flow signal with the kind and the zone the catalog states, in the order the analyzer declares them: the sixteen inherited, then the four of `TypeScript`.</summary>
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
		("CredentialNamedLiteralAssignment", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("AngularFormPasswordRead", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("NestBodyPasswordForwarded", FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		("NestConfigServiceCredentialRead", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		("TypedCredentialNamedLiteralAssignment", FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly)
	];

	/// <summary>The signals whose evidence is the browser's, or markup or prompt wording that a server file may merely hold: they are refused in a file that declares itself <c>node</c>.</summary>
	private static readonly ImmutableArray<string> _signalsThatExcludeTheNodeDialect =
	[
		"AngularFormPasswordRead", "BrowserPromptCall", "BrowserStorageRead", "DomPasswordFieldRead", "FormPasswordPropertyRead", "HtmlPasswordInputMarkup", "PromptWording", "XhrFiveArgumentOpen"
	];

	/// <summary>One or more fixtures per signal: the signal's name, the evidence it gives and a source in which it reads.</summary>
	private static readonly ImmutableArray<(string Signal, FlowEvidence Evidence, string Source)> _signalFixtures =
	[
		("DomPasswordFieldRead", FlowEvidence.Interactive, "const typed = document.getElementById('password').value;"),
		("DomPasswordFieldRead", FlowEvidence.Interactive, "const typed = (document.querySelector('input[type=password]') as HTMLInputElement).value;"),
		("FormPasswordPropertyRead", FlowEvidence.Interactive, "const typed = form.elements.password.value;"),
		("FormPasswordPropertyRead", FlowEvidence.Interactive, "const typed = new FormData(form).get('password');"),
		("BrowserPromptCall", FlowEvidence.Interactive, "const typed = window.prompt('Enter the value');"),
		("BrowserStorageRead", FlowEvidence.Interactive, "const stored = localStorage.getItem('password');"),
		("XhrFiveArgumentOpen", FlowEvidence.Interactive, "request.open('GET', url, true, 'exampleUser', 'exampleSecret');"),
		("HtmlPasswordInputMarkup", FlowEvidence.Interactive, "const markup = `<input type=\"password\" name=\"entry\">`;"),
		("RequestBodyPasswordForwarded", FlowEvidence.Interactive, "const typed = req.body.password;"),
		("RequestBodyPasswordForwarded", FlowEvidence.Interactive, "const { userName, password } = req.body;"),
		("NodeMutedPasswordPrompt", FlowEvidence.Interactive, "readlineInterface._writeToOutput = function () {};"),
		("PromptLibraryPasswordType", FlowEvidence.Interactive, "const answers = await ask({ type: 'password', name: 'entry' });"),
		("PromptWording", FlowEvidence.Interactive, "const answer = await readlineInterface.question('Enter your password: ');"),
		("ReadlineInterfaceQuestion", FlowEvidence.Interactive, "const reader = readline.createInterface({ input, output });\nconst answer = await reader.question('Name: ');"),
		("MutedWritableOutput", FlowEvidence.Interactive, "const sink = new Writable({ write(chunk, encoding, callback) { if (!muted) { output.write(chunk, encoding); } callback(); } });"),
		("ProcessEnvCredentialRead", FlowEvidence.Fixed, "const entry = process.env.API_PASSWORD;"),
		("ConfigLibraryCredentialRead", FlowEvidence.Fixed, "const entry = config.get('apiPassword');"),
		("SecretManagerClientCall", FlowEvidence.Fixed, "const entry = await client.getSecretValue({ SecretId: name });"),
		("CredentialNamedLiteralAssignment", FlowEvidence.Fixed, "const apiPassword = 'exampleValue';"),
		("AngularFormPasswordRead", FlowEvidence.Interactive, "const typed = this.form.value.password;"),
		("AngularFormPasswordRead", FlowEvidence.Interactive, "const typed = this.loginForm.get('password')?.value;"),
		("AngularFormPasswordRead", FlowEvidence.Interactive, "const typed = this.form.get(\"password\")!.value;"),
		("AngularFormPasswordRead", FlowEvidence.Interactive, "const typed = this.form.controls['password'].value;"),
		("AngularFormPasswordRead", FlowEvidence.Interactive, "const typed = this.form.controls.password.value;"),
		("AngularFormPasswordRead", FlowEvidence.Interactive, "const typed = this.form.getRawValue().password;"),
		("AngularFormPasswordRead", FlowEvidence.Interactive, "const typed = this.form.getRawValue()['password'];"),
		("AngularFormPasswordRead", FlowEvidence.Interactive, "const raw = this.form.getRawValue();\nconst typed = raw.password;"),
		("NestBodyPasswordForwarded", FlowEvidence.Interactive, "async login(@Body('password') secret: string) { return secret; }"),
		("NestBodyPasswordForwarded", FlowEvidence.Interactive, "async login(@Body() { username, password }: LoginDto) { return password; }"),
		("NestBodyPasswordForwarded", FlowEvidence.Interactive, "async login(@Body(\"userPassword\") entered: string) { return entered; }"),
		("NestBodyPasswordForwarded", FlowEvidence.Interactive, "async login(@Body() dto: LoginDto) { return forward(dto.password); }"),
		("NestConfigServiceCredentialRead", FlowEvidence.Fixed, "const dbPassword = this.configService.get<string>('DB_PASSWORD');"),
		("NestConfigServiceCredentialRead", FlowEvidence.Fixed, "const key = this.configService.getOrThrow('API_SECRET');"),
		("NestConfigServiceCredentialRead", FlowEvidence.Fixed, "const entry = configService.get('db.password');"),
		("TypedCredentialNamedLiteralAssignment", FlowEvidence.Fixed, "private readonly apiPassword: string = 'exampleValue';"),
		("TypedCredentialNamedLiteralAssignment", FlowEvidence.Fixed, "const dbToken: string | undefined = \"abc\";"),
		("TypedCredentialNamedLiteralAssignment", FlowEvidence.Fixed, "static secret: Secret = `abc`;")
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

	/// <summary>The pinned constant: fourteen interactive signals and six fixed-credential ones, each with the kind and the zone the catalog declares, the inherited sixteen first, and both kinds exist.</summary>
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

	/// <summary>The first sixteen signals are the `JavaScript` declaration, as it stands: the same names, kinds, zones, exclusions and expression text.</summary>
	[Fact]
	public void TheSixteenInheritedSignalsAreTheJavaScriptDeclarationAsItStands()
	{
		ImmutableArray<FlowSignal> inherited = JavaScriptRegisteredExpressions.Analyzer.CandidateSelectionAndRecommendation.FlowSignals;
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(_INHERITED_SIGNAL_COUNT, inherited.Length);

		for (int index = 0; index < inherited.Length; index++)
		{
			Assert.Equal(inherited[index].Name, signals[index].Name);
			Assert.Equal(inherited[index].Kind, signals[index].Kind);
			Assert.Equal(inherited[index].Zone, signals[index].Zone);
			Assert.Equal(inherited[index].ExcludedDialects, signals[index].ExcludedDialects);
			Assert.Equal(inherited[index].Expression.ToString(), signals[index].Expression.ToString());
		}
	}

	[Fact]
	public void EverySignalExpressionDeclaresThePolicyMatchTimeoutAndTheAnalyzerFitsTheContract()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.All(signals, signal => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, signal.Expression.MatchTimeout));
		Assert.Empty(AnalyzerContractValidator.Validate(ReadAnalyzer()));
	}

	/// <summary>Signal names are unique within the analyzer, so the contract's duplicate-name check has nothing to report.</summary>
	[Fact]
	public void SignalNamesAreUniqueWithinTheAnalyzer()
	{
		ImmutableArray<FlowSignal> signals = ReadAnalyzer().CandidateSelectionAndRecommendation.FlowSignals;

		Assert.Equal(signals.Length, signals.Select(signal => signal.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.DoesNotContain(
			AnalyzerContractValidator.Validate(ReadAnalyzer()),
			violation => violation.Code == AnalyzerContractViolationCode.FlowSignalNameDuplicated);
	}

	[Fact]
	public void EverySignalOfTheTableHasAtLeastOneFixture()
	{
		Assert.Equal(
			_declaredTable.Select(row => row.Name).Order(StringComparer.Ordinal),
			_signalFixtures.Select(fixture => fixture.Signal).Distinct().Order(StringComparer.Ordinal));
	}

	/// <summary>
	/// The dialects of the declaration: the browser-document signals, the two that read markup and prompt wording and the Angular form read exclude <c>node</c>, and no signal uses an
	/// include list, because an include list would silence the Node.js prompt idioms in a plain file. No signal tracks a variable.
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

	/// <summary>
	/// The dialect of a file: <c>script</c> by default, <c>node</c> for a Node.js shebang and for <c>.cts</c> (a <c>.d.cts</c> file by its final extension), <c>script</c> for a
	/// <c>ts-node</c> or <c>tsx</c> shebang, which is declared, and nothing for <c>.tsx</c> or <c>.mts</c>. The names are the `JavaScript` ones.
	/// </summary>
	[Fact]
	public void TheFileDialectsAreScriptByDefaultAndNodeForTheShebangAndTheCommonJsExtensionOnly()
	{
		FileDialects dialects = ReadAnalyzer().CandidateSelectionAndRecommendation.FileDialects!;

		Assert.Equal(_SCRIPT_DIALECT, dialects.DefaultDialect);
		Assert.Equal([_NODE_DIALECT, _NODEJS_INTERPRETER, _TS_NODE_INTERPRETER, _TSX_INTERPRETER], dialects.ShebangInterpreters.Keys.Order(StringComparer.Ordinal));
		Assert.Equal(_NODE_DIALECT, dialects.ShebangInterpreters[_NODE_DIALECT]);
		Assert.Equal(_NODE_DIALECT, dialects.ShebangInterpreters[_NODEJS_INTERPRETER]);
		Assert.Equal(_SCRIPT_DIALECT, dialects.ShebangInterpreters[_TS_NODE_INTERPRETER]);
		Assert.Equal(_SCRIPT_DIALECT, dialects.ShebangInterpreters[_TSX_INTERPRETER]);
		Assert.Equal([".cts"], dialects.ExtensionDialects.Keys.ToArray());
		Assert.Equal(_NODE_DIALECT, dialects.ExtensionDialects[".cts"]);
		Assert.DoesNotContain(".tsx", dialects.ExtensionDialects.Keys);
		Assert.DoesNotContain(".mts", dialects.ExtensionDialects.Keys);
		Assert.Equal(
			JavaScriptRegisteredExpressions.Analyzer.CandidateSelectionAndRecommendation.FileDialects!.DefaultDialect,
			dialects.DefaultDialect);
		Assert.Equal(
			JavaScriptRegisteredExpressions.Analyzer.CandidateSelectionAndRecommendation.FileDialects!.ExtensionDialects[".cjs"],
			dialects.ExtensionDialects[".cts"]);
	}

	/// <summary>The dialect each kind of file resolves to, read through the engine's own resolution.</summary>
	[Theory]
	[InlineData("", "src/client.ts", "script")]
	[InlineData("", "src/client.cts", "node")]
	[InlineData("", "src/client.CTS", "node")]
	[InlineData("", "types/client.d.cts", "node")]
	[InlineData("", "src/client.mts", "script")]
	[InlineData("", "src/client.tsx", "script")]
	[InlineData("", "types/client.d.ts", "script")]
	[InlineData("", "types/client.d.mts", "script")]
	[InlineData("#!/usr/bin/env node\n", "src/client.ts", "node")]
	[InlineData("#!/usr/bin/nodejs\n", "src/client.ts", "node")]
	[InlineData("#!/usr/bin/env ts-node\n", "src/client.ts", "script")]
	[InlineData("#!/usr/bin/env ts-node\n", "src/client.cts", "script")]
	[InlineData("#!/usr/bin/env tsx\n", "src/client.cts", "script")]
	[InlineData("#!/usr/bin/env deno\n", "src/client.cts", "node")]
	public void AFileResolvesToTheDialectTheCatalogStates(string firstLine, string path, string expected)
	{
		FileDialects dialects = ReadAnalyzer().CandidateSelectionAndRecommendation.FileDialects!;

		Assert.Equal(expected, FileDialectResolution.Resolve(dialects, firstLine + "const value = 1;\n", path));
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
		RecognitionResult result = new TypeScriptRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

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
	/// A code-only signal quoted inside a template body is not evidence. The two string-body signals are read there by design, and a fixture that holds a backtick of its own is
	/// not wrapped.
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

	/// <summary>The string-body signals do read the markup and the wording inside a string: that is the zone they declare. An Angular inline template is a string body.</summary>
	[Theory]
	[InlineData("HtmlPasswordInputMarkup", "const markup = '<input type=\"password\">';")]
	[InlineData("HtmlPasswordInputMarkup", "@Component({ selector: 'app-login', template: `<form><input type=password></form>` })")]
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
	/// The browser-document signals, the two string signals and the Angular form read give no evidence in a file that declares itself <c>node</c> (a Node.js shebang or <c>.cts</c>, a
	/// <c>.d.cts</c> file included), and give it in a plain <c>.ts</c> file, in <c>.mts</c> and in <c>.tsx</c>, which have no dialect of their own, and in a file that opens with a runner shebang.
	/// </summary>
	[Theory]
	[InlineData("DomPasswordFieldRead", "const typed = document.getElementById('password').value;")]
	[InlineData("FormPasswordPropertyRead", "const typed = form.elements.password.value;")]
	[InlineData("BrowserPromptCall", "const typed = window.prompt('Enter the value');")]
	[InlineData("BrowserStorageRead", "const stored = localStorage.getItem('password');")]
	[InlineData("XhrFiveArgumentOpen", "request.open('GET', url, true, 'exampleUser', 'exampleSecret');")]
	[InlineData("HtmlPasswordInputMarkup", "const markup = '<input type=\"password\">';")]
	[InlineData("PromptWording", "const answer = await rl.question('Enter your password: ');")]
	[InlineData("AngularFormPasswordRead", "const typed = this.form.value.password;")]
	public void AnExcludedSignalIsEvidenceExceptInAFileThatDeclaresItselfNode(string signal, string source)
	{
		string body = WithTheHighConstruct(source);

		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, _NODE_SHEBANG + body, _PLAIN_PATH));
		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, _NODEJS_SHEBANG + body, _PLAIN_PATH));
		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, body, _COMMONJS_PATH));
		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, body, _UPPER_CASE_COMMONJS_PATH));
		Assert.Equal(FlowEvidence.None, EvidenceOfSignalAlone(signal, body, _COMMONJS_DECLARATION_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, body, _PLAIN_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, body, _MODULE_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, body, _JSX_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, _OTHER_SHEBANG + body, _PLAIN_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, _TS_NODE_SHEBANG + body, _COMMONJS_PATH));
		Assert.Equal(FlowEvidence.Interactive, EvidenceOfSignalAlone(signal, _TSX_SHEBANG + body, _COMMONJS_PATH));
	}

	/// <summary>The signals that carry no exclusion read in a Node.js file as in any other: a terminal prompt, a request body, a body decorator, a configuration service and the fixed-credential reads.</summary>
	[Theory]
	[InlineData("RequestBodyPasswordForwarded", "const typed = req.body.password;", FlowEvidence.Interactive)]
	[InlineData("NestBodyPasswordForwarded", "async login(@Body('password') secret: string) { return secret; }", FlowEvidence.Interactive)]
	[InlineData("NodeMutedPasswordPrompt", "readlineInterface._writeToOutput = function () {};", FlowEvidence.Interactive)]
	[InlineData("PromptLibraryPasswordType", "const answers = await ask({ type: 'password', name: 'entry' });", FlowEvidence.Interactive)]
	[InlineData("ReadlineInterfaceQuestion", "const reader = readline.createInterface({ input, output });\nconst answer = await reader.question('Name: ');", FlowEvidence.Interactive)]
	[InlineData("ProcessEnvCredentialRead", "const entry = process.env.API_PASSWORD;", FlowEvidence.Fixed)]
	[InlineData("CredentialNamedLiteralAssignment", "const apiPassword = 'exampleValue';", FlowEvidence.Fixed)]
	[InlineData("NestConfigServiceCredentialRead", "const dbPassword = this.configService.get<string>('DB_PASSWORD');", FlowEvidence.Fixed)]
	[InlineData("TypedCredentialNamedLiteralAssignment", "private readonly apiPassword: string = 'exampleValue';", FlowEvidence.Fixed)]
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
	/// THE FLOW NEAR-MISSES OF THE CATALOG, read alone: the five inherited families (a Fetch mode that names a credential option, a record named <c>document</c>, a <c>prompt</c> defined in
	/// the file, a label that carries a credential word, a validation message), a request-body member that is not a credential, a storage read of a non-credential key, a configuration
	/// read of a port and an environment read of the mode; and the families `TypeScript` adds: a form definition with no read, a DTO class that only declares the member, a type member
	/// with no value, a configuration read of a key that names no credential and a receiver that is not a configuration service.
	/// </summary>
	[Theory]
	[InlineData("CredentialNamedLiteralAssignment", "await fetch(url, { credentials: 'include' });")]
	[InlineData("CredentialNamedLiteralAssignment", "const credentials = \"same-origin\";")]
	[InlineData("CredentialNamedLiteralAssignment", "const passwordLabel = 'Password';")]
	[InlineData("CredentialNamedLiteralAssignment", "const apiPassword = process.env.ENTRY;")]
	[InlineData("DomPasswordFieldRead", "const document = await store.read(id);\nconst typed = document.password;")]
	[InlineData("FormPasswordPropertyRead", "const typed = form.elements.namedItem('password');")]
	[InlineData("BrowserPromptCall", "function prompt(message: string) {\n\treturn message;\n}\nconst typed = prompt('Enter your password');")]
	[InlineData("BrowserStorageRead", "const stored = localStorage.getItem('theme');")]
	[InlineData("XhrFiveArgumentOpen", "request.open('GET', url, true);")]
	[InlineData("HtmlPasswordInputMarkup", "const markup = '<input type=\"text\" name=\"entry\">';")]
	[InlineData("RequestBodyPasswordForwarded", "const typed = req.body.userName;")]
	[InlineData("NodeMutedPasswordPrompt", "process.stdin.setRawMode(true);")]
	[InlineData("PromptLibraryPasswordType", "const answers = await ask({ type: 'text', name: 'entry' });")]
	[InlineData("PromptWording", "throw new Error('Password must have eight characters');")]
	[InlineData("ReadlineInterfaceQuestion", "const answer = await helper.question('Name: ');")]
	[InlineData("MutedWritableOutput", "const sink = new Writable({ write(chunk, encoding, callback) { output.write(chunk, encoding); callback(); } });")]
	[InlineData("ProcessEnvCredentialRead", "const entry = process.env.NODE_ENV;")]
	[InlineData("ConfigLibraryCredentialRead", "const entry = config.get('port');")]
	[InlineData("SecretManagerClientCall", "const entry = await client.getEntry({ name });")]
	[InlineData("AngularFormPasswordRead", "this.form.controls.password.setValue(value);")]
	[InlineData("AngularFormPasswordRead", "const name = this.form.get('username')?.value;")]
	[InlineData("AngularFormPasswordRead", "const typed = formData.get('password');")]
	[InlineData("AngularFormPasswordRead", "this.form.get('password')?.setValidators([Validators.required]);")]
	[InlineData("AngularFormPasswordRead", "this.form = new FormGroup({ password: new FormControl('') });")]
	[InlineData("AngularFormPasswordRead", "const typed = this.settings.value.password;")]
	[InlineData("AngularFormPasswordRead", "const typed = this.settings.get('password')?.value;")]
	[InlineData("AngularFormPasswordRead", "const raw = this.settings.getRawValue();\nconst typed = raw.password;")]
	[InlineData("AngularFormPasswordRead", "const raw = this.form.getRawValue();\nconst name = raw.username;")]
	[InlineData("AngularFormPasswordRead", "const typed = this.form.getRawValue().username;")]
	[InlineData("AngularFormPasswordRead", "const typed = user.password;")]
	[InlineData("NestBodyPasswordForwarded", "async login(@Body() dto: LoginDto) { return dto; }")]
	[InlineData("NestBodyPasswordForwarded", "async login(@Body('name') name: string) { return name; }")]
	[InlineData("NestBodyPasswordForwarded", "async login(@Query('password') entered: string) { return entered; }")]
	[InlineData("NestBodyPasswordForwarded", "async login(@Body() { name, title }: Dto) { return name; }")]
	[InlineData("NestBodyPasswordForwarded", "class LoginDto {\n\tusername!: string;\n\tpassword!: string;\n}")]
	[InlineData("NestConfigServiceCredentialRead", "const port = this.configService.get<number>('PORT');")]
	[InlineData("NestConfigServiceCredentialRead", "const entry = this.settingsService.get('DB_PASSWORD');")]
	[InlineData("NestConfigServiceCredentialRead", "this.configService.set('DB_PASSWORD', value);")]
	[InlineData("TypedCredentialNamedLiteralAssignment", "const apiPassword = 'exampleValue';")]
	[InlineData("TypedCredentialNamedLiteralAssignment", "const apiPasswordLabel: string = 'Password';")]
	[InlineData("TypedCredentialNamedLiteralAssignment", "private apiPassword: string;")]
	[InlineData("TypedCredentialNamedLiteralAssignment", "interface Login {\n\tpassword: string;\n}")]
	[InlineData("TypedCredentialNamedLiteralAssignment", "type Credentials = { password: string };")]
	[InlineData("TypedCredentialNamedLiteralAssignment", "const mode: string = 'basic';")]
	[InlineData("TypedCredentialNamedLiteralAssignment", "credentials: RequestCredentials = 'include';")]
	[InlineData("TypedCredentialNamedLiteralAssignment", "const apiPassword: string = '';")]
	public void ANearMissCarriesNoEvidenceFromTheSignalItResembles(string signal, string source)
	{
		FlowEvidence alone = EvidenceOfSignalAlone(signal, WithTheHighConstruct(source));

		Assert.True(alone == FlowEvidence.None, $"{signal} read a near-miss: {alone}.");
	}

	/// <summary>
	/// KNOWN COST. A `.ts` server file that renders a page is in the <c>script</c> dialect, so the <c>node</c> exclusion cannot silence it: it reads as interactive, and as nothing when the file
	/// declares itself <c>node</c> by a shebang or by <c>.cts</c>.
	/// </summary>
	[Fact]
	public void AServerFileThatRendersMarkupReadsAsInteractiveWhenItDoesNotDeclareItselfNodeAndAsNothingWhenItDoes()
	{
		string page = WithTheHighConstruct("const page = '<form><input type=\"password\" name=\"entry\"></form>';");

		Assert.Equal(FlowEvidence.Interactive, new TypeScriptRecognitionHarness().RecognizeFully(page, _PLAIN_PATH).FlowEvidence);
		Assert.Equal(FlowEvidence.None, new TypeScriptRecognitionHarness().RecognizeFully(_NODE_SHEBANG + page, _PLAIN_PATH).FlowEvidence);
		Assert.Equal(FlowEvidence.None, new TypeScriptRecognitionHarness().RecognizeFully(page, _COMMONJS_PATH).FlowEvidence);
	}

	/// <summary>Framework markers alone are not evidence: an Angular service and a NestJS controller each act for a person or for themselves, and nothing in the markers says which.</summary>
	[Theory]
	[InlineData("@Component({ selector: 'app-login' })\nexport class LoginComponent {}")]
	[InlineData("@Injectable({ providedIn: 'root' })\nexport class DocumentService {}")]
	[InlineData("@Controller('auth')\nexport class AuthController {}")]
	[InlineData("@Module({ controllers: [AuthController] })\nexport class AuthModule {}")]
	[InlineData("import http from 'node:http';\nconst root = __dirname;")]
	public void FrameworkMarkersAloneAreNotFlowEvidence(string source)
	{
		RecognitionResult result = new TypeScriptRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
	}

	/// <summary>The interactive signal of a file wins over the fixed one, and a fixed signal alone decides Client Credentials.</summary>
	[Theory]
	[InlineData("const typed = this.form.value.password;\nconst entry = process.env.API_PASSWORD;", FlowEvidence.Interactive)]
	[InlineData("async login(@Body('password') secret: string) { return secret; }\nconst entry = this.configService.get('DB_PASSWORD');", FlowEvidence.Interactive)]
	[InlineData("const entry = process.env.API_PASSWORD;", FlowEvidence.Fixed)]
	[InlineData("private readonly apiPassword: string = 'exampleValue';", FlowEvidence.Fixed)]
	public void AnInteractiveSignalWinsOverAFixedOneInTheSameFile(string source, FlowEvidence expected)
	{
		RecognitionResult result = new TypeScriptRecognitionHarness().RecognizeFully(WithTheHighConstruct(source));

		Assert.Equal(expected, result.FlowEvidence);
	}

	/// <summary>
	/// THE LITERAL RULE. A credential written into source is not flow evidence: the engine's literal-credential signal stays a security property. A file whose only credential is a
	/// literal, with no credential-named assignment, decides the default, and the literal signal is still raised.
	/// </summary>
	[Fact]
	public void ALiteralCredentialIsNotFlowEvidenceAndItsFileDecidesTheDefault()
	{
		string source = LiteralOnlyContent();
		RecognitionResult result = new TypeScriptRecognitionHarness().RecognizeFully(source);

		Assert.Equal(FlowEvidence.None, result.FlowEvidence);
		Assert.Contains(result.Constructs, construct => construct.LiteralCredentialSignal);

		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_LITERAL_ONLY_FILE_NAME, source);

		harness.RunTheWholeWorkflow();

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _LITERAL_ONLY_FILE_NAME, _LITERAL_PATTERN_CLASS);

		Assert.Equal(nameof(FlowDecision.Default), finding.FlowDecision);
		Assert.Equal(nameof(CredentialFlow.ClientCredentials), finding.Flow);
	}

	// ------------------------------------------------------------------------------------------
	// The flow a finding renders under the signals of TypeScript
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// An Angular form read and a NestJS body read each render Authorization Code, with the Code use site and its labelled variants; a NestJS configuration read and a typed literal each
	/// render Client Credentials with the fixed note; a file with no signal renders Client Credentials with the default note.
	/// </summary>
	[Fact]
	public void TheSignalsOfTypeScriptDecideTheFlowAFindingRenders()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_ANGULAR_FILE_NAME, $"const typed = this.loginForm.value.password;\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_NEST_FILE_NAME, $"async login(@Body('password') secret: string) {{ return secret; }}\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_NEST_CONFIGURATION_FILE_NAME, $"const entry = this.configService.get<string>('DB_PASSWORD');\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_TYPED_LITERAL_FILE_NAME, $"private readonly apiPassword: string = 'exampleValue';\n{_HIGH_CONSTRUCT}{_NEWLINE}");
		harness.WriteSourceFile(_PLAIN_FILE_NAME, $"{_HIGH_CONSTRUCT}{_NEWLINE}");

		harness.RunTheWholeWorkflow();

		ArtifactDocument document = ReadArtifact(harness);

		foreach (string fileName in new[] { _ANGULAR_FILE_NAME, _NEST_FILE_NAME })
		{
			ArtifactFindingDocument interactive = FindingOf(document, fileName, _HEADER_PATTERN_CLASS);

			Assert.Equal(nameof(CredentialFlow.AuthorizationCode), interactive.Flow);
			Assert.Equal(nameof(FlowDecision.InteractiveSignal), interactive.FlowDecision);
			Assert.Equal(FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE, interactive.FlowNote);
			Assert.Contains(_AUTHORIZATION_CODE_GRANT, interactive.Recommendation, StringComparison.Ordinal);
			Assert.DoesNotContain(_CLIENT_CREDENTIALS_GRANT, interactive.Recommendation, StringComparison.Ordinal);
			Assert.Contains("**Variant 1 — Browser with a server-side component**", interactive.Recommendation, StringComparison.Ordinal);
			Assert.Contains("**Variant 3 — Node.js command line or desktop**", interactive.Recommendation, StringComparison.Ordinal);
		}

		foreach (string fileName in new[] { _NEST_CONFIGURATION_FILE_NAME, _TYPED_LITERAL_FILE_NAME })
		{
			ArtifactFindingDocument fixedCredential = FindingOf(document, fileName, _HEADER_PATTERN_CLASS);

			Assert.Equal(nameof(CredentialFlow.ClientCredentials), fixedCredential.Flow);
			Assert.Equal(nameof(FlowDecision.FixedSignal), fixedCredential.FlowDecision);
			Assert.Equal(FlowNotes.FIXED_SIGNAL_CLIENT_CREDENTIALS_NOTE, fixedCredential.FlowNote);
			Assert.Contains(_CLIENT_CREDENTIALS_GRANT, fixedCredential.Recommendation, StringComparison.Ordinal);
		}

		ArtifactFindingDocument plain = FindingOf(document, _PLAIN_FILE_NAME, _HEADER_PATTERN_CLASS);

		Assert.Equal(nameof(CredentialFlow.ClientCredentials), plain.Flow);
		Assert.Equal(nameof(FlowDecision.Default), plain.FlowDecision);
		Assert.Equal(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, plain.FlowNote);
	}

	/// <summary>
	/// KNOWN LIMIT (the mixed idiom). A file that holds both an interactive and a fixed-credential signal is assigned Authorization Code, because the shared rule lets an interactive signal
	/// win. The mismatch with the expected flow is disclosed; this test pins it so that no change moves it unseen.
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

		ArtifactFindingDocument finding = FindingOf(ReadArtifact(harness), _BOTH_KINDS_FILE_NAME, _HEADER_PATTERN_CLASS);

		Assert.Equal(nameof(CredentialFlow.AuthorizationCode), finding.Flow);
		Assert.Equal(nameof(FlowDecision.InteractiveSignal), finding.FlowDecision);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static string LiteralOnlyContent()
	{
		return "const header = 'Basic ' + btoa('exampleUser:exampleSecretValue');\n";
	}

	private static string WithTheHighConstruct(string source)
	{
		return $"{source}{_NEWLINE}{_HIGH_CONSTRUCT}{_NEWLINE}";
	}

	/// <summary>The evidence one signal gives alone: the shipped analyzer, narrowed to that signal, on a file the shipped scanner and engine read.</summary>
	private static FlowEvidence EvidenceOfSignalAlone(string signalName, string source, string relativePath = TypeScriptRecognitionHarness.UNDEMOTED_RELATIVE_PATH)
	{
		TypeScriptRecognitionHarness harness = new();
		FlowSignal signal = harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals.Single(candidate => candidate.Name == signalName);
		FileRecognitionContext context = harness.ContextFor(source, relativePath);
		FileRecognitionContext alone = context with { Analyzer = new SingleSignalTypeScriptAnalyzer(harness.Analyzer, signal) };
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
		return TypeScriptRegisteredExpressions.Analyzer;
	}

	/// <summary>The shipped analyzer with every flow signal but one removed: the same profile, rules, demotion and dialects, so one signal can be read on its own.</summary>
	private sealed class SingleSignalTypeScriptAnalyzer : ILanguageAnalyzer
	{
		private readonly ILanguageAnalyzer _inner;

		internal SingleSignalTypeScriptAnalyzer(ILanguageAnalyzer inner, FlowSignal signal)
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
