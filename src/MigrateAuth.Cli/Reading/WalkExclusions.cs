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

	private static readonly HashSet<string> _excludedDirectoryNames =
		new(EXCLUDED_DIRECTORY_NAMES, StringComparer.Ordinal);

	/// <summary>
	/// Whether a directory of this name is walked.
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
}
