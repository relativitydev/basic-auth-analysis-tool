using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Configuration;

/// <summary>What Pre-flight Validation concluded. One outcome permits a session; the rest refuse.</summary>
public enum PreflightOutcome
{
	/// <summary>The session may start. Degradation notices may still have been written.</summary>
	Passed = 0,

	/// <summary>
	/// The destination in effect coincides with the repository, contains it, or is
	/// contained by it. Refused here rather than at the first write, which is after the whole run.
	/// </summary>
	WriteBoundaryRefused = 1,

	/// <summary>A configuration that disables all eight languages. No session.</summary>
	NoLanguageEnabled = 2
}

/// <summary>The verdict, and the code the process carries out when the verdict refuses.</summary>
public sealed record PreflightVerdict
{
	/// <summary>What was concluded.</summary>
	public required PreflightOutcome Outcome { get; init; }

	/// <summary>Meaningful only when the verdict refuses; <see cref="ExitCode.Success"/> otherwise.</summary>
	public required ExitCode ExitCode { get; init; }

	/// <summary>Whether the prompt gets to exist.</summary>
	public bool PermitsSession => Outcome is PreflightOutcome.Passed;
}

/// <summary>
/// Pre-flight Validation: one step, run once at session startup, before <c>scan</c>
/// becomes available, and never repeated inside the session.
///
/// It is a STEP AND NOT A STAGE. It is not gated, not cascade-invalidated and not
/// persisted as a completed stage - a session either passed it or does not exist. That is also
/// what makes the effective configuration a session-scoped constant, and therefore what makes the
/// provenance record a statement about the session rather than about whichever stage last
/// read a file.
///
/// TWO CHECKS WITH TWO DIFFERENT FAILURE SHAPES, AND THE ASYMMETRY IS THE DECISION.
///
/// A configuration file that exists and is malformed DEGRADES: the built-in defaults apply, the
/// session says so, and it starts. A broken configuration must not cost the user the ability to
/// run, and the defaults are a safe answer because they are the widest ones.
///
/// A configuration that disables all eight languages REFUSES: no session at all. There is no safe
/// default here and that is the whole reason for the difference - an empty scan produces a report
/// of zero findings, and a report of zero findings is a claim about the client's code that is
/// indistinguishable from a clean repository. A configuration mistake must never be able to
/// produce that claim.
///
/// A MISSING FILE IS NOT AN ERROR at either location and says nothing at all, because nothing was
/// found to be wrong.
///
/// AND A THIRD CHECK THAT IS NOT A THIRD BOUNDARY. The containment comparison of the write boundary runs here
/// as well, against the destination then in effect, by calling the same primitive
/// (<see cref="WriteBoundary.Evaluate"/>) that the write path calls. With the default destination
/// resolved to the working directory, running the tool from inside the repository under audit -
/// the most natural invocation there is - makes the destination and the repository the same
/// folder. Discovering that immediately before the first write means discovering it after the
/// entire run. The write-time check stays and remains the enforcement point; what the early call
/// removes is the wasted run, not the guarantee.
///
/// EVERY MESSAGE IS A CONSTANT ON THIS CLASS, as the gating message is: a literal that may have
/// to change is one edit and one test rather than a search through a binary.
/// </summary>
public static class PreflightValidation
{
	/// <summary>
	/// The degradation message. It states both halves that matter - that a file was
	/// FOUND, and that its structure was not the expected one - which is what distinguishes it
	/// from a missing file, the case that is not an error and says nothing. The second sentence is
	/// the consequence, chosen from the three below by what the OTHER file contributes.
	/// </summary>
	public const string CONFIGURATION_FILE_MALFORMED_MESSAGE_FORMAT =
		"The configuration file {0} was found but does not have the expected structure. {1}";

	/// <summary>
	/// The degradation message for a file above the size this tool reads. It states the limit instead of
	/// calling the structure unexpected, because the structure was never examined: the file was refused
	/// for its size, and the limit is what the user has to act on. {0} is the location label, {1} the
	/// limit as <see cref="DescribeSizeLimit"/> words it, as the User Guide does, and {2} the consequence.
	/// </summary>
	public const string CONFIGURATION_FILE_TOO_LARGE_MESSAGE_FORMAT =
		"The configuration file {0} was found but is larger than the {1} this tool reads, so it was not read. {2}";

	/// <summary>
	/// The neighbouring case the degradation message does not name: found, and impossible to open. It degrades
	/// identically and says something different, because telling a user their file is malformed
	/// when it is merely unreadable sends them to edit a file that is already correct.
	/// </summary>
	public const string CONFIGURATION_FILE_UNREADABLE_MESSAGE_FORMAT =
		"The configuration file {0} was found and could not be read. {1}";

