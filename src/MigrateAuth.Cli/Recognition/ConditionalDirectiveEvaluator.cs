namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE THREE-VALUED EVALUATOR OF A CONDITIONAL-COMPILATION CONDITION. It reads <c>!</c>, <c>&amp;&amp;</c>,
/// <c>||</c>, <c>==</c>, <c>!=</c>, parentheses, <c>true</c>, <c>false</c> and symbols, in the language's own
/// precedence (<c>!</c>, then <c>==</c> and <c>!=</c>, then <c>&amp;&amp;</c>, then <c>||</c>), by a hand-written
/// recursive descent whose nesting depth (parentheses and chained <c>!</c>) is capped at
/// <see cref="MAX_EXPRESSION_DEPTH"/>. Kleene's logic: <c>!</c> swaps true and false and keeps unknown; <c>&amp;&amp;</c>
/// is false if either side is false, true if both are true and unknown otherwise; <c>||</c> is true if either side is true,
/// false if both are false and unknown otherwise; <c>==</c> and <c>!=</c> compare only two known values.
///
/// A CONDITION THAT DOES NOT PARSE IS UNKNOWN, NEVER FALSE: an empty condition, a character that belongs to no token,
/// unbalanced parentheses, a missing operand, text after the expression, a depth over the cap. Unknown deactivates
/// nothing, which is the whole of the safety rule "an unparsable directive deactivates nothing".
///
/// THE EVALUATION IS LINEAR IN THE CONDITION'S LENGTH: each character is consumed once and every production either
/// consumes at least one character or fails, with no backtracking.
/// </summary>
public static class ConditionalDirectiveEvaluator
{
	/// <summary>The deepest nesting of parentheses and chained negations a condition may have; a deeper one is unparsable.</summary>
	public const int MAX_EXPRESSION_DEPTH = 32;

	private const char _IDENTIFIER_UNDERSCORE = '_';

	/// <summary>The value of one condition, with every symbol read from <paramref name="symbols"/>.</summary>
	public static ConditionValue Evaluate(string condition, ConditionalSymbolTable symbols)
	{
		ArgumentNullException.ThrowIfNull(condition);
		ArgumentNullException.ThrowIfNull(symbols);

		ConditionParser parser = new(condition, symbols);
		ConditionValue value = parser.ParseWholeCondition();

		return parser.Failed ? ConditionValue.Unknown : value;
	}

	/// <summary>
	/// The symbol a <c>define</c> or an <c>undef</c> names: one identifier and nothing else but blanks. False when the
	/// operand is anything else, in which case the directive defines nothing.
	/// </summary>
	public static bool TryReadSymbolName(string operand, out string symbol)
	{
		ArgumentNullException.ThrowIfNull(operand);

		string trimmed = operand.Trim();
		bool isOneIdentifier = trimmed.Length > 0
			&& IsIdentifierStart(trimmed[0])
			&& trimmed.All(IsIdentifierCharacter);

		symbol = isOneIdentifier ? trimmed : string.Empty;

		return isOneIdentifier;
	}

	internal static bool IsIdentifierStart(char character)
	{
		return char.IsLetter(character) || character == _IDENTIFIER_UNDERSCORE;
	}

	internal static bool IsIdentifierCharacter(char character)
	{
		return char.IsLetterOrDigit(character) || character == _IDENTIFIER_UNDERSCORE;
	}

	private static ConditionValue Not(ConditionValue value)
	{
		return value switch
		{
			ConditionValue.True => ConditionValue.False,
			ConditionValue.False => ConditionValue.True,
			_ => ConditionValue.Unknown
		};
	}

	private static ConditionValue And(ConditionValue left, ConditionValue right)
	{
		ConditionValue result = ConditionValue.Unknown;

		if (left == ConditionValue.False || right == ConditionValue.False)
		{
			result = ConditionValue.False;
		}
		else if (left == ConditionValue.True && right == ConditionValue.True)
		{
			result = ConditionValue.True;
		}

		return result;
	}

	private static ConditionValue Or(ConditionValue left, ConditionValue right)
	{
		ConditionValue result = ConditionValue.Unknown;

		if (left == ConditionValue.True || right == ConditionValue.True)
		{
			result = ConditionValue.True;
		}
		else if (left == ConditionValue.False && right == ConditionValue.False)
		{
			result = ConditionValue.False;
		}

		return result;
	}

	private static ConditionValue Equal(ConditionValue left, ConditionValue right)
	{
		bool bothKnown = left != ConditionValue.Unknown && right != ConditionValue.Unknown;
		ConditionValue result = ConditionValue.Unknown;

		if (bothKnown)
		{
			result = left == right ? ConditionValue.True : ConditionValue.False;
		}

		return result;
	}

