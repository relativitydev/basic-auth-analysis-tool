namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The shared vocabulary of the `Python` pattern classes, as constants the class expressions are built from; the expressions themselves are declared in the parts that follow. Every window is bounded, greedy and followed by
/// its target, and none is a lazy quantifier, because the regular-expression source generator of .NET 10 emits a defective cleanup for a lazy bounded loop (the structural guard counts zero defective sites). A look-behind
/// stands only after a forward guard on a rare literal, never on a frequent match, and every refusal is placed after the positive match where the positive part is cheaper, so it runs only on a success.
///
/// THE WINDOWS. The argument window of one call is at most 1,000 units; a unit is a string literal of one line read as one atomic piece (at most 300 characters, an escape reads two), one other character, a quote that did
/// not close, or a balanced bracket group read whole to a depth of four by unrolling, at most 120 units per level, the three kinds of bracket paired loosely. A group that does not close ends the window, and a bracket
/// that closes the call itself is never a unit, so a window cannot leave its call. The quantifier texts below are compiler-forced (an integer constant cannot be joined into an attribute's pattern) and a test asserts that
/// each equals its integer bound.
/// </summary>
internal static partial class PythonPatternExpressions
{
	/// <summary>The most units one argument window reads.</summary>
	private const int _ARGUMENT_WINDOW_UNITS = 1_000;

	/// <summary>The most units one nested bracket group reads, at every level.</summary>
	private const int _NESTED_GROUP_UNITS = 120;

	/// <summary>The depth of nested bracket groups a window reads; a fifth level ends the window.</summary>
	private const int _NESTING_DEPTH = 4;

	/// <summary>The most characters of one string literal that a unit reads.</summary>
	private const int _STRING_BODY_CHARACTERS = 300;

	/// <summary>The quantifier texts of the three bounds. A source-generated expression needs a constant pattern and an integer constant cannot be joined into one, so each text is written once here and a test asserts that it equals its bound.</summary>
	private const string _STRING_BODY_QUANTIFIER = "{0,300}";
	private const string _NESTED_GROUP_QUANTIFIER = "{0,120}";
	private const string _WINDOW_QUANTIFIER = "{0,1000}";

	private const string _B = @"[ \t]{0,8}";
	private const string _S = @"[ \t\n]{0,40}";
	private const string _PREFIX = @"[rRbBuUfF]{0,2}";

	/// <summary>The placeholder vocabulary the shipped languages filter from a credential literal (empty, mask characters, a bracketed marker word, the closed word list).</summary>
	private const string _PLACEHOLDER =
		@"(?i:[*x.\-_#]{3,}|<[^>\n]{0,100}>|\[[^\]\n]{0,100}\]|\{\{[^}\n]{0,100}\}\}|%[^%\n]{0,100}%|placeholder|changeme|change-me|change_me|replace-me|replaceme|your-password|your_password|redacted|todo|fixme|dummy|not-a-real-secret)";

	private const string _CREDENTIAL_WORD =
		@"(?:(?i:passw(?:or)?d|passphrase|secret|credential|creds?|token)|(?<![A-Za-z0-9])(?i:pass|pw|pwd|apikey)(?![A-Za-z0-9])|(?<=[a-z])Pass(?![a-z0-9]))";

	private const string _NARROW_CREDENTIAL_WORD =
		@"(?i:passw(?:or)?d|pwd|credentials)";

	private const string _LABEL_SUFFIX =
		@"(?i:_label|_placeholder|_field|_name|_pattern|_regex|_length|_hint|_message|_error|_prompt)";

	/// <summary>A string literal of one line, as one atomic unit: at most 300 characters, an escape reads two.</summary>
	private const string _STRING =
		@"(?:\x27(?>[^\x27\\\n]|\\.)" + _STRING_BODY_QUANTIFIER + @"\x27|\x22(?>[^\x22\\\n]|\\.)" + _STRING_BODY_QUANTIFIER + @"\x22)";

	private const string _FLAT = _STRING + @"|[^()\[\]{}\x27\x22]|[\x27\x22]";

	private const string _LEAF = @"(?>" + _FLAT + @")";

	private const string _UNIT_1 = @"(?>" + _FLAT + @"|[(\[{]" + _LEAF + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _UNIT_2 = @"(?>" + _FLAT + @"|[(\[{]" + _UNIT_1 + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _UNIT_3 = @"(?>" + _FLAT + @"|[(\[{]" + _UNIT_2 + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _UNIT_4 = @"(?>" + _FLAT + @"|[(\[{]" + _UNIT_3 + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	/// <summary>The argument window of one call: at most 1,000 units, greedy, always followed by its target.</summary>
	private const string _WINDOW = _UNIT_4 + _WINDOW_QUANTIFIER;

