using System.Text;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// FRAMES A PATH AS A MARKDOWN CODE SPAN THAT THE PATH ITSELF CANNOT CLOSE.
///
/// A code span ends at the first run of backticks as long as the one that opened it. The delimiter is
/// therefore one backtick longer than the longest run of backticks inside the path, so no run inside the
/// path can match it, and one space is placed inside each end so that a path that starts or ends with a
/// backtick is not read as part of a delimiter. No escaping is done inside the span: in a code span a
/// backslash is itself.
///
/// The same framing is used wherever a path is placed in text that an agent reads, so the rule has one
/// implementation.
/// </summary>
public static class PathCodeSpan
{
	private const char _BACKTICK = '`';
	private const char _PADDING = ' ';
	private const int _DELIMITERS_PER_SPAN = 2;
	private const int _PADDING_CHARACTERS_PER_SPAN = 2;

	/// <summary>The length of the longest run of consecutive backticks in <paramref name="text"/>.</summary>
	public static int LongestBacktickRun(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		int longestRun = 0;
		int currentRun = 0;

		foreach (char character in text)
		{
			currentRun = character == _BACKTICK ? currentRun + 1 : 0;
			longestRun = Math.Max(longestRun, currentRun);
		}

		return longestRun;
	}

	/// <summary>The path inside a code span: delimiter, one space, the path, one space, delimiter.</summary>
	public static string Frame(string path)
	{
		ArgumentNullException.ThrowIfNull(path);

		string delimiter = new(_BACKTICK, LongestBacktickRun(path) + 1);

		return new StringBuilder((delimiter.Length * _DELIMITERS_PER_SPAN) + path.Length + _PADDING_CHARACTERS_PER_SPAN)
			.Append(delimiter)
			.Append(_PADDING)
			.Append(path)
			.Append(_PADDING)
			.Append(delimiter)
			.ToString();
	}
}
