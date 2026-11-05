using System.Text;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// LOGICAL LINES OF A FILE'S NORMALIZED TEXT: physical lines joined across a backslash-newline, by plain string
/// operations. A line feed that is preceded by an ODD number of backslashes is a continuation (an even number
/// is an escaped backslash and ends the line). Offsets are always offsets into the file's own text, so the zone
/// mask can still be asked about them; the joined text of a line is a separate string, built only to be handed
/// to an expression, and is capped.
/// </summary>
internal static class LogicalLines
{
	private const char _LINE_FEED = '\n';
	private const char _CONTINUATION = '\\';

	/// <summary>The start of the logical line that contains <paramref name="position"/>.</summary>
	internal static int StartOf(string text, int position)
	{
		int start = PhysicalLineStart(text, position);

		while (start > 0 && IsContinuation(text, start - 1))
		{
			start = PhysicalLineStart(text, start - 1);
		}

		return start;
	}

	/// <summary>
	/// The end, exclusive, of the logical line that starts at <paramref name="start"/>: the offset of the line
	/// feed that ends it, or the text's length.
	/// </summary>
	internal static int EndOf(string text, int start)
	{
		int end = text.IndexOf(_LINE_FEED, start);

		while (end >= 0 && IsContinuation(text, end))
		{
			end = text.IndexOf(_LINE_FEED, end + 1);
		}

		return end < 0 ? text.Length : end;
	}

	/// <summary>
	/// The offset after the rest of the physical line that holds <paramref name="from"/> and the next
	/// <paramref name="lines"/> physical lines, or the text's length.
	/// </summary>
	internal static int WindowEnd(string text, int from, int lines)
	{
		int position = from;
		int remaining = lines + 1;

		while (remaining > 0 && position < text.Length)
		{
			int next = text.IndexOf(_LINE_FEED, position);

			position = next < 0 ? text.Length : next + 1;
			remaining--;
		}

		return position;
	}

	/// <summary>
	/// The text of [<paramref name="start"/>, <paramref name="end"/>) with every backslash-newline removed,
	/// cut at <paramref name="maxLength"/> characters.
	/// </summary>
	internal static string JoinedText(string text, int start, int end, int maxLength)
	{
		StringBuilder joined = new();
		int position = start;

		while (position < end && joined.Length < maxLength)
		{
			if (text[position] == _CONTINUATION && position + 1 < end && text[position + 1] == _LINE_FEED)
			{
				position += 2;
			}
			else
			{
				joined.Append(text[position]);
				position++;
			}
		}

		return joined.ToString();
	}

	private static int PhysicalLineStart(string text, int position)
	{
		int previousLineFeed = position > 0 ? text.LastIndexOf(_LINE_FEED, position - 1) : -1;

		return previousLineFeed + 1;
	}

	private static bool IsContinuation(string text, int lineFeedPosition)
	{
		int backslashes = 0;
		int position = lineFeedPosition - 1;

		while (position >= 0 && text[position] == _CONTINUATION)
		{
			backslashes++;
			position--;
		}

		return backslashes % 2 == 1;
	}
}
