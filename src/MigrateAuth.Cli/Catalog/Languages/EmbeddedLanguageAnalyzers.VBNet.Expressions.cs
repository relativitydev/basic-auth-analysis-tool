using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// Every expression the `VBNet` analyzer hands over, generated at build time, in three files: the shared vocabulary and the first nine classes
/// here, the other ten classes with the symbol-table and content-marker expressions in <c>EmbeddedLanguageAnalyzers.VBNet.SecondHalf.cs</c>, and the
/// twenty flow signals in <c>EmbeddedLanguageAnalyzers.VBNet.FlowSignals.cs</c>. Each declares the policy match timeout,
/// <see cref="RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS"/>, and <c>IgnoreCase | CultureInvariant | ExplicitCapture</c>: VB.NET
/// is case-insensitive in its keywords, its names and its type names, and the case is never the author's decision.
///
/// THE SHAPE RULES THEY KEEP: a bounded lazy window is never inside an
/// optional group; every inserted argument expression is grouped; every alternative opens on a literal or a word start; a unit of a window is a
/// character or an atomic group, never a run, so the window reads the same from either end; and string literals are atomic units, so a colon inside one
/// does not end a statement.
///
/// A QUALIFIED SCHEME CONSTANT (`Constants.SCHEME`) IS NOT READ as the scheme argument of <c>VB-003</c> or <c>VB-006</c>: the scheme argument is a
/// literal or a bare identifier, and a qualified constant is a declared limit.
/// </summary>
internal static partial class VBNetPatternExpressions
{
	private const RegexOptions OPTIONS = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

	private const int TIMEOUT_IN_MILLISECONDS = RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS;

	// ------------------------------------------------------------------------------------------
	// Names, strings, comments
	// ------------------------------------------------------------------------------------------

	private const string _QUOTE = @"\x22";

	private const string _NAME = @"(?:[A-Za-z_][A-Za-z0-9_]*|\[[A-Za-z_][A-Za-z0-9_]*\])";

	/// <summary>A plain literal with the doubled close, or a typographic one with its doubled closer, optionally interpolated; one atomic unit, no line end inside.</summary>
	private const string _STRING =
		@"(?:\$?" + _QUOTE + @"(?>[^" + _QUOTE + @"\n]+|" + _QUOTE + _QUOTE + @")*" + _QUOTE
		+ @"|[“”](?>[^“”\n]+|””|““)*[“”])";

	private const string _COMMENT_OPENER = @"['‘’＇]";

	private const string _TRAILING_COMMENT = @"(?>" + _COMMENT_OPENER + @"[^\n]*)";

	// ------------------------------------------------------------------------------------------
	// Gaps
	// ------------------------------------------------------------------------------------------

	/// <summary>A blank run, or an explicit continuation: a blank before, the underscore, blanks, an optional trailing comment and the line end. Atomic runs, bounded by the text.</summary>
	private const string _GAP = @"(?:(?>[ \t]+)|(?<=[ \t])_[ \t]*" + _TRAILING_COMMENT + @"?\n)+";

	/// <summary>The gap plus a bare line end (with an optional trailing comment), for the positions an implicit continuation is legal at: after an opening parenthesis, a comma or an operator, and before a closing one.</summary>
	private const string _GAP_IMPLICIT = @"(?:(?>[ \t]+)|(?<=[ \t])_[ \t]*" + _TRAILING_COMMENT + @"?\n|" + _TRAILING_COMMENT + @"?\n)*";

	private const string _OPTIONAL_GAP = @"(?:" + _GAP + @")?";

	// ------------------------------------------------------------------------------------------
	// Bracket groups, four levels deep, atomic at every level, strings as units, spanning lines
	// ------------------------------------------------------------------------------------------

	private const string _STRING_UNIT = _QUOTE + @"(?>[^" + _QUOTE + @"\n]+|" + _QUOTE + _QUOTE + @")*" + _QUOTE;

	private const string _GROUP_INNERMOST = @"[({](?>" + _STRING_UNIT + @"|[^(){}" + _QUOTE + @"]+)*[)}]";

	private const string _GROUP_THIRD = @"[({](?>" + _STRING_UNIT + @"|[^(){}" + _QUOTE + @"]+|" + _GROUP_INNERMOST + @")*[)}]";

