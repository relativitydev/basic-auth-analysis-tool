namespace MigrateAuth.Cli.Agent;

/// <summary>Which of the console's two standard streams a mode belongs to.</summary>
public enum NativeConsoleStream
{
	/// <summary>The standard input of the console.</summary>
	Input = 0,

	/// <summary>The standard output of the console.</summary>
	Output = 1
}

/// <summary>
/// THE ONE SEAM FOR WHAT ONLY THE WINDOWS CONSOLE CAN DO: reading and setting a stream's mode, and throwing
/// away input the user typed ahead. A child process may change the console mode and leave it changed, and
/// anything typed while it ran belongs to it, not to the next question. Every member answers whether it could
/// act, because a process without a console (redirected streams, a service) has nothing to save or restore.
/// </summary>
public interface INativeConsole
{
	/// <summary>Reads the mode of <paramref name="stream"/>. False when there is no console to read it from.</summary>
	bool TryGetMode(NativeConsoleStream stream, out uint mode);

	/// <summary>Sets the mode of <paramref name="stream"/>. False when there is no console or the mode was refused.</summary>
	bool TrySetMode(NativeConsoleStream stream, uint mode);

	/// <summary>Discards the input typed and not yet read. False when there is no console.</summary>
	bool TryFlushInput();
}
