namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE STRAIGHTFORWARD READING OF ARITHMETIC AND COMMAND SUBSTITUTIONS, written in the test project from the shell's
/// rules and from nothing the scanner holds: a double parenthesis is arithmetic where reading ahead from it, counting
/// parentheses, finds the first closing with none open followed by a second; inside arithmetic a parenthesis after a
/// dollar sign that is not itself a run is a command substitution, which is a command context again; in a command
/// context <c>case</c> at a command position opens an arm block, a closing parenthesis at the depth of an open block ends
/// an arm and nothing else, and <c>esac</c> at a command position ends the block opened at its own depth. It reads one
/// position at a time with no memory of what it has read, so a question about a run is answered by reading again.
/// </summary>
internal static class NaiveArithmeticReading
{
	private const string _OPENING_KEYWORD = "case";
	private const string _CLOSING_KEYWORD = "esac";
	private const string _COMMAND_POSITION_PREDECESSORS = "\n;(";
	private const char _OPENING = '(';
	private const char _CLOSING = ')';
	private const char _SUBSTITUTION_SIGIL = '$';
	private const int _MINIMUM_RUN = 2;
	private const int _KEYWORD_LENGTH = 4;
	private const int _NO_END = -1;
	private const int _SUBSTITUTION_OPENING_DEPTH = 1;
	private const char _WORD_CONNECTOR = '_';
	private const char _SPACE = ' ';
	private const char _TAB = '\t';

	/// <summary>Whether a here-document opening at the position is inside arithmetic, and so refused.</summary>
	internal static bool IsInsideArithmetic(string text, int position)
	{
		List<Frame> frames = [];
		int cursor = 0;

		while (cursor < position)
		{
			cursor = Advance(text, cursor, frames);
		}

		PopTheEndedArithmetic(frames, position);

		return frames.Count > 0 && frames[^1].IsArithmetic;
	}

	private static void PopTheEndedArithmetic(List<Frame> frames, int position)
	{
		while (frames.Count > 0 && frames[^1].IsArithmetic && position >= frames[^1].End)
		{
			frames.RemoveAt(frames.Count - 1);
		}
	}

	private static int Advance(string text, int position, List<Frame> frames)
	{
		PopTheEndedArithmetic(frames, position);

		Frame? top = frames.Count > 0 ? frames[^1] : null;
		int next = position + 1;

		if (top is { IsArithmetic: false } && TryReadAKeyword(text, position, top, out int length))
		{
			next = position + length;
		}
		else if (text[position] == _OPENING)
		{
			OpenAParenthesis(text, position, frames);
		}
		else if (text[position] == _CLOSING)
		{
			CloseAParenthesis(position, frames);
		}

		return next;
	}

	private static bool TryReadAKeyword(string text, int position, Frame frame, out int length)
	{
		bool opens = IsAWord(text, position, _OPENING_KEYWORD) && IsACommandPosition(text, position, frame.ArmEnd);
		bool closes = !opens
			&& frame.IsAtAnArm
			&& IsAWord(text, position, _CLOSING_KEYWORD)
			&& IsACommandPosition(text, position, frame.ArmEnd);

		if (opens)
		{
			frame.Arms.Push(frame.Depth);
		}
		else if (closes)
		{
			_ = frame.Arms.Pop();
		}

		length = opens || closes ? _KEYWORD_LENGTH : 0;

		return opens || closes;
	}

	private static void OpenAParenthesis(string text, int position, List<Frame> frames)
	{
		Frame? top = frames.Count > 0 ? frames[^1] : null;
		int run = RunOf(text, position, _OPENING);

		if (top is { IsArithmetic: true })
		{
			if (run < _MINIMUM_RUN && position > 0 && text[position - 1] == _SUBSTITUTION_SIGIL)
			{
				frames.Add(Frame.ForASubstitution());
			}
		}
		else
		{
			int end = run >= _MINIMUM_RUN ? ArithmeticEnd(text, position + _MINIMUM_RUN) : _NO_END;

			if (end > 0)
			{
				frames.Add(Frame.ForArithmetic(end));
			}
			else if (top is not null)
			{
				top.Depth++;
			}
		}
	}

	private static void CloseAParenthesis(int position, List<Frame> frames)
	{
		if (frames.Count > 0 && !frames[^1].IsArithmetic)
		{
			Frame top = frames[^1];

			if (top.IsAtAnArm)
			{
				top.ArmEnd = position + 1;
			}
			else
			{
				top.Depth--;

				if (top.Depth == 0)
				{
					frames.RemoveAt(frames.Count - 1);
				}
			}
		}
	}

