using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE DIFFERENTIAL CHECK OF THE DRAFT `Python` EXPRESSIONS OVER THE STRESS SHAPES. The source-generated form of an expression and the interpreted form must report the same
/// matches over a benign shape, not only over the semantic samples: a look-ahead or a look-behind that follows a backtracking group can make the compiled and generated engines disagree with
/// the interpreter, and the semantic samples are too short to show it. The guarded argument window, the nested bracket groups and the look-behinds that stand behind a forward guard are the constructs the shapes must show agree. Every shape
/// is cut at a line boundary at <see cref="PREFIX_LIMIT_IN_CHARACTERS"/> so that the interpreter stays fast; the cut keeps every window family of the shape in its text.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time (the interpreted form runs under a bound of its own), so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// It is written and compiled with the suite and run apart, in a separate timing run.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class PythonDraftExpressionDifferentialTimingTests
{
	internal const int PREFIX_LIMIT_IN_CHARACTERS = 60_000;

	private const int _INTERPRETED_TIMEOUT_IN_SECONDS = 60;

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryPythonDraftExpressionReportTheSameMatchesOnEveryStressShape()
	{
		List<string> disagreements = [];

		foreach (MatchBoundStressShape shape in PythonDraftCostGateStressSet.Build())
		{
			string text = PythonDraftCostGateStressSet.PrefixAtALineBoundary(shape.Text, PREFIX_LIMIT_IN_CHARACTERS);

			foreach ((string identifier, Regex generated) in PythonDraftExpressionSet.MatchBoundExpressions)
			{
				Regex interpreted = new(generated.ToString(), generated.Options, TimeSpan.FromSeconds(_INTERPRETED_TIMEOUT_IN_SECONDS));

				if (!MatchesOf(generated, text).SequenceEqual(MatchesOf(interpreted, text)))
				{
					disagreements.Add($"{identifier} on {shape.Name}");
				}
			}
		}

		Assert.Empty(disagreements);
	}

	private static ImmutableArray<(int Index, int Length)> MatchesOf(Regex expression, string text)
	{
		ImmutableArray<(int Index, int Length)>.Builder matches = ImmutableArray.CreateBuilder<(int Index, int Length)>();

		for (Match match = expression.Match(text); match.Success; match = match.NextMatch())
		{
			matches.Add((match.Index, match.Length));
		}

		return matches.ToImmutable();
	}
}