	// The guarded copy of the unit: no character of it may begin the literal `grant_type`, in a string, in a nested group or at the top level.
	private const string _GUARD = @"(?!grant_type)";

	private const string _GUARDED_STRING =
		@"(?:\x27(?>" + _GUARD + @"[^\x27\\\n]|\\.)" + _STRING_BODY_QUANTIFIER + @"\x27|\x22(?>" + _GUARD + @"[^\x22\\\n]|\\.)" + _STRING_BODY_QUANTIFIER + @"\x22)";

	private const string _GUARDED_FLAT = _GUARDED_STRING + @"|" + _GUARD + @"[^()\[\]{}\x27\x22]|[\x27\x22]";

	private const string _GUARDED_LEAF = @"(?>" + _GUARDED_FLAT + @")";

	private const string _GUARDED_UNIT_1 = @"(?>" + _GUARDED_FLAT + @"|[(\[{]" + _GUARDED_LEAF + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_UNIT_2 = @"(?>" + _GUARDED_FLAT + @"|[(\[{]" + _GUARDED_UNIT_1 + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_UNIT_3 = @"(?>" + _GUARDED_FLAT + @"|[(\[{]" + _GUARDED_UNIT_2 + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_UNIT_4 = @"(?>" + _GUARDED_FLAT + @"|[(\[{]" + _GUARDED_UNIT_3 + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_WINDOW = _GUARDED_UNIT_4 + _WINDOW_QUANTIFIER;

	/// <summary>One unit of a tuple element or an argument: as the window's, and a comma at the top level ends it (a comma inside a nested group does not).</summary>
	private const string _ELEMENT_UNIT =
		@"(?>" + _STRING + @"|[^()\[\]{},\x27\x22]|[\x27\x22]|[(\[{]" + _UNIT_1 + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	/// <summary>One element or argument: not blank at its start, atomic, at most 200 units.</summary>
	private const string _ELEMENT =
		@"(?>(?![ \t\n]{0,40}[,)])" + _ELEMENT_UNIT + @"{1,200})";

	/// <summary>The guarded copy of <see cref="_ELEMENT_UNIT"/>: no character of it may begin the literal `grant_type`, in a string, in a nested group or at the top level.</summary>
	private const string _GUARDED_ELEMENT_UNIT =
		@"(?>" + _GUARDED_STRING + @"|" + _GUARD + @"[^()\[\]{},\x27\x22]|[\x27\x22]|[(\[{]" + _GUARDED_UNIT_1 + _NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_ELEMENT =
		@"(?>(?![ \t\n]{0,40}[,)])" + _GUARDED_ELEMENT_UNIT + @"{1,200})";

	private const string _CREDENTIAL_LITERAL =
		@"(?:\x27(?!" + _PLACEHOLDER + @"\x27)(?<credential>[^\x27\\\n{}]{1,200})\x27|\x22(?!" + _PLACEHOLDER + @"\x22)(?<credential>[^\x22\\\n{}]{1,200})\x22)";

	/// <summary>A literal that is the whole argument: a comma or the closing parenthesis follows it.</summary>
	private const string _CREDENTIAL_ARGUMENT =
		_PREFIX + _CREDENTIAL_LITERAL + @"(?=" + _S + @"[,)])";

	/// <summary>A literal that is a whole tuple element: a comma, an optional trailing comma, or the closing parenthesis follows it.</summary>
	private const string _CREDENTIAL_TUPLE_ELEMENT =
		_PREFIX + _CREDENTIAL_LITERAL + @"(?=" + _S + @",?" + _S + @"\))";

	/// <summary>The credential literal of a tuple element, refused when its body holds the literal `grant_type`.</summary>
	private const string _GUARDED_CREDENTIAL_TUPLE_ELEMENT =
		_PREFIX + @"(?:\x27(?!" + _PLACEHOLDER + @"\x27)(?<credential>(?>" + _GUARD + @"[^\x27\\\n{}]){1,200})\x27|\x22(?!" + _PLACEHOLDER + @"\x22)(?<credential>(?>" + _GUARD + @"[^\x22\\\n{}]){1,200})\x22)(?=" + _S + @",?" + _S + @"\))";

