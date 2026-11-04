using System.Collections.Immutable;

namespace MigrateAuth.Cli.Findings;

/// <summary>
/// WHAT COMPOSITION PRODUCES WHEN A CALLER ALSO NEEDS TO KNOW WHETHER THE FINDINGS CAP TRIPPED.
///
/// <see cref="FindingComposition.Compose"/> returns only an <see cref="ImmutableArray{T}"/> of
/// <see cref="Finding"/>. A caller that needs the truncation counter -
/// <see cref="Pipeline.AnalyzeStage"/>, which is the one place findings are committed to the
/// session state - calls <see cref="FindingComposition.ComposeWithCapOutcome"/> instead, and
/// this is what it gets back.
/// </summary>
public sealed record ComposedFindingSet
{
	/// <summary>The complete, ordered, numbered finding set, already reduced by both caps.</summary>
	public required ImmutableArray<Finding> Findings { get; init; }

	/// <summary>What the caps admitted and what they held back.</summary>
	public required FindingsCapOutcome CapOutcome { get; init; }
}
