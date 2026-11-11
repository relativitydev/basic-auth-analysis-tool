using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// WHAT A CANCELLATION OF THE STAGE'S TOKEN DOES, AND WHEN IT DOES NOTHING. Before the launch the token is checked at three
/// decision points only (after the selection, before the pack is written, before the confirmation is read); once the
/// confirmation was read the child owns the terminal, and a cancellation during or after the launch changes neither the
/// output that follows nor the flow.
/// </summary>
public sealed class AiAssistCancellationTests
{
	private const int _CONFIRMATION_READ_INDEX = 1;

	[Fact]
	public void ACancellationBeforeTheConfirmationReadPrintsTheInterruptedLineAndOffersNothingFurther()
	{
		using AiAssistHarness harness = new(["1", "y"]);
		using CancellationTokenSource cancellation = new();

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Output.When(AiAssistTexts.EntryCountLine(PromptPackFixture.Scope()), cancellation.Cancel);

		StageRunOutcome outcome = harness.Run(cancellation.Token);

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.EndsWith($"{AiAssistTexts.INTERRUPTED}{Environment.NewLine}", harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.Confirmation(AgentEnvironment.ClaudeCode), harness.Transcript, StringComparison.Ordinal);
		Assert.Equal(1, harness.Input.ReadCount);
		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
		Assert.Contains("a supplemental section naming ai-assist-claude-code.md was added", harness.Transcript, StringComparison.Ordinal);
		harness.AssertNothingWasLaunched();
	}

	[Fact]
	public void ACancellationDuringTheLaunchChangesNeitherThePostLaunchOutputNorTheFlow()
	{
		using AiAssistHarness cancelled = new(["1", "y"]);
		using AiAssistHarness uncancelled = new(["1", "y"]);
		using CancellationTokenSource cancellation = new();

		cancelled.InstallFake(AgentEnvironment.ClaudeCode);
		uncancelled.InstallFake(AgentEnvironment.ClaudeCode);
		cancelled.Input.OnRead = index =>
		{
			if (index == _CONFIRMATION_READ_INDEX)
			{
				cancellation.Cancel();
			}
		};

		StageRunOutcome cancelledOutcome = cancelled.Run(cancellation.Token);
		StageRunOutcome uncancelledOutcome = uncancelled.Run();

		Assert.Equal(uncancelledOutcome, cancelledOutcome);
		Assert.Equal(StageRunOutcome.SelfReported, cancelledOutcome);
		Assert.Equal(PostLaunchLines(uncancelled.Transcript), PostLaunchLines(cancelled.Transcript));
		Assert.DoesNotContain(AiAssistTexts.INTERRUPTED, cancelled.Transcript, StringComparison.Ordinal);
		Assert.True(cancelled.FakeWasStarted(AgentEnvironment.ClaudeCode));
	}

	[Fact]
	public void ACancellationAfterTheLaunchHasEndedChangesNothingEither()
	{
		using AiAssistHarness harness = new(["1", "y"]);
		using CancellationTokenSource cancellation = new();

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Output.When("ai-assist: Claude Code ended with exit code 0.", cancellation.Cancel);

		StageRunOutcome outcome = harness.Run(cancellation.Token);

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains("ai-assist: the launch folder ", harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.INTERRUPTED, harness.Transcript, StringComparison.Ordinal);
	}

	private static string PostLaunchLines(string transcript)
	{
		int start = transcript.IndexOf("ai-assist: Claude Code ended", StringComparison.Ordinal);
		string tail = transcript[start..];

		return System.Text.RegularExpressions.Regex.Replace(tail, "ai-assist-launch-[0-9a-f]{32}", "ai-assist-launch-ID");
	}
}
