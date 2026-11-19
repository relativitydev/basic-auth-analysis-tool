using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.MatchBoundStress;

/// <summary>
/// THE FIRST LINES THE `Python` LEXICAL-BUDGET GATE TIMES ARE WHAT THEY SAY THEY ARE, with no clock: the gate times the first-line expression of the registered analyzer on <see cref="PythonLexicalBudgetMeasurements.BuildFirstLineShapes"/>,
/// which holds the lines of <see cref="PythonFirstLineStressSet"/>, and asserts that every line the set calls accepted is matched and every other is not. This class makes that claim hold on the shipped expression before the timing
/// window runs, so a window never fails on a shape that was declared wrongly. Every line is at most the reader's line ceiling and every name is unique.
/// </summary>
public sealed class PythonLexicalBudgetFirstLineShapeTests
{
	[Fact]
	public void EveryFirstLineTheGateTimesIsReadByTheShippedExpressionAsTheSetDeclaresIt()
	{
		Regex expression = PythonShippedAnalyzer.Instance.LanguageIdentification.FirstLineDetectionExpression!;
		ImmutableArray<PythonFirstLineShape> shapes = PythonLexicalBudgetMeasurements.BuildFirstLineShapes();

		Assert.All(shapes, shape => Assert.True(expression.IsMatch(shape.Line) == shape.Accepted, shape.Name));
	}

	[Fact]
	public void TheFirstLinesTheGateTimesAreUniquelyNamedAreWithinTheLineCeilingAndHoldTheStressSetOfTheFirstLine()
	{
		ImmutableArray<PythonFirstLineShape> shapes = PythonLexicalBudgetMeasurements.BuildFirstLineShapes();
		ImmutableArray<FirstLineStressShape> stressSet = PythonFirstLineStressSet.Build();

		Assert.Equal(shapes.Length, shapes.Select(shape => shape.Name).Distinct(StringComparer.Ordinal).Count());
		Assert.All(shapes, shape => Assert.True(shape.Line.Length <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS, shape.Name));
		Assert.All(stressSet, stressShape => Assert.Contains(shapes, shape => shape.Name == $"{PythonLexicalBudgetMeasurements.STRESS_SET_SHAPE_NAME_PREFIX}{stressShape.Name}" && shape.Line == stressShape.Line));
	}
}
