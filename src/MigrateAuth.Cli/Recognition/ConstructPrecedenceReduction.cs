using System.Buffers;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// ONE FINDING PER CONSTRUCT, EVALUATED BY THE ENGINE FROM A DECLARATION THE ANALYZER MADE
/// (<see cref="ConstructPrecedence"/>), AFTER SUBSUMPTION.
///
/// A sort-and-sweep. The constructs that take part are put in anchor order, the statement boundaries are found
/// in one pass over the code of the text, and one sweep over both gives every participant its statement: the sort
/// is O(n log n), the boundary pass is linear in the text, and the sweep is linear in the constructs, so no step
/// compares one construct with every other. Nothing in <c>Findings/</c> is read or changed: what leaves here is
/// a shorter list of the same <see cref="RecognizedConstruct"/> values, in the order they came in.
///
/// SUBSUMPTION ANSWERS "THE SAME TEXT", THIS ANSWERS "THE SAME STATEMENT". Subsumption is a relation between two
/// classes whose spans meet; this is a choice among classes that read one statement whatever text each read.
/// Absent a declaration the input is returned unchanged.
/// </summary>
internal static class ConstructPrecedenceReduction
{
	private const char _LINE_FEED = '\n';

	/// <summary>
	/// The constructs that survive, in the order they were given, with the winner of each statement carrying the
	/// literal-credential signal of every finding it suppressed.
	/// </summary>
	internal static ImmutableArray<RecognizedConstruct> Apply(
		ConstructPrecedence? precedence,
		char? codeEscapeCharacter,
		string normalizedText,
		ZoneMask mask,
		ImmutableArray<RecognizedConstruct> constructs)
	{
		ImmutableArray<RecognizedConstruct> result = constructs;

		if (precedence is not null)
		{
			int[] participants = SelectParticipantsInAnchorOrder(precedence, mask, constructs);

			if (participants.Length > 1)
			{
				CodeText code = new(normalizedText, mask, codeEscapeCharacter);

				result = ReduceParticipants(precedence, code, constructs, participants);
			}
		}

		return result;
	}

	/// <summary>
	/// The indices of the constructs whose anchor counts as code, ordered by
	/// anchor and then by their place in the input, so the order never depends on how the sort treats a tie.
	/// </summary>
	private static int[] SelectParticipantsInAnchorOrder(
		ConstructPrecedence precedence,
		ZoneMask mask,
		ImmutableArray<RecognizedConstruct> constructs)
	{
		List<int> participants = [];

		for (int index = 0; index < constructs.Length; index++)
		{
			RecognizedConstruct construct = constructs[index];
			bool takesPart =
				construct.AnchorOffset >= 0
				&& construct.AnchorOffset < mask.Length
				&& LexicalZones.CountsAsCode(mask.ZoneAt(construct.AnchorOffset));

			if (takesPart)
			{
				participants.Add(index);
			}
		}

		int[] ordered = [.. participants];
		Array.Sort(ordered, (left, right) => CompareByAnchorThenInputPosition(constructs, left, right));

		return ordered;
	}

	private static int CompareByAnchorThenInputPosition(ImmutableArray<RecognizedConstruct> constructs, int left, int right)
	{
		int byAnchor = constructs[left].AnchorOffset.CompareTo(constructs[right].AnchorOffset);

		return byAnchor != 0 ? byAnchor : left.CompareTo(right);
	}

