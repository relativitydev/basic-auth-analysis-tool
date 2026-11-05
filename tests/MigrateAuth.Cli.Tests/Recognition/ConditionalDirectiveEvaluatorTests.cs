using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Recognition;

/// <summary>
/// THE THREE-VALUED EVALUATOR OF A CONDITIONAL-COMPILATION CONDITION: every operator against each pair of values, the
/// precedence, parentheses, literals, the unparsable conditions (unknown, never false) and the depth cap. The symbols are
/// invented: <c>ON</c> is defined, <c>OFF</c> is undefined and <c>DEBUG</c> is a well-known symbol, so the three read
/// true, false and unknown.
/// </summary>
public sealed class ConditionalDirectiveEvaluatorTests
{
	private const char _OPENING_PARENTHESIS = '(';
	private const char _CLOSING_PARENTHESIS = ')';
	private const char _NEGATION = '!';
	private const int _CAPPED_PARENTHESES_COUNT = 16;
	private const int _OVER_CAP_PARENTHESES_COUNT = 17;
	private const int _NEGATIONS_COUNT = 16;
	private const string _TRUE_SYMBOL = "ON";
	private const string _FALSE_SYMBOL = "OFF";
	private const string _UNKNOWN_SYMBOL = "DEBUG";
	private const string _AND = "&&";
	private const string _OR = "||";
	private const string _EQUAL = "==";
	private const string _NOT_EQUAL = "!=";

	private static readonly string[] _OPERAND_SPELLINGS = [_FALSE_SYMBOL, _TRUE_SYMBOL, _UNKNOWN_SYMBOL];

	private static readonly ConditionValue[] _OPERAND_VALUES = [ConditionValue.False, ConditionValue.True, ConditionValue.Unknown];

	/// <summary>Rows are the left value and columns the right value, each in the order false, true, unknown.</summary>
	private static readonly Dictionary<string, ConditionValue[][]> _TRUTH_TABLES = new()
	{
		[_AND] =
		[
			[ConditionValue.False, ConditionValue.False, ConditionValue.False],
			[ConditionValue.False, ConditionValue.True, ConditionValue.Unknown],
			[ConditionValue.False, ConditionValue.Unknown, ConditionValue.Unknown]
		],
		[_OR] =
		[
			[ConditionValue.False, ConditionValue.True, ConditionValue.Unknown],
			[ConditionValue.True, ConditionValue.True, ConditionValue.True],
			[ConditionValue.Unknown, ConditionValue.True, ConditionValue.Unknown]
		],
		[_EQUAL] =
		[
			[ConditionValue.True, ConditionValue.False, ConditionValue.Unknown],
			[ConditionValue.False, ConditionValue.True, ConditionValue.Unknown],
			[ConditionValue.Unknown, ConditionValue.Unknown, ConditionValue.Unknown]
		],
		[_NOT_EQUAL] =
		[
			[ConditionValue.False, ConditionValue.True, ConditionValue.Unknown],
			[ConditionValue.True, ConditionValue.False, ConditionValue.Unknown],
			[ConditionValue.Unknown, ConditionValue.Unknown, ConditionValue.Unknown]
		]
	};

	public static TheoryData<string, string, string, ConditionValue> EveryBinaryOperatorAgainstEachPairOfValues()
	{
		TheoryData<string, string, string, ConditionValue> rows = [];

		foreach ((string binaryOperator, ConditionValue[][] table) in _TRUTH_TABLES)
		{
			for (int left = 0; left < _OPERAND_VALUES.Length; left++)
			{
				for (int right = 0; right < _OPERAND_VALUES.Length; right++)
				{
					rows.Add(binaryOperator, _OPERAND_SPELLINGS[left], _OPERAND_SPELLINGS[right], table[left][right]);
				}
			}
		}

		return rows;
	}

	[Theory]
	[MemberData(nameof(EveryBinaryOperatorAgainstEachPairOfValues))]
	public void EveryBinaryOperatorReadsItsKleeneTableForEachPairOfValues(string binaryOperator, string left, string right, ConditionValue expected)
	{
		Assert.Equal(expected, Evaluate($"{left} {binaryOperator} {right}"));
	}

	[Theory]
	[InlineData(_FALSE_SYMBOL, ConditionValue.True)]
	[InlineData(_TRUE_SYMBOL, ConditionValue.False)]
	[InlineData(_UNKNOWN_SYMBOL, ConditionValue.Unknown)]
	public void TheNegationSwapsTrueAndFalseAndKeepsUnknown(string operand, ConditionValue expected)
	{
		Assert.Equal(expected, Evaluate($"!{operand}"));
	}

