using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The expressions of the eleven `Go` flow signals, six <see cref="FlowSignalKind.InteractiveUser"/> and five <see cref="FlowSignalKind.FixedCredential"/>. They are members of the same partial type as
/// the detection rules, so they read the same shared shapes: the bounded literal and window units, the whole-word guards and the credential-name vocabulary. Every expression carries the policy match
/// timeout and is evaluated by the engine alone, once per file that holds a High or Medium construct; the anchor is the group <c>anchor</c>, placed on a code token (or, for the markup signal, on the
/// opening of the tag) so that a signal that is not read in a string body can be told from one that is.
///
/// THE CREDENTIAL WORDS. <c>password</c>, <c>passwd</c>, <c>secret</c>, <c>credential</c>, <c>cred</c> and <c>token</c> match anywhere in a name, without regard to case; <c>pass</c>, <c>pw</c>, <c>pwd</c> and
/// <c>apikey</c> only as a whole token, and <c>Pass</c> also after a lower-case letter (a camel-case boundary), so <c>dbPass</c> and <c>DB_PASS</c> match and <c>bypass</c> and <c>compass</c> do not.
///
/// NO LITERAL CREDENTIAL IS FLOW EVIDENCE. The signal that reads a credential-named name assigned a string literal is an expression of its own with its own credential-name requirement; the literal-credential
/// signal of the detection rules is a security property and is never read here.
/// </summary>
internal static partial class GoPatternExpressions
{
	/// <summary>One credential word: the first group matches anywhere in a name, the second and third only as whole tokens.</summary>
	private const string _CREDENTIAL_WORD =
		@"(?:(?i:passw(?:or)?d|secret|credential|creds?|token)|(?<![A-Za-z0-9])(?i:pass|pw|pwd|apikey)(?![A-Za-z0-9])|(?<=[a-z])Pass(?![a-z0-9]))";

	/// <summary>A name that holds a credential word, read as a whole identifier of at most sixty-four characters.</summary>
	private const string _CREDENTIAL_NAME =
		@"(?:(?=[\p{L}\p{N}_]{0,64}" + _CREDENTIAL_WORD + @")[\p{L}\p{N}_]{1,64})";

	/// <summary>A one-line literal that holds a credential word, with at most one hundred characters before it and one hundred after it.</summary>
	private const string _CREDENTIAL_KEY_LITERAL =
		@"[\x22`][^\x22`\n]{0,100}" + _CREDENTIAL_WORD + @"[^\x22`\n]{0,100}[\x22`]";

	private const string _DESCRIPTOR_NAME =
		@"(?<![\p{L}\p{N}_.])(?:fd|descriptor|fileDescriptor)" + _NOT_WORD_AHEAD;

	private const string _STDIN_READ_CALL =
		@"(?:NewReader|NewScanner|ReadString)[ \t]{0,3}\(|Fscan";

	private const string _STDIN_CREDENTIAL_PROMPT =
		@"[\x22`][^\x22`\n]{0,100}(?i:password|passwd|passphrase|pass:)";

	private const string _AROUND_THE_STDIN_READ =
		@"(?:(?<=(?:" + _STDIN_READ_CALL + @")[\s\S]{0,300})|(?=[\s\S]{0,300}?(?:" + _STDIN_READ_CALL + @")))";

	private const string _AROUND_THE_STDIN_PROMPT =
		@"(?:(?<=" + _STDIN_CREDENTIAL_PROMPT + @"[\s\S]{0,300})|(?=[\s\S]{0,300}?" + _STDIN_CREDENTIAL_PROMPT + @"))";

	private const string _LOGIN_SEGMENT =
		@"(?:[^\x22`\n]{0,100}[/ {])?(?i:login|signin|sign-in|signon|logon)(?=[/ {}\x22`])";

	/// <summary>A terminal password read: the callee <c>ReadPassword</c>, qualified or bare, with standard input or a descriptor name on its line; its own declaration is refused.</summary>
	private const string _TERMINAL_PASSWORD_READ =
		_NOT_WORD_BEHIND + @"(?<anchor>ReadPassword)" + _NOT_WORD_AHEAD + @"(?<!(?:func|type)[ \t]{1,8}ReadPassword)(?<!\)[ \t]{0,8}ReadPassword)" + _SPACE + @"\((?=[^\n]{0,200}(?:" + _NOT_WORD_BEHIND + @"Stdin" + _NOT_WORD_AHEAD + @"|" + _DESCRIPTOR_NAME + @"))";

	/// <summary>Standard input with a read call and a prompt wording that names a credential, each within three hundred characters of it; the wording is in a string, so the signal reads string bodies.</summary>
	private const string _STDIN_CREDENTIAL_PROMPT_READ =
		_NOT_WORD_BEHIND + @"(?<anchor>Stdin)" + _NOT_WORD_AHEAD + _AROUND_THE_STDIN_READ + _AROUND_THE_STDIN_PROMPT;

