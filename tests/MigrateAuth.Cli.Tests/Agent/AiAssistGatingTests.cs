using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE SHELL'S GATE IN FRONT OF THE STAGE: <c>ai-assist</c> before <c>report</c> gives the report stage's own gating
/// message and runs nothing; after <c>report</c> it runs, and may run again; a resumed session (no report written by this
/// process) passes the same gate; and once <c>analyze</c> runs again the stage is gated again.
/// </summary>
public sealed class AiAssistGatingTests
{
	private const string _AI_ASSIST_COMMAND = "ai-assist";
	private const string _EXIT_COMMAND = "exit";

	private static readonly CommandId[] _fourStages = [CommandId.Scan, CommandId.Analyze, CommandId.Suggest, CommandId.Report];

	private static void CompleteTheFourStages(SessionState state)
	{
		foreach (CommandId stage in _fourStages)
		{
			state.MarkCompleted(stage);
		}
	}

	private static SessionShell BuildShell(AiAssistHarness harness)
	{
		SessionConsole console = new(harness.Input, harness.Output);

		return new SessionShell(
			harness.RepositoryPath,
			harness.State,
			new SessionStateStore(harness.Destination),
			harness.Runner,
			console);
	}

	[Fact]
	public void BeforeTheReportTheGateSaysSoAndNothingRuns()
	{
		using AiAssistHarness harness = new([_AI_ASSIST_COMMAND, "1", _EXIT_COMMAND]);

		harness.IsTerminal = false;
		harness.State.TryFindUnmetPredecessor(CommandId.AiAssist, out CommandDescriptor? unmet);
		new SessionStateStore(harness.Destination).Save(harness.RepositoryPath, harness.State);

		byte[] stateBefore = File.ReadAllBytes(harness.StateFilePath);

		BuildShell(harness).Run();

		Assert.NotNull(unmet);
		Assert.Contains(CommandRegistry.FormatGatingMessage(unmet!.Id), harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(AiAssistTexts.SELECTION_HEADING, harness.Transcript, StringComparison.Ordinal);
		Assert.Empty(harness.PackFiles());
		Assert.Equal(stateBefore, File.ReadAllBytes(harness.StateFilePath));
	}

	[Fact]
	public void AfterTheReportTheStageRunsAndMayRunAgain()
	{
		using AiAssistHarness harness = new([_AI_ASSIST_COMMAND, "1", _AI_ASSIST_COMMAND, "2", _EXIT_COMMAND]);

		harness.IsTerminal = false;
		CompleteTheFourStages(harness.State);

		BuildShell(harness).Run();

		Assert.Equal([AiAssistHarness.CLAUDE_PACK_FILE_NAME, AiAssistHarness.CODEX_PACK_FILE_NAME], harness.PackFiles());
		Assert.False(harness.State.IsCompleted(CommandId.AiAssist));
		Assert.DoesNotContain(string.Format(System.Globalization.CultureInfo.InvariantCulture, SessionShell.STAGE_COMPLETED_MESSAGE_FORMAT, _AI_ASSIST_COMMAND), harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(string.Format(System.Globalization.CultureInfo.InvariantCulture, SessionShell.STAGE_FAILED_MESSAGE_FORMAT, _AI_ASSIST_COMMAND), harness.Transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(string.Format(System.Globalization.CultureInfo.InvariantCulture, SessionShell.STAGE_CANCELLED_MESSAGE_FORMAT, _AI_ASSIST_COMMAND), harness.Transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void AResumedSessionPassesTheGateWritesThePackAndAppendsNothing()
	{
		using AiAssistHarness harness = new([_AI_ASSIST_COMMAND, "3", _EXIT_COMMAND], writeReport: false);

		harness.IsTerminal = false;
		CompleteTheFourStages(harness.State);

		BuildShell(harness).Run();

		Assert.Equal([AiAssistHarness.COPILOT_PACK_FILE_NAME], harness.PackFiles());
		Assert.Contains("a section is added only to a report written in this same run", harness.Transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void AfterAnalyzeRunsAgainTheStageIsGatedAgain()
	{
		using AiAssistHarness harness = new([_AI_ASSIST_COMMAND, "1", _EXIT_COMMAND]);

		harness.IsTerminal = false;
		CompleteTheFourStages(harness.State);
		harness.State.MarkCompleted(CommandId.Analyze);
		harness.State.TryFindUnmetPredecessor(CommandId.AiAssist, out CommandDescriptor? unmet);

		BuildShell(harness).Run();

		Assert.NotNull(unmet);
		Assert.Contains(CommandRegistry.FormatGatingMessage(unmet!.Id), harness.Transcript, StringComparison.Ordinal);
		Assert.Empty(harness.PackFiles());
	}
}
