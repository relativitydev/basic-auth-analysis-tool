using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE DRAFT `Python` EXPRESSIONS OF THE COST GATE RUN BEFORE THE CATALOG WAS FINAL, as test-local constants. Nothing here is read from <c>src/</c>: no `Python` expression exists there before the
/// catalog is final, and no draft file is ever used to close an analyzer set. The fourteen classes `PY-001` to `PY-014` (`EX-PY-001` to `EX-PY-014`), the first-line expression (`EX-PY-100`), the constant declaration (`EX-PY-101`),
/// the two content markers (`EX-PY-110`, `EX-PY-111`) and the twelve flow signals (`EX-PY-120` to `EX-PY-127`, `EX-PY-130` to `EX-PY-133`) are written here, each a source-generated expression carrying the policy timeout of the engine
/// that charges it.
///
/// THE SHAPES THE CATALOG ALLOWS, AND THE DECISIONS THIS DRAFT TAKES WHERE THE CATALOG GIVES NO NUMBER.
///   * The argument window is a loop of at most 1,000 units; a unit is a string literal read as one atomic piece (at most 300 characters, one line), one ordinary character, a lone quote that did not close, or a bracket group
///     balanced to a depth of four by unrolling, at most 120 units per level. The kind of bracket is not paired (a group opens with any of the three and closes with any of the three): the draft needs the span, not the grammar.
///     A group that does not close ends the window (no alternative consumes a lone bracket), so a failed group costs its own length once. A bracket that closes the call itself is never a unit, so the window cannot leave its call.
///   * NO LAZY QUANTIFIER AT ALL (the source-generator defect with lazy quantifiers): every window is a greedy bounded loop of atomic units followed by its target, which reaches the same set of matches as the lazy form.
///   * EVERY QUANTIFIER IS BOUNDED, except the first-line rule, which is the catalog's own text and reads one line.
///   * A look-behind is only opened after a forward guard on a rare literal (`auth=(`, `HTTPBasicAuth(`, `input(` with its prompt words, `://` ...), never on a frequent match (the `.get` case).
///   * The refusal of the literal `grant_type` (PY-002) is built into a second, guarded copy of the unit (a character-level negative look-ahead), because a literal inside a nested group or a string is not visible to a window that
///     steps by whole units. PY-006 has no call to bound, so its refusal is the same line (300 characters either side).
///   * The credential capture is the body of a string literal (a field-free f-string, a raw or a bytes literal included) that is the WHOLE value, never the delimiters; the placeholder vocabulary is the one the shipped languages filter.
///   * The loopback receiver (`EX-PY-123`) cannot express the file-wide conjunction of the catalog (a loopback bind AND a query read of the key `code` in the same file) in one expression; the draft takes it as a span of 1,000
///     characters either side of the bind. This is the one approximation the catalog does not state.
/// </summary>
internal static partial class PythonDraftExpressions
{
	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private const RegexOptions _LINE_OPTIONS = _OPTIONS | RegexOptions.Multiline;

	/// <summary>The most units one argument window reads.</summary>
	internal const int ARGUMENT_WINDOW_UNITS = 1_000;

	/// <summary>The most units one nested bracket group reads, at every level.</summary>
	internal const int NESTED_GROUP_UNITS = 120;

	/// <summary>The depth of nested bracket groups a window reads; a fifth level ends the window.</summary>
	internal const int NESTING_DEPTH = 4;

	/// <summary>The most characters of one string literal that a unit reads.</summary>
	internal const int STRING_BODY_CHARACTERS = 300;

	#region Vocabulary

	/// <summary>The quantifier texts of the three bounds. A source-generated expression needs a constant pattern and an integer constant cannot be joined into one, so each text is written once here and a test asserts that it equals its bound.</summary>
	internal const string STRING_BODY_QUANTIFIER = "{0,300}";
	internal const string NESTED_GROUP_QUANTIFIER = "{0,120}";
	internal const string WINDOW_QUANTIFIER = "{0,1000}";

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
		@"(?:\x27(?>[^\x27\\\n]|\\.)" + STRING_BODY_QUANTIFIER + @"\x27|\x22(?>[^\x22\\\n]|\\.)" + STRING_BODY_QUANTIFIER + @"\x22)";

	private const string _FLAT = _STRING + @"|[^()\[\]{}\x27\x22]|[\x27\x22]";

	private const string _LEAF = @"(?>" + _FLAT + @")";

