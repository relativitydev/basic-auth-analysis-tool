using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;

using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// The peak working set of this process over one operation, taken by sampling.
///
/// WHY SAMPLED. The operating system's own peak counter is a high-water mark for the whole life of
/// the process, and a test host has run other things before it reaches a volume proof; the counter
/// cannot be reset from managed code on Windows. So the process's current working set is read on a
/// short interval from a background thread, and the maximum read is the figure. It can only
/// under-read a spike shorter than the interval; the operations measured here hold their memory for
/// hundreds of milliseconds, and the interval is a few.
///
/// <see cref="ReleaseUnusedMemory"/> is called before each operation so that what an earlier
/// operation left resident is not attributed to this one. On Windows it also trims the working set,
/// because freed managed memory otherwise stays resident until the operating system needs it back;
/// elsewhere the collector's own return of memory is all there is, so a figure taken after a
/// larger earlier operation is an upper bound on the operation's own.
/// </summary>
internal sealed class PeakWorkingSetSampler
{
	private const int _SAMPLE_INTERVAL_IN_MILLISECONDS = 2;
	private const string _SAMPLER_THREAD_NAME = "peak-working-set-sampler";

	/// <summary>The smallest pooled buffer worth draining: below it the pool holds too little for the figures to notice.</summary>
	private const int _SMALLEST_DRAINED_BUFFER_IN_BYTES = 1024 * 1024;

	private readonly Thread _thread;

	private volatile bool _stopRequested;
	private long _peakBytes;

	private PeakWorkingSetSampler()
	{
		_thread = new Thread(SampleUntilStopped)
		{
			IsBackground = true,
			Name = _SAMPLER_THREAD_NAME
		};
	}

	/// <summary>
	/// Collects what can be collected, and trims the working set where the platform lets a process do so.
	///
	/// The shared buffer pool is drained first. A collection cannot free what the pool holds: the JSON serializer rents its output
	/// buffer from <see cref="ArrayPool{T}.Shared"/>, which keeps a returned buffer for some seconds before it trims it on a later
	/// collection. The save of the largest session state leaves the buffer, up to a quarter of a gigabyte, in the pool,
	/// and the next proof's baseline then depends on how recently an earlier proof saved or loaded. Renting one buffer of every pooled size
	/// takes the held one out of the pool, and dropping it lets the collection below free it.
	/// </summary>
	internal static void ReleaseUnusedMemory()
	{
		DrainTheSharedBufferPool();

		GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

		if (OperatingSystem.IsWindows())
		{
			using Process current = Process.GetCurrentProcess();

			EmptyWorkingSet(current.Handle);
		}
	}

	private static void DrainTheSharedBufferPool()
	{
		ulong largestBuffer = BitOperations.RoundUpToPowerOf2((ulong)SessionStateStore.MAXIMUM_STATE_FILE_LENGTH_IN_BYTES);

		for (ulong length = _SMALLEST_DRAINED_BUFFER_IN_BYTES; length <= largestBuffer; length <<= 1)
		{
			_ = ArrayPool<byte>.Shared.Rent((int)length);
		}
	}

	/// <summary>Begins sampling. The first sample is taken before this returns, so the figure is never below the starting point.</summary>
	internal static PeakWorkingSetSampler Start()
	{
		PeakWorkingSetSampler sampler = new();

		sampler.Record();
		sampler._thread.Start();

		return sampler;
	}

	/// <summary>Stops sampling and returns the largest working set read, in bytes.</summary>
	internal long StopAndReadPeakInBytes()
	{
		_stopRequested = true;
		_thread.Join();
		Record();

		return Interlocked.Read(ref _peakBytes);
	}

	private void SampleUntilStopped()
	{
		while (!_stopRequested)
		{
			Record();
			Thread.Sleep(_SAMPLE_INTERVAL_IN_MILLISECONDS);
		}
	}

	private void Record()
	{
		using Process current = Process.GetCurrentProcess();

		long workingSetBytes = current.WorkingSet64;
		long knownPeakBytes = Interlocked.Read(ref _peakBytes);

		while (workingSetBytes > knownPeakBytes)
		{
			long observedBytes = Interlocked.CompareExchange(ref _peakBytes, workingSetBytes, knownPeakBytes);

			knownPeakBytes = observedBytes == knownPeakBytes ? workingSetBytes : observedBytes;
		}
	}

	[DllImport("psapi.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EmptyWorkingSet(IntPtr processHandle);
}
