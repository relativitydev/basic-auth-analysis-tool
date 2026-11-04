using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// An analyzer built for a test rather than for a language.
///
/// THE EMBEDDED CATALOG DOES NOT SERVE THESE TESTS, and that is deliberate rather than a
/// shortcut: it declares <c>CSharp</c> alone, so the supported row, the class count and the
/// template route would all be read from one real language's catalog, and the cases below need
/// analyzers whose declarations the test controls. The reference fixture does not serve either:
/// it declares the invented names <c>ExemplarOne</c> and <c>ExemplarTwo</c>, which match none of
/// the eight canonical names a finding carries.
///
/// So the catalogs below are assembled from analyzers that DO declare a real language name.
/// Nothing about the shape is invented: the contract is the real <see cref="ILanguageAnalyzer"/>,
/// and a member added to it breaks this file rather than being silently skipped.
/// </summary>
internal sealed class TestLanguageAnalyzer : ILanguageAnalyzer
{
	private readonly FrozenDictionary<string, RecommendationTemplate> _templates;
	private readonly FrozenDictionary<string, RecommendationTemplate> _authorizationCodeTemplates;

	/// <summary>
	/// <paramref name="templates"/> answer the Client Credentials flow and
	/// <paramref name="authorizationCodeTemplates"/> the Authorization Code flow, so a fixture can
	/// carry a template for one flow of a class and none for the other. Likewise
	/// <paramref name="languageFallbackText"/> is the Client Credentials text and
	/// <paramref name="authorizationCodeFallbackText"/> the Authorization Code one.
	/// </summary>
	internal TestLanguageAnalyzer(
		string languageName,
		string fileExtension,
		IEnumerable<string> patternClassIdentifiers,
		IReadOnlyDictionary<string, RecommendationTemplate> templates,
		IEnumerable<DeclaredUnsupportedCoverage>? declaredUnsupportedCoverage = null,
		string? languageFallbackText = null,
		IReadOnlyDictionary<string, RecommendationTemplate>? authorizationCodeTemplates = null,
		string? authorizationCodeFallbackText = null)
	{
		_templates = templates.ToFrozenDictionary(StringComparer.Ordinal);
		_authorizationCodeTemplates = (authorizationCodeTemplates ?? new Dictionary<string, RecommendationTemplate>())
			.ToFrozenDictionary(StringComparer.Ordinal);

		LexicalProfile = new LexicalProfile
		{
			ProfileName = languageName,
			ConstantDeclarationExpression = new Regex("^$", RegexOptions.CultureInvariant),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = []
		};

		LanguageIdentification = new LanguageIdentification
		{
			FileExtensions = new[] { fileExtension }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { languageName }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = (_, _) => languageName,
			DeclaredUnsupportedCoverage = (declaredUnsupportedCoverage ?? [])
				.ToFrozenSet()
		};

		DetectionRulesByPatternClass = patternClassIdentifiers
			.ToDictionary(identifier => identifier, BuildRule, StringComparer.Ordinal)
			.ToFrozenDictionary(StringComparer.Ordinal);

		DemotionSignals = new LanguageShapedDemotionSignals
		{
			FileNameSignals = FrozenSet<FileNameSignal>.Empty,
			FolderNameSignals = FrozenSet<FolderNameSignal>.Empty,
			GenerationMarkerExpressions = [],
			ContentMarkers = []
		};

		CandidateSelectionAndRecommendation = new CandidateSelectionAndRecommendation
		{
			PreselectionAnchors = FrozenSet<PreselectionAnchor>.Empty,
			TemplateProvider = ProvideTemplate,
			FlowSignals = [],
			LanguageFallbackTextByFlow = BuildFallbackTextByFlow(languageFallbackText, authorizationCodeFallbackText)
		};

		SubsumptionRelation = FrozenSet<SubsumptionRule>.Empty;
	}

	public LexicalProfile LexicalProfile { get; }

