namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// THE ONE RECOGNITION RULE THAT BELONGS TO THE ENGINE AND IS NOT EXPRESSIBLE IN A CATALOG, the
/// same shape and for the same reason as <see cref="LexicalEnginePolicy"/>: a bound an analyzer
/// could choose is a bound each analyzer would choose differently, and this one is stated at
/// the level of the file rather than of the rule. The two findings caps are <see cref="FindingsCapPolicy"/>'s.
///
/// WHAT IS TIMED. The bound charges the time spent evaluating every analyzer-declared expression for
/// one file: each detection rule's <c>Match</c>/<c>NextMatch</c> calls, the lexical profile's
/// constant-declaration expression (<c>Recognition.FileSymbolTable.Collect</c>) and every
/// generation-marker and content-marker expression (<c>Recognition.FileDemotionSignals</c>).
/// <see cref="MigrateAuth.Cli.Recognition.MatchBoundWatch"/> accumulates that time through the shared door
/// <see cref="MigrateAuth.Cli.Recognition.TimedRegexEvaluation"/>: one file, one budget. Classification
/// and subsumption, the engine's own two phases, are not charged, because no catalog expression controls
/// them. The lexical scan and first-line language attribution run before a file's budget opens and remain
/// outside it (<c>TimedRegexEvaluation</c>'s own summary). A trip therefore means an expression that does
/// not finish, not a large file and not a slow engine phase.
///
/// The value in force is <see cref="PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS"/>.
/// </summary>
public static class RecognitionEnginePolicy
{
	/// <summary>
	/// The declared exception to determinism, expressed in milliseconds
	/// so that the constant carries its unit in its name: the bound on the analyzer-expression
	/// time of one file.
	/// </summary>
	public const int PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS = 22750;

	/// <summary>
	/// The bound itself. Read as a <see cref="TimeSpan"/> everywhere, so that a retuning changes
	/// exactly one number and no arithmetic at a call site changes with it.
	/// </summary>
	public static readonly TimeSpan MATCH_BOUND = TimeSpan.FromMilliseconds(PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS);
}
