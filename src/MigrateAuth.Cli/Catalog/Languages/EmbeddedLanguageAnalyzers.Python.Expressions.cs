using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The expressions of the `Python` analyzer, as source-generated factories: the constant declaration of the file symbol table (<c>EX-PY-101</c>), the fourteen pattern classes <c>PY-001</c> to <c>PY-014</c>
/// (<c>EX-PY-001</c> to <c>EX-PY-014</c>), the two content markers of the demotion signals (<c>EX-PY-110</c>, <c>EX-PY-111</c>), the file-name generation marker, and the twelve flow-signal expressions
/// (<c>EX-PY-120</c> to <c>EX-PY-133</c>) that the flow signals of a later part declare. The first-line rule stays with the identification, in <c>EmbeddedLanguageAnalyzers.Python.cs</c>. Every factory carries the
/// recognition engine's policy match timeout, <c>CultureInvariant</c> and <c>ExplicitCapture</c>, and none is case-insensitive as a whole except the generated-stub file-name expression, which reads file names without regard to case; the words of the other expressions that are matched without regard to case say so in their own group.
///
/// THE SHAPES THE CATALOG ALLOWS, AND THE DECISIONS THE CATALOG GIVES NO NUMBER FOR.
///   * The argument window is a loop of at most 1,000 units; a unit is a string literal read as one atomic piece (at most 300 characters, one line), one ordinary character, a lone quote that did not close, or a bracket group
///     balanced to a depth of four by unrolling, at most 120 units per level. The kind of bracket is not paired: the window needs the span, not the grammar. A group that does not close ends the window (no alternative consumes
///     a lone bracket), so a failed group costs its own length once, and a bracket that closes the call itself is never a unit, so the window cannot leave its call. Its width is therefore capped by a span of characters
///     and never balanced across lines without a cap.
///   * NO LAZY QUANTIFIER AT ALL (the source-generator defect with lazy quantifiers): every window is a greedy bounded loop of atomic units followed by its target, which reaches the same set of matches as the lazy form.
///   * EVERY QUANTIFIER IS BOUNDED. A look-behind is opened only after a forward guard on a rare literal (<c>auth=(</c>, <c>HTTPBasicAuth(</c>, <c>input(</c> with its prompt words, <c>://</c>), never on a frequent match.
///   * The refusal of the literal <c>grant_type</c> (PY-002) is built into a second, guarded copy of the unit (a character-level negative look-ahead), because a literal inside a nested group or a string is not visible to a
///     window that steps by whole units. PY-006 has no call to bound, so its refusal is the same line, 300 characters either side.
///   * The credential capture is the body of a single-line string literal that is the WHOLE value, never the delimiters; the placeholder vocabulary is the one the shipped languages filter.
///   * The loopback receiver (<c>EX-PY-123</c>) cannot express the file-wide conjunction of the catalog (a loopback bind and a query read of the key <c>code</c> in one file) in one expression; it is read as a span of 1,000
///     characters either side of the bind.
/// </summary>
internal static partial class PythonPatternExpressions
{
	private const RegexOptions _OPTIONS = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
	private const RegexOptions _LINE_OPTIONS = _OPTIONS | RegexOptions.Multiline;

	/// <summary>The name of a generated protocol-stub module: the file ends in <c>_pb2.py</c> or <c>_pb2_grpc.py</c>. Evaluated over a file name and the folder segments of its path, without regard to case.</summary>
	private const string _GENERATED_STUB_FILE_NAME = @"_pb2(?:_grpc)?\.py\z";

	// ------------------------------------------------------------------------------------------
	// PY-001: the helper name, whole word, directly followed by an opening parenthesis with at least one argument, whatever qualifies it. The refusal of a value of `proxy_auth` is a bounded look-behind that stands
	// after the forward guard on the rare name.
	// ------------------------------------------------------------------------------------------

	private const string _EX_PY_001 =
		@"(?<!\w)(?=(?:HTTPBasicAuth|BasicAuth)\()(?<!proxy_auth" + _B + @"=" + _B + @"(?:\w{1,40}\.){0,3})(?<construct>HTTPBasicAuth|BasicAuth)\((?!" + _S + @"\))"
			+ @"(?:" + _ELEMENT + @"," + _S + @"(?:(?:password|passwd)" + _B + @"=(?!=)" + _B + @")?" + _CREDENTIAL_ARGUMENT
			+ @"|" + _WINDOW + @"(?<![\w.])(?:password|passwd)" + _B + @"=(?!=)" + _B + _CREDENTIAL_ARGUMENT + @")?";

