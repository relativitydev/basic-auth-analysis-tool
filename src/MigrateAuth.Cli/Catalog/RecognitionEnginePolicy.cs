namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// THE ONE RECOGNITION RULE THAT BELONGS TO THE ENGINE AND IS NOT EXPRESSIBLE IN A CATALOG, the
/// same shape and for the same reason as <see cref="LexicalEnginePolicy"/>: a bound an analyzer
/// could choose is a bound each analyzer would choose differently, and this one is stated at
/// the level of the file rather than of the rule.
///
/// The bound applies to the time spent, for one file, inside every analyzer-declared expression the engine
/// evaluates: detection rules, the constant-declaration expression, generation and content markers, and flow
/// signals, accumulated by <see cref="MigrateAuth.Cli.Recognition.MatchBoundWatch"/> through
/// <see cref="MigrateAuth.Cli.Recognition.TimedRegexEvaluation"/>. The engine's own classification and
/// subsumption phases are not charged to it, and the lexical scan and first-line language attribution run
/// before a file's budget opens. A trip means an expression that does not finish, not a large file and not a
/// slow engine phase. The two findings caps are <see cref="FindingsCapPolicy"/>'s.
/// </summary>
public static class RecognitionEnginePolicy
{
	/// <summary>
	/// The declared exception to determinism, expressed in milliseconds
	/// so that the constant carries its unit in its name.
	/// </summary>
	public const int PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS = 112000;

	/// <summary>
	/// The bound itself. Read as a <see cref="TimeSpan"/> everywhere, so that a retuning changes
	/// exactly one number and no arithmetic at a call site changes with it.
	/// </summary>
	public static readonly TimeSpan MATCH_BOUND = TimeSpan.FromMilliseconds(PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS);
}
