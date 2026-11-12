using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE AIR-GAP PROOF ON LINUX: the built CLI runs generate-only inside a network namespace that has no
/// interface at all (<c>unshare -rn</c>) and still finishes with exit code 0 and writes its pack, so nothing in the
/// generate-only path needs a network. A second test runs the same command under <c>strace</c> and asserts that no
/// internet-family socket is even created.
///
/// WHERE IT RUNS. On Linux only; elsewhere both tests are declared no-ops. On Linux the tests FAIL, and never skip,
/// when the namespace cannot be made or when the tracer is absent: a proof that quietly turns into a skip proves
/// nothing, and the test machine is expected to have both.
///
/// No agent is started: the child's search path is replaced as for every process test and holds no agent, so the stage
/// writes the pack and reports that the command was not found.
/// </summary>
public sealed class AirGapNetworkNamespaceProcessTests
{
	private const string _UNSHARE_COMMAND_NAME = "unshare";
	private const string _NEW_USER_AND_NETWORK_NAMESPACE_OPTIONS = "-rn";
	private const string _SUDO_COMMAND_NAME = "sudo";
	private const string _SUDO_NON_INTERACTIVE_OPTION = "-n";
	private const string _NEW_NETWORK_NAMESPACE_OPTION = "-n";
	private const string _TRUE_COMMAND_NAME = "true";
	private const string _COMMAND_LINE_SEPARATOR = " ";
	private const string _STRACE_PATH = "/usr/bin/strace";
	private const string _STRACE_FOLLOW_CHILDREN_OPTION = "-f";
	private const string _STRACE_EXPRESSION_OPTION = "-e";
	private const string _STRACE_SOCKET_AND_CONNECT_EXPRESSION = "trace=socket,connect";
	private const string _STRACE_OUTPUT_FILE_OPTION = "-o";
	private const string _TRACE_FILE_NAME = "socket-trace.txt";
	private const string _SELECTION_NUMBER_OF_THE_FIRST_ENVIRONMENT = "1";

	/// <summary>
	/// A <c>socket()</c> call that creates an IPv4 or IPv6 socket, or a <c>connect()</c> whose address family is one of
	/// them. These two families are asserted on specifically: they are the ones that can leave the machine. AF_UNIX is
	/// tolerated because the runtime itself may open a local socket for its own diagnostics, which is not a network.
	/// </summary>
	private const string _INTERNET_FAMILY_PATTERN = @"socket\(AF_INET6?\b|sa_family=AF_INET6?\b";

	private const string _NO_UNSHARE_MESSAGE_FORMAT =
		"The network namespace could not be created: '{0}' exited with code {1} and wrote '{2}'; then '{3}' exited with code {4} and wrote '{5}'. This test fails rather than skips, because without the namespace the air-gap proof is missing; the machine must allow unprivileged user namespaces or passwordless sudo for unshare.";

	private const string _NO_TRACER_MESSAGE_FORMAT =
		"The tracer {0} is not present. This test fails rather than skips, because without it the socket trace is missing; install strace on the machine that runs the Linux proof.";

	private const string _CHILD_FAILED_MESSAGE_FORMAT =
		"The generate-only session did not end with exit code 0 inside the empty network namespace. Exit code {0}. Standard error: {1}";

	private const string _INTERNET_SOCKET_MESSAGE_FORMAT =
		"The trace shows an internet-family socket, which a generate-only run must never create:{0}{1}";

	private const string _PROCESS_START_FAILED_MESSAGE_FORMAT = "The process {0} did not start.";

	private const string _PROCESS_TIMED_OUT_MESSAGE_FORMAT = "The process did not exit within {0} seconds and was killed.";

	[Fact]
	public void GenerateOnlyInANetworkNamespaceWithoutInterfacesSucceedsAndWritesThePack()
	{
		if (OperatingSystem.IsLinux())
		{
			NamespaceLauncher launcher = RequireTheNamespaceCanBeMade();

			using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
			using TemporaryFolder destination = TemporaryFolder.Create();

			AiAssistProcessSupport.WriteRepository(repository);

			ProcessResult result = RunGenerateOnly(launcher, repository, destination, []);

			Assert.True(result.ExitCode == (int)ExitCode.Success, string.Format(CultureInfo.InvariantCulture, _CHILD_FAILED_MESSAGE_FORMAT, result.ExitCode, result.StandardError));
			Assert.Single(AiAssistProcessSupport.PackNames(destination));
			Assert.Empty(AiAssistProcessSupport.LaunchFolders(destination));
		}
	}

