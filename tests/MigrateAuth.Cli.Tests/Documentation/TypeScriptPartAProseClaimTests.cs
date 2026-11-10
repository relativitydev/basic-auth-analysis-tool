using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// Part A of TypeScript says, for the classes below (the typed credential object, the injected-service class and the two shared classes it reads on TypeScript text),
/// what the tool reads and what it does not, and a reader acts on those sentences. Each sentence below is a claim, a table row: the section whose prose makes it, a phrase copied from that prose,
/// an invented input, and what the analyzer must do with it (read at a level, or not read, by the named class). Every phrase must still stand in its section, so a rewritten sentence is
/// re-checked. Every input is invented from the prose, with placeholder names.
/// </summary>
public sealed class TypeScriptPartAProseClaimTests
{
	private const string _TYPED_OBJECT_CLASS = "TS-001";
	private const string _SERVICE_CLASS = "TS-003";
	private const string _CLIENT_OBJECT_CLASS = "JS-001";
	private const string _BASE64_PAIR_CLASS = "JS-005";
	private const string _GENERIC_AUTH_OBJECT_CLASS = "JS-011";

	private sealed record ProseClaim(string Section, string Phrase, string PatternClass, bool IsRead, ConfidenceLevel Level, string Source);

	private static readonly ImmutableArray<ProseClaim> _claims =
	[
		// TS-001: a value of any form, and a bare name, are read
		new(_TYPED_OBJECT_CLASS, "so `{ username: userName, password: this.clientSecret }` is read as well as a literal pair", _TYPED_OBJECT_CLASS, true, ConfidenceLevel.Medium, "const config: AxiosRequestConfig = { baseURL: host, auth: { username: userName, password: this.clientSecret } };\n"),
		new(_TYPED_OBJECT_CLASS, "so `{ username: userName, password: this.clientSecret }` is read as well as a literal pair", _TYPED_OBJECT_CLASS, true, ConfidenceLevel.Medium, "const options = { auth: { username: dto.username, password: dto.password } } satisfies AxiosRequestConfig;\n"),
		new(_TYPED_OBJECT_CLASS, "`{ username, password }` is read, and either member may be written either way (`{ username: accountName, password }`)", _TYPED_OBJECT_CLASS, true, ConfidenceLevel.Medium, "const config: AxiosRequestConfig = { baseURL: host, auth: { username, password } };\n"),
		new(_TYPED_OBJECT_CLASS, "`{ username, password }` is read, and either member may be written either way (`{ username: accountName, password }`)", _TYPED_OBJECT_CLASS, true, ConfidenceLevel.Medium, "const options = { auth: { username: accountName, password } } satisfies AxiosRequestConfig;\n"),
		// TS-001: what it does not read
		new(_TYPED_OBJECT_CLASS, "an uncited type, the same object under a type name the tool does not know", _TYPED_OBJECT_CLASS, false, ConfidenceLevel.Medium, "const config: ProjectConfig = { auth: { username: userName, password: secret } };\n"),
		new(_TYPED_OBJECT_CLASS, "an `auth` object with only one of the two members", _TYPED_OBJECT_CLASS, false, ConfidenceLevel.Medium, "const config: AxiosRequestConfig = { auth: { username: userName } };\n"),
		new(_TYPED_OBJECT_CLASS, "a name that is the value of another key (`{ label: username, password }`)", _TYPED_OBJECT_CLASS, false, ConfidenceLevel.Medium, "const config: AxiosRequestConfig = { auth: { label: username, password } };\n"),
		new(_TYPED_OBJECT_CLASS, "a member named `user` or `pass` (the class reads `username` and `password` only)", _TYPED_OBJECT_CLASS, false, ConfidenceLevel.Medium, "const config: AxiosRequestConfig = { auth: { user: userName, pass: secret } };\n"),
		new(_TYPED_OBJECT_CLASS, "a type member with no value (`auth: { username: string; password: string }` in an interface or a type alias)", _TYPED_OBJECT_CLASS, false, ConfidenceLevel.Medium, "type Settings = AxiosRequestConfig & { auth: { username: string; password: string } };\n"),
		new(_TYPED_OBJECT_CLASS, "a type member with no value (`auth: { username: string; password: string }` in an interface or a type alias)", _TYPED_OBJECT_CLASS, false, ConfidenceLevel.Medium, "interface Settings extends AxiosRequestConfig {\n\tauth: { username: string; password: string };\n}\n"),
		// TS-003: a bare name is a member, the member names, the request body, the name heuristic
		new(_SERVICE_CLASS, "A member written as a bare name with no value counts as a member (`{ username, password }`, or `{ username: accountName, password }`)", _SERVICE_CLASS, true, ConfidenceLevel.High, "this.httpService.get<Reply>(url, { auth: { username, password } });\n"),
		new(_SERVICE_CLASS, "A member written as a bare name with no value counts as a member (`{ username, password }`, or `{ username: accountName, password }`)", _SERVICE_CLASS, true, ConfidenceLevel.High, "this.httpService.post(url, body, { auth: { username: accountName, password } });\n"),
		new(_SERVICE_CLASS, "a member named `user` or `pass` is not read by this class, and the same object is reported at Low by `JS-011` for manual review", _SERVICE_CLASS, false, ConfidenceLevel.High, "this.httpClient.post(url, body, { auth: { user: userName, pass: secret } });\n"),
		new(_SERVICE_CLASS, "a member named `user` or `pass` is not read by this class, and the same object is reported at Low by `JS-011` for manual review", _GENERIC_AUTH_OBJECT_CLASS, true, ConfidenceLevel.Low, "this.httpClient.post(url, body, { auth: { user: userName, pass: secret } });\n"),
		new(_SERVICE_CLASS, "a member named `user` or `pass` is not read by this class, and the same object is reported at Low by `JS-011` for manual review", _GENERIC_AUTH_OBJECT_CLASS, true, ConfidenceLevel.Low, "http.put(url, body, { auth: { user: userName, pass: secret } });\n"),
		new(_SERVICE_CLASS, "The pair as the request body with no `auth` key (`this.httpService.post(url, { username, password })`)", _SERVICE_CLASS, false, ConfidenceLevel.High, "this.httpService.post(url, { username, password });\n"),
		new(_SERVICE_CLASS, "and an `auth` object with only one of the two members are not read", _SERVICE_CLASS, false, ConfidenceLevel.High, "this.httpService.get(url, { auth: { username } });\n"),
		new(_SERVICE_CLASS, "which is a false positive and a declared limit; confirm the type of the receiver by hand and review each such finding before acting", _SERVICE_CLASS, true, ConfidenceLevel.High, "this.http.get(url, { auth: { username: userName, password: secret } });\n"),
		new(_SERVICE_CLASS, "which is a false positive and a declared limit; confirm the type of the receiver by hand and review each such finding before acting", _SERVICE_CLASS, true, ConfidenceLevel.High, "this.accountHttp.get(url, { auth: { username: userName, password: secret } });\n"),
		new(_SERVICE_CLASS, "An injected service whose name does not contain `http` (`this.client.get(url, { auth: ... })`) is not read as this class", _SERVICE_CLASS, false, ConfidenceLevel.High, "this.client.get(url, { auth: { username: userName, password: secret } });\n"),
		// JS-001 on TypeScript text: a bare name is a member
		new(_CLIENT_OBJECT_CLASS, "so `axios.get(url, { auth: { username, password } })` is read", _CLIENT_OBJECT_CLASS, true, ConfidenceLevel.High, "const response = await axios.get(url, { auth: { username, password } });\n"),
		new(_CLIENT_OBJECT_CLASS, "either member may be written either way (`{ username: userName, password }`)", _CLIENT_OBJECT_CLASS, true, ConfidenceLevel.High, "const client = axios.create({ baseURL: host, auth: { username: userName, password } });\n"),
		new(_CLIENT_OBJECT_CLASS, "the same two bare names as the request body with no `auth` key (`axios.post(url, { username, password })`)", _CLIENT_OBJECT_CLASS, false, ConfidenceLevel.High, "await axios.post(url, { username, password });\n"),
		new(_CLIENT_OBJECT_CLASS, "a name that is the value of another key (`{ label: username, password: secret }`)", _CLIENT_OBJECT_CLASS, false, ConfidenceLevel.High, "await axios.get(url, { auth: { label: username, password: secret } });\n"),
		// JS-005 on TypeScript text: a pair split across two literals
		new(_BASE64_PAIR_CLASS, "`btoa('exampleUser:' + 'exampleSecret')`, with or without more concatenation after the second literal", _BASE64_PAIR_CLASS, true, ConfidenceLevel.Medium, "const header: string = 'Basic ' + btoa('exampleUser:' + 'exampleSecret');\n"),
		new(_BASE64_PAIR_CLASS, "`btoa('exampleUser:' + 'exampleSecret')`, with or without more concatenation after the second literal", _BASE64_PAIR_CLASS, true, ConfidenceLevel.Medium, "const header: string = 'Basic ' + btoa('exampleUser:' + 'exampleSecret' + extra + this.stamp);\n"),
		new(_BASE64_PAIR_CLASS, "a literal that ends in a colon followed by code (`btoa('exampleUser:' + secret)`)", _BASE64_PAIR_CLASS, false, ConfidenceLevel.Medium, "const header: string = 'Basic ' + btoa('exampleUser:' + secret);\n"),
		new(_BASE64_PAIR_CLASS, "two literals with no colon between them (`btoa('exampleUser' + 'exampleSecret')`)", _BASE64_PAIR_CLASS, false, ConfidenceLevel.Medium, "const header: string = 'Basic ' + btoa('exampleUser' + 'exampleSecret');\n")
	];