	/// <summary>A formatted scan into a bare credential-named variable, from the standard input or from an unqualified scan.</summary>
	private const string _FMT_SCAN_CREDENTIAL_VARIABLE =
		_NOT_WORD_BEHIND + @"(?<anchor>Scanln|Scanf|Scan|(?:Fscanln|Fscan)(?=" + _SPACE + @"\(" + _SPACE + @"[\p{L}\p{N}_.]{0,41}Stdin" + _NOT_WORD_AHEAD + @"))" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _ARGUMENT_WINDOW + @"&" + _SPACE + _CREDENTIAL_NAME + _NOT_WORD_AHEAD;

	/// <summary>A form read whose key carries a credential word: <c>FormValue</c>, <c>PostFormValue</c>, <c>Form.Get</c>, <c>PostForm.Get</c> and the two index forms.</summary>
	private const string _LOGIN_FORM_PASSWORD_READ =
		@"(?:" + _NOT_WORD_BEHIND + @"(?<anchor>FormValue|PostFormValue)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + _CREDENTIAL_KEY_LITERAL
			+ @"|\.(?<anchor>Get)" + _NOT_WORD_AHEAD + @"(?=" + _SPACE + @"\(" + _SPACE + _CREDENTIAL_KEY_LITERAL + @")(?<=" + _NOT_WORD_BEHIND + @"(?:PostForm|Form)\.Get)"
			+ @"|" + _NOT_WORD_BEHIND + @"(?<anchor>PostForm|Form)" + _SPACE + @"\[" + _SPACE + _CREDENTIAL_KEY_LITERAL + _SPACE + @"\])";

	/// <summary>A password input in markup written into a literal: the attribute is in the same tag, quoted (escaped quotes of an interpreted literal included) or bare.</summary>
	private const string _HTML_PASSWORD_INPUT_MARKUP =
		@"(?<anchor>(?i:<input))(?=[^>]{0,200}(?<![\p{L}\p{N}_-])(?i:type)[ \t]{0,10}=[ \t]{0,10}\\?[\x27\x22]?password(?![\p{L}\p{N}_-]))";

