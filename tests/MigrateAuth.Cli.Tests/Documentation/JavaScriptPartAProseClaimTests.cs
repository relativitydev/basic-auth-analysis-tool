using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// Part A of JavaScript says, for the classes below, what the tool reads and what it does not, and a reader acts on those sentences. Each sentence below is a
/// claim, a table row: the section whose prose makes it, a phrase copied from that prose, an invented input, and what the analyzer must do with it (read at a level, or not read, by the
/// named class). Every phrase must still stand in its section, so a rewritten sentence is re-checked. Every input is invented from the prose, with placeholder names.
/// </summary>
public sealed class JavaScriptPartAProseClaimTests
{
	private const string _CLIENT_OBJECT_CLASS = "JS-001";
	private const string _BASE64_PAIR_CLASS = "JS-005";
	private const string _ENVIRONMENT_CLASS = "JS-018";
	private const string _HEADER_CLASS = "JS-004";
	private const string _USERINFO_CLASS = "JS-013";
	private const string _ENVIRONMENT_HEADER = "await fetch(url, { headers: { Authorization: process.env.API_AUTH } });\n";

	private sealed record ProseClaim(string Section, string Phrase, string PatternClass, bool IsRead, ConfidenceLevel Level, string Source);

	private static readonly ImmutableArray<ProseClaim> _claims =
	[
		// JS-001: a bare member name is a member
		new(_CLIENT_OBJECT_CLASS, "so `axios.get(url, { auth: { username, password } })` is read", _CLIENT_OBJECT_CLASS, true, ConfidenceLevel.High, "const response = await axios.get(url, { auth: { username, password } });\n"),
		new(_CLIENT_OBJECT_CLASS, "either member may be written either way (`{ username: userName, password }`)", _CLIENT_OBJECT_CLASS, true, ConfidenceLevel.High, "const client = axios.create({ baseURL: host, auth: { username: userName, password } });\n"),
		new(_CLIENT_OBJECT_CLASS, "the same two bare names as the request body with no `auth` key (`axios.post(url, { username, password })`)", _CLIENT_OBJECT_CLASS, false, ConfidenceLevel.High, "await axios.post(url, { username, password });\n"),
		new(_CLIENT_OBJECT_CLASS, "a name that is the value of another key (`{ label: username, password: secret }`)", _CLIENT_OBJECT_CLASS, false, ConfidenceLevel.High, "await axios.get(url, { auth: { label: username, password: secret } });\n"),
		// JS-005: a pair split across two literals
		new(_BASE64_PAIR_CLASS, "`btoa('exampleUser:' + 'exampleSecret')`, with or without more concatenation after the second literal", _BASE64_PAIR_CLASS, true, ConfidenceLevel.Medium, "const header = 'Basic ' + btoa('exampleUser:' + 'exampleSecret');\n"),
		new(_BASE64_PAIR_CLASS, "`btoa('exampleUser:' + 'exampleSecret')`, with or without more concatenation after the second literal", _BASE64_PAIR_CLASS, true, ConfidenceLevel.Medium, "const line = 'Basic ' + btoa('exampleUser:' + 'exampleSecret' + extra + this.stamp);\n"),
		new(_BASE64_PAIR_CLASS, "a literal that ends in a colon followed by code (`btoa('exampleUser:' + secret)`)", _BASE64_PAIR_CLASS, false, ConfidenceLevel.Medium, "const header = 'Basic ' + btoa('exampleUser:' + secret);\n"),
		new(_BASE64_PAIR_CLASS, "two literals with no colon between them (`btoa('exampleUser' + 'exampleSecret')`)", _BASE64_PAIR_CLASS, false, ConfidenceLevel.Medium, "const header = 'Basic ' + btoa('exampleUser' + 'exampleSecret');\n"),
		// JS-018: the forms it reads
		new(_ENVIRONMENT_CLASS, "a template whose only hole is the read", _ENVIRONMENT_CLASS, true, ConfidenceLevel.Low, "await fetch(url, { headers: { Authorization: `${process.env.API_AUTH}` } });\n"),
		new(_ENVIRONMENT_CLASS, "or an identifier that was assigned from one of these in the same file", _ENVIRONMENT_CLASS, true, ConfidenceLevel.Low, "const authValue = process.env.API_AUTH;\nawait fetch(url, { headers: { Authorization: authValue } });\n"),
		new(_ENVIRONMENT_CLASS, "`Deno.env.get('NAME')` and `Bun.env.NAME`", _ENVIRONMENT_CLASS, true, ConfidenceLevel.Low, "const h = new Headers({ Authorization: Deno.env.get('API_AUTH') });\n"),
		// JS-018: the forms it does not read
		new(_ENVIRONMENT_CLASS, "A fallback literal that begins with `Basic` is not this class", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'Basic dXNlcjpwYXNz' } });\n"),
		new(_ENVIRONMENT_CLASS, "A fallback that begins with `Bearer` is not read", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'Bearer exampleToken' } });\n"),
		new(_ENVIRONMENT_CLASS, "a read with optional chaining", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "await fetch(url, { headers: { Authorization: process.env?.API_AUTH } });\n"),
		new(_ENVIRONMENT_CLASS, "a destructuring", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "const { API_AUTH } = process.env;\nawait fetch(url, { headers: { Authorization: API_AUTH } });\n"),
		new(_ENVIRONMENT_CLASS, "a different header (`Proxy-Authorization`, `X-Authorization`)", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "await fetch(url, { headers: { 'X-Authorization': process.env.API_AUTH } });\n"),
		// JS-018: the guard, declared in full
		new(_ENVIRONMENT_CLASS, "written within thirty lines of it, before or after", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n" + _ENVIRONMENT_HEADER),
		new(_ENVIRONMENT_CLASS, "written within thirty lines of it, before or after", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, _ENVIRONMENT_HEADER + "await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n"),
		new(_ENVIRONMENT_CLASS, "written within thirty lines of it, before or after", _ENVIRONMENT_CLASS, true, ConfidenceLevel.Low, "await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n" + string.Concat(Enumerable.Repeat("const unrelated = compute();\n", 31)) + _ENVIRONMENT_HEADER),
		new(_ENVIRONMENT_CLASS, "written within thirty lines of it, before or after", _ENVIRONMENT_CLASS, true, ConfidenceLevel.Low, _ENVIRONMENT_HEADER + string.Concat(Enumerable.Repeat("const unrelated = compute();\n", 31)) + "await fetch(first, { headers: { Authorization: `Bearer ${token}` } });\n"),
		new(_ENVIRONMENT_CLASS, "a comment or a string that spells the header name with a `Bearer` literal silences it too", _ENVIRONMENT_CLASS, false, ConfidenceLevel.Low, "// Authorization: 'Bearer exampleToken'\n" + _ENVIRONMENT_HEADER),
		new(_ENVIRONMENT_CLASS, "a `Bearer` literal held in a variable first and given to the header later is not read", _ENVIRONMENT_CLASS, true, ConfidenceLevel.Low, "const authorization = 'Bearer ' + token;\n" + _ENVIRONMENT_HEADER),
		// JS-004: the fallback
		new(_HEADER_CLASS, "which is a Basic header written in the code itself", _HEADER_CLASS, true, ConfidenceLevel.High, "await fetch(url, { headers: { Authorization: process.env.API_AUTH || 'Basic dXNlcjpwYXNz' } });\n"),
		// JS-013: the proxy refusal
		new(_USERINFO_CLASS, "marks the address as a credential for a proxy and not for the service", _USERINFO_CLASS, false, ConfidenceLevel.Low, "const proxyUrl = 'http://exampleUser:exampleSecret@proxy.example:3128';\n"),
		new(_USERINFO_CLASS, "an address held under a neutral name is still reported", _USERINFO_CLASS, true, ConfidenceLevel.Medium, "const egressUrl = 'http://exampleUser:exampleSecret@proxy.example:3128';\n")
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
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(claim.Source);

		if (claim.IsRead)
		{
			Assert.Contains(constructs, construct => construct.PatternClassIdentifier == claim.PatternClass && construct.Level == claim.Level);
		}
		else
		{
			Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == claim.PatternClass);
		}
	}

	/// <summary>The literal rule of Part A: a literal is a credential literal only when it is the whole value, and a literal Basic token of the header class raises the in-code secret warning.</summary>
	[Theory]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'prefix' + suffix } });\n", "JS-001", false)]
	[InlineData("axios.get(url, { auth: { username: userName, password: 'exampleSecret' } });\n", "JS-001", true)]
	[InlineData("await fetch(url, { headers: { Authorization: 'Basic dXNlcjpwYXNz' } });\n", "JS-004", true)]
	[InlineData("await fetch(url, { headers: { Authorization: 'Basic dXNlcjpwYXNz' + suffix } });\n", "JS-004", false)]
	public void TheLiteralRuleAndTheSecretWarningOfPartAHold(string source, string patternClass, bool expectedWarning)
	{
		string partA = File.ReadAllText(DocumentedLanguage.JavaScript.PartAPath());
		RecognizedConstruct construct = Assert.Single(new JavaScriptRecognitionHarness().Recognize(source), candidate => candidate.PatternClassIdentifier == patternClass);

		Assert.Contains("a concatenation of a literal with code is not literal", partA, StringComparison.Ordinal);
		Assert.Contains("the match also raises the in-code secret warning", partA, StringComparison.Ordinal);
		Assert.Equal(expectedWarning, construct.LiteralCredentialSignal);
	}

	[Fact]
	public void EveryPhraseOfEveryClaimStillStandsInTheSectionThatMakesIt()
	{
		string partA = File.ReadAllText(DocumentedLanguage.JavaScript.PartAPath());
		IReadOnlyDictionary<string, DetectionRule> rules = DocumentedLanguage.JavaScript.Analyzer().DetectionRulesByPatternClass;

		foreach (ProseClaim claim in _claims)
		{
			string section = PublicDocumentChecks.SectionOf(partA, PublicDocumentChecks.ClassHeading(claim.Section, rules[claim.Section].DisplayName)) ?? string.Empty;

			Assert.True(section.Contains(claim.Phrase, StringComparison.Ordinal), $"The section of {claim.Section} no longer holds the phrase: {claim.Phrase}");
		}
	}

	[Fact]
	public void TheClaimsCoverTheClassesTheAmendmentsTouched()
	{
		Assert.Equal(
			new[] { _CLIENT_OBJECT_CLASS, _BASE64_PAIR_CLASS, _ENVIRONMENT_CLASS, _HEADER_CLASS, _USERINFO_CLASS }.Order(StringComparer.Ordinal),
			_claims.Select(claim => claim.Section).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
	}
}
