using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The expressions of the sixteen `JavaScript` flow signals, twelve <see cref="FlowSignalKind.InteractiveUser"/> and four <see cref="FlowSignalKind.FixedCredential"/>
/// (<see cref="LanguageCatalog.CurrentVersion"/> carries its own number). They are members of the same partial type as the detection rules, so they read the same
/// vocabulary: the argument window, the definition window and the blanks. Every expression carries the policy match timeout and is evaluated by the engine alone, once per file
/// that holds a High or Medium construct; the anchor is the optional group <c>anchor</c>, placed on the callee, the property, the key or the markup the signal stands on, so a
/// <see cref="FlowSignalZone.CodeOnly"/> signal is admitted only when that token is code.
///
/// THE CREDENTIAL WORDS are the other catalogs' whole-token rule: <c>password</c>, <c>passwd</c>, <c>secret</c>, <c>credential</c>, <c>credentials</c>, <c>creds</c> and <c>token</c> match
/// anywhere in a name, case-insensitively, and <c>pass</c>, <c>pw</c>, <c>pwd</c> and <c>apikey</c> only as a whole token or after a camel-case boundary, so <c>dbPass</c> matches and
/// <c>bypass</c> does not.
///
/// A LITERAL CREDENTIAL IS NOT FLOW EVIDENCE: the engine's literal-credential signal is a security property and nothing here reads it. The one signal that reads a credential written
/// into source, <c>CredentialNamedLiteralAssignment</c>, is an expression of its own with its own credential-named requirement, and it does not filter the placeholder vocabulary.
/// </summary>
internal static partial class JavaScriptPatternExpressions
{
	private const string _CREDENTIAL_WORD =
		@"(?:(?i:passw(?:or)?d|secret|credential|creds?|token)|(?<![A-Za-z0-9])(?i:pass|pw|pwd|apikey)(?![A-Za-z0-9])|(?<=[a-z])Pass(?![a-z0-9]))";

	private const string _CREDENTIAL_NAME =
		@"(?:(?=[\w$]{0,64}?" + _CREDENTIAL_WORD + @")[\w$]{1,64})";

	private const string _EX_JS_120 =
		@"(?<![\w$])(?<anchor>getElementById|querySelectorAll|querySelector)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,100}?(?:" + _CREDENTIAL_WORD + @"|type\s{0,10}=\s{0,10}[\x27\x22]?password)[^\x27\x22`\n]{0,100}[\x27\x22`]" + _SPACE + @"\)[\s\S]{0,200}?\.value(?![\w$])";

	private const string _FORM_RECEIVER =
		@"(?<![\w$.])(?:[A-Za-z_$][\w$]{0,40}\.){0,3}(?:[\w$]{0,40}(?i:form)[\w$]{0,40}|elements)(?:\[\d{1,3}\])?(?:\.elements)?";

	private const string _FORM_DATA_RECEIVER =
		@"(?:new[ \t]{1,8}FormData" + _SPACE + @"\([^)\n]{0,200}\)|(?<![\w$.])[\w$]{0,40}(?i:formdata))";

