using System.Globalization;

using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// THE STAGE'S CONSOLE LINES THAT TAKE A VALUE, filled from the constants of <see cref="AiAssistTexts"/> with the
/// invariant culture. The stage decides WHICH line a branch prints; this decides what the line is. Nothing here
/// is read from input or from the repository, and nothing here is a path: a file name, a count, a command name
/// and the destination as the user supplied it are the only values that ever reach a line.
/// </summary>
internal static class AiAssistConsoleLines
{
	private const string _LINE_PART_SEPARATOR = " ";
	private const int _SUCCESS_EXIT_CODE = 0;

	/// <summary>Written: the pack's bare name and the destination as supplied.</summary>
	internal static string Written(string packFileName, string destinationConsoleText)
	{
		return string.Format(CultureInfo.InvariantCulture, AiAssistTexts.WRITTEN_FORMAT, packFileName, destinationConsoleText);
	}

	/// <summary>The section was added to the report.</summary>
	internal static string SectionAppended(string packFileName)
	{
		return string.Format(CultureInfo.InvariantCulture, AiAssistTexts.SECTION_APPENDED_FORMAT, packFileName);
	}

	/// <summary>The fixed line for a refused append.</summary>
	internal static string AppendRefusal(ReportAppendRefusal refusal)
	{
		string line = refusal switch
		{
			ReportAppendRefusal.NotWrittenByThisProcess => AiAssistTexts.REPORT_NOT_WRITTEN_BY_THIS_RUN,
			ReportAppendRefusal.ReportReplacedOrLinked => AiAssistTexts.APPEND_REFUSED_REPLACED_OR_LINKED,
			ReportAppendRefusal.ReportChanged => AiAssistTexts.APPEND_REFUSED_REPORT_CHANGED,
			ReportAppendRefusal.OutsideBoundary => AiAssistTexts.APPEND_REFUSED_OUTSIDE_BOUNDARY,
			ReportAppendRefusal.NotWritable => AiAssistTexts.APPEND_REFUSED_NOT_WRITABLE,
			_ => throw new ArgumentOutOfRangeException(nameof(refusal), refusal, "An append that was not refused has no refusal line.")
		};

		return line;
	}

	/// <summary>The line printed before the search, naming the command.</summary>
	internal static string Searching(string commandName)
	{
		return string.Format(CultureInfo.InvariantCulture, AiAssistTexts.SEARCH_FORMAT, commandName);
	}

	/// <summary>The command was not found.</summary>
	internal static string NotFound(string commandName)
	{
		return string.Format(CultureInfo.InvariantCulture, AiAssistTexts.NOT_FOUND_FORMAT, commandName);
	}

	/// <summary>The only match lay inside the repository and was ignored.</summary>
	internal static string MatchInsideRepository(string commandName)
	{
		return string.Format(CultureInfo.InvariantCulture, AiAssistTexts.MATCH_INSIDE_REPOSITORY_FORMAT, commandName);
	}

	/// <summary>The fixed line for a launch that is withheld, and why.</summary>
	internal static string Ineligible(AgentLaunchEligibility eligibility, string commandName)
	{
		string line = eligibility switch
		{
			AgentLaunchEligibility.UnsafeCommandLine =>
				string.Format(CultureInfo.InvariantCulture, AiAssistTexts.UNSAFE_COMMAND_LINE_FORMAT, commandName),
			AgentLaunchEligibility.PathContainsControlCharacter => AiAssistTexts.PATH_CONTAINS_CONTROL_CHARACTER,
			AgentLaunchEligibility.WrapperNeedsLocalRepository =>
				string.Format(CultureInfo.InvariantCulture, AiAssistTexts.WRAPPER_NEEDS_LOCAL_REPOSITORY_FORMAT, commandName),
			_ => throw new ArgumentOutOfRangeException(nameof(eligibility), eligibility, "A launchable agent has no withheld line.")
		};

		return line;
	}

	/// <summary>The resolution line: file name, position of the search-path entry and how it would be started. Never a path.</summary>
	internal static string Resolution(AgentResolutionDescription resolution)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			AiAssistTexts.RESOLUTION_LINE_FORMAT,
			resolution.FileName,
			resolution.EntryPosition,
			resolution.EntryCount,
			resolution.StartKind is AgentStartKind.ThroughCommandInterpreter ? AiAssistTexts.STARTED_THROUGH_COMMAND_INTERPRETER : AiAssistTexts.STARTED_DIRECTLY);
	}

	/// <summary>The agent could not be started; no exception text is ever added.</summary>
	internal static string CouldNotBeStarted(string agentDisplayName)
	{
		return string.Format(CultureInfo.InvariantCulture, AiAssistTexts.COULD_NOT_BE_STARTED_FORMAT, agentDisplayName);
	}

	/// <summary>
	/// The termination message: how the agent ended, the fallback sentence for a non-zero exit, and the sentence
	/// that this tool changed neither the report nor the pack.
	/// </summary>
	internal static string Termination(AgentTerminationStatus status, AgentEnvironmentDescriptor environment)
	{
		string name = environment.DisplayName;
		string message = status.Outcome switch
		{
			AgentTerminationOutcome.ExitedWithCode => ExitedMessage(status.ExitCode.GetValueOrDefault(), environment),
			AgentTerminationOutcome.TerminatedBySignal =>
				string.Format(CultureInfo.InvariantCulture, AiAssistTexts.ENDED_BY_SIGNAL_FORMAT, name, status.SignalNumber.GetValueOrDefault()),
			AgentTerminationOutcome.CancelledByUser =>
				string.Format(CultureInfo.InvariantCulture, AiAssistTexts.INTERRUPTED_BY_USER_FORMAT, name),
			AgentTerminationOutcome.TimedOut =>
				string.Format(CultureInfo.InvariantCulture, AiAssistTexts.TIMED_OUT_FORMAT, name),
			AgentTerminationOutcome.NotFound => CouldNotBeStarted(name),
			_ => throw new ArgumentOutOfRangeException(nameof(status), status.Outcome, "Every outcome has a message.")
		};

		return $"{message}{_LINE_PART_SEPARATOR}{AiAssistTexts.TERMINATION_SUFFIX}";
	}

	private static string ExitedMessage(int exitCode, AgentEnvironmentDescriptor environment)
	{
		string message;

		if (exitCode == _SUCCESS_EXIT_CODE)
		{
			message = string.Format(CultureInfo.InvariantCulture, AiAssistTexts.ENDED_NORMALLY_FORMAT, environment.DisplayName);
		}
		else
		{
			string ended = string.Format(CultureInfo.InvariantCulture, AiAssistTexts.ENDED_WITH_CODE_FORMAT, environment.DisplayName, exitCode);
			string fallback = string.Format(CultureInfo.InvariantCulture, AiAssistTexts.FALLBACK_SENTENCE_FORMAT, environment.CommandName);

			message = $"{ended}{_LINE_PART_SEPARATOR}{fallback}";
		}

		return message;
	}

	/// <summary>The launch folder: the destination as the user supplied it and the folder's name, left in place.</summary>
	internal static string LaunchFolder(string destinationConsoleText, string folderName)
	{
		return string.Format(CultureInfo.InvariantCulture, AiAssistTexts.LAUNCH_FOLDER_FORMAT, AiAssistTexts.LaunchFolderText(destinationConsoleText, folderName));
	}
}
