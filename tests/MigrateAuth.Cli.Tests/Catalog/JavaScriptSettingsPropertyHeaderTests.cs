using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// AN `Authorization` HEADER WHOSE WHOLE VALUE IS A SETTINGS PROPERTY FILLED FROM AN ENVIRONMENT VALUE (the class `JS-019`):
/// read at Low only, for manual review, with no recommendation, in both languages. Each of the five environment forms in the object-literal and the
/// property-assignment shapes, the receiver `this`, each of the five carriers, the generic shape of an object-literal member read through a fetch header and through a defaults header, every near-miss by kind (a `headers` receiver, a flag key, a
/// non-credential environment name, a header other than the whole word, an inbound read, a property assigned twice, a literal assignment of the same name), the `Bearer` control inside the window and
/// one thirty-one lines away, a member assigned after its use, a member with a scheme fallback and the declared variants (asserted as no finding) are pinned, and `JS-018` and `JS-019` are disjoint.
/// Every script is invented with placeholder names, through the shipped analyzers and the real recognition engine.
/// </summary>
public sealed class JavaScriptSettingsPropertyHeaderTests
{
	private const string _CLASS = "JS-019";
	private const string _ENVIRONMENT_CLASS = "JS-018";
	private const string _JAVASCRIPT = "JavaScript";
	private const string _TYPESCRIPT = "TypeScript";
	private const string _MEMBER_SHAPE_OBJECT = "const settings = {{\n\tserviceAuthorization: {0}\n}};\n";
	private const string _MEMBER_SHAPE_ASSIGNMENT = "const settings = {{}};\nsettings.serviceAuthorization = {0};\n";
	private const string _MEMBER_SHAPE_THIS = "class Settings {{\n\tconstructor() {{\n\t\tthis.serviceAuthorization = {0};\n\t}}\n}}\n";
	private const string _USE = "await fetch(url, {{ headers: {{ Authorization: settings.serviceAuthorization }} }});\n";
	private const string _MEMBER = "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n};\n";
	private const string _USE_LINE = "await fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n";
	private const string _BEARER_WRITE = "await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n";
	private const string _FILLER_LINE = "const unrelated = compute();\n";
	private const int _FILLER_LINES_INSIDE_THE_BEARER_WINDOW = 29;
	private const int _FILLER_LINES_AT_THE_BEARER_WINDOW_EDGE = 30;
	private const int _LINE_LENGTH_INSIDE_THE_LINE_BOUND = 400;
	private const int _LINE_LENGTH_BEYOND_THE_LINE_BOUND = 401;
	private const int _COMMENT_MARKER_LENGTH = 2;

	private static readonly string[] _languages = [_JAVASCRIPT, _TYPESCRIPT];

	private static readonly string[] _environmentForms =
	[
		"process.env.SERVICE_AUTH_HEADER",
		"process.env['SERVICE_AUTH_HEADER']",
		"import.meta.env.VITE_SERVICE_TOKEN",
		"Bun.env.SERVICE_SECRET",
		"Deno.env.get('SERVICE_AUTH_HEADER')"
	];

	private static readonly string[] _carrierUses =
	[
		"await fetch(url, { headers: { Authorization: settings.serviceAuthorization } });\n",
		"client.setRequestHeader('Authorization', settings.serviceAuthorization);\n",
		"const h = new Headers({ Authorization: settings.serviceAuthorization });\n",
		"h.set('Authorization', settings.serviceAuthorization);\n",
		"axios.defaults.headers.common['Authorization'] = settings.serviceAuthorization;\n"
	];

	public static IEnumerable<object[]> EveryLanguageEnvironmentFormAndShape()
	{
		foreach (string language in _languages)
		{
			foreach (string environmentForm in _environmentForms)
			{
				foreach (string shape in new[] { _MEMBER_SHAPE_OBJECT, _MEMBER_SHAPE_ASSIGNMENT, _MEMBER_SHAPE_THIS })
				{
					yield return [language, $"{string.Format(CultureInfo.InvariantCulture, shape, environmentForm)}{string.Format(CultureInfo.InvariantCulture, _USE)}"];
				}
			}
		}
	}

