using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>The twenty flow signals of the `VBNet` catalog: eleven interactive-user and nine fixed-credential. They read the same vocabulary as the detection rules.</summary>
internal static partial class VBNetPatternExpressions
{
	// ------------------------------------------------------------------------------------------
	// Flow signals, CodeOnly or CodeOrStringBody as declared
	// ------------------------------------------------------------------------------------------

	[GeneratedRegex(@"\bConsole\.(?:Out\.)?Write(?:Line)?[ \t]*\([ \t]*" + _QUOTE + @"[^" + _QUOTE + @"\n]{0,80}?(?<anchor>pass(?:word|phrase)|secret|pin\b)[^" + _QUOTE + @"\n]{0,80}" + _QUOTE, OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex ConsolePasswordPromptText();

	[GeneratedRegex(@"(?<=(?<anchor>" + _CREDENTIAL_NAME + @")(?:[ \t]+As[ \t]+[A-Za-z0-9_.]+)?[ \t]*=[ \t]*)Console\.(?:In\.)?ReadLine[ \t]*\(\)", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex ConsoleReadLineIntoCredentialVariable();

	[GeneratedRegex(@"\bConsole\.(?<anchor>ReadKey)[ \t]*\([ \t]*(?:intercept:=[ \t]*)?True[ \t]*\)", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex ConsoleMaskedKeyRead();

	[GeneratedRegex(@"\b(?:Microsoft\.VisualBasic\.)?(?:Interaction\.)?(?<anchor>InputBox)[ \t]*\([ \t]*" + _QUOTE + @"[^" + _QUOTE + @"\n]{0,80}?(?:pass(?:word|phrase)|secret|pin\b|user[ ]?name)[^" + _QUOTE + @"\n]{0,80}" + _QUOTE, OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex InputBoxCredentialPrompt();

	[GeneratedRegex(@"\.(?<anchor>Password|SecurePassword)\b(?<=(?:\bAs[ \t]+PasswordBox|\bNew[ \t]+PasswordBox|\b(?:DirectCast|CType|TryCast)[ \t]*\([^\n()]{0,60}(?:\([^\n()]{0,20}\))?[^\n()]{0,20},[ \t]*PasswordBox[ \t]*\)|\bTypeOf[ \t]+[A-Za-z0-9_.()]{1,60}[ \t]+Is[ \t]+PasswordBox)" + _LINE_WINDOW_THREE + @"\.(?:Password|SecurePassword))", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex WpfPasswordBoxRead();

	[GeneratedRegex(@"\.?(?<anchor>UseSystemPasswordChar)[ \t]*=[ \t]*True|(?<![A-Za-z0-9_])(?<anchor>PasswordChar)[ \t]*=", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex WinFormsPasswordMasking();

	[GeneratedRegex(@"(?<anchor>Membership\.ValidateUser|FormsAuthentication\.Authenticate|SignInManager\.PasswordSignIn(?:Async)?)[ \t]*\(", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex PasswordSignInCall();

	[GeneratedRegex(@"(?<anchor>\bHandles[ \t]+[A-Za-z0-9_]+\.Authenticate)\b|\bHandles[ \t]+(?<anchor>[A-Za-z0-9_]+\.Click)" + _LINE_WINDOW_THREE + @"(?:" + _CREDENTIAL_NAME + @")\.Text\b", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex WebFormsLoginHandlerBinding();

	[GeneratedRegex(@"<(?<anchor>DataType)[ \t]*\([ \t]*(?:[A-Za-z0-9_.]*\.)?DataType\.Password[ \t]*\)>", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex PasswordDataTypeAttribute();

	[GeneratedRegex(_QUOTE + @"[^" + _QUOTE + @"\n]{0,200}?<input[^>" + _QUOTE + @"\n]{0,100}(?<anchor>type=(?:" + _QUOTE + _QUOTE + @"|')?password)", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex HtmlPasswordInputMarkup();

	[GeneratedRegex(@"\bRequest(?:\.Form)?[ \t]*\([ \t]*" + _QUOTE + @"(?<anchor>" + _CREDENTIAL_NAME + @")" + _QUOTE + @"[ \t]*\)", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex PostedFormCredentialFieldRead();

	[GeneratedRegex(@"(?<anchor>Configuration|Config|Section|Settings)[ \t]*\([ \t]*(?:" + _QUOTE + @"[^" + _QUOTE + @"\n]{0,60}" + _CREDENTIAL_NAME + @"[^" + _QUOTE + @"\n]{0,60}" + _QUOTE + @"|" + _CREDENTIAL_NAME + @")[ \t]*\)|(?<anchor>GetSection)[ \t]*" + _GROUP + @"[ \t]*\([ \t]*" + _QUOTE + @"[^" + _QUOTE + @"\n]{0,60}" + _CREDENTIAL_NAME + @"[^" + _QUOTE + @"\n]{0,60}" + _QUOTE + @"[ \t]*\)", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex ConfigurationIndexerCredentialRead();

	[GeneratedRegex(@"\bConfigurationManager\.(?<anchor>AppSettings|ConnectionStrings)(?:\.Get)?[ \t]*\([ \t]*" + _QUOTE + @"[^" + _QUOTE + @"\n]{0,60}" + _CREDENTIAL_NAME + @"[^" + _QUOTE + @"\n]{0,60}" + _QUOTE + @"[ \t]*\)", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex ConfigurationManagerCredentialRead();

	[GeneratedRegex(@"\bMy\.Settings\.(?<anchor>" + _CREDENTIAL_NAME + @")", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex MySettingsCredentialRead();

	[GeneratedRegex(@"\.(?:GetString|GetValue|GetInt32|Get)[ \t]*\([ \t]*(?:" + _QUOTE + @"[^" + _QUOTE + @"\n]{0,60}" + _QUOTE + @"|" + _NAME + @")[ \t]*,[ \t]*(?:" + _QUOTE + @"[^" + _QUOTE + @"\n]{0,60}(?<anchor>" + _CREDENTIAL_NAME + @")[^" + _QUOTE + @"\n]{0,60}" + _QUOTE + @"|(?<anchor>" + _CREDENTIAL_NAME + @"))[ \t]*\)", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex PlatformInstanceSettingCredentialRead();

	[GeneratedRegex(@"\bEnvironment\.(?<anchor>GetEnvironmentVariable)[ \t]*\([ \t]*" + _QUOTE + @"[^" + _QUOTE + @"\n]{0,60}" + _CREDENTIAL_NAME + @"[^" + _QUOTE + @"\n]{0,60}" + _QUOTE, OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex EnvironmentVariableCredentialRead();

	[GeneratedRegex(@"\bSecretClient\b" + _LINE_WINDOW_THREE + @"\.(?<anchor>GetSecret(?:Async)?)[ \t]*\(", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex SecretClientRead();

	[GeneratedRegex(@"(?:GetSecretStore[ \t]*\([ \t]*\)|SecretStore)\.(?<anchor>Get)[ \t]*\(", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex RelativitySecretStoreRead();

	[GeneratedRegex(@"\bProtectedData\.(?<anchor>Unprotect)[ \t]*\(", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex ProtectedDataUnprotectRead();

	[GeneratedRegex(@"\.(?<anchor>AddUserSecrets)(?:[ \t]*\(Of[ \t]+[A-Za-z0-9_.]+\))?[ \t]*\(", OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex UserSecretsConfigurationSource();
}

/// <summary>The `VBNet` analyzer's <see cref="CandidateSelectionAndRecommendation.FlowSignals"/>: eleven interactive-user signals and nine fixed-credential signals.</summary>
internal static class VBNetFlowSignalDeclarations
{
	internal static ImmutableArray<FlowSignal> Build()
	{
		return [.. InteractiveUserSignals(), .. FixedCredentialSignals()];
	}

	private static ImmutableArray<FlowSignal> InteractiveUserSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(nameof(VBNetPatternExpressions.ConsolePasswordPromptText), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, VBNetPatternExpressions.ConsolePasswordPromptText()),
			Signal(nameof(VBNetPatternExpressions.ConsoleReadLineIntoCredentialVariable), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, VBNetPatternExpressions.ConsoleReadLineIntoCredentialVariable()),
			Signal(nameof(VBNetPatternExpressions.ConsoleMaskedKeyRead), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, VBNetPatternExpressions.ConsoleMaskedKeyRead()),
			Signal(nameof(VBNetPatternExpressions.InputBoxCredentialPrompt), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, VBNetPatternExpressions.InputBoxCredentialPrompt()),
			Signal(nameof(VBNetPatternExpressions.WpfPasswordBoxRead), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, VBNetPatternExpressions.WpfPasswordBoxRead()),
			Signal(nameof(VBNetPatternExpressions.WinFormsPasswordMasking), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, VBNetPatternExpressions.WinFormsPasswordMasking()),
			Signal(nameof(VBNetPatternExpressions.PasswordSignInCall), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, VBNetPatternExpressions.PasswordSignInCall()),
			Signal(nameof(VBNetPatternExpressions.WebFormsLoginHandlerBinding), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, VBNetPatternExpressions.WebFormsLoginHandlerBinding()),
			Signal(nameof(VBNetPatternExpressions.PasswordDataTypeAttribute), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, VBNetPatternExpressions.PasswordDataTypeAttribute()),
			Signal(nameof(VBNetPatternExpressions.HtmlPasswordInputMarkup), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, VBNetPatternExpressions.HtmlPasswordInputMarkup()),
			Signal(nameof(VBNetPatternExpressions.PostedFormCredentialFieldRead), FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, VBNetPatternExpressions.PostedFormCredentialFieldRead())
		];

		return signals;
	}

	private static ImmutableArray<FlowSignal> FixedCredentialSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(nameof(VBNetPatternExpressions.ConfigurationIndexerCredentialRead), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, VBNetPatternExpressions.ConfigurationIndexerCredentialRead()),
			Signal(nameof(VBNetPatternExpressions.ConfigurationManagerCredentialRead), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, VBNetPatternExpressions.ConfigurationManagerCredentialRead()),
			Signal(nameof(VBNetPatternExpressions.MySettingsCredentialRead), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, VBNetPatternExpressions.MySettingsCredentialRead()),
			Signal(nameof(VBNetPatternExpressions.PlatformInstanceSettingCredentialRead), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOrStringBody, VBNetPatternExpressions.PlatformInstanceSettingCredentialRead()),
			Signal(nameof(VBNetPatternExpressions.EnvironmentVariableCredentialRead), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, VBNetPatternExpressions.EnvironmentVariableCredentialRead()),
			Signal(nameof(VBNetPatternExpressions.SecretClientRead), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, VBNetPatternExpressions.SecretClientRead()),
			Signal(nameof(VBNetPatternExpressions.RelativitySecretStoreRead), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, VBNetPatternExpressions.RelativitySecretStoreRead()),
			Signal(nameof(VBNetPatternExpressions.ProtectedDataUnprotectRead), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, VBNetPatternExpressions.ProtectedDataUnprotectRead()),
			Signal(nameof(VBNetPatternExpressions.UserSecretsConfigurationSource), FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, VBNetPatternExpressions.UserSecretsConfigurationSource())
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
