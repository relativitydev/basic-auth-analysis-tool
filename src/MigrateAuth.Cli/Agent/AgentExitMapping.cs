namespace MigrateAuth.Cli.Agent;

/// <summary>
/// HOW THE CODE THE OPERATING SYSTEM REPORTED BECOMES A TERMINATION STATUS. Pure: the platform is an argument,
/// so both tables are exercised on every machine.
///
/// Windows: the status the system gives a process ended by the keyboard interrupt is a cancellation by the
/// user; every other code is the agent's own. Through the command interpreter the code is the interpreter's,
/// which is the answer after its own prompt about ending a batch job, and the table says so.
///
/// Linux: 130 is the shell's code for an interrupt and is a cancellation; 129 to 192 other than 130 is a
/// signal, numbered by the code minus 128; every other code is the agent's own. A program that exits by itself
/// with a code in that range is misread as a signal. That is a declared limit of the heuristic, because the
/// interactive exit codes of the agents are unverified.
///
/// There is no product timeout. The only way to get a timeout status is a launcher built with a test budget.
/// </summary>
public static class AgentExitMapping
{
	/// <summary>The status Windows reports for a process ended by a keyboard interrupt.</summary>
	public const int WINDOWS_CONTROL_C_EXIT_CODE = unchecked((int)0xC000013A);

	/// <summary>The exit code a shell reports for a process ended by the keyboard interrupt.</summary>
	public const int LINUX_INTERRUPT_EXIT_CODE = 130;

	/// <summary>The first exit code that a shell uses to report a signal.</summary>
	public const int LINUX_FIRST_SIGNAL_EXIT_CODE = 129;

	/// <summary>The last exit code that a shell uses to report a signal.</summary>
	public const int LINUX_LAST_SIGNAL_EXIT_CODE = 192;

	/// <summary>The number a shell adds to a signal's number to build its exit code.</summary>
	public const int LINUX_SIGNAL_EXIT_BASE = 128;

	/// <summary>Maps the code of a process that ended on its own, on the Windows or the Linux table.</summary>
	public static AgentTerminationStatus Map(int exitCode, bool isWindows)
	{
		return isWindows ? MapWindows(exitCode) : MapLinux(exitCode);
	}

	private static AgentTerminationStatus MapWindows(int exitCode)
	{
		return exitCode == WINDOWS_CONTROL_C_EXIT_CODE
			? AgentTerminationStatus.CancelledByUser()
			: AgentTerminationStatus.ExitedWithCode(exitCode);
	}

	private static AgentTerminationStatus MapLinux(int exitCode)
	{
		AgentTerminationStatus status;

		if (exitCode == LINUX_INTERRUPT_EXIT_CODE)
		{
			status = AgentTerminationStatus.CancelledByUser();
		}
		else if (exitCode >= LINUX_FIRST_SIGNAL_EXIT_CODE && exitCode <= LINUX_LAST_SIGNAL_EXIT_CODE)
		{
			status = AgentTerminationStatus.TerminatedBySignal(exitCode - LINUX_SIGNAL_EXIT_BASE);
		}
		else
		{
			status = AgentTerminationStatus.ExitedWithCode(exitCode);
		}

		return status;
	}
}
