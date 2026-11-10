using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>What one run of the real binary produced, as a caller outside the process can see it.</summary>
internal sealed record CliProcessOutcome
{
	/// <summary>
	/// The process's own exit code, which is the one thing no in-process composition can produce.
	/// The exit code is last-condition-wins over a whole session, and a session is a process - so a stage
	/// runner composed in a test can report what each stage returned and still say nothing about what a
	/// script calling this tool would observe.
	/// </summary>
	internal required int ExitCode { get; init; }

	/// <summary>Everything the session wrote to standard output, in order.</summary>
	internal required string StandardOutput { get; init; }

	/// <summary>Everything it wrote to standard error, which for a normal session is nothing.</summary>
	internal required string StandardError { get; init; }

	/// <summary>How long the process took, start to exit. Reported, never asserted on.</summary>
	internal required TimeSpan Duration { get; init; }

	/// <summary>
	/// The empty folder the child's <c>USERPROFILE</c> and <c>HOME</c> were pointed at. It is removed
	/// when the run ends; the path is kept so a test can state where the child was aimed.
	/// </summary>
	internal required string IsolatedProfileFolderPath { get; init; }
}

/// <summary>
/// The real entry point, in its own process, with the process's own exit code.
///
/// What runs is <c>dotnet migrate-auth.dll</c>: a separate process, the same <c>Program.Main</c>,
/// and an exit code the host propagates unchanged.
///
/// The framework-dependent apphost (<c>migrate-auth.exe</c>) is not launched: on Windows it can be
/// refused with a side-by-side configuration error before any managed code runs. <c>dotnet run</c> is
/// not used because it re-enters MSBuild from inside a test run and mixes the application's exit code
/// with the driver's own build-failure codes. The Native AOT binary under <c>artifacts/publish</c> is
/// not used because that folder is git-ignored and machine-local.
///
/// This harness therefore exercises the JIT composition, not the published binary, and its launch is
/// written for Windows; the one line below that names a Unix file name is the host's naming rule and
/// not a cross-platform mechanism.
/// </summary>
internal static class CliProcessHarness
{
	/// <summary>The managed entry assembly, copied beside the test assembly by the project reference.</summary>
	internal const string ENTRY_ASSEMBLY_FILE_NAME = "migrate-auth.dll";

	/// <summary>The shared-framework host's name on Windows, which is the platform these tests were run on.</summary>
	internal const string WINDOWS_HOST_FILE_NAME = "dotnet.exe";

	/// <summary>The host's name everywhere else. Present for the path, exercised nowhere.</summary>
	internal const string UNIX_HOST_FILE_NAME = "dotnet";

	/// <summary>The variable the SDK sets to the host that is driving the current build or test run.</summary>
	internal const string HOST_PATH_VARIABLE_NAME = "DOTNET_HOST_PATH";

	/// <summary>The variable Windows and the runtime's profile lookup read for the user's profile folder.</summary>
	internal const string USER_PROFILE_VARIABLE_NAME = "USERPROFILE";

	/// <summary>The variable Unix systems read for the user's home folder.</summary>
	internal const string HOME_VARIABLE_NAME = "HOME";

	/// <summary>
	/// How long a session is given before it is killed and the test fails. It is deliberately
	/// generous, so that a busy machine does not fire it.
	/// </summary>
	internal const int PROCESS_TIMEOUT_IN_SECONDS = 180;

	/// <summary>
	/// How far the shared framework's directory sits below the host executable:
	/// <c>&lt;root&gt;/shared/Microsoft.NETCore.App/&lt;version&gt;</c> is three levels down.
	/// </summary>
	private const int _RUNTIME_DIRECTORY_DEPTH_BELOW_THE_HOST = 3;

	private const string _ENTRY_ASSEMBLY_ABSENT_MESSAGE_FORMAT =
		"The CLI entry assembly was not found beside the test assembly at {0}. The test project references the CLI project, so this means the build did not complete.";

	private const string _HOST_ABSENT_MESSAGE =
		"The shared-framework host could not be located from the environment or from the running runtime directory.";

	private const string _PROCESS_TIMED_OUT_MESSAGE_FORMAT =
		"The session did not exit within {0} seconds and was killed.";

