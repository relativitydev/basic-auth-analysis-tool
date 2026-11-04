using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE REGISTERED `VBNet` EXPRESSIONS, read from the shipped analyzer and not restated: the nineteen class rules by catalog identifier, the profile's constant
/// declaration, the three content markers and the twenty flow signals, forty-three in all. The termination, agreement, exposure and cost checks run over
/// these, so what they prove is about the binary and not about a copy.
/// </summary>
internal static class VBNetRegisteredExpressions
{
	internal const string CONSTANT_DECLARATION_IDENTIFIER = "ConstantDeclaration";

	private const string _LANGUAGE_NAME = "VBNet";

	internal static ILanguageAnalyzer Analyzer { get; } = EmbeddedLanguageAnalyzers
		.CreateAll()
		.Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains(_LANGUAGE_NAME));

	/// <summary>The nineteen class expressions, by catalog identifier, in the catalog's order.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> Classes { get; } =
	[
		.. Analyzer.DetectionRulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => (entry.Key, entry.Value.Expression))
	];

	/// <summary>The support expressions: the constant declaration and the content markers.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> Support { get; } =
	[
		(CONSTANT_DECLARATION_IDENTIFIER, Analyzer.LexicalProfile.ConstantDeclarationExpression),
		.. Analyzer.DemotionSignals.ContentMarkers.Select((marker, index) => ("ContentMarker" + index, marker.Expression))
	];

	/// <summary>The twenty flow signals, by name.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> FlowSignals { get; } =
	[
		.. Analyzer.CandidateSelectionAndRecommendation.FlowSignals.Select(signal => (signal.Name, signal.Expression))
	];

	/// <summary>Every registered expression, the classes first.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> All { get; } = [.. Classes, .. Support, .. FlowSignals];

	/// <summary>The same pattern, read by the interpreter, for the agreement check: the generator's pattern text is the one Regex.ToString() returns.</summary>
	internal static Regex Interpreted(Regex generated)
	{
		return new Regex(generated.ToString(), generated.Options, generated.MatchTimeout);
	}
}
