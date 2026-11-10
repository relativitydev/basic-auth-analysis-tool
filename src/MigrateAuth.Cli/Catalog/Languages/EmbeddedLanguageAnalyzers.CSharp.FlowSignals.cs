using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The seventeen flow signals of the catalog:
/// ten <see cref="FlowSignalKind.InteractiveUser"/> and seven <see cref="FlowSignalKind.FixedCredential"/>.
///
/// Every expression declares <c>matchTimeoutMilliseconds</c> equal to
/// <see cref="RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS"/> and is
/// evaluated by the engine alone, once per file holding a High or Medium construct.
///
/// THE ANCHOR. Each expression names its anchor with the optional group <c>anchor</c>, placed
/// where its catalog entry places it: on the prompt wording, the key literal or the setting-name literal
/// for the signals that live in a string body, and on the call, attribute or type name for the code ones.
/// A window that must not swallow a later occurrence is written as a lookahead, so the match itself stays
/// small and the engine's walk to the next match cannot skip a genuine occurrence.
///
/// THE WHOLE-TOKEN RULE. <c>password</c>, <c>secret</c>, <c>credential</c>
/// and <c>token</c> match as case-insensitive substrings of a key. <c>pass</c>, <c>pwd</c> and
/// <c>apikey</c> match only as a whole token: a segment delimited by the key's start or end, by any
/// non-alphanumeric character, or by a lower-to-upper camel-case boundary. <c>apikey</c> is either the
/// token <c>apikey</c> or the two adjacent tokens <c>api</c> and <c>key</c>. The camel-case boundary tests
/// are case-sensitive and sit outside the <c>(?i:...)</c> groups that carry the words.
///
/// NO LITERAL CREDENTIAL IS FLOW EVIDENCE: nothing here reads a credential written into source.
///
/// EXPRESSION SHAPE. Every alternative starts on a literal or a word start, every window is bounded, and
/// no bounded lazy window sits inside an optional group, which the source generator mishandles.
/// </summary>
internal static partial class CSharpFlowSignalExpressions
{
	/// <summary>The start of a token: the key's start, a non-alphanumeric character, or a lower-to-upper boundary.</summary>
	private const string _TOKEN_START = @"(?:(?<![^\W_])|(?<=[a-z])(?=[A-Z]))";

	/// <summary>The end of a token: the key's end, a non-alphanumeric character, or a lower-to-upper boundary.</summary>
	private const string _TOKEN_END = @"(?:(?![^\W_])|(?<=[a-z])(?=[A-Z]))";

	/// <summary>Between the tokens <c>api</c> and <c>key</c>: a run of separators inside one key, or a camel-case boundary.</summary>
	private const string _API_KEY_SEPARATOR = @"(?:(?:_|[^\w""\r\n])+|(?<=[a-z])(?=[A-Z]))";

	private const string _WHOLE_TOKEN_CONFIGURATION_WORDS =
		_TOKEN_START + @"(?:(?i:pass|pwd|apikey)|(?i:api)" + _API_KEY_SEPARATOR + @"(?i:key))" + _TOKEN_END;

	private const string _CONFIGURATION_KEY_WORDS =
		@"(?:(?i:password|secret|credential|token)|" + _WHOLE_TOKEN_CONFIGURATION_WORDS + ")";

	/// <summary>Password-field words only: never <c>token</c>, so an anti-forgery token field does not match.</summary>
	private const string _FORM_FIELD_KEY_WORDS =
		@"(?:(?i:password)|" + _TOKEN_START + @"(?i:pass|pwd)" + _TOKEN_END + ")";

	private const string _CONSOLE_CREDENTIAL_WORDS =
		@"(?:(?i:passphrase|password|secret)|" + _TOKEN_START + @"(?i:pin)" + _TOKEN_END + ")";

	private const string _STRING_PREFIX = @"[$@]{0,2}";

	private const string _KEY_LOOKAHEAD_OPENING = @"(?=[^""\r\n]{0,120}?";

	private const string _CONFIGURATION_KEY_LITERAL =
		_STRING_PREFIX + @"""" + _KEY_LOOKAHEAD_OPENING + _CONFIGURATION_KEY_WORDS + @")[^""\r\n]{1,120}""";

	private const string _CONFIGURATION_KEY_LITERAL_ANCHORED =
		_STRING_PREFIX + @"""" + _KEY_LOOKAHEAD_OPENING + _CONFIGURATION_KEY_WORDS + @")(?<anchor>[^""\r\n]{1,120})""";

