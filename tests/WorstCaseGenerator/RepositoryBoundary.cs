namespace MigrateAuth.WorstCase;

/// <summary>
/// Decides whether a folder lies inside a repository. The generated tree is measurement material and
/// must never be written where it could be committed, so a target inside a repository is refused.
/// A folder is inside one when it, or an ancestor, holds a repository marker, or when it lies inside
/// the repository this tool was started from or built in.
/// </summary>
public static class RepositoryBoundary
{
	private const string _GIT_MARKER = ".git";
	private const string _SOLUTION_MARKER = "migrate-auth.sln";

	/// <summary>True when the folder lies inside a repository.</summary>
	public static bool IsInsideRepository(string folder)
	{
		string fullPath = Path.GetFullPath(folder);
		bool inside = FindRepositoryRoot(fullPath) is not null;

		foreach (string startFolder in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
		{
			string? root = FindRepositoryRoot(Path.GetFullPath(startFolder));

			inside = inside || (root is not null && IsSameOrBelow(fullPath, root));
		}

		return inside;
	}

	/// <summary>The nearest ancestor-or-self holding a repository marker, or null.</summary>
	public static string? FindRepositoryRoot(string startFolder)
	{
		DirectoryInfo? current = new(startFolder);
		string? root = null;

		while (current is not null && root is null)
		{
			bool marked = Directory.Exists(Path.Combine(current.FullName, _GIT_MARKER))
				|| File.Exists(Path.Combine(current.FullName, _GIT_MARKER))
				|| File.Exists(Path.Combine(current.FullName, _SOLUTION_MARKER));

			root = marked ? current.FullName : null;
			current = current.Parent;
		}

		return root;
	}

	private static bool IsSameOrBelow(string path, string root)
	{
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		string rootWithSeparator = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
		string pathWithSeparator = Path.TrimEndingDirectorySeparator(path) + Path.DirectorySeparatorChar;

		return pathWithSeparator.StartsWith(rootWithSeparator, comparison);
	}
}
