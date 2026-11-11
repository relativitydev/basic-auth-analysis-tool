using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// Resolution of an agent command from the injected search path: what is found, what is skipped, and in
/// which order. Every resolution here goes through the test resolver, whose guard fails any command found
/// outside the temporary folder of the test, and every search path is a list the test built; none is the
/// machine's own.
///
/// Tests that need one platform are written for the platform they run on and do nothing on the other, and
/// the link test does nothing where the operating system refuses to make a link without a privilege.
/// </summary>
public sealed class AgentResolutionTests(ITestOutputHelper output)
{
	private const string _COMMAND_PREFIX = "agent-";
	private const string _GUID_FORMAT = "N";
	private const string _WINDOWS_EXECUTABLE_SUFFIX = ".exe";
	private const string _WINDOWS_WRAPPER_SUFFIX = ".cmd";
	private const string _REPOSITORY_BIN_FOLDER_NAME = "bin";
	private const string _LINK_FOLDER_NAME = "linked";
	private const string _FIRST_FOLDER_NAME = "first";
	private const string _SECOND_FOLDER_NAME = "second";
	private const string _RELATIVE_FOLDER_PREFIX = "relative-";
	private const string _CURRENT_DIRECTORY_ENTRY = ".";
	private const string _PATH_VARIABLE_NAME = "PATH";
	private const int _FIRST_POSITION = 1;
	private const int _SECOND_POSITION = 2;
	private const int _THIRD_POSITION = 3;
	private const string _REAL_FAKE_COMMAND = "claude";
	private const string _LINK_NOT_CREATED_NOTE = "No link was created (the operating system refused it without a privilege); this test did nothing.";

	[Fact]
	public void AFakeOnTheTestSearchPathIsFound()
	{
		using FakeAgentFolder fakes = new();
		using TemporaryFolder repository = TemporaryFolder.Create();
		string installed = fakes.Install(_REAL_FAKE_COMMAND, new FakeAgentBehavior());

		AgentResolution resolution = TestAgentResolver.Resolve([fakes.FolderPath], repository.Path, _REAL_FAKE_COMMAND);

		Assert.True(resolution.WasFound);
		Assert.Equal(Path.GetFileName(installed), resolution.ExecutableFileName);
		Assert.False(resolution.IsWrapperScript);
	}

	[Fact]
	public void NothingOnTheSearchPathIsNotFound()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder empty = TemporaryFolder.Create();

		AgentResolution resolution = TestAgentResolver.Resolve([empty.Path], repository.Path, NewCommandName());

