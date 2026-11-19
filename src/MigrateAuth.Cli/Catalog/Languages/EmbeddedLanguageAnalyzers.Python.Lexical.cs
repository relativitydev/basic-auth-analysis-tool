using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The lexical profile of the `Python` analyzer: the <c>#</c> line comment guarded by <see cref="HashIsNotAFormatSpecMark"/>, no block comment, and 84 quote rules. Four are plain (<c>"""</c>, <c>'''</c>,
/// <c>"</c>, <c>'</c>), carrying the 14 plain prefixes as literal prefixes whose escape is the rule's own (a raw literal still protects its quote, so no prefix suppresses the escape). Eighty are expanded: the ten
/// <c>f</c> prefixes and the ten <c>t</c> prefixes of 3.14 crossed with the four quotes, each with a bracketed <c>{</c> <c>}</c> replacement field whose braces nest, a doubled brace that is literal, and
/// <see cref="PrefixDoesNotEndAWord"/> as the opening predicate. No file dialect and no code escape, arithmetic context or executed string is declared.
///
/// WHY THE PLAIN PREFIXES RIDE ON LITERAL PREFIXES AND THE <c>f</c> AND <c>t</c> ONES ON EXPANDED OPENINGS. A literal prefix registers the bare opening as well as the prefixed one, so an interpolating rule that carried
/// prefixes would make its bare quote interpolate. The predicate is only on the expanded rules because it is called with the position of the first prefix letter of an expanded opening, which is what lets it
/// refuse <c>1 if"a" else 2</c>; for the plain rules the same words change nothing in the mask.
///
/// THE FORMAT SPECIFICATION IS NOT MODELLED. The text after the <c>:</c> of a replacement field is literal text in Python and the scanner reads the field interior as code. The predicate repairs most forms of a hash
/// there (the alternate-form flag after a colon, an alignment character, a sign or an equals sign) and declares the rest: a space sign, the <c>z</c> option, a quote used as a fill character and a backslash before a brace.
/// </summary>
internal static class PythonLexicalDeclarations
{
	private const string _PROFILE_NAME = "Python";

	private const string _LINE_COMMENT_OPENING = "#";
	private const string _TRIPLE_DOUBLE_QUOTE = "\"\"\"";
	private const string _TRIPLE_SINGLE_QUOTE = "'''";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _SINGLE_QUOTE = "'";
	private const string _REPLACEMENT_FIELD_OPENING = "{";
	private const string _REPLACEMENT_FIELD_CLOSING = "}";

	private const char _ESCAPE_CHARACTER = '\\';
	private const char _UNDERSCORE = '_';

	private const int _FIRST_POSITION = 0;
	private const int _ONE_CHARACTER_BEFORE = 1;
	private const int _SINGLE_QUOTE_LENGTH = 1;

	/// <summary>The characters that, directly before a hash, make it a character of a format specification and not a comment: the colon that introduces the specification, the alignment characters, the sign characters and the equals sign.</summary>
	private const string _FORMAT_SPEC_PREDECESSORS = ":<>^=+-";

	/// <summary>The four quotings, the triple-quoted ones first.</summary>
	private static readonly ImmutableArray<string> _quotes = [_TRIPLE_DOUBLE_QUOTE, _TRIPLE_SINGLE_QUOTE, _DOUBLE_QUOTE, _SINGLE_QUOTE];

	/// <summary>The 14 plain prefixes: raw, bytes and unicode, alone and as the raw-bytes pairs, in both cases. They change the value and not the mask.</summary>
	private static readonly ImmutableArray<string> _plainPrefixes =
	[
		"r", "R", "b", "B", "u", "U", "rb", "rB", "Rb", "RB", "br", "bR", "Br", "BR"
	];

	/// <summary>The 20 interpolating prefixes: the ten format prefixes and the ten template prefixes of 3.14, which read a replacement field as the format prefixes do.</summary>
	private static readonly ImmutableArray<string> _interpolatingPrefixes =
	[
		"f", "F", "rf", "rF", "Rf", "RF", "fr", "fR", "Fr", "FR",
		"t", "T", "rt", "rT", "Rt", "RT", "tr", "tR", "Tr", "TR"
	];

