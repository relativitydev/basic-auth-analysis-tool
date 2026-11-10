namespace MigrateAuth.Cli.Agent;

/// <summary>
/// HOW A LAUNCHED AGENT ENDED. There are five outcomes because each carries a different consequence
/// for the session. Not finding the agent is a configuration problem the user can act on; an exit
/// code is the agent's own verdict and belongs to the agent; a signal says the environment killed it;
/// a cancellation is the user changing their mind and is not a failure at all - the same reading
/// <see cref="MigrateAuth.Cli.Session.StageRunOutcome.Cancelled"/> already takes; and a timeout is an
/// agent that hung and was not waited on further, which is not the same as an agent that ran and lost.
/// </summary>
public enum AgentTerminationOutcome
{
	/// <summary>The agent could not be resolved, so nothing was launched.</summary>
	NotFound = 0,

	/// <summary>The agent ran to completion and returned its own exit code.</summary>
	ExitedWithCode = 1,

	/// <summary>The environment terminated the agent with a signal.</summary>
	TerminatedBySignal = 2,

	/// <summary>The user interrupted the agent. Not a failure, and it sets no exit code.</summary>
	CancelledByUser = 3,

	/// <summary>The agent outlived the budget the launcher gives it and was not waited on further.</summary>
	TimedOut = 4
}

/// <summary>
/// WHAT A LAUNCH PRODUCED - the outcome, plus the one datum that outcome and no other has.
///
/// The shape is <see cref="MigrateAuth.Cli.Writing.ArtifactWriteResult"/>'s: a closed enumeration
/// carried alongside the fields the enumerated cases need, rather than five types a caller has to
/// test with <c>is</c>. WHAT IT DOES DIFFERENTLY IS LEAVE THE INAPPLICABLE FIELDS ABSENT rather
/// than empty. An exit code means something only when the agent exited with one, and a signal
/// number only when a signal killed it; published as plain integers they would read as zero in the
/// other four cases, and ZERO IS A REAL EXIT CODE - the one meaning success. A caller that forgot
/// to check the outcome first would conclude the agent succeeded from an agent that was never
/// found. Nullable removes that reading: absent is not a value that can be mistaken for one.
///
/// THERE IS NO MESSAGE FIELD: the text that reports each of these five belongs to the console
/// surface, not to this type.
///
/// NOTHING HERE ORIGINATES IN THE ANALYZED REPOSITORY, and nothing can: the type's whole content is
/// one enumeration value and two integers produced by the operating system.
/// </summary>
public sealed record AgentTerminationStatus
{
	/// <summary>The lowest number a signal can carry, below which the value is not a signal.</summary>
	public const int LOWEST_SIGNAL_NUMBER = 1;

	private AgentTerminationStatus(AgentTerminationOutcome outcome, int? exitCode, int? signalNumber)
	{
		Outcome = outcome;
		ExitCode = exitCode;
		SignalNumber = signalNumber;
	}

	/// <summary>Which of the five happened.</summary>
	public AgentTerminationOutcome Outcome { get; }

	/// <summary>
	/// The code the agent exited with. Absent for every outcome but
	/// <see cref="AgentTerminationOutcome.ExitedWithCode"/>.
	/// </summary>
	public int? ExitCode { get; }

	/// <summary>
	/// The signal that terminated the agent. Absent for every outcome but
	/// <see cref="AgentTerminationOutcome.TerminatedBySignal"/>.
	/// </summary>
	public int? SignalNumber { get; }

	/// <summary>The agent was not resolved from the search set, so no process was started.</summary>
	public static AgentTerminationStatus NotFound() =>
		new(AgentTerminationOutcome.NotFound, exitCode: null, signalNumber: null);

	/// <summary>The agent exited on its own and this is the code it returned.</summary>
	public static AgentTerminationStatus ExitedWithCode(int exitCode) =>
		new(AgentTerminationOutcome.ExitedWithCode, exitCode, signalNumber: null);

	/// <summary>The agent was terminated by a signal, which the platform numbers.</summary>
	public static AgentTerminationStatus TerminatedBySignal(int signalNumber)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(signalNumber, LOWEST_SIGNAL_NUMBER);

		return new AgentTerminationStatus(
			AgentTerminationOutcome.TerminatedBySignal,
			exitCode: null,
			signalNumber);
	}

	/// <summary>The user interrupted the agent.</summary>
	public static AgentTerminationStatus CancelledByUser() =>
		new(AgentTerminationOutcome.CancelledByUser, exitCode: null, signalNumber: null);

	/// <summary>The agent outlived its budget.</summary>
	public static AgentTerminationStatus TimedOut() =>
		new(AgentTerminationOutcome.TimedOut, exitCode: null, signalNumber: null);
}
