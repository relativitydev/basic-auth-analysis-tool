using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Lexical;

// THE REGION HALF OF THE CODE WALK: what a walk keeps about the command structure it is reading, beyond the
// text of the hole. Two things live here and neither exists for a profile that does not declare it: the ARM
// BLOCK (a keyword pair whose arms end in a closing token that is not the hole's) and the ARITHMETIC CONTEXT (a
// bracketed expression in which a deferred-body opening is an operator). Both are read from the code positions
// the walk visits, so a literal's or a comment's characters never reach them.
public sealed partial class LexicalScanner
{
	private const int _NO_ARITHMETIC_END = -1;
	private const int _NO_CLOSER = -1;
	private const char _OPTION_WORD_INTRODUCER = '-';
	private const int _ARITHMETIC_LOOKAHEAD_CHARACTERS_PER_TEXT_CHARACTER = 8;
	private const int _ARITHMETIC_LOOKAHEAD_BASE_CHARACTERS = 4096;

	/// <summary>
	/// THE ARM BLOCK'S KEYWORDS. At a command position, the opening keyword records the depth the block opened
	/// at, and the closing keyword ends the block opened at the SAME depth: a closing keyword inside a nested
	/// substitution never ends the outer block, and an opening keyword in a nested substitution opens a block
	/// of its own at the deeper depth, which the walk unwinds when that substitution closes. While a block is
	/// open, a closing token met at the depth it opened at is an arm terminator (see
	/// <see cref="CodeWalk.IsAtAnArmTerminator"/>). The same blocks are kept for a command substitution opened
	/// inside an arithmetic context, which the walk of a hole does not count by nesting.
	/// </summary>
	private bool TryStepOverAnArmBlockKeyword(ScanContext context, CodeRegion region, ref CodeWalk walk)
	{
		ContextFrame? frame = region.State?.Top is { IsArithmetic: false } top ? top : null;
		bool inArithmetic = region.State?.IsInArithmetic is true;
		ArmBlockRule? block = inArithmetic ? null : walk.ArmBlock ?? (frame is null ? null : _armBlock);
		bool stepped = false;

		if (block is not null)
		{
			KeywordContext keywordContext = new(context.Text, region.Start, block, Math.Max(walk.ArmTerminatorEnd, frame?.ArmTerminatorEnd ?? 0));
			bool opens = StandsAsACommandKeyword(keywordContext, walk.Position, block.OpeningKeyword);
			bool closes = !opens
				&& (walk.IsAtAnArmTerminator || frame?.IsAtAnArmTerminator is true)
				&& StandsAsACommandKeyword(keywordContext, walk.Position, block.ClosingKeyword);

			if (opens)
			{
				OpenTheBlockOfTheWalk(ref walk, frame, block);
			}
			else if (closes)
			{
				CloseTheBlockOfTheWalk(ref walk, frame, block);
			}

			stepped = opens || closes;
		}

		return stepped;
	}

	private static void OpenTheBlockOfTheWalk(ref CodeWalk walk, ContextFrame? frame, ArmBlockRule block)
	{
		if (walk.ArmBlock is not null)
		{
			walk.PushAnArmBlock();
		}

		frame?.PushAnArmBlock();
		walk.CountedANestedOpening = true;
		walk.Position += block.OpeningKeyword.Length;
	}

	private static void CloseTheBlockOfTheWalk(ref CodeWalk walk, ContextFrame? frame, ArmBlockRule block)
	{
		if (walk.IsAtAnArmTerminator)
		{
			walk.PopAnArmBlock();
		}

		if (frame?.IsAtAnArmTerminator is true)
		{
			frame.PopAnArmBlock();
		}

		walk.Position += block.ClosingKeyword.Length;
	}

	private bool StandsAsACommandKeyword(in KeywordContext keywordContext, int position, string keyword)
	{
		string text = keywordContext.Text;
		bool standsAsAWord =
			MatchesAt(text, position, keyword, _keywordComparison)
			&& StandsAsAWholeWord(text, position, position + keyword.Length);

		return standsAsAWord && StandsAtACommandPosition(keywordContext, position);
	}

