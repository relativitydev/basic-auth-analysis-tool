using System.Collections.Immutable;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// What recognition yields for one file: its constructs, classified, subsumption applied and
/// in the total order, and ONE <see cref="Findings.FlowEvidence"/> for the whole file. It is a value,
/// returned to the caller; the engine emits no finding.
/// </summary>
public sealed record RecognitionResult
{
	public required ImmutableArray<RecognizedConstruct> Constructs { get; init; }

	/// <summary>
	/// What the file's flow signals established. <see cref="Findings.FlowEvidence.None"/> where the file
	/// holds no High or Medium construct, where no signal fired, and where evaluation timed out.
	/// </summary>
	public FlowEvidence FlowEvidence { get; init; } = FlowEvidence.None;

	/// <summary>
	/// Whether flow-signal evaluation timed out on this file. The constructs the file already
	/// produced are kept, any partial signal evidence is discarded, and the file is not skipped.
	/// </summary>
	public bool FlowEvaluationTripped { get; init; }
}