	/// <summary>The tuple of two built from guarded units (PY-002): a literal `grant_type` anywhere inside it refuses the whole match.</summary>
	private const string _GUARDED_TUPLE_OF_TWO =
		@"\(" + _S + _GUARDED_ELEMENT + _S + @"," + _S + @"(?:" + _GUARDED_CREDENTIAL_TUPLE_ELEMENT + @"|" + _GUARDED_ELEMENT + @")" + _S + @",?" + _S + @"\)";

	/// <summary>A parenthesized tuple of exactly two elements (a trailing comma allowed); the second element's literal is the credential.</summary>
	private const string _TUPLE_OF_TWO =
		@"\(" + _S + _ELEMENT + _S + @"," + _S + @"(?:" + _CREDENTIAL_TUPLE_ELEMENT + @"|" + _ELEMENT + @")" + _S + @",?" + _S + @"\)";

	/// <summary>The scheme word of a Basic value, then the optional literal token; a leading word other than Basic is not read, and a `t` prefix is not a prefix.</summary>
	private const string _BASIC_LITERAL_VALUE =
		_PREFIX + @"[\x27\x22](?i:basic)(?!\w)(?:[ \t]{1,8}(?!" + _PLACEHOLDER + @"[\x27\x22])(?<credential>[A-Za-z0-9+/=_-]{4,200})(?=[\x27\x22]))?";

	private const string _BASIC_ENCODER_VALUE = @"(?:\w{1,40}\.){0,3}encode_basic_auth\(";

	private const string _BASIC_VALUE = @"(?:" + _BASIC_LITERAL_VALUE + @"|" + _BASIC_ENCODER_VALUE + @")";

	/// <summary>
	/// THE REPLACEMENT FIELD OF AN F-STRING THAT HOLDS QUOTES (detection catalog 1.50.0, `PY-007`). Between the braces, with no brace, backslash, newline, `#`, conversion, format specification or operator inside: a name chain of at most 60
	/// characters followed by at most three trailers, each a subscript whose content is one quoted string or a call whose arguments are quoted strings or name chains, nested to one level (an argument or a subscript content may
	/// itself be a name chain with one trailer of the same two kinds). A quoted string inside a field is at most 40 characters, holds no backslash and no brace, and is of either kind of quote, the literal's own included
	/// (the nested same-quote form of Python 3.12): the field is closed by its brace, so it never reads past it. The field as a whole is at most 100 characters, checked by a look-ahead on the first closing brace. The field is
	/// one atomic unit, so a half of the pair backtracks over whole fields and never into one. Every quantifier is bounded and none is lazy.
	/// </summary>
	private const string _FIELD_NAME = @"\w[\w.]{0,59}";

	private const string _FIELD_QUOTED = @"(?:\x27[^\x27\\{}\n]{0,40}\x27|\x22[^\x22\\{}\n]{0,40}\x22)";

	private const string _FIELD_ATOM = @"(?:" + _FIELD_QUOTED + @"|" + _FIELD_NAME + @")";

	private const string _FIELD_FLAT_TRAILER = @"(?:\[" + _FIELD_QUOTED + @"\]|\(" + _FIELD_ATOM + @"(?:" + _B + @"," + _B + _FIELD_ATOM + @"){0,2}\))";

	private const string _FIELD_NESTED_ATOM = @"(?:" + _FIELD_QUOTED + @"|" + _FIELD_NAME + _FIELD_FLAT_TRAILER + @"?)";

	private const string _FIELD_TRAILER =
		@"(?:\[(?:" + _FIELD_QUOTED + @"|" + _FIELD_NAME + _FIELD_FLAT_TRAILER + @")\]|\(" + _FIELD_NESTED_ATOM + @"(?:" + _B + @"," + _B + _FIELD_NESTED_ATOM + @"){0,2}\))";

	private const string _FIELD = @"(?>\{(?=[^{}\n]{1,98}\})" + _FIELD_NAME + _FIELD_TRAILER + @"{0,3}\})";

	/// <summary>
	/// A character that may stand between the opening brace of a field and a position inside it. It never starts the f-prefixed pair literal of an encoder call (the encoder name as a whole word, its parenthesis, an f prefix and a quote): the literal always follows the opening parenthesis of an encoder, so a brace that
	/// lies before the literal (a mapping that holds the encoder call) always has that start between it and the literal, and never counts as the opening of a field of the literal. A call of an encoder inside a field takes quoted arguments without a prefix, so it is no start.
	/// </summary>
	private const string _FIELD_SCOPE_CHARACTER = @"(?:(?!(?<!\w)(?:standard_b64encode|b64encode|encodebytes)" + _B + @"\(" + _S + @"[rR]?[fF][rR]?[\x27\x22])[^{}\n])";

