using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// What a flow signal is evidence of. The definition is language-agnostic: an interactive-user
/// signal is evidence that the credential belongs to a person who supplies it at runtime, and a
/// fixed-credential signal is evidence that the credential is fixed for the integration.
///
/// <see cref="Undeclared"/> is the zero value on purpose. A <c>required</c> member cannot see an
/// explicit default, so a signal whose author never chose a kind would otherwise read as a valid
/// one; the sentinel makes "no kind" detectable and <see cref="AnalyzerContractValidator"/> rejects it.
/// </summary>
public enum FlowSignalKind
{
	Undeclared = 0,
	InteractiveUser = 1,
	FixedCredential = 2
}

/// <summary>
/// Where a flow signal is permitted to fire. <see cref="CodeOnly"/> admits the
/// <see cref="Lexical.LexicalZone.Code"/> zone only and does not admit a delimiter, following the
/// content-marker predicate. <see cref="CodeOrStringBody"/> admits every zone except a comment.
/// Both require a trustworthy mask position at the signal's anchor.
///
/// <see cref="Undeclared"/> is the zero value for the same reason <see cref="FlowSignalKind.Undeclared"/> is.
/// </summary>
public enum FlowSignalZone
{
	Undeclared = 0,
	CodeOnly = 1,
	CodeOrStringBody = 2
}

/// <summary>
/// One flow signal of a language: a named expression that, where it fires, is evidence of one
/// <see cref="FlowSignalKind"/>. A signal is data - a name, a kind, a zone and an already constructed
/// expression - and never a function, so an analyzer still never sees the file. The expression is
/// evaluated by the engine alone, inside the same per-file match bound as every other
/// analyzer-declared expression.
///
/// The anchor is the optional capture group named <see cref="AnchorGroupName"/>; where the expression
/// declares no such group the anchor is the start of the match.
/// </summary>
public sealed record FlowSignal
{
	/// <summary>The name of the optional capture group that carries a signal's anchor.</summary>
	public const string AnchorGroupName = "anchor";

	/// <summary>Unique within one analyzer, by ordinal comparison.</summary>
	public required string Name { get; init; }

	public required FlowSignalKind Kind { get; init; }

	public required FlowSignalZone Zone { get; init; }

	/// <summary>Declared as a source-generated expression carrying the policy match bound.</summary>
	public required Regex Expression { get; init; }
}