	private static ImmutableArray<RecognizedConstruct> ReduceParticipants(
		ConstructPrecedence precedence,
		CodeText code,
		ImmutableArray<RecognizedConstruct> constructs,
		int[] participants)
	{
		int lastAnchor = constructs[participants[^1]].AnchorOffset;
		List<int> boundaries = StatementBoundaries.Find(precedence, code, lastAnchor);
		RecognizedConstruct[] reduced = [.. constructs];
		bool[] suppressed = new bool[constructs.Length];

		int groupStart = 0;

		while (groupStart < participants.Length)
		{
			int statement = StatementBoundaries.StatementOf(boundaries, constructs[participants[groupStart]].AnchorOffset);
			int groupEnd = groupStart + 1;

			while (groupEnd < participants.Length
				&& StatementBoundaries.StatementOf(boundaries, constructs[participants[groupEnd]].AnchorOffset) == statement)
			{
				groupEnd++;
			}

			ReduceStatement(precedence, constructs, participants.AsSpan(groupStart, groupEnd - groupStart), reduced, suppressed);
			groupStart = groupEnd;
		}

		ImmutableArray<RecognizedConstruct> survivors =
		[
			.. reduced.Where((_, index) => !suppressed[index])
		];

		return survivors;
	}

	/// <summary>
	/// One statement's participants, in anchor order. The winner is the best by level, then declared rank, then class
	/// identifier in ordinal order, and the first of equals in anchor order; every participant of a different class is
	/// suppressed, and its literal-credential signal is carried into the winner.
	/// </summary>
	private static void ReduceStatement(
		ConstructPrecedence precedence,
		ImmutableArray<RecognizedConstruct> constructs,
		ReadOnlySpan<int> statementParticipants,
		RecognizedConstruct[] reduced,
		bool[] suppressed)
	{
		int winner = statementParticipants[0];

		foreach (int candidate in statementParticipants[1..])
		{
			if (Outranks(precedence, constructs[candidate], constructs[winner]))
			{
				winner = candidate;
			}
		}

		bool literalCredentialSignal = constructs[winner].LiteralCredentialSignal;

		foreach (int participant in statementParticipants)
		{
			bool differentClass = !string.Equals(
				constructs[participant].PatternClassIdentifier,
				constructs[winner].PatternClassIdentifier,
				StringComparison.Ordinal);

			if (differentClass)
			{
				suppressed[participant] = true;
				literalCredentialSignal |= constructs[participant].LiteralCredentialSignal;
			}
		}

		reduced[winner] = constructs[winner] with { LiteralCredentialSignal = literalCredentialSignal };
	}

	private static bool Outranks(ConstructPrecedence precedence, RecognizedConstruct candidate, RecognizedConstruct incumbent)
	{
		bool outranks;

		if (candidate.Level != incumbent.Level)
		{
			outranks = candidate.Level > incumbent.Level;
		}
		else
		{
			int candidateRank = precedence.RankOf(candidate.PatternClassIdentifier);
			int incumbentRank = precedence.RankOf(incumbent.PatternClassIdentifier);

			outranks = candidateRank != incumbentRank
				? candidateRank < incumbentRank
				: string.CompareOrdinal(candidate.PatternClassIdentifier, incumbent.PatternClassIdentifier) < 0;
		}

		return outranks;
	}

	/// <summary>
	/// THE STATEMENT BOUNDARIES OF THE CODE, FOUND IN ONE PASS. A boundary is the offset of an unescaped line feed or
	/// of a declared separator standing outside every nesting; a nesting is counted from a declared opener to a
	/// declared closer, never goes below zero, and is closed by the next unescaped line feed whatever its depth.
	/// Only a position that counts as code is read, and a token preceded by an odd run of the profile's code
	/// escape character is escaped and read as text.
	/// </summary>
	private static class StatementBoundaries
	{
		internal static List<int> Find(ConstructPrecedence precedence, CodeText code, int lastOffset)
		{
			string text = code.Text;
			ZoneMask mask = code.Mask;
			char? codeEscapeCharacter = code.EscapeCharacter;
			TokenTables tables = TokenTables.Build(precedence);
			List<int> boundaries = [];
			int depth = 0;
			int position = 0;
			int end = Math.Min(lastOffset + 1, text.Length);

			while (position < end)
			{
				int next = text.AsSpan(position, end - position).IndexOfAny(tables.FirstCharacters);

				if (next < 0)
				{
					break;
				}

				position += next;
				int consumed = 1;

				if (LexicalZones.CountsAsCode(mask.ZoneAt(position)) && !IsEscaped(text, position, codeEscapeCharacter))
				{
					consumed = ReadToken(tables, text, position, ref depth, boundaries);
				}

				position += consumed;
			}

			return boundaries;
		}