	// ------------------------------------------------------------------------------------------
	// PY-002: the named-callee half of the tuple-keyword form. The callee is the literal module word and a verb (an alias or a name imported with `from` makes the callee unnamed: PY-006 reads it). The keyword `auth` is
	// a direct keyword of the call (depth one, as the window steps over nested groups whole), followed by a two-element tuple. The literal `grant_type` anywhere in the call refuses it: the guarded window cannot pass it, before
	// the keyword or after the tuple. The anchor group is `auth`, the same code position as PY-006's.
	// ------------------------------------------------------------------------------------------

	private const string _EX_PY_002 =
		@"(?<![\w.])(?:requests\.(?:get|post|put|patch|delete|head|options|request|Request)|httpx\.(?:get|post|put|patch|delete|head|options|request|stream|Client|AsyncClient))(?!\w)" + _B + @"\("
			+ _GUARDED_WINDOW + @"(?<![\w.])(?<auth>auth)" + _B + @"=(?!=)" + _B + _GUARDED_TUPLE_OF_TWO + @"(?=" + _GUARDED_WINDOW + @"\))";

	// ------------------------------------------------------------------------------------------
	// PY-003: a carrier, the header name `Authorization` (any case, whole word: a proxy or an X- name does not match) and a value whose leading literal is `Basic` or a call of `encode_basic_auth(`. The match extends into the
	// string zones to read the name and the value; the anchor is the carrier token.
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The encoder that a Basic header value is built from, read as the end of the value: after the scheme literal, a concatenation (the closing quote, a plus) or a replacement field of an f-string, then the callee
	/// <c>b64encode</c>, <c>standard_b64encode</c> or <c>encodebytes</c> (up to three qualifiers) and its opening parenthesis. It is optional and bounded, and it only lengthens the match of a value that was already read, so
	/// the span of the header class covers the encoder and the encoder class, which anchors at the callee, is subsumed by overlap. A value whose encoder is in another statement does not have it and is two findings.
	/// </summary>
	private const string _BASIC_ENCODER_TAIL =
		@"(?:[ \t]{0,8}(?:[\x27\x22][ \t]{0,8}\+|\{)[ \t]{0,8}(?:\w{1,40}\.){0,3}(?:standard_b64encode|b64encode|encodebytes)[ \t]{0,8}\()?";

	private const string _EX_PY_003 = _CARRIERS + @"(?:" + _BASIC_LITERAL_VALUE + _BASIC_ENCODER_TAIL + @"|" + _BASIC_ENCODER_VALUE + @")";

	private const string _EX_PY_004 =
		@"(?<!\w)(?<construct>HTTPBasicAuthHandler)\(";

	private const string _EX_PY_005 =
		@"(?<!\w)(?<construct>make_headers)" + _B + @"\(" + _WINDOW + @"(?<![\w.])basic_auth" + _B + @"=(?!=)"
			+ @"(?:" + _B + _PREFIX + @"(?:\x27[^:{}\x27\\\n]{1,100}:(?!" + _PLACEHOLDER + @"\x27)(?<credential>[^{}\x27\\\n]{1,200})\x27|\x22[^:{}\x22\\\n]{1,100}:(?!" + _PLACEHOLDER + @"\x22)(?<credential>[^{}\x22\\\n]{1,200})\x22))?";

	// ------------------------------------------------------------------------------------------
	// PY-006: the unnamed half. The word `auth` as a keyword, an attribute after a dot or a simple name, `=` and a two-element tuple; no receiver is examined (that is the cap). The refusal of `grant_type` is the same line, 300 characters either side of the keyword: the forward guard stands right after the keyword, so it covers the gap, the tuple and what follows it.
	// ------------------------------------------------------------------------------------------

	private const string _EX_PY_006 =
		@"(?<!\w)(?=auth" + _B + @"=(?!=)" + _B + @"\()(?<!grant_type[^\n]{0,300})(?<auth>auth)(?![^\n]{0,300}grant_type)" + _B + @"=(?!=)" + _B + _TUPLE_OF_TWO;

