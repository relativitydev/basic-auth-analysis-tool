using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The structural half of the C# analyzer.
///
/// It registers the eighteen pattern classes the catalog declares. The expressions of CS-011..CS-018,
/// <see cref="CSharpPatternExpressions"/>, live in a second partial file,
/// <c>EmbeddedLanguageAnalyzers.CSharp.SecondHalf.cs</c>, since <see cref="CSharpAnalyzer"/>
/// itself is <c>file</c>-scoped and cannot be split. The fourteen preselection anchors are declared
/// here in full: the anchor collection is a property of the catalog's whole vocabulary, and the second
/// file extends the SAME registered analyzer rather than registering a second one.
///
/// The sixteen recommendation templates of the catalog - CS-001..CS-009 and
/// CS-012..CS-018 - are declared for BOTH flows. The template text lives in a third
/// partial file, <c>EmbeddedLanguageAnalyzers.CSharp.Templates.cs</c>, as
/// <see cref="CSharpRecommendationTemplates"/>'s constants, because <see cref="CSharpAnalyzer"/> is
/// <c>file</c>-scoped and cannot be split. CS-011 and CS-019 return null for both flows: both are
/// base Low and routed to manual review, per the catalog's "Classes without a template".
///
/// The per-language fallback slot
/// <see cref="CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow"/> carries one entry per
/// flow, the catalog's two "Generic fallback content for CSharp" texts, also encoded in
/// <see cref="CSharpRecommendationTemplates"/>. `RecommendationTemplateProvider` returning null for
/// a High or Medium class still means "no catalog template for this class"; the declared fallback
/// text for the finding's flow is what a finding taking that route renders. In practice no `CSharp`
/// High or Medium class reaches the route at all, because every one of them has an entry; the
/// declared texts exist for a class added without one.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddCSharpAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new CSharpAnalyzer());
	}
}

/// <summary>
/// Every expression the `CSharp` analyzer hands over, generated at build time. The analyzer contract requires this:
/// the generator needs a constant pattern and produces a method, not data, and every expression
/// declares <see cref="RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS"/> per
/// The catalog's "every expression carries its own match timeout".
/// </summary>
internal static partial class CSharpPatternExpressions
{
	/// <summary>
	/// The longest run of expression characters an indexer suffix may hold, as regular-expression text.
	/// </summary>
	private const string _INDEXER_CONTENT_MAXIMUM_LENGTH = "120";

	/// <summary>
	/// One indexer suffix of <see cref="_ARGUMENT_EXPRESSION"/>: a bracket pair that follows the identifier
	/// or its call directly, on the same line, holding either an ordinary quoted literal or a short run of
	/// expression characters with no bracket, comma, quote, parenthesis or semicolon. The two contents open
	/// on disjoint characters, and both the length bound and the closing bracket are required, so the
	/// suffix stays linear. An indexer inside an indexer, a top-level comma and a bracket that opens an
	/// argument are not read.
	/// </summary>
	private const string _INDEXER_SUFFIX =
		@"(?>\[(?:[ \t]*""[^""\r\n]*""[ \t]*|[^\[\](),""';\r\n]{1," + _INDEXER_CONTENT_MAXIMUM_LENGTH + @"})\])";

	/// <summary>
	/// An argument that is itself a construction, <c>new T(...)</c>, with at most one further level of
	/// nested parentheses inside it. Deeper nesting is not read. The alternative opens on <c>new</c>.
	/// </summary>
	private const string _CONSTRUCTED_OBJECT_ARGUMENT =
		@"new\s+[A-Za-z_][A-Za-z0-9_.]*\s*\((?:[^()]|\([^()]*\))*\)";

	/// <summary>
	/// The catalog's "How a credential capture group is written": the group covers the
	/// credential argument and nothing else. A bare identifier or a literal alone misses the
	/// ordinary forms real code uses - a member-access chain (<c>options.Password</c>,
	/// <c>settings.Credentials.User</c>) or that chain ending in one invocation, itself carrying at
	/// most one further level of nested parentheses (<c>Convert.ToBase64String(bytes)</c>). "No
	/// commas at the top level" keeps the expression from swallowing a sibling constructor argument;
	/// the one permitted nested paren pair is what lets a single inner call through without opening
	/// this into a general expression grammar. One or two indexer suffixes
	/// (<see cref="_INDEXER_SUFFIX"/>) may follow the chain or its call, so a value read from
	/// configuration or split from a string is an argument too; an indexer argument is code and never a
	/// literal. It stays linear by construction - no quantifier here
	/// can restart at a position another has already tried - so the per-expression match-bound
	/// timeout remains a safety net rather than something this shape leans on.
	/// </summary>
	private const string _ARGUMENT_EXPRESSION = _QUOTED_LITERAL_ARGUMENT + @"|" + _IDENTIFIER_ROOTED_ARGUMENT;

	/// <summary>The quoted-literal alternative of <see cref="_ARGUMENT_EXPRESSION"/>: an ordinary quoted literal on one line.</summary>
	private const string _QUOTED_LITERAL_ARGUMENT = @"""[^""\r\n]*""";

	/// <summary>
	/// The identifier-rooted alternative of <see cref="_ARGUMENT_EXPRESSION"/>, which is the argument form
	/// without its quoted literal: a dotted identifier, an optional call and up to two indexer suffixes. The
	/// halves of a colon-joined pair take this form, so that a literal, which is how an empty half is
	/// written, can never be one of them and the pair stays disjoint from the empty-half class.
	/// </summary>
	private const string _IDENTIFIER_ROOTED_ARGUMENT =
		@"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*(?:\s*\(\s*(?:[^(),]|\([^()]*\))*\s*\))?(?:" + _INDEXER_SUFFIX + @"){0,2}";

	/// <summary>
	/// How a created type may be written in front of its name: an optional <c>global::</c> and then zero or
	/// more <c>qualifier.</c> segments, so the fully qualified, the <c>global::</c> and the partial namespace
	/// spellings all read the same. Where a class anchors on the type name, the anchor group is the name
	/// itself, after this prefix.
	/// </summary>
	private const string _TYPE_QUALIFIER = @"(?:global::)?(?:[A-Za-z_][A-Za-z0-9_]*\.)*";

	/// <summary>
	/// A constructor's argument text, the parentheses included, with at most one further level of nested
	/// parentheses. Atomic: the text can only end at the first closing parenthesis of the outer level, so
	/// nothing is ever given back.
	/// </summary>
	private const string _CONSTRUCTOR_ARGUMENT_TEXT = @"\((?>(?:[^()]|\([^()]*\))*)\)";

	/// <summary>
	/// The creation of the credential type, up to its opening parenthesis: <c>new</c>, then the type written
	/// plain or qualified.
	/// </summary>
	private const string _NETWORK_CREDENTIAL_CREATION = @"new\s+" + _TYPE_QUALIFIER + @"NetworkCredential";

	/// <summary>
	/// An address argument that is itself a creation: <c>new T(...)</c>, with the type plain or qualified,
	/// or the target-typed <c>new(...)</c>. The alternative opens on <c>new</c>.
	/// </summary>
	private const string _ADDRESS_CONSTRUCTION_ARGUMENT =
		@"new(?:\s+" + _TYPE_QUALIFIER + _IDENTIFIER + @")?\s*" + _CONSTRUCTOR_ARGUMENT_TEXT;

	/// <summary>
	/// What follows the declared type of a target-typed creation, <c>Type variable = new</c>: the variable and
	/// the <c>new</c> keyword. The type name stands directly in front of the variable, which is what puts it
	/// in the text, and each alternative that uses this opens on that type name as a literal, so it is tried
	/// only at the words that can begin it.
	/// </summary>
	private const string _TARGET_TYPED_CREATION_SUFFIX = @"\s+" + _IDENTIFIER + @"\s*=\s*new";

	/// <summary>
	/// CS-007's own allow-list, in the catalog: "the credential construction must sit
	/// within an HTTP client or handler construction." Factored out because CS-007's direct-assignment
	/// alternative needs the identical list the object-initializer alternative already names.
	/// </summary>
	private const string _HTTP_CLIENT_OR_HANDLER_TYPE_NAMES =
		@"HttpClientHandler|SocketsHttpHandler|WinHttpHandler|HttpClient";

	/// <summary>
	/// The non-HTTP sinks CS-019 reads when the credential it carries is assembled across statements: the
	/// two types of its closed set that expose a settable or constructor-supplied <c>Credentials</c>
	/// member and are built with <c>new</c>. <c>FtpWebRequest</c> is obtained by a cast, and
	/// <c>DirectoryEntry</c> and <c>DirectorySearcher</c> take their credential as text, so they never
	/// carry a <c>NetworkCredential</c> object through a variable.
	/// </summary>
	private const string _MAIL_OR_PROXY_SINK_TYPE_NAMES = @"SmtpClient|WebProxy";

	/// <summary>
	/// CS-016's own shape as a fragment CS-019 can extend, beginning after the <c>new</c> keyword: a
	/// credential built from an object initializer carrying the user name, then a password assigned
	/// through a variable within the bounded five-line window. That variable is captured as <c>cv</c>
	/// from the password assignment's receiver, exactly as CS-016 reads the same two statements (it too
	/// does not tie the receiver to the initializer's declaration), and the text that follows this
	/// fragment then requires the sink to bind that same variable. Reading the name there, rather than
	/// from the declaration in front of the initializer, keeps the fragment opening on <c>new</c>, which is
	/// tried only where the keyword is, instead of on an identifier, which is tried at every word of every
	/// file. The window is required, never inside an optional group.
	/// </summary>
	private const string _CREDENTIAL_ASSEMBLED_ACROSS_STATEMENTS =
		@"\s+" + _NETWORK_CREDENTIAL_INITIALIZER_OPENING + @"[^{}]*?UserName\s*=[^{}]*?\}\s*;(?:[^\n]*\n){0,5}?[^\n]*?\b(?<cv>" + _IDENTIFIER + @")\.Password\s*=\s*(?<credential>[^;\r\n]*?)\s*;";

	/// <summary>
	/// The opening of a credential built from an object initializer, from the type name to the brace: the type
	/// written plain or qualified, then constructor parentheses that hold no argument, which may stand before
	/// the initializer. CS-016 and the across-statements shape of CS-019 share it, so both read the same
	/// spellings.
	/// </summary>
	private const string _NETWORK_CREDENTIAL_INITIALIZER_OPENING = _TYPE_QUALIFIER + @"NetworkCredential(?:\s*\(\s*\))?\s*\{";

	/// <summary>The bounded five-line window between two statements: required wherever it is used, never inside an optional group.</summary>
	private const string _FIVE_LINE_WINDOW = @"(?:[^\n]*\n){0,5}?[^\n]*?";

	/// <summary>
	/// A mail client or a proxy written plain or qualified, the sink type captured as <c>construct</c> after
	/// any qualifier, which is where CS-019 anchors.
	/// </summary>
	private const string _MAIL_OR_PROXY_SINK_CREATION = _TYPE_QUALIFIER + @"(?<construct>" + _MAIL_OR_PROXY_SINK_TYPE_NAMES + @")";

	/// <summary>The credential pair of a created credential: both arguments in the shared argument form, captured as <c>user</c> and <c>credential</c>.</summary>
	private const string _CREDENTIAL_ARGUMENT_PAIR =
		@"\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)";

