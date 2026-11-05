using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// AN ANALYZER THAT DECLARES A REAL LANGUAGE NAME, WHICH IS WHAT THESE TESTS NEED AND THE EMBEDDED
/// CATALOG CANNOT GIVE THEM.
///
/// The reference fixture's declared names are <c>ExemplarOne</c> and <c>ExemplarTwo</c>. Neither
/// parses to an <see cref="AssessedLanguage"/>, and a <c>Finding</c> must carry one - so a pipeline
/// run against it produces zero findings by construction. The embedded catalog's classes and templates
/// are not the controlled three these cases need to reach every route of <c>suggest</c> and <c>report</c>.
///
/// So this analyzer declares <c>CSharp</c>, exactly as the real C# analyzer does.
/// Nothing about its shape is invented: the contract is the real <see cref="ILanguageAnalyzer"/>,
/// so a member added to it breaks this file rather than being silently skipped, and the three rules
/// below are modelled on the reference profile's own.
///
/// ITS THREE CLASSES EXIST TO REACH THE THREE RECOMMENDATION ROUTES. One High class with a template, one High
/// class without one, and one Low class - which route to <c>FromTemplate</c>, <c>Generic</c> and
/// <c>ManualReview</c> respectively. A fixture with only the first would leave two thirds of the
/// Suggest stage unexercised.
/// </summary>
internal sealed class PipelineLanguageAnalyzer : ILanguageAnalyzer
{
	/// <summary>High confidence, and the catalog carries a template for it.</summary>
	internal const string TEMPLATED_PATTERN_CLASS = "PIP-001";

	/// <summary>Low confidence, so the manual-review rule routes it to manual review without a lookup.</summary>
	internal const string LOW_PATTERN_CLASS = "PIP-002";

	/// <summary>High confidence with no template, which is the generic fallback.</summary>
	internal const string UNTEMPLATED_PATTERN_CLASS = "PIP-003";

	/// <summary>The construct the templated class recognizes, and one of the preselection anchors.</summary>
	internal const string TEMPLATED_CONSTRUCT = "NewPipelineCredentials";

	/// <summary>The construct the Low class recognizes.</summary>
	internal const string LOW_CONSTRUCT = "LegacyBasicHelper";

	/// <summary>The construct the untemplated class recognizes.</summary>
	internal const string UNTEMPLATED_CONSTRUCT = "HandRolledAuthHeader";

	/// <summary>One display name per pattern class, unique within this analyzer.</summary>
	internal const string TEMPLATED_DISPLAY_NAME = "Templated pipeline pattern class";

	/// <summary>Rendered even though this class is never routed through a template.</summary>
	internal const string LOW_DISPLAY_NAME = "Low-confidence pipeline pattern class";

	internal const string UNTEMPLATED_DISPLAY_NAME = "Untemplated pipeline pattern class";

	/// <summary>The extension files of this language carry.</summary>
	internal const string FILE_EXTENSION = ".cs";

	/// <summary>The one folder this fixture analyzer demotes.</summary>
	internal const string DEMOTED_FOLDER_NAME = "Deployment";

	private const string _CONSTRUCT_CAPTURE_GROUP = "construct";
	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";
	private const string _PROFILE_NAME = "PipelineCSharp";
	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";
	private const string _DOUBLE_QUOTE = "\"";
	private const char _ESCAPE_CHARACTER = '\\';

	private static readonly FrozenDictionary<string, RecommendationTemplate> _templatesByPatternClass =
		BuildTemplates();

