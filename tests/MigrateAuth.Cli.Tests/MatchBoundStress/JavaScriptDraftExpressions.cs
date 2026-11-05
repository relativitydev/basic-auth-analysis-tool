using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE DRAFT `JavaScript` EXPRESSIONS OF THE COST GATE: the expressions of the detection catalog, as test-local constants, each compiled by <c>[GeneratedRegex]</c> the way the
/// analyzer compiles it, with the policy match timeout (the lexical budget for the first-line rule, the match bound for every other expression). Nothing here is read from <c>src/</c>.
///
/// THE CATALOG STATES THESE EXPRESSIONS AS PROSE (the class entries, the content markers and the flow signals) AND BY EXPRESSION IDENTIFIER. This file is the TIGHTEST FAITHFUL REGULAR-EXPRESSION
/// FORM OF EACH, built from the shapes the catalog fixes (the argument window balanced to a depth of four by unrolling, bounded at 1,500 units, lazy and always required; the definition window of thirty
/// lines of at most 400 characters; the statement window of 300 characters; the whole-word header name; the five carriers; refusals placed after the positive match), and never goes
/// beyond what the catalog says. The semantic checks of
/// <see cref="JavaScriptDraftExpressionSemanticTests"/> are what show that each expression reads its positive forms and refuses its near-misses; the exposure scan shows that no lazy bounded loop sits in an optional group.
///
/// A flow signal's anchor is the capture group <c>signal</c>, a class's the group <c>construct</c>, its literal credential the group <c>credential</c> (the catalog's placeholder vocabulary refused).
/// </summary>
internal static partial class JavaScriptDraftExpressions
{
	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private const RegexOptions _LINE_OPTIONS = _OPTIONS | RegexOptions.Multiline;

	/// <summary>The expression charged to the lexical budget: the first-line detection.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> LexicalExpressions { get; } =
	[
		("EX-JS-100", Ex100())
	];

	/// <summary>The expressions charged to the recognition engine's match bound: the constant declaration (the symbol table), the class rules, the two content markers and the flow signals.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> MatchBoundExpressions { get; } =
	[
		("EX-JS-101", Ex101()),
		("EX-JS-001", Ex001()),
		("EX-JS-002", Ex002()),
		("EX-JS-003", Ex003()),
		("EX-JS-004", Ex004()),
		("EX-JS-005", Ex005()),
		("EX-JS-006", Ex006()),
		("EX-JS-007", Ex007()),
		("EX-JS-008", Ex008()),
		("EX-JS-009", Ex009()),
		("EX-JS-010", Ex010()),
		("EX-JS-011", Ex011()),
		("EX-JS-012", Ex012()),
		("EX-JS-013", Ex013()),
		("EX-JS-014", Ex014()),
		("EX-JS-015", Ex015()),
		("EX-JS-016", Ex016()),
		("EX-JS-017", Ex017()),
		("EX-JS-110", Ex110()),
		("EX-JS-111", Ex111()),
		("EX-JS-120", Ex120()),
		("EX-JS-121", Ex121()),
		("EX-JS-122", Ex122()),
		("EX-JS-123", Ex123()),
		("EX-JS-124", Ex124()),
		("EX-JS-125", Ex125()),
		("EX-JS-126", Ex126()),
		("EX-JS-127", Ex127()),
		("EX-JS-128", Ex128()),
		("EX-JS-129", Ex129()),
		("EX-JS-130", Ex130()),
		("EX-JS-131", Ex131()),
		("EX-JS-132", Ex132()),
		("EX-JS-133", Ex133()),
		("EX-JS-134", Ex134()),
		("EX-JS-135", Ex135())
	];

	#region The expression text, as built from the shapes the catalog fixes

	// ------------------------------------------------------------------------------------------
	// Literals read as one atomic, bounded unit (a string, a template, a regular-expression literal)
	// ------------------------------------------------------------------------------------------

	private const string _SQ_LITERAL =
		@"\x27(?>[^\x27\\\n]|\\[\s\S]){0,1000}\x27";

	private const string _DQ_LITERAL =
		@"\x22(?>[^\x22\\\n]|\\[\s\S]){0,1000}\x22";

	private const string _TEMPLATE_LITERAL =
		@"`(?>[^`\\]|\\[\s\S]){0,1000}`";

	/// <summary>The slash comes first and the look-behind that reads what precedes it comes after it, so the look-behind is evaluated only at a slash and never at every character of a window.</summary>
	private const string _REGEX_LITERAL =
		@"/(?<=[(,=:\[!&|?{;][ \t]{0,8}/)(?![/*])(?>[^/\\\n\[]|\\.|\[(?>[^\]\\\n]|\\.){0,100}\]){1,200}/[a-z]{0,8}";

