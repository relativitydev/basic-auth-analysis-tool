using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE EQUIVALENCE OF THE `TypeScript` ANALYZER'S INHERITED `JavaScript` RULES, asserted in the normal suite (no timing, no figure). The `TypeScript` analyzer registers the inherited rules under the same `JS-` keys. Every `JS-` class the `TypeScript` analyzer ships has the same expression
/// (pattern, options and match timeout) as the `JavaScript` analyzer's, as do the member declaration, the content markers and the sixteen flow signals of `JavaScript` that `TypeScript` carries. It also pins what differs: the `TypeScript` file is charged six expressions a `JavaScript` file is not (its own constant declaration, `TS-001`, `TS-003` and four flow signals), so the figure of a `TypeScript` shape is not the figure of the `JavaScript` shape of the same text.
/// </summary>
public sealed class TypeScriptInheritedJavaScriptRulesEquivalenceTests
{
	private const string _INHERITED_CLASS_PREFIX = "JS-";
	private const string _JAVASCRIPT_SHAPE_PREFIX = "js-";
	private const string _TYPESCRIPT_SHAPE_PREFIX = "ts-";
	private const string _MARKUP_SHAPE_PREFIX = "tsx-";
	private const int _TYPESCRIPT_ONLY_RULE_COUNT = 2;
	private const string _TYPESCRIPT_FIRST_OWN_RULE_KEY = "TS-001";
	private const string _TYPESCRIPT_SECOND_OWN_RULE_KEY = "TS-003";
	private const int _JAVASCRIPT_FLOW_SIGNAL_COUNT = 16;
	private const int _TYPESCRIPT_ONLY_FLOW_SIGNAL_COUNT = 4;

	private static readonly ImmutableArray<string> _TYPESCRIPT_ONLY_RULE_KEYS = [_TYPESCRIPT_FIRST_OWN_RULE_KEY, _TYPESCRIPT_SECOND_OWN_RULE_KEY];

	private static readonly ImmutableArray<string> _SHAPES_WITH_THE_SAME_TEXT =
	[
		"describe-lines-without-it",
		"generated-marker-lookalike-lines",
		"long-line-axios-openings-without-a-close",
		"long-line-btoa-pairs",
		"long-line-buffer-pairs",
		"long-line-headers-keys-without-a-close",
		"long-line-request-chain-openings",
		"long-line-unclosed-quotes",
		"member-records-one-line-in-five-distinct-names",
		"member-records-one-line-in-five-same-name"
	];

	[Fact]
	public void EveryJavaScriptClassTheTypeScriptAnalyzerShipsIsRegisteredUnderTheSameKeyWithTheSameExpression()
	{
		ILanguageAnalyzer javaScript = JavaScriptRegisteredExpressions.Analyzer;
		ILanguageAnalyzer typeScript = TypeScriptRegisteredExpressions.Analyzer;
		ImmutableArray<string> inheritedKeys = [.. typeScript.DetectionRulesByPatternClass.Keys.Where(key => key.StartsWith(_INHERITED_CLASS_PREFIX, StringComparison.Ordinal)).Order(StringComparer.Ordinal)];

		Assert.Equal(typeScript.DetectionRulesByPatternClass.Count - _TYPESCRIPT_ONLY_RULE_COUNT, inheritedKeys.Length);
		Assert.Equal(javaScript.DetectionRulesByPatternClass.Count, inheritedKeys.Length);
		Assert.All(inheritedKeys, key => AssertTheSameExpression(javaScript.DetectionRulesByPatternClass[key].Expression, typeScript.DetectionRulesByPatternClass[key].Expression, key));
	}

	[Fact]
	public void TheMemberDeclarationTheContentMarkersAndTheJavaScriptFlowSignalsAreTheSameExpressionsInTypeScript()
	{
		ILanguageAnalyzer javaScript = JavaScriptRegisteredExpressions.Analyzer;
		ILanguageAnalyzer typeScript = TypeScriptRegisteredExpressions.Analyzer;
		ImmutableArray<Regex> typeScriptFlowExpressions = [.. typeScript.CandidateSelectionAndRecommendation.FlowSignals.Select(signal => signal.Expression)];
		ImmutableArray<Regex> javaScriptFlowExpressions = [.. javaScript.CandidateSelectionAndRecommendation.FlowSignals.Select(signal => signal.Expression)];

		Assert.NotNull(javaScript.LexicalProfile.MemberDeclarationExpression);
		AssertTheSameExpression(javaScript.LexicalProfile.MemberDeclarationExpression, typeScript.LexicalProfile.MemberDeclarationExpression, "member declaration");
		Assert.Equal(javaScript.DemotionSignals.ContentMarkers.Count(), typeScript.DemotionSignals.ContentMarkers.Count());
		Assert.All(javaScript.DemotionSignals.ContentMarkers, marker => Assert.Contains(typeScript.DemotionSignals.ContentMarkers, candidate => ExpressionsAreTheSame(marker.Expression, candidate.Expression)));
		Assert.Equal(_JAVASCRIPT_FLOW_SIGNAL_COUNT, javaScriptFlowExpressions.Length);
		Assert.All(javaScriptFlowExpressions, expression => Assert.Contains(typeScriptFlowExpressions, candidate => ExpressionsAreTheSame(expression, candidate)));
	}