	public LanguageIdentification LanguageIdentification { get; }

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; }

	public LanguageShapedDemotionSignals DemotionSignals { get; }

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; }

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; }

	private RecommendationTemplate? ProvideTemplate(string patternClassIdentifier, CredentialFlow flow)
	{
		FrozenDictionary<string, RecommendationTemplate> templatesOfFlow =
			flow is CredentialFlow.ClientCredentials ? _templates : _authorizationCodeTemplates;

		return templatesOfFlow.TryGetValue(patternClassIdentifier, out RecommendationTemplate? template) ? template : null;
	}

	private static FrozenDictionary<CredentialFlow, string>? BuildFallbackTextByFlow(
		string? clientCredentialsText,
		string? authorizationCodeText)
	{
		Dictionary<CredentialFlow, string> declared = [];

		if (clientCredentialsText is not null)
		{
			declared[CredentialFlow.ClientCredentials] = clientCredentialsText;
		}

		if (authorizationCodeText is not null)
		{
			declared[CredentialFlow.AuthorizationCode] = authorizationCodeText;
		}

		return declared.Count == 0 ? null : declared.ToFrozenDictionary();
	}

	private static DetectionRule BuildRule(string patternClassIdentifier)
	{
		return new DetectionRule
		{
			Expression = new Regex("^$", RegexOptions.CultureInvariant),
			AnchorPosition = MatchAnchor.AtMatchStart(),
			BaseLevel = ConfidenceLevel.High,
			SelfSufficiency = SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			DestinationCertainty = DestinationCertainty.Established,
			DisplayName = ReportHarness.DisplayNameFor(patternClassIdentifier)
		};
	}
}

/// <summary>
/// A generic-recommendation provider a test can watch.
///
/// It is here rather than a mock because what the test asserts is that the RENDERER CALLED THE
/// EXTENSION POINT instead of inventing text, and the cheapest honest evidence of that is a
/// provider that records the findings it was asked about.
/// </summary>
internal sealed class RecordingGenericRecommendationProvider : IGenericRecommendationProvider
{
	internal const string TEXT = "the generic recommendation this double stands in for";

	internal List<CredentialFlow> AskedAbout { get; } = [];

	public string DescribeFor(CredentialFlow flow)
	{
		AskedAbout.Add(flow);

		return TEXT;
	}
}

/// <summary>
/// The fixtures the report tests are built from: catalogs with a chosen set of analyzers,
/// configurations with a chosen threshold and a chosen set of disabled languages, and findings
/// placed where a test wants them.
///
/// THE FINDINGS ARE HAND-BUILT rather than composed from recognized files, and that is the right
/// fixture for these tests. What is under test is rendering - which sections appear, how a group is
/// labelled, which recommendation route fires - and none of that is a property of how a construct
/// came to be recognized. Composing them through the pipeline would constrain the fixture to the
/// arrangements the reference profile's expressions happen to produce, which does not include a
/// Low finding beside a High one in a language with a template.
///
/// The identifiers are assigned by <see cref="FindingIdentifiers"/> exactly as composition assigns
/// them, so a test that asserts an identifier survived the threshold filter is asserting against
/// the real format rather than a literal someone typed.
/// </summary>
internal static class ReportHarness
{
	internal const string TEMPLATED_PATTERN_CLASS = "TST-001";
	internal const string UNTEMPLATED_PATTERN_CLASS = "TST-002";
	internal const string SECOND_PATTERN_CLASS = "TST-003";

	internal const string TEMPLATE_SOURCE_FORM = "a credential object built from a user name and a password";
	internal const string TEMPLATE_TARGET_FORM = "a bearer token obtained from the identity endpoint";
	internal const string TEMPLATE_CREDENTIAL_PROVENANCE = "the process environment or a secret store, never source";
	internal const string TEMPLATE_VERIFICATION_NOTE = "confirm the request still authenticates after the change";

	/// <summary>A fixture language's own declared generic-fallback text.</summary>
	internal const string LANGUAGE_FALLBACK_TEXT = "this fixture language's own declared generic fallback text";

	internal const string REPOSITORY_LABEL = "report-harness-repository";

	/// <summary>The fixed run date every fixture content carries, unless a test overrides it.</summary>
	internal static readonly DateOnly RUN_DATE = new(2026, 9, 18);

	/// <summary>
	/// Inserted lower-case-first so a naive "as declared" reading and an
	/// ordinal sort disagree: ordinal puts every upper-case letter before every lower-case one, so
	/// <see cref="UPPERCASE_FIRST_CONSTRUCT"/> sorts BEFORE <see cref="LOWERCASE_FIRST_CONSTRUCT"/>
	/// even though it is declared second here and even though a culture-aware, case-insensitive
	/// reading would put them in alphabetical order regardless of case.
	/// </summary>
	internal const string LOWERCASE_FIRST_CONSTRUCT = "zebra-shaped construct declared first";

