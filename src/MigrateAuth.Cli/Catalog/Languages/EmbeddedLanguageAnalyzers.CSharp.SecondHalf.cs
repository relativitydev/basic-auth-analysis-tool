using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The second half of the <c>CSharp</c> pattern expressions, CS-011..CS-018, declared as a second
/// partial declaration of <see cref="CSharpPatternExpressions"/>. <c>CSharpAnalyzer</c> is declared
/// <c>file</c>-scoped in the first file and therefore cannot be split: every <c>BuildCS011</c>..<c>BuildCS018</c>
/// <see cref="DetectionRule"/> builder stays there, beside the ten registered by the first half.
///
/// Every expression here declares <c>matchTimeoutMilliseconds</c> equal to
/// <see cref="RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS"/>, exactly like the ten before them.
/// </summary>
internal static partial class CSharpPatternExpressions
{
	/// <summary>
	/// CS-011. A method declaration whose name carries one of the anchor tokens <c>Credential</c>,
	/// <c>Authorization</c> or <c>Authenticator</c>. An access-modifier keyword and an opening brace are both
	/// required, so a call site, which carries neither, does not match.
	/// </summary>
	[GeneratedRegex(
		@"\b(?:(?:public|private|internal|protected|static|virtual|override|async)\s+)+[A-Za-z_][A-Za-z0-9_]*(?:<[A-Za-z0-9_,\s]*>)?(?:\[\])?\s+(?<declaration>[A-Za-z_][A-Za-z0-9_]*(?:Credential|Authorization|Authenticator)[A-Za-z0-9_]*)\s*\([^()]*\)\s*\{",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthenticationHelperDefinitionDeclaration();

	/// <summary>
	/// A call to a helper whose name carries the scheme word as a name component - <c>BasicHelper(</c>,
	/// <c>CreateBasicValue(</c>, <c>encoder.MakeBasicHeader(</c> - optionally through a member-access
	/// chain. <c>Basic</c> must not be followed by a lower-case letter, so an ordinary word such as
	/// <c>Basically</c> is not the scheme. The alternative sits directly after the assignment's
	/// <c>=</c>, so it has exactly one place to begin, and the identifier run before the scheme word and
	/// the member-access repetition cannot overlap: one ends at a dot and the other cannot contain one.
	/// </summary>
	private const string _SCHEME_NAMED_HELPER_CALL =
		@"(?:" + _IDENTIFIER + @"\s*\.\s*)*[A-Za-z0-9_]*[Bb]asic(?![a-z])[A-Za-z0-9_]*\s*\(";

	/// <summary>The header enumeration member CS-012 is anchored on, the header name never read from a string.</summary>
	private const string _HEADER_ENUM_MEMBER = @"HttpRequestHeader\.Authorization";

	/// <summary>
	/// The two writes CS-012 reads on the header collection besides the indexer assignment: <c>Add</c> and the
	/// replacing <c>Set</c>, each taking the enumeration member and then the value.
	/// </summary>
	private const string _ENUM_HEADER_WRITE_METHOD_NAMES = @"(?:Add|Set)";

	/// <summary>
	/// CS-012. Anchored on the <c>HttpRequestHeader.Authorization</c> enum member. The rule requires
	/// an assignment TO the indexer - <c>=</c> immediately after the closing bracket - never a read:
	/// a read has the indexer on the right of an assignment, never followed by one. Four value shapes,
	/// mirroring CS-006's own so the shared <see cref="RecognitionEnginePolicy"/> scheme verdict
	/// machinery applies: a literal-prefix scheme concatenation and a named-constant scheme concatenation,
	/// each ending in a credential accepting <see cref="_ARGUMENT_EXPRESSION"/>, so a member-access chain
	/// or a single invocation is captured whole; a FULLY LITERAL single string carrying both the scheme and
	/// the credential with no concatenation at all (e.g. <c>"Basic ZGVtbzpodW50ZXIy"</c>), optionally
	/// opened with a leading <c>$</c>, since <c>{</c> is excluded from
	/// <see cref="_FULLY_LITERAL_CREDENTIAL_TOKEN"/>; and a PARTLY LITERAL single string, also optionally
	/// opened with <c>$</c>, followed by a concatenated expression (e.g. <c>"Basic abc" + suffix</c>), whose
	/// credential group spans into the appended expression, so the literal-credential signal reads false
	/// even though the finding still fires. A verbatim, raw or interpolated-raw literal is not reached - see
	/// <see cref="LanguageIdentification.DeclaredUnsupportedCoverage"/>. The fully literal shape's credential
	/// cannot be mistaken for the literal-prefix concatenation shape because that shape requires a closing
	/// quote immediately after the scheme word (optionally preceded by whitespace) where this shape requires
	/// a non-quote, non-whitespace token character instead.
	///
	/// A fifth value shape is the value produced by a call to a helper named for the scheme
	/// (<see cref="_SCHEME_NAMED_HELPER_CALL"/>): the assignment then carries no literal scheme word,
	/// so the match has no <c>scheme</c> group and no <c>credential</c> group, the scheme verdict reads as
	/// indeterminate and the literal-credential signal as false.
	/// </summary>
	[GeneratedRegex(
		@"\.Headers\s*(?:\[\s*(?<construct>" + _HEADER_ENUM_MEMBER + @")\s*\]\s*=|\.\s*" + _ENUM_HEADER_WRITE_METHOD_NAMES + @"\s*\(\s*(?<construct>" + _HEADER_ENUM_MEMBER + @")\s*,)\s*(?:""(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s*""\s*\+\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")|\$?""(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s+(?<credential>" + _FULLY_LITERAL_CREDENTIAL_TOKEN + @")""(?!\s*\+)|\$?""(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s+(?<credential>" + _FULLY_LITERAL_CREDENTIAL_TOKEN + @"""\s*\+\s*(?:" + _ARGUMENT_EXPRESSION + @"))|(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s*\+\s*""\s*""\s*\+\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")|" + _SCHEME_NAMED_HELPER_CALL + @")",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HttpRequestHeaderEnumAuthorizationConstruction();

	/// <summary>
	/// CS-013. The required part is the binding assignment
	/// <c>ClientCredentialType = HttpClientCredentialType.Basic;</c>, anchored on the credential-type
	/// member. The credential capture is OPTIONAL, reaching for the
	/// <c>ClientCredentials.UserName.Password</c> assignment in a DIFFERENT statement, within the same
	/// five-line proximity bound as CS-016, CS-017 and CS-019.
	///
	/// The five-line window is written as nested alternations rather than as a lazy bounded loop under an
	/// optional group, because the <c>[GeneratedRegex]</c> source generator does not terminate on that shape
	/// (every <see cref="Match.NextMatch"/> returns the same match). Every exit of the outer group is "password
	/// now" or "try one more line", never "loop exhausted, credential absent". Keep it unrolled.
	/// </summary>
	private const string _CS_013_BINDING_ANCHOR =
		@"\bClientCredentialType\s*=\s*(?<construct>HttpClientCredentialType\.Basic)\s*;";

	/// <summary>One window line, consumed whole - the same shape CS-009/CS-016/CS-017/CS-018/CS-019 use.</summary>
	private const string _CS_013_WINDOW_LINE = @"[^\n]*\n";

	/// <summary>The credential statement itself, optional only through the alternation around it, never through a loop.</summary>
	private const string _CS_013_PASSWORD_ASSIGNMENT =
		@"[^\n]*?\.ClientCredentials\.UserName\.Password\s*=\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*;";

	/// <summary>Password on the binding's own line, or one line later - the innermost, deepest exit.</summary>
	private const string _CS_013_PASSWORD_UP_TO_ONE_LINE_LATER =
		"(?:" + _CS_013_PASSWORD_ASSIGNMENT + "|" + _CS_013_WINDOW_LINE + _CS_013_PASSWORD_ASSIGNMENT + ")";

	private const string _CS_013_PASSWORD_UP_TO_TWO_LINES_LATER =
		"(?:" + _CS_013_PASSWORD_ASSIGNMENT + "|" + _CS_013_WINDOW_LINE + _CS_013_PASSWORD_UP_TO_ONE_LINE_LATER + ")";

	private const string _CS_013_PASSWORD_UP_TO_THREE_LINES_LATER =
		"(?:" + _CS_013_PASSWORD_ASSIGNMENT + "|" + _CS_013_WINDOW_LINE + _CS_013_PASSWORD_UP_TO_TWO_LINES_LATER + ")";

	private const string _CS_013_PASSWORD_UP_TO_FOUR_LINES_LATER =
		"(?:" + _CS_013_PASSWORD_ASSIGNMENT + "|" + _CS_013_WINDOW_LINE + _CS_013_PASSWORD_UP_TO_THREE_LINES_LATER + ")";

	/// <summary>
	/// The outer, OPTIONAL exit: password on the binding's own line, or up to four further lines
	/// before it - five lines total - and absent otherwise. The only optional
	/// group in this expression; everything nested inside it is a required alternation, never a loop.
	/// </summary>
	private const string _CS_013_OPTIONAL_PASSWORD_UP_TO_FIVE_LINES_LATER =
		"(?:" + _CS_013_PASSWORD_ASSIGNMENT + "|" + _CS_013_WINDOW_LINE + _CS_013_PASSWORD_UP_TO_FOUR_LINES_LATER + ")?";

	[GeneratedRegex(
		_CS_013_BINDING_ANCHOR + _CS_013_OPTIONAL_PASSWORD_UP_TO_FIVE_LINES_LATER,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex WcfBasicTransportCredentialConstruction();

	/// <summary>
	/// CS-014. A method whose name names the scheme, taking two credential arguments, anchored on the
	/// spelling <c>WithBasicAuth</c>.
	/// </summary>
	[GeneratedRegex(
		@"\.(?<construct>WithBasicAuth)\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SchemeNamingExtensionMethodConstruction();

	/// <summary>
	/// CS-015. Anchored on the SDK type name with a word boundary: the trailing <c>\b</c> after
	/// <c>ImportAPI</c> rejects a longer identifier such as <c>ImportAPIWrapper</c>. The three-argument form is
	/// required exactly: two credential arguments plus the web service URL. The overloads taking only the URL
	/// or a bearer-token factory have no two top-level commas and do not match.
	///
	/// A line comment or a block comment may sit between any two tokens of the argument list
	/// (<see cref="_ARGUMENT_LIST_GAP"/>), and the third argument accepts any expression with no comma at its
	/// top level (<see cref="_LOOSE_ARGUMENT_EXPRESSION"/>). The first two stay on <see cref="_ARGUMENT_EXPRESSION"/>.
	/// </summary>
	[GeneratedRegex(
		@"\bnew\s+(?<construct>ImportAPI)\b\s*\(" + _ARGUMENT_LIST_GAP + @"(?<user>" + _ARGUMENT_EXPRESSION + @")" + _ARGUMENT_LIST_GAP + @"," + _ARGUMENT_LIST_GAP + @"(?<credential>" + _ARGUMENT_EXPRESSION + @")" + _ARGUMENT_LIST_GAP + @"," + _ARGUMENT_LIST_GAP + @"(?<url>" + _LOOSE_ARGUMENT_EXPRESSION + @")" + _ARGUMENT_LIST_GAP + @"\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SdkConstructorWithUserAndPasswordConstruction();

	/// <summary>
	/// CS-016. An object initializer carrying one half (<c>UserName</c>, anchored as <c>construct</c>) and the
	/// other half arriving by a later <c>.Password</c> property assignment, within five lines. The window is
	/// required: a match without the second statement is not an instance of the class.
	/// </summary>
	[GeneratedRegex(
		@"\bnew\s+NetworkCredential\s*\{[^{}]*?(?<construct>UserName)\s*=\s*(?<user>" + _ARGUMENT_EXPRESSION + @")[^{}]*?\}(?:[^\n]*\n){0,5}?[^\n]*?\.Password\s*=\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialAssembledAcrossStatementsConstruction();

	/// <summary>
	/// CS-017. BOTH settings properties are required - a username property alone is not a credential,
	/// per the catalog - so <c>RelativityUsername</c> (anchored as <c>construct</c>) and
	/// <c>RelativityPassword</c> both appear, within the same bounded five-line proximity window CS-009, CS-013, CS-016 and CS-019 use for a pair split across statements.
	/// </summary>
	[GeneratedRegex(
		@"\.Settings\.(?<construct>RelativityUsername)\s*=\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*;(?:[^\n]*\n){0,5}?[^\n]*?\.Settings\.RelativityPassword\s*=\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SdkSettingsCredentialPropertiesConstruction();

	/// <summary>
	/// The colon-separated pair with one half empty, written INSIDE the encoder call rather than as a
	/// statement of its own before it: <c>Convert.ToBase64String(Encoding.UTF8.GetBytes($"{""}:{token}"))</c>.
	/// The encoder is the anchor, as in the sibling alternatives, and the pair must open within
	/// <c>200</c> characters of the call's parenthesis with no statement end between them. Two
	/// families, each written with a single credential capture so the expression stays small, and each
	/// opening on a literal or on a parenthesis, so none can begin in the middle of an identifier. The
	/// left-empty family names the empty half before the credential: an interpolated empty hole, an
	/// explicit empty operand, or the colon literal as the whole argument's first operand. The
	/// right-empty family names the credential first, after an interpolation's opening brace or the
	/// argument's opening parenthesis, and then the colon with nothing after it: closing the hole and
	/// the literal, closing the hole with an empty second hole, or a concatenated colon literal with an
	/// optional empty operand. A pair whose two halves are both non-empty opens on none of them, so it
	/// can never reach this class. The families accept a few spellings that are not valid C# (a
	/// closing brace after a parenthesis, say); no valid code reads differently for it.
	/// </summary>
	private const string _INLINE_PAIR_ENCODER_WINDOW_MAXIMUM_LENGTH = "200";

	private const string _CS_018_PAIR_INSIDE_THE_ENCODER_CALL =
		@"(?<encoder>Convert\.ToBase64String)\s*\([^;]{0," + _INLINE_PAIR_ENCODER_WINDOW_MAXIMUM_LENGTH + @"}?(?:"
		+ @"(?:\$""\{\s*""""\s*\}\s*:\s*\{\s*|""""\s*\+\s*"":""\s*\+\s*|\(\s*"":""\s*\+\s*)(?<credential>" + _INLINE_CREDENTIAL_OPERAND + @")"
		+ @"|(?:\$""\{|\()\s*(?<credential>" + _INLINE_CREDENTIAL_OPERAND + @")\s*(?:\}\s*:\s*(?:\{\s*""""\s*\})?""|\+\s*"":""\s*(?:\+\s*"""")?\s*\))"
		+ @")";

	/// <summary>
	/// The credential operand of CS-018's inline shapes: a quoted literal, or a member-access chain with at
	/// most one call whose arguments hold no parentheses. It is the same idea as
	/// <see cref="_ARGUMENT_EXPRESSION"/> without its nested-parenthesis level, which keeps the inline
	/// alternatives small; an operand that is a call inside a call is not read.
	/// </summary>
	private const string _INLINE_CREDENTIAL_OPERAND =
		@"""[^""\r\n]*""|[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*(?:\s*\([^()\r\n]*\))?";

	/// <summary>
	/// Where CS-018's right-empty operand may begin: at a string literal's
	/// opening quote, or anywhere not directly preceded by an identifier-start character. The
	/// operand is the only CS-018 alternative with no literal in front of it, so without this the
	/// matcher re-enters <see cref="_ARGUMENT_EXPRESSION"/> at every character of every identifier -
	/// a start in the middle of an identifier re-scans the same tail to the same end, and so reaches
	/// the same continuation with the same outcome as the start one character earlier, which is
	/// always tried first. Excluding those starts removes only repeated work, never a match: the
	/// leftmost start of the same identifier has already succeeded or failed on the identical text.
	/// </summary>
	private const string _CS_018_RIGHT_EMPTY_OPERAND_START = @"(?:(?<![A-Za-z_])|(?=""))";

	/// <summary>
	/// CS-018. Value shapes for the colon-separated pair with one half empty. Left-empty: the empty-string
	/// operand form, with the literal <c>""</c> immediately before the colon literal; the assignment form, with
	/// the colon literal opening immediately after <c>=</c>; and the interpolated form, <c>$"{""}:{token}"</c>.
	/// Right-empty: <c>token + ":" + ""</c>, with the literal <c>""</c> immediately after the colon;
	/// <c>token + ":"</c>, with the colon literal the last thing before the statement's <c>;</c>; and the
	/// interpolated <c>$"{token}:"</c> and <c>$"{token}:{""}"</c>, with an identifier or
	/// <see cref="_ARGUMENT_EXPRESSION"/> in the first hole. These anchors keep <c>identifier + ":" + secret</c>,
	/// CS-009's construct, out of this class. All seven shapes reach the encoder within a five-line window,
	/// anchored as <c>encoder</c> like CS-009.
	/// </summary>
	[GeneratedRegex(
		@"(?:""""\s*\+\s*"":""\s*\+\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")"
		+ @"|=\s*"":""\s*\+\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")"
		+ @"|\$""\{\s*""""\s*\}\s*:\s*\{\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\}"""
		+ @"|" + _CS_018_RIGHT_EMPTY_OPERAND_START + @"(?>(?<credential>" + _ARGUMENT_EXPRESSION + @"))\s*\+\s*"":""\s*(?:\+\s*""""|;)"
		+ @"|\$""\{\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\}\s*:\s*"""
		+ @"|\$""\{\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\}\s*:\s*\{\s*""""\s*\}"""
		+ @")(?:[^\n]*\n){0,5}?[^\n]*?(?<encoder>Convert\.ToBase64String)\s*\("
		+ @"|" + _CS_018_PAIR_INSIDE_THE_ENCODER_CALL,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TokenCarriedUnderBasicSchemeConstruction();
}
