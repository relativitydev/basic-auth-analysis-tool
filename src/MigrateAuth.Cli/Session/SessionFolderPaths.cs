using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// Canonicalization shared by the session's two folder paths. It is one implementation used
/// twice rather than two, so that the repository and the destination are canonicalized by the
/// same rule - which is what lets the write boundary compare them for containment.
///
/// The rule itself lives in <see cref="CanonicalPath"/>, which resolves links at every component of
/// the path, so a destination reached through a directory link anywhere above it is compared in its
/// resolved form.
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
		return CanonicalPath.Resolve(suppliedText);
	}

	/// <summary>
	/// The label used in output in place of the path. It is the folder's own name and
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
/// It is a distinct type from <see cref="DocumentationDestinationFolderPath"/>, so no call site can
/// pass one where the other is expected and still compile. Together with named options on the
/// command line, this makes transposing the two paths unrepresentable.
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
	/// keys the session state file, and it is never emitted, because no absolute path appears in
	/// anything the product writes.
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
/// launch. It holds the session state file, the report and the data artifact.
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
