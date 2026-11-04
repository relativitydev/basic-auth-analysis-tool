using System.Text;

namespace MigrateAuth.Cli.Reading;

/// <summary>What a pass over the normalized text established about its shape.</summary>
public readonly record struct TextMeasurement
{
	internal TextMeasurement(int lineCount, int longestLineLength)
	{
		LineCount = lineCount;
		LongestLineLength = longestLineLength;
	}

	/// <summary>Lines, counted after normalization. An empty file has none.</summary>
	public int LineCount { get; }

	/// <summary>The longest line, in UTF-16 code units.</summary>
	public int LongestLineLength { get; }
}

/// <summary>
/// LINE ENDINGS ARE NORMALIZED BEFORE ANY CONTENT-DERIVED CALCULATION, AND THIS IS MANDATORY
/// RATHER THAN TIDY.
///
/// The version control client converts line endings by default on one of the three platforms, so
/// THE SAME COMMIT HAS DIFFERENT BYTES on different platforms. Every number derived from the raw
/// bytes - the line count, a line number, a content fingerprint - then differs across platforms
/// for a repository that is identical, and the output cannot be deterministic across
/// platforms. Normalizing first is what makes the calculation depend on the file rather than
/// on the checkout.
///
/// WHAT COUNTS AS A LINE ENDING IS A DECLARED CHOICE, because it decides the line number every
/// finding will carry. A line feed ends a line; a carriage return ends one whether or not a line
/// feed follows it, which is what makes the old single-carriage-return convention behave; and
/// the Unicode line and paragraph separators DO NOT end a line.
/// </summary>
public static class TextNormalization
{
	private const char _CARRIAGE_RETURN = '\r';
	private const char _LINE_FEED = '\n';

	/// <summary>
	/// Every line ending becomes a line feed. Text that already has none is returned unchanged
	/// rather than rebuilt, which is the common case in a walk of half a million files and costs
	/// nothing to check.
	/// </summary>
	public static string Normalize(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		return text.Contains(_CARRIAGE_RETURN, StringComparison.Ordinal) ? Rewrite(text) : text;
	}

	/// <summary>
	/// Counts lines and finds the longest, in one pass over normalized text, and refuses the file
	/// when a line exceeds the ceiling.
	///
	/// THE WHOLE FILE IS REFUSED, NOT THE LINE. The scanner that runs later is a state machine
	/// over the file: a line dropped from the middle of it leaves that machine's state undefined,
	/// and an unterminated literal opened inside the dropped line would corrupt the zone mask for
	/// everything after it. File granularity is the only sound option, which applies equally when
	/// the ceiling is breached by one line near the end of an otherwise ordinary file.
	///
	/// POSITIONS ARE COUNTED IN UTF-16 CODE UNITS, declared because a file holding characters
	/// outside the basic multilingual plane would measure - and later sort - differently under
	/// any other unit.
	/// </summary>
	public static bool TryMeasure(string normalizedText, out TextMeasurement measurement)
	{
		ArgumentNullException.ThrowIfNull(normalizedText);

		int lineCount = 0;
		int longestLineLength = 0;
		int currentLineLength = 0;

		foreach (char character in normalizedText)
		{
			if (character == _LINE_FEED)
			{
				lineCount++;
				longestLineLength = Math.Max(longestLineLength, currentLineLength);
				currentLineLength = 0;
			}
			else
			{
				currentLineLength++;
			}
		}

		longestLineLength = Math.Max(longestLineLength, currentLineLength);
		lineCount += currentLineLength > 0 ? 1 : 0;
		measurement = new TextMeasurement(lineCount, longestLineLength);

		return longestLineLength <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS;
	}

	private static string Rewrite(string text)
	{
		StringBuilder normalizedText = new(text.Length);

		for (int position = 0; position < text.Length; position++)
		{
			char character = text[position];

			if (character == _CARRIAGE_RETURN)
			{
				normalizedText.Append(_LINE_FEED);
				position += IsFollowedByLineFeed(text, position) ? 1 : 0;
			}
			else
			{
				normalizedText.Append(character);
			}
		}

		return normalizedText.ToString();
	}

	private static bool IsFollowedByLineFeed(string text, int position)
	{
		return position + 1 < text.Length && text[position + 1] == _LINE_FEED;
	}
}