	// ------------------------------------------------------------------------------------------
	// PY-007: the encoder callee (the URL-safe and the other alphabets are different whole words) whose first argument is a colon-joined pair, then the use requirement: `Basic` or `Authorization` within 300 characters of
	// the encoder in either direction. The pair forms: a literal pair (the credential is the part after the colon), a literal with fields, a concatenation, a `%` format, `.format(` and `":".join(`.
	// ------------------------------------------------------------------------------------------

	private const string _COLON_JOINED_PAIR =
		@"(?:" + @"[rRbBuU]{0,2}(?:\x27[^:{}\x27\\\n]{1,100}:(?!" + _PLACEHOLDER + @"\x27)(?<credential>[^{}\x27\\\n]{1,200})\x27|\x22[^:{}\x22\\\n]{1,100}:(?!" + _PLACEHOLDER + @"\x22)(?<credential>[^{}\x22\\\n]{1,200})\x22)"
		+ @"|" + _PREFIX + @"[\x27\x22][^\x27\x22\\\n]{0,100}:[^\x27\x22\\\n]{0,100}[\x27\x22]"
		+ @"|" + _QUOTE_BEARING_F_STRING_PAIR
		+ @"|\w[\w.]{0,60}" + _B + @"\+" + _B + @"[\x27\x22]:[\x27\x22]" + _B + @"\+"
		+ @"|[\x27\x22]%s:%s[\x27\x22]" + _B + @"%"
		+ @"|[\x27\x22]\{[^{}\n]{0,20}\}:\{[^{}\n]{0,20}\}[\x27\x22]\.format\("
		+ @"|[\x27\x22]:[\x27\x22]\.join\()";

	private const string _USE_WORD = @"(?i:basic|authorization)";

	/// <summary>The use requirement of the held-name shape: the word `Basic` alone; `Authorization` is not enough for a name whose content is unknown.</summary>
	private const string _HELD_USE_WORD = @"(?i:basic)";

	/// <summary>
	/// The held-name shape of the catalog: the first argument is a bare identifier or an attribute chain, or a bare name followed by `.encode(...)`, or a call of a bare `encode(name)`; the colon-joined construction lies elsewhere.
	/// A literal, a concatenation or a call with other arguments is not this shape (the argument must end at a comma or the closing parenthesis). There is no credential position. Since detection catalog 1.50.0 the argument may span lines:
	/// every seam of the shape (after the inner opening parenthesis of `encode(`, around the argument text of `.encode(`, and between the held name, or the close of its inner call, and the outer comma or closing
	/// parenthesis) is a run of at most 40 blanks, tabs and line breaks, so a comment in a seam is not read.
	/// </summary>
	private const string _HELD_NAME =
		@"(?:(?:\w{1,40}\.){0,3}encode\(" + _S + @"\w[\w.]{0,60}" + _S + @"\)|\w[\w.]{0,60}(?:\.encode\(" + _HELD_ENCODE_ARGUMENT + @"\))?)(?=" + _S + @"[,)])";

	private const string _EX_PY_007 =
		@"(?<!\w)(?<construct>standard_b64encode|b64encode|encodebytes)" + _B + @"\(" + _S
			+ @"(?:" + _COLON_JOINED_PAIR + @"(?:(?<=" + _USE_WORD + @"[\s\S]{0,300})|(?=[\s\S]{0,300}" + _USE_WORD + @"))"
			+ @"|" + _HELD_NAME + @"(?:(?<=" + _HELD_USE_WORD + @"[\s\S]{0,300})|(?=[\s\S]{0,300}" + _HELD_USE_WORD + @")))";

	private const string _EX_PY_008 =
		@"(?<!\w)(?<construct>encode_basic_auth)\((?!" + _S + @"\))"
			+ @"(?:" + _ELEMENT + @"," + _S + @"(?:password" + _B + @"=(?!=)" + _B + @")?" + _CREDENTIAL_ARGUMENT + @")?";

	private const string _EX_PY_009 =
		@"\.(?<construct>add_password)\((?:" + _S + _ELEMENT + @",){3}" + _S + @"(?:" + _CREDENTIAL_ARGUMENT + @"|" + _ELEMENT + @")";

