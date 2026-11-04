using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// READS THE CONDITIONAL-COMPILATION DIRECTIVES OF ONE FILE INTO A <see cref="ConditionalRegionMap"/>.
///
/// WHICH TEXT IS A DIRECTIVE. Only a line whose first non-blank text is the profile's directive opening, with the zone
/// mask saying <see cref="LexicalZone.Code"/> at that position, below the mask's trust extent, and whose keyword is one of
/// <c>if</c>, <c>elif</c>, <c>else</c>, <c>endif</c>, <c>define</c>, <c>undef</c>; any other keyword (<c>region</c>,
/// <c>pragma</c>, <c>nullable</c>, <c>line</c>, <c>error</c>, <c>warning</c>) is skipped. The condition is the rest of the
/// line up to the first position the mask marks as a comment. A directive-like line inside a verbatim string, a plain raw
/// string or a block comment is a string body or a comment, so it is no directive.
///
/// STATE OF A BRANCH. For one conditional group with the enclosing state
/// <c>parent</c>: an <c>if</c> is Dead when its parent is Dead (its condition is not evaluated, so a define nested in dead
/// text defines nothing), otherwise by its value: false is Dead, true is Live when the parent is Live and Maybe otherwise,
/// unknown is Maybe. An <c>elif</c> is Dead when the parent is Dead or an earlier branch was definitely true; otherwise by
/// its value in the same way, except that a true value is Live only when the parent is Live and every earlier condition of
/// the group was definitely false. An <c>else</c> is Dead when the parent is Dead or an earlier branch was definitely
/// true, Live when the parent is Live and every earlier condition was definitely false, Maybe otherwise. A define or an
/// undef in a Live branch sets the symbol, in a Maybe branch makes it unknown and in a Dead branch does nothing.
///
/// SAFETY, EVERY RULE IN THE DIRECTION OF DEACTIVATING NOTHING. An unparsable condition is unknown (see
/// <see cref="ConditionalDirectiveEvaluator"/>). An <c>elif</c>, <c>else</c> or <c>endif</c> with no open <c>if</c> is
/// ignored. A group that never reaches its <c>endif</c> before the end of the file or of the trust extent contributes no
/// interval, and so does a group with a second <c>else</c> or an <c>elif</c> after its <c>else</c>. A nesting deeper than
/// <see cref="ConditionalRegionMap.MAX_NESTING_DEPTH"/> stops the reading and discards the open groups. A file that holds a literal the mask
/// cannot vouch for (an interpolated raw string, or a literal opened with more than one dollar sign: the scanner reads its
/// body as code, so a directive-like line inside it could pair with a real closing directive) has no map at all.
///
/// LINEARITY. The pass is O(n): one ordinal search for the opening, one scan of the dollar signs, and one walk over the
/// lines in which every character is examined at most a constant number of times (the search for the line end, the skip of
/// the leading blanks, the keyword and the operand). A branch is pushed once and popped once, and the intervals are placed in
/// a list indexed by the ordinal of the directive that opened them, so they come out sorted without a sort.
/// </summary>
internal sealed class ConditionalRegionMapBuilder
{
	private const string _IF_KEYWORD = "if";
	private const string _ELIF_KEYWORD = "elif";
	private const string _ELSE_KEYWORD = "else";
	private const string _ENDIF_KEYWORD = "endif";
	private const string _DEFINE_KEYWORD = "define";
	private const string _UNDEF_KEYWORD = "undef";
	private const string _RAW_STRING_QUOTES = "\"\"\"";
	private const char _LINE_FEED = '\n';
	private const char _DOLLAR = '$';
	private const char _QUOTE = '"';
	private const char _SPACE = ' ';
	private const char _TAB = '\t';

	private readonly ConditionalCompilationRule _rule;
	private readonly string _text;
	private readonly ZoneMask _mask;
	private readonly int _readLimit;
	private readonly ConditionalSymbolTable _symbols;
	private readonly Stack<BranchGroup> _openGroups = new();
	private readonly List<(int Start, int End)?> _intervalByOpeningDirective = [];
	private bool _stopped;

