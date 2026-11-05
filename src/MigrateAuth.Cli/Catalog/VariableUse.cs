using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// OPTIONAL, on a <see cref="FlowSignal"/>. The same-file question "is the variable this signal's match
/// names later used for something that sends it". The signal's expression captures the variable's name in
/// <see cref="VariableGroupName"/> (or the signal names a <see cref="DefaultVariableName"/>); the engine
/// then walks the logical lines after the match, inside a bounded window, and a line that references the
/// variable (<c>$NAME</c> or <c>${NAME}</c>, identifier-boundary checked, at a non-comment trustworthy
/// position) and that <see cref="UsePattern"/> matches is a use that SUPPRESSES this match. The walk
/// follows a plain copy of the variable into a second one for up to <see cref="AliasDepth"/> hops.
///
/// All of it is data and expressions the engine evaluates alone. The integers are BOUNDS of the walk, never
/// ordinals or positions: no analyzer numbers anything with them.
/// </summary>
public sealed record FlowSignalVariableUse
{
	/// <summary>The default for <see cref="VariableGroupName"/>.</summary>
	public const string DEFAULT_VARIABLE_GROUP_NAME = "var";

	/// <summary>The default number of physical lines after the match that are walked.</summary>
	public const int DEFAULT_WINDOW_LINES = 40;

	/// <summary>The default cap, in characters, of one logical line handed to <see cref="UsePattern"/>.</summary>
	public const int DEFAULT_MAX_LOGICAL_LINE_LENGTH = 400;

	/// <summary>The default number of copy hops followed.</summary>
	public const int DEFAULT_ALIAS_DEPTH = 2;

	/// <summary>The engine's ceiling for <see cref="WindowLines"/>.</summary>
	public const int MAXIMUM_WINDOW_LINES = 1000;

	/// <summary>The engine's ceiling for <see cref="MaxLogicalLineLength"/>.</summary>
	public const int MAXIMUM_LOGICAL_LINE_LENGTH = 4000;

	/// <summary>The engine's ceiling for <see cref="AliasDepth"/>.</summary>
	public const int MAXIMUM_ALIAS_DEPTH = 8;

	/// <summary>The capture group of the signal's expression that holds the variable's name.</summary>
	public string VariableGroupName { get; init; } = DEFAULT_VARIABLE_GROUP_NAME;

	/// <summary>The variable a match that names none reads (for example the one a bare <c>read</c> fills). Null when there is none.</summary>
	public string? DefaultVariableName { get; init; }

	/// <summary>
	/// Tested against ONE logical line (continuations joined, capped at <see cref="MaxLogicalLineLength"/>)
	/// that references the variable. Declared as a source-generated expression carrying the policy match bound.
	/// </summary>
	public required Regex UsePattern { get; init; }

	/// <summary>How many physical lines after the rest of the match's own physical line are walked (the rest of that line is walked too).</summary>
	public int WindowLines { get; init; } = DEFAULT_WINDOW_LINES;

	/// <summary>How many characters of one logical line are read; with <see cref="StatementSeparators"/> the cap applies to the segment.</summary>
	public int MaxLogicalLineLength { get; init; } = DEFAULT_MAX_LOGICAL_LINE_LENGTH;

	/// <summary>How many plain copies of the variable into another are followed.</summary>
	public int AliasDepth { get; init; } = DEFAULT_ALIAS_DEPTH;

	/// <summary>
	/// OPTIONAL. Plain-text statement separators (for example a semicolon, a double ampersand). Empty, the default,
	/// tests <see cref="UsePattern"/> on the whole logical line. When non-empty it is tested only on the segment of the
	/// logical line that holds the variable reference, bounded by the nearest declared separator before and after the reference.
	/// A pipe is a separator only if the signal says so. The search is quote-unaware.
	/// </summary>
	public ImmutableArray<string> StatementSeparators { get; init; } = [];
}

/// <summary>
/// OPTIONAL, on a <see cref="DetectionRule"/>. A construct whose match names a variable (the capture group
/// <see cref="VariableGroupName"/> participated) is kept only if the variable is referenced
/// (<c>$NAME</c> or <c>${NAME}</c>, identifier-boundary checked) at least once ANYWHERE in the file outside
/// the match's own span, at a position that is not a comment. A construct that builds a credential into a
/// variable nothing ever reads is never sent anywhere, and the engine drops it without a finding.
/// A rule that declares none, or a match where the group did not participate, keeps the construct.
/// </summary>
public sealed record RuleVariableUse
{
	/// <summary>The capture group of the rule's expression that holds the variable's name.</summary>
	public string VariableGroupName { get; init; } = FlowSignalVariableUse.DEFAULT_VARIABLE_GROUP_NAME;
}
