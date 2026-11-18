using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The searches of Part A for Python, checked against the forms the analyzer's own tests read: the one invented fixture of each class, and the further forms
/// below, each of which the analyzer is shown to read as the class it is filed under. The further forms are the ones a class gives a second spelling for, which
/// the one fixture of each class does not show.
/// </summary>
public sealed class PythonPartASearchTests : PartASearchTests
{
	/// <summary>A form the class reads beyond its one fixture: the class, what the form is, and the script.</summary>
	private static readonly ImmutableArray<(string PatternClass, string Label, string Source)> _furtherForms =
	[
		("PY-001", "a helper of httpx with keywords", "client = httpx.Client(auth=httpx.BasicAuth(username=\"user\", password=\"secret\"))\n"),
		("PY-002", "a pair given to a client", "client = httpx.AsyncClient(auth=(\"user\", \"secret\"))\n"),
		("PY-003", "a header assigned on a session", "session.headers[\"Authorization\"] = f\"Basic {encoded}\"\n"),
		("PY-003", "a header set with add_header", "request.add_header(\"Authorization\", \"Basic %s\" % encoded)\n"),
		("PY-005", "a header built from an f-string pair", "headers = urllib3.util.make_headers(basic_auth=f\"{user_name}:{secret_value}\")\n"),
		("PY-009", "a prior-authentication manager", "manager.add_password(None, \"https://example.test/\", \"user\", \"secret\", is_authenticated=True)\n"),
		("PY-011", "an address given to urlopen", "response = urllib.request.urlopen(\"https://user:secret@example.test/items\")\n"),
		("PY-012", "an address given as a keyword", "client = Client(base_url=\"https://user:secret@example.test/\")\n")
	];

	public PythonPartASearchTests()
		: base(DocumentedLanguage.Python)
	{
	}

	internal override ImmutableArray<SearchableForm> CoveredForms()
	{
		ImmutableArray<SearchableForm>.Builder forms = ImmutableArray.CreateBuilder<SearchableForm>();

		foreach ((string patternClass, ConfidenceLevel level, string source) in PythonAnalyzerTests.ClassFixtures())
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
		PythonRecognitionHarness harness = new();

		foreach ((string patternClass, string label, string source) in _furtherForms)
		{
			ImmutableArray<RecognizedConstruct> constructs = harness.Recognize(source);

			Assert.True(
				constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
				$"The form '{label}' is not read as {patternClass}: {string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}");
		}
	}
}
