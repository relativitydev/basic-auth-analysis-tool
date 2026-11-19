using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The resolution set a test supplies: exactly the entries it was given, and a count of how many times
/// it was read. It is the only search path any test hands a resolver.
/// </summary>
internal sealed class TestAgentSearchPathProvider : IAgentSearchPathProvider
{
	private readonly List<string> _entries;

	internal TestAgentSearchPathProvider(IEnumerable<string> entries)
	{
		_entries = [.. entries];
	}

	/// <summary>How many times the entries were read.</summary>
	internal int ReadCount { get; private set; }

	/// <summary>The entries as they are now, without counting as a read.</summary>
	internal IReadOnlyList<string> PeekEntries()
	{
		return [.. _entries];
	}

	/// <summary>Removes every entry, to prove a later read would find nothing.</summary>
	internal void ClearEntries()
	{
		_entries.Clear();
	}

	public IReadOnlyList<string> GetSearchPathEntries()
	{
		ReadCount++;

		return [.. _entries];
	}
}

/// <summary>
/// THE GUARD IN THE TEST RESOLVER. Every resolution a test makes goes through
/// <see cref="Resolve"/>, which fails the test when the command was found anywhere other than in the
/// test's own temporary folder. A command that is not found passes the guard; a command found anywhere
/// else, whatever the search path was, never does.
/// </summary>
internal static class TestAgentResolver
{
	private const string _OUTSIDE_TEMPORARY_FOLDER_MESSAGE =
		"A resolution found a command outside the test's temporary folder: ";

	/// <summary>Resolves <paramref name="commandName"/> from exactly <paramref name="entries"/>, under the guard.</summary>
	internal static AgentResolution Resolve(
		IEnumerable<string> entries,
		string repositoryFolderPath,
		string commandName)
	{
		TestAgentSearchPathProvider provider = new(entries);

		return Resolve(provider, repositoryFolderPath, commandName);
	}

	/// <summary>Resolves from a provider the test keeps hold of, under the guard.</summary>
	internal static AgentResolution Resolve(
		TestAgentSearchPathProvider provider,
		string repositoryFolderPath,
		string commandName)
	{
		AgentExecutableResolver resolver = new(
			provider,
			RepositoryFolderPath.FromSuppliedText(repositoryFolderPath),
			commandName);
		AgentResolution resolution = resolver.Resolve();

		RequireInsideTestTemporaryFolder(resolution);

		return resolution;
	}

	/// <summary>Fails when a found command is not inside the platform's temporary folder.</summary>
	internal static void RequireInsideTestTemporaryFolder(AgentResolution resolution)
	{
		bool allowed = !resolution.WasFound
			|| WriteBoundary.ComparePaths(TemporaryFolder.CanonicalRoot, resolution.ExecutablePath)
				is PathOverlap.DestinationInsideRepository;

		Assert.True(allowed, $"{_OUTSIDE_TEMPORARY_FOLDER_MESSAGE}{resolution.ExecutablePath}");
	}

	/// <summary>
	/// A small stand-in command file for resolution tests, which look at names, folders and the execute bit and
	/// never run the file. On Linux it is given the execute bit when <paramref name="executable"/> is true.
	/// </summary>
	internal static string WriteCommandFile(string folderPath, string fileName, bool executable = true)
	{
		string path = Path.Combine(folderPath, fileName);

		File.WriteAllText(path, "stand-in");

		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(
				path,
				executable
					? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
					: UnixFileMode.UserRead | UnixFileMode.UserWrite);
		}

		return path;
	}

	/// <summary>
	/// Creates a directory link through the framework call only. Where Windows refuses the call for lack of
	/// the symbolic-link privilege, it returns false and the test that asked is a declared no-op there; no
	/// process is started to make a link.
	/// </summary>
	internal static bool TryCreateDirectoryLink(string linkPath, string targetPath)
	{
		bool created;

		try
		{
			Directory.CreateSymbolicLink(linkPath, targetPath);
			created = true;
		}
		catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
		{
			created = false;
		}
		catch (IOException) when (OperatingSystem.IsWindows())
		{
			created = false;
		}

		return created;
	}
}
