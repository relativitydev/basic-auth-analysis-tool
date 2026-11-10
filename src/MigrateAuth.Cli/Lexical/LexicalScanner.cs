using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// THE LEXICAL SCANNER. ONE WALK OVER THE DECODED TEXT, ONE ZONE PER POSITION, DRIVEN ENTIRELY BY A
/// LEXICAL PROFILE. THERE IS NO SCANNER PER LANGUAGE AND NOTHING HERE NAMES ONE.
///
/// WHAT IT DOES NOT DO. No anchor is tested, no symbol is resolved,
/// no detection rule is applied and nothing is classified. The output is the mask and the signals
/// the mask-building discovered, and not one statement about what a zone MEANS to a catalog rule.
///
/// AND IT NEVER FILTERS. Dropping every match outside a code zone at the point of scanning would
/// empty the rubric's entire Low population and leave the Low level unreachable. The mask is built
/// accurately for every position, INCLUDING inside comments and literal bodies, and what
/// classification later does with a demoting zone - demote, never discard - is not pre-empted by
/// refusing to mask there.
///
/// SCANNING A LITERAL AND SCANNING AN INTERPOLATION HOLE ARE THE SAME PASS OVER ONE POINTER.
/// <see cref="ScanCodeRegion"/> and <see cref="ScanLiteralBody"/> call each other. They have to:
/// finding where a hole ends already requires lexical knowledge, because a closing brace
/// belonging to a nested string inside the hole does not close the hole, and a brace counter
/// closes early and inverts the mask's polarity for the rest of the line.
///
/// THE RECURSION IS BOUNDED BY AN EXPLICIT COUNTER CHECKED BEFORE EACH DESCENT, NEVER BY THE CALL
/// STACK. A stack overflow on this platform kills the process with no defined exit code and no
/// diagnostics, so the bound cannot be discovered by reaching it. See
/// <see cref="RefusesToDescend"/> for the counter and for why bounding holes bounds everything.
///
/// IT IS CONSTRUCTED ONCE PER PROFILE AND REUSED. The only state it holds is the rule index,
/// which is derived from the profile and is immutable; every scan's mutable state lives in a
/// <see cref="ScanContext"/> created for that one file, so the type is pure with respect to the
/// file.
/// </summary>
public sealed partial class LexicalScanner
{
	private const char _LINE_FEED = '\n';
	private const char _WORD_CONNECTOR_CHARACTER = '_';

	private readonly LexicalRuleIndex _index;
	private readonly StringComparison _delimiterComparison;
	private readonly StringComparison _keywordComparison;
	private readonly LexicalBudgetWatch _budgetWatch;

	public LexicalScanner(LexicalProfile profile)
		: this(profile, TimeProvider.System)
	{
	}

	/// <summary>
	/// The clock is an input so that the lexical budget is testable without waiting, on the
	/// same terms as the recognition engine's own.
	/// </summary>
	public LexicalScanner(LexicalProfile profile, TimeProvider timeProvider)
	{
		ArgumentNullException.ThrowIfNull(profile);
		ArgumentNullException.ThrowIfNull(timeProvider);

		Profile = profile;
		_index = new LexicalRuleIndex(profile);
		_delimiterComparison = ComparisonFor(profile.DelimiterCaseSensitivity);
		_keywordComparison = ComparisonFor(profile.KeywordCaseSensitivity);
		_budgetWatch = new LexicalBudgetWatch(timeProvider);
	}

	/// <summary>The profile this scanner was parameterized by.</summary>
	public LexicalProfile Profile { get; }

