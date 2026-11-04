using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Pipeline;

/// <summary>
/// The third wrap, identical in shape to the two below it. It handles <c>suggest</c> and delegates
/// everything else, so wiring this stage cannot change what any other one does.
/// </summary>
public sealed class SuggestWiredStageRunner : IWorkflowStageRunner
{
	private readonly SuggestStage _suggestStage;
	private readonly IWorkflowStageRunner _runnerForEveryOtherStage;

	public SuggestWiredStageRunner(SuggestStage suggestStage, IWorkflowStageRunner runnerForEveryOtherStage)
	{
		ArgumentNullException.ThrowIfNull(suggestStage);
		ArgumentNullException.ThrowIfNull(runnerForEveryOtherStage);

		_suggestStage = suggestStage;
		_runnerForEveryOtherStage = runnerForEveryOtherStage;
	}

	/// <inheritdoc />
	public StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		return workflowCommand is CommandId.Suggest
			? _suggestStage.Run(state, cancellationToken)
			: _runnerForEveryOtherStage.Run(workflowCommand, state, cancellationToken);
	}
}
