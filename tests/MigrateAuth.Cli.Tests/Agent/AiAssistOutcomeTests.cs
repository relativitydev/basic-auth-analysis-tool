using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// EVERY PATH OF THE STAGE ENDS WITH THE SELF-REPORTED OUTCOME, after its own line, and the shell adds none of its own:
/// cancelled selection, interrupted, generation failure, declined, no terminal, not found, a launch that ended every way,
/// and a launch that could not start. Driven through the shell, no completion, failure or cancellation line appears.
/// </summary>
public sealed class AiAssistOutcomeTests
{
	private const string _AI_ASSIST_COMMAND = "ai-assist";
	private const int _FAKE_EXIT_CODE = 3;

	public static TheoryData<string> Scenarios => new()
	{
		"cancelled selection",
		"interrupted",
		"generation failure",
		"no terminal",
		"not found",
		"declined",
		"launched, exit zero",
		"launched, exit non-zero",
		"launched, interrupted by the user",
		"could not be started"
	};

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void EveryPathReturnsSelfReportedAndTheShellAddsNoLineOfItsOwn(string scenario)
	{
		using AiAssistHarness harness = Build(scenario, out CancellationTokenSource? cancellation);
		using (cancellation)
		{
			StageRunOutcome outcome = harness.Run(cancellation?.Token ?? CancellationToken.None);

			Assert.Equal(StageRunOutcome.SelfReported, outcome);
		}

		Assert.Contains(AiAssistTexts.CONSOLE_LINE_PREFIX, harness.Transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void DrivenThroughTheShellNoCompletionFailureOrCancellationLineIsAdded()
	{
		using AiAssistHarness harness = new([_AI_ASSIST_COMMAND, "1", "y", "exit"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode, new FakeAgentBehavior { ExitCode = _FAKE_EXIT_CODE });

		SessionSnapshot? atConfirmation = null;

		harness.CompleteTheFourStagesAndSaveTheState();
		harness.Input.OnRead = index =>
		{
			if (index == AiAssistHarness.SHELL_CONFIRMATION_READ_INDEX)
			{
				atConfirmation = harness.TakeSnapshot();
			}
		};

		ExitCode exitCode = harness.RunThroughTheShell();

		Assert.NotNull(atConfirmation);
		harness.AssertNothingChangedSince(atConfirmation, exitCode);

		foreach (string format in (string[])[SessionShell.STAGE_COMPLETED_MESSAGE_FORMAT, SessionShell.STAGE_FAILED_MESSAGE_FORMAT, SessionShell.STAGE_CANCELLED_MESSAGE_FORMAT])
		{
			Assert.DoesNotContain(
				string.Format(System.Globalization.CultureInfo.InvariantCulture, format, _AI_ASSIST_COMMAND),
				harness.Transcript,
				StringComparison.Ordinal);
		}

		Assert.Contains("ended with exit code 3", harness.Transcript, StringComparison.Ordinal);
		Assert.False(harness.State.IsCompleted(CommandId.AiAssist));
	}

	[Fact]
	public void AnyOtherCommandIsAProgrammingErrorAndRunsNothing()
	{
		using AiAssistHarness harness = new(["1"]);

		Assert.Throws<ArgumentOutOfRangeException>(() => harness.Runner.Run(CommandId.Report, harness.State, CancellationToken.None));
		Assert.Empty(harness.PackFiles());
	}

	private static AiAssistHarness Build(string scenario, out CancellationTokenSource? cancellation)
	{
		AiAssistHarness harness = new(scenario switch
		{
			"cancelled selection" => [string.Empty],
			"declined" => ["1", "n"],
			_ => ["1", "y"]
		});

		cancellation = null;

		if (scenario == "interrupted")
		{
			cancellation = new CancellationTokenSource();
			cancellation.Cancel();
		}

		Arrange(harness, scenario);

		return harness;
	}

	private static void Arrange(AiAssistHarness harness, string scenario)
	{
		switch (scenario)
		{
			case "generation failure":
				harness.IsTerminal = false;
				harness.ReplaceDestinationWithFile();
				break;

			case "no terminal":
				harness.IsTerminal = false;
				harness.InstallFake(AgentEnvironment.ClaudeCode);
				break;

			case "launched, exit non-zero":
				harness.InstallFake(AgentEnvironment.ClaudeCode, new FakeAgentBehavior { ExitCode = _FAKE_EXIT_CODE });
				break;

			case "launched, interrupted by the user":
				harness.InstallFake(AgentEnvironment.ClaudeCode, new FakeAgentBehavior { Ending = FakeAgentEnding.Interrupt });
				break;

			case "could not be started":
				string executable = harness.InstallFake(AgentEnvironment.ClaudeCode);

				harness.Input.OnRead = index => DeleteAtConfirmation(index, executable);
				break;

			case "not found" or "cancelled selection":
				break;

			default:
				harness.InstallFake(AgentEnvironment.ClaudeCode);
				break;
		}
	}

	private static void DeleteAtConfirmation(int readIndex, string executable)
	{
		if (readIndex == AiAssistHarness.CONFIRMATION_READ_INDEX)
		{
			File.Delete(executable);
		}
	}
}
