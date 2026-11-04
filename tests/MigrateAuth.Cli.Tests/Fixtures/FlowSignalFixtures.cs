using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.Fixtures;

/// <summary>
/// Every flow-signal expression the tests use, generated at build time and declaring the policy
/// match timeout. They live under tests only and the tokens below are invented, so the engine's flow
/// evaluation is exercised independently of the signals any language catalog declares.
/// </summary>
internal static partial class FlowSignalFixtureExpressions
{
	[GeneratedRegex(
		@"\bPromptForUser\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex InteractiveToken();

	[GeneratedRegex(
		@"\bUseServiceAccount\b",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex FixedToken();

	/// <summary>Anchored on the opening quote of a literal, which the scanner masks as a delimiter.</summary>
	[GeneratedRegex(
		@"(?<anchor>"")DelimiterTail",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: RecognitionEnginePolicy.PROVISIONAL_MATCH_BOUND_IN_MILLISECONDS)]
	public static partial Regex QuotedDelimiterAnchored();
}

/// <summary>Named flow signals over those expressions, one per kind and zone the tests need.</summary>
internal static class FlowSignalFixtures
{
	internal const string INTERACTIVE_TOKEN = "PromptForUser";
	internal const string FIXED_TOKEN = "UseServiceAccount";

	private const string _INTERACTIVE_CODE_ONLY_NAME = "fixture-interactive-code-only";
	private const string _INTERACTIVE_BODY_NAME = "fixture-interactive-code-or-string-body";
	private const string _FIXED_CODE_ONLY_NAME = "fixture-fixed-code-only";
	private const string _FIXED_BODY_NAME = "fixture-fixed-code-or-string-body";
	private const string _DELIMITER_CODE_ONLY_NAME = "fixture-delimiter-code-only";
	private const string _DELIMITER_BODY_NAME = "fixture-delimiter-code-or-string-body";

	internal static FlowSignal InteractiveCodeOnly { get; } = new()
	{
		Name = _INTERACTIVE_CODE_ONLY_NAME,
		Kind = FlowSignalKind.InteractiveUser,
		Zone = FlowSignalZone.CodeOnly,
		Expression = FlowSignalFixtureExpressions.InteractiveToken()
	};

	internal static FlowSignal InteractiveCodeOrStringBody { get; } = new()
	{
		Name = _INTERACTIVE_BODY_NAME,
		Kind = FlowSignalKind.InteractiveUser,
		Zone = FlowSignalZone.CodeOrStringBody,
		Expression = FlowSignalFixtureExpressions.InteractiveToken()
	};

	internal static FlowSignal FixedCodeOnly { get; } = new()
	{
		Name = _FIXED_CODE_ONLY_NAME,
		Kind = FlowSignalKind.FixedCredential,
		Zone = FlowSignalZone.CodeOnly,
		Expression = FlowSignalFixtureExpressions.FixedToken()
	};

	internal static FlowSignal FixedCodeOrStringBody { get; } = new()
	{
		Name = _FIXED_BODY_NAME,
		Kind = FlowSignalKind.FixedCredential,
		Zone = FlowSignalZone.CodeOrStringBody,
		Expression = FlowSignalFixtureExpressions.FixedToken()
	};

	internal static FlowSignal DelimiterAnchoredCodeOnly { get; } = new()
	{
		Name = _DELIMITER_CODE_ONLY_NAME,
		Kind = FlowSignalKind.FixedCredential,
		Zone = FlowSignalZone.CodeOnly,
		Expression = FlowSignalFixtureExpressions.QuotedDelimiterAnchored()
	};

	internal static FlowSignal DelimiterAnchoredCodeOrStringBody { get; } = new()
	{
		Name = _DELIMITER_BODY_NAME,
		Kind = FlowSignalKind.FixedCredential,
		Zone = FlowSignalZone.CodeOrStringBody,
		Expression = FlowSignalFixtureExpressions.QuotedDelimiterAnchored()
	};
}

/// <summary>
/// Any analyzer, with its flow signals replaced. Everything else is the wrapped analyzer's own, so
/// a test states the one thing it varies. Declaring signals is done here, under tests, and never in
/// a language catalog.
/// </summary>
internal sealed class FlowSignalFixtureAnalyzer : ILanguageAnalyzer
{
	private readonly ILanguageAnalyzer _inner;

	internal FlowSignalFixtureAnalyzer(ILanguageAnalyzer inner, ImmutableArray<FlowSignal> flowSignals)
	{
		_inner = inner;
		CandidateSelectionAndRecommendation = new CandidateSelectionAndRecommendation
		{
			PreselectionAnchors = inner.CandidateSelectionAndRecommendation.PreselectionAnchors,
			TemplateProvider = inner.CandidateSelectionAndRecommendation.TemplateProvider,
			LanguageFallbackTextByFlow = inner.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow,
			FlowSignals = flowSignals
		};
	}

	public LexicalProfile LexicalProfile => _inner.LexicalProfile;

	public LanguageIdentification LanguageIdentification => _inner.LanguageIdentification;

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass => _inner.DetectionRulesByPatternClass;

	public LanguageShapedDemotionSignals DemotionSignals => _inner.DemotionSignals;

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; }

	public FrozenSet<SubsumptionRule> SubsumptionRelation => _inner.SubsumptionRelation;
}
