namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// Contract element 2. One construct this analyzer's language exposes that its
/// catalog does not attempt to recognize, and why - the declared-unsupported discipline,
/// read as a field of the contract rather than as a claim a catalog
/// document makes on its own.
///
/// A record, so a <see cref="System.Collections.Frozen.FrozenSet{T}"/> of these dedups on value
/// rather than on reference - two entries naming the same construct with the same reason collapse
/// to one, exactly as the set type is meant to be used elsewhere in this contract. A duplicate
/// <see cref="Construct"/> carrying a DIFFERENT <see cref="Reason"/> is not caught by that
/// dedup - the two records are unequal - so <see cref="AnalyzerContractValidator"/> checks for it
/// separately.
///
/// <see cref="Construct"/> is a generic description: a vendor or library name has no place here, only the
/// shape of the construct itself.
/// </summary>
public sealed record DeclaredUnsupportedCoverage
{
	/// <summary>The construct's generic description, never a third-party name.</summary>
	public required string Construct { get; init; }

	/// <summary>Why this catalog does not attempt to recognize it.</summary>
	public required string Reason { get; init; }
}
