namespace MigrateAuth.Cli.Agent;

/// <summary>How a resolved agent would be started.</summary>
public enum AgentStartKind
{
	/// <summary>The agent is a program and is started directly.</summary>
	Direct = 0,

	/// <summary>The agent is a script wrapper and is started through the Windows command interpreter.</summary>
	ThroughCommandInterpreter = 1
}

/// <summary>
/// WHAT THE CONSOLE MAY SAY ABOUT THE RESOLUTION A LAUNCHER MADE: the file name, how it would be started and where in
/// the search path it was found. It carries no absolute path: that stays inside the launcher, which starts the agent
/// from the very resolution this describes.
/// </summary>
/// <param name="FileName">The file name of the resolved command.</param>
/// <param name="StartKind">Directly or through the command interpreter.</param>
/// <param name="EntryPosition">The one-based position of the search-path entry the command was found in.</param>
/// <param name="EntryCount">How many entries the search path had.</param>
public sealed record AgentResolutionDescription(string FileName, AgentStartKind StartKind, int EntryPosition, int EntryCount);