	/// <summary>
	/// The four workflow stages in run order, then <c>exit</c>, taken from the registry rather than
	/// written out - so a renamed command breaks this harness at compile time through the identity
	/// rather than silently feeding an unrecognized token to the prompt.
	///
	/// <c>ai-assist</c> is deliberately absent: it is not shipped in this release, and running it would
	/// not exercise the four-stage pipeline.
	/// </summary>
	internal static ImmutableArray<string> TheFourStagesThenExit =>
	[
		CommandRegistry.Describe(CommandId.Scan).Name,
		CommandRegistry.Describe(CommandId.Analyze).Name,
		CommandRegistry.Describe(CommandId.Suggest).Name,
		CommandRegistry.Describe(CommandId.Report).Name,
		CommandRegistry.Describe(CommandId.Exit).Name
	];

	/// <summary>The assembly under test, resolved beside the test assembly rather than from a path.</summary>
	internal static string EntryAssemblyPath => Path.Combine(AppContext.BaseDirectory, ENTRY_ASSEMBLY_FILE_NAME);

	/// <summary>
	/// The host that will run it. The SDK publishes its own host in an environment variable while a
	/// build or a test run is in progress, and that is the authoritative answer; where it is absent -
	/// a run driven by an IDE's test host, for instance - the host is derived from the runtime
	/// directory this test process is itself executing on, which is the same installation by
	/// construction. Neither route consults the PATH, because the first <c>dotnet</c> on a
	/// developer's PATH is not necessarily the one under test.
	/// </summary>
	internal static string HostPath
	{
		get
		{
			string? declaredHostPath = Environment.GetEnvironmentVariable(HOST_PATH_VARIABLE_NAME);

			return declaredHostPath is not null && File.Exists(declaredHostPath)
				? declaredHostPath
				: DeriveHostFromTheRunningRuntime();
		}
	}

	/// <summary>
	/// Runs one session end to end: both folders by name on the command line, the entered commands
	/// on standard input, and the process's own exit code back.
	/// </summary>
	internal static CliProcessOutcome Run(
		string repositoryFolderPath,
		string destinationFolderPath,
		IEnumerable<string> enteredCommands)
	{
		return RunProcess(BuildStartInfo(repositoryFolderPath, destinationFolderPath), enteredCommands);
	}

	/// <summary>
	/// Runs the process with an arbitrary launch argument list and nothing entered on standard
	/// input, for <c>--help</c>/<c>-h</c>/<c>--version</c>: none of the three opens a prompt, so
	/// there is nothing this caller would ever have a chance to type.
	/// </summary>
	internal static CliProcessOutcome RunWithArguments(IEnumerable<string> launchArguments)
	{
		return RunProcess(BuildStartInfo(launchArguments), []);
	}

	/// <summary>
	/// Runs one session with the child's own console code page forced to UTF-8 (65001) before
	/// it starts, so a byte-order mark sent on standard input decodes the way a Windows PowerShell pipe
	/// on a machine whose active code page is already UTF-8 decodes it: as the single character
	/// <c>SessionConsole</c> has to strip, not as three OEM-code-page glyphs.
	///
	/// A fully redirected child with no console window of its own still resolves
	/// <see cref="Console.InputEncoding"/> from <c>GetConsoleCP</c>, and that call falls back to the
	/// machine's default OEM code page, which decodes a UTF-8 byte-order mark as three separate glyphs
	/// instead of one character. Nothing on <see cref="ProcessStartInfo"/> lets a parent set that for a
	/// child directly: it is a property of the console session the child inherits. Wrapping the launch in
	/// <c>cmd /c chcp 65001 &amp; ...</c> changes it for the child, because the code page becomes the one
	/// the shared console session was just set to. This is therefore the one launch in this file that is
	/// Windows-specific in its mechanism as well as in its scope.
	///
	/// On Linux and macOS the plain launch already is the scenario. Standard input is decoded as
	/// UTF-8 there by default, so the mark already reaches <c>SessionConsole</c> as the single
	/// character U+FEFF, and there is no code page to force and no <c>cmd.exe</c> to wrap the
	/// launch in. The same assertion therefore runs on every platform, each through the launch that
	/// reproduces the already-decoded mark on it.
	/// </summary>
	internal static CliProcessOutcome RunWithUtf8CodePage(
		string repositoryFolderPath,
		string destinationFolderPath,
		IEnumerable<string> enteredCommands)
	{
		ProcessStartInfo startInfo = OperatingSystem.IsWindows()
			? BuildUtf8CodePageStartInfo(repositoryFolderPath, destinationFolderPath)
			: BuildStartInfo(repositoryFolderPath, destinationFolderPath);

		return RunProcess(startInfo, enteredCommands);
	}

