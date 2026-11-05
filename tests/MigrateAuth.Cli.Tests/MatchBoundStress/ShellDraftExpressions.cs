using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE DRAFT `Shell` EXPRESSIONS OF THE COST GATE: forty-six expressions, as test-local constants, each compiled by
/// <c>[GeneratedRegex]</c> the way the analyzer compiles it, with the policy match timeout (the lexical-budget timeout for the three that run in the scanner and
/// the attribution, the match bound for the rest). Nothing here is read from <c>src/</c>.
///
/// THE CONSTANT SECTION BELOW IS THE TEXT OF THE EXPRESSIONS, CHARACTER FOR CHARACTER. A reshape changes that text, and the semantic checks of
/// <see cref="ShellDraftExpressionSemanticTests"/> are what show that the reshape kept the behaviour.
/// </summary>
internal static partial class ShellDraftExpressions
{
	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private const RegexOptions _LINE_OPTIONS = _OPTIONS | RegexOptions.Multiline;

	/// <summary>The expressions charged to the lexical budget: the two here-document openings and the first-line detection.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> LexicalExpressions { get; } =
	[
		("EX-SH-01", Ex01()),
		("EX-SH-02", Ex02()),
		("EX-SH-04", Ex04())
	];

	/// <summary>The expressions charged to the recognition engine's match bound: the constant declaration, the class rules, the content markers and the flow signals.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> MatchBoundExpressions { get; } =
	[
		("EX-SH-03", Ex03()),
		("EX-SH-05", Ex05()),
		("EX-SH-06", Ex06()),
		("EX-SH-07", Ex07()),
		("EX-SH-08", Ex08()),
		("EX-SH-09", Ex09()),
		("EX-SH-10", Ex10()),
		("EX-SH-11", Ex11()),
		("EX-SH-12", Ex12()),
		("EX-SH-13", Ex13()),
		("EX-SH-14", Ex14()),
		("EX-SH-15", Ex15()),
		("EX-SH-16", Ex16()),
		("EX-SH-17", Ex17()),
		("EX-SH-18", Ex18()),
		("EX-SH-19", Ex19()),
		("EX-SH-20", Ex20()),
		("EX-SH-21", Ex21()),
		("EX-SH-22", Ex22()),
		("EX-SH-23", Ex23()),
		("EX-SH-24", Ex24()),
		("EX-SH-25", Ex25()),
		("EX-SH-26", Ex26()),
		("EX-SH-27", Ex27()),
		("EX-SH-28", Ex28()),
		("EX-SH-29", Ex29()),
		("EX-SH-30", Ex30()),
		("EX-SH-31", Ex31()),
		("EX-SH-32", Ex32()),
		("EX-SH-33", Ex33()),
		("EX-SH-34", Ex34()),
		("EX-SH-35", Ex35()),
		("EX-SH-36", Ex36()),
		("EX-SH-37", Ex37()),
		("EX-SH-38", Ex38()),
		("EX-SH-39", Ex39()),
		("EX-SH-40", Ex40()),
		("EX-SH-41", Ex41()),
		("EX-SH-42", Ex42()),
		("EX-SH-43", Ex43()),
		("EX-SH-44", Ex44()),
		("EX-SH-45", Ex45()),
		("EX-SH-46", Ex46())
	];

	#region The expression text, as written in the catalog

	private const string _GAP =
		@"(?>(?:[ \t]+|\\\n[ \t]*){1,16})";

	private const string _GAP_OPT =
		@"(?>(?:[ \t]+|\\\n[ \t]*){0,16})";

	private const string _LINE_START =
		@"(?<![^\n])";

	private const string _SQ_STRING =
		@"\x27[^\x27]{0,4096}\x27";

	private const string _DQ_STRING =
		@"\x22(?>[^\x22\\]|\\[\s\S]){0,4096}\x22";

	private const string _UNIT =
		@"(?>[^\n;&|\x27\x22\\]|" + _SQ_STRING + @"|" + _DQ_STRING + @"|\\[\s\S])";

	private const string _TOKEN =
		@"(?>[^\n;&|\x27\x22\\ \t(){}`!]+|[ \t]+|[(){}`!]|" + _SQ_STRING + @"|" + _DQ_STRING + @"|\\[\s\S])";

	private const string _CHAR_UNIT =
		@"(?>[^\n;&|\\]|\\\n|\\)";

	private const string _CHAR_TOKEN =
		@"(?>[\w-]+|[^\n;&|\\\w-]|\\\n|\\)";

	private const string _STOP_AT_READ =
		@"(?!(?<![\w.$/-])read(?![\w.-]))";

	private const string _STOP_AT_CLIENT =
		@"(?!(?<![\w.$/-])(?:(?:curl|wget)(?:\.exe)?(?![\w.-])|(?:aria2c|lwp-request)(?![\w.-])|https?(?=[ \t])))";

	private const string _STOP_AT_ECHO_OR_PRINTF =
		@"(?!(?<![\w.$/-])(?:echo|printf)(?![\w-]))";

	private const string _STOP_AT_DIALOG_TOOL =
		@"(?!(?<![\w.$/-])(?:whiptail|dialog|zenity|kdialog)(?![\w-]))";

	private const string _STOP_AT_OSASCRIPT =
		@"(?!(?<![\w.$/-])osascript(?![\w-]))";

	private const string _STOP_AT_STTY =
		@"(?!(?<![\w.$/-])stty(?![\w-]))";

	private const string _WINDOW_CURL =
		@"(?:" + _STOP_AT_CLIENT + _TOKEN + @"){0,300}?";

	private const string _WINDOW_WGET =
		@"(?:" + _STOP_AT_CLIENT + _TOKEN + @"){0,300}?";

	private const string _WINDOW_HTTPIE =
		@"(?:" + _STOP_AT_CLIENT + _TOKEN + @"){0,300}?";

	private const string _WINDOW_ARIA2C =
		@"(?:" + _STOP_AT_CLIENT + _TOKEN + @"){0,300}?";

	private const string _WINDOW_LWP =
		@"(?:" + _STOP_AT_CLIENT + _TOKEN + @"){0,300}?";

	private const string _WINDOW_CLIENT =
		@"(?:" + _STOP_AT_CLIENT + _TOKEN + @"){0,300}?";

	private const string _WINDOW_ECHO_OR_PRINTF =
		@"(?:" + _STOP_AT_ECHO_OR_PRINTF + _TOKEN + @"){0,300}?";

	private const string _WINDOW_DIALOG_TOOL =
		@"(?:" + _STOP_AT_DIALOG_TOOL + _UNIT + @"){0,300}?";

	private const string _WINDOW_ANY_UNIT =
		@"(?:" + _TOKEN + @"){0,300}?";

	private const string _CHAR_WINDOW_CONT =
		_CHAR_TOKEN + @"{0,600}?";

