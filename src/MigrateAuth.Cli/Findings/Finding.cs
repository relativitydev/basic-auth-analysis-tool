using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// THE FINDING. THE WHOLE OF WHAT THE PRODUCT KNOWS ABOUT ONE RECOGNIZED CONSTRUCT.
///
/// The fields of a finding: identifier, relative path, line, column, language, pattern class,
/// confidence level, recommendation template reference, the content-free fingerprint, the
/// literal-credential signal, the parallel numeric index and the destination certainty.
///
/// NEVER THE MATCHED TEXT. Nothing here originates in the file's
/// content except the relative path, which is the data invariant's single exemption. That is the
/// same rule governing <see cref="EmittedFact"/>, and this type obeys it although it does NOT
/// travel through <see cref="EmissionPoint"/>: the chokepoint governs what leaves the process,
/// and this is a value in memory that is serialized later. Obeying the invariant here is what
/// makes that serialization a mechanical step rather than a filtering one.
///
/// THERE IS NO RECOMMENDATION FIELD, AND THAT IS THE REFERENCE RATHER THAN ITS ABSENCE. The finding contract
/// lists a "recommendation template reference" and <see cref="PatternClass"/> IS it: the template
/// catalog is indexed by pattern class, so a separate field would hold a second copy of
/// a value already present and give the two somewhere to disagree. A RENDERED recommendation is
/// deliberately not here either - it is produced at write time, which keeps the set in memory
/// small however many findings it holds.
///
/// TWO IDENTITIES, BECAUSE THE FINDING CARRIES TWO. <see cref="Identifier"/> and
/// <see cref="SequentialIndex"/> are the sequential number, for human reading and reference
/// WITHIN THE RUN; <see cref="Fingerprint"/> is the content-free identity that survives ACROSS
/// runs. They answer different questions and neither substitutes for the other.
///
/// IT IS A VALUE TYPE. The accumulation of findings is sized for the largest repositories: as a
/// reference record the set is that many separately allocated objects, each with its own header,
/// traced individually by every collection; as a value record an
/// <see cref="System.Collections.Immutable.ImmutableArray{T}"/> of them is ONE contiguous
/// allocation carrying no headers at all. Nothing here argues for reference semantics in
/// exchange - the type is immutable, its equality is structural, and it is never held by identity.
/// </summary>
public readonly record struct Finding
{
	/// <summary>
	/// The sequential identifier, zero-padded to at least three digits. Assigned over
	/// the FULL ordered set, before the threshold filter removes anything - so a filtered
	/// artifact shows gaps, which is the lesser cost against an identifier that would
	/// otherwise be a function of the reader's configuration.
	/// </summary>
	public required string Identifier { get; init; }

	/// <summary>
	/// The same number unformatted, one-based. The data artifact requires it because
	/// sorting on <see cref="Identifier"/> is lexicographic and places finding 1000 before 999.
	/// </summary>
	public required int SequentialIndex { get; init; }

	/// <summary>Where the construct sits, relative to the repository root. The only path form permitted.</summary>
	public required RepositoryRelativePath Path { get; init; }

	/// <summary>The one-based line of the construct's ANCHOR, never of the start of its match.</summary>
	public required int Line { get; init; }

	/// <summary>
	/// The one-based column of that anchor. It is published rather than confining it to the
	/// sort key, so that two matches of one class on one line are distinguishable in the artifact
	/// and so that the ordering which generated the identifiers is reconstructible from it.
	/// </summary>
	public required int Column { get; init; }

	/// <summary>
	/// The language of the file the construct sits in.
	/// </summary>
	public required AssessedLanguage Language { get; init; }

	/// <summary>
	/// The catalog pattern class, as its identifier. Also the recommendation template reference -
	/// The catalog is indexed by exactly this value.
	/// </summary>
	public required RuleIdentifier PatternClass { get; init; }

	/// <summary>
	/// The one level the confidence rubric assigns, composed by minimum over its candidates during recognition and
	/// carried here unchanged. It is never recomposed: a second composition over the candidates is
	/// precisely the accumulating decrement the minimum rule exists to forbid.
	/// </summary>
	public required ConfidenceLevel Level { get; init; }

	/// <summary>
	/// Established or unknown, read from the rule element the catalog declares it on. NEVER
	/// computed here - the engine cannot determine the host a request reaches, and a value derived
	/// at this distance from the construct would be asserting knowledge nobody has. The report
	/// groups by it rather than footnoting it, because two findings that look identical are not
	/// the same work.
	/// </summary>
	public required DestinationCertainty DestinationCertainty { get; init; }

	/// <summary>
	/// The literal-credential signal, established during recognition from the ZONE of the
	/// credential argument and never from its value. It does not change
	/// <see cref="Level"/> and it triggers the additional security recommendation.
	/// </summary>
	public required bool LiteralCredentialSignal { get; init; }

	/// <summary>The content-free identity, which survives between runs as the number does not.</summary>
	public required FindingFingerprint Fingerprint { get; init; }

	/// <summary>
	/// Why a Low finding is Low: which of the confidence rubric's signals proposed the level. Carried beside
	/// <see cref="Level"/>, never read to compute it, and <see cref="LowConfidenceReasons.None"/> on a
	/// finding that is not Low.
	/// </summary>
	public LowConfidenceReasons LowReasons { get; init; }

	/// <summary>
	/// What the finding's file established about the credential flow: null on a Low finding,
	/// which carries no flow evidence, and the file's one value on a High or Medium finding.
	/// </summary>
	public FlowEvidence? FlowEvidence { get; init; }

	/// <summary>
	/// The OAuth 2.0 flow this finding's recommendation targets, assigned by
	/// <see cref="FlowAssignment"/> after composition. Null on a Low finding, and null until the
	/// assignment step has run.
	/// </summary>
	public CredentialFlow? Flow { get; init; }

	/// <summary>Why <see cref="Flow"/> was assigned. Null exactly when <see cref="Flow"/> is.</summary>
	public FlowDecision? FlowDecision { get; init; }

	/// <summary>
	/// The total ordering key for this finding, derived rather than stored.
	///
	/// It is a property and not a field because every term is already present: storing it would be
	/// a second copy of five values, and the copy would be the one a later edit forgot to update.
	/// The path term comes from the fingerprint, which is the same normalization by construction -
	/// see <see cref="NormalizedPathKey"/> on why there is only one.
	/// </summary>
	public FindingOrderKey OrderKey =>
		new()
		{
			Path = Fingerprint.Path,
			Line = Line,
			Column = Column,
			PatternClassIdentifier = PatternClass.Value,
			RawPath = Path.Value
		};
}
