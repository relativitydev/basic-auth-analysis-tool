using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The searches of Part A for Go, checked against the forms the analyzer's own tests read: the one invented fixture of each class, and the further forms
/// below, each of which the analyzer is shown to read as the class it is filed under. The further forms are the ones a class gives a second spelling for, which
/// the one fixture of each class does not show.
/// </summary>
public sealed class GoPartASearchTests : PartASearchTests
{
	/// <summary>A form the class reads beyond its one fixture: the class, what the form is, and the script.</summary>
	private static readonly ImmutableArray<(string PatternClass, string Label, string Source)> _furtherForms =
	[
		("GO-001", "a method expression", "(*http.Request).SetBasicAuth(request, userName, secret)\n"),
		("GO-001", "a call over several lines", "request.SetBasicAuth(\n\tuserName,\n\tsecret,\n)\n"),
		("GO-002", "a header added in lower case", "request.Header.Add(\"authorization\", \"Basic \"+encoded)\n"),
		("GO-002", "a raw literal header", "h.Set(`Authorization`, `Basic ` + encoded)\n"),
		("GO-002", "an index assignment with append", "headers[\"authorization\"] = append(headers[\"authorization\"], \"Basic \"+encoded)\n"),
		("GO-002", "a Sprintf value", "request.Header.Set(\"Authorization\", fmt.Sprintf(\"Basic %s\", encoded))\n"),
		("GO-003", "an aliased type", "other := h.Header{\"authorization\": []string{\"Basic \" + encoded}}\n"),
		("GO-003", "a bare type", "headers := Header{\"Authorization\": {\"Basic \" + encoded}}\n"),
		("GO-004", "a map of lists", "headers := map[string][]string{\"Authorization\": {\"Basic \" + encoded}}\n"),
		("GO-005", "a request built with a context", "request, err := http.NewRequestWithContext(ctx, http.MethodGet, \"https://user:secret@host.example/path\", nil)\n"),
		("GO-005", "an upper-case scheme", "response, err := client.Get(\"HTTPS://user:secret@host.example/path\")\n"),
		("GO-006", "an aliased encoder over Sprintf", "pair := b64.StdEncoding.EncodeToString([]byte(fmt.Sprintf(\"%s:%s\", userName, secret)))\nvalue := \"Basic \" + pair\n"),
		("GO-006", "an unpadded encoder over Join", "pair := base64.RawStdEncoding.EncodeToString([]byte(strings.Join(parts, \":\")))\nvalue := \"Basic \" + pair\n"),
		("GO-006", "a whole literal pair", "pair := base64.StdEncoding.EncodeToString([]byte(\"user:secret\"))\nvalue := \"Basic \" + pair\n"),
		("GO-007", "a field of a literal", "address := &url.URL{Scheme: \"https\", User: url.UserPassword(userName, secret), Host: \"host.example\"}\n"),
		("GO-007", "an aliased call with a literal password", "address.User = neturl.UserPassword(userName, \"exampleSecret\")\n"),
		("GO-008", "a held constant", "const serviceURL = \"https://user:secret@host.example/path\"\n"),
		("GO-008", "a typed variable", "var serviceURL string = \"http://user:secret@host.example/path\"\n"),
		("GO-008", "a request parse", "address, err := url.ParseRequestURI(\"https://user:secret@host.example/path\")\n"),
		("GO-009", "an index assignment", "value := \"Basic \" + encoded\nheaders[\"Authorization\"] = []string{value}\n"),
		(GoPartATestConstants.HELPER_CLASS, "a function that writes the header", "func setAuth(request *http.Request, encoded string) {\n\trequest.Header.Set(\"Authorization\", \"Basic \"+encoded)\n}\n"),
		(GoPartATestConstants.HELPER_CLASS, "an encoded helper", "func basicHeader(raw []byte) string {\n\tencoded := base64.StdEncoding.EncodeToString(raw)\n\treturn \"Basic \" + encoded\n}\n"),
		(GoPartATestConstants.ENVIRONMENT_CLASS, "a held environment read", "value, found := os.LookupEnv(\"SERVICE_AUTH_HEADER\")\nrequest.Header.Set(\"Authorization\", value)\n"),
		(GoPartATestConstants.HELPER_CALL_CLASS, "an index assignment", $"{GoPartATestConstants.BASIC_HELPER_DECLARATION}func send() {{\n\trequest.Header[\"Authorization\"] = []string{{basicHeader(userName, secret)}}\n}}\n")
	];

	public GoPartASearchTests()
		: base(DocumentedLanguage.Go)
	{
	}

	internal override ImmutableArray<SearchableForm> CoveredForms()
	{
		ImmutableArray<SearchableForm>.Builder forms = ImmutableArray.CreateBuilder<SearchableForm>();

		foreach ((string patternClass, ConfidenceLevel level, string source) in GoAnalyzerTests.ClassFixtures())
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
		GoRecognitionHarness harness = new();

		foreach ((string patternClass, string label, string source) in _furtherForms)
		{
			ImmutableArray<RecognizedConstruct> constructs = harness.Recognize(source);

			Assert.True(
				constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
				$"The form '{label}' is not read as {patternClass}: {string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}");
		}
	}
}
