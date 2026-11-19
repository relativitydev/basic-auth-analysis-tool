using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// Choosing the environment: a number or an accepted name selects it, the first invalid input is told once, the second
/// cancels, an empty line or the end of input cancels, the out-of-scope editor agent is answered as such, and the
/// text the user typed is never echoed. A cancelled selection writes nothing and leaves both artifacts as they were.
/// </summary>
public sealed class EnvironmentSelectionTests
{
	private const string _HOSTILE_INPUT = "\u001b[2JHOSTILE-MARKER\u001b]0;pwned\u0007";
	private const string _SECOND_HOSTILE_INPUT = "SECOND-HOSTILE-MARKER";
	private const int _TWO_SELECTION_READS = 2;

	public static TheoryData<string, AgentEnvironment> Selections => new()
	{
		{ "1", AgentEnvironment.ClaudeCode },
		{ "2", AgentEnvironment.OpenAiCodex },
		{ "3", AgentEnvironment.GitHubCopilotCli },
		{ " 2 ", AgentEnvironment.OpenAiCodex },
		{ "claude code", AgentEnvironment.ClaudeCode },
		{ "Claude", AgentEnvironment.ClaudeCode },
		{ "CLAUDE CODE", AgentEnvironment.ClaudeCode },
		{ "openai codex", AgentEnvironment.OpenAiCodex },
		{ "Codex", AgentEnvironment.OpenAiCodex },
		{ "github copilot cli", AgentEnvironment.GitHubCopilotCli },
		{ "GitHub Copilot CLI", AgentEnvironment.GitHubCopilotCli },
		{ "copilot", AgentEnvironment.GitHubCopilotCli }
	};

	[Theory]
	[MemberData(nameof(Selections))]
	public void ANumberOrAnAcceptedNameSelectsTheMatchingEnvironment(string input, AgentEnvironment expected)
	{
		using AiAssistHarness harness = new([input]);

		harness.IsTerminal = false;

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Equal([$"{AgentEnvironmentCatalog.Describe(expected).PackFileNameStem}.md"], harness.PackFiles());
		Assert.Equal(1, harness.Input.ReadCount);
		Assert.DoesNotContain(AiAssistTexts.SELECTION_REASK, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, harness.Transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void TheNumberedListAndThePromptArePrintedBeforeTheFirstRead()
	{
		using AiAssistHarness harness = new(["1"]);

		harness.IsTerminal = false;
		harness.Run();

		string beforeFirstRead = harness.Input.OutputBeforeEachRead[0];

		Assert.StartsWith(AiAssistTexts.SELECTION_HEADING, beforeFirstRead, StringComparison.Ordinal);
		Assert.Contains("  1  Claude Code", beforeFirstRead, StringComparison.Ordinal);
		Assert.Contains("  2  OpenAI Codex (or a Codex-style agent)", beforeFirstRead, StringComparison.Ordinal);
		Assert.Contains("  3  GitHub Copilot CLI", beforeFirstRead, StringComparison.Ordinal);
		Assert.EndsWith(AiAssistTexts.SELECTION_PROMPT, beforeFirstRead, StringComparison.Ordinal);
	}

	[Fact]
	public void AFirstInvalidInputPrintsTheReaskOnceAndASecondValidInputSelects()
	{
		using AiAssistHarness harness = new(["nonsense", "3"]);

		harness.IsTerminal = false;
		harness.Run();

		Assert.Equal(1, CountOccurrences(harness.Transcript, AiAssistTexts.SELECTION_REASK));
		Assert.EndsWith($"{AiAssistTexts.SELECTION_REASK}{Environment.NewLine}", harness.Input.OutputBeforeEachRead[1], StringComparison.Ordinal);
		Assert.Equal([$"{AgentEnvironmentCatalog.Describe(AgentEnvironment.GitHubCopilotCli).PackFileNameStem}.md"], harness.PackFiles());
	}

	[Fact]
	public void ASecondInvalidInputCancelsAfterTheReaskWasPrintedOnceAndWritesNothing()
	{
		using AiAssistHarness harness = new(["nonsense", "more nonsense", "1"]);
		byte[] reportBefore = harness.ReportBytes();
		byte[] dataBefore = harness.DataArtifactBytes();

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Equal(_TWO_SELECTION_READS, harness.Input.ReadCount);
		Assert.Equal(1, CountOccurrences(harness.Transcript, AiAssistTexts.SELECTION_REASK));
		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, harness.Transcript, StringComparison.Ordinal);
		Assert.Empty(harness.PackFiles());
		Assert.Equal(reportBefore, harness.ReportBytes());
		Assert.Equal(dataBefore, harness.DataArtifactBytes());
		harness.AssertNothingWasLaunched();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void AnEmptyLineCancelsWithoutTheReaskAndLeavesBothArtifactsAsTheyWere(string input)
	{
		using AiAssistHarness harness = new([input, "1"]);
		byte[] reportBefore = harness.ReportBytes();
		byte[] dataBefore = harness.DataArtifactBytes();

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Equal(1, harness.Input.ReadCount);
		Assert.DoesNotContain(AiAssistTexts.SELECTION_REASK, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, harness.Transcript, StringComparison.Ordinal);
		Assert.Empty(harness.PackFiles());
		Assert.Equal(reportBefore, harness.ReportBytes());
		Assert.Equal(dataBefore, harness.DataArtifactBytes());
	}

	[Fact]
	public void TheEndOfInputCancelsAtTheFirstAndAtTheSecondRead()
	{
		using AiAssistHarness atFirstRead = new([]);
		using AiAssistHarness atSecondRead = new(["nonsense"]);

		Assert.Equal(StageRunOutcome.SelfReported, atFirstRead.Run());
		Assert.Equal(StageRunOutcome.SelfReported, atSecondRead.Run());

		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, atFirstRead.Transcript, StringComparison.Ordinal);
		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, atSecondRead.Transcript, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(atSecondRead.Transcript, AiAssistTexts.SELECTION_REASK));
		Assert.Empty(atFirstRead.PackFiles());
		Assert.Empty(atSecondRead.PackFiles());
	}

