using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED `Go` ANALYZER AND ITS EXPRESSIONS, read from the binary and not restated: every pattern-class expression, the constant declaration of the symbol table, the content markers
/// and the generation markers of the demotion signals. The termination, agreement and equivalence checks run over these, so what they prove is about the binary and not about a copy.
///
/// The analyzer is reached through <see cref="EmbeddedLanguageAnalyzers.CreateAll"/>, the registration the binary ships, and is the one whose declared language is <c>Go</c>. A `Go` file has no
/// first-line rule, no flow signal and no quote that opens with a captured terminator, so none of those has an entry here.
/// </summary>
internal static class GoRegisteredExpressions
{
	internal const string LANGUAGE_NAME = "Go";

	internal const string CONSTANT_DECLARATION_IDENTIFIER = "ConstantDeclaration";

	internal const string MEMBER_DECLARATION_IDENTIFIER = "MemberDeclaration";

	internal const string GENERATION_MARKER_IDENTIFIER_PREFIX = "GenerationMarker";

	internal const string CONTENT_MARKER_IDENTIFIER_PREFIX = "ContentMarker";

	internal static ILanguageAnalyzer Analyzer { get; } = CreateTheShippedAnalyzer();

	/// <summary>The pattern-class expressions, by catalog identifier.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> Classes { get; } =
	[
		.. Analyzer.DetectionRulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => (entry.Key, entry.Value.Expression))
	];

	/// <summary>The support expressions: the constant declaration, the member declaration, the generation markers and the content markers.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> Support { get; } =
	[
		(CONSTANT_DECLARATION_IDENTIFIER, Analyzer.LexicalProfile.ConstantDeclarationExpression),
		(MEMBER_DECLARATION_IDENTIFIER, Analyzer.LexicalProfile.MemberDeclarationExpression!),
		.. Analyzer.DemotionSignals.GenerationMarkerExpressions.Select((expression, index) => ($"{GENERATION_MARKER_IDENTIFIER_PREFIX}{index}", expression)),
		.. Analyzer.DemotionSignals.ContentMarkers.Select((marker, index) => ($"{CONTENT_MARKER_IDENTIFIER_PREFIX}{index}", marker.Expression))
	];

	/// <summary>Every shipped expression, the classes first.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> All { get; } = [.. Classes, .. Support];

	/// <summary>The same pattern, read by the interpreter, for the agreement check: the generator's pattern text is the one Regex.ToString() returns.</summary>
	internal static Regex Interpreted(Regex generated)
	{
		return new Regex(generated.ToString(), generated.Options, generated.MatchTimeout);
	}

	private static ILanguageAnalyzer CreateTheShippedAnalyzer()
	{
		return EmbeddedLanguageAnalyzers.CreateAll().Single(analyzer => analyzer.LanguageIdentification.DeclaredLanguageNames.Contains(LANGUAGE_NAME));
	}
}