	/// <summary>
	/// Where a shell reads a reserved word: a command position, or after a chain of licensing words whose first
	/// stands at one. A command position is the start of the region, a position after one of the block's
	/// predecessor characters (blanks skipped), directly after the closing token that ended an arm's pattern, or
	/// after the empty parentheses of a function definition. A licensing word (<c>then</c>, <c>!</c>, <c>time</c>
	/// and the rest of the block's words) licenses what follows only where it stands at a command position
	/// itself, so a word of that spelling among a command's arguments licenses nothing; a chain of them
	/// (<c>then then case</c>, a syntax error in a shell) is read as licensed. Not after an assignment word or a
	/// plain command word, which are arguments.
	/// </summary>
	private bool StandsAtACommandPosition(in KeywordContext keywordContext, int position)
	{
		int cursor = SkipBlanksBackwards(keywordContext, position);

		return IsACommandPositionAfter(keywordContext, cursor)
			|| (cursor < position && FollowsALicensingChain(keywordContext, cursor));
	}

	private static int SkipBlanksBackwards(in KeywordContext keywordContext, int position)
	{
		int cursor = position;

		while (cursor > keywordContext.RegionStart && (keywordContext.Text[cursor - 1] == _SPACE || keywordContext.Text[cursor - 1] == _TAB))
		{
			cursor--;
		}

		return cursor;
	}

	private bool IsACommandPositionAfter(in KeywordContext keywordContext, int cursor)
	{
		return cursor <= keywordContext.RegionStart
			|| cursor == keywordContext.ArmTerminatorEnd
			|| keywordContext.Block.CommandPositionPredecessors.Contains(keywordContext.Text[cursor - 1])
			|| EndsAFunctionDefinitionHead(keywordContext, cursor);
	}

	/// <summary>
	/// A closing parenthesis directly after an opening one (blanks between allowed) that does not end a
	/// substitution opening: the head of a function definition, whose body is a command.
	/// </summary>
	private bool EndsAFunctionDefinitionHead(in KeywordContext keywordContext, int cursor)
	{
		string text = keywordContext.Text;
		int closingIndex = cursor - _SINGLE_CHARACTER_LENGTH;
		bool ends = false;

		if (_substitutionRule is { InterpolationOpening: string opening, InterpolationClosing: string closing }
			&& closingIndex > keywordContext.RegionStart
			&& text[closingIndex] == closing[^1])
		{
			int open = closingIndex - _SINGLE_CHARACTER_LENGTH;

			while (open > keywordContext.RegionStart && (text[open] == _SPACE || text[open] == _TAB))
			{
				open--;
			}

			ends = open >= keywordContext.RegionStart
				&& text[open] == opening[^1]
				&& !MatchesAt(text, open - (opening.Length - _SINGLE_CHARACTER_LENGTH), opening, _delimiterComparison);
		}

		return ends;
	}

	/// <summary>
	/// Walks back over the licensing words that end at the position, each separated from the next by blanks, and
	/// is true when the first of them stands at a command position. The walk ends at the first word that
	/// licenses nothing, so a keyword-named argument breaks it.
	/// </summary>
	private bool FollowsALicensingChain(in KeywordContext keywordContext, int wordEnd)
	{
		int cursor = wordEnd;
		bool follows = false;
		bool decided = false;
		bool sawAnOption = false;

		while (!decided)
		{
			int wordStart = StartOfTheWordBefore(keywordContext, cursor);
			bool licenses = IsAWordOf(keywordContext, wordStart, cursor, keywordContext.Block.CommandPositionKeywords);
			bool isAnOption = !licenses && IsAnOptionWord(keywordContext, wordStart, cursor);
			int before = SkipBlanksBackwards(keywordContext, wordStart);

			if (licenses && !(sawAnOption && !IsAWordOf(keywordContext, wordStart, cursor, keywordContext.Block.OptionTakingKeywords)))
			{
				follows = IsACommandPositionAfter(keywordContext, before);
				decided = follows || before == wordStart;
				sawAnOption = false;
			}
			else if (isAnOption)
			{
				sawAnOption = true;
				decided = before == wordStart;
			}
			else
			{
				decided = true;
			}

			cursor = before;
		}

		return follows;
	}

