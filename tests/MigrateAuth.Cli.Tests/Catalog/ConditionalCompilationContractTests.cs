using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Lexical;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// VALIDATOR CODES 77 AND 78 AND THE NO-EFFECT PROOF OF THE CONDITIONAL-COMPILATION ELEMENT. The element is profile
/// vocabulary: a well-formed declaration is accepted, each malformed one is a violation of exactly its code, and only the
/// `CSharp` profile declares it, so `VBNet`, `PowerShell`, `Shell`, `JavaScript` and `TypeScript` are read without it and the
/// engine holds no language test.
/// </summary>
public sealed class ConditionalCompilationContractTests
{
	private const int _DIRECTIVE_OPENING_MALFORMED_CODE = 77;
	private const int _WELL_KNOWN_WORD_MALFORMED_CODE = 78;
	private const string _DIRECTIVE_OPENING = "%";
	private const string _SHELL_LINE_COMMENT_OPENING = "#";
	private const string _CSHARP_LANGUAGE_NAME = "CSharp";

	[Fact]
	public void TheCodesFollowTheHighestCodeOfTheLine()
	{
		Assert.Equal(_DIRECTIVE_OPENING_MALFORMED_CODE, (int)AnalyzerContractViolationCode.ConditionalCompilationDirectiveOpeningMalformed);
		Assert.Equal(_WELL_KNOWN_WORD_MALFORMED_CODE, (int)AnalyzerContractViolationCode.ConditionalCompilationWellKnownSymbolWordMalformed);
	}

	[Fact]
	public void AWellFormedElementAndNoElementAreBothAccepted()
	{
		Assert.Empty(Validate(Element(_DIRECTIVE_OPENING, "ALPHA", $"BETA{ConditionalCompilationRule.PREFIX_WILDCARD_MARKER}", $"GAMMA{ConditionalCompilationRule.VERSION_TAIL_MARKER}")));
		Assert.Empty(Validate(null));
	}

	[Fact]
	public void AnElementWithNoWellKnownWordIsAccepted()
	{
		Assert.Empty(Validate(Element(_DIRECTIVE_OPENING)));
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("% ")]
	[InlineData("%\n")]
	[InlineData(_SHELL_LINE_COMMENT_OPENING)]
	public void ADirectiveOpeningThatIsEmptyHoldsABlankOrEqualsADeclaredOpeningIsAViolationOfCode77(string opening)
	{
		AssertSingleViolation(Element(opening, "ALPHA"), AnalyzerContractViolationCode.ConditionalCompilationDirectiveOpeningMalformed);
	}

	[Theory]
	[InlineData("")]
	[InlineData("*")]
	[InlineData("+")]
	[InlineData("AL PHA")]
	[InlineData("AL-PHA")]
	[InlineData("ALPHA**")]
	[InlineData("ALPHA*+")]
	[InlineData("AL*PHA")]
	public void AWellKnownWordThatNamesNoSymbolOrHoldsAForeignCharacterIsAViolationOfCode78(string word)
	{
		AssertSingleViolation(Element(_DIRECTIVE_OPENING, word), AnalyzerContractViolationCode.ConditionalCompilationWellKnownSymbolWordMalformed);
	}

	[Fact]
	public void ARepeatedWellKnownWordIsAViolationOfCode78()
	{
		AssertSingleViolation(Element(_DIRECTIVE_OPENING, "ALPHA", "ALPHA"), AnalyzerContractViolationCode.ConditionalCompilationWellKnownSymbolWordMalformed);
		AssertSingleViolation(
			Element(_DIRECTIVE_OPENING, "ALPHA", $"ALPHA{ConditionalCompilationRule.PREFIX_WILDCARD_MARKER}", "ALPHA"),
			AnalyzerContractViolationCode.ConditionalCompilationWellKnownSymbolWordMalformed);
	}

	[Fact]
	public void BothMalformationsAreBothReported()
	{
		ImmutableArray<AnalyzerContractViolation> violations = Validate(Element(string.Empty, "AL PHA"));

		AnalyzerContractViolationCode[] expected =
		[
			AnalyzerContractViolationCode.ConditionalCompilationDirectiveOpeningMalformed,
			AnalyzerContractViolationCode.ConditionalCompilationWellKnownSymbolWordMalformed
		];

		Assert.Equal(expected, violations.Select(violation => violation.Code).ToArray());
	}

	[Fact]
	public void NoShippedProfileViolatesEitherCode()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach (ILanguageAnalyzer analyzer in catalog.AnalyzersByLanguageName.Values.Distinct())
		{
			Assert.DoesNotContain(
				AnalyzerContractValidator.Validate(analyzer),
				violation => violation.Code is AnalyzerContractViolationCode.ConditionalCompilationDirectiveOpeningMalformed
					or AnalyzerContractViolationCode.ConditionalCompilationWellKnownSymbolWordMalformed);
		}
	}

	[Fact]
	public void OnlyTheCSharpProfileDeclaresTheElement()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach ((string languageName, ILanguageAnalyzer analyzer) in catalog.AnalyzersByLanguageName)
		{
			bool declared = analyzer.LexicalProfile.ConditionalCompilation is not null;

			Assert.Equal(languageName == _CSHARP_LANGUAGE_NAME, declared);
		}
	}

	private static ConditionalCompilationRule Element(string opening, params string[] words)
	{
		return new ConditionalCompilationRule { DirectiveOpening = opening, WellKnownSymbolWords = [.. words] };
	}

	private static void AssertSingleViolation(ConditionalCompilationRule rule, AnalyzerContractViolationCode expected)
	{
		ImmutableArray<AnalyzerContractViolation> violations = Validate(rule);

		Assert.Single(violations);
		Assert.Equal(expected, violations[0].Code);
	}

	private static ImmutableArray<AnalyzerContractViolation> Validate(ConditionalCompilationRule? rule)
	{
		LexicalProfile shaped = ShellShapedProfile.BuildWithExecutedStrings();
		LexicalProfile profile = new()
		{
			ProfileName = nameof(ConditionalCompilationContractTests),
			ConstantDeclarationExpression = shaped.ConstantDeclarationExpression,
			LineCommentRules = shaped.LineCommentRulesInEngineOrder,
			BlockCommentRules = [],
			QuoteRules = shaped.QuoteRulesInEngineOrder,
			CodeEscapeCharacter = shaped.CodeEscapeCharacter,
			ArithmeticContext = shaped.ArithmeticContext,
			ExecutedStrings = shaped.ExecutedStrings,
			ConditionalCompilation = rule
		};

		return AnalyzerContractValidator.Validate(WithProfile(profile));
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