	[Theory]
	[InlineData("cursor")]
	[InlineData("Cursor")]
	[InlineData("  CURSOR ")]
	public void CursorIsAnsweredAsOutOfScopeAndCountsAsInvalid(string input)
	{
		using AiAssistHarness harness = new([input, "2"]);

		harness.IsTerminal = false;
		harness.Run();

		Assert.Equal(1, CountOccurrences(harness.Transcript, AiAssistTexts.SELECTION_CURSOR_NOT_SUPPORTED));
		Assert.DoesNotContain(AiAssistTexts.SELECTION_REASK, harness.Transcript, StringComparison.Ordinal);
		Assert.Equal([$"{AgentEnvironmentCatalog.Describe(AgentEnvironment.OpenAiCodex).PackFileNameStem}.md"], harness.PackFiles());
	}

	[Fact]
	public void CursorTwiceIsAnsweredAsOutOfScopeAtEachReadAndThenCancels()
	{
		using AiAssistHarness harness = new(["cursor", "cursor"]);

		harness.Run();

		Assert.Equal(_TWO_SELECTION_READS, CountOccurrences(harness.Transcript, AiAssistTexts.SELECTION_CURSOR_NOT_SUPPORTED));
		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, harness.Transcript, StringComparison.Ordinal);
		Assert.Empty(harness.PackFiles());
	}

	[Fact]
	public void CursorAsTheSecondReadIsAnsweredAsOutOfScopeInsteadOfTheReaskAndCancels()
	{
		using AiAssistHarness harness = new(["nonsense", "cursor", "1"]);
		byte[] reportBefore = harness.ReportBytes();

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Equal(_TWO_SELECTION_READS, harness.Input.ReadCount);
		Assert.Equal(1, CountOccurrences(harness.Transcript, AiAssistTexts.SELECTION_REASK));
		Assert.Equal(1, CountOccurrences(harness.Transcript, AiAssistTexts.SELECTION_CURSOR_NOT_SUPPORTED));
		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, harness.Transcript, StringComparison.Ordinal);
		Assert.Empty(harness.PackFiles());
		Assert.Equal(reportBefore, harness.ReportBytes());
	}

	/// <summary>The runner never echoes the user's input, so no filter has to be remembered: a hostile string never appears.</summary>
	[Fact]
	public void TheUsersTextIsNeverEchoed()
	{
		using AiAssistHarness harness = new([_HOSTILE_INPUT, _SECOND_HOSTILE_INPUT]);

		harness.Run();

		Assert.DoesNotContain("HOSTILE-MARKER", harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(_SECOND_HOSTILE_INPUT, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistHarness.ESCAPE_CHARACTER, harness.Transcript);
		Assert.DoesNotContain(AiAssistHarness.BELL_CHARACTER, harness.Transcript);
	}

	/// <summary>The interrupted line comes from a token that was cancelled at the decision point that follows the selection.</summary>
	[Fact]
	public void ACancelledTokenAfterTheSelectionPrintsTheInterruptedLineAndWritesNothing()
	{
		using AiAssistHarness harness = new(["1"]);
		using CancellationTokenSource cancellation = new();
		byte[] reportBefore = harness.ReportBytes();

		harness.Input.OnRead = _ => cancellation.Cancel();

		StageRunOutcome outcome = harness.Run(cancellation.Token);

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains(AiAssistTexts.INTERRUPTED, harness.Transcript, StringComparison.Ordinal);
		Assert.Empty(harness.PackFiles());
		Assert.Equal(reportBefore, harness.ReportBytes());
		harness.AssertNothingWasLaunched();
	}

	[Fact]
	public void ACancelledTokenWithNoValidSelectionStillSaysThatNoEnvironmentWasChosen()
	{
		using AiAssistHarness harness = new([string.Empty]);
		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();
		harness.Run(cancellation.Token);

		Assert.Contains(AiAssistTexts.NO_ENVIRONMENT_CHOSEN, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.INTERRUPTED, harness.Transcript, StringComparison.Ordinal);
	}

	private static int CountOccurrences(string text, string fragment)
	{
		int count = 0;
		int index = text.IndexOf(fragment, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(fragment, index + fragment.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