	[Fact]
	public void TheSocketTraceOfAGenerateOnlyRunShowsNoInternetFamilySocket()
	{
		if (OperatingSystem.IsLinux())
		{
			NamespaceLauncher launcher = RequireTheNamespaceCanBeMade();
			RequireTheTracer();

			using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
			using TemporaryFolder destination = TemporaryFolder.Create();
			using TemporaryFolder traceFolder = TemporaryFolder.Create();

			AiAssistProcessSupport.WriteRepository(repository);

			string traceFilePath = traceFolder.Combine(_TRACE_FILE_NAME);
			ProcessResult result = RunGenerateOnly(launcher, repository, destination, TracerArguments(traceFilePath));

			Assert.True(result.ExitCode == (int)ExitCode.Success, string.Format(CultureInfo.InvariantCulture, _CHILD_FAILED_MESSAGE_FORMAT, result.ExitCode, result.StandardError));
			Assert.True(File.Exists(traceFilePath), "The tracer wrote no trace file.");

			string[] internetLines = [.. File.ReadAllLines(traceFilePath).Where(line => Regex.IsMatch(line, _INTERNET_FAMILY_PATTERN))];

			Assert.True(
				internetLines.Length == 0,
				string.Format(CultureInfo.InvariantCulture, _INTERNET_SOCKET_MESSAGE_FORMAT, Environment.NewLine, string.Join(Environment.NewLine, internetLines)));
		}
	}

	/// <summary>The tracer and its options, ending where the traced command begins.</summary>
	private static string[] TracerArguments(string traceFilePath)
	{
		return
		[
			_STRACE_PATH,
			_STRACE_FOLLOW_CHILDREN_OPTION,
			_STRACE_EXPRESSION_OPTION,
			_STRACE_SOCKET_AND_CONNECT_EXPRESSION,
			_STRACE_OUTPUT_FILE_OPTION,
			traceFilePath
		];
	}

	private static void RequireTheTracer()
	{
		if (!File.Exists(_STRACE_PATH))
		{
			Assert.Fail(string.Format(CultureInfo.InvariantCulture, _NO_TRACER_MESSAGE_FORMAT, _STRACE_PATH));
		}
	}

	/// <summary>
	/// Makes the namespace around the command <c>true</c> with <c>unshare -rn</c> and, when that fails, with
	/// <c>sudo -n unshare -n</c>; returns the form that works, and fails with both reasons when neither does.
	/// </summary>
	private static NamespaceLauncher RequireTheNamespaceCanBeMade()
	{
		NamespaceLauncher unprivileged = new(_UNSHARE_COMMAND_NAME, [_NEW_USER_AND_NETWORK_NAMESPACE_OPTIONS]);
		NamespaceLauncher privileged = new(_SUDO_COMMAND_NAME, [_SUDO_NON_INTERACTIVE_OPTION, _UNSHARE_COMMAND_NAME, _NEW_NETWORK_NAMESPACE_OPTION]);

		ProcessResult unprivilegedResult = ProbeLauncher(unprivileged);
		NamespaceLauncher working = unprivileged;

		if (unprivilegedResult.ExitCode != (int)ExitCode.Success)
		{
			ProcessResult privilegedResult = ProbeLauncher(privileged);

			working = privileged;

			if (privilegedResult.ExitCode != (int)ExitCode.Success)
			{
				Assert.Fail(string.Format(
					CultureInfo.InvariantCulture,
					_NO_UNSHARE_MESSAGE_FORMAT,
					unprivileged.CommandLine,
					unprivilegedResult.ExitCode,
					unprivilegedResult.StandardError.Trim(),
					privileged.CommandLine,
					privilegedResult.ExitCode,
					privilegedResult.StandardError.Trim()));
			}
		}

		return working;
	}

