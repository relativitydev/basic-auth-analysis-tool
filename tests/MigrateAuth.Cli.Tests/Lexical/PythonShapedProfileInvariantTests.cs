using System.Linq;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// Structural invariants of the test-local `Python` draft profile only (the Python lexical design). This is not the hard-form scanner test set.
/// </summary>
public sealed class PythonShapedProfileInvariantTests
{
	private const int _EXPECTED_PLAIN_RULE_COUNT = 4;
	private const int _EXPECTED_PLAIN_PREFIX_COUNT = 14;
	private const int _EXPECTED_INTERPOLATING_PREFIX_COUNT = 20;
	private const int _EXPECTED_QUOTE_KIND_COUNT = 4;
	private const int _EXPECTED_INTERPOLATING_RULE_COUNT = _EXPECTED_INTERPOLATING_PREFIX_COUNT * _EXPECTED_QUOTE_KIND_COUNT;

	[Fact]
	public void TheProfileDeclaresEightyFourQuoteRulesOneLineCommentAndNoBlockComment()
	{
		LexicalProfile profile = PythonShapedProfile.Build();

		Assert.Equal(PythonShapedProfile.EXPECTED_QUOTE_RULE_COUNT, profile.QuoteRulesInEngineOrder.Length);
		Assert.Single(profile.LineCommentRulesInEngineOrder);
		Assert.Equal("#", profile.LineCommentRulesInEngineOrder[0].Opening);
		Assert.NotNull(profile.LineCommentRulesInEngineOrder[0].OpeningContextPredicate);
		Assert.Empty(profile.BlockCommentRulesInEngineOrder);
	}

	[Fact]
	public void TheFourPlainRulesCarryTheFourteenPlainPrefixesAndNoInterpolation()
	{
		QuoteRule[] plainRules = [.. PythonShapedProfile.Build().QuoteRulesInEngineOrder.Where(rule => rule.InterpolationOpening is null)];

		Assert.Equal(_EXPECTED_PLAIN_PREFIX_COUNT, PythonShapedProfile.PlainPrefixes.Length);
		Assert.Equal(_EXPECTED_PLAIN_RULE_COUNT, plainRules.Length);
		Assert.All(plainRules, rule =>
		{
			Assert.Equal(_EXPECTED_PLAIN_PREFIX_COUNT, rule.LiteralPrefixes.Length);
			Assert.All(rule.LiteralPrefixes, prefix => Assert.Equal(EscapeHandling.AsDeclaredByTheQuoteRule, prefix.EscapeHandling));
			Assert.Null(rule.OpeningContextPredicate);
		});
	}

	[Fact]
	public void TheEightyExpandedRulesAreTwentyPrefixesTimesFourQuotesWithNestedHolesAndAPredicate()
	{
		QuoteRule[] expandedRules = [.. PythonShapedProfile.Build().QuoteRulesInEngineOrder.Where(rule => rule.InterpolationOpening is not null)];

		Assert.Equal(_EXPECTED_INTERPOLATING_PREFIX_COUNT, PythonShapedProfile.InterpolatingPrefixes.Length);
		Assert.Equal(_EXPECTED_QUOTE_KIND_COUNT, PythonShapedProfile.Quotes.Length);
		Assert.Equal(_EXPECTED_INTERPOLATING_RULE_COUNT, expandedRules.Length);
		Assert.All(expandedRules, rule =>
		{
			Assert.Equal("{", rule.InterpolationOpening);
			Assert.Equal("}", rule.InterpolationClosing);
			Assert.Equal("{", rule.InterpolationNestingOpening);
			Assert.True(rule.DoubledInterpolationOpeningIsLiteral);
			Assert.Empty(rule.LiteralPrefixes);
			Assert.NotNull(rule.OpeningContextPredicate);
		});
	}

	[Theory]
	[InlineData(':', false)]
	[InlineData('<', false)]
	[InlineData('>', false)]
	[InlineData('^', false)]
	[InlineData('=', false)]
	[InlineData('+', false)]
	[InlineData('-', false)]
	[InlineData(' ', true)]
	[InlineData('x', true)]
	[InlineData(')', true)]
	public void TheHashPredicateRefusesTheSevenFormatSpecPredecessorsAndNothingElse(char previous, bool opensAComment)
	{
		string text = $"{previous}# note";

		Assert.Equal(opensAComment, PythonShapedProfile.HashIsNotAFormatSpecMark(text, 1));
		Assert.True(PythonShapedProfile.HashIsNotAFormatSpecMark("# note", 0));
	}

	[Theory]
	[InlineData('f', false)]
	[InlineData('7', false)]
	[InlineData('_', false)]
	[InlineData(' ', true)]
	[InlineData('(', true)]
	[InlineData('=', true)]
	public void ThePrefixPredicateRefusesAPrefixThatEndsALongerWord(char previous, bool opens)
	{
		string text = $"{previous}f\"a\"";

		Assert.Equal(opens, PythonShapedProfile.PrefixDoesNotEndAWord(text, 1));
		Assert.True(PythonShapedProfile.PrefixDoesNotEndAWord("f\"a\"", 0));
	}

	[Fact]
	public void TheConstantDeclarationReadsALiteralAndAnEnvironmentFormWithTheirGroups()
	{
		var literal = PythonShapedProfile.ConstantDeclaration().Match("AUTH_SCHEME: str = \"Basic\"");
		var environment = PythonShapedProfile.ConstantDeclaration().Match("token = os.environ.get(\"TOKEN\", \"bearer x\")");

		Assert.True(literal.Success);
		Assert.Equal("Basic", literal.Groups["value"].Value);
		Assert.True(environment.Success);
		Assert.True(environment.Groups["environment"].Success);
		Assert.Equal("bearer", environment.Groups["fallbackWord"].Value);
	}
}
