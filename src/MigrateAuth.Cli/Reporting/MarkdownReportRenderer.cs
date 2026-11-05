using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// The report: five sections, in this order, always.
///
/// Every section is present when empty. A report that omits a section it has nothing to say about is
/// indistinguishable from one where that stage did not run: a missing "skipped items" heading and a
/// "skipped items: none" heading say opposite things to a reader. There is no branch below that omits
/// a heading.
///
/// The order is by what a reader needs first, not by pipeline order. Coverage leads because in
/// the zero-findings case it is the only section worth reading. Skipped items precede the findings
/// because they bound what the findings can be trusted to cover. Provenance is last because it is
/// read when comparing two reports rather than when reading one.
///
/// It writes into the stream and builds no document. Every recommendation is rendered at the
/// moment it is written and released immediately, so nothing here accumulates.
///
/// The confidence threshold is applied here and the numbers are not: identifiers were assigned over
/// the full set before it, so a filtered report shows gaps in its sequence.
///
/// Every piece of repository-chosen text is escaped for Markdown on the way in - paths through
/// <see cref="EmittedPathText.ForDestination"/> and the repository label through its sibling. A file
/// name is chosen by the repository and may carry markup that closes a code fence, opens a link or
/// starts a table row.
/// </summary>
public static class MarkdownReportRenderer
{
	/// <summary>The document's title. It carries no date.</summary>
	public const string REPORT_TITLE = "# Basic Authentication assessment";

	/// <summary>
	/// The findings summary table's heading. It is not one of the report's five sections and carries no
	/// number: it sits ahead of them, right after the title, so the five keep their numbers and their
	/// order, and a reader meets the list of findings before the long per-finding entries of section 4.
	/// </summary>
	public const string FINDINGS_SUMMARY_HEADING = "## Findings summary";

	/// <summary>Said above the summary table, so the table is read as an index of section 4 and not as a second list.</summary>
	public const string FINDINGS_SUMMARY_INTRODUCTION =
		"Every finding reported in section 4, in the same order and with the same identifiers. Section 4 gives each finding's recommendation.";

	/// <summary>Report section 1.</summary>
	public const string COVERAGE_HEADING = "## 1. Coverage";

	/// <summary>Report section 2.</summary>
	public const string SCAN_SUMMARY_HEADING = "## 2. Scan summary";

	/// <summary>Report section 3.</summary>
	public const string SKIPPED_ITEMS_HEADING = "## 3. Skipped items";

	/// <summary>Report section 4.</summary>
	public const string FINDINGS_HEADING = "## 4. Findings";

	/// <summary>Report section 5.</summary>
	public const string PROVENANCE_HEADING = "## 5. Provenance";

	/// <summary>The first destination-certainty group, named for what is known about it.</summary>
	public const string ESTABLISHED_GROUP_HEADING = "### Destination established as Relativity";

	/// <summary>
	/// The second destination-certainty group. It is "not established" and never "not Relativity": the
	/// engine establishes nothing about these findings in either direction, and a client who reads them
	/// as out of scope discards work that may not be.
	/// </summary>
	public const string NOT_ESTABLISHED_GROUP_HEADING = "### Destination not established";

	/// <summary>Said under a group heading that has nothing under it, so the heading is not bare.</summary>
	public const string NO_FINDINGS_IN_GROUP = "No findings in this group.";

	/// <summary>Said when the scan enumerated nothing, which is a first-class case.</summary>
	public const string NO_FILES_ENUMERATED = "No files were enumerated.";

	/// <summary>Said in section 3 when a category enumerated no paths.</summary>
	public const string NO_PATHS_ENUMERATED = "None.";

	/// <summary>
	/// The coverage section's third piece, after the per-language table: what a
	/// supported language's own catalog declares it does not attempt to recognize, and why.
	/// </summary>
	public const string DECLARED_UNSUPPORTED_CONSTRUCTS_HEADING = "### Declared unsupported constructs";

	/// <summary>Said under <see cref="DECLARED_UNSUPPORTED_CONSTRUCTS_HEADING"/> when no supported language declares one.</summary>
	public const string NO_DECLARED_UNSUPPORTED_CONSTRUCTS = "None declared.";

	/// <summary>Said when the repository's own configuration file asked for nothing that was refused.</summary>
	public const string NO_IGNORED_CONFIGURATION_FIELDS = "None.";

	/// <summary>
	/// What stands where the summary table would, and repeats in section 4, when nothing is reported. A
	/// count of zero on its own reads as a clean result; the segments that follow it say what was not
	/// looked at, which is the part a reader needs.
	/// </summary>
	public const string NO_FINDINGS_REPORTED = "0 findings";

	/// <summary>A segment of <see cref="NO_FINDINGS_REPORTED"/>'s sentence: files in languages with no analyzer in this release.</summary>
	public const string NOT_ASSESSED_SEGMENT_FORMAT = "{0:N0} {1} in languages this release does not assess ({2})";

	/// <summary>A segment of <see cref="NO_FINDINGS_REPORTED"/>'s sentence: files in languages the configuration turned off.</summary>
	public const string DISABLED_BY_CONFIGURATION_SEGMENT_FORMAT = "{0:N0} {1} in languages disabled by configuration ({2})";

	/// <summary>The same segment when the repository's own configuration file is where the enabled languages came from.</summary>
	public const string DISABLED_BY_REPOSITORY_SEGMENT_FORMAT =
		"{0:N0} {1} in languages disabled by the repository's configuration file ({2})";

	/// <summary>A segment of <see cref="NO_FINDINGS_REPORTED"/>'s sentence: findings recognized and hidden by the threshold.</summary>
	public const string BELOW_THRESHOLD_SEGMENT_FORMAT =
		"{0:N0} recognized {1} below the minimum confidence level ({2}) and not shown";

	/// <summary>Section 2's line for the folders the walk does not enter. Always written, with a zero, like every count of the report.</summary>
	public const string EXCLUDED_FOLDERS_FORMAT =
		"Folders excluded from the walk and not read: {0:N0}. They are excluded by design and are not skipped items.";

	private const string _SCAN_TOTALS_FORMAT = "Files enumerated: {0:N0}. Lines counted: {1:N0}. Candidate files: {2:N0}.";

	/// <summary>Section 2's "Assessed" column, for a row whose language carries a registered analyzer.</summary>
	public const string ASSESSED_YES = "yes";

	/// <summary>Section 2's "Assessed" column, for a row whose language does not - see <see cref="LanguageAssessment"/>.</summary>
	public const string ASSESSED_NO = "no - no analyzer in this release";

	/// <summary>
	/// Section 2's "Assessed" column for the
	/// <see cref="Scanning.LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME"/> row: it was never
	/// searched at all, which is a third state and not the same fact as
	/// <see cref="ASSESSED_NO"/>'s "a detected language this release does not assess".
	/// </summary>
	public const string ASSESSED_NOT_APPLICABLE = "n/a - no language detected";

