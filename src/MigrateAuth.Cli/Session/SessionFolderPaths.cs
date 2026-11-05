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
	private const string _CURRENT_FOLDER_TEXT = ".";
	private const string _PARENT_FOLDER_TEXT = "..";
	private const string _RELATIVE_FORM_PREFIX = "./";
	private static readonly string[] _EXPLICITLY_RELATIVE_PREFIXES = ["./", ".\\", "../", "..\\"];

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

	/// <summary>
	/// The form of a folder path the CONSOLE may show: the text the user supplied, as supplied, with
	/// the console-safety stripping applied. An absolute path is shown as the user typed it. A relative path
	/// is shown relative to the working folder with a leading <c>./</c> unless it already starts with
	/// <c>./</c>, <c>.\</c>, <c>../</c> or <c>..\</c>, and the user's own separators are kept. The folder that is the
	/// working folder itself is shown as <c>.</c>. The canonical path is never an input here and never
	/// appears in the result.
	/// </summary>
	internal static string DeriveConsoleDisplayText(string suppliedText)
	{
		string safeText = ConsoleTextSafety.StripUnsafeCharacters(suppliedText);
		bool alreadyShownRelativeToTheWorkingFolder =
			Path.IsPathRooted(safeText)
			|| string.Equals(safeText, _CURRENT_FOLDER_TEXT, StringComparison.Ordinal)
			|| string.Equals(safeText, _PARENT_FOLDER_TEXT, StringComparison.Ordinal)
			|| _EXPLICITLY_RELATIVE_PREFIXES.Any(prefix => safeText.StartsWith(prefix, StringComparison.Ordinal));

		return alreadyShownRelativeToTheWorkingFolder ? safeText : _RELATIVE_FORM_PREFIX + safeText;
	}

	/// <summary>The console form of the working folder at launch, the destination's default.</summary>
	internal static string DefaultConsoleDisplayText => _CURRENT_FOLDER_TEXT;
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
	/// path may appear in anything the product writes. (The rule is narrowed for the OUTPUT folder only:
	/// see <see cref="DocumentationDestinationFolderPath.ConsoleDisplayText"/>. The repository stays label-only.)
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
///
/// THE CONSOLE NAMES THIS FOLDER BY THE TEXT THE USER SUPPLIED, through
/// <see cref="ConsoleDisplayText"/>. The report and the data artifact never name it at all, and the
/// canonical path is never printed anywhere.
/// </summary>
public readonly record struct DocumentationDestinationFolderPath
{
	private DocumentationDestinationFolderPath(string canonicalPath, string label, string consoleDisplayText)
	{
		CanonicalPath = canonicalPath;
		Label = label;
		ConsoleDisplayText = consoleDisplayText;
	}

	/// <summary>The absolute, link-resolved path. Never emitted, exactly as above.</summary>
	public string CanonicalPath { get; }

	/// <summary>The folder's own name. The console names this folder by <see cref="ConsoleDisplayText"/> instead.</summary>
	public string Label { get; }

	/// <summary>
	/// The only form of this folder that the console may show: the path as the user supplied it (relative to
	/// the working folder when it is the default or relative, for example <c>./Outputs</c>), already stripped
	/// of anything that could act on a terminal. It is never the canonical path, and it is never written to
	/// the report or the data artifact, which stay label-only.
	/// </summary>
	public string ConsoleDisplayText { get; }

	/// <summary>Canonicalizes the text a user supplied to the named option.</summary>
	public static DocumentationDestinationFolderPath FromSuppliedText(string suppliedText)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(suppliedText);

		return FromResolvedText(suppliedText, SessionFolderPath.DeriveConsoleDisplayText(suppliedText));
	}

	/// <summary>
	/// Canonicalizes <paramref name="textToCanonicalize"/> and shows <paramref name="consoleDisplayText"/> for
	/// it. For the callers whose canonical source is not what the user typed: the working folder
	/// default, and a leading <c>~</c> that was expanded before canonicalization.
	/// </summary>
	public static DocumentationDestinationFolderPath FromResolvedText(string textToCanonicalize, string consoleDisplayText)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(textToCanonicalize);
		ArgumentException.ThrowIfNullOrWhiteSpace(consoleDisplayText);

		string canonicalPath = SessionFolderPath.Canonicalize(textToCanonicalize);

		return new DocumentationDestinationFolderPath(
			canonicalPath,
			SessionFolderPath.DeriveLabel(canonicalPath),
			ConsoleTextSafety.StripUnsafeCharacters(consoleDisplayText));
	}

	/// <summary>The console form of a supplied text, for a caller that has expanded it and shows the original.</summary>
	public static string DeriveConsoleDisplayText(string suppliedText) =>
		SessionFolderPath.DeriveConsoleDisplayText(suppliedText);

	/// <summary>The console form of the default destination, the working folder at launch.</summary>
	public static string DefaultConsoleDisplayText => SessionFolderPath.DefaultConsoleDisplayText;
}