	private ConditionalRegionMapBuilder(ConditionalCompilationRule rule, string text, ZoneMask mask)
	{
		_rule = rule;
		_text = text;
		_mask = mask;
		_readLimit = Math.Min(mask.ValidThroughPosition, text.Length);
		_symbols = new ConditionalSymbolTable(WellKnownBuildSymbols.From(rule));
	}

	private enum DirectiveKind
	{
		None,
		If,
		Elif,
		Else,
		Endif,
		Define,
		Undef
	}

	internal static ConditionalRegionMap Build(ConditionalCompilationRule rule, string text, ZoneMask mask)
	{
		ConditionalRegionMap map = ConditionalRegionMap.Empty;

		if (text.Contains(rule.DirectiveOpening, StringComparison.Ordinal) && !HoldsAMaskGapLiteral(text, mask))
		{
			map = new ConditionalRegionMapBuilder(rule, text, mask).ReadEveryLine();
		}

		return map;
	}

	/// <summary>
	/// Whether the text holds an interpolated raw string (one dollar sign, then three quotes) or a literal opened with more
	/// than one dollar sign, outside a comment or a string body, below the trust extent. Safe in the over-reading direction:
	/// a false positive only discards a map.
	/// </summary>
	private static bool HoldsAMaskGapLiteral(string text, ZoneMask mask)
	{
		int readLimit = Math.Min(mask.ValidThroughPosition, text.Length);
		int dollar = text.IndexOf(_DOLLAR);
		bool holdsAGap = false;

		while (!holdsAGap && dollar >= 0 && dollar < readLimit)
		{
			int runEnd = dollar;

			while (runEnd < text.Length && text[runEnd] == _DOLLAR)
			{
				runEnd++;
			}

			holdsAGap = OpensAGapLiteral(text, mask, dollar, runEnd);
			dollar = runEnd < text.Length ? text.IndexOf(_DOLLAR, runEnd) : -1;
		}

		return holdsAGap;
	}

	private static bool OpensAGapLiteral(string text, ZoneMask mask, int runStart, int runEnd)
	{
		bool quoteFollows = runEnd < text.Length && text[runEnd] == _QUOTE;
		bool moreThanOneDollar = runEnd - runStart > 1;
		bool rawQuotesFollow = quoteFollows && text.AsSpan(runEnd).StartsWith(_RAW_STRING_QUOTES, StringComparison.Ordinal);
		LexicalZone zone = mask.ZoneAt(runStart);

		return quoteFollows
			&& (moreThanOneDollar || rawQuotesFollow)
			&& zone is not (LexicalZone.Comment or LexicalZone.StringBody);
	}

	private ConditionalRegionMap ReadEveryLine()
	{
		int lineStart = 0;

		while (!_stopped && lineStart < _readLimit)
		{
			int lineEnd = _text.IndexOf(_LINE_FEED, lineStart);
			lineEnd = lineEnd < 0 ? _text.Length : lineEnd;

			ReadOneLine(lineStart, lineEnd);
			lineStart = lineEnd + 1;
		}

		return ConditionalRegionMap.Of([.. _intervalByOpeningDirective.Where(interval => interval.HasValue).Select(interval => interval!.Value)]);
	}

	private void ReadOneLine(int lineStart, int lineEnd)
	{
		int firstNonBlank = lineStart;

		while (firstNonBlank < lineEnd && _text[firstNonBlank] is _SPACE or _TAB)
		{
			firstNonBlank++;
		}

		bool opensADirective = firstNonBlank < _readLimit
			&& _text.AsSpan(firstNonBlank, lineEnd - firstNonBlank).StartsWith(_rule.DirectiveOpening, StringComparison.Ordinal)
			&& _mask.ZoneAt(firstNonBlank) is LexicalZone.Code;

		if (opensADirective)
		{
			ReadDirective(lineStart, firstNonBlank + _rule.DirectiveOpening.Length, lineEnd);
		}
	}

