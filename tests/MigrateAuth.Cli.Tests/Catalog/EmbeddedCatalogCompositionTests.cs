using System.Reflection;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// The embedded catalog is exactly <c>CSharp</c>, <c>PowerShell</c>, <c>Shell</c> and <c>VBNet</c> in every build, and the tested build
/// is the shipped build. The reference fixture analyzer lives in the test project and is registered
/// in-process, so nothing in the shipped assembly names it and validator codes 35 to 37 and the
/// lexical budget guard nothing in the release binary until a language analyzer declares one of the three
/// pre-budget expressions.
/// </summary>
public sealed class EmbeddedCatalogCompositionTests
{
	private const string _CSHARP_LANGUAGE_NAME = "CSharp";
	private const string _POWERSHELL_LANGUAGE_NAME = "PowerShell";
	private const string _SHELL_LANGUAGE_NAME = "Shell";
	private const string _VBNET_LANGUAGE_NAME = "VBNet";
	private const string _REFERENCE_TYPE_NAME_FRAGMENT = "Exemplar";
	private const string _SOURCE_FOLDER_NAME = "src";
	private const string _SOLUTION_FILE_PATTERN = "*.sln";
	private const string _SOURCE_FILE_PATTERN = "*.cs";
	private const string _BUILD_OUTPUT_FOLDER_SEGMENT_BIN = "bin";
	private const string _BUILD_OUTPUT_FOLDER_SEGMENT_OBJ = "obj";

	[Fact]
	public void TheEmbeddedAnalyzersDeclareExactlyCSharpPowerShellShellAndVBNet()
	{
		string[] declaredLanguageNames =
		[
			.. EmbeddedLanguageAnalyzers
				.CreateAll()
				.SelectMany(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames)
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal([_CSHARP_LANGUAGE_NAME, _POWERSHELL_LANGUAGE_NAME, _SHELL_LANGUAGE_NAME, _VBNET_LANGUAGE_NAME], declaredLanguageNames);
	}

	[Fact]
	public void TheEmbeddedCatalogHoldsFourAnalyzersAndTheyAreCSharpPowerShellShellAndVBNet()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.Equal(
			[_CSHARP_LANGUAGE_NAME, _POWERSHELL_LANGUAGE_NAME, _SHELL_LANGUAGE_NAME, _VBNET_LANGUAGE_NAME],
			catalog.AnalyzersByLanguageName.Keys.Order(StringComparer.Ordinal));
		Assert.Equal(4, EmbeddedLanguageAnalyzers.CreateAll().Length);
	}

	[Fact]
	public void TheValidatorReportsNothingForEveryAnalyzerOfTheRegisteredCatalog()
	{
		Assert.All(
			EmbeddedLanguageAnalyzers.CreateAll(),
			analyzer => Assert.Empty(AnalyzerContractValidator.Validate(analyzer)));
	}

	[Fact]
	public void NoTypeNamedExemplarExistsInTheShippedAssembly()
	{
		Assembly shippedAssembly = typeof(EmbeddedLanguageAnalyzers).Assembly;

		string[] referenceTypeNames =
		[
			.. shippedAssembly
				.GetTypes()
				.Select(type => type.FullName ?? type.Name)
				.Where(name => name.Contains(_REFERENCE_TYPE_NAME_FRAGMENT, StringComparison.Ordinal))
		];

		Assert.Empty(referenceTypeNames);
	}

	[Fact]
	public void NoSourceFileUnderSrcMentionsExemplar()
	{
		string sourceFolder = Path.Combine(LocateRepositoryRoot(), _SOURCE_FOLDER_NAME);

		string[] offendingFiles =
		[
			.. Directory
				.EnumerateFiles(sourceFolder, _SOURCE_FILE_PATTERN, SearchOption.AllDirectories)
				.Where(path => !IsBuildOutput(path))
				.Where(path => File.ReadAllText(path).Contains(_REFERENCE_TYPE_NAME_FRAGMENT, StringComparison.Ordinal))
		];

		Assert.Empty(offendingFiles);
	}

	private static bool IsBuildOutput(string path)
	{
		string[] segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		return segments.Contains(_BUILD_OUTPUT_FOLDER_SEGMENT_BIN) || segments.Contains(_BUILD_OUTPUT_FOLDER_SEGMENT_OBJ);
	}

	private static string LocateRepositoryRoot()
	{
		DirectoryInfo? candidate = new(AppContext.BaseDirectory);

		while (candidate is not null && candidate.GetFiles(_SOLUTION_FILE_PATTERN).Length == 0)
		{
			candidate = candidate.Parent;
		}

		return candidate?.FullName
			?? throw new DirectoryNotFoundException("No folder holding a solution file sits above the test assembly.");
	}
}
