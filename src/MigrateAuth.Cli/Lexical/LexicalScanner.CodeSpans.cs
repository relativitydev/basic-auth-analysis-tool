using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Lexical;

// THE CODE-SPAN HALF OF THE CODE WALK: a span that one token opens and closes on the same physical line (the shell's backtick command substitution) and whose
// content is a command of its own. Nothing here exists for a profile that declares no CodeSpanElement: the walk's step asks the declaration first and every method
// below is reached only from a non-empty one.
//
// THE READ IS THE EXECUTED-STRING READ WITH ONE DIFFERENCE. The content, with the span's escapes removed, is scanned as the outermost code region of a nested
// scan and its zones are written back through the position table. A here-document opened in the content has, in every shell, an EMPTY body (its body would
// start after a line feed that the span does not contain), so each pending opening is dropped at the span's end and stays code, and the lines after the span are
// read as code. A span that cannot be read soundly (no closing on its line, a nested scan abandoned or recovered, the nesting bound) stays what the walk made it,
// an ordinary run of characters, with no anomaly recorded.
public sealed partial class LexicalScanner
{
	private int ReadACodeSpanAt(ScanContext context, int position)
	{
		int next = position;

		if (!_codeSpans.IsEmpty)
		{
			CodeSpanElement? span = FindTheCodeSpanAt(context.Text, position);
			int closingStart = span is null ? _NO_POSITION : FindTheCodeSpanClosing(context, span, position);

			if (span is not null && closingStart >= 0 && ReadTheCodeSpanContent(context, span, position, closingStart))
			{
				next = closingStart + span.Closing.Length;
			}
		}

		return next;
	}

	private CodeSpanElement? FindTheCodeSpanAt(string text, int position)
	{
		CodeSpanElement? found = null;

		foreach (CodeSpanElement span in _codeSpans)
		{
			if (found is null && MatchesAt(text, position, span.Opening, _delimiterComparison))
			{
				found = span;
			}
		}

		return found;
	}

	/// <summary>
	/// The closing of the span that opens at the position: the first closing token on the opening's own physical line that no odd run of the span's escape
	/// precedes, with no nesting counted. The search stops at the line-length ceiling and at the per-scan search budget, and the characters it read are charged
	/// to it, so openers that never close cost a bounded multiple of the text.
	/// </summary>
	private int FindTheCodeSpanClosing(ScanContext context, CodeSpanElement span, int position)
	{
		string text = context.Text;
		char escape = span.EscapeCharacter[0];
		int contentStart = position + span.Opening.Length;
		int searchEnd = (int)Math.Min(text.Length, (long)position + Math.Min(ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS, context.InlineSearchRemaining));
		int cursor = contentStart;
		int closingStart = _NO_POSITION;
		bool endsTheLine = false;

		while (cursor < searchEnd && closingStart < 0 && !endsTheLine)
		{
			if (text[cursor] == _LINE_FEED)
			{
				endsTheLine = true;
			}
			else if (text[cursor] == escape)
			{
				endsTheLine = cursor + _SINGLE_CHARACTER_LENGTH < text.Length && text[cursor + _SINGLE_CHARACTER_LENGTH] == _LINE_FEED;
				cursor += _ESCAPE_PAIR_LENGTH;
			}
			else if (MatchesAt(text, cursor, span.Closing, _delimiterComparison))
			{
				closingStart = cursor;
			}
			else
			{
				cursor++;
			}
		}

		context.InlineSearchRemaining -= Math.Max(Math.Min(cursor, searchEnd) - contentStart, _SINGLE_CHARACTER_LENGTH);

		return closingStart;
	}

	/// <summary>The content read as code, and the two tokens marked as delimiters where it was read; a span that could not be read is left as it was.</summary>
	private bool ReadTheCodeSpanContent(ScanContext context, CodeSpanElement span, int position, int closingStart)
	{
		int contentStart = position + span.Opening.Length;
		bool isRead = ReadTheSpanAsCode(context, contentStart, closingStart, span.EscapeCharacter[0], span.EscapableCharacters, dropsThePendingBodies: true);

		if (isRead)
		{
			context.Mark(position, span.Opening.Length, LexicalZone.Delimiter);
			context.Mark(closingStart, span.Closing.Length, LexicalZone.Delimiter);
		}

		return isRead;
	}

	/// <summary>
	/// A here-document opened in a span has an empty body: every opening still pending at the end of the nested scan is dropped, and its own span, which the
	/// nested scan marked as a delimiter, is reset to code, the zone an opening refused where it stands takes.
	/// </summary>
	private static void DropThePendingOpenings(ScanContext nested)
	{
		foreach (LiteralOccurrence pending in nested.DequeueEveryPendingBody())
		{
			nested.Mark(pending.OpeningPosition, pending.BodyStart - pending.OpeningPosition, LexicalZone.Code);
		}
	}
}
