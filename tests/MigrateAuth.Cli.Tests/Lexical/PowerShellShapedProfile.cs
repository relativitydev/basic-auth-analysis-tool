using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// A TEST-LOCAL PROFILE in the shape of the second language's lexical vocabulary, built here from the
/// profile vocabulary and not read from the shipped catalog. It exists so the scanner paths that language
/// needs (a context predicate, positional anchoring, nested interpolation holes, typographic pairs) can
/// be run on real-shaped text.
/// </summary>
internal static class PowerShellShapedProfile
{
	private const string _PROFILE_NAME = "PowerShellShapedTestProfile";
	private const string _CONSTANT_PATTERN = @"^\s*\$(?<name>\w+)\s*=";
	private const string _LINE_COMMENT_OPENING = "#";
	private const string _BLOCK_COMMENT_OPENING = "<#";
	private const string _BLOCK_COMMENT_CLOSING = "#>";
	private const string _SINGLE_QUOTE = "'";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _EXPANDABLE_HERE_STRING_OPENING = "@\"";
	private const string _EXPANDABLE_HERE_STRING_CLOSING = "\"@";
	private const string _LITERAL_HERE_STRING_OPENING = "@'";
	private const string _LITERAL_HERE_STRING_CLOSING = "'@";
	private const string _TYPOGRAPHIC_DOUBLE_OPENING = "“";
	private const string _TYPOGRAPHIC_DOUBLE_CLOSING = "”";
	private const string _TYPOGRAPHIC_SINGLE_OPENING = "‘";
	private const string _TYPOGRAPHIC_SINGLE_CLOSING = "’";
	private const string _SUBEXPRESSION_OPENING = "$(";
	private const string _SUBEXPRESSION_CLOSING = ")";
	private const string _NESTING_OPENING = "(";
	private const char _ESCAPE_CHARACTER = '`';
	private const string _COMMENT_PREDECESSORS = ";(){}|&,=";

	internal static LexicalProfile Build(bool countsNestedParentheses)
	{
		return new LexicalProfile
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_CONSTANT_PATTERN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
			KeywordCaseSensitivity = CaseSensitivity.Insensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Insensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Insensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = _LINE_COMMENT_OPENING, OpeningContextPredicate = CommentOpeningStandsHere }
			],
			BlockCommentRules =
			[
				new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
			],
			QuoteRules = BuildQuoteRules(countsNestedParentheses)
		};
	}

	internal static ZoneMask Scan(string source, bool countsNestedParentheses)
	{
		return new LexicalScanner(Build(countsNestedParentheses)).Scan(source);
	}

	private static ImmutableArray<QuoteRule> BuildQuoteRules(bool countsNestedParentheses)
	{
		return
		[
			new QuoteRule
			{
				Opening = _SINGLE_QUOTE,
				Closing = _SINGLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				DoubledClosingEscapes = true,
				OpeningContextPredicate = QuoteOpeningIsNotBacktickEscaped
			},
			new QuoteRule
			{
				Opening = _DOUBLE_QUOTE,
				Closing = _DOUBLE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				EscapeCharacter = _ESCAPE_CHARACTER,
				DoubledClosingEscapes = true,
				InterpolationOpening = _SUBEXPRESSION_OPENING,
				InterpolationClosing = _SUBEXPRESSION_CLOSING,
				InterpolationNestingOpening = countsNestedParentheses ? _NESTING_OPENING : null,
				OpeningContextPredicate = QuoteOpeningIsNotBacktickEscaped
			},
			new QuoteRule
			{
				Opening = _EXPANDABLE_HERE_STRING_OPENING,
				Closing = _EXPANDABLE_HERE_STRING_CLOSING,
				NewlinePolicy = NewlinePolicy.Allowed,
				EscapeCharacter = _ESCAPE_CHARACTER,
				OpeningAnchoring = PositionalAnchoring.EndOfLineAfterOptionalBlanks,
				ClosingAnchoring = PositionalAnchoring.StartOfLine
			},
			new QuoteRule
			{
				Opening = _LITERAL_HERE_STRING_OPENING,
				Closing = _LITERAL_HERE_STRING_CLOSING,
				NewlinePolicy = NewlinePolicy.Allowed,
				OpeningAnchoring = PositionalAnchoring.EndOfLineAfterOptionalBlanks,
				ClosingAnchoring = PositionalAnchoring.StartOfLine
			},
			new QuoteRule
			{
				Opening = _TYPOGRAPHIC_DOUBLE_OPENING,
				Closing = _TYPOGRAPHIC_DOUBLE_CLOSING,
				NewlinePolicy = NewlinePolicy.Allowed,
				EscapeCharacter = _ESCAPE_CHARACTER,
				DoubledClosingEscapes = true
			},
			new QuoteRule
			{
				Opening = _TYPOGRAPHIC_SINGLE_OPENING,
				Closing = _TYPOGRAPHIC_SINGLE_CLOSING,
				NewlinePolicy = NewlinePolicy.Allowed,
				DoubledClosingEscapes = true
			}
		];
	}

	/// <summary>True at the start of the text, after whitespace, or after one of the statement and operator characters; false after a backtick or a word character.</summary>
	private static bool CommentOpeningStandsHere(ReadOnlySpan<char> text, int delimiterPosition)
	{
		bool stands = delimiterPosition == 0
			|| char.IsWhiteSpace(text[delimiterPosition - 1])
			|| _COMMENT_PREDECESSORS.Contains(text[delimiterPosition - 1]);

		return stands;
	}

	/// <summary>An opening quote preceded by an odd run of backticks is escaped text, not an opening.</summary>
	private static bool QuoteOpeningIsNotBacktickEscaped(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int run = 0;

		while (delimiterPosition - run - 1 >= 0 && text[delimiterPosition - run - 1] == _ESCAPE_CHARACTER)
		{
			run++;
		}

		return run % 2 == 0;
	}
}
