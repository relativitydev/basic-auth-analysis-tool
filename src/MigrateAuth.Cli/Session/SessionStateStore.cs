using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Session;

/// <summary>Why a state file did not produce a resumed session. A code rather than prose.</summary>
public enum SessionStateRejectionReason
{
	/// <summary>The state was read and accepted.</summary>
	None = 0,

	/// <summary>No state file exists for this repository. The ordinary first run.</summary>
	FileAbsent = 1,

	/// <summary>The file exists and could not be read.</summary>
	FileUnreadable = 2,

	/// <summary>
	/// The file is larger than <see cref="SessionStateStore.MAXIMUM_STATE_FILE_LENGTH_IN_BYTES"/>, the
	/// ceiling on a state file this tool writes, so it was not read at all.
	/// </summary>
	FileTooLarge = 3,

	/// <summary>The file is not the JSON this schema declares.</summary>
	DocumentMalformed = 4,

	/// <summary>The document declares another version of its own shape.</summary>
	SchemaVersionMismatch = 5,

	/// <summary>The document was produced under another catalog version.</summary>
	CatalogVersionMismatch = 6,

	/// <summary>The document's repository label is absent, is a path, or names another checkout.</summary>
	RepositoryLabelMismatch = 7,

	/// <summary>A completed stage is not a workflow command this build knows.</summary>
	CompletedStageUnknown = 8,

	/// <summary>A stage is completed whose predecessor is not, which no real session can produce.</summary>
	CompletedStagesNotPrefixClosed = 9,

	/// <summary>A candidate path is absent, absolute, or escapes the repository root.</summary>
	CandidatePathNotContained = 10,

	/// <summary>
	/// A persisted finding is not one this engine could have produced: a missing or out-of-range
	/// number, a path that escapes the repository root, a name outside a closed vocabulary, a
	/// pattern class that does not carry a catalog identifier's shape, or a set whose indices are
	/// not the sequence the finding model assigns.
	/// </summary>
	FindingNotWellFormed = 11,

	/// <summary>
	/// A present <c>scanSummary</c> is not one Scan could have produced: a negative count, a
	/// row whose counts do not sum to the totals, or a row carrying a language name outside the
	/// closed set this catalog can declare. ALSO REACHED WHEN IT IS ABSENT AND SHOULD NOT BE:
	/// <c>completedStages</c> names <c>scan</c> complete, and a completed scan
	/// always snapshots its own outcome, so an absent <c>scanSummary</c> beside a completed
	/// <c>scan</c> is not a document this tool wrote.
	/// </summary>
	ScanSummaryNotWellFormed = 12,

	/// <summary>
	/// A present skipped-item tally set is not one this ledger could have produced: not
	/// exactly the eleven declared categories, a negative count, or an enumerated path that is
	/// absent, escapes the repository root, or exceeds <see cref="Reading.ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY"/>
	/// or its own category's count. ALSO REACHED WHEN IT IS ABSENT AND SHOULD NOT BE, on the same
	/// terms <see cref="ScanSummaryNotWellFormed"/> documents - <c>scanSkippedItems</c> for a
	/// completed <c>scan</c>, <c>analysisSkippedItems</c> for a completed <c>analyze</c>.
	/// </summary>
	SkippedItemTallyNotWellFormed = 13,

	/// <summary>
	/// A present <c>findingsCapOutcome</c> is not one <see cref="Findings.FindingComposition"/>
	/// could have produced: a negative count, a retained count that disagrees with the persisted
	/// finding set's own length, or counts that do not sum the way the accumulator guarantees they
	/// always do. ALSO REACHED WHEN IT IS ABSENT AND SHOULD NOT BE, on the same terms
	/// <see cref="ScanSummaryNotWellFormed"/> documents, for a completed <c>analyze</c>.
	/// </summary>
	FindingsCapOutcomeNotWellFormed = 14,

	/// <summary>
	/// A present <c>scanEnabledLanguages</c> is not one Scan could have produced: a name that
	/// is not one of the eight canonical names, or a name repeated. ALSO REACHED WHEN IT IS ABSENT AND
	/// SHOULD NOT BE, on the same terms <see cref="ScanSummaryNotWellFormed"/> documents: a completed
	/// <c>scan</c> always snapshots the enabled set it narrowed candidate selection with.
	/// </summary>
	ScanEnabledLanguagesNotWellFormed = 15,

	/// <summary>
	/// The per-candidate attribution is not one Scan could have produced: not the same length
	/// as the candidate set, or a name no analyzer in this catalog declares. Absent is accepted only
	/// beside an empty candidate set.
	/// </summary>
	CandidateAttributionNotWellFormed = 16,

	/// <summary>
	/// The effective default flow and its origin are not ones Analysis could have produced: a
	/// name outside the closed vocabulary, one member without the other, a built-in origin naming a
	/// flow other than the built-in one, or a finding decided by the default under a different flow.
	/// ALSO REACHED WHEN THEY ARE ABSENT AND SHOULD NOT BE, on the same terms
	/// <see cref="ScanSummaryNotWellFormed"/> documents, for a completed <c>analyze</c>.
	/// </summary>
	DefaultFlowNotWellFormed = 17,

	/// <summary>
	/// The flow-signal evaluation counters are not ones Analysis could have produced:
	/// a negative count, more timeouts than evaluations, or more evaluations than candidates. ALSO
	/// REACHED WHEN THEY ARE ABSENT AND SHOULD NOT BE, for a completed <c>analyze</c>.
	/// </summary>
	FlowEvaluationDiagnosticsNotWellFormed = 18
}

/// <summary>The outcome of reading the state file: a session either way, and why.</summary>
public sealed record SessionStateLoad
{
	/// <summary>The session to run. Never null: an anomaly yields a fresh session, not a failure.</summary>
	public required SessionState State { get; init; }

	/// <summary>Whether the state on disk was accepted.</summary>
	public required bool WasResumed { get; init; }

	/// <summary>Why it was not, when it was not.</summary>
	public required SessionStateRejectionReason RejectionReason { get; init; }
}

/// <summary>
/// The session state file. Every rule this class implements is binding and each one is
/// here for a stated reason:
///
/// it is written to a temporary and renamed, because a half-written state must read as absent;
/// it is created with restrictive user permissions, because it is the one artifact nobody
/// sends anywhere and hardening it costs nothing;
/// its name carries a keyed hash of the canonicalized repository path, so that a session
/// resuming finds its own state among every repository a developer has scanned - the hash is
/// over the path only and never over content, which is what keeps the prohibition on
/// fingerprinting source content intact;
/// the hash has no salt and must not have one, because a salted name would not be findable on
/// resume, which is the whole purpose;
/// the file records the repository root as a label and never as a traversable path;
/// and it is treated as untrusted input, so any anomaly rejects into a fresh session rather
/// than failing - the missing-file rule covers the missing and unreadable cases, and the
/// readable-and-hostile case is what would otherwise let someone inject fabricated findings
/// into a client's report.
///
/// A repository moved or renamed therefore starts a fresh session. That is correct rather than
/// merely acceptable: the tool cannot distinguish a moved checkout from a different one.
/// </summary>
public sealed class SessionStateStore
{
	/// <summary>The prefix every state file name carries, so a human can recognize one.</summary>
	public const string STATE_FILE_NAME_PREFIX = "migrate-auth.session.";

	/// <summary>The suffix every state file name carries.</summary>
	public const string STATE_FILE_NAME_SUFFIX = ".json";

	/// <summary>
	/// The schema version this build writes and the only one it accepts.
	///
	/// A state file of any other version starts a fresh session, with its reason shown, rather than
	/// being read as a file of this version, and a file of a newer version is refused by a build that
	/// cannot read it instead of failing on it.
	/// </summary>
	public const int CURRENT_SCHEMA_VERSION = 8;

	/// <summary>
	/// The ceiling on a state file, in bytes, on both the write and the read side. A state past it
	/// meets the write-side check rather than producing a file that cannot be loaded later: default
	/// JSON escaping expands a non-ASCII path character up to six times.
	/// </summary>
	public const long MAXIMUM_STATE_FILE_LENGTH_IN_BYTES = _CEILING_IN_MEBIBYTES * BYTES_PER_MEBIBYTE;

	/// <summary>The unit the user-facing ceiling is stated in alongside its byte count.</summary>
	public const long BYTES_PER_MEBIBYTE = 1024L * 1024;

	/// <summary>
	/// The ceiling on the size of a session state file, in bytes, rounded to a multiple of 16 MiB. The
	/// volume proof fails if this and the measured size of its synthetic state stop being one rounding
	/// unit apart.
	/// </summary>
	private const long _CEILING_IN_MEBIBYTES = 128;

	/// <summary>How a ceiling is stated to the user: bytes, then mebibytes. See <see cref="DescribeCeiling"/>.</summary>
	public const string CEILING_DESCRIPTION_FORMAT = "{0:N0} bytes ({1:0.###} MiB)";

	/// <summary>
	/// Said when a save would pass the ceiling. It names the ceiling (argument 0, from
	/// <see cref="DescribeCeiling"/>) and says what the refusal left in place, so that the user knows
	/// the previously saved state is intact.
	/// </summary>
	public const string STATE_TOO_LARGE_MESSAGE_FORMAT =
		"The session state is larger than the limit for a session state file, {0}, so it was not saved. The previously saved state is unchanged; a resumed session restarts from the last stage that saved.";

	/// <summary>The persisted name of <see cref="FlowEvidence.None"/>.</summary>
	public const string FLOW_EVIDENCE_NONE_NAME = "None";

	/// <summary>The persisted name of <see cref="FlowEvidence.Fixed"/>.</summary>
	public const string FLOW_EVIDENCE_FIXED_NAME = "Fixed";

