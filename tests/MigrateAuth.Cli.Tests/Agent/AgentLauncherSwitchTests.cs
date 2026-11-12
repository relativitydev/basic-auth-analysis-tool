using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Documentation;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// One launcher per environment, chosen by an exhaustive switch over the enumeration, every implementation a
/// sealed class named statically. Nothing is created by reflection, which would not survive ahead-of-time
/// compilation, and each launcher looks for its own command and for no other.
/// </summary>
public sealed class AgentLauncherSwitchTests
{
	private const string _SWITCH_SOURCE_FILE = "AgentLauncherSwitch.cs";
	private const int _UNDEFINED_ENVIRONMENT = 99;

	private static readonly string[] _reflectionTokens =
		["System.Reflection", "Activator", "CreateInstance", "GetType(", "MakeGenericType", "Assembly."];

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode, typeof(ClaudeCodeLauncher))]
	[InlineData(AgentEnvironment.OpenAiCodex, typeof(OpenAiCodexLauncher))]
	[InlineData(AgentEnvironment.GitHubCopilotCli, typeof(GitHubCopilotCliLauncher))]
	public void EachEnvironmentSelectsItsOwnSealedLauncher(AgentEnvironment environment, Type expectedType)
	{
		using LauncherScenario scenario = new();

		IExternalAgentLauncher launcher = scenario.CreateLauncher(environment);

		Assert.Same(expectedType, launcher.GetType());
		Assert.True(expectedType.IsSealed);
		Assert.True(launcher is ExternalAgentLauncherBase);
	}

	[Fact]
	public void EveryEnvironmentOfTheEnumerationIsCoveredAndTheLaunchersAreDistinct()
	{
		using LauncherScenario scenario = new();
		Type[] types =
		[
			.. Enum.GetValues<AgentEnvironment>().Select(environment => scenario.CreateLauncher(environment).GetType())
		];

		Assert.Equal(Enum.GetValues<AgentEnvironment>().Length, types.Distinct().Count());
	}

	[Fact]
	public void AnUndefinedEnvironmentIsAProgrammingError()
	{
		using LauncherScenario scenario = new();

		Assert.Throws<ArgumentOutOfRangeException>(() => AgentLauncherSwitch.Create(
			(AgentEnvironment)_UNDEFINED_ENVIRONMENT,
			new TestAgentSearchPathProvider([scenario.Fakes.FolderPath]),
			scenario.Repository,
			testBudget: null));
	}

	[Theory]
	[InlineData(AgentEnvironment.ClaudeCode, "claude")]
	[InlineData(AgentEnvironment.OpenAiCodex, "codex")]
	[InlineData(AgentEnvironment.GitHubCopilotCli, "copilot")]
	public void EachLauncherFindsItsOwnCommandAndNoOther(AgentEnvironment environment, string commandName)
	{
		using LauncherScenario scenario = new();

		scenario.Fakes.Install(commandName, new FakeAgentBehavior());

		foreach (AgentEnvironment candidate in Enum.GetValues<AgentEnvironment>())
		{
			AgentAvailability expected = candidate == environment ? AgentAvailability.Available : AgentAvailability.NotFound;

			Assert.Equal(expected, scenario.CreateLauncher(candidate).CheckAvailability());
		}
	}

	[Fact]
	public void AMatchOnlyInsideTheRepositoryIsReportedAsIgnored()
	{
		using LauncherScenario scenario = new();
		string repositoryBin = Directory.CreateDirectory(Path.Combine(scenario.RepositoryPath, "bin")).FullName;

		TestAgentResolver.WriteCommandFile(repositoryBin, FakeAgentFolder.ExecutableName("claude"));
		TestAgentSearchPathProvider provider = new([repositoryBin]);
		IExternalAgentLauncher launcher = AgentLauncherSwitch.Create(
			AgentEnvironment.ClaudeCode,
			provider,
			scenario.Repository,
			testBudget: null);

		Assert.Equal(AgentAvailability.NotFoundMatchInsideRepositoryIgnored, launcher.CheckAvailability());
	}

	[Fact]
	public void TheSwitchSourceNamesNoReflection()
	{
		string source = File.ReadAllText(Path.Combine(
			PublicDocuments.RepositoryRoot(),
			"src",
			"MigrateAuth.Cli",
			"Agent",
			_SWITCH_SOURCE_FILE));

		Assert.All(_reflectionTokens, token => Assert.DoesNotContain(token, source, StringComparison.Ordinal));
		Assert.Contains("environment switch", source, StringComparison.Ordinal);
	}

	[Fact]
	public void TheLauncherSourcesNameNoReflectionEither()
	{
		string agentFolder = Path.Combine(PublicDocuments.RepositoryRoot(), "src", "MigrateAuth.Cli", "Agent");
		string[] launcherFiles = [.. Directory.EnumerateFiles(agentFolder, "*Launcher*.cs")];

		Assert.NotEmpty(launcherFiles);
		Assert.All(
			launcherFiles,
			file => Assert.All(
				_reflectionTokens,
				token => Assert.DoesNotContain(token, File.ReadAllText(file), StringComparison.Ordinal)));
	}
}
