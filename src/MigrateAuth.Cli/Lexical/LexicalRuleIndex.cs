using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Lexical;

/// <summary>Which of the three rule collections a candidate came from.</summary>
internal enum LexicalRuleKind
{
	LineComment = 0,
	BlockComment = 1,
	Quote = 2
}

/// <summary>
/// ONE THING THAT CAN OPEN AT A POSITION, FLATTENED ACROSS THE THREE RULE COLLECTIONS.
///
/// It exists because the first engine rule - the longest opening wins - is stated over the
/// profile's rules and not over one collection of them. <see cref="LexicalProfile"/> orders each
/// of its three collections internally and cannot order them against each other, so a line
/// comment opening of two characters and a quote opening of one have no relative order until
/// something merges them. Without the merge a language whose comment opener extends its regular
/// expression delimiter reads every comment as a literal.
///
/// A LITERAL PREFIX IS A CANDIDATE IN ITS OWN RIGHT rather than something looked for behind an
/// opening already matched. The prefixed form is strictly longer than the bare one, so entering
/// it in the same ordering is what makes the longest-opening rule decide between them instead of
/// leaving the scanner to rewrite zones it has already written.
/// </summary>
internal sealed record LexicalRuleCandidate
{
	/// <summary>Which collection this came from.</summary>
	public required LexicalRuleKind Kind { get; init; }

	/// <summary>
	/// The text that must stand at the position, prefix included. It is what the ordering is by
	/// and what the bucket is keyed on; the scanner matches the parts separately, because a
	/// prefix carries its own case rule and the delimiter carries the profile's.
	/// </summary>
	public required string OpeningText { get; init; }

	/// <summary>Set exactly when <see cref="Kind"/> is <see cref="LexicalRuleKind.LineComment"/>.</summary>
	public LineCommentRule? LineComment { get; init; }

	/// <summary>Set exactly when <see cref="Kind"/> is <see cref="LexicalRuleKind.BlockComment"/>.</summary>
	public BlockCommentRule? BlockComment { get; init; }

	/// <summary>Set exactly when <see cref="Kind"/> is <see cref="LexicalRuleKind.Quote"/>.</summary>
	public QuoteRule? Quote { get; init; }

	/// <summary>The prefix this candidate carries, or null for the bare form of the same rule.</summary>
	public LiteralPrefixRule? Prefix { get; init; }
}

/// <summary>
/// THE PROFILE'S RULES, MERGED, ORDERED LONGEST OPENING FIRST, AND BUCKETED BY THE CHARACTER
/// THEY START WITH.
///
/// The bucketing keeps the cost of a scan from scaling with the number of rules in a profile:
/// without it every rule would be tried at every character.
///
/// IT RE-SORTS NOTHING THE PROFILE ALREADY ORDERED. Each of the three collections arrives in the
/// engine's own order and this type never reads an author's order, which is unreachable by
/// construction. What it adds is the relation BETWEEN the collections, which no collection can
/// carry, and it derives that from the same rule - longest opening first - with ordinal,
/// culture-invariant tie-breakers so the result is identical on every platform.
/// </summary>
internal sealed class LexicalRuleIndex
{
	/// <summary>
	/// Buckets for the ASCII range, which is where essentially every delimiter of the eight
	/// languages lives. A direct index costs one bounds check against a dictionary probe per
	/// character of every file scanned, and the character loop is the hottest loop in the tool.
	/// </summary>
	private const int _DIRECT_BUCKET_COUNT = 128;

	private static readonly ImmutableArray<LexicalRuleCandidate> _noCandidates = [];

	private readonly ImmutableArray<LexicalRuleCandidate>[] _directBuckets;
	private readonly FrozenDictionary<char, ImmutableArray<LexicalRuleCandidate>> _wideBuckets;

	internal LexicalRuleIndex(LexicalProfile profile)
	{
		Dictionary<char, List<LexicalRuleCandidate>> buckets = [];

		CollectLineComments(profile, buckets);
		CollectBlockComments(profile, buckets);
		CollectQuotes(profile, buckets);

		_directBuckets = new ImmutableArray<LexicalRuleCandidate>[_DIRECT_BUCKET_COUNT];
		Array.Fill(_directBuckets, _noCandidates);
		_wideBuckets = Freeze(buckets, _directBuckets);
	}

	/// <summary>
	/// Everything that could open at a character, longest opening first. The scanner walks this
	/// and takes the first candidate whose gates - anchoring, word boundary, context predicate -
	/// also hold, which is what lets a longer opening that fails its predicate fall through to a
	/// shorter one rather than suppress it.
	/// </summary>
	internal ImmutableArray<LexicalRuleCandidate> CandidatesFor(char openingCharacter)
	{
		ImmutableArray<LexicalRuleCandidate> candidates;

		if (openingCharacter < _DIRECT_BUCKET_COUNT)
		{
			candidates = _directBuckets[openingCharacter];
		}
		else
		{
			candidates = _wideBuckets.TryGetValue(openingCharacter, out ImmutableArray<LexicalRuleCandidate> found)
				? found
				: _noCandidates;
		}

		return candidates;
	}