	internal const string UPPERCASE_FIRST_CONSTRUCT = "Apple-shaped construct declared second";
	internal const string DECLARED_UNSUPPORTED_REASON = "an invented reason this catalog does not attempt it";

	/// <summary>
	/// The format behind every <see cref="TestLanguageAnalyzer"/> rule's
	/// <see cref="DetectionRule.DisplayName"/>, derived from its own pattern class identifier so
	/// it is unique by construction across a fixture's classes. Read only through
	/// <see cref="DisplayNameFor"/>, so a test asserting the rendered text builds the same value
	/// rather than typing a second copy of the format.
	/// </summary>
	private const string _DISPLAY_NAME_FORMAT = "Display name for {0}";

	private const string _REPOSITORY_ROOT = "report-harness-repository";
	private const string _CSHARP_FILE_EXTENSION = ".cs";
	private const string _POWERSHELL_FILE_EXTENSION = ".ps1";

	private static readonly RepositoryFolderPath _repository =
		RepositoryFolderPath.FromSuppliedText(Path.Combine(Path.GetTempPath(), _REPOSITORY_ROOT));

	/// <summary>
	/// A catalog holding an analyzer for C# only. Seven of the eight languages are therefore
	/// declared unsupported, which is the case the coverage statement exists for and the one a
	/// loop over the catalog registry would render as silence.
	/// </summary>
	internal static LanguageCatalog CatalogWithCSharpOnly()
	{
		return new LanguageCatalog(LanguageCatalog.CurrentVersion, [BuildCSharpAnalyzer()]);
	}

	/// <summary>
	/// A catalog whose C# analyzer declares two unsupported constructs, in an
	/// order chosen so that ordinal sort and declaration order disagree - see
	/// <see cref="LOWERCASE_FIRST_CONSTRUCT"/>.
	/// </summary>
	internal static LanguageCatalog CatalogWithDeclaredUnsupportedCoverage()
	{
		return new LanguageCatalog(LanguageCatalog.CurrentVersion, [BuildCSharpAnalyzerWithDeclaredUnsupportedCoverage()]);
	}

	/// <summary>A catalog holding analyzers for two of the eight, to show the count is per language.</summary>
	internal static LanguageCatalog CatalogWithTwoLanguages()
	{
		return new LanguageCatalog(
			LanguageCatalog.CurrentVersion,
			[BuildCSharpAnalyzer(), BuildPowerShellAnalyzer()]);
	}

	/// <summary>A catalog with no analyzer at all.</summary>
	internal static LanguageCatalog CatalogWithNoAnalyzers()
	{
		return new LanguageCatalog(LanguageCatalog.CurrentVersion, []);
	}

	/// <summary>
	/// A catalog holding a C# analyzer that declares
	/// <see cref="LANGUAGE_FALLBACK_TEXT"/> as its generic-fallback text, for the tests that
	/// assert the renderer's third route uses it in place of the base's language-neutral one.
	/// </summary>
	internal static LanguageCatalog CatalogWithLanguageFallbackText()
	{
		return new LanguageCatalog(LanguageCatalog.CurrentVersion, [BuildCSharpAnalyzer(LANGUAGE_FALLBACK_TEXT)]);
	}

	/// <summary>
	/// A catalog whose C# analyzer declares a fallback text for Client Credentials and, when
	/// supplied, one for Authorization Code - and, when supplied, a template for the templated class under
	/// Authorization Code as well.
	/// </summary>
	internal static LanguageCatalog CatalogWithPerFlowFallbackText(
		string? clientCredentialsText,
		string? authorizationCodeText)
	{
		return new LanguageCatalog(
			LanguageCatalog.CurrentVersion,
			[BuildCSharpAnalyzer(clientCredentialsText, authorizationCodeText)]);
	}

