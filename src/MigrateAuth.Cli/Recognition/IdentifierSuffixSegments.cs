using System.Collections.Immutable;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE IDENTIFIER-SUFFIX READING OF A CREDENTIAL WORD'S OCCURRENCE. A credential word that is directly followed
/// by an identifier suffix names the identifier of a credential and not the credential, so the occurrence does not count.
///
/// The text is read in identifier word segments. A segment boundary is the start or the end of the text, any character that is
/// not a letter or a digit (an underscore, a dot, a bracket, a space, an operator), a change from a lower-case letter to an
/// upper-case one, or an upper-case run followed by an upper-case letter and a lower-case one (the acronym boundary, used only
/// to cut the suffix segment). Digits do not delimit. An occurrence is directly followed by a suffix when it ends at a segment
/// boundary and the next segment, whole and compared ignoring letter case, is one of the declared suffixes.
///
/// One linear pass over the characters that follow the occurrence: no expression, no backtracking, nothing that can exhaust
/// the match bound. The occurrences themselves come from the condition's word expression, which stays the charged one.
/// </summary>
public static class IdentifierSuffixSegments
{
	/// <summary>Whether the occurrence of a credential word that ends at <paramref name="wordEnd"/> is directly followed by one of <paramref name="suffixes"/>.</summary>
	public static bool IsDirectlyFollowedByASuffix(string text, int wordEnd, ImmutableArray<string> suffixes)
	{
		bool followed = false;

		if (IsSegmentBoundary(text, wordEnd))
		{
			int segmentStart = wordEnd;

			while (segmentStart < text.Length && !char.IsLetterOrDigit(text[segmentStart]))
			{
				segmentStart++;
			}

			if (segmentStart < text.Length)
			{
				int segmentEnd = segmentStart + 1;

				while (segmentEnd < text.Length && !IsSegmentBoundary(text, segmentEnd))
				{
					segmentEnd++;
				}

				followed = IsOneOf(text.AsSpan(segmentStart, segmentEnd - segmentStart), suffixes);
			}
		}

		return followed;
	}

	/// <summary>Whether a segment of the identifier ends, and the next begins, at <paramref name="position"/> (0 to the text's length).</summary>
	private static bool IsSegmentBoundary(string text, int position)
	{
		bool boundary = position >= text.Length || !char.IsLetterOrDigit(text[position]);

		if (!boundary && position > 0)
		{
			char previous = text[position - 1];
			char current = text[position];

			boundary = (char.IsLower(previous) && char.IsUpper(current))
				|| (char.IsUpper(previous) && char.IsUpper(current) && position + 1 < text.Length && char.IsLower(text[position + 1]));
		}

		return boundary;
	}

	private static bool IsOneOf(ReadOnlySpan<char> segment, ImmutableArray<string> suffixes)
	{
		bool found = false;

		foreach (string suffix in suffixes)
		{
			found |= segment.Equals(suffix, StringComparison.OrdinalIgnoreCase);
		}

		return found;
	}
}