	/// <summary>
	/// A sink's initializer that carries a credential created in place, from its opening brace to its closing
	/// one: <c>{ Credentials = new NetworkCredential(user, password) }</c>. The creation of the credential is
	/// the sink's own span, so the credential is reported once, by the sink.
	/// </summary>
	private const string _INITIALIZER_CARRYING_A_NEW_CREDENTIAL =
		@"\s*\{[^{}]*?\bCredentials\s*=\s*" + _NETWORK_CREDENTIAL_CREATION + _CREDENTIAL_ARGUMENT_PAIR + @"[^{}]*?\}";

	/// <summary>
	/// The credential token inside a FULLY LITERAL <c>"&lt;Scheme&gt; &lt;token&gt;"</c>
	/// string value - CS-006's and CS-012's shared fully literal value shape, where the whole header
	/// value is one string literal with no concatenation operator and no interpolation hole at all.
	/// The first character excludes the closing quote and whitespace, so the token
	/// cannot start empty and cannot swallow the single space that separates it from the scheme word;
	/// the remainder runs to the literal's own closing quote. The alternative built from this constant
	/// always opens on the literal's own opening quote - a required literal anchor - so it cannot
	/// overlap the sibling concatenation alternative, which requires a closing quote immediately after
	/// the scheme word (optionally preceded by whitespace) rather than a token there.
	///
	/// Both character classes also exclude <c>{</c>, so this
	/// token never crosses an interpolation hole's own opening brace: paired with the optional
	/// <c>$</c> the fully literal alternative accepts on its opening quote, a hole-free
	/// interpolated value such as <c>$"Basic ZGVtbzpodW50ZXIy"</c> reaches this shape, while a value
	/// carrying a hole, such as <c>$"Basic {credential}"</c>, still declines it here and is reached
	/// only through the sibling interpolated-hole alternative, which the alternation tries first.
	/// </summary>
	private const string _FULLY_LITERAL_CREDENTIAL_TOKEN = @"[^""\s{][^""\r\n{]*";

	/// <summary>An identifier, used where a scheme word or a variable name is captured.</summary>
	private const string _IDENTIFIER = @"[A-Za-z_][A-Za-z0-9_]*";

	/// <summary>
	/// The methods CS-006 reads as writing a header: the framework collection's <c>Add</c> and the
	/// <c>AddHeader</c> a REST client library's request object carries. Both take the header name and
	/// then its value.
	/// </summary>
	private const string _HEADER_WRITE_METHOD_NAMES = @"(?:Add|AddHeader)";

	/// <summary>
	/// A header value written as <c>string.Format("&lt;Scheme&gt; {0}", credential)</c>. The scheme word is
	/// captured from inside the format literal so the same scheme verdict applies as for every other CS-006
	/// value shape, and the credential accepts <see cref="_ARGUMENT_EXPRESSION"/>, so a call to a project's
	/// own encoding helper is captured whole. The alternative opens on the literal <c>string.Format</c> or
	/// <c>String.Format</c>.
	/// </summary>
	private const string _FORMAT_SCHEME_VALUE =
		@"(?:string|String)\.Format\s*\(\s*""(?<scheme>" + _IDENTIFIER + @")\s+\{0\}""\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)";

	/// <summary>
	/// The value shapes CS-006 also reads when the header value is first stored in a variable and the
	/// variable is passed to the header write: the format shape, an interpolated literal-prefix scheme and
	/// a concatenated literal-prefix scheme. The whole alternation is grouped, and each of its own
	/// alternatives opens on a literal.
	/// </summary>
	private const string _VARIABLE_CARRIED_SCHEME_VALUE =
		@"(?:" + _FORMAT_SCHEME_VALUE
		+ @"|\$""(?<scheme>" + _IDENTIFIER + @")\s*\{\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\}"""
		+ @"|""(?<scheme>" + _IDENTIFIER + @")\s*""\s*\+\s*(?<credential>" + _ARGUMENT_EXPRESSION + @"))";

	/// <summary>
	/// The line-comment, block-comment and whitespace gap CS-015 allows between the tokens of its argument
	/// list, so a constructor call whose arguments each sit on their own line, with or without a comment,
	/// reads the same as one written on a single line. Each comment form is atomic, so a run of comment
	/// characters cannot be split in more than one way.
	/// </summary>
	private const string _ARGUMENT_LIST_GAP = @"(?:\s|(?>//[^\n]*)|(?>/\*[\s\S]*?\*/))*";

	/// <summary>
	/// CS-015's third argument. Its spelling does not decide the class, only its presence does, so it may
	/// be any expression with no comma, semicolon or unbalanced parenthesis at its top level: plain text, a
	/// quoted literal (which may itself hold a comma), or a parenthesized group one level deep inside which
	/// commas are allowed. The whole run is atomic, and its three alternatives open on disjoint characters.
	/// </summary>
	private const string _LOOSE_ARGUMENT_EXPRESSION =
		@"(?>(?:[^(),;""]|""[^""\r\n]*""|\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*\))+)";

