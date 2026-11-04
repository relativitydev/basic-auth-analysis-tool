using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Lexical;

// THE EXECUTED-STRING HALF OF THE LITERAL WALK: a string, a here-document body, a backtick or a parameter word that
// the profile's declared commands run as a command is read AGAIN, as code, once the outer scan has closed it. Nothing here exists for a profile
// that declares no ExecutedStringContext: the reader and the tables are null, and every method below is guarded by them
// before it does anything.
//
// THE READ IS A NESTED SCAN OF THE BODY WITH ITS ESCAPES REMOVED. The outer quoting removes a backslash before a dollar
// sign, a backtick, a double quote or a backslash, and a backslash-newline pair, so the command the shell runs is not the
// text of the file. The body is rebuilt with those removed and with a table of where every character stood in the file,
// scanned as the outermost region of a context of its own (so its comments, strings and here-documents are recognized
// as in code, and nothing it leaves open can reach the file's own queues), and its zones are written back through the
// table. A body that cannot be read soundly stays what the outer scan made it: a string zone.
public sealed partial class LexicalScanner
{
	/// <summary>The answer of a search that found no position: no word start, no closing, no body end.</summary>
	private const int _NO_POSITION = -1;
	private const int _INLINE_SEARCH_CHARACTERS_PER_TEXT_CHARACTER = 8;
	private const int _INLINE_SEARCH_BASE_CHARACTERS = 4096;

	private readonly ExecutedStringContext? _executedDeclaration;
	private readonly ExecutedCommandReader? _executedReader;
	private readonly Dictionary<string, string>? _executedBodies;
	private readonly ImmutableArray<InlineExecutedContext> _inlineContexts;

	/// <summary>Whether, and under what condition, the closed body of one literal is read again as code.</summary>
	private enum ExecutedMode
	{
		/// <summary>The body stays a string zone.</summary>
		None = 0,

		/// <summary>The words before the opening say the profile's declared commands run the body.</summary>
		Always = 1,

		/// <summary>A here-document of a command that is not a shell: read as code if its first line is the profile's declared shebang.</summary>
		IfTheFirstLineIsAShebang = 2
	}

	/// <summary>The decision about one literal, and the escapes the outer quoting removes from its body.</summary>
	private readonly record struct ExecutedReading(ExecutedMode Mode, string EscapableCharacters);

	/// <summary>The window of one closing search: the text, where it starts reading and where it must stop.</summary>
	private readonly record struct InlineSearch(string Text, int From, int End);

	/// <summary>The span of an inline construct: where its content starts and ends and where the construct ends.</summary>
	private readonly record struct InlineSpan(int ContentStart, int ContentEnd, int End);

	private static ExecutedCommandReader? BuildTheExecutedReader(LexicalProfile profile, StringComparison keywordComparison)
	{
		return profile.ExecutedStrings is ExecutedStringContext declaration
			? new ExecutedCommandReader(declaration, keywordComparison, profile.CodeEscapeCharacter)
			: null;
	}

	private static Dictionary<string, string>? BuildTheExecutedBodies(LexicalProfile profile)
	{
		return profile.ExecutedStrings?.ReadableBodies.ToDictionary(body => body.QuoteOpening, body => body.EscapableCharacters, StringComparer.Ordinal);
	}

	/// <summary>
	/// Decides, where the opening has just matched, whether the body will be read as code: the quote form is one the
	/// profile lists, it carries no literal prefix, and the words around the opening say the profile's declared commands run it. A here-document
	/// that no word marks is still a candidate, read as code if its first line turns out to be a shebang.
	/// </summary>
	private LiteralOccurrence WithTheExecutedReading(ScanContext context, LexicalRuleCandidate candidate, LiteralOccurrence occurrence)
	{
		string? escapable = null;
		bool isCandidate = _executedReader is not null
			&& candidate.Prefix is null
			&& _executedBodies is not null
			&& _executedBodies.TryGetValue(occurrence.Rule.Opening, out escapable);
		ExecutedMode mode = isCandidate ? DecideTheExecutedMode(context, occurrence) : ExecutedMode.None;

		return mode is ExecutedMode.None ? occurrence : occurrence with { Executed = new ExecutedReading(mode, escapable!) };
	}