	private const string _UNIT_1 = @"(?>" + _FLAT + @"|[(\[{]" + _LEAF + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _UNIT_2 = @"(?>" + _FLAT + @"|[(\[{]" + _UNIT_1 + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _UNIT_3 = @"(?>" + _FLAT + @"|[(\[{]" + _UNIT_2 + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _UNIT_4 = @"(?>" + _FLAT + @"|[(\[{]" + _UNIT_3 + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	/// <summary>The argument window of one call: at most 1,000 units, greedy, always followed by its target.</summary>
	private const string _WINDOW = _UNIT_4 + WINDOW_QUANTIFIER;

	// The guarded copy of the unit: no character of it may begin the literal `grant_type`, in a string, in a nested group or at the top level.
	private const string _GUARD = @"(?!grant_type)";

	private const string _GUARDED_STRING =
		@"(?:\x27(?>" + _GUARD + @"[^\x27\\\n]|\\.)" + STRING_BODY_QUANTIFIER + @"\x27|\x22(?>" + _GUARD + @"[^\x22\\\n]|\\.)" + STRING_BODY_QUANTIFIER + @"\x22)";

	private const string _GUARDED_FLAT = _GUARDED_STRING + @"|" + _GUARD + @"[^()\[\]{}\x27\x22]|[\x27\x22]";

	private const string _GUARDED_LEAF = @"(?>" + _GUARDED_FLAT + @")";

	private const string _GUARDED_UNIT_1 = @"(?>" + _GUARDED_FLAT + @"|[(\[{]" + _GUARDED_LEAF + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_UNIT_2 = @"(?>" + _GUARDED_FLAT + @"|[(\[{]" + _GUARDED_UNIT_1 + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_UNIT_3 = @"(?>" + _GUARDED_FLAT + @"|[(\[{]" + _GUARDED_UNIT_2 + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_UNIT_4 = @"(?>" + _GUARDED_FLAT + @"|[(\[{]" + _GUARDED_UNIT_3 + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	private const string _GUARDED_WINDOW = _GUARDED_UNIT_4 + WINDOW_QUANTIFIER;

	/// <summary>One unit of a tuple element or an argument: as the window's, and a comma at the top level ends it (a comma inside a nested group does not).</summary>
	private const string _ELEMENT_UNIT =
		@"(?>" + _STRING + @"|[^()\[\]{},\x27\x22]|[\x27\x22]|[(\[{]" + _UNIT_1 + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

	/// <summary>One element or argument: not blank at its start, atomic, at most 200 units.</summary>
	private const string _ELEMENT =
		@"(?>(?![ \t\n]{0,40}[,)])" + _ELEMENT_UNIT + @"{1,200})";

	/// <summary>The guarded copy of <see cref="_ELEMENT_UNIT"/>: no character of it may begin the literal `grant_type`, in a string, in a nested group or at the top level.</summary>
	private const string _GUARDED_ELEMENT_UNIT =
		@"(?>" + _GUARDED_STRING + @"|" + _GUARD + @"[^()\[\]{},\x27\x22]|[\x27\x22]|[(\[{]" + _GUARDED_UNIT_1 + NESTED_GROUP_QUANTIFIER + @"[)\]}])";

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

	private const string _HEADER_KEY = @"[\x27\x22](?i:authorization)[\x27\x22]";

	/// <summary>
	/// The carriers K1 to K3 of the catalog, each ending right before the value. K1: the `headers` name or keyword before a mapping literal, a subscript, or `.update(`; K2: `add_header(`, `add_unredirected_header(`, `putheader(`;
	/// K3: the keyword `Authorization` in `dict(...)`. K4 (a quoted key outside a carrier) is not here: it is the anchor of `PY-014` alone.
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

	#endregion

	#region The fourteen classes

	// ------------------------------------------------------------------------------------------
	// PY-001: the helper name, whole word, directly followed by an opening parenthesis with at least one argument, whatever qualifies it. The refusal of a value of `proxy_auth` is a bounded look-behind that stands
	// after the forward guard on the rare name.
	// ------------------------------------------------------------------------------------------