	public static IEnumerable<object[]> EveryLanguageAndCarrier()
	{
		foreach (string language in _languages)
		{
			foreach (string use in _carrierUses)
			{
				yield return [language, $"{_MEMBER}{use}"];
			}
		}
	}

	public static IEnumerable<object[]> EveryLanguage()
	{
		foreach (string language in _languages)
		{
			yield return [language];
		}
	}

	// ---- The positives: Low only, for manual review ----

	[Theory]
	[MemberData(nameof(EveryLanguageEnvironmentFormAndShape))]
	public void EachEnvironmentFormInEachShapeFeedsOneFindingAtLowAndNothingElse(string language, string source)
	{
		AssertOnlyTheSettingsPropertyClassAtLow(language, source, 1);
	}

	[Theory]
	[MemberData(nameof(EveryLanguageAndCarrier))]
	public void EachOfTheFiveCarriersReadsTheSettingsPropertyAtLow(string language, string source)
	{
		AssertOnlyTheSettingsPropertyClassAtLow(language, source, 1);
	}

	[Theory]
	[MemberData(nameof(EveryLanguage))]
	public void TheGenericShapeOfTheSettingsPropertyReadsAtLowInBothLanguagesAndNothingItAddsReachesHighOrMedium(string language)
	{
		const string defaultsHeaderUse = "client.defaults.headers.common['Authorization'] = config.serviceAuthorization;\n";
		const string source = $"{_MEMBER}{_USE_LINE}{defaultsHeaderUse}";

		ImmutableArray<RecognizedConstruct> constructs = Recognize(language, source);

		Assert.Equal(2, constructs.Length);
		Assert.All(constructs, construct => Assert.Equal(_CLASS, construct.PatternClassIdentifier));
		Assert.All(constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
		Assert.All(constructs, construct => Assert.False(construct.LiteralCredentialSignal));
	}

	[Theory]
	[InlineData(_JAVASCRIPT)]
	[InlineData(_TYPESCRIPT)]
	public void AMemberAssignedAfterItsUseIsFoundBecauseResolutionIsOrderFree(string language)
	{
		AssertOnlyTheSettingsPropertyClassAtLow(language, $"{_USE_LINE}{_MEMBER}", 1);
	}

	[Theory]
	[InlineData(_JAVASCRIPT)]
	[InlineData(_TYPESCRIPT)]
	public void AReceiverThatIsAChainOfUpToEightIdentifiersIsReadAndNineIsNot(string language)
	{
		const string eight = "await fetch(url, { headers: { Authorization: a.b.c.d.e.f.g.h.serviceAuthorization } });\n";
		const string nine = "await fetch(url, { headers: { Authorization: a.b.c.d.e.f.g.h.i.serviceAuthorization } });\n";

		AssertOnlyTheSettingsPropertyClassAtLow(language, $"{_MEMBER}{eight}", 1);
		AssertSilent(language, $"{_MEMBER}{nine}");
	}

	[Theory]
	[InlineData(_JAVASCRIPT, "\tSERVICE_AUTHORIZATION: process.env.SERVICE_AUTH_HEADER\n", "settings.SERVICE_AUTHORIZATION")]
	[InlineData(_TYPESCRIPT, "\tAuthorization: process.env.SERVICE_AUTH_HEADER\n", "settings.Authorization")]
	public void ThePropertyNameIsReadInAnyCase(string language, string memberLine, string value)
	{
		string source = $"const settings = {{\n{memberLine}}};\nawait fetch(url, {{ headers: {{ Authorization: {value} }} }});\n";

		AssertOnlyTheSettingsPropertyClassAtLow(language, source, 1);
	}

	// ---- The near-misses, by kind: no finding at any level ----

	[Theory]
	[InlineData("headers receiver", "await fetch(url, { headers: { Authorization: config.headers.serviceAuthorization } });\n")]
	[InlineData("X-Authorization", "await fetch(url, { headers: { 'X-Authorization': settings.serviceAuthorization } });\n")]
	[InlineData("Proxy-Authorization", "await fetch(url, { headers: { 'Proxy-Authorization': settings.serviceAuthorization } });\n")]
	[InlineData("inbound read", "const value = req.headers.authorization;\nconst other = settings.serviceAuthorization;\n")]
	[InlineData("another header", "await fetch(url, { headers: { Accept: settings.serviceAuthorization } });\n")]
	[InlineData("an index on the property", "await fetch(url, { headers: { Authorization: settings.serviceAuthorization[0] } });\n")]
	[InlineData("a call on the property", "await fetch(url, { headers: { Authorization: settings.serviceAuthorization() } });\n")]
	[InlineData("a further segment", "await fetch(url, { headers: { Authorization: settings.serviceAuthorization.value } });\n")]
	[InlineData("a quoted value", "await fetch(url, { headers: { Authorization: 'settings.serviceAuthorization' } });\n")]
	public void ANearMissOfTheUseSideIsSilentEvenWithTheMemberRecordInTheFile(string kind, string use)
	{
		foreach (string language in _languages)
		{
			AssertSilent(language, $"{_MEMBER}{use}", kind);
		}
	}

	[Theory]
	[InlineData("a flag key", "\trequireAuthorization: process.env.SERVICE_AUTH_FLAG", "settings.requireAuthorization")]
	[InlineData("a skip flag key", "\tskipAuthorization: process.env.SERVICE_AUTH_FLAG", "settings.skipAuthorization")]
	[InlineData("an enable flag key", "\tenableAuthorization: process.env.SERVICE_AUTH_FLAG", "settings.enableAuthorization")]
	[InlineData("a host name", "\tserviceAuthorization: process.env.SERVICE_HOST", "settings.serviceAuthorization")]
	[InlineData("an address name", "\tserviceAuthorization: process.env.SERVICE_ADDRESS", "settings.serviceAuthorization")]
	[InlineData("an endpoint name", "\tserviceAuthorization: process.env.SERVICE_ENDPOINT", "settings.serviceAuthorization")]
	[InlineData("a port name", "\tserviceAuthorization: process.env.SERVICE_PORT", "settings.serviceAuthorization")]
	public void ANearMissOfTheMemberSideIsSilent(string kind, string memberLine, string value)
	{
		string source = $"const settings = {{\n{memberLine}\n}};\nawait fetch(url, {{ headers: {{ Authorization: {value} }} }});\n";

		foreach (string language in _languages)
		{
			AssertSilent(language, source, kind);
		}
	}

	[Theory]
	[InlineData("a property assigned twice", "const first = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n};\nconst second = {\n\tserviceAuthorization: process.env.OTHER_AUTH_HEADER\n};\n")]
	[InlineData("a literal assignment of the same name", "const first = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n};\nconst second = {\n\tserviceAuthorization: 'static'\n};\n")]
	[InlineData("a call assignment of the same name", "settings.serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\nsettings.serviceAuthorization = loadValue();\n")]
	[InlineData("a literal alone", "const settings = {\n\tserviceAuthorization: 'static'\n};\n")]
	[InlineData("no assignment at all", "const settings = loadSettings();\n")]
	public void AMemberAssignedTwiceInAnyFormOrNeverFromTheEnvironmentIsIndeterminateAndSilent(string kind, string members)
	{
		foreach (string language in _languages)
		{
			AssertSilent(language, $"{members}{_USE_LINE}", kind);
		}
	}

	// ---- The Bearer guard, worded as built ----

	[Theory]
	[InlineData(_JAVASCRIPT, _FILLER_LINES_INSIDE_THE_BEARER_WINDOW, false)]
	[InlineData(_JAVASCRIPT, _FILLER_LINES_AT_THE_BEARER_WINDOW_EDGE, true)]
	[InlineData(_TYPESCRIPT, _FILLER_LINES_INSIDE_THE_BEARER_WINDOW, false)]
	[InlineData(_TYPESCRIPT, _FILLER_LINES_AT_THE_BEARER_WINDOW_EDGE, true)]
	public void ABearerWriteBeforeTheUseSilencesTheClassWithTwentyNineFillerLinesAndAThirtyFirstLineAwayDoesNot(string language, int fillerLines, bool classReads)
	{
		AssertReadsOnlyWhen(language, $"{_MEMBER}{_BEARER_WRITE}{Filler(fillerLines)}{_USE_LINE}", classReads);
	}

	[Theory]
	[InlineData(_JAVASCRIPT, _FILLER_LINES_INSIDE_THE_BEARER_WINDOW, false)]
	[InlineData(_JAVASCRIPT, _FILLER_LINES_AT_THE_BEARER_WINDOW_EDGE, true)]
	[InlineData(_TYPESCRIPT, _FILLER_LINES_INSIDE_THE_BEARER_WINDOW, false)]
	[InlineData(_TYPESCRIPT, _FILLER_LINES_AT_THE_BEARER_WINDOW_EDGE, true)]
	public void ABearerWriteAfterTheUseSilencesTheClassWithTwentyNineFillerLinesAndAThirtyFirstLineAwayDoesNot(string language, int fillerLines, bool classReads)
	{
		AssertReadsOnlyWhen(language, $"{_MEMBER}{_USE_LINE}{Filler(fillerLines)}{_BEARER_WRITE}", classReads);
	}

	[Theory]
	[InlineData(_LINE_LENGTH_INSIDE_THE_LINE_BOUND, false)]
	[InlineData(_LINE_LENGTH_BEYOND_THE_LINE_BOUND, true)]
	public void AFillerLineOfFourHundredCharactersKeepsTheWindowAndOneOfFourHundredAndOneBreaksIt(int lineLength, bool classReads)
	{
		string longLine = $"//{new string('x', lineLength - _COMMENT_MARKER_LENGTH)}\n";

		foreach (string language in _languages)
		{
			AssertReadsOnlyWhen(language, $"{_MEMBER}{_BEARER_WRITE}{longLine}{_USE_LINE}", classReads);
		}
	}

	// ---- A scheme fallback, and the declared variants asserted as no finding ----

	[Theory]
	[InlineData("process.env.SERVICE_AUTH_HEADER || 'Basic dXNlcjpwYXNz'")]
	[InlineData("process.env.SERVICE_AUTH_HEADER ?? \"Bearer exampleToken\"")]
	[InlineData("Deno.env.get('SERVICE_AUTH_HEADER') || `basic dXNlcjpwYXNz`")]
	public void AMemberWithASchemeFallbackIsARecordTheCellSuppressesAndNothingIsReported(string value)
	{
		foreach (string language in _languages)
		{
			AssertSilent(language, $"const settings = {{\n\tserviceAuthorization: {value}\n}};\n{_USE_LINE}", value);
		}
	}

	[Theory]
	[InlineData("a property that does not end with the word", "const settings = {\n\tauthHeader: process.env.SERVICE_AUTH_HEADER\n};\nawait fetch(url, { headers: { Authorization: settings.authHeader } });\n")]
	[InlineData("several members on one line", "const settings = { other: 1, serviceAuthorization: process.env.SERVICE_AUTH_HEADER };\n" + _USE_LINE)]
	[InlineData("an object shorthand", "const serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\nconst settings = { serviceAuthorization };\n" + _USE_LINE)]
	[InlineData("a call written as the receiver", "getSettings().serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\nawait fetch(url, { headers: { Authorization: getSettings().serviceAuthorization } });\n")]
	[InlineData("a value built from the read", "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER + suffix\n};\n" + _USE_LINE)]
	[InlineData("a member read from another file", "import { settings } from './settings';\n" + _USE_LINE)]
	[InlineData("an optional chain", "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n};\nawait fetch(url, { headers: { Authorization: settings?.serviceAuthorization } });\n")]
	public void ADeclaredVariantOfTheSettingsFormIsNotReadAndReportsNothing(string variant, string source)
	{
		foreach (string language in _languages)
		{
			AssertSilent(language, source, variant);
		}
	}

	[Theory]
	[InlineData("a non-null mark at the use", "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization! } });\n")]
	[InlineData("a type assertion at the use", "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER\n};\nawait fetch(url, { headers: { Authorization: settings.serviceAuthorization as string } });\n")]
	[InlineData("a non-null mark on the read", "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER!\n};\n" + _USE_LINE)]
	[InlineData("a type assertion on the read", "const settings = {\n\tserviceAuthorization: process.env.SERVICE_AUTH_HEADER as string\n};\n" + _USE_LINE)]
	public void ATypedTailAtTheUseOrOnTheReadIsNotReadAndReportsNothing(string variant, string source)
	{
		AssertSilent(_TYPESCRIPT, source, variant);
	}

	// ---- Class properties, and the member that only declares a type ----

	[Fact]
	public void AnUnannotatedClassFieldWithAnEnvironmentInitializerIsNotReadAtTheUseThroughThis()
	{
		const string source = "class Client {\n\tserviceAuthorization = process.env.API_AUTH;\n}\nawait fetch(url, { headers: { Authorization: this.serviceAuthorization } });\n";

		foreach (string language in _languages)
		{
			AssertSilent(language, source, "class field");
		}
	}

	[Theory]
	[InlineData("class Settings {\n\tserviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	[InlineData("class Settings {\n\tprivate serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	[InlineData("class Settings {\n\tstatic readonly serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	[InlineData("class Settings {\n\tpublic serviceAuthorization = Deno.env.get('SERVICE_AUTH_HEADER');\n}\n")]
	[InlineData("class Settings {\n\tserviceAuthorization: string = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	[InlineData("class Settings {\n\tprivate readonly serviceAuthorization?: string = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	public void AClassFieldWithAnEnvironmentInitializerIsADeclaredFormThatIsNotReadAndGivesNoFinding(string member)
	{
		// A class field assigned from an environment value is not read, so it never resolves the use.
		foreach (string language in _languages)
		{
			AssertSilent(language, $"{member}{_USE_LINE}", member);
		}
	}

	[Theory]
	[InlineData("class Settings {\n\tserviceAuthorization = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	[InlineData("class Settings {\n\tprivate serviceAuthorization: string = process.env.SERVICE_AUTH_HEADER;\n}\n")]
	public void AClassFieldCountsForAmbiguitySoAnEnvironmentObjectLiteralOfTheSameNameBesideItIsIndeterminate(string field)
	{
		foreach (string language in _languages)
		{
			AssertSilent(language, $"{field}{_MEMBER}{_USE_LINE}", field);
			AssertOnlyTheSettingsPropertyClassAtLow(language, $"{_MEMBER}{_USE_LINE}", 1);
		}
	}

	[Theory]
	[InlineData("class Settings {\n\tserviceAuthorization = 'fixed';\n\tserviceAuthorization2 = 1;\n}\n")]
	[InlineData("class Settings {\n\tprotected serviceAuthorization = 'fixed';\n}\n")]
	[InlineData("class Settings {\n\tprivate readonly serviceAuthorization = loadValue();\n}\n")]
	public void AnUnannotatedOrModifiedClassFieldWithAnotherInitializerMakesAnEnvironmentRecordOfTheSameNameIndeterminate(string field)
	{
		foreach (string language in _languages)
		{
			AssertSilent(language, $"{_MEMBER}{field}{_USE_LINE}", field);
		}
	}

	[Theory]
	[InlineData("interface Settings {\n\tserviceAuthorization: AuthHeader\n}\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: Models.AuthHeader<string>\n}\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: 'Basic' | 'Bearer'\n}\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: AuthHeader | undefined\n}\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: string[]\n}\n")]
	[InlineData("interface Settings {\n\tserviceAuthorization: AuthHeader;\n}\n")]
	[InlineData("type Settings = {\n\tserviceAuthorization: 'Basic' | 'Bearer'\n};\n")]
	[InlineData("class Settings {\n\tprivate readonly serviceAuthorization: AuthHeader\n}\n")]
	public void AMemberThatOnlyDeclaresATypeIsNoAssignmentSoAnEnvironmentObjectLiteralOfTheSameNameStillReadsAtLow(string declaration)
	{
		AssertOnlyTheSettingsPropertyClassAtLow(_TYPESCRIPT, $"{declaration}{_MEMBER}{_USE_LINE}", 1);
	}

	[Theory]
	[InlineData("const other = {\n\tserviceAuthorization: 'fixed'\n};\n")]
	[InlineData("const other = {\n\tserviceAuthorization: other.path,\n};\n")]
	[InlineData("const other = {\n\tserviceAuthorization: { value: 1 }\n};\n")]
	public void AValueThatIsALiteralOrEndsWithACommaIsAnAssignmentSoItMakesTheNameIndeterminate(string assignment)
	{
		AssertSilent(_TYPESCRIPT, $"{_MEMBER}{assignment}{_USE_LINE}", assignment);
	}

	// ---- Disjointness with JS-018 and JS-017 ----

	[Theory]
	[MemberData(nameof(EveryLanguage))]
	public void AnEnvironmentReadWhoseNameEndsWithAuthorizationIsTheEnvironmentClassAndNeverTheSettingsClass(string language)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(language, "await fetch(url, { headers: { Authorization: process.env.SERVICE_AUTHORIZATION } });\n");

		Assert.Single(constructs);
		Assert.Equal(_ENVIRONMENT_CLASS, constructs[0].PatternClassIdentifier);
	}

	[Theory]
	[MemberData(nameof(EveryLanguage))]
	public void AnIdentifierValueIsNeverTheSettingsClassAndADottedValueIsNeverTheEnvironmentClass(string language)
	{
		ImmutableArray<RecognizedConstruct> copied = Recognize(language, "const serviceAuthorization = process.env.SERVICE_AUTH_HEADER;\nawait fetch(url, { headers: { Authorization: serviceAuthorization } });\n");
		ImmutableArray<RecognizedConstruct> member = Recognize(language, $"{_MEMBER}{_USE_LINE}");

		Assert.Single(copied);
		Assert.Equal(_ENVIRONMENT_CLASS, copied[0].PatternClassIdentifier);
		Assert.Single(member);
		Assert.Equal(_CLASS, member[0].PatternClassIdentifier);
	}

	[Theory]
	[MemberData(nameof(EveryLanguage))]
	public void TheRulesOfTheTwoLanguagesCarryTheSameClassExpressionAndTheLevelIsLowByDefinition(string language)
	{
		DetectionRule rule = AnalyzerOf(language).DetectionRulesByPatternClass[_CLASS];

		Assert.Equal(ConfidenceLevel.Low, rule.BaseLevel);
		Assert.Equal(SelfSufficiency.RequiresCallPathToBeEstablished, rule.SelfSufficiency);
		Assert.Equal("AuthorizationHeaderValueFromSettingsProperty", rule.DisplayName);
		Assert.Same(JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_CLASS].Expression, rule.Expression);
	}

	private static ILanguageAnalyzer AnalyzerOf(string language)
	{
		return language == _TYPESCRIPT ? TypeScriptRegisteredExpressions.Analyzer : JavaScriptRegisteredExpressions.Analyzer;
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(string language, string source)
	{
		return language == _TYPESCRIPT
			? new TypeScriptRecognitionHarness().Recognize(source)
			: new JavaScriptRecognitionHarness().Recognize(source);
	}

	private static void AssertOnlyTheSettingsPropertyClassAtLow(string language, string source, int count)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(language, source);

		Assert.True(
			constructs.Length == count && constructs.All(construct => construct.PatternClassIdentifier == _CLASS && construct.Level == ConfidenceLevel.Low),
			$"{language}: [{source}] produced [{Describe(constructs)}]");
	}

	private static void AssertSilent(string language, string source, string kind = "")
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(language, source);

		Assert.True(constructs.IsEmpty, $"{language} ({kind}): [{source}] produced [{Describe(constructs)}]");
	}

	private static void AssertReadsOnlyWhen(string language, string source, bool classReads)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(language, source);
		bool read = constructs.Any(construct => construct.PatternClassIdentifier == _CLASS && construct.Level == ConfidenceLevel.Low);

		Assert.True(read == classReads, $"{language}: expected the class to read: {classReads}; got [{Describe(constructs)}] on [{source}]");
	}

	private static string Filler(int lines)
	{
		return string.Concat(Enumerable.Repeat(_FILLER_LINE, lines));
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}
}