	/// <summary>The ordinary character is the first alternative, because it is by far the commonest unit; the quote or slash that opens no literal is the last.</summary>
	private const string _LEAF_ALTERNATIVES =
		@"[^\x27\x22`()\[\]{}/]|" + _SQ_LITERAL + @"|" + _DQ_LITERAL + @"|" + _TEMPLATE_LITERAL + @"|" + _REGEX_LITERAL + @"|[\x27\x22`/]";

	private const string _LEAF_ALTERNATIVES_WITHOUT_A_COMMA =
		@"[^\x27\x22`()\[\]{},/]|" + _SQ_LITERAL + @"|" + _DQ_LITERAL + @"|" + _TEMPLATE_LITERAL + @"|" + _REGEX_LITERAL + @"|[\x27\x22`/]";

	// ------------------------------------------------------------------------------------------
	// The window unit: balanced to a depth of four by unrolling. The three kinds of bracket are balanced loosely (any closing bracket closes any opening one), every group is
	// atomic with a possessive body, and a bracket that does not close is not consumed, so a failed group costs its own length once.
	// ------------------------------------------------------------------------------------------

	private const string _GROUP_1 =
		@"(?>[(\[{](?>(?:(?>" + _LEAF_ALTERNATIVES + @")){0,400})[)\]}])";

	private const string _UNIT_1 =
		@"(?>" + _LEAF_ALTERNATIVES + @"|" + _GROUP_1 + @")";

	private const string _GROUP_2 =
		@"(?>[(\[{](?>(?:" + _UNIT_1 + @"){0,400})[)\]}])";

	private const string _UNIT_2 =
		@"(?>" + _LEAF_ALTERNATIVES + @"|" + _GROUP_2 + @")";

	private const string _GROUP_3 =
		@"(?>[(\[{](?>(?:" + _UNIT_2 + @"){0,400})[)\]}])";

	private const string _UNIT_3 =
		@"(?>" + _LEAF_ALTERNATIVES + @"|" + _GROUP_3 + @")";

	private const string _GROUP_4 =
		@"(?>[(\[{](?>(?:" + _UNIT_3 + @"){0,400})[)\]}])";

	/// <summary>One unit of an argument window: a literal, a character, or a balanced group to depth four.</summary>
	private const string _ARGUMENT_UNIT =
		@"(?>" + _LEAF_ALTERNATIVES + @"|" + _GROUP_4 + @")";

	/// <summary>One unit of an argument, which a top-level comma ends.</summary>
	private const string _ARGUMENT_UNIT_WITHOUT_A_COMMA =
		@"(?>" + _LEAF_ALTERNATIVES_WITHOUT_A_COMMA + @"|" + _GROUP_4 + @")";

	/// <summary>`_ARGS`: the argument window of one call, bounded at 1,500 units, lazy, always required and never inside an optional group.</summary>
	private const string _ARGUMENT_WINDOW =
		@"(?:" + _ARGUMENT_UNIT + @"){0,1500}?";

	/// <summary>One argument: up to 300 units, possessive (a top-level comma or the closing parenthesis ends it, so giving a unit back can never help the match).</summary>
	private const string _ONE_ARGUMENT =
		@"(?>(?:" + _ARGUMENT_UNIT_WITHOUT_A_COMMA + @"){1,300})";

	/// <summary>
	/// One link of a method chain: a member name with its optional balanced argument list, or an argument list on its own (the receiver called directly). A statement end breaks a chain by
	/// construction, because it is neither of the two.
	/// </summary>
	private const string _CHAIN_LINK =
		@"(?>" + _SPACE + @"(?:\.\s{0,40}[A-Za-z_$][\w$]{0,40}(?:" + _SPACE + _GROUP_4 + @")?|" + _GROUP_4 + @"))";

	/// <summary>The chain window: at most twenty links, lazy, always required and never inside an optional group.</summary>
	private const string _CHAIN_WINDOW =
		@"(?:" + _CHAIN_LINK + @"){0,20}?";

	// ------------------------------------------------------------------------------------------
	// Shared small shapes. The blanks before a refusal are atomic, so that a negative look-ahead cannot be defeated by giving a blank back.
	// ------------------------------------------------------------------------------------------

	private const string _SPACE =
		@"(?>\s{0,40})";

	private const string _BLANKS =
		@"[ \t]{0,8}";

	private const string _PLACEHOLDER_BODY =
		@"(?i:[*x.\-_#]{3,}|<[^>\n]{0,100}>|\[[^\]\n]{0,100}\]|\{\{[^}\n]{0,100}\}\}|%[^%\n]{0,100}%|placeholder|changeme|change-me|change_me|replace-me|replaceme|your-password|your_password|redacted|todo|fixme|dummy|not-a-real-secret)";