	private const string _GROUP_SECOND = @"[({](?>" + _STRING_UNIT + @"|[^(){}" + _QUOTE + @"]+|" + _GROUP_THIRD + @")*[)}]";

	private const string _GROUP = @"[({](?>" + _STRING_UNIT + @"|[^(){}" + _QUOTE + @"]+|" + _GROUP_SECOND + @")*[)}]";

	// ------------------------------------------------------------------------------------------
	// The shared argument form: a literal, or a dotted name followed by atomic bracket groups
	// ------------------------------------------------------------------------------------------

	private const string _ROOT = @"(?:(?:Me|MyClass|MyBase|Global|My)\.)?";

	private const string _DOTTED_WITH_GROUPS = _ROOT + _NAME + @"(?:\." + _NAME + @"|" + _GROUP + @")*";

	private const string _NEW_EXPRESSION = @"\bNew[ \t]+" + _NAME + @"(?:\." + _NAME + @")*(?:[ \t]*" + _GROUP + @")?(?:[ \t]+With[ \t]*" + _GROUP + @")?";

	private const string _ARGUMENT = @"(?:" + _STRING + @"|" + _NEW_EXPRESSION + @"|" + _DOTTED_WITH_GROUPS + @"|" + _GROUP + @")";

	/// <summary>The key of a header write is never a member of the enumeration: the enumeration's Authorization member is the other class's, so the two cannot overlap, and no other member is a header key at all.</summary>
	private const string _NOT_THE_ENUMERATION_MEMBER = @"(?!(?:[A-Za-z0-9_.]*\.)?HttpRequestHeader\.[A-Za-z0-9_])";

	/// <summary>An in-order named-argument prefix, optional.</summary>
	private const string _NAMED_PREFIX = @"(?:" + _NAME + @"[ \t]*:=" + _GAP_IMPLICIT + @")?";

	/// <summary>An argument with an in-order named-argument prefix.</summary>
	private const string _ARGUMENT_NAMED = _NAMED_PREFIX + _ARGUMENT;

	private const string _QUALIFIER = @"(?:Global\.)?(?:" + _NAME + @"\.)*";

	private const string _COMMA = _GAP_IMPLICIT + @"," + _GAP_IMPLICIT;

	// ------------------------------------------------------------------------------------------
	// The logical-line window: a statement, bounded at 1,200 units, lazy, required
	// ------------------------------------------------------------------------------------------

	private const string _CONTINUATION_TOKEN_BEFORE_LINE_END =
		@"(?<=(?:[,({.&+\-*^=<>]|\b(?:And|Or|Xor|AndAlso|OrElse|Of|In|Is|Mod|Like|From))[ \t]*)\n";

	/// <summary>One character or indivisible token of a statement: a string; an explicit or implicit continuation; a bracket group; any other character but a line end, a colon, a comment opener or a bracket.</summary>
	private const string _STATEMENT_UNIT =
		_STRING_UNIT + @"|(?<=[ \t])_[ \t]*" + _TRAILING_COMMENT + @"?\n|" + _CONTINUATION_TOKEN_BEFORE_LINE_END + @"|(?>" + _GROUP + @"|[(){}])|[^\n:(){}" + _QUOTE + @"'‘’＇]";

	private const string _STATEMENT_WINDOW = @"(?:" + _STATEMENT_UNIT + @"){0,1200}?";

	/// <summary>The rest of a call read forward only, ordinary characters in atomic runs, bounded at 150 units.</summary>
	private const string _STATEMENT_FORWARD_WINDOW =
		@"(?:" + _STRING_UNIT + @"|(?<=[ \t])_[ \t]*" + _TRAILING_COMMENT + @"?\n|" + _CONTINUATION_TOKEN_BEFORE_LINE_END + @"|(?>" + _GROUP + @"|[(){}])|(?>[^\n:(){}" + _QUOTE + @"'‘’＇_]+)|_){0,150}?";

