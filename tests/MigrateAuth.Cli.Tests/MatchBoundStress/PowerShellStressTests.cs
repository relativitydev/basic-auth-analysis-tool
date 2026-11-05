using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE `PowerShell` ANALYZER AGAINST THE MATCH BOUND AND THE LEXICAL BUDGET, on the shapes of <see cref="PowerShellMatchBoundStressSet"/>.
///
/// WHAT THIS ASSERTS. That every shape respects the reader's ceilings; that the lexical scan of each shape finishes inside the lexical budget
/// with no anomaly the shape does not carry; that no detection rule's own enumeration stalls or repeats a match (the source generator's one known defect);
/// that no expression's own timeout fires; that the whole-file recognition of each shape stops short of the match bound and is never reported as stopped by
/// it; that every flow signal terminates and agrees with its interpreted form; and that the source-generated and the interpreted form of every
/// detection rule and flow signal agree on a slice of every shape. The multiple, the figures and the bound they would derive belong to the match-bound timing tests, not
/// to this test: a test that failed on a fast machine for a statement about a slower one would assert nothing.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class PowerShellStressTests
{
	/// <summary>The interpreted form is slower, so the agreement is checked on this many characters of every shape, cut at a line end.</summary>
	private const int AGREEMENT_SLICE_IN_CHARACTERS = 160_000;

	private const string _BINARY_LIKE_SHAPE_MARKER = "binary-like";
	private const int _MATCH_ENUMERATION_CAP = 2_000_000;

	private readonly ITestOutputHelper _output;

	public PowerShellStressTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void EveryStressShapeRespectsTheReaderCeilingsSoTheEngineWouldReceiveIt()
	{
		ImmutableArray<MatchBoundStressShape> shapes = PowerShellMatchBoundStressSet.Build();

		Assert.NotEmpty(shapes);
		Assert.All(shapes, shape => Assert.True(
			Encoding.UTF8.GetByteCount(shape.Text) < ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES,
			shape.Name));
		Assert.All(shapes, shape => Assert.True(
			shape.Text.Split('\n').Max(line => line.Length) <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS,
			shape.Name));
	}

	[Fact]
	public void TheLexicalScanOfEveryShapeFinishesInsideTheBudgetAndWithNoAnomaly()
	{
		PowerShellRecognitionHarness harness = new();

		foreach (MatchBoundStressShape shape in PowerShellMatchBoundStressSet.Build())
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			FileRecognitionContext context = harness.ContextFor(shape.Text, StressPath(shape));
			stopwatch.Stop();

			_output.WriteLine($"{shape.Name}: lexical scan {stopwatch.Elapsed.TotalMilliseconds:F1} ms, anomaly {context.Mask.AnomalyReason}, valid through {context.Mask.ValidThroughPosition} of {context.Mask.Length}");

			Assert.NotEqual(LexicalAnomalyReason.LexicalBudgetExceeded, context.Mask.AnomalyReason);
			Assert.True(
				context.Mask.IsCompletelyValid || shape.Name.Contains(_BINARY_LIKE_SHAPE_MARKER, StringComparison.Ordinal),
				shape.Name);
		}
	}

	[Fact]
	public void NoDetectionRuleStallsOrExpiresOnAnyShapeAndTheWholeFileStopsShortOfTheBound()
	{
		PowerShellRecognitionHarness harness = new();

		foreach (MatchBoundStressShape shape in PowerShellMatchBoundStressSet.Build())
		{
			FileRecognitionContext context = harness.ContextFor(shape.Text, StressPath(shape));

			Assert.All(
				harness.Analyzer.DetectionRulesByPatternClass,
				entry => Assert.True(
					MatchBoundTimingHarness.EnumerationAdvances(entry.Value.Expression, context.NormalizedText),
					shape.Name + "/" + entry.Key));

			RecordingEmissionSink sink = new();
			RecognitionEngine engine = new(new EmissionPoint([sink]), TimeProvider.System);
			Stopwatch stopwatch = Stopwatch.StartNew();
			int recognized = engine.Recognize(context).Constructs.Length;
			stopwatch.Stop();

			_output.WriteLine($"{shape.Name}: {recognized} constructs, analyzer expressions {engine.LastFileMatchingTimeForDiagnostics.TotalMilliseconds:F1} ms, whole file {stopwatch.Elapsed.TotalMilliseconds:F1} ms");

			Assert.DoesNotContain(sink.Received, fact => fact.SkippedItem is SkippedItemCategory.MatchBoundExceeded);
			Assert.True(
				engine.LastFileMatchingTimeForDiagnostics < RecognitionEnginePolicy.MATCH_BOUND,
				shape.Name);
		}
	}

	[Fact]
	public void EveryFlowSignalTerminatesAndNoShapeTripsTheFlowEvaluation()
	{
		PowerShellRecognitionHarness harness = new();

		foreach (MatchBoundStressShape shape in PowerShellMatchBoundStressSet.Build())
		{
			FileRecognitionContext context = harness.ContextFor(shape.Text, StressPath(shape));

			Assert.All(
				harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals,
				signal => Assert.True(
					MatchBoundTimingHarness.EnumerationAdvances(signal.Expression, context.NormalizedText),
					shape.Name + "/" + signal.Name));

			RecognitionEngine engine = new(harness.EmissionPoint, TimeProvider.System);
			engine.Recognize(context);

			Assert.Equal(0, engine.FlowSignalTripCount);

			if (shape.Name.StartsWith(PowerShellMatchBoundStressSet.FLOW_SHAPE_NAME_PREFIX, StringComparison.Ordinal))
			{
				Assert.True(engine.FilesEvaluatedForFlowSignals == 1, shape.Name);
			}
		}
	}

	[Fact]
	public void TheGeneratedAndTheInterpretedFormOfEveryExpressionAgreeOnASliceOfEveryShape()
	{
		PowerShellRecognitionHarness harness = new();
		ImmutableArray<(string Name, Regex Expression)> expressions =
		[
			.. harness.Analyzer.DetectionRulesByPatternClass.Select(entry => (entry.Key, entry.Value.Expression)),
			.. harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals.Select(signal => (signal.Name, signal.Expression)),
			.. harness.Analyzer.DemotionSignals.ContentMarkers.Select((marker, index) => ("ContentMarker" + index, marker.Expression)),
			("ConstantDeclaration", harness.Analyzer.LexicalProfile.ConstantDeclarationExpression)
		];

		foreach (MatchBoundStressShape shape in PowerShellMatchBoundStressSet.Build())
		{
			string slice = SliceAtALineEnd(TextNormalization.Normalize(shape.Text));

			foreach ((string name, Regex generated) in expressions)
			{
				Regex interpreted = new(generated.ToString(), generated.Options, Regex.InfiniteMatchTimeout);

				ImmutableArray<(int Index, int Length, string Groups)> expected = Summarize(interpreted, slice);
				ImmutableArray<(int Index, int Length, string Groups)> actual = Summarize(generated, slice);

				Assert.True(
					expected.SequenceEqual(actual),
					$"{shape.Name}/{name}: interpreted {expected.Length} matches, generated {actual.Length}; first difference at {FirstDifference(expected, actual)}");
				_output.WriteLine($"{shape.Name}/{name}: agreed");
			}
		}
	}

	private static string FirstDifference(
		ImmutableArray<(int Index, int Length, string Groups)> expected,
		ImmutableArray<(int Index, int Length, string Groups)> actual)
	{
		int shared = Math.Min(expected.Length, actual.Length);
		int position = 0;

		while (position < shared && expected[position] == actual[position])
		{
			position++;
		}

		return position < shared
			? $"match {position}: interpreted {expected[position]}, generated {actual[position]}"
			: $"match {position}: one side ends";
	}

	private static string StressPath(MatchBoundStressShape shape)
	{
		return PowerShellMatchBoundStressSet.RelativePathOf(shape);
	}

	private static string SliceAtALineEnd(string text)
	{
		int cut = text.Length <= AGREEMENT_SLICE_IN_CHARACTERS ? text.Length : text.IndexOf('\n', AGREEMENT_SLICE_IN_CHARACTERS) + 1;

		return cut <= 0 ? text : text[..cut];
	}

	private static ImmutableArray<(int Index, int Length, string Groups)> Summarize(Regex expression, string text)
	{
		ImmutableArray<(int Index, int Length, string Groups)>.Builder matches = ImmutableArray.CreateBuilder<(int, int, string)>();
		Match match = expression.Match(text);

		while (match.Success && matches.Count < _MATCH_ENUMERATION_CAP)
		{
			matches.Add((match.Index, match.Length, DescribeGroups(match)));
			match = match.NextMatch();
		}

		Assert.True(matches.Count < _MATCH_ENUMERATION_CAP, "the enumeration did not terminate within the cap");

		return matches.ToImmutable();
	}

	private static string DescribeGroups(Match match)
	{
		return string.Join(
			";",
			match.Groups.Cast<Group>().Where(group => group.Success && !char.IsDigit(group.Name[0])).Select(group => $"{group.Name}@{group.Index}+{group.Length}"));
	}
}
