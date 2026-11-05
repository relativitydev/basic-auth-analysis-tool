using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// One partial file per language, and one line here to register it.
///
/// The declarations below are partial methods with no body of their own. A language whose
/// file does not implement one contributes nothing.
///
/// The embedded catalog is exactly <c>CSharp</c>, <c>JavaScript</c>, <c>PowerShell</c>, <c>Shell</c>, <c>TypeScript</c> and <c>VBNet</c>, six languages, in every build: the shipped binary is the
/// tested binary, and no reference language is registered here.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	/// <summary>Creates every analyzer embedded in this build.</summary>
	public static ImmutableArray<ILanguageAnalyzer> CreateAll()
	{
		List<ILanguageAnalyzer> analyzers = [];

		AddCSharpAnalyzer(analyzers);
		AddPowerShellAnalyzer(analyzers);
		AddVBNetAnalyzer(analyzers);
		AddShellAnalyzer(analyzers);
		AddJavaScriptAnalyzer(analyzers);
		AddTypeScriptAnalyzer(analyzers);

		return [.. analyzers];
	}

	static partial void AddCSharpAnalyzer(ICollection<ILanguageAnalyzer> analyzers);

	static partial void AddPowerShellAnalyzer(ICollection<ILanguageAnalyzer> analyzers);

	static partial void AddVBNetAnalyzer(ICollection<ILanguageAnalyzer> analyzers);

	static partial void AddShellAnalyzer(ICollection<ILanguageAnalyzer> analyzers);

	static partial void AddJavaScriptAnalyzer(ICollection<ILanguageAnalyzer> analyzers);

	static partial void AddTypeScriptAnalyzer(ICollection<ILanguageAnalyzer> analyzers);
}
