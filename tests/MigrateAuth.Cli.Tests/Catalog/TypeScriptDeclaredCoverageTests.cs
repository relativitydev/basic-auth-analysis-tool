using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Release;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE DECLARED UNSUPPORTED COVERAGE OF THE SHIPPED `TypeScript` ANALYZER, and the report section that lists it.
///
/// The list is `TypeScript`'s own and not the `JavaScript` list passed through: every `JavaScript` entry is present as it stands, adapted (replaced by the entry `TypeScript` carries in its place),
/// or on the allow-list of `JavaScript`-only entries, and the parity test below fails on any other. The entries that exist for `TypeScript` alone are each bound to exactly one entry by a word
/// that only that entry's construct carries, so a dropped, duplicated or merged entry fails here. A form the catalog says is not supported must yield no finding where the form is a read, and the
/// tests below write such a form. Every script is invented, with placeholder names.
/// </summary>
public sealed class TypeScriptDeclaredCoverageTests
{
	private const string _LANGUAGE_NAME = "TypeScript";
	private const int _TYPESCRIPT_ONLY_ENTRY_COUNT = 32;
	private const int _ADAPTED_ENTRY_COUNT = 11;
	private const int _ADAPTED_CATALOG_ENTRY_COUNT = 7;
	private const int _JAVASCRIPT_ONLY_ENTRY_COUNT = 1;
	private const int _OT_ENTRY_COUNT = 39;
	private const int _OT_COST_ENTRY_COUNT = 19;
	private const string _SETTINGS_PROPERTY_PATTERN_CLASS = "JS-019";

	/// <summary>The words that only one `TypeScript`-alone or adapted entry's construct carries, with the identifier the catalog gives it.</summary>
	private static readonly ImmutableArray<(string Id, string Keyword)> _typeScriptEntries =
	[
		("OT-1", "extensionless runner script"),
		("OT-2", "a blank before the postfix non-null mark"),
		("OT-3", "a closing brace before the postfix non-null mark"),
		("OT-4", "two postfix non-null marks in a row"),
		("OT-5", "in .tsx element text: wow"),
		("OT-6", "an instantiation expression"),
		("OT-7", "receiver name does not contain http"),
		("OT-8", "generic nesting beyond depth 3"),
		("OT-9", "cannot be told from a header value"),
		("OT-10", "a typed URL binding"),
		("OT-11", "regular-expression opener inside the argument window"),
		("OT-12", "the value tails this.x"),
		("OT-13", "generated-file sentence for hand-written ambient typing"),
		("OT-14", "type resolution across files"),
		("OT-15", "TypeScript embedded in other files"),
		("OT-16", "generated API clients (OpenAPI generators"),
		("OT-17", "what .tsx inherits from the JSX limits"),
		("OT-18", "an auth option in a module configuration"),
		("OT-19", "an identifier value in an Angular Authorization object"),
		("OT-20", "inbound Basic Authentication"),
		("OT-21", "Relativity-published TypeScript client"),
		("OT-22", "a typed credential object whose type name the tool does not know"),
		("OT-23", "Angular and NestJS API names not read"),
		("OT-24", "an injected client as the receiver of a callee-anchored class"),
		("OT-25", "an environment read with a TypeScript tail"),
		("OT-26", "the flow misreads TypeScript's layout makes likelier"),
		("OT-27", "inside a headers: member"),
		("OT-28", "whose auth key follows a nested brace"),
		("OT-29", "the fetch-based HttpClient"),
		("OT-30", "a typed declaration whose name denotes a proxy"),
		("OT-31", "a credential literal followed by a TypeScript tail"),
		("OT-32", "a generic call read in JavaScript text"),
		("OT-33", "a header-object finding lost on JavaScript text"),
		("OT-34", "an Angular HttpClient or another client injected under a name that contains http"),
		("OT-35", "a property of a settings interface, type or class"),
		("OT-36", "read by bracket access"),
		("OT-37", "the project's own encoder function"),
		("OT-38", "chosen by a selector"),
		("OT-39", "typed tail (a non-null mark or a type assertion) at its use")
	];

	/// <summary>The identifiers of the entries the catalog gives the kind cost; the others are unsupported or not applicable.</summary>
	private static readonly ImmutableArray<string> _costEntryIdentifiers =
	[
		"OT-2", "OT-3", "OT-4", "OT-5", "OT-6", "OT-8", "OT-9", "OT-10", "OT-11", "OT-12", "OT-13", "OT-17", "OT-26", "OT-27", "OT-30", "OT-31", "OT-32", "OT-33", "OT-34"
	];

