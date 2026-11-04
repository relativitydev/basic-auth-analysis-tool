using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The expressions of the `JavaScript` analyzer: the first-line rule, the constant declaration, the seventeen pattern classes JS-001 to JS-017 and the two
/// demotion content markers. Each carries the policy match timeout (the lexical budget for the first-line rule), is `CultureInvariant | ExplicitCapture` and
/// case-sensitive, the case-insensitive words being written inline. Every class anchors on a code token (the group <c>construct</c>) and never on a quoted
/// header name or a quoted scheme word, because an anchor in a string body composes to Low.
/// </summary>
internal static partial class JavaScriptPatternExpressions
{
	/// <summary>
	/// The first-line rule of an extensionless file: a shebang that names <c>node</c> or <c>nodejs</c>, directly or through <c>env</c> with its flags. It reads what the base
	/// shebang table reads, as the `Shell` rule does: the interpreter is the last path segment of the first word, or, where that word is <c>env</c>, the first following word that
	/// is not a flag, taken as written; white space is every character the base table splits on. Case-sensitive, and it carries the lexical budget as its timeout.
	/// </summary>
	private const string _EX_JS_100 =
		@"^#!\s*(?:[^\s/]*/)*(?:env\s+(?:-\S*\s+)*)?(?:nodejs|node)(?:\s|\z)";

	/// <summary>
	/// The constant declaration of the file symbol table: <c>const</c>, <c>let</c>, <c>var</c> and a bare <c>NAME = literal</c>, anchored at the start of the line and read on one line; the value is the leading word
	/// of the first literal. It also reads, at the same anchor, a name assigned from an environment value (<see cref="_ENVIRONMENT_DECLARATION"/>), which it marks with the empty group
	/// <c>environment</c> and gives the word of its fallback in the group <c>fallbackWord</c>.
	/// </summary>
	private const string _EX_JS_101 =
		@"^[ \t]*(?:export[ \t]+)?(?:(?:const|let|var)[ \t]+)?(?<name>[A-Za-z_$][A-Za-z0-9_$]*)[ \t]*=(?!=)[ \t]*(?:(?:\x27(?<value>(?>[^\s\x27\\]*))(?>(?:[^\x27\\\n]|\\.)*)\x27|\x22(?<value>(?>[^\s\x22\\]*))(?>(?:[^\x22\\\n]|\\.)*)\x22|`(?<value>(?>[^\s`$\\]*))(?>(?:[^`$\\\n]|\$(?!\{)|\\.)*)`)(?=[ \t]*(?:[;,+]|$|//|/\*))|" + _ENVIRONMENT_DECLARATION + @"(?=[ \t]*(?:[;,]|$|//|/\*)))";

