using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Lexical;

// THE LITERAL HALF OF THE MUTUAL RECURSION. Everything here is reached from ScanCodeRegion and
// reaches back into it through an interpolation hole. It is a separate FILE and deliberately not a
// separate TYPE: splitting the two halves into two types would let someone give them two pointers,
// which is the design error the scanner's design rules out.
public sealed partial class LexicalScanner
{
	/// <summary>
	/// Matches a literal's opening at a position and, where it matches, scans the whole literal.
	/// The opening - the literal prefix included where one is present - is
	/// <see cref="LexicalZone.Delimiter"/>, which counts as code.
	/// </summary>
	private int BeginALiteral(
		ScanContext context,
		CodeRegion region,
		LexicalRuleCandidate candidate,
		int position)
	{
		int prefixLength = candidate.Prefix?.Prefix.Length ?? 0;
		OpeningAttempt attempt = new()
		{
			Candidate = candidate,
			ConstructStart = position,
			OpeningStart = position + prefixLength,
			Depth = region.Depth
		};

		LiteralOccurrence? occurrence = PrefixStandsHere(context.Text, attempt)
			? MatchLiteralOpening(context, attempt)
			: null;

		int next = position;

		if (occurrence is not null)
		{
			context.Mark(position, occurrence.BodyStart - position, LexicalZone.Delimiter);
			next = ScanLiteralBody(context, occurrence);
		}

		return next;
	}

	/// <summary>
	/// A literal prefix carries its OWN case rule rather than the profile's delimiter rule, which
	/// is why the prefix and the opening are matched separately even though the index ordered
	/// them as one token.
	/// </summary>
	private static bool PrefixStandsHere(string text, OpeningAttempt attempt)
	{
		bool stands = attempt.Candidate.Prefix is not LiteralPrefixRule prefix
			|| MatchesAt(text, attempt.ConstructStart, prefix.Prefix, ComparisonFor(prefix.Sensitivity));

		return stands;
	}

	private LiteralOccurrence? MatchLiteralOpening(ScanContext context, OpeningAttempt attempt)
	{
		QuoteRule rule = attempt.Candidate.Quote!;

		LiteralOccurrence? occurrence = rule.CapturedTerminator is CapturedTerminator capturedTerminator
			? MatchACapturedTerminatorOpening(context, attempt, capturedTerminator)
			: MatchAConstantOpening(context, attempt);

		return occurrence;
	}

	private LiteralOccurrence? MatchAConstantOpening(ScanContext context, OpeningAttempt attempt)
	{
		QuoteRule rule = attempt.Candidate.Quote!;
		int openingLength = MatchOpeningLength(context.Text, rule, attempt.OpeningStart);
		LiteralOccurrence? occurrence = null;

		if (openingLength > 0 && OpeningGatesHold(context, attempt, attempt.OpeningStart + openingLength))
		{
			occurrence = new LiteralOccurrence
			{
				Rule = rule,
				OpeningPosition = attempt.ConstructStart,
				BodyStart = attempt.OpeningStart + openingLength,
				Closing = rule.Closing,
				MinimumClosingLength = MinimumClosingLengthFor(rule, openingLength),
				EscapeCharacter = EffectiveEscapeCharacter(rule, attempt.Candidate.Prefix),
				Interpolates = DeclaresAnInterpolationHole(rule),
				Depth = attempt.Depth
			};
		}

		return occurrence;
	}

	/// <summary>
	/// The closing run's required length. It is only ever the opening's ACTUAL matched length under
	/// <see cref="DelimiterLength.VariableWithAtLeastEqualClosing"/> - the raw literal, where the
	/// author's own run length is what the closing has to reach. Under
	/// <see cref="DelimiterLength.Fixed"/> the closing is a constant of the rule, independent of how
	/// long the opening happened to be, which matters wherever the two differ in length, as an
	/// opening prefix does.
	/// </summary>
	private static int MinimumClosingLengthFor(QuoteRule rule, int openingLength)
	{
		int minimumClosingLength = rule.DelimiterLength is DelimiterLength.VariableWithAtLeastEqualClosing
			? openingLength
			: rule.Closing.Length;

		return minimumClosingLength;
	}

