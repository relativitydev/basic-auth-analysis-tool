using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SHIPPED `Go` LEXICAL PROFILE, run through the shipped scanner over the forms the catalog names: the raw string that holds slashes and a line feed, the back quote that follows a backslash and closes the literal,
/// the rune literals that hold a double quote, an escaped quote, a backslash, a slash and a star, the interpreted string with an escaped quote, a struct tag in either literal kind, the line comment that holds a block
/// opener, the block comment that holds a line comment and does not nest, a directive comment and a comment that ends in a backslash. A literal that `Go` itself rejects (across a line, with an escaped line feed, a lone rune
/// opener) is recovered at the line end, and only an unterminated block comment abandons the file, from its opening.
///
/// Every fixture is followed by a code identifier on a later line, <c>after</c>, so that its zone proves the scan resumed after the form. The profile is also held to the test-local profile that was verified
/// against the language's own tokenizer: the same rules in the same order, and the same masks on every text of this class. All text is invented with generic names.
/// </summary>
public sealed class GoLexicalProfileTests
{
	private const string _FOLLOWING_IDENTIFIER = "after";
	private const string _BLOCK_COMMENT_OPENING = "/*";

	private const string _RAW_MULTILINE = "x := `line one // not a comment\n/* not a comment\nstill raw`\nafter := 1\n";
	private const string _RAW_BACKSLASH = "y := `\\`\nafter := 1\n";
	private const string _ESCAPED_QUOTE = "z := \"\\\"\"\nafter := 1\n";
	private const string _RUNES = "a := '\"'\nb := '\\''\nc := '\\\\'\nd := '/'\ne := '*'\nafter := 1\n";
	private const string _STRUCT_TAGS = "type T struct {\n\tName string `json:\"name\"`\n\tID   int    \"db:\\\"id\\\"\"\n}\nafter := 1\n";
	private const string _BLOCK_WITH_LINE_COMMENT = "/* a // b */\nafter := 1\n";
	private const string _LINE_COMMENT_WITH_BLOCK_OPENER = "x := 1 // opens /* nothing\nafter := 2\n";
	private const string _NON_NESTED_BLOCK = "/* /* */ code := 1 */\nafter := 2\n";
	private const string _SLASHES_IN_INTERPRETED = "u := \"http://h.example/*x*/\"\nafter := 1\n";
	private const string _COMMENT_TRAILING_BACKSLASH = "// note \\\nafter := 1\n";
	private const string _DIRECTIVE_COMMENTS = "//go:build linux\n//go:generate stringer -type=Kind\n// +build linux\npackage sample\nafter := 1\n";
	private const string _CGO_PREAMBLE = "/*\n#include <stdio.h>\nstatic void noop() {}\n*/\nimport \"C\"\nafter := 1\n";
	private const string _SLASH_DIVISION = "d := a / b / c\ne := x /* y */ / z\nafter := 1\n";
	private const string _INTERPRETED_ACROSS_RAW_NEWLINE = "s := \"abc\nafter := 1\n";
	private const string _ESCAPED_NEWLINE = "s := \"abc\\\ndef\"\nafter := 1\n";
	private const string _LONE_RUNE_BACKSLASH = "r := '\\'\nafter := 1\n";
	private const string _UNTERMINATED_RUNE = "r := 'a\nafter := 1\n";
	private const string _UNTERMINATED_BLOCK = "x := 1\n/* never closed\nafter := 1\n";
	private const string _ALL_FORMS = "a := `r // /* x\ny`\nb := \"\\\"\" // c /* d\nc := '\"'\nd := '\\''\ne := '\\\\'\nf := '/'\ng := '*'\n/* p // q */\nh := \"// /*\"\n/* /* */ i := 1 */\nj := `\\`\n// t \\\nafter := 1\n";

	private static readonly LexicalProfile _shipped = GoRegisteredExpressions.Analyzer.LexicalProfile;

