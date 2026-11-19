namespace MigrateAuth.Cli.Agent;

/// <summary>
/// Whether an external agent can be launched at all.
///
/// An agent that cannot be launched is not available: on one of the supported platforms the agents
/// install script wrappers, and checking availability and knowing how to launch are the same problem.
/// The third value exists only to let the console say WHY nothing was found: the one match there was lay
/// inside the repository being assessed, and a repository never decides which program starts. It is still
/// a not-found answer.
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
	Available = 1,

	/// <summary>
	/// Nothing outside the repository is an agent this launcher can start, and a command of that name
	/// inside the repository was seen and ignored.
	/// </summary>
	NotFoundMatchInsideRepositoryIgnored = 2
}

/// <summary>
/// Whether a resolved agent may be OFFERED to the user for launch, and when not, the reason, so that a
/// fixed line can say why. Asked before the offer, on the pack that was written, and answered from the
/// file system and the text of paths only: nothing is started to answer it.
/// </summary>
public enum AgentLaunchEligibility
{
	/// <summary>The launch can be offered.</summary>
	Launchable = 0,

	/// <summary>
	/// The command line the Windows command interpreter would receive holds a character outside the
	/// allow-list, or is longer than the limit.
	/// </summary>
	UnsafeCommandLine = 1,

	/// <summary>The path of the launch copy contains a control character.</summary>
	PathContainsControlCharacter = 2,

	/// <summary>
	/// The agent is a Windows script wrapper and the repository is not on a local drive letter within the
	/// command interpreter's limits, so the interpreter would start the agent in the wrong folder.
	/// </summary>
	WrapperNeedsLocalRepository = 3
}

/// <summary>
/// THE ISOLATION BOUNDARY FOR THE EXTERNAL AGENT, AS THE CONTRACT AND NOTHING ELSE.
///
/// Every argument, every wrapper quirk and every platform difference lives behind an implementation
/// of this interface, so changes in an external tool's command-line interface stay contained, and the
/// session above it knows only whether an agent is available and how the run ended.
///
/// THREE IMPLEMENTATIONS SELECTED BY ONE STATIC SWITCH. Every implementation is statically referenced and
/// selected by an exhaustive switch over the environment enumeration; no type is created reflectively,
/// which breaks under ahead-of-time compilation.
///
/// FOUR MEMBERS FOR THE FIVE OPERATIONS - check availability, check whether the launch can be offered,
/// describe the resolution, launch with a prompt pack path, and return termination status. The last is the
/// launch's answer rather than a separate call: the child INHERITS THE TERMINAL with its input and output
/// unredirected, which makes the launch a blocking call that owns the console until the agent is gone.
///
/// THE LAUNCH TAKES THE PACK PATH AND NOTHING ELSE. There is no cancellation token, because the child
/// owns the terminal: an interrupt reaches the AGENT, not us, and comes back as
/// <see cref="AgentTerminationOutcome.CancelledByUser"/>. There is no timeout budget either; the
/// timeout is a property of the launcher, never of the caller.
///
/// NO REPOSITORY-DERIVED TYPE REACHES ANY OPERATION. <see cref="PromptPackFilePath"/> can only be
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
	/// Whether the launch can be offered for the pack that was written, decided before the offer on the
	/// path the launch copy WILL have. It takes the written pack, so that no text and no
	/// repository-derived type enters the contract. Meaningful only after <see cref="CheckAvailability"/>
	/// answered <see cref="AgentAvailability.Available"/>; asked of a launcher that found nothing, it is a
	/// programming error.
	/// </summary>
	/// <exception cref="InvalidOperationException">No agent was resolved.</exception>
	AgentLaunchEligibility CheckLaunchable(PromptPackFilePath writtenPack);

	/// <summary>
	/// What the console may say about the resolution this launcher made: the file name, how it would be started and
	/// the position in the search path. It is the same resolution the launch uses, made once, and it carries no
	/// absolute path. Meaningful only after <see cref="CheckAvailability"/> answered
	/// <see cref="AgentAvailability.Available"/>.
	/// </summary>
	/// <exception cref="InvalidOperationException">No agent was resolved.</exception>
	AgentResolutionDescription DescribeResolution();

	/// <summary>
	/// Launches the agent against a prompt pack that has already been written, and returns only
	/// once it is gone.
	/// </summary>
	/// <param name="promptPackPath">
	/// The pack file this session wrote. It is the whole of what crosses the boundary.
	/// </param>
	AgentTerminationStatus Launch(PromptPackFilePath promptPackPath);
}
