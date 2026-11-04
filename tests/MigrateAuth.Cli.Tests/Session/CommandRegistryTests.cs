using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// The registry is the single place the dispatcher and <c>help</c> both read, so these tests
/// are about the registry being total and well formed rather than about any command's
/// behaviour. Nothing here asserts a count as a literal: the count is whatever the registry
/// carries, and the requirement's own list is pinned by name instead.
/// </summary>
public sealed class CommandRegistryTests
{
	[Fact]
	public void EveryCommandIdentityIsDescribedExactlyOnce()
	{
		foreach (CommandId command in Enum.GetValues<CommandId>())
		{
			int descriptorCount = CommandRegistry.CommandsInHelpOrder.Count(descriptor => descriptor.Id == command);

			Assert.Equal(1, descriptorCount);
		}
	}

	[Fact]
	public void EveryCommandIdentityResolvesFromItsOwnName()
	{
		foreach (CommandDescriptor descriptor in CommandRegistry.CommandsInHelpOrder)
		{
			bool resolved = CommandRegistry.TryResolve(descriptor.Name, out CommandDescriptor? matched);

			Assert.True(resolved);
			Assert.Equal(descriptor.Id, matched!.Id);
		}
	}

	/// <summary>
	/// the list, pinned by name. This is the one place the eight commands are stated,
	/// and it states them as the requirement does rather than as a number.
	/// </summary>
	[Fact]
	public void RegistryCarriesTheCommandsTheRequirementNames()
	{
		string[] expectedWorkflowNames = ["scan", "analyze", "suggest", "report", "ai-assist"];
		string[] expectedSessionDisplayNames = ["help", "clear", "exit/quit"];

		string[] actualWorkflowNames = [.. CommandRegistry.WorkflowCommandsInRunOrder.Select(command => command.Name)];
		string[] actualSessionDisplayNames = [.. CommandRegistry.SessionCommandsInHelpOrder.Select(command => command.DisplayName)];

		Assert.Equal(expectedWorkflowNames, actualWorkflowNames);
		Assert.Equal(expectedSessionDisplayNames, actualSessionDisplayNames);
	}

	[Fact]
	public void HelpOrderIsTheWorkflowGroupFollowedByTheSessionGroup()
	{
		CommandDescriptor[] expectedOrder =
		[
			.. CommandRegistry.WorkflowCommandsInRunOrder,
			.. CommandRegistry.SessionCommandsInHelpOrder
		];

		Assert.Equal(expectedOrder, CommandRegistry.CommandsInHelpOrder);
	}

	[Theory]
	[InlineData(CommandId.Analyze, CommandId.Scan)]
	[InlineData(CommandId.Suggest, CommandId.Analyze)]
	[InlineData(CommandId.Report, CommandId.Suggest)]
	[InlineData(CommandId.AiAssist, CommandId.Report)]
	public void WorkflowCommandIsGatedBehindTheOneBeforeIt(CommandId command, CommandId expectedPredecessor)
	{
		CommandId? actualPredecessor = CommandRegistry.Describe(command).GatingPredecessor;

		Assert.Equal(expectedPredecessor, actualPredecessor);
	}

	[Fact]
	public void ScanIsTheOnlyUngatedWorkflowCommand()
	{
		CommandDescriptor[] ungatedWorkflowCommands =
		[
			.. CommandRegistry.WorkflowCommandsInRunOrder.Where(command => command.GatingPredecessor is null)
		];

		Assert.Equal([CommandId.Scan], ungatedWorkflowCommands.Select(command => command.Id));
	}

	[Fact]
	public void NoSessionCommandIsGated()
	{
		foreach (CommandDescriptor sessionCommand in CommandRegistry.SessionCommandsInHelpOrder)
		{
			Assert.Null(sessionCommand.GatingPredecessor);
		}
	}

	/// <summary>The optional stage: <c>ai-assist</c> gates like any other stage.</summary>
	[Fact]
	public void AiAssistBlocksNothingBecauseNothingFollowsIt()
	{
		Assert.Empty(CommandRegistry.WorkflowCommandsAfter(CommandId.AiAssist));
	}

	[Fact]
	public void DownstreamStagesAreTheOnesAfterTheGivenStageInRunOrder()
	{
		CommandId[] downstreamOfAnalyze =
		[
			.. CommandRegistry.WorkflowCommandsAfter(CommandId.Analyze).Select(command => command.Id)
		];

		Assert.Equal([CommandId.Suggest, CommandId.Report, CommandId.AiAssist], downstreamOfAnalyze);
	}

	[Theory]
	[InlineData("exit")]
	[InlineData("quit")]
	[InlineData("QUIT")]
	[InlineData("  exit  ")]
	public void ExitAndQuitAreOneCommandWithTwoNames(string enteredText)
	{
		bool resolved = CommandRegistry.TryResolve(enteredText, out CommandDescriptor? command);

		Assert.True(resolved);
		Assert.Equal(CommandId.Exit, command!.Id);
	}

	[Theory]
	[InlineData("scam")]
	[InlineData("")]
	[InlineData("scan analyze")]
	[InlineData("--repositoryFolderPath")]
	public void TokenTheRegistryDoesNotCarryDoesNotResolve(string enteredText)
	{
		bool resolved = CommandRegistry.TryResolve(enteredText, out CommandDescriptor? command);

		Assert.False(resolved);
		Assert.Null(command);
	}

	/// <summary>
	/// The format literal itself, pinned independently of the constant. The requirement
	/// specifies it verbatim.
	/// </summary>
	[Fact]
	public void GatingMessageFormatIsTheRunStageFirstLiteral()
	{
		Assert.Equal("Run '{0}' first.", CommandRegistry.GATING_MESSAGE_FORMAT);
	}

	[Fact]
	public void GatingMessageNamesTheMissingPredecessorAndNotTheTool()
	{
		string message = CommandRegistry.FormatGatingMessage(CommandId.Scan);

		Assert.Equal("Run 'scan' first.", message);
	}

	[Fact]
	public void GatingMessageNamesWhicheverPredecessorIsMissing()
	{
		string message = CommandRegistry.FormatGatingMessage(CommandId.Report);

		Assert.Equal("Run 'report' first.", message);
	}

	[Fact]
	public void NotRecognizedMessageIsTheOneTheRequirementStates()
	{
		string message = CommandRegistry.FormatNotRecognizedMessage("scam");

		Assert.Equal("command not found: scam", message);
	}

	[Fact]
	public void DescribingAnIdentityTheRegistryDoesNotCarryIsRejected()
	{
		const CommandId notACommand = (CommandId)int.MaxValue;

		Assert.Throws<ArgumentOutOfRangeException>(() => CommandRegistry.Describe(notACommand));
	}

	[Fact]
	public void DownstreamStagesAreAskedOnlyOfWorkflowCommands()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CommandRegistry.WorkflowCommandsAfter(CommandId.Help));
	}
}
