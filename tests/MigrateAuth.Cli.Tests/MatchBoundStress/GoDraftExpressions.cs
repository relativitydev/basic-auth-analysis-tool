using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE DRAFT `Go` EXPRESSIONS OF THE COST GATE RUN BEFORE THE CATALOG WAS FINAL: the expressions of the draft detection catalog (the `Go` catalog, version 0.2, and the corrections of version 0.5: the closing tails of GO-001 and GO-007, the whole-value rule of the GO-002 token, the encoded Basic helper row of GO-011, the member declaration EX-GO-102 and the call-site class GO-013), as test-local constants, each compiled
/// by <c>[GeneratedRegex]</c> the way the analyzer will compile it, with the policy match timeout of the recognition engine. Nothing here is read from <c>src/</c>: no `Go` expression
/// exists there before the catalog is final.
///
/// THE CATALOG STATES THESE EXPRESSIONS AS PROSE (the class entries, the constant declaration, the two content markers and the flow signals) AND BY EXPRESSION IDENTIFIER. This file is the TIGHTEST
/// FAITHFUL REGULAR-EXPRESSION FORM OF EACH, built from the shapes the catalog's expression-shape section fixes: the argument window balanced to a depth of four by unrolling, bounded at 1,500 units, lazy and always
/// required; the definition window of thirty lines of at most 400 characters; the statement window of 300 characters; the whole-word header name; the four carriers; refusals placed after the
/// positive match; a frequent literal (<c>Set</c>, <c>Add</c>, <c>Get</c>) opened by a dot or followed at once by a rare literal; a look-behind that reads what precedes an anchor placed after the
/// rare literal, never at every character of a window. No lazy bounded loop sits in an optional group (F11); the exposure scan of <see cref="GoDraftExpressionExposureTests"/> is what shows it.
///
/// A SECOND SHAPE OF THE GENERATOR DEFECT, FOUND WHILE WRITING THIS DRAFT: a lazy bounded loop that FOLLOWS an optional group (an optional receiver, an optional type, an optional second parameter)
/// makes the source-generated matcher read past the loop's bound, while the interpreter stops at it. The scan of the exposure tests cannot see it (it looks for a loop INSIDE an optional group), so the draft
/// reads the definition window greedily, keeps every lazy window ahead of every optional group, and the semantic tests compare the generated and the interpreted form at the bound and one past it.
///
/// A CLASS'S ANCHOR is the capture group <c>construct</c>, a flow signal's the group <c>anchor</c> (<see cref="FlowSignal.AnchorGroupName"/>), a literal credential the group <c>credential</c> (the
/// catalog's placeholder vocabulary refused). Zones are the engine's: an expression reads the decoded text, and the scanner's mask decides whether an anchor lies in code, a comment or a string.
/// `Go` declares no first-line rule, so there is no lexical-budget expression here.
/// </summary>
internal static partial class GoDraftExpressions
{
	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private const RegexOptions _LINE_OPTIONS = _OPTIONS | RegexOptions.Multiline;

	/// <summary>The expressions charged to the recognition engine's match bound: the constant declaration (the symbol table, F18), the class rules, the two content markers and the flow signals.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> MatchBoundExpressions { get; } =
	[
		("EX-GO-101", Ex101()),
		("EX-GO-001", Ex001()),
		("EX-GO-002", Ex002()),
		("EX-GO-003", Ex003()),
		("EX-GO-004", Ex004()),
		("EX-GO-005", Ex005()),
		("EX-GO-006", Ex006()),
		("EX-GO-007", Ex007()),
		("EX-GO-008", Ex008()),
		("EX-GO-009", Ex009()),
		("EX-GO-010", Ex010()),
		("EX-GO-011", Ex011()),
		("EX-GO-012", Ex012()),
		("EX-GO-013", Ex013()),
		("EX-GO-102", Ex102()),
		("EX-GO-110", Ex110()),
		("EX-GO-111", Ex111()),
		("EX-GO-120", Ex120()),
		("EX-GO-121", Ex121()),
		("EX-GO-122", Ex122()),
		("EX-GO-123", Ex123()),
		("EX-GO-124", Ex124()),
		("EX-GO-125", Ex125()),
		("EX-GO-130", Ex130()),
		("EX-GO-131", Ex131()),
		("EX-GO-132", Ex132()),
		("EX-GO-133", Ex133()),
		("EX-GO-134", Ex134())
	];

	#region The expression text, as built from the shapes the catalog fixes

	// ------------------------------------------------------------------------------------------
	// Words and literals. A literal is read as one atomic, bounded unit: an interpreted string, a raw string, a rune, and (inside a window) a line comment or a block comment.
	// ------------------------------------------------------------------------------------------

	private const string _NOT_WORD_BEHIND =
		@"(?<![\p{L}\p{N}_])";

	private const string _NOT_WORD_AHEAD =
		@"(?![\p{L}\p{N}_])";

	/// <summary>An identifier read atomically, so that a failure after it never gives a character of it back.</summary>
	private const string _IDENTIFIER =
		@"(?>[\p{L}_][\p{L}\p{N}_]{0,63})";

	/// <summary>The same identifier, non-atomic, for the forms read inside a look-behind.</summary>
	private const string _IDENTIFIER_PLAIN =
		@"[\p{L}_][\p{L}\p{N}_]{0,63}";

	/// <summary>An optional qualifier: a package name or a receiver, a dot (an import alias is a qualifier the expression cannot verify).</summary>
	private const string _QUALIFIER =
		@"(?:[\p{L}_][\p{L}\p{N}_]{0,40}\.)?";

	private const string _DECLARED_TYPE =
		@"(?:\*|\[\])?[\p{L}_][\p{L}\p{N}_.]{0,40}";

	private const string _DQ_LITERAL =
		@"\x22(?>[^\x22\\\n]|\\[^\n]){0,1000}\x22";

