using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Lexical;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// A CLOCK THAT RECORDS WHAT THE LEXICAL BUDGET IS CHARGED. The scanner's budget watch reads the clock twice around every match of an analyzer-declared expression, and nowhere else:
/// once for the start and once for the elapsed time. This clock reads the system's, and adds up the span between each pair of reads, so what it holds after a scan is exactly the time
/// the budget charged to that file - the here-document openings' own evaluation, with the scanner's work between the matches and the opening-context predicates left out.
/// </summary>
internal sealed class ChargeRecordingTimeProvider : TimeProvider
{
	private const double _MILLISECONDS_PER_SECOND = 1000.0;

	private long _pendingStart;
	private bool _hasPendingStart;
	private long _chargedTicks;

	/// <summary>The time charged to the budget since the last <see cref="Reset"/>, in milliseconds.</summary>
	internal double ChargedMilliseconds => _chargedTicks * _MILLISECONDS_PER_SECOND / TimestampFrequency;

	internal void Reset()
	{
		_chargedTicks = 0;
		_hasPendingStart = false;
	}

	public override long GetTimestamp()
	{
		long now = Stopwatch.GetTimestamp();

		if (_hasPendingStart)
		{
			_chargedTicks += now - _pendingStart;
			_hasPendingStart = false;
		}
		else
		{
			_pendingStart = now;
			_hasPendingStart = true;
		}

		return now;
	}

	public override long TimestampFrequency => Stopwatch.Frequency;
}

/// <summary>What the lexical-budget gate measured for one `Shell` stress shape.</summary>
internal sealed record ShellLexicalTiming(
	string ShapeName,
	int SizeInCharacters,
	double ChargedMilliseconds,
	double ArithmeticChargedMilliseconds,
	double WholeScanMilliseconds,
	double PredicateMilliseconds,
	int PredicateCalls,
	double CommentPredicateMilliseconds,
	int CommentPredicateCalls,
	bool Abandoned,
	bool TrippedTheBudget);

/// <summary>
/// THE MEASUREMENTS OF THE `Shell` LEXICAL-BUDGET GATE. Three quantities per stress shape, each the worst of the measured iterations:
/// the time the lexical budget is charged (the here-document openings' own evaluation, through a scan with <see cref="ShellShapedProfile"/>, which carries the draft openings); the whole scan, for context; and the arithmetic-context opening predicate of the here-document opening, which the budget does NOT charge, called at every position a `&lt;&lt;`
/// stands at, as the scanner would call it - its own row, because nothing else bounds it.
/// </summary>
internal static class ShellLexicalBudgetMeasurements
{
	internal const int WARM_UP_ITERATIONS = 1;

	internal const int MEASURED_ITERATIONS = 3;

	private const string _HERE_DOCUMENT_OPENING = "<<";

	private const string _LINE_COMMENT_OPENING = "#";

	internal static ImmutableArray<ShellLexicalTiming> MeasureEveryShape(ImmutableArray<MatchBoundStressShape> shapes)
	{
		ImmutableArray<ShellLexicalTiming> timings = [.. shapes.Select(Measure)];

		return timings;
	}

	private static ShellLexicalTiming Measure(MatchBoundStressShape shape)
	{
		DelimiterContextPredicate predicate = TheHereDocumentOpeningPredicate();
		DelimiterContextPredicate commentPredicate = TheCommentOpeningPredicate();
		ScanFigures plain = TimeTheScan(ShellShapedProfile.Build(), shape);
		ScanFigures withExecutedStrings = TimeTheScan(ShellShapedProfile.BuildWithExecutedStrings(), shape);
		(double predicateMilliseconds, int predicateCalls) = TimeThePredicate(predicate, shape.Text, _HERE_DOCUMENT_OPENING);
		(double commentMilliseconds, int commentCalls) = TimeThePredicate(commentPredicate, shape.Text, _LINE_COMMENT_OPENING);

		return new ShellLexicalTiming(
			shape.Name,
			shape.Text.Length,
			Math.Max(plain.Charged, withExecutedStrings.Charged),
			Math.Max(plain.Arithmetic, withExecutedStrings.Arithmetic),
			Math.Max(plain.WholeScan, withExecutedStrings.WholeScan),
			predicateMilliseconds,
			predicateCalls,
			commentMilliseconds,
			commentCalls,
			plain.Abandoned || withExecutedStrings.Abandoned,
			plain.Tripped || withExecutedStrings.Tripped);
	}

