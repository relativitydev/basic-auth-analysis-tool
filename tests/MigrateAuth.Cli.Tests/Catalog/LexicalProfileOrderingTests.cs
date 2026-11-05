using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The longest opening wins and the engine orders the rules, not the profile author. An author
/// listing the single quote before the triple cannot break the scanner; these tests prove that
/// outcome is unreachable rather than merely unlikely.
/// </summary>
public sealed class LexicalProfileOrderingTests
{
	private const string _PROFILE_NAME = "ordering-probe";
	private const string _SHORT_OPENING = "'";
	private const string _MEDIUM_OPENING = "\"\"";
	private const string _LONG_OPENING = "\"\"\"";
	private const string _SHORT_LINE_COMMENT_OPENING = "#";
	private const string _LONG_LINE_COMMENT_OPENING = "#--";
	private const string _SHORT_BLOCK_COMMENT_OPENING = "<#";
	private const string _LONG_BLOCK_COMMENT_OPENING = "<#[[";
	private const string _BLOCK_COMMENT_CLOSING = "#>";
	private const string _ANY_CONSTANT_DECLARATION_PATTERN = @"^\s*const\s+(?<name>\w+)";
	private const string _AUTHOR_SUPPLIED_QUOTE_RULES_PROPERTY = "QuoteRules";

	[Fact]
	public void EngineOrdersQuoteRulesLongestOpeningFirstWhateverOrderTheAuthorWrote()
	{
		LexicalProfile shortestFirst = BuildProfile([_SHORT_OPENING, _MEDIUM_OPENING, _LONG_OPENING]);
		LexicalProfile longestFirst = BuildProfile([_LONG_OPENING, _MEDIUM_OPENING, _SHORT_OPENING]);

		IReadOnlyList<string> shortestFirstOrder = ReadOpenings(shortestFirst.QuoteRulesInEngineOrder);
		IReadOnlyList<string> longestFirstOrder = ReadOpenings(longestFirst.QuoteRulesInEngineOrder);

		Assert.Equal(shortestFirstOrder, longestFirstOrder);
		Assert.Equal([_LONG_OPENING, _MEDIUM_OPENING, _SHORT_OPENING], shortestFirstOrder);
	}

	[Fact]
	public void EngineOrdersCommentRulesLongestOpeningFirstToo()
	{
		LexicalProfile profile = BuildProfile([_SHORT_OPENING]);

		Assert.Equal(_LONG_LINE_COMMENT_OPENING, profile.LineCommentRulesInEngineOrder[0].Opening);
		Assert.Equal(_SHORT_LINE_COMMENT_OPENING, profile.LineCommentRulesInEngineOrder[1].Opening);
		Assert.Equal(_LONG_BLOCK_COMMENT_OPENING, profile.BlockCommentRulesInEngineOrder[0].Opening);
		Assert.Equal(_SHORT_BLOCK_COMMENT_OPENING, profile.BlockCommentRulesInEngineOrder[1].Opening);
	}

	/// <summary>
	/// The author's order is not merely overridden - it is unreadable. There is no accessor
	/// that returns the sequence as it was supplied, so no later component can reach for it.
	/// </summary>
	[Fact]
	public void ProfileOffersNoWayToReadBackTheOrderTheAuthorSupplied()
	{
		PropertyInfo authorSuppliedRules = typeof(LexicalProfile).GetProperty(_AUTHOR_SUPPLIED_QUOTE_RULES_PROPERTY)!;

		Assert.Null(authorSuppliedRules.GetMethod);
		Assert.NotNull(authorSuppliedRules.SetMethod);
	}

	private static LexicalProfile BuildProfile(IReadOnlyList<string> quoteOpeningsInAuthorOrder)
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_ANY_CONSTANT_DECLARATION_PATTERN),
			LineCommentRules =
			[
				new LineCommentRule { Opening = _SHORT_LINE_COMMENT_OPENING },
				new LineCommentRule { Opening = _LONG_LINE_COMMENT_OPENING }
			],
			BlockCommentRules =
			[
				new BlockCommentRule { Opening = _SHORT_BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING },
				new BlockCommentRule { Opening = _LONG_BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
			],
			QuoteRules = BuildQuoteRules(quoteOpeningsInAuthorOrder)
		};

		return profile;
	}

	private static ImmutableArray<QuoteRule> BuildQuoteRules(IReadOnlyList<string> openingsInAuthorOrder)
	{
		ImmutableArray<QuoteRule> rules =
		[
			.. openingsInAuthorOrder.Select(opening => new QuoteRule
			{
				Opening = opening,
				Closing = opening,
				NewlinePolicy = NewlinePolicy.Forbidden
			})
		];

		return rules;
	}

	private static IReadOnlyList<string> ReadOpenings(ImmutableArray<QuoteRule> rules)
	{
		IReadOnlyList<string> openings = [.. rules.Select(rule => rule.Opening)];

		return openings;
	}
}
