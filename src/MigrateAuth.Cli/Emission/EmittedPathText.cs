using System.Text;

using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Emission;

/// <summary>Where a fact is on its way to, which is what decides how its path is escaped.</summary>
public enum EmissionDestination
{
	/// <summary>A terminal, which interprets control and formatting characters.</summary>
	Terminal = 0,

	/// <summary>The Markdown report, whose metacharacters change how text renders.</summary>
	Markdown = 1,

	/// <summary>The data artifact, which relies on its own encoder.</summary>
	StructuredData = 2
}

/// <summary>
/// A PATH IS UNTRUSTED TEXT ON THE WAY OUT.
///
/// The invariant permits the path relative to the repository and says nothing about its
/// content, and the content is not ours: a filename is chosen by the repository. On one
/// platform it may carry a line ending or a terminal control sequence; on any platform it may
/// carry Markdown that closes a code fence, opens a link, or starts a table row.
///
/// So the path is escaped FOR ITS DESTINATION rather than once and for all, because the three
/// destinations are wrong about different characters. A terminal cares about control and
/// formatting characters and not at all about an asterisk; Markdown cares about the asterisk
/// and cannot be protected by removing it; and the data artifact needs neither, because its
/// own encoder is the correct answer and escaping before it would double-escape.
/// </summary>
public static class EmittedPathText
{
	private const char _MARKDOWN_ESCAPE_CHARACTER = '\\';

	/// <summary>
	/// The characters that change INLINE rendering. Each is here for a construct it can open:
	/// the backslash escapes, the backtick opens a code span, the asterisk and underscore open
	/// emphasis, the brackets and parentheses open a link, the angle brackets open an autolink
	/// or raw HTML, the ampersand opens an entity, the bar opens a table cell, the tilde opens
	/// strikethrough, the exclamation mark opens an image, and the braces open an attribute
	/// block in the flavours that have one.
	/// </summary>
	private const string _MARKDOWN_INLINE_METACHARACTERS = "\\`*_[]()<>&|~!{}";

	/// <summary>
	/// The characters that mean nothing inline but open a BLOCK when they lead a line: a
	/// heading, a bullet, a setext underline. They are escaped only in leading position, which
	/// is what keeps an ordinary name such as <c>basic-auth.md</c> readable.
	/// </summary>
	private const string _MARKDOWN_LEADING_METACHARACTERS = "#-+=>";

	/// <summary>
	/// The characters that turn a leading run of digits into an ordered list item. They are
	/// escaped only when everything before them is a digit.
	/// </summary>
	private const string _MARKDOWN_ORDERED_LIST_METACHARACTERS = ".)";

	/// <summary>
	/// Renders an emitted path for where it is going. A path that was never declared renders as
	/// nothing rather than as a placeholder, because a placeholder in a report reads as a file.
	/// </summary>
	public static string ForDestination(RepositoryRelativePath path, EmissionDestination destination)
	{
		string renderedText = path.IsDeclared
			? Escape(path.Value, destination)
			: string.Empty;

		return renderedText;
	}

	/// <summary>
	/// The same escaping for the one other piece of repository-chosen text the invariant permits
	/// in output: the repository root as a LABEL, emitted in place of the path itself.
	///
	/// It is here rather than in a second escaper because the hazard is identical - a folder name
	/// is chosen by the repository exactly as a file name is, and a label carrying a bar or a
	/// leading hash breaks the table or opens a heading in the report just as readily. The label
	/// has already been stripped of renderer directives when it was derived; escaping is
	/// idempotent over that and the two steps compose.
	/// </summary>
	public static string LabelForDestination(string label, EmissionDestination destination)
	{
		ArgumentNullException.ThrowIfNull(label);

		return Escape(label, destination);
	}

	private static string Escape(string pathText, EmissionDestination destination)
	{
		string escapedText = destination switch
		{
			EmissionDestination.Terminal => ConsoleTextSafety.StripUnsafeCharacters(pathText),
			EmissionDestination.Markdown => EscapeForMarkdown(ConsoleTextSafety.StripUnsafeCharacters(pathText)),
			EmissionDestination.StructuredData => pathText,
			_ => throw new ArgumentOutOfRangeException(nameof(destination))
		};

		return escapedText;
	}

	/// <summary>
	/// Backslash-escapes what Markdown would otherwise read as markup. Control and formatting
	/// characters are already gone by the time this runs: they cannot be escaped into
	/// harmlessness, only removed.
	/// </summary>
	private static string EscapeForMarkdown(string pathText)
	{
		StringBuilder escapedText = new(pathText.Length);
		bool insideLeadingDigitRun = true;

		for (int position = 0; position < pathText.Length; position++)
		{
			char character = pathText[position];

			if (RequiresMarkdownEscape(character, position, insideLeadingDigitRun))
			{
				escapedText.Append(_MARKDOWN_ESCAPE_CHARACTER);
			}

			escapedText.Append(character);
			insideLeadingDigitRun = insideLeadingDigitRun && char.IsAsciiDigit(character);
		}

		return escapedText.ToString();
	}

	private static bool RequiresMarkdownEscape(char character, int position, bool insideLeadingDigitRun)
	{
		bool requiresEscape =
			_MARKDOWN_INLINE_METACHARACTERS.Contains(character, StringComparison.Ordinal)
			|| (position == 0 && _MARKDOWN_LEADING_METACHARACTERS.Contains(character, StringComparison.Ordinal))
			|| (position > 0
				&& insideLeadingDigitRun
				&& _MARKDOWN_ORDERED_LIST_METACHARACTERS.Contains(character, StringComparison.Ordinal));

		return requiresEscape;
	}
}