	internal const string EX_PY_001 =
		@"(?<!\w)(?=(?:HTTPBasicAuth|BasicAuth)\()(?<!proxy_auth" + _B + @"=" + _B + @"(?:\w{1,40}\.){0,3})(?<construct>HTTPBasicAuth|BasicAuth)\((?!" + _S + @"\))"
			+ @"(?:" + _ELEMENT + @"," + _S + @"(?:(?:password|passwd)" + _B + @"=(?!=)" + _B + @")?" + _CREDENTIAL_ARGUMENT
			+ @"|" + _WINDOW + @"(?<![\w.])(?:password|passwd)" + _B + @"=(?!=)" + _B + _CREDENTIAL_ARGUMENT + @")?";

	// ------------------------------------------------------------------------------------------
	// PY-002: the named-callee half of the tuple-keyword form. The callee is the literal module word and a verb (an alias or a name imported with `from` makes the callee unnamed: PY-006 reads it). The keyword `auth` is
	// a direct keyword of the call (depth one, as the window steps over nested groups whole), followed by a two-element tuple. The literal `grant_type` anywhere in the call refuses it: the guarded window cannot pass it, before
	// the keyword or after the tuple. The anchor group is `auth`, the same code position as PY-006's.
	// ------------------------------------------------------------------------------------------

	internal const string EX_PY_002 =
		@"(?<![\w.])(?:requests\.(?:get|post|put|patch|delete|head|options|request|Request)|httpx\.(?:get|post|put|patch|delete|head|options|request|stream|Client|AsyncClient))(?!\w)" + _B + @"\("
			+ _GUARDED_WINDOW + @"(?<![\w.])(?<auth>auth)" + _B + @"=(?!=)" + _B + _GUARDED_TUPLE_OF_TWO + @"(?=" + _GUARDED_WINDOW + @"\))";

	// ------------------------------------------------------------------------------------------
	// PY-003: a carrier, the header name `Authorization` (any case, whole word: a proxy or an X- name does not match) and a value whose leading literal is `Basic` or a call of `encode_basic_auth(`. The match extends into the
	// string zones to read the name and the value; the anchor is the carrier token.
	// ------------------------------------------------------------------------------------------

	internal const string EX_PY_003 = _CARRIERS + _BASIC_VALUE;

	internal const string EX_PY_004 =
		@"(?<!\w)(?<construct>HTTPBasicAuthHandler)\(";

	internal const string EX_PY_005 =
		@"(?<!\w)(?<construct>make_headers)" + _B + @"\(" + _WINDOW + @"(?<![\w.])basic_auth" + _B + @"=(?!=)"
			+ @"(?:" + _B + _PREFIX + @"(?:\x27[^:{}\x27\\\n]{1,100}:(?!" + _PLACEHOLDER + @"\x27)(?<credential>[^{}\x27\\\n]{1,200})\x27|\x22[^:{}\x22\\\n]{1,100}:(?!" + _PLACEHOLDER + @"\x22)(?<credential>[^{}\x22\\\n]{1,200})\x22))?";

	// ------------------------------------------------------------------------------------------
	// PY-006: the unnamed half. The word `auth` as a keyword, an attribute after a dot or a simple name, `=` and a two-element tuple; no receiver is examined (that is the cap). The refusal of `grant_type` is the same line, 300 characters either side of the keyword: the forward guard stands right after the keyword, so it covers the gap, the tuple and what follows it.
	// ------------------------------------------------------------------------------------------

	internal const string EX_PY_006 =
		@"(?<!\w)(?=auth" + _B + @"=(?!=)" + _B + @"\()(?<!grant_type[^\n]{0,300})(?<auth>auth)(?![^\n]{0,300}grant_type)" + _B + @"=(?!=)" + _B + _TUPLE_OF_TWO;

	// ------------------------------------------------------------------------------------------
	// PY-007: the encoder callee (the URL-safe and the other alphabets are different whole words) whose first argument is a colon-joined pair, then the use requirement: `Basic` or `Authorization` within 300 characters of
	// the encoder in either direction. The pair forms: a literal pair (the credential is the part after the colon), a literal with fields, a concatenation, a `%` format, `.format(` and `":".join(`.
	// ------------------------------------------------------------------------------------------

