using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// Every language catalog ends up embedded in the same binary, so the registration
/// shape matters: one partial file per language, and one line here to register it. An
/// expression error in one language is then confined to that language's file instead of
/// sitting in a file all eight have to share.
///
/// The declarations below are partial methods with no body of their own. A language whose
/// file has not been written yet simply contributes nothing, and the registration list does
/// not have to be edited twice to add one.
///
/// The embedded catalog is exactly <c>CSharp</c> in every build: the shipped binary is the
/// tested binary, and no reference language is registered here.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	/// <summary>Creates every analyzer embedded in this build.</summary>
	public static ImmutableArray<ILanguageAnalyzer> CreateAll()
	{
		List<ILanguageAnalyzer> analyzers = [];

		AddCSharpAnalyzer(analyzers);

		return [.. analyzers];
	}

	static partial void AddCSharpAnalyzer(ICollection<ILanguageAnalyzer> analyzers);
}