	/// <summary>
	/// Optional: this fixture's own declared generic-fallback text, defaulting to
	/// null so a caller that supplies none exercises the shared engine's language-neutral fallback
	/// through <see cref="UNTEMPLATED_PATTERN_CLASS"/>.
	/// </summary>
	internal PipelineLanguageAnalyzer(string? languageFallbackText = null, ImmutableArray<FlowSignal> flowSignals = default)
	{
		CandidateSelectionAndRecommendation = BuildCandidateSelectionAndRecommendation(
			languageFallbackText,
			flowSignals.IsDefault ? [] : flowSignals);
	}

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = BuildDemotionSignals();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; }

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = FrozenSet<SubsumptionRule>.Empty;

	/// <summary>A catalog holding this analyzer and nothing else.</summary>
	internal static LanguageCatalog BuildCatalog(
		string? languageFallbackText = null,
		ImmutableArray<FlowSignal> flowSignals = default)
	{
		return new LanguageCatalog(
			LanguageCatalog.CurrentVersion,
			[new PipelineLanguageAnalyzer(languageFallbackText, flowSignals)]);
	}

	private static LexicalProfile BuildLexicalProfile()
	{
		return new LexicalProfile
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = BuildExpression(
				@"^\s*const\s+\w+\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<value>[^""\r\n]*)""",
				RegexOptions.Multiline),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			LineCommentRules = [new LineCommentRule { Opening = _LINE_COMMENT_OPENING }],
			BlockCommentRules =
			[
				new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
			],
			QuoteRules = BuildQuoteRules()
		};
	}

	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		ImmutableArray<QuoteRule> rules =
		[
			new QuoteRule
			{
				Opening = _DOUBLE_QUOTE,
				Closing = _DOUBLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Forbidden,
				EscapeCharacter = _ESCAPE_CHARACTER
			}
		];

		return rules;
	}

	private static LanguageIdentification BuildLanguageIdentification()
	{
		return new LanguageIdentification
		{
			FileExtensions = new[] { FILE_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { AssessedLanguages.CanonicalName(AssessedLanguage.CSharp) }
				.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = DeriveLanguageName
		};
	}

	private static string DeriveLanguageName(string fileExtension, string? firstLine)
	{
		return AssessedLanguages.CanonicalName(AssessedLanguage.CSharp);
	}

	private static FrozenDictionary<string, DetectionRule> BuildDetectionRules()
	{
		Dictionary<string, DetectionRule> rules = new(StringComparer.Ordinal)
		{
			[TEMPLATED_PATTERN_CLASS] = BuildTemplatedRule(),
			[LOW_PATTERN_CLASS] = BuildLowRule(),
			[UNTEMPLATED_PATTERN_CLASS] = BuildUntemplatedRule()
		};

		return rules.ToFrozenDictionary(StringComparer.Ordinal);
	}

	/// <summary>
	/// Anchored on its type name and carrying its credential at argument two, so that a literal
	/// credential in the source sets the literal-credential signal from the ZONE of that argument rather than from
	/// its value.
	///
	/// THE CREDENTIAL GROUP CAPTURES INSIDE THE QUOTES RATHER THAN AROUND THEM, and that is the
	/// difference between a rule whose signal fires and one whose signal never does. The reference
	/// profile's own group includes the opening quote, so the offset it resolves to is the literal's
	/// DELIMITER rather than its body - which <c>LiteralCredentialSignalTests</c> pins as the
	/// existing behaviour. A real analyzer wants the body, because that is the zone the signal is read
	/// from.
	/// </summary>
	private static DetectionRule BuildTemplatedRule()
	{
		return new DetectionRule
		{
			Expression = BuildExpression(
				$@"\b(?<{_CONSTRUCT_CAPTURE_GROUP}>{TEMPLATED_CONSTRUCT})\s*\(\s*[A-Za-z_][A-Za-z0-9_]*\s*,\s*(?:""(?<{_CREDENTIAL_CAPTURE_GROUP}>[^""\r\n]*)""|(?<{_CREDENTIAL_CAPTURE_GROUP}>[A-Za-z_][A-Za-z0-9_]*))\s*\)"),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = TEMPLATED_DISPLAY_NAME
		};
	}

	/// <summary>
	/// Anchored on its construct name, with an OPTIONAL trailing credential argument - which adds
	/// the capture group so a test can place a Low finding that also carries the
	/// literal-credential signal, which `securityNote` must still report even where
	/// `recommendation` is null. The zero-argument form (<c>LegacyBasicHelper()</c>) also matches:
	/// the whole credential alternation is optional and an empty group never sets the signal.
	/// </summary>
	private static DetectionRule BuildLowRule()
	{
		return new DetectionRule
		{
			Expression = BuildExpression(
				$@"\b(?<{_CONSTRUCT_CAPTURE_GROUP}>{LOW_CONSTRUCT})\s*\(\s*(?:""(?<{_CREDENTIAL_CAPTURE_GROUP}>[^""\r\n]*)""|(?<{_CREDENTIAL_CAPTURE_GROUP}>[A-Za-z_][A-Za-z0-9_]*))?\s*\)"),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.Low,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.AtCaptureGroup(_CREDENTIAL_CAPTURE_GROUP),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = LOW_DISPLAY_NAME
		};
	}

	private static DetectionRule BuildUntemplatedRule()
	{
		return new DetectionRule
		{
			Expression = BuildExpression($@"\b(?<{_CONSTRUCT_CAPTURE_GROUP}>{UNTEMPLATED_CONSTRUCT})\b"),
			AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_CAPTURE_GROUP),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			CredentialPosition = CredentialPosition.None(),
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = UNTEMPLATED_DISPLAY_NAME
		};
	}

	private static LanguageShapedDemotionSignals BuildDemotionSignals()
	{
		return new LanguageShapedDemotionSignals
		{
			FileNameSignals = FrozenSet<FileNameSignal>.Empty,
			// The shared engine does not demote this folder, so this fixture analyzer declares it itself,
			// which keeps the demoted-file reason reachable through the pipeline without the shared engine.
			FolderNameSignals = new[]
			{
				new FolderNameSignal { FolderName = DEMOTED_FOLDER_NAME, Sensitivity = CaseSensitivity.Insensitive }
			}.ToFrozenSet(),
			GenerationMarkerExpressions = [],
			ContentMarkers = []
		};
	}

	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation(
		string? languageFallbackText,
		ImmutableArray<FlowSignal> flowSignals)
	{
		return new CandidateSelectionAndRecommendation
		{
			PreselectionAnchors = new[]
			{
				new PreselectionAnchor { Text = TEMPLATED_CONSTRUCT },
				new PreselectionAnchor { Text = LOW_CONSTRUCT },
				new PreselectionAnchor { Text = UNTEMPLATED_CONSTRUCT }
			}.ToFrozenSet(),
			TemplateProvider = ProvideTemplate,
			FlowSignals = flowSignals,
			LanguageFallbackTextByFlow = languageFallbackText is null
				? null
				: new Dictionary<CredentialFlow, string> { [CredentialFlow.ClientCredentials] = languageFallbackText }.ToFrozenDictionary()
		};
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

	/// <summary>
	/// One template, for one of the three classes. The other two are deliberately absent: a High
	/// class with no entry is the generic route, and a Low class must never be looked up at all.
	/// </summary>
	private static FrozenDictionary<string, RecommendationTemplate> BuildTemplates()
	{
		Dictionary<string, RecommendationTemplate> templates = new(StringComparer.Ordinal)
		{
			[TEMPLATED_PATTERN_CLASS] = new RecommendationTemplate
			{
				GenericSourceForm = "a credential object built from a user name and a password",
				TargetFormUnderTokenAuthentication = "a bearer token obtained from the identity endpoint",
				CredentialProvenance = "the process environment or a secret store, never source",
				VerificationNote = "confirm the request still authenticates after the change"
			}
		};

		return templates.ToFrozenDictionary(StringComparer.Ordinal);
	}

	private static Regex BuildExpression(string pattern, RegexOptions additionalOptions = RegexOptions.None)
	{
		return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | additionalOptions);
	}
}