	/// <summary>
	/// One profile's scan of one shape, the worst of the measured iterations. What is charged is read from the clock that records every span the watch
	/// charges (the expression searches and the arithmetic read-aheads together, which is what the budget weighs), and the arithmetic share from the scanner's own account of it.
	/// The profile with its executed-string context declared is measured as well as the one without, so the figure includes what reading an executed string adds.
	/// </summary>
	private static ScanFigures TimeTheScan(LexicalProfile profile, MatchBoundStressShape shape)
	{
		ChargeRecordingTimeProvider clock = new();
		LexicalScanner scanner = new(profile, clock);
		ScanFigures worst = new(0.0, 0.0, 0.0, false, false);

		for (int iteration = 0; iteration < WARM_UP_ITERATIONS + MEASURED_ITERATIONS; iteration++)
		{
			clock.Reset();

			Stopwatch stopwatch = Stopwatch.StartNew();
			ZoneMask mask = scanner.Scan(shape.Text);

			stopwatch.Stop();

			bool abandoned = worst.Abandoned || mask.WasAbandonedMidFile;
			bool tripped = worst.Tripped || mask.AnomalyReason is LexicalAnomalyReason.LexicalBudgetExceeded;

			worst = iteration >= WARM_UP_ITERATIONS
				? new ScanFigures(
					Math.Max(worst.Charged, clock.ChargedMilliseconds),
					Math.Max(worst.Arithmetic, scanner.ChargeOfTheLastScan.ArithmeticReadAhead.TotalMilliseconds),
					Math.Max(worst.WholeScan, stopwatch.Elapsed.TotalMilliseconds),
					abandoned,
					tripped)
				: worst with { Abandoned = abandoned, Tripped = tripped };
		}

		return worst;
	}

	private readonly record struct ScanFigures(double Charged, double Arithmetic, double WholeScan, bool Abandoned, bool Tripped);

	/// <summary>The opening-context predicate of the plain here-document rule, read from the profile and not restated.</summary>
	internal static DelimiterContextPredicate TheHereDocumentOpeningPredicate()
	{
		QuoteRule rule = ShellShapedProfile.Build().QuoteRulesInEngineOrder.Single(candidate => candidate.Opening == _HERE_DOCUMENT_OPENING);

		return rule.OpeningContextPredicate!;
	}

	/// <summary>The opening-context predicate of the line-comment rule (PR-SH-01), read from the profile and not restated.</summary>
	internal static DelimiterContextPredicate TheCommentOpeningPredicate()
	{
		LineCommentRule rule = ShellShapedProfile.Build().LineCommentRulesInEngineOrder.Single(candidate => candidate.Opening == _LINE_COMMENT_OPENING);

		return rule.OpeningContextPredicate!;
	}

	/// <summary>The predicate called at every position its opening stands at in the text, the worst of the measured iterations, and how many calls that is.</summary>
	private static (double Milliseconds, int Calls) TimeThePredicate(DelimiterContextPredicate predicate, string text, string opening)
	{
		int calls = 0;
		double worst = 0.0;
		bool anyAccepted = false;

		for (int iteration = 0; iteration < WARM_UP_ITERATIONS + MEASURED_ITERATIONS; iteration++)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			int position = text.IndexOf(opening, StringComparison.Ordinal);
			int callsThisIteration = 0;

			while (position >= 0)
			{
				anyAccepted |= predicate(text.AsSpan(), position);
				callsThisIteration++;
				position = text.IndexOf(opening, position + 1, StringComparison.Ordinal);
			}

			stopwatch.Stop();
			calls = callsThisIteration;

			if (iteration >= WARM_UP_ITERATIONS)
			{
				worst = Math.Max(worst, stopwatch.Elapsed.TotalMilliseconds);
			}
		}

		GC.KeepAlive(anyAccepted);

		return (worst, calls);
	}

	/// <summary>The time of one whole evaluation of the first-line expression over a first line, the worst of the measured iterations.</summary>
	internal static double TimeTheFirstLineExpression(Regex expression, string line)
	{
		double worst = 0.0;

		for (int iteration = 0; iteration < LexicalBudgetTimingTests.WARM_UP_ITERATIONS + LexicalBudgetTimingTests.MEASURED_ITERATIONS; iteration++)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();

			expression.IsMatch(line);
			stopwatch.Stop();

			if (iteration >= LexicalBudgetTimingTests.WARM_UP_ITERATIONS)
			{
				worst = Math.Max(worst, stopwatch.Elapsed.TotalMilliseconds);
			}
		}

		return worst;
	}
}
