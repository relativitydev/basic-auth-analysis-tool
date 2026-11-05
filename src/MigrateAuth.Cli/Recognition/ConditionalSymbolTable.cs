namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE VALUE OF EVERY CONDITIONAL-COMPILATION SYMBOL AT ONE POINT OF A FILE. A symbol the file has defined is true, one it
/// has undefined is false, one it defined or undefined inside a branch that may not be taken is unknown; every other
/// symbol is unknown if the build can define it (<see cref="WellKnownBuildSymbols"/>) and false otherwise. Symbols are
/// case-sensitive.
/// </summary>
public sealed class ConditionalSymbolTable
{
	private readonly Dictionary<string, ConditionValue> _valueByDeclaredSymbol = new(StringComparer.Ordinal);
	private readonly WellKnownBuildSymbols _wellKnownSymbols;

	/// <param name="wellKnownSymbols">The symbols the file can never know the value of, until it undefines them.</param>
	public ConditionalSymbolTable(WellKnownBuildSymbols wellKnownSymbols)
	{
		ArgumentNullException.ThrowIfNull(wellKnownSymbols);

		_wellKnownSymbols = wellKnownSymbols;
	}

	/// <summary>The value of one symbol now.</summary>
	public ConditionValue ValueOf(string symbol)
	{
		ConditionValue value;

		if (!_valueByDeclaredSymbol.TryGetValue(symbol, out value))
		{
			value = _wellKnownSymbols.Contains(symbol) ? ConditionValue.Unknown : ConditionValue.False;
		}

		return value;
	}

	/// <summary>A define in live code.</summary>
	public void Define(string symbol)
	{
		_valueByDeclaredSymbol[symbol] = ConditionValue.True;
	}

	/// <summary>An undefine in live code.</summary>
	public void Undefine(string symbol)
	{
		_valueByDeclaredSymbol[symbol] = ConditionValue.False;
	}

	/// <summary>A define or an undefine inside a branch that may not be taken.</summary>
	public void MarkUnknown(string symbol)
	{
		_valueByDeclaredSymbol[symbol] = ConditionValue.Unknown;
	}
}