	/// <summary>
	/// Section 2's "Assessed" column, for a row whose language this session's configuration
	/// disabled - distinct from <see cref="ASSESSED_NO"/> even where no analyzer exists either, on
	/// the same precedence <see cref="LanguageAssessmentState.DisabledByConfiguration"/> declares.
	/// </summary>
	public const string ASSESSED_DISABLED = "no - disabled by configuration";

	/// <summary>
	/// Said once after Section 2's table, on the same condition and in the same words as
	/// <see cref="Scanning.ScanStage.UNASSESSED_LANGUAGES_SUMMARY_FORMAT"/> - the two are declared
	/// apart because each renderer already owns its own Section 2 vocabulary, not because the two
	/// sentences are free to drift.
	/// </summary>
	public const string UNASSESSED_LANGUAGES_SUMMARY_FORMAT =
		"Files in languages this release does not assess: {0:N0} of {1:N0}. They are counted here and not analyzed.";

	/// <summary>
	/// Said once after Section 2's table, on the same condition and in the same words as
	/// <see cref="Scanning.ScanStage.DISABLED_LANGUAGES_SUMMARY_FORMAT"/>.
	/// </summary>
	public const string DISABLED_LANGUAGES_SUMMARY_FORMAT =
		"Files in languages disabled by configuration: {0:N0} of {1:N0}. They are counted here and not analyzed.";

	/// <summary>
	/// Said once in Section 2 when the persisted, scan-time enabled set -
	/// <see cref="Reporting.ReportContent.ScanEnabledLanguages"/> - differs from this session's
	/// current configuration, so a reader does not silently read Section 2's per-row state as
	/// current when it describes the last completed scan instead. The console twin of
	/// <see cref="Pipeline.ReportStage.SCAN_ENABLED_LANGUAGES_STALE_MESSAGE"/>.
	/// </summary>
	public const string SCAN_ENABLED_LANGUAGES_STALE_MESSAGE =
		"The scan used a different set of enabled languages than this session; re-run 'scan' to apply the current configuration.";

	/// <summary>
	/// The aggregate. Unlike the per-category table beside it, this sum does not distinguish
	/// <see cref="Reading.SkippedItemCategory.MalformedMatch"/> from the other file-counted categories,
	/// and a file counted under it is not necessarily empty of findings - see
	/// <see cref="Reading.SkippedItemLedger"/>. The per-category table is where that distinction is
	/// visible.
	/// </summary>
	private const string _SKIPPED_TOTALS_FORMAT =
		"Skipped files: {0:N0}. Directories whose contents could not be listed: {1:N0}. The two are never added: a directory whose contents could not be listed is a hole of unknown size.";

	private const string _FINDINGS_TOTAL_FORMAT =
		"Findings reported: {0:N0} of {1:N0} recognized. Minimum confidence level reported: {2}. Identifiers are assigned over the full set before this filter, so a filtered report shows gaps.";

	/// <summary>
	/// The truncated case's own version of <see cref="_FINDINGS_TOTAL_FORMAT"/>. The
	/// untruncated sentence's second number is the same as <see cref="Findings.FindingsCapOutcome.TotalRecognizedCount"/>
	/// because nothing was capped; once something is, <c>content.Findings.Length</c> is the RETAINED
	/// count and no longer that total, so this format names both numbers rather than letting one word,
	/// "recognized", stand for two different counts across the two lines that follow it.
	/// </summary>
	private const string _FINDINGS_TOTAL_TRUNCATED_FORMAT =
		"Findings reported: {0:N0} of {1:N0} kept after the findings caps, out of {2:N0} recognized. Minimum confidence level reported: {3}. Identifiers are assigned over the full set before this filter, so a filtered report shows gaps.";

	private const string _TRUNCATION_FORMAT = "{0:N0} further {1} are counted above and not listed.";

	/// <summary>
	/// The truncation declaration, said when either cap removed anything. Never "the result is
	/// complete".
	/// </summary>
	public const string FINDINGS_CAPPED_FORMAT =
		"Findings capped: this result is TRUNCATED. {0:N0} recognized construct(s) of {1:N0} were omitted - {2:N0} by the per-file cap across {3:N0} file(s), {4:N0} by the global cap. Do not read this report as complete.";

	/// <summary>
	/// The "state zero" convention applied to the findings caps: said when neither cap tripped. Note:
	/// "below" rather than "above" - this line is written before the finding groups it describes,
	/// never after them.
	/// </summary>
	public const string FINDINGS_NOT_CAPPED = "Findings capped: no. Every recognized construct is included below.";

	private const string _SUMMARY_TABLE_HEADING_ROW = "| ID | File:Line | Language | Pattern | Confidence | Flow |";
	private const string _SUMMARY_TABLE_RULE_ROW = "| --- | --- | --- | --- | --- | --- |";
	private const string _SUMMARY_ROW_FORMAT = "| {0} | {1}:{2} | {3} | {4} | {5} | {6} |";
	private const string _SENTENCE_SEGMENT_SEPARATOR = "; ";
	private const string _SENTENCE_END = ".";
	private const string _FILE_WORD = "file";
	private const string _FILES_WORD = "files";
	private const string _FINDING_IS_WORD = "finding is";
	private const string _FINDINGS_ARE_WORD = "findings are";
	private const string _FOLDERS_WORD = "folders";
	private const string _TABLE_CELL_BAR = "|";
	private const string _ESCAPED_TABLE_CELL_BAR = "\\|";
	private const string _TABLE_CELL_LINE_ENDING_REPLACEMENT = " ";
	private const string _LOW_REASON_ROW_FORMAT = "- {0}: {1}";
	private const string _COVERAGE_TABLE_HEADING_ROW = "| Language | Coverage |";
	private const string _COVERAGE_TABLE_RULE_ROW = "| --- | --- |";
	private const string _INVENTORY_TABLE_HEADING_ROW = "| Language | Files | Lines | Candidates | Share of files | Assessed |";
	private const string _INVENTORY_TABLE_RULE_ROW = "| --- | ---: | ---: | ---: | ---: | --- |";
	private const string _SKIPPED_TABLE_HEADING_ROW = "| Category | Unit | Count |";
	private const string _SKIPPED_TABLE_RULE_ROW = "| --- | --- | ---: |";

	/// <summary>
	/// The run date in the provenance record, labeled "(local)" because the value is the local calendar
	/// date of the run - the same date <see cref="Writing.OutputArtifactNaming"/> stamps into the
	/// file name a reader is already looking at, and never a value converted to any other zone.
	/// </summary>
	private const string _RUN_DATE_LABEL = "Run date (local)";