	private ExecutedMode DecideTheExecutedMode(ScanContext context, LiteralOccurrence occurrence)
	{
		ExecutedMode mode = ExecutedMode.None;

		if (occurrence.DefersItsBody)
		{
			HereDocumentFeed feed = _executedReader!.ReadTheHereDocumentFeed(context.Text, context.Zones, occurrence.OpeningPosition, occurrence.BodyStart);

			mode = feed.IsFedToAShell || feed.WritesAScript ? ExecutedMode.Always : ExecutedMode.IfTheFirstLineIsAShebang;
		}
		else if (_executedReader!.StartsAnExecutedString(context.Text, context.Zones, occurrence.OpeningPosition))
		{
			mode = ExecutedMode.Always;
		}

		return mode;
	}

	/// <summary>
	/// Reads the body again as code where the outer scan closed it properly (a body that ended on a raw newline, was
	/// never closed or abandoned the scan is left alone) and the decision is yes.
	/// </summary>
	private void ReadTheClosedBodyAsCodeIfItIsExecuted(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		bool isClosedProperly = cursor.Closed && cursor.BodyEnd >= occurrence.BodyStart && !context.IsAbandoned;

		if (isClosedProperly && occurrence.Executed.Mode is not ExecutedMode.None)
		{
			bool isRead = occurrence.Executed.Mode is ExecutedMode.Always
				|| _executedReader!.BeginsWithAShellShebang(
					context.Text,
					occurrence.BodyStart,
					cursor.BodyEnd,
					occurrence.Rule.ClosingAnchoring is PositionalAnchoring.WholeLineAfterLeadingTabs,
					_TAB);

			if (isRead)
			{
				ReadTheSpanAsCode(
					context,
					occurrence.BodyStart,
					cursor.BodyEnd,
					UnescapesItsBody(occurrence) ? occurrence.EscapeCharacter : null,
					occurrence.Executed.EscapableCharacters);
			}
		}
	}

	/// <summary>
	/// The outer shell removes escapes from a double-quoted string and from an unquoted here-document body; a
	/// single-quoted string and a quoted here-document are taken literally.
	/// </summary>
	private static bool UnescapesItsBody(LiteralOccurrence occurrence)
	{
		return occurrence.EscapeCharacter is not null && (!occurrence.DefersItsBody || occurrence.JoinsContinuedLines);
	}

	/// <summary>
	/// Reads a span of the file as the code the profile's declared commands run, and reports whether it did. The nesting bound is checked
	/// first and a span past it is not read; a nested scan that abandoned (a construct open at its end, a here-document
	/// body pending, the nesting recovered or an anomaly recorded) leaves the span as it was, with the exception of the
	/// lexical budget, which is a fact about the file and abandons the file's scan like any other budget trip.
	/// </summary>
	private bool ReadTheSpanAsCode(ScanContext context, int start, int end, char? escape, string escapable)
	{
		return ReadTheSpanAsCode(context, start, end, escape, escapable, dropsThePendingBodies: false);
	}

	/// <summary>
	/// The same read, with the decision about a here-document body left pending at the end of the nested scan: an executed string abandons the nested read, and the
	/// span stays what the outer scan made it; a code span drops each pending opening, because a here-document opened in a span that holds no line feed has an
	/// empty body in every shell.
	/// </summary>
	private bool ReadTheSpanAsCode(ScanContext context, int start, int end, char? escape, string escapable, bool dropsThePendingBodies)
	{
		bool isRead = false;

		if (context.ExecutedDepth < LexicalEnginePolicy.MAXIMUM_EXECUTED_STRING_NESTING_DEPTH)
		{
			UnescapedText body = ExecutedBodyUnescaping.Unescape(context.Text, start, end, escape, escapable);
			ScanContext nested = new(body.Text) { ExecutedDepth = context.ExecutedDepth + 1 };

			_ = ScanCodeRegion(nested, new CodeRegion
			{
				Start = 0,
				EndExclusive = body.Text.Length,
				Depth = 0,
				IsOutermost = true
			});

			if (dropsThePendingBodies)
			{
				DropThePendingOpenings(nested);
			}
			else
			{
				nested.AbandonIfBodiesAreStillPending();
			}

			if (nested.WasAbandonedForTheLexicalBudget)
			{
				context.Abandon(LexicalAnomalyReason.LexicalBudgetExceeded, context.OutermostConstructOpening, start, LexicalZone.StringBody);
			}
			else if (!nested.IsAbandoned && !nested.HasRecoveredFromAnAnomaly)
			{
				context.MergeTheZonesOf(nested, body.Map);
				isRead = true;
			}
		}

		return isRead;
	}

