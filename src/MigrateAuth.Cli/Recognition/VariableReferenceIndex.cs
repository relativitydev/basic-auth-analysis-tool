using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// EVERY REFERENCE TO A NAMED VARIABLE IN ONE FILE, FOUND BY PLAIN STRING SEARCH AND NO EXPRESSION. A reference
/// is <c>$NAME</c> or <c>${NAME}</c> (anything after the name in the braced form: the name is only checked for
/// its boundary), and the name must END at an identifier boundary, so <c>$NAMES</c> is not a reference to
/// <c>NAME</c>. The offsets of one name are found once per file and kept; the mask decides, per query, which of
/// them count.
///
/// TWO READINGS OF "COUNTS", EACH THE CONSERVATIVE ONE FOR ITS QUESTION. A <see cref="LiveReference"/> is a
/// STRICT reading - a position the scanner vouches for and that is not in a comment - and it is what a use
/// statement needs: a use that cannot be trusted suppresses nothing. A <see cref="ReferencedOutside"/> asks the
/// LENIENT question - is there any reference that is not known to be in a comment - because the construct it
/// protects is dropped when the answer is no, and a position the scanner never reached cannot be allowed to
/// drop it.
/// </summary>
internal sealed class VariableReferenceIndex
{
	private const string _REFERENCE_PREFIX = "$";
	private const string _BRACED_REFERENCE_PREFIX = "${";
	private const char _IDENTIFIER_CONNECTOR = '_';

	private readonly string _text;
	private readonly ZoneMask _mask;
	private readonly Dictionary<string, List<int>> _offsetsByName = new(StringComparer.Ordinal);

	internal VariableReferenceIndex(string text, ZoneMask mask)
	{
		_text = text;
		_mask = mask;
	}

	/// <summary>
	/// The offsets of the <c>$</c> of every reference to <paramref name="name"/> that lies in
	/// [<paramref name="from"/>, <paramref name="to"/>), is trustworthy and is not in a comment. Ascending.
	/// </summary>
	internal List<int> LiveReferencesIn(string name, int from, int to)
	{
		return [.. OffsetsOf(name).Where(offset => offset >= from && offset < to && IsLiveReference(offset))];
	}

	/// <summary>
	/// Whether the name is referenced anywhere in the file outside [<paramref name="spanStart"/>,
	/// <paramref name="spanEnd"/>), at a position that is not known to be a comment.
	/// </summary>
	internal bool ReferencedOutside(string name, int spanStart, int spanEnd)
	{
		return OffsetsOf(name).Any(offset =>
			(offset < spanStart || offset >= spanEnd) && !IsKnownToBeInAComment(offset));
	}

	private bool IsLiveReference(int offset)
	{
		return FileDemotionSignals.IsTrustworthyOutsideACommentAt(_mask, offset);
	}

	private bool IsKnownToBeInAComment(int offset)
	{
		return _mask.IsTrustworthyAt(offset) && _mask.ZoneAt(offset) is LexicalZone.Comment;
	}

	private List<int> OffsetsOf(string name)
	{
		if (!_offsetsByName.TryGetValue(name, out List<int>? offsets))
		{
			offsets = name.Length == 0 ? [] : FindOffsets(name);
			_offsetsByName[name] = offsets;
		}

		return offsets;
	}

	private List<int> FindOffsets(string name)
	{
		List<int> offsets = [];

		AddOffsets(name, _REFERENCE_PREFIX, offsets);
		AddOffsets(name, _BRACED_REFERENCE_PREFIX, offsets);
		offsets.Sort();

		return offsets;
	}

	private void AddOffsets(string name, string prefix, List<int> offsets)
	{
		string needle = prefix + name;
		int found = _text.IndexOf(needle, StringComparison.Ordinal);

		while (found >= 0)
		{
			int end = found + needle.Length;

			if (end >= _text.Length || !IsIdentifierCharacter(_text[end]))
			{
				offsets.Add(found);
			}

			found = _text.IndexOf(needle, found + 1, StringComparison.Ordinal);
		}
	}

	internal static bool IsIdentifierCharacter(char character)
	{
		return char.IsAsciiLetterOrDigit(character) || character == _IDENTIFIER_CONNECTOR;
	}
}
