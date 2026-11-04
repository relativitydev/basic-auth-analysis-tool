using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// Canonicalization shared by the session's two folder paths. It is one implementation used
/// twice rather than two, so that the repository and the destination are canonicalized by the
/// same rule - which is what lets the write boundary compare them for containment without
/// arguing about whose normalization was applied.
///
/// The rule itself belongs to the write boundary and lives in <see cref="CanonicalPath"/>, which is the
/// type that owns it. This one delegates rather than keeping a second version, so a link is resolved at
/// every component of the path: a destination reached through a directory link named anywhere above it
/// is compared in its resolved form rather than as separate from the repository.
/// </summary>
internal static class SessionFolderPath
{
	private const string _ROOT_FOLDER_LABEL = "(root)";

	/// <summary>
	/// Resolves a supplied folder path to an absolute path with links followed along its whole
	/// length. Link resolution is best effort: a path that does not exist yet, or that cannot
	/// be probed, keeps its non-resolved absolute form, and the caller's own usability check is
	/// what reports the problem.
	/// </summary>
	internal static string Canonicalize(string suppliedText)
	{
		string canonicalPath = CanonicalPath.Resolve(suppliedText);

		RefuseAPathThePlatformRejectsForItsLength(canonicalPath);

		return canonicalPath;
	}

	/// <summary>
	/// Throws <see cref="PathTooLongException"/> when the platform refuses the path for its length,
	/// and does nothing for every other outcome, which the caller's own usability check reports.
	///
	/// THE PLATFORMS REFUSE A LONG PATH AT DIFFERENT MOMENTS. On Windows, making the path absolute
	/// already throws. On Linux and macOS it does not: the path is only text until the system is
	/// asked about it, and the existence checks that follow answer "not there" for a path the system
	/// would not even look up - which tells the user their folder is missing when the real problem is
	/// that the path cannot be used at all. Asking the system for the path's attributes once, here,
	/// surfaces the refusal as the same exception on every platform, and leaves the limit to the
	/// system that enforces it rather than to a number copied into this code.
	/// </summary>
	private static void RefuseAPathThePlatformRejectsForItsLength(string canonicalPath)
	{
		try
		{
			File.GetAttributes(canonicalPath);
		}
		catch (IOException exception) when (exception is not PathTooLongException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	/// <summary>
	/// The label that is permitted in output in place of the path. It is the folder's own name and
	/// carries no route to it, and it is stripped of anything that would act on a renderer
	/// because the name comes from the filesystem rather than from us.
	/// </summary>
	internal static string DeriveLabel(string canonicalPath)
	{
		string trimmedPath = canonicalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string folderName = Path.GetFileName(trimmedPath);
		string label = string.IsNullOrEmpty(folderName) ? _ROOT_FOLDER_LABEL : folderName;

		return ConsoleTextSafety.StripUnsafeCharacters(label);
	}
}

/// <summary>
/// The folder the session audits, supplied by the named option
/// <c>--repositoryFolderPath</c>.
///
/// It is a distinct type from <see cref="DocumentationDestinationFolderPath"/> and that is the
/// point. The two paths must arrive as named options because transposing two
/// positional ones aims the output at the audited repository, and the write boundary
/// catching that afterwards is a last line of defence rather than a first. Naming them makes
/// the transposition unrepresentable on the command line; giving each its own type makes it
/// unrepresentable in the code as well, because no call site can pass one where the other is
/// expected and still compile.
/// </summary>
public readonly record struct RepositoryFolderPath
{
	private RepositoryFolderPath(string canonicalPath, string label)
	{
		CanonicalPath = canonicalPath;
		Label = label;
	}

	/// <summary>
	/// The absolute, link-resolved path. Working data only: it is what opens files and what
	/// keys the session state file, and it is never emitted, because no absolute
	/// path may appear in anything the product writes.
	/// </summary>
	public string CanonicalPath { get; }

	/// <summary>The only form of this path that may appear in output.</summary>
	public string Label { get; }

	/// <summary>Canonicalizes the text a user supplied to the named option.</summary>
	public static RepositoryFolderPath FromSuppliedText(string suppliedText)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(suppliedText);

		string canonicalPath = SessionFolderPath.Canonicalize(suppliedText);

		return new RepositoryFolderPath(canonicalPath, SessionFolderPath.DeriveLabel(canonicalPath));
	}
}

/// <summary>
/// The folder the session writes to, supplied by the named option
/// <c>--documentationDestinationFolderPath</c> and defaulting to the working directory at
/// launch. It holds the session state file, and later the
/// report and the data artifact.
/// </summary>
public readonly record struct DocumentationDestinationFolderPath
{
	private DocumentationDestinationFolderPath(string canonicalPath, string label)
	{
		CanonicalPath = canonicalPath;
		Label = label;
	}

	/// <summary>The absolute, link-resolved path. Never emitted, exactly as above.</summary>
	public string CanonicalPath { get; }

	/// <summary>The only form of this path that may appear in output.</summary>
	public string Label { get; }

	/// <summary>Canonicalizes the text a user supplied to the named option.</summary>
	public static DocumentationDestinationFolderPath FromSuppliedText(string suppliedText)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(suppliedText);

		string canonicalPath = SessionFolderPath.Canonicalize(suppliedText);

		return new DocumentationDestinationFolderPath(canonicalPath, SessionFolderPath.DeriveLabel(canonicalPath));
	}
}
