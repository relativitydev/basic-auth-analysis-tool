using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SHIPPED `JavaScript` LEXICAL PROFILE, run through the shipped scanner over the constructs the catalog names: a template three deep with a closing brace and a backtick inside the
/// strings of its holes, a regular expression after <c>return</c> and after an equals sign with a slash in its character class, a division after a closing parenthesis, the
/// hashbang, a private name and a <c>.jsx</c> closing tag. Every fixture is invented from the ECMAScript specification, with generic names.
///
/// The profile the binary ships is also held to the profile the contract probe verified: every fixture of the probe and every row of its backward predicate must read the same on the
/// two, position by position, so that the shipped profile is the probed one.
/// </summary>
public sealed class JavaScriptLexicalProfileTests
{
	private const string _FOLLOWING_STATEMENT = "const after = 1;";
	private const string _HASHBANG_LINE = "#!/usr/bin/env node";
	private const byte _UTF8_MARK_FIRST = 0xEF;
	private const byte _UTF8_MARK_SECOND = 0xBB;
	private const byte _UTF8_MARK_THIRD = 0xBF;
	private const int _TEMPLATE_DEPTH_EIGHT = 8;
	private const int _TEMPLATE_DEPTH_NINE = 9;
	private const string _NESTED_INNERMOST = "f('}`')";

	private const string _TEMPLATE_THREE_DEEP = "const t = `a ${ f('}`') + `b ${ g(\"`}\") + `c ${ h } c` } b` } a`;\nconst after = 1;\n";
	private const string _REGEX_AFTER_RETURN_AND_EQUALS = "function f(s) { return /[/]+/.test(s); }\nconst r = /[/]+/;\nconst after = 1;\n";
	private const string _DIVISION_AFTER_A_PARENTHESIS = "const r = (a + b) / 2 / 3;\nconst after = 1;\n";
	private const string _HASHBANG_AT_ZERO = "#!/usr/bin/env node\n'use strict';\nconst b = 2;\n";
	private const string _HASHBANG_ELSEWHERE = "const a = 1;\n#!b ok;\nconst b = 2;\n";
	private const string _PRIVATE_FIELD = "class Counter {\n\t#count = 0;\n\tinc() { return this.#count + 1; }\n\tstatic has(o) { return #count in o; }\n}\n";
	private const string _JSX_CLOSING_TAG = "const el = <a href={u}>go</a>;\nconst after = 1;\n";
	private const string _JSX_SELF_CLOSING_AFTER_A_HOLE = "const el = <Img src={u} />;\nconst el2 = <Img src={u}/>;\nconst after = 1;\n";
	private const string _LINE_COMMENT = "const a = 1; // note it's here\nconst b = 2;\n";
	private const string _BLOCK_COMMENT = "a(); /* multi\nline ' \" ` */ b();\n";
	private const string _QUOTES = "const a = 'it\\'s';\nconst b = \"say \\\"hi\\\"\";\nconst c = 'x' + \"y\";\n";
	private const string _CONTINUATION = "const a = 'one \\\ntwo';\nconst b = \"three \\\nfour\";\nconst c = 2;\n";
	private const string _RAW_NEWLINE_IN_A_STRING = "const a = 'open\nconst b = 2;\n";
	private const string _OBJECT_IN_A_HOLE = "const t = `v ${ ({ a: '}', b: { c: `}` } }).a } end`;\nconst after = 1;\n";
	private const string _COMMENT_AND_REGEX_IN_A_HOLE = "const t = `a ${ x /* } ` */ + /[}`]/.source + y // }\n } b`;\nconst after = 1;\n";
	private const string _RAW_AND_TAGGED_TEMPLATES = "const p = String.raw`C:\\new\\${name}`;\nconst q = tag`a ${b} c`;\nconst e = `x\\`y\\${z}`;\nconst after = 1;\n";
	private const string _ESCAPED_BRACKET_IN_A_REGEX = "const r = /[\\]/]+/.test(s);\nconst after = 1;\n";
	private const string _REGEX_AFTER_A_TRAILING_COMMENT = "const x = 1; // check the trailing\n/re/.test(s);\nconst after = 1;\n";
	private const string _REGEX_AFTER_A_BLOCK_COMMENT = "const x = 1; /* note */ /re/.test(s);\nconst after = 1;\n";
	private const string _HTML_LIKE_COMMENTS = "const a = x<!--y;\nwhile (n --> 0) { n--; }\n--> text here\nconst after = 1;\n";
	private const string _JSX_APOSTROPHE = "const el = <p>It's text</p>;\nconst after = 1;\n";
	private const string _JSX_QUOTE_IN_AN_ATTRIBUTE = "const el = <a title=\"it's\" href='x'>go</a>;\nconst after = 1;\n";
	private const string _JSX_BACKTICK = "const el = <p>Use ` here</p>;\nconst after = 1;\n";
	private const string _JSX_BLOCK_COMMENT_OPENER = "const el = <p>a /* b</p>;\nconst after = 1;\n";
	private const string _CRLF = "const a = 'x \\\r\nnext'; // c\r\nb();\r\n";

