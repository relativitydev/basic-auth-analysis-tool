using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.Agent;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE TRANSCRIPT OF EVERY BRANCH A REAL PROCESS CAN REACH, with its input and output redirected: the only prompt the
/// stage ever prints is the selection, never the confirmation, never one that asks for anything else; what a user typed
/// is never echoed; no absolute path of the repository or of the folder the agents were found in is printed; and the
/// session ends with exit code zero in each.
/// </summary>
public sealed class AiAssistTranscriptProcessTests
{
	/// <summary>The redirected branches, by name.</summary>
	public enum Branch
	{
		CancelledByAnEmptyLine,
		CancelledAfterTwoInvalidAnswers,
		CancelledAtTheEndOfInput,
		TheOutOfScopeName,
		ReaskThenSelected,
		GenerateOnlyWithAnAgentFound,
		NotFound,
		MatchInsideTheRepository,
		ResumedSession
	}

	public static IEnumerable<object[]> Branches => Enum.GetValues<Branch>().Select(branch => new object[] { branch });

	[Theory]
	[MemberData(nameof(Branches))]
	public void EveryRedirectedBranchPrintsOnlyTheSelectionPromptAndEchoesNothingTheUserTyped(Branch branch)
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();
		using FakeAgentFolder fakes = new();

		AiAssistProcessSupport.WriteRepository(repository);

		string agentFolder = ArrangeAgentFolder(branch, repository, fakes);
		CliProcessOutcome outcome = RunBranch(branch, repository, destination, agentFolder);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);
		Assert.Equal(0, AiAssistProcessSupport.CountConfirmations(outcome.StandardOutput));
		Assert.Equal(1, AiAssistProcessSupport.CountSelectionPrompts(outcome.StandardOutput));
		Assert.DoesNotContain(AiAssistProcessSupport.HOSTILE_ANSWER, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.False(AiAssistProcessSupport.FakeWasStarted(fakes, AgentEnvironment.ClaudeCode));
		AiAssistProcessSupport.AssertNoAbsolutePath(outcome, repository.RootPath, agentFolder);
	}

	private static string ArrangeAgentFolder(Branch branch, RepositoryFixture repository, FakeAgentFolder fakes)
	{
		string commandName = AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).CommandName;
		string folder = fakes.FolderPath;

		if (branch is Branch.GenerateOnlyWithAnAgentFound)
		{
			fakes.Install(commandName, new FakeAgentBehavior());
		}
		else if (branch is Branch.MatchInsideTheRepository)
		{
			folder = repository.CreateFolder(AiAssistHarness.REPOSITORY_TOOLS_FOLDER);
			TestAgentResolver.WriteCommandFile(folder, FakeAgentFolder.ExecutableName(commandName));
		}

		return folder;
	}

	private static CliProcessOutcome RunBranch(Branch branch, RepositoryFixture repository, TemporaryFolder destination, string agentFolder)
	{
		string stage = AiAssistProcessSupport.AiAssistCommand;
		string exit = AiAssistProcessSupport.ExitCommand;
		string hostile = AiAssistProcessSupport.HOSTILE_ANSWER;
		string claude = AiAssistProcessSupport.SelectionNumberOf(AgentEnvironment.ClaudeCode);

		if (branch is Branch.ResumedSession)
		{
			AiAssistProcessSupport.Run(repository, destination, AiAssistProcessSupport.FourStagesThen(exit));
		}

		string[] lines = branch switch
		{
			Branch.CancelledByAnEmptyLine => AiAssistProcessSupport.FourStagesThen(stage, string.Empty, exit),
			Branch.CancelledAfterTwoInvalidAnswers => AiAssistProcessSupport.FourStagesThen(stage, hostile, hostile, exit),
			Branch.CancelledAtTheEndOfInput => AiAssistProcessSupport.FourStagesThen(stage),
			Branch.TheOutOfScopeName => AiAssistProcessSupport.FourStagesThen(stage, AiAssistTexts.CURSOR_NAME, string.Empty, exit),
			Branch.ReaskThenSelected => AiAssistProcessSupport.FourStagesThen(stage, hostile, claude, exit),
			Branch.ResumedSession => [stage, claude, exit],
			_ => AiAssistProcessSupport.FourStagesThen(stage, claude, exit)
		};

		return AiAssistProcessSupport.Run(repository, destination, lines, agentFolder);
	}
}