	/// <summary>The persisted name of <see cref="FlowEvidence.Interactive"/>.</summary>
	public const string FLOW_EVIDENCE_INTERACTIVE_NAME = "Interactive";

	/// <summary>The persisted name for a finding whose destination the construct establishes.</summary>
	public const string DESTINATION_ESTABLISHED_NAME = "established";

	/// <summary>The persisted name for a finding whose destination is not established.</summary>
	public const string DESTINATION_UNKNOWN_NAME = "unknown";

	/// <summary>
	/// The lowest value a line or a column may carry. Both count from one, for the reason
	/// <see cref="FindingIdentifiers.FIRST_INDEX"/> gives about the identifier beside them: a human
	/// reads them, and a zero among values that count from one is the inconsistency nobody notices
	/// until someone acts on it.
	/// </summary>
	private const int _FIRST_LINE_OR_COLUMN = 1;

	/// <summary>
	/// The same scale <see cref="Scanning.LanguageInventory"/> uses to turn a file-count
	/// ratio into a percentage, so a row's share is recomputed bit-identically on read rather than
	/// trusted from a second, persisted copy.
	/// </summary>
	private const double _PERCENTAGE_SCALE = 100.0;

	private const string _TEMPORARY_FILE_NAME_SUFFIX = ".writing";
	private const int _STATE_FILE_NAME_HASH_CHARACTER_COUNT = 32;

	private const char _PORTABLE_DIRECTORY_SEPARATOR = '/';
	private const string _PARENT_DIRECTORY_SEGMENT = "..";

	/// <summary>
	/// The domain-separation key of the file-name hash. It is fixed by the build and it is not
	/// a secret: it grants no authority, protects nothing, and is published in this source. It
	/// is a key rather than a salt precisely because it must produce the same name on every run
	/// and every machine - a per-run salt would make the file unfindable on resume.
	/// </summary>
	private const string _STATE_FILE_NAME_HASH_DOMAIN = "migrate-auth/session-state/v1";

	private static readonly byte[] _stateFileNameHashKey = Encoding.UTF8.GetBytes(_STATE_FILE_NAME_HASH_DOMAIN);

	private readonly DocumentationDestinationFolderPath _destinationFolder;
	private readonly long _maximumStateFileLengthInBytes;

	public SessionStateStore(DocumentationDestinationFolderPath destinationFolder)
		: this(destinationFolder, MAXIMUM_STATE_FILE_LENGTH_IN_BYTES)
	{
	}