	/// <summary>
	/// The inside of a call or an initializer read forward from its opening, up to 300 units, lazy: a string is one atomic unit, an explicit or implicit continuation is one unit, and
	/// any other character but a line end, a quote or a comment opener is one unit. Brackets are ordinary characters here, so a construct inside a call can be reached.
	/// </summary>
	private const string _CALL_WINDOW =
		@"(?:" + _STRING_UNIT + @"|(?<=[ \t])_[ \t]*" + _TRAILING_COMMENT + @"?\n|" + _CONTINUATION_TOKEN_BEFORE_LINE_END + @"|[^\n:" + _QUOTE + @"'\u2018\u2019\uFF07]){0,200}?";

	private const string _HTTP_TYPES = @"(?:HttpClientHandler|WebRequestHandler|SocketsHttpHandler|WinHttpHandler|WebClient|HttpWebRequest|HttpWebClientProtocol|SoapHttpClientProtocol)";

	/// <summary>Three lines, each read to at most 120 characters: the window of the flow signals, which read a short neighbourhood.</summary>
	private const string _LINE_WINDOW_THREE = @"(?:[^\n]{0,120}\n){0,3}?[^\n]{0,120}?";

	/// <summary>Five lines, each read to at most 200 characters: on a line at the 10,000-character ceiling the window is bounded by the 200, not by the line.</summary>
	private const string _LINE_WINDOW_FIVE = @"(?:[^\n]{0,200}\n){0,5}?[^\n]{0,200}?";

	// ------------------------------------------------------------------------------------------
	// Credential words: substring words, and whole-token words
	// ------------------------------------------------------------------------------------------

	private const string _CREDENTIAL_NAME =
		@"(?<![A-Za-z0-9_])(?:[A-Za-z0-9_]*(?:password|secret|credential|token)[A-Za-z0-9_]*"
		+ @"|(?:[A-Za-z0-9]*[a-z0-9])?(?-i:Pass|Pwd|ApiKey)(?![a-z])[A-Za-z0-9_]*"
		+ @"|(?:[A-Za-z0-9_]*_)?(?:pass|pwd|api_?key|api-key)(?:_[A-Za-z0-9_]*)?(?![A-Za-z0-9]))";

	private const string _SCHEME_WORD = @"[A-Za-z][A-Za-z0-9]*";

	/// <summary>The value shapes V1 to V7 of a scheme-bearing header value, the credential last. The scheme and credential groups are named in each branch.</summary>
	private const string _VALUE_SHAPES =
		@"(?:" + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")[ ](?<credential>[^" + _QUOTE + @"\s{]+)" + _QUOTE + @"(?!" + _GAP + @"?[&+])"
		+ @"|" + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")[ ]*" + _QUOTE + _GAP_IMPLICIT + @"[&+]" + _GAP_IMPLICIT + @"(?<credential>" + _ARGUMENT + @")"
		+ @"|\$" + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")[ ]\{[ \t]*(?<credential>" + _ARGUMENT + @")[ \t]*\}" + _QUOTE
		+ @"|\$" + _QUOTE + @"\{[ \t]*(?<scheme>" + _NAME + @")[ \t]*\}[ ]\{[ \t]*(?<credential>" + _ARGUMENT + @")[ \t]*\}" + _QUOTE
		+ @"|(?:String\.Format|Format)[ \t]*\([ \t]*" + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")[ ]\{0\}" + _QUOTE + _COMMA + @"(?<credential>" + _ARGUMENT + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|(?:String\.Format|Format)[ \t]*\([ \t]*" + _QUOTE + @"\{0\}[ ]\{1\}" + _QUOTE + _COMMA + @"(?:" + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")" + _QUOTE + @"|(?<scheme>" + _NAME + @"))" + _COMMA + @"(?<credential>" + _ARGUMENT + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|(?:String\.)?Concat[ \t]*\([ \t]*" + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")[ ]" + _QUOTE + _COMMA + @"(?<credential>" + _ARGUMENT + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|(?<scheme>" + _NAME + @")[ \t]*&[ \t]*" + _QUOTE + @"[ ]" + _QUOTE + @"[ \t]*&[ \t]*(?<credential>" + _ARGUMENT + @")"
		+ @"|(?<![A-Za-z0-9_])(?:String\.)?Join[ \t]*\([ \t]*" + _QUOTE + @"[ ]" + _QUOTE + _COMMA + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")" + _QUOTE + _COMMA + @"(?<credential>" + _ARGUMENT + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|(?<credential>(?:" + _NAME + @"\.)*[A-Za-z0-9_]*Basic(?-i:(?![a-z]))[A-Za-z0-9_]*[ \t]*" + _GROUP + @"))";

