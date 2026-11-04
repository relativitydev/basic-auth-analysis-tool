using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The shared vocabulary of the `JavaScript` expressions: the literal, window and header shapes every class expression is built from. Each window is
/// bounded and atomic, balanced to a depth of four by unrolling, lazy and always required, and never inside an optional group, so no expression can
/// backtrack across a file; the policy match timeout is the final bound. A flow signal's anchor is the group <c>signal</c>, a class's the group
/// <c>construct</c> and its literal credential the group <c>credential</c>, which refuses the catalog's placeholder vocabulary.
/// </summary>
internal static partial class JavaScriptPatternExpressions
{
	/// <summary>
	/// A zero-width point that always matches and gives backtracking exactly one way to fail: one of a word boundary and a non-boundary holds at every position.
	/// It is appended inside the body of a lazy bounded loop over a group, <c>(?:body POINT){m,n}?</c>, because the regular-expression source generator of .NET 10
	/// (generator 10.0.14.15411) emits a defective cleanup of its backtracking stack for such a loop when the body has no backtracking construct of its own
	/// (fixed upstream in dotnet/runtime PR 129628, not backported to release/10.0). It changes no match.
	/// </summary>
	private const string _BACKTRACK_POINT = @"(?:\b|\B)";

	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private const RegexOptions _LINE_OPTIONS = _OPTIONS | RegexOptions.Multiline;

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
		@"(?:(?:" + _ARGUMENT_UNIT + @")" + _BACKTRACK_POINT + @"){0,1500}?";

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
		@"(?:(?:" + _CHAIN_LINK + @")" + _BACKTRACK_POINT + @"){0,20}?";

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
		@"(?:(?=[\x27\x22`]https?://[^\s:/@\x27\x22`$\\]{1,64}:(?!" + _PLACEHOLDER_BODY + @"@)(?<credential>[^\s/@\x27\x22`$\{\}\\]{1,200})@))?";

	private const string _USERINFO_URL =
		_USERINFO_URL_CREDENTIAL + @"[\x27\x22`]https?://[^\s:/@\x27\x22`$\\]{1,64}:[^\s/@\x27\x22`$\{\}\\]{1,200}@";

	private const string _USE_WORDS =
		@"(?:(?i:basic)(?<![A-Za-z0-9_$](?i:basic))(?![A-Za-z0-9_$])|(?i:authorization))";

	private const string _USE_REQUIREMENT =
		@"(?:(?<=" + _USE_WORDS + @"[\s\S]{0,300})|(?=[\s\S]{0,300}?" + _USE_WORDS + @"))";

	private const string _DEFINITION_WINDOW =
		@"(?:(?:[^\n]{0,400}\n)" + _BACKTRACK_POINT + @"){0,30}?[^\n]{0,400}?";
}
