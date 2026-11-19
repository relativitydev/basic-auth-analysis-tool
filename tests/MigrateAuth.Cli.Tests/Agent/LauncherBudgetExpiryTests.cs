using System.Diagnostics;

using MigrateAuth.Cli.Agent;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// A launcher built with a test budget ends a hanging agent by killing its process tree and reports
/// <see cref="AgentTerminationOutcome.TimedOut"/>; a launcher built without one waits for the agent to end by
/// itself. These tests assert the outcome and that no fake survives, never how long anything took.
/// </summary>
public sealed class LauncherBudgetExpiryTests
{
	private const string _COMMAND_NAME = "claude";
	private const int _BUDGET_SECONDS = 5;
	private const int _SHORT_LIFETIME_SECONDS = 1;
	private const int _EXIT_CODE_AFTER_LIFETIME = 4;
	private const int _RECORD_POLL_ATTEMPTS = 600;
	private const int _RECORD_POLL_MILLISECONDS = 100;

	[Fact]
	public void AHangingFakeIsKilledAndTheLaunchReportsATimeoutWithNoSurvivor()
	{
		using LauncherScenario scenario = new();
		string executable = scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior { Ending = FakeAgentEnding.Hang });
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode, TimeSpan.FromSeconds(_BUDGET_SECONDS));

		AgentTerminationStatus status = launcher.Launch(scenario.WriteLaunchCopy());

		Assert.Equal(AgentTerminationStatus.TimedOut(), status);
		Assert.Null(status.ExitCode);
		Assert.Null(status.SignalNumber);
		Assert.False(FakeAgentFolder.IsAnyRunningFrom(executable), "The fake agent survived the end of its test budget.");
	}

	[Fact]
	public void TheTreeBehindAWrapperIsKilledWithTheInterpreterAndNoFakeSurvives()
	{
		if (OperatingSystem.IsWindows())
		{
			using LauncherScenario scenario = new();
			string target = scenario.Fakes.InstallCmdOnly(_COMMAND_NAME, new FakeAgentBehavior { Ending = FakeAgentEnding.Hang });
			IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode, TimeSpan.FromSeconds(_BUDGET_SECONDS));
			PromptPackFilePath copy = scenario.WriteLaunchCopy();

			Assert.Equal(AgentLaunchEligibility.Launchable, launcher.CheckLaunchable(copy));
			Assert.Equal(AgentTerminationStatus.TimedOut(), launcher.Launch(copy));
			Assert.False(FakeAgentFolder.IsAnyRunningFrom(target), "The fake behind the wrapper survived the end of its test budget.");
		}
	}

	[Fact]
	public void AFakeThatEndsWithinTheBudgetIsReportedByItsExitAndNotAsATimeout()
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior { ExitCode = _EXIT_CODE_AFTER_LIFETIME });

		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode, TimeSpan.FromSeconds(_BUDGET_SECONDS));

		Assert.Equal(AgentTerminationStatus.ExitedWithCode(_EXIT_CODE_AFTER_LIFETIME), launcher.Launch(scenario.WriteLaunchCopy()));
	}

	[Fact]
	public void ALauncherWithoutABudgetWaitsForAFakeThatEndsItselfAfterItsLifetime()
	{
		using LauncherScenario scenario = new();
		string executable = scenario.Fakes.Install(
			_COMMAND_NAME,
			new FakeAgentBehavior { Ending = FakeAgentEnding.Hang, HangLifetimeSeconds = _SHORT_LIFETIME_SECONDS, ExitCode = _EXIT_CODE_AFTER_LIFETIME });
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode);

		AgentTerminationStatus status = launcher.Launch(scenario.WriteLaunchCopy());

		Assert.Equal(AgentTerminationStatus.ExitedWithCode(_EXIT_CODE_AFTER_LIFETIME), status);
		Assert.True(FakeAgentRecord.ReadFrom($"{executable}{FakeAgentRecord.FILE_SUFFIX}").EndedByLifetime);
	}

	[Fact]
	public void TheSurvivorCheckFindsAFakeThatIsStillRunningAndNotOneThatWasKilled()
	{
		using FakeAgentFolder fakes = new();
		string executable = fakes.Install(_COMMAND_NAME, new FakeAgentBehavior { Ending = FakeAgentEnding.Hang });
		using Process fake = FakeAgentFolder.Start(executable, [], adjustEnvironment: null, redirectInput: false);

		WaitForTheRecord(executable);

		bool foundWhileRunning = FakeAgentFolder.IsAnyRunningFrom(executable);

		fake.Kill(entireProcessTree: true);
		fake.WaitForExit();

		Assert.True(foundWhileRunning, "The survivor check did not find a fake that was running.");
		Assert.False(FakeAgentFolder.IsAnyRunningFrom(executable), "The survivor check found a fake that had been killed.");
	}

	/// <summary>Waits until the fake has started and written its record, so that the check runs against a live fake.</summary>
	private static void WaitForTheRecord(string executablePath)
	{
		string recordPath = $"{executablePath}{FakeAgentRecord.FILE_SUFFIX}";

		for (int attempt = 0; attempt < _RECORD_POLL_ATTEMPTS && !File.Exists(recordPath); attempt++)
		{
			Thread.Sleep(_RECORD_POLL_MILLISECONDS);
		}

		Assert.True(File.Exists(recordPath), "The fake did not start and write its record in time.");
	}
}