	/// <summary>
	/// The last segment of a value name carries the word bearer, or the words access and token, as a whole token in any spelling (camelCase, PascalCase, snake_case, UPPER_CASE):
	/// the word starts the segment, follows an underscore or a lower-case letter or digit before a capital, and ends where no lower-case letter follows. The spelling groups
	/// are case-sensitive (the expression ignores case), so <c>unbearable</c> and <c>accessory</c> keep their reading. Used as a look-ahead at the start of the last segment.
	/// </summary>
	private const string _NOT_NAMED_FOR_A_BEARER =
		@"(?![A-Za-z0-9_]*?(?:(?<![A-Za-z0-9])|(?<=(?-i:[a-z0-9]))(?=(?-i:[A-Z])))(?:(?-i:bearer|Bearer|BEARER)|(?-i:access|Access|ACCESS)_?(?-i:token|Token|TOKEN))(?-i:(?![a-z])))";

	/// <summary>Alternative P of VB-007: a value that is a bare or dotted identifier whose last segment carries a credential word.</summary>
	private const string _CREDENTIAL_NAMED_VALUE = @"(?<credential>(?:" + _NAME + @"\.)*" + _NOT_NAMED_FOR_A_BEARER + _CREDENTIAL_NAME + @")(?![A-Za-z0-9_(])";

	// ------------------------------------------------------------------------------------------
	// The classes, VB-001 to VB-019
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// A half whose last name token is port (port, portNumber, portNo, PortNum, server_port, SERVER_PORT) is an address, not a credential half. The token
	/// starts the name, follows an underscore or follows a lower-case letter or digit before a capital, and ends where no lower-case letter follows, so <c>passport</c> and <c>portal</c>
	/// stay. Read as a look-behind at the end of the half; the spelling groups are case-sensitive.
	/// </summary>
	private const string _NOT_PORT_HALF =
		@"(?![A-Za-z0-9_])(?<!" + _PORT_TOKEN_START + @"(?-i:port|Port|PORT))(?<!" + _PORT_TOKEN_START + @"(?-i:port|Port|PORT)(?-i:(?![a-z]))_?(?-i:Number|number|NUMBER|Num|num|NUM|No|no|NO))";

	private const string _PORT_TOKEN_START = @"(?:(?<![A-Za-z0-9])|(?<=(?-i:[a-z0-9]))(?=(?-i:P)))";

	/// <summary>A half whose last member is a size, a count, a length or a port (with an optional text conversion) is a fingerprint or an address, not a credential half.</summary>
	private const string _NOT_SIZE_HALF = @"(?<!\.(?:Length|Count|Size|Port)(?:\.ToString\(\))?)" + _NOT_PORT_HALF;

	/// <summary>A half of a pair: the identifier-rooted argument form, without its quoted-literal alternative, so that this class and VB-015 stay mutually exclusive.</summary>
	private const string _PAIR_HALF = _DOTTED_WITH_GROUPS + _NOT_SIZE_HALF;

	/// <summary>The spellings of an assembled pair that read as one expression: a format with the two placeholders, an interpolation, a concatenation or a join with the colon as an element.</summary>
	private const string _PAIR_ASSEMBLY =
		@"(?:String\.Format|Format)[ \t]*\([ \t]*" + _QUOTE + @"\{0\}:\{1\}" + _QUOTE + _COMMA + @"(?<half1>" + _PAIR_HALF + @")" + _COMMA + @"(?<credential>" + _PAIR_HALF + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|\$" + _QUOTE + @"\{[ \t]*(?<half1>" + _PAIR_HALF + @")[ \t]*\}:\{[ \t]*(?<credential>" + _PAIR_HALF + @")[ \t]*\}" + _QUOTE
		+ @"|(?:String\.Concat|String\.Join|Concat|Join)[ \t]*\([ \t]*(?:" + _QUOTE + @":" + _QUOTE + @",[ \t]*)?(?<half1>" + _PAIR_HALF + @")[ \t]*,[ \t]*(?:" + _QUOTE + @":" + _QUOTE + @"[ \t]*,[ \t]*)?(?<credential>" + _PAIR_HALF + @")[ \t]*\)";

