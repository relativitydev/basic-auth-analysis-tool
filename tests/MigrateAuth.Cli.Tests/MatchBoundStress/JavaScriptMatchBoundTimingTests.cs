using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE COST GATE OF THE `JavaScript` DRAFT CATALOG, on the pattern of the `Shell` cost gate: the draft expressions of <see cref="JavaScriptDraftExpressions"/> that the recognition engine
/// charges to its match bound run over every shape of <see cref="JavaScriptMatchBoundStressSet"/> on the system clock, and the worst per-file time is recorded against the design AIM of about 975 ms.
///
/// THE 975 ms FIGURE IS AN AIM, NOT A GATE. What this asserts is what must hold whatever the aim: that no expression timed
/// out; that no enumeration stalled (a match that begins before the previous one ended); and that the worst per-file figure stays under the MATCH BOUND in force, which is the figure the engine charges against.
/// The aim is printed in the report beside the worst shape, so a figure above it is visible.
///
/// WHAT IS TIMED. For each shape and each expression, the whole enumeration of its matches over the shape's text, with the policy match timeout. The per-file figure of an iteration is the SUM over every
/// expression (the engine charges every rule, every content marker, the constant declaration and every evaluated flow signal to the one file accumulator; every shape is charged all of them, the conservative reading).
/// The first-line rule is charged to the lexical budget and is not in the sum. The table is written where <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class JavaScriptMatchBoundTimingTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_JAVASCRIPT";

	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER_JAVASCRIPT";

	/// <summary>When set to a comma-separated list of shape-name fragments, only the shapes whose name holds one of them are timed: a reshape probe, never the gate (the gate runs with it unset).</summary>
	internal const string SHAPE_FILTER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_SHAPES_JAVASCRIPT";

	/// <summary>The design aim: the worst benign per-file analyzer-expression time, in milliseconds. An aim, not a gate.</summary>
	internal const double AIM_WORST_FILE_MILLISECONDS = 975.0;

	internal const int WARM_UP_ITERATIONS = 1;

	internal const int MEASURED_ITERATIONS = 3;

	private const char _SHAPE_FILTER_SEPARATOR = ',';

	private readonly ITestOutputHelper _output;

	public JavaScriptMatchBoundTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheJavaScriptDraftExpressionsTerminateOnEveryBenignShapeAndTheWorstFileStaysUnderTheMatchBound()
	{
		ImmutableArray<MatchBoundStressShape> shapes = JavaScriptMatchBoundStressSet.Build();
		ImmutableArray<JavaScriptShapeTiming> timings = [.. SelectTheShapesToTime(shapes).Select(Measure)];
		string report = Describe(timings);

		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);
		WriteTheStressSetWhereRequested(shapes);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingExpressions));
		Assert.All(timings, timing => Assert.True(
			timing.WorstFileMilliseconds < RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS,
			$"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against the match bound of {RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS} ms"));
	}

	internal static bool TheTestAssemblyIsOptimized()
	{
		DebuggableAttribute? attribute = typeof(JavaScriptMatchBoundTimingTests).Assembly.GetCustomAttribute<DebuggableAttribute>();

		return attribute is null || !attribute.IsJITOptimizerDisabled;
	}

	private static IEnumerable<MatchBoundStressShape> SelectTheShapesToTime(ImmutableArray<MatchBoundStressShape> shapes)
	{
		string? filter = Environment.GetEnvironmentVariable(SHAPE_FILTER_VARIABLE_NAME);
		string[] fragments = string.IsNullOrWhiteSpace(filter) ? [] : filter.Split(_SHAPE_FILTER_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		return fragments.Length == 0
			? shapes
			: shapes.Where(shape => fragments.Any(fragment => shape.Name.Contains(fragment, StringComparison.Ordinal)));
	}

	private static JavaScriptShapeTiming Measure(MatchBoundStressShape shape)
	{
		string text = shape.Text;
		Dictionary<string, double> worstByExpression = [];
		List<string> nonTerminating = [];
		double worstFile = 0.0;
		bool timedOut = false;

		try
		{
			for (int iteration = 0; iteration < WARM_UP_ITERATIONS + MEASURED_ITERATIONS; iteration++)
			{
				double file = 0.0;

				foreach ((string identifier, Regex expression) in JavaScriptDraftExpressions.MatchBoundExpressions)
				{
					(double milliseconds, bool stalled) = VBNetMatchBoundTimingTests.TimeOne(expression, text);

					file += milliseconds;

					if (stalled && !nonTerminating.Contains(identifier))
					{
						nonTerminating.Add(identifier);
					}

					if (iteration >= WARM_UP_ITERATIONS)
					{
						worstByExpression[identifier] = Math.Max(worstByExpression.GetValueOrDefault(identifier), milliseconds);
					}
				}

				if (iteration >= WARM_UP_ITERATIONS)
				{
					worstFile = Math.Max(worstFile, file);
				}
			}
		}
		catch (RegexMatchTimeoutException)
		{
			timedOut = true;
		}

		return new JavaScriptShapeTiming(shape.Name, text.Length, worstFile, [.. worstByExpression.OrderBy(pair => pair.Key, StringComparer.Ordinal)], [.. nonTerminating], timedOut);
	}

	private static string Describe(ImmutableArray<JavaScriptShapeTiming> timings)
	{
		StringBuilder report = new();
		JavaScriptShapeTiming worst = timings.OrderByDescending(timing => timing.WorstFileMilliseconds).First();
		ImmutableArray<string> columns = [.. JavaScriptDraftExpressions.MatchBoundExpressions.Select(pair => pair.Identifier)];

		report.AppendLine($"# The cost gate of the JavaScript draft catalog. One row per stress shape; milliseconds, the worst of {MEASURED_ITERATIONS} measured iterations after {WARM_UP_ITERATIONS} warm-up.");
		report.AppendLine($"# Aim (not a gate): worst benign per-file figure about {AIM_WORST_FILE_MILLISECONDS.ToString("F0", CultureInfo.InvariantCulture)} ms on the Linux VM. Gate: under the match bound of {RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS} ms. Worst shape here: {worst.ShapeName}, {worst.WorstFileMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms.");
		report.AppendLine($"# Build: {(TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's")}.");
		report.AppendLine(string.Format(CultureInfo.InvariantCulture, "# Environment: {0}; {1}; {2} logical processors; runtime {3}", Environment.OSVersion, System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture, Environment.ProcessorCount, Environment.Version));
		report.AppendLine($"shape\tcharacters\tfile_ms\t{string.Join('\t', columns)}");

		foreach (JavaScriptShapeTiming timing in timings)
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

	private static void WriteTheReportWhereRequested(string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(REPORT_PATH_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(reportPath))
		{
			File.WriteAllText(reportPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}

	private static void WriteTheStressSetWhereRequested(ImmutableArray<MatchBoundStressShape> shapes)
	{
		string? stressFolder = Environment.GetEnvironmentVariable(STRESS_FOLDER_VARIABLE_NAME);

		if (!string.IsNullOrWhiteSpace(stressFolder))
		{
			foreach (MatchBoundStressShape shape in shapes)
			{
				string path = Path.Combine(stressFolder, JavaScriptMatchBoundStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
	}
}

/// <summary>What the timing procedure measured for one `JavaScript` stress shape.</summary>
internal sealed record JavaScriptShapeTiming(
	string ShapeName,
	int SizeInCharacters,
	double WorstFileMilliseconds,
	ImmutableArray<KeyValuePair<string, double>> WorstExpressionMilliseconds,
	ImmutableArray<string> NonTerminatingExpressions,
	bool AnExpressionTimedOut);
