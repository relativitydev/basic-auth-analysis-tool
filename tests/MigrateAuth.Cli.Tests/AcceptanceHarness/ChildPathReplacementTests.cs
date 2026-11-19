using System.Diagnostics;

using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// The harness replaces the child's search path and removes the provider credential variables. These tests
/// look at the start information the harness builds and start nothing.
/// </summary>
public sealed class ChildPathReplacementTests
{
	private const string _INHERITED_SECRET_VALUE = "invented-value";
	private const string _UNRELATED_VARIABLE_NAME = "CHILD_PATH_REPLACEMENT_TEST_UNRELATED";
	private const string _UNRELATED_VARIABLE_VALUE = "kept";
	private const int _EXPECTED_ENTRY_COUNT = 2;

	[Fact]
	public void TheChildSearchPathIsTheGivenFolderThenTheSystemPartAndNothingInherited()
	{
		using TemporaryFolder agentFolder = TemporaryFolder.Create();
		ProcessStartInfo startInfo = new();
		startInfo.Environment[ChildPathReplacement.PATH_VARIABLE_NAME] = "inherited-entry";

		using ChildPathReplacement replacement = ChildPathReplacement.Apply(startInfo, agentFolder.Path);
		string[] entries = startInfo.Environment[ChildPathReplacement.PATH_VARIABLE_NAME]!.Split(Path.PathSeparator);

		Assert.Equal(_EXPECTED_ENTRY_COUNT, entries.Length);
		Assert.Equal(agentFolder.Path, entries[0]);
		Assert.Equal(replacement.SystemPartPath, entries[1]);
		Assert.DoesNotContain("inherited-entry", startInfo.Environment[ChildPathReplacement.PATH_VARIABLE_NAME], StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutAFolderTheFirstEntryIsAnEmptyTemporaryFolderThatIsRemovedAfterwards()
	{
		ProcessStartInfo startInfo = new();
		string emptyFolderPath;

		using (ChildPathReplacement replacement = ChildPathReplacement.Apply(startInfo, agentFolderPath: null))
		{
			emptyFolderPath = replacement.AgentFolderPath;

			Assert.True(Directory.Exists(emptyFolderPath));
			Assert.Empty(Directory.EnumerateFileSystemEntries(emptyFolderPath));
		}

		Assert.False(Directory.Exists(emptyFolderPath));
	}

	[Fact]
	public void TheSystemPartOnWindowsIsTheSystemDirectory()
	{
		if (OperatingSystem.IsWindows())
		{
			using ChildPathReplacement replacement = ChildPathReplacement.Apply(new ProcessStartInfo(), agentFolderPath: null);

			Assert.Equal(Environment.SystemDirectory, replacement.SystemPartPath);
		}
	}

	[Fact]
	public void EveryProviderCredentialVariableIsRemovedAndOthersAreKept()
	{
		ProcessStartInfo startInfo = new();

		foreach (string credentialName in ChildPathReplacement.ProviderCredentialVariableNames)
		{
			startInfo.Environment[credentialName] = _INHERITED_SECRET_VALUE;
		}

		startInfo.Environment[_UNRELATED_VARIABLE_NAME] = _UNRELATED_VARIABLE_VALUE;

		using ChildPathReplacement replacement = ChildPathReplacement.Apply(startInfo, agentFolderPath: null);

		Assert.All(ChildPathReplacement.ProviderCredentialVariableNames, name => Assert.False(startInfo.Environment.ContainsKey(name)));
		Assert.Equal(_UNRELATED_VARIABLE_VALUE, startInfo.Environment[_UNRELATED_VARIABLE_NAME]);
	}
}
