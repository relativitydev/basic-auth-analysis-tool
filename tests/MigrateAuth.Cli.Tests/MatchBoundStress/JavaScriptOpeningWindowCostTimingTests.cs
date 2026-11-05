using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE COST OF THE OPENING WINDOW, MEASURED AND NOT BOUNDED. <c>LexicalScanner.EndOfTheOpeningWindow</c> matches a captured-terminator opening in the rest of the
/// logical line that holds it plus ONE LINE-LENGTH CEILING beyond (a terminator word quoted across a raw line feed ends on a later line). The `JavaScript` profile declares no captured terminator, so the
/// window is never evaluated on a `JavaScript` file; the shipped profile that declares one is the `Shell` profile, and the stress shapes of the `Shell` set (the language that declares one) are the ones measured here, and the `JavaScript` side is the structural fact asserted by the first test.
///
/// WHAT IS MEASURED. For every stress shape of the `Shell` set and three shapes of this file that leave the opening's terminator word unclosed the shipped `Shell` profile's mask is built once, and at
/// every position of a captured-terminator rule's opening that stands in code (or is the delimiter of an opening that matched) the rule's OWN opening expression is matched twice, as the scanner matches it:
/// within the logical line alone, and within the logical line plus one line-length ceiling. The difference of the two sums is the cost of the extra look-ahead. A sum is stopped when it reaches the lexical budget, and says so. The scanner itself is
/// NOT changed and not run differently: the real scan of the shipped profile, charged through the same clock the budget reads, is reported beside the simulation.
///
/// Nothing is asserted about the size of the extra window cost, which is measured and reported. What is asserted is that the
/// real scan of no shape trips the lexical budget, and that the `JavaScript` profile declares no captured terminator (so no `JavaScript` file is ever charged the window).
///
/// Timing-sensitive: it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class JavaScriptOpeningWindowCostTimingTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_OPENING_WINDOW_REPORT_JAVASCRIPT";

	private const double _MILLISECONDS_PER_SECOND = 1000.0;

	private const string _SHELL_LANGUAGE_NAME = "Shell";

	private const int _ODD_REMAINDER = 1;

	private const int _PAIR_SIZE = 2;

	private const int _WARM_UP_ITERATIONS = 1;

	private const int _MEASURED_ITERATIONS = 3;

	private const char _LINE_FEED = '\n';

	private const char _BACKSLASH = '\\';

	private const int _SIZE_MARGIN_IN_CHARACTERS = 16 * 1024;

	private const int _LINE_MARGIN_IN_CHARACTERS = 16;

	private const string _UNCLOSED_QUOTE_PER_LINE = "cat <<'word\n";

	private const string _UNCLOSED_QUOTE_ON_A_LONG_LINE = "cat <<'word ";

	private const string _PIECEWISE_QUOTED_WORD = "<<E\"O\"F\"O\"";

	private readonly ITestOutputHelper _output;

	public JavaScriptOpeningWindowCostTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheJavaScriptProfileDeclaresNoCapturedTerminatorSoNoJavaScriptShapeIsChargedTheOpeningWindow()
	{
		LexicalProfile profile = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;

		Assert.All(profile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.CapturedTerminator));
	}

	[Fact]
	public void TheCostOfTheExtraLookAheadWindowOfACapturedTerminatorOpeningIsMeasuredOnTheStressShapesAndTheRealScanNeverTripsTheBudget()
	{
		LexicalProfile profile = ShellRegisteredExpressions.Analyzer.LexicalProfile;
		ImmutableArray<QuoteRule> rules = [.. profile.QuoteRulesInEngineOrder.Where(rule => rule.CapturedTerminator is not null)];
		ImmutableArray<(string Language, MatchBoundStressShape Shape)> shapes =
		[
			.. ShellMatchBoundStressSet.Build().Select(shape => (_SHELL_LANGUAGE_NAME, shape)),
			.. OpeningWindowShapes().Select(shape => (_SHELL_LANGUAGE_NAME, shape))
		];
		ImmutableArray<OpeningWindowTiming> timings = [.. shapes.Select(entry => Measure(profile, rules, entry.Language, entry.Shape))];
		string report = Describe(timings, rules);

		_output.WriteLine(report);
		WriteWhereRequested(report);

		Assert.NotEmpty(rules);
		Assert.All(timings, timing => Assert.False(timing.RealScanTrippedTheBudget, timing.ShapeName));
		Assert.Contains(timings, timing => timing.Openings > 0);
	}

	/// <summary>The window the scanner builds, restated for the measure: the end of the logical line that holds the position, plus one line-length ceiling, capped at the text.</summary>
	internal static int EndOfTheOpeningWindow(string text, int position, bool withTheExtraCeiling)
	{
		int end = EndOfLogicalLineFrom(text, position);

		return withTheExtraCeiling
			? (int)Math.Min(text.Length, (long)end + ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS)
			: end;
	}

	private static int EndOfLogicalLineFrom(string text, int position)
	{
		int end = EndOfLineFrom(text, position);

		while (end < text.Length && FollowsAnOddBackslashRun(text, end))
		{
			end = EndOfLineFrom(text, end + 1);
		}

		return end;
	}

	private static int EndOfLineFrom(string text, int position)
	{
		int lineFeed = text.IndexOf(_LINE_FEED, position);

		return lineFeed < 0 ? text.Length : lineFeed;
	}

	private static bool FollowsAnOddBackslashRun(string text, int lineFeedPosition)
	{
		int run = 0;

		while (lineFeedPosition - run - 1 >= 0 && text[lineFeedPosition - run - 1] == _BACKSLASH)
		{
			run++;
		}

		return run % _PAIR_SIZE == _ODD_REMAINDER;
	}

	private static OpeningWindowTiming Measure(LexicalProfile profile, ImmutableArray<QuoteRule> rules, string language, MatchBoundStressShape shape)
	{
		string text = shape.Text;
		ChargeRecordingTimeProvider clock = new();
		LexicalScanner scanner = new(profile, clock);
		ZoneMask mask = scanner.Scan(text);
		double realCharge = 0.0;
		bool tripped = mask.AnomalyReason is LexicalAnomalyReason.LexicalBudgetExceeded;

		for (int iteration = 0; iteration < _WARM_UP_ITERATIONS + _MEASURED_ITERATIONS; iteration++)
		{
			clock.Reset();
			tripped |= scanner.Scan(text).AnomalyReason is LexicalAnomalyReason.LexicalBudgetExceeded;
			realCharge = iteration >= _WARM_UP_ITERATIONS ? Math.Max(realCharge, clock.ChargedMilliseconds) : realCharge;
		}

		ImmutableArray<(QuoteRule Rule, int Position)> openings = FindTheOpenings(text, mask, rules);
		WindowFigures lineOnly = TimeTheWindow(text, openings, withTheExtraCeiling: false);
		WindowFigures extended = TimeTheWindow(text, openings, withTheExtraCeiling: true);

		return new OpeningWindowTiming(language, shape.Name, text.Length, openings.Length, realCharge, tripped, lineOnly, extended);
	}

	private static ImmutableArray<(QuoteRule Rule, int Position)> FindTheOpenings(string text, ZoneMask mask, ImmutableArray<QuoteRule> rules)
	{
		ImmutableArray<(QuoteRule Rule, int Position)>.Builder openings = ImmutableArray.CreateBuilder<(QuoteRule, int)>();

		foreach (QuoteRule rule in rules)
		{
			int position = text.IndexOf(rule.Opening, StringComparison.Ordinal);

			while (position >= 0 && position < mask.Length)
			{
				LexicalZone zone = mask.ZoneAt(position);

				if (zone is LexicalZone.Code or LexicalZone.Delimiter)
				{
					openings.Add((rule, position));
				}

				position = text.IndexOf(rule.Opening, position + 1, StringComparison.Ordinal);
			}
		}

		return openings.ToImmutable();
	}

	/// <summary>The sum, over every opening, of one match of the rule's own opening expression within the window; the worst of the measured iterations. The sum is stopped at the lexical budget.</summary>
	private static WindowFigures TimeTheWindow(string text, ImmutableArray<(QuoteRule Rule, int Position)> openings, bool withTheExtraCeiling)
	{
		double worst = 0.0;
		int evaluated = 0;
		bool capped = false;

		if (openings.Length > 0)
		{
			for (int iteration = 0; iteration < _WARM_UP_ITERATIONS + _MEASURED_ITERATIONS; iteration++)
			{
				(double milliseconds, int done, bool stopped) = OneSum(text, openings, withTheExtraCeiling);

				if (iteration >= _WARM_UP_ITERATIONS)
				{
					worst = Math.Max(worst, milliseconds);
					evaluated = done;
					capped |= stopped;
				}
			}
		}

		return new WindowFigures(worst, evaluated, capped);
	}

	private static (double Milliseconds, int Evaluated, bool Capped) OneSum(string text, ImmutableArray<(QuoteRule Rule, int Position)> openings, bool withTheExtraCeiling)
	{
		double budget = LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS;
		long ticks = 0;
		int evaluated = 0;
		bool capped = false;
		int cachedLineStart = -1;
		int cachedWindowEnd = -1;

		foreach ((QuoteRule rule, int position) in openings)
		{
			if (capped)
			{
				break;
			}

			if (cachedLineStart < 0 || position < cachedLineStart || position >= cachedWindowEnd)
			{
				cachedLineStart = position;
				cachedWindowEnd = EndOfTheOpeningWindow(text, position, withTheExtraCeiling);
			}

			int windowEnd = Math.Max(cachedWindowEnd, position);
			long started = Stopwatch.GetTimestamp();

			try
			{
				rule.CapturedTerminator!.OpeningExpression.Match(text, position, windowEnd - position);
			}
			catch (RegexMatchTimeoutException)
			{
				capped = true;
			}

			ticks += Stopwatch.GetTimestamp() - started;
			evaluated++;
			capped |= ticks * _MILLISECONDS_PER_SECOND / Stopwatch.Frequency >= budget;
		}

		return (ticks * _MILLISECONDS_PER_SECOND / Stopwatch.Frequency, evaluated, capped);
	}

	/// <summary>Three shapes that leave the opening's terminator word unclosed, so the window is read to its end at every opening: the cases the extra ceiling matters on.</summary>
	private static ImmutableArray<MatchBoundStressShape> OpeningWindowShapes()
	{
		int fileSize = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES - _SIZE_MARGIN_IN_CHARACTERS;
		int lineLength = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS - _LINE_MARGIN_IN_CHARACTERS;

		return
		[
			new MatchBoundStressShape { Name = "sh-opening-window-quote-never-closed-one-per-line", Text = Fill(_UNCLOSED_QUOTE_PER_LINE, fileSize, lineLength, breakLines: false) },
			new MatchBoundStressShape { Name = "sh-opening-window-quote-never-closed-on-long-lines", Text = Fill(_UNCLOSED_QUOTE_ON_A_LONG_LINE, fileSize, lineLength, breakLines: true) },
			new MatchBoundStressShape { Name = "sh-opening-window-piecewise-quoted-words-on-long-lines", Text = Fill(_PIECEWISE_QUOTED_WORD + " ", fileSize, lineLength, breakLines: true) }
		];
	}

	private static string Fill(string unit, int size, int lineLength, bool breakLines)
	{
		StringBuilder text = new(size);
		int currentLine = 0;

		while (text.Length + unit.Length < size)
		{
			text.Append(unit);
			currentLine += unit.Length;

			if (breakLines && currentLine + unit.Length >= lineLength)
			{
				text.Append(_LINE_FEED);
				currentLine = 0;
			}
		}

		return text.ToString();
	}

	private static string Describe(ImmutableArray<OpeningWindowTiming> timings, ImmutableArray<QuoteRule> rules)
	{
		StringBuilder report = new();

		AppendHeader(report, rules);

		foreach (OpeningWindowTiming timing in timings)
		{
			AppendRow(report, timing);
		}

		AppendSummary(report, timings);

		return report.ToString();
	}

	private static void AppendHeader(StringBuilder report, ImmutableArray<QuoteRule> rules)
	{
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Cost of the opening window (concern 3). Captured-terminator rules of the shipped Shell profile: {0}. {1} warm-up and {2} measured iterations, the worst reported; build {3}",
			string.Join(" ", rules.Select(rule => rule.Opening)),
			_WARM_UP_ITERATIONS,
			_MEASURED_ITERATIONS,
			JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug)"));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Environment: {0}; {1}; {2} logical processors; runtime {3}; one line-length ceiling = {4} characters; a sum is stopped at the lexical budget of {5} ms",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version,
			ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));
		report.AppendLine("language	shape	characters	openings	real_scan_charged_ms	tripped	line_only_ms	line_only_evaluated	line_only_capped	with_ceiling_ms	with_ceiling_evaluated	with_ceiling_capped	extra_ms");
	}

	private static void AppendRow(StringBuilder report, OpeningWindowTiming timing)
	{
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"{0}	{1}	{2}	{3}	{4:F4}	{5}	{6:F3}	{7}	{8}	{9:F3}	{10}	{11}	{12:F3}",
			timing.Language,
			timing.ShapeName,
			timing.Characters,
			timing.Openings,
			timing.RealScanChargedMilliseconds,
			timing.RealScanTrippedTheBudget,
			timing.LineOnly.Milliseconds,
			timing.LineOnly.Evaluated,
			timing.LineOnly.Capped,
			timing.WithTheCeiling.Milliseconds,
			timing.WithTheCeiling.Evaluated,
			timing.WithTheCeiling.Capped,
			timing.WithTheCeiling.Milliseconds - timing.LineOnly.Milliseconds));
	}

	private static void AppendSummary(StringBuilder report, ImmutableArray<OpeningWindowTiming> timings)
	{
		OpeningWindowTiming worstExtra = timings.OrderByDescending(timing => timing.WithTheCeiling.Milliseconds - timing.LineOnly.Milliseconds).First();
		OpeningWindowTiming worstReal = timings.OrderByDescending(timing => timing.RealScanChargedMilliseconds).First();

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Worst extra cost of the ceiling {0:F3} ms on {1} ({2} openings); worst real charge of the shipped profile {3:F4} ms on {4}; shapes with openings {5} of {6}; budget in force {7} ms",
			worstExtra.WithTheCeiling.Milliseconds - worstExtra.LineOnly.Milliseconds,
			worstExtra.ShapeName,
			worstExtra.Openings,
			worstReal.RealScanChargedMilliseconds,
			worstReal.ShapeName,
			timings.Count(timing => timing.Openings > 0),
			timings.Length,
			LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS));
	}

	private static void WriteWhereRequested(string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}

	private readonly record struct WindowFigures(double Milliseconds, int Evaluated, bool Capped);

	private sealed record OpeningWindowTiming(
		string Language,
		string ShapeName,
		int Characters,
		int Openings,
		double RealScanChargedMilliseconds,
		bool RealScanTrippedTheBudget,
		WindowFigures LineOnly,
		WindowFigures WithTheCeiling);
}
