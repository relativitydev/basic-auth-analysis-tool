using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE RULES `TypeScript` COMPOSES FROM `JavaScript`, and the relation between them.
///
/// The inherited classes are taken by reference from the `JavaScript` builders and filtered by an explicit allow-list, so a class added to `JavaScript` is not inherited until the list names it,
/// and a `JavaScript` class that is not on it fails here. The subsumption relation is the `JavaScript` relation
/// plus four triples, each held at identical anchors and each with a fixture that shows the subsumed class gone. Every script is invented, with placeholder names.
/// </summary>
public sealed class TypeScriptRuleDeclarationTests
{
	private const int _ALLOW_LISTED_CLASS_COUNT = 19;
	private const int _JAVASCRIPT_SUBSUMPTION_COUNT = 7;
	private const int _ADDED_TRIPLE_COUNT = 4;

	private static readonly ImmutableArray<string> _pinnedAllowList =
	[
		"JS-001", "JS-002", "JS-003", "JS-004", "JS-005", "JS-006", "JS-007", "JS-008", "JS-009",
		"JS-010", "JS-011", "JS-012", "JS-013", "JS-014", "JS-015", "JS-016", "JS-017", "JS-018", "JS-019"
	];

	private static readonly ImmutableArray<string> _pinnedLowClasses = ["JS-007", "JS-009", "JS-011", "JS-014", "JS-016", "JS-018", "JS-019"];

