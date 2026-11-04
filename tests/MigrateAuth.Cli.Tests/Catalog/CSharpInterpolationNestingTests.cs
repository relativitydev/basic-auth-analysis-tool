using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Lexical;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE THREE INTERPOLATED RULES OF THE `CSharp` ANALYZER COUNT THE BRACES NESTED
/// INSIDE A HOLE.
///
/// Every source is invented from the catalog's own generic forms and runs through the real embedded analyzer, the real
/// scanner and the real recognition engine. A hole that holds an object initializer, a lambda block body or a switch
/// expression closes at the brace that balances its opener; the stretch after the inner brace is code, and an anchor
/// there composes at Medium instead of the Low of the string body. The recovery policy of the scanner, the nested
/// string that holds a brace, and the doubled opening brace that the scanner reads as a hole opener are asserted as the
/// catalog declares them in the lexical profile section.
/// </summary>
public sealed class CSharpInterpolationNestingTests
{
	private const string _CS_008 = "CS-008";
	private const string _CREDENTIAL_CONSTRUCTION = "new NetworkCredential(user, password)";

	private const string _OBJECT_INITIALIZER_THEN_THE_ANCHOR =
		"var line = $\"{Describe(new Options { Retries = 3 }, new NetworkCredential(user, password))}\";";
	private const string _LAMBDA_BLOCK_BODY_THEN_MORE_CODE =
		"var line = $\"{items.Select(i => { return i; }).First()}:{password}\";";
	private const string _VERBATIM_SWITCH_EXPRESSION =
		"var line = $@\"{value switch { 1 => \"a\", _ => \"b\" }} {suffix}\";";
	private const string _VERBATIM_SWITCH_EXPRESSION_THEN_THE_ANCHOR =
		"var line = $@\"{Pick(value switch { 1 => \"a\", _ => \"b\" }, new NetworkCredential(user, password))}\";";
	private const string _AT_FIRST_OBJECT_INITIALIZER_THEN_THE_ANCHOR =
		"var line = @$\"{Describe(new Options { Retries = 3 }, new NetworkCredential(user, password))}\";";
	private const string _BODY_AFTER_THE_HOLE =
		"var line = $\"{new Options { A = 1 }.Name} new NetworkCredential(user, password)\";";
	private const string _NESTED_STRING_HOLDING_A_CLOSING_BRACE =
		"var line = $\"{Format(\"}\", value)} new NetworkCredential(user, password)\";";
	private const string _NESTED_STRING_HOLDING_AN_OPENING_BRACE =
		"var line = $\"{Format(\"{\", value)} new NetworkCredential(user, password)\";";
	private const string _HOLE_THAT_NEVER_CLOSES =
		"var line = $\"{Describe(new Options { Retries = 3 }, value)\nvar next = 1;\n";
	private const string _BALANCED_DOUBLED_OPENER = "var line = $\"{{text}} tail\";\nvar next = 1;\n";
	private const string _UNPAIRED_DOUBLED_OPENER = "var line = $\"literal {{ brace }\";\nvar next = 1;\n";

