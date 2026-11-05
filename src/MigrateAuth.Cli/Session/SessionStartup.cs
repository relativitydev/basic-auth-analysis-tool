using System.Globalization;

using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// Everything that has to hold before the prompt exists.
///
/// Two exit codes are established here and nowhere else, because both describe a session that cannot
/// start: an unusable repository path terminates with <see cref="ExitCode.UnusableRepositoryPath"/>,
/// and a destination that cannot be written terminates with
/// <see cref="ExitCode.OutputDestinationNotWritable"/>. A destination that becomes unwritable
/// mid-session returns to the prompt instead, in <see cref="SessionShell"/>.
///
/// Every message here names a folder by its label: no absolute path appears in anything the product
/// emits, including a failure message.
/// </summary>
public static class SessionStartup
{
	/// <summary>Said when the repository option was not supplied at all.</summary>
	public const string REPOSITORY_FOLDER_NOT_SUPPLIED_MESSAGE =
		"No repository folder was supplied, so there is nothing to assess.";

	/// <summary>Said when the supplied text cannot be read as a path.</summary>
	public const string REPOSITORY_FOLDER_PATH_MALFORMED_MESSAGE =
		"The repository folder path supplied is not a usable path.";

	/// <summary>Said when the folder is not there.</summary>
	public const string REPOSITORY_FOLDER_NOT_FOUND_MESSAGE_FORMAT =
		"The repository folder does not exist: {0}.";

	/// <summary>Said when the path names something that is not a folder.</summary>
	public const string REPOSITORY_PATH_NOT_A_FOLDER_MESSAGE_FORMAT =
		"The repository path is not a folder: {0}.";

	/// <summary>Said when the folder exists and cannot be opened at all.</summary>
	public const string REPOSITORY_FOLDER_NOT_READABLE_MESSAGE_FORMAT =
		"The repository folder cannot be opened: {0}.";

	/// <summary>Said when the destination cannot be created or written to.</summary>
	public const string DESTINATION_FOLDER_NOT_WRITABLE_MESSAGE_FORMAT =
		"The documentation destination folder cannot be written to: {0}.";

	/// <summary>Said when the supplied destination text cannot be read as a path.</summary>
	public const string DESTINATION_FOLDER_PATH_MALFORMED_MESSAGE =
		"The documentation destination folder path supplied is not a usable path.";

	/// <summary>Said when the state on disk was accepted, followed by the stages it carried.</summary>
	public const string SESSION_RESUMED_MESSAGE_FORMAT = "Resuming the session for {0}.";

	/// <summary>Names the stages a resumed session already holds, so gating is no surprise.</summary>
	public const string COMPLETED_STAGES_MESSAGE_FORMAT = "Completed stages: {0}.";

	/// <summary>Said when the session starts with nothing completed.</summary>
	public const string SESSION_FRESH_MESSAGE_FORMAT = "Starting a fresh session for {0}.";

	/// <summary>
	/// Said when a state file existed and was not used. The reason is reported as a code: the file is
	/// untrusted, so nothing out of it is echoed back. The code is never the raw
	/// <see cref="SessionStateRejectionReason"/> name - <see cref="ReportVocabulary.DescriptionOf"/> is
	/// what fills this format's argument.
	/// </summary>
	public const string SESSION_STATE_DISCARDED_MESSAGE_FORMAT = "The saved session state was not used: {0}.";

	private const string _WRITE_PROBE_FILE_NAME = "migrate-auth.write-probe";
	private const string _STAGE_NAME_SEPARATOR = ", ";

	/// <summary>
	/// The check, run before the prompt exists. Denial is established by attempting
	/// rather than by inspecting, so the folder is actually enumerated: a folder that
	/// exists and refuses to open is exactly the "cannot be opened at all" case.
	/// </summary>
	public static bool TryResolveRepositoryFolder(
		CommandLineParseResult parsedCommandLine,
		TextWriter outputWriter,
		out RepositoryFolderPath repositoryFolder)
	{
		ArgumentNullException.ThrowIfNull(parsedCommandLine);
		ArgumentNullException.ThrowIfNull(outputWriter);

		repositoryFolder = default;
		string? suppliedText = parsedCommandLine.RepositoryFolderPathText;
		bool resolved = false;

		if (string.IsNullOrWhiteSpace(suppliedText))
		{
			outputWriter.WriteLine(REPOSITORY_FOLDER_NOT_SUPPLIED_MESSAGE);
			outputWriter.WriteLine(CommandLineParser.UsageLine);
		}
		else if (!TryCanonicalizeRepositoryFolder(suppliedText, outputWriter, out RepositoryFolderPath candidate))
		{
			repositoryFolder = default;
		}
		else if (DescribeRepositoryProblem(candidate) is string problemMessage)
		{
			outputWriter.WriteLine(problemMessage);
		}
		else
		{
			repositoryFolder = candidate;
			resolved = true;
		}

		return resolved;
	}

