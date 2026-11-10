using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The golden test of the analyzer contract: a reference profile is registered and consumed with
/// no engine change.
///
/// What makes this demonstrate the property rather than assert it:
///
/// - The profile is reached only through <see cref="ExemplarFixtureCatalog"/> and the public
///   contract. The analyzer lives in the test project, so no engine type can even name it:
///   special-casing the reference language in the engine would not compile, rather than merely
///   failing a review.
/// - The consumer is <see cref="AnalyzerContractValidator"/>, which reads everything back
///   through the contract - the constructed expressions' group names, the language derivation,
///   the template provider - and has no language-specific branch to take.
/// - <see cref="NoEngineTypeCarriesAnyKnowledgeOfTheReferenceLanguage"/> fails the moment an
///   engine type, member or string constant mentions this language, which is what "the engine
///   was not touched" means operationally.
/// - <see cref="TheReferenceLanguageIsVisibleWhereItIsSupposedToBe"/> is the non-vacuity check:
///   it fails if the tokens the previous test looks for stopped appearing anywhere at all.
/// </summary>
public sealed class ReferenceProfileGoldenTests
{
	private const string _FIRST_LANGUAGE_NAME = "ExemplarOne";
	private const string _SECOND_LANGUAGE_NAME = "ExemplarTwo";
	private const string _FIRST_LANGUAGE_FILE_EXTENSION = ".ex1";
	private const string _SECOND_LANGUAGE_FILE_EXTENSION = ".ex2";
	private const string _CREDENTIAL_TYPE_PATTERN_CLASS = "EXM-001";
	private const string _HAND_ASSEMBLED_HEADER_PATTERN_CLASS = "EXM-002";
	private const string _SHEBANG_FIRST_LINE = "#!/usr/bin/env exemplar";
	private const string _UNCLAIMED_FILE_EXTENSION = ".notaclaimedextension";

	private const string _ENGINE_NAMESPACE_PREFIX = "MigrateAuth.Cli";
	private const string _LANGUAGE_NAMESPACE_PREFIX = "MigrateAuth.Cli.Catalog.Languages";

	private const int _EXPECTED_DECLARED_LANGUAGE_COUNT = 2;
	/// <summary>
	/// Three classes. Subsumption needs two classes that recognize ONE construct, and the first two
	/// cannot overlap in any text - a credential construction and a header assembly are disjoint by
	/// shape. The legacy client is the class that overlaps, and it is what makes the subsumption
	/// relation something the engine is tested against rather than something the contract merely accepts.
	/// </summary>
	private const int _EXPECTED_DETECTION_RULE_COUNT = 3;
	private const int _EXPECTED_NEWLINE_POLICY_COUNT = 3;

	private static readonly ImmutableArray<string> _referenceLanguageTokens =
	[
		"Exemplar",
		_FIRST_LANGUAGE_FILE_EXTENSION,
		_SECOND_LANGUAGE_FILE_EXTENSION,
		"EXM-",
		"NewExemplarCredentials",
		"BuildScheme"
	];

	/// <summary>A sample written in the invented language, exercising its whole vocabulary.</summary>
	private const string _SAMPLE_SOURCE = """
		#!/usr/bin/env exemplar
		-- a line comment
		REM a comment opened by a whole word
		<# a block comment #>
		const AUTH_SCHEME = "Basic"
		let quoted = 'it''s doubled'
		let message = "value ${ name } after"
		let raw = r"no \escape here"
		let block = ```
		still inside the literal
		```
		let pattern = /ab[/]cd/
		let document = <<'EOT'
		literal body
		EOT
		let credentials = NewExemplarCredentials(operatorName, "hunter2")
		let request = SetHeader("Authorization", BuildScheme(AUTH_SCHEME))
		""";