	private const string _EX_PY_010 =
		@"(?<!\w)(?<construct>NetRCAuth)\(";

	// ------------------------------------------------------------------------------------------
	// PY-011: the callee token of a request-making call (the module-qualified forms and the method names), whose argument window holds a userinfo URL literal of the scheme `http` or `https`. A literal that is the value of
	// a name, keyword or key that contains `proxy` or `proxies` is refused by a look-behind that stands after the forward walk to the literal (rare: only a window with a URL in it gets here).
	// PY-012: the declaration name or keyword that holds such a literal, with no call in between.
	// ------------------------------------------------------------------------------------------

	private const string _EX_PY_011 =
		@"(?<!\w)(?<construct>" + _REQUEST_METHODS + @"|urlopen|Request)(?!\w)" + _B + @"\(" + _WINDOW + _PREFIX + _USERINFO_URL_FORWARD_GUARD + _PROXY_NAMED_PREFIX + @"[\x27\x22]" + _USERINFO_URL;

	private const string _EX_PY_012 =
		@"(?<![\w.])(?<construct>(?>[A-Za-z_]\w{0,63}))" + _B + @"(?::=|=(?!=))" + _B + _PREFIX
			+ _USERINFO_URL_FORWARD_GUARD + @"(?<!(?i:prox(?:y|ies))\w{0,40}" + _B + @"(?::=|=)" + _B + _PREFIX + @")[\x27\x22]" + _USERINFO_URL;

	/// <summary>
	/// The credential position of `PY-013` (detection catalog 1.50.0, form (a) of the held declaration): a look-behind, after the match of the carrier, that finds the declaration `NAME = "Basic <token>"` of the very name the carrier
	/// uses (the group `scheme`), at a line start with an optional annotation, whose literal is the scheme word, a blank and a token of 4 to 200 letters, digits or `+ / = _ -` that fills the rest of the literal (the placeholder
	/// filter of the header literal applies), followed only by the end of the statement; the group `credential` is the token. It reads back at most thirty lines of at most 400 characters (the shape of the look-behind of
	/// the prompt signal), is optional so a name with no such declaration in that window keeps no credential position, and stands behind the forward match of the carrier, which is rare. The literal is never decoded.
	/// </summary>
	private const string _HELD_LITERAL_TOKEN =
		@"(?:(?<=(?:\A|\n)[ \t]{0,40}\k<scheme>" + _B + @"(?::[^=\n]{1,200})?=(?!=)" + _B + @"[rRuUfF]{0,2}"
			+ @"(?:\x27(?i:basic)[ \t]{1,8}(?!" + _PLACEHOLDER + @"\x27)(?<credential>[A-Za-z0-9+/=_-]{4,200})\x27"
			+ @"|\x22(?i:basic)[ \t]{1,8}(?!" + _PLACEHOLDER + @"\x22)(?<credential>[A-Za-z0-9+/=_-]{4,200})\x22)"
			+ _B + @"(?:[;#][^\n]{0,400})?(?:\n[^\n]{0,400}){0,30}))?";

	private const string _EX_PY_013 =
		_CARRIERS + @"(?<scheme>(?>[A-Za-z_]\w{0,63}))(?=" + _B + @"(?:[,)}\]#\n]|\z)|" + _S + @"[,)}\]])" + _HELD_LITERAL_TOKEN;

	private const string _EX_PY_014 =
		@"[\x27\x22](?<construct>(?i:authorization))[\x27\x22]" + _B + @":" + _B + _BASIC_LITERAL_VALUE + _BASIC_ENCODER_TAIL;


