using System.Text;

using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE CONTRACT-PROBE FIXTURES OF THE GO PROFILE. The twenty snippets of the profile design (snippets 1 to 16, 3b, 3c, 9b and 1b) are held
/// inline with generic names, run through the SHIPPED scanner with the test-local profile of <see cref="GoShapedProfile"/>, and each asserts the zone
/// mask and the validity and anomaly. THE EXPECTED MASKS ARE NOT THE SCANNER'S OUTPUT: each was derived from the tokenization of the Go toolchain's own
/// <c>go/scanner</c> (comments on), version go1.27.3: a COMMENT token is comment, the first and last character of a STRING or CHAR token are delimiters and
/// the characters between them are string body, a literal that go/scanner reports as "not terminated" has no closing delimiter, and everything else is
/// code. In the notation below <c>C</c> is code, <c>D</c> a delimiter, <c>M</c> a comment, <c>S</c> a string body and <c>|</c> a line feed, whatever its
/// zone. Each case also pins the number of literals that go/scanner reports as not terminated, which is the number of literals the scanner recovers at a
/// line end (<see cref="ZoneMask.NewlineTerminatedLiteralCount"/>). Go REJECTS a snippet where its record says so; the scanner is asked for the mask
/// of every snippet all the same, because the tool reads text that does not compile. A case that fails is a finding about the profile or the scanner.
/// The dump test writes every mask to the path named by <see cref="DUMP_VARIABLE_NAME"/>. Nothing here reads any file of a repository.
/// </summary>
public sealed class GoProbeFixtureTests
{
	internal const string DUMP_VARIABLE_NAME = "MIGRATE_AUTH_GO_PROBE_DUMP";

	private const char _LINE_FEED_GLYPH = '|';
	private const char _CODE_GLYPH = 'C';
	private const char _COMMENT_GLYPH = 'M';
	private const char _STRING_BODY_GLYPH = 'S';
	private const char _DELIMITER_GLYPH = 'D';
	private const string _CASE_COUNT_MESSAGE = "The design lists twenty snippets.";
	private const int _EXPECTED_CASE_COUNT = 20;

	/// <summary>One snippet: its id, its name, the text, the text as go/scanner saw it (line feeds shown as a bar), the Go-derived mask in the same notation (null where the scan abandons), the offset at which the scan abandons (null otherwise) and the number of literals go/scanner reports as not terminated.</summary>
	private sealed record GoCase(string Id, string Name, string Text, string? GoText, string? GoMask, int? AbandonedAt, int NotTerminated);