	/// <summary>
	/// How far the declaration look-arounds of the SDK credential type of VB-001 and VB-002 and of the first argument of VB-016 reach on each side of the
	/// candidate, in characters. The reach is bounded, as the other windows of the profile are, so a candidate never costs a scan of the whole file.
	/// </summary>
	private const string _DECLARATION_REACH_IN_CHARACTERS = "2048";

	private const string _DECLARATION_REACH_BEHIND = @"[\s\S]{0," + _DECLARATION_REACH_IN_CHARACTERS + @"}";

	private const string _DECLARATION_REACH_AHEAD = @"[\s\S]{0," + _DECLARATION_REACH_IN_CHARACTERS + @"}?";

	/// <summary>
	/// The qualifier of the SDK credential type of VB-001 and VB-002: a qualified spelling names the SDK and is read; an unqualified one is read only
	/// where no Class or Structure of that simple name is declared within <see cref="_DECLARATION_REACH_IN_CHARACTERS"/> characters before or after the use. The unqualified branch
	/// begins with a look-ahead on the type name, so the scans run only at a construction of that type.
	/// </summary>
	private const string _SDK_CREDENTIAL_TYPE_QUALIFIER =
		@"(?:Global\.(?:" + _NAME + @"\.)*|(?:" + _NAME + @"\.)+|(?=UsernamePasswordCredentials(?![A-Za-z0-9_]))(?<!" + _OWN_CREDENTIAL_TYPE_DECLARATION + _DECLARATION_REACH_BEHIND + @")(?!" + _DECLARATION_REACH_AHEAD + @"(?:Class|Structure)[ \t]+UsernamePasswordCredentials(?![A-Za-z0-9_])))";

	private const string _OWN_CREDENTIAL_TYPE_DECLARATION = @"\b(?:Class|Structure)[ \t]+UsernamePasswordCredentials(?![A-Za-z0-9_])";

