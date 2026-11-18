using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.MatchBoundStress;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// A TEST-LOCAL DRAFT of the `Python` lexical profile of the Python lexical design, built here from the profile vocabulary and not read from any shipped
/// catalog (no shipped `Python` profile exists). It declares: the <c>#</c> line comment guarded by <see cref="HashIsNotAFormatSpecMark"/> (the full set <c>: &lt; &gt; ^ = + -</c>
/// that the design states, not the colon alone); no block comments; 84 quote rules, that is 4 plain rules (<c>"""</c>,
/// <c>'''</c>, <c>"</c>, <c>'</c>) carrying the 14 plain prefixes as <c>LiteralPrefixes</c> with the escape as the rule declares it (a raw prefix still protects its
/// quote), and 80 expanded rules (10 <c>f</c> and 10 <c>t</c> prefixes crossed with the four quotes) whose opening is the prefix plus the quote, with the bracketed
/// <c>{</c> <c>}</c> hole, brace nesting, the doubled-brace literal and <see cref="PrefixDoesNotEndAWord"/>. Triple-quoted rules allow a raw newline; single-quoted
/// rules continue only on an escaped line feed. The format-specification text of a field, a quote used as a fill character, and a backslash before a brace are NOT
/// modelled (declared limits of the design).
///
/// It exists so that the scanner tests and the lexical-budget measurement can run the 84 rules on real-shaped text before any catalog declares the profile.
/// </summary>
public static partial class PythonShapedProfile
{
	public const string PROFILE_NAME = "PythonShapedTestProfile";

	/// <summary>The quote rules of the profile: four plain ones and twenty interpolating prefixes times four quote kinds.</summary>
	public const int EXPECTED_QUOTE_RULE_COUNT = 84;

	private const string _TRIPLE_DOUBLE_QUOTE = "\"\"\"";
	private const string _TRIPLE_SINGLE_QUOTE = "'''";
	private const string _DOUBLE_QUOTE = "\"";
	private const string _SINGLE_QUOTE = "'";
	private const string _HOLE_OPENING = "{";
	private const string _HOLE_CLOSING = "}";
	private const char _ESCAPE_CHARACTER = '\\';
	private const char _UNDERSCORE = '_';
	private const int _FIRST_POSITION = 0;

	/// <summary>The characters that, directly before a <c>#</c>, make it a format-specification character and not a comment: a colon, the alignment characters, the sign characters and the equals sign.</summary>
	private const string _FORMAT_SPEC_PREDECESSORS = ":<>^=+-";

	/// <summary>The 14 plain prefixes: raw and bytes and unicode, alone and as the two-letter raw-bytes pairs, in both cases. They change the value and not the mask.</summary>
	public static readonly ImmutableArray<string> PlainPrefixes =
	[
		"r", "R", "b", "B", "u", "U", "rb", "rB", "Rb", "RB", "br", "bR", "Br", "BR"
	];

	/// <summary>The 20 interpolating prefixes: the 10 format prefixes and the 10 template prefixes (Python 3.14).</summary>
	public static readonly ImmutableArray<string> InterpolatingPrefixes =
	[
		"f", "F", "rf", "rF", "Rf", "RF", "fr", "fR", "Fr", "FR",
		"t", "T", "rt", "rT", "Rt", "RT", "tr", "tR", "Tr", "TR"
	];

	/// <summary>The four quotings, triple-quoted first.</summary>
	public static readonly ImmutableArray<string> Quotes = [_TRIPLE_DOUBLE_QUOTE, _TRIPLE_SINGLE_QUOTE, _DOUBLE_QUOTE, _SINGLE_QUOTE];

