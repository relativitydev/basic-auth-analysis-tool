using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// One pair of tokens inside which a statement separator does not end the statement, such as a command
/// substitution's opening and closing. The two tokens differ: a token that opens and closes with the same text
/// cannot be counted without reading the language, so the validator refuses it.
/// </summary>
public sealed record StatementNesting
{
	public required string Opening { get; init; }

	public required string Closing { get; init; }
}

/// <summary>
/// The seventh contract element, and an OPTIONAL one: how a language says that several of its classes reading
/// one command are one construct, so that one finding is reported for it. A profile may declare none.
///
/// "ONE CONSTRUCT" IS "ONE STATEMENT", DECLARED PER PROFILE, AND NOT "OVERLAPPING SPANS". Two classes can read
/// different text of one command (an option's pair and a URL's userinfo), and overlapping spans would leave those
/// as two findings. The profile therefore declares what ends a statement: the separator tokens, and the nesting
/// pairs inside which a separator is part of the enclosing statement. An unescaped line feed always ends a
/// statement and every nesting open at that point, and that is the engine's rule and not a declaration, so one
/// unmatched opener can never swallow the rest of a file. A separator, an opener or a closer counts only at a
/// position that counts as code, so the text of a string, a comment or a here-document body never splits one.
///
/// WHICH FINDING SURVIVES. Within one statement and across DIFFERENT classes, the highest level wins, then the
/// class listed first in <see cref="PatternClassesInPrecedenceOrder"/>, then the class identifier in ordinal
/// order. A class the list does not name ranks after every listed class, and the unlisted ones are told apart by
/// the ordinal, so every class takes part and the outcome never depends on how a rule set was enumerated. The
/// order of the list is a precedence the profile states about its own classes; it is never an order of
/// output, which the engine alone imposes. Two findings of the SAME class in one statement read different text
/// and both stay. The survivor keeps the winner's fields, with the literal-credential signal true if any
/// suppressed finding's was.
///
/// A class's finding takes part only where its anchor counts as code: a finding anchored in a string body or a
/// comment has no command the engine can name, so it is never merged.
/// </summary>
public sealed class ConstructPrecedence
{
	private readonly Lazy<FrozenDictionary<string, int>> _rankByPatternClass;

	public ConstructPrecedence()
	{
		_rankByPatternClass = new Lazy<FrozenDictionary<string, int>>(BuildRanks);
	}

	/// <summary>The declared classes, the most specific first.</summary>
	public required ImmutableArray<string> PatternClassesInPrecedenceOrder { get; init; }

	/// <summary>
	/// The tokens that end a statement where they stand outside every nesting, in code. The line feed is not
	/// listed: it always ends one.
	/// </summary>
	public FrozenSet<string> StatementSeparators { get; init; } = FrozenSet<string>.Empty;

	/// <summary>The pairs inside which a separator does not end the enclosing statement.</summary>
	public FrozenSet<StatementNesting> StatementNestings { get; init; } = FrozenSet<StatementNesting>.Empty;

	/// <summary>
	/// The position of a class in the declared order; a class the list does not name takes the position after the
	/// last listed one, which every unlisted class shares.
	/// </summary>
	internal int RankOf(string patternClassIdentifier)
	{
		return _rankByPatternClass.Value.TryGetValue(patternClassIdentifier, out int rank)
			? rank
			: PatternClassesInPrecedenceOrder.Length;
	}

	private FrozenDictionary<string, int> BuildRanks()
	{
		Dictionary<string, int> ranks = new(StringComparer.Ordinal);

		for (int position = 0; position < PatternClassesInPrecedenceOrder.Length; position++)
		{
			ranks.TryAdd(PatternClassesInPrecedenceOrder[position], position);
		}

		return ranks.ToFrozenDictionary(StringComparer.Ordinal);
	}
}
