using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Tests.Fixtures;

/// <summary>
/// Builds the in-process catalog that holds the reference analyzer alongside the embedded analyzers. The
/// embedded catalog is exactly <c>CSharp</c>, so tests that need the invented reference grammar
/// register it here, through the same public <see cref="ILanguageAnalyzer"/> contract every embedded
/// language uses. Nothing in the shipped binary can plug an analyzer in.
/// </summary>
public static class ExemplarFixtureCatalog
{
	/// <summary>Creates a catalog of <see cref="ExemplarFixtureAnalyzer"/> followed by every embedded analyzer.</summary>
	public static LanguageCatalog Build()
	{
		ImmutableArray<ILanguageAnalyzer> analyzers =
		[
			new ExemplarFixtureAnalyzer(),
			.. EmbeddedLanguageAnalyzers.CreateAll()
		];

		return new LanguageCatalog(LanguageCatalog.CurrentVersion, analyzers);
	}
}
