using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Scanning;

/// <summary>
/// ONE STAGE WIRED, THE REST DELEGATED.
///
/// This runner makes <c>scan</c> do real work and DELEGATES every other command to the runner it
/// wraps, so it holds no second copy of their behaviour to keep in step.
///
/// The next stage's runner wraps this one the same way.
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
