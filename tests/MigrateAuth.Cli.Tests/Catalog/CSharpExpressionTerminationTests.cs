using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.AcceptanceHarness;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE `[GeneratedRegex]` SOURCE-GENERATOR DEFECT OF SDK 10.0.201 / RUNTIME
/// 10.0.5, AND WHAT IT DOES AND DOES NOT REACH ACROSS THE `CSharp` CATALOG'S EIGHTEEN RULES.
///
/// A lazy bounded loop, <c>{0,N}?</c>, wrapped in an OPTIONAL group, whose upper bound is exhausted
/// while the continuation after it then fails: the source-generated matcher returns a "successful"
/// <see cref="Match"/> whose span does not contain its own declared group, and every subsequent
/// <see cref="Match.NextMatch"/> reproduces the identical match - an infinite enumeration in
/// <see cref="RecognitionEngine"/>'s own <c>while (match.Success)</c> loop, which nothing but a
/// wall-clock watch stops. <c>new Regex(pattern)</c> (the interpreter) reads the identical pattern
/// correctly throughout. CS-013's own <c>WcfBasicTransportCredentialConstruction</c> has an optional
/// credential capture, and expresses the window inside it as a nested required alternation rather than a loop
/// (see that method's own doc comment in <c>EmbeddedLanguageAnalyzers.CSharp.SecondHalf.cs</c>).
///
/// THIS SUITE PROVES TWO THINGS DIRECTLY AGAINST THE REGISTERED RULES' OWN EXPRESSIONS, NEVER A
/// HAND-BUILT SUBSTITUTE PATTERN: first, that CS-013's form terminates, agrees between
/// the generator and the interpreter, and still reports correctly through the real engine; second,
/// that every OTHER rule sharing the same <c>(?:[^\n]*\n){0,5}?</c> window shape - CS-009, CS-016,
/// CS-017, CS-018 and CS-019 - is not exposed, because in every one of them that window is
/// REQUIRED rather than optional, which the exposure table below records with its own reason.
///
/// EXPOSURE TABLE:
///
/// | Rule   | Window shape                              | Exposed | Reason                                    |
/// |--------|--------------------------------------------|---------|--------------------------------------------|
/// | CS-009 | `(?:[^\n]*\n){0,5}?...` REQUIRED            | No      | No enclosing optional group around the loop |
/// | CS-013 | `(?:...)?` OPTIONAL, nested alternation     | No      | No lazy bounded loop inside the optional group |
/// | CS-016 | `(?:[^\n]*\n){0,5}?...` REQUIRED            | No      | No enclosing optional group around the loop |
/// | CS-017 | `(?:[^\n]*\n){0,5}?...` REQUIRED            | No      | No enclosing optional group around the loop |
/// | CS-018 | `(?:[^\n]*\n){0,5}?...` REQUIRED            | No      | No enclosing optional group around the loop |
/// | CS-019 | `(?:[^\n]*\n){0,5}?...` REQUIRED            | No      | No enclosing optional group around the loop |
///
/// Every other rule (CS-001..CS-008, CS-011, CS-012, CS-014, CS-015) carries no lazy bounded loop
/// at all - CS-007 and CS-016's own <c>[^{}]*?</c> object-initializer spans are lazy but UNBOUNDED
/// (no <c>{0,N}</c> ceiling to exhaust) and sit inside a REQUIRED <c>{...}</c> shape, not an
/// optional one, so neither the diagnosed mechanism nor the shape it requires is present.
/// </summary>
public sealed class CSharpExpressionTerminationTests
{
	private const string _CS_009 = "CS-009";
	private const string _CS_013 = "CS-013";
	private const string _CS_016 = "CS-016";
	private const string _CS_017 = "CS-017";
	private const string _CS_018 = "CS-018";
	private const string _CS_019 = "CS-019";

	private const int _EXPECTED_PATTERN_CLASS_COUNT = 19;

	private const string _CREDENTIAL_CAPTURE_GROUP = "credential";

	/// <summary>
	/// Generous relative to every fixture below, which never legitimately produces more than two
	/// matches - reaching it is itself the proof that an enumeration failed to terminate naturally.
	/// </summary>
	private const int _MATCH_ENUMERATION_CAP = 1_000;

	private const string _CS_013_BINDING_LINE =
		"binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;";

	private const string _CS_013_PASSWORD_LINE_FORMAT =
		"client.ClientCredentials.UserName.Password = \"hunter{0}\";";

	public static IEnumerable<object[]> CanonicalPositiveFixtures()
	{
		foreach ((string patternClass, string source) in CSharpAnalyzerVolumeProofTests.CanonicalConstructs)
		{
			yield return [patternClass, source];
		}
	}

