namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The shapes of an <c>Authorization</c> header whose value comes from the process environment: the five forms of the environment read, the value that is the read alone, the read with one
/// fallback literal and the template whose only hole is the read, the Basic fallback that the header class of a Basic scheme also reads, and the guard that a file which also writes an explicit
/// bearer value for the header is silent. Every run is bounded, atomic where it can be, and opens at a literal prefix (<c>process</c>, <c>import</c>, <c>Deno</c>, <c>Bun</c>) behind the carrier
/// that the expression already matched, so the cost of a file with no such carrier is nothing and the cost of one with a carrier is its own few hundred characters.
/// </summary>
internal static partial class JavaScriptPatternExpressions
{
	/// <summary>The name of an environment variable written as a property: at most 128 characters, and not followed by a further identifier character.</summary>
	private const string _ENVIRONMENT_NAME =
		@"[A-Za-z_$][\w$]{0,127}(?![\w$])";

	/// <summary>The key of a bracket spelling or of the argument of the Deno read: a quoted literal of at most 128 characters. A template or an identifier key is not read.</summary>
	private const string _ENVIRONMENT_KEY =
		@"(?:\x27[^\x27\\\n]{1,128}\x27|\x22[^\x22\\\n]{1,128}\x22)";

	/// <summary>
	/// The environment read, in one of five forms and only these: <c>process.env.NAME</c>, <c>process.env['NAME']</c>, <c>import.meta.env.NAME</c> (and its bracket spelling), <c>Deno.env.get('NAME')</c> and
	/// <c>Bun.env.NAME</c> (and its bracket spelling). The root name starts at a token boundary: not after an identifier character, a dollar sign or a dot, so an alias of the object, optional chaining and a
	/// read through another name are not this form.
	/// </summary>
	private const string _ENVIRONMENT_READ =
		@"(?:(?<![\w$.])(?:process\.env|import\.meta\.env|Bun\.env)(?:\." + _ENVIRONMENT_NAME + @"|\[" + _ENVIRONMENT_KEY + @"\])|(?<![\w$.])Deno\.env\.get[ \t]{0,8}\([ \t]{0,8}" + _ENVIRONMENT_KEY + @"[ \t]{0,8}\))";

	private const string _FALLBACK_OPERATOR =
		@"[ \t]{0,8}(?:\|\||\?\?)[ \t]{0,8}";

	/// <summary>A literal that begins with the whole word <c>Basic</c> or <c>Bearer</c>, in any letter case, which a fallback literal of the low class may not do.</summary>
	private const string _NOT_A_SCHEME_WORD =
		@"(?!(?i:basic|bearer)(?!\w))";

	/// <summary>
	/// The fallback literal of the low class: a quoted literal or a hole-free template that is WHOLE (the value terminator follows its closing delimiter) and does not begin with the word <c>Basic</c> or
	/// <c>Bearer</c>. It may be empty. One fallback only: a chain of fallbacks, a call and a concatenation are not this shape.
	/// </summary>
	private const string _FALLBACK_LITERAL_WITHOUT_A_SCHEME =
		@"(?:\x27" + _NOT_A_SCHEME_WORD + @"(?>[^\x27\\\n]|\\.){0,200}\x27|\x22" + _NOT_A_SCHEME_WORD + @"(?>[^\x22\\\n]|\\.){0,200}\x22|`" + _NOT_A_SCHEME_WORD + @"(?>[^`$\\\n]|\$(?!\{)|\\.){0,200}`)" + _VALUE_TERMINATOR;

	/// <summary>
	/// The value of the header: the read alone, the read with one fallback literal that is neither a Basic nor a bearer scheme, or a template whose only hole is the read and which holds nothing else. A
	/// fallback that begins with <c>Basic</c> is the header class of a Basic scheme, and one that begins with <c>Bearer</c> is read by nothing.
	/// </summary>
	private const string _ENVIRONMENT_VALUE =
		@"(?:" + _ENVIRONMENT_READ + @"(?:" + _VALUE_TERMINATOR + @"|" + _FALLBACK_OPERATOR + _FALLBACK_LITERAL_WITHOUT_A_SCHEME + @")|`\$\{" + _ENVIRONMENT_READ + @"\}`" + _VALUE_TERMINATOR + @")";

