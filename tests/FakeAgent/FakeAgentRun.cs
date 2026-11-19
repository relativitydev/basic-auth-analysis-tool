namespace MigrateAuth.FakeAgent;

/// <summary>
/// One run of the fake: write the record, then end the way the behavior says. It reads no standard
/// input, so a launcher that leaves the fake's input open cannot make it block.
/// </summary>
internal static class FakeAgentRun
{
	private const int _INTERRUPT_EXIT_CODE_LINUX = 130;
	private const int _INTERRUPT_EXIT_CODE_WINDOWS = unchecked((int)0xC000013A);
	private const int _SIGNAL_EXIT_BASE = 128;
	private const int _SIGNAL_UNAVAILABLE_EXIT_CODE = 98;
	private const int _SIGNAL_GRACE_SECONDS = 30;
	private const int _MILLISECONDS_PER_SECOND = 1000;

	internal static int Execute(string executablePath, string[] arguments)
	{
		FakeAgentBehavior behavior = FakeAgentBehavior.ReadFor(executablePath);
		string recordPath = string.IsNullOrEmpty(behavior.RecordPath)
			? $"{executablePath}{FakeAgentRecord.FILE_SUFFIX}"
			: behavior.RecordPath;
		FakeAgentRecord record = Observe(behavior, arguments);

		record.WriteTo(recordPath);

		return End(behavior, record, recordPath);
	}

	private static FakeAgentRecord Observe(FakeAgentBehavior behavior, string[] arguments)
	{
		Dictionary<string, string?> watched = [];

		foreach (string name in behavior.WatchedEnvironmentNames)
		{
			watched[name] = Environment.GetEnvironmentVariable(name);
		}

		return new FakeAgentRecord
		{
			Arguments = arguments,
			WorkingDirectory = Directory.GetCurrentDirectory(),
			PackPath = behavior.PackPath,
			PackExists = behavior.PackPath.Length > 0 && (File.Exists(behavior.PackPath) || Directory.Exists(behavior.PackPath)),
			WatchedEnvironment = watched,
			EnvironmentNames = Environment.GetEnvironmentVariables().Keys.Cast<string>().Order(StringComparer.Ordinal).ToArray(),
			ProcessId = Environment.ProcessId,
		};
	}

	private static int End(FakeAgentBehavior behavior, FakeAgentRecord record, string recordPath)
	{
		int exitCode;

		switch (behavior.Ending)
		{
			case FakeAgentEnding.Interrupt:
				exitCode = OperatingSystem.IsWindows() ? _INTERRUPT_EXIT_CODE_WINDOWS : _INTERRUPT_EXIT_CODE_LINUX;
				break;
			case FakeAgentEnding.Signal:
				exitCode = EndBySignal(behavior);
				break;
			case FakeAgentEnding.Hang:
				exitCode = Hang(behavior, record, recordPath);
				break;
			default:
				exitCode = behavior.ExitCode;
				break;
		}

		return exitCode;
	}

	private static int EndBySignal(FakeAgentBehavior behavior)
	{
		int exitCode = _SIGNAL_UNAVAILABLE_EXIT_CODE;

		if (!OperatingSystem.IsWindows() && PosixSignal.TryRaiseOnSelf(behavior.SignalNumber))
		{
			// The signal normally ends the process before this returns; if it was caught or
			// ignored, the process still ends itself, with the shell-style code of that signal.
			Thread.Sleep(_SIGNAL_GRACE_SECONDS * _MILLISECONDS_PER_SECOND);
			exitCode = _SIGNAL_EXIT_BASE + behavior.SignalNumber;
		}

		return exitCode;
	}

	private static int Hang(FakeAgentBehavior behavior, FakeAgentRecord record, string recordPath)
	{
		Thread.Sleep(behavior.HangLifetimeSeconds * _MILLISECONDS_PER_SECOND);
		(record with { EndedByLifetime = true }).WriteTo(recordPath);

		return behavior.ExitCode;
	}
}
