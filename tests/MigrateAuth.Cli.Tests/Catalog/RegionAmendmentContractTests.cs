using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Lexical;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// VALIDATOR CODES 44 AND 45 AND THE NO-EFFECT PROOF OF THE REGION VOCABULARY. The arm block and the
/// arithmetic context are profile vocabulary: this shows a well-formed declaration of each is accepted, an
/// inconsistent one is a violation of exactly its code, and no shipped profile but `Shell`'s declares either nor a
/// deferred body start, so no other shipped profile can take any of the scanner branches they guard. The branches themselves are
/// guarded by the declarations: the region state exists only for a profile that declares an arithmetic
/// context or a deferred body start, and a hole's arm block is read only where one is declared.
/// </summary>
public sealed class RegionAmendmentContractTests
{
	private const int _ARM_BLOCK_WITHOUT_NESTING_OPENING_CODE = 44;
	private const int _ARITHMETIC_CONTEXT_MALFORMED_CODE = 45;
	private const int _FIRST_REGION_AMENDMENT_CODE = 44;
	private const int _LAST_REGION_AMENDMENT_CODE = 45;
	private const string _OPENING_RUN = "((";
	private const string _CLOSING_RUN = "))";
	private const string _CASE_KEYWORD = "case";
	private const string _ESAC_KEYWORD = "esac";
	private const string _PREDECESSORS = ";\n";
	private const string _SUBSTITUTION_OPENING = "$(";
	private const string _SUBSTITUTION_CLOSING = ")";
	private const string _NESTING_OPENING = "(";
	private const string _DOUBLE_QUOTE = "\"";

