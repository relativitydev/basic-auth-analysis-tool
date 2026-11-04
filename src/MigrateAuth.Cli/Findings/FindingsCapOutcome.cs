namespace MigrateAuth.Cli.Findings;

/// <summary>
/// THE REPORTED TRUNCATION COUNTER: what the per-file and global findings caps admitted and
/// what they held back, so that a truncated result can declare itself truncated rather than reading
/// as complete.
///
/// THE TWO CAPS ARE COUNTED SEPARATELY AND NEVER SUMMED INTO ONE NUMBER, for the same reason
/// <see cref="Reading.SkippedItemTally"/> never adds files to directories: a per-file omission and a
/// global omission answer different questions - "did one file overflow its own share" against "did
/// the run overflow its whole budget" - and collapsing them loses the one a reader needs to tell a
/// single pathological file from an ordinarily large repository.
/// </summary>
public sealed record FindingsCapOutcome
{
	/// <summary>
	/// Every construct recognition produced, before either cap removed anything. Never itself
	/// retained findings, held only as a count.
	/// </summary>
	public required int TotalRecognizedCount { get; init; }

	/// <summary>The findings that survived both caps. Equal to the composed finding set's length.</summary>
	public required int RetainedCount { get; init; }

	/// <summary>How many distinct files, on their own, offered more than the per-file cap allows.</summary>
	public required int PerFileCapTrippedFileCount { get; init; }

	/// <summary>
	/// How many findings were omitted specifically because their own file exceeded the per-file cap
	/// - counted independently of whether the global cap also had room, per
	/// <see cref="FindingComposition"/>'s accumulator.
	/// </summary>
	public required int PerFileOmittedCount { get; init; }

	/// <summary>Whether the run as a whole produced more retainable findings than the global cap allows.</summary>
	public required bool GlobalCapTripped { get; init; }

	/// <summary>
	/// How many findings were omitted specifically because the run's global budget was spent,
	/// independent of any per-file omission already counted for the same file.
	/// </summary>
	public required int GlobalOmittedCount { get; init; }

	/// <summary>Whether either cap removed anything at all. What both shareable artifacts declare on.</summary>
	public bool IsTruncated => PerFileCapTrippedFileCount > 0 || GlobalCapTripped;

	/// <summary>The two omission counts added together, for a reader who wants one number.</summary>
	public int TotalOmittedCount => PerFileOmittedCount + GlobalOmittedCount;

	/// <summary>
	/// The outcome of a process that has not completed an analysis, on the same terms
	/// <see cref="Reporting.ScanSummary.Empty"/> states for Section 2: a report always has a
	/// truncation declaration to render, and this is what it renders before anything has run.
	/// </summary>
	public static FindingsCapOutcome Empty { get; } = new()
	{
		TotalRecognizedCount = 0,
		RetainedCount = 0,
		PerFileCapTrippedFileCount = 0,
		PerFileOmittedCount = 0,
		GlobalCapTripped = false,
		GlobalOmittedCount = 0
	};
}
