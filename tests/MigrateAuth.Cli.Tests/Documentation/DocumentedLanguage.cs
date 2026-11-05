using System.Collections.Immutable;

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
	/// <summary>The two variants of how an Authorization Code entry says the code is received, as the labels each language's templates write them.</summary>
	public ImmutableArray<(string Element, string Marker)> AuthorizationCodeVariantMarkers { get; init; } =
	[
		("the server-side variant", PublicDocumentChecks.SERVER_SIDE_VARIANT_MARKER),
		("the desktop variant", PublicDocumentChecks.DESKTOP_VARIANT_MARKER)
	];

	/// <summary>The label of the Client Credentials advice on caching the token, as each language's templates write it.</summary>
	public string CacheAndReacquireMarker { get; init; } = PublicDocumentChecks.CACHE_AND_RE_ACQUIRE_MARKER;

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

	/// <summary>The third language: Part A and Part C of VB.NET.</summary>
	public static readonly DocumentedLanguage VBNet = new(
		"VB.NET",
		"VBNet",
		AssessedLanguage.VBNet,
		"part-a-vbnet.md",
		"part-c-vbnet.md",
		VBNetRenderedTexts.UNDECLARED_PATTERN_CLASS,
		"vbnet",
		"*.vb");

	/// <summary>The fourth language: Part A and Part C of Shell.</summary>
	public static readonly DocumentedLanguage Shell = new(
		"Shell",
		"Shell",
		AssessedLanguage.Shell,
		"part-a-shell.md",
		"part-c-shell.md",
		ShellRenderedTexts.UNDECLARED_PATTERN_CLASS,
		"sh",
		"*.sh")
	{
		AuthorizationCodeVariantMarkers =
		[
			("the manual copy variant", PublicDocumentChecks.MANUAL_COPY_VARIANT_MARKER),
			("the loopback listener variant", PublicDocumentChecks.LOOPBACK_LISTENER_VARIANT_MARKER)
		]
	};

	/// <summary>The fifth language: Part A and Part C of JavaScript.</summary>
	public static readonly DocumentedLanguage JavaScript = new(
		"JavaScript",
		"JavaScript",
		AssessedLanguage.JavaScript,
		"part-a-javascript.md",
		"part-c-javascript.md",
		JavaScriptRenderedTexts.UNDECLARED_PATTERN_CLASS,
		"javascript",
		"*.js")
	{
		CacheAndReacquireMarker = PublicDocumentChecks.CACHE_AND_RE_ACQUIRE_WITHOUT_COMMA_MARKER,
		AuthorizationCodeVariantMarkers =
		[
			("the browser variant", PublicDocumentChecks.BROWSER_VARIANT_MARKER),
			("the Node.js server variant", PublicDocumentChecks.NODE_SERVER_VARIANT_MARKER),
			("the Node.js command line or desktop variant", PublicDocumentChecks.NODE_COMMAND_LINE_VARIANT_MARKER)
		]
	};

	/// <summary>Every language whose Part A and Part C are published, in the order the User Guide lists them.</summary>
	public static readonly ImmutableArray<DocumentedLanguage> All = [CSharp, PowerShell, VBNet, Shell, JavaScript];

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
