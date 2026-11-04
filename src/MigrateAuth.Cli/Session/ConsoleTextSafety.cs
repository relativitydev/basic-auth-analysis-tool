using System.Globalization;
using System.Text;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// The other half of the path rule: a path is untrusted text on the way out. A folder name
/// is chosen by the repository, and on one platform it may carry a line ending or a terminal
/// control sequence. Anything the session echoes back - a folder label, a token the user typed
/// - passes through here first, so that a name cannot repaint the terminal or forge a line of
/// output that looks like the tool's own.
///
/// Escaping for Markdown belongs to <see cref="Emission.EmittedPathText"/>, which calls this
/// first and then escapes what remains; the data artifact relies on its own encoder.
/// </summary>
public static class ConsoleTextSafety
{
	/// <summary>
	/// Removes every character that can act on a renderer rather than appear in it, leaving the
	/// rest of the text untouched.
	///
	/// IT COVERS MORE THAN CONTROL CHARACTERS, and this is the reason. Control characters are the
	/// C0 and C1 ranges, and stripping them stops an escape sequence and a forged line. They do
	/// not include the Unicode FORMATTING characters, which are a separate category and which
	/// include the bidirectional overrides - and a filename carrying a right-to-left override
	/// renders as a different filename than it is, in a terminal and in Markdown alike, without
	/// containing a single control character. That is the same class of problem this class
	/// exists for: text from the repository changing what the reader sees rather than being
	/// what the reader sees.
	///
	/// The cost of the wider rule is that a legitimate name using a zero-width joiner loses it
	/// and renders slightly differently. That is the safe direction, for the reason the write
	/// boundary folds case everywhere: the visible, recoverable error is preferable to the
	/// silent one.
	/// </summary>
	public static string StripUnsafeCharacters(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		StringBuilder safeText = new(text.Length);

		foreach (char character in text)
		{
			if (!IsRendererDirective(character))
			{
				safeText.Append(character);
			}
		}

		return safeText.ToString();
	}

	private static bool IsRendererDirective(char character)
	{
		bool isDirective =
			char.IsControl(character)
			|| CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.Format;

		return isDirective;
	}
}