	public static TheoryData<int> ClaimIndexes()
	{
		TheoryData<int> indexes = [];

		for (int index = 0; index < _claims.Length; index++)
		{
			indexes.Add(index);
		}

		return indexes;
	}

	[Theory]
	[MemberData(nameof(ClaimIndexes))]
	public void EveryClaimOfPartAIsTrueOfTheRegisteredRule(int index)
	{
		ProseClaim claim = _claims[index];
		ImmutableArray<RecognizedConstruct> constructs = new TypeScriptRecognitionHarness().Recognize(claim.Source);

		if (claim.IsRead)
		{
			Assert.Contains(constructs, construct => construct.PatternClassIdentifier == claim.PatternClass && construct.Level == claim.Level);
		}
		else
		{
			Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == claim.PatternClass);
		}
	}

	/// <summary>The literal rule of Part A for the typed class: the in-code secret warning is raised only when the password member is a whole literal, and never for a name, a member access, a call or a bare name.</summary>
	[Theory]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: userName, password: 'exampleSecret' } };\n", true)]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: 'exampleUser', password: secret } };\n", false)]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: userName, password: this.clientSecret } };\n", false)]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: userName, password } };\n", false)]
	[InlineData("const config: AxiosRequestConfig = { auth: { username: userName, password: 'exampleSecret' as string } };\n", false)]
	public void TheSecretWarningOfTheTypedClassIsRaisedOnlyForAWholeLiteralPassword(string source, bool expectedWarning)
	{
		string partA = File.ReadAllText(DocumentedLanguage.TypeScript.PartAPath());
		RecognizedConstruct construct = Assert.Single(new TypeScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == _TYPED_OBJECT_CLASS);

		Assert.Contains("The in-code secret warning is raised only when the password member is a whole string literal", partA, StringComparison.Ordinal);
		Assert.Equal(expectedWarning, construct.LiteralCredentialSignal);
	}

	[Fact]
	public void EveryPhraseOfEveryClaimStillStandsInTheSectionThatMakesIt()
	{
		string partA = File.ReadAllText(DocumentedLanguage.TypeScript.PartAPath());
		IReadOnlyDictionary<string, DetectionRule> rules = DocumentedLanguage.TypeScript.Analyzer().DetectionRulesByPatternClass;

		foreach (ProseClaim claim in _claims)
		{
			string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(claim.Section, rules[claim.Section].DisplayName)) ?? string.Empty;

			Assert.True(section.Contains(claim.Phrase, StringComparison.Ordinal), $"The section of {claim.Section} no longer holds the phrase: {claim.Phrase}");
		}
	}

	[Fact]
	public void TheClaimsCoverTheClassesTheAmendmentTouched()
	{
		Assert.Equal(
			new[] { _TYPED_OBJECT_CLASS, _SERVICE_CLASS, _CLIENT_OBJECT_CLASS, _BASE64_PAIR_CLASS }.Order(StringComparer.Ordinal),
			_claims.Select(claim => claim.Section).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
	}
}
