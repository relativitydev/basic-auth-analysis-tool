namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// The two counters of one analysis run's flow-signal evaluation: how many files it was run on (the
/// count, which includes files whose findings the global cap later omitted) and how many
/// of those evaluations timed out.
///
/// Recorded on the session state beside the other analysis outcomes and persisted with them (schema
/// version 5), so that the counters a report carries survive a resume.
/// </summary>
public sealed record FlowEvaluationDiagnostics
{
	public static FlowEvaluationDiagnostics Empty { get; } = new() { FilesEvaluated = 0, SignalTripCount = 0 };

	public required int FilesEvaluated { get; init; }

	public required int SignalTripCount { get; init; }
}
