using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// One recognized construct, classified. It carries what ordering and numbering need: which class
/// matched, where its anchor is, which level it received, and whether its credential was written into
/// the source. Identity, fingerprint, global ordinal, line and column are built by a later stage over
/// the full ordered set.
///
/// The literal-credential signal is computed here because it is read off the zone of a named capture
/// group of a match that exists only while this file is being recognized.
///
/// The level is already composed by minimum over the candidate levels and is final; later stages,
/// including the confidence threshold filter, read it and never recompute it.
/// </summary>
public readonly record struct RecognizedConstruct
{
	/// <summary>The catalog pattern class that recognized it. The last key of the total finding order.</summary>
	public required string PatternClassIdentifier { get; init; }

	/// <summary>
	/// The absolute offset, in UTF-16 code units of the normalized text, of the position whose zone
	/// decided classification. It is the anchor and never the start of the match: the mask is tested at
	/// that position, so it is also the position a reader should be sent to.
	/// </summary>
	public required int AnchorOffset { get; init; }

	/// <summary>The one confidence level assigned, already composed by minimum over its candidates.</summary>
	public required ConfidenceLevel Level { get; init; }

	/// <summary>
	/// The literal-credential signal. True where the credential argument this rule declares
	/// lies in a <see cref="LexicalZone.StringBody"/> zone, meaning the credential is embedded in
	/// source rather than supplied at run time.
	///
	/// It is computed from the zone and never from the value: the finding says a secret sits at a
	/// location, never what it is. It is false, never an error, where the rule declares no credential
	/// position at all.
	///
	/// It does not change the level; it travels beside it and is read by the additional security
	/// recommendation.
	/// </summary>
	public required bool LiteralCredentialSignal { get; init; }

	/// <summary>
	/// Which of the Low signals proposed Low, so that a report can say why a Low finding is Low. It
	/// travels beside <see cref="Level"/> and is never read to compute one; it is
	/// <see cref="LowConfidenceReasons.None"/> whenever the level is not Low.
	/// </summary>
	public LowConfidenceReasons LowReasons { get; init; }
}