	private const string _RAW_LITERAL =
		@"`[^`]{0,1000}`";

	private const string _RUNE_LITERAL =
		@"\x27(?>[^\x27\\\n]|\\[^\n]){1,16}\x27";

	private const string _LINE_COMMENT =
		@"//[^\n]{0,1000}";

	private const string _BLOCK_COMMENT =
		@"/\*(?>[^*]|\*(?!/)){0,1000}\*/";

	/// <summary>The ordinary character is the first alternative, because it is by far the commonest unit; the quote or slash that opens no literal is the last.</summary>
	private const string _LEAF_ALTERNATIVES =
		@"[^\x27\x22`()\[\]{}/]|" + _RUNE_LITERAL + @"|" + _DQ_LITERAL + @"|" + _RAW_LITERAL + @"|" + _LINE_COMMENT + @"|" + _BLOCK_COMMENT + @"|[\x27\x22`/]";

	private const string _LEAF_ALTERNATIVES_WITHOUT_A_COMMA =
		@"[^\x27\x22`()\[\]{},/]|" + _RUNE_LITERAL + @"|" + _DQ_LITERAL + @"|" + _RAW_LITERAL + @"|" + _LINE_COMMENT + @"|" + _BLOCK_COMMENT + @"|[\x27\x22`/]";

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

	/// <summary>One unit of an argument window: a literal, a comment, a character, or a balanced group to depth four.</summary>
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

	/// <summary>The window of a conversion or of a pair: at most 300 units, lazy, required.</summary>
	private const string _SHORT_WINDOW =
		@"(?:" + _ARGUMENT_UNIT + @"){0,300}?";

	// ------------------------------------------------------------------------------------------
	// Shared small shapes. The blanks before a refusal are atomic, so that a negative look-ahead cannot be defeated by giving a blank back.
	// ------------------------------------------------------------------------------------------

	private const string _SPACE =
		@"(?>\s{0,40})";

	private const string _PLACEHOLDER_BODY =
		@"(?i:[*x.\-_#]{3,}|<[^>\n]{0,100}>|\[[^\]\n]{0,100}\]|\{\{[^}\n]{0,100}\}\}|%[^%\n]{0,100}%|placeholder|changeme|change-me|change_me|replace-me|replaceme|your-password|your_password|redacted|todo|fixme|dummy|not-a-real-secret)";

	private const string _CREDENTIAL_LITERAL =
		@"(?:\x22(?!" + _PLACEHOLDER_BODY + @"\x22)(?<credential>[^\x22\\\n]{1,200})\x22|`(?!" + _PLACEHOLDER_BODY + @"`)(?<credential>[^`]{1,200})`)";

	/// <summary>One unit of the tail that may stand between the last argument of a call and its closing parenthesis: a blank or a line break, a block comment or a line comment.</summary>
	private const string _CLOSING_TAIL_UNIT =
		@"(?>\s|/\*(?>[^*]|\*(?!/)){0,200}\*/|//[^\n]{0,200})";

	/// <summary>
	/// What may stand between the last argument of a call and its closing parenthesis (a call written as gofmt writes it over several lines): blanks, tabs, line breaks and comments in any order, with at most one comma,
	/// within 200 characters of the parenthesis. It is atomic, bounded and has no lazy loop.
	/// </summary>
	private const string _CLOSING_TAIL =
		@"(?=[\s\S]{0,200}\))(?>(?:" + _CLOSING_TAIL_UNIT + @"){0,200}(?:,(?:" + _CLOSING_TAIL_UNIT + @"){0,200})?)(?<=\k<credential>[\x22`][\s\S]{0,200})";

	private const string _PAIR_CREDENTIAL =
		@"(?:\x22[^\x22\\\n:]{0,100}:(?!" + _PLACEHOLDER_BODY + @"\x22)(?<credential>[^\x22\\\n]{1,200})\x22|`[^`:]{0,100}:(?!" + _PLACEHOLDER_BODY + @"`)(?<credential>[^`]{1,200})`)";

	private const string _WHOLE_LITERAL_PAIR =
		@"(?:\x22[^\x22\\\n:\s]{1,100}:[^\x22\\\n\s]{1,200}\x22|`[^`:\s]{1,100}:[^`\s]{1,200}`)";

	private const string _COLON_LITERAL =
		@"(?:\x22:\x22|`:`)";

	private const string _USERINFO_URL_CREDENTIAL =
		@"(?:(?=[\x22`](?i:https?)://[^\s:/@\x22`\\]{1,64}:(?!" + _PLACEHOLDER_BODY + @"@)(?<credential>[^\s/@\x22`\\]{1,200})@))?";

	private const string _USERINFO_URL =
		_USERINFO_URL_CREDENTIAL + @"[\x22`](?i:https?)://[^\s:/@\x22`\\]{1,64}:[^\s/@\x22`\\]{1,200}@";

	private const string _CREDENTIAL_WORD =
		@"(?:(?i:passw(?:or)?d|secret|credential|creds?|token)|(?<![A-Za-z0-9])(?i:pass|pw|pwd|apikey)(?![A-Za-z0-9])|(?<=[a-z])Pass(?![a-z0-9]))";

	private const string _CREDENTIAL_NAME =
		@"(?:(?=[\p{L}\p{N}_]{0,64}" + _CREDENTIAL_WORD + @")[\p{L}\p{N}_]{1,64})";

	private const string _USE_WORDS =
		@"(?:(?i:basic)(?<![\p{L}\p{N}_](?i:basic))(?![\p{L}\p{N}_])|(?i:authorization))";