	private static readonly string[] _probedFixtures =
	[
		_TEMPLATE_THREE_DEEP, _REGEX_AFTER_RETURN_AND_EQUALS, _DIVISION_AFTER_A_PARENTHESIS, _HASHBANG_AT_ZERO, _HASHBANG_ELSEWHERE, _PRIVATE_FIELD, _JSX_CLOSING_TAG,
		_JSX_SELF_CLOSING_AFTER_A_HOLE, _LINE_COMMENT, _BLOCK_COMMENT, _QUOTES, _CONTINUATION, _RAW_NEWLINE_IN_A_STRING, _OBJECT_IN_A_HOLE, _COMMENT_AND_REGEX_IN_A_HOLE,
		_RAW_AND_TAGGED_TEMPLATES, _ESCAPED_BRACKET_IN_A_REGEX, _REGEX_AFTER_A_TRAILING_COMMENT, _REGEX_AFTER_A_BLOCK_COMMENT, _HTML_LIKE_COMMENTS, _JSX_APOSTROPHE,
		_JSX_QUOTE_IN_AN_ATTRIBUTE, _JSX_BACKTICK, _JSX_BLOCK_COMMENT_OPENER, _CRLF
	];

	public static IEnumerable<object[]> ProbedFixtures => _probedFixtures.Select(fixture => new object[] { fixture });

	[Fact]
	public void TheProfileDeclaresTheConstructsOfTheCatalogAndNoOthers()
	{
		LexicalProfile profile = ShippedProfile();

		Assert.Equal(["#!", "//"], profile.LineCommentRulesInEngineOrder.Select(rule => rule.Opening).Order(StringComparer.Ordinal));
		Assert.Equal("/*", Assert.Single(profile.BlockCommentRulesInEngineOrder).Opening);
		Assert.Equal(["\"", "'", "/", "`"], profile.QuoteRulesInEngineOrder.Select(rule => rule.Opening).Order(StringComparer.Ordinal));
		Assert.Null(profile.CodeEscapeCharacter);
		Assert.Null(profile.ArithmeticContext);
		Assert.Null(profile.ExecutedStrings);
		Assert.Equal(CaseSensitivity.Sensitive, profile.KeywordCaseSensitivity);
		Assert.Equal(CaseSensitivity.Sensitive, profile.DelimiterCaseSensitivity);
		Assert.Equal(IdentifierCaseSensitivityMode.Sensitive, profile.IdentifierCaseSensitivity);
	}

	[Fact]
	public void TheRegularExpressionRuleIsTheFirstShippedUseOfASlashPredicateAndASuspendedSubRange()
	{
		QuoteRule regex = ShippedProfile().QuoteRulesInEngineOrder.Single(rule => rule.Opening == "/");

		Assert.NotNull(regex.OpeningContextPredicate);
		Assert.Equal(NewlinePolicy.Forbidden, regex.NewlinePolicy);
		Assert.Equal("[", regex.SuspendedSubRange!.Opening);
		Assert.Equal("]", regex.SuspendedSubRange.Closing);
	}

