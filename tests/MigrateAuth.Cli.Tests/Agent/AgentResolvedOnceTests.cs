using System.Diagnostics;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The resolution happens once, and the launch uses exactly the path it found: the search path is read a
/// single time however often the answer is asked for, a later change to the search path or to the folders
/// does not change the answer, and a fake started from the resolved path is the file that was resolved.
/// </summary>
public sealed class AgentResolvedOnceTests
{
	private const string _COMMAND_NAME = "claude";
	private const int _PROCESS_SAFETY_MILLISECONDS = 60000;
	private const int _ASKED_REPEATEDLY = 5;

	[Fact]
	public void TheSearchPathIsReadOnceHoweverOftenTheAnswerIsAskedFor()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder folder = TemporaryFolder.Create();
		TestAgentResolver.WriteCommandFile(folder.Path, FakeAgentFolder.ExecutableName(_COMMAND_NAME));
		TestAgentSearchPathProvider provider = new([folder.Path]);
		AgentExecutableResolver resolver = new(provider, RepositoryFolderPath.FromSuppliedText(repository.Path), _COMMAND_NAME);

		for (int asked = 0; asked < _ASKED_REPEATEDLY; asked++)
		{
			resolver.Resolve();
		}

		Assert.Equal(1, provider.ReadCount);
	}

	[Fact]
	public void ALaterChangeToTheSearchPathOrTheFolderDoesNotChangeTheAnswer()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder folder = TemporaryFolder.Create();
		string commandFile = TestAgentResolver.WriteCommandFile(folder.Path, FakeAgentFolder.ExecutableName(_COMMAND_NAME));
		TestAgentSearchPathProvider provider = new([folder.Path]);
		AgentExecutableResolver resolver = new(provider, RepositoryFolderPath.FromSuppliedText(repository.Path), _COMMAND_NAME);
		AgentResolution first = resolver.Resolve();

		provider.ClearEntries();
		File.Delete(commandFile);
		AgentResolution second = resolver.Resolve();

		Assert.True(first.WasFound);
		Assert.Same(first, second);
		Assert.Equal(first.ExecutablePath, second.ExecutablePath);
	}

	[Fact]
	public void ANotFoundAnswerIsAlsoKept()
	{
		using TemporaryFolder repository = TemporaryFolder.Create();
		using TemporaryFolder folder = TemporaryFolder.Create();
		TestAgentSearchPathProvider provider = new([folder.Path]);
		AgentExecutableResolver resolver = new(provider, RepositoryFolderPath.FromSuppliedText(repository.Path), _COMMAND_NAME);

		Assert.False(resolver.Resolve().WasFound);

		TestAgentResolver.WriteCommandFile(folder.Path, FakeAgentFolder.ExecutableName(_COMMAND_NAME));

		Assert.False(resolver.Resolve().WasFound);
		Assert.Equal(1, provider.ReadCount);
	}

	[Fact]
	public void AFakeStartedFromTheResolvedPathIsTheFileThatWasResolved()
	{
		using FakeAgentFolder fakes = new();
		using TemporaryFolder repository = TemporaryFolder.Create();
		fakes.Install(_COMMAND_NAME, new FakeAgentBehavior());
		AgentResolution resolution = TestAgentResolver.Resolve([fakes.FolderPath], repository.Path, _COMMAND_NAME);
		string recordPath = $"{resolution.ExecutablePath}{FakeAgentRecord.FILE_SUFFIX}";

		using Process process = FakeAgentFolder.Start(resolution.ExecutablePath, [], null, redirectInput: false);

		Assert.True(process.WaitForExit(_PROCESS_SAFETY_MILLISECONDS));
		Assert.True(File.Exists(recordPath));
	}

	[Fact]
	public void TheLaunchStartsExactlyTheFileTheAvailabilityCheckResolved()
	{
		using LauncherScenario scenario = new();
		using TemporaryFolder laterFolder = TemporaryFolder.Create();
		string resolvedFirst = scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior());
		string notResolved = Path.Combine(laterFolder.Path, FakeAgentFolder.ExecutableName(_COMMAND_NAME));

		File.Copy(FakeAgentFolder.PublishedFakePath(), notResolved);
		new FakeAgentBehavior().WriteFor(notResolved);
		TestAgentSearchPathProvider provider = new([scenario.Fakes.FolderPath, laterFolder.Path]);
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode, provider);
		PromptPackFilePath launchCopy = scenario.WriteLaunchCopy();

		Assert.Equal(AgentAvailability.Available, launcher.CheckAvailability());
		Assert.Equal(AgentLaunchEligibility.Launchable, launcher.CheckLaunchable(launchCopy));

		provider.ClearEntries();

		Assert.Equal(AgentTerminationStatus.ExitedWithCode(0), launcher.Launch(launchCopy));
		Assert.True(File.Exists($"{resolvedFirst}{FakeAgentRecord.FILE_SUFFIX}"));
		Assert.False(File.Exists($"{notResolved}{FakeAgentRecord.FILE_SUFFIX}"));
		Assert.Equal(1, provider.ReadCount);
	}

	[Fact]
	public void ALauncherThatFoundNothingStaysNotFoundAndNothingIsLaunchedWhenAnAgentAppearsLater()
	{
		using LauncherScenario scenario = new();
		TestAgentSearchPathProvider provider = new([scenario.Fakes.FolderPath]);
		IExternalAgentLauncher launcher = scenario.CreateLauncher(AgentEnvironment.ClaudeCode, provider);

		Assert.Equal(AgentAvailability.NotFound, launcher.CheckAvailability());

		string appearedLater = scenario.Fakes.Install(_COMMAND_NAME, new FakeAgentBehavior());

		Assert.Equal(AgentTerminationStatus.NotFound(), launcher.Launch(scenario.WriteLaunchCopy()));
		Assert.False(File.Exists($"{appearedLater}{FakeAgentRecord.FILE_SUFFIX}"));
		Assert.Equal(1, provider.ReadCount);
	}
}