	private const string _COLON_JOINED_PAIR =
		@"(?:" + @"[rRbBuU]{0,2}(?:\x27[^:{}\x27\\\n]{1,100}:(?!" + _PLACEHOLDER + @"\x27)(?<credential>[^{}\x27\\\n]{1,200})\x27|\x22[^:{}\x22\\\n]{1,100}:(?!" + _PLACEHOLDER + @"\x22)(?<credential>[^{}\x22\\\n]{1,200})\x22)"
		+ @"|" + _PREFIX + @"[\x27\x22][^\x27\x22\\\n]{0,100}:[^\x27\x22\\\n]{0,100}[\x27\x22]"
		+ @"|\w[\w.]{0,60}" + _B + @"\+" + _B + @"[\x27\x22]:[\x27\x22]" + _B + @"\+"
		+ @"|[\x27\x22]%s:%s[\x27\x22]" + _B + @"%"
		+ @"|[\x27\x22]\{[^{}\n]{0,20}\}:\{[^{}\n]{0,20}\}[\x27\x22]\.format\("
		+ @"|[\x27\x22]:[\x27\x22]\.join\()";

	private const string _USE_WORD = @"(?i:basic|authorization)";

	/// <summary>The use requirement of the held-name shape (0.6): the word `Basic` alone; `Authorization` is not enough for a name whose content is unknown.</summary>
	private const string _HELD_USE_WORD = @"(?i:basic)";

	/// <summary>
	/// The held-name shape of the catalog (0.6): the first argument is a bare identifier or an attribute chain, or a bare name followed by `.encode(...)`, or a call of a bare `encode(name)`; the colon-joined construction lies elsewhere.
	/// A literal, a concatenation or a call with other arguments is not this shape (the argument must end at a comma or the closing parenthesis). There is no credential position.
	/// </summary>
	private const string _HELD_NAME =
		@"(?:(?:\w{1,40}\.){0,3}encode\(" + _B + @"\w[\w.]{0,60}" + _B + @"\)|\w[\w.]{0,60}(?:\.encode\([^()\n]{0,60}\))?)(?=" + _B + @"[,)])";

	internal const string EX_PY_007 =
		@"(?<!\w)(?<construct>standard_b64encode|b64encode|encodebytes)" + _B + @"\(" + _S
			+ @"(?:" + _COLON_JOINED_PAIR + @"(?:(?<=" + _USE_WORD + @"[\s\S]{0,300})|(?=[\s\S]{0,300}" + _USE_WORD + @"))"
			+ @"|" + _HELD_NAME + @"(?:(?<=" + _HELD_USE_WORD + @"[\s\S]{0,300})|(?=[\s\S]{0,300}" + _HELD_USE_WORD + @")))";

	internal const string EX_PY_008 =
		@"(?<!\w)(?<construct>encode_basic_auth)\((?!" + _S + @"\))"
			+ @"(?:" + _ELEMENT + @"," + _S + @"(?:password" + _B + @"=(?!=)" + _B + @")?" + _CREDENTIAL_ARGUMENT + @")?";

	internal const string EX_PY_009 =
		@"\.(?<construct>add_password)\((?:" + _S + _ELEMENT + @",){3}" + _S + @"(?:" + _CREDENTIAL_ARGUMENT + @"|" + _ELEMENT + @")";

	internal const string EX_PY_010 =
		@"(?<!\w)(?<construct>NetRCAuth)\(";

	// ------------------------------------------------------------------------------------------
	// PY-011: the callee token of a request-making call (the module-qualified forms and the method names), whose argument window holds a userinfo URL literal of the scheme `http` or `https`. A literal that is the value of
	// a name, keyword or key that contains `proxy` or `proxies` is refused by a look-behind that stands after the forward walk to the literal (rare: only a window with a URL in it gets here).
	// PY-012: the declaration name or keyword that holds such a literal, with no call in between.
	// ------------------------------------------------------------------------------------------

	internal const string EX_PY_011 =
		@"(?<!\w)(?<construct>" + _REQUEST_METHODS + @"|urlopen|Request)(?!\w)" + _B + @"\(" + _WINDOW + _PREFIX + _USERINFO_URL_FORWARD_GUARD + _PROXY_NAMED_PREFIX + @"[\x27\x22]" + _USERINFO_URL;

	internal const string EX_PY_012 =
		@"(?<![\w.])(?<construct>(?>[A-Za-z_]\w{0,63}))" + _B + @"(?::=|=(?!=))" + _B + _PREFIX
			+ _USERINFO_URL_FORWARD_GUARD + @"(?<!(?i:prox(?:y|ies))\w{0,40}" + _B + @"(?::=|=)" + _B + _PREFIX + @")[\x27\x22]" + _USERINFO_URL;

