using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SEVENTEEN `CSharp` FLOW SIGNALS AT THE SIZE CEILING, OVER TEXT BUILT TO MAKE EACH ONE DO ITS
/// MOST WORK.
///
/// Every shape is legal, ordinary-looking C# - each repeats, up to the reader's file-size ceiling, the
/// construct that opens one signal's window or one signal's alternation without ever completing it:
/// a key read with no credential word near it, a posted-action attribute with no login-named method
/// after it, a secret-client type with no fetch, a console write with a long literal and no credential
/// word. Two properties are asserted, and only those: the whole engine finishes each shape inside the
/// provisional match bound with no trip, and every signal's own full enumeration over each shape
/// terminates, matching the interpreted form of the same expression - the guard against the
/// source-generator defect that makes an enumeration stop advancing.
///
/// The shapes are the flow-signal shapes of <see cref="MatchBoundStressSet"/>, so the match-bound
/// timing procedure measures the same text. The elapsed times are written to the test output, never
/// asserted as thresholds beyond the bound. The test is in a serialized collection because the bound
/// is charged in wall-clock time (see <see cref="TimingSensitiveStressCollection"/>).
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class CSharpFlowSignalStressTests
{
	private const int _MATCH_ENUMERATION_CAP = 2_000_000;

	private readonly ITestOutputHelper _output;

	public CSharpFlowSignalStressTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void EveryShapeIsRecognizedInsideTheMatchBoundWithNoFlowTripAndEverySignalEnumerationTerminates()
	{
		CSharpRecognitionHarness harness = new();
		ImmutableArray<FlowSignal> signals = harness.Analyzer.CandidateSelectionAndRecommendation.FlowSignals;
		StringBuilder table = new();

		foreach ((string name, string text) in Shapes())
		{
			FileRecognitionContext context = harness.ContextFor(text);
			RecognitionEngine engine = new(harness.EmissionPoint);
			Stopwatch stopwatch = Stopwatch.StartNew();
			RecognitionResult result = engine.Recognize(context);
			stopwatch.Stop();

			string line = string.Format(CultureInfo.InvariantCulture, "{0}: engine {1:N0} ms, charged {2:N0} ms tripped {3}", name, stopwatch.ElapsedMilliseconds, engine.LastFileMatchingTimeForDiagnostics.TotalMilliseconds, result.FlowEvaluationTripped);
			table.AppendLine(line);
			_output.WriteLine(line);

			Assert.NotEmpty(result.Constructs);
			Assert.Equal(1, engine.FilesEvaluatedForFlowSignals);
			Assert.False(result.FlowEvaluationTripped, name);
			Assert.DoesNotContain(harness.Sink.Received, fact => fact.Category is EmissionCategory.SkippedItem);
			Assert.True(stopwatch.ElapsedMilliseconds < RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS, name);

			AssertEverySignalEnumerationTerminatesAndAgreesWithTheInterpretedForm(signals, context.NormalizedText, name);
		}

		_output.WriteLine(table.ToString());
	}

	private static void AssertEverySignalEnumerationTerminatesAndAgreesWithTheInterpretedForm(
		ImmutableArray<FlowSignal> signals,
		string text,
		string shapeName)
	{
		foreach (FlowSignal signal in signals)
		{
			Regex interpreted = new(signal.Expression.ToString(), signal.Expression.Options, Regex.InfiniteMatchTimeout);

			int generatedCount = CountMatches(signal.Expression, text);
			int interpretedCount = CountMatches(interpreted, text);

			Assert.True(generatedCount < _MATCH_ENUMERATION_CAP, $"{shapeName}/{signal.Name}: the enumeration did not terminate.");
			Assert.True(generatedCount == interpretedCount, $"{shapeName}/{signal.Name}: generated {generatedCount}, interpreted {interpretedCount}.");
		}
	}

	private static IEnumerable<(string Name, string Text)> Shapes()
	{
		return MatchBoundStressSet.Build()
			.Where(shape => shape.Name.StartsWith(MatchBoundStressSet.FLOW_SHAPE_NAME_PREFIX, StringComparison.Ordinal))
			.Select(shape => (shape.Name, shape.Text));
	}

	private static int CountMatches(Regex expression, string text)
	{
		int count = 0;
		Match match = expression.Match(text);

		while (match.Success && count < _MATCH_ENUMERATION_CAP)
		{
			count++;
			match = match.NextMatch();
		}

		return count;
	}
}
