using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// ONE LANGUAGE OF THE PUBLIC MIGRATION DOCUMENTATION: WHICH ANALYZER IT IS READ FROM, WHICH TWO FILES DESCRIBE IT, AND THE
/// IDENTIFIER THE GENERIC ROUTE IS COMPOSED FOR.
///
/// Part A and Part C are written once for each language, so every check over them takes the language it runs for. The two
/// instances below are the only places that name a file, an analyzer or a fence label of a language; nothing else in the
/// documentation tests spells them.
/// </summary>
public sealed record DocumentedLanguage(
	string DisplayName,
	string LanguageName,
	AssessedLanguage Language,
	string PartAFileName,
	string PartCFileName,
	string UndeclaredPatternClass,
	string ExampleFenceLabel,
	string SourceFileGlob)
{
	/// <summary>The first language: Part A and Part C of C#.</summary>
	public static readonly DocumentedLanguage CSharp = new(
		"C#",
		"CSharp",
		AssessedLanguage.CSharp,
		"part-a-csharp.md",
		"part-c-csharp.md",
		CSharpRenderedTexts.UNDECLARED_PATTERN_CLASS,
		"csharp",
		"*.cs");

	/// <summary>The second language: Part A and Part C of PowerShell.</summary>
	public static readonly DocumentedLanguage PowerShell = new(
		"PowerShell",
		"PowerShell",
		AssessedLanguage.PowerShell,
		"part-a-powershell.md",
		"part-c-powershell.md",
		PowerShellRenderedTexts.UNDECLARED_PATTERN_CLASS,
		"powershell",
		"*.ps1");

	/// <summary>The embedded analyzer of this language, read through the same catalog the report is built from.</summary>
	public ILanguageAnalyzer Analyzer()
	{
		return CSharpRenderedTexts.Catalog().AnalyzersByLanguageName[LanguageName];
	}

	public string PartAPath()
	{
		return Path.Combine(PublicDocuments.MigrationFolder(), PartAFileName);
	}

	public string PartCPath()
	{
		return Path.Combine(PublicDocuments.MigrationFolder(), PartCFileName);
	}
}
