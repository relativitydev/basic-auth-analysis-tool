using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE DRAFT COST GATE OF THE `Python` CATALOG, on the pattern of the `TypeScript` draft cost gate: the draft expressions of <see cref="PythonDraftExpressionSet"/> that the recognition
/// engine charges to its match bound (the constant declaration, the fourteen classes, the two content markers and the twelve flow signals) run over every shape of <see cref="PythonDraftCostGateStressSet"/> on the system clock
/// (V1 typical and V2 worst per decoded file: 2 MiB per file, lines capped at 10,000 characters), and the worst per-file time is recorded. It is a DRAFT gate: it is never used to close an analyzer set, and no shipped analyzer
/// exists for it to time; the engine-measured gate of the shipped analyzer is a separate test.
///
/// WHAT THIS ASSERTS (the pass rule of the `TypeScript` gate) is what must hold whatever the aim: that no expression timed out; that no enumeration stalled (a match that begins before the previous one ended); and that the worst
/// per-file figure stays under the MATCH BOUND in force, which is the figure the engine charges against. The `JavaScript` aim of about 975 ms on Linux is printed beside the worst shape so a figure above it is visible; it
/// is an aim and not a gate. A draft figure above 11,075 ms on Linux starts the weighing of a reformulation; it is printed, never asserted.
///
/// WHAT IS TIMED. For each shape and each expression, the whole enumeration of its matches over the shape's text, with the policy match timeout. The per-file figure of an iteration is the SUM over every expression (the engine
/// charges every rule, every content marker, the constant declaration and every evaluated flow signal to the one file accumulator; every shape is charged all of them, the conservative reading). The first-line rule is charged to
/// the lexical budget and is not here: its own measurement is <see cref="PythonLexicalBudgetTimingTests"/>. The table is written where <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
///
/// Timing-sensitive: it asserts on wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing". It is written and compiled here, and run only in a separate timing run.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class PythonDraftCostGateTimingTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_PYTHON_DRAFT";

	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER_PYTHON_DRAFT";

	/// <summary>When set to a comma-separated list of shape-name fragments, only the shapes whose name holds one of them are timed: a reshape probe, never the gate (the gate runs with it unset).</summary>
	internal const string SHAPE_FILTER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_SHAPES_PYTHON_DRAFT";

	/// <summary>A draft figure above this per-file figure on the Linux VM starts the reformulation weighing (printed, never asserted).</summary>
	internal const double REFORMULATION_WEIGHING_FIGURE_MILLISECONDS = 11_075.0;

	private const char _SHAPE_FILTER_SEPARATOR = ',';

	private readonly ITestOutputHelper _output;

	public PythonDraftCostGateTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void ThePythonDraftExpressionsTerminateOnEveryBenignShapeAndTheWorstFileStaysUnderTheMatchBound()
	{
		ImmutableArray<MatchBoundStressShape> shapes = PythonDraftCostGateStressSet.Build();
		ImmutableArray<PythonShapeTiming> timings = [.. SelectTheShapesToTime(shapes).Select(Measure)];
		string report = Describe(timings);

		_output.WriteLine(report);
		PythonTimingReportWriter.WriteWhereRequested(REPORT_PATH_VARIABLE_NAME, report);
		WriteTheStressSetWhereRequested(shapes);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingExpressions));
		Assert.All(timings, timing => Assert.True(
			timing.WorstFileMilliseconds < RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS,
			$"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against the match bound of {RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS} ms"));
	}

	private static IEnumerable<MatchBoundStressShape> SelectTheShapesToTime(ImmutableArray<MatchBoundStressShape> shapes)
	{
		string? filter = Environment.GetEnvironmentVariable(SHAPE_FILTER_VARIABLE_NAME);
		string[] fragments = string.IsNullOrWhiteSpace(filter) ? [] : filter.Split(_SHAPE_FILTER_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		return fragments.Length == 0
			? shapes
			: shapes.Where(shape => fragments.Any(fragment => shape.Name.Contains(fragment, StringComparison.Ordinal)));
	}

	private static PythonShapeTiming Measure(MatchBoundStressShape shape)
	{
		string text = shape.Text;
		Dictionary<string, double> worstByExpression = [];
		List<string> nonTerminating = [];
		double worstFile = 0.0;
		bool timedOut = false;

		try
		{
			for (int iteration = 0; iteration < PythonTestDraftConstants.WARM_UP_ITERATIONS + PythonTestDraftConstants.MEASURED_ITERATIONS; iteration++)
			{
				double file = 0.0;

				foreach ((string identifier, Regex expression) in PythonDraftExpressionSet.MatchBoundExpressions)
				{
					(double milliseconds, bool stalled) = VBNetMatchBoundTimingTests.TimeOne(expression, text);

					file += milliseconds;

					if (stalled && !nonTerminating.Contains(identifier))
					{
						nonTerminating.Add(identifier);
					}

					if (iteration >= PythonTestDraftConstants.WARM_UP_ITERATIONS)
					{
						worstByExpression[identifier] = Math.Max(worstByExpression.GetValueOrDefault(identifier), milliseconds);
					}
				}

				if (iteration >= PythonTestDraftConstants.WARM_UP_ITERATIONS)
				{
					worstFile = Math.Max(worstFile, file);
				}
			}
		}
		catch (RegexMatchTimeoutException)
		{
			timedOut = true;
		}

		return new PythonShapeTiming(shape.Name, text.Length, worstFile, [.. worstByExpression.OrderBy(pair => pair.Key, StringComparer.Ordinal)], [.. nonTerminating], timedOut);
	}

	private static string Describe(ImmutableArray<PythonShapeTiming> timings)
	{
		StringBuilder report = new();
		PythonShapeTiming worst = timings.OrderByDescending(timing => timing.WorstFileMilliseconds).First();
		ImmutableArray<string> columns = [.. PythonDraftExpressionSet.MatchBoundExpressions.Select(pair => pair.Identifier)];

		report.AppendLine($"# The draft cost gate of the Python catalog. One row per stress shape; milliseconds, the worst of {PythonTestDraftConstants.MEASURED_ITERATIONS} measured iterations after {PythonTestDraftConstants.WARM_UP_ITERATIONS} warm-up.");
		report.AppendLine($"# Aim (not a gate, as for JavaScript): worst benign per-file figure about {JavaScriptMatchBoundTimingTests.AIM_WORST_FILE_MILLISECONDS.ToString("F0", CultureInfo.InvariantCulture)} ms on the Linux VM. Gate: under the match bound of {RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS} ms. Worst shape here: {worst.ShapeName}, {worst.WorstFileMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms.");
		report.AppendLine($"# Reformulation weighing (figure): a per-file figure above {REFORMULATION_WEIGHING_FIGURE_MILLISECONDS.ToString("F0", CultureInfo.InvariantCulture)} ms on Linux starts the weighing of a reformulation; this run: {(worst.WorstFileMilliseconds > REFORMULATION_WEIGHING_FIGURE_MILLISECONDS ? "ABOVE" : "not above")}.");
		report.AppendLine($"# Build: {(JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's")}.");
		report.AppendLine(string.Format(CultureInfo.InvariantCulture, "# Environment: {0}; {1}; {2} logical processors; runtime {3}", Environment.OSVersion, System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture, Environment.ProcessorCount, Environment.Version));
		report.AppendLine($"shape\tcharacters\tfile_ms\t{string.Join('\t', columns)}");

		foreach (PythonShapeTiming timing in timings)
		{
			report.Append(timing.ShapeName).Append('\t').Append(timing.SizeInCharacters.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(timing.WorstFileMilliseconds.ToString("F1", CultureInfo.InvariantCulture));

			foreach (string column in columns)
			{
				double value = timing.WorstExpressionMilliseconds.FirstOrDefault(pair => pair.Key == column).Value;

				report.Append('\t').Append(value.ToString("F1", CultureInfo.InvariantCulture));
			}

			report.AppendLine();
		}

		return report.ToString();
	}

	private static void WriteTheStressSetWhereRequested(ImmutableArray<MatchBoundStressShape> shapes)
	{
		string? stressFolder = Environment.GetEnvironmentVariable(STRESS_FOLDER_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(stressFolder))
		{
			foreach (MatchBoundStressShape shape in shapes)
			{
				string path = Path.Combine(stressFolder, PythonDraftCostGateStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
	}
}

/// <summary>What the timing procedure measured for one `Python` stress shape.</summary>
internal sealed record PythonShapeTiming(
	string ShapeName,
	int SizeInCharacters,
	double WorstFileMilliseconds,
	ImmutableArray<KeyValuePair<string, double>> WorstExpressionMilliseconds,
	ImmutableArray<string> NonTerminatingExpressions,
	bool AnExpressionTimedOut);
