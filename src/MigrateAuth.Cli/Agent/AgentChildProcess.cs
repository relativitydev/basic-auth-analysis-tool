using System.ComponentModel;
using System.Diagnostics;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// What running the child produced, before it is turned into a termination status: whether it started at
/// all, whether a test budget ended it, and the code the operating system reported.
/// </summary>
public readonly record struct AgentChildResult
{
	/// <summary>The result of a child that could not be started.</summary>
	public static AgentChildResult NotStarted { get; } = new() { Started = false };

	/// <summary>Whether a process was started.</summary>
	public bool Started { get; init; }

	/// <summary>Whether the test budget ran out and the process tree was killed.</summary>
	public bool TimedOut { get; init; }

	/// <summary>The exit code the operating system reported. Meaningful only when it started and did not time out.</summary>
	public int ExitCode { get; init; }
}

/// <summary>
/// BUILDS AND RUNS THE ONE CHILD PROCESS OF A LAUNCH. This is the only place in the product that starts a
/// process.
///
/// A direct launch starts the resolved program by its absolute path with the arguments in an argument list,
/// never through a shell. A script wrapper starts the Windows command interpreter, by absolute path from the
/// system folder, with <c>/d /s /c</c> and one constructed command line (see
/// <see cref="AgentCommandLineSafety"/>). Both start in the repository's canonical path, with the environment
/// of <see cref="AgentChildEnvironment"/>, and with nothing redirected: the child inherits the terminal.
///
/// Waiting is unbounded unless a test supplies a budget, in which case the whole process tree is killed when
/// it runs out. A failure to start is a result, not an exception.
/// </summary>
public static class AgentChildProcess
{
	private const string _COMMAND_INTERPRETER_FILE_NAME = "cmd.exe";

	/// <summary>Builds the start information for the resolved command and its arguments.</summary>
	public static ProcessStartInfo CreateStartInfo(
		AgentResolution resolution,
		IReadOnlyList<string> arguments,
		string repositoryCanonicalPath,
		IReadOnlyList<string> searchPathEntries)
	{
		ArgumentNullException.ThrowIfNull(resolution);
		ArgumentNullException.ThrowIfNull(arguments);
		ArgumentException.ThrowIfNullOrWhiteSpace(repositoryCanonicalPath);

		ProcessStartInfo startInfo = new()
		{
			UseShellExecute = false,
			WorkingDirectory = repositoryCanonicalPath
		};

		if (resolution.IsWrapperScript)
		{
			startInfo.FileName = Path.Combine(Environment.SystemDirectory, _COMMAND_INTERPRETER_FILE_NAME);
			startInfo.Arguments = AgentCommandLineSafety.BuildWrapperCommandLine(resolution.ExecutablePath, arguments);
		}
		else
		{
			startInfo.FileName = resolution.ExecutablePath;

			foreach (string argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}
		}

		AgentChildEnvironment.Apply(startInfo, searchPathEntries, repositoryCanonicalPath);

		return startInfo;
	}

	/// <summary>Starts the child and waits for it, with a budget only when a test supplied one.</summary>
	public static AgentChildResult Run(ProcessStartInfo startInfo, TimeSpan? testBudget)
	{
		ArgumentNullException.ThrowIfNull(startInfo);

		AgentChildResult result;

		try
		{
			using Process? process = Process.Start(startInfo);

			result = process is null ? AgentChildResult.NotStarted : WaitFor(process, testBudget);
		}
		catch (Win32Exception)
		{
			result = AgentChildResult.NotStarted;
		}
		catch (InvalidOperationException)
		{
			result = AgentChildResult.NotStarted;
		}

		return result;
	}

	private static AgentChildResult WaitFor(Process process, TimeSpan? testBudget)
	{
		bool timedOut = false;

		if (testBudget is null)
		{
			process.WaitForExit();
		}
		else if (!process.WaitForExit(testBudget.Value))
		{
			timedOut = true;
			KillTree(process);
		}

		return new AgentChildResult
		{
			Started = true,
			TimedOut = timedOut,
			ExitCode = timedOut ? 0 : process.ExitCode
		};
	}

	private static void KillTree(Process process)
	{
		try
		{
			process.Kill(entireProcessTree: true);
		}
		catch (InvalidOperationException)
		{
			// The process ended between the wait and the kill; there is nothing left to stop.
		}
		catch (Win32Exception)
		{
			// Part of the tree could not be stopped; the wait below reports whether the process itself is gone.
		}

		process.WaitForExit();
	}
}