	/// <summary>
	/// Whether a position stands inside a replacement field: the last brace before it is an opening one, at most 98 characters back (a field is at most 100 characters with its braces and holds no brace inside).
	/// </summary>
	private const string _INSIDE_A_FIELD = @"(?<=\{" + _FIELD_SCOPE_CHARACTER + @"{0,98})";

	private const string _OUTSIDE_EVERY_FIELD = @"(?<!\{" + _FIELD_SCOPE_CHARACTER + @"{0,98})";

	/// <summary>
	/// One CHARACTER of a half of the pair, so that the bound of 100 counts characters and a field counts by its characters: a character of the literal text, the opening brace of a field whose whole grammar follows (checked by
	/// a look-ahead, so the brace is consumed alone), or a character of a field that is already open, its closing brace included. Each step is atomic, so a half backtracks only by giving back whole characters.
	/// </summary>
	private const string _FIELD_OPENING_AND_INTERIOR = @"(?=" + _FIELD + @")\{|" + _INSIDE_A_FIELD + @"[^{}\n]|" + _INSIDE_A_FIELD + @"\}";

	private const string _HALF_UNIT_WITHOUT_COLON_IN_SINGLE_QUOTES = @"(?>[^:\x27\\{}\n]|" + _FIELD_OPENING_AND_INTERIOR + @")";

	private const string _HALF_UNIT_WITHOUT_COLON_IN_DOUBLE_QUOTES = @"(?>[^:\x22\\{}\n]|" + _FIELD_OPENING_AND_INTERIOR + @")";

	private const string _HALF_UNIT_IN_SINGLE_QUOTES = @"(?>[^\x27\\{}\n]|" + _FIELD_OPENING_AND_INTERIOR + @")";

	private const string _HALF_UNIT_IN_DOUBLE_QUOTES = @"(?>[^\x22\\{}\n]|" + _FIELD_OPENING_AND_INTERIOR + @")";

	/// <summary>
	/// The colon-joined pair of an `f`-string whose fields hold quotes (detection catalog 1.50.0, `PY-007`): a single-line literal of one kind of quote, with a prefix of up to two of `r R b B u U f F`, no backslash and no triple quote. The
	/// colon of the pair is the first colon outside every field, a quote occurs inside a replacement field of the literal (a literal whose quotes are all outside its fields keeps its earlier reading), each half is at most 100 characters, the characters of a field counting in the half it stands in, and the closing quote is the first
	/// quote of the literal's own kind outside every field.
	/// </summary>
	private const string _QUOTE_IN_A_FIELD_OF_A_SINGLE_QUOTED_LITERAL = @"(?=(?>[^{\n\x27\\]|\{[^{}\n\x27\x22]{0,98}\}){0,200}\{[^{}\n\x27\x22]{0,98}[\x27\x22])";

	private const string _QUOTE_IN_A_FIELD_OF_A_DOUBLE_QUOTED_LITERAL = @"(?=(?>[^{\n\x22\\]|\{[^{}\n\x27\x22]{0,98}\}){0,200}\{[^{}\n\x27\x22]{0,98}[\x27\x22])";

	private const string _F_STRING_PREFIX = @"(?:[rRbBuU]?[fF]|[fF][rRbBuU])";

	private const string _QUOTE_BEARING_F_STRING_PAIR =
		_F_STRING_PREFIX + @"(?:\x27(?=[^\n]{0,100}:)" + _QUOTE_IN_A_FIELD_OF_A_SINGLE_QUOTED_LITERAL + _HALF_UNIT_WITHOUT_COLON_IN_SINGLE_QUOTES + @"{0,100}" + _OUTSIDE_EVERY_FIELD + @":" + _HALF_UNIT_IN_SINGLE_QUOTES + @"{0,100}" + _OUTSIDE_EVERY_FIELD + @"\x27"
		+ @"|\x22(?=[^\n]{0,100}:)" + _QUOTE_IN_A_FIELD_OF_A_DOUBLE_QUOTED_LITERAL + _HALF_UNIT_WITHOUT_COLON_IN_DOUBLE_QUOTES + @"{0,100}" + _OUTSIDE_EVERY_FIELD + @":" + _HALF_UNIT_IN_DOUBLE_QUOTES + @"{0,100}" + _OUTSIDE_EVERY_FIELD + @"\x22)";

