namespace MigrateAuth.Cli.Reading;

/// <summary>
/// THE CEILINGS ENFORCED BY THE WALK AND THE DECODER.
/// </summary>
public static class ReadingCeilings
{
	/// <summary>
	/// Two megabytes. No hand-written source file reaches it; past that point a file is minified
	/// or generated. It also bounds exposure to catastrophic backtracking, which a line-length
	/// ceiling alone would not cover.
	/// </summary>
	public const int MAXIMUM_FILE_SIZE_IN_BYTES = 2 * 1024 * 1024;

	/// <summary>
	/// Ten thousand characters, counted in UTF-16 code units like every other position here.
	///
	/// A size ceiling does not bound line length, and line length is what drives catastrophic
	/// backtracking. The value is the point past which a line cannot plausibly have been written
	/// by a person.
	///
	/// THE WHOLE FILE IS SKIPPED, NOT THE OFFENDING LINE. Skipping a line is not available: the
	/// scanner is a state machine over the file, so a skipped line leaves the mask state
	/// undefined and an unterminated literal opened inside it would corrupt everything after.
	/// File granularity is the only sound option, which is precisely why the ceiling is generous
	/// - every file it catches is a file nobody looked at.
	/// </summary>
	public const int MAXIMUM_LINE_LENGTH_IN_CHARACTERS = 10_000;

	/// <summary>
	/// A hundred paths per skipped-item category, and the count is never truncated. The
	/// enumeration exists to show a pattern rather than to be complete: a reader is looking for
	/// the shape of the hole - all under one subtree, all the same generated bundle - and a
	/// hundred shows that as well as a thousand does. Across eleven categories the worst case is
	/// an eleven-hundred-line appendix, against the half million a single unreadable subtree
	/// could produce without the bound.
	/// </summary>
	public const int ENUMERATED_SKIPPED_PATHS_PER_CATEGORY = 100;
}