	/// <summary>
	/// THE HELD SCHEME VALUE THAT BEGINS WITH A WORD (detection catalog 1.50.0, `PY-013`, a Python-side capture of the declaration expression; the shared symbol table is unchanged). A leading literal of an optional prefix of up to two of
	/// `r R u U f F` and one kind of single-line quote whose body begins with a word (at most 64 characters, not followed by a word character) is captured in the group <c>value</c> as that WORD ONLY, so the scheme verdict of the
	/// engine, which compares the declared value with `Basic` without regard to case, reads the leading word: `Basic` confirms, any other word (`Bearer`, `Token`, `Digest`) does not match. What may follow the literal, before
	/// the end of the statement that the declaration expression requires: nothing (the literal alone, a token after the word included, or an `f`-string), `+` or `%` and an operand, or `.format(` and its arguments. Any other
	/// continuation (a second `+`, a conditional, `or`, a fourth call group, a line break) fails the expression, so the declaration is not read and the name is indeterminate, as before. It stands before the plain literal
	/// alternatives, which read the rest (a literal that does not begin with a word, an empty one, a longer body is the same as before). The leading literal of an `f`-string may hold replacement fields of up to 98 characters
	/// (a field may hold the literal's own quote), which are skipped and never examined.
	/// </summary>
	private const string _COMPOUND_DECLARED_VALUE =
		@"(?:[rRuU]{0,2}(?:\x27(?<value>(?>\w{1,64}))(?!\w)(?>[^\x27\\\n]|\\.){0,400}\x27|\x22(?<value>(?>\w{1,64}))(?!\w)(?>[^\x22\\\n]|\\.){0,400}\x22)"
			+ @"|[rR]?[fF][rR]?(?:\x27(?<value>(?>\w{1,64}))(?!\w)(?>[^\x27\\\n{]|\\.|\{\{|\{[^{}\n]{0,98}\}){0,400}\x27|\x22(?<value>(?>\w{1,64}))(?!\w)(?>[^\x22\\\n{]|\\.|\{\{|\{[^{}\n]{0,98}\}){0,400}\x22))" + _COMPOUND_TAIL;

	private const string _ENVIRONMENT_KEY = @"(?:\x27[^\x27\\\n]{1,200}\x27|\x22[^\x22\\\n]{1,200}\x22)";

	/// <summary>
	/// The constant declaration that the file symbol table counts: `NAME = literal` and `NAME: annotation = literal` at a line start (the annotation is greedy and cannot cross an equals sign or a line end, so no lazy loop
	/// stands in an optional group), a single-line single- or double-quoted literal, or an `os.environ` or `os.getenv` read marked by the empty group `environment` with the leading word of its fallback in `fallbackWord`.
	/// The environment alternative opens behind a forward guard on the rare literal `os.`.
	/// </summary>
	private const string _EX_PY_101 =
		@"^[ \t]{0,40}(?<name>[A-Za-z_][A-Za-z0-9_]{0,127})[ \t]{0,8}(?::[^=\n]{1,200})?=(?!=)[ \t]{0,8}(?:" + _COMPOUND_DECLARED_VALUE + @"|[rRuU]?\x27(?<value>(?>[^\x27\\\n]|\\.){0,400})\x27|[rRuU]?\x22(?<value>(?>[^\x22\\\n]|\\.){0,400})\x22"
			+ @"|(?=os\.)(?<environment>)os\.(?:environ\[[ \t]{0,8}" + _ENVIRONMENT_KEY + @"[ \t]{0,8}\](?<fallbackWord>)"
			+ @"|(?:environ\.get|getenv)\([ \t]{0,8}" + _ENVIRONMENT_KEY + @"[ \t]{0,8}(?:,[ \t]{0,8}(?:\x27(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x27\\\n]|\\.){0,200}\x27|\x22(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^\x22\\\n]|\\.){0,200}\x22)[ \t]{0,8}|(?<fallbackWord>))\)))"
			+ @"(?=[ \t]{0,8}(?:;|$|#))";

	/// <summary>`EX-PY-110`: a class statement whose base is `TestCase` (`unittest.TestCase`, `TestCase`, `IsolatedAsyncioTestCase`). Kind Test, zone code only.</summary>
	private const string _EX_PY_110 =
		@"^[ \t]{0,40}class[ \t]{1,8}\w{1,64}[ \t]{0,8}\([^)\n]{0,200}(?<![\w])(?:IsolatedAsyncioTestCase|TestCase)(?!\w)";

	/// <summary>
	/// `EX-PY-111`: the generated-file banner. (a) Within the first three lines, a comment that holds `auto-generated`, `autogenerated`, `auto generated`, `@generated` or `generated by`; (b) within the first ten lines, a comment that
	/// holds `do not edit`, `do not modify`, `do not hand-edit`, `changes will be lost` or `changes will be overwritten`. Anchored at the start of the text; every line before the comment is at most 400 characters.
	/// </summary>
	private const string _EX_PY_111 =
		@"(?:\A(?:[^\n]{0,400}\n){0,2}[ \t]{0,40}#[^\n]{0,300}(?i:auto-generated|autogenerated|auto generated|@generated|generated by)"
			+ @"|\A(?:[^\n]{0,400}\n){0,9}[ \t]{0,40}#[^\n]{0,300}(?i:do not edit|do not modify|do not hand-edit|changes will be lost|changes will be overwritten))";

