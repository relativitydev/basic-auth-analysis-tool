using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE DIFFERENTIAL CHECK OF THE `Shell` DRAFT EXPRESSIONS OVER THE STRESS SHAPES. The source-generated form of an expression and the interpreted form must report the same
/// matches over a benign shape, not only over the semantic samples: a look-ahead that follows a backtracking group can make the compiled and generated engines disagree with
/// the interpreter, and the semantic samples are too short to show it. Every shape is cut at a line boundary at <see cref="PREFIX_LIMIT_IN_CHARACTERS"/> so that the interpreter
/// stays fast; the cut keeps every window family of the shape in its text.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class ShellDraftExpressionDifferentialTimingTests
{
	internal const int PREFIX_LIMIT_IN_CHARACTERS = 150_000;

	private const char _LINE_FEED = '\n';
	private const int _INTERPRETED_TIMEOUT_IN_SECONDS = 60;

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryDraftExpressionReportTheSameMatchesOnEveryStressShape()
	{
		List<string> disagreements = [];

		foreach (MatchBoundStressShape shape in ShellMatchBoundStressSet.Build())
		{
			string text = PrefixAtALineBoundary(shape.Text);

			foreach ((string identifier, Regex generated) in ShellDraftExpressions.MatchBoundExpressions)
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

	private static string PrefixAtALineBoundary(string text)
	{
		string prefix = text;

		if (text.Length > PREFIX_LIMIT_IN_CHARACTERS)
		{
			prefix = text[..(text.LastIndexOf(_LINE_FEED, PREFIX_LIMIT_IN_CHARACTERS) + 1)];
		}

		return prefix;
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
