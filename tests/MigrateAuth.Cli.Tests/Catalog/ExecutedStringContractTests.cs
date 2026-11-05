using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Lexical;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// VALIDATOR CODES 66 TO 71 AND THE NO-EFFECT PROOF OF THE EXECUTED-STRING CONTEXT. The context is profile vocabulary: a
/// well-formed declaration is accepted, each malformed one is a violation of exactly its code, and no shipped profile but
/// `Shell`'s declares it, so no other shipped profile can take any of the executed-string scanner branches (the reader and the
/// tables are null and every branch is guarded by them).
/// </summary>
public sealed class ExecutedStringContractTests
{
	private const int _CONTEXT_MALFORMED_CODE = 66;
	private const int _QUOTE_NAMES_NO_RULE_CODE = 67;
	private const int _WORD_MALFORMED_CODE = 68;
	private const int _INLINE_MALFORMED_CODE = 69;
	private const int _WITHOUT_CODE_ESCAPE_CODE = 70;
	private const int _GRAMMAR_MALFORMED_CODE = 71;
	private const string _SINGLE_QUOTE = "'";
	private const string _UNKNOWN_QUOTE_OPENING = "~~";
	private const string _CONSTRUCT = "curl -u user:pass";

	[Fact]
	public void TheTestLocalDeclarationOnTheShellShapedProfileIsAccepted()
	{
		Assert.Empty(AnalyzerContractValidator.Validate(WithProfile(ShellShapedProfile.BuildWithExecutedStrings())));
	}

