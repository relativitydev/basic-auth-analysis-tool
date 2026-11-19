using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Documentation;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE PRODUCT'S NATIVE CALLS ARE SOURCE-GENERATED AND NOTHING ELSE. A runtime-bound import marshals through code
/// generated at run time, which the published native binary does not carry; the generated form is bound at build
/// time. These rules read the product's own files: no runtime-bound import attribute, every source-generated
/// import loaded from the system folder only, and they live in the one file of the Windows console.
/// </summary>
public sealed class NoUnmanagedSurfaceSourceTests
{
	private const string _BUILD_OUTPUT_OBJECT_FOLDER = "obj";
	private const string _BUILD_OUTPUT_BINARY_FOLDER = "bin";
	private const string _PRODUCT_FOLDER = "MigrateAuth.Cli";
	private const string _CONSOLE_FILE = "WindowsNativeConsole.cs";
	private const string _SYSTEM_FOLDER_ATTRIBUTE = "[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]";
	private const int _DECLARED_CONSOLE_CALLS = 4;

	[Fact]
	public void NoRuntimeBoundImportAttributeAppearsInTheProductSource()
	{
		Regex runtimeBoundImport = new(@"\bDllImport\b", RegexOptions.CultureInvariant);

		string[] offenders = [.. ProductFiles().Where(file => runtimeBoundImport.IsMatch(File.ReadAllText(file))).Select(file => Path.GetFileName(file))];

		Assert.Empty(offenders);
	}

	[Fact]
	public void SourceGeneratedImportsAppearOnlyInTheWindowsConsoleFile()
	{
		string[] files = [.. ProductFiles().Where(file => File.ReadAllText(file).Contains("[LibraryImport(", StringComparison.Ordinal)).Select(file => Path.GetFileName(file))];

		Assert.Equal([_CONSOLE_FILE], files);
	}

	[Fact]
	public void TheWindowsConsoleDeclaresExactlyTheFourCallsEachLoadedFromTheSystemFolderOnly()
	{
		string source = File.ReadAllText(ProductFiles().Single(file => Path.GetFileName(file) == _CONSOLE_FILE));

		Assert.Equal(_DECLARED_CONSOLE_CALLS, Regex.Count(source, Regex.Escape("[LibraryImport(")));
		Assert.Equal(_DECLARED_CONSOLE_CALLS, Regex.Count(source, Regex.Escape(_SYSTEM_FOLDER_ATTRIBUTE)));
	}

	[Fact]
	public void TheProjectAllowsUnsafeBlocksForTheGeneratedCodeAndTheSourceWritesNone()
	{
		string project = File.ReadAllText(Path.Combine(PublicDocuments.RepositoryRoot(), "src", _PRODUCT_FOLDER, $"{_PRODUCT_FOLDER}.csproj"));

		Assert.Contains("<AllowUnsafeBlocks>true</AllowUnsafeBlocks>", project, StringComparison.Ordinal);
		Assert.DoesNotContain(ProductFiles(), file => Regex.IsMatch(File.ReadAllText(file), @"\bunsafe\b"));
	}

	/// <summary>The product's source files, leaving out build output, which holds the generator's own code.</summary>
	private static string[] ProductFiles()
	{
		string productRoot = Path.Combine(PublicDocuments.RepositoryRoot(), "src", _PRODUCT_FOLDER);

		return
		[
			.. Directory.EnumerateFiles(productRoot, "*.cs", SearchOption.AllDirectories)
				.Where(file => Path.GetRelativePath(productRoot, file).Split(Path.DirectorySeparatorChar)[0] is not (_BUILD_OUTPUT_OBJECT_FOLDER or _BUILD_OUTPUT_BINARY_FOLDER))
		];
	}
}
