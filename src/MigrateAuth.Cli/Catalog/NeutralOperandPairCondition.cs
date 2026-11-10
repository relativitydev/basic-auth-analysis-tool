using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// OPTIONAL, on a <see cref="DetectionRule"/> as <see cref="DetectionRule.LowWhenBothOperandsAreNeutral"/>. A per-match
/// Low condition declared as data, never as a delegate: two capture groups of the rule's expression, each with a word
/// expression. The engine proposes Low for that one finding if and only if BOTH groups participated in the match and
/// NEITHER group's captured text matches its word expression. A group that did not participate proposes nothing, so
/// the failure direction keeps the class level.
///
/// Both word expressions are evaluated by the engine through the timed door, so each must carry the policy match
/// bound (see <see cref="AnalyzerContractValidator"/>); they hold alternations of literals and no loop, which is what
/// keeps the evaluation linear in the captured text.
/// </summary>
public sealed record NeutralOperandPairCondition
{
	/// <summary>The capture group of the rule's expression that holds the first operand's text.</summary>
	public required string FirstOperandGroupName { get; init; }

	/// <summary>The words that make the first operand credential-named; matched against the group's whole captured text.</summary>
	public required Regex FirstOperandWordExpression { get; init; }

	/// <summary>The capture group of the rule's expression that holds the second operand's text.</summary>
	public required string SecondOperandGroupName { get; init; }

	/// <summary>The words that make the second operand credential-named; matched against the group's whole captured text.</summary>
	public required Regex SecondOperandWordExpression { get; init; }

	/// <summary>
	/// OPTIONAL. The identifier suffixes (compared whole and ignoring letter case, on identifier word segments). An occurrence of
	/// a word expression's word that is directly followed by one of them does not name a credential, so a half counts as named
	/// only if some occurrence of its words is not followed by a suffix. Empty (the default): any occurrence counts.
	/// </summary>
	public ImmutableArray<string> IdentifierSuffixes { get; init; } = [];
}
