using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// WHAT THE TIMING PROCEDURE RUNS ONE LANGUAGE'S SHAPES THROUGH: the real analyzer, the emission point its harness records into, how the harness builds a
/// file's recognition context, and the relative path a shape stands at. Both languages' harnesses have exactly these members, so the one procedure
/// below measures either.
/// </summary>
internal sealed record MatchBoundTimingTarget
{
	internal required ILanguageAnalyzer Analyzer { get; init; }

	internal required EmissionPoint EmissionPoint { get; init; }

	internal required Func<string, string, FileRecognitionContext> ContextFor { get; init; }

	internal required Func<MatchBoundStressShape, string> RelativePathOf { get; init; }
}

/// <summary>What the timing procedure measured for one stress shape.</summary>
internal sealed record MatchBoundShapeTiming
{
	internal required string ShapeName { get; init; }

	internal required int SizeInCharacters { get; init; }

	/// <summary>
	/// The worst measured ANALYZER-EXPRESSION time - the file's own <see cref="MatchBoundWatch"/>
	/// accumulator, read back through <see cref="RecognitionEngine.LastFileMatchingTimeForDiagnostics"/>
	/// after a real run on the system clock - or null where a rule's enumeration does not terminate
	/// on this shape - there is then no benign time to measure, only a file the bound will always
	/// stop. This is the quantity <see cref="MatchBoundWatch"/> bounds
	/// and the one the derived value in <see cref="Describe"/> is taken from.
	/// </summary>
	internal required double? WorstFileMilliseconds { get; init; }

	/// <summary>
	/// The worst measured WHOLE-FILE time - <see cref="RecognitionEngine.Recognize"/> end to end,
	/// symbol table through subsumption - kept for context only; the bound is taken from
	/// <see cref="WorstFileMilliseconds"/>.
	/// Null under the same non-terminating condition as the matching-only figure.
	/// </summary>
	internal required double? WorstWholeFileMilliseconds { get; init; }

	internal required int RecognizedConstructs { get; init; }

	/// <summary>
	/// How many files the measuring engine ran the flow signals on, read from
	/// <see cref="RecognitionEngine.FilesEvaluatedForFlowSignals"/> after the last measured run. One
	/// means the shape holds a High or Medium construct and the accumulator read back into
	/// <see cref="WorstFileMilliseconds"/> includes the flow signals' own evaluation; zero means it
	/// does not.
	/// </summary>
	internal required int FlowEvaluations { get; init; }

	/// <summary>
	/// How many times a flow evaluation was cut short by the bound on the last measured run. Nonzero on a
	/// benign shape means the bound is too small for the flow signals.
	/// </summary>
	internal required int FlowTrips { get; init; }

	internal required ImmutableSortedDictionary<string, double> WorstRuleMilliseconds { get; init; }

	/// <summary>
	/// Rules whose <c>Match</c>/<c>NextMatch</c> enumeration stopped advancing on this shape: a
	/// match that begins before the previous one ended, which no correct enumeration produces.
	/// </summary>
	internal required ImmutableArray<string> NonTerminatingRules { get; init; }

	internal required bool AnExpressionTimedOut { get; init; }
}

/// <summary>
/// THE MATCH-BOUND TIMING PROCEDURE, RUN ON <see cref="MatchBoundStressSet"/> AND
/// NEVER ON THE TEST REPOSITORY.
///
/// WHAT IS TIMED, AND WHY BOTH. <see cref="MatchBoundWatch"/>
/// accumulates every ANALYZER-DECLARED EXPRESSION'S OWN EVALUATION TIME - the lexical profile's
/// constant-declaration expression, every generation marker and content marker, and every rule's own
/// <c>Match</c>/<c>NextMatch</c> calls - rather than the whole of <see cref="RecognitionEngine.Recognize"/>
/// for the file. <see cref="MatchBoundShapeTiming.WorstFileMilliseconds"/> is computed by
/// <see cref="TimeEveryAnalyzerExpression"/>: a real engine run on the system clock, read back
/// through <see cref="RecognitionEngine.LastFileMatchingTimeForDiagnostics"/> - the exact quantity
/// the bound itself accumulates, rather than an independent re-summing of any subset of it.
///
/// WHY THE HEADLINE FIGURE IS NOT A SUM OF PER-RULE TIMES. <see cref="TimeEachRule"/> enumerates each rule's own expression
/// over the same text outside the engine, which never includes the symbol-table or demotion-signal phases that the bound's own
/// bracket covers, so it feeds the per-rule diagnostic column alone.
///
/// THE FLOW SIGNALS ARE MEASURED TOO. <see cref="MatchBoundStressSet"/> carries shapes that open with
/// one High construct and are then filled with the text each flow signal does its most work on, so
/// the engine evaluates the flow signals inside the same accumulator; the timing records how
/// many flow evaluations ran and how many were cut short, so a shape that never reaches the signals
/// cannot pass for one that does.
///
/// The whole-file figure is kept as <see cref="MatchBoundShapeTiming.WorstWholeFileMilliseconds"/>
/// for context: it is what the engine's own phases (classification, subsumption) cost
/// around the matches, which the bound does not track.
///
/// THE ENGINE RUNS WITH A FROZEN CLOCK AND IS TIMED FROM OUTSIDE for the whole-file context figure,
/// so the bound cannot cut a slow file short and hide the very time this procedure exists to see. The
/// analyzer-expression figure instead runs the engine on the SYSTEM clock, against the policy bound
/// itself - already an order of magnitude above the worst benign figure by construction, so a benign
/// shape finishes well inside it and the accumulator read back afterward is real elapsed time. Both
/// routes are safe only for a file on which every rule's enumeration terminates, so termination is
/// probed first, per rule, with a guard that stops at the first match that does not advance - and a
/// shape that fails the probe is recorded as non-terminating and never handed to the engine unbounded.
/// </summary>
internal static class MatchBoundTimingHarness
{
	internal const int WARM_UP_ITERATIONS = 2;
	internal const int MEASURED_ITERATIONS = 5;