		/// <summary>
		/// The number of boundaries at or before the offset: the statement the offset lies in. A binary search, so the
		/// sweep over the participants costs a logarithm per participant at worst.
		/// </summary>
		internal static int StatementOf(List<int> boundaries, int offset)
		{
			int low = 0;
			int high = boundaries.Count;

			while (low < high)
			{
				int middle = low + ((high - low) / 2);

				if (boundaries[middle] <= offset)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			return low;
		}

		private static int ReadToken(TokenTables tables, string text, int position, ref int depth, List<int> boundaries)
		{
			int consumed = 1;

			if (text[position] == _LINE_FEED)
			{
				boundaries.Add(position);
				depth = 0;
			}
			else if (TryMatch(tables.Openers, text, position, out int openerLength))
			{
				depth++;
				consumed = openerLength;
			}
			else if (TryMatch(tables.Closers, text, position, out int closerLength))
			{
				depth = Math.Max(0, depth - 1);
				consumed = closerLength;
			}
			else if (depth == 0 && TryMatch(tables.Separators, text, position, out int separatorLength))
			{
				boundaries.Add(position);
				consumed = separatorLength;
			}

			return consumed;
		}

		private static bool TryMatch(string[] tokensLongestFirst, string text, int position, out int length)
		{
			length = 0;

			foreach (string token in tokensLongestFirst)
			{
				if (text.AsSpan(position).StartsWith(token, StringComparison.Ordinal))
				{
					length = token.Length;
					break;
				}
			}

			return length > 0;
		}

		private static bool IsEscaped(string text, int position, char? codeEscapeCharacter)
		{
			int run = 0;

			if (codeEscapeCharacter is char escape)
			{
				int cursor = position - 1;

				while (cursor >= 0 && text[cursor] == escape)
				{
					run++;
					cursor--;
				}
			}

			return run % 2 == 1;
		}
	}

	/// <summary>The text of one file, the mask that says what is code in it and the profile's code escape character, carried together.</summary>
	private readonly record struct CodeText(string Text, ZoneMask Mask, char? EscapeCharacter);

	/// <summary>The declared tokens, longest first so a longer token is never hidden by its own prefix, and the characters they start with.</summary>
	private sealed class TokenTables
	{
		private TokenTables(string[] openers, string[] closers, string[] separators, SearchValues<char> firstCharacters)
		{
			Openers = openers;
			Closers = closers;
			Separators = separators;
			FirstCharacters = firstCharacters;
		}

		internal string[] Openers { get; }

		internal string[] Closers { get; }

		internal string[] Separators { get; }

		internal SearchValues<char> FirstCharacters { get; }

		internal static TokenTables Build(ConstructPrecedence precedence)
		{
			string[] openers = LongestFirst(precedence.StatementNestings.Select(nesting => nesting.Opening));
			string[] closers = LongestFirst(precedence.StatementNestings.Select(nesting => nesting.Closing));
			string[] separators = LongestFirst(precedence.StatementSeparators);
			char[] first = [_LINE_FEED, .. openers.Concat(closers).Concat(separators).Select(token => token[0])];

			return new TokenTables(openers, closers, separators, SearchValues.Create(first));
		}

		private static string[] LongestFirst(IEnumerable<string> tokens)
		{
			return
			[
				.. tokens
					.Distinct(StringComparer.Ordinal)
					.OrderByDescending(token => token.Length)
					.ThenBy(token => token, StringComparer.Ordinal)
			];
		}
	}
}