	public static IEnumerable<object[]> EveryText =>
	[
		[_RAW_MULTILINE], [_RAW_BACKSLASH], [_ESCAPED_QUOTE], [_RUNES], [_STRUCT_TAGS], [_BLOCK_WITH_LINE_COMMENT], [_LINE_COMMENT_WITH_BLOCK_OPENER], [_NON_NESTED_BLOCK],
		[_SLASHES_IN_INTERPRETED], [_COMMENT_TRAILING_BACKSLASH], [_DIRECTIVE_COMMENTS], [_CGO_PREAMBLE], [_SLASH_DIVISION], [_INTERPRETED_ACROSS_RAW_NEWLINE], [_ESCAPED_NEWLINE],
		[_LONE_RUNE_BACKSLASH], [_UNTERMINATED_RUNE], [_UNTERMINATED_BLOCK], [_ALL_FORMS]
	];

	// ------------------------------------------------------------------------------------------
	// What the profile declares
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheProfileDeclaresTheConstructsOfTheCatalogAndNoOthers()
	{
		Assert.Equal("//", Assert.Single(_shipped.LineCommentRulesInEngineOrder).Opening);
		BlockCommentRule block = Assert.Single(_shipped.BlockCommentRulesInEngineOrder);
		Assert.Equal(_BLOCK_COMMENT_OPENING, block.Opening);
		Assert.Equal("*/", block.Closing);
		Assert.Equal(new[] { "\"", "'", "`" }.Order(StringComparer.Ordinal), _shipped.QuoteRulesInEngineOrder.Select(rule => rule.Opening).Order(StringComparer.Ordinal));
		Assert.Null(_shipped.CodeEscapeCharacter);
		Assert.Null(_shipped.ArithmeticContext);
		Assert.Null(_shipped.ExecutedStrings);
		Assert.Empty(_shipped.CodeSpans);
		Assert.Equal(CaseSensitivity.Sensitive, _shipped.KeywordCaseSensitivity);
		Assert.Equal(CaseSensitivity.Sensitive, _shipped.DelimiterCaseSensitivity);
		Assert.Equal(IdentifierCaseSensitivityMode.Sensitive, _shipped.IdentifierCaseSensitivity);
	}

	[Fact]
	public void NoRuleOfTheProfileCarriesAPredicateASuspendedRangeOrACapturedTerminator()
	{
		Assert.All(_shipped.LineCommentRulesInEngineOrder, rule => Assert.Null(rule.OpeningContextPredicate));
		Assert.All(_shipped.QuoteRulesInEngineOrder, rule =>
		{
			Assert.Null(rule.OpeningContextPredicate);
			Assert.Null(rule.SuspendedSubRange);
			Assert.Null(rule.CapturedTerminator);
		});
	}

