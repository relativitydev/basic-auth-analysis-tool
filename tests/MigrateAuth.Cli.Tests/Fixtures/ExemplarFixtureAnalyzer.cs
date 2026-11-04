using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Recognition;

namespace MigrateAuth.Cli.Tests.Fixtures;

/// <summary>
/// Every expression this catalog hands over, generated at build time: the generator needs a
/// constant pattern and produces a method, not data, so a profile carrying expressions as strings
/// and a profile carrying build-time generated expressions cannot both exist. Under ahead-of-time
/// compilation the alternative is also interpreted rather than compiled.
/// </summary>
internal static partial class ExemplarPatternExpressions
{
	/// <summary>Declares the policy match timeout the validator requires of every detection rule.</summary>
	[GeneratedRegex(
		@"\b(?<construct>NewExemplarCredentials)\s*\(\s*(?<user>[A-Za-z_][A-Za-z0-9_]*)\s*,\s*(?<credential>""[^""\r\n]*""|[A-Za-z_][A-Za-z0-9_]*)\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex CredentialTypeConstruction();

	/// <summary>Declares the policy match timeout the validator requires of every detection rule.</summary>
	[GeneratedRegex(
		@"""(?<headerName>Authorization)""\s*,\s*(?<construct>BuildScheme)\s*\(\s*(?<scheme>[A-Za-z_][A-Za-z0-9_]*)\s*\)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex HandAssembledHeader();

	/// <summary>
	/// The overlapping construct subsumption exists for: a client built FROM a credential
	/// object, whose span necessarily contains the start of the credential construction the other
	/// rule recognizes.
	///
	/// Declares the policy match timeout the validator requires of every detection rule.
	/// </summary>
	[GeneratedRegex(
		@"\b(?<construct>NewExemplarClient)\s*\(\s*NewExemplarCredentials\s*\(",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex LegacyClientConstruction();

	/// <summary>Declares the policy match timeout, which <c>FileSymbolTable.TryCollect</c> brackets with the engine's own match-bound watch.</summary>
	[GeneratedRegex(
		@"^\s*const\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>[^""\r\n]*)""",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	[GeneratedRegex(
		@"<<(?<quoted>')?(?<terminator>[A-Za-z_][A-Za-z0-9_]*)'?",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex HereDocumentOpening();

	/// <summary>Declares the policy match timeout, which <c>FileDemotionSignals</c> brackets with the engine's own match-bound watch as a generation-marker expression.</summary>
	[GeneratedRegex(
		@"^--\s*generated\s+by\s+the\s+exemplar\s+generator\b",
		RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GenerationMarker();

	/// <summary>
	/// A content marker, distinct from <see cref="GenerationMarker"/>: the same
	/// generated-header wording, but constructed with the match-bound timeout every content marker
	/// declares and consumed as an <see cref="ContentMarkerZone.AnyZone"/> entry in
	/// <see cref="LanguageShapedDemotionSignals.ContentMarkers"/> rather than against a file name or
	/// a folder segment. The header opens with the language's own line-comment token, so it lands in
	/// <see cref="MigrateAuth.Cli.Lexical.LexicalZone.Comment"/> - which an
	/// <see cref="ContentMarkerZone.AnyZone"/> marker must reach, since a generated-code header is
	/// itself a comment.
	/// </summary>
	[GeneratedRegex(
		@"^--\s*generated\s+by\s+the\s+exemplar\s+generator\b",
		RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex GeneratedHeaderContentMarker();

	/// <summary>
	/// A test-attribute-like content marker, <see cref="ContentMarkerZone.CodeOnly"/>:
	/// this invented token must demote a file when it sits on executable code and must NOT demote
	/// one when it sits inside a comment or a string literal, which is exactly what distinguishes
	/// <see cref="ContentMarkerZone.CodeOnly"/> from <see cref="ContentMarkerZone.AnyZone"/>.
	/// </summary>
	[GeneratedRegex(
		@"\bExemplarTestMarker\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex TestAttributeContentMarker();

	[GeneratedRegex(
		@"^#!.*\bexemplar\b",
		RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex FirstLineDetection();

	/// <summary>
	/// The identifier-extent interpolation hole. The hole has no closing token at all: its
	/// end is wherever this expression stops matching from the character after the sigil.
	/// </summary>
	[GeneratedRegex(
		@"[A-Za-z_][A-Za-z0-9_]*",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex InterpolatedIdentifier();
}

/// <summary>
/// The reference analyzer, registered in-process by tests through <see cref="ILanguageAnalyzer"/>. It lives
/// in the test project on purpose: the embedded catalog is exactly <c>CSharp</c> in every build,
/// and this type holds the only declarations of the three pre-budget expressions.
/// </summary>
public sealed class ExemplarFixtureAnalyzer : ILanguageAnalyzer
{
	private const string _PROFILE_NAME = "Exemplar";
	private const string _FIRST_LANGUAGE_NAME = "ExemplarOne";
	private const string _SECOND_LANGUAGE_NAME = "ExemplarTwo";
	private const string _FIRST_LANGUAGE_FILE_EXTENSION = ".ex1";
	private const string _SECOND_LANGUAGE_FILE_EXTENSION = ".ex2";

	private const string _CREDENTIAL_TYPE_PATTERN_CLASS = "EXM-001";
	private const string _HAND_ASSEMBLED_HEADER_PATTERN_CLASS = "EXM-002";
	private const string _LEGACY_CLIENT_PATTERN_CLASS = "EXM-003";

	/// <summary>One display name per pattern class, unique within this analyzer.</summary>
	private const string _CREDENTIAL_TYPE_DISPLAY_NAME = "Credential object construction";

	private const string _HAND_ASSEMBLED_HEADER_DISPLAY_NAME = "Hand-assembled authorization header";
	private const string _LEGACY_CLIENT_DISPLAY_NAME = "Legacy client built from a credential object";

	/// <summary>
	/// Two invented, generic constructs this reference grammar does not attempt to
	/// recognize - more than one, so a consumer sorting them ordinally by
	/// <see cref="DeclaredUnsupportedCoverage.Construct"/> has something to sort.
	/// </summary>
	private const string _INDIRECT_ASSIGNMENT_CONSTRUCT =
		"a credential value assigned through an intermediate variable several statements before use";

	private const string _INDIRECT_ASSIGNMENT_REASON =
		"resolving it requires flow analysis this engine does not provide";

	private const string _REFLECTIVE_HEADER_CONSTRUCT =
		"an authorization header name or scheme token built through a dynamically resolved member rather than written literally";

	private const string _REFLECTIVE_HEADER_REASON =
		"the lexical scanner anchors on literal text, and a dynamically resolved token is not literal text";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _SCHEME_CAPTURE_GROUP = "scheme";
	private const string _TERMINATOR_CAPTURE_GROUP = "terminator";
	private const string _QUOTED_TERMINATOR_CAPTURE_GROUP = "quoted";

	private const string _EXPECTED_SCHEME_TOKEN = "Basic";
	private const string _CREDENTIAL_TYPE_ANCHOR_TEXT = "NewExemplarCredentials";
	private const string _HEADER_ANCHOR_TEXT = "Authorization";

	private const string _TEST_FILE_NAME_SUFFIX = ".spec.ex1";
	private const string _GENERATED_FOLDER_NAME = "exemplar-generated";

	private const string _LINE_COMMENT_OPENING = "--";
	private const string _WORD_LINE_COMMENT_OPENING = "REM";
	private const string _BLOCK_COMMENT_OPENING = "<#";
	private const string _BLOCK_COMMENT_CLOSING = "#>";
	private const string _SINGLE_QUOTE = "'";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _RAW_QUOTE = "```";
	private const string _REGULAR_EXPRESSION_DELIMITER = "/";
	private const string _HERE_DOCUMENT_OPENING = "<<";
	private const string _CLOSING_IS_CAPTURED_AT_THE_OPENING = "";
	private const string _INTERPOLATION_OPENING = "${";
	private const string _INTERPOLATION_CLOSING = "}";
	private const string _IDENTIFIER_INTERPOLATION_SIGIL = "$";
	private const string _RAW_LITERAL_PREFIX = "r";
	private const string _CHARACTER_CLASS_OPENING = "[";
	private const string _CHARACTER_CLASS_CLOSING = "]";
	private const char _ESCAPE_CHARACTER = '\\';
	private const char _WORD_CONNECTOR_CHARACTER = '_';

	private static readonly FrozenDictionary<string, RecommendationTemplate> _templatesByPatternClass =
		BuildTemplates();

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = BuildDemotionSignals();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } =
		BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = BuildSubsumptionRelation();

	private static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = ExemplarPatternExpressions.ConstantDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Insensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Insensitive,
			LineCommentRules = BuildLineCommentRules(),
			BlockCommentRules = BuildBlockCommentRules(),
			QuoteRules = BuildQuoteRules()
		};

		return profile;
	}

	private static ImmutableArray<LineCommentRule> BuildLineCommentRules()
	{
		ImmutableArray<LineCommentRule> rules =
		[
			new LineCommentRule { Opening = _LINE_COMMENT_OPENING },
			new LineCommentRule { Opening = _WORD_LINE_COMMENT_OPENING, RequiresWordBoundary = true }
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
	/// Deliberately written shortest opening first. The engine reorders it, and the test that
	/// proves it does is the only reason the order here is wrong on purpose.
	/// </summary>
	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		ImmutableArray<QuoteRule> rules =
		[
			BuildDoubledCloseLiteralRule(),
			BuildInterpolatingLiteralRule(),
			BuildRegularExpressionLiteralRule(),
			BuildRawLiteralRule(),
			BuildHereDocumentRule()
		];

		return rules;
	}

	/// <summary>Doubled close, and a newline policy that forbids continuation outright.</summary>
	private static QuoteRule BuildDoubledCloseLiteralRule()
	{
		QuoteRule rule = new()
		{
			Opening = _SINGLE_QUOTE,
			Closing = _SINGLE_QUOTE,
			NewlinePolicy = NewlinePolicy.Forbidden,
			DoubledClosingEscapes = true
		};

		return rule;
	}

	/// <summary>Literal prefix and interpolation, over a continued newline policy.</summary>
	private static QuoteRule BuildInterpolatingLiteralRule()
	{
		QuoteRule rule = new()
		{
			Opening = _DOUBLE_QUOTE,
			Closing = _DOUBLE_QUOTE,
			NewlinePolicy = NewlinePolicy.EscapedContinues,
			EscapeCharacter = _ESCAPE_CHARACTER,
			InterpolationOpening = _INTERPOLATION_OPENING,
			InterpolationClosing = _INTERPOLATION_CLOSING,
			LiteralPrefixes =
			[
				new LiteralPrefixRule
				{
					Prefix = _RAW_LITERAL_PREFIX,
					EscapeHandling = EscapeHandling.Suppressed,
					Sensitivity = CaseSensitivity.Insensitive
				}
			]
		};

		return rule;
	}

	/// <summary>Variable-length delimiter, with newlines as content.</summary>
	private static QuoteRule BuildRawLiteralRule()
	{
		QuoteRule rule = new()
		{
			Opening = _RAW_QUOTE,
			Closing = _RAW_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			DelimiterLength = DelimiterLength.VariableWithAtLeastEqualClosing
		};

		return rule;
	}

	/// <summary>Delimiter context predicate, and suspended sub-range.</summary>
	private static QuoteRule BuildRegularExpressionLiteralRule()
	{
		QuoteRule rule = new()
		{
			Opening = _REGULAR_EXPRESSION_DELIMITER,
			Closing = _REGULAR_EXPRESSION_DELIMITER,
			NewlinePolicy = NewlinePolicy.Forbidden,
			EscapeCharacter = _ESCAPE_CHARACTER,
			OpeningContextPredicate = OpensARegularExpressionLiteral,
			SuspendedSubRange = new SuspendedSubRange
			{
				Opening = _CHARACTER_CLASS_OPENING,
				Closing = _CHARACTER_CLASS_CLOSING
			}
		};

		return rule;
	}

	/// <summary>
	/// Positional anchoring, captured terminator, and the SECOND interpolation
	/// hole kind - a sigil and an identifier, with no closing token.
	///
	/// The three belong on one rule because they are one construct. The captured terminator says
	/// the closing is whatever the author wrote at the opening, and that the body interpolates
	/// only where the author did not quote that token; the identifier-extent hole is what
	/// "interpolates" then MEANS for this construct.
	/// </summary>
	private static QuoteRule BuildHereDocumentRule()
	{
		QuoteRule rule = new()
		{
			Opening = _HERE_DOCUMENT_OPENING,
			Closing = _CLOSING_IS_CAPTURED_AT_THE_OPENING,
			NewlinePolicy = NewlinePolicy.Allowed,
			OpeningAnchoring = PositionalAnchoring.EndOfLine,
			ClosingAnchoring = PositionalAnchoring.StartOfLine,
			InterpolationOpening = _IDENTIFIER_INTERPOLATION_SIGIL,
			InterpolationIdentifierExtent = ExemplarPatternExpressions.InterpolatedIdentifier(),
			CapturedTerminator = new CapturedTerminator
			{
				OpeningExpression = ExemplarPatternExpressions.HereDocumentOpening(),
				TerminatorCaptureGroupName = _TERMINATOR_CAPTURE_GROUP,
				NonInterpolatingQuoteCaptureGroupName = _QUOTED_TERMINATOR_CAPTURE_GROUP
			}
		};

		return rule;
	}

	/// <summary>
	/// The context predicate in its preceding-token form: a slash opens a literal only
	/// where what precedes it cannot end a value, which is what separates it from division.
	/// </summary>
	private static bool OpensARegularExpressionLiteral(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int index = delimiterPosition - 1;

		while (index >= 0 && char.IsWhiteSpace(text[index]))
		{
			index--;
		}

		bool opensLiteral = index < 0 || !EndsAValue(text[index]);

		return opensLiteral;
	}

	private static bool EndsAValue(char character)
	{
		bool endsValue =
			char.IsLetterOrDigit(character)
			|| character == _WORD_CONNECTOR_CHARACTER
			|| character == ')'
			|| character == ']'
			|| character == '"';

		return endsValue;
	}

	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _FIRST_LANGUAGE_FILE_EXTENSION, _SECOND_LANGUAGE_FILE_EXTENSION }
				.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _FIRST_LANGUAGE_NAME, _SECOND_LANGUAGE_NAME }
				.ToFrozenSet(StringComparer.Ordinal),
			FirstLineDetectionExpression = ExemplarPatternExpressions.FirstLineDetection(),
			LanguageNameDerivation = DeriveLanguageName,
			DeclaredUnsupportedCoverage = BuildDeclaredUnsupportedCoverage()
		};

		return identification;
	}

	/// <summary>
	/// Two invented constructs, so both languages this analyzer declares - the
	/// not-one-to-one profile-to-language relation - show the same entries: the coverage is a
	/// property of this analyzer's catalog, not of either declared name on its own.
	/// </summary>
	private static FrozenSet<DeclaredUnsupportedCoverage> BuildDeclaredUnsupportedCoverage()
	{
		FrozenSet<DeclaredUnsupportedCoverage> coverage = new[]
		{
			new DeclaredUnsupportedCoverage
			{
				Construct = _INDIRECT_ASSIGNMENT_CONSTRUCT,
				Reason = _INDIRECT_ASSIGNMENT_REASON
			},
			new DeclaredUnsupportedCoverage
			{
				Construct = _REFLECTIVE_HEADER_CONSTRUCT,
				Reason = _REFLECTIVE_HEADER_REASON
			}
		}.ToFrozenSet();

		return coverage;
	}

	/// <summary>
	/// Two languages share this one profile, so the language a finding is
	/// emitted under is derived here rather than read off the profile. The first line wins
	/// over the extension, which is what the file-attribution rule asks for.
	/// </summary>
	private static string DeriveLanguageName(string fileExtension, string? firstLine)
	{
		bool firstLineClaimsTheFirstLanguage =
			firstLine is not null && ExemplarPatternExpressions.FirstLineDetection().IsMatch(firstLine);

		bool isSecondLanguage =
			!firstLineClaimsTheFirstLanguage
			&& string.Equals(fileExtension, _SECOND_LANGUAGE_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase);

		string languageName = isSecondLanguage ? _SECOND_LANGUAGE_NAME : _FIRST_LANGUAGE_NAME;

		return languageName;
	}

	private static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			[_CREDENTIAL_TYPE_PATTERN_CLASS] = BuildCredentialTypeRule(),
			[_HAND_ASSEMBLED_HEADER_PATTERN_CLASS] = BuildHandAssembledHeaderRule(),
			[_LEGACY_CLIENT_PATTERN_CLASS] = BuildLegacyClientRule()
		};

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>
	/// Anchored at its type name and carrying its credential at argument two, which is why the
	/// credential position cannot be the anchor offset.
	/// </summary>
	private static DetectionRule BuildCredentialTypeRule()
	{
		DetectionRule rule = new()
		{
			Expression = ExemplarPatternExpressions.CredentialTypeConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = _CREDENTIAL_TYPE_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// The case the anchor position exists for: the expression begins on the opening quote
	/// of a header name, so the start of the match lies inside a literal, while the anchor
	/// lies on executable code further along.
	/// </summary>
	private static DetectionRule BuildHandAssembledHeaderRule()
	{
		DetectionRule rule = new()
		{
			Expression = ExemplarPatternExpressions.HandAssembledHeader(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Medium,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = new SymbolicVerdictInterpretation
			{
				SchemeArgumentCaptureGroupName = _SCHEME_CAPTURE_GROUP,
				ExpectedSchemeToken = _EXPECTED_SCHEME_TOKEN,
				WhenResolvedMatching = SymbolicVerdictResponse.ConfirmsPatternClass,
				WhenResolvedNotMatching = SymbolicVerdictResponse.SuppressesFinding,
				WhenIndeterminate = SymbolicVerdictResponse.ProposesMediumLevel
			},
			CredentialPosition = CredentialPosition.None(),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _HAND_ASSEMBLED_HEADER_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>
	/// The overlapping class: a legacy client constructed with a credential object
	/// matches both the client class and the credential class. Its span contains the start of the
	/// credential construction, so one planted pattern is recognized twice unless the subsumption
	/// relation below says which of the two is the one to emit.
	///
	/// It declares no preselection anchor of its own, and that is not an omission: the expression
	/// literally requires the credential type's own name, so the anchor already declared for that
	/// class selects every file this one could ever match, and a second anchor would only widen the
	/// candidate set with files this class cannot fire on.
	/// </summary>
	private static DetectionRule BuildLegacyClientRule()
	{
		DetectionRule rule = new()
		{
			Expression = ExemplarPatternExpressions.LegacyClientConstruction(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.None(),
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = _LEGACY_CLIENT_DISPLAY_NAME
		};

		return rule;
	}

	private static LanguageShapedDemotionSignals BuildDemotionSignals()
	{
		LanguageShapedDemotionSignals signals = new()
		{
			FileNameSignals = new[]
			{
				new FileNameSignal
				{
					Match = FileNameMatch.Suffix,
					Text = _TEST_FILE_NAME_SUFFIX,
					Sensitivity = CaseSensitivity.Insensitive
				}
			}.ToFrozenSet(),
			FolderNameSignals = new[]
			{
				new FolderNameSignal
				{
					FolderName = _GENERATED_FOLDER_NAME,
					Sensitivity = CaseSensitivity.Insensitive
				}
			}.ToFrozenSet(),
			GenerationMarkerExpressions = [ExemplarPatternExpressions.GenerationMarker()],
			ContentMarkers = BuildContentMarkers()
		};

		return signals;
	}

	/// <summary>
	/// One <see cref="ContentMarkerZone.AnyZone"/> entry and one
	/// <see cref="ContentMarkerZone.CodeOnly"/> entry, so an integration test reaching this
	/// analyzer through <c>RecognitionHarness</c> can exercise both zones against a real scan
	/// rather than a fabricated mask.
	/// </summary>
	private static ImmutableArray<ContentMarker> BuildContentMarkers()
	{
		ImmutableArray<ContentMarker> markers =
		[
			new ContentMarker
			{
				Expression = ExemplarPatternExpressions.GeneratedHeaderContentMarker(),
				Zone = ContentMarkerZone.AnyZone
			},
			new ContentMarker
			{
				Expression = ExemplarPatternExpressions.TestAttributeContentMarker(),
				Zone = ContentMarkerZone.CodeOnly
			}
		];

		return markers;
	}

	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = new[]
			{
				new PreselectionAnchor { Text = _CREDENTIAL_TYPE_ANCHOR_TEXT },
				new PreselectionAnchor { Text = _HEADER_ANCHOR_TEXT, Sensitivity = CaseSensitivity.Insensitive }
			}.ToFrozenSet(),
			TemplateProvider = ProvideTemplate,
			FlowSignals = []
		};

		return element;
	}

	private static RecommendationTemplate? ProvideTemplate(string patternClassIdentifier, CredentialFlow flow)
	{
		RecommendationTemplate? template = null;

		if (flow is CredentialFlow.ClientCredentials)
		{
			_templatesByPatternClass.TryGetValue(patternClassIdentifier, out template);
		}

		return template;
	}

	private static FrozenDictionary<string, RecommendationTemplate> BuildTemplates()
	{
		Dictionary<string, RecommendationTemplate> templates = new(StringComparer.Ordinal)
		{
			[_CREDENTIAL_TYPE_PATTERN_CLASS] = new RecommendationTemplate
			{
				GenericSourceForm = "a credential object built from a user name and a password",
				TargetFormUnderTokenAuthentication = "a bearer token obtained from the identity endpoint",
				CredentialProvenance = "the process environment or a secret store, never source",
				VerificationNote = "confirm the request still authenticates after the change"
			},
			[_HAND_ASSEMBLED_HEADER_PATTERN_CLASS] = new RecommendationTemplate
			{
				GenericSourceForm = "an authorization header assembled by hand from a scheme token",
				TargetFormUnderTokenAuthentication = "an authorization header carrying a bearer token",
				CredentialProvenance = "the process environment or a secret store, never source",
				VerificationNote = "confirm the header is accepted by the destination after the change"
			},
			[_LEGACY_CLIENT_PATTERN_CLASS] = new RecommendationTemplate
			{
				GenericSourceForm = "a client constructed directly from a credential object",
				TargetFormUnderTokenAuthentication = "a client configured with a token provider",
				CredentialProvenance = "the process environment or a secret store, never source",
				VerificationNote = "confirm the client still authenticates after the change"
			}
		};

		return templates.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>
	/// The subsumption relations. Two declared triples, and the pair that is deliberately NOT declared.
	///
	/// The legacy client subsumes the credential type, the one relation this profile can actually
	/// exercise: the client's span contains the start of the credential construction, so both classes
	/// recognize one planted pattern and exactly one of them is emitted.
	///
	/// The credential type subsumes the hand-assembled header on the same terms. Nothing is
	/// declared between the legacy client and the hand-assembled header: absent a declared relation
	/// both classes are emitted, so an omission is visible in the output rather than silent.
	/// </summary>
	private static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		FrozenSet<SubsumptionRule> relation = new[]
		{
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _CREDENTIAL_TYPE_PATTERN_CLASS,
				SubsumedPatternClassIdentifier = _HAND_ASSEMBLED_HEADER_PATTERN_CLASS,
				When = SubsumptionCondition.MatchSpansOverlap
			},
			new SubsumptionRule
			{
				SubsumingPatternClassIdentifier = _LEGACY_CLIENT_PATTERN_CLASS,
				SubsumedPatternClassIdentifier = _CREDENTIAL_TYPE_PATTERN_CLASS,
				When = SubsumptionCondition.MatchSpansOverlap
			}
		}.ToFrozenSet();

		return relation;
	}
}