	[Fact]
	public void ATemplateThreeDeepWithAClosingBraceAndABacktickInAHolesStringsReadsThemAsBody()
	{
		ZoneMask mask = Scan(_TEMPLATE_THREE_DEEP);

		AssertValid(mask, _TEMPLATE_THREE_DEEP);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_THREE_DEEP, "}`", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_THREE_DEEP, "`}", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_THREE_DEEP, " h ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_THREE_DEEP, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void ATemplateEightDeepIsReadCompletelyAndOneNineDeepAbandonsTheFile()
	{
		string eight = $"const t = {NestedTemplate(_TEMPLATE_DEPTH_EIGHT)};\n{_FOLLOWING_STATEMENT}\n";
		string nine = $"const t = {NestedTemplate(_TEMPLATE_DEPTH_NINE)};\n{_FOLLOWING_STATEMENT}\n";

		AssertValid(Scan(eight), eight);
		Assert.Equal(LexicalAnomalyReason.InterpolationDepthExceeded, Scan(nine).AnomalyReason);
	}

	[Fact]
	public void ARegularExpressionAfterReturnAndAfterAnEqualsSignWithASlashInItsClassIsOneLiteralEach()
	{
		ZoneMask mask = Scan(_REGEX_AFTER_RETURN_AND_EQUALS);

		AssertValid(mask, _REGEX_AFTER_RETURN_AND_EQUALS);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_RETURN_AND_EQUALS, "[/]+", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_RETURN_AND_EQUALS, "[/]+", LexicalZone.StringBody, occurrence: 1);
		ZoneAssertions.SpansZone(mask, _REGEX_AFTER_RETURN_AND_EQUALS, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void ASlashAfterAClosingParenthesisIsADivisionAndLeavesTheRestOfTheLineCode()
	{
		ZoneMask mask = Scan(_DIVISION_AFTER_A_PARENTHESIS);

		AssertValid(mask, _DIVISION_AFTER_A_PARENTHESIS);
		ZoneAssertions.SpansZone(mask, _DIVISION_AFTER_A_PARENTHESIS, "(a + b) / 2 / 3;", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, _DIVISION_AFTER_A_PARENTHESIS, LexicalZone.StringBody);
	}

	[Fact]
	public void ARegularExpressionWithAnEscapedBracketInItsClassKeepsTheClassOpen()
	{
		ZoneMask mask = Scan(_ESCAPED_BRACKET_IN_A_REGEX);

		AssertValid(mask, _ESCAPED_BRACKET_IN_A_REGEX);
		ZoneAssertions.SpansZone(mask, _ESCAPED_BRACKET_IN_A_REGEX, "[\\]/]+", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _ESCAPED_BRACKET_IN_A_REGEX, ".test(s);", LexicalZone.Code);
	}

	[Fact]
	public void TheHashbangAtPositionZeroIsACommentAndOneElsewhereIsCode()
	{
		ZoneMask atZero = Scan(_HASHBANG_AT_ZERO);
		ZoneMask elsewhere = Scan(_HASHBANG_ELSEWHERE);

		ZoneAssertions.SpansZone(atZero, _HASHBANG_AT_ZERO, _HASHBANG_LINE, LexicalZone.Comment);
		ZoneAssertions.SpansZone(atZero, _HASHBANG_AT_ZERO, "const b = 2;", LexicalZone.Code);
		ZoneAssertions.SpansZone(elsewhere, _HASHBANG_ELSEWHERE, "#!b ok;", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(elsewhere, _HASHBANG_ELSEWHERE, LexicalZone.Comment);
	}

	/// <summary>The mark-plus-hashbang fixture: the decoder removes the mark, so the scanner's position zero is the first character of the file and the hashbang is a comment.</summary>
	[Fact]
	public void AUtf8MarkFollowedByAHashbangIsDecodedSoTheHashbangStandsAtPositionZero()
	{
		byte[] bytes = [_UTF8_MARK_FIRST, _UTF8_MARK_SECOND, _UTF8_MARK_THIRD, .. Encoding.UTF8.GetBytes(_HASHBANG_AT_ZERO)];

		bool decoded = FileDecoding.TryDecode(bytes, out DecodedContent content);
		string text = TextNormalization.Normalize(content.Text);
		ZoneMask mask = Scan(text);
		string markedText = $"\uFEFF{_HASHBANG_AT_ZERO}";

		Assert.True(decoded);
		Assert.Equal('#', text[0]);
		AssertValid(mask, text);
		ZoneAssertions.SpansZone(mask, text, _HASHBANG_LINE, LexicalZone.Comment);
		ZoneAssertions.SpansZone(Scan(markedText), markedText, "#!", LexicalZone.Code);
	}

	[Fact]
	public void APrivateNameOpensNothing()
	{
		ZoneMask mask = Scan(_PRIVATE_FIELD);

		AssertValid(mask, _PRIVATE_FIELD);
		ZoneAssertions.NoPositionCarries(mask, _PRIVATE_FIELD, LexicalZone.Comment);
		ZoneAssertions.NoPositionCarries(mask, _PRIVATE_FIELD, LexicalZone.StringBody);
		ZoneAssertions.NoPositionCarries(mask, _PRIVATE_FIELD, LexicalZone.Delimiter);
	}

	[Fact]
	public void AJsxClosingTagAndASelfClosingTagAfterAnExpressionHoleAreCodeAndNeverARegularExpression()
	{
		ZoneMask closing = Scan(_JSX_CLOSING_TAG);
		ZoneMask selfClosing = Scan(_JSX_SELF_CLOSING_AFTER_A_HOLE);

		AssertValid(closing, _JSX_CLOSING_TAG);
		ZoneAssertions.SpansZone(closing, _JSX_CLOSING_TAG, "</a>;", LexicalZone.Code);
		ZoneAssertions.SpansZone(closing, _JSX_CLOSING_TAG, _FOLLOWING_STATEMENT, LexicalZone.Code);
		AssertValid(selfClosing, _JSX_SELF_CLOSING_AFTER_A_HOLE);
		ZoneAssertions.SpansZone(selfClosing, _JSX_SELF_CLOSING_AFTER_A_HOLE, " />;", LexicalZone.Code);
		ZoneAssertions.SpansZone(selfClosing, _JSX_SELF_CLOSING_AFTER_A_HOLE, "/>;", LexicalZone.Code);
		ZoneAssertions.SpansZone(selfClosing, _JSX_SELF_CLOSING_AFTER_A_HOLE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void AnApostropheInJsxTextOpensAStringThatTheLineEndRecoversAndABacktickOrABlockCommentOpenerAbandonsTheTail()
	{
		ZoneMask apostrophe = Scan(_JSX_APOSTROPHE);

		AssertValid(apostrophe, _JSX_APOSTROPHE);
		Assert.Equal(1, apostrophe.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(apostrophe, _JSX_APOSTROPHE, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.False(Scan(_JSX_BACKTICK).IsCompletelyValid);
		Assert.False(Scan(_JSX_BLOCK_COMMENT_OPENER).IsCompletelyValid);
	}

	[Fact]
	public void HtmlLikeCommentsAreNotModelledSoTheirLinesAreCode()
	{
		ZoneMask mask = Scan(_HTML_LIKE_COMMENTS);

		AssertValid(mask, _HTML_LIKE_COMMENTS);
		ZoneAssertions.NoPositionCarries(mask, _HTML_LIKE_COMMENTS, LexicalZone.Comment);
	}

	[Fact]
	public void ARegexAfterATrailingLineCommentOrAfterABlockCommentIsADeclaredCostAndReadsAsDivisionAndCode()
	{
		ZoneMask trailing = Scan(_REGEX_AFTER_A_TRAILING_COMMENT);
		ZoneMask block = Scan(_REGEX_AFTER_A_BLOCK_COMMENT);

		ZoneAssertions.SpansZone(trailing, _REGEX_AFTER_A_TRAILING_COMMENT, "/re/.test(s);", LexicalZone.Code);
		ZoneAssertions.SpansZone(block, _REGEX_AFTER_A_BLOCK_COMMENT, "/re/.test(s);", LexicalZone.Code);
	}

	[Theory]
	[MemberData(nameof(ProbedFixtures))]
	public void TheShippedProfileReadsEveryProbedFixtureAsTheProbedProfileDoes(string fixture)
	{
		ZoneMask shipped = Scan(fixture);
		ZoneMask probed = JavaScriptShapedProfile.Scan(fixture);

		AssertSameMask(probed, shipped, fixture);
	}

	[Theory]
	[MemberData(nameof(JavaScriptProbeFixtureTests.PredicateRows), MemberType = typeof(JavaScriptProbeFixtureTests))]
	public void TheShippedBackwardPredicateOpensOrDividesAsEveryRowOfTheProbeSays(string id, string prefix, bool opens)
	{
		string source = $"{prefix}/re/ 1;\n";
		int slash = prefix.Length;

		ZoneMask mask = Scan(source);

		AssertValid(mask, source);
		Assert.True(opens ? mask.ZoneAt(slash) == LexicalZone.Delimiter : mask.ZoneAt(slash) == LexicalZone.Code, $"{id}: {ZoneAssertions.Render(mask, source)}");
	}

	[Fact]
	public void AWhitespaceGapAtTheCeilingAnswersOpensAndOneJustUnderItReadsThroughToTheCharacterBeforeIt()
	{
		string under = $"a{new string('\n', ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - 1)}/re/ 1;\n";
		string at = $"a{new string('\n', ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS)}/re/ 1;\n";

		Assert.Equal(LexicalZone.Code, Scan(under).ZoneAt(under.IndexOf('/', StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.Delimiter, Scan(at).ZoneAt(at.IndexOf('/', StringComparison.Ordinal)));
	}

	[Fact]
	public void AnEscapedLineFeedContinuesAStringAndARawOneEndsItAsARecoveredAnomaly()
	{
		ZoneMask continued = Scan(_CONTINUATION);
		ZoneMask raw = Scan(_RAW_NEWLINE_IN_A_STRING);

		Assert.Equal(0, continued.NewlineTerminatedLiteralCount);
		Assert.Equal(1, raw.NewlineTerminatedLiteralCount);
		AssertValid(raw, _RAW_NEWLINE_IN_A_STRING);
	}

	[Theory]
	[InlineData("const header = 'Basic ' + encoded;", "header", "Basic")]
	[InlineData("let mode = \"Bearer abc\";", "mode", "Bearer")]
	[InlineData("token = `Bearer`;", "token", "Bearer")]
	[InlineData("var plain = 'x';", "plain", "x")]
	[InlineData("export const exported = 'one';", "exported", "one")]
	public void TheConstantDeclarationCountsConstLetVarAndABareAssignmentWithTheLeadingWordOfTheLiteral(string line, string name, string value)
	{
		Match match = ShippedProfile().ConstantDeclarationExpression.Match(line);

		Assert.True(match.Success);
		Assert.Equal(name, match.Groups["name"].Value);
		Assert.Equal(value, match.Groups["value"].Value);
	}

	[Theory]
	[InlineData("const header = `Bearer ${token}`;")]
	[InlineData("const a = b;")]
	[InlineData("same === 'basic'")]
	public void TheConstantDeclarationDoesNotCountANonLiteralOrAComparison(string line)
	{
		Assert.DoesNotMatch(ShippedProfile().ConstantDeclarationExpression, line);
	}

	[Fact]
	public void TheConstantDeclarationCarriesThePolicyMatchBound()
	{
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, ShippedProfile().ConstantDeclarationExpression.MatchTimeout);
	}

	private static LexicalProfile ShippedProfile()
	{
		return JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;
	}

	private static ZoneMask Scan(string source)
	{
		return new LexicalScanner(ShippedProfile()).Scan(source);
	}

	private static string NestedTemplate(int depth)
	{
		return NestedTemplate(depth, _NESTED_INNERMOST);
	}

	private static string NestedTemplate(int depth, string innermost)
	{
		return depth == 0 ? innermost : $"`a${{{NestedTemplate(depth - 1, innermost)}}}z`";
	}

	private static void AssertValid(ZoneMask mask, string text)
	{
		Assert.True(mask.IsCompletelyValid, $"The mask is not valid through the text: {ZoneAssertions.Render(mask, text)}");
	}

	private static void AssertSameMask(ZoneMask expected, ZoneMask actual, string text)
	{
		Assert.Equal(expected.Length, actual.Length);
		Assert.Equal(expected.IsCompletelyValid, actual.IsCompletelyValid);
		Assert.Equal(expected.ValidThroughPosition, actual.ValidThroughPosition);
		Assert.Equal(expected.AnomalyReason, actual.AnomalyReason);
		Assert.Equal(expected.NewlineTerminatedLiteralCount, actual.NewlineTerminatedLiteralCount);
		Assert.True(expected.Zones.SequenceEqual(actual.Zones), $"The zones differ on [{text}]: {ZoneAssertions.Render(actual, text)}");
	}
}
