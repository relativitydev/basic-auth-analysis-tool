using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The expressions of the `Go` analyzer: the constant declaration, the member declaration and the thirteen pattern classes GO-001 to GO-013. Each carries the policy match timeout, is
/// `CultureInvariant | ExplicitCapture` and case-sensitive, the case-insensitive words being written inline. Every class anchors on a code token (the group <c>construct</c>) and never on a
/// quoted header name or a quoted scheme word, because an anchor in a string body composes to Low. `Go` declares no first-line rule, so no expression here carries the lexical budget.
/// </summary>
internal static partial class GoPatternExpressions
{
	// ------------------------------------------------------------------------------------------
	// The constant declaration (the symbol table)
	// ------------------------------------------------------------------------------------------

	private const string _ENVIRONMENT_READ =
		_QUALIFIER + @"(?:Getenv|LookupEnv)[ \t]{0,3}\([ \t]{0,3}(?:\x22[^\x22\\\n]{1,128}\x22|`[^`\n]{1,128}`)[ \t]{0,3}\)";

	private const string _DECLARATION_HEAD =
		@"^[ \t]*(?:(?:const|var)[ \t]{1,8})?(?<name>(?!(?:const|var)[ \t])" + _IDENTIFIER_PLAIN + @")(?:[ \t]{1,8}" + _DECLARED_TYPE + @")?[ \t]*(?::=|=(?!=))[ \t]*";

	private const string _TWO_VALUE_HEAD =
		@"^[ \t]*(?<name>" + _IDENTIFIER_PLAIN + @")[ \t]{0,8},[ \t]{0,8}" + _IDENTIFIER_PLAIN + @"[ \t]{0,8}(?::=|=(?!=))[ \t]*";

	private const string _EX_GO_101 =
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
	private const string _EX_GO_001 =
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
		@"(?<sprintf>" + _QUALIFIER + @"Sprintf" + _SPACE + @"\(" + _SPACE + @")?[\x22`](?i:basic)(?=[ \t\x22`])(?:[ \t]{1,8}(?!" + _PLACEHOLDER_BODY + @"[\x22`])(?<credential>[A-Za-z0-9+/=_-]{4,})(?=[\x22`]" + _SPACE + @"(?:[,)}\];]|\z|//|/\*)))?";

	/// <summary>A value that is an identifier (a precomputed token): not a call, not a member, not an index, and the whole value.</summary>
	private const string _IDENTIFIER_VALUE =
		@"(?![\x22`])(?<scheme>" + _IDENTIFIER + @")(?![\p{L}\p{N}_.(\[])(?=" + _SPACE + @"(?:[,)}\];]|\z))";

	/// <summary>
	/// The nested encoder, read after a value that starts with the literal `Basic` (the declared subsumption of GO-002, GO-003 and GO-004 over GO-006, which is a span overlap): the rest of the scheme literal
	/// to its closing quote, a `+` (or, only when the value is the first argument of a `Sprintf` call, the `,` that ends that argument; the comma of a list or of an `append` call never extends the match), an optional qualifier, and the encoder up to its opening parenthesis. It is optional, atomic and bounded, so a value that
	/// is not built with the encoder in the same expression is matched exactly as before, and a nested value gives a match whose span covers the encoder call.
	/// </summary>
	private const string _NESTED_ENCODER_TAIL_HEAD =
		@"[^\x22`\n]{0,60}[\x22`]" + _SPACE + @"(?(sprintf),|\+)" + _SPACE + _QUALIFIER + @"(?:RawStdEncoding|StdEncoding)" + _SPACE + @"\." + _SPACE + @"(?:EncodeToString|AppendEncode|Encode)" + _NOT_WORD_AHEAD + _SPACE + @"\(";

	/// <summary>The nested encoder tail of GO-003 and GO-004: optional, atomic and bounded, ending at the opening parenthesis of the encoder call.</summary>
	private const string _NESTED_ENCODER_TAIL =
		@"(?>" + _NESTED_ENCODER_TAIL_HEAD + @")?";

	/// <summary>The nested encoder tail of GO-002 (0.5): the same tail, and the whole-literal pair operand of the encoder is the credential of the header class.</summary>
	private const string _NESTED_ENCODER_TAIL_WITH_PAIR_CREDENTIAL =
		@"(?>" + _NESTED_ENCODER_TAIL_HEAD + _NESTED_WHOLE_LITERAL_PAIR_CREDENTIAL + @")?";

	/// <summary>
	/// The credential of the nested encoder (0.5): its operand is `[]byte(` and one whole-literal pair, the pair rule of GO-006 (a user part with no colon or blank, a token with no blank, no placeholder), and the closing
	/// parenthesis of the conversion. The whole-literal pair is the credential of the header class even though the encoder match is subsumed.
	/// </summary>
	private const string _NESTED_WHOLE_LITERAL_PAIR_CREDENTIAL =
		@"(?:(?=" + _SPACE + @"\[\]byte" + _SPACE + @"\(" + _SPACE + @"(?=" + _WHOLE_LITERAL_PAIR + @")" + _PAIR_CREDENTIAL + _SPACE + @"\)))?";

