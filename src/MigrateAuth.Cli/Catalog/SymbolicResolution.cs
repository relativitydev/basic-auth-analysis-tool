namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The three verdicts file-scoped symbolic resolution returns for an argument that
/// arrives as an identifier. There is no fourth, and ambiguity is decided by counting
/// declarations rather than by a scope model the engine does not have.
/// </summary>
public enum SymbolicVerdict
{
	Indeterminate = 0,
	ResolvedMatching = 1,
	ResolvedNotMatching = 2
}

/// <summary>
/// What a verdict does to a rule that is subject to symbolic resolution. The responses are
/// an enumeration rather than a delegate so that a rule declares an interpretation and
/// cannot execute one: an analyzer states what a verdict means, and the engine applies it.
/// </summary>
public enum SymbolicVerdictResponse
{
	/// <summary>The verdict has no effect on this rule.</summary>
	NoEffect = 0,

	/// <summary>The verdict confirms the pattern class applies. It never promotes on its own.</summary>
	ConfirmsPatternClass = 1,

	/// <summary>
	/// The construct is not an instance of this class, so there is no finding at all. A
	/// demonstrated negative is placed in recognition rather than in classification, which is
	/// what keeps "every finding receives exactly one level" true.
	/// </summary>
	SuppressesFinding = 2,

	/// <summary>The verdict proposes Medium, as one candidate level among the others.</summary>
	ProposesMediumLevel = 3
}

/// <summary>
/// How a rule interprets the symbolic verdicts affecting it, and
/// what its scheme argument is compared against.
///
/// The comparison semantics are not here: the engine fixes them as case-insensitive and tolerant
/// of surrounding whitespace, uniformly, because an HTTP authentication scheme is a
/// case-insensitive token and an ordinal comparison against "Basic" produces a false negative
/// on code that works. Uniform semantics belong to the engine, so there is no field for them.
/// </summary>
public sealed record SymbolicVerdictInterpretation
{
	/// <summary>A rule with no argument subject to symbolic resolution.</summary>
	public static SymbolicVerdictInterpretation NotSubjectToSymbolicResolution { get; } = new();

	/// <summary>The group holding the scheme argument to resolve, or null when there is none.</summary>
	public string? SchemeArgumentCaptureGroupName { get; init; }

	/// <summary>The token a resolved scheme is compared against, or null when there is none.</summary>
	public string? ExpectedSchemeToken { get; init; }

	public SymbolicVerdictResponse WhenResolvedMatching { get; init; } = SymbolicVerdictResponse.NoEffect;

	public SymbolicVerdictResponse WhenResolvedNotMatching { get; init; } = SymbolicVerdictResponse.NoEffect;

	public SymbolicVerdictResponse WhenIndeterminate { get; init; } = SymbolicVerdictResponse.NoEffect;

	/// <summary>True when this rule declares an argument for the engine to resolve.</summary>
	public bool IsSubjectToSymbolicResolution => SchemeArgumentCaptureGroupName is not null;
}
