using System.Text;

using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SHIPPED SCANNER, RUN WITH THE GO-SHAPED TEST PROFILE OVER THE HARD LEXICAL FORMS OF GO.
///
/// Every expected mask below was derived INDEPENDENTLY of the scanner under test: each fixture was run through the Go standard library
/// tokenizer (<c>go/scanner</c>, Go 1.27.3, comments retained) and its tokens were mapped to zones - a comment token is <c>c</c> from its first to its
/// last character, a string, raw string or rune token is <c>d</c> on its opening and closing quote and <c>s</c> between them, and everything else
/// (identifiers, operators, line feeds outside a comment or literal) is code, <c>.</c>. The scanner under test was compared to those masks only afterwards.
/// A literal the Go tokenizer reports as "not terminated" has its opening quote and a body up to the line end and NO closing quote. A line feed
/// carries the zone of the construct that contains it, so a raw literal or a block comment spanning lines carries it through.
///
/// Every fixture is followed by a code identifier on a later line, <c>after</c>, so that its zone proves the scan resumed after the form.
/// All text is invented with generic names.
/// </summary>
public sealed class GoScannerFormTests
{
	private const string _FOLLOWING_IDENTIFIER = "after";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const char _CODE_GLYPH = '.';
	private const char _COMMENT_GLYPH = 'c';
	private const char _STRING_BODY_GLYPH = 's';
	private const char _DELIMITER_GLYPH = 'd';
	private const int _ONE_RECOVERED_LITERAL = 1;
	private const int _TWO_RECOVERED_LITERALS = 2;
	private const int _NO_RECOVERED_LITERAL = 0;

	private const string _RAW_MULTILINE_SOURCE = "x := `line one // not a comment\n/* not a comment\nstill raw`\nafter := 1\n";
	private const string _RAW_MULTILINE_MASK = ".....dssssssssssssssssssssssssssssssssssssssssssssssssssssd............";

	private const string _RAW_BACKSLASH_SOURCE = "y := `\\`\nafter := 1\n";
	private const string _RAW_BACKSLASH_MASK = ".....dsd............";

	private const string _ESCAPED_QUOTE_SOURCE = "z := \"\\\"\"\nafter := 1\n";
	private const string _ESCAPED_QUOTE_MASK = ".....dssd............";

	private const string _RUNES_SOURCE = "a := '\"'\nb := '\\''\nc := '\\\\'\nd := '/'\ne := '*'\nafter := 1\n";
	private const string _RUNES_MASK = ".....dsd......dssd......dssd......dsd......dsd............";

	private const string _STRUCT_TAGS_SOURCE = "type T struct {\n\tName string `json:\"name\"`\n\tID   int    \"db:\\\"id\\\"\"\n}\nafter := 1\n";
	private const string _STRUCT_TAGS_MASK = ".............................dsssssssssssd..............dsssssssssd..............";

	private const string _BLOCK_WITH_LINE_COMMENT_SOURCE = "/* a // b */\nafter := 1\n";
	private const string _BLOCK_WITH_LINE_COMMENT_MASK = "cccccccccccc............";

	private const string _LINE_COMMENT_WITH_BLOCK_OPENER_SOURCE = "x := 1 // opens /* nothing\nafter := 2\n";
	private const string _LINE_COMMENT_WITH_BLOCK_OPENER_MASK = ".......ccccccccccccccccccc............";

	private const string _NON_NESTED_BLOCK_SOURCE = "/* /* */ code := 1 */\nafter := 2\n";
	private const string _NON_NESTED_BLOCK_MASK = "cccccccc.........................";

	private const string _SLASHES_IN_INTERPRETED_SOURCE = "u := \"http://h.example/*x*/\"\nafter := 1\n";
	private const string _SLASHES_IN_INTERPRETED_MASK = ".....dsssssssssssssssssssssd............";