	private const string _STRESS_RELATIVE_PATH_FORMAT = "stress/{0}.cs";

	internal static MatchBoundShapeTiming Measure(CSharpRecognitionHarness harness, MatchBoundStressShape shape)
	{
		return Measure(ForCSharp(harness), shape);
	}

	/// <summary>The `CSharp` harness as a target: its shapes stand at <c>stress/{name}.cs</c>.</summary>
	internal static MatchBoundTimingTarget ForCSharp(CSharpRecognitionHarness harness)
	{
		return new MatchBoundTimingTarget
		{
			Analyzer = harness.Analyzer,
			EmissionPoint = harness.EmissionPoint,
			ContextFor = (text, path) => harness.ContextFor(text, path),
			RelativePathOf = shape => string.Format(CultureInfo.InvariantCulture, _STRESS_RELATIVE_PATH_FORMAT, shape.Name)
		};
	}

	/// <summary>The `PowerShell` harness as a target: its shapes stand where <see cref="PowerShellMatchBoundStressSet.RelativePathOf"/> puts them.</summary>
	internal static MatchBoundTimingTarget ForPowerShell(PowerShellRecognitionHarness harness)
	{
		return new MatchBoundTimingTarget
		{
			Analyzer = harness.Analyzer,
			EmissionPoint = harness.EmissionPoint,
			ContextFor = (text, path) => harness.ContextFor(text, path),
			RelativePathOf = PowerShellMatchBoundStressSet.RelativePathOf
		};
	}

	internal static MatchBoundShapeTiming Measure(MatchBoundTimingTarget target, MatchBoundStressShape shape)
	{
		FileRecognitionContext context = target.ContextFor(shape.Text, target.RelativePathOf(shape));
		ImmutableArray<string> nonTerminating = ProbeTermination(target.Analyzer, context.NormalizedText);
		MatchBoundShapeTiming timing;

		try
		{
			timing = TimeTheShape(target, context, shape, nonTerminating);
		}
		catch (RegexMatchTimeoutException)
		{
			timing = new MatchBoundShapeTiming
			{
				ShapeName = shape.Name,
				SizeInCharacters = shape.Text.Length,
				WorstFileMilliseconds = null,
				WorstWholeFileMilliseconds = null,
				RecognizedConstructs = 0,
				FlowEvaluations = 0,
				FlowTrips = 0,
				WorstRuleMilliseconds = ImmutableSortedDictionary<string, double>.Empty,
				NonTerminatingRules = nonTerminating,
				AnExpressionTimedOut = true
			};
		}

		return timing;
	}