	/// <summary>
	/// Builds the zone mask for one file's decoded, normalized text.
	///
	/// THE TEXT MUST ALREADY BE NORMALIZED - <see cref="Reading.TextNormalization"/> owns that and
	/// this does not repeat it. Every line-position question below asks about a line feed and
	/// nothing else, so a carriage return that reached here would read as an ordinary character
	/// and every anchored delimiter in the file would silently stop matching.
	/// </summary>
	public ZoneMask Scan(string normalizedText)
	{
		ArgumentNullException.ThrowIfNull(normalizedText);

		ScanContext context = new(normalizedText);

		_budgetWatch.BeginFile();

		_ = ScanCodeRegion(context, new CodeRegion
		{
			Start = 0,
			EndExclusive = normalizedText.Length,
			Depth = 0,
			IsOutermost = true
		});

		return context.ToMask();
	}

	private static StringComparison ComparisonFor(CaseSensitivity sensitivity)
	{
		return sensitivity is CaseSensitivity.Insensitive
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
	}

	/// <summary>
	/// THE CODE HALF OF THE MUTUAL RECURSION. It walks a region as executable code and is the
	/// same method for the whole file, for the inside of a bracketed interpolation hole, and for
	/// the identifier that IS an identifier-extent hole. The three differ only in the region
	/// handed to them, which is the property that makes both hole kinds compose with one descent
	/// rather than with one algorithm each.
	/// </summary>
	private RegionResult ScanCodeRegion(ScanContext context, CodeRegion region)
	{
		int position = region.Start;
		bool closed = false;

		while (position < region.EndExclusive && !closed && !context.IsAbandoned)
		{
			int holeClosingLength = MatchHoleClosingLength(context, region, position);

			if (holeClosingLength > 0)
			{
				context.Mark(position, holeClosingLength, LexicalZone.Delimiter);
				position += holeClosingLength;
				closed = true;
			}
			else
			{
				position = BeginAConstructOrAdvance(context, region, position);
			}
		}

		return new RegionResult { Position = position, ClosedItsConstruct = closed };
	}

	/// <summary>
	/// The bracketed hole's closing token, and ONLY where this region is that hole's interior. A
	/// nested literal inside the hole is scanned by <see cref="ScanLiteralBody"/>, which never
	/// consults this - which is precisely why a closing brace inside that nested literal cannot
	/// end the hole.
	/// </summary>
	private int MatchHoleClosingLength(ScanContext context, CodeRegion region, int position)
	{
		int length = 0;

		if (region.EnclosingHole is QuoteRule hole
			&& hole.InterpolationClosing is string closing
			&& MatchesAt(context.Text, position, closing, _delimiterComparison))
		{
			length = closing.Length;
		}

		return length;
	}

	/// <summary>
	/// Tries everything that can open at this character, longest opening first, and takes the
	/// first whose gates also hold. A position no construct claimed stays
	/// <see cref="LexicalZone.Code"/>, which is the mask's zero value and therefore costs no
	/// write at all - the overwhelmingly common case in any real file.
	/// </summary>
	private int BeginAConstructOrAdvance(ScanContext context, CodeRegion region, int position)
	{
		if (region.IsOutermost)
		{
			context.OutermostConstructOpening = position;
		}

		int next = position;

		foreach (LexicalRuleCandidate candidate in _index.CandidatesFor(context.Text[position]))
		{
			next = BeginAConstruct(context, region, candidate, position);

			if (next > position || context.IsAbandoned)
			{
				break;
			}
		}

		return next > position ? next : position + 1;
	}

	private int BeginAConstruct(
		ScanContext context,
		CodeRegion region,
		LexicalRuleCandidate candidate,
		int position)
	{
		int next = candidate.Kind switch
		{
			LexicalRuleKind.LineComment => BeginALineComment(context, candidate.LineComment!, position),
			LexicalRuleKind.BlockComment => BeginABlockComment(context, candidate.BlockComment!, position),
			_ => BeginALiteral(context, region, candidate, position)
		};

		return next;
	}

