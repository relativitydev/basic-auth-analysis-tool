using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE PER-MATCH LOW PROPOSAL OF <see cref="DetectionRule.LowWhenBothOperandsAreNeutral"/>, AND ITS INTERPRETED CROSS-CHECK.
/// A finding is proposed Low if and only if both condition groups participated and neither group's whole captured text
/// matches its word expression; the proposal sits inside the one minimum, so nothing promotes and the reasons coexist.
/// The analyzer is test-local and every name in it is invented.
/// </summary>
public sealed class NeutralOperandPairConditionTests
{
	private const string _CONSTRUCT_GROUP = "construct";
	private const string _FIRST_GROUP = "first";
	private const string _SECOND_GROUP = "second";
	private const string _PAIR_CLASS = "NPC-001";
	private const string _PROBE_PATH = "scripts/run.sh";
	private const string _FIRST_OPERAND_WORDS = "(?i:user|login|principal)";
	private const string _SECOND_OPERAND_WORDS = "(?i:password|secret|token)";
	private const string _NEUTRAL_PAIR_LINE = "pairof width:height";
	private const string _FIRST_NAMED_LINE = "pairof userName:height";
	private const string _SECOND_NAMED_LINE = "pairof width:secretValue";
	private const string _BOTH_NAMED_LINE = "pairof loginName:passwordText";
	private const string _MEMBER_CHAIN_FIRST_NAMED_LINE = "pairof account.principalName:height";
	private const string _SECOND_GROUP_ABSENT_LINE = "pairof width";
	private const string _COMMENTED_NEUTRAL_PAIR_LINE = "# pairof width:height";
	private const string _PAIR_PATTERN =
		@"(?<construct>pairof)[ \t]+(?<first>[A-Za-z_][A-Za-z0-9_.]*)(?:[ \t]*:[ \t]*(?<second>[A-Za-z_][A-Za-z0-9_.]*))?";
	private const string _INTERPRETED_SECOND_ABSENT_PATTERN =
		@"(?<construct>pairof)[ \t]+(?<first>[A-Za-z_][A-Za-z0-9_.]*):(?:(?<second>zzz))?[A-Za-z_][A-Za-z0-9_.]*";
	private const string _INTERPRETED_FIRST_SHORTER_PATTERN =
		@"(?<construct>pairof)[ \t]+(?<first>[A-Za-z_])[A-Za-z0-9_.]*:(?<second>[A-Za-z_][A-Za-z0-9_.]*)";
	private const string _GENERATED_PAIR_PATTERN =
		@"(?<construct>pairof)[ \t]+(?<first>[A-Za-z_][A-Za-z0-9_.]*):(?<second>[A-Za-z_][A-Za-z0-9_.]*)";

