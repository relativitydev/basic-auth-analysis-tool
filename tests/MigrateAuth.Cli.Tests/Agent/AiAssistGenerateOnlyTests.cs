using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// GENERATE ONLY: with no terminal, the stage writes the pack, adds the section to the report this process wrote, leaves the
/// data artifact byte-identical, prints what it wrote, and launches nothing. A failure to write the pack is a generation
/// failure that leaves the report and the data artifact as they were.
/// </summary>
public sealed class AiAssistGenerateOnlyTests
{
	private const string _LINE_ENDING = PromptPackWriter.LINE_ENDING;

	[Fact]
	public void ThePackIsWrittenTheSectionIsAppendedAndTheDataArtifactIsUntouched()
	{
		using AiAssistHarness harness = new(["1"]);
		byte[] reportBefore = harness.ReportBytes();
		byte[] dataBefore = harness.DataArtifactBytes();

		harness.IsTerminal = false;

		StageRunOutcome outcome = harness.Run();

		string report = System.Text.Encoding.UTF8.GetString(harness.ReportBytes());
		string expectedSection = $"{string.Join(_LINE_ENDING, AiAssistTexts.SectionLines(AgentEnvironment.ClaudeCode, AiAssistHarness.CLAUDE_PACK_FILE_NAME, 6))}{_LINE_ENDING}";

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME], harness.PackFiles());
		Assert.Equal(reportBefore, harness.ReportBytes()[..reportBefore.Length]);
		Assert.EndsWith(expectedSection, report, StringComparison.Ordinal);
		Assert.Contains(AiAssistTexts.REVIEW_LABEL, report[reportBefore.Length..], StringComparison.Ordinal);
		Assert.Equal(dataBefore, harness.DataArtifactBytes());
		harness.AssertNothingWasLaunched();
	}

	[Fact]
	public void TheConsoleSaysWhatWasWrittenAndAddedUsingTheDestinationAsSupplied()
	{
		using AiAssistHarness harness = new(["2"]);

		harness.IsTerminal = false;
		harness.Run();

		Assert.Contains("Written: ai-assist-openai-codex.md (in the output folder ./Outputs).", harness.Transcript, StringComparison.Ordinal);
		Assert.Contains("ai-assist: a supplemental section naming ai-assist-openai-codex.md was added to the report.", harness.Transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePackNamesTheReportByItsBareNameAndStatesItIsNotProvided()
	{
		using AiAssistHarness harness = new(["1"]);

		harness.IsTerminal = false;
		harness.Run();

		string pack = File.ReadAllText(harness.DestinationFolder.Combine(AiAssistHarness.CLAUDE_PACK_FILE_NAME));

		Assert.Contains($"The assessment report is named {PromptPackFixture.REPORT_FILE_NAME}; it is not provided to you.", pack, StringComparison.Ordinal);
	}

	/// <summary>Every name the pack could take is taken, so the writer refuses: the report and the data artifact are exactly as they were.</summary>
	[Fact]
	public void AnInjectedWriteRefusalLeavesNoPartialPackAndBothArtifactsByteIdentical()
	{
		using AiAssistHarness harness = new(["1"]);
		byte[] reportBefore = harness.ReportBytes();
		byte[] dataBefore = harness.DataArtifactBytes();

		harness.IsTerminal = false;
		Directory.CreateDirectory(harness.DestinationFolder.Combine(AiAssistHarness.CLAUDE_PACK_FILE_NAME));

		for (int ordinal = 2; ordinal <= AtomicArtifactWriter.MAXIMUM_NAME_ORDINAL; ordinal++)
		{
			Directory.CreateDirectory(harness.DestinationFolder.Combine($"{AiAssistHarness.CLAUDE_PACK_STEM}-{ordinal}.md"));
		}

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains(AiAssistTexts.PACK_GENERATION_FAILED, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains("The output destination (./Outputs)", harness.Transcript, StringComparison.Ordinal);
		Assert.Empty(Directory.GetFiles(harness.DestinationFolder.Path, "*.writing"));
		Assert.Equal(reportBefore, harness.ReportBytes());
		Assert.Equal(dataBefore, harness.DataArtifactBytes());
		Assert.DoesNotContain("supplemental section", harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>Through the shell, a refused pack write returns to the prompt and the session ends with the exit code it would have had.</summary>
	[Fact]
	public void AnInjectedWriteRefusalReturnsToThePromptAndKeepsTheExitCode()
	{
		using AiAssistHarness harness = new(["ai-assist", "1", "exit"]);

		harness.IsTerminal = false;
		harness.CompleteTheFourStagesAndSaveTheState();
		Directory.CreateDirectory(harness.DestinationFolder.Combine(AiAssistHarness.CLAUDE_PACK_FILE_NAME));

		for (int ordinal = 2; ordinal <= AtomicArtifactWriter.MAXIMUM_NAME_ORDINAL; ordinal++)
		{
			Directory.CreateDirectory(harness.DestinationFolder.Combine($"{AiAssistHarness.CLAUDE_PACK_STEM}-{ordinal}.md"));
		}

		byte[] stateBefore = File.ReadAllBytes(harness.StateFilePath);
		ExitCode exitCode = harness.RunThroughTheShell();
		int failure = harness.Transcript.IndexOf(AiAssistTexts.PACK_GENERATION_FAILED, StringComparison.Ordinal);

		Assert.Equal(ExitCode.Success, exitCode);
		Assert.True(failure >= 0);
		Assert.True(harness.Transcript.IndexOf(SessionShell.PROMPT, failure, StringComparison.Ordinal) > failure, "The prompt returns after the failure line.");
		Assert.Equal(stateBefore, File.ReadAllBytes(harness.StateFilePath));
		Assert.False(harness.State.IsCompleted(CommandId.AiAssist));
	}

	/// <summary>The destination is replaced by a file before the stage runs, so the pack write is refused by the writer.</summary>
	[Fact]
	public void APackThatCannotBeWrittenIsAGenerationFailureAndTheSessionContinues()
	{
		using AiAssistHarness harness = new(["1", "2"]);

		harness.IsTerminal = false;
		harness.ReplaceDestinationWithFile();

		StageRunOutcome outcome = harness.Run();

		Assert.Equal(StageRunOutcome.SelfReported, outcome);
		Assert.Contains(AiAssistTexts.PACK_GENERATION_FAILED, harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain("Written:", harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.NO_TERMINAL, harness.Transcript, StringComparison.Ordinal);
		Assert.Equal(StageRunOutcome.SelfReported, harness.Run());
	}
}
