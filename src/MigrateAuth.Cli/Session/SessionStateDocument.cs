using System.Text.Json.Serialization;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// The persisted shape of the session state. Every member is nullable on purpose: the file is
/// untrusted input, so "absent" must be a value the validator can see rather than a
/// default that silently passes for a real one.
///
/// A member the schema does not declare is rejected rather than ignored. The configuration
/// read is the tolerant one; this is the hostile one, and a file carrying fields we do
/// not understand is a file we cannot claim to have validated.
///
/// THE FILE IS WRITTEN UNINDENTED AND READ BY STREAMING. At the volume the state is sized for
/// the indentation is bytes that only a human reader would use, and nobody reads this file.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record SessionStateDocument
{
	/// <summary>
	/// The version of this document's own shape. It is what lets a later version add the findings
	/// section without an older binary misreading a newer file: a document at another schema
	/// version starts a fresh session.
	/// </summary>
	public int? SchemaVersion { get; init; }

	/// <summary>Major component of the catalog version that produced the state.</summary>
	public int? CatalogVersionMajor { get; init; }

	/// <summary>Minor component of the catalog version that produced the state.</summary>
	public int? CatalogVersionMinor { get; init; }

	/// <summary>Revision component of the catalog version that produced the state.</summary>
	public int? CatalogVersionRevision { get; init; }

	/// <summary>
	/// The audited repository as a label. The file may not carry a traversable path,
	/// and a label is enough for a human to tell one checkout's state from another's.
	/// </summary>
	public string? RepositoryRootLabel { get; init; }

	/// <summary>The completed stages, written with the registry's own command names.</summary>
	public string[]? CompletedStages { get; init; }

	/// <summary>The candidate set, as paths relative to the repository root.</summary>
	public string[]? CandidateRelativePaths { get; init; }

	/// <summary>
	/// The third part of the state, which schema version 2 was declared to admit. Absent in a document this
	/// build did not write, which cannot arrive here anyway: a document at another schema version
	/// starts a fresh session before any of these are read.
	/// </summary>
	public PersistedFindingDocument[]? Findings { get; init; }

	/// <summary>
	/// Section 2's numbers, as Scan last left them. <c>Save</c> always
	/// writes this as an object once a session exists, empty or not - never as null on a document
	/// this build produced. It reads as null only in a document an earlier build wrote, or
	/// one an earlier session in this same run never got as far as Scan; the read path rejects null
	/// beside a completed Scan  rather than accept it as "the scan found nothing".
	/// </summary>
	public PersistedScanSummaryDocument? ScanSummary { get; init; }

	/// <summary>The Scan stage's own skipped-item tallies, all eleven categories.</summary>
	public PersistedSkippedItemTallyDocument[]? ScanSkippedItems { get; init; }

	/// <summary>The Analysis stage's own skipped-item tallies, all eleven categories.</summary>
	public PersistedSkippedItemTallyDocument[]? AnalysisSkippedItems { get; init; }

	/// <summary>
	/// The findings-cap truncation counter, as Analysis last left it. Null on the
	/// same terms as <see cref="ScanSummary"/>: <c>Save</c> always writes it once a session exists,
	/// and it reads as null only in a document this build did not write that way.
	/// </summary>
	public PersistedFindingsCapOutcomeDocument? FindingsCapOutcome { get; init; }

	/// <summary>
	/// The canonical names of the languages Scan's own last completed run
	/// had enabled - at most the eight <c>AssessedLanguages</c> names, no name repeated. Null on the
	/// same terms as <see cref="ScanSummary"/>: accepted only where the document's own completed
	/// <c>scan</c> is absent, because a real scan always snapshots the set it narrowed candidate
	/// selection with.
	/// </summary>
	public string[]? ScanEnabledLanguages { get; init; }

	/// <summary>
	/// SCHEMA VERSION 5. The language name Scan's first-line attribution gave each candidate,
	/// parallel to <see cref="CandidateRelativePaths"/> and the same length. Absent reads as empty, which
	/// is accepted only beside an empty candidate set. A parallel array rather than an object per
	/// candidate, because the candidate set is the one term that grows with the repository and a
	/// name per candidate costs a few bytes where an object would repeat a member name on each.
	/// </summary>
	public string[]? CandidateLanguages { get; init; }

	/// <summary>
	/// SCHEMA VERSION 5. The effective default flow Analysis applied, by its declared name. Null
	/// on the same terms as <see cref="ScanSummary"/>, and null together with
	/// <see cref="DefaultFlowOrigin"/> or not at all.
	/// </summary>
	public string? DefaultFlow { get; init; }

	/// <summary>SCHEMA VERSION 5. Where <see cref="DefaultFlow"/> came from: <c>BuiltInDefault</c> or <c>Profile</c>.</summary>
	public string? DefaultFlowOrigin { get; init; }

	/// <summary>
	/// SCHEMA VERSION 5. The flow-signal evaluation counters as Analysis last left
	/// them. Null on the same terms as <see cref="ScanSummary"/>.
	/// </summary>
	public PersistedFlowEvaluationDiagnosticsDocument? FlowEvaluationDiagnostics { get; init; }
}

