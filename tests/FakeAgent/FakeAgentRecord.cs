using System.Text.Json;

namespace MigrateAuth.FakeAgent;

/// <summary>What a fake agent saw when it was launched, written before it ends.</summary>
public sealed record FakeAgentRecord
{
	/// <summary>The suffix of the default record file.</summary>
	public const string FILE_SUFFIX = ".record";

	/// <summary>The full argument vector, in order.</summary>
	public string[] Arguments { get; init; } = [];

	/// <summary>The working directory.</summary>
	public string WorkingDirectory { get; init; } = string.Empty;

	/// <summary>The path whose existence was asked, empty when none was.</summary>
	public string PackPath { get; init; } = string.Empty;

	/// <summary>Whether <see cref="PackPath"/> existed (file or folder) when the fake started.</summary>
	public bool PackExists { get; init; }

	/// <summary>Each watched variable: its value, or null when it was absent from the environment.</summary>
	public Dictionary<string, string?> WatchedEnvironment { get; init; } = [];

	/// <summary>The names of every variable the process received, sorted, without values.</summary>
	public string[] EnvironmentNames { get; init; } = [];

	/// <summary>The process id.</summary>
	public int ProcessId { get; init; }

	/// <summary>Set by a hang that reached its maximum lifetime and ended itself.</summary>
	public bool EndedByLifetime { get; init; }

	/// <summary>Writes the record to <paramref name="recordPath"/>.</summary>
	public void WriteTo(string recordPath)
	{
		File.WriteAllText(recordPath, JsonSerializer.Serialize(this));
	}

	/// <summary>Reads a record.</summary>
	public static FakeAgentRecord ReadFrom(string recordPath)
	{
		return JsonSerializer.Deserialize<FakeAgentRecord>(File.ReadAllText(recordPath))
			?? throw new InvalidDataException($"The fake agent record is empty: {recordPath}");
	}
}
