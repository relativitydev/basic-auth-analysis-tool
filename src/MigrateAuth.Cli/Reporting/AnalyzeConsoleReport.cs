using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE FINDINGS TABLE, THE CONFIDENCE SUMMARY AND THE CREDENTIAL WARNINGS, ALL THREE OWNED BY <c>analyze</c> RATHER THAN
/// <c>report</c>.
///
/// <see cref="Pipeline.AnalyzeStage"/> calls this type once, at the end of a completed run.
///
/// THE TABLE IS RENDERED OVER THE CONFIGURED THRESHOLD, THE SAME ONE THE REPORT APPLIES. This type
/// calls <see cref="ThresholdFilter"/> rather than filtering on its own terms, so the console table
/// and the report cannot disagree about which findings they show. Composition's full, unfiltered
/// set still reaches <see cref="Session.SessionState"/> unfiltered; this type only ever reads what
/// composition already produced.
///
/// THE TABLE AND THE WARNING LIST ARE EACH BOUNDED AT
/// <see cref="ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS"/> ROWS. A console is read by a
/// person watching a terminal scroll, not by a parser, and a hundred-thousand-finding
/// volume would otherwise turn one `analyze` run into that many lines of unreadable output.
/// The report carries every row without a bound, and both surfaces say so when they truncate.
///
/// THE WARNING LIST WALKS THE FULL SET, NEVER THE FILTERED ONE. A credential written into source
/// is a risk at every confidence level, so a warning can name a finding the threshold hides from
/// the table right above it - that warning says so, so "the report lists them all" is never
/// claimed of a set the report does not hold.
///
/// NEITHER SURFACE PRINTS THE SECRET ITSELF. The warning line carries the finding's location and
/// pattern class only. The path is escaped the same way <see cref="ConsoleFindingsTable"/> escapes
/// it, through <see cref="EmittedPathText"/>.
/// </summary>
public static class AnalyzeConsoleReport
{
	/// <summary>The confidence summary, over the full set composition produced.</summary>
	public const string FINDINGS_BY_CONFIDENCE_MESSAGE_FORMAT =
		"By confidence: High {0:N0}, Medium {1:N0}, Low {2:N0}.";

	/// <summary>Said only when the configured threshold actually hides at least one finding from the table.</summary>
	public const string THRESHOLD_HIDES_FINDINGS_MESSAGE_FORMAT =
		"Showing {0:N0} of {1:N0} findings at minimum confidence level {2}.";

	/// <summary>
	/// Appended to <see cref="THRESHOLD_HIDES_FINDINGS_MESSAGE_FORMAT"/> - and to the report stage's own
	/// twin of it - when the threshold that hides the rows came from the repository's own configuration
	/// file, so a reader is not left to assume that they chose it.
	/// </summary>
	public const string THRESHOLD_SET_BY_REPOSITORY_SUFFIX =
		" The configuration file in the repository root set this minimum.";

	/// <summary>The table's own row-cap pointer, said only past <see cref="ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS"/>.</summary>
	public const string FURTHER_FINDINGS_NOT_SHOWN_MESSAGE_FORMAT =
		"... {0:N0} further findings are not shown here; the report lists every finding.";

	/// <summary>The warning, one per literal-credential finding, in the report's own order.</summary>
	public const string LITERAL_CREDENTIAL_WARNING_MESSAGE_FORMAT =
		"Warning: {0} {1} {2}: a credential appears to be written into the source code. Treat it as compromised and rotate it. See " + SecretManagementPointer.PART_B_NAME + ".";

	/// <summary>
	/// Appended to a warning line whose finding falls below the
	/// configured minimum confidence level: the warning walks the full set and the threshold does
	/// not apply to it, so a reader who compares this list against the report needs to know which
	/// warnings name a finding the report does not list.
	/// </summary>
	public const string BELOW_REPORTING_THRESHOLD_SUFFIX = " (below the reporting threshold; not in the report)";

	/// <summary>
	/// The warning list's own row-cap pointer, at the same bound the table uses. It
	/// names how many of the omitted warnings are actually in the report, because
	/// past the row cap "the report lists them all" is no longer true when the threshold has already
	/// hidden some of them from it.
	/// </summary>
	public const string FURTHER_LITERAL_CREDENTIAL_WARNINGS_MESSAGE_FORMAT =
		"... {0:N0} further literal-credential warnings are not shown here; {1:N0} of them are in the report.";

	private const string _LOCATION_FORMAT = "{0}:{1}";