	[Fact]
	public void TheReferenceProfileIsRegisteredInTheEmbeddedCatalog()
	{
		LanguageCatalog catalog = ExemplarFixtureCatalog.Build();

		Assert.True(catalog.AnalyzersByLanguageName.ContainsKey(_FIRST_LANGUAGE_NAME));
		Assert.True(catalog.AnalyzersByLanguageName.ContainsKey(_SECOND_LANGUAGE_NAME));
		Assert.True(catalog.TryGetAnalyzerForFileExtension(_FIRST_LANGUAGE_FILE_EXTENSION, out _));
		Assert.False(catalog.TryGetAnalyzerForFileExtension(_UNCLAIMED_FILE_EXTENSION, out _));
	}

	/// <summary>
	/// The engine consumes the profile and finds nothing wrong with it. The consumer is generic
	/// and this is the only place the reference profile is named.
	/// </summary>
	[Fact]
	public void TheReferenceProfileFitsTheContractWithoutTheEngineBeingModified()
	{
		ILanguageAnalyzer analyzer = ReadReferenceAnalyzer();

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(analyzer);

		Assert.Empty(violations);
	}

	/// <summary>
	/// The profile-to-language relation is not one to one: one profile, two
	/// languages, and the analyzer declares how the finding's language value is derived.
	/// </summary>
	[Fact]
	public void TwoLanguagesShareOneProfileAndTheAnalyzerDerivesWhichOneAFindingCarries()
	{
		ILanguageAnalyzer analyzer = ReadReferenceAnalyzer();
		LanguageIdentification identification = analyzer.LanguageIdentification;

		Assert.Equal(_EXPECTED_DECLARED_LANGUAGE_COUNT, identification.DeclaredLanguageNames.Count);
		Assert.Equal(_FIRST_LANGUAGE_NAME, identification.LanguageNameDerivation(_FIRST_LANGUAGE_FILE_EXTENSION, null));
		Assert.Equal(_SECOND_LANGUAGE_NAME, identification.LanguageNameDerivation(_SECOND_LANGUAGE_FILE_EXTENSION, null));
		Assert.Equal(
			_FIRST_LANGUAGE_NAME,
			identification.LanguageNameDerivation(_SECOND_LANGUAGE_FILE_EXTENSION, _SHEBANG_FIRST_LINE));
	}

	/// <summary>
	/// The anchor position, exercised rather than declared: the match starts on the opening
	/// quote of a header name, which lies in a literal, while the anchor lies on code further
	/// along. A rule tested at its match start could never emit this finding.
	/// </summary>
	[Fact]
	public void TheEngineResolvesAnAnchorThatIsNotTheStartOfTheMatch()
	{
		DetectionRule rule = ReadRule(_HAND_ASSEMBLED_HEADER_PATTERN_CLASS);

		Match match = rule.Expression.Match(_SAMPLE_SOURCE);

		Assert.True(match.Success);
		Assert.True(rule.AnchorPosition.TryResolveOffset(match, out int anchorOffset));
		Assert.True(anchorOffset > match.Index);
	}

	/// <summary>
	/// Why the credential position is separate: a credential type is anchored at its
	/// type name and carries its credential at argument two, so the two offsets differ.
	/// </summary>
	[Fact]
	public void TheCredentialPositionIsNotTheAnchorOffset()
	{
		DetectionRule rule = ReadRule(_CREDENTIAL_TYPE_PATTERN_CLASS);

		Match match = rule.Expression.Match(_SAMPLE_SOURCE);

		Assert.True(match.Success);
		Assert.True(rule.AnchorPosition.TryResolveOffset(match, out int anchorOffset));
		Assert.True(rule.CredentialPosition.TryResolveOffset(match, out int credentialOffset));
		Assert.NotEqual(anchorOffset, credentialOffset);
		Assert.True(credentialOffset > anchorOffset);
	}

