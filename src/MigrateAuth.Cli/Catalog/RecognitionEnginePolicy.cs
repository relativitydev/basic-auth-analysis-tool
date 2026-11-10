namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// THE ONE RECOGNITION RULE THAT BELONGS TO THE ENGINE AND IS NOT EXPRESSIBLE IN A CATALOG, the
/// same shape and for the same reason as <see cref="LexicalEnginePolicy"/>: a bound an analyzer
/// could choose is a bound each analyzer would choose differently, so this one is stated at
/// the level of the file rather than of the rule.
///
/// WHAT IS TIMED. The bound charges every analyzer-declared expression's own evaluation over a
/// file: a detection rule's <c>Match</c>/<c>NextMatch</c> calls, the lexical profile's
/// constant-declaration expression (<c>Recognition.FileSymbolTable.Collect</c>) and every
/// generation-marker and content-marker expression (<c>Recognition.FileDemotionSignals</c>).
/// <see cref="MigrateAuth.Cli.Recognition.MatchBoundWatch"/> accumulates the time spent inside any
/// of those, through the shared door <see cref="MigrateAuth.Cli.Recognition.TimedRegexEvaluation"/>,
/// with one budget per file. The engine's own classification and subsumption phases are not charged,
/// because no catalog expression controls them, and the lexical scan and first-line language
/// attribution run before a file's budget opens and remain outside it (<c>TimedRegexEvaluation</c>'s
/// own summary).
///
/// A trip therefore means an expression that does not finish, not a large file and not a slow
/// engine phase. Every <c>CSharp</c> rule's <c>[GeneratedRegex]</c> names this constant as its
/// single-match timeout.
/// </summary>
public static class RecognitionEnginePolicy
{
	/// <summary>
	/// The engine's declared exception to determinism: the per-file bound on analyzer-expression
	/// time, expressed in milliseconds so that the constant carries its unit in its name.
	/// </summary>
	public const int PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS = 5250;

	/// <summary>
	/// The bound itself. Read as a <see cref="TimeSpan"/> everywhere, so that a change to it changes
	/// exactly one number and no arithmetic at a call site changes with it.
	/// </summary>
	public static readonly TimeSpan MATCH_BOUND = TimeSpan.FromMilliseconds(PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS);
}