	/// <summary>
	/// Canonicalizes the destination, and does nothing else. The value comes from the effective
	/// configuration - the command line, then the user profile - and the fallback when neither supplied
	/// one is the working directory at launch, captured once here so that a session changing directory
	/// later does not move its own output. The repository's own configuration file is not one of the
	/// candidates and never reaches this method.
	///
	/// Writability is a separate step. Establishing that a folder can be written to means writing to it,
	/// and the destination may be the repository under audit - so probing before the containment
	/// comparison would put a file inside the repository. Pre-flight therefore runs between the two:
	/// canonicalize, compare, then probe.
	/// </summary>
	public static bool TryCanonicalizeDestinationFolder(
		EffectiveConfiguration configuration,
		TextWriter outputWriter,
		out DocumentationDestinationFolderPath destinationFolder)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(outputWriter);

		destinationFolder = default;
		string suppliedText = configuration.OutputPathText ?? Directory.GetCurrentDirectory();
		bool canonicalized = false;

		try
		{
			destinationFolder = DocumentationDestinationFolderPath.FromSuppliedText(suppliedText);
			canonicalized = true;
		}
		catch (ArgumentException)
		{
			outputWriter.WriteLine(DESTINATION_FOLDER_PATH_MALFORMED_MESSAGE);
		}
		catch (NotSupportedException)
		{
			outputWriter.WriteLine(DESTINATION_FOLDER_PATH_MALFORMED_MESSAGE);
		}

