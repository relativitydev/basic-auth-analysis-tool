using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE CONTRACT-PROBE FIXTURES OF THE JAVASCRIPT PROFILE. One synthetic fixture per construct, written from the ECMAScript specification with
/// generic names and held inline, run through the SHIPPED scanner with the test-local profile of <see cref="JavaScriptShapedProfile"/>, each
/// asserting the zone mask. A case that fails is a finding about a profile declaration or about the scanner. The dump test below writes every
/// fixture's mask to the path named by <see cref="DUMP_VARIABLE_NAME"/>, which is what a later change under <c>Lexical/</c> re-runs to compare.
///
/// Cases that RECORD a declared cost say so in their summary: the assertion pins what the scanner does today, so that a later change is a
/// decision and not an accident. Nothing here reads any file of a repository.
/// </summary>
public sealed class JavaScriptProbeFixtureTests
{
	internal const string DUMP_VARIABLE_NAME = "MIGRATE_AUTH_JS_PROBE_DUMP";

	private const string _FOLLOWING_STATEMENT = "const after = 1;";
	private const string _REGEX_CANDIDATE = "/re/ 1;\n";
	private const string _HASHBANG_LINE = "#!/usr/bin/env node";
	private const string _NESTED_INNERMOST = "f('}`')";
	private const int _TEMPLATE_DEPTH_THREE = 3;
	private const int _TEMPLATE_DEPTH_EIGHT = 8;
	private const int _TEMPLATE_DEPTH_NINE = 9;
	private const int _WHITESPACE_GAP_BELOW_THE_CEILING = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - 1;
	private const int _WHITESPACE_GAP_AT_THE_CEILING = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS;
	private const int _MAXIMUM_DUMPED_FIXTURE_LENGTH = 600;
	private const byte _UTF8_MARK_FIRST = 0xEF;
	private const byte _UTF8_MARK_SECOND = 0xBB;
	private const byte _UTF8_MARK_THIRD = 0xBF;
	private const int _AFTER_EVERY_DECLARATION = int.MaxValue;
	private const string _TEMPLATE_HOLE_OPENING = "${";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const char _BACKTICK = '`';
	private const char _SLASH = '/';
	private const int _FIRST_CHARACTER_AFTER_THE_SLASH = 1;
	private const int _SECOND_CHARACTER_AFTER_THE_SLASH = 2;

	/// <summary>Without normalization the first string ends at the raw line feed after the escaped carriage return, and the closing quote that follows then opens a second string that the next line feed ends.</summary>
	private const int _UNNORMALIZED_CRLF_RECOVERED_LITERALS = 2;

	private const string _LINE_COMMENT = "const a = 1; // note it's here\nconst b = 2;\n";
	private const string _BLOCK_COMMENT = "a(); /* multi\nline ' \" ` */ b();\n";
	private const string _HASHBANG_AT_ZERO = "#!/usr/bin/env node\n'use strict';\nconst b = 2;\n";
	private const string _HASH_BANG_ELSEWHERE = "const a = 1;\n#!b ok;\nconst b = 2;\n";
	private const string _HASH_BANG_AFTER_A_BLANK_LINE = "\n#!/usr/bin/env node\nconst b = 2;\n";
	private const string _PRIVATE_FIELD = "class Counter {\n\t#count = 0;\n\tinc() { return this.#count + 1; }\n\tstatic has(o) { return #count in o; }\n}\n";
	private const string _QUOTES = "const a = 'it\\'s';\nconst b = \"say \\\"hi\\\"\";\nconst c = 'x' + \"y\";\n";
	private const string _CONTINUATION = "const a = 'one \\\ntwo';\nconst b = \"three \\\nfour\";\nconst c = 2;\n";
	private const string _RAW_NEWLINE_IN_A_STRING = "const a = 'open\nconst b = 2;\n";
	private const string _TEMPLATE_DEPTH_THREE_SOURCE = "const t = `a ${ f('}`') + `b ${ g(\"`}\") + `c ${ h } c` } b` } a`;\nconst after = 1;\n";
	private const string _OBJECT_IN_A_HOLE = "const t = `v ${ ({ a: '}', b: { c: `}` } }).a } end`;\nconst after = 1;\n";
	private const string _COMMENT_AND_REGEX_IN_A_HOLE = "const t = `a ${ x /* } ` */ + /[}`]/.source + y // }\n } b`;\nconst after = 1;\n";
	private const string _RAW_AND_TAGGED_TEMPLATES = "const p = String.raw`C:\\new\\${name}`;\nconst q = tag`a ${b} c`;\nconst e = `x\\`y\\${z}`;\nconst after = 1;\n";
	private const string _SLASH_IN_A_CLASS = "const r = /[/]+\\/x/g.test(s);\nconst after = 1;\n";
	private const string _ESCAPED_BRACKET_IN_A_REGEX = "const r = /[\\]/]+/.test(s);\nconst after = 1;\n";
	private const string _REGEX_AFTER_A_TRAILING_COMMENT = "const x = 1; // check the trailing\n/re/.test(s);\nconst after = 1;\n";
	private const string _REGEX_AFTER_A_TRAILING_COMMENT_DAMAGE = "const x = 1; // check the note\n/'/.test(s);\nconst after = 1;\n";
	private const string _REGEX_AFTER_A_BLOCK_COMMENT = "const x = 1; /* note */ /re/.test(s);\nconst after = 1;\n";
	private const string _REGEX_AFTER_A_BLOCK_COMMENT_DAMAGE = "const x = 1; /* note */ /'/.test(s);\nconst after = 1;\n";
	private const string _REGEX_AFTER_A_TRAILING_COMMENT_BACKTICK = "const x = 1; // check the note\n/`/.test(s);\nconst after = 1;\n";
	private const string _REGEX_AFTER_A_BLOCK_COMMENT_BLOCK_OPENER = "const x = 1; /* note */ /[/*]/.test(s);\nconst after = 1;\n";
	private const string _CLASS_REGEX_AFTER_RETURN_AND_EQUALS = "function f(s) { return /[/]+/.test(s); }\nconst r = /[/]+/;\nconst after = 1;\n";
	private const string _DIVISION_CHAINS = "const r = a / b / c / d / e;\nconst s = (a + b) / 2 / 3;\nconst t = a[0] / 'x'.length / `y`.length / 4;\nconst u = a++ / b-- / c;\nconst after = 1;\n";
	private const string _JSX_CLOSING_TAG = "const el = <a href={u}>go</a>;\nconst after = 1;\n";
	private const string _JSX_SELF_CLOSING_AFTER_A_HOLE = "const el = <Img src={u} />;\nconst el2 = <Img src={u}/>;\nconst el3 = <br />;\nconst after = 1;\n";
	private const string _JSX_APOSTROPHE = "const el = <p>It's text</p>;\nconst after = 1;\n";
	private const string _JSX_QUOTE_IN_AN_ATTRIBUTE = "const el = <a title=\"it's\" href='x'>go</a>;\nconst after = 1;\n";
	private const string _JSX_BACKTICK = "const el = <p>Use ` here</p>;\nconst after = 1;\n";
	private const string _JSX_BLOCK_COMMENT_OPENER = "const el = <p>a /* b</p>;\nconst after = 1;\n";
	private const string _JSX_LINE_COMMENT_OPENER = "const el = <a href=\"x\">http://host.test/path</a>;\nconst after = 1;\n";
	private const string _HTML_LIKE_COMMENTS = "const a = x<!--y;\nwhile (n --> 0) { n--; }\n--> text here\nconst after = 1;\n";
	private const string _CRLF = "const a = 'x \\\r\nnext'; // c\r\nb();\r\n";
	private const string _SEPARATORS = "const a = 'x\u2028y';\n// note\u2028hidden();\nconst after = 1;\n";
	private const string _ESCAPED_CONSTANT_DECLARATIONS = "const a = 'it\\'s';\nlet b = \"say \\\"hi\\\"\";\nvar c = `x\\ny`;\nconst d = 'a\\\\';\nvar e = `a$b`;\n";
	private const string _EXECUTED_STRINGS = "eval(\"fetch(u, { headers: { Authorization: 'Basic ' + t } })\");\nconst f = new Function(\"return atob(s) // x\");\nexec(\"curl -u user:pass https://host.test/\");\nconst after = 1;\n";
	private const string _EXPORTED_CONSTANT_DECLARATIONS = "export const a = 'one';\nexport let b = \"two\";\nexport var c = `three`;\nexport d = 'four';\n";
	private const string _SAME_LINE_CONSTANT_DECLARATIONS = "{ let a = 'one'; }\na = 'two';\nif (x) { const b = 'one'; }\nconst c = 'one'; const d = 'two';\n";
	private const string _CONSTANT_DECLARATIONS = "const a = 'x';\nlet b = \"y\";\nvar c = `z`;\nd = 'w';\nlet e = 'one';\ne = 'two';\nlet k = 'one';\nk = compute();\nf = g();\nconst h = 'a' + b;\nconst m = `z${y}`;\n/*\nconst q = 'c';\n*/\nconst after = 1;\n";
	private const string _SAME_LINE_HEADER_AND_ENCODER = "axios.post(u, d, { headers: { Authorization: 'Basic ' + btoa(user + ':' + pass) } });\nconst after = 1;\n";
	private const string _SAME_LINE_QUOTED_KEY_AND_BUFFER = "fetch(u, { headers: { 'Authorization': 'Basic ' + Buffer.from(a + ':' + b).toString('base64') } });\nconst after = 1;\n";
	private const string _SAME_LINE_OPTION_PAIR_AND_HEADER = "$.ajax({ url: u, username: a, password: b, beforeSend: x => x.setRequestHeader('Authorization', 'Basic ' + btoa(a + ':' + b)) });\nconst after = 1;\n";