	private static DeclaredUnsupportedCoverage[] JavaScriptDeclared => [.. JavaScriptRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];

	private static DeclaredUnsupportedCoverage[] Declared => [.. TypeScriptRegisteredExpressions.Analyzer.LanguageIdentification.DeclaredUnsupportedCoverage];

	// ------------------------------------------------------------------------------------------
	// The parity with the JavaScript list
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryJavaScriptEntryIsPresentAdaptedOrOnTheAllowListOfJavaScriptOnlyEntries()
	{
		DeclaredUnsupportedCoverage[] typeScript = Declared;
		string[] unaccounted =
		[
			.. JavaScriptDeclared
				.Where(entry => !IsPresent(entry, typeScript) && !IsAdapted(entry, typeScript) && !IsJavaScriptOnly(entry))
				.Select(entry => entry.Construct)
		];

		Assert.True(unaccounted.Length == 0, $"A JavaScript declared entry is neither present, adapted nor on the allow-list of JavaScript-only entries: {string.Join(" | ", unaccounted)}");
	}

	[Fact]
	public void EveryAdaptedAndEveryJavaScriptOnlyStartNamesExactlyOneJavaScriptEntryAndNoEntryIsBothAdaptedAndJavaScriptOnly()
	{
		DeclaredUnsupportedCoverage[] javaScript = [.. JavaScriptDeclared];

		Assert.Equal(_ADAPTED_ENTRY_COUNT, TypeScriptInternalHooks.AdaptedEntries.Length);
		Assert.Single(TypeScriptInternalHooks.JavaScriptOnlyConstructStarts);
		Assert.All(
			TypeScriptInternalHooks.AdaptedEntries,
			adapted => Assert.Single(javaScript, entry => entry.Construct.StartsWith(adapted.JavaScriptConstructStart, StringComparison.Ordinal)));
		Assert.All(
			TypeScriptInternalHooks.JavaScriptOnlyConstructStarts,
			start => Assert.Single(javaScript, entry => entry.Construct.StartsWith(start, StringComparison.Ordinal)));
		Assert.DoesNotContain(javaScript, entry => IsJavaScriptOnly(entry) && TypeScriptInternalHooks.AdaptedEntries.Any(adapted => entry.Construct.StartsWith(adapted.JavaScriptConstructStart, StringComparison.Ordinal)));
	}

	[Fact]
	public void TheJavaScriptOnlyEntryIsTheHtmlLikeCommentAndIsNotCarriedByTypeScript()
	{
		string start = Assert.Single(TypeScriptInternalHooks.JavaScriptOnlyConstructStarts);

		Assert.StartsWith("HTML-like comments", start, StringComparison.Ordinal);
		Assert.DoesNotContain(Declared, entry => entry.Construct.StartsWith("HTML-like comments", StringComparison.Ordinal));
	}

	[Fact]
	public void NoAdaptedEntryKeepsTheJavaScriptWordingThatNamesAJavaScriptExtension()
	{
		DeclaredUnsupportedCoverage[] typeScript = Declared;

		Assert.DoesNotContain(typeScript, entry => entry.Construct.Contains(".js, .mjs, .cjs", StringComparison.Ordinal));
		Assert.DoesNotContain(typeScript, entry => entry.Construct.StartsWith("JavaScript embedded in other files", StringComparison.Ordinal));
		Assert.Contains(typeScript, entry => entry.Construct.StartsWith("a .ts, .tsx, .mts or .cts file with any line over 10,000 characters", StringComparison.Ordinal));
	}

	// ------------------------------------------------------------------------------------------
	// The entries
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryEntryHasAReasonAndNoInternalTermAndNoConstructRepeats()
	{
		DeclaredUnsupportedCoverage[] declared = Declared;

		Assert.Equal(declared.Length, declared.Select(entry => entry.Construct).Distinct(StringComparer.Ordinal).Count());
		Assert.All(declared, entry =>
		{
			Assert.False(string.IsNullOrWhiteSpace(entry.Construct));
			Assert.False(string.IsNullOrWhiteSpace(entry.Reason));
			Assert.Empty(InternalTermsIn(entry.Construct));
			Assert.Empty(InternalTermsIn(entry.Reason));
		});
	}

	[Fact]
	public void TheListHoldsTheThirtyTwoEntriesOfTypeScriptAlonePlusTheAdaptedAndInheritedOnes()
	{
		DeclaredUnsupportedCoverage[] typeScript = Declared;
		DeclaredUnsupportedCoverage[] javaScript = [.. JavaScriptDeclared];
		DeclaredUnsupportedCoverage[] alone = [.. typeScript.Where(entry => !javaScript.Any(candidate => candidate.Construct == entry.Construct) && !IsAnAdaptedReplacement(entry))];

		Assert.Equal(_TYPESCRIPT_ONLY_ENTRY_COUNT, alone.Length);
		Assert.Equal(javaScript.Length - _JAVASCRIPT_ONLY_ENTRY_COUNT + _TYPESCRIPT_ONLY_ENTRY_COUNT, typeScript.Length);
	}