	/// <summary>
	/// The argument text of the `.encode(` call of the held-name shape: as before, a single line of at most 60 characters; or, when the call spans lines (detection catalog 1.50.0), a seam of at most 40 blanks, tabs and line breaks, at most 60
	/// characters that start on a character that is not white space and hold no `#`, and a second seam of at most 40. A comment is not white space, so a `#` is never read in a seam.
	/// </summary>
	private const string _HELD_ENCODE_ARGUMENT = @"(?:[^()\n]{0,60}|(?>[ \t\n]{0,40}(?:[^()\s#](?>[^()\n#]{0,59})(?>[ \t\n]{0,40}))?))";

	/// <summary>
	/// THE COMPOUND HELD SCHEME VALUE OF `PY-013` (detection catalog 1.50.0). A call group is balanced, on one line, of at most 120 units, nested to one level (a unit is one character or a nested group of at most 120 characters); an operand is a name or an attribute chain of at most 64 characters followed
	/// by up to three call groups in all, each group possibly preceded by attribute accesses, and possibly followed by attribute accesses.
	/// </summary>
	private const string _COMPOUND_NESTED_GROUP = @"\([^()\n]{0,120}\)";

	private const string _COMPOUND_GROUP = @"\((?>[^()\n]|" + _COMPOUND_NESTED_GROUP + @"){0,120}\)";

	private const string _COMPOUND_ATTRIBUTES = @"(?:\.\w{1,40}){0,3}";

	private const string _COMPOUND_OPERAND = @"(?>[A-Za-z_][\w.]{0,63})(?:" + _COMPOUND_ATTRIBUTES + _COMPOUND_GROUP + @"){0,3}" + _COMPOUND_ATTRIBUTES;

	/// <summary>What follows the leading literal of a held scheme value, before the end of the statement: nothing (form a and c), a plus or a percent and an operand (forms b and d), or `.format(` and its arguments (form e).</summary>
	private const string _COMPOUND_TAIL =
		@"(?:" + _B + @"[+%]" + _B + _COMPOUND_OPERAND + @"|\.format" + _COMPOUND_GROUP + @")?";

	private const string _HEADER_KEY = @"[\x27\x22](?i:authorization)[\x27\x22]";

	/// <summary>
	/// The carriers of the header class, each ending right before the value. The first: the `headers` name or keyword before a mapping literal, a subscript, or `.update(`; the second: `add_header(`, `add_unredirected_header(`, `putheader(`;
	/// The third: the keyword `Authorization` in `dict(...)`. A quoted key outside a carrier is not here: it is the anchor of `PY-014` alone.
	/// </summary>
	private const string _CARRIERS =
		@"(?:(?<!\w)(?<construct>headers)(?!\w)" + _B + @"(?::[^=\n]{1,80})?=(?!=)" + _S + @"\{" + _WINDOW + _HEADER_KEY + _S + @":" + _S
		+ @"|(?<!\w)(?<construct>headers)(?!\w)" + _B + @"\[" + _S + _HEADER_KEY + _S + @"\]" + _B + @"=(?!=)" + _B
		+ @"|(?<!\w)(?<construct>headers)\.update\(" + _S + @"\{" + _WINDOW + _HEADER_KEY + _S + @":" + _S
		+ @"|(?<!\w)(?<construct>add_header|add_unredirected_header|putheader)\(" + _S + _HEADER_KEY + _S + @"," + _S
		+ @"|(?<!\w)dict\(" + _WINDOW + @"(?<![\w.])(?<construct>(?i:authorization))" + _B + @"=(?!=)" + _B + @")";

	private const string _REQUEST_METHODS = @"get|post|put|patch|delete|head|options|request|stream";

	/// <summary>The proxy refusal of a userinfo URL: the literal is the value of a name, keyword or key that contains `proxy` or `proxies`, directly or as a member of a mapping that holds it.</summary>
	private const string _PROXY_NAMED_PREFIX =
		@"(?<!(?i:prox(?:y|ies))\w{0,40}[\x27\x22]?" + _B + @"[=:]" + _B + @"(?:\{[^{}\n]{0,200})?" + _PREFIX + @")";

	/// <summary>The forward guard on the rare literal: a quote and a scheme. The proxy look-behind stands behind it, so it runs only where a URL literal follows.</summary>
	private const string _USERINFO_URL_FORWARD_GUARD = @"(?=[\x27\x22](?i:https?)://)";

	private const string _USERINFO_URL =
		@"(?i:https?)://[^\s:/@\x27\x22\\{}]{1,64}:(?!" + _PLACEHOLDER + @"@)(?<credential>[^\s/@\x27\x22\\{}]{1,200})@";
}
