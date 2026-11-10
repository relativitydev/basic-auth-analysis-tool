using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// THE SCANNER. ONE WALK OVER THE DECODED TEXT, ONE ZONE PER POSITION, DRIVEN ENTIRELY BY A
/// PROFILE. THERE IS NO SCANNER PER LANGUAGE AND NOTHING HERE NAMES ONE.
///
/// WHAT IT DOES NOT DO. No anchor is tested, no symbol is resolved, no detection rule is applied
/// and nothing is classified. The output is the mask and the signals the mask-building discovered,
/// and not one statement about what a zone MEANS to a catalog rule.
///
/// AND IT NEVER FILTERS. The mask is built accurately for every position, INCLUDING inside comments
/// and literal bodies, and what the classifier later does with a demoting zone - demote, never
/// discard - is not pre-empted by refusing to mask there.
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
/// IT IS CONSTRUCTED ONCE PER PROFILE AND REUSED. The state it holds is derived from the profile and
/// is immutable (the rule index and the declarations the walk consults), except the lexical budget
/// watch, which is reset at the start of every file's scan; every scan's other mutable state lives in a
/// <see cref="ScanContext"/> created for that one file, so a scan is pure with respect to the file, as
/// per-file analysis requires, and one scanner is not shared by two scans running at once.
/// </summary>
public sealed partial class LexicalScanner
{
	private static readonly LiteralOccurrence[] _emptyPendingBodies = [];

	private const char _LINE_FEED = '\n';
	private const int _SINGLE_CHARACTER_LENGTH = 1;
	private const char _SPACE = ' ';
	private const char _TAB = '\t';
	private const char _WORD_CONNECTOR_CHARACTER = '_';
	private const char _ESCAPING_BACKSLASH = '\\';
	private const int _BACKSLASH_PAIR_LENGTH = 2;
	private const int _ESCAPE_PAIR_LENGTH = 2;
	private const int _DOUBLED_CLOSING_FACTOR = 2;
	private const int _NO_LOGICAL_LINE = -1;

	/// <summary>
	/// The lower bound on the length of an opening a deferred here-document rule can match, in
	/// characters: the two-character <c>&lt;&lt;</c> delimiter, with an EMPTY captured terminator, which
	/// the two whole-line closings admit. Nothing shorter is an opening, so this is the divisor of
	/// <see cref="MAXIMUM_PENDING_BODIES"/>.
	/// </summary>
	public const int SHORTEST_DEFERRED_OPENING_LENGTH_IN_CHARACTERS = 2;

	/// <summary>
	/// THE QUEUE'S CAP, a count of pending entries. Each pending queue (the file's own, and one per
	/// interpolation hole region) holds at most this many entries, and an opening is refused (it stays
	/// code) once its region's queue already holds that many. The value is
	/// <see cref="ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS"/> divided by
	/// <see cref="SHORTEST_DEFERRED_OPENING_LENGTH_IN_CHARACTERS"/>, the most openings one line could
	/// hold. It is NOT a proof that every admitted input stays under the cap by line geometry: a
	/// backslash-newline continues the command, so queued openings can sit on more than one
	/// physical line, and the cap is what enforces the bound.
	/// </summary>
	public const int MAXIMUM_PENDING_BODIES =
		ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS / SHORTEST_DEFERRED_OPENING_LENGTH_IN_CHARACTERS;