	/// <summary>
	/// The captured terminator. The closing is not a constant of the rule but the token the
	/// author chose at the opening, and whether the body interpolates is decided PER OCCURRENCE
	/// by whether the non-interpolating capture group took part - which is why both are read off
	/// this match and neither off the rule.
	/// </summary>
	private LiteralOccurrence? MatchACapturedTerminatorOpening(
		ScanContext context,
		OpeningAttempt attempt,
		CapturedTerminator capturedTerminator)
	{
		QuoteRule rule = attempt.Candidate.Quote!;
		string text = context.Text;

		Match? opening = MatchesAt(text, attempt.OpeningStart, rule.Opening, _delimiterComparison)
			? MatchAnchored(context, capturedTerminator.OpeningExpression, attempt.OpeningStart)
			: null;

		string closing = opening is null
			? string.Empty
			: opening.Groups[capturedTerminator.TerminatorCaptureGroupName].Value;

		LiteralOccurrence? occurrence = null;

		if (opening is not null
			&& closing.Length > 0
			&& OpeningGatesHold(context, attempt, attempt.OpeningStart + opening.Length))
		{
			occurrence = BuildACapturedTerminatorOccurrence(attempt, capturedTerminator, opening, closing);
		}

		return occurrence;
	}

	private static LiteralOccurrence BuildACapturedTerminatorOccurrence(
		OpeningAttempt attempt,
		CapturedTerminator capturedTerminator,
		Match opening,
		string closing)
	{
		QuoteRule rule = attempt.Candidate.Quote!;
		bool thisOccurrenceIsQuoted =
			capturedTerminator.NonInterpolatingQuoteCaptureGroupName is string nonInterpolatingGroup
			&& opening.Groups[nonInterpolatingGroup].Success;

		return new LiteralOccurrence
		{
			Rule = rule,
			OpeningPosition = attempt.ConstructStart,
			BodyStart = attempt.OpeningStart + opening.Length,
			Closing = closing,
			MinimumClosingLength = closing.Length,
			EscapeCharacter = EffectiveEscapeCharacter(rule, attempt.Candidate.Prefix),
			Interpolates = DeclaresAnInterpolationHole(rule) && !thisOccurrenceIsQuoted,
			Depth = attempt.Depth
		};
	}

	/// <summary>
	/// The variable-length delimiter, on the opening side. The opening's ACTUAL matched
	/// length - the whole run, not the rule's minimum - is what the closing will have to reach,
	/// so it is captured here and carried on the occurrence.
	/// </summary>
	private int MatchOpeningLength(string text, QuoteRule rule, int position)
	{
		int length = 0;

		if (MatchesAt(text, position, rule.Opening, _delimiterComparison))
		{
			length = rule.DelimiterLength is DelimiterLength.VariableWithAtLeastEqualClosing
				? RunLength(text, position, rule.Opening[0])
				: rule.Opening.Length;

			length = length >= rule.Opening.Length ? length : 0;
		}

		return length;
	}

	/// <summary>
	/// Start-of-line anchoring is asked of the CONSTRUCT's start, prefix included, while
	/// end-of-line anchoring is asked past the end of the opening - which for a captured
	/// terminator is the end of the whole opening expression and not of its two-character token,
	/// or a here-document naming its terminator would never satisfy it.
	/// </summary>
	private static bool OpeningGatesHold(ScanContext context, OpeningAttempt attempt, int openingEnd)
	{
		QuoteRule rule = attempt.Candidate.Quote!;
		string text = context.Text;

		bool holds =
			SatisfiesAnchoring(text, attempt.ConstructStart, openingEnd, rule.OpeningAnchoring)
			&& (rule.OpeningContextPredicate is null || rule.OpeningContextPredicate(text, attempt.OpeningStart));

		return holds;
	}

	private static char? EffectiveEscapeCharacter(QuoteRule rule, LiteralPrefixRule? prefix)
	{
		char? escapeCharacter = rule.EscapeCharacter;

		if (prefix is not null)
		{
			escapeCharacter = prefix.EscapeHandling is EscapeHandling.Suppressed
				? null
				: prefix.EscapeCharacterOverride ?? escapeCharacter;
		}

		return escapeCharacter;
	}