	/// <summary>
	/// The whole console surface `analyze` owns, in the fixed order of the requirements:
	/// the confidence totals, the capped table over the
	/// threshold, and the capped warning list over the full set.
	/// </summary>
	public static void Render(
		TextWriter writer,
		ImmutableArray<Finding> findings,
		LanguageCatalog catalog,
		ConfidenceLevel minimumConfidenceLevel,
		bool thresholdSetByRepository = false)
	{
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(catalog);

		WriteConfidenceTotals(writer, findings);

		if (findings.Length > 0)
		{
			RenderThresholdNote(writer, findings, minimumConfidenceLevel, thresholdSetByRepository);
			RenderCappedTable(writer, findings, catalog, minimumConfidenceLevel);
			RenderLiteralCredentialWarnings(writer, findings, catalog, minimumConfidenceLevel);
		}
	}

	private static void WriteConfidenceTotals(TextWriter writer, ImmutableArray<Finding> findings)
	{
		int highCount = findings.Count(finding => finding.Level == ConfidenceLevel.High);
		int mediumCount = findings.Count(finding => finding.Level == ConfidenceLevel.Medium);
		int lowCount = findings.Count(finding => finding.Level == ConfidenceLevel.Low);

		writer.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			FINDINGS_BY_CONFIDENCE_MESSAGE_FORMAT,
			highCount,
			mediumCount,
			lowCount));
	}

	private static void RenderThresholdNote(
		TextWriter writer,
		ImmutableArray<Finding> findings,
		ConfidenceLevel minimumConfidenceLevel,
		bool thresholdSetByRepository)
	{
		int reportedCount = ThresholdFilter.CountReported(findings, minimumConfidenceLevel);

		if (reportedCount < findings.Length)
		{
			string note = string.Format(
				CultureInfo.InvariantCulture,
				THRESHOLD_HIDES_FINDINGS_MESSAGE_FORMAT,
				reportedCount,
				findings.Length,
				ConfidenceLevels.Name(minimumConfidenceLevel));

			writer.WriteLine(thresholdSetByRepository ? $"{note}{THRESHOLD_SET_BY_REPOSITORY_SUFFIX}" : note);
		}
	}

	private static void RenderCappedTable(
		TextWriter writer,
		ImmutableArray<Finding> findings,
		LanguageCatalog catalog,
		ConfidenceLevel minimumConfidenceLevel)
	{
		int reportedCount = ThresholdFilter.CountReported(findings, minimumConfidenceLevel);
		IEnumerable<Finding> reported = ThresholdFilter.Apply(findings, minimumConfidenceLevel);

		ConsoleFindingsTable.Render(
			writer,
			reported.Take(ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS),
			catalog);

		if (reportedCount > ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS)
		{
			writer.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				FURTHER_FINDINGS_NOT_SHOWN_MESSAGE_FORMAT,
				reportedCount - ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS));
		}
	}

	private static void RenderLiteralCredentialWarnings(
		TextWriter writer,
		ImmutableArray<Finding> findings,
		LanguageCatalog catalog,
		ConfidenceLevel minimumConfidenceLevel)
	{
		ImmutableArray<Finding> credentialFindings =
			[.. findings.Where(finding => finding.LiteralCredentialSignal)];

		foreach (Finding finding in credentialFindings.Take(ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS))
		{
			writer.WriteLine(BuildWarningLine(finding, catalog, minimumConfidenceLevel));
		}

		if (credentialFindings.Length > ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS)
		{
			WriteWarningOverflowPointer(writer, credentialFindings, minimumConfidenceLevel);
		}
	}

	/// <summary>
	/// The omitted count, and how many of those omitted warnings name a finding that is actually in
	/// the report, since the threshold may already have hidden some of them from it.
	/// </summary>
	private static void WriteWarningOverflowPointer(
		TextWriter writer,
		ImmutableArray<Finding> credentialFindings,
		ConfidenceLevel minimumConfidenceLevel)
	{
		int omittedCount = credentialFindings.Length - ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS;
		int omittedInReportCount = credentialFindings
			.Skip(ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS)
			.Count(finding => ThresholdFilter.IsReported(finding, minimumConfidenceLevel));

		writer.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			FURTHER_LITERAL_CREDENTIAL_WARNINGS_MESSAGE_FORMAT,
			omittedCount,
			omittedInReportCount));
	}

	private static string BuildWarningLine(Finding finding, LanguageCatalog catalog, ConfidenceLevel minimumConfidenceLevel)
	{
		string location = string.Format(
			CultureInfo.InvariantCulture,
			_LOCATION_FORMAT,
			EmittedPathText.ForDestination(finding.Path, EmissionDestination.Terminal),
			finding.Line);

		string warningLine = string.Format(
			CultureInfo.InvariantCulture,
			LITERAL_CREDENTIAL_WARNING_MESSAGE_FORMAT,
			finding.Identifier,
			location,
			ReportVocabulary.PatternClassText(catalog, finding));

		return ThresholdFilter.IsReported(finding, minimumConfidenceLevel) ? warningLine : $"{warningLine}{BELOW_REPORTING_THRESHOLD_SUFFIX}";
	}
}
