using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Progress;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Scanning;

/// <summary>
/// What the Scan stage is built from. It is a context rather than six constructor parameters
/// because six is past the point where a call site reads as an ordered list of anonymous values,
/// and because the last two - where output goes and which clock the cadence is measured against -
/// exist to be substituted in a test and are clearer named than positional.
/// </summary>
public sealed record ScanStageContext
{
	/// <summary>The repository under assessment.</summary>
	public required RepositoryFolderPath Repository { get; init; }

	/// <summary>The embedded catalog, whose analyzers supply identification and anchors.</summary>
	public required LanguageCatalog Catalog { get; init; }

	/// <summary>
	/// The session's effective configuration, read here for the enabled-language set: it
	/// narrows PASS 2's own candidate selection, decides this stage's own console suffixes and is
	/// snapshotted onto the session state at completion, alongside the candidate set it decided.
	/// </summary>
	public required EffectiveConfiguration Configuration { get; init; }

	/// <summary>The one door everything leaving the process passes through.</summary>
	public required EmissionPoint EmissionPoint { get; init; }

	/// <summary>
	/// The sink the walk's skipped items accumulate in. The stage CLEARS it at the start of its own
	/// run, reads the counts back for the scan summary, and snapshots
	/// them onto the session state at completion - there is no second reporting path for a skipped
	/// item anywhere in this stage, and no other stage clears or reads this same instance during a
	/// Scan run.
	/// </summary>
	public required SkippedItemLedger SkippedItems { get; init; }

	/// <summary>Where the stage writes.</summary>
	public required TextWriter Output { get; init; }

	/// <summary>The clock the two-second progress cadence is measured against.</summary>
	public required TimeProvider TimeProvider { get; init; }
}

/// <summary>
/// The Scan workflow stage.
///
/// It runs the two passes in order - count, warn, then read - and hands the candidate set to the
/// session state, where it is persisted as an output of Scan. The shell does the persisting, the
/// cascade and the gating; this stage reports only how it ended.
///
/// Everything it prints is a count or a catalog name. No path, no line of any analyzed file, and
/// no framework message reaches this console surface.
/// </summary>
public sealed class ScanStage
{
	/// <summary>Said before the enumeration pass, so that a long walk is not a silent one.</summary>
	public const string ENUMERATION_STARTED_MESSAGE = $"{_STAGE_NAME}: enumerating the repository.";

	/// <summary>Said between the passes, naming the total the advance warning was decided from.</summary>
	public const string ENUMERATION_COMPLETED_MESSAGE_FORMAT = $"{_STAGE_NAME}: {{0:N0}} files enumerated.";

	/// <summary>the table, whose header names the denominator of the share column.</summary>
	public const string INVENTORY_HEADER_MESSAGE = "Inventory by language (share of files enumerated):";

	/// <summary>One language's row.</summary>
	public const string INVENTORY_ROW_FORMAT = "  {0,-20}{1,12:N0} files{2,14:N0} lines{3,8:F1} %";

	/// <summary>
	/// Appended to a row whose language names one of the eight `AssessedLanguage` values but
	/// carries no registered analyzer in this build - see <see cref="LanguageAssessment"/> for what
	/// "assessed" means. Never appended for <see cref="LanguageAssessmentState.NoLanguageDetected"/>:
	/// the unrecognized bucket was never searched, so it is not a language this release failed to
	/// assess.
	/// </summary>
	public const string UNASSESSED_ROW_SUFFIX = "  not assessed: no analyzer in this release";

	/// <summary>
	/// Appended to a row whose language this session's configuration disabled - distinct from
	/// <see cref="UNASSESSED_ROW_SUFFIX"/> even where no analyzer exists either, because disabling is
	/// this session's own choice and the reader needs to see that rather than a coverage gap the
	/// choice made moot.
	/// </summary>
	public const string DISABLED_ROW_SUFFIX = "  not assessed: disabled by configuration";

	/// <summary>
	/// Said once, after the table, when at least one enumerated file fell in a language this
	/// release does not assess - so a reader does not have to add up individual row suffixes to learn
	/// the total that reflects on this run's own coverage.
	/// </summary>
	public const string UNASSESSED_LANGUAGES_SUMMARY_FORMAT =
		"Files in languages this release does not assess: {0:N0} of {1:N0}. They are counted here and not analyzed.";

	/// <summary>
	/// Said once, after the table, when at least one enumerated file fell in a language this
	/// session's configuration disabled - the console twin of <see cref="UNASSESSED_LANGUAGES_SUMMARY_FORMAT"/>,
	/// kept apart from it for the same reason DisabledByConfiguration is its own
	/// <see cref="LanguageAssessmentState"/> rather than folded into NotAssessed.
	/// </summary>
	public const string DISABLED_LANGUAGES_SUMMARY_FORMAT =
		"Files in languages disabled by configuration: {0:N0} of {1:N0}. They are counted here and not analyzed.";

	/// <summary>The candidate count, said distinctly from the total file count.</summary>
	public const string CANDIDATE_COUNT_MESSAGE_FORMAT = "Candidate files: {0:N0} of {1:N0}.";