	/// <summary>The consequence when no other configuration file is applied: nothing else supplies a value.</summary>
	public const string DEGRADATION_TO_BUILT_IN_DEFAULTS_MESSAGE =
		"The built-in default configuration is used instead.";

	/// <summary>
	/// The consequence when the repository's file degrades and the user profile's file is applied:
	/// resolution is per setting, so the profile still supplies what it sets.
	/// </summary>
	public const string DEGRADATION_TO_USER_PROFILE_MESSAGE =
		"Each setting is taken from the next source: the configuration file in the user profile folder where it sets one, the built-in default otherwise.";

	/// <summary>
	/// The consequence when the user profile's file degrades and the repository's file is applied:
	/// the repository's file still supplies what it sets.
	/// </summary>
	public const string DEGRADATION_BESIDE_REPOSITORY_FILE_MESSAGE =
		"The built-in defaults take its place for every setting the configuration file in the repository root does not set.";

	/// <summary>
	/// Said when the repository's file carries an output path. It names no path, because a path
	/// out of the repository is exactly what must never be emitted.
	/// </summary>
	public const string REPOSITORY_OUTPUT_PATH_IGNORED_MESSAGE =
		"The configuration file in the repository root sets an output path and is ignored for that field: a repository does not decide where its own assessment is written.";

	/// <summary>
	/// Said when the repository's file carries a default flow. It names no value: the key is never
	/// read from a repository, so what it holds is neither validated nor repeated.
	/// </summary>
	public const string REPOSITORY_DEFAULT_FLOW_IGNORED_MESSAGE =
		"The configuration file in the repository root sets a default flow and is ignored for that field: a repository does not decide which flow its own findings default to.";

	/// <summary>The message for a configuration that enables no language.</summary>
	public const string NO_LANGUAGE_ENABLED_MESSAGE =
		"No languages are configured for processing, so there is nothing to scan and no session is started.";

	/// <summary>
	/// Said when the list that left no language enabled was not empty but made only of names this version
	/// does not know, so the user is not told their list was empty. {0} is the unknown names as the file
	/// wrote them and {1} the names this version accepts.
	/// </summary>
	public const string NO_LANGUAGE_ENABLED_ALL_UNKNOWN_MESSAGE_FORMAT =
		"Every language the enabled languages list names is unknown to this version ({0}), so no language is enabled. The accepted names are {1}.";

	/// <summary>
	/// What to do about it, said in the same breath as the refusal: {0} is the file name and {1} the
	/// location label of the file whose list left no language enabled, so the user knows which one to fix.
	/// </summary>
	public const string NO_LANGUAGE_ENABLED_REMEDY_MESSAGE_FORMAT =
		"Enable at least one language in {0} {1} before starting a session.";

	/// <summary>How the repository's own file is named in output. Never as a path.</summary>
	public const string REPOSITORY_ROOT_LOCATION_LABEL = "in the repository root";

	/// <summary>How the user profile's file is named in output. Never as a path.</summary>
	public const string USER_PROFILE_LOCATION_LABEL = "in the user profile folder";

	private const string _NAME_LIST_SEPARATOR = ", ";
	private const string _SIZE_LIMIT_FORMAT = "{0} bytes ({1} KiB)";
	private const string _THOUSANDS_SEPARATED_FORMAT = "N0";
	private const int _BYTES_PER_KIBIBYTE = 1024;

	private static readonly PreflightVerdict _passed = new()
	{
		Outcome = PreflightOutcome.Passed,
		ExitCode = ExitCode.Success
	};

	/// <summary>
	/// Runs the step. Writes what it found, and answers whether the prompt gets to exist.
	///
	/// The order of the three checks is fixed and reasoned: the notices first, because they
	/// describe the configuration the two refusals are then evaluated against; the containment
	/// comparison next, because a destination this tool may not write to makes every later
	/// question moot; and the language check last.
	/// </summary>
	public static PreflightVerdict Run(
		EffectiveConfiguration configuration,
		RepositoryFolderPath repositoryFolder,
		DocumentationDestinationFolderPath destinationFolder,
		TextWriter outputWriter)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(outputWriter);

		ReportConfigurationFileConditions(configuration, outputWriter);
		ReportRepositoryEffect(configuration.Provenance.RepositoryEffect, outputWriter);
		ReportIgnoredRepositoryFields(configuration.Provenance, outputWriter);

		PreflightVerdict verdict =
			EvaluateWriteBoundary(repositoryFolder, destinationFolder, outputWriter)
			?? EvaluateEnabledLanguages(configuration, outputWriter)
			?? _passed;

