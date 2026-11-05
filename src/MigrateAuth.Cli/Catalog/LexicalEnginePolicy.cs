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
	/// The recursion bound. Exceeding it is an anomaly, never a stack overflow, and it is
	/// the engine's number rather than a profile's.
	/// </summary>
	public const int MAXIMUM_INTERPOLATION_RECURSION_DEPTH = 8;

	/// <summary>
	/// How many times one file may fall back from nested-hole counting to the
	/// first closing. Each fallback re-reads the hole, so an unbounded number of unbalanced openers
	/// would make the scan quadratic; past this count the rest of the file reads every hole by its
	/// first closing and no further anomaly is recorded.
	/// </summary>
	public const int MAXIMUM_NESTING_RECOVERIES_PER_FILE = 8;

	/// <summary>
	/// How many characters before a quoted string or a here-document opening the scanner looks back for the
	/// command position that begins the words of an executed-string context, and the most words it reads between
	/// that position and the opening. The look-back costs at most this much per opening, so a file of openings
	/// costs a bounded multiple of its length; a command whose words begin farther back, or that has more words, is
	/// not read as an executed-string context and its strings stay string zones, with no anomaly recorded.
	/// </summary>
	public const int EXECUTED_STRING_LOOKBACK_CHARACTERS = 256;

	/// <summary>The most words of one command the executed-string context reads before the opening it asks about.</summary>
	public const int EXECUTED_STRING_MAXIMUM_COMMAND_WORDS = 32;

	/// <summary>
	/// How many executed strings may be read one inside another, a body read as code that holds a string it again
	/// reads as code. It is the recursion bound of the interpolation holes, the engine's one nesting number; a string
	/// nested beyond it stays a string zone and records no anomaly.
	/// </summary>
	public const int MAXIMUM_EXECUTED_STRING_NESTING_DEPTH = MAXIMUM_INTERPOLATION_RECURSION_DEPTH;

	/// <summary>
	/// In milliseconds so the constant carries its unit in its name. The per-file, per-stage
	/// budget for the three analyzer-declared expressions evaluated outside the recognition
	/// engine's match bound: a here-document opening and an interpolation identifier extent
	/// (charged in the lexical scan) and a first-line detection expression (charged in language
	/// attribution). Each of the three also declares this figure as its own regex timeout, because
	/// a cooperative watch cannot interrupt one stuck match.
	/// </summary>
	public const int PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS = 1000;

	/// <summary>
	/// The budget itself. Read as a <see cref="TimeSpan"/> everywhere, so that a retuning
	/// changes exactly one number and no arithmetic at a call site changes with it.
	/// </summary>
	public static readonly TimeSpan LEXICAL_BUDGET = TimeSpan.FromMilliseconds(PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS);

	/// <summary>Uniform across literals and comments alike.</summary>
	public const UnterminatedConstructPolicy UNTERMINATED_CONSTRUCT_POLICY =
		UnterminatedConstructPolicy.ReportAnomalyAndCloseAtBoundary;

	/// <summary>
	/// Longest opening first. The tie-breakers are ordinal and culture-invariant so that the
	/// resulting order is identical on every platform, which the ordering guarantee needs.
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
