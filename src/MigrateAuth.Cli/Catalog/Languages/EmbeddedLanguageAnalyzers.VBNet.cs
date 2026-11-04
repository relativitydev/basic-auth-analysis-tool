using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `VBNet` analyzer. Nineteen classes, seventeen of which can reach High or Medium and two of
/// which are Low by construction; twenty flow signals; and the lexical profile of a language whose comments open with an apostrophe or the word
/// <c>REM</c>, whose strings double their closing quote and take newlines, and whose names do not depend on case.
///
/// FILES. The analyzer type is <c>file</c>-scoped, so its expressions live in <see cref="VBNetPatternExpressions"/> (three files), the flow-signal
/// declarations in <see cref="VBNetFlowSignalDeclarations"/> and the declared unsupported coverage in <see cref="VBNetDeclaredCoverage"/>. The
/// registration is the one line in <see cref="EmbeddedLanguageAnalyzers.CreateAll"/> and the partial method below.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddVBNetAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new VBNetAnalyzer());
	}
}

/// <summary>
/// The `VBNet` analyzer. File-local per the registration pattern every embedded language follows, so no engine type can name it and no special
/// case can be written against it.
/// </summary>
file sealed class VBNetAnalyzer : ILanguageAnalyzer
{
	private const string _PROFILE_NAME = "VBNet";
	private const string _LANGUAGE_NAME = "VBNet";
	private const string _FILE_EXTENSION = ".vb";

	private const string _VB_001 = "VB-001";
	private const string _VB_002 = "VB-002";
	private const string _VB_003 = "VB-003";
	private const string _VB_004 = "VB-004";
	private const string _VB_005 = "VB-005";
	private const string _VB_006 = "VB-006";
	private const string _VB_007 = "VB-007";
	private const string _VB_008 = "VB-008";
	private const string _VB_009 = "VB-009";
	private const string _VB_010 = "VB-010";
	private const string _VB_011 = "VB-011";
	private const string _VB_012 = "VB-012";
	private const string _VB_013 = "VB-013";
	private const string _VB_014 = "VB-014";
	private const string _VB_015 = "VB-015";
	private const string _VB_016 = "VB-016";
	private const string _VB_017 = "VB-017";
	private const string _VB_018 = "VB-018";
	private const string _VB_019 = "VB-019";

	private const string _PARAGRAPH_SEPARATOR = "\n\n";

	private const string _VB_001_DISPLAY_NAME = "SdkUserNamePasswordCredentials";
	private const string _VB_002_DISPLAY_NAME = "SdkLegacyRsapiCredentials";
	private const string _VB_003_DISPLAY_NAME = "CredentialCacheBasicScheme";
	private const string _VB_004_DISPLAY_NAME = "ThirdPartyBasicAuthenticatorType";
	private const string _VB_005_DISPLAY_NAME = "SchemeNamingExtensionMethod";
	private const string _VB_006_DISPLAY_NAME = "AuthorizationHeaderValueScheme";
	private const string _VB_007_DISPLAY_NAME = "AuthorizationHeaderWrite";
	private const string _VB_008_DISPLAY_NAME = "HttpRequestHeaderEnumAuthorization";
	private const string _VB_009_DISPLAY_NAME = "WcfBasicTransportCredential";
	private const string _VB_010_DISPLAY_NAME = "SdkConstructorWithUserAndPassword";
	private const string _VB_011_DISPLAY_NAME = "SdkSettingsCredentialProperties";
	private const string _VB_012_DISPLAY_NAME = "NetworkCredentialOnHttpType";
	private const string _VB_013_DISPLAY_NAME = "NetworkCredentialConstruction";
	private const string _VB_014_DISPLAY_NAME = "Base64UserPasswordPair";
	private const string _VB_015_DISPLAY_NAME = "TokenCarriedUnderBasicScheme";
	private const string _VB_016_DISPLAY_NAME = "NetworkTransferWithCredentials";
	private const string _VB_017_DISPLAY_NAME = "LateBoundHttpObjectCredentials";
	private const string _VB_018_DISPLAY_NAME = "NonHttpCredentialSink";
	private const string _VB_019_DISPLAY_NAME = "AuthenticationHelperDefinition";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _HEADER_CAPTURE_GROUP = "header";
	private const string _ENCODER_CAPTURE_GROUP = "encoder";
	private const string _DECLARATION_CAPTURE_GROUP = "declaration";
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _SCHEME_CAPTURE_GROUP = "scheme";

	private const string _EXPECTED_SCHEME_TOKEN = "Basic";

	private const string _APOSTROPHE = "'";
	private const string _TYPOGRAPHIC_APOSTROPHE_LEFT = "‘";
	private const string _TYPOGRAPHIC_APOSTROPHE_RIGHT = "’";
	private const string _REMARK_KEYWORD = "REM";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _INTERPOLATED_OPENING = "$\"";
	private const string _TYPOGRAPHIC_DOUBLE_LEFT = "“";
	private const string _TYPOGRAPHIC_DOUBLE_RIGHT = "”";
	private const string _HOLE_OPENING = "{";
	private const string _HOLE_CLOSING = "}";
	private const char _BRACKETED_NAME_OPENING = '[';

	private const string _TESTS_FILE_SUFFIX = "Tests.vb";
	private const string _DOTTED_TEST_FILE_SUFFIX = ".Test.vb";
	private const string _SPEC_FILE_SUFFIX = ".Spec.vb";
	private const string _DESIGNER_FILE_SUFFIX = ".Designer.vb";
	private const string _SHORT_GENERATED_FILE_SUFFIX = ".g.vb";
	private const string _GENERATED_FILE_SUFFIX = ".generated.vb";
	private const string _FAKE_FILE_SUFFIX = "Fake.vb";
	private const string _MOCK_FILE_SUFFIX = "Mock.vb";
	private const string _STUB_FILE_SUFFIX = "Stub.vb";
	private const string _FAKE_FILE_PREFIX = "Fake";
	private const string _MOCK_FILE_PREFIX = "Mock";
	private const string _STUB_FILE_PREFIX = "Stub";
	private const string _SAMPLE_FILE_PREFIX = "Sample";
	private const string _EXAMPLE_FILE_PREFIX = "Example";

	private const string _TEST_FOLDER_NAME = "test";
	private const string _TESTS_FOLDER_NAME = "tests";
	private const string _SPEC_FOLDER_NAME = "spec";
	private const string _FIXTURES_FOLDER_NAME = "fixtures";
	private const string _SAMPLE_FOLDER_NAME = "sample";
	private const string _SAMPLES_FOLDER_NAME = "samples";
	private const string _EXAMPLE_FOLDER_NAME = "example";
	private const string _EXAMPLES_FOLDER_NAME = "examples";
	private const string _VENDOR_FOLDER_NAME = "vendor";
	private const string _THIRD_PARTY_FOLDER_NAME = "third-party";
	private const string _SERVICE_REFERENCES_FOLDER_NAME = "Service References";
	private const string _WEB_REFERENCES_FOLDER_NAME = "Web References";
	private const string _TEST_FOLDER_SUFFIX_TEST = ".Test";
	private const string _TEST_FOLDER_SUFFIX_TESTS = ".Tests";

	private const string _ANCHOR_CREDENTIAL = "Credential";
	private const string _ANCHOR_AUTHORIZATION = "Authorization";
	private const string _ANCHOR_AUTHENTICATOR = "Authenticator";
	private const string _ANCHOR_TO_BASE64_STRING = "ToBase64String";
	private const string _ANCHOR_WITH_BASIC_AUTH = "WithBasicAuth";
	private const string _ANCHOR_IMPORT_API = "ImportAPI";
	private const string _ANCHOR_RELATIVITY_USERNAME = "RelativityUsername";
	private const string _ANCHOR_RELATIVITY_PASSWORD = "RelativityPassword";
	private const string _ANCHOR_CREATE_OBJECT = "CreateObject";
	private const string _ANCHOR_DOWNLOAD_FILE = "DownloadFile";
	private const string _ANCHOR_UPLOAD_FILE = "UploadFile";

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = BuildDemotionSignals();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } =
		BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = BuildSubsumptionRelation();

	/// <summary>Built once: seventeen entries, one per class that advises, whose credential provenance ends with part 2 for Client Credentials.</summary>
	private static readonly FrozenDictionary<string, RecommendationTemplate> _clientCredentialsTemplatesByPatternClass =
		BuildTemplates(VBNetRecommendationTemplates.CLIENT_CREDENTIALS_TOKEN_ACQUISITION);

	/// <summary>The same seventeen classes for Authorization Code: part 1 is identical and the credential provenance ends with part 2 for Authorization Code.</summary>
	private static readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplatesByPatternClass =
		BuildTemplates(VBNetRecommendationTemplates.AUTHORIZATION_CODE_TOKEN_ACQUISITION);

	// ------------------------------------------------------------------------------------------
	// Element 1: the lexical profile
	// ------------------------------------------------------------------------------------------

	private static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = VBNetPatternExpressions.ConstantDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Insensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Insensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Insensitive,
			LineCommentRules = BuildLineCommentRules(),
			BlockCommentRules = [],
			QuoteRules = BuildQuoteRules()
		};

		return profile;
	}

	/// <summary>
	/// The apostrophe and its two typographic spellings open a comment to the end of the line; <c>REM</c> opens one as a whole word wherever it stands, a
	/// bracketed name excepted. The fullwidth apostrophe is declared unsupported by the catalog and has no rule.
	/// </summary>
	private static ImmutableArray<LineCommentRule> BuildLineCommentRules()
	{
		ImmutableArray<LineCommentRule> rules =
		[
			new LineCommentRule { Opening = _APOSTROPHE },
			new LineCommentRule { Opening = _TYPOGRAPHIC_APOSTROPHE_LEFT },
			new LineCommentRule { Opening = _TYPOGRAPHIC_APOSTROPHE_RIGHT },
			new LineCommentRule { Opening = _REMARK_KEYWORD, RequiresWordBoundary = true, OpeningContextPredicate = RemarkIsNotABracketedName }
		];

		return rules;
	}

	/// <summary>
	/// Four quote rules. The plain literal and the interpolated one double their closing quote, take newlines as content and have no escape character; the
	/// interpolated one has one bracketed hole whose nested braces are counted, and reads a doubled opening brace as literal text. The two typographic
	/// pairs read as the plain literal; the right double quotation mark also opens, as the compiler allows.
	/// </summary>
	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		ImmutableArray<QuoteRule> rules =
		[
			BuildLiteralRule(_DOUBLE_QUOTE, _DOUBLE_QUOTE),
			BuildInterpolatedRule(),
			BuildLiteralRule(_TYPOGRAPHIC_DOUBLE_LEFT, _TYPOGRAPHIC_DOUBLE_RIGHT),
			BuildLiteralRule(_TYPOGRAPHIC_DOUBLE_RIGHT, _TYPOGRAPHIC_DOUBLE_RIGHT)
		];

		return rules;
	}

	private static QuoteRule BuildLiteralRule(string opening, string closing)
	{
		QuoteRule rule = new()
		{
			Opening = opening,
			Closing = closing,
			NewlinePolicy = NewlinePolicy.Allowed,
			DoubledClosingEscapes = true
		};

		return rule;
	}

	private static QuoteRule BuildInterpolatedRule()
	{
		QuoteRule rule = new()
		{
			Opening = _INTERPOLATED_OPENING,
			Closing = _DOUBLE_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			DoubledClosingEscapes = true,
			InterpolationOpening = _HOLE_OPENING,
			InterpolationClosing = _HOLE_CLOSING,
			InterpolationNestingOpening = _HOLE_OPENING,
			DoubledInterpolationOpeningIsLiteral = true
		};

		return rule;
	}

	/// <summary>
	/// <c>REM</c> opens a comment wherever it stands as a whole word outside brackets, after a dot included; it is a name only as <c>[Rem]</c>, so the
	/// one position that does not open is the one right after an opening square bracket.
	/// </summary>
	private static bool RemarkIsNotABracketedName(ReadOnlySpan<char> text, int delimiterPosition)
	{
		return delimiterPosition == 0 || text[delimiterPosition - 1] != _BRACKETED_NAME_OPENING;
	}

	// ------------------------------------------------------------------------------------------
	// Element 2: identification and declared unsupported coverage
	// ------------------------------------------------------------------------------------------

	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _FILE_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = VBNetDeclaredCoverage.Build()
		};

		return identification;
	}

	// ------------------------------------------------------------------------------------------
	// Element 3: the detection rules
	// ------------------------------------------------------------------------------------------

	private static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			[_VB_001] = BuildSdkRule(VBNetPatternExpressions.SdkUserNamePasswordCredentials(), _VB_001_DISPLAY_NAME),
			[_VB_002] = BuildSdkRule(VBNetPatternExpressions.SdkLegacyRsapiCredentials(), _VB_002_DISPLAY_NAME),
			[_VB_003] = BuildVB003(),
			[_VB_004] = BuildVB004(),
			[_VB_005] = BuildVB005(),
			[_VB_006] = BuildHeaderRule(VBNetPatternExpressions.AuthorizationHeaderValueScheme(), _CONSTRUCT_CAPTURE_GROUP, _VB_006_DISPLAY_NAME),
			[_VB_007] = BuildHeaderRule(VBNetPatternExpressions.AuthorizationHeaderWrite(), _HEADER_CAPTURE_GROUP, _VB_007_DISPLAY_NAME),
			[_VB_008] = BuildHeaderRule(VBNetPatternExpressions.HttpRequestHeaderEnumAuthorization(), _CONSTRUCT_CAPTURE_GROUP, _VB_008_DISPLAY_NAME),
			[_VB_009] = BuildVB009(),
			[_VB_010] = BuildSdkRule(VBNetPatternExpressions.SdkConstructorWithUserAndPassword(), _VB_010_DISPLAY_NAME),
			[_VB_011] = BuildSdkRule(VBNetPatternExpressions.SdkSettingsCredentialProperties(), _VB_011_DISPLAY_NAME),
			[_VB_012] = BuildUnschemedRule(VBNetPatternExpressions.NetworkCredentialOnHttpType(), _CONSTRUCT_CAPTURE_GROUP, ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), _VB_012_DISPLAY_NAME),
			[_VB_013] = BuildUnschemedRule(VBNetPatternExpressions.NetworkCredentialConstruction(), _CONSTRUCT_CAPTURE_GROUP, ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), _VB_013_DISPLAY_NAME),
			[_VB_014] = BuildUnschemedRule(VBNetPatternExpressions.Base64UserPasswordPair(), _ENCODER_CAPTURE_GROUP, ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), _VB_014_DISPLAY_NAME),
			[_VB_015] = BuildUnschemedRule(VBNetPatternExpressions.TokenCarriedUnderBasicScheme(), _ENCODER_CAPTURE_GROUP, ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), _VB_015_DISPLAY_NAME),
			[_VB_016] = BuildUnschemedRule(VBNetPatternExpressions.NetworkTransferWithCredentials(), _CONSTRUCT_CAPTURE_GROUP, ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), _VB_016_DISPLAY_NAME),
			[_VB_017] = BuildUnschemedRule(VBNetPatternExpressions.LateBoundHttpObjectCredentials(), _CONSTRUCT_CAPTURE_GROUP, ConfidenceLevel.Medium, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), _VB_017_DISPLAY_NAME),
			[_VB_018] = BuildUnschemedRule(VBNetPatternExpressions.NonHttpCredentialSink(), _CONSTRUCT_CAPTURE_GROUP, ConfidenceLevel.Low, CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP), _VB_018_DISPLAY_NAME),
			[_VB_019] = BuildUnschemedRule(VBNetPatternExpressions.AuthenticationHelperDefinition(), _DECLARATION_CAPTURE_GROUP, ConfidenceLevel.Low, CredentialPosition.None(), _VB_019_DISPLAY_NAME)
		};

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>
	/// An SDK credential type or entry point establishes its destination: self-sufficient, High, with the password argument as the credential position.
	/// Used for <c>VB-001</c>, <c>VB-002</c>, <c>VB-010</c> and <c>VB-011</c>, the four whose anchor is a name from the Relativity SDK.
	/// </summary>
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

	/// <summary>The scheme is an argument and the cache entry's third parameter is a credential by its type, so the class is self-sufficient.</summary>
	private static DetectionRule BuildVB003()
	{
		DetectionRule rule = new()
		{
			Expression = VBNetPatternExpressions.CredentialCacheBasicScheme(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = BuildSchemeInterpretation(),
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _VB_003_DISPLAY_NAME
		};

		return rule;
	}

	/// <summary>A type name that declares the scheme and the role, constructed with a credential pair: self-sufficient, High, no scheme argument to resolve.</summary>
	private static DetectionRule BuildVB004()
	{
		return BuildDeclaringRule(VBNetPatternExpressions.ThirdPartyBasicAuthenticatorType(), _VB_004_DISPLAY_NAME);
	}

	/// <summary>A method name that declares the scheme and an authentication word, invoked with a credential pair.</summary>
	private static DetectionRule BuildVB005()
	{
		return BuildDeclaringRule(VBNetPatternExpressions.SchemeNamingExtensionMethod(), _VB_005_DISPLAY_NAME);
	}

	private static DetectionRule BuildDeclaringRule(Regex expression, string displayName)
	{
		DetectionRule rule = new()
		{
			Expression = expression,
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = displayName
		};

		return rule;
	}

	/// <summary>
	/// A hand-built header establishes nothing about the destination and needs the call path: High base, Medium by composition, the scheme verdict applying.
	/// The anchor of <c>VB-007</c> is the opening quote of the key literal, whose zone counts as code.
	/// </summary>
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

	/// <summary>
	/// The transport credential type declares Basic, and the password is optional: it is read within the five logical lines after, where it is written
	/// there. No scheme argument to resolve.
	/// </summary>
	private static DetectionRule BuildVB009()
	{
		DetectionRule rule = new()
		{
			Expression = VBNetPatternExpressions.WcfBasicTransportCredential(),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = _VB_009_DISPLAY_NAME
		};

		return rule;
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

	// ------------------------------------------------------------------------------------------
	// Element 4: demotion signals
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The language's own file signals, every one declared <c>Insensitive</c> because file and folder names are case-insensitive on Windows. The
	/// <c>Deployment</c> folder is not a signal, so a High construct under it reports High.
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
			FileName(FileNameMatch.Suffix, _TESTS_FILE_SUFFIX),
			FileName(FileNameMatch.Suffix, _DOTTED_TEST_FILE_SUFFIX),
			FileName(FileNameMatch.Suffix, _SPEC_FILE_SUFFIX),
			FileName(FileNameMatch.Suffix, _DESIGNER_FILE_SUFFIX),
			FileName(FileNameMatch.Suffix, _SHORT_GENERATED_FILE_SUFFIX),
			FileName(FileNameMatch.Suffix, _GENERATED_FILE_SUFFIX),
			FileName(FileNameMatch.Suffix, _FAKE_FILE_SUFFIX),
			FileName(FileNameMatch.Suffix, _MOCK_FILE_SUFFIX),
			FileName(FileNameMatch.Suffix, _STUB_FILE_SUFFIX),
			FileName(FileNameMatch.Prefix, _FAKE_FILE_PREFIX),
			FileName(FileNameMatch.Prefix, _MOCK_FILE_PREFIX),
			FileName(FileNameMatch.Prefix, _STUB_FILE_PREFIX),
			FileName(FileNameMatch.Prefix, _SAMPLE_FILE_PREFIX),
			FileName(FileNameMatch.Prefix, _EXAMPLE_FILE_PREFIX)
		}.ToFrozenSet();

		return signals;
	}

	private static FileNameSignal FileName(FileNameMatch match, string text)
	{
		return new FileNameSignal { Match = match, Text = text, Sensitivity = CaseSensitivity.Insensitive };
	}

	private static FrozenSet<FolderNameSignal> BuildFolderNameSignals()
	{
		FrozenSet<FolderNameSignal> signals = new[]
		{
			NamedFolder(_TEST_FOLDER_NAME),
			NamedFolder(_TESTS_FOLDER_NAME),
			NamedFolder(_SPEC_FOLDER_NAME),
			NamedFolder(_FIXTURES_FOLDER_NAME),
			NamedFolder(_SAMPLE_FOLDER_NAME),
			NamedFolder(_SAMPLES_FOLDER_NAME),
			NamedFolder(_EXAMPLE_FOLDER_NAME),
			NamedFolder(_EXAMPLES_FOLDER_NAME),
			NamedFolder(_SERVICE_REFERENCES_FOLDER_NAME),
			NamedFolder(_WEB_REFERENCES_FOLDER_NAME),
			new FolderNameSignal { FolderName = _VENDOR_FOLDER_NAME, Sensitivity = CaseSensitivity.Insensitive, MarksVendoredCode = true },
			new FolderNameSignal { FolderName = _THIRD_PARTY_FOLDER_NAME, Sensitivity = CaseSensitivity.Insensitive, MarksVendoredCode = true },
			new FolderNameSignal { FolderName = _TEST_FOLDER_SUFFIX_TEST, Match = FolderNameMatch.Suffix, Sensitivity = CaseSensitivity.Insensitive },
			new FolderNameSignal { FolderName = _TEST_FOLDER_SUFFIX_TESTS, Match = FolderNameMatch.Suffix, Sensitivity = CaseSensitivity.Insensitive }
		}.ToFrozenSet();

		return signals;
	}

	private static FolderNameSignal NamedFolder(string folderName)
	{
		return new FolderNameSignal { FolderName = folderName, Sensitivity = CaseSensitivity.Insensitive };
	}

	private static ImmutableArray<ContentMarker> BuildContentMarkers()
	{
		ImmutableArray<ContentMarker> markers =
		[
			new ContentMarker { Expression = VBNetPatternExpressions.GeneratedHeaderContentMarker(), Zone = ContentMarkerZone.AnyZone },
			new ContentMarker { Expression = VBNetPatternExpressions.GeneratedCodeAttributeContentMarker(), Zone = ContentMarkerZone.CodeOnly },
			new ContentMarker { Expression = VBNetPatternExpressions.TestAttributeContentMarker(), Zone = ContentMarkerZone.CodeOnly }
		];

		return markers;
	}

	// ------------------------------------------------------------------------------------------
	// Element 5: preselection anchors, flow signals, recommendations
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// The eleven anchors, all declared <c>Insensitive</c> (VB.NET writes <c>credential</c> as often as <c>Credential</c>): every class brings its own.
	/// <c>Basic</c> is deliberately not an anchor, and neither are <c>UserName</c> and <c>Base64</c>.
	/// </summary>
	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = new[]
			{
				InsensitiveAnchor(_ANCHOR_CREDENTIAL),
				InsensitiveAnchor(_ANCHOR_AUTHORIZATION),
				InsensitiveAnchor(_ANCHOR_AUTHENTICATOR),
				InsensitiveAnchor(_ANCHOR_TO_BASE64_STRING),
				InsensitiveAnchor(_ANCHOR_WITH_BASIC_AUTH),
				InsensitiveAnchor(_ANCHOR_IMPORT_API),
				InsensitiveAnchor(_ANCHOR_RELATIVITY_USERNAME),
				InsensitiveAnchor(_ANCHOR_RELATIVITY_PASSWORD),
				InsensitiveAnchor(_ANCHOR_CREATE_OBJECT),
				InsensitiveAnchor(_ANCHOR_DOWNLOAD_FILE),
				InsensitiveAnchor(_ANCHOR_UPLOAD_FILE)
			}.ToFrozenSet(),
			TemplateProvider = ProvideTemplate,
			FlowSignals = VBNetFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = new Dictionary<CredentialFlow, string>
			{
				[CredentialFlow.ClientCredentials] = VBNetRecommendationTemplates.VBNET_LANGUAGE_FALLBACK_TEXT,
				[CredentialFlow.AuthorizationCode] = VBNetRecommendationTemplates.VBNET_CODE_LANGUAGE_FALLBACK_TEXT
			}.ToFrozenDictionary()
		};

		return element;
	}

	private static PreselectionAnchor InsensitiveAnchor(string text)
	{
		return new PreselectionAnchor { Text = text, Sensitivity = CaseSensitivity.Insensitive };
	}

	/// <summary>The class's template for the finding's flow; the two Low classes and any undeclared class answer null and go to manual review or the generic route.</summary>
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
			[_VB_001] = Template(VBNetRecommendationTemplates.VB_001_SOURCE_FORM, VBNetRecommendationTemplates.VB_001_TARGET_FORM, VBNetRecommendationTemplates.VB_001_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_001_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_002] = Template(VBNetRecommendationTemplates.VB_002_SOURCE_FORM, VBNetRecommendationTemplates.VB_002_TARGET_FORM, VBNetRecommendationTemplates.VB_002_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_002_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_003] = Template(VBNetRecommendationTemplates.VB_003_SOURCE_FORM, VBNetRecommendationTemplates.VB_003_TARGET_FORM, VBNetRecommendationTemplates.VB_003_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_003_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_004] = Template(VBNetRecommendationTemplates.VB_004_SOURCE_FORM, VBNetRecommendationTemplates.VB_004_TARGET_FORM, VBNetRecommendationTemplates.VB_004_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_004_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_005] = Template(VBNetRecommendationTemplates.VB_005_SOURCE_FORM, VBNetRecommendationTemplates.VB_005_TARGET_FORM, VBNetRecommendationTemplates.VB_005_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_005_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_006] = Template(VBNetRecommendationTemplates.VB_006_SOURCE_FORM, VBNetRecommendationTemplates.VB_006_TARGET_FORM, VBNetRecommendationTemplates.VB_006_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_006_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_007] = Template(VBNetRecommendationTemplates.VB_007_SOURCE_FORM, VBNetRecommendationTemplates.VB_007_TARGET_FORM, VBNetRecommendationTemplates.VB_007_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_007_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_008] = Template(VBNetRecommendationTemplates.VB_008_SOURCE_FORM, VBNetRecommendationTemplates.VB_008_TARGET_FORM, VBNetRecommendationTemplates.VB_008_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_008_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_009] = Template(VBNetRecommendationTemplates.VB_009_SOURCE_FORM, VBNetRecommendationTemplates.VB_009_TARGET_FORM, VBNetRecommendationTemplates.VB_009_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_009_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_010] = Template(VBNetRecommendationTemplates.VB_010_SOURCE_FORM, VBNetRecommendationTemplates.VB_010_TARGET_FORM, VBNetRecommendationTemplates.VB_010_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_010_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_011] = Template(VBNetRecommendationTemplates.VB_011_SOURCE_FORM, VBNetRecommendationTemplates.VB_011_TARGET_FORM, VBNetRecommendationTemplates.VB_011_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_011_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_012] = Template(VBNetRecommendationTemplates.VB_012_SOURCE_FORM, VBNetRecommendationTemplates.VB_012_TARGET_FORM, VBNetRecommendationTemplates.VB_012_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_012_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_013] = Template(VBNetRecommendationTemplates.VB_013_SOURCE_FORM, VBNetRecommendationTemplates.VB_013_TARGET_FORM, VBNetRecommendationTemplates.VB_013_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_013_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_014] = Template(VBNetRecommendationTemplates.VB_014_SOURCE_FORM, VBNetRecommendationTemplates.VB_014_TARGET_FORM, VBNetRecommendationTemplates.VB_014_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_014_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_015] = Template(VBNetRecommendationTemplates.VB_015_SOURCE_FORM, VBNetRecommendationTemplates.VB_015_TARGET_FORM, VBNetRecommendationTemplates.VB_015_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_015_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_016] = Template(VBNetRecommendationTemplates.VB_016_SOURCE_FORM, VBNetRecommendationTemplates.VB_016_TARGET_FORM, VBNetRecommendationTemplates.VB_016_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_016_VERIFICATION_NOTE, tokenAcquisition),
			[_VB_017] = Template(VBNetRecommendationTemplates.VB_017_SOURCE_FORM, VBNetRecommendationTemplates.VB_017_TARGET_FORM, VBNetRecommendationTemplates.VB_017_CREDENTIAL_PROVENANCE, VBNetRecommendationTemplates.VB_017_VERIFICATION_NOTE, tokenAcquisition)
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
	/// The declared triples. <c>VB-002</c> subsumes <c>VB-001</c> at identical anchors (both anchor on the credential-type token). The four sink classes
	/// carry the credential construction inside their own span, so a credential that a recognized sink consumes is the sink's and not also the residue's:
	/// <c>VB-003</c>, <c>VB-012</c>, <c>VB-016</c> and <c>VB-018</c> each subsume <c>VB-013</c> at overlapping spans. <c>VB-015</c> subsumes <c>VB-014</c> at
	/// identical anchors: the empty half is the stronger fact.
	/// </summary>
	private static FrozenSet<SubsumptionRule> BuildSubsumptionRelation()
	{
		FrozenSet<SubsumptionRule> relation = new[]
		{
			Subsumption(_VB_002, _VB_001, SubsumptionCondition.AnchorPositionsAreIdentical),
			Subsumption(_VB_003, _VB_013, SubsumptionCondition.MatchSpansOverlap),
			Subsumption(_VB_012, _VB_013, SubsumptionCondition.MatchSpansOverlap),
			Subsumption(_VB_016, _VB_013, SubsumptionCondition.MatchSpansOverlap),
			Subsumption(_VB_018, _VB_013, SubsumptionCondition.MatchSpansOverlap),
			Subsumption(_VB_015, _VB_014, SubsumptionCondition.AnchorPositionsAreIdentical)
		}.ToFrozenSet();

		return relation;
	}

	private static SubsumptionRule Subsumption(string subsuming, string subsumed, SubsumptionCondition when)
	{
		return new SubsumptionRule
		{
			SubsumingPatternClassIdentifier = subsuming,
			SubsumedPatternClassIdentifier = subsumed,
			When = when
		};
	}
}
