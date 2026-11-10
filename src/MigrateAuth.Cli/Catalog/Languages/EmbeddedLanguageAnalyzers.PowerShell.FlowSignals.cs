using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The nine flow signals of the `PowerShell` catalog, five <see cref="FlowSignalKind.InteractiveUser"/> and four
/// <see cref="FlowSignalKind.FixedCredential"/> (<see cref="LanguageCatalog.CurrentVersion"/> carries its own number). Their expressions are members of
/// <see cref="PowerShellPatternExpressions"/>, the same partial type as the detection rules, so they read the same vocabulary: the parameter dash and
/// the parameter prefixes, the command window and the six kinds of quoted literal.
///
/// Every expression declares the policy match timeout and is evaluated by the engine alone, once per file that holds a High or Medium
/// construct. The anchor is the optional group <c>anchor</c>, placed on the command name or the member for the signals that live in code, and on
/// the wording or the name for the two that live in a string body.
///
/// THE WHOLE-TOKEN RULE (shared with the `CSharp` analyzer). <c>password</c>, <c>secret</c>, <c>credential</c> and <c>token</c> match as substrings of a
/// name. <c>pass</c>, <c>pwd</c> and <c>apikey</c> match only as a whole token, a segment bounded by the name's ends, by any non-alphanumeric
/// character (an underscore included) or by a lower-to-upper camel-case boundary; <c>apikey</c> is also the two tokens <c>api</c> and <c>key</c>. The
/// camel-case tests are case-sensitive and sit outside the case-insensitive reading of the words.
///
/// NO LITERAL CREDENTIAL IS FLOW EVIDENCE: nothing here reads a credential written into source, and the plain-text conversion excludes a literal argument.
/// </summary>
internal static partial class PowerShellPatternExpressions
{
	private const string _CAMEL_CASE_BOUNDARY = @"(?-i:(?<=[a-z])(?=[A-Z]))";

	private const string _TOKEN_START = @"(?:(?<![^\W_])|" + _CAMEL_CASE_BOUNDARY + @")";

	private const string _TOKEN_END = @"(?:(?![^\W_])|" + _CAMEL_CASE_BOUNDARY + @")";

	private const string _API_KEY_SEPARATOR = @"(?:(?:_|[^\w""'\n])+|" + _CAMEL_CASE_BOUNDARY + @")";

	private const string _WHOLE_TOKEN_WORDS =
		_TOKEN_START + @"(?:pass|pwd|apikey|api" + _API_KEY_SEPARATOR + @"key)" + _TOKEN_END;

	private const string _CREDENTIAL_NAME_WORDS = @"(?:password|secret|credential|token|" + _WHOLE_TOKEN_WORDS + @")";

	/// <summary>The words a prompt asks for: a password, a passphrase, a secret, a PIN, a user name.</summary>
	private const string _PROMPT_CREDENTIAL_WORDS =
		@"(?:passphrase|password|secret|user[ ]?name|(?<![A-Za-z0-9])pin(?![A-Za-z0-9]))";

	private const string _PARAMETER_AS_SECURE_STRING =
		_PARAMETER_START + @"A(?:sSecureString|sSecureStrin|sSecureStri|sSecureStr|sSecureSt|sSecureS|sSecure|sSecur|sSecu|sSec|sSe|sS|s)?" + _PARAMETER_END;

	private const string _PARAMETER_MASK_INPUT =
		_PARAMETER_START + @"M(?:askInput|askInpu|askInp|askIn|askI|ask|as|a)?" + _PARAMETER_END;

	private const string _PARAMETER_AS_PLAIN_TEXT =
		_PARAMETER_START + @"A(?:sPlainText|sPlainTex|sPlainTe|sPlainT|sPlain|sPlai|sPla|sPl|sP|s)?" + _PARAMETER_END;

	private const string _PARAMETER_FORCE = _PARAMETER_START + @"F(?:orce|orc|or|o)?" + _PARAMETER_END;

	private const string _PARAMETER_STRING = _PARAMETER_START + @"St(?:ring|rin|ri|r)?" + _PARAMETER_END;