	[Fact]
	public void TheInterpretedStringAndTheRuneForbidARawLineFeedAndEscapeWithABackslashAndTheRawStringAllowsOneAndHasNoEscape()
	{
		QuoteRule interpreted = _shipped.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "\"");
		QuoteRule rune = _shipped.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "'");
		QuoteRule raw = _shipped.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "`");

		Assert.All(new[] { interpreted, rune }, rule =>
		{
			Assert.Equal(rule.Opening, rule.Closing);
			Assert.Equal(NewlinePolicy.Forbidden, rule.NewlinePolicy);
			Assert.Equal('\\', rule.EscapeCharacter);
		});
		Assert.Equal("`", raw.Closing);
		Assert.Equal(NewlinePolicy.Allowed, raw.NewlinePolicy);
		Assert.Null(raw.EscapeCharacter);
	}

	[Fact]
	public void TheConstantDeclarationIsTheExpressionTheSupportListNames()
	{
		Assert.Same(_shipped.ConstantDeclarationExpression, GoRegisteredExpressions.Support.Single(pair => pair.Identifier == GoRegisteredExpressions.CONSTANT_DECLARATION_IDENTIFIER).Expression);
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, _shipped.ConstantDeclarationExpression.MatchTimeout);
	}

	// ------------------------------------------------------------------------------------------
	// The hard forms, one test each
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void ARawStringHoldingSlashesAndALineFeedIsOneLiteralAndTheScanResumesAfterIt()
	{
		ZoneMask mask = Scan(_RAW_MULTILINE);

		AssertValid(mask, _RAW_MULTILINE);
		ZoneAssertions.SpansZone(mask, _RAW_MULTILINE, "line one // not a comment\n/* not a comment\nstill raw", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _RAW_MULTILINE, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, _RAW_MULTILINE, LexicalZone.Comment);
	}

	[Fact]
	public void ABackslashBeforeTheClosingBackQuoteIsTextAndTheRawLiteralEndsThere()
	{
		ZoneMask mask = Scan(_RAW_BACKSLASH);

		AssertValid(mask, _RAW_BACKSLASH);
		ZoneAssertions.SpansZone(mask, _RAW_BACKSLASH, "\\", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _RAW_BACKSLASH, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Fact]
	public void AnEscapedDoubleQuoteDoesNotCloseAnInterpretedString()
	{
		ZoneMask mask = Scan(_ESCAPED_QUOTE);

		AssertValid(mask, _ESCAPED_QUOTE);
		ZoneAssertions.SpansZone(mask, _ESCAPED_QUOTE, "\\\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _ESCAPED_QUOTE, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Fact]
	public void RunesHoldingADoubleQuoteAnEscapedQuoteABackslashASlashAndAStarAreLiteralsAndNeverAStringOrAComment()
	{
		ZoneMask mask = Scan(_RUNES);

		AssertValid(mask, _RUNES);
		ZoneAssertions.SpansZone(mask, _RUNES, "\"", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _RUNES, "\\'", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _RUNES, "\\\\", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _RUNES, "/", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _RUNES, "*", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.NoPositionCarries(mask, _RUNES, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _RUNES, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Fact]
	public void AStructTagInEitherLiteralKindIsMaskedAsTheLiteralItIs()
	{
		ZoneMask mask = Scan(_STRUCT_TAGS);

		AssertValid(mask, _STRUCT_TAGS);
		ZoneAssertions.SpansZone(mask, _STRUCT_TAGS, "json:\"name\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _STRUCT_TAGS, "db:\\\"id\\\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _STRUCT_TAGS, "Name string", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _STRUCT_TAGS, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Fact]
	public void ABlockCommentHoldingALineCommentIsOneCommentAndALineCommentHoldingABlockOpenerOpensNothing()
	{
		ZoneMask block = Scan(_BLOCK_WITH_LINE_COMMENT);
		ZoneMask line = Scan(_LINE_COMMENT_WITH_BLOCK_OPENER);

		AssertValid(block, _BLOCK_WITH_LINE_COMMENT);
		AssertValid(line, _LINE_COMMENT_WITH_BLOCK_OPENER);
		ZoneAssertions.SpansZone(block, _BLOCK_WITH_LINE_COMMENT, "/* a // b */", LexicalZone.Comment);
		ZoneAssertions.SpansZone(line, _LINE_COMMENT_WITH_BLOCK_OPENER, "// opens /* nothing", LexicalZone.Comment);
		ZoneAssertions.SpansZone(line, _LINE_COMMENT_WITH_BLOCK_OPENER, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Fact]
	public void ABlockCommentDoesNotNestSoTheFirstClosingEndsItAndTheRestOfTheLineIsCode()
	{
		ZoneMask mask = Scan(_NON_NESTED_BLOCK);

		AssertValid(mask, _NON_NESTED_BLOCK);
		ZoneAssertions.SpansZone(mask, _NON_NESTED_BLOCK, "/* /* */", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _NON_NESTED_BLOCK, " code := 1 */", LexicalZone.Code);
	}

	[Fact]
	public void SlashesAndAStarInsideAnInterpretedStringAreBodyAndNotAComment()
	{
		ZoneMask mask = Scan(_SLASHES_IN_INTERPRETED);

		AssertValid(mask, _SLASHES_IN_INTERPRETED);
		ZoneAssertions.SpansZone(mask, _SLASHES_IN_INTERPRETED, "http://h.example/*x*/", LexicalZone.StringBody);
		ZoneAssertions.NoPositionCarries(mask, _SLASHES_IN_INTERPRETED, LexicalZone.Comment);
	}

	[Fact]
	public void ALineCommentThatEndsInABackslashDoesNotContinueOntoTheNextLine()
	{
		ZoneMask mask = Scan(_COMMENT_TRAILING_BACKSLASH);

		AssertValid(mask, _COMMENT_TRAILING_BACKSLASH);
		ZoneAssertions.SpansZone(mask, _COMMENT_TRAILING_BACKSLASH, "// note \\", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _COMMENT_TRAILING_BACKSLASH, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Fact]
	public void ADirectiveCommentAndACgoPreambleAreCommentsAndNoMoreAndALoneSlashIsADivisionInCode()
	{
		ZoneMask directives = Scan(_DIRECTIVE_COMMENTS);
		ZoneMask preamble = Scan(_CGO_PREAMBLE);
		ZoneMask division = Scan(_SLASH_DIVISION);

		AssertValid(directives, _DIRECTIVE_COMMENTS);
		ZoneAssertions.SpansZone(directives, _DIRECTIVE_COMMENTS, "//go:build linux", LexicalZone.Comment);
		ZoneAssertions.SpansZone(directives, _DIRECTIVE_COMMENTS, "//go:generate stringer -type=Kind", LexicalZone.Comment);
		ZoneAssertions.SpansZone(directives, _DIRECTIVE_COMMENTS, "package sample", LexicalZone.Code);
		AssertValid(preamble, _CGO_PREAMBLE);
		ZoneAssertions.SpansZone(preamble, _CGO_PREAMBLE, "/*\n#include <stdio.h>\nstatic void noop() {}\n*/", LexicalZone.Comment);
		AssertValid(division, _SLASH_DIVISION);
		ZoneAssertions.SpansZone(division, _SLASH_DIVISION, "d := a / b / c", LexicalZone.Code);
		ZoneAssertions.SpansZone(division, _SLASH_DIVISION, "/* y */", LexicalZone.Comment);
	}

	// ------------------------------------------------------------------------------------------
	// What `Go` itself rejects is recovered at the line end, and only a block comment that never closes abandons the file
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData(_INTERPRETED_ACROSS_RAW_NEWLINE, 1)]
	[InlineData(_ESCAPED_NEWLINE, 2)]
	[InlineData(_LONE_RUNE_BACKSLASH, 1)]
	[InlineData(_UNTERMINATED_RUNE, 1)]
	public void AFormGoRejectsRecoversAtTheLineEndWithoutAbandoningTheFile(string source, int recoveredLiterals)
	{
		ZoneMask mask = Scan(source);

		Assert.True(mask.IsCompletelyValid, $"valid only through {mask.ValidThroughPosition} ({mask.AnomalyReason}).");
		Assert.False(mask.WasAbandonedMidFile);
		Assert.Equal(LexicalAnomalyReason.NewlineInsideALiteral, mask.AnomalyReason);
		Assert.Equal(recoveredLiterals, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
	}

	[Fact]
	public void AnEscapedLineFeedDoesNotContinueAnInterpretedLiteralSoTheNextLineIsReadAsCode()
	{
		ZoneMask mask = Scan(_ESCAPED_NEWLINE);

		ZoneAssertions.SpansZone(mask, _ESCAPED_NEWLINE, "def", LexicalZone.Code);
	}

	[Fact]
	public void AnUnterminatedBlockCommentAbandonsTheFileAtItsOpening()
	{
		ZoneMask mask = Scan(_UNTERMINATED_BLOCK);
		int opening = _UNTERMINATED_BLOCK.IndexOf(_BLOCK_COMMENT_OPENING, StringComparison.Ordinal);

		Assert.False(mask.IsCompletelyValid);
		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		Assert.Equal(opening, mask.ValidThroughPosition);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, _UNTERMINATED_BLOCK);
	}

	[Fact]
	public void EveryFormInOneFileReadsAsEachFormDoesAlone()
	{
		ZoneMask mask = Scan(_ALL_FORMS);

		AssertValid(mask, _ALL_FORMS);
		ZoneAssertions.SpansZone(mask, _ALL_FORMS, "/* p // q */", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _ALL_FORMS, "// /*", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _ALL_FORMS, "/* /* */", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _ALL_FORMS, _FOLLOWING_IDENTIFIER, LexicalZone.Code);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, _ALL_FORMS);
	}

	// ------------------------------------------------------------------------------------------
	// The shipped profile is the one that was verified against the language's tokenizer
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheShippedRulesEqualTheVerifiedProfilesRulesInOrderFieldByField()
	{
		LexicalProfile reference = GoShapedProfile.Build();

		Assert.Equal(reference.LineCommentRulesInEngineOrder.Select(rule => rule.Opening), _shipped.LineCommentRulesInEngineOrder.Select(rule => rule.Opening));
		Assert.Equal(
			reference.BlockCommentRulesInEngineOrder.Select(rule => (rule.Opening, rule.Closing)),
			_shipped.BlockCommentRulesInEngineOrder.Select(rule => (rule.Opening, rule.Closing)));
		Assert.Equal(
			reference.QuoteRulesInEngineOrder.Select(rule => (rule.Opening, rule.Closing, rule.NewlinePolicy, rule.EscapeCharacter)),
			_shipped.QuoteRulesInEngineOrder.Select(rule => (rule.Opening, rule.Closing, rule.NewlinePolicy, rule.EscapeCharacter)));
		Assert.Equal(reference.KeywordCaseSensitivity, _shipped.KeywordCaseSensitivity);
		Assert.Equal(reference.DelimiterCaseSensitivity, _shipped.DelimiterCaseSensitivity);
		Assert.Equal(reference.IdentifierCaseSensitivity, _shipped.IdentifierCaseSensitivity);
	}

	[Theory]
	[MemberData(nameof(EveryText))]
	public void TheShippedProfileAndTheVerifiedProfileGiveTheSameMaskOnEveryText(string source)
	{
		ZoneMask shipped = Scan(source);
		ZoneMask reference = GoShapedProfile.Scan(source);

		Assert.Equal(reference.IsCompletelyValid, shipped.IsCompletelyValid);
		Assert.Equal(reference.AnomalyReason, shipped.AnomalyReason);
		Assert.Equal(reference.ValidThroughPosition, shipped.ValidThroughPosition);
		Assert.Equal(reference.NewlineTerminatedLiteralCount, shipped.NewlineTerminatedLiteralCount);
		Assert.Equal(Glyphs(reference), Glyphs(shipped));
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static ZoneMask Scan(string source)
	{
		return new LexicalScanner(_shipped).Scan(source);
	}

	private static void AssertValid(ZoneMask mask, string source)
	{
		Assert.True(mask.IsCompletelyValid, $"valid only through {mask.ValidThroughPosition} ({mask.AnomalyReason}).{Environment.NewLine}{ZoneAssertions.Render(mask, source)}");
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	private static string Glyphs(ZoneMask mask)
	{
		StringBuilder glyphs = new(mask.Length);

		for (int position = 0; position < mask.Length; position++)
		{
			glyphs.Append(mask.ZoneAt(position) switch
			{
				LexicalZone.Comment => 'c',
				LexicalZone.StringBody => 's',
				LexicalZone.Delimiter => 'd',
				_ => '.'
			});
		}

		return glyphs.ToString();
	}
}
