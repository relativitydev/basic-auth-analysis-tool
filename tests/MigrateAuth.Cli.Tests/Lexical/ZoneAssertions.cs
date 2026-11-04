using System.Text;

using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// "THIS SUBSTRING LIES ENTIRELY IN THIS ZONE", WHICH IS THE ONLY NOTATION THESE CASES ASSERT IN.
///
/// A hand-counted mask is easy to get wrong, and a miscounted offset reads as a scanner defect. A
/// mask is for reading; a substring and a zone is what a human can state correctly.
///
/// The failure message renders the mask, because when one of these does fail the next question is
/// always "then what did it produce".
/// </summary>
internal static class ZoneAssertions
{
	private const char _CODE_GLYPH = '.';
	private const char _COMMENT_GLYPH = 'c';
	private const char _STRING_BODY_GLYPH = 's';
	private const char _DELIMITER_GLYPH = 'd';
	private const char _UNPRINTABLE_GLYPH = '·';

	/// <summary>The first occurrence of the substring lies entirely in the zone.</summary>
	internal static void SpansZone(ZoneMask mask, string text, string substring, LexicalZone zone)
	{
		SpansZone(mask, text, substring, zone, occurrence: 0);
	}

	/// <summary>The nth occurrence, zero-based, for text that repeats a fragment on purpose.</summary>
	internal static void SpansZone(ZoneMask mask, string text, string substring, LexicalZone zone, int occurrence)
	{
		int start = IndexOfOccurrence(text, substring, occurrence);

		Assert.True(start >= 0, $"The text does not contain occurrence {occurrence} of the asserted substring.");

		LexicalZone[] expected = new LexicalZone[substring.Length];
		LexicalZone[] actual = mask.Zones.Slice(start, substring.Length).ToArray();

		Array.Fill(expected, zone);
		Assert.True(expected.SequenceEqual(actual), Explain(mask, text, start, substring.Length));
	}

	/// <summary>No position in the whole file carries the zone. Used to prove an absence.</summary>
	internal static void NoPositionCarries(ZoneMask mask, string text, LexicalZone zone)
	{
		bool carried = false;

		for (int position = 0; position < mask.Length; position++)
		{
			carried = carried || mask.ZoneAt(position) == zone;
		}

		Assert.False(carried, Explain(mask, text, 0, mask.Length));
	}

	/// <summary>
	/// The zone mask's contract in its literal form: one zone per position, no position without one and
	/// none with two. The mask is a total function over the text's own length, which is what the
	/// second half of that sentence means operationally.
	/// </summary>
	internal static void CoversEveryPositionExactlyOnce(ZoneMask mask, string text)
	{
		Assert.Equal(text.Length, mask.Length);

		for (int position = 0; position < mask.Length; position++)
		{
			Assert.True(Enum.IsDefined(mask.ZoneAt(position)));
		}
	}

	/// <summary>The mask beside its text, one glyph per position, for a failure message.</summary>
	internal static string Render(ZoneMask mask, string text)
	{
		StringBuilder rendered = new();

		rendered.AppendLine(Printable(text));
		rendered.AppendLine(Glyphs(mask));

		return rendered.ToString();
	}

	private static string Explain(ZoneMask mask, string text, int start, int length)
	{
		return $"Positions {start}..{start + length} did not carry the asserted zone.{Environment.NewLine}{Render(mask, text)}";
	}

	private static int IndexOfOccurrence(string text, string substring, int occurrence)
	{
		int found = -1;
		int searchFrom = 0;

		for (int index = 0; index <= occurrence && searchFrom <= text.Length; index++)
		{
			found = text.IndexOf(substring, searchFrom, StringComparison.Ordinal);
			searchFrom = found < 0 ? text.Length + 1 : found + 1;
		}

		return found;
	}

	private static string Printable(string text)
	{
		StringBuilder printable = new(text.Length);

		foreach (char character in text)
		{
			printable.Append(char.IsControl(character) ? _UNPRINTABLE_GLYPH : character);
		}

		return printable.ToString();
	}

	private static string Glyphs(ZoneMask mask)
	{
		StringBuilder glyphs = new(mask.Length);

		for (int position = 0; position < mask.Length; position++)
		{
			glyphs.Append(GlyphFor(mask.ZoneAt(position)));
		}

		return glyphs.ToString();
	}

	private static char GlyphFor(LexicalZone zone)
	{
		return zone switch
		{
			LexicalZone.Comment => _COMMENT_GLYPH,
			LexicalZone.StringBody => _STRING_BODY_GLYPH,
			LexicalZone.Delimiter => _DELIMITER_GLYPH,
			_ => _CODE_GLYPH
		};
	}
}
