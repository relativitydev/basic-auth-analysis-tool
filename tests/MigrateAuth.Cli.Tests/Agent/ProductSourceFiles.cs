using MigrateAuth.Cli.Tests.Documentation;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>The product's own source files, for the tests that read rules from them rather than from behavior.</summary>
internal static class ProductSourceFiles
{
	internal const string PRODUCT_FOLDER = "MigrateAuth.Cli";
	internal const string AGENT_FOLDER = "Agent";
	internal const string SESSION_FOLDER = "Session";
	internal const string CATALOG_FOLDER = "Catalog";
	internal const string PROGRAM_FILE = "Program.cs";

	internal const string SOURCE_PATTERN = "*.cs";
	internal const string BUILD_OUTPUT_OBJECT_FOLDER = "obj";
	internal const string BUILD_OUTPUT_BINARY_FOLDER = "bin";
	internal const string TESTS_FOLDER = "tests";
	internal const string TEST_PROJECT_FOLDER = "MigrateAuth.Cli.Tests";

	private const string _SOURCE_FOLDER = "src";

	/// <summary>The test project's folder.</summary>
	internal static string TestsRoot => Path.Combine(PublicDocuments.RepositoryRoot(), TESTS_FOLDER, TEST_PROJECT_FOLDER);

	/// <summary>The product's folder.</summary>
	internal static string ProductRoot => Path.Combine(PublicDocuments.RepositoryRoot(), _SOURCE_FOLDER, PRODUCT_FOLDER);

	/// <summary>Every source file of the product, never a build output.</summary>
	internal static string[] All()
	{
		return SourceFilesUnder(ProductRoot);
	}

	/// <summary>Every source file of the test project, never a build output.</summary>
	internal static string[] AllTests()
	{
		return SourceFilesUnder(TestsRoot);
	}

	private static string[] SourceFilesUnder(string root)
	{
		return
		[
			.. Directory.EnumerateFiles(root, SOURCE_PATTERN,SearchOption.AllDirectories)
				.Where(path => !IsBuildOutput(root, path))
				.Order(StringComparer.Ordinal)
		];
	}

	/// <summary>The first folder of the file's path below the product's folder, or empty for a file directly in it.</summary>
	internal static string TopFolderOf(string path)
	{
		string relative = Path.GetRelativePath(ProductRoot, path);
		int separator = relative.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

		return separator < 0 ? string.Empty : relative[..separator];
	}

	/// <summary>The path below the product's folder, with forward slashes.</summary>
	internal static string RelativePathOf(string path)
	{
		return Path.GetRelativePath(ProductRoot, path).Replace('\\', '/');
	}

	internal static string Read(string path)
	{
		return File.ReadAllText(path);
	}

	private static bool IsBuildOutput(string root, string path)
	{
		string firstFolder = Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];

		return firstFolder is BUILD_OUTPUT_OBJECT_FOLDER or BUILD_OUTPUT_BINARY_FOLDER;
	}
}