	private const string _LINE_COMMENT_TRAILING_BACKSLASH_SOURCE = "// note \\\nafter := 1\n";
	private const string _LINE_COMMENT_TRAILING_BACKSLASH_MASK = "ccccccccc............";

	private const string _ALL_FORMS_SOURCE = "a := `r // /* x\ny`\nb := \"\\\"\" // c /* d\nc := '\"'\nd := '\\''\ne := '\\\\'\nf := '/'\ng := '*'\n/* p // q */\nh := \"// /*\"\n/* /* */ i := 1 */\nj := `\\`\n// t \\\nafter := 1\n";
	private const string _ALL_FORMS_MASK = ".....dsssssssssssd......dssd.ccccccccc......dsd......dssd......dssd......dsd......dsd.cccccccccccc......dsssssd.cccccccc................dsd.cccccc............";

	private const string _INTERPRETED_ACROSS_RAW_NEWLINE_SOURCE = "s := \"abc\nafter := 1\n";
	private const string _INTERPRETED_ACROSS_RAW_NEWLINE_MASK = ".....dsss............";

	private const string _ESCAPED_NEWLINE_SOURCE = "s := \"abc\\\ndef\"\nafter := 1\n";
	private const string _ESCAPED_NEWLINE_MASK = ".....dssss....d............";

	private const string _LONE_RUNE_BACKSLASH_SOURCE = "r := '\\'\nafter := 1\n";
	private const string _LONE_RUNE_BACKSLASH_MASK = ".....dss............";

	private const string _UNTERMINATED_RUNE_SOURCE = "r := 'a\nafter := 1\n";
	private const string _UNTERMINATED_RUNE_MASK = ".....ds............";

	private const string _UNTERMINATED_BLOCK_SOURCE = "x := 1\n/* never closed\nafter := 1\n";
	private const string _UNTERMINATED_BLOCK_VALID_PREFIX_MASK = ".......";

	public static IEnumerable<object[]> ValidForms =>
	[
		["raw literal across lines holding // and /*", _RAW_MULTILINE_SOURCE, _RAW_MULTILINE_MASK],
		["backslash before the closing back quote is text", _RAW_BACKSLASH_SOURCE, _RAW_BACKSLASH_MASK],
		["escaped double quote in an interpreted literal", _ESCAPED_QUOTE_SOURCE, _ESCAPED_QUOTE_MASK],
		["runes holding a quote, an escaped quote, a backslash, a slash and a star", _RUNES_SOURCE, _RUNES_MASK],
		["struct tags, raw and interpreted", _STRUCT_TAGS_SOURCE, _STRUCT_TAGS_MASK],
		["block comment holding //", _BLOCK_WITH_LINE_COMMENT_SOURCE, _BLOCK_WITH_LINE_COMMENT_MASK],
		["line comment holding /* opens nothing", _LINE_COMMENT_WITH_BLOCK_OPENER_SOURCE, _LINE_COMMENT_WITH_BLOCK_OPENER_MASK],
		["block comment does not nest", _NON_NESTED_BLOCK_SOURCE, _NON_NESTED_BLOCK_MASK],
		["// and /* inside an interpreted literal", _SLASHES_IN_INTERPRETED_SOURCE, _SLASHES_IN_INTERPRETED_MASK],
		["line comment ending in a backslash does not continue", _LINE_COMMENT_TRAILING_BACKSLASH_SOURCE, _LINE_COMMENT_TRAILING_BACKSLASH_MASK],
		["every form in one file", _ALL_FORMS_SOURCE, _ALL_FORMS_MASK]
	];