	/// <summary>The position after the closing run of the expression whose opening ends before <paramref name="from"/>, or no end.</summary>
	private static int ArithmeticEnd(string text, int from)
	{
		List<bool> opens = [false];
		Stack<int> arms = [];
		int armEnd = 0;
		int end = _NO_END;
		int position = from;

		while (position < text.Length && end < 0 && opens.Count > 0)
		{
			int step = 1;
			bool inSubstitution = opens[^1];
			bool atAnArm = arms.Count > 0 && arms.Peek() == opens.Count;

			if (inSubstitution && IsAWord(text, position, _OPENING_KEYWORD) && IsACommandPosition(text, position, armEnd))
			{
				arms.Push(opens.Count);
				step = _KEYWORD_LENGTH;
			}
			else if (inSubstitution && atAnArm && IsAWord(text, position, _CLOSING_KEYWORD) && IsACommandPosition(text, position, armEnd))
			{
				_ = arms.Pop();
				step = _KEYWORD_LENGTH;
			}
			else if (text[position] == _OPENING)
			{
				opens.Add(position > 0 && text[position - 1] == _SUBSTITUTION_SIGIL && RunOf(text, position, _OPENING) < _MINIMUM_RUN);
			}
			else if (text[position] == _CLOSING && atAnArm)
			{
				armEnd = position + 1;
			}
			else if (text[position] == _CLOSING)
			{
				opens.RemoveAt(opens.Count - 1);
				end = opens.Count == 0 && RunOf(text, position, _CLOSING) >= _MINIMUM_RUN ? position + _MINIMUM_RUN : _NO_END;
			}

			position += step;
		}

		return end;
	}

	private static bool IsAWord(string text, int position, string word)
	{
		int end = position + word.Length;

		return string.CompareOrdinal(text, position, word, 0, word.Length) == 0
			&& (position == 0 || !IsAWordCharacter(text[position - 1]))
			&& (end >= text.Length || !IsAWordCharacter(text[end]));
	}

	private static bool IsAWordCharacter(char character)
	{
		return char.IsLetterOrDigit(character) || character == _WORD_CONNECTOR;
	}

	/// <summary>
	/// A command may begin here: at the start of the text, after a semicolon, an opening parenthesis or a line feed
	/// (blanks skipped), directly after the closing parenthesis that ended an arm's pattern, or after the empty
	/// parentheses of a function definition.
	/// </summary>
	private static bool IsACommandPosition(string text, int position, int armEnd)
	{
		int cursor = position;

		while (cursor > 0 && (text[cursor - 1] == _SPACE || text[cursor - 1] == _TAB))
		{
			cursor--;
		}

		return cursor == 0
			|| cursor == armEnd
			|| _COMMAND_POSITION_PREDECESSORS.Contains(text[cursor - 1])
			|| EndsAFunctionHead(text, cursor);
	}

	private static bool EndsAFunctionHead(string text, int cursor)
	{
		bool ends = false;

		if (cursor > 1 && text[cursor - 1] == _CLOSING)
		{
			int open = cursor - 2;

			while (open > 0 && (text[open] == _SPACE || text[open] == _TAB))
			{
				open--;
			}

			ends = text[open] == _OPENING && !(open > 0 && text[open - 1] == _SUBSTITUTION_SIGIL);
		}

		return ends;
	}

	private static int RunOf(string text, int position, char character)
	{
		int length = 0;

		while (position + length < text.Length && text[position + length] == character)
		{
			length++;
		}

		return length;
	}

	/// <summary>An arithmetic expression, ending at a position, or a command substitution with the parentheses open in it and its arm blocks.</summary>
	private sealed class Frame
	{
		public bool IsArithmetic { get; private init; }

		public int End { get; private init; }

		public int Depth { get; set; }

		public int ArmEnd { get; set; }

		public Stack<int> Arms { get; } = [];

		public bool IsAtAnArm => Arms.Count > 0 && Arms.Peek() == Depth;

		public static Frame ForArithmetic(int end)
		{
			return new Frame { IsArithmetic = true, End = end };
		}

		public static Frame ForASubstitution()
		{
			return new Frame { Depth = _SUBSTITUTION_OPENING_DEPTH };
		}
	}
}