	private const string _CREDENTIAL_LITERAL =
		@"(?:\x27(?!" + _PLACEHOLDER_BODY + @"\x27)(?<credential>[^\x27\\\n]{1,200})\x27|\x22(?!" + _PLACEHOLDER_BODY + @"\x22)(?<credential>[^\x22\\\n]{1,200})\x22|`(?!" + _PLACEHOLDER_BODY + @"`)(?<credential>(?>[^`$\\\n]|\$(?!\{)){1,200})`)";

	private const string _PAIR_CREDENTIAL =
		@"(?:\x27[^\x27\\\n:]{0,100}:(?!" + _PLACEHOLDER_BODY + @"\x27)(?<credential>[^\x27\\\n]{1,200})\x27|\x22[^\x22\\\n:]{0,100}:(?!" + _PLACEHOLDER_BODY + @"\x22)(?<credential>[^\x22\\\n]{1,200})\x22|`[^`\\\n:]{0,100}:(?!" + _PLACEHOLDER_BODY + @"`)(?<credential>(?>[^`$\\\n]|\$(?!\{)){1,200})`)";

	private const string _WHOLE_LITERAL_PAIR =
		@"[\x27\x22`][^\x27\x22`:\s]{1,100}:[^\x27\x22`\s]{1,200}[\x27\x22`]";

	private const string _USERINFO_URL_CREDENTIAL =
		@"(?:(?=[\x27\x22`]https?://[^\s:/@\x27\x22`$\\]{1,64}:(?!" + _PLACEHOLDER_BODY + @"@)(?<credential>[^\s/@\x27\x22`\\]{1,200})@))?";

	private const string _USERINFO_URL =
		_USERINFO_URL_CREDENTIAL + @"[\x27\x22`]https?://[^\s:/@\x27\x22`$\\]{1,64}:[^\s/@\x27\x22`\\]{1,200}@";

	private const string _CREDENTIAL_WORD =
		@"(?:(?i:passw(?:or)?d|secret|credential|creds?|token)|(?<![A-Za-z0-9])(?i:pass|pw|pwd|apikey)(?![A-Za-z0-9])|(?<=[a-z])Pass(?![a-z0-9]))";

	private const string _CREDENTIAL_NAME =
		@"(?:(?=[\w$]{0,64}?" + _CREDENTIAL_WORD + @")[\w$]{1,64})";

	private const string _USE_WORDS =
		@"(?:(?i:basic)(?<![A-Za-z0-9_$](?i:basic))(?![A-Za-z0-9_$])|(?i:authorization))";

	private const string _USE_REQUIREMENT =
		@"(?:(?<=" + _USE_WORDS + @"[\s\S]{0,300})|(?=[\s\S]{0,300}?" + _USE_WORDS + @"))";

	private const string _DEFINITION_WINDOW =
		@"(?:[^\n]{0,400}\n){0,30}?[^\n]{0,400}?";

	// ------------------------------------------------------------------------------------------
	// The first-line rule and the constant declaration
	// ------------------------------------------------------------------------------------------

	internal const string EX_JS_100 =
		@"^#![ \t]*(?:[^\s/]*/)*(?:env[ \t]+(?:-\S*[ \t]+)*)?(?:nodejs|node)(?:[ \t]|$)";

	internal const string EX_JS_101 =
		@"^[ \t]*(?:export[ \t]+)?(?:(?:const|let|var)[ \t]+)?(?<name>[A-Za-z_$][A-Za-z0-9_$]*)[ \t]*=(?!=)[ \t]*(?:\x27(?<value>(?>[^\s\x27\\]*))(?>(?:[^\x27\\\n]|\\.)*)\x27|\x22(?<value>(?>[^\s\x22\\]*))(?>(?:[^\x22\\\n]|\\.)*)\x22|`(?<value>(?>[^\s`$\\]*))(?>(?:[^`$\\\n]|\$(?!\{)|\\.)*)`)(?=[ \t]*(?:[;,+]|$|//|/\*))";

	// ------------------------------------------------------------------------------------------
	// The classes
	// ------------------------------------------------------------------------------------------