	private void ReadDirective(int lineStart, int afterOpening, int lineEnd)
	{
		int keywordStart = afterOpening;

		while (keywordStart < lineEnd && _text[keywordStart] is _SPACE or _TAB)
		{
			keywordStart++;
		}

		int keywordEnd = keywordStart;

		while (keywordEnd < lineEnd && ConditionalDirectiveEvaluator.IsIdentifierCharacter(_text[keywordEnd]))
		{
			keywordEnd++;
		}

		DirectiveKind kind = KindOf(_text.AsSpan(keywordStart, keywordEnd - keywordStart));

		if (kind is not DirectiveKind.None)
		{
			Apply(kind, ReadOperand(keywordEnd, lineEnd), lineStart, Math.Min(lineEnd + 1, _text.Length));
		}
	}

	private static DirectiveKind KindOf(ReadOnlySpan<char> keyword)
	{
		DirectiveKind kind = DirectiveKind.None;

		if (keyword.SequenceEqual(_IF_KEYWORD))
		{
			kind = DirectiveKind.If;
		}
		else if (keyword.SequenceEqual(_ELIF_KEYWORD))
		{
			kind = DirectiveKind.Elif;
		}
		else if (keyword.SequenceEqual(_ELSE_KEYWORD))
		{
			kind = DirectiveKind.Else;
		}
		else if (keyword.SequenceEqual(_ENDIF_KEYWORD))
		{
			kind = DirectiveKind.Endif;
		}
		else if (keyword.SequenceEqual(_DEFINE_KEYWORD))
		{
			kind = DirectiveKind.Define;
		}
		else if (keyword.SequenceEqual(_UNDEF_KEYWORD))
		{
			kind = DirectiveKind.Undef;
		}

		return kind;
	}

	/// <summary>The rest of the line up to the first comment position, the end of the line or the trust extent.</summary>
	private string ReadOperand(int operandStart, int lineEnd)
	{
		int operandEnd = operandStart;

		while (operandEnd < lineEnd && operandEnd < _readLimit && _mask.ZoneAt(operandEnd) is not LexicalZone.Comment)
		{
			operandEnd++;
		}

		return _text[operandStart..operandEnd];
	}

	private void Apply(DirectiveKind kind, string operand, int lineStart, int afterLine)
	{
		switch (kind)
		{
			case DirectiveKind.If:
				OpenGroup(operand, afterLine);
				break;
			case DirectiveKind.Elif:
				ContinueGroup(operand, lineStart, afterLine, isElse: false);
				break;
			case DirectiveKind.Else:
				ContinueGroup(operand, lineStart, afterLine, isElse: true);
				break;
			case DirectiveKind.Endif:
				CloseGroup(lineStart);
				break;
			default:
				ChangeSymbol(kind, operand);
				break;
		}
	}

	private ConditionalRegionState CurrentState()
	{
		return _openGroups.Count == 0 ? ConditionalRegionState.Live : _openGroups.Peek().BranchState;
	}

	private void OpenGroup(string condition, int afterLine)
	{
		if (_openGroups.Count >= ConditionalRegionMap.MAX_NESTING_DEPTH)
		{
			_stopped = true;
			_openGroups.Clear();
		}
		else
		{
			BranchGroup group = new() { ParentState = CurrentState() };

			_openGroups.Push(group);
			OpenBranch(group, StateOfAConditionalBranch(group, condition), afterLine);
		}
	}

	private void ContinueGroup(string condition, int lineStart, int afterLine, bool isElse)
	{
		if (_openGroups.Count > 0)
		{
			BranchGroup group = _openGroups.Peek();

			if (group.ElseSeen)
			{
				group.Malformed = true;
			}
			else
			{
				EndBranch(group, lineStart);
				group.ElseSeen = isElse;
				OpenBranch(group, isElse ? StateOfAnElseBranch(group) : StateOfAConditionalBranch(group, condition), afterLine);
			}
		}
	}

	private void CloseGroup(int lineStart)
	{
		if (_openGroups.Count > 0)
		{
			BranchGroup group = _openGroups.Pop();

			EndBranch(group, lineStart);

			if (!group.Malformed)
			{
				foreach ((int ordinal, (int Start, int End) interval) in group.PendingIntervals)
				{
					_intervalByOpeningDirective[ordinal] = interval;
				}
			}
		}
	}