	[Theory]
	[InlineData("true", ConditionValue.True)]
	[InlineData("false", ConditionValue.False)]
	[InlineData("!true", ConditionValue.False)]
	[InlineData("true == true", ConditionValue.True)]
	[InlineData("false != true", ConditionValue.True)]
	[InlineData("True", ConditionValue.False)]
	[InlineData("FALSE", ConditionValue.False)]
	public void TheLiteralsAreTheLowerCaseWordsAndEverySpellingOfTheWordsIsASymbol(string condition, ConditionValue expected)
	{
		Assert.Equal(expected, Evaluate(condition));
	}

	[Theory]
	[InlineData("OFF || ON && OFF", ConditionValue.False)]
	[InlineData("ON || OFF && OFF", ConditionValue.True)]
	[InlineData("(ON || OFF) && OFF", ConditionValue.False)]
	[InlineData("!OFF == ON", ConditionValue.True)]
	[InlineData("!ON == OFF", ConditionValue.True)]
	[InlineData("ON == ON == ON", ConditionValue.True)]
	[InlineData("ON != OFF == ON", ConditionValue.True)]
	[InlineData("ON != OFF != ON", ConditionValue.False)]
	[InlineData("OFF && ON == OFF", ConditionValue.False)]
	[InlineData("ON && OFF == OFF", ConditionValue.True)]
	[InlineData("!(OFF || DEBUG)", ConditionValue.Unknown)]
	[InlineData("!(OFF && DEBUG)", ConditionValue.True)]
	[InlineData("  ON  &&  ( OFF || ON )  ", ConditionValue.True)]
	[InlineData("ON&&!OFF", ConditionValue.True)]
	[InlineData("OFF==false", ConditionValue.True)]
	[InlineData("DEBUG!=true", ConditionValue.Unknown)]
	public void ThePrecedenceIsNotThenEqualityThenAndThenOrAndParenthesesGroup(string condition, ConditionValue expected)
	{
		Assert.Equal(expected, Evaluate(condition));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("OFF &&")]
	[InlineData("&& ON")]
	[InlineData("|| ON")]
	[InlineData("ON ||")]
	[InlineData("(ON")]
	[InlineData("ON)")]
	[InlineData("()")]
	[InlineData("(")]
	[InlineData("ON ON")]
	[InlineData("ON & OFF")]
	[InlineData("ON | OFF")]
	[InlineData("ON = OFF")]
	[InlineData("ON ==")]
	[InlineData("== ON")]
	[InlineData("!")]
	[InlineData("!=")]
	[InlineData("1")]
	[InlineData("ON && 1")]
	[InlineData("ON ? OFF : ON")]
	[InlineData("OFF && (ON")]
	public void AConditionThatDoesNotParseIsUnknownAndNeverFalse(string condition)
	{
		Assert.Equal(ConditionValue.Unknown, Evaluate(condition));
	}

	[Fact]
	public void AnUnparsableTailMakesAFalseHeadUnknownRatherThanFalse()
	{
		Assert.Equal(ConditionValue.Unknown, Evaluate("OFF && ("));
		Assert.Equal(ConditionValue.Unknown, Evaluate("OFF && ON ON"));
	}

	[Fact]
	public void ThirtyTwoNestedParenthesesParseAndThirtyThreeDoNot()
	{
		Assert.Equal(ConditionValue.True, Evaluate(Nested("(", ")", ConditionalDirectiveEvaluator.MAX_EXPRESSION_DEPTH)));
		Assert.Equal(ConditionValue.Unknown, Evaluate(Nested("(", ")", ConditionalDirectiveEvaluator.MAX_EXPRESSION_DEPTH + 1)));
	}

	[Fact]
	public void ThirtyTwoChainedNegationsParseAndThirtyThreeDoNot()
	{
		Assert.Equal(ConditionValue.True, Evaluate(Nested("!", string.Empty, ConditionalDirectiveEvaluator.MAX_EXPRESSION_DEPTH)));
		Assert.Equal(ConditionValue.Unknown, Evaluate(Nested("!", string.Empty, ConditionalDirectiveEvaluator.MAX_EXPRESSION_DEPTH + 1)));
	}

