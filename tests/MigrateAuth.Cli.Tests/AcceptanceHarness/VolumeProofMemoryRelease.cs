using System.Runtime.InteropServices;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// Returns the memory a volume-proof class leaves behind, when the class starts and when it ends.
///
/// WHY. The volume proofs sample the working set of this same test host and assert it under one gibibyte, and the host outlives
/// every class that runs in it. What an earlier class leaves resident - managed memory the collector has not returned, and on Linux the
/// native allocator's per-thread arenas, which a collection does not return - is counted in the next proof's baseline. A forced, blocking,
/// compacting collection (<see cref="PeakWorkingSetSampler.ReleaseUnusedMemory"/>) and, on Linux, a trim of the allocator return it.
/// </summary>
public sealed class VolumeProofMemoryRelease : IDisposable
{
	private const int _TRIM_KEEPING_NO_PADDING = 0;

	public VolumeProofMemoryRelease()
	{
		ReleaseAndTrim();
	}

	/// <summary>Collects what can be collected and, on Linux, asks the native allocator to give its free memory back.</summary>
	internal static void ReleaseAndTrim()
	{
		PeakWorkingSetSampler.ReleaseUnusedMemory();

		if (OperatingSystem.IsLinux())
		{
			TrimNativeAllocator(_TRIM_KEEPING_NO_PADDING);
		}
	}

	public void Dispose()
	{
		ReleaseAndTrim();
	}

	[DllImport("libc", EntryPoint = "malloc_trim")]
	private static extern int TrimNativeAllocator(int padding);
}
