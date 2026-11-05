using System.Collections.Frozen;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// A language analyzer supplies exactly the six required contract elements and one optional seventh, whose rule element
/// carries eight fields per class, and never orders, numbers or writes.
///
/// The counts are asserted here; the rest of
/// what the contract forbids is enforced by the shape of the types rather than by a prohibition, and
/// these assertions are the check that the shape still holds.
/// </summary>
public sealed class AnalyzerContractSurfaceTests
{
	private const int _EXPECTED_TOP_LEVEL_ELEMENT_COUNT = 7;
	private const int _EXPECTED_DETECTION_RULE_FIELD_COUNT = 8;
	private const int _EXPECTED_CONTENT_MARKER_FIELD_COUNT = 3;
	private const string _PROPERTY_GETTER_PREFIX = "get_";
	private const string _INIT_ONLY_MODIFIER_NAME = "IsExternalInit";
	private const string _UNTERMINATED_RULE_TOKEN = "Unterminated";
	private const string _RECURSION_RULE_TOKEN = "Recursion";
	private const int _EXPECTED_SHELL_HERE_DOCUMENT_RULE_COUNT = 2;
	private const string _SHELL_PROFILE_NAME_PREFIX = "Shell";
	private const string _IMMEDIATE_BODY_START_NAME = nameof(HereDocumentBodyStart.AfterTheOpening);

	private static readonly Type[] _contractTypes =
	[
		typeof(ILanguageAnalyzer),
		typeof(LexicalProfile),
		typeof(LanguageIdentification),
		typeof(DetectionRule),
		typeof(SymbolicVerdictInterpretation),
		typeof(LanguageShapedDemotionSignals),
		typeof(CandidateSelectionAndRecommendation),
		typeof(SubsumptionRule),
		typeof(ConstructPrecedence),
		typeof(StatementNesting),
		typeof(QuoteRule),
		typeof(LineCommentRule),
		typeof(BlockCommentRule),
		typeof(LiteralPrefixRule),
		typeof(SuspendedSubRange),
		typeof(CapturedTerminator),
		typeof(ArmBlockRule),
		typeof(ArithmeticContextRule),
		typeof(PreselectionAnchor),
		typeof(RecommendationTemplate),
		typeof(FileNameSignal),
		typeof(FolderNameSignal),
		typeof(ContentMarker),
		typeof(FlowSignal)
	];

	[Fact]
	public void AnalyzerSuppliesExactlySevenTopLevelElementsAndTheSeventhIsOptional()
	{
		PropertyInfo[] elements = typeof(ILanguageAnalyzer).GetProperties(BindingFlags.Public | BindingFlags.Instance);

		Assert.Equal(_EXPECTED_TOP_LEVEL_ELEMENT_COUNT, elements.Length);
		Assert.Contains(elements, element => element.PropertyType == typeof(ConstructPrecedence));
	}

	/// <summary>
	/// The seventh element is optional and only the `Shell` analyzer declares it; every other shipped profile declares
	/// none. The mechanism is pinned on test-local profiles (the one-finding-per-construct
	/// tests), and `Shell`'s declaration is the one under <c>src/</c> that uses it.
	/// </summary>
	[Fact]
	public void OnlyTheShellAnalyzerDeclaresAConstructPrecedence()
	{
		IReadOnlyList<ILanguageAnalyzer> analyzers = [.. LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName.Values.Distinct()];
		ILanguageAnalyzer shell = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("Shell"));

