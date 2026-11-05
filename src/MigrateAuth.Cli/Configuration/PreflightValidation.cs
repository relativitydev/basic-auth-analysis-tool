using System.Globalization;

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
/// TWO CHECKS WITH TWO DIFFERENT FAILURE SHAPES.
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
/// AND A THIRD CHECK THAT IS NOT A THIRD BOUNDARY. The write boundary's containment comparison runs
/// here as well, against the destination then in effect, by calling the same primitive
/// (<see cref="WriteBoundary.Evaluate"/>) that the write path calls. With the default destination
/// resolved to the working directory, running the tool from inside the repository under audit -
/// the most natural invocation there is - makes the destination and the repository the same
/// folder. Discovering that immediately before the first write means discovering it after the
/// entire run. The write-time check stays and remains the enforcement point; what the early call
/// removes is the wasted run, not the guarantee.
///
/// EVERY MESSAGE IS A CONSTANT ON THIS CLASS: a literal that may have to change is one edit and
/// one test rather than a search through a binary.
/// </summary>
public static class PreflightValidation
{
	/// <summary>
	/// The degradation message. It states both halves that matter - that a file was
	/// FOUND, and that its structure was not the expected one - which is what distinguishes it
	/// from a missing file, the case that is not an error and says nothing.
	/// </summary>
	public const string CONFIGURATION_FILE_MALFORMED_MESSAGE_FORMAT =
		"The configuration file {0} was found but does not have the expected structure. The built-in default configuration is used instead.";

	/// <summary>
	/// The neighbouring case the degradation message does not name: found, and impossible to open. It degrades
	/// identically and says something different, because telling a user their file is malformed
	/// when it is merely unreadable sends them to edit a file that is already correct.
	/// </summary>
	public const string CONFIGURATION_FILE_UNREADABLE_MESSAGE_FORMAT =
		"The configuration file {0} was found and could not be read. The built-in default configuration is used instead.";

	/// <summary>
	/// Said when the repository's file carries an output path. It names no path, because a path
	/// out of the repository may not be emitted.
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

	/// <summary>What to do about it, said in the same breath as the refusal.</summary>
	public const string NO_LANGUAGE_ENABLED_REMEDY_MESSAGE_FORMAT =
		"Enable at least one language in {0} before starting a session.";

	/// <summary>How the repository's own file is named in output. Never as a path.</summary>
	public const string REPOSITORY_ROOT_LOCATION_LABEL = "in the repository root";

	/// <summary>How the user profile's file is named in output. Never as a path.</summary>
	public const string USER_PROFILE_LOCATION_LABEL = "in the user profile folder";

	private static readonly PreflightVerdict _passed = new()
	{
		Outcome = PreflightOutcome.Passed,
		ExitCode = ExitCode.Success
	};

	/// <summary>
	/// Runs the step. Writes what it found, and answers whether the prompt gets to exist.
	///
	/// The order of the three checks is fixed: the notices first, because they
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
	/// what was wrong with it; and for each file that was applied, a warning per key or language
	/// name this version skips and per key whose value it ignores. The detail is always a separate line,
	/// so the degradation message itself stays the same literal text.
	/// </summary>
	private static void ReportConfigurationFileConditions(
		EffectiveConfiguration configuration,
		TextWriter outputWriter)
	{
		foreach (ConfigurationFileState fileState in configuration.Provenance.ConfigurationFiles)
		{
			if (fileState.RequiresDegradationNotice)
			{
				string messageFormat = fileState.Condition is ConfigurationFileCondition.Malformed
					? CONFIGURATION_FILE_MALFORMED_MESSAGE_FORMAT
					: CONFIGURATION_FILE_UNREADABLE_MESSAGE_FORMAT;

				outputWriter.WriteLine(Format(messageFormat, DescribeLocation(fileState.Location)));
			}

			foreach (ConfigurationFileDiagnostic diagnostic in configuration.FileDiagnostics
				.Where(candidate => candidate.Location == fileState.Location))
			{
				outputWriter.WriteLine(ConfigurationDiagnosticText.Describe(diagnostic));
			}
		}
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
	/// An ignored field is made visible rather than quietly overridden, and is recorded
	/// in the provenance. The provenance reaches a reader only through the report, which is
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
	/// because it is the same code. The refusal names both folders by label, which the verdict
	/// already does.
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

	private static PreflightVerdict? EvaluateEnabledLanguages(
		EffectiveConfiguration configuration,
		TextWriter outputWriter)
	{
		PreflightVerdict? verdict = null;

		if (!configuration.HasAnyEnabledLanguage)
		{
			outputWriter.WriteLine(NO_LANGUAGE_ENABLED_MESSAGE);
			outputWriter.WriteLine(Format(
				NO_LANGUAGE_ENABLED_REMEDY_MESSAGE_FORMAT,
				ConfigurationResolver.CONFIGURATION_FILE_NAME));

			verdict = new PreflightVerdict
			{
				Outcome = PreflightOutcome.NoLanguageEnabled,
				ExitCode = ExitCode.NoLanguageEnabled
			};
		}

		return verdict;
	}

	private static string Format(string messageFormat, string argument) =>
		string.Format(CultureInfo.InvariantCulture, messageFormat, argument);
}
