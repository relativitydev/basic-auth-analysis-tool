using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;

using MigrateAuth.Cli.Tests.Fixtures;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// The scanner under the reference profile, which was written to exercise every field of the
/// profile vocabulary at once. Reaching it through the registry rather than by naming the analyzer
/// type is deliberate and is the same route the golden test takes: the analyzer class is file-
/// local, so a test that could name it would be evidence the engine can too.
/// </summary>
internal static class ReferenceProfileScanner
{
	private const string _REFERENCE_LANGUAGE_NAME = "ExemplarOne";

	/// <summary>
	/// Scans text written in the reference language, normalizing first exactly as the reader
	/// does. The scanner's contract is that its input is already normalized, so the test path has
	/// to honour it rather than hand over a raw literal with whatever line endings this file
	/// happens to have been checked out with.
	/// </summary>
	internal static ZoneMask Scan(string sourceText)
	{
		return new LexicalScanner(Profile()).Scan(Normalize(sourceText));
	}

	/// <summary>The normalized form, which is what a zone assertion has to index into.</summary>
	internal static string Normalize(string sourceText)
	{
		return TextNormalization.Normalize(sourceText);
	}

	internal static LexicalProfile Profile()
	{
		LanguageCatalog catalog = ExemplarFixtureCatalog.Build();

		return catalog.AnalyzersByLanguageName[_REFERENCE_LANGUAGE_NAME].LexicalProfile;
	}
}