	private const string _OBJECT_USER_MEMBER =
		@"(?=" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?(?:username|user)[\x27\x22]?" + _SPACE + @":)";

	private const string _OBJECT_PASSWORD_MEMBER =
		@"(?=" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?(?:password|pass)[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @")?)";

	private const string _SEND_IMMEDIATELY_FALSE =
		@"(?<![\w$.])sendImmediately(?![\w$])" + _SPACE + @":" + _SPACE + @"false(?![\w$])";

	private const string _AXIOS_OR_REQUEST_CALLEE =
		@"(?<![\w$.])(?:axios(?:\.(?:get|post|put|patch|delete|head|options|request|create))?|request(?:\.(?:get|post|put|patch|del|delete|head|options))?)(?![\w$])" + _SPACE + @"\(";

	private const string _REQUEST_CALLEE =
		@"(?<![\w$.])request(?:\.(?:get|post|put|patch|del|delete|head|options))?(?![\w$])" + _SPACE + @"\(";

	internal const string EX_JS_001 =
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

	internal const string EX_JS_002 =
		_CHAIN_TO_AUTH_METHOD + _SPACE + @"(?!null(?![\w$]))" + _ONE_ARGUMENT + @"," + _SPACE + @"(?!" + _TYPE_OPTION_BEARER + @")(?!" + _ONE_ARGUMENT + @"," + _SPACE + @"(?:false(?![\w$])|" + _TYPE_OPTION_AUTO + @"))(?:" + _CREDENTIAL_LITERAL + @")?";

	internal const string EX_JS_003 =
		@"(?<![\w$.])(?<construct>https?\.(?:get|request)|axios(?:\.(?:get|post|put|patch|delete|head|options|request))?|got(?:\.(?:get|post|put|patch|delete|head|stream))?|superagent\.(?:get|post|put|patch|delete|head)|request(?:\.(?:get|post|put|patch|del|delete|head|options))?|fetch|undici\.(?:fetch|request)|new[ \t]{1,8}Request)(?![\w$])" + _SPACE + @"\(" + _ARGUMENT_WINDOW + _USERINFO_URL;

	private const string _BASIC_VALUE =
		@"[\x27\x22`](?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"[\x27\x22`])(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x27\x22`]))?";

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

	internal const string EX_JS_004 =
		@"(?:" + _CARRIERS + @"|" + _CARRIER_K6_AUTHORIZATION_KEY_OF_ANY_OBJECT + @")" + _BASIC_VALUE;

	internal const string EX_JS_017 =
		_CARRIERS + _IDENTIFIER_VALUE;

	private const string _PAIR_SHAPES =
		@"(?:[\x27\x22]:[\x27\x22]|`(?:[^`]{0,200}?\}:|[^`$:]{1,100}:)[^`]{0,200}`|\.join\s{0,10}\(\s{0,10}[\x27\x22]:[\x27\x22])";

	internal const string EX_JS_005 =
		@"(?<![\w$])(?<construct>btoa)(?![\w$])" + _SPACE + @"\((?:(?=" + _SPACE + _WHOLE_LITERAL_PAIR + _SPACE + @"\))|(?=" + _ARGUMENT_WINDOW + _PAIR_SHAPES + @"))(?:(?=" + _SPACE + _PAIR_CREDENTIAL + @"))?" + _USE_REQUIREMENT;

	internal const string EX_JS_006 =
		@"(?:(?<![\w$.])new[ \t]{1,8}(?<construct>Buffer)|(?<![\w$.])(?<construct>Buffer)\.from)(?![\w$])" + _SPACE + @"\((?=(?:" + _SPACE + _WHOLE_LITERAL_PAIR + _SPACE + @"|" + _ARGUMENT_WINDOW + _PAIR_SHAPES + _ARGUMENT_WINDOW + @")\)\s{0,10}\.toString\s{0,10}\(\s{0,10}[\x27\x22]base64[\x27\x22])(?:(?=" + _SPACE + _PAIR_CREDENTIAL + @"))?" + _USE_REQUIREMENT;

	private const string _FUNCTION_HEAD =
		@"(?:(?<![\w$.])function[ \t]{0,8}\*?[ \t]{0,8}(?<construct>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"\(" + _SPACE + @"(?<param>[A-Za-z_$][\w$]{0,63})(?:" + _SPACE + @"," + _SPACE + @"(?<second>[A-Za-z_$][\w$]{0,63}))?"
			+ @"|(?<![\w$.])(?:const|let|var)[ \t]{1,8}(?<construct>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"=" + _BLANKS + @"(?:async[ \t]{1,8})?(?:function[ \t]{0,8}\*?[ \t]{0,8})?\(" + _SPACE + @"(?<param>[A-Za-z_$][\w$]{0,63})(?:" + _SPACE + @"," + _SPACE + @"(?<second>[A-Za-z_$][\w$]{0,63}))?"
			+ @"|(?<![\w$.])(?:const|let|var)[ \t]{1,8}(?<construct>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"=" + _BLANKS + @"(?:async[ \t]{1,8})?(?<param>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"=>)";

	internal const string EX_JS_007 =
		_FUNCTION_HEAD + _DEFINITION_WINDOW + @"(?:btoa\s{0,10}\(|(?<![\w$.])Buffer(?:\.from)?\s{0,10}\(|[\x27\x22`](?i:basic)[ \t])[^\n]{0,300}?(?<![\w$.])(?:\k<param>|\k<second>)(?![\w$])";

	internal const string EX_JS_008 =
		@"(?<![\w$.])https?\.(?:get|request)(?![\w$])" + _SPACE + @"\(" + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"(?:" + _PAIR_CREDENTIAL + @")?";

	/// <summary>The method name after a dot, with the receiver refused once the name has matched (`window.open` and `document.open` are not an XHR open).</summary>
	private const string _OPEN_AFTER_A_DOT =
		@"\.";

	private const string _NOT_ON_A_WINDOW_OR_DOCUMENT =
		@"(?<!(?<![\w$.])(?:window|document|self)\.open)";

	private const string _FIVE_ARGUMENTS =
		_SPACE + _ONE_ARGUMENT + @"(?:" + _SPACE + @"," + _SPACE + _ONE_ARGUMENT + @"){3}" + _SPACE + @"," + _SPACE;

	internal const string EX_JS_009 =
		_OPEN_AFTER_A_DOT + @"(?<construct>open)(?![\w$])" + _NOT_ON_A_WINDOW_OR_DOCUMENT + _SPACE + @"\((?:" + _FIVE_ARGUMENTS + @"(?=(?:" + _CREDENTIAL_LITERAL + @")?(?>(?:" + _ARGUMENT_UNIT_WITHOUT_A_COMMA + @"){0,300})" + _SPACE + @"\))|" + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + _USERINFO_URL + @")";

	private const string _URL_BINDING_BEHIND =
		@"(?<=(?<![\w$.])(?:(?:const|let|var)[ \t]{1,8})?\k<receiver>" + _BLANKS + @"=" + _BLANKS + @"new[ \t]{1,8}URL" + _SPACE + @"\((?!" + _SPACE + @"[\x27\x22`](?!https?://)[A-Za-z][A-Za-z0-9+.-]{0,20}://)[^\n]{0,400}(?:\n[^\n]{0,400}){0,30})";

	/// <summary>The match opens at the property token, so the receiver, which precedes it, is read by a look-behind and is captured there for the binding's back-reference.</summary>
	internal const string EX_JS_010 =
		@"\.(?<construct>username|password)(?![\w$])" + _BLANKS + @"=(?!=)" + _BLANKS + @"(?<=(?<![\w$.])(?<receiver>[A-Za-z_$][\w$]{0,63})\.(?:username|password)(?![\w$])" + _BLANKS + @"=" + _BLANKS + @")" + _URL_BINDING_BEHIND + @"(?:" + _CREDENTIAL_LITERAL + @")?";

	internal const string EX_JS_011 =
		@"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"\{(?=" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?(?:username|user|login)[\x27\x22]?" + _SPACE + @":)(?=" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?(?:password|pass|pwd)[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @")?)";

	internal const string EX_JS_012 =
		_CHAIN_TO_AUTH_METHOD + _SPACE + @"(?:" + _ONE_ARGUMENT + @"\)|" + _ONE_ARGUMENT + @"," + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + _TYPE_OPTION_AUTO + @")";

	/// <summary>The match opens at the colon or the equals sign and looks ahead for the URL first, so the name that precedes it is read by a look-behind only where a userinfo URL follows.</summary>
	internal const string EX_JS_013 =
		@"[:=](?![=>])(?=" + _SPACE + @"[\x27\x22`]https?://)(?<=(?<![\w$])(?<construct>[A-Za-z_$][\w$]{0,63})" + _BLANKS + @"[:=])" + _SPACE + _USERINFO_URL;

	private const string _OBJECT_USERNAME_KEY =
		@"(?=" + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?username[\x27\x22]?" + _SPACE + @":)";

	private const string _OBJECT_PASSWORD_KEY =
		@"(?=" + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])[\x27\x22]?password[\x27\x22]?" + _SPACE + @":" + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @")?)";

	internal const string EX_JS_014 =
		@"(?<![\w$.])(?:\$|jQuery)\.(?<construct>ajaxSetup|ajax)(?![\w$])" + _SPACE + @"\(" + _OBJECT_USERNAME_KEY + _OBJECT_PASSWORD_KEY;

	internal const string EX_JS_015 =
		@"(?<![\w$.])(?<construct>got)(?:\.(?:get|post|put|patch|delete|head|extend|stream|paginate))?(?![\w$])" + _SPACE + @"\(" + _OBJECT_USERNAME_KEY + _OBJECT_PASSWORD_KEY;

	internal const string EX_JS_016 =
		@"(?:" + _REQUEST_CALLEE + _ARGUMENT_WINDOW + @"\{" + _ARGUMENT_WINDOW + @"(?<![\w$.])(?<construct>auth)(?![\w$])" + _SPACE + @":" + _SPACE + @"\{(?=" + _ARGUMENT_WINDOW + _SEND_IMMEDIATELY_FALSE + @")|"
			+ _CHAIN_TO_AUTH_METHOD_OF_REQUEST + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + @"false(?![\w$]))";

	// ------------------------------------------------------------------------------------------
	// The two content markers (demotion)
	// ------------------------------------------------------------------------------------------

	internal const string EX_JS_110 =
		@"(?<![\w$.])describe(?:\.(?:only|skip))?" + _SPACE + @"\((?:[^\n]{0,400}\n){0,100}?[^\n]{0,400}?(?<![\w$.])(?:it|test)(?:\.(?:only|skip))?" + _SPACE + @"\(";

	internal const string EX_JS_111 =
		@"\A(?:#![^\n]{0,400}\n)?[ \t]{0,8}(?://|/\*{1,8}|\*)[^\n]{0,300}?(?i:auto-generated|autogenerated|auto generated|@generated|generated by)|\A(?:[^\n]{0,400}\n){0,9}?[ \t]{0,8}(?://|/\*{1,8}|\*)[^\n]{0,300}?(?i:do not edit|do not modify|do not hand-edit|changes will be lost|changes will be overwritten)";

	// ------------------------------------------------------------------------------------------
	// The flow signals
	// ------------------------------------------------------------------------------------------

	internal const string EX_JS_120 =
		@"(?<![\w$])(?<signal>getElementById|querySelectorAll|querySelector)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,100}?(?:" + _CREDENTIAL_WORD + @"|type\s{0,10}=\s{0,10}[\x27\x22]?password)[^\x27\x22`\n]{0,100}[\x27\x22`]" + _SPACE + @"\)[\s\S]{0,200}?\.value(?![\w$])";

	private const string _FORM_RECEIVER =
		@"(?<![\w$.])(?:[A-Za-z_$][\w$]{0,40}\.){0,3}(?:[\w$]{0,40}(?i:form)[\w$]{0,40}|elements)(?:\[\d{1,3}\])?(?:\.elements)?";

	private const string _FORM_DATA_RECEIVER =
		@"(?:new[ \t]{1,8}FormData" + _SPACE + @"\([^)\n]{0,200}\)|(?<![\w$.])[\w$]{0,40}(?i:formdata))";

	/// <summary>Each alternative opens at a literal (the dot or the bracket), and the receiver that precedes it is read by a look-behind once the property has matched.</summary>
	internal const string EX_JS_121 =
		@"(?:\.(?<signal>password)(?![\w$])(?<=" + _FORM_RECEIVER + @"\.password)\.value(?![\w$])|\[\s{0,10}[\x27\x22](?<signal>password)[\x27\x22]\s{0,10}\](?<=" + _FORM_RECEIVER + @"\[\s{0,10}[\x27\x22]password[\x27\x22]\s{0,10}\])\.value(?![\w$])|\.(?<signal>get)(?<=" + _FORM_DATA_RECEIVER + @"\.get)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]|\.(?<signal>namedItem)(?![\w$])(?<=" + _FORM_RECEIVER + @"\.namedItem)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]" + _SPACE + @"\)\.value(?![\w$]))";

	private const string _PROMPT_DECLARATION_BEHIND =
		@"(?<!(?:(?:function|const|let|var|class)[ \t]{1,8}prompt(?![\w$])|[(,][ \t]{0,8}prompt[ \t]{0,8}[,)=]|import[^\n;]{0,200}(?<![\w$.])prompt(?![\w$]))[^\n]{0,400}(?:\n[^\n]{0,400}){0,30})";

	internal const string EX_JS_122 =
		@"(?:(?<![\w$.])window\.(?<signal>prompt)(?![\w$])" + _SPACE + @"\(|(?<![\w$.])(?<signal>prompt)(?![\w$])" + _PROMPT_DECLARATION_BEHIND + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,200}?" + _CREDENTIAL_WORD + @")";

	internal const string EX_JS_123 =
		@"(?<![\w$])(?<signal>localStorage|sessionStorage)(?:\.getItem" + _SPACE + @"\(|\[)" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,100}?(?i:password|passwd|pwd|credentials)[^\x27\x22`\n]{0,100}[\x27\x22`]";

	internal const string EX_JS_124 =
		_OPEN_AFTER_A_DOT + @"(?<signal>open)(?![\w$])" + _NOT_ON_A_WINDOW_OR_DOCUMENT + _SPACE + @"\(" + _FIVE_ARGUMENTS + @"(?=(?>(?:" + _ARGUMENT_UNIT_WITHOUT_A_COMMA + @"){1,300})" + _SPACE + @"\))";

	internal const string EX_JS_125 =
		@"(?<signal>(?i:<input))(?=[\s\S]{0,200}?(?<![\w$-])type\s{0,10}=\s{0,10}\\?[\x27\x22]?password(?![\w-]))";

	internal const string EX_JS_126 =
		@"(?:(?<![\w$.])(?:req|request|ctx\.request)\.(?<signal>body)(?:\.(?:" + _CREDENTIAL_NAME + @")(?![\w$])|\[\s{0,10}[\x27\x22](?:" + _CREDENTIAL_NAME + @")[\x27\x22]\s{0,10}\])|\{[^{}\n]{0,200}?(?<![\w$])" + _CREDENTIAL_NAME + @"(?![\w$])[^{}\n]{0,200}\}\s{0,10}=\s{0,10}(?<![\w$.])(?:req|request|ctx\.request)\.(?<signal>body)(?![\w$])|" + _SEARCH_PARAMS_FROM_THE_REQUEST_INLINE + @"|" + _SEARCH_PARAMS_FROM_THE_REQUEST_BOUND + @")";

	/// <summary>A word that says the text read is the request's own body.</summary>
	private const string _REQUEST_BODY_WORD =
		@"(?<![\w$])(?i:req|request|body|chunks?|payload)(?![\w$])";

	/// <summary>The inline form: `new URLSearchParams(body).get('password')`, the constructor's argument naming the request's text.</summary>
	private const string _SEARCH_PARAMS_FROM_THE_REQUEST_INLINE =
		@"(?<![\w$.])new[ \t]{1,8}URLSearchParams" + _SPACE + @"\((?=[^)\n]{0,200}" + _REQUEST_BODY_WORD + @")(?>[^)\n]{0,200})\)" + _SPACE + @"\.(?<signal>get)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]";

	/// <summary>The bound form: a name bound to `new URLSearchParams(` of the request's text within the definition window, then `.get('password')` on it.</summary>
	private const string _SEARCH_PARAMS_FROM_THE_REQUEST_BOUND =
		@"\.(?<signal>get)(?![\w$])(?=" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22])(?<=(?<![\w$.])(?<receiver>[A-Za-z_$][\w$]{0,40})\.get)(?<=(?<![\w$.])(?:(?:const|let|var)[ \t]{1,8})?\k<receiver>" + _BLANKS + @"=" + _BLANKS + @"new[ \t]{1,8}URLSearchParams" + _BLANKS + @"\((?=[^)\n]{0,200}" + _REQUEST_BODY_WORD + @")[^\n]{0,400}(?:\n[^\n]{0,400}){0,30})" + _SPACE + @"\(" + _SPACE + @"[\x27\x22]password[\x27\x22]";

	internal const string EX_JS_127 =
		@"(?<![\w$])(?<signal>_writeToOutput)" + _BLANKS + @"=(?!=)|(?<![\w$])(?<signal>setRawMode)(?![\w$])" + _SPACE + @"\((?:(?<=(?i:passw(?:or)?d|passphrase|secret|credential|pwd)[\s\S]{0,600})|(?=[\s\S]{0,600}?(?i:passw(?:or)?d|passphrase|secret|credential|pwd)))";

	internal const string EX_JS_128 =
		@"(?<![\w$.\x27\x22])(?<signal>type)(?![\w$])" + _SPACE + @":" + _SPACE + @"[\x27\x22`]password[\x27\x22`]";

	internal const string EX_JS_129 =
		@"(?<![\w$])(?<signal>question|prompt)(?![\w$])" + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,200}?(?i:password|passphrase)";

	internal const string EX_JS_130 =
		@"(?<![\w$.])process\.(?<signal>env)(?:\.(?<name>" + _CREDENTIAL_NAME + @")(?![\w$])|\[\s{0,10}[\x27\x22](?<name>" + _CREDENTIAL_NAME + @")[\x27\x22]\s{0,10}\])";

	internal const string EX_JS_131 =
		@"(?<![\w$.])(?<signal>config|conf|nconf|settings|cfg)(?:\.(?:get|getString|getValue|require)" + _SPACE + @"\(" + _SPACE + @"[\x27\x22`][^\x27\x22`\n]{0,100}?" + _CREDENTIAL_WORD + @"|\.(?<name>" + _CREDENTIAL_NAME + @")(?![\w$]))";

	internal const string EX_JS_132 =
		@"(?<![\w$])(?<signal>getSecretValue|GetSecretValueCommand|getSecret|accessSecretVersion|getPassword)" + _SPACE + @"\(";

	internal const string EX_JS_133 =
		@"(?<![\w$])(?:(?:const|let|var)[ \t]{1,8})?(?<signal>" + _CREDENTIAL_NAME + @")(?<!(?:Label|Placeholder|Field|Name|Pattern|Regex|Length|Hint|Message|Error))" + _BLANKS + @"[=:](?![=>])" + _SPACE + @"(?:\x27(?!(?:include|omit|same-origin)\x27)[^\x27\n]{1,400}\x27|\x22(?!(?:include|omit|same-origin)\x22)[^\x22\n]{1,400}\x22|`(?!(?:include|omit|same-origin)`)[^`$\n]{1,400}`)";

	/// <summary>
	/// The first interactive signal: `question(` on a name that a `createInterface(` call binds within the definition window of the same file. The prompt's wording is not read, so a
	/// question asked through a helper that holds the call is read as well.
	/// </summary>
	internal const string EX_JS_134 =
		@"\.(?<signal>question)(?![\w$])(?=" + _SPACE + @"\()(?<=(?<![\w$.])(?<receiver>[A-Za-z_$][\w$]{0,40})\.question)(?<=(?<![\w$.])(?:(?:const|let|var)[ \t]{1,8})?\k<receiver>" + _BLANKS + @"=" + _BLANKS + @"(?:await[ \t]{1,8})?(?:[A-Za-z_$][\w$]{0,40}\.){0,2}createInterface" + _BLANKS + @"\([^\n]{0,400}(?:\n[^\n]{0,400}){0,30})" + _SPACE + @"\(";

	/// <summary>The second interactive signal: the constructor of a `Writable` whose text names a muted state within 400 units (the idiom that hides typed characters).</summary>
	internal const string EX_JS_135 =
		@"(?<![\w$.])new[ \t]{1,8}(?<signal>Writable)(?![\w$])" + _SPACE + @"\((?=[\s\S]{0,400}?(?:(?<![A-Za-z])(?i:mute)|(?<=[a-z])M(?i:ute))d?(?![A-Za-z]))";

	#endregion

	[GeneratedRegex(EX_JS_100, _OPTIONS, matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	internal static partial Regex Ex100();

	[GeneratedRegex(EX_JS_101, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex101();

	[GeneratedRegex(EX_JS_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex001();

	[GeneratedRegex(EX_JS_002, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex002();

	[GeneratedRegex(EX_JS_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex003();

	[GeneratedRegex(EX_JS_004, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex004();

	[GeneratedRegex(EX_JS_005, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex005();

	[GeneratedRegex(EX_JS_006, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex006();

	[GeneratedRegex(EX_JS_007, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex007();

	[GeneratedRegex(EX_JS_008, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex008();

	[GeneratedRegex(EX_JS_009, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex009();

	[GeneratedRegex(EX_JS_010, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex010();

	[GeneratedRegex(EX_JS_011, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex011();

	[GeneratedRegex(EX_JS_012, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex012();

	[GeneratedRegex(EX_JS_013, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex013();

	[GeneratedRegex(EX_JS_014, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex014();

	[GeneratedRegex(EX_JS_015, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex015();

	[GeneratedRegex(EX_JS_016, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex016();

	[GeneratedRegex(EX_JS_017, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex017();

	[GeneratedRegex(EX_JS_110, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex110();

	[GeneratedRegex(EX_JS_111, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex111();

	[GeneratedRegex(EX_JS_120, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex120();

	[GeneratedRegex(EX_JS_121, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex121();

	[GeneratedRegex(EX_JS_122, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex122();

	[GeneratedRegex(EX_JS_123, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex123();

	[GeneratedRegex(EX_JS_124, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex124();

	[GeneratedRegex(EX_JS_125, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex125();

	[GeneratedRegex(EX_JS_126, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex126();

	[GeneratedRegex(EX_JS_127, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex127();

	[GeneratedRegex(EX_JS_128, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex128();

	[GeneratedRegex(EX_JS_129, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex129();

	[GeneratedRegex(EX_JS_130, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex130();

	[GeneratedRegex(EX_JS_131, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex131();

	[GeneratedRegex(EX_JS_132, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex132();

	[GeneratedRegex(EX_JS_133, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex133();

	[GeneratedRegex(EX_JS_134, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex134();

	[GeneratedRegex(EX_JS_135, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex135();
}