	private static bool DeclaresAnInterpolationHole(QuoteRule rule)
	{
		bool declares =
			rule.InterpolationOpening is not null
			&& (rule.InterpolationClosing is not null || rule.InterpolationIdentifierExtent is not null);

		return declares;
	}

	/// <summary>
	/// The literal's body, from the end of its opening to whatever ends it. Reaching the end of
	/// the file with it still open is an unterminated construct: the mask stands up to the
	/// literal's OPENING position and is declared undefined after it.
	/// </summary>
	private int ScanLiteralBody(ScanContext context, LiteralOccurrence occurrence)
	{
		LiteralCursor cursor = new() { Position = occurrence.BodyStart };

		while (cursor.Position < context.Text.Length && !cursor.Closed && !context.IsAbandoned)
		{
			StepThroughTheBody(context, occurrence, cursor);
		}

		if (!cursor.Closed && !context.IsAbandoned)
		{
			context.Abandon(
				LexicalAnomalyReason.UnterminatedLiteral,
				occurrence.OpeningPosition,
				cursor.Position,
				LexicalZone.StringBody);
		}

		return cursor.Position;
	}

	/// <summary>
	/// ONE STEP, AND THE ORDER OF THESE SEVEN IS THE DESIGN.
	///
	/// A suspended sub-range comes first because while one is open the literal's own closing does
	/// not apply at all. The escape comes before the closing, or an escaped quote closes the
	/// literal. The DOUBLED closing comes before the plain one, or the second character of the
	/// pair closes it, and it inverts the mask's polarity for the rest of the line rather than merely
	/// mis-reading two
	/// characters. The closing comes before the sub-range opening, so a literal whose closing and
	/// whose sub-range opening share a character still closes. And the newline comes last, so a
	/// rule that admits none has already had every chance to end the literal properly.
	/// </summary>
	private void StepThroughTheBody(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		bool handled =
			TryStepInsideASuspendedSubRange(context, occurrence, cursor)
			|| TryConsumeAnEscape(context, occurrence, cursor)
			|| TryConsumeADoubledClosing(context, occurrence, cursor)
			|| TryCloseTheLiteral(context, occurrence, cursor)
			|| TryOpenASuspendedSubRange(context, occurrence, cursor)
			|| TryEnterAnInterpolationHole(context, occurrence, cursor)
			|| TryMeetARawNewline(context, occurrence, cursor);

		if (!handled)
		{
			context.Mark(cursor.Position, 1, LexicalZone.StringBody);
			cursor.Position++;
		}
	}

	/// <summary>
	/// THE SUSPENDED SUB-RANGE, AND THE DEFECT CLASS IT EXISTS FOR.
	///
	/// While one is open the literal's closing delimiter does not apply, which is what stops a
	/// character class inside a regular expression literal from ending the literal at its first
	/// slash. Get it wrong and the literal truncates there, every zone for the rest of the line
	/// inverts, and the construct the scan was looking for is invisible.
	///
	/// Inside the range only two things are still recognized: the escape character, because a
	/// character class escapes its own closing bracket, and the range's own closing. Neither an
	/// interpolation hole nor the literal's closing is looked for.
	/// </summary>
	private bool TryStepInsideASuspendedSubRange(
		ScanContext context,
		LiteralOccurrence occurrence,
		LiteralCursor cursor)
	{
		bool inside = cursor.Suspended;

		if (inside)
		{
			bool consumed =
				TryConsumeAnEscape(context, occurrence, cursor)
				|| TryCloseASuspendedSubRange(context, occurrence, cursor)
				|| TryMeetARawNewline(context, occurrence, cursor);

			if (!consumed)
			{
				context.Mark(cursor.Position, 1, LexicalZone.StringBody);
				cursor.Position++;
			}
		}

		return inside;
	}

	private bool TryOpenASuspendedSubRange(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		SuspendedSubRange? subRange = occurrence.Rule.SuspendedSubRange;
		bool opened =
			subRange is not null
			&& MatchesAt(context.Text, cursor.Position, subRange.Opening, _delimiterComparison);

		if (opened)
		{
			context.Mark(cursor.Position, subRange!.Opening.Length, LexicalZone.StringBody);
			cursor.Position += subRange.Opening.Length;
			cursor.Suspended = true;
		}

		return opened;
	}

