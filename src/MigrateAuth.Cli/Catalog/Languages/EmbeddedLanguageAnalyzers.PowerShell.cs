using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `PowerShell` analyzer. Fourteen classes, eleven of which can reach High or Medium and
/// three of which are Low by construction; nine flow signals; the recommendation text of eleven classes for both flows; and the lexical profile of a
/// language whose delimiters, keywords and names do not depend on case.
///
/// FILES. The analyzer type is <c>file</c>-scoped, so its expressions live in <see cref="PowerShellPatternExpressions"/> (two files and the flow-signal
/// file), the flow-signal declarations in <see cref="PowerShellFlowSignalDeclarations"/> and the recommendation text in
/// <see cref="PowerShellRecommendationTemplates"/>. The registration is the one line in <see cref="EmbeddedLanguageAnalyzers.CreateAll"/> and the
/// partial method below.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddPowerShellAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new PowerShellAnalyzer());
	}
}

/// <summary>
/// The `PowerShell` analyzer. File-local per the registration pattern every embedded language follows, so no engine type can name it and no special
/// case can be written against it.
/// </summary>
file sealed class PowerShellAnalyzer : ILanguageAnalyzer
{
	private const string _PROFILE_NAME = "PowerShell";
	private const string _LANGUAGE_NAME = "PowerShell";
	private const string _SCRIPT_EXTENSION = ".ps1";
	private const string _MODULE_EXTENSION = ".psm1";
	private const string _DATA_EXTENSION = ".psd1";

	private const string _PS_001 = "PS-001";
	private const string _PS_002 = "PS-002";
	private const string _PS_003 = "PS-003";
	private const string _PS_004 = "PS-004";
	private const string _PS_005 = "PS-005";
	private const string _PS_006 = "PS-006";
	private const string _PS_007 = "PS-007";
	private const string _PS_008 = "PS-008";
	private const string _PS_009 = "PS-009";
	private const string _PS_010 = "PS-010";
	private const string _PS_011 = "PS-011";
	private const string _PS_012 = "PS-012";
	private const string _PS_013 = "PS-013";
	private const string _PS_014 = "PS-014";

	private const string _PS_001_DISPLAY_NAME = "SdkUserNamePasswordCredentials";
	private const string _PS_002_DISPLAY_NAME = "SdkLegacyRsapiCredentials";
	private const string _PS_003_DISPLAY_NAME = "ExplicitBasicAuthenticationParameter";
	private const string _PS_004_DISPLAY_NAME = "AuthorizationHeaderHashtableLiteral";
	private const string _PS_005_DISPLAY_NAME = "AuthorizationHeaderStringKeyWrite";
	private const string _PS_006_DISPLAY_NAME = "AuthorizationHeaderEnumWrite";
	private const string _PS_007_DISPLAY_NAME = "AuthenticationHeaderValueScheme";
	private const string _PS_008_DISPLAY_NAME = "CredentialCacheBasicScheme";
	private const string _PS_009_DISPLAY_NAME = "NetworkCredentialOnHttpType";
	private const string _PS_010_DISPLAY_NAME = "Base64UserPasswordPair";
	private const string _PS_011_DISPLAY_NAME = "TokenCarriedUnderBasicScheme";
	private const string _PS_012_DISPLAY_NAME = "CredentialOnWebCmdletWithoutScheme";
	private const string _PS_013_DISPLAY_NAME = "NonHttpCredentialSink";
	private const string _PS_014_DISPLAY_NAME = "AuthenticationHelperDefinition";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _HEADER_CAPTURE_GROUP = "header";
	private const string _ENCODER_CAPTURE_GROUP = "encoder";
	private const string _DECLARATION_CAPTURE_GROUP = "declaration";
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _SCHEME_CAPTURE_GROUP = "scheme";

	private const string _EXPECTED_SCHEME_TOKEN = "Basic";

	private const string _LINE_COMMENT_OPENING = "#";
	private const string _BLOCK_COMMENT_OPENING = "<#";
	private const string _BLOCK_COMMENT_CLOSING = "#>";
	private const string _SINGLE_QUOTE = "'";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _EXPANDABLE_HERE_STRING_OPENING = "@\"";
	private const string _EXPANDABLE_HERE_STRING_CLOSING = "\"@";
	private const string _LITERAL_HERE_STRING_OPENING = "@'";
	private const string _LITERAL_HERE_STRING_CLOSING = "'@";
	private const string _TYPOGRAPHIC_DOUBLE_OPENING = "\u201C";
	private const string _TYPOGRAPHIC_DOUBLE_CLOSING = "\u201D";
	private const string _TYPOGRAPHIC_SINGLE_OPENING = "\u2018";
	private const string _TYPOGRAPHIC_SINGLE_CLOSING = "\u2019";
	private const string _SUB_EXPRESSION_OPENING = "$(";
	private const string _SUB_EXPRESSION_CLOSING = ")";
	private const string _NESTING_OPENING = "(";
	private const char _ESCAPE_CHARACTER = '`';
	private const string _COMMENT_PREDECESSOR_CHARACTERS = ";(){}|&,=";

	private const string _TEST_FILE_SUFFIX = ".Tests.ps1";
	private const string _GENERATED_FILE_SUFFIX_GENERATED = ".generated.ps1";
	private const string _GENERATED_FILE_SUFFIX_SHORT = ".g.ps1";
	private const string _SAMPLE_FILE_PREFIX_SAMPLE = "Sample";
	private const string _SAMPLE_FILE_PREFIX_EXAMPLE = "Example";

	private const string _TEST_FOLDER_NAME_TEST = "test";
	private const string _TEST_FOLDER_NAME_TESTS = "tests";
	private const string _MOCK_FOLDER_NAME_MOCK = "mock";
	private const string _MOCK_FOLDER_NAME_MOCKS = "mocks";
	private const string _FIXTURE_FOLDER_NAME_FIXTURE = "fixture";
	private const string _FIXTURE_FOLDER_NAME_FIXTURES = "fixtures";
	private const string _SAMPLE_FOLDER_NAME_SAMPLE = "sample";
	private const string _SAMPLE_FOLDER_NAME_SAMPLES = "samples";
	private const string _SAMPLE_FOLDER_NAME_EXAMPLE = "example";
	private const string _SAMPLE_FOLDER_NAME_EXAMPLES = "examples";
	private const string _VENDORED_FOLDER_NAME_VENDOR = "vendor";
	private const string _VENDORED_FOLDER_NAME_THIRD_PARTY = "third-party";

	private const string _ANCHOR_CREDENTIAL = "Credential";
	private const string _ANCHOR_AUTHORIZATION = "Authorization";
	private const string _ANCHOR_AUTHENTICATION = "Authentication";
	private const string _ANCHOR_TO_BASE64_STRING = "ToBase64String";

	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = BuildDemotionSignals();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } =
		BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = BuildSubsumptionRelation();

	/// <summary>Built once: eleven entries, one per class that advises, whose credential provenance ends with part 2 for Client Credentials.</summary>
	private static readonly FrozenDictionary<string, RecommendationTemplate> _clientCredentialsTemplatesByPatternClass =
		BuildTemplates(PowerShellRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION);

	/// <summary>The same eleven classes for Authorization Code: part 1 is identical and the credential provenance ends with part 2 for Authorization Code.</summary>
	private static readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplatesByPatternClass =
		BuildTemplates(PowerShellRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION);

	// ------------------------------------------------------------------------------------------
	// Element 1: the lexical profile
	// ------------------------------------------------------------------------------------------

	private static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = PowerShellPatternExpressions.ConstantDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Insensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Insensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Insensitive,
			LineCommentRules = BuildLineCommentRules(),
			BlockCommentRules = BuildBlockCommentRules(),
			QuoteRules = BuildQuoteRules()
		};

		return profile;
	}

	/// <summary>
	/// The <c>#</c> comment opens where a statement or an argument may begin: at the start of the text, after whitespace, or after one of the
	/// statement and operator characters. A hash inside an unquoted argument or after a word character is not a comment, and one after a backtick is
	/// escaped.
	/// </summary>
	private static ImmutableArray<LineCommentRule> BuildLineCommentRules()
	{
		ImmutableArray<LineCommentRule> rules =
		[
			new LineCommentRule { Opening = _LINE_COMMENT_OPENING, OpeningContextPredicate = CommentOpeningStandsHere }
		];

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
	/// Six quote rules. The two ordinary quotes take newlines as content, the doubled close escapes, the double quote also has the backtick and one
	/// interpolation hole, <c>$( … )</c>, with nested parentheses counted. The two here-strings open at the end of a line (blanks may follow) and close at
	/// the start of one. The two typographic pairs read as their ASCII counterparts. A bare <c>$name</c> or <c>${name}</c> in a double-quoted string is string body:
	/// a quote rule declares one hole kind.
	/// </summary>
	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		ImmutableArray<QuoteRule> rules =
		[
			BuildSingleQuotedRule(),
			BuildDoubleQuotedRule(),
			BuildHereStringRule(_EXPANDABLE_HERE_STRING_OPENING, _EXPANDABLE_HERE_STRING_CLOSING, _ESCAPE_CHARACTER),
			BuildHereStringRule(_LITERAL_HERE_STRING_OPENING, _LITERAL_HERE_STRING_CLOSING, escapeCharacter: null),
			BuildTypographicRule(_TYPOGRAPHIC_DOUBLE_OPENING, _TYPOGRAPHIC_DOUBLE_CLOSING, _ESCAPE_CHARACTER),
			BuildTypographicRule(_TYPOGRAPHIC_SINGLE_OPENING, _TYPOGRAPHIC_SINGLE_CLOSING, escapeCharacter: null)
		];

		return rules;
	}

	private static QuoteRule BuildSingleQuotedRule()
	{
		QuoteRule rule = new()
		{
			Opening = _SINGLE_QUOTE,
			Closing = _SINGLE_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			DoubledClosingEscapes = true,
			OpeningContextPredicate = QuoteOpeningIsNotBacktickEscaped
		};

		return rule;
	}

	private static QuoteRule BuildDoubleQuotedRule()
	{
		QuoteRule rule = new()
		{
			Opening = _DOUBLE_QUOTE,
			Closing = _DOUBLE_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			EscapeCharacter = _ESCAPE_CHARACTER,
			DoubledClosingEscapes = true,
			InterpolationOpening = _SUB_EXPRESSION_OPENING,
			InterpolationClosing = _SUB_EXPRESSION_CLOSING,
			InterpolationNestingOpening = _NESTING_OPENING,
			OpeningContextPredicate = QuoteOpeningIsNotBacktickEscaped
		};

		return rule;
	}

	/// <summary>
	/// The expandable form declares no hole, so its body is string body to the scanner (the expression inside such a hole is invisible, a declared
	/// limit); the literal form has none by nature.
	/// </summary>
	private static QuoteRule BuildHereStringRule(string opening, string closing, char? escapeCharacter)
	{
		QuoteRule rule = new()
		{
			Opening = opening,
			Closing = closing,
			NewlinePolicy = NewlinePolicy.Allowed,
			EscapeCharacter = escapeCharacter,
			OpeningAnchoring = PositionalAnchoring.EndOfLineAfterOptionalBlanks,
			ClosingAnchoring = PositionalAnchoring.StartOfLine
		};

		return rule;
	}

	private static QuoteRule BuildTypographicRule(string opening, string closing, char? escapeCharacter)
	{
		QuoteRule rule = new()
		{
			Opening = opening,
			Closing = closing,
			NewlinePolicy = NewlinePolicy.Allowed,
			EscapeCharacter = escapeCharacter,
			DoubledClosingEscapes = true
		};

		return rule;
	}

	/// <summary>True at the start of the text, after whitespace, or after one of the statement and operator characters; false after a backtick or a word character.</summary>
	private static bool CommentOpeningStandsHere(ReadOnlySpan<char> text, int delimiterPosition)
	{
		bool stands = delimiterPosition == 0
			|| char.IsWhiteSpace(text[delimiterPosition - 1])
			|| _COMMENT_PREDECESSOR_CHARACTERS.Contains(text[delimiterPosition - 1]);

		return stands;
	}

	/// <summary>An opening quote preceded by an odd run of backticks is an escaped character of the code, not an opening.</summary>
	private static bool QuoteOpeningIsNotBacktickEscaped(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int backtickRun = 0;

		while (delimiterPosition - backtickRun - 1 >= 0 && text[delimiterPosition - backtickRun - 1] == _ESCAPE_CHARACTER)
		{
			backtickRun++;
		}

		return backtickRun % 2 == 0;
	}

	// ------------------------------------------------------------------------------------------
	// Element 2: identification and declared unsupported coverage
	// ------------------------------------------------------------------------------------------

	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _SCRIPT_EXTENSION, _MODULE_EXTENSION, _DATA_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			FirstLineDetectionExpression = PowerShellPatternExpressions.ShebangFirstLine(),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = BuildDeclaredUnsupportedCoverage()
		};

		return identification;
	}

	/// <summary>
	/// Written for the client reading the report: each construct names what the tool does not detect or cannot tell in plain words, each reason says why
	/// and, where useful, what to do about it. No internal design term, class identifier or section reference belongs in either field. The three classes
	/// supported at Low only are a different statement (they ARE recognized) and do not appear here. Split into five grouped helpers, each under the
	/// method-length limit.
	/// </summary>
	private static FrozenSet<DeclaredUnsupportedCoverage> BuildDeclaredUnsupportedCoverage()
	{
		IEnumerable<DeclaredUnsupportedCoverage> entries = UnsupportedCoverageForDirectionAndScope()
			.Concat(UnsupportedCoverageForWhatTheEngineCannotJoin())
			.Concat(UnsupportedCoverageForCredentialForms())
			.Concat(UnsupportedCoverageForPairsAndWhatTheTextCannotShow())
			.Concat(UnsupportedCoverageForTextAndContainers());

		return entries.ToFrozenSet();
	}

	/// <summary>Who is authenticating, to what and which way it faces, and what the tool limits: the flow-shaped and limit-shaped gaps.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForDirectionAndScope()
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
				Construct = "confirmation that a matched Relativity SDK credential type actually comes from the Relativity SDK, rather than a different library that happens to use the same type name",
				Reason = "the tool matches on the type name as written in the script, not on which assembly it was loaded from; a different library using an identically named type would be matched the same way - confirm the source of the type where this matters"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a service that checks incoming Basic Authentication from its own callers, rather than sending it out to another server",
				Reason = "the tool cannot always tell which direction a credential is flowing in from the code in one file; a service that only accepts Basic Authentication from its callers has nothing to migrate to Relativity's token-based authentication. A service that decodes the header itself may still be reported as if it were sending credentials out, so confirm the direction before acting on a finding like this"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a single hand-built Authorization header that the tool reports as two or three separate findings at nearby locations",
				Reason = "the tool does not merge findings that come from more than one piece of code working together, such as a credential encoded on one line and used in a header on another. Each part is reported separately even though they are one migration, and all of the reported locations need to be fixed together"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a file whose analysis takes too long and is stopped by a time limit",
				Reason = "when this happens, every finding for that file is discarded, not just the part that was slow. The file is counted in the report's \"Skipped items\" section, so the loss is visible as a number, but which specific issues were lost in that file is not shown - such files should be reviewed by hand"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "which specific findings were left out when the tool reached its limit on the number of findings it reports, for a single file or for the whole scan",
				Reason = "the tool does limit how many findings it reports per file and overall, and the report states how many were left out - only aggregate counts are reported; the omitted findings themselves are not named or located individually"
			}
		];

		return entries;
	}

	/// <summary>What the tool cannot join across files, statements or windows, or resolve to a constant.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForWhatTheEngineCannotJoin()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "credentials, flow evidence or an authentication scheme supplied from another file: a dot-sourced script, an imported module, a loaded configuration or data file; and secret-store commands other than the three the tool lists",
				Reason = "the tool examines each file on its own and cannot join a definition in one file to its use in another. For many scripts the recommended flow therefore falls back to the default, which is not a failure of the tool: the note tells the developer to use the Authorization Code flow where the script acts for a signed-in user. A vault command it does not list is not read as evidence that the credential is fixed"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a header table or a header value built in another file or received as a parameter",
				Reason = "the value never appears in the text of the file the tool is reading, so there is nothing for it to match"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a splatted hashtable whose key is written more than twenty lines before the command that uses it, in another file, or built from $PSBoundParameters or $args",
				Reason = "the tool reads a splatted hashtable only within twenty lines before the command that splats it, including a table that is built key by key in that window with a dot write, an indexer or the Add method; a longer gap, or a table whose keys come from another file or from run-time parameters, is not followed"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "parameters supplied through $PSDefaultParameterValues",
				Reason = "the cmdlet call then carries no credential or authentication parameter in its own text, so the tool has nothing to match against"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a header written with a key or a value held in a variable, with no assignment that names the scheme in the five lines before it; and a value produced by the script's own function whose name does not carry the scheme word",
				Reason = "the scheme is not in the file's text near the write; a variable assigned a scheme-bearing value nearby, and a helper whose name carries the scheme word, are both read"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "an authentication scheme held in a variable that the tool cannot read as a constant: set by Set-Variable or New-Variable, by a parameter default, as a member of an object or hashtable, by a multiple assignment, through $using:, written inside a double-quoted string as a bare variable, or declared in another file",
				Reason = "the finding is kept and is reported at a lower confidence level, so a header whose scheme is actually Bearer but is held in such a variable is reported as a possible Basic header and should be dismissed on review. A parameter default is left unresolved on purpose, because a caller can override it"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a variable written inside a double-quoted string as $name or ${name}",
				Reason = "the tool reads such a variable as part of the string's text and not as code. No construct can hide in a bare variable name, so detection is not affected, but a name that itself contains a quote character can end the string early for the tool and not for PowerShell"
			}
		];

		return entries;
	}

	/// <summary>Credential objects and Basic Authentication in forms the tool does not read.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForCredentialForms()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "credential objects created in a form the tool does not read: a hashtable cast, -Property or a property assignment on an SDK credential type; a NetworkCredential built in one statement and assigned to a handler's Credentials through a variable; [Activator]::CreateInstance, reflection, or a type name computed from a string or a variable; an SDK entry point constructed directly from a user name and a password; and an SDK settings object with user-name and password properties",
				Reason = "the type name is never written out as plain text in these forms, or the assignment sees only a variable, so the tool has nothing to match against"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a user name and password joined by a colon and encoded by a helper whose body the file does not contain, by a native tool, or by a method other than the standard library's Base64 conversion",
				Reason = "the tool recognizes the standard library's conversion only; widening it to any call with Base64 in its name would read every conversion as the end of a credential pair. A header value that names the scheme, written next to the conversion, is still reported"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a user name and password written directly into a URL, such as \"https://user:password@host\", including through [UriBuilder]",
				Reason = "the tool does not detect this form at all. This is a real exposure independent of the OAuth2 migration and should be searched for and removed regardless of which authentication scheme is in use"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "Basic Authentication performed by a native program the script calls: curl.exe with -u or --user, an \"Authorization: Basic ...\" header argument, wget with --user",
				Reason = "an argument to an external command is text and not a PowerShell construct. On the newer edition of PowerShell curl and wget are these programs and not aliases of the web cmdlets"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "Basic Authentication through other stacks that take the credential as a parameter: a COM request's open call with a user name and password or its SetCredentials, Start-BitsTransfer -CustomHeaders carrying a header string, and the proxy object of New-WebServiceProxy",
				Reason = "the scheme is implicit, or the header is a string argument the tool does not read. A COM request that writes the Authorization header with setRequestHeader is read"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a Basic header declared as text and turned into a table at run time: an expandable here-string of name-and-value lines converted by ConvertFrom-StringData, or a JSON text converted by ConvertFrom-Json",
				Reason = "the key and the scheme sit in the body of a string, which the tool reads as text and reports at the lowest level; a script that builds its header this way is reported only through its Base64 step when one is visible, and through nothing when the credential arrives already encoded"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a Base64-encoded value made of two parts separated by a colon that is not actually a credential, such as a host and a port",
				Reason = "the tool cannot always tell such values apart from an encoded user name and password pair, so a non-credential value with this same shape may occasionally be reported and should be dismissed on review"
			}
		];

		return entries;
	}

	/// <summary>
	/// How a credential pair is put together before it is encoded, in forms the tool does not follow, and what a per-file reading of the text cannot show: whether
	/// the code runs, whether anything is sent, and over which protocol.
	/// </summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForPairsAndWhatTheTextCannotShow()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "a user name and password whose halves, or whose joined pair, reach the Base64 conversion through a pipeline: as $_ or $PSItem inside a ForEach-Object script block, or accumulated in its Begin, Process and End blocks",
				Reason = "the tool does not follow a value from one stage of a pipeline to the next, so the pair is never written in one of the spellings it reads. A pair written whole before the pipeline that encodes it is read. A script built this way is reported through its header write when the scheme is visible, and through nothing otherwise"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a user name and password accumulated over two statements with +=, such as a variable assigned the user name and then appended with a colon and the password",
				Reason = "the second statement alone is a colon followed by a value, which the tool reads as a token carried under the Basic scheme. The construct is reported at the same confidence level and with the same flow advice, but as that other kind of finding, so read its guidance with that in mind"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "whether a constructed credential, header or Base64 pair is ever sent, is reached on a path that runs, or goes to an HTTP server and not to another protocol: a header built and only logged, code in a branch that never runs, a web client aimed at an ftp:// address, a Base64 pair placed in a request body",
				Reason = "knowing this takes control-flow and data-flow analysis that a tool reading the text of each file does not have. Such code is still reported at its confidence level, and is a false positive to dismiss on review"
			}
		];

		return entries;
	}

	/// <summary>What sits outside a lexical, per-file analysis: containers, quoting and encodings the tool does not read, and flow evidence it does not list.</summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> UnsupportedCoverageForTextAndContainers()
	{
		ImmutableArray<DeclaredUnsupportedCoverage> entries =
		[
			new DeclaredUnsupportedCoverage
			{
				Construct = "PowerShell data and configuration files and other containers: constructs in module and data manifests (.psd1 files), DSC documents, .ps1xml, .psc1, .cdxml and .mof files, and credentials stored in configuration files",
				Reason = "no detection is designed for a data file, and configuration files are not source code in any language the tool scans; this is where a credential usually lives, so review those files separately"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "PowerShell code inside another file type, and code inside an Add-Type -TypeDefinition here-string, a -Command string or an Invoke-Expression argument",
				Reason = "the inner code is read as the text of the string that holds it and is not analyzed as code; the damage is bounded to that string and the rest of the file is read correctly. PowerShell embedded in a batch file, a pipeline definition or a SQL script is a different file type that the tool does not scan"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a call written inside the \"$( ... )\" part of an expandable here-string (a multi-line string opened with @\" and closed with \"@), or a user name and password written as a colon pair in the body of one",
				Reason = "the tool reads the body of such a string as plain text, so a credential prompt or an encoding call written inside one of its embedded expressions is not seen, and neither is a pair spelled across its embedded variables"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a string delimited by a typographic quote other than the double pair and the single pair the tool reads (U+201C and U+201D, U+2018 and U+2019), or opened with one member of a quote set and closed with another",
				Reason = "the tool reads such a string wrongly from its opening quote to the next quote of its kind, and in PowerShell a string can span lines, so the misreading can reach the end of the file: code after it may be read as text and not examined. Replace such quotes with ordinary ones and scan again"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "the name of a command that decides a finding, written so that the tool cannot read it: a backtick inside the name, a computed name invoked with &, a user-defined alias for a web cmdlet, or Invoke-Expression",
				Reason = "the command name is not text the tool's matching sees"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "a PowerShell script saved in a single-byte text encoding other than Windows-1252 (for example a Cyrillic or Central European code page), rather than in UTF-8, UTF-16 or Windows-1252",
				Reason = "a file that is not UTF-8 or UTF-16 is read as Windows-1252, which covers Latin-1 and the other Western European files. A file in another single-byte code page that holds a byte Windows-1252 leaves undefined cannot be read that way: it is counted among the files that could not be read as text, and nothing inside it is examined for Basic Authentication. One that holds only bytes Windows-1252 defines is read, but its accented or other non-ASCII characters come out as different characters. Convert those files to UTF-8 and scan again, or review them by hand"
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = "flow evidence the tool's signals do not list: an encrypted standard string read with Get-Content and converted without -AsPlainText, a vault command other than the three it lists, and credentials passed in with $using:",
				Reason = "PowerShell has many ways to hold a fixed credential, and the tool reads the nine with a stable shape. Where none is present the recommended flow is the default one"
			}
		];

		return entries;
	}

	// ------------------------------------------------------------------------------------------
	// Element 3: the detection rules
	// ------------------------------------------------------------------------------------------

	private static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			[_PS_001] = BuildPS001(),
			[_PS_002] = BuildPS002(),
			[_PS_003] = BuildPS003(),
			[_PS_004] = BuildPS004(),
			[_PS_005] = BuildPS005(),
			[_PS_006] = BuildPS006(),
			[_PS_007] = BuildPS007(),
			[_PS_008] = BuildPS008(),
			[_PS_009] = BuildPS009(),
			[_PS_010] = BuildPS010(),
			[_PS_011] = BuildPS011(),
			[_PS_012] = BuildPS012(),
			[_PS_013] = BuildPS013(),
			[_PS_014] = BuildPS014()
		};

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static DetectionRule BuildPS001()
	{
		return BuildSdkRule(PowerShellPatternExpressions.SdkUserNamePasswordCredentialsCreation(), _PS_001_DISPLAY_NAME);
	}

	/// <summary>Anchored on the same token as PS-001, which is what the subsumption triple relies on.</summary>
	private static DetectionRule BuildPS002()
	{
		return BuildSdkRule(PowerShellPatternExpressions.SdkLegacyRsapiCredentialsCreation(), _PS_002_DISPLAY_NAME);
	}

	/// <summary>An SDK credential type establishes its destination: self-sufficient, High, with the password argument as the credential position.</summary>
	private static DetectionRule BuildSdkRule(Regex expression, string displayName)
	{
		DetectionRule rule = new()
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = displayName
		};

		return rule;
	}

	/// <summary>
	/// The parameter declares the scheme and the cmdlet declares the outbound call, so the carrier is declared by the cmdlet and the class is
	/// self-sufficient with no credential position. The bare word <c>Basic</c> and the enumeration member are code, which the verdict cannot read as a
	/// literal, so an indeterminate verdict here leaves the level alone (see <see cref="BuildExplicitSchemeInterpretation"/>).
	/// </summary>
	private static DetectionRule BuildPS003()
	{
		DetectionRule rule = new()
		{
			Expression = PowerShellPatternExpressions.ExplicitBasicAuthenticationParameterConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = BuildExplicitSchemeInterpretation(),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _PS_003_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildPS004()
	{
		return BuildHeaderRule(PowerShellPatternExpressions.AuthorizationHeaderHashtableLiteralConstruction(), _HEADER_CAPTURE_GROUP, _PS_004_DISPLAY_NAME);
	}

	private static DetectionRule BuildPS005()
	{
		return BuildHeaderRule(PowerShellPatternExpressions.AuthorizationHeaderStringKeyWriteConstruction(), _HEADER_CAPTURE_GROUP, _PS_005_DISPLAY_NAME);
	}

	private static DetectionRule BuildPS006()
	{
		return BuildHeaderRule(PowerShellPatternExpressions.AuthorizationHeaderEnumWriteConstruction(), _CONSTRUCT_CAPTURE_GROUP, _PS_006_DISPLAY_NAME);
	}

	private static DetectionRule BuildPS007()
	{
		return BuildHeaderRule(PowerShellPatternExpressions.AuthenticationHeaderValueSchemeConstruction(), _CONSTRUCT_CAPTURE_GROUP, _PS_007_DISPLAY_NAME);
	}

	/// <summary>A hand-built header establishes nothing about the destination and needs the call path: High base, Medium by composition, the scheme verdict applying.</summary>
	private static DetectionRule BuildHeaderRule(Regex expression, string anchorGroupName, string displayName)
	{
		DetectionRule rule = new()
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(anchorGroupName),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = BuildSchemeInterpretation(),
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName
		};

		return rule;
	}

	/// <summary>The scheme is an argument and the third argument is a credential by the call's own signature, so the class is self-sufficient.</summary>
	private static DetectionRule BuildPS008()
	{
		DetectionRule rule = new()
		{
			Expression = PowerShellPatternExpressions.CredentialCacheBasicSchemeConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = BuildSchemeInterpretation(),
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _PS_008_DISPLAY_NAME
		};

		return rule;
	}

	private static DetectionRule BuildPS009()
	{
		return BuildUnschemedRule(
			PowerShellPatternExpressions.NetworkCredentialOnHttpTypeConstruction(),
			_CONSTRUCT_CAPTURE_GROUP,
			ConfidenceLevel.Medium,
			CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			_PS_009_DISPLAY_NAME);
	}

	private static DetectionRule BuildPS010()
	{
		return BuildUnschemedRule(
			PowerShellPatternExpressions.Base64UserPasswordPairConstruction(),
			_ENCODER_CAPTURE_GROUP,
			ConfidenceLevel.Medium,
			CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			_PS_010_DISPLAY_NAME);
	}

	private static DetectionRule BuildPS011()
	{
		return BuildUnschemedRule(
			PowerShellPatternExpressions.TokenCarriedUnderBasicSchemeConstruction(),
			_ENCODER_CAPTURE_GROUP,
			ConfidenceLevel.Medium,
			CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			_PS_011_DISPLAY_NAME);
	}

	/// <summary>Low: the credential is passed and no scheme is, so as written the call does not send Basic to RelativityOne. No credential position.</summary>
	private static DetectionRule BuildPS012()
	{
		return BuildUnschemedRule(
			PowerShellPatternExpressions.CredentialOnWebCmdletWithoutSchemeConstruction(),
			_CONSTRUCT_CAPTURE_GROUP,
			ConfidenceLevel.Low,
			CredentialPosition.None(),
			_PS_012_DISPLAY_NAME);
	}

	/// <summary>A near-miss class: the credential position is read for the .NET sinks, and the cmdlet form never declares the group.</summary>
	private static DetectionRule BuildPS013()
	{
		return BuildUnschemedRule(
			PowerShellPatternExpressions.NonHttpCredentialSinkConstruction(),
			_CONSTRUCT_CAPTURE_GROUP,
			ConfidenceLevel.Low,
			CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			_PS_013_DISPLAY_NAME);
	}

	/// <summary>A definition and not a call site: Low, with no credential position.</summary>
	private static DetectionRule BuildPS014()
	{
		return BuildUnschemedRule(
			PowerShellPatternExpressions.AuthenticationHelperDefinition(),
			_DECLARATION_CAPTURE_GROUP,
			ConfidenceLevel.Low,
			CredentialPosition.None(),
			_PS_014_DISPLAY_NAME);
	}

	/// <summary>A class that needs the call path established and carries no scheme argument to resolve.</summary>
	private static DetectionRule BuildUnschemedRule(
		Regex expression,
		string anchorGroupName,
		ConfidenceLevel baseLevel,
		CredentialPosition credentialPosition,
		string displayName)
	{
		DetectionRule rule = new()
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(anchorGroupName),
			BaseLevel = baseLevel,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = credentialPosition,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName
		};

		return rule;
	}

	/// <summary>
	/// The scheme interpretation of the header classes and the credential cache: a scheme that resolves to Basic confirms, one that resolves to anything else
	/// suppresses the finding, and one that cannot be resolved proposes Medium and keeps it.
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
	/// The explicit parameter's interpretation. The bare word <c>Basic</c> and the enumeration member are written as code, so the verdict has no literal
	/// to read and calls them indeterminate; the expression matches those two forms on the text <c>Basic</c> itself and a bare <c>Bearer</c> or
	/// <c>OAuth</c> never matches at all. An indeterminate verdict therefore has no effect here. An assembled scheme (<c>'Ba' + 'sic'</c>) declares no
	/// scheme group, so the finding is kept and proposed at Medium.
	/// </summary>
	private static SymbolicVerdictInterpretation BuildExplicitSchemeInterpretation()
	{
		SymbolicVerdictInterpretation interpretation = new()
		{
			SchemeArgumentCaptureGroupName = _SCHEME_CAPTURE_GROUP,
			ExpectedSchemeToken = _EXPECTED_SCHEME_TOKEN,
			WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
			WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
			WhenIndeterminate = SymbolicVerdictResponse.NoEffect,
			WhenSchemeArgumentDidNotParticipate = SymbolicVerdictResponse.ProposesMediumLevel
		};

		return interpretation;
	}

	// ------------------------------------------------------------------------------------------
	// Element 4: demotion signals
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The language's own file signals, every one declared <c>Insensitive</c> because file and folder names are case-insensitive on Windows. The
	/// <c>Deployment</c> folder is not a signal.
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
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _TEST_FILE_SUFFIX, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Test },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _GENERATED_FILE_SUFFIX_GENERATED, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Generated },
			new FileNameSignal { Match = FileNameMatch.Suffix, Text = _GENERATED_FILE_SUFFIX_SHORT, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Generated },
			new FileNameSignal { Match = FileNameMatch.Prefix, Text = _SAMPLE_FILE_PREFIX_SAMPLE, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Sample },
			new FileNameSignal { Match = FileNameMatch.Prefix, Text = _SAMPLE_FILE_PREFIX_EXAMPLE, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Sample }
		}.ToFrozenSet();

		return signals;
	}

	private static FrozenSet<FolderNameSignal> BuildFolderNameSignals()
	{
		FrozenSet<FolderNameSignal> signals = new[]
		{
			NamedFolder(_TEST_FOLDER_NAME_TEST, DemotionKind.Test),
			NamedFolder(_TEST_FOLDER_NAME_TESTS, DemotionKind.Test),
			NamedFolder(_MOCK_FOLDER_NAME_MOCK, DemotionKind.Mock),
			NamedFolder(_MOCK_FOLDER_NAME_MOCKS, DemotionKind.Mock),
			NamedFolder(_FIXTURE_FOLDER_NAME_FIXTURE, DemotionKind.Test),
			NamedFolder(_FIXTURE_FOLDER_NAME_FIXTURES, DemotionKind.Test),
			NamedFolder(_SAMPLE_FOLDER_NAME_SAMPLE, DemotionKind.Sample),
			NamedFolder(_SAMPLE_FOLDER_NAME_SAMPLES, DemotionKind.Sample),
			NamedFolder(_SAMPLE_FOLDER_NAME_EXAMPLE, DemotionKind.Sample),
			NamedFolder(_SAMPLE_FOLDER_NAME_EXAMPLES, DemotionKind.Sample),
			new FolderNameSignal { FolderName = _VENDORED_FOLDER_NAME_VENDOR, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Vendored },
			new FolderNameSignal { FolderName = _VENDORED_FOLDER_NAME_THIRD_PARTY, Sensitivity = CaseSensitivity.Insensitive, Kind = DemotionKind.Vendored }
		}.ToFrozenSet();

		return signals;
	}

	private static FolderNameSignal NamedFolder(string folderName, DemotionKind kind)
	{
		return new FolderNameSignal { FolderName = folderName, Sensitivity = CaseSensitivity.Insensitive, Kind = kind };
	}

	private static ImmutableArray<ContentMarker> BuildContentMarkers()
	{
		ImmutableArray<ContentMarker> markers =
		[
			new ContentMarker
			{
				Expression = PowerShellPatternExpressions.GeneratedHeaderContentMarker(),
				Zone = ContentMarkerZone.AnyZone,
				Kind = DemotionKind.Generated
			},
			new ContentMarker
			{
				Expression = PowerShellPatternExpressions.PesterTestContentMarker(),
				Zone = ContentMarkerZone.CodeOnly,
				Kind = DemotionKind.Test
			}
		];

		return markers;
	}

	// ------------------------------------------------------------------------------------------
	// Element 5: preselection anchors, flow signals, recommendations
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The four anchors, all declared <c>Insensitive</c>: every class brings its own. <c>Basic</c> is not an anchor. Also declares the per-flow generic
	/// fallback text.
	/// </summary>
	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = new[]
			{
				InsensitiveAnchor(_ANCHOR_CREDENTIAL),
				InsensitiveAnchor(_ANCHOR_AUTHORIZATION),
				InsensitiveAnchor(_ANCHOR_AUTHENTICATION),
				InsensitiveAnchor(_ANCHOR_TO_BASE64_STRING)
			}.ToFrozenSet(),
			TemplateProvider = ProvideTemplate,
			FlowSignals = PowerShellFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = new Dictionary<CredentialFlow, string>
			{
				[CredentialFlow.ClientCredentials] = PowerShellRecommendationTemplates.POWERSHELL_LANGUAGE_FALLBACK_TEXT,
				[CredentialFlow.AuthorizationCode] = PowerShellRecommendationTemplates.POWERSHELL_CODE_LANGUAGE_FALLBACK_TEXT
			}.ToFrozenDictionary()
		};

		return element;
	}

	private static PreselectionAnchor InsensitiveAnchor(string text)
	{
		return new PreselectionAnchor { Text = text, Sensitivity = CaseSensitivity.Insensitive };
	}

	/// <summary>
	/// Eleven classes resolve to their entry for either flow; the three Low classes - and any identifier this analyzer does not declare - resolve to null,
	/// which routes a Low finding to manual review.
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

	private static FrozenDictionary<string, RecommendationTemplate> BuildTemplates(string tokenAcquisition)
	{
		Dictionary<string, RecommendationTemplate> templates = new(StringComparer.Ordinal)
		{
			[_PS_001] = Template(PowerShellRecommendationTemplates.PS_001_SOURCE_FORM, PowerShellRecommendationTemplates.PS_001_TARGET_FORM, PowerShellRecommendationTemplates.PS_001_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_001_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_002] = Template(PowerShellRecommendationTemplates.PS_002_SOURCE_FORM, PowerShellRecommendationTemplates.PS_002_TARGET_FORM, PowerShellRecommendationTemplates.PS_002_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_002_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_003] = Template(PowerShellRecommendationTemplates.PS_003_SOURCE_FORM, PowerShellRecommendationTemplates.PS_003_TARGET_FORM, PowerShellRecommendationTemplates.PS_003_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_003_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_004] = Template(PowerShellRecommendationTemplates.PS_004_SOURCE_FORM, PowerShellRecommendationTemplates.PS_004_TARGET_FORM, PowerShellRecommendationTemplates.PS_004_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_004_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_005] = Template(PowerShellRecommendationTemplates.PS_005_SOURCE_FORM, PowerShellRecommendationTemplates.PS_005_TARGET_FORM, PowerShellRecommendationTemplates.PS_005_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_005_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_006] = Template(PowerShellRecommendationTemplates.PS_006_SOURCE_FORM, PowerShellRecommendationTemplates.PS_006_TARGET_FORM, PowerShellRecommendationTemplates.PS_006_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_006_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_007] = Template(PowerShellRecommendationTemplates.PS_007_SOURCE_FORM, PowerShellRecommendationTemplates.PS_007_TARGET_FORM, PowerShellRecommendationTemplates.PS_007_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_007_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_008] = Template(PowerShellRecommendationTemplates.PS_008_SOURCE_FORM, PowerShellRecommendationTemplates.PS_008_TARGET_FORM, PowerShellRecommendationTemplates.PS_008_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_008_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_009] = Template(PowerShellRecommendationTemplates.PS_009_SOURCE_FORM, PowerShellRecommendationTemplates.PS_009_TARGET_FORM, PowerShellRecommendationTemplates.PS_009_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_009_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_010] = Template(PowerShellRecommendationTemplates.PS_010_SOURCE_FORM, PowerShellRecommendationTemplates.PS_010_TARGET_FORM, PowerShellRecommendationTemplates.PS_010_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_010_VERIFICATION_NOTE, tokenAcquisition),
			[_PS_011] = Template(PowerShellRecommendationTemplates.PS_011_SOURCE_FORM, PowerShellRecommendationTemplates.PS_011_TARGET_FORM, PowerShellRecommendationTemplates.PS_011_CREDENTIAL_PROVENANCE, PowerShellRecommendationTemplates.PS_011_VERIFICATION_NOTE, tokenAcquisition)
		};

		return templates.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>The credential provenance is the class's own share followed by part 2 for the flow, as two paragraphs.</summary>
	private static RecommendationTemplate Template(
		string sourceForm,
		string targetForm,
		string credentialProvenanceShare,
		string verificationNote,
		string tokenAcquisition)
	{
		return new RecommendationTemplate
		{
			GenericSourceForm = sourceForm,
			TargetFormUnderTokenAuthentication = targetForm,
			CredentialProvenance = string.Concat(credentialProvenanceShare, _PARAGRAPH_SEPARATOR, tokenAcquisition),
			VerificationNote = verificationNote
		};
	}

	// ------------------------------------------------------------------------------------------
	// Element 6: subsumption
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The declared triples. PS-002 subsumes PS-001 at identical anchor positions: both anchor on the same credential-type token. PS-003 subsumes PS-012
	/// at overlapping spans: a command that sends Basic explicitly is not also reported as a credential that sends nothing. PS-013 subsumes PS-009 at
	/// overlapping spans, because ambiguity resolves downward to the lower of two candidate levels.
	/// </summary>
	private static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		FrozenSet<SubsumptionRule> relation = new[]
		{
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _PS_002,
				SubsumedPatternClassIdentifier = _PS_001,
				When = SubsumptionCondition.AnchorPositionsAreIdentical
			},
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _PS_003,
				SubsumedPatternClassIdentifier = _PS_012,
				When = SubsumptionCondition.MatchSpansOverlap
			},
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _PS_013,
				SubsumedPatternClassIdentifier = _PS_009,
				When = SubsumptionCondition.MatchSpansOverlap
			},
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _PS_011,
				SubsumedPatternClassIdentifier = _PS_010,
				When = SubsumptionCondition.AnchorPositionsAreIdentical
			}
		}.ToFrozenSet();

		return relation;
	}
}
