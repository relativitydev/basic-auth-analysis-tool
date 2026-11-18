using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SHAPE OF THE SHIPPED `Python` LEXICAL PROFILE, read from the registered analyzer: 84 quote rules (four plain ones carrying the 14 plain prefixes, eighty expanded ones for the ten `f` and the ten `t` prefixes
/// crossed with the four quotes), one line comment guarded by the hash predicate, no block comment, no code escape, arithmetic context, executed string, code span or file dialect, and no prefix that suppresses an
/// escape: a raw literal still protects its quote. The profile's behaviour on text is in <see cref="PythonLexicalProfileHardFormTests"/> and its equality with the probed profile in
/// <see cref="PythonLexicalProfileEqualityTests"/>.
/// </summary>
public sealed class PythonLexicalProfileTests
{
	private const int _EXPECTED_QUOTE_RULE_COUNT = 84;
	private const int _EXPECTED_PLAIN_RULE_COUNT = 4;
	private const int _EXPECTED_PLAIN_PREFIX_COUNT = 14;
	private const int _EXPECTED_FORMAT_PREFIX_COUNT = 10;
	private const int _EXPECTED_TEMPLATE_PREFIX_COUNT = 10;
	private const int _EXPECTED_QUOTE_KIND_COUNT = 4;
	private const string _PROFILE_NAME = "Python";

	private static readonly string[] _quotes = ["\"\"\"", "'''", "\"", "'"];
	private static readonly string[] _formatPrefixes = ["f", "F", "rf", "rF", "Rf", "RF", "fr", "fR", "Fr", "FR"];
	private static readonly string[] _templatePrefixes = ["t", "T", "rt", "rT", "Rt", "RT", "tr", "tR", "Tr", "TR"];
	private static readonly string[] _plainPrefixes = ["r", "R", "b", "B", "u", "U", "rb", "rB", "Rb", "RB", "br", "bR", "Br", "BR"];

	private static readonly LexicalProfile _profile = PythonShippedAnalyzer.Instance.LexicalProfile;

	[Fact]
	public void TheProfileIsNamedPythonAndDeclaresCaseSensitiveKeywordsDelimitersAndIdentifiers()
	{
		Assert.Equal(_PROFILE_NAME, _profile.ProfileName);
		Assert.Equal(CaseSensitivity.Sensitive, _profile.KeywordCaseSensitivity);
		Assert.Equal(CaseSensitivity.Sensitive, _profile.DelimiterCaseSensitivity);
		Assert.Equal(IdentifierCaseSensitivityMode.Sensitive, _profile.IdentifierCaseSensitivity);
	}

	[Fact]
	public void TheProfileDeclaresEightyFourQuoteRulesOneLineCommentAndNoBlockComment()
	{
		Assert.Equal(_EXPECTED_QUOTE_RULE_COUNT, _profile.QuoteRulesInEngineOrder.Length);
		Assert.Equal("#", Assert.Single(_profile.LineCommentRulesInEngineOrder).Opening);
		Assert.NotNull(_profile.LineCommentRulesInEngineOrder[0].OpeningContextPredicate);
		Assert.Empty(_profile.BlockCommentRulesInEngineOrder);
	}

	[Fact]
	public void TheProfileDeclaresNoCodeEscapeArithmeticContextExecutedStringCodeSpanOrFileDialect()
	{
		Assert.Null(_profile.CodeEscapeCharacter);
		Assert.Null(_profile.ArithmeticContext);
		Assert.Null(_profile.ExecutedStrings);
		Assert.Empty(_profile.CodeSpans);
		Assert.Null(PythonShippedAnalyzer.Instance.CandidateSelectionAndRecommendation.FileDialects);
	}

	[Fact]
	public void TheFourPlainRulesCarryTheFourteenPlainPrefixesAndNoInterpolationAndNoPredicate()
	{
		QuoteRule[] plainRules = [.. _profile.QuoteRulesInEngineOrder.Where(rule => rule.InterpolationOpening is null)];

		Assert.Equal(_EXPECTED_PLAIN_RULE_COUNT, plainRules.Length);
		Assert.Equal(_quotes.Order(StringComparer.Ordinal), plainRules.Select(rule => rule.Opening).Order(StringComparer.Ordinal));
		Assert.All(plainRules, rule =>
		{
			Assert.Equal(_plainPrefixes.Order(StringComparer.Ordinal), rule.LiteralPrefixes.Select(prefix => prefix.Prefix).Order(StringComparer.Ordinal));
			Assert.Equal(_EXPECTED_PLAIN_PREFIX_COUNT, rule.LiteralPrefixes.Length);
			Assert.Equal(rule.Opening, rule.Closing);
			Assert.Equal('\\', rule.EscapeCharacter);
			Assert.Null(rule.OpeningContextPredicate);
		});
	}

	[Fact]
	public void NoPlainPrefixSuppressesTheEscapeBecauseARawLiteralStillProtectsItsQuote()
	{
		Assert.All(
			_profile.QuoteRulesInEngineOrder.SelectMany(rule => rule.LiteralPrefixes),
			prefix => Assert.Equal(EscapeHandling.AsDeclaredByTheQuoteRule, prefix.EscapeHandling));
	}

	[Fact]
	public void TheEightyExpandedRulesAreTheTwentyInterpolatingPrefixesTimesTheFourQuotesWithNestedHolesAndAPredicate()
	{
		QuoteRule[] expandedRules = [.. _profile.QuoteRulesInEngineOrder.Where(rule => rule.InterpolationOpening is not null)];
		string[] expectedOpenings =
		[
			.. _formatPrefixes.Concat(_templatePrefixes).SelectMany(_ => _quotes, (prefix, quote) => prefix + quote).Order(StringComparer.Ordinal)
		];

		Assert.Equal((_EXPECTED_FORMAT_PREFIX_COUNT + _EXPECTED_TEMPLATE_PREFIX_COUNT) * _EXPECTED_QUOTE_KIND_COUNT, expandedRules.Length);
		Assert.Equal(expectedOpenings, expandedRules.Select(rule => rule.Opening).Order(StringComparer.Ordinal));
		Assert.All(expandedRules, rule =>
		{
			Assert.Equal("{", rule.InterpolationOpening);
			Assert.Equal("}", rule.InterpolationClosing);
			Assert.Equal("{", rule.InterpolationNestingOpening);
			Assert.True(rule.DoubledInterpolationOpeningIsLiteral);
			Assert.Empty(rule.LiteralPrefixes);
			Assert.NotNull(rule.OpeningContextPredicate);
			Assert.Equal('\\', rule.EscapeCharacter);
		});
	}

	[Fact]
	public void TheTemplatePrefixesOfPythonThreeFourteenAreDeclaredAsRulesAndNotOnlyTheFormatPrefixes()
	{
		string[] openings = [.. _profile.QuoteRulesInEngineOrder.Select(rule => rule.Opening)];

		Assert.All(_templatePrefixes, prefix => Assert.All(_quotes, quote => Assert.Contains(prefix + quote, openings)));
	}

	[Fact]
	public void EverySingleQuotedRuleContinuesOnlyAcrossAnEscapedLineFeedAndEveryTripleQuotedRuleTakesANewlineAsContent()
	{
		Assert.All(_profile.QuoteRulesInEngineOrder, rule =>
		{
			bool triple = rule.Closing.Length == _quotes[0].Length;

			Assert.Equal(triple ? NewlinePolicy.Allowed : NewlinePolicy.EscapedContinues, rule.NewlinePolicy);
		});
	}

	[Fact]
	public void TheConstantDeclarationExpressionCarriesThePolicyMatchBoundAsItsTimeout()
	{
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, _profile.ConstantDeclarationExpression.MatchTimeout);
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
	[InlineData('}', true)]
	[InlineData('\t', true)]
	[InlineData('\n', true)]
	public void TheHashPredicateRefusesTheSevenFormatSpecPredecessorsAndNothingElseAndOpensAtPositionZero(char previous, bool opensAComment)
	{
		DelimiterContextPredicate predicate = _profile.LineCommentRulesInEngineOrder.Single().OpeningContextPredicate!;

		Assert.Equal(opensAComment, predicate($"{previous}# note", 1));
		Assert.True(predicate("# note", 0));
	}

	[Theory]
	[InlineData('f', false)]
	[InlineData('Z', false)]
	[InlineData('7', false)]
	[InlineData('_', false)]
	[InlineData(' ', true)]
	[InlineData('(', true)]
	[InlineData('=', true)]
	[InlineData(',', true)]
	[InlineData('\n', true)]
	public void ThePrefixPredicateRefusesAPrefixThatEndsALongerWordAndOpensAtPositionZero(char previous, bool opens)
	{
		DelimiterContextPredicate predicate = _profile.QuoteRulesInEngineOrder.First(rule => rule.InterpolationOpening is not null).OpeningContextPredicate!;

		Assert.Equal(opens, predicate($"{previous}f\"a\"", 1));
		Assert.True(predicate("f\"a\"", 0));
	}

	[Fact]
	public void TheConstantDeclarationReadsALiteralAndAnEnvironmentFormWithTheirGroups()
	{
		var literal = _profile.ConstantDeclarationExpression.Match("AUTH_SCHEME: str = \"Basic\"");
		var environment = _profile.ConstantDeclarationExpression.Match("token = os.environ.get(\"TOKEN\", \"bearer x\")");

		Assert.True(literal.Success);
		Assert.Equal("Basic", literal.Groups["value"].Value);
		Assert.True(environment.Success);
		Assert.True(environment.Groups["environment"].Success);
		Assert.Equal("bearer", environment.Groups["fallbackWord"].Value);
	}
}
