using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE DRAFT `TypeScript` EXPRESSIONS OF THE COST GATE, as test-local constants. A `TypeScript` file is read by three kinds of expression, and the gate charges all three to the one file:
/// the inherited `JavaScript` expressions that stay as they are, the shared expressions that the two shared amendments change, and the expressions that exist for `TypeScript` alone. Nothing here is read from
/// <c>src/</c>.
///
/// THE TWO SHARED AMENDMENTS ARE DRAFTED HERE BECAUSE THE SHARED WINDOWS CANNOT READ THEM. The window vocabulary treats an angle bracket as a plain character, so a generic argument list between a callee and its
/// opening parenthesis is not read by the callee expressions; and its key carrier reads a keyed literal that stands in type position. (the generic-argument group) The callee expressions of `JS-001`, `JS-003`, `JS-015` and `JS-016` gain an optional,
/// atomic group that balances angle brackets to a depth of three, at most 120 units per level, a unit being one character, the two-character arrow, or one nested group. (the tail refusal) The object-key carrier K6 of `JS-004`, and
/// that carrier only, refuses a `Basic` literal directly followed (blanks between allowed) by a semicolon or a single vertical bar, and the template `Basic ${string}` or `Basic ${number}`.
///
/// THIS FILE IS A SECOND PART OF <see cref="JavaScriptDraftExpressions"/>, composing the `TypeScript` expressions as a part of the shared expression class: it reaches the shared window vocabulary (the windows,
/// the blanks, the placeholder body, the carriers) by name, so a rename in the `JavaScript` draft breaks this file at compile time. The public face is <see cref="TypeScriptDraftExpressions"/>.
///
/// THE MERGED VOCABULARY. The shipped vocabulary (the value terminator, the credential literal under the whole-literal rule, the user-information URL, the Basic value, the
/// environment declaration and the Basic fallback of an environment read) is held in private constants of the shipped class, which no test-local file may reach. This file therefore carries a copy of each such constant,
/// under the name <c>MERGED_*</c>, and the semantic tests pin every copy to the shipped expressions (the shipped pattern text must contain the copy), so a change to the shipped vocabulary breaks the semantic tests and
/// is never silently charged as the old text.
/// </summary>
internal static partial class JavaScriptDraftExpressions
{
	/// <summary>The bound of one level of the generic-argument group, in units.</summary>
	internal const int GENERIC_UNITS_PER_LEVEL = 120;

	/// <summary>The regular-expression quantifier that carries <see cref="GENERIC_UNITS_PER_LEVEL"/>; a constant string cannot be built from the integer, so the two are stated side by side and must agree.</summary>
	private const string _GENERIC_UNITS_QUANTIFIER = "{0,120}";

	#region The merged 0.8 vocabulary (copies of the shipped constants, pinned to the shipped expressions by the semantic tests)

	/// <summary>The value terminator of the whole-literal rule: after the closing delimiter and optional blanks, a comma, a closing parenthesis, brace or bracket, a semicolon, a line end, a comment start or the end of the text.</summary>
	internal const string MERGED_VALUE_TERMINATOR =
		@"(?=[ \t]*(?:[,)}\];\n]|//|/\*|\z))";

	/// <summary>The credential literal under the whole-literal rule: the group `credential` is captured only when the literal is the whole value.</summary>
	internal const string MERGED_CREDENTIAL_LITERAL =
		@"(?:\x27(?!" + _PLACEHOLDER_BODY + @"\x27)(?<credential>[^\x27\\\n]{1,200})\x27" + MERGED_VALUE_TERMINATOR + @"|\x22(?!" + _PLACEHOLDER_BODY + @"\x22)(?<credential>[^\x22\\\n]{1,200})\x22" + MERGED_VALUE_TERMINATOR + @"|`(?!" + _PLACEHOLDER_BODY + @"`)(?<credential>(?>[^`$\\\n]|\$(?!\{)){1,200})`" + MERGED_VALUE_TERMINATOR + @")";

	/// <summary>The user-information credential of a request URL, with the terminator look-ahead.</summary>
	internal const string MERGED_USERINFO_URL_CREDENTIAL =
		@"(?:(?=[\x27\x22`]https?://[^\s:/@\x27\x22`$\\]{1,64}:(?!" + _PLACEHOLDER_BODY + @"@)(?<credential>[^\s/@\x27\x22`$\{\}\\]{1,200})@(?>[^\x27\x22`\n$]|\$(?!\{)){0,300}[\x27\x22`]" + MERGED_VALUE_TERMINATOR + @"))?";

