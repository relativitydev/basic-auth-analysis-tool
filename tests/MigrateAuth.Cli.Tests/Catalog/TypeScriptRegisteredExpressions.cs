using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED `TypeScript` ANALYZER, read from the binary and not restated. The analyzer is reached through <see cref="EmbeddedLanguageAnalyzers.CreateAll"/>, the registration the binary ships,
/// and is the one whose declared language is <c>TypeScript</c>.
/// </summary>
internal static class TypeScriptRegisteredExpressions
{
	internal const string LANGUAGE_NAME = "TypeScript";

	internal static ILanguageAnalyzer Analyzer { get; } = CreateTheShippedAnalyzer();

	private static ILanguageAnalyzer CreateTheShippedAnalyzer()
	{
		return EmbeddedLanguageAnalyzers.CreateAll().Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains(LANGUAGE_NAME));
	}
}