	private const string _CHAR_WINDOW_CURL =
		@"(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @"){0,600}?";

	private const string _CHAR_WINDOW_WGET =
		@"(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @"){0,600}?";

	private const string _CHAR_WINDOW_HTTPIE =
		@"(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @"){0,600}?";

	private const string _CHAR_WINDOW_ARIA2C =
		@"(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @"){0,600}?";

	private const string _CHAR_WINDOW_LWP =
		@"(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @"){0,600}?";

	private const string _CHAR_WINDOW_CLIENT =
		@"(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @"){0,600}?";

	private const string _CHAR_UNIT_WITHIN_THE_COMMAND =
		@"(?:" + _STOP_AT_CLIENT + _CHAR_UNIT + @")";

	private const string _REST_HAS_INPUT_REDIRECT =
		@"[^\n;&|]{0,600}<(?![ \t]*/dev/tty)";

	private const string _UNIT_NO_INPUT_REDIRECT =
		@"(?>[^\n;&|<\x27\x22\\ \t(){}`!]+|[ \t]+|[(){}`!]|" + _SQ_STRING + @"|" + _DQ_STRING + @"|\\[\s\S]|<[ \t]*(?=/dev/tty))";

	private const string _WINDOW_NO_INPUT_REDIRECT =
		@"(?:" + _STOP_AT_READ + _UNIT_NO_INPUT_REDIRECT + @"){0,300}?";

	private const string _DEFINITION_LINES =
		@"[^\n]{0,400}\n(?:[^\n]{0,400}\n){0,29}?";

	private const string _DEFINITION_WINDOW =
		_DEFINITION_LINES + @"[^\n]{0,400}?";

	private const string _DEFINITION_WINDOW_BEHIND =
		@"[^\n]{0,400}\n(?>[^\n]{0,400}\n){0,29}(?>[^\n]{0,200})";

	private const string _COMMAND_START =
		@"(?<![\w.$/-])(?:/(?:[\w.+-]+/)*)?";

	private const string _CURL =
		_COMMAND_START + @"curl(?:\.exe)?(?![\w.-])";

	private const string _WGET =
		_COMMAND_START + @"wget(?:\.exe)?(?![\w.-])";

	private const string _HTTPIE_COMMAND =
		_COMMAND_START + @"https?(?=[ \t])";

	private const string _ARIA2C =
		_COMMAND_START + @"aria2c(?![\w.-])";

	private const string _LWP =
		_COMMAND_START + @"lwp-request(?![\w.-])";

	private const string _CLIENT_COMMAND =
		_COMMAND_START + @"(?:(?:curl|wget)(?:\.exe)?(?![\w.-])|(?:aria2c|lwp-request)(?![\w.-])|https?(?=[ \t]))";

	private const string _OPTION_START =
		@"(?<=[ \t\n])";

	private const string _CURL_FLAG =
		@"[aqfGIpk046jlLMnNZ#JORSis231BvV]";

	private const string _WORD_END =
		@"(?![^\s;&|<>)])";

	private const string _BARE_NC =
		@"[^\s\x27\x22$`\\:;&|<>(){}*?\[\]]";

	private const string _BARE =
		@"[^\s\x27\x22$`\\;&|<>(){}*?\[\]]";

	private const string _QPART_SQ =
		@"\x27[^\x27\n]{0,512}\x27";

	private const string _QPART_DQ =
		@"\x22[^\x22$`\\\n]{0,512}\x22";

	private const string _USER_PART =
		@"(?>(?:" + _BARE_NC + @"|" + _QPART_SQ + @"|" + _QPART_DQ + @")+)";

	private const string _PASS_PART =
		@"(?>(?:" + _BARE + @"|" + _QPART_SQ + @"|" + _QPART_DQ + @")+)";

	private const string _CRED_NAME =
		@"(?:[A-Za-z0-9_]*(?i:passw(?:or)?d|secret|credential|creds?|token)[A-Za-z0-9_]*|(?:[A-Za-z0-9]+_)*(?i:pass|pw|pwd|apikey)(?![A-Za-z0-9])|[a-z][a-z0-9]*Pass(?![a-z0-9]))";

	private const string _NON_HTTP_SCHEME_WORDS =
		@"(?:ftps?|sftp|scp|smtps?|imaps?|pop3s?|ldaps?|telnet|smbs?|tftp|dict|gopher|mqtts?|rtsp|file)";

	private const string _NON_HTTP_URL =
		@"(?<![\w-])" + _NON_HTTP_SCHEME_WORDS + @"://";

	private const string _METHOD_FLAG =
		_OPTION_START + @"--(?:digest|ntlm|ntlm-wb|negotiate|anyauth|aws-sigv4)(?![\w-])";

	private const string _METHOD_FLAG_EXCEPT_ANYAUTH =
		_OPTION_START + @"--(?:digest|ntlm|ntlm-wb|negotiate|aws-sigv4)(?![\w-])";

	private const string _DECLARATION_OPTIONAL =
		@"(?:(?:export|readonly|local|declare|typeset)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?";

	private const string _URL_WORD_OF_A_VARIABLE_HOLDING_A_NON_HTTP_SCHEME =
		@"(?<=[ \t\n])\x22?\$\{?(?<urlvar>[A-Za-z_]\w*)\}?(?>/[^\s\x22;&|<>)]{0,200})?\x22?" + _WORD_END + @"(?<=" + _LINE_START + @"[ \t]*" + _DECLARATION_OPTIONAL + @"\k<urlvar>=[\x22\x27]?" + _NON_HTTP_SCHEME_WORDS + @"://" + _DEFINITION_WINDOW_BEHIND + @")";

	private const string _NOT_NON_BASIC_OR_NON_HTTP =
		@"(?<!(?:" + _METHOD_FLAG + @"|" + _NON_HTTP_URL + @")" + _CHAR_UNIT_WITHIN_THE_COMMAND + @"{0,600})(?!" + _CHAR_WINDOW_CLIENT + @"(?:" + _METHOD_FLAG + @"|" + _NON_HTTP_URL + @"|" + _URL_WORD_OF_A_VARIABLE_HOLDING_A_NON_HTTP_SCHEME + @"))";

	private const string _NOT_NON_BASIC_OR_NON_HTTP_EXCEPT_ANYAUTH =
		@"(?<!(?:" + _METHOD_FLAG_EXCEPT_ANYAUTH + @"|" + _NON_HTTP_URL + @")" + _CHAR_UNIT_WITHIN_THE_COMMAND + @"{0,600})(?!" + _CHAR_WINDOW_CLIENT + @"(?:" + _METHOD_FLAG_EXCEPT_ANYAUTH + @"|" + _NON_HTTP_URL + @"|" + _URL_WORD_OF_A_VARIABLE_HOLDING_A_NON_HTTP_SCHEME + @"))";

	private const string _USER_VALUE_LITERAL =
		@"(?:\x27(?<user>[^\x27:\n]*):(?<credential>[^\x27\n]+)\x27" + _WORD_END + @"|\x22(?<user>[^\x22:$`\\\n]*):(?<credential>[^\x22$`\\\n]+)\x22" + _WORD_END + @"|(?!~)(?<user>" + _USER_PART + @"):(?<credential>" + _PASS_PART + @")" + _WORD_END + @")";

	private const string _USER_VALUE_EXPANDING =
		@"(?=(?:[^\s\x27\x22\\;&|<>:$`]|\\[\s\S]|\x27[^\x27:$`]{0,512}\x27|\x22[^\x22\\:$`]{0,512}\x22){0,600}(?:[:$`]|[\x27\x22][^\x27\x22]{0,512}[:$`]))(?>(?:[^\s\x27\x22\\;&|<>]|\\[\s\S]|" + _SQ_STRING + @"|" + _DQ_STRING + @")+)";

	private const string _USER_VALUE_NO_COLON =
		@"(?!-)(?:\x27[^\x27:$`\\\n]+\x27|\x22[^\x22:$`\\\n]+\x22|(?!~)[^\s\x27\x22$`\\:;&|<>(){}*?\[\]=](?:" + _BARE_NC + @")*)" + _WORD_END;

	private const string _CURL_USER_OPTION =
		@"(?:(?<construct>-" + _CURL_FLAG + @"*u)" + _GAP_OPT + @"|(?<construct>--user)(?:=|" + _GAP + @"))";

	private const string _URL_USERINFO_LITERAL =
		@"(?<![\w-])https?://(?<user>[^\s/@:\x27\x22$`\\]+):(?<credential>[^\s/@\x27\x22$`\\]+)@[^\s\x27\x22$`\\;&|<>()]*(?=[\x27\x22\s;&|)<>]|$)";

	private const string _URL_USERINFO_ANY =
		@"(?<![\w-])https?://[^\s/@:\x27\x22]+:[^\s/@\x27\x22]+@";

	internal const string EX_SH_01 =
		@"\G<<(?![<-])[ \t]*(?:\x27(?<terminator>[^\x27\n]*)\x27(?<quoted>)|\x22(?<terminator>[^\x22\n]*)\x22(?<quoted>)|\\(?<terminator>[^\s|&;()<>\x27\x22\\]+)(?<quoted>)|(?<terminator>[^\s|&;()<>\x27\x22\\]+))(?![^\s|&;()<>])";

	internal const string EX_SH_02 =
		@"\G<<-(?!<)[ \t]*(?:\x27(?<terminator>[^\x27\n]*)\x27(?<quoted>)|\x22(?<terminator>[^\x22\n]*)\x22(?<quoted>)|\\(?<terminator>[^\s|&;()<>\x27\x22\\]+)(?<quoted>)|(?<terminator>[^\s|&;()<>\x27\x22\\]+))(?![^\s|&;()<>])";

	internal const string EX_SH_03 =
		@"^[ \t]*(?:(?:readonly|export|local|declare|typeset)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?(?<name>[A-Za-z_][A-Za-z0-9_]*)=(?:\x27(?<value>[^\x27\n]*)\x27|\x22(?<value>[^\x22$`\\\n]*)\x22|(?<value>[^\s\x27\x22$`\\;&|<>()]+))(?=[ \t;]|$)";

	internal const string EX_SH_04 =
		@"^#![ \t]*(?:[^\s/]*/)*(?:env[ \t]+(?:-\S*[ \t]+)*)?(?:bash|sh|zsh|ksh)(?:[ \t]|$)";

	internal const string EX_SH_05 =
		_CURL + _WINDOW_CURL + _OPTION_START + _CURL_USER_OPTION + @"(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @")" + _NOT_NON_BASIC_OR_NON_HTTP;

	internal const string EX_SH_06 =
		_CURL + _WINDOW_CURL + _OPTION_START + _CURL_USER_OPTION + _USER_VALUE_NO_COLON + _NOT_NON_BASIC_OR_NON_HTTP;

	private const string _HEADER_VALUE =
		@"[\x22\x27]?(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+(?:(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22\x27\s;&|)]|$)|(?=[^\x22\x27\s]*[$`\\])|\{\{[^{}\n]{0,200}\}\})";

	private const string _CURL_HEADER_OPTION =
		@"(?:(?<construct>-" + _CURL_FLAG + @"*H)" + _GAP_OPT + @"|(?<construct>--(?:expand-)?header)(?:=|" + _GAP + @"))";

	internal const string EX_SH_07 =
		@"(?:" + _CURL + _WINDOW_CURL + _OPTION_START + _CURL_HEADER_OPTION + @"|" + _WGET + _WINDOW_WGET + _OPTION_START + @"(?<construct>--header)(?:=|" + _GAP + @"))" + _HEADER_VALUE;

	private const string _EXECUTE_OPTION =
		_OPTION_START + @"(?:-e|--execute)(?:=|" + _GAP_OPT + @")";

	private const string _WGET_PREEMPTIVE_SWITCH =
		@"(?:" + _OPTION_START + @"--auth-no-challenge(?![\w-])|" + _EXECUTE_OPTION + @"[\x22\x27]?auth_no_challenge[ \t]*=[ \t]*(?:on|1|yes)(?![\w-]))";

	private const string _WGET_PASSWORD_OPTION_LITERAL =
		_OPTION_START + @"--(?:http-)?passw(?:or)?d(?:=|" + _GAP + @")(?!~)(?<credential>" + _PASS_PART + @")" + _WORD_END;

	private const string _WGET_CREDENTIAL_OPTION_ANY =
		_OPTION_START + @"--(?:http-)?(?:user|passw(?:or)?d)(?:=|" + _GAP + @")";

	private const string _WGET_CREDENTIAL_LOOKAHEADS =
		@"(?:(?=" + _WINDOW_WGET + _WGET_PASSWORD_OPTION_LITERAL + @")|(?=" + _CHAR_WINDOW_WGET + _URL_USERINFO_LITERAL + @")|(?=" + _WINDOW_WGET + _WGET_CREDENTIAL_OPTION_ANY + @")|(?=" + _CHAR_WINDOW_WGET + _URL_USERINFO_ANY + @"))";

	internal const string EX_SH_08 =
		@"(?<construct>" + _WGET + @")(?=" + _WINDOW_WGET + _WGET_PREEMPTIVE_SWITCH + @")" + _WGET_CREDENTIAL_LOOKAHEADS;

	internal const string EX_SH_09 =
		@"(?<construct>" + _WGET + @")" + _WGET_CREDENTIAL_LOOKAHEADS;

	internal const string EX_SH_10 =
		@"(?<construct>" + _CURL + @")(?:(?=" + _CHAR_WINDOW_CURL + _URL_USERINFO_LITERAL + @")|(?=" + _CHAR_WINDOW_CURL + _URL_USERINFO_ANY + @"))(?!" + _CHAR_WINDOW_CURL + _METHOD_FLAG + @")";

	internal const string EX_SH_11 =
		_CURL + _WINDOW_CURL + _OPTION_START + @"(?<construct>-" + _CURL_FLAG + @"*n(?![\w-])|--netrc(?:-file|-optional)?(?![\w-]))";

	private const string _PORT_TOKEN =
		@"\$\{?(?:[A-Za-z0-9]+_)*(?i:port)(?:_[A-Za-z0-9]+)*(?![A-Za-z0-9])";

	private const string _HOST_EXPANSION =
		@"\$\{?(?:[A-Za-z0-9]+_)*(?i:host|hostname|server|addr|address)(?:_[A-Za-z0-9]+)*(?![A-Za-z0-9])";

	private const string _PAIR =
		@"(?<=[\s\x27\x22])(?!" + _HOST_EXPANSION + @"[^\s:\x27\x22|;&]*:(?!\$\{?" + _CRED_NAME + @"(?![A-Za-z0-9_])))[A-Za-z_$%{][^\s:\x27\x22|;&]*:(?!//)(?!\d{1,5}(?![\w$]))[^\s:\x27\x22|;&/\\]";

	private const string _NOT_PORT_EXPANSION =
		@"(?!(?:" + _STOP_AT_ECHO_OR_PRINTF + @"[^\n|;&]){0,400}?" + _PORT_TOKEN + @")";

	private const string _ENCODER =
		@"(?:g?base64|basenc" + _GAP + @"--base64|busybox" + _GAP + @"base64|openssl" + _GAP + @"(?:enc" + _GAP + @"-(?:base64|a)|base64)|uuencode" + _GAP + @"-m)(?![\w-])";

	private const string _DECODE_REFUSAL =
		@"(?![^\n|;&]*?(?<=\s)(?:-d|-D|--decode)(?![\w-]))";

	private const string _FILTER_STAGE =
		@"(?:tr|cut|head|sed|awk|fold|tee)(?![\w-])(?>(?:" + _UNIT + @"){0,60})" + _GAP_OPT + @"\|(?!\|)" + _GAP_OPT;

	private const string _ENCODE_WORD =
		@"(?:b64encode|standard_b64encode|encodebytes|encode_base64)";

	private const string _PROGRAM_OPENING =
		@"(?:python3?|perl)(?![\w-])[^\n;&|\x27\x22]{0,60}?[\x27\x22]";

	private const string _PIPE_PRODUCER_PIPELINE =
		@"(?<construct>printf|echo)(?![\w-])" + _NOT_PORT_EXPANSION + @"(?=(?:" + _STOP_AT_ECHO_OR_PRINTF + @"[^\n|;&]){0,400}?" + _PAIR + @")" + _WINDOW_ECHO_OR_PRINTF + _GAP_OPT + @"\|(?!\|)" + _GAP_OPT + @"(?:" + _FILTER_STAGE + @"){0,2}(?:(?<consumer>" + _ENCODER + @")" + _DECODE_REFUSAL + @"|(?<consumer>" + _PROGRAM_OPENING + @"[^\n]{0,600}?" + _ENCODE_WORD + @"))";

	private const string _HERE_STRING_PIPELINE =
		@"(?<construct>" + _ENCODER + @")" + _DECODE_REFUSAL + @"(?>(?:[ \t]+-[\w-]+)*)[ \t]*<<<[ \t]*[\x22\x27]?(?=" + _PAIR + @")(?![^\n|;&)]{0,200}?" + _PORT_TOKEN + @")";

	private const string _PROGRAM_WITH_PAIR_PIPELINE =
		@"(?<construct>python3?|perl)(?![\w-])[^\n;&|\x27\x22]{0,60}?[\x27\x22](?=[^\n]{0,600}?" + _ENCODE_WORD + @")(?=[^\n]{0,600}?" + _PAIR + @")";

	private const string _ENCODING_PIPELINE =
		@"(?:(?<![\w.$/-])" + _PIPE_PRODUCER_PIPELINE + @"|(?<![\w.$/-])" + _HERE_STRING_PIPELINE + @"|(?<![\w.$/-])" + _PROGRAM_WITH_PAIR_PIPELINE + @")";

	private const string _VALID_USE_OF_ASSIGNED_BY_A_CLIENT =
		@"\$\{?\k<assigned>(?![A-Za-z0-9_])(?<=" + _CLIENT_COMMAND + _CHAR_UNIT + @"{0,600}\$\{?\k<assigned>)";

	private const string _USE_OF_ASSIGNED_BY_A_CLIENT =
		@"(?>[^\n]{0,400}\n(?:[^\n$]*(?:(?!" + _VALID_USE_OF_ASSIGNED_BY_A_CLIENT + @")\$[^\n$]*)*\n){0,29}[^\n$]*(?:(?!" + _VALID_USE_OF_ASSIGNED_BY_A_CLIENT + @")\$[^\n$]*)*)" + _VALID_USE_OF_ASSIGNED_BY_A_CLIENT;

	private const string _VALID_USE_OF_CONSTRUCT_BY_A_CLIENT =
		@"\$\{?\k<construct>(?![A-Za-z0-9_])(?<=" + _CLIENT_COMMAND + _CHAR_UNIT + @"{0,600}\$\{?\k<construct>)";

	private const string _USE_OF_CONSTRUCT_BY_A_CLIENT =
		@"(?>[^\n]{0,400}\n(?:[^\n$]*(?:(?!" + _VALID_USE_OF_CONSTRUCT_BY_A_CLIENT + @")\$[^\n$]*)*\n){0,29}[^\n$]*(?:(?!" + _VALID_USE_OF_CONSTRUCT_BY_A_CLIENT + @")\$[^\n$]*)*)" + _VALID_USE_OF_CONSTRUCT_BY_A_CLIENT;

	private const string _VALID_USE_OF_ENCODED_BY_A_CLIENT =
		@"\$\{?\k<encoded>(?![A-Za-z0-9_])(?<=" + _CLIENT_COMMAND + _CHAR_UNIT + @"{0,600}\$\{?\k<encoded>)";

	private const string _USE_OF_ENCODED_BY_A_CLIENT =
		@"(?>[^\n]{0,400}\n(?:[^\n$]*(?:(?!" + _VALID_USE_OF_ENCODED_BY_A_CLIENT + @")\$[^\n$]*)*\n){0,29}[^\n$]*(?:(?!" + _VALID_USE_OF_ENCODED_BY_A_CLIENT + @")\$[^\n$]*)*)" + _VALID_USE_OF_ENCODED_BY_A_CLIENT;

	private const string _PIPELINE_START_WORD =
		@"(?:printf|echo|g?base64|basenc|busybox|openssl|uuencode|python3?|perl)(?![\w-])";

	private const string _SUBSTITUTION_OF_A_CLIENT_BEHIND =
		@"(?<=" + _CLIENT_COMMAND + _CHAR_UNIT + @"{0,600}(?:\$\(|`)[ \t]*)";

	private const string _SUBSTITUTION_OF_AN_ASSIGNMENT_BEHIND =
		@"(?<=^[ \t]*" + _DECLARATION_OPTIONAL + @"(?<assigned>[A-Za-z_]\w*)=(?:\$\(|`)[ \t]*)";

	internal const string EX_SH_12 =
		@"(?:\$\(|`)[ \t]*(?=" + _PIPELINE_START_WORD + @")(?:" + _SUBSTITUTION_OF_A_CLIENT_BEHIND + @"|" + _SUBSTITUTION_OF_AN_ASSIGNMENT_BEHIND + @")(?>" + _ENCODING_PIPELINE + @")(?(assigned)(?=" + _USE_OF_ASSIGNED_BY_A_CLIENT + @"))";

	private const string _HEADER_OPTION_ANY =
		_OPTION_START + @"(?:(?<construct>-" + _CURL_FLAG + @"*H)" + _GAP_OPT + @"|(?<construct>--(?:expand-)?header)(?:=|" + _GAP + @"))";

	internal const string EX_SH_13 =
		@"^[ \t]*(?:(?:export|readonly|local|declare)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?(?<variable>[A-Za-z_]\w*)=(?:[\x22\x27]?(?i:basic)(?![\w-])" + _DEFINITION_WINDOW + _HEADER_OPTION_ANY + @"[\x22\x27]?(?i:authorization)[ \t]*:[ \t]*\$\{?\k<variable>(?![A-Za-z0-9_])|[\x22\x27]?(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+" + _DEFINITION_WINDOW + _HEADER_OPTION_ANY + @"[\x22\x27]?\$\{?\k<variable>\}?[\x22\x27]?(?![\w$]))";

	private const string _BASIC_HEADER_EVIDENCE_AHEAD =
		@"(?=[ \t\n\\\x22\x27=]{1,16}(?i:authorization)[ \t]*:[ \t]*[\x22\x27]?(?i:basic)(?![\w-]))";

	private const string _ARRAY_OPTION =
		@"(?<=[ \t\n\x22\x27(=])(?<construct>-" + _CURL_FLAG + @"*(?:u|a|C)|--user|--netrc|--auth|--http-passwd|(?:-" + _CURL_FLAG + @"*H|--header)" + _BASIC_HEADER_EVIDENCE_AHEAD + @")(?![\w-])";

	internal const string EX_SH_14 =
		@"^[ \t]*(?:(?:declare|typeset|local)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?(?<array>[A-Za-z_]\w*)(?:\+?=\([^)]{0,600}?" + _ARRAY_OPTION + @"[^)]{0,600}?\)|\[[^\]\n]{1,40}\]\+?=[^\n]{0,300}?" + _ARRAY_OPTION + @")" + _DEFINITION_WINDOW + _CLIENT_COMMAND + _WINDOW_CLIENT + @"\x22?\$\{\k<array>\[(?:[@*]|[^\]\n]{1,40})\]\}";

	internal const string EX_SH_15 =
		@"^[ \t]*(?:(?:export|readonly|local)[ \t]+)?(?<construct>[A-Za-z_]\w*)=[\x22\x27]?(?<![\w-])https?://(?:(?<user>[^\s/@:\x27\x22$`\\]+):(?<credential>[^\s/@\x27\x22$`\\]+)|[^\s/@:\x27\x22]+:[^\s/@\x27\x22]+)@[^\s\x27\x22$`\\;&|<>()]*(?=[\x27\x22\s;&|)<>]|$)" + _USE_OF_CONSTRUCT_BY_A_CLIENT;

	private const string _HELPER_NAME =
		@"(?=[A-Za-z0-9_:-]*(?i:basic))(?=[A-Za-z0-9_:-]*(?i:auth|header|credential))[A-Za-z_][A-Za-z0-9_:-]{2,60}";

	internal const string EX_SH_16 =
		@"^[ \t]*(?:function[ \t]+(?<construct>" + _HELPER_NAME + @")(?:[ \t]*\(\))?|(?<construct>" + _HELPER_NAME + @")[ \t]*\(\))";

	internal const string EX_SH_17 =
		@"(?:\A(?:#![^\n]*\n)?[ \t]*#[^\n]*?(?i:auto[\- ]?generated|generated[ \t]+by)|\A(?:[^\n]*\n){0,9}[ \t]*#[^\n]*?(?i:do[ \t]+not[ \t]+(?:edit|modify|change|hand-edit)|changes[ \t]+will[ \t]+be[ \t]+(?:lost|overwritten)))";

	internal const string EX_SH_18 =
		@"^[ \t]*(?:\.|source)[ \t]+[^\n#]*(?:shunit2|bashunit|assert\.sh|bats-(?:assert|support)|shellspec_helper|roundup)";

	internal const string EX_SH_19 =
		@"^[ \t]*Describe[ \t]+[\x27\x22][^\n]*\n(?:(?![ \t]*Describe[ \t])[^\n]*\n){0,100}?[ \t]*It[ \t]+[\x27\x22]";

	private const string _EXCLUSION_TOKEN =
		@"(?:sudo[ \t]+(?:-\w*S|--stdin)(?![\w-])|sshpass(?![\w-])|ssh(?![\w-])|openssl[ \t][^\n]{0,200}?(?:-pass(?:in|out)|pass:))";

	private const string _COMMAND_POSITION =
		@"(?<=(?:" + _LINE_START + @"[ \t]*|[;&|({`][ \t]*|\$\([ \t]*|(?<![\w-])(?:then|do|else)[ \t]+))";

	private const string _COMMAND_PREFIX =
		@"(?:(?:sudo|env|time|command)[ \t]+(?:-[\w-]+[ \t]+)*)*";

	private const string _EXCLUSION_IN_COMMAND_POSITION =
		_COMMAND_POSITION + _COMMAND_PREFIX + _EXCLUSION_TOKEN;

	private const string _SEPARATOR_THEN_EXCLUSION =
		@"(?:[;&|({`][ \t]*|\$\([ \t]*|(?:then|do|else)(?<=(?<![\w-])(?:then|do|else))[ \t]+)" + _COMMAND_PREFIX + _EXCLUSION_TOKEN;

	private const string _TRACKED_VARIABLE_USE =
		@"(?!(?>[^\n]{0,400})\n(?:[^\n]{0,400}\n){0,39}?(?=[^\n]{0,400}\$\{?\k<name>(?![A-Za-z0-9_]))(?:(?=[ \t]*" + _COMMAND_PREFIX + _EXCLUSION_TOKEN + @")|(?=[^\n]{0,400}?" + _SEPARATOR_THEN_EXCLUSION + @")))";

	private const string _PROMPT_EXCLUSION_AFTER =
		@"(?![^\n]{0,120}?" + _SEPARATOR_THEN_EXCLUSION + @")";

	private const string _PROMPT_EXCLUSION_BEFORE_AND_AFTER =
		@"(?<!" + _EXCLUSION_IN_COMMAND_POSITION + @"[^\n;&]{0,120}?)" + _PROMPT_EXCLUSION_AFTER;

	private const string _PROMPT_EXCLUSION =
		_PROMPT_EXCLUSION_AFTER + _TRACKED_VARIABLE_USE;

	private const string _READ_OPTIONS =
		@"(?:[ \t]+-[A-Za-z]+(?:[ \t]+(?:\x27[^\x27\n]{0,200}\x27|\x22[^\x22\n]{0,200}\x22|[0-9]+))?){0,6}";

	private const string _READ_NAME_TAIL =
		@"[ \t]+(?<name>[A-Za-z_]\w*)[ \t]*(?:[;&|)<#\n]|$)";

	private const string _READ_NAME_AHEAD =
		@"(?>(?=" + _READ_OPTIONS + _READ_NAME_TAIL + @")|)";

	private const string _READ_NAME_AFTER_THE_PROMPT =
		@"(?>(?=[^\x27\x22\n]{0,200}[\x27\x22]" + _READ_OPTIONS + _READ_NAME_TAIL + @")|)";

	private const string _SHEBANG_PREFIX_BEHIND =
		@"\A#![ \t]*(?:[^\s/]*/)*(?:env[ \t]+(?:-\S*[ \t]+)*)?";

	private const string _FIRST_LINES_BEHIND =
		@"(?:[ \t][^\n]*)?\n(?>[^\n]{0,400}\n){0,8}[^\n]{0,160}";

	private const string _DIALECT_WINDOW_IN_CHARACTERS_FROM_THE_FILE_START =
		@"(?<=[\s\S]{2000})";

	private const string _NOT_KSH_FILE =
		@"(?:" + _DIALECT_WINDOW_IN_CHARACTERS_FROM_THE_FILE_START + @"|(?<=[^\n]{161})|(?<!" + _SHEBANG_PREFIX_BEHIND + @"(?:ksh93|ksh|mksh)" + _FIRST_LINES_BEHIND + @"))";

	private const string _NOT_ZSH_OR_KSH_FILE =
		@"(?:" + _DIALECT_WINDOW_IN_CHARACTERS_FROM_THE_FILE_START + @"|(?<=[^\n]{161})|(?<!" + _SHEBANG_PREFIX_BEHIND + @"(?:zsh|ksh93|ksh|mksh)" + _FIRST_LINES_BEHIND + @"))";

	private const string _LOOP_DONE_FROM_FILE =
		@"(?:[^\n]{0,400}\n){0,20}?[^\n]{0,400}?(?<![\w-])done[ \t]*<(?![ \t]*/dev/tty)";

	private const string _WHILE_OR_UNTIL_ON_THE_LINE =
		@"(?<=(?<![\w.$/-])(?:while|until)(?![\w-])[^\n]{0,300})";

	private const string _NOT_LOOP_READ_FROM_FILE =
		@"(?!" + _WHILE_OR_UNTIL_ON_THE_LINE + _LOOP_DONE_FROM_FILE + @")";

	private const string _NOT_AFTER_PIPE =
		@"(?<!(?<![|\\])\|(?!\|)[ \t]*(?:(?:while|until|if|then|do|!)[ \t]+)?(?:[A-Za-z_]\w*=\S*[ \t]+)*)";

	internal const string EX_SH_20 =
		_COMMAND_START + @"(?<signal>read)(?![\w-])(?>" + _WINDOW_NO_INPUT_REDIRECT + _OPTION_START + @"-[rsE]*s[A-Za-z0-9]*(?![\w-]))(?!" + _REST_HAS_INPUT_REDIRECT + @")" + _NOT_KSH_FILE + _READ_NAME_AHEAD + _PROMPT_EXCLUSION;

	internal const string EX_SH_21 =
		@"(?<![\w.$/-])(?<signal>stty)(?![\w-])[ \t](?:" + _STOP_AT_STTY + @"[^\n;&|]){0,300}?(?<=[ \t])-echo(?![\w-])(?:[^\n]*\n){0,5}?[^\n]{0,300}?(?<![\w.$/-])read(?![\w-])";

	internal const string EX_SH_22 =
		_COMMAND_START + _NOT_AFTER_PIPE + @"(?<signal>read)(?![\w-])(?>" + _WINDOW_NO_INPUT_REDIRECT + _OPTION_START + @"(?<name>" + _CRED_NAME + @")(?![\w=/.$-])|[ \t]+(?:-[A-Za-z]+[ \t]+)*[\x27\x22](?<name>" + _CRED_NAME + @")\?)(?!" + _REST_HAS_INPUT_REDIRECT + @")" + _NOT_LOOP_READ_FROM_FILE + _PROMPT_EXCLUSION;

	internal const string EX_SH_23 =
		@"(?<![\w.$/-])(?<signal>systemd-ask-password|(?:whiptail|dialog)(?![\w-])" + _WINDOW_DIALOG_TOOL + @"--passwordbox|zenity(?![\w-])" + _WINDOW_DIALOG_TOOL + @"(?:--password|--entry(?![\w-])" + _WINDOW_DIALOG_TOOL + @"--hide-text)|kdialog(?![\w-])" + _WINDOW_DIALOG_TOOL + @"--password)(?![\w-])" + _PROMPT_EXCLUSION_BEFORE_AND_AFTER;

	internal const string EX_SH_24 =
		@"(?<![\w.$/-])(?<signal>osascript)(?![\w-])(?:" + _STOP_AT_OSASCRIPT + @"[^\n]){0,600}?(?i:with[ \t]+hidden[ \t]+answer)";

	private const string _SIGNAL_USER_OPTION =
		@"(?:(?<signal>-" + _CURL_FLAG + @"*u)" + _GAP_OPT + @"|(?<signal>--user)(?:=|" + _GAP + @"))";

	private const string _CURL_TARGET =
		@"(?:/(?:[\w.+-]+/)*)?curl(?:\.exe)?(?![\w.-])";

	private const string _WGET_TARGET =
		@"(?:/(?:[\w.+-]+/)*)?wget(?:\.exe)?(?![\w.-])";

	private const string _ALIAS_LOOKBEHIND =
		@"(?<=^[ \t]*alias[ \t]+(?<kind>)(?<name>[A-Za-z_][\w-]{0,40})=)";

	private const string _VARIABLE_LOOKBEHIND =
		@"(?<=^[ \t]*" + _DECLARATION_OPTIONAL + @"(?<name>[A-Za-z_]\w{0,40})=)";

	private const string _COMMAND_VARIABLE_END =
		@"[\x22\x27]?(?=[ \t]*(?:[;&|#\n]|\z))";

	private const string _ALIAS_USE_WORD =
		@"\k<name>(?=[ \t])";

	private const string _VARIABLE_USE_WORD =
		@"\x22?\$\{?\k<name>\}?\x22?(?=[ \t])";

	private const string _USE_WORD_AS_CONSTRUCT_BEHIND =
		@"(?<=(?<construct>\x22?\$\{?\k<name>\}?\x22?|\k<name>))";

	private const string _CURL_ALIAS_OR_VARIABLE_TO_USE =
		@"=(?=[\x22\x27]?(?:(?:/(?:[\w.+-]+/)*)?curl|\$\(|`))(?:" + _ALIAS_LOOKBEHIND + @"[\x22\x27]?" + _CURL_TARGET + @"|" + _VARIABLE_LOOKBEHIND + @"[\x22\x27]?(?:" + _CURL_TARGET + @"|\$\([ \t]*(?:command[ \t]+-v|which|type[ \t]+-p)[ \t]+curl(?:\.exe)?[ \t]*\)|`[ \t]*(?:command[ \t]+-v|which|type[ \t]+-p)[ \t]+curl(?:\.exe)?[ \t]*`)" + _COMMAND_VARIABLE_END + @")" + _DEFINITION_WINDOW + _COMMAND_POSITION + @"(?(kind)" + _ALIAS_USE_WORD + @"|" + _VARIABLE_USE_WORD + @")";

	private const string _WGET_ALIAS_OR_VARIABLE_TO_USE =
		@"=(?=[\x22\x27]?(?:(?:/(?:[\w.+-]+/)*)?wget|\$\(|`))(?:" + _ALIAS_LOOKBEHIND + @"[\x22\x27]?" + _WGET_TARGET + @"|" + _VARIABLE_LOOKBEHIND + @"[\x22\x27]?(?:" + _WGET_TARGET + @"|\$\([ \t]*(?:command[ \t]+-v|which|type[ \t]+-p)[ \t]+wget(?:\.exe)?[ \t]*\)|`[ \t]*(?:command[ \t]+-v|which|type[ \t]+-p)[ \t]+wget(?:\.exe)?[ \t]*`)" + _COMMAND_VARIABLE_END + @")" + _DEFINITION_WINDOW + _COMMAND_POSITION + @"(?(kind)" + _ALIAS_USE_WORD + @"|" + _VARIABLE_USE_WORD + @")";

	internal const string EX_SH_25 =
		@"(?:" + _CURL + _WINDOW_CURL + @"|" + _CURL_ALIAS_OR_VARIABLE_TO_USE + _WINDOW_ANY_UNIT + @")" + _OPTION_START + _SIGNAL_USER_OPTION + _USER_VALUE_NO_COLON + _NOT_NON_BASIC_OR_NON_HTTP;

	internal const string EX_SH_26 =
		_WGET + _WINDOW_WGET + _OPTION_START + @"(?<signal>--ask-password)(?![\w-])";

	internal const string EX_SH_27 =
		@"(?<![\w.$/-])(?:read(?![\w-])(?>" + _WINDOW_NO_INPUT_REDIRECT + _OPTION_START + @"-[rseE]*p" + _GAP_OPT + @"[\x27\x22][^\x27\x22\n]{0,200}?(?<signal>(?i:pass(?:word|phrase))))" + _NOT_ZSH_OR_KSH_FILE + _READ_NAME_AFTER_THE_PROMPT + @"|read(?![\w-])[ \t]+(?:-[A-Za-z]+[ \t]+)*[\x27\x22](?<name>[A-Za-z_]\w*)\?[^\x27\x22\n]{0,200}?(?<signal>(?i:pass(?:word|phrase)))|(?:printf|echo)(?![\w-])(?>(?:" + _STOP_AT_ECHO_OR_PRINTF + @"[^\n]){0,300}?(?<signal>(?i:pass(?:word|phrase))))(?>[^\n]*)\n(?:[ \t]*\n){0,2}[ \t]*(?:read(?![\w-])|[^\n;&|]{0,300}?[;&|][ \t]*read(?![\w-]))" + _READ_NAME_AHEAD + @")" + _PROMPT_EXCLUSION;

	internal const string EX_SH_28 =
		_CLIENT_COMMAND + _CHAR_WINDOW_CLIENT + @"(?<signal>\$\{?" + _CRED_NAME + @"(?![A-Za-z0-9_])|\$\{[A-Za-z_]\w*:\?)";

	internal const string EX_SH_29 =
		@"(?<![\w.$/-])(?<signal>security[ \t]+find-(?:generic|internet)-password|secret-tool[ \t]+lookup|pass[ \t]+show|gopass[ \t]+show|vault[ \t]+kv[ \t]+get|aws[ \t]+secretsmanager[ \t]+get-secret-value|aws[ \t]+ssm[ \t]+get-parameter|az[ \t]+keyvault[ \t]+secret[ \t]+show|gcloud[ \t]+secrets[ \t]+versions[ \t]+access|op[ \t]+read|bw[ \t]+get[ \t]+password)(?![\w-])";

	internal const string EX_SH_30 =
		_CURL + _WINDOW_CURL + _OPTION_START + @"(?<signal>-" + _CURL_FLAG + @"*n(?![\w-])|--netrc(?:-file|-optional)?(?![\w-]))";

	internal const string EX_SH_31 =
		_COMMAND_START + @"(?<signal>read)(?![\w-])(?:[ \t]+-[A-Za-z]+(?:[ \t]+\S+)?)*[ \t]+(?<name>" + _CRED_NAME + @")[ \t]*<[ \t]*(?!<)";

	private const string _AUTH_TYPE_OTHER_THAN_BASIC =
		@"(?:" + _OPTION_START + @"-A(?:=|" + _GAP_OPT + @")|" + _OPTION_START + @"--auth-type(?:=|" + _GAP + @"))(?!basic(?![\w-]))";

	private const string _NOT_NON_HTTP_OR_OTHER_AUTH_TYPE =
		@"(?<!(?:" + _AUTH_TYPE_OTHER_THAN_BASIC + @"|" + _NON_HTTP_URL + @")" + _CHAR_UNIT + @"{0,600})(?!" + _CHAR_WINDOW_CONT + @"(?:" + _AUTH_TYPE_OTHER_THAN_BASIC + @"|" + _NON_HTTP_URL + @"))";

	internal const string EX_SH_32 =
		@"(?:" + _HTTPIE_COMMAND + _WINDOW_HTTPIE + _OPTION_START + @"(?:(?<construct>-a)" + _GAP_OPT + @"|(?<construct>--auth)(?:=|" + _GAP + @"))(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @"|" + _USER_VALUE_NO_COLON + @")|(?<construct>" + _HTTPIE_COMMAND + @")(?=" + _CHAR_WINDOW_HTTPIE + @"(?:" + _OPTION_START + _HEADER_VALUE + @"|" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @")))" + _NOT_NON_HTTP_OR_OTHER_AUTH_TYPE;

	internal const string EX_SH_33 =
		_HTTPIE_COMMAND + _WINDOW_HTTPIE + _OPTION_START + @"(?:(?<signal>-a)" + _GAP_OPT + @"|(?<signal>--auth)(?:=|" + _GAP + @"))" + _USER_VALUE_NO_COLON + _NOT_NON_HTTP_OR_OTHER_AUTH_TYPE;

	private const string _ARIA_CHALLENGE_ON =
		_OPTION_START + @"--http-auth-challenge(?:=true(?![\w-])|(?![\w=-]))";

	private const string _NOT_ARIA_CHALLENGE =
		@"(?<!" + _ARIA_CHALLENGE_ON + _CHAR_UNIT + @"{0,600})(?!" + _CHAR_WINDOW_CONT + _ARIA_CHALLENGE_ON + @")";

	private const string _ARIA_PASSWORD_VALUE =
		@"(?:(?!~)(?<credential>" + _PASS_PART + @")" + _WORD_END + @"|" + _USER_VALUE_EXPANDING + @")";

	internal const string EX_SH_34 =
		@"(?:" + _ARIA2C + _WINDOW_ARIA2C + _OPTION_START + @"(?:(?<construct>--http-passwd)(?:=|" + _GAP + @")" + _ARIA_PASSWORD_VALUE + @"|(?<construct>--header)(?:=|" + _GAP + @")" + _HEADER_VALUE + @")|(?<construct>" + _ARIA2C + @")(?=" + _CHAR_WINDOW_ARIA2C + @"(?:" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @")))" + _NOT_ARIA_CHALLENGE;

	internal const string EX_SH_35 =
		@"(?:" + _LWP + _WINDOW_LWP + _OPTION_START + @"(?:(?<construct>-C)" + _GAP_OPT + @"(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @")|(?<construct>-H)" + _GAP_OPT + _HEADER_VALUE + @")|(?<construct>" + _LWP + @")(?=" + _CHAR_WINDOW_LWP + @"(?:" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @"))|" + _ARIA2C + _WINDOW_ARIA2C + _OPTION_START + @"(?<construct>--http-passwd)(?:=|" + _GAP + @")" + _ARIA_PASSWORD_VALUE + @"(?:(?<=" + _ARIA_CHALLENGE_ON + _CHAR_UNIT + @"{0,600})|(?=" + _CHAR_WINDOW_ARIA2C + _ARIA_CHALLENGE_ON + @")))";

	private const string _WGET_EXECUTE_PASSWORD_LITERAL =
		_EXECUTE_OPTION + @"[\x22\x27]?(?:http_)?passw(?:or)?d[ \t]*=[ \t]*(?!~)(?<credential>" + _PASS_PART + @")[\x22\x27]?" + _WORD_END;

	private const string _WGET_EXECUTE_CREDENTIAL_ANY =
		_EXECUTE_OPTION + @"[\x22\x27]?(?:http_)?(?:user|passw(?:or)?d)[ \t]*=";

	private const string _WGET_EXECUTE_LOOKAHEADS =
		@"(?:(?=" + _WINDOW_WGET + _WGET_EXECUTE_PASSWORD_LITERAL + @")|(?=" + _WINDOW_WGET + _WGET_EXECUTE_CREDENTIAL_ANY + @"))";

	internal const string EX_SH_36 =
		@"(?<construct>" + _WGET + @")(?=" + _WINDOW_WGET + _WGET_PREEMPTIVE_SWITCH + @")" + _WGET_EXECUTE_LOOKAHEADS;

	internal const string EX_SH_37 =
		@"(?<construct>" + _WGET + @")" + _WGET_EXECUTE_LOOKAHEADS;

	private const string _REQUEST_LINE =
		@"(?:(?<![\w-])|(?<=\\n))(?:GET|POST|PUT|DELETE|HEAD|PATCH|OPTIONS)[ \t]+\S+[ \t]+HTTP/1\.[01]";

	private const string _REQUEST_HEADER_TEXT =
		@"(?:(?<![\w-])|(?<=\\n))(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+(?:(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22\x27\s\\;&|)]|$)|\$|%s)(?<!(?<![\w.$/-])(?:grep|egrep|fgrep|awk|sed|case)(?![\w-])[^\n]{0,300})";

	private const string _NOT_LISTENING_SIDE =
		@"(?!(?:nc|ncat|netcat|socat)(?![\w.-])[^\n;&|]{0,200}?(?:(?<=[ \t])-[A-Za-z]*l[A-Za-z]*(?![\w-])|--listen(?![\w-])|TCP4?-LISTEN))";

	private const string _HAND_BUILT_REQUEST_SINK =
		@"(?:(?<construct><>|>)(?=[ \t]*[\x22\x27]/dev/tcp/)|(?<!(?:<>|>)[ \t]*[\x22\x27])(?<construct>/dev/tcp)(?=/)|" + _COMMAND_START + _NOT_LISTENING_SIDE + @"(?<construct>nc|ncat|netcat|socat)(?![\w.-])|(?<construct>s_client)(?<=(?<![\w.$/-])openssl[ \t]+s_client)(?![\w-]))";

	private const string _SINK_WITHIN_TWELVE_LINES =
		@"(?:(?<=" + _HAND_BUILT_REQUEST_SINK + @"[^\n]{0,400}(?:\n[^\n]{0,400}){0,12})|(?=(?:[^\n]{0,400}\n){0,12}?[^\n]{0,400}?" + _HAND_BUILT_REQUEST_SINK + @"))";

	private const string _REQUEST_LINE_WITHIN_TWELVE_LINES =
		@"(?:(?<=" + _REQUEST_LINE + @"[^\n]{0,400}(?:\n[^\n]{0,400}){0,12})|(?=(?:[^\n]{0,400}\n){0,12}?[^\n]{0,400}?" + _REQUEST_LINE + @"))";

	internal const string EX_SH_38 =
		_REQUEST_HEADER_TEXT + _SINK_WITHIN_TWELVE_LINES + _REQUEST_LINE_WITHIN_TWELVE_LINES;

	private const string _CURL_CONFIGURATION_TEXT =
		@"(?:--?)?(?:user[ \t]*[=:\t ][ \t]*[\x22\x27]?[^\x22\x27\s:]*:[^\x22\x27\s]+|header[ \t]*[=:\t ][ \t]*[\x22\x27]?(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+\S+)";

	private const string _WGET_CONFIGURATION_TEXT =
		@"(?:http_)?(?:passw(?:or)?d|user)[ \t]*=";

	private const string _CONFIGURATION_PATH_USE =
		@"(?:[\x22\x27]?\$\{?(?<cfgvar>[A-Za-z_]\w*)\}?[\x22\x27]?|[\x22\x27]?(?<cfglit>[\w./~+-]{1,200})[\x22\x27]?)(?![\w.$-])";

	private const string _CONFIGURATION_PATH_REFERENCE =
		@"(?:[\x22\x27]?\$\{?\k<cfgvar>\}?[\x22\x27]?|[\x22\x27]?\k<cfglit>[\x22\x27]?)(?![\w.-])";

	private const string _WRITE_TO_A_PATH =
		@"(?:>>?[ \t]*|(?<![\w.$/-])tee(?:[ \t]+-a)?[ \t]+)" + _CONFIGURATION_PATH_USE;

	private const string _HEREDOC_OPENING =
		@"<<-?[ \t]*[\x22\x27]?[A-Za-z_]\w*[\x22\x27]?";

	private const string _HERE_DOCUMENT_BODY_LINE_BEHIND =
		@"(?<=(?<![^\n])[^\n]{0,300}?(?:" + _WRITE_TO_A_PATH + @"[^\n]{0,200}?" + _HEREDOC_OPENING + @"|" + _HEREDOC_OPENING + @"[^\n]{0,200}?" + _WRITE_TO_A_PATH + @")[^\n]{0,200}\n(?>[^\n]{0,400}\n){0,38}[ \t]*\k<cfg>)";

	private const string _CLOSING_WORD =
		@"(?:[})]|done(?![\w-]))";

	private const string _QUOTED_CONFIGURATION_WRITTEN_BY_PRINTF_OR_ECHO_ON_ITS_LINE_OR_BY_A_GROUP_REDIRECTED_BELOW =
		@"(?<=[\x22\x27]\k<cfg>)(?<=(?<![\w.$/-])(?:printf|echo)(?![\w-])[^\n]{0,300})(?=(?:[^\n]{0,300}?|(?>[^\n]{0,300})\n(?:(?![ \t]*" + _CLOSING_WORD + @")[^\n]{0,400}\n){0,29}?[ \t]*" + _CLOSING_WORD + @"[ \t]*(?:\|[ \t]*)?)" + _WRITE_TO_A_PATH + @")";

	private const string _CURL_CONFIGURATION_OPTION =
		@"(?:(?<construct>-" + _CURL_FLAG + @"*K)" + _GAP_OPT + @"|(?<construct>--config)(?:=|" + _GAP + @"))";

	private const string _CURL_READS_THE_CONFIGURATION_WRITTEN_ABOVE =
		@"(?=" + _DEFINITION_WINDOW + _CURL + _WINDOW_CURL + _OPTION_START + _CURL_CONFIGURATION_OPTION + _CONFIGURATION_PATH_REFERENCE + @")";

	private const string _CURL_READS_THE_CONFIGURATION_FROM_ITS_OWN_INPUT =
		_CURL + _WINDOW_CURL + _OPTION_START + _CURL_CONFIGURATION_OPTION + @"(?:(?<![\w.-])-(?![\w.-])(?=[^\n]{0,400}?" + _HEREDOC_OPENING + @"[^\n]{0,200}\n(?:[^\n]{0,400}\n){0,39}?[ \t]*" + _CURL_CONFIGURATION_TEXT + @")|<\((?=[^)\n]{0,400}?" + _CURL_CONFIGURATION_TEXT + @"))";

	internal const string EX_SH_39 =
		@"(?:" + _CURL_READS_THE_CONFIGURATION_FROM_ITS_OWN_INPUT + @"|(?<cfg>" + _CURL_CONFIGURATION_TEXT + @")(?:" + _QUOTED_CONFIGURATION_WRITTEN_BY_PRINTF_OR_ECHO_ON_ITS_LINE_OR_BY_A_GROUP_REDIRECTED_BELOW + @"|" + _HERE_DOCUMENT_BODY_LINE_BEHIND + @")" + _CURL_READS_THE_CONFIGURATION_WRITTEN_ABOVE + @")";

	private const string _WGETRC_ANCHOR =
		@"(?:(?<![\w.$-])(?<construct>WGETRC)=" + _CONFIGURATION_PATH_REFERENCE + @"|" + _WGET + _WINDOW_WGET + _OPTION_START + @"(?<construct>--config)(?:=|" + _GAP + @")" + _CONFIGURATION_PATH_REFERENCE + @")";

	private const string _WGETRC_ANCHOR_WITHIN_THE_DEFINITION_WINDOW =
		@"(?:(?<=" + _WGETRC_ANCHOR + @"[^\n]{0,400}(?:\n[^\n]{0,400}){0,29})|(?=" + _DEFINITION_WINDOW + _WGETRC_ANCHOR + @"))";

	internal const string EX_SH_40 =
		@"(?<cfg>" + _WGET_CONFIGURATION_TEXT + @")(?:" + _QUOTED_CONFIGURATION_WRITTEN_BY_PRINTF_OR_ECHO_ON_ITS_LINE_OR_BY_A_GROUP_REDIRECTED_BELOW + @"|" + _HERE_DOCUMENT_BODY_LINE_BEHIND + @")" + _WGETRC_ANCHOR_WITHIN_THE_DEFINITION_WINDOW;

	private const string _CURL_USE_EVIDENCE =
		@"(?:" + _WINDOW_ANY_UNIT + @"(?:" + _OPTION_START + _CURL_USER_OPTION + @"(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @"|" + _USER_VALUE_NO_COLON + @")|" + _OPTION_START + _CURL_HEADER_OPTION + _HEADER_VALUE + @")|(?=" + _CHAR_WINDOW_CONT + @"(?:" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @"))" + _USE_WORD_AS_CONSTRUCT_BEHIND + @")";

	private const string _WGET_USE_EVIDENCE =
		@"(?:" + _WINDOW_ANY_UNIT + _OPTION_START + @"(?<construct>--(?:http-)?(?:user|passw(?:or)?d))(?:=|" + _GAP + @")|(?=" + _CHAR_WINDOW_CONT + @"(?:" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @"))" + _USE_WORD_AS_CONSTRUCT_BEHIND + @")";

	internal const string EX_SH_41 =
		_CURL_ALIAS_OR_VARIABLE_TO_USE + _CURL_USE_EVIDENCE + _NOT_NON_BASIC_OR_NON_HTTP;

	internal const string EX_SH_42 =
		_WGET_ALIAS_OR_VARIABLE_TO_USE + _WGET_USE_EVIDENCE;

	private const string _ANYAUTH =
		_OPTION_START + @"--anyauth(?![\w-])";

	internal const string EX_SH_43 =
		_CURL + @"(?=" + _CHAR_WINDOW_CLIENT + _ANYAUTH + @")" + _WINDOW_CURL + _OPTION_START + _CURL_USER_OPTION + @"(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @")" + _NOT_NON_BASIC_OR_NON_HTTP_EXCEPT_ANYAUTH;

	private const string _ASSIGNED_PAIR_VALUE =
		@"[\x22\x27]?(?![^\s\x27\x22;&|]{0,200}?" + _PORT_TOKEN + @")(?!" + _HOST_EXPANSION + @"[^\s:\x27\x22]*:(?!\$\{?" + _CRED_NAME + @"(?![A-Za-z0-9_])))[A-Za-z_$%{][^\s:\x27\x22|;&]*:(?!//)(?!\d{1,5}(?![\w$]))[^\s:\x27\x22|;&/\\]";

	private const string _ENCODER_STATEMENT_ON_PAIR =
		@"(?:(?<![\w.$/-])(?<construct>printf|echo)(?![\w-])(?:" + _STOP_AT_ECHO_OR_PRINTF + @"[^\n|;&]){0,400}?\$\{?\k<pair>(?![A-Za-z0-9_])\}?[\x22\x27]?" + _WINDOW_ECHO_OR_PRINTF + _GAP_OPT + @"\|(?!\|)" + _GAP_OPT + @"(?:" + _FILTER_STAGE + @"){0,2}(?<consumer>" + _ENCODER + @")" + _DECODE_REFUSAL + @"|(?<![\w.$/-])(?<construct>" + _ENCODER + @")" + _DECODE_REFUSAL + @"(?>(?:[ \t]+-[\w-]+)*)[ \t]*<<<[ \t]*[\x22\x27]?\$\{?\k<pair>(?![A-Za-z0-9_]))";

	internal const string EX_SH_44 =
		@"=(?=[\x22\x27]?[A-Za-z_$%{][^\s:\x27\x22|;&]*:)(?<=^[ \t]*" + _DECLARATION_OPTIONAL + @"(?<pair>[A-Za-z_]\w*)=)" + _ASSIGNED_PAIR_VALUE + _DEFINITION_LINES + @"(?:[^\n]{0,400}?" + _CLIENT_COMMAND + _CHAR_UNIT + @"{0,600}?(?:\$\(|`)[ \t]*" + _ENCODER_STATEMENT_ON_PAIR + @"|[ \t]*" + _DECLARATION_OPTIONAL + @"(?<encoded>[A-Za-z_]\w*)=(?:\$\(|`)[ \t]*" + _ENCODER_STATEMENT_ON_PAIR + _USE_OF_ENCODED_BY_A_CLIENT + @")";

	internal const string EX_SH_45 =
		_COMMAND_START + @"(?<signal>read)(?![\w-])(?<=(?<![\w.$/-])(?:while|until)[ \t]+(?:[A-Za-z_]\w*=\S*[ \t]+)*read)(?=[^\n;&|<]{0,300}(?<=[ \t])(?<name>" + _CRED_NAME + @")(?![\w=/.$-])[ \t]*(?:[;&|)<#\n]|$))(?=" + _LOOP_DONE_FROM_FILE + @")";

	private const string _STRING_OPTION =
		@"(?:-[A-Za-z]*u[ \t]+(?=\S*[:$`])|--user[ \t]+(?=\S*[:$`])|--netrc(?:-file|-optional)?(?![\w-])|--(?:user|password|http-user|http-password)=|-a[ \t]+\S|--auth[ \t=]+\S|--http-passwd(?![\w-]))";

	internal const string EX_SH_46 =
		@"^[ \t]*" + _DECLARATION_OPTIONAL + @"(?<construct>[A-Za-z_]\w*)=[\x22\x27]?(?=[^\n\x27\x22]{0,300}?(?<=[ \t\x22\x27=])" + _STRING_OPTION + @")" + _DEFINITION_WINDOW + _CLIENT_COMMAND + _WINDOW_CLIENT + @"\$\{?\k<construct>(?![A-Za-z0-9_])";

	#endregion

	[GeneratedRegex(EX_SH_01, _OPTIONS, matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	internal static partial Regex Ex01();

	[GeneratedRegex(EX_SH_02, _OPTIONS, matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	internal static partial Regex Ex02();

	[GeneratedRegex(EX_SH_03, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex03();

	[GeneratedRegex(EX_SH_04, _OPTIONS, matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	internal static partial Regex Ex04();

	[GeneratedRegex(EX_SH_05, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex05();

	[GeneratedRegex(EX_SH_06, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex06();

	[GeneratedRegex(EX_SH_07, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex07();

	[GeneratedRegex(EX_SH_08, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex08();

	[GeneratedRegex(EX_SH_09, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex09();

	[GeneratedRegex(EX_SH_10, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex10();

	[GeneratedRegex(EX_SH_11, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex11();

	[GeneratedRegex(EX_SH_12, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex12();

	[GeneratedRegex(EX_SH_13, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex13();

	[GeneratedRegex(EX_SH_14, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex14();

	[GeneratedRegex(EX_SH_15, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex15();

	[GeneratedRegex(EX_SH_16, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex16();

	[GeneratedRegex(EX_SH_17, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex17();

	[GeneratedRegex(EX_SH_18, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex18();

	[GeneratedRegex(EX_SH_19, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex19();

	[GeneratedRegex(EX_SH_20, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex20();

	[GeneratedRegex(EX_SH_21, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex21();

	[GeneratedRegex(EX_SH_22, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex22();

	[GeneratedRegex(EX_SH_23, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex23();

	[GeneratedRegex(EX_SH_24, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex24();

	[GeneratedRegex(EX_SH_25, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex25();

	[GeneratedRegex(EX_SH_26, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex26();

	[GeneratedRegex(EX_SH_27, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex27();

	[GeneratedRegex(EX_SH_28, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex28();

	[GeneratedRegex(EX_SH_29, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex29();

	[GeneratedRegex(EX_SH_30, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex30();

	[GeneratedRegex(EX_SH_31, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex31();

	[GeneratedRegex(EX_SH_32, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex32();

	[GeneratedRegex(EX_SH_33, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex33();

	[GeneratedRegex(EX_SH_34, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex34();

	[GeneratedRegex(EX_SH_35, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex35();

	[GeneratedRegex(EX_SH_36, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex36();

	[GeneratedRegex(EX_SH_37, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex37();

	[GeneratedRegex(EX_SH_38, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex38();

	[GeneratedRegex(EX_SH_39, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex39();

	[GeneratedRegex(EX_SH_40, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex40();

	[GeneratedRegex(EX_SH_41, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex41();

	[GeneratedRegex(EX_SH_42, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex42();

	[GeneratedRegex(EX_SH_43, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex43();

	[GeneratedRegex(EX_SH_44, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex44();

	[GeneratedRegex(EX_SH_45, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex45();

	[GeneratedRegex(EX_SH_46, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex46();
}