		return canonicalized;
	}

	/// <summary>
	/// Establishes that the destination can be written to, by writing to it. Called after
	/// Pre-flight has cleared the write boundary, never before.
	/// </summary>
	public static bool TryEstablishDestinationIsWritable(
		DocumentationDestinationFolderPath destinationFolder,
		TextWriter outputWriter)
	{
		ArgumentNullException.ThrowIfNull(outputWriter);

		bool writable = IsWritable(destinationFolder);

		if (!writable)
		{
			WriteFormatted(outputWriter, DESTINATION_FOLDER_NOT_WRITABLE_MESSAGE_FORMAT, destinationFolder.Label);
		}

		return writable;
	}

	/// <summary>Reports the tokens the command line parser refused, without ending anything.</summary>
	public static void ReportRejectedArguments(CommandLineParseResult parsedCommandLine, TextWriter outputWriter)
	{
		ArgumentNullException.ThrowIfNull(parsedCommandLine);
		ArgumentNullException.ThrowIfNull(outputWriter);

		foreach (string unrecognizedToken in parsedCommandLine.UnrecognizedTokens)
		{
			outputWriter.WriteLine(CommandRegistry.FormatNotRecognizedMessage(unrecognizedToken));
		}

		foreach (string optionName in parsedCommandLine.OptionsMissingValue)
		{
			outputWriter.WriteLine(CommandLineParser.FormatMissingOptionValueMessage(optionName));
		}

		if (parsedCommandLine.UnrecognizedTokens.Length > 0 || parsedCommandLine.OptionsMissingValue.Length > 0)
		{
			outputWriter.WriteLine(CommandLineParser.UsageLine);
		}
	}

	/// <summary>
	/// Says whether the session resumed and, when it did not and a file was there, why. A
	/// missing file is the ordinary first run and is not reported as a discard.
	/// </summary>
	public static void ReportSessionStart(
		SessionStateLoad sessionStateLoad,
		RepositoryFolderPath repositoryFolder,
		TextWriter outputWriter)
	{
		ArgumentNullException.ThrowIfNull(sessionStateLoad);
		ArgumentNullException.ThrowIfNull(outputWriter);

		bool stateWasDiscarded =
			!sessionStateLoad.WasResumed
			&& sessionStateLoad.RejectionReason is not SessionStateRejectionReason.FileAbsent;

		if (stateWasDiscarded)
		{
			WriteFormatted(
				outputWriter,
				SESSION_STATE_DISCARDED_MESSAGE_FORMAT,
				ReportVocabulary.DescriptionOf(sessionStateLoad.RejectionReason));
		}

		IEnumerable<string> completedStageNames = sessionStateLoad.State
			.CompletedStagesInRunOrder()
			.Select(stage => stage.Name);

		if (sessionStateLoad.WasResumed && sessionStateLoad.State.CompletedStagesInRunOrder().Length > 0)
		{
			WriteFormatted(outputWriter, SESSION_RESUMED_MESSAGE_FORMAT, repositoryFolder.Label);
			WriteFormatted(
				outputWriter,
				COMPLETED_STAGES_MESSAGE_FORMAT,
				string.Join(_STAGE_NAME_SEPARATOR, completedStageNames));
		}
		else
		{
			WriteFormatted(outputWriter, SESSION_FRESH_MESSAGE_FORMAT, repositoryFolder.Label);
		}
	}

	private static bool TryCanonicalizeRepositoryFolder(
		string suppliedText,
		TextWriter outputWriter,
		out RepositoryFolderPath repositoryFolder)
	{
		repositoryFolder = default;
		bool canonicalized = false;

		try
		{
			repositoryFolder = RepositoryFolderPath.FromSuppliedText(suppliedText);
			canonicalized = true;
		}
		catch (ArgumentException)
		{
			outputWriter.WriteLine(REPOSITORY_FOLDER_PATH_MALFORMED_MESSAGE);
		}
		catch (NotSupportedException)
		{
			outputWriter.WriteLine(REPOSITORY_FOLDER_PATH_MALFORMED_MESSAGE);
		}

		return canonicalized;
	}

	private static string? DescribeRepositoryProblem(RepositoryFolderPath repositoryFolder)
	{
		string? problemMessage = null;

		if (File.Exists(repositoryFolder.CanonicalPath))
		{
			problemMessage = Format(REPOSITORY_PATH_NOT_A_FOLDER_MESSAGE_FORMAT, repositoryFolder.Label);
		}
		else if (!Directory.Exists(repositoryFolder.CanonicalPath))
		{
			problemMessage = Format(REPOSITORY_FOLDER_NOT_FOUND_MESSAGE_FORMAT, repositoryFolder.Label);
		}
		else if (!IsEnumerable(repositoryFolder))
		{
			problemMessage = Format(REPOSITORY_FOLDER_NOT_READABLE_MESSAGE_FORMAT, repositoryFolder.Label);
		}

		return problemMessage;
	}

	private static bool IsEnumerable(RepositoryFolderPath repositoryFolder)
	{
		bool enumerable;

		try
		{
			using IEnumerator<string> entries =
				Directory.EnumerateFileSystemEntries(repositoryFolder.CanonicalPath).GetEnumerator();

			entries.MoveNext();
			enumerable = true;
		}
		catch (UnauthorizedAccessException)
		{
			enumerable = false;
		}
		catch (IOException)
		{
			enumerable = false;
		}

		return enumerable;
	}

	/// <summary>
	/// Establishes writability by writing, for the same reason the repository check enumerates
	/// rather than inspects: a permission model differs per platform and an attempt does not.
	/// </summary>
	private static bool IsWritable(DocumentationDestinationFolderPath destinationFolder)
	{
		string probePath = Path.Combine(destinationFolder.CanonicalPath, _WRITE_PROBE_FILE_NAME);
		bool writable;

		try
		{
			Directory.CreateDirectory(destinationFolder.CanonicalPath);

			using (FileStream probeStream = new(probePath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				writable = true;
			}
		}
		catch (UnauthorizedAccessException)
		{
			writable = false;
		}
		catch (IOException)
		{
			writable = false;
		}
		catch (NotSupportedException)
		{
			writable = false;
		}
		finally
		{
			DeleteProbe(probePath);
		}

		return writable;
	}

	private static void DeleteProbe(string probePath)
	{
		try
		{
			File.Delete(probePath);
		}
		catch (UnauthorizedAccessException)
		{
		}
		catch (IOException)
		{
		}
	}

	private static void WriteFormatted(TextWriter outputWriter, string messageFormat, string argument)
	{
		outputWriter.WriteLine(Format(messageFormat, argument));
	}

	private static string Format(string messageFormat, string argument) =>
		string.Format(CultureInfo.InvariantCulture, messageFormat, argument);
}
