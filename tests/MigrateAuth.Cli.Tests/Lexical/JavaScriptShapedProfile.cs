using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// A TEST-LOCAL PROFILE in the shape of the JavaScript lexical vocabulary, built here from the profile vocabulary and not read from any
/// shipped catalog. It declares exactly these constructs and nothing more: <c>//</c> and <c>/* */</c>; the hashbang as a <c>#!</c> line
/// comment that opens only at position zero; nothing for a private <c>#field</c>; the two quotes that continue on an escaped line feed; the
/// template with its <c>${</c> hole and brace nesting; the regular expression as a quote rule with a forbidden newline, a bracket suspended
/// sub-range and the backward predicate of <see cref="RegexOpeningStandsHere"/>; and the constant declaration expression. HTML-like comments
/// and JSX are NOT modelled, and the profile declares no code escape, no arithmetic context and no executed strings.
///
/// It exists so that every one of those declarations can be run on real-shaped text, on the shipped scanner, independently of any
/// catalog. It is verified on invented text only.
/// </summary>
internal static partial class JavaScriptShapedProfile
{
	private const string _PROFILE_NAME = "JavaScriptShapedTestProfile";
	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";
	private const string _HASHBANG_OPENING = "#!";
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
	private const int _NOT_FOUND = -1;
	private const string _DIVIDING_CHARACTERS = ")]}/'\"`";

	private static readonly HashSet<string> _regexOpeningKeywords = new(StringComparer.Ordinal)
	{
		"return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw", "case", "do", "else", "yield", "await"
	};

	/// <summary>
	/// A statement-start declaration of a string literal: the optional <c>const</c>, <c>let</c> or <c>var</c>, or none (a bare
	/// <c>NAME = literal</c>, so a rebinding counts). The value is a single-quoted, double-quoted or hole-free single-line template literal that
	/// ends the statement; an escaped character inside the literal is part of it (the two branches of the repetition start with different
	/// characters, so the match stays linear). Anchored at the line start, one line, so a declaration after a brace or a semicolon on the
	/// same line is not counted. An optional <c>export</c> may precede the declaration. A dollar sign in a template is part of the value unless
	/// a brace follows it (that is a hole, which is not counted).
	/// </summary>
	[GeneratedRegex(
		@"^[ \t]*(?:export[ \t]+)?(?:(?:const|let|var)[ \t]+)?(?<name>[A-Za-z_$][A-Za-z0-9_$]*)[ \t]*=(?!=)[ \t]*(?:'(?<value>(?:[^'\\\n]|\\.)*)'|""(?<value>(?:[^""\\\n]|\\.)*)""|`(?<value>(?:[^`$\\\n]|\$(?!\{)|\\.)*)`)(?=[ \t]*(?:[;,]|$|//|/\*))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	internal static partial Regex ConstantDeclaration();

	internal static LexicalProfile Build()
	{
		return new LexicalProfile
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = ConstantDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Sensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = _LINE_COMMENT_OPENING },
				new LineCommentRule { Opening = _HASHBANG_OPENING, OpeningContextPredicate = HashbangStandsAtTheStartOfTheText }
			],
			BlockCommentRules =
			[
				new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
			],
			QuoteRules = BuildQuoteRules()
		};
	}

	internal static ZoneMask Scan(string source)
	{
		return new LexicalScanner(Build()).Scan(source);
	}

	private static ImmutableArray<QuoteRule> BuildQuoteRules()
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
				OpeningContextPredicate = RegexOpeningStandsHere
			}
		];
	}

	private static QuoteRule BuildStringRule(string quote)
	{
		return new QuoteRule
		{
			Opening = quote,
			Closing = quote,
			NewlinePolicy = NewlinePolicy.EscapedContinues,
			EscapeCharacter = _ESCAPE_CHARACTER
		};
	}

	/// <summary>The hashbang opens only at the very first position of the text (the decoder strips a byte-order mark, so position zero is the first character of the file).</summary>
	private static bool HashbangStandsAtTheStartOfTheText(ReadOnlySpan<char> text, int delimiterPosition)
	{
		return delimiterPosition == _HASHBANG_POSITION;
	}

	/// <summary>
	/// THE BACKWARD PREDICATE. True when the slash at <paramref name="delimiterPosition"/> opens a regular
	/// expression literal, false when it divides. Read backwards from the slash over whitespace, line feeds included, for at most
	/// <see cref="ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS"/> characters (then it answers "opens"). The previous significant character
	/// decides: start of text and the operator and opening characters open; a less-than sign opens except a slash directly after it (a JSX closing
	/// tag); a closing parenthesis, bracket or BRACE divides (a regular expression that starts a statement after a block is read as a division, a
	/// declared cost, which keeps a JSX self-closing tag after an expression hole out of the regular-expression reading); <c>++</c> and <c>--</c>
	/// divide; a slash divides; a string, template or identifier character divides, unless the word is one of the keywords of
	/// <see cref="_regexOpeningKeywords"/> and is not preceded by a dot (or <c>?.</c>). Any other character (for example a dot, an at sign or a
	/// hash) is treated as opening: a slash read as a
	/// regular expression that is really a division recovers at the line end, the opposite error can abandon the tail.
	/// </summary>
	private static bool RegexOpeningStandsHere(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int previous = FindThePreviousSignificantCharacter(text, delimiterPosition);
		bool opens = true;

		if (previous != _NOT_FOUND)
		{
			opens = PreviousCharacterOpensARegularExpression(text, previous, delimiterPosition);
		}

		return opens;
	}

	/// <summary>The position of the nearest non-whitespace character before the slash, or <see cref="_NOT_FOUND"/> at the start of the text or when the whitespace gap reaches the ceiling.</summary>
	private static int FindThePreviousSignificantCharacter(ReadOnlySpan<char> text, int delimiterPosition)
	{
		int found = _NOT_FOUND;
		int offset = 1;

		while (found == _NOT_FOUND && offset <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS && delimiterPosition - offset >= 0)
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
			opens = previous != slashPosition - 1;
		}
		else if (character is _PLUS or _MINUS)
		{
			opens = previous == 0 || text[previous - 1] != character;
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
		int length = 1;

		while (start > 0 && length <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS && IsIdentifierCharacter(text[start - 1]))
		{
			start--;
			length++;
		}

		bool afterADot = start > 0 && text[start - 1] == _DOT;

		return !afterADot && _regexOpeningKeywords.GetAlternateLookup<ReadOnlySpan<char>>().Contains(text.Slice(start, length));
	}

	private static bool IsIdentifierCharacter(char character)
	{
		return char.IsLetterOrDigit(character) || character == _UNDERSCORE || character == _DOLLAR;
	}
}