	/// <summary>
	/// Points the child's user profile and home folder at a folder the caller supplies, so that no
	/// configuration file on the machine running the tests can change a flow. The lookup honours
	/// these variables on Linux and macOS; on Windows the runtime asks the operating system for the
	/// profile folder instead, and the isolation there is proved in-process, through the resolver
	/// overload that takes the profile folder as an argument.
	/// </summary>
	internal static ProcessStartInfo IsolateProfile(ProcessStartInfo startInfo, string emptyProfileFolderPath)
	{
		startInfo.Environment[USER_PROFILE_VARIABLE_NAME] = emptyProfileFolderPath;
		startInfo.Environment[HOME_VARIABLE_NAME] = emptyProfileFolderPath;

		return startInfo;
	}

	/// <summary>
	/// Standard output and standard error are drained by the event handlers rather than read to the
	/// end after the wait, which is what keeps a chatty session from filling a pipe buffer and
	/// deadlocking against a parent that is not reading yet.
	///
	/// Every process is launched with its profile and home folders pointed at an empty temporary
	/// folder, removed when the run ends.
	/// </summary>
	private static CliProcessOutcome RunProcess(ProcessStartInfo startInfo, IEnumerable<string> enteredCommands)
	{
		RequireTheEntryAssemblyExists();

		using TemporaryFolder emptyProfileFolder = TemporaryFolder.Create();

		IsolateProfile(startInfo, emptyProfileFolder.Path);

		StringBuilder standardOutput = new();
		StringBuilder standardError = new();

		using Process process = new()
		{
			StartInfo = startInfo
		};

		process.OutputDataReceived += (sender, received) => AppendLine(standardOutput, received.Data);
		process.ErrorDataReceived += (sender, received) => AppendLine(standardError, received.Data);

		Stopwatch stopwatch = Stopwatch.StartNew();

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		EnterCommands(process, enteredCommands);
		WaitForTheSessionToEnd(process);

		stopwatch.Stop();

		return new CliProcessOutcome
		{
			ExitCode = process.ExitCode,
			StandardOutput = standardOutput.ToString(),
			StandardError = standardError.ToString(),
			Duration = stopwatch.Elapsed,
			IsolatedProfileFolderPath = emptyProfileFolder.Path
		};
	}

	private static void RequireTheEntryAssemblyExists()
	{
		if (!File.Exists(EntryAssemblyPath))
		{
			throw new FileNotFoundException(
				string.Format(
					CultureInfo.InvariantCulture,
					_ENTRY_ASSEMBLY_ABSENT_MESSAGE_FORMAT,
					EntryAssemblyPath),
				EntryAssemblyPath);
		}
	}

	/// <summary>
	/// The host installation this test process is running on, found from the directory the shared
	/// framework was loaded from: <c>&lt;root&gt;/shared/Microsoft.NETCore.App/&lt;version&gt;</c>,
	/// three levels below the host executable.
	/// </summary>
	private static string DeriveHostFromTheRunningRuntime()
	{
		DirectoryInfo? directory = new FileInfo(typeof(object).Assembly.Location).Directory;

		for (int level = 0; level < _RUNTIME_DIRECTORY_DEPTH_BELOW_THE_HOST && directory is not null; level++)
		{
			directory = directory.Parent;
		}

		string? hostPath = directory is null
			? null
			: Path.Combine(
				directory.FullName,
				OperatingSystem.IsWindows() ? WINDOWS_HOST_FILE_NAME : UNIX_HOST_FILE_NAME);

		if (hostPath is null || !File.Exists(hostPath))
		{
			throw new FileNotFoundException(_HOST_ABSENT_MESSAGE);
		}

		return hostPath;
	}

	/// <summary>
	/// The command line: two named options, no positional form, behind the entry assembly the host is
	/// asked to run.
	/// </summary>
	private static ProcessStartInfo BuildStartInfo(string repositoryFolderPath, string destinationFolderPath)
	{
		return BuildStartInfo(
		[
			CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
			repositoryFolderPath,
			CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION,
			destinationFolderPath
		]);
	}

