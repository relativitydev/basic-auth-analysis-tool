using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Fixtures;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// VALIDATOR CODES 40 TO 43, one inconsistent declaration each, over the test-local here-document
/// exemplar: a whole-line anchoring on an opening, and a deferred body start without a whole-line
/// closing, without newlines as content, or with an anchored opening. A well-formed declaration
/// and every shipped profile are shown to violate none of them.
/// </summary>
public sealed class HereDocumentContractValidatorTests
{
	private const int _FIRST_AMENDMENT_CODE = 40;
	private const int _LAST_AMENDMENT_CODE = 43;
	private const int _WHOLE_LINE_ANCHORING_ON_AN_OPENING_CODE = 40;
	private const int _DEFERRED_BODY_START_WITHOUT_WHOLE_LINE_CLOSING_CODE = 41;
	private const int _DEFERRED_BODY_START_WITHOUT_ALLOWED_NEWLINES_CODE = 42;
	private const int _DEFERRED_BODY_START_WITH_OPENING_ANCHORING_CODE = 43;
	private const string _PLAIN_OPENING = "<<";
	private const string _LINE_COMMENT_OPENING = "#";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";

	[Fact]
	public void TheExemplarWithBothHereDocumentFormsIsWellFormed()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(new HereDocumentExemplarAnalyzer()));
	}

	[Theory]
	[InlineData(PositionalAnchoring.WholeLine)]
	[InlineData(PositionalAnchoring.WholeLineAfterLeadingTabs)]
	public void AWholeLineAnchoringOnAQuoteOpeningIsAViolation(PositionalAnchoring anchoring)
	{
		QuoteRule rule = BuildDeferredRule() with { OpeningAnchoring = anchoring };

		ImmutableArray<AnalyzerContractViolation> violations =
			AnalyzerContractValidator.Validate(WithQuoteRule(rule));

		Assert.Contains(violations, violation => violation.Code == AnalyzerContractViolationCode.WholeLineAnchoringOnAnOpening);
	}

	[Theory]
	[InlineData(PositionalAnchoring.WholeLine)]
	[InlineData(PositionalAnchoring.WholeLineAfterLeadingTabs)]
	public void AWholeLineAnchoringOnALineCommentOpeningIsAViolation(PositionalAnchoring anchoring)
	{
		LexicalProfile profile = BuildProfile(
			[new LineCommentRule { Opening = _LINE_COMMENT_OPENING, OpeningAnchoring = anchoring }],
			[]);

		ImmutableArray<AnalyzerContractViolation> violations =
			AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.WholeLineAnchoringOnAnOpening, violations[0].Code);
	}

	[Theory]
	[InlineData(PositionalAnchoring.WholeLine)]
	[InlineData(PositionalAnchoring.WholeLineAfterLeadingTabs)]
	public void AWholeLineAnchoringOnABlockCommentOpeningIsAViolationButOnItsClosingIsNot(PositionalAnchoring anchoring)
	{
		LexicalProfile onTheOpening = BuildProfile(
			[],
			[new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING, OpeningAnchoring = anchoring }]);
		LexicalProfile onTheClosing = BuildProfile(
			[],
			[new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING, ClosingAnchoring = anchoring }]);

		ImmutableArray<AnalyzerContractViolation> opening = AnalyzerContractValidator.Validate(WithProfile(onTheOpening));
		ImmutableArray<AnalyzerContractViolation> closing = AnalyzerContractValidator.Validate(WithProfile(onTheClosing));

		Assert.Single(opening);
		Assert.Equal(AnalyzerContractViolationCode.WholeLineAnchoringOnAnOpening, opening[0].Code);
		Assert.Empty(closing);
	}

	[Theory]
	[InlineData(PositionalAnchoring.Anywhere)]
	[InlineData(PositionalAnchoring.StartOfLine)]
	[InlineData(PositionalAnchoring.EndOfLine)]
	[InlineData(PositionalAnchoring.EndOfLineAfterOptionalBlanks)]
	public void ADeferredBodyStartWithAClosingThatIsNotWholeLineIsAViolation(PositionalAnchoring closingAnchoring)
	{
		QuoteRule rule = BuildDeferredRule() with { ClosingAnchoring = closingAnchoring };

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithQuoteRule(rule));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.DeferredBodyStartWithoutWholeLineClosing, violations[0].Code);
	}

	[Theory]
	[InlineData(NewlinePolicy.Forbidden)]
	[InlineData(NewlinePolicy.EscapedContinues)]
	public void ADeferredBodyStartWithoutNewlinesAsContentIsAViolation(NewlinePolicy newlinePolicy)
	{
		QuoteRule rule = BuildDeferredRule() with { NewlinePolicy = newlinePolicy };

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithQuoteRule(rule));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.DeferredBodyStartWithoutAllowedNewlines, violations[0].Code);
	}

	[Theory]
	[InlineData(PositionalAnchoring.StartOfLine)]
	[InlineData(PositionalAnchoring.EndOfLine)]
	[InlineData(PositionalAnchoring.EndOfLineAfterOptionalBlanks)]
	public void ADeferredBodyStartWithAnAnchoredOpeningIsAViolation(PositionalAnchoring openingAnchoring)
	{
		QuoteRule rule = BuildDeferredRule() with { OpeningAnchoring = openingAnchoring };

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithQuoteRule(rule));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.DeferredBodyStartWithOpeningAnchoring, violations[0].Code);
	}

	[Fact]
	public void AWholeLineClosingWithoutADeferredBodyStartIsWellFormed()
	{
		QuoteRule rule = HereDocumentExemplarAnalyzer.BuildHereDocumentRule(
			_PLAIN_OPENING,
			HereDocumentExemplarPatternExpressions.PlainOpening(),
			PositionalAnchoring.WholeLine,
			HereDocumentBodyStart.AfterTheOpening);

		Assert.Empty(AnalyzerContractValidator.Validate(WithQuoteRule(rule)));
	}

	[Fact]
	public void TheFourCodesKeepTheirNumbers()
	{
		Assert.Equal(_WHOLE_LINE_ANCHORING_ON_AN_OPENING_CODE, (int)AnalyzerContractViolationCode.WholeLineAnchoringOnAnOpening);
		Assert.Equal(_DEFERRED_BODY_START_WITHOUT_WHOLE_LINE_CLOSING_CODE, (int)AnalyzerContractViolationCode.DeferredBodyStartWithoutWholeLineClosing);
		Assert.Equal(_DEFERRED_BODY_START_WITHOUT_ALLOWED_NEWLINES_CODE, (int)AnalyzerContractViolationCode.DeferredBodyStartWithoutAllowedNewlines);
		Assert.Equal(_DEFERRED_BODY_START_WITH_OPENING_ANCHORING_CODE, (int)AnalyzerContractViolationCode.DeferredBodyStartWithOpeningAnchoring);
	}

	[Fact]
	public void NoShippedProfileViolatesAnyOfTheFourCodes()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach (ILanguageAnalyzer analyzer in catalog.AnalyzersByLanguageName.Values.Distinct())
		{
			Assert.DoesNotContain(
				AnalyzerContractValidator.Validate(analyzer),
				violation => (int)violation.Code is >= _FIRST_AMENDMENT_CODE and <= _LAST_AMENDMENT_CODE);
		}
	}

	private static QuoteRule BuildDeferredRule()
	{
		return HereDocumentExemplarAnalyzer.BuildHereDocumentRule(
			_PLAIN_OPENING,
			HereDocumentExemplarPatternExpressions.PlainOpening(),
			PositionalAnchoring.WholeLine,
			HereDocumentBodyStart.AfterTheOpeningLine);
	}

	private static ILanguageAnalyzer WithQuoteRule(QuoteRule rule)
	{
		return WithProfile(HereDocumentExemplarAnalyzer.BuildProfileWith([rule]));
	}

	private static LexicalProfile BuildProfile(
		IEnumerable<LineCommentRule> lineCommentRules,
		IEnumerable<BlockCommentRule> blockCommentRules)
	{
		LexicalProfile profile = new()
		{
			ProfileName = nameof(HereDocumentContractValidatorTests),
			ConstantDeclarationExpression = HereDocumentExemplarPatternExpressions.ConstantDeclaration(),
			LineCommentRules = lineCommentRules,
			BlockCommentRules = blockCommentRules,
			QuoteRules = []
		};

		return profile;
	}

	/// <summary>The exemplar's five other elements, unchanged, around the lexical profile under test.</summary>
	private static ILanguageAnalyzer WithProfile(LexicalProfile profile)
	{
		HereDocumentExemplarAnalyzer exemplar = new();
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LexicalProfile).Returns(profile);
		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(exemplar.LanguageIdentification);
		analyzer.SetupGet(supplied => supplied.DetectionRulesByPatternClass).Returns(exemplar.DetectionRulesByPatternClass);
		analyzer.SetupGet(supplied => supplied.DemotionSignals).Returns(exemplar.DemotionSignals);
		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation).Returns(exemplar.CandidateSelectionAndRecommendation);
		analyzer.SetupGet(supplied => supplied.SubsumptionRelation).Returns(exemplar.SubsumptionRelation);

		return analyzer.Object;
	}
}
