using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE TWO BASE64 CLASSES ON THE LONG-LINE SHAPES THAT EXERCISE THEIR WINDOWS: lines at the line-length ceiling that repeat a statement the classes' windows
/// open at, and never close - the array assignment of one shape and the encoder call of another. The bound this asserts is the figure below which a shape needs no
/// declared trip - a tenth of the match bound - on any machine this suite runs on; the match-bound timing figures, not this test, say how much faster they are.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class PowerShellBase64WindowStressTimingTests
{
	private const double _BOUND_PER_FILE_IN_MILLISECONDS = 2_875;
	private const string _RECORDED_SHAPE_NAME = "ps-long-lines-at-line-ceiling";
	private const string _ARRAY_ASSIGNMENT_SHAPE_NAME = "ps-long-line-array-assignments";
	private const string _ENCODER_CALL_SHAPE_NAME = "ps-long-line-encoder-calls-followed-by-values";
	private const string _BINARY_LIKE_SHAPE_NAME = "ps-binary-like-extensionless";
	private const string _PAIR_CLASS = "PS-010";
	private const string _TOKEN_CLASS = "PS-011";

	private readonly ITestOutputHelper _output;

	public PowerShellBase64WindowStressTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Theory]
	[InlineData(_RECORDED_SHAPE_NAME)]
	[InlineData(_ARRAY_ASSIGNMENT_SHAPE_NAME)]
	[InlineData(_ENCODER_CALL_SHAPE_NAME)]
	[InlineData(_BINARY_LIKE_SHAPE_NAME)]
	public void ThePairClassAndTheTokenClassFinishWellInsideTheBoundOnTheShapesOfTheirOwnWindows(string shapeName)
	{
		MatchBoundStressShape shape = PowerShellMatchBoundStressSet.Build().Single(candidate => candidate.Name == shapeName);
		PowerShellRecognitionHarness harness = new();
		string text = harness.ContextFor(shape.Text, PowerShellMatchBoundStressSet.RelativePathOf(shape)).NormalizedText;

		foreach (string patternClass in new[] { _PAIR_CLASS, _TOKEN_CLASS })
		{
			Regex expression = harness.Analyzer.DetectionRulesByPatternClass[patternClass].Expression;
			Stopwatch watch = Stopwatch.StartNew();
			int matches = expression.Matches(text).Count;

			watch.Stop();
			_output.WriteLine($"{shapeName}/{patternClass}: {watch.Elapsed.TotalMilliseconds:F1} ms, {matches} matches");

			Assert.Equal(0, matches);
			Assert.True(watch.Elapsed.TotalMilliseconds < _BOUND_PER_FILE_IN_MILLISECONDS, $"{shapeName}/{patternClass}");
		}
	}

	[Fact]
	public void TheEightLongLineShapesAreBenignLinesBetweenNineAndTenThousandCharactersAfterAnAnchorLine()
	{
		const int MINIMUM_LINE_LENGTH = 9_000;
		const string ANCHOR_LINE = "$encoded = [Convert]::ToBase64String($bytes)";
		const string SHAPE_NAME_PREFIX = "ps-long-line-";
		ImmutableArray<MatchBoundStressShape> shapes = [.. PowerShellMatchBoundStressSet.Build().Where(shape => shape.Name.StartsWith(SHAPE_NAME_PREFIX, StringComparison.Ordinal))];

		Assert.Equal(8, shapes.Length);
		Assert.All(shapes, shape =>
		{
			string[] lines = shape.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

			Assert.Equal(ANCHOR_LINE, lines[0]);
			Assert.All(lines.Skip(1), line => Assert.InRange(line.Length, MINIMUM_LINE_LENGTH, ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - 1));
		});
	}
}