	[GeneratedRegex(_EX_JS_100, _OPTIONS, matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex ShebangFirstLine();

	[GeneratedRegex(_EX_JS_101, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	/// <summary>EX-JS-001: the expression of JS-001, ClientAuthCredentialObject.</summary>
	[GeneratedRegex(_EX_JS_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ClientAuthCredentialObject();

	/// <summary>EX-JS-002: the expression of JS-002, ClientAuthMethodCall.</summary>
	[GeneratedRegex(_EX_JS_002, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ClientAuthMethodCall();

	/// <summary>EX-JS-003: the expression of JS-003, CredentialsInRequestUrl.</summary>
	[GeneratedRegex(_EX_JS_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialsInRequestUrl();

	/// <summary>EX-JS-004: the expression of JS-004, AuthorizationHeaderScheme.</summary>
	[GeneratedRegex(_EX_JS_004, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderScheme();

	/// <summary>EX-JS-005: the expression of JS-005, Base64UserPasswordPair.</summary>
	[GeneratedRegex(_EX_JS_005, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Base64UserPasswordPair();

	/// <summary>EX-JS-006: the expression of JS-006, NodeBufferBase64Pair.</summary>
	[GeneratedRegex(_EX_JS_006, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NodeBufferBase64Pair();

	/// <summary>EX-JS-007: the expression of JS-007, AuthenticationHelperDefinition.</summary>
	[GeneratedRegex(_EX_JS_007, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthenticationHelperDefinition();

	/// <summary>EX-JS-008: the expression of JS-008, NodeCoreAuthString.</summary>
	[GeneratedRegex(_EX_JS_008, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NodeCoreAuthString();

	/// <summary>EX-JS-009: the expression of JS-009, XhrOpenCredentials.</summary>
	[GeneratedRegex(_EX_JS_009, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex XhrOpenCredentials();

	/// <summary>EX-JS-010: the expression of JS-010, UrlCredentialAssignment.</summary>
	[GeneratedRegex(_EX_JS_010, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UrlCredentialAssignment();

	/// <summary>EX-JS-011: the expression of JS-011, NonHttpAuthObject.</summary>
	[GeneratedRegex(_EX_JS_011, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NonHttpAuthObject();

	/// <summary>EX-JS-012: the expression of JS-012, ClientAuthMethodSingleArgument.</summary>
	[GeneratedRegex(_EX_JS_012, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ClientAuthMethodSingleArgument();

	/// <summary>EX-JS-013: the expression of JS-013, UrlUserinfoHeldLiteral.</summary>
	[GeneratedRegex(_EX_JS_013, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UrlUserinfoHeldLiteral();

	/// <summary>EX-JS-014: the expression of JS-014, JqueryAjaxCredentials.</summary>
	[GeneratedRegex(_EX_JS_014, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex JqueryAjaxCredentials();

	/// <summary>EX-JS-015: the expression of JS-015, GotCredentialOptions.</summary>
	[GeneratedRegex(_EX_JS_015, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GotCredentialOptions();

	/// <summary>EX-JS-016: the expression of JS-016, RequestChallengeDependentAuth.</summary>
	[GeneratedRegex(_EX_JS_016, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex RequestChallengeDependentAuth();

	/// <summary>EX-JS-017: the expression of JS-017, AuthorizationHeaderValueHeldInVariable.</summary>
	[GeneratedRegex(_EX_JS_017, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueHeldInVariable();

	/// <summary>EX-JS-018: the expression of JS-018, AuthorizationHeaderValueFromEnvironment.</summary>
	[GeneratedRegex(_EX_JS_018, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueFromEnvironment();

	/// <summary>EX-JS-110: the testframework demotion marker.</summary>
	[GeneratedRegex(_EX_JS_110, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TestFrameworkContentMarker();

	/// <summary>EX-JS-111: the generatedfilebanner demotion marker.</summary>
	[GeneratedRegex(_EX_JS_111, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedFileBannerContentMarker();

	/// <summary>
	/// The tail of a bare member name (a shorthand property): the name ends there, and blanks and line ends only lie before a comma or a closing brace. The credential group is never part of it.
	/// </summary>
	private const string _SHORTHAND_MEMBER_TAIL =
		@"(?![\w$])" + _SPACE + @"[,}]";

	/// <summary>
	/// The user member: a key, optional quotes and a colon, or a bare name directly after the opening brace (already consumed where this look-ahead runs) or after a comma at depth one reached through the
	/// argument window. A name that is the value of another key (<c>label: username</c>) is preceded by a colon, and a name in a nested group is consumed whole by the window, so neither is a member.
	/// Each candidate costs one forward step.
	/// </summary>
	private const string _OBJECT_USER_MEMBER =
		@"(?=(?:" + _SPACE + @"(?:username|user)" + _SHORTHAND_MEMBER_TAIL + @"|" + _ARGUMENT_WINDOW + @"(?:(?<![\w$.])[\x27\x22]?(?:username|user)[\x27\x22]?" + _SPACE + @":|," + _SPACE + @"(?:username|user)" + _SHORTHAND_MEMBER_TAIL + @")))";

	private const string _OBJECT_PASSWORD_MEMBER =
		@"(?=(?:" + _SPACE + @"(?:password|pass)" + _SHORTHAND_MEMBER_TAIL + @"|" + _ARGUMENT_WINDOW + @"(?:(?<![\w$.])[\x27\x22]?(?:password|pass)[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @")?|," + _SPACE + @"(?:password|pass)" + _SHORTHAND_MEMBER_TAIL + @")))";

	private const string _SEND_IMMEDIATELY_FALSE =
		@"(?<![\w$.])sendImmediately(?![\w$])" + _SPACE + @":" + _SPACE + @"false(?![\w$])";

	/// <summary>The bound of one level of the generic-argument group, in units: the two-character arrow, a character other than an angle bracket or a line feed, or a nested group. A string cannot be built from the integer, so the two sit side by side and must agree.</summary>
	private const string _GENERIC_UNITS_QUANTIFIER = "{0,120}";

	private const string _GENERIC_LEVEL_1 =
		$@"<(?>(?:(?>=>|[^<>\n])){_GENERIC_UNITS_QUANTIFIER})>";

	private const string _GENERIC_LEVEL_2 =
		$@"<(?>(?:(?>=>|[^<>\n]|{_GENERIC_LEVEL_1})){_GENERIC_UNITS_QUANTIFIER})>";

	private const string _GENERIC_LEVEL_3 =
		$@"<(?>(?:(?>=>|[^<>\n]|{_GENERIC_LEVEL_2})){_GENERIC_UNITS_QUANTIFIER})>";

	/// <summary>
	/// The optional generic-argument group between a callee and its opening parenthesis (<c>axios.create&lt;Config&gt;(</c>). It balances angle brackets to depth three at no more than 120 units per level, is written with
	/// possessive bodies and no lazy loop, and is atomic as a whole: a list that does not close within the bound, nests deeper than three or spans a line is not a group, and the callee is then not read.
	/// It follows the callee name directly, with no blank before the opening angle bracket.
	/// </summary>
	private const string _GENERIC_ARGUMENTS =
		$@"(?>(?:{_GENERIC_LEVEL_3})?)";

	private const string _AXIOS_OR_REQUEST_CALLEE =
		$@"(?<![\w$.])(?:axios(?:\.(?:get|post|put|patch|delete|head|options|request|create))?|request(?:\.(?:get|post|put|patch|del|delete|head|options))?)(?![\w$]){_GENERIC_ARGUMENTS}{_SPACE}\(";

	private const string _REQUEST_CALLEE =
		$@"(?<![\w$.])request(?:\.(?:get|post|put|patch|del|delete|head|options))?(?![\w$]){_GENERIC_ARGUMENTS}{_SPACE}\(";

	private const string _EX_JS_001 =
		@"(?:" + _AXIOS_OR_REQUEST_CALLEE + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":|(?<![\w$.])axios\.defaults\.(?<construct>auth)(?![\w$])" + _SPACE + @"=(?!=))" + _SPACE + @"\{"
			+ _OBJECT_USER_MEMBER + _OBJECT_PASSWORD_MEMBER + @"(?!" + _ARGUMENT_WINDOW + _SEND_IMMEDIATELY_FALSE + @")";

	private const string _CHAIN_TO_AUTH_METHOD =
		@"(?<![\w$.])(?:superagent|request)(?![\w$])" + _CHAIN_WINDOW + _SPACE + @"\." + _SPACE + @"(?<construct>auth)(?![\w$])" + _SPACE + @"\(";

	private const string _CHAIN_TO_AUTH_METHOD_OF_REQUEST =
		@"(?<![\w$.])request(?![\w$])" + _CHAIN_WINDOW + _SPACE + @"\." + _SPACE + @"(?<construct>auth)(?![\w$])" + _SPACE + @"\(";

	private const string _TYPE_OPTION_BEARER =
		@"\{[^}\n]{0,200}type\s{0,10}:\s{0,10}[\x27\x22]bearer";

	private const string _TYPE_OPTION_AUTO =
		@"\{[^}\n]{0,200}type\s{0,10}:\s{0,10}[\x27\x22]auto[\x27\x22]";

	private const string _EX_JS_002 =
		_CHAIN_TO_AUTH_METHOD + _SPACE + @"(?!null(?![\w$]))" + _ONE_ARGUMENT + @"," + _SPACE + @"(?!" + _TYPE_OPTION_BEARER + @")(?!" + _ONE_ARGUMENT + @"," + _SPACE + @"(?:false(?![\w$])|" + _TYPE_OPTION_AUTO + @"))(?:" + _CREDENTIAL_LITERAL + @")?";

	private const string _EX_JS_003 =
		$@"(?<![\w$.])(?<construct>https?\.(?:get|request)|axios(?:\.(?:get|post|put|patch|delete|head|options|request))?|got(?:\.(?:get|post|put|patch|delete|head|stream))?|superagent\.(?:get|post|put|patch|delete|head)|request(?:\.(?:get|post|put|patch|del|delete|head|options))?|fetch|undici\.(?:fetch|request)|new[ \t]{{1,8}}Request)(?![\w$]){_GENERIC_ARGUMENTS}{_SPACE}\({_ARGUMENT_WINDOW}{_USERINFO_URL}";

	private const string _BASIC_VALUE =
		@"[\x27\x22`](?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"[\x27\x22`])(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x27\x22`]" + _VALUE_TERMINATOR + @"))?";

	private const string _IDENTIFIER_VALUE =
		@"(?![\x27\x22`])(?<scheme>[A-Za-z_$][\w$]{0,63})(?![\w$.(\[])(?=" + _SPACE + @"(?:[,})\];]|\z))";

	private const string _HEADER_NAME_KEY =
		@"(?<![\w$-])[\x27\x22`]?(?i:authorization)[\x27\x22`]?";

	private const string _HEADER_NAME_QUOTED =
		@"[\x27\x22`](?i:authorization)[\x27\x22`]";

	private const string _CARRIER_K1_HEADERS_KEY =
		@"(?<![\w$])(?<construct>headers)(?![\w$])" + _BLANKS + @"[:=](?!=)" + _SPACE + @"\{" + _ARGUMENT_WINDOW + _HEADER_NAME_KEY + _SPACE + @":" + _SPACE;

	private const string _CARRIER_K2_HEADER_SETTING_CALL =
		@"(?<![\w$])(?<construct>setRequestHeader|setHeader)(?![\w$])" + _SPACE + @"\(" + _SPACE + _HEADER_NAME_QUOTED + _SPACE + @"," + _SPACE;

	private const string _CARRIER_K3_NEW_HEADERS =
		@"(?<![\w$.])new[ \t]{1,8}(?<construct>Headers)(?![\w$])" + _SPACE + @"\(" + _SPACE + @"(?:\{|\[" + _SPACE + @"\[)" + _ARGUMENT_WINDOW + _HEADER_NAME_KEY + _SPACE + @"[:,]" + _SPACE;

	private const string _CARRIER_K4_SET_OR_APPEND =
		@"(?<=\.)(?<construct>set|append)(?![\w$])" + _SPACE + @"\(" + _SPACE + _HEADER_NAME_QUOTED + _SPACE + @"," + _SPACE;

	private const string _CARRIER_K5_PROPERTY_ASSIGNMENT =
		@"(?<![\w$])(?<construct>headers)(?:\.common)?(?:" + _BLANKS + @"\[\s{0,10}" + _HEADER_NAME_QUOTED + @"\s{0,10}\]|\.(?i:authorization)(?![\w$]))" + _BLANKS + @"=(?!=)\s{0,10}";

	private const string _CARRIERS =
		@"(?:" + _CARRIER_K1_HEADERS_KEY + @"|" + _CARRIER_K2_HEADER_SETTING_CALL + @"|" + _CARRIER_K3_NEW_HEADERS + @"|" + _CARRIER_K4_SET_OR_APPEND + @"|" + _CARRIER_K5_PROPERTY_ASSIGNMENT + @")";

	/// <summary>
	/// K6, read by `JS-004` only: the whole word `Authorization`, as the key of any object literal, whose value starts with the literal `Basic`. The key is the code token, a quoted key is
	/// read as the same word, and the value may be the template of a tag (an identifier directly before the backtick). A dot or a hyphen before the word refuses `Proxy-Authorization` and a member read.
	/// </summary>
	private const string _CARRIER_K6_AUTHORIZATION_KEY_OF_ANY_OBJECT =
		@"(?<![\w$.-])(?<construct>(?i:authorization))(?![\w$-])[\x27\x22`]?" + _SPACE + @":" + _SPACE + @"(?:[A-Za-z_$][\w$]{0,63}(?=`))?";

	/// <summary>The bound of the literal that the K6 tail refusal reads, in characters. A string cannot be built from the integer, so the two sit side by side and must agree.</summary>
	private const string _K6_REFUSED_LITERAL_QUANTIFIER = "{0,300}";

	/// <summary>
	/// The tail refusal of the K6 carrier alone, a look-ahead placed after the key and before the value. A `Basic` literal that closes within 300 characters and is directly followed, blanks allowed, by a semicolon or by
	/// a single vertical bar (a doubled one is a value and is not refused) is a type member, and so is a template whose only hole has the body `string` or `number`. It is a negative look-ahead over a greedy bounded class, with no lazy loop.
	/// </summary>
	private const string _K6_TAIL_REFUSAL =
		$@"(?![\x27\x22`](?i:basic)(?!\w)[^\x27\x22`\n]{_K6_REFUSED_LITERAL_QUANTIFIER}[\x27\x22`]{_BLANKS}(?:;|\|(?!\|)))(?!`(?i:basic){_BLANKS}\$\{{{_BLANKS}(?:string|number){_BLANKS}\}})";

	private const string _EX_JS_004 =
		$@"(?:{_CARRIERS}|{_CARRIER_K6_AUTHORIZATION_KEY_OF_ANY_OBJECT}{_K6_TAIL_REFUSAL})(?:{_BASIC_VALUE}|{_ENVIRONMENT_READ_WITH_A_BASIC_FALLBACK})";

	private const string _EX_JS_017 =
		_CARRIERS + _IDENTIFIER_VALUE;

	/// <summary>
	/// EX-JS-018: a carrier, then the value of the header, which is either a whole environment value or an identifier that the symbol table resolves to an environment declaration (the
	/// cell of the rule decides which identifiers), and then the guard. The identifier alternative is the one of EX-JS-017.
	/// </summary>
	private const string _EX_JS_018 =
		_CARRIERS + @"(?:" + _ENVIRONMENT_VALUE + @"|" + _IDENTIFIER_VALUE + @")" + _BEARER_GUARD;

	private const string _PAIR_SHAPES =
		@"(?:[\x27\x22]:[\x27\x22]|`(?:[^`]{0,200}?\}:|[^`$:]{1,100}:)[^`]{0,200}`|\.join\s{0,10}\(\s{0,10}[\x27\x22]:[\x27\x22])";

	/// <summary>
	/// The split pair: a quoted literal whose last character is the colon, then a plus and a second quoted literal that is not empty. What follows the second literal does not matter, and no credential
	/// is captured (the pair is not one literal). A forward look-ahead of one literal, a plus and one quote, read only directly after the opening parenthesis of the encoder.
	/// </summary>
	private const string _SPLIT_LITERAL_PAIR =
		@"(?:\x27[^\x27\x22`:\s]{1,100}:\x27|\x22[^\x27\x22`:\s]{1,100}:\x22)" + _SPACE + @"\+" + _SPACE + @"[\x27\x22][^\x27\x22\n]";

	private const string _EX_JS_005 =
		@"(?<![\w$])(?<construct>btoa)(?![\w$])" + _SPACE + @"\((?:(?=" + _SPACE + _WHOLE_LITERAL_PAIR + _SPACE + @"[)+])|(?=" + _SPACE + _SPLIT_LITERAL_PAIR + @")|(?=" + _ARGUMENT_WINDOW + _PAIR_SHAPES + @"))(?:(?=" + _SPACE + _PAIR_CREDENTIAL + @"))?" + _USE_REQUIREMENT;

	/// <summary>
	/// The rule-level merge of the subsumption triple of the header class over this one. An encoder is refused here, and so read once, at the header, only where a header write that `EX-JS-004` reads leads
	/// straight into it: the very carrier forms of that expression, reused as constants, whose tail is the point just before the value (K2, the header-setting call; K4, `.set` or `.append`; K5, the assignment on
	/// `headers` or `headers.common`; K6, the key `Authorization` of an object literal, which also covers K1 and the object form of K3), then the opener `Basic` and a hole or a concatenation, then the encoder.
	/// The carriers' own heads are not repeated, and K6 has no positional anchor, so a key that is not the first entry of its object is subsumed too. Only two forms are not mirrored, and the encoder there is
	/// still emitted beside the header finding: the array form of K3 (`new Headers([['Authorization', ...]])`), and a name that a dot precedes inside the head of K1, which that carrier reads and K6 refuses (a
	/// hyphen before the name is refused by both). Every run is bounded and the look-behind opens after the literal `Buffer`, so it runs only at an encoder.
	/// </summary>
	private const string _BASIC_VALUE_THEN_ENCODER_BEHIND =
		@"[\x27\x22`](?i:basic)[ \t]{0,8}(?:\$\{|[\x27\x22`][ \t]{0,40}\+)[ \t]{0,40}(?:new[ \t]{1,8}Buffer|Buffer\.from)[ \t]{0,40}\(";

	private const string _NOT_IN_A_BASIC_HEADER_VALUE =
		@"(?<!(?:" + _CARRIER_K2_HEADER_SETTING_CALL + @"|" + _CARRIER_K4_SET_OR_APPEND + @"|" + _CARRIER_K5_PROPERTY_ASSIGNMENT + @"|" + _CARRIER_K6_AUTHORIZATION_KEY_OF_ANY_OBJECT + @")" + _BASIC_VALUE_THEN_ENCODER_BEHIND + @")";

	private const string _EX_JS_006 =
		@"(?:(?<![\w$.])new[ \t]{1,8}(?<construct>Buffer)|(?<![\w$.])(?<construct>Buffer)\.from)(?![\w$])" + _SPACE + @"\(" + _NOT_IN_A_BASIC_HEADER_VALUE + @"(?=(?:" + _SPACE + _WHOLE_LITERAL_PAIR + _SPACE + @"|" + _ARGUMENT_WINDOW + _PAIR_SHAPES + _ARGUMENT_WINDOW + @")\)\s{0,10}\.toString\s{0,10}\(\s{0,10}[\x27\x22]base64[\x27\x22])(?:(?=" + _SPACE + _PAIR_CREDENTIAL + @"))?" + _USE_REQUIREMENT;

	private const string _FUNCTION_HEAD =
		@"(?:(?<![\w$.])function[ \t]{0,8}\*?[ \t]{0,8}(?<construct>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"\(" + _SPACE + @"(?<param>[A-Za-z_$][\w$]{0,63})(?:" + _SPACE + @"," + _SPACE + @"(?<second>[A-Za-z_$][\w$]{0,63}))?"
			+ @"|(?<![\w$.])(?:const|let|var)[ \t]{1,8}(?<construct>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"=" + _BLANKS + @"(?:async[ \t]{1,8})?(?:function[ \t]{0,8}\*?[ \t]{0,8})?\(" + _SPACE + @"(?<param>[A-Za-z_$][\w$]{0,63})(?:" + _SPACE + @"," + _SPACE + @"(?<second>[A-Za-z_$][\w$]{0,63}))?"
			+ @"|(?<![\w$.])(?:const|let|var)[ \t]{1,8}(?<construct>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"=" + _BLANKS + @"(?:async[ \t]{1,8})?(?<param>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"=>)";

	private const string _EX_JS_007 =
		_FUNCTION_HEAD + _DEFINITION_WINDOW + @"(?:btoa\s{0,10}\(|(?<![\w$.])Buffer(?:\.from)?\s{0,10}\(|[\x27\x22`](?i:basic)[ \t])[^\n]{0,300}?(?<![\w$.])(?:\k<param>|\k<second>)(?![\w$])";

	private const string _EX_JS_008 =
		@"(?<![\w$.])https?\.(?:get|request)(?![\w$])" + _SPACE + @"\(" + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"(?:" + _PAIR_CREDENTIAL + @")?";

	/// <summary>The method name after a dot, with the receiver refused once the name has matched (`window.open` and `document.open` are not an XHR open).</summary>
	private const string _OPEN_AFTER_A_DOT =
		@"\.";

	private const string _NOT_ON_A_WINDOW_OR_DOCUMENT =
		@"(?<!(?<![\w$.])(?:window|document|self)\.open)";

	private const string _FIVE_ARGUMENTS =
		_SPACE + _ONE_ARGUMENT + @"(?:" + _SPACE + @"," + _SPACE + _ONE_ARGUMENT + @"){3}" + _SPACE + @"," + _SPACE;

	private const string _EX_JS_009 =
		_OPEN_AFTER_A_DOT + @"(?<construct>open)(?![\w$])" + _NOT_ON_A_WINDOW_OR_DOCUMENT + _SPACE + @"\((?:" + _FIVE_ARGUMENTS + @"(?=(?:" + _CREDENTIAL_LITERAL + @")?(?>(?:" + _ARGUMENT_UNIT_WITHOUT_A_COMMA + @"){0,300})" + _SPACE + @"\))|" + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + _USERINFO_URL + @")";

	private const string _URL_BINDING_BEHIND =
		@"(?<=(?<![\w$.])(?:(?:const|let|var)[ \t]{1,8})?\k<receiver>" + _BLANKS + @"=" + _BLANKS + @"new[ \t]{1,8}URL" + _SPACE + @"\((?!" + _SPACE + @"[\x27\x22`](?!https?://)[A-Za-z][A-Za-z0-9+.-]{0,20}://)[^\n]{0,400}(?:\n[^\n]{0,400}){0,30})";

	/// <summary>The match opens at the property token, so the receiver, which precedes it, is read by a look-behind and is captured there for the binding's back-reference.</summary>
	private const string _EX_JS_010 =
		@"\.(?<construct>username|password)(?![\w$])" + _BLANKS + @"=(?!=)" + _BLANKS + @"(?<=(?<![\w$.])(?<receiver>[A-Za-z_$][\w$]{0,63})\.(?:username|password)(?![\w$])" + _BLANKS + @"=" + _BLANKS + @")" + _URL_BINDING_BEHIND + @"(?:" + _CREDENTIAL_LITERAL + @")?";

	private const string _EX_JS_011 =
		@"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"\{(?=" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?(?:username|user|login)[\x27\x22]?" + _SPACE + @":)(?=" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?(?:password|pass|pwd)[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @")?)";

	private const string _EX_JS_012 =
		_CHAIN_TO_AUTH_METHOD + _SPACE + @"(?:" + _ONE_ARGUMENT + @"\)|" + _ONE_ARGUMENT + @"," + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + _TYPE_OPTION_AUTO + @")";

	/// <summary>The match opens at the colon or the equals sign and looks ahead for the URL first, so the name that precedes it is read by a look-behind only where a userinfo URL follows.</summary>
	private const string _EX_JS_013 =
		@"[:=](?![=>])(?=" + _SPACE + @"[\x27\x22`]https?://)(?<=(?<![\w$])" + _NOT_A_NAME_THAT_DENOTES_A_PROXY + @"(?<construct>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"[:=])" + _SPACE + _USERINFO_URL;

	/// <summary>
	/// A name or key that contains the letters <c>proxy</c> or <c>proxies</c>, in any letter case, anywhere in it, denotes a proxy: the user information it holds is a credential for the proxy and not
	/// for the service, and the match is refused. The refusal is read from the start of the name and runs only behind the forward guard on the quote and the scheme.
	/// </summary>
	private const string _NOT_A_NAME_THAT_DENOTES_A_PROXY =
		@"(?![\w$]*(?i:proxy|proxies))";

	private const string _OBJECT_USERNAME_KEY =
		@"(?=" + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?username[\x27\x22]?" + _SPACE + @":)";

	private const string _OBJECT_PASSWORD_KEY =
		@"(?=" + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?password[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @")?)";

	private const string _EX_JS_014 =
		@"(?<![\w$.])(?:\$|jQuery)\.(?<construct>ajaxSetup|ajax)(?![\w$])" + _SPACE + @"\(" + _OBJECT_USERNAME_KEY + _OBJECT_PASSWORD_KEY;

	private const string _EX_JS_015 =
		$@"(?<![\w$.])(?<construct>got)(?:\.(?:get|post|put|patch|delete|head|extend|stream|paginate))?(?![\w$]){_GENERIC_ARGUMENTS}{_SPACE}\({_OBJECT_USERNAME_KEY}{_OBJECT_PASSWORD_KEY}";

	private const string _EX_JS_016 =
		@"(?:" + _REQUEST_CALLEE + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"\{(?=" + _ARGUMENT_WINDOW + _SEND_IMMEDIATELY_FALSE + @")|"
			+ _CHAIN_TO_AUTH_METHOD_OF_REQUEST + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + @"false(?![\w$]))";

	private const string _EX_JS_110 =
		@"(?<![\w$.])describe(?:\.(?:only|skip))?" + _SPACE + @"\((?:(?:[^\n]{0,400}\n)" + _BACKTRACK_POINT + @"){0,100}?[^\n]{0,400}?(?<![\w$.])(?:it|test)(?:\.(?:only|skip))?" + _SPACE + @"\(";

	private const string _EX_JS_111 =
		@"\A(?:#![^\n]{0,400}\n)?[ \t]{0,8}(?://|/\*{1,8}|\*)[^\n]{0,300}?(?i:auto-generated|autogenerated|auto generated|@generated|generated by)|\A(?:(?:[^\n]{0,400}\n)" + _BACKTRACK_POINT + @"){0,9}?[ \t]{0,8}(?://|/\*{1,8}|\*)[^\n]{0,300}?(?i:do not edit|do not modify|do not hand-edit|changes will be lost|changes will be overwritten)";
}
