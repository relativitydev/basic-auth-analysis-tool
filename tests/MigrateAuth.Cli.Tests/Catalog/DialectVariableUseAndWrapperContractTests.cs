using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE OPTIONAL CONTRACT ELEMENTS OF DIALECTS, SAME-FILE VARIABLE USE AND SAME-FILE WRAPPERS: what the validator
/// refuses (each with its own code), what the contract surface guarantees, and that a profile declaring none of
/// them reads exactly as without them. The analyzer is the test-local one of <see cref="ShellProbe"/>.
/// </summary>
public sealed class DialectVariableUseAndWrapperContractTests
{
	private const int _FIRST_NEW_CODE = (int)AnalyzerContractViolationCode.FileDialectsDefaultDialectBlank;
	private const int _LAST_NEW_CODE = (int)AnalyzerContractViolationCode.FlowSignalVariableUseStatementSeparatorBlank;
	private const int _FIRST_PRECEDENCE_CODE = _LAST_NEW_CODE + 1;
	private const int _LAST_PRECEDENCE_CODE = _FIRST_PRECEDENCE_CODE + 1;
	private const int _FIRST_EXECUTED_STRING_CODE = _LAST_PRECEDENCE_CODE + 1;
	private const int _LAST_EXECUTED_STRING_CODE = _FIRST_EXECUTED_STRING_CODE + 5;
	private const int _DEMOTION_KIND_CODE = _LAST_EXECUTED_STRING_CODE + 1;
	private const int _CODE_SPAN_CODE = _DEMOTION_KIND_CODE + 1;
	private const int _SOURCE_KIND_CODE = _CODE_SPAN_CODE + 1;
	private const int _NEUTRAL_OPERAND_PAIR_GROUP_CODE = _SOURCE_KIND_CODE + 1;
	private const int _NEUTRAL_OPERAND_PAIR_BOUND_CODE = _NEUTRAL_OPERAND_PAIR_GROUP_CODE + 1;
	private const int _CONDITIONAL_COMPILATION_OPENING_CODE = _NEUTRAL_OPERAND_PAIR_BOUND_CODE + 1;
	private const int _CONDITIONAL_COMPILATION_WORD_CODE = _CONDITIONAL_COMPILATION_OPENING_CODE + 1;
	private const int _MEMBER_DECLARATION_BOUND_CODE = _CONDITIONAL_COMPILATION_WORD_CODE + 1;
	private const int _MEMBER_DECLARATION_MALFORMED_CODE = _MEMBER_DECLARATION_BOUND_CODE + 1;
	private const int _MEMBER_KIND_WITHOUT_EXPRESSION_CODE = _MEMBER_DECLARATION_MALFORMED_CODE + 1;
	private const int _ONE_MILLISECOND = 1;
	private const string _ABSENT_GROUP = "groupThatTheExpressionDoesNotDeclare";
	private const string _UNDECLARED_DIALECT = "fish";

	private static readonly string[] _boundPropertyNames =
	[
		nameof(FlowSignalVariableUse.WindowLines),
		nameof(FlowSignalVariableUse.MaxLogicalLineLength),
		nameof(FlowSignalVariableUse.AliasDepth),
		nameof(WrapperTracking.MaxBodyLines),
		nameof(WrapperTracking.MaxCalls)
	];

	private static readonly Type[] _newContractTypes = [typeof(FileDialects), typeof(FlowSignalVariableUse), typeof(RuleVariableUse), typeof(WrapperTracking)];

	// ---- the validator: dialects ----

	[Fact]
	public void CleanDeclarationsOfEveryNewElementProduceNoNewViolation()
	{
		ImmutableArray<AnalyzerContractViolation> violations = Validate(CleanAnalyzer());

		Assert.Empty(NewViolations(violations));
	}

	[Fact]
	public void ABlankDefaultDialectIsAViolation()
	{
		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithDialects(ShellProbe.Dialects with { DefaultDialect = "  " }));

