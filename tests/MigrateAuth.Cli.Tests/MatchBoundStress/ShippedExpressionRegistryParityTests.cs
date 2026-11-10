using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE REGISTRY OF THE COST GATE AGAINST THE EXPRESSIONS THE ANALYZERS SHIP. The registries of the `JavaScript` and `TypeScript` gates (<see cref="TypeScriptDraftExpressions"/> reads the shipped
/// `JavaScript` expressions by identifier, and holds the `TypeScript` ones it adds) must hold every expression an analyzer ships, and these tests fail when they do not: the number of
/// expressions an analyzer ships, counted from the analyzer itself (the constant declaration, the member declaration, one for every pattern class, every content marker and every flow
/// signal), must equal the number the registry charges to a file. The file-name generation marker of `JavaScript` is outside the registry, and is counted apart in
/// <see cref="TheGenerationMarkerExpressionsAreCountedApartBecauseTheRegistryNeverHeldThem"/>. The `JavaScript` registry is checked by reference as well: every expression the analyzer ships is a registered one and no registered
/// expression is anything else.
/// </summary>
public sealed class ShippedExpressionRegistryParityTests
{
	private const int _SHIPPED_JAVASCRIPT_EXPRESSION_COUNT = 1 + 1 + 19 + 2 + 16;
	private const int _SHIPPED_TYPESCRIPT_EXPRESSION_COUNT = 1 + 1 + 21 + 2 + 20;
	private const string _MEMBER_EXPRESSION_IDENTIFIER = "EX-JS-102";

	[Fact]
	public void TheJavaScriptRegistryHoldsExactlyTheExpressionsTheAnalyzerShips()
	{
		ImmutableArray<Regex> shipped = ShippedExpressionsOf(JavaScriptRegisteredExpressions.Analyzer);
		ImmutableArray<Regex> registered = [.. TypeScriptDraftExpressions.ShippedJavaScriptExpressions.Select(entry => entry.Expression)];

		Assert.Equal(_SHIPPED_JAVASCRIPT_EXPRESSION_COUNT, shipped.Length);
		Assert.Equal(shipped.Length, registered.Length);
		Assert.Equal(registered.Length, TypeScriptDraftExpressions.ShippedJavaScriptExpressions.Select(entry => entry.Identifier).Distinct(StringComparer.Ordinal).Count());
		Assert.Empty(shipped.Where(expression => !registered.Any(entry => ReferenceEquals(entry, expression))));
		Assert.Empty(registered.Where(expression => !shipped.Any(entry => ReferenceEquals(entry, expression))));
	}

	[Fact]
	public void TheRegistryCountsAreTheAnalyzersDeclaredCountsAndMoveWithThem()
	{
		ILanguageAnalyzer javaScript = JavaScriptRegisteredExpressions.Analyzer;

		Assert.Equal(TypeScriptDraftExpressions.SHIPPED_CLASS_COUNT, javaScript.DetectionRulesByPatternClass.Count);
		Assert.Equal(TypeScriptDraftExpressions.SHIPPED_CONTENT_MARKER_COUNT, javaScript.DemotionSignals.ContentMarkers.Count());
		Assert.Equal(TypeScriptDraftExpressions.SHIPPED_FLOW_SIGNAL_COUNT, javaScript.CandidateSelectionAndRecommendation.FlowSignals.Length);
		Assert.Equal(TypeScriptDraftExpressions.SHIPPED_MEMBER_DECLARATION_COUNT, javaScript.LexicalProfile.MemberDeclarationExpression is null ? 0 : 1);
	}

	[Fact]
	public void TheTypeScriptRegistryChargesExactlyAsManyExpressionsAsTheTypeScriptAnalyzerShips()
	{
		ImmutableArray<Regex> shipped = ShippedExpressionsOf(TypeScriptRegisteredExpressions.Analyzer);

		Assert.Equal(_SHIPPED_TYPESCRIPT_EXPRESSION_COUNT, shipped.Length);
		Assert.Equal(shipped.Length, TypeScriptDraftExpressions.MatchBoundExpressions.Length);
		Assert.Equal(shipped.Length, TypeScriptDraftExpressions.MatchBoundExpressions.Select(entry => entry.Identifier).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void TheGenerationMarkerExpressionsAreCountedApartBecauseTheRegistryNeverHeldThem()
	{
		Assert.Single(JavaScriptRegisteredExpressions.Analyzer.DemotionSignals.GenerationMarkerExpressions);
		Assert.Empty(TypeScriptRegisteredExpressions.Analyzer.DemotionSignals.GenerationMarkerExpressions);
		Assert.All(JavaScriptRegisteredExpressions.Analyzer.DemotionSignals.GenerationMarkerExpressions, expression => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, expression.MatchTimeout));
	}

	[Fact]
	public void EveryExpressionTheRegistriesChargeCarriesTheMatchBound()
	{
		Assert.All(TypeScriptDraftExpressions.ShippedJavaScriptExpressions, entry => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Expression.MatchTimeout));
		Assert.All(ShippedExpressionsOf(TypeScriptRegisteredExpressions.Analyzer), expression => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, expression.MatchTimeout));
	}

	[Fact]
	public void ADifferenceBetweenTheShippedCountAndTheRegistryIsWhatTheTestsFailOn()
	{
		ImmutableArray<Regex> shipped = ShippedExpressionsOf(JavaScriptRegisteredExpressions.Analyzer);
		ImmutableArray<Regex> registeredWithoutTheMemberExpression = [.. TypeScriptDraftExpressions.ShippedJavaScriptExpressions.Where(entry => entry.Identifier != _MEMBER_EXPRESSION_IDENTIFIER).Select(entry => entry.Expression)];

		Assert.NotEqual(shipped.Length, registeredWithoutTheMemberExpression.Length);
	}

	/// <summary>Every expression the match bound is charged for, counted from the analyzer: the lexical budget's first-line expression is not one of them.</summary>
	private static ImmutableArray<Regex> ShippedExpressionsOf(ILanguageAnalyzer analyzer)
	{
		ImmutableArray<Regex>.Builder expressions = ImmutableArray.CreateBuilder<Regex>();

		expressions.Add(analyzer.LexicalProfile.ConstantDeclarationExpression);

		if (analyzer.LexicalProfile.MemberDeclarationExpression is Regex member)
		{
			expressions.Add(member);
		}

		expressions.AddRange(analyzer.DetectionRulesByPatternClass.Values.Select(rule => rule.Expression));
		expressions.AddRange(analyzer.DemotionSignals.ContentMarkers.Select(marker => marker.Expression));
		expressions.AddRange(analyzer.CandidateSelectionAndRecommendation.FlowSignals.Select(signal => signal.Expression));

		return expressions.ToImmutable();
	}
}