	/// <summary>
	/// Contract element 1. Feeds <c>FileSymbolTable</c>'s file-scoped constant collection.
	///
	/// Declares the policy match timeout, which
	/// <c>FileSymbolTable.TryCollect</c> also brackets with the engine's own match-bound watch.
	/// </summary>
	[GeneratedRegex(
		@"\b(?:(?:public|private|internal|protected|static)\s+)*const\s+string\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>[^""\r\n]*)""",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	/// <summary>
	/// CS-001. Both the direct construction <c>new UsernamePasswordCredentials(user, pass)</c> and
	/// the C# 9 target-typed form <c>UsernamePasswordCredentials credentials = new(user, pass);</c>,
	/// catalogued from the language per the catalog.
	/// Both arguments accept <see cref="_ARGUMENT_EXPRESSION"/>.
	/// </summary>
	[GeneratedRegex(
		@"\b(?<construct>UsernamePasswordCredentials)(?:\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)|\s+[A-Za-z_][A-Za-z0-9_]*\s*=\s*new\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SdkUserNamePasswordCredentialsConstruction();

	/// <summary>
	/// CS-002. Anchored on the NESTED credential type, per the catalog - the same token CS-001
	/// anchors on, which is what makes <c>AnchorPositionsAreIdentical</c> the right subsumption
	/// condition between the two. The outer client's first argument, and both halves of the nested
	/// credential pair, all accept <see cref="_ARGUMENT_EXPRESSION"/>, so variable and literal arguments
	/// are both read. The first argument's <see cref="_ARGUMENT_EXPRESSION"/> sits inside a non-capturing
	/// group of its own, because that constant carries a top-level <c>|</c>.
	///
	/// The nested credential may be written qualified, with
	/// <c>global::</c> or with a partial namespace (<see cref="_TYPE_QUALIFIER"/>), and the anchor group stays
	/// the type name itself after the qualifier, so this class still subsumes CS-001 at identical anchor
	/// positions. The client may also be declared with the target-typed creation, <c>RSAPIClient client =
	/// new(first, new UsernamePasswordCredentials(user, password))</c>, where the declared type stands
	/// directly in front of the variable. A nested credential that is itself target-typed, a <c>new(...)</c>
	/// with no type name, names nothing to anchor on and is declared unsupported, as is a type renamed by a
	/// <c>using</c> alias.
	/// </summary>
	[GeneratedRegex(
		@"\bRSAPIClient(?:\s*\(|" + _TARGET_TYPED_CREATION_SUFFIX + @"\s*\()\s*(?:" + _CONSTRUCTED_OBJECT_ARGUMENT + @"|" + _ARGUMENT_EXPRESSION + @")\s*,\s*new\s+" + _TYPE_QUALIFIER + @"(?<construct>UsernamePasswordCredentials)\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex SdkLegacyRsapiCredentialsConstruction();

	/// <summary>
	/// CS-003. Anchored on the <c>Add</c> invocation. The scheme argument is captured as either a
	/// literal or an identifier so file-scoped resolution can read either form. The
	/// credential pair's own two arguments accept <see cref="_ARGUMENT_EXPRESSION"/>. The
	/// <c>new Uri(...)</c> argument accepts one level of nested parentheses, so a call such as
	/// <c>new Uri(x.ToString())</c> is read, while a sibling constructor argument at the top level is not. The
	/// enclosing group stays linear (no quantifier can restart at a position another has already tried),
	/// so the per-expression match-bound timeout remains a safety net rather than something this shape leans on.
	///
	/// The address takes the shared argument form or any creation with at most one further level of nested parentheses, written
	/// plain, qualified or target-typed (<see cref="_ADDRESS_CONSTRUCTION_ARGUMENT"/>). The constructed
	/// credential may be written qualified, with <c>global::</c> or with a partial namespace
	/// (<see cref="_NETWORK_CREDENTIAL_CREATION"/>). Declared unsupported: a type renamed by a <c>using</c>
	/// alias, a credential written as the target-typed <c>new(user, password)</c> in the entry's last
	/// argument, a scheme written as a qualified constant, and an entry added by a collection initializer,
	/// which has no <c>Add</c> call to anchor on.
	/// </summary>
	[GeneratedRegex(
		@"\.(?<construct>Add)\s*\(\s*(?:" + _ADDRESS_CONSTRUCTION_ARGUMENT + @"|" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?:(?:" + _ARGUMENT_EXPRESSION + @")\s*,\s*)?(?<scheme>""[^""\r\n]*""|[A-Za-z_][A-Za-z0-9_]*)\s*,\s*" + _NETWORK_CREDENTIAL_CREATION + @"\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialCacheBasicSchemeConstruction();

	/// <summary>
	/// CS-004. Closed by SHAPE, never by enumeration: a type name carrying both the
	/// scheme token and an authenticator-role token, constructed with a credential pair. Arity is
	/// unconstrained, so the match ends on a comma or a closing parenthesis rather than requiring exactly
	/// two arguments. The prefix before <c>Basic</c> is optional, so the bare name
	/// <c>BasicAuthenticator</c> matches - the leading <c>\b</c> still
	/// requires the match to begin at an identifier boundary, so a name only carrying <c>Basic</c>
	/// without the authenticator-role suffix is unaffected and still declines. Both arguments accept
	/// <see cref="_ARGUMENT_EXPRESSION"/>.
	///
	/// The type name is read wherever it sits, so the qualified spellings are read. The target-typed
	/// declaration <c>HttpBasicAuthenticator authenticator = new(user, password)</c> is read too; the
	/// call-shaped alternative does not apply to it because the parenthesis does not follow the
	/// type name: the declared type stands directly in front of the variable, and the match then reads the
	/// <c>new(</c> that follows. A target-typed creation anywhere the type name is not in front of the
	/// variable, and a type renamed by a <c>using</c> alias, are declared unsupported.
	/// </summary>
	[GeneratedRegex(
		@"\b(?<construct>(?:[A-Za-z_][A-Za-z0-9_]*)?Basic[A-Za-z0-9_]*Authenticator)(?:\s*\(|" + _TARGET_TYPED_CREATION_SUFFIX + @"\s*\()\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*[,)]",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ThirdPartyBasicAuthenticatorTypeConstruction();

	/// <summary>
	/// CS-005. Anchored on the type name. The scheme argument is the first constructor argument; the
	/// credential argument accepts <see cref="_ARGUMENT_EXPRESSION"/>.
	///
	/// The type may be written qualified, with <c>global::</c> or with a partial namespace
	/// (<see cref="_TYPE_QUALIFIER"/>), and may be declared with the target-typed creation,
	/// <c>AuthenticationHeaderValue header = new(scheme, credential)</c>, where the declared type stands
	/// directly in front of the variable. The target-typed assignment to the header property names no type
	/// and would anchor this class on a property name that other types also carry, so it is declared
	/// unsupported, with a type renamed by a <c>using</c> alias and a scheme written as a qualified constant.
	/// </summary>
	[GeneratedRegex(
		@"(?:\bnew\s+" + _TYPE_QUALIFIER + @"(?<construct>AuthenticationHeaderValue)|\b(?<construct>AuthenticationHeaderValue)" + _TARGET_TYPED_CREATION_SUFFIX + @")\s*\(\s*(?<scheme>""[^""\r\n]*""|[A-Za-z_][A-Za-z0-9_]*)\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderValueSchemeConstruction();

	/// <summary>
	/// CS-006. The anchor group captures the WHOLE <c>"Authorization"</c> literal, quotes included,
	/// so its resolved offset is the literal's opening quote per the catalog -
	/// the anchor's zone then reads <c>Delimiter</c>, which counts as code, rather than
	/// <c>StringBody</c>, which would demote a High-base class to Low on every match. The rule
	/// requires a write (an <c>Add</c> call or an indexer assignment), never a read, and the value
	/// must carry a scheme token immediately at its own opening. Six value shapes: an interpolated
	/// literal-prefix scheme, a concatenated literal-prefix scheme (its own
	/// credential accepts <see cref="_ARGUMENT_EXPRESSION"/>, grouped), a FULLY LITERAL single string
	/// carrying both the scheme and the
	/// credential with no concatenation at all (e.g. <c>"Basic ZGVtbzpodW50ZXIy"</c> - a
	/// PLAIN <c>"..."</c> literal, optionally opened with a leading <c>$</c>: <c>{</c> is excluded
	/// from <see cref="_FULLY_LITERAL_CREDENTIAL_TOKEN"/>, so a hole-free
	/// interpolated value such as <c>$"Basic ZGVtbzpodW50ZXIy"</c> reaches this shape while a value
	/// carrying a hole still reaches only the interpolated literal-prefix-scheme alternative above; a
	/// verbatim, raw or interpolated-raw literal opens on a different token neither alternative
	/// itself expects and is therefore not reached, a declared limit
	/// - see <see cref="LanguageIdentification.DeclaredUnsupportedCoverage"/>'s entry for it), a
	/// PARTLY LITERAL single string followed by a concatenated expression (e.g.
	/// <c>"Basic abc" + suffix</c>: the pure fully literal alternative above declines this shape
	/// through its own trailing <c>(?!\s*\+)</c>, and this alternative's credential group spans from
	/// the token through the literal's closing quote and the concatenation into the appended
	/// expression, so the group's captured extent always crosses into code and the literal-credential
	/// signal reads false even though the finding still fires; also accepts an
	/// optional leading <c>$</c>, so a hole-free interpolated literal concatenated with an
	/// expression is found with the literal signal false), an interpolated
	/// NAMED-CONSTANT scheme in its own hole (<c>$"{SCHEME} {credential}"</c>), and a concatenated
	/// named-constant scheme (<c>SCHEME + " " + credential</c>) - the interpolated and concatenated
	/// named-constant shapes are what make the scheme verdict (Confirms/Suppresses/ProposesMedium)
	/// reachable through an identifier, since file-scoped resolution is not limited to one;
	/// the fully literal shape reaches the same verdict machinery directly, because the scheme
	/// group's captured extent is <c>StringBody</c> either way. The credential inside EITHER
	/// interpolation hole, and inside the literal-prefix concatenation shape, accepts
	/// <see cref="_ARGUMENT_EXPRESSION"/>; the fully literal shape's credential is
	/// <see cref="_FULLY_LITERAL_CREDENTIAL_TOKEN"/>, which cannot be mistaken for the literal-prefix
	/// concatenation shape because that shape requires a closing quote immediately after the scheme
	/// word (optionally preceded by whitespace) where this shape requires a non-quote, non-whitespace
	/// token character instead.
	///
	/// The header write may also be a request object's
	/// <c>AddHeader</c>, not only <c>Add</c> (<see cref="_HEADER_WRITE_METHOD_NAMES"/>). A seventh value
	/// shape reads <c>string.Format("Basic {0}", credential)</c> written in the header write itself
	/// (<see cref="_FORMAT_SCHEME_VALUE"/>). And a second top-level alternative reads the same value stored
	/// first in a variable, within the usual bounded five-line window, and passed to the header write
	/// through that same variable (<see cref="_VARIABLE_CARRIED_SCHEME_VALUE"/>); the variable is captured
	/// once and read back, so a header write passing a different variable does not match. The window is
	/// required, never inside an optional group. The anchor stays the header literal, which sits inside
	/// the match in both alternatives. The variable alternative opens on the assignment's own <c>=</c>
	/// and reads the variable's name backwards in a lookbehind, rather than opening on an identifier:
	/// an alternative that opens on an identifier is tried at every word of every file, and this one is
	/// tried only where an assignment is.
	/// </summary>
	[GeneratedRegex(
		@"(?:\.(?:DefaultRequestHeaders\.)?" + _HEADER_WRITE_METHOD_NAMES + @"\s*\(\s*(?<header>""Authorization"")\s*,\s*|\[\s*(?<header>""Authorization"")\s*\]\s*=\s*)(?:\$""(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\}""|""(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s*""\s*\+\s*(?<credential>(?:" + _ARGUMENT_EXPRESSION + @"))|\$?""(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s+(?<credential>" + _FULLY_LITERAL_CREDENTIAL_TOKEN + @")""(?!\s*\+)|\$?""(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s+(?<credential>" + _FULLY_LITERAL_CREDENTIAL_TOKEN + @"""\s*\+\s*(?:" + _ARGUMENT_EXPRESSION + @"))|\$""\{\s*(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s*\}[^{}""]*\{\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\}""|(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s*\+\s*""\s*""\s*\+\s*(?<credential>[A-Za-z_][A-Za-z0-9_]*)|" + _FORMAT_SCHEME_VALUE + @")"
			+ @"|=(?<=\b(?<v>" + _IDENTIFIER + @")\s*=)\s*" + _VARIABLE_CARRIED_SCHEME_VALUE + @"\s*;(?:[^\n]*\n){0,5}?[^\n]*?\.(?:DefaultRequestHeaders\.)?" + _HEADER_WRITE_METHOD_NAMES + @"\s*\(\s*(?<header>""Authorization"")\s*,\s*\k<v>\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex AuthorizationHeaderStringLiteralConstruction();

	/// <summary>
	/// CS-007. Anchored on the property assignment. The credential construction must be the
	/// assignment's right-hand side directly - <c>Credentials = CredentialCache.DefaultCredentials</c>
	/// and <c>UseDefaultCredentials = true</c> cannot match this shape - and it must sit inside an
	/// object initializer for a type from the small, extensible HTTP client/handler allow-list
	/// the catalog describes (seeded here from the catalog's own archetype, <c>HttpClientHandler</c>).
	/// Both arguments accept <see cref="_ARGUMENT_EXPRESSION"/>.
	///
	/// A second alternative reads the direct assignment <c>handler.Credentials = new NetworkCredential(...)</c>,
	/// where the credential construction, not a bare variable,
	/// is the assignment's right-hand side. The allow-list variable is captured once as <c>v</c> and
	/// read back with <c>\k&lt;v&gt;</c>, so the trailing property assignment must name the SAME
	/// variable the allow-list type was constructed into - a construction assigned to one variable
	/// followed by a property write through a different one does not match. The bounded five-line
	/// window between the two statements is REQUIRED, never wrapped in an optional group: a lazy
	/// line-bounded window placed inside an optional group makes the platform's
	/// source-generated regex return a malformed, repeating match. <c>construct</c>, <c>user</c> and
	/// <c>credential</c> are declared again in this alternative, which the platform allows for two
	/// alternatives of the same expression.
	///
	/// The handler or client type and the constructed
	/// credential may be written qualified, with <c>global::</c> or with a partial namespace
	/// (<see cref="_TYPE_QUALIFIER"/>). The initializer alternative accepts constructor parentheses, empty or
	/// with arguments, before the brace (<see cref="_CONSTRUCTOR_ARGUMENT_TEXT"/>), and the target-typed
	/// declarations <c>T x = new() { ... }</c> and <c>T x = new(...) { ... }</c>; the property-write alternative
	/// also reads <c>T x = new(...);</c> followed by the write. In each target-typed form the declared type
	/// stands directly in front of the variable, and the alternative opens on that type name. A handler type
	/// renamed by a <c>using</c> alias, a target-typed creation where the type name is not in front of the
	/// variable and a credential assigned from a variable stay unsupported, and the last is CS-008's.
	/// </summary>
	[GeneratedRegex(
		@"(?:\bnew\s+" + _TYPE_QUALIFIER + @"(?:" + _HTTP_CLIENT_OR_HANDLER_TYPE_NAMES + @")(?:\s*" + _CONSTRUCTOR_ARGUMENT_TEXT + @")?|\b(?:" + _HTTP_CLIENT_OR_HANDLER_TYPE_NAMES + @")" + _TARGET_TYPED_CREATION_SUFFIX + @"\s*" + _CONSTRUCTOR_ARGUMENT_TEXT + @")\s*\{[^{}]*?(?<construct>Credentials)\s*=\s*" + _NETWORK_CREDENTIAL_CREATION + @"\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)[^{}]*?\}"
		+ @"|(?:\b(?<v>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*new\s+" + _TYPE_QUALIFIER + @"(?:" + _HTTP_CLIENT_OR_HANDLER_TYPE_NAMES + @")|\b(?:" + _HTTP_CLIENT_OR_HANDLER_TYPE_NAMES + @")\s+(?<v>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*new)\s*\([^;]*\)\s*;(?:[^\n]*\n){0,5}?[^\n]*?\k<v>\.(?<construct>Credentials)\s*=\s*" + _NETWORK_CREDENTIAL_CREATION + @"\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NetworkCredentialOnHttpHandlerConstruction();

	/// <summary>
	/// CS-008. Every <c>NetworkCredential</c> construction, bare. It matches every occurrence CS-007
	/// and CS-019 also reach - that overlap is the point: the subsumption relation, not this
	/// expression, is what keeps a bound credential from being reported twice. Both arguments accept
	/// <see cref="_ARGUMENT_EXPRESSION"/>.
	///
	/// The type may be written qualified, with <c>global::</c> or with a partial namespace
	/// (<see cref="_TYPE_QUALIFIER"/>), and may be declared with the target-typed creation,
	/// <c>NetworkCredential credential = new(user, password)</c>, where the declared type stands directly in
	/// front of the variable. The anchor group is the type name after any qualifier, so the subsumption by
	/// CS-003, CS-007 and CS-019 still holds at overlapping spans. Declared unsupported: a type renamed by a
	/// <c>using</c> alias, a target-typed creation where the type name is not in front of the variable, and a
	/// credential created by an initializer that carries both halves.
	/// </summary>
	[GeneratedRegex(
		@"(?:\bnew\s+" + _TYPE_QUALIFIER + @"(?<construct>NetworkCredential)|\b(?<construct>NetworkCredential)" + _TARGET_TYPED_CREATION_SUFFIX + @")\s*\(\s*(?<user>" + _ARGUMENT_EXPRESSION + @")\s*,\s*(?<credential>" + _ARGUMENT_EXPRESSION + @")\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NetworkCredentialConstruction();

	/// <summary>
	/// The credential half of CS-009's pair: the whole second half, captured as one group, in the
	/// identifier-rooted argument form. Atomic, so nothing is given back into its indexer suffixes and the
	/// capture always covers the full half.
	/// </summary>
	private const string _CS_009_CREDENTIAL_HALF = @"(?>(?<credential>" + _IDENTIFIER_ROOTED_ARGUMENT + @"))";

	/// <summary>
	/// CS-009's interpolated, formatted and joined spellings of a colon-joined pair, each with both halves in
	/// the identifier-rooted argument form (<see cref="_IDENTIFIER_ROOTED_ARGUMENT"/>), so a member access, a
	/// call or an indexer is a half and a quoted literal never is. An empty half is the literal <c>""</c>, so
	/// excluding the literal is what keeps this class and the empty-half class disjoint. Every alternative
	/// opens on a literal, so none is tried at a word that cannot begin it. They are one alternation and the
	/// caller groups it.
	/// </summary>
	private const string _CS_009_LITERAL_OPENED_PAIR_SPELLINGS =
		@"\$""\{\s*" + _IDENTIFIER_ROOTED_ARGUMENT + @"\s*\}\s*:\s*\{\s*" + _CS_009_CREDENTIAL_HALF + @"\s*\}"""
		+ @"|(?:string|String)\.Format\s*\(\s*""\{0\}:\{1\}""\s*,\s*" + _IDENTIFIER_ROOTED_ARGUMENT + @"\s*,\s*" + _CS_009_CREDENTIAL_HALF + @"\s*\)"
		+ @"|(?:string|String)\.Join\s*\(\s*"":""\s*,\s*" + _IDENTIFIER_ROOTED_ARGUMENT + @"\s*,\s*" + _CS_009_CREDENTIAL_HALF + @"\s*\)";

	/// <summary>
	/// CS-009's concatenated spelling when the pair is a statement of its own, anchored on the operator and
	/// the colon literal that follows it, with the left half read BACKWARDS in a lookbehind: the half is an
	/// identifier-rooted argument that no identifier character precedes and that ends, past any blanks,
	/// directly before the operator. The alternative therefore opens on a plus sign, which is tried only
	/// where one is, and not on an identifier, which is tried at every word of every file and, since a half may carry a call, would cost the whole class a call's arguments at each of them. The match begins at the
	/// operator; the credential and the encoder are inside it, and the left half is not read for its value.
	/// </summary>
	private const string _CS_009_CONCATENATED_PAIR_AFTER_ITS_LEFT_HALF =
		@"(?<=(?<![A-Za-z0-9_])" + _IDENTIFIER_ROOTED_ARGUMENT + @"\s*)\+\s*"":""\s*\+\s*" + _CS_009_CREDENTIAL_HALF;

	/// <summary>
	/// CS-009's concatenated spelling inside the encoder call, read forwards: the left half must open inside
	/// the window that follows the call's parenthesis, which a lookbehind could not guarantee, since a
	/// call that has just closed would then be read as the left half of a pair written after it.
	/// </summary>
	private const string _CS_009_CONCATENATED_PAIR_INSIDE_THE_WINDOW =
		@"(?<![A-Za-z0-9_])" + _IDENTIFIER_ROOTED_ARGUMENT + @"\s*\+\s*"":""\s*\+\s*" + _CS_009_CREDENTIAL_HALF;

	/// <summary>
	/// CS-009's pair written INSIDE the encoder call, with both halves non-empty. The encoder is the anchor
	/// and the pair must open within 200 characters of the call's parenthesis with no statement end between
	/// them. The four spellings are those of the separate-statement form, with the same identifier-rooted
	/// halves. A pair with an empty half opens on none of them and stays the empty-half class's.
	/// </summary>
	private const string _CS_009_PAIR_INSIDE_THE_ENCODER_CALL =
		@"(?<encoder>Convert\.ToBase64String)\s*\([^;]{0," + _INLINE_PAIR_ENCODER_WINDOW_MAXIMUM_LENGTH + @"}?(?:" + _CS_009_LITERAL_OPENED_PAIR_SPELLINGS + @"|" + _CS_009_CONCATENATED_PAIR_INSIDE_THE_WINDOW + @")";

	/// <summary>
	/// CS-009. Four spellings of the pair - an interpolated pair, a concatenated pair,
	/// <c>string.Format("{0}:{1}", a, b)</c> and <c>string.Join(":", a, b)</c> - each requiring BOTH
	/// halves non-empty (the empty-half idiom is CS-018, registered in the second analyzer file), followed
	/// within a bounded five-line window by the Base64 encoder.
	///
	/// Each half of the pair takes the identifier-rooted argument form, so the halves may be a member access
	/// (<c>passwordInput.Password</c>), a call (<c>Console.ReadLine()</c>) or an indexer, in all four spellings,
	/// both when the pair is a statement of its own and when it is written inside the encoder call. The
	/// credential position covers the whole second half. A quoted literal is not a half: the empty half is
	/// the literal <c>""</c>, so admitting one would overlap the empty-half class; with it excluded the two
	/// classes stay mutually exclusive by construction and no subsumption triple is needed. A half that is an
	/// arithmetic or conditional expression, or a call nested deeper than the argument form's one level of
	/// parentheses, is not a half.
	/// </summary>
	[GeneratedRegex(
		@"(?:" + _CS_009_LITERAL_OPENED_PAIR_SPELLINGS + @"|" + _CS_009_CONCATENATED_PAIR_AFTER_ITS_LEFT_HALF + @")(?:[^\n]*\n){0,5}?[^\n]*?(?<encoder>Convert\.ToBase64String)\s*\("
			+ @"|" + _CS_009_PAIR_INSIDE_THE_ENCODER_CALL,
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex Base64UserPasswordPairConstruction();

	/// <summary>
	/// The factory call that obtains a file-transfer request: <c>Create</c> called through <c>WebRequest</c> or
	/// through <c>FtpWebRequest</c>, either written plain or qualified, with its argument list read to the end of
	/// the statement. The argument list is bounded by the statement's own semicolon, never past it.
	/// </summary>
	private const string _FILE_TRANSFER_REQUEST_FACTORY_CALL =
		_TYPE_QUALIFIER + @"(?:Ftp)?WebRequest\.Create\s*\([^;]*\)";

	/// <summary>
	/// CS-019. The closed BCL set of non-HTTP credential sinks the catalog declares, in the shapes the
	/// closed set takes.
	///
	/// The FIRST alternative is the property-assignment shape:
	/// <c>SmtpClient</c> and <c>WebProxy</c> both expose a settable <c>Credentials</c> property,
	/// followed within a bounded five-line window by the credential assignment. Its construct
	/// alternation also names <c>FtpWebRequest</c>, <c>DirectoryEntry</c> and <c>DirectorySearcher</c>,
	/// which do not occur in this shape.
	///
	/// The SECOND alternative covers <c>FtpWebRequest</c>, which has no public constructor and is
	/// obtained through the factory <c>Create</c>, called through <c>WebRequest</c> or through
	/// <c>FtpWebRequest</c>, with the derived type given by a cast in front of the call or by <c>as</c> after
	/// it (<see cref="_FILE_TRANSFER_REQUEST_FACTORY_CALL"/>); the type name is read literally, so an
	/// <c>as HttpWebRequest</c> is not read. The four spellings share the one prefix of the <c>=</c>
	/// alternative: the receiving variable is
	/// captured once (<c>var x = ...</c> and <c>FtpWebRequest x = ...</c> both satisfy "the identifier
	/// immediately before <c>=</c>") and read back by that SAME variable's own <c>.Credentials</c>
	/// write within the bounded window, mirroring CS-007's own direct-assignment alternative and its
	/// back-reference.
	///
	/// The THIRD alternative covers <c>DirectoryEntry</c>, which has no <c>Credentials</c> property -
	/// its credential is passed to the constructor, so the shape is credential-first: a named
	/// <c>NetworkCredential</c> is captured, and the bounded window looks forward for a
	/// <c>DirectoryEntry</c> construction whose argument list references that SAME variable by
	/// back-reference (bounded to the one statement, never past its own closing <c>)</c>).
	/// <c>DirectorySearcher</c> takes no credential of its own - the catalog reaches it only through
	/// the root <c>DirectoryEntry</c> it is built from, inline or across statements - so this same
	/// alternative covers both forms with no separate shape.
	///
	/// Every window here is REQUIRED, never wrapped in an optional group: a
	/// lazy line-bounded window placed inside an optional group makes the platform's source-generated
	/// regex return a malformed, repeating match. Each alternative's span necessarily contains (Ftp: at
	/// its end; DirectoryEntry: at its start) a <see cref="NetworkCredentialConstruction"/> match, which
	/// is what makes <c>MatchSpansOverlap</c> the right subsumption condition against CS-008 for all
	/// three shapes. Every credential pair accepts <see cref="_ARGUMENT_EXPRESSION"/>.
	///
	/// The FOURTH and FIFTH alternatives read a credential assembled across statements
	/// (the shape CS-016 reads: an initializer carrying the user name, then a later password assignment
	/// through the same variable, <see cref="_CREDENTIAL_ASSEMBLED_ACROSS_STATEMENTS"/>) and then bound to
	/// a mail client or a proxy (<see cref="_MAIL_OR_PROXY_SINK_TYPE_NAMES"/>). The fourth reads the
	/// credential first and the sink at the end, either built with the variable in its own initializer;
	/// the fifth reads the sink first and the binding through <c>.Credentials</c> last. In both the
	/// credential variable is captured once and read back, so a sink that binds a different variable
	/// does not match, and the span of each contains the span of CS-016's own match, which is what lets
	/// <c>MatchSpansOverlap</c> declare CS-019 as subsuming CS-016 too. The same shape bound to an HTTP
	/// handler, or to no sink within the window, matches neither alternative and is still CS-016 alone.
	/// The first, fourth and fifth alternatives all open on the <c>new</c> keyword, so they sit in one
	/// group under a single <c>\bnew</c>, which is tried only where the keyword is.
	///
	/// Every alternative reads
	/// the sink, the constructed credential and the file-transfer cast with its factory written plain or
	/// qualified (<see cref="_TYPE_QUALIFIER"/>). An alternative in the <c>new</c> group reads a credential
	/// built in the sink's own initializer, <c>new Sink(arguments) { Credentials = new NetworkCredential(user,
	/// password) }</c>, with or without constructor parentheses, for the mail client and the proxy
	/// (<see cref="_INITIALIZER_CARRYING_A_NEW_CREDENTIAL"/>). The sink decides, so the finding is this class at Low, and the existing subsumption of CS-008 and CS-016 removes
	/// the credential inside it at overlapping spans. A top-level alternative reads
	/// the target-typed declaration, <c>SmtpClient mailer = new(...)</c>, with the declared type directly in
	/// front of the variable, in its initializer form, followed by the property binding, and followed by a
	/// credential assembled across statements and bound later; the credential-first alternative reads the
	/// target-typed sink in the same way. The target-typed alternative opens on the sink's type name as a
	/// literal. Declared unsupported: a type renamed by a <c>using</c> alias and a target-typed creation where
	/// the type name is not in front of the variable.
	/// </summary>
	[GeneratedRegex(
		@"\bnew(?:\s+" + _TYPE_QUALIFIER + @"(?<construct>SmtpClient|FtpWebRequest|DirectoryEntry|DirectorySearcher|WebProxy)\s*\([^;]*\)\s*;" + _FIVE_LINE_WINDOW + @"\.Credentials\s*=\s*" + _NETWORK_CREDENTIAL_CREATION + _CREDENTIAL_ARGUMENT_PAIR
		+ @"|\s+" + _MAIL_OR_PROXY_SINK_CREATION + @"\s*\([^;]*\)\s*;" + _FIVE_LINE_WINDOW + @"\bnew" + _CREDENTIAL_ASSEMBLED_ACROSS_STATEMENTS + _FIVE_LINE_WINDOW + @"\.Credentials\s*=\s*\k<cv>\b"
		+ @"|" + _CREDENTIAL_ASSEMBLED_ACROSS_STATEMENTS + _FIVE_LINE_WINDOW + @"(?:\bnew\s+" + _MAIL_OR_PROXY_SINK_CREATION + @"|\b(?<construct>" + _MAIL_OR_PROXY_SINK_TYPE_NAMES + @")" + _TARGET_TYPED_CREATION_SUFFIX + @")\b[^;]{0,300}?\bCredentials\s*=\s*\k<cv>\b"
		+ @"|(?<=\b(?<c>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*new)\s+" + _TYPE_QUALIFIER + @"NetworkCredential" + _CREDENTIAL_ARGUMENT_PAIR + @"\s*;" + _FIVE_LINE_WINDOW + @"\bnew\s+" + _TYPE_QUALIFIER + @"(?<construct>DirectoryEntry)\s*\([^;]*?\b\k<c>\b[^;]*?\)"
		+ @"|\s+" + _MAIL_OR_PROXY_SINK_CREATION + @"\b(?:\s*" + _CONSTRUCTOR_ARGUMENT_TEXT + @")?" + _INITIALIZER_CARRYING_A_NEW_CREDENTIAL + @")"
		+ @"|=(?<=\b(?<v>[A-Za-z_][A-Za-z0-9_]*)\s*=)\s*(?:\(\s*" + _TYPE_QUALIFIER + @"(?<construct>FtpWebRequest)\s*\)\s*" + _FILE_TRANSFER_REQUEST_FACTORY_CALL + @"|" + _FILE_TRANSFER_REQUEST_FACTORY_CALL + @"\s*as\s+" + _TYPE_QUALIFIER + @"(?<construct>FtpWebRequest))\s*;" + _FIVE_LINE_WINDOW + @"\b\k<v>\.Credentials\s*=\s*" + _NETWORK_CREDENTIAL_CREATION + _CREDENTIAL_ARGUMENT_PAIR
		+ @"|\b(?<construct>" + _MAIL_OR_PROXY_SINK_TYPE_NAMES + @")" + _TARGET_TYPED_CREATION_SUFFIX + @"(?:\s*\([^;]*\)\s*;" + _FIVE_LINE_WINDOW + @"\.Credentials\s*=\s*" + _NETWORK_CREDENTIAL_CREATION + _CREDENTIAL_ARGUMENT_PAIR
		+ @"|\s*" + _CONSTRUCTOR_ARGUMENT_TEXT + _INITIALIZER_CARRYING_A_NEW_CREDENTIAL
		+ @"|\s*\([^;]*\)\s*;" + _FIVE_LINE_WINDOW + @"\bnew" + _CREDENTIAL_ASSEMBLED_ACROSS_STATEMENTS + _FIVE_LINE_WINDOW + @"\.Credentials\s*=\s*\k<cv>\b)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex NonHttpCredentialSinkConstruction();

	/// <summary>
	/// The generated-code header content marker, <c>AnyZone</c>: the
	/// header opens with the language's own line-comment token, so it lands in
	/// <see cref="MigrateAuth.Cli.Lexical.LexicalZone.Comment"/>, which an <c>AnyZone</c> marker must
	/// still reach.
	/// </summary>
	[GeneratedRegex(
		@"^\s*//\s*<auto-generated",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedHeaderContentMarker();

	/// <summary>
	/// The <c>GeneratedCode</c> attribute content marker, <c>CodeOnly</c>:
	/// the attribute written inside a comment or a string does not demote.
	/// </summary>
	[GeneratedRegex(
		@"\[(?:[A-Za-z_][A-Za-z0-9_.]*\.)?GeneratedCode(?:Attribute)?\s*\(",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedCodeAttributeContentMarker();

	/// <summary>
	/// The test-attribute content marker, <c>CodeOnly</c>: five common
	/// xUnit/MSTest/NUnit attribute tokens.
	/// </summary>
	[GeneratedRegex(
		@"\[(?:Fact|Test|TestMethod|TestFixture|Theory)\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TestAttributeContentMarker();
}

/// <summary>
/// The `CSharp` analyzer. File-local per the registration pattern every embedded language follows,
/// so no engine type can name it and no special case can be written against it.
/// </summary>
file sealed class CSharpAnalyzer : ILanguageAnalyzer
{
	private const string _PROFILE_NAME = "CSharp";
	private const string _LANGUAGE_NAME = "CSharp";
	private const string _FILE_EXTENSION = ".cs";

	private const string _CS_001 = "CS-001";
	private const string _CS_002 = "CS-002";
	private const string _CS_003 = "CS-003";
	private const string _CS_004 = "CS-004";
	private const string _CS_005 = "CS-005";
	private const string _CS_006 = "CS-006";
	private const string _CS_007 = "CS-007";
	private const string _CS_008 = "CS-008";
	private const string _CS_009 = "CS-009";
	private const string _CS_011 = "CS-011";
	private const string _CS_012 = "CS-012";
	private const string _CS_013 = "CS-013";
	private const string _CS_014 = "CS-014";
	private const string _CS_015 = "CS-015";
	private const string _CS_016 = "CS-016";
	private const string _CS_017 = "CS-017";
	private const string _CS_018 = "CS-018";
	private const string _CS_019 = "CS-019";

	/// <summary>Names from the catalog's own class-heading mapping.</summary>
	private const string _CS_001_DISPLAY_NAME = "SdkUserNamePasswordCredentials";

	private const string _CS_002_DISPLAY_NAME = "SdkLegacyRsapiCredentials";
	private const string _CS_003_DISPLAY_NAME = "CredentialCacheBasicScheme";
	private const string _CS_004_DISPLAY_NAME = "ThirdPartyBasicAuthenticatorType";
	private const string _CS_005_DISPLAY_NAME = "AuthorizationHeaderValueScheme";
	private const string _CS_006_DISPLAY_NAME = "AuthorizationHeaderStringLiteral";
	private const string _CS_007_DISPLAY_NAME = "NetworkCredentialOnHttpHandler";
	private const string _CS_008_DISPLAY_NAME = "NetworkCredentialConstruction";
	private const string _CS_009_DISPLAY_NAME = "Base64UserPasswordPair";
	private const string _CS_011_DISPLAY_NAME = "AuthenticationHelperDefinition";
	private const string _CS_012_DISPLAY_NAME = "HttpRequestHeaderEnumAuthorization";
	private const string _CS_013_DISPLAY_NAME = "WcfBasicTransportCredential";
	private const string _CS_014_DISPLAY_NAME = "SchemeNamingExtensionMethod";
	private const string _CS_015_DISPLAY_NAME = "SdkConstructorWithUserAndPassword";
	private const string _CS_016_DISPLAY_NAME = "CredentialAssembledAcrossStatements";
	private const string _CS_017_DISPLAY_NAME = "SdkSettingsCredentialProperties";
	private const string _CS_018_DISPLAY_NAME = "TokenCarriedUnderBasicScheme";
	private const string _CS_019_DISPLAY_NAME = "NonHttpCredentialSink";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _SCHEME_CAPTURE_GROUP = "scheme";
	private const string _ENCODER_CAPTURE_GROUP = "encoder";
	private const string _HEADER_CAPTURE_GROUP = "header";
	private const string _DECLARATION_CAPTURE_GROUP = "declaration";

	private const string _EXPECTED_SCHEME_TOKEN = "Basic";

	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";

	private const string _PLAIN_STRING_QUOTE = "\"";
	private const string _VERBATIM_STRING_OPENING = "@\"";
	private const string _INTERPOLATED_STRING_OPENING = "$\"";
	private const string _INTERPOLATED_VERBATIM_OPENING_DOLLAR_FIRST = "$@\"";
	private const string _INTERPOLATED_VERBATIM_OPENING_AT_FIRST = "@$\"";
	private const string _RAW_STRING_QUOTE = "\"\"\"";
	private const string _CHARACTER_QUOTE = "'";
	private const string _INTERPOLATION_OPENING = "{";
	private const string _INTERPOLATION_CLOSING = "}";
	private const string _INTERPOLATION_NESTING_OPENING = "{";
	private const char _ESCAPE_CHARACTER = '\\';

	private const string _TEST_FILE_SUFFIX_TESTS = ".Tests.cs";
	private const string _TEST_FILE_SUFFIX_TEST = ".Test.cs";
	private const string _TEST_FILE_SUFFIX_SPEC = ".Spec.cs";
	private const string _GENERATED_FILE_SUFFIX_G = ".g.cs";
	private const string _GENERATED_FILE_SUFFIX_DESIGNER = ".Designer.cs";
	private const string _GENERATED_FILE_SUFFIX_GENERATED = ".generated.cs";
	private const string _TEST_DOUBLE_PREFIX_FAKE = "Fake";
	private const string _TEST_DOUBLE_PREFIX_MOCK = "Mock";
	private const string _TEST_DOUBLE_PREFIX_STUB = "Stub";
	private const string _SAMPLE_FILE_PREFIX_SAMPLE = "Sample";
	private const string _SAMPLE_FILE_PREFIX_EXAMPLE = "Example";
	private const string _TEST_DOUBLE_SUFFIX_MOCK = "Mock.cs";
	private const string _TEST_DOUBLE_SUFFIX_STUB = "Stub.cs";
	private const string _TEST_DOUBLE_SUFFIX_FAKE = "Fake.cs";

	private const string _TEST_FOLDER_NAME_TEST = "test";
	private const string _TEST_FOLDER_NAME_TESTS = "tests";
	private const string _TEST_FOLDER_NAME_SPEC = "spec";
	private const string _TEST_FOLDER_NAME_FIXTURES = "fixtures";
	private const string _SAMPLE_FOLDER_NAME_SAMPLE = "sample";
	private const string _SAMPLE_FOLDER_NAME_SAMPLES = "samples";
	private const string _SAMPLE_FOLDER_NAME_EXAMPLE = "example";
	private const string _SAMPLE_FOLDER_NAME_EXAMPLES = "examples";
	private const string _VENDORED_FOLDER_NAME_VENDOR = "vendor";
	private const string _VENDORED_FOLDER_NAME_THIRD_PARTY = "third-party";
	private const string _DEPLOYMENT_FOLDER_NAME = "Deployment";
	private const string _TEST_FOLDER_SUFFIX_TEST = ".Test";
	private const string _TEST_FOLDER_SUFFIX_TESTS = ".Tests";

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = BuildDemotionSignals();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } =
		BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = BuildSubsumptionRelation();

	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	/// <summary>
	/// Built once, following the reference analyzer's own pattern: sixteen entries, one per catalog
	/// class, whose credential provenance ends with part 2 for Client Credentials, sourced
	/// from <see cref="CSharpRecommendationTemplates"/>.
	/// </summary>
	private static readonly FrozenDictionary<string, RecommendationTemplate> _clientCredentialsTemplatesByPatternClass =
		BuildTemplates(CSharpRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION);

	/// <summary>
	/// The same sixteen classes for Authorization Code: part 1 is identical and the credential
	/// provenance ends with part 2 for Authorization Code, both variants.
	/// </summary>
	private static readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplatesByPatternClass =
		BuildTemplates(CSharpRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION);

	private static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = CSharpPatternExpressions.ConstantDeclaration(),
			LineCommentRules = BuildLineCommentRules(),
			BlockCommentRules = BuildBlockCommentRules(),
			QuoteRules = BuildQuoteRules()
		};

		return profile;
	}

	private static ImmutableArray<LineCommentRule> BuildLineCommentRules()
	{
		ImmutableArray<LineCommentRule> rules = [new LineCommentRule { Opening = _LINE_COMMENT_OPENING }];

		return rules;
	}

	private static ImmutableArray<BlockCommentRule> BuildBlockCommentRules()
	{
		ImmutableArray<BlockCommentRule> rules =
		[
			new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
		];

		return rules;
	}

	/// <summary>
	/// Catalog element 1. Seven quote rules: the interpolated and verbatim combinations
	/// each declare interpolation and newline handling on the <see cref="QuoteRule"/> itself rather
	/// than through a <see cref="LiteralPrefixRule"/>, because a prefix cannot carry
	/// them, and the raw form's interpolated variants are the declared profile gap that this
	/// leaves. Every opening is a distinct token, so the engine's own longest-opening-first ordering
	/// (<see cref="LexicalEnginePolicy"/>) is what tells <c>$@"</c> and <c>@$"</c> apart from <c>$"</c>
	/// and <c>@"</c>, and <c>"""</c> apart from a plain <c>"</c>.
	/// </summary>
	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		ImmutableArray<QuoteRule> rules =
		[
			BuildPlainStringRule(),
			BuildVerbatimStringRule(),
			BuildInterpolatedStringRule(),
			BuildInterpolatedVerbatimStringRule(_INTERPOLATED_VERBATIM_OPENING_DOLLAR_FIRST),
			BuildInterpolatedVerbatimStringRule(_INTERPOLATED_VERBATIM_OPENING_AT_FIRST),
			BuildRawStringRule(),
			BuildCharacterLiteralRule()
		];

		return rules;
	}

	/// <summary>Backslash escape, no newline - the catalog's "String" row.</summary>
	private static QuoteRule BuildPlainStringRule()
	{
		QuoteRule rule = new()
		{
			Opening = _PLAIN_STRING_QUOTE,
			Closing = _PLAIN_STRING_QUOTE,
			NewlinePolicy = NewlinePolicy.Forbidden,
			EscapeCharacter = _ESCAPE_CHARACTER
		};

		return rule;
	}

	/// <summary>Doubled close escapes, newlines allowed, no backslash escape - the "Verbatim" row.</summary>
	private static QuoteRule BuildVerbatimStringRule()
	{
		QuoteRule rule = new()
		{
			Opening = _VERBATIM_STRING_OPENING,
			Closing = _PLAIN_STRING_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			DoubledClosingEscapes = true
		};

		return rule;
	}

	/// <summary>Holes on <c>{</c>/<c>}</c> counting nested braces, backslash escape, no newline - the "Interpolated" row.</summary>
	private static QuoteRule BuildInterpolatedStringRule()
	{
		QuoteRule rule = new()
		{
			Opening = _INTERPOLATED_STRING_OPENING,
			Closing = _PLAIN_STRING_QUOTE,
			NewlinePolicy = NewlinePolicy.Forbidden,
			EscapeCharacter = _ESCAPE_CHARACTER,
			InterpolationOpening = _INTERPOLATION_OPENING,
			InterpolationClosing = _INTERPOLATION_CLOSING,
			InterpolationNestingOpening = _INTERPOLATION_NESTING_OPENING,
			DoubledInterpolationOpeningIsLiteral = true
		};

		return rule;
	}

	/// <summary>Both orders of the "Interpolated verbatim" row: doubled close, newlines allowed, holes.</summary>
	private static QuoteRule BuildInterpolatedVerbatimStringRule(string opening)
	{
		QuoteRule rule = new()
		{
			Opening = opening,
			Closing = _PLAIN_STRING_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			DoubledClosingEscapes = true,
			InterpolationOpening = _INTERPOLATION_OPENING,
			InterpolationClosing = _INTERPOLATION_CLOSING,
			InterpolationNestingOpening = _INTERPOLATION_NESTING_OPENING,
			DoubledInterpolationOpeningIsLiteral = true
		};

		return rule;
	}

	/// <summary>Variable-length delimiter, newlines as content, no escape - the "Raw" row.</summary>
	private static QuoteRule BuildRawStringRule()
	{
		QuoteRule rule = new()
		{
			Opening = _RAW_STRING_QUOTE,
			Closing = _RAW_STRING_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			DelimiterLength = DelimiterLength.VariableWithAtLeastEqualClosing
		};

		return rule;
	}

	/// <summary>The "Character" row.</summary>
	private static QuoteRule BuildCharacterLiteralRule()
	{
		QuoteRule rule = new()
		{
			Opening = _CHARACTER_QUOTE,
			Closing = _CHARACTER_QUOTE,
			NewlinePolicy = NewlinePolicy.Forbidden,
			EscapeCharacter = _ESCAPE_CHARACTER
		};

		return rule;
	}

	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _FILE_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = BuildDeclaredUnsupportedCoverage()
		};

		return identification;
	}

	/// <summary>
	/// Written for the client reading the report, not for
	/// whoever reads this source: each <see cref="DeclaredUnsupportedCoverage.Construct"/> names
	/// what the tool does not detect or cannot tell in plain words, and each
	/// <see cref="DeclaredUnsupportedCoverage.Reason"/> says why, and where useful what the reader
	/// should do about it. No internal design term, class identifier or section reference belongs
	/// in either field - these entries are restated from the catalog's declared scope. The two classes supported at Low only (CS-011, CS-019) are a
	/// DIFFERENT statement - they ARE recognized, just excluded from the recall denominator - so
	/// they do not appear here. Split into five grouped helpers, each under the method-length
	/// limit, and concatenated.
	/// </summary>
	private static FrozenSet<DeclaredUnsupportedCoverage> BuildDeclaredUnsupportedCoverage()
	{
		IEnumerable<DeclaredUnsupportedCoverage> entries = UnsupportedCoverageForCredentialFlowAndScope()
			.Concat(UnsupportedCoverageForResolutionAndConstruction())
			.Concat(UnsupportedCoverageForCreationAndSchemeForms())
			.Concat(UnsupportedCoverageForEngineLimits())
			.Concat(UnsupportedCoverageForOutOfLanguageScope());

		return entries.ToFrozenSet();
	}

	/// <summary>Who is authenticating, to what, and by what architecture - the three flow-shaped gaps.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForCredentialFlowAndScope()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "confirming whether a piece of Basic Authentication code is actually calling RelativityOne or a different server",
				Reason = "the tool cannot determine, from the code in a single file, what server a request is actually sent to; such findings are still reported, with a note that the finding may not apply if the destination is not RelativityOne - review each one to confirm the destination before acting on it"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a service that checks incoming Basic Authentication from its own callers, rather than sending it out to another server",
				Reason = "the tool cannot always tell which direction a credential is flowing in from the code in one file; a service that only accepts Basic Authentication from its callers has nothing to migrate to Relativity's token-based authentication. The tool excludes the most common inbound form, but a service that decodes the header itself may still be reported as if it were sending credentials out, so confirm the direction before acting on a finding like this"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a project's own custom authentication helper or wrapper written in a form the tool does not recognize",
				Reason = "the tool matches known code patterns and cannot see inside every custom wrapper. It does detect the common case where a helper method or class visibly builds or holds a credential, but not every form a custom wrapper can take - review any authentication helper code by hand"
			}
		];

		return entries;
	}

	/// <summary>What the engine cannot join, resolve or see the origin of - nine gaps.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForResolutionAndConstruction()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "credentials supplied through dependency injection, configuration binding, or another mechanism that connects code across multiple files",
				Reason = "the tool examines each file on its own and cannot trace how a credential registered in one file reaches the code that consumes it in another file"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "credential objects created through reflection or another dynamically computed expression",
				Reason = "the type or method name is never written out as plain text in the source in this case, so the tool has nothing to match against"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "credentials stored in configuration files rather than in source code",
				Reason = "configuration files are not source code in any language the tool scans; they are a different format and are not part of what it reads - review configuration files separately"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a user name and password written directly into a URL, such as \"https://user:password@host\"",
				Reason = "the tool does not detect this form at all. This is a real exposure independent of the OAuth2 migration and should be searched for and removed regardless of which authentication scheme is in use"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "confirmation that a matched Relativity SDK credential type actually comes from the Relativity SDK, rather than a different library that happens to use the same type name",
				Reason = "the tool matches on the type name as written in the code, not on which package or library it was installed from; a different library using an identically named type would be matched the same way - confirm the source of the type where this matters"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "the retired RSAPI client obtained through a generic factory method that does not show a credential",
				Reason = "the tool only recognizes this retired client when a credential is visibly built together with it, so as not to incorrectly flag code that is not a problem. A client obtained through a factory with no visible credential in that same code is not detected, even though it is still worth reviewing by hand"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a credential, scheme name, or header value written using an unusual quoting style - such as a verbatim or raw string literal - rather than an ordinary quoted string",
				Reason = "the tool's matching only recognizes the ordinary quoted-string style. Text written with a different quoting style is not matched, even though the rest of the file around it is still read correctly, apart from the forms listed under preprocessor blocks"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "an interpolated string that writes a literal brace as a doubled opening brace",
				Reason = "the tool reads the doubled opening brace as the start of an interpolated expression followed by an inner brace, not as a literal brace. Text between a balanced pair is read as code, so a construct written there can be reported as if it were live code, and an unpaired doubled brace can make the tool read a later stretch of the file as the inside of that expression until it finds the end. The loss is a false finding or a demoted one in the lines near such a literal"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a user name and password joined by a colon and converted to Base64 by the project's own helper method, rather than by the standard library's Base64 conversion, or by that conversion called without its type name through a \"using static\" directive",
				Reason = "the tool recognizes the standard library's conversion only, written with its type name, so a colon-joined pair converted by a differently named helper, or by the conversion reached without its type name, is not matched on its own. A header value that names the scheme, written next to that conversion, is still reported - review any code that builds an Authorization header through its own encoding helper"
			}
		];

		return entries;
	}

	/// <summary>
	/// The spellings of a created type and of a scheme the tool does not read - five gaps: a renamed type, the
	/// target-typed creation away from its type name, a cache entry or a credential built by an initializer, a
	/// scheme named by a qualified or directly concatenated constant, and a password control reached through an
	/// operand the signal does not read.
	/// </summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForCreationAndSchemeForms()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "a credential type, client, authenticator, header type or SDK entry point that the file renames with a \"using\" alias, or an enumeration member or conversion reached through \"using static\" without its type",
				Reason = "the type name appears only on the \"using\" line, and following a rename means resolving what a name refers to, which the tool does not do. A use written under the alias, or a member written without its type, is not recognized, so a file that does nothing else is missed - search for such aliases and review those files by hand"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a credential or client created with the shorthand \"new(...)\" where the type name does not stand in front of the variable (an assignment to an existing variable, a return, an argument, a property initializer, a credential written as the last argument of a credential-cache entry)",
				Reason = "without the type name nothing in the text marks the call as one of the recognized kinds. The form with the declared type directly in front of the variable, \"Type x = new(...)\", is recognized. The shorthand assignment to the header property, \"Authorization = new(\"Basic\", credential)\", is left out for the same reason: reading it would match on a property name that other types also carry"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a credential cache entry added by a collection initializer, and a credential created by an object initializer that carries both the user name and the password in the one initializer",
				Reason = "the first has no \"Add\" call for the tool to match on, so only the credential inside it is reported, as a plain credential construction. The second is read by no rule: the rule for a credential completed in a later statement needs the password there, and the construction rule needs constructor arguments. Review such code by hand"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a scheme named by a constant written with a type or namespace qualifier, as the scheme argument of a credential-cache entry, of an authentication header value or of a header written by name, or in the interpolated spelling of a header written by name; a header-enumeration value built from a constant that carries the scheme and the space and is concatenated directly, or formatted with the standard library's format call; a qualified constant that names another scheme in any segment of its spelling, in the header-enumeration value",
				Reason = "a qualified constant is read only where the header is written through the header enumeration, concatenated or in an interpolation hole, and the other classes are widened when the shape is shown to them. A constant that carries the scheme and the space is not read when it is concatenated directly, because that shape would otherwise match every two-operand concatenation written into an Authorization header. A qualified constant is never resolved, so one that names another scheme without Bearer, Negotiate, Digest, Ntlm or Kerberos anywhere in its spelling is reported at Medium as if it were Basic, and one that has such a word in its spelling but names Basic is not reported. Review such code by hand"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a Windows Presentation Foundation password control reached through a call with a nested call in its argument, a constructed operand or a parenthesized operand in the stored \"as\" form (\"Find(Get(\"x\")) as PasswordBox\", \"new Factory() as PasswordBox\", \"(expression) as PasswordBox\")",
				Reason = "the operand of the cast or \"as\" is bounded, so the signal that a person typed a password into the control does not fire on these three operands and the finding is decided as if the signal were absent. That can only move the result toward the default flow; it never assigns a flow the file does not show"
			}
		];

		return entries;
	}

	/// <summary>What the engine itself cannot yet bound or reconcile - three gaps.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForEngineLimits()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "a file whose analysis takes too long and is stopped by a time limit",
				Reason = "when this happens, every finding for that file is discarded, not just the part that was slow. The file is counted in the report's \"Skipped items\" section, so the loss is visible as a number, but which specific issues were lost in that file is not shown - such files should be reviewed by hand"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "which specific findings were left out when the tool reached its limit on the number of findings it reports, for a single file or for the whole scan",
				Reason = "the tool does limit how many findings it reports per file and overall, and the report states how many were left out - only aggregate counts are reported; the omitted findings themselves are not named or located individually"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a single hand-built Authorization header that the tool reports as two or three separate findings at nearby locations",
				Reason = "the tool does not merge findings that come from more than one piece of code working together, such as a credential built on one line and used on another. Each part is reported separately even though they are one migration, and all of the reported locations need to be fixed together"
			}
		];

		return entries;
	}

	/// <summary>What sits outside a lexical, per-file, eight-language analysis entirely - five gaps.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForOutOfLanguageScope()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "a C# source file saved in a single-byte text encoding other than Windows-1252 (for example a Cyrillic or Central European code page), rather than in UTF-8, UTF-16 or Windows-1252",
				Reason = "a file that is not UTF-8 or UTF-16 is read as Windows-1252, which covers Latin-1 and the other Western European files. A file in another single-byte code page that holds a byte Windows-1252 leaves undefined cannot be read that way: it is counted among the files that could not be read as text, and nothing inside it is examined for Basic Authentication. One that holds only bytes Windows-1252 defines is read, but its accented or other non-ASCII characters come out as different characters; a credential written in plain ASCII is still found, and one whose value holds such a character is read with the wrong character. Convert those files to UTF-8 and scan again, or review them by hand"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "authentication performed inside a compiled library or binary dependency, rather than in the client's own source code",
				Reason = "the tool reads source code as text; a compiled binary has no source text for it to examine"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "C# code embedded inside another file type, such as a Razor view, or code written in a related .NET language such as VB.NET",
				Reason = "this analyzer only scans plain C# source files; other file types and other languages are not covered by it"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "code inside a preprocessor conditional block (such as \"#if\"/\"#endif\"), and certain unusual string literal forms (an interpolated raw string, or a raw string opened with more than one \"$\")",
				Reason = "these are known gaps in how the tool reads C# text into code versus comments and strings, rather than a limitation of any one detection rule"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a Base64-encoded value made of two parts separated by a colon that is not actually a credential",
				Reason = "the tool cannot always tell such values apart from an encoded user name and password pair, so a non-credential value with this same shape may occasionally be reported and should be dismissed on review"
			}
		];

		return entries;
	}

	private static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			[_CS_001] = BuildCS001(),
			[_CS_002] = BuildCS002(),
			[_CS_003] = BuildCS003(),
			[_CS_004] = BuildCS004(),
			[_CS_005] = BuildCS005(),
			[_CS_006] = BuildCS006(),
			[_CS_007] = BuildCS007(),
			[_CS_008] = BuildCS008(),
			[_CS_009] = BuildCS009(),
			[_CS_011] = BuildCS011(),
			[_CS_012] = BuildCS012(),
			[_CS_013] = BuildCS013(),
			[_CS_014] = BuildCS014(),
			[_CS_015] = BuildCS015(),
			[_CS_016] = BuildCS016(),
			[_CS_017] = BuildCS017(),
			[_CS_018] = BuildCS018(),
			[_CS_019] = BuildCS019()
		};

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static DetectionRule BuildCS001()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.SdkUserNamePasswordCredentialsConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = _CS_001_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>Anchored on the same token as CS-001, which is what the subsumption triple relies on.</summary>
	private static DetectionRule BuildCS002()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.SdkLegacyRsapiCredentialsConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = _CS_002_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildCS003()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.CredentialCacheBasicSchemeConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = BuildSchemeInterpretation(),
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_003_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildCS004()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.ThirdPartyBasicAuthenticatorTypeConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_004_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildCS005()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.AuthorizationHeaderValueSchemeConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = BuildSchemeInterpretation(),
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_005_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>The anchor group captures the whole header literal - see the expression's own doc comment.</summary>
	private static DetectionRule BuildCS006()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.AuthorizationHeaderStringLiteralConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_HEADER_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = BuildSchemeInterpretation(),
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_006_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildCS007()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.NetworkCredentialOnHttpHandlerConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Medium,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_007_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildCS008()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.NetworkCredentialConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Medium,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_008_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildCS009()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.Base64UserPasswordPairConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_ENCODER_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Medium,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_009_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// CS-011. Low, routed to manual review - no recommendation template. Credential position stays the
	/// default <see cref="CredentialPosition.None"/>: no argument of a method DECLARATION is the
	/// credential. Deliberately not subsumed with, and not subsuming, CS-001 (the catalog's
	/// "three absences" list) - a helper that constructs the credential matches both and both emit.
	/// </summary>
	private static DetectionRule BuildCS011()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.AuthenticationHelperDefinitionDeclaration(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_DECLARATION_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Low,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_011_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// CS-012. High base, not self-sufficient (the legacy BCL handler's own binding still has to be
	/// established), so composition lands it at Medium. Carries the shared scheme interpretation -
	/// the same verdict machinery CS-003, CS-005 and CS-006 use - because both value shapes capture
	/// a <c>scheme</c> group subject to symbolic resolution.
	/// </summary>
	private static DetectionRule BuildCS012()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.HttpRequestHeaderEnumAuthorizationConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = BuildSchemeInterpretation(),
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_012_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// CS-013. High base, not self-sufficient: the binding's
	/// own expression cannot witness the credential, which lives in a different statement. No
	/// scheme argument here - the scheme is explicit text in the credential-type MEMBER NAME, never
	/// an argument symbolic resolution handles - so <see cref="SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution"/>.
	/// </summary>
	private static DetectionRule BuildCS013()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.WcfBasicTransportCredentialConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_013_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// CS-014. High base, self-sufficient - the method name declares both the scheme and that two
	/// credential arguments follow, per the catalog's <c>Declares</c> row.
	/// </summary>
	private static DetectionRule BuildCS014()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.SchemeNamingExtensionMethodConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_014_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// CS-015. High base, self-sufficient only at the three-argument arity the catalog requires -
	/// the SDK type establishes its own destination, per the <c>Established</c> row, the
	/// same as CS-001, CS-002 and CS-017.
	/// </summary>
	private static DetectionRule BuildCS015()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.SdkConstructorWithUserAndPasswordConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = _CS_015_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// CS-016. Medium base, not self-sufficient: the pair never appears in one expression, so the second
	/// half's only evidence is the property name reached across the bounded window.
	/// </summary>
	private static DetectionRule BuildCS016()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.CredentialAssembledAcrossStatementsConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Medium,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_016_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// CS-017. High base, self-sufficient - the property names declare both the carrier and the
	/// vendor, per the catalog. The
	/// vendor-named properties establish the destination, per the <c>Established</c> row.
	/// </summary>
	private static DetectionRule BuildCS017()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.SdkSettingsCredentialPropertiesConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = _CS_017_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// CS-018. Medium base per the catalog. No scheme argument: the migration
	/// this class recommends is the same regardless of what scheme token is present.
	/// </summary>
	private static DetectionRule BuildCS018()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.TokenCarriedUnderBasicSchemeConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_ENCODER_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Medium,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_018_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildCS019()
	{
		DetectionRule rule = new()
		{
			Expression = CSharpPatternExpressions.NonHttpCredentialSinkConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Low,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _CS_019_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// The catalog's scheme interpretation, shared by CS-003, CS-005 and CS-006 - the three
	/// classes that carry a scheme argument subject to symbolic resolution.
	/// </summary>
	private static SymbolicVerdictInterpretation BuildSchemeInterpretation()
	{
		SymbolicVerdictInterpretation interpretation = new()
		{
			SchemeArgumentCaptureGroupName = _SCHEME_CAPTURE_GROUP,
			ExpectedSchemeToken = _EXPECTED_SCHEME_TOKEN,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.ProposesMediumLevel
		};

		return interpretation;
	}

	/// <summary>
	/// The catalog's demotion signals: file-name suffixes, folder names and
	/// suffixes, and three content markers. The generation-marker slot (name and
	/// folder segments only) is declared and empty - the generated-code header and the
	/// <c>GeneratedCode</c> attribute both need content, which is what <see cref="ContentMarker"/>
	/// is for.
	/// </summary>
	private static LanguageShapedDemotionSignals BuildDemotionSignals()
	{
		LanguageShapedDemotionSignals signals = new()
		{
			FileNameSignals = BuildFileNameSignals(),
			FolderNameSignals = BuildFolderNameSignals(),
			GenerationMarkerExpressions = [],
			ContentMarkers = BuildContentMarkers()
		};

		return signals;
	}

	private static FrozenSet<FileNameSignal> BuildFileNameSignals()
	{
		FrozenSet<FileNameSignal> signals = new[]
		{
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _GENERATED_FILE_SUFFIX_G, Kind = DemotionKind.Generated },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _GENERATED_FILE_SUFFIX_DESIGNER, Kind = DemotionKind.Generated },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _GENERATED_FILE_SUFFIX_GENERATED, Kind = DemotionKind.Generated },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _TEST_FILE_SUFFIX_TESTS, Kind = DemotionKind.Test },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _TEST_FILE_SUFFIX_TEST, Kind = DemotionKind.Test },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _TEST_FILE_SUFFIX_SPEC, Kind = DemotionKind.Test },
			new FileNameSignal { Match = FileNameMatch.Prefix, Text = _TEST_DOUBLE_PREFIX_FAKE, Kind = DemotionKind.Mock },
			new FileNameSignal { Match = FileNameMatch.Prefix, Text = _TEST_DOUBLE_PREFIX_MOCK, Kind = DemotionKind.Mock },
			new FileNameSignal { Match = FileNameMatch.Prefix, Text = _TEST_DOUBLE_PREFIX_STUB, Kind = DemotionKind.Mock },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _TEST_DOUBLE_SUFFIX_MOCK, Kind = DemotionKind.Mock },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _TEST_DOUBLE_SUFFIX_STUB, Kind = DemotionKind.Mock },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _TEST_DOUBLE_SUFFIX_FAKE, Kind = DemotionKind.Mock },
			new FileNameSignal { Match = FileNameMatch.Prefix, Text = _SAMPLE_FILE_PREFIX_SAMPLE, Kind = DemotionKind.Sample },
			new FileNameSignal { Match = FileNameMatch.Prefix, Text = _SAMPLE_FILE_PREFIX_EXAMPLE, Kind = DemotionKind.Sample }
		}.ToFrozenSet();

		return signals;
	}

	private static FrozenSet<FolderNameSignal> BuildFolderNameSignals()
	{
		FrozenSet<FolderNameSignal> signals = new[]
		{
			new FolderNameSignal { FolderName = _TEST_FOLDER_NAME_TEST, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test },
			new FolderNameSignal { FolderName = _TEST_FOLDER_NAME_TESTS, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test },
			new FolderNameSignal { FolderName = _TEST_FOLDER_NAME_SPEC, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test },
			new FolderNameSignal { FolderName = _TEST_FOLDER_NAME_FIXTURES, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test },
			new FolderNameSignal { FolderName = _SAMPLE_FOLDER_NAME_SAMPLE, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Sample },
			new FolderNameSignal { FolderName = _SAMPLE_FOLDER_NAME_SAMPLES, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Sample },
			new FolderNameSignal { FolderName = _SAMPLE_FOLDER_NAME_EXAMPLE, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Sample },
			new FolderNameSignal { FolderName = _SAMPLE_FOLDER_NAME_EXAMPLES, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Sample },
			new FolderNameSignal { FolderName = _VENDORED_FOLDER_NAME_VENDOR, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Vendored },
			new FolderNameSignal { FolderName = _VENDORED_FOLDER_NAME_THIRD_PARTY, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Vendored },
			// The Deployment demotion lives here, not in the shared path-shaped set. It is a demotion, never an exclusion.
			new FolderNameSignal { FolderName = _DEPLOYMENT_FOLDER_NAME, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Deployment },
			new FolderNameSignal
			{
				FolderName = _TEST_FOLDER_SUFFIX_TEST,
				Match = FolderNameMatch.Suffix,
				Sensitivity = CaseSensitivity.Insensitive,
				Kind = DemotionKind.Test
			},
			new FolderNameSignal
			{
				FolderName = _TEST_FOLDER_SUFFIX_TESTS,
				Match = FolderNameMatch.Suffix,
				Sensitivity = CaseSensitivity.Insensitive,
				Kind = DemotionKind.Test
			}
		}.ToFrozenSet();

		return signals;
	}

	private static ImmutableArray<ContentMarker> BuildContentMarkers()
	{
		ImmutableArray<ContentMarker> markers =
		[
			new ContentMarker
			{
				Expression = CSharpPatternExpressions.GeneratedHeaderContentMarker(),
				Zone = ContentMarkerZone.AnyZone,
				Kind = DemotionKind.Generated
			},
			new ContentMarker
			{
				Expression = CSharpPatternExpressions.GeneratedCodeAttributeContentMarker(),
				Zone = ContentMarkerZone.CodeOnly,
				Kind = DemotionKind.Generated
			},
			new ContentMarker
			{
				Expression = CSharpPatternExpressions.TestAttributeContentMarker(),
				Zone = ContentMarkerZone.CodeOnly,
				Kind = DemotionKind.Test
			}
		];

		return markers;
	}

	/// <summary>
	/// The catalog's fourteen case-sensitive anchors: every one of CS-011..CS-018 is reached by an anchor in this list
	/// (<c>Credential</c>, <c>Authorization</c> and <c>Authenticator</c> for CS-011; <c>Authorization</c>
	/// for CS-012; <c>ClientCredentialType</c> for CS-013; <c>WithBasicAuth</c> for CS-014;
	/// <c>ImportAPI</c> for CS-015; <c>Credential</c> for CS-016; <c>RelativityUsername</c> and
	/// <c>RelativityPassword</c> for CS-017; <c>Base64</c> for CS-018) - the anchor collection is a property
	/// of the whole catalog's vocabulary and this is the one registered `CSharp` analyzer both halves
	/// extend.
	///
	/// Also declares <see cref="CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow"/> for
	/// both flows, the catalog's two "Generic fallback content for CSharp" texts.
	/// </summary>
	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = new[]
			{
				new PreselectionAnchor { Text = "Credential" },
				new PreselectionAnchor { Text = "Authorization" },
				new PreselectionAnchor { Text = "Authenticator" },
				new PreselectionAnchor { Text = "ToBase64String" },
				new PreselectionAnchor { Text = "ServiceFactorySettings" },
				new PreselectionAnchor { Text = "RSAPIClient" },
				new PreselectionAnchor { Text = "CredentialCache" },
				new PreselectionAnchor { Text = "ClientCredentialType" },
				new PreselectionAnchor { Text = "WithBasicAuth" },
				new PreselectionAnchor { Text = "ImportAPI" },
				new PreselectionAnchor { Text = "Base64" },
				new PreselectionAnchor { Text = "UserName" },
				new PreselectionAnchor { Text = "RelativityUsername" },
				new PreselectionAnchor { Text = "RelativityPassword" }
			}.ToFrozenSet(),
			TemplateProvider = ProvideTemplate,
			FlowSignals = CSharpFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = new Dictionary<CredentialFlow, string>
			{
				[CredentialFlow.ClientCredentials] = CSharpRecommendationTemplates.CSHARP_LANGUAGE_FALLBACK_TEXT,
				[CredentialFlow.AuthorizationCode] = CSharpRecommendationTemplates.CSHARP_CODE_LANGUAGE_FALLBACK_TEXT
			}.ToFrozenDictionary()
		};

		return element;
	}

	/// <summary>
	/// Sixteen classes resolve to their catalog entry for either flow; CS-011 and CS-019 - and
	/// any identifier this analyzer does not declare - resolve to null, which is what routes a Low
	/// finding to manual review and would route an unrecognized High or Medium finding to the
	/// generic route with this analyzer's own declared fallback text for the flow, since
	/// <see cref="BuildCandidateSelectionAndRecommendation"/> supplies one per flow.
	/// `RecommendationTemplateProvider` declares no third outcome.
	/// </summary>
	private static RecommendationTemplate? ProvideTemplate(string patternClassIdentifier, CredentialFlow flow)
	{
		FrozenDictionary<string, RecommendationTemplate> templatesByPatternClass =
			flow is CredentialFlow.AuthorizationCode
				? _authorizationCodeTemplatesByPatternClass
				: _clientCredentialsTemplatesByPatternClass;

		templatesByPatternClass.TryGetValue(patternClassIdentifier, out RecommendationTemplate? found);

		return found;
	}

	/// <summary>
	/// The catalog's sixteen templates, one call per class, matching the per-class builder
	/// pattern <see cref="BuildDetectionRules"/> already uses for the eighteen detection rules. The
	/// token acquisition passed in is part 2 of the flow being built.
	/// </summary>
	private static FrozenDictionary<string, RecommendationTemplate> BuildTemplates(string tokenAcquisition)
	{
		Dictionary<string, RecommendationTemplate> templates = new(StringComparer.Ordinal)
		{
			[_CS_001] = BuildCS001Template(tokenAcquisition),
			[_CS_002] = BuildCS002Template(tokenAcquisition),
			[_CS_003] = BuildCS003Template(tokenAcquisition),
			[_CS_004] = BuildCS004Template(tokenAcquisition),
			[_CS_005] = BuildCS005Template(tokenAcquisition),
			[_CS_006] = BuildCS006Template(tokenAcquisition),
			[_CS_007] = BuildCS007Template(tokenAcquisition),
			[_CS_008] = BuildCS008Template(tokenAcquisition),
			[_CS_009] = BuildCS009Template(tokenAcquisition),
			[_CS_012] = BuildCS012Template(tokenAcquisition),
			[_CS_013] = BuildCS013Template(tokenAcquisition),
			[_CS_014] = BuildCS014Template(tokenAcquisition),
			[_CS_015] = BuildCS015Template(tokenAcquisition),
			[_CS_016] = BuildCS016Template(tokenAcquisition),
			[_CS_017] = BuildCS017Template(tokenAcquisition),
			[_CS_018] = BuildCS018Template(tokenAcquisition)
		};

		return templates.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>
	/// The credential provenance is the class's own share followed by part 2 for the flow, as two
	/// paragraphs (the catalog's "How a recommendation is composed").
	/// </summary>
	private static string ComposeCredentialProvenance(string classShare, string tokenAcquisition)
	{
		string provenance = string.Concat(classShare, _PARAGRAPH_SEPARATOR, tokenAcquisition);

		return provenance;
	}

	private static RecommendationTemplate BuildCS001Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_001_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_001_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_001_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_001_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS002Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_002_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_002_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_002_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_002_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS003Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_003_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_003_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_003_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_003_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS004Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_004_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_004_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_004_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_004_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS005Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_005_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_005_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_005_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_005_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS006Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_006_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_006_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_006_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_006_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS007Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_007_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_007_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_007_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_007_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS008Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_008_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_008_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_008_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_008_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS009Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_009_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_009_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_009_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_009_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS012Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_012_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_012_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_012_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_012_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS013Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_013_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_013_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_013_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_013_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS014Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_014_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_014_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_014_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_014_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS015Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_015_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_015_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_015_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_015_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS016Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_016_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_016_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_016_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_016_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS017Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_017_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_017_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_017_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_017_VERIFICATION_NOTE
		};
	}

	private static RecommendationTemplate BuildCS018Template(string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = CSharpRecommendationTemplates.CS_018_SOURCE_FORM,
			TargetFormUnderTokenAuthentication = CSharpRecommendationTemplates.CS_018_TARGET_FORM,
			CredentialProvenance = ComposeCredentialProvenance(CSharpRecommendationTemplates.CS_018_CREDENTIAL_PROVENANCE, tokenAcquisition),
			VerificationNote = CSharpRecommendationTemplates.CS_018_VERIFICATION_NOTE
		};
	}

	/// <summary>
	/// The catalog's declared triples. CS-002 subsumes CS-001 at identical anchor
	/// positions - both anchor on the same credential-type token. CS-003, CS-007 and CS-019 each subsume
	/// CS-008 at overlapping spans - an HTTP binding and a non-HTTP sink both bind the credential. CS-019
	/// also subsumes CS-016 at overlapping spans: a credential assembled across statements and bound to
	/// a non-HTTP sink is the sink's near-miss, not a Medium finding of its own.
	/// </summary>
	private static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		FrozenSet<SubsumptionRule> relation = new[]
		{
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _CS_002,
				SubsumedPatternClassIdentifier = _CS_001,
				When = SubsumptionCondition.AnchorPositionsAreIdentical
			},
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _CS_003,
				SubsumedPatternClassIdentifier = _CS_008,
				When = SubsumptionCondition.MatchSpansOverlap
			},
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _CS_007,
				SubsumedPatternClassIdentifier = _CS_008,
				When = SubsumptionCondition.MatchSpansOverlap
			},
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _CS_019,
				SubsumedPatternClassIdentifier = _CS_008,
				When = SubsumptionCondition.MatchSpansOverlap
			},
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _CS_019,
				SubsumedPatternClassIdentifier = _CS_016,
				When = SubsumptionCondition.MatchSpansOverlap
			}
		}.ToFrozenSet();

		return relation;
	}
}