	private bool TryCloseASuspendedSubRange(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		SuspendedSubRange subRange = occurrence.Rule.SuspendedSubRange!;
		bool closed = MatchesAt(context.Text, cursor.Position, subRange.Closing, _delimiterComparison);

		if (closed)
		{
			context.Mark(cursor.Position, subRange.Closing.Length, LexicalZone.StringBody);
			cursor.Position += subRange.Closing.Length;
			cursor.Suspended = false;
		}

		return closed;
	}

	private static bool TryConsumeAnEscape(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		string text = context.Text;
		bool consumed = occurrence.EscapeCharacter is char escapeCharacter && text[cursor.Position] == escapeCharacter;

		if (consumed)
		{
			int length = EscapeLength(text, occurrence, cursor.Position);

			context.Mark(cursor.Position, length, LexicalZone.StringBody);
			cursor.Position += length;
		}

		return consumed;
	}

	/// <summary>
	/// The three-valued newline policy, on the escape side, and the reason the field is not a
	/// boolean. Under <see cref="NewlinePolicy.EscapedContinues"/> the escape swallows the line
	/// feed and the literal continues; under <see cref="NewlinePolicy.Forbidden"/> it does not,
	/// and the line feed is left for the raw-newline step to end the literal on. A boolean
	/// conflates the two, and resolving the ambiguity the other way makes the literal's closing
	/// quote read as an opening one - the same polarity inversion, reached by a different route.
	/// </summary>
	private static int EscapeLength(string text, LiteralOccurrence occurrence, int position)
	{
		int following = position + 1;
		bool escapesWhatFollows =
			following < text.Length
			&& (text[following] != _LINE_FEED
				|| occurrence.Rule.NewlinePolicy is NewlinePolicy.EscapedContinues or NewlinePolicy.Allowed);

		return escapesWhatFollows ? 2 : 1;
	}

	/// <summary>
	/// The doubled close, and THE PAIR IS CONSUMED WHOLE. Consuming one character instead of
	/// two would let the second character close the literal and invert the mask for the rest of
	/// the line.
	/// </summary>
	private bool TryConsumeADoubledClosing(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		string text = context.Text;
		int length = occurrence.Closing.Length;
		bool consumed =
			occurrence.Rule.DoubledClosingEscapes
			&& length > 0
			&& MatchesAt(text, cursor.Position, occurrence.Closing, _delimiterComparison)
			&& MatchesAt(text, cursor.Position + length, occurrence.Closing, _delimiterComparison);

		if (consumed)
		{
			context.Mark(cursor.Position, length * 2, LexicalZone.StringBody);
			cursor.Position += length * 2;
		}

		return consumed;
	}

	private bool TryCloseTheLiteral(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		int length = MatchClosingLength(context, occurrence, cursor.Position);
		bool closed = length > 0;

		if (closed)
		{
			context.Mark(cursor.Position, length, LexicalZone.Delimiter);
			cursor.Position += length;
			cursor.Closed = true;
		}

		return closed;
	}

	/// <summary>
	/// The closing side of the variable-length delimiter: the run has to reach the length the
	/// OPENING matched, not the length the rule declares. A shorter run is body text, as in a raw
	/// literal containing a shorter quote run.
	/// </summary>
	private int MatchClosingLength(ScanContext context, LiteralOccurrence occurrence, int position)
	{
		string text = context.Text;
		int length = 0;

		if (MatchesAt(text, position, occurrence.Closing, _delimiterComparison))
		{
			length = occurrence.Rule.DelimiterLength is DelimiterLength.VariableWithAtLeastEqualClosing
				? RunLength(text, position, occurrence.Closing[0])
				: occurrence.Closing.Length;

			length = length >= occurrence.MinimumClosingLength ? length : 0;
		}

		if (length > 0
			&& !SatisfiesAnchoring(text, position, position + length, occurrence.Rule.ClosingAnchoring))
		{
			length = 0;
		}

		return length;
	}

