using System.Collections.Immutable;
using System.Diagnostics;

using MigrateAuth.Cli.Tests.Session;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE CHILD'S SEARCH PATH, REPLACED RATHER THAN INHERITED.
///
/// A session that is started by a test must never be able to find an AI agent that happens to be
/// installed on the machine running the tests, so the child's <c>PATH</c> is built here instead of
/// inherited: the folder a test names (or an empty folder of its own) followed by the minimum the
/// platform needs. On Windows that minimum is the system directory. On Linux it is a temporary folder
/// of links to <c>dotnet</c>, <c>sh</c> and <c>script</c> and never a system binary folder, because a
/// distribution package can put an agent there.
///
/// The provider credential variables a real agent would read are removed from the child as well, so
/// that nothing the developer's shell holds can reach a process a test starts.
///
/// The replacement is on for every run. It is not an option a test has to remember to ask for, because
/// a new test that reaches the launch would otherwise be unprotected until someone noticed.
/// </summary>
internal sealed class ChildPathReplacement : IDisposable
{
	/// <summary>The name of the search path variable.</summary>
	internal const string PATH_VARIABLE_NAME = "PATH";

	private const string _DOTNET_LINK_NAME = "dotnet";
	private const string _SHELL_LINK_NAME = "sh";
	private const string _SCRIPT_LINK_NAME = "script";

	/// <summary>
	/// The provider credential variables removed from every child. A fixed list, so that adding a name is a
	/// visible change in a review.
	/// </summary>
	internal static readonly ImmutableArray<string> ProviderCredentialVariableNames =
	[
		"ANTHROPIC_API_KEY",
		"ANTHROPIC_AUTH_TOKEN",
		"CLAUDE_CODE_OAUTH_TOKEN",
		"OPENAI_API_KEY",
		"CODEX_API_KEY",
		"GITHUB_TOKEN",
		"GH_TOKEN",
		"COPILOT_GITHUB_TOKEN"
	];

	private readonly TemporaryFolder? _emptyAgentFolder;
	private readonly TemporaryFolder? _linuxSystemFolder;

	private ChildPathReplacement(
		string agentFolderPath,
		string systemPartPath,
		TemporaryFolder? emptyAgentFolder,
		TemporaryFolder? linuxSystemFolder)
	{
		AgentFolderPath = agentFolderPath;
		SystemPartPath = systemPartPath;
		_emptyAgentFolder = emptyAgentFolder;
		_linuxSystemFolder = linuxSystemFolder;
	}

	/// <summary>The first entry of the child's search path: the caller's fake folder, or an empty one.</summary>
	internal string AgentFolderPath { get; }

	/// <summary>The second entry: the Windows system directory, or the Linux folder of links.</summary>
	internal string SystemPartPath { get; }

	/// <summary>The search path the child receives.</summary>
	internal string ChildPath => $"{AgentFolderPath}{Path.PathSeparator}{SystemPartPath}";

	/// <summary>
	/// Replaces the search path of <paramref name="startInfo"/> and removes the credential variables, and
	/// returns the holder of the temporary folders that were made, which the caller disposes after the run.
	/// </summary>
	internal static ChildPathReplacement Apply(ProcessStartInfo startInfo, string? agentFolderPath)
	{
		TemporaryFolder? emptyAgentFolder = agentFolderPath is null ? TemporaryFolder.Create() : null;
		TemporaryFolder? linuxSystemFolder = OperatingSystem.IsWindows() ? null : CreateLinuxSystemFolder();
		string systemPart = linuxSystemFolder?.Path ?? Environment.SystemDirectory;

		ChildPathReplacement replacement = new(
			agentFolderPath ?? emptyAgentFolder!.Path,
			systemPart,
			emptyAgentFolder,
			linuxSystemFolder);

		startInfo.Environment[PATH_VARIABLE_NAME] = replacement.ChildPath;

		foreach (string credentialName in ProviderCredentialVariableNames)
		{
			startInfo.Environment.Remove(credentialName);
		}

		return replacement;
	}

	public void Dispose()
	{
		_emptyAgentFolder?.Dispose();
		_linuxSystemFolder?.Dispose();
	}

	/// <summary>
	/// A temporary folder holding links to the host, <c>sh</c> and <c>script</c> only. The latter two are
	/// found on the inherited search path by name, which is safe because neither is an agent; the links
	/// live in the temporary folder, so the child never sees the folders they came from.
	/// </summary>
	private static TemporaryFolder CreateLinuxSystemFolder()
	{
		TemporaryFolder folder = TemporaryFolder.Create();

		File.CreateSymbolicLink(folder.Combine(_DOTNET_LINK_NAME), CliProcessHarness.HostPath);
		LinkFromInheritedPath(folder, _SHELL_LINK_NAME);
		LinkFromInheritedPath(folder, _SCRIPT_LINK_NAME);

		return folder;
	}

	private static void LinkFromInheritedPath(TemporaryFolder folder, string commandName)
	{
		string inherited = Environment.GetEnvironmentVariable(PATH_VARIABLE_NAME) ?? string.Empty;
		string? found = inherited
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(entry => Path.Combine(entry, commandName))
			.FirstOrDefault(File.Exists);

		if (found is not null)
		{
			File.CreateSymbolicLink(folder.Combine(commandName), found);
		}
	}
}