	/// <summary>The rows of the backward predicate: the text before the slash, and whether the slash opens a regular expression (true) or divides (false).</summary>
	private static readonly (string Id, string Prefix, bool Opens)[] _predicateRows =
	[
		("P01-start-of-text", "", true),
		("P02-opening-parenthesis", "f(", true),
		("P03-comma", "f(a, ", true),
		("P04-equals", "x = ", true),
		("P05-colon", "{ a: ", true),
		("P06-opening-bracket", "[ ", true),
		("P07-exclamation", "!", true),
		("P08-ampersand", "a && ", true),
		("P09-vertical-bar", "a || ", true),
		("P10-question-mark", "a ? ", true),
		("P11-opening-brace", "{ ", true),
		("P12-semicolon", "a; ", true),
		("P13-plus", "a + ", true),
		("P14-minus", "a - ", true),
		("P15-asterisk", "a * ", true),
		("P16-percent", "a % ", true),
		("P17-caret", "a ^ ", true),
		("P18-tilde", "~", true),
		("P19-greater-than", "a > ", true),
		("P20-arrow", "f = x => ", true),
		("P21-less-than-then-blank", "a < ", true),
		("P22-less-than-directly-before-slash", "a<", false),
		("P23-closing-parenthesis", "f(a) ", false),
		("P24-closing-bracket", "a[0] ", false),
		("P25-closing-brace", "{} ", false),
		("P26-postfix-plus-plus", "a++ ", false),
		("P27-postfix-decrement", "a-- ", false),
		("P28-end-of-a-regex", "x = /a/ ", false),
		("P29-digit", "x = 1 ", false),
		("P30-identifier", "x = a ", false),
		("P31-string", "x = 'a' ", false),
		("P32-template", "x = `a` ", false),
		("P33-dollar", "x = $ ", false),
		("P34-underscore", "x = _ ", false),
		("P35-plus-after-blank", "a + +", true),
		("P36-return", "return ", true),
		("P37-typeof", "typeof ", true),
		("P38-instanceof", "a instanceof ", true),
		("P39-in", "a in ", true),
		("P40-of", "a of ", true),
		("P41-new", "new ", true),
		("P42-delete", "delete ", true),
		("P43-void", "void ", true),
		("P44-throw", "throw ", true),
		("P45-case", "case ", true),
		("P46-do", "do ", true),
		("P47-else", "else ", true),
		("P48-yield", "yield ", true),
		("P49-await", "await ", true),
		("P50-keyword-then-line-feed", "return\n", true),
		("P51-operator-then-line-feeds", "x =\n\n\t  ", true),
		("P52-word-that-contains-a-keyword", "within ", false),
		("P53-word-that-starts-with-a-keyword", "returned ", false),
		("P54-keyword-after-a-dot", "a.return ", false),
		("P55-in-after-a-dot", "a.in ", false),
		("P56-of-after-optional-chaining", "a?.of ", false),
		("P57-await-after-a-dot", "a.await ", false),
		("P58-unlisted-dot", "x = 1. ", true)
	];

	public static IEnumerable<object[]> PredicateRows => _predicateRows.Select(row => new object[] { row.Id, row.Prefix, row.Opens });