		Assert.NotNull(shell.ConstructPrecedence);
		Assert.All(analyzers.Where(analyzer => analyzer != shell), analyzer => Assert.Null(analyzer.ConstructPrecedence));
	}

	[Fact]
	public void RuleElementCarriesExactlyEightRequiredFieldsPerPatternClass()
	{
		PropertyInfo[] fields = typeof(DetectionRule).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		PropertyInfo[] requiredAndEarlierOptionalFields = [.. fields.Where(field => field.Name != nameof(DetectionRule.VariableUse))];

		Assert.Equal(_EXPECTED_DETECTION_RULE_FIELD_COUNT, requiredAndEarlierOptionalFields.Length);
	}

	/// <summary>
	/// The ninth field is OPTIONAL and none by default, so a rule that declares none reads exactly as one with
	/// eight fields, and the eight above are exactly the ones counted.
	/// </summary>
	[Fact]
	public void TheNinthRuleFieldIsTheOptionalVariableUseAndNoneByDefault()
	{
		DetectionRule rule = new()
		{
			Expression = new Regex(string.Empty),
			AnchorPosition = MatchAnchor.AtMatchStart(),
			BaseLevel = ConfidenceLevel.Low,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = "Probe rule without a variable use"
		};

		Assert.Equal(_EXPECTED_DETECTION_RULE_FIELD_COUNT + 1, typeof(DetectionRule).GetProperties(BindingFlags.Public | BindingFlags.Instance).Length);
		Assert.Null(rule.VariableUse);
	}

	[Fact]
	public void CredentialPositionAndDestinationCertaintyAreFieldsOfTheRuleAndNotTopLevelElements()
	{
		PropertyInfo[] topLevelElements = typeof(ILanguageAnalyzer).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		Type[] topLevelElementTypes = [.. topLevelElements.Select(element => element.PropertyType)];

		Assert.DoesNotContain(typeof(CredentialPosition), topLevelElementTypes);
		Assert.DoesNotContain(typeof(DestinationCertainty), topLevelElementTypes);
		Assert.NotNull(typeof(DetectionRule).GetProperty(nameof(DetectionRule.CredentialPosition)));
		Assert.NotNull(typeof(DetectionRule).GetProperty(nameof(DetectionRule.DestinationCertainty)));
	}

	[Fact]
	public void CredentialPositionIsOptionalAndDefaultsToNone()
	{
		DetectionRule ruleWithoutACredential = new()
		{
			Expression = new Regex(string.Empty),
			AnchorPosition = MatchAnchor.AtMatchStart(),
			BaseLevel = ConfidenceLevel.Low,
			SelfSufficiency = SelfSufficiency.RequiresCallPathToBeEstablished,
			SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
			DestinationCertainty = DestinationCertainty.Unknown,
			DisplayName = "Probe rule without a credential"
		};

		Assert.False(ruleWithoutACredential.CredentialPosition.IsDeclared);
		Assert.Equal(CredentialPosition.None(), ruleWithoutACredential.CredentialPosition);
	}

	/// <summary>
	/// An analyzer cannot write because it is never handed anything to write to. The interface
	/// has no methods at all, so there is no point at which it is invoked with a sink.
	/// </summary>
	[Fact]
	public void AnalyzerExposesNoMethodBeyondItsPropertyAccessors()
	{
		MethodInfo[] methods = typeof(ILanguageAnalyzer).GetMethods(BindingFlags.Public | BindingFlags.Instance);

		Assert.All(methods, method => Assert.StartsWith(_PROPERTY_GETTER_PREFIX, method.Name, StringComparison.Ordinal));
	}

	[Fact]
	public void NoContractMemberExposesAnOutputSink()
	{
		IReadOnlyList<Type> memberTypes = ReadEveryContractMemberType();

		Assert.All(memberTypes, memberType =>
		{
			Assert.False(typeof(TextWriter).IsAssignableFrom(memberType));
			Assert.False(typeof(TextReader).IsAssignableFrom(memberType));
			Assert.False(typeof(Stream).IsAssignableFrom(memberType));
		});
	}

	/// <summary>
	/// No contract member type - across every element, not only the demotion signals' - is one of the types
	/// that could carry the file's text, its mask or its path to an analyzer:
	/// <see cref="FileRecognitionContext"/> itself, <see cref="ZoneMask"/>, or
	/// <see cref="RepositoryRelativePath"/>. <see cref="NoContractMemberExposesAnOutputSink"/> rules
	/// out a WRITE sink, and this rules out a READ one.
	/// </summary>
	[Fact]
	public void NoContractMemberCanReceiveOrExposeTheAnalyzedFile()
	{
		IReadOnlyList<Type> memberTypes = ReadEveryContractMemberType();

		Assert.All(memberTypes, memberType =>
		{
			Assert.NotEqual(typeof(FileRecognitionContext), memberType);
			Assert.NotEqual(typeof(ZoneMask), memberType);
			Assert.NotEqual(typeof(RepositoryRelativePath), memberType);
		});
	}

	/// <summary>
	/// A content marker is exactly the expression and the zone it fires in - never a delegate the
	/// engine could invoke with the file, which is the shape <see cref="LanguageNameDerivation"/>,
	/// <see cref="RecommendationTemplateProvider"/> and <see cref="DelimiterContextPredicate"/>
	/// already have elsewhere in this contract and which a content marker does not need: the
	/// engine calls <see cref="ContentMarker.Expression"/> itself, so there is nothing here for a
	/// callback to do.
	/// </summary>
	[Fact]
	public void ContentMarkerExposesExactlyExpressionAndZoneAndNeitherIsADelegate()
	{
		PropertyInfo[] properties = typeof(ContentMarker).GetProperties(BindingFlags.Public | BindingFlags.Instance);

		Assert.Equal(_EXPECTED_CONTENT_MARKER_FIELD_COUNT, properties.Length);
		Assert.Equal(typeof(Regex), typeof(ContentMarker).GetProperty(nameof(ContentMarker.Expression))!.PropertyType);
		Assert.Equal(
			typeof(ContentMarkerZone),
			typeof(ContentMarker).GetProperty(nameof(ContentMarker.Zone))!.PropertyType);
		Assert.Equal(
			typeof(DemotionKind),
			typeof(ContentMarker).GetProperty(nameof(ContentMarker.Kind))!.PropertyType);
		Assert.All(
			properties,
			property => Assert.False(typeof(Delegate).IsAssignableFrom(property.PropertyType)));
	}

	/// <summary>
	/// An analyzer cannot number, because the contract carries no integer anywhere. A position
	/// inside a match is a named capture group, and only the engine turns a name into an offset.
	/// </summary>
	[Fact]
	public void NoContractMemberCarriesAnOrdinalOrAnyOtherNumber()
	{
		IReadOnlyList<Type> memberTypes = ReadEveryContractMemberType();

		Assert.All(memberTypes, memberType => Assert.NotEqual(typeof(int), memberType));
		Assert.Equal(typeof(string), typeof(MatchAnchor).GetProperty(nameof(MatchAnchor.CaptureGroupName))!.PropertyType);
		Assert.Equal(typeof(string), typeof(CredentialPosition).GetProperty(nameof(CredentialPosition.CaptureGroupName))!.PropertyType);
	}

	/// <summary>
	/// An analyzer cannot order, because every collection it supplies is unordered by type. A
	/// frozen set and a frozen dictionary have no index, so an order written in a catalog has
	/// nowhere to survive.
	/// </summary>
	[Fact]
	public void EveryCollectionTheAnalyzerSuppliesIsUnorderedByType()
	{
		PropertyInfo[] elements = typeof(ILanguageAnalyzer).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		IReadOnlyList<Type> collectionElements =
		[
			.. elements
				.Select(element => element.PropertyType)
				.Where(IsGenericCollection)
		];

		Assert.NotEmpty(collectionElements);
		Assert.All(collectionElements, collectionType => Assert.True(IsUnorderedCollection(collectionType)));
	}

	[Fact]
	public void NoContractMemberIsSettableAfterConstruction()
	{
		IReadOnlyList<PropertyInfo> properties = ReadEveryContractProperty();

		Assert.All(properties, property => Assert.True(property.SetMethod is null || IsInitOnly(property.SetMethod)));
	}

	/// <summary>
	/// The unterminated-construct policy and the recursion bound are
	/// the engine's. There is no field for either, so a profile author cannot supply one.
	/// </summary>
	[Fact]
	public void ProfileCannotExpressTheEngineOwnedUnterminatedPolicyOrRecursionBound()
	{
		IReadOnlyList<PropertyInfo> properties = ReadEveryContractProperty();

		Assert.All(properties, property =>
		{
			Assert.DoesNotContain(_UNTERMINATED_RULE_TOKEN, property.Name, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain(_RECURSION_RULE_TOKEN, property.Name, StringComparison.OrdinalIgnoreCase);
		});

		Assert.True(LexicalEnginePolicy.MAXIMUM_INTERPOLATION_RECURSION_DEPTH > 0);
	}

	/// <summary>
	/// One policy, uniform across every rule. The enumeration offers a single value, so two
	/// different policies for a literal and for a comment is not a thing that can be written.
	/// </summary>
	[Fact]
	public void UnterminatedConstructPolicyOffersExactlyOneChoice()
	{
		UnterminatedConstructPolicy[] policies = Enum.GetValues<UnterminatedConstructPolicy>();

		Assert.Single(policies);
		Assert.Equal(UnterminatedConstructPolicy.ReportAnomalyAndCloseAtBoundary, LexicalEnginePolicy.UNTERMINATED_CONSTRUCT_POLICY);
	}

	[Fact]
	public void NewlinePolicyIsThreeValuedAndNotABoolean()
	{
		NewlinePolicy[] policies = Enum.GetValues<NewlinePolicy>();

		Assert.Contains(NewlinePolicy.Forbidden, policies);
		Assert.Contains(NewlinePolicy.EscapedContinues, policies);
		Assert.Contains(NewlinePolicy.Allowed, policies);
	}

	/// <summary>
	/// THE ONLY SHIPPED RULES THAT USE THE WHOLE-LINE ANCHORINGS AND THE DEFERRED BODY START ARE `Shell`'S TWO. This lists the anchorings and the
	/// body start of every rule every shipped analyzer declares and shows that the
	/// `<<` rule (a whole-line closing) and the `<<-` rule (a whole-line closing after leading tabs), both with their body after the opening line, are the
	/// only ones that do, and that no other rule declares a whole-line closing or a deferred body start.
	/// </summary>
	[Fact]
	public void OnlyTheTwoShellHereDocumentRulesDeclareAWholeLineAnchoringAndADeferredBodyStart()
	{
		IReadOnlyList<string> listing = ListEveryShippedRuleAnchoringAndBodyStart();
		string[] usingTheAmendment =
		[
			.. listing.Where(line => line.Contains(nameof(PositionalAnchoring.WholeLine), StringComparison.Ordinal)
				|| !line.EndsWith(_IMMEDIATE_BODY_START_NAME, StringComparison.Ordinal))
		];

		Assert.NotEmpty(listing);
		Assert.Equal(_EXPECTED_SHELL_HERE_DOCUMENT_RULE_COUNT, usingTheAmendment.Length);
		Assert.All(usingTheAmendment, line =>
		{
			Assert.StartsWith(_SHELL_PROFILE_NAME_PREFIX, line, StringComparison.Ordinal);
			Assert.EndsWith(nameof(HereDocumentBodyStart.AfterTheOpeningLine), line, StringComparison.Ordinal);
		});
		Assert.Single(usingTheAmendment, line => line.Contains($"|{PositionalAnchoring.WholeLine}|", StringComparison.Ordinal));
		Assert.Single(usingTheAmendment, line => line.Contains($"|{PositionalAnchoring.WholeLineAfterLeadingTabs}|", StringComparison.Ordinal));

		IEnumerable<QuoteRule> capturedTerminatorRulesOfOtherLanguages = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName.Values.Distinct()
			.Where(analyzer => !analyzer.LexicalProfile.ProfileName.StartsWith(_SHELL_PROFILE_NAME_PREFIX, StringComparison.Ordinal))
			.SelectMany(analyzer => analyzer.LexicalProfile.QuoteRulesInEngineOrder)
			.Where(rule => rule.CapturedTerminator is not null);

		Assert.All(capturedTerminatorRulesOfOtherLanguages, rule =>
			Assert.DoesNotContain(rule.ClosingAnchoring, new[] { PositionalAnchoring.WholeLine, PositionalAnchoring.WholeLineAfterLeadingTabs }));
	}

	private static IReadOnlyList<string> ListEveryShippedRuleAnchoringAndBodyStart()
	{
		List<string> listing = [];

		foreach (ILanguageAnalyzer analyzer in LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName.Values.Distinct())
		{
			LexicalProfile profile = analyzer.LexicalProfile;

			listing.AddRange(profile.LineCommentRulesInEngineOrder.Select(rule =>
				$"{profile.ProfileName}|line {rule.Opening}|{rule.OpeningAnchoring}|{PositionalAnchoring.Anywhere}|{_IMMEDIATE_BODY_START_NAME}"));
			listing.AddRange(profile.BlockCommentRulesInEngineOrder.Select(rule =>
				$"{profile.ProfileName}|block {rule.Opening}|{rule.OpeningAnchoring}|{rule.ClosingAnchoring}|{_IMMEDIATE_BODY_START_NAME}"));
			listing.AddRange(profile.QuoteRulesInEngineOrder.Select(rule =>
				$"{profile.ProfileName}|quote {rule.Opening}|{rule.OpeningAnchoring}|{rule.ClosingAnchoring}|{DescribeBodyStart(rule)}"));
		}

		return listing;
	}

	private static string DescribeBodyStart(QuoteRule rule)
	{
		HereDocumentBodyStart bodyStart = rule.CapturedTerminator?.BodyStart ?? HereDocumentBodyStart.AfterTheOpening;

		return bodyStart is HereDocumentBodyStart.AfterTheOpening
			? _IMMEDIATE_BODY_START_NAME
			: bodyStart.ToString();
	}

	private static IReadOnlyList<PropertyInfo> ReadEveryContractProperty()
	{
		List<PropertyInfo> properties = [];

		foreach (Type contractType in _contractTypes)
		{
			properties.AddRange(contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
		}

		return properties;
	}

	private static IReadOnlyList<Type> ReadEveryContractMemberType()
	{
		IReadOnlyList<Type> memberTypes = [.. ReadEveryContractProperty().Select(property => property.PropertyType)];

		return memberTypes;
	}

	private static bool IsInitOnly(MethodInfo setter)
	{
		bool isInitOnly = setter
			.ReturnParameter
			.GetRequiredCustomModifiers()
			.Any(modifier => string.Equals(modifier.Name, _INIT_ONLY_MODIFIER_NAME, StringComparison.Ordinal));

		return isInitOnly;
	}

	private static bool IsGenericCollection(Type candidate)
	{
		bool isCollection = candidate.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(candidate);

		return isCollection;
	}

	private static bool IsUnorderedCollection(Type candidate)
	{
		Type openType = candidate.GetGenericTypeDefinition();
		bool isUnordered = openType == typeof(FrozenSet<>) || openType == typeof(FrozenDictionary<,>);

		return isUnordered;
	}
}
