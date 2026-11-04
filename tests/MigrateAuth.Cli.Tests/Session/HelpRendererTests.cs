using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// <c>help</c> lists every command, split into the two groups, with none missing. The
/// assertions read the registry rather than a list of their own, so a command added to the
/// registry is a command these tests immediately require in <c>help</c>.
/// </summary>
public sealed class HelpRendererTests
{
	[Fact]
	public void HelpNamesEveryRegisteredCommandExactlyOnce()
	{
		string[] renderedLines = RenderHelpLines();

		foreach (CommandDescriptor command in CommandRegistry.CommandsInHelpOrder)
		{
			int lineCount = renderedLines.Count(line => line.TrimStart().StartsWith(command.DisplayName, StringComparison.Ordinal));

			Assert.Equal(1, lineCount);
		}
	}

	[Fact]
	public void HelpCarriesOneCommandLinePerRegisteredCommand()
	{
		string[] renderedLines = RenderHelpLines();

		int commandLineCount = renderedLines.Count(line =>
			CommandRegistry.CommandsInHelpOrder.Any(command =>
				line.TrimStart().StartsWith(command.DisplayName, StringComparison.Ordinal)));

		Assert.Equal(CommandRegistry.CommandsInHelpOrder.Length, commandLineCount);
	}

	[Fact]
	public void HelpSplitsTheCommandsIntoTheTwoRequiredGroups()
	{
		string[] renderedLines = RenderHelpLines();

		int workflowHeadingIndex = Array.IndexOf(renderedLines, HelpRenderer.WORKFLOW_GROUP_HEADING);
		int sessionHeadingIndex = Array.IndexOf(renderedLines, HelpRenderer.SESSION_GROUP_HEADING);

		Assert.InRange(workflowHeadingIndex, 0, sessionHeadingIndex - 1);
	}

	[Fact]
	public void HelpListsTheWorkflowCommandsInRunOrder()
	{
		string[] renderedLines = RenderHelpLines();

		int[] workflowLineIndexes =
		[
			.. CommandRegistry.WorkflowCommandsInRunOrder.Select(command => IndexOfCommandLine(renderedLines, command))
		];

		Assert.Equal(workflowLineIndexes.Order(), workflowLineIndexes);
	}

	[Fact]
	public void HelpListsEveryWorkflowCommandBeforeEverySessionCommand()
	{
		string[] renderedLines = RenderHelpLines();

		int lastWorkflowLineIndex = CommandRegistry.WorkflowCommandsInRunOrder
			.Max(command => IndexOfCommandLine(renderedLines, command));

		int firstSessionLineIndex = CommandRegistry.SessionCommandsInHelpOrder
			.Min(command => IndexOfCommandLine(renderedLines, command));

		Assert.InRange(lastWorkflowLineIndex, 0, firstSessionLineIndex - 1);
	}

	[Fact]
	public void HelpShowsBothNamesOfTheCommandThatHasTwo()
	{
		string renderedHelp = RenderHelp();

		Assert.Contains("exit/quit", renderedHelp, StringComparison.Ordinal);
	}

	[Fact]
	public void HelpDescribesEveryCommandItLists()
	{
		string renderedHelp = RenderHelp();

		foreach (CommandDescriptor command in CommandRegistry.CommandsInHelpOrder)
		{
			Assert.Contains(command.Description, renderedHelp, StringComparison.Ordinal);
		}
	}

	/// <summary>The staleness warning lives in the help text.</summary>
	[Fact]
	public void HelpWarnsThatCompletedStagesDoNotTrackCodeChanges()
	{
		string renderedHelp = RenderHelp();

		Assert.Contains(HelpRenderer.STALE_STATE_NOTICE, renderedHelp, StringComparison.Ordinal);
	}

	private static int IndexOfCommandLine(string[] renderedLines, CommandDescriptor command)
	{
		return Array.FindIndex(
			renderedLines,
			line => line.TrimStart().StartsWith(command.DisplayName, StringComparison.Ordinal));
	}

	private static string[] RenderHelpLines()
	{
		return RenderHelp().Split(Environment.NewLine);
	}

	private static string RenderHelp()
	{
		using StringWriter writer = new();

		HelpRenderer.Render(writer);

		return writer.ToString();
	}
}
