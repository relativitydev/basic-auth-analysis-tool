using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE HARD FORMS OF THE `Python` LEXICAL PROFILE, RUN THROUGH THE PROFILE THE BINARY SHIPS. The fixtures are the numbered list of the contract probe and the
/// hard-form cases written beside it: a raw literal that ends in an escaped quote, a continued and a triple-quoted literal, a same-quote field to depth three, a comment and a nested literal inside a field, a
/// prefix run that ends a longer word, every plain, format and template prefix, and the format-specification forms the hash predicate repairs, misses or abandons. Each asserts a zone mask expressed as "this
/// substring lies entirely in this zone", and the numbered list asserts the whole mask, the valid-through position, the anomaly reason and the two recovered counters, position by position. Every fixture is
/// synthetic and uses generic names. The tests whose name starts with <c>DeclaredCost</c> do not assert what Python does: they pin the MEASURED DAMAGE of a form the vocabulary cannot model, so that a later change
/// to the profile or to the scanner is a decision and not an accident.
///
/// The cases are those of <see cref="PythonProbeFixtureTests"/>, which runs them through the test-local draft of the profile; here they run through <see cref="PythonShippedAnalyzer"/>, so that the shipped profile
/// is the probed one. <see cref="PythonLexicalProfileEqualityTests"/> holds the two profiles equal over the whole set.
/// </summary>
public sealed class PythonLexicalProfileHardFormTests
{
	private const string _TRAILER = "y = 'a'\n";
	private const string _TRAILER_TEXT = "y = 'a'";
	private const int _FIRST_LITERAL_START = 4;
	private const int _EXPECTED_ONE = 1;
	private const int _EXPECTED_TWO = 2;
	private const int _ABANDONED_AT_THE_FIRST_LITERAL = 4;
	private const string _TIGHT_ELSE_APOSTROPHE = "else:#it's\nz = 1\n";
	private const string _TIGHT_ELSE_TRIPLE = "else:#'''\nz = 1\n";

	private const string _RAW_ESCAPED_QUOTE = "x = r\"\\\"\"\n" + _TRAILER;
	private const string _RAW_UNTERMINATED = "x = r\"\\\"\n" + _TRAILER;
	private const string _RAW_ESCAPED_BACKSLASH = "x = r\"\\\\\"\n" + _TRAILER;
	private const string _PLAIN_CONTINUATION = "x = \"ab\\\ncd\"\n" + _TRAILER;
	private const string _RAW_CONTINUATION = "x = r\"ab\\\ncd\"\n" + _TRAILER;
	private const string _BARE_NEWLINE_IN_SINGLE_QUOTES = "x = \"ab\ncd\"\n" + _TRAILER;
	private const string _COMMENT_WITH_TRAILING_BACKSLASH = "# comment \\\nx = 1\n";
	private const string _TRIPLE_WITH_CONTENT = "x = \"\"\"line # one\n 'q' \"two\" \"\"three\n\"\"\"\n" + _TRAILER;
	private const string _TRIPLE_QUOTE_RUN = "x = \"\"\"\"a\"\"\"\n" + _TRAILER;
	private const string _TRIPLE_SINGLE_QUOTES = "x = '''it's \"q\" # not'''\n" + _TRAILER;
	private const string _TRIPLE_ESCAPED_QUOTE = "x = \"\"\"a\\\"\"\"b\"\"\"\n" + _TRAILER;
	private const string _DOCSTRING = "def f():\n\t\"\"\"Doc # text.\n\tMore 'text'.\n\t\"\"\"\n\treturn 1\n";

	private const string _FSTRING_SUBSCRIPT = "x = f\"{d[\"k\"]}\"\n" + _TRAILER;
	private const string _FSTRING_DEPTH_TWO = "x = f\"{f\"{y}\"}\"\n" + _TRAILER;
	private const string _FSTRING_DEPTH_THREE = "x = f\"{f\"{f\"{y}\"}\"}\"\n" + _TRAILER;
	private const string _FSTRING_TRIPLE_BOTH_QUOTES = "x = f\"\"\"{d[\"k\"] + d['j']}\"\"\"\n" + _TRAILER;
	private const string _FSTRING_MULTILINE_COMMENT = "x = f\"{n # note\n}\"\n" + _TRAILER;
	private const string _FSTRING_HASH_IN_NESTED_LITERAL = "x = f\"{'#'}\"\n" + _TRAILER;
	private const string _FSTRING_DOUBLED_BRACES = "x = f\"{{{n}}}\"\n" + _TRAILER;
	private const string _FSTRING_CONVERSION = "x = f\"{n!r}\"\n" + _TRAILER;
	private const string _FSTRING_NESTED_SPEC = "x = f\"{n!r:>{w}}\"\n" + _TRAILER;
	private const string _FSTRING_NESTED_SPEC_TWO = "x = f\"{n:{w}.{p}f}\"\n" + _TRAILER;
	private const string _FSTRING_HASH_FLAG = "x = f\"{n:#x}\"\n" + _TRAILER;
	private const string _FSTRING_HASH_AFTER_ALIGN = "x = f\"{n:>#10x}\"\n" + _TRAILER;
	private const string _FSTRING_HASH_AFTER_FILL_AND_ALIGN = "x = f\"{n:*^#x}\"\n" + _TRAILER;
	private const string _FSTRING_HASH_AFTER_SIGN = "x = f\"{n:+#b}\"\n" + _TRAILER;
	private const string _FSTRING_BACKSLASH_BEFORE_BRACE = "x = f\"\\{n}\"\n" + _TRAILER;
	private const string _RAW_FSTRING_BACKSLASH_BEFORE_BRACE = "x = rf\"\\{n}\"\n" + _TRAILER;
	private const string _FSTRING_NAMED_ESCAPE = "x = f\"\\N{EM DASH}{n}\"\n" + _TRAILER;
	private const string _KEYWORD_ADJACENT_QUOTES = "y = 1 if\"a\" else 2\nz = 1 if x else\"b\"\nw = x or\"b\"\n";
	private const string _KEYWORD_ADJACENT_IF_THEN_BRACES = "y = 1 if\"{n}\" else 2\n" + _TRAILER;
	private const string _FSTRING_AFTER_A_BLANK = "y = a if f\"{n}\" else b\n" + _TRAILER;

	private const string _COST_SPACE_SIGN = "x = f\"{255: #x}\"\n" + _TRAILER;
	private const string _COST_Z_OPTION = "x = f\"{3.14159:z#.2f}\"\n" + _TRAILER;
	private const string _COST_QUOTE_FILL = "x = f\"{n:'>10}\"\n" + _TRAILER;
	private const string _COST_QUOTE_FILL_SINGLE_OUTER = "x = f'{n:\"^10}'\n" + _TRAILER;
	private const string _COST_TIGHT_COMMENT_AFTER_COLON = "if y:# tight comment\n\tpass\n";
	private const string _SPACED_COMMENT_AFTER_COLON = "if x:  # spaced comment\n\tpass\n";
	private const string _COST_TIGHT_COMMENT_AFTER_OPERATOR = "z = x+#note\n";

	private const char _CODE_GLYPH = '.';
	private const char _COMMENT_GLYPH = 'c';
	private const char _STRING_BODY_GLYPH = 's';
	private const char _DELIMITER_GLYPH = 'd';

