using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The shared vocabulary of the `Shell` pattern classes, as constants the class expressions are built from. Every window is bounded, required and over
/// units rather than runs, and none sits inside an optional group; every alternative opens on a literal or a word start; a refusal is placed after the positive
/// match where the positive part is cheaper, so it runs only on a success. A credential capture group excludes every expansion character, so no capture can
/// hold text that the shell would expand.
/// </summary>
internal static partial class ShellPatternExpressions
{
	/// <summary>
	/// A zero-width point that always matches and gives backtracking exactly one way to fail: one of a word boundary and a non-boundary holds at every position.
	/// It is appended inside the body of a lazy bounded loop over a group, <c>(?:body POINT){m,n}?</c>, because the regular-expression source generator of .NET 10
	/// (generator 10.0.14.15411) emits a defective cleanup of its backtracking stack for such a loop when the body has no backtracking construct of its own
	/// (fixed upstream in dotnet/runtime PR 129628, not backported to release/10.0). It changes no match.
	/// </summary>
	private const string _BACKTRACK_POINT = @"(?:\b|\B)";

	/// <summary>What may stand between two words of one command: a run of blanks, or a backslash-line-feed and the next line's leading blanks. Atomic and bounded.</summary>
	private const string _GAP =
		@"(?>(?:[ \t]+|\\\n[ \t]*){1,16})";

	/// <summary>The same gap with zero units allowed, for a short option whose value is attached.</summary>
	private const string _GAP_OPT =
		@"(?>(?:[ \t]+|\\\n[ \t]*){0,16})";

	/// <summary>The start of a line.</summary>
	private const string _LINE_START =
		@"(?<![^\n])";

	private const string _SQ_STRING =
		@"\x27[^\x27]{0,4096}\x27";

	private const string _DQ_STRING =
		@"\x22(?>[^\x22\\]|\\[\s\S]){0,4096}\x22";

	/// <summary>One unit of a command window: a character that does not end the command, a quoted string as one atomic unit, or a backslash and the character it escapes.</summary>
	private const string _UNIT =
		@"(?>[^\n;&|\x27\x22\\]|" + _SQ_STRING + @"|" + _DQ_STRING + @"|\\[\s\S])";

	/// <summary>One token of a command window: a run of word characters, a run of blanks, one grouping character, a quoted string or an escaped character.</summary>
	private const string _TOKEN =
		@"(?>[^\n;&|\x27\x22\\ \t(){}`!]+|[ \t]+|[(){}`!]|" + _SQ_STRING + @"|" + _DQ_STRING + @"|\\[\s\S])";

	/// <summary>One character of the character window.</summary>
	private const string _CHAR_UNIT =
		@"(?>[^\n;&|\\]|\\\n|\\)";

	/// <summary>One token of the character window used by look-aheads that must see inside a string; a backslash-line-feed counts as one unit so a window spans a continuation.</summary>
	private const string _CHAR_TOKEN =
		@"(?>[\w-]+|[^\n;&|\\\w-]|\\\n|\\)";

	/// <summary>Refuses to step over the command word of another covered client, so a window never leaves its own command by a word.</summary>
	private const string _STOP_AT_CLIENT =
		@"(?!(?<![\w.$/-])(?:(?:curl|wget)(?:\.exe)?(?![\w.-])|(?:aria2c|lwp-request)(?![\w.-])|https?(?=[ \t])))";

	private const string _STOP_AT_ECHO_OR_PRINTF =
		@"(?!(?<![\w.$/-])(?:echo|printf)(?![\w-]))";

	/// <summary>The window of one `curl` command: tokens, bounded, lazy and never inside an optional group.</summary>
	private const string _WINDOW_CURL =
		@"(?:(?:" + _STOP_AT_CLIENT + _TOKEN + @")" + _BACKTRACK_POINT + @"){0,300}?";

	private const string _WINDOW_WGET =
		@"(?:(?:" + _STOP_AT_CLIENT + _TOKEN + @")" + _BACKTRACK_POINT + @"){0,300}?";

	private const string _WINDOW_HTTPIE =
		@"(?:(?:" + _STOP_AT_CLIENT + _TOKEN + @")" + _BACKTRACK_POINT + @"){0,300}?";

	private const string _WINDOW_ARIA2C =
		@"(?:(?:" + _STOP_AT_CLIENT + _TOKEN + @")" + _BACKTRACK_POINT + @"){0,300}?";

