using System.Diagnostics;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The fake agent: it records its full argument vector, working directory, whether
/// the pack exists and the environment variables added or removed; it reads no standard input; it exits with
/// a chosen code, ends by a chosen signal (Linux), simulates an interrupt, or hangs and ends itself; and it
/// starts under a replaced PATH without DOTNET_ROOT, because it is self-contained. Every run here is a fake
/// copied into the test's own temporary folder and started by full path.
/// </summary>
public sealed class FakeAgentTests
{
	private const string _AGENT_NAME = "claude";
	private const int _PROCESS_SAFETY_MILLISECONDS = 60000;
	private const int _INTERRUPT_EXIT_CODE_LINUX = 130;
	private const int _INTERRUPT_EXIT_CODE_WINDOWS = unchecked((int)0xC000013A);
	private const int _KILL_SIGNAL = 9;
	private const int _SIGNAL_EXIT_BASE = 128;
	private const int _HANG_LIFETIME_SECONDS = 1;
	private const int _HANG_EXIT_CODE = 7;
	private const string _ADDED_VARIABLE = "FAKE_AGENT_TEST_ADDED";
	private const string _ADDED_VALUE = "added value";
	private const string _REMOVED_VARIABLE = "FAKE_AGENT_TEST_REMOVED";
	private const string _DOTNET_ROOT_VARIABLE = "DOTNET_ROOT";
	private const string _PATH_VARIABLE = "PATH";
	private const int _WRAPPER_EXIT_CODE = 9;
	private const string _WRAPPER_SUFFIX = ".cmd";
	private const string _WINDOWS_ONLY_ARGUMENT = "hello";