	/// <summary>The numbered fixture list of the probe (an id that begins with Spec stands for the list's id that begins with the letter S): id, text, the mask of the wide-set profile (one glyph per position), valid-through, anomaly reason, newline-terminated count, unbalanced count.</summary>
	private static readonly ProbeFixtureRow[] _fixtureList =
	[
		new("A1", "x = r\"\\\"\"\ny = 'a'\n", "....ddssd.....dsd.", new(18, LexicalAnomalyReason.None, 0, 0)),
		new("A2", "x = r\"\\\"\ny = 'a'\n", "....ddss.....dsd.", new(17, LexicalAnomalyReason.NewlineInsideALiteral, 1, 0)),
		new("A3", "x = r\"\\\\\"\ny = 'a'\n", "....ddssd.....dsd.", new(18, LexicalAnomalyReason.None, 0, 0)),
		new("B1", "s = \"ab\\\ncd\"\ny = 'a'\n", "....dssssssd.....dsd.", new(21, LexicalAnomalyReason.None, 0, 0)),
		new("B2", "s = \"ab\ncd\"\ny = 'a'\n", "....dss...d.....dsd.", new(20, LexicalAnomalyReason.NewlineInsideALiteral, 2, 0)),
		new("B3", "s = r\"ab\\\ncd\"\ny = 'a'\n", "....ddssssssd.....dsd.", new(22, LexicalAnomalyReason.None, 0, 0)),
		new("C1", "s = \"\"\"ab\ncd\"\"\"\ny = 'a'\n", "....dddsssssddd.....dsd.", new(24, LexicalAnomalyReason.None, 0, 0)),
		new("C2", "x = \"\"\"\"a\"\"\"\ny = 'a'\n", "....dddssddd.....dsd.", new(21, LexicalAnomalyReason.None, 0, 0)),
		new("C3", "x = \"\"\"a\\\"\"\"\"\ny = 'a'\n", "....dddsssddd.....dsd.", new(22, LexicalAnomalyReason.None, 0, 0)),
		new("C4", "x = ''''a'''\ny = 'a'\n", "....dddssddd.....dsd.", new(21, LexicalAnomalyReason.None, 0, 0)),
		new("C5", "x = \"\"\"a\"b\"\"c\"\"\"\ny = 'a'\n", "....dddssssssddd.....dsd.", new(25, LexicalAnomalyReason.None, 0, 0)),
		new("C6", "x = \"\"\"\"\"\"\ny = 'a'\n", "....dddddd.....dsd.", new(19, LexicalAnomalyReason.None, 0, 0)),
		new("D1", "x = f\"{d[\"k\"]}\"\ny = 'a'\n", "....ddd..dsd.dd.....dsd.", new(24, LexicalAnomalyReason.None, 0, 0)),
		new("D2", "x = f\"{f\"{y}\"}\"\ny = 'a'\n", "....dddddd.dddd.....dsd.", new(24, LexicalAnomalyReason.None, 0, 0)),
		new("D5", "x = f\"{n!r:>{w}}\"\ny = 'a'\n", "....ddd........dd.....dsd.", new(26, LexicalAnomalyReason.None, 0, 0)),
		new("D6", "x = f\"{n # note\n}\"\ny = 'a'\n", "....ddd..cccccc.dd.....dsd.", new(27, LexicalAnomalyReason.None, 0, 0)),
		new("D7", "x = f\"{'#'}\"\ny = 'a'\n", "....ddddsddd.....dsd.", new(21, LexicalAnomalyReason.None, 0, 0)),
		new("D8", "x = f\"{{{n}}}\"\ny = 'a'\n", "....ddssd.dssd.....dsd.", new(23, LexicalAnomalyReason.None, 0, 0)),
		new("D9", "x = f\"\\{n}\"\ny = 'a'\n", "....ddssssd.....dsd.", new(20, LexicalAnomalyReason.None, 0, 0)),
		new("D10", "x = f\"\\N{EM DASH}{n}\"\ny = 'a'\n", "....ddssd.......dd.dd.....dsd.", new(30, LexicalAnomalyReason.None, 0, 0)),
		new("D11", "x = rf\"\\{n}\"\ny = 'a'\n", "....dddssssd.....dsd.", new(21, LexicalAnomalyReason.None, 0, 0)),
		new("D12", "x = f\"{n:{w}.{p}f}\"\ny = 'a'\n", "....ddd..........dd.....dsd.", new(28, LexicalAnomalyReason.None, 0, 0)),
		new("D13", "x = f\"{(lambda a: a)(1)}\"\ny = 'a'\n", "....ddd................dd.....dsd.", new(34, LexicalAnomalyReason.None, 0, 0)),
		new("D14", "x = f\"{'\\n'.join(a)}\"\ny = 'a'\n", "....ddddssd........dd.....dsd.", new(30, LexicalAnomalyReason.None, 0, 0)),
		new("D15", "x = f'{n:\"^10}'\ny = 'a'\n", "....ddssssssssssssssssss", new(4, LexicalAnomalyReason.UnterminatedLiteral, 1, 0)),
		new("D16", "x = f\"\"\"{d[\"k\"]}{d['k']}\"\"\"\ny = 'a'\n", "....ddddd..dsd.dd..dsd.dddd.....dsd.", new(36, LexicalAnomalyReason.None, 0, 0)),
		new("D17", "x = f\"\"\"{n  # note\n}\"\"\"\ny = 'a'\n", "....ddddd...cccccc.dddd.....dsd.", new(32, LexicalAnomalyReason.None, 0, 0)),
		new("D19", "x = f\"{when:%H:%M}\"\ny = f\"{v=}\"\nz = f\"{v!r}\"\n", "....ddd..........dd.....ddd..dd.....ddd...dd.", new(45, LexicalAnomalyReason.None, 0, 0)),
		new("D20", "x = f\"{f\"{f\"{y}\"}\"}\"\ny = 'a'\n", "....ddddddddd.dddddd.....dsd.", new(29, LexicalAnomalyReason.None, 0, 0)),
		new("E1", "y = 1 if\"a\" else 2\nz = 1 if x else\"b\"\nw = x or\"b\"\n", "........dsd.......................dsd........ddsd.", new(50, LexicalAnomalyReason.None, 0, 0)),
		new("E2", "x = \"a # b\"\ny = 'a'\n", "....dsssssd.....dsd.", new(20, LexicalAnomalyReason.None, 0, 0)),
		new("E3", "# comment \\\nx = 'a'\n", "ccccccccccc.....dsd.", new(20, LexicalAnomalyReason.None, 0, 0)),
		new("E4", "def f():\n    \"\"\"Doc with 'quote' and # hash.\"\"\"\n    return 1\n", ".............dddssssssssssssssssssssssssssssddd..............", new(61, LexicalAnomalyReason.None, 0, 0)),
		new("E5", "# it's a \"comment\" with 'quotes'\nx = 'a'\n", "cccccccccccccccccccccccccccccccc.....dsd.", new(41, LexicalAnomalyReason.None, 0, 0)),
		new("E6", "x = b\"\\xff\" + u\"e\" + Rb'\\d' + BR\"\\d\"\ny = 'a'\n", "....ddssssd...ddsd...dddssd...dddssd.....dsd.", new(45, LexicalAnomalyReason.None, 0, 0)),
		new("E7", "x = f\"{ {'a':'b'}['a'] }\"\ny = 'a'\n", "....ddd..dsd.dsd..dsd..dd.....dsd.", new(34, LexicalAnomalyReason.None, 0, 0)),
		new("E8", "d = {\"a\":\"b\"}\ny = 'a'\n", ".....dsd.dsd......dsd.", new(22, LexicalAnomalyReason.None, 0, 0)),
		new("Spec1", "x = f\"{n:#x}\"\ny = 'a'\n", "....ddd....dd.....dsd.", new(22, LexicalAnomalyReason.None, 0, 0)),
		new("Spec2", "x = f\"{n:>#10x}\"\ny = 'a'\n", "....ddd.......dd.....dsd.", new(25, LexicalAnomalyReason.None, 0, 0)),
		new("Spec3", "x = f\"{n:*^#x}\"\ny = 'a'\n", "....ddd......dd.....dsd.", new(24, LexicalAnomalyReason.None, 0, 0)),
		new("Spec4", "x = f\"{n:+#b}\"\ny = 'a'\n", "....ddd.....dd.....dsd.", new(23, LexicalAnomalyReason.None, 0, 0)),
		new("Spec5", "x = f\"{n:'>10}\"\ny = 'a'\n", "....ddssssssssssssssssss", new(4, LexicalAnomalyReason.UnterminatedLiteral, 1, 0)),
		new("Spec6", "if y:# tight comment\n    z = 1\nw = 'a'\n", "...................................dsd.", new(39, LexicalAnomalyReason.None, 0, 0)),
		new("Spec6b", "if x:  # c\n    z = 1\nw = 'a'\n", ".......ccc...............dsd.", new(29, LexicalAnomalyReason.None, 0, 0)),
		new("Spec7", "z = (x+#note\n    1)\nw = 'a'\n", "........................dsd.", new(28, LexicalAnomalyReason.None, 0, 0)),
		new("Spec7b", "d = {\"b\":# note\n    1}\nw = 'a'\n", ".....dsd...................dsd.", new(31, LexicalAnomalyReason.None, 0, 0)),
		new("Spec8", "x = f\"{n:'>10}\"\ny = {1: 2}\nw = 'a'\n", "....ddd..dsssss..........d.....dsd.", new(35, LexicalAnomalyReason.UnbalancedInterpolationNesting, 2, 1)),
		new("Spec9", "x = f\"{255: #x}\"\ny = 'a'\n", "....ddsssssssssssssssssss", new(4, LexicalAnomalyReason.UnterminatedLiteral, 0, 0)),
		new("Spec10", "x = f\"{3.14159:z#.2f}\"\ny = 'a'\n", "....ddsssssssssssssssssssssssss", new(4, LexicalAnomalyReason.UnterminatedLiteral, 0, 0)),
		new("Spec11", "if x:\n    pass\nelse:#it's\n    pass\nw = 'a'\n", ".......................ds..............dsd.", new(43, LexicalAnomalyReason.NewlineInsideALiteral, 1, 0)),
		new("Spec12", "if x:\n    pass\nelse:#'''\n    pass\nw = 'a'\n", ".....................dddssssssssssssssssss", new(21, LexicalAnomalyReason.UnterminatedLiteral, 0, 0)),
		new("Spec13", "x = f\"{(lambda a: a)(1):#x}\"\ny = 'a'\n", "....ddd...................dd.....dsd.", new(37, LexicalAnomalyReason.None, 0, 0)),
		new("Spec14", "x = f\"{\n    n:#x}\"\ny = 'a'\n", "....ddd.........dd.....dsd.", new(27, LexicalAnomalyReason.None, 0, 0)),
		new("T1", "x = t\"{n}\"\ny = 'a'\n", "....ddd.dd.....dsd.", new(19, LexicalAnomalyReason.None, 0, 0)),
		new("T2", "x = t\"{d[\"k\"]}\"\ny = 'a'\n", "....ddd..dsd.dd.....dsd.", new(24, LexicalAnomalyReason.None, 0, 0)),
		new("T3", "x = t\"{n:#x}\"\ny = 'a'\n", "....ddd....dd.....dsd.", new(22, LexicalAnomalyReason.None, 0, 0)),
		new("T4", "x = rt\"\\{n}\"\ny = 'a'\n", "....dddssssd.....dsd.", new(21, LexicalAnomalyReason.None, 0, 0)),
		new("T5", "x = t\"a{f\"{y}\"}\"\ny = 'a'\n", "....ddsdddd.dddd.....dsd.", new(25, LexicalAnomalyReason.None, 0, 0)),
		new("T6", "x = t\"\"\"{d[\"k\"]}\"\"\"\ny = 'a'\n", "....ddddd..dsd.dddd.....dsd.", new(28, LexicalAnomalyReason.None, 0, 0)),
		new("I1", "q = \"select name from users where a = 'x' and b = '#y'\"\ny = 'a'\n", "....dsssssssssssssssssssssssssssssssssssssssssssssssssd.....dsd.", new(64, LexicalAnomalyReason.None, 0, 0)),
		new("I2", "t = \"\"\"<p>{{ user.name }}</p>\n{% if ok %}it's text{% endif %}\"\"\"\ny = 'a'\n", "....dddssssssssssssssssssssssssssssssssssssssssssssssssssssssddd.....dsd.", new(73, LexicalAnomalyReason.None, 0, 0)),
		new("I3", "p = re.compile(r\"['\\\"]#(?P<n>\\w+)\")\ny = 'a'\n", "...............ddssssssssssssssssd......dsd.", new(44, LexicalAnomalyReason.None, 0, 0)),
		new("I4", "x = ur\"abc\"\ny = 'a'\n", ".....ddsssd.....dsd.", new(20, LexicalAnomalyReason.None, 0, 0)),
		new("I5", "print \"x\"\ny = `z`\nw = 'a'\n", "......dsd.............dsd.", new(26, LexicalAnomalyReason.None, 0, 0)),
		new("I6", "x = 1  # it's here\ny = 'a'\n", ".......ccccccccccc.....dsd.", new(27, LexicalAnomalyReason.None, 0, 0)),
	];