	private const string _TOOL_VERSION_LABEL = "Tool version";
	private const string _CATALOG_VERSION_LABEL = "Catalog version";
	private const string _REPOSITORY_LABEL = "Repository assessed";
	/// <summary>
	/// "Languages enabled", not "Languages assessed": this reflects
	/// configuration - <see cref="Configuration.LanguageEnablement"/> - and not which languages
	/// carry a registered analyzer in this build, which is Section 2's own "Assessed" column.
	/// </summary>
	private const string _LANGUAGES_ENABLED_LABEL = "Languages enabled";
	private const string _MINIMUM_LEVEL_LABEL = "Minimum confidence level reported";
	private const string _ENABLED_LANGUAGES_ORIGIN_LABEL = "Enabled languages resolved from";
	private const string _OUTPUT_DESTINATION_ORIGIN_LABEL = "Output destination resolved from";
	private const string _DEFAULT_FLOW_LABEL = "Default flow";
	private const string _DEFAULT_FLOW_ORIGIN_FORMAT = "{0} (from {1})";
	private const string _CONFIGURATION_FILE_LABEL_FORMAT = "Configuration file, {0}";
	private const string _APPLIED_CONFIGURATION_LABEL = "Repository configuration applied";
	private const string _IGNORED_CONFIGURATION_LABEL = "Repository configuration ignored";
	private const string _IGNORED_CONFIGURATION_LABEL_FORMAT = "Repository configuration ignored, {0}";

	/// <summary>
	/// The line ending both artifacts are written with, on every platform.
	///
	/// Identical content and identical catalog must produce identical output, and a writer left on its
	/// platform default emits CRLF on one operating system and LF on another - so the same repository
	/// would produce two files that differ in every line.
	/// </summary>
	private const string _PORTABLE_LINE_ENDING = "\n";

	private const string _COVERAGE_ROW_FORMAT = "| {0} | {1} |";
	private const string _DECLARED_UNSUPPORTED_LANGUAGE_FORMAT = "- {0}:";
	private const string _DECLARED_UNSUPPORTED_ENTRY_FORMAT = "  - {0}: {1}";
	private const string _INVENTORY_ROW_FORMAT = "| {0} | {1:N0} | {2:N0} | {3:N0} | {4:F1} % | {5} |";
	private const string _SKIPPED_ROW_FORMAT = "| {0} | {1} | {2:N0} |";
	private const string _FINDING_HEADING_FORMAT = "#### {0} - {1}:{2} - {3} - {4} - {5}";
	private const string _PROVENANCE_ROW_FORMAT = "- {0}: {1}";
	private const string _RECOMMENDATION_ROW_FORMAT = "- {0}: {1}";

	/// <summary>
	/// The flow a High or Medium finding targets and why it was assigned, beside the
	/// finding and outside the recommendation text.
	/// </summary>
	private const string _FLOW_ROW_FORMAT = "- Flow: {0} (decision: {1})";

	/// <summary>
	/// The full flow note of the finding's (decision, flow), beside the finding. The
	/// report carries the whole note for every finding; only the Suggest console abbreviates it to a key.
	/// </summary>
	private const string _FLOW_NOTE_ROW_FORMAT = "- Flow note: {0}";

	/// <summary>
	/// The fence-first-line case: the label alone, with no value on the same line - the
	/// value's first physical line is written as the item's first continuation line instead. See
	/// <see cref="WriteLabeledListItem"/>.
	/// </summary>
	private const string _RECOMMENDATION_LABEL_ONLY_FORMAT = "- {0}:";

	private const string _BULLET_FORMAT = "- {0}";
	private const string _PATH_LIST_ITEM_FORMAT = "  - {0}";
	private const string _VALUE_SEPARATOR = ", ";
	private const string _NONE_VALUE = "none";

	/// <summary>
	/// A template piece's own paragraph and code-fence breaks arrive as this
	/// character inside the field's text - the templates are written in C# source with a literal
	/// `\n`, never `Environment.NewLine` - so splitting on it is what finds every continuation line
	/// a field carries, on any platform this builds on.
	/// </summary>
	private const char _TEMPLATE_LINE_BREAK = '\n';

	/// <summary>
	/// CommonMark's list-item content column for the `"- "` marker this report writes:
	/// marker width 1 plus the single space that follows it, so a continuation line must be
	/// indented at least this far to remain part of the same list item rather than starting a new
	/// block at column 0. Applied uniformly - including to an otherwise-blank continuation line,
	/// which stays "blank" under CommonMark's own definition (a line of only spaces or tabs still
	/// counts as blank) so this indentation never turns a paragraph break into content.
	/// </summary>
	private const string _LIST_ITEM_CONTENT_INDENT = "  ";

	/// <summary>
	/// Writes the whole report. The five section calls are the decision, so they are visible as
	/// five lines rather than buried in a loop over a collection of section objects.
	/// </summary>
	public static void Render(
		TextWriter writer,
		ReportContent content,
		IGenericRecommendationProvider genericRecommendations)
	{
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(content);
		ArgumentNullException.ThrowIfNull(genericRecommendations);

		RecommendationRenderer recommendations = new(content.Catalog, genericRecommendations);

		writer.NewLine = _PORTABLE_LINE_ENDING;

		writer.WriteLine(REPORT_TITLE);
		writer.WriteLine();

		WriteFindingsSummary(writer, content);
		WriteCoverage(writer, content);
		WriteScanSummary(writer, content);
		WriteSkippedItems(writer, content);
		WriteFindings(writer, content, recommendations);
		WriteProvenance(writer, content);
	}

	/// <summary>
	/// The index of what section 4 lists: one row per reported finding, walked from the same filtered
	/// sequence section 4 and the data artifact walk, so the three cannot disagree about which findings
	/// exist or in what order. It streams like section 4 - a row is written and released - and holds
	/// nothing. With nothing to list, the table is replaced by one line that also says what was not
	/// assessed, because that is the fact a zero cannot carry alone.
	/// </summary>
	private static void WriteFindingsSummary(TextWriter writer, ReportContent content)
	{
		ConfidenceLevel minimumLevel = content.Configuration.MinimumConfidenceLevel;

		writer.WriteLine(FINDINGS_SUMMARY_HEADING);
		writer.WriteLine();

		if (ThresholdFilter.CountReported(content.Findings, minimumLevel) == 0)
		{
			writer.WriteLine(DescribeNothingReported(content, RaisedThresholdStatementNamesEveryHiddenFinding(content)));
		}
		else
		{
			writer.WriteLine(FINDINGS_SUMMARY_INTRODUCTION);
			writer.WriteLine();
			writer.WriteLine(_SUMMARY_TABLE_HEADING_ROW);
			writer.WriteLine(_SUMMARY_TABLE_RULE_ROW);

			foreach (Finding finding in ThresholdFilter.Apply(content.Findings, minimumLevel))
			{
				WriteSummaryRow(writer, finding, content.Catalog);
			}
		}

		writer.WriteLine();
		WriteRaisedThresholdStatement(writer, content);
	}

