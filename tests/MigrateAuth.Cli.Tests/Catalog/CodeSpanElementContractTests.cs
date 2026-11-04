using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Lexical;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// VALIDATOR CODE 73 AND THE NO-EFFECT PROOF OF THE CODE-SPAN ELEMENT, the lexical vocabulary for the shell backtick: a
/// well-formed declaration is accepted, each malformed one is a violation of exactly its code, and no shipped profile but `Shell` declares it, so no
/// other shipped profile can take any of the scanner's code-span branches (the declaration is empty and the step asks it first). The contract tests
/// pin that `CSharp`, `PowerShell`, `VBNet` and `JavaScript` keep their zones.
/// </summary>
public sealed class CodeSpanElementContractTests
{
	private const int _CODE_SPAN_MALFORMED_CODE = 73;
	private const string _BACKTICK = "`";
	private const string _ESCAPE = "\\";
	private const string _ESCAPABLE = "`\\$";
	private const string _SINGLE_QUOTE = "'";
	private const string _LINE_COMMENT_OPENING = "#";

	[Fact]
	public void TheCodeKeepsItsNumber()
	{
		Assert.Equal(_CODE_SPAN_MALFORMED_CODE, (int)AnalyzerContractViolationCode.CodeSpanElementMalformed);
	}

	[Fact]
	public void TheBacktickSpanOfTheShellShapedReferenceIsAccepted()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(ShellShapedProfile.BuildWithCodeSpans())));
	}

	[Fact]
	public void AProfileThatDeclaresNoSpanIsAccepted()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(ShellShapedProfile.BuildWithExecutedStrings())));
	}

	[Theory]
	[InlineData("", _BACKTICK)]
	[InlineData(_BACKTICK, "")]
	[InlineData("`\n", _BACKTICK)]
	[InlineData(_BACKTICK, "\r`")]
	public void AnEmptyOpeningOrClosingOrOneThatHoldsALineTerminatorIsAViolation(string opening, string closing)
	{
		CodeSpanElement span = BacktickSpan() with { Opening = opening, Closing = closing };

		AssertSingleViolation([span]);
	}

	[Theory]
	[InlineData("")]
	[InlineData("\\\\")]
	public void AnEscapeCharacterThatIsNotOneCharacterIsAViolation(string escape)
	{
		AssertSingleViolation([BacktickSpan() with { EscapeCharacter = escape }]);
	}

	[Fact]
	public void AnEmptyEscapableSetIsAViolation()
	{
		AssertSingleViolation([BacktickSpan() with { EscapableCharacters = string.Empty }]);
	}

	[Fact]
	public void TwoElementsWithOneOpeningAreAViolation()
	{
		AssertSingleViolation([BacktickSpan(), BacktickSpan() with { Closing = "~" }]);
	}

	[Fact]
	public void AnOpeningEqualToTheOpeningOfADeclaredQuoteRuleIsAViolation()
	{
		AssertSingleViolation([BacktickSpan() with { Opening = _SINGLE_QUOTE, Closing = _SINGLE_QUOTE }]);
	}

	[Fact]
	public void AnOpeningEqualToTheOpeningOfADeclaredCommentRuleIsAViolation()
	{
		AssertSingleViolation([BacktickSpan() with { Opening = _LINE_COMMENT_OPENING, Closing = _LINE_COMMENT_OPENING }]);
	}

	[Fact]
	public void NoShippedProfileViolatesTheCodeSpanCode()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach (ILanguageAnalyzer analyzer in catalog.AnalyzersByLanguageName.Values.Distinct())
		{
			Assert.DoesNotContain(AnalyzerContractValidator.Validate(analyzer), violation => violation.Code == AnalyzerContractViolationCode.CodeSpanElementMalformed);
		}
	}

	/// <summary>
	/// PROOF (a) OF THE CODE-SPAN ELEMENT: no shipped profile but `Shell`'s declares one, which is the one condition under which the scanner's step looks for a span, so
	/// `CSharp`, `PowerShell`, `VBNet` and `JavaScript` take no code-span branch (`PowerShell` uses the backtick as its code escape and `JavaScript` as a quote opening, both
	/// through fields of their own profiles).
	/// </summary>
	[Fact]
	public void OnlyTheShellProfileDeclaresACodeSpan()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		ILanguageAnalyzer[] analyzers = [.. catalog.AnalyzersByLanguageName.Values.Distinct()];
		ILanguageAnalyzer shell = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("Shell"));

		Assert.Single(shell.LexicalProfile.CodeSpans);
		Assert.All(analyzers.Where(analyzer => analyzer != shell), analyzer => Assert.Empty(analyzer.LexicalProfile.CodeSpans));
	}

	/// <summary>
	/// A text with a one-line backtick substitution that opens a here-document is read by every shipped profile but `Shell`'s exactly as without the element: the
	/// backtick is code (`PowerShell`'s escape, `CSharp`'s and `VBNet`'s ordinary character) or a quote opening (`JavaScript`'s template), so the zone of the line after it is
	/// the one the profile gives the text without any span.
	/// </summary>
	[Fact]
	public void EveryShippedProfileButShellsReadsABacktickSubstitutionAsItDidBeforeTheElement()
	{
		const string source = "x = `cat <<EOF`\ncurl -u user:pass host\n";
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach (ILanguageAnalyzer analyzer in catalog.AnalyzersByLanguageName.Values.Distinct().Where(candidate => !candidate.LanguageIdentification.DeclaredLanguageNames.Contains("Shell")))
		{
			LexicalProfile withoutSpans = CopyWithoutSpans(analyzer.LexicalProfile);
			ZoneMask shipped = new LexicalScanner(analyzer.LexicalProfile).Scan(source);
			ZoneMask reference = new LexicalScanner(withoutSpans).Scan(source);

			Assert.Equal(reference.Zones.ToArray(), shipped.Zones.ToArray());
		}
	}

	private static LexicalProfile CopyWithoutSpans(LexicalProfile profile)
	{
		return new LexicalProfile
		{
			ProfileName = profile.ProfileName,
			ConstantDeclarationExpression = profile.ConstantDeclarationExpression,
			KeywordCaseSensitivity = profile.KeywordCaseSensitivity,
			DelimiterCaseSensitivity = profile.DelimiterCaseSensitivity,
			IdentifierCaseSensitivity = profile.IdentifierCaseSensitivity,
			CodeEscapeCharacter = profile.CodeEscapeCharacter,
			ArithmeticContext = profile.ArithmeticContext,
			ExecutedStrings = profile.ExecutedStrings,
			LineCommentRules = profile.LineCommentRulesInEngineOrder,
			BlockCommentRules = profile.BlockCommentRulesInEngineOrder,
			QuoteRules = profile.QuoteRulesInEngineOrder
		};
	}

	private static CodeSpanElement BacktickSpan()
	{
		return new CodeSpanElement { Opening = _BACKTICK, Closing = _BACKTICK, EscapeCharacter = _ESCAPE, EscapableCharacters = _ESCAPABLE };
	}

	private static void AssertSingleViolation(ImmutableArray<CodeSpanElement> spans)
	{
		LexicalProfile shaped = ShellShapedProfile.BuildWithExecutedStrings();
		LexicalProfile profile = new()
		{
			ProfileName = nameof(CodeSpanElementContractTests),
			ConstantDeclarationExpression = shaped.ConstantDeclarationExpression,
			LineCommentRules = shaped.LineCommentRulesInEngineOrder,
			BlockCommentRules = [],
			QuoteRules = shaped.QuoteRulesInEngineOrder,
			CodeEscapeCharacter = shaped.CodeEscapeCharacter,
			ArithmeticContext = shaped.ArithmeticContext,
			ExecutedStrings = shaped.ExecutedStrings,
			CodeSpans = spans
		};

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.CodeSpanElementMalformed, violations[0].Code);
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
