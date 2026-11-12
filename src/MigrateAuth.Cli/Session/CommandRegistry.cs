using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// The two groups <c>help</c> to list under, and the difference between
/// them: a workflow command advances the assessment and is gated; a session command operates
/// the session itself and is never gated.
/// </summary>
public enum CommandGroup
{
	/// <summary>Listed first, in the order the commands are meant to be run.</summary>
	Workflow = 0,

	/// <summary>Listed second, and never part of the gating chain.</summary>
	Session = 1
}

/// <summary>
/// The identity of every command the session accepts.
///
/// It is an enumeration and not a string, and that is what makes the requirement that
/// every command be listed structural rather than maintained. Two mechanisms hold it:
///
/// the dispatcher switches over this type with no discard arm, so a value added here without
/// a dispatch arm does not compile under warnings-as-errors; and <see cref="CommandRegistry"/>
/// refuses to construct unless every value carries a descriptor, so a value added here without
/// a registry entry - and therefore without a line in <c>help</c> - does not start.
///
/// The dispatcher's only entry point is <see cref="CommandRegistry.TryResolve"/>, so a command
/// the registry does not carry is not reachable from user input at all.
/// </summary>
public enum CommandId
{
	/// <summary>Workflow, first: walks the repository and selects the candidate files.</summary>
	Scan = 0,

	/// <summary>Workflow, second: analyzes the candidates Scan selected.</summary>
	Analyze = 1,

	/// <summary>Workflow, third: attaches recommendations to the findings.</summary>
	Suggest = 2,

	/// <summary>Workflow, fourth: writes the report and the data artifact.</summary>
	Report = 3,

	/// <summary>Workflow, fifth and optional: hands the report to an external coding agent.</summary>
	AiAssist = 4,

	/// <summary>Session: lists every command.</summary>
	Help = 5,

	/// <summary>Session: clears the console.</summary>
	Clear = 6,

	/// <summary>Session: ends the session. Answers to two names.</summary>
	Exit = 7
}

/// <summary>
/// One registry entry. It has exactly five fields: name, group, order within the group,
/// one-line description, and gating predecessor.
/// </summary>
public sealed record CommandDescriptor
{
	/// <summary>Separates the two names of a command that answers to two.</summary>
	public const string ALTERNATE_NAME_SEPARATOR = "/";

	/// <summary>The identity the dispatcher switches over.</summary>
	public required CommandId Id { get; init; }

	/// <summary>The name the user types.</summary>
	public required string Name { get; init; }

	/// <summary>
	/// A second name for the same command, or null. It is one command with two names rather
	/// than two commands, which is what keeps the count at eight.
	/// </summary>
	public string? AlternateName { get; init; }

	/// <summary>Which of the two sections the command is listed under.</summary>
	public required CommandGroup Group { get; init; }

	/// <summary>Position within the group, starting at one and contiguous.</summary>
	public required int OrderWithinGroup { get; init; }

	/// <summary>The one line <c>help</c> prints beside the name.</summary>
	public required string Description { get; init; }

	/// <summary>
	/// The command that must have completed before this one may run, or null when the command
	/// is ungated. the gating and the cascade both read it from here.
	/// </summary>
	public CommandId? GatingPredecessor { get; init; }

	/// <summary>How the command is written in <c>help</c>: both names when it has two.</summary>
	public string DisplayName =>
		AlternateName is null ? Name : $"{Name}{ALTERNATE_NAME_SEPARATOR}{AlternateName}";
}

/// <summary>
/// The single command registry. Both the dispatcher and <c>help</c> read it, and neither
/// carries a list of its own - that is the whole mechanism by which <c>help</c> listing every
/// command is structurally true instead of maintained. A hand-written help text drifts from
/// the dispatcher on the first command added; there is no help text here to drift.
/// </summary>
public static class CommandRegistry
{
	/// <summary>
	/// The gating message.
	///
	/// It names the missing stage and not the tool, because the message is only ever shown inside
	/// the interactive session, where the tool is already running and naming it would instruct a
	/// separate process invocation.
	/// </summary>
	public const string GATING_MESSAGE_FORMAT = "Run '{0}' first.";

	/// <summary>
	/// The message for a command the session does not recognize. An unrecognized command-line
	/// option is reported with this same message rather than with a second vocabulary for the
	/// same mistake.
	/// </summary>
	public const string NOT_RECOGNIZED_MESSAGE_FORMAT = "command not found: {0}";

	/// <summary>The prompt to run <c>help</c> that is paired with the message above.</summary>
	public const string RUN_HELP_HINT = "Run 'help' to see every command this session accepts.";

	private const int _FIRST_ORDER_WITHIN_GROUP = 1;

	private const string _AI_ASSIST_DESCRIPTION =
		"Prepare a prompt pack for an AI-assisted review of the findings, and optionally launch your own AI command-line tool against it.";

