using System.Collections.Immutable;
using System.Diagnostics;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>What the lexical-budget gate measured for one `TypeScript` stress shape.</summary>
internal sealed record TypeScriptLexicalTiming(
	string ShapeName,
	int SizeInCharacters,
	double ChargedMilliseconds,
	double WholeScanMilliseconds,
	double PredicateMilliseconds,
	int PredicateCalls,
	bool Abandoned,
	bool TrippedTheBudget);

/// <summary>
/// THE MEASUREMENTS OF THE `TypeScript` LEXICAL-BUDGET GATE, on the pattern of <see cref="ShellLexicalBudgetMeasurements"/>. Three quantities per stress shape, each the worst of the measured iterations: the time the lexical
/// budget is charged, read from <see cref="ChargeRecordingTimeProvider"/> through a scan with the SHIPPED profile (a `TypeScript` file declares no first-line rule, no here-document opening and no interpolation extent, so the
/// figure is what the budget watch is asked to charge and is expected to be nothing); the whole scan, for context; and the backward predicate that decides whether a slash opens a regular expression (after an
/// exclamation mark, the `JavaScript` predicate elsewhere), which the budget does NOT charge, called at every position a slash stands at, as the scanner would call it, so that the ten thousand lines of
/// <c>x!/y</c> and the anchor-dense declaration files have their own row, because nothing else bounds the predicate.
/// </summary>
internal static class TypeScriptLexicalBudgetMeasurements
{
	internal const int WARM_UP_ITERATIONS = 1;

	internal const int MEASURED_ITERATIONS = 3;

	private const string _REGULAR_EXPRESSION_OPENING = "/";

	internal static ImmutableArray<TypeScriptLexicalTiming> MeasureEveryShape(ImmutableArray<MatchBoundStressShape> shapes)
	{
		LexicalProfile profile = TypeScriptRegisteredExpressions.Analyzer.LexicalProfile;
		DelimiterContextPredicate predicate = TheRegularExpressionOpeningPredicate(profile);
		ImmutableArray<TypeScriptLexicalTiming> timings = [.. shapes.Select(shape => Measure(profile, predicate, shape))];

		return timings;
	}

	/// <summary>The opening-context predicate of the regular-expression rule, read from the shipped profile and not restated.</summary>
	internal static DelimiterContextPredicate TheRegularExpressionOpeningPredicate(LexicalProfile profile)
	{
		QuoteRule rule = profile.QuoteRulesInEngineOrder.Single(candidate => candidate.Opening == _REGULAR_EXPRESSION_OPENING);

		return rule.OpeningContextPredicate!;
	}

	private static TypeScriptLexicalTiming Measure(LexicalProfile profile, DelimiterContextPredicate predicate, MatchBoundStressShape shape)
	{
		ScanFigures scan = TimeTheScan(profile, shape);
		(double predicateMilliseconds, int predicateCalls) = TimeThePredicate(predicate, shape.Text);

		return new TypeScriptLexicalTiming(
			shape.Name,
			shape.Text.Length,
			scan.Charged,
			scan.WholeScan,
			predicateMilliseconds,
			predicateCalls,
			scan.Abandoned,
			scan.Tripped);
	}

	private static ScanFigures TimeTheScan(LexicalProfile profile, MatchBoundStressShape shape)
	{
		ChargeRecordingTimeProvider clock = new();
		LexicalScanner scanner = new(profile, clock);
		ScanFigures worst = new(0.0, 0.0, false, false);

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
					Math.Max(worst.WholeScan, stopwatch.Elapsed.TotalMilliseconds),
					abandoned,
					tripped)
				: worst with { Abandoned = abandoned, Tripped = tripped };
		}

		return worst;
	}

	private readonly record struct ScanFigures(double Charged, double WholeScan, bool Abandoned, bool Tripped);

	/// <summary>The predicate called at every position a slash stands at in the text, the worst of the measured iterations, and how many calls that is.</summary>
	private static (double Milliseconds, int Calls) TimeThePredicate(DelimiterContextPredicate predicate, string text)
	{
		int calls = 0;
		double worst = 0.0;
		bool anyAccepted = false;

		for (int iteration = 0; iteration < WARM_UP_ITERATIONS + MEASURED_ITERATIONS; iteration++)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			int position = text.IndexOf(_REGULAR_EXPRESSION_OPENING, StringComparison.Ordinal);
			int callsThisIteration = 0;

			while (position >= 0)
			{
				anyAccepted |= predicate(text.AsSpan(), position);
				callsThisIteration++;
				position = text.IndexOf(_REGULAR_EXPRESSION_OPENING, position + 1, StringComparison.Ordinal);
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
}
