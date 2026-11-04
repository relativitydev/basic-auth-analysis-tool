namespace MigrateAuth.Cli.Agent;

/// <summary>
/// Whether an external agent can be launched at all.
///
/// An agent that is present but cannot be launched is not available: on one of the supported
/// platforms the agents install script wrappers which the direct process launch mechanism cannot
/// execute, so checking availability and knowing how to launch are the same question.
///
/// IT CARRIES NO RESOLVED PATH. The absolute path the launch uses is settled inside the
/// implementation and stays there: no absolute path may appear in anything the product emits,
/// and an availability answer is read in order to say something to the user.
/// </summary>
public enum AgentAvailability
{
	/// <summary>Nothing in the declared resolution set is an agent this launcher can start.</summary>
	NotFound = 0,

	/// <summary>An agent was resolved and can be launched.</summary>
	Available = 1
}

/// <summary>
/// THE ISOLATION BOUNDARY FOR THE EXTERNAL AGENT, AS A CONTRACT.
///
/// Every argument, every wrapper quirk and every platform difference lives behind an implementation
/// of this interface, so changes in an external tool's command-line interface stay contained, and the
/// session above it knows only whether an agent is available and how the run ended.
///
/// TWO MEMBERS FOR THE THREE OPERATIONS - check availability, launch with a prompt pack path, and
/// return termination status. The third is the second's answer rather than a separate call: the child
/// INHERITS THE TERMINAL with its input and output unredirected, which makes the launch a blocking call
/// that owns the console until the agent is gone.
///
/// THE LAUNCH TAKES THE PACK PATH AND NOTHING ELSE. There is no cancellation token, because the child
/// owns the terminal: an interrupt reaches the AGENT, not us, and comes back as
/// <see cref="AgentTerminationOutcome.CancelledByUser"/>. There is no timeout budget either; the
/// timeout is a property of the launcher, never of the caller.
///
/// NO REPOSITORY-DERIVED TYPE REACHES EITHER OPERATION. <see cref="PromptPackFilePath"/> can only be
/// produced from the documentation destination and a completed write, and every other type in these
/// signatures is a closed enumeration or a status of integers. The pack's CONTENT is restricted by
/// <see cref="PromptPackFindingEntry"/>.
/// </summary>
public interface IExternalAgentLauncher
{
	/// <summary>
	/// Whether an agent can be launched, resolved from the search path and the platform's
	/// documented wrapper locations only - the current directory is excluded from that set,
	/// because a session run from inside the audited repository would otherwise let the repository
	/// decide which executable gets started.
	/// </summary>
	AgentAvailability CheckAvailability();

	/// <summary>
	/// Launches the agent against a prompt pack that has already been written, and returns only
	/// once it is gone.
	/// </summary>
	/// <param name="promptPackPath">
	/// The pack file this session wrote. It is the whole of what crosses the boundary.
	/// </param>
	AgentTerminationStatus Launch(PromptPackFilePath promptPackPath);
}