	private static readonly GoCase[] _cases =
	[
		new("Case1", "raw-literal-newline-backslash-quotes", "var a = `line one \\n \"quoted\" 'q'\nline two \\`\nvar after = \"ok\"\n", "var a = `line one \\n \"quoted\" 'q'|line two \\`|var after = \"ok\"|", "CCCCCCCCDSSSSSSSSSSSSSSSSSSSSSSSS|SSSSSSSSSSD|CCCCCCCCCCCCDSSD|", null, 0),
		new("Case2", "interpreted-raw-newline", "var a = \"one\ntwo\"\nvar after = 1\n", "var a = \"one|two\"|var after = 1|", "CCCCCCCCDSSS|CCCD|CCCCCCCCCCCCC|", null, 2),
		new("Case3", "rune-literals", "var r1 = 'a'\nvar r2 = '\"'\nvar r3 = '\\n'\nvar after = \"ok\"\n", "var r1 = 'a'|var r2 = '\"'|var r3 = '\\n'|var after = \"ok\"|", "CCCCCCCCCDSD|CCCCCCCCCDSD|CCCCCCCCCDSSD|CCCCCCCCCCCCDSSD|", null, 0),
		new("Case4", "interpreted-escaped-quotes", "var s = \"say \\\"hi\\\" // not a comment\"\nvar after = 1\n", "var s = \"say \\\"hi\\\" // not a comment\"|var after = 1|", "CCCCCCCCDSSSSSSSSSSSSSSSSSSSSSSSSSSSD|CCCCCCCCCCCCC|", null, 0),
		new("Case5", "slashes-inside-literals", "var u = \"http://host.example/p\" // real comment\nvar v = `/* not a comment */ // nor this`\nvar after = 1\n", "var u = \"http://host.example/p\" // real comment|var v = `/* not a comment */ // nor this`|var after = 1|", "CCCCCCCCDSSSSSSSSSSSSSSSSSSSSSDCMMMMMMMMMMMMMMM|CCCCCCCCDSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSD|CCCCCCCCCCCCC|", null, 0),
		new("Case6", "block-comment-not-nested", "/* outer /* inner */ still := 1 */\nvar after = \"ok\"\n", "/* outer /* inner */ still := 1 */|var after = \"ok\"|", "MMMMMMMMMMMMMMMMMMMMCCCCCCCCCCCCCC|CCCCCCCCCCCCDSSD|", null, 0),
		new("Case7", "struct-tags", "type U struct {\n\tName string `json:\"name\" xml:\"n\"`\n\tPass string \"plain-tag\"\n}\n", "type U struct {|\tName string `json:\"name\" xml:\"n\"`|\tPass string \"plain-tag\"|}|", "CCCCCCCCCCCCCCC|CCCCCCCCCCCCCDSSSSSSSSSSSSSSSSSSSD|CCCCCCCCCCCCCDSSSSSSSSSD|C|", null, 0),
		new("Case8", "no-prefixes", "var a = r\"abc\"\nvar after = 1\n", "var a = r\"abc\"|var after = 1|", "CCCCCCCCCDSSSD|CCCCCCCCCCCCC|", null, 0),
		new("Case9", "interpreted-escaped-backslash-closes", "var c = \"a\\\\\"\nvar after = 1\n", "var c = \"a\\\\\"|var after = 1|", "CCCCCCCCDSSSD|CCCCCCCCCCCCC|", null, 0),
		new("Case10", "line-comment-trailing-backslash", "// comment \\\nvar x = 1\n", "// comment \\|var x = 1|", "MMMMMMMMMMMM|CCCCCCCCC|", null, 0),
		new("Case11", "block-comment-multiline-then-code", "/* line one\nline two */ var a = 1\nvar after = \"ok\"\n", "/* line one|line two */ var a = 1|var after = \"ok\"|", "MMMMMMMMMMM|MMMMMMMMMMMCCCCCCCCCC|CCCCCCCCCCCCDSSD|", null, 0),
		new("Case12", "quotes-inside-comments", "// it's a \"test\nvar a = 1 /* it's \" */\nvar after = 2\n", "// it's a \"test|var a = 1 /* it's \" */|var after = 2|", "MMMMMMMMMMMMMMM|CCCCCCCCCCMMMMMMMMMMMM|CCCCCCCCCCCCC|", null, 0),
		new("Case13", "slash-and-star-runes", "var s = '/'\nvar t = '*'\nvar d = 4 / 2 * 3\nvar after = 1\n", "var s = '/'|var t = '*'|var d = 4 / 2 * 3|var after = 1|", "CCCCCCCCDSD|CCCCCCCCDSD|CCCCCCCCCCCCCCCCC|CCCCCCCCCCCCC|", null, 0),
		new("Case14", "block-opener-inside-line-comment", "// /* not opening\nvar a = \"ok\"\n", "// /* not opening|var a = \"ok\"|", "MMMMMMMMMMMMMMMMM|CCCCCCCCDSSD|", null, 0),
		new("Case15", "adjacent-forms", "var m, n = \"a\" + `b`, 'c' /* c */ // d\nvar after = 1\n", "var m, n = \"a\" + `b`, 'c' /* c */ // d|var after = 1|", "CCCCCCCCCCCDSDCCCDSDCCDSDCMMMMMMMCMMMM|CCCCCCCCCCCCC|", null, 0),
		new("Case16", "unterminated-block", "/* never closed\nvar a = 1\n", null, null, 0, 0),
		new("Case3b", "rune-escaped-backslash-and-quote", "var r4 = '\\\\'\nvar r5 = '\\''\nvar r6 = '\\x41'\n", "var r4 = '\\\\'|var r5 = '\\''|var r6 = '\\x41'|", "CCCCCCCCCDSSD|CCCCCCCCCDSSD|CCCCCCCCCDSSSSD|", null, 0),
		new("Case3c", "rune-lone-backslash-before-close", "var r = '\\'\nvar after = 1\n", "var r = '\\'|var after = 1|", "CCCCCCCCDSS|CCCCCCCCCCCCC|", null, 1),
		new("Case9b", "interpreted-escaped-newline", "var c = \"a\\\nb\"\n", "var c = \"a\\|b\"|", "CCCCCCCCDSS|CD|", null, 2),
		new("Case1b", "raw-backslash-quote-run", "var a = `\\\"` + \"x\"\nvar b = `\\`\n", "var a = `\\\"` + \"x\"|var b = `\\`|", "CCCCCCCCDSSDCCCDSD|CCCCCCCCDSD|", null, 0),
	];

	public static IEnumerable<object[]> CaseIds()
	{
		return _cases.Select(goCase => new object[] { goCase.Id });
	}

	[Fact]
	public void TheTwentySnippetsOfTheDesignAreAllHeld()
	{
		Assert.Equal(_EXPECTED_CASE_COUNT, _cases.Length);
		Assert.Equal(_EXPECTED_CASE_COUNT, _cases.Select(goCase => goCase.Id).Distinct(StringComparer.Ordinal).Count());
		Assert.True(_cases.Select(goCase => goCase.Id).SequenceEqual(["Case1", "Case2", "Case3", "Case4", "Case5", "Case6", "Case7", "Case8", "Case9", "Case10", "Case11", "Case12", "Case13", "Case14", "Case15", "Case16", "Case3b", "Case3c", "Case9b", "Case1b"]), _CASE_COUNT_MESSAGE);
	}