	/// <summary>
	/// A line comment runs to the line feed and does not include it: the line feed is a code
	/// position, so a construct on the next line is not swallowed. Reaching the end of the file
	/// inside one is NOT unterminated - the end of the file ends the line.
	/// </summary>
	private int BeginALineComment(ScanContext context, LineCommentRule rule, int position)
	{
		string text = context.Text;
		StringComparison comparison = rule.RequiresWordBoundary ? _keywordComparison : _delimiterComparison;
		int openingEnd = position + rule.Opening.Length;
		int next = position;

		bool opens =
			MatchesAt(text, position, rule.Opening, comparison)
			&& SatisfiesAnchoring(text, position, openingEnd, rule.OpeningAnchoring)
			&& (!rule.RequiresWordBoundary || StandsAsAWholeWord(text, position, openingEnd))
			&& (rule.OpeningContextPredicate is null || rule.OpeningContextPredicate(text, position));

		if (opens)
		{
			next = EndOfLineFrom(text, position);
			context.Mark(position, next - position, LexicalZone.Comment);
		}

		return next;
	}

	private int BeginABlockComment(ScanContext context, BlockCommentRule rule, int position)
	{
		string text = context.Text;
		int openingEnd = position + rule.Opening.Length;
		int next = position;

		bool opens =
			MatchesAt(text, position, rule.Opening, _delimiterComparison)
			&& SatisfiesAnchoring(text, position, openingEnd, rule.OpeningAnchoring)
			&& (rule.OpeningContextPredicate is null || rule.OpeningContextPredicate(text, position));

		if (opens)
		{
			next = CloseABlockComment(context, rule, position, openingEnd);
		}

		return next;
	}

	/// <summary>
	/// The unterminated-construct policy, on the comment side. The mask established before
	/// the opening stands; from the opening onward it is declared untrustworthy rather than
	/// silently masked, because silently masking the remainder suppresses findings
	/// deterministically with nothing declaring that it did.
	/// </summary>
	private int CloseABlockComment(ScanContext context, BlockCommentRule rule, int position, int openingEnd)
	{
		int closingStart = FindBlockCommentClosing(context, rule, openingEnd);
		int next;

		if (closingStart < 0)
		{
			context.Abandon(LexicalAnomalyReason.UnterminatedBlockComment, position, position, LexicalZone.Comment);
			next = context.Text.Length;
		}
		else
		{
			next = closingStart + rule.Closing.Length;
			context.Mark(position, next - position, LexicalZone.Comment);
		}

		return next;
	}

	private int FindBlockCommentClosing(ScanContext context, BlockCommentRule rule, int searchFrom)
	{
		string text = context.Text;
		int found = -1;

		for (int position = searchFrom; position + rule.Closing.Length <= text.Length; position++)
		{
			if (MatchesAt(text, position, rule.Closing, _delimiterComparison)
				&& SatisfiesAnchoring(text, position, position + rule.Closing.Length, rule.ClosingAnchoring))
			{
				found = position;
				break;
			}
		}

		return found;
	}

	/// <summary>
	/// THE RECURSION BOUND, CHECKED BEFORE THE DESCENT AND NEVER DISCOVERED BY MAKING IT.
	///
	/// The counter counts INTERPOLATION HOLES currently open. Bounding holes
	/// bounds the whole recursion, and the argument is structural rather than empirical: a
	/// literal is only ever entered from <see cref="ScanCodeRegion"/>, and a code region deeper
	/// than the file itself is only ever entered from a hole. Literal frames are therefore always
	/// separated by hole frames, so the deepest possible C# call chain is two frames per level
	/// plus the outermost - seventeen at the bound of eight, against a stack sized in thousands.
	///
	/// Exceeding it is a <see cref="LexicalAnomalyReason.InterpolationDepthExceeded"/> anomaly and
	/// never a process failure. The boundary it declares is the OUTERMOST construct's opening,
	/// not the refusal point, because everything from that opening onward was being read under an
	/// assumption the scanner has just abandoned.
	/// </summary>
	private static bool RefusesToDescend(ScanContext context, int holeDepth, int position)
	{
		bool refuses = holeDepth > LexicalEnginePolicy.MAXIMUM_INTERPOLATION_RECURSION_DEPTH;

		if (refuses)
		{
			context.Abandon(
				LexicalAnomalyReason.InterpolationDepthExceeded,
				context.OutermostConstructOpening,
				position,
				LexicalZone.StringBody);
		}

		return refuses;
	}

