using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE CONSOLE NEVER PRINTS AN ABSOLUTE PATH, IN ANY BRANCH. No path the harness knows (the repository, the destination,
/// the folder of fakes, the temporary root, a launch folder) appears in any scenario's transcript, and nothing in a
/// transcript has the shape of a drive path, a share path or a rooted path. The folders are named only by the destination as
/// the user supplied it.
/// </summary>
public sealed class AiAssistConsoleNoAbsolutePathTests
{
	private static readonly Regex _driveOrSharePath = new(@"[A-Za-z]:[\\/]|\\\\", RegexOptions.CultureInvariant);

	private static readonly Regex _rootedPath = new(@"(?<![.\w)])/[A-Za-z0-9_.-]", RegexOptions.CultureInvariant);

	[Fact]
	public void NoKnownAbsolutePathAppearsInAnyBranch()
	{
		foreach (ScenarioResult scenario in AiAssistScenarios.All)
		{
			foreach (string path in scenario.AbsolutePaths.Where(candidate => candidate.Length > 0))
			{
				Assert.DoesNotContain(path, scenario.Transcript, StringComparison.OrdinalIgnoreCase);
			}
		}
	}

	[Fact]
	public void NothingInAnyBranchHasTheShapeOfAnAbsolutePath()
	{
		foreach (ScenarioResult scenario in AiAssistScenarios.All)
		{
			Assert.False(_driveOrSharePath.IsMatch(scenario.Transcript), $"{scenario.Name}: {_driveOrSharePath.Match(scenario.Transcript).Value}");
			Assert.False(_rootedPath.IsMatch(scenario.Transcript), $"{scenario.Name}: {_rootedPath.Match(scenario.Transcript).Value}");
		}
	}

	[Fact]
	public void TheLaunchFolderIsNamedByTheDestinationAsSuppliedAndTheFolderName()
	{
		ScenarioResult launched = AiAssistScenarios.All.Single(scenario => scenario.Name == "exit zero");

		Assert.Matches(@"the launch folder \./Outputs/ai-assist-launch-[0-9a-f]{32} holds a copy", launched.Transcript);
	}
}
