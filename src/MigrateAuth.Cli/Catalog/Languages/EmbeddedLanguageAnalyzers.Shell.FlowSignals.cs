using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The expressions of the fifteen `Shell` flow signals, ten <see cref="FlowSignalKind.InteractiveUser"/> and five <see cref="FlowSignalKind.FixedCredential"/>
/// (<see cref="LanguageCatalog.CurrentVersion"/> carries its own number). They are members of the same partial type as the detection rules, so they read the same
/// vocabulary: the command words, the windows that stop at the next command of their own kind, and the credential-name words. Every expression carries the policy
/// match timeout and is evaluated by the engine alone, once per file that holds a High or Medium construct; the anchor is the optional group <c>anchor</c>, placed on
/// the command word, the option or the wording the signal stands on.
///
/// THE DIRECT-FORM EXCLUSION. The prompt signals refuse a prompt whose answer goes directly to a local privilege tool, a remote login or a passphrase
/// argument: a match whose own line carries such a tool in command position after the prompt (and, for the dialog programs, before it). That is a
/// negative context in the expression. The form where the answer is read into a variable and used in a later
/// statement is the engine's variable tracking, declared by <see cref="ShellFlowSignalDeclarations"/> and not written here.
///
/// A CREDENTIAL IS READ FROM A PERSON ONLY WHEN THE INPUT IS THE TERMINAL: a <c>read</c> whose input is redirected on its line, a loop whose <c>done</c> is
/// redirected from a file and a <c>read</c> that follows a pipe read something that was stored, and are refused by the interactive signals. NO LITERAL
/// CREDENTIAL IS FLOW EVIDENCE: nothing here reads a credential written into source.
/// </summary>
internal static partial class ShellPatternExpressions
{
	private const string _STOP_AT_READ =
		@"(?!(?<![\w.$/-])read(?![\w.-]))";

	private const string _STOP_AT_DIALOG_TOOL =
		@"(?!(?<![\w.$/-])(?:whiptail|dialog|zenity|kdialog)(?![\w-]))";

	private const string _STOP_AT_OSASCRIPT =
		@"(?!(?<![\w.$/-])osascript(?![\w-]))";

	private const string _STOP_AT_STTY =
		@"(?!(?<![\w.$/-])stty(?![\w-]))";

	private const string _WINDOW_DIALOG_TOOL =
		@"(?:" + _STOP_AT_DIALOG_TOOL + _UNIT + @"){0,300}?";

	/// <summary>An input redirection later in the command, other than from the terminal: the read then reads something that was stored.</summary>
	private const string _REST_HAS_INPUT_REDIRECT =
		@"[^\n;&|]{0,600}<(?![ \t]*/dev/tty)";

	private const string _UNIT_NO_INPUT_REDIRECT =
		@"(?>[^\n;&|<\x27\x22\\ \t(){}`!]+|[ \t]+|[(){}`!]|" + _SQ_STRING + @"|" + _DQ_STRING + @"|\\[\s\S]|<[ \t]*(?=/dev/tty))";

	private const string _WINDOW_NO_INPUT_REDIRECT =
		@"(?:" + _STOP_AT_READ + _UNIT_NO_INPUT_REDIRECT + @"){0,300}?";

	private const string _EXCLUSION_TOKEN =
		@"(?:sudo[ \t]+(?:-\w*S|--stdin)(?![\w-])|sshpass(?![\w-])|ssh(?![\w-])|openssl[ \t][^\n]{0,200}?(?:-pass(?:in|out)|pass:))";

	private const string _COMMAND_PREFIX =
		@"(?:(?:sudo|env|time|command)[ \t]+(?:-[\w-]+[ \t]+)*)*";

	/// <summary>The exclusion token standing in command position, read backwards from a dialog program.</summary>
	private const string _EXCLUSION_IN_COMMAND_POSITION =
		_COMMAND_POSITION + _COMMAND_PREFIX + _EXCLUSION_TOKEN;

	/// <summary>The exclusion token standing in command position after a separator, read forwards from a prompt.</summary>
	private const string _SEPARATOR_THEN_EXCLUSION =
		@"(?:[;&|({`][ \t]*|\$\([ \t]*|(?:then|do|else)(?<=(?<![\w-])(?:then|do|else))[ \t]+)" + _COMMAND_PREFIX + _EXCLUSION_TOKEN;