	private const string _FORM_FIELD_KEY_LITERAL_ANCHORED =
		_STRING_PREFIX + @"""" + _KEY_LOOKAHEAD_OPENING + _FORM_FIELD_KEY_WORDS + @")(?<anchor>[^""\r\n]{1,120})""";

	/// <summary>
	/// What may stand directly before an indexer bracket or a <c>.GetValue</c> for the read to be a
	/// configuration read, written for a lookbehind: a name that is or ends in a configuration word, with
	/// an optional null-conditional mark, or a <c>GetSection(...)</c> call with no nested parentheses and
	/// an optional null-conditional mark. Every branch is a bounded, literal-anchored sequence.
	/// </summary>
	private const string _CONFIGURATION_RECEIVER =
		@"(?:(?:[Cc]onfiguration|[Cc]onfig|[Ss]ection)\??|\bGetSection\s*\([^()\r\n]{0,120}\)\??)";

	private const string _ANY_STRING_LITERAL =_STRING_PREFIX + @"""[^""\r\n]{0,120}""";

	private const string _READ_KEY_CALL_TEXT = @"\bConsole\s*\.\s*ReadKey\s*\(\s*(?:intercept\s*:\s*)?true\s*\)";

	private const string _READ_KEY_CALL_ANCHORED =
		@"\bConsole\s*\.\s*(?<anchor>ReadKey)\s*\(\s*(?:intercept\s*:\s*)?true\s*\)";

	private const string _ENTER_COMPARISON = @"ConsoleKey\s*\.\s*Enter\b";

	private const string _KEY_CHARACTER_APPEND =
		@"(?:\+=|\.\s*Append\s*\()\s*[A-Za-z_][A-Za-z0-9_]*\s*\.\s*KeyChar\b";

	private const string _READ_KEY_NEAR_A_CREDENTIAL_NAME =
		@"(?:(?=[\s\S]{0,300}?" + _CONSOLE_CREDENTIAL_WORDS + @")|(?<=" + _CONSOLE_CREDENTIAL_WORDS + @"[\s\S]{0,300}" + _READ_KEY_CALL_TEXT + "))";

	private const string _READ_KEY_ACCUMULATING_UNTIL_ENTER =
		@"(?=[\s\S]{0,400}?" + _KEY_CHARACTER_APPEND + @")(?:(?=[\s\S]{0,400}?" + _ENTER_COMPARISON + @")|(?<=" + _ENTER_COMPARISON + @"[\s\S]{0,400}" + _READ_KEY_CALL_TEXT + "))";

	/// <summary>The window, in characters, in which a stored cast's own password member must be read; the figure the masked key read uses.</summary>
	private const string _CAST_READ_WINDOW_CHARACTERS = "400";

	private const string _PASSWORD_MEMBER = @"\s*\.\s*(?:Password|SecurePassword)\b";

	private const string _IDENTIFIER_CHAIN = @"[A-Za-z_][A-Za-z0-9_.]{0,80}";

	/// <summary>
	/// What a cast or <c>as</c> may be applied to: an identifier chain, then up to four call or indexer
	/// suffixes, each with argument text of at most 120 characters that holds no parenthesis, bracket or
	/// semicolon and stays on one line, and each optionally followed by a further member chain. Atomic, so
	/// nothing backtracks into it.
	/// </summary>
	private const string _CAST_OPERAND =
		@"(?>" + _IDENTIFIER_CHAIN + @"(?:\s*(?:\([^()\[\];\r\n]{0,120}\)|\[[^()\[\];\r\n]{0,120}\])(?:\.[A-Za-z_][A-Za-z0-9_.]{0,80})?){0,4})";

	private const string _PASSWORD_BOX_DECLARED_OR_READ =
		@"\b(?<anchor>PasswordBox)\b(?:\s+[A-Za-z_]|\s*\(|" + _PASSWORD_MEMBER + ")";

	/// <summary>A cast, or an <c>as</c> inside parentheses, whose closing parenthesis is followed directly by the password member.</summary>
	private const string _PASSWORD_BOX_CAST_READ_DIRECTLY =
		@"(?:(?<=\(\s{0,8})(?<anchor>PasswordBox)\b\s*\)\s*" + _CAST_OPERAND + @"\s*\)"
		+ @"|(?<=\bas\s{1,8})(?<anchor>PasswordBox)\b\s*\))" + _PASSWORD_MEMBER;

	/// <summary>A variable assigned a cast or an <c>as</c> result, and that same variable's password member read later.</summary>
	private const string _PASSWORD_BOX_CAST_STORED_THEN_READ =
		@"(?<![A-Za-z0-9_.])(?<variable>(?>[A-Za-z_][A-Za-z0-9_]{0,80}))\s*=\s*"
		+ @"(?:\(\s*(?<anchor>PasswordBox)\b|" + _CAST_OPERAND + @"\s+as\s+(?<anchor>PasswordBox)\b)"
		+ @"(?=[\s\S]{0," + _CAST_READ_WINDOW_CHARACTERS + @"}?(?<![A-Za-z0-9_.])\k<variable>" + _PASSWORD_MEMBER + ")";

