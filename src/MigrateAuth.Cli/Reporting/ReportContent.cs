using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// Section 2's numbers, lifted out of the live inventory so that the renderers consume a value
/// rather than a mutable accumulator.
///
/// <see cref="LanguageInventory"/> is a running tally that the Scan stage writes to while it
/// walks. Handing that object to a renderer would make the report a function of when it was
/// asked rather than of what was scanned, and would give two renderers a shared mutable input
/// that could answer them differently. The rows are already in the report's total order when they come out,
/// and they stay in that order here.
/// </summary>
public sealed record ScanSummary
{
	/// <summary>Every file the inventory pass reached, read or skipped.</summary>
	public required int FileCount { get; init; }

	/// <summary>Every line counted, across all languages.</summary>
	public required long LineCount { get; init; }

	/// <summary>The candidate-file count, distinct from the total file count.</summary>
	public required int CandidateFileCount { get; init; }

	/// <summary>One row per language the inventory attributed a file to, in report order.</summary>
	public required ImmutableArray<LanguageInventoryRow> Rows { get; init; }

	/// <summary>
	/// How many files were attributed by their extension because first-line detection tripped the
	/// lexical budget. Zero in every release that declares no first-line expression.
	/// </summary>
	public int FirstLineFallbackCount { get; init; }

	/// <summary>
	/// The folders the walk did not enter, by policy: a count and a bounded list. Kept apart from the
	/// skipped items, which are holes the tool could not avoid; an excluded folder is one it chose.
	/// </summary>
	public ExcludedFolderSummary ExcludedFolders { get; init; } = ExcludedFolderSummary.None;

	/// <summary>
	/// The files the last step of the decode ladder read as Windows-1252: a count and a bounded list.
	/// They were read, so they are not skipped items; they are listed because that decode is a guess.
	/// </summary>
	public LegacyEncodedFileSummary LegacyEncodedFiles { get; init; } = LegacyEncodedFileSummary.None;

	/// <summary>
	/// The summary of a scan that enumerated nothing. It is a real answer rather than a missing
	/// one: a repository with no candidate files still produces a report, and the emptiest form of it
	/// has no files at all.
	/// </summary>
	public static ScanSummary Empty { get; } = new()
	{
		FileCount = 0,
		LineCount = 0,
		CandidateFileCount = 0,
		Rows = []
	};

	/// <summary>Takes a snapshot of the inventory the Scan stage accumulated.</summary>
	public static ScanSummary From(LanguageInventory inventory)
	{
		ArgumentNullException.ThrowIfNull(inventory);

		return new ScanSummary
		{
			FileCount = inventory.TotalFileCount,
			LineCount = inventory.TotalLineCount,
			CandidateFileCount = inventory.TotalCandidateFileCount,
			Rows = inventory.RowsInReportOrder(),
			FirstLineFallbackCount = inventory.FirstLineFallbackCount
		};
	}
}

/// <summary>
/// EVERYTHING THE REPORT AND THE DATA ARTIFACT ARE RENDERED FROM, AND NOTHING ELSE.
///
/// The report has five sections and the artifact must carry the same five as
/// structured fields. Two renderers over one input is what makes "the same five" a property of
/// the code rather than a thing a maintainer keeps true - a section added to one and forgotten in
/// the other would have to be added to this type first, where both see it.
///
/// THE FINDING SET HERE IS THE COMPLETE ONE. <see cref="FindingComposition"/> delivers every
/// recognized construct, ordered and numbered, with nothing removed; the threshold filter
/// runs at render time over this collection and never before it, so the identifiers in the report are
/// the ones assigned over the whole. Applying the filter earlier would make a finding's
/// identifier a function of a configuration file.
///
/// THE REPOSITORY TRAVELS AS A LABEL AND NEVER AS A PATH. No absolute path is permitted in
/// anything the product emits, and the root is the one path that has no relative form. The type
/// therefore holds a label string rather than a <see cref="Session.RepositoryFolderPath"/>,
/// because the latter also carries the canonical path and a renderer holding it could print it.
/// </summary>
public sealed record ReportContent
{
	/// <summary>The complete, ordered, numbered finding set. Unfiltered.</summary>
	public required ImmutableArray<Finding> Findings { get; init; }

	/// <summary>The session's configuration, which supplies the threshold and the provenance.</summary>
	public required EffectiveConfiguration Configuration { get; init; }

	/// <summary>The embedded catalog: its version, its analyzers, and its templates.</summary>
	public required LanguageCatalog Catalog { get; init; }

	/// <summary>Section 2's numbers.</summary>
	public required ScanSummary ScanSummary { get; init; }

	/// <summary>
	/// The enabled-language set Scan's own last completed run actually used - <see cref="Session.SessionState.ScanEnabledLanguages"/>,
	/// persisted rather than read live. Section 2's per-row assessed/disabled state and its two
	/// after-table summaries are rendered from THIS value rather than from <see cref="Configuration"/>'s
	/// own current one, so a report resumed in a later session - possibly under a configuration that
	/// changed after the scan ran - still describes what the scan itself saw. Coverage (Section 1) is
	/// rendered from it too, through <see cref="CoverageRows"/>, because it states what was looked at;
	/// Provenance (Section 5) is deliberately NOT: it reads <see cref="Configuration"/> directly,
	/// because it describes this session's configuration.
	/// </summary>
	public required ImmutableArray<AssessedLanguage> ScanEnabledLanguages { get; init; }

	/// <summary>
	/// Whether <see cref="ScanEnabledLanguages"/> differs from this session's current enabled set.
	/// It is the one condition behind the stale-scan sentence on the console and in the Markdown
	/// report and behind the data artifact's <c>enabledLanguagesDifferFromSession</c> field. Only the
	/// enabled languages are compared; no other setting is.
	/// </summary>
	public bool ScanEnabledLanguagesDifferFromSession =>
		!ScanEnabledLanguages.SequenceEqual(Configuration.EnabledLanguagesInDeclaredOrder);

	/// <summary>
	/// Section 1's rows: what was looked at, which is what the scan selected candidates for. They are
	/// composed from <see cref="ScanEnabledLanguages"/> and not from the current configuration, so that
	/// a language the configuration disabled after the scan, and whose findings are in the report, is
	/// not described as unassessed beside them.
	/// </summary>
	public ImmutableArray<LanguageCoverageRow> CoverageRows =>
		CoverageStatement.Compose(Catalog, ScanEnabledLanguages.ToHashSet());

	/// <summary>Section 3's tallies - all eleven categories.</summary>
	public required ImmutableArray<SkippedItemTally> SkippedItems { get; init; }

	/// <summary>
	/// The truncation counter over <see cref="Findings"/>. Carried beside the set rather than
	/// computed from it, because a filtered or re-counted view could never reconstruct how many
	/// constructs the caps removed before this array ever existed.
	/// </summary>
	public required FindingsCapOutcome FindingsCapOutcome { get; init; }

	/// <summary>The audited repository's label, from <c>RepositoryFolderPath.Label</c>.</summary>
	public required string RepositoryLabel { get; init; }

	/// <summary>
	/// The local date the run's two artifacts are dated with - the same value
	/// <see cref="Pipeline.ReportStage"/> reads once from its <c>TimeProvider</c> and uses for both
	/// the file name and this field, so the two can never disagree about which day a report is from.
	/// Carried here rather than read again by each renderer for the same reason the rest of this type
	/// is: a value both renderers must agree on belongs on the one input they both read, not on two
	/// separate clock reads that could straddle midnight differently.
	/// </summary>
	public required DateOnly RunDate { get; init; }
}
