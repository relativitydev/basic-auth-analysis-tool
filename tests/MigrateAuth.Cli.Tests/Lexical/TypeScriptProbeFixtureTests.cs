using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE CONTRACT-PROBE FIXTURES OF THE TYPESCRIPT PROFILE. One synthetic fixture per construct, written from the language handbook with generic
/// names and held inline, run through the SHIPPED scanner with the test-local profile of <see cref="TypeScriptShapedProfile"/>, each asserting
/// the zone mask. A case that fails is a finding about a profile declaration or about the scanner. The dump test below writes every fixture's mask
/// to the path named by <see cref="DUMP_VARIABLE_NAME"/>, which is what a later change under <c>Lexical/</c> re-runs to compare.
///
/// Cases that RECORD a declared cost say so in their summary: the assertion pins what the scanner does today, so that a later change is a decision
/// and not an accident. Where a fixture shows why the postfix-assertion branch exists, the JavaScript-shaped profile is run on the same text for
/// contrast. Nothing here reads any file of a repository.
/// </summary>
public sealed class TypeScriptProbeFixtureTests
{
	internal const string DUMP_VARIABLE_NAME = "MIGRATE_AUTH_TS_PROBE_DUMP";

	private const string _FOLLOWING_STATEMENT = "const after = 1;";
	private const string _REGEX_CANDIDATE = "/re/ 1;\n";
	private const int _FIRST_CHARACTER_AFTER_THE_SLASH = 1;
	private const int _SECOND_CHARACTER_AFTER_THE_SLASH = 2;
	private const int _MAXIMUM_DUMPED_FIXTURE_LENGTH = 700;
	private const int _TEMPLATE_DEPTH_NINE = 9;
	private const int _AFTER_EVERY_DECLARATION = int.MaxValue;
	private const char _BACKTICK = '`';
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _NESTED_INNERMOST = "f('}`')";

	private const string _NON_NULL_DIVISION = "const half = x!/2;\nconst after = 1;\n";
	private const string _NOT_BEFORE_A_REGEX = "const bad = !/re/.test(s);\nconst after = 1;\n";
	private const string _SPACED_NON_NULL = "const q = a! / b!;\nconst chain = a!/b!/c;\nconst after = 1;\n";
	private const string _GENERIC_THEN_DIVISION = "const r = f<T>(x) / 2;\nconst m: Map<string, number> = new Map<string, number>();\nconst deep = g<Array<Map<K, V>>>(y) / 3;\nconst after = 1;\n";
	private const string _INSTANTIATION_EXPRESSION_THEN_DIVISION = "const q = id<number> / 2;\nconst after = 1;\n";
	private const string _TSX_ARROW_GENERIC = "const id = <T,>(x: T) => /re/.test(x);\nconst after = 1;\n";
	private const string _TSX_ARROW_GENERIC_WITH_ELEMENT = "const f = <T extends object>(x: T) => <div>{x}</div>;\nconst g = <T,>(x: T) => x;\nconst after = 1;\n";
	private const string _ANGLE_BRACKET_ASSERTION = "const s = <string>value;\nconst t = <string>value / 2;\nconst r = <RegExp>/re/;\nconst after = 1;\n";
	private const string _AS_ASSERTION_THEN_DIVISION = "const a = x as T / 2;\nconst b = x as unknown as number / 2;\nconst c = x satisfies T / 2;\nconst after = 1;\n";
	private const string _DECORATOR_THEN_REGEX = "@Component({ selector: 'app-box', template: `<p>{{ label }}</p>` })\nclass Box {\n\t@Input() pattern = /ab'c/;\n\t@Input({ required: true }) name!: string;\n\tmatch(s: string) { return /x[/]y/.test(s); }\n}\nconst after = 1;\n";
	private const string _DECORATOR_ARGUMENT_THEN_SLASH = "@Dec({ a: 1 })\n/re/.test(s);\nconst after = 1;\n";
	private const string _TEMPLATE_LITERAL_TYPES = "type Header = `Basic ${string}`;\ntype Pair = `${Prefix}-${number}`;\ntype Nested = `a${`b${C}`}`;\nconst after = 1;\n";
	private const string _TRIPLE_SLASH_DIRECTIVE = "/// <reference path=\"types.d.ts\" />\n/// <reference types=\"node\" />\nconst after = 1;\nconst x = 1; /// trailing\n";
	private const string _ENUM_AND_NAMESPACE = "enum Scheme {\n\tBasic = 'Basic',\n\tBearer = 'Bearer',\n\tMask = 1 << 3,\n\tHalf = 4 / 2\n}\nconst enum Flag { A = 1, B = A * 2 }\ndeclare enum Ambient { X }\nnamespace Auth {\n\texport const scheme = 'Basic';\n\texport function isBasic(s: string) { return /^Basic /.test(s); }\n\texport namespace Inner { export const x = 1 / 3; }\n}\ndeclare module 'client' {\n\texport interface Options { auth?: { username: string; password: string } }\n}\nconst after = 1;\n";
	private const string _TSX_RECOVERED_HAZARDS = "const View = <T,>(p: Props<T>) => (\n\t<div title=\"it's\">\n\t\t<p>It's text</p>\n\t\t<a href=\"x\">http://host.test/path</a>\n\t\t<Img src={u} />\n\t\t<List<string> items={xs} />\n\t\t{ok && <b>bold</b>}\n\t</div>\n);\nconst after = 1;\n";
	private const string _TSX_BACKTICK_IN_TEXT = "const el = <p>Use ` here</p>;\nconst after = 1;\n";
	private const string _TSX_BLOCK_COMMENT_OPENER_IN_TEXT = "const el = <p>a /* b</p>;\nconst after = 1;\n";
	private const string _TSX_BANG_SLASH_IN_TEXT = "const el = <p>wow!/</p>;\nconst el2 = <p>wow !/</p>;\nconst after = 1;\n";
	private const string _DECLARATION_FILE = "/// <reference types=\"node\" />\ndeclare module 'client' {\n\texport interface Credentials { username: string; password: string }\n\texport interface Request { auth?: Credentials; headers: { Authorization: 'Basic' | 'Bearer'; 'X-Id': `id-${number}` } }\n\texport function send<T = unknown>(url: string, init?: Request): Promise<T>;\n}\nexport declare const defaultScheme: 'Basic';\nexport declare function isBasic(value: string): value is 'Basic';\ndeclare global { interface Window { token?: string } }\nexport as namespace Client;\nexport {};\nconst after = 1;\n";
	private const string _RETURN_SPACE_NOT_REGEX = "function f(s: string) { return !/re/.test(s); }\nconst after = 1;\n";
	private const string _RETURN_NO_SPACE_NOT_REGEX = "function f(s: string) { return!/re/.test(s); }\nconst after = 1;\n";
	private const string _NON_NULL_MEMBER = "const v = maybe!.x;\nconst w = maybe!['k'];\nconst z = fn!(1);\nconst after = 1;\n";
	private const string _LINE_FEED_BEFORE_A_BANG = "const a = x\n!/re/.test(s);\nconst after = 1;\n";
	private const string _BLANK_BEFORE_A_BANG = "const a = x !/2;\nconst after = 1;\n";
	private const string _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH = "const a = x !/2; const b = c/3;\nconst after = 1;\n";
	private const string _BLANKS_AROUND_A_BANG = "const e = x ! / 2;\nconst after = 1;\n";
	private const string _BRACE_BEFORE_A_BANG = "const o = { a: 1 }!/2;\nconst after = 1;\n";
	private const string _DOUBLE_NON_NULL = "const d = x!!/2;\nconst after = 1;\n";
	private const string _TYPED_CONSTANT_DECLARATIONS = "const a: string = 'x';\nexport declare const b: 'Basic' | 'Bearer' = 'Basic';\nclass C {\n\tprivate readonly c: string = 'y';\n\tstatic d = 'z';\n\te: number = compute();\n\tf = this.a;\n}\nlet g: (x: number) => string = 'no';\nconst h: Map<string, number> = 'w';\nconst after = 1;\n";