	/// <summary>The direct form, ahead: no exclusion token in command position on the rest of the line, within a bounded window.</summary>
	private const string _PROMPT_EXCLUSION_AFTER =
		@"(?![^\n]{0,120}?" + _SEPARATOR_THEN_EXCLUSION + @")";

	/// <summary>The direct form, behind and ahead: the answer of a dialog program is the input of the tool before it or after it.</summary>
	private const string _PROMPT_EXCLUSION_BEFORE_AND_AFTER =
		@"(?<!" + _EXCLUSION_IN_COMMAND_POSITION + @"[^\n;&]{0,120}?)" + _PROMPT_EXCLUSION_AFTER;

	/// <summary>The options a <c>read</c> may carry before the name it fills, each an option cluster with an optional quoted or numeric argument.</summary>
	private const string _READ_OPTIONS =
		@"(?:[ \t]+-[A-Za-z]+(?:[ \t]+(?:\x27[^\x27\n]{0,200}\x27|\x22[^\x22\n]{0,200}\x22|[0-9]+))?){0,6}";

	private const string _READ_NAME_TAIL =
		@"[ \t]+(?<name>[A-Za-z_]\w*)[ \t]*(?:[;&|)<#\n]|$)";

	/// <summary>Captures, when there is one, the variable the <c>read</c> fills, so the engine can follow it; no capture means the default variable.</summary>
	private const string _READ_NAME_AHEAD =
		@"(?>(?=" + _READ_OPTIONS + _READ_NAME_TAIL + @")|)";

	private const string _READ_NAME_AFTER_THE_PROMPT =
		@"(?>(?=[^\x27\x22\n]{0,200}[\x27\x22]" + _READ_OPTIONS + _READ_NAME_TAIL + @")|)";

	private const string _LOOP_DONE_FROM_FILE =
		@"(?:[^\n]{0,400}\n){0,20}?[^\n]{0,400}?(?<![\w-])done[ \t]*<(?![ \t]*/dev/tty)";

	private const string _WHILE_OR_UNTIL_ON_THE_LINE =
		@"(?<=(?<![\w.$/-])(?:while|until)(?![\w-])[^\n]{0,300})";

	private const string _NOT_LOOP_READ_FROM_FILE =
		@"(?!" + _WHILE_OR_UNTIL_ON_THE_LINE + _LOOP_DONE_FROM_FILE + @")";

	private const string _NOT_AFTER_PIPE =
		@"(?<!(?<![|\\])\|(?!\|)[ \t]*(?:(?:while|until|if|then|do|!)[ \t]+)?(?:[A-Za-z_]\w*=\S*[ \t]+)*)";

	private const string _ANCHOR_USER_OPTION =
		@"(?:(?<anchor>-" + _CURL_FLAG + @"*u)" + _GAP_OPT + @"|(?<anchor>--user)(?:=|" + _GAP + @"))";

	private const string _PROMPT_WORDS =
		@"(?i:pass(?:word|phrase))";

	/// <summary>The command word that statement-starts a command that uses the answer: a privilege tool, a remote login or a passphrase argument.</summary>
	private const string _SENT_USE_OF_THE_ANSWER =
		@"(?:\A[ \t]*|[|({`][ \t]*|\$\([ \t]*|(?<![\w-])(?:then|do|else)[ \t]+)" + _COMMAND_PREFIX + _EXCLUSION_TOKEN;

	/// <summary>
	/// The silent read: the builtin <c>read</c> whose option cluster holds <c>s</c> before any option that takes an argument, with the input not redirected on the
	/// line (the terminal is still a terminal), and the variable it fills captured for the engine.
	/// </summary>
	private const string _SILENT_READ =
		_COMMAND_START + @"(?<anchor>read)(?![\w-])(?>" + _WINDOW_NO_INPUT_REDIRECT + _OPTION_START + @"-[rsE]*s[A-Za-z0-9]*(?![\w-]))(?!" + _REST_HAS_INPUT_REDIRECT + @")" + _READ_NAME_AHEAD + _PROMPT_EXCLUSION_AFTER;