	/// <summary>
	/// A word that begins with a hyphen and holds something after it: an option, which keeps the command position of
	/// the keyword that takes it (a shell's <c>time -p</c>).
	/// </summary>
	private static bool IsAnOptionWord(in KeywordContext keywordContext, int wordStart, int wordEnd)
	{
		return wordEnd - wordStart > _SINGLE_CHARACTER_LENGTH && keywordContext.Text[wordStart] == _OPTION_WORD_INTRODUCER;
	}

	private static int StartOfTheWordBefore(in KeywordContext keywordContext, int wordEnd)
	{
		int wordStart = wordEnd;

		while (wordStart > keywordContext.RegionStart
			&& !char.IsWhiteSpace(keywordContext.Text[wordStart - 1])
			&& !keywordContext.Block.CommandPositionPredecessors.Contains(keywordContext.Text[wordStart - 1]))
		{
			wordStart--;
		}

		return wordStart;
	}

	private bool IsAWordOf(in KeywordContext keywordContext, int wordStart, int wordEnd, ImmutableArray<string> keywords)
	{
		ReadOnlySpan<char> word = keywordContext.Text.AsSpan(wordStart, wordEnd - wordStart);
		bool isOne = false;

		foreach (string keyword in keywords)
		{
			isOne = isOne || word.Equals(keyword, _keywordComparison);
		}

		return isOne;
	}

	/// <summary>
	/// THE CONTEXT FRAMES OF A REGION, advanced over the code positions the walk visits, and over every
	/// character of a multi-character token the walk steps across (a nesting opening, a hole's closing), so a
	/// bracket inside a token is counted. See <see cref="ArithmeticContextRule"/>.
	/// </summary>
	private void TrackTheContext(ScanContext context, CodeRegion region, WalkToken token)
	{
		if (_arithmeticContext is ArithmeticContextRule rule && region.State is RegionState state)
		{
			for (int offset = 0; offset < token.Length; offset++)
			{
				int position = token.Position + offset;
				char character = context.Text[position];

				state.EndTheArithmeticBefore(position);

				if (character == rule.Opening[0])
				{
					OpenABracket(context, rule, state, position);
				}
				else if (character == rule.Closing[0])
				{
					CloseABracket(state, position, token.IsAnArmTerminator);
				}
			}
		}
	}

	/// <summary>
	/// An opening bracket. In a command context a run of the opening's length starts an arithmetic frame only
	/// where the run is closed by its own closing run, which is the shell's rule: a double parenthesis that is
	/// not so closed is two nested subshells. Inside arithmetic a bracket after a substitution opening starts a
	/// command context again; any other is a grouping bracket the end of the frame already counts.
	/// </summary>
	private void OpenABracket(ScanContext context, ArithmeticContextRule rule, RegionState state, int position)
	{
		string text = context.Text;
		ContextFrame? top = state.Top;
		bool startsARun = state.Runs.RemainingRunOf(text, position, rule.Opening[0]) >= rule.Opening.Length;

		if (top is { IsArithmetic: true })
		{
			if (!startsARun && EndsTheSubstitutionOpening(text, position))
			{
				state.Push(ContextFrame.ForASubstitution());
			}
		}
		else
		{
			int end = startsARun ? FindTheArithmeticEnd(context, position + rule.Opening.Length, rule) : _NO_ARITHMETIC_END;

			if (end > 0)
			{
				state.Push(ContextFrame.ForArithmetic(end));
			}
			else if (top is not null)
			{
				top.Depth++;
			}
		}
	}

	private static void CloseABracket(RegionState state, int position, bool atAnArmTerminator)
	{
		if (state.Top is { IsArithmetic: false } frame)
		{
			if (atAnArmTerminator || frame.IsAtAnArmTerminator)
			{
				frame.ArmTerminatorEnd = position + _SINGLE_CHARACTER_LENGTH;
			}
			else
			{
				frame.Depth--;

				if (frame.Depth == 0)
				{
					state.Pop();
				}
			}
		}
	}

	private bool EndsTheSubstitutionOpening(string text, int position)
	{
		return _substitutionRule?.InterpolationOpening is string opening
			&& MatchesAt(text, position - (opening.Length - _SINGLE_CHARACTER_LENGTH), opening, _delimiterComparison);
	}

