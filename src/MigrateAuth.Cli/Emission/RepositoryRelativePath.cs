using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Emission;

/// <summary>
/// The one form of a path permitted in anything the product emits: relative to the
/// repository root, never absolute, and never the root itself - the root travels as a label.
///
/// It exists as a type rather than as a convention because the invariant's single exemption is
/// the hardest one to police. "No text from the analyzed file, OTHER THAN its path relative to
/// the repository" is a hole shaped exactly like a string, and a string parameter called
/// <c>path</c> accepts a matched line as readily as it accepts a path. So there is no
/// constructor that takes arbitrary text: the only way to obtain one is to hand over a path
/// and the repository it is supposed to sit under, and have the write boundary's containment
/// primitive agree that it does.
///
/// Separators are normalized to the portable form, which makes the emitted value identical on
/// every platform.
/// </summary>
public readonly record struct RepositoryRelativePath
{
	private const char _PORTABLE_DIRECTORY_SEPARATOR = '/';

	private RepositoryRelativePath(string value)
	{
		Value = value;
	}

	/// <summary>The relative path, portable separators. Empty on <c>default</c>.</summary>
	public string Value { get; }

	/// <summary>Whether this instance names a file at all.</summary>
	public bool IsDeclared => !string.IsNullOrEmpty(Value);

	/// <summary>
	/// Accepts a path that the write boundary agrees is contained in the repository, and
	/// refuses everything else - an absolute path elsewhere, a traversal out of the tree, the
	/// repository root itself, or text that is not a path at all.
	///
	/// The containment test is the same primitive the write boundary uses, and the outcome it
	/// must return is the one that REFUSES a write: a path this product may name is exactly a
	/// path inside the repository under assessment.
	/// </summary>
	public static bool TryFromScannedPath(
		RepositoryFolderPath repository,
		string scannedPath,
		out RepositoryRelativePath relativePath)
	{
		bool accepted = WriteBoundary.PermitsReading(repository, scannedPath);

		relativePath = accepted
			? new RepositoryRelativePath(Relativize(repository, scannedPath))
			: default;

		return accepted;
	}

	private static string Relativize(RepositoryFolderPath repository, string scannedPath)
	{
		string absolutePath = Path.GetFullPath(scannedPath, repository.CanonicalPath);
		string relativePath = Path.GetRelativePath(repository.CanonicalPath, absolutePath);

		return relativePath
			.Replace(Path.DirectorySeparatorChar, _PORTABLE_DIRECTORY_SEPARATOR)
			.Replace(Path.AltDirectorySeparatorChar, _PORTABLE_DIRECTORY_SEPARATOR);
	}
}