	/// <summary>
	/// The store with a ceiling other than <see cref="MAXIMUM_STATE_FILE_LENGTH_IN_BYTES"/>. It exists so
	/// that a test can prove the write-side refusal without writing a file of the production
	/// size; the shipped binary always takes the constructor above.
	/// </summary>
	public SessionStateStore(DocumentationDestinationFolderPath destinationFolder, long maximumStateFileLengthInBytes)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumStateFileLengthInBytes);

		_destinationFolder = destinationFolder;
		_maximumStateFileLengthInBytes = maximumStateFileLengthInBytes;
	}

	/// <summary>
	/// The state file name for a repository. Same repository, same name, every run; different
	/// repositories, different names. Folded to upper case invariantly before hashing, for the
	/// same reason paths are compared case-insensitively on every platform: case sensitivity
	/// is a property of the volume rather than of the operating system, and nothing derivable
	/// from the running platform answers the question.
	/// </summary>
	public static string BuildStateFileName(RepositoryFolderPath repository)
	{
		string repositoryKey = BuildRepositoryKey(repository);
		byte[] keyedHash = HMACSHA256.HashData(_stateFileNameHashKey, Encoding.UTF8.GetBytes(repositoryKey));
		string hashText = Convert.ToHexStringLower(keyedHash)[.._STATE_FILE_NAME_HASH_CHARACTER_COUNT];

		return $"{STATE_FILE_NAME_PREFIX}{hashText}{STATE_FILE_NAME_SUFFIX}";
	}

	/// <summary>
	/// Reads the state for a repository, or produces a fresh session and says why.
	///
	/// IT TAKES THE CATALOG RATHER THAN ITS VERSION ALONE, because Section 2's persisted
	/// language names (<see cref="ValidateInventoryLanguageName"/>) are checked against what this
	/// build's catalog can actually declare - the eight assessed languages, the unrecognized bucket,
	/// or a name this catalog's own analyzers declare - and the version comparison reads
	/// <paramref name="catalog"/>'s own <see cref="Catalog.LanguageCatalog.Version"/>.
	/// </summary>
	public SessionStateLoad Load(RepositoryFolderPath repository, LanguageCatalog catalog)
	{
		ArgumentNullException.ThrowIfNull(catalog);

		DocumentReadResult read = ReadDocument(BuildStateFilePath(repository));
		SessionStateLoad load;

		if (read.Document is null)
		{
			load = BuildFreshLoad(read.Reason, catalog.Version, repository.Label);
		}
		else
		{
			load = ValidateDocument(read.Document, catalog, repository);
		}

		return load;
	}

	/// <summary>
	/// Writes the state atomically: to a temporary file created with restrictive permissions,
	/// then renamed over the final name. A process killed mid-write leaves the previous state
	/// intact and never a half-written one.
	///
	/// The document is streamed to the temporary file, unindented, and the bytes are counted as
	/// they are written. Past the ceiling the save is refused with <see cref="SessionStateTooLargeException"/>,
	/// and the temporary file is discarded before the rename, so the previous state file is untouched
	/// and a resume restarts from the last stage that saved. Any other failure discards the temporary
	/// file in the same way.
	/// </summary>
	public void Save(RepositoryFolderPath repository, SessionState state)
	{
		ArgumentNullException.ThrowIfNull(state);

		string finalPath = BuildStateFilePath(repository);
		string temporaryPath = $"{finalPath}{_TEMPORARY_FILE_NAME_SUFFIX}";
		bool committed = false;

		Directory.CreateDirectory(_destinationFolder.CanonicalPath);

		try
		{
			WriteDocumentStreaming(temporaryPath, BuildDocument(state));
			File.Move(temporaryPath, finalPath, overwrite: true);
			committed = true;
		}
		finally
		{
			if (!committed)
			{
				DiscardTemporaryFile(temporaryPath);
			}
		}
	}

	/// <summary>
	/// The user-facing statement of a ceiling, naming it in bytes and in mebibytes. One place builds it
	/// so the refusal a save raises and the description a rejected load shows cannot state different
	/// numbers.
	/// </summary>
	public static string DescribeCeiling(long ceilingInBytes)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			CEILING_DESCRIPTION_FORMAT,
			ceilingInBytes,
			(double)ceilingInBytes / BYTES_PER_MEBIBYTE);
	}

	private static string BuildRepositoryKey(RepositoryFolderPath repository)
	{
		string portablePath = repository.CanonicalPath
			.Replace(Path.DirectorySeparatorChar, _PORTABLE_DIRECTORY_SEPARATOR)
			.Replace(Path.AltDirectorySeparatorChar, _PORTABLE_DIRECTORY_SEPARATOR);

		return portablePath.TrimEnd(_PORTABLE_DIRECTORY_SEPARATOR).ToUpperInvariant();
	}

	private static SessionStateDocument BuildDocument(SessionState state) =>
		new()
		{
			SchemaVersion = CURRENT_SCHEMA_VERSION,
			CatalogVersionMajor = state.CatalogVersion.Major,
			CatalogVersionMinor = state.CatalogVersion.Minor,
			CatalogVersionRevision = state.CatalogVersion.Revision,
			RepositoryRootLabel = state.RepositoryRootLabel,
			CompletedStages = [.. state.CompletedStagesInRunOrder().Select(stage => stage.Name)],
			CandidateRelativePaths = [.. state.CandidateRelativePaths],
			Findings = [.. state.Findings.Select(BuildFindingDocument)],
			ScanSummary = BuildScanSummaryDocument(state.ScanOutcome),
			ScanSkippedItems = [.. state.ScanSkippedItems.Select(BuildSkippedItemTallyDocument)],
			AnalysisSkippedItems = [.. state.AnalysisSkippedItems.Select(BuildSkippedItemTallyDocument)],
			FindingsCapOutcome = BuildFindingsCapOutcomeDocument(state.FindingsCapOutcome),
			ScanEnabledLanguages = [.. state.ScanEnabledLanguages.Select(AssessedLanguages.CanonicalName)],
			CandidateLanguages = [.. state.CandidateLanguageNames],
			DefaultFlow = CredentialFlows.Name(state.DefaultFlow.Flow),
			DefaultFlowOrigin = ReportVocabulary.DefaultFlowOriginValueOf(state.DefaultFlow.Origin),
			FlowEvaluationDiagnostics = new PersistedFlowEvaluationDiagnosticsDocument
			{
				FilesEvaluated = state.FlowEvaluationDiagnostics.FilesEvaluated,
				SignalTripCount = state.FlowEvaluationDiagnostics.SignalTripCount
			}
		};

	/// <summary>
	/// One finding, flattened to the persisted shape. Only the terms that are not derivable are
	/// written - see <see cref="PersistedFindingDocument"/> for why the identifier, the fingerprint
	/// path and the order key are absent.
	/// </summary>
	private static PersistedFindingDocument BuildFindingDocument(Finding finding) =>
		new()
		{
			SequentialIndex = finding.SequentialIndex,
			RelativePath = finding.Path.Value,
			Line = finding.Line,
			Column = finding.Column,
			Language = AssessedLanguages.CanonicalName(finding.Language),
			PatternClass = finding.PatternClass.Value,
			ConfidenceLevel = ConfidenceLevels.Name(finding.Level),
			DestinationCertainty = finding.DestinationCertainty is Catalog.DestinationCertainty.Established
				? DESTINATION_ESTABLISHED_NAME
				: DESTINATION_UNKNOWN_NAME,
			LiteralCredentialSignal = finding.LiteralCredentialSignal,
			OccurrenceOrdinal = finding.Fingerprint.OccurrenceOrdinal,
			FlowEvidence = finding.FlowEvidence is FlowEvidence evidence ? NameOfEvidence(evidence) : null,
			Flow = finding.Flow is CredentialFlow flow ? CredentialFlows.Name(flow) : null,
			FlowDecision = finding.FlowDecision is FlowDecision decision ? CredentialFlows.Name(decision) : null,
			LowReasons = finding.LowReasons == LowConfidenceReasons.None ? null : LowReasonVocabulary.ValuesOf(finding.LowReasons)
		};

	/// <summary>
	/// Section 2's numbers, flattened. The share percentage is not written - see
	/// <see cref="PersistedScanSummaryDocument"/> for why it is derived on read instead.
	/// </summary>
	private static PersistedScanSummaryDocument BuildScanSummaryDocument(ScanSummary scanSummary) =>
		new()
		{
			FileCount = scanSummary.FileCount,
			LineCount = scanSummary.LineCount,
			CandidateFileCount = scanSummary.CandidateFileCount,
			Rows = [.. scanSummary.Rows.Select(BuildLanguageInventoryRowDocument)],
			FirstLineFallbackCount = scanSummary.FirstLineFallbackCount,
			ExcludedFolderCount = scanSummary.ExcludedFolders.OccurrenceCount,
			ExcludedFolderPaths = [.. scanSummary.ExcludedFolders.Paths.Select(path => path.Value)],
			LegacyEncodedFileCount = scanSummary.LegacyEncodedFiles.OccurrenceCount,
			LegacyEncodedFilePaths = [.. scanSummary.LegacyEncodedFiles.Paths.Select(path => path.Value)]
		};

	private static PersistedLanguageInventoryRowDocument BuildLanguageInventoryRowDocument(LanguageInventoryRow row) =>
		new()
		{
			LanguageName = row.LanguageName,
			FileCount = row.FileCount,
			LineCount = row.LineCount,
			CandidateFileCount = row.CandidateFileCount
		};

	/// <summary>One category's tally, keyed by <see cref="SkippedItemCategoryNames"/>'s declared name.</summary>
	private static PersistedSkippedItemTallyDocument BuildSkippedItemTallyDocument(SkippedItemTally tally) =>
		new()
		{
			Category = SkippedItemCategoryNames.NameOf(tally.Category),
			Count = tally.Count,
			EnumeratedPaths = [.. tally.EnumeratedPaths.Select(path => path.Value)]
		};

	/// <summary>The findings-cap truncation counter, flattened.</summary>
	private static PersistedFindingsCapOutcomeDocument BuildFindingsCapOutcomeDocument(FindingsCapOutcome outcome) =>
		new()
		{
			TotalRecognizedCount = outcome.TotalRecognizedCount,
			RetainedCount = outcome.RetainedCount,
			PerFileCapTrippedFileCount = outcome.PerFileCapTrippedFileCount,
			PerFileOmittedCount = outcome.PerFileOmittedCount,
			GlobalCapTripped = outcome.GlobalCapTripped,
			GlobalOmittedCount = outcome.GlobalOmittedCount
		};

	/// <summary>
	/// Streams the document over a file created through <see cref="RestrictiveFileCreation"/>, which is
	/// the one place a file this product writes is created (the report and the data artifact take the
	/// same permissions, so the three cannot drift apart, and the declared Windows divergence is
	/// stated there). The stream in between counts the bytes and refuses the write past the ceiling.
	/// </summary>
	private void WriteDocumentStreaming(string temporaryPath, SessionStateDocument document)
	{
		using FileStream fileStream = RestrictiveFileCreation.CreateStream(temporaryPath, FileMode.Create);
		using CeilingEnforcingStream ceilingStream = new(fileStream, _maximumStateFileLengthInBytes);

		JsonSerializer.Serialize(ceilingStream, document, SessionStateJsonContext.Default.SessionStateDocument);
	}

	/// <summary>
	/// Removes the temporary file of a save that did not commit. Failing to remove it is not worth
	/// hiding the failure that led here, and a leftover temporary is overwritten by the next save.
	/// </summary>
	private static void DiscardTemporaryFile(string temporaryPath)
	{
		try
		{
			File.Delete(temporaryPath);
		}
		catch (IOException)
		{
			// Nothing further to do: the previous state file was never touched.
		}
		catch (UnauthorizedAccessException)
		{
			// As above.
		}
	}

	private DocumentReadResult ReadDocument(string statePath)
	{
		SessionStateDocument? document = null;
		SessionStateRejectionReason reason;

		try
		{
			FileInfo stateFile = new(statePath);

			if (!stateFile.Exists)
			{
				reason = SessionStateRejectionReason.FileAbsent;
			}
			else
			{
				document = DeserializeStreaming(statePath, _maximumStateFileLengthInBytes);

				reason = document is null
					? SessionStateRejectionReason.DocumentMalformed
					: SessionStateRejectionReason.None;
			}
		}
		catch (ReadCeilingExceededException)
		{
			document = null;
			reason = SessionStateRejectionReason.FileTooLarge;
		}
		catch (JsonException)
		{
			document = null;
			reason = SessionStateRejectionReason.DocumentMalformed;
		}
		catch (IOException)
		{
			document = null;
			reason = SessionStateRejectionReason.FileUnreadable;
		}
		catch (UnauthorizedAccessException)
		{
			document = null;
			reason = SessionStateRejectionReason.FileUnreadable;
		}
		catch (NotSupportedException)
		{
			document = null;
			reason = SessionStateRejectionReason.FileUnreadable;
		}

		return new DocumentReadResult(document, reason);
	}

	/// <summary>
	/// Reads the document by streaming over the file with the source-generated context, so the
	/// text of a file the size of the ceiling is never held as one string beside the object graph it
	/// produces. The ceiling is enforced on the opened stream and on the bytes it delivers, not on a
	/// length read before the open, so a file that grows after the open cannot pass it.
	/// </summary>
	/// <exception cref="ReadCeilingExceededException">The file holds more than the ceiling.</exception>
	private static SessionStateDocument? DeserializeStreaming(string statePath, long ceilingInBytes)
	{
		using FileStream stateStream = new(statePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		using ReadCeilingEnforcingStream boundedStream = ReadCeilingEnforcingStream.Wrap(stateStream, ceilingInBytes);

		return JsonSerializer.Deserialize(boundedStream, SessionStateJsonContext.Default.SessionStateDocument);
	}

	/// <summary>
	/// THE ORCHESTRATOR, KEPT TO THE FOUR STEPS THE READ PATH ACTUALLY HAS: identity, the three
	/// parts every schema version has carried, the three added later, and the outcome. Each step is
	/// its own method precisely so this one stays short enough to read as that list - see
	/// <see cref="ReadCoreSections"/> and <see cref="ReadOutcomeSections"/> for what "the three" and
	/// "the three added later" actually check.
	/// </summary>
	private static SessionStateLoad ValidateDocument(
		SessionStateDocument document,
		LanguageCatalog catalog,
		RepositoryFolderPath repository)
	{
		string repositoryRootLabel = repository.Label;
		SessionStateRejectionReason reason = ValidateIdentity(document, catalog.Version, repositoryRootLabel);
		ImmutableArray<CommandDescriptor> completedStages = [];
		ImmutableArray<string> candidateRelativePaths = [];
		ImmutableArray<Finding> findings = [];
		DocumentOutcomeSections outcome = DocumentOutcomeSections.Empty;

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadCoreSections(document, repository, out completedStages, out candidateRelativePaths, out findings);
		}

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadOutcomeSections(
				document,
				catalog,
				repository,
				completedStages,
				candidateRelativePaths.Length,
				findings.Length,
				out outcome);
		}

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadFlowSections(document, catalog, completedStages, candidateRelativePaths.Length, findings, ref outcome);
		}

		return BuildLoad(reason, catalog.Version, repositoryRootLabel, completedStages, candidateRelativePaths, findings, outcome);
	}

	/// <summary>
	/// SCHEMA VERSION 5'S ADDITIONS: each candidate's attribution, the effective default flow
	/// with its origin, and the flow-signal counters. Read after the sections they are
	/// cross-checked against, and folded into <paramref name="outcome"/> so the load path does
	/// not widen.
	/// </summary>
	private static SessionStateRejectionReason ReadFlowSections(
		SessionStateDocument document,
		LanguageCatalog catalog,
		ImmutableArray<CommandDescriptor> completedStages,
		int candidateRelativePathCount,
		ImmutableArray<Finding> findings,
		ref DocumentOutcomeSections outcome)
	{
		bool analyzeCompleted = IsCompleted(completedStages, CommandId.Analyze);
		SessionStateRejectionReason reason = ReadCandidateLanguages(
			document.CandidateLanguages,
			candidateRelativePathCount,
			catalog,
			out ImmutableArray<string> candidateLanguageNames);
		EffectiveDefaultFlow defaultFlow = EffectiveDefaultFlow.BuiltIn;
		FlowEvaluationDiagnostics diagnostics = FlowEvaluationDiagnostics.Empty;

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadDefaultFlow(document, findings, analyzeCompleted, out defaultFlow);
		}

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadFlowEvaluationDiagnostics(
				document.FlowEvaluationDiagnostics,
				candidateRelativePathCount,
				analyzeCompleted,
				out diagnostics);
		}

		outcome = outcome with
		{
			CandidateLanguageNames = candidateLanguageNames,
			DefaultFlow = defaultFlow,
			FlowEvaluationDiagnostics = diagnostics
		};

		return reason;
	}

	/// <summary>The three parts every schema version since 2 has carried: completed stages, candidates, findings.</summary>
	private static SessionStateRejectionReason ReadCoreSections(
		SessionStateDocument document,
		RepositoryFolderPath repository,
		out ImmutableArray<CommandDescriptor> completedStages,
		out ImmutableArray<string> candidateRelativePaths,
		out ImmutableArray<Finding> findings)
	{
		candidateRelativePaths = [];
		findings = [];

		SessionStateRejectionReason reason = ReadCompletedStages(document.CompletedStages, out completedStages);

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadCandidateRelativePaths(document.CandidateRelativePaths, out candidateRelativePaths);
		}

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadFindings(document.Findings, repository, out findings);
		}

		return reason;
	}

	/// <summary>
	/// THE THREE LATER ADDITIONS PLUS THE FOURTH, EACH CHECKED AGAINST WHETHER THE STAGE THAT
	/// PRODUCES IT IS RECORDED COMPLETE . <paramref name="completedStages"/> is what
	/// <see cref="ReadCoreSections"/> already validated as prefix-closed, so "scan complete" and
	/// "analyze complete" read from it are themselves trustworthy by the time this method asks.
	///
	/// The scan summary's own candidate count is cross-checked here against
	/// <paramref name="candidateRelativePathCount"/> - <see cref="ReadCoreSections"/>'s independently
	/// validated count of the candidate paths the same document carries - because the two are written
	/// from the same scan and can never legitimately disagree; a document asserting otherwise was not
	/// produced by this tool.
	/// </summary>
	private static SessionStateRejectionReason ReadOutcomeSections(
		SessionStateDocument document,
		LanguageCatalog catalog,
		RepositoryFolderPath repository,
		ImmutableArray<CommandDescriptor> completedStages,
		int candidateRelativePathCount,
		int findingsCount,
		out DocumentOutcomeSections outcome)
	{
		bool scanCompleted = IsCompleted(completedStages, CommandId.Scan);
		bool analyzeCompleted = IsCompleted(completedStages, CommandId.Analyze);
		ScanSummary scanSummary = ScanSummary.Empty;
		ImmutableArray<SkippedItemTally> scanSkippedItems = SkippedItemTallies.Empty;
		ImmutableArray<SkippedItemTally> analysisSkippedItems = SkippedItemTallies.Empty;
		FindingsCapOutcome findingsCapOutcome = FindingsCapOutcome.Empty;
		ImmutableArray<AssessedLanguage> scanEnabledLanguages = [];

		SessionStateRejectionReason reason = ReadScanSummary(document.ScanSummary, catalog, repository, scanCompleted, out scanSummary);

		if (reason is SessionStateRejectionReason.None && scanSummary.CandidateFileCount != candidateRelativePathCount)
		{
			reason = SessionStateRejectionReason.ScanSummaryNotWellFormed;
		}

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadSkippedItemTallies(document.ScanSkippedItems, repository, scanCompleted, out scanSkippedItems);
		}

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadSkippedItemTallies(document.AnalysisSkippedItems, repository, analyzeCompleted, out analysisSkippedItems);
		}

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadFindingsCapOutcome(document.FindingsCapOutcome, findingsCount, analyzeCompleted, out findingsCapOutcome);
		}

		if (reason is SessionStateRejectionReason.None)
		{
			reason = ReadScanEnabledLanguages(document.ScanEnabledLanguages, scanCompleted, out scanEnabledLanguages);
		}

		outcome = new DocumentOutcomeSections(
			scanSummary,
			scanSkippedItems,
			analysisSkippedItems,
			findingsCapOutcome,
			scanEnabledLanguages,
			[],
			EffectiveDefaultFlow.BuiltIn,
			FlowEvaluationDiagnostics.Empty);

		return reason;
	}

	/// <summary>Whether the closed, prefix-closed set of completed stages names this one.</summary>
	private static bool IsCompleted(ImmutableArray<CommandDescriptor> completedStages, CommandId stage)
	{
		return completedStages.Any(completedStage => completedStage.Id == stage);
	}

	private static SessionStateLoad BuildLoad(
		SessionStateRejectionReason reason,
		CatalogVersion catalogVersion,
		string repositoryRootLabel,
		ImmutableArray<CommandDescriptor> completedStages,
		ImmutableArray<string> candidateRelativePaths,
		ImmutableArray<Finding> findings,
		DocumentOutcomeSections outcome)
	{
		return reason is SessionStateRejectionReason.None
			? new SessionStateLoad
			{
				State = SessionState.Resumed(
					catalogVersion,
					repositoryRootLabel,
					completedStages,
					candidateRelativePaths,
					outcome.CandidateLanguageNames,
					findings,
					outcome.ScanSummary,
					outcome.ScanSkippedItems,
					outcome.AnalysisSkippedItems,
					outcome.FindingsCapOutcome,
					outcome.ScanEnabledLanguages,
					outcome.DefaultFlow,
					outcome.FlowEvaluationDiagnostics),
				WasResumed = true,
				RejectionReason = SessionStateRejectionReason.None
			}
			: BuildFreshLoad(reason, catalogVersion, repositoryRootLabel);
	}

	/// <summary>
	/// THE THREE LATER ADDITIONS PLUS THE FOURTH, CARRIED AS ONE VALUE SO
	/// <see cref="ReadOutcomeSections"/> AND <see cref="BuildLoad"/> DO NOT EACH TAKE FIVE MORE
	/// PARAMETERS FOR THEM. Not part of <see cref="SessionState"/>'s own surface - it exists only for
	/// the width of this read path.
	/// </summary>
	private readonly record struct DocumentOutcomeSections(
		ScanSummary ScanSummary,
		ImmutableArray<SkippedItemTally> ScanSkippedItems,
		ImmutableArray<SkippedItemTally> AnalysisSkippedItems,
		FindingsCapOutcome FindingsCapOutcome,
		ImmutableArray<AssessedLanguage> ScanEnabledLanguages,
		ImmutableArray<string> CandidateLanguageNames,
		EffectiveDefaultFlow DefaultFlow,
		FlowEvaluationDiagnostics FlowEvaluationDiagnostics)
	{
		internal static DocumentOutcomeSections Empty { get; } = new(
			ScanSummary.Empty,
			SkippedItemTallies.Empty,
			SkippedItemTallies.Empty,
			FindingsCapOutcome.Empty,
			[],
			[],
			EffectiveDefaultFlow.BuiltIn,
			FlowEvaluationDiagnostics.Empty);
	}

	private static SessionStateRejectionReason ValidateIdentity(
		SessionStateDocument document,
		CatalogVersion catalogVersion,
		string repositoryRootLabel)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;

		if (document.SchemaVersion != CURRENT_SCHEMA_VERSION)
		{
			reason = SessionStateRejectionReason.SchemaVersionMismatch;
		}
		else if (!MatchesCatalogVersion(document, catalogVersion))
		{
			reason = SessionStateRejectionReason.CatalogVersionMismatch;
		}
		else if (!string.Equals(document.RepositoryRootLabel, repositoryRootLabel, StringComparison.Ordinal))
		{
			reason = SessionStateRejectionReason.RepositoryLabelMismatch;
		}

		return reason;
	}

	/// <summary>
	/// The comparison is by component and not by text, because the catalog version is a value
	/// rather than a string.
	/// </summary>
	private static bool MatchesCatalogVersion(SessionStateDocument document, CatalogVersion catalogVersion)
	{
		bool matches = false;

		if (document.CatalogVersionMajor is int major
			&& document.CatalogVersionMinor is int minor
			&& document.CatalogVersionRevision is int revision
			&& major >= 0
			&& minor >= 0
			&& revision >= 0)
		{
			matches = new CatalogVersion(major, minor, revision).CompareTo(catalogVersion) == 0;
		}

		return matches;
	}

	/// <summary>
	/// Reads the completed stages, rejecting anything a real session could not have produced:
	/// a name that is not a workflow command, a repeated name, or a set that is not closed under
	/// the gating chain. The last one is the interesting check - a state claiming a completed
	/// <c>report</c> with no completed <c>scan</c> was not written by this tool.
	/// </summary>
	private static SessionStateRejectionReason ReadCompletedStages(
		string[]? completedStageNames,
		out ImmutableArray<CommandDescriptor> completedStages)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;
		HashSet<CommandId> completedIdentities = [];

		foreach (string stageName in completedStageNames ?? [])
		{
			bool isWorkflowCommand =
				CommandRegistry.TryResolve(stageName, out CommandDescriptor? stage)
				&& stage!.Group is CommandGroup.Workflow
				&& string.Equals(stage.Name, stageName, StringComparison.Ordinal);

			if (!isWorkflowCommand || !completedIdentities.Add(stage!.Id))
			{
				reason = SessionStateRejectionReason.CompletedStageUnknown;
				break;
			}
		}

		if (reason is SessionStateRejectionReason.None && !IsPrefixClosed(completedIdentities))
		{
			reason = SessionStateRejectionReason.CompletedStagesNotPrefixClosed;
		}

		completedStages = reason is SessionStateRejectionReason.None
			? [.. CommandRegistry.WorkflowCommandsInRunOrder.Where(stage => completedIdentities.Contains(stage.Id))]
			: [];

		return reason;
	}

	private static bool IsPrefixClosed(HashSet<CommandId> completedIdentities)
	{
		bool isPrefixClosed = true;

		foreach (CommandId completedIdentity in completedIdentities)
		{
			CommandId? predecessor = CommandRegistry.Describe(completedIdentity).GatingPredecessor;

			if (predecessor is CommandId required && !completedIdentities.Contains(required))
			{
				isPrefixClosed = false;
				break;
			}
		}

		return isPrefixClosed;
	}

	/// <summary>
	/// The read-path containment check. The state file is untrusted and a path escaping the
	/// repository root is schema-valid, so the candidates are checked before anything opens
	/// them rather than after.
	/// </summary>
	private static SessionStateRejectionReason ReadCandidateRelativePaths(
		string[]? candidateRelativePaths,
		out ImmutableArray<string> containedPaths)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;
		ImmutableArray<string>.Builder accepted = ImmutableArray.CreateBuilder<string>();

		foreach (string candidatePath in candidateRelativePaths ?? [])
		{
			if (IsContainedRelativePath(candidatePath))
			{
				accepted.Add(candidatePath);
			}
			else
			{
				reason = SessionStateRejectionReason.CandidatePathNotContained;
				break;
			}
		}

		containedPaths = reason is SessionStateRejectionReason.None ? accepted.ToImmutable() : [];

		return reason;
	}

	private static bool IsContainedRelativePath(string candidatePath)
	{
		string[] segments = candidatePath.Split(
			[Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, _PORTABLE_DIRECTORY_SEPARATOR]);

		bool isContained =
			!string.IsNullOrWhiteSpace(candidatePath)
			&& !Path.IsPathRooted(candidatePath)
			&& !segments.Contains(_PARENT_DIRECTORY_SEGMENT, StringComparer.Ordinal);

		return isContained;
	}

	/// <summary>
	/// THE THIRD PART OF THE STATE, READ AS HOSTILE INPUT RATHER THAN AS OUR OWN OUTPUT.
	///
	/// This is the check the class comment names as the reason the whole file is treated as
	/// untrusted: "the readable-and-hostile case is what would otherwise let someone inject
	/// fabricated findings into a client's report." Everything below exists because a finding that
	/// reaches <see cref="SessionState"/> is written verbatim into a Markdown report and a data
	/// artifact that a client reads and acts on.
	///
	/// THE WHOLE SET IS REFUSED WHEN ANY ONE FINDING IS MALFORMED, never the malformed one alone.
	/// A partially accepted set would present as a complete analysis while silently missing
	/// whatever was dropped, and the identifiers are assigned over the whole - so a set with a
	/// hole in it is not a smaller finding set, it is a finding set that lies about its own
	/// numbering. A fresh session is the honest outcome, and <c>analyze</c> simply runs again.
	///
	/// THE INDEX IS CHECKED AGAINST THE POSITION, which is the one structural check the per-field
	/// validation cannot supply. The full ordered set is numbered from one with no gaps, so the
	/// finding at position n must carry index n+1; an edited file that reorders, duplicates or
	/// removes an entry fails here even when every individual field is well formed.
	/// </summary>
	private static SessionStateRejectionReason ReadFindings(
		PersistedFindingDocument[]? persistedFindings,
		RepositoryFolderPath repository,
		out ImmutableArray<Finding> findings)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;
		ImmutableArray<Finding>.Builder accepted = ImmutableArray.CreateBuilder<Finding>();
		int expectedIndex = FindingIdentifiers.FIRST_INDEX;

		foreach (PersistedFindingDocument persistedFinding in persistedFindings ?? [])
		{
			if (TryReadOneFinding(persistedFinding, repository, expectedIndex, out Finding finding))
			{
				accepted.Add(finding);
				expectedIndex++;
			}
			else
			{
				reason = SessionStateRejectionReason.FindingNotWellFormed;
				break;
			}
		}

		findings = reason is SessionStateRejectionReason.None
			? PathDisambiguation.Apply(accepted.ToImmutable())
			: [];

		return reason;
	}

	/// <summary>
	/// One persisted finding, rebuilt through the same constructors the engine itself is confined
	/// to. The typed path comes from <see cref="RepositoryRelativePath.TryFromScannedPath"/> and
	/// the pattern class from <see cref="RuleIdentifier.TryFromPatternClass"/>, so a path escaping
	/// the repository and a pattern class carrying arbitrary text are both refused by the same
	/// primitives that refuse them everywhere else - rather than by a second rule written here that
	/// could come out differently.
	///
	/// The fingerprint is REBUILT rather than read, from the path and pattern class just validated
	/// plus the persisted ordinal, exactly as <see cref="FindingComposition"/> builds it. That is
	/// what makes a fingerprint inconsistent with its own finding unrepresentable in the file.
	/// The path disambiguator is not in the file either: <see cref="PathDisambiguation"/> derives it
	/// over the whole loaded set, in <see cref="ReadFindings"/>, exactly as composition does.
	/// </summary>
	private static bool TryReadOneFinding(
		PersistedFindingDocument persistedFinding,
		RepositoryFolderPath repository,
		int expectedIndex,
		out Finding finding)
	{
		finding = default;

		bool wellFormed = false;

		if (persistedFinding.SequentialIndex == expectedIndex
			&& persistedFinding.Line is int line
			&& line >= _FIRST_LINE_OR_COLUMN
			&& persistedFinding.Column is int column
			&& column >= _FIRST_LINE_OR_COLUMN
			&& persistedFinding.OccurrenceOrdinal is int occurrenceOrdinal
			&& occurrenceOrdinal >= FindingFingerprint.FIRST_OCCURRENCE_ORDINAL
			&& persistedFinding.LiteralCredentialSignal is bool literalCredentialSignal
			&& persistedFinding.RelativePath is string relativePathText
			&& IsContainedRelativePath(relativePathText)
			&& RepositoryRelativePath.TryFromScannedPath(repository, relativePathText, out RepositoryRelativePath path)
			&& AssessedLanguages.TryParse(persistedFinding.Language ?? string.Empty, out AssessedLanguage language)
			&& RuleIdentifier.TryFromPatternClass(persistedFinding.PatternClass ?? string.Empty, out RuleIdentifier patternClass)
			&& ConfidenceLevels.TryParse(persistedFinding.ConfidenceLevel, out ConfidenceLevel level)
			&& TryReadDestinationCertainty(
				persistedFinding.DestinationCertainty,
				out Catalog.DestinationCertainty destinationCertainty)
			&& TryReadFlowFields(persistedFinding, level, out PersistedFlowFields flowFields)
			&& TryReadLowReasons(persistedFinding, level, out LowConfidenceReasons lowReasons))
		{
			finding = BuildFinding(
				new PersistedFindingScalars(line, column, literalCredentialSignal, occurrenceOrdinal),
				expectedIndex,
				path,
				language,
				patternClass,
				level,
				destinationCertainty,
				flowFields,
				lowReasons);

			wellFormed = true;
		}

		return wellFormed;
	}

	/// <summary>
	/// Assembles the finding from fields already validated by <see cref="TryReadOneFinding"/>,
	/// rebuilding the fingerprint from the validated path and pattern class plus the persisted ordinal.
	/// </summary>
	private static Finding BuildFinding(
		PersistedFindingScalars scalars,
		int expectedIndex,
		RepositoryRelativePath path,
		AssessedLanguage language,
		RuleIdentifier patternClass,
		ConfidenceLevel level,
		Catalog.DestinationCertainty destinationCertainty,
		PersistedFlowFields flowFields,
		LowConfidenceReasons lowReasons)
	{
		Finding finding = new Finding
		{
			Identifier = FindingIdentifiers.Of(expectedIndex),
			SequentialIndex = expectedIndex,
			Path = path,
			Line = scalars.Line,
			Column = scalars.Column,
			Language = language,
			PatternClass = patternClass,
			Level = level,
			DestinationCertainty = destinationCertainty,
			LiteralCredentialSignal = scalars.LiteralCredentialSignal,
			LowReasons = lowReasons,
			FlowEvidence = flowFields.Evidence,
			Flow = flowFields.Flow,
			FlowDecision = flowFields.Decision,
			Fingerprint = new FindingFingerprint
			{
				Path = NormalizedPathKey.From(path),
				PatternClass = patternClass,
				OccurrenceOrdinal = scalars.OccurrenceOrdinal
			}
		};

		return finding;
	}

	/// <summary>
	/// SECTION 2 ON THE READ PATH. Null is accepted only where <paramref name="scanCompleted"/>
	/// is false - <see cref="ScanSummary.Empty"/> is what a fresh session already starts with, so
	/// there is nothing to reject about a document an earlier build wrote, or one this build
	/// wrote before <c>scan</c> ever ran. NULL BESIDE A COMPLETED SCAN IS REJECTED : a
	/// completed scan always snapshots its own summary, so a document claiming one without the other
	/// was not written by this tool.
	/// </summary>
	private static SessionStateRejectionReason ReadScanSummary(
		PersistedScanSummaryDocument? persisted,
		LanguageCatalog catalog,
		RepositoryFolderPath repository,
		bool scanCompleted,
		out ScanSummary scanSummary)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;

		scanSummary = ScanSummary.Empty;

		if (persisted is not null)
		{
			reason = TryReadScanSummary(persisted, catalog, repository, out ScanSummary read)
				? SessionStateRejectionReason.None
				: SessionStateRejectionReason.ScanSummaryNotWellFormed;

			if (reason is SessionStateRejectionReason.None)
			{
				scanSummary = read;
			}
		}
		else if (scanCompleted)
		{
			reason = SessionStateRejectionReason.ScanSummaryNotWellFormed;
		}

		return reason;
	}

	private static bool TryReadScanSummary(
		PersistedScanSummaryDocument persisted,
		LanguageCatalog catalog,
		RepositoryFolderPath repository,
		out ScanSummary scanSummary)
	{
		scanSummary = ScanSummary.Empty;

		bool wellFormed = false;

		if (persisted.FileCount >= 0
			&& persisted.LineCount >= 0
			&& persisted.CandidateFileCount >= 0
			&& (persisted.FirstLineFallbackCount ?? 0) >= 0
			&& (persisted.FirstLineFallbackCount ?? 0) <= persisted.FileCount
			&& TryReadInventoryRows(persisted.Rows, persisted.FileCount.Value, catalog, out ImmutableArray<LanguageInventoryRow> rows)
			&& rows.Sum(row => row.FileCount) == persisted.FileCount.Value
			&& rows.Sum(row => row.LineCount) == persisted.LineCount.Value
			&& rows.Sum(row => row.CandidateFileCount) == persisted.CandidateFileCount.Value
			&& (persisted.ExcludedFolderCount ?? 0) >= 0
			&& TryReadEnumeratedPaths(
				persisted.ExcludedFolderPaths,
				persisted.ExcludedFolderCount ?? 0,
				repository,
				out ImmutableArray<RepositoryRelativePath> excludedFolderPaths)
			&& (persisted.LegacyEncodedFileCount ?? 0) >= 0
			&& (persisted.LegacyEncodedFileCount ?? 0) <= persisted.FileCount
			&& TryReadEnumeratedPaths(
				persisted.LegacyEncodedFilePaths,
				persisted.LegacyEncodedFileCount ?? 0,
				repository,
				out ImmutableArray<RepositoryRelativePath> legacyEncodedFilePaths))
		{
			scanSummary = new ScanSummary
			{
				FileCount = persisted.FileCount.Value,
				LineCount = persisted.LineCount.Value,
				CandidateFileCount = persisted.CandidateFileCount.Value,
				Rows = rows,
				FirstLineFallbackCount = persisted.FirstLineFallbackCount ?? 0,
				ExcludedFolders = new ExcludedFolderSummary
				{
					OccurrenceCount = persisted.ExcludedFolderCount ?? 0,
					Paths = excludedFolderPaths
				},
				LegacyEncodedFiles = new LegacyEncodedFileSummary
				{
					OccurrenceCount = persisted.LegacyEncodedFileCount ?? 0,
					Paths = legacyEncodedFilePaths
				}
			};

			wellFormed = true;
		}

		return wellFormed;
	}

	/// <summary>
	/// The rows the totals above must sum from. The share percentage is never read - it is
	/// recomputed here with the SAME FORMULA <see cref="LanguageInventory"/> uses, so a resumed
	/// value is bit-identical to the one the running scan would have shown, rather than a second
	/// stored copy that could drift from the totals beside it.
	///
	/// Two cross-checks a real scan can never violate. A language appears at
	/// most once - <see cref="LanguageInventory"/> holds one tally per language, so a second row for
	/// a language already seen is rejected rather than silently accepted as a second entry the sums
	/// above would then double-count. And a row's own candidate count never exceeds its own file
	/// count - a candidate is a counted file by construction, never a file the row does not have.
	/// </summary>
	private static bool TryReadInventoryRows(
		PersistedLanguageInventoryRowDocument[]? persistedRows,
		int totalFileCount,
		LanguageCatalog catalog,
		out ImmutableArray<LanguageInventoryRow> rows)
	{
		ImmutableArray<LanguageInventoryRow>.Builder accepted = ImmutableArray.CreateBuilder<LanguageInventoryRow>();
		HashSet<string> seenLanguageNames = new(StringComparer.Ordinal);
		bool wellFormed = true;

		foreach (PersistedLanguageInventoryRowDocument persistedRow in persistedRows ?? [])
		{
			if (persistedRow.FileCount >= 0
				&& persistedRow.LineCount >= 0
				&& persistedRow.CandidateFileCount >= 0
				&& persistedRow.CandidateFileCount <= persistedRow.FileCount
				&& IsValidInventoryLanguageName(persistedRow.LanguageName, catalog)
				&& seenLanguageNames.Add(persistedRow.LanguageName!))
			{
				accepted.Add(new LanguageInventoryRow
				{
					LanguageName = persistedRow.LanguageName!,
					FileCount = persistedRow.FileCount.Value,
					LineCount = persistedRow.LineCount.Value,
					CandidateFileCount = persistedRow.CandidateFileCount.Value,
					ShareOfFilesPercentage = totalFileCount == 0
						? 0.0
						: persistedRow.FileCount.Value * _PERCENTAGE_SCALE / totalFileCount
				});
			}
			else
			{
				wellFormed = false;
				break;
			}
		}

		rows = wellFormed ? accepted.ToImmutable() : [];

		return wellFormed;
	}

	/// <summary>
	/// THE CHECK ITSELF IS SOUND WITHOUT BEING A CLOSED LIST OF EIGHT, which is what the
	/// requirement asks for: an inventory row's name is exactly one of three shapes by construction
	/// - one of <see cref="AssessedLanguages.AllInDeclaredOrder"/>'s canonical names, the
	/// <see cref="Scanning.LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME"/> bucket, or a name
	/// <paramref name="catalog"/>'s own <see cref="LanguageCatalog.AnalyzersByLanguageName"/> declares
	/// - and no fourth shape reaches <c>ReportInventory</c> anywhere in this codebase. Checking
	/// membership in that union is therefore sound for every language this build could ever have
	/// written, including one not yet in the eight - a later language analyzer widens the third
	/// branch on its own, with no change owed here - while still refusing arbitrary text: a path, a
	/// line of source, or any other string an attacker might place matches none of the three.
	/// </summary>
	private static bool IsValidInventoryLanguageName(string? languageName, LanguageCatalog catalog)
	{
		return languageName is not null
			&& (AssessedLanguages.AllInDeclaredOrder.Any(language =>
					string.Equals(AssessedLanguages.CanonicalName(language), languageName, StringComparison.Ordinal))
				|| string.Equals(languageName, Scanning.LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, StringComparison.Ordinal)
				|| catalog.AnalyzersByLanguageName.ContainsKey(languageName));
	}

	/// <summary>
	/// SECTION 3 ON THE READ PATH. Null is accepted only where <paramref name="stageCompleted"/>
	/// is false, on the same terms <see cref="ReadScanSummary"/> documents - and rejected where it is
	/// true, for the same reason: <paramref name="stageCompleted"/> is Scan's completion for
	/// <c>scanSkippedItems</c> and Analysis's for <c>analysisSkippedItems</c>, and either stage always
	/// snapshots its own tallies on completion, empty or not.
	/// </summary>
	private static SessionStateRejectionReason ReadSkippedItemTallies(
		PersistedSkippedItemTallyDocument[]? persisted,
		RepositoryFolderPath repository,
		bool stageCompleted,
		out ImmutableArray<SkippedItemTally> tallies)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;

		tallies = SkippedItemTallies.Empty;

		if (persisted is not null)
		{
			reason = TryReadSkippedItemTallies(persisted, repository, out ImmutableArray<SkippedItemTally> read)
				? SessionStateRejectionReason.None
				: SessionStateRejectionReason.SkippedItemTallyNotWellFormed;

			if (reason is SessionStateRejectionReason.None)
			{
				tallies = read;
			}
		}
		else if (stageCompleted)
		{
			reason = SessionStateRejectionReason.SkippedItemTallyNotWellFormed;
		}

		return reason;
	}

	/// <summary>Exactly the eleven declared categories, each exactly once - the shape <see cref="SkippedItemLedger.Tallies"/> always produces.</summary>
	private static bool TryReadSkippedItemTallies(
		PersistedSkippedItemTallyDocument[] persisted,
		RepositoryFolderPath repository,
		out ImmutableArray<SkippedItemTally> tallies)
	{
		tallies = [];

		bool wellFormed = persisted.Length == SkippedItemCategories.ALL.Length;
		HashSet<SkippedItemCategory> seenCategories = [];
		ImmutableArray<SkippedItemTally>.Builder accepted = ImmutableArray.CreateBuilder<SkippedItemTally>(persisted.Length);

		foreach (PersistedSkippedItemTallyDocument persistedTally in wellFormed ? persisted : [])
		{
			if (TryReadOneSkippedItemTally(persistedTally, repository, seenCategories, out SkippedItemTally tally))
			{
				accepted.Add(tally);
			}
			else
			{
				wellFormed = false;
				break;
			}
		}

		if (wellFormed)
		{
			tallies = accepted.ToImmutable();
		}

		return wellFormed;
	}

	private static bool TryReadOneSkippedItemTally(
		PersistedSkippedItemTallyDocument persisted,
		RepositoryFolderPath repository,
		HashSet<SkippedItemCategory> seenCategories,
		out SkippedItemTally tally)
	{
		tally = null!;

		bool wellFormed = false;

		if (SkippedItemCategoryNames.TryParse(persisted.Category, out SkippedItemCategory category)
			&& seenCategories.Add(category)
			&& persisted.Count >= 0
			&& TryReadEnumeratedPaths(
				persisted.EnumeratedPaths,
				persisted.Count.Value,
				repository,
				out ImmutableArray<RepositoryRelativePath> enumeratedPaths))
		{
			tally = new SkippedItemTally
			{
				Category = category,
				Unit = SkippedItemCategories.UnitOf(category),
				Count = persisted.Count.Value,
				EnumeratedPaths = enumeratedPaths
			};

			wellFormed = true;
		}

		return wellFormed;
	}

	/// <summary>
	/// The bound on enumerated paths, enforced on the read path exactly as <see cref="ReadCandidateRelativePaths"/>
	/// already enforces containment: at most the count, at most the ceiling, and every one contained.
	/// </summary>
	private static bool TryReadEnumeratedPaths(
		string[]? persistedPaths,
		int count,
		RepositoryFolderPath repository,
		out ImmutableArray<RepositoryRelativePath> enumeratedPaths)
	{
		string[] pathTexts = persistedPaths ?? [];

		enumeratedPaths = [];

		bool wellFormed = pathTexts.Length <= count
			&& pathTexts.Length <= ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY;
		ImmutableArray<RepositoryRelativePath>.Builder accepted = ImmutableArray.CreateBuilder<RepositoryRelativePath>(pathTexts.Length);

		foreach (string pathText in wellFormed ? pathTexts : [])
		{
			if (IsContainedRelativePath(pathText)
				&& RepositoryRelativePath.TryFromScannedPath(repository, pathText, out RepositoryRelativePath path))
			{
				accepted.Add(path);
			}
			else
			{
				wellFormed = false;
				break;
			}
		}

		if (wellFormed)
		{
			enumeratedPaths = accepted.ToImmutable();
		}

		return wellFormed;
	}

	/// <summary>
	/// THE FINDINGS-CAP COUNTER ON THE READ PATH. Null is accepted only where
	/// <paramref name="analyzeCompleted"/> is false, on the same terms <see cref="ReadScanSummary"/>
	/// documents - rejected where it is true, for the same reason: a completed analysis
	/// always snapshots its own truncation counter, empty or not.
	/// </summary>
	private static SessionStateRejectionReason ReadFindingsCapOutcome(
		PersistedFindingsCapOutcomeDocument? persisted,
		int findingsCount,
		bool analyzeCompleted,
		out FindingsCapOutcome findingsCapOutcome)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;

		findingsCapOutcome = FindingsCapOutcome.Empty;

		if (persisted is not null)
		{
			reason = TryReadFindingsCapOutcome(persisted, findingsCount, out FindingsCapOutcome read)
				? SessionStateRejectionReason.None
				: SessionStateRejectionReason.FindingsCapOutcomeNotWellFormed;

			if (reason is SessionStateRejectionReason.None)
			{
				findingsCapOutcome = read;
			}
		}
		else if (analyzeCompleted)
		{
			reason = SessionStateRejectionReason.FindingsCapOutcomeNotWellFormed;
		}

		return reason;
	}

	/// <summary>
	/// THE RELATION <see cref="Findings.FindingComposition"/>'S ACCUMULATOR ALWAYS HOLDS, CHECKED
	/// RATHER THAN ASSUMED: every recognized construct is either retained or omitted by exactly one
	/// of the two caps, so the total is always the sum of the three; the global-tripped flag is
	/// always exactly whether the global omission is positive; and a file trips the per-file cap if
	/// and only if the per-file omission is positive. The retained count is additionally checked
	/// against the persisted finding SET's own length, which is what stops a file from claiming a
	/// truncation outcome for a finding set it does not actually carry.
	/// </summary>
	private static bool TryReadFindingsCapOutcome(
		PersistedFindingsCapOutcomeDocument persisted,
		int findingsCount,
		out FindingsCapOutcome findingsCapOutcome)
	{
		findingsCapOutcome = FindingsCapOutcome.Empty;

		bool wellFormed = false;

		if (persisted.TotalRecognizedCount >= 0
			&& persisted.RetainedCount >= 0
			&& persisted.PerFileCapTrippedFileCount >= 0
			&& persisted.PerFileOmittedCount >= 0
			&& persisted.GlobalOmittedCount >= 0
			&& persisted.GlobalCapTripped is bool globalCapTripped
			&& persisted.RetainedCount == findingsCount
			&& persisted.TotalRecognizedCount == persisted.RetainedCount + persisted.PerFileOmittedCount + persisted.GlobalOmittedCount
			&& globalCapTripped == persisted.GlobalOmittedCount > 0
			&& (persisted.PerFileCapTrippedFileCount == 0) == (persisted.PerFileOmittedCount == 0))
		{
			findingsCapOutcome = new FindingsCapOutcome
			{
				TotalRecognizedCount = persisted.TotalRecognizedCount.Value,
				RetainedCount = persisted.RetainedCount.Value,
				PerFileCapTrippedFileCount = persisted.PerFileCapTrippedFileCount.Value,
				PerFileOmittedCount = persisted.PerFileOmittedCount.Value,
				GlobalCapTripped = globalCapTripped,
				GlobalOmittedCount = persisted.GlobalOmittedCount.Value
			};

			wellFormed = true;
		}

		return wellFormed;
	}

	/// <summary>
	/// THE ENABLED-LANGUAGE SET ON THE READ PATH. Null is accepted only where
	/// <paramref name="scanCompleted"/> is false, on the same terms <see cref="ReadScanSummary"/>
	/// documents - rejected where it is true, for the same reason: a completed scan always
	/// snapshots the enabled set it narrowed candidate selection with, empty or not (an empty set is
	/// schema-valid here even though Pre-flight's own check refuses a session that would
	/// ever produce one in practice - this is the read path's own well-formedness question, not a
	/// re-litigation of that refusal).
	/// </summary>
	private static SessionStateRejectionReason ReadScanEnabledLanguages(
		string[]? persisted,
		bool scanCompleted,
		out ImmutableArray<AssessedLanguage> scanEnabledLanguages)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;

		scanEnabledLanguages = [];

		if (persisted is not null)
		{
			reason = TryReadScanEnabledLanguages(persisted, out ImmutableArray<AssessedLanguage> read)
				? SessionStateRejectionReason.None
				: SessionStateRejectionReason.ScanEnabledLanguagesNotWellFormed;

			if (reason is SessionStateRejectionReason.None)
			{
				scanEnabledLanguages = read;
			}
		}
		else if (scanCompleted)
		{
			reason = SessionStateRejectionReason.ScanEnabledLanguagesNotWellFormed;
		}

		return reason;
	}

	/// <summary>Each name one of the eight canonical names, and no name repeated.</summary>
	private static bool TryReadScanEnabledLanguages(
		string[] persisted,
		out ImmutableArray<AssessedLanguage> scanEnabledLanguages)
	{
		scanEnabledLanguages = [];

		bool wellFormed = true;
		HashSet<AssessedLanguage> seenLanguages = [];
		ImmutableArray<AssessedLanguage>.Builder accepted = ImmutableArray.CreateBuilder<AssessedLanguage>(persisted.Length);

		foreach (string languageName in persisted)
		{
			if (AssessedLanguages.TryParse(languageName, out AssessedLanguage language) && seenLanguages.Add(language))
			{
				accepted.Add(language);
			}
			else
			{
				wellFormed = false;
				break;
			}
		}

		if (wellFormed)
		{
			scanEnabledLanguages = accepted.ToImmutable();
		}

		return wellFormed;
	}

	/// <summary>
	/// A FINDING'S FLOW FIELDS ON THE READ PATH. A High or Medium finding carries all three,
	/// each a declared name, and the three must agree: an interactive signal can only have decided the
	/// authorization-code flow, a fixed one only client credentials, and a finding decided by the
	/// default carries no evidence that overrode it. A Low finding carries none of the three, as it
	/// does when the engine builds it. The pairing with the effective default is checked once the
	/// default is read - see <see cref="ReadDefaultFlow"/>.
	/// </summary>
	private static bool TryReadFlowFields(
		PersistedFindingDocument persistedFinding,
		ConfidenceLevel level,
		out PersistedFlowFields flowFields)
	{
		flowFields = default;

		bool wellFormed = false;

		if (level is ConfidenceLevel.High or ConfidenceLevel.Medium)
		{
			if (TryParseByName(persistedFinding.FlowEvidence, NameOfEvidence, out FlowEvidence evidence)
				&& TryParseByName(persistedFinding.Flow, CredentialFlows.Name, out CredentialFlow flow)
				&& TryParseByName(persistedFinding.FlowDecision, CredentialFlows.Name, out FlowDecision decision)
				&& IsDecisionConsistent(evidence, flow, decision))
			{
				flowFields = new PersistedFlowFields(evidence, flow, decision);
				wellFormed = true;
			}
		}
		else
		{
			wellFormed =
				persistedFinding.FlowEvidence is null
				&& persistedFinding.Flow is null
				&& persistedFinding.FlowDecision is null;
		}

		return wellFormed;
	}

	/// <summary>
	/// The reasons a finding is Low, from the closed vocabulary only. A reason beside a finding that is
	/// not Low was not written by this tool and refuses the document, as a flow beside a Low one does.
	/// </summary>
	private static bool TryReadLowReasons(
		PersistedFindingDocument persistedFinding,
		ConfidenceLevel level,
		out LowConfidenceReasons lowReasons)
	{
		bool wellFormed = LowReasonVocabulary.TryParse(persistedFinding.LowReasons, out lowReasons);

		return wellFormed && (level == ConfidenceLevel.Low || lowReasons == LowConfidenceReasons.None);
	}

	private static string NameOfEvidence(FlowEvidence evidence)
	{
		string name = evidence switch
		{
			FlowEvidence.None => FLOW_EVIDENCE_NONE_NAME,
			FlowEvidence.Fixed => FLOW_EVIDENCE_FIXED_NAME,
			FlowEvidence.Interactive => FLOW_EVIDENCE_INTERACTIVE_NAME,
			_ => throw new ArgumentOutOfRangeException(nameof(evidence), evidence, "No name is declared for this flow evidence.")
		};

		return name;
	}

	/// <summary>
	/// Exact and ordinal, against the declared names only. It is a walk over the enumeration's members
	/// rather than an enum parse for the reason <see cref="TryReadDestinationCertainty"/> gives: a
	/// reflective parse accepts the integer forms and other casings, and a file is not entitled to
	/// either.
	/// </summary>
	private static bool TryParseByName<TMember>(string? name, Func<TMember, string> nameOf, out TMember member)
		where TMember : struct, Enum
	{
		bool parsed = false;

		member = default;

		foreach (TMember candidate in Enum.GetValues<TMember>())
		{
			if (string.Equals(nameOf(candidate), name, StringComparison.Ordinal))
			{
				member = candidate;
				parsed = true;
				break;
			}
		}

		return parsed;
	}

	private static bool IsDecisionConsistent(FlowEvidence evidence, CredentialFlow flow, FlowDecision decision)
	{
		bool consistent = evidence switch
		{
			FlowEvidence.Interactive => decision is FlowDecision.InteractiveSignal && flow is CredentialFlow.AuthorizationCode,
			FlowEvidence.Fixed => decision is FlowDecision.FixedSignal && flow is CredentialFlow.ClientCredentials,
			_ => decision is FlowDecision.Default
		};

		return consistent;
	}

	/// <summary>
	/// EACH CANDIDATE'S ATTRIBUTION ON THE READ PATH. The array must be exactly as long as the
	/// candidate set, so a candidate cannot arrive without the language Scan attributed it to, and each
	/// name must be one a registered analyzer of this catalog declares, since a candidate is by
	/// definition a file some analyzer claimed. An absent array is an empty one, which is only ever
	/// well formed beside an empty candidate set.
	/// </summary>
	private static SessionStateRejectionReason ReadCandidateLanguages(
		string[]? persisted,
		int candidateCount,
		LanguageCatalog catalog,
		out ImmutableArray<string> candidateLanguageNames)
	{
		string[] persistedNames = persisted ?? [];
		HashSet<string> canonicalNames = new(StringComparer.Ordinal);
		ImmutableArray<string>.Builder accepted = ImmutableArray.CreateBuilder<string>(persistedNames.Length);
		bool wellFormed = persistedNames.Length == candidateCount;

		foreach (string persistedName in wellFormed ? persistedNames : [])
		{
			if (persistedName is not null && catalog.AnalyzersByLanguageName.ContainsKey(persistedName))
			{
				// One string instance per distinct name: 500,000 candidates hold a handful of names.
				canonicalNames.Add(persistedName);
				canonicalNames.TryGetValue(persistedName, out string? canonicalName);
				accepted.Add(canonicalName!);
			}
			else
			{
				wellFormed = false;
				break;
			}
		}

		candidateLanguageNames = wellFormed ? accepted.MoveToImmutable() : [];

		return wellFormed
			? SessionStateRejectionReason.None
			: SessionStateRejectionReason.CandidateAttributionNotWellFormed;
	}

	/// <summary>
	/// THE EFFECTIVE DEFAULT FLOW ON THE READ PATH. Both members or neither. Null is accepted
	/// only where <paramref name="analyzeCompleted"/> is false, on the same terms
	/// <see cref="ReadScanSummary"/> documents: a completed analysis always records the default it
	/// decided under. A finding decided by the default must carry exactly that flow - the persisted
	/// decisions and the persisted default are one analysis's output and cannot disagree.
	/// </summary>
	private static SessionStateRejectionReason ReadDefaultFlow(
		SessionStateDocument document,
		ImmutableArray<Finding> findings,
		bool analyzeCompleted,
		out EffectiveDefaultFlow defaultFlow)
	{
		bool absent = document.DefaultFlow is null && document.DefaultFlowOrigin is null;
		bool wellFormed = false;

		defaultFlow = EffectiveDefaultFlow.BuiltIn;

		if (absent)
		{
			wellFormed = !analyzeCompleted && AgreesWithDefaultDecisions(findings, defaultFlow.Flow);
		}
		else if (TryReadDefaultFlow(document.DefaultFlow, document.DefaultFlowOrigin, out EffectiveDefaultFlow read)
			&& AgreesWithDefaultDecisions(findings, read.Flow))
		{
			defaultFlow = read;
			wellFormed = true;
		}

		return wellFormed
			? SessionStateRejectionReason.None
			: SessionStateRejectionReason.DefaultFlowNotWellFormed;
	}

	/// <summary>Both declared names, and a built-in origin only beside the built-in flow.</summary>
	private static bool TryReadDefaultFlow(string? flowName, string? originName, out EffectiveDefaultFlow defaultFlow)
	{
		defaultFlow = EffectiveDefaultFlow.BuiltIn;

		bool wellFormed = false;

		if (TryParseByName(flowName, CredentialFlows.Name, out CredentialFlow flow)
			&& ReportVocabulary.TryParseDefaultFlowOrigin(originName, out ConfigurationSource origin)
			&& (origin is not ConfigurationSource.BuiltInDefault || flow == CredentialFlows.BUILT_IN_DEFAULT))
		{
			defaultFlow = new EffectiveDefaultFlow(flow, origin);
			wellFormed = true;
		}

		return wellFormed;
	}

	private static bool AgreesWithDefaultDecisions(ImmutableArray<Finding> findings, CredentialFlow defaultFlow)
	{
		bool agrees = true;

		foreach (Finding finding in findings)
		{
			if (finding.FlowDecision is FlowDecision.Default && finding.Flow != defaultFlow)
			{
				agrees = false;
				break;
			}
		}

		return agrees;
	}

	/// <summary>
	/// THE FLOW-SIGNAL COUNTERS ON THE READ PATH. Null is accepted only where
	/// <paramref name="analyzeCompleted"/> is false, on the same terms <see cref="ReadScanSummary"/>
	/// documents. Neither count is negative, no more evaluations time out than were run, and no more
	/// files are evaluated than the candidate set holds, since each candidate is evaluated at most once.
	/// </summary>
	private static SessionStateRejectionReason ReadFlowEvaluationDiagnostics(
		PersistedFlowEvaluationDiagnosticsDocument? persisted,
		int candidateCount,
		bool analyzeCompleted,
		out FlowEvaluationDiagnostics diagnostics)
	{
		SessionStateRejectionReason reason = SessionStateRejectionReason.None;

		diagnostics = FlowEvaluationDiagnostics.Empty;

		if (persisted is null)
		{
			reason = analyzeCompleted
				? SessionStateRejectionReason.FlowEvaluationDiagnosticsNotWellFormed
				: SessionStateRejectionReason.None;
		}
		else if (persisted.FilesEvaluated >= 0
			&& persisted.SignalTripCount >= 0
			&& persisted.SignalTripCount <= persisted.FilesEvaluated
			&& persisted.FilesEvaluated <= candidateCount)
		{
			diagnostics = new FlowEvaluationDiagnostics
			{
				FilesEvaluated = persisted.FilesEvaluated.Value,
				SignalTripCount = persisted.SignalTripCount.Value
			};
		}
		else
		{
			reason = SessionStateRejectionReason.FlowEvaluationDiagnosticsNotWellFormed;
		}

		return reason;
	}

	/// <summary>
	/// The two declared names, and nothing else. It is a closed pair rather than an enum parse for
	/// the reason every other vocabulary in this product is declared as data: a reflective parse is
	/// the ahead-of-time failure that compiles, publishes and breaks on the client's machine - and
	/// here it would additionally accept the integer forms, so a file carrying <c>"1"</c> would
	/// silently read as Established.
	/// </summary>
	private static bool TryReadDestinationCertainty(string? name, out Catalog.DestinationCertainty certainty)
	{
		bool recognized = true;

		if (string.Equals(name, DESTINATION_ESTABLISHED_NAME, StringComparison.Ordinal))
		{
			certainty = Catalog.DestinationCertainty.Established;
		}
		else if (string.Equals(name, DESTINATION_UNKNOWN_NAME, StringComparison.Ordinal))
		{
			certainty = Catalog.DestinationCertainty.Unknown;
		}
		else
		{
			certainty = Catalog.DestinationCertainty.Unknown;
			recognized = false;
		}

		return recognized;
	}

	private static SessionStateLoad BuildFreshLoad(
		SessionStateRejectionReason reason,
		CatalogVersion catalogVersion,
		string repositoryRootLabel) =>
		new()
		{
			State = SessionState.Fresh(catalogVersion, repositoryRootLabel),
			WasResumed = false,
			RejectionReason = reason
		};

	private string BuildStateFilePath(RepositoryFolderPath repository) =>
		Path.Combine(_destinationFolder.CanonicalPath, BuildStateFileName(repository));

	/// <summary>
	/// THE WRITE-SIDE CHECK: a write-only pass-through that counts the bytes it forwards and
	/// refuses the write that would take the total past the ceiling, before that write reaches the
	/// file. The serializer flushes in small chunks, so the refusal comes at the ceiling and not after
	/// a file of unbounded size has been produced.
	/// </summary>
	private sealed class CeilingEnforcingStream : Stream
	{
		private readonly Stream _inner;
		private readonly long _ceilingInBytes;

		private long _bytesWritten;

		internal CeilingEnforcingStream(Stream inner, long ceilingInBytes)
		{
			_inner = inner;
			_ceilingInBytes = ceilingInBytes;
		}

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => _bytesWritten;
			set => throw new NotSupportedException();
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			CountBytes(buffer.Length);
			_inner.Write(buffer);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Write(buffer.AsSpan(offset, count));
		}

		public override void Flush()
		{
			_inner.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		private void CountBytes(int count)
		{
			_bytesWritten += count;

			if (_bytesWritten > _ceilingInBytes)
			{
				throw new SessionStateTooLargeException(
					string.Format(CultureInfo.InvariantCulture, STATE_TOO_LARGE_MESSAGE_FORMAT, DescribeCeiling(_ceilingInBytes)));
			}
		}
	}

	/// <summary>The validated, non-null scalar fields of one persisted finding.</summary>
	private readonly record struct PersistedFindingScalars(
		int Line,
		int Column,
		bool LiteralCredentialSignal,
		int OccurrenceOrdinal);

	/// <summary>A finding's three flow fields as read: all three set on a High or Medium finding, none on a Low one.</summary>
	private readonly record struct PersistedFlowFields(
		FlowEvidence? Evidence,
		CredentialFlow? Flow,
		FlowDecision? Decision);

	/// <summary>A document and the reason there is none, exactly one of which is meaningful.</summary>
	private readonly record struct DocumentReadResult(
		SessionStateDocument? Document,
		SessionStateRejectionReason Reason);
}
