namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// THE FINDINGS CAP, THE SAME SHAPE AS <see cref="RecognitionEnginePolicy"/> AND FOR THE SAME
/// REASON: a bound an analyzer could choose is a bound each analyzer would choose differently, so
/// this one is stated at the level of the engine rather than of any one catalog.
///
/// BOTH VALUES ARE BOUNDS ON PATHOLOGICAL OR GENERATED INPUT, NOT ON ORDINARY REPOSITORIES. The
/// per-file cap exists so that one generated file with tens of thousands of matches cannot spend
/// the whole global budget before a second file is ever read.
/// </summary>
public static class FindingsCapPolicy
{
	/// <summary>
	/// How many findings ONE file may contribute to a run. A bound on a generated or pathological
	/// file. Findings are
	/// admitted in <see cref="Findings.RecognizedFile.Constructs"/>'s own order - anchor offset,
	/// then pattern class identifier, which is the local order for one file - so which findings
	/// of an over-cap file survive is a property of the file's own content and never of recognition's
	/// internal rule order or of an arrival accident.
	/// </summary>
	public const int PROVISIONAL_PER_FILE_FINDINGS_CAP = 2_000;

	/// <summary>
	/// How many findings a whole run may retain. A bound on a generated or pathological repository.
	/// Findings are admitted by a total order - the normalized path key,
	/// then the raw path compared ordinally - and not in the order files happen to arrive in, so the
	/// survivors are the leading prefix by path rather than an arrival accident.
	/// </summary>
	public const int PROVISIONAL_GLOBAL_FINDINGS_CAP = 50_000;
}
