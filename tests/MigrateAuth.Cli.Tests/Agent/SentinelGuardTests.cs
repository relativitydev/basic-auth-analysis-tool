using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE SENTINEL GUARD. Every agent test command puts the sentinel folder - claude,
/// codex and copilot, each a sentinel build of the fake agent - first on PATH, so a test that resolved an
/// agent through the real PATH would invoke a sentinel instead of a real agent. An invocation leaves a
/// flag beside the sentinel; this class fails when the folder is not first on PATH, when it does not hold
/// all three names, or when any flag exists.
/// </summary>
public sealed class SentinelGuardTests
{
	private const string _PATH_VARIABLE = "PATH";
	private const string _WINDOWS_EXECUTABLE_SUFFIX = ".exe";
	private static readonly string[] _agentNames = ["claude", "codex", "copilot"];

	[Fact]
	public void TheSentinelFolderIsFirstOnPathAndHoldsAllThreeNames()
	{
		string firstEntry = FirstPathEntry();
		string[] missing = _agentNames
			.Where(name => !File.Exists(SentinelMarkerPath(firstEntry, name)))
			.ToArray();

		Assert.True(
			missing.Length == 0,
			$"The first PATH entry is not the sentinel folder (build it with tests/FakeAgent/make-sentinels.py and put it first on PATH). Missing a sentinel marker for: {string.Join(", ", missing)}");
	}

	[Fact]
	public void NoSentinelHasBeenInvoked()
	{
		string[] flags = Directory
			.EnumerateFiles(FirstPathEntry(), $"*{SentinelMode.FLAG_SUFFIX}")
			.ToArray();

		Assert.True(
			flags.Length == 0,
			$"A sentinel agent was invoked; stop the run and look into it: {string.Join(", ", flags)}");
	}

	private static string FirstPathEntry()
	{
		string path = Environment.GetEnvironmentVariable(_PATH_VARIABLE) ?? string.Empty;
		string firstEntry = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

		Assert.True(Directory.Exists(firstEntry), "PATH has no usable first entry; the sentinel folder must be first on PATH.");

		return firstEntry;
	}

	private static string SentinelMarkerPath(string folder, string name)
	{
		string executableName = OperatingSystem.IsWindows() ? $"{name}{_WINDOWS_EXECUTABLE_SUFFIX}" : name;

		return Path.Combine(folder, $"{executableName}{SentinelMode.MARKER_SUFFIX}");
	}
}
