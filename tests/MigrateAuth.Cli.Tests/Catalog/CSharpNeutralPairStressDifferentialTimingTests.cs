using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.MatchBoundStress;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE GENERATED-AGAINST-INTERPRETED COMPARISON OF `CS-009` OVER THE STRESS FILES. Every shape of the `CSharp` stress set is read by the
/// shipped, source-generated expression and by the interpreted reading of the same pattern, and the two must agree on every match's
/// index and length and on the participation, index and length of the left operand, the credential and the encoder.
///
/// Timing-sensitive in cost, not in assertion: the interpreted reference over the stress shapes takes minutes, so it runs alone in the
/// timing collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class CSharpNeutralPairStressDifferentialTimingTests
{
	private readonly ITestOutputHelper _output;

	public CSharpNeutralPairStressDifferentialTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedExpressionAgreeOnEveryGroupTheConditionReadsOverEveryStressShape()
	{
		Regex generated = CSharpNeutralPairDifferential.Generated();
		Regex interpreted = CSharpNeutralPairDifferential.Interpreted(generated);
		List<string> differences = [];
		long compared = 0;
		long withALeftOperand = 0;

		foreach (MatchBoundStressShape shape in MatchBoundStressSet.Build())
		{
			CSharpNeutralPairDifferential.Outcome outcome = CSharpNeutralPairDifferential.Compare(
				$"stress/{shape.Name}",
				TextNormalization.Normalize(shape.Text),
				generated,
				interpreted);

			compared += outcome.Compared;
			withALeftOperand += outcome.WithALeftOperand;
			differences.AddRange(outcome.Differences);
		}

		_output.WriteLine($"{compared} matches compared over the stress shapes, {withALeftOperand} with a left operand, {differences.Count} differences.");
		Assert.True(compared > 0, "No match was read at all: the comparison compared nothing.");
		Assert.Equal(compared, withALeftOperand);
		Assert.True(differences.Count == 0, CSharpNeutralPairDifferential.Describe(differences));
	}

	/// <summary>The shapes that exercise the identifier-suffix walk of the condition, one case each.</summary>
	public static IEnumerable<object[]> IdentifierSuffixShapeNames()
	{
		return
		[
			[MatchBoundStressSet.IDENTIFIER_SUFFIX_CAMEL_RUNS_SHAPE_NAME],
			[MatchBoundStressSet.IDENTIFIER_SUFFIX_UNDERSCORE_RUNS_SHAPE_NAME],
			[MatchBoundStressSet.IDENTIFIER_SUFFIX_LAST_WORD_UNSUFFIXED_SHAPE_NAME],
			[MatchBoundStressSet.IDENTIFIER_SUFFIX_ACRONYM_RUNS_SHAPE_NAME],
			[MatchBoundStressSet.IDENTIFIER_SUFFIX_LINE_CEILING_SHAPE_NAME]
		];
	}

	/// <summary>
	/// THE IDENTIFIER-SUFFIX WALK JOINS THE `CSharp` COST GATE. Each shape holds, in both halves of every pair, hundreds of credential-word occurrences that the engine walks one by one through the
	/// timed door and the suffix segment scanner; the real engine reads the file on the system clock, the enumeration of every rule must terminate, no expression may reach the match bound,
	/// and the worst per-file figure stays under the bound. The worst figure is reported.
	/// </summary>
	[Theory]
	[MemberData(nameof(IdentifierSuffixShapeNames))]
	public void TheIdentifierSuffixWalkOverAStressShapeTerminatesUnderTheMatchBound(string shapeName)
	{
		MatchBoundStressShape shape = MatchBoundStressSet.Build().Single(candidate => candidate.Name == shapeName);

		MatchBoundShapeTiming timing = MatchBoundTimingHarness.Measure(new CSharpRecognitionHarness(), shape, timingTableRequested: false);

		_output.WriteLine($"{shapeName}: worst per-file expression time {timing.WorstFileMilliseconds} ms, {timing.RecognizedConstructs} constructs, bound {RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS} ms.");
		Assert.False(timing.AnExpressionTimedOut, shapeName);
		Assert.Empty(timing.NonTerminatingRules);
		Assert.NotNull(timing.WorstFileMilliseconds);
		Assert.True(timing.WorstFileMilliseconds < RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS, shapeName);
	}
}
