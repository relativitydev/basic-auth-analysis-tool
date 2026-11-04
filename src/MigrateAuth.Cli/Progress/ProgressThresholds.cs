namespace MigrateAuth.Cli.Progress;

/// <summary>
/// THE TWO PROGRESS THRESHOLDS AND THE CADENCE BETWEEN THEM, AS NAMED VALUES.
///
/// They are in their own namespace rather than in the scan, because neither number is a property
/// of scanning: they are properties of the repository, and progress is reported during Analysis
/// as well.
/// </summary>
public static class ProgressThresholds
{
	/// <summary>
	/// Five thousand files. Past this, progress is required output rather than a run that is
	/// indistinguishable from a hung one. Below it, the run is short enough that a progress line
	/// is noise, and the silence is deliberate rather than an omission.
	/// </summary>
	public const int PROGRESS_REPORTING_FILE_COUNT = 5_000;

	/// <summary>
	/// Fifty thousand files. Past this, an advance warning says the scan will take
	/// several minutes - BEFORE scanning begins, which is why the stage is split into an
	/// enumeration pass and an inventory pass. At 5,000 files a minute, fifty thousand files is
	/// ten minutes.
	/// </summary>
	public const int ADVANCE_WARNING_FILE_COUNT = 50_000;

	/// <summary>
	/// Two seconds, which is the required interval. It is a lower bound on how often a line
	/// appears and never a timer: nothing is scheduled, nothing runs on another thread, and a
	/// pass that stalls on one very slow file prints its next line late rather than out of turn.
	/// </summary>
	public const int PROGRESS_REPORTING_INTERVAL_IN_SECONDS = 2;
}
