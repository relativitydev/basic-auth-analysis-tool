using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Lexical;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE COST GATE OF THE `Go` DRAFT CATALOG (the gate run on the draft before the catalog was final), on the pattern of the `JavaScript` and `TypeScript` gates: the draft expressions of <see cref="GoDraftExpressions"/>
/// that the recognition engine charges to its match bound run over every shape of <see cref="GoDraftCostGateStressSet"/> on the system clock, and the test-local lexical profile of <see cref="GoShapedProfile"/>
/// scans the same shapes against the lexical budget. THE SHAPES ARE THE TYPICAL V1 AND THE WORST V2 DECODED FILES OF THE DESIGN (volume row C3), the long-line files (row C8) and the anchor-dense runs.
///
/// WHAT IS ASSERTED is what must hold whatever the aim: that no expression timed out; that no enumeration stalled (a match that begins before the previous one ended); that the worst per-file figure stays
/// under the MATCH BOUND in force, which is the figure the engine charges against; and that the lexical scan of every shape stays under the LEXICAL BUDGET in force and never trips it. The aim (the worst benign
/// per-file figure the design hopes for on the Linux VM) is printed beside the worst shape, so a figure above it is visible and has to be accepted before the catalog is final.
///
/// WHAT IS TIMED. For each shape and each expression, the whole enumeration of its matches over the shape's text, with the policy match timeout. The per-file figure of an iteration is the SUM over every
/// expression (the engine charges every rule, every content marker, the constant declaration and every evaluated flow signal to the one file accumulator; every shape is charged all of them, the conservative
/// reading). The tables are written where <see cref="REPORT_PATH_VARIABLE_NAME"/> and <see cref="LEXICAL_REPORT_PATH_VARIABLE_NAME"/> point.
///
/// THESE TESTS ARE WRITTEN AND COMPILED, NEVER RUN DURING THE BUILD: they are run on their own, on a machine with no other load, on Windows and on Linux.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class GoDraftCostGateTimingTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_GO";

	internal const string LEXICAL_REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_LEXICAL_BUDGET_REPORT_GO";

	internal const string STRESS_FOLDER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_STRESS_FOLDER_GO";

	/// <summary>When set to a comma-separated list of shape-name fragments, only the shapes whose name holds one of them are timed: a reshape probe, never the gate (the gate runs with it unset).</summary>
	internal const string SHAPE_FILTER_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_SHAPES_GO";

	/// <summary>The design aim: the worst benign per-file analyzer-expression time on the Linux VM. An aim, not a gate (the same figure the `JavaScript` and `Shell` gates print).</summary>
	internal const double AIM_WORST_FILE_MILLISECONDS = 975.0;

	/// <summary>On a build with the JIT optimizer disabled the generated expressions run several times slower than in the product: ten times the bound would still catch an expression that lost its bound.</summary>
	internal const double UNOPTIMIZED_MATCH_CEILING_MILLISECONDS = MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE * RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS;

	internal const double UNOPTIMIZED_LEXICAL_CEILING_MILLISECONDS = MatchBoundTimingTests.DECLARED_SAFETY_MULTIPLE * LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS;

	internal const int WARM_UP_ITERATIONS = 1;

	internal const int MEASURED_ITERATIONS = 3;

	private const char _SHAPE_FILTER_SEPARATOR = ',';

	private readonly ITestOutputHelper _output;

	public GoDraftCostGateTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheGoDraftExpressionsTerminateOnEveryBenignShapeAndTheWorstFileStaysUnderTheMatchBound()
	{
		ImmutableArray<MatchBoundStressShape> shapes = GoDraftCostGateStressSet.Build();
		ImmutableArray<GoShapeTiming> timings = [.. SelectTheShapesToTime(shapes).Select(MeasureTheExpressions)];
		string report = DescribeTheExpressions(timings);
		double ceiling = TheTestAssemblyIsOptimized() ? RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS : UNOPTIMIZED_MATCH_CEILING_MILLISECONDS;

		_output.WriteLine(report);
		WriteWhereRequested(REPORT_PATH_VARIABLE_NAME, report);
		WriteTheStressSetWhereRequested(shapes);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingExpressions));
		Assert.All(timings, timing => Assert.True(
			timing.WorstFileMilliseconds < ceiling,
			$"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against the match bound of {ceiling:F0} ms"));
	}

	[Fact]
	public void NoGoShapeSpendsTheLexicalBudgetOnTheDraftProfile()
	{
		ImmutableArray<MatchBoundStressShape> shapes = GoDraftCostGateStressSet.Build();
		ImmutableArray<GoLexicalTiming> scans = [.. SelectTheShapesToTime(shapes).Select(TimeTheScan)];
		double ceiling = TheTestAssemblyIsOptimized() ? LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS : UNOPTIMIZED_LEXICAL_CEILING_MILLISECONDS;
		string report = DescribeTheScans(scans, ceiling);

		_output.WriteLine(report);
		WriteWhereRequested(LEXICAL_REPORT_PATH_VARIABLE_NAME, report);

		Assert.All(scans, scan => Assert.False(scan.Tripped, scan.ShapeName));
		Assert.All(scans, scan => Assert.True(scan.ChargedMilliseconds < ceiling, $"{scan.ShapeName}: {scan.ChargedMilliseconds:F4} ms charged against {ceiling:F1} ms"));
	}

	internal static bool TheTestAssemblyIsOptimized()
	{
		DebuggableAttribute? attribute = typeof(GoDraftCostGateTimingTests).Assembly.GetCustomAttribute<DebuggableAttribute>();

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

	private static GoShapeTiming MeasureTheExpressions(MatchBoundStressShape shape)
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

				foreach ((string identifier, Regex expression) in GoDraftExpressions.MatchBoundExpressions)
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

		return new GoShapeTiming(shape.Name, text.Length, worstFile, [.. worstByExpression.OrderBy(pair => pair.Key, StringComparer.Ordinal)], [.. nonTerminating], timedOut);
	}

	private static GoLexicalTiming TimeTheScan(MatchBoundStressShape shape)
	{
		ChargeRecordingTimeProvider clock = new();
		LexicalScanner scanner = new(GoShapedProfile.Build(), clock);
		double charged = 0.0;
		double wholeScan = 0.0;
		bool abandoned = false;
		bool tripped = false;

		for (int iteration = 0; iteration < WARM_UP_ITERATIONS + MEASURED_ITERATIONS; iteration++)
		{
			clock.Reset();

			Stopwatch stopwatch = Stopwatch.StartNew();
			ZoneMask mask = scanner.Scan(shape.Text);

			stopwatch.Stop();
			abandoned |= mask.WasAbandonedMidFile;
			tripped |= mask.AnomalyReason is LexicalAnomalyReason.LexicalBudgetExceeded;

			if (iteration >= WARM_UP_ITERATIONS)
			{
				charged = Math.Max(charged, clock.ChargedMilliseconds);
				wholeScan = Math.Max(wholeScan, stopwatch.Elapsed.TotalMilliseconds);
			}
		}

		return new GoLexicalTiming(shape.Name, shape.Text.Length, charged, wholeScan, abandoned, tripped);
	}

	private static string DescribeTheExpressions(ImmutableArray<GoShapeTiming> timings)
	{
		StringBuilder report = new();
		GoShapeTiming worst = timings.OrderByDescending(timing => timing.WorstFileMilliseconds).First();
		ImmutableArray<string> columns = [.. GoDraftExpressions.MatchBoundExpressions.Select(pair => pair.Identifier)];

		report.AppendLine($"# The cost gate of the Go draft catalog. One row per stress shape; milliseconds, the worst of {MEASURED_ITERATIONS} measured iterations after {WARM_UP_ITERATIONS} warm-up.");
		report.AppendLine($"# Aim (not a gate): worst benign per-file figure about {AIM_WORST_FILE_MILLISECONDS.ToString("F0", CultureInfo.InvariantCulture)} ms on the Linux VM. Gate: under the match bound of {RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS} ms. Worst shape here: {worst.ShapeName}, {worst.WorstFileMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms.");
		report.AppendLine($"# Build: {(TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's")}.");
		report.AppendLine(string.Format(CultureInfo.InvariantCulture, "# Environment: {0}; {1}; {2} logical processors; runtime {3}", Environment.OSVersion, System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture, Environment.ProcessorCount, Environment.Version));
		report.AppendLine($"shape\tcharacters\tfile_ms\t{string.Join('\t', columns)}");

		foreach (GoShapeTiming timing in timings)
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

	private static string DescribeTheScans(ImmutableArray<GoLexicalTiming> scans, double ceiling)
	{
		StringBuilder report = new();
		GoLexicalTiming worst = scans.OrderByDescending(scan => scan.ChargedMilliseconds).First();

		report.AppendLine(string.Format(
			CultureInfo.InvariantCulture,
			"# Lexical budget of the Go draft profile: {0} warm-up and {1} measured iterations per shape, the worst reported; build {2}; budget in force {3:F1} ms",
			WARM_UP_ITERATIONS,
			MEASURED_ITERATIONS,
			TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug)",
			ceiling));
		report.AppendLine(string.Format(CultureInfo.InvariantCulture, "# Environment: {0}; {1}; {2} logical processors; runtime {3}", Environment.OSVersion, System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture, Environment.ProcessorCount, Environment.Version));
		report.AppendLine("shape\tcharacters\tcharged_ms\twhole_scan_ms\tabandoned\ttripped");

		foreach (GoLexicalTiming scan in scans)
		{
			report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2:F4}\t{3:F1}\t{4}\t{5}", scan.ShapeName, scan.SizeInCharacters, scan.ChargedMilliseconds, scan.WholeScanMilliseconds, scan.Abandoned, scan.Tripped));
		}

		report.AppendLine(string.Format(CultureInfo.InvariantCulture, "Worst charged per file {0:F4} ms on {1}", worst.ChargedMilliseconds, worst.ShapeName));

		return report.ToString();
	}

	private static void WriteWhereRequested(string variableName, string report)
	{
		string? reportPath = Environment.GetEnvironmentVariable(variableName);

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
				string path = Path.Combine(stressFolder, GoDraftCostGateStressSet.RelativePathOf(shape).Replace('/', Path.DirectorySeparatorChar));

				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, shape.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}
	}
}

/// <summary>What the timing procedure measured for one `Go` stress shape: the draft expressions.</summary>
internal sealed record GoShapeTiming(
	string ShapeName,
	int SizeInCharacters,
	double WorstFileMilliseconds,
	ImmutableArray<KeyValuePair<string, double>> WorstExpressionMilliseconds,
	ImmutableArray<string> NonTerminatingExpressions,
	bool AnExpressionTimedOut);

/// <summary>What the timing procedure measured for one `Go` stress shape: the lexical scan of the draft profile.</summary>
internal sealed record GoLexicalTiming(
	string ShapeName,
	int SizeInCharacters,
	double ChargedMilliseconds,
	double WholeScanMilliseconds,
	bool Abandoned,
	bool Tripped);