	private static void WriteSummaryRow(TextWriter writer, Finding finding, LanguageCatalog catalog)
	{
		WriteFormatted(
			writer,
			_SUMMARY_ROW_FORMAT,
			finding.Identifier,
			EmittedPathText.ForDestination(finding.Path, EmissionDestination.Markdown),
			finding.Line,
			AssessedLanguages.CanonicalName(finding.Language),
			EscapeTableCell(ReportVocabulary.PatternClassText(catalog, finding)),
			ConfidenceLevels.Name(finding.Level),
			finding.Flow is CredentialFlow flow ? CredentialFlows.Name(flow) : _NONE_VALUE);
	}

	/// <summary>
	/// Text this product wrote, going into a table cell: only the bar can end the cell and only a line
	/// ending can end the row, so those two are the whole of the escaping it needs. Repository-chosen text
	/// never comes through here - a path takes <see cref="EmittedPathText"/>'s fuller escaping.
	/// </summary>
	private static string EscapeTableCell(string text)
	{
		return text.Replace(_TABLE_CELL_BAR, _ESCAPED_TABLE_CELL_BAR, StringComparison.Ordinal)
			.ReplaceLineEndings(_TABLE_CELL_LINE_ENDING_REPLACEMENT);
	}

	/// <summary>
	/// The one sentence of a report with nothing in it: "0 findings", then each thing that was not
	/// assessed - files in languages this release does not assess, files in languages the configuration
	/// disabled, said as the repository's own file when that is where the enabled languages came from -
	/// and findings the threshold hides. Files in which no language was detected are not named here: no
	/// language was ever a candidate for them, and section 2 counts them. The count of hidden findings
	/// is left out when <paramref name="hiddenFindingsStatedByRaise"/> is set: the raised-threshold
	/// statement that follows already names every one of them, and the same fact twice in a row reads
	/// as two different ones.
	/// </summary>
	private static string DescribeNothingReported(ReportContent content, bool hiddenFindingsStatedByRaise)
	{
		List<string> segments = [NO_FINDINGS_REPORTED];

		AddLanguageSegment(segments, content, LanguageAssessmentState.NotAssessed, NOT_ASSESSED_SEGMENT_FORMAT);
		AddLanguageSegment(
			segments,
			content,
			LanguageAssessmentState.DisabledByConfiguration,
			content.Configuration.Provenance.EnabledLanguagesOrigin is ConfigurationSource.RepositoryFile
				? DISABLED_BY_REPOSITORY_SEGMENT_FORMAT
				: DISABLED_BY_CONFIGURATION_SEGMENT_FORMAT);

		if (content.Findings.Length > 0 && !hiddenFindingsStatedByRaise)
		{
			segments.Add(string.Format(
				CultureInfo.InvariantCulture,
				BELOW_THRESHOLD_SEGMENT_FORMAT,
				content.Findings.Length,
				content.Findings.Length == 1 ? _FINDING_IS_WORD : _FINDINGS_ARE_WORD,
				ConfidenceLevels.Name(content.Configuration.MinimumConfidenceLevel)));
		}

		return string.Concat(string.Join(_SENTENCE_SEGMENT_SEPARATOR, segments), _SENTENCE_END);
	}

	private static void AddLanguageSegment(
		List<string> segments,
		ReportContent content,
		LanguageAssessmentState state,
		string segmentFormat)
	{
		IReadOnlySet<AssessedLanguage> scanEnabledLanguages = content.ScanEnabledLanguages.ToHashSet();
		LanguageInventoryRow[] rows =
		[
			.. content.ScanSummary.Rows.Where(row =>
				LanguageAssessment.StateOf(content.Catalog, row.LanguageName, scanEnabledLanguages) == state
				&& row.FileCount > 0)
		];

		if (rows.Length > 0)
		{
			int fileCount = rows.Sum(row => row.FileCount);

			segments.Add(string.Format(
				CultureInfo.InvariantCulture,
				segmentFormat,
				fileCount,
				fileCount == 1 ? _FILE_WORD : _FILES_WORD,
				string.Join(_VALUE_SEPARATOR, rows.Select(row => EscapeForMarkdown(row.LanguageName)))));
		}
	}

	/// <summary>
	/// True when the repository's own file raised the threshold and that raise alone hides every
	/// recognized finding, so the raised-threshold statement under the summary carries the whole count.
	/// When a lower level set elsewhere hides some of them too, the two counts differ and both are said.
	/// </summary>
	private static bool RaisedThresholdStatementNamesEveryHiddenFinding(ReportContent content)
	{
		return content.Configuration.Provenance.RepositoryEffect.ThresholdChange is ConfidenceLevelChange { IsRaise: true } change
			&& ThresholdFilter.CountHiddenByRaise(content.Findings, change) == content.Findings.Length;
	}

	/// <summary>
	/// A raise of the threshold by the repository's own file, said where a reader looks first: the
	/// source, the levels and how many findings it hides. Section 5 says it again beside the rest of the
	/// provenance; the number is the same because both take it from <see cref="ThresholdFilter"/>.
	/// </summary>
	private static void WriteRaisedThresholdStatement(TextWriter writer, ReportContent content)
	{
		if (content.Configuration.Provenance.RepositoryEffect.ThresholdChange is ConfidenceLevelChange { IsRaise: true } change)
		{
			writer.WriteLine(RepositoryConfigurationEffectText.ThresholdStatementFor(
				change,
				ThresholdFilter.CountHiddenByRaise(content.Findings, change)));
			writer.WriteLine();
		}
	}

	private static void WriteCoverage(TextWriter writer, ReportContent content)
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(content.Catalog, content.Configuration);

		writer.WriteLine(COVERAGE_HEADING);
		writer.WriteLine();
		writer.WriteLine(CoverageStatement.NOT_A_GUARANTEE_OF_ABSENCE);
		writer.WriteLine();
		writer.WriteLine(_COVERAGE_TABLE_HEADING_ROW);
		writer.WriteLine(_COVERAGE_TABLE_RULE_ROW);

		foreach (LanguageCoverageRow row in rows)
		{
			WriteFormatted(writer, _COVERAGE_ROW_FORMAT, row.LanguageName, CoverageStatement.Describe(row));
		}