	[Fact]
	public void TheTypeScriptFileIsChargedExpressionsTheJavaScriptFileIsNotSoTheSameTextDoesNotCostTheSame()
	{
		ILanguageAnalyzer javaScript = JavaScriptRegisteredExpressions.Analyzer;
		ILanguageAnalyzer typeScript = TypeScriptRegisteredExpressions.Analyzer;
		ImmutableArray<Regex> javaScriptFlowExpressions = [.. javaScript.CandidateSelectionAndRecommendation.FlowSignals.Select(signal => signal.Expression)];
		ImmutableArray<Regex> typeScriptOnlyFlow = [.. typeScript.CandidateSelectionAndRecommendation.FlowSignals
			.Select(signal => signal.Expression)
			.Where(expression => !javaScriptFlowExpressions.Any(candidate => ExpressionsAreTheSame(expression, candidate)))];

		Assert.Equal(_TYPESCRIPT_ONLY_FLOW_SIGNAL_COUNT, typeScriptOnlyFlow.Length);
		Assert.False(ExpressionsAreTheSame(javaScript.LexicalProfile.ConstantDeclarationExpression, typeScript.LexicalProfile.ConstantDeclarationExpression));
		Assert.Equal(
			[.. javaScript.DetectionRulesByPatternClass.Keys.Concat(_TYPESCRIPT_ONLY_RULE_KEYS).Order(StringComparer.Ordinal)],
			[.. typeScript.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal)]);
	}

	/// <summary>
	/// The `TypeScript` shapes whose text is character for character a `JavaScript` shape's are these ten, of the twenty-three names the two cost gates share (the other thirteen share a name only, and stand a different text, so the rule identity says nothing about them).
	/// A `TypeScript` file is charged six expressions a `JavaScript` file is not.
	/// </summary>
	[Fact]
	public void TheShapesTheTwoCostGatesRepeatWithTheSameTextAreExactlyTheseTen()
	{
		Dictionary<string, MatchBoundStressShape> javaScript = JavaScriptMatchBoundStressSet.Build().ToDictionary(shape => StripPrefix(shape.Name, _JAVASCRIPT_SHAPE_PREFIX), StringComparer.Ordinal);
		ImmutableArray<string> sameText =
		[
			.. TypeScriptShippedCostGateStressSet.Build()
				.Select(shape => (Common: StripPrefix(StripPrefix(StripPrefix(shape.Name, TypeScriptShippedCostGateStressSet.SHAPE_NAME_PREFIX), _TYPESCRIPT_SHAPE_PREFIX), _MARKUP_SHAPE_PREFIX), shape.Text))
				.Where(candidate => javaScript.TryGetValue(candidate.Common, out MatchBoundStressShape? counterpart) && counterpart.Text == candidate.Text)
				.Select(candidate => candidate.Common)
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal(string.Join(Environment.NewLine, _SHAPES_WITH_THE_SAME_TEXT), string.Join(Environment.NewLine, sameText));
	}

	private static string StripPrefix(string name, string prefix)
	{
		return name.StartsWith(prefix, StringComparison.Ordinal) ? name[prefix.Length..] : name;
	}

	private static bool ExpressionsAreTheSame(Regex first, Regex second)
	{
		return first.ToString() == second.ToString() && first.Options == second.Options && first.MatchTimeout == second.MatchTimeout;
	}

	private static void AssertTheSameExpression(Regex? expected, Regex? actual, string what)
	{
		Assert.NotNull(expected);
		Assert.NotNull(actual);
		Assert.Equal(expected.ToString(), actual.ToString());
		Assert.True(expected.Options == actual.Options, $"{what}: options differ");
		Assert.Equal(expected.MatchTimeout, actual.MatchTimeout);
	}
}