	private const string _LOGIN_METHOD_NAME =
		@"(?<![A-Za-z0-9_])[A-Za-z0-9_]*?(?:Login|LogIn|SignIn|LogOn)[A-Za-z0-9_]*\s*\(";

	/// <summary>
	/// Console output whose first literal argument asks the person for a password, passphrase, secret or
	/// PIN. The anchor is the wording, which lives in a string body.
	/// </summary>
	[GeneratedRegex(
		@"\bConsole\s*\.\s*(?:Out\s*\.\s*)?Write(?:Line)?\s*\(\s*" + _STRING_PREFIX + @"""[^""\r\n]{0,120}?(?<anchor>" + _CONSOLE_CREDENTIAL_WORDS + ")",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConsolePasswordPromptText();

	/// <summary>A standard-input line assigned to a credential-named variable, field or property. The anchor is the name.</summary>
	[GeneratedRegex(
		@"(?<anchor>" + _CONSOLE_CREDENTIAL_WORDS + @"[A-Za-z0-9_]*)\s*=\s*(?:\(\s*string\s*\)\s*)?\bConsole\s*\.\s*(?:In\s*\.\s*)?ReadLine\s*\(\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConsoleReadLineIntoCredentialVariable();

	/// <summary>
	/// The echo-suppressing key read, in shape (a) near a credential-named identifier or in shape (b) in a
	/// loop that appends each key character until Enter. The anchor is the call.
	/// </summary>
	[GeneratedRegex(
		_READ_KEY_CALL_ANCHORED + "(?:" + _READ_KEY_NEAR_A_CREDENTIAL_NAME + "|" + _READ_KEY_ACCUMULATING_UNTIL_ENTER + ")",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConsoleMaskedKeyRead();

	/// <summary>
	/// The desktop password control's type, declared, constructed or read, or cast to and read. The
	/// anchor is the type name. Beyond the declared, constructed and member-read forms, two more are
	/// read: a cast or <c>as</c> expression whose password member is read directly, and a cast or
	/// <c>as</c> result stored in a variable whose own password member is read within 400 characters
	/// after it. The operand of the cast or <c>as</c> is an identifier chain, a call or an indexer, each
	/// with an optional member chain after it, on one statement and with no nested parentheses or
	/// brackets in the arguments. The variable is bound by name and the window is a lookahead, so the
	/// match stays small.
	/// </summary>
	[GeneratedRegex(
		_PASSWORD_BOX_DECLARED_OR_READ + "|" + _PASSWORD_BOX_CAST_READ_DIRECTLY + "|" + _PASSWORD_BOX_CAST_STORED_THEN_READ,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WpfPasswordBoxRead();

	/// <summary>The property assignments that turn a text control into a password field. The anchor is the property name.</summary>
	[GeneratedRegex(
		@"(?<![A-Za-z0-9_])(?:(?<anchor>UseSystemPasswordChar)\s*=\s*true\b|(?<anchor>PasswordChar)\s*=(?!=))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WinFormsPasswordMasking();

	/// <summary>A call to one of the identity methods that validate a submitted password. The anchor is the member name.</summary>
	[GeneratedRegex(
		@"\.\s*(?<anchor>PasswordSignInAsync|CheckPasswordSignInAsync|CheckPasswordAsync)\s*\(",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PasswordSignInCall();

	/// <summary>A posted-action attribute followed by a login-named method. The anchor is the attribute name.</summary>
	[GeneratedRegex(
		@"\[\s*(?<anchor>HttpPost)\b[^\]\r\n]{0,200}\](?=[^;{}]{0,300}?" + _LOGIN_METHOD_NAME + ")",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PostedLoginHandlerBinding();

	/// <summary>The password data-type attribute, in its short or qualified form. The anchor is the attribute name.</summary>
	[GeneratedRegex(
		@"(?<![A-Za-z0-9_])(?<anchor>DataType)(?:Attribute)?\s*\(\s*(?:[A-Za-z_][A-Za-z0-9_]*\s*\.\s*)*DataType\s*\.\s*Password\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PasswordDataTypeAttribute();

	/// <summary>
	/// The markup attribute <c>type="password"</c> inside a string, with its quotes escaped, doubled or
	/// single as the literal form requires. The anchor is the attribute text.
	/// </summary>
	[GeneratedRegex(
		@"(?<![A-Za-z0-9_-])(?<anchor>(?i:type))=(?:\\""|""""|""|')(?i:password)(?:\\""|""""|""|')",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HtmlPasswordInputMarkup();

	/// <summary>A posted form collection's indexer, keyed by a password-field name. The anchor is the key.</summary>
	[GeneratedRegex(
		@"(?:\b(?:form|collection)|(?:Form|Collection))\s*\??\s*\[\s*" + _FORM_FIELD_KEY_LITERAL_ANCHORED + @"\s*\]",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PostedFormCredentialFieldRead();

	/// <summary>
	/// A configuration read, by indexer or by <c>GetValue</c> or <c>GetSection</c>, keyed by a credential
	/// name. The anchor is the start of the read: the indexer bracket or the method name.
	///
	/// THE RECEIVER IS PART OF THE SHAPE. The expression cannot see a
	/// receiver's type, so it reads the receiver's name, as <see cref="PostedFormCredentialFieldRead"/>
	/// does: an indexer counts only where the name before the bracket is or ends in
	/// <c>Configuration</c>, <c>configuration</c>, <c>Config</c>, <c>config</c>, <c>Section</c> or
	/// <c>section</c>, or where the receiver is a <c>GetSection(...)</c> call - so a header, session,
	/// query, cookie or plain dictionary indexer keyed by a credential-like word does not count.
	/// <c>GetSection</c> is configuration-specific by name and keeps its meaning for any receiver.
	/// <c>GetValue</c> keeps its meaning where it carries a type argument, the configuration binder's
	/// own form, and otherwise needs the same receiver name, because the bare method name is shared with
	/// JSON and other object models.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?<=" + _CONFIGURATION_RECEIVER + @")(?<anchor>\[)\s*" + _CONFIGURATION_KEY_LITERAL + @"\s*\]"
		+ @"|\.\s*(?<anchor>GetSection)\s*(?:<[^<>;(){}\r\n]{1,80}>)?\s*\(\s*" + _CONFIGURATION_KEY_LITERAL
		+ @"|\.\s*(?<anchor>GetValue)\s*<[^<>;(){}\r\n]{1,80}>\s*\(\s*" + _CONFIGURATION_KEY_LITERAL
		+ @"|(?<=" + _CONFIGURATION_RECEIVER + @")\.\s*(?<anchor>GetValue)\s*\(\s*" + _CONFIGURATION_KEY_LITERAL + ")",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConfigurationIndexerCredentialRead();

	/// <summary>The framework-style settings collections, keyed by a credential name. The anchor is the collection name.</summary>
	[GeneratedRegex(
		@"\b(?:Web)?ConfigurationManager\s*\.\s*(?<anchor>AppSettings|ConnectionStrings)\s*(?:\[\s*" + _CONFIGURATION_KEY_LITERAL + @"\s*\]|\.\s*Get(?:Values?)?\s*\(\s*" + _CONFIGURATION_KEY_LITERAL + ")",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConfigurationManagerCredentialRead();

	/// <summary>An instance-setting read by section and name, the name credential-named. The anchor is the name literal.</summary>
	[GeneratedRegex(
		@"\.\s*Get[A-Za-z]{0,24}\s*\(\s*" + _ANY_STRING_LITERAL + @"\s*,\s*" + _CONFIGURATION_KEY_LITERAL_ANCHORED,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PlatformInstanceSettingCredentialRead();

	/// <summary>An environment-variable read whose name is credential-named. The anchor is the call.</summary>
	[GeneratedRegex(
		@"\bEnvironment\s*\.\s*(?<anchor>GetEnvironmentVariable)\s*\(\s*" + _CONFIGURATION_KEY_LITERAL,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex EnvironmentVariableCredentialRead();

	/// <summary>A secret-client type and a later secret fetch within a bounded window. The anchor is the fetch call.</summary>
	[GeneratedRegex(
		@"\bSecretClient\b(?=[\s\S]{0,2000}?\.\s*(?<anchor>GetSecret(?:Async)?)\s*\()",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SecretClientRead();

	/// <summary>The platform data-protection call that decrypts a stored value. The anchor is the member name.</summary>
	[GeneratedRegex(
		@"\bProtectedData\s*\.\s*(?<anchor>Unprotect)\s*\(",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ProtectedDataUnprotectRead();

	/// <summary>The configuration-builder call that adds the development secret store. The anchor is the member name.</summary>
	[GeneratedRegex(
		@"\.\s*(?<anchor>AddUserSecrets)\s*(?:<[^<>;(){}\r\n]{1,80}>)?\s*\(",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UserSecretsConfigurationSource();
}

/// <summary>The `CSharp` analyzer's <see cref="CandidateSelectionAndRecommendation.FlowSignals"/>.</summary>
internal static class CSharpFlowSignalDeclarations
{
	private const string _CONSOLE_PASSWORD_PROMPT_TEXT = "ConsolePasswordPromptText";
	private const string _CONSOLE_READ_LINE_INTO_CREDENTIAL_VARIABLE = "ConsoleReadLineIntoCredentialVariable";
	private const string _CONSOLE_MASKED_KEY_READ = "ConsoleMaskedKeyRead";
	private const string _WPF_PASSWORD_BOX_READ = "WpfPasswordBoxRead";
	private const string _WIN_FORMS_PASSWORD_MASKING = "WinFormsPasswordMasking";
	private const string _PASSWORD_SIGN_IN_CALL = "PasswordSignInCall";
	private const string _POSTED_LOGIN_HANDLER_BINDING = "PostedLoginHandlerBinding";
	private const string _PASSWORD_DATA_TYPE_ATTRIBUTE = "PasswordDataTypeAttribute";
	private const string _HTML_PASSWORD_INPUT_MARKUP = "HtmlPasswordInputMarkup";
	private const string _POSTED_FORM_CREDENTIAL_FIELD_READ = "PostedFormCredentialFieldRead";
	private const string _CONFIGURATION_INDEXER_CREDENTIAL_READ = "ConfigurationIndexerCredentialRead";
	private const string _CONFIGURATION_MANAGER_CREDENTIAL_READ = "ConfigurationManagerCredentialRead";
	private const string _PLATFORM_INSTANCE_SETTING_CREDENTIAL_READ = "PlatformInstanceSettingCredentialRead";
	private const string _ENVIRONMENT_VARIABLE_CREDENTIAL_READ = "EnvironmentVariableCredentialRead";
	private const string _SECRET_CLIENT_READ = "SecretClientRead";
	private const string _PROTECTED_DATA_UNPROTECT_READ = "ProtectedDataUnprotectRead";
	private const string _USER_SECRETS_CONFIGURATION_SOURCE = "UserSecretsConfigurationSource";

	internal static ImmutableArray<FlowSignal> Build()
	{
		return [.. InteractiveUserSignals(), .. FixedCredentialSignals()];
	}

	private static ImmutableArray<FlowSignal> InteractiveUserSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(_CONSOLE_PASSWORD_PROMPT_TEXT, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, CSharpFlowSignalExpressions.ConsolePasswordPromptText()),
			Signal(_CONSOLE_READ_LINE_INTO_CREDENTIAL_VARIABLE, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.ConsoleReadLineIntoCredentialVariable()),
			Signal(_CONSOLE_MASKED_KEY_READ, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.ConsoleMaskedKeyRead()),
			Signal(_WPF_PASSWORD_BOX_READ, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.WpfPasswordBoxRead()),
			Signal(_WIN_FORMS_PASSWORD_MASKING, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.WinFormsPasswordMasking()),
			Signal(_PASSWORD_SIGN_IN_CALL, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.PasswordSignInCall()),
			Signal(_POSTED_LOGIN_HANDLER_BINDING, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.PostedLoginHandlerBinding()),
			Signal(_PASSWORD_DATA_TYPE_ATTRIBUTE, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.PasswordDataTypeAttribute()),
			Signal(_HTML_PASSWORD_INPUT_MARKUP, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, CSharpFlowSignalExpressions.HtmlPasswordInputMarkup()),
			Signal(_POSTED_FORM_CREDENTIAL_FIELD_READ, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, CSharpFlowSignalExpressions.PostedFormCredentialFieldRead())
		];

		return signals;
	}

	private static ImmutableArray<FlowSignal> FixedCredentialSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(_CONFIGURATION_INDEXER_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.ConfigurationIndexerCredentialRead()),
			Signal(_CONFIGURATION_MANAGER_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.ConfigurationManagerCredentialRead()),
			Signal(_PLATFORM_INSTANCE_SETTING_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOrStringBody, CSharpFlowSignalExpressions.PlatformInstanceSettingCredentialRead()),
			Signal(_ENVIRONMENT_VARIABLE_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.EnvironmentVariableCredentialRead()),
			Signal(_SECRET_CLIENT_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.SecretClientRead()),
			Signal(_PROTECTED_DATA_UNPROTECT_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.ProtectedDataUnprotectRead()),
			Signal(_USER_SECRETS_CONFIGURATION_SOURCE, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, CSharpFlowSignalExpressions.UserSecretsConfigurationSource())
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