	/// <summary>The use words of `GO-006`: the `Authorization` word does not count when it is the tail of `Proxy-Authorization` (a fixed-length look-behind of six characters, case-insensitive like the word; no lazy or wide window).</summary>
	private const string _USE_WORDS_OF_THE_ENCODER =
		@"(?:(?i:basic)(?<![\p{L}\p{N}_](?i:basic))(?![\p{L}\p{N}_])|(?<!(?i:proxy-))(?i:authorization))";

	/// <summary>The header name of a proxy credential, the word the use requirement of `GO-006` refuses (case-insensitive).</summary>
	private const string _PROXY_HEADER_NAME =
		@"(?i:proxy-authorization)";

	/// <summary>
	/// The use requirement of `GO-006`, refused at statement level: a use word within 300 units behind (or ahead) meets it only when `Proxy-Authorization` is not within the same 300 units on that side,
	/// whichever use word (`Basic` or `Authorization`) meets it. The refusals are plain bounded look-behind and greedy look-ahead windows, never lazy; the word-level refusal stays for the window edge,
	/// where the `Proxy-` prefix may fall one unit outside the 300 while the tail `Authorization` falls inside.
	/// </summary>
	private const string _USE_REQUIREMENT =
		@"(?:(?<=" + _USE_WORDS_OF_THE_ENCODER + @"[\s\S]{0,300})(?<!" + _PROXY_HEADER_NAME + @"[\s\S]{0,300})"
			+ @"|(?=[\s\S]{0,300}?" + _USE_WORDS_OF_THE_ENCODER + @")(?![\s\S]{0,300}" + _PROXY_HEADER_NAME + @"))";

	private const string _DEFINITION_WINDOW =
		@"(?:[^\n]{0,400}\n){0,30}[^\n]{0,400}";

	private const string _HEADER_NAME_LITERAL =
		@"(?:\x22(?i:authorization)\x22|`(?i:authorization)`)";

	// ------------------------------------------------------------------------------------------
	// The constant declaration (the symbol table)
	// ------------------------------------------------------------------------------------------

	private const string _ENVIRONMENT_READ =
		_QUALIFIER + @"(?:Getenv|LookupEnv)[ \t]{0,3}\([ \t]{0,3}(?:\x22[^\x22\\\n]{1,128}\x22|`[^`\n]{1,128}`)[ \t]{0,3}\)";

	private const string _DECLARATION_HEAD =
		@"^[ \t]*(?:(?:const|var)[ \t]{1,8})?(?<name>(?!(?:const|var)[ \t])" + _IDENTIFIER_PLAIN + @")(?:[ \t]{1,8}" + _DECLARED_TYPE + @")?[ \t]*(?::=|=(?!=))[ \t]*";

	private const string _TWO_VALUE_HEAD =
		@"^[ \t]*(?<name>" + _IDENTIFIER_PLAIN + @")[ \t]{0,8},[ \t]{0,8}" + _IDENTIFIER_PLAIN + @"[ \t]{0,8}(?::=|=(?!=))[ \t]*";

	internal const string EX_GO_101 =
		_DECLARATION_HEAD + @"(?:\x22(?<value>(?>[^\s\x22\\]*))(?>(?:[^\x22\\\n]|\\.)*)\x22|`(?<value>(?>[^\s`]*))(?>[^`\n]*)`)(?=[ \t]*(?:[;,+]|$|//|/\*))"
			+ @"|" + _DECLARATION_HEAD + @"(?<environment>)" + _ENVIRONMENT_READ + @"(?<fallbackWord>)(?=[ \t]*(?:;|$|//|/\*))"
			+ @"|" + _TWO_VALUE_HEAD + @"(?<environment>)" + _ENVIRONMENT_READ + @"(?<fallbackWord>)(?=[ \t]*(?:;|$|//|/\*))";

	// ------------------------------------------------------------------------------------------
	// The classes
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// GO-001: the method `SetBasicAuth` after a dot, in a call. The dot opens the match and the method is the anchor, so a declaration (`func (c *T) SetBasicAuth(`, a `)` and a blank before the name)
	/// and a type declaration never match. The credential is the last argument when it is the whole of a string literal: the arguments before it are read possessively, one at a time, and the literal is
	/// followed only by the closing tail (blanks, line breaks, at most one comma and comments) and the closing parenthesis.
	/// </summary>
	internal const string EX_GO_001 =
		@"\.(?<construct>SetBasicAuth)" + _NOT_WORD_AHEAD + _SPACE + @"\((?:(?=" + _SPACE + @"(?:(?>" + _ONE_ARGUMENT + @"," + _SPACE + @")){1,8}" + _CREDENTIAL_LITERAL + _CLOSING_TAIL + @"\)))?";

	/// <summary>K1: `Set` or `Add` after a dot, first argument the whole header name `Authorization` as an interpreted or a raw literal. The dot opens the match; the rare literal follows at once.</summary>
	private const string _CARRIER_K1 =
		@"\.(?<construct>Set|Add)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + _HEADER_NAME_LITERAL + _SPACE + @"," + _SPACE;

	/// <summary>The right-hand side of a K2 assignment: a `[]string{` literal, or `append(` with the indexed entry as its first argument.</summary>
	private const string _K2_VALUE_START =
		@"(?:\[\]string" + _SPACE + @"\{" + _SPACE + @"|append" + _SPACE + @"\(" + _SPACE + @"[^,\n]{1,100}," + _SPACE + @")";

	/// <summary>K2: the indexed identifier (the receiver is not constrained), the key `Authorization`, an assignment (never a comparison) of a slice.</summary>
	private const string _CARRIER_K2 =
		_NOT_WORD_BEHIND + @"(?<construct>" + _IDENTIFIER + @")" + _SPACE + @"\[" + _SPACE + _HEADER_NAME_LITERAL + _SPACE + @"\]" + _SPACE + @"=(?!=)" + _SPACE + _K2_VALUE_START;

	private const string _CARRIERS_K1_AND_K2 =
		@"(?:" + _CARRIER_K1 + @"|" + _CARRIER_K2 + @")";