	private const string _EX_GO_002 =
		_CARRIERS_K1_AND_K2 + _BASIC_VALUE + _NESTED_ENCODER_TAIL_WITH_PAIR_CREDENTIAL;

	private const string _KEY_IN_A_LITERAL =
		_HEADER_NAME_LITERAL + _SPACE + @":" + _SPACE;

	private const string _EX_GO_003 =
		_NOT_WORD_BEHIND + @"(?<construct>Header)" + _NOT_WORD_AHEAD + _SPACE + @"\{" + _ARGUMENT_WINDOW + _KEY_IN_A_LITERAL + @"(?:\[\]string" + _SPACE + @")?\{" + _SPACE + _BASIC_VALUE + _NESTED_ENCODER_TAIL;

	private const string _EX_GO_004 =
		_NOT_WORD_BEHIND + @"(?<construct>map)" + _NOT_WORD_AHEAD + @"\[string\](?:\[\]string|string)" + _SPACE + @"\{" + _ARGUMENT_WINDOW + _KEY_IN_A_LITERAL + @"(?:(?:\[\]string)?" + _SPACE + @"\{" + _SPACE + @")?" + _BASIC_VALUE + _NESTED_ENCODER_TAIL;

	private const string _CALLEES_WITH_A_URL =
		@"NewRequestWithContext|NewRequest|PostForm|Post|Head|Get";

	private const string _EX_GO_005 =
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

	private const string _EX_GO_006 =
		_NOT_WORD_BEHIND + @"(?<construct>RawStdEncoding|StdEncoding)" + _SPACE + @"\." + _SPACE + @"(?:EncodeToString|AppendEncode|Encode)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _PAIR_CONVERSION + _USE_REQUIREMENT + _WHOLE_LITERAL_PAIR_CREDENTIAL;

	private const string _TWO_ARGUMENTS_TO_THE_CLOSING_PARENTHESIS =
		@"(?=" + _SPACE + _ONE_ARGUMENT + @"," + _SPACE + @"(?:" + _CREDENTIAL_LITERAL + @"(?=" + _SPACE + @"(?:," + _SPACE + @")?\)))?(?>(?:" + _ARGUMENT_UNIT_WITHOUT_A_COMMA + @"){0,300})" + _SPACE + @"(?:," + _SPACE + @")?\))";

	/// <summary>The refusals of `UserPassword`, read after the rare literal: a declaration, the argument window of `ProxyURL(`, a name that holds `proxy` or `proxies`, and a `url.URL` literal whose `Scheme:` is another literal.</summary>
	private const string _USER_PASSWORD_REFUSALS =
		@"(?<!(?:func|type)[ \t]{1,8}UserPassword)(?<!\)[ \t]{0,8}UserPassword)"
			+ @"(?<!ProxyURL" + _SPACE + @"\([\s\S]{0,400}UserPassword)"
			+ @"(?<!(?i:proxy|proxies)[\p{L}\p{N}_.]{0,40}[ \t]{0,8}(?::=|=|:)[^\n]{0,200}UserPassword)"
			+ @"(?<!Scheme[ \t]{0,3}:[ \t]{0,3}\x22(?!(?i:https?)\x22)[A-Za-z][A-Za-z0-9+.-]{0,20}\x22[^\n]{0,400}UserPassword)";

	private const string _EX_GO_007 =
		_NOT_WORD_BEHIND + @"(?<construct>UserPassword)" + _NOT_WORD_AHEAD + _USER_PASSWORD_REFUSALS + _SPACE + @"\(" + _TWO_ARGUMENTS_TO_THE_CLOSING_PARENTHESIS;

	private const string _NAME_WITHOUT_PROXY =
		@"(?![\p{L}\p{N}_]{0,64}(?i:proxy|proxies))";

	/// <summary>
	/// A declaration is read from the line start, as the constant declaration is (a leading blank or tab is allowed, so a name inside a block is read): the optional keyword, the declared name (the anchor, a
	/// name that holds `proxy` or `proxies` refused), an optional type, the assignment, and the credentialed URL literal. Read backwards from the equals sign, a type would be mistaken for the name.
	/// </summary>
	private const string _HELD_URL_DECLARATION =
		@"^[ \t]{0,16}(?:(?:const|var)[ \t]{1,8})?(?<construct>(?!(?:const|var)[ \t])" + _NAME_WITHOUT_PROXY + _IDENTIFIER_PLAIN + @")(?:[ \t]{1,8}" + _DECLARED_TYPE + @")?[ \t]{0,8}(?::=|=(?!=))" + _SPACE + _USERINFO_URL;

	private const string _EX_GO_008 =
		@"(?:" + _NOT_WORD_BEHIND + @"(?<construct>ParseRequestURI|Parse)" + _NOT_WORD_AHEAD + _SPACE + @"\(" + _SPACE + _USERINFO_URL + @"|" + _HELD_URL_DECLARATION + @")";

	private const string _EX_GO_009 =
		_CARRIERS_K1_AND_K2 + _IDENTIFIER_VALUE;