	[Fact]
	public void AProfileDeclaringAnArmBlockAndAnArithmeticContextWellFormedIsAccepted()
	{
		LexicalProfile profile = BuildProfile(BuildHoleRule(nestingOpening: _NESTING_OPENING, BuildArmBlock()), BuildArithmeticContext());

		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(profile)));
	}

	[Fact]
	public void AnArmBlockWithoutANestingOpeningIsAViolation()
	{
		LexicalProfile profile = BuildProfile(BuildHoleRule(nestingOpening: null, BuildArmBlock()), arithmeticContext: null);

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.ArmBlockWithoutNestingOpening, violations[0].Code);
	}

	[Theory]
	[InlineData("", _ESAC_KEYWORD, _PREDECESSORS)]
	[InlineData(_CASE_KEYWORD, "", _PREDECESSORS)]
	[InlineData(_CASE_KEYWORD, _ESAC_KEYWORD, "")]
	public void AnArmBlockWithAnEmptyKeywordOrNoPredecessorIsAViolation(string opening, string closing, string predecessors)
	{
		ArmBlockRule armBlock = new() { OpeningKeyword = opening, ClosingKeyword = closing, CommandPositionPredecessors = predecessors };
		LexicalProfile profile = BuildProfile(BuildHoleRule(nestingOpening: _NESTING_OPENING, armBlock), arithmeticContext: null);

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.ArmBlockWithoutNestingOpening, violations[0].Code);
	}

	[Theory]
	[InlineData("(", ")")]
	[InlineData("([", "))")]
	[InlineData("((", "((")]
	[InlineData("((", "")]
	[InlineData("((", ")")]
	[InlineData("((", ")))")]
	[InlineData("(((", "))")]
	[InlineData("", "))")]
	public void AnArithmeticContextWhoseDelimitersAreNotRunsOfTwoBracketsIsAViolation(string opening, string closing)
	{
		ArithmeticContextRule context = new() { Opening = opening, Closing = closing };
		LexicalProfile profile = BuildProfile(BuildHoleRule(nestingOpening: _NESTING_OPENING, armBlock: null), context);

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.ArithmeticContextMalformed, violations[0].Code);
	}

	[Fact]
	public void AnArithmeticContextWhoseClosingRunHasTheLengthOfTheOpeningRunIsAccepted()
	{
		ArithmeticContextRule context = new() { Opening = "(((", Closing = ")))" };
		LexicalProfile profile = BuildProfile(BuildHoleRule(nestingOpening: _NESTING_OPENING, armBlock: null), context);

		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(profile)));
	}

	[Fact]
	public void ADecodingGroupAbsentFromTheOpeningExpressionIsAViolationAndAPresentOneIsNot()
	{
		QuoteRule deferred = HereDocumentExemplarAnalyzer.BuildHereDocumentRule(
			"<<",
			HereDocumentExemplarPatternExpressions.PlainOpening(),
			PositionalAnchoring.WholeLine,
			HereDocumentBodyStart.AfterTheOpeningLine);
		QuoteRule absent = deferred with { CapturedTerminator = deferred.CapturedTerminator! with { AnsiCQuotedCaptureGroupName = "absent" } };
		QuoteRule present = deferred with { CapturedTerminator = deferred.CapturedTerminator! with { AnsiCQuotedCaptureGroupName = "terminator" } };
		QuoteRule doubleQuotedAbsent = deferred with { CapturedTerminator = deferred.CapturedTerminator! with { DoubleQuotedCaptureGroupName = "absent" } };
		QuoteRule doubleQuotedPresent = deferred with { CapturedTerminator = deferred.CapturedTerminator! with { DoubleQuotedCaptureGroupName = "terminator" } };

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(BuildProfile(absent, arithmeticContext: null)));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.CapturedTerminatorGroupAbsentFromExpression, violations[0].Code);
		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(BuildProfile(present, arithmeticContext: null))));
		Assert.Single(AnalyzerContractValidator.Validate(WithProfile(BuildProfile(doubleQuotedAbsent, arithmeticContext: null))));
		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(BuildProfile(doubleQuotedPresent, arithmeticContext: null))));
	}

	[Theory]
	[InlineData(' ')]
	[InlineData('\t')]
	[InlineData('\n')]
	public void AWhitespaceCodeEscapeIsAViolation(char escape)
	{
		LexicalProfile profile = BuildProfileWith([BuildHoleRule(nestingOpening: _NESTING_OPENING, armBlock: null)], escape);

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.CodeEscapeCharacterIsWhitespace, violations[0].Code);
	}

	[Fact]
	public void AVisibleCodeEscapeIsAcceptedAndTheCodeKeepsItsNumber()
	{
		LexicalProfile profile = BuildProfileWith([BuildHoleRule(nestingOpening: _NESTING_OPENING, armBlock: null)], '\\');

		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(profile)));
		Assert.Equal(46, (int)AnalyzerContractViolationCode.CodeEscapeCharacterIsWhitespace);
	}

	[Fact]
	public void TwoArmBlocksOfTheSameContentAreAcceptedAndTwoOfDifferentContentAreAViolation()
	{
		QuoteRule first = BuildHoleRule(nestingOpening: _NESTING_OPENING, BuildArmBlock());
		QuoteRule sameContent = first with { Opening = "'", Closing = "'", InterpolationArmBlock = BuildArmBlock() };
		QuoteRule different = first with { Opening = "'", Closing = "'", InterpolationArmBlock = BuildArmBlock() with { ClosingKeyword = "fi" } };

		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(BuildProfileWith([first, sameContent], escape: null))));

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(BuildProfileWith([first, different], escape: null)));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.DifferentArmBlocksDeclared, violations[0].Code);
		Assert.Equal(47, (int)AnalyzerContractViolationCode.DifferentArmBlocksDeclared);
	}

	[Fact]
	public void TheTwoCodesKeepTheirNumbers()
	{
		Assert.Equal(_ARM_BLOCK_WITHOUT_NESTING_OPENING_CODE, (int)AnalyzerContractViolationCode.ArmBlockWithoutNestingOpening);
		Assert.Equal(_ARITHMETIC_CONTEXT_MALFORMED_CODE, (int)AnalyzerContractViolationCode.ArithmeticContextMalformed);
	}

	[Fact]
	public void NoShippedProfileViolatesEitherCode()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach (ILanguageAnalyzer analyzer in catalog.AnalyzersByLanguageName.Values.Distinct())
		{
			Assert.DoesNotContain(
				AnalyzerContractValidator.Validate(analyzer),
				violation => (int)violation.Code is >= _FIRST_REGION_AMENDMENT_CODE and <= _LAST_REGION_AMENDMENT_CODE);
		}
	}

	/// <summary>
	/// PROOF (a) OF THE REGION VOCABULARY: no shipped profile but `Shell`'s declares an arithmetic context, an arm block or a
	/// deferred body start, which are the three conditions under which the scanner keeps any region state or
	/// reads any of the region branches.
	/// </summary>
	[Fact]
	public void OnlyTheShellProfileDeclaresAnArithmeticContextAnArmBlockOrADeferredBodyStart()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		ILanguageAnalyzer[] analyzers = [.. catalog.AnalyzersByLanguageName.Values.Distinct()];
		ILanguageAnalyzer shell = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("Shell"));

		Assert.NotNull(shell.LexicalProfile.ArithmeticContext);
		Assert.NotNull(shell.LexicalProfile.CodeEscapeCharacter);
		Assert.Contains(shell.LexicalProfile.QuoteRulesInEngineOrder, rule => rule.InterpolationArmBlock is not null);
		Assert.Contains(shell.LexicalProfile.QuoteRulesInEngineOrder, rule => rule.CapturedTerminator is not null);
		Assert.All(analyzers.Where(analyzer => analyzer != shell), analyzer =>
		{
			Assert.Null(analyzer.LexicalProfile.ArithmeticContext);
			Assert.Null(analyzer.LexicalProfile.CodeEscapeCharacter);
			Assert.All(analyzer.LexicalProfile.QuoteRulesInEngineOrder, rule =>
			{
				Assert.Null(rule.InterpolationArmBlock);
				Assert.Null(rule.CapturedTerminator);
			});
		});
	}

	/// <summary>
	/// A profile that declares none of the three reads a case arm, a double parenthesis and a here-document
	/// inside a hole with none of the region branches: the arm closes the hole, the line-bound predicate
	/// decides arithmetic, and an opening whose body does not defer reads its body from the opening.
	/// </summary>
	[Fact]
	public void AProfileThatDeclaresNoneOfTheThreeReadsAsItDidBefore()
	{
		LexicalProfile profile = BuildProfile(BuildHoleRule(nestingOpening: _NESTING_OPENING, armBlock: null), arithmeticContext: null);
		const string source = "echo \"$(case a in a) echo hit;; esac)\" tail\n";

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		ZoneAssertions.SpansZone(mask, source, "case a in a", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " echo hit;; esac)", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
	}

	private static ArmBlockRule BuildArmBlock()
	{
		return new ArmBlockRule
		{
			OpeningKeyword = _CASE_KEYWORD,
			ClosingKeyword = _ESAC_KEYWORD,
			CommandPositionPredecessors = _PREDECESSORS
		};
	}

	private static ArithmeticContextRule BuildArithmeticContext()
	{
		return new ArithmeticContextRule { Opening = _OPENING_RUN, Closing = _CLOSING_RUN };
	}

	private static QuoteRule BuildHoleRule(string? nestingOpening, ArmBlockRule? armBlock)
	{
		return new QuoteRule
		{
			Opening = _DOUBLE_QUOTE,
			Closing = _DOUBLE_QUOTE,
			NewlinePolicy = NewlinePolicy.Allowed,
			InterpolationOpening = _SUBSTITUTION_OPENING,
			InterpolationClosing = _SUBSTITUTION_CLOSING,
			InterpolationNestingOpening = nestingOpening,
			InterpolationArmBlock = armBlock
		};
	}

	private static LexicalProfile BuildProfile(QuoteRule quoteRule, ArithmeticContextRule? arithmeticContext)
	{
		return new LexicalProfile
		{
			ProfileName = nameof(RegionAmendmentContractTests),
			ConstantDeclarationExpression = HereDocumentExemplarPatternExpressions.ConstantDeclaration(),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = [quoteRule],
			ArithmeticContext = arithmeticContext
		};
	}

	private static LexicalProfile BuildProfileWith(IEnumerable<QuoteRule> quoteRules, char? escape)
	{
		return new LexicalProfile
		{
			ProfileName = nameof(RegionAmendmentContractTests),
			ConstantDeclarationExpression = HereDocumentExemplarPatternExpressions.ConstantDeclaration(),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = quoteRules,
			CodeEscapeCharacter = escape
		};
	}

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
