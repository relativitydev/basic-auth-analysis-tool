using System.Collections.Immutable;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SHAPES THE `Go` LEXICAL-BUDGET GATE TIMES ARE WHAT THEY SAY THEY ARE, with no clock: the gate times the shipped profile of the registered analyzer on <see cref="GoLexicalBudgetMeasurements.BuildShapes"/>. This class
/// holds the profile to the declaration the gate is written for (three quote rules, two comment rules, no predicate, no first-line rule), and every shape to what the reader accepts, so a window never fails on a shape that
/// was declared wrongly. The gate is timed only in a timing window; this class runs in the normal suite and times nothing.
/// </summary>
public sealed class GoLexicalBudgetShapeTests
{
	private const string _LINE_FEED = "\n";
	private const int _CEILING_FRACTION_DIVISOR = 2;
	private const int _QUOTE_RULE_COUNT = 3;
	private const int _COMMENT_RULE_COUNT = 2;

	[Fact]
	public void TheShippedProfileIsTheOneTheGateIsWrittenForThreeQuoteRulesTwoCommentRulesAndNoPredicate()
	{
		LexicalProfile profile = GoRegisteredExpressions.Analyzer.LexicalProfile;

		Assert.Equal(_QUOTE_RULE_COUNT, profile.QuoteRulesInEngineOrder.Length);
		Assert.Equal(_COMMENT_RULE_COUNT, profile.LineCommentRulesInEngineOrder.Length + profile.BlockCommentRulesInEngineOrder.Length);
		Assert.All(profile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.OpeningContextPredicate));
		Assert.All(profile.LineCommentRulesInEngineOrder, rule => Assert.Null(rule.OpeningContextPredicate));
		Assert.All(profile.BlockCommentRulesInEngineOrder, rule => Assert.Null(rule.OpeningContextPredicate));
		Assert.All(profile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.InterpolationOpening));
		Assert.Null(GoRegisteredExpressions.Analyzer.LanguageIdentification.FirstLineDetectionExpression);
	}

	[Fact]
	public void EveryShapeIsLegalForTheReaderAndFillsTheFileToJustUnderTheCeiling()
	{
		List<string> refused = [];

		foreach (MatchBoundStressShape shape in GoLexicalBudgetMeasurements.BuildShapes())
		{
			int bytes = Encoding.UTF8.GetByteCount(shape.Text);
			bool tooLarge = bytes > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;
			bool tooSmall = bytes < ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES / _CEILING_FRACTION_DIVISOR;
			bool lineTooLong = shape.Text.Split(_LINE_FEED).Any(line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);
			bool illegal = shape.Text.Contains('\r') || shape.FileExtension != GoShippedCostGateStressSet.FILE_EXTENSION;

			if (tooLarge || tooSmall || lineTooLong || illegal)
			{
				refused.Add($"{shape.Name}: bytes={bytes} lineTooLong={lineTooLong} extension={shape.FileExtension}");
			}
		}

		Assert.Empty(refused);
	}

	[Fact]
	public void EveryShapeNameIsUniqueCarriesThePrefixAndTheIdentifierDenseShapeTheDecisionNamesLeadsTheSet()
	{
		ImmutableArray<MatchBoundStressShape> shapes = GoLexicalBudgetMeasurements.BuildShapes();

		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.All(shapes, shape => Assert.StartsWith(GoLexicalBudgetMeasurements.SHAPE_NAME_PREFIX, shape.Name, StringComparison.Ordinal));
		Assert.Equal(GoLexicalBudgetMeasurements.IDENTIFIER_DENSE_SHAPE_NAME, shapes[0].Name);
	}

	[Fact]
	public void TheSetPutsEachRuleToWorkTheThreeLiteralsClosedAndNeverClosedTheTwoCommentsAndTheDivisionOperator()
	{
		ImmutableArray<MatchBoundStressShape> shapes = GoLexicalBudgetMeasurements.BuildShapes();
		string[] families =
		[
			"interpreted-strings-closed", "rune-literals", "raw-strings-closed", "interpreted-strings-never-closed", "rune-literals-never-closed", "raw-string-opened-and-never-closed",
			"line-comments-holding-the-other-openers", "block-comments-closed", "block-comment-opened-and-never-closed", "division-against-the-comment-openers", "ordinary-code-at-the-ceiling"
		];

		Assert.All(families, family => Assert.Contains(shapes, shape => shape.Name.EndsWith(family, StringComparison.Ordinal)));
	}
}