	private static bool MatchesAt(string text, int position, string token, StringComparison comparison)
	{
		bool matches =
			token.Length > 0
			&& position >= 0
			&& position + token.Length <= text.Length
			&& text.AsSpan(position, token.Length).Equals(token, comparison);

		return matches;
	}

	/// <summary>
	/// How far a run of one repeated character extends. It is what
	/// <see cref="DelimiterLength.VariableWithAtLeastEqualClosing"/> measures, and the comparison
	/// is ordinal.
	/// </summary>
	private static int RunLength(string text, int position, char character)
	{
		int length = 0;

		while (position + length < text.Length && text[position + length] == character)
		{
			length++;
		}

		return length;
	}

	/// <summary>
	/// Positional anchoring, checked against the ALREADY-NORMALIZED text. Start of line
	/// means column zero and not "the first token on the line".
	/// </summary>
	private static bool SatisfiesAnchoring(string text, int start, int endExclusive, PositionalAnchoring anchoring)
	{
		bool satisfied = anchoring switch
		{
			PositionalAnchoring.StartOfLine => start == 0 || text[start - 1] == _LINE_FEED,
			PositionalAnchoring.EndOfLine => endExclusive >= text.Length || text[endExclusive] == _LINE_FEED,
			_ => true
		};

		return satisfied;
	}

	/// <summary>
	/// The positional form of the context predicate: an opener that is a word opens a comment
	/// only where it stands as a whole word, or an identifier that merely begins with those
	/// letters swallows the rest of the line.
	/// </summary>
	private static bool StandsAsAWholeWord(string text, int start, int endExclusive)
	{
		bool standsAlone =
			(start == 0 || !IsWordCharacter(text[start - 1]))
			&& (endExclusive >= text.Length || !IsWordCharacter(text[endExclusive]));

		return standsAlone;
	}

	private static bool IsWordCharacter(char character)
	{
		return char.IsLetterOrDigit(character) || character == _WORD_CONNECTOR_CHARACTER;
	}

	private static int EndOfLineFrom(string text, int position)
	{
		int lineFeed = text.IndexOf(_LINE_FEED, position);

		return lineFeed < 0 ? text.Length : lineFeed;
	}

	/// <summary>
	/// An expression required to match AT a position rather than anywhere after it, searched
	/// within the current line only.
	///
	/// THE WINDOW IS THE POINT. Both expressions that reach here - a here-document's opening and
	/// an identifier-extent hole's identifier - are line-local by construction in every language
	/// that has them, and without the window a failed match at one candidate position would scan
	/// forward through the whole remaining file. In a 2 MB file whose content happens to start
	/// with the candidate's first character that is quadratic.
	///
	/// THE WINDOW BOUNDS HOW MUCH TEXT A STUCK EXPRESSION COULD WALK, NOT WHETHER WALKING IT
	/// TERMINATES, so the search is charged to the file's lexical budget. Where the budget is spent,
	/// or the expression exceeds its own timeout, the scan is abandoned here: the mask stays sound up
	/// to the opening of the outermost construct being read, exactly as for the recursion bound.
	/// </summary>
	private Match? MatchAnchored(ScanContext context, Regex expression, int position)
	{
		string text = context.Text;
		int windowEnd = EndOfLineFrom(text, position);
		Match? anchored = null;

		if (position < windowEnd)
		{
			Match found = _budgetWatch.MatchWithin(expression, text, position, windowEnd - position);
			anchored = found.Success && found.Index == position ? found : null;

			if (_budgetWatch.HasBeenExceeded)
			{
				anchored = null;
				context.Abandon(
					LexicalAnomalyReason.LexicalBudgetExceeded,
					context.OutermostConstructOpening,
					position,
					LexicalZone.StringBody);
			}
		}

		return anchored;
	}