	[Theory]
	[MemberData(nameof(CaseIds))]
	public void TheSnippetIsTheTextThatGoScannerTokenized(string id)
	{
		GoCase goCase = Find(id);

		if (goCase.GoText is not null)
		{
			Assert.Equal(goCase.GoText, goCase.Text.Replace('\n', _LINE_FEED_GLYPH));
		}
	}

	[Theory]
	[MemberData(nameof(CaseIds))]
	public void TheMaskEqualsTheMaskDerivedFromGoScanner(string id)
	{
		GoCase goCase = Find(id);
		ZoneMask mask = GoShapedProfile.Scan(goCase.Text);

		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, goCase.Text);

		if (goCase.AbandonedAt is int abandonedAt)
		{
			Assert.False(mask.IsCompletelyValid);
			Assert.Equal(abandonedAt, mask.ValidThroughPosition);
			Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		}
		else
		{
			Assert.Equal(goCase.GoMask, Glyphs(mask, goCase.Text));
			Assert.True(mask.IsCompletelyValid, ZoneAssertions.Render(mask, goCase.Text));
			Assert.Equal(goCase.NotTerminated, mask.NewlineTerminatedLiteralCount);
			Assert.Equal(goCase.NotTerminated == 0 ? LexicalAnomalyReason.None : LexicalAnomalyReason.NewlineInsideALiteral, mask.AnomalyReason);
			Assert.Equal(0, mask.UnbalancedNestingCount);
		}
	}

	[Fact]
	public void ARawLiteralHasNoEscapeSoABackslashBeforeTheClosingBackQuoteIsText()
	{
		string source = "var b = `\\`\nvar after = \"ok\"\n";
		ZoneMask mask = GoShapedProfile.Scan(source);

		Assert.Equal("CCCCCCCCDSD|CCCCCCCCCCCCDSSD|", Glyphs(mask, source));
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
	}

	[Fact]
	public void AnEscapedLineFeedDoesNotContinueAnInterpretedLiteral()
	{
		ZoneMask mask = GoShapedProfile.Scan(Find("Case9b").Text);

		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(2, mask.NewlineTerminatedLiteralCount);
		Assert.Equal(LexicalAnomalyReason.NewlineInsideALiteral, mask.AnomalyReason);
	}

	/// <summary>The inline-language forms, outside the twenty snippets: a cgo preamble is a block comment to go/scanner, SQL text in a raw literal is a literal, and a first line that begins with two slashes is a line comment. The masks are go/scanner's.</summary>
	[Fact]
	public void TheInlineLanguageFormsMaskAsGoScannerTokenizesThem()
	{
		string cgoAndSql = "/*\n#include <stdio.h>\nstatic void hello(void) { puts(\"hi\"); }\n*/\nimport \"C\"\nvar q = `SELECT name FROM users WHERE id = 'x' -- trailing`\n";
		string runnableFirstLine = "//usr/bin/env go run \"$0\" \"$@\"; exit\nvar after = 1\n";

		Assert.Equal(
			"MM|MMMMMMMMMMMMMMMMMM|MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM|MM|CCCCCCCDSD|CCCCCCCCDSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSD|",
			Glyphs(GoShapedProfile.Scan(cgoAndSql), cgoAndSql));
		Assert.Equal(
			"MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM|CCCCCCCCCCCCC|",
			Glyphs(GoShapedProfile.Scan(runnableFirstLine), runnableFirstLine));
	}

	/// <summary>
	/// THE DUMP. When <see cref="DUMP_VARIABLE_NAME"/> names a path, the zone mask of every snippet is written to it: its id, the validity, the anomaly, the
	/// text with line feeds shown as a bar and one glyph per position. Without the variable the test does nothing and passes.
	/// </summary>
	[Fact]
	public void TheZoneMaskOfEverySnippetIsDumpedWhenAnOutputPathIsNamed()
	{
		string? destination = Environment.GetEnvironmentVariable(DUMP_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(destination))
		{
			StringBuilder dump = new();

			foreach (GoCase goCase in _cases)
			{
				ZoneMask mask = GoShapedProfile.Scan(goCase.Text);

				dump.AppendLine($"### {goCase.Id} {goCase.Name}: valid-through {mask.ValidThroughPosition} of {mask.Length}; complete {mask.IsCompletelyValid}; anomaly {mask.AnomalyReason}; newline-terminated {mask.NewlineTerminatedLiteralCount}");
				dump.AppendLine(goCase.Text.Replace('\n', _LINE_FEED_GLYPH));
				dump.AppendLine(Glyphs(mask, goCase.Text));
			}

			File.WriteAllText(destination, dump.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			Assert.True(File.Exists(destination));
		}
	}

	private static GoCase Find(string id)
	{
		return _cases.Single(goCase => goCase.Id == id);
	}

	/// <summary>The mask as one glyph per position, a line feed always shown as a bar, as the design's notation has it.</summary>
	private static string Glyphs(ZoneMask mask, string text)
	{
		StringBuilder glyphs = new(mask.Length);

		for (int position = 0; position < mask.Length; position++)
		{
			glyphs.Append(text[position] == '\n' ? _LINE_FEED_GLYPH : GlyphFor(mask.ZoneAt(position)));
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
