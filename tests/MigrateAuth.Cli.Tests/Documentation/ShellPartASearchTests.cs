using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The searches of Part A for Shell, checked against the forms the analyzer's own tests read: the one invented fixture of each class, and the further forms
/// below, each of which the analyzer is shown to read as the class it is filed under. The further forms are the ones a class gives a second search for, which
/// the one fixture of each class does not show.
/// </summary>
public sealed class ShellPartASearchTests : PartASearchTests
{
	/// <summary>A form the class reads beyond its one fixture: the class, what the form is, and the script.</summary>
	private static readonly ImmutableArray<(string PatternClass, string Label, string Source)> _furtherForms =
	[
		("SH-003", "an expanded header option", "curl --expand-header \"Authorization: Basic dXNlcjpwYXNz\" \"$URL\"\n"),
		("SH-008", "a whole header held in a variable", "HEADER=\"Authorization: Basic ${ENCODED}\"\ncurl -sS -H \"${HEADER}\" \"$URL\"\n"),
		("SH-012", "a helper written with the function keyword", "function build_basic_auth_header {\n\tprintf 'Authorization: Basic %s' \"$1\"\n}\n"),
		("SH-013", "a request item of HTTPie", "http GET \"$HOST/api\" \"Authorization:Basic ${ENCODED}\"\n"),
		("SH-021", "an alias of curl", "alias c=curl\nc -sS -u \"$U:$P\" \"$URL\"\n")
	];

	public ShellPartASearchTests()
		: base(DocumentedLanguage.Shell)
	{
	}

	internal override ImmutableArray<SearchableForm> CoveredForms()
	{
		ImmutableArray<SearchableForm>.Builder forms = ImmutableArray.CreateBuilder<SearchableForm>();

		foreach ((string patternClass, ConfidenceLevel level, string source) in ShellAnalyzerTests.ClassFixtures())
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
		ShellRecognitionHarness harness = new();

		foreach ((string patternClass, string label, string source) in _furtherForms)
		{
			ImmutableArray<RecognizedConstruct> constructs = harness.Recognize(source);

			Assert.True(
				constructs.Any(construct => construct.PatternClassIdentifier == patternClass),
				$"The form '{label}' is not read as {patternClass}: {string.Join(", ", constructs.Select(construct => construct.PatternClassIdentifier))}");
		}
	}
}
