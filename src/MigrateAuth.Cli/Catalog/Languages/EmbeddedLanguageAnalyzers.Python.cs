using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Catalog.Languages;

/// <summary>
/// The `Python` analyzer, the seventh language the embedded catalog carries: the lexical profile of `Python` source, the identification of its files and, in the files that follow, its pattern classes, demotion
/// signals and declared coverage.
///
/// FILES. The analyzer type is <c>file</c>-scoped, so its lexical profile lives in <see cref="PythonLexicalDeclarations"/>, its expressions in parts of <see cref="PythonPatternExpressions"/>, its detection rules in
/// <see cref="PythonRuleDeclarations"/>, its demotion signals in <see cref="PythonDemotionDeclarations"/> and its declared coverage in <see cref="PythonDeclaredCoverage"/>. The registration is the partial method
/// below. No member of any other language's file is read, renamed or made visible.
///
/// IDENTIFICATION. The analyzer claims <c>.py</c> and <c>.pyw</c> by extension. The first line is read only for an extensionless file, by <see cref="PythonPatternExpressions.FirstLineDetection"/>, which reads what
/// the base shebang table reads (the interpreter is the last path segment of the first word, or, after <c>env</c>, the first following word that is not a flag) for the interpreters <c>python</c>, <c>python2</c>,
/// <c>python3</c> and <c>python3.{ASCII digits}</c>, and refuses the interpreter followed by the flag <c>-S</c>. The forms <c>python2.{digits}</c>, <c>pypy</c>, <c>env VAR=1 python</c> and <c>python -S</c> are
/// declared and not claimed. <c>.pyi</c>, <c>.ipynb</c> and <c>.pyx</c> are not claimed.
/// </summary>
public static partial class EmbeddedLanguageAnalyzers
{
	static partial void AddPythonAnalyzer(ICollection<ILanguageAnalyzer> analyzers)
	{
		analyzers.Add(new PythonAnalyzer());
	}
}

/// <summary>
/// The `Python` first-line detection expression, a part of the class that holds the other `Python` expressions. It carries the lexical budget as its timeout, because it runs in the lexical pass once per
/// extensionless file.
/// </summary>
internal static partial class PythonPatternExpressions
{
	/// <summary>
	/// The first-line detection of a file with no recognized extension, anchored at the start of the first line: <c>#!</c>, optional white space, a path of <c>[^\s/]*/</c> segments, optionally <c>env</c> with
	/// its <c>-</c> flags (<c>-S</c> among them), the interpreter <c>python</c>, <c>python2</c>, <c>python3</c> or <c>python3.{ASCII digits}</c>, then, unless the interpreter is followed by white space and the
	/// flag <c>-S</c>, white space or the end of the line. Case-sensitive. White space is <c>\s</c>, the set the base shebang table splits a first line on (a vertical tab, a form feed and a no-break space separate words
	/// here as they do there), and the end is <c>\z</c>. The digits are <c>[0-9]</c> and not <c>\d</c>, so a decimal digit outside ASCII does not match, as in the base table. The refusal is a fixed literal after the
	/// positive match and holds no loop, so it adds no backtracking surface; it carries the lexical budget as its timeout.
	/// </summary>
	[GeneratedRegex(
		@"^#!\s*(?:[^\s/]*/)*(?:env\s+(?:-\S*\s+)*)?python(?:2|3(?:\.[0-9]+)?)?(?!\s+-S(?:\s|\z))(?:\s|\z)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
		matchTimeoutMilliseconds: LexicalEnginePolicy.PROVISIONAL_LEXICAL_BUDGET_IN_MILLISECONDS)]
	public static partial Regex FirstLineDetection();
}

/// <summary>
/// The `Python` analyzer. File-local per the registration pattern every embedded language follows, so no engine type can name it and no special case can be written against it.
/// </summary>
file sealed class PythonAnalyzer : ILanguageAnalyzer
{
	private const string _LANGUAGE_NAME = "Python";

	private const string _SCRIPT_EXTENSION = ".py";
	private const string _WINDOWS_SCRIPT_EXTENSION = ".pyw";

	public LexicalProfile LexicalProfile { get; } = PythonLexicalDeclarations.BuildLexicalProfile();

	public LanguageIdentification LanguageIdentification { get; } = BuildLanguageIdentification();

	public FrozenDictionary<string, DetectionRule> DetectionRulesByPatternClass { get; } = PythonRuleDeclarations.BuildDetectionRules();

	public LanguageShapedDemotionSignals DemotionSignals { get; } = PythonDemotionDeclarations.Build();

	public CandidateSelectionAndRecommendation CandidateSelectionAndRecommendation { get; } = BuildCandidateSelectionAndRecommendation();

	public FrozenSet<SubsumptionRule> SubsumptionRelation { get; } = PythonRuleDeclarations.BuildSubsumptionRelation();

	/// <summary>Two extensions and a first-line rule of the analyzer's own: an extensionless file is attributed only through the rule, so a base-table hit alone never yields this analyzer.</summary>
	private static LanguageIdentification BuildLanguageIdentification()
	{
		LanguageIdentification identification = new()
		{
			FileExtensions = new[] { _SCRIPT_EXTENSION, _WINDOWS_SCRIPT_EXTENSION }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			FirstLineDetectionExpression = PythonPatternExpressions.FirstLineDetection(),
			LanguageNameDerivation = static (_, _) => _LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = PythonDeclaredCoverage.Build()
		};

		return identification;
	}

	/// <summary>
	/// The anchors are those of the rule declarations. The templates answer for thirteen classes under both flows and answer null for the Low class, which is routed to manual review; the generic fallback text of
	/// each flow is the language's own, and the twelve flow signals are declared by <see cref="PythonFlowSignalDeclarations"/>.
	/// </summary>
	private static CandidateSelectionAndRecommendation BuildCandidateSelectionAndRecommendation()
	{
		CandidateSelectionAndRecommendation element = new()
		{
			PreselectionAnchors = PythonRuleDeclarations.BuildPreselectionAnchors(),
			TemplateProvider = PythonTemplateDeclarations.Provide,
			FlowSignals = PythonFlowSignalDeclarations.Build(),
			LanguageFallbackTextByFlow = PythonTemplateDeclarations.FallbackTextByFlow
		};

		return element;
	}
}
