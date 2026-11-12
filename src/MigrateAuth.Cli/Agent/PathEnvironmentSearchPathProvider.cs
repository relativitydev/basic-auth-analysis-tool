namespace MigrateAuth.Cli.Agent;

/// <summary>
/// The shipped resolution set: the process's own search path variable, split into its entries.
///
/// THIS IS THE ONE PLACE under the agent folder that reads the environment. Every other class receives
/// its resolution set through <see cref="IAgentSearchPathProvider"/>, so a source test can hold the
/// whole folder to that rule.
/// </summary>
public sealed class PathEnvironmentSearchPathProvider : IAgentSearchPathProvider
{
	private const string _PATH_VARIABLE_NAME = "PATH";

	/// <inheritdoc />
	public IReadOnlyList<string> GetSearchPathEntries()
	{
		string? declaredPath = Environment.GetEnvironmentVariable(_PATH_VARIABLE_NAME);

		return string.IsNullOrEmpty(declaredPath)
			? []
			: declaredPath.Split(Path.PathSeparator);
	}
}