	// ------------------------------------------------------------------------------------------
	// The allow-list
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheAllowListIsExactlyTheNineteenPinnedIdentifiersAndSevenOfThemAreLow()
	{
		FrozenSet<string> allowList = TypeScriptInternalHooks.InheritedClassAllowList;
		IReadOnlyDictionary<string, DetectionRule> rules = JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass;

		Assert.Equal(_ALLOW_LISTED_CLASS_COUNT, allowList.Count);
		Assert.Equal(_pinnedAllowList.Order(StringComparer.Ordinal), allowList.Order(StringComparer.Ordinal));
		Assert.Equal(
			_pinnedLowClasses.Order(StringComparer.Ordinal),
			allowList.Where(patternClass => rules[patternClass].BaseLevel == ConfidenceLevel.Low).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void EveryJavaScriptClassIsOnTheAllowListSoANewJavaScriptClassIsADecisionAndNotAnAccident()
	{
		string[] notInherited = [.. JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass.Keys.Where(patternClass => !TypeScriptInternalHooks.InheritedClassAllowList.Contains(patternClass))];

		Assert.True(notInherited.Length == 0, $"A JavaScript class is not on the TypeScript allow-list: {string.Join(", ", notInherited)}. Decide, then change the list and this pinned set in one commit.");
	}

	[Fact]
	public void AClassOfTheAllowListThatJavaScriptNoLongerDeclaresWouldFailTheCompositionAtOnce()
	{
		IReadOnlyDictionary<string, DetectionRule> javaScript = JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass;

		Assert.All(TypeScriptInternalHooks.InheritedClassAllowList, patternClass => Assert.Contains(patternClass, javaScript.Keys));
	}

	[Fact]
	public void TheTypeScriptRulesAreTheAllowListPlusTheTwoClassesOfTypeScriptAlone()
	{
		IReadOnlyDictionary<string, DetectionRule> rules = TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass;

		Assert.Equal(
			TypeScriptInternalHooks.InheritedClassAllowList.Concat(["TS-001", "TS-003"]).Order(StringComparer.Ordinal),
			rules.Keys.Order(StringComparer.Ordinal));
	}

	// ------------------------------------------------------------------------------------------
	// The subsumption relation
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheRelationIsTheJavaScriptRelationPlusTheFourTriples()
	{
		FrozenSet<SubsumptionRule> relation = TypeScriptRegisteredExpressions.Analyzer.SubsumptionRelation;
		FrozenSet<SubsumptionRule> javaScript = JavaScriptRegisteredExpressions.Analyzer.SubsumptionRelation;
		(string Subsuming, string Subsumed, SubsumptionCondition When)[] added =
		[
			("JS-001", "TS-001", SubsumptionCondition.AnchorPositionsAreIdentical),
			("TS-001", "JS-011", SubsumptionCondition.AnchorPositionsAreIdentical),
			("TS-003", "JS-011", SubsumptionCondition.AnchorPositionsAreIdentical),
			("TS-003", "TS-001", SubsumptionCondition.AnchorPositionsAreIdentical)
		];

		Assert.Equal(_JAVASCRIPT_SUBSUMPTION_COUNT, javaScript.Count);
		Assert.Equal(_JAVASCRIPT_SUBSUMPTION_COUNT + _ADDED_TRIPLE_COUNT, relation.Count);
		Assert.Equal(
			Triples(javaScript).Concat(added).Order().ToArray(),
			Triples(relation).Order().ToArray());
	}

	[Fact]
	public void EveryClassNamedByARuleOfTheRelationIsDeclared()
	{
		IReadOnlyDictionary<string, DetectionRule> rules = TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass;

		Assert.All(TypeScriptRegisteredExpressions.Analyzer.SubsumptionRelation, rule =>
		{
			Assert.Contains(rule.SubsumingPatternClassIdentifier, rules.Keys);
			Assert.Contains(rule.SubsumedPatternClassIdentifier, rules.Keys);
		});
	}

	[Fact]
	public void TheEnumeratedClientObjectIsReadByItsOwnClassAndTheTypedClassAndTheLowClassAreSubsumed()
	{
		const string source = "const response = await axios.get(url, { auth: { username: 'exampleUser', password: 'exampleSecret' } } as AxiosRequestConfig);\n";

		AssertOnly(source, "JS-001", ConfidenceLevel.High);
	}

	[Fact]
	public void TheTypedObjectIsReadByTheTypedClassAndTheLowClassIsSubsumed()
	{
		const string source = "const config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n";

		AssertOnly(source, "TS-001", ConfidenceLevel.Medium);
	}

	[Fact]
	public void TheServiceCallIsReadByTheServiceClassAndTheLowClassIsSubsumed()
	{
		const string source = "this.httpService.get(url, { auth: { username: userName, password: secret } });\n";

		AssertOnly(source, "TS-003", ConfidenceLevel.High);
	}

	[Fact]
	public void TheServiceCallWithATypedObjectIsReadByTheServiceClassAndBothOtherClassesAreSubsumed()
	{
		const string source = "this.httpService.post(url, { auth: { username: 'exampleUser', password: 'exampleSecret' } } as AxiosRequestConfig);\n";

		AssertOnly(source, "TS-003", ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n", "TS-001", "JS-011")]
	[InlineData("this.httpService.get(url, { auth: { username: userName, password: secret } });\n", "TS-003", "JS-011")]
	public void ASubsumedClassStillMatchesItsOwnExpressionAtTheSameAnchorAsTheSubsumingClass(string source, string subsuming, string subsumed)
	{
		TypeScriptRecognitionHarness harness = new();
		FileRecognitionContext context = harness.ContextFor(source);
		DetectionRule subsumingRule = harness.Analyzer.DetectionRulesByPatternClass[subsuming];
		DetectionRule subsumedRule = harness.Analyzer.DetectionRulesByPatternClass[subsumed];

		System.Text.RegularExpressions.Match subsumingMatch = subsumingRule.Expression.Match(context.NormalizedText);
		System.Text.RegularExpressions.Match subsumedMatch = subsumedRule.Expression.Match(context.NormalizedText);

		Assert.True(subsumingMatch.Success);
		Assert.True(subsumedMatch.Success);
		Assert.Equal(subsumingMatch.Groups[subsumingRule.AnchorPosition.CaptureGroupName!].Index, subsumedMatch.Groups[subsumedRule.AnchorPosition.CaptureGroupName!].Index);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static IEnumerable<(string Subsuming, string Subsumed, SubsumptionCondition When)> Triples(FrozenSet<SubsumptionRule> relation)
	{
		return relation.Select(rule => (rule.SubsumingPatternClassIdentifier, rule.SubsumedPatternClassIdentifier, rule.When));
	}

	private static void AssertOnly(string source, string patternClass, ConfidenceLevel level)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);
		string described = string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));

		Assert.True(constructs.Length == 1, $"[{source}] produced [{described}]");
		Assert.Equal(patternClass, constructs[0].PatternClassIdentifier);
		Assert.Equal(level, constructs[0].Level);
	}
}