	private static MatchBoundShapeTiming TimeTheShape(
		MatchBoundTimingTarget target,
		FileRecognitionContext context,
		MatchBoundStressShape shape,
		ImmutableArray<string> nonTerminating)
	{
		bool fileCanBeTimed = nonTerminating.IsEmpty;
		double worstAnalyzerExpression = 0;
		double worstWholeFile = 0;
		int constructs = 0;
		(int Evaluations, int Trips) flow = (0, 0);
		Dictionary<string, double> worstPerRule = new(StringComparer.Ordinal);

		for (int iteration = 0; iteration < WARM_UP_ITERATIONS + MEASURED_ITERATIONS; iteration++)
		{
			bool measured = iteration >= WARM_UP_ITERATIONS;
			(double wholeFileMilliseconds, int recognized) = fileCanBeTimed ? TimeTheWholeFile(target, context) : (0, 0);
			(double analyzerExpressionMilliseconds, int flowEvaluations, int flowTrips) = fileCanBeTimed
				? TimeEveryAnalyzerExpression(target, context)
				: (0, 0, 0);
			Dictionary<string, double> ruleMilliseconds = TimeEachRule(target.Analyzer, context.NormalizedText, nonTerminating);

			worstAnalyzerExpression = measured
				? Math.Max(worstAnalyzerExpression, analyzerExpressionMilliseconds)
				: worstAnalyzerExpression;
			worstWholeFile = measured ? Math.Max(worstWholeFile, wholeFileMilliseconds) : worstWholeFile;
			constructs = recognized;
			flow = (flowEvaluations, flowTrips);
			FoldInTheWorst(worstPerRule, ruleMilliseconds, measured);
		}

		return new MatchBoundShapeTiming
		{
			ShapeName = shape.Name,
			SizeInCharacters = shape.Text.Length,
			WorstFileMilliseconds = fileCanBeTimed ? worstAnalyzerExpression : null,
			WorstWholeFileMilliseconds = fileCanBeTimed ? worstWholeFile : null,
			RecognizedConstructs = constructs,
			FlowEvaluations = flow.Evaluations,
			FlowTrips = flow.Trips,
			WorstRuleMilliseconds = worstPerRule.ToImmutableSortedDictionary(StringComparer.Ordinal),
			NonTerminatingRules = nonTerminating,
			AnExpressionTimedOut = false
		};
	}

	/// <summary>
	/// A REAL <see cref="RecognitionEngine"/> RUN, ON THE SYSTEM
	/// CLOCK, READ BACK THROUGH THE ENGINE'S OWN DIAGNOSTIC ACCUMULATOR - the seam
	/// <see cref="RecognitionEngine.LastFileMatchingTimeForDiagnostics"/> exists for. This is what
	/// <see cref="MatchBoundWatch"/> would have charged a real scan of this file: the symbol table's
	/// constant-declaration pass, every generation marker and content marker, and every rule's own
	/// matching, all through the same bracket production code uses
	/// (<see cref="TimedRegexEvaluation"/>) - never a re-summing of a subset of those phases measured
	/// independently. The engine runs with a GENEROUS bound (<see cref="RecognitionEnginePolicy.MATCH_BOUND"/>
	/// itself, already ten times the worst benign figure by construction) rather than an unbounded
	/// one, on the system clock rather than a frozen one, so a benign stress shape completes well
	/// inside it and the figure read back afterward is real elapsed time, not zero.
	/// </summary>
	private static (double Milliseconds, int FlowEvaluations, int FlowTrips) TimeEveryAnalyzerExpression(
		MatchBoundTimingTarget target,
		FileRecognitionContext context)
	{
		RecognitionEngine engine = new(target.EmissionPoint, TimeProvider.System);

		engine.Recognize(context);

		return (engine.LastFileMatchingTimeForDiagnostics.TotalMilliseconds, engine.FilesEvaluatedForFlowSignals, engine.FlowSignalTripCount);
	}

	/// <summary>
	/// Every rule whose enumeration stops advancing on the text. A correct enumeration returns each
	/// next match at or after the end of the previous one; a match that begins earlier is the
	/// enumeration revisiting ground it has covered, and it will do so without end.
	/// </summary>
	private static ImmutableArray<string> ProbeTermination(ILanguageAnalyzer analyzer, string normalizedText)
	{
		return
		[
			.. analyzer.DetectionRulesByPatternClass
				.Where(entry => !EnumerationAdvances(entry.Value.Expression, normalizedText))
				.Select(entry => entry.Key)
				.Order(StringComparer.Ordinal)
		];
	}

	internal static bool EnumerationAdvances(Regex expression, string normalizedText)
	{
		bool advances = true;
		int previousEnd = 0;
		Match match = expression.Match(normalizedText);

		while (match.Success && advances)
		{
			advances = match.Index >= previousEnd;
			previousEnd = match.Index + Math.Max(match.Length, 1);
			match = match.NextMatch();
		}

		return advances;
	}

	private static (double Milliseconds, int Recognized) TimeTheWholeFile(
		MatchBoundTimingTarget target,
		FileRecognitionContext context)
	{
		RecognitionEngine engine = new(target.EmissionPoint, new FrozenTimeProvider());
		Stopwatch stopwatch = Stopwatch.StartNew();
		int recognized = engine.Recognize(context).Constructs.Length;
		stopwatch.Stop();

		return (stopwatch.Elapsed.TotalMilliseconds, recognized);
	}

