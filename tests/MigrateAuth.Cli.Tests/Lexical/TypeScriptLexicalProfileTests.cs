using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SHIPPED `TypeScript` LEXICAL PROFILE, run through the shipped scanner over the constructs the catalog names: a postfix non-null mark before a slash divides, a logical not before a
/// regular expression opens it, a line feed before the mark leaves it a logical not, a member access after the mark is code, and the residual forms the catalog declares recover at the line end.
/// Every fixture is invented from the language handbook, with generic names.
///
/// The profile the binary ships is also held to the profile the contract probe verified: every fixture below and every row of the probe's backward predicate must read the same on the two,
/// position by position, so that the shipped profile is the probed one.
/// </summary>
public sealed class TypeScriptLexicalProfileTests
{
	private const string _FOLLOWING_STATEMENT = "const after = 1;";
	private const string _REGEX_CANDIDATE = "/re/ 1;\n";
	private const int _AFTER_EVERY_DECLARATION = int.MaxValue;
	private const int _OPERAND_END_LINE_COUNT = 7;
	private const int _FIRST_CHARACTER_AFTER_THE_SLASH = 1;
	private const int _SECOND_CHARACTER_AFTER_THE_SLASH = 2;
	private const int _LONG_WORD_LENGTH = 9_000;
	private const int _QUOTE_RULE_COUNT = 4;

	private const string _NON_NULL_DIVISION = "const half = x!/2;\nconst after = 1;\n";
	private const string _NOT_BEFORE_A_REGEX = "const bad = !/re/.test(s);\nconst after = 1;\n";
	private const string _RETURN_SPACE_NOT_REGEX = "function f(s: string) { return !/re/.test(s); }\nconst after = 1;\n";
	private const string _RETURN_NO_SPACE_NOT_REGEX = "function f(s: string) { return!/re/.test(s); }\nconst after = 1;\n";
	private const string _LINE_FEED_BEFORE_A_BANG = "const a = x\n!/re/.test(s);\nconst after = 1;\n";
	private const string _NON_NULL_MEMBER = "const v = maybe!.x;\nconst w = maybe!['k'];\nconst z = fn!(1);\nconst after = 1;\n";
	private const string _SPACED_NON_NULL = "const q = a! / b!;\nconst chain = a!/b!/c;\nconst after = 1;\n";
	private const string _NON_NULL_AFTER_EVERY_OPERAND_END = "const a = (f)!/2;\nconst b = list[0]!/2;\nconst c = 'k'!/2;\nconst d = `t`!/2;\nconst e = 1!/2;\nconst f = o.p!/2;\nconst g = o?.q!/2;\nconst after = 1;\n";
	private const string _BLANK_BEFORE_A_BANG = "const a = x !/2;\nconst after = 1;\n";
	private const string _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH = "const a = x !/2; const b = c/3;\nconst after = 1;\n";
	private const string _BLANKS_AROUND_A_BANG = "const e = x ! / 2;\nconst after = 1;\n";
	private const string _BRACE_BEFORE_A_BANG = "const o = { a: 1 }!/2;\nconst after = 1;\n";
	private const string _DOUBLE_NON_NULL = "const d = x!!/2;\nconst after = 1;\n";
	private const string _TSX_BLANK_BANG_SLASH_IN_TEXT = "const el = <p>wow !/</p>;\nconst after = 1;\n";
	private const string _TSX_BANG_SLASH_AFTER_A_WORD_IN_TEXT = "const el = <p>wow!/</p>;\nconst after = 1;\n";
	private const string _HASHBANG_AT_ZERO = "#!/usr/bin/env ts-node\nconst b = 2;\n";
	private const string _HASHBANG_ELSEWHERE = "const a = 1;\n#!b ok;\nconst b = 2;\n";
	private const string _TYPED_CONSTANT_DECLARATIONS = "const a: string = 'x';\nexport declare const b: 'Basic' | 'Bearer' = 'Basic';\nclass C {\n\tprivate readonly c: string = 'y';\n\tstatic d = 'z';\n\te: number = compute();\n\tf = this.a;\n}\nlet g: (x: number) => string = 'no';\nconst h: Map<string, number> = 'w';\nconst after = 1;\n";