	[Fact]
	public void AClassWithNoCredentialArgumentDeclaresNoCredentialPosition()
	{
		DetectionRule rule = ReadRule(_HAND_ASSEMBLED_HEADER_PATTERN_CLASS);

		Assert.False(rule.CredentialPosition.IsDeclared);
		Assert.Equal(DestinationCertainty.Unknown, rule.DestinationCertainty);
		Assert.Equal(DestinationCertainty.Established, ReadRule(_CREDENTIAL_TYPE_PATTERN_CLASS).DestinationCertainty);
	}

	[Fact]
	public void TheSubsumptionRelationIsDeclaredOverClassesTheAnalyzerAlsoDeclares()
	{
		ILanguageAnalyzer analyzer = ReadReferenceAnalyzer();

		Assert.Equal(_EXPECTED_DETECTION_RULE_COUNT, analyzer.DetectionRulesByPatternClass.Count);
		Assert.NotEmpty(analyzer.SubsumptionRelation);
		Assert.All(analyzer.SubsumptionRelation, subsumption =>
		{
			Assert.True(analyzer.DetectionRulesByPatternClass.ContainsKey(subsumption.SubsumingPatternClassIdentifier));
			Assert.True(analyzer.DetectionRulesByPatternClass.ContainsKey(subsumption.SubsumedPatternClassIdentifier));
		});
	}

	/// <summary>
	/// Every lexical-profile field has to be reachable from a profile, and the only honest way to know
	/// is to write one that uses them.
	/// </summary>
	[Fact]
	public void TheReferenceProfileExercisesEveryFieldOfTheProfileVocabulary()
	{
		LexicalProfile profile = ReadReferenceAnalyzer().LexicalProfile;
		ImmutableArray<QuoteRule> quoteRules = profile.QuoteRulesInEngineOrder;

		Assert.NotNull(profile.ConstantDeclarationExpression);
		Assert.Equal(CaseSensitivity.Insensitive, profile.KeywordCaseSensitivity);
		Assert.Equal(CaseSensitivity.Insensitive, profile.DelimiterCaseSensitivity);
		Assert.Contains(profile.LineCommentRulesInEngineOrder, rule => rule.RequiresWordBoundary);
		Assert.NotEmpty(profile.BlockCommentRulesInEngineOrder);
		Assert.Contains(quoteRules, rule => rule.OpeningContextPredicate is not null);
		Assert.Contains(quoteRules, rule => rule.OpeningAnchoring != PositionalAnchoring.Anywhere);
		Assert.Contains(quoteRules, rule => rule.ClosingAnchoring != PositionalAnchoring.Anywhere);
		Assert.Contains(quoteRules, rule => rule.DelimiterLength == DelimiterLength.VariableWithAtLeastEqualClosing);
		Assert.Contains(quoteRules, rule => !rule.LiteralPrefixes.IsEmpty);
		Assert.Contains(quoteRules, rule => rule.SuspendedSubRange is not null);
		Assert.Contains(quoteRules, rule => rule.CapturedTerminator is not null);
		Assert.Contains(quoteRules, rule => rule.DoubledClosingEscapes);
		Assert.Contains(quoteRules, rule => rule.EscapeCharacter is not null);
		Assert.Contains(quoteRules, rule => rule.InterpolationOpening is not null);
		Assert.Contains(quoteRules, rule => rule.InterpolationClosing is not null);
		Assert.Contains(quoteRules, rule => rule.InterpolationIdentifierExtent is not null);
		Assert.Equal(_EXPECTED_NEWLINE_POLICY_COUNT, quoteRules.Select(rule => rule.NewlinePolicy).Distinct().Count());
	}

	[Fact]
	public void TheAnalyzerDeclaresDemotionSignalsAndPreselectionAnchorsAndTemplates()
	{
		ILanguageAnalyzer analyzer = ReadReferenceAnalyzer();

		Assert.NotEmpty(analyzer.DemotionSignals.FileNameSignals);
		Assert.NotEmpty(analyzer.DemotionSignals.FolderNameSignals);
		Assert.False(analyzer.DemotionSignals.GenerationMarkerExpressions.IsEmpty);
		Assert.NotEmpty(analyzer.CandidateSelectionAndRecommendation.PreselectionAnchors);
		Assert.NotNull(analyzer.CandidateSelectionAndRecommendation.TemplateProvider(_CREDENTIAL_TYPE_PATTERN_CLASS, CredentialFlow.ClientCredentials));
	}

