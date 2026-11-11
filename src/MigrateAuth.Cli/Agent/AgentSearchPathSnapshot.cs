namespace MigrateAuth.Cli.Agent;

/// <summary>
/// The injected resolution set, read from its source once and kept. The resolver and the child's
/// environment both take their entries from here, so the search path is read a single time per launcher
/// however often it is asked, and the child can find exactly what the resolver could.
/// </summary>
internal sealed class AgentSearchPathSnapshot(IAgentSearchPathProvider source) : IAgentSearchPathProvider
{
	private IReadOnlyList<string>? _entries;

	/// <inheritdoc />
	public IReadOnlyList<string> GetSearchPathEntries()
	{
		_entries ??= source.GetSearchPathEntries();

		return _entries;
	}
}