	/// <summary>
	/// THE INLINE CONSTRUCT, tried at a position of a double-quoted body: a backtick or a parameter word, which the outer
	/// shell runs or expands as code though the outer scan holds it as string body. Where its closing is found it is
	/// consumed whole, so a quote inside it neither ends the string nor pairs with a later one: its opening and closing
	/// tokens are delimiters, its content is read as code where it can be and stays string body where it cannot. Where no
	/// closing is found, or the construct has no word, nothing is consumed and the default step reads the character.
	/// </summary>
	private bool TryReadAnInlineExecutedContext(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		InlineExecutedContext? inline = FindTheInlineContextAt(context.Text, occurrence, cursor.Position);
		InlineSpan? span = inline is null ? null : FindTheInlineSpan(context, occurrence, inline, cursor.Position);

		if (inline is not null && span is InlineSpan found)
		{
			int closingStart = found.End - inline.Closing.Length;

			context.Mark(cursor.Position, found.End - cursor.Position, LexicalZone.StringBody);

			bool isRead = ReadTheSpanAsCode(context, found.ContentStart, found.ContentEnd, occurrence.EscapeCharacter, inline.EscapableCharacters);

			if (isRead)
			{
				context.Mark(cursor.Position, inline.Opening.Length, LexicalZone.Delimiter);
				context.Mark(closingStart, inline.Closing.Length, LexicalZone.Delimiter);
			}

			cursor.Position = found.End;
		}

		return span is not null;
	}

	private InlineExecutedContext? FindTheInlineContextAt(string text, LiteralOccurrence occurrence, int position)
	{
		InlineExecutedContext? found = null;

		foreach (InlineExecutedContext inline in _inlineContexts)
		{
			if (found is null
				&& inline.QuoteOpening == occurrence.Rule.Opening
				&& MatchesAt(text, position, inline.Opening, _delimiterComparison))
			{
				found = inline;
			}
		}

		return found;
	}

	/// <summary>
	/// Where the construct that opens at the position ends: after the parameter name and operator where the construct
	/// has a word, and at its first unescaped closing, counting nested openings when the closing differs from the
	/// opening. The search stops at the line-length ceiling, so a construct that never closes costs a bounded read.
	/// </summary>
	private InlineSpan? FindTheInlineSpan(ScanContext context, LiteralOccurrence occurrence, InlineExecutedContext inline, int position)
	{
		string text = context.Text;
		int contentStart = position + inline.Opening.Length;
		int searchEnd = (int)Math.Min(text.Length, (long)position + Math.Min(ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS, context.InlineSearchRemaining));
		int wordStart = inline.WordOperators.IsEmpty ? contentStart : FindTheWordStart(text, inline, contentStart, searchEnd);
		int closingStart = wordStart < 0 ? _NO_POSITION : FindTheInlineClosing(context, occurrence, inline, wordStart, searchEnd);

		return closingStart < 0 ? null : new InlineSpan(wordStart, closingStart, closingStart + inline.Closing.Length);
	}

