using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE COST GATE OF THE `VBNet` ANALYZER: the registered expressions of
/// <see cref="VBNetRegisteredExpressions"/> run over every shape of <see cref="VBNetMatchBoundStressSet"/> on the system clock, and the worst per-file
/// analyzer-expression time is compared with <see cref="TARGET_WORST_FILE_MILLISECONDS"/>.
///
/// WHAT IS TIMED. For each shape and each registered expression, the whole enumeration of its matches over the shape's text, with the policy match timeout. The
/// per-file figure of an iteration is the SUM over every expression: the engine charges every rule, every content marker and every evaluated flow signal
/// to the one file accumulator. The flow signals are charged on every shape here, which is the conservative reading (the engine evaluates them only on
/// a file that holds a High or Medium construct). The worst of the measured iterations is the shape's figure; the worst shape is the gate's.
///
/// WHAT THIS ASSERTS. That no expression timed out; that no enumeration stalled (a match that begins before the previous one ended); and that the worst
/// per-file figure on the machine that runs it stays under the target. The table is written where <see cref="REPORT_PATH_VARIABLE_NAME"/> points.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class VBNetMatchBoundTimingTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_VBNET";

	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER_VBNET";

	/// <summary>The target: the worst benign per-file analyzer-expression time, in milliseconds.</summary>
	internal const double TARGET_WORST_FILE_MILLISECONDS = 975.0;

	/// <summary>
	/// The ceiling that stands in for the target when the test assembly was built with the JIT optimizer disabled (a Debug build), where the source-generated
	/// expressions run several times slower than in the Native AOT binary the target describes. It is ten times the target rounded up to the next 250 ms, which
	/// still catches an expression that has lost its bound; the target itself is asserted on the optimized build.
	/// </summary>
	internal const double UNOPTIMIZED_BUILD_CEILING_MILLISECONDS = 9750.0;

	internal const int WARM_UP_ITERATIONS = 2;

	internal const int MEASURED_ITERATIONS = 5;

	private readonly ITestOutputHelper _output;

	public VBNetMatchBoundTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheRegisteredExpressionsTerminateOnEveryBenignShapeAndTheWorstFileStaysUnderTheTarget()
	{
		ImmutableArray<MatchBoundStressShape> shapes = VBNetMatchBoundStressSet.Build();
		ImmutableArray<VBNetShapeTiming> timings = [.. shapes.Select(Measure)];
		string report = Describe(timings);

		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);
		WriteTheStressSetWhereRequested(shapes);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingExpressions));
		double ceiling = TheTestAssemblyIsOptimized() ? TARGET_WORST_FILE_MILLISECONDS : UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;
		Assert.All(timings, timing => Assert.True(timing.WorstFileMilliseconds < ceiling, $"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against {ceiling:F0} ms"));
	}

	private static bool TheTestAssemblyIsOptimized()
	{
		DebuggableAttribute? attribute = typeof(VBNetMatchBoundTimingTests).Assembly.GetCustomAttribute<DebuggableAttribute>();

		return attribute is null || !attribute.IsJITOptimizerDisabled;
	}

	private static VBNetShapeTiming Measure(MatchBoundStressShape shape)
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

				foreach ((string identifier, Regex expression) in VBNetRegisteredExpressions.All)
				{
					(double milliseconds, bool stalled) = TimeOne(expression, text);

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

		return new VBNetShapeTiming(shape.Name, text.Length, worstFile, [.. worstByExpression.OrderBy(pair => pair.Key, StringComparer.Ordinal)], [.. nonTerminating], timedOut);
	}

	/// <summary>The time of one expression's whole enumeration, and whether the enumeration stopped advancing.</summary>
	internal static (double Milliseconds, bool Stalled) TimeOne(Regex expression, string text)
	{
		Stopwatch watch = Stopwatch.StartNew();
		int previousEnd = -1;
		bool stalled = false;
		int count = 0;

		for (Match match = expression.Match(text); match.Success && !stalled; match = match.NextMatch())
		{
			stalled = match.Index < previousEnd || ++count > text.Length;
			previousEnd = match.Index + Math.Max(match.Length, 1);
		}

		watch.Stop();

		return (watch.Elapsed.TotalMilliseconds, stalled);
	}

	private static string Describe(ImmutableArray<VBNetShapeTiming> timings)
	{
		StringBuilder report = new();
		VBNetShapeTiming worst = timings.OrderByDescending(timing => timing.WorstFileMilliseconds).First();
		ImmutableArray<string> columns = [.. VBNetRegisteredExpressions.All.Select(pair => pair.Identifier)];

		report.AppendLine("# The cost gate of the VBNet catalog. One row per stress shape; milliseconds, the worst of " + MEASURED_ITERATIONS.ToString(CultureInfo.InvariantCulture) + " measured iterations after " + WARM_UP_ITERATIONS.ToString(CultureInfo.InvariantCulture) + " warm-up.");
		report.AppendLine("# Target: worst benign per-file figure about " + TARGET_WORST_FILE_MILLISECONDS.ToString("F0", CultureInfo.InvariantCulture) + " ms on the Linux VM. Worst shape here: " + worst.ShapeName + ", " + worst.WorstFileMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms.");
		report.AppendLine("# Build: " + (TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's") + ".");
		report.AppendLine("shape\tcharacters\tfile_ms\t" + string.Join('\t', columns));

		foreach (VBNetShapeTiming timing in timings)
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
				string path = Path.Combine(stressFolder, VBNetMatchBoundStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
	}
}

/// <summary>What the timing procedure measured for one stress shape.</summary>
internal sealed record VBNetShapeTiming(
	string ShapeName,
	int SizeInCharacters,
	double WorstFileMilliseconds,
	ImmutableArray<KeyValuePair<string, double>> WorstExpressionMilliseconds,
	ImmutableArray<string> NonTerminatingExpressions,
	bool AnExpressionTimedOut);
