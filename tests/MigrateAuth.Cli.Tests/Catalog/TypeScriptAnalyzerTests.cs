using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The `TypeScript` analyzer against its catalog, through the analyzer the binary ships and the real recognition engine: never through a hand-built construct, because what is under test
/// is whether the expressions the analyzer declares recognize the forms the catalog states and decline the exclusions it states.
///
/// A fixture for every class that fires (the eighteen inherited `JavaScript` classes read on typed text, and the two classes that exist for `TypeScript` alone), one for every near-miss
/// that stays Low or produces nothing, and the contract the analyzer declares. Every script below is INVENTED from the catalog's own prose, with placeholder names throughout, and carries
/// no fragment, path or third-party name from any repository.
/// </summary>
public sealed class TypeScriptAnalyzerTests
{
	private const string _TYPED_CREDENTIAL_CLASS = "TS-001";
	private const string _SERVICE_AUTH_CLASS = "TS-003";
	private const string _CLIENT_OBJECT_CLASS = "JS-001";
	private const string _ENCODER_PAIR_CLASS = "JS-005";
	private const string _MAIL_TRANSPORT_CLASS = "JS-011";
	private const int _CLASS_COUNT = 21;
	private const int _INHERITED_CLASS_COUNT = 19;
	private const int _ANCHOR_COUNT = 9;
	private const int _RELIED_ON_CONSTANT_COUNT = 11;
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _CREDENTIAL_GROUP = "credential";
	private const string _NAME_GROUP = "name";
	private const string _VALUE_GROUP = "value";
	private const string _ENVIRONMENT_GROUP = "environment";
	private const string _FALLBACK_WORD_GROUP = "fallbackWord";
	private const string _TYPED_CONFIG_PREFIX = "const config: AxiosRequestConfig = ";
	private const string _TYPED_USE = "await fetch(url, { headers: { Authorization: authValue } });\n";
	private const string _DECLARATION_FILE_PATH = "src/credentials.d.ts";