	/// <summary>
	/// A raw line feed inside a literal. Under <see cref="NewlinePolicy.Allowed"/> it is content;
	/// otherwise it "ends the literal and is an anomaly", and both halves happen here.
	/// The line feed itself is left as a code position and the cursor does not move past it, so
	/// the code scan resumes exactly there - the mask after the recovery is as sound as the mask
	/// before it, which is why this anomaly does not abandon the file.
	/// </summary>
	private static bool TryMeetARawNewline(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		bool met = context.Text[cursor.Position] == _LINE_FEED;

		if (met && occurrence.Rule.NewlinePolicy is NewlinePolicy.Allowed)
		{
			context.Mark(cursor.Position, 1, LexicalZone.StringBody);
			cursor.Position++;
		}
		else if (met)
		{
			context.RecordANewlineTerminatedLiteral();
			cursor.Closed = true;
		}

		return met;
	}

	/// <summary>
	/// THE POINT WHERE THE LITERAL HALF CALLS BACK INTO THE CODE HALF. An
	/// interpolation's interior is code, and the two hole kinds differ only in what bounds that
	/// interior - a closing token found by the same recursive scan, or the extent of a declared
	/// identifier expression.
	/// </summary>
	private bool TryEnterAnInterpolationHole(
		ScanContext context,
		LiteralOccurrence occurrence,
		LiteralCursor cursor)
	{
		QuoteRule rule = occurrence.Rule;
		bool atAHole =
			occurrence.Interpolates
			&& rule.InterpolationOpening is string opening
			&& MatchesAt(context.Text, cursor.Position, opening, _delimiterComparison);

		bool entered = atAHole
			&& (rule.InterpolationClosing is not null
				? EnterABracketedHole(context, occurrence, cursor)
				: EnterAnIdentifierExtentHole(context, occurrence, cursor));

		return entered;
	}

	/// <summary>
	/// The bracketed kind. Its interior is scanned as code by the very method that scans the
	/// file, so a nested literal inside the hole is entered as a literal and ITS closing brace is
	/// body text - which is the whole reason the hole's end cannot be found by counting braces.
	/// A hole that never closes means the literal that opened it never closes either.
	/// </summary>
	private bool EnterABracketedHole(ScanContext context, LiteralOccurrence occurrence, LiteralCursor cursor)
	{
		int openingLength = occurrence.Rule.InterpolationOpening!.Length;
		int holeDepth = occurrence.Depth + 1;

		if (!RefusesToDescend(context, holeDepth, cursor.Position))
		{
			context.Mark(cursor.Position, openingLength, LexicalZone.Delimiter);

			RegionResult hole = ScanABracketedHoleInterior(context, occurrence, cursor.Position + openingLength);

			AbandonAnUnclosedHole(context, occurrence, cursor, hole);
			cursor.Position = hole.Position;
		}

		return true;
	}

	/// <summary>
	/// The recovery policy for a nested hole. A hole that counted a nested opener and never closed
	/// was misread: an unbalanced opener kept the closing from reaching depth zero, and everything
	/// after it was read as the hole's code. The scan state is rewound to the hole's start and the hole
	/// is read again under the first-closing rule, recording one anomaly. The outer literal's own
	/// closing cannot be told from a nested literal's opening at that point, so the test is whether
	/// the hole closed at all. A hole that never counted a nested opener reads identically either way
	/// and keeps the unterminated-construct policy.
	/// </summary>
	private RegionResult ScanABracketedHoleInterior(ScanContext context, LiteralOccurrence occurrence, int interiorStart)
	{
		ScanSnapshot snapshot = context.TakeSnapshot();
		RegionResult hole = ScanTheHoleInterior(context, occurrence, interiorStart);

		if (!hole.ClosedItsConstruct && hole.CountedANestedOpening && context.CanRecoverFromNesting)
		{
			context.RewindForNestingRecovery(snapshot, interiorStart);
			hole = ScanTheHoleInterior(context, occurrence, interiorStart);
			context.ResumeNesting();
		}

		return hole;
	}