	private static readonly ImmutableArray<CommandDescriptor> _commandsInHelpOrder = BuildValidatedCommands();

	private static readonly ImmutableArray<CommandDescriptor> _workflowCommandsInRunOrder =
	[
		.. _commandsInHelpOrder
			.Where(command => command.Group is CommandGroup.Workflow)
			.OrderBy(command => command.OrderWithinGroup)
	];

	private static readonly ImmutableArray<CommandDescriptor> _sessionCommandsInHelpOrder =
	[
		.. _commandsInHelpOrder
			.Where(command => command.Group is CommandGroup.Session)
			.OrderBy(command => command.OrderWithinGroup)
	];

	private static readonly FrozenDictionary<CommandId, CommandDescriptor> _commandsById =
		_commandsInHelpOrder.ToFrozenDictionary(command => command.Id);

	private static readonly FrozenDictionary<string, CommandDescriptor> _commandsByName =
		BuildNameIndex(_commandsInHelpOrder);

	/// <summary>Every command, workflow group first, each group in its declared order.</summary>
	public static ImmutableArray<CommandDescriptor> CommandsInHelpOrder => _commandsInHelpOrder;

	/// <summary>The workflow commands, in the order they are meant to be run.</summary>
	public static ImmutableArray<CommandDescriptor> WorkflowCommandsInRunOrder => _workflowCommandsInRunOrder;

	/// <summary>The session commands, in the order <c>help</c> lists them.</summary>
	public static ImmutableArray<CommandDescriptor> SessionCommandsInHelpOrder => _sessionCommandsInHelpOrder;

	/// <summary>
	/// Resolves what the user typed. This is the only route from input to a
	/// <see cref="CommandId"/>, so the dispatcher can never be reached by a command the
	/// registry does not carry - and therefore never by one <c>help</c> does not list.
	/// </summary>
	public static bool TryResolve(string enteredText, out CommandDescriptor? command)
	{
		ArgumentNullException.ThrowIfNull(enteredText);

		bool resolved = _commandsByName.TryGetValue(enteredText.Trim(), out CommandDescriptor? matched);
		command = matched;

		return resolved;
	}

	/// <summary>The descriptor of a known command.</summary>
	public static CommandDescriptor Describe(CommandId command)
	{
		if (!_commandsById.TryGetValue(command, out CommandDescriptor? descriptor))
		{
			throw new ArgumentOutOfRangeException(
				nameof(command),
				command,
				"No registry entry describes this command.");
		}

		return descriptor;
	}

	/// <summary>
	/// The workflow commands that follow the given one in run order. the cascade is
	/// exactly this collection, which is why the order lives in the registry and nowhere else.
	/// </summary>
	public static ImmutableArray<CommandDescriptor> WorkflowCommandsAfter(CommandId workflowCommand)
	{
		CommandDescriptor descriptor = Describe(workflowCommand);

		if (descriptor.Group is not CommandGroup.Workflow)
		{
			throw new ArgumentOutOfRangeException(
				nameof(workflowCommand),
				workflowCommand,
				"Only a workflow command has downstream stages.");
		}

		ImmutableArray<CommandDescriptor> downstream =
		[
			.. _workflowCommandsInRunOrder.Where(candidate => candidate.OrderWithinGroup > descriptor.OrderWithinGroup)
		];

		return downstream;
	}

	/// <summary>Builds the message for a predecessor that has not completed.</summary>
	public static string FormatGatingMessage(CommandId unmetPredecessor) =>
		string.Format(CultureInfo.InvariantCulture, GATING_MESSAGE_FORMAT, Describe(unmetPredecessor).Name);

	/// <summary>Builds the message for a token the session does not recognize.</summary>
	public static string FormatNotRecognizedMessage(string enteredText) =>
		string.Format(CultureInfo.InvariantCulture, NOT_RECOGNIZED_MESSAGE_FORMAT, enteredText);

