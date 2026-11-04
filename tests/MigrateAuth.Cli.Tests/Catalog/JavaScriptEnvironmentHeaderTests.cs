using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// AN `Authorization` HEADER WHOSE WHOLE VALUE COMES FROM THE ENVIRONMENT: the class `JS-018` reads it at
/// Low, for manual review, because the code does not show whether the scheme is Basic or a bearer token. The five forms of the environment read, the value copied into a
/// name of the same file, the fallback forms (a `||` or `??` literal, or a template whose only hole is the read) and the guard that a file which also writes an explicit
/// bearer value for the header is silent are each pinned with their positives and their neighbour controls. A fallback literal that begins with the word `Basic` is the
/// higher class `JS-004`, at High; a bearer fallback is silent. Every script is invented with placeholder names, through the shipped analyzer and the real recognition
/// engine.
/// </summary>
public sealed class JavaScriptEnvironmentHeaderTests
{
	private const string _LOW_CLASS = "JS-018";
	private const string _BASIC_CLASS = "JS-004";
	private const string _HELD_CLASS = "JS-017";

	// ---- The five environment forms, each over a carrier of its own ----

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("if (process.env.API_AUTH) {\n\theaders['Authorization'] = process.env['API_AUTH'];\n}\n")]
	[InlineData("headers[\"Authorization\"] = process.env[\"API_AUTH\"];\n")]
	[InlineData("client.setRequestHeader('Authorization', import.meta.env.VITE_API_AUTH);\n")]
	[InlineData("axios.defaults.headers.common['Authorization'] = import.meta.env['VITE_API_AUTH'];\n")]
	[InlineData("const h = new Headers({ Authorization: Deno.env.get('API_AUTH') });\n")]
	[InlineData("const h = new Headers();\nh.set('Authorization', Deno.env.get(\"API_AUTH\"));\n")]
	[InlineData("await fetch(url, { headers: { Authorization: Bun.env.API_AUTH } });\n")]
	[InlineData("headers.authorization = Bun.env['API_AUTH'];\n")]
	public void AnEnvironmentReadOfEachOfTheFiveFormsAsTheWholeValueIsReadAtLowAndNotAtAnyOtherLevel(string source)
	{
		AssertOnlyTheLowClass(source);
	}

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: Deno.env.get(name) } });\n")]
	[InlineData("const { API_AUTH } = process.env;\nawait fetch(url, { headers: { Authorization: API_AUTH } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env?.API_AUTH } });\n")]
	[InlineData("const env = process.env;\nawait fetch(url, { headers: { Authorization: env.API_AUTH } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env[key] } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: Deno.env.toObject().API_AUTH } });\n")]
	public void AReadThatIsNotOneOfTheFiveFormsIsSilent(string source)
	{
		AssertSilent(source);
	}

	// ---- The guard, and the headers and values that are not this class ----

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: `Bearer ${process.env.API_TOKEN}` } });\n")]
	[InlineData("headers.set('Authorization', 'Bearer ' + process.env.API_TOKEN);\n")]
	[InlineData("await fetch(url, { headers: { Authorization: `Bearer ${import.meta.env.VITE_TOKEN}` } });\n")]
	[InlineData("const h = new Headers({ Authorization: `Bearer ${Deno.env.get('API_TOKEN')}` });\n")]
	[InlineData("await fetch(url, { headers: { 'X-Api-Version': process.env.API_VERSION } });\n")]
	[InlineData("await fetch(url, { headers: { Accept: process.env.ACCEPT } });\n")]
	[InlineData("await fetch(url, { headers: { 'Proxy-Authorization': process.env.PROXY_AUTH } });\n")]
	[InlineData("await fetch(url, { headers: { 'X-Authorization': process.env.API_AUTH } });\n")]
	[InlineData("const user = process.env.USER_NAME;\nif (process.env.NODE_ENV === 'production') { run(user); }\n")]
	[InlineData("const value = req.headers.authorization;\n")]
	public void ABearerValueANonAuthenticationHeaderAndAnInboundReadAreSilent(string source)
	{
		AssertSilent(source);
	}

	[Fact]
	public void AFileThatWritesAnExplicitBearerValueForOneRequestIsSilentForAnEnvironmentValueOfAnotherWithinTheWindow()
	{
		const string source =
			"await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n"
			+ "await fetch(second, { headers: { Authorization: process.env.API_AUTH } });\n";

		AssertSilent(source);
	}

	[Fact]
	public void TheGuardReadsTheBearerValueWrittenAfterTheEnvironmentValueToo()
	{
		const string source =
			"await fetch(second, { headers: { Authorization: process.env.API_AUTH } });\n"
			+ "await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n";

		AssertSilent(source);
	}

	[Fact]
	public void TheGuardIsAWindowOfThirtyLinesAndABearerValueFartherAwayDoesNotSilenceTheClass()
	{
		string filler = string.Concat(Enumerable.Repeat("const unrelated = compute();\n", 40));
		string source =
			"await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n"
			+ filler
			+ "await fetch(second, { headers: { Authorization: process.env.API_AUTH } });\n";

		AssertFires(source, _LOW_CLASS);
	}

	[Fact]
	public void ABearerWordInACommentOrInAnotherHeaderIsNotAnExplicitBearerValue()
	{
		const string source =
			"// the old code used Bearer tokens\n"
			+ "await fetch(first, { headers: { 'X-Token': `Bearer ${token}` } });\n"
			+ "await fetch(second, { headers: { Authorization: process.env.API_AUTH } });\n";

		AssertFires(source, _LOW_CLASS);
	}

	// ---- The forms of the guard that the catalog lists ----

	[Theory]
	[InlineData("x.headers.Authorization = 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("x.defaults.headers.common.Authorization = `Bearer ${token}`;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("headers.Authorization = 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("const first = process.env.OLD_AUTH || 'Bearer exampleToken';\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("const first = process.env.OLD_AUTH ?? `Bearer exampleToken`;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	public void ADotAssignmentOnADottedHeadersReceiverAndABearerFallbackAreExplicitBearerValuesAndSilenceTheClass(string source)
	{
		AssertSilent(source);
	}

	[Theory]
	[InlineData("x.headers['Proxy-Authorization'] = 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("x.other.Authorization = 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("const first = process.env.OLD_AUTH || 'neutral';\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	public void AProxyHeaderADotAssignmentOnAnotherReceiverAndANeutralFallbackAreNotExplicitBearerValues(string source)
	{
		AssertFires(source, _LOW_CLASS);
	}

	[Theory]
	[InlineData("const authorization = 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("let authorization = `Bearer ${token}`;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("authorization = 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	public void AVariableMerelyNamedAuthorizationThatHoldsABearerLiteralIsNotReadByTheGuardAndTheClassStillFires(string source)
	{
		AssertFires(source, _LOW_CLASS);
	}

	[Theory]
	[InlineData("xhr.setRequestHeader(\n\t'Authorization',\n\t'Bearer ' + token\n);\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("headers.set(\n\t'Authorization',\n\t`Bearer ${token}`\n);\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("const first = { Authorization:\n\t'Bearer ' + token };\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("headers[ 'Authorization' ] =\n\t'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("headers.Authorization\n\t= 'Bearer ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	public void AnExplicitBearerWriteSplitOverLinesAsTheCarriersAcceptItSilencesTheClass(string source)
	{
		AssertSilent(source);
	}

	[Theory]
	[InlineData("xhr.setRequestHeader(\n\t'X-Token',\n\t'Bearer ' + token\n);\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("const first = { 'Proxy-Authorization':\n\t'Bearer ' + token };\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	[InlineData("headers[ 'Authorization' ] =\n\t'Basic ' + token;\nawait fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n")]
	public void ALineSplitWriteOfAnotherHeaderOrAnotherSchemeDoesNotSilenceTheClass(string source)
	{
		AssertFires(source, _LOW_CLASS);
	}

	// ---- The value copied into a name of the same file ----

	[Theory]
	[InlineData("const authValue = process.env.API_AUTH;\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	[InlineData("let authValue = process.env['API_AUTH'];\nheaders['Authorization'] = authValue;\n")]
	[InlineData("authValue = import.meta.env.VITE_API_AUTH;\nxhr.setRequestHeader('Authorization', authValue);\n")]
	[InlineData("const authValue = Deno.env.get('API_AUTH');\nconst h = new Headers();\nh.set('Authorization', authValue);\n")]
	[InlineData("var authValue = Bun.env.API_AUTH;\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	[InlineData("const authValue = process.env.API_AUTH ?? '';\nheaders.append('Authorization', authValue);\n")]
	[InlineData("export const authValue = process.env.API_AUTH || 'neutral';\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	public void AnIdentifierAssignedFromAnEnvironmentValueAndUsedAsTheWholeHeaderValueIsReadAtLow(string source)
	{
		AssertOnlyTheLowClass(source);
	}

	[Fact]
	public void ACopyWhoseFallbackIsABasicLiteralIsTheHeldValueClassAtMediumAndNotTheLowClass()
	{
		const string source = "const authValue = process.env.API_AUTH || 'Basic dXNlcjpwYXNz';\nawait fetch(url, { headers: { Authorization: authValue } });\n";

		AssertFires(source, _HELD_CLASS, ConfidenceLevel.Medium);
		AssertNeverFires(source, _LOW_CLASS);
	}

	[Theory]
	[InlineData("const authValue = process.env.API_AUTH;\nawait fetch(url, { headers: { 'X-Api-Key': authValue } });\n")]
	[InlineData("const authValue = process.env.API_AUTH;\nauthValue = 'fixed';\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	[InlineData("const authValue = loadAuthValue();\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	[InlineData("const authValue = process.env.API_AUTH;\nconst authValue2 = authValue;\nawait fetch(url, { headers: { Authorization: authValue2 } });\n")]
	[InlineData("const authValue = process.env.API_AUTH;\nlet authValue = process.env.OTHER_AUTH;\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: authValue } });\nconst authValue = process.env.API_AUTH;\n")]
	[InlineData("const authValue = process.env.API_AUTH || 'Bearer exampleToken';\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	[InlineData("const authValue = process.env.API_AUTH;\nconst bearer = `Bearer ${token}`;\nawait fetch(first, { headers: { Authorization: `Bearer ${token}` } });\nawait fetch(url, { headers: { Authorization: authValue } });\n")]
	public void ACopyThatTheCatalogKeepsOutIsSilent(string source)
	{
		AssertSilent(source);
	}

	// ---- The fallback forms ----

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'neutral' } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH ?? '' } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH ?? \"\" } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || `neutral` } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: `${process.env.API_AUTH}` } });\n")]
	[InlineData("headers.set('Authorization', `${import.meta.env.VITE_API_AUTH}`);\n")]
	public void AFallbackLiteralThatIsNeitherBasicNorBearerAndATemplateWhoseOnlyHoleIsTheReadAreReadAtLow(string source)
	{
		AssertOnlyTheLowClass(source);
	}

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'Bearer exampleToken' } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH ?? 'bearer exampleToken' } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: `${process.env.A}${suffix}` } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: `Token ${process.env.API_AUTH}` } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: `${process.env.API_AUTH} extra` } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || compute() } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'x' + suffix } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || process.env.OTHER_AUTH } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'x' || 'y' } });\n")]
	public void AFallbackThatIsABearerSchemeNotWholeOrNotALiteralIsSilent(string source)
	{
		AssertSilent(source);
	}

	// ---- The Basic fallback is the higher class, at High, with the literal rule applied ----

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'Basic dXNlcjpwYXNz' } });\n")]
	[InlineData("headers.set('Authorization', process.env.API_AUTH ?? 'Basic dXNlcjpwYXNz');\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || \"basic dXNlcjpwYXNz\" } });\n")]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || `Basic dXNlcjpwYXNz` } });\n")]
	[InlineData("const h = { Authorization: Deno.env.get('API_AUTH') ?? 'Basic dXNlcjpwYXNz' };\n")]
	public void AFallbackLiteralThatBeginsWithTheWordBasicIsTheHeaderClassAtHighWithTheInCodeSecretWarning(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		RecognizedConstruct header = Assert.Single(constructs, construct => construct.PatternClassIdentifier == _BASIC_CLASS);

		Assert.Equal(ConfidenceLevel.High, header.Level);
		Assert.True(header.LiteralCredentialSignal);
		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _LOW_CLASS);
	}

	[Theory]
	[InlineData("await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'Basic ' + suffix } });\n")]
	[InlineData("await fetch(url, { headers: { 'X-Api-Key': process.env.API_AUTH || 'Basic dXNlcjpwYXNz' } });\n")]
	public void ABasicFallbackThatIsNotWholeOrIsUnderAnotherHeaderIsNotTheHeaderClass(string source)
	{
		AssertNeverFires(source, _BASIC_CLASS);
		AssertNeverFires(source, _LOW_CLASS);
	}

	[Fact]
	public void ABasicFallbackWithAPlaceholderPayloadIsStillTheHeaderClassWithNoInCodeSecretWarning()
	{
		const string source = "await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'Basic changeme' } });\n";

		RecognizedConstruct header = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), construct => construct.PatternClassIdentifier == _BASIC_CLASS);

		Assert.Equal(ConfidenceLevel.High, header.Level);
		Assert.False(header.LiteralCredentialSignal);
	}

	// ---- The class is Low by definition and carries no flow ----

	[Fact]
	public void TheLowClassGivesAFindingWithNoFlowAndNoRecommendation()
	{
		RecognizedConstruct construct = Assert.Single(
			new JavaScriptRecognitionHarness().Recognize("await fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n"),
			candidate => candidate.PatternClassIdentifier == _LOW_CLASS);

		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	// ---- Helpers ----

	private static void AssertOnlyTheLowClass(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Length == 1 && constructs[0].PatternClassIdentifier == _LOW_CLASS && constructs[0].Level == ConfidenceLevel.Low,
			$"[{source}] produced [{Describe(constructs)}]");
	}

	private static void AssertSilent(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.True(constructs.IsEmpty, $"[{source}] produced [{Describe(constructs)}]");
	}

	private static void AssertFires(string source, string patternClass, ConfidenceLevel level = ConfidenceLevel.Low)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.True(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass && construct.Level == level),
			$"{patternClass} did not fire at {level} on [{source}]; got [{Describe(constructs)}]");
	}

	private static void AssertNeverFires(string source, string patternClass)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(source);

		Assert.False(
			constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
			$"{patternClass} fired on [{source}]; got [{Describe(constructs)}]");
	}

	private static string Describe(ImmutableArray<RecognizedConstruct> constructs)
	{
		return string.Join(", ", constructs.Select(construct => $"{construct.PatternClassIdentifier}:{construct.Level}"));
	}
}
