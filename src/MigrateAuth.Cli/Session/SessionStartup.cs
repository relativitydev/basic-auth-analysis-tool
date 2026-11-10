using System.Globalization;

using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// Everything that has to hold before the prompt exists.
///
/// Two of the exit codes are established here and nowhere else, because both describe a
/// session that cannot start: an unusable repository path terminates with
/// <see cref="ExitCode.UnusableRepositoryPath"/> (stop immediately, say clearly
/// what is wrong, do not continue), and a destination that cannot be written terminates with
/// <see cref="ExitCode.OutputDestinationNotWritable"/>. The second one is the half of the
/// split between startup and mid-session failures that has nothing to salvage; the mid-session half returns to the prompt and lives in
/// <see cref="SessionShell"/>.
///
/// A message about the destination echoes the folder as the user supplied it (console channel only): relative to the working folder when it is the default or relative, for
/// example <c>./Outputs</c>, and as typed when it is absolute or a leading <c>~</c> from the profile file.
/// The canonical resolved path is never printed, and the report and the data artifact stay label-only. A message about a
/// repository path that cannot be used echoes the text the user supplied, exactly as given, because the user typed
/// it and needs to see which path was refused - the label alone is only its last segment.
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

	/// <summary>
	/// Said when the profile's output path starts with <c>~</c> and the user's home folder cannot be
	/// resolved. The run ends with <see cref="ExitCode.OutputDestinationNotWritable"/>.
	/// </summary>
	public const string DESTINATION_HOME_FOLDER_UNRESOLVED_MESSAGE_FORMAT =
		"The documentation destination folder path {0} starts with '~', but the home folder of the current user cannot be determined, so the path cannot be expanded. Supply an absolute path instead.";

	/// <summary>
	/// Said when the profile's output path is a <c>~user</c> form. The run ends with
	/// <see cref="ExitCode.OutputDestinationNotWritable"/>.
	/// </summary>
	public const string DESTINATION_OTHER_USER_HOME_NOT_SUPPORTED_MESSAGE_FORMAT =
		"The documentation destination folder path {0} names the home folder of another user, which is not supported. Use '~', '~/' or '~\\' for your own home folder, or supply an absolute path.";

	/// <summary>Said when the supplied destination text cannot be read as a path.</summary>
	public const string DESTINATION_FOLDER_PATH_MALFORMED_MESSAGE =
		"The documentation destination folder path supplied is not a usable path.";

	/// <summary>Said when the state on disk was accepted, followed by the stages it carried.</summary>
	public const string SESSION_RESUMED_MESSAGE_FORMAT = "Resuming the session for {0}.";

	/// <summary>Names the stages a resumed session already holds, so gating is no surprise.</summary>
	public const string COMPLETED_STAGES_MESSAGE_FORMAT = "Completed stages: {0}.";

	/// <summary>
	/// Said once, at resume or else at <c>analyze</c>, when the configuration now enables a different set of
	/// languages than the one the completed scan used. The analysis works on the candidates the scan
	/// selected, so the earlier set is the one still in effect until <c>scan</c> runs again.
	/// </summary>
	public const string SCAN_ENABLED_LANGUAGES_CHANGED_MESSAGE =
		"The configuration enables a different set of languages than the saved scan used. Analysis covers the languages the scan selected; re-run 'scan' to apply the current configuration.";

	/// <summary>Said when the session starts with nothing completed.</summary>
	public const string SESSION_FRESH_MESSAGE_FORMAT = "Starting a fresh session for {0}.";

	/// <summary>
	/// Said when a state file existed and was not used. The reason is
	/// reported, and it is reported as a code: the file is untrusted, so nothing out of it is
	/// echoed back. The code is never the raw <see cref="SessionStateRejectionReason"/>
	/// name - <see cref="ReportVocabulary.DescriptionOf"/> is what fills this format's argument.
	/// </summary>
	public const string SESSION_STATE_DISCARDED_MESSAGE_FORMAT = "The saved session state was not used: {0}.";

	private const string _WRITE_PROBE_FILE_NAME = "migrate-auth.write-probe";
	private const string _STAGE_NAME_SEPARATOR = ", ";

	/// <summary>
	/// The repository check, run before the prompt exists. Denial is established by attempting
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

			// The rejected-arguments report has already printed the usage line for this launch.
			if (!HasRejectedArguments(parsedCommandLine))
			{
				outputWriter.WriteLine(CommandLineParser.UsageLine);
			}
		}
		else if (!TryCanonicalizeRepositoryFolder(suppliedText, outputWriter, out RepositoryFolderPath candidate))
		{
			repositoryFolder = default;
		}
		else if (DescribeRepositoryProblem(candidate, suppliedText) is string problemMessage)
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
	/// configuration, which has already applied its order - the command line, then the user
	/// profile - and the fallback when neither supplied one is the working directory at launch,
	/// captured once here so that a session changing directory later
	/// does not move its own output. The repository's own configuration file is not one of the
	/// candidates and never reaches this method.
	///
	/// A leading <c>~</c>, <c>~/</c> or <c>~\</c> in a value that came from the USER PROFILE file is expanded to the
	/// home folder BEFORE canonicalization, so Pre-flight's containment comparison sees the expanded folder; an unresolvable
	/// home folder and a <c>~user</c> form are refused with a message, and the caller ends the run with exit code 4.
	/// A command-line value is never expanded (the shell does that), and neither is <c>repositoryFolderPath</c>.
	/// <paramref name="homeFolderProvider"/> defaults to the platform's user profile folder and is
	/// injected by tests so that they never change the process environment.
	///
	/// WRITABILITY IS A SEPARATE STEP ON PURPOSE, and the split is what the containment rule costs. Establishing
	/// that a folder can be written to means writing to it, and the most natural invocation of
	/// this tool makes that folder the repository under audit - so probing before the containment
	/// comparison would put a file inside the repository that this tool never
	/// writes to. Pre-flight therefore runs between the two: canonicalize, compare, then probe.
	/// </summary>
	public static bool TryCanonicalizeDestinationFolder(
		EffectiveConfiguration configuration,
		TextWriter outputWriter,
		out DocumentationDestinationFolderPath destinationFolder,
		Func<string>? homeFolderProvider = null)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(outputWriter);

		destinationFolder = default;
		bool canonicalized = false;
		string? configuredText = configuration.OutputPathText;
		OutputPathExpansion expansion = ExpandConfiguredOutputPath(
			configuration,
			homeFolderProvider ?? OutputPathHomeExpansion.ReadUserProfileFolder);

		if (expansion.Outcome is OutputPathExpansionOutcome.HomeFolderUnresolved)
		{
			WriteFormatted(outputWriter, DESTINATION_HOME_FOLDER_UNRESOLVED_MESSAGE_FORMAT, ForConsole(expansion.Text));
		}
		else if (expansion.Outcome is OutputPathExpansionOutcome.OtherUserFormRefused)
		{
			WriteFormatted(outputWriter, DESTINATION_OTHER_USER_HOME_NOT_SUPPORTED_MESSAGE_FORMAT, ForConsole(expansion.Text));
		}
		else
		{
			canonicalized = TryCanonicalizeDestinationText(configuredText, expansion, outputWriter, out destinationFolder);
		}

		return canonicalized;
	}

	private static OutputPathExpansion ExpandConfiguredOutputPath(
		EffectiveConfiguration configuration,
		Func<string> homeFolderProvider)
	{
		string? configuredText = configuration.OutputPathText;
		bool comesFromTheProfileFile = configuration.Provenance.OutputPathOrigin is ConfigurationSource.UserProfileFile;

		return configuredText is not null && comesFromTheProfileFile
			? OutputPathHomeExpansion.Expand(configuredText, homeFolderProvider)
			: new OutputPathExpansion(OutputPathExpansionOutcome.NotApplicable, configuredText ?? string.Empty);
	}

	/// <summary>
	/// Canonicalizes what <see cref="ExpandConfiguredOutputPath"/> left, keeping the text the console will show: the
	/// default is shown as the working folder, an expanded <c>~</c> as the user wrote it, anything else by its supplied form.
	/// </summary>
	private static bool TryCanonicalizeDestinationText(
		string? configuredText,
		OutputPathExpansion expansion,
		TextWriter outputWriter,
		out DocumentationDestinationFolderPath destinationFolder)
	{
		destinationFolder = default;
		bool canonicalized = false;

		try
		{
			destinationFolder = configuredText is null
				? DocumentationDestinationFolderPath.FromResolvedText(
					Directory.GetCurrentDirectory(),
					DocumentationDestinationFolderPath.DefaultConsoleDisplayText)
				: DocumentationDestinationFolderPath.FromResolvedText(
					expansion.Text,
					expansion.Outcome is OutputPathExpansionOutcome.Expanded
						? configuredText
						: DocumentationDestinationFolderPath.DeriveConsoleDisplayText(configuredText));
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
		catch (PathTooLongException)
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
			WriteFormatted(outputWriter, DESTINATION_FOLDER_NOT_WRITABLE_MESSAGE_FORMAT, destinationFolder.ConsoleDisplayText);
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

		if (HasRejectedArguments(parsedCommandLine))
		{
			outputWriter.WriteLine(CommandLineParser.UsageLine);
		}
	}

	private static bool HasRejectedArguments(CommandLineParseResult parsedCommandLine)
	{
		return parsedCommandLine.UnrecognizedTokens.Length > 0 || parsedCommandLine.OptionsMissingValue.Length > 0;
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

	/// <summary>
	/// Says, once a session resumes, that the configuration changed since the saved scan, where the user
	/// can still act on it rather than at <c>report</c>, which is the last stage. It says it ONCE per
	/// session: <c>analyze</c> and <c>report</c> call it too, and stay silent when it was already said.
	/// </summary>
	public static void ReportScanConfigurationChange(
		SessionState state,
		EffectiveConfiguration configuration,
		TextWriter outputWriter)
	{
		ArgumentNullException.ThrowIfNull(state);
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(outputWriter);

		if (state.ScanUsedDifferentLanguagesThan(configuration.EnabledLanguagesInDeclaredOrder)
			&& state.TryMarkScanConfigurationChangeAnnounced())
		{
			outputWriter.WriteLine(SCAN_ENABLED_LANGUAGES_CHANGED_MESSAGE);
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
		catch (PathTooLongException)
		{
			outputWriter.WriteLine(REPOSITORY_FOLDER_PATH_MALFORMED_MESSAGE);
		}

		return canonicalized;
	}

	private static string? DescribeRepositoryProblem(RepositoryFolderPath repositoryFolder, string suppliedText)
	{
		string? problemMessage = null;

		if (File.Exists(repositoryFolder.CanonicalPath))
		{
			problemMessage = Format(REPOSITORY_PATH_NOT_A_FOLDER_MESSAGE_FORMAT, suppliedText);
		}
		else if (!Directory.Exists(repositoryFolder.CanonicalPath))
		{
			problemMessage = Format(REPOSITORY_FOLDER_NOT_FOUND_MESSAGE_FORMAT, suppliedText);
		}
		else if (!IsEnumerable(repositoryFolder))
		{
			problemMessage = Format(REPOSITORY_FOLDER_NOT_READABLE_MESSAGE_FORMAT, suppliedText);
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

			using (FileStream probeStream = RestrictiveFileCreation.CreateStream(probePath, FileMode.Create))
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

	private static string ForConsole(string suppliedText) => ConsoleTextSafety.StripUnsafeCharacters(suppliedText);

	private static string Format(string messageFormat, string argument) =>
		string.Format(CultureInfo.InvariantCulture, messageFormat, argument);
}