	internal const string EX_PY_013 =
		_CARRIERS + @"(?<scheme>(?>[A-Za-z_]\w{0,63}))(?=" + _B + @"(?:[,)}\]#\n]|\z)|" + _S + @"[,)}\]])";

	internal const string EX_PY_014 =
		@"[\x27\x22](?<construct>(?i:authorization))[\x27\x22]" + _B + @":" + _B + _BASIC_LITERAL_VALUE;

	#endregion

	#region The first-line rule, the constant declaration and the two content markers

	/// <summary>The first-line detection expression, as the catalog states it: the `python -S` refusal is the negative look-ahead; `env -S python3` stays accepted. Charged to the lexical budget.</summary>
	internal const string EX_PY_100 =
		@"^#![ \t]*(?:[^\s/]*/)*(?:env[ \t]+(?:-\S*[ \t]+)*)?python(?:2|3(?:\.[0-9]+)?)?(?![ \t]+-S(?:[ \t]|$))(?:[ \t]|$)";

	private const string _ENVIRONMENT_KEY = @"(?:\x27[^\x27\\\n]{1,200}\x27|\x22[^\x22\\\n]{1,200}\x22)";

	/// <summary>
	/// The constant declaration that the file symbol table counts: `NAME = literal` and `NAME: annotation = literal` at a line start (the annotation is greedy and cannot cross an equals sign or a line end, so no lazy loop
	/// stands in an optional group), a single-line single- or double-quoted literal, or an `os.environ` or `os.getenv` read marked by the empty group `environment` with the leading word of its fallback in `fallbackWord`.
	/// The environment alternative opens behind a forward guard on the rare literal `os.`.
	/// </summary>
	internal const string EX_PY_101 =
		@"^[ \t]{0,40}(?<name>[A-Za-z_][A-Za-z0-9_]{0,127})[ \t]{0,8}(?::[^=\n]{1,200})?=(?!=)[ \t]{0,8}(?:[rRuU]?\x27(?<value>(?>[^\x27\\\n]|\\.){0,400})\x27|[rRuU]?\x22(?<value>(?>[^\x22\\\n]|\\.){0,400})\x22"
			+ @"|(?=os\.)(?<environment>)os\.(?:environ\[[ \t]{0,8}" + _ENVIRONMENT_KEY + @"[ \t]{0,8}\](?<fallbackWord>)"
			+ @"|(?:environ\.get|getenv)\([ \t]{0,8}" + _ENVIRONMENT_KEY + @"[ \t]{0,8}(?:,[ \t]{0,8}(?:\x27(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x27\\\n]|\\.){0,200}\x27|\x22(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x22\\\n]|\\.){0,200}\x22)[ \t]{0,8}|(?<fallbackWord>))\)))"
			+ @"(?=[ \t]{0,8}(?:;|$|#))";

	/// <summary>`EX-PY-110`: a class statement whose base is `TestCase` (`unittest.TestCase`, `TestCase`, `IsolatedAsyncioTestCase`). Kind Test, zone code only.</summary>
	internal const string EX_PY_110 =
		@"^[ \t]{0,40}class[ \t]{1,8}\w{1,64}[ \t]{0,8}\([^)\n]{0,200}(?<![\w])(?:IsolatedAsyncioTestCase|TestCase)(?!\w)";

	/// <summary>
	/// `EX-PY-111`: the generated-file banner. (a) Within the first three lines, a comment that holds `auto-generated`, `autogenerated`, `auto generated`, `@generated` or `generated by`; (b) within the first ten lines, a comment that
	/// holds `do not edit`, `do not modify`, `do not hand-edit`, `changes will be lost` or `changes will be overwritten`. Anchored at the start of the text; every line before the comment is at most 400 characters.
	/// </summary>
	internal const string EX_PY_111 =
		@"(?:\A(?:[^\n]{0,400}\n){0,2}[ \t]{0,40}#[^\n]{0,300}(?i:auto-generated|autogenerated|auto generated|@generated|generated by)"
			+ @"|\A(?:[^\n]{0,400}\n){0,9}[ \t]{0,40}#[^\n]{0,300}(?i:do not edit|do not modify|do not hand-edit|changes will be lost|changes will be overwritten))";

	#endregion

	#region The twelve flow signals (the group that anchors a signal is `anchor`, as the engine reads it)

