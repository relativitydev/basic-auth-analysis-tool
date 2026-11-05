using System.Text.Json.Serialization;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// One entry of a language's declared-unsupported coverage, as the artifact
/// carries it - a sibling of the finding record's own fields, not one of them: this describes the
/// catalog's own declared scope, not any one finding.
/// </summary>
public sealed record ArtifactDeclaredUnsupportedConstructDocument
{
	/// <summary>The construct's generic description.</summary>
	public string Construct { get; init; } = string.Empty;

	/// <summary>Why this catalog does not attempt to recognize it.</summary>
	public string Reason { get; init; } = string.Empty;
}

/// <summary>One language's coverage row, as the artifact carries it.</summary>
public sealed record ArtifactCoverageRowDocument
{
	/// <summary>The canonical language name. One of the eight, always present.</summary>
	public string Language { get; init; } = string.Empty;

	/// <summary>The row text: supported with a count, declared unsupported, or disabled.</summary>
	public string Coverage { get; init; } = string.Empty;

	/// <summary>How many pattern classes were searched for. Zero where nothing ran.</summary>
	public int PatternClassesSearched { get; init; }

	/// <summary>
	/// What this language's analyzer declares it does not attempt to recognize,
	/// sorted ordinally by <see cref="ArtifactDeclaredUnsupportedConstructDocument.Construct"/>.
	/// Empty for a language with no analyzer, exactly as
	/// <see cref="PatternClassesSearched"/> is zero for the same reason.
	/// </summary>
	public ArtifactDeclaredUnsupportedConstructDocument[] DeclaredUnsupportedConstructs { get; init; } = [];
}

/// <summary>
/// Section 1 of the data artifact as a structured field. The catalog's declared scope defines the
/// recall denominator, so a grader that cannot see this section cannot compute one.
/// </summary>
public sealed record ArtifactCoverageDocument
{
	/// <summary>The statement that the assessment is not a guarantee of absence.</summary>
	public string Statement { get; init; } = string.Empty;

	/// <summary>All eight, in the enumeration's declared order.</summary>
	public ArtifactCoverageRowDocument[] Languages { get; init; } = [];
}

/// <summary>One language's inventory row.</summary>
public sealed record ArtifactInventoryRowDocument
{
	/// <summary>The detected language, or the unrecognized bucket.</summary>
	public string Language { get; init; } = string.Empty;

	/// <summary>Files attributed to it, including the ones that could not be read.</summary>
	public int Files { get; init; }

	/// <summary>Lines counted after normalization.</summary>
	public long Lines { get; init; }

	/// <summary>How many of those files are candidates.</summary>
	public int CandidateFiles { get; init; }

	/// <summary>Share of the enumerated FILE count, which is the stated denominator.</summary>
	public double ShareOfFilesPercentage { get; init; }

	/// <summary>
	/// Nullable. Three values, matching
	/// <see cref="LanguageAssessmentState"/>: <c>true</c> - an analyzer covered this language;
	/// <c>false</c> - a detected language this release does not assess, or one this session's
	/// configuration disabled (see <see cref="NotAssessedReason"/> for which); <c>null</c> -
	/// no language was detected (the unrecognized bucket, which is not one of the eight
	/// `AssessedLanguage` names and was never searched at all). The field is always present, even
	/// when its value is null - a nullable field a caller omits and a nullable field a caller sets to
	/// null are not the same JSON, and only the second one is what this row means.
	/// </summary>
	public bool? Assessed { get; init; }

	/// <summary>
	/// Why <see cref="Assessed"/> is <c>false</c>, as a closed vocabulary value:
	/// <c>"noAnalyzer"</c> - this release ships no analyzer for the language; <c>"disabledByConfiguration"</c> -
	/// this session's configuration disabled it, regardless of whether an analyzer exists. Null when
	/// <see cref="Assessed"/> is <c>true</c> or <c>null</c> - there is no reason to carry for a row
	/// that was assessed or was never searched at all.
	/// </summary>
	public string? NotAssessedReason { get; init; }
}

/// <summary>Section 2 of the data artifact as a structured field.</summary>
public sealed record ArtifactScanSummaryDocument
{
	/// <summary>Every file the inventory pass reached.</summary>
	public int FilesEnumerated { get; init; }

