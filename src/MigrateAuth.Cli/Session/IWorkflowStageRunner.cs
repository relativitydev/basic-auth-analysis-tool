namespace MigrateAuth.Cli.Session;

/// <summary>How a stage ended. Each of the four has a different consequence.</summary>
public enum StageRunOutcome
{
	/// <summary>The stage finished. It commits here, and only here.</summary>
	Completed = 0,

	/// <summary>
	/// The stage was interrupted. The session stays intact, the stage is not completed, and
	/// nothing upstream is invalidated. It is not an error and it sets no exit code.
	/// </summary>
	Cancelled = 1,

	/// <summary>
	/// The stage could not finish its work. It reports and returns to the prompt for the same
	/// reason cancellation does: the completed stages are still valid.
	/// </summary>
	Failed = 2,

	/// <summary>
	/// The stage printed its own result: <c>ai-assist</c>, which owns its console, its decisions and its own
	/// closing line. The shell leaves completion state exactly as it found it - a stage commits only on
	/// <see cref="Completed"/>, so there is nothing to mark and nothing downstream to invalidate - and writes
	/// nothing further, because a second, generic line would either repeat what the stage said or contradict it.
	/// Every path of that stage returns this value, and the session state is read by it and never written.
	/// </summary>
	SelfReported = 3
}

/// <summary>
/// The seam each later stage plugs into. The shell owns gating, cascade invalidation
/// and persistence; a stage owns its work and reports only how it ended.
///
/// Keeping the two apart is what lets the walk, the analysis, the recommendations and the
/// report arrive one at a time without any of them touching the gating chain.
/// </summary>
public interface IWorkflowStageRunner
{
	/// <summary>Runs one workflow stage.</summary>
	/// <param name="workflowCommand">The stage to run. Gating has already been satisfied.</param>
	/// <param name="state">The session state the stage reads from and, later, contributes to.</param>
	/// <param name="cancellationToken">Signalled when the user interrupts the stage.</param>
	StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken);
}
