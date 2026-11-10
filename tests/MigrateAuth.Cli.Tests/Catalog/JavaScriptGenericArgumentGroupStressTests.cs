using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.MatchBoundStress;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE NON-TIMING STRESS CHECKS OF THE GENERIC-ARGUMENT GROUP, over the shipped expressions of `JS-001`, `JS-003`, `JS-015` and `JS-016`. Each adversarial text of
/// <see cref="JavaScriptGenericArgumentGroupStressSet"/> is read through the whole enumeration of every one of the four expressions, in the generated form and in the interpreted form built from the same pattern. What is
/// asserted carries no clock: the enumeration ends without the policy match timeout being raised, advances (no match begins before the previous one ended and the count never passes the length of the text), and
/// the two engines agree on the index and length of every match. The wall-clock proof of the same texts at the file ceiling is <see cref="MatchBoundStress.JavaScriptGenericArgumentGroupTimingTests"/>.
/// </summary>
public sealed class JavaScriptGenericArgumentGroupStressTests
{
	private const int _MINIMUM_SHAPES = 16;
	private const int _MINIMUM_ADVANCE = 1;

	private static readonly ImmutableArray<string> _FOUR_IDS = ["JS-001", "JS-003", "JS-015", "JS-016"];

	private static readonly ImmutableArray<JavaScriptGenericArgumentGroupStressSet.Shape> _SHAPES =
		JavaScriptGenericArgumentGroupStressSet.Build(JavaScriptGenericArgumentGroupStressSet.STRESS_TEXT_CHARACTERS);

	[Fact]
	public void EveryAdversarialTextEndsOnEveryOfTheFourExpressionsWithoutTheTimeoutAndTheEnginesAgree()
	{
		List<string> failures = [];

		foreach (JavaScriptGenericArgumentGroupStressSet.Shape shape in _SHAPES)
		{
			foreach (string id in _FOUR_IDS)
			{
				Regex generated = JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[id].Expression;
				Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);

				string? problem = Compare(shape.Text, generated, interpreted);

				if (problem is not null)
				{
					failures.Add($"{shape.Name} on {id}: {problem}");
				}
			}
		}

		Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
	}

	[Fact]
	public void TheStressSetHoldsTheShapesOfTheDesignAndEachTextReachesTheSizeAsked()
	{
		Assert.True(_SHAPES.Length >= _MINIMUM_SHAPES);
		Assert.All(_SHAPES, shape => Assert.True(shape.Text.Length >= JavaScriptGenericArgumentGroupStressSet.STRESS_TEXT_CHARACTERS, shape.Name));
		Assert.Equal(_SHAPES.Length, _SHAPES.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
	}

	private static string? Compare(string text, Regex generated, Regex interpreted)
	{
		string? problem = null;
		Match fromGenerated;
		Match fromInterpreted;
		int previousEnd = -1;
		int count = 0;

		try
		{
			fromGenerated = generated.Match(text);
			fromInterpreted = interpreted.Match(text);

			while (problem is null && (fromGenerated.Success || fromInterpreted.Success))
			{
				if (fromGenerated.Success != fromInterpreted.Success || fromGenerated.Index != fromInterpreted.Index || fromGenerated.Length != fromInterpreted.Length)
				{
					problem = $"the engines disagree at {fromGenerated.Index}/{fromInterpreted.Index}";
				}
				else if (fromGenerated.Index < previousEnd || ++count > text.Length)
				{
					problem = "the enumeration did not advance";
				}
				else
				{
					previousEnd = fromGenerated.Index + Math.Max(fromGenerated.Length, _MINIMUM_ADVANCE);
					fromGenerated = fromGenerated.NextMatch();
					fromInterpreted = fromInterpreted.NextMatch();
				}
			}
		}
		catch (RegexMatchTimeoutException)
		{
			problem = "the match timeout was raised";
		}

		return problem;
	}
}