	/// <summary>Every line counted, across all languages.</summary>
	public long LinesCounted { get; init; }

	/// <summary>The candidate count, distinct from the file count.</summary>
	public int CandidateFiles { get; init; }

	/// <summary>One row per language the inventory attributed a file to.</summary>
	public ArtifactInventoryRowDocument[] Languages { get; init; } = [];

	/// <summary>
	/// How many files were attributed by their extension because first-line
	/// detection exceeded the lexical budget. Zero in a release that declares no first-line expression.
	/// </summary>
	public int FirstLineFallbackFiles { get; init; }

	/// <summary>
	/// The folders the walk did not enter, by the tool's own policy. Kept apart from
	/// <c>skippedItems</c>: an excluded folder is a choice of the tool, not a hole it could not avoid.
	/// </summary>
	public ArtifactExcludedFoldersDocument ExcludedFolders { get; init; } = new();

	/// <summary>
	/// True when the enabled languages of the scan this report describes differ from the session's
	/// current enabled languages, which is when the Markdown report and the console say to re-run
	/// 'scan'. The rows above then describe the earlier scan. Only the enabled languages are
	/// compared; other settings changed since the scan are not detected. False otherwise.
	/// </summary>
	public bool EnabledLanguagesDifferFromSession { get; init; }
}

/// <summary>
/// The folders the walk met and did not enter: the count is whole and the path list is bounded, and
/// the two are separate fields so that a consumer cannot mistake the second for the first.
/// </summary>
public sealed record ArtifactExcludedFoldersDocument
{
	/// <summary>How many excluded folders the walk met. Never truncated.</summary>
	public int Count { get; init; }

	/// <summary>How many are counted above and not listed below.</summary>
	public int OmittedPathCount { get; init; }

	/// <summary>At most a hundred, relative to the repository and ordered ordinally.</summary>
	public string[] Paths { get; init; } = [];
}

/// <summary>
/// Section 3 of the data artifact, one of the eleven skipped-item categories. The count is whole and
/// the path list is bounded, and the two are separate fields so that a consumer cannot mistake the
/// second for the first.
/// </summary>
public sealed record ArtifactSkippedItemDocument
{
	/// <summary>
	/// Which of the eleven, as a stable identifier. Never rewritten for readability - see
	/// <see cref="Description"/> for the client-facing text.
	/// </summary>
	public string Category { get; init; } = string.Empty;

	/// <summary>
	/// What the category means for a client reading the artifact, from
	/// <see cref="ReportVocabulary.DescriptionOf(Reading.SkippedItemCategory)"/> - the same text the
	/// Markdown Section 3 table renders for this category.
	/// </summary>
	public string Description { get; init; } = string.Empty;

	/// <summary>Files for ten of them, directories for one. Never added together.</summary>
	public string Unit { get; init; } = string.Empty;

	/// <summary>The full count. Never truncated.</summary>
	public int Count { get; init; }

	/// <summary>How many are counted above and not listed below.</summary>
	public int OmittedPathCount { get; init; }

	/// <summary>At most a hundred, in the order they were observed.</summary>
	public string[] Paths { get; init; } = [];
}

/// <summary>
/// The truncation counter as a structured field. A consumer that reads
/// <see cref="ArtifactDocument.Findings"/> without this field cannot tell a complete result from a
/// capped one: a truncated result is declared truncated, never presented as complete.
/// </summary>
public sealed record ArtifactFindingsCapDocument
{
	/// <summary>Whether either cap removed anything. The one field a consumer checks first.</summary>
	public bool Truncated { get; init; }

	/// <summary>Every construct recognition produced, before either cap removed anything.</summary>
	public int TotalRecognized { get; init; }

	/// <summary>The findings that survived both caps. Equal to the length of the findings array.</summary>
	public int Retained { get; init; }

	/// <summary>How many distinct files, on their own, offered more than the per-file cap allows.</summary>
	public int PerFileCapTrippedFileCount { get; init; }

	/// <summary>How many findings were omitted specifically because their own file exceeded the per-file cap.</summary>
	public int PerFileOmittedCount { get; init; }

	/// <summary>Whether the run as a whole produced more retainable findings than the global cap allows.</summary>
	public bool GlobalCapTripped { get; init; }