	/// <summary>A route registration whose first argument holds a path segment that is a login word.</summary>
	private const string _LOGIN_ROUTE_REGISTRATION =
		_NOT_WORD_BEHIND + @"(?<anchor>HandleFunc|Handle)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + @"[\x22`]" + _LOGIN_SEGMENT;

	private const string _ENVIRONMENT_CREDENTIAL_READ =
		_NOT_WORD_BEHIND + @"(?<anchor>Getenv|LookupEnv)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + _CREDENTIAL_KEY_LITERAL;

	/// <summary>A credential-named setting read from a configuration receiver, directly or after one intermediate member, or a credential-named variable bound with <c>:=</c> from an indexed receiver.</summary>
	private const string _CONFIG_RECEIVER_CREDENTIAL_READ =
		@"(?:" + _NOT_WORD_BEHIND + @"(?<anchor>(?i:cfg|conf|config|settings))" + _NOT_WORD_AHEAD + @"(?:\.(?:Get|GetString|String)" + _SPACE + @"\(" + _SPACE + _CREDENTIAL_KEY_LITERAL + @"|\.(?:[\p{L}\p{N}_]{1,64}\.)?" + _CREDENTIAL_NAME + _NOT_WORD_AHEAD + @")"
			+ @"|" + _NOT_WORD_BEHIND + @"(?<anchor>" + _CREDENTIAL_NAME + @")" + _NOT_WORD_AHEAD + @"[ \t]{0,8}:=[ \t]{0,8}(?:[\p{L}\p{N}_]{1,64}\.){0,2}(?i:cfg|conf|config|settings)" + _NOT_WORD_AHEAD + @"[ \t]{0,8}\[)";

	private const string _SECRET_STORE_CLIENT_CALL =
		_NOT_WORD_BEHIND + @"(?<anchor>GetSecretValue|AccessSecretVersion|GetSecret)" + _NOT_WORD_AHEAD + @"(?<!(?:func|type)[ \t]{1,8}(?:GetSecretValue|AccessSecretVersion|GetSecret))(?<!\)[ \t]{0,8}(?:GetSecretValue|AccessSecretVersion|GetSecret))" + _SPACE + @"\(";

	/// <summary>A credential-named identifier or key assigned a one-line string literal; a name that ends in a label word is refused, the suffix matched as written.</summary>
	private const string _CREDENTIAL_NAMED_LITERAL_ASSIGNMENT =
		_NOT_WORD_BEHIND + @"(?:(?:const|var)[ \t]{1,8})?(?<anchor>" + _CREDENTIAL_NAME + @")(?<!(?:Label|Placeholder|Field|Name|Pattern|Regex|Length|Hint|Message|Error|Key|Header|Param|Env|EnvName|EnvVar|URL|Path))" + @"(?:[ \t]{1,8}" + _DECLARED_TYPE + @")?[ \t]{0,8}(?::=|=(?!=)|:(?!=))[ \t]{0,8}(?:\x22[^\x22\n]{1,400}\x22|`[^`\n]{1,400}`)";

	private const string _SECRET_FILE_READ =
		_NOT_WORD_BEHIND + @"(?<anchor>ReadFile)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + @"[\x22`][^\x22`\n]{0,200}(?i:secret|password|credential|token|/run/secrets/)[^\x22`\n]{0,200}[\x22`]";

	[GeneratedRegex(_TERMINAL_PASSWORD_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TerminalPasswordReadSignal();

	[GeneratedRegex(_STDIN_CREDENTIAL_PROMPT_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex StdinCredentialPromptReadSignal();

	[GeneratedRegex(_FMT_SCAN_CREDENTIAL_VARIABLE, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex FmtScanCredentialVariableSignal();

	[GeneratedRegex(_LOGIN_FORM_PASSWORD_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex LoginFormPasswordReadSignal();

	[GeneratedRegex(_HTML_PASSWORD_INPUT_MARKUP, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HtmlPasswordInputMarkupSignal();

	[GeneratedRegex(_LOGIN_ROUTE_REGISTRATION, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex LoginRouteRegistrationSignal();

	[GeneratedRegex(_ENVIRONMENT_CREDENTIAL_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex EnvironmentCredentialReadSignal();

	[GeneratedRegex(_CONFIG_RECEIVER_CREDENTIAL_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConfigReceiverCredentialReadSignal();

	[GeneratedRegex(_SECRET_STORE_CLIENT_CALL, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SecretManagerClientCallSignal();

	[GeneratedRegex(_CREDENTIAL_NAMED_LITERAL_ASSIGNMENT, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialNamedLiteralAssignmentSignal();

	[GeneratedRegex(_SECRET_FILE_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SecretFileReadSignal();
}

/// <summary>
/// The `Go` analyzer's <see cref="CandidateSelectionAndRecommendation.FlowSignals"/>: eleven signals, six interactive (three that stand for a command-line program and three for a web server) and five
/// fixed-credential. `Go` declares no dialect, no wrapper tracking and no variable use, so no signal carries any.
/// </summary>
internal static class GoFlowSignalDeclarations
{
	internal const string TERMINAL_PASSWORD_READ = "TerminalPasswordRead";
	internal const string STDIN_CREDENTIAL_PROMPT_READ = "StdinCredentialPromptRead";
	internal const string FMT_SCAN_CREDENTIAL_VARIABLE = "FmtScanCredentialVariable";
	internal const string LOGIN_FORM_PASSWORD_READ = "LoginFormPasswordRead";
	internal const string HTML_PASSWORD_INPUT_MARKUP = "HtmlPasswordInputMarkup";
	internal const string LOGIN_ROUTE_REGISTRATION = "LoginRouteRegistration";
	internal const string ENVIRONMENT_CREDENTIAL_READ = "EnvironmentCredentialRead";
	internal const string CONFIG_RECEIVER_CREDENTIAL_READ = "ConfigReceiverCredentialRead";
	internal const string SECRET_MANAGER_CLIENT_CALL = "SecretManagerClientCall";
	internal const string CREDENTIAL_NAMED_LITERAL_ASSIGNMENT = "CredentialNamedLiteralAssignment";
	internal const string SECRET_FILE_READ = "SecretFileRead";

	internal static ImmutableArray<FlowSignal> Build()
	{
		return [.. InteractiveUserSignals(), .. FixedCredentialSignals()];
	}

	private static ImmutableArray<FlowSignal> InteractiveUserSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(TERMINAL_PASSWORD_READ, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, GoPatternExpressions.TerminalPasswordReadSignal()),
			Signal(STDIN_CREDENTIAL_PROMPT_READ, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, GoPatternExpressions.StdinCredentialPromptReadSignal()),
			Signal(FMT_SCAN_CREDENTIAL_VARIABLE, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, GoPatternExpressions.FmtScanCredentialVariableSignal()),
			Signal(LOGIN_FORM_PASSWORD_READ, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, GoPatternExpressions.LoginFormPasswordReadSignal()),
			Signal(HTML_PASSWORD_INPUT_MARKUP, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, GoPatternExpressions.HtmlPasswordInputMarkupSignal()),
			Signal(LOGIN_ROUTE_REGISTRATION, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, GoPatternExpressions.LoginRouteRegistrationSignal())
		];

		return signals;
	}

	private static ImmutableArray<FlowSignal> FixedCredentialSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(ENVIRONMENT_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, GoPatternExpressions.EnvironmentCredentialReadSignal()),
			Signal(CONFIG_RECEIVER_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, GoPatternExpressions.ConfigReceiverCredentialReadSignal()),
			Signal(SECRET_MANAGER_CLIENT_CALL, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, GoPatternExpressions.SecretManagerClientCallSignal()),
			Signal(CREDENTIAL_NAMED_LITERAL_ASSIGNMENT, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, GoPatternExpressions.CredentialNamedLiteralAssignmentSignal()),
			Signal(SECRET_FILE_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, GoPatternExpressions.SecretFileReadSignal())
		];

		return signals;
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
