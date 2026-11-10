using System.Collections.Frozen;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `TypeScript` analyzer: the lexical profile of `TypeScript` source (the `JavaScript` profile composed by reference, with the one coded branch of the
/// regular-expression predicate for the postfix non-null mark), the identification of its files, the eighteen inherited `JavaScript` classes and the two classes that exist for `TypeScript` alone, its demotion
/// signals, including the declaration files, and its declared coverage.
///
/// FILES. The analyzer type is <c>file</c>-scoped, so its lexical profile lives in <see cref="TypeScriptLexicalDeclarations"/>, its expressions in a part of <see cref="JavaScriptPatternExpressions"/>, its detection
/// rules in <see cref="TypeScriptRuleDeclarations"/>, its demotion signals in <see cref="TypeScriptDemotionDeclarations"/> and its declared coverage in <see cref="TypeScriptDeclaredCoverage"/>. The registration
/// is the partial method below.
///
/// FLOW, DIALECTS AND TEMPLATES. The flow signals (the sixteen of `JavaScript` and four of its own) and the dialect declaration are in <see cref="TypeScriptFlowSignalDeclarations"/> and
/// <see cref="TypeScriptDialectDeclarations"/>; the recommendation templates, which render the `JavaScript` text with the `typescript` fence and add the classes and forms that exist for `TypeScript`, and the two
/// fallback texts are in <see cref="TypeScriptTemplateDeclarations"/>.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddTypeScriptAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new TypeScriptAnalyzer());
	}
}

/// <summary>
/// The `TypeScript` analyzer. File-local per the registration pattern every embedded language follows, so no engine type can name it and no special case can be written against it.
/// </summary>
file sealed class TypeScriptAnalyzer : ILanguageAnalyzer
{
	private const string _LANGUAGE_NAME = "TypeScript";

	private const string _SCRIPT_EXTENSION = ".ts";
	private const string _MODULE_EXTENSION = ".mts";
	private const string _COMMONJS_EXTENSION = ".cts";
	private const string _JSX_EXTENSION = ".tsx";

	public LexicalProfile LexicalProfile { get; } = TypeScriptLexicalDeclarations.BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = TypeScriptRuleDeclarations.BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = TypeScriptDemotionDeclarations.Build();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } = BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = TypeScriptRuleDeclarations.BuildSubsumptionRelation();

	/// <summary>
	/// Four extensions and no first-line rule: a declaration file is attributed by its final extension, and an extensionless runner script has no interpreter in the base shebang table, so it is declared and not
	/// assessed.
	/// </summary>
	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _SCRIPT_EXTENSION, _MODULE_EXTENSION, _COMMONJS_EXTENSION, _JSX_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = TypeScriptDeclaredCoverage.Build()
		};

		return identification;
	}

	/// <summary>The anchors are those of `JavaScript`; the flow signals, the dialects, the templates and the fallback texts are `TypeScript`'s own declarations.</summary>
	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = TypeScriptRuleDeclarations.BuildPreselectionAnchors(),
			TemplateProvider = TypeScriptTemplateDeclarations.Provide,
			FlowSignals = TypeScriptFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = TypeScriptTemplateDeclarations.FallbackTextByFlow,
			FileDialects = TypeScriptDialectDeclarations.Build()
		};

		return element;
	}
}
