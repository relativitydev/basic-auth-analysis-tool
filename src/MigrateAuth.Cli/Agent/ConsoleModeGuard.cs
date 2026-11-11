namespace MigrateAuth.Cli.Agent;

/// <summary>
/// KEEPS THE CONSOLE AS IT WAS ACROSS A LAUNCH. The modes of the standard input and output are saved before the
/// launch and put back after it, also when the launch throws, and the input typed while the child ran is
/// discarded once the child is gone. Streams that have no console are skipped: nothing is saved for them and
/// nothing is restored. The same seam also answers a flush on demand, for the confirmation read that comes
/// before a launch.
/// </summary>
public sealed class ConsoleModeGuard
{
	private static readonly NativeConsoleStream[] _guardedStreams = [NativeConsoleStream.Input, NativeConsoleStream.Output];

	private readonly INativeConsole _console;

	/// <summary>Creates a guard over <paramref name="console"/>.</summary>
	public ConsoleModeGuard(INativeConsole console)
	{
		ArgumentNullException.ThrowIfNull(console);

		_console = console;
	}

	/// <summary>The guard for this platform: the Windows console where there is one, and one that does nothing elsewhere.</summary>
	public static ConsoleModeGuard ForThisPlatform()
	{
		INativeConsole console = new NoNativeConsole();

		if (OperatingSystem.IsWindows())
		{
			console = new WindowsNativeConsole();
		}

		return new ConsoleModeGuard(console);
	}

	/// <summary>Discards the input typed ahead and not yet read.</summary>
	public void FlushPendingInput()
	{
		_console.TryFlushInput();
	}

	/// <summary>Runs <paramref name="launch"/> with the console modes saved before and restored after.</summary>
	public TResult Run<TResult>(Func<TResult> launch)
	{
		ArgumentNullException.ThrowIfNull(launch);

		Dictionary<NativeConsoleStream, uint> saved = Save();
		TResult result;

		try
		{
			result = launch();
		}
		finally
		{
			Restore(saved);
			FlushPendingInput();
		}

		return result;
	}

	private Dictionary<NativeConsoleStream, uint> Save()
	{
		Dictionary<NativeConsoleStream, uint> saved = [];

		foreach (NativeConsoleStream stream in _guardedStreams)
		{
			if (_console.TryGetMode(stream, out uint mode))
			{
				saved[stream] = mode;
			}
		}

		return saved;
	}

	private void Restore(Dictionary<NativeConsoleStream, uint> saved)
	{
		foreach (KeyValuePair<NativeConsoleStream, uint> entry in saved)
		{
			_console.TrySetMode(entry.Key, entry.Value);
		}
	}
}
