using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Emission;

/// <summary>
/// What kind of thing is being emitted. It is the one field every emitted fact carries, which
/// is what lets a sink decide where a fact belongs without inspecting anything else about it.
///
/// This is NOT the skipped-item enumeration. That one is a closed list that says WHY an item was
/// skipped rather than what kind of fact this is, and it travels as
/// <see cref="EmittedFact.SkippedItem"/>, a separate field.
/// </summary>
public enum EmissionCategory
{
	/// <summary>No category. The value of <c>default</c>, which no emission accepts.</summary>
	None = 0,

	/// <summary>A recognized construct.</summary>
	Finding = 1,

	/// <summary>A file selected for analysis.</summary>
	Candidate = 2,

	/// <summary>An item the scan did not cover. The skipped-item category says why.</summary>
	SkippedItem = 3,

	/// <summary>What the assessment looked for and what it declares unsupported.</summary>
	Coverage = 4,

	/// <summary>Tool version, catalog version, active languages and filters.</summary>
	Provenance = 5,

	/// <summary>Something that went wrong, said without the framework's own message.</summary>
	Diagnostic = 6
}

/// <summary>
/// THE DATA INVARIANT MADE INTO A TYPE.
///
/// > No artifact the product emits contains text taken from the analyzed file, other than its
/// > path relative to the repository.
///
/// The invariant reaches all stages, derived values as well as literals, and eleven sinks -
/// console, report, data artifact, state file, diagnostic log, exception messages, prompt pack,
/// and whatever is added later. Stated as a rule that is exactly right and exactly unenforceable:
/// a rule is honoured from memory at eleven sites, and forgetting it is silent. Nothing fails,
/// no counter fires, and it surfaces when a client reads a credential in their report.
///
/// The case that matters is not carelessness. It is someone adding the matched line to the
/// report FOR CONTEXT, which is a genuine improvement and is exactly the violation.
///
/// So the invariant has a chokepoint instead, and this record is its shape: the six permitted
/// fields, and nowhere to put a seventh. Five of the six cannot hold text at all - three
/// enumerations and an integer. The sixth is the relative path, which is the invariant's one
/// exemption and is a type that only the write boundary's containment primitive can produce.
/// Emitting text from the analyzed file therefore requires ADDING A FIELD HERE, which is a
/// signature change, which is visible in review and cannot happen by accident.
///
/// <see cref="SkippedItem"/> is admitted because it is a closed enumeration that cannot express
/// one character of an analyzed file, which is the standard any seventh field has to meet.
///
/// IT DOES NOT MAKE VIOLATION IMPOSSIBLE and is not presented as doing so: a direct write to a
/// stream bypasses it entirely. What it does is make the correct path the easy one and the
/// incorrect one visible.
/// </summary>
public sealed record EmittedFact
{
	/// <summary>What kind of fact this is. The only field every emission carries.</summary>
	public required EmissionCategory Category { get; init; }

	/// <summary>Where, relative to the repository root. Absent for a fact about no one file.</summary>
	public RepositoryRelativePath Path { get; init; }

	/// <summary>Which line. Absent where the fact is about a file rather than a place in it.</summary>
	public int? LineNumber { get; init; }

	/// <summary>Which catalog pattern class. Absent for anything that is not a finding.</summary>
	public RuleIdentifier Rule { get; init; }

	/// <summary>
	/// The confidence the rubric assigned. Absent where confidence has no meaning.
	/// It is the assigned level and never a threshold: configuration stays out of
	/// assignment and only filters what appears in output.
	/// </summary>
	public ConfidenceLevel? Level { get; init; }

	/// <summary>
	/// WHY an item was skipped, from the closed skipped-item list. Present exactly when
	/// <see cref="Category"/> is <see cref="EmissionCategory.SkippedItem"/>, and absent
	/// otherwise - which <see cref="EmissionPoint.Emit"/> enforces rather than trusts, because a
	/// skipped item with no reason is an aggregate count that says nothing useful.
	/// </summary>
	public SkippedItemCategory? SkippedItem { get; init; }
}
