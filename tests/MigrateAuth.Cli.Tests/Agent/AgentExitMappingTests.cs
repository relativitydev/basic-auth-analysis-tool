using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The table that turns the code the operating system reported into a termination status, on both platforms'
/// rows whatever machine runs the test, and then each ending a launcher can see (exit 0, a non-zero exit, a
/// signal, an interruption) through a launcher and its fake. The ending by a test budget is in
/// <see cref="LauncherBudgetExpiryTests"/>.
/// </summary>
public sealed class AgentExitMappingTests
{
	private const string _COMMAND_NAME = "claude";
	private const int _NON_ZERO_EXIT_CODE = 3;
	private const int _SIGNAL_TERMINATE = 15;
	private const int _SIGNAL_KILL = 9;
	private const int _LINUX_INTERRUPT = 130;
	private const int _WINDOWS_INTERRUPT = unchecked((int)0xC000013A);
	private const int _ONE_BELOW_SIGNAL_RANGE = 128;
	private const int _ONE_ABOVE_SIGNAL_RANGE = 193;
	private const int _FIRST_SIGNAL_CODE = 129;
	private const int _LAST_SIGNAL_CODE = 192;

	[Fact]
	public void OnWindowsTheControlCStatusIsACancellationAndEveryOtherCodeIsTheAgentsOwn()
	{
		Assert.Equal(AgentTerminationStatus.CancelledByUser(), AgentExitMapping.Map(_WINDOWS_INTERRUPT, isWindows: true));
		Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), AgentExitMapping.Map(0, isWindows: true));
		Assert.Equal(AgentTerminationStatus.ExitedWithCode(_NON_ZERO_EXIT_CODE), AgentExitMapping.Map(_NON_ZERO_EXIT_CODE, isWindows: true));
		Assert.Equal(AgentTerminationStatus.ExitedWithCode(_LINUX_INTERRUPT), AgentExitMapping.Map(_LINUX_INTERRUPT, isWindows: true));
		Assert.Equal(AgentTerminationStatus.ExitedWithCode(_SIGNAL_TERMINATE + _ONE_BELOW_SIGNAL_RANGE), AgentExitMapping.Map(_SIGNAL_TERMINATE + _ONE_BELOW_SIGNAL_RANGE, isWindows: true));
	}

	[Fact]
	public void OnLinuxOneHundredAndThirtyIsACancellation()
	{
		Assert.Equal(AgentTerminationStatus.CancelledByUser(), AgentExitMapping.Map(_LINUX_INTERRUPT, isWindows: false));
	}

	[Theory]
	[InlineData(_FIRST_SIGNAL_CODE, 1)]
	[InlineData(_ONE_BELOW_SIGNAL_RANGE + _SIGNAL_KILL, _SIGNAL_KILL)]
	[InlineData(_ONE_BELOW_SIGNAL_RANGE + _SIGNAL_TERMINATE, _SIGNAL_TERMINATE)]
	[InlineData(_LAST_SIGNAL_CODE, 64)]
	public void OnLinuxTheRangeOtherThanTheInterruptIsASignalNumberedByTheCodeMinusOneHundredAndTwentyEight(int exitCode, int signalNumber)
	{
		Assert.Equal(AgentTerminationStatus.TerminatedBySignal(signalNumber), AgentExitMapping.Map(exitCode, isWindows: false));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(_NON_ZERO_EXIT_CODE)]
	[InlineData(_ONE_BELOW_SIGNAL_RANGE)]
	[InlineData(_ONE_ABOVE_SIGNAL_RANGE)]
	[InlineData(255)]
	[InlineData(_WINDOWS_INTERRUPT)]
	public void OnLinuxEveryOtherCodeIsTheAgentsOwn(int exitCode)
	{
		Assert.Equal(AgentTerminationStatus.ExitedWithCode(exitCode), AgentExitMapping.Map(exitCode, isWindows: false));
	}

	[Fact]
	public void NoMappingYieldsATimeoutBecauseOnlyATestBudgetCanEndOneThatWay()
	{
		int[] sampleCodes = [0, 1, _NON_ZERO_EXIT_CODE, _FIRST_SIGNAL_CODE, _LINUX_INTERRUPT, _LAST_SIGNAL_CODE, _ONE_ABOVE_SIGNAL_RANGE, _WINDOWS_INTERRUPT];

		foreach (int code in sampleCodes)
		{
			Assert.NotEqual(AgentTerminationOutcome.TimedOut, AgentExitMapping.Map(code, isWindows: true).Outcome);
			Assert.NotEqual(AgentTerminationOutcome.TimedOut, AgentExitMapping.Map(code, isWindows: false).Outcome);
			Assert.NotEqual(AgentTerminationOutcome.NotFound, AgentExitMapping.Map(code, isWindows: false).Outcome);
		}
	}

	[Fact]
	public void AFakeThatExitsWithZeroIsReportedAsExitedWithZero()
	{
		Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), LaunchFake(new FakeAgentBehavior()));
	}

	[Fact]
	public void AFakeThatExitsWithANonZeroCodeIsReportedWithThatCode()
	{
		Assert.Equal(
			AgentTerminationStatus.ExitedWithCode(_NON_ZERO_EXIT_CODE),
			LaunchFake(new FakeAgentBehavior { ExitCode = _NON_ZERO_EXIT_CODE }));
	}

	[Fact]
	public void AFakeThatEndsTheWayAnInterruptedProcessEndsIsReportedAsCancelledByTheUser()
	{
		Assert.Equal(
			AgentTerminationStatus.CancelledByUser(),
			LaunchFake(new FakeAgentBehavior { Ending = FakeAgentEnding.Interrupt }));
	}

	[Fact]
	public void AFakeEndedByASignalOnLinuxIsReportedWithThatSignalNumber()
	{
		if (OperatingSystem.IsLinux())
		{
			Assert.Equal(
				AgentTerminationStatus.TerminatedBySignal(_SIGNAL_KILL),
				LaunchFake(new FakeAgentBehavior { Ending = FakeAgentEnding.Signal, SignalNumber = _SIGNAL_KILL }));
		}
	}

	private static AgentTerminationStatus LaunchFake(FakeAgentBehavior behavior)
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(_COMMAND_NAME, behavior);

		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);

		return launcher.Launch(scenario.WriteLaunchCopy());
	}
}