	private static readonly ImmutableArray<string> _inheritedClasses =
		[.. Enumerable.Range(1, _INHERITED_CLASS_COUNT).Select(number => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"JS-{number:D3}"))];

	private static readonly ImmutableArray<string> _everyClass = [.. _inheritedClasses, "TS-001", "TS-003"];

	private static readonly ImmutableArray<string> _highClasses = ["JS-001", "JS-002", "JS-003", "JS-004", "JS-008", "JS-015", "TS-003"];

	private static readonly ImmutableArray<string> _mediumClasses = ["JS-005", "JS-006", "JS-010", "JS-012", "JS-013", "JS-017", "TS-001"];

	private static readonly ImmutableArray<string> _lowClasses = ["JS-007", "JS-009", "JS-011", "JS-014", "JS-016", "JS-018", "JS-019"];

	// ------------------------------------------------------------------------------------------
	// The contract
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheTypeScriptAnalyzerFitsTheContractWithNoViolations()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(TypeScriptRegisteredExpressions.Analyzer));
	}

	[Fact]
	public void TwentyOneClassesAreDeclaredTheNineteenInheritedOnesAndTwoOfTypeScriptAlone()
	{
		ILanguageAnalyzer analyzer = TypeScriptRegisteredExpressions.Analyzer;

		Assert.Equal(_CLASS_COUNT, analyzer.DetectionRulesByPatternClass.Count);
		Assert.Equal(_everyClass.Order(StringComparer.Ordinal), analyzer.DetectionRulesByPatternClass.Keys.Order(StringComparer.Ordinal));
		Assert.Equal(_highClasses.Order(StringComparer.Ordinal), ClassesAt(analyzer, ConfidenceLevel.High));
		Assert.Equal(_mediumClasses.Order(StringComparer.Ordinal), ClassesAt(analyzer, ConfidenceLevel.Medium));
		Assert.Equal(_lowClasses.Order(StringComparer.Ordinal), ClassesAt(analyzer, ConfidenceLevel.Low));
	}

	[Fact]
	public void TheWithdrawnIdentifierIsNeverDeclaredAndTheTwoNewClassesCarryTheirCatalogNames()
	{
		IReadOnlyDictionary<string, DetectionRule> rules = TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass;

		Assert.DoesNotContain("TS-002", rules.Keys);
		Assert.Equal("TypedBasicCredentialAnnotation", rules["TS-001"].DisplayName);
		Assert.Equal("NestHttpServiceAuthObject", rules["TS-003"].DisplayName);
		Assert.Equal(ConfidenceLevel.Medium, rules["TS-001"].BaseLevel);
		Assert.Equal(SelfSufficiency.RequiresCallPathToBeEstablished, rules["TS-001"].SelfSufficiency);
		Assert.Equal(ConfidenceLevel.High, rules["TS-003"].BaseLevel);
		Assert.Equal(SelfSufficiency.DeclaresSchemeAndCredentialCarrier, rules["TS-003"].SelfSufficiency);
	}

	[Fact]
	public void EveryExpressionIsCaseSensitiveCarriesTheInvariantOptionsAndThePolicyTimeout()
	{
		const RegexOptions required = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

		Assert.All(TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(required, entry.Value.Expression.Options & required);
			Assert.Equal(RegexOptions.None, entry.Value.Expression.Options & RegexOptions.IgnoreCase);
			Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Value.Expression.MatchTimeout);
		});
	}

	[Fact]
	public void EveryRuleAnchorsOnTheConstructGroupAndTheTwoNewClassesAnchorOnTheAuthKeyWithTheLiteralPasswordAsTheCredential()
	{
		Assert.All(TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass, entry =>
		{
			Assert.Equal(_CONSTRUCT_GROUP, entry.Value.AnchorPosition.CaptureGroupName);
			Assert.Contains(_CONSTRUCT_GROUP, entry.Value.Expression.GetGroupNames());
			Assert.Equal(DestinationCertainty.Unknown, entry.Value.DestinationCertainty);
		});

		foreach (string patternClass in new[] { "TS-001", "TS-003" })
		{
			DetectionRule rule = TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[patternClass];

			Assert.Equal(_CREDENTIAL_GROUP, rule.CredentialPosition.CaptureGroupName);
			Assert.Contains(_CREDENTIAL_GROUP, rule.Expression.GetGroupNames());
			Assert.False(rule.SymbolicVerdictInterpretation.IsSubjectToSymbolicResolution);
		}
	}

	[Fact]
	public void TheInheritedRulesAreTheJavaScriptRuleObjectsRegisteredUnderTheirOwnKeys()
	{
		IReadOnlyDictionary<string, DetectionRule> typeScript = TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass;
		IReadOnlyDictionary<string, DetectionRule> javaScript = JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass;

		Assert.All(_inheritedClasses, patternClass =>
		{
			Assert.Same(javaScript[patternClass].Expression, typeScript[patternClass].Expression);
			Assert.Equal(javaScript[patternClass].BaseLevel, typeScript[patternClass].BaseLevel);
			Assert.Equal(javaScript[patternClass].SelfSufficiency, typeScript[patternClass].SelfSufficiency);
		});
	}

	[Fact]
	public void TheAnchorsAreTheJavaScriptAnchorsAndTypeScriptAddsNone()
	{
		string[] typeScript = AnchorTextsOf(TypeScriptRegisteredExpressions.Analyzer);
		string[] javaScript = AnchorTextsOf(JavaScriptRegisteredExpressions.Analyzer);

		Assert.Equal(_ANCHOR_COUNT, typeScript.Length);
		Assert.Equal(javaScript, typeScript);
		Assert.Contains("auth", typeScript);
		Assert.Contains("password", typeScript);
	}

	[Fact]
	public void TheLexicalProfileIsNamedTypeScriptAndItsConstantDeclarationCarriesTheFourGroupsAndThePolicyBound()
	{
		LexicalProfile profile = TypeScriptRegisteredExpressions.Analyzer.LexicalProfile;
		string[] groups = profile.ConstantDeclarationExpression.GetGroupNames();

		Assert.Equal(TypeScriptRegisteredExpressions.LANGUAGE_NAME, profile.ProfileName);
		Assert.Contains(_NAME_GROUP, groups);
		Assert.Contains(_VALUE_GROUP, groups);
		Assert.Contains(_ENVIRONMENT_GROUP, groups);
		Assert.Contains(_FALLBACK_WORD_GROUP, groups);
		Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, profile.ConstantDeclarationExpression.MatchTimeout);
	}

	[Fact]
	public void TheSharedConstantsTheTypeScriptExpressionsRelyOnAreListedOnceEach()
	{
		ImmutableArray<string> relied = TypeScriptInternalHooks.ReliedOnSharedConstants;

		Assert.Equal(_RELIED_ON_CONSTANT_COUNT, relied.Length);
		Assert.Equal(relied.Length, relied.Distinct(StringComparer.Ordinal).Count());
		Assert.All(relied, constant => Assert.False(string.IsNullOrWhiteSpace(constant)));
	}

	/// <summary>No lazy bounded quantifier in any expression of `TypeScript` alone: the generator backtracks wrongly on one, so the greedy windows of the shared vocabulary stand in.</summary>
	[Fact]
	public void NoExpressionOfTypeScriptAloneCarriesALazyQuantifier()
	{
		Regex lazyQuantifier = new(@"(?<!\\)(?:[*+]|\{\d+(?:,\d*)?\})\?", RegexOptions.CultureInvariant);
		Regex[] expressions =
		[
			TypeScriptRegisteredExpressions.Analyzer.LexicalProfile.ConstantDeclarationExpression,
			TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass["TS-001"].Expression,
			TypeScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass["TS-003"].Expression
		];

		Assert.All(expressions, expression => Assert.DoesNotMatch(lazyQuantifier, expression.ToString()));
	}

	// ------------------------------------------------------------------------------------------
	// Identification
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData(".ts")]
	[InlineData(".tsx")]
	[InlineData(".mts")]
	[InlineData(".cts")]
	[InlineData(".TS")]
	[InlineData(".Tsx")]
	public void TheFourTypeScriptExtensionsAreAssessedWithoutCase(string extension)
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.True(catalog.TryGetAnalyzerForFileExtension(extension, out ILanguageAnalyzer? analyzer));
		Assert.Contains(TypeScriptRegisteredExpressions.LANGUAGE_NAME, (ISet<string>)analyzer!.LanguageIdentification.DeclaredLanguageNames);
		Assert.Equal(TypeScriptRegisteredExpressions.LANGUAGE_NAME, analyzer.LanguageIdentification.LanguageNameDerivation(extension, null));
	}

	[Fact]
	public void TheAnalyzerDeclaresExactlyTheFourExtensionsAndNoFirstLineRule()
	{
		LanguageIdentification identification = TypeScriptRegisteredExpressions.Analyzer.LanguageIdentification;

		Assert.Equal(new[] { ".cts", ".mts", ".ts", ".tsx" }, identification.FileExtensions.Order(StringComparer.Ordinal));
		Assert.Null(identification.FirstLineDetectionExpression);
	}

	[Theory]
	[InlineData("client.ts")]
	[InlineData("view.tsx")]
	[InlineData("client.mts")]
	[InlineData("client.cts")]
	[InlineData("credentials.d.ts")]
	[InlineData("types.d.mts")]
	[InlineData("legacy.d.cts")]
	public void AFileWithATypeScriptExtensionOrADeclarationFileSuffixIsAttributedToTypeScriptAndAssessed(string relativePath)
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		AttributedLanguage attributed = new LanguageAttribution(harness.Catalog).Attribute(harness.PathOf(relativePath), firstLine: null);

		Assert.Equal(TypeScriptRegisteredExpressions.LANGUAGE_NAME, attributed.LanguageName);
		Assert.True(attributed.IsRecognized);
		Assert.NotNull(attributed.Analyzer);
	}

	[Theory]
	[InlineData("#!/usr/bin/env ts-node")]
	[InlineData("#!/usr/bin/env tsx")]
	[InlineData("#!/usr/bin/env -S deno run")]
	public void AnExtensionlessRunnerScriptIsNotAttributedToTypeScript(string firstLine)
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		AttributedLanguage attributed = new LanguageAttribution(harness.Catalog).Attribute(harness.PathOf("run-client"), firstLine);

		Assert.NotEqual(TypeScriptRegisteredExpressions.LANGUAGE_NAME, attributed.LanguageName);
	}

	// ------------------------------------------------------------------------------------------
	// A fixture per class, and the level it ends at
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void EveryClassHasAFixtureAndTheFixtureFiresAtTheLevelOfItsClass()
	{
		Assert.Equal(_everyClass.AsEnumerable(), ClassFixtures().Select(fixture => fixture.PatternClass).Order(StringComparer.Ordinal));

		foreach ((string patternClass, ConfidenceLevel level, string source) in ClassFixtures())
		{
			AssertFires(source, patternClass, level);
		}
	}

	[Fact]
	public void TheAnchorOfEveryClassFixtureIsCode()
	{
		TypeScriptRecognitionHarness harness = new();

		foreach ((string patternClass, _, string source) in ClassFixtures())
		{
			FileRecognitionContext context = harness.ContextFor(source);
			RecognizedConstruct construct = harness.Recognize(source).First(candidate => candidate.PatternClassIdentifier == patternClass);

			Assert.Equal(LexicalZone.Code, context.Mask.ZoneAt(construct.AnchorOffset));
		}
	}

	[Theory]
	[InlineData("src/client.ts")]
	[InlineData("src/client.mts")]
	[InlineData("src/client.cts")]
	[InlineData("src/view.tsx")]
	public void AHeaderConstructIsFoundInEveryTypeScriptExtension(string relativePath)
	{
		AssertFires("await fetch(url, { headers: { Authorization: `Basic ${encoded}` } });\n", "JS-004", ConfidenceLevel.High, relativePath);
	}

	[Theory]
	[InlineData("const response: Reply = await axios.get(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("const response = await axios.get<Reply>(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("const response = await axios.get<Page<Item>>(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("const response = await axios.get(url, { auth: { username: userName, password: secret } } as AxiosRequestConfig);\n")]
	public void AnEnumeratedClientWithATypedResultOrAGenericListIsStillReadAtHighByItsOwnClass(string source)
	{
		AssertFires(source, "JS-001", ConfidenceLevel.High);
		AssertNeverFires(source, "JS-011");
	}

	[Fact]
	public void ATypeScriptHeaderBuiltForAnAngularClientIsReadAtTheCarrier()
	{
		AssertFires("const headers = new HttpHeaders({ Authorization: 'Basic ' + encoded });\n", "JS-004", ConfidenceLevel.High);
		AssertFires("const options = { setHeaders: { Authorization: `Basic ${encoded}` } };\n", "JS-004", ConfidenceLevel.High);
		AssertFires("const request = original.clone().set('Authorization', 'Basic ' + encoded);\n", "JS-004", ConfidenceLevel.High);
	}

	[Fact]
	public void AnEnvironmentFallbackThatIsABasicLiteralIsReadAtHighOnTypeScriptText()
	{
		AssertFires("await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'Basic dXNlcjpwYXNz' } });\n", "JS-004", ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("const authValue: string = process.env.API_AUTH;\n", "JS-018", ConfidenceLevel.Low)]
	[InlineData("const authValue: string = process.env.API_AUTH ?? '';\n", "JS-018", ConfidenceLevel.Low)]
	[InlineData("private readonly authValue: string = process.env.API_AUTH || 'neutral';\n", "JS-018", ConfidenceLevel.Low)]
	[InlineData("const authValue: string = process.env.API_AUTH || 'Basic dXNlcjpwYXNz';\n", "JS-017", ConfidenceLevel.Medium)]
	[InlineData("export const authValue: string = 'Basic ' + pair;\n", "JS-017", ConfidenceLevel.Medium)]
	[InlineData("private readonly authValue = 'Basic ' + pair;\n", "JS-017", ConfidenceLevel.Medium)]
	public void ATypedDeclarationOfTheHeldValueGivesTheSameOutcomeAsAnUntypedOne(string declaration, string expectedClass, ConfidenceLevel expectedLevel)
	{
		RecognizedConstruct only = Assert.Single(new TypeScriptRecognitionHarness().Recognize($"{declaration}{_TYPED_USE}"));

		Assert.Equal(expectedClass, only.PatternClassIdentifier);
		Assert.Equal(expectedLevel, only.Level);
	}

	[Theory]
	[InlineData("const authValue: string = process.env.API_AUTH || 'Bearer exampleToken';\n")]
	[InlineData("const authValue: string = 'Bearer ' + token;\n")]
	[InlineData("const authValue: string = process.env.API_AUTH!;\n")]
	[InlineData("const authValue: string = process.env.API_AUTH as string;\n")]
	[InlineData("const authValue: string = loadValue();\n")]
	public void ABearerValueATypeScriptTailOnTheEnvironmentReadAndACallAreSilentForTheHeldValue(string declaration)
	{
		Assert.Empty(new TypeScriptRecognitionHarness().Recognize($"{declaration}{_TYPED_USE}"));
	}

	[Fact]
	public void ATypedConstantIsCountedByTheTypeScriptDeclarationAndAnUntypedOneToo()
	{
		const string source = "const typed: string = 'x';\nconst plain = 'y';\nclass Holder {\n\tprivate readonly field: string = 'z';\n\tstatic other = 'w';\n\tcomputed: number = compute();\n}\nconst after = 1;\n";
		LexicalProfile profile = TypeScriptRegisteredExpressions.Analyzer.LexicalProfile;
		FileSymbolTable table = FileSymbolTable.Collect(profile, source, new LexicalScanner(profile).Scan(source));

		Assert.True(table.TryResolveUnambiguously("typed", int.MaxValue, out string typed));
		Assert.Equal("x", typed);
		Assert.True(table.TryResolveUnambiguously("plain", int.MaxValue, out string plain));
		Assert.Equal("y", plain);
		Assert.True(table.TryResolveUnambiguously("field", int.MaxValue, out string field));
		Assert.Equal("z", field);
		Assert.True(table.TryResolveUnambiguously("other", int.MaxValue, out string other));
		Assert.Equal("w", other);
		Assert.Equal(0, table.DeclarationCountOf("computed"));
	}

	// ------------------------------------------------------------------------------------------
	// The two classes of TypeScript alone: what fires
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("const config: InternalAxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("const defaults: CreateAxiosDefaults = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("const config: Axios.AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("const settings = { auth: { username: 'exampleUser', password: 'exampleSecret' } } as AxiosRequestConfig;\n")]
	[InlineData("const settings = { auth: { username: 'exampleUser', password: 'exampleSecret' } } satisfies AxiosRequestConfig;\n")]
	[InlineData("const settings = <AxiosRequestConfig>{ auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("const settings = { auth: <AxiosBasicCredentials>{ username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("const settings = { auth: { username: 'exampleUser', password: 'exampleSecret' } as AxiosBasicCredentials };\n")]
	[InlineData("const settings = { auth: { username: 'exampleUser', password: 'exampleSecret' } satisfies AxiosBasicCredentials };\n")]
	[InlineData("function send(config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } }) { return config; }\n")]
	[InlineData("class Sender {\n\tprivate readonly config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n}\n")]
	[InlineData("const settings = { auth: { username: 'exampleUser', password: 'exampleSecret' } } as AxiosRequestConfig<Body>;\n")]
	public void ATypedCredentialObjectNamingACitedClientTypeIsReadAtMediumByTheTypedClass(string source)
	{
		AssertFires(source, "TS-001", ConfidenceLevel.Medium);
		AssertNeverFires(source, "JS-011");
	}

	[Theory]
	[InlineData("const reply = await firstValueFrom(this.httpService.get<Reply>(url, { auth: { username: userName, password: secret } }));\n")]
	[InlineData("const reply = await firstValueFrom(this.httpService.post(url, body, { auth: { username: 'exampleUser', password: 'exampleSecret' } }));\n")]
	[InlineData("const reply = await firstValueFrom(this.http.put<Reply>(url, body, { auth: { username: userName, password: secret } }));\n")]
	[InlineData("this.httpService.request({ url, auth: { username: userName, password: secret } });\n")]
	[InlineData("this.httpService.postForm(url, data, { auth: { username: userName, password: secret } });\n")]
	[InlineData("this.httpService.query(url, { auth: { username: userName, password: secret } });\n")]
	public void AnAuthObjectGivenToAnHttpNamedReceiverAndADocumentedMethodIsReadAtHighByTheServiceClass(string source)
	{
		AssertFires(source, "TS-003", ConfidenceLevel.High);
		AssertNeverFires(source, "JS-011");
	}

	[Fact]
	public void ALiteralWholePasswordCarriesTheLiteralSignalAndOneFollowedByATypeScriptTailDoesNot()
	{
		RecognizedConstruct whole = Single("const config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n", "TS-001");
		RecognizedConstruct cast = Single("const config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' as string } };\n", "TS-001");
		RecognizedConstruct satisfied = Single("const config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' satisfies Secret } };\n", "TS-001");
		RecognizedConstruct service = Single("this.httpService.get(url, { auth: { username: userName, password: 'exampleSecret' } });\n", "TS-003");
		RecognizedConstruct serviceCast = Single("this.httpService.get(url, { auth: { username: userName, password: 'exampleSecret' as string } });\n", "TS-003");

		Assert.True(whole.LiteralCredentialSignal);
		Assert.False(cast.LiteralCredentialSignal);
		Assert.False(satisfied.LiteralCredentialSignal);
		Assert.True(service.LiteralCredentialSignal);
		Assert.False(serviceCast.LiteralCredentialSignal);
		Assert.Equal(ConfidenceLevel.Medium, cast.Level);
		Assert.Equal(ConfidenceLevel.High, serviceCast.Level);
	}

	// ------------------------------------------------------------------------------------------
	// TS-001 reads any value form and a shorthand member, TS-003 reads username and password only and a shorthand member,
	// JS-001 and JS-005 are the shared classes read on TypeScript text. Every positive has its neighbour controls, and both outcomes are asserted.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username: userName, password: secret } };\n")]
	[InlineData("const settings: AxiosRequestConfig = { baseURL: host, auth: { username: accountName, password: this.clientSecret } };\n")]
	[InlineData("const options = { auth: { username: dto.username, password: dto.password } } satisfies AxiosRequestConfig;\n")]
	[InlineData("const options = { auth: <AxiosBasicCredentials>{ username: userName, password: secret } };\n")]
	[InlineData("const options = { auth: { username: userName, password: secret } as AxiosBasicCredentials };\n")]
	[InlineData("const options = { auth: { username: userName, password: secret } satisfies AxiosBasicCredentials };\n")]
	[InlineData("const options = { auth: { username: loadName(), password: `${prefix}${suffix}` } } as AxiosRequestConfig;\n")]
	public void ATypedCredentialObjectWhoseMembersAreNamesOrMemberAccessesIsReadAtMediumByTheTypedClassWithNoSecretWarning(string source)
	{
		RecognizedConstruct construct = Single(source, _TYPED_CREDENTIAL_CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
		AssertNeverFires(source, _MAIL_TRANSPORT_CLASS);
	}

	[Theory]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: secret } };\n", false)]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: userName, password: 'exampleSecret' } };\n", true)]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n", true)]
	[InlineData("const config = { auth: { username: 'exampleUser', password: 'exampleSecret' } } satisfies AxiosRequestConfig;\n", true)]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: userName, password } };\n", false)]
	public void TheSecretWarningOfTheTypedClassIsRaisedOnlyWhenThePasswordMemberIsAWholeLiteral(string source, bool expectedWarning)
	{
		RecognizedConstruct construct = Single(source, _TYPED_CREDENTIAL_CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.Equal(expectedWarning, construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const config: AxiosRequestConfig = { baseURL: host, auth: { username, password } };\n")]
	[InlineData("const config: AxiosRequestConfig = { auth: {\n\tusername,\n\tpassword\n} };\n")]
	[InlineData("const options = { auth: { username: accountName, password } } satisfies AxiosRequestConfig;\n")]
	[InlineData("const options = { auth: { password, username } } as AxiosRequestConfig;\n")]
	[InlineData("const options = { auth: <AxiosBasicCredentials>{ username, password } };\n")]
	public void ATypedCredentialObjectWithAShorthandMemberIsReadAtMediumByTheTypedClassWithNoSecretWarning(string source)
	{
		RecognizedConstruct construct = Single(source, _TYPED_CREDENTIAL_CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const config: ProjectConfig = { auth: { username: userName, password: secret } };\n")]
	[InlineData("type Settings = AxiosRequestConfig & { auth: { username: string; password: string } };\n")]
	[InlineData("type Settings = { auth: { username: string; password: string } };\n")]
	[InlineData("interface Settings extends AxiosRequestConfig {\n\tauth: { username: string; password: string };\n}\n")]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: userName } };\n")]
	[InlineData("const config: AxiosRequestConfig = { auth: { password: secret } };\n")]
	[InlineData("const config: AxiosRequestConfig = { proxy: { auth: { username: userName, password: secret } } };\n")]
	[InlineData("const config: AxiosRequestConfig = { headers: { Accept: 'text/plain' }, auth: { username: userName, password: secret } };\n")]
	[InlineData("const config: AxiosRequestConfig = { auth: { label: username, password } };\n")]
	[InlineData("const config: AxiosRequestConfig = { auth: { username, label: password } };\n")]
	[InlineData("const config: AxiosRequestConfig = { auth: { username?: userName, password?: secret } };\n")]
	[InlineData("const config: AxiosRequestConfig = { data: { username, password } };\n")]
	[InlineData("const config: AxiosRequestConfig = { auth: { user: userName, pass: secret } };\n")]
	public void ANeighbourOfTheAnyValueTypedObjectIsNotReadAtHighOrMediumByTheTypedClass(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _TYPED_CREDENTIAL_CLASS);
		Assert.DoesNotContain(constructs, construct => construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("const reply = await firstValueFrom(this.httpService.get<Reply>(url, { auth: { username, password } }));\n")]
	[InlineData("const first = await firstValueFrom(this.httpService.post(url, body, { auth: { username: accountName, password } }));\n")]
	[InlineData("const first = await firstValueFrom(this.httpService.post(url, body, { auth: { username, password: secret } }));\n")]
	[InlineData("this.httpService.put(url, body, { auth: {\n\tpassword,\n\tusername\n} });\n")]
	public void AnAuthObjectWithAShorthandMemberOnTheInjectedServiceIsReadAtHighByTheServiceClassWithNoSecretWarning(string source)
	{
		RecognizedConstruct construct = Single(source, _SERVICE_AUTH_CLASS);

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
		AssertNeverFires(source, _MAIL_TRANSPORT_CLASS);
	}

	[Theory]
	[InlineData("await firstValueFrom(this.httpService.post(url, { username, password }));\n")]
	[InlineData("await firstValueFrom(this.httpService.get(url, { auth: { username } }));\n")]
	[InlineData("await firstValueFrom(this.httpService.get(url, { auth: { password } }));\n")]
	[InlineData("await firstValueFrom(this.httpService.get(url, { auth: { label: username, password } }));\n")]
	[InlineData("await firstValueFrom(this.httpService.get(url, { auth: { username, nested: { password } } }));\n")]
	[InlineData("await firstValueFrom(this.httpService.get(url, { auth: { username.value, password } }));\n")]
	public void ANeighbourOfAShorthandPairOnTheInjectedServiceIsNotReadByTheServiceClass(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _SERVICE_AUTH_CLASS);
		Assert.DoesNotContain(constructs, construct => construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("this.httpClient.post(url, body, { auth: { user: userName, pass: secret } });\n")]
	[InlineData("http.put(url, body, { auth: { user: userName, pass: secret } });\n")]
	[InlineData("const client = buildClient();\nclient.http.get(url, { auth: { user: userName, pass: secret } });\n")]
	[InlineData("this.httpService.get(url, { auth: { user: userName, password: secret } });\n")]
	[InlineData("this.httpService.get(url, { auth: { username: userName, pass: secret } });\n")]
	public void AnAuthObjectWithUserAndPassOnAnHttpNamedReceiverIsNotReadAtHighOrMediumAndIsReadAtLowByTheGenericAuthObjectClass(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _SERVICE_AUTH_CLASS);
		Assert.DoesNotContain(constructs, construct => construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium);
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _MAIL_TRANSPORT_CLASS && construct.Level == ConfidenceLevel.Low);
	}

	[Theory]
	[InlineData("this.httpService.get(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("this.apiHTTP.get(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("this.accountHttp.post(url, body, { auth: { username: userName, password: secret } });\n")]
	[InlineData("this.HTTP_SERVICE.get(url, { auth: { username, password } });\n")]
	public void AnHttpNamedReceiverWithUsernameAndPasswordIsStillReadAtHighByTheServiceClass(string source)
	{
		AssertFires(source, _SERVICE_AUTH_CLASS, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("const http = buildFetchClient();\nreturn http.put(url, body, { auth: { user: userName, pass: secret } });\n")]
	[InlineData("const http = buildFetchClient();\nhttp.post(url, body, { auth: { user: userName, pass: secret } });\n")]
	public void ALocalHttpNamedClientBuiltByAFunctionCallWithUserAndPassIsNotReadAtHighOrMediumByTheServiceClass(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _SERVICE_AUTH_CLASS && construct.Level is ConfidenceLevel.High or ConfidenceLevel.Medium);
	}

	[Theory]
	[InlineData("const http = buildFetchClient();\nreturn http.put(url, body, { auth: { username: userName, password: secret } });\n")]
	[InlineData("const http = buildFetchClient();\nhttp.post(url, body, { auth: { username: userName, password: secret } });\n")]
	public void ALocalHttpNamedClientBuiltByAFunctionCallWithUsernameAndPasswordIsStillReadAtHighByTheServiceClass(string source)
	{
		AssertFires(source, _SERVICE_AUTH_CLASS, ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("const response = await axios.get(url, { auth: { username, password } });\n")]
	[InlineData("const response: Reply = await axios.get<Reply>(url, { auth: { username: userName, password } });\n")]
	[InlineData("const client = axios.create({ baseURL: host, auth: { username, password: secret } });\n")]
	public void AShorthandMemberInTheAuthOptionOfAnAxiosCallIsReadAtHighByTheSharedClientClassOnTypeScriptText(string source)
	{
		RecognizedConstruct construct = Single(source, _CLIENT_OBJECT_CLASS);

		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("await axios.post(url, { username, password });\n")]
	[InlineData("const response: Reply = await axios.get<Reply>(url, { auth: { label: username, password: secret } });\n")]
	[InlineData("await axios.get(url, { auth: { username } });\n")]
	public void ANeighbourOfAShorthandPairOnTypeScriptTextIsNotReadByTheSharedClientClass(string source)
	{
		AssertNeverFires(source, _CLIENT_OBJECT_CLASS);
	}

	[Theory]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser:' + 'exampleSecret');\n")]
	[InlineData("const header: string = 'Basic ' + btoa(\"exampleUser:\" + \"exampleSecret\" + suffix);\n")]
	public void APairSplitAcrossTwoLiteralsOnTypeScriptTextIsReadAtMediumByTheSharedEncoderClassWithNoSecretWarning(string source)
	{
		RecognizedConstruct construct = Single(source, _ENCODER_PAIR_CLASS);

		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	[Theory]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser:' + secret);\n")]
	[InlineData("const header: string = 'Basic ' + btoa('exampleUser' + 'exampleSecret');\n")]
	public void ANeighbourOfASplitPairOnTypeScriptTextIsNotReadByTheSharedEncoderClass(string source)
	{
		AssertNeverFires(source, _ENCODER_PAIR_CLASS);
	}

	// ------------------------------------------------------------------------------------------
	// The near-misses: kept out, or Low
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("interface Login {\n\tusername: string;\n\tpassword: string;\n}\n")]
	[InlineData("type Credentials = { user: string; pass: string };\nclass LoginForm {\n\tpassword!: string;\n\tusername!: string;\n}\n")]
	[InlineData("@Entity()\nexport class LoginDto {\n\t@IsString() username: string;\n\t@IsString() password: string;\n}\n")]
	[InlineData("type Scheme = 'Basic' | 'Bearer';\ntype Mode = 'Basic';\n")]
	[InlineData("interface Headers {\n\tAuthorization: 'Basic' | 'Bearer';\n}\n")]
	[InlineData("type Header = `Basic ${string}`;\ninterface Headers {\n\tAuthorization: `Basic ${number}`;\n}\n")]
	[InlineData("enum Scheme {\n\tBasic = 'Basic',\n\tBearer = 'Bearer',\n\tAuthorization = 'Authorization'\n}\n")]
	[InlineData("const o: AxiosBasicCredentials = { username: 'exampleUser', password: 'exampleSecret' };\n")]
	[InlineData("await fetch(url, { headers: { Authorization: `Bearer ${token}` } });\n")]
	[InlineData("const header = request.headers['authorization'];\nif (!header?.startsWith('Basic ')) {\n\tthrow new UnauthorizedException();\n}\n")]
	[InlineData("const decoded = Buffer.from(token, 'base64').toString();\nconst text: string = atob(value);\n")]
	[InlineData("const o: Options = { headers: { 'Proxy-Authorization': 'Basic x' } };\n")]
	[InlineData("fetch(url, { credentials: 'include' });\n")]
	public void ANearMissThatTheCatalogKeepsOutProducesNoConstructAtAll(string source)
	{
		AssertSilent(source);
	}

	[Theory]
	[InlineData("const transport: Transport = openMailTransport({ host: mailHost, auth: { user: userName, pass: secret } });\n")]
	[InlineData("const config: ProjectConfig = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("import type { AxiosRequestConfig as Cfg } from 'axios';\nconst config: Cfg = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("const config: AxiosRequestConfig<Body> = { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("const config: AxiosRequestConfig = { headers: { Accept: 'text/plain' }, auth: { username: 'exampleUser', password: 'exampleSecret' } };\n")]
	[InlineData("function make(): AxiosRequestConfig {\n\treturn { auth: { username: 'exampleUser', password: 'exampleSecret' } };\n}\n")]
	[InlineData("this.client.get(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("this.httpService.options(url, { auth: { username: userName, password: secret } });\n")]
	[InlineData("const reply = await this.httpService.axiosRef.get(url, { auth: { username: userName, password: secret } });\n")]
	public void ANearMissThatTheCatalogRoutesToManualReviewIsRecognizedAtLowAndNeverAtHighOrMedium(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.NotEmpty(constructs);
		Assert.True(constructs.All(construct => construct.Level is ConfidenceLevel.Low), $"[{source}] produced [{Describe(constructs)}]");
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "JS-011");
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier is "TS-001" or "TS-003");
	}

	[Fact]
	public void AnAngularTestControllerThatReadsTheExpectedRequestHeaderIsKeptOutAndIsNotReportedAsATestConstruct()
	{
		const string source = "const req = httpMock.expectOne(url);\nexpect(req.request.headers.get('Authorization')).toBe(expected);\n";

		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source, "src/client.spec.ts");

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{Describe(constructs)}]");
	}

	[Fact]
	public void ADeclarationFileThatHoldsCredentialMembersAndHeaderUnionsReportsNothingAboveLow()
	{
		const string source = "export interface Credentials {\n\tusername: string;\n\tpassword: string;\n}\nexport interface Request {\n\tauth?: Credentials;\n\theaders: { Authorization: 'Basic' | 'Bearer' };\n}\n";

		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source, _DECLARATION_FILE_PATH);

		Assert.All(constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
	}

	[Fact]
	public void AnHttpNamedReceiverThatIsAnotherClientIsReadAtHighAsTheDeclaredHeuristicSays()
	{
		AssertFires("httpClient.post(url, { auth: { username: userName, password: secret } });\n", _SERVICE_AUTH_CLASS, ConfidenceLevel.High);
	}

	[Fact]
	public void TheLowClassesCarryNoTemplate()
	{
		CandidateSelectionAndRecommendation element = TypeScriptRegisteredExpressions.Analyzer.CandidateSelectionAndRecommendation;

		Assert.All(_lowClasses, patternClass => Assert.Null(element.TemplateProvider(patternClass, CredentialFlow.ClientCredentials)));
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private static string[] AnchorTextsOf(ILanguageAnalyzer analyzer)
	{
		return [.. analyzer.CandidateSelectionAndRecommendation.PreselectionAnchors.Select(anchor => anchor.Text).Order(StringComparer.Ordinal)];
	}

	private static ImmutableArray<string> ClassesAt(ILanguageAnalyzer analyzer, ConfidenceLevel level)
	{
		return [.. analyzer.DetectionRulesByPatternClass.Where(entry => entry.Value.BaseLevel == level).Select(entry => entry.Key).Order(StringComparer.Ordinal)];
	}

	private static RecognizedConstruct Single(string source, string patternClass)
	{
		return new TypeScriptRecognitionHarness().Recognize(source).Single(construct => construct.PatternClassIdentifier == patternClass);
	}

	private static void AssertFires(string source, string patternClass, ConfidenceLevel level, string relativePath = TypeScriptRecognitionHarness.UNDEMOTED_RELATIVE_PATH)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source, relativePath);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
			$"{patternClass} did not fire at {level} on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertNeverFires(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.False(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
			$"{patternClass} fired on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertSilent(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}

	/// <summary>One invented fixture per class, on typed text where the catalog keeps the typed form, with the level the catalog gives the class.</summary>
	internal static ImmutableArray<(string PatternClass, ConfidenceLevel Level, string Source)> ClassFixtures()
	{
		return
		[
			("JS-001", ConfidenceLevel.High, "const response = await axios.get<Reply>(url, { auth: { username: userName, password: secret } });\n"),
			("JS-002", ConfidenceLevel.High, "const response: Reply = await superagent.get(url).auth(userName, secret);\n"),
			("JS-003", ConfidenceLevel.High, "const response: Response = await fetch('https://exampleUser:exampleSecret@host.example/path');\n"),
			("JS-004", ConfidenceLevel.High, "const headers = new HttpHeaders({ Authorization: 'Basic ' + encoded });\n"),
			("JS-005", ConfidenceLevel.Medium, "const header: string = 'Basic ' + btoa(userName + ':' + secret);\n"),
			("JS-006", ConfidenceLevel.Medium, "const header: string = 'Basic ' + Buffer.from(`${userName}:${secret}`).toString('base64');\n"),
			("JS-007", ConfidenceLevel.Low, "function buildBasicHeader(userName: string, secret: string): Record<string, string> {\n\treturn { Authorization: 'Basic ' + btoa(userName + ':' + secret) };\n}\n"),
			("JS-008", ConfidenceLevel.High, "const req = http.request({ host: hostName, path: '/path', auth: `${userName}:${secret}` }, handler);\n"),
			("JS-009", ConfidenceLevel.Low, "xhr.open('GET', url, true, userName, secret);\n"),
			("JS-010", ConfidenceLevel.Medium, "const target = new URL(base);\ntarget.username = userName;\ntarget.password = secret;\n"),
			("JS-011", ConfidenceLevel.Low, "const transport: Transport = openMailTransport({ host: mailHost, port: mailPort, auth: { user: userName, pass: secret } });\n"),
			("JS-012", ConfidenceLevel.Medium, "const response: Reply = await superagent.get(url).auth(credentialPair);\n"),
			("JS-013", ConfidenceLevel.Medium, "const baseUrl: string = 'https://exampleUser:exampleSecret@host.example/path';\n"),
			("JS-014", ConfidenceLevel.Low, "$.ajax({ url: apiUrl, username: userName, password: secret });\n"),
			("JS-015", ConfidenceLevel.High, "const response = await got<Reply>(url, { username: userName, password: secret });\n"),
			("JS-016", ConfidenceLevel.Low, "request.get<Reply>(url, { auth: { user: userName, pass: secret, sendImmediately: false } });\n"),
			("JS-017", ConfidenceLevel.Medium, "const encodedHeader: string = 'Basic ' + encodedPair;\nawait fetch(url, { headers: { Authorization: encodedHeader } });\n"),
			("JS-018", ConfidenceLevel.Low, "await fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n"),
			("JS-019", ConfidenceLevel.Low, "const settings = {\n\tserviceAuthorization: process.env.API_AUTH\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n"),
			("TS-001", ConfidenceLevel.Medium, $"{_TYPED_CONFIG_PREFIX}{{ baseURL: host, auth: {{ username: 'exampleUser', password: 'exampleSecret' }} }};\n"),
			("TS-003", ConfidenceLevel.High, "const reply = await firstValueFrom(this.httpService.get<Reply>(url, { auth: { username: userName, password: secret } }));\n")
		];
	}
}
