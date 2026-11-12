using System.Text.Json;

namespace MigrateAuth.FakeAgent;

/// <summary>
/// What a fake agent does, written by the test beside the executable as <c>&lt;executable&gt;.behavior</c>.
/// The behavior travels in a file, not in the environment or the arguments, because both of those are
/// exactly what the fake records and a test must be able to assert on them untouched.
/// </summary>
public sealed record FakeAgentBehavior
{
	/// <summary>The suffix of the behavior file.</summary>
	public const string FILE_SUFFIX = ".behavior";

	private const int _DEFAULT_LIFETIME_SECONDS = 60;

	/// <summary>How the fake ends.</summary>
	public FakeAgentEnding Ending { get; init; } = FakeAgentEnding.Exit;

	/// <summary>The exit code of <see cref="FakeAgentEnding.Exit"/>, and of a hang that reaches its lifetime.</summary>
	public int ExitCode { get; init; }

	/// <summary>The signal number of <see cref="FakeAgentEnding.Signal"/>.</summary>
	public int SignalNumber { get; init; }

	/// <summary>The maximum lifetime of <see cref="FakeAgentEnding.Hang"/>, after which the fake ends itself.</summary>
	public int HangLifetimeSeconds { get; init; } = _DEFAULT_LIFETIME_SECONDS;

	/// <summary>Where the fake writes its record. Empty means <c>&lt;executable&gt;.record</c>.</summary>
	public string RecordPath { get; init; } = string.Empty;

	/// <summary>A path whose existence the fake records (the prompt pack). Empty records nothing.</summary>
	public string PackPath { get; init; } = string.Empty;

	/// <summary>Environment variable names whose presence and value the fake records.</summary>
	public string[] WatchedEnvironmentNames { get; init; } = [];

	/// <summary>Writes this behavior as the behavior file of the executable at <paramref name="executablePath"/>.</summary>
	public void WriteFor(string executablePath)
	{
		File.WriteAllText($"{executablePath}{FILE_SUFFIX}", JsonSerializer.Serialize(this));
	}

	/// <summary>Reads the behavior of the executable, or the default (exit 0) when it has none.</summary>
	public static FakeAgentBehavior ReadFor(string executablePath)
	{
		string behaviorPath = $"{executablePath}{FILE_SUFFIX}";
		FakeAgentBehavior? behavior = File.Exists(behaviorPath)
			? JsonSerializer.Deserialize<FakeAgentBehavior>(File.ReadAllText(behaviorPath))
			: null;

		return behavior ?? new FakeAgentBehavior();
	}
}