	/// <summary>Each alternative opens at a literal (the dot or the bracket), and the receiver that precedes it is read by a look-behind once the property has matched.</summary>
	private const string _EX_JS_121 =
		@"(?:\.(?<anchor>password)(?![\w$])(?<=" + _FORM_RECEIVER + @"\.password)\.value(?![\w$])|\[\s{0,10}[\x27\x22](?<anchor>password)[\x27\x22]\s{0,10}\](?<=" + _FORM_RECEIVER + @"\[\s{0,10}[\x27\x22]password[\x27\x22]\s{0,10}\])\.value(?![\w$])|\.(?<anchor>get)(?=" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22])(?<=" + _FORM_DATA_RECEIVER + @"\.get)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]|\.(?<anchor>namedItem)(?![\w$])(?<=" + _FORM_RECEIVER + @"\.namedItem)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]" + _SPACE + @"\)\.value(?![\w$]))";

	private const string _PROMPT_DECLARATION_BEHIND =
		@"(?<!(?:(?:function|const|let|var|class)[ \t]{1,8}prompt(?![\w$])|[(,][ \t]{0,8}prompt[ \t]{0,8}[,)=]|import[^\n;]{0,200}(?<![\w$.])prompt(?![\w$]))[^\n]{0,400}(?:\n[^\n]{0,400}){0,30})";

	private const string _EX_JS_122 =
		@"(?:(?<![\w$.])window\.(?<anchor>prompt)(?![\w$])" + _SPACE + @"\(|(?<![\w$.])(?<anchor>prompt)(?![\w$])(?=" + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,200}?" + _CREDENTIAL_WORD + @")" + _PROMPT_DECLARATION_BEHIND + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,200}?" + _CREDENTIAL_WORD + @")";

	private const string _EX_JS_123 =
		@"(?<![\w$])(?<anchor>localStorage|sessionStorage)(?:\.getItem" + _SPACE + @"\(|\[)" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,100}?(?i:password|passwd|pwd|credentials)[^\x27\x22`\n]{0,100}[\x27\x22`]";

	private const string _EX_JS_124 =
		_OPEN_AFTER_A_DOT + @"(?<anchor>open)(?![\w$])" + _NOT_ON_A_WINDOW_OR_DOCUMENT + _SPACE + @"\(" + _FIVE_ARGUMENTS + @"(?=(?>(?:" + _ARGUMENT_UNIT_WITHOUT_A_COMMA + @"){1,300})" + _SPACE + @"\))";

	private const string _EX_JS_125 =
		@"(?<anchor>(?i:<input))(?=[\s\S]{0,200}?(?<![\w$-])type\s{0,10}=\s{0,10}\\?[\x27\x22]?password(?![\w-]))";

	private const string _EX_JS_126 =
		@"(?:(?<![\w$.])(?:req|request|ctx\.request)\.(?<anchor>body)(?:\.(?:" + _CREDENTIAL_NAME + @")(?![\w$])|\[\s{0,10}[\x27\x22](?:" + _CREDENTIAL_NAME + @")[\x27\x22]\s{0,10}\])|\{[^{}\n]{0,200}?(?<![\w$])" + _CREDENTIAL_NAME + @"(?![\w$])[^{}\n]{0,200}\}\s{0,10}=\s{0,10}(?<![\w$.])(?:req|request|ctx\.request)\.(?<anchor>body)(?![\w$])|" + _SEARCH_PARAMS_FROM_THE_REQUEST_INLINE + @"|" + _SEARCH_PARAMS_FROM_THE_REQUEST_BOUND + @")";

	/// <summary>A word that says the text read is the request's own body.</summary>
	private const string _REQUEST_BODY_WORD =
		@"(?<![\w$])(?i:req|request|body|chunks?|payload)(?![\w$])";

	/// <summary>The inline form of the request's own body: `new URLSearchParams(body).get('password')`, the constructor's argument naming the request's text.</summary>
	private const string _SEARCH_PARAMS_FROM_THE_REQUEST_INLINE =
		@"(?<![\w$.])new[ \t]{1,8}URLSearchParams" + _SPACE + @"\((?=[^)\n]{0,200}" + _REQUEST_BODY_WORD + @")(?>[^)\n]{0,200})\)" + _SPACE + @"\.(?<anchor>get)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]";

	/// <summary>The bound form of the request's own body: a name bound to `new URLSearchParams(` of the request's text within the definition window, then `.get('password')` on it.</summary>
	private const string _SEARCH_PARAMS_FROM_THE_REQUEST_BOUND =
		@"\.(?<anchor>get)(?![\w$])(?=" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22])(?<=(?<![\w$.])(?<receiver>[A-Za-z_$][\w$]{0,40})\.get)(?<=(?<![\w$.])(?:(?:const|let|var)[ \t]{1,8})?\k<receiver>" + _BLANKS + @"=" + _BLANKS + @"new[ \t]{1,8}URLSearchParams" + _BLANKS + @"\((?=[^)\n]{0,200}" + _REQUEST_BODY_WORD + @")[^\n]{0,400}(?:\n[^\n]{0,400}){0,30})" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]";

	private const string _EX_JS_127 =
		@"(?<![\w$])(?<anchor>_writeToOutput)" + _BLANKS + @"=(?!=)|(?<![\w$])(?<anchor>setRawMode)(?![\w$])" + _SPACE + @"\((?:(?<=(?i:passw(?:or)?d|passphrase|secret|credential|pwd)[\s\S]{0,600})|(?=[\s\S]{0,600}?(?i:passw(?:or)?d|passphrase|secret|credential|pwd)))";

	private const string _EX_JS_128 =
		@"(?<![\w$.\x27\x22])(?<anchor>type)(?![\w$])" + _SPACE + @":" + _SPACE + @"[\x27\x22`]password[\x27\x22`]";

	private const string _EX_JS_129 =
		@"(?<![\w$])(?<anchor>question|prompt)(?![\w$])" + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,200}?(?i:password|passphrase)";

	private const string _EX_JS_130 =
		@"(?<![\w$.])process\.(?<anchor>env)(?:\.(?<name>" + _CREDENTIAL_NAME + @")(?![\w$])|\[\s{0,10}[\x27\x22](?<name>" + _CREDENTIAL_NAME + @")[\x27\x22]\s{0,10}\])";

	private const string _EX_JS_131 =
		@"(?<![\w$.])(?<anchor>config|conf|nconf|settings|cfg)(?:\.(?:get|getString|getValue|require)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,100}?" + _CREDENTIAL_WORD + @"|\.(?<name>" + _CREDENTIAL_NAME + @")(?![\w$]))";

	private const string _EX_JS_132 =
		@"(?<![\w$])(?<anchor>getSecretValue|GetSecretValueCommand|getSecret|accessSecretVersion|getPassword)" + _SPACE + @"\(";

	private const string _EX_JS_133 =
		@"(?<![\w$])(?:(?:const|let|var)[ \t]{1,8})?(?<anchor>" + _CREDENTIAL_NAME + @")(?<!(?:Label|Placeholder|Field|Name|Pattern|Regex|Length|Hint|Message|Error))" + _BLANKS + @"[=:](?![=>])" + _SPACE + @"(?:\x27(?!(?:include|omit|same-origin)\x27)[^\x27\n]{1,400}\x27|\x22(?!(?:include|omit|same-origin)\x22)[^\x22\n]{1,400}\x22|`(?!(?:include|omit|same-origin)`)[^`$\n]{1,400}`)";

	/// <summary>
	/// The first interactive signal: `question(` on a name that a `createInterface(` call binds within the definition window of the same file. The prompt's wording is not read, so a
	/// question asked through a helper that holds the call is read as well.
	/// </summary>
	private const string _EX_JS_134 =
		@"\.(?<anchor>question)(?![\w$])(?=" + _SPACE + @"\()(?<=(?<![\w$.])(?<receiver>[A-Za-z_$][\w$]{0,40})\.question)(?<=(?<![\w$.])(?:(?:const|let|var)[ \t]{1,8})?\k<receiver>" + _BLANKS + @"=" + _BLANKS + @"(?:await[ \t]{1,8})?(?:[A-Za-z_$][\w$]{0,40}\.){0,2}createInterface" + _BLANKS + @"\([^\n]{0,400}(?:\n[^\n]{0,400}){0,30})" + _SPACE + @"\(";

	/// <summary>The second interactive signal: the constructor of a `Writable` whose text names a muted state within 400 units (the idiom that hides typed characters).</summary>
	private const string _EX_JS_135 =
		@"(?<![\w$.])new[ \t]{1,8}(?<anchor>Writable)(?![\w$])" + _SPACE + @"\((?=[\s\S]{0,400}?(?:(?<![A-Za-z])(?i:mute)|(?<=[a-z])M(?i:ute))d?(?![A-Za-z]))";

	/// <summary>EX-JS-120: the expression of the <c>DomPasswordFieldRead</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_120, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex DomPasswordFieldReadSignal();

	/// <summary>EX-JS-121: the expression of the <c>FormPasswordPropertyRead</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_121, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex FormPasswordPropertyReadSignal();

	/// <summary>EX-JS-122: the expression of the <c>BrowserPromptCall</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_122, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex BrowserPromptCallSignal();

	/// <summary>EX-JS-123: the expression of the <c>BrowserStorageRead</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_123, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex BrowserStorageReadSignal();

	/// <summary>EX-JS-124: the expression of the <c>XhrFiveArgumentOpen</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_124, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex XhrFiveArgumentOpenSignal();

	/// <summary>EX-JS-125: the expression of the <c>HtmlPasswordInputMarkup</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_125, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HtmlPasswordInputMarkupSignal();

	/// <summary>EX-JS-126: the expression of the <c>RequestBodyPasswordForwarded</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_126, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex RequestBodyPasswordForwardedSignal();

	/// <summary>EX-JS-127: the expression of the <c>NodeMutedPasswordPrompt</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_127, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NodeMutedPasswordPromptSignal();

	/// <summary>EX-JS-128: the expression of the <c>PromptLibraryPasswordType</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_128, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PromptLibraryPasswordTypeSignal();

	/// <summary>EX-JS-129: the expression of the <c>PromptWording</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_129, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PromptWordingSignal();

	/// <summary>EX-JS-134: the expression of the <c>ReadlineInterfaceQuestion</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_134, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ReadlineInterfaceQuestionSignal();

	/// <summary>EX-JS-135: the expression of the <c>MutedWritableOutput</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_135, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex MutedWritableOutputSignal();

	/// <summary>EX-JS-130: the expression of the <c>ProcessEnvCredentialRead</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_130, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ProcessEnvCredentialReadSignal();

	/// <summary>EX-JS-131: the expression of the <c>ConfigLibraryCredentialRead</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_131, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConfigLibraryCredentialReadSignal();

	/// <summary>EX-JS-132: the expression of the <c>SecretManagerClientCall</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_132, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SecretManagerClientCallSignal();

	/// <summary>EX-JS-133: the expression of the <c>CredentialNamedLiteralAssignment</c> flow signal.</summary>
	[GeneratedRegex(_EX_JS_133, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialNamedLiteralAssignmentSignal();
}

/// <summary>The dialects of a `JavaScript` file: the default <c>script</c>, and <c>node</c> for the files that declare themselves Node.js.</summary>
internal static class JavaScriptDialectNames
{
	internal const string SCRIPT = "script";
	internal const string NODE = "node";
}

/// <summary>
/// The `JavaScript` analyzer's <see cref="CandidateSelectionAndRecommendation.FlowSignals"/>: sixteen signals, twelve interactive and four fixed-credential. The
/// browser-document signals and the two that read markup and prompt wording exclude the <c>node</c> dialect, so a server file that merely holds a variable named
/// <c>document</c>, markup in a string or prompt wording in a validation message is a near-miss; no signal uses an include list. No signal tracks a variable:
/// variable use is not tracked in `JavaScript`.
/// </summary>
internal static class JavaScriptFlowSignalDeclarations
{
	private const string _DOM_PASSWORD_FIELD_READ = "DomPasswordFieldRead";
	private const string _FORM_PASSWORD_PROPERTY_READ = "FormPasswordPropertyRead";
	private const string _BROWSER_PROMPT_CALL = "BrowserPromptCall";
	private const string _BROWSER_STORAGE_READ = "BrowserStorageRead";
	private const string _XHR_FIVE_ARGUMENT_OPEN = "XhrFiveArgumentOpen";
	private const string _HTML_PASSWORD_INPUT_MARKUP = "HtmlPasswordInputMarkup";
	private const string _REQUEST_BODY_PASSWORD_FORWARDED = "RequestBodyPasswordForwarded";
	private const string _NODE_MUTED_PASSWORD_PROMPT = "NodeMutedPasswordPrompt";
	private const string _PROMPT_LIBRARY_PASSWORD_TYPE = "PromptLibraryPasswordType";
	private const string _PROMPT_WORDING = "PromptWording";
	private const string _READLINE_INTERFACE_QUESTION = "ReadlineInterfaceQuestion";
	private const string _MUTED_WRITABLE_OUTPUT = "MutedWritableOutput";
	private const string _PROCESS_ENV_CREDENTIAL_READ = "ProcessEnvCredentialRead";
	private const string _CONFIG_LIBRARY_CREDENTIAL_READ = "ConfigLibraryCredentialRead";
	private const string _SECRET_MANAGER_CLIENT_CALL = "SecretManagerClientCall";
	private const string _CREDENTIAL_NAMED_LITERAL_ASSIGNMENT = "CredentialNamedLiteralAssignment";

	/// <summary>The dialect whose files are server or command-line code, and so are not read for the browser-document signals.</summary>
	private static readonly ImmutableArray<string> _dialectsThatAreNotABrowser = [JavaScriptDialectNames.NODE];

	internal static ImmutableArray<FlowSignal> Build()
	{
		return [.. InteractiveUserSignals(), .. FixedCredentialSignals()];
	}

	private static ImmutableArray<FlowSignal> InteractiveUserSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			BrowserSignal(_DOM_PASSWORD_FIELD_READ, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.DomPasswordFieldReadSignal()),
			BrowserSignal(_FORM_PASSWORD_PROPERTY_READ, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.FormPasswordPropertyReadSignal()),
			BrowserSignal(_BROWSER_PROMPT_CALL, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.BrowserPromptCallSignal()),
			BrowserSignal(_BROWSER_STORAGE_READ, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.BrowserStorageReadSignal()),
			BrowserSignal(_XHR_FIVE_ARGUMENT_OPEN, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.XhrFiveArgumentOpenSignal()),
			BrowserSignal(_HTML_PASSWORD_INPUT_MARKUP, FlowSignalZone.CodeOrStringBody, JavaScriptPatternExpressions.HtmlPasswordInputMarkupSignal()),
			Signal(_REQUEST_BODY_PASSWORD_FORWARDED, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.RequestBodyPasswordForwardedSignal()),
			Signal(_NODE_MUTED_PASSWORD_PROMPT, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.NodeMutedPasswordPromptSignal()),
			Signal(_PROMPT_LIBRARY_PASSWORD_TYPE, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.PromptLibraryPasswordTypeSignal()),
			BrowserSignal(_PROMPT_WORDING, FlowSignalZone.CodeOrStringBody, JavaScriptPatternExpressions.PromptWordingSignal()),
			Signal(_READLINE_INTERFACE_QUESTION, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.ReadlineInterfaceQuestionSignal()),
			Signal(_MUTED_WRITABLE_OUTPUT, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.MutedWritableOutputSignal())
		];

		return signals;
	}

	private static ImmutableArray<FlowSignal> FixedCredentialSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(_PROCESS_ENV_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.ProcessEnvCredentialReadSignal()),
			Signal(_CONFIG_LIBRARY_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.ConfigLibraryCredentialReadSignal()),
			Signal(_SECRET_MANAGER_CLIENT_CALL, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.SecretManagerClientCallSignal()),
			Signal(_CREDENTIAL_NAMED_LITERAL_ASSIGNMENT, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, JavaScriptPatternExpressions.CredentialNamedLiteralAssignmentSignal())
		];

		return signals;
	}

	/// <summary>An interactive signal of the browser: refused in a file that declares itself <c>node</c>.</summary>
	private static FlowSignal BrowserSignal(string name, FlowSignalZone zone, Regex expression)
	{
		return new FlowSignal
		{
			Name = name,
			Kind = FlowSignalKind.InteractiveUser,
			Zone = zone,
			Expression = expression,
			ExcludedDialects = _dialectsThatAreNotABrowser
		};
	}

	private static FlowSignal Signal(string name, FlowSignalKind kind, FlowSignalZone zone, Regex expression)
	{
		return new FlowSignal
		{
			Name = name,
			Kind = kind,
			Zone = zone,
			Expression = expression
		};
	}
}
