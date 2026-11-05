using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE BUILD SYMBOLS THE FILE CAN NEVER KNOW THE VALUE OF, read from a profile's <see cref="ConditionalCompilationRule"/>.
/// Three kinds of word, written as the rule documents them: an exact symbol, a prefix wildcard (every symbol that starts
/// with the word) and a platform word (the word, or the word and an optional tail of digits and underscores, with or
/// without a trailing <c>OR_GREATER</c>). Comparison is ordinal and case-sensitive, as the language's own symbols are.
/// The decision of every symbol is a bounded walk over the declared words and the symbol's own characters.
/// </summary>
public sealed class WellKnownBuildSymbols
{
	private const string _OR_GREATER_TAIL = "OR_GREATER";
	private const char _TAIL_UNDERSCORE = '_';

	private readonly HashSet<string> _exactSymbols;
	private readonly string[] _prefixes;
	private readonly string[] _platformWords;

	private WellKnownBuildSymbols(HashSet<string> exactSymbols, string[] prefixes, string[] platformWords)
	{
		_exactSymbols = exactSymbols;
		_prefixes = prefixes;
		_platformWords = platformWords;
	}

	/// <summary>No well-known symbol: every symbol the file does not define is false.</summary>
	public static WellKnownBuildSymbols None { get; } = new([], [], []);

	/// <summary>The words of one profile's element.</summary>
	public static WellKnownBuildSymbols From(ConditionalCompilationRule rule)
	{
		ArgumentNullException.ThrowIfNull(rule);

		HashSet<string> exactSymbols = new(StringComparer.Ordinal);
		List<string> prefixes = [];
		List<string> platformWords = [];

		foreach (string word in rule.WellKnownSymbolWords)
		{
			if (word.EndsWith(ConditionalCompilationRule.PREFIX_WILDCARD_MARKER))
			{
				prefixes.Add(word[..^1]);
			}
			else if (word.EndsWith(ConditionalCompilationRule.VERSION_TAIL_MARKER))
			{
				platformWords.Add(word[..^1]);
			}
			else
			{
				exactSymbols.Add(word);
			}
		}

		return new WellKnownBuildSymbols(exactSymbols, [.. prefixes], [.. platformWords]);
	}

	/// <summary>Whether the build can define this symbol outside the file.</summary>
	public bool Contains(string symbol)
	{
		ArgumentNullException.ThrowIfNull(symbol);

		return _exactSymbols.Contains(symbol)
			|| _prefixes.Any(prefix => symbol.StartsWith(prefix, StringComparison.Ordinal))
			|| _platformWords.Any(word => IsThePlatformWordWithAnOptionalVersionTail(symbol, word));
	}

	private static bool IsThePlatformWordWithAnOptionalVersionTail(string symbol, string platformWord)
	{
		bool startsWithTheWord = symbol.StartsWith(platformWord, StringComparison.Ordinal);
		ReadOnlySpan<char> tail = startsWithTheWord ? symbol.AsSpan(platformWord.Length) : default;

		if (tail.EndsWith(_OR_GREATER_TAIL, StringComparison.Ordinal))
		{
			tail = tail[..^_OR_GREATER_TAIL.Length];
		}

		return startsWithTheWord && IsOnlyDigitsAndUnderscores(tail);
	}

	private static bool IsOnlyDigitsAndUnderscores(ReadOnlySpan<char> tail)
	{
		bool onlyDigitsAndUnderscores = true;

		foreach (char character in tail)
		{
			onlyDigitsAndUnderscores = onlyDigitsAndUnderscores && (char.IsAsciiDigit(character) || character == _TAIL_UNDERSCORE);
		}

		return onlyDigitsAndUnderscores;
	}
}
