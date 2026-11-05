namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// A body with the outer quoting's escapes removed, and where each of its characters stood: the command a shell runs,
/// and the table that writes what is learned about it back to the file's own positions.
/// </summary>
internal readonly record struct UnescapedText(string Text, int[] Map);

/// <summary>
/// REMOVES FROM A BODY WHAT THE OUTER QUOTING REMOVES, AND NOTHING WHERE IT REMOVES NOTHING: with no escapable character
/// declared (a single-quoted body, in which a backslash and a line feed both stay) the body is taken as it stands. A backslash before one of the declared characters is dropped and
/// the character kept, a backslash with the line feed after it is dropped whole, and a backslash before anything else
/// stays as the character it is. The result is linear in the body and carries the file position of every character it
/// keeps, so a zone written for one of them lands on the character that stands there in the file; the dropped
/// backslashes are not in the table and keep the zone the outer scan gave them.
/// </summary>
internal static class ExecutedBodyUnescaping
{
	private const char _LINE_FEED = '\n';
	private const int _ESCAPE_PAIR_LENGTH = 2;
	private const int _SINGLE_CHARACTER_LENGTH = 1;

	internal static UnescapedText Unescape(string text, int start, int end, char? escape, string escapable)
	{
		char[] kept = new char[end - start];
		int[] map = new int[end - start];
		int count = 0;
		int position = start;

		while (position < end)
		{
			bool startsAPair = escape is char escapeCharacter
				&& escapable.Length > 0
				&& text[position] == escapeCharacter
				&& position + _SINGLE_CHARACTER_LENGTH < end;
			char following = startsAPair ? text[position + _SINGLE_CHARACTER_LENGTH] : default;
			bool dropsTheEscape = startsAPair && (following == _LINE_FEED || escapable.Contains(following));

			if (dropsTheEscape && following != _LINE_FEED)
			{
				kept[count] = following;
				map[count] = position + _SINGLE_CHARACTER_LENGTH;
				count++;
			}
			else if (!dropsTheEscape)
			{
				kept[count] = text[position];
				map[count] = position;
				count++;
			}

			position += dropsTheEscape ? _ESCAPE_PAIR_LENGTH : _SINGLE_CHARACTER_LENGTH;
		}

		return new UnescapedText(new string(kept, 0, count), count == map.Length ? map : map[..count]);
	}
}