	/// <summary>
	/// The position after the closing run that ends the arithmetic expression whose opening run ends just before
	/// <paramref name="from"/>, or <see cref="_NO_ARITHMETIC_END"/> where the text does not close it as arithmetic.
	/// The closing is the first closing bracket met with none of the expression's own open (brackets of the opening
	/// kind nest), and the expression ends only if the whole closing run stands there; otherwise the opening was two
	/// nested groups, as a shell reads <c>((cmd) ...)</c>.
	///
	/// ONE READ-AHEAD ANSWERS EVERY RUN IT PASSES. It keeps the parentheses it has open and, when one closes, records
	/// where, so the later question about a run inside the text it has read is a lookup; a read-ahead that reaches
	/// the end of the text records the parentheses still open as unmatched. Each position is read once however many
	/// runs the text holds, and what it answers is what a read-ahead started at that run alone would answer: the
	/// parentheses of a run are never a command substitution's, so a keyword standing directly inside one is read by
	/// neither.
	/// A read-ahead is started afresh only for a run no earlier one passed in code, which happens when the
	/// text it skipped as quoted or as a comment disagrees with the walk's own zones (a body of a here-document with
	/// an unbalanced quote). Those reads are bounded by a per-file budget proportional to the text, and a run whose
	/// closing lies beyond what is left is read as not arithmetic.
	/// </summary>
	private int FindTheArithmeticEnd(ScanContext context, int from, ArithmeticContextRule rule)
	{
		int key = from - _SINGLE_CHARACTER_LENGTH;
		int end = _NO_ARITHMETIC_END;

		if (!context.ArithmeticClosers.ContainsKey(key))
		{
			ReadAheadFrom(context, from, rule);
		}

		if (context.ArithmeticClosers.TryGetValue(key, out int closer)
			&& closer > _NO_CLOSER
			&& RunLength(context.Text, closer, rule.Closing[0]) >= rule.Closing.Length)
		{
			end = closer + rule.Closing.Length;
		}

		return end;
	}

	private void ReadAheadFrom(ScanContext context, int from, ArithmeticContextRule rule)
	{
		string text = context.Text;
		int limit = (int)Math.Min(text.Length, from + Math.Min(context.ArithmeticLookaheadRemaining, text.Length));
		ReadAhead state = new(from);
		long chargeStarted = _budgetWatch.BeginArithmeticCharge();

		while (state.Position < limit && !state.IsFinished)
		{
			StepOverTheReadAhead(context, rule, limit, state);
		}

		context.ArithmeticLookaheadRemaining -= Math.Max(state.Position - from, _SINGLE_CHARACTER_LENGTH);

		bool reachedTheEndOfTheText = limit == text.Length && state.Position >= limit;

		if (reachedTheEndOfTheText && !state.IsFinished)
		{
			state.RecordTheOpenOnesAsUnmatched(context, rule);
		}

		ChargeTheReadAhead(context, from, chargeStarted);
	}

	/// <summary>
	/// The read-ahead is the arithmetic opening's evaluation, and it is charged to the file's lexical budget like the
	/// expression searches are: once per read-ahead, which answers every run it passes, and never once per opening. Where
	/// that spends the budget the scan is abandoned here, on the same terms as a spent expression search.
	/// </summary>
	private void ChargeTheReadAhead(ScanContext context, int from, long chargeStarted)
	{
		_budgetWatch.EndArithmeticCharge(chargeStarted);

		if (_budgetWatch.HasBeenExceeded)
		{
			context.Abandon(
				LexicalAnomalyReason.LexicalBudgetExceeded,
				context.OutermostConstructOpening,
				from,
				LexicalZone.StringBody);
		}
	}

	private void StepOverTheReadAhead(ScanContext context, ArithmeticContextRule rule, int limit, ReadAhead state)
	{
		int skipped = LengthOfASkippedRun(context.Text, state.Position, limit);

		if (skipped > 0)
		{
			state.Position += skipped;
		}
		else if (!StepOverAKeywordInTheReadAhead(context.Text, state))
		{
			StepOverABracketInTheReadAhead(context, rule, state);
		}
	}

