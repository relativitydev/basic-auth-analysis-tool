using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Pipeline;

/// <summary>
/// THE OUTERMOST WRAP, WITH THE EMPTY RUNNER UNDERNEATH IT.
///
/// Four of the five workflow stages do real work. <c>ai-assist</c> does not: it resolves
/// through <c>AiAssistNotAvailableStageRunner</c> at the bottom of this chain, which is the one
/// and only place <c>ai-assist</c> is handled.
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