	[Fact]
	public void EveryCatalogIdentifierIsBoundToExactlyOneEntryAndEveryTypeScriptSpecificEntryToExactlyOneIdentifier()
	{
		DeclaredUnsupportedCoverage[] declared = Declared;

		Assert.Equal(_typeScriptEntries.Length, _typeScriptEntries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count());
		Assert.All(_typeScriptEntries, entry =>
			Assert.Single(declared, candidate => candidate.Construct.Contains(entry.Keyword, StringComparison.Ordinal)));
		Assert.Equal(
			_TYPESCRIPT_ONLY_ENTRY_COUNT + _ADAPTED_CATALOG_ENTRY_COUNT,
			declared.Count(candidate => _typeScriptEntries.Any(entry => candidate.Construct.Contains(entry.Keyword, StringComparison.Ordinal))));
	}

	[Fact]
	public void TheTypeScriptEntriesNumberThirtyNineAndNineteenOfThemAreOfKindCost()
	{
		Assert.Equal(_OT_ENTRY_COUNT, _typeScriptEntries.Length);
		Assert.Equal(_OT_COST_ENTRY_COUNT, _costEntryIdentifiers.Length);
		Assert.All(_costEntryIdentifiers, identifier => Assert.Contains(_typeScriptEntries, entry => entry.Id == identifier));
		Assert.Equal(_OT_COST_ENTRY_COUNT, _typeScriptEntries.Count(entry => _costEntryIdentifiers.Contains(entry.Id)));
	}

	[Theory]
	[InlineData("a bare member name for the user name or the password")]
	[InlineData("built inside the project's own function")]
	public void TheTwoEntriesOfTheJavaScriptShorthandAndSplitPairCorrectionAreInheritedByTypeScriptAsTheyStand(string keyword)
	{
		DeclaredUnsupportedCoverage inherited = Assert.Single(JavaScriptDeclared, entry => entry.Construct.Contains(keyword, StringComparison.Ordinal));

		Assert.Contains(Declared, entry => entry.Construct == inherited.Construct && entry.Reason == inherited.Reason);
	}

	[Fact]
	public void TheEntryOfTheReceiverNamedHttpThatIsNotAnHttpServiceCarriesTheSignedConstructAndReasonAndIsListedOnce()
	{
		const string construct = "an Angular HttpClient or another client injected under a name that contains http, given an auth object with a username and a password, which is read as a call on the NestJS HttpService";
		const string reason = "the receiver is recognised by its name, because no page documents a naming convention for the injected service; such a call is reported at the High level although the client may not send Basic Authentication, so review each one before acting";

		DeclaredUnsupportedCoverage entry = Assert.Single(Declared, candidate => candidate.Construct == construct);

		Assert.Equal(reason, entry.Reason);
		Assert.DoesNotContain(JavaScriptDeclared, candidate => candidate.Construct == construct);
	}

	[Fact]
	public void NoEntryOfEitherLanguageListSaysThatTypeScriptFilesAreCountedAndNotAssessed()
	{
		string[] words = ["counted and not assessed", "not assessed"];

		Assert.DoesNotContain(Declared, entry => words.Any(word => entry.Construct.Contains(word, StringComparison.OrdinalIgnoreCase) || entry.Reason.Contains(word, StringComparison.OrdinalIgnoreCase)));
		Assert.DoesNotContain(JavaScriptDeclared, entry => words.Any(word => entry.Construct.Contains(word, StringComparison.OrdinalIgnoreCase) || entry.Reason.Contains(word, StringComparison.OrdinalIgnoreCase)));
	}