	/// <summary>The refusal of a local declaration of `input` in the thirty lines before the call: a function of that name, an assignment to it, or a parameter of the enclosing function.</summary>
	private const string _LOCAL_INPUT_DECLARATION =
		@"(?<!(?:def[ \t]{1,8}input(?!\w)|(?<![\w.])input[ \t]{0,8}=(?!=)|def[ \t]{1,8}\w{1,64}\([^)\n]{0,200}(?<![\w.])input(?!\w))(?:[^\n]{0,400}\n){0,30}[^\n]{0,400})";

	private const string _EX_PY_120 =
		@"(?<!\w)(?<anchor>getpass)\(";

	private const string _EX_PY_121 =
		@"(?<![\w.])(?=input\(" + _S + _PREFIX + @"[\x27\x22][^\x27\x22\n]{0,100}" + _NARROW_CREDENTIAL_WORD + @")" + _LOCAL_INPUT_DECLARATION + @"(?<anchor>input)\(";

	private const string _EX_PY_122 =
		@"(?<![\w.])webbrowser\.(?<anchor>open_new_tab|open_new|open)(?!\w)" + _B + @"\(";

	private const string _QUERY_CODE_READ = @"parse_qsl?\([\s\S]{0,300}[\x27\x22]code[\x27\x22]";

	private const string _EX_PY_123 =
		@"(?<!\w)(?<anchor>ThreadingHTTPServer|HTTPServer|TCPServer)(?!\w)" + _B + @"\([^\n]{0,200}[\x27\x22](?:127\.0\.0\.1|localhost|::1)[\x27\x22]"
			+ @"(?:(?=[\s\S]{0,1000}" + _QUERY_CODE_READ + @")|(?<=" + _QUERY_CODE_READ + @"[\s\S]{0,1000}))";

	private const string _EX_PY_124 =
		@"(?<![\w.])request\.(?<anchor>form|POST)(?!\w)(?:\[" + _S + @"|\.get\(" + _S + @")[\x27\x22][^\x27\x22\n]{0,100}" + _NARROW_CREDENTIAL_WORD + @"(?!\w{0,30}" + _LABEL_SUFFIX + @")";

	private const string _EX_PY_125 =
		@"(?:(?<![\w.])(?<anchor>OAuth2PasswordRequestForm)(?!\w)|(?<![\w])(?<anchor>Form)\((?<=" + _NARROW_CREDENTIAL_WORD + @"\w{0,30}" + _B + @":[^\n]{0,150}Form\())";

	private const string _EX_PY_126 =
		@"(?:(?<!\w)(?<anchor>authenticate)\(" + _WINDOW + @"(?<![\w.])password" + _B + @"=(?!=)|(?<!\w)(?<anchor>login)\(" + _B + @"request(?!\w))";

	private const string _EX_PY_127 =
		@"(?:(?<![\w.])request\.(?<anchor>session)\[|^[ \t]{0,40}from[ \t]{1,8}flask[ \t]{1,8}import[^\n]{0,200}(?<![\w.])(?<anchor>session)(?!\w))";

	private const string _CREDENTIAL_KEY_LITERAL = @"[\x27\x22][^\x27\x22\n]{0,100}" + _CREDENTIAL_WORD;

	private const string _EX_PY_130 =
		@"(?<![\w.])os\.(?<anchor>environ|getenv)(?!\w)(?:\[" + _S + @"|\.get\(" + _S + @"|\(" + _S + @")" + _CREDENTIAL_KEY_LITERAL;

	private const string _CONFIGURATION_RECEIVER = @"(?<anchor>(?i:config|conf|settings|cfg|parser))(?!\w)";

	/// <summary>The attribute form: a member whose name holds a credential word, read as an attribute of the receiver (a call of the member is not an attribute read).</summary>
	private const string _CREDENTIAL_NAMED_MEMBER = @"\.(?=\w{0,64}" + _CREDENTIAL_WORD + @")(?>\w{1,64})(?![\w(])";