	/// <summary>
	/// The value that starts with the literal `Basic` (the value rule): an interpreted or a raw literal whose first word is `Basic`, followed by a blank or the end of the literal, or `Sprintf` whose
	/// format literal starts so. A token after `Basic ` that is the whole literal is the credential, a placeholder refused.
	/// </summary>
	private const string _BASIC_VALUE =
		@"(?:" + _QUALIFIER + @"Sprintf" + _SPACE + @"\(" + _SPACE + @")?[\x22`](?i:basic)(?=[ \t\x22`])(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"[\x22`])(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22`]" + _SPACE + @"(?:[,)}\];]|\z|//|/\*)))?";

	/// <summary>A value that is an identifier (a precomputed token): not a call, not a member, not an index, and the whole value.</summary>
	private const string _IDENTIFIER_VALUE =
		@"(?![\x22`])(?<scheme>" + _IDENTIFIER + @")(?![\p{L}\p{N}_.(\[])(?=" + _SPACE + @"(?:[,)}\];]|\z))";

	internal const string EX_GO_002 =
		_CARRIERS_K1_AND_K2 + _BASIC_VALUE;

	private const string _KEY_IN_A_LITERAL =
		_HEADER_NAME_LITERAL + _SPACE + @":" + _SPACE;

	internal const string EX_GO_003 =
		_NOT_WORD_BEHIND + @"(?<construct>Header)" + _NOT_WORD_AHEAD + _SPACE + @"\{" + _ARGUMENT_WINDOW + _KEY_IN_A_LITERAL + @"(?:\[\]string" + _SPACE + @")?\{" + _SPACE + _BASIC_VALUE;

	internal const string EX_GO_004 =
		_NOT_WORD_BEHIND + @"(?<construct>map)" + _NOT_WORD_AHEAD + @"\[string\](?:\[\]string|string)" + _SPACE + @"\{" + _ARGUMENT_WINDOW + _KEY_IN_A_LITERAL + @"(?:(?:\[\]string)?" + _SPACE + @"\{" + _SPACE + @")?" + _BASIC_VALUE;

	private const string _CALLEES_WITH_A_URL =
		@"NewRequestWithContext|NewRequest|PostForm|Post|Head|Get";

	internal const string EX_GO_005 =
		_NOT_WORD_BEHIND + @"(?<construct>" + _CALLEES_WITH_A_URL + @")" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _ARGUMENT_WINDOW + _USERINFO_URL;

	private const string _SPRINTF_PAIR =
		@"[\p{L}\p{N}_.]{0,41}Sprintf" + _SPACE + @"\(" + _SPACE + @"\x22[^\x22%\\\n]{0,10}%[-+# 0-9.]{0,6}[a-zA-Z]:%[-+# 0-9.]{0,6}[a-zA-Z]";

	private const string _JOIN_PAIR =
		@"[\p{L}\p{N}_.]{0,41}Join" + _SPACE + @"\(" + _SHORT_WINDOW + @"," + _SPACE + _COLON_LITERAL + _SPACE + @"\)";

	private const string _CONCATENATED_PAIR =
		@"(?=" + _SHORT_WINDOW + @"\+" + _SPACE + _COLON_LITERAL + _SPACE + @"\+)";

	/// <summary>The source argument: a `[]byte(` conversion of a colon-joined pair. The window to the conversion is the call's own argument window.</summary>
	private const string _PAIR_CONVERSION =
		@"(?=" + _ARGUMENT_WINDOW + @"\[\]byte" + _SPACE + @"\(" + _SPACE + @"(?:" + _WHOLE_LITERAL_PAIR + _SPACE + @"\)|" + _SPRINTF_PAIR + @"|" + _JOIN_PAIR + @"|" + _CONCATENATED_PAIR + @"))";

	private const string _WHOLE_LITERAL_PAIR_CREDENTIAL =
		@"(?:(?=" + _SPACE + @"\[\]byte" + _SPACE + @"\(" + _SPACE + _PAIR_CREDENTIAL + _SPACE + @"\)))?";

	internal const string EX_GO_006 =
		_NOT_WORD_BEHIND + @"(?<construct>RawStdEncoding|StdEncoding)" + _SPACE + @"\." + _SPACE + @"(?:EncodeToString|AppendEncode|Encode)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _PAIR_CONVERSION + _USE_REQUIREMENT + _WHOLE_LITERAL_PAIR_CREDENTIAL;

	private const string _TWO_ARGUMENTS_TO_THE_CLOSING_PARENTHESIS =
		@"(?=" + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @"(?=" + _SPACE + @"(?:," + _SPACE + @")?\)))?(?>(?:" + _ARGUMENT_UNIT_WITHOUT_A_COMMA + @"){0,300})" + _SPACE + @"(?:," + _SPACE + @")?\))";

	/// <summary>The refusals of `UserPassword`, read after the rare literal: a declaration, the argument window of `ProxyURL(`, a name that holds `proxy` or `proxies`, and a `url.URL` literal whose `Scheme:` is another literal.</summary>
	private const string _USER_PASSWORD_REFUSALS =
		@"(?<!(?:func|type)[ \t]{1,8}UserPassword)(?<!\)[ \t]{0,8}UserPassword)"
			+ @"(?<!ProxyURL" + _SPACE + @"\([\s\S]{0,400}UserPassword)"
			+ @"(?<!(?i:proxy|proxies)[\p{L}\p{N}_.]{0,40}[ \t]{0,8}(?::=|=|:)[^\n]{0,200}UserPassword)"
			+ @"(?<!Scheme[ \t]{0,3}:[ \t]{0,3}\x22(?!(?i:https?)\x22)[A-Za-z][A-Za-z0-9+.-]{0,20}\x22[^\n]{0,400}UserPassword)";

	internal const string EX_GO_007 =
		_NOT_WORD_BEHIND + @"(?<construct>UserPassword)" + _NOT_WORD_AHEAD + _USER_PASSWORD_REFUSALS + _SPACE + @"\(" + _TWO_ARGUMENTS_TO_THE_CLOSING_PARENTHESIS;

	private const string _NAME_WITHOUT_PROXY =
		@"(?![\p{L}\p{N}_]{0,64}(?i:proxy|proxies))";

	/// <summary>
	/// A declaration is read from the line start, as the constant declaration is (a leading blank or tab is allowed, so a name inside a block is read): the optional keyword, the declared name (the anchor, a
	/// name that holds `proxy` or `proxies` refused), an optional type, the assignment, and the credentialed URL literal. Read backwards from the equals sign, a type would be mistaken for the name.
	/// </summary>
	private const string _HELD_URL_DECLARATION =
		@"^[ \t]{0,16}(?:(?:const|var)[ \t]{1,8})?(?<construct>(?!(?:const|var)[ \t])" + _NAME_WITHOUT_PROXY + _IDENTIFIER_PLAIN + @")(?:[ \t]{1,8}" + _DECLARED_TYPE + @")?[ \t]{0,8}(?::=|=(?!=))" + _SPACE + _USERINFO_URL;

	internal const string EX_GO_008 =
		@"(?:" + _NOT_WORD_BEHIND + @"(?<construct>ParseRequestURI|Parse)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + _USERINFO_URL + @"|" + _HELD_URL_DECLARATION + @")";

	internal const string EX_GO_009 =
		_CARRIERS_K1_AND_K2 + _IDENTIFIER_VALUE;

	private const string _ENVIRONMENT_VALUE =
		_QUALIFIER + @"(?:Getenv|LookupEnv)" + _SPACE + @"\(" + _SPACE + @"(?:" + _DQ_LITERAL + @"|" + _RAW_LITERAL + @")" + _SPACE + @"\)(?=" + _SPACE + @"(?:[,)}\];]|\z))";

	internal const string EX_GO_012 =
		_CARRIERS_K1_AND_K2 + @"(?:" + _ENVIRONMENT_VALUE + @"|" + _IDENTIFIER_VALUE + @")";

	private const string _BASIC_LEAD =
		@"(?:[\p{L}\p{N}_.]{0,41}Sprintf" + _SPACE + @"\(" + _SPACE + @")?[\x22`](?i:basic)(?=[ \t\x22`])";

	/// <summary>The use requirement read greedily (no lazy loop may follow the optional groups of a function head).</summary>
	private const string _USE_REQUIREMENT_GREEDY =
		@"(?:(?<=" + _USE_WORDS + @"[\s\S]{0,300})|(?=[\s\S]{0,300}" + _USE_WORDS + @"))";

	/// <summary>
	/// The constructs that count in a body, each as its own class reads it and none of them lazy: K1 (`Set` or `Add`, `Authorization`, a leading-`Basic` value), K2 (an index assignment of a `[]string{` or
	/// `append(` with a leading-`Basic` value), K3 (a `Header{` literal), K4 (a map literal) and the encoder of a colon-joined pair with the use requirement. A bare `Basic` literal counts for none.
	/// </summary>
	private const string _BODY_CONSTRUCTS =
		@"(?:\.(?:Set|Add)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + _HEADER_NAME_LITERAL + _SPACE + @"," + _SPACE + _BASIC_LEAD
			+ @"|" + _NOT_WORD_BEHIND + _IDENTIFIER + _SPACE + @"\[" + _SPACE + _HEADER_NAME_LITERAL + _SPACE + @"\]" + _SPACE + @"=(?!=)" + _SPACE + _K2_VALUE_START + _BASIC_LEAD
			+ @"|" + _NOT_WORD_BEHIND + @"Header" + _NOT_WORD_AHEAD + _SPACE + @"\{[^\n]{0,200}" + _HEADER_NAME_LITERAL + _SPACE + @":" + _SPACE + @"(?:\[\]string" + _SPACE + @")?\{" + _SPACE + _BASIC_LEAD
			+ @"|" + _NOT_WORD_BEHIND + @"map\[string\](?:\[\]string|string)" + _SPACE + @"\{[^\n]{0,200}" + _HEADER_NAME_LITERAL + _SPACE + @":" + _SPACE + @"(?:(?:\[\]string)?" + _SPACE + @"\{" + _SPACE + @")?" + _BASIC_LEAD
			+ @"|" + _NOT_WORD_BEHIND + @"(?:RawStdEncoding|StdEncoding)" + _SPACE + @"\." + _SPACE + @"(?:EncodeToString|AppendEncode|Encode)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _USE_REQUIREMENT_GREEDY + @"(?=[^\n]{0,300}(?:" + _COLON_LITERAL + @"|%[-+# 0-9.]{0,6}[a-zA-Z]:%|\x22[^\x22\\\n:]{1,100}:[^\x22\\\n]{1,200}\x22)))";

	internal const string EX_GO_010 =
		_NOT_WORD_BEHIND + @"func" + _SPACE + @"\(" + _SPACE + @"[^)\n]{0,100}\)" + _SPACE + @"(?<construct>RoundTrip)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _DEFINITION_WINDOW + @"(?:\.SetBasicAuth" + _SPACE + @"\(|" + _BODY_CONSTRUCTS + @")";

	/// <summary>The head, from the identifier after `func` through the eighth parameter, is read atomically, so a failed body window is not retried once per parameter variant (no type or parameter group is given back).</summary>
	private const string _FUNCTION_HEAD =
		_NOT_WORD_BEHIND + @"func[ \t]{1,8}(?:\([^)\n]{0,100}\)[ \t]{0,3})?(?!RoundTrip" + _NOT_WORD_AHEAD + @")(?>(?<construct>" + _IDENTIFIER + @")" + _FUNCTION_PARAMETERS + @")";

	/// <summary>The opening parenthesis and the parameters of a function head, through the eighth, each with its optional type; read inside the atomic head and inside the member declaration's look-ahead.</summary>
	private const string _FUNCTION_PARAMETERS =
		@"[ \t]{0,3}\(" + _SPACE + @"(?<p1>" + _IDENTIFIER + @")" + _PARAMETER_TYPE + _NEXT_PARAMETER_2 + _NEXT_PARAMETER_3 + _NEXT_PARAMETER_4 + _NEXT_PARAMETER_5 + _NEXT_PARAMETER_6 + _NEXT_PARAMETER_7 + _NEXT_PARAMETER_8;

	/// <summary>One of the function's own parameters, as a whole word never right after a dot: the group of each parameter that exists, an unset group failing its back-reference.</summary>
	private const string _OWN_PARAMETER =
		@"(?<![\p{L}\p{N}_.])(?:\k<p1>|\k<p2>|\k<p3>|\k<p4>|\k<p5>|\k<p6>|\k<p7>|\k<p8>)" + _NOT_WORD_AHEAD;

	/// <summary>The function's own parameters, up to eight, each a capture group; an unset group fails its back-reference, so only the parameters that exist are compared.</summary>
	private const string _PARAMETER_TYPE =
		@"(?:[ \t]{1,8}(?:\*|\[\]|\.\.\.)?[\p{L}_][\p{L}\p{N}_.]{0,40})?";

	private const string _NEXT_PARAMETER_2 = @"(?:" + _SPACE + @"," + _SPACE + @"(?<p2>" + _IDENTIFIER + @")" + _PARAMETER_TYPE + @")?";
	private const string _NEXT_PARAMETER_3 = @"(?:" + _SPACE + @"," + _SPACE + @"(?<p3>" + _IDENTIFIER + @")" + _PARAMETER_TYPE + @")?";
	private const string _NEXT_PARAMETER_4 = @"(?:" + _SPACE + @"," + _SPACE + @"(?<p4>" + _IDENTIFIER + @")" + _PARAMETER_TYPE + @")?";
	private const string _NEXT_PARAMETER_5 = @"(?:" + _SPACE + @"," + _SPACE + @"(?<p5>" + _IDENTIFIER + @")" + _PARAMETER_TYPE + @")?";
	private const string _NEXT_PARAMETER_6 = @"(?:" + _SPACE + @"," + _SPACE + @"(?<p6>" + _IDENTIFIER + @")" + _PARAMETER_TYPE + @")?";
	private const string _NEXT_PARAMETER_7 = @"(?:" + _SPACE + @"," + _SPACE + @"(?<p7>" + _IDENTIFIER + @")" + _PARAMETER_TYPE + @")?";
	private const string _NEXT_PARAMETER_8 = @"(?:" + _SPACE + @"," + _SPACE + @"(?<p8>" + _IDENTIFIER + @")" + _PARAMETER_TYPE + @")?";

	/// <summary>The value or pair operand of the construct: the rest of the construct's own statement (no `;`, no brace), where a whole-word parameter must appear.</summary>
	private const string _COUNTED_CONSTRUCT_OVER_AN_OWN_PARAMETER =
		_DEFINITION_WINDOW + _BODY_CONSTRUCTS + @"[^\n;{}]{0,300}" + _OWN_PARAMETER;

	/// <summary>The encoder of the encoded Basic helper (0.5): `StdEncoding` or `RawStdEncoding` with `.EncodeToString(`, `.Encode(` or `.AppendEncode(`, over any operand.</summary>
	private const string _ENCODER_OVER_ANY_OPERAND =
		_NOT_WORD_BEHIND + @"(?:RawStdEncoding|StdEncoding)" + _SPACE + @"\." + _SPACE + @"(?:EncodeToString|AppendEncode|Encode)" + _NOT_WORD_AHEAD + _SPACE + @"\(";

	/// <summary>The encoder call as the operand of a concatenation: the qualifier, if any, `StdEncoding` or `RawStdEncoding`, a dot and the method.</summary>
	private const string _ENCODER_CALL_OPERAND =
		_QUALIFIER + @"(?:RawStdEncoding|StdEncoding)" + _SPACE + @"\." + _SPACE + @"(?:EncodeToString|AppendEncode|Encode)" + _NOT_WORD_AHEAD;

	private const string _DOTTED_IDENTIFIER =
		_IDENTIFIER + @"(?:\." + _IDENTIFIER + @"){0,100}";

	/// <summary>
	/// The leading literal of the encoded Basic helper (0.5): a whole literal whose body is `Basic` (any case) and one blank, concatenated with `+` to an identifier with dotted selectors or to the encoder call.
	/// </summary>
	private const string _BASIC_LITERAL_CONCATENATED =
		@"(?:\x22(?i:basic)[ \t]\x22|`(?i:basic)[ \t]`)" + _SPACE + @"\+" + _SPACE + @"(?:" + _ENCODER_CALL_OPERAND + @"|" + _DOTTED_IDENTIFIER + @")";

	/// <summary>
	/// The encoded Basic helper row (0.5), zero-width: an encoder over any operand and a leading `"Basic "` literal concatenated with an identifier or the encoder call, in either order inside the definition window of
	/// the head, and one of the function's parameters anywhere in the same window. Each conjunct is its own bounded look-ahead.
	/// </summary>
	private const string _ENCODED_BASIC_HELPER =
		@"(?=" + _DEFINITION_WINDOW + _ENCODER_OVER_ANY_OPERAND + @")(?=" + _DEFINITION_WINDOW + _BASIC_LITERAL_CONCATENATED + @")(?=" + _DEFINITION_WINDOW + _OWN_PARAMETER + @")";

	/// <summary>What GO-011 reads after the head: a counted construct over an own parameter, or the encoded Basic helper row.</summary>
	private const string _BASIC_HELPER_BODY =
		@"(?:" + _COUNTED_CONSTRUCT_OVER_AN_OWN_PARAMETER + @"|" + _ENCODED_BASIC_HELPER + @")";

	internal const string EX_GO_011 =
		_FUNCTION_HEAD + _BASIC_HELPER_BODY;

	/// <summary>
	/// EX-GO-102, the member declaration of the file symbol table's second dictionary (0.5): one record per `func` declaration at the line start, a function or a method, its name and an empty mark when GO-011 reads the declaration
	/// (its head, its window and its conjunction). The mark is the engine's own empty group for a record of the second kind, <c>environment</c> with its empty <c>fallbackWord</c>, and the empty group <c>basicHelper</c> names it for
	/// the catalog. A `RoundTrip` method, a generic function and a function with no parameter are recorded and never marked.
	/// </summary>
	internal const string EX_GO_102 =
		@"^[ \t]{0,64}func[ \t]{1,8}(?:\([^)\n]{0,200}\)[ \t]{0,3})?(?<name>" + _IDENTIFIER + @")(?=[ \t]{0,3}[(\[])(?:(?<!(?<![\p{L}\p{N}_])RoundTrip)(?=(?>" + _FUNCTION_PARAMETERS + @")" + _BASIC_HELPER_BODY + @")(?<environment>)(?<fallbackWord>)(?<basicHelper>))?";

	/// <summary>
	/// EX-GO-013: a carrier K1 or K2 for `Authorization` whose value is a call `name(…)` to a bare name, with no dot before it, and nothing after the call but an optional trailing comma and what closes the carrier. The group <c>name</c> is the called name,
	/// which the engine resolves against the member table.
	/// </summary>
	internal const string EX_GO_013 =
		_CARRIERS_K1_AND_K2 + @"(?<![\p{L}\p{N}_.])(?<name>" + _IDENTIFIER + @")[ \t]{0,3}\(" + _ARGUMENT_WINDOW + @"\)(?=" + _SPACE + @"(?:," + _SPACE + @")?[)}])";


	// ------------------------------------------------------------------------------------------
	// The two content markers (demotion)
	// ------------------------------------------------------------------------------------------

	/// <summary>The generated-file header: the line-comment form on a line of its own, preceded by nothing but blank lines and line comments (the first fifty lines).</summary>
	internal const string EX_GO_110 =
		@"\A(?:[ \t]{0,8}(?://[^\n]{0,300})?\n){0,50}[ \t]{0,8}//[ ]Code generated [^\n]{0,300}[ ]DO NOT EDIT\.[ \t]{0,8}(?:\n|\z)";

	/// <summary>A documentation example: `func` at the line start, the name (`Example`, `ExampleName`, `ExampleType_Method`, `Example_suffix`), then `(` and `)`.</summary>
	internal const string EX_GO_111 =
		@"^func[ \t]{1,8}Example(?:[_\p{Lu}][\p{L}\p{N}_]{0,60})?[ \t]{0,8}\([ \t]{0,8}\)";

	// ------------------------------------------------------------------------------------------
	// The flow signals
	// ------------------------------------------------------------------------------------------

	private const string _DESCRIPTOR_NAME =
		@"(?<![\p{L}\p{N}_.])(?:fd|descriptor|fileDescriptor)" + _NOT_WORD_AHEAD;

	internal const string EX_GO_120 =
		_NOT_WORD_BEHIND + @"(?<anchor>ReadPassword)" + _NOT_WORD_AHEAD + @"(?<!(?:func|type)[ \t]{1,8}ReadPassword)(?<!\)[ \t]{0,8}ReadPassword)" + _SPACE + @"\((?=[^\n]{0,200}(?:" + _NOT_WORD_BEHIND + @"Stdin" + _NOT_WORD_AHEAD + @"|" + _DESCRIPTOR_NAME + @"))";

	private const string _STDIN_READ_CALL =
		@"(?:NewReader|NewScanner|ReadString)[ \t]{0,3}\(|Fscan";

	private const string _CREDENTIAL_PROMPT =
		@"[\x22`][^\x22`\n]{0,100}(?i:password|passwd|passphrase|pass:)";

	private const string _AROUND_THE_ANCHOR_READ =
		@"(?:(?<=(?:" + _STDIN_READ_CALL + @")[\s\S]{0,300})|(?=[\s\S]{0,300}?(?:" + _STDIN_READ_CALL + @")))";

	private const string _AROUND_THE_ANCHOR_PROMPT =
		@"(?:(?<=" + _CREDENTIAL_PROMPT + @"[\s\S]{0,300})|(?=[\s\S]{0,300}?" + _CREDENTIAL_PROMPT + @"))";

	internal const string EX_GO_121 =
		_NOT_WORD_BEHIND + @"(?<anchor>Stdin)" + _NOT_WORD_AHEAD + _AROUND_THE_ANCHOR_READ + _AROUND_THE_ANCHOR_PROMPT;

	internal const string EX_GO_122 =
		_NOT_WORD_BEHIND + @"(?<anchor>Scanln|Scanf|Scan|(?:Fscanln|Fscan)(?=" + _SPACE + @"\(" + _SPACE + @"[\p{L}\p{N}_.]{0,41}Stdin" + _NOT_WORD_AHEAD + @"))" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _ARGUMENT_WINDOW + @"&" + _SPACE + _CREDENTIAL_NAME + _NOT_WORD_AHEAD;

	private const string _CREDENTIAL_KEY_LITERAL =
		@"[\x22`][^\x22`\n]{0,100}" + _CREDENTIAL_WORD + @"[^\x22`\n]{0,100}[\x22`]";

	internal const string EX_GO_123 =
		@"(?:" + _NOT_WORD_BEHIND + @"(?<anchor>FormValue|PostFormValue)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + _CREDENTIAL_KEY_LITERAL
			+ @"|\.(?<anchor>Get)" + _NOT_WORD_AHEAD + @"(?=" + _SPACE + @"\(" + _SPACE + _CREDENTIAL_KEY_LITERAL + @")(?<=" + _NOT_WORD_BEHIND + @"(?:PostForm|Form)\.Get)"
			+ @"|" + _NOT_WORD_BEHIND + @"(?<anchor>PostForm|Form)" + _SPACE + @"\[" + _SPACE + _CREDENTIAL_KEY_LITERAL + _SPACE + @"\])";

	/// <summary>The markup is in an interpreted or a raw literal; the attribute is in the same tag, quoted (escaped quotes of an interpreted literal included) or bare.</summary>
	internal const string EX_GO_124 =
		@"(?<anchor>(?i:<input))(?=[^>]{0,200}(?<![\p{L}\p{N}_-])(?i:type)[ \t]{0,10}=[ \t]{0,10}\\?[\x27\x22]?password(?![\p{L}\p{N}_-]))";

	private const string _LOGIN_SEGMENT =
		@"(?:[^\x22`\n]{0,100}[/ {])?(?i:login|signin|sign-in|signon|logon)(?=[/ {}\x22`])";

	internal const string EX_GO_125 =
		_NOT_WORD_BEHIND + @"(?<anchor>HandleFunc|Handle)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + @"[\x22`]" + _LOGIN_SEGMENT;

	internal const string EX_GO_130 =
		_NOT_WORD_BEHIND + @"(?<anchor>Getenv|LookupEnv)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + _CREDENTIAL_KEY_LITERAL;

	internal const string EX_GO_131 =
		@"(?:" + _NOT_WORD_BEHIND + @"(?<anchor>(?i:cfg|conf|config|settings))" + _NOT_WORD_AHEAD + @"(?:\.(?:Get|GetString|String)" + _SPACE + @"\(" + _SPACE + _CREDENTIAL_KEY_LITERAL + @"|\.(?:[\p{L}\p{N}_]{1,64}\.)?" + _CREDENTIAL_NAME + _NOT_WORD_AHEAD + @")"
		+ @"|" + _NOT_WORD_BEHIND + @"(?<anchor>" + _CREDENTIAL_NAME + @")" + _NOT_WORD_AHEAD + @"[ \t]{0,8}:=[ \t]{0,8}(?:[\p{L}\p{N}_]{1,64}\.){0,2}(?i:cfg|conf|config|settings)" + _NOT_WORD_AHEAD + @"[ \t]{0,8}\[)";

	internal const string EX_GO_132 =
		_NOT_WORD_BEHIND + @"(?<anchor>GetSecretValue|AccessSecretVersion|GetSecret)" + _NOT_WORD_AHEAD + @"(?<!(?:func|type)[ \t]{1,8}(?:GetSecretValue|AccessSecretVersion|GetSecret))(?<!\)[ \t]{0,8}(?:GetSecretValue|AccessSecretVersion|GetSecret))" + _SPACE + @"\(";

	internal const string EX_GO_133 =
		_NOT_WORD_BEHIND + @"(?:(?:const|var)[ \t]{1,8})?(?<anchor>" + _CREDENTIAL_NAME + @")(?<!(?:Label|Placeholder|Field|Name|Pattern|Regex|Length|Hint|Message|Error|Key|Header|Param|Env|EnvName|EnvVar|URL|Path))" + @"(?:[ \t]{1,8}" + _DECLARED_TYPE + @")?[ \t]{0,8}(?::=|=(?!=)|:(?!=))[ \t]{0,8}(?:\x22[^\x22\n]{1,400}\x22|`[^`\n]{1,400}`)";

	internal const string EX_GO_134 =
		_NOT_WORD_BEHIND + @"(?<anchor>ReadFile)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + @"[\x22`][^\x22`\n]{0,200}(?i:secret|password|credential|token|/run/secrets/)[^\x22`\n]{0,200}[\x22`]";

	#endregion

	[GeneratedRegex(EX_GO_101, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex101();

	[GeneratedRegex(EX_GO_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex001();

	[GeneratedRegex(EX_GO_002, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex002();

	[GeneratedRegex(EX_GO_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex003();

	[GeneratedRegex(EX_GO_004, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex004();

	[GeneratedRegex(EX_GO_005, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex005();

	[GeneratedRegex(EX_GO_006, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex006();

	[GeneratedRegex(EX_GO_007, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex007();

	[GeneratedRegex(EX_GO_008, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex008();

	[GeneratedRegex(EX_GO_009, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex009();

	[GeneratedRegex(EX_GO_010, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex010();

	[GeneratedRegex(EX_GO_011, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex011();

	[GeneratedRegex(EX_GO_012, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex012();

	[GeneratedRegex(EX_GO_013, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex013();

	[GeneratedRegex(EX_GO_102, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex102();

	[GeneratedRegex(EX_GO_110, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex110();

	[GeneratedRegex(EX_GO_111, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex111();

	[GeneratedRegex(EX_GO_120, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex120();

	[GeneratedRegex(EX_GO_121, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex121();

	[GeneratedRegex(EX_GO_122, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex122();

	[GeneratedRegex(EX_GO_123, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex123();

	[GeneratedRegex(EX_GO_124, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex124();

	[GeneratedRegex(EX_GO_125, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex125();

	[GeneratedRegex(EX_GO_130, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex130();

	[GeneratedRegex(EX_GO_131, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex131();

	[GeneratedRegex(EX_GO_132, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex132();

	[GeneratedRegex(EX_GO_133, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex133();

	[GeneratedRegex(EX_GO_134, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex Ex134();
}
