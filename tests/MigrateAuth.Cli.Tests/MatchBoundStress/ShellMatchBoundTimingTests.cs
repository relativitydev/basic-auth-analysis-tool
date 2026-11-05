using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE COST GATE OF THE `Shell` DRAFT CATALOG: the draft expressions of <see cref="ShellDraftExpressions"/> that the recognition engine charges to its match bound
/// run over every shape of <see cref="ShellMatchBoundStressSet"/> on the system clock, and the worst per-file time is compared with the target,
/// <see cref="TARGET_WORST_FILE_MILLISECONDS"/>.
///
/// WHAT IS TIMED. For each shape and each expression, the whole enumeration of its matches over the shape's text, with the policy match timeout. The per-file figure of an
/// iteration is the SUM over every expression: the engine charges every rule, every content marker, the constant declaration and every evaluated flow signal to the one file
/// accumulator. The flow signals are charged on every shape here, which is the conservative reading (the engine evaluates them only on a file that holds a High or Medium
/// construct), and so are all the class rules (the engine runs a rule only on a file its preselection anchors select). The two here-document openings and the first-line
/// detection are not in the sum: they are charged to the lexical budget, which <see cref="LexicalBudgetTimingTests"/> measures.
///
/// WHAT THIS ASSERTS. That no expression timed out; that no enumeration stalled (a match that begins before the previous one ended); and that the worst per-file figure on the
/// machine that runs it stays under the target. The table is written where <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class ShellMatchBoundTimingTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_SHELL";

	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER_SHELL";

	/// <summary>When set to a comma-separated list of shape-name fragments, only the shapes whose name holds one of them are timed: a reshape probe, never the gate (the gate runs with it unset).</summary>
	internal const string SHAPE_FILTER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_SHAPES_SHELL";

	private const char _SHAPE_FILTER_SEPARATOR = ',';

	/// <summary>The target for the worst benign per-file analyzer-expression time.</summary>
	internal const double TARGET_WORST_FILE_MILLISECONDS = 975.0;

	/// <summary>
	/// The ceiling that stands in for the target when the test assembly was built with the JIT optimizer disabled (a Debug build). The source-generated expressions then run
	/// several times slower than in the Native AOT binary the target describes, so a Debug figure is not the product's. It is the design aim, ten times the target rounded up to
	/// the next 250 ms, which still catches an expression that has lost its bound; the target itself is asserted on the optimized build.
	/// </summary>
	internal const double UNOPTIMIZED_BUILD_CEILING_MILLISECONDS = 9750.0;

	internal const int WARM_UP_ITERATIONS = 1;

	internal const int MEASURED_ITERATIONS = 3;

	/// <summary>
	/// The shapes of the shared stress set that exercise the shipped <c>SH-009</c>, whose form differs from the draft's; they are timed against the
	/// shipped expression by the shipped cost gate, not here.
	/// </summary>
	internal static readonly ImmutableHashSet<string> SHAPES_OF_THE_SHIPPED_SH_009 =
		["sh-unfinished-array-lines-to-the-size-ceiling", "sh-one-line-arrays-of-four-hundred-pairs"];

	private readonly ITestOutputHelper _output;

	public ShellMatchBoundTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheDraftExpressionsTerminateOnEveryBenignShapeAndTheWorstFileStaysUnderTheTarget()
	{
		ImmutableArray<MatchBoundStressShape> shapes = ShellMatchBoundStressSet.Build();
		ImmutableArray<MatchBoundStressShape> draftShapes = [.. shapes.Where(shape => !SHAPES_OF_THE_SHIPPED_SH_009.Contains(shape.Name))];
		ImmutableArray<ShellShapeTiming> timings = [.. SelectTheShapesToTime(draftShapes).Select(Measure)];
		string report = Describe(timings);

		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);
		WriteTheStressSetWhereRequested(shapes);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingExpressions));
		double ceiling = TheTestAssemblyIsOptimized() ? TARGET_WORST_FILE_MILLISECONDS : UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;
		Assert.All(timings, timing => Assert.True(timing.WorstFileMilliseconds < ceiling, $"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against {ceiling:F0} ms"));
	}

	private static IEnumerable<MatchBoundStressShape> SelectTheShapesToTime(ImmutableArray<MatchBoundStressShape> shapes)
	{
		string? filter = Environment.GetEnvironmentVariable(SHAPE_FILTER_VARIABLE_NAME);
		string[] fragments = string.IsNullOrWhiteSpace(filter) ? [] : filter.Split(_SHAPE_FILTER_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		return fragments.Length == 0
			? shapes
			: shapes.Where(shape => fragments.Any(fragment => shape.Name.Contains(fragment, StringComparison.Ordinal)));
	}

	internal static bool TheTestAssemblyIsOptimized()
	{
		DebuggableAttribute? attribute = typeof(ShellMatchBoundTimingTests).Assembly.GetCustomAttribute<DebuggableAttribute>();

		return attribute is null || !attribute.IsJITOptimizerDisabled;
	}

	private static ShellShapeTiming Measure(MatchBoundStressShape shape)
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

				foreach ((string identifier, Regex expression) in ShellDraftExpressions.MatchBoundExpressions)
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

		return new ShellShapeTiming(shape.Name, text.Length, worstFile, [.. worstByExpression.OrderBy(pair => pair.Key, StringComparer.Ordinal)], [.. nonTerminating], timedOut);
	}

	private static string Describe(ImmutableArray<ShellShapeTiming> timings)
	{
		StringBuilder report = new();
		ShellShapeTiming worst = timings.OrderByDescending(timing => timing.WorstFileMilliseconds).First();
		ImmutableArray<string> columns = [.. ShellDraftExpressions.MatchBoundExpressions.Select(pair => pair.Identifier)];

		report.AppendLine($"# The cost gate of the Shell draft catalog. One row per stress shape; milliseconds, the worst of {MEASURED_ITERATIONS} measured iterations after {WARM_UP_ITERATIONS} warm-up.");
		report.AppendLine($"# Target: worst benign per-file figure about {TARGET_WORST_FILE_MILLISECONDS.ToString("F0", CultureInfo.InvariantCulture)} ms on the Linux VM. Worst shape here: {worst.ShapeName}, {worst.WorstFileMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms.");
		report.AppendLine($"# Build: {(TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's")}.");
		report.AppendLine(string.Format(CultureInfo.InvariantCulture, "# Environment: {0}; {1}; {2} logical processors; runtime {3}", Environment.OSVersion, System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture, Environment.ProcessorCount, Environment.Version));
		report.AppendLine($"shape\tcharacters\tfile_ms\t{string.Join('\t', columns)}");

		foreach (ShellShapeTiming timing in timings)
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
				string path = Path.Combine(stressFolder, ShellMatchBoundStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
	}
}

/// <summary>What the timing procedure measured for one `Shell` stress shape.</summary>
internal sealed record ShellShapeTiming(
	string ShapeName,
	int SizeInCharacters,
	double WorstFileMilliseconds,
	ImmutableArray<KeyValuePair<string, double>> WorstExpressionMilliseconds,
	ImmutableArray<string> NonTerminatingExpressions,
	bool AnExpressionTimedOut);