		Assert.False(resolution.WasFound);
		Assert.False(resolution.MatchInsideRepositoryWasIgnored);
		Assert.Equal(1, resolution.EntryCount);
	}

	[Fact]
	public void ACommandReachableOnlyThroughTheCurrentDirectoryIsNotFound()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder empty = TemporaryFolder.Create();
		string command = NewCommandName();
		string stray = TestAgentResolver.WriteCommandFile(Directory.GetCurrentDirectory(), ExecutableName(command));

		try
		{
			AgentResolution resolution = TestAgentResolver.Resolve([empty.Path], repository.Path, command);

			Assert.False(resolution.WasFound);
		}
		finally
		{
			File.Delete(stray);
		}
	}

	[Fact]
	public void ADotEntryIsSkippedEvenWhenTheCurrentDirectoryHoldsTheCommand()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		string command = NewCommandName();
		string stray = TestAgentResolver.WriteCommandFile(Directory.GetCurrentDirectory(), ExecutableName(command));

		try
		{
			AgentResolution resolution = TestAgentResolver.Resolve([_CURRENT_DIRECTORY_ENTRY], repository.Path, command);

			Assert.False(resolution.WasFound);
		}
		finally
		{
			File.Delete(stray);
		}
	}

	[Fact]
	public void AnEmptyEntryIsSkippedEvenWhenTheCurrentDirectoryHoldsTheCommand()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		string command = NewCommandName();
		string stray = TestAgentResolver.WriteCommandFile(Directory.GetCurrentDirectory(), ExecutableName(command));

		try
		{
			AgentResolution resolution = TestAgentResolver.Resolve([string.Empty], repository.Path, command);

			Assert.False(resolution.WasFound);
		}
		finally
		{
			File.Delete(stray);
		}
	}

	[Fact]
	public void ARelativeEntryIsSkippedEvenWhenItNamesAFolderHoldingTheCommand()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		string command = NewCommandName();
		string relativeFolderName = $"{_RELATIVE_FOLDER_PREFIX}{Guid.NewGuid().ToString(_GUID_FORMAT)}";
		string relativeFolderPath = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), relativeFolderName)).FullName;
		TestAgentResolver.WriteCommandFile(relativeFolderPath, ExecutableName(command));

		try
		{
			AgentResolution resolution = TestAgentResolver.Resolve([relativeFolderName], repository.Path, command);

			Assert.False(resolution.WasFound);
		}
		finally
		{
			Directory.Delete(relativeFolderPath, recursive: true);
		}
	}

	[Fact]
	public void AnAbsoluteEntryInsideTheRepositoryIsNotFoundAndTheMatchIsRemembered()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		string command = NewCommandName();
		string insideFolder = Directory.CreateDirectory(Path.Combine(repository.Path, _REPOSITORY_BIN_FOLDER_NAME)).FullName;
		TestAgentResolver.WriteCommandFile(insideFolder, ExecutableName(command));

		AgentResolution resolution = TestAgentResolver.Resolve([insideFolder], repository.Path, command);

		Assert.False(resolution.WasFound);
		Assert.True(resolution.MatchInsideRepositoryWasIgnored);
	}

	[Fact]
	public void ALinkIntoTheRepositoryIsNotFoundAndTheMatchIsRemembered()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder outside = TemporaryFolder.Create();
		string command = NewCommandName();
		string insideFolder = Directory.CreateDirectory(Path.Combine(repository.Path, _REPOSITORY_BIN_FOLDER_NAME)).FullName;
		TestAgentResolver.WriteCommandFile(insideFolder, ExecutableName(command));
		string linkPath = Path.Combine(outside.Path, _LINK_FOLDER_NAME);

		if (TestAgentResolver.TryCreateDirectoryLink(linkPath, insideFolder))
		{
			AgentResolution resolution = TestAgentResolver.Resolve([linkPath], repository.Path, command);

			Assert.False(resolution.WasFound);
			Assert.True(resolution.MatchInsideRepositoryWasIgnored);
		}
		else
		{
			output.WriteLine(_LINK_NOT_CREATED_NOTE);
		}
	}

	[Fact]
	public void AMatchInsideTheRepositoryDoesNotHideALaterMatchOutsideIt()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder outside = TemporaryFolder.Create();
		string command = NewCommandName();
		string insideFolder = Directory.CreateDirectory(Path.Combine(repository.Path, _REPOSITORY_BIN_FOLDER_NAME)).FullName;
		TestAgentResolver.WriteCommandFile(insideFolder, ExecutableName(command));
		TestAgentResolver.WriteCommandFile(outside.Path, ExecutableName(command));

		AgentResolution resolution = TestAgentResolver.Resolve([insideFolder, outside.Path], repository.Path, command);

		Assert.True(resolution.WasFound);
		Assert.True(resolution.MatchInsideRepositoryWasIgnored);
		Assert.Equal(_SECOND_POSITION, resolution.EntryPosition);
	}

	[Fact]
	public void TheEntryPositionAndCountIncludeSkippedEntries()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder outside = TemporaryFolder.Create();
		string command = NewCommandName();
		TestAgentResolver.WriteCommandFile(outside.Path, ExecutableName(command));

		AgentResolution resolution = TestAgentResolver.Resolve(
			[string.Empty, _CURRENT_DIRECTORY_ENTRY, outside.Path],
			repository.Path,
			command);

		Assert.True(resolution.WasFound);
		Assert.Equal(_THIRD_POSITION, resolution.EntryPosition);
		Assert.Equal(_THIRD_POSITION, resolution.EntryCount);
	}

	[Fact]
	public void TheFirstDirectoryHoldingTheCommandWins()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder parent = TemporaryFolder.Create();
		string command = NewCommandName();
		string first = Directory.CreateDirectory(Path.Combine(parent.Path, _FIRST_FOLDER_NAME)).FullName;
		string second = Directory.CreateDirectory(Path.Combine(parent.Path, _SECOND_FOLDER_NAME)).FullName;
		TestAgentResolver.WriteCommandFile(first, ExecutableName(command));
		TestAgentResolver.WriteCommandFile(second, ExecutableName(command));

		AgentResolution resolution = TestAgentResolver.Resolve([second, first], repository.Path, command);

		Assert.Equal(_FIRST_POSITION, resolution.EntryPosition);
		Assert.StartsWith(second, resolution.ExecutablePath, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void AWrapperInAnEarlierDirectoryBeatsAProgramInALaterOne()
	{
		if (OperatingSystem.IsWindows())
		{
			using TemporaryFolder repository = TemporaryFolder.Create();
			using TemporaryFolder parent = TemporaryFolder.Create();
			string command = NewCommandName();
			string first = Directory.CreateDirectory(Path.Combine(parent.Path, _FIRST_FOLDER_NAME)).FullName;
			string second = Directory.CreateDirectory(Path.Combine(parent.Path, _SECOND_FOLDER_NAME)).FullName;
			TestAgentResolver.WriteCommandFile(first, $"{command}{_WINDOWS_WRAPPER_SUFFIX}");
			TestAgentResolver.WriteCommandFile(second, $"{command}{_WINDOWS_EXECUTABLE_SUFFIX}");

			AgentResolution resolution = TestAgentResolver.Resolve([first, second], repository.Path, command);

			Assert.True(resolution.IsWrapperScript);
			Assert.Equal(_FIRST_POSITION, resolution.EntryPosition);
		}
	}

	[Fact]
	public void InOneDirectoryTheProgramIsPreferredToTheWrapper()
	{
		if (OperatingSystem.IsWindows())
		{
			using TemporaryFolder repository = TemporaryFolder.Create();
			using TemporaryFolder folder = TemporaryFolder.Create();
			string command = NewCommandName();
			TestAgentResolver.WriteCommandFile(folder.Path, $"{command}{_WINDOWS_WRAPPER_SUFFIX}");
			TestAgentResolver.WriteCommandFile(folder.Path, $"{command}{_WINDOWS_EXECUTABLE_SUFFIX}");

			AgentResolution resolution = TestAgentResolver.Resolve([folder.Path], repository.Path, command);

			Assert.False(resolution.IsWrapperScript);
			Assert.Equal($"{command}{_WINDOWS_EXECUTABLE_SUFFIX}", resolution.ExecutableFileName, ignoreCase: true);
		}
	}

	[Fact]
	public void AWrapperOnlyCommandIsFoundOnWindows()
	{
		if (OperatingSystem.IsWindows())
		{
			using TemporaryFolder repository = TemporaryFolder.Create();
			using FakeAgentFolder fakes = new();
			fakes.InstallCmdOnly(_REAL_FAKE_COMMAND, new FakeAgentBehavior());

			AgentResolution resolution = TestAgentResolver.Resolve([fakes.FolderPath], repository.Path, _REAL_FAKE_COMMAND);

			Assert.True(resolution.WasFound);
			Assert.True(resolution.IsWrapperScript);
			Assert.Equal($"{_REAL_FAKE_COMMAND}{_WINDOWS_WRAPPER_SUFFIX}", resolution.ExecutableFileName, ignoreCase: true);
		}
	}

	[Theory]
	[InlineData(".ps1")]
	[InlineData(".bat")]
	[InlineData(".com")]
	public void OtherWindowsScriptKindsAreNeverCandidates(string suffix)
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder folder = TemporaryFolder.Create();
		string command = NewCommandName();
		TestAgentResolver.WriteCommandFile(folder.Path, $"{command}{suffix}");

		AgentResolution resolution = TestAgentResolver.Resolve([folder.Path], repository.Path, command);

		Assert.False(resolution.WasFound);
	}

	[Fact]
	public void AFileWithoutTheExecuteBitIsNotFoundOnLinux()
	{
		if (!OperatingSystem.IsWindows())
		{
			using TemporaryFolder repository = TemporaryFolder.Create();
			using TemporaryFolder folder = TemporaryFolder.Create();
			string command = NewCommandName();
			TestAgentResolver.WriteCommandFile(folder.Path, command, executable: false);

			AgentResolution resolution = TestAgentResolver.Resolve([folder.Path], repository.Path, command);

			Assert.False(resolution.WasFound);
		}
	}

	[Fact]
	public void AFileWithTheExecuteBitIsFoundOnLinux()
	{
		if (!OperatingSystem.IsWindows())
		{
			using TemporaryFolder repository = TemporaryFolder.Create();
			using TemporaryFolder folder = TemporaryFolder.Create();
			string command = NewCommandName();
			TestAgentResolver.WriteCommandFile(folder.Path, command);

			AgentResolution resolution = TestAgentResolver.Resolve([folder.Path], repository.Path, command);

			Assert.True(resolution.WasFound);
		}
	}

	[Fact]
	public void AFolderNamedLikeTheCommandIsNotACommand()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder folder = TemporaryFolder.Create();
		string command = NewCommandName();
		Directory.CreateDirectory(Path.Combine(folder.Path, ExecutableName(command)));

		AgentResolution resolution = TestAgentResolver.Resolve([folder.Path], repository.Path, command);

		Assert.False(resolution.WasFound);
	}

	[Fact]
	public void TheShippedProviderReturnsTheSearchPathVariableSplitIntoEntries()
	{
		string declared = Environment.GetEnvironmentVariable(_PATH_VARIABLE_NAME) ?? string.Empty;
		string[] expected = declared.Length == 0 ? [] : declared.Split(Path.PathSeparator);

		Assert.Equal(expected, new PathEnvironmentSearchPathProvider().GetSearchPathEntries());
	}

	[Fact]
	public void TheGuardFailsAResolutionOutsideTheTestTemporaryFolder()
	{
		// The test's own output folder can itself lie under the temporary folder (a build of a copy of the repository can sit there),
		// so the path outside it is taken at the root of the temporary folder's volume, where it never lies inside.
		string volumeRoot = Path.GetPathRoot(TemporaryFolder.CanonicalRoot) ?? Path.DirectorySeparatorChar.ToString();
		string outsideTemporaryFolder = Path.Combine(volumeRoot, NewCommandName(), ExecutableName(NewCommandName()));
		AgentResolution outside = AgentResolution.None with
		{
			WasFound = true,
			ExecutablePath = outsideTemporaryFolder
		};

		Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => TestAgentResolver.RequireInsideTestTemporaryFolder(outside));
	}

	[Fact]
	public void TheGuardPassesAResolutionInsideTheTestTemporaryFolder()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		AgentResolution inside = AgentResolution.None with
		{
			WasFound = true,
			ExecutablePath = folder.Combine(ExecutableName(NewCommandName()))
		};

		TestAgentResolver.RequireInsideTestTemporaryFolder(inside);
	}

	private static string NewCommandName()
	{
		return $"{_COMMAND_PREFIX}{Guid.NewGuid().ToString(_GUID_FORMAT)}";
	}

	private static string ExecutableName(string command)
	{
		return OperatingSystem.IsWindows() ? $"{command}{_WINDOWS_EXECUTABLE_SUFFIX}" : command;
	}
}