	private sealed record ProbeFixtureOutcome(int ValidThrough, LexicalAnomalyReason AnomalyReason, int NewlineTerminated, int Unbalanced);

	private sealed record ProbeFixtureRow(string FixtureId, string Text, string ExpectedMask, ProbeFixtureOutcome Outcome);

	private sealed record ZoneRange(int Offset, int Length, LexicalZone Zone);

	private static readonly string[] _plainPrefixes = ["r", "R", "b", "B", "u", "U", "rb", "rB", "Rb", "RB", "br", "bR", "Br", "BR"];
	private static readonly string[] _formatPrefixes = ["f", "F", "rf", "rF", "Rf", "RF", "fr", "fR", "Fr", "FR"];
	private static readonly string[] _templatePrefixes = ["t", "T", "rt", "rT", "Rt", "RT", "tr", "tR", "Tr", "TR"];
	private static readonly char[] _specPredecessors = [':', '<', '>', '^', '=', '+', '-'];
	private static readonly char[] _notSpecPredecessors = [' ', 'x', '1', '(', ')', ',', '{', '}', '_', '\t', '\n'];

	public static IEnumerable<object[]> FixtureList => _fixtureList.Select(row => new object[] { row.FixtureId });

	public static IEnumerable<object[]> PlainPrefixes => _plainPrefixes.Select(prefix => new object[] { prefix });

	public static IEnumerable<object[]> InterpolatingPrefixes => _formatPrefixes.Concat(_templatePrefixes).Select(prefix => new object[] { prefix });

	public static IEnumerable<object[]> SpecPredecessors => _specPredecessors.Select(character => new object[] { character });

	public static IEnumerable<object[]> NotSpecPredecessors => _notSpecPredecessors.Select(character => new object[] { character });

	/// <summary>Fixture A1. A raw prefix still protects the quote: <c>r"\""</c> is one valid literal, the escaped quote is body.</summary>
	[Fact]
	public void ARawStringStillEscapesItsQuote()
	{
		ZoneMask mask = ShippedProfile.Scan(_RAW_ESCAPED_QUOTE);

		AssertValid(mask, _RAW_ESCAPED_QUOTE);
		AssertRange(mask, _RAW_ESCAPED_QUOTE, "r\"\\\"\"", new ZoneRange(0, 2, LexicalZone.Delimiter));
		AssertRange(mask, _RAW_ESCAPED_QUOTE, "r\"\\\"\"", new ZoneRange(2, 2, LexicalZone.StringBody));
		AssertRange(mask, _RAW_ESCAPED_QUOTE, "r\"\\\"\"", new ZoneRange(4, 1, LexicalZone.Delimiter));
		AssertTrailerIntact(mask, _RAW_ESCAPED_QUOTE);
	}

	/// <summary>Fixture A2. <c>r"\"</c> is an unterminated literal in Python; the scanner reproduces that by recovering at the line end with <see cref="LexicalAnomalyReason.NewlineInsideALiteral"/>, and the next line is code again.</summary>
	[Fact]
	public void ARawStringEndingInABackslashQuoteIsRecoveredAtTheLineEnd()
	{
		ZoneMask mask = ShippedProfile.Scan(_RAW_UNTERMINATED);

		Assert.Equal(_EXPECTED_ONE, mask.NewlineTerminatedLiteralCount);
		Assert.Equal(LexicalAnomalyReason.NewlineInsideALiteral, mask.AnomalyReason);
		AssertTrailerIntact(mask, _RAW_UNTERMINATED);
	}

