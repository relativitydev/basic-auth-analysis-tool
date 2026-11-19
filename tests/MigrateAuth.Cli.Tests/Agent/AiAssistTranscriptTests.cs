using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Tests.AcceptanceHarness;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE TRANSCRIPT OF EVERY BRANCH THAT REACHES A TERMINAL, IN PROCESS, with the terminal predicate forced true and the fakes
/// of the launcher tests as real child processes (a stub launcher where a platform or a clock cannot give the ending). In
/// every branch the stage reads exactly the lines it announced by a prompt: the selection, its one re-ask, and at most one
/// confirmation, which is the last read there is. Nothing is read while the child runs or after it ends, nothing a user
/// typed is echoed, and no prompt other than those two kinds is ever printed.
/// </summary>
public sealed class AiAssistTranscriptTests
{
	private const string _HOSTILE_ANSWER = "HOSTILE-ANSWER-7c1e";
	private const int _FAKE_EXIT_CODE = 5;
	private const int _SIGNAL_NUMBER = 9;

	private sealed record Branch(string Name, string[] Inputs, bool ReachesConfirmation, Action<AiAssistHarness> Arrange);

	private static readonly Branch[] _branches =
	[
		new("cancelled by an empty line", [string.Empty], false, harness => { }),
		new("cancelled after two invalid answers", [_HOSTILE_ANSWER, _HOSTILE_ANSWER], false, harness => { }),
		new("cancelled at the end of input", [], false, harness => { }),
		new("not found", ["1"], false, harness => { }),
		new("match only inside the repository", ["1"], false, PlaceMatchInsideTheRepository),
		new("withheld, unsafe command line", ["1"], false, harness => Withhold(harness, AgentLaunchEligibility.UnsafeCommandLine)),
		new("withheld, control character", ["1"], false, harness => Withhold(harness, AgentLaunchEligibility.PathContainsControlCharacter)),
		new("withheld, wrapper needs a local repository", ["1"], false, harness => Withhold(harness, AgentLaunchEligibility.WrapperNeedsLocalRepository)),
		new("re-ask then declined by default", [_HOSTILE_ANSWER, "1", string.Empty], true, harness => harness.InstallFake(AgentEnvironment.ClaudeCode)),
		new("declined", ["2", "n"], true, harness => harness.InstallFake(AgentEnvironment.OpenAiCodex)),
		new("answer that is not a yes", ["3", _HOSTILE_ANSWER], true, harness => harness.InstallFake(AgentEnvironment.GitHubCopilotCli)),
		new("end of input at the confirmation", ["1"], true, harness => harness.InstallFake(AgentEnvironment.ClaudeCode)),
		new("confirmed, exit zero", ["1", "y"], true, harness => harness.InstallFake(AgentEnvironment.ClaudeCode)),
		new("confirmed, non-zero exit", ["2", "yes"], true, harness => harness.InstallFake(AgentEnvironment.OpenAiCodex, new FakeAgentBehavior { ExitCode = _FAKE_EXIT_CODE })),
		new("confirmed, interrupted", ["3", "y"], true, harness => harness.InstallFake(AgentEnvironment.GitHubCopilotCli, new FakeAgentBehavior { Ending = FakeAgentEnding.Interrupt })),
		new("confirmed, signal", ["1", "y"], true, harness => LaunchWithStatus(harness, AgentTerminationStatus.TerminatedBySignal(_SIGNAL_NUMBER))),
		new("confirmed, timed out", ["1", "y"], true, harness => LaunchWithStatus(harness, AgentTerminationStatus.TimedOut())),
		new("confirmed, could not be started", ["1", "y"], true, harness => LaunchWithStatus(harness, AgentTerminationStatus.NotFound()))
	];

	public static IEnumerable<object[]> BranchNames => _branches.Select(branch => new object[] { branch.Name });

	[Theory]
	[MemberData(nameof(BranchNames))]
	public void EveryBranchReadsOnlyWhatItAnnouncedByAPromptAndEchoesNothingTheUserTyped(string branchName)
	{
		Branch branch = _branches.Single(candidate => candidate.Name == branchName);
		using AiAssistHarness harness = new(branch.Inputs);

		branch.Arrange(harness);
		harness.Run();

		string transcript = harness.Transcript;
		int selectionPrompts = AiAssistProcessSupport.CountOccurrences(transcript, AiAssistTexts.SELECTION_PROMPT);
		int reasks = AiAssistProcessSupport.CountOccurrences(transcript, AiAssistTexts.SELECTION_REASK);
		int confirmations = AgentEnvironmentCatalog.All.Sum(descriptor => AiAssistProcessSupport.CountOccurrences(transcript, AiAssistTexts.Confirmation(descriptor.Environment)));

		Assert.Equal(1, selectionPrompts);
		Assert.Equal(branch.ReachesConfirmation ? 1 : 0, confirmations);
		Assert.Equal(selectionPrompts + reasks + confirmations, harness.Input.ReadCount);
		Assert.DoesNotContain(_HOSTILE_ANSWER, transcript, StringComparison.Ordinal);
		Assert.True(reasks <= 1, "The selection is re-asked at most once.");

		if (branch.ReachesConfirmation)
		{
			string lastRead = harness.Input.OutputBeforeEachRead[^1];

			Assert.Contains(AgentEnvironmentCatalog.All, descriptor => lastRead.EndsWith(AiAssistTexts.Confirmation(descriptor.Environment), StringComparison.Ordinal));
		}
	}

	private static void PlaceMatchInsideTheRepository(AiAssistHarness harness)
	{
		string insideRepository = Path.Combine(harness.Repository.Path, AiAssistHarness.REPOSITORY_TOOLS_FOLDER);

		Directory.CreateDirectory(insideRepository);
		TestAgentResolver.WriteCommandFile(insideRepository, FakeAgentFolder.ExecutableName(AgentEnvironmentCatalog.Describe(AgentEnvironment.ClaudeCode).CommandName));
		harness.SearchPath = new TestAgentSearchPathProvider([insideRepository]);
	}

	private static void Withhold(AiAssistHarness harness, AgentLaunchEligibility eligibility)
	{
		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.LauncherOverride = new StubLauncher(AgentAvailability.Available, eligibility, AgentTerminationStatus.NotFound());
	}

	private static void LaunchWithStatus(AiAssistHarness harness, AgentTerminationStatus status)
	{
		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.LauncherOverride = new StubLauncher(AgentAvailability.Available, AgentLaunchEligibility.Launchable, status);
	}
}
