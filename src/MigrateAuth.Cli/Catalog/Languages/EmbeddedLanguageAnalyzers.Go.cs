using System.Collections.Frozen;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `Go` analyzer, an analyzer of the language catalog: the lexical profile of `Go` source (the line and block comments, and the interpreted, rune and raw string literals, with no
/// predicate and no change to the vocabulary), the identification of its files by the <c>.go</c> extension alone, its thirteen pattern classes, its demotion signals and its declared coverage.
///
/// FILES. The analyzer type is <c>file</c>-scoped, so its lexical profile lives in <see cref="GoLexicalDeclarations"/>, its expressions in <see cref="GoPatternExpressions"/>, its detection rules in
/// <see cref="GoRuleDeclarations"/>, its demotion signals in <see cref="GoDemotionDeclarations"/> and its declared coverage in <see cref="GoDeclaredCoverage"/>. The registration is the partial method
/// below.
///
/// NO FIRST-LINE RULE. A `Go` source file always has the <c>.go</c> extension, and the base table of interpreters has no entry for it, so a file with no extension is never attributed to `Go`: a
/// run script whose first line is a `Go` comment that is also a shell command is declared and not assessed.
///
/// FLOW, DIALECTS AND TEMPLATES. No dialect, wrapper tracking, variable use, arithmetic context, executed-string context or construct precedence is declared, because `Go` needs none. The eleven flow
/// signals are declared by <see cref="GoFlowSignalDeclarations"/> and the recommendation text of the ten classes that give advice, with the generic fallback text of each flow, by
/// <see cref="GoTemplateDeclarations"/>; the three Low classes answer no template and are routed to manual review.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddGoAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new GoAnalyzer());
	}
}

/// <summary>
/// The `Go` analyzer. File-local per the registration pattern every embedded language follows, so no engine type can name it and no special case can be written against it.
/// </summary>
file sealed class GoAnalyzer : ILanguageAnalyzer
{
	private const string _LANGUAGE_NAME = "Go";

	private const string _SOURCE_EXTENSION = ".go";

	public LexicalProfile LexicalProfile { get; } = GoLexicalDeclarations.BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = GoRuleDeclarations.BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = GoDemotionDeclarations.Build();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } = BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = GoRuleDeclarations.BuildSubsumptionRelation();

	/// <summary>One extension and no first-line rule: the extension settles the question on its own, and the base table of interpreters has no entry that could claim a file with no extension.</summary>
	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _SOURCE_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = GoDeclaredCoverage.Build()
		};

		return identification;
	}

	/// <summary>The anchors are the six of the rules, the flow signals and the template provider are the `Go` declarations, and the language's own fallback text answers each flow; `Go` declares no dialect.</summary>
	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = GoRuleDeclarations.BuildPreselectionAnchors(),
			TemplateProvider = GoTemplateDeclarations.Provide,
			FlowSignals = GoFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = GoTemplateDeclarations.FallbackTextByFlow
		};

		return element;
	}
}
