namespace MigrateAuth.Cli.Reading;

/// <summary>
/// THE DIRECTORY NAMES THE WALK DOES NOT DESCEND INTO.
///
/// The list holds eight names, and nothing is added to it. Vendored code and environment
/// folders are read and judged on their own: a language's demotion signals lower what a
/// vendored folder holds, and nothing here hides it.
///
/// AN EXCLUDED DIRECTORY IS NOT A SKIPPED ITEM. Nothing here produces a skipped-item record: a
/// skipped item is a hole in coverage the tool could not avoid, and an exclusion is a policy the
/// tool chose. What the exclusions cost is stated in the coverage statement instead, which is
/// where a reader looks to find out what was not examined on purpose.
///
/// A NAME-BASED EXCLUSION LIST CANNOT BE COMPLETE, because deployment and vendoring folder names
/// are project conventions, and the safe direction for an incomplete list is to demote rather
/// than to exclude. Excluding trades a false positive for a silent false negative, and only one
/// of those is visible in the output.
/// </summary>
public static class WalkExclusions
{
	/// <summary>
	/// The eight excluded names, in a fixed order.
	/// </summary>
	public static readonly string[] EXCLUDED_DIRECTORY_NAMES =
	[
		"bin",
		"obj",
		".git",
		".vs",
		"node_modules",
		"dist",
		"build",
		"packages"
	];

	/// <summary>
	/// The one name on the list that is excluded only conditionally. See <see cref="IsBuildOutputBin"/>.
	/// </summary>
	public const string CONDITIONALLY_EXCLUDED_DIRECTORY_NAME = "bin";

	/// <summary>
	/// The extensions of a compiled project file. A folder named <c>bin</c> is build output when its
	/// parent holds a file with one of these.
	/// </summary>
	public static readonly string[] COMPILED_PROJECT_FILE_EXTENSIONS =
	[
		".csproj",
		".vbproj",
		".fsproj"
	];

	private static readonly HashSet<string> _excludedDirectoryNames =
		new(EXCLUDED_DIRECTORY_NAMES.Where(name => !string.Equals(name, CONDITIONALLY_EXCLUDED_DIRECTORY_NAME, StringComparison.Ordinal)), StringComparer.Ordinal);

	/// <summary>
	/// Whether a directory of this name is excluded by its name alone. <c>bin</c> is not: its exclusion
	/// depends on the folder that holds it, which a name cannot say - use <see cref="IsBuildOutputBin"/>.
	///
	/// THE COMPARISON IS ORDINAL - case-SENSITIVE - ON EVERY PLATFORM. It must be identical on
	/// every platform, which rules out deferring to the filesystem's own case rule, since that is a
	/// property of the volume rather than of the operating system; ordinal comparison consults no
	/// locale data and no platform API, so the same tree is excluded identically everywhere, which
	/// keeps the SET of findings deterministic and not only their order.
	///
	/// Folding case would EXCLUDE MORE: a hand-written source folder named Bin or Build would vanish
	/// silently. Comparing ordinally INCLUDES MORE: a build folder named BIN is walked and its
	/// generated content becomes noise a demotion signal can handle. The safe direction for an
	/// incomplete list is to exclude less, and the tooling that creates every name on this list
	/// creates it in lower case.
	/// </summary>
	public static bool IsExcluded(string directoryName)
	{
		return !string.IsNullOrEmpty(directoryName) && _excludedDirectoryNames.Contains(directoryName);
	}

	/// <summary>
	/// A FOLDER NAMED <c>bin</c> IS BUILD OUTPUT, AND STAYS EXCLUDED, WHEN ITS PARENT HOLDS A COMPILED
	/// PROJECT FILE (<c>.csproj</c>, <c>.vbproj</c> or <c>.fsproj</c>); EVERY OTHER <c>bin</c> IS
	/// WALKED LIKE ANY OTHER FOLDER. Elsewhere that name is where scripts are kept, and excluding it
	/// would hide them silently.
	///
	/// THE DECISION USES FILE NAMES ONLY. <paramref name="parentEntries"/> is the listing of the
	/// parent directory that the walk has already taken, so nothing is opened and nothing is listed
	/// again, and the cost is one pass over one directory's names per <c>bin</c> folder. The folder
	/// name is compared ordinally and case-sensitively, as every other name on the list is, so
	/// <c>BIN</c> is walked. The project extension is compared ordinally and case-insensitively, as
	/// language extensions are (<c>App.CSPROJ</c> is a project file); both are independent of the
	/// platform and of the volume's case rule. A directory that merely carries such a name is not a
	/// project file.
	/// </summary>
	public static bool IsBuildOutputBin(string directoryName, IEnumerable<FileSystemInfo> parentEntries)
	{
		ArgumentNullException.ThrowIfNull(parentEntries);

		return string.Equals(directoryName, CONDITIONALLY_EXCLUDED_DIRECTORY_NAME, StringComparison.Ordinal)
			&& parentEntries.Any(entry => entry is not DirectoryInfo && HasCompiledProjectExtension(entry.Name));
	}

	private static bool HasCompiledProjectExtension(string fileName)
	{
		return COMPILED_PROJECT_FILE_EXTENSIONS.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
	}
}