	private const string _EX_PY_131 =
		@"(?<!\w)" + _CONFIGURATION_RECEIVER + @"(?:(?:\[|\.get\()" + _S + @"(?:[\x27\x22][^\x27\x22\n]{0,100}[\x27\x22]" + _B + @"(?:\]\[|,)" + _S + @")?" + _CREDENTIAL_KEY_LITERAL + @"|" + _CREDENTIAL_NAMED_MEMBER + @")";

	private const string _EX_PY_132 =
		@"\.(?<anchor>authenticators)\(";

	private const string _EX_PY_133 =
		@"(?<![\w.])(?=(?>\w{1,64})" + _B + @"(?::[^=\n]{1,100})?=(?!=))(?=\w{0,64}" + _CREDENTIAL_WORD + @")(?<anchor>(?>\w{1,64}))(?<!" + _LABEL_SUFFIX + @")" + _B + @"(?::[^=\n]{1,100})?=(?!=)" + _B
			+ @"(?:[rRbBuU]{0,2}(?:\x27(?!" + _PLACEHOLDER + @"\x27)[^\x27\\\n]{1,400}\x27|\x22(?!" + _PLACEHOLDER + @"\x22)[^\x22\\\n]{1,400}\x22)|(?:[fF][rR]?|[rR][fF])(?:\x27(?!" + _PLACEHOLDER + @"\x27)[^\x27\\\n{}]{1,400}\x27|\x22(?!" + _PLACEHOLDER + @"\x22)[^\x22\\\n{}]{1,400}\x22))";

	[GeneratedRegex(_EX_PY_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex BasicAuthHelperConstruction();

	[GeneratedRegex(_EX_PY_002, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NamedClientAuthTuple();

	[GeneratedRegex(_EX_PY_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderBasicScheme();

	[GeneratedRegex(_EX_PY_004, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UrllibBasicAuthHandlerConstruction();

	[GeneratedRegex(_EX_PY_005, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Urllib3MakeHeadersBasicAuth();

	[GeneratedRegex(_EX_PY_006, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UnnamedAuthTuple();

	[GeneratedRegex(_EX_PY_007, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Base64UserPasswordPair();

	[GeneratedRegex(_EX_PY_008, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AiohttpEncodeBasicAuth();

	[GeneratedRegex(_EX_PY_009, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex PasswordManagerAddPassword();

	[GeneratedRegex(_EX_PY_010, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HttpxNetrcAuth();

	[GeneratedRegex(_EX_PY_011, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialsInRequestUrl();

	[GeneratedRegex(_EX_PY_012, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UrlUserinfoHeldLiteral();

	[GeneratedRegex(_EX_PY_013, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueHeldInVariable();

	[GeneratedRegex(_EX_PY_014, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex BasicLiteralInAuthorizationMapping();

	[GeneratedRegex(_EX_PY_110, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UnitTestCaseContentMarker();

	[GeneratedRegex(_EX_PY_111, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedFileBannerContentMarker();

	[GeneratedRegex(_EX_PY_120, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GetpassCall();

	[GeneratedRegex(_EX_PY_121, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex InputPasswordPrompt();

	[GeneratedRegex(_EX_PY_122, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WebBrowserOpenCall();

	[GeneratedRegex(_EX_PY_123, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex LoopbackRedirectReceiver();

	[GeneratedRegex(_EX_PY_124, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex RequestFormCredentialRead();

	[GeneratedRegex(_EX_PY_125, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex FastApiCredentialForm();

	[GeneratedRegex(_EX_PY_126, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex DjangoAuthenticateOrLogin();

	[GeneratedRegex(_EX_PY_127, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex FrameworkSessionUse();

	[GeneratedRegex(_EX_PY_130, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex EnvironmentCredentialRead();

	[GeneratedRegex(_EX_PY_131, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConfigFileCredentialRead();

	[GeneratedRegex(_EX_PY_132, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NetrcCredentialRead();

	[GeneratedRegex(_EX_PY_133, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialNamedLiteralAssignment();

	[GeneratedRegex(_EX_PY_101, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	[GeneratedRegex(_GENERATED_STUB_FILE_NAME, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedStubFileNameMarker();
}
