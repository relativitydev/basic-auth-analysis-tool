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
/// DEPLOYMENT IS NOT ON THIS LIST. A folder of that name may hold minified bundle output, but it
/// is also where DEPLOYMENT SCRIPTS live, in PowerShell and Shell. Excluding it by name would
/// hide those scripts, so it is a demotion signal instead, which degrades minified output without
/// hiding a script.
///
/// A NAME-BASED EXCLUSION LIST CANNOT BE COMPLETE, because deployment and vendoring folder names
/// are project conventions, and the safe direction for an incomplete list is to demote rather than
/// to exclude. Excluding trades a false positive for a silent false negative, and only one of those
/// is visible in the output.
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
	/// THE COMPARISON IS ORDINAL - case-SENSITIVE - ON EVERY PLATFORM. Ordinal comparison consults
	/// no locale data and no platform API, so the same tree is excluded identically everywhere,
	/// unlike deferring to the filesystem's own case rule, which is a property of the volume. Where
	/// case folding would exclude a hand-written folder named Bin or Build, ordinal comparison walks
	/// it, which is the safe direction; the tooling that creates every name on this list creates it
	/// in lower case.
	/// </summary>
	public static bool IsExcluded(string directoryName)
	{
		return !string.IsNullOrEmpty(directoryName) && _excludedDirectoryNames.Contains(directoryName);
	}
}
