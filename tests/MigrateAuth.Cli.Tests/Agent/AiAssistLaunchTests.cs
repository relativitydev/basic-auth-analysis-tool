using System.Globalization;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE LAUNCH BRANCHES, IN PROCESS: the terminal predicate is true, the search path is the folder of fakes, and each fake
/// is started as a real child process. Confirming launches the fake once with the documented argument count and the
/// repository as working directory; every way the agent can end prints its own message and leaves the report, the pack
/// and the session state as they were; and an agent that is gone between the check and the launch is "could not be started".
/// </summary>
public sealed class AiAssistLaunchTests
{
	private const int _FAKE_EXIT_CODE = 7;
	private const string _DESCRIBED_FILE_NAME = "described-agent.cmd";
	private const int _DESCRIBED_POSITION = 4;
	private const int _DESCRIBED_COUNT = 9;
	private const int _FAKE_SIGNAL_NUMBER = 15;
	private const int _TEST_BUDGET_SECONDS = 3;
	private const int _CONFIRMATION_READ_INDEX = 1;
	private const string _AI_ASSIST_COMMAND = "ai-assist";
	private const string _EXIT_COMMAND = "exit";
	private const int _FAKE_LIFETIME_SECONDS = 30;

	public static TheoryData<AgentEnvironment, int> ArgumentCounts => new()
	{
		{ AgentEnvironment.ClaudeCode, 1 },
		{ AgentEnvironment.OpenAiCodex, 1 },
		{ AgentEnvironment.GitHubCopilotCli, 2 }
	};

	[Theory]
	[MemberData(nameof(ArgumentCounts))]
	public void ConfirmingLaunchesTheFakeOnceWithTheDocumentedArgumentCountAndTheRepositoryAsWorkingDirectory(
		AgentEnvironment environment,
		int expectedArgumentCount)
	{
		string selection = AgentEnvironmentCatalog.Describe(environment).SelectionNumber.ToString(CultureInfo.InvariantCulture);

		using AiAssistHarness harness = new([selection, "y"]);

		harness.InstallFake(environment);

		StageRunOutcome outcome = harness.Run();
		FakeAgentRecord record = harness.RecordOf(environment);

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Equal(expectedArgumentCount, record.Arguments.Length);
		Assert.StartsWith(ExternalAgentLauncherBase.LAUNCH_SENTENCE, record.Arguments[^1], StringComparison.Ordinal);
		Assert.True(SamePath(harness.Repository.Path, record.WorkingDirectory));
		Assert.Single(Directory.GetFiles(harness.Fakes.FolderPath, $"*{FakeAgentRecord.FILE_SUFFIX}"));
		Assert.Equal(2, harness.Input.ReadCount);
	}