	private static readonly string[] _fixtures =
	[
		_NON_NULL_DIVISION, _NOT_BEFORE_A_REGEX, _RETURN_SPACE_NOT_REGEX, _RETURN_NO_SPACE_NOT_REGEX, _LINE_FEED_BEFORE_A_BANG, _NON_NULL_MEMBER, _SPACED_NON_NULL,
		_NON_NULL_AFTER_EVERY_OPERAND_END, _BLANK_BEFORE_A_BANG, _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH, _BLANKS_AROUND_A_BANG, _BRACE_BEFORE_A_BANG, _DOUBLE_NON_NULL,
		_TSX_BLANK_BANG_SLASH_IN_TEXT, _TSX_BANG_SLASH_AFTER_A_WORD_IN_TEXT, _HASHBANG_AT_ZERO, _HASHBANG_ELSEWHERE, _TYPED_CONSTANT_DECLARATIONS,
		"const t = `Basic ${string}`;\ntype H = `a${`b${C}`}`;\nconst after = 1;\n",
		"const r = f<T>(x) / 2;\nconst m: Map<string, number> = new Map<string, number>();\nconst after = 1;\n",
		"const s = <string>value;\nconst t = <string>value / 2;\nconst r = <RegExp>/re/;\nconst after = 1;\n",
		"const a = x as T / 2;\nconst c = x satisfies T / 2;\nconst after = 1;\n"
	];

	public static IEnumerable<object[]> Fixtures => _fixtures.Select(fixture => new object[] { fixture });

	[Fact]
	public void TheProfileDeclaresTheCommentRulesTheQuoteRulesAndTheCaseSwitchesOfJavaScriptAndANameOfItsOwn()
	{
		LexicalProfile profile = ShippedProfile();
		LexicalProfile javaScript = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;

		Assert.Equal(TypeScriptRegisteredExpressions.LANGUAGE_NAME, profile.ProfileName);
		Assert.Equal(["#!", "//"], profile.LineCommentRulesInEngineOrder.Select(rule => rule.Opening).Order(StringComparer.Ordinal));
		Assert.Equal("/*", Assert.Single(profile.BlockCommentRulesInEngineOrder).Opening);
		Assert.Equal(_QUOTE_RULE_COUNT, profile.QuoteRulesInEngineOrder.Length);
		Assert.Equal(
			javaScript.QuoteRulesInEngineOrder.Select(rule => (rule.Opening, rule.Closing, rule.NewlinePolicy)).Order().ToArray(),
			profile.QuoteRulesInEngineOrder.Select(rule => (rule.Opening, rule.Closing, rule.NewlinePolicy)).Order().ToArray());
		Assert.Null(profile.CodeEscapeCharacter);
		Assert.Null(profile.ArithmeticContext);
		Assert.Null(profile.ExecutedStrings);
		Assert.Equal(CaseSensitivity.Sensitive, profile.KeywordCaseSensitivity);
		Assert.Equal(CaseSensitivity.Sensitive, profile.DelimiterCaseSensitivity);
		Assert.Equal(IdentifierCaseSensitivityMode.Sensitive, profile.IdentifierCaseSensitivity);
	}

	[Fact]
	public void TheRegularExpressionRuleCarriesATypeScriptPredicateAndTheSuspendedBracketRange()
	{
		QuoteRule typeScript = ShippedProfile().QuoteRulesInEngineOrder.Single(rule => rule.Opening == "/");
		QuoteRule javaScript = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "/");

