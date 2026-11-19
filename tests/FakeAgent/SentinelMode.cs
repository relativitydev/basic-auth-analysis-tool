using System.Text;

namespace MigrateAuth.FakeAgent;

/// <summary>
/// THE SENTINEL: a fake that must never be invoked. The sentinel folder holds one of these under each
/// agent name and is first on the PATH of every test command, so a test that resolved an agent
/// through the real PATH would reach it. An invocation writes a flag beside the executable and exits
/// <see cref="EXIT_CODE"/>; the sentinel guard test fails on any flag.
/// </summary>
public static class SentinelMode
{
	/// <summary>The suffix of the marker file that turns an executable into a sentinel.</summary>
	public const string MARKER_SUFFIX = ".sentinel";

	/// <summary>The suffix of the flag file an invocation leaves beside the executable.</summary>
	public const string FLAG_SUFFIX = ".invoked";

	/// <summary>The exit code of a sentinel invocation.</summary>
	public const int EXIT_CODE = 97;

	internal static int Run(string executablePath, string[] arguments)
	{
		StringBuilder flag = new();

		flag.AppendLine("sentinel invoked");
		flag.AppendLine($"working directory: {Directory.GetCurrentDirectory()}");
		flag.AppendLine($"argument count: {arguments.Length}");
		File.WriteAllText($"{executablePath}{FLAG_SUFFIX}", flag.ToString());
		Console.Error.WriteLine("A sentinel agent was invoked; the test run must stop.");

		return EXIT_CODE;
	}
}
