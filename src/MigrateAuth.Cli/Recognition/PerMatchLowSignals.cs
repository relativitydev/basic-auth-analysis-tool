namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE PER-MATCH LOW PROPOSALS OF ONE MATCH, COMPUTED ONCE IN <c>TryClassify</c> AND READ BY BOTH
/// <c>ComposeLevel</c> AND <c>ReadTheLowReasons</c>, so the level and the reason it is given cannot disagree.
/// Each value is a Low proposal and nothing here promotes: a <see langword="false"/> proposes nothing.
/// </summary>
internal readonly struct PerMatchLowSignals
{
	/// <summary>
	/// The rule's declared per-match condition proposed Low: both operand groups participated and neither one's
	/// captured text names a credential.
	/// </summary>
	internal bool NeutralOperandPair { get; init; }

	/// <summary>The match's anchor offset lies in a conditional-compilation region that is definitely not taken.</summary>
	internal bool InactiveRegion { get; init; }
}
