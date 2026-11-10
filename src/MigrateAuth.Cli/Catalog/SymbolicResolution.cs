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
	ResolvedNotMatching = 2,

	/// <summary>
	/// The declared scheme group took no part in the match: the construct was recognized by an
	/// alternative that carries no scheme argument at all (an assembled value, say). Distinct from
	/// <see cref="Indeterminate"/>, where the argument is present and its value cannot be resolved.
	/// A rule answers it with <see cref="SymbolicVerdictInterpretation.WhenSchemeArgumentDidNotParticipate"/>.
	/// </summary>
	ArgumentDidNotParticipate = 3
}

/// <summary>
/// Where a declaration of the file symbol table came from. A record is a string constant (<see cref="Literal"/>, the default of every record a
/// declaration expression produces) or a value read from the process environment (<see cref="Environment"/>), which carries the word of its fallback.
/// </summary>
public enum SymbolSourceKind
{
	/// <summary>A string constant: the word of the record is the leading word of its literal.</summary>
	Literal = 0,

	/// <summary>A value read from the environment: the word of the record is the leading word of its fallback literal, empty where there is none.</summary>
	Environment = 1
}

/// <summary>
/// The groups through which a profile's constant-declaration expression says that a declaration is an environment record, by the same engine convention as the
/// groups <c>name</c> and <c>value</c>: the empty group <see cref="MARKER"/> takes part in the match of an environment declaration, and the group
/// <see cref="FALLBACK_WORD"/> captures the word of its fallback (it takes part, empty, where there is no fallback). An expression that declares the marker
/// without the fallback group is refused by the validator.
/// </summary>
public static class EnvironmentDeclarationGroups
{
	/// <summary>The empty group that marks a declaration as read from the environment.</summary>
	public const string MARKER = "environment";

	/// <summary>The group that holds the word of the fallback of an environment declaration.</summary>
	public const string FALLBACK_WORD = "fallbackWord";
}

/// <summary>
/// Which dictionary of the file symbol table a cell reads. A <see cref="Variable"/> record is a line-start declaration of a name and resolves only where exactly one declaration
/// of the name stands BEFORE the use. A <see cref="Member"/> record is a line-start assignment of a member name (an object-literal member or a property assignment), keyed by the
/// member name alone, and resolution is ORDER-FREE: exactly one record of the name resolves wherever it stands in the file, because a settings object may be built after the
/// function that uses it. Zero records, or two or more, are indeterminate; a single record that is not an environment read is indeterminate too.
/// </summary>
public enum SymbolRecordKind
{
	/// <summary>The variable dictionary, fed by <see cref="LexicalProfile.ConstantDeclarationExpression"/>.</summary>
	Variable = 0,

	/// <summary>The member dictionary, fed by <see cref="LexicalProfile.MemberDeclarationExpression"/>.</summary>
	Member = 1
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
	/// The construct is not an instance of this class, so there is no finding at all. The
	/// design places a demonstrated negative in recognition rather than in classification, which is
	/// what keeps "every finding receives exactly one level" true.
	/// </summary>
	SuppressesFinding = 2,

	/// <summary>The verdict proposes Medium, as one candidate level among the others.</summary>
	ProposesMediumLevel = 3
}

/// <summary>
/// Contract element 3 field 5. How a rule interprets the symbolic verdicts affecting it, and
/// what its scheme argument is compared against.
///
/// The comparison semantics are not here: they are fixed as case-insensitive and tolerant
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

	/// <summary>
	/// The token a resolved scheme is compared against, or null when there is none. A cell that expects a source kind may leave it null or blank, which reads a record
	/// of that kind whose word is empty.
	/// </summary>
	public string? ExpectedSchemeToken { get; init; }

	/// <summary>
	/// Optional, null by default: the kind of record the cell reads. When set, the verdict of the cell is resolved and matching only for a record of that kind whose
	/// word matches <see cref="ExpectedSchemeToken"/>, and a record of the other kind is resolved and not matching; when null the cell reads records of both kinds.
	/// Meaningful only with a scheme argument.
	/// </summary>
	public SymbolSourceKind? ExpectedSourceKind { get; init; }

	/// <summary>
	/// Optional, <see cref="Catalog.SymbolRecordKind.Variable"/> by default: the dictionary the scheme argument is resolved in. <see cref="Catalog.SymbolRecordKind.Member"/> reads
	/// the member dictionary by the captured name, and requires the profile to declare <see cref="LexicalProfile.MemberDeclarationExpression"/>. Meaningful only with a scheme argument.
	/// </summary>
	public SymbolRecordKind SymbolRecordKind { get; init; } = SymbolRecordKind.Variable;

	public SymbolicVerdictResponse WhenResolvedMatching { get; init; } = SymbolicVerdictResponse.NoEffect;

	public SymbolicVerdictResponse WhenResolvedNotMatching { get; init; } = SymbolicVerdictResponse.NoEffect;

	public SymbolicVerdictResponse WhenIndeterminate { get; init; } = SymbolicVerdictResponse.NoEffect;

	/// <summary>
	/// What a match in which the scheme group did not participate means for the rule. Null, the default,
	/// reads it as <see cref="WhenIndeterminate"/>. Only meaningful with a scheme group.
	/// </summary>
	public SymbolicVerdictResponse? WhenSchemeArgumentDidNotParticipate { get; init; }

	/// <summary>True when this rule declares an argument for the engine to resolve.</summary>
	public bool IsSubjectToSymbolicResolution => SchemeArgumentCaptureGroupName is not null;
}