	/// <summary>How many findings were omitted specifically because the run's global budget was spent.</summary>
	public int GlobalOmittedCount { get; init; }
}

/// <summary>
/// One finding, with the seven finding-record fields (`id`, `filePath`, `line`, `language`,
/// `patternClass`, `confidence`, `recommendation`), plus the declared extensions and the
/// envelope's own `securityNote`.
///
/// Never the matched text, because a secret's value is never reported. Nothing here originates in the
/// analyzed file except the relative path.
/// </summary>
public sealed record ArtifactFindingDocument
{
	/// <summary>
	/// The finding record's `id`: the zero-padded sequential identifier, assigned before any filter.
	/// </summary>
	public string Id { get; init; } = string.Empty;

	/// <summary>
	/// A declared extension: the same number unformatted, so that sorting is numeric rather
	/// than lexicographic. Not part of the finding record.
	/// </summary>
	public int SequentialIndex { get; init; }

	/// <summary>The finding record's `filePath`: the path relative to the repository root, the only form permitted.</summary>
	public string FilePath { get; init; } = string.Empty;

	/// <summary>The finding record's `line`: the one-based line of the construct's anchor.</summary>
	public int Line { get; init; }

	/// <summary>A declared extension: the one-based column of that anchor. Not part of the finding record.</summary>
	public int Column { get; init; }

	/// <summary>The finding record's `language`: the canonical name, without which recall is unmeasurable from here.</summary>
	public string Language { get; init; } = string.Empty;

	/// <summary>
	/// The finding record's `patternClass`, carried as its identifier rather than its display name, so
	/// that it stays stable for consumers that key on it.
	/// </summary>
	public string PatternClass { get; init; } = string.Empty;

	/// <summary>The finding record's `confidence`: the level the rubric assigned. Never a function of the threshold.</summary>
	public string Confidence { get; init; } = string.Empty;

	/// <summary>A declared extension: established or unknown. Read from the rule, never computed.</summary>
	public string DestinationCertainty { get; init; } = string.Empty;

	/// <summary>A declared extension: the literal-credential signal.</summary>
	public bool LiteralCredentialSignal { get; init; }

	/// <summary>A declared extension: the finding's content-free fingerprint.</summary>
	public string Fingerprint { get; init; } = string.Empty;

	/// <summary>
	/// The finding record's `recommendation`: the same text <see cref="MarkdownReportRenderer"/> renders
	/// for this finding, rendered at write time and held by nobody. Non-null for High and Medium -
	/// the four template pieces in order for the template route, the generic narrative for
	/// the generic route. Null for Low: a Low finding's null is the manual-review signal, and there is no
	/// separate flag for it.
	/// </summary>
	public string? Recommendation { get; init; }

	/// <summary>
	/// The security note, promoted to its own field so that `recommendation` carries only the migration
	/// advice. Null when the literal-credential signal did not fire, at every confidence level including
	/// Low.
	/// </summary>
	public string? SecurityNote { get; init; }

	/// <summary>
	/// A declared extension: the OAuth 2.0 flow the recommendation targets,
	/// <c>ClientCredentials</c> or <c>AuthorizationCode</c>. Null for a Low finding, which stays in
	/// manual review with no flow. Flat on the finding, beside the seven finding record fields.
	/// </summary>
	public string? Flow { get; init; }

	/// <summary>
	/// A declared extension: why <see cref="Flow"/> was assigned - <c>InteractiveSignal</c>,
	/// <c>FixedSignal</c> or <c>Default</c>. NULL exactly when <see cref="Flow"/> is.
	/// </summary>
	public string? FlowDecision { get; init; }

	/// <summary>
	/// A declared extension: the note for this finding's decision and flow, derived at render time
	/// by <see cref="FlowNotes.NoteFor"/> and carried only here, never inside
	/// <see cref="Recommendation"/>. NULL exactly when <see cref="Flow"/> is.
	/// </summary>
	public string? FlowNote { get; init; }

