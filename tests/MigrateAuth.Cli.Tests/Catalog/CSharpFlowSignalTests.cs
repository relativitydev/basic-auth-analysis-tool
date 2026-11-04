using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `CSharp` ANALYZER'S SEVENTEEN FLOW SIGNALS, THROUGH THE REAL EMBEDDED
/// ANALYZER, THE REAL LEXICAL SCANNER AND THE REAL RECOGNITION ENGINE.
///
/// Each signal is evaluated ON ITS OWN - the analyzer is wrapped with that single signal - so a fixture
/// proves which signal fired rather than that some signal did. Every fixture is invented, from the
/// catalog's own prose, with generic names only.
/// </summary>
public sealed class CSharpFlowSignalTests
{
	private const string _NEWLINE = "\n";
	private const string _HIGH_CONSTRUCT = "var result = client.WithBasicAuth(operatorName, accessKey);";
	private const string _LINE_COMMENT_PREFIX = "// ";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";
	private const string _ANCHOR_GROUP_NAME = "anchor";
	private const string _UNTERMINATED_BLOCK_COMMENT = "/* never closes";
	private const int _INTERACTIVE_SIGNAL_COUNT = 10;
	private const int _FIXED_SIGNAL_COUNT = 7;
	private const int _MAXIMUM_MATCH_ENUMERATION = 100_000;

	private const string _CONSOLE_PASSWORD_PROMPT_TEXT = "ConsolePasswordPromptText";
	private const string _CONSOLE_READ_LINE = "ConsoleReadLineIntoCredentialVariable";
	private const string _CONSOLE_MASKED_KEY_READ = "ConsoleMaskedKeyRead";
	private const string _WPF_PASSWORD_BOX = "WpfPasswordBoxRead";
	private const string _WIN_FORMS_MASKING = "WinFormsPasswordMasking";
	private const string _PASSWORD_SIGN_IN_CALL = "PasswordSignInCall";
	private const string _POSTED_LOGIN_HANDLER = "PostedLoginHandlerBinding";
	private const string _PASSWORD_DATA_TYPE = "PasswordDataTypeAttribute";
	private const string _HTML_PASSWORD_INPUT = "HtmlPasswordInputMarkup";
	private const string _POSTED_FORM_FIELD = "PostedFormCredentialFieldRead";
	private const string _CONFIGURATION_INDEXER = "ConfigurationIndexerCredentialRead";
	private const string _CONFIGURATION_MANAGER = "ConfigurationManagerCredentialRead";
	private const string _PLATFORM_INSTANCE_SETTING = "PlatformInstanceSettingCredentialRead";
	private const string _ENVIRONMENT_VARIABLE = "EnvironmentVariableCredentialRead";
	private const string _SECRET_CLIENT = "SecretClientRead";
	private const string _PROTECTED_DATA = "ProtectedDataUnprotectRead";
	private const string _USER_SECRETS = "UserSecretsConfigurationSource";

	/// <summary>Filler lines between a stored cast and the read of its variable: one inside the 400-character window and one past it.</summary>
	private static readonly string _WPF_FILLER_INSIDE_THE_WINDOW = string.Join(_NEWLINE, Enumerable.Repeat("var total = first + second;", 5));

	private static readonly string _WPF_FILLER_BEYOND_THE_WINDOW = string.Join(_NEWLINE, Enumerable.Repeat("var total = first + second;", 20));

	/// <summary>The longest identifier chain the cast operand accepts, 81 characters.</summary>
	private static readonly string _WPF_LONGEST_IDENTIFIER_CHAIN = "a" + new string('b', 80);

	/// <summary>The longest argument text a suffix accepts is 120 characters: a quoted literal of 118 characters and its two quotes.</summary>
	private static readonly string _WPF_LONGEST_ARGUMENT_LITERAL_BODY = new string('c', 118);

