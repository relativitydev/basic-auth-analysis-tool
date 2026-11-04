using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The pieces of the `JavaScript` lexical profile that a sibling dialect composes from rather than copies: the four quote rules, the string rule, the backward predicate that decides
/// whether a slash opens a regular expression with its three helpers and the keyword set it reads, and the predicate that lets the hashbang open only at the first position. A change
/// to the predicate reaches every profile composed from these parts by construction. The `JavaScript` analyzer composes its profile from them.
/// </summary>
internal static class JavaScriptLexicalParts
{
	private const string _SINGLE_QUOTE = "'";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _TEMPLATE_QUOTE = "`";
	private const string _TEMPLATE_HOLE_OPENING = "${";
	private const string _TEMPLATE_HOLE_CLOSING = "}";
	private const string _TEMPLATE_HOLE_NESTING_OPENING = "{";
	private const string _REGEX_DELIMITER = "/";
	private const string _CLASS_OPENING = "[";
	private const string _CLASS_CLOSING = "]";

	private const char _ESCAPE_CHARACTER = '\\';
	private const char _LESS_THAN = '<';
	private const char _PLUS = '+';
	private const char _MINUS = '-';
	private const char _DOT = '.';
	private const char _UNDERSCORE = '_';
	private const char _DOLLAR = '$';
	private const int _HASHBANG_POSITION = 0;
	private const int _FIRST_INDEX = 0;
	private const int _INITIAL_WORD_LENGTH = 1;
	private const int _NOT_FOUND = -1;
	private const int _ONE_CHARACTER_BEFORE = 1;

	/// <summary>The characters after which a slash divides: the closers of a group, an index, a block, a literal or a regular expression, and the slash itself (the end of a regular expression or of a block comment).</summary>
	private const string _DIVIDING_CHARACTERS = ")]}/'\"`";

	/// <summary>The words after which a slash opens a regular expression: the keywords that stand where an operand is expected.</summary>
	private static readonly FrozenSet<string> _regexOpeningKeywords = new[]
	{
		"return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw", "case", "do", "else", "yield", "await"
	}.ToFrozenSet(StringComparer.Ordinal);

	/// <summary>
	/// Four quote rules. The single and double quoted strings continue on an escaped line feed and end at a raw one; the template takes newlines, escapes with a
	/// backslash and has one bracketed hole whose braces nest; the regular expression forbids a newline, escapes with a backslash, suspends its closing inside a character class
	/// and opens only where the backward predicate says a regular expression can start. A raw template, a tagged template and a private name need no rule of their own.
	/// </summary>
	internal static ImmutableArray<QuoteRule> BuildQuoteRules(DelimiterContextPredicate regexOpeningPredicate)
	{
		return
		[
			BuildStringRule(_SINGLE_QUOTE),
			BuildStringRule(_DOUBLE_QUOTE),
			new QuoteRule
			{
				Opening = _TEMPLATE_QUOTE,
				Closing = _TEMPLATE_QUOTE,
				NewlinePolicy = NewlinePolicy.Allowed,
				EscapeCharacter = _ESCAPE_CHARACTER,
				InterpolationOpening = _TEMPLATE_HOLE_OPENING,
				InterpolationClosing = _TEMPLATE_HOLE_CLOSING,
				InterpolationNestingOpening = _TEMPLATE_HOLE_NESTING_OPENING
			},
			new QuoteRule
			{
				Opening = _REGEX_DELIMITER,
				Closing = _REGEX_DELIMITER,
				NewlinePolicy = NewlinePolicy.Forbidden,
				EscapeCharacter = _ESCAPE_CHARACTER,
				SuspendedSubRange = new SuspendedSubRange { Opening = _CLASS_OPENING, Closing = _CLASS_CLOSING },
				OpeningContextPredicate = regexOpeningPredicate
			}
		];
	}

	internal static QuoteRule BuildStringRule(string quote)
	{
		return new QuoteRule
		{
			Opening = quote,
			Closing = quote,
			NewlinePolicy = NewlinePolicy.EscapedContinues,
			EscapeCharacter = _ESCAPE_CHARACTER
		};
	}

	/// <summary>The hashbang opens only at the very first position of the text: the decoder strips a byte-order mark, so position zero is the first character of the file, and an <c>#!</c> anywhere else is code.</summary>
	internal static bool HashbangStandsAtTheStartOfTheText(ReadOnlySpan<char> text, int delimiterPosition)
	{
		return delimiterPosition == _HASHBANG_POSITION;
	}

	/// <summary>
	/// THE BACKWARD PREDICATE, one pure function of the text and the position. True when the slash at <paramref name="delimiterPosition"/> opens a regular expression
	/// literal, false when it divides. It reads backwards from the slash over white space, line feeds included, for at most
	/// <see cref="ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS"/> characters, and answers "opens" when it reaches the start of the text or that ceiling. The previous
	/// significant character decides: an operator, an opening bracket, a comma, a colon, a semicolon, a question mark, an exclamation mark or a greater-than sign opens; a
	/// less-than sign opens except a slash directly after it (a closing tag); a closing parenthesis, bracket or brace divides (the closing brace keeps a self-closing tag
	/// after an expression hole out of the regular-expression reading); <c>++</c> and <c>--</c> divide; a slash divides; a string, template or identifier character
	/// divides unless the word is one of the keywords that stand where an operand is expected and is not a property name; any other character opens.
	/// </summary>
	internal static bool RegexOpeningStandsHere(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int previous = FindThePreviousSignificantCharacter(text, delimiterPosition);
		bool opens = true;

		if (previous != _NOT_FOUND)
		{
			opens = PreviousCharacterOpensARegularExpression(text, previous, delimiterPosition);
		}

		return opens;
	}

	/// <summary>The position of the nearest non-white-space character before the slash, or <see cref="_NOT_FOUND"/> at the start of the text or when the gap reaches the ceiling.</summary>
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

	private static bool PreviousCharacterOpensARegularExpression(ReadOnlySpan<char> text, int previous, int slashPosition)
	{
		char character = text[previous];
		bool opens;

		if (character == _LESS_THAN)
		{
			opens = previous != slashPosition - _ONE_CHARACTER_BEFORE;
		}
		else if (character is _PLUS or _MINUS)
		{
			opens = previous == _FIRST_INDEX || text[previous - _ONE_CHARACTER_BEFORE] != character;
		}
		else if (_DIVIDING_CHARACTERS.Contains(character, StringComparison.Ordinal))
		{
			opens = false;
		}
		else if (IsIdentifierCharacter(character))
		{
			opens = WordIsAnOpeningKeyword(text, previous);
		}
		else
		{
			opens = true;
		}

		return opens;
	}

	/// <summary>The identifier word that ends at the position is an opening keyword and is not a property name (not preceded by a dot, which also covers <c>?.</c>).</summary>
	private static bool WordIsAnOpeningKeyword(ReadOnlySpan<char> text, int lastCharacter)
	{
		int start = lastCharacter;
		int length = _INITIAL_WORD_LENGTH;

		while (start > _FIRST_INDEX && length <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS && IsIdentifierCharacter(text[start - _ONE_CHARACTER_BEFORE]))
		{
			start--;
			length++;
		}

		bool afterADot = start > _FIRST_INDEX && text[start - _ONE_CHARACTER_BEFORE] == _DOT;

		return !afterADot && _regexOpeningKeywords.GetAlternateLookup<ReadOnlySpan<char>>().Contains(text.Slice(start, length));
	}

	private static bool IsIdentifierCharacter(char character)
	{
		return char.IsLetterOrDigit(character) || character == _UNDERSCORE || character == _DOLLAR;
	}
}