	/// <summary>
	/// THE PER-RULE DIAGNOSTIC COLUMN ONLY - NOT WHERE THE HEADLINE
	/// FIGURE COMES FROM. Each terminating rule's expression enumerated over the file exactly as
	/// <see cref="RecognitionEngine"/> enumerates it - <c>Match</c>, then <c>NextMatch</c> until none
	/// remains - without the classification that follows each match, and without the symbol-table or
	/// demotion-signal phases <see cref="TimeEveryAnalyzerExpression"/> measures alongside it. Useful
	/// for seeing which rule is slowest on a shape; not summed into
	/// <see cref="MatchBoundShapeTiming.WorstFileMilliseconds"/>, because that
	/// sum would omit the symbol-table and demotion-signal phases the bound covers.
	/// </summary>
	private static Dictionary<string, double> TimeEachRule(
		ILanguageAnalyzer analyzer,
		string normalizedText,
		ImmutableArray<string> nonTerminating)
	{
		Dictionary<string, double> milliseconds = new(StringComparer.Ordinal);

		foreach ((string patternClass, DetectionRule rule) in analyzer.DetectionRulesByPatternClass
			.Where(entry => !nonTerminating.Contains(entry.Key, StringComparer.Ordinal)))
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			Match match = rule.Expression.Match(normalizedText);

			while (match.Success)
			{
				match = match.NextMatch();
			}

			stopwatch.Stop();
			milliseconds[patternClass] = stopwatch.Elapsed.TotalMilliseconds;
		}

		return milliseconds;
	}

	private static void FoldInTheWorst(Dictionary<string, double> worst, Dictionary<string, double> latest, bool measured)
	{
		foreach ((string patternClass, double milliseconds) in latest)
		{
			double previous = worst.GetValueOrDefault(patternClass);
			worst[patternClass] = measured ? Math.Max(previous, milliseconds) : previous;
		}
	}

	/// <summary>The timing table, in the form the procedure records it.</summary>
	internal static string Describe(ImmutableArray<MatchBoundShapeTiming> timings, int safetyMultiple, int roundingStep)
	{
		StringBuilder report = new();
		double worstBenign = timings.Max(timing => timing.WorstFileMilliseconds ?? 0);
		double derived = Math.Ceiling(worstBenign * safetyMultiple / roundingStep) * roundingStep;

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Match-bound timing: {0} warm-up and {1} measured iterations per shape; worst measured iteration reported",
			WARM_UP_ITERATIONS,
			MEASURED_ITERATIONS));
		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Environment: {0}; {1}; {2} logical processors; runtime {3}",
			Environment.OSVersion,
			System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
			Environment.ProcessorCount,
			Environment.Version));

		foreach (MatchBoundShapeTiming timing in timings)
		{
			AppendShape(report, timing);
		}

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"Worst benign per-file ANALYZER-EXPRESSION time over terminating shapes: {0:F1} ms; x{1} = {2:F1} ms; rounded up to a multiple of {3} ms = {4:F0} ms; bound in force = {5} ms",
			worstBenign,
			safetyMultiple,
			worstBenign * safetyMultiple,
			roundingStep,
			derived,
			RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS));

		return report.ToString();
	}

	private static void AppendShape(StringBuilder report, MatchBoundShapeTiming timing)
	{
		string fileFigure = timing.WorstFileMilliseconds is double milliseconds
			? milliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms"
			: "not measurable - enumeration does not terminate";
		string wholeFileFigure = timing.WorstWholeFileMilliseconds is double wholeFileMilliseconds
			? wholeFileMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms"
			: "not measurable - enumeration does not terminate";

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"shape {0}: {1} characters, {2} constructs recognized, worst analyzer-expression {3} (worst whole-file, context only, {4}), non-terminating rules [{5}], expression timeout {6}, flow evaluations {7}, flow trips {8}",
			timing.ShapeName,
			timing.SizeInCharacters,
			timing.RecognizedConstructs,
			fileFigure,
			wholeFileFigure,
			string.Join(", ", timing.NonTerminatingRules),
			timing.AnExpressionTimedOut ? "fired" : "did not fire",
			timing.FlowEvaluations,
			timing.FlowTrips));
		report.AppendLine("  per rule (ms): " + string.Join(
			", ",
			timing.WorstRuleMilliseconds.Select(entry => string.Format(
				CultureInfo.InvariantCulture,
				"{0}={1:F1}",
				entry.Key,
				entry.Value))));
	}
}
