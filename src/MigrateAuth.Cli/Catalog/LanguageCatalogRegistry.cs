using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// Where the engine obtains the catalog embedded in this binary. It knows the version
/// and it knows that analyzers exist; it knows no language, and cannot, because every analyzer
/// implementation is file-local to the file that registers it.
/// </summary>
public static class LanguageCatalogRegistry
{
	/// <summary>Builds the catalog embedded in this binary, stamped with the current version.</summary>
	public static LanguageCatalog BuildEmbeddedCatalog()
	{
		LanguageCatalog catalog = new(LanguageCatalog.CurrentVersion, EmbeddedLanguageAnalyzers.CreateAll());

		return catalog;
	}
}