	/// <summary>The refusal of a local declaration of `input` in the thirty lines before the call: a function of that name, an assignment to it, or a parameter of the enclosing function.</summary>
	private const string _LOCAL_INPUT_DECLARATION =
		@"(?<!(?:def[ \t]{1,8}input(?!\w)|(?<![\w.])input[ \t]{0,8}=(?!=)|def[ \t]{1,8}\w{1,64}\([^)\n]{0,200}(?<![\w.])input(?!\w))(?:[^\n]{0,400}\n){0,30}[^\n]{0,400})";

	internal const string EX_PY_120 =
		@"(?<!\w)(?<anchor>getpass)\(";

	internal const string EX_PY_121 =
		@"(?<![\w.])(?=input\(" + _S + _PREFIX + @"[\x27\x22][^\x27\x22\n]{0,100}" + _NARROW_CREDENTIAL_WORD + @")" + _LOCAL_INPUT_DECLARATION + @"(?<anchor>input)\(";

	internal const string EX_PY_122 =
		@"(?<![\w.])webbrowser\.(?<anchor>open_new_tab|open_new|open)(?!\w)" + _B + @"\(";

	private const string _QUERY_CODE_READ = @"parse_qsl?\([\s\S]{0,300}[\x27\x22]code[\x27\x22]";

	internal const string EX_PY_123 =
		@"(?<!\w)(?<anchor>ThreadingHTTPServer|HTTPServer|TCPServer)(?!\w)" + _B + @"\([^\n]{0,200}[\x27\x22](?:127\.0\.0\.1|localhost|::1)[\x27\x22]"
			+ @"(?:(?=[\s\S]{0,1000}" + _QUERY_CODE_READ + @")|(?<=" + _QUERY_CODE_READ + @"[\s\S]{0,1000}))";

	internal const string EX_PY_124 =
		@"(?<![\w.])request\.(?<anchor>form|POST)(?!\w)(?:\[" + _S + @"|\.get\(" + _S + @")[\x27\x22][^\x27\x22\n]{0,100}" + _NARROW_CREDENTIAL_WORD + @"(?!\w{0,30}" + _LABEL_SUFFIX + @")";

	internal const string EX_PY_125 =
		@"(?:(?<![\w.])(?<anchor>OAuth2PasswordRequestForm)(?!\w)|(?<![\w])(?<anchor>Form)\((?<=" + _NARROW_CREDENTIAL_WORD + @"\w{0,30}" + _B + @":[^\n]{0,150}Form\())";

	internal const string EX_PY_126 =
		@"(?:(?<!\w)(?<anchor>authenticate)\(" + _WINDOW + @"(?<![\w.])password" + _B + @"=(?!=)|(?<!\w)(?<anchor>login)\(" + _B + @"request(?!\w))";

	internal const string EX_PY_127 =
		@"(?:(?<![\w.])request\.(?<anchor>session)\[|^[ \t]{0,40}from[ \t]{1,8}flask[ \t]{1,8}import[^\n]{0,200}(?<![\w.])(?<anchor>session)(?!\w))";

	private const string _CREDENTIAL_KEY_LITERAL = @"[\x27\x22][^\x27\x22\n]{0,100}" + _CREDENTIAL_WORD;

	internal const string EX_PY_130 =
		@"(?<![\w.])os\.(?<anchor>environ|getenv)(?!\w)(?:\[" + _S + @"|\.get\(" + _S + @"|\(" + _S + @")" + _CREDENTIAL_KEY_LITERAL;

	private const string _CONFIGURATION_RECEIVER = @"(?<anchor>(?i:config|conf|settings|cfg|parser))(?!\w)";

	/// <summary>The attribute form (0.6): a member whose name holds a credential word, read as an attribute of the receiver (a call of the member is not an attribute read).</summary>
	private const string _CREDENTIAL_NAMED_MEMBER = @"\.(?=\w{0,64}" + _CREDENTIAL_WORD + @")(?>\w{1,64})(?![\w(])";

	internal const string EX_PY_131 =
		@"(?<!\w)" + _CONFIGURATION_RECEIVER + @"(?:(?:\[|\.get\()" + _S + @"(?:[\x27\x22][^\x27\x22\n]{0,100}[\x27\x22]" + _B + @"(?:\]\[|,)" + _S + @")?" + _CREDENTIAL_KEY_LITERAL + @"|" + _CREDENTIAL_NAMED_MEMBER + @")";

	internal const string EX_PY_132 =
		@"\.(?<anchor>authenticators)\(";