	/// <summary>Fixture F01. <c>//</c> runs to the line feed, exclusive, and a quote inside it is comment text.</summary>
	[Fact]
	public void ALineCommentRunsToTheLineFeedAndAQuoteInsideItIsCommentText()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_LINE_COMMENT);

		AssertValid(mask, _LINE_COMMENT);
		ZoneAssertions.SpansZone(mask, _LINE_COMMENT, "// note it's here", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _LINE_COMMENT, "const b = 2;", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, _LINE_COMMENT, LexicalZone.StringBody);
	}

	/// <summary>Fixture F02. <c>/* */</c> spans lines, and quotes and a backtick inside it are comment text.</summary>
	[Fact]
	public void ABlockCommentSpansLinesAndQuotesInsideItAreCommentText()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_BLOCK_COMMENT);

		AssertValid(mask, _BLOCK_COMMENT);
		ZoneAssertions.SpansZone(mask, _BLOCK_COMMENT, "/* multi\nline ' \" ` */", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _BLOCK_COMMENT, "a();", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _BLOCK_COMMENT, " b();", LexicalZone.Code);
	}

	/// <summary>Fixture F03. The hashbang at position zero is a line comment.</summary>
	[Fact]
	public void TheHashbangAtPositionZeroIsALineComment()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_HASHBANG_AT_ZERO);

		AssertValid(mask, _HASHBANG_AT_ZERO);
		ZoneAssertions.SpansZone(mask, _HASHBANG_AT_ZERO, _HASHBANG_LINE, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _HASHBANG_AT_ZERO, "use strict", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _HASHBANG_AT_ZERO, "const b = 2;", LexicalZone.Code);
	}

	/// <summary>Fixture F04 and F04b. A <c>#!</c> that is not at position zero is code, whether it is on a later line or after a leading blank line.</summary>
	[Fact]
	public void AHashAndBangThatAreNotAtPositionZeroAreCode()
	{
		ZoneMask later = JavaScriptShapedProfile.Scan(_HASH_BANG_ELSEWHERE);
		ZoneMask afterBlankLine = JavaScriptShapedProfile.Scan(_HASH_BANG_AFTER_A_BLANK_LINE);

		AssertValid(later, _HASH_BANG_ELSEWHERE);
		ZoneAssertions.SpansZone(later, _HASH_BANG_ELSEWHERE, "#!b ok;", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(later, _HASH_BANG_ELSEWHERE, LexicalZone.Comment);
		AssertValid(afterBlankLine, _HASH_BANG_AFTER_A_BLANK_LINE);
		ZoneAssertions.SpansZone(afterBlankLine, _HASH_BANG_AFTER_A_BLANK_LINE, "#!", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(afterBlankLine, _HASH_BANG_AFTER_A_BLANK_LINE, LexicalZone.Comment);
	}

	/// <summary>
	/// Fixture F05. A UTF-8 mark followed by a hashbang, through the decoder path: the decoder removes the mark, so the
	/// scanner's position zero is the first character of the file and the hashbang is a comment. The contrast shows why the path matters: a mark that
	/// reached the scanner would stand at position zero and push the hashbang to position one, where it is code.
	/// </summary>
	[Fact]
	public void AUtf8MarkFollowedByAHashbangIsDecodedSoTheHashbangStandsAtPositionZero()
	{
		byte[] bytes = [_UTF8_MARK_FIRST, _UTF8_MARK_SECOND, _UTF8_MARK_THIRD, .. Encoding.UTF8.GetBytes(_HASHBANG_AT_ZERO)];

		bool decoded = FileDecoding.TryDecode(bytes, out DecodedContent content);
		string text = TextNormalization.Normalize(content.Text);
		ZoneMask mask = JavaScriptShapedProfile.Scan(text);
		string markedText = $"\uFEFF{_HASHBANG_AT_ZERO}";
		ZoneMask markedMask = JavaScriptShapedProfile.Scan(markedText);

		Assert.True(decoded);
		Assert.Equal('#', text[0]);
		AssertValid(mask, text);
		ZoneAssertions.SpansZone(mask, text, _HASHBANG_LINE, LexicalZone.Comment);
		ZoneAssertions.SpansZone(markedMask, markedText, "#!", LexicalZone.Code);
	}

	/// <summary>Fixture F06. A private name <c>#field</c> opens nothing: no comment, no string, no regular expression.</summary>
	[Fact]
	public void APrivateFieldOpensNothing()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_PRIVATE_FIELD);

		AssertValid(mask, _PRIVATE_FIELD);
		ZoneAssertions.NoPositionCarries(mask, _PRIVATE_FIELD, LexicalZone.Comment);
		ZoneAssertions.NoPositionCarries(mask, _PRIVATE_FIELD, LexicalZone.StringBody);
		ZoneAssertions.NoPositionCarries(mask, _PRIVATE_FIELD, LexicalZone.Delimiter);
	}

	/// <summary>Fixture F07. Single and double quotes, each with its escaped quote inside the body.</summary>
	[Fact]
	public void BothQuoteKindsReadTheirEscapedQuoteAsBody()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_QUOTES);

		AssertValid(mask, _QUOTES);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _QUOTES, "it\\'s", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _QUOTES, "say \\\"hi\\\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _QUOTES, "x", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _QUOTES, "const c = ", LexicalZone.Code);
	}

	/// <summary>Fixture F08. A backslash before a line feed continues the string, in both quote kinds; it is not an anomaly.</summary>
	[Fact]
	public void ABackslashNewlineContinuesAStringAndIsNoAnomaly()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_CONTINUATION);

		AssertValid(mask, _CONTINUATION);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _CONTINUATION, "one \\\ntwo", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _CONTINUATION, "three \\\nfour", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _CONTINUATION, "const c = 2;", LexicalZone.Code);
	}

	/// <summary>Fixture F09. A raw line feed in a string ends it and is a recovered anomaly: counted on the mask, the file stays completely valid and the next line is code.</summary>
	[Fact]
	public void ARawNewlineInAStringIsARecoveredAnomalyAndTheNextLineIsCode()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_RAW_NEWLINE_IN_A_STRING);

		AssertValid(mask, _RAW_NEWLINE_IN_A_STRING);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		Assert.Equal(LexicalAnomalyReason.NewlineInsideALiteral, mask.AnomalyReason);
		ZoneAssertions.SpansZone(mask, _RAW_NEWLINE_IN_A_STRING, "open", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _RAW_NEWLINE_IN_A_STRING, "const b = 2;", LexicalZone.Code);
	}

	/// <summary>
	/// Fixture F10. A template nested three deep, with a closing brace and a backtick inside the strings of its holes: neither closes anything. The
	/// middle layers read as body, the hole contents as code.
	/// </summary>
	[Fact]
	public void ATemplateNestedThreeDeepReadsAClosingBraceAndABacktickInAHolesStringAsBody()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_TEMPLATE_DEPTH_THREE_SOURCE);

		AssertValid(mask, _TEMPLATE_DEPTH_THREE_SOURCE);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_DEPTH_THREE_SOURCE, "}`", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_DEPTH_THREE_SOURCE, "`}", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_DEPTH_THREE_SOURCE, "c ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_DEPTH_THREE_SOURCE, " h ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_DEPTH_THREE_SOURCE, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.Equal(_TEMPLATE_DEPTH_THREE, CountTemplateOpeningsIn(_TEMPLATE_DEPTH_THREE_SOURCE));
	}

	/// <summary>Fixture F11. Eight open holes, the bound: the file is read completely, the innermost hole is code and a closing brace and a backtick in its string are body.</summary>
	[Fact]
	public void ATemplateEightHolesDeepIsReadCompletely()
	{
		string source = $"const t = {NestedTemplate(_TEMPLATE_DEPTH_EIGHT, _NESTED_INNERMOST)};\n{_FOLLOWING_STATEMENT}\n";

		ZoneMask mask = JavaScriptShapedProfile.Scan(source);

		AssertValid(mask, source);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, source, "f(", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "}`", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F12. An object literal in a hole, with a closing brace in its strings and a nested object and template: the hole closes at depth zero.</summary>
	[Fact]
	public void AnObjectLiteralInAHoleClosesTheHoleOnlyAtDepthZero()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_OBJECT_IN_A_HOLE);

		AssertValid(mask, _OBJECT_IN_A_HOLE);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, _OBJECT_IN_A_HOLE, ").a ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _OBJECT_IN_A_HOLE, " end", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _OBJECT_IN_A_HOLE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F13. A ninth open hole is refused before the descent: the file is abandoned as InterpolationDepthExceeded, a declared cost. The mask is valid up to the outermost construct that was being read.</summary>
	[Fact]
	public void ATemplateNineHolesDeepAbandonsTheFileAsInterpolationDepthExceeded()
	{
		string source = $"const t = {NestedTemplate(_TEMPLATE_DEPTH_NINE, _NESTED_INNERMOST)};\n{_FOLLOWING_STATEMENT}\n";

		ZoneMask mask = JavaScriptShapedProfile.Scan(source);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.InterpolationDepthExceeded, mask.AnomalyReason);
		Assert.Equal(source.IndexOf(_BACKTICK, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>Fixture F12b. A hole's code region reads its own comments and regular expressions: a closing brace and a backtick inside a block comment, a regular expression with both in a class, and a closing brace in a line comment close nothing.</summary>
	[Fact]
	public void ACommentAndARegularExpressionInAHoleHideTheirClosingBraceAndBacktick()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_COMMENT_AND_REGEX_IN_A_HOLE);

		AssertValid(mask, _COMMENT_AND_REGEX_IN_A_HOLE);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, _COMMENT_AND_REGEX_IN_A_HOLE, "/* } ` */", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _COMMENT_AND_REGEX_IN_A_HOLE, "[}`]", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _COMMENT_AND_REGEX_IN_A_HOLE, "// }", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _COMMENT_AND_REGEX_IN_A_HOLE, " b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _COMMENT_AND_REGEX_IN_A_HOLE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F14. String.raw and a tagged template are ordinary templates: an escaped dollar-brace is text, an escaped backtick does not close, a hole is code.</summary>
	[Fact]
	public void ARawOrTaggedTemplateEndsOnlyAtAnUnescapedBacktickAndAnEscapedDollarBraceIsText()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_RAW_AND_TAGGED_TEMPLATES);

		AssertValid(mask, _RAW_AND_TAGGED_TEMPLATES);
		ZoneAssertions.SpansZone(mask, _RAW_AND_TAGGED_TEMPLATES, "C:\\new\\${name}", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _RAW_AND_TAGGED_TEMPLATES, "a ", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _RAW_AND_TAGGED_TEMPLATES, " c", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _RAW_AND_TAGGED_TEMPLATES, "x\\`y\\${z}", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _RAW_AND_TAGGED_TEMPLATES, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F15. A regular expression with a slash inside a character class: the class suspends the closing, so the literal is one token.</summary>
	[Fact]
	public void ARegularExpressionWithASlashInACharacterClassIsOneLiteral()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_SLASH_IN_A_CLASS);

		AssertValid(mask, _SLASH_IN_A_CLASS);
		ZoneAssertions.SpansZone(mask, _SLASH_IN_A_CLASS, "[/]+\\/x", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _SLASH_IN_A_CLASS, "g.test(s);", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _SLASH_IN_A_CLASS, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F15b. A regular expression after <c>return</c> and after <c>=</c>, each with a slash inside its character class: both are one literal.</summary>
	[Fact]
	public void ARegularExpressionWithASlashInItsClassAfterReturnAndAfterEqualsIsOneLiteralEach()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_CLASS_REGEX_AFTER_RETURN_AND_EQUALS);

		AssertValid(mask, _CLASS_REGEX_AFTER_RETURN_AND_EQUALS);
		ZoneAssertions.SpansZone(mask, _CLASS_REGEX_AFTER_RETURN_AND_EQUALS, "[/]+", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _CLASS_REGEX_AFTER_RETURN_AND_EQUALS, "[/]+", LexicalZone.StringBody, occurrence: 1);
		ZoneAssertions.SpansZone(mask, _CLASS_REGEX_AFTER_RETURN_AND_EQUALS, ".test(s); }", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _CLASS_REGEX_AFTER_RETURN_AND_EQUALS, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F16. A regular expression with an escaped closing bracket inside its class: the escape keeps the class open, the next bracket closes it, the next slash ends the literal.</summary>
	[Fact]
	public void ARegularExpressionWithAnEscapedBracketInItsClassKeepsTheClassOpen()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_ESCAPED_BRACKET_IN_A_REGEX);

		AssertValid(mask, _ESCAPED_BRACKET_IN_A_REGEX);
		ZoneAssertions.SpansZone(mask, _ESCAPED_BRACKET_IN_A_REGEX, "[\\]/]+", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _ESCAPED_BRACKET_IN_A_REGEX, ".test(s);", LexicalZone.Code);
	}

	/// <summary>Fixtures P01 to P58. Every row of the backward predicate: the slash after the prefix is a regular-expression opening (a delimiter, then body) or a division (code), and the file stays completely valid.</summary>
	[Theory]
	[MemberData(nameof(PredicateRows))]
	public void TheBackwardPredicateOpensOrDividesAsItsRowSays(string id, string prefix, bool opens)
	{
		string source = $"{prefix}{_REGEX_CANDIDATE}";
		int slash = prefix.Length;

		ZoneMask mask = JavaScriptShapedProfile.Scan(source);

		AssertValid(mask, source);
		Assert.True(opens ? mask.ZoneAt(slash) == LexicalZone.Delimiter : mask.ZoneAt(slash) == LexicalZone.Code, $"{id}: {ZoneAssertions.Render(mask, source)}");
		Assert.Equal(opens ? LexicalZone.StringBody : LexicalZone.Code, mask.ZoneAt(slash + _FIRST_CHARACTER_AFTER_THE_SLASH));
		Assert.Equal(opens ? LexicalZone.StringBody : LexicalZone.Code, mask.ZoneAt(slash + _SECOND_CHARACTER_AFTER_THE_SLASH));
	}

	/// <summary>Fixture P59. A whitespace gap just under the ceiling is read through to the character before it, so a division stays a division.</summary>
	[Fact]
	public void AWhitespaceGapJustUnderTheCeilingIsReadThroughToTheCharacterBeforeIt()
	{
		string source = $"a{new string('\n', _WHITESPACE_GAP_BELOW_THE_CEILING)}{_REGEX_CANDIDATE}";

		ZoneMask mask = JavaScriptShapedProfile.Scan(source);

		AssertValid(mask, source);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf(_SLASH, StringComparison.Ordinal)));
	}

	/// <summary>Fixture P60. A whitespace gap that reaches the ceiling answers "opens", whatever stands before it (declared; it bounds the predicate's cost).</summary>
	[Fact]
	public void AWhitespaceGapThatReachesTheCeilingAnswersOpens()
	{
		string source = $"a{new string('\n', _WHITESPACE_GAP_AT_THE_CEILING)}{_REGEX_CANDIDATE}";

		ZoneMask mask = JavaScriptShapedProfile.Scan(source);

		AssertValid(mask, source);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf(_SLASH, StringComparison.Ordinal)));
	}

	/// <summary>Fixture F17. DECLARED COST, regex after a trailing line comment: the predicate reads the comment's last letter and divides, so the regular expression is read as code. Harmless unless its body holds a quote.</summary>
	[Fact]
	public void ARegexAfterATrailingLineCommentIsReadAsDivisionAndItsBodyAsCode()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_REGEX_AFTER_A_TRAILING_COMMENT);

		AssertValid(mask, _REGEX_AFTER_A_TRAILING_COMMENT);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_A_TRAILING_COMMENT, "// check the trailing", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_A_TRAILING_COMMENT, "/re/.test(s);", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_A_TRAILING_COMMENT, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F17b. The same declared cost with a quote in the regular expression: the quote opens a string that a raw newline ends, a recovered anomaly, and the next line is code.</summary>
	[Fact]
	public void AQuoteInARegexAfterATrailingLineCommentOpensAStringThatTheLineEndRecovers()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_REGEX_AFTER_A_TRAILING_COMMENT_DAMAGE);

		AssertValid(mask, _REGEX_AFTER_A_TRAILING_COMMENT_DAMAGE);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_A_TRAILING_COMMENT_DAMAGE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F18. DECLARED COST, regex after a block comment: the predicate reads the slash that ends the comment and divides.</summary>
	[Fact]
	public void ARegexAfterABlockCommentIsReadAsDivisionAndItsBodyAsCode()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_REGEX_AFTER_A_BLOCK_COMMENT);

		AssertValid(mask, _REGEX_AFTER_A_BLOCK_COMMENT);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_A_BLOCK_COMMENT, "/* note */", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_A_BLOCK_COMMENT, "/re/.test(s);", LexicalZone.Code);
	}

	/// <summary>Fixture F18b. The same declared cost with a quote in the regular expression: a recovered anomaly, and the next line is code.</summary>
	[Fact]
	public void AQuoteInARegexAfterABlockCommentOpensAStringThatTheLineEndRecovers()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_REGEX_AFTER_A_BLOCK_COMMENT_DAMAGE);

		AssertValid(mask, _REGEX_AFTER_A_BLOCK_COMMENT_DAMAGE);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_A_BLOCK_COMMENT_DAMAGE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F17c. The same declared cost with a backtick in the regular expression: the backtick opens a template that nothing closes, so the tail of the file is abandoned from it (the mechanism of F23).</summary>
	[Fact]
	public void ABacktickInARegexAfterATrailingLineCommentAbandonsTheTailOfTheFile()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_REGEX_AFTER_A_TRAILING_COMMENT_BACKTICK);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(_REGEX_AFTER_A_TRAILING_COMMENT_BACKTICK.IndexOf(_BACKTICK, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>Fixture F18c. The same declared cost with a block-comment opener in the regular expression's class: the opener starts a comment that nothing closes, so the tail of the file is abandoned from it (the mechanism of F24).</summary>
	[Fact]
	public void ABlockCommentOpenerInARegexAfterABlockCommentAbandonsTheTailOfTheFile()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_REGEX_AFTER_A_BLOCK_COMMENT_BLOCK_OPENER);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		Assert.Equal(_REGEX_AFTER_A_BLOCK_COMMENT_BLOCK_OPENER.LastIndexOf(_BLOCK_COMMENT_OPENING, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>Fixture F19. Division chains: every slash after an identifier, a digit, a closing bracket, a postfix operator or a string divides, and no regular expression is invented.</summary>
	[Fact]
	public void DivisionChainsAreCodeFromEndToEnd()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_DIVISION_CHAINS);

		AssertValid(mask, _DIVISION_CHAINS);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.NoPositionCarries(mask, _DIVISION_CHAINS, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _DIVISION_CHAINS, "a / b / c / d / e;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _DIVISION_CHAINS, "(a + b) / 2 / 3;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _DIVISION_CHAINS, ".length / ", LexicalZone.Code, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _DIVISION_CHAINS, "a++ / b-- / c;", LexicalZone.Code);
	}

	/// <summary>Fixture F20. A JSX closing tag: the slash after a less-than sign divides, so the tag is code and nothing is opened.</summary>
	[Fact]
	public void AJsxClosingTagIsCode()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_JSX_CLOSING_TAG);

		AssertValid(mask, _JSX_CLOSING_TAG);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _JSX_CLOSING_TAG, "go</a>;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _JSX_CLOSING_TAG, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F21b. A string that a program executes (an evaluated string, a function body, a command line) is string body whatever it holds: the scanner reads text, not behaviour.</summary>
	[Fact]
	public void AStringThatAProgramExecutesStaysStringBody()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_EXECUTED_STRINGS);

		AssertValid(mask, _EXECUTED_STRINGS);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _EXECUTED_STRINGS, "fetch(u, { headers: { Authorization: 'Basic ' + t } })", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _EXECUTED_STRINGS, "return atob(s) // x", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _EXECUTED_STRINGS, "curl -u user:pass https://host.test/", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _EXECUTED_STRINGS, "const after = 1;", LexicalZone.Code);
	}

	/// <summary>Fixture F21. A self-closing JSX tag after an expression hole, directly or after a blank, and after an attribute-free name: the slash divides (the closing-brace row).</summary>
	[Fact]
	public void ASelfClosingJsxTagAfterAnExpressionHoleIsCode()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_JSX_SELF_CLOSING_AFTER_A_HOLE);

		AssertValid(mask, _JSX_SELF_CLOSING_AFTER_A_HOLE);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.NoPositionCarries(mask, _JSX_SELF_CLOSING_AFTER_A_HOLE, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _JSX_SELF_CLOSING_AFTER_A_HOLE, "<Img src={u} />;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _JSX_SELF_CLOSING_AFTER_A_HOLE, "<Img src={u}/>;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _JSX_SELF_CLOSING_AFTER_A_HOLE, "<br />;", LexicalZone.Code);
	}

	/// <summary>Fixture F22. JSX HAZARD, recovered: an apostrophe in text opens a string that the line end recovers; the rest of that line is string body and the next line is code.</summary>
	[Fact]
	public void AnApostropheInJsxTextIsARecoveredAnomalyThatHidesTheRestOfItsLine()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_JSX_APOSTROPHE);

		AssertValid(mask, _JSX_APOSTROPHE);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _JSX_APOSTROPHE, "s text</p>;", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _JSX_APOSTROPHE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F22b. A quote inside a JSX attribute value is ordinary string text, balanced, and no anomaly.</summary>
	[Fact]
	public void AQuoteInsideAJsxAttributeValueIsOrdinaryStringBody()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_JSX_QUOTE_IN_AN_ATTRIBUTE);

		AssertValid(mask, _JSX_QUOTE_IN_AN_ATTRIBUTE);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _JSX_QUOTE_IN_AN_ATTRIBUTE, "it's", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _JSX_QUOTE_IN_AN_ATTRIBUTE, "go</a>;", LexicalZone.Code);
	}

	/// <summary>Fixture F23. JSX HAZARD, unrecoverable (declared cost): a backtick in text opens a template that no later backtick closes, so the tail of the file is abandoned from the backtick.</summary>
	[Fact]
	public void ABacktickInJsxTextAbandonsTheTailOfTheFile()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_JSX_BACKTICK);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(_JSX_BACKTICK.IndexOf(_BACKTICK, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>Fixture F24. JSX HAZARD, unrecoverable (declared cost): a block-comment opener in text opens a comment that nothing closes, so the tail of the file is abandoned from the opener.</summary>
	[Fact]
	public void ABlockCommentOpenerInJsxTextAbandonsTheTailOfTheFile()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_JSX_BLOCK_COMMENT_OPENER);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		Assert.Equal(_JSX_BLOCK_COMMENT_OPENER.IndexOf(_BLOCK_COMMENT_OPENING, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>Fixture F25. JSX HAZARD, recovered: two slashes in text (a URL) open a line comment to the end of the line, so the rest of that line is comment and the next line is code.</summary>
	[Fact]
	public void ADoubleSlashInJsxTextIsALineCommentToTheEndOfTheLine()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_JSX_LINE_COMMENT_OPENER);

		AssertValid(mask, _JSX_LINE_COMMENT_OPENER);
		ZoneAssertions.SpansZone(mask, _JSX_LINE_COMMENT_OPENER, "//host.test/path</a>;", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _JSX_LINE_COMMENT_OPENER, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture F26. HTML-like comments are NOT modelled (declared): <c>x&lt;!--y</c>, <c>n --&gt; 0</c> and a line that starts with <c>--&gt;</c> are code, so no position is a comment.</summary>
	[Fact]
	public void HtmlLikeCommentTextIsReadAsCode()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_HTML_LIKE_COMMENTS);

		AssertValid(mask, _HTML_LIKE_COMMENTS);
		ZoneAssertions.NoPositionCarries(mask, _HTML_LIKE_COMMENTS, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _HTML_LIKE_COMMENTS, "x<!--y;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _HTML_LIKE_COMMENTS, "while (n --> 0) { n--; }", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _HTML_LIKE_COMMENTS, "--> text here", LexicalZone.Code);
	}

	/// <summary>
	/// Fixture F27. CRLF: the reader normalizes line endings before the scanner, so a backslash before a CRLF continues a string and a line comment ends
	/// before the line feed. The contrast scans the un-normalized text, which is not a path the product takes: there the carriage return is the escaped
	/// character, the line feed is raw, the string ends in a recovered anomaly, and its closing quote then opens a second string (two recovered anomalies).
	/// </summary>
	[Fact]
	public void CrlfIsNormalizedBeforeTheScannerSoABackslashContinuesTheString()
	{
		string text = TextNormalization.Normalize(_CRLF);

		ZoneMask mask = JavaScriptShapedProfile.Scan(text);
		ZoneMask unnormalized = JavaScriptShapedProfile.Scan(_CRLF);

		Assert.DoesNotContain('\r', text);
		AssertValid(mask, text);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, text, "x \\\nnext", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, text, "// c", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, text, "b();", LexicalZone.Code);
		Assert.Equal(_UNNORMALIZED_CRLF_RECOVERED_LITERALS, unnormalized.NewlineTerminatedLiteralCount);
	}

	/// <summary>
	/// Fixture F28. U+2028 is not a line end for the scanner or the normalizer (declared): inside a string it is body, as ES2019 allows, and inside a
	/// line comment it does not end the comment, although JavaScript reads it as a line terminator, so code after it on the same line is read as comment.
	/// </summary>
	[Fact]
	public void TheLineSeparatorIsNotALineEndForTheScanner()
	{
		ZoneMask mask = JavaScriptShapedProfile.Scan(_SEPARATORS);

		Assert.Equal(_SEPARATORS, TextNormalization.Normalize(_SEPARATORS));
		AssertValid(mask, _SEPARATORS);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _SEPARATORS, "x\u2028y", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _SEPARATORS, "// note\u2028hidden();", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _SEPARATORS, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>
	/// Fixture F29. The constant declaration expression: const, let, var and a bare assignment of a literal are one declaration each; a rebinding makes
	/// the name indeterminate; a value that is not a literal, a template with a hole and a declaration inside a comment are not declarations. DECLARED
	/// RESIDUAL: a rebinding to a non-literal is not counted, so the name still resolves to its first literal.
	/// </summary>
	[Fact]
	public void TheConstantDeclarationExpressionCountsDeclarationsAndARebindingIsIndeterminate()
	{
		LexicalProfile profile = JavaScriptShapedProfile.Build();
		ZoneMask mask = JavaScriptShapedProfile.Scan(_CONSTANT_DECLARATIONS);

		FileSymbolTable symbols = FileSymbolTable.Collect(profile, _CONSTANT_DECLARATIONS, mask);

		AssertValid(mask, _CONSTANT_DECLARATIONS);
		Assert.True(symbols.TryResolveUnambiguously("a", _AFTER_EVERY_DECLARATION, out string constantValue));
		Assert.Equal("x", constantValue);
		Assert.True(symbols.TryResolveUnambiguously("b", _AFTER_EVERY_DECLARATION, out string letValue));
		Assert.Equal("y", letValue);
		Assert.True(symbols.TryResolveUnambiguously("c", _AFTER_EVERY_DECLARATION, out string varValue));
		Assert.Equal("z", varValue);
		Assert.True(symbols.TryResolveUnambiguously("d", _AFTER_EVERY_DECLARATION, out string bareValue));
		Assert.Equal("w", bareValue);
		Assert.Equal(2, symbols.DeclarationCountOf("e"));
		Assert.False(symbols.TryResolveUnambiguously("e", _AFTER_EVERY_DECLARATION, out _));
		Assert.Equal(1, symbols.DeclarationCountOf("k"));
		Assert.True(symbols.TryResolveUnambiguously("k", _AFTER_EVERY_DECLARATION, out string residualValue));
		Assert.Equal("one", residualValue);
		Assert.Equal(0, symbols.DeclarationCountOf("f"));
		Assert.Equal(0, symbols.DeclarationCountOf("h"));
		Assert.Equal(0, symbols.DeclarationCountOf("m"));
		Assert.Equal(0, symbols.DeclarationCountOf("q"));
	}

	/// <summary>Fixture F29b. A literal that holds an escaped character (an escaped quote, an escaped letter, an escaped backslash) is a declaration, and its value is the text between the quotes, escapes kept.</summary>
	[Fact]
	public void ALiteralWithAnEscapedCharacterIsCountedAsADeclaration()
	{
		LexicalProfile profile = JavaScriptShapedProfile.Build();
		ZoneMask mask = JavaScriptShapedProfile.Scan(_ESCAPED_CONSTANT_DECLARATIONS);

		FileSymbolTable symbols = FileSymbolTable.Collect(profile, _ESCAPED_CONSTANT_DECLARATIONS, mask);

		AssertValid(mask, _ESCAPED_CONSTANT_DECLARATIONS);
		Assert.True(symbols.TryResolveUnambiguously("a", _AFTER_EVERY_DECLARATION, out string singleQuoted));
		Assert.Equal("it\\'s", singleQuoted);
		Assert.True(symbols.TryResolveUnambiguously("b", _AFTER_EVERY_DECLARATION, out string doubleQuoted));
		Assert.Equal("say \\\"hi\\\"", doubleQuoted);
		Assert.Equal(1, symbols.DeclarationCountOf("c"));
		Assert.Equal(1, symbols.DeclarationCountOf("d"));
		Assert.True(symbols.TryResolveUnambiguously("e", _AFTER_EVERY_DECLARATION, out string dollarValue));
		Assert.Equal("a$b", dollarValue);
	}

	/// <summary>Fixture F29c. A declaration after the keyword that exports it is counted as the same declaration without it.</summary>
	[Fact]
	public void AnExportedDeclarationIsCountedAsADeclaration()
	{
		LexicalProfile profile = JavaScriptShapedProfile.Build();
		ZoneMask mask = JavaScriptShapedProfile.Scan(_EXPORTED_CONSTANT_DECLARATIONS);

		FileSymbolTable symbols = FileSymbolTable.Collect(profile, _EXPORTED_CONSTANT_DECLARATIONS, mask);

		AssertValid(mask, _EXPORTED_CONSTANT_DECLARATIONS);
		Assert.True(symbols.TryResolveUnambiguously("a", _AFTER_EVERY_DECLARATION, out string constantValue));
		Assert.Equal("one", constantValue);
		Assert.True(symbols.TryResolveUnambiguously("b", _AFTER_EVERY_DECLARATION, out string letValue));
		Assert.Equal("two", letValue);
		Assert.True(symbols.TryResolveUnambiguously("c", _AFTER_EVERY_DECLARATION, out string varValue));
		Assert.Equal("three", varValue);
		Assert.Equal(1, symbols.DeclarationCountOf("d"));
	}

	/// <summary>
	/// Fixture F29d. DECLARED RESIDUAL: the expression is anchored at the line start, so a declaration that follows a brace or a semicolon on the
	/// same line is not counted. A name with one counted and one uncounted declaration therefore resolves to the counted value, which can be wrong.
	/// </summary>
	[Fact]
	public void ADeclarationAfterABraceOrASemicolonOnTheSameLineIsNotCounted()
	{
		LexicalProfile profile = JavaScriptShapedProfile.Build();
		ZoneMask mask = JavaScriptShapedProfile.Scan(_SAME_LINE_CONSTANT_DECLARATIONS);

		FileSymbolTable symbols = FileSymbolTable.Collect(profile, _SAME_LINE_CONSTANT_DECLARATIONS, mask);

		AssertValid(mask, _SAME_LINE_CONSTANT_DECLARATIONS);
		Assert.Equal(1, symbols.DeclarationCountOf("a"));
		Assert.True(symbols.TryResolveUnambiguously("a", _AFTER_EVERY_DECLARATION, out string wronglyUnambiguous));
		Assert.Equal("two", wronglyUnambiguous);
		Assert.Equal(0, symbols.DeclarationCountOf("b"));
		Assert.Equal(1, symbols.DeclarationCountOf("c"));
		Assert.Equal(0, symbols.DeclarationCountOf("d"));
	}

	/// <summary>
	/// Fixture F30. Same-line doubles: a header carrier and an encoder, a quoted-key header and a Buffer encoder, and an option pair with a header
	/// setter and an encoder each stand on ONE line, with every anchor in the code zone.
	/// </summary>
	[Fact]
	public void SameLineDoublesExistAndEveryAnchorOfThemIsInTheCodeZone()
	{
		ZoneMask header = JavaScriptShapedProfile.Scan(_SAME_LINE_HEADER_AND_ENCODER);
		ZoneMask quotedKey = JavaScriptShapedProfile.Scan(_SAME_LINE_QUOTED_KEY_AND_BUFFER);
		ZoneMask optionPair = JavaScriptShapedProfile.Scan(_SAME_LINE_OPTION_PAIR_AND_HEADER);

		AssertValid(header, _SAME_LINE_HEADER_AND_ENCODER);
		ZoneAssertions.SpansZone(header, _SAME_LINE_HEADER_AND_ENCODER, "headers", LexicalZone.Code);
		ZoneAssertions.SpansZone(header, _SAME_LINE_HEADER_AND_ENCODER, "btoa(", LexicalZone.Code);
		AssertValid(quotedKey, _SAME_LINE_QUOTED_KEY_AND_BUFFER);
		ZoneAssertions.SpansZone(quotedKey, _SAME_LINE_QUOTED_KEY_AND_BUFFER, "headers", LexicalZone.Code);
		ZoneAssertions.SpansZone(quotedKey, _SAME_LINE_QUOTED_KEY_AND_BUFFER, "Authorization", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(quotedKey, _SAME_LINE_QUOTED_KEY_AND_BUFFER, "Buffer.from(", LexicalZone.Code);
		AssertValid(optionPair, _SAME_LINE_OPTION_PAIR_AND_HEADER);
		ZoneAssertions.SpansZone(optionPair, _SAME_LINE_OPTION_PAIR_AND_HEADER, "username: a, password: b", LexicalZone.Code);
		ZoneAssertions.SpansZone(optionPair, _SAME_LINE_OPTION_PAIR_AND_HEADER, "setRequestHeader(", LexicalZone.Code);
		ZoneAssertions.SpansZone(optionPair, _SAME_LINE_OPTION_PAIR_AND_HEADER, "btoa(", LexicalZone.Code);
	}

	/// <summary>
	/// THE DUMP. When <see cref="DUMP_VARIABLE_NAME"/> names a path, the zone mask of every fixture of this class (the long whitespace-gap fixtures
	/// excluded) is written to it, one block per fixture: its id, the mask's validity and anomaly, the text with control characters shown as a middle dot,
	/// and one glyph per position (. code, c comment, s string body, d delimiter). Without the variable the test does nothing and passes, as the other dump and census
	/// harnesses do. It is what a later change under <c>Lexical/</c> re-runs.
	/// </summary>
	[Fact]
	public void TheZoneMaskOfEveryFixtureIsDumpedWhenAnOutputPathIsNamed()
	{
		string? destination = Environment.GetEnvironmentVariable(DUMP_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(destination))
		{
			StringBuilder dump = new();

			foreach ((string id, string source) in Fixtures())
			{
				ZoneMask mask = JavaScriptShapedProfile.Scan(source);

				dump.AppendLine($"### {id}: valid-through {mask.ValidThroughPosition} of {mask.Length}; anomaly {mask.AnomalyReason}; newline-terminated {mask.NewlineTerminatedLiteralCount}; unbalanced {mask.UnbalancedNestingCount}");
				dump.Append(ZoneAssertions.Render(mask, source));
			}

			File.WriteAllText(destination, dump.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			Assert.True(File.Exists(destination));
		}
	}

	private static IEnumerable<(string Id, string Source)> Fixtures()
	{
		return NamedFixturesOfTheCommentsAndTheRegularExpressions()
			.Concat(NamedFixturesOfTheMarkupAndTheDeclarations())
			.Concat(_predicateRows.Select(row => (row.Id, $"{row.Prefix}{_REGEX_CANDIDATE}")))
			.Where(fixture => fixture.Item2.Length <= _MAXIMUM_DUMPED_FIXTURE_LENGTH);
	}

	private static (string Id, string Source)[] NamedFixturesOfTheCommentsAndTheRegularExpressions()
	{
		return
		[
			("F01-line-comment", _LINE_COMMENT),
			("F02-block-comment", _BLOCK_COMMENT),
			("F03-hashbang-at-zero", _HASHBANG_AT_ZERO),
			("F04-hash-bang-later", _HASH_BANG_ELSEWHERE),
			("F04b-hash-bang-after-a-blank-line", _HASH_BANG_AFTER_A_BLANK_LINE),
			("F05-mark-then-hashbang", DecodeMarkedHashbang()),
			("F05b-mark-reaching-the-scanner", $"\uFEFF{_HASHBANG_AT_ZERO}"),
			("F06-private-field", _PRIVATE_FIELD),
			("F07-quotes", _QUOTES),
			("F08-continuation", _CONTINUATION),
			("F09-raw-newline-in-a-string", _RAW_NEWLINE_IN_A_STRING),
			("F10-template-depth-3", _TEMPLATE_DEPTH_THREE_SOURCE),
			("F11-template-depth-8", $"const t = {NestedTemplate(_TEMPLATE_DEPTH_EIGHT, _NESTED_INNERMOST)};\n{_FOLLOWING_STATEMENT}\n"),
			("F12-object-in-a-hole", _OBJECT_IN_A_HOLE),
			("F13-template-depth-9", $"const t = {NestedTemplate(_TEMPLATE_DEPTH_NINE, _NESTED_INNERMOST)};\n{_FOLLOWING_STATEMENT}\n"),
			("F12b-comment-and-regex-in-a-hole", _COMMENT_AND_REGEX_IN_A_HOLE),
			("F14-raw-and-tagged-templates", _RAW_AND_TAGGED_TEMPLATES),
			("F15-slash-in-a-class", _SLASH_IN_A_CLASS),
			("F15b-class-regex-after-return-and-equals", _CLASS_REGEX_AFTER_RETURN_AND_EQUALS),
			("F16-escaped-bracket", _ESCAPED_BRACKET_IN_A_REGEX),
			("F17-regex-after-a-trailing-comment", _REGEX_AFTER_A_TRAILING_COMMENT),
			("F17b-trailing-comment-with-a-quote", _REGEX_AFTER_A_TRAILING_COMMENT_DAMAGE),
			("F17c-trailing-comment-with-a-backtick", _REGEX_AFTER_A_TRAILING_COMMENT_BACKTICK),
			("F18-regex-after-a-block-comment", _REGEX_AFTER_A_BLOCK_COMMENT),
			("F18c-block-comment-with-a-block-comment-opener", _REGEX_AFTER_A_BLOCK_COMMENT_BLOCK_OPENER),
			("F18b-block-comment-with-a-quote", _REGEX_AFTER_A_BLOCK_COMMENT_DAMAGE),
		];
	}

	private static (string Id, string Source)[] NamedFixturesOfTheMarkupAndTheDeclarations()
	{
		return
		[
			("F19-division-chains", _DIVISION_CHAINS),
			("F20-jsx-closing-tag", _JSX_CLOSING_TAG),
			("F21-jsx-self-closing-after-a-hole", _JSX_SELF_CLOSING_AFTER_A_HOLE),
			("F21b-executed-strings", _EXECUTED_STRINGS),
			("F22-jsx-apostrophe", _JSX_APOSTROPHE),
			("F22b-jsx-quote-in-an-attribute", _JSX_QUOTE_IN_AN_ATTRIBUTE),
			("F23-jsx-backtick", _JSX_BACKTICK),
			("F24-jsx-block-comment-opener", _JSX_BLOCK_COMMENT_OPENER),
			("F25-jsx-line-comment-opener", _JSX_LINE_COMMENT_OPENER),
			("F26-html-like-comments", _HTML_LIKE_COMMENTS),
			("F27-crlf-normalized", TextNormalization.Normalize(_CRLF)),
			("F27b-crlf-unnormalized", _CRLF),
			("F28-line-separator", _SEPARATORS),
			("F29-constant-declarations", _CONSTANT_DECLARATIONS),
			("F29b-escaped-constant-declarations", _ESCAPED_CONSTANT_DECLARATIONS),
			("F29c-exported-constant-declarations", _EXPORTED_CONSTANT_DECLARATIONS),
			("F29d-same-line-constant-declarations", _SAME_LINE_CONSTANT_DECLARATIONS),
			("F30a-same-line-header-and-encoder", _SAME_LINE_HEADER_AND_ENCODER),
			("F30b-same-line-quoted-key-and-buffer", _SAME_LINE_QUOTED_KEY_AND_BUFFER),
			("F30c-same-line-option-pair-and-header", _SAME_LINE_OPTION_PAIR_AND_HEADER)
		];
	}

	private static string DecodeMarkedHashbang()
	{
		byte[] bytes = [_UTF8_MARK_FIRST, _UTF8_MARK_SECOND, _UTF8_MARK_THIRD, .. Encoding.UTF8.GetBytes(_HASHBANG_AT_ZERO)];

		FileDecoding.TryDecode(bytes, out DecodedContent content);

		return TextNormalization.Normalize(content.Text);
	}

	/// <summary>A template whose hole holds a template, <paramref name="depth"/> holes deep, with the given code in the innermost hole.</summary>
	private static string NestedTemplate(int depth, string innermost)
	{
		return depth == 0 ? innermost : $"`a${{{NestedTemplate(depth - 1, innermost)}}}z`";
	}

	private static int CountTemplateOpeningsIn(string source)
	{
		return source.Split(_TEMPLATE_HOLE_OPENING, StringSplitOptions.None).Length - 1;
	}

	private static void AssertValid(ZoneMask mask, string source)
	{
		Assert.True(mask.IsCompletelyValid, $"The mask is valid only through {mask.ValidThroughPosition} ({mask.AnomalyReason}).{Environment.NewLine}{ZoneAssertions.Render(mask, source)}");
	}
}