	private static ImmutableArray<CommandDescriptor> BuildValidatedCommands()
	{
		ImmutableArray<CommandDescriptor> commands =
		[
			new CommandDescriptor
			{
				Id = CommandId.Scan,
				Name = "scan",
				Group = CommandGroup.Workflow,
				OrderWithinGroup = 1,
				Description = "Walk the repository, inventory it by language, and select the candidate files."
			},
			new CommandDescriptor
			{
				Id = CommandId.Analyze,
				Name = "analyze",
				Group = CommandGroup.Workflow,
				OrderWithinGroup = 2,
				Description = "Analyze the candidate files and produce the findings.",
				GatingPredecessor = CommandId.Scan
			},
			new CommandDescriptor
			{
				Id = CommandId.Suggest,
				Name = "suggest",
				Group = CommandGroup.Workflow,
				OrderWithinGroup = 3,
				Description = "Attach a migration recommendation to each finding that warrants one.",
				GatingPredecessor = CommandId.Analyze
			},
			new CommandDescriptor
			{
				Id = CommandId.Report,
				Name = "report",
				Group = CommandGroup.Workflow,
				OrderWithinGroup = 4,
				Description = "Write the Markdown report and the findings JSON file.",
				GatingPredecessor = CommandId.Suggest
			},
			new CommandDescriptor
			{
				Id = CommandId.AiAssist,
				Name = "ai-assist",
				Group = CommandGroup.Workflow,
				OrderWithinGroup = 5,
				Description = _AI_ASSIST_DESCRIPTION,
				GatingPredecessor = CommandId.Report
			},
			new CommandDescriptor
			{
				Id = CommandId.Help,
				Name = "help",
				Group = CommandGroup.Session,
				OrderWithinGroup = 1,
				Description = "List every command this session accepts."
			},
			new CommandDescriptor
			{
				Id = CommandId.Clear,
				Name = "clear",
				Group = CommandGroup.Session,
				OrderWithinGroup = 2,
				Description = "Clear the console."
			},
			new CommandDescriptor
			{
				Id = CommandId.Exit,
				Name = "exit",
				AlternateName = "quit",
				Group = CommandGroup.Session,
				OrderWithinGroup = 3,
				Description = "End the session. The completed stages are kept for the next one."
			}
		];

		RequireOneDescriptorPerCommandId(commands);
		RequireContiguousGroupOrdering(commands);
		RequireGatingChainFollowsRunOrder(commands);

		return commands;
	}

	/// <summary>
	/// The runtime half of the structural guarantee: a command identity with no descriptor
	/// would have no line in <c>help</c>, so the registry refuses to exist rather than let the
	/// dispatcher and <c>help</c> disagree. The compile-time half is the exhaustive switch the
	/// dispatcher performs over <see cref="CommandId"/>.
	/// </summary>
	private static void RequireOneDescriptorPerCommandId(ImmutableArray<CommandDescriptor> commands)
	{
		foreach (CommandId command in Enum.GetValues<CommandId>())
		{
			int descriptorCount = commands.Count(descriptor => descriptor.Id == command);

			if (descriptorCount != 1)
			{
				throw new InvalidOperationException(
					$"The command registry carries exactly one descriptor per command identity; {command} has {descriptorCount}.");
			}
		}
	}

	private static void RequireContiguousGroupOrdering(ImmutableArray<CommandDescriptor> commands)
	{
		foreach (CommandGroup group in Enum.GetValues<CommandGroup>())
		{
			int[] declaredOrders =
			[
				.. commands
					.Where(descriptor => descriptor.Group == group)
					.Select(descriptor => descriptor.OrderWithinGroup)
					.Order()
			];

			int[] expectedOrders = [.. Enumerable.Range(_FIRST_ORDER_WITHIN_GROUP, declaredOrders.Length)];

			if (!declaredOrders.SequenceEqual(expectedOrders))
			{
				throw new InvalidOperationException(
					$"The commands of group {group} must be ordered contiguously from {_FIRST_ORDER_WITHIN_GROUP}.");
			}
		}
	}

	private static void RequireGatingChainFollowsRunOrder(ImmutableArray<CommandDescriptor> commands)
	{
		foreach (CommandDescriptor sessionCommand in commands.Where(descriptor => descriptor.Group is CommandGroup.Session))
		{
			if (sessionCommand.GatingPredecessor is not null)
			{
				throw new InvalidOperationException($"Session command {sessionCommand.Name} must not be gated.");
			}
		}

		CommandDescriptor[] workflowCommands =
		[
			.. commands
				.Where(descriptor => descriptor.Group is CommandGroup.Workflow)
				.OrderBy(descriptor => descriptor.OrderWithinGroup)
		];

		for (int position = 0; position < workflowCommands.Length; position++)
		{
			CommandId? expectedPredecessor = position == 0 ? null : workflowCommands[position - 1].Id;

			if (workflowCommands[position].GatingPredecessor != expectedPredecessor)
			{
				throw new InvalidOperationException(
					$"Workflow command {workflowCommands[position].Name} must be gated behind the command preceding it in run order.");
			}
		}
	}

	private static FrozenDictionary<string, CommandDescriptor> BuildNameIndex(ImmutableArray<CommandDescriptor> commands)
	{
		Dictionary<string, CommandDescriptor> index = new(StringComparer.OrdinalIgnoreCase);

		foreach (CommandDescriptor command in commands)
		{
			AddName(index, command.Name, command);

			if (command.AlternateName is not null)
			{
				AddName(index, command.AlternateName, command);
			}
		}

		return index.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
	}

	private static void AddName(Dictionary<string, CommandDescriptor> index, string name, CommandDescriptor command)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new InvalidOperationException($"Command {command.Id} carries an empty name.");
		}

		if (!index.TryAdd(name, command))
		{
			throw new InvalidOperationException($"Two commands answer to the name {name}.");
		}
	}
}
