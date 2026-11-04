using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The searches of Part A for TypeScript, checked against the forms the analyzer's own tests read: the one invented fixture of each class, the inherited classes
/// included. Each documented search must find every form its section covers.
/// </summary>
public sealed class TypeScriptPartASearchTests : PartASearchTests
{
	/// <summary>A form the class reads beyond its one fixture, taken from the examples of Part A: the class, what the form is, and the source.</summary>
	private static readonly ImmutableArray<(string PatternClass, string Label, string Source)> _furtherForms =
	[
		("TS-001", "a typed declaration with a literal pair", "const config: AxiosRequestConfig = { baseURL: host, auth: { username: 'exampleUser', password: 'exampleSecret' } };\n"),
		("TS-001", "a satisfies clause after the object", "const settings = { auth: { username: 'exampleUser', password: 'exampleSecret' } } satisfies AxiosRequestConfig;\n"),
		("JS-001", "a client created with the auth object", "const client = axios.create({ baseURL: host, auth: { username: userName, password: secret } });\n"),
		("JS-003", "a credential address in an http call", "const request = http.get(\"https://exampleUser:exampleSecret@host.example/path\", handler);\n"),
		("JS-004", "a header written with setRequestHeader", "xhr.setRequestHeader(\"Authorization\", \"Basic \" + encoded);\n"),
		("JS-005", "a template pair inside btoa", "const header = \"Basic \" + btoa(`${userName}:${secret}`);\n"),
		("JS-006", "a pair given to the legacy Buffer constructor", "const legacy = \"Basic \" + new Buffer(userName + \":\" + secret).toString(\"base64\");\n"),
		("JS-016", "a third argument false on the auth method", "request.get(url).auth(userName, secret, false);\n")
	];

	public TypeScriptPartASearchTests()
		: base(DocumentedLanguage.TypeScript)
	{
	}

	internal override ImmutableArray<SearchableForm> CoveredForms()
	{
		ImmutableArray<SearchableForm>.Builder forms = ImmutableArray.CreateBuilder<SearchableForm>();

		foreach ((string patternClass, ConfidenceLevel level, string source) in TypeScriptAnalyzerTests.ClassFixtures())
		{
			forms.Add(new SearchableForm(patternClass, $"{patternClass} fixture at {level}", source));
		}

		forms.AddRange(_furtherForms.Select(form => new SearchableForm(form.PatternClass, $"{form.PatternClass} {form.Label}", form.Source)));

		return forms.ToImmutable();
	}

	/// <summary>Each further form is a form the analyzer reads as its own class, so a search that finds it is held to a covered form.</summary>
	[Fact]
	public void EveryFurtherFormIsReadByTheAnalyzerAsTheClassItIsFiledUnder()
	{
		TypeScriptRecognitionHarness harness = new();

		foreach ((string patternClass, string label, string source) in _furtherForms)
		{
			ImmutableArray<RecognizedConstruct> constructs = harness.Recognize(source);

			Assert.True(
				constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
				$"The form '{label}' is not read as {patternClass}: {string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}");
		}
	}
}
