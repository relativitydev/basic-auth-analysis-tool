using System.Runtime.InteropServices;

using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The quit-signal shield. On Linux the real signal is raised on the test process itself while the shield is
/// active, and the test asserts the process survived and the signal was counted. Where the platform has no such
/// signal the shield does nothing, which is asserted too: those bodies run on Windows and are the no-op case.
/// </summary>
public sealed class InterruptShieldTests
{
	private const int _SIGQUIT_NUMBER = 3;
	private const int _ARRIVAL_POLL_ATTEMPTS = 100;
	private const int _ARRIVAL_POLL_MILLISECONDS = 50;
	private const int _EXPECTED_ARRIVALS = 1;

	[Fact]
	public void WhereTheSignalCannotBeRegisteredTheShieldDoesNothing()
	{
		if (OperatingSystem.IsWindows())
		{
			using InterruptShield shield = InterruptShield.Begin();

			Assert.False(shield.IsActive);
			Assert.Equal(0, shield.InterceptedCount);
		}
	}

	[Fact]
	public void OnLinuxTheShieldIsActiveForTheLaunchAndInactiveAfterIt()
	{
		if (OperatingSystem.IsLinux())
		{
			InterruptShield shield = InterruptShield.Begin();

			Assert.True(shield.IsActive);

			shield.Dispose();

			Assert.False(shield.IsActive);
		}
	}

	[Fact]
	public void OnLinuxAQuitSignalRaisedWhileTheShieldIsActiveDoesNotEndTheProcess()
	{
		if (OperatingSystem.IsLinux())
		{
			using InterruptShield shield = InterruptShield.Begin();

			Assert.Equal(0, KillProcess(Environment.ProcessId, _SIGQUIT_NUMBER));
			Assert.True(WaitForArrivals(shield), "The signal was not counted by the shield.");
			Assert.Equal(_EXPECTED_ARRIVALS, shield.InterceptedCount);
		}
	}

	[Fact]
	public void DisposingTwiceIsHarmless()
	{
		InterruptShield shield = InterruptShield.Begin();

		shield.Dispose();
		shield.Dispose();

		Assert.False(shield.IsActive);
	}

	private static bool WaitForArrivals(InterruptShield shield)
	{
		bool arrived = false;

		for (int attempt = 0; attempt < _ARRIVAL_POLL_ATTEMPTS && !arrived; attempt++)
		{
			arrived = shield.InterceptedCount >= _EXPECTED_ARRIVALS;

			if (!arrived)
			{
				Thread.Sleep(_ARRIVAL_POLL_MILLISECONDS);
			}
		}

		return arrived;
	}

	[DllImport("libc", EntryPoint = "kill", SetLastError = true)]
	private static extern int KillProcess(int processId, int signalNumber);
}