	// ------------------------------------------------------------------------------------------
	// Sanity: the table above stays in step with the catalog's own nineteen classes.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void NineteenPatternClassesAreCoveredByTheCanonicalFixtureTable()
	{
		CSharpRecognitionHarness harness = new();

		Assert.Equal(_EXPECTED_PATTERN_CLASS_COUNT, CSharpAnalyzerVolumeProofTests.CanonicalConstructs.Length);
		Assert.Equal(_EXPECTED_PATTERN_CLASS_COUNT, harness.Analyzer.DetectionRulesByPatternClass.Count);
	}

	// ------------------------------------------------------------------------------------------
	// 3a. CS-013: the declared, source-generated expression itself.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS013DeclaredExpressionTerminatesOverBindingOnlyFollowedBySixFurtherLines()
	{
		CSharpRecognitionHarness harness = new();
		string source = BuildCS013BindingOnlyFollowedByFillerLines(fillerLineCount: 6, blankFiller: false);

		AssertCS013BindingOnlyEnumerationTerminatesCorrectly(harness, source);
	}

	[Fact]
	public void CS013DeclaredExpressionTerminatesOverBindingOnlyFollowedBySixBlankLines()
	{
		CSharpRecognitionHarness harness = new();
		string source = BuildCS013BindingOnlyFollowedByFillerLines(fillerLineCount: 6, blankFiller: true);

		AssertCS013BindingOnlyEnumerationTerminatesCorrectly(harness, source);
	}

	[Fact]
	public void CS013DeclaredExpressionTerminatesOverBindingOnlyFollowedByManyMoreThanSixLines()
	{
		CSharpRecognitionHarness harness = new();
		string source = BuildCS013BindingOnlyFollowedByFillerLines(fillerLineCount: 40, blankFiller: false);

		AssertCS013BindingOnlyEnumerationTerminatesCorrectly(harness, source);
	}

	private static void AssertCS013BindingOnlyEnumerationTerminatesCorrectly(CSharpRecognitionHarness harness, string source)
	{
		FileRecognitionContext context = harness.ContextFor(source);
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[_CS_013];

		ImmutableArray<Match> matches = EnumerateWithCap(rule.Expression, context.NormalizedText, _MATCH_ENUMERATION_CAP);

		Assert.True(matches.Length < _MATCH_ENUMERATION_CAP, "CS-013 enumeration did not terminate within the cap.");
		Match construct = Assert.Single(matches);
		AssertEverySpanContainsItsAnchor(rule, matches);
		Assert.False(construct.Groups[_CREDENTIAL_CAPTURE_GROUP].Success);
	}

	// ------------------------------------------------------------------------------------------
	// 3a. Generator-vs-interpreter parity for CS-013's form, across the window.
	// ------------------------------------------------------------------------------------------

	/// <summary>
	/// <c>fillerLineCount</c> unrelated lines between the binding and the password puts the
	/// password <c>fillerLineCount + 1</c> window repetitions away - the binding's own line is the
	/// first repetition, since the window's first exit already consumes the REST of the binding's
	/// own line. Five repetitions is the bound, so zero through four filler lines still
	/// captures and five or more does not - the theories below are split on exactly that boundary.
	/// </summary>
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	public void CS013GeneratedAndInterpretedFormsAgreeOnAPasswordWithinTheWindow(int fillerLineCount)
	{
		CSharpRecognitionHarness harness = new();
		string source = BuildCS013SourceWithPasswordAfterFillerLines(fillerLineCount);

		AssertGeneratedAndInterpretedCS013FormsAgree(harness, source, expectCredentialCaptured: true);
	}