	private static readonly ImmutableArray<(string Name, FlowSignalKind Kind, FlowSignalZone Zone)> _declaredTable =
	[
		(_CONSOLE_PASSWORD_PROMPT_TEXT, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		(_CONSOLE_READ_LINE, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		(_CONSOLE_MASKED_KEY_READ, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		(_WPF_PASSWORD_BOX, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		(_WIN_FORMS_MASKING, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		(_PASSWORD_SIGN_IN_CALL, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		(_POSTED_LOGIN_HANDLER, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		(_PASSWORD_DATA_TYPE, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly),
		(_HTML_PASSWORD_INPUT, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		(_POSTED_FORM_FIELD, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody),
		(_CONFIGURATION_INDEXER, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		(_CONFIGURATION_MANAGER, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		(_PLATFORM_INSTANCE_SETTING, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOrStringBody),
		(_ENVIRONMENT_VARIABLE, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		(_SECRET_CLIENT, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		(_PROTECTED_DATA, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly),
		(_USER_SECRETS, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly)
	];

	/// <summary>One positive per signal, and the further shapes the catalog names; the set of signals each one fires.</summary>
	private static readonly ImmutableArray<SignalFixture> _positiveFixtures =
	[
		new("prompt-write", "Console.Write(\"Enter your password: \");", [_CONSOLE_PASSWORD_PROMPT_TEXT]),
		new("prompt-out-writeline-passphrase", "Console.Out.WriteLine(\"Passphrase:\");", [_CONSOLE_PASSWORD_PROMPT_TEXT]),
		new("prompt-pin", "Console.Write(\"Type the PIN and press enter\");", [_CONSOLE_PASSWORD_PROMPT_TEXT]),
		new("readline-into-password", "string userPassword = Console.ReadLine();", [_CONSOLE_READ_LINE]),
		new("readline-in-into-secret-field", "this.secretValue = Console.In.ReadLine();", [_CONSOLE_READ_LINE]),
		new("readline-into-pin-token", "var accountPin = Console.ReadLine();", [_CONSOLE_READ_LINE]),
		new(
			"masked-key-shape-a-name-after",
			Lines("var pressed = Console.ReadKey(true);", "password += pressed.KeyChar;"),
			[_CONSOLE_MASKED_KEY_READ]),
		new(
			"masked-key-shape-a-name-before",
			Lines("var enteredSecret = string.Empty;", "var pressed = Console.ReadKey(true);"),
			[_CONSOLE_MASKED_KEY_READ]),
		new(
			"masked-key-shape-b-loop-without-a-credential-name",
			Lines(
				"ConsoleKeyInfo key;",
				"var buffer = string.Empty;",
				"while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)",
				"{",
				"\tbuffer += key.KeyChar;",
				"}"),
			[_CONSOLE_MASKED_KEY_READ]),
		new(
			"masked-key-shape-b-comparison-before-the-call",
			Lines(
				"while (key.Key != ConsoleKey.Enter)",
				"{",
				"\tkey = Console.ReadKey(true);",
				"\tbuilder.Append(key.KeyChar);",
				"}"),
			[_CONSOLE_MASKED_KEY_READ]),
		new("wpf-read", "var entered = PasswordBox.Password;", [_WPF_PASSWORD_BOX]),
		new("wpf-declaration", "PasswordBox entry = new PasswordBox();", [_WPF_PASSWORD_BOX]),
		new("wpf-cast-read-directly", "var entered = ((PasswordBox)sender).Password;", [_WPF_PASSWORD_BOX]),
		new("wpf-cast-secure-read-directly", "var entered = ((PasswordBox)sender).SecurePassword;", [_WPF_PASSWORD_BOX]),
		new("wpf-as-read-directly", "var entered = (sender as PasswordBox).Password;", [_WPF_PASSWORD_BOX]),
		new("wpf-cast-stored-then-read", Lines("var box = (PasswordBox)sender;", "var entered = box.Password;"), [_WPF_PASSWORD_BOX]),
		new("wpf-as-stored-then-secure-read", Lines("var box = sender as PasswordBox;", "Submit(box.SecurePassword);"), [_WPF_PASSWORD_BOX]),
		new(
			"wpf-cast-stored-then-read-inside-the-window",
			Lines("var box = (PasswordBox)sender;", _WPF_FILLER_INSIDE_THE_WINDOW, "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-as-call-stored-then-read",
			Lines("var box = FindName(\"PasswordInput\") as PasswordBox;", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-as-call-stored-then-read-inside-the-window",
			Lines("var box = FindName(\"PasswordInput\") as PasswordBox;", _WPF_FILLER_INSIDE_THE_WINDOW, "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-as-indexer-stored-then-secure-read",
			Lines("var box = controls[\"entry\"] as PasswordBox;", "Submit(box.SecurePassword);"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-as-chain-and-call-stored-then-read",
			Lines("var box = this.Root.FindName(\"PasswordInput\") as PasswordBox;", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-as-call-then-member-stored-then-read",
			Lines("var box = Locate(\"PasswordInput\").Control as PasswordBox;", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new("wpf-as-call-read-directly", "var entered = (FindName(\"PasswordInput\") as PasswordBox).Password;", [_WPF_PASSWORD_BOX]),
		new("wpf-cast-call-read-directly", "var entered = ((PasswordBox)FindName(\"PasswordInput\")).Password;", [_WPF_PASSWORD_BOX]),
		new("wpf-cast-indexer-read-directly", "var entered = ((PasswordBox)controls[\"entry\"]).SecurePassword;", [_WPF_PASSWORD_BOX]),
		new(
			"wpf-cast-call-stored-then-read",
			Lines("var box = (PasswordBox)FindName(\"PasswordInput\");", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-operand-identifier-chain-of-the-longest-length-stored-as-then-read",
			Lines($"var box = {_WPF_LONGEST_IDENTIFIER_CHAIN} as PasswordBox;", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-operand-with-the-most-suffixes-each-followed-by-a-member-chain-stored-as-then-read",
			Lines("var box = Root.Find(\"a\").Child.Get(1).Item[2].Next(3).Control as PasswordBox;", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-operand-with-the-longest-argument-text-stored-as-then-read",
			Lines($"var box = Find(\"{_WPF_LONGEST_ARGUMENT_LITERAL_BODY}\") as PasswordBox;", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-operand-with-the-most-suffixes-direct-cast-read",
			"var entered = ((PasswordBox)Root.Find(\"a\").Child.Get(1).Item[2].Next(3).Control).Password;",
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-direct-as-accepts-an-operand-with-a-nested-call",
			"var entered = (Find(Get(\"x\")) as PasswordBox).Password;",
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-direct-as-accepts-a-constructed-operand",
			"var entered = (new Factory() as PasswordBox).Password;",
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-direct-as-accepts-a-parenthesized-operand",
			"var entered = ((expression) as PasswordBox).SecurePassword;",
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-stored-cast-accepts-an-operand-with-a-nested-call",
			Lines("var box = (PasswordBox)Find(Get(\"x\"));", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-stored-cast-accepts-a-constructed-operand",
			Lines("var box = (PasswordBox)new Factory();", "var entered = box.Password;"),
			[_WPF_PASSWORD_BOX]),
		new(
			"wpf-stored-cast-accepts-a-parenthesized-operand",
			Lines("var box = (PasswordBox)(expression);", "var entered = box.SecurePassword;"),
			[_WPF_PASSWORD_BOX]),
		new("winforms-system-char", "field.UseSystemPasswordChar = true;", [_WIN_FORMS_MASKING]),
		new("winforms-password-char", "field.PasswordChar = '*';", [_WIN_FORMS_MASKING]),
		new("sign-in-call", "var result = await signInManager.PasswordSignInAsync(user, entered, false, false);", [_PASSWORD_SIGN_IN_CALL]),
		new("check-password-call", "var valid = await userManager.CheckPasswordAsync(user, entered);", [_PASSWORD_SIGN_IN_CALL]),
		new(
			"posted-handler",
			Lines("[HttpPost]", "public IActionResult SignInUser(SignInInput input)", "{", "}"),
			[_POSTED_LOGIN_HANDLER]),
		new(
			"posted-handler-with-arguments-and-other-attributes",
			Lines("[HttpPost(\"account\")]", "[ValidateAntiForgeryToken]", "public async Task<IActionResult> LogOn(LogOnInput input)", "{", "}"),
			[_POSTED_LOGIN_HANDLER]),
		new("data-type-attribute", Lines("[DataType(DataType.Password)]", "public string Entry { get; set; }"), [_PASSWORD_DATA_TYPE]),
		new(
			"data-type-attribute-qualified",
			Lines("[System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]", "public string Entry { get; set; }"),
			[_PASSWORD_DATA_TYPE]),
		new("html-markup-escaped-quotes", "var markup = \"<input type=\\\"password\\\" name=\\\"entry\\\" />\";", [_HTML_PASSWORD_INPUT]),
		new("html-markup-single-quotes", "var markup = \"<input type='password' name='entry' />\";", [_HTML_PASSWORD_INPUT]),
		new("html-markup-verbatim", "var markup = @\"<input type=\"\"password\"\" />\";", [_HTML_PASSWORD_INPUT]),
		new("form-request-form", "var entry = Request.Form[\"Password\"];", [_POSTED_FORM_FIELD]),
		new("form-collection-pwd-token", "var entry = formCollection[\"Db_Pwd\"];", [_POSTED_FORM_FIELD]),
		new("configuration-indexer-pass-token", "var value = configuration[\"Db:Pass\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-api-key-camel", "var value = configuration[\"Section:ApiKey\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-api-key-upper", "var value = configuration[\"API_KEY\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-api-then-key", "var value = configuration[\"Auth:Api:Key\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-camel-pass", "var value = configuration[\"SmtpPass\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-secret-substring", "var value = configuration[\"Service:ClientSecretValue\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-token-substring", "var value = configuration?[\"Service:AccessToken\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-get-value", "var value = configuration.GetValue<string>(\"Db:Pwd\");", [_CONFIGURATION_INDEXER]),
		new("configuration-get-section", "var section = configuration.GetSection(\"Auth:Credentials\");", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-field-receiver", "var value = _config[\"Db:Pwd\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-property-receiver", "var value = this.Configuration[\"Auth:ClientSecret\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-section-receiver", "var value = authSection[\"Password\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-get-section-receiver", "var value = configuration.GetSection(\"Auth\")[\"Password\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-indexer-null-conditional-receiver", "var value = appConfiguration?[\"Auth:AccessToken\"];", [_CONFIGURATION_INDEXER]),
		new("configuration-get-value-with-a-type-argument-on-any-receiver", "var value = platformValues.GetValue<string>(\"Db:Pwd\");", [_CONFIGURATION_INDEXER]),
		new("configuration-get-value-without-a-type-argument-on-a-configuration-receiver", "var value = Configuration.GetValue(\"Db:Pwd\");", [_CONFIGURATION_INDEXER]),
		new("configuration-get-section-on-any-receiver", "var section = platformValues.GetSection(\"Auth:Credentials\");", [_CONFIGURATION_INDEXER]),
		new(
			"configuration-manager-app-settings",
			"var value = ConfigurationManager.AppSettings[\"SmtpPass\"];",
			[_CONFIGURATION_MANAGER]),
		new(
			"configuration-manager-connection-strings",
			"var value = ConfigurationManager.ConnectionStrings[\"Db:Pwd\"];",
			[_CONFIGURATION_MANAGER]),
		new("configuration-manager-getter", "var value = ConfigurationManager.AppSettings.Get(\"ApiKey\");", [_CONFIGURATION_MANAGER]),
		new(
			"instance-setting-by-section-and-name",
			"var value = await instanceSettings.GetStringAsync(\"Section\", \"ServiceSecret\");",
			[_PLATFORM_INSTANCE_SETTING]),
		new("environment-variable", "var value = Environment.GetEnvironmentVariable(\"DB_PWD\");", [_ENVIRONMENT_VARIABLE]),
		new(
			"secret-client",
			Lines("var client = new SecretClient(vaultUri, vaultAccess);", "var fetched = await client.GetSecretAsync(\"name\");"),
			[_SECRET_CLIENT]),
		new("protected-data", "var bytes = ProtectedData.Unprotect(stored, null, DataProtectionScope.CurrentUser);", [_PROTECTED_DATA]),
		new("user-secrets", "builder.Configuration.AddUserSecrets<Program>();", [_USER_SECRETS])
	];

	/// <summary>Constructs that resemble a signal's evidence and must fire nothing, or only what is stated.</summary>
	private static readonly ImmutableArray<SignalFixture> _negativeFixtures =
	[
		new("prompt-without-a-credential-word", "Console.Write(\"Press any key to continue\");", []),
		new("readline-into-a-plain-name", "string customerName = Console.ReadLine();", []),
		new("readline-into-a-name-containing-pin", "var spinner = Console.ReadLine();", []),
		new(
			"masked-key-menu",
			Lines("var choice = Console.ReadKey(true);", "if (choice.Key == ConsoleKey.Escape)", "{", "}"),
			[]),
		new("masked-key-unmasked-call", "var pressed = Console.ReadKey();", []),
		new("wpf-type-only-in-a-typeof", "var kind = typeof(PasswordBox);", []),
		new("wpf-cast-without-a-password-read", "((PasswordBox)sender).Focus();", []),
		new("wpf-cast-stored-without-a-password-read", Lines("var box = (PasswordBox)sender;", "box.Focus();"), []),
		new("wpf-cast-to-another-control-type", "var entered = ((TextBox)sender).Password;", []),
		new("wpf-as-another-control-type-stored", Lines("var box = sender as TextBox;", "var entered = box.Password;"), []),
		new("wpf-type-name-merely-beginning-with-the-type", "var entered = ((PasswordBoxHelper)sender).Password;", []),
		new("wpf-stored-type-name-merely-beginning-with-the-type", Lines("var box = (PasswordBoxHelper)sender;", "var entered = box.Password;"), []),
		new("wpf-as-type-name-merely-beginning-with-the-type", Lines("var box = sender as PasswordBoxHelper;", "var entered = box.Password;"), []),
		new("wpf-read-on-a-different-variable", Lines("var box = (PasswordBox)sender;", "var entered = other.Password;"), []),
		new("wpf-read-on-a-variable-ending-in-the-bound-name", Lines("var box = (PasswordBox)sender;", "var entered = outerbox.Password;"), []),
		new("wpf-read-on-a-member-of-another-object-with-the-bound-name", Lines("var box = (PasswordBox)sender;", "var entered = holder.box.Password;"), []),
		new("wpf-read-beyond-the-window", Lines("var box = (PasswordBox)sender;", _WPF_FILLER_BEYOND_THE_WINDOW, "var entered = box.Password;"), []),
		new("wpf-cast-inside-a-string", "var text = \"var box = (PasswordBox)sender; var entered = box.Password;\";", []),
		new("wpf-direct-cast-inside-a-string", "var text = \"((PasswordBox)sender).Password\";", []),
		new(
			"wpf-as-call-stored-without-a-password-read",
			Lines("var box = FindName(\"PasswordInput\") as PasswordBox;", "box.Focus();"),
			[]),
		new(
			"wpf-as-call-read-on-a-different-variable",
			Lines("var box = FindName(\"PasswordInput\") as PasswordBox;", "var entered = other.Password;"),
			[]),
		new(
			"wpf-as-call-type-name-merely-beginning-with-the-type",
			Lines("var box = FindName(\"PasswordInput\") as PasswordBoxHelper;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-as-call-another-control-type",
			Lines("var box = FindName(\"PasswordInput\") as TextBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-as-call-read-beyond-the-window",
			Lines("var box = FindName(\"PasswordInput\") as PasswordBox;", _WPF_FILLER_BEYOND_THE_WINDOW, "var entered = box.Password;"),
			[]),
		new(
			"wpf-as-call-inside-a-string",
			"var text = \"var box = FindName(\\\"PasswordInput\\\") as PasswordBox; var entered = box.Password;\";",
			[]),
		new("wpf-cast-call-without-a-password-read", "((PasswordBox)FindName(\"PasswordInput\")).Focus();", []),
		new("wpf-cast-call-to-another-control-type", "var entered = ((TextBox)FindName(\"PasswordInput\")).Password;", []),
		new(
			"wpf-declared-operand-a-call-with-a-nested-call-stored-as-then-read",
			Lines("var box = Find(Get(\"x\")) as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-declared-operand-a-call-with-a-nested-name-of-call-stored-as-then-read",
			Lines("var box = FindName(nameof(entry)) as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-declared-operand-a-constructed-object-stored-as-then-read",
			Lines("var box = new Factory() as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-declared-operand-a-parenthesized-expression-stored-as-then-read",
			Lines("var box = (expression) as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-declared-operand-a-call-with-a-nested-call-in-a-direct-cast",
			"var entered = ((PasswordBox)Find(Get(\"x\"))).Password;",
			[]),
		new(
			"wpf-declared-operand-a-constructed-object-in-a-direct-cast",
			"var entered = ((PasswordBox)new Factory()).Password;",
			[]),
		new(
			"wpf-operand-identifier-chain-one-character-over-the-longest-length",
			Lines($"var box = {_WPF_LONGEST_IDENTIFIER_CHAIN}x as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-operand-one-suffix-over-the-most-suffixes",
			Lines("var box = Root.Find(\"a\").Child.Get(1).Item[2].Next(3).More(4).Control as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-operand-argument-text-one-character-over-the-longest-length",
			Lines($"var box = Find(\"{_WPF_LONGEST_ARGUMENT_LITERAL_BODY}x\") as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-operand-argument-text-spread-over-two-lines",
			Lines("var box = Find(\"first", "second\") as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-operand-argument-text-holding-a-statement-end",
			Lines("var box = Find(\"a;b\") as PasswordBox;", "var entered = box.Password;"),
			[]),
		new(
			"wpf-as-on-a-later-statement-does-not-bind-the-earlier-variable",
			Lines("var box = Build(); var other = source as PasswordBox;", "var entered = box.Password;"),
			[]),
		new("winforms-comparison-is-not-an-assignment", "var masked = field.PasswordChar == '*';", []),
		new("winforms-false-is-not-masking", "field.UseSystemPasswordChar = false;", []),
		new("sign-in-method-declaration", "Task<SignInResult> PasswordSignInAsync(string user, string entered);", []),
		new("posted-action-with-a-plain-name", Lines("[HttpPost]", "public IActionResult Save(RecordInput input)", "{", "}"), []),
		new("data-type-of-another-kind", "[DataType(DataType.EmailAddress)]", []),
		new("markup-of-another-input-type", "var markup = \"<input type=\\\"text\\\" name=\\\"entry\\\" />\";", []),
		new("code-assignment-of-a-type-variable", "var type = \"password\";", []),
		new("anti-forgery-token-is-not-a-form-field-read", "var value = Request.Form[\"__RequestVerificationToken\"];", []),
		new("configuration-key-containing-pass-as-a-suffix", "var value = configuration[\"Bypass\"];", []),
		new("configuration-key-containing-pass-in-the-middle", "var value = configuration[\"Compass\"];", []),
		new("configuration-key-passage", "var value = configuration[\"Passage\"];", []),
		new("configuration-manager-key-compass", "var value = ConfigurationManager.AppSettings.Get(\"Compass\");", []),
		new("environment-variable-bypass-mode", "var value = Environment.GetEnvironmentVariable(\"BYPASS_MODE\");", []),
		new("environment-variable-without-a-credential-word", "var value = Environment.GetEnvironmentVariable(\"HOME_FOLDER\");", []),
		new("instance-setting-without-a-credential-word", "var value = await instanceSettings.GetStringAsync(\"Section\", \"PageSize\");", []),
		new("secret-fetch-without-a-secret-client", "var fetched = await store.GetSecretAsync(\"name\");", []),
		new("literal-credential-is-not-flow-evidence", "var credentials = new NetworkCredential(\"operator\", \"a-literal-value\");", []),
		new("header-indexer-keyed-by-a-credential-word", "var value = Request.Headers[\"token\"];", []),
		new("session-indexer-keyed-by-a-credential-word", "var value = Session[\"password\"];", []),
		new("dictionary-indexer-keyed-by-a-credential-word", "var value = values[\"token\"];", []),
		new("query-indexer-keyed-by-a-credential-word", "var value = Request.Query[\"ApiKey\"];", []),
		new("cookie-indexer-keyed-by-a-credential-word", "var value = Request.Cookies[\"secret\"];", []),
		new("chained-indexer-keyed-by-a-credential-word", "var value = lookup[\"Auth\"][\"Password\"];", []),
		new("get-value-without-a-type-argument-on-a-json-receiver", "var value = payload.GetValue(\"token\");", [])
	];

	public static IEnumerable<object[]> PositiveLabels()
	{
		return _positiveFixtures.Select(fixture => new object[] { fixture.Label });
	}

	public static IEnumerable<object[]> NegativeLabels()
	{
		return _negativeFixtures.Select(fixture => new object[] { fixture.Label });
	}

	// ---- (a): each signal matches its construct, and only the signals a fixture names ----

	[Fact]
	public void EverySignalHasAPositiveFixtureThatFiresIt()
	{
		HashSet<string> covered = [.. _positiveFixtures.SelectMany(fixture => fixture.ExpectedSignals)];

		Assert.Equal(_declaredTable.Length, _declaredTable.Count(entry => covered.Contains(entry.Name)));
	}

	[Theory]
	[MemberData(nameof(PositiveLabels))]
	public void APositiveFixtureFiresExactlyTheSignalsItNames(string label)
	{
		SignalFixture fixture = _positiveFixtures.Single(candidate => candidate.Label == label);

		ImmutableArray<string> firing = FiringSignalNames(fixture.Source);

		AssertSameSignals(fixture.ExpectedSignals, firing);
	}

	[Theory]
	[MemberData(nameof(NegativeLabels))]
	public void ANegativeFixtureFiresOnlyWhatItStates(string label)
	{
		SignalFixture fixture = _negativeFixtures.Single(candidate => candidate.Label == label);

		ImmutableArray<string> firing = FiringSignalNames(fixture.Source);

		AssertSameSignals(fixture.ExpectedSignals, firing);
	}

	// ---- the receiver rule of the configuration indexer ----

	[Theory]
	[InlineData("configuration")]
	[InlineData("Configuration")]
	[InlineData("_configuration")]
	[InlineData("hostConfiguration")]
	[InlineData("config")]
	[InlineData("_config")]
	[InlineData("appConfig")]
	[InlineData("section")]
	[InlineData("Section")]
	[InlineData("authSection")]
	[InlineData("this.Configuration")]
	[InlineData("builder.Configuration")]
	[InlineData("configuration.GetSection(\"Auth\")")]
	[InlineData("configuration?")]
	public void AnIndexerOnAConfigurationShapedReceiverKeyedByACredentialFiresTheIndexerSignal(string receiver)
	{
		ImmutableArray<string> firing = FiringSignalNames($"var value = {receiver}[\"Db:Password\"];");

		Assert.Equal(new[] { _CONFIGURATION_INDEXER }, firing.ToArray());
	}

	[Theory]
	[InlineData("Request.Headers")]
	[InlineData("Session")]
	[InlineData("values")]
	[InlineData("dictionary")]
	[InlineData("Request.Query")]
	[InlineData("Request.Cookies")]
	[InlineData("HttpContext.Items")]
	[InlineData("claims")]
	[InlineData("row")]
	[InlineData("Properties")]
	[InlineData("settingsValues")]
	public void AnIndexerOnAnotherKindOfReceiverKeyedByACredentialDoesNotFireTheIndexerSignal(string receiver)
	{
		ImmutableArray<string> firing = FiringSignalNames($"var value = {receiver}[\"Db:Password\"];");

		Assert.DoesNotContain(_CONFIGURATION_INDEXER, firing);
	}

	[Theory]
	[InlineData("configuration.GetValue<string>(\"Db:Password\")")]
	[InlineData("options.GetValue<string>(\"Db:Password\")")]
	[InlineData("configuration.GetSection(\"Db:Password\")")]
	[InlineData("options.GetSection(\"Db:Password\")")]
	[InlineData("configuration.GetValue(\"Db:Password\")")]
	[InlineData("_config?.GetValue(\"Db:Password\")")]
	public void AConfigurationMethodReadKeyedByACredentialFiresTheIndexerSignal(string read)
	{
		ImmutableArray<string> firing = FiringSignalNames($"var value = {read};");

		Assert.Equal(new[] { _CONFIGURATION_INDEXER }, firing.ToArray());
	}

	[Theory]
	[InlineData("payload.GetValue(\"Db:Password\")")]
	[InlineData("row.GetValue(\"Db:Password\")")]
	public void AGetValueWithNoTypeArgumentOnAnotherKindOfReceiverDoesNotFireTheIndexerSignal(string read)
	{
		ImmutableArray<string> firing = FiringSignalNames($"var value = {read};");

		Assert.DoesNotContain(_CONFIGURATION_INDEXER, firing);
	}

	// ---- the whole-token rule ----

	[Theory]
	[InlineData("Db:Pass")]
	[InlineData("DB_PWD")]
	[InlineData("ApiKey")]
	[InlineData("API_KEY")]
	[InlineData("api-key")]
	[InlineData("SmtpPass")]
	[InlineData("db.pass")]
	[InlineData("MyPasswordHere")]
	[InlineData("ClientSECRET")]
	[InlineData("Credentials")]
	[InlineData("AccessToken")]
	public void AConfigurationKeyNamingACredentialFiresTheIndexerSignal(string key)
	{
		ImmutableArray<string> firing = FiringSignalNames($"var value = configuration[\"{key}\"];");

		Assert.Equal(new[] { _CONFIGURATION_INDEXER }, firing.ToArray());
	}

	[Theory]
	[InlineData("Bypass")]
	[InlineData("Compass")]
	[InlineData("Passage")]
	[InlineData("BYPASS_MODE")]
	[InlineData("Keyboard")]
	[InlineData("Apikeys")]
	[InlineData("Pass2")]
	public void AConfigurationKeyThatOnlyContainsAShortWordFiresNothing(string key)
	{
		ImmutableArray<string> firing = FiringSignalNames($"var value = configuration[\"{key}\"];");

		Assert.Empty(firing);
	}

	[Theory]
	[InlineData("ConfirmPassword")]
	[InlineData("pwd")]
	[InlineData("Db_Pass")]
	public void APostedFormKeyNamingAPasswordFieldFiresTheFormSignal(string key)
	{
		ImmutableArray<string> firing = FiringSignalNames($"var entry = Request.Form[\"{key}\"];");

		Assert.Contains(_POSTED_FORM_FIELD, firing);
	}

	[Theory]
	[InlineData("__RequestVerificationToken")]
	[InlineData("Bypass")]
	[InlineData("ClientSecret")]
	[InlineData("ApiKey")]
	public void APostedFormKeyThatIsNotAPasswordFieldDoesNotFireTheFormSignal(string key)
	{
		ImmutableArray<string> firing = FiringSignalNames($"var entry = Request.Form[\"{key}\"];");

		Assert.DoesNotContain(_POSTED_FORM_FIELD, firing);
	}

	// ---- (b): nothing fires inside a comment ----

	[Theory]
	[MemberData(nameof(PositiveLabels))]
	public void NoSignalFiresInsideALineComment(string label)
	{
		SignalFixture fixture = _positiveFixtures.Single(candidate => candidate.Label == label);
		string commented = string.Join(_NEWLINE, fixture.Source.Split(_NEWLINE).Select(line => _LINE_COMMENT_PREFIX + line));

		Assert.Empty(FiringSignalNames(commented));
	}

	[Theory]
	[MemberData(nameof(PositiveLabels))]
	public void NoSignalFiresInsideABlockComment(string label)
	{
		SignalFixture fixture = _positiveFixtures.Single(candidate => candidate.Label == label);
		string commented = _BLOCK_COMMENT_OPENING + _NEWLINE + fixture.Source + _NEWLINE + _BLOCK_COMMENT_CLOSING;

		Assert.Empty(FiringSignalNames(commented));
	}

	// ---- (c): CodeOrStringBody fires on a prompt's literal, CodeOnly does not ----

	[Fact]
	public void ACodeOrStringBodySignalFiresOnAPromptLiteralWhileCodeOnlySignalsDoNotFireOnTextInsideAString()
	{
		string prompt = "Console.Write(\"Enter your password: \");";
		string quotedCalls = "var help = \"call signInManager.PasswordSignInAsync(user, entered) or Environment.GetEnvironmentVariable(\\\"DB_PWD\\\")\";";

		Assert.Equal(new[] { _CONSOLE_PASSWORD_PROMPT_TEXT }, FiringSignalNames(prompt).ToArray());
		Assert.Empty(FiringSignalNames(quotedCalls));
	}

	[Fact]
	public void ACodeOnlySignalDoesNotFireOnATypeNameQuotedInAString()
	{
		string quotedTypeName = "var kind = \"PasswordBox.Password\";";

		Assert.Empty(FiringSignalNames(quotedTypeName));
	}

	// ---- (d): a signal past a truncated mask's valid extent does not fire ----

	[Fact]
	public void ASignalAnchoredPastATruncatedMasksValidExtentDoesNotFire()
	{
		string signals = Lines("var entered = PasswordBox.Password;", "Console.Write(\"Enter your password: \");");
		string truncated = Lines(_HIGH_CONSTRUCT, _UNTERMINATED_BLOCK_COMMENT, signals);
		string complete = Lines(_HIGH_CONSTRUCT, signals);
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext truncatedContext = harness.ContextFor(truncated);
		FileRecognitionContext completeContext = harness.ContextFor(complete);
		int signalPosition = truncated.IndexOf("PasswordBox", StringComparison.Ordinal);

		Assert.False(truncatedContext.Mask.IsCompletelyValid);
		Assert.True(signalPosition >= truncatedContext.Mask.ValidThroughPosition);
		Assert.False(truncatedContext.Mask.IsTrustworthyAt(signalPosition));

		RecognitionResult afterTheBreak = Recognize(harness, truncatedContext);
		RecognitionResult withoutABreak = Recognize(harness, completeContext);

		Assert.NotEmpty(afterTheBreak.Constructs);
		Assert.Equal(FlowEvidence.None, afterTheBreak.FlowEvidence);
		Assert.Equal(FlowEvidence.Interactive, withoutABreak.FlowEvidence);
	}

	// ---- the cast and as shapes of the password control signal ----

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfTheChangedPasswordControlSignalAgreeOverTheNewShapesAndNearMisses()
	{
		ImmutableArray<SignalFixture> wpfFixtures =
			[.. _positiveFixtures.Concat(_negativeFixtures).Where(fixture => fixture.Label.StartsWith("wpf-", StringComparison.Ordinal))];
		FlowSignal signal = ReadSignals().Single(candidate => candidate.Name == _WPF_PASSWORD_BOX);
		System.Text.RegularExpressions.Regex interpreted = new(signal.Expression.ToString(), signal.Expression.Options, System.Text.RegularExpressions.Regex.InfiniteMatchTimeout);
		string text = Lines([.. wpfFixtures.Select(fixture => fixture.Source)]);

		ImmutableArray<(int Index, int Length)> expected = MatchSummary(interpreted, text);

		Assert.NotEmpty(expected);
		Assert.True(expected.SequenceEqual(MatchSummary(signal.Expression, text)));

		foreach (SignalFixture fixture in wpfFixtures)
		{
			Assert.True(
				MatchSummary(interpreted, fixture.Source).SequenceEqual(MatchSummary(signal.Expression, fixture.Source)),
				fixture.Label);
		}
	}

	[Fact]
	public void AStoredPasswordBoxLocatedByACallMakesAHighFindingAnInteractiveSignalFlow()
	{
		string source = Lines(
			_HIGH_CONSTRUCT,
			"var box = FindName(\"PasswordInput\") as PasswordBox;",
			"var entered = box.Password;");
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();

		harness.WriteSourceFile("Program.cs", source);
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		Finding finding = Assert.Single(harness.State.Findings);
		Assert.Equal(CredentialFlow.AuthorizationCode, finding.Flow);
		Assert.Equal(FlowDecision.InteractiveSignal, finding.FlowDecision);
	}

	private static ImmutableArray<(int Index, int Length)> MatchSummary(System.Text.RegularExpressions.Regex expression, string text)
	{
		List<(int Index, int Length)> summary = [];
		System.Text.RegularExpressions.Match match = expression.Match(text);

		while (match.Success && summary.Count < _MAXIMUM_MATCH_ENUMERATION)
		{
			summary.Add((match.Index, match.Length));
			match = match.NextMatch();
		}

		return [.. summary];
	}

	// ---- the contract ----

	[Fact]
	public void TheAnalyzerDeclaresTheSeventeenSignalsOfTheCatalogWithTheirKindsAndZones()
	{
		ImmutableArray<FlowSignal> signals = ReadSignals();

		Assert.Equal(_declaredTable.Length, signals.Length);
		Assert.Equal(_INTERACTIVE_SIGNAL_COUNT, signals.Count(signal => signal.Kind is FlowSignalKind.InteractiveUser));
		Assert.Equal(_FIXED_SIGNAL_COUNT, signals.Count(signal => signal.Kind is FlowSignalKind.FixedCredential));
		Assert.Equal(
			_declaredTable.Select(entry => (entry.Name, entry.Kind, entry.Zone)),
			signals.Select(signal => (signal.Name, signal.Kind, signal.Zone)));
	}

	[Fact]
	public void TheContractValidatorFindsNoViolationInTheAnalyzerWithItsSignals()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;

		Assert.Empty(AnalyzerContractValidator.Validate(analyzer));
	}

	[Fact]
	public void EverySignalDeclaresThePolicyMatchBoundAndAnAnchorGroup()
	{
		foreach (FlowSignal signal in ReadSignals())
		{
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, signal.Expression.MatchTimeout);
			Assert.Contains(_ANCHOR_GROUP_NAME, signal.Expression.GetGroupNames());
		}
	}

	// ---- helpers ----

	private static void AssertSameSignals(ImmutableArray<string> expected, ImmutableArray<string> actual)
	{
		Assert.Equal(expected.OrderBy(name => name, StringComparer.Ordinal), actual.OrderBy(name => name, StringComparer.Ordinal));
	}

	private static string Lines(params string[] lines)
	{
		return string.Join(_NEWLINE, lines);
	}

	private static RecognitionResult Recognize(CSharpRecognitionHarness harness, FileRecognitionContext context)
	{
		return new RecognitionEngine(harness.EmissionPoint, new FrozenTimeProvider()).Recognize(context);
	}

	private static ImmutableArray<FlowSignal> ReadSignals()
	{
		return new CSharpRecognitionHarness().Analyzer.CandidateSelectionAndRecommendation.FlowSignals;
	}

	/// <summary>
	/// The names of the signals that fire on the source, each evaluated alone by the real engine over a
	/// file that also holds one High construct, so the file is one the engine evaluates.
	/// </summary>
	private static ImmutableArray<string> FiringSignalNames(string source)
	{
		CSharpRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(Lines(_HIGH_CONSTRUCT, source));
		List<string> firing = [];

		foreach (FlowSignal signal in harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals)
		{
			RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());
			FlowSignalFixtureAnalyzer alone = new(harness.Analyzer, [signal]);
			RecognitionResult result = engine.Recognize(context with { Analyzer = alone });

			Assert.Equal(1, engine.FilesEvaluatedForFlowSignals);
			Assert.False(result.FlowEvaluationTripped);

			if (result.FlowEvidence != FlowEvidence.None)
			{
				firing.Add(signal.Name);
			}
		}

		return [.. firing];
	}

	private sealed record SignalFixture(string Label, string Source, ImmutableArray<string> ExpectedSignals);
}