	/// <summary>
	/// The arm block's keywords inside a command substitution met by the read-ahead: an arm's closing token ends
	/// no parenthesis, so the read-ahead keeps the same blocks the walk keeps. Inside a plain parenthesis of the
	/// expression a keyword is a name and opens nothing.
	/// </summary>
	private bool StepOverAKeywordInTheReadAhead(string text, ReadAhead state)
	{
		bool stepped = false;

		if (_armBlock is ArmBlockRule block && state.TopIsASubstitution)
		{
			KeywordContext keywordContext = new(text, state.RegionStart, block, state.ArmTerminatorEnd);
			bool opens = StandsAsACommandKeyword(keywordContext, state.Position, block.OpeningKeyword);
			bool closes = !opens
				&& state.IsAtAnArmTerminator
				&& StandsAsACommandKeyword(keywordContext, state.Position, block.ClosingKeyword);

			if (opens)
			{
				state.PushAnArmBlock();
				state.Position += block.OpeningKeyword.Length;
			}
			else if (closes)
			{
				state.PopAnArmBlock();
				state.Position += block.ClosingKeyword.Length;
			}

			stepped = opens || closes;
		}

		return stepped;
	}

	private void StepOverABracketInTheReadAhead(ScanContext context, ArithmeticContextRule rule, ReadAhead state)
	{
		string text = context.Text;
		char character = text[state.Position];

		if (character == rule.Opening[0])
		{
			bool isASubstitution = EndsTheSubstitutionOpening(text, state.Position)
				&& state.Runs.RemainingRunOf(text, state.Position, character) < rule.Opening.Length;

			state.Open(state.Position, isASubstitution);
		}
		else if (character == rule.Closing[0] && state.IsAtAnArmTerminator)
		{
			state.ArmTerminatorEnd = state.Position + _SINGLE_CHARACTER_LENGTH;
		}
		else if (character == rule.Closing[0])
		{
			state.Close(context, rule);
		}

		state.Position++;
	}

	/// <summary>
	/// The length of the run the read-ahead skips whole where it stands: an escape pair, a line comment (to the end of
	/// the line), or a quoted run read by the quote rule's own escape and newline handling, the literal prefix's
	/// escape included. A quoted run that never closes before the limit takes everything to the limit.
	/// </summary>
	private int LengthOfASkippedRun(string text, int position, int limit)
	{
		int length = 0;

		if (_codeEscape is char escape && text[position] == escape)
		{
			length = _ESCAPE_PAIR_LENGTH;
		}

		if (length == 0)
		{
			length = LengthOfALineComment(text, position, limit);
		}

		for (int index = 0; length == 0 && index < _skippedQuotes.Length; index++)
		{
			if (MatchesAt(text, position, _skippedQuotes[index].Opening, _delimiterComparison))
			{
				length = LengthOfAQuotedRun(text, position, limit, _skippedQuotes[index]);
			}
		}

		return length;
	}

	private int LengthOfALineComment(string text, int position, int limit)
	{
		int length = 0;

		foreach (LineCommentRule rule in _lineCommentRules)
		{
			int openingEnd = position + rule.Opening.Length;
			bool opens =
				length == 0
				&& MatchesAt(text, position, rule.Opening, rule.RequiresWordBoundary ? _keywordComparison : _delimiterComparison)
				&& SatisfiesAnchoring(text, position, openingEnd, rule.OpeningAnchoring)
				&& (!rule.RequiresWordBoundary || StandsAsAWholeWord(text, position, openingEnd))
				&& (rule.OpeningContextPredicate is null || rule.OpeningContextPredicate(text, position));

			if (opens)
			{
				length = Math.Min(EndOfLineFrom(text, position), limit) - position;
			}
		}

		return length;
	}

	private int LengthOfAQuotedRun(string text, int position, int limit, SkippedQuote quote)
	{
		int cursor = position + quote.Opening.Length;
		int length = limit - position;
		bool ended = false;

		while (cursor < limit && !ended)
		{
			if (quote.Escape is char escape && text[cursor] == escape)
			{
				cursor += _ESCAPE_PAIR_LENGTH;
			}
			else if (quote.Rule.DoubledClosingEscapes && MatchesAt(text, cursor, quote.Rule.Closing, _delimiterComparison) && MatchesAt(text, cursor + quote.Rule.Closing.Length, quote.Rule.Closing, _delimiterComparison))
			{
				cursor += quote.Rule.Closing.Length * _DOUBLED_CLOSING_FACTOR;
			}
			else if (MatchesAt(text, cursor, quote.Rule.Closing, _delimiterComparison))
			{
				length = cursor + quote.Rule.Closing.Length - position;
				ended = true;
			}
			else if (text[cursor] == _LINE_FEED && quote.Rule.NewlinePolicy is not NewlinePolicy.Allowed)
			{
				length = cursor - position;
				ended = true;
			}
			else
			{
				cursor++;
			}
		}

		return length;
	}

