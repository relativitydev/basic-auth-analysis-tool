using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED `Shell` ANALYZER AND ITS EXPRESSIONS, read from the binary and not restated: the profile's two here-document openings, the constant declaration,
/// the first-line rule, every rule and every signal. The termination, agreement and parity checks run
/// over these, so what they prove is about the binary and not about a copy.
///
/// The analyzer is reached through <see cref="EmbeddedLanguageAnalyzers.CreateAll"/>, the registration the binary ships, and is the one whose declared
/// language is <c>Shell</c>.
/// </summary>
internal static class ShellRegisteredExpressions
{
	internal const string CONSTANT_DECLARATION_IDENTIFIER = "ConstantDeclaration";
	internal const string PLAIN_OPENING_IDENTIFIER = "PlainOpening";
	internal const string TAB_STRIPPING_OPENING_IDENTIFIER = "TabStrippingOpening";
	internal const string FIRST_LINE_IDENTIFIER = "ShebangFirstLine";

	private const string _LANGUAGE_NAME = "Shell";
	private const string _PLAIN_HERE_DOCUMENT_OPENING = "<<";
	private const string _TAB_STRIPPING_HERE_DOCUMENT_OPENING = "<<-";

	internal static ILanguageAnalyzer Analyzer { get; } = CreateTheShippedAnalyzer();

	/// <summary>The pattern-class expressions, by catalog identifier.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> Classes { get; } =
	[
		.. Analyzer.DetectionRulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => (entry.Key, entry.Value.Expression))
	];

	/// <summary>The support expressions: the constant declaration, the content markers, the two here-document openings and the first-line rule.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> Support { get; } =
	[
		(CONSTANT_DECLARATION_IDENTIFIER, Analyzer.LexicalProfile.ConstantDeclarationExpression),
		.. Analyzer.DemotionSignals.ContentMarkers.Select((marker, index) => ($"ContentMarker{index}", marker.Expression)),
		(PLAIN_OPENING_IDENTIFIER, OpeningOf(_PLAIN_HERE_DOCUMENT_OPENING)),
		(TAB_STRIPPING_OPENING_IDENTIFIER, OpeningOf(_TAB_STRIPPING_HERE_DOCUMENT_OPENING)),
		(FIRST_LINE_IDENTIFIER, Analyzer.LanguageIdentification.FirstLineDetectionExpression!)
	];

	/// <summary>The flow signals, by name.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> FlowSignals { get; } =
	[
		.. Analyzer.CandidateSelectionAndRecommendation.FlowSignals.Select(signal => (signal.Name, signal.Expression))
	];

	/// <summary>Every shipped expression, the classes first.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> All { get; } = [.. Classes, .. Support, .. FlowSignals];

	/// <summary>The same pattern, read by the interpreter, for the agreement check: the generator's pattern text is the one Regex.ToString() returns.</summary>
	internal static Regex Interpreted(Regex generated)
	{
		return new Regex(generated.ToString(), generated.Options, generated.MatchTimeout);
	}

	private static Regex OpeningOf(string opening)
	{
		return Analyzer.LexicalProfile.QuoteRulesInEngineOrder.Single(rule => rule.Opening == opening).CapturedTerminator!.OpeningExpression;
	}

	private static ILanguageAnalyzer CreateTheShippedAnalyzer()
	{
		return EmbeddedLanguageAnalyzers.CreateAll().Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains(_LANGUAGE_NAME));
	}
}