/// <summary>The two flow-signal evaluation counters, as the state file carries them.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PersistedFlowEvaluationDiagnosticsDocument
{
	/// <summary>How many files flow-signal evaluation was run on.</summary>
	public int? FilesEvaluated { get; init; }

	/// <summary>How many of those evaluations timed out.</summary>
	public int? SignalTripCount { get; init; }
}

/// <summary>
/// Section 2's numbers as the state file carries them. The share percentage is
/// deliberately absent: it is derived from the three counts below by the same formula
/// <see cref="Scanning.LanguageInventory"/> uses, so persisting it as well would be a second copy of
/// a value that must always agree with the first - and on a file an attacker can edit, a second copy
/// is a second thing to validate for consistency rather than extra protection.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PersistedScanSummaryDocument
{
	/// <summary>Every file the inventory pass reached, read or skipped.</summary>
	public int? FileCount { get; init; }

	/// <summary>Every line counted, across all languages.</summary>
	public long? LineCount { get; init; }

	/// <summary>The candidate file count, distinct from the total file count.</summary>
	public int? CandidateFileCount { get; init; }

	/// <summary>One row per language the inventory attributed a file to.</summary>
	public PersistedLanguageInventoryRowDocument[]? Rows { get; init; }

	/// <summary>
	/// SCHEMA VERSION 5. How many files were attributed by their extension because first-line
	/// detection tripped the lexical budget. Absent reads as zero, and it never exceeds
	/// <see cref="FileCount"/>: a fallback is a counted file.
	/// </summary>
	public int? FirstLineFallbackCount { get; init; }

	/// <summary>
	/// SCHEMA VERSION 6. How many folders the walk met and did not enter. Absent reads as zero, and the
	/// listed paths never outnumber it: a listed folder is a counted one.
	/// </summary>
	public int? ExcludedFolderCount { get; init; }

	/// <summary>
	/// SCHEMA VERSION 6. At most the reading ceiling of those folders, relative to the repository root
	/// and re-checked for containment on read, on the same terms as a skipped item's paths.
	/// </summary>
	public string[]? ExcludedFolderPaths { get; init; }

	/// <summary>
	/// SCHEMA VERSION 8. How many files the decode ladder read as Windows-1252. Absent reads as zero,
	/// it never exceeds <see cref="FileCount"/>, and the listed paths never outnumber it: a listed
	/// file is a counted one.
	/// </summary>
	public int? LegacyEncodedFileCount { get; init; }

	/// <summary>
	/// SCHEMA VERSION 8. At most the reading ceiling of those files, relative to the repository root
	/// and re-checked for containment on read, on the same terms as a skipped item's paths.
	/// </summary>
	public string[]? LegacyEncodedFilePaths { get; init; }
}

/// <summary>One language's row of <see cref="PersistedScanSummaryDocument.Rows"/>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PersistedLanguageInventoryRowDocument
{
	/// <summary>The detected language, or the unrecognized bucket the inventory requires.</summary>
	public string? LanguageName { get; init; }

	/// <summary>Every file attributed to this language, including the ones that could not be read.</summary>
	public int? FileCount { get; init; }

	/// <summary>Lines, counted after normalization.</summary>
	public long? LineCount { get; init; }

	/// <summary>How many of those files are candidates.</summary>
	public int? CandidateFileCount { get; init; }
}

/// <summary>
/// One skipped-item category's tally, keyed by <see cref="Reading.SkippedItemCategoryNames"/>'s
/// declared identifier rather than by the enum's numeric value or its member name - see that type
/// for why.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PersistedSkippedItemTallyDocument
{
	/// <summary>The declared name of one of the eleven categories.</summary>
	public string? Category { get; init; }

	/// <summary>The full count. Never truncated.</summary>
	public int? Count { get; init; }

	/// <summary>At most a hundred, in the order they were observed.</summary>
	public string[]? EnumeratedPaths { get; init; }
}

