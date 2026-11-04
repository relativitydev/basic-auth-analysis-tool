using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// CLAIMS ABOUT THE SHIPPED EXPRESSIONS, CONFIRMED BY RUNNING THEM IN .NET. Each test runs the expression of the JavaScript analyzer that the binary ships,
/// taken through the registered analyzer, on invented TypeScript-shaped text with generic names, and asserts what the claim says.
/// </summary>
public sealed class TypeScriptReasonedClaimTests
{
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _SCHEME_GROUP = "scheme";
	private const string _AUTH_OBJECT = "{ auth: { username: 'u', password: 'p' } }";
	private const int _AFTER_EVERY_DECLARATION = int.MaxValue;

	public static IEnumerable<object[]> CallsWithAndWithoutAGenericArgumentList =>
	[
		["JS-001", "axios.post(url, { auth: { username: 'u', password: 'p' } });", "axios.post<Reply>(url, { auth: { username: 'u', password: 'p' } });"],
		["JS-001", "axios(url, { auth: { username: 'u', password: 'p' } });", "axios<Reply>(url, { auth: { username: 'u', password: 'p' } });"],
		["JS-001", "request(url, { auth: { username: 'u', password: 'p' } });", "request<Reply>(url, { auth: { username: 'u', password: 'p' } });"],
		["JS-003", "axios.get('https://user:secret@host.test/path');", "axios.get<Reply>('https://user:secret@host.test/path');"],
		["JS-015", "got(url, { username: 'u', password: 'p' });", "got<Reply>(url, { username: 'u', password: 'p' });"],
		["JS-016", "request({ auth: { user: 'u', pass: 'p', sendImmediately: false } });", "request<Reply>({ auth: { user: 'u', pass: 'p', sendImmediately: false } });"]
	];

	public static IEnumerable<object[]> AngularFormsThatTheHeaderClassReads =>
	[
		["const h = new HttpHeaders({ Authorization: 'Basic ' + token });", "Authorization"],
		["const h = new HttpHeaders({ 'Authorization': `Basic ${token}` });", "Authorization"],
		["const r = req.clone({ setHeaders: { Authorization: 'Basic ' + token } });", "Authorization"],
		["headers.set('Authorization', 'Basic ' + token);", "set"],
		["headers.append('Authorization', 'Basic ' + token);", "append"]
	];

	public static IEnumerable<object[]> IdentifierValuesThatTheHeaderClassDoesNotRead =>
	[
		["const h = new HttpHeaders({ Authorization: header });", false],
		["const r = req.clone({ setHeaders: { Authorization: header } });", false],
		["headers.set('Authorization', header);", true],
		["const o = { headers: { Authorization: header } };", true]
	];

	public static IEnumerable<object[]> TypePositionKeyedLiteralsThatTheTailRefusalRefuses =>
	[
		["interface Req { Authorization: 'Basic'; }"],
		["type H = { Authorization: 'Basic' | 'Bearer' };"],
		["type H = { Authorization: `Basic ${string}` };"],
		["type H = { Authorization: `Basic ${number}` };"]
	];

	public static IEnumerable<object[]> TypePositionKeyedLiterals =>
	[
		["interface Req {\n\tAuthorization: 'Basic'\n}"],
		["type H = { Authorization: 'Basic', other: string };"],
		["type H = { Authorization: 'Basic' };"],
		["headers.Authorization = 'Basic';"]
	];

	public static IEnumerable<object[]> IdentifierTails =>
	[
		["const o = { headers: { Authorization: header } };", true],
		["const o = { headers: { Authorization: this.header } };", false],
		["const o = { headers: { Authorization: header! } };", false],
		["const o = { headers: { Authorization: header as string } };", false],
		["const o = { headers: { Authorization: header.value } };", false],
		["const o = { headers: { Authorization: getHeader() } };", false],
		["const o = { headers: { Authorization: table[0] } };", false]
	];