	private const string _ENVIRONMENT_VALUE =
		_QUALIFIER + @"(?:Getenv|LookupEnv)" + _SPACE + @"\(" + _SPACE + @"(?:" + _DQ_LITERAL + @"|" + _RAW_LITERAL + @")" + _SPACE + @"\)(?=" + _SPACE + @"(?:[,)}\];]|\z))";

	private const string _EX_GO_012 =
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

	private const string _EX_GO_010 =
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

	private const string _EX_GO_011 =
		_FUNCTION_HEAD + _BASIC_HELPER_BODY;

	/// <summary>
	/// EX-GO-102, the member declaration of the file symbol table's second dictionary (0.5): one record per `func` declaration at the line start, a function or a method, its name and an empty mark when GO-011 reads the declaration
	/// (its head, its window and its conjunction). The mark is the engine's own empty group for a record of the second kind, <c>environment</c> with its empty <c>fallbackWord</c>, and the empty group <c>basicHelper</c> names it for
	/// the catalog. A `RoundTrip` method, a generic function and a function with no parameter are recorded and never marked.
	/// </summary>
	private const string _EX_GO_102 =
		@"^[ \t]{0,64}func[ \t]{1,8}(?:\([^)\n]{0,200}\)[ \t]{0,3})?(?<name>" + _IDENTIFIER + @")(?=[ \t]{0,3}[(\[])(?:(?<!(?<![\p{L}\p{N}_])RoundTrip)(?=(?>" + _FUNCTION_PARAMETERS + @")" + _BASIC_HELPER_BODY + @")(?<environment>)(?<fallbackWord>)(?<basicHelper>))?";

	/// <summary>
	/// EX-GO-013: a carrier K1 or K2 for `Authorization` whose value is a call `name(…)` to a bare name, with no dot before it, and nothing after the call but an optional trailing comma and what closes the carrier. The group <c>name</c> is the called name,
	/// which the engine resolves against the member table.
	/// </summary>
	private const string _EX_GO_013 =
		_CARRIERS_K1_AND_K2 + @"(?<![\p{L}\p{N}_.])(?<name>" + _IDENTIFIER + @")[ \t]{0,3}\(" + _ARGUMENT_WINDOW + @"\)(?=" + _SPACE + @"(?:," + _SPACE + @")?[)}])";

	/// <summary>EX-GO-101: the constant declaration of the file symbol table (the symbol table reads it before any class does).</summary>
	[GeneratedRegex(_EX_GO_101, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	/// <summary>EX-GO-001: the expression of GO-001, RequestSetBasicAuth.</summary>
	[GeneratedRegex(_EX_GO_001, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex RequestSetBasicAuth();

	/// <summary>EX-GO-002: the expression of GO-002, AuthorizationHeaderBasicScheme.</summary>
	[GeneratedRegex(_EX_GO_002, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderBasicScheme();

	/// <summary>EX-GO-003: the expression of GO-003, HeaderLiteralBasicScheme.</summary>
	[GeneratedRegex(_EX_GO_003, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HeaderLiteralBasicScheme();

	/// <summary>EX-GO-004: the expression of GO-004, MapLiteralBasicScheme.</summary>
	[GeneratedRegex(_EX_GO_004, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex MapLiteralBasicScheme();

	/// <summary>EX-GO-005: the expression of GO-005, UrlUserinfoInRequestCall.</summary>
	[GeneratedRegex(_EX_GO_005, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UrlUserinfoInRequestCall();

	/// <summary>EX-GO-006: the expression of GO-006, Base64UserPasswordPair.</summary>
	[GeneratedRegex(_EX_GO_006, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Base64UserPasswordPair();

	/// <summary>EX-GO-007: the expression of GO-007, UserinfoUserPassword.</summary>
	[GeneratedRegex(_EX_GO_007, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UserinfoUserPassword();

	/// <summary>EX-GO-008: the expression of GO-008, UrlUserinfoHeldLiteral.</summary>
	[GeneratedRegex(_EX_GO_008, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex UrlUserinfoHeldLiteral();

	/// <summary>EX-GO-009: the expression of GO-009, AuthorizationHeaderValueHeldInVariable.</summary>
	[GeneratedRegex(_EX_GO_009, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueHeldInVariable();

	/// <summary>EX-GO-010: the expression of GO-010, RoundTripperBasicAuthDefinition.</summary>
	[GeneratedRegex(_EX_GO_010, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex RoundTripperBasicAuthDefinition();

	/// <summary>EX-GO-011: the expression of GO-011, AuthenticationHelperDefinition.</summary>
	[GeneratedRegex(_EX_GO_011, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthenticationHelperDefinition();

	/// <summary>EX-GO-012: the expression of GO-012, AuthorizationHeaderValueFromEnvironment.</summary>
	[GeneratedRegex(_EX_GO_012, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueFromEnvironment();

	/// <summary>EX-GO-013: the expression of GO-013, AuthorizationHeaderValueFromLocalHelper.</summary>
	[GeneratedRegex(_EX_GO_013, _OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueFromLocalHelper();

	/// <summary>EX-GO-102: the member declaration of the file symbol table's second dictionary.</summary>
	[GeneratedRegex(_EX_GO_102, _LINE_OPTIONS, matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex MemberDeclaration();
}
