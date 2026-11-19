using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>The `Python` analyzer as the shipped binary registers it, found by its declared language name in the embedded analyzers, so a test reads the registered object and never a copy of it.</summary>
internal static class PythonShippedAnalyzer
{
	internal const string LANGUAGE_NAME = "Python";

	internal static ILanguageAnalyzer Instance { get; } = FindTheRegisteredAnalyzer();

	private static ILanguageAnalyzer FindTheRegisteredAnalyzer()
	{
		return EmbeddedLanguageAnalyzers.CreateAll().Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains(LANGUAGE_NAME));
	}
}