	/// <summary>VB-001 · SdkUserNamePasswordCredentials</summary>
	[GeneratedRegex(
		@"\bNew[ \t]+" + _SDK_CREDENTIAL_TYPE_QUALIFIER + @"(?<construct>UsernamePasswordCredentials)(?![A-Za-z0-9_])[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + @"(?<credential>" + _ARGUMENT_NAMED + @")" + _GAP_IMPLICIT + @"\)",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex SdkUserNamePasswordCredentials();

	/// <summary>VB-002 · SdkLegacyRsapiCredentials</summary>
	[GeneratedRegex(
		@"\bNew[ \t]+" + _QUALIFIER + @"RSAPIClient[ \t]*\(" + _GAP_IMPLICIT + @"(?:" + _ARGUMENT_NAMED + @"|\bNew[ \t]+" + _QUALIFIER + _NAME + @"[ \t]*" + _GROUP + @")" + _COMMA
		+ @"New[ \t]+" + _SDK_CREDENTIAL_TYPE_QUALIFIER + @"(?<construct>UsernamePasswordCredentials)(?![A-Za-z0-9_])[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + @"(?<credential>" + _ARGUMENT_NAMED + @")" + _GAP_IMPLICIT + @"\)",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex SdkLegacyRsapiCredentials();

	/// <summary>VB-003 · CredentialCacheBasicScheme (a URI-first cache entry; the host-and-port overload is VB-018's)</summary>
	[GeneratedRegex(
		@"\.(?<construct>Add)[ \t]*\(" + _GAP_IMPLICIT + @"(?:\bNew[ \t]+" + _QUALIFIER + @"Uri[ \t]*" + _GROUP + @"|" + _ARGUMENT_NAMED + @")" + _COMMA
		+ @"(?:" + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")" + _QUOTE + @"|(?<scheme>" + _NAME + @"))" + _COMMA
		+ @"(?:\bNew[ \t]+" + _QUALIFIER + @"NetworkCredential[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + @"(?<credential>" + _ARGUMENT_NAMED + @")" + _GAP_IMPLICIT + @"\)|(?<credential>" + _ARGUMENT + @"))" + _GAP_IMPLICIT + @"\)",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex CredentialCacheBasicScheme();

	/// <summary>VB-004 · ThirdPartyBasicAuthenticatorType</summary>
	[GeneratedRegex(
		@"\bNew[ \t]+" + _QUALIFIER + @"(?<construct>[A-Za-z0-9_]*Basic[A-Za-z0-9_]*Authenticator[A-Za-z0-9_]*)[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + @"(?<credential>" + _ARGUMENT_NAMED + @")",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex ThirdPartyBasicAuthenticatorType();

	/// <summary>VB-005 · SchemeNamingExtensionMethod</summary>
	[GeneratedRegex(
		@"(?<=\.[A-Za-z0-9_]*)(?<construct>Basic[A-Za-z0-9_]*Auth[A-Za-z0-9_]*)[ \t]*\(" + _GAP_IMPLICIT + _ARGUMENT_NAMED + _COMMA + @"(?<credential>" + _ARGUMENT_NAMED + @")" + _GAP_IMPLICIT + @"\)",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex SchemeNamingExtensionMethod();

	/// <summary>VB-006 · AuthorizationHeaderValueScheme (the constructor, and the static factory with a literal-prefix scheme)</summary>
	[GeneratedRegex(
		@"\bNew[ \t]+" + _QUALIFIER + @"(?<construct>AuthenticationHeaderValue)(?![A-Za-z0-9_])[ \t]*\(" + _GAP_IMPLICIT + _NAMED_PREFIX + @"(?:" + _QUOTE + @"(?<scheme>" + _SCHEME_WORD + @")" + _QUOTE + @"|(?<scheme>" + _NAME + @"))" + _COMMA + @"(?<credential>" + _ARGUMENT_NAMED + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|\b(?<construct>AuthenticationHeaderValue)\.Parse[ \t]*\(" + _GAP_IMPLICIT + @"(?:" + _VALUE_SHAPES + @")" + _GAP_IMPLICIT + @"\)",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueScheme();

	/// <summary>VB-007 · AuthorizationHeaderWrite: W1, W2, W1' (single-string overload), K (named-constant key), the values V1 to V7, V8 (variable carried), and alternative P.</summary>
	[GeneratedRegex(
		@"(?:\.(?:DefaultRequestHeaders\.)?(?:Add|Set|Append|TryAddWithoutValidation|AddHeader|SetRequestHeader)[ \t]*\(" + _GAP_IMPLICIT + _NAMED_PREFIX + @"(?<header>" + _QUOTE + @"Authorization" + _QUOTE + @"|" + _NOT_THE_ENUMERATION_MEMBER + @"(?:" + _NAME + @"\.)*[A-Za-z0-9_]*Authoriz[A-Za-z0-9_]*)" + _COMMA + _NAMED_PREFIX + @"(?:" + _VALUE_SHAPES + @"|" + _CREDENTIAL_NAMED_VALUE + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|\.(?:DefaultRequestHeaders\.)?Add[ \t]*\(" + _GAP_IMPLICIT + @"(?<header>" + _QUOTE + @")Authorization:[ ](?<scheme>" + _SCHEME_WORD + @")[ ](?<credential>[^" + _QUOTE + @"\s]*)" + _QUOTE + @"[ \t]*\)"
		+ @"|\.(?:DefaultRequestHeaders\.)?Add[ \t]*\(" + _GAP_IMPLICIT + @"(?<header>" + _QUOTE + @")Authorization:[ ](?<scheme>" + _SCHEME_WORD + @")[ ]" + _QUOTE + _GAP_IMPLICIT + @"[&+]" + _GAP_IMPLICIT + @"(?<credential>" + _ARGUMENT + @")" + _GAP_IMPLICIT + @"\)"
		+ @"|\.Headers(?:\.Item)?[ \t]*\(" + _GAP_IMPLICIT + @"(?<header>" + _QUOTE + @"Authorization" + _QUOTE + @"|" + _NOT_THE_ENUMERATION_MEMBER + @"(?:" + _NAME + @"\.)*[A-Za-z0-9_]*Authoriz[A-Za-z0-9_]*)" + _GAP_IMPLICIT + @"\)" + _GAP_IMPLICIT + @"=" + _GAP_IMPLICIT + @"(?:" + _VALUE_SHAPES + @"|" + _CREDENTIAL_NAMED_VALUE + @")"
		+ @"|=(?<=\b(?<v>" + _NAME + @")(?:[ \t]+As[ \t]+[A-Za-z_][A-Za-z0-9_.]*)?[ \t]*=)" + _GAP_IMPLICIT + @"(?:" + _VALUE_SHAPES + @")" + _LINE_WINDOW_FIVE
		+ @"\.(?:DefaultRequestHeaders\.)?(?:Add|Set|AddHeader|SetRequestHeader)[ \t]*\([ \t]*(?<header>" + _QUOTE + @"Authorization" + _QUOTE + @")[ \t]*,[ \t]*\k<v>[ \t]*\))",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderWrite();

	/// <summary>VB-008 · HttpRequestHeaderEnumAuthorization: the indexer, Add and Set (with or without the leading Headers, so a With block is read, and with in-order named arguments), and V8 (a variable carried).</summary>
	[GeneratedRegex(
		@"(?:\.Headers[ \t]*\(" + _GAP_IMPLICIT + @"(?:[A-Za-z0-9_.]*\.)?(?<construct>HttpRequestHeader\.Authorization)" + _GAP_IMPLICIT + @"\)" + _GAP_IMPLICIT + @"=" + _GAP_IMPLICIT
		+ @"|\.(?:Headers\.)?(?:Add|Set)[ \t]*\(" + _GAP_IMPLICIT + _NAMED_PREFIX + @"(?:[A-Za-z0-9_.]*\.)?(?<construct>HttpRequestHeader\.Authorization)" + _COMMA + _NAMED_PREFIX + @")(?:" + _VALUE_SHAPES + @")"
		+ @"|=(?<=\b(?<v>" + _NAME + @")(?:[ \t]+As[ \t]+[A-Za-z_][A-Za-z0-9_.]*)?[ \t]*=)" + _GAP_IMPLICIT + @"(?:" + _VALUE_SHAPES + @")" + _LINE_WINDOW_FIVE
		+ @"(?=\.)(?:\.Headers[ \t]*\(" + _GAP_IMPLICIT + @"(?:[A-Za-z0-9_.]*\.)?(?<construct>HttpRequestHeader\.Authorization)" + _GAP_IMPLICIT + @"\)" + _GAP_IMPLICIT + @"=" + _GAP_IMPLICIT + @"\k<v>(?![A-Za-z0-9_.(])"
		+ @"|\.(?:Headers\.)?(?:Add|Set)[ \t]*\(" + _GAP_IMPLICIT + _NAMED_PREFIX + @"(?:[A-Za-z0-9_.]*\.)?(?<construct>HttpRequestHeader\.Authorization)" + _COMMA + _NAMED_PREFIX + @"\k<v>" + _GAP_IMPLICIT + @"\))",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex HttpRequestHeaderEnumAuthorization();

	/// <summary>VB-009 · WcfBasicTransportCredential, the password assignment an optional required-window alternative (never an optional lazy window).</summary>
	[GeneratedRegex(
		@"\.?ClientCredentialType[ \t]*=[ \t]*(?:[A-Za-z0-9_.]*\.)?(?<construct>HttpClientCredentialType\.Basic)(?![A-Za-z0-9_])"
		+ _LINE_WINDOW_FIVE + @"\.(?:Client)?Credentials\.UserName\.Password[ \t]*=[ \t]*(?<credential>" + _ARGUMENT + @")"
		+ @"|\.?ClientCredentialType[ \t]*=[ \t]*(?:[A-Za-z0-9_.]*\.)?(?<construct>HttpClientCredentialType\.Basic)(?![A-Za-z0-9_])",
		OPTIONS, matchTimeoutMilliseconds: TIMEOUT_IN_MILLISECONDS)]
	public static partial Regex WcfBasicTransportCredential();
}
