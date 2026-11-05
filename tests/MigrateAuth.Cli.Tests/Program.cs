using MigrateAuth.Cli.Tests.AcceptanceHarness;

namespace MigrateAuth.Cli.Tests;

/// <summary>
/// The test assembly's entry point. A test run never calls it: the volume proofs that assert a working set start this assembly
/// as a child process to measure one operation in a process that has done nothing else (<see cref="VolumeProofChildProcess"/>).
/// It replaces the empty entry point the test SDK would otherwise generate.
/// </summary>
internal static class Program
{
	internal static int Main(string[] args)
	{
		return VolumeProofChildProcess.RunRequestedOperation(args);
	}
}