	/// <summary>
	/// The skipped count, in the scan summary. The two numbers are never added: ten categories count
	/// files and the eleventh counts directories, and a directory whose contents could not be listed is
	/// a hole of unknown size, so a sum of the two would be a number that means nothing.
	/// </summary>
	public const string SKIPPED_SUMMARY_MESSAGE_FORMAT =
		"Skipped files: {0:N0}. Directories whose contents could not be listed: {1:N0}.";

	/// <summary>
	/// Said once, and only when the walk met at least one folder it does not enter. A count and nothing
	/// else: the report lists the folders. Kept apart from <see cref="SKIPPED_SUMMARY_MESSAGE_FORMAT"/>
	/// because an excluded folder is a policy of the tool and not a hole it could not avoid.
	/// </summary>
	public const string EXCLUDED_FOLDERS_MESSAGE_FORMAT =
		"Folders excluded from the scan and not read: {0:N0}. The report lists them.";

	/// <summary>
	/// The empty repository, which is not an error. A repository with no candidate files completes every
	/// stage and produces a valid report, and the emptiest form of it is a repository with no files at all.
	/// </summary>
	public const string NO_FILES_MESSAGE = $"{_STAGE_NAME}: no files were found under the repository root.";

	/// <summary>
	/// The stage's own name, and the same text the registry carries for the command. It is a
	/// constant here rather than a registry lookup because the three messages above are compile-
	/// time constants that tests assert against verbatim; the test that pins them to the registry
	/// is what keeps the two from drifting.
	/// </summary>
	private const string _STAGE_NAME = "scan";

	private const string _ITEM_LABEL = "files";

	private readonly ScanStageContext _context;

	public ScanStage(ScanStageContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(context.Catalog);
		ArgumentNullException.ThrowIfNull(context.Configuration);
		ArgumentNullException.ThrowIfNull(context.EmissionPoint);
		ArgumentNullException.ThrowIfNull(context.SkippedItems);
		ArgumentNullException.ThrowIfNull(context.Output);
		ArgumentNullException.ThrowIfNull(context.TimeProvider);

		_context = context;
	}

	/// <summary>
	/// The stage body. The order is the requirement: the enumeration completes and reports its
	/// total, the advance warning fires from that total, and only then does the inventory pass
	/// open its first file.
	///
	/// The ledger is cleared before either pass, so a second Scan run in the same process
	/// starts from nothing rather than adding this run's skips onto an earlier run's - the same
	/// replace-not-append discipline <see cref="Session.SessionState.RecordScanOutcome"/> already
	/// applies to the candidate set.
	/// </summary>
	public StageRunOutcome Run(SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		_context.SkippedItems.Clear();

		RepositoryInventoryScan scan = new(
			_context.Repository,
			_context.Catalog,
			_context.EmissionPoint,
			_context.Configuration.EnabledLanguageSet,
			_context.TimeProvider);
		EnumerationResult enumeration = RunEnumerationPass(scan, cancellationToken);
		StageRunOutcome outcome;

		if (enumeration.WasCancelled)
		{
			outcome = StageRunOutcome.Cancelled;
		}
		else
		{
			outcome = RunInventoryPass(scan, enumeration, state, cancellationToken);
		}

		return outcome;
	}

	/// <summary>
	/// PASS 1, which opens nothing. Its progress reporter has no total to report a share of,
	/// because the total is what it is producing.
	/// </summary>
	private EnumerationResult RunEnumerationPass(RepositoryInventoryScan scan, CancellationToken cancellationToken)
	{
		_context.Output.WriteLine(ENUMERATION_STARTED_MESSAGE);

		StageProgressReporter progress = new(
			_STAGE_NAME,
			_ITEM_LABEL,
			totalItemCount: null,
			_context.Output,
			_context.TimeProvider);

		return scan.Enumerate(progress, cancellationToken);
	}

	/// <summary>
	/// PASS 2. The two lines before it state the total and fire the advance warning - both from the
	/// enumeration's count, both before the inventory pass exists - and the reporter for this pass is
	/// the one that carries a denominator.
	/// </summary>
	private StageRunOutcome RunInventoryPass(
		RepositoryInventoryScan scan,
		EnumerationResult enumeration,
		SessionState state,
		CancellationToken cancellationToken)
	{
		StageProgressReporter progress = new(
			_STAGE_NAME,
			_ITEM_LABEL,
			enumeration.FilePaths.Count,
			_context.Output,
			_context.TimeProvider);

		AnnounceTheWorkAhead(enumeration, progress);

		InventoryResult inventory = scan.ReadInventory(enumeration.FilePaths, progress, cancellationToken);
		StageRunOutcome outcome;

		if (inventory.WasCancelled)
		{
			outcome = StageRunOutcome.Cancelled;
		}
		else
		{
			ReportInventory(inventory, enumeration.ExcludedFolders);
			state.RecordScanOutcome(
				inventory.CandidateRelativePaths,
				inventory.CandidateLanguageNames,
				ScanSummary.From(inventory.Inventory) with { ExcludedFolders = enumeration.ExcludedFolders },
				_context.SkippedItems.Tallies(),
				_context.Configuration.EnabledLanguagesInDeclaredOrder);
			outcome = StageRunOutcome.Completed;
		}

		return outcome;
	}

