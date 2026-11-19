namespace MigrateAuth.Cli.Agent;

/// <summary>
/// What resolving an agent command found, and the facts about the search that the caller needs to say
/// something useful when nothing was found.
///
/// The absolute path in here is working data for the launch. It is never emitted: the console shows the
/// file name and the position of the entry only.
/// </summary>
public sealed record AgentResolution
{
	/// <summary>The outcome when no entry held a usable command outside the repository.</summary>
	public static AgentResolution None { get; } = new()
	{
		WasFound = false,
		ExecutablePath = string.Empty,
		IsWrapperScript = false,
		EntryPosition = 0,
		EntryCount = 0,
		MatchInsideRepositoryWasIgnored = false
	};

	/// <summary>Whether a launchable command was found outside the repository.</summary>
	public required bool WasFound { get; init; }

	/// <summary>The exact absolute path the launch uses. Empty when nothing was found.</summary>
	public required string ExecutablePath { get; init; }

	/// <summary>Whether the command is a Windows wrapper script and not a program.</summary>
	public required bool IsWrapperScript { get; init; }

	/// <summary>The one-based position of the entry the command was found in, or zero.</summary>
	public required int EntryPosition { get; init; }

	/// <summary>How many entries the search path had, empty ones included.</summary>
	public required int EntryCount { get; init; }

	/// <summary>
	/// Whether a command of that name was seen inside the repository and skipped. It lets the not-found
	/// message say so, because a repository must never decide which program gets started.
	/// </summary>
	public required bool MatchInsideRepositoryWasIgnored { get; init; }

	/// <summary>The file name of the resolved command, which is the only form fit to appear in output.</summary>
	public string ExecutableFileName => Path.GetFileName(ExecutablePath);
}
