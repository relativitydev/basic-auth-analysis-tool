using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The searches of Part A for JavaScript, checked against the forms the analyzer's own tests read: the one invented fixture of each class, and the further forms
/// below, each of which the analyzer is shown to read as the class it is filed under. The further forms are the ones a class gives a second search for, which the
/// one fixture of each class does not show.
/// </summary>
public sealed class JavaScriptPartASearchTests : PartASearchTests
{
	/// <summary>A form the class reads beyond its one fixture: the class, what the form is, and the script.</summary>
	private static readonly ImmutableArray<(string PatternClass, string Label, string Source)> _furtherForms =
	[
		("JS-001", "an auth option on a created client", "const client = axios.create({ baseURL: base, auth: { username: userName, password: secret } });\n"),
		("JS-002", "an auth call with a pair", "const response = await request.post(url).auth(userName, secret);\n"),
		("JS-004", "a header set on an object", "headers.set('Authorization', `Basic ${encoded}`);\n"),
		("JS-005", "a header built from a pair and an encoder", "const header = 'Basic ' + btoa(`${userName}:${secret}`);\n"),
		("JS-006", "a header built from a buffer", "const header = `Basic ${Buffer.from(userName + ':' + secret).toString('base64')}`;\n")
	];

	public JavaScriptPartASearchTests()
		: base(DocumentedLanguage.JavaScript)
	{
	}

	internal override ImmutableArray<SearchableForm> CoveredForms()
	{
		ImmutableArray<SearchableForm>.Builder forms = ImmutableArray.CreateBuilder<SearchableForm>();

		foreach ((string patternClass, ConfidenceLevel level, string source) in JavaScriptAnalyzerTests.ClassFixtures())
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
		JavaScriptRecognitionHarness harness = new();

		foreach ((string patternClass, string label, string source) in _furtherForms)
		{
			ImmutableArray<RecognizedConstruct> constructs = harness.Recognize(source);

			Assert.True(
				constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
				$"The form '{label}' is not read as {patternClass}: {string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}");
		}
	}
}
