using System.Buffers;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// A REPOSITORY-RELATIVE PATH AS IT IS WRITTEN IN THE PACK: escaped by rune, then framed as a code span.
///
/// A file name is chosen by whoever wrote the repository and the pack is read by an agent, so the name is
/// data that must not be able to change what the pack says. Every rune in a category that can hide or
/// reorder text (control, format - tag characters and bidirectional controls included - private use,
/// unassigned, line and paragraph separators) is written as a visible <c>\u{HEX}</c> escape, and so is an
/// unpaired surrogate; nothing is dropped. The result is framed by <see cref="PathCodeSpan"/>, whose
/// delimiter no backtick run in the path can close. No other escaping is done inside the span.
///
/// The walk is by rune rather than by <c>char</c>: a character outside the basic plane is two chars, and
/// a per-char test would see neither half as the category it has.
/// </summary>
public static class PackPathText
{
	private const string _ESCAPE_PREFIX = "\\u{";
	private const string _ESCAPE_SUFFIX = "}";
	private const string _HEXADECIMAL_UPPER_FORMAT = "X";

	/// <summary>The escaped text of the path without its code-span framing.</summary>
	public static string Escape(RepositoryRelativePath path)
	{
		string value = path.Value ?? string.Empty;
		StringBuilder escaped = new(value.Length);
		int position = 0;

		while (position < value.Length)
		{
			OperationStatus status = Rune.DecodeFromUtf16(value.AsSpan(position), out Rune rune, out int consumed);

			if (status != OperationStatus.Done)
			{
				AppendEscape(escaped, value[position]);
			}
			else if (NeedsEscape(rune))
			{
				AppendEscape(escaped, rune.Value);
			}
			else
			{
				escaped.Append(rune.ToString());
			}

			position += consumed;
		}

		return escaped.ToString();
	}

	/// <summary>The escaped path inside a code span no part of the path can close.</summary>
	public static string Render(RepositoryRelativePath path)
	{
		return PathCodeSpan.Frame(Escape(path));
	}

	private static bool NeedsEscape(Rune rune)
	{
		UnicodeCategory category = Rune.GetUnicodeCategory(rune);

		return category is UnicodeCategory.Control
			or UnicodeCategory.Format
			or UnicodeCategory.PrivateUse
			or UnicodeCategory.OtherNotAssigned
			or UnicodeCategory.LineSeparator
			or UnicodeCategory.ParagraphSeparator
			or UnicodeCategory.Surrogate;
	}

	private static void AppendEscape(StringBuilder escaped, int codePoint)
	{
		escaped
			.Append(_ESCAPE_PREFIX)
			.Append(codePoint.ToString(_HEXADECIMAL_UPPER_FORMAT, CultureInfo.InvariantCulture))
			.Append(_ESCAPE_SUFFIX);
	}
}
