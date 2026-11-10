using System.Globalization;

namespace MigrateAuth.Cli.Session;

/// <summary>How a stage ended. Each of the three has a different consequence.</summary>
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
	/// The stage is not offered by this release (<c>ai-assist</c>). The shell leaves completion
	/// state exactly as it found it: a stage commits only on <see cref="Completed"/>, so there is
	/// nothing to mark and nothing downstream to invalidate. The shell also writes nothing further,
	/// because the stage runner that returns this outcome has already said everything there is to
	/// say in its own, client-facing words - a second, generic line here would either repeat that
	/// text or contradict it.
	/// </summary>
	NotAvailable = 3
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

/// <summary>
/// THE STAGE RUNNER FOR THE ONE WORKFLOW COMMAND THIS RELEASE DOES NOT SHIP: <c>ai-assist</c>.
///
/// It is the class at the bottom of <c>Program</c>'s decorator chain. Running it names what the
/// client already has, and the outcome is <see cref="StageRunOutcome.NotAvailable"/> rather than
/// <see cref="StageRunOutcome.Completed"/>, so the session's own bookkeeping does not assert a
/// review took place and <c>help</c> can still say the stage ships later.
/// </summary>
public sealed class AiAssistNotAvailableStageRunner : IWorkflowStageRunner
{
	/// <summary>
	/// The command list names <c>ai-assist</c> as the optional fifth workflow command; this is what
	/// running it says while its build belongs to a later release.
	/// </summary>
	public const string AI_ASSIST_NOT_AVAILABLE_MESSAGE_FORMAT =
		"{0}: AI-assisted review is not available in this release. The report and the JSON findings file already hold the complete assessment.";

	private readonly TextWriter _outputWriter;

	public AiAssistNotAvailableStageRunner(TextWriter outputWriter)
	{
		ArgumentNullException.ThrowIfNull(outputWriter);

		_outputWriter = outputWriter;
	}

	/// <inheritdoc />
	public StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		string stageName = CommandRegistry.Describe(workflowCommand).Name;

		_outputWriter.WriteLine(
			string.Format(CultureInfo.InvariantCulture, AI_ASSIST_NOT_AVAILABLE_MESSAGE_FORMAT, stageName));

		return StageRunOutcome.NotAvailable;
	}
}