	private RegionResult ScanTheHoleInterior(ScanContext context, LiteralOccurrence occurrence, int interiorStart)
	{
		return ScanCodeRegion(context, new CodeRegion
		{
			Start = interiorStart,
			EndExclusive = context.Text.Length,
			Depth = occurrence.Depth + 1,
			IsOutermost = false,
			EnclosingHole = occurrence.Rule
		});
	}

	private static void AbandonAnUnclosedHole(
		ScanContext context,
		LiteralOccurrence occurrence,
		LiteralCursor cursor,
		RegionResult hole)
	{
		if (!hole.ClosedItsConstruct && !context.IsAbandoned)
		{
			context.Abandon(
				LexicalAnomalyReason.UnterminatedLiteral,
				occurrence.OpeningPosition,
				cursor.Position,
				LexicalZone.StringBody);
		}
	}

	/// <summary>
	/// The identifier-extent kind: a sigil, an identifier, and NO CLOSING TOKEN. It composes with
	/// the same descent rather than bypassing it - the identifier is handed to
	/// <see cref="ScanCodeRegion"/> as a region bounded by its own extent, so it is code for the
	/// same reason a bracketed hole's interior is, it costs the same depth against the same
	/// bound, and there is no second algorithm to keep in step with the first.
	///
	/// WHERE THE EXPRESSION DOES NOT MATCH THE SIGIL IS NOT A HOLE. It is ordinary literal text -
	/// a currency sign before a space, a sigil before a digit - and returning false here lets the
	/// body's default step mark it as such rather than opening a hole over nothing.
	/// </summary>
	private bool EnterAnIdentifierExtentHole(
		ScanContext context,
		LiteralOccurrence occurrence,
		LiteralCursor cursor)
	{
		QuoteRule rule = occurrence.Rule;
		int openingLength = rule.InterpolationOpening!.Length;
		int identifierStart = cursor.Position + openingLength;
		Match? identifier = MatchAnchored(context, rule.InterpolationIdentifierExtent!, identifierStart);
		bool entered = identifier is not null && identifier.Length > 0;

		if (entered && !RefusesToDescend(context, occurrence.Depth + 1, cursor.Position))
		{
			context.Mark(cursor.Position, openingLength, LexicalZone.Delimiter);

			RegionResult hole = ScanCodeRegion(context, new CodeRegion
			{
				Start = identifierStart,
				EndExclusive = identifierStart + identifier!.Length,
				Depth = occurrence.Depth + 1,
				IsOutermost = false
			});

			cursor.Position = hole.Position;
		}

		return entered;
	}

	/// <summary>One candidate opening being tried at one position.</summary>
	private readonly record struct OpeningAttempt
	{
		public required LexicalRuleCandidate Candidate { get; init; }

		/// <summary>Where the construct starts, which is the literal prefix where there is one.</summary>
		public required int ConstructStart { get; init; }

		/// <summary>Where the quote rule's own opening starts, past any prefix.</summary>
		public required int OpeningStart { get; init; }

		/// <summary>Interpolation holes already open around this literal.</summary>
		public required int Depth { get; init; }
	}

	/// <summary>
	/// ONE OCCURRENCE of a quote rule, which is not the same thing as the rule. Four of these six
	/// fields are decided per occurrence rather than per rule - the closing where it was captured
	/// at the opening, the minimum closing length where the opening was variable, the escape
	/// character where a prefix suppressed or replaced it, and whether the body interpolates
	/// where the opening's quoting decided that.
	/// </summary>
	private sealed record LiteralOccurrence
	{
		public required QuoteRule Rule { get; init; }

		/// <summary>Where the construct began, prefix included. It is the boundary an unterminated literal declares.</summary>
		public required int OpeningPosition { get; init; }

		public required int BodyStart { get; init; }

		public required string Closing { get; init; }

		public required int MinimumClosingLength { get; init; }

		public required char? EscapeCharacter { get; init; }

		public required bool Interpolates { get; init; }

		public required int Depth { get; init; }
	}

	/// <summary>Where the body scan has reached, and the two things that end it.</summary>
	private sealed class LiteralCursor
	{
		public required int Position { get; set; }

		/// <summary>Whether a suspended sub-range is currently open.</summary>
		public bool Suspended { get; set; }

		public bool Closed { get; set; }
	}
}