	/// <summary>
	/// A declared extension: why a Low finding is Low, as values of a closed vocabulary -
	/// <c>patternClass</c>, <c>commentOrStringLiteral</c>, <c>demotedFile</c> and <c>demotedVendoredFile</c>, in that order, more
	/// than one when several apply. NULL for a finding that is not Low; empty for a Low finding whose
	/// reason was not recorded.
	/// </summary>
	public string[]? LowReasons { get; init; }
}

/// <summary>One of the two configuration file locations and what was found there.</summary>
public sealed record ArtifactConfigurationFileDocument
{
	/// <summary>Repository root or user profile.</summary>
	public string Location { get; init; } = string.Empty;

	/// <summary>Absent, applied, malformed or unreadable.</summary>
	public string Condition { get; init; } = string.Empty;
}

/// <summary>A field of the repository's own configuration file that had no effect.</summary>
public sealed record ArtifactIgnoredConfigurationFieldDocument
{
	/// <summary>Which of the configuration fields: the output path or the default flow.</summary>
	public string Field { get; init; } = string.Empty;

	/// <summary>Why it had no effect: the repository's file is never read for it.</summary>
	public string Reason { get; init; } = string.Empty;

	/// <summary>What it asked for, in this product's own vocabulary. Empty for the output path and the default flow.</summary>
	public string WouldHaveBeen { get; init; } = string.Empty;
}

/// <summary>A change of the confidence threshold that the repository's own configuration file made.</summary>
public sealed record ArtifactThresholdChangeDocument
{
	/// <summary>The level that would have applied without the repository's file.</summary>
	public string Previous { get; init; } = string.Empty;

	/// <summary>The level the repository's file set, which is the one in effect.</summary>
	public string Current { get; init; } = string.Empty;

	/// <summary><c>Raised</c> when the change hides findings, <c>Lowered</c> when it shows more.</summary>
	public string Direction { get; init; } = string.Empty;

	/// <summary>
	/// How many findings a raise hides that the previous level would have shown: those between the two
	/// levels, counted over the same set the <c>findings</c> array is filtered from. Zero when the
	/// direction is <c>Lowered</c>.
	/// </summary>
	public int HiddenFindingCount { get; init; }
}

/// <summary>
/// What the repository's own configuration file changed, against what would have applied without
/// it. Every list holds canonical language names and is empty in the ordinary case.
/// </summary>
public sealed record ArtifactRepositoryConfigurationEffectDocument
{
	/// <summary>The languages the repository's file named, when it named any.</summary>
	public string[] LanguagesSetByRepository { get; init; } = [];

	/// <summary>Languages that would have been enabled without the repository's file, and are not.</summary>
	public string[] LanguagesDisabledByRepository { get; init; } = [];

	/// <summary>Languages the repository's file enabled that would not otherwise have been.</summary>
	public string[] LanguagesEnabledByRepository { get; init; } = [];

	/// <summary>The threshold change the repository's file made. Null when it made none.</summary>
	public ArtifactThresholdChangeDocument? ThresholdChange { get; init; }
}

/// <summary>
/// Section 5 of the data artifact as a structured field. Without the catalog version two reports are
/// not comparable.
/// </summary>
public sealed record ArtifactProvenanceDocument
{
	/// <summary>
	/// THE LOCAL CALENDAR DATE OF THE RUN -
	/// never converted to UTC or any other zone - in the same <c>yyyy-MM-dd</c> invariant form the
	/// file name already carries it in, and the same date for both: see
	/// <see cref="ReportContent.RunDate"/> for why it is read once and shared by both artifacts.
	/// </summary>
	public string RunDate { get; init; } = string.Empty;

	/// <summary>The product version, from the declared constant.</summary>
	public string ToolVersion { get; init; } = string.Empty;

	/// <summary>The catalog version, numbered independently of the product.</summary>
	public string CatalogVersion { get; init; } = string.Empty;

	/// <summary>The audited repository as a label. Never a path.</summary>
	public string RepositoryLabel { get; init; } = string.Empty;

	/// <summary>The canonical names of the languages this session assessed.</summary>
	public string[] EnabledLanguages { get; init; } = [];

	/// <summary>The threshold in effect, which decided what appears in the findings field.</summary>
	public string MinimumConfidenceLevel { get; init; } = string.Empty;

	/// <summary>Where the threshold came from.</summary>
	public string MinimumConfidenceLevelOrigin { get; init; } = string.Empty;