	/// <summary>
	/// The entry assembly behind the host, followed by whatever launch arguments the caller
	/// supplies. The arguments are added to the list rather than joined into a string, so a value
	/// containing a space is passed as one argument by the platform instead of by a quoting rule
	/// written here.
	///
	/// STANDARD INPUT IS DECLARED UTF-8 WITHOUT A BOM EMITTER, deliberately rather than left to
	/// whatever this test host's own default is - so that a parent write of a known character
	/// always puts the same bytes on the pipe, regardless of this machine's own console code page.
	/// It governs only the PARENT's side of the pipe; see
	/// <see cref="RunWithUtf8CodePage(string, string, IEnumerable{string})"/> for the CHILD's side,
	/// which this alone does not control.
	/// </summary>
	private static ProcessStartInfo BuildStartInfo(IEnumerable<string> launchArguments)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = HostPath,
			WorkingDirectory = AppContext.BaseDirectory,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
		};

		startInfo.ArgumentList.Add(EntryAssemblyPath);

		foreach (string launchArgument in launchArguments)
		{
			startInfo.ArgumentList.Add(launchArgument);
		}

		return startInfo;
	}

	/// <summary>
	/// <c>cmd /c chcp 65001 &amp; ...</c>, with the whole command line built by hand into
	/// <see cref="ProcessStartInfo.Arguments"/> rather than through
	/// <see cref="ProcessStartInfo.ArgumentList"/>. <c>cmd</c>'s <c>/c</c> takes one string and
	/// splits it itself, so the arguments this parent wants the CLI to receive have to be quoted
	/// into that one string rather than handed to the platform as separate tokens - and
	/// <c>ArgumentList</c> quotes each of its own entries again before building the native command
	/// line, which double-escapes a value that already carries the quotes <c>cmd</c> needs. Setting
	/// <c>Arguments</c> directly is what keeps this string exactly what <c>cmd</c> receives.
	/// </summary>
	private static ProcessStartInfo BuildUtf8CodePageStartInfo(string repositoryFolderPath, string destinationFolderPath)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = "cmd.exe",
			WorkingDirectory = AppContext.BaseDirectory,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
		};

		string childCommandLine = string.Join(
			' ',
			Quote(HostPath),
			Quote(EntryAssemblyPath),
			CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
			Quote(repositoryFolderPath),
			CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION,
			Quote(destinationFolderPath));

		startInfo.Arguments = $"/c chcp 65001 >nul & {childCommandLine}";

		return startInfo;
	}

	private static string Quote(string value) => $"\"{value}\"";

	/// <summary>
	/// The commands, then the closed stream. Closing it matters beyond tidiness: a session whose
	/// input ends returns <see cref="ExitCode.Success"/> by the same path <c>exit</c> takes, so an
	/// unclosed stream would leave the process at a prompt until the timeout killed it.
	/// </summary>
	private static void EnterCommands(Process process, IEnumerable<string> enteredCommands)
	{
		foreach (string enteredCommand in enteredCommands)
		{
			process.StandardInput.WriteLine(enteredCommand);
		}

		process.StandardInput.Flush();
		process.StandardInput.Close();
	}

	/// <summary>
	/// Waits for the exit, then waits again without a timeout. The second wait is not redundant: it
	/// is what guarantees the asynchronous output handlers have run to completion, so the captured
	/// transcript is the whole one rather than however much had arrived when the process died.
	/// </summary>
	private static void WaitForTheSessionToEnd(Process process)
	{
		if (!process.WaitForExit(TimeSpan.FromSeconds(PROCESS_TIMEOUT_IN_SECONDS)))
		{
			process.Kill(entireProcessTree: true);
			process.WaitForExit();

			throw new TimeoutException(string.Format(
				CultureInfo.InvariantCulture,
				_PROCESS_TIMED_OUT_MESSAGE_FORMAT,
				PROCESS_TIMEOUT_IN_SECONDS));
		}

		process.WaitForExit();
	}

	private static void AppendLine(StringBuilder transcript, string? line)
	{
		if (line is not null)
		{
			transcript.AppendLine(line);
		}
	}
}
