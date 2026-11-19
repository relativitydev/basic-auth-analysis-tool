namespace MigrateAuth.Cli.Agent;

/// <summary>The console seam on a platform that has no console mode to guard: every member reports that it did nothing.</summary>
public sealed class NoNativeConsole : INativeConsole
{
	/// <inheritdoc />
	public bool TryGetMode(NativeConsoleStream stream, out uint mode)
	{
		mode = 0;

		return false;
	}

	/// <inheritdoc />
	public bool TrySetMode(NativeConsoleStream stream, uint mode)
	{
		return false;
	}

	/// <inheritdoc />
	public bool TryFlushInput()
	{
		return false;
	}
}
