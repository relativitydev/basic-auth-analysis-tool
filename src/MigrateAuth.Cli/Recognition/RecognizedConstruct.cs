using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// ONE RECOGNIZED CONSTRUCT, CLASSIFIED, AND NOTHING ELSE YET.
///
/// FIVE FIELDS, AND THE SHORTNESS OF THE LIST IS DELIBERATE. This stage recognizes and
/// classifies; a later stage builds a finding's identity, its fingerprint and its global
/// ordinal, and numbering runs over the FULL ordered set after every file
/// has been recognized. A per-file component that carried an identifier would therefore be
/// carrying one it cannot compute, and a per-file component that carried a line and column would
/// be computing them twice - once here and once where the total ordering is built.
///
/// So what leaves here is the minimum that ordering and numbering need and no more: WHICH class
/// matched, WHERE its anchor is, WHICH level it received, and whether its credential was written
/// into the source, and, for a Low level, why. Line and column are a pure function of the anchor offset and the file's text,
/// both of which the next stage already holds; the relative path is a property of the file rather
/// than of the construct and is not repeated on every one of them.
///
/// THE LITERAL-CREDENTIAL SIGNAL IS HERE BECAUSE NOWHERE ELSE CAN COMPUTE IT. The literal-credential
/// signal is read off the zone of the credential argument, and the credential argument is a named
/// capture group of a match that exists only while this file is being recognized. The stage that
/// builds findings holds the text and the mask but not the match, so a signal it tried to compute
/// would have to re-run the expression - which is both the cost of recognition a second time and
/// a second place for the two answers to disagree.
///
/// THE LEVEL IS ALREADY COMPOSED AND IS FINAL. Exactly one level is assigned per finding, by
/// minimum over a candidate set, and this type holds the outcome rather than the candidates - a
/// later stage holding the candidates could compose them a second time, which is precisely the
/// accumulating decrement the minimum rule exists to forbid. The threshold filter reads this
/// value and never recomputes it.
/// </summary>
public readonly record struct RecognizedConstruct
{
	/// <summary>The catalog pattern class that recognized it. The last key of the total order.</summary>
	public required string PatternClassIdentifier { get; init; }

	/// <summary>
	/// The absolute offset, in UTF-16 code units of the normalized text, of the position whose zone
	/// decided classification. It is the ANCHOR and never the start of the match: that
	/// position is the one tested against the mask, so it is also the position a reader should be sent to.
	/// </summary>
	public required int AnchorOffset { get; init; }

	/// <summary>The one level the confidence rubric assigns, already composed by minimum over its candidates.</summary>
	public required ConfidenceLevel Level { get; init; }

	/// <summary>
	/// The literal-credential signal. True where the credential argument this rule declares
	/// lies in a <see cref="LexicalZone.StringBody"/> zone, meaning the credential is embedded in
	/// source rather than supplied at run time.
	///
	/// IT IS COMPUTED FROM THE ZONE AND NEVER FROM THE VALUE, which is what makes it compatible
	/// with never reporting a secret's value: the finding says a secret sits at a location, never what it is. It
	/// is FALSE, never an error, where the rule declares no credential position at all - the analyzer contract
	/// makes that field optional and "none" the honest answer for a class where no argument is
	/// the credential.
	///
	/// It does not change the level: it travels beside the level rather than into it, and the
	/// additional security recommendation is what reads it.
	/// </summary>
	public required bool LiteralCredentialSignal { get; init; }

	/// <summary>
	/// Whether the Test demotion kind fired on the file, at any level, so that the finding can be marked as test code. It
	/// travels beside <see cref="Level"/> and never into it, and it is not a Low reason: a reason exists only on a Low finding.
	/// </summary>
	public required bool TestCode { get; init; }

	/// <summary>
	/// Which of the confidence rubric's signals proposed Low, so that a report can say why a Low finding is Low. It
	/// travels beside <see cref="Level"/> and is never read to compute one; it is
	/// <see cref="LowConfidenceReasons.None"/> whenever the level is not Low.
	/// </summary>
	public LowConfidenceReasons LowReasons { get; init; }
}