	internal static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = PythonPatternExpressions.ConstantDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Sensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = _LINE_COMMENT_OPENING, OpeningContextPredicate = HashIsNotAFormatSpecMark }
			],
			BlockCommentRules = [],
			QuoteRules = BuildQuoteRules()
		};

		return profile;
	}

	/// <summary>
	/// True when the hash at <paramref name="delimiterPosition"/> may open a comment: it is the first character, or the character before it is not one of <c>: &lt; &gt; ^ = + -</c>. Pure, constant cost per
	/// call and uncharged by the lexical budget.
	/// </summary>
	internal static bool HashIsNotAFormatSpecMark(ReadOnlySpan<char> text, int delimiterPosition)
	{
		bool opensAComment = true;

		if (delimiterPosition > _FIRST_POSITION)
		{
			opensAComment = !_FORMAT_SPEC_PREDECESSORS.Contains(text[delimiterPosition - _ONE_CHARACTER_BEFORE], StringComparison.Ordinal);
		}

		return opensAComment;
	}

	/// <summary>
	/// True when the expanded opening whose first prefix letter stands at <paramref name="delimiterPosition"/> may open: it is the first character, or the character before it is not a letter, a digit or an
	/// underscore, so <c>if"a"</c> is a plain literal after the word <c>if</c> and not an <c>f</c> literal. Pure and constant cost per call.
	/// </summary>
	internal static bool PrefixDoesNotEndAWord(ReadOnlySpan<char> text, int delimiterPosition)
	{
		bool opens = true;

		if (delimiterPosition > _FIRST_POSITION)
		{
			char previous = text[delimiterPosition - _ONE_CHARACTER_BEFORE];

			opens = !(char.IsLetterOrDigit(previous) || previous == _UNDERSCORE);
		}

		return opens;
	}

	/// <summary>The four plain rules, then the eighty expanded ones: each interpolating prefix crossed with each quote.</summary>
	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		ImmutableArray<QuoteRule>.Builder rules = ImmutableArray.CreateBuilder<QuoteRule>();
		ImmutableArray<LiteralPrefixRule> plainPrefixes = [.. _plainPrefixes.Select(prefix => new LiteralPrefixRule { Prefix = prefix, EscapeHandling = EscapeHandling.AsDeclaredByTheQuoteRule })];

		foreach (string quote in _quotes)
		{
			rules.Add(new QuoteRule
			{
				Opening = quote,
				Closing = quote,
				NewlinePolicy = NewlinePolicyOf(quote),
				EscapeCharacter = _ESCAPE_CHARACTER,
				LiteralPrefixes = plainPrefixes
			});
		}

		foreach (string prefix in _interpolatingPrefixes)
		{
			foreach (string quote in _quotes)
			{
				rules.Add(BuildInterpolatingRule(prefix, quote));
			}
		}

		return rules.ToImmutable();
	}

	private static QuoteRule BuildInterpolatingRule(string prefix, string quote)
	{
		return new QuoteRule
		{
			Opening = prefix + quote,
			Closing = quote,
			NewlinePolicy = NewlinePolicyOf(quote),
			EscapeCharacter = _ESCAPE_CHARACTER,
			InterpolationOpening = _REPLACEMENT_FIELD_OPENING,
			InterpolationClosing = _REPLACEMENT_FIELD_CLOSING,
			InterpolationNestingOpening = _REPLACEMENT_FIELD_OPENING,
			DoubledInterpolationOpeningIsLiteral = true,
			OpeningContextPredicate = PrefixDoesNotEndAWord
		};
	}

	/// <summary>A single-quoted literal continues only across an escaped line feed; a triple-quoted one takes a raw newline as content.</summary>
	private static NewlinePolicy NewlinePolicyOf(string quote)
	{
		return quote.Length == _SINGLE_QUOTE_LENGTH ? NewlinePolicy.EscapedContinues : NewlinePolicy.Allowed;
	}
}
