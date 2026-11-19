using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// RESOLVES ONE AGENT COMMAND FROM THE INJECTED SEARCH PATH, ONCE.
///
/// Entries are taken in order and one directory at a time. An entry that is empty, is <c>.</c>, or is
/// not an absolute path is skipped, so the current directory can never decide which program starts. On
/// Windows a directory is tried for the command with <c>.exe</c> and then with <c>.cmd</c>, and for
/// nothing else: not <c>.ps1</c>, not <c>.bat</c>, not <c>.com</c>, and the executable-extension
/// variable is not read. On Linux a candidate is a regular file with an execute bit.
///
/// Each candidate is canonicalized with its links resolved, with the same primitive the write boundary
/// uses, and a candidate that lands inside the repository is skipped, with the fact remembered. A
/// repository that puts a program on the search path, directly or through a link, is not an agent.
///
/// THE ANSWER IS COMPUTED ONCE AND KEPT. The availability check and the launch read the same value, so
/// the launch starts exactly the path the check reported and the search path is read a single time.
/// </summary>
public sealed class AgentExecutableResolver
{
	private const string _CURRENT_DIRECTORY_ENTRY = ".";
	private const string _DIRECT_EXECUTABLE_SUFFIX = ".exe";
	private const string _WRAPPER_SCRIPT_SUFFIX = ".cmd";
	private const UnixFileMode _ANY_EXECUTE_BIT =
		UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

	private readonly IAgentSearchPathProvider _searchPathProvider;
	private readonly RepositoryFolderPath _repository;
	private readonly string _commandName;
	private AgentResolution? _resolution;

	/// <summary>Creates the resolver for one command name: the name of an agent's program, without a suffix.</summary>
	public AgentExecutableResolver(
		IAgentSearchPathProvider searchPathProvider,
		RepositoryFolderPath repository,
		string commandName)
	{
		ArgumentNullException.ThrowIfNull(searchPathProvider);
		ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

		_searchPathProvider = searchPathProvider;
		_repository = repository;
		_commandName = commandName;
	}

	/// <summary>The resolution, computed on the first call and returned unchanged afterwards.</summary>
	public AgentResolution Resolve()
	{
		_resolution ??= ResolveFromSearchPath();

		return _resolution;
	}

	private AgentResolution ResolveFromSearchPath()
	{
		IReadOnlyList<string> entries = _searchPathProvider.GetSearchPathEntries();
		bool ignoredMatchInsideRepository = false;
		AgentResolution? found = null;

		for (int index = 0; index < entries.Count && found is null; index++)
		{
			if (IsAbsoluteEntry(entries[index]))
			{
				found = TryResolveInDirectory(entries[index], index + 1, entries.Count, ref ignoredMatchInsideRepository);
			}
		}

		return found ?? AgentResolution.None with
		{
			EntryCount = entries.Count,
			MatchInsideRepositoryWasIgnored = ignoredMatchInsideRepository
		};
	}

	private static bool IsAbsoluteEntry(string entry)
	{
		return !string.IsNullOrWhiteSpace(entry)
			&& entry != _CURRENT_DIRECTORY_ENTRY
			&& Path.IsPathFullyQualified(entry);
	}

	private AgentResolution? TryResolveInDirectory(
		string directory,
		int entryPosition,
		int entryCount,
		ref bool ignoredMatchInsideRepository)
	{
		AgentResolution? found = null;

		foreach (string candidateName in CandidateFileNames())
		{
			string candidatePath = Path.Combine(directory, candidateName);

			if (found is null && IsUsableCommandFile(candidatePath))
			{
				string? canonicalPath = CanonicalizeOutsideRepository(candidatePath, ref ignoredMatchInsideRepository);

				found = canonicalPath is null
					? null
					: new AgentResolution
					{
						WasFound = true,
						ExecutablePath = canonicalPath,
						IsWrapperScript = candidateName.EndsWith(_WRAPPER_SCRIPT_SUFFIX, StringComparison.Ordinal),
						EntryPosition = entryPosition,
						EntryCount = entryCount,
						MatchInsideRepositoryWasIgnored = ignoredMatchInsideRepository
					};
			}
		}

		return found;
	}

	/// <summary>The file names tried in one directory, in the order they are tried.</summary>
	private string[] CandidateFileNames()
	{
		return OperatingSystem.IsWindows()
			? [$"{_commandName}{_DIRECT_EXECUTABLE_SUFFIX}", $"{_commandName}{_WRAPPER_SCRIPT_SUFFIX}"]
			: [_commandName];
	}

	private static bool IsUsableCommandFile(string candidatePath)
	{
		bool usable;

		try
		{
			usable = File.Exists(candidatePath)
				&& (OperatingSystem.IsWindows() || (File.GetUnixFileMode(candidatePath) & _ANY_EXECUTE_BIT) != 0);
		}
		catch (ArgumentException)
		{
			usable = false;
		}
		catch (IOException)
		{
			usable = false;
		}
		catch (UnauthorizedAccessException)
		{
			usable = false;
		}
		catch (NotSupportedException)
		{
			usable = false;
		}

		return usable;
	}

	/// <summary>
	/// The candidate's canonical form, or null when it lands inside the repository (which is then
	/// remembered) or cannot be canonicalized.
	/// </summary>
	private string? CanonicalizeOutsideRepository(string candidatePath, ref bool ignoredMatchInsideRepository)
	{
		string? canonicalPath;

		try
		{
			string resolvedPath = CanonicalPath.Resolve(candidatePath);
			bool insideRepository = WriteBoundary.ComparePaths(_repository.CanonicalPath, resolvedPath)
				!= PathOverlap.Separate;

			ignoredMatchInsideRepository |= insideRepository;
			canonicalPath = insideRepository ? null : resolvedPath;
		}
		catch (ArgumentException)
		{
			canonicalPath = null;
		}
		catch (IOException)
		{
			canonicalPath = null;
		}
		catch (NotSupportedException)
		{
			canonicalPath = null;
		}

		return canonicalPath;
	}
}
