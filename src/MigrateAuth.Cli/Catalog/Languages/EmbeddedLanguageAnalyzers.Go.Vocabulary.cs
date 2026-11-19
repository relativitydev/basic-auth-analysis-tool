using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The shared vocabulary of the `Go` expressions: the literal, window and header shapes every class expression is built from. Each window is bounded and atomic, balanced to a depth of
/// four by unrolling, lazy and always required, and never inside an optional group, so no expression can backtrack across a file; the policy match timeout is the final bound. A class's
/// anchor is the capture group <c>construct</c> and its literal credential the group <c>credential</c>, which refuses the placeholder vocabulary of the catalog.
/// </summary>
internal static partial class GoPatternExpressions
{
	/// <summary>
	/// A zero-width point that always matches and gives backtracking exactly one way to fail: one of a word boundary and a non-boundary holds at every position. It is appended inside the body of a
	/// lazy bounded loop over a group, <c>(?:body POINT){m,n}?</c>, because the regular-expression source generator of .NET 10 (generator 10.0.14.15411) emits a defective cleanup of its
	/// backtracking stack for such a loop when the body has no backtracking construct of its own. It changes no match.
	/// </summary>
	private const string _BACKTRACK_POINT = @"(?:\b|\B)";

	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private const RegexOptions _LINE_OPTIONS = _OPTIONS | RegexOptions.Multiline;

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
		@"(?:(?:" + _ARGUMENT_UNIT + @")" + _BACKTRACK_POINT + @"){0,1500}?";

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
}