	[Fact]
	public void TheDepthCountsParenthesesAndNegationsTogether()
	{
		string withinTheCap = $"{new string(_OPENING_PARENTHESIS, _CAPPED_PARENTHESES_COUNT)}{new string(_NEGATION, _NEGATIONS_COUNT)}{_TRUE_SYMBOL}{new string(_CLOSING_PARENTHESIS, _CAPPED_PARENTHESES_COUNT)}";
		string overTheCap = $"{new string(_OPENING_PARENTHESIS, _OVER_CAP_PARENTHESES_COUNT)}{new string(_NEGATION, _NEGATIONS_COUNT)}{_TRUE_SYMBOL}{new string(_CLOSING_PARENTHESIS, _OVER_CAP_PARENTHESES_COUNT)}";

		Assert.Equal(ConditionValue.True, Evaluate(withinTheCap));
		Assert.Equal(ConditionValue.Unknown, Evaluate(overTheCap));
	}

	[Fact]
	public void SiblingGroupsDoNotAccumulateDepth()
	{
		string siblings = string.Join(" && ", Enumerable.Repeat("(ON)", ConditionalDirectiveEvaluator.MAX_EXPRESSION_DEPTH * 2));

		Assert.Equal(ConditionValue.True, Evaluate(siblings));
	}

	[Theory]
	[InlineData("SYMBOL", "SYMBOL")]
	[InlineData("  SYMBOL  ", "SYMBOL")]
	[InlineData("_Symbol_2", "_Symbol_2")]
	public void ASymbolNameIsOneIdentifierAndNothingElseButBlanks(string operand, string expected)
	{
		Assert.True(ConditionalDirectiveEvaluator.TryReadSymbolName(operand, out string symbol));
		Assert.Equal(expected, symbol);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("TWO WORDS")]
	[InlineData("1SYMBOL")]
	[InlineData("SYM-BOL")]
	[InlineData("SYMBOL()")]
	public void AnythingElseNamesNoSymbol(string operand)
	{
		Assert.False(ConditionalDirectiveEvaluator.TryReadSymbolName(operand, out string symbol));
		Assert.Empty(symbol);
	}

	[Fact]
	public void SymbolsAreCaseSensitive()
	{
		ConditionalSymbolTable symbols = TableOfTheInventedSymbols();

		Assert.Equal(ConditionValue.True, ConditionalDirectiveEvaluator.Evaluate(_TRUE_SYMBOL, symbols));
		Assert.Equal(ConditionValue.False, ConditionalDirectiveEvaluator.Evaluate("On", symbols));
		Assert.Equal(ConditionValue.False, ConditionalDirectiveEvaluator.Evaluate("debug", symbols));
	}

	[Fact]
	public void AnUnknownSymbolStaysUnknownUntilItIsUndefinedAndThenReadsFalse()
	{
		ConditionalSymbolTable symbols = TableOfTheInventedSymbols();

		Assert.Equal(ConditionValue.Unknown, symbols.ValueOf(_UNKNOWN_SYMBOL));

		symbols.Undefine(_UNKNOWN_SYMBOL);

		Assert.Equal(ConditionValue.False, symbols.ValueOf(_UNKNOWN_SYMBOL));

		symbols.Define(_UNKNOWN_SYMBOL);

		Assert.Equal(ConditionValue.True, symbols.ValueOf(_UNKNOWN_SYMBOL));

		symbols.MarkUnknown(_UNKNOWN_SYMBOL);

		Assert.Equal(ConditionValue.Unknown, symbols.ValueOf(_UNKNOWN_SYMBOL));
	}

	private static string Nested(string opening, string closing, int depth)
	{
		return $"{string.Concat(Enumerable.Repeat(opening, depth))}{_TRUE_SYMBOL}{string.Concat(Enumerable.Repeat(closing, depth))}";
	}

	private static ConditionValue Evaluate(string condition)
	{
		return ConditionalDirectiveEvaluator.Evaluate(condition, TableOfTheInventedSymbols());
	}

	private static ConditionalSymbolTable TableOfTheInventedSymbols()
	{
		ConditionalCompilationRule rule = new()
		{
			DirectiveOpening = "#",
			WellKnownSymbolWords = [_UNKNOWN_SYMBOL]
		};
		ConditionalSymbolTable symbols = new(WellKnownBuildSymbols.From(rule));

		symbols.Define(_TRUE_SYMBOL);
		symbols.Undefine(_FALSE_SYMBOL);

		return symbols;
	}
}