	/// <summary>
	/// A configuration at the supplied threshold, with every language enabled except the ones named.
	/// The provenance is the built-in-default shape, which is what a session with no configuration
	/// file anywhere produces.
	/// </summary>
	internal static EffectiveConfiguration ConfigurationWith(
		ConfidenceLevel minimumLevel,
		params AssessedLanguage[] disabledLanguages)
	{
		ImmutableArray<LanguageEnablement> enablement =
		[
			.. AssessedLanguages.AllInDeclaredOrder.Select(language => new LanguageEnablement
			{
				Language = language,
				IsEnabled = !disabledLanguages.Contains(language)
			})
		];

		return new EffectiveConfiguration
		{
			LanguageEnablement = enablement,
			MinimumConfidenceLevel = minimumLevel,
			OutputPathText = null,
			Provenance = new ConfigurationProvenance
			{
				EnabledLanguagesOrigin = ConfigurationSource.BuiltInDefault,
				MinimumConfidenceLevelOrigin = ConfigurationSource.BuiltInDefault,
				OutputPathOrigin = ConfigurationSource.WorkingDirectoryAtLaunch,
				LanguageEnablement = enablement,
				IgnoredRepositoryFields = [],
				ConfigurationFiles =
				[
					new ConfigurationFileState
					{
						Location = ConfigurationFileLocation.RepositoryRoot,
						Condition = ConfigurationFileCondition.Absent
					},
					new ConfigurationFileState
					{
						Location = ConfigurationFileLocation.UserProfile,
						Condition = ConfigurationFileCondition.Absent
					}
				]
			}
		};
	}

	/// <summary>The content of a run that found nothing, which is the empty case.</summary>
	internal static ReportContent EmptyContent(LanguageCatalog catalog, EffectiveConfiguration configuration)
	{
		return ContentWith(catalog, configuration, []);
	}

	/// <summary>
	/// The content of a run that produced the supplied findings, with the findings cap untripped -
	/// <see cref="FindingsCapOutcome.Empty"/> - unless a test names one through the overload below.
	/// </summary>
	internal static ReportContent ContentWith(
		LanguageCatalog catalog,
		EffectiveConfiguration configuration,
		ImmutableArray<Finding> findings)
	{
		return ContentWith(catalog, configuration, findings, FindingsCapOutcome.Empty);
	}

	/// <summary>
	/// The content of a run that produced the supplied findings under the supplied cap outcome.
	///
	/// <see cref="ReportContent.ScanEnabledLanguages"/> defaults to
	/// <paramref name="configuration"/>'s OWN current enabled set - the ordinary, non-stale case,
	/// where the scan that produced a report ran under the
	/// same configuration the report is now rendered under. A test of the stale case builds its
	/// content with the <c>with</c> expression instead, exactly as it does for
	/// <see cref="ReportContent.ScanSummary"/>.
	/// </summary>
	internal static ReportContent ContentWith(
		LanguageCatalog catalog,
		EffectiveConfiguration configuration,
		ImmutableArray<Finding> findings,
		FindingsCapOutcome findingsCapOutcome)
	{
		return new ReportContent
		{
			Findings = findings,
			Configuration = configuration,
			Catalog = catalog,
			ScanSummary = ScanSummary.Empty,
			SkippedItems = new SkippedItemLedger().Tallies(),
			FindingsCapOutcome = findingsCapOutcome,
			RepositoryLabel = REPOSITORY_LABEL,
			RunDate = RUN_DATE,
			ScanEnabledLanguages = configuration.EnabledLanguagesInDeclaredOrder
		};
	}

	/// <summary>
	/// One finding, numbered as composition would have numbered it at the supplied index.
	///
	/// The relative path goes through <see cref="RepositoryRelativePath.TryFromScannedPath"/>
	/// because that is the only way to obtain one - there is no constructor taking arbitrary text,
	/// which is the whole point of the type.
	/// </summary>
	internal static Finding FindingAt(
		int sequentialIndex,
		string relativePath,
		int line,
		string patternClassIdentifier,
		ConfidenceLevel level,
		DestinationCertainty certainty = DestinationCertainty.Established,
		bool literalCredentialSignal = false,
		AssessedLanguage language = AssessedLanguage.CSharp,
		CredentialFlow? flow = null,
		FlowDecision? flowDecision = null,
		LowConfidenceReasons lowReasons = LowConfidenceReasons.None)
	{
		RepositoryRelativePath path = PathOf(relativePath);
		RuleIdentifier patternClass = PatternClassOf(patternClassIdentifier);

		return new Finding
		{
			Identifier = FindingIdentifiers.Of(sequentialIndex),
			SequentialIndex = sequentialIndex,
			Path = path,
			Line = line,
			Column = 1,
			Language = language,
			PatternClass = patternClass,
			Level = level,
			DestinationCertainty = certainty,
			LiteralCredentialSignal = literalCredentialSignal,
			LowReasons = lowReasons,
			Flow = flow,
			FlowDecision = flowDecision,
			Fingerprint = new FindingFingerprint
			{
				Path = NormalizedPathKey.From(path),
				PatternClass = patternClass,
				OccurrenceOrdinal = FindingFingerprint.FIRST_OCCURRENCE_ORDINAL
			}
		};
	}

