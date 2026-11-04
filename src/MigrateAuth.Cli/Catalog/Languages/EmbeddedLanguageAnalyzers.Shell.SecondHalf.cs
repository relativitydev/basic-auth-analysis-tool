using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>The expressions of the last thirteen `Shell` pattern classes, SH-013 to SH-025, on the same terms as the first twelve.</summary>
internal static partial class ShellPatternExpressions
{
	/// <summary>SH-013: `http` or `https` with an authentication option (literal pair, expansion or a prompt word), a literal Basic header item, or a URL with userinfo; another authentication type or a non-HTTP URL refuses it.</summary>
	private const string _HTTPIE_AUTHENTICATION =
		@"(?:" + _HTTPIE_COMMAND + _WINDOW_HTTPIE + _OPTION_START + @"(?:(?<construct>-a)" + _GAP_OPT + @"|(?<construct>--auth)(?:=|" + _GAP + @"))(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @"|" + _USER_VALUE_NO_COLON + @")|(?<construct>" + _HTTPIE_COMMAND + @")(?=" + _CHAR_WINDOW_HTTPIE + @"(?:" + _OPTION_START + _HEADER_VALUE + @"|" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @")))" + _NOT_NON_HTTP_OR_OTHER_AUTH_TYPE;

	[GeneratedRegex(_HTTPIE_AUTHENTICATION, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HttpieAuthentication();

	/// <summary>SH-014: `aria2c` with a password option, a literal Basic header or a URL with userinfo, outside its challenge-only switch.</summary>
	private const string _ARIA2C_CREDENTIALS =
		@"(?:" + _ARIA2C + _WINDOW_ARIA2C + _OPTION_START + @"(?:(?<construct>--http-passwd)(?:=|" + _GAP + @")" + _ARIA_PASSWORD_VALUE + @"|(?<construct>--header)(?:=|" + _GAP + @")" + _HEADER_VALUE + @")|(?<construct>" + _ARIA2C + @")(?=" + _CHAR_WINDOW_ARIA2C + @"(?:" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @")))" + _NOT_ARIA_CHALLENGE;

	[GeneratedRegex(_ARIA2C_CREDENTIALS, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Aria2cCredentials();

	/// <summary>SH-015: `lwp-request` with a user option, a literal Basic header or a URL with userinfo, and `aria2c` with a password option under its challenge switch.</summary>
	private const string _CHALLENGE_DEPENDENT_CLIENT_CREDENTIALS =
		@"(?:" + _LWP + _WINDOW_LWP + _OPTION_START + @"(?:(?<construct>-C)" + _GAP_OPT + @"(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @")|(?<construct>-H)" + _GAP_OPT + _HEADER_VALUE + @")|(?<construct>" + _LWP + @")(?=" + _CHAR_WINDOW_LWP + @"(?:" + _URL_USERINFO_LITERAL + @"|" + _URL_USERINFO_ANY + @"))|" + _ARIA2C + _WINDOW_ARIA2C + _OPTION_START + @"(?<construct>--http-passwd)(?:=|" + _GAP + @")" + _ARIA_PASSWORD_VALUE + @"(?:(?<=" + _ARIA_CHALLENGE_ON + _CHAR_UNIT + @"{0,600})|(?=" + _CHAR_WINDOW_ARIA2C + _ARIA_CHALLENGE_ON + @")))";

	[GeneratedRegex(_CHALLENGE_DEPENDENT_CLIENT_CREDENTIALS, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ChallengeDependentClientCredentials();

	/// <summary>SH-016: `wget` with the preemptive switch and a credential given as a configuration command with `-e` or `--execute`.</summary>
	private const string _WGET_EXECUTE_CREDENTIALS_PREEMPTIVE =
		@"(?<construct>" + _WGET + @")(?=" + _WINDOW_WGET + _WGET_PREEMPTIVE_SWITCH + @")" + _WGET_EXECUTE_LOOKAHEADS;

	[GeneratedRegex(_WGET_EXECUTE_CREDENTIALS_PREEMPTIVE, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WgetExecuteCredentialsPreemptive();

	/// <summary>SH-017: `wget` with a credential given as a configuration command with `-e` or `--execute`.</summary>
	private const string _WGET_EXECUTE_CREDENTIALS_CHALLENGE_DEPENDENT =
		@"(?<construct>" + _WGET + @")" + _WGET_EXECUTE_LOOKAHEADS;

	[GeneratedRegex(_WGET_EXECUTE_CREDENTIALS_CHALLENGE_DEPENDENT, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WgetExecuteCredentialsChallengeDependent();

	/// <summary>SH-018: A literal Basic `Authorization` request header in a request written to a socket: the sink is `/dev/tcp`, the operator that opens a quoted `/dev/tcp` path, `nc`, `ncat`, `netcat`, `socat` or `openssl s_client`.</summary>
	private const string _HAND_BUILT_BASIC_REQUEST =
		_REQUEST_HEADER_TEXT + _SINK_WITHIN_TWELVE_LINES + _REQUEST_LINE_WITHIN_TWELVE_LINES;

	[GeneratedRegex(_HAND_BUILT_BASIC_REQUEST, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HandBuiltBasicRequest();

	/// <summary>SH-019: A `curl` configuration line the script writes (a user or a Basic header line) and the `curl` command that reads it: through standard input, through a path or through a process substitution.</summary>
	private const string _CURL_CONFIGURATION_WRITTEN_BY_SCRIPT =
		@"(?:" + _CURL_READS_THE_CONFIGURATION_FROM_ITS_OWN_INPUT + @"|(?<cfg>" + _CURL_CONFIGURATION_TEXT + @")(?:" + _QUOTED_CONFIGURATION_WRITTEN_BY_PRINTF_OR_ECHO_ON_ITS_LINE_OR_BY_A_GROUP_REDIRECTED_BELOW + @"|" + _HERE_DOCUMENT_BODY_LINE_BEHIND + @")" + _CURL_READS_THE_CONFIGURATION_WRITTEN_ABOVE + @")";

	[GeneratedRegex(_CURL_CONFIGURATION_WRITTEN_BY_SCRIPT, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CurlConfigurationWrittenByScript();

	/// <summary>SH-020: A `wget` configuration line the script writes and the `WGETRC` assignment or `--config` option that names the same path.</summary>
	private const string _WGET_CONFIGURATION_WRITTEN_BY_SCRIPT =
		@"(?<cfg>" + _WGET_CONFIGURATION_TEXT + @")(?:" + _QUOTED_CONFIGURATION_WRITTEN_BY_PRINTF_OR_ECHO_ON_ITS_LINE_OR_BY_A_GROUP_REDIRECTED_BELOW + @"|" + _HERE_DOCUMENT_BODY_LINE_BEHIND + @")" + _WGETRC_ANCHOR_WITHIN_THE_DEFINITION_WINDOW;

	[GeneratedRegex(_WGET_CONFIGURATION_WRITTEN_BY_SCRIPT, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WgetConfigurationWrittenByScript();

	/// <summary>SH-021: A same-file alias or command variable defined to be `curl`, and its use with a user option, a literal Basic header or a URL with userinfo.</summary>
	private const string _CURL_VIA_SAME_FILE_ALIAS_OR_VARIABLE =
		_CURL_ALIAS_OR_VARIABLE_TO_USE + _CURL_USE_EVIDENCE + _NOT_NON_BASIC_OR_NON_HTTP;

	[GeneratedRegex(_CURL_VIA_SAME_FILE_ALIAS_OR_VARIABLE, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CurlViaSameFileAliasOrVariable();

	/// <summary>SH-022: A same-file alias or command variable defined to be `wget`, and its use with a credential option (`--user` among them) or a URL with userinfo.</summary>
	private const string _WGET_VIA_SAME_FILE_ALIAS_OR_VARIABLE =
		_WGET_ALIAS_OR_VARIABLE_TO_USE + _WGET_USE_EVIDENCE;

	[GeneratedRegex(_WGET_VIA_SAME_FILE_ALIAS_OR_VARIABLE, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WgetViaSameFileAliasOrVariable();

	/// <summary>SH-023: `curl` with `--anyauth` and a user option.</summary>
	private const string _CURL_ANYAUTH_WITH_USER =
		_CURL + @"(?=" + _CHAR_WINDOW_CLIENT + _ANYAUTH + @")" + _WINDOW_CURL + _OPTION_START + _CURL_USER_OPTION + @"(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @")" + _NOT_NON_BASIC_OR_NON_HTTP_EXCEPT_ANYAUTH;

	[GeneratedRegex(_CURL_ANYAUTH_WITH_USER, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CurlAnyauthWithUser();

	/// <summary>SH-024: A colon pair assigned to a variable, an encoder that consumes the variable, and a covered client that uses the encoded output.</summary>
	private const string _ENCODED_PAIR_HELD_IN_VARIABLE =
		@"=(?=[\x22\x27]?[A-Za-z_$%{][^\s:\x27\x22|;&]*:)(?<=^[ \t]*" + _DECLARATION_OPTIONAL + @"(?<pair>[A-Za-z_]\w*)=)" + _ASSIGNED_PAIR_VALUE + _DEFINITION_LINES + @"(?:[^\n]{0,400}?" + _CLIENT_COMMAND + @"(?:" + _CHAR_UNIT + _BACKTRACK_POINT + @"){0,600}?(?:\$\(|`)[ \t]*" + _ENCODER_STATEMENT_ON_PAIR + @"|[ \t]*" + _DECLARATION_OPTIONAL + @"(?<encoded>[A-Za-z_]\w*)=(?:\$\(|`)[ \t]*" + _ENCODER_STATEMENT_ON_PAIR + _USE_OF_ENCODED_BY_A_CLIENT + @")";

	[GeneratedRegex(_ENCODED_PAIR_HELD_IN_VARIABLE, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex EncodedPairHeldInVariable();

	/// <summary>SH-025: A string variable that holds a credential option, expanded unquoted by a covered client within the definition window.</summary>
	private const string _OPTIONS_HELD_IN_STRING_VARIABLE =
		@"^[ \t]*" + _DECLARATION_OPTIONAL + @"(?<construct>[A-Za-z_]\w*)=[\x22\x27]?(?=[^\n\x27\x22]{0,300}?(?<=[ \t\x22\x27=])" + _STRING_OPTION + @")" + _DEFINITION_WINDOW + _CLIENT_COMMAND + _WINDOW_CLIENT + @"\$\{?\k<construct>(?![A-Za-z0-9_])";

	[GeneratedRegex(_OPTIONS_HELD_IN_STRING_VARIABLE, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex OptionsHeldInStringVariable();
}
