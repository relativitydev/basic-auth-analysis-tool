using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The lexical profile of the `TypeScript` analyzer: the pieces of the `JavaScript` profile composed by reference (<see cref="JavaScriptLexicalParts"/>) plus the one coded branch of
/// the backward predicate that `TypeScript` adds. The profile declares the same comment rules, the same quote rules and the same case switches as `JavaScript`; only
/// the predicate that decides whether a slash opens a regular expression differs, and only after an exclamation mark.
///
/// THE EXCLAMATION MARK. A <c>!</c> directly after an operand is the postfix non-null assertion, and a slash after it divides (<c>x!/2</c>); a <c>!</c> anywhere else is a logical not, and a
/// slash after it opens a regular expression (<c>!/re/.test(s)</c>, <c>return !/re/</c>). The decision reads the character directly before the mark, with no white space between: a closing
/// parenthesis, a closing bracket, a quote or a backtick, a digit, or an identifier character whose word is not one of the keywords that stand where an operand is expected. A closing brace is
/// not an operand end for this row. Each mark is examined by one slash and the word read is capped at the line ceiling, so the cost stays linear. The residual misreadings (a blank before the
/// mark, a closing brace before it, two marks in a row, JSX element text) open a literal at the slash that recovers at the line end.
/// </summary>
internal static class TypeScriptLexicalDeclarations
{
	private const string _PROFILE_NAME = "TypeScript";

	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";
	private const string _HASHBANG_OPENING = "#!";

	private const char _EXCLAMATION_MARK = '!';
	private const char _CLOSING_PARENTHESIS = ')';
	private const char _CLOSING_BRACKET = ']';
	private const char _SINGLE_QUOTE = '\'';
	private const char _DOUBLE_QUOTE = '"';
	private const char _BACKTICK = '`';
	private const char _UNDERSCORE = '_';
	private const char _DOLLAR = '$';

	private const int _FIRST_INDEX = 0;
	private const int _NOT_FOUND = -1;
	private const int _ONE_CHARACTER_BEFORE = 1;

	internal static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = JavaScriptPatternExpressions.TypeScriptConstantDeclaration(),
			MemberDeclarationExpression = JavaScriptPatternExpressions.MemberDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Sensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = _LINE_COMMENT_OPENING },
				new LineCommentRule { Opening = _HASHBANG_OPENING, OpeningContextPredicate = JavaScriptLexicalParts.HashbangStandsAtTheStartOfTheText }
			],
			BlockCommentRules =
			[
				new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
			],
			QuoteRules = JavaScriptLexicalParts.BuildQuoteRules(RegexOpeningStandsHere)
		};

		return profile;
	}

	/// <summary>
	/// True when the slash at <paramref name="delimiterPosition"/> opens a regular expression literal. The answer is that of the `JavaScript` predicate except where the previous
	/// significant character is an exclamation mark, which the postfix branch decides.
	/// </summary>
	internal static bool RegexOpeningStandsHere(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int previous = FindThePreviousSignificantCharacter(text, delimiterPosition);
		bool opens;

		if (previous != _NOT_FOUND && text[previous] == _EXCLAMATION_MARK)
		{
			opens = ExclamationMarkLeavesTheSlashOpening(text, previous);
		}
		else
		{
			opens = JavaScriptLexicalParts.RegexOpeningStandsHere(text, delimiterPosition);
		}

		return opens;
	}

	/// <summary>
	/// The coded branch. The mark is postfix, and the slash divides, only when the character directly before the mark ends an operand; any other neighbour (nothing, a blank, a line feed, a closing
	/// brace, an operator, an opening bracket, another mark) leaves the mark a logical not and the slash opening. For an operand end the `JavaScript` predicate is asked about the position of the mark itself,
	/// which answers "divides" for a closing parenthesis, bracket, quote or backtick and, for an identifier word, "opens" only for a regular-expression-opening keyword that is not a property name.
	/// </summary>
	private static bool ExclamationMarkLeavesTheSlashOpening(ReadOnlySpan<char> text, int markPosition)
	{
		bool opens = true;

		if (markPosition > _FIRST_INDEX && EndsAnOperand(text[markPosition - _ONE_CHARACTER_BEFORE]))
		{
			opens = JavaScriptLexicalParts.RegexOpeningStandsHere(text, markPosition);
		}

		return opens;
	}

	private static bool EndsAnOperand(char character)
	{
		return character is _CLOSING_PARENTHESIS or _CLOSING_BRACKET or _SINGLE_QUOTE or _DOUBLE_QUOTE or _BACKTICK
			|| char.IsLetterOrDigit(character)
			|| character == _UNDERSCORE
			|| character == _DOLLAR;
	}

	/// <summary>The position of the nearest non-white-space character before the slash, or <see cref="_NOT_FOUND"/> at the start of the text or when the gap reaches the line ceiling.</summary>
	private static int FindThePreviousSignificantCharacter(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int found = _NOT_FOUND;
		int offset = _ONE_CHARACTER_BEFORE;

		while (found == _NOT_FOUND && offset <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS && delimiterPosition - offset >= _FIRST_INDEX)
		{
			int candidate = delimiterPosition - offset;

			found = char.IsWhiteSpace(text[candidate]) ? _NOT_FOUND : candidate;
			offset++;
		}

		return found;
	}
}
