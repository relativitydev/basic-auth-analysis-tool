using System.Diagnostics;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.MatchBoundStress;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// The wall-clock tests of the `Shell` scanner corrections of <see cref="ShellScannerCorrectionTests"/>: the growth of the expressions and of the walk with the length of the text,
/// and the scans of many openings that must finish within a ceiling. They reuse the builders and constants of <see cref="ShellScannerCorrectionTests"/>.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class ShellScannerCorrectionTimingTests
{
	private const int _GENEROUS_GROWTH_MARGIN = 3;
	private const int _TIMING_RUNS = 7;
	private const int _UNCLOSED_LINES = 60000;
	private const int _PATHOLOGICAL_RUNS = 3;
	private const int _TICKS_FLOOR_DIVISOR = 50;
	private const int _NESTED_SUBSHELL_LINES = 20000;
	private const int _CLOSINGS_PER_NESTED_SUBSHELL_LINE = 2;
	private static readonly TimeSpan _boundedReadAheadCeiling = TimeSpan.FromSeconds(10);

	[Fact]
	public void O523TheExpressionsGrowLinearlyWithAWordThatNeverEnds()
	{
		double shortTicks = SmallestFailingMatchTicks(ShellScannerCorrectionTests.SHORT_WORD_PARTS);
		double longTicks = SmallestFailingMatchTicks(ShellScannerCorrectionTests.SHORT_WORD_PARTS * ShellScannerCorrectionTests.LENGTH_FACTOR);

		// Growth in proportion to the length is a ratio near the length factor and quadratic growth one near its square; the bound
		// sits between them with room for the timer's noise, and each side is the smallest of several runs.
		Assert.True(
			longTicks <= Math.Max(shortTicks, Stopwatch.Frequency / _TICKS_FLOOR_DIVISOR) * ShellScannerCorrectionTests.LENGTH_FACTOR * _GENEROUS_GROWTH_MARGIN,
			$"a word {ShellScannerCorrectionTests.LENGTH_FACTOR} times longer took {longTicks / shortTicks:F1} times as long");
	}

	[Fact]
	public void O523ManyOpeningsOnOneContinuedCommandAreReadWithOneWalkOfItsLogicalLine()
	{
		const string openingWithoutAWord = "<<\\\n";
		string source = $"cat {string.Concat(Enumerable.Repeat(openingWithoutAWord, _UNCLOSED_LINES))};\nafter=1\n";
		Stopwatch stopwatch = Stopwatch.StartNew();

		ZoneMask mask = ShellShapedProfile.Scan(source);

		stopwatch.Stop();
		ZoneAssertions.SpansZone(mask, source, ShellScannerCorrectionTests.FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf(openingWithoutAWord, StringComparison.Ordinal)));
		Assert.True(mask.IsCompletelyValid);
		Assert.True(stopwatch.Elapsed < _boundedReadAheadCeiling, $"the scan took {stopwatch.Elapsed.TotalSeconds:F1} s");
	}

	[Fact]
	public void O524ManyUnclosedDoubleParenthesesDoNotHideTheArithmeticThatFollowsThem()
	{
		string source = $"{string.Concat(Enumerable.Repeat("((x\n", _UNCLOSED_LINES))}echo $(( 1 +\n 2 << 3 ))\ncat <<EOF\nbody\nEOF\nafter=1\n";
		Stopwatch stopwatch = Stopwatch.StartNew();

		ZoneMask mask = ShellShapedProfile.Scan(source);

		stopwatch.Stop();
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 3", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
		Assert.True(stopwatch.Elapsed < _boundedReadAheadCeiling, $"the scan took {stopwatch.Elapsed.TotalSeconds:F1} s");
	}

	[Fact]
	public void O524ManyDoubleParenthesesThatCloseAsNestedSubshellsAreReadOnceAndDoNotHideTheArithmeticThatFollowsThem()
	{
		string source = $"{string.Concat(Enumerable.Repeat("((x\n", _NESTED_SUBSHELL_LINES))}{string.Concat(Enumerable.Repeat(")x", (_NESTED_SUBSHELL_LINES * _CLOSINGS_PER_NESTED_SUBSHELL_LINE) + 1))}\necho $(( 1 +\n 2 << 3 ))\ncat <<EOF\nbody\nEOF\nafter=1\n";
		Stopwatch stopwatch = Stopwatch.StartNew();

		ZoneMask mask = ShellShapedProfile.Scan(source);

		stopwatch.Stop();
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 3", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		Assert.True(mask.IsCompletelyValid);
		Assert.True(stopwatch.Elapsed < _boundedReadAheadCeiling, $"the scan took {stopwatch.Elapsed.TotalSeconds:F1} s");
	}

	[Theory]
	[InlineData(ShellScannerCorrectionTests.PathologicalShape.RunsOfOpeningBrackets)]
	[InlineData(ShellScannerCorrectionTests.PathologicalShape.BlanksBeforeAKeyword)]
	[InlineData(ShellScannerCorrectionTests.PathologicalShape.OneLongWordBeforeAKeyword)]
	[InlineData(ShellScannerCorrectionTests.PathologicalShape.ALongChainOfLicensingWords)]
	[InlineData(ShellScannerCorrectionTests.PathologicalShape.ManyUnclosedDoubleParentheses)]
	public void O524TheWalkGrowsInProportionToTheTextOnEveryShapeThatCouldMakeItQuadratic(ShellScannerCorrectionTests.PathologicalShape shape)
	{
		double shortTicks = SmallestScanTicks(ShellScannerCorrectionTests.BuildThePathologicalText(shape, ShellScannerCorrectionTests.PATHOLOGICAL_UNITS));
		double longTicks = SmallestScanTicks(ShellScannerCorrectionTests.BuildThePathologicalText(shape, ShellScannerCorrectionTests.PATHOLOGICAL_UNITS * ShellScannerCorrectionTests.LENGTH_FACTOR));

		Assert.True(
			longTicks <= Math.Max(shortTicks, Stopwatch.Frequency / _TICKS_FLOOR_DIVISOR) * ShellScannerCorrectionTests.LENGTH_FACTOR * _GENEROUS_GROWTH_MARGIN,
			$"{shape}: a text {ShellScannerCorrectionTests.LENGTH_FACTOR} times longer took {longTicks / shortTicks:F1} times as long");
	}

	private static double SmallestFailingMatchTicks(int parts)
	{
		string text = ShellScannerCorrectionTests.BuildAWordThatNeverEnds(parts);
		Regex generated = ShellShapedProfile.PlainOpening();
		long smallest = long.MaxValue;

		for (int run = 0; run < _TIMING_RUNS; run++)
		{
			long started = Stopwatch.GetTimestamp();

			_ = generated.Match(text, ShellScannerCorrectionTests.OPENING_START);
			smallest = Math.Min(smallest, Stopwatch.GetTimestamp() - started);
		}

		return Math.Max(smallest, 1);
	}

	private static double SmallestScanTicks(string source)
	{
		long smallest = long.MaxValue;

		for (int run = 0; run < _PATHOLOGICAL_RUNS; run++)
		{
			long started = Stopwatch.GetTimestamp();

			_ = ShellShapedProfile.Scan(source);
			smallest = Math.Min(smallest, Stopwatch.GetTimestamp() - started);
		}

		return Math.Max(smallest, 1);
	}
}
