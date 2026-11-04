namespace MigrateAuth.Cli.Findings;

/// <summary>
/// A LINE AND A COLUMN, BOTH ONE-BASED, AS A READER OF THE REPORT WOULD COUNT THEM.
///
/// The column is published as a field rather than kept inside the sort key, for two reasons:
/// two matches of the same class on the same line otherwise produce findings
/// indistinguishable in the artifact except by identifier, and the ordering that GENERATES the
/// identifiers would not be reconstructible from the artifact.
///
/// The pair is a type rather than two loose integers so that the two can never be handed over in
/// the wrong order - which a pair of same-typed parameters invites and which no compiler catches.
/// </summary>
public readonly record struct TextPosition
{
	internal TextPosition(int line, int column)
	{
		Line = line;
		Column = column;
	}

	/// <summary>The line, counted from one. Line feeds before the offset are what advance it.</summary>
	public int Line { get; }

	/// <summary>The column, counted from one, in UTF-16 code units from the start of the line.</summary>
	public int Column { get; }
}

/// <summary>
/// THE OFFSET-TO-POSITION CONVERSION, PURE AND STATELESS.
///
/// IT COUNTS LINE FEEDS AND NOTHING ELSE, and that is only sound because the text it is handed is
/// the text <see cref="Reading.TextNormalization"/> produced. That component declares what ends a
/// line - a line feed does, a carriage return does whether or not a line feed follows it, and the
/// Unicode line and paragraph separators DO NOT - and rewrites every one of them to a line feed
/// before anything content-derived is computed. So the decision about what a line is was taken
/// there, once; repeating it here would be a second copy of a declared choice, and the two copies
/// would disagree the day one of them is revised.
///
/// POSITIONS ARE COUNTED IN UTF-16 CODE UNITS, because a file
/// holding characters outside the basic multilingual plane would measure, and later sort,
/// differently under any other unit. The anchor offsets this converts are in the same unit.
///
/// THE COST IS LINEAR IN THE OFFSET. A carried scan position would make a file's conversions
/// linear in the file instead, because the constructs of one file arrive in ascending anchor
/// order - but it would also make this stateful, and a stateful converter is one whose answer
/// depends on what it was asked before. The file size ceiling bounds a single file, and findings
/// are sparse within one, so this stays a function whose answer depends on its arguments alone.
/// </summary>
public static class TextPositions
{
	/// <summary>The line an offset before the first line feed sits on.</summary>
	public const int FIRST_LINE = 1;

	/// <summary>The column an offset at the start of a line sits at.</summary>
	public const int FIRST_COLUMN = 1;

	private const char _LINE_FEED = '\n';
	private const int _NO_PRECEDING_LINE_FEED = -1;

	/// <summary>
	/// The one-based line and column of an absolute offset in normalized text.
	///
	/// AN OFFSET LANDING EXACTLY ON A LINE FEED BELONGS TO THE LINE THAT FEED ENDS, not to the one
	/// it begins, because the line feed at that position has not been counted yet. That is the
	/// reading a human gives it too: the end of line four is on line four.
	///
	/// The offset may equal the text's length, which is the position one past the last character.
	/// Nothing in this product anchors a construct there, but a converter that threw on it would
	/// be refusing a legitimate position rather than catching a defect.
	/// </summary>
	public static TextPosition LineAndColumnOf(string normalizedText, int offset)
	{
		ArgumentNullException.ThrowIfNull(normalizedText);
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, normalizedText.Length);

		int line = FIRST_LINE;
		int precedingLineFeedPosition = _NO_PRECEDING_LINE_FEED;

		for (int position = 0; position < offset; position++)
		{
			if (normalizedText[position] == _LINE_FEED)
			{
				line++;
				precedingLineFeedPosition = position;
			}
		}

		return new TextPosition(line, offset - precedingLineFeedPosition);
	}
}
