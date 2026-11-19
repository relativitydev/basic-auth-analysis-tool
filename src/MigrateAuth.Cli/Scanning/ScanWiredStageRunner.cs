using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Scanning;

/// <summary>
/// THE INNERMOST WRAP: <c>scan</c> IS HANDLED HERE AND EVERY OTHER COMMAND IS DELEGATED.
///
/// This runner makes <c>scan</c> do real work. It must not change what <c>analyze</c>, <c>suggest</c>,
/// <c>report</c> and <c>ai-assist</c> do, and the safest way to guarantee that is to not have a
/// second copy of their behaviour to keep in step: this runner DELEGATES every command but its own
/// to the runner it wraps, so those stages are still executed by the same classes they always were.
///
/// The next stage's runner wraps this one the same way, and the one after that wraps that; the
/// runner at the bottom of the chain handles <c>ai-assist</c>.
/// </summary>
public sealed class ScanWiredStageRunner : IWorkflowStageRunner
{
	private readonly ScanStage _scanStage;
	private readonly IWorkflowStageRunner _runnerForEveryOtherStage;

	public ScanWiredStageRunner(ScanStage scanStage, IWorkflowStageRunner runnerForEveryOtherStage)
	{
		ArgumentNullException.ThrowIfNull(scanStage);
		ArgumentNullException.ThrowIfNull(runnerForEveryOtherStage);

		_scanStage = scanStage;
		_runnerForEveryOtherStage = runnerForEveryOtherStage;
	}

	/// <inheritdoc />
	public StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		return workflowCommand is CommandId.Scan
			? _scanStage.Run(state, cancellationToken)
			: _runnerForEveryOtherStage.Run(workflowCommand, state, cancellationToken);
	}
}