	[Fact]
	public void TheSixCodesKeepTheirNumbers()
	{
		Assert.Equal(_CONTEXT_MALFORMED_CODE, (int)AnalyzerContractViolationCode.ExecutedStringContextMalformed);
		Assert.Equal(_QUOTE_NAMES_NO_RULE_CODE, (int)AnalyzerContractViolationCode.ExecutedBodyQuoteNamesNoQuoteRule);
		Assert.Equal(_WORD_MALFORMED_CODE, (int)AnalyzerContractViolationCode.ExecutedStringWordMalformed);
		Assert.Equal(_INLINE_MALFORMED_CODE, (int)AnalyzerContractViolationCode.InlineExecutedContextMalformed);
		Assert.Equal(_WITHOUT_CODE_ESCAPE_CODE, (int)AnalyzerContractViolationCode.ExecutedStringContextWithoutCodeEscape);
		Assert.Equal(_GRAMMAR_MALFORMED_CODE, (int)AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
	}

	[Fact]
	public void ADeclarationThatReadsNothingIsAViolation()
	{
		ExecutedStringContext nothing = Declaration() with { EvalWords = [], ShellWords = [], CommandStringWords = [], RemoteShellWords = [], InlineContexts = [] };

		AssertSingle(nothing, AnalyzerContractViolationCode.ExecutedStringContextMalformed);
	}

	[Theory]
	[InlineData("")]
	[InlineData("c")]
	[InlineData("--c")]
	[InlineData("-cc")]
	[InlineData("-1")]
	public void AStringOptionThatIsNotAHyphenAndOneLetterIsAViolation(string option)
	{
		AssertSingle(Declaration() with { StringOption = option }, AnalyzerContractViolationCode.ExecutedStringContextMalformed);
	}

	[Fact]
	public void ADeclarationWithNoReadableBodyOrNoPredecessorIsAViolation()
	{
		AssertSingle(Declaration() with { ReadableBodies = [] }, AnalyzerContractViolationCode.ExecutedStringContextMalformed);
		AssertSingle(Declaration() with { CommandPositionPredecessors = string.Empty }, AnalyzerContractViolationCode.ExecutedStringContextMalformed);
	}

	[Fact]
	public void AReadableBodyThatNamesNoQuoteRuleIsAViolation()
	{
		ExecutedStringContext named = Declaration() with
		{
			ReadableBodies = [.. Declaration().ReadableBodies, new ExecutedBodyQuote { QuoteOpening = _UNKNOWN_QUOTE_OPENING, EscapableCharacters = string.Empty }]
		};

		AssertSingle(named, AnalyzerContractViolationCode.ExecutedBodyQuoteNamesNoQuoteRule);
	}

	[Fact]
	public void TwoReadableBodiesOfOneQuoteOpeningAreAViolation()
	{
		ExecutedStringContext twice = Declaration() with
		{
			ReadableBodies = [.. Declaration().ReadableBodies, new ExecutedBodyQuote { QuoteOpening = _SINGLE_QUOTE, EscapableCharacters = string.Empty }]
		};

		AssertSingle(twice, AnalyzerContractViolationCode.ExecutedBodyQuoteNamesNoQuoteRule);
	}

	[Fact]
	public void AnInlineContextThatNamesNoQuoteRuleIsAViolation()
	{
		InlineExecutedContext inline = Declaration().InlineContexts[0] with { QuoteOpening = _UNKNOWN_QUOTE_OPENING };

		AssertSingle(Declaration() with { InlineContexts = [inline] }, AnalyzerContractViolationCode.ExecutedBodyQuoteNamesNoQuoteRule);
	}

	[Theory]
	[InlineData("")]
	[InlineData("a b")]
	[InlineData("it's")]
	[InlineData("say\"hi")]
	public void AWordThatCouldNeverMatchIsAViolation(string word)
	{
		AssertSingle(Declaration() with { EvalWords = [word] }, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
		AssertSingle(Declaration() with { ShellWords = [word] }, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
		AssertSingle(Declaration() with { OutputRedirections = [word] }, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
	}

	[Fact]
	public void AnOptionWithAnArgumentThatDoesNotBeginWithAHyphenOrAPlusIsAViolation()
	{
		AssertSingle(Declaration() with { PrivilegeOptionsWithArgument = ["u"] }, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
		AssertSingle(Declaration() with { StdinOption = "s" }, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
	}

	[Theory]
	[InlineData("sh")]
	[InlineData("")]
	[InlineData(".")]
	public void AScriptFileExtensionWithoutALeadingDotAndAFollowingLetterIsAViolation(string extension)
	{
		AssertSingle(Declaration() with { ScriptFileExtensions = [extension] }, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
	}

	[Fact]
	public void AnInlineContextWithAnEmptyOpeningClosingOrOperatorIsAViolation()
	{
		InlineExecutedContext inline = Declaration().InlineContexts[0];

		AssertSingle(Declaration() with { InlineContexts = [inline with { Opening = string.Empty }] }, AnalyzerContractViolationCode.InlineExecutedContextMalformed);
		AssertSingle(Declaration() with { InlineContexts = [inline with { Closing = string.Empty }] }, AnalyzerContractViolationCode.InlineExecutedContextMalformed);
		AssertSingle(Declaration() with { InlineContexts = [inline with { WordOperators = [string.Empty] }] }, AnalyzerContractViolationCode.InlineExecutedContextMalformed);
	}

	[Fact]
	public void AnEmptyGrammarSetIsAViolation()
	{
		AssertSingle(Declaration() with { BlankCharacters = string.Empty }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		AssertSingle(Declaration() with { OptionIntroducers = string.Empty }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed, AnalyzerContractViolationCode.ExecutedStringContextMalformed, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
		AssertSingle(Declaration() with { RedirectionCharacters = string.Empty }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		AssertSingle(Declaration() with { ListOperatorCharacters = string.Empty }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
	}

	[Fact]
	public void AnUnsetGrammarCharacterIsAViolation()
	{
		AssertSingle(Declaration() with { PathSeparator = default }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		AssertSingle(Declaration() with { CommentStart = default }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
	}

	[Fact]
	public void ABlankThatIsNotWhitespaceOrIsAQuoteIsAViolation()
	{
		AssertSingle(Declaration() with { BlankCharacters = "x" }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		AssertSingle(Declaration() with { BlankCharacters = " \t'" }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
	}

	[Fact]
	public void ModifiersWithoutTheDuplicationAndAPairOfTheWrongLengthAreViolations()
	{
		AssertSingle(Declaration() with { RedirectionModifierCharacters = "|" }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		AssertSingle(Declaration() with { ListOperatorPairs = ["&&&"] }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
	}

	[Fact]
	public void AnEndOfOptionsWordThatIsNoOptionOrNoPipeOperatorOrANonDigitDescriptorIsAViolation()
	{
		AssertSingle(Declaration() with { EndOfOptionsWord = string.Empty }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
		AssertSingle(Declaration() with { EndOfOptionsWord = "x" }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		AssertSingle(Declaration() with { PipeOperators = [] }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		AssertSingle(Declaration() with { PipeOperators = [string.Empty] }, AnalyzerContractViolationCode.ExecutedStringWordMalformed);
		AssertSingle(Declaration() with { StandardOutputDescriptor = "a" }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		AssertSingle(Declaration() with { StandardOutputDescriptor = string.Empty }, AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
	}

	[Fact]
	public void ADeclarationOnAProfileWithNoCodeEscapeIsAViolation()
	{
		LexicalProfile profile = new()
		{
			ProfileName = nameof(ExecutedStringContractTests),
			ConstantDeclarationExpression = HereDocumentExemplarPatternExpressions.ConstantDeclaration(),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = ShellShapedProfile.Build().QuoteRulesInEngineOrder,
			ExecutedStrings = Declaration()
		};

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Single(violations);
		Assert.Equal(AnalyzerContractViolationCode.ExecutedStringContextWithoutCodeEscape, violations[0].Code);
	}

	[Fact]
	public void NoShippedProfileViolatesAnExecutedStringCode()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		foreach (ILanguageAnalyzer analyzer in catalog.AnalyzersByLanguageName.Values.Distinct())
		{
			Assert.DoesNotContain(
				AnalyzerContractValidator.Validate(analyzer),
				violation => (int)violation.Code is >= _CONTEXT_MALFORMED_CODE and <= _GRAMMAR_MALFORMED_CODE);
		}
	}

	/// <summary>
	/// PROOF (a) OF THE EXECUTED-STRING CONTEXT: no shipped profile but `Shell`'s declares it, which is the one condition under which
	/// the scanner builds a reader, reads a quote's words, keeps a table of escapes or tries an inline construct.
	/// </summary>
	[Fact]
	public void OnlyTheShellProfileDeclaresAnExecutedStringContext()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		ILanguageAnalyzer[] analyzers = [.. catalog.AnalyzersByLanguageName.Values.Distinct()];
		ILanguageAnalyzer shell = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("Shell"));

		Assert.NotNull(shell.LexicalProfile.ExecutedStrings);
		Assert.All(analyzers.Where(analyzer => analyzer != shell), analyzer => Assert.Null(analyzer.LexicalProfile.ExecutedStrings));
	}

	/// <summary>
	/// A shipped profile that declares no executed-string context reads a string that looks like an executed one exactly as a string: the text that a declaring
	/// profile reads as code is string body under the profiles of CSharp, PowerShell and VBNet, and code under `Shell`'s.
	/// </summary>
	[Fact]
	public void EveryShippedProfileButShellsReadsATextThatLooksLikeAnExecutedStringAsAString()
	{
		const string source = "eval \"curl -u user:pass host\"\n";
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		int construct = source.IndexOf(_CONSTRUCT, StringComparison.Ordinal);

		foreach (ILanguageAnalyzer analyzer in catalog.AnalyzersByLanguageName.Values.Distinct())
		{
			ZoneMask mask = new LexicalScanner(analyzer.LexicalProfile).Scan(source);
			LexicalZone expected = analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("Shell") ? LexicalZone.Code : LexicalZone.StringBody;

			Assert.Equal(expected, mask.ZoneAt(construct));
		}
	}

	private static void AssertSingle(ExecutedStringContext declaration, AnalyzerContractViolationCode code, params AnalyzerContractViolationCode[] alsoExpected)
	{
		LexicalProfile profile = BuildProfile(declaration);
		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Equal(alsoExpected.Length + 1, violations.Length);
		Assert.Contains(violations, violation => violation.Code == code);
		Assert.All(alsoExpected, expected => Assert.Contains(violations, violation => violation.Code == expected));
	}

	private static ExecutedStringContext Declaration()
	{
		return ShellExecutedStringsDeclaration.Build();
	}

	private static LexicalProfile BuildProfile(ExecutedStringContext declaration)
	{
		LexicalProfile shell = ShellShapedProfile.Build();

		return new LexicalProfile
		{
			ProfileName = nameof(ExecutedStringContractTests),
			ConstantDeclarationExpression = HereDocumentExemplarPatternExpressions.ConstantDeclaration(),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = shell.QuoteRulesInEngineOrder,
			CodeEscapeCharacter = shell.CodeEscapeCharacter,
			ArithmeticContext = shell.ArithmeticContext,
			ExecutedStrings = declaration
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
