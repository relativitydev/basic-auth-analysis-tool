using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED `JavaScript` ANALYZER AND ITS EXPRESSIONS, read from the binary and not restated: its rules, demotion signals, identification and profile. The analyzer is reached through <see cref="EmbeddedLanguageAnalyzers.CreateAll"/>, the registration the binary ships, and is the one
/// whose declared language is <c>JavaScript</c>.
/// </summary>
internal static class JavaScriptRegisteredExpressions
{
	internal const string LANGUAGE_NAME = "JavaScript";

	internal static ILanguageAnalyzer Analyzer { get; } = CreateTheShippedAnalyzer();

	private static ILanguageAnalyzer CreateTheShippedAnalyzer()
	{
		return EmbeddedLanguageAnalyzers.CreateAll().Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains(LANGUAGE_NAME));
	}
}