	public static IEnumerable<object[]> RecoveredForms =>
	[
		["interpreted literal across a raw newline", _INTERPRETED_ACROSS_RAW_NEWLINE_SOURCE, _INTERPRETED_ACROSS_RAW_NEWLINE_MASK, _ONE_RECOVERED_LITERAL],
		["escaped newline in an interpreted literal", _ESCAPED_NEWLINE_SOURCE, _ESCAPED_NEWLINE_MASK, _TWO_RECOVERED_LITERALS],
		["lone rune opener with a backslash", _LONE_RUNE_BACKSLASH_SOURCE, _LONE_RUNE_BACKSLASH_MASK, _ONE_RECOVERED_LITERAL],
		["unterminated rune", _UNTERMINATED_RUNE_SOURCE, _UNTERMINATED_RUNE_MASK, _ONE_RECOVERED_LITERAL]
	];

	[Theory]
	[MemberData(nameof(ValidForms))]
	public void EachFormReadsExactlyAsTheGoTokenizerReadsItAndTheScanResumesAfterwards(string form, string source, string expectedMask)
	{
		ZoneMask mask = GoShapedProfile.Scan(source);

		Assert.True(mask.IsCompletelyValid, $"{form}: valid only through {mask.ValidThroughPosition} ({mask.AnomalyReason}).{Environment.NewLine}{ZoneAssertions.Render(mask, source)}");
		Assert.Equal(_NO_RECOVERED_LITERAL, mask.NewlineTerminatedLiteralCount);
		Assert.Equal(expectedMask.Length, source.Length);
		Assert.Equal(expectedMask, Glyphs(mask));
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Theory]
	[MemberData(nameof(RecoveredForms))]
	public void AFormGoRejectsRecoversAtTheLineEndWithoutAbandoningTheFile(string form, string source, string expectedMask, int recoveredLiterals)
	{
		ZoneMask mask = GoShapedProfile.Scan(source);

		Assert.True(mask.IsCompletelyValid, $"{form}: valid only through {mask.ValidThroughPosition} ({mask.AnomalyReason}).{Environment.NewLine}{ZoneAssertions.Render(mask, source)}");
		Assert.False(mask.WasAbandonedMidFile);
		Assert.Equal(LexicalAnomalyReason.NewlineInsideALiteral, mask.AnomalyReason);
		Assert.Equal(recoveredLiterals, mask.NewlineTerminatedLiteralCount);
		Assert.Equal(expectedMask, Glyphs(mask));
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Fact]
	public void AnUnterminatedBlockCommentAbandonsTheFileAtItsOpeningAndTheMaskBeforeItIsExact()
	{
		ZoneMask mask = GoShapedProfile.Scan(_UNTERMINATED_BLOCK_SOURCE);
		int opening = _UNTERMINATED_BLOCK_SOURCE.IndexOf(_BLOCK_COMMENT_OPENING, StringComparison.Ordinal);

		Assert.False(mask.IsCompletelyValid);
		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		Assert.Equal(opening, mask.ValidThroughPosition);
		Assert.Equal(_UNTERMINATED_BLOCK_VALID_PREFIX_MASK, Glyphs(mask)[..mask.ValidThroughPosition]);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, _UNTERMINATED_BLOCK_SOURCE);
	}

	[Fact]
	public void AnEscapedLineFeedDoesNotContinueAnInterpretedLiteralSoTheNextLineIsReadAsCode()
	{
		ZoneMask mask = GoShapedProfile.Scan(_ESCAPED_NEWLINE_SOURCE);

		ZoneAssertions.SpansZone(mask, _ESCAPED_NEWLINE_SOURCE, "def", LexicalZone.Code);
	}

	private static string Glyphs(ZoneMask mask)
	{
		StringBuilder glyphs = new(mask.Length);

		for (int position = 0; position < mask.Length; position++)
		{
			glyphs.Append(GlyphFor(mask.ZoneAt(position)));
		}

		return glyphs.ToString();
	}

	private static char GlyphFor(LexicalZone zone)
	{
		return zone switch
		{
			LexicalZone.Comment => _COMMENT_GLYPH,
			LexicalZone.StringBody => _STRING_BODY_GLYPH,
			LexicalZone.Delimiter => _DELIMITER_GLYPH,
			_ => _CODE_GLYPH
		};
	}
}
