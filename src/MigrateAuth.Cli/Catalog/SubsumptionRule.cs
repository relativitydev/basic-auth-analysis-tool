namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// When one pattern class subsumes another. A declared condition rather than a predicate the
/// analyzer executes: the analyzer states the relation and the engine evaluates it, which is
/// what keeps the decision about what gets emitted on the engine's side of the contract.
/// </summary>
public enum SubsumptionCondition
{
	/// <summary>The two matches cover exactly the same span.</summary>
	MatchSpansAreIdentical = 0,

	/// <summary>The two matches overlap at all.</summary>
	MatchSpansOverlap = 1,

	/// <summary>The two matches resolve their anchors to the same position.</summary>
	AnchorPositionsAreIdentical = 2
}

/// <summary>
/// Contract element 6, and rule overlap. A declared triple of subsuming, subsumed and
/// when. Without it, one construct matching two classes emits twice.
///
/// Absent a declared relation, both classes are emitted, so the omission is visible in the
/// output rather than silent.
/// </summary>
public sealed record SubsumptionRule
{
	/// <summary>The class that is emitted.</summary>
	public required string SubsumingPatternClassIdentifier { get; init; }

	/// <summary>The class that is suppressed.</summary>
	public required string SubsumedPatternClassIdentifier { get; init; }

	public required SubsumptionCondition When { get; init; }
}
