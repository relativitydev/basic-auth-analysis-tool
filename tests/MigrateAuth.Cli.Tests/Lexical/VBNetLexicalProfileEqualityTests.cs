using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SHIPPED `VBNet` PROFILE IS PINNED to <see cref="VisualBasicShapedProfile"/>, the reference the real-shaped text tests run on:
/// the same rules, in the same order, field by field, with the context predicates compared by what they answer; and the same masks on the same text.
/// </summary>
public sealed class VBNetLexicalProfileEqualityTests
{
	private const string _REMARK_OPENING = "REM";

	private static readonly LexicalProfile _shipped = VBNetRegisteredExpressions.Analyzer.LexicalProfile;

	private static readonly LexicalProfile _reference = VisualBasicShapedProfile.Build();

	[Fact]
	public void TheShippedProfileDeclaresTheCaseSensitivitiesOfTheReference()
	{
		Assert.Equal(_reference.KeywordCaseSensitivity, _shipped.KeywordCaseSensitivity);
		Assert.Equal(_reference.DelimiterCaseSensitivity, _shipped.DelimiterCaseSensitivity);
		Assert.Equal(_reference.IdentifierCaseSensitivity, _shipped.IdentifierCaseSensitivity);
	}

	[Fact]
	public void TheShippedLineCommentRulesEqualTheReferenceOnesInOrderOnceThePredicateIsSetAside()
	{
		Assert.Equal(
			_reference.LineCommentRulesInEngineOrder.Select(rule => rule with { OpeningContextPredicate = null }),
			_shipped.LineCommentRulesInEngineOrder.Select(rule => rule with { OpeningContextPredicate = null }));
		Assert.Empty(_reference.BlockCommentRulesInEngineOrder);
		Assert.Empty(_shipped.BlockCommentRulesInEngineOrder);
	}

	[Fact]
	public void TheShippedQuoteRulesEqualTheReferenceOnesInOrderAndTheInterpolatedRuleReadsADoubledOpenerAsLiteral()
	{
		Assert.Equal(
			_reference.QuoteRulesInEngineOrder.Select(rule => rule with { LiteralPrefixes = default }),
			_shipped.QuoteRulesInEngineOrder.Select(rule => rule with { LiteralPrefixes = default }));
		Assert.All(_shipped.QuoteRulesInEngineOrder, rule => Assert.Empty(rule.LiteralPrefixes));
		Assert.Contains(_shipped.QuoteRulesInEngineOrder, rule => rule.DoubledInterpolationOpeningIsLiteral);
	}

	[Theory]
	[InlineData("REM comment", 0, true)]
	[InlineData("x.REM comment", 2, true)]
	[InlineData("[REM] = 1", 1, false)]
	[InlineData("x = [REM]", 5, false)]
	public void TheRemarkPredicateAnswersAsTheReferenceDoes(string text, int position, bool expected)
	{
		LineCommentRule shipped = _shipped.LineCommentRulesInEngineOrder.Single(rule => rule.Opening == _REMARK_OPENING);
		LineCommentRule reference = _reference.LineCommentRulesInEngineOrder.Single(rule => rule.Opening == _REMARK_OPENING);

		Assert.Equal(expected, reference.OpeningContextPredicate!(text, position));
		Assert.Equal(expected, shipped.OpeningContextPredicate!(text, position));
	}

	[Theory]
	[InlineData("Dim a As Integer = 1 ' a comment \"quoted\"\nDim b As String = \"x\"\"y\"\n")]
	[InlineData("REM a remark\nDim s = $\"{{x}} {y}\"\n")]
	[InlineData("Dim s = \u201Cabc\u201D & \u201Dabc\u201D\n\u2018 comment\nDim n = 1\n")]
	[InlineData("Dim s = \"multi\nline\" ' tail\nDim n = [Rem]\n")]
	public void TheShippedProfileAndTheReferenceProduceTheSameMask(string source)
	{
		ZoneMask referenceMask = VisualBasicShapedProfile.Scan(source);
		ZoneMask shippedMask = new LexicalScanner(_shipped).Scan(source);

		Assert.Equal(referenceMask.IsCompletelyValid, shippedMask.IsCompletelyValid);

		for (int position = 0; position < source.Length; position++)
		{
			Assert.Equal(referenceMask.ZoneAt(position), shippedMask.ZoneAt(position));
		}
	}

	[Theory]
	[InlineData("Private Const Scheme As String = \"Basic\"", "Scheme", "Basic")]
	[InlineData("Dim s$ = \"x\"", "s", "x")]
	public void TheShippedConstantDeclarationReadsTheNameAndTheValueOfTheReference(string line, string name, string value)
	{
		Match shipped = _shipped.ConstantDeclarationExpression.Match(line);
		Match reference = _reference.ConstantDeclarationExpression.Match(line);

		Assert.True(shipped.Success);
		Assert.Equal(reference.Groups["name"].Value, shipped.Groups["name"].Value);
		Assert.Equal(reference.Groups["value"].Value.Trim('"'), shipped.Groups["value"].Value);
		Assert.Equal(name, shipped.Groups["name"].Value);
		Assert.Equal(value, shipped.Groups["value"].Value);
	}
}
