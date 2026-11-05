using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>The expressions of the first twelve `Shell` pattern classes, SH-001 to SH-012. Each carries the policy match timeout; each is `CultureInvariant | ExplicitCapture` and case-sensitive, the case-insensitive words being written inline.</summary>
internal static partial class ShellPatternExpressions
{
	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private const RegexOptions _LINE_OPTIONS = _OPTIONS | RegexOptions.Multiline;

	/// <summary>SH-001: The user option of `curl` with a pair, a literal one (the password half is captured) or any word that expands, outside every other method flag and every non-HTTP URL.</summary>
	private const string _CURL_USER_PASSWORD =
		_CURL + _WINDOW_CURL + _OPTION_START + _CURL_USER_OPTION + @"(?:" + _USER_VALUE_LITERAL + @"|" + _USER_VALUE_EXPANDING + @")" + _NOT_NON_BASIC_OR_NON_HTTP;

	[GeneratedRegex(_CURL_USER_PASSWORD, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CurlUserPassword();

	/// <summary>SH-002: The user option of `curl` with a literal word that holds no colon: the password is prompted for.</summary>
	private const string _CURL_USER_WITHOUT_PASSWORD =
		_CURL + _WINDOW_CURL + _OPTION_START + _CURL_USER_OPTION + _USER_VALUE_NO_COLON + _NOT_NON_BASIC_OR_NON_HTTP;

	[GeneratedRegex(_CURL_USER_WITHOUT_PASSWORD, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CurlUserWithoutPassword();

	/// <summary>SH-003: A literal `Authorization: Basic` header given to `curl` (`-H`, `--header`, `--expand-header`) or to `wget` (`--header`).</summary>
	private const string _AUTHORIZATION_HEADER_BASIC =
		@"(?:" + _CURL + _WINDOW_CURL + _OPTION_START + _CURL_HEADER_OPTION + @"|" + _WGET + _WINDOW_WGET + _OPTION_START + @"(?<construct>--header)(?:=|" + _GAP + @"))" + _HEADER_VALUE;

	[GeneratedRegex(_AUTHORIZATION_HEADER_BASIC, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderBasic();

	/// <summary>SH-004: `wget` with the preemptive switch and a credential option or a URL with userinfo; the credential options are required by look-ahead.</summary>
	private const string _WGET_PREEMPTIVE_CREDENTIALS =
		@"(?<construct>" + _WGET + @")(?=" + _WINDOW_WGET + _WGET_PREEMPTIVE_SWITCH + @")" + _WGET_CREDENTIAL_LOOKAHEADS;

	[GeneratedRegex(_WGET_PREEMPTIVE_CREDENTIALS, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WgetPreemptiveCredentials();

	/// <summary>SH-005: A URL with userinfo given to `curl`, outside every method flag.</summary>
	private const string _CURL_URL_USERINFO =
		@"(?<construct>" + _CURL + @")(?:(?=" + _CHAR_WINDOW_CURL + _URL_USERINFO_LITERAL + @")|(?=" + _CHAR_WINDOW_CURL + _URL_USERINFO_ANY + @"))(?!" + _CHAR_WINDOW_CURL + _METHOD_FLAG + @")";

	[GeneratedRegex(_CURL_URL_USERINFO, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CurlUrlUserinfo();

	/// <summary>SH-006: The netrc options of `curl`.</summary>
	private const string _CURL_NETRC_OPTION =
		_CURL + _WINDOW_CURL + _OPTION_START + @"(?<construct>-" + _CURL_FLAG + @"*n(?![\w-])|--netrc(?:-file|-optional)?(?![\w-]))";

	[GeneratedRegex(_CURL_NETRC_OPTION, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CurlNetrcOption();

	/// <summary>SH-007: An encoded colon pair whose output reaches a covered client: inside its command as a substitution, or assigned to a variable the client uses within the definition window.</summary>
	private const string _ENCODED_CREDENTIAL_PAIR =
		@"(?:\$\(|`)[ \t]*(?=" + _PIPELINE_START_WORD + @")(?:" + _SUBSTITUTION_OF_A_CLIENT_BEHIND + @"|" + _SUBSTITUTION_OF_AN_ASSIGNMENT_BEHIND + @")(?>" + _ENCODING_PIPELINE + @")(?(assigned)(?=" + _USE_OF_ASSIGNED_BY_A_CLIENT + @"))";

	[GeneratedRegex(_ENCODED_CREDENTIAL_PAIR, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex EncodedCredentialPair();

	/// <summary>SH-008: A variable assigned a Basic token, a whole Basic header or the scheme word, and a header option of a covered client that names it within the definition window.</summary>
	private const string _BASIC_TOKEN_IN_VARIABLE =
		@"^[ \t]*(?:(?:export|readonly|local|declare)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?(?<variable>[A-Za-z_]\w*)=(?:[\x22\x27]?(?i:basic)(?:[ \t]+(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22\x27]?[ \t]*(?:[;&|#]|$)))?(?![\w-])" + _DEFINITION_WINDOW + _HEADER_OPTION_ANY + @"[\x22\x27]?(?i:authorization)[ \t]*:[ \t]*\$\{?\k<variable>(?![A-Za-z0-9_])|[\x22\x27]?(?i:authorization)[ \t]*:[ \t]*(?i:basic)[ \t]+(?:(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22\x27]?[ \t]*(?:[;&|#]|$)))?" + _DEFINITION_WINDOW + _HEADER_OPTION_ANY + @"[\x22\x27]?\$\{?\k<variable>\}?[\x22\x27]?(?![\w$]))";

	[GeneratedRegex(_BASIC_TOKEN_IN_VARIABLE, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex BasicTokenInVariable();

	/// <summary>
	/// The text of an array, from the character after its opening parenthesis to its closing parenthesis, read only where the closing stands within the bound that the
	/// definition window gives every other class: on the opening's own physical line, up to the 10,000-character line ceiling, or across up to thirty physical lines of at most
	/// 400 characters. The two bounds are look-aheads that cannot run past a closing parenthesis or past their line count, so a line of `x=(` that never closes costs its own
	/// bound once and a file of them costs a linear multiple of its size.
	/// </summary>
	private const string _ARRAY_TEXT_WITHIN_ITS_BOUND =
		@"(?:(?=[^)\n]{0,10000}\))|(?=(?:[^)\n]{0,400}\n){1,29}[^)\n]{0,400}\)))";

	/// <summary>SH-009: An array, an append, an element or an associative array that holds a credential option, and a covered client that uses it within the definition window.</summary>
	private const string _ARGUMENT_ARRAY_CREDENTIALS =
		@"^[ \t]*(?:(?:declare|typeset|local)[ \t]+(?:-[A-Za-z]+[ \t]+)*)?(?<array>[A-Za-z_]\w*)(?:\+?=\(" + _ARRAY_TEXT_WITHIN_ITS_BOUND + @"(?>[^)]*?" + _ARRAY_OPTION + @"[^)]*\))|\[[^\]\n]{1,40}\]\+?=[^\n]{0,300}?" + _ARRAY_OPTION + @")" + _DEFINITION_WINDOW + _CLIENT_COMMAND + _WINDOW_CLIENT + @"\x22?\$\{\k<array>\[(?:[@*]|[^\]\n]{1,40})\]\}";

	[GeneratedRegex(_ARGUMENT_ARRAY_CREDENTIALS, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ArgumentArrayCredentials();

	/// <summary>SH-010: An assignment of a URL with userinfo, and a covered client that uses the variable within the definition window.</summary>
	private const string _URL_USERINFO_ASSIGNMENT =
		@"^[ \t]*(?:(?:export|readonly|local)[ \t]+)?(?<construct>[A-Za-z_]\w*)=[\x22\x27]?(?<![\w-])https?://(?:(?<user>[^\s/@:\x27\x22$`\\]+):(?<credential>[^\s/@\x27\x22$`\\]+)|[^\s/@:\x27\x22]+:[^\s/@\x27\x22]+)@[^\s\x27\x22$`\\;&|<>()]*(?=[\x27\x22\s;&|)<>]|$)" + _USE_OF_CONSTRUCT_BY_A_CLIENT;

	[GeneratedRegex(_URL_USERINFO_ASSIGNMENT, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UrlUserinfoAssignment();

	/// <summary>SH-011: `wget` with a credential option or a URL with userinfo and no preemptive switch required.</summary>
	private const string _WGET_CHALLENGE_DEPENDENT_CREDENTIALS =
		@"(?<construct>" + _WGET + @")" + _WGET_CREDENTIAL_LOOKAHEADS;

	[GeneratedRegex(_WGET_CHALLENGE_DEPENDENT_CREDENTIALS, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WgetChallengeDependentCredentials();

	/// <summary>SH-012: The definition of a function whose name marks it as the script's own Basic authentication helper.</summary>
	private const string _AUTHENTICATION_HELPER_DEFINITION =
		@"^[ \t]*(?:function[ \t]+(?<construct>" + _HELPER_NAME + @")(?:[ \t]*\(\))?|(?<construct>" + _HELPER_NAME + @")[ \t]*\(\))";

	[GeneratedRegex(_AUTHENTICATION_HELPER_DEFINITION, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthenticationHelperDefinition();
}