	/// <summary>
	/// The credential prompt that is directly a remoting-family cmdlet's <c>-Credential</c> argument, read from the text before it: the credential then
	/// authenticates to Windows and says nothing about the flow. Only the direct form is excluded; a variable assigned the prompt and passed later is not.
	/// </summary>
	private const string _REMOTING_CREDENTIAL_ARGUMENT_BEFORE =
		_NON_HTTP_SINK_CMDLET + @"(?![\w\-])(?:[^\n|;`]|`\n|`.){0,400}" + _PARAMETER_CREDENTIAL + @"(?:[ \t]*:[ \t]*|[ \t]+)\$?\([ \t]*";

	/// <summary>The credential prompt command. It is a command in its own right: <c>Get-CredentialFromVault</c> is another command.</summary>
	[GeneratedRegex(
		@"(?<![\w\-$.])(?<!" + _REMOTING_CREDENTIAL_ARGUMENT_BEFORE + @")(?<anchor>Get-Credential)(?![\w\-])",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GetCredentialCallSignal();

	/// <summary>The console read with the echo suppressed: <c>-AsSecureString</c> under every prefix from <c>-A</c>, or <c>-MaskInput</c> on the newer edition.</summary>
	[GeneratedRegex(
		@"(?<![\w\-$.])(?<anchor>Read-Host)(?![\w\-])" + _COMMAND_WINDOW + @"(?:" + _PARAMETER_AS_SECURE_STRING + @"|" + _PARAMETER_MASK_INPUT + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ReadHostSecureInputSignal();

	/// <summary>The host-level credential dialog, with the host variable spelled <c>$Host</c> or <c>$host</c>.</summary>
	[GeneratedRegex(
		@"\$host\.UI\.(?<anchor>PromptForCredential)[ \t]*\(",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PromptForCredentialCallSignal();

	/// <summary>
	/// A console read whose prompt text asks for a credential. The evidence is the wording, which exists only in a string body, so the anchor sits on the
	/// words and the signal is read outside comments.
	/// </summary>
	[GeneratedRegex(
		@"(?<![\w\-$.])Read-Host(?![\w\-])[^\n]{0,200}?" + _QUOTE_OPENING + @"[^""'\u2019\u201D\n]{0,120}?(?<anchor>" + _PROMPT_CREDENTIAL_WORDS + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ReadHostCredentialPromptTextSignal();

	/// <summary>
	/// A desktop password control: the form-library property assignments (<c>UseSystemPasswordChar = $true</c>, a <c>PasswordChar</c> assignment), and a
	/// presentation-library <c>PasswordBox</c> created or cast whose <c>Password</c> or <c>SecurePassword</c> is read within ten lines.
	/// </summary>
	[GeneratedRegex(
		@"(?:(?<![\w])(?<anchor>UseSystemPasswordChar)[ \t]*=[ \t]*\$true(?![\w])"
		+ @"|(?<![\w])(?<anchor>PasswordChar)(?![\w])[ \t]*="
		+ @"|(?:" + _NEW_OBJECT + @"[ \t]+|\[)(?:[A-Za-z_]\w*\.){0,6}(?<anchor>PasswordBox)(?![\w])" + _LINE_WINDOW_TEN + @"\.(?:Password|SecurePassword)(?![\w]))",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex DesktopPasswordControlSignal();

	/// <summary>
	/// A credential read from the process environment: <c>$env:NAME</c>, <c>${env:NAME}</c>, the base library's <c>GetEnvironmentVariable</c> and
	/// <c>Get-Item Env:NAME</c>, where the name carries a credential word. The anchor sits on the <c>env</c> token or the command, which are code.
	/// </summary>
	[GeneratedRegex(
		@"(?:\$(?<anchor>env):[A-Za-z0-9_]{0,60}?" + _CREDENTIAL_NAME_WORDS
		+ @"|\$\{(?<anchor>env):[^{}\n]{0,60}?" + _CREDENTIAL_NAME_WORDS
		+ @"|\[(?:System\.)?Environment\]::(?<anchor>GetEnvironmentVariable)[ \t]*\([ \t]*['""][^'""\n]{0,60}?" + _CREDENTIAL_NAME_WORDS
		+ @"|(?<![\w\-$.])(?<anchor>Get-Item|Get-ChildItem)(?![\w\-])[^\n|]{0,60}?Env:[A-Za-z0-9_]{0,60}?" + _CREDENTIAL_NAME_WORDS + @")",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex EnvironmentCredentialReadSignal();

	/// <summary>The command that reads a credential saved with <c>Export-Clixml</c>.</summary>
	[GeneratedRegex(
		@"(?<![\w\-$.])(?<anchor>Import-Clixml)(?![\w\-])",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ImportClixmlReadSignal();

	/// <summary>The secret-store reads of the three commands the catalog lists. Another vault command is not recognized.</summary>
	[GeneratedRegex(
		@"(?<![\w\-$.])(?<anchor>Get-Secret|Get-StoredCredential|Get-AutomationPSCredential)(?![\w\-])",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SecretStoreReadSignal();

	/// <summary>
	/// A string argument that is a quoted literal, directly after the command, after <c>-String</c>, or after the plain-text switch and <c>-Force</c> in
	/// any order: a credential written into source is not flow evidence, so the conversion of a literal is excluded.
	/// </summary>
	private const string _LITERAL_SECURE_STRING_ARGUMENT =
		@"(?:[ \t]+(?:" + _PARAMETER_AS_PLAIN_TEXT + @"|" + _PARAMETER_FORCE + @"|" + _PARAMETER_STRING + @"[ \t]*:?))*[ \t]*:?[ \t]*(?:" + _QUOTE_OPENING + @"|@['""])";

	/// <summary>
	/// <c>ConvertTo-SecureString</c> with <c>-AsPlainText</c> (every prefix from <c>-A</c>) whose string argument is NOT a quoted literal: it arrives from a
	/// variable, an environment read or a call.
	/// </summary>
	[GeneratedRegex(
		@"(?<![\w\-$.])(?<anchor>ConvertTo-SecureString)(?![\w\-])(?!" + _LITERAL_SECURE_STRING_ARGUMENT + @")" + _COMMAND_WINDOW + _PARAMETER_AS_PLAIN_TEXT,
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PlainTextSecureStringSignal();
}

/// <summary>The `PowerShell` analyzer's <see cref="CandidateSelectionAndRecommendation.FlowSignals"/>.</summary>
internal static class PowerShellFlowSignalDeclarations
{
	private const string _GET_CREDENTIAL_CALL = "GetCredentialCall";
	private const string _READ_HOST_SECURE_INPUT = "ReadHostSecureInput";
	private const string _PROMPT_FOR_CREDENTIAL_CALL = "PromptForCredentialCall";
	private const string _READ_HOST_CREDENTIAL_PROMPT_TEXT = "ReadHostCredentialPromptText";
	private const string _DESKTOP_PASSWORD_CONTROL = "DesktopPasswordControl";
	private const string _ENVIRONMENT_CREDENTIAL_READ = "EnvironmentCredentialRead";
	private const string _IMPORT_CLIXML_READ = "ImportClixmlRead";
	private const string _SECRET_STORE_READ = "SecretStoreRead";
	private const string _PLAIN_TEXT_SECURE_STRING = "PlainTextSecureString";

	internal static ImmutableArray<FlowSignal> Build()
	{
		return [.. InteractiveUserSignals(), .. FixedCredentialSignals()];
	}

	private static ImmutableArray<FlowSignal> InteractiveUserSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(_GET_CREDENTIAL_CALL, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PowerShellPatternExpressions.GetCredentialCallSignal()),
			Signal(_READ_HOST_SECURE_INPUT, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PowerShellPatternExpressions.ReadHostSecureInputSignal()),
			Signal(_PROMPT_FOR_CREDENTIAL_CALL, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PowerShellPatternExpressions.PromptForCredentialCallSignal()),
			Signal(_READ_HOST_CREDENTIAL_PROMPT_TEXT, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, PowerShellPatternExpressions.ReadHostCredentialPromptTextSignal()),
			Signal(_DESKTOP_PASSWORD_CONTROL, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, PowerShellPatternExpressions.DesktopPasswordControlSignal())
		];

		return signals;
	}

	private static ImmutableArray<FlowSignal> FixedCredentialSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(_ENVIRONMENT_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, PowerShellPatternExpressions.EnvironmentCredentialReadSignal()),
			Signal(_IMPORT_CLIXML_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, PowerShellPatternExpressions.ImportClixmlReadSignal()),
			Signal(_SECRET_STORE_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, PowerShellPatternExpressions.SecretStoreReadSignal()),
			Signal(_PLAIN_TEXT_SECURE_STRING, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, PowerShellPatternExpressions.PlainTextSecureStringSignal())
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