	/// <summary>
	/// The state of a region as the walk starts. A bracketed hole's interior that starts inside the hole's
	/// own opening begins inside the arithmetic context when the opening ends in the first bracket of a
	/// closed run (<c>$((</c>).
	/// </summary>
	private RegionState StartTheRegionState(ScanContext context, CodeRegion region)
	{
		RegionState state = new();

		if (_arithmeticContext is ArithmeticContextRule rule
			&& region.EnclosingHole?.InterpolationOpening is string holeOpening
			&& holeOpening.Length > 0
			&& holeOpening[^1] == rule.Opening[0]
			&& region.Start > 0
			&& RunLength(context.Text, region.Start - 1, rule.Opening[0]) >= rule.Opening.Length)
		{
			int end = FindTheArithmeticEnd(context, region.Start - 1 + rule.Opening.Length, rule);

			if (end > 0)
			{
				state.Push(ContextFrame.ForArithmetic(end));
			}
		}

		return state;
	}

	private static QuoteRule? FindTheSubstitutionRule(LexicalProfile profile)
	{
		char? bracket = profile.ArithmeticContext?.Opening[0];

		return profile.QuoteRulesInEngineOrder
			.Where(rule => rule.InterpolationClosing is { Length: > 0 } && rule.InterpolationOpening is { Length: > 1 })
			.FirstOrDefault(rule => bracket is null || rule.InterpolationOpening![^1] == bracket);
	}

	private static ArmBlockRule? FindTheArmBlock(LexicalProfile profile)
	{
		return profile.QuoteRulesInEngineOrder.Select(rule => rule.InterpolationArmBlock).FirstOrDefault(block => block is not null);
	}

	/// <summary>
	/// The quoted forms the read-ahead skips whole, longest opening first: every quote rule that closes on a constant,
	/// once for each of its literal prefixes (with the prefix's escape handling) and once without.
	/// </summary>
	private static SkippedQuote[] FindTheSkippedQuotes(LexicalProfile profile)
	{
		List<SkippedQuote> quotes = [];

		foreach (QuoteRule rule in profile.QuoteRulesInEngineOrder.Where(rule => rule.CapturedTerminator is null && rule.Closing.Length > 0))
		{
			foreach (LiteralPrefixRule prefix in rule.LiteralPrefixes)
			{
				quotes.Add(new SkippedQuote($"{prefix.Prefix}{rule.Opening}", EffectiveEscapeCharacter(rule, prefix), rule));
			}

			quotes.Add(new SkippedQuote(rule.Opening, rule.EscapeCharacter, rule));
		}

		return [.. quotes.OrderByDescending(quote => quote.Opening.Length)];
	}

	/// <summary>
	/// What the reading of a reserved word needs besides its position: the text, where the region starts, the arm
	/// block whose words are read and the position after the closing token that last ended an arm's pattern.
	/// </summary>
	private readonly record struct KeywordContext(string Text, int RegionStart, ArmBlockRule Block, int ArmTerminatorEnd);

	/// <summary>A token the walk steps across at once: where it starts, how long it is and whether it is an arm's terminator.</summary>
	private readonly record struct WalkToken(int Position, int Length, bool IsAnArmTerminator);

	/// <summary>A quoted form the arithmetic read-ahead skips whole.</summary>
	private readonly record struct SkippedQuote(string Opening, char? Escape, QuoteRule Rule);

	/// <summary>
	/// One arithmetic read-ahead: where it stands, the parentheses it has open (the one before its start at the
	/// bottom), the arm blocks it has seen and the end of the run of brackets it last measured.
	/// </summary>
	private sealed class ReadAhead : ArmBlockTracker
	{
		private readonly List<(int Position, bool IsASubstitution)> _open = [];

		public ReadAhead(int from)
		{
			Position = from;
			RegionStart = Math.Max(0, from - _SINGLE_CHARACTER_LENGTH);
			_open.Add((from - _SINGLE_CHARACTER_LENGTH, false));
		}

		public int Position { get; set; }

		public int RegionStart { get; }

