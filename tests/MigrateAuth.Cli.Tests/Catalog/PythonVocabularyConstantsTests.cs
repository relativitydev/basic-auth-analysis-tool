using System.Globalization;
using System.Reflection;

using MigrateAuth.Cli.Catalog.Languages;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE QUANTIFIER TEXTS OF THE `Python` VOCABULARY EQUAL THEIR INTEGER BOUNDS. A source-generated expression needs a constant pattern, and an integer constant cannot be joined into one, so each bound is written
/// once as text in the vocabulary of the pattern classes. The two cannot drift: the text is read here, by reflection, because the vocabulary is internal to the shipped assembly, and compared with the integer.
/// </summary>
public sealed class PythonVocabularyConstantsTests
{
	private const string _TYPE_NAME = "MigrateAuth.Cli.Catalog.Languages.PythonPatternExpressions";
	private const BindingFlags _NON_PUBLIC_STATIC = BindingFlags.NonPublic | BindingFlags.Static;

	[Theory]
	[InlineData("_STRING_BODY_QUANTIFIER", "_STRING_BODY_CHARACTERS")]
	[InlineData("_NESTED_GROUP_QUANTIFIER", "_NESTED_GROUP_UNITS")]
	[InlineData("_WINDOW_QUANTIFIER", "_ARGUMENT_WINDOW_UNITS")]
	public void EachQuantifierTextIsTheBoundItsIntegerConstantDeclares(string quantifierName, string boundName)
	{
		int bound = Assert.IsType<int>(ConstantOf(boundName));
		string quantifier = Assert.IsType<string>(ConstantOf(quantifierName));

		Assert.Equal($"{{0,{bound.ToString(CultureInfo.InvariantCulture)}}}", quantifier);
	}

	[Fact]
	public void TheWindowBoundsAreTheCatalogsNumbers()
	{
		Assert.Equal(1_000, ConstantOf("_ARGUMENT_WINDOW_UNITS"));
		Assert.Equal(120, ConstantOf("_NESTED_GROUP_UNITS"));
		Assert.Equal(4, ConstantOf("_NESTING_DEPTH"));
		Assert.Equal(300, ConstantOf("_STRING_BODY_CHARACTERS"));
	}

	private static object? ConstantOf(string name)
	{
		Type vocabulary = typeof(EmbeddedLanguageAnalyzers).Assembly.GetType(_TYPE_NAME, throwOnError: true)!;
		FieldInfo field = vocabulary.GetField(name, _NON_PUBLIC_STATIC) ?? throw new InvalidOperationException($"The vocabulary declares no constant named {name}.");

		return field.GetRawConstantValue();
	}
}