	/// <summary>
	/// A statement-start declaration of a string literal or of an environment read: the name, an optional <c>: annotation</c> that runs up to the equals sign (it
	/// cannot cross an equals sign or a line end), the equals sign, then a single-line single- or double-quoted literal (optionally raw or unicode prefixed) or an
	/// <c>os.environ[...]</c>, <c>os.environ.get(...)</c> or <c>os.getenv(...)</c> read. The environment form is marked by the empty group <c>environment</c> and gives the leading
	/// word of its fallback literal (<c>basic</c>, <c>bearer</c> or empty) in the group <c>fallbackWord</c>; a read with no fallback gives an empty one. The statement
	/// must end after it (a line end, a comment, or a semicolon). Anchored at the line start, one line, the match timeout the provisional policy bound.
	/// </summary>
	[GeneratedRegex(
		@"^[ \t]*(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*(?::[^=\n]{1,200}?)?=(?!=)[ \t]*(?:[rRuU]?'(?<value>(?:[^'\\\n]|\\.)*)'|[rRuU]?""(?<value>(?:[^""\\\n]|\\.)*)""|(?<environment>)os\.(?:environ\[[ \t]*(?:'[^'\\\n]{1,200}'|""[^""\\\n]{1,200}"")[ \t]*\](?<fallbackWord>)|(?:environ\.get|getenv)\([ \t]*(?:'[^'\\\n]{1,200}'|""[^""\\\n]{1,200}"")[ \t]*(?:,[ \t]*(?:'(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^'\\\n]|\\.){0,200}'|""(?<fallbackWord>(?i:basic|bearer)(?!\w)|)(?>[^""\\\n]|\\.){0,200}"")[ \t]*|(?<fallbackWord>))\)))(?=[ \t]*(?:[;]|$|#))",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex ConstantDeclaration();

	public static LexicalProfile Build()
	{
		return new LexicalProfile
		{
			ProfileName = PROFILE_NAME,
			ConstantDeclarationExpression = ConstantDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Sensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = PythonTestDraftConstants.LINE_COMMENT_OPENING, OpeningContextPredicate = HashIsNotAFormatSpecMark }
			],
			BlockCommentRules = [],
			QuoteRules = BuildQuoteRules()
		};
	}

	public static ZoneMask Scan(string source)
	{
		return new LexicalScanner(Build()).Scan(source);
	}

	/// <summary>True when the <c>#</c> at the position may open a comment: it is not directly preceded by one of <c>: &lt; &gt; ^ = + -</c>. At position zero it opens.</summary>
	public static bool HashIsNotAFormatSpecMark(ReadOnlySpan<char> text, int delimiterPosition)
	{
		bool opens = true;

		if (delimiterPosition > _FIRST_POSITION)
		{
			opens = !_FORMAT_SPEC_PREDECESSORS.Contains(text[delimiterPosition - 1], StringComparison.Ordinal);
		}

		return opens;
	}

	/// <summary>True when the expanded opening whose first prefix letter stands at the position may open: it is the first character, or the previous character is not a letter, a digit or an underscore (so <c>if"a"</c> is not an <c>f</c> string).</summary>
	public static bool PrefixDoesNotEndAWord(ReadOnlySpan<char> text, int delimiterPosition)
	{
		bool opens = true;

		if (delimiterPosition > _FIRST_POSITION)
		{
			char previous = text[delimiterPosition - 1];

			opens = !(char.IsLetterOrDigit(previous) || previous == _UNDERSCORE);
		}

		return opens;
	}

	private static ImmutableArray<QuoteRule> BuildQuoteRules()
	{
		ImmutableArray<QuoteRule>.Builder rules = ImmutableArray.CreateBuilder<QuoteRule>();
		ImmutableArray<LiteralPrefixRule> plainPrefixes = [.. PlainPrefixes.Select(prefix => new LiteralPrefixRule { Prefix = prefix, EscapeHandling = EscapeHandling.AsDeclaredByTheQuoteRule })];

		foreach (string quote in Quotes)
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

		foreach (string prefix in InterpolatingPrefixes)
		{
			foreach (string quote in Quotes)
			{
				rules.Add(new QuoteRule
				{
					Opening = $"{prefix}{quote}",
					Closing = quote,
					NewlinePolicy = NewlinePolicyOf(quote),
					EscapeCharacter = _ESCAPE_CHARACTER,
					InterpolationOpening = _HOLE_OPENING,
					InterpolationClosing = _HOLE_CLOSING,
					InterpolationNestingOpening = _HOLE_OPENING,
					DoubledInterpolationOpeningIsLiteral = true,
					OpeningContextPredicate = PrefixDoesNotEndAWord
				});
			}
		}

		return rules.ToImmutable();
	}

	private static NewlinePolicy NewlinePolicyOf(string quote)
	{
		return quote.Length == 1 ? NewlinePolicy.EscapedContinues : NewlinePolicy.Allowed;
	}
}
