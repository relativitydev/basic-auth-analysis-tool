using System.Collections.Immutable;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE STRUCTURE OF THE SHIPPED `TypeScript` COST GATE, asserted in the normal suite (no timing, no figure): the stress set of <see cref="TypeScriptShippedCostGateStressSet"/> is legal for the reader, carries every shape
/// the cost gate names and every shape of the generic-argument group and the tail refusal, stands under every file form the analyzer reads, and the shipped profile declares nothing the lexical budget would charge, which is why the
/// budget's own measurement (<see cref="TypeScriptLexicalBudgetTimingTests"/>) is a measurement of the scan.
/// </summary>
public sealed class TypeScriptShippedStressSetShapeTests
{
	private const string _TEN_THOUSAND_LINES_SHAPE_FRAGMENT = "ten-thousand-lines-of-non-null-assertion-then-slash";
	private const string _DECLARATION_FILE_SHAPE_PREFIX = "dts-anchor-dense";
	private const string _GENERIC_GROUP_SHAPE_FRAGMENT = "generic-body";
	private const string _REFUSAL_SHAPE_FRAGMENT = "refusal";
	private const int _MINIMUM_ADDED_SHAPE_COUNT = 40;
	private const int _MINIMUM_TEN_THOUSAND_LINE_COUNT = 10_000;
	private const char _LINE_FEED = '\n';

	private static readonly Lazy<ImmutableArray<MatchBoundStressShape>> _shapes = new(TypeScriptShippedCostGateStressSet.Build);

	/// <summary>The shapes the cost gate names, each as a name or a name fragment the set must carry.</summary>
	private static readonly ImmutableArray<string> _designShapeFragments =
	[
		"ts-long-line-annotations-at-the-ceiling",
		"ts-long-line-unions-at-the-ceiling",
		"ts-generic-heavy-declarations",
		"ts-decorator-chains",
		"tsx-nested-templates",
		_TEN_THOUSAND_LINES_SHAPE_FRAGMENT,
		"dts-anchor-dense-deep-nesting",
		"ts-long-line-n3-generic-body-at-the-length-bound",
		"ts-long-line-n4-refusal-body-at-the-bound"
	];

	[Fact]
	public void TheShippedSetHoldsEveryShapeOfTheDraftSetAndAddsTheShapesTheShippedExpressionsNeed()
	{
		ImmutableArray<MatchBoundStressShape> added = TypeScriptShippedCostGateStressSet.BuildTheAddedShapes();
		HashSet<string> names = [.. _shapes.Value.Select(shape => shape.Name)];

		Assert.Equal(_shapes.Value.Length, names.Count);
		Assert.True(added.Length >= _MINIMUM_ADDED_SHAPE_COUNT, $"{added.Length} shapes added.");
		Assert.Equal(_shapes.Value.Length - added.Length, TypeScriptMatchBoundStressSet.Build().Length);
		Assert.All(added, shape => Assert.StartsWith(TypeScriptShippedCostGateStressSet.SHAPE_NAME_PREFIX, shape.Name, StringComparison.Ordinal));
	}

	[Fact]
	public void EveryShapeTheDesignNamesIsInTheShippedSet()
	{
		Assert.All(_designShapeFragments, fragment => Assert.Contains(_shapes.Value, shape => shape.Name.Contains(fragment, StringComparison.Ordinal)));
		Assert.Contains(_shapes.Value, shape => shape.Name.StartsWith(_DECLARATION_FILE_SHAPE_PREFIX, StringComparison.Ordinal));
		Assert.Contains(_shapes.Value, shape => shape.Name.Contains(_GENERIC_GROUP_SHAPE_FRAGMENT, StringComparison.Ordinal));
		Assert.Contains(_shapes.Value, shape => shape.Name.Contains(_REFUSAL_SHAPE_FRAGMENT, StringComparison.Ordinal));
	}

	[Fact]
	public void TheShippedSetStandsUnderEveryFileFormTheAnalyzerReads()
	{
		HashSet<string> extensions = [.. _shapes.Value.Select(shape => shape.FileExtension)];

		Assert.Contains(TypeScriptMatchBoundStressSet.FILE_EXTENSION, extensions);
		Assert.Contains(TypeScriptMatchBoundStressSet.MARKUP_FILE_EXTENSION, extensions);
		Assert.Contains(TypeScriptMatchBoundStressSet.DECLARATION_FILE_EXTENSION, extensions);
		Assert.Contains(TypeScriptShippedCostGateStressSet.MODULE_FILE_EXTENSION, extensions);
		Assert.Contains(TypeScriptShippedCostGateStressSet.COMMONJS_FILE_EXTENSION, extensions);
		Assert.All(_shapes.Value, shape => Assert.StartsWith(".", shape.FileExtension, StringComparison.Ordinal));
	}

	[Fact]
	public void EveryShapeOfTheShippedSetRespectsTheReaderCeilingsSoTheEngineWouldReceiveIt()
	{
		Assert.All(_shapes.Value, shape => Assert.True(
			Encoding.UTF8.GetByteCount(shape.Text) < ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES,
			shape.Name));
		Assert.All(_shapes.Value, shape => Assert.True(
			LongestLineLength(shape.Text) <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS,
			shape.Name));
	}

	[Fact]
	public void EveryTenThousandLineShapeHoldsAtLeastTenThousandLines()
	{
		ImmutableArray<MatchBoundStressShape> shapes = [.. _shapes.Value.Where(shape => shape.Name.Contains(_TEN_THOUSAND_LINES_SHAPE_FRAGMENT, StringComparison.Ordinal))];

		Assert.NotEmpty(shapes);
		Assert.All(shapes, shape => Assert.True(shape.Text.Count(character => character == _LINE_FEED) >= _MINIMUM_TEN_THOUSAND_LINE_COUNT, shape.Name));
	}

	[Fact]
	public void TheTypeScriptProfileDeclaresNoExpressionTheLexicalBudgetCoversAndTheAnalyzerDeclaresNoFirstLineRule()
	{
		ILanguageAnalyzer analyzer = TypeScriptRegisteredExpressions.Analyzer;

		Assert.All(analyzer.LexicalProfile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.InterpolationIdentifierExtent));
		Assert.All(analyzer.LexicalProfile.QuoteRulesInEngineOrder, rule => Assert.Null(rule.CapturedTerminator));
		Assert.Null(analyzer.LanguageIdentification.FirstLineDetectionExpression);
		Assert.NotEqual(LexicalEnginePolicy.LEXICAL_BUDGET, analyzer.LexicalProfile.ConstantDeclarationExpression.MatchTimeout);
	}

	private static int LongestLineLength(string text)
	{
		return text.Split(_LINE_FEED).Max(line => line.Length);
	}
}
