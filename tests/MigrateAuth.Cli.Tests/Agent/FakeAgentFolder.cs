using System.Diagnostics;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// A temporary folder of fake agents. Each fake is a copy of the self-contained published fake (the
/// one <c>tests/FakeAgent/make-sentinels.py</c> builds beside the sentinel folder) under the name a test
/// needs, with its behavior file beside it. The published fake is found from the sentinel folder, which
/// is first on PATH for every agent test command; no agent is ever resolved through the real PATH.
/// </summary>
internal sealed class FakeAgentFolder : IDisposable
{
	private const string _PATH_VARIABLE = "PATH";
	private const string _FAKE_FOLDER_NAME = "fake-agent";
	private const string _FAKE_BASE_NAME = "fake-agent";
	private const string _WINDOWS_EXECUTABLE_SUFFIX = ".exe";
	private const string _WINDOWS_WRAPPER_SUFFIX = ".cmd";
	private const string _CMD_ONLY_TARGET_SUFFIX = "-target";
	private const string _WRAPPER_FORMAT = "@\"%~dp0{0}\" %*\r\n@exit /b %ERRORLEVEL%\r\n";
	private const string _FOLDER_PREFIX = "fake-agents-";
	private const int _SURVIVOR_POLL_ATTEMPTS = 50;
	private const int _SURVIVOR_WAIT_MILLISECONDS = 100;

	internal string FolderPath { get; }

	internal FakeAgentFolder()
	{
		FolderPath = Path.Combine(Path.GetTempPath(), $"{_FOLDER_PREFIX}{Guid.NewGuid():N}");
		Directory.CreateDirectory(FolderPath);
	}

	/// <summary>Installs a fake under <paramref name="name"/> (an <c>.exe</c> on Windows) and returns its path.</summary>
	internal string Install(string name, FakeAgentBehavior behavior)
	{
		string executablePath = Path.Combine(FolderPath, ExecutableName(name));

		File.Copy(PublishedFakePath(), executablePath);
		behavior.WriteFor(executablePath);

		return executablePath;
	}

	/// <summary>
	/// Windows only: installs a fake reachable only through a <c>name.cmd</c> wrapper, which calls a
	/// target executable beside it. Returns the path of the target, whose behavior and record are the fake's.
	/// </summary>
	internal string InstallCmdOnly(string name, FakeAgentBehavior behavior)
	{
		string targetName = $"{name}{_CMD_ONLY_TARGET_SUFFIX}{_WINDOWS_EXECUTABLE_SUFFIX}";
		string targetPath = Path.Combine(FolderPath, targetName);

		File.Copy(PublishedFakePath(), targetPath);
		behavior.WriteFor(targetPath);
		File.WriteAllText(
			Path.Combine(FolderPath, $"{name}{_WINDOWS_WRAPPER_SUFFIX}"),
			string.Format(System.Globalization.CultureInfo.InvariantCulture, _WRAPPER_FORMAT, targetName));

		return targetPath;
	}

	/// <summary>Starts the process the way a launcher would: no shell, the given arguments, an environment a test can adjust.</summary>
	internal static Process Start(string fileName, IReadOnlyList<string> arguments, Action<IDictionary<string, string?>>? adjustEnvironment, bool redirectInput, string? workingDirectory = null)
	{
		ProcessStartInfo startInfo = new(fileName)
		{
			UseShellExecute = false,
			RedirectStandardInput = redirectInput,
			WorkingDirectory = workingDirectory ?? string.Empty,
		};

		foreach (string argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		adjustEnvironment?.Invoke(startInfo.Environment);

		return Process.Start(startInfo) ?? throw new InvalidOperationException($"The fake agent did not start: {fileName}");
	}

	/// <summary>The name of an executable on this platform.</summary>
	internal static string ExecutableName(string name)
	{
		return OperatingSystem.IsWindows() ? $"{name}{_WINDOWS_EXECUTABLE_SUFFIX}" : name;
	}

	/// <summary>The self-contained published fake, found beside the sentinel folder that is first on PATH.</summary>
	internal static string PublishedFakePath()
	{
		string path = Environment.GetEnvironmentVariable(_PATH_VARIABLE) ?? string.Empty;
		string firstEntry = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
		string sentinelFolder = Path.TrimEndingDirectorySeparator(firstEntry);
		string outputFolder = Path.GetDirectoryName(sentinelFolder) ?? string.Empty;
		string fakePath = Path.Combine(outputFolder, _FAKE_FOLDER_NAME, ExecutableName(_FAKE_BASE_NAME));

		Assert.True(File.Exists(fakePath), $"The published fake agent is not beside the sentinel folder that must be first on PATH: {fakePath}");

		return fakePath;
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(FolderPath, recursive: true);
		}
		catch (IOException)
		{
			// A fake still running holds its file; the folder is a temporary one and is left behind.
		}
		catch (UnauthorizedAccessException)
		{
			// Same: nothing is killed or forced from here.
		}
	}

	/// <summary>
	/// Whether the process is still running after a short wait, which is how a test proves that a fake did not
	/// outlive the end of the launch that started it.
	/// </summary>
	internal static bool IsRunning(int processId)
	{
		bool running = true;

		for (int attempt = 0; attempt < _SURVIVOR_POLL_ATTEMPTS && running; attempt++)
		{
			try
			{
				using Process survivor = Process.GetProcessById(processId);

				running = !survivor.WaitForExit(_SURVIVOR_WAIT_MILLISECONDS);
			}
			catch (ArgumentException)
			{
				running = false;
			}
		}

		return running;
	}

	/// <summary>
	/// Whether a process started from <paramref name="executablePath"/> is still running after a short wait. Unlike
	/// <see cref="IsRunning"/>, it needs no record from the fake, so it also holds when a launch ended before the fake
	/// had started and written one. A process is matched by its name and by the last two segments of its image path
	/// (the fake's own folder and file name), which are the same whether the temporary folder is spelt in its long
	/// or its short form.
	/// </summary>
	internal static bool IsAnyRunningFrom(string executablePath)
	{
		string processName = Path.GetFileNameWithoutExtension(executablePath);
		string imageSuffix = Path.Combine(Path.GetFileName(Path.GetDirectoryName(executablePath)) ?? string.Empty, Path.GetFileName(executablePath));
		bool running = AnyProcessRunsFrom(processName, imageSuffix);

		for (int attempt = 1; attempt < _SURVIVOR_POLL_ATTEMPTS && running; attempt++)
		{
			Thread.Sleep(_SURVIVOR_WAIT_MILLISECONDS);
			running = AnyProcessRunsFrom(processName, imageSuffix);
		}

		return running;
	}

	private static bool AnyProcessRunsFrom(string processName, string imageSuffix)
	{
		bool found = false;

		foreach (Process candidate in Process.GetProcessesByName(processName))
		{
			using (candidate)
			{
				found |= RunsFrom(candidate, imageSuffix);
			}
		}

		return found;
	}

	private static bool RunsFrom(Process candidate, string imageSuffix)
	{
		bool runsFrom = false;

		try
		{
			string imagePath = candidate.MainModule?.FileName ?? string.Empty;

			runsFrom = !candidate.HasExited && imagePath.EndsWith(imageSuffix, StringComparison.OrdinalIgnoreCase);
		}
		catch (System.ComponentModel.Win32Exception)
		{
			// The process ended, or belongs to another user, while it was being read; it is not one of this test's fakes.
		}
		catch (InvalidOperationException)
		{
			// The process ended between the listing and the read.
		}

		return runsFrom;
	}
}