	/// <summary>A generic argument list between the callee and the opening parenthesis is read by the callee expressions of four classes (the generic-argument group), as the same call without it is.</summary>
	[Theory]
	[MemberData(nameof(CallsWithAndWithoutAGenericArgumentList))]
	public void AGenericArgumentListBetweenTheCalleeAndTheParenthesisIsRead(string patternClass, string withoutGenerics, string withGenerics)
	{
		Assert.Matches(ExpressionOf(patternClass), withoutGenerics);
		Assert.Matches(ExpressionOf(patternClass), withGenerics);
	}

	/// <summary>The client-option class names its callee: a call through an injected service whose name is not <c>axios</c> or <c>request</c> is not read by it, and the Low class that anchors on the object alone reads it instead.</summary>
	[Theory]
	[InlineData("this.httpService.post(url, { auth: { username: 'u', password: 'p' } });")]
	[InlineData("this.http.get<Reply>(url, { auth: { username: 'u', password: 'p' } });")]
	public void AnInjectedServiceCallIsNotReadByTheClientOptionClassAndTheLowClassReadsItInstead(string text)
	{
		Match low = ExpressionOf("JS-011").Match(text);

		Assert.DoesNotMatch(ExpressionOf("JS-001"), text);
		Assert.True(low.Success);
		Assert.Equal("auth", low.Groups[_CONSTRUCT_GROUP].Value);
		Assert.Equal(ConfidenceLevel.Low, RuleOf("JS-011").BaseLevel);
		Assert.Equal(ConfidenceLevel.High, RuleOf("JS-001").BaseLevel);
	}

	/// <summary>The header class reads an object-literal key and a header-setting call when the value starts with a Basic literal, and its anchor is the named group.</summary>
	[Theory]
	[MemberData(nameof(AngularFormsThatTheHeaderClassReads))]
	public void TheHeaderClassReadsTheObjectKeyAndTheSetOrAppendCallWhenTheValueStartsWithABasicLiteral(string text, string anchor)
	{
		Match match = ExpressionOf("JS-004").Match(text);

		Assert.True(match.Success, text);
		Assert.Equal(anchor, match.Groups[_CONSTRUCT_GROUP].Value);
	}

	/// <summary>An identifier value is read by the symbolic class only through a carrier it lists, and the object-key carrier of the header class is not among them.</summary>
	[Theory]
	[MemberData(nameof(IdentifierValuesThatTheHeaderClassDoesNotRead))]
	public void AnIdentifierValueIsReadByTheSymbolicClassOnlyThroughItsOwnCarriers(string text, bool symbolicClassReadsIt)
	{
		Assert.DoesNotMatch(ExpressionOf("JS-004"), text);
		Assert.Equal(symbolicClassReadsIt, ExpressionOf("JS-017").IsMatch(text));
	}

	/// <summary>A keyed literal in type position that ends with a semicolon or a single vertical bar, or whose template hole is `string` or `number`, is refused by the tail refusal of the object-key carrier.</summary>
	[Theory]
	[MemberData(nameof(TypePositionKeyedLiteralsThatTheTailRefusalRefuses))]
	public void AKeyedLiteralInTypePositionWithARefusedTailIsNotReadByTheObjectKeyCarrier(string text)
	{
		Assert.DoesNotMatch(ExpressionOf("JS-004"), text);
	}