	[GeneratedRegex(_SILENT_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SilentReadSignal();

	/// <summary>The terminal echo turned off and a <c>read</c> within the next five lines.</summary>
	private const string _STTY_ECHO =
		@"(?<![\w.$/-])(?<anchor>stty)(?![\w-])[ \t](?:" + _STOP_AT_STTY + @"[^\n;&|]){0,300}?(?<=[ \t])-echo(?![\w-])(?:[^\n]*\n){0,5}?[^\n]{0,300}?(?<![\w.$/-])read(?![\w-])";

	[GeneratedRegex(_STTY_ECHO, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SttyEchoSignal();

	/// <summary>
	/// A <c>read</c> into a variable whose name carries a credential word, or in the form that names the variable and the prompt in one word; its input not
	/// redirected on the line, not the read of a loop that is redirected from a file below, and not the read that follows a pipe.
	/// </summary>
	private const string _CREDENTIAL_NAMED_READ =
		_COMMAND_START + _NOT_AFTER_PIPE + @"(?<anchor>read)(?![\w-])(?>" + _WINDOW_NO_INPUT_REDIRECT + _OPTION_START + @"(?<name>" + _CRED_NAME + @")(?![\w=/.$-])|[ \t]+(?:-[A-Za-z]+[ \t]+)*[\x27\x22](?<name>" + _CRED_NAME + @")\?)(?!" + _REST_HAS_INPUT_REDIRECT + @")" + _NOT_LOOP_READ_FROM_FILE + _PROMPT_EXCLUSION_AFTER;

	[GeneratedRegex(_CREDENTIAL_NAMED_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialNamedReadSignal();

	/// <summary>The programs that ask a person for a secret in a dialog, outside the direct form.</summary>
	private const string _PASSWORD_DIALOG =
		@"(?<![\w.$/-])(?<anchor>systemd-ask-password|(?:whiptail|dialog)(?![\w-])" + _WINDOW_DIALOG_TOOL + @"--passwordbox|zenity(?![\w-])" + _WINDOW_DIALOG_TOOL + @"(?:--password|--entry(?![\w-])" + _WINDOW_DIALOG_TOOL + @"--hide-text)|kdialog(?![\w-])" + _WINDOW_DIALOG_TOOL + @"--password)(?![\w-])" + _PROMPT_EXCLUSION_BEFORE_AND_AFTER;

	[GeneratedRegex(_PASSWORD_DIALOG, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PasswordDialogSignal();

	/// <summary>The desktop dialog with a hidden answer: the evidence is the phrase inside the script string given to the dialog command.</summary>
	private const string _HIDDEN_ANSWER_DIALOG =
		@"(?<![\w.$/-])osascript(?![\w-])(?:" + _STOP_AT_OSASCRIPT + @"[^\n]){0,600}?(?<anchor>(?i:with[ \t]+hidden[ \t]+answer))";

	[GeneratedRegex(_HIDDEN_ANSWER_DIALOG, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HiddenAnswerDialogSignal();

	/// <summary>The user option of `curl` given a word with no colon: the client prompts for the password, also behind a same-file alias or command variable.</summary>
	private const string _CURL_USER_WITHOUT_PASSWORD_PROMPT =
		@"(?:" + _CURL + _WINDOW_CURL + @"|" + _CURL_ALIAS_OR_VARIABLE_TO_USE + _WINDOW_ANY_UNIT + @")" + _OPTION_START + _ANCHOR_USER_OPTION + _USER_VALUE_NO_COLON + _NOT_NON_BASIC_OR_NON_HTTP;

	[GeneratedRegex(_CURL_USER_WITHOUT_PASSWORD_PROMPT, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CurlUserWithoutPasswordSignal();

	/// <summary>`wget` asked to prompt for the password.</summary>
	private const string _WGET_ASK_PASSWORD =
		_WGET + _WINDOW_WGET + _OPTION_START + @"(?<anchor>--ask-password)(?![\w-])";

	[GeneratedRegex(_WGET_ASK_PASSWORD, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WgetAskPasswordSignal();

	/// <summary>The option of `read` that is a prompt, <c>-p</c> and a string whose words ask for a password or a passphrase. A prompt only in the shells whose <c>-p</c> is one.</summary>
	private const string _PROMPT_WORDING_OF_THE_PROMPT_OPTION =
		@"(?<![\w.$/-])read(?![\w-])(?>" + _WINDOW_NO_INPUT_REDIRECT + _OPTION_START + @"-[rseE]*p" + _GAP_OPT + @"[\x27\x22][^\x27\x22\n]{0,200}?(?<anchor>" + _PROMPT_WORDS + @"))" + _READ_NAME_AFTER_THE_PROMPT + _PROMPT_EXCLUSION_AFTER;

	[GeneratedRegex(_PROMPT_WORDING_OF_THE_PROMPT_OPTION, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PromptWordingOfThePromptOptionSignal();

	/// <summary>
	/// The wording of a prompt in the other two forms: the string of the <c>read</c> that names the variable and the prompt in one word, and the <c>printf</c> or
	/// <c>echo</c> on the line before a <c>read</c>, which is how a script that cannot use the option prompts.
	/// </summary>
	private const string _PROMPT_WORDING =
		@"(?<![\w.$/-])(?:read(?![\w-])[ \t]+(?:-[A-Za-z]+[ \t]+)*[\x27\x22](?<name>[A-Za-z_]\w*)\?[^\x27\x22\n]{0,200}?(?<anchor>" + _PROMPT_WORDS + @")|(?:printf|echo)(?![\w-])(?>(?:" + _STOP_AT_ECHO_OR_PRINTF + @"[^\n]){0,300}?(?<anchor>" + _PROMPT_WORDS + @"))(?>[^\n]*)\n(?:[ \t]*\n){0,2}[ \t]*(?:read(?![\w-])|[^\n;&|]{0,300}?[;&|][ \t]*read(?![\w-]))" + _READ_NAME_AHEAD + @")" + _PROMPT_EXCLUSION_AFTER;

	[GeneratedRegex(_PROMPT_WORDING, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PromptWordingSignal();

	/// <summary>The authentication option of HTTPie given a word with no colon: the client prompts for the password.</summary>
	private const string _HTTPIE_AUTH_WITHOUT_PASSWORD =
		_HTTPIE_COMMAND + _WINDOW_HTTPIE + _OPTION_START + @"(?:(?<anchor>-a)" + _GAP_OPT + @"|(?<anchor>--auth)(?:=|" + _GAP + @"))" + _USER_VALUE_NO_COLON + _NOT_NON_HTTP_OR_OTHER_AUTH_TYPE;

	[GeneratedRegex(_HTTPIE_AUTH_WITHOUT_PASSWORD, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HttpieAuthWithoutPasswordSignal();

	/// <summary>In a covered client's arguments, an expansion of a credential-named variable or of the required-variable form. The arguments span a backslash-line-feed.</summary>
	private const string _CREDENTIAL_NAMED_EXPANSION =
		_CLIENT_COMMAND + _CHAR_WINDOW_CLIENT + @"(?<anchor>\$\{?" + _CRED_NAME + @"(?![A-Za-z0-9_])|\$\{[A-Za-z_]\w*:\?)";

	[GeneratedRegex(_CREDENTIAL_NAMED_EXPANSION, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialNamedExpansionSignal();

	/// <summary>The secret-store reads the signal lists, by command and subcommand. Another store is not recognized.</summary>
	private const string _SECRET_STORE_TOOL =
		@"(?<![\w.$/-])(?<anchor>security[ \t]+find-(?:generic|internet)-password|secret-tool[ \t]+lookup|pass[ \t]+show|gopass[ \t]+show|vault[ \t]+kv[ \t]+get|aws[ \t]+secretsmanager[ \t]+get-secret-value|aws[ \t]+ssm[ \t]+get-parameter|az[ \t]+keyvault[ \t]+secret[ \t]+show|gcloud[ \t]+secrets[ \t]+versions[ \t]+access|op[ \t]+read|bw[ \t]+get[ \t]+password)(?![\w-])";

	[GeneratedRegex(_SECRET_STORE_TOOL, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SecretStoreToolSignal();

	/// <summary>The netrc options of `curl`: a credential file prepared by whoever deploys the script.</summary>
	private const string _NETRC_OPTION =
		_CURL + _WINDOW_CURL + _OPTION_START + @"(?<anchor>-" + _CURL_FLAG + @"*n(?![\w-])|--netrc(?:-file|-optional)?(?![\w-]))";

	[GeneratedRegex(_NETRC_OPTION, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NetrcOptionSignal();

	/// <summary>A <c>read</c> into a credential-named variable whose input is redirected from a file or a here-string: a stored value, not a typed one.</summary>
	private const string _REDIRECTED_CREDENTIAL_READ =
		_COMMAND_START + @"(?<anchor>read)(?![\w-])(?:[ \t]+-[A-Za-z]+(?:[ \t]+\S+)?)*[ \t]+(?<name>" + _CRED_NAME + @")[ \t]*<[ \t]*(?!<)";

	[GeneratedRegex(_REDIRECTED_CREDENTIAL_READ, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex RedirectedCredentialReadSignal();

	/// <summary>A <c>while</c> or <c>until</c> loop whose <c>read</c> fills a credential-named variable and whose <c>done</c> is redirected from a file.</summary>
	private const string _LOOP_READ_FROM_FILE =
		_COMMAND_START + @"(?<anchor>read)(?![\w-])(?<=(?<![\w.$/-])(?:while|until)[ \t]+(?:[A-Za-z_]\w*=\S*[ \t]+)*read)(?=[^\n;&|<]{0,300}(?<=[ \t])(?<name>" + _CRED_NAME + @")(?![\w=/.$-])[ \t]*(?:[;&|)<#\n]|$))(?=" + _LOOP_DONE_FROM_FILE + @")";

	[GeneratedRegex(_LOOP_READ_FROM_FILE, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex LoopReadFromFileSignal();

	/// <summary>
	/// The statement that sends the answer of a prompt where it does not reach the REST service: a privilege tool reading standard input, a remote login, a
	/// password-handing wrapper or a passphrase argument, in command position of the statement that references the variable.
	/// </summary>
	[GeneratedRegex(
		_SENT_USE_OF_THE_ANSWER,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PromptAnswerSentElsewhereUse();
}

/// <summary>
/// The `Shell` analyzer's <see cref="CandidateSelectionAndRecommendation.FlowSignals"/>: fifteen signals, with the dialects each reads in and the variable
/// tracking of the three that capture the variable a prompt fills.
/// </summary>
internal static class ShellFlowSignalDeclarations
{
	private const string _SILENT_READ = "SilentRead";
	private const string _STTY_ECHO = "SttyEcho";
	private const string _CREDENTIAL_NAMED_READ = "CredentialNamedRead";
	private const string _PASSWORD_DIALOG = "PasswordDialog";
	private const string _HIDDEN_ANSWER_DIALOG = "HiddenAnswerDialog";
	private const string _CURL_USER_WITHOUT_PASSWORD = "CurlUserWithoutPassword";
	private const string _WGET_ASK_PASSWORD = "WgetAskPassword";
	private const string _PROMPT_WORDING_OF_THE_PROMPT_OPTION = "PromptWordingOfThePromptOption";
	private const string _PROMPT_WORDING = "PromptWording";
	private const string _HTTPIE_AUTH_WITHOUT_PASSWORD = "HttpieAuthWithoutPassword";
	private const string _CREDENTIAL_NAMED_EXPANSION = "CredentialNamedExpansion";
	private const string _SECRET_STORE_TOOL = "SecretStoreTool";
	private const string _NETRC_OPTION = "NetrcOption";
	private const string _REDIRECTED_CREDENTIAL_READ = "RedirectedCredentialRead";
	private const string _LOOP_READ_FROM_FILE = "LoopReadFromFile";

	/// <summary>The group the prompt expressions capture the filled variable in.</summary>
	private const string _PROMPT_VARIABLE_GROUP = "name";

	/// <summary>The variable a <c>read</c> that names none fills.</summary>
	private const string _DEFAULT_PROMPT_VARIABLE = "REPLY";

	private const string _SEQUENCE = ";";
	private const string _AND_LIST = "&&";
	private const string _OR_LIST = "||";
	private const string _BACKGROUND = "&";

	/// <summary>The dialects whose <c>-s</c> option does not hide the input: it is accepted and the answer is echoed.</summary>
	private static readonly ImmutableArray<string> _dialectsWhereTheSilentOptionDoesNotHide = [ShellDialectNames.KSH93, ShellDialectNames.MKSH];

	/// <summary>The dialects whose <c>-p</c> option of <c>read</c> is a prompt: only <c>bash</c> reads its string as one, and <c>sh</c> is read as <c>bash</c>.</summary>
	private static readonly ImmutableArray<string> _dialectsWherePromptOptionIsAPrompt = [ShellDialectNames.BASH];

	internal static ImmutableArray<FlowSignal> Build()
	{
		return [.. InteractiveUserSignals(), .. FixedCredentialSignals()];
	}

	private static ImmutableArray<FlowSignal> InteractiveUserSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			new FlowSignal
			{
				Name = _SILENT_READ,
				Kind = FlowSignalKind.InteractiveUser,
				Zone = FlowSignalZone.CodeOnly,
				Expression = ShellPatternExpressions.SilentReadSignal(),
				ExcludedDialects = _dialectsWhereTheSilentOptionDoesNotHide,
				VariableUse = PromptAnswerUse()
			},
			Signal(_STTY_ECHO, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, ShellPatternExpressions.SttyEchoSignal()),
			new FlowSignal
			{
				Name = _CREDENTIAL_NAMED_READ,
				Kind = FlowSignalKind.InteractiveUser,
				Zone = FlowSignalZone.CodeOnly,
				Expression = ShellPatternExpressions.CredentialNamedReadSignal(),
				VariableUse = PromptAnswerUse()
			},
			Signal(_PASSWORD_DIALOG, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, ShellPatternExpressions.PasswordDialogSignal()),
			Signal(_HIDDEN_ANSWER_DIALOG, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOrStringBody, ShellPatternExpressions.HiddenAnswerDialogSignal()),
			Signal(_CURL_USER_WITHOUT_PASSWORD, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, ShellPatternExpressions.CurlUserWithoutPasswordSignal()),
			Signal(_WGET_ASK_PASSWORD, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, ShellPatternExpressions.WgetAskPasswordSignal()),
			new FlowSignal
			{
				Name = _PROMPT_WORDING_OF_THE_PROMPT_OPTION,
				Kind = FlowSignalKind.InteractiveUser,
				Zone = FlowSignalZone.CodeOrStringBody,
				Expression = ShellPatternExpressions.PromptWordingOfThePromptOptionSignal(),
				Dialects = _dialectsWherePromptOptionIsAPrompt,
				VariableUse = PromptAnswerUse()
			},
			new FlowSignal
			{
				Name = _PROMPT_WORDING,
				Kind = FlowSignalKind.InteractiveUser,
				Zone = FlowSignalZone.CodeOrStringBody,
				Expression = ShellPatternExpressions.PromptWordingSignal(),
				VariableUse = PromptAnswerUse()
			},
			Signal(_HTTPIE_AUTH_WITHOUT_PASSWORD, FlowSignalKind.InteractiveUser, FlowSignalZone.CodeOnly, ShellPatternExpressions.HttpieAuthWithoutPasswordSignal())
		];

		return signals;
	}

	private static ImmutableArray<FlowSignal> FixedCredentialSignals()
	{
		ImmutableArray<FlowSignal> signals =
		[
			Signal(_CREDENTIAL_NAMED_EXPANSION, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOrStringBody, ShellPatternExpressions.CredentialNamedExpansionSignal()),
			Signal(_SECRET_STORE_TOOL, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, ShellPatternExpressions.SecretStoreToolSignal()),
			Signal(_NETRC_OPTION, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, ShellPatternExpressions.NetrcOptionSignal()),
			Signal(_REDIRECTED_CREDENTIAL_READ, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, ShellPatternExpressions.RedirectedCredentialReadSignal()),
			Signal(_LOOP_READ_FROM_FILE, FlowSignalKind.FixedCredential, FlowSignalZone.CodeOnly, ShellPatternExpressions.LoopReadFromFileSignal())
		];

		return signals;
	}

	/// <summary>
	/// The variable tracking of a prompt: a match is refused when the variable it fills is used, within the engine's window of the lines that follow, by a
	/// statement that sends it somewhere other than the REST service. A statement ends at <c>;</c>, <c>&amp;&amp;</c>, <c>||</c> or <c>&amp;</c>; a pipe does not end
	/// it, so <c>printf "$P" | sudo -S</c> is one statement.
	/// </summary>
	private static FlowSignalVariableUse PromptAnswerUse()
	{
		return new FlowSignalVariableUse
		{
			VariableGroupName = _PROMPT_VARIABLE_GROUP,
			DefaultVariableName = _DEFAULT_PROMPT_VARIABLE,
			UsePattern = ShellPatternExpressions.PromptAnswerSentElsewhereUse(),
			StatementSeparators = [_SEQUENCE, _AND_LIST, _OR_LIST, _BACKGROUND]
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