	private const string _WINDOW_LWP =
		@"(?:(?:" + _STOP_AT_CLIENT + _TOKEN + @")" + _BACKTRACK_POINT + @"){0,300}?";

	private const string _WINDOW_CLIENT =
		@"(?:(?:" + _STOP_AT_CLIENT + _TOKEN + @")" + _BACKTRACK_POINT + @"){0,300}?";

	private const string _WINDOW_ECHO_OR_PRINTF =
		@"(?:(?:" + _STOP_AT_ECHO_OR_PRINTF + _TOKEN + @")" + _BACKTRACK_POINT + @"){0,300}?";

	private const string _WINDOW_ANY_UNIT =
		@"(?:(?:" + _TOKEN + @")" + _BACKTRACK_POINT + @"){0,300}?";

	/// <summary>The character window every look-ahead and look-behind over a command's text uses; it spans a continuation and stops at an unescaped separator.</summary>
	private const string _CHAR_WINDOW_CONT =
		@"(?:" + _CHAR_TOKEN + _BACKTRACK_POINT + @"){0,600}?";

	private const string _CHAR_WINDOW_CURL =
		@"(?:(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @")" + _BACKTRACK_POINT + @"){0,600}?";

	private const string _CHAR_WINDOW_WGET =
		@"(?:(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @")" + _BACKTRACK_POINT + @"){0,600}?";

	private const string _CHAR_WINDOW_HTTPIE =
		@"(?:(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @")" + _BACKTRACK_POINT + @"){0,600}?";

	private const string _CHAR_WINDOW_ARIA2C =
		@"(?:(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @")" + _BACKTRACK_POINT + @"){0,600}?";

	private const string _CHAR_WINDOW_LWP =
		@"(?:(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @")" + _BACKTRACK_POINT + @"){0,600}?";

	private const string _CHAR_WINDOW_CLIENT =
		@"(?:(?:" + _STOP_AT_CLIENT + _CHAR_TOKEN + @")" + _BACKTRACK_POINT + @"){0,600}?";

	private const string _CHAR_UNIT_WITHIN_THE_COMMAND =
		@"(?:" + _STOP_AT_CLIENT + _CHAR_UNIT + @")";

	/// <summary>The lines between a definition and its use: thirty lines of at most four hundred characters.</summary>
	private const string _DEFINITION_LINES =
		@"[^\n]{0,400}\n(?:(?:[^\n]{0,400}\n)" + _BACKTRACK_POINT + @"){0,29}?";

	/// <summary>The definition window: from a definition to its use, bounded in lines and in characters per line.</summary>
	private const string _DEFINITION_WINDOW =
		_DEFINITION_LINES + @"[^\n]{0,400}?";

	/// <summary>The definition window read backwards from a use to its definition.</summary>
	private const string _DEFINITION_WINDOW_BEHIND =
		@"[^\n]{0,400}\n(?>[^\n]{0,400}\n){0,29}(?>[^\n]{0,200})";

	/// <summary>The start of a command word: not part of a longer word, optionally preceded by a path.</summary>
	private const string _COMMAND_START =
		@"(?<![\w.$/-])(?:/(?:[\w.+-]+/)*)?";

	/// <summary>The `curl` command word, with an optional `.exe`, never a part of a longer word.</summary>
	private const string _CURL =
		_COMMAND_START + @"curl(?:\.exe)?(?![\w.-])";

	/// <summary>The `wget` command word, with an optional `.exe`, never a part of a longer word.</summary>
	private const string _WGET =
		_COMMAND_START + @"wget(?:\.exe)?(?![\w.-])";

	/// <summary>The `http` or `https` command word of HTTPie, which must be followed by a blank so a URL scheme is never a command.</summary>
	private const string _HTTPIE_COMMAND =
		_COMMAND_START + @"https?(?=[ \t])";

	private const string _ARIA2C =
		_COMMAND_START + @"aria2c(?![\w.-])";

	private const string _LWP =
		_COMMAND_START + @"lwp-request(?![\w.-])";

	/// <summary>The command word of any covered client.</summary>
	private const string _CLIENT_COMMAND =
		_COMMAND_START + @"(?:(?:curl|wget)(?:\.exe)?(?![\w.-])|(?:aria2c|lwp-request)(?![\w.-])|https?(?=[ \t]))";

	/// <summary>An option word starts after a blank or a line feed.</summary>
	private const string _OPTION_START =
		@"(?<=[ \t\n])";

	/// <summary>The flag-only letters of `curl`, the letters that may precede another short option in one cluster.</summary>
	private const string _CURL_FLAG =
		@"[aqfGIpk046jlLMnNZ#JORSis231BvV]";

	/// <summary>A shell word ends before white space or one of the characters that end a word.</summary>
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

	/// <summary>A variable name that names a credential.</summary>
	private const string _CRED_NAME =
		@"(?:[A-Za-z0-9_]*(?i:passw(?:or)?d|secret|credential|creds?|token)[A-Za-z0-9_]*|(?:[A-Za-z0-9]+_)*(?i:pass|pw|pwd|apikey)(?![A-Za-z0-9])|[a-z][a-z0-9]*Pass(?![a-z0-9]))";

	private const string _NON_HTTP_SCHEME_WORDS =
		@"(?:ftps?|sftp|scp|smtps?|imaps?|pop3s?|ldaps?|telnet|smbs?|tftp|dict|gopher|mqtts?|rtsp|file)";

	/// <summary>A URL whose scheme is not HTTP.</summary>
	private const string _NON_HTTP_URL =
		@"(?<![\w-])" + _NON_HTTP_SCHEME_WORDS + @"://";

	/// <summary>A method flag that does not send Basic on the first request.</summary>
	private const string _METHOD_FLAG =
		_OPTION_START + @"--(?:digest|ntlm|ntlm-wb|negotiate|anyauth|aws-sigv4)(?![\w-])";

	private const string _METHOD_FLAG_EXCEPT_ANYAUTH =
		_OPTION_START + @"--(?:digest|ntlm|ntlm-wb|negotiate|aws-sigv4)(?![\w-])";

	/// <summary>An optional declaration keyword with its flags.</summary>
	private const string _DECLARATION_OPTIONAL =
		@"(?:(?:export|readonly|local|declare|typeset)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?";

	private const string _URL_WORD_OF_A_VARIABLE_HOLDING_A_NON_HTTP_SCHEME =
		@"(?<=[ \t\n])\x22?\$\{?(?<urlvar>[A-Za-z_]\w*)\}?(?>/[^\s\x22;&|<>)]{0,200})?\x22?" + _WORD_END + @"(?<=" + _LINE_START + @"[ \t]*" + _DECLARATION_OPTIONAL + @"\k<urlvar>=[\x22\x27]?" + _NON_HTTP_SCHEME_WORDS + @"://" + _DEFINITION_WINDOW_BEHIND + @")";

	/// <summary>Refuses a command that carries a method flag or a non-HTTP URL, before or after the match, in the command or in a variable assigned within the definition window.</summary>
	private const string _NOT_NON_BASIC_OR_NON_HTTP =
		@"(?<!(?:" + _METHOD_FLAG + @"|" + _NON_HTTP_URL + @")" + _CHAR_UNIT_WITHIN_THE_COMMAND + @"{0,600})(?!" + _CHAR_WINDOW_CLIENT + @"(?:" + _METHOD_FLAG + @"|" + _NON_HTTP_URL + @"|" + _URL_WORD_OF_A_VARIABLE_HOLDING_A_NON_HTTP_SCHEME + @"))";

	private const string _NOT_NON_BASIC_OR_NON_HTTP_EXCEPT_ANYAUTH =
		@"(?<!(?:" + _METHOD_FLAG_EXCEPT_ANYAUTH + @"|" + _NON_HTTP_URL + @")" + _CHAR_UNIT_WITHIN_THE_COMMAND + @"{0,600})(?!" + _CHAR_WINDOW_CLIENT + @"(?:" + _METHOD_FLAG_EXCEPT_ANYAUTH + @"|" + _NON_HTTP_URL + @"|" + _URL_WORD_OF_A_VARIABLE_HOLDING_A_NON_HTTP_SCHEME + @"))";

	/// <summary>The literal shapes of a user option value, a bare word, a single-quoted word, a double-quoted word or adjacent parts, none of which expands; captures the user and the password half.</summary>
	private const string _USER_VALUE_LITERAL =
		@"(?:\x27(?<user>[^\x27:\n]*):(?<credential>[^\x27\n]+)\x27" + _WORD_END + @"|\x22(?<user>[^\x22:$`\\\n]*):(?<credential>[^\x22$`\\\n]+)\x22" + _WORD_END + @"|(?!~)(?<user>" + _USER_PART + @"):(?<credential>" + _PASS_PART + @")" + _WORD_END + @")";

	/// <summary>Any other word that holds a colon or an expansion: read as a gated positive, with no credential position.</summary>
	private const string _USER_VALUE_EXPANDING =
		@"(?=(?:[^\s\x27\x22\\;&|<>:$`]|\\[\s\S]|\x27[^\x27:$`]{0,512}\x27|\x22[^\x22\\:$`]{0,512}\x22){0,600}(?:[:$`]|[\x27\x22][^\x27\x22]{0,512}[:$`]))(?>(?:[^\s\x27\x22\\;&|<>]|\\[\s\S]|" + _SQ_STRING + @"|" + _DQ_STRING + @")+)";

	/// <summary>A literal word holding no colon, no expansion and no backslash: the shape of a prompt.</summary>
	private const string _USER_VALUE_NO_COLON =
		@"(?!-)(?:\x27[^\x27:$`\\\n]+\x27|\x22[^\x22:$`\\\n]+\x22|(?!~)[^\s\x27\x22$`\\:;&|<>(){}*?\[\]=](?:" + _BARE_NC + @")*)" + _WORD_END;

	/// <summary>The user option of `curl`, short (attached, separate or last in a cluster) or long (separate or with an equals sign).</summary>
	private const string _CURL_USER_OPTION =
		@"(?:(?<construct>-" + _CURL_FLAG + @"*u)" + _GAP_OPT + @"|(?<construct>--user)(?:=|" + _GAP + @"))";

	/// <summary>The part of a URL after its userinfo: host, port and path, up to the end of the word.</summary>
	private const string _URL_AFTER_USERINFO =
		@"@[^\s\x27\x22$`\\;&|<>()]*(?=[\x27\x22\s;&|)<>]|$)";

	/// <summary>
	/// An HTTP URL whose userinfo is literal; captures the user and the password, which exclude every expansion character. Where the URL opens right after a
	/// quote its text is literal, so a brace, a glob character and a bracket are allowed in the userinfo; where it is a bare word the shell expands them, so
	/// the userinfo excludes them and the whole word is read without a credential position.
	/// </summary>
	private const string _URL_USERINFO_LITERAL =
		@"(?:(?<=[\x22\x27])https?://(?<user>[^\s/@:\x27\x22$`\\]+):(?<credential>[^\s/@\x27\x22$`\\]+)" + _URL_AFTER_USERINFO
		+ @"|(?<![\w\x22\x27-])https?://(?<user>[^\s/@:\x27\x22$`\\{}*?\[\]]+):(?<credential>[^\s/@\x27\x22$`\\{}*?\[\]]+)" + _URL_AFTER_USERINFO + @")";

	/// <summary>An HTTP URL with userinfo of any shape, read without a credential position.</summary>
	private const string _URL_USERINFO_ANY =
		@"(?<![\w-])https?://[^\s/@:\x27\x22]+:[^\s/@\x27\x22]+@";

	/// <summary>A literal Basic Authorization header: the name and the scheme in any case, then a literal token (captured), an expansion or a template.</summary>
	private const string _HEADER_VALUE =
		@"[\x22\x27]?(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+(?:(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22\x27\s;&|)]|$)|(?=[^\x22\x27\s]*[$`\\])|\{\{[^{}\n]{0,200}\}\})";

	private const string _CURL_HEADER_OPTION =
		@"(?:(?<construct>-" + _CURL_FLAG + @"*H)" + _GAP_OPT + @"|(?<construct>--(?:expand-)?header)(?:=|" + _GAP + @"))";

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

	private const string _PORT_TOKEN =
		@"\$\{?(?:[A-Za-z0-9]+_)*(?i:port)(?:_[A-Za-z0-9]+)*(?![A-Za-z0-9])";

	private const string _HOST_EXPANSION =
		@"\$\{?(?:[A-Za-z0-9]+_)*(?i:host|hostname|server|addr|address)(?:_[A-Za-z0-9]+)*(?![A-Za-z0-9])";

	/// <summary>A colon pair: a left half that starts like a name, a colon not followed by two slashes, and a right half that is not a path, not a port and not a host and port.</summary>
	private const string _PAIR =
		@"(?<=[\s\x27\x22])(?!" + _HOST_EXPANSION + @"[^\s:\x27\x22|;&]*:(?!\$\{?" + _CRED_NAME + @"(?![A-Za-z0-9_])))[A-Za-z_$%{][^\s:\x27\x22|;&]*:(?!//)(?!\d{1,5}(?![\w$]))[^\s:\x27\x22|;&/\\]";

	/// <summary>
	/// The characters a literal credential word may hold: none that the shell expands or splits (<c>$</c>, the backtick, the backslash, the quotes, a brace, a
	/// glob character, a bracket), no format character, so a printf format is never read as a literal.
	/// </summary>
	private const string _LITERAL_WORD_CHARACTER = @"[^\s\x27\x22$`\\;&|<>(){}*?\[\]%]";

	/// <summary>
	/// A literal colon pair as a whole shell word, captured as the credential: a name before the colon and a right half that is not a path and not a port,
	/// the word ending at a closing quote, white space or a separator. A word that expands in any part is not captured, and the pair test that follows it
	/// reads it without a position.
	/// </summary>
	private const string _LITERAL_PAIR =
		@"(?<=[\s\x27\x22])(?<credential>[A-Za-z_]" + _LITERAL_WORD_CHARACTER + @"*:(?!//)(?!\d{1,5}(?![\w$]))[^\s\x27\x22$`\\;&|<>(){}*?\[\]%/:]" + _LITERAL_WORD_CHARACTER + @"*)(?=[\x22\x27]?(?:[\s;&|)]|$))";

	/// <summary>A literal pair, captured, or else any pair, read without a credential position.</summary>
	private const string _LITERAL_PAIR_OR_PAIR = @"(?:" + _LITERAL_PAIR + @"|" + _PAIR + @")";

	private const string _NOT_PORT_EXPANSION =
		@"(?!(?:(?:" + _STOP_AT_ECHO_OR_PRINTF + @"[^\n|;&])" + _BACKTRACK_POINT + @"){0,400}?" + _PORT_TOKEN + @")";

	/// <summary>The Base64 encoders the class reads, by name.</summary>
	private const string _ENCODER =
		@"(?:g?base64|basenc" + _GAP + @"--base64|busybox" + _GAP + @"base64|openssl" + _GAP + @"(?:enc" + _GAP + @"-(?:base64|a)|base64)|uuencode" + _GAP + @"-m)(?![\w-])";

	/// <summary>A decode flag refuses the match.</summary>
	private const string _DECODE_REFUSAL =
		@"(?![^\n|;&]*?(?<=\s)(?:-d|-D|--decode)(?![\w-]))";

	private const string _FILTER_STAGE =
		@"(?:tr|cut|head|sed|awk|fold|tee)(?![\w-])(?>(?:" + _UNIT + @"){0,60})" + _GAP_OPT + @"\|(?!\|)" + _GAP_OPT;

	private const string _ENCODE_WORD =
		@"(?:b64encode|standard_b64encode|encodebytes|encode_base64)";

	private const string _PROGRAM_OPENING =
		@"(?:python3?|perl)(?![\w-])[^\n;&|\x27\x22]{0,60}?[\x27\x22]";

	private const string _PIPE_PRODUCER_PIPELINE =
		@"(?<construct>printf|echo)(?![\w-])" + _NOT_PORT_EXPANSION + @"(?=(?:(?:" + _STOP_AT_ECHO_OR_PRINTF + @"[^\n|;&])" + _BACKTRACK_POINT + @"){0,400}?" + _LITERAL_PAIR_OR_PAIR + @")" + _WINDOW_ECHO_OR_PRINTF + _GAP_OPT + @"\|(?!\|)" + _GAP_OPT + @"(?:" + _FILTER_STAGE + @"){0,2}(?:(?<consumer>" + _ENCODER + @")" + _DECODE_REFUSAL + @"|(?<consumer>" + _PROGRAM_OPENING + @"[^\n]{0,600}?" + _ENCODE_WORD + @"))";

	private const string _HERE_STRING_PIPELINE =
		@"(?<construct>" + _ENCODER + @")" + _DECODE_REFUSAL + @"(?>(?:[ \t]+-[\w-]+)*)[ \t]*<<<[ \t]*[\x22\x27]?(?=" + _LITERAL_PAIR_OR_PAIR + @")(?![^\n|;&)]{0,200}?" + _PORT_TOKEN + @")";

	private const string _PROGRAM_WITH_PAIR_PIPELINE =
		@"(?<construct>python3?|perl)(?![\w-])[^\n;&|\x27\x22]{0,60}?[\x27\x22](?=[^\n]{0,600}?" + _ENCODE_WORD + @")(?=[^\n]{0,600}?" + _LITERAL_PAIR_OR_PAIR + @")";

	/// <summary>A producer piped to an encoder, a here-string given to an encoder, or a program one-liner that encodes a pair.</summary>
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

	private const string _HEADER_OPTION_ANY =
		_OPTION_START + @"(?:(?<construct>-" + _CURL_FLAG + @"*H)" + _GAP_OPT + @"|(?<construct>--(?:expand-)?header)(?:=|" + _GAP + @"))";

	private const string _BASIC_HEADER_EVIDENCE_AHEAD =
		@"(?=[ \t\n\\\x22\x27=]{1,16}(?i:authorization)[ \t]*:[ \t]*[\x22\x27]?(?i:basic)(?![\w-]))";

	/// <summary>A credential option as an unquoted array element, or a header option followed by a literal Basic header.</summary>
	private const string _ARRAY_OPTION =
		@"(?<=[ \t\n\x22\x27(=])(?<construct>-" + _CURL_FLAG + @"*(?:u|a|C)|--user|--netrc|--auth|--http-passwd|(?:-" + _CURL_FLAG + @"*H|--header)" + _BASIC_HEADER_EVIDENCE_AHEAD + @")(?![\w-])";

	/// <summary>The name of an authentication helper: it carries `basic` and one of `auth`, `header` or `credential`.</summary>
	private const string _HELPER_NAME =
		@"(?=[A-Za-z0-9_:-]*(?i:basic))(?=[A-Za-z0-9_:-]*(?i:auth|header|credential))[A-Za-z_][A-Za-z0-9_:-]{2,60}";

	private const string _COMMAND_POSITION =
		@"(?<=(?:" + _LINE_START + @"[ \t]*|[;&|({`][ \t]*|\$\([ \t]*|(?<![\w-])(?:then|do|else)[ \t]+))";

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

	private const string _AUTH_TYPE_OTHER_THAN_BASIC =
		@"(?:" + _OPTION_START + @"-A(?:=|" + _GAP_OPT + @")|" + _OPTION_START + @"--auth-type(?:=|" + _GAP + @"))(?!basic(?![\w-]))";

	private const string _NOT_NON_HTTP_OR_OTHER_AUTH_TYPE =
		@"(?<!(?:" + _AUTH_TYPE_OTHER_THAN_BASIC + @"|" + _NON_HTTP_URL + @")" + _CHAR_UNIT + @"{0,600})(?!" + _CHAR_WINDOW_CONT + @"(?:" + _AUTH_TYPE_OTHER_THAN_BASIC + @"|" + _NON_HTTP_URL + @"))";

	private const string _ARIA_CHALLENGE_ON =
		_OPTION_START + @"--http-auth-challenge(?:=true(?![\w-])|(?![\w=-]))";

	private const string _NOT_ARIA_CHALLENGE =
		@"(?<!" + _ARIA_CHALLENGE_ON + _CHAR_UNIT + @"{0,600})(?!" + _CHAR_WINDOW_CONT + _ARIA_CHALLENGE_ON + @")";

	private const string _ARIA_PASSWORD_VALUE =
		@"(?:(?!~)(?<credential>" + _PASS_PART + @")" + _WORD_END + @"|" + _USER_VALUE_EXPANDING + @")";

	private const string _WGET_EXECUTE_PASSWORD_LITERAL =
		_EXECUTE_OPTION + @"[\x22\x27]?(?:http_)?passw(?:or)?d[ \t]*=[ \t]*(?!~)(?<credential>" + _PASS_PART + @")[\x22\x27]?" + _WORD_END;

	private const string _WGET_EXECUTE_CREDENTIAL_ANY =
		_EXECUTE_OPTION + @"[\x22\x27]?(?:http_)?(?:user|passw(?:or)?d)[ \t]*=";

	private const string _WGET_EXECUTE_LOOKAHEADS =
		@"(?:(?=" + _WINDOW_WGET + _WGET_EXECUTE_PASSWORD_LITERAL + @")|(?=" + _WINDOW_WGET + _WGET_EXECUTE_CREDENTIAL_ANY + @"))";

	private const string _REQUEST_LINE =
		@"(?:(?<![\w-])|(?<=\\n))(?:GET|POST|PUT|DELETE|HEAD|PATCH|OPTIONS)[ \t]+\S+[ \t]+HTTP/1\.[01]";

	private const string _REQUEST_HEADER_TEXT =
		@"(?:(?<![\w-])|(?<=\\n))(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+(?:(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22\x27\s\\;&|)]|$)|\$|%s)(?<!(?<![\w.$/-])(?:grep|egrep|fgrep|awk|sed|case)(?![\w-])[^\n]{0,300})";

	private const string _NOT_LISTENING_SIDE =
		@"(?!(?:nc|ncat|netcat|socat)(?![\w.-])[^\n;&|]{0,200}?(?:(?<=[ \t])-[A-Za-z]*l[A-Za-z]*(?![\w-])|--listen(?![\w-])|TCP4?-LISTEN))";

	private const string _HAND_BUILT_REQUEST_SINK =
		@"(?:(?<construct><>|>)(?=[ \t]*[\x22\x27]/dev/tcp/)|(?<!(?:<>|>)[ \t]*[\x22\x27])(?<construct>/dev/tcp)(?=/)|" + _COMMAND_START + _NOT_LISTENING_SIDE + @"(?<construct>nc|ncat|netcat|socat)(?![\w.-])|(?<construct>s_client)(?<=(?<![\w.$/-])openssl[ \t]+s_client)(?![\w-]))";

	private const string _SINK_WITHIN_TWELVE_LINES =
		@"(?:(?<=" + _HAND_BUILT_REQUEST_SINK + @"[^\n]{0,400}(?:\n[^\n]{0,400}){0,12})|(?=(?:(?:[^\n]{0,400}\n)" + _BACKTRACK_POINT + @"){0,12}?[^\n]{0,400}?" + _HAND_BUILT_REQUEST_SINK + @"))";

	private const string _REQUEST_LINE_WITHIN_TWELVE_LINES =
		@"(?:(?<=" + _REQUEST_LINE + @"[^\n]{0,400}(?:\n[^\n]{0,400}){0,12})|(?=(?:(?:[^\n]{0,400}\n)" + _BACKTRACK_POINT + @"){0,12}?[^\n]{0,400}?" + _REQUEST_LINE + @"))";

	private const string _CURL_CONFIGURATION_TEXT =
		@"(?:--?)?(?:user[ \t]*[=:\t ][ \t]*[\x22\x27]?(?:(?<credential>[^\x22\x27\s:$`\\;&|<>(){}*?\[\]%]*:[^\x22\x27\s$`\\;&|<>(){}*?\[\]%]+)(?=[\x22\x27]?(?:\s|\\n|$))|[^\x22\x27\s:]*:[^\x22\x27\s]+)|header[ \t]*[=:\t ][ \t]*[\x22\x27]?(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+(?:(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22\x27]?(?:\s|\\n|$))|\S+))";

	private const string _WGET_CONFIGURATION_TEXT =
		@"(?:http_)?(?:passw(?:or)?d|user)[ \t]*=(?:(?<=passw(?:or)?d[ \t]*=)(?=[ \t]*[\x22\x27]?(?!~)(?<credential>[^\s\x27\x22$`\\;&|<>(){}*?\[\]%]+)(?=[\x22\x27]?(?:\s|\\n|$)))|)";

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
		@"(?<=[\x22\x27]\k<cfg>)(?<=(?<![\w.$/-])(?:printf|echo)(?![\w-])[^\n]{0,300})(?=(?:[^\n]{0,300}?|(?>[^\n]{0,300})\n(?:(?:(?![ \t]*" + _CLOSING_WORD + @")[^\n]{0,400}\n)" + _BACKTRACK_POINT + @"){0,29}?[ \t]*" + _CLOSING_WORD + @"[ \t]*(?:\|[ \t]*)?)" + _WRITE_TO_A_PATH + @")";

	private const string _CURL_CONFIGURATION_OPTION =
		@"(?:(?<construct>-" + _CURL_FLAG + @"*K)" + _GAP_OPT + @"|(?<construct>--config)(?:=|" + _GAP + @"))";

	private const string _CURL_READS_THE_CONFIGURATION_WRITTEN_ABOVE =
		@"(?=" + _DEFINITION_WINDOW + _CURL + _WINDOW_CURL + _OPTION_START + _CURL_CONFIGURATION_OPTION + _CONFIGURATION_PATH_REFERENCE + @")";

	private const string _CURL_READS_THE_CONFIGURATION_FROM_ITS_OWN_INPUT =
		_CURL + _WINDOW_CURL + _OPTION_START + _CURL_CONFIGURATION_OPTION + @"(?:(?<![\w.-])-(?![\w.-])(?=[^\n]{0,400}?" + _HEREDOC_OPENING + @"[^\n]{0,200}\n(?:(?:[^\n]{0,400}\n)" + _BACKTRACK_POINT + @"){0,39}?[ \t]*" + _CURL_CONFIGURATION_TEXT + @")|<\((?=[^)\n]{0,400}?" + _CURL_CONFIGURATION_TEXT + @"))";

	private const string _WGETRC_ANCHOR =
		@"(?:(?<![\w.$-])(?<construct>WGETRC)=" + _CONFIGURATION_PATH_REFERENCE + @"|" + _WGET + _WINDOW_WGET + _OPTION_START + @"(?<construct>--config)(?:=|" + _GAP + @")" + _CONFIGURATION_PATH_REFERENCE + @")";

	private const string _WGETRC_ANCHOR_WITHIN_THE_DEFINITION_WINDOW =
		@"(?:(?<=" + _WGETRC_ANCHOR + @"[^\n]{0,400}(?:\n[^\n]{0,400}){0,29})|(?=" + _DEFINITION_WINDOW + _WGETRC_ANCHOR + @"))";

	private const string _CURL_USE_EVIDENCE =
		@"(?:" + _WINDOW_ANY_UNIT + @"(?:" + _OPTION_START + _CURL_USER_OPTION + @"(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @"|" + _USER_VALUE_NO_COLON + @")|" + _OPTION_START + _CURL_HEADER_OPTION + _HEADER_VALUE + @")|(?=" + _CHAR_WINDOW_CONT + @"(?:" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @"))" + _USE_WORD_AS_CONSTRUCT_BEHIND + @")";

	private const string _WGET_USE_EVIDENCE =
		@"(?:" + _WINDOW_ANY_UNIT + _OPTION_START + @"(?<construct>--(?:http-)?(?:user|passw(?:or)?d))(?:=|" + _GAP + @")|(?=" + _CHAR_WINDOW_CONT + @"(?:" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @"))" + _USE_WORD_AS_CONSTRUCT_BEHIND + @")";

	private const string _ANYAUTH =
		_OPTION_START + @"--anyauth(?![\w-])";

	private const string _ASSIGNED_PAIR_VALUE =
		@"[\x22\x27]?(?![^\s\x27\x22;&|]{0,200}?" + _PORT_TOKEN + @")(?!" + _HOST_EXPANSION + @"[^\s:\x27\x22]*:(?!\$\{?" + _CRED_NAME + @"(?![A-Za-z0-9_])))[A-Za-z_$%{][^\s:\x27\x22|;&]*:(?!//)(?!\d{1,5}(?![\w$]))[^\s:\x27\x22|;&/\\]";

	private const string _ENCODER_STATEMENT_ON_PAIR =
		@"(?:(?<![\w.$/-])(?<construct>printf|echo)(?![\w-])(?:(?:" + _STOP_AT_ECHO_OR_PRINTF + @"[^\n|;&])" + _BACKTRACK_POINT + @"){0,400}?\$\{?\k<pair>(?![A-Za-z0-9_])\}?[\x22\x27]?" + _WINDOW_ECHO_OR_PRINTF + _GAP_OPT + @"\|(?!\|)" + _GAP_OPT + @"(?:" + _FILTER_STAGE + @"){0,2}(?<consumer>" + _ENCODER + @")" + _DECODE_REFUSAL + @"|(?<![\w.$/-])(?<construct>" + _ENCODER + @")" + _DECODE_REFUSAL + @"(?>(?:[ \t]+-[\w-]+)*)[ \t]*<<<[ \t]*[\x22\x27]?\$\{?\k<pair>(?![A-Za-z0-9_]))";

	private const string _STRING_OPTION =
		@"(?:-[A-Za-z]*u[ \t]+(?=\S*[:$`])|--user[ \t]+(?=\S*[:$`])|--netrc(?:-file|-optional)?(?![\w-])|--(?:user|password|http-user|http-password)=|-a[ \t]+\S|--auth[ \t=]+\S|--http-passwd(?![\w-]))";
}
