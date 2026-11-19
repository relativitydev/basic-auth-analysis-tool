using System.Runtime.InteropServices;

namespace MigrateAuth.FakeAgent;

/// <summary>Raises a signal on this very process, so a fake can end by one on Linux.</summary>
internal static class PosixSignal
{
	[DllImport("libc", EntryPoint = "kill", SetLastError = true)]
	private static extern int KillProcess(int processId, int signalNumber);

	internal static bool TryRaiseOnSelf(int signalNumber)
	{
		bool raised;

		try
		{
			raised = KillProcess(Environment.ProcessId, signalNumber) == 0;
		}
		catch (DllNotFoundException)
		{
			raised = false;
		}
		catch (EntryPointNotFoundException)
		{
			raised = false;
		}

		return raised;
	}
}