	/// <summary>A keyed literal in type position is read by the object-key carrier, with the key as the anchor, and the anchor is in the code zone of the scanned text, so the read is at the class's own level.</summary>
	[Theory]
	[MemberData(nameof(TypePositionKeyedLiterals))]
	public void AKeyedLiteralInTypePositionIsReadByTheObjectKeyCarrierWithACodeAnchor(string text)
	{
		Match match = ExpressionOf("JS-004").Match(text);
		ZoneMask mask = TypeScriptShapedProfile.Scan(text);

		Assert.True(match.Success, text);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(match.Groups[_CONSTRUCT_GROUP].Index));
	}

	/// <summary>The symbolic class refuses a value that is a member access, a call, an element access, a non-null assertion or a cast, and reads a bare identifier.</summary>
	[Theory]
	[MemberData(nameof(IdentifierTails))]
	public void TheSymbolicClassReadsABareIdentifierAndRefusesEveryOtherTail(string text, bool read)
	{
		Match match = ExpressionOf("JS-017").Match(text);

		Assert.Equal(read, match.Success);

		if (read)
		{
			Assert.False(string.IsNullOrEmpty(match.Groups[_SCHEME_GROUP].Value));
		}
	}

	/// <summary>
	/// The window vocabulary has its own regular-expression reading, which opens after an exclamation mark: inside an argument window a postfix assertion before a slash
	/// can start a literal that swallows the object the window was looking for, when a later slash on the same line closes it. The same call without the mark, and the call
	/// with the mark but no later slash on the line, are read.
	/// </summary>
	[Fact]
	public void AnExclamationMarkBeforeASlashInsideAnArgumentWindowCanHideTheObjectThatTheWindowSeeks()
	{
		string withoutMark = $"axios.post(x/2, {_AUTH_OBJECT}); // done/";
		string withMarkAndLaterSlash = $"axios.post(x!/2, {_AUTH_OBJECT}); // done/";
		string withMarkAndNoLaterSlash = $"axios.post(x!/2, {_AUTH_OBJECT});";

		Assert.Matches(ExpressionOf("JS-001"), withoutMark);
		Assert.DoesNotMatch(ExpressionOf("JS-001"), withMarkAndLaterSlash);
		Assert.Matches(ExpressionOf("JS-001"), withMarkAndNoLaterSlash);
	}

	/// <summary>The constant declaration of the shipped JavaScript profile does not count an annotated declaration, so its name is never collected; the test-local TypeScript profile counts it, and refuses a value that is not a literal.</summary>
	[Fact]
	public void AnAnnotatedConstantIsNotCountedByTheShippedProfileAndIsCountedByTheTypeScriptShapedOne()
	{
		const string text = "const a: string = 'Basic x';\nconst b = 'Basic y';\nconst c: string = compute();\n";
		LexicalProfile shipped = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;
		LexicalProfile typeScript = TypeScriptShapedProfile.Build();

		FileSymbolTable shippedSymbols = FileSymbolTable.Collect(shipped, text, new LexicalScanner(shipped).Scan(text));
		FileSymbolTable typeScriptSymbols = FileSymbolTable.Collect(typeScript, text, new LexicalScanner(typeScript).Scan(text));

		Assert.Equal(0, shippedSymbols.DeclarationCountOf("a"));
		Assert.Equal(1, shippedSymbols.DeclarationCountOf("b"));
		Assert.Equal(1, typeScriptSymbols.DeclarationCountOf("a"));
		Assert.Equal(1, typeScriptSymbols.DeclarationCountOf("b"));
		Assert.Equal(0, typeScriptSymbols.DeclarationCountOf("c"));
		Assert.True(typeScriptSymbols.TryResolveUnambiguously("a", _AFTER_EVERY_DECLARATION, out string value));
		Assert.Equal("Basic x", value);
	}

	/// <summary>A declaration file is attributed by its final extension: the .NET definition of the extension of a path with several dots is the last one, and the shipped attribution names it TypeScript through the base table.</summary>
	[Theory]
	[InlineData("types/client.d.ts", ".ts")]
	[InlineData("types/client.d.mts", ".mts")]
	[InlineData("types/client.d.cts", ".cts")]
	[InlineData("src/app.ts", ".ts")]
	[InlineData("src/view.tsx", ".tsx")]
	[InlineData("src/module.mts", ".mts")]
	[InlineData("src/legacy.cts", ".cts")]
	public void ADeclarationFileIsAttributedByItsFinalExtension(string relativePath, string extension)
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(new LanguageCatalog(LanguageCatalog.CurrentVersion, EmbeddedLanguageAnalyzers.CreateAll()));

		AttributedLanguage result = attribution.Attribute(harness.PathOf(relativePath), firstLine: null);

		Assert.Equal(extension, Path.GetExtension(relativePath));
		Assert.Equal("TypeScript", result.LanguageName);
	}

	private static DetectionRule RuleOf(string patternClass)
	{
		return JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[patternClass];
	}

	private static Regex ExpressionOf(string patternClass)
	{
		return RuleOf(patternClass).Expression;
	}
}
