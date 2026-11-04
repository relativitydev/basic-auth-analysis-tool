using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Lexical;
using MigrateAuth.Cli.Tests.Recognition;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SOURCE KIND OF A SYMBOL-TABLE RECORD, AND THE CELL THAT EXPECTS ONE (the symbol table and the identifier-anchored cell for a name that was assigned,
/// in the same file, from an environment value). The record carries a kind, `Literal` by default and `Environment`, and an environment record carries the word of its fallback,
/// empty for a value with none; a cell that expects a kind reads a record of the other kind as resolved and not matching; the counting rule counts both kinds together; and the
/// validator refuses an expected kind on a cell with no symbolic verdict and a declaration expression that declares an environment record without the group that captures its
/// fallback word. The semantic test of the copied value is here: a name with no fallback or a fallback that is neither `Basic` nor `Bearer`
/// reaches the Low class, a `Basic` fallback confirms the Medium class, a `Bearer` fallback and an indeterminate name stay silent, and a name reassigned to a literal is
/// indeterminate. No profile but the `JavaScript` one declares an environment record or an expected kind.
/// </summary>
public sealed class SymbolTableSourceKindTests
{
	private const string _HELD_CLASS = "JS-017";
	private const string _ENVIRONMENT_CLASS = "JS-018";
	private const string _SETTINGS_PROPERTY_CLASS = "JS-019";
	private const int _SOURCE_KIND_CODE = 74;
	private const string _USE = "await fetch(url, { headers: { Authorization: authValue } });\n";

	private static readonly LexicalProfile _profile = JavaScriptRegisteredExpressions.Analyzer.LexicalProfile;

	[Fact]
	public void TheCodeKeepsItsNumber()
	{
		Assert.Equal(_SOURCE_KIND_CODE, (int)AnalyzerContractViolationCode.SourceKindDeclarationMalformed);
	}

	// ---- The record ----

	[Theory]
	[InlineData("const authValue = process.env.API_AUTH;\n", SymbolSourceKind.Environment, "")]
	[InlineData("const authValue = process.env.API_AUTH ?? '';\n", SymbolSourceKind.Environment, "")]
	[InlineData("const authValue = process.env.API_AUTH || 'Basic dXNlcjpwYXNz';\n", SymbolSourceKind.Environment, "Basic")]
	[InlineData("const authValue = process.env.API_AUTH || \"bearer exampleToken\";\n", SymbolSourceKind.Environment, "bearer")]
	[InlineData("const authValue = process.env.API_AUTH || 'neutral';\n", SymbolSourceKind.Environment, "")]
	[InlineData("let authValue = `${import.meta.env.VITE_AUTH}`;\n", SymbolSourceKind.Environment, "")]
	[InlineData("authValue = Deno.env.get('API_AUTH');\n", SymbolSourceKind.Environment, "")]
	[InlineData("export var authValue = Bun.env['API_AUTH'];\n", SymbolSourceKind.Environment, "")]
	[InlineData("const authValue = 'Basic ' + pair;\n", SymbolSourceKind.Literal, "Basic")]
	public void ADeclarationIsRecordedWithItsKindAndTheWordOfItsFallback(string source, SymbolSourceKind kind, string word)
	{
		FileSymbolTable table = TableOf(source + _USE);

		Assert.True(table.TryResolveRecordUnambiguously("authValue", source.Length, out ResolvedSymbol record));
		Assert.Equal(kind, record.SourceKind);
		Assert.Equal(word, record.Value);
	}

	[Theory]
	[InlineData("const authValue = process.env.API_AUTH + suffix;\n")]
	[InlineData("const authValue = process.env.API_AUTH || compute();\n")]
	[InlineData("const authValue = process.env?.API_AUTH;\n")]
	[InlineData("const { authValue } = process.env;\n")]
	[InlineData("const authValue = loadValue();\n")]
	public void ADeclarationOfAnotherShapeIsNotCounted(string source)
	{
		FileSymbolTable table = TableOf(source + _USE);

		Assert.False(table.TryResolveRecordUnambiguously("authValue", source.Length, out _));
		Assert.Equal(0, table.DeclarationCountOf("authValue"));
	}

	[Fact]
	public void TheCountingRuleCountsBothKindsTogetherAndAReassignmentToALiteralIsIndeterminate()
	{
		const string source = "const authValue = process.env.API_AUTH;\nauthValue = 'fixed';\n";
		FileSymbolTable table = TableOf(source + _USE);

		Assert.Equal(2, table.DeclarationCountOf("authValue"));
		Assert.False(table.TryResolveRecordUnambiguously("authValue", source.Length, out _));
		Assert.False(table.TryResolveUnambiguously("authValue", source.Length, out _));
	}

	[Fact]
	public void ADeclarationAfterTheUseAndANameDeclaredNowhereAreIndeterminate()
	{
		const string source = "const authValue = process.env.API_AUTH;\n";
		FileSymbolTable table = TableOf(source);

		Assert.False(table.TryResolveRecordUnambiguously("authValue", 0, out _));
		Assert.False(table.TryResolveRecordUnambiguously("neverDeclared", source.Length, out _));
	}

	[Fact]
	public void TheEarlierReadStillResolvesAnEnvironmentRecordToItsFallbackWord()
	{
		const string source = "const authValue = process.env.API_AUTH || 'Basic dXNlcjpwYXNz';\n";
		FileSymbolTable table = TableOf(source + _USE);

		Assert.True(table.TryResolveUnambiguously("authValue", source.Length, out string word));
		Assert.Equal("Basic", word);
	}

	// ---- The three outcomes of one identifier-valued carrier: the semantic test of the copied value ----

	[Theory]
	[InlineData("const authValue = process.env.API_AUTH;\n", _ENVIRONMENT_CLASS, ConfidenceLevel.Low)]
	[InlineData("const authValue = process.env.API_AUTH ?? '';\n", _ENVIRONMENT_CLASS, ConfidenceLevel.Low)]
	[InlineData("const authValue = process.env.API_AUTH || 'neutral';\n", _ENVIRONMENT_CLASS, ConfidenceLevel.Low)]
	[InlineData("const authValue = process.env.API_AUTH || 'Basic dXNlcjpwYXNz';\n", _HELD_CLASS, ConfidenceLevel.Medium)]
	[InlineData("const authValue = 'Basic ' + pair;\n", _HELD_CLASS, ConfidenceLevel.Medium)]
	public void OneCarrierWithAnIdentifierValueIsExactlyOneOfTheLowClassTheMediumClassOrSilence(string declaration, string expectedClass, ConfidenceLevel expectedLevel)
	{
		ImmutableArray<RecognizedConstruct> constructs = new JavaScriptRecognitionHarness().Recognize(declaration + _USE);

		RecognizedConstruct only = Assert.Single(constructs);

		Assert.Equal(expectedClass, only.PatternClassIdentifier);
		Assert.Equal(expectedLevel, only.Level);
	}

	[Theory]
	[InlineData("const authValue = process.env.API_AUTH || 'Bearer exampleToken';\n")]
	[InlineData("const authValue = 'Bearer ' + token;\n")]
	[InlineData("const authValue = process.env.API_AUTH;\nauthValue = 'fixed';\n")]
	[InlineData("const authValue = loadValue();\n")]
	[InlineData("")]
	public void ABearerFallbackAResolvedOtherSchemeAnIndeterminateNameAndAnUndeclaredNameAreSilent(string declaration)
	{
		Assert.Empty(new JavaScriptRecognitionHarness().Recognize(declaration + _USE));
	}

	// ---- The contract ----

	[Fact]
	public void TheJavaScriptEnvironmentAndSettingsPropertyClassesDeclareAnExpectedKindAndNoOtherClassDoes()
	{
		ILanguageAnalyzer analyzer = JavaScriptRegisteredExpressions.Analyzer;

		Assert.Equal(SymbolSourceKind.Environment, analyzer.DetectionRulesByPatternClass[_ENVIRONMENT_CLASS].SymbolicVerdictInterpretation.ExpectedSourceKind);
		Assert.Equal(SymbolSourceKind.Environment, analyzer.DetectionRulesByPatternClass[_SETTINGS_PROPERTY_CLASS].SymbolicVerdictInterpretation.ExpectedSourceKind);
		Assert.All(
			LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName.Values.Distinct().SelectMany(candidate => candidate.DetectionRulesByPatternClass)
				.Where(entry => entry.Key != _ENVIRONMENT_CLASS && entry.Key != _SETTINGS_PROPERTY_CLASS),
			entry => Assert.Null(entry.Value.SymbolicVerdictInterpretation.ExpectedSourceKind));
	}

	[Fact]
	public void OnlyTheJavaScriptAndTypeScriptProfilesDeclareAnEnvironmentRecord()
	{
		ILanguageAnalyzer[] analyzers = [.. LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName.Values.Distinct()];
		ILanguageAnalyzer javaScript = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("JavaScript"));
		ILanguageAnalyzer typeScript = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("TypeScript"));

		Assert.All(
			new[] { javaScript, typeScript },
			analyzer =>
			{
				Assert.Contains(EnvironmentDeclarationGroups.MARKER, analyzer.LexicalProfile.ConstantDeclarationExpression.GetGroupNames());
				Assert.Contains(EnvironmentDeclarationGroups.FALLBACK_WORD, analyzer.LexicalProfile.ConstantDeclarationExpression.GetGroupNames());
			});
		Assert.All(
			analyzers.Where(analyzer => analyzer != javaScript && analyzer != typeScript),
			analyzer => Assert.DoesNotContain(EnvironmentDeclarationGroups.MARKER, analyzer.LexicalProfile.ConstantDeclarationExpression.GetGroupNames()));
	}

	[Fact]
	public void NoShippedAnalyzerViolatesTheSourceKindCode()
	{
		foreach (ILanguageAnalyzer analyzer in LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName.Values.Distinct())
		{
			Assert.DoesNotContain(AnalyzerContractValidator.Validate(analyzer), violation => violation.Code == AnalyzerContractViolationCode.SourceKindDeclarationMalformed);
		}
	}

	[Fact]
	public void AnExpectedKindOnACellWithNoSchemeArgumentIsAViolation()
	{
		ILanguageAnalyzer analyzer = WithInterpretation("JS-001", new SymbolicVerdictInterpretation { ExpectedSourceKind = SymbolSourceKind.Environment });

		Assert.Contains(AnalyzerContractValidator.Validate(analyzer), violation => violation.Code == AnalyzerContractViolationCode.SourceKindDeclarationMalformed);
	}

	[Fact]
	public void ADeclarationExpressionThatDeclaresAnEnvironmentRecordWithoutTheFallbackWordGroupIsAViolation()
	{
		LexicalProfile shaped = ShellShapedProfile.Build();
		LexicalProfile profile = new()
		{
			ProfileName = nameof(SymbolTableSourceKindTests),
			ConstantDeclarationExpression = new Regex(
				"^(?<name>[A-Za-z_]+)=(?<value>x)(?<environment>)",
				RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
				RecognitionEnginePolicy.MATCH_BOUND),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules = shaped.QuoteRulesInEngineOrder
		};

		ImmutableArray<AnalyzerContractViolation> violations = AnalyzerContractValidator.Validate(WithProfile(profile));

		Assert.Contains(violations, violation => violation.Code == AnalyzerContractViolationCode.SourceKindDeclarationMalformed);
	}

	[Fact]
	public void ACellThatExpectsAKindMayLeaveTheExpectedTokenBlankButACellThatDoesNotMayNot()
	{
		SymbolicVerdictInterpretation held = JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_HELD_CLASS].SymbolicVerdictInterpretation;
		ILanguageAnalyzer blankWithoutAKind = WithInterpretation(_HELD_CLASS, new SymbolicVerdictInterpretation
		{
			SchemeArgumentCaptureGroupName = held.SchemeArgumentCaptureGroupName,
			ExpectedSchemeToken = " ",
			WhenResolvedMatching = held.WhenResolvedMatching,
			WhenResolvedNotMatching = held.WhenResolvedNotMatching,
			WhenIndeterminate = held.WhenIndeterminate
		});

		Assert.True(string.IsNullOrWhiteSpace(JavaScriptRegisteredExpressions.Analyzer.DetectionRulesByPatternClass[_ENVIRONMENT_CLASS].SymbolicVerdictInterpretation.ExpectedSchemeToken));
		Assert.Empty(AnalyzerContractValidator.Validate(JavaScriptRegisteredExpressions.Analyzer));
		Assert.Contains(AnalyzerContractValidator.Validate(blankWithoutAKind), violation => violation.Code == AnalyzerContractViolationCode.ExpectedSchemeTokenMissing);
	}

	private static ILanguageAnalyzer WithInterpretation(string patternClass, SymbolicVerdictInterpretation interpretation)
	{
		ILanguageAnalyzer javaScript = JavaScriptRegisteredExpressions.Analyzer;
		DetectionRule original = javaScript.DetectionRulesByPatternClass[patternClass];
		DetectionRule changed = new()
		{
			Expression = original.Expression,
			AnchorPosition = original.AnchorPosition,
			BaseLevel = original.BaseLevel,
			SelfSufficiency = original.SelfSufficiency,
			SymbolicVerdictInterpretation = interpretation,
			CredentialPosition = original.CredentialPosition,
			DestinationCertainty = original.DestinationCertainty,
			DisplayName = original.DisplayName,
			VariableUse = original.VariableUse
		};
		Dictionary<string, DetectionRule> rules = new(javaScript.DetectionRulesByPatternClass, StringComparer.Ordinal)
		{
			[patternClass] = changed
		};
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LexicalProfile).Returns(javaScript.LexicalProfile);
		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(javaScript.LanguageIdentification);
		analyzer.SetupGet(supplied => supplied.DetectionRulesByPatternClass).Returns(rules.ToFrozenDictionary(StringComparer.Ordinal));
		analyzer.SetupGet(supplied => supplied.DemotionSignals).Returns(javaScript.DemotionSignals);
		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation).Returns(javaScript.CandidateSelectionAndRecommendation);
		analyzer.SetupGet(supplied => supplied.SubsumptionRelation).Returns(javaScript.SubsumptionRelation);
		analyzer.SetupGet(supplied => supplied.ConstructPrecedence).Returns(javaScript.ConstructPrecedence);

		return analyzer.Object;
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

	private static FileSymbolTable TableOf(string source)
	{
		ZoneMask mask = new LexicalScanner(_profile).Scan(source);

		return FileSymbolTable.Collect(_profile, source, mask);
	}
}
