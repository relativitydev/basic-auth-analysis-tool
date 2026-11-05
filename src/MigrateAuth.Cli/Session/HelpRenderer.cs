namespace MigrateAuth.Cli.Session;

/// <summary>
/// Renders <c>help</c>. It holds no list of commands and cannot: every line it writes is
/// read from <see cref="CommandRegistry"/>, which is the same place the dispatcher resolves
/// against. Listing every command is therefore a property of the structure rather than
/// something a maintainer has to keep true.
/// </summary>
public static class HelpRenderer
{
	/// <summary>Heading of the first section.</summary>
	public const string WORKFLOW_GROUP_HEADING = "Workflow commands, in the order they are meant to be run:";

	/// <summary>Heading of the second section.</summary>
	public const string SESSION_GROUP_HEADING = "Session commands:";

	/// <summary>
	/// A KNOWN LIMITATION is said in the help text rather than enforced in behaviour:
	/// the tool does not track the repository's source content across sessions, so a code change
	/// after a stage completed is the user's cue to start again from <c>scan</c>.
	/// </summary>
	public const string STALE_STATE_NOTICE =
		"Completed stages are remembered across sessions. The tool does not watch the repository, so re-run 'scan' after any code change.";

	private const string _COMMAND_LINE_INDENT = "  ";
	private const int _GAP_BETWEEN_NAME_AND_DESCRIPTION = 2;

	/// <summary>Writes the two groups, every command in each, in declared order.</summary>
	public static void Render(TextWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);

		int nameColumnWidth = CommandRegistry.CommandsInHelpOrder.Max(command => command.DisplayName.Length)
			+ _GAP_BETWEEN_NAME_AND_DESCRIPTION;

		WriteGroup(writer, WORKFLOW_GROUP_HEADING, CommandRegistry.WorkflowCommandsInRunOrder, nameColumnWidth);
		writer.WriteLine();
		WriteGroup(writer, SESSION_GROUP_HEADING, CommandRegistry.SessionCommandsInHelpOrder, nameColumnWidth);
		writer.WriteLine();
		writer.WriteLine(STALE_STATE_NOTICE);
	}

	private static void WriteGroup(
		TextWriter writer,
		string heading,
		IEnumerable<CommandDescriptor> commands,
		int nameColumnWidth)
	{
		writer.WriteLine(heading);

		foreach (CommandDescriptor command in commands)
		{
			writer.WriteLine($"{_COMMAND_LINE_INDENT}{command.DisplayName.PadRight(nameColumnWidth)}{command.Description}");
		}
	}
}
