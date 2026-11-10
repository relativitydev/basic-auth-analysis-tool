using System.Diagnostics;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// CS-018's declared, source-generated expression over the benign
/// stress shapes. Each right-empty concatenation form must finish a single <c>Match</c> call over
/// a zero-match shape at the size ceiling inside the match bound.
///
/// The shapes are scaled down to a fraction of their size and must enumerate within the same
/// fraction of the bound - the rate a full-size shape needs to finish inside the bound with the
/// margin the match-bound timing procedure's safety multiple assumes. The bound accumulates
/// matching-only time (see <see cref="RecognitionEnginePolicy"/>), and what this test times - one
/// rule's own <c>Match</c>/<c>NextMatch</c> enumeration, nothing else - is that same quantity.
///
/// Timing-sensitive: it asserts on, or is bounded by, wall-clock time, so it runs alone in the <c>TimingSensitiveStressCollection</c> collection and its name carries "Timing".
/// </summary>
[Collection(TimingSensitiveStressCollection.NAME)]
public sealed class CS018StressThroughputTimingTests
{
	private const string _CS_018 = "CS-018";

	/// <summary>Each shape is cut to this fraction of its size, at a line boundary.</summary>
	private const int _SCALE_DIVISOR = 4;

	/// <summary>The scaled shape must enumerate within the bound divided by this.</summary>
	private const int _TIME_BUDGET_DIVISOR = 4;

	private const char _LINE_BREAK = '\n';

	public static IEnumerable<object[]> StressShapeNames()
	{
		return MatchBoundStressSet.Build().Select(shape => new object[] { shape.Name });
	}

	[Theory]
	[MemberData(nameof(StressShapeNames))]
	public void CS018EnumeratesAScaledStressShapeWellInsideTheMatchBoundAndAlwaysAdvances(string shapeName)
	{
		CSharpRecognitionHarness harness = new();
		Regex expression = harness.Analyzer.DetectionRulesByPatternClass[_CS_018].Expression;
		string text = ScaledDown(MatchBoundStressSet.Build().Single(shape => shape.Name == shapeName).Text);
		double budget = RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS / (double)_TIME_BUDGET_DIVISOR;

		Stopwatch stopwatch = Stopwatch.StartNew();
		bool advances = EnumerationAdvances(expression, RecognitionHarness.Normalize(text));
		stopwatch.Stop();

		Assert.True(advances, $"{shapeName}: a match began before the previous one ended.");
		Assert.True(
			stopwatch.Elapsed.TotalMilliseconds < budget,
			$"{shapeName}: {stopwatch.Elapsed.TotalMilliseconds:F1} ms against a budget of {budget:F1} ms.");
	}

	/// <summary>
	/// The right-empty operand may not begin in the middle of an identifier, which must never clip
	/// a member-access chain, an invocation or a literal: the capture still starts at the operand's
	/// own first character.
	/// </summary>
	[Theory]
	[InlineData("string pair = settings.Token + \":\";", "settings.Token")]
	[InlineData("string pair = settings.Token + \":\" + \"\";", "settings.Token")]
	[InlineData("string pair = ReadToken(scope) + \":\";", "ReadToken(scope)")]
	[InlineData("string pair = \"fixed\" + \":\";", "\"fixed\"")]
	public void CS018RightEmptyOperandIsCapturedWholeFromItsFirstCharacter(string pairStatement, string expectedCredential)
	{
		CSharpRecognitionHarness harness = new();
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[_CS_018];
		string source = pairStatement + "\nstring encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));";

		Match match = rule.Expression.Match(RecognitionHarness.Normalize(source));

		Assert.True(match.Success);
		Assert.Equal(expectedCredential, match.Groups[rule.CredentialPosition.CaptureGroupName!].Value);
	}

	private static string ScaledDown(string text)
	{
		int cut = text.LastIndexOf(_LINE_BREAK, text.Length / _SCALE_DIVISOR);

		return text[..(cut + 1)];
	}

	private static bool EnumerationAdvances(Regex expression, string normalizedText)
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
}
