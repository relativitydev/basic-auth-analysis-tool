using System.Diagnostics;
using System.Text.Json;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE PROCESS HARNESS AND THE USER PROFILE: NO CONFIGURATION FILE ON THE MACHINE RUNNING
/// THE TESTS CAN CHANGE A FLOW.
///
/// The harness points <c>USERPROFILE</c> and <c>HOME</c> at an empty temporary folder for every
/// process it launches. That is what isolates the child on Linux and macOS. On Windows the runtime
/// asks the operating system for the profile folder and ignores the variables, so the isolation
/// there is proved in-process, by the resolver test that passes the profile folder as an argument;
/// this file proves what the harness does, and does not claim more.
/// </summary>
public sealed class ProfileIsolationTests
{
	private const string _REPOSITORY_FILE_NAME = "Credentials.cs";
	private const string _PROVENANCE_FIELD = "provenance";
	private const string _DEFAULT_FLOW_ORIGIN_FIELD = "defaultFlowOrigin";
	private const string _BUILT_IN_ORIGIN = "BuiltInDefault";

	private const string _REPOSITORY_CONTENT =
		"class Anchored\n{\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	[Fact]
	public void TheHarnessPointsBothProfileVariablesAtTheEmptyFolderItIsGiven()
	{
		using TemporaryFolder emptyFolder = TemporaryFolder.Create();
		ProcessStartInfo startInfo = new();

		CliProcessHarness.IsolateProfile(startInfo, emptyFolder.Path);

		Assert.Equal(emptyFolder.Path, startInfo.Environment[CliProcessHarness.USER_PROFILE_VARIABLE_NAME]);
		Assert.Equal(emptyFolder.Path, startInfo.Environment[CliProcessHarness.HOME_VARIABLE_NAME]);
		Assert.True(Directory.Exists(emptyFolder.Path));
		Assert.Empty(Directory.EnumerateFileSystemEntries(emptyFolder.Path));
	}

	[Fact]
	public void EveryProcessRunIsAimedAtAnEmptyTemporaryFolderAndNotAtTheRealProfile()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_REPOSITORY_FILE_NAME, _REPOSITORY_CONTENT);

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			CliProcessHarness.TheFourStagesThenExit);

		string realProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.StartsWith(TemporaryFolder.CanonicalRoot, outcome.IsolatedProfileFolderPath, StringComparison.OrdinalIgnoreCase);
		Assert.NotEqual(realProfileFolder, outcome.IsolatedProfileFolderPath);
	}

	/// <summary>The run's assertion: with no profile, the origin reads BuiltInDefault.</summary>
	[Fact]
	public void AProcessRunWithNoProfileReportsTheBuiltInDefaultFlowOrigin()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_REPOSITORY_FILE_NAME, _REPOSITORY_CONTENT);

		CliProcessHarness.Run(repository.RootPath, destination.Path, CliProcessHarness.TheFourStagesThenExit);

		using JsonDocument artifact = JsonDocument.Parse(
			ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.StructuredDataArtifact));

		Assert.Equal(
			_BUILT_IN_ORIGIN,
			artifact.RootElement.GetProperty(_PROVENANCE_FIELD).GetProperty(_DEFAULT_FLOW_ORIGIN_FIELD).GetString());
	}
}