	/// <summary>One region handed to <see cref="ScanCodeRegion"/>, and what it is a region of.</summary>
	private readonly record struct CodeRegion
	{
		public required int Start { get; init; }

		public required int EndExclusive { get; init; }

		/// <summary>Interpolation holes currently open, which is what the bound counts.</summary>
		public required int Depth { get; init; }

		/// <summary>Whether this is the file's own top-level code, rather than a hole's interior.</summary>
		public required bool IsOutermost { get; init; }

		/// <summary>
		/// The quote rule whose bracketed hole this region is the interior of, or null for the
		/// file's top level and for an identifier-extent hole - which has no closing token to
		/// look for, its extent being the region's own end.
		/// </summary>
		public QuoteRule? EnclosingHole { get; init; }
	}

	/// <summary>Where a region stopped, and whether it stopped because its construct closed.</summary>
	private readonly record struct RegionResult
	{
		public required int Position { get; init; }

		public required bool ClosedItsConstruct { get; init; }
	}

	/// <summary>
	/// EVERY SCAN'S MUTABLE STATE, FOR ONE FILE, AND NOWHERE ELSE. The scanner itself holds only
	/// the immutable rule index, so two files never share a byte of state - which is what makes
	/// per-file analysis pure with respect to the file.
	/// </summary>
	private sealed class ScanContext
	{
		internal ScanContext(string text)
		{
			Text = text;
			Zones = new LexicalZone[text.Length];
			ValidThroughPosition = text.Length;
		}

		internal string Text { get; }

		internal LexicalZone[] Zones { get; }

		internal int ValidThroughPosition { get; private set; }

		internal bool IsAbandoned { get; private set; }

		/// <summary>
		/// The last top-level position the scan examined, which during any descent is the opening
		/// of the outermost construct currently being read - the top-level loop does not advance
		/// again until that construct is finished.
		/// </summary>
		internal int OutermostConstructOpening { get; set; }

		private LexicalAnomalyReason AbandonedFor { get; set; } = LexicalAnomalyReason.None;

		private int NewlineTerminatedLiteralCount { get; set; }

		internal void Mark(int start, int length, LexicalZone zone)
		{
			int from = Math.Clamp(start, 0, Zones.Length);
			int count = Math.Clamp(length, 0, Zones.Length - from);

			Array.Fill(Zones, zone, from, count);
		}

		/// <summary>
		/// A raw newline inside a literal that does not admit one. It ends the literal and
		/// is an anomaly, and the recovery is sound - the scanner resumes at a line boundary it
		/// knows the position of - so this records the anomaly WITHOUT abandoning the file.
		/// </summary>
		internal void RecordANewlineTerminatedLiteral()
		{
			NewlineTerminatedLiteralCount++;
		}

		/// <summary>
		/// Stops the scan, declares the boundary the mask is sound up to, and fills what is left
		/// in the demoting direction so that every position still carries exactly one zone. The
		/// first abandonment wins: a second one discovered while unwinding would move the boundary
		/// forward, which is the one direction it must never move.
		/// </summary>
		internal void Abandon(LexicalAnomalyReason reason, int boundaryPosition, int fillFrom, LexicalZone fillZone)
		{
			if (IsAbandoned)
			{
				return;
			}

			IsAbandoned = true;
			AbandonedFor = reason;
			ValidThroughPosition = Math.Clamp(boundaryPosition, 0, Zones.Length);

			Mark(fillFrom, Zones.Length - fillFrom, fillZone);
		}

		internal ZoneMask ToMask()
		{
			LexicalAnomalyReason reason = IsAbandoned
				? AbandonedFor
				: NewlineTerminatedLiteralCount > 0
					? LexicalAnomalyReason.NewlineInsideALiteral
					: LexicalAnomalyReason.None;

			return new ZoneMask(Zones, ValidThroughPosition, reason, NewlineTerminatedLiteralCount);
		}
	}
}