	[Theory]
	[InlineData("y")]
	[InlineData("Y")]
	[InlineData("yes")]
	[InlineData("YES")]
	[InlineData("  yes  ")]
	public void OnlyYOrYesConfirmsWhateverTheCase(string answer)
	{
		using AiAssistHarness harness = new(["1", answer]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.True(harness.FakeWasStarted(AgentEnvironment.ClaudeCode));
	}

	[Theory]
	[InlineData("")]
	[InlineData("n")]
	[InlineData("no")]
	[InlineData("yes please")]
	[InlineData("ye")]
	[InlineData("\u001b[2Jyes")]
	public void AnythingElseDeclinesAndTheAnswerIsNeverEchoed(string answer)
	{
		using AiAssistHarness harness = new(["1", answer]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.Contains(AiAssistTexts.LAUNCH_DECLINED, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain("yes please", harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistHarness.ESCAPE_CHARACTER, harness.Transcript);
		harness.AssertNothingWasLaunched();
	}

	[Fact]
	public void TheEndOfInputAtTheConfirmationDeclines()
	{
		using AiAssistHarness harness = new(["1"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.Contains(AiAssistTexts.LAUNCH_DECLINED, harness.Transcript, StringComparison.Ordinal);
		Assert.Equal(2, harness.Input.ReadCount);
		harness.AssertNothingWasLaunched();
	}

	[Fact]
	public void TheResolutionLineNamesTheFileTheEntryAndHowItWouldBeStartedAndNoPath()
	{
		using AiAssistHarness harness = new(["1", "n"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		string file = FakeAgentFolder.ExecutableName(AiAssistHarness.ClaudeCommandName);
		string expected = $"ai-assist: found {file} in entry 1 of 1 on the PATH; it would be started directly.";

		Assert.Contains(expected, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains($"ai-assist: looking for {AiAssistHarness.ClaudeCommandName} on PATH...", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>The search path is read once for the whole offered launch: availability, eligibility, the line, the launch and the child's own path.</summary>
	[Fact]
	public void TheSearchPathIsReadOnceForAnOfferedLaunchAndTheAgentStarts()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.Equal(1, harness.SearchPath.ReadCount);
		Assert.True(harness.FakeWasStarted(AgentEnvironment.ClaudeCode));
	}

	/// <summary>The resolution line is printed from what the launcher says it resolved, not from a resolution of the stage's own.</summary>
	[Fact]
	public void TheResolutionLineComesFromTheLaunchersOwnResolution()
	{
		using AiAssistHarness harness = new(["1", "n"]);
		AgentResolutionDescription described = new(_DESCRIBED_FILE_NAME, AgentStartKind.ThroughCommandInterpreter, _DESCRIBED_POSITION, _DESCRIBED_COUNT);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.LauncherOverride = new StubLauncher(
			AgentAvailability.Available,
			AgentLaunchEligibility.Launchable,
			AgentTerminationStatus.NotFound(),
			described);
		harness.Run();

		Assert.Contains(
			$"ai-assist: found {_DESCRIBED_FILE_NAME} in entry {_DESCRIBED_POSITION} of {_DESCRIBED_COUNT} on the PATH; it would be started through the Windows command interpreter (cmd.exe).",
			harness.Transcript,
			StringComparison.Ordinal);
		Assert.Equal(0, harness.SearchPath.ReadCount);
	}

	[Fact]
	public void TheEntryCountLineIsPrintedBeforeTheQuestion()
	{
		using AiAssistHarness harness = new(["1", "n"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.Contains(
			"ai-assist: the pack lists 6 entries: 3 ambiguous cases, 1 finding whose flow was defaulted, 2 others for confirmation.",
			harness.Input.OutputBeforeEachRead[_CONFIRMATION_READ_INDEX],
			StringComparison.Ordinal);
	}

	[Fact]
	public void ExitZeroPrintsTheEndedNormallyMessageAndTheSuffix()
	{
		AssertTermination(
			new FakeAgentBehavior(),
			"ai-assist: Claude Code ended with exit code 0. This tool did not change the report or the pack.");
	}

	[Fact]
	public void ANonZeroExitPrintsTheCodeTheFallbackSentenceAndTheSuffix()
	{
		AssertTermination(
			new FakeAgentBehavior { ExitCode = _FAKE_EXIT_CODE },
			$"ai-assist: Claude Code ended with exit code 7. The prompt pack is ready and can be given to {AiAssistHarness.ClaudeCommandName} by hand. This tool did not change the report or the pack.");
	}

	[Fact]
	public void AnInterruptionPrintsTheInterruptedMessageAndTheSuffix()
	{
		AssertTermination(
			new FakeAgentBehavior { Ending = FakeAgentEnding.Interrupt },
			"ai-assist: Claude Code was interrupted. This tool did not change the report or the pack.");
	}

	/// <summary>A signal ends the real fake only where the platform has signals; elsewhere the stub below drives the same line.</summary>
	[Fact]
	public void ASignalEndPrintsTheSignalMessageAndTheSuffix()
	{
		if (!OperatingSystem.IsWindows())
		{
			AssertTermination(
				new FakeAgentBehavior { Ending = FakeAgentEnding.Signal, SignalNumber = _FAKE_SIGNAL_NUMBER },
				"ai-assist: Claude Code was ended by signal 15. This tool did not change the report or the pack.");
		}
	}

	[Fact]
	public void ASignalStatusFromAnyLauncherPrintsTheSignalMessage()
	{
		StubLauncher launcher = new(
			AgentAvailability.Available,
			AgentLaunchEligibility.Launchable,
			AgentTerminationStatus.TerminatedBySignal(_FAKE_SIGNAL_NUMBER));

		AssertEndedLeavingEverythingAsItWas(
			harness =>
			{
				harness.InstallFake(AgentEnvironment.ClaudeCode);
				harness.LauncherOverride = launcher;
			},
			"ai-assist: Claude Code was ended by signal 15. This tool did not change the report or the pack.");

		Assert.Equal(1, launcher.LaunchCount);
	}

	/// <summary>The test budget is a launcher property: the agent outlives it, the tree is killed and the message says so.</summary>
	[Fact]
	public void AnAgentThatOutlivesTheTestBudgetIsStoppedAndTheMessageSaysSo()
	{
		AssertEndedLeavingEverythingAsItWas(
			harness =>
			{
				harness.TestBudget = TimeSpan.FromSeconds(_TEST_BUDGET_SECONDS);
				harness.InstallFake(AgentEnvironment.ClaudeCode, new FakeAgentBehavior { Ending = FakeAgentEnding.Hang, HangLifetimeSeconds = _FAKE_LIFETIME_SECONDS });
			},
			"ai-assist: Claude Code did not end within the time allowed and was stopped. This tool did not change the report or the pack.");
	}

	[Fact]
	public void AnAgentThatIsGoneBetweenTheCheckAndTheLaunchCouldNotBeStarted()
	{
		using AiAssistHarness harness = new(["1", "y"]);
		string executable = harness.InstallFake(AgentEnvironment.ClaudeCode);

		harness.Input.OnRead = index =>
		{
			if (index == _CONFIRMATION_READ_INDEX)
			{
				File.Delete(executable);
			}
		};

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains(
			"ai-assist: Claude Code could not be started. Nothing was launched. This tool did not change the report or the pack.",
			harness.Transcript,
			StringComparison.Ordinal);
		Assert.Single(harness.LaunchFolders());
		Assert.False(harness.FakeWasStarted(AgentEnvironment.ClaudeCode));
	}

	[Fact]
	public void TheLaunchFolderLineFollowsTheTerminationMessage()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		int termination = harness.Transcript.IndexOf("ai-assist: Claude Code ended with exit code 0.", StringComparison.Ordinal);
		int folder = harness.Transcript.IndexOf("ai-assist: the launch folder ", StringComparison.Ordinal);

		Assert.True(termination >= 0 && folder > termination);
	}

	private static void AssertTermination(FakeAgentBehavior behavior, string expectedMessage)
	{
		AssertEndedLeavingEverythingAsItWas(harness => harness.InstallFake(AgentEnvironment.ClaudeCode, behavior), expectedMessage);
	}

	/// <summary>
	/// Runs the stage through the shell as a session does, after the four stages, and asserts that however the agent ended
	/// the message is printed, the report and the pack are byte-identical to what they were when the question was asked, no
	/// session state was written and the session ends with the exit code it would have had.
	/// </summary>
	private static void AssertEndedLeavingEverythingAsItWas(Action<AiAssistHarness> arrange, string expectedMessage)
	{
		using AiAssistHarness harness = new([_AI_ASSIST_COMMAND, "1", "y", _EXIT_COMMAND]);
		SessionSnapshot? atConfirmation = null;

		harness.CompleteTheFourStagesAndSaveTheState();
		arrange(harness);
		harness.Input.OnRead = index =>
		{
			if (index == AiAssistHarness.SHELL_CONFIRMATION_READ_INDEX)
			{
				atConfirmation = harness.TakeSnapshot();
			}
		};

		ExitCode exitCode = harness.RunThroughTheShell();

		Assert.Contains(expectedMessage, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains("ai-assist: the launch folder ", harness.Transcript, StringComparison.Ordinal);
		Assert.NotNull(atConfirmation);
		harness.AssertNothingChangedSince(atConfirmation, exitCode);
	}

	private static bool SamePath(string expected, string actual)
	{
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

		return string.Equals(
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(expected)),
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(actual)),
			comparison);
	}
}
