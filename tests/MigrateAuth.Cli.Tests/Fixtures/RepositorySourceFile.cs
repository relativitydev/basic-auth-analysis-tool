namespace MigrateAuth.Cli.Tests.Fixtures;

/// <summary>
/// Locates a file of this repository's own source tree from the test host, for the tests whose claim
/// is about what the source says rather than about what it does. The root is found by walking up from
/// the test assembly until the product project is seen, so the answer does not depend on the
/// configuration or the framework folder the tests were built into.
/// </summary>
internal static class RepositorySourceFile
{
	private const string _ROOT_MARKER_RELATIVE_PATH = "src/MigrateAuth.Cli/MigrateAuth.Cli.csproj";

	/// <summary>The full path of a file given relative to the repository root.</summary>
	internal static string PathOf(string repositoryRelativePath)
	{
		DirectoryInfo? directory = new(AppContext.BaseDirectory);

		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, _ROOT_MARKER_RELATIVE_PATH)))
		{
			directory = directory.Parent;
		}

		if (directory is null)
		{
			throw new DirectoryNotFoundException(
				$"No parent of '{AppContext.BaseDirectory}' holds '{_ROOT_MARKER_RELATIVE_PATH}'.");
		}

		return Path.Combine(directory.FullName, repositoryRelativePath);
	}
}