	/// <summary>Runs <c>true</c> inside the namespace made by the launcher.</summary>
	private static ProcessResult ProbeLauncher(NamespaceLauncher launcher)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = FindOnInheritedPath(launcher.CommandName)
		};

		foreach (string argument in launcher.LeadingArguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		startInfo.ArgumentList.Add(_TRUE_COMMAND_NAME);

		return RunToEnd(startInfo, []);
	}

	/// <summary>
	/// The generate-only session wrapped in the namespace: <c>unshare -rn [tracer ...] dotnet migrate-auth.dll ...</c>,
	/// with the four stages, the stage's command and the first environment typed on redirected input, so the stage writes
	/// its pack and offers nothing.
	/// </summary>
	private static ProcessResult RunGenerateOnly(NamespaceLauncher launcher, RepositoryFixture repository, TemporaryFolder destination, string[] tracerArguments)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = FindOnInheritedPath(launcher.CommandName),
			WorkingDirectory = AppContext.BaseDirectory,
			StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
		};

		foreach (string leadingArgument in launcher.LeadingArguments)
		{
			startInfo.ArgumentList.Add(leadingArgument);
		}

		foreach (string tracerArgument in tracerArguments)
		{
			startInfo.ArgumentList.Add(tracerArgument);
		}

		startInfo.ArgumentList.Add(CliProcessHarness.HostPath);
		startInfo.ArgumentList.Add(CliProcessHarness.EntryAssemblyPath);
		startInfo.ArgumentList.Add(CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION);
		startInfo.ArgumentList.Add(repository.RootPath);
		startInfo.ArgumentList.Add(CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION);
		startInfo.ArgumentList.Add(destination.Path);

		using TemporaryFolder emptyProfileFolder = TemporaryFolder.Create();
		using ChildPathReplacement pathReplacement = ChildPathReplacement.Apply(startInfo, agentFolderPath: null);

		CliProcessHarness.IsolateProfile(startInfo, emptyProfileFolder.Path);

		return RunToEnd(
			startInfo,
			AiAssistProcessSupport.FourStagesThen(
				AiAssistProcessSupport.AiAssistCommand,
				_SELECTION_NUMBER_OF_THE_FIRST_ENVIRONMENT,
				AiAssistProcessSupport.ExitCommand));
	}

	private static string FindOnInheritedPath(string commandName)
	{
		string inherited = Environment.GetEnvironmentVariable(ChildPathReplacement.PATH_VARIABLE_NAME) ?? string.Empty;
		string? found = inherited
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(entry => Path.Combine(entry, commandName))
			.FirstOrDefault(File.Exists);

		return found ?? commandName;
	}

	/// <summary>Runs one process to its end with the given input lines, killing it when it outlives the harness timeout.</summary>
	private static ProcessResult RunToEnd(ProcessStartInfo startInfo, IEnumerable<string> inputLines)
	{
		startInfo.RedirectStandardInput = true;
		startInfo.RedirectStandardOutput = true;
		startInfo.RedirectStandardError = true;
		startInfo.UseShellExecute = false;

		StringBuilder standardError = new();

		using Process process = Process.Start(startInfo)
			?? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, _PROCESS_START_FAILED_MESSAGE_FORMAT, startInfo.FileName));

		Task<string> outputReader = process.StandardOutput.ReadToEndAsync();
		Task<string> errorReader = process.StandardError.ReadToEndAsync();

		foreach (string inputLine in inputLines)
		{
			process.StandardInput.WriteLine(inputLine);
		}

		process.StandardInput.Close();

		if (!process.WaitForExit(TimeSpan.FromSeconds(CliProcessHarness.PROCESS_TIMEOUT_IN_SECONDS)))
		{
			process.Kill(entireProcessTree: true);
			process.WaitForExit();

			throw new TimeoutException(string.Format(
				CultureInfo.InvariantCulture,
				_PROCESS_TIMED_OUT_MESSAGE_FORMAT,
				CliProcessHarness.PROCESS_TIMEOUT_IN_SECONDS));
		}

		outputReader.GetAwaiter().GetResult();
		standardError.Append(errorReader.GetAwaiter().GetResult());

		return new ProcessResult(process.ExitCode, standardError.ToString());
	}

	private sealed record ProcessResult(int ExitCode, string StandardError);

	/// <summary>The command and leading options that make the empty network namespace around whatever follows.</summary>
	private sealed record NamespaceLauncher(string CommandName, string[] LeadingArguments)
	{
		public string CommandLine => string.Join(_COMMAND_LINE_SEPARATOR, LeadingArguments.Prepend(CommandName).Append(_TRUE_COMMAND_NAME));
	}
}
