namespace MigrateAuth.Cli.Agent;

/// <summary>
/// Whether an external agent can be launched at all.
///
/// TWO VALUES AND NOT THREE, because on one of the supported platforms
/// "checking availability and knowing how to launch are the same problem" - the agents install
/// script wrappers there which the direct process launch mechanism cannot execute. A third value
/// meaning "present but not launchable" would therefore be a state the implementation has to
/// resolve before it can answer, not one it can report. An agent that cannot be launched is not
/// available.
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
/// THE ISOLATION BOUNDARY FOR EXTERNAL AGENTS, AS THE CONTRACT AND NOTHING ELSE.
///
/// Its purpose is that changes in an external tool's command-line interface stay contained.
/// Containment means one seam: every argument, every wrapper quirk and every platform difference
/// lives behind an implementation of this interface, and the session above it knows only that an
/// agent is available or not, and how the run ended.
///
/// THIS IS THE CONTRACT AND NOT AN IMPLEMENTATION OF IT. There is no static switch over an
/// environment enumeration here and no implementation for it to select. The shape of that switch is
/// fixed: every implementation statically referenced, with no reflective type creation, which breaks
/// under ahead-of-time compilation.
///
/// TWO MEMBERS FOR THREE OPERATIONS. The three are "check availability, launch with a prompt pack
/// path, return termination status", and the third is the second's answer rather than a separate
/// call: the child INHERITS THE TERMINAL with its input and output unredirected, which makes the
/// launch a blocking call that owns the console until the agent is gone. A separate
/// status-retrieval member would describe a launch that returns while the agent still runs, which
/// is a shape this product has nowhere to put - it runs sequentially, with no parallelism and no
/// concurrency primitives.
///
/// AND THE LAUNCH TAKES THE PACK PATH AND NOTHING ELSE. No cancellation token, because the child
/// owns the terminal: an interrupt reaches the AGENT, not us, and comes back as
/// <see cref="AgentTerminationOutcome.CancelledByUser"/> - a token would model a cancellation we are
/// not in a position to deliver. No timeout budget either; the timeout is a property of the
/// launcher, and a caller-supplied one would put the decision about how long an external tool may
/// hang in the session shell, which is exactly the knowledge this seam exists to contain.
///
/// NO REPOSITORY-DERIVED TYPE REACHES EITHER OPERATION. <see cref="PromptPackFilePath"/> can only be
/// produced from the documentation destination and a completed write, and every other type in these
/// signatures is a closed enumeration or a status of integers. The rule on the pack's CONTENT - built
/// exclusively from the finding record plus static template text, with repository fragments
/// forbidden - is carried by <see cref="PromptPackFindingEntry"/>, so that the two halves of "the
/// pack is the only artifact intended to leave the machine" are each enforced by a signature rather
/// than by a reviewer.
/// </summary>
public interface IExternalAgentLauncher
{
	/// <summary>
	/// Whether an agent can be launched, resolved from the search path and the platform's
	/// documented wrapper locations only - the current directory is excluded from that set, because a session run from inside the audited repository would otherwise let the repository
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