	/// <summary>The position after the parameter name and the operator that begins its word, or a negative number where there is none.</summary>
	private int FindTheWordStart(string text, InlineExecutedContext inline, int contentStart, int searchEnd)
	{
		int cursor = contentStart;

		while (cursor < searchEnd && (char.IsAsciiLetterOrDigit(text[cursor]) || text[cursor] == _executedDeclaration!.NameConnectorCharacter))
		{
			cursor++;
		}

		string? operatorFound = cursor > contentStart
			? inline.WordOperators
				.Where(candidate => MatchesAt(text, cursor, candidate, _delimiterComparison))
				.OrderByDescending(candidate => candidate.Length)
				.FirstOrDefault()
			: null;

		return operatorFound is null ? _NO_POSITION : cursor + operatorFound.Length;
	}

	/// <summary>
	/// The closing of the inline construct, found by the scan below, with the characters the scan read charged to the
	/// per-scan budget of inline searches.
	/// </summary>
	private int FindTheInlineClosing(ScanContext context, LiteralOccurrence occurrence, InlineExecutedContext inline, int from, int searchEnd)
	{
		int closingStart = ScanForTheInlineClosing(new InlineSearch(context.Text, from, searchEnd), occurrence, inline, out int scannedTo);

		context.InlineSearchRemaining -= Math.Max(scannedTo - from, _SINGLE_CHARACTER_LENGTH);

		return closingStart;
	}

	/// <summary>
	/// The first unescaped closing, counting nested openings where the closing differs from the opening and skipping the
	/// quoted runs of a parameter word. A quote that never closes makes the construct unreadable: no position.
	/// </summary>
	private int ScanForTheInlineClosing(InlineSearch search, LiteralOccurrence occurrence, InlineExecutedContext inline, out int scannedTo)
	{
		string text = search.Text;
		int searchEnd = search.End;
		int cursor = search.From;
		int depth = 0;
		int closingStart = _NO_POSITION;
		bool closesOnItsOpening = inline.Closing == inline.Opening;
		bool isUnreadable = false;

		while (cursor < searchEnd && closingStart < 0 && !isUnreadable)
		{
			bool atTheClosing = MatchesAt(text, cursor, inline.Closing, _delimiterComparison);

			if (occurrence.EscapeCharacter is char escape && text[cursor] == escape)
			{
				cursor += _ESCAPE_PAIR_LENGTH;
			}
			else if (!closesOnItsOpening && _executedDeclaration!.QuoteCharacters.Contains(text[cursor]))
			{
				int afterTheQuote = SkipAQuotedRun(text, cursor, searchEnd);

				isUnreadable = afterTheQuote < 0;
				cursor = isUnreadable ? searchEnd : afterTheQuote;
			}
			else if (atTheClosing && (closesOnItsOpening || depth == 0))
			{
				closingStart = cursor;
			}
			else if (atTheClosing)
			{
				depth--;
				cursor += inline.Closing.Length;
			}
			else if (MatchesAt(text, cursor, inline.Opening, _delimiterComparison))
			{
				depth++;
				cursor += inline.Opening.Length;
			}
			else
			{
				cursor++;
			}
		}

		scannedTo = cursor;

		return isUnreadable ? _NO_POSITION : closingStart;
	}

	/// <summary>
	/// The position after the quoted run that opens at the position and closes on its own quote character, with the
	/// outer escape honoured inside a quote whose declared body escapes anything. A quote that does not close before the
	/// search ends makes the whole construct unreadable: the answer is no position.
	/// </summary>
	private int SkipAQuotedRun(string text, int position, int searchEnd)
	{
		char quote = text[position];
		bool honoursTheEscape = _executedDeclaration!.ReadableBodies.Any(body => body.QuoteOpening == quote.ToString() && body.EscapableCharacters.Length > 0);
		int cursor = position + _SINGLE_CHARACTER_LENGTH;

		while (cursor < searchEnd && text[cursor] != quote)
		{
			bool isAnEscape = honoursTheEscape && _codeEscape is char escape && text[cursor] == escape;

			cursor += isAnEscape ? _ESCAPE_PAIR_LENGTH : _SINGLE_CHARACTER_LENGTH;
		}

		return cursor < searchEnd ? cursor + _SINGLE_CHARACTER_LENGTH : _NO_POSITION;
	}
}
