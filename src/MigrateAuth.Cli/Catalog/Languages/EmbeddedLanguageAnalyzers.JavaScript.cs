using System.Collections.Frozen;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `JavaScript` analyzer: the lexical profile of ECMAScript source (line and block comments, the hashbang that opens only
/// at the first position, the two quoted strings that continue on an escaped line feed, the template literal with its nesting holes, and the regular-expression literal whose
/// opening a backward predicate decides), the identification of its files, its seventeen pattern classes, its demotion signals and its declared coverage.
///
/// FILES. The analyzer type is <c>file</c>-scoped, so its expressions live in <see cref="JavaScriptPatternExpressions"/>, its detection rules in
/// <see cref="JavaScriptRuleDeclarations"/>, its demotion signals in <see cref="JavaScriptDemotionDeclarations"/> and its declared coverage in
/// <see cref="JavaScriptDeclaredCoverage"/>. The registration is the partial method below.
///
/// FLOW, DIALECTS AND TEMPLATES. The sixteen flow signals live in <see cref="JavaScriptFlowSignalDeclarations"/>, the recommendation templates and the language fallback text of each
/// flow in <see cref="JavaScriptTemplateDeclarations"/>, and the dialects of a file are declared below: <c>script</c> by default and <c>node</c> for a Node.js shebang or the
/// <c>.cjs</c> extension, with none for <c>.jsx</c> or <c>.mjs</c>. No construct precedence, wrapper tracking, variable use, arithmetic context or executed-string context is
/// declared, because `JavaScript` needs none.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddJavaScriptAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new JavaScriptAnalyzer());
	}
}

/// <summary>
/// The `JavaScript` analyzer. File-local per the registration pattern every embedded language follows, so no engine type can name it and no special
/// case can be written against it.
/// </summary>
file sealed class JavaScriptAnalyzer : ILanguageAnalyzer
{
	private const string _PROFILE_NAME = "JavaScript";
	private const string _LANGUAGE_NAME = "JavaScript";

	private const string _SCRIPT_EXTENSION = ".js";
	private const string _MODULE_EXTENSION = ".mjs";
	private const string _COMMONJS_EXTENSION = ".cjs";
	private const string _JSX_EXTENSION = ".jsx";

	private const string _NODE_INTERPRETER = "node";
	private const string _NODEJS_INTERPRETER = "nodejs";

	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";
	private const string _HASHBANG_OPENING = "#!";

	public LexicalProfile LexicalProfile { get; } = BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = JavaScriptRuleDeclarations.BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = JavaScriptDemotionDeclarations.Build();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } = BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = JavaScriptRuleDeclarations.BuildSubsumptionRelation();

	// ------------------------------------------------------------------------------------------
	// Element 1: the lexical profile
	// ------------------------------------------------------------------------------------------

	private static LexicalProfile BuildLexicalProfile()
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = JavaScriptPatternExpressions.ConstantDeclaration(),
			MemberDeclarationExpression = JavaScriptPatternExpressions.MemberDeclaration(),
			KeywordCaseSensitivity = CaseSensitivity.Sensitive,
			DelimiterCaseSensitivity = CaseSensitivity.Sensitive,
			IdentifierCaseSensitivity = IdentifierCaseSensitivityMode.Sensitive,
			LineCommentRules =
			[
				new LineCommentRule { Opening = _LINE_COMMENT_OPENING },
				new LineCommentRule { Opening = _HASHBANG_OPENING, OpeningContextPredicate = JavaScriptLexicalParts.HashbangStandsAtTheStartOfTheText }
			],
			BlockCommentRules =
			[
				new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }
			],
			QuoteRules = JavaScriptLexicalParts.BuildQuoteRules(JavaScriptLexicalParts.RegexOpeningStandsHere)
		};

		return profile;
	}

	// ------------------------------------------------------------------------------------------
	// Element 2: identification
	// ------------------------------------------------------------------------------------------

	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _SCRIPT_EXTENSION, _MODULE_EXTENSION, _COMMONJS_EXTENSION, _JSX_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			FirstLineDetectionExpression = JavaScriptPatternExpressions.ShebangFirstLine(),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = JavaScriptDeclaredCoverage.Build()
		};

		return identification;
	}

	// ------------------------------------------------------------------------------------------
	// Element 5: candidate selection, templates, flow signals and the dialects of a file
	// ------------------------------------------------------------------------------------------

	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = JavaScriptRuleDeclarations.BuildPreselectionAnchors(),
			TemplateProvider = JavaScriptTemplateDeclarations.Provide,
			FlowSignals = JavaScriptFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = JavaScriptTemplateDeclarations.FallbackTextByFlow,
			FileDialects = BuildFileDialects()
		};

		return element;
	}

	/// <summary>
	/// The dialect a file is read in, resolved once per file: from the interpreter its first line names, then from its extension, then the default. A shebang that names
	/// <c>node</c> or <c>nodejs</c> and the <c>.cjs</c> extension resolve to <c>node</c>; every other file is <c>script</c>. A <c>.jsx</c> file and a <c>.mjs</c> file get no
	/// dialect of their own.
	/// </summary>
	private static FileDialects BuildFileDialects()
	{
		return new FileDialects
		{
			DefaultDialect = JavaScriptDialectNames.SCRIPT,
			ShebangInterpreters = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[_NODE_INTERPRETER] = JavaScriptDialectNames.NODE,
				[_NODEJS_INTERPRETER] = JavaScriptDialectNames.NODE
			}.ToFrozenDictionary(StringComparer.Ordinal),
			ExtensionDialects = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[_COMMONJS_EXTENSION] = JavaScriptDialectNames.NODE
			}.ToFrozenDictionary(StringComparer.Ordinal)
		};
	}
}