	private ConditionalRegionState StateOfAConditionalBranch(BranchGroup group, string condition)
	{
		ConditionValue value = ConditionValue.False;
		ConditionalRegionState state = ConditionalRegionState.Dead;

		if (group.ParentState is not ConditionalRegionState.Dead && !group.AnEarlierConditionWasTrue)
		{
			value = ConditionalDirectiveEvaluator.Evaluate(condition, _symbols);
			state = StateOfAnEvaluatedBranch(group, value);
		}

		group.AnEarlierConditionWasTrue = group.AnEarlierConditionWasTrue || value is ConditionValue.True;
		group.EveryEarlierConditionWasFalse = group.EveryEarlierConditionWasFalse && value is ConditionValue.False;

		return state;
	}

	private static ConditionalRegionState StateOfAnEvaluatedBranch(BranchGroup group, ConditionValue value)
	{
		ConditionalRegionState state = ConditionalRegionState.Maybe;

		if (value is ConditionValue.False)
		{
			state = ConditionalRegionState.Dead;
		}
		else if (value is ConditionValue.True && group.ParentState is ConditionalRegionState.Live && group.EveryEarlierConditionWasFalse)
		{
			state = ConditionalRegionState.Live;
		}

		return state;
	}

	private static ConditionalRegionState StateOfAnElseBranch(BranchGroup group)
	{
		ConditionalRegionState state = ConditionalRegionState.Maybe;

		if (group.ParentState is ConditionalRegionState.Dead || group.AnEarlierConditionWasTrue)
		{
			state = ConditionalRegionState.Dead;
		}
		else if (group.ParentState is ConditionalRegionState.Live && group.EveryEarlierConditionWasFalse)
		{
			state = ConditionalRegionState.Live;
		}

		return state;
	}

	private void OpenBranch(BranchGroup group, ConditionalRegionState state, int afterLine)
	{
		group.BranchState = state;
		group.BranchStart = afterLine;
		group.BranchOrdinal = _intervalByOpeningDirective.Count;
		_intervalByOpeningDirective.Add(null);
	}

	/// <summary>
	/// A branch that is Dead on its own account (its parent is not Dead, since a Dead parent's interval already covers it)
	/// is held by its group until the group's closing directive confirms the structure.
	/// </summary>
	private static void EndBranch(BranchGroup group, int branchEnd)
	{
		bool deadOnItsOwnAccount = group.BranchState is ConditionalRegionState.Dead && group.ParentState is not ConditionalRegionState.Dead;

		if (deadOnItsOwnAccount && group.BranchStart < branchEnd)
		{
			group.PendingIntervals.Add((group.BranchOrdinal, (group.BranchStart, branchEnd)));
		}
	}

	private void ChangeSymbol(DirectiveKind kind, string operand)
	{
		ConditionalRegionState state = CurrentState();

		if (state is not ConditionalRegionState.Dead && ConditionalDirectiveEvaluator.TryReadSymbolName(operand, out string symbol))
		{
			if (state is ConditionalRegionState.Maybe)
			{
				_symbols.MarkUnknown(symbol);
			}
			else if (kind is DirectiveKind.Define)
			{
				_symbols.Define(symbol);
			}
			else
			{
				_symbols.Undefine(symbol);
			}
		}
	}

	/// <summary>One conditional group still open: its enclosing state, what its earlier conditions said, and the branch it is in.</summary>
	private sealed class BranchGroup
	{
		internal required ConditionalRegionState ParentState { get; init; }

		internal bool AnEarlierConditionWasTrue { get; set; }

		internal bool EveryEarlierConditionWasFalse { get; set; } = true;

		internal bool ElseSeen { get; set; }

		internal bool Malformed { get; set; }

		internal ConditionalRegionState BranchState { get; set; }

		internal int BranchStart { get; set; }

		internal int BranchOrdinal { get; set; }

		internal List<(int Ordinal, (int Start, int End) Interval)> PendingIntervals { get; } = [];
	}
}
