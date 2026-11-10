using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// The one policy the engine applies to a construct that never closes. It is an
/// enumeration with a single value because there is exactly one policy and it is uniform
/// across every rule: two different policies for a literal and for a comment is a defect,
/// not a choice, so the type offers no second value to choose.
/// </summary>
public enum UnterminatedConstructPolicy
{
	ReportAnomalyAndCloseAtBoundary = 0
}

/// <summary>
/// The three lexical rules that belong to the engine and are not expressible in a
/// profile. They live here, on an engine type, rather than as fields a profile author could
/// supply:
///
/// 1. The longest opening wins, and the engine orders the rules. A profile hands over its
///    rules as a sequence, and <see cref="LexicalProfile"/> discards that sequence's order
///    on construction - the author's order is not recoverable from a constructed profile,
///    so listing the single quote before the triple cannot break the scanner.
/// 2. The unterminated-construct policy is the engine's and is uniform across all rules.
/// 3. The recursion bound is the engine's.
/// </summary>
public static class LexicalEnginePolicy
{
	/// <summary>
	/// The interpolation recursion bound. Exceeding it is an anomaly, never a stack overflow, and it is
	/// the engine's number rather than a profile's. Each nesting level costs one stack frame of
	/// recursive descent.
	/// </summary>
	public const int MAXIMUM_INTERPOLATION_RECURSION_DEPTH = 8;

	/// <summary>
	/// In milliseconds so the constant carries its unit in its name. The per-file, per-stage
	/// budget for the three analyzer-declared expressions evaluated outside the recognition
	/// engine's match bound: a here-document opening and an interpolation identifier extent
	/// (charged in the lexical scan) and a first-line detection expression (charged in language
	/// attribution). Each of the three also declares this figure as its own regex timeout, because
	/// a cooperative watch cannot interrupt one stuck match.
	///
	/// It sits far below the recognition engine's own match bound, because every search charged
	/// here is confined to one line's window rather than run over a whole file.
	/// </summary>
	public const int PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS = 250;

	/// <summary>
	/// The budget itself. Read as a <see cref="TimeSpan"/> everywhere, so that a change to it
	/// changes exactly one number and no arithmetic at a call site changes with it.
	/// </summary>
	public static readonly TimeSpan LEXICAL_BUDGET = TimeSpan.FromMilliseconds(PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS);

	/// <summary>Uniform across literals and comments alike.</summary>
	public const UnterminatedConstructPolicy UNTERMINATED_CONSTRUCT_POLICY =
		UnterminatedConstructPolicy.ReportAnomalyAndCloseAtBoundary;

	/// <summary>
	/// Longest opening first. The tie-breakers are ordinal and culture-invariant so that the
	/// resulting order is identical on every platform.
	/// </summary>
	public static ImmutableArray<QuoteRule> OrderQuoteRules(IEnumerable<QuoteRule> quoteRules)
	{
		ArgumentNullException.ThrowIfNull(quoteRules);

		ImmutableArray<QuoteRule> ordered =
		[
			.. quoteRules
				.OrderByDescending(rule => rule.Opening.Length)
				.ThenBy(rule => rule.Opening, StringComparer.Ordinal)
				.ThenBy(rule => rule.Closing, StringComparer.Ordinal)
		];

		return ordered;
	}

	/// <summary>Longest opening first, for the same reason and with the same tie-breakers.</summary>
	public static ImmutableArray<LineCommentRule> OrderLineCommentRules(IEnumerable<LineCommentRule> lineCommentRules)
	{
		ArgumentNullException.ThrowIfNull(lineCommentRules);

		ImmutableArray<LineCommentRule> ordered =
		[
			.. lineCommentRules
				.OrderByDescending(rule => rule.Opening.Length)
				.ThenBy(rule => rule.Opening, StringComparer.Ordinal)
		];

		return ordered;
	}

	/// <summary>Longest opening first, for the same reason and with the same tie-breakers.</summary>
	public static ImmutableArray<BlockCommentRule> OrderBlockCommentRules(IEnumerable<BlockCommentRule> blockCommentRules)
	{
		ArgumentNullException.ThrowIfNull(blockCommentRules);

		ImmutableArray<BlockCommentRule> ordered =
		[
			.. blockCommentRules
				.OrderByDescending(rule => rule.Opening.Length)
				.ThenBy(rule => rule.Opening, StringComparer.Ordinal)
				.ThenBy(rule => rule.Closing, StringComparer.Ordinal)
		];

		return ordered;
	}
}