		writer.WriteLine();
		WriteDeclaredUnsupportedConstructs(writer, rows);
	}

	/// <summary>
	/// One language per bullet, for every supported language that declares at least
	/// one unsupported construct - the entries already arrive sorted ordinally by
	/// <see cref="DeclaredUnsupportedCoverage.Construct"/>, from
	/// <see cref="CoverageStatement.Compose"/>, so nothing is sorted here. The heading is written
	/// unconditionally and the zero case says so explicitly, on the same "state present when empty"
	/// convention as every other section - a missing heading and a heading saying "none declared" would
	/// say opposite things to a reader.
	/// </summary>
	private static void WriteDeclaredUnsupportedConstructs(TextWriter writer, ImmutableArray<LanguageCoverageRow> rows)
	{
		writer.WriteLine(DECLARED_UNSUPPORTED_CONSTRUCTS_HEADING);
		writer.WriteLine();

		bool wroteAny = false;

		foreach (LanguageCoverageRow row in rows.Where(row => !row.DeclaredUnsupportedConstructs.IsDefaultOrEmpty))
		{
			WriteFormatted(writer, _DECLARED_UNSUPPORTED_LANGUAGE_FORMAT, row.LanguageName);

			foreach (DeclaredUnsupportedCoverage entry in row.DeclaredUnsupportedConstructs)
			{
				WriteFormatted(writer, _DECLARED_UNSUPPORTED_ENTRY_FORMAT, entry.Construct, entry.Reason);
			}

			wroteAny = true;
		}

		if (!wroteAny)
		{
			writer.WriteLine(NO_DECLARED_UNSUPPORTED_CONSTRUCTS);
		}

		writer.WriteLine();
	}

	/// <summary>
	/// Section 2's per-row state and its two after-table summaries read the PERSISTED,
	/// scan-time enabled set rather than <see cref="ReportContent.Configuration"/>'s current one - see
	/// <see cref="ReportContent.ScanEnabledLanguages"/>'s own doc comment - and the section names the
	/// staleness explicitly when the two sets differ.
	/// </summary>
	private static void WriteScanSummary(TextWriter writer, ReportContent content)
	{
		ScanSummary summary = content.ScanSummary;
		IReadOnlySet<AssessedLanguage> scanEnabledLanguages = content.ScanEnabledLanguages.ToHashSet();

		writer.WriteLine(SCAN_SUMMARY_HEADING);
		writer.WriteLine();
		WriteFormatted(writer, _SCAN_TOTALS_FORMAT, summary.FileCount, summary.LineCount, summary.CandidateFileCount);
		writer.WriteLine();

		if (summary.FirstLineFallbackCount > 0)
		{
			WriteFormatted(writer, ReportVocabulary.FIRST_LINE_FALLBACK_FORMAT, summary.FirstLineFallbackCount);
			writer.WriteLine();
		}

		if (summary.Rows.IsDefaultOrEmpty)
		{
			writer.WriteLine(NO_FILES_ENUMERATED);
		}
		else
		{
			writer.WriteLine(_INVENTORY_TABLE_HEADING_ROW);
			writer.WriteLine(_INVENTORY_TABLE_RULE_ROW);

			foreach (LanguageInventoryRow row in summary.Rows)
			{
				WriteInventoryRow(writer, row, content.Catalog, scanEnabledLanguages);
			}

			WriteUnassessedLanguagesSummary(writer, summary.Rows, summary.FileCount, content.Catalog, scanEnabledLanguages);
		}

		if (content.ScanEnabledLanguagesDifferFromSession)
		{
			writer.WriteLine(SCAN_ENABLED_LANGUAGES_STALE_MESSAGE);
		}

		writer.WriteLine();
		WriteExcludedFolders(writer, summary.ExcludedFolders);
	}

	/// <summary>
	/// The folders the walk did not enter, as a whole count and a bounded list of relative paths, on
	/// the terms a skipped item's paths are listed. Written even at zero, so that "none were excluded"
	/// is a statement the report makes and not an absence a reader has to infer.
	/// </summary>
	private static void WriteExcludedFolders(TextWriter writer, ExcludedFolderSummary excludedFolders)
	{
		WriteFormatted(writer, EXCLUDED_FOLDERS_FORMAT, excludedFolders.OccurrenceCount);

		foreach (RepositoryRelativePath path in excludedFolders.Paths)
		{
			WriteFormatted(writer, _BULLET_FORMAT, EmittedPathText.ForDestination(path, EmissionDestination.Markdown));
		}

		if (excludedFolders.OmittedPathCount > 0)
		{
			WriteFormatted(
				writer,
				_BULLET_FORMAT,
				string.Format(CultureInfo.InvariantCulture, _TRUNCATION_FORMAT, excludedFolders.OmittedPathCount, _FOLDERS_WORD));
		}

		writer.WriteLine();
	}

	private static void WriteInventoryRow(
		TextWriter writer,
		LanguageInventoryRow row,
		LanguageCatalog catalog,
		IReadOnlySet<AssessedLanguage> scanEnabledLanguages)
	{
		LanguageAssessmentState state = LanguageAssessment.StateOf(catalog, row.LanguageName, scanEnabledLanguages);

		WriteFormatted(
			writer,
			_INVENTORY_ROW_FORMAT,
			EscapeForMarkdown(row.LanguageName),
			row.FileCount,
			row.LineCount,
			row.CandidateFileCount,
			row.ShareOfFilesPercentage,
			DescribeAssessedCell(state));
	}

	/// <summary>Section 2's "Assessed" column, one cell per state.</summary>
	private static string DescribeAssessedCell(LanguageAssessmentState state)
	{
		string cell = state switch
		{
			LanguageAssessmentState.Assessed => ASSESSED_YES,
			LanguageAssessmentState.NotAssessed => ASSESSED_NO,
			LanguageAssessmentState.DisabledByConfiguration => ASSESSED_DISABLED,
			_ => ASSESSED_NOT_APPLICABLE
		};

		return cell;
	}

	/// <summary>
	/// The Markdown twin of <see cref="Scanning.ScanStage.UNASSESSED_ROW_SUFFIX"/>'s
	/// and <see cref="Scanning.ScanStage.DISABLED_ROW_SUFFIX"/>'s own after-table sentences - see
	/// <see cref="LanguageAssessment"/> for what "assessed" means here. Each sum counts only its own
	/// state, never the unrecognized bucket and never the other's state.
	/// </summary>
	private static void WriteUnassessedLanguagesSummary(
		TextWriter writer,
		ImmutableArray<LanguageInventoryRow> rows,
		int totalFileCount,
		LanguageCatalog catalog,
		IReadOnlySet<AssessedLanguage> scanEnabledLanguages)
	{
		int unassessedFileCount = SumByState(rows, catalog, scanEnabledLanguages, LanguageAssessmentState.NotAssessed);
		int disabledFileCount = SumByState(rows, catalog, scanEnabledLanguages, LanguageAssessmentState.DisabledByConfiguration);

		if (unassessedFileCount > 0)
		{
			WriteFormatted(writer, UNASSESSED_LANGUAGES_SUMMARY_FORMAT, unassessedFileCount, totalFileCount);
		}

		if (disabledFileCount > 0)
		{
			WriteFormatted(writer, DISABLED_LANGUAGES_SUMMARY_FORMAT, disabledFileCount, totalFileCount);
		}
	}

	private static int SumByState(
		ImmutableArray<LanguageInventoryRow> rows,
		LanguageCatalog catalog,
		IReadOnlySet<AssessedLanguage> scanEnabledLanguages,
		LanguageAssessmentState state)
	{
		return rows
			.Where(row => LanguageAssessment.StateOf(catalog, row.LanguageName, scanEnabledLanguages) == state)
			.Sum(row => row.FileCount);
	}

	/// <summary>
	/// The eleven skipped-item categories, always all eleven, with the count whole and the path list
	/// bounded. A category with a count of zero is information: it is what lets the report say that no
	/// file was undecodable rather than leave the reader to infer it from an absence.
	/// </summary>
	private static void WriteSkippedItems(TextWriter writer, ReportContent content)
	{
		writer.WriteLine(SKIPPED_ITEMS_HEADING);
		writer.WriteLine();
		WriteFormatted(
			writer,
			_SKIPPED_TOTALS_FORMAT,
			SumOf(content.SkippedItems, SkippedItemUnit.Files),
			SumOf(content.SkippedItems, SkippedItemUnit.Directories));
		writer.WriteLine();
		writer.WriteLine(_SKIPPED_TABLE_HEADING_ROW);
		writer.WriteLine(_SKIPPED_TABLE_RULE_ROW);

		foreach (SkippedItemTally tally in content.SkippedItems)
		{
			WriteFormatted(
				writer,
				_SKIPPED_ROW_FORMAT,
				ReportVocabulary.DescriptionOf(tally.Category),
				ReportVocabulary.NameOf(tally.Unit),
				tally.Count);
		}

		writer.WriteLine();

		foreach (SkippedItemTally tally in content.SkippedItems.Where(tally => tally.Count > 0))
		{
			WriteSkippedPaths(writer, tally);
		}
	}

	private static void WriteSkippedPaths(TextWriter writer, SkippedItemTally tally)
	{
		WriteFormatted(writer, _BULLET_FORMAT, ReportVocabulary.DescriptionOf(tally.Category));

		if (tally.EnumeratedPaths.IsDefaultOrEmpty)
		{
			WriteFormatted(writer, _PATH_LIST_ITEM_FORMAT, NO_PATHS_ENUMERATED);
		}

		foreach (RepositoryRelativePath path in tally.EnumeratedPaths)
		{
			WriteFormatted(
				writer,
				_PATH_LIST_ITEM_FORMAT,
				EmittedPathText.ForDestination(path, EmissionDestination.Markdown));
		}

		if (tally.OmittedPathCount > 0)
		{
			WriteFormatted(
				writer,
				_PATH_LIST_ITEM_FORMAT,
				string.Format(
					CultureInfo.InvariantCulture,
					_TRUNCATION_FORMAT,
					tally.OmittedPathCount,
					ReportVocabulary.NameOf(tally.Unit)));
		}

		writer.WriteLine();
	}

	/// <summary>
	/// Two groups, split by destination certainty, each written by walking the filtered sequence in the
	/// total finding order. Nothing is sorted here and nothing is collected.
	/// </summary>
	private static void WriteFindings(TextWriter writer, ReportContent content, RecommendationRenderer recommendations)
	{
		ConfidenceLevel minimumLevel = content.Configuration.MinimumConfidenceLevel;

		writer.WriteLine(FINDINGS_HEADING);
		writer.WriteLine();
		WriteFindingsTotal(writer, content, minimumLevel);
		writer.WriteLine();
		WriteNothingReportedLine(writer, content, minimumLevel);
		WriteFindingsCapOutcome(writer, content.FindingsCapOutcome);
		writer.WriteLine();
		writer.WriteLine(FlowCounts.Over(ThresholdFilter.Apply(content.Findings, minimumLevel)).Describe());
		writer.WriteLine();

		WriteFindingGroup(writer, content, recommendations, DestinationCertainty.Established, ESTABLISHED_GROUP_HEADING);
		WriteFindingGroup(writer, content, recommendations, DestinationCertainty.Unknown, NOT_ESTABLISHED_GROUP_HEADING);
	}

	/// <summary>
	/// The summary's zero-findings sentence, repeated where the findings are, so that the section a
	/// reader opens to find nothing tells them what was not assessed and not only that nothing was found.
	/// </summary>
	private static void WriteNothingReportedLine(TextWriter writer, ReportContent content, ConfidenceLevel minimumLevel)
	{
		if (ThresholdFilter.CountReported(content.Findings, minimumLevel) == 0)
		{
			writer.WriteLine(DescribeNothingReported(content, false));
			writer.WriteLine();
		}
	}

	/// <summary>
	/// The truncated case names <see cref="Findings.FindingsCapOutcome.TotalRecognizedCount"/>
	/// as "recognized" rather than <c>content.Findings.Length</c>, which the cap outcome line right
	/// below already names as the RETAINED count - see <see cref="_FINDINGS_TOTAL_TRUNCATED_FORMAT"/>.
	/// </summary>
	private static void WriteFindingsTotal(TextWriter writer, ReportContent content, ConfidenceLevel minimumLevel)
	{
		int reportedCount = ThresholdFilter.CountReported(content.Findings, minimumLevel);

		if (content.FindingsCapOutcome.IsTruncated)
		{
			WriteFormatted(
				writer,
				_FINDINGS_TOTAL_TRUNCATED_FORMAT,
				reportedCount,
				content.Findings.Length,
				content.FindingsCapOutcome.TotalRecognizedCount,
				ConfidenceLevels.Name(minimumLevel));
		}
		else
		{
			WriteFormatted(writer, _FINDINGS_TOTAL_FORMAT, reportedCount, content.Findings.Length, ConfidenceLevels.Name(minimumLevel));
		}
	}

	/// <summary>
	/// The truncation declaration, in the report's own "state zero" convention: a section says
	/// its state explicitly rather than being silent about it, whether or not anything was capped.
	/// </summary>
	private static void WriteFindingsCapOutcome(TextWriter writer, FindingsCapOutcome capOutcome)
	{
		if (capOutcome.IsTruncated)
		{
			WriteFormatted(
				writer,
				FINDINGS_CAPPED_FORMAT,
				capOutcome.TotalOmittedCount,
				capOutcome.TotalRecognizedCount,
				capOutcome.PerFileOmittedCount,
				capOutcome.PerFileCapTrippedFileCount,
				capOutcome.GlobalOmittedCount);
		}
		else
		{
			writer.WriteLine(FINDINGS_NOT_CAPPED);
		}
	}

	private static void WriteFindingGroup(
		TextWriter writer,
		ReportContent content,
		RecommendationRenderer recommendations,
		DestinationCertainty certainty,
		string heading)
	{
		writer.WriteLine(heading);
		writer.WriteLine();

		bool wroteAny = false;

		foreach (Finding finding in ThresholdFilter.InGroup(
			content.Findings,
			content.Configuration.MinimumConfidenceLevel,
			certainty))
		{
			WriteFinding(writer, finding, recommendations, content.Catalog);
			wroteAny = true;
		}

		if (!wroteAny)
		{
			writer.WriteLine(NO_FINDINGS_IN_GROUP);
			writer.WriteLine();
		}
	}

	/// <summary>
	/// One finding and its recommendation, rendered at this point and released with the loop
	/// iteration. The pattern class renders as the catalog's display name, looked up by
	/// identifier, with the identifier itself kept visible in parentheses so a
	/// reader can still cross-reference the artifact, which keys on the identifier alone.
	/// </summary>
	private static void WriteFinding(
		TextWriter writer,
		Finding finding,
		RecommendationRenderer recommendations,
		LanguageCatalog catalog)
	{
		WriteFormatted(
			writer,
			_FINDING_HEADING_FORMAT,
			finding.Identifier,
			EmittedPathText.ForDestination(finding.Path, EmissionDestination.Markdown),
			finding.Line,
			AssessedLanguages.CanonicalName(finding.Language),
			ReportVocabulary.PatternClassText(catalog, finding),
			ConfidenceLevels.Name(finding.Level));
		writer.WriteLine();

		WriteLowReason(writer, finding);
		WriteFlow(writer, finding);
		WriteRecommendation(writer, recommendations.Render(finding, EmissionDestination.Markdown));
		writer.WriteLine();
	}

	/// <summary>
	/// Why a Low finding is Low, from the closed vocabulary of <see cref="LowReasonVocabulary"/>: the
	/// demotion that applied, or that the class is Low itself. A finding that is not Low has none.
	/// </summary>
	private static void WriteLowReason(TextWriter writer, Finding finding)
	{
		if (finding.Level == ConfidenceLevel.Low)
		{
			WriteFormatted(
				writer,
				_LOW_REASON_ROW_FORMAT,
				LowReasonVocabulary.REASON_LABEL,
				LowReasonVocabulary.SentenceOf(finding.LowReasons));
		}
	}

	/// <summary>
	/// The flow and its full note, for a finding Analysis assigned a flow to - which is every High and
	/// Medium finding and no Low one. Written as their own list items ahead of the recommendation, so
	/// the note is never inside the recommendation text.
	/// </summary>
	private static void WriteFlow(TextWriter writer, Finding finding)
	{
		if (finding.Flow is CredentialFlow flow && finding.FlowDecision is FlowDecision decision)
		{
			WriteFormatted(writer, _FLOW_ROW_FORMAT, CredentialFlows.Name(flow), CredentialFlows.Name(decision));
			WriteFormatted(writer, _FLOW_NOTE_ROW_FORMAT, FlowNotes.NoteFor(decision, flow));
		}
	}

	/// <summary>
	/// The four labels are <see cref="RecommendationRenderer"/>'s own constants rather than a copy
	/// held here, so that this list-item text and the artifact's `recommendation` string
	/// are built from one declared label each and cannot drift apart.
	///
	/// EACH OF THE FOUR TEMPLATE PIECES IS ITS OWN LIST ITEM, and every continuation line
	/// it carries - a blank line, a nested list, a fenced code block including a tab-indented line
	/// inside it - is indented to <see cref="_LIST_ITEM_CONTENT_INDENT"/> by
	/// <see cref="WriteLabeledListItem"/>. A single-line piece renders as one row, and no continuation
	/// line follows it.
	/// </summary>
	private static void WriteRecommendation(TextWriter writer, RenderedRecommendation recommendation)
	{
		if (recommendation.Kind is RecommendationKind.FromTemplate)
		{
			WriteLabeledListItem(writer, RecommendationRenderer.SOURCE_FORM_LABEL, recommendation.GenericSourceForm);
			WriteLabeledListItem(
				writer,
				RecommendationRenderer.TARGET_FORM_LABEL,
				recommendation.TargetFormUnderTokenAuthentication);
			WriteLabeledListItem(writer, RecommendationRenderer.CREDENTIALS_LABEL, recommendation.CredentialProvenance);
			WriteLabeledListItem(writer, RecommendationRenderer.VERIFICATION_LABEL, recommendation.VerificationNote);
		}
		else
		{
			WriteUnlabeledListItem(writer, recommendation.Narrative);
		}

		if (recommendation.OAuth2ClientBlock.Length > 0)
		{
			WriteLabeledListItem(writer, RecommendationComposer.OAUTH2_CLIENT_LABEL, recommendation.OAuth2ClientBlock);
		}

		if (recommendation.SecurityNote.Length > 0)
		{
			WriteUnlabeledListItem(writer, recommendation.SecurityNote);
		}
	}

	/// <summary>
	/// One template piece as its own CommonMark list item.
	///
	/// The trap this guards against: a field whose own first line opens a code fence - CS-001's
	/// source form is exactly this - cannot share the bullet line with its label. `"- Source form:
	/// ```csharp"` is not a fenced block under CommonMark; the text after the marker is inline
	/// content on the bullet's own paragraph, and a fence only opens at the start of a line. So
	/// when the value's first line opens a fence, the label is written alone and the fence becomes
	/// the item's first continuation line instead - CommonMark permits a fenced block to interrupt
	/// a paragraph, so no blank line has to separate them. A field whose first line is ordinary
	/// text keeps the label and that line on one row.
	/// </summary>
	private static void WriteLabeledListItem(TextWriter writer, string label, string value)
	{
		string[] lines = value.Split(_TEMPLATE_LINE_BREAK);
		int continuationStartIndex;

		if (CodeFenceDetection.OpensFence(lines[0]))
		{
			WriteFormatted(writer, _RECOMMENDATION_LABEL_ONLY_FORMAT, label);
			continuationStartIndex = 0;
		}
		else
		{
			WriteFormatted(writer, _RECOMMENDATION_ROW_FORMAT, label, lines[0]);
			continuationStartIndex = 1;
		}

		WriteContinuationLines(writer, lines, continuationStartIndex);
	}

	/// <summary>
	/// The generic narrative and the security note: one bullet, no label. Every route this build
	/// ships gives both a single physical line, so the loop in <see cref="WriteContinuationLines"/>
	/// is a no-op here - it is still called, rather than skipped, so a narrative that carries more
	/// than one line is indented correctly.
	/// </summary>
	private static void WriteUnlabeledListItem(TextWriter writer, string value)
	{
		string[] lines = value.Split(_TEMPLATE_LINE_BREAK);

		WriteFormatted(writer, _BULLET_FORMAT, lines[0]);
		WriteContinuationLines(writer, lines, 1);
	}

	/// <summary>
	/// Every line of a list item's value after the one written on its own bullet row, indented to
	/// <see cref="_LIST_ITEM_CONTENT_INDENT"/> with no other change - a fence marker, a fence's own
	/// body (any leading tab it carries preserved, only the content-column indent added ahead of
	/// it), a nested bullet, and a blank line all pass through this loop identically.
	/// </summary>
	private static void WriteContinuationLines(TextWriter writer, string[] lines, int startIndex)
	{
		for (int index = startIndex; index < lines.Length; index++)
		{
			writer.WriteLine($"{_LIST_ITEM_CONTENT_INDENT}{lines[index]}");
		}
	}

	/// <summary>
	/// The provenance section. The scanned path appears as a label and never as a path.
	/// </summary>
	private static void WriteProvenance(TextWriter writer, ReportContent content)
	{
		ConfigurationProvenance provenance = content.Configuration.Provenance;

		writer.WriteLine(PROVENANCE_HEADING);
		writer.WriteLine();
		WriteFormatted(writer, _PROVENANCE_ROW_FORMAT, _RUN_DATE_LABEL, content.RunDate.ToString(OutputArtifactNaming.DATE_FORMAT, CultureInfo.InvariantCulture));
		WriteFormatted(writer, _PROVENANCE_ROW_FORMAT, _TOOL_VERSION_LABEL, ToolVersion.CURRENT);
		WriteFormatted(writer, _PROVENANCE_ROW_FORMAT, _CATALOG_VERSION_LABEL, content.Catalog.Version.DisplayValue);
		WriteFormatted(
			writer,
			_PROVENANCE_ROW_FORMAT,
			_REPOSITORY_LABEL,
			EmittedPathText.LabelForDestination(content.RepositoryLabel, EmissionDestination.Markdown));
		WriteFormatted(writer, _PROVENANCE_ROW_FORMAT, _LANGUAGES_ENABLED_LABEL, DescribeEnabledLanguages(content));
		WriteFormatted(
			writer,
			_PROVENANCE_ROW_FORMAT,
			_MINIMUM_LEVEL_LABEL,
			$"{ConfidenceLevels.Name(content.Configuration.MinimumConfidenceLevel)} (from {ReportVocabulary.NameOf(provenance.MinimumConfidenceLevelOrigin)})");
		WriteFormatted(
			writer,
			_PROVENANCE_ROW_FORMAT,
			_ENABLED_LANGUAGES_ORIGIN_LABEL,
			ReportVocabulary.NameOf(provenance.EnabledLanguagesOrigin));
		WriteFormatted(
			writer,
			_PROVENANCE_ROW_FORMAT,
			_OUTPUT_DESTINATION_ORIGIN_LABEL,
			ReportVocabulary.NameOf(provenance.OutputPathOrigin));
		WriteFormatted(
			writer,
			_PROVENANCE_ROW_FORMAT,
			_DEFAULT_FLOW_LABEL,
			string.Format(
				CultureInfo.InvariantCulture,
				_DEFAULT_FLOW_ORIGIN_FORMAT,
				CredentialFlows.Name(content.Configuration.DefaultFlow),
				ReportVocabulary.NameOf(provenance.DefaultFlowOrigin)));

		WriteConfigurationFiles(writer, provenance);
		WriteAppliedRepositoryConfiguration(writer, content);
		WriteIgnoredConfigurationFields(writer, provenance);
		writer.WriteLine();
	}

	/// <summary>
	/// A repository's own file may narrow its own assessment, so what it changed is stated here,
	/// once per change, in the same words Pre-flight used. The sentences are built from this
	/// product's own vocabularies and carry no text from the file.
	/// </summary>
	private static void WriteAppliedRepositoryConfiguration(TextWriter writer, ReportContent content)
	{
		RepositoryConfigurationEffect repositoryEffect = content.Configuration.Provenance.RepositoryEffect;
		int hiddenFindingCount = repositoryEffect.ThresholdChange is ConfidenceLevelChange change
			? ThresholdFilter.CountHiddenByRaise(content.Findings, change)
			: 0;

		foreach (string statement in RepositoryConfigurationEffectText.StatementsFor(repositoryEffect, hiddenFindingCount))
		{
			WriteFormatted(writer, _PROVENANCE_ROW_FORMAT, _APPLIED_CONFIGURATION_LABEL, statement);
		}
	}

	private static void WriteConfigurationFiles(TextWriter writer, ConfigurationProvenance provenance)
	{
		foreach (ConfigurationFileState file in provenance.ConfigurationFiles)
		{
			WriteFormatted(
				writer,
				_PROVENANCE_ROW_FORMAT,
				string.Format(CultureInfo.InvariantCulture, _CONFIGURATION_FILE_LABEL_FORMAT, ReportVocabulary.NameOf(file.Location)),
				ReportVocabulary.NameOf(file.Condition));
		}
	}

	/// <summary>
	/// A field of the repository's own file that is never read from a repository is recorded here rather
	/// than quietly overridden.
	/// </summary>
	private static void WriteIgnoredConfigurationFields(TextWriter writer, ConfigurationProvenance provenance)
	{
		if (provenance.IgnoredRepositoryFields.IsDefaultOrEmpty)
		{
			WriteFormatted(writer, _PROVENANCE_ROW_FORMAT, _IGNORED_CONFIGURATION_LABEL, NO_IGNORED_CONFIGURATION_FIELDS);
		}

		foreach (IgnoredRepositoryConfigurationField field in provenance.IgnoredRepositoryFields)
		{
			WriteFormatted(
				writer,
				_PROVENANCE_ROW_FORMAT,
				string.Format(CultureInfo.InvariantCulture, _IGNORED_CONFIGURATION_LABEL_FORMAT, ReportVocabulary.NameOf(field.Field)),
				$"{ReportVocabulary.NameOf(field.Reason)}{DescribeIgnoredValue(field)}");
		}
	}

	private static string DescribeIgnoredValue(IgnoredRepositoryConfigurationField field)
	{
		return field.WouldHaveBeen.Length == 0
			? string.Empty
			: $" (it asked for {EscapeForMarkdown(field.WouldHaveBeen)})";
	}

	private static string DescribeEnabledLanguages(ReportContent content)
	{
		string[] enabledNames =
		[
			.. content.Configuration.LanguageEnablement
				.Where(enablement => enablement.IsEnabled)
				.Select(enablement => AssessedLanguages.CanonicalName(enablement.Language))
		];

		return enabledNames.Length == 0 ? _NONE_VALUE : string.Join(_VALUE_SEPARATOR, enabledNames);
	}

	private static int SumOf(ImmutableArray<SkippedItemTally> tallies, SkippedItemUnit unit)
	{
		return tallies.Where(tally => tally.Unit == unit).Sum(tally => tally.Count);
	}

	private static string EscapeForMarkdown(string text)
	{
		return EmittedPathText.LabelForDestination(text, EmissionDestination.Markdown);
	}

	private static void WriteFormatted(TextWriter writer, string format, params object?[] arguments)
	{
		writer.WriteLine(string.Format(CultureInfo.InvariantCulture, format, arguments));
	}
}