	private readonly LexicalRuleIndex _index;
	private readonly StringComparison _delimiterComparison;
	private readonly StringComparison _keywordComparison;
	private readonly LexicalBudgetWatch _budgetWatch;
	private readonly ArithmeticContextRule? _arithmeticContext;
	private readonly char? _codeEscape;
	private readonly QuoteRule? _substitutionRule;
	private readonly ArmBlockRule? _armBlock;
	private readonly SkippedQuote[] _skippedQuotes;
	private readonly ImmutableArray<LineCommentRule> _lineCommentRules;
	private readonly bool _tracksRegionState;

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
		_arithmeticContext = profile.ArithmeticContext;
		_codeEscape = profile.CodeEscapeCharacter;
		_substitutionRule = FindTheSubstitutionRule(profile);
		_armBlock = FindTheArmBlock(profile);
		_skippedQuotes = FindTheSkippedQuotes(profile);
		_lineCommentRules = profile.LineCommentRulesInEngineOrder;
		_tracksRegionState = _arithmeticContext is not null || _armBlock is not null || DeclaresADeferredBodyStart(profile);
		_executedDeclaration = profile.ExecutedStrings;
		_executedReader = BuildTheExecutedReader(profile, _keywordComparison);
		_executedBodies = BuildTheExecutedBodies(profile);
		_inlineContexts = profile.ExecutedStrings?.InlineContexts ?? [];
	}

	/// <summary>The profile this scanner was parameterized by.</summary>
	public LexicalProfile Profile { get; }

	/// <summary>
	/// What the last <see cref="Scan"/> was charged to the lexical budget, split into the expression searches and the arithmetic
	/// read-aheads. Observability for the timing gate and nothing else: no decision reads it.
	/// </summary>
	public LexicalCharge ChargeOfTheLastScan => _budgetWatch.Charged;

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

		context.AbandonIfBodiesAreStillPending();

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
	private RegionResult ScanCodeRegion(ScanContext context, CodeRegion declared)
	{
		CodeRegion region = _tracksRegionState ? declared with { State = StartTheRegionState(context, declared) } : declared;
		CodeWalk walk = StartTheWalk(context, region);

		while (walk.Position < region.EndExclusive && !walk.Closed && !context.IsAbandoned)
		{
			StepThroughCode(context, region, ref walk);
		}

		return new RegionResult
		{
			Position = walk.Position,
			ClosedItsConstruct = walk.Closed,
			CountedANestedOpening = walk.CountedANestedOpening,
			LeftOverBodies = walk.Closed && !region.IsOutermost && region.State?.HasPendingBodies is true
				? region.State.DequeueEveryPendingBody()
				: null
		};
	}

	private static CodeWalk StartTheWalk(ScanContext context, CodeRegion region)
	{
		QuoteRule? hole = region.EnclosingHole;
		bool nestingIsSuspended = context.NestingIsSuspendedForARecovery
			|| (context.NestingRecoveriesAreSpent && hole?.InterpolationArmBlock is null);
		string? nestingOpening = nestingIsSuspended ? null : hole?.InterpolationNestingOpening;

		return new CodeWalk
		{
			Position = region.Start,
			NestingOpening = nestingOpening,
			ArmBlock = nestingOpening is null ? null : region.EnclosingHole?.InterpolationArmBlock
		};
	}

	/// <summary>
	/// ONE STEP OF THE CODE WALK. Where the profile declares a code escape, the character after it is part of the
	/// word: the walk steps over the pair, and the escaped character opens nothing, closes nothing, counts as no
	/// bracket and starts no keyword (an escaped line feed joins the lines, so it drains nothing).
	/// </summary>
	private void StepThroughCode(ScanContext context, CodeRegion region, ref CodeWalk walk)
	{
		if (_codeEscape is char escape && context.Text[walk.Position] == escape)
		{
			walk.Position += Math.Min(_ESCAPE_PAIR_LENGTH, region.EndExclusive - walk.Position);
		}
		else
		{
			StepThroughCodeThatIsNotEscaped(context, region, ref walk);
		}
	}

	/// <summary>
	/// A step of the code walk that does not start an escape, in this order: a hole's closing (an arm terminator
	/// first, which is not one), a nested opening, an arm block's keyword, the drain of this region's pending
	/// bodies, and any construct that can open at the position.
	/// </summary>
	private void StepThroughCodeThatIsNotEscaped(ScanContext context, CodeRegion region, ref CodeWalk walk)
	{
		int holeClosingLength = MatchHoleClosingLength(context, region, walk.Position);
		bool atANestingOpening = holeClosingLength == 0
			&& walk.NestingOpening is not null
			&& MatchesAt(context.Text, walk.Position, walk.NestingOpening, _delimiterComparison);
		bool atAnArmTerminator = holeClosingLength > 0 && walk.IsAtAnArmTerminator;
		int tokenLength = holeClosingLength > 0 ? holeClosingLength : atANestingOpening ? walk.NestingOpening!.Length : _SINGLE_CHARACTER_LENGTH;

		TrackTheContext(context, region, new WalkToken(walk.Position, tokenLength, atAnArmTerminator));

		if (atAnArmTerminator)
		{
			walk.EndAnArm(holeClosingLength);
		}
		else if (holeClosingLength > 0 && walk.NestingDepth > 0)
		{
			walk.CloseANesting(holeClosingLength);
		}
		else if (holeClosingLength > 0)
		{
			context.Mark(walk.Position, holeClosingLength, LexicalZone.Delimiter);
			walk.Position += holeClosingLength;
			walk.Closed = true;
		}
		else if (atANestingOpening)
		{
			walk.NestingDepth++;
			walk.CountedANestedOpening = true;
			walk.Position += walk.NestingOpening!.Length;
		}
		else if (!TryStepOverAnArmBlockKeyword(context, region, ref walk))
		{
			StepOverTheRestOfTheCode(context, region, ref walk);
		}
	}

	private void StepOverTheRestOfTheCode(ScanContext context, CodeRegion region, ref CodeWalk walk)
	{
		if (ShouldDrainThePendingBodies(context, region, walk.Position))
		{
			walk.Position = DrainThePendingBodies(context, region, walk.Position);
		}
		else
		{
			walk.Position = BeginAConstructOrAdvance(context, region, walk.Position);
		}
	}

	/// <summary>
	/// A pending here-document body starts after the line feed that ends the line holding its opening, and
	/// each region drains its OWN queue: the file's top-level code the file's queue, and a bracketed
	/// hole's interior the queue of that hole, whose bodies are the lines after the opening line inside the
	/// hole, so a drain can never take another region's opening. A line feed preceded by an odd run of
	/// backslashes continues the command, so the bodies start after the line that ends it, EXCEPT where that
	/// backslash is the last character of a comment: a comment runs to the line feed whatever it ends in, so the
	/// line feed ends the command (a here-document opened before <c># c\</c> reads its body from the next line).
	/// </summary>
	private static bool ShouldDrainThePendingBodies(ScanContext context, CodeRegion region, int position)
	{
		bool shouldDrain =
			HasPendingBodies(context, region)
			&& context.Text[position] == _LINE_FEED
			&& !ContinuesTheCommand(context, position);

		return shouldDrain;
	}

	private static bool ContinuesTheCommand(ScanContext context, int lineFeedPosition)
	{
		return FollowsAnOddBackslashRun(context.Text, lineFeedPosition)
			&& context.Zones[lineFeedPosition - _SINGLE_CHARACTER_LENGTH] is not LexicalZone.Comment;
	}

	private static bool HasPendingBodies(ScanContext context, CodeRegion region)
	{
		return region.IsOutermost
			? context.HasPendingBodies
			: region.State?.HasPendingBodies is true;
	}

	private static bool FollowsAnOddBackslashRun(string text, int position)
	{
		int runLength = 0;

		while (position - runLength > 0 && text[position - runLength - 1] == _ESCAPING_BACKSLASH)
		{
			runLength++;
		}

		return runLength % _BACKSLASH_PAIR_LENGTH != 0;
	}

	private static bool DeclaresADeferredBodyStart(LexicalProfile profile)
	{
		return profile.QuoteRulesInEngineOrder.Any(
			rule => rule.CapturedTerminator?.BodyStart is HereDocumentBodyStart.AfterTheOpeningLine);
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
	/// The counter counts INTERPOLATION HOLES currently open. Bounding holes bounds the whole
	/// recursion: a literal is only ever entered from <see cref="ScanCodeRegion"/>, and a code region
	/// deeper than the file itself is only ever entered from a hole. Literal frames are therefore always
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
			PositionalAnchoring.EndOfLineAfterOptionalBlanks => EndsTheLineAfterOptionalBlanks(text, endExclusive),
			PositionalAnchoring.WholeLine => EndsTheLine(text, endExclusive) && BeginsTheLine(text, start),
			PositionalAnchoring.WholeLineAfterLeadingTabs =>
				EndsTheLine(text, endExclusive) && OnlyTabsPrecedeOnTheLine(text, start),
			_ => true
		};

		return satisfied;
	}

	private static bool BeginsTheLine(string text, int start)
	{
		return start == 0 || text[start - 1] == _LINE_FEED;
	}

	private static bool EndsTheLine(string text, int endExclusive)
	{
		return endExclusive >= text.Length || text[endExclusive] == _LINE_FEED;
	}

	/// <summary>
	/// Only tab characters stand between the previous line feed, or the start of the text, and the
	/// position: the tab-stripping here-document's terminator may be indented by tabs and by nothing
	/// else.
	/// </summary>
	private static bool OnlyTabsPrecedeOnTheLine(string text, int start)
	{
		int position = start;

		while (position > 0 && text[position - 1] == _TAB)
		{
			position--;
		}

		return BeginsTheLine(text, position);
	}

	/// <summary>
	/// The here-string opener shape. Only spaces and tabs may stand between the delimiter and the end of
	/// the line, which is the shape of a here-string opener the language accepts. Any other
	/// character, a carriage return included, makes it not an opener.
	/// </summary>
	private static bool EndsTheLineAfterOptionalBlanks(string text, int endExclusive)
	{
		int position = endExclusive;

		while (position < text.Length && (text[position] == _SPACE || text[position] == _TAB))
		{
			position++;
		}

		return position >= text.Length || text[position] == _LINE_FEED;
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
	/// The window a captured-terminator opening is matched in: the rest of the logical line that holds it, and
	/// at most one line-length ceiling beyond, because a terminator word quoted across a raw line feed
	/// (<c>&lt;&lt;'E</c> newline <c>F'</c>) ends on a later line. The bound keeps a stuck expression to a constant
	/// number of characters per opening, and the search is charged to the lexical budget.
	/// </summary>
	private static int EndOfTheOpeningWindow(ScanContext context, int position)
	{
		return (int)Math.Min(context.Text.Length, (long)EndOfLogicalLineFrom(context, position) + ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);
	}

	/// <summary>
	/// The end of the logical line that holds the position: the first line feed that no odd run of
	/// backslashes continues, so a command written across a backslash-newline is one line. A here-document's
	/// terminator word may continue across one, which a physical line's window would cut. The answer is kept
	/// for the whole logical line, so the openings met on one long continued command cost one walk of it
	/// between them and not one each.
	/// </summary>
	private static int EndOfLogicalLineFrom(ScanContext context, int position)
	{
		string text = context.Text;
		int end = context.LogicalLineCovering(position);

		if (end < 0)
		{
			end = EndOfLineFrom(text, position);

			while (end < text.Length && FollowsAnOddBackslashRun(text, end))
			{
				end = EndOfLineFrom(text, end + 1);
			}

			context.RememberTheLogicalLine(position, end);
		}

		return end;
	}

	/// <summary>
	/// An expression required to match AT a position rather than anywhere after it, searched
	/// within the window the caller gives (the end of the current line, or the window of a captured-terminator
	/// opening, see <see cref="EndOfTheOpeningWindow"/>).
	///
	/// THE WINDOW IS THE POINT. Both expressions that reach here - a here-document's opening and
	/// an identifier-extent hole's identifier - are line-local by construction in every language
	/// that has them, and without the window a failed match at one candidate position would scan
	/// forward through the whole remaining file. In a 2 MB file whose content happens to start
	/// with the candidate's first character that is quadratic, which is the exact cost shape
	/// the per-file ceilings exist to cap rather than to invite.
	///
	/// THE WINDOW BOUNDS HOW MUCH TEXT A STUCK EXPRESSION COULD WALK, NOT WHETHER WALKING IT
	/// TERMINATES, so the search is charged to the file's lexical budget. Where the budget is spent,
	/// or the expression exceeds its own timeout, the scan is abandoned here: the mask stays sound up
	/// to the opening of the outermost construct being read, exactly as for the recursion bound.
	/// </summary>
	private Match? MatchAnchored(ScanContext context, Regex expression, int position, int windowEnd)
	{
		string text = context.Text;
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

		/// <summary>
		/// What this region keeps while it is walked, or null when the profile declares nothing that needs it:
		/// the bodies waiting for a drain and the arithmetic brackets that are open.
		/// </summary>
		public RegionState? State { get; init; }
	}

	/// <summary>
	/// The mutable position of one walk over a region, and the nesting it has counted. A struct passed by
	/// reference, so a region read costs no allocation for it.
	/// </summary>
	private struct CodeWalk
	{
		public required int Position { get; set; }

		public bool Closed { get; set; }

		public int NestingDepth { get; set; }

		/// <summary>Set by a nesting opener and by an arm block's opening keyword: both defer the hole's closing.</summary>
		public bool CountedANestedOpening { get; set; }

		public string? NestingOpening { get; init; }

		public ArmBlockRule? ArmBlock { get; init; }

		/// <summary>The nesting depths at which the arm blocks now open were opened, innermost last.</summary>
		private Stack<int>? ArmBlockDepths { get; set; }

		/// <summary>The position after the closing token that last ended an arm's pattern, or zero: a command begins there.</summary>
		public int ArmTerminatorEnd { get; set; }

		/// <summary>
		/// A closing token met at the depth an arm block opened at ends nothing: it ends an arm's pattern. The
		/// same holds of the block's closing keyword, which ends only a block opened at the depth it stands at.
		/// </summary>
		public readonly bool IsAtAnArmTerminator => ArmBlockDepths is { Count: > 0 } depths && depths.Peek() == NestingDepth;

		public void PushAnArmBlock()
		{
			ArmBlockDepths ??= new Stack<int>();
			ArmBlockDepths.Push(NestingDepth);
		}

		public readonly void PopAnArmBlock()
		{
			_ = ArmBlockDepths!.Pop();
		}

		public void EndAnArm(int tokenLength)
		{
			ArmTerminatorEnd = Position + tokenLength;
			Position += tokenLength;
		}

		/// <summary>
		/// A closing token that ends a nested level. No arm block needs ending with it: a block opened at a depth
		/// turns every closing token met at that depth into an arm terminator, so this method runs only at a
		/// depth above every open block.
		/// </summary>
		public void CloseANesting(int tokenLength)
		{
			NestingDepth--;
			Position += tokenLength;
		}
	}

	/// <summary>
	/// The openings whose bodies wait for a drain, in the order the shell reads them: those of a hole that closed on
	/// its line first (a shell reads their bodies when the substitution ends, before the bodies of the openings that
	/// came earlier on the command, and in the order the holes closed), then the rest in the order of their openings.
	/// </summary>
	private sealed class PendingBodyList
	{
		private readonly List<LiteralOccurrence> _bodies = [];
		private int _closedHoleCount;

		public int Count => _bodies.Count;

		public bool IsFull => _bodies.Count >= MAXIMUM_PENDING_BODIES;

		/// <summary>The position of the earliest opening among them, which is where a body that never ends declares the mask undefined.</summary>
		public int EarliestOpening => _bodies.Min(body => body.OpeningPosition);

		public void Add(LiteralOccurrence occurrence)
		{
			_bodies.Add(occurrence);
		}

		public void AddBeforeTheOthers(LiteralOccurrence occurrence)
		{
			_bodies.Insert(_closedHoleCount, occurrence);
			_closedHoleCount++;
		}

		public LiteralOccurrence[] ToArray()
		{
			return [.. _bodies];
		}

		/// <summary>Puts back the openings of a snapshot. A speculative read of a hole never changes this list, so the order of its first entries stands.</summary>
		public void Restore(LiteralOccurrence[] bodies)
		{
			_bodies.Clear();
			_bodies.AddRange(bodies);
			_closedHoleCount = Math.Min(_closedHoleCount, _bodies.Count);
		}

		public List<LiteralOccurrence> TakeAll()
		{
			List<LiteralOccurrence> taken = [.. _bodies];

			_bodies.Clear();
			_closedHoleCount = 0;

			return taken;
		}
	}

	/// <summary>
	/// What one region keeps while it is walked, and nothing a second region can read. The bodies of the
	/// openings met in a hole's interior wait here, and not in the file's queue, so that the hole's own
	/// line feed drains them and a nesting recovery that re-reads the hole starts from an empty queue.
	/// </summary>
	private sealed class RegionState
	{
		private PendingBodyList? _pendingBodies;
		private List<ContextFrame>? _frames;

		public bool HasPendingBodies => _pendingBodies is { Count: > 0 };

		public bool PendingBodiesAreFull => _pendingBodies is { IsFull: true };

		/// <summary>The innermost context frame, or null in plain command code.</summary>
		public ContextFrame? Top => _frames is { Count: > 0 } frames ? frames[^1] : null;

		/// <summary>Whether the innermost frame is an arithmetic one.</summary>
		public bool IsInArithmetic => Top is { IsArithmetic: true };

		public void DeferTheBodyOf(LiteralOccurrence occurrence)
		{
			_pendingBodies ??= new PendingBodyList();
			_pendingBodies.Add(occurrence);
		}

		public void DeferTheBodyOfAClosedHole(LiteralOccurrence occurrence)
		{
			_pendingBodies ??= new PendingBodyList();
			_pendingBodies.AddBeforeTheOthers(occurrence);
		}

		public List<LiteralOccurrence> DequeueEveryPendingBody()
		{
			return _pendingBodies is null ? [] : _pendingBodies.TakeAll();
		}

		/// <summary>The runs of the arithmetic bracket this region's walk has measured.</summary>
		public BracketRunMeasure Runs { get; } = new();

		public void Push(ContextFrame frame)
		{
			_frames ??= [];
			_frames.Add(frame);
		}

		public void Pop()
		{
			_frames!.RemoveAt(_frames.Count - 1);
		}

		/// <summary>Ends every arithmetic frame whose closing lies before the position.</summary>
		public void EndTheArithmeticBefore(int position)
		{
			while (Top is { IsArithmetic: true } frame && position >= frame.End)
			{
				Pop();
			}
		}
	}

	/// <summary>Where a region stopped, and whether it stopped because its construct closed.</summary>
	private readonly record struct RegionResult
	{
		public required int Position { get; init; }

		public required bool ClosedItsConstruct { get; init; }

		/// <summary>
		/// Whether the region counted at least one nested level, whether by a nesting opener or by the opening keyword of
		/// an arm block, which is what makes a hole that never closed a candidate for recovery: a nesting that was
		/// never unwound may have swallowed the hole's own closing.
		/// </summary>
		public bool CountedANestedOpening { get; init; }

		/// <summary>
		/// The bodies still waiting when a hole closed: its closing came on the line of their openings, so the
		/// region that holds the literal reads them after the line that ends its own command.
		/// </summary>
		public List<LiteralOccurrence>? LeftOverBodies { get; init; }
	}

	/// <summary>
	/// The state a speculative hole read can change and a recovery must restore. It includes the
	/// bodies waiting for their drain: a hole's interior can neither add to them nor drain them, so the
	/// capture is a guard rather than a repair, and a test pins that a recovery leaves them intact.
	/// </summary>
	private readonly record struct ScanSnapshot(
		int ValidThroughPosition,
		bool IsAbandoned,
		LexicalAnomalyReason AbandonedFor,
		int NewlineTerminatedLiteralCount,
		LiteralOccurrence[] PendingBodies);

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
			ArithmeticLookaheadRemaining = ((long)text.Length * _ARITHMETIC_LOOKAHEAD_CHARACTERS_PER_TEXT_CHARACTER) + _ARITHMETIC_LOOKAHEAD_BASE_CHARACTERS;
			InlineSearchRemaining = ((long)text.Length * _INLINE_SEARCH_CHARACTERS_PER_TEXT_CHARACTER) + _INLINE_SEARCH_BASE_CHARACTERS;
		}

		internal string Text { get; }

		internal LexicalZone[] Zones { get; }

		/// <summary>How many executed strings are open around this scan: zero for the file's own, one more for each body read as code inside another.</summary>
		internal int ExecutedDepth { get; init; }

		/// <summary>
		/// How many more characters the inline-construct closing searches may read in this scan: proportional to the text,
		/// so that openers that never close cost a bounded multiple of its length in all.
		/// </summary>
		internal long InlineSearchRemaining { get; set; }

		/// <summary>Whether the scan stopped because the file's lexical budget was spent, which is a fact about the file and not about the text read.</summary>
		internal bool WasAbandonedForTheLexicalBudget => IsAbandoned && AbandonedFor is LexicalAnomalyReason.LexicalBudgetExceeded;

		/// <summary>Whether the scan recovered from an anomaly without stopping: a hole read by its first closing, or a literal ended by a raw newline.</summary>
		internal bool HasRecoveredFromAnAnomaly => NestingRecoveryCount > 0 || NewlineTerminatedLiteralCount > 0;

		/// <summary>Writes the zones of a nested scan to the positions its characters have in this one's text.</summary>
		internal void MergeTheZonesOf(ScanContext nested, int[] positions)
		{
			for (int index = 0; index < positions.Length; index++)
			{
				Zones[positions[index]] = nested.Zones[index];
			}
		}

		internal int ValidThroughPosition { get; private set; }

		internal bool IsAbandoned { get; private set; }

		/// <summary>
		/// How many more characters the arithmetic read-ahead may read in this file: proportional to the text, so
		/// that the read-ahead of every double parenthesis in a file costs a bounded multiple of its length.
		/// </summary>
		internal long ArithmeticLookaheadRemaining { get; set; }

		/// <summary>
		/// The last top-level position the scan examined, which during any descent is the opening
		/// of the outermost construct currently being read - the top-level loop does not advance
		/// again until that construct is finished.
		/// </summary>
		internal int OutermostConstructOpening { get; set; }

		private LexicalAnomalyReason AbandonedFor { get; set; } = LexicalAnomalyReason.None;

		private int NewlineTerminatedLiteralCount { get; set; }

		private int NestingRecoveryCount { get; set; }

		/// <summary>
		/// The deferred here-documents whose openings were met and whose bodies have not been read, in the
		/// order their openings appeared. Filled only by a rule that declares a deferred body start.
		/// </summary>
		private PendingBodyList PendingBodies { get; } = new();

		private int NestingSuspensionCount { get; set; }

		/// <summary>Whether bracketed holes read by their first closing during a recovery re-read.</summary>
		internal bool NestingIsSuspendedForARecovery => NestingSuspensionCount > 0;

		/// <summary>
		/// Whether the per-file recovery bound is spent. A hole whose rule declares an arm block still counts its
		/// nesting afterwards, and one that then never closes is abandoned as unterminated and not read by its first
		/// closing, so the file is never silently mis-zoned; every other hole is read by its first closing.
		/// </summary>
		internal bool NestingRecoveriesAreSpent =>
			NestingRecoveryCount >= LexicalEnginePolicy.MAXIMUM_NESTING_RECOVERIES_PER_FILE;

		/// <summary>
		/// A hole read under nesting may be re-read unless the scan stopped for the lexical budget, which is
		/// a fact about the file and not a consequence of a misread paren, or the recovery bound is spent.
		/// </summary>
		internal bool CanRecoverFromNesting =>
			!NestingRecoveriesAreSpent && (!IsAbandoned || AbandonedFor is not LexicalAnomalyReason.LexicalBudgetExceeded);

		/// <summary>
		/// The matching closing position of each parenthesis that ends an opening run, found by the arithmetic
		/// read-ahead, or the unmatched marker where the text ended with it open. A read-ahead resolves every
		/// such parenthesis it passes, so each position of the text is read once however many runs it holds.
		/// </summary>
		internal Dictionary<int, int> ArithmeticClosers { get; } = [];

		/// <summary>The start and end of the logical line last walked, so that a later position inside it is answered without a new walk.</summary>
		private (int Start, int End) LogicalLine { get; set; } = (_NO_LOGICAL_LINE, _NO_LOGICAL_LINE);

		/// <summary>The end of the logical line already walked that holds the position, or a negative number if none does.</summary>
		internal int LogicalLineCovering(int position)
		{
			return position >= LogicalLine.Start && position <= LogicalLine.End ? LogicalLine.End : _NO_LOGICAL_LINE;
		}

		internal void RememberTheLogicalLine(int start, int end)
		{
			LogicalLine = (start, end);
		}

		internal bool HasPendingBodies => PendingBodies.Count > 0;

		internal bool PendingBodiesAreFull => PendingBodies.IsFull;

		internal void DeferTheBodyOf(LiteralOccurrence occurrence)
		{
			PendingBodies.Add(occurrence);
		}

		internal void DeferTheBodyOfAClosedHole(LiteralOccurrence occurrence)
		{
			PendingBodies.AddBeforeTheOthers(occurrence);
		}

		/// <summary>
		/// Takes every pending body at once, so that nothing a body contains can add to or read from the
		/// queue being drained.
		/// </summary>
		internal List<LiteralOccurrence> DequeueEveryPendingBody()
		{
			return PendingBodies.TakeAll();
		}

		/// <summary>
		/// The end of the file with bodies still waiting is an unterminated literal: the mask stands up to
		/// the earliest pending opening and is declared undefined after it.
		/// </summary>
		internal void AbandonIfBodiesAreStillPending()
		{
			if (HasPendingBodies)
			{
				Abandon(
					LexicalAnomalyReason.UnterminatedLiteral,
					PendingBodies.EarliestOpening,
					Text.Length,
					LexicalZone.StringBody);
			}
		}

		internal ScanSnapshot TakeSnapshot()
		{
			return new ScanSnapshot(
				ValidThroughPosition,
				IsAbandoned,
				AbandonedFor,
				NewlineTerminatedLiteralCount,
				PendingBodies.Count is 0 ? _emptyPendingBodies : PendingBodies.ToArray());
		}

		/// <summary>
		/// Undoes a speculative read of a hole. Zones are cleared from the hole's start to the end of the
		/// file because a code position is the zero value and costs no write, so a stale mark would
		/// otherwise survive the re-read. Bounded by <see cref="LexicalEnginePolicy.MAXIMUM_NESTING_RECOVERIES_PER_FILE"/>.
		/// </summary>
		internal void RewindForNestingRecovery(ScanSnapshot snapshot, int holeStart)
		{
			ValidThroughPosition = snapshot.ValidThroughPosition;
			IsAbandoned = snapshot.IsAbandoned;
			AbandonedFor = snapshot.AbandonedFor;
			NewlineTerminatedLiteralCount = snapshot.NewlineTerminatedLiteralCount;
			PendingBodies.Restore(snapshot.PendingBodies);

			Array.Clear(Zones, holeStart, Zones.Length - holeStart);
			NestingRecoveryCount++;
			NestingSuspensionCount++;
		}

		internal void ResumeNesting()
		{
			NestingSuspensionCount--;
		}

		internal void Mark(int start, int length, LexicalZone zone)
		{
			int from = Math.Clamp(start, 0, Zones.Length);
			int count = Math.Clamp(length, 0, Zones.Length - from);

			Array.Fill(Zones, zone, from, count);
		}

		/// <summary>
		/// A raw newline inside a literal that does not admit one. It "ends the literal and
		/// is an anomaly", and the recovery is sound - the scanner resumes at a line boundary it
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
			if (!IsAbandoned)
			{
				IsAbandoned = true;
				AbandonedFor = reason;
				ValidThroughPosition = Math.Clamp(boundaryPosition, 0, Zones.Length);

				Mark(fillFrom, Zones.Length - fillFrom, fillZone);
			}
		}

		internal ZoneMask ToMask()
		{
			LexicalAnomalyReason reason = IsAbandoned
				? AbandonedFor
				: NestingRecoveryCount > 0
					? LexicalAnomalyReason.UnbalancedInterpolationNesting
					: NewlineTerminatedLiteralCount > 0
						? LexicalAnomalyReason.NewlineInsideALiteral
						: LexicalAnomalyReason.None;

			return new ZoneMask(Zones, ValidThroughPosition, reason, NewlineTerminatedLiteralCount, NestingRecoveryCount);
		}
	}
}
