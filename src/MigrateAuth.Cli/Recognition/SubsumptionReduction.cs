using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// One classified match, before subsumption has had its say. It carries the match span, which
/// <see cref="RecognizedConstruct"/> deliberately does not: the span exists to answer a
/// subsumption condition and has no consumer after that, and carrying it further would put a
/// second position beside the anchor for a later stage to order on by mistake.
/// </summary>
internal readonly record struct ClassifiedMatch
{
	internal required string PatternClassIdentifier { get; init; }

	internal required int MatchIndex { get; init; }

	internal required int MatchLength { get; init; }

	internal required int AnchorOffset { get; init; }

	internal required ConfidenceLevel Level { get; init; }

	/// <summary>The literal-credential signal, read off the mask while the match still exists.</summary>
	internal required bool LiteralCredentialSignal { get; init; }

	/// <summary>Whether the Test demotion kind fired on the file, at any level; it travels beside the level and is never a Low reason.</summary>
	internal required bool TestCode { get; init; }

	/// <summary>Which signals proposed Low, read where the composition happens and carried beside the level.</summary>
	internal LowConfidenceReasons LowReasons { get; init; }
}

/// <summary>
/// SUBSUMPTION, EVALUATED BY THE ENGINE FROM A RELATION THE ANALYZER DECLARED.
///
/// Two rules can match the same construct - a legacy client constructed with a credential object
/// matches both the client class and the credential class. The catalog declares WHEN one class
/// subsumes another; this decides what is emitted.
///
/// ABSENT A DECLARED RELATION, BOTH ARE EMITTED.
///
/// THE ORDER OUT IS THE TOTAL KEY RESTRICTED TO ONE FILE. The total key is path, then line, then
/// column, then pattern class identifier; within a single file the first term is constant and line
/// followed by column is strictly monotone in the anchor offset, so ordering here by anchor offset
/// and then by class identifier IS that key and not an approximation of it. Sorting here rather
/// than leaving it to the stage that assigns identifiers costs nothing and means no consumer ever
/// sees this component's output in an order that depends on how a rule set enumerated.
/// </summary>
internal static class SubsumptionReduction
{
	/// <summary>
	/// The surviving constructs, ordered. Subsumption is evaluated over the whole file's matches at
	/// once, after classification: a subsumed construct is one that was recognized and classified
	/// and is then not emitted, which is a different statement from one that was never recognized.
	/// A file's matches are grouped by pattern-class identifier once, and each rule then walks only
	/// the two groups it names.
	/// </summary>
	internal static ImmutableArray<RecognizedConstruct> Reduce(
		FrozenSet<SubsumptionRule> subsumptionRelation,
		IReadOnlyList<ClassifiedMatch> classifiedMatches)
	{
		bool[] isSubsumed = new bool[classifiedMatches.Count];
		Dictionary<string, List<int>> indicesByPatternClass = GroupIndicesByPatternClass(classifiedMatches);

		foreach (SubsumptionRule subsumption in subsumptionRelation)
		{
			MarkSubsumedMatches(subsumption, classifiedMatches, indicesByPatternClass, isSubsumed);
		}

		ImmutableArray<RecognizedConstruct> surviving =
		[
			.. classifiedMatches
				.Where((classifiedMatch, index) => !isSubsumed[index])
				.Select(ToRecognizedConstruct)
				.OrderBy(construct => construct.AnchorOffset)
				.ThenBy(construct => construct.PatternClassIdentifier, StringComparer.Ordinal)
		];

		return surviving;
	}

	/// <summary>
	/// One pass over the file's matches, building the index lists every subsumption rule then reuses.
	/// A match's own index, not the match itself, is what is grouped - the list stays cheap to build
	/// and to walk, and <paramref name="classifiedMatches"/> is read once per surviving index rather
	/// than copied into a second collection.
	/// </summary>
	private static Dictionary<string, List<int>> GroupIndicesByPatternClass(IReadOnlyList<ClassifiedMatch> classifiedMatches)
	{
		Dictionary<string, List<int>> indicesByPatternClass = new(StringComparer.Ordinal);

		for (int index = 0; index < classifiedMatches.Count; index++)
		{
			string patternClassIdentifier = classifiedMatches[index].PatternClassIdentifier;

			if (!indicesByPatternClass.TryGetValue(patternClassIdentifier, out List<int>? indices))
			{
				indices = [];
				indicesByPatternClass[patternClassIdentifier] = indices;
			}

			indices.Add(index);
		}

		return indicesByPatternClass;
	}

	private static void MarkSubsumedMatches(
		SubsumptionRule subsumption,
		IReadOnlyList<ClassifiedMatch> classifiedMatches,
		Dictionary<string, List<int>> indicesByPatternClass,
		bool[] isSubsumed)
	{
		bool subsumingClassPresent = indicesByPatternClass.TryGetValue(
			subsumption.SubsumingPatternClassIdentifier,
			out List<int>? subsumingIndices);
		bool subsumedClassPresent = indicesByPatternClass.TryGetValue(
			subsumption.SubsumedPatternClassIdentifier,
			out List<int>? subsumedIndices);

		if (subsumingClassPresent && subsumedClassPresent)
		{
			MarkEveryPairTheConditionSubsumes(subsumption.When, classifiedMatches, subsumingIndices!, subsumedIndices!, isSubsumed);
		}
	}

	private static void MarkEveryPairTheConditionSubsumes(
		SubsumptionCondition condition,
		IReadOnlyList<ClassifiedMatch> classifiedMatches,
		List<int> subsumingIndices,
		List<int> subsumedIndices,
		bool[] isSubsumed)
	{
		foreach (int subsumingIndex in subsumingIndices)
		{
			foreach (int subsumedIndex in subsumedIndices)
			{
				bool subsumes =
					subsumedIndex != subsumingIndex
					&& ConditionHolds(condition, classifiedMatches[subsumingIndex], classifiedMatches[subsumedIndex]);

				if (subsumes)
				{
					isSubsumed[subsumedIndex] = true;
				}
			}
		}
	}

	private static bool ConditionHolds(
		SubsumptionCondition condition,
		ClassifiedMatch subsuming,
		ClassifiedMatch subsumed)
	{
		bool holds = condition switch
		{
			SubsumptionCondition.MatchSpansAreIdentical =>
				subsuming.MatchIndex == subsumed.MatchIndex && subsuming.MatchLength == subsumed.MatchLength,
			SubsumptionCondition.AnchorPositionsAreIdentical =>
				subsuming.AnchorOffset == subsumed.AnchorOffset,
			_ => SpansOverlap(subsuming, subsumed)
		};

		return holds;
	}

	/// <summary>
	/// Half-open intervals, so two matches that merely abut do not count as overlapping - a
	/// construct ending where the next begins is two constructs, and counting them as one would
	/// suppress a finding on the strength of adjacency alone.
	/// </summary>
	private static bool SpansOverlap(ClassifiedMatch first, ClassifiedMatch second)
	{
		return first.MatchIndex < second.MatchIndex + second.MatchLength
			&& second.MatchIndex < first.MatchIndex + first.MatchLength;
	}

	private static RecognizedConstruct ToRecognizedConstruct(ClassifiedMatch classifiedMatch)
	{
		return new RecognizedConstruct
		{
			PatternClassIdentifier = classifiedMatch.PatternClassIdentifier,
			AnchorOffset = classifiedMatch.AnchorOffset,
			Level = classifiedMatch.Level,
			LiteralCredentialSignal = classifiedMatch.LiteralCredentialSignal,
			TestCode = classifiedMatch.TestCode,
			LowReasons = classifiedMatch.LowReasons
		};
	}
}