	private void AnnounceTheWorkAhead(EnumerationResult enumeration, StageProgressReporter progress)
	{
		if (enumeration.FilePaths.Count == 0)
		{
			_context.Output.WriteLine(NO_FILES_MESSAGE);
		}
		else
		{
			WriteFormatted(ENUMERATION_COMPLETED_MESSAGE_FORMAT, enumeration.FilePaths.Count);
		}

		progress.WarnWhenTheWorkIsLarge();
	}

	/// <summary>
	/// the inventory and the candidate count, plus the skipped count. The table is written only
	/// where there is a row to write: a header over nothing reads as a defect rather than as an
	/// empty repository.
	/// </summary>
	private void ReportInventory(InventoryResult inventory, ExcludedFolderSummary excludedFolders)
	{
		ImmutableArray<LanguageInventoryRow> rows = inventory.Inventory.RowsInReportOrder();

		if (rows.Length > 0)
		{
			_context.Output.WriteLine(INVENTORY_HEADER_MESSAGE);

			IReadOnlySet<AssessedLanguage> enabledLanguages = _context.Configuration.EnabledLanguageSet;

			foreach (LanguageInventoryRow row in rows)
			{
				WriteInventoryRow(row, enabledLanguages);
			}

			WriteUnassessedLanguagesSummary(rows, inventory.Inventory.TotalFileCount, enabledLanguages);
		}

		_context.Output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			CANDIDATE_COUNT_MESSAGE_FORMAT,
			inventory.Inventory.TotalCandidateFileCount,
			inventory.Inventory.TotalFileCount));

		_context.Output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			SKIPPED_SUMMARY_MESSAGE_FORMAT,
			_context.SkippedItems.SkippedFiles,
			_context.SkippedItems.DirectoriesOfUnknownContents));

		if (excludedFolders.OccurrenceCount > 0)
		{
			WriteFormatted(EXCLUDED_FOLDERS_MESSAGE_FORMAT, excludedFolders.OccurrenceCount);
		}
	}

	/// <summary>
	/// Every enumerated file whose language carries no registered analyzer,
	/// summed once after the table, and separately every enumerated file whose language this session's
	/// configuration disabled - see <see cref="LanguageAssessment"/> for what "assessed" means here.
	/// Each sum counts only its own <see cref="LanguageAssessmentState"/>, never the unrecognized
	/// bucket and never the other's state - disabled and unanalyzed are two distinct reasons.
	/// </summary>
	private void WriteUnassessedLanguagesSummary(
		ImmutableArray<LanguageInventoryRow> rows,
		int totalFileCount,
		IReadOnlySet<AssessedLanguage> enabledLanguages)
	{
		int unassessedFileCount = SumByState(rows, enabledLanguages, LanguageAssessmentState.NotAssessed);
		int disabledFileCount = SumByState(rows, enabledLanguages, LanguageAssessmentState.DisabledByConfiguration);

		if (unassessedFileCount > 0)
		{
			_context.Output.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				UNASSESSED_LANGUAGES_SUMMARY_FORMAT,
				unassessedFileCount,
				totalFileCount));
		}

		if (disabledFileCount > 0)
		{
			_context.Output.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				DISABLED_LANGUAGES_SUMMARY_FORMAT,
				disabledFileCount,
				totalFileCount));
		}
	}

	private int SumByState(
		ImmutableArray<LanguageInventoryRow> rows,
		IReadOnlySet<AssessedLanguage> enabledLanguages,
		LanguageAssessmentState state)
	{
		return rows
			.Where(row => LanguageAssessment.StateOf(_context.Catalog, row.LanguageName, enabledLanguages) == state)
			.Sum(row => row.FileCount);
	}

	private void WriteInventoryRow(LanguageInventoryRow row, IReadOnlySet<AssessedLanguage> enabledLanguages)
	{
		LanguageAssessmentState state = LanguageAssessment.StateOf(_context.Catalog, row.LanguageName, enabledLanguages);
		string formattedRow = string.Format(
			CultureInfo.InvariantCulture,
			INVENTORY_ROW_FORMAT,
			row.LanguageName,
			row.FileCount,
			row.LineCount,
			row.ShareOfFilesPercentage);
		string rowSuffix = state switch
		{
			LanguageAssessmentState.NotAssessed => UNASSESSED_ROW_SUFFIX,
			LanguageAssessmentState.DisabledByConfiguration => DISABLED_ROW_SUFFIX,
			_ => string.Empty
		};

		_context.Output.WriteLine($"{formattedRow}{rowSuffix}");
	}

	private void WriteFormatted(string messageFormat, int count)
	{
		_context.Output.WriteLine(string.Format(CultureInfo.InvariantCulture, messageFormat, count));
	}
}