	/// <summary>The rows of the backward predicate that the TypeScript-shaped predicate inherits unchanged: the text before the slash, and whether the slash opens a regular expression (true) or divides (false).</summary>
	private static readonly (string Id, string Prefix, bool Opens)[] _inheritedRows =
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

	/// <summary>The rows that are new for TypeScript: an exclamation mark directly before the slash (or a blank between), the characters that end an operand before the mark, and the words that join no keyword list.</summary>
	private static readonly (string Id, string Prefix, bool Opens)[] _typeScriptRows =
	[
		("TP01-postfix-after-identifier", "x!", false),
		("TP02-postfix-after-closing-parenthesis", "(a)!", false),
		("TP03-postfix-after-closing-bracket", "a[0]!", false),
		("TP04-postfix-after-single-quote", "'s'!", false),
		("TP05-postfix-after-double-quote", "\"s\"!", false),
		("TP06-postfix-after-backtick", "`t`!", false),
		("TP07-postfix-after-digit", "1!", false),
		("TP08-postfix-after-dollar", "$!", false),
		("TP09-postfix-after-underscore", "_!", false),
		("TP10-postfix-after-a-member", "a.b!", false),
		("TP11-postfix-after-optional-member", "a?.b!", false),
		("TP12-keyword-name-after-a-dot", "a.return!", false),
		("TP13-blank-before-the-bang", "x !", true),
		("TP14-line-feed-before-the-bang", "x\n!", true),
		("TP15-closing-brace-before-the-bang", "{}!", true),
		("TP16-bang-after-an-operator", "x = !", true),
		("TP17-bang-after-and", "a && !", true),
		("TP18-bang-after-an-opening-parenthesis", "(!", true),
		("TP19-bang-after-return-and-a-blank", "return !", true),
		("TP20-bang-directly-after-return", "return!", true),
		("TP21-bang-directly-after-typeof", "typeof!", true),
		("TP22-bang-directly-after-await", "await!", true),
		("TP23-bang-directly-after-case", "case!", true),
		("TP24-bang-after-a-bang", "!!", true),
		("TP25-two-postfix-bangs", "x!!", true),
		("TP26-bang-after-a-semicolon", "a;!", true),
		("TP27-blank-after-a-closing-parenthesis-before-the-bang", "(a) !", true),
		("TP28-bang-at-the-start-after-blanks", "  !", true),
		("TP30-satisfies-joins-no-keyword-list", "x satisfies ", false),
		("TP31-keyof-joins-no-keyword-list", "keyof ", false),
		("TP32-infer-joins-no-keyword-list", "infer ", false),
		("TP33-is-joins-no-keyword-list", "x is ", false),
		("TP34-asserts-joins-no-keyword-list", "asserts ", false),
		("TP35-declare-joins-no-keyword-list", "declare ", false),
		("TP36-abstract-joins-no-keyword-list", "abstract ", false),
		("TP37-readonly-joins-no-keyword-list", "readonly ", false),
		("TP38-as-joins-no-keyword-list", "x as ", false),
		("TP39-closing-angle-of-a-type-argument-list", "f<T>", true)
	];

