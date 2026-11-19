using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Pipeline;

/// <summary>
/// THE OUTERMOST WRAP, WITH THE AI-ASSIST RUNNER AT THE BOTTOM OF THE CHAIN.
///
/// All five workflow stages do real work. The four that belong to the assessment are wrapped here and
/// in the three runners beneath this one; <c>ai-assist</c> is handled by the runner at the bottom of
/// the chain, which the session composition builds, and nothing in these four stages references it.
/// This runner decides only whether the command is <c>report</c>, and delegates every other command
/// to the runner it wraps.
/// </summary>
public sealed class ReportWiredStageRunner : IWorkflowStageRunner
{
	private readonly ReportStage _reportStage;
	private readonly IWorkflowStageRunner _runnerForEveryOtherStage;

	public ReportWiredStageRunner(ReportStage reportStage, IWorkflowStageRunner runnerForEveryOtherStage)
	{
		ArgumentNullException.ThrowIfNull(reportStage);
		ArgumentNullException.ThrowIfNull(runnerForEveryOtherStage);

		_reportStage = reportStage;
		_runnerForEveryOtherStage = runnerForEveryOtherStage;
	}

	/// <inheritdoc />
	public StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		return workflowCommand is CommandId.Report
			? _reportStage.Run(state, cancellationToken)
			: _runnerForEveryOtherStage.Run(workflowCommand, state, cancellationToken);
	}
}