/// <summary>The findings-cap truncation counter, flattened to the persisted shape.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PersistedFindingsCapOutcomeDocument
{
	/// <summary>Every construct recognition produced, before either cap removed anything.</summary>
	public int? TotalRecognizedCount { get; init; }

	/// <summary>The findings that survived both caps.</summary>
	public int? RetainedCount { get; init; }

	/// <summary>How many distinct files, on their own, offered more than the per-file cap allows.</summary>
	public int? PerFileCapTrippedFileCount { get; init; }

	/// <summary>How many findings were omitted specifically because their own file exceeded the per-file cap.</summary>
	public int? PerFileOmittedCount { get; init; }

	/// <summary>Whether the run as a whole produced more retainable findings than the global cap allows.</summary>
	public bool? GlobalCapTripped { get; init; }

	/// <summary>How many findings were omitted specifically because the run's global budget was spent.</summary>
	public int? GlobalOmittedCount { get; init; }
}

/// <summary>
/// ONE FINDING, AS THE STATE FILE CARRIES IT. It is a separate shape from
/// <see cref="Findings.Finding"/> rather than that type serialized, and every difference between
/// the two is deliberate.
///
/// EVERY MEMBER IS NULLABLE FOR THE REASON THE DOCUMENT ABOVE GIVES: the file is untrusted input,
/// so "absent" must be a value the validator can see rather than a default that passes for a real
/// one. A <see cref="Findings.Finding"/> declares every member <c>required</c>, which is the
/// opposite discipline and the right one for a value the engine built.
///
/// IT CARRIES NO DERIVED VALUE. The identifier is not here because
/// <see cref="Findings.FindingIdentifiers.Of"/> renders it from the index; the fingerprint's path
/// and pattern class are not here because they are the finding's own path and pattern class by
/// construction; the order key is not here because every one of its terms already is. A second
/// copy of a derived value is the copy that disagrees, and on a file an attacker can edit it is
/// also a second thing to validate for consistency with the first.
///
/// AND IT CARRIES NOTHING FROM THE ANALYZED FILE. Every member is either a number, a closed
/// vocabulary name, a catalog identifier, or the relative path, the one path form allowed - which is
/// exactly the restriction <see cref="Findings.Finding"/> already holds, so persisting it is a
/// mechanical step rather than a filtering one.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record PersistedFindingDocument
{
	/// <summary>The one-based index, which the identifier is rendered from.</summary>
	public int? SequentialIndex { get; init; }

	/// <summary>The path relative to the repository root, re-checked for containment on read.</summary>
	public string? RelativePath { get; init; }

	/// <summary>The one-based line of the construct's anchor.</summary>
	public int? Line { get; init; }

	/// <summary>The one-based column of that anchor.</summary>
	public int? Column { get; init; }

	/// <summary>The canonical language name, as <c>AssessedLanguages</c> declares it.</summary>
	public string? Language { get; init; }

	/// <summary>The catalog pattern class identifier, re-checked for shape on read.</summary>
	public string? PatternClass { get; init; }

	/// <summary>The level the rubric assigned, as <c>ConfidenceLevels</c> names it.</summary>
	public string? ConfidenceLevel { get; init; }

	/// <summary>Whether the destination was established, as the two declared names below.</summary>
	public string? DestinationCertainty { get; init; }

	/// <summary>The literal-credential signal.</summary>
	public bool? LiteralCredentialSignal { get; init; }

	/// <summary>The third fingerprint component: which occurrence of this class in this file.</summary>
	public int? OccurrenceOrdinal { get; init; }

	/// <summary>
	/// SCHEMA VERSION 5. What the finding's file established about the flow, by its declared
	/// name. Present on a High or Medium finding and null on a Low one, as on the finding itself.
	/// </summary>
	public string? FlowEvidence { get; init; }

	/// <summary>SCHEMA VERSION 5. The flow Analysis assigned, by its declared name. Null exactly when <see cref="FlowEvidence"/> is.</summary>
	public string? Flow { get; init; }

	/// <summary>
	/// SCHEMA VERSION 5. Why <see cref="Flow"/> was assigned, by its declared name. Null exactly
	/// when <see cref="FlowEvidence"/> is. The note the report prints is derived from the decision and
	/// the flow and is never persisted.
	/// </summary>
	public string? FlowDecision { get; init; }

	/// <summary>
	/// SCHEMA VERSION 6. Why a Low finding is Low, as the closed values <c>LowReasonVocabulary</c>
	/// declares. Absent on a finding that is not Low and on a Low finding whose reason was not
	/// recorded; a value outside the vocabulary refuses the document.
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? LowReasons { get; init; }
}

/// <summary>
/// Serialization is generated at build time with an explicitly declared context. Under
/// ahead-of-time compilation a reflection-based serializer has the worst failure mode
/// available - it compiles, it publishes, and it fails on the client's machine.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SessionStateDocument))]
[JsonSerializable(typeof(PersistedFindingDocument[]))]
[JsonSerializable(typeof(PersistedSkippedItemTallyDocument[]))]
[JsonSerializable(typeof(PersistedLanguageInventoryRowDocument[]))]
internal sealed partial class SessionStateJsonContext : JsonSerializerContext
{
}