		return verdict;
	}

	/// <summary>The label a location is named by in output, which is never its path.</summary>
	public static string DescribeLocation(ConfigurationFileLocation location) =>
		location is ConfigurationFileLocation.RepositoryRoot
			? REPOSITORY_ROOT_LOCATION_LABEL
			: USER_PROFILE_LOCATION_LABEL;

	/// <summary>
	/// The degradation message for each file that degrades, immediately followed by the line that says
	/// what was wrong with it (a file above the size limit has none: its degradation message states the limit); and for each file that was applied, a warning per key or language
	/// name this version skips and per key whose value it ignores. The detail is always a separate line,
	/// so the degradation message itself stays the same literal text.
	/// </summary>
	private static void ReportConfigurationFileConditions(
		EffectiveConfiguration configuration,
		TextWriter outputWriter)
	{
		foreach (ConfigurationFileState fileState in configuration.Provenance.ConfigurationFiles)
		{
			ConfigurationFileDiagnostic[] diagnostics =
			[
				.. configuration.FileDiagnostics.Where(candidate => candidate.Location == fileState.Location)
			];
			bool tooLarge = diagnostics.Any(candidate => candidate.Kind is ConfigurationDiagnosticKind.TooLarge);

			if (fileState.RequiresDegradationNotice)
			{
				outputWriter.WriteLine(DescribeDegradation(configuration, fileState, tooLarge));
			}

			foreach (ConfigurationFileDiagnostic diagnostic in diagnostics
				.Where(candidate => candidate.Kind is not ConfigurationDiagnosticKind.TooLarge))
			{
				outputWriter.WriteLine(ConfigurationDiagnosticText.Describe(diagnostic));
			}
		}
	}

	/// <summary>The limit in bytes with thousands separators and in KiB, such as "262,144 bytes (256 KiB)".</summary>
	private static string DescribeSizeLimit(int limitInBytes)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			_SIZE_LIMIT_FORMAT,
			limitInBytes.ToString(_THOUSANDS_SEPARATED_FORMAT, CultureInfo.InvariantCulture),
			limitInBytes / _BYTES_PER_KIBIBYTE);
	}

	/// <summary>
	/// The degradation message of one file: for a file above the size limit, the limit; otherwise
	/// the malformed or unreadable message. The consequence is the same sentence in each.
	/// </summary>
	private static string DescribeDegradation(
		EffectiveConfiguration configuration,
		ConfigurationFileState fileState,
		bool tooLarge)
	{
		string consequence = DescribeDegradationConsequence(fileState.Location, configuration.Provenance.ConfigurationFiles);
		string location = DescribeLocation(fileState.Location);

		return tooLarge
			? string.Format(
				CultureInfo.InvariantCulture,
				CONFIGURATION_FILE_TOO_LARGE_MESSAGE_FORMAT,
				location,
				DescribeSizeLimit(ConfigurationFileReader.MAXIMUM_CONFIGURATION_FILE_LENGTH_IN_BYTES),
				consequence)
			: string.Format(
				CultureInfo.InvariantCulture,
				fileState.Condition is ConfigurationFileCondition.Malformed
					? CONFIGURATION_FILE_MALFORMED_MESSAGE_FORMAT
					: CONFIGURATION_FILE_UNREADABLE_MESSAGE_FORMAT,
				location,
				consequence);
	}

	/// <summary>
	/// What the degradation means for the settings, which depends on the other file: a file that
	/// was applied still supplies its settings, so "the built-in default configuration is used" is
	/// true only when no other file was applied.
	/// </summary>
	private static string DescribeDegradationConsequence(
		ConfigurationFileLocation degradedLocation,
		ImmutableArray<ConfigurationFileState> configurationFiles)
	{
		ConfigurationFileLocation otherLocation = degradedLocation is ConfigurationFileLocation.RepositoryRoot
			? ConfigurationFileLocation.UserProfile
			: ConfigurationFileLocation.RepositoryRoot;
		bool otherFileApplied = configurationFiles.Any(candidate =>
			candidate.Location == otherLocation && candidate.Condition is ConfigurationFileCondition.Accepted);
		string consequence = DEGRADATION_TO_BUILT_IN_DEFAULTS_MESSAGE;

		if (otherFileApplied)
		{
			consequence = degradedLocation is ConfigurationFileLocation.RepositoryRoot
				? DEGRADATION_TO_USER_PROFILE_MESSAGE
				: DEGRADATION_BESIDE_REPOSITORY_FILE_MESSAGE;
		}

		return consequence;
	}

	/// <summary>
	/// What the repository's own file changed, said where the user can still act on it. The
	/// repository's file may narrow its own assessment, and the report that records it is written
	/// at the END of a run, so a narrowing is also said here, at the start. The console sentence
	/// and the report's are the same text from the same record.
	/// </summary>
	private static void ReportRepositoryEffect(
		RepositoryConfigurationEffect repositoryEffect,
		TextWriter outputWriter)
	{
		foreach (string statement in RepositoryConfigurationEffectText.StatementsFor(repositoryEffect))
		{
			outputWriter.WriteLine(statement);
		}
	}

	/// <summary>
	/// Says each ignored field of the repository's file, which is also recorded in the provenance.
	/// The provenance reaches a reader only through the report, which is
	/// written at the END of a run - so it is also said here, where the user can still act on it.
	/// The console line and the provenance entry are the same fact from the same record; neither
	/// is derived from the file's raw text.
	/// </summary>
	private static void ReportIgnoredRepositoryFields(
		ConfigurationProvenance provenance,
		TextWriter outputWriter)
	{
		foreach (IgnoredRepositoryConfigurationField ignoredField in provenance.IgnoredRepositoryFields)
		{
			outputWriter.WriteLine(ignoredField.Field is ConfigurationField.OutputPath
				? REPOSITORY_OUTPUT_PATH_IGNORED_MESSAGE
				: REPOSITORY_DEFAULT_FLOW_IGNORED_MESSAGE);
		}
	}

	/// <summary>
	/// The write-boundary call site. It is <see cref="WriteBoundary.Evaluate"/> and not a comparison of its
	/// own: one implementation, and an early check that cannot disagree with the enforcement point
	/// because it is the same code. The refusal names the repository by label and the destination as the user supplied it,
	/// which the verdict already does.
	/// </summary>
	private static PreflightVerdict? EvaluateWriteBoundary(
		RepositoryFolderPath repositoryFolder,
		DocumentationDestinationFolderPath destinationFolder,
		TextWriter outputWriter)
	{
		WriteBoundaryVerdict boundaryVerdict = WriteBoundary.Evaluate(repositoryFolder, destinationFolder);
		PreflightVerdict? verdict = null;

		if (!boundaryVerdict.PermitsWriting)
		{
			outputWriter.WriteLine(boundaryVerdict.RefusalMessage);

			verdict = new PreflightVerdict
			{
				Outcome = PreflightOutcome.WriteBoundaryRefused,
				ExitCode = WriteBoundary.REFUSAL_EXIT_CODE
			};
		}

		return verdict;
	}

	/// <summary>
	/// When the file that emptied the list also named languages this version skipped, every name it listed
	/// was unknown - a known name would have stayed enabled - so the refusal says that, names them, and
	/// names the accepted ones. A list that was empty to begin with has no skipped name and says nothing here.
	/// </summary>
	private static void ReportListMadeOnlyOfUnknownNames(
		EffectiveConfiguration configuration,
		ConfigurationFileLocation emptyingLocation,
		TextWriter outputWriter)
	{
		string[] unknownNames =
		[
			.. configuration.FileDiagnostics
				.Where(candidate => candidate.Location == emptyingLocation
					&& candidate.Kind is ConfigurationDiagnosticKind.UnknownLanguage)
				.Select(candidate => candidate.Subject)
		];

		if (unknownNames.Length > 0)
		{
			outputWriter.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				NO_LANGUAGE_ENABLED_ALL_UNKNOWN_MESSAGE_FORMAT,
				string.Join(_NAME_LIST_SEPARATOR, unknownNames.Select(name => $"\"{name}\"")),
				string.Join(_NAME_LIST_SEPARATOR, AssessedLanguages.AllInDeclaredOrder.Select(AssessedLanguages.CanonicalName))));
		}
	}

	private static PreflightVerdict? EvaluateEnabledLanguages(
		EffectiveConfiguration configuration,
		TextWriter outputWriter)
	{
		PreflightVerdict? verdict = null;

		if (!configuration.HasAnyEnabledLanguage)
		{
			outputWriter.WriteLine(NO_LANGUAGE_ENABLED_MESSAGE);
			ConfigurationFileLocation emptyingLocation =
				configuration.Provenance.EnabledLanguagesOrigin is ConfigurationSource.RepositoryFile
					? ConfigurationFileLocation.RepositoryRoot
					: ConfigurationFileLocation.UserProfile;

			ReportListMadeOnlyOfUnknownNames(configuration, emptyingLocation, outputWriter);

			outputWriter.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				NO_LANGUAGE_ENABLED_REMEDY_MESSAGE_FORMAT,
				ConfigurationResolver.CONFIGURATION_FILE_NAME,
				DescribeLocation(emptyingLocation)));

			verdict = new PreflightVerdict
			{
				Outcome = PreflightOutcome.NoLanguageEnabled,
				ExitCode = ExitCode.NoLanguageEnabled
			};
		}

		return verdict;
	}
}