	/// <summary>The display name <see cref="TestLanguageAnalyzer"/> gives one pattern class.</summary>
	internal static string DisplayNameFor(string patternClassIdentifier)
	{
		return string.Format(System.Globalization.CultureInfo.InvariantCulture, _DISPLAY_NAME_FORMAT, patternClassIdentifier);
	}

	internal static RepositoryRelativePath PathOf(string relativePath)
	{
		if (!RepositoryRelativePath.TryFromScannedPath(_repository, relativePath, out RepositoryRelativePath path))
		{
			throw new InvalidOperationException($"The harness could not build a relative path from '{relativePath}'.");
		}

		return path;
	}

	private static RuleIdentifier PatternClassOf(string patternClassIdentifier)
	{
		if (!RuleIdentifier.TryFromPatternClass(patternClassIdentifier, out RuleIdentifier patternClass))
		{
			throw new InvalidOperationException($"The harness could not build a pattern class from '{patternClassIdentifier}'.");
		}

		return patternClass;
	}

	private static TestLanguageAnalyzer BuildCSharpAnalyzer(
		string? languageFallbackText = null,
		string? authorizationCodeFallbackText = null)
	{
		return new TestLanguageAnalyzer(
			AssessedLanguages.CanonicalName(AssessedLanguage.CSharp),
			_CSHARP_FILE_EXTENSION,
			[TEMPLATED_PATTERN_CLASS, UNTEMPLATED_PATTERN_CLASS],
			new Dictionary<string, RecommendationTemplate>(StringComparer.Ordinal)
			{
				[TEMPLATED_PATTERN_CLASS] = new RecommendationTemplate
				{
					GenericSourceForm = TEMPLATE_SOURCE_FORM,
					TargetFormUnderTokenAuthentication = TEMPLATE_TARGET_FORM,
					CredentialProvenance = TEMPLATE_CREDENTIAL_PROVENANCE,
					VerificationNote = TEMPLATE_VERIFICATION_NOTE
				}
			},
			languageFallbackText: languageFallbackText,
			authorizationCodeFallbackText: authorizationCodeFallbackText);
	}

	/// <summary>Same fixture as <see cref="BuildCSharpAnalyzer"/>, plus two declared-unsupported entries.</summary>
	private static TestLanguageAnalyzer BuildCSharpAnalyzerWithDeclaredUnsupportedCoverage()
	{
		return new TestLanguageAnalyzer(
			AssessedLanguages.CanonicalName(AssessedLanguage.CSharp),
			_CSHARP_FILE_EXTENSION,
			[TEMPLATED_PATTERN_CLASS, UNTEMPLATED_PATTERN_CLASS],
			new Dictionary<string, RecommendationTemplate>(StringComparer.Ordinal)
			{
				[TEMPLATED_PATTERN_CLASS] = new RecommendationTemplate
				{
					GenericSourceForm = TEMPLATE_SOURCE_FORM,
					TargetFormUnderTokenAuthentication = TEMPLATE_TARGET_FORM,
					CredentialProvenance = TEMPLATE_CREDENTIAL_PROVENANCE,
					VerificationNote = TEMPLATE_VERIFICATION_NOTE
				}
			},
			[
				new DeclaredUnsupportedCoverage { Construct = LOWERCASE_FIRST_CONSTRUCT, Reason = DECLARED_UNSUPPORTED_REASON },
				new DeclaredUnsupportedCoverage { Construct = UPPERCASE_FIRST_CONSTRUCT, Reason = DECLARED_UNSUPPORTED_REASON }
			]);
	}

	private static TestLanguageAnalyzer BuildPowerShellAnalyzer()
	{
		return new TestLanguageAnalyzer(
			AssessedLanguages.CanonicalName(AssessedLanguage.PowerShell),
			_POWERSHELL_FILE_EXTENSION,
			[SECOND_PATTERN_CLASS],
			new Dictionary<string, RecommendationTemplate>(StringComparer.Ordinal));
	}
}
