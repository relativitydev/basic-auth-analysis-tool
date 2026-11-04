using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SHAPE OF THE `CSharp` COST GATE'S ADDITIONS FOR THE NEUTRAL-PAIR LEVEL RULE, checked without any clock. The gate's registry holds
/// both word expressions of `CS-009`; the stress set holds a file of many <c>+ ":" +</c> sites after long neutrally named left halves and
/// a file of many plus signs with no colon literal; and the first of them really reaches the condition (its pairs read at Low). The
/// timing itself is taken by the timing tests over the same shapes.
/// </summary>
public sealed class CSharpNeutralPairCostGateShapeTests
{
	private const string _CS_009 = "CS-009";
	private const string _COLON_LITERAL = "\":\"";
	private const string _PLUS_SIGN = "+";
	private const string _COLON_PAIR_SITE = " + \":\" + ";
	private const string _FIRST_MEMBER_END = "internal static void Member000001";
	private const int _MINIMUM_SITES = 1000;
	private const int _MINIMUM_LEFT_HALF_LENGTH = 500;
	private const int _MINIMUM_SUFFIX_PAIR_SITES = 100;
	private const int _LINE_CEILING_TOLERANCE = 200;

	[Fact]
	public void TheRegistryHoldsBothWordExpressionsOfCS009AndEachCarriesTheMatchBound()
	{
		DetectionRule rule = CSharpRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_CS_009];
		NeutralOperandPairCondition condition = rule.LowWhenBothOperandsAreNeutral!;
		ImmutableArray<string> identifiers = [.. CSharpRegisteredExpressions.All.Select(entry => entry.Identifier)];

