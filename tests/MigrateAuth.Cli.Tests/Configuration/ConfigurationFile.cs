using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Tests.Session;

namespace MigrateAuth.Cli.Tests.Configuration;

/// <summary>
/// Writes a real <c>.migrate-auth.json</c> into a real folder. The resolver reads files, and the
/// invariant under test is about what a repository can put on disk, so the tests put it there
/// rather than injecting a document.
/// </summary>
internal static class ConfigurationFile
{
	/// <summary>Writes the configuration file of a folder with the given contents.</summary>
	internal static void WriteInto(TemporaryFolder folder, string contents)
	{
		File.WriteAllText(folder.Combine(ConfigurationResolver.CONFIGURATION_FILE_NAME), contents);
	}

	/// <summary>The path of a folder's configuration file, written or not.</summary>
	internal static string PathIn(TemporaryFolder folder) =>
		folder.Combine(ConfigurationResolver.CONFIGURATION_FILE_NAME);
}
