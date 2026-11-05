namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// WHETHER ONE LOGICAL LINE IS A PLAIN COPY OF ONE VARIABLE INTO ANOTHER, by plain string operations:
/// <c>NAME2=$NAME</c>, <c>NAME2="$NAME"</c>, <c>NAME2=${NAME}</c>, with an optional run of declaration words
/// (<c>export</c>, <c>local</c>, <c>declare</c>, <c>typeset</c>, <c>readonly</c>) and their option words before
/// the assignment. The value must be EXACTLY the reference: a value that adds anything to it is not a copy, and
/// the variable it builds is not followed. The word after the value may be a blank, a separator or a comment.
/// </summary>
internal static class SimpleCopyAssignment
{
	private const char _ASSIGNMENT = '=';
	private const char _DOLLAR = '$';
	private const char _OPENING_BRACE = '{';
	private const char _CLOSING_BRACE = '}';
	private const char _DOUBLE_QUOTE = '"';
	private const char _OPTION_PREFIX = '-';
	private const string _VALUE_TERMINATORS = ";&|#)";

	private static readonly string[] _declarationWords = ["export", "local", "declare", "typeset", "readonly"];

	/// <summary>The name <paramref name="line"/> copies <paramref name="source"/> into, where it is a plain copy.</summary>
	internal static bool TryReadTarget(string line, string source, out string target)
	{
		int position = SkipBlanks(line, 0);

		position = SkipDeclarationWords(line, position);

		int nameStart = position;

		while (position < line.Length && VariableReferenceIndex.IsIdentifierCharacter(line[position]))
		{
			position++;
		}

		string candidate = line[nameStart..position];
		bool isCopy = candidate.Length > 0
			&& !string.Equals(candidate, source, StringComparison.Ordinal)
			&& position < line.Length
			&& line[position] == _ASSIGNMENT
			&& ValueIsExactlyAReference(line, position + 1, source);

		target = isCopy ? candidate : string.Empty;

		return isCopy;
	}

	private static bool ValueIsExactlyAReference(string line, int valueStart, string source)
	{
		int position = valueStart;
		bool quoted = position < line.Length && line[position] == _DOUBLE_QUOTE;

		if (quoted)
		{
			position++;
		}

		int referenceEnd = EndOfReference(line, position, source);
		bool isReference = referenceEnd >= 0;

		if (isReference && quoted)
		{
			isReference = referenceEnd < line.Length && line[referenceEnd] == _DOUBLE_QUOTE;
			referenceEnd++;
		}

		return isReference && ValueEndsAt(line, referenceEnd);
	}

	/// <summary>Where a reference to <paramref name="source"/> that starts at <paramref name="position"/> ends, or -1.</summary>
	private static int EndOfReference(string line, int position, string source)
	{
		int end = -1;

		if (position < line.Length && line[position] == _DOLLAR)
		{
			bool braced = position + 1 < line.Length && line[position + 1] == _OPENING_BRACE;
			int nameStart = position + (braced ? 2 : 1);
			int nameEnd = nameStart + source.Length;
			bool nameMatches = nameEnd <= line.Length
				&& string.CompareOrdinal(line, nameStart, source, 0, source.Length) == 0;

			if (nameMatches && braced)
			{
				end = nameEnd < line.Length && line[nameEnd] == _CLOSING_BRACE ? nameEnd + 1 : -1;
			}
			else if (nameMatches)
			{
				end = nameEnd >= line.Length || !VariableReferenceIndex.IsIdentifierCharacter(line[nameEnd]) ? nameEnd : -1;
			}
		}

		return end;
	}

	private static bool ValueEndsAt(string line, int position)
	{
		return position >= line.Length
			|| char.IsWhiteSpace(line[position])
			|| _VALUE_TERMINATORS.Contains(line[position]);
	}

	private static int SkipDeclarationWords(string line, int start)
	{
		int position = start;
		bool isDeclarationWord = true;

		while (isDeclarationWord)
		{
			int wordEnd = EndOfWord(line, position);
			string word = line[position..wordEnd];

			isDeclarationWord = _declarationWords.Contains(word, StringComparer.Ordinal);

			if (isDeclarationWord)
			{
				position = SkipOptionWords(line, SkipBlanks(line, wordEnd));
			}
		}

		return position;
	}

	private static int SkipOptionWords(string line, int start)
	{
		int position = start;

		while (position < line.Length && line[position] == _OPTION_PREFIX)
		{
			position = SkipBlanks(line, EndOfWord(line, position));
		}

		return position;
	}

	private static int EndOfWord(string line, int start)
	{
		int position = start;

		while (position < line.Length && !char.IsWhiteSpace(line[position]))
		{
			position++;
		}

		return position;
	}

	private static int SkipBlanks(string line, int start)
	{
		int position = start;

		while (position < line.Length && char.IsWhiteSpace(line[position]))
		{
			position++;
		}

		return position;
	}
}