	internal const string MERGED_USERINFO_URL =
		MERGED_USERINFO_URL_CREDENTIAL + @"[\x27\x22`]https?://[^\s:/@\x27\x22`$\\]{1,64}:[^\s/@\x27\x22`$\{\}\\]{1,200}@";

	internal const string MERGED_OBJECT_PASSWORD_MEMBER =
		@"(?=(?:" + _SPACE + @"(?:password|pass)(?![\w$])" + _SPACE + @"[,}]|" + _TS_ARGUMENT_WINDOW + @"(?:(?<![\w$.])[\x27\x22]?(?:password|pass)[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + MERGED_CREDENTIAL_LITERAL + @")?|," + _SPACE + @"(?:password|pass)(?![\w$])" + _SPACE + @"[,}])))";

	internal const string MERGED_OBJECT_PASSWORD_KEY =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"\{" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?password[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + MERGED_CREDENTIAL_LITERAL + @")?)";

	/// <summary>The Basic value of a header: the credential payload is a literal credential only when the literal is whole.</summary>
	internal const string MERGED_BASIC_VALUE =
		@"[\x27\x22`](?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"[\x27\x22`])(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x27\x22`]" + MERGED_VALUE_TERMINATOR + @"))?";

	internal const string MERGED_ENVIRONMENT_NAME =
		@"[A-Za-z_$][\w$]{0,127}(?![\w$])";

	internal const string MERGED_ENVIRONMENT_KEY =
		@"(?:\x27[^\x27\\\n]{1,128}\x27|\x22[^\x22\\\n]{1,128}\x22)";

	/// <summary>The five forms of the environment read: `process.env`, `import.meta.env`, `Bun.env` (dot or bracket) and `Deno.env.get(...)`.</summary>
	internal const string MERGED_ENVIRONMENT_READ =
		@"(?:(?<![\w$.])(?:process\.env|import\.meta\.env|Bun\.env)(?:\." + MERGED_ENVIRONMENT_NAME + @"|\[" + MERGED_ENVIRONMENT_KEY + @"\])|(?<![\w$.])Deno\.env\.get[ \t]{0,8}\([ \t]{0,8}" + MERGED_ENVIRONMENT_KEY + @"[ \t]{0,8}\))";

	internal const string MERGED_FALLBACK_OPERATOR =
		@"[ \t]{0,8}(?:\|\||\?\?)[ \t]{0,8}";

	/// <summary>The right-hand side that assigns a name from an environment value: the empty group `environment` marks it and the group `fallbackWord` holds the leading word of the fallback (validator code 74 refuses one without the other).</summary>
	internal const string MERGED_ENVIRONMENT_DECLARATION =
		@"(?=(?:process|import|Deno|Bun)(?![\w$])|`\$\{)(?<environment>)(?:" + MERGED_ENVIRONMENT_READ + @"(?:(?<fallbackWord>)|" + MERGED_FALLBACK_OPERATOR + @"(?:\x27(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x27\\\n]|\\.){0,200}\x27|\x22(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x22\\\n]|\\.){0,200}\x22|`(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^`$\\\n]|\$(?!\{)|\\.){0,200}`))|`\$\{" + MERGED_ENVIRONMENT_READ + @"\}`(?<fallbackWord>))";

	/// <summary>The Basic fallback of an environment read (the second alternative of the merged `EX-JS-004`), behind a forward guard on the five read roots.</summary>
	internal const string MERGED_ENVIRONMENT_READ_WITH_A_BASIC_FALLBACK =
		@"(?=(?:process|import|Deno|Bun)(?![\w$]))" + MERGED_ENVIRONMENT_READ + MERGED_FALLBACK_OPERATOR + @"(?:\x27(?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"\x27)(?<credential>[A-Za-z0-9+/=_-]{4,})(?=\x27))?(?>[^\x27\\\n]|\\.){0,200}\x27|\x22(?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"\x22)(?<credential>[A-Za-z0-9+/=_-]{4,})(?=\x22))?(?>[^\x22\\\n]|\\.){0,200}\x22|`(?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"`)(?<credential>[A-Za-z0-9+/=_-]{4,})(?=`))?(?>[^`$\\\n]|\$(?!\{)|\\.){0,200}`)" + MERGED_VALUE_TERMINATOR;

	#endregion

	#region The non-lazy windows (no lazy bounded quantifier in any TypeScript draft expression)

	// ------------------------------------------------------------------------------------------
	// The shared windows of the JavaScript draft and of the shipped expressions are lazy bounded loops (`{0,1500}?`, `{0,20}?`), which the .NET 10 source generator backtracks wrongly on . The TypeScript
	// expressions therefore carry their own greedy forms of the same windows, with the same unit and the same bound. A greedy bounded loop followed by its target reaches the same set of matches as the lazy one
	// (it tries the longest window first and gives units back); inside a look-ahead, where only whether a match exists is asked, the answer is identical. Where a window stands in the main match and an object holds
	// two matching keys, the greedy form reports the last of them where the lazy form reported the first (the anchor `auth`, `Authorization` or `headers` of the last qualifying key); every sample reads the same.
	// ------------------------------------------------------------------------------------------

	/// <summary>The greedy argument window of one call: at most 1,500 units, the same unit as the shared window.</summary>
	internal const string ARGUMENT_WINDOW_WITHOUT_LAZINESS =
		@"(?:" + _ARGUMENT_UNIT + @"){0,1500}";

	/// <summary>The lazy window of the shipped text, for the pin test only: no expression of this file uses it.</summary>
	// The copy carries the zero-width point inside the lazy loop body, as the shipped expressions carry it.
	internal const string LAZY_ARGUMENT_WINDOW_OF_THE_SHIPPED_TEXT =
		@"(?:(?:" + _ARGUMENT_UNIT + @")(?:\b|\B)){0,1500}?";

	private const string _TS_ARGUMENT_WINDOW = ARGUMENT_WINDOW_WITHOUT_LAZINESS;

	/// <summary>The greedy chain window: at most twenty links.</summary>
	private const string _TS_CHAIN_WINDOW =
		@"(?:" + _CHAIN_LINK + @"){0,20}";

	private const string _TS_OBJECT_USER_MEMBER =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?(?:username|user)[\x27\x22]?" + _SPACE + @":)";

	private const string _TS_OBJECT_USERNAME_KEY =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"\{" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?username[\x27\x22]?" + _SPACE + @":)";

	private const string _TS_CHAIN_TO_AUTH_METHOD_OF_REQUEST =
		@"(?<![\w$.])request(?![\w$])" + _TS_CHAIN_WINDOW + _SPACE + @"\." + _SPACE + @"(?<construct>auth)(?![\w$])" + _SPACE + @"\(";

	/// <summary>The credential-named identifier: the look-ahead that asks for a credential word within the first 64 characters, greedy.</summary>
	private const string _TS_CREDENTIAL_NAME =
		@"(?:(?=[\w$]{0,64}" + _CREDENTIAL_WORD + @")[\w$]{1,64})";

	private const string _TS_CARRIER_K1_HEADERS_KEY =
		@"(?<![\w$])(?<construct>headers)(?![\w$])" + _BLANKS + @"[:=](?!=)" + _SPACE + @"\{" + _TS_ARGUMENT_WINDOW + _HEADER_NAME_KEY + _SPACE + @":" + _SPACE;

	private const string _TS_CARRIER_K3_NEW_HEADERS =
		@"(?<![\w$.])new[ \t]{1,8}(?<construct>Headers)(?![\w$])" + _SPACE + @"\(" + _SPACE + @"(?:\{|\[" + _SPACE + @"\[)" + _TS_ARGUMENT_WINDOW + _HEADER_NAME_KEY + _SPACE + @"[:,]" + _SPACE;

	private const string _TS_CARRIERS =
		@"(?:" + _TS_CARRIER_K1_HEADERS_KEY + @"|" + _CARRIER_K2_HEADER_SETTING_CALL + @"|" + _TS_CARRIER_K3_NEW_HEADERS + @"|" + _CARRIER_K4_SET_OR_APPEND + @"|" + _CARRIER_K5_PROPERTY_ASSIGNMENT + @")";

	#endregion

	#region The expression text of the `TypeScript` draft

	// ------------------------------------------------------------------------------------------
	// The generic-argument group. Atomic at every level (a failed group costs its own length once), bounded at 120 units per level, balanced to depth three. A unit is the two-character
	// arrow (so that a function type inside the list does not close it), a character that is neither angle bracket nor a line feed, or a group of the level below. A fourth level is not read.
	// ------------------------------------------------------------------------------------------

	private const string _GENERIC_LEVEL_1 =
		$@"<(?>(?:(?>=>|[^<>\n])){_GENERIC_UNITS_QUANTIFIER})>";

	private const string _GENERIC_LEVEL_2 =
		$@"<(?>(?:(?>=>|[^<>\n]|{_GENERIC_LEVEL_1})){_GENERIC_UNITS_QUANTIFIER})>";

	private const string _GENERIC_LEVEL_3 =
		$@"<(?>(?:(?>=>|[^<>\n]|{_GENERIC_LEVEL_2})){_GENERIC_UNITS_QUANTIFIER})>";

	/// <summary>`_GENERIC`: the optional generic-argument group between a callee and its opening parenthesis.</summary>
	private const string _GENERIC_ARGUMENTS =
		@"(?:" + _GENERIC_LEVEL_3 + @")?";

	// ------------------------------------------------------------------------------------------
	// The amended callee prefixes: the callee text of the `JavaScript` draft, with the generic group placed between the name and the blanks before the parenthesis.
	// ------------------------------------------------------------------------------------------

	private const string _AXIOS_OR_REQUEST_CALLEE_WITH_GENERICS =
		@"(?<![\w$.])(?:axios(?:\.(?:get|post|put|patch|delete|head|options|request|create))?|request(?:\.(?:get|post|put|patch|del|delete|head|options))?)(?![\w$])" + _GENERIC_ARGUMENTS + _SPACE + @"\(";

	private const string _REQUEST_CALLEE_WITH_GENERICS =
		@"(?<![\w$.])request(?:\.(?:get|post|put|patch|del|delete|head|options))?(?![\w$])" + _GENERIC_ARGUMENTS + _SPACE + @"\(";

	internal const string EX_TS_G_001 =
		@"(?:" + _AXIOS_OR_REQUEST_CALLEE_WITH_GENERICS + _TS_ARGUMENT_WINDOW + @"\{" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":|(?<![\w$.])axios\.defaults\.(?<construct>auth)(?![\w$])" + _SPACE + @"=(?!=))" + _SPACE + @"\{"
			+ _TS_OBJECT_USER_MEMBER + MERGED_OBJECT_PASSWORD_MEMBER + @"(?!" + _TS_ARGUMENT_WINDOW + _SEND_IMMEDIATELY_FALSE + @")";

	internal const string EX_TS_G_003 =
		@"(?<![\w$.])(?<construct>https?\.(?:get|request)|axios(?:\.(?:get|post|put|patch|delete|head|options|request))?|got(?:\.(?:get|post|put|patch|delete|head|stream))?|superagent\.(?:get|post|put|patch|delete|head)|request(?:\.(?:get|post|put|patch|del|delete|head|options))?|fetch|undici\.(?:fetch|request)|new[ \t]{1,8}Request)(?![\w$])" + _GENERIC_ARGUMENTS + _SPACE + @"\(" + _TS_ARGUMENT_WINDOW + MERGED_USERINFO_URL;

	internal const string EX_TS_G_015 =
		@"(?<![\w$.])(?<construct>got)(?:\.(?:get|post|put|patch|delete|head|extend|stream|paginate))?(?![\w$])" + _GENERIC_ARGUMENTS + _SPACE + @"\(" + _TS_OBJECT_USERNAME_KEY + MERGED_OBJECT_PASSWORD_KEY;

	internal const string EX_TS_G_016 =
		@"(?:" + _REQUEST_CALLEE_WITH_GENERICS + _TS_ARGUMENT_WINDOW + @"\{" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"\{(?=" + _TS_ARGUMENT_WINDOW + _SEND_IMMEDIATELY_FALSE + @")|"
			+ _TS_CHAIN_TO_AUTH_METHOD_OF_REQUEST + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + @"false(?![\w$]))";

	// ------------------------------------------------------------------------------------------
	// The tail refusal of the object-key carrier K6. It stands after the key and before the value, so that the optional credential of the value cannot be given back to defeat it: a `Basic` literal
	// that closes and is directly followed by a semicolon or by a single vertical bar (a doubled bar is a logical or and is not refused), and a template whose hole holds `string` or `number`.
	// ------------------------------------------------------------------------------------------

	private const string _K6_TAIL_REFUSAL =
		@"(?![\x27\x22`](?i:basic)(?!\w)[^\x27\x22`\n]{0,300}[\x27\x22`]" + _BLANKS + @"(?:;|\|(?!\|)))"
			+ @"(?!`(?i:basic)[ \t]{0,8}\$\{[ \t]{0,8}(?:string|number)[ \t]{0,8}\})";

	internal const string EX_TS_T_004 =
		@"(?:" + _TS_CARRIERS + @"|" + _CARRIER_K6_AUTHORIZATION_KEY_OF_ANY_OBJECT + _K6_TAIL_REFUSAL + @")(?:" + MERGED_BASIC_VALUE + @"|" + MERGED_ENVIRONMENT_READ_WITH_A_BASIC_FALLBACK + @")";

	// ------------------------------------------------------------------------------------------
	// The typed constant declaration (the symbol table). The annotation is greedy and cannot cross an equals sign or a line end, so it needs no lazy loop (a lazy bounded loop in an optional group is
	// the defect the exposure scan looks for).
	// ------------------------------------------------------------------------------------------

	internal const string EX_TS_101 =
		@"^[ \t]*(?:(?:export|declare|public|private|protected|readonly|static)[ \t]+){0,6}(?:(?:const|let|var)[ \t]+)?(?<name>[A-Za-z_$][A-Za-z0-9_$]*)[ \t]*(?::[^=\n]{1,200})?=(?!=)[ \t]*(?:(?:\x27(?<value>(?>[^\s\x27\\]*))(?>(?:[^\x27\\\n]|\\.)*)\x27|\x22(?<value>(?>[^\s\x22\\]*))(?>(?:[^\x22\\\n]|\\.)*)\x22|`(?<value>(?>[^\s`$\\]*))(?>(?:[^`$\\\n]|\$(?!\{)|\\.)*)`)(?=[ \t]*(?:[;,+]|$|//|/\*))|" + MERGED_ENVIRONMENT_DECLARATION + @"(?=[ \t]*(?:[;,]|$|//|/\*)))";

	// ------------------------------------------------------------------------------------------
	// TS-001: an object literal whose declared or asserted type names a documented request-configuration or credential type of the client, with a literal username and password. The match opens
	// at the rare literal (the `auth` key), reads its two members by look-ahead, and only then reads the type, by one look-behind (the annotation, the typed parameter default or the angle-bracket
	// assertion stand before the object) or one look-ahead (the `as` and `satisfies` forms stand after it). The four type names are those of the client's published declaration file:
	// AxiosRequestConfig, AxiosBasicCredentials, CreateAxiosDefaults and InternalAxiosRequestConfig. A member that holds a nested object before the `auth` key is not read.
	// ------------------------------------------------------------------------------------------

	private const string _QUALIFIER =
		@"(?:[A-Za-z_$][\w$]{0,40}\.){0,2}";

	private const string _CONFIGURATION_TYPE =
		_QUALIFIER + @"(?:AxiosRequestConfig|InternalAxiosRequestConfig|CreateAxiosDefaults)(?![\w$])";

	private const string _CREDENTIALS_TYPE =
		_QUALIFIER + @"AxiosBasicCredentials(?![\w$])";

	private const string _CONFIGURATION_BEFORE_THE_AUTH_KEY =
		@"(?<=(?::" + _BLANKS + _CONFIGURATION_TYPE + _BLANKS + @"=|<" + _CONFIGURATION_TYPE + @">)" + _BLANKS + @"\{[^{}]{0,300}(?<![\w$.])auth" + _SPACE + @":" + _SPACE + @"\{)";

	private const string _CREDENTIALS_BEFORE_THE_AUTH_OBJECT =
		@"(?<=<" + _CREDENTIALS_TYPE + @">" + _BLANKS + @"\{)";

	private const string _CONFIGURATION_AFTER_THE_OBJECT =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"\}[^{}]{0,300}\}" + _BLANKS + @"(?:as|satisfies)[ \t]{1,8}" + _CONFIGURATION_TYPE + @")";

	private const string _CREDENTIALS_AFTER_THE_AUTH_OBJECT =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"\}" + _BLANKS + @"(?:as|satisfies)[ \t]{1,8}" + _CREDENTIALS_TYPE + @")";

	private const string _LITERAL_USER_MEMBER =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?username[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + _SQ_LITERAL + @"|" + _DQ_LITERAL + @"|" + _TEMPLATE_LITERAL + @"))";

	private const string _LITERAL_PASSWORD_MEMBER =
		@"(?=" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?password[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?=[\x27\x22`])(?:" + MERGED_CREDENTIAL_LITERAL + @")?)";

	internal const string EX_TS_001 =
		@"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"(?:<" + _CREDENTIALS_TYPE + @">" + _BLANKS + @")?\{" + _LITERAL_USER_MEMBER + _LITERAL_PASSWORD_MEMBER
			+ @"(?:" + _CONFIGURATION_BEFORE_THE_AUTH_KEY + @"|" + _CREDENTIALS_BEFORE_THE_AUTH_OBJECT + @"|" + _CONFIGURATION_AFTER_THE_OBJECT + @"|" + _CREDENTIALS_AFTER_THE_AUTH_OBJECT + @")";

	// ------------------------------------------------------------------------------------------
	// TS-003: a receiver whose name contains `http` (case-insensitive), then a documented `HttpService` method, a generic list allowed, then the options object whose `auth` key holds a user member and a
	// password member. The match opens at the literal `http`, so the name that contains it is not read backwards. The eleven methods are those of the class in the wrapped-client package at 12.0.1
	// (there is no `options` method); the receiver rule is a heuristic.
	// ------------------------------------------------------------------------------------------

	internal const string EX_TS_003 =
		@"(?i:http)[\w$]{0,40}\.(?:postForm|putForm|patchForm|post|put|patch|get|delete|head|request|query)(?![\w$])" + _GENERIC_ARGUMENTS + _SPACE + @"\(" + _TS_ARGUMENT_WINDOW + @"\{" + _TS_ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"\{"
			+ _TS_OBJECT_USER_MEMBER + MERGED_OBJECT_PASSWORD_MEMBER;

	// ------------------------------------------------------------------------------------------
	// The four `TypeScript`-only flow signals. Each opens at a literal and tests the receiver, where it reads one, by a look-behind placed after a look-ahead on the rare literal.
	// ------------------------------------------------------------------------------------------

	private const string _FORM_CONTROL_READ_TAIL =
		@"\??!?\.value(?![\w$])";

	internal const string EX_TS_120 =
		@"(?:\.(?<signal>get)(?![\w$])(?=" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]" + _SPACE + @"\)" + _FORM_CONTROL_READ_TAIL + @")(?<=" + _FORM_RECEIVER + @"\.get)"
			+ @"|\.(?<signal>controls)(?![\w$])(?=(?:\[" + _SPACE + @"[\x27\x22]password[\x27\x22]" + _SPACE + @"\]|\.password(?![\w$]))" + _FORM_CONTROL_READ_TAIL + @")(?<=" + _FORM_RECEIVER + @"\.controls)"
			+ @"|\.(?<signal>value)(?=\.password(?![\w$]))(?<=" + _FORM_RECEIVER + @"\.value))";

	internal const string EX_TS_121 =
		@"@(?<signal>Body)(?![\w$])" + _SPACE + @"\(" + _SPACE + @"(?:[\x27\x22](?:" + _TS_CREDENTIAL_NAME + @")[\x27\x22]" + _SPACE + @"\)|\)" + _SPACE + @"\{[^{}\n]{0,200}(?<![\w$])" + _TS_CREDENTIAL_NAME + @"(?![\w$])[^{}\n]{0,200}\})";

	internal const string EX_TS_122 =
		@"(?i:config)[\w$]{0,40}\.(?<signal>getOrThrow|get)(?![\w$])" + _GENERIC_ARGUMENTS + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,100}" + _CREDENTIAL_WORD;

	internal const string EX_TS_123 =
		@"(?<![\w$])(?:(?:export|declare|public|private|protected|readonly|static|const|let|var)[ \t]{1,8}){0,4}(?<signal>" + _TS_CREDENTIAL_NAME + @")(?<!(?:Label|Placeholder|Field|Name|Pattern|Regex|Length|Hint|Message|Error))" + _BLANKS + @":(?>[^=\n,;\x27\x22`]{1,100})=(?![=>])" + _SPACE
			+ @"(?:\x27(?!(?:include|omit|same-origin)\x27)[^\x27\n]{1,400}\x27|\x22(?!(?:include|omit|same-origin)\x22)[^\x22\n]{1,400}\x22|`(?!(?:include|omit|same-origin)`)[^`$\n]{1,400}`)";

	#endregion

	[GeneratedRegex(EX_TS_101, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTs101();

	[GeneratedRegex(EX_TS_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTs001();

	[GeneratedRegex(EX_TS_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTs003();

	[GeneratedRegex(EX_TS_G_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTsG001();

	[GeneratedRegex(EX_TS_G_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTsG003();

	[GeneratedRegex(EX_TS_G_015, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTsG015();

	[GeneratedRegex(EX_TS_G_016, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTsG016();

	[GeneratedRegex(EX_TS_T_004, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTsT004();

	[GeneratedRegex(EX_TS_120, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTs120();

	[GeneratedRegex(EX_TS_121, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTs121();

	[GeneratedRegex(EX_TS_122, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTs122();

	[GeneratedRegex(EX_TS_123, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExTs123();
}

/// <summary>
/// THE PUBLIC FACE OF THE DRAFT `TypeScript` EXPRESSIONS. <see cref="NewAndAmendedExpressions"/> holds the twelve expressions that are new for `TypeScript` or that replace an inherited one: the typed constant declaration
/// (with the environment alternative), `TS-001`, `TS-003`, the four callee expressions as the generic-argument group amends them, the object-key carrier with its tail refusal (over both alternatives of the merged `EX-JS-004`),
/// and the four `TypeScript`-only flow signals, all over the merged vocabulary.
/// <see cref="MatchBoundExpressions"/> is the whole set one `TypeScript` file is charged, by the conservative-charge rule (every inherited expression in full, so a class the allow-list leaves out is still counted):
/// the SHIPPED `JavaScript` expressions (<see cref="InheritedShippedExpressions"/>, `EX-JS-018` included) that stay as they are, and the twelve above, which stand in place of the six they replace.
/// </summary>
internal static class TypeScriptDraftExpressions
{
	private const string _FLOW_SIGNAL_PREFIX = "EX-TS-12";

	/// <summary>The inherited expressions that a `TypeScript` file does not read as they stand: each is replaced by an expression of <see cref="NewAndAmendedExpressions"/>.</summary>
	internal static ImmutableArray<string> ReplacedInheritedIdentifiers { get; } =
	[
		"EX-JS-101",
		"EX-JS-001",
		"EX-JS-003",
		"EX-JS-004",
		"EX-JS-015",
		"EX-JS-016"
	];

	/// <summary>The shipped `JavaScript` flow signals by their declared name, each with the identifier of its expression.</summary>
	private static readonly ImmutableDictionary<string, string> _FLOW_SIGNAL_IDENTIFIERS = ImmutableDictionary<string, string>.Empty
		.Add("DomPasswordFieldRead", "EX-JS-120")
		.Add("FormPasswordPropertyRead", "EX-JS-121")
		.Add("BrowserPromptCall", "EX-JS-122")
		.Add("BrowserStorageRead", "EX-JS-123")
		.Add("XhrFiveArgumentOpen", "EX-JS-124")
		.Add("HtmlPasswordInputMarkup", "EX-JS-125")
		.Add("RequestBodyPasswordForwarded", "EX-JS-126")
		.Add("NodeMutedPasswordPrompt", "EX-JS-127")
		.Add("PromptLibraryPasswordType", "EX-JS-128")
		.Add("PromptWording", "EX-JS-129")
		.Add("ProcessEnvCredentialRead", "EX-JS-130")
		.Add("ConfigLibraryCredentialRead", "EX-JS-131")
		.Add("SecretManagerClientCall", "EX-JS-132")
		.Add("CredentialNamedLiteralAssignment", "EX-JS-133")
		.Add("ReadlineInterfaceQuestion", "EX-JS-134")
		.Add("MutedWritableOutput", "EX-JS-135");

	private const string _CONSTANT_DECLARATION_IDENTIFIER = "EX-JS-101";
	private const string _MEMBER_DECLARATION_IDENTIFIER = "EX-JS-102";
	private const string _TEST_FRAMEWORK_MARKER_IDENTIFIER = "EX-JS-110";
	private const string _GENERATED_BANNER_MARKER_IDENTIFIER = "EX-JS-111";
	private const string _CLASS_IDENTIFIER_FORMAT = "JS-{0:D3}";
	private const string _EXPRESSION_IDENTIFIER_FORMAT = "EX-JS-{0:D3}";
	private const int _FIRST_CLASS_NUMBER = 1;

	/// <summary>The class expressions of the shipped analyzer: `EX-JS-001` to `EX-JS-019`.</summary>
	internal const int SHIPPED_CLASS_COUNT = 19;

	/// <summary>The member-declaration expression of the profile (`EX-JS-102`), the second expression of the symbol-collection phase.</summary>
	internal const int SHIPPED_MEMBER_DECLARATION_COUNT = 1;

	/// <summary>The shipped content markers: the generated-file banner and the test framework.</summary>
	internal const int SHIPPED_CONTENT_MARKER_COUNT = 2;

	/// <summary>The shipped flow signals, `EX-JS-120` to `EX-JS-135`.</summary>
	internal const int SHIPPED_FLOW_SIGNAL_COUNT = 16;

	/// <summary>
	/// The shipped `JavaScript` expressions that the recognition engine charges to the match bound for a file, read from the registered analyzer (no test-local file reaches the shipped expression class): the constant declaration,
	/// the member declaration, the nineteen class expressions `EX-JS-001` to `EX-JS-019`, the two content markers and the sixteen flow signals. The first-line rule is a lexical expression and is not charged here.
	/// </summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> ShippedJavaScriptExpressions { get; } = ReadTheShippedExpressions();

	internal static ImmutableArray<(string Identifier, Regex Expression)> NewAndAmendedExpressions { get; } =
	[
		("EX-TS-101", JavaScriptDraftExpressions.ExTs101()),
		("EX-TS-001", JavaScriptDraftExpressions.ExTs001()),
		("EX-TS-003", JavaScriptDraftExpressions.ExTs003()),
		("EX-TS-G-001", JavaScriptDraftExpressions.ExTsG001()),
		("EX-TS-G-003", JavaScriptDraftExpressions.ExTsG003()),
		("EX-TS-G-015", JavaScriptDraftExpressions.ExTsG015()),
		("EX-TS-G-016", JavaScriptDraftExpressions.ExTsG016()),
		("EX-TS-T-004", JavaScriptDraftExpressions.ExTsT004()),
		("EX-TS-120", JavaScriptDraftExpressions.ExTs120()),
		("EX-TS-121", JavaScriptDraftExpressions.ExTs121()),
		("EX-TS-122", JavaScriptDraftExpressions.ExTs122()),
		("EX-TS-123", JavaScriptDraftExpressions.ExTs123())
	];

	/// <summary>The shipped inherited expressions that a `TypeScript` file reads as they stand: every shipped `JavaScript` expression that no `TypeScript` expression replaces, `EX-JS-018` included.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> InheritedShippedExpressions { get; } =
		[.. ShippedJavaScriptExpressions.Where(entry => !ReplacedInheritedIdentifiers.Contains(entry.Identifier))];

	/// <summary>The expressions charged to the recognition engine's match bound for one `TypeScript` file. A `TypeScript` file has no first-line rule, so nothing is charged to the lexical budget.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> MatchBoundExpressions { get; } =
	[
		.. InheritedShippedExpressions,
		.. NewAndAmendedExpressions
	];

	private static ImmutableArray<(string Identifier, Regex Expression)> ReadTheShippedExpressions()
	{
		ILanguageAnalyzer analyzer = JavaScriptRegisteredExpressions.Analyzer;
		ImmutableArray<(string Identifier, Regex Expression)>.Builder expressions = ImmutableArray.CreateBuilder<(string Identifier, Regex Expression)>();

		expressions.Add((_CONSTANT_DECLARATION_IDENTIFIER, analyzer.LexicalProfile.ConstantDeclarationExpression));
		expressions.Add((_MEMBER_DECLARATION_IDENTIFIER, analyzer.LexicalProfile.MemberDeclarationExpression!));

		for (int classNumber = _FIRST_CLASS_NUMBER; classNumber <= SHIPPED_CLASS_COUNT; classNumber++)
		{
			string classIdentifier = string.Format(CultureInfo.InvariantCulture, _CLASS_IDENTIFIER_FORMAT, classNumber);

			expressions.Add((string.Format(CultureInfo.InvariantCulture, _EXPRESSION_IDENTIFIER_FORMAT, classNumber), analyzer.DetectionRulesByPatternClass[classIdentifier].Expression));
		}

		foreach (ContentMarker marker in analyzer.DemotionSignals.ContentMarkers)
		{
			expressions.Add((marker.Kind == DemotionKind.Test ? _TEST_FRAMEWORK_MARKER_IDENTIFIER : _GENERATED_BANNER_MARKER_IDENTIFIER, marker.Expression));
		}

		foreach (FlowSignal signal in analyzer.CandidateSelectionAndRecommendation.FlowSignals)
		{
			expressions.Add((_FLOW_SIGNAL_IDENTIFIERS[signal.Name], signal.Expression));
		}

		return expressions.ToImmutable();
	}

	/// <summary>The identifiers that a flow signal carries: the group that anchors a signal is <c>signal</c>, a class's is <c>construct</c>.</summary>
	internal static bool IsAFlowSignal(string identifier)
	{
		return identifier.StartsWith(_FLOW_SIGNAL_PREFIX, StringComparison.Ordinal);
	}
}
