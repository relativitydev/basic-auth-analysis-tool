using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE SHAPES OF THE `Python` FIRST-LINE STRESS SET ARE WHAT THE SET SAYS THEY ARE, and the shipped expression answers each of them without spending its timeout. The figures of the shapes are timed in the shipped
/// cost gate, which is run apart from the suite; this class keeps no clock. It asserts that every shape is a line of exactly the reader's ceiling, that the names are unique, that the expression claims the
/// shapes named valid and refuses the rest, and that the expression carries the lexical budget as its timeout, so a shape that made it backtrack without bound would throw here instead of passing.
/// </summary>
public sealed class PythonFirstLineStressShapeTests
{
	private const int _MINIMUM_VALID_SHAPES = 4;
	private const int _MINIMUM_REFUSED_SHAPES = 10;

	[Fact]
	public void EveryShapeIsALineOfExactlyTheReadingCeilingAndTheNamesAreUnique()
	{
		ImmutableArray<FirstLineStressShape> shapes = PythonFirstLineStressSet.Build();

		Assert.All(shapes, shape => Assert.Equal(PythonFirstLineStressSet.LINE_LENGTH_IN_CHARACTERS, shape.Line.Length));
		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void TheCatalogsFourNamedShapesAreInTheSet()
	{
		string[] names = [.. PythonFirstLineStressSet.Build().Select(shape => shape.Name)];

		Assert.Contains("shebang-with-an-interpreter-path-of-many-segments", names);
		Assert.Contains("shebang-through-env-with-many-flags-and-no-interpreter", names);
		Assert.Contains("shebang-then-blanks-and-no-interpreter", names);
		Assert.Contains("valid-shebang-with-a-versioned-interpreter-and-a-long-digit-run", names);
	}

	[Fact]
	public void TheShippedExpressionClaimsTheShapesNamedValidAndRefusesTheRest()
	{
		Regex expression = PythonShippedAnalyzer.Instance.LanguageIdentification.FirstLineDetectionExpression!;
		ImmutableArray<FirstLineStressShape> shapes = PythonFirstLineStressSet.Build();

		Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, expression.MatchTimeout);
		Assert.True(shapes.Count(shape => IsNamedValid(shape)) >= _MINIMUM_VALID_SHAPES);
		Assert.True(shapes.Count(shape => !IsNamedValid(shape)) >= _MINIMUM_REFUSED_SHAPES);

		foreach (FirstLineStressShape shape in shapes)
		{
			Assert.True(expression.IsMatch(shape.Line) == IsNamedValid(shape), shape.Name);
		}
	}

	private static bool IsNamedValid(FirstLineStressShape shape)
	{
		return shape.Name.StartsWith(PythonFirstLineStressSet.VALID_SHAPE_NAME_PREFIX, StringComparison.Ordinal);
	}
}
