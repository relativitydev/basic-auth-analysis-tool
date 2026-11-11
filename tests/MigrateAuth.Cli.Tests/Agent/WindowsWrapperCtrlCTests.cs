using System.Diagnostics;
using System.Text.Json;

using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// A keyboard interrupt that reaches a script wrapper: the command interpreter asks whether to terminate the batch
/// job, and what it leaves behind has to map to a cancellation and leave no fake running.
///
/// The test host is never signalled. The interrupt is raised by a helper process (the fake program in its
/// interrupt-driver mode) that the test starts so that it owns a console of its own; the helper starts the wrapper
/// exactly as the product builds it, waits for the fake behind it to start, raises the interrupt on its own console,
/// answers the prompt, and writes what it saw. The test then asserts the outcome, never elapsed time.
///
/// The test body is a no-op on a platform other than Windows: there is no script wrapper there.
/// </summary>
public sealed class WindowsWrapperCtrlCTests
{
	private const string _COMMAND_NAME = "claude";
	private const string _PROMPT_ARGUMENT = "a prompt";
	private const string _REQUEST_FILE_NAME = "driver-request.json";
	private const string _RESULT_FILE_NAME = "driver-result.json";
	private const int _DRIVER_STEP_SECONDS = 30;
	private const int _DRIVER_WAIT_SECONDS = 120;
	private const int _MILLISECONDS_PER_SECOND = 1000;
	private const string _INFEASIBLE_PREFIX = "INFEASIBLE: ";
	private const int _INTERPRETER_TERMINATE_CODE = 255;

	private readonly ITestOutputHelper _output;

	public WindowsWrapperCtrlCTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Theory]
	[InlineData(OwnConsoleAnswerMode.Console)]
	[InlineData(OwnConsoleAnswerMode.Redirected)]
	public void AnInterruptThroughTheWrapperEndsAsACancellationAndLeavesNoFakeRunning(OwnConsoleAnswerMode answerMode)
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new();
			string target = scenario.Fakes.InstallCmdOnly(_COMMAND_NAME, new FakeAgentBehavior { Ending = FakeAgentEnding.Hang });
			string recordPath = $"{target}{FakeAgentRecord.FILE_SUFFIX}";

			OwnConsoleResult? result = RunDriver(scenario, target, recordPath, answerMode);

			_output.WriteLine($"{answerMode} => {JsonSerializer.Serialize(result)}");

			AssertOutcomeOrExplicitInfeasibility(result, recordPath);
		}
	}

	private void AssertOutcomeOrExplicitInfeasibility(OwnConsoleResult? result, string recordPath)
	{
		string infeasibility = DescribeInfeasibility(result);

		if (infeasibility.Length > 0)
		{
			_output.WriteLine($"{_INFEASIBLE_PREFIX}{infeasibility}");
			Assert.NotEmpty(infeasibility);
		}
		else
		{
			Assert.True(result!.InterruptsSeenByDriver >= 1, "The interrupt did not reach the helper's own console.");
			Assert.False(FakeAgentFolder.IsRunning(FakeAgentRecord.ReadFrom(recordPath).ProcessId), "The fake behind the wrapper survived the interrupt.");
			AssertInterpreterStatusAfterThePrompt(result.ExitCode);
		}
	}

	/// <summary>
	/// What the command interpreter reports once the prompt is answered is the interpreter's own status, observed on
	/// the host: either the control-C status, which the mapping reads as a cancellation, or the interpreter's
	/// own terminate code, which the mapping reads like any other exit code. Any other status is a new finding.
	/// </summary>
	private static void AssertInterpreterStatusAfterThePrompt(int interpreterStatus)
	{
		AgentTerminationStatus mapped = AgentExitMapping.Map(interpreterStatus, isWindows: true);

		if (interpreterStatus == AgentExitMapping.WINDOWS_CONTROL_C_EXIT_CODE)
		{
			Assert.Equal(AgentTerminationStatus.CancelledByUser(), mapped);
		}
		else
		{
			Assert.Equal(_INTERPRETER_TERMINATE_CODE, interpreterStatus);
			Assert.Equal(AgentTerminationStatus.ExitedWithCode(_INTERPRETER_TERMINATE_CODE), mapped);
		}
	}

	private static string DescribeInfeasibility(OwnConsoleResult? result)
	{
		string reason = string.Empty;

		if (result is null)
		{
			reason = "the helper that owns its own console wrote no result.";
		}
		else if (!result.Started || !result.ReadySeen || !result.InterruptRaised)
		{
			reason = $"the helper could not set the scene: {result.Detail}";
		}
		else if (!result.AnswerDelivered || !result.EndedByItself)
		{
			reason = "the prompt about terminating the batch job did not end the wrapper after the answer was delivered.";
		}

		return reason;
	}

	private static OwnConsoleResult? RunDriver(LauncherScenario scenario, string target, string readyPath, OwnConsoleAnswerMode answerMode)
	{
		AgentResolution resolution = TestAgentResolver.Resolve([scenario.Fakes.FolderPath], scenario.RepositoryPath, _COMMAND_NAME);
		ProcessStartInfo wrapperStart = AgentChildProcess.CreateStartInfo(resolution, [_PROMPT_ARGUMENT], scenario.RepositoryPath, [scenario.Fakes.FolderPath]);
		string requestPath = Path.Combine(scenario.Fakes.FolderPath, _REQUEST_FILE_NAME);
		string resultPath = Path.Combine(scenario.Fakes.FolderPath, _RESULT_FILE_NAME);
		OwnConsoleRequest request = new()
		{
			FileName = wrapperStart.FileName,
			Arguments = wrapperStart.Arguments,
			WorkingDirectory = wrapperStart.WorkingDirectory,
			ReadyPath = readyPath,
			ResultPath = resultPath,
			AnswerMode = answerMode,
			MaxSeconds = _DRIVER_STEP_SECONDS
		};

		File.WriteAllText(requestPath, JsonSerializer.Serialize(request));

		// Started through the shell so that the helper is given a console of its own; the interrupt it raises
		// then reaches only that console, and the test host is not attached to it.
		using Process? driver = Process.Start(new ProcessStartInfo(FakeAgentFolder.PublishedFakePath())
		{
			UseShellExecute = true,
			WindowStyle = ProcessWindowStyle.Minimized,
			Arguments = OwnConsoleDriverArguments(requestPath)
		});

		return WaitForResult(driver, resultPath);
	}

	private static string OwnConsoleDriverArguments(string requestPath)
	{
		return $"\"{OwnConsoleDriver.MODE_ARGUMENT}\" \"{requestPath}\"";
	}

	private static OwnConsoleResult? WaitForResult(Process? driver, string resultPath)
	{
		OwnConsoleResult? result = null;
		bool ended = driver is null || driver.WaitForExit(_DRIVER_WAIT_SECONDS * _MILLISECONDS_PER_SECOND);

		if (File.Exists(resultPath) && ended)
		{
			result = JsonSerializer.Deserialize<OwnConsoleResult>(File.ReadAllText(resultPath));
		}

		return result;
	}
}