		Assert.Contains(CSharpRegisteredExpressions.FirstOperandWordsIdentifierOf(_CS_009), identifiers);
		Assert.Contains(CSharpRegisteredExpressions.SecondOperandWordsIdentifierOf(_CS_009), identifiers);
		Assert.Contains(CSharpRegisteredExpressions.All, entry => ReferenceEquals(entry.Expression, condition.FirstOperandWordExpression));
		Assert.Contains(CSharpRegisteredExpressions.All, entry => ReferenceEquals(entry.Expression, condition.SecondOperandWordExpression));
		Assert.All(CSharpRegisteredExpressions.All, entry => Assert.Equal(RecognitionEnginePolicy.MATCH_BOUND, entry.Expression.MatchTimeout));
		Assert.Equal(CSharpRegisteredExpressions.Classes.Count + 2, CSharpRegisteredExpressions.All.Count);
	}

	[Fact]
	public void TheStressSetHoldsTheFileOfNeutralColonPairsWithLongLeftHalves()
	{
		MatchBoundStressShape shape = MatchBoundStressSet.Build().Single(candidate => candidate.Name == MatchBoundStressSet.NEUTRAL_COLON_PAIR_SHAPE_NAME);
		int sites = CountOf(shape.Text, _COLON_PAIR_SITE);
		string[] lines = shape.Text.Split('\n');
		string firstSiteLine = lines.First(line => line.Contains(_COLON_PAIR_SITE, StringComparison.Ordinal));

		Assert.True(sites >= _MINIMUM_SITES, $"{sites} sites");
		Assert.True(firstSiteLine.IndexOf(_COLON_PAIR_SITE, StringComparison.Ordinal) >= _MINIMUM_LEFT_HALF_LENGTH, "the left half before the operator is long");
		Assert.All(lines, line => Assert.True(line.Length <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS));
	}

	[Fact]
	public void TheStressSetHoldsTheFileOfManyPlusSignsWithNoColonLiteral()
	{
		MatchBoundStressShape shape = MatchBoundStressSet.Build().Single(candidate => candidate.Name == MatchBoundStressSet.PLUS_SIGNS_WITHOUT_A_COLON_LITERAL_SHAPE_NAME);

		Assert.True(CountOf(shape.Text, _PLUS_SIGN) >= _MINIMUM_SITES);
		Assert.DoesNotContain(_COLON_LITERAL, shape.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePairsOfTheNeutralColonShapeReachTheConditionAndReadAtLow()
	{
		MatchBoundStressShape shape = MatchBoundStressSet.Build().Single(candidate => candidate.Name == MatchBoundStressSet.NEUTRAL_COLON_PAIR_SHAPE_NAME);
		string firstMembers = shape.Text[..shape.Text.IndexOf(_FIRST_MEMBER_END, StringComparison.Ordinal)];

		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize($"{firstMembers}}}\n}}\n");
		ImmutableArray<RecognizedConstruct> pairs = [.. constructs.Where(construct => construct.PatternClassIdentifier == _CS_009)];

		Assert.NotEmpty(pairs);
		Assert.All(pairs, pair => Assert.Equal(ConfidenceLevel.Low, pair.Level));
		Assert.All(pairs, pair => Assert.Equal(LowConfidenceReasons.NeutralOperandPair, pair.LowReasons));
	}

	public static IEnumerable<object[]> ShapesWhoseEveryOccurrenceIsSuffixed()
	{
		return
		[
			[MatchBoundStressSet.IDENTIFIER_SUFFIX_CAMEL_RUNS_SHAPE_NAME],
			[MatchBoundStressSet.IDENTIFIER_SUFFIX_UNDERSCORE_RUNS_SHAPE_NAME],
			[MatchBoundStressSet.IDENTIFIER_SUFFIX_LINE_CEILING_SHAPE_NAME]
		];
	}

	[Fact]
	public void TheStressSetHoldsTheIdentifierSuffixShapesWithManyPairSitesAndOneLineAtTheCeiling()
	{
		ImmutableArray<MatchBoundStressShape> shapes = MatchBoundStressSet.Build();
		string[] names =
		[
			MatchBoundStressSet.IDENTIFIER_SUFFIX_CAMEL_RUNS_SHAPE_NAME,
			MatchBoundStressSet.IDENTIFIER_SUFFIX_UNDERSCORE_RUNS_SHAPE_NAME,
			MatchBoundStressSet.IDENTIFIER_SUFFIX_LAST_WORD_UNSUFFIXED_SHAPE_NAME,
			MatchBoundStressSet.IDENTIFIER_SUFFIX_ACRONYM_RUNS_SHAPE_NAME,
			MatchBoundStressSet.IDENTIFIER_SUFFIX_LINE_CEILING_SHAPE_NAME
		];

		Assert.All(names, name => Assert.Single(shapes, shape => shape.Name == name));
		Assert.All(names, name => Assert.True(CountOf(shapes.Single(shape => shape.Name == name).Text, _COLON_PAIR_SITE) >= _MINIMUM_SUFFIX_PAIR_SITES, name));
		Assert.True(
			shapes.Single(shape => shape.Name == MatchBoundStressSet.IDENTIFIER_SUFFIX_LINE_CEILING_SHAPE_NAME).Text.Split('\n').Max(line => line.Length)
				>= MatchBoundStressSet.LONG_LINE_LENGTH_IN_CHARACTERS - _LINE_CEILING_TOLERANCE);
	}

	[Theory]
	[MemberData(nameof(ShapesWhoseEveryOccurrenceIsSuffixed))]
	public void ThePairsOfAShapeWhoseEveryOccurrenceIsSuffixedReadAtLowBecauseNeitherHalfIsNamed(string shapeName)
	{
		ImmutableArray<RecognizedConstruct> pairs = PairsOfTheFirstMemberOf(shapeName);

		Assert.NotEmpty(pairs);
		Assert.All(pairs, pair => Assert.Equal(LowConfidenceReasons.NeutralOperandPair, pair.LowReasons));
	}

	[Fact]
	public void TheAcronymRunPairReadsAtLowAndTheLongSegmentPairIsNamedBecauseItsCredentialWordIsNotAtASegmentBoundary()
	{
		ImmutableArray<RecognizedConstruct> pairs = [.. PairsOfTheFirstMemberOf(MatchBoundStressSet.IDENTIFIER_SUFFIX_ACRONYM_RUNS_SHAPE_NAME).OrderBy(pair => pair.AnchorOffset)];

		Assert.Equal(2, pairs.Length);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, pairs[0].LowReasons);
		Assert.NotEqual(LowConfidenceReasons.NeutralOperandPair, pairs[1].LowReasons);
	}

	[Fact]
	public void ThePairsOfTheShapeWhoseLastWordIsUnsuffixedAreNotNeutralBecauseTheWalkEndsOnTheLastOccurrence()
	{
		ImmutableArray<RecognizedConstruct> pairs = PairsOfTheFirstMemberOf(MatchBoundStressSet.IDENTIFIER_SUFFIX_LAST_WORD_UNSUFFIXED_SHAPE_NAME);

		Assert.NotEmpty(pairs);
		Assert.All(pairs, pair => Assert.NotEqual(LowConfidenceReasons.NeutralOperandPair, pair.LowReasons));
	}

	private static ImmutableArray<RecognizedConstruct> PairsOfTheFirstMemberOf(string shapeName)
	{
		MatchBoundStressShape shape = MatchBoundStressSet.Build().Single(candidate => candidate.Name == shapeName);
		string firstMembers = shape.Text[..shape.Text.IndexOf(_FIRST_MEMBER_END, StringComparison.Ordinal)];
		ImmutableArray<RecognizedConstruct> constructs = new CSharpRecognitionHarness().Recognize($"{firstMembers}}}\n}}\n");

		return [.. constructs.Where(construct => construct.PatternClassIdentifier == _CS_009)];
	}

	private static int CountOf(string text, string fragment)
	{
		int count = 0;
		int index = text.IndexOf(fragment, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(fragment, index + fragment.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
