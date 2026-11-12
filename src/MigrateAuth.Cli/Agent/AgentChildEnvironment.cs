using System.Diagnostics;

using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// THE ENVIRONMENT OF THE CHILD, which differs from the inherited one in two ways and no more.
///
/// The child's search path has every entry that lies inside the repository removed, so a program a
/// repository put on it cannot be found by anything the agent or its wrapper starts by a bare name.
/// An empty entry and a relative entry mean the working directory, which is the repository, so they are
/// resolved against it and removed when they land inside; an entry that cannot be read as a path is removed
/// too, because a search path entry nobody can interpret is not one to keep. An entry that stays is kept
/// exactly as it was written (an entry written in quotes is read without them). On Windows the child also gets the variable that stops the current directory
/// being searched for programs.
///
/// The entries come from the same injected resolution set the resolver used, so what the child can find and
/// what the resolver found are one list.
/// </summary>
public static class AgentChildEnvironment
{
	/// <summary>The name of the search path variable.</summary>
	public const string SEARCH_PATH_VARIABLE = "PATH";

	/// <summary>The Windows variable that removes the current directory from the program search.</summary>
	public const string NO_CURRENT_DIRECTORY_VARIABLE = "NoDefaultCurrentDirectoryInExePath";

	/// <summary>The value the variable is set to.</summary>
	public const string NO_CURRENT_DIRECTORY_VALUE = "1";

	private const char _QUOTE = '"';

	/// <summary>Applies the child's environment to <paramref name="startInfo"/>.</summary>
	public static void Apply(
		ProcessStartInfo startInfo,
		IReadOnlyList<string> searchPathEntries,
		string repositoryCanonicalPath)
	{
		ArgumentNullException.ThrowIfNull(startInfo);

		startInfo.Environment[SEARCH_PATH_VARIABLE] =
			string.Join(Path.PathSeparator, KeepEntriesOutsideRepository(searchPathEntries, repositoryCanonicalPath));

		if (OperatingSystem.IsWindows())
		{
			startInfo.Environment[NO_CURRENT_DIRECTORY_VARIABLE] = NO_CURRENT_DIRECTORY_VALUE;
		}
	}

	/// <summary>The entries that do not lie inside the repository, in their order and unchanged.</summary>
	public static IReadOnlyList<string> KeepEntriesOutsideRepository(
		IReadOnlyList<string> searchPathEntries,
		string repositoryCanonicalPath)
	{
		ArgumentNullException.ThrowIfNull(searchPathEntries);
		ArgumentException.ThrowIfNullOrWhiteSpace(repositoryCanonicalPath);

		return [.. searchPathEntries.Where(entry => !IsInsideRepository(entry, repositoryCanonicalPath))];
	}

	private static bool IsInsideRepository(string entry, string repositoryCanonicalPath)
	{
		bool inside;

		try
		{
			string unquotedEntry = entry.Trim().Trim(_QUOTE);
			string absoluteEntry = unquotedEntry.Length == 0
				? repositoryCanonicalPath
				: Path.GetFullPath(unquotedEntry, repositoryCanonicalPath);
			PathOverlap overlap = WriteBoundary.ComparePaths(repositoryCanonicalPath, CanonicalPath.Resolve(absoluteEntry));

			inside = overlap is PathOverlap.SamePath or PathOverlap.DestinationInsideRepository;
		}
		catch (ArgumentException)
		{
			inside = true;
		}
		catch (IOException)
		{
			inside = true;
		}
		catch (NotSupportedException)
		{
			inside = true;
		}

		return inside;
	}
}
