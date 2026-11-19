namespace MigrateAuth.FakeAgent;

/// <summary>
/// THE FAKE AGENT. One program that stands in for the three external agents. A file named
/// <c>&lt;this executable&gt;.sentinel</c> beside it makes it a sentinel (see <see cref="SentinelMode"/>);
/// without that file it behaves as <c>&lt;this executable&gt;.behavior</c> says (see <see cref="FakeAgentRun"/>).
/// It reads no standard input. One more mode exists for a single Windows test: given the interrupt-driver
/// argument and a request file, it is the helper that owns its own console (see <see cref="OwnConsoleDriver"/>);
/// no launcher ever passes that argument.
/// </summary>
internal static class Program
{
	private const int _DRIVER_ARGUMENT_COUNT = 2;

	private static int Main(string[] arguments)
	{
		string executablePath = Environment.ProcessPath ?? string.Empty;
		int exitCode;

		if (File.Exists($"{executablePath}{SentinelMode.MARKER_SUFFIX}"))
		{
			exitCode = SentinelMode.Run(executablePath, arguments);
		}
		else if (arguments.Length == _DRIVER_ARGUMENT_COUNT && arguments[0] == OwnConsoleDriver.MODE_ARGUMENT)
		{
			exitCode = OwnConsoleDriver.Run(arguments);
		}
		else
		{
			exitCode = FakeAgentRun.Execute(executablePath, arguments);
		}

		return exitCode;
	}
}