	[Fact]
	public void TheThreeInterpolatedRulesDeclareTheNestingOpening()
	{
		ILanguageAnalyzer analyzer = new CSharpRecognitionHarness().Analyzer;
		ImmutableArray<QuoteRule> interpolated =
		[
			.. analyzer.LexicalProfile.QuoteRulesInEngineOrder.Where(rule => rule.InterpolationOpening is not null)
		];

		Assert.Equal(3, interpolated.Length);
		Assert.All(interpolated, rule => Assert.Equal("{", rule.InterpolationNestingOpening));
		Assert.Equal(["$\"", "$@\"", "@$\""], interpolated.Select(rule => rule.Opening).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void AnObjectInitializerInAHoleLeavesTheRemainderAsCodeAndTheRealClosingAsADelimiter()
	{
		FileRecognitionContext context = new CSharpRecognitionHarness().ContextFor(_OBJECT_INITIALIZER_THEN_THE_ANCHOR);
		int realClosing = _OBJECT_INITIALIZER_THEN_THE_ANCHOR.IndexOf("))}\"", StringComparison.Ordinal) + 2;

		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, "}, new NetworkCredential(user, password))", LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, context.Mask.ZoneAt(realClosing));
		Assert.True(context.Mask.IsCompletelyValid);
		Assert.Equal(0, context.Mask.UnbalancedNestingCount);
	}

	[Fact]
	public void AnAnchorInTheRemainderAfterAnObjectInitializerComposesAtMedium()
	{
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(_OBJECT_INITIALIZER_THEN_THE_ANCHOR));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void ALambdaBlockBodyInAHoleLeavesTheRemainderAsCodeAndTheRealClosingAsADelimiter()
	{
		FileRecognitionContext context = new CSharpRecognitionHarness().ContextFor(_LAMBDA_BLOCK_BODY_THEN_MORE_CODE);
		int realClosing = _LAMBDA_BLOCK_BODY_THEN_MORE_CODE.IndexOf("()}:", StringComparison.Ordinal) + 2;

		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, "}).First()", LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, context.Mask.ZoneAt(realClosing));
		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, ":", LexicalZone.StringBody);
		Assert.True(context.Mask.IsCompletelyValid);
	}

	[Fact]
	public void ASwitchExpressionInAVerbatimHoleLeavesTheRemainderAsCodeAndTheRealClosingAsADelimiter()
	{
		FileRecognitionContext context = new CSharpRecognitionHarness().ContextFor(_VERBATIM_SWITCH_EXPRESSION);
		int realClosing = _VERBATIM_SWITCH_EXPRESSION.IndexOf("}} {suffix}", StringComparison.Ordinal) + 1;

		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, " }", LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, context.Mask.ZoneAt(realClosing));
		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, "suffix", LexicalZone.Code);
		Assert.True(context.Mask.IsCompletelyValid);
	}

	[Fact]
	public void AnAnchorInTheRemainderAfterAVerbatimSwitchExpressionComposesAtMedium()
	{
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(_VERBATIM_SWITCH_EXPRESSION_THEN_THE_ANCHOR));

		Assert.Equal(_CS_008, construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.Medium, construct.Level);
	}

	[Fact]
	public void TheAtFirstVerbatimInterpolatedFormCountsTheNestedBracesToo()
	{
		FileRecognitionContext context = new CSharpRecognitionHarness().ContextFor(_AT_FIRST_OBJECT_INITIALIZER_THEN_THE_ANCHOR);
		int realClosing = _AT_FIRST_OBJECT_INITIALIZER_THEN_THE_ANCHOR.IndexOf("))}\"", StringComparison.Ordinal) + 2;
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(_AT_FIRST_OBJECT_INITIALIZER_THEN_THE_ANCHOR));

		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, "}, new NetworkCredential(user, password))", LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, context.Mask.ZoneAt(realClosing));
		Assert.Equal((_CS_008, ConfidenceLevel.Medium), (construct.PatternClassIdentifier, construct.Level));
	}

	[Fact]
	public void AnchorShapedTextThatIsStringBodyAfterAHoleWithABracePairStaysStringBodyAndComposesLow()
	{
		FileRecognitionContext context = new CSharpRecognitionHarness().ContextFor(_BODY_AFTER_THE_HOLE);
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(_BODY_AFTER_THE_HOLE));

		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, _CREDENTIAL_CONSTRUCTION, LexicalZone.StringBody);
		Assert.Equal((_CS_008, ConfidenceLevel.Low), (construct.PatternClassIdentifier, construct.Level));
	}

	[Theory]
	[InlineData(_NESTED_STRING_HOLDING_A_CLOSING_BRACE)]
	[InlineData(_NESTED_STRING_HOLDING_AN_OPENING_BRACE)]
	public void ABraceInsideANestedStringIsNotCountedAndTheBodyAfterTheHoleStaysStringBodyAndLow(string source)
	{
		FileRecognitionContext context = new CSharpRecognitionHarness().ContextFor(source);
		RecognizedConstruct construct = Assert.Single(new CSharpRecognitionHarness().Recognize(source));

		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, _CREDENTIAL_CONSTRUCTION, LexicalZone.StringBody);
		Assert.Equal(0, context.Mask.UnbalancedNestingCount);
		Assert.Equal((_CS_008, ConfidenceLevel.Low), (construct.PatternClassIdentifier, construct.Level));
	}

	[Fact]
	public void AHoleThatHoldsANestedOpenerAndNeverClosesIsRecoveredWithOneAnomalyAndTheFileIsNotAbandoned()
	{
		ZoneMask mask = new CSharpRecognitionHarness().ContextFor(_HOLE_THAT_NEVER_CLOSES).Mask;

		Assert.Equal(1, mask.UnbalancedNestingCount);
		Assert.Equal(LexicalAnomalyReason.UnbalancedInterpolationNesting, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid);
		Assert.False(mask.WasAbandonedMidFile);
		ZoneAssertions.SpansZone(mask, _HOLE_THAT_NEVER_CLOSES, "Describe(new Options ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, _HOLE_THAT_NEVER_CLOSES, ", value)", LexicalZone.StringBody);
	}

	[Fact]
	public void ABalancedDoubledOpenerReadsItsTextAsCodeAndTheTailAsStringBody()
	{
		FileRecognitionContext context = new CSharpRecognitionHarness().ContextFor(_BALANCED_DOUBLED_OPENER);

		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, "text", LexicalZone.Code);
		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, " tail", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(context.Mask, context.NormalizedText, "var next = 1;", LexicalZone.Code);
		Assert.Equal(0, context.Mask.UnbalancedNestingCount);
		Assert.True(context.Mask.IsCompletelyValid);
	}

	[Fact]
	public void AnUnpairedDoubledOpenerThatRunsToTheEndOfTheFileRestoresTheFirstClosingReadWithOneAnomaly()
	{
		ZoneMask mask = new CSharpRecognitionHarness().ContextFor(_UNPAIRED_DOUBLED_OPENER).Mask;

		Assert.Equal(1, mask.UnbalancedNestingCount);
		Assert.Equal(LexicalAnomalyReason.UnbalancedInterpolationNesting, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid);
		Assert.False(mask.WasAbandonedMidFile);
	}
}