	internal const string EX_PY_133 =
		@"(?<![\w.])(?=(?>\w{1,64})" + _B + @"(?::[^=\n]{1,100})?=(?!=))(?=\w{0,64}" + _CREDENTIAL_WORD + @")(?<anchor>(?>\w{1,64}))(?<!" + _LABEL_SUFFIX + @")" + _B + @"(?::[^=\n]{1,100})?=(?!=)" + _B
			+ @"(?:[rRbBuU]{0,2}(?:\x27(?!" + _PLACEHOLDER + @"\x27)[^\x27\\\n]{1,400}\x27|\x22(?!" + _PLACEHOLDER + @"\x22)[^\x22\\\n]{1,400}\x22)|(?:[fF][rR]?|[rR][fF])(?:\x27(?!" + _PLACEHOLDER + @"\x27)[^\x27\\\n{}]{1,400}\x27|\x22(?!" + _PLACEHOLDER + @"\x22)[^\x22\\\n{}]{1,400}\x22))";

	#endregion

	[GeneratedRegex(EX_PY_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy001();

	[GeneratedRegex(EX_PY_002, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy002();

	[GeneratedRegex(EX_PY_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy003();

	[GeneratedRegex(EX_PY_004, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy004();

	[GeneratedRegex(EX_PY_005, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy005();

	[GeneratedRegex(EX_PY_006, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy006();

	[GeneratedRegex(EX_PY_007, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy007();

	[GeneratedRegex(EX_PY_008, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy008();

	[GeneratedRegex(EX_PY_009, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy009();

	[GeneratedRegex(EX_PY_010, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy010();

	[GeneratedRegex(EX_PY_011, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy011();

	[GeneratedRegex(EX_PY_012, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy012();

	[GeneratedRegex(EX_PY_013, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy013();

	[GeneratedRegex(EX_PY_014, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy014();

	[GeneratedRegex(EX_PY_100, _OPTIONS, matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	internal static partial Regex ExPy100();

	[GeneratedRegex(EX_PY_101, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy101();

	[GeneratedRegex(EX_PY_110, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy110();

	[GeneratedRegex(EX_PY_111, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy111();

	[GeneratedRegex(EX_PY_120, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy120();

	[GeneratedRegex(EX_PY_121, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy121();

	[GeneratedRegex(EX_PY_122, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy122();

	[GeneratedRegex(EX_PY_123, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy123();

	[GeneratedRegex(EX_PY_124, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy124();

	[GeneratedRegex(EX_PY_125, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy125();

	[GeneratedRegex(EX_PY_126, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy126();

	[GeneratedRegex(EX_PY_127, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy127();

	[GeneratedRegex(EX_PY_130, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy130();

	[GeneratedRegex(EX_PY_131, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy131();

	[GeneratedRegex(EX_PY_132, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy132();

	[GeneratedRegex(EX_PY_133, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ExPy133();
}

/// <summary>
/// THE PUBLIC FACE OF THE DRAFT `Python` EXPRESSIONS: the lists the tests and the cost gate read. <see cref="MatchBoundExpressions"/> is the whole set one `Python` file is charged by the conservative-charge rule (the constant
/// declaration, the fourteen class expressions, the two content markers and the twelve flow signals, every one in full whatever the allow-list would skip); the first-line rule is charged to the lexical budget and is not in it.
/// </summary>
internal static class PythonDraftExpressionSet
{
	/// <summary>The names of the capture groups that anchor a match, as the tests read them.</summary>
	internal const string ANCHOR_GROUP = "anchor";
	internal const string CONSTRUCT_GROUP = "construct";
	internal const string AUTH_GROUP = "auth";
	internal const string CREDENTIAL_GROUP = "credential";

	internal const string FIRST_LINE_IDENTIFIER = "EX-PY-100";
	internal const string CONSTANT_DECLARATION_IDENTIFIER = "EX-PY-101";
	internal const string TEST_CASE_MARKER_IDENTIFIER = "EX-PY-110";
	internal const string GENERATED_BANNER_MARKER_IDENTIFIER = "EX-PY-111";
	internal const string NAMED_TUPLE_IDENTIFIER = "EX-PY-002";
	internal const string UNNAMED_TUPLE_IDENTIFIER = "EX-PY-006";

	/// <summary>The fourteen class expressions, `PY-001` to `PY-014`.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> ClassExpressions { get; } =
	[
		("EX-PY-001", PythonDraftExpressions.ExPy001()),
		("EX-PY-002", PythonDraftExpressions.ExPy002()),
		("EX-PY-003", PythonDraftExpressions.ExPy003()),
		("EX-PY-004", PythonDraftExpressions.ExPy004()),
		("EX-PY-005", PythonDraftExpressions.ExPy005()),
		("EX-PY-006", PythonDraftExpressions.ExPy006()),
		("EX-PY-007", PythonDraftExpressions.ExPy007()),
		("EX-PY-008", PythonDraftExpressions.ExPy008()),
		("EX-PY-009", PythonDraftExpressions.ExPy009()),
		("EX-PY-010", PythonDraftExpressions.ExPy010()),
		("EX-PY-011", PythonDraftExpressions.ExPy011()),
		("EX-PY-012", PythonDraftExpressions.ExPy012()),
		("EX-PY-013", PythonDraftExpressions.ExPy013()),
		("EX-PY-014", PythonDraftExpressions.ExPy014())
	];

	/// <summary>The two content markers of output 4: the unit-test class (kind Test) and the generated-file banner (kind Generated).</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> ContentMarkerExpressions { get; } =
	[
		(TEST_CASE_MARKER_IDENTIFIER, PythonDraftExpressions.ExPy110()),
		(GENERATED_BANNER_MARKER_IDENTIFIER, PythonDraftExpressions.ExPy111())
	];

	/// <summary>The twelve flow signals: eight interactive (four `app:cli`, four `app:web-server`) and four fixed-credential.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> FlowSignalExpressions { get; } =
	[
		("EX-PY-120", PythonDraftExpressions.ExPy120()),
		("EX-PY-121", PythonDraftExpressions.ExPy121()),
		("EX-PY-122", PythonDraftExpressions.ExPy122()),
		("EX-PY-123", PythonDraftExpressions.ExPy123()),
		("EX-PY-124", PythonDraftExpressions.ExPy124()),
		("EX-PY-125", PythonDraftExpressions.ExPy125()),
		("EX-PY-126", PythonDraftExpressions.ExPy126()),
		("EX-PY-127", PythonDraftExpressions.ExPy127()),
		("EX-PY-130", PythonDraftExpressions.ExPy130()),
		("EX-PY-131", PythonDraftExpressions.ExPy131()),
		("EX-PY-132", PythonDraftExpressions.ExPy132()),
		("EX-PY-133", PythonDraftExpressions.ExPy133())
	];

	internal static Regex ConstantDeclaration { get; } = PythonDraftExpressions.ExPy101();

	internal static Regex FirstLine { get; } = PythonDraftExpressions.ExPy100();

	/// <summary>The expressions charged to the lexical budget: the first-line detection.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> LexicalExpressions { get; } =
	[
		(FIRST_LINE_IDENTIFIER, PythonDraftExpressions.ExPy100())
	];

	/// <summary>The expressions charged to the recognition engine's match bound for one `Python` file.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> MatchBoundExpressions { get; } =
	[
		(CONSTANT_DECLARATION_IDENTIFIER, PythonDraftExpressions.ExPy101()),
		.. ClassExpressions,
		.. ContentMarkerExpressions,
		.. FlowSignalExpressions
	];

	/// <summary>Every draft expression, the lexical one included.</summary>
	internal static ImmutableArray<(string Identifier, Regex Expression)> AllExpressions { get; } =
	[
		.. LexicalExpressions,
		.. MatchBoundExpressions
	];

	/// <summary>The subsumption relation the catalog declares (sections 3.6.1 and 3.6.2): the triple at identical `auth` anchors, and the five overlap triples.</summary>
	internal static ImmutableArray<(string Subsuming, string Subsumed, SubsumptionCondition When)> SubsumptionTriples { get; } =
	[
		("PY-002", "PY-006", SubsumptionCondition.AnchorPositionsAreIdentical),
		("PY-003", "PY-007", SubsumptionCondition.MatchSpansOverlap),
		("PY-003", "PY-008", SubsumptionCondition.MatchSpansOverlap),
		("PY-003", "PY-014", SubsumptionCondition.MatchSpansOverlap),
		("PY-007", "PY-014", SubsumptionCondition.MatchSpansOverlap),
		("PY-011", "PY-012", SubsumptionCondition.MatchSpansOverlap)
	];
}
