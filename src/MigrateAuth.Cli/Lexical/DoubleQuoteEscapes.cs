using System.Text;

namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// THE ESCAPES OF A DOUBLE-QUOTED WORD, removed the way a shell removes them from a here-document's terminator:
/// a backslash before a backslash, a double quote, a dollar sign or a backtick is removed and the character
/// stays; a backslash before a line feed is a line continuation and the pair is removed, so a word quoted across
/// a continuation (<c>"E\&lt;newline&gt;F"</c>) is the word <c>EF</c>; any other backslash stays with its
/// character. The text handed over is the content between the quotes.
/// </summary>
public static class DoubleQuoteEscapes
{
	private const char _ESCAPE_INTRODUCER = '\\';
	private const char _LINE_FEED = '\n';
	private const int _ESCAPE_INTRODUCER_LENGTH = 1;
	private const int _CONTINUATION_LENGTH = 2;
	private const string _ESCAPABLE_CHARACTERS = "\\\"$`";

	/// <summary>Decodes the content of a double-quoted word.</summary>
	public static string Decode(string content)
	{
		StringBuilder decoded = new(content.Length);
		int position = 0;

		while (position < content.Length)
		{
			bool hasACharacterAfter = content[position] == _ESCAPE_INTRODUCER && position + _ESCAPE_INTRODUCER_LENGTH < content.Length;

			if (hasACharacterAfter && content[position + _ESCAPE_INTRODUCER_LENGTH] == _LINE_FEED)
			{
				position += _CONTINUATION_LENGTH;
			}
			else
			{
				if (hasACharacterAfter && _ESCAPABLE_CHARACTERS.Contains(content[position + _ESCAPE_INTRODUCER_LENGTH]))
				{
					position += _ESCAPE_INTRODUCER_LENGTH;
				}

				decoded.Append(content[position]);
				position++;
			}
		}

		return decoded.ToString();
	}
}
