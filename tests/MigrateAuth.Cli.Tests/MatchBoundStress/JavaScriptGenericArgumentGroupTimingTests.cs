using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE TIMING PROOF OF THE GENERIC-ARGUMENT GROUP OF THE CALLEE EXPRESSIONS OF `JS-001`, `JS-003`, `JS-015` AND `JS-016`: the group has its own timing proof in the budget gate, because the
/// shared windows treat an angle bracket as a plain character. Each adversarial text of <see cref="JavaScriptGenericArgumentGroupStressSet"/>, at the file ceiling of two million characters, is read through the whole
/// enumeration of each of the four shipped expressions on the system clock, and the sum over the four is the per-file figure (the engine charges every rule to one file accumulator).
///
/// WHAT THIS ASSERTS: that no expression raised the policy match timeout, that no enumeration stalled, and that the worst per-file figure stays under the MATCH BOUND in force. The figures are printed (and written where
/// <see cref="REPORT_PATH_VARIABLE_NAME"/> points). A timeout here can be a source-generator defect and not catastrophic backtracking: the structural guard
/// has zero defective sites for these expressions, which is the first thing to check.
///
/// Timing-sensitive: its name carries "Timing" and it runs alone in the <c>TimingSensitiveStressCollection</c> collection.
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class JavaScriptGenericArgumentGroupTimingTests
{
	internal const string REPORT_PATH_VARIABLE_NAME = "MIGRATE_AUTH_MATCH_BOUND_REPORT_JAVASCRIPT_GENERIC_GROUP";

	private static readonly ImmutableArray<string> _FOUR_IDS = ["JS-001", "JS-003", "JS-015", "JS-016"];

	private readonly ITestOutputHelper _output;

	public JavaScriptGenericArgumentGroupTimingTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void TheGenericArgumentGroupTerminatesOnEveryAdversarialTextAtTheFileCeilingAndTheWorstFileStaysUnderTheMatchBound()
	{
		ImmutableArray<JavaScriptGenericArgumentGroupStressSet.Shape> shapes = JavaScriptGenericArgumentGroupStressSet.Build(JavaScriptGenericArgumentGroupStressSet.FILE_CEILING_CHARACTERS);
		ImmutableArray<(string Id, Regex Expression)> expressions =
		[
			.. _FOUR_IDS.Select(id => (id, JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[id].Expression))
		];
		ImmutableArray<GenericGroupShapeTiming> timings = [.. shapes.Select(shape => Measure(shape, expressions))];
		string report = Describe(timings);
		double ceiling = JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized()
			? RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS
			: JavaScriptShippedCostGateTimingTests.UNOPTIMIZED_BUILD_CEILING_MILLISECONDS;

		_output.WriteLine(report);
		WriteTheReportWhereRequested(report);

		Assert.All(timings, timing => Assert.False(timing.AnExpressionTimedOut, timing.ShapeName));
		Assert.All(timings, timing => Assert.Empty(timing.NonTerminatingExpressions));
		Assert.All(timings, timing => Assert.True(timing.WorstFileMilliseconds < ceiling, $"{timing.ShapeName}: {timing.WorstFileMilliseconds:F1} ms against {ceiling:F0} ms"));
	}

	private static GenericGroupShapeTiming Measure(JavaScriptGenericArgumentGroupStressSet.Shape shape, ImmutableArray<(string Id, Regex Expression)> expressions)
	{
		Dictionary<string, double> worstByExpression = [];
		List<string> nonTerminating = [];
		double worstFile = 0.0;
		bool timedOut = false;

		try
		{
			for (int iteration = 0; iteration < TypeScriptMatchBoundTimingTests.WARM_UP_ITERATIONS + TypeScriptMatchBoundTimingTests.MEASURED_ITERATIONS; iteration++)
			{
				double file = 0.0;

				foreach ((string id, Regex expression) in expressions)
				{
					(double milliseconds, bool stalled) = VBNetMatchBoundTimingTests.TimeOne(expression, shape.Text);

					file += milliseconds;

					if (stalled && !nonTerminating.Contains(id))
					{
						nonTerminating.Add(id);
					}

					if (iteration >= TypeScriptMatchBoundTimingTests.WARM_UP_ITERATIONS)
					{
						worstByExpression[id] = Math.Max(worstByExpression.GetValueOrDefault(id), milliseconds);
					}
				}

				if (iteration >= TypeScriptMatchBoundTimingTests.WARM_UP_ITERATIONS)
				{
					worstFile = Math.Max(worstFile, file);
				}
			}
		}
		catch (RegexMatchTimeoutException)
		{
			timedOut = true;
		}

		return new GenericGroupShapeTiming(shape.Name, shape.Text.Length, worstFile, [.. worstByExpression.OrderBy(pair => pair.Key, StringComparer.Ordinal)], [.. nonTerminating], timedOut);
	}

	private static string Describe(ImmutableArray<GenericGroupShapeTiming> timings)
	{
		StringBuilder report = new();

		report.AppendLine($"# The timing proof of the N3 generic-argument group. One row per adversarial shape at the file ceiling; milliseconds, the worst of {TypeScriptMatchBoundTimingTests.MEASURED_ITERATIONS} measured iterations after {TypeScriptMatchBoundTimingTests.WARM_UP_ITERATIONS} warm-up.");
		report.AppendLine($"# Gate: under the match bound of {RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS:F0} ms. Build: {(JavaScriptMatchBoundTimingTests.TheTestAssemblyIsOptimized() ? "optimized (Release)" : "JIT optimizer disabled (Debug), so the figures are not the product's")}.");
		report.AppendLine(string.Format(CultureInfo.InvariantCulture, "# Environment: {0}; {1}; {2} logical processors; runtime {3}", Environment.OSVersion, System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture, Environment.ProcessorCount, Environment.Version));
		report.AppendLine($"shape\tcharacters\tfile_ms\t{string.Join('\t', _FOUR_IDS)}");

		foreach (GenericGroupShapeTiming timing in timings)
		{
			report.Append(timing.ShapeName).Append('\t').Append(timing.SizeInCharacters.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(timing.WorstFileMilliseconds.ToString("F1", CultureInfo.InvariantCulture));

			foreach (string id in _FOUR_IDS)
			{
				double value = timing.WorstExpressionMilliseconds.FirstOrDefault(pair => pair.Key == id).Value;

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

	/// <summary>What the timing procedure measured for one adversarial shape of the generic-argument group.</summary>
	internal sealed record GenericGroupShapeTiming(
		string ShapeName,
		int SizeInCharacters,
		double WorstFileMilliseconds,
		ImmutableArray<KeyValuePair<string, double>> WorstExpressionMilliseconds,
		ImmutableArray<string> NonTerminatingExpressions,
		bool AnExpressionTimedOut);
}