	public static IEnumerable<object[]> InheritedRows => _inheritedRows.Select(row => new object[] { row.Id, row.Prefix, row.Opens });

	public static IEnumerable<object[]> TypeScriptRows => _typeScriptRows.Select(row => new object[] { row.Id, row.Prefix, row.Opens });

	/// <summary>Fixture T01. A postfix non-null assertion directly before a slash: the slash divides, so the line is code from end to end.</summary>
	[Fact]
	public void APostfixNonNullAssertionBeforeASlashDividesAndTheLineIsCode()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_NON_NULL_DIVISION);

		AssertValid(mask, _NON_NULL_DIVISION);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _NON_NULL_DIVISION, "x!/2;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _NON_NULL_DIVISION, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T01b. Contrast: the JavaScript-shaped predicate has no row for the postfix mark, so it opens a regular expression at <c>x!/2</c>, which a raw newline ends as a recovered anomaly.</summary>
	[Fact]
	public void TheJavaScriptShapedPredicateReadsAPostfixNonNullBeforeASlashAsARegularExpression()
	{
		ZoneMask javaScript = JavaScriptShapedProfile.Scan(_NON_NULL_DIVISION);

		AssertValid(javaScript, _NON_NULL_DIVISION);
		Assert.Equal(1, javaScript.NewlineTerminatedLiteralCount);
		Assert.Equal(LexicalZone.Delimiter, javaScript.ZoneAt(_NON_NULL_DIVISION.IndexOf('/', StringComparison.Ordinal)));
	}

	/// <summary>Fixture T02. A logical not before a regular expression: the slash opens, the body is string body and the call after it is code.</summary>
	[Fact]
	public void ALogicalNotBeforeARegularExpressionOpensTheLiteral()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_NOT_BEFORE_A_REGEX);

		AssertValid(mask, _NOT_BEFORE_A_REGEX);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _NOT_BEFORE_A_REGEX, "re", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _NOT_BEFORE_A_REGEX, ".test(s);", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _NOT_BEFORE_A_REGEX, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T03. Postfix marks around a spaced division, and a chain of two postfix divisions: no regular expression is invented.</summary>
	[Fact]
	public void PostfixMarksAroundADivisionAreCodeFromEndToEnd()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_SPACED_NON_NULL);

		AssertValid(mask, _SPACED_NON_NULL);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.NoPositionCarries(mask, _SPACED_NON_NULL, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _SPACED_NON_NULL, "a! / b!;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _SPACED_NON_NULL, "a!/b!/c;", LexicalZone.Code);
	}

	/// <summary>Fixture T04. A generic call followed by a division, a generic annotation, and nested type arguments: every slash divides and no angle bracket opens anything.</summary>
	[Fact]
	public void GenericArgumentListsAndADivisionAfterTheCallAreCode()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_GENERIC_THEN_DIVISION);

		AssertValid(mask, _GENERIC_THEN_DIVISION);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.NoPositionCarries(mask, _GENERIC_THEN_DIVISION, LexicalZone.StringBody);
		ZoneAssertions.NoPositionCarries(mask, _GENERIC_THEN_DIVISION, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _GENERIC_THEN_DIVISION, "f<T>(x) / 2;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _GENERIC_THEN_DIVISION, "g<Array<Map<K, V>>>(y) / 3;", LexicalZone.Code);
	}

	/// <summary>Fixture T04b. DECLARED COST: an instantiation expression (a generic function named with its type arguments and no call) followed by a division is valid, and the greater-than before the slash opens a literal that the raw line end recovers.</summary>
	[Fact]
	public void AnInstantiationExpressionFollowedByADivisionOpensALiteralThatTheLineEndRecovers()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_INSTANTIATION_EXPRESSION_THEN_DIVISION);

		AssertValid(mask, _INSTANTIATION_EXPRESSION_THEN_DIVISION);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _INSTANTIATION_EXPRESSION_THEN_DIVISION, "id<number> ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _INSTANTIATION_EXPRESSION_THEN_DIVISION, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T05. A <c>.tsx</c> arrow generic with a trailing comma, then a regular expression after the arrow: the slash after the arrow's greater-than opens.</summary>
	[Fact]
	public void AnArrowGenericWithATrailingCommaThenARegularExpressionOpensTheLiteral()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TSX_ARROW_GENERIC);

		AssertValid(mask, _TSX_ARROW_GENERIC);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _TSX_ARROW_GENERIC, "<T,>(x: T) => ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _TSX_ARROW_GENERIC, "re", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TSX_ARROW_GENERIC, ".test(x);", LexicalZone.Code);
	}

	/// <summary>Fixture T05b. An arrow generic with a constraint whose body is an element with a closing tag: the closing tag's slash directly after the less-than divides, and nothing opens.</summary>
	[Fact]
	public void AnArrowGenericWhoseBodyIsAnElementWithAClosingTagIsCode()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TSX_ARROW_GENERIC_WITH_ELEMENT);

		AssertValid(mask, _TSX_ARROW_GENERIC_WITH_ELEMENT);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.NoPositionCarries(mask, _TSX_ARROW_GENERIC_WITH_ELEMENT, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TSX_ARROW_GENERIC_WITH_ELEMENT, "<div>{x}</div>;", LexicalZone.Code);
	}

	/// <summary>Fixture T06. An angle-bracket assertion in a <c>.ts</c> file: followed by an identifier and a division it divides; followed directly by a regular expression it opens (the greater-than row).</summary>
	[Fact]
	public void AnAngleBracketAssertionDividesAfterAnIdentifierAndOpensBeforeARegularExpression()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_ANGLE_BRACKET_ASSERTION);

		AssertValid(mask, _ANGLE_BRACKET_ASSERTION);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _ANGLE_BRACKET_ASSERTION, "<string>value;", LexicalZone.Code, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _ANGLE_BRACKET_ASSERTION, "<string>value / 2;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _ANGLE_BRACKET_ASSERTION, "re", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _ANGLE_BRACKET_ASSERTION, ";", LexicalZone.Code, occurrence: 2);
	}

	/// <summary>Fixture T07. A type assertion by <c>as</c>, a chain of two, and <c>satisfies</c>, each followed by a division: the slash after the type name divides.</summary>
	[Fact]
	public void AnAsAssertionAndASatisfiesClauseFollowedByADivisionAreCode()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_AS_ASSERTION_THEN_DIVISION);

		AssertValid(mask, _AS_ASSERTION_THEN_DIVISION);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.NoPositionCarries(mask, _AS_ASSERTION_THEN_DIVISION, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _AS_ASSERTION_THEN_DIVISION, "x as T / 2;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _AS_ASSERTION_THEN_DIVISION, "x as unknown as number / 2;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _AS_ASSERTION_THEN_DIVISION, "x satisfies T / 2;", LexicalZone.Code);
	}

	/// <summary>Fixture T08. A decorator with an object argument whose template holds markup text, then members that hold regular expressions after an equals sign and after a return: each regular expression is one literal and the quote inside the first is string body.</summary>
	[Fact]
	public void ADecoratorWithAnObjectArgumentIsFollowedByRegularExpressionsThatOpen()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_DECORATOR_THEN_REGEX);

		AssertValid(mask, _DECORATOR_THEN_REGEX);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _DECORATOR_THEN_REGEX, "<p>{{ label }}</p>", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _DECORATOR_THEN_REGEX, "ab'c", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _DECORATOR_THEN_REGEX, "x[/]y", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _DECORATOR_THEN_REGEX, ".test(s); }", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _DECORATOR_THEN_REGEX, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T08b. DECLARED, and not valid TypeScript: a slash directly after a decorator's closing parenthesis divides, as after any closing parenthesis. A decorator is never followed by a regular expression in valid code.</summary>
	[Fact]
	public void ASlashAfterADecoratorArgumentDividesLikeAnyClosingParenthesis()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_DECORATOR_ARGUMENT_THEN_SLASH);

		AssertValid(mask, _DECORATOR_ARGUMENT_THEN_SLASH);
		ZoneAssertions.NoPositionCarries(mask, _DECORATOR_ARGUMENT_THEN_SLASH, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _DECORATOR_ARGUMENT_THEN_SLASH, "/re/.test(s);", LexicalZone.Code);
	}

	/// <summary>Fixture T09. Template-literal types are templates to the scanner: the text is string body, a hole is code, and a template nested in a hole is read to its own end.</summary>
	[Fact]
	public void TemplateLiteralTypesAreTemplatesWithCodeHoles()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TEMPLATE_LITERAL_TYPES);

		AssertValid(mask, _TEMPLATE_LITERAL_TYPES);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_LITERAL_TYPES, "Basic ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_LITERAL_TYPES, "string", LexicalZone.Code, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_LITERAL_TYPES, "Prefix", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_LITERAL_TYPES, "-", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_LITERAL_TYPES, "C", LexicalZone.Code, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _TEMPLATE_LITERAL_TYPES, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T10. A triple-slash directive is a line comment to the line feed, including its self-closing marker; a triple slash after code is a line comment too.</summary>
	[Fact]
	public void ATripleSlashDirectiveIsALineComment()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TRIPLE_SLASH_DIRECTIVE);

		AssertValid(mask, _TRIPLE_SLASH_DIRECTIVE);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _TRIPLE_SLASH_DIRECTIVE, "/// <reference path=\"types.d.ts\" />", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _TRIPLE_SLASH_DIRECTIVE, "/// <reference types=\"node\" />", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _TRIPLE_SLASH_DIRECTIVE, "/// trailing", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _TRIPLE_SLASH_DIRECTIVE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T11. Enums, a const enum, an ambient enum, a namespace, a nested namespace and an ambient module are brace-structured code: string members are string body, a division in an initializer is code, a regular expression after return opens.</summary>
	[Fact]
	public void EnumsNamespacesAndAmbientModulesAreBraceStructuredCode()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_ENUM_AND_NAMESPACE);

		AssertValid(mask, _ENUM_AND_NAMESPACE);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _ENUM_AND_NAMESPACE, "Basic", LexicalZone.StringBody, occurrence: 1);
		ZoneAssertions.SpansZone(mask, _ENUM_AND_NAMESPACE, "Half = 4 / 2", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _ENUM_AND_NAMESPACE, "^Basic ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _ENUM_AND_NAMESPACE, "x = 1 / 3;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _ENUM_AND_NAMESPACE, "client", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _ENUM_AND_NAMESPACE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T12a. A <c>.tsx</c> file with the recoverable hazards: an apostrophe in text is a recovered literal that hides the rest of its line, a double slash in text is a line comment, a self-closing tag after a hole, a generic element, a closing tag after a hole and a closing tag are code.</summary>
	[Fact]
	public void ARecoverableTsxFileKeepsEveryLineAfterTheDamagedOnes()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TSX_RECOVERED_HAZARDS);

		AssertValid(mask, _TSX_RECOVERED_HAZARDS);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _TSX_RECOVERED_HAZARDS, "it's", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TSX_RECOVERED_HAZARDS, "s text</p>", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TSX_RECOVERED_HAZARDS, "//host.test/path</a>", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _TSX_RECOVERED_HAZARDS, "<Img src={u} />", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _TSX_RECOVERED_HAZARDS, "<List<string> items={xs} />", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _TSX_RECOVERED_HAZARDS, "{ok && <b>bold</b>}", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _TSX_RECOVERED_HAZARDS, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T12b. DECLARED COST, unrecoverable: a backtick in element text opens a template that nothing closes, so the tail of the file is abandoned from the backtick.</summary>
	[Fact]
	public void ABacktickInElementTextAbandonsTheTailOfTheFile()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TSX_BACKTICK_IN_TEXT);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(_TSX_BACKTICK_IN_TEXT.IndexOf(_BACKTICK, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>Fixture T12c. DECLARED COST, unrecoverable: a block-comment opener in element text opens a comment that nothing closes, so the tail of the file is abandoned from the opener.</summary>
	[Fact]
	public void ABlockCommentOpenerInElementTextAbandonsTheTailOfTheFile()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TSX_BLOCK_COMMENT_OPENER_IN_TEXT);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		Assert.Equal(_TSX_BLOCK_COMMENT_OPENER_IN_TEXT.IndexOf(_BLOCK_COMMENT_OPENING, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>Fixture T12d. DECLARED COST, unrecoverable: a template nested nine holes deep abandons the file as an interpolation depth overflow, valid through the outermost backtick.</summary>
	[Fact]
	public void ATemplateNineHolesDeepAbandonsTheFile()
	{
		string source = $"const t = {NestedTemplate(_TEMPLATE_DEPTH_NINE, _NESTED_INNERMOST)};\n{_FOLLOWING_STATEMENT}\n";

		ZoneMask mask = TypeScriptShapedProfile.Scan(source);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.InterpolationDepthExceeded, mask.AnomalyReason);
		Assert.Equal(source.IndexOf(_BACKTICK, StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>
	/// Fixture T12e. Exclamation mark and slash in element text: directly after a word the slash divides, so <c>wow!/</c> is code and so is the closing tag after it;
	/// after a blank the mark is read as a logical not and the slash opens a literal (DECLARED COST, the blank row), which the slash of the closing tag then closes, so the
	/// literal is one character wide and the damage is bounded: no anomaly, the rest of the line is code.
	/// </summary>
	[Fact]
	public void AnExclamationMarkAndASlashInElementTextDividesDirectlyAfterAWordAndOpensAfterABlank()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TSX_BANG_SLASH_IN_TEXT);
		int blankedBang = _TSX_BANG_SLASH_IN_TEXT.IndexOf("wow !/", StringComparison.Ordinal) + "wow ".Length;

		AssertValid(mask, _TSX_BANG_SLASH_IN_TEXT);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _TSX_BANG_SLASH_IN_TEXT, "<p>wow!/</p>;", LexicalZone.Code);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(blankedBang));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(blankedBang + 1));
		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(blankedBang + 2));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(blankedBang + 3));
		ZoneAssertions.SpansZone(mask, _TSX_BANG_SLASH_IN_TEXT, "p>;", LexicalZone.Code, occurrence: 1);
		ZoneAssertions.SpansZone(mask, _TSX_BANG_SLASH_IN_TEXT, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T13. A declaration file: ambient modules, interfaces, overloaded signatures, type predicates, a global augmentation and exports are code with no regular expression; a literal type is string body and a template-literal type has a code hole.</summary>
	[Fact]
	public void ADeclarationFileIsCodeWithLiteralTypesAsStringBody()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_DECLARATION_FILE);

		AssertValid(mask, _DECLARATION_FILE);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _DECLARATION_FILE, "/// <reference types=\"node\" />", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _DECLARATION_FILE, "client", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _DECLARATION_FILE, "Basic", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _DECLARATION_FILE, "X-Id", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _DECLARATION_FILE, "number", LexicalZone.Code, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _DECLARATION_FILE, "export as namespace Client;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _DECLARATION_FILE, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T14. <c>return !/re/</c>: a blank before the mark, the previous character is not an operand end, the mark is a logical not and the slash opens.</summary>
	[Fact]
	public void AReturnThenALogicalNotThenARegularExpressionOpensTheLiteral()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_RETURN_SPACE_NOT_REGEX);

		AssertValid(mask, _RETURN_SPACE_NOT_REGEX);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _RETURN_SPACE_NOT_REGEX, "re", LexicalZone.StringBody, occurrence: 1);
		ZoneAssertions.SpansZone(mask, _RETURN_SPACE_NOT_REGEX, ".test(s); }", LexicalZone.Code);
	}

	/// <summary>Fixture T15. <c>return!/re/</c>: the word directly before the mark is an opening keyword, so the mark is a logical not and the slash opens.</summary>
	[Fact]
	public void AReturnDirectlyBeforeALogicalNotThenARegularExpressionOpensTheLiteral()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_RETURN_NO_SPACE_NOT_REGEX);

		AssertValid(mask, _RETURN_NO_SPACE_NOT_REGEX);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _RETURN_NO_SPACE_NOT_REGEX, "re", LexicalZone.StringBody, occurrence: 1);
		ZoneAssertions.SpansZone(mask, _RETURN_NO_SPACE_NOT_REGEX, ".test(s); }", LexicalZone.Code);
	}

	/// <summary>Fixture T16. A postfix mark before a member access, an element access and a call opens nothing: the line is code but for the one quoted key.</summary>
	[Fact]
	public void APostfixMarkBeforeAMemberAccessAnElementAccessAndACallOpensNothing()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_NON_NULL_MEMBER);

		AssertValid(mask, _NON_NULL_MEMBER);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _NON_NULL_MEMBER, "k", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _NON_NULL_MEMBER, "const v = maybe!.x;", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _NON_NULL_MEMBER, "const z = fn!(1);", LexicalZone.Code);
	}

	/// <summary>Fixture T17. A line feed before the mark: the mark starts a new statement (a postfix assertion cannot follow a line end), so it is a logical not and the slash opens.</summary>
	[Fact]
	public void ALineFeedBeforeTheMarkMakesItALogicalNotAndTheSlashOpens()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_LINE_FEED_BEFORE_A_BANG);

		AssertValid(mask, _LINE_FEED_BEFORE_A_BANG);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _LINE_FEED_BEFORE_A_BANG, "re", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _LINE_FEED_BEFORE_A_BANG, ".test(s);", LexicalZone.Code);
	}

	/// <summary>Fixture T18a. DECLARED COST: a blank before the mark makes it a logical not, so <c>x !/2</c> opens a literal that the raw line end recovers (one recovered literal, the next line is code).</summary>
	[Fact]
	public void ABlankBeforeThePostfixMarkOpensALiteralThatTheLineEndRecovers()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_BLANK_BEFORE_A_BANG);

		AssertValid(mask, _BLANK_BEFORE_A_BANG);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _BLANK_BEFORE_A_BANG, "2;", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _BLANK_BEFORE_A_BANG, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T18b. DECLARED COST: the same misreading with a later slash on the line closes the literal there, so the code between the two slashes is read as a regular-expression body and the anomaly count stays zero.</summary>
	[Fact]
	public void ABlankBeforeThePostfixMarkWithALaterSlashHidesTheCodeBetweenTheSlashes()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_BLANK_BEFORE_A_BANG_WITH_LATER_SLASH);

		AssertValid(mask, _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH, "2; const b = c", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH, "3;", LexicalZone.Code);
	}

	/// <summary>Fixture T18c. DECLARED COST: blanks on both sides of the mark, <c>x ! / 2</c>, read the mark as a logical not and open a literal at the slash.</summary>
	[Fact]
	public void BlanksOnBothSidesOfThePostfixMarkOpenALiteralAtTheSlash()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_BLANKS_AROUND_A_BANG);

		AssertValid(mask, _BLANKS_AROUND_A_BANG);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _BLANKS_AROUND_A_BANG, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T18d. DECLARED COST: a closing brace is not the end of an operand for the mark, so <c>}!/2</c> opens a literal that the raw line end recovers.</summary>
	[Fact]
	public void AClosingBraceBeforeThePostfixMarkOpensALiteralThatTheLineEndRecovers()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_BRACE_BEFORE_A_BANG);

		AssertValid(mask, _BRACE_BEFORE_A_BANG);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _BRACE_BEFORE_A_BANG, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T18e. DECLARED COST: two postfix marks in a row, <c>x!!/2</c>, put a mark before the second one, which is not an operand end, so the slash opens a literal that the raw line end recovers.</summary>
	[Fact]
	public void TwoPostfixMarksInARowOpenALiteralAtTheSlash()
	{
		ZoneMask mask = TypeScriptShapedProfile.Scan(_DOUBLE_NON_NULL);

		AssertValid(mask, _DOUBLE_NON_NULL);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, _DOUBLE_NON_NULL, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	/// <summary>Fixture T19. The constant declaration expression with a type annotation and field modifiers: typed constants and class fields with a literal are counted; a field with a call value, a value that reads another name, a function-typed annotation and a generic annotation holding a literal behave as stated.</summary>
	[Fact]
	public void TheTypedConstantDeclarationCountsAnnotatedConstantsAndFieldsWithALiteral()
	{
		LexicalProfile profile = TypeScriptShapedProfile.Build();
		ZoneMask mask = TypeScriptShapedProfile.Scan(_TYPED_CONSTANT_DECLARATIONS);

		FileSymbolTable symbols = FileSymbolTable.Collect(profile, _TYPED_CONSTANT_DECLARATIONS, mask);

		AssertValid(mask, _TYPED_CONSTANT_DECLARATIONS);
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

	/// <summary>Fixtures P01 to P58. Every row of the backward predicate that the TypeScript-shaped predicate inherits: the slash opens or divides as the row says, the file stays completely valid, and the mask is identical to the one the JavaScript-shaped profile gives.</summary>
	[Theory]
	[MemberData(nameof(InheritedRows))]
	public void TheInheritedPredicateRowsAnswerAsTheJavaScriptShapedPredicateDoes(string id, string prefix, bool opens)
	{
		string source = $"{prefix}{_REGEX_CANDIDATE}";

		ZoneMask mask = TypeScriptShapedProfile.Scan(source);
		ZoneMask javaScript = JavaScriptShapedProfile.Scan(source);

		AssertRow(id, prefix, opens, source, mask);
		Assert.Equal(javaScript.Zones.ToArray(), mask.Zones.ToArray());
	}

	/// <summary>Fixtures TP01 onward. The rows that are new for TypeScript: the mark directly before the slash and the characters that end an operand before it, the blank and the line feed and the closing brace that do not, the keywords directly before the mark, and the words that join no keyword list.</summary>
	[Theory]
	[MemberData(nameof(TypeScriptRows))]
	public void TheNewPredicateRowsOpenOrDivideAsTheirRowSays(string id, string prefix, bool opens)
	{
		string source = $"{prefix}{_REGEX_CANDIDATE}";

		ZoneMask mask = TypeScriptShapedProfile.Scan(source);

		AssertRow(id, prefix, opens, source, mask);
	}

	/// <summary>The only rows on which the two predicates differ are the ones where the mark stands directly after an operand end: there TypeScript divides and JavaScript opens.</summary>
	[Fact]
	public void TheTwoPredicatesDifferExactlyWhereAPostfixMarkEndsAnOperand()
	{
		string[] differing = [.. _typeScriptRows
			.Where(row => TypeScriptShapedProfile.Scan($"{row.Prefix}{_REGEX_CANDIDATE}").ZoneAt(row.Prefix.Length) != JavaScriptShapedProfile.Scan($"{row.Prefix}{_REGEX_CANDIDATE}").ZoneAt(row.Prefix.Length))
			.Select(row => row.Id)];
		string[] expected = [.. _typeScriptRows.Where(row => row.Prefix.EndsWith('!') && !row.Opens).Select(row => row.Id)];

		Assert.Equal(expected, differing);
		Assert.NotEmpty(expected);
	}

	/// <summary>The two inline long-line fixtures, <c>directly</c> and <c>blank</c>, which are not rows of the numbered list and so are not dumped. The slash after a mark that directly follows a 9,000-character word divides however long the line is, and when a blank stands between the word and the mark the character directly before the mark is the blank, so the slash is read as a regular-expression opener.</summary>
	[Fact]
	public void AMarkAfterALongWordOnALongLineStillDividesAndABlankBeforeItDoesNot()
	{
		string longWord = new('a', 9_000);
		string directly = $"{longWord}!{_REGEX_CANDIDATE}";
		string blank = $"{longWord} !{_REGEX_CANDIDATE}";

		ZoneMask directMask = TypeScriptShapedProfile.Scan(directly);
		ZoneMask blankMask = TypeScriptShapedProfile.Scan(blank);

		AssertValid(directMask, directly);
		Assert.Equal(LexicalZone.Code, directMask.ZoneAt(directly.IndexOf('/', StringComparison.Ordinal)));
		AssertValid(blankMask, blank);
		Assert.Equal(LexicalZone.Delimiter, blankMask.ZoneAt(blank.IndexOf('/', StringComparison.Ordinal)));
	}

	/// <summary>
	/// THE DUMP. When <see cref="DUMP_VARIABLE_NAME"/> names a path, the zone mask of every fixture of this class (the long fixtures excluded) is written to it, one block per
	/// fixture: its id, the mask's validity and anomaly, the text with control characters shown as a middle dot, and one glyph per position (. code, c comment, s string body,
	/// d delimiter). Without the variable the test does nothing and passes, as the other dump and census harnesses do. It is what a later change under <c>Lexical/</c> re-runs.
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
				ZoneMask mask = TypeScriptShapedProfile.Scan(source);

				dump.AppendLine($"### {id}: valid-through {mask.ValidThroughPosition} of {mask.Length}; anomaly {mask.AnomalyReason}; newline-terminated {mask.NewlineTerminatedLiteralCount}; unbalanced {mask.UnbalancedNestingCount}");
				dump.Append(ZoneAssertions.Render(mask, source));
			}

			File.WriteAllText(destination, dump.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			Assert.True(File.Exists(destination));
		}
	}

	private static IEnumerable<(string Id, string Source)> Fixtures()
	{
		return NamedFixtures()
			.Concat(_typeScriptRows.Select(row => (row.Id, $"{row.Prefix}{_REGEX_CANDIDATE}")))
			.Concat(_inheritedRows.Select(row => (row.Id, $"{row.Prefix}{_REGEX_CANDIDATE}")))
			.Where(fixture => fixture.Item2.Length <= _MAXIMUM_DUMPED_FIXTURE_LENGTH);
	}

	private static (string Id, string Source)[] NamedFixtures()
	{
		return
		[
			("T01-non-null-division", _NON_NULL_DIVISION),
			("T02-not-before-a-regex", _NOT_BEFORE_A_REGEX),
			("T03-spaced-non-null", _SPACED_NON_NULL),
			("T04-generic-then-division", _GENERIC_THEN_DIVISION),
			("T04b-instantiation-expression-then-division", _INSTANTIATION_EXPRESSION_THEN_DIVISION),
			("T05-tsx-arrow-generic", _TSX_ARROW_GENERIC),
			("T05b-tsx-arrow-generic-with-element", _TSX_ARROW_GENERIC_WITH_ELEMENT),
			("T06-angle-bracket-assertion", _ANGLE_BRACKET_ASSERTION),
			("T07-as-assertion-then-division", _AS_ASSERTION_THEN_DIVISION),
			("T08-decorator-then-regex", _DECORATOR_THEN_REGEX),
			("T08b-decorator-argument-then-slash", _DECORATOR_ARGUMENT_THEN_SLASH),
			("T09-template-literal-types", _TEMPLATE_LITERAL_TYPES),
			("T10-triple-slash-directive", _TRIPLE_SLASH_DIRECTIVE),
			("T11-enum-and-namespace", _ENUM_AND_NAMESPACE),
			("T12a-tsx-recovered-hazards", _TSX_RECOVERED_HAZARDS),
			("T12b-tsx-backtick-in-text", _TSX_BACKTICK_IN_TEXT),
			("T12c-tsx-block-comment-opener-in-text", _TSX_BLOCK_COMMENT_OPENER_IN_TEXT),
			("T12d-template-nine-deep", $"const t = {NestedTemplate(_TEMPLATE_DEPTH_NINE, _NESTED_INNERMOST)};\n{_FOLLOWING_STATEMENT}\n"),
			("T12e-tsx-bang-slash-in-text", _TSX_BANG_SLASH_IN_TEXT),
			("T13-declaration-file", _DECLARATION_FILE),
			("T14-return-space-not-regex", _RETURN_SPACE_NOT_REGEX),
			("T15-return-no-space-not-regex", _RETURN_NO_SPACE_NOT_REGEX),
			("T16-non-null-member", _NON_NULL_MEMBER),
			("T17-line-feed-before-a-bang", _LINE_FEED_BEFORE_A_BANG),
			("T18a-blank-before-a-bang", _BLANK_BEFORE_A_BANG),
			("T18b-blank-before-a-bang-with-later-slash", _BLANK_BEFORE_A_BANG_WITH_LATER_SLASH),
			("T18c-blanks-around-a-bang", _BLANKS_AROUND_A_BANG),
			("T18d-brace-before-a-bang", _BRACE_BEFORE_A_BANG),
			("T18e-double-non-null", _DOUBLE_NON_NULL),
			("T19-typed-constant-declarations", _TYPED_CONSTANT_DECLARATIONS)
		];
	}

	private static void AssertRow(string id, string prefix, bool opens, string source, ZoneMask mask)
	{
		int slash = prefix.Length;

		AssertValid(mask, source);
		Assert.True(opens ? mask.ZoneAt(slash) == LexicalZone.Delimiter : mask.ZoneAt(slash) == LexicalZone.Code, $"{id}: {ZoneAssertions.Render(mask, source)}");
		Assert.Equal(opens ? LexicalZone.StringBody : LexicalZone.Code, mask.ZoneAt(slash + _FIRST_CHARACTER_AFTER_THE_SLASH));
		Assert.Equal(opens ? LexicalZone.StringBody : LexicalZone.Code, mask.ZoneAt(slash + _SECOND_CHARACTER_AFTER_THE_SLASH));
	}

	/// <summary>A template whose hole holds a template, <paramref name="depth"/> holes deep, with the given code in the innermost hole.</summary>
	private static string NestedTemplate(int depth, string innermost)
	{
		return depth == 0 ? innermost : $"`a${{{NestedTemplate(depth - 1, innermost)}}}z`";
	}

	private static void AssertValid(ZoneMask mask, string source)
	{
		Assert.True(mask.IsCompletelyValid, $"The mask is valid only through {mask.ValidThroughPosition} ({mask.AnomalyReason}).{Environment.NewLine}{ZoneAssertions.Render(mask, source)}");
	}
}