	/// <summary>Where the set of enabled languages came from.</summary>
	public string EnabledLanguagesOrigin { get; init; } = string.Empty;

	/// <summary>Where the output destination came from.</summary>
	public string OutputPathOrigin { get; init; } = string.Empty;

	/// <summary>
	/// The flow a finding is assigned when no flow signal decided it: <c>ClientCredentials</c> or
	/// <c>AuthorizationCode</c>. It decided every finding whose <c>flowDecision</c> is <c>Default</c>.
	/// </summary>
	public string DefaultFlow { get; init; } = string.Empty;

	/// <summary>
	/// Where <see cref="DefaultFlow"/> came from: <c>BuiltInDefault</c> or <c>Profile</c>.
	/// The repository's own file is never a source.
	/// </summary>
	public string DefaultFlowOrigin { get; init; } = string.Empty;

	/// <summary>The condition of each of the two configuration files, including the absent ones.</summary>
	public ArtifactConfigurationFileDocument[] ConfigurationFiles { get; init; } = [];

	/// <summary>What the repository's file asked for and did not get. Empty is the ordinary case.</summary>
	public ArtifactIgnoredConfigurationFieldDocument[] IgnoredRepositoryFields { get; init; } = [];

	/// <summary>What the repository's file changed, narrowing included. Empty lists and no threshold change is the ordinary case.</summary>
	public ArtifactRepositoryConfigurationEffectDocument RepositoryConfigurationEffect { get; init; } = new();
}

/// <summary>
/// The data artifact as one document: the handling notice and its five sections.
///
/// It is the read shape, and the write path does not use it. The artifact is written field by
/// field with a <see cref="System.Text.Json.Utf8JsonWriter"/> and the findings array element by
/// element, so that the finding set never exists as one object graph in memory.
///
/// It is the schema the artifact claims to have: a consumer - and the test that round-trips a written
/// artifact back through the same context - reads one of these, so the property names the writer
/// emits by hand are checked against a declared shape.
/// </summary>
public sealed record ArtifactDocument
{
	/// <summary>The handling notice, as a field its own serializer places.</summary>
	public string HandlingNotice { get; init; } = string.Empty;

	/// <summary>Section 1.</summary>
	public ArtifactCoverageDocument Coverage { get; init; } = new();

	/// <summary>Section 2.</summary>
	public ArtifactScanSummaryDocument ScanSummary { get; init; } = new();

	/// <summary>Section 3. All eleven categories, always.</summary>
	public ArtifactSkippedItemDocument[] SkippedItems { get; init; } = [];

	/// <summary>
	/// The truncation counter over <see cref="Findings"/>. Carried as its own field rather than
	/// folded into Section 4, because it describes findings that are not in the array below and a
	/// consumer reading only the array has no other way to learn they ever existed.
	/// </summary>
	public ArtifactFindingsCapDocument FindingsCap { get; init; } = new();

	/// <summary>
	/// Section 4, in the total finding order rather than grouped. The report groups by destination
	/// certainty; the artifact carries the field on every finding so a consumer can make the same split.
	/// </summary>
	public ArtifactFindingDocument[] Findings { get; init; } = [];

	/// <summary>Section 5.</summary>
	public ArtifactProvenanceDocument Provenance { get; init; } = new();
}

/// <summary>
/// Serialization is generated at build time with an explicitly declared context, never by
/// reflection. Under ahead-of-time compilation a reflection-based serializer compiles and publishes,
/// and then fails at run time on the client's machine at the last stage of the flow.
///
/// Every type serialized directly by the writer is registered here by name, so a document type
/// added without a registration fails to compile at its call site rather than at run time on a
/// client's repository.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ArtifactDocument))]
[JsonSerializable(typeof(ArtifactCoverageDocument))]
[JsonSerializable(typeof(ArtifactScanSummaryDocument))]
[JsonSerializable(typeof(ArtifactSkippedItemDocument))]
[JsonSerializable(typeof(ArtifactFindingsCapDocument))]
[JsonSerializable(typeof(ArtifactFindingDocument))]
[JsonSerializable(typeof(ArtifactProvenanceDocument))]
public sealed partial class ReportJsonContext : JsonSerializerContext
{
}
