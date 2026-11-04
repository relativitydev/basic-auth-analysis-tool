using MigrateAuth.Cli.Tests.AcceptanceHarness;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// Where the public Migration Documentation for C# lives and how the tests read it. The repository
/// root is found by searching upward from the test assembly for the solution file, the same marker
/// the acceptance harness uses, so the lookup is identical on Windows, Linux and macOS.
/// </summary>
internal static class PublicDocuments
{
	public const string GITATTRIBUTES_FILE_NAME = ".gitattributes";
	public const string PART_A_FILE_NAME = "part-a-csharp.md";
	public const string PART_C_FILE_NAME = "part-c-csharp.md";
	public const string README_FILE_NAME = "README.md";
	public const string USER_GUIDE_INDEX_FILE_NAME = "index.md";
	public const string MARKDOWN_SEARCH_PATTERN = "*.md";

	public const int ROW_USING_THE_TOOL = 1;
	public const int ROW_RELEASE_NOTES = 2;
	public const int ROW_LIMITS = 3;
	public const int ROW_OAUTH2_ESSENTIALS = 4;
	public const int ROW_CONFIGURING_THE_CLIENT = 5;
	public const int ROW_SECRETS = 6;
	public const int ROW_TOKEN_EXPIRY = 7;
	public const int ROW_MORE_INFORMATION = 8;

	private const string _DOCS_FOLDER_NAME = "docs";
	private const string _PUBLIC_FOLDER_NAME = "public";
	private const string _MIGRATION_FOLDER_NAME = "migration";
	private const string _USER_GUIDE_FOLDER_NAME = "user-guide";

	/// <summary>The file of each User Guide row, indexed by row number minus one: one file per row.</summary>
	private static readonly string[] _userGuideRowFileNames =
	[
		"01-using-the-tool.md",
		"02-release-notes.md",
		"03-limits.md",
		"04-oauth2-essentials.md",
		"05-configuring-the-oauth2-client.md",
		"06-secrets.md",
		"07-token-expiry.md",
		"08-more-from-relativity.md"
	];

	public static string RepositoryRoot()
	{
		DirectoryInfo? candidate = new(AppContext.BaseDirectory);

		while (candidate is not null
			&& !File.Exists(Path.Combine(candidate.FullName, StageBenchmarkTable.REPOSITORY_ROOT_MARKER_FILE_NAME)))
		{
			candidate = candidate.Parent;
		}

		return candidate?.FullName
			?? throw new DirectoryNotFoundException("No folder holding the solution file sits above the test assembly.");
	}

	public static string MigrationFolder()
	{
		return Path.Combine(RepositoryRoot(), _DOCS_FOLDER_NAME, _PUBLIC_FOLDER_NAME, _MIGRATION_FOLDER_NAME);
	}

	/// <summary>The root of the public set: nothing outside this folder is published.</summary>
	public static string PublicFolder()
	{
		return Path.Combine(RepositoryRoot(), _DOCS_FOLDER_NAME, _PUBLIC_FOLDER_NAME);
	}

	public static string UserGuideFolder()
	{
		return Path.Combine(PublicFolder(), _USER_GUIDE_FOLDER_NAME);
	}

	public static string ReadmePath()
	{
		return Path.Combine(PublicFolder(), README_FILE_NAME);
	}

	public static int UserGuideRowCount()
	{
		return _userGuideRowFileNames.Length;
	}

	public static string UserGuideRowPath(int row)
	{
		return Path.Combine(UserGuideFolder(), _userGuideRowFileNames[row - 1]);
	}

	public static string ReadUserGuideRow(int row)
	{
		return File.ReadAllText(UserGuideRowPath(row));
	}

	/// <summary>The index and the eight rows of the User Guide.</summary>
	public static string[] UserGuidePaths()
	{
		return [Path.Combine(UserGuideFolder(), USER_GUIDE_INDEX_FILE_NAME), .. Enumerable.Range(1, UserGuideRowCount()).Select(UserGuideRowPath)];
	}

	/// <summary>Every Markdown file of the public set, in a fixed order.</summary>
	public static string[] AllPublicMarkdownPaths()
	{
		return [.. Directory.EnumerateFiles(PublicFolder(), MARKDOWN_SEARCH_PATTERN, SearchOption.AllDirectories).Order(StringComparer.Ordinal)];
	}

	public static string PartAPath()
	{
		return Path.Combine(MigrationFolder(), PART_A_FILE_NAME);
	}

	public static string PartCPath()
	{
		return Path.Combine(MigrationFolder(), PART_C_FILE_NAME);
	}

	public static string GitAttributesPath()
	{
		return Path.Combine(RepositoryRoot(), GITATTRIBUTES_FILE_NAME);
	}
}