		Assert.NotNull(typeScript.OpeningContextPredicate);
		Assert.NotEqual(javaScript.OpeningContextPredicate, typeScript.OpeningContextPredicate);
		Assert.Equal(NewlinePolicy.Forbidden, typeScript.NewlinePolicy);
		Assert.Equal("[", typeScript.SuspendedSubRange!.Opening);
		Assert.Equal("]", typeScript.SuspendedSubRange.Closing);
	}

	// ------------------------------------------------------------------------------------------
	// The constructs of the lexical profile
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void APostfixNonNullMarkBeforeASlashDividesAndTheLineIsCodeFromEndToEnd()
	{
		ZoneMask mask = Scan(_NON_NULL_DIVISION);

		AssertValid(mask, _NON_NULL_DIVISION);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _NON_NULL_DIVISION, "x!/2;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _NON_NULL_DIVISION, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void TheMarkAfterEveryKindOfOperandEndLeavesTheSlashADivision()
	{
		ZoneMask mask = Scan(_NON_NULL_AFTER_EVERY_OPERAND_END);

		AssertValid(mask, _NON_NULL_AFTER_EVERY_OPERAND_END);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.NoPositionCarries(mask, _NON_NULL_AFTER_EVERY_OPERAND_END, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _NON_NULL_AFTER_EVERY_OPERAND_END, "(f)!/2;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _NON_NULL_AFTER_EVERY_OPERAND_END, "list[0]!/2;", LexicalZone.Code);
		for (int occurrence = 0; occurrence < _OPERAND_END_LINE_COUNT; occurrence++)
		{
			ZoneAssertions.SpansZone(mask, _NON_NULL_AFTER_EVERY_OPERAND_END, "!/2;", LexicalZone.Code, occurrence);
		}
		ZoneAssertions.SpansZone(mask, _NON_NULL_AFTER_EVERY_OPERAND_END, "o?.q!/2;", LexicalZone.Code);
	}

	[Fact]
	public void ALogicalNotBeforeARegularExpressionOpensTheLiteral()
	{
		ZoneMask mask = Scan(_NOT_BEFORE_A_REGEX);

		AssertValid(mask, _NOT_BEFORE_A_REGEX);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _NOT_BEFORE_A_REGEX, "re", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _NOT_BEFORE_A_REGEX, ".test(s);", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _NOT_BEFORE_A_REGEX, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Theory]
	[InlineData("function f(s: string) { return !/re/.test(s); }\nconst after = 1;\n")]
	[InlineData("function f(s: string) { return!/re/.test(s); }\nconst after = 1;\n")]
	public void AMarkAfterReturnWithOrWithoutABlankIsALogicalNotAndTheRegularExpressionOpens(string source)
	{
		ZoneMask mask = Scan(source);

		AssertValid(mask, source);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		int slash = source.IndexOf("/re/", StringComparison.Ordinal);

		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(slash));
		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(slash + _FIRST_CHARACTER_AFTER_THE_SLASH));
		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(slash + _SECOND_CHARACTER_AFTER_THE_SLASH));
		ZoneAssertions.SpansZone(mask, source, ".test(s); }", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void ALineFeedBeforeTheMarkMakesItALogicalNotAndTheRegularExpressionOpens()
	{
		ZoneMask mask = Scan(_LINE_FEED_BEFORE_A_BANG);

		AssertValid(mask, _LINE_FEED_BEFORE_A_BANG);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _LINE_FEED_BEFORE_A_BANG, "re", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _LINE_FEED_BEFORE_A_BANG, ".test(s);", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _LINE_FEED_BEFORE_A_BANG, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void AMarkBeforeAMemberAccessAnElementAccessOrACallOpensNothing()
	{
		ZoneMask mask = Scan(_NON_NULL_MEMBER);

		AssertValid(mask, _NON_NULL_MEMBER);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _NON_NULL_MEMBER, "maybe!.x;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _NON_NULL_MEMBER, "fn!(1);", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _NON_NULL_MEMBER, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void PostfixMarksAroundASpacedDivisionAndAChainOfTwoPostfixDivisionsAreCodeFromEndToEnd()
	{
		ZoneMask mask = Scan(_SPACED_NON_NULL);

		AssertValid(mask, _SPACED_NON_NULL);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.NoPositionCarries(mask, _SPACED_NON_NULL, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _SPACED_NON_NULL, "a! / b!;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _SPACED_NON_NULL, "a!/b!/c;", LexicalZone.Code);
	}

	[Fact]
	public void AMarkAfterALongWordOnALongLineStillDividesAndABlankBeforeItDoesNot()
	{
		string longWord = new('a', _LONG_WORD_LENGTH);
		string directly = $"{longWord}!{_REGEX_CANDIDATE}";
		string blank = $"{longWord} !{_REGEX_CANDIDATE}";

		ZoneMask directMask = Scan(directly);
		ZoneMask blankMask = Scan(blank);

		AssertValid(directMask, directly);
		Assert.Equal(LexicalZone.Code, directMask.ZoneAt(directly.IndexOf('/', StringComparison.Ordinal)));
		AssertValid(blankMask, blank);
		Assert.Equal(LexicalZone.Delimiter, blankMask.ZoneAt(blank.IndexOf('/', StringComparison.Ordinal)));
	}

	// ------------------------------------------------------------------------------------------
	// The declared residuals recover at the line end
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("const a = x !/2;\nconst after = 1;\n", 1)]
	[InlineData("const e = x ! / 2;\nconst after = 1;\n", 1)]
	[InlineData("const o = { a: 1 }!/2;\nconst after = 1;\n", 1)]
	[InlineData("const d = x!!/2;\nconst after = 1;\n", 1)]
	[InlineData("const el = <p>wow !/</p>;\nconst after = 1;\n", 0)]
	public void ADeclaredResidualReadsAsTheCatalogStatesAndTheNextStatementIsCode(string source, int recoveredLiteralCount)
	{
		ZoneMask mask = Scan(source);

		AssertValid(mask, source);
		Assert.Equal(recoveredLiteralCount, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void ABlankBeforeTheMarkWithALaterSlashOnTheLineReadsTheCodeBetweenTheTwoSlashesAsStringBodyWithNoAnomalyCounted()
	{
		ZoneMask mask = Scan(_BLANK_BEFORE_A_BANG_WITH_LATER_SLASH);

		AssertValid(mask, _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH, "2; const b = c", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void AMarkDirectlyAfterAWordInElementTextReadsCorrectly()
	{
		ZoneMask direct = Scan(_TSX_BANG_SLASH_AFTER_A_WORD_IN_TEXT);

		AssertValid(direct, _TSX_BANG_SLASH_AFTER_A_WORD_IN_TEXT);
		Assert.Equal(0, direct.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(direct, _TSX_BANG_SLASH_AFTER_A_WORD_IN_TEXT, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	// ------------------------------------------------------------------------------------------
	// The hashbang
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheHashbangAtPositionZeroIsACommentAndOneElsewhereIsCode()
	{
		ZoneMask atZero = Scan(_HASHBANG_AT_ZERO);
		ZoneMask elsewhere = Scan(_HASHBANG_ELSEWHERE);

		AssertValid(atZero, _HASHBANG_AT_ZERO);
		ZoneAssertions.SpansZone(atZero, _HASHBANG_AT_ZERO, "#!/usr/bin/env ts-node", LexicalZone.Comment);
		AssertValid(elsewhere, _HASHBANG_ELSEWHERE);
		ZoneAssertions.SpansZone(elsewhere, _HASHBANG_ELSEWHERE, "#!b ok;", LexicalZone.Code);
	}

	// ------------------------------------------------------------------------------------------
	// The shipped profile is the probed one
	// ------------------------------------------------------------------------------------------

	[Theory]
	[MemberData(nameof(Fixtures))]
	public void TheShippedProfileReadsEveryFixtureAsTheProbedProfileDoes(string fixture)
	{
		ZoneMask probed = TypeScriptShapedProfile.Scan(fixture);
		ZoneMask shipped = Scan(fixture);

		Assert.Equal(probed.Zones.ToArray(), shipped.Zones.ToArray());
		Assert.Equal(probed.NewlineTerminatedLiteralCount, shipped.NewlineTerminatedLiteralCount);
		Assert.Equal(probed.ValidThroughPosition, shipped.ValidThroughPosition);
	}

	[Theory]
	[MemberData(nameof(TypeScriptProbeFixtureTests.TypeScriptRows), MemberType = typeof(TypeScriptProbeFixtureTests))]
	public void TheShippedBackwardPredicateOpensOrDividesAsEveryNewRowOfTheProbeSays(string id, string prefix, bool opens)
	{
		AssertRow(id, prefix, opens);
	}

	[Theory]
	[MemberData(nameof(TypeScriptProbeFixtureTests.InheritedRows), MemberType = typeof(TypeScriptProbeFixtureTests))]
	public void TheShippedBackwardPredicateAnswersAsTheJavaScriptPredicateOnEveryInheritedRow(string id, string prefix, bool opens)
	{
		string source = $"{prefix}{_REGEX_CANDIDATE}";

		AssertRow(id, prefix, opens);
		Assert.Equal(JavaScriptShapedProfile.Scan(source).Zones.ToArray(), Scan(source).Zones.ToArray());
	}

	// ------------------------------------------------------------------------------------------
	// The constant declaration of the symbol table
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheTypedConstantDeclarationCountsAnnotatedConstantsAndFieldsWithALiteral()
	{
		LexicalProfile profile = ShippedProfile();
		ZoneMask mask = Scan(_TYPED_CONSTANT_DECLARATIONS);

		FileSymbolTable symbols = FileSymbolTable.Collect(profile, _TYPED_CONSTANT_DECLARATIONS, mask);

		Assert.True(symbols.TryResolveUnambiguously("a", _AFTER_EVERY_DECLARATION, out string annotated));
		Assert.Equal("x", annotated);
		Assert.True(symbols.TryResolveUnambiguously("b", _AFTER_EVERY_DECLARATION, out string modified));
		Assert.Equal("Basic", modified);
		Assert.True(symbols.TryResolveUnambiguously("c", _AFTER_EVERY_DECLARATION, out string privateField));
		Assert.Equal("y", privateField);
		Assert.True(symbols.TryResolveUnambiguously("d", _AFTER_EVERY_DECLARATION, out string staticField));
		Assert.Equal("z", staticField);
		Assert.Equal(0, symbols.DeclarationCountOf("e"));
		Assert.Equal(0, symbols.DeclarationCountOf("f"));
		Assert.Equal(0, symbols.DeclarationCountOf("g"));
		Assert.True(symbols.TryResolveUnambiguously("h", _AFTER_EVERY_DECLARATION, out string generic));
		Assert.Equal("w", generic);
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static LexicalProfile ShippedProfile()
	{
		return TypeScriptRegisteredExpressions.Analyzer.LexicalProfile;
	}

	private static ZoneMask Scan(string source)
	{
		return new LexicalScanner(ShippedProfile()).Scan(source);
	}

	private static void AssertRow(string id, string prefix, bool opens)
	{
		string source = $"{prefix}{_REGEX_CANDIDATE}";
		ZoneMask mask = Scan(source);
		int slash = prefix.Length;

		AssertValid(mask, source);
		Assert.True(opens ? mask.ZoneAt(slash) == LexicalZone.Delimiter : mask.ZoneAt(slash) == LexicalZone.Code, $"{id}: {ZoneAssertions.Render(mask, source)}");
		Assert.Equal(opens ? LexicalZone.StringBody : LexicalZone.Code, mask.ZoneAt(slash + _FIRST_CHARACTER_AFTER_THE_SLASH));
		Assert.Equal(opens ? LexicalZone.StringBody : LexicalZone.Code, mask.ZoneAt(slash + _SECOND_CHARACTER_AFTER_THE_SLASH));
	}

	private static void AssertValid(ZoneMask mask, string text)
	{
		Assert.True(mask.IsCompletelyValid, $"The mask is valid only through {mask.ValidThroughPosition} ({mask.AnomalyReason}).{Environment.NewLine}{ZoneAssertions.Render(mask, text)}");
	}
}
