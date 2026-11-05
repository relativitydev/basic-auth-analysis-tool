using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE SHIPPED `CSharp` ANALYZER AND THE EXPRESSIONS THE COST GATE MEASURES, read from the binary and not restated: every pattern-class
/// expression, and the word expressions a rule's per-match condition runs after a match, which the engine charges to the file's match
/// bound like any analyzer-declared expression. The cost gate's registry holds both kinds, so a word expression cannot stand outside
/// the measurement.
/// </summary>
internal static class CSharpRegisteredExpressions
{
	private const string _LANGUAGE_NAME = "CSharp";
	private const string _FIRST_OPERAND_WORDS_SUFFIX = "/FirstOperandWords";
	private const string _SECOND_OPERAND_WORDS_SUFFIX = "/SecondOperandWords";

	internal static ILanguageAnalyzer Analyzer { get; } = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME];

	/// <summary>The pattern-class expressions, by catalog identifier.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> Classes { get; } =
	[
		.. Analyzer.DetectionRulesByPatternClass.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => (entry.Key, entry.Value.Expression))
	];

	/// <summary>The word expressions of every declared per-match condition, by the identifier of the class that declares them.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> PerMatchConditionWords { get; } =
	[
		.. Analyzer.DetectionRulesByPatternClass
			.Where(entry => entry.Value.LowWhenBothOperandsAreNeutral is not null)
			.OrderBy(entry => entry.Key, StringComparer.Ordinal)
			.SelectMany(entry => new (string Identifier, Regex Expression)[]
			{
				($"{entry.Key}{_FIRST_OPERAND_WORDS_SUFFIX}", entry.Value.LowWhenBothOperandsAreNeutral!.FirstOperandWordExpression),
				($"{entry.Key}{_SECOND_OPERAND_WORDS_SUFFIX}", entry.Value.LowWhenBothOperandsAreNeutral!.SecondOperandWordExpression)
			})
	];

	/// <summary>Every expression the cost gate measures, the classes first.</summary>
	internal static IReadOnlyList<(string Identifier, Regex Expression)> All { get; } = [.. Classes, .. PerMatchConditionWords];

	internal static string FirstOperandWordsIdentifierOf(string patternClass)
	{
		return $"{patternClass}{_FIRST_OPERAND_WORDS_SUFFIX}";
	}

	internal static string SecondOperandWordsIdentifierOf(string patternClass)
	{
		return $"{patternClass}{_SECOND_OPERAND_WORDS_SUFFIX}";
	}
}