		AssertContains(violations, AnalyzerContractViolationCode.FileDialectsDefaultDialectBlank);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void AnEmptyKeyOfEitherMapIsAViolation(bool shebangMap)
	{
		FrozenDictionary<string, string> withEmptyKey = new Dictionary<string, string> { [string.Empty] = ShellProbe.BASH }.ToFrozenDictionary();
		FileDialects dialects = shebangMap
			? ShellProbe.Dialects with { ShebangInterpreters = withEmptyKey }
			: ShellProbe.Dialects with { ExtensionDialects = withEmptyKey };

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithDialects(dialects));

		AssertContains(violations, AnalyzerContractViolationCode.FileDialectsMapKeyEmpty);
	}

	[Fact]
	public void ABlankValueOfEitherMapIsAViolation()
	{
		FileDialects dialects = ShellProbe.Dialects with
		{
			ShebangInterpreters = new Dictionary<string, string> { ["bash"] = " " }.ToFrozenDictionary()
		};

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithDialects(dialects));

		AssertContains(violations, AnalyzerContractViolationCode.FileDialectsMapValueBlank);
	}

	[Fact]
	public void TwoExtensionKeysThatDifferOnlyByCaseContradictEachOther()
	{
		FileDialects dialects = ShellProbe.Dialects with
		{
			ExtensionDialects = new Dictionary<string, string> { [".zsh"] = ShellProbe.ZSH, [".ZSH"] = ShellProbe.BASH }.ToFrozenDictionary()
		};

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithDialects(dialects));

		AssertContains(violations, AnalyzerContractViolationCode.FileDialectsExtensionKeysConflict);
	}

	[Theory]
	[InlineData("zsh")]
	[InlineData(".ZSH")]
	[InlineData(".")]
	public void AnExtensionKeyThatCouldNeverMatchALowerCasedExtensionIsAViolation(string key)
	{
		FileDialects dialects = ShellProbe.Dialects with
		{
			ExtensionDialects = new Dictionary<string, string> { [key] = ShellProbe.ZSH }.ToFrozenDictionary()
		};

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithDialects(dialects));

		AssertContains(violations, AnalyzerContractViolationCode.FileDialectsExtensionKeyMalformed);
	}

	[Fact]
	public void ASignalThatNamesAnUndeclaredDialectIsAViolationWhetherItIncludesOrExcludesIt()
	{
		FlowSignal including = ShellProbe.SilentReadInBash with { Dialects = [_UNDECLARED_DIALECT] };
		FlowSignal excluding = ShellProbe.SelectPromptExceptInZsh with { ExcludedDialects = [_UNDECLARED_DIALECT] };

		ImmutableArray<AnalyzerContractViolation> viaInclude = Validate(WithSignals([including], ShellProbe.Dialects));
		ImmutableArray<AnalyzerContractViolation> viaExclude = Validate(WithSignals([excluding], ShellProbe.Dialects));

		AssertContains(viaInclude, AnalyzerContractViolationCode.FlowSignalDialectNotDeclared);
		AssertContains(viaExclude, AnalyzerContractViolationCode.FlowSignalDialectNotDeclared);
	}

	[Fact]
	public void ADialectIsDeclaredByTheDefaultAndByEveryValueOfEitherMap()
	{
		FileDialects dialects = new()
		{
			DefaultDialect = "alpha",
			ShebangInterpreters = new Dictionary<string, string> { ["b"] = "beta" }.ToFrozenDictionary(),
			ExtensionDialects = new Dictionary<string, string> { [".g"] = "gamma" }.ToFrozenDictionary()
		};
		ImmutableArray<FlowSignal> signals = [ShellProbe.SilentReadInBash with { Dialects = ["alpha", "beta", "gamma"] }];

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithSignals(signals, dialects));

		Assert.Empty(NewViolations(violations));
	}

	[Fact]
	public void ASignalThatNamesADialectWhereTheLanguageDeclaresNoneIsAViolation()
	{
		ImmutableArray<AnalyzerContractViolation> violations = Validate(ShellProbe.AnalyzerWith([ShellProbe.SilentReadInBash]));

		AssertContains(violations, AnalyzerContractViolationCode.FlowSignalDialectNotDeclared);
	}

	[Fact]
	public void ADialectBothIncludedAndExcludedByOneSignalIsAViolation()
	{
		FlowSignal contradicting = ShellProbe.SilentReadInBash with { ExcludedDialects = [ShellProbe.BASH] };

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithSignals([contradicting], ShellProbe.Dialects));

		AssertContains(violations, AnalyzerContractViolationCode.FlowSignalDialectIncludedAndExcluded);
	}

	// ---- the validator: variable use ----

	[Fact]
	public void AVariableGroupTheExpressionDoesNotDeclareIsAViolationUnlessADefaultNameIsGiven()
	{
		FlowSignalVariableUse withoutDefault = new() { UsePattern = ShellProbeExpressions.PasswordOnStandardInput(), VariableGroupName = _ABSENT_GROUP };
		FlowSignalVariableUse withDefault = withoutDefault with { DefaultVariableName = "REPLY" };

		ImmutableArray<AnalyzerContractViolation> refused = Validate(WithSignals([ShellProbe.PromptFeeding(withoutDefault)]));
		ImmutableArray<AnalyzerContractViolation> accepted = Validate(WithSignals([ShellProbe.PromptFeeding(withDefault)]));

		AssertContains(refused, AnalyzerContractViolationCode.FlowSignalVariableUseGroupAbsentFromExpression);
		Assert.Empty(NewViolations(accepted));
	}

	[Fact]
	public void ABlankDefaultVariableNameIsAViolation()
	{
		FlowSignalVariableUse blank = new() { UsePattern = ShellProbeExpressions.PasswordOnStandardInput(), DefaultVariableName = "  " };

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithSignals([ShellProbe.PromptFeeding(blank)]));

		AssertContains(violations, AnalyzerContractViolationCode.FlowSignalVariableUseGroupAbsentFromExpression);
	}

	[Fact]
	public void ABlankUsePatternIsAViolation()
	{
		FlowSignalVariableUse blank = new()
		{
			UsePattern = new Regex(string.Empty, RegexOptions.CultureInvariant, RecognitionEnginePolicy.MATCH_BOUND)
		};

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithSignals([ShellProbe.PromptFeeding(blank)]));

		AssertContains(violations, AnalyzerContractViolationCode.FlowSignalVariableUsePatternBlank);
	}

	[Fact]
	public void AUsePatternThatDoesNotCarryThePolicyMatchBoundIsAViolation()
	{
		FlowSignalVariableUse offPolicy = new()
		{
			UsePattern = new Regex("sudo", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(_ONE_MILLISECOND))
		};

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithSignals([ShellProbe.PromptFeeding(offPolicy)]));

		AssertContains(violations, AnalyzerContractViolationCode.FlowSignalVariableUseMatchTimeoutNotPolicyBound);
	}

	[Theory]
	[InlineData(0, FlowSignalVariableUse.DEFAULT_MAX_LOGICAL_LINE_LENGTH, FlowSignalVariableUse.DEFAULT_ALIAS_DEPTH)]
	[InlineData(FlowSignalVariableUse.MAXIMUM_WINDOW_LINES + 1, FlowSignalVariableUse.DEFAULT_MAX_LOGICAL_LINE_LENGTH, FlowSignalVariableUse.DEFAULT_ALIAS_DEPTH)]
	[InlineData(FlowSignalVariableUse.DEFAULT_WINDOW_LINES, 0, FlowSignalVariableUse.DEFAULT_ALIAS_DEPTH)]
	[InlineData(FlowSignalVariableUse.DEFAULT_WINDOW_LINES, FlowSignalVariableUse.MAXIMUM_LOGICAL_LINE_LENGTH + 1, FlowSignalVariableUse.DEFAULT_ALIAS_DEPTH)]
	[InlineData(FlowSignalVariableUse.DEFAULT_WINDOW_LINES, FlowSignalVariableUse.DEFAULT_MAX_LOGICAL_LINE_LENGTH, -1)]
	[InlineData(FlowSignalVariableUse.DEFAULT_WINDOW_LINES, FlowSignalVariableUse.DEFAULT_MAX_LOGICAL_LINE_LENGTH, FlowSignalVariableUse.MAXIMUM_ALIAS_DEPTH + 1)]
	public void ABoundOutsideTheEnginesBoundsIsAViolation(int windowLines, int maxLogicalLineLength, int aliasDepth)
	{
		FlowSignalVariableUse outside = new()
		{
			UsePattern = ShellProbeExpressions.PasswordOnStandardInput(),
			WindowLines = windowLines,
			MaxLogicalLineLength = maxLogicalLineLength,
			AliasDepth = aliasDepth
		};

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithSignals([ShellProbe.PromptFeeding(outside)]));

		AssertContains(violations, AnalyzerContractViolationCode.FlowSignalVariableUseBoundInvalid);
	}

	[Fact]
	public void ABlankStatementSeparatorIsAViolation()
	{
		FlowSignalVariableUse blank = new()
		{
			UsePattern = ShellProbeExpressions.PasswordOnStandardInput(),
			StatementSeparators = [";", "  "]
		};

		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithSignals([ShellProbe.PromptFeeding(blank)]));

		AssertContains(violations, AnalyzerContractViolationCode.FlowSignalVariableUseStatementSeparatorBlank);
	}

	[Fact]
	public void ARuleVariableGroupTheRuleExpressionDoesNotDeclareIsAViolation()
	{
		ShellProbeAnalyzer analyzer = ShellProbe.AnalyzerWith(
			[],
			ShellProbe.Dialects,
			builtCredentialVariableUse: new RuleVariableUse { VariableGroupName = _ABSENT_GROUP });

		ImmutableArray<AnalyzerContractViolation> violations = Validate(analyzer);

		AssertContains(violations, AnalyzerContractViolationCode.RuleVariableUseGroupAbsentFromExpression);
	}

	// ---- the validator: wrapper tracking ----

	[Fact]
	public void EveryWrapperExpressionMustCarryThePolicyMatchBound()
	{
		Regex offPolicy = new("x", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(_ONE_MILLISECOND));
		WrapperTracking clean = ShellProbe.Wrappers();
		WrapperTracking[] variants =
		[
			clean with { FunctionDefinitionExpression = offPolicy },
			clean with { PositionalReferenceExpression = offPolicy },
			clean with { ParameterAliasExpression = offPolicy },
			clean with { CallSiteExpression = offPolicy },
			clean with { ArgumentExpression = offPolicy },
			clean with { CredentialBearingArgument = offPolicy }
		];

		foreach (WrapperTracking variant in variants)
		{
			ImmutableArray<AnalyzerContractViolation> violations = Validate(WithWrappers(variant));

			Assert.Contains(violations, violation => violation.Code == AnalyzerContractViolationCode.WrapperTrackingMatchTimeoutNotPolicyBound);
		}
	}

	[Fact]
	public void EveryWrapperExpressionMustDeclareTheGroupsTheEngineReads()
	{
		Regex noGroups = new("x", RegexOptions.CultureInvariant, RecognitionEnginePolicy.MATCH_BOUND);
		WrapperTracking clean = ShellProbe.Wrappers();
		WrapperTracking[] variants =
		[
			clean with { FunctionDefinitionExpression = noGroups },
			clean with { PositionalReferenceExpression = noGroups },
			clean with { ParameterAliasExpression = noGroups },
			clean with { CallSiteExpression = noGroups }
		];

		foreach (WrapperTracking variant in variants)
		{
			ImmutableArray<AnalyzerContractViolation> violations = Validate(WithWrappers(variant));

			Assert.Contains(violations, violation => violation.Code == AnalyzerContractViolationCode.WrapperTrackingGroupAbsentFromExpression);
		}
	}

	[Theory]
	[InlineData(0, WrapperTracking.DEFAULT_MAX_CALLS)]
	[InlineData(WrapperTracking.MAXIMUM_BODY_LINES + 1, WrapperTracking.DEFAULT_MAX_CALLS)]
	[InlineData(WrapperTracking.DEFAULT_MAX_BODY_LINES, 0)]
	[InlineData(WrapperTracking.DEFAULT_MAX_BODY_LINES, WrapperTracking.MAXIMUM_CALLS + 1)]
	public void AWrapperBoundOutsideTheEnginesBoundsIsAViolation(int maxBodyLines, int maxCalls)
	{
		ImmutableArray<AnalyzerContractViolation> violations = Validate(WithWrappers(ShellProbe.Wrappers(maxBodyLines, maxCalls)));

		AssertContains(violations, AnalyzerContractViolationCode.WrapperTrackingBoundInvalid);
	}

	[Fact]
	public void TheNewViolationCodesAreDistinctAndFollowTheExistingNumbering()
	{
		int[] values = [.. Enum.GetValues<AnalyzerContractViolationCode>().Select(code => (int)code)];

		Assert.Equal(values.Length, values.Distinct().Count());
		Assert.Equal(_FIRST_NEW_CODE, (int)AnalyzerContractViolationCode.DifferentArmBlocksDeclared + 1);
		Assert.Equal(_FIRST_PRECEDENCE_CODE, (int)AnalyzerContractViolationCode.ConstructPrecedenceListsAnUnknownOrRepeatedPatternClass);
		Assert.Equal(_LAST_PRECEDENCE_CODE, (int)AnalyzerContractViolationCode.ConstructPrecedenceTokenMalformed);
		Assert.Equal(_FIRST_EXECUTED_STRING_CODE, (int)AnalyzerContractViolationCode.ExecutedStringContextMalformed);
		Assert.Equal(_LAST_EXECUTED_STRING_CODE, (int)AnalyzerContractViolationCode.ExecutedStringGrammarMalformed);
		Assert.Equal(_DEMOTION_KIND_CODE, (int)AnalyzerContractViolationCode.DemotionSignalKindUndeclared);
		Assert.Equal(_CODE_SPAN_CODE, (int)AnalyzerContractViolationCode.CodeSpanElementMalformed);
		Assert.Equal(_SOURCE_KIND_CODE, (int)AnalyzerContractViolationCode.SourceKindDeclarationMalformed);
		Assert.Equal(_NEUTRAL_OPERAND_PAIR_GROUP_CODE, (int)AnalyzerContractViolationCode.NeutralOperandPairGroupAbsentFromExpression);
		Assert.Equal(_NEUTRAL_OPERAND_PAIR_BOUND_CODE, (int)AnalyzerContractViolationCode.NeutralOperandPairWordExpressionMatchTimeoutNotPolicyBound);
		Assert.Equal(_CONDITIONAL_COMPILATION_OPENING_CODE, (int)AnalyzerContractViolationCode.ConditionalCompilationDirectiveOpeningMalformed);
		Assert.Equal(_CONDITIONAL_COMPILATION_WORD_CODE, (int)AnalyzerContractViolationCode.ConditionalCompilationWellKnownSymbolWordMalformed);
		Assert.Equal(_MEMBER_DECLARATION_BOUND_CODE, (int)AnalyzerContractViolationCode.MemberDeclarationExpressionMatchTimeoutNotPolicyBound);
		Assert.Equal(_MEMBER_DECLARATION_MALFORMED_CODE, (int)AnalyzerContractViolationCode.MemberDeclarationExpressionMalformed);
		Assert.Equal(_MEMBER_KIND_WITHOUT_EXPRESSION_CODE, (int)AnalyzerContractViolationCode.MemberRecordKindWithoutMemberDeclarationExpression);
		Assert.Equal(_MEMBER_KIND_WITHOUT_EXPRESSION_CODE, values.Max());
	}

	// ---- the contract surface ----

	[Fact]
	public void TheNewContractTypesAreInitOnlyAndCarryNoDelegateNoSinkAndNoAnalyzedFile()
	{
		IEnumerable<PropertyInfo> properties = _newContractTypes.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

		Assert.All(properties, property =>
		{
			Assert.True(property.SetMethod is null || property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Any(modifier => modifier.Name == "IsExternalInit"));
			Assert.False(typeof(Delegate).IsAssignableFrom(property.PropertyType));
			Assert.False(typeof(TextWriter).IsAssignableFrom(property.PropertyType));
			Assert.NotEqual(typeof(FileRecognitionContext), property.PropertyType);
			Assert.NotEqual(typeof(MigrateAuth.Cli.Lexical.ZoneMask), property.PropertyType);
		});
	}

	[Fact]
	public void TheOnlyIntegersTheNewContractTypesCarryAreTheBoundsOfAnEngineWalk()
	{
		IEnumerable<PropertyInfo> integers = _newContractTypes
			.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			.Where(property => property.PropertyType == typeof(int));

		Assert.Equal(_boundPropertyNames.Order(), integers.Select(property => property.Name).Order());
	}

	[Fact]
	public void EveryCollectionTheNewDialectDeclarationSuppliesIsUnorderedByType()
	{
		PropertyInfo[] collections =
		[
			.. typeof(FileDialects).GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(property => property.PropertyType.IsGenericType)
		];

		Assert.NotEmpty(collections);
		Assert.All(collections, property => Assert.Equal(typeof(FrozenDictionary<,>), property.PropertyType.GetGenericTypeDefinition()));
	}

	[Fact]
	public void EveryNewElementIsOptionalAndDefaultsToTheBehaviourThatCameBefore()
	{
		FlowSignal bare = ShellProbe.ServiceToken;
		CandidateSelectionAndRecommendation element = ShellProbe.Element([]);

		Assert.True(bare.Dialects.IsEmpty);
		Assert.True(bare.ExcludedDialects.IsEmpty);
		Assert.Null(bare.VariableUse);
		Assert.Null(element.FileDialects);
		Assert.Null(element.WrapperTracking);
		Assert.Equal(FlowSignalVariableUse.DEFAULT_VARIABLE_GROUP_NAME, new RuleVariableUse().VariableGroupName);
	}

	[Fact]
	public void OnlyTheShellAnalyzerDeclaresAnyOfTheNewElementsAndTheJavaScriptAndTypeScriptAnalyzersDeclareTheirDialectsAlone()
	{
		ILanguageAnalyzer[] analyzers = [.. LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName.Values.Distinct()];
		ILanguageAnalyzer shell = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("Shell"));
		ILanguageAnalyzer javaScript = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("JavaScript"));
		ILanguageAnalyzer typeScript = analyzers.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains("TypeScript"));

		Assert.NotNull(shell.CandidateSelectionAndRecommendation.FileDialects);
		Assert.NotNull(shell.CandidateSelectionAndRecommendation.WrapperTracking);
		Assert.Contains(shell.DetectionRulesByPatternClass.Values, rule => rule.VariableUse is not null);
		Assert.NotNull(javaScript.CandidateSelectionAndRecommendation.FileDialects);
		Assert.Null(javaScript.CandidateSelectionAndRecommendation.WrapperTracking);
		Assert.All(javaScript.CandidateSelectionAndRecommendation.FlowSignals, signal =>
		{
			Assert.True(signal.Dialects.IsDefaultOrEmpty);
			Assert.Null(signal.VariableUse);
		});
		Assert.All(javaScript.DetectionRulesByPatternClass.Values, rule => Assert.Null(rule.VariableUse));
		Assert.NotNull(typeScript.CandidateSelectionAndRecommendation.FileDialects);
		Assert.Null(typeScript.CandidateSelectionAndRecommendation.WrapperTracking);
		Assert.All(typeScript.CandidateSelectionAndRecommendation.FlowSignals, signal =>
		{
			Assert.True(signal.Dialects.IsDefaultOrEmpty);
			Assert.Null(signal.VariableUse);
		});
		Assert.All(typeScript.DetectionRulesByPatternClass.Values, rule => Assert.Null(rule.VariableUse));
		Assert.All(analyzers.Where(analyzer => analyzer != shell && analyzer != javaScript && analyzer != typeScript), analyzer =>
		{
			Assert.Null(analyzer.CandidateSelectionAndRecommendation.FileDialects);
			Assert.Null(analyzer.CandidateSelectionAndRecommendation.WrapperTracking);
			Assert.All(analyzer.CandidateSelectionAndRecommendation.FlowSignals, signal =>
			{
				Assert.True(signal.Dialects.IsDefaultOrEmpty);
				Assert.True(signal.ExcludedDialects.IsDefaultOrEmpty);
				Assert.Null(signal.VariableUse);
			});
			Assert.All(analyzer.DetectionRulesByPatternClass.Values, rule => Assert.Null(rule.VariableUse));
		});
	}

	// ---- a profile that declares none of them reads as without them ----

	[Theory]
	[InlineData("#!/bin/bash\ncurl -u svc:pw https://example.invalid/\nread -p \"Password: \" PW\necho \"$PW\" | sudo -S true\nUSE_SERVICE_TOKEN\n")]
	[InlineData("fetch() {\n\tcurl -u svc:pw https://example.invalid/\n}\nfetch admin:hunter2 https://example.invalid/a\n")]
	[InlineData("mkauth into TOK\nmkauth\n")]
	[InlineData("# nothing here\necho hello\n")]
	public void ADeclarationNothingExercisesChangesNothingInTheConstructsOrTheEvidence(string source)
	{
		ShellProbeAnalyzer bare = ShellProbe.AnalyzerWith([ShellProbe.ServiceToken]);
		ShellProbeAnalyzer declaring = ShellProbe.AnalyzerWith([ShellProbe.ServiceToken], ShellProbe.Dialects, ShellProbe.Wrappers());

		RecognitionResult before = ShellProbe.Recognize(bare, source).Result;
		RecognitionResult after = ShellProbe.Recognize(declaring, source).Result;

		Assert.Equal(before.FlowEvidence, after.FlowEvidence);
		Assert.True(before.Constructs.SequenceEqual(after.Constructs));
	}

	// ---- helpers ----

	private static ImmutableArray<AnalyzerContractViolation> Validate(ILanguageAnalyzer analyzer)
	{
		return AnalyzerContractValidator.Validate(analyzer);
	}

	private static IEnumerable<AnalyzerContractViolation> NewViolations(ImmutableArray<AnalyzerContractViolation> violations)
	{
		return violations.Where(violation => (int)violation.Code >= _FIRST_NEW_CODE && (int)violation.Code <= _LAST_NEW_CODE);
	}

	private static void AssertContains(ImmutableArray<AnalyzerContractViolation> violations, AnalyzerContractViolationCode code)
	{
		Assert.Contains(violations, violation => violation.Code == code);
	}

	private static ShellProbeAnalyzer CleanAnalyzer()
	{
		return ShellProbe.AnalyzerWith(
			[ShellProbe.SilentReadInBash, ShellProbe.PromptReadInBashAndDash, ShellProbe.PromptFeedingAPasswordRead, ShellProbe.SelectPromptExceptInZsh, ShellProbe.ServiceToken],
			ShellProbe.Dialects,
			ShellProbe.Wrappers(),
			new RuleVariableUse());
	}

	private static ShellProbeAnalyzer WithDialects(FileDialects dialects)
	{
		return ShellProbe.AnalyzerWith([], dialects);
	}

	private static ShellProbeAnalyzer WithSignals(ImmutableArray<FlowSignal> signals, FileDialects? dialects = null)
	{
		return ShellProbe.AnalyzerWith(signals, dialects ?? ShellProbe.Dialects);
	}

	private static ShellProbeAnalyzer WithWrappers(WrapperTracking wrappers)
	{
		return ShellProbe.AnalyzerWith([], ShellProbe.Dialects, wrappers);
	}
}