	/// <summary>Fixture A3. <c>r"\\"</c> is valid: the backslash pair is body and the quote after it closes.</summary>
	[Fact]
	public void ARawStringWithAnEscapedBackslashCloses()
	{
		ZoneMask mask = ShippedProfile.Scan(_RAW_ESCAPED_BACKSLASH);

		AssertValid(mask, _RAW_ESCAPED_BACKSLASH);
		AssertRange(mask, _RAW_ESCAPED_BACKSLASH, "r\"\\\\\"", new ZoneRange(2, 2, LexicalZone.StringBody));
		AssertRange(mask, _RAW_ESCAPED_BACKSLASH, "r\"\\\\\"", new ZoneRange(4, 1, LexicalZone.Delimiter));
		AssertTrailerIntact(mask, _RAW_ESCAPED_BACKSLASH);
	}

	/// <summary>Fixture B1. A backslash-newline continues a single-quoted literal: one literal across the line feed.</summary>
	[Fact]
	public void ABackslashNewlineContinuesAPlainLiteral()
	{
		ZoneMask mask = ShippedProfile.Scan(_PLAIN_CONTINUATION);

		AssertValid(mask, _PLAIN_CONTINUATION);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _PLAIN_CONTINUATION, "ab\\\ncd", LexicalZone.StringBody);
		AssertTrailerIntact(mask, _PLAIN_CONTINUATION);
	}

	/// <summary>Fixture B3. The same continuation holds with a raw prefix.</summary>
	[Fact]
	public void ABackslashNewlineContinuesARawLiteral()
	{
		ZoneMask mask = ShippedProfile.Scan(_RAW_CONTINUATION);

		AssertValid(mask, _RAW_CONTINUATION);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _RAW_CONTINUATION, "ab\\\ncd", LexicalZone.StringBody);
		AssertTrailerIntact(mask, _RAW_CONTINUATION);
	}

	/// <summary>Fixture B2. A bare newline in a single-quoted literal is a syntax error in Python; the scanner recovers at each line end (two newline-terminated literals) and the trailer is code.</summary>
	[Fact]
	public void ABareNewlineInASingleQuotedLiteralIsRecoveredTwice()
	{
		ZoneMask mask = ShippedProfile.Scan(_BARE_NEWLINE_IN_SINGLE_QUOTES);

		Assert.Equal(_EXPECTED_TWO, mask.NewlineTerminatedLiteralCount);
		AssertTrailerIntact(mask, _BARE_NEWLINE_IN_SINGLE_QUOTES);
	}

	/// <summary>Fixture E3. A trailing backslash does not continue a comment: the next line is code.</summary>
	[Fact]
	public void ATrailingBackslashDoesNotContinueAComment()
	{
		ZoneMask mask = ShippedProfile.Scan(_COMMENT_WITH_TRAILING_BACKSLASH);

		AssertValid(mask, _COMMENT_WITH_TRAILING_BACKSLASH);
		ZoneAssertions.SpansZone(mask, _COMMENT_WITH_TRAILING_BACKSLASH, "# comment \\", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _COMMENT_WITH_TRAILING_BACKSLASH, "x = 1", LexicalZone.Code);
	}

	/// <summary>Fixture C1. A triple-quoted literal keeps newlines, a hash, single quotes, double quotes and a run of two quotes as body.</summary>
	[Fact]
	public void ATripleQuotedLiteralKeepsNewlinesHashesAndQuotesAsBody()
	{
		ZoneMask mask = ShippedProfile.Scan(_TRIPLE_WITH_CONTENT);

		AssertValid(mask, _TRIPLE_WITH_CONTENT);
		ZoneAssertions.SpansZone(mask, _TRIPLE_WITH_CONTENT, "line # one\n 'q' \"two\" \"\"three\n", LexicalZone.StringBody);
		AssertTrailerIntact(mask, _TRIPLE_WITH_CONTENT);
	}

	/// <summary>Fixture C2. <c>""""a"""</c> is read by the fixed rule as the tokenizer does: an opener of three quotes, the body <c>"a</c>, a closer of three.</summary>
	[Fact]
	public void AQuoteRunAfterATripleOpenerIsReadByTheFixedRule()
	{
		ZoneMask mask = ShippedProfile.Scan(_TRIPLE_QUOTE_RUN);

		AssertValid(mask, _TRIPLE_QUOTE_RUN);
		AssertRange(mask, _TRIPLE_QUOTE_RUN, "\"\"\"\"a\"\"\"", new ZoneRange(0, 3, LexicalZone.Delimiter));
		AssertRange(mask, _TRIPLE_QUOTE_RUN, "\"\"\"\"a\"\"\"", new ZoneRange(3, 2, LexicalZone.StringBody));
		AssertRange(mask, _TRIPLE_QUOTE_RUN, "\"\"\"\"a\"\"\"", new ZoneRange(5, 3, LexicalZone.Delimiter));
		AssertTrailerIntact(mask, _TRIPLE_QUOTE_RUN);
	}

	/// <summary>Fixture C3. A triple-single-quoted literal holds an apostrophe, double quotes and a hash as body.</summary>
	[Fact]
	public void ATripleSingleQuotedLiteralHoldsAnApostropheAndAHashAsBody()
	{
		ZoneMask mask = ShippedProfile.Scan(_TRIPLE_SINGLE_QUOTES);

		AssertValid(mask, _TRIPLE_SINGLE_QUOTES);
		ZoneAssertions.SpansZone(mask, _TRIPLE_SINGLE_QUOTES, "it's \"q\" # not", LexicalZone.StringBody);
		AssertTrailerIntact(mask, _TRIPLE_SINGLE_QUOTES);
	}

	/// <summary>Fixture C4. An escaped quote at the head of a quote run inside a triple literal does not close it: the closer is the later run of three.</summary>
	[Fact]
	public void AnEscapedQuoteBeforeAQuotePairDoesNotCloseATripleLiteral()
	{
		ZoneMask mask = ShippedProfile.Scan(_TRIPLE_ESCAPED_QUOTE);

		AssertValid(mask, _TRIPLE_ESCAPED_QUOTE);
		ZoneAssertions.SpansZone(mask, _TRIPLE_ESCAPED_QUOTE, "a\\\"\"\"b", LexicalZone.StringBody);
		AssertTrailerIntact(mask, _TRIPLE_ESCAPED_QUOTE);
	}

	/// <summary>Fixture E4. A docstring is a string literal: its hash and apostrophes are body, not comment.</summary>
	[Fact]
	public void ADocstringIsAStringLiteral()
	{
		ZoneMask mask = ShippedProfile.Scan(_DOCSTRING);

		AssertValid(mask, _DOCSTRING);
		ZoneAssertions.SpansZone(mask, _DOCSTRING, "Doc # text.\n\tMore 'text'.\n\t", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _DOCSTRING, "return 1", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, _DOCSTRING, LexicalZone.Comment);
	}

	/// <summary>Fixture D1. A same-quote subscript inside an f-string field (3.12 and later): the field is code, the inner literal is its own literal.</summary>
	[Fact]
	public void AnFStringFieldMayReuseTheOuterQuote()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_SUBSCRIPT);

		AssertValid(mask, _FSTRING_SUBSCRIPT);
		AssertRange(mask, _FSTRING_SUBSCRIPT, "f\"{d[\"k\"]}\"", new ZoneRange(0, 3, LexicalZone.Delimiter));
		AssertRange(mask, _FSTRING_SUBSCRIPT, "f\"{d[\"k\"]}\"", new ZoneRange(3, 2, LexicalZone.Code));
		AssertRange(mask, _FSTRING_SUBSCRIPT, "f\"{d[\"k\"]}\"", new ZoneRange(5, 1, LexicalZone.Delimiter));
		AssertRange(mask, _FSTRING_SUBSCRIPT, "f\"{d[\"k\"]}\"", new ZoneRange(6, 1, LexicalZone.StringBody));
		AssertRange(mask, _FSTRING_SUBSCRIPT, "f\"{d[\"k\"]}\"", new ZoneRange(7, 1, LexicalZone.Delimiter));
		AssertRange(mask, _FSTRING_SUBSCRIPT, "f\"{d[\"k\"]}\"", new ZoneRange(8, 1, LexicalZone.Code));
		AssertRange(mask, _FSTRING_SUBSCRIPT, "f\"{d[\"k\"]}\"", new ZoneRange(9, 2, LexicalZone.Delimiter));
		AssertTrailerIntact(mask, _FSTRING_SUBSCRIPT);
	}

	/// <summary>Fixture D2. Same-quote nesting to depth two.</summary>
	[Fact]
	public void AnFStringNestedInAFieldToDepthTwoIsMaskedAsTheInterpreterReadsIt()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_DEPTH_TWO);

		AssertValid(mask, _FSTRING_DEPTH_TWO);
		AssertRange(mask, _FSTRING_DEPTH_TWO, "f\"{f\"{y}\"}\"", new ZoneRange(0, 6, LexicalZone.Delimiter));
		AssertRange(mask, _FSTRING_DEPTH_TWO, "f\"{f\"{y}\"}\"", new ZoneRange(6, 1, LexicalZone.Code));
		AssertRange(mask, _FSTRING_DEPTH_TWO, "f\"{f\"{y}\"}\"", new ZoneRange(7, 4, LexicalZone.Delimiter));
		AssertTrailerIntact(mask, _FSTRING_DEPTH_TWO);
	}

	/// <summary>Fixture D20. Same-quote nesting to depth three.</summary>
	[Fact]
	public void AnFStringNestedInAFieldToDepthThreeIsMaskedAsTheInterpreterReadsIt()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_DEPTH_THREE);

		AssertValid(mask, _FSTRING_DEPTH_THREE);
		AssertRange(mask, _FSTRING_DEPTH_THREE, "f\"{f\"{f\"{y}\"}\"}\"", new ZoneRange(0, 9, LexicalZone.Delimiter));
		AssertRange(mask, _FSTRING_DEPTH_THREE, "f\"{f\"{f\"{y}\"}\"}\"", new ZoneRange(9, 1, LexicalZone.Code));
		AssertRange(mask, _FSTRING_DEPTH_THREE, "f\"{f\"{f\"{y}\"}\"}\"", new ZoneRange(10, 6, LexicalZone.Delimiter));
		AssertTrailerIntact(mask, _FSTRING_DEPTH_THREE);
	}

	/// <summary>Fixture D16. A triple-quoted f-string whose field holds a double-quoted and a single-quoted literal.</summary>
	[Fact]
	public void ATripleQuotedFStringFieldMayHoldBothQuoteStyles()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_TRIPLE_BOTH_QUOTES);

		AssertValid(mask, _FSTRING_TRIPLE_BOTH_QUOTES);
		ZoneAssertions.SpansZone(mask, _FSTRING_TRIPLE_BOTH_QUOTES, "f\"\"\"{", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _FSTRING_TRIPLE_BOTH_QUOTES, "d[", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _FSTRING_TRIPLE_BOTH_QUOTES, "k", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _FSTRING_TRIPLE_BOTH_QUOTES, "] + d[", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _FSTRING_TRIPLE_BOTH_QUOTES, "j", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _FSTRING_TRIPLE_BOTH_QUOTES, "}\"\"\"", LexicalZone.Delimiter);
		AssertTrailerIntact(mask, _FSTRING_TRIPLE_BOTH_QUOTES);
	}

	/// <summary>Fixture D6. A field that spans two physical lines carries a hash comment: the comment is a comment, the field's closer after the line feed is delimiter.</summary>
	[Fact]
	public void AMultiLineFieldMayCarryAHashComment()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_MULTILINE_COMMENT);

		AssertValid(mask, _FSTRING_MULTILINE_COMMENT);
		ZoneAssertions.SpansZone(mask, _FSTRING_MULTILINE_COMMENT, "n ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _FSTRING_MULTILINE_COMMENT, "# note", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _FSTRING_MULTILINE_COMMENT, "}\"", LexicalZone.Delimiter);
		AssertTrailerIntact(mask, _FSTRING_MULTILINE_COMMENT);
	}

	/// <summary>Fixture D7. A hash inside a literal nested in a field is string body, not a comment.</summary>
	[Fact]
	public void AHashInsideALiteralNestedInAFieldIsStringBody()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_HASH_IN_NESTED_LITERAL);

		AssertValid(mask, _FSTRING_HASH_IN_NESTED_LITERAL);
		ZoneAssertions.SpansZone(mask, _FSTRING_HASH_IN_NESTED_LITERAL, "#", LexicalZone.StringBody);
		ZoneAssertions.NoPositionCarries(mask, _FSTRING_HASH_IN_NESTED_LITERAL, LexicalZone.Comment);
		AssertTrailerIntact(mask, _FSTRING_HASH_IN_NESTED_LITERAL);
	}

	/// <summary>Fixture D8. <c>{{{n}}}</c>: a doubled brace is text, then the field, then a doubled brace of text; the braces pair from the left.</summary>
	[Fact]
	public void DoubledBracesAreTextAndPairFromTheLeft()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_DOUBLED_BRACES);

		AssertValid(mask, _FSTRING_DOUBLED_BRACES);
		AssertRange(mask, _FSTRING_DOUBLED_BRACES, "{{{n}}}", new ZoneRange(0, 2, LexicalZone.StringBody));
		AssertRange(mask, _FSTRING_DOUBLED_BRACES, "{{{n}}}", new ZoneRange(2, 1, LexicalZone.Delimiter));
		AssertRange(mask, _FSTRING_DOUBLED_BRACES, "{{{n}}}", new ZoneRange(3, 1, LexicalZone.Code));
		AssertRange(mask, _FSTRING_DOUBLED_BRACES, "{{{n}}}", new ZoneRange(4, 1, LexicalZone.Delimiter));
		AssertRange(mask, _FSTRING_DOUBLED_BRACES, "{{{n}}}", new ZoneRange(5, 2, LexicalZone.StringBody));
		AssertTrailerIntact(mask, _FSTRING_DOUBLED_BRACES);
	}

	/// <summary>Fixture D5a. A conversion <c>!r</c> stays inside the field, which is code.</summary>
	[Fact]
	public void AConversionSuffixStaysInsideTheField()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_CONVERSION);

		AssertValid(mask, _FSTRING_CONVERSION);
		ZoneAssertions.SpansZone(mask, _FSTRING_CONVERSION, "n!r", LexicalZone.Code);
		AssertTrailerIntact(mask, _FSTRING_CONVERSION);
	}

	/// <summary>Fixture D5. A conversion, a format spec and a nested field <c>{w}</c>: the scan stays balanced and the closer is found.</summary>
	[Fact]
	public void ANestedSpecFieldKeepsTheScanBalanced()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_NESTED_SPEC);

		AssertValid(mask, _FSTRING_NESTED_SPEC);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, _FSTRING_NESTED_SPEC, "n!r:>{w}", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _FSTRING_NESTED_SPEC, "}\"", LexicalZone.Delimiter);
		AssertTrailerIntact(mask, _FSTRING_NESTED_SPEC);
	}

	/// <summary>Fixture D12. Two nested fields in one spec.</summary>
	[Fact]
	public void TwoNestedSpecFieldsKeepTheScanBalanced()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_NESTED_SPEC_TWO);

		AssertValid(mask, _FSTRING_NESTED_SPEC_TWO);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, _FSTRING_NESTED_SPEC_TWO, "n:{w}.{p}f", LexicalZone.Code);
		AssertTrailerIntact(mask, _FSTRING_NESTED_SPEC_TWO);
	}

	/// <summary>Fixture H1. The alternate-form flag <c>{n:#x}</c>: the hash after the colon is spec text, so the guard keeps it from opening a comment and the scan stays valid.</summary>
	[Fact]
	public void TheHashFlagAfterAColonDoesNotOpenAComment()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_HASH_FLAG);

		AssertValid(mask, _FSTRING_HASH_FLAG);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		ZoneAssertions.SpansZone(mask, _FSTRING_HASH_FLAG, "n:#x", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _FSTRING_HASH_FLAG, "}\"", LexicalZone.Delimiter);
		ZoneAssertions.NoPositionCarries(mask, _FSTRING_HASH_FLAG, LexicalZone.Comment);
		AssertTrailerIntact(mask, _FSTRING_HASH_FLAG);
	}

	/// <summary>Fixture H2. <c>{n:>#10x}</c>: the hash after an alignment character is guarded too.</summary>
	[Fact]
	public void TheHashFlagAfterAnAlignmentCharacterDoesNotOpenAComment()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_HASH_AFTER_ALIGN);

		AssertValid(mask, _FSTRING_HASH_AFTER_ALIGN);
		ZoneAssertions.SpansZone(mask, _FSTRING_HASH_AFTER_ALIGN, "n:>#10x", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, _FSTRING_HASH_AFTER_ALIGN, LexicalZone.Comment);
		AssertTrailerIntact(mask, _FSTRING_HASH_AFTER_ALIGN);
	}

	/// <summary>Fixture H3. <c>{n:*^#x}</c>: fill, alignment, then the hash.</summary>
	[Fact]
	public void TheHashFlagAfterAFillAndAlignmentDoesNotOpenAComment()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_HASH_AFTER_FILL_AND_ALIGN);

		AssertValid(mask, _FSTRING_HASH_AFTER_FILL_AND_ALIGN);
		ZoneAssertions.NoPositionCarries(mask, _FSTRING_HASH_AFTER_FILL_AND_ALIGN, LexicalZone.Comment);
		AssertTrailerIntact(mask, _FSTRING_HASH_AFTER_FILL_AND_ALIGN);
	}

	/// <summary>Fixture H4. <c>{n:+#b}</c>: a sign, then the hash.</summary>
	[Fact]
	public void TheHashFlagAfterASignDoesNotOpenAComment()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_HASH_AFTER_SIGN);

		AssertValid(mask, _FSTRING_HASH_AFTER_SIGN);
		ZoneAssertions.NoPositionCarries(mask, _FSTRING_HASH_AFTER_SIGN, LexicalZone.Comment);
		AssertTrailerIntact(mask, _FSTRING_HASH_AFTER_SIGN);
	}

	/// <summary>Fixture D9. A backslash directly before a brace escapes it in the scanner: the field is not entered and its body is string body (no scan damage; the field's content is missed, a bounded cost).</summary>
	[Fact]
	public void ABackslashBeforeABraceKeepsTheFieldAsStringBody()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_BACKSLASH_BEFORE_BRACE);

		AssertValid(mask, _FSTRING_BACKSLASH_BEFORE_BRACE);
		ZoneAssertions.SpansZone(mask, _FSTRING_BACKSLASH_BEFORE_BRACE, "\\{n}", LexicalZone.StringBody);
		AssertTrailerIntact(mask, _FSTRING_BACKSLASH_BEFORE_BRACE);
	}

	/// <summary>Fixture D11. The same reading for a raw f-string.</summary>
	[Fact]
	public void ABackslashBeforeABraceInARawFStringKeepsTheFieldAsStringBody()
	{
		ZoneMask mask = ShippedProfile.Scan(_RAW_FSTRING_BACKSLASH_BEFORE_BRACE);

		AssertValid(mask, _RAW_FSTRING_BACKSLASH_BEFORE_BRACE);
		ZoneAssertions.SpansZone(mask, _RAW_FSTRING_BACKSLASH_BEFORE_BRACE, "\\{n}", LexicalZone.StringBody);
		AssertTrailerIntact(mask, _RAW_FSTRING_BACKSLASH_BEFORE_BRACE);
	}

	/// <summary>Fixture D10. <c>\N{EM DASH}</c>: the escape is consumed, the name is read as a hole whose body is code (harmless and balanced), and the real field after it is code.</summary>
	[Fact]
	public void ANamedEscapeIsHarmlessAndBalanced()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_NAMED_ESCAPE);

		AssertValid(mask, _FSTRING_NAMED_ESCAPE);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, _FSTRING_NAMED_ESCAPE, "\\N", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _FSTRING_NAMED_ESCAPE, "EM DASH", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _FSTRING_NAMED_ESCAPE, "n", LexicalZone.Code);
		AssertTrailerIntact(mask, _FSTRING_NAMED_ESCAPE);
	}

	/// <summary>Fixture E1. <c>if"a"</c>, <c>else"b"</c>, <c>or"b"</c>: a prefix letter that ends a longer word is not a prefix; <c>if"a"</c> is a plain literal, not an <c>f</c> string.</summary>
	[Fact]
	public void AKeywordEndingInAPrefixLetterBeforeAQuoteIsNotAPrefix()
	{
		ZoneMask mask = ShippedProfile.Scan(_KEYWORD_ADJACENT_QUOTES);

		AssertValid(mask, _KEYWORD_ADJACENT_QUOTES);
		ZoneAssertions.SpansZone(mask, _KEYWORD_ADJACENT_QUOTES, "y = 1 if", LexicalZone.Code);
		AssertRange(mask, _KEYWORD_ADJACENT_QUOTES, "if\"a\"", new ZoneRange(2, 1, LexicalZone.Delimiter));
		AssertRange(mask, _KEYWORD_ADJACENT_QUOTES, "if\"a\"", new ZoneRange(3, 1, LexicalZone.StringBody));
		AssertRange(mask, _KEYWORD_ADJACENT_QUOTES, "if\"a\"", new ZoneRange(4, 1, LexicalZone.Delimiter));
		ZoneAssertions.SpansZone(mask, _KEYWORD_ADJACENT_QUOTES, "else", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _KEYWORD_ADJACENT_QUOTES, "w = x o", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _KEYWORD_ADJACENT_QUOTES, "r\"", LexicalZone.Delimiter, 0);
	}

	/// <summary>Fixture E1b. The braces after <c>if"</c> are body: had the predicate failed, <c>f"{n}"</c> would have opened a field.</summary>
	[Fact]
	public void BracesAfterAKeywordEndingInFAreStringBody()
	{
		ZoneMask mask = ShippedProfile.Scan(_KEYWORD_ADJACENT_IF_THEN_BRACES);

		AssertValid(mask, _KEYWORD_ADJACENT_IF_THEN_BRACES);
		ZoneAssertions.SpansZone(mask, _KEYWORD_ADJACENT_IF_THEN_BRACES, "{n}", LexicalZone.StringBody);
		AssertTrailerIntact(mask, _KEYWORD_ADJACENT_IF_THEN_BRACES);
	}

	/// <summary>Fixture E1c. The converse: an <c>f</c> that follows a blank is a prefix and opens a field.</summary>
	[Fact]
	public void AnFAfterABlankIsAPrefixAndOpensAField()
	{
		ZoneMask mask = ShippedProfile.Scan(_FSTRING_AFTER_A_BLANK);

		AssertValid(mask, _FSTRING_AFTER_A_BLANK);
		ZoneAssertions.SpansZone(mask, _FSTRING_AFTER_A_BLANK, "n", LexicalZone.Code);
		AssertTrailerIntact(mask, _FSTRING_AFTER_A_BLANK);
	}

	/// <summary>Plain prefix family (14): the prefix and quote are delimiter, a hash and a brace pair are body (no interpolation), the trailer is intact.</summary>
	[Theory]
	[MemberData(nameof(PlainPrefixes))]
	public void EveryPlainPrefixOpensALiteralWithoutInterpolation(string prefix)
	{
		string source = $"x = {prefix}\"#{{n}}\"\n{_TRAILER}";
		ZoneMask mask = ShippedProfile.Scan(source);

		AssertValid(mask, source);
		AssertRange(mask, source, source, new ZoneRange(_FIRST_LITERAL_START, prefix.Length + 1, LexicalZone.Delimiter));
		AssertRange(mask, source, source, new ZoneRange(_FIRST_LITERAL_START + prefix.Length + 1, 4, LexicalZone.StringBody));
		AssertRange(mask, source, source, new ZoneRange(_FIRST_LITERAL_START + prefix.Length + 5, 1, LexicalZone.Delimiter));
		AssertTrailerIntact(mask, source);
	}

	/// <summary>Format (10) and template (10) prefix families: the field opens, a same-quote nested literal is its own literal, the hash after the field is body.</summary>
	[Theory]
	[MemberData(nameof(InterpolatingPrefixes))]
	public void EveryFormatAndTemplatePrefixInterpolatesAndAllowsSameQuoteNesting(string prefix)
	{
		string source = $"x = {prefix}\"a{{d[\"k\"]}}#b\"\n{_TRAILER}";
		ZoneMask mask = ShippedProfile.Scan(source);
		int opening = _FIRST_LITERAL_START + prefix.Length + 1;

		AssertValid(mask, source);
		AssertRange(mask, source, source, new ZoneRange(_FIRST_LITERAL_START, prefix.Length + 1, LexicalZone.Delimiter));
		AssertRange(mask, source, source, new ZoneRange(opening, 1, LexicalZone.StringBody));
		AssertRange(mask, source, source, new ZoneRange(opening + 1, 1, LexicalZone.Delimiter));
		AssertRange(mask, source, source, new ZoneRange(opening + 2, 2, LexicalZone.Code));
		AssertRange(mask, source, source, new ZoneRange(opening + 4, 1, LexicalZone.Delimiter));
		AssertRange(mask, source, source, new ZoneRange(opening + 5, 1, LexicalZone.StringBody));
		AssertRange(mask, source, source, new ZoneRange(opening + 6, 1, LexicalZone.Delimiter));
		AssertRange(mask, source, source, new ZoneRange(opening + 7, 1, LexicalZone.Code));
		AssertRange(mask, source, source, new ZoneRange(opening + 8, 1, LexicalZone.Delimiter));
		AssertRange(mask, source, source, new ZoneRange(opening + 9, 2, LexicalZone.StringBody));
		AssertRange(mask, source, source, new ZoneRange(opening + 11, 1, LexicalZone.Delimiter));
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Comment);
		AssertTrailerIntact(mask, source);
	}

	/// <summary>Fixture T2. Without the template prefixes <c>t"{d["k"]}"</c> would read <c>k</c> as code; with them the key is string body and the subscript is code.</summary>
	[Fact]
	public void ATemplateStringFieldIsReadLikeAnFStringField()
	{
		string source = "x = t\"{d[\"k\"]}\"\n" + _TRAILER;
		ZoneMask mask = ShippedProfile.Scan(source);

		AssertValid(mask, source);
		ZoneAssertions.SpansZone(mask, source, "k", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "d[", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "]", LexicalZone.Code);
		AssertTrailerIntact(mask, source);
	}

	/// <summary>Every fixture of the numbered list, by id: the zone mask the list states under the wide-set profile, position by position, with its valid-through position, anomaly reason and the two recovered counters.</summary>
	[Theory]
	[MemberData(nameof(FixtureList))]
	public void EveryFixtureOfTheListHasTheMaskTheListStates(string fixtureId)
	{
		ProbeFixtureRow row = _fixtureList.Single(candidate => candidate.FixtureId == fixtureId);
		string text = row.Text;
		string expectedMask = row.ExpectedMask;
		ProbeFixtureOutcome outcome = row.Outcome;
		ZoneMask mask = ShippedProfile.Scan(text);
		string context = $"Fixture {fixtureId}.{Environment.NewLine}{ZoneAssertions.Render(mask, text)}";

		Assert.True(expectedMask == RenderGlyphs(mask), $"{context}Expected {expectedMask}");
		Assert.True(outcome.ValidThrough == mask.ValidThroughPosition, $"{context}Valid through {mask.ValidThroughPosition}, expected {outcome.ValidThrough}.");
		Assert.True(outcome.AnomalyReason == mask.AnomalyReason, $"{context}Anomaly {mask.AnomalyReason}, expected {outcome.AnomalyReason}.");
		Assert.True(outcome.NewlineTerminated == mask.NewlineTerminatedLiteralCount, $"{context}Newline-terminated {mask.NewlineTerminatedLiteralCount}, expected {outcome.NewlineTerminated}.");
		Assert.True(outcome.Unbalanced == mask.UnbalancedNestingCount, $"{context}Unbalanced {mask.UnbalancedNestingCount}, expected {outcome.Unbalanced}.");
	}

	/// <summary>The hash predicate refuses to open a comment directly after each of <c>: &lt; &gt; ^ = + -</c>.</summary>
	[Theory]
	[MemberData(nameof(SpecPredecessors))]
	public void TheHashPredicateIsFalseAfterEachSpecCharacter(char predecessor)
	{
		string text = $"{predecessor}#";

		Assert.False(ShippedProfile.HashIsNotAFormatSpecMark(text, 1));
	}

	/// <summary>The hash predicate lets a hash open after a blank, a letter, a digit, a closing bracket and the like, and at position zero.</summary>
	[Theory]
	[MemberData(nameof(NotSpecPredecessors))]
	public void TheHashPredicateIsTrueAfterAnyOtherCharacter(char predecessor)
	{
		string text = $"{predecessor}#";

		Assert.True(ShippedProfile.HashIsNotAFormatSpecMark(text, 1));
		Assert.True(ShippedProfile.HashIsNotAFormatSpecMark("#", 0));
	}

	/// <summary>The prefix predicate is false when the previous character is a letter, a digit or an underscore (the prefix would end a longer word) and true after a blank, an opening bracket, a comma, or at position zero.</summary>
	[Theory]
	[InlineData("if", 1, false)]
	[InlineData("or", 1, false)]
	[InlineData("_f", 1, false)]
	[InlineData("1f", 1, false)]
	[InlineData("Af", 1, false)]
	[InlineData(" f", 1, true)]
	[InlineData("(f", 1, true)]
	[InlineData(",f", 1, true)]
	[InlineData("=f", 1, true)]
	[InlineData("\nf", 1, true)]
	[InlineData("f", 0, true)]
	public void ThePrefixPredicateRefusesAPrefixThatEndsAWord(string text, int position, bool expected)
	{
		Assert.Equal(expected, ShippedProfile.PrefixDoesNotEndAWord(text, position));
	}

	/// <summary>DECLARED COST. A space sign before the hash, <c>{255: #x}</c>, is valid Python the guard cannot cover: the hash opens a comment, swallows the closer and the scan is abandoned. Pinned as measured; a change is a decision.</summary>
	[Fact]
	public void DeclaredCostASpaceSignBeforeTheHashAbandonsTheScan()
	{
		ZoneMask mask = ShippedProfile.Scan(_COST_SPACE_SIGN);

		Assert.False(mask.IsCompletelyValid, ZoneAssertions.Render(mask, _COST_SPACE_SIGN));
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		AssertRange(mask, _COST_SPACE_SIGN, _COST_SPACE_SIGN, new ZoneRange(_FIRST_LITERAL_START + 2, _COST_SPACE_SIGN.Length - _FIRST_LITERAL_START - 2, LexicalZone.StringBody));
	}

	/// <summary>DECLARED COST. The <c>z</c> option before the hash, <c>{3.14159:z#.2f}</c>, is not covered either.</summary>
	[Fact]
	public void DeclaredCostTheZOptionBeforeTheHashAbandonsTheScan()
	{
		ZoneMask mask = ShippedProfile.Scan(_COST_Z_OPTION);

		Assert.False(mask.IsCompletelyValid, ZoneAssertions.Render(mask, _COST_Z_OPTION));
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
	}

	/// <summary>DECLARED COST. A quote as the fill character, <c>{n:'>10}</c>, is not repaired: the scan is abandoned at the first literal (valid through 4) and the whole tail, trailer included, is filled as string body, so a later detection would read as string text.</summary>
	[Fact]
	public void DeclaredCostAQuoteFillCharacterAbandonsTheScanAndFillsTheTailAsStringBody()
	{
		ZoneMask mask = ShippedProfile.Scan(_COST_QUOTE_FILL);

		Assert.False(mask.IsCompletelyValid, ZoneAssertions.Render(mask, _COST_QUOTE_FILL));
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(_ABANDONED_AT_THE_FIRST_LITERAL, mask.ValidThroughPosition);
		AssertRange(mask, _COST_QUOTE_FILL, _COST_QUOTE_FILL, new ZoneRange(_ABANDONED_AT_THE_FIRST_LITERAL, 2, LexicalZone.Delimiter));
		AssertRange(mask, _COST_QUOTE_FILL, _COST_QUOTE_FILL, new ZoneRange(_FIRST_LITERAL_START + 2, _COST_QUOTE_FILL.Length - _FIRST_LITERAL_START - 2, LexicalZone.StringBody));
	}

	/// <summary>DECLARED COST. The same with the opposite outer quote: <c>f'{n:"^10}'</c>.</summary>
	[Fact]
	public void DeclaredCostAQuoteFillCharacterUnderASingleQuotedOuterLiteralAbandonsTheScan()
	{
		ZoneMask mask = ShippedProfile.Scan(_COST_QUOTE_FILL_SINGLE_OUTER);

		Assert.False(mask.IsCompletelyValid, ZoneAssertions.Render(mask, _COST_QUOTE_FILL_SINGLE_OUTER));
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
	}

	/// <summary>DECLARED COST. A tight comment after a colon, <c>if y:# note</c>, is read as code, so a pattern spelled in it could fire.</summary>
	[Fact]
	public void DeclaredCostATightCommentAfterAColonIsReadAsCode()
	{
		ZoneMask mask = ShippedProfile.Scan(_COST_TIGHT_COMMENT_AFTER_COLON);

		ZoneAssertions.SpansZone(mask, _COST_TIGHT_COMMENT_AFTER_COLON, "# tight comment", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, _COST_TIGHT_COMMENT_AFTER_COLON, LexicalZone.Comment);
	}

	/// <summary>The normal spacing is unchanged by the guard: a comment after a colon and a blank is still a comment.</summary>
	[Fact]
	public void ASpacedCommentAfterAColonIsStillAComment()
	{
		ZoneMask mask = ShippedProfile.Scan(_SPACED_COMMENT_AFTER_COLON);

		AssertValid(mask, _SPACED_COMMENT_AFTER_COLON);
		ZoneAssertions.SpansZone(mask, _SPACED_COMMENT_AFTER_COLON, "# spaced comment", LexicalZone.Comment);
	}

	/// <summary>DECLARED COST. A tight comment after an operator sign, <c>x+#note</c>, is read as code.</summary>
	[Fact]
	public void DeclaredCostATightCommentAfterAnOperatorIsReadAsCode()
	{
		ZoneMask mask = ShippedProfile.Scan(_COST_TIGHT_COMMENT_AFTER_OPERATOR);

		ZoneAssertions.SpansZone(mask, _COST_TIGHT_COMMENT_AFTER_OPERATOR, "#note", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, _COST_TIGHT_COMMENT_AFTER_OPERATOR, LexicalZone.Comment);
	}

	/// <summary>DECLARED COST. A tight comment holding an apostrophe, <c>else:#it's</c>, opens a literal that a line end recovers: one newline-terminated literal.</summary>
	[Fact]
	public void DeclaredCostATightCommentWithAnApostropheOpensARecoveredLiteral()
	{
		ZoneMask mask = ShippedProfile.Scan(_TIGHT_ELSE_APOSTROPHE);

		Assert.Equal(_EXPECTED_ONE, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _TIGHT_ELSE_APOSTROPHE, "z = 1", LexicalZone.Code);
	}

	/// <summary>DECLARED COST. A tight comment holding a triple quote, <c>else:#'''</c>, opens a triple literal that nothing closes: the file tail is abandoned.</summary>
	[Fact]
	public void DeclaredCostATightCommentWithATripleQuoteAbandonsTheFileTail()
	{
		ZoneMask mask = ShippedProfile.Scan(_TIGHT_ELSE_TRIPLE);

		Assert.False(mask.IsCompletelyValid, ZoneAssertions.Render(mask, _TIGHT_ELSE_TRIPLE));
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
	}

	private static string RenderGlyphs(ZoneMask mask)
	{
		char[] glyphs = new char[mask.Length];

		for (int position = 0; position < mask.Length; position++)
		{
			glyphs[position] = mask.ZoneAt(position) switch
			{
				LexicalZone.Comment => _COMMENT_GLYPH,
				LexicalZone.StringBody => _STRING_BODY_GLYPH,
				LexicalZone.Delimiter => _DELIMITER_GLYPH,
				_ => _CODE_GLYPH
			};
		}

		return new string(glyphs);
	}

	private static void AssertValid(ZoneMask mask, string source)
	{
		Assert.True(mask.IsCompletelyValid, $"The mask is valid only through {mask.ValidThroughPosition} ({mask.AnomalyReason}).{Environment.NewLine}{ZoneAssertions.Render(mask, source)}");
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	/// <summary>The range of <paramref name="length"/> positions starting <paramref name="offset"/> after the first occurrence of <paramref name="marker"/> lies entirely in the zone.</summary>
	private static void AssertRange(ZoneMask mask, string source, string marker, ZoneRange range)
	{
		int offset = range.Offset;
		int length = range.Length;
		LexicalZone zone = range.Zone;
		int start = source.IndexOf(marker, StringComparison.Ordinal);

		Assert.True(start >= 0, "The source does not contain the marker.");

		LexicalZone[] actual = mask.Zones.Slice(start + offset, length).ToArray();
		LexicalZone[] expected = Enumerable.Repeat(zone, length).ToArray();

		Assert.True(expected.SequenceEqual(actual), $"Positions {start + offset}..{start + offset + length} did not carry {zone}.{Environment.NewLine}{ZoneAssertions.Render(mask, source)}");
	}

	/// <summary>The statement after the fixture is untouched: code outside its literal, the literal's quotes delimiters and its content string body.</summary>
	private static void AssertTrailerIntact(ZoneMask mask, string source)
	{
		int start = source.LastIndexOf(_TRAILER_TEXT, StringComparison.Ordinal);

		AssertRange(mask, source, source[start..], new ZoneRange(0, 4, LexicalZone.Code));
		AssertRange(mask, source, source[start..], new ZoneRange(4, 1, LexicalZone.Delimiter));
		AssertRange(mask, source, source[start..], new ZoneRange(5, 1, LexicalZone.StringBody));
		AssertRange(mask, source, source[start..], new ZoneRange(6, 1, LexicalZone.Delimiter));
	}

	/// <summary>The profile the binary ships, built once, with the scanner over it and the two predicates it declares, read from the profile's own rules.</summary>
	private static class ShippedProfile
	{
		private static readonly LexicalProfile _profile = PythonShippedAnalyzer.Instance.LexicalProfile;

		private static readonly LexicalScanner _scanner = new(_profile);

		internal static ZoneMask Scan(string source)
		{
			return _scanner.Scan(source);
		}

		internal static bool HashIsNotAFormatSpecMark(string text, int delimiterPosition)
		{
			return _profile.LineCommentRulesInEngineOrder.Single().OpeningContextPredicate!(text, delimiterPosition);
		}

		internal static bool PrefixDoesNotEndAWord(string text, int delimiterPosition)
		{
			return _profile.QuoteRulesInEngineOrder.First(rule => rule.InterpolationOpening is not null).OpeningContextPredicate!(text, delimiterPosition);
		}
	}
}