	// ------------------------------------------------------------------------------------------
	// The report lists the entries
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheCoverageStatementCarriesEveryDeclaredEntryOfTypeScriptSortedOrdinallyWhenTypeScriptIsEnabled()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			LanguageCatalogRegistry.BuildEmbeddedCatalog(),
			new HashSet<AssessedLanguage> { AssessedLanguage.TypeScript });
		LanguageCoverageRow row = rows.Single(candidate => candidate.LanguageName == _LANGUAGE_NAME);

		Assert.Equal(LanguageCoverageState.Supported, row.State);
		Assert.Equal(TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass.Count, row.PatternClassesSearched);
		Assert.Equal(Declared.Length, row.DeclaredUnsupportedConstructs.Length);
		Assert.Equal(
			Declared.Select(entry => entry.Construct).Order(StringComparer.Ordinal),
			row.DeclaredUnsupportedConstructs.Select(entry => entry.Construct));
	}

	[Fact]
	public void TypeScriptIsNoLongerAFileTheReportCountsAndDoesNotAssessAndTheJavaScriptRowDoesNotSayIt()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			LanguageCatalogRegistry.BuildEmbeddedCatalog(),
			new HashSet<AssessedLanguage> { AssessedLanguage.JavaScript, AssessedLanguage.TypeScript });
		LanguageCoverageRow javaScript = rows.Single(row => row.Language == AssessedLanguage.JavaScript);
		LanguageCoverageRow typeScript = rows.Single(row => row.Language == AssessedLanguage.TypeScript);

		Assert.Equal(LanguageCoverageState.Supported, javaScript.State);
		Assert.Equal(LanguageCoverageState.Supported, typeScript.State);
		Assert.DoesNotContain(javaScript.DeclaredUnsupportedConstructs, entry => entry.Construct.Contains("TypeScript", StringComparison.Ordinal) && entry.Reason.Contains("assess", StringComparison.OrdinalIgnoreCase));
		Assert.NotEmpty(typeScript.DeclaredUnsupportedConstructs);
	}

	// ------------------------------------------------------------------------------------------
	// A declared form yields no finding where the catalog says so
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("const o = { [key]: 'Basic ' + encoded };\nconst auth = 1;\n")]
	[InlineData("const response: Reply = await client.users.list({ page: 1 });\nconst auth = session;\n")]
	[InlineData("new HttpHeaders({ Authorization: header });\nconst auth = true;\n")]
	[InlineData("const options: Options = loadOptions();\nawait fetch(url, options);\nconst auth = options;\n")]
	[InlineData("const header: string = process.env.API_AUTH!;\nawait fetch(url, { headers: { Authorization: header } });\n")]
	[InlineData("const authValue: string = process.env.API_AUTH as string;\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	[InlineData("const header: string = this.value;\nawait fetch(url, { headers: { Authorization: header } });\n")]
	[InlineData("const settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization! } });\n")]
	[InlineData("const settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization as string } });\n")]
	[InlineData("const settings = {\n\tserviceAuthorization: process.env.API_AUTH!\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n")]
	[InlineData("const settings = {\n\tserviceAuthorization: process.env.API_AUTH as string\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: string;\n}\nconst settings: Settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\nsettings.serviceAuthorization = loadHeader();\n")]
	[InlineData("class Settings {\n\tserviceAuthorization: string = loadHeader();\n}\nconst settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n")]
	public void AFormTheCatalogDeclaresNotSupportedYieldsNoFinding(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}]");
	}

	[Theory]
	[InlineData("const config: ProjectConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("this.client.get(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("this.httpService.options(url, { auth: { username: userName, password: secret } });\n")]
	public void AFormTheCatalogDeclaresNotSupportedIsReportedAtLowAtMost(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.All(constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
	}

	[Theory]
	[InlineData("interface Settings {\n\tserviceAuthorization: string;\n}\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization?: string;\n}\n")]
	[InlineData("interface Settings {\n\treadonly serviceAuthorization: string;\n}\n")]
	[InlineData("class Settings {\n\tserviceAuthorization: string;\n}\n")]
	[InlineData("type Settings = {\n\tserviceAuthorization: string\n};\n")]
	public void ATypeOnlyDeclarationOfTheNameIsNoAssignmentAndTheEnvironmentValueStillReportsAtLow(string declaration)
	{
		string source = $"{declaration}const settings = {{\n\tserviceAuthorization: process.env.API_AUTH\n}};\nawait fetch(url, {{ headers: {{ Authorization: settings.serviceAuthorization }} }});\n";

		RecognizedConstruct construct = Assert.Single(new TypeScriptRecognitionHarness().Recognize(source));

		Assert.Equal(_SETTINGS_PROPERTY_PATTERN_CLASS, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
	}

	[Fact]
	public void AnInitializedClassPropertyIsAnAssignmentThatMakesTheNameIndeterminate()
	{
		const string source = "class Settings {\n\tserviceAuthorization: string = 'literal';\n}\nconst settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n";

		Assert.DoesNotContain(new TypeScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _SETTINGS_PROPERTY_PATTERN_CLASS);
	}

	[Theory]
	[InlineData("class Client {\n\tserviceAuthorization: string = process.env.SERVICE_AUTH_HEADER;\n\tsend() {\n\t\treturn fetch(url, { headers: { Authorization: this.serviceAuthorization } });\n\t}\n}\n")]
	[InlineData("class Client {\n\tprivate readonly serviceAuthorization?: string = process.env.SERVICE_AUTH_HEADER;\n\tsend() {\n\t\treturn fetch(url, { headers: { Authorization: this.serviceAuthorization } });\n\t}\n}\n")]
	[InlineData("class Client {\n\tserviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n\tsend() {\n\t\treturn fetch(url, { headers: { Authorization: this.serviceAuthorization } });\n\t}\n}\n")]
	public void AClassFieldFilledFromAnEnvironmentValueIsADeclaredFormThatIsNotReadWithOrWithoutATypeAnnotation(string source)
	{
		// A class field assigned from an environment value is not read, so it never resolves the use.
		Assert.Empty(new TypeScriptRecognitionHarness().Recognize(source));
	}

	[Theory]
	[InlineData("class Settings {\n\tserviceAuthorization = process.env.API_AUTH;\n}\nconst settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n")]
	[InlineData("class Settings {\n\tprivate serviceAuthorization: string = process.env.API_AUTH;\n}\nconst settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n")]
	public void AnEnvironmentClassFieldAndAnObjectLiteralEnvironmentAssignmentOfTheSameNameAreIndeterminate(string source)
	{
		Assert.DoesNotContain(new TypeScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _SETTINGS_PROPERTY_PATTERN_CLASS);
	}

	[Theory]
	[InlineData("class Settings {\n\tserviceAuthorization: string = 'literal';\n}\nconst settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n")]
	[InlineData("class Settings {\n\tserviceAuthorization = 'literal';\n}\nconst settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n")]
	[InlineData("class Client {\n\tserviceAuthorization: string = process.env.SERVICE_AUTH_HEADER;\n\tsend() {\n\t\tthis.serviceAuthorization = loadHeader();\n\t\treturn fetch(url, { headers: { Authorization: this.serviceAuthorization } });\n\t}\n}\n")]
	public void AnAnnotatedOrPlainClassFieldAssignedAnotherFormMakesTheNameIndeterminate(string source)
	{
		Assert.DoesNotContain(new TypeScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _SETTINGS_PROPERTY_PATTERN_CLASS);
	}

	[Fact]
	public void AProxyNamedTypedDeclarationIsReadAtMediumAsTheDeclaredCostSays()
	{
		const string source = "const proxyUrl: string = 'https://exampleUser:exampleSecret@host.example/path';\n";

		RecognizedConstruct construct = Assert.Single(new TypeScriptRecognitionHarness().Recognize(source));

		Assert.Equal("JS-013", construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void AProxyNamedUntypedDeclarationIsKeptOutAsOnJavaScriptText()
	{
		Assert.Empty(new TypeScriptRecognitionHarness().Recognize("const proxyUrl = 'https://exampleUser:exampleSecret@host.example/path';\n"));
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static bool IsPresent(DeclaredUnsupportedCoverage entry, DeclaredUnsupportedCoverage[] typeScript)
	{
		return typeScript.Any(candidate => candidate.Construct == entry.Construct && candidate.Reason == entry.Reason);
	}

	private static bool IsAdapted(DeclaredUnsupportedCoverage entry, DeclaredUnsupportedCoverage[] typeScript)
	{
		return TypeScriptInternalHooks.AdaptedEntries.Any(adapted =>
			entry.Construct.StartsWith(adapted.JavaScriptConstructStart, StringComparison.Ordinal)
			&& typeScript.Any(candidate => candidate.Construct == adapted.Replacement.Construct && candidate.Reason == adapted.Replacement.Reason));
	}

	private static bool IsJavaScriptOnly(DeclaredUnsupportedCoverage entry)
	{
		return TypeScriptInternalHooks.JavaScriptOnlyConstructStarts.Any(start => entry.Construct.StartsWith(start, StringComparison.Ordinal));
	}

	private static bool IsAnAdaptedReplacement(DeclaredUnsupportedCoverage entry)
	{
		return TypeScriptInternalHooks.AdaptedEntries.Any(adapted => adapted.Replacement.Construct == entry.Construct);
	}

	private static ImmutableArray<string> InternalTermsIn(string text)
	{
		return
		[
			.. PublicSourceIdentifierTests.InternalCoverageTerms().Where(term => text.Contains(term, StringComparison.Ordinal)),
			.. PublicSourceIdentifierTests.InternalIdentifierPatterns().SelectMany(pattern => pattern.Matches(text)).Select(match => match.Value)
		];
	}
}