	/// <summary>
	/// The Basic fallback that the header class of a Basic scheme reads: an environment read, one fallback operator, and a literal that begins with the whole word <c>Basic</c> and is whole. The literal is a
	/// Basic header in the code itself, used whenever the variable is absent. The credential is the payload after <c>Basic</c>, a literal credential only when the literal is whole and the payload is not a
	/// placeholder. A forward guard on the rare word that opens the read comes before anything else.
	/// </summary>
	private const string _ENVIRONMENT_READ_WITH_A_BASIC_FALLBACK =
		@"(?=(?:process|import|Deno|Bun)(?![\w$]))" + _ENVIRONMENT_READ + _FALLBACK_OPERATOR + @"(?:\x27(?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"\x27)(?<credential>[A-Za-z0-9+/=_-]{4,})(?=\x27))?(?>[^\x27\\\n]|\\.){0,200}\x27|\x22(?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"\x22)(?<credential>[A-Za-z0-9+/=_-]{4,})(?=\x22))?(?>[^\x22\\\n]|\\.){0,200}\x22|`(?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"`)(?<credential>[A-Za-z0-9+/=_-]{4,})(?=`))?(?>[^`$\\\n]|\$(?!\{)|\\.){0,200}`)" + _VALUE_TERMINATOR;

	/// <summary>
	/// The right-hand side of a declaration that assigns a name from an environment value, for the file symbol table: the read alone, the read with one fallback literal, or a template whose only hole is the
	/// read. It takes part in the match of the constant-declaration expression with the empty group <c>environment</c> and the group <c>fallbackWord</c>, which holds the leading word of the fallback literal
	/// where that word is <c>Basic</c> or <c>Bearer</c> (the two words that decide a verdict) and is empty otherwise, including where there is no fallback. The whole-literal rule is the declaration's own
	/// terminator after the closing delimiter.
	/// </summary>
	private const string _ENVIRONMENT_DECLARATION =
		@"(?=(?:process|import|Deno|Bun)(?![\w$])|`\$\{)(?<environment>)(?:" + _ENVIRONMENT_READ + @"(?:(?<fallbackWord>)|" + _FALLBACK_OPERATOR + @"(?:\x27(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x27\\\n]|\\.){0,200}\x27|\x22(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x22\\\n]|\\.){0,200}\x22|`(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^`$\\\n]|\$(?!\{)|\\.){0,200}`))|`\$\{" + _ENVIRONMENT_READ + @"\}`(?<fallbackWord>))";

	/// <summary>
	/// An explicit bearer value, the forms the guard reads: the header name as a key (<c>Authorization: 'Bearer ...'</c>, quoted or not); a quoted header name then a comma (the first
	/// argument of a header-setting call, <c>.set</c>, <c>.append</c>, the <c>Headers</c> constructor) or a bracket and an equals sign; a dot assignment on <c>headers</c> or <c>headers.common</c>, whatever
	/// stands before that receiver (<c>x.headers.Authorization = ...</c>, <c>x.defaults.headers.common.Authorization = ...</c>); or the bearer fallback of an environment read (<c>|| 'Bearer ...'</c>,
	/// <c>?? 'Bearer ...'</c>). The value is a string or a template whose leading word is the whole word <c>Bearer</c>. A dot or a hyphen before a bare name refuses <c>Proxy-Authorization</c> and a member read, and
	/// a bare variable that is merely named <c>authorization</c> and holds a bearer literal is NOT one: a bearer literal held in a variable first is not read. The guard reads text, not zones, so a header name
	/// and a bearer word spelled together in a comment or in a string read as one. The name, the separator and the value are joined by the carriers' own blanks (<see cref="_SPACE"/>, line feeds included), so a
	/// write split over lines counts as the carriers read it.
	/// </summary>
	private const string _BEARER_WRITE =
		@"(?:(?:(?<![\w$.-])(?i:authorization)" + _SPACE + @":|(?<![\w$.-])[\x27\x22`](?i:authorization)[\x27\x22`](?:" + _SPACE + @"[:,]|" + _SPACE + @"\]" + _SPACE + @"=)|(?<=(?<![\w$])headers(?:\.common)?)\.(?i:authorization)" + _SPACE + @"=)" + _SPACE + @"[\x27\x22`](?i:bearer)(?![\w$])|" + _ENVIRONMENT_READ + _FALLBACK_OPERATOR + @"[\x27\x22`](?i:bearer)(?![\w$]))";

	/// <summary>
	/// THE GUARD, read as a window because the engine's contract has no file-wide guard and this adds none: no finding where an explicit bearer value for the header is written within thirty lines of at most
	/// 400 characters before the match or after it. The look-behind runs from the match backwards through the line it ends on and the thirty lines before it; the look-ahead runs lazily forward through the
	/// thirty lines after it; both only after a carrier and a value that match, which is a handful of places in a file.
	/// </summary>
	private const string _BEARER_GUARD =
		@"(?<!" + _BEARER_WRITE + @"[^\n]{0,400}(?:\n[^\n]{0,400}){0,30})(?!(?:[^\n]{0,400}\n){0,30}?[^\n]{0,400}?" + _BEARER_WRITE + @")";
}