	private static void CollectLineComments(
		LexicalProfile profile,
		Dictionary<char, List<LexicalRuleCandidate>> buckets)
	{
		foreach (LineCommentRule rule in profile.LineCommentRulesInEngineOrder)
		{
			Register(
				buckets,
				new LexicalRuleCandidate
				{
					Kind = LexicalRuleKind.LineComment,
					OpeningText = rule.Opening,
					LineComment = rule
				},
				SensitivityFor(profile, rule.RequiresWordBoundary));
		}
	}

	private static void CollectBlockComments(
		LexicalProfile profile,
		Dictionary<char, List<LexicalRuleCandidate>> buckets)
	{
		foreach (BlockCommentRule rule in profile.BlockCommentRulesInEngineOrder)
		{
			Register(
				buckets,
				new LexicalRuleCandidate
				{
					Kind = LexicalRuleKind.BlockComment,
					OpeningText = rule.Opening,
					BlockComment = rule
				},
				profile.DelimiterCaseSensitivity);
		}
	}

	private static void CollectQuotes(LexicalProfile profile, Dictionary<char, List<LexicalRuleCandidate>> buckets)
	{
		foreach (QuoteRule rule in profile.QuoteRulesInEngineOrder)
		{
			Register(
				buckets,
				new LexicalRuleCandidate
				{
					Kind = LexicalRuleKind.Quote,
					OpeningText = rule.Opening,
					Quote = rule
				},
				profile.DelimiterCaseSensitivity);

			foreach (LiteralPrefixRule prefix in rule.LiteralPrefixes)
			{
				Register(
					buckets,
					new LexicalRuleCandidate
					{
						Kind = LexicalRuleKind.Quote,
						OpeningText = $"{prefix.Prefix}{rule.Opening}",
						Quote = rule,
						Prefix = prefix
					},
					prefix.Sensitivity);
			}
		}
	}

	/// <summary>
	/// A COMMENT OPENER THAT IS A WORD IS A KEYWORD, AND EVERYTHING ELSE IS A DELIMITER.
	///
	/// An opener that only opens a comment where it stands as a whole word is compared under the
	/// keyword case rule, and every punctuation opener under the delimiter one.
	/// </summary>
	private static CaseSensitivity SensitivityFor(LexicalProfile profile, bool requiresWordBoundary)
	{
		return requiresWordBoundary ? profile.KeywordCaseSensitivity : profile.DelimiterCaseSensitivity;
	}

	/// <summary>
	/// Files the candidate under the character it starts with, and under BOTH cases of it where
	/// the comparison that will be applied is insensitive - otherwise a lower-case occurrence of
	/// an upper-case opener never reaches the bucket that holds it, and the bucketing would
	/// silently undo the case-sensitivity field.
	/// </summary>
	private static void Register(
		Dictionary<char, List<LexicalRuleCandidate>> buckets,
		LexicalRuleCandidate candidate,
		CaseSensitivity sensitivity)
	{
		if (candidate.OpeningText.Length > 0)
		{
			char openingCharacter = candidate.OpeningText[0];

			AddTo(buckets, openingCharacter, candidate);

			if (sensitivity is CaseSensitivity.Insensitive)
			{
				AddTo(buckets, char.ToUpperInvariant(openingCharacter), candidate);
				AddTo(buckets, char.ToLowerInvariant(openingCharacter), candidate);
			}
		}
	}

	private static void AddTo(
		Dictionary<char, List<LexicalRuleCandidate>> buckets,
		char key,
		LexicalRuleCandidate candidate)
	{
		if (!buckets.TryGetValue(key, out List<LexicalRuleCandidate>? bucket))
		{
			bucket = [];
			buckets[key] = bucket;
		}

		if (!bucket.Contains(candidate))
		{
			bucket.Add(candidate);
		}
	}

	private static FrozenDictionary<char, ImmutableArray<LexicalRuleCandidate>> Freeze(
		Dictionary<char, List<LexicalRuleCandidate>> buckets,
		ImmutableArray<LexicalRuleCandidate>[] directBuckets)
	{
		Dictionary<char, ImmutableArray<LexicalRuleCandidate>> wideBuckets = [];

		foreach (KeyValuePair<char, List<LexicalRuleCandidate>> bucket in buckets)
		{
			ImmutableArray<LexicalRuleCandidate> ordered = Order(bucket.Value);

			if (bucket.Key < _DIRECT_BUCKET_COUNT)
			{
				directBuckets[bucket.Key] = ordered;
			}
			else
			{
				wideBuckets[bucket.Key] = ordered;
			}
		}

		return wideBuckets.ToFrozenDictionary();
	}

	/// <summary>
	/// Longest opening first, with the same ordinal, culture-invariant tie-breakers
	/// <see cref="LexicalEnginePolicy"/> applies within a collection, plus the kind as the last
	/// one so that two equally long openings from different collections have a fixed order
	/// rather than one inherited from a dictionary's enumeration.
	/// </summary>
	private static ImmutableArray<LexicalRuleCandidate> Order(List<LexicalRuleCandidate> candidates)
	{
		ImmutableArray<LexicalRuleCandidate> ordered =
		[
			.. candidates
				.OrderByDescending(candidate => candidate.OpeningText.Length)
				.ThenBy(candidate => candidate.Kind)
				.ThenBy(candidate => candidate.OpeningText, StringComparer.Ordinal)
		];

		return ordered;
	}
}
