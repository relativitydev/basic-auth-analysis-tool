namespace MigrateAuth.Cli.Agent;

/// <summary>
/// The injected resolution set: the ordered list of directory strings an agent command is looked up in.
///
/// It is a seam so that the lookup never reads the process environment on its own. The shipped value
/// reads the search path variable (<see cref="PathEnvironmentSearchPathProvider"/>); a test supplies
/// the folder of fake agents it made and nothing else, so no test can reach an agent installed on the
/// machine running it.
///
/// Entries are returned exactly as they appeared, in order, empty ones included, because the position of
/// an entry in the list is something the user can compare with their own shell. Deciding which entries
/// may be used is the resolver's job, not the provider's.
/// </summary>
public interface IAgentSearchPathProvider
{
	/// <summary>The directory entries in search order, unfiltered. Read once per resolution.</summary>
	IReadOnlyList<string> GetSearchPathEntries();
}
