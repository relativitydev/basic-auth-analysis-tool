using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Pipeline;

/// <summary>
/// THE SECOND WRAP, ON THE SAME SHAPE AS <c>ScanWiredStageRunner</c>.
///
/// It handles <c>analyze</c> and changes nothing about <c>scan</c>, <c>suggest</c>, <c>report</c>
/// or <c>ai-assist</c>, because it does not contain a second copy of their behaviour - it
/// delegates every command but its own to the runner beneath it.
/// </summary>
public sealed class AnalyzeWiredStageRunner : IWorkflowStageRunner
{
	private readonly AnalyzeStage _analyzeStage;
	private readonly IWorkflowStageRunner _runnerForEveryOtherStage;

	public AnalyzeWiredStageRunner(AnalyzeStage analyzeStage, IWorkflowStageRunner runnerForEveryOtherStage)
	{
		ArgumentNullException.ThrowIfNull(analyzeStage);
		ArgumentNullException.ThrowIfNull(runnerForEveryOtherStage);

		_analyzeStage = analyzeStage;
		_runnerForEveryOtherStage = runnerForEveryOtherStage;
	}

	/// <inheritdoc />
	public StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		return workflowCommand is CommandId.Analyze
			? _analyzeStage.Run(state, cancellationToken)
			: _runnerForEveryOtherStage.Run(workflowCommand, state, cancellationToken);
	}
}