	[Theory]
	[InlineData(0)]
	[InlineData(3)]
	[InlineData(42)]
	public void ItExitsWithTheChosenCode(int exitCode)
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior { ExitCode = exitCode });

		Assert.Equal(exitCode, RunToEnd(executable, [], null));
	}

	[Fact]
	public void ItRecordsTheFullArgumentVectorExactly()
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior());
		string[] arguments = ["-i", "a sentence with spaces and `code`", "", "trailing"];

		RunToEnd(executable, arguments, null);

		Assert.Equal(arguments, FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}").Arguments);
	}

	[Fact]
	public void ItRecordsTheWorkingDirectory()
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior());
		string workingDirectory = Directory.CreateDirectory(Path.Combine(folder.FolderPath, "work here")).FullName;

		RunToEnd(executable, [], null, workingDirectory);

		Assert.Equal(workingDirectory, FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}").WorkingDirectory);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ItRecordsWhetherThePackExists(bool packExists)
	{
		using FakeAgentFolder folder = new();
		string packPath = Path.Combine(folder.FolderPath, "pack with spaces.md");
		string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior { PackPath = packPath });

		if (packExists)
		{
			File.WriteAllText(packPath, "pack");
		}

		RunToEnd(executable, [], null);

		FakeAgentRecord record = FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}");

		Assert.Equal(packPath, record.PackPath);
		Assert.Equal(packExists, record.PackExists);
	}

	[Fact]
	public void ItRecordsEnvironmentVariablesAddedAndRemoved()
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(
			_AGENT_NAME,
			new FakeAgentBehavior { WatchedEnvironmentNames = [_ADDED_VARIABLE, _REMOVED_VARIABLE] });

		RunToEnd(
			executable,
			[],
			environment =>
			{
				environment[_ADDED_VARIABLE] = _ADDED_VALUE;
				environment.Remove(_REMOVED_VARIABLE);
			});

		FakeAgentRecord record = FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}");

		Assert.Equal(_ADDED_VALUE, record.WatchedEnvironment[_ADDED_VARIABLE]);
		Assert.Null(record.WatchedEnvironment[_REMOVED_VARIABLE]);
		Assert.Contains(_ADDED_VARIABLE, record.EnvironmentNames);
		Assert.DoesNotContain(_REMOVED_VARIABLE, record.EnvironmentNames);
	}

	[Fact]
	public void ItReadsNoStandardInput()
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior { ExitCode = 5 });

		// The input is redirected and never written to nor closed: a fake that read it would block.
		Assert.Equal(5, RunToEnd(executable, [], null, redirectInput: true));
	}

	[Fact]
	public void ItStartsWithPathReplacedAndWithoutDotnetRoot()
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior { ExitCode = 11 });

		int exitCode = RunToEnd(
			executable,
			[],
			environment =>
			{
				environment[_PATH_VARIABLE] = folder.FolderPath;
				environment.Remove(_DOTNET_ROOT_VARIABLE);
			});

		Assert.Equal(11, exitCode);
	}

	[Fact]
	public void ASimulatedInterruptEndsTheWayAKeyboardInterruptDoesOnThisPlatform()
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior { Ending = FakeAgentEnding.Interrupt });
		int expected = OperatingSystem.IsWindows() ? _INTERRUPT_EXIT_CODE_WINDOWS : _INTERRUPT_EXIT_CODE_LINUX;

		Assert.Equal(expected, RunToEnd(executable, [], null));
	}

	[Fact]
	public void ItEndsByTheChosenSignalOnLinux()
	{
		if (!OperatingSystem.IsWindows())
		{
			using FakeAgentFolder folder = new();
			string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior { Ending = FakeAgentEnding.Signal, SignalNumber = _KILL_SIGNAL });

			Assert.Equal(_SIGNAL_EXIT_BASE + _KILL_SIGNAL, RunToEnd(executable, [], null));
		}
	}

	[Fact]
	public void AHangEndsItselfAtItsMaximumLifetime()
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(
			_AGENT_NAME,
			new FakeAgentBehavior { Ending = FakeAgentEnding.Hang, HangLifetimeSeconds = _HANG_LIFETIME_SECONDS, ExitCode = _HANG_EXIT_CODE });

		int exitCode = RunToEnd(executable, [], null);

		Assert.Equal(_HANG_EXIT_CODE, exitCode);
		Assert.True(FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}").EndedByLifetime);
	}

	[Fact]
	public void ACmdWrapperStartsTheFakeOnWindows()
	{
		if (OperatingSystem.IsWindows())
		{
			using FakeAgentFolder folder = new();
			string target = folder.InstallCmdOnly(_AGENT_NAME, new FakeAgentBehavior { ExitCode = _WRAPPER_EXIT_CODE });
			string wrapper = Path.Combine(folder.FolderPath, $"{_AGENT_NAME}{_WRAPPER_SUFFIX}");

			int exitCode = RunToEnd(wrapper, [_WINDOWS_ONLY_ARGUMENT], null);

			Assert.Equal(_WRAPPER_EXIT_CODE, exitCode);
			Assert.Equal([_WINDOWS_ONLY_ARGUMENT], FakeAgentRecord.ReadFrom($"{target}{FakeAgentRecord.FILE_SUFFIX}").Arguments);
		}
	}

	[Fact]
	public void AFakeWithoutTheSentinelMarkerWritesNoSentinelFlag()
	{
		using FakeAgentFolder folder = new();
		string executable = folder.Install(_AGENT_NAME, new FakeAgentBehavior());

		RunToEnd(executable, [], null);

		Assert.False(File.Exists($"{executable}{SentinelMode.FLAG_SUFFIX}"));
	}

	private static int RunToEnd(string executable, IReadOnlyList<string> arguments, Action<IDictionary<string, string?>>? adjustEnvironment, string? workingDirectory = null, bool redirectInput = false)
	{
		using Process process = FakeAgentFolder.Start(executable, arguments, adjustEnvironment, redirectInput, workingDirectory);
		int exitCode;

		if (process.WaitForExit(_PROCESS_SAFETY_MILLISECONDS))
		{
			exitCode = process.ExitCode;
		}
		else
		{
			process.Kill(entireProcessTree: true);
			throw new TimeoutException($"The fake agent did not end by itself: {executable}");
		}

		return exitCode;
	}
}