		public bool IsFinished { get; private set; }

		public int ArmTerminatorEnd { get; set; }

		/// <summary>Whether the innermost open parenthesis is a command substitution's, the only place a keyword is read.</summary>
		public bool TopIsASubstitution => _open[^1].IsASubstitution;

		/// <summary>The runs of the arithmetic bracket this read-ahead has measured.</summary>
		public BracketRunMeasure Runs { get; } = new();

		protected override int CurrentDepth => _open.Count;

		public void Open(int position, bool isASubstitution)
		{
			_open.Add((position, isASubstitution));
		}

		/// <summary>Closes the innermost parenthesis and records where, if it is one a later run could ask about.</summary>
		public void Close(ScanContext context, ArithmeticContextRule rule)
		{
			(int position, _) = _open[^1];

			_open.RemoveAt(_open.Count - 1);

			if (EndsAnOpeningRun(context.Text, position, rule))
			{
				context.ArithmeticClosers[position] = Position;
			}

			IsFinished = _open.Count == 0;
		}

		public void RecordTheOpenOnesAsUnmatched(ScanContext context, ArithmeticContextRule rule)
		{
			foreach ((int position, _) in _open.Where(open => EndsAnOpeningRun(context.Text, open.Position, rule)))
			{
				_ = context.ArithmeticClosers.TryAdd(position, _NO_CLOSER);
			}
		}

		private static bool EndsAnOpeningRun(string text, int position, ArithmeticContextRule rule)
		{
			int first = position - rule.Opening.Length + _SINGLE_CHARACTER_LENGTH;

			return first >= 0 && text.AsSpan(first, rule.Opening.Length).IndexOfAnyExcept(rule.Opening[0]) < 0;
		}
	}

	/// <summary>
	/// A frame of the command structure inside an arithmetic context: an arithmetic one, which ends where the
	/// read-ahead found its closing, or a command substitution opened inside one (or inside another such
	/// substitution), which ends when its parentheses balance and keeps its own arm blocks.
	/// </summary>
	private sealed class ContextFrame : ArmBlockTracker
	{
		private const int _SUBSTITUTION_FRAME_OPENING_DEPTH = 1;

		public bool IsArithmetic { get; private init; }

		/// <summary>For an arithmetic frame, the position after its closing run.</summary>
		public int End { get; private init; }

		/// <summary>For a substitution frame, the parentheses open in it, its own opening included.</summary>
		public int Depth { get; set; }

		/// <summary>The position after the closing token that last ended an arm's pattern in this frame, or zero.</summary>
		public int ArmTerminatorEnd { get; set; }

		protected override int CurrentDepth => Depth;

		public static ContextFrame ForArithmetic(int end)
		{
			return new ContextFrame { IsArithmetic = true, End = end };
		}

		public static ContextFrame ForASubstitution()
		{
			return new ContextFrame { Depth = _SUBSTITUTION_FRAME_OPENING_DEPTH };
		}
	}

	/// <summary>
	/// The depths at which the arm blocks now open were opened, innermost last, for a context that counts its own
	/// depth: a closing token met at the depth the innermost block opened at ends an arm's pattern and nothing else.
	/// </summary>
	private abstract class ArmBlockTracker
	{
		private Stack<int>? _armBlockDepths;

		/// <summary>The nesting depth the context is at now.</summary>
		protected abstract int CurrentDepth { get; }

		public bool IsAtAnArmTerminator => _armBlockDepths is { Count: > 0 } depths && depths.Peek() == CurrentDepth;

		public void PushAnArmBlock()
		{
			_armBlockDepths ??= new Stack<int>();
			_armBlockDepths.Push(CurrentDepth);
		}

		public void PopAnArmBlock()
		{
			_ = _armBlockDepths!.Pop();
		}
	}

	/// <summary>
	/// How many of one bracket a run that holds a position still has from the position on. A run is measured once,
	/// where it is first met, and every later position inside it reads the answer, so a long run of one bracket costs
	/// the walk its length and not its square.
	/// </summary>
	private sealed class BracketRunMeasure
	{
		private int _runEnd;

		public int RemainingRunOf(string text, int position, char character)
		{
			if (position >= _runEnd)
			{
				_runEnd = position + RunLength(text, position, character);
			}

			return _runEnd - position;
		}
	}
}