	[Theory]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(9)]
	public void CS013GeneratedAndInterpretedFormsAgreeWhenThePasswordIsBeyondTheWindow(int fillerLineCount)
	{
		CSharpRecognitionHarness harness = new();
		string source = BuildCS013SourceWithPasswordAfterFillerLines(fillerLineCount);

		AssertGeneratedAndInterpretedCS013FormsAgree(harness, source, expectCredentialCaptured: false);
	}

	[Fact]
	public void CS013GeneratedAndInterpretedFormsAgreeWhenThePasswordIsAbsentEntirely()
	{
		CSharpRecognitionHarness harness = new();

		AssertGeneratedAndInterpretedCS013FormsAgree(harness, _CS_013_BINDING_LINE, expectCredentialCaptured: false);
	}

	private static string BuildCS013SourceWithPasswordAfterFillerLines(int fillerLineCount)
	{
		List<string> lines = [_CS_013_BINDING_LINE];

		for (int fillerIndex = 1; fillerIndex <= fillerLineCount; fillerIndex++)
		{
			lines.Add($"var filler{fillerIndex} = {fillerIndex};");
		}

		lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, _CS_013_PASSWORD_LINE_FORMAT, fillerLineCount));

		return string.Join("\n", lines);
	}

	private static void AssertGeneratedAndInterpretedCS013FormsAgree(
		CSharpRecognitionHarness harness,
		string source,
		bool expectCredentialCaptured)
	{
		FileRecognitionContext context = harness.ContextFor(source);
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[_CS_013];
		Regex generated = rule.Expression;
		Regex interpreted = new(generated.ToString(), generated.Options, generated.MatchTimeout);

		ImmutableArray<Match> generatedMatches = EnumerateWithCap(generated, context.NormalizedText, _MATCH_ENUMERATION_CAP);
		ImmutableArray<Match> interpretedMatches = EnumerateWithCap(interpreted, context.NormalizedText, _MATCH_ENUMERATION_CAP);

		Assert.True(generatedMatches.Length < _MATCH_ENUMERATION_CAP, "generated enumeration did not terminate within the cap.");
		Assert.True(interpretedMatches.Length < _MATCH_ENUMERATION_CAP, "interpreted enumeration did not terminate within the cap.");

		Match generatedMatch = Assert.Single(generatedMatches);
		Match interpretedMatch = Assert.Single(interpretedMatches);

		Assert.Equal(interpretedMatch.Index, generatedMatch.Index);
		Assert.Equal(interpretedMatch.Length, generatedMatch.Length);
		Assert.Equal(expectCredentialCaptured, generatedMatch.Groups[_CREDENTIAL_CAPTURE_GROUP].Success);
		Assert.Equal(interpretedMatch.Groups[_CREDENTIAL_CAPTURE_GROUP].Success, generatedMatch.Groups[_CREDENTIAL_CAPTURE_GROUP].Success);

		if (expectCredentialCaptured)
		{
			Assert.Equal(interpretedMatch.Groups[_CREDENTIAL_CAPTURE_GROUP].Value, generatedMatch.Groups[_CREDENTIAL_CAPTURE_GROUP].Value);
		}
	}

	// ------------------------------------------------------------------------------------------
	// 3b. All eighteen rules, over their own positive fixture, doubled with filler between.
	// ------------------------------------------------------------------------------------------

	[Theory]
	[MemberData(nameof(CanonicalPositiveFixtures))]
	public void EachOfTheEighteenRulesEnumeratesToTerminationOverItsOwnRepeatedPositiveFixture(
		string patternClass,
		string canonicalSource)
	{
		CSharpRecognitionHarness harness = new();
		string source = BuildRepeatedFixtureTextWithFillerBetween(canonicalSource);
		FileRecognitionContext context = harness.ContextFor(source);
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[patternClass];

		ImmutableArray<Match> matches = EnumerateWithCap(rule.Expression, context.NormalizedText, _MATCH_ENUMERATION_CAP);

		Assert.True(matches.Length < _MATCH_ENUMERATION_CAP, $"{patternClass}: enumeration did not terminate within the cap.");
		Assert.Equal(2, matches.Length);
		AssertEverySpanContainsItsAnchor(rule, matches);
		AssertSuccessiveMatchesStrictlyAdvance(matches);
	}

	private static string BuildRepeatedFixtureTextWithFillerBetween(string canonicalSource)
	{
		const int fillerLineCount = 6;
		string block = canonicalSource + "\n" + BuildFillerLines(fillerLineCount) + "\n";

		return block + block;
	}

	// ------------------------------------------------------------------------------------------
	// 2. The exposure table's negative half: CS-009, CS-016, CS-017, CS-018, CS-019's own required
	// windows, exercised past their bound. A required window that finds nothing must fail the
	// WHOLE match rather than succeed with a corrupted span, which is what "required" means.

	[Theory]
	[InlineData(_CS_009)]
	[InlineData(_CS_016)]
	[InlineData(_CS_017)]
	[InlineData(_CS_018)]
	[InlineData(_CS_019)]
	public void EachRequiredFiveLineWindowFailsCleanlyRatherThanCorruptWhenItsContinuationIsBeyondTheBound(
		string patternClass)
	{
		CSharpRecognitionHarness harness = new();
		string source = BuildBeyondTheWindowSourceFor(patternClass);
		FileRecognitionContext context = harness.ContextFor(source);
		DetectionRule rule = harness.Analyzer.DetectionRulesByPatternClass[patternClass];

		ImmutableArray<Match> matches = EnumerateWithCap(rule.Expression, context.NormalizedText, _MATCH_ENUMERATION_CAP);

		Assert.True(matches.Length < _MATCH_ENUMERATION_CAP, $"{patternClass}: enumeration did not terminate within the cap.");
		Assert.Empty(matches);
	}

	/// <summary>
	/// Each required-window class's own first statement, followed by more than five lines of
	/// filler before its own required continuation - out of reach of the bound, so the class must
	/// not match at all.
	/// </summary>
	private static string BuildBeyondTheWindowSourceFor(string patternClass)
	{
		const int fillerLineCount = 10;
		string firstStatement = patternClass switch
		{
			_CS_009 => "string pair = identifier + \":\" + secret;",
			_CS_016 => "NetworkCredential credential = new NetworkCredential { UserName = userName };",
			_CS_017 => "importJob.Settings.RelativityUsername = userName;",
			_CS_018 => "string pair = \"\" + \":\" + accessToken;",
			_CS_019 => "var sink = new SmtpClient(host, port);",
			_ => throw new ArgumentOutOfRangeException(nameof(patternClass), patternClass, "Unrecognized pattern class.")
		};
		string secondStatement = patternClass switch
		{
			_CS_009 => "string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));",
			_CS_016 => "credential.Password = password;",
			_CS_017 => "importJob.Settings.RelativityPassword = password;",
			_CS_018 => "string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pair));",
			_CS_019 => "sink.Credentials = new NetworkCredential(userName, password);",
			_ => throw new ArgumentOutOfRangeException(nameof(patternClass), patternClass, "Unrecognized pattern class.")
		};

		return firstStatement + "\n" + BuildFillerLines(fillerLineCount) + "\n" + secondStatement;
	}

	private static string BuildFillerLines(int count)
	{
		IEnumerable<string> lines = Enumerable.Range(1, count).Select(index => $"var innocuousFiller{index} = {index};");

		return string.Join("\n", lines);
	}

	// ------------------------------------------------------------------------------------------
	// 3c. The real engine, the real match bound, on CS-013's binding-only form.
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void CS013BindingOnlyFormAnalysesCleanlyThroughTheRealEngineWithNoMatchBoundTrip()
	{
		CSharpRecognitionHarness harness = new();
		string source = BuildCS013BindingOnlyFollowedByFillerLines(fillerLineCount: 6, blankFiller: false);
		FileRecognitionContext context = harness.ContextFor(source);
		RecognitionEngine engine = new(harness.EmissionPoint);

		ImmutableArray<RecognizedConstruct> recognized = engine.Recognize(context).Constructs;

		ImmutableArray<EmittedFact> skipped =
			[.. harness.Sink.Received.Where(fact => fact.Category == EmissionCategory.SkippedItem)];

		Assert.Empty(skipped);

		RecognizedConstruct construct = Assert.Single(recognized);
		Assert.Equal(_CS_013, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
		Assert.False(construct.LiteralCredentialSignal);
	}

	// ------------------------------------------------------------------------------------------
	// Shared helpers.
	// ------------------------------------------------------------------------------------------

	private static string BuildCS013BindingOnlyFollowedByFillerLines(int fillerLineCount, bool blankFiller)
	{
		IEnumerable<string> fillerLines = Enumerable.Range(1, fillerLineCount)
			.Select(index => blankFiller ? string.Empty : $"var filler{index} = {index};");

		return string.Join("\n", new[] { _CS_013_BINDING_LINE }.Concat(fillerLines));
	}

	/// <summary>
	/// Enumerates <c>Match</c>/<c>NextMatch</c> under a hard cap rather than an unbounded
	/// <c>while</c> loop, which is exactly the shape <see cref="RecognitionEngine"/> itself avoids
	/// only through a wall-clock watch this test does not have. Reaching the cap - rather than
	/// <see cref="Match.Success"/> becoming <see langword="false"/> on its own - is the failure this
	/// suite is written to catch.
	/// </summary>
	private static ImmutableArray<Match> EnumerateWithCap(Regex expression, string text, int cap)
	{
		List<Match> matches = [];
		Match match = expression.Match(text);

		while (match.Success && matches.Count < cap)
		{
			matches.Add(match);
			match = match.NextMatch();
		}

		return [.. matches];
	}

	private static void AssertEverySpanContainsItsAnchor(DetectionRule rule, ImmutableArray<Match> matches)
	{
		foreach (Match match in matches)
		{
			bool resolved = rule.AnchorPosition.TryResolveOffset(match, out int anchorOffset);

			Assert.True(resolved, "the rule's anchor position did not resolve against its own match.");
			Assert.True(
				anchorOffset >= match.Index && anchorOffset < match.Index + match.Length,
				$"anchor offset {anchorOffset} falls outside match span [{match.Index}, {match.Index + match.Length}).");
		}
	}

	private static void AssertSuccessiveMatchesStrictlyAdvance(ImmutableArray<Match> matches)
	{
		for (int matchIndex = 1; matchIndex < matches.Length; matchIndex++)
		{
			Assert.True(
				matches[matchIndex].Index > matches[matchIndex - 1].Index,
				"successive matches must strictly advance rather than repeat the same position.");
		}
	}
}
