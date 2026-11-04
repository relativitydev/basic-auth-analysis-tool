using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The confidence level a finding receives. The engine composes candidate levels by minimum, so a
/// rule declares the level it proposes and never the level a finding ends up with.
/// </summary>
public enum ConfidenceLevel
{
	Low = 0,
	Medium = 1,
	High = 2
}

/// <summary>
/// Whether the recognized construct declares both the scheme and the credential
/// carrier on its own. A class that needs the call path established cannot reach High,
/// because that requires flow analysis the engine does not provide.
/// </summary>
public enum SelfSufficiency
{
	RequiresCallPathToBeEstablished = 0,
	DeclaresSchemeAndCredentialCarrier = 1
}

/// <summary>
/// Destination certainty. Two groups and not three: a construct anchored on a
/// credential type from the Relativity SDK packages establishes its destination, while a
/// hand-assembled header establishes nothing in either direction. A third group saying
/// "not directed at Relativity" would assert knowledge the engine does not have.
/// </summary>
public enum DestinationCertainty
{
	Unknown = 0,
	Established = 1
}

/// <summary>
/// Contract element 3. One detection rule, carrying exactly eight fields per pattern class. The
/// pattern class identifier is not one of them: it is the key this rule is registered under,
/// so the eight below are the rule and nothing else.
///
/// 1. <see cref="Expression"/> - the constructed expression
/// 2. <see cref="AnchorPosition"/> - the anchor position
/// 3. <see cref="BaseLevel"/> - the base level
/// 4. <see cref="SelfSufficiency"/> - the self-sufficiency declaration
/// 5. <see cref="SymbolicVerdictInterpretation"/> - how it reads the verdicts affecting it
/// 6. <see cref="CredentialPosition"/> - the credential position, optional, defaulting to none
/// 7. <see cref="DestinationCertainty"/> - the destination certainty
/// 8. <see cref="DisplayName"/> - the human-legible name the report renders
///
/// Fields six through eight ride inside this element rather than becoming top-level elements of
/// the contract: the contract grows by fields within its elements, not by elements.
///
/// The expression arrives already constructed and is never a string. The generator that
/// produces it at build time needs a constant pattern and yields a method, not data, so a
/// contract carrying strings and a contract carrying build-time generated expressions cannot
/// both be true. Under ahead-of-time compilation a runtime-constructed expression is also
/// interpreted, so this is a throughput decision as well as a correctness one.
/// </summary>
public sealed class DetectionRule
{
	/// <summary>Field 1. Handed over already constructed; the engine never sees its provenance.</summary>
	public required Regex Expression { get; init; }

	/// <summary>Field 2. The offset within the match whose zone decides classification.</summary>
	public required MatchAnchor AnchorPosition { get; init; }

	/// <summary>Field 3. The level this class proposes before any other candidate applies.</summary>
	public required ConfidenceLevel BaseLevel { get; init; }

	/// <summary>Field 4. Whether the construct stands on its own.</summary>
	public required SelfSufficiency SelfSufficiency { get; init; }

	/// <summary>Field 5. How this rule reads the symbolic verdicts affecting it.</summary>
	public required SymbolicVerdictInterpretation SymbolicVerdictInterpretation { get; init; }

	/// <summary>Field 6. Optional, and none by default.</summary>
	public CredentialPosition CredentialPosition { get; init; } = CredentialPosition.None();

	/// <summary>Field 7. Established or unknown.</summary>
	public required DestinationCertainty DestinationCertainty { get; init; }

	/// <summary>
	/// Field 8. The pattern class's human name - never the matched text, and never
	/// derived from anything in the analyzed file. The report renders the name, looked up from the
	/// catalog by id; this is the field that lookup reads. Required and non-empty, per
	/// <see cref="AnalyzerContractValidator"/>, and unique within the analyzer that declares it - two
	/// pattern classes sharing one name would make the report's own cross-reference to the identifier
	/// ambiguous. The catalog identifier remains what the artifact keys on; this field is read-only
	/// prose for a human, never a key.
	/// </summary>
	public required string DisplayName { get; init; }
}