	[Fact]
	public void BothOperandsWithOrdinaryNamesProposeLowAndRecordTheReason()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_NEUTRAL_PAIR_LINE);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
	}

	[Theory]
	[InlineData(_FIRST_NAMED_LINE)]
	[InlineData(_SECOND_NAMED_LINE)]
	[InlineData(_BOTH_NAMED_LINE)]
	[InlineData(_MEMBER_CHAIN_FIRST_NAMED_LINE)]
	public void AtLeastOneCredentialNamedOperandKeepsTheClassLevelAndRecordsNoReason(string line)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(line);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	[Fact]
	public void TheWordsAreTestedOnTheWholeCapturedTextOfTheOperandAndNotOnItsLastIdentifier()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_MEMBER_CHAIN_FIRST_NAMED_LINE);

		Assert.Equal(ConfidenceLevel.High, Assert.Single(constructs).Level);
	}

	[Fact]
	public void AGroupThatDidNotParticipateProposesNothing()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_SECOND_GROUP_ABSENT_LINE);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	[Fact]
	public void ARuleThatDeclaresNoConditionKeepsANeutralPairAtTheClassLevel()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(condition: null, _NEUTRAL_PAIR_LINE);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
	}

	[Theory]
	[InlineData(ConfidenceLevel.High, SelfSufficiency.DeclaresSchemeAndCredentialCarrier)]
	[InlineData(ConfidenceLevel.High, SelfSufficiency.RequiresCallPathToBeEstablished)]
	[InlineData(ConfidenceLevel.Medium, SelfSufficiency.DeclaresSchemeAndCredentialCarrier)]
	public void TheNewLowBeatsAHighOrAMediumProposal(ConfidenceLevel baseLevel, SelfSufficiency selfSufficiency)
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(
			Condition(),
			baseLevel,
			selfSufficiency,
			_NEUTRAL_PAIR_LINE);

		Assert.Equal(ConfidenceLevel.Low, Assert.Single(constructs).Level);
	}

	[Fact]
	public void NothingPromotesAClassThatIsAlreadyLowAndBothReasonsCoexist()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(
			Condition(),
			ConfidenceLevel.Low,
			SelfSufficiency.DeclaresSchemeAndCredentialCarrier,
			_FIRST_NAMED_LINE,
			_NEUTRAL_PAIR_LINE);

		Assert.Equal(2, constructs.Length);
		Assert.Equal(LowConfidenceReasons.PatternClass, constructs[0].LowReasons);
		Assert.Equal(LowConfidenceReasons.PatternClass | LowConfidenceReasons.NeutralOperandPair, constructs[1].LowReasons);
		Assert.All(constructs, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
	}

	[Fact]
	public void AnAnchorInACommentKeepsItsOwnReasonBesideTheNeutralOperandPair()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_COMMENTED_NEUTRAL_PAIR_LINE);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.CommentOrStringLiteral | LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
	}

	[Fact]
	public void AFindingThatIsNotLowCarriesNoReasonEvenWhenTheNewSignalIsFalse()
	{
		ImmutableArray<RecognizedConstruct> constructs = Recognize(_BOTH_NAMED_LINE);

		Assert.Equal(LowConfidenceReasons.None, Assert.Single(constructs).LowReasons);
		Assert.False(Assert.Single(constructs).TestCode);
	}

	[Fact]
	public void WhenTheInterpretedCaptureOfAConditionGroupDisagreesTheInterpretedCapturesAreUsedAndTheDefectIsCounted()
	{
		DivergingRegex expression = new(_GENERATED_PAIR_PATTERN, _INTERPRETED_SECOND_ABSENT_PATTERN);

		(ImmutableArray<RecognizedConstruct> constructs, ImmutableArray<EmittedFact> skipped) = RecognizeWith(expression, _NEUTRAL_PAIR_LINE);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
		Assert.Equal(LowConfidenceReasons.None, construct.LowReasons);
		Assert.Equal(SkippedItemCategory.MalformedMatch, Assert.Single(skipped).SkippedItem);
	}

	[Fact]
	public void WhenTheInterpretedIndexOrLengthOfAGroupDisagreesTheInterpretedCaptureDecidesTheLevel()
	{
		DivergingRegex expression = new(_GENERATED_PAIR_PATTERN, _INTERPRETED_FIRST_SHORTER_PATTERN);

		(ImmutableArray<RecognizedConstruct> constructs, ImmutableArray<EmittedFact> skipped) = RecognizeWith(expression, _FIRST_NAMED_LINE);

		RecognizedConstruct construct = Assert.Single(constructs);
		Assert.Equal(ConfidenceLevel.Low, construct.Level);
		Assert.Equal(LowConfidenceReasons.NeutralOperandPair, construct.LowReasons);
		Assert.Equal(SkippedItemCategory.MalformedMatch, Assert.Single(skipped).SkippedItem);
	}

	[Fact]
	public void WhenTheTwoEnginesAgreeNothingIsCounted()
	{
		DivergingRegex expression = new(_GENERATED_PAIR_PATTERN, _GENERATED_PAIR_PATTERN);

		(ImmutableArray<RecognizedConstruct> constructs, ImmutableArray<EmittedFact> skipped) = RecognizeWith(expression, _NEUTRAL_PAIR_LINE);

		Assert.Equal(ConfidenceLevel.Low, Assert.Single(constructs).Level);
		Assert.Empty(skipped);
	}

	[Fact]
	public void TheValidatorRefusesAGroupNameTheExpressionDoesNotDeclare()
	{
		NeutralOperandPairCondition undeclaredFirst = Condition() with { FirstOperandGroupName = "undeclaredGroup" };
		NeutralOperandPairCondition undeclaredSecond = Condition() with { SecondOperandGroupName = "undeclaredGroup" };

		Assert.Contains(ValidatedCodes(undeclaredFirst), code => code == AnalyzerContractViolationCode.NeutralOperandPairGroupAbsentFromExpression);
		Assert.Contains(ValidatedCodes(undeclaredSecond), code => code == AnalyzerContractViolationCode.NeutralOperandPairGroupAbsentFromExpression);
	}

	[Fact]
	public void TheValidatorRefusesAWordExpressionThatDoesNotCarryTheMatchBound()
	{
		Regex unbounded = new(_FIRST_OPERAND_WORDS, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
		NeutralOperandPairCondition firstUnbounded = Condition() with { FirstOperandWordExpression = unbounded };
		NeutralOperandPairCondition secondUnbounded = Condition() with { SecondOperandWordExpression = unbounded };

		Assert.Contains(ValidatedCodes(firstUnbounded), code => code == AnalyzerContractViolationCode.NeutralOperandPairWordExpressionMatchTimeoutNotPolicyBound);
		Assert.Contains(ValidatedCodes(secondUnbounded), code => code == AnalyzerContractViolationCode.NeutralOperandPairWordExpressionMatchTimeoutNotPolicyBound);
	}

	[Fact]
	public void ACleanConditionAndNoConditionProduceNeitherNewCode()
	{
		Assert.DoesNotContain(ValidatedCodes(Condition()), IsANeutralOperandPairCode);
		Assert.DoesNotContain(ValidatedCodes(null), IsANeutralOperandPairCode);
	}

	[Fact]
	public void TheNewValidatorCodesFollowTheHighestCodeOfTheLine()
	{
		Assert.Equal(75, (int)AnalyzerContractViolationCode.NeutralOperandPairGroupAbsentFromExpression);
		Assert.Equal(76, (int)AnalyzerContractViolationCode.NeutralOperandPairWordExpressionMatchTimeoutNotPolicyBound);
		
	}

	private static bool IsANeutralOperandPairCode(AnalyzerContractViolationCode code)
	{
		return code is AnalyzerContractViolationCode.NeutralOperandPairGroupAbsentFromExpression
			or AnalyzerContractViolationCode.NeutralOperandPairWordExpressionMatchTimeoutNotPolicyBound;
	}

	private static AnalyzerContractViolationCode[] ValidatedCodes(NeutralOperandPairCondition? condition)
	{
		PairProbeAnalyzer analyzer = new(PairExpression(), condition, ConfidenceLevel.High, SelfSufficiency.DeclaresSchemeAndCredentialCarrier);

		return [.. AnalyzerContractValidator.Validate(analyzer).Select(violation => violation.Code)];
	}

	private static NeutralOperandPairCondition Condition()
	{
		return new NeutralOperandPairCondition
		{
			FirstOperandGroupName = _FIRST_GROUP,
			FirstOperandWordExpression = BoundedWords(_FIRST_OPERAND_WORDS),
			SecondOperandGroupName = _SECOND_GROUP,
			SecondOperandWordExpression = BoundedWords(_SECOND_OPERAND_WORDS)
		};
	}

	private static Regex BoundedWords(string pattern)
	{
		return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, RecognitionEnginePolicy.MATCH_BOUND);
	}

	private static Regex PairExpression()
	{
		return new Regex(_PAIR_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, RecognitionEnginePolicy.MATCH_BOUND);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(params string[] lines)
	{
		return Recognize(Condition(), lines);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(NeutralOperandPairCondition? condition, params string[] lines)
	{
		return Recognize(condition, ConfidenceLevel.High, SelfSufficiency.DeclaresSchemeAndCredentialCarrier, lines);
	}

	private static ImmutableArray<RecognizedConstruct> Recognize(
		NeutralOperandPairCondition? condition,
		ConfidenceLevel baseLevel,
		SelfSufficiency selfSufficiency,
		params string[] lines)
	{
		PairProbeAnalyzer analyzer = new(PairExpression(), condition, baseLevel, selfSufficiency);

		return RunEngine(analyzer, lines).Constructs;
	}

	private static (ImmutableArray<RecognizedConstruct> Constructs, ImmutableArray<EmittedFact> Skipped) RecognizeWith(Regex expression, string line)
	{
		PairProbeAnalyzer analyzer = new(expression, Condition(), ConfidenceLevel.High, SelfSufficiency.DeclaresSchemeAndCredentialCarrier);
		RecognitionHarness harness = new();
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());
		RecognitionResult result = engine.Recognize(ShellProbe.ContextFor(harness, analyzer, line, _PROBE_PATH));

		return (result.Constructs, harness.SkippedItems());
	}

	private static RecognitionResult RunEngine(PairProbeAnalyzer analyzer, string[] lines)
	{
		RecognitionHarness harness = new();
		RecognitionEngine engine = new(harness.EmissionPoint, new FrozenTimeProvider());

		return engine.Recognize(ShellProbe.ContextFor(harness, analyzer, ShellProbe.Source(lines), _PROBE_PATH));
	}

	/// <summary>
	/// A regular expression that matches with one pattern and reports another through <see cref="ToString"/>, which is
	/// what the engine re-builds its interpreted cross-check from: the stand-in for a generated expression the
	/// interpreter reads differently.
	/// </summary>
	private sealed class DivergingRegex : Regex
	{
		private readonly string _reportedPattern;

		internal DivergingRegex(string matchingPattern, string reportedPattern)
			: base(matchingPattern, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, RecognitionEnginePolicy.MATCH_BOUND)
		{
			_reportedPattern = reportedPattern;
		}

		public override string ToString()
		{
			return _reportedPattern;
		}
	}

	/// <summary>One detection rule over the pair expression, with the condition a test declares, in the shell-shaped profile.</summary>
	private sealed class PairProbeAnalyzer : ILanguageAnalyzer
	{
		private const string _DISPLAY_NAME = "Pair of two operands";
		private const string _EXEMPLAR_LANGUAGE_NAME = "ExemplarOne";

		private static readonly LexicalProfile _LEXICAL_PROFILE = ShellShapedProfile.Build();

		private readonly ILanguageAnalyzer _inner = Fixtures.ExemplarFixtureCatalog.Build().AnalyzersByLanguageName[_EXEMPLAR_LANGUAGE_NAME];

		internal PairProbeAnalyzer(Regex expression, NeutralOperandPairCondition? condition, ConfidenceLevel baseLevel, SelfSufficiency selfSufficiency)
		{
			DetectionRulesByPatternClass = new Dictionary<string, DetectionRule>
			{
				[_PAIR_CLASS] = new DetectionRule
				{
					Expression = expression,
					AnchorPosition = MatchAnchor.AtCaptureGroup(_CONSTRUCT_GROUP),
					BaseLevel = baseLevel,
					SelfSufficiency = selfSufficiency,
					SymbolicVerdictInterpretation = SymbolicVerdictInterpretation.NotSubjectToSymbolicResolution,
					DestinationCertainty = DestinationCertainty.Unknown,
					DisplayName = _DISPLAY_NAME,
					LowWhenBothOperandsAreNeutral = condition
				}
			}.ToFrozenDictionary(StringComparer.Ordinal);
		}

		public LexicalProfile LexicalProfile => _LEXICAL_PROFILE;

		public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

		public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; }

		public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

		public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation => _inner.CandidateSelectionAndRecommendation;

		public FrozenSet<SubsumptionRule> SubsumptionRelation => FrozenSet<SubsumptionRule>.Empty;
	}
}