	/// <summary>One parse of one condition. The position only moves forward and the depth is counted on the way down.</summary>
	private sealed class ConditionParser(string condition, ConditionalSymbolTable symbols)
	{
		private const string _OR_OPERATOR = "||";
		private const string _AND_OPERATOR = "&&";
		private const string _EQUAL_OPERATOR = "==";
		private const string _NOT_EQUAL_OPERATOR = "!=";
		private const string _TRUE_WORD = "true";
		private const string _FALSE_WORD = "false";
		private const char _NOT_OPERATOR = '!';
		private const char _OPENING_PARENTHESIS = '(';
		private const char _CLOSING_PARENTHESIS = ')';

		private int _position;
		private int _depth;

		internal bool Failed { get; private set; }

		internal ConditionValue ParseWholeCondition()
		{
			ConditionValue value = ParseOr();

			SkipBlanks();

			if (_position != condition.Length)
			{
				Failed = true;
			}

			return value;
		}

		private ConditionValue ParseOr()
		{
			ConditionValue value = ParseAnd();

			while (!Failed && TryConsume(_OR_OPERATOR))
			{
				value = Or(value, ParseAnd());
			}

			return value;
		}

		private ConditionValue ParseAnd()
		{
			ConditionValue value = ParseEquality();

			while (!Failed && TryConsume(_AND_OPERATOR))
			{
				value = And(value, ParseEquality());
			}

			return value;
		}

		private ConditionValue ParseEquality()
		{
			ConditionValue value = ParseUnary();
			bool anotherComparison = !Failed;

			while (anotherComparison)
			{
				if (TryConsume(_EQUAL_OPERATOR))
				{
					value = Equal(value, ParseUnary());
				}
				else if (TryConsume(_NOT_EQUAL_OPERATOR))
				{
					value = Not(Equal(value, ParseUnary()));
				}
				else
				{
					anotherComparison = false;
				}

				anotherComparison = anotherComparison && !Failed;
			}

			return value;
		}

		private ConditionValue ParseUnary()
		{
			SkipBlanks();

			ConditionValue value;

			if (_position < condition.Length && condition[_position] == _NOT_OPERATOR)
			{
				_position++;
				value = Not(ParseNested(ParseUnary, nestsAParenthesisLevel: false));
			}
			else
			{
				value = ParsePrimary();
			}

			return value;
		}

		private ConditionValue ParsePrimary()
		{
			SkipBlanks();

			ConditionValue value = ConditionValue.Unknown;

			if (_position >= condition.Length)
			{
				Failed = true;
			}
			else if (condition[_position] == _OPENING_PARENTHESIS)
			{
				_position++;
				value = ParseNested(ParseOr, nestsAParenthesisLevel: true);
			}
			else if (IsIdentifierStart(condition[_position]))
			{
				value = ReadWord();
			}
			else
			{
				Failed = true;
			}

			return value;
		}

		/// <summary>One level deeper: the depth is checked before the descent, so the cap is never exceeded; a parenthesis level must close.</summary>
		private ConditionValue ParseNested(Func<ConditionValue> parseInside, bool nestsAParenthesisLevel)
		{
			_depth++;

			ConditionValue value = ConditionValue.Unknown;

			if (_depth > MAX_EXPRESSION_DEPTH)
			{
				Failed = true;
			}
			else
			{
				value = parseInside();

				if (nestsAParenthesisLevel && !Failed)
				{
					SkipBlanks();
					Failed = _position >= condition.Length || condition[_position] != _CLOSING_PARENTHESIS;
					_position++;
				}
			}

			_depth--;

			return value;
		}

		private ConditionValue ReadWord()
		{
			int wordStart = _position;

			while (_position < condition.Length && IsIdentifierCharacter(condition[_position]))
			{
				_position++;
			}

			string word = condition[wordStart.._position];

			return word switch
			{
				_TRUE_WORD => ConditionValue.True,
				_FALSE_WORD => ConditionValue.False,
				_ => symbols.ValueOf(word)
			};
		}

		private bool TryConsume(string token)
		{
			SkipBlanks();

			bool consumed = string.CompareOrdinal(condition, _position, token, 0, token.Length) == 0;

			if (consumed)
			{
				_position += token.Length;
			}

			return consumed;
		}

		private void SkipBlanks()
		{
			while (_position < condition.Length && char.IsWhiteSpace(condition[_position]))
			{
				_position++;
			}
		}
	}
}