	/// <summary>
	/// The half of "without touching the engine" a passing validator cannot show. It fails the
	/// moment any engine type, member or string constant acquires knowledge of this language.
	/// </summary>
	[Fact]
	public void NoEngineTypeCarriesAnyKnowledgeOfTheReferenceLanguage()
	{
		IReadOnlyList<Type> engineTypes = [.. typeof(ILanguageAnalyzer).Assembly.GetTypes().Where(IsEngineType)];
		IReadOnlyList<string> knowledgeFound = ReadReferenceLanguageMentions(IsEngineType);

		Assert.NotEmpty(engineTypes);
		Assert.Empty(knowledgeFound);
	}

	/// <summary>
	/// Non-vacuity. If the tokens the previous test hunts for vanished from the binary, that
	/// test would pass for the wrong reason, so this one insists they are still somewhere.
	/// </summary>
	[Fact]
	public void TheReferenceLanguageIsVisibleWhereItIsSupposedToBe()
	{
		IReadOnlyList<string> knowledgeFound = ReadReferenceLanguageMentions(IsLanguageType);

		Assert.NotEmpty(knowledgeFound);
	}

	private static ILanguageAnalyzer ReadReferenceAnalyzer()
	{
		LanguageCatalog catalog = ExemplarFixtureCatalog.Build();
		ILanguageAnalyzer analyzer = catalog.AnalyzersByLanguageName[_FIRST_LANGUAGE_NAME];

		return analyzer;
	}

	private static DetectionRule ReadRule(string patternClassIdentifier)
	{
		DetectionRule rule = ReadReferenceAnalyzer().DetectionRulesByPatternClass[patternClassIdentifier];

		return rule;
	}

	private static bool IsEngineType(Type candidate)
	{
		bool isEngineType =
			candidate.Namespace is string candidateNamespace
			&& candidateNamespace.StartsWith(_ENGINE_NAMESPACE_PREFIX, StringComparison.Ordinal)
			&& !candidateNamespace.StartsWith(_LANGUAGE_NAMESPACE_PREFIX, StringComparison.Ordinal);

		return isEngineType;
	}

	private static bool IsLanguageType(Type candidate)
	{
		bool isLanguageType =
			candidate.Namespace is string candidateNamespace
			&& candidateNamespace.StartsWith(_LANGUAGE_NAMESPACE_PREFIX, StringComparison.Ordinal);

		return isLanguageType;
	}

	private static IReadOnlyList<string> ReadReferenceLanguageMentions(Func<Type, bool> typeFilter)
	{
		List<string> mentions = [];
		IEnumerable<Type> types = typeof(ILanguageAnalyzer).Assembly.GetTypes().Where(typeFilter);

		foreach (Type type in types)
		{
			CollectMentions(type.FullName ?? type.Name, mentions);

			foreach (MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
			{
				CollectMentions(member.Name, mentions);
			}

			CollectConstantMentions(type, mentions);
		}

		return mentions;
	}

	private static void CollectConstantMentions(Type type, List<string> mentions)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

		foreach (FieldInfo field in fields)
		{
			if (field.IsLiteral && field.FieldType == typeof(string) && field.GetRawConstantValue() is string constantValue)
			{
				CollectMentions(constantValue, mentions);
			}
		}
	}

	private static void CollectMentions(string candidate, List<string> mentions)
	{
		foreach (string token in _referenceLanguageTokens)
		{
			if (candidate.Contains(token, StringComparison.OrdinalIgnoreCase))
			{
				mentions.Add(candidate);
			}
		}
	}
}
