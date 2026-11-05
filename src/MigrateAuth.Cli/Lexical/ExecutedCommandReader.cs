using System.Buffers;
using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Lexical;

/// <summary>
/// WHAT A HERE-DOCUMENT'S COMMAND DOES WITH ITS BODY, as far as the words before and after the opening say.
/// </summary>
internal readonly record struct HereDocumentFeed(bool IsFedToAShell, bool WritesAScript);

/// <summary>
/// WHAT A COMMAND DOES WITH THE TEXT THAT FOLLOWS ITS WORDS: a string argument it runs as a command, its standard
/// input it reads as a script, both (a remote shell with no command) or neither.
/// </summary>
[Flags]
internal enum CommandReading
{
	None = 0,
	RunsTheNextString = 1,
	ReadsStandardInputAsCode = 2
}

/// <summary>
/// READS THE WORDS OF THE COMMAND THAT STANDS BEFORE A STRING OR A HERE-DOCUMENT OPENING, AGAINST AN
/// <see cref="ExecutedStringContext"/>. It is purely lexical and bounded: it looks back at most
/// <see cref="LexicalEnginePolicy.EXECUTED_STRING_LOOKBACK_CHARACTERS"/> characters for the command position that
/// begins the command, reads at most <see cref="LexicalEnginePolicy.EXECUTED_STRING_MAXIMUM_COMMAND_WORDS"/> words
/// from there, and trusts only the characters the scan has already put in a code zone: a blank, a separator or a
/// command word inside a string or a comment is not one. A command it cannot read this way is no executed-string
/// context, which leaves the string a string zone.
///
/// The reader names no language: every word, separator, blank, quote, option introducer, end-of-options word, pipe
/// operator, descriptor and redirection character it compares against is the profile's declaration. What it knows of its
/// own is the line feed, which the normalized text uses for every line end, and the ASCII letters and digits. An escape pair that holds a line feed is removed from a word as the declared escape
/// removes it: it joins what stands on both sides (so it makes one word of them) and is nothing at all beside a blank.
///
/// The reader holds no state, so one is shared by every scan of the profile.
/// </summary>
internal sealed class ExecutedCommandReader
{
	private const char _LINE_FEED = '\n';
	private const int _ESCAPE_PAIR_LENGTH = 2;
	private const int _OPTION_AND_ARGUMENT_LENGTH = 2;
	private const int _SINGLE_CHARACTER_LENGTH = 1;
	private const int _MAXIMUM_SHEBANG_WORDS = 8;
	private const int _MINIMUM_OPTION_CLUSTER_LENGTH = 2;
	private const int _NO_COMMAND_START = -1;
	private const int _STRING_OPTION_LETTER_INDEX = 1;
	private const int _STRING_OPTION_LENGTH = 2;
	private const int _FIRST_INDEX = 0;
	private const string _ASCII_LETTERS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private static readonly SearchValues<char> _letters = SearchValues.Create(_ASCII_LETTERS);

	private readonly ExecutedStringContext _declaration;
	private readonly StringComparison _comparison;
	private readonly char? _escape;
	private readonly char _stringOptionLetter;
	private readonly char[] _quoteCharacters;
	private readonly char[] _listOperatorCharacters;

	internal ExecutedCommandReader(ExecutedStringContext declaration, StringComparison comparison, char? escape)
	{
		ArgumentNullException.ThrowIfNull(declaration);

		_declaration = declaration;
		_comparison = comparison;
		_escape = escape;
		_stringOptionLetter = declaration.StringOption.Length == _STRING_OPTION_LENGTH ? declaration.StringOption[_STRING_OPTION_LETTER_INDEX] : default;
		_quoteCharacters = [.. declaration.QuoteCharacters];
		_listOperatorCharacters = [.. declaration.ListOperatorCharacters];
	}

	/// <summary>
	/// Whether the quoted string that opens at the position, starting a word, is the argument that a command runs as
	/// code: an eval word's argument, the string after the string option of a declared shell or command-string word, the remote
	/// command of a remote shell.
	/// </summary>
	internal bool StartsAnExecutedString(string text, LexicalZone[] zones, int quoteStart)
	{
		CommandWords? command = StartsAWord(text, zones, quoteStart) ? ReadTheCommandBefore(text, zones, quoteStart) : null;

		return command is not null && ReadTheCommand(text, command.Words, _FIRST_INDEX).HasFlag(CommandReading.RunsTheNextString);
	}

	/// <summary>
	/// What the command whose here-document opens at <paramref name="openingStart"/> and whose opening token ends at
	/// <paramref name="openingEnd"/> does with the body: it is a shell (or a remote shell with no command), or it
	/// redirects its output to a file with a script's extension, before the opening or after it on the same line.
	/// </summary>
	internal HereDocumentFeed ReadTheHereDocumentFeed(string text, LexicalZone[] zones, int openingStart, int openingEnd)
	{
		CommandWords? command = ReadTheCommandBefore(text, zones, openingStart);
		List<List<Word>> stages = SplitTheRestOfTheLineIntoStages(text, openingEnd);
		bool isFedDirectly = command is not null && ReadTheCommand(text, command.Words, _FIRST_INDEX).HasFlag(CommandReading.ReadsStandardInputAsCode);
		bool isPipedToAShell = stages.Count > _SINGLE_CHARACTER_LENGTH
			&& ReadTheCommand(text, TakeOutTheRedirections(text, stages[^1]).Words, _FIRST_INDEX).HasFlag(CommandReading.ReadsStandardInputAsCode);
		bool writesAScript = (command?.OutputTargets ?? []).Concat(ReadTheOutputTargets(text, stages[_FIRST_INDEX])).Any(HasAScriptExtension);

		return new HereDocumentFeed(isFedDirectly || isPipedToAShell, writesAScript);
	}

	/// <summary>
	/// Whether the first line of the body, after the leading tabs of a tab-stripping body, is a shebang that names a
	/// shell, directly or through a launcher word.
	/// </summary>
	internal bool BeginsWithAShellShebang(string text, int bodyStart, int bodyEnd, bool skipsLeadingTabs, char leadingTab)
	{
		bool begins = false;

		if (_declaration.ShebangOpening is string opening)
		{
			int lineStart = bodyStart;

			while (skipsLeadingTabs && lineStart < bodyEnd && text[lineStart] == leadingTab)
			{
				lineStart++;
			}

			int lineFeed = text.IndexOf(_LINE_FEED, lineStart, Math.Max(0, bodyEnd - lineStart));
			int lineEnd = lineFeed < 0 ? bodyEnd : lineFeed;
			ReadOnlySpan<char> line = text.AsSpan(lineStart, Math.Max(0, lineEnd - lineStart));

			begins = line.StartsWith(opening, StringComparison.Ordinal) && NamesAShell(line[opening.Length..]);
		}

		return begins;
	}

	private bool NamesAShell(ReadOnlySpan<char> afterTheOpening)
	{
		List<string> words = SplitOnBlanks(afterTheOpening, _MAXIMUM_SHEBANG_WORDS);
		int index = 0;
		bool launched = words.Count > 0 && IsOneOf(BaseName(words[_FIRST_INDEX]), _declaration.ShebangLauncherWords);

		if (launched)
		{
			index = _SINGLE_CHARACTER_LENGTH;

			while (index < words.Count && (IsOption(words[index]) || IsAssignment(words[index])))
			{
				index++;
			}
		}

		return index < words.Count && IsOneOf(BaseName(words[index]), _declaration.ShellWords);
	}

	private bool IsBlank(char character)
	{
		return _declaration.BlankCharacters.Contains(character);
	}

	private List<string> SplitOnBlanks(ReadOnlySpan<char> line, int maximumWords)
	{
		List<string> words = [];
		int position = 0;

		while (position < line.Length && words.Count < maximumWords)
		{
			while (position < line.Length && IsBlank(line[position]))
			{
				position++;
			}

			int wordStart = position;

			while (position < line.Length && !IsBlank(line[position]))
			{
				position++;
			}

			if (position > wordStart)
			{
				words.Add(line[wordStart..position].ToString());
			}
		}

		return words;
	}

	private ReadOnlySpan<char> BaseName(string path)
	{
		int separator = path.LastIndexOf(_declaration.PathSeparator);

		return path.AsSpan(separator + _SINGLE_CHARACTER_LENGTH);
	}

	private bool HasAScriptExtension(string target)
	{
		string trimmed = target.Trim(_quoteCharacters).TrimEnd(_listOperatorCharacters);

		return _declaration.ScriptFileExtensions.Any(extension =>
			trimmed.Length > extension.Length && trimmed.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Whether the position starts a word: an unescaped blank stands before it (an escaped blank belongs to the word before), once the escape pairs that hold a line feed are
	/// stepped back over (the shell removes them, so they belong to no word of their own and join nothing to a blank).
	/// </summary>
	private bool StartsAWord(string text, LexicalZone[] zones, int position)
	{
		int cursor = position;

		while (cursor >= _ESCAPE_PAIR_LENGTH && text[cursor - _SINGLE_CHARACTER_LENGTH] == _LINE_FEED && IsEscaped(text, zones, cursor - _SINGLE_CHARACTER_LENGTH))
		{
			cursor -= _ESCAPE_PAIR_LENGTH;
		}

		bool followsABlank = cursor > 0
			&& IsBlank(text[cursor - _SINGLE_CHARACTER_LENGTH])
			&& !IsEscaped(text, zones, cursor - _SINGLE_CHARACTER_LENGTH);

		return cursor == 0 || followsABlank;
	}

	/// <summary>
	/// The command that stands before the position, as its words after its redirections, licensing words and
	/// assignments are taken out, with the output targets of its redirections; null where no command position lies
	/// within the look-back bound, a word lies in a comment, or the command has more words than the bound.
	/// </summary>
	private CommandWords? ReadTheCommandBefore(string text, LexicalZone[] zones, int position)
	{
		int start = FindTheCommandStart(text, zones, position);
		List<Word>? raw = start == _NO_COMMAND_START ? null : SplitIntoWords(text, zones, start, position);

		return raw is null ? null : TakeOutTheRedirections(text, raw);
	}

	private int FindTheCommandStart(string text, LexicalZone[] zones, int position)
	{
		int limit = Math.Max(0, position - LexicalEnginePolicy.EXECUTED_STRING_LOOKBACK_CHARACTERS);
		int start = _NO_COMMAND_START;

		for (int cursor = position - _SINGLE_CHARACTER_LENGTH; cursor >= limit && start == _NO_COMMAND_START; cursor--)
		{
			if (zones[cursor] is LexicalZone.Code or LexicalZone.Delimiter
				&& _declaration.CommandPositionPredecessors.Contains(text[cursor])
				&& !IsEscaped(text, zones, cursor)
				&& !IsPartOfARedirection(text, cursor))
			{
				start = cursor + _SINGLE_CHARACTER_LENGTH;
			}
		}

		return start == _NO_COMMAND_START && limit == 0 ? 0 : start;
	}

	/// <summary>
	/// A descriptor duplication or a clobber holds a separator character that separates nothing: the character
	/// stands directly after a redirection character and is one of the declared redirection modifiers.
	/// </summary>
	private bool IsPartOfARedirection(string text, int position)
	{
		return position > 0
			&& _declaration.RedirectionModifierCharacters.Contains(text[position])
			&& _declaration.RedirectionCharacters.Contains(text[position - _SINGLE_CHARACTER_LENGTH]);
	}

	/// <summary>
	/// Whether the character at the position is escaped. The characters directly before it that are the escape character
	/// form a run, and the character is escaped exactly when the run is of odd length: the last escape of an odd run has
	/// no escape of its own to be escaped by, so it escapes the next character, while the escapes of an even run escape
	/// one another in pairs. An escape inside a comment is text.
	/// </summary>
	private bool IsEscaped(string text, LexicalZone[] zones, int position)
	{
		int escapesBefore = 0;

		while (_escape is char escape && position - escapesBefore > 0 && text[position - escapesBefore - _SINGLE_CHARACTER_LENGTH] == escape)
		{
			escapesBefore++;
		}

		bool runIsOdd = escapesBefore % _ESCAPE_PAIR_LENGTH != 0;

		return runIsOdd && zones[position - _SINGLE_CHARACTER_LENGTH] is not LexicalZone.Comment;
	}

	private List<Word>? SplitIntoWords(string text, LexicalZone[] zones, int start, int end)
	{
		List<Word>? words = [];
		int position = SkipBlanks(text, zones, start, end);

		while (position < end && words is not null)
		{
			int wordStart = position;
			int contentEnd = position;

			while (position < end && !IsABlankAt(text, zones, position))
			{
				if (zones[position] is LexicalZone.Comment)
				{
					words = null;
					position = end;
				}
				else
				{
					int step = IsAnEscapePairAt(text, zones, position, end) ? _ESCAPE_PAIR_LENGTH : _SINGLE_CHARACTER_LENGTH;
					bool isARemovedPair = step == _ESCAPE_PAIR_LENGTH && text[position + _SINGLE_CHARACTER_LENGTH] == _LINE_FEED;

					position += step;
					contentEnd = isARemovedPair && (position >= end || IsABlankAt(text, zones, position)) ? contentEnd : position;
				}
			}

			if (words is not null)
			{
				words = words.Count >= LexicalEnginePolicy.EXECUTED_STRING_MAXIMUM_COMMAND_WORDS ? null : words;
				words?.Add(new Word(wordStart, contentEnd - wordStart));
				position = SkipBlanks(text, zones, position, end);
			}
		}

		return words;
	}

	private bool IsAnEscapePairAt(string text, LexicalZone[] zones, int position, int end)
	{
		return _escape is char escape
			&& text[position] == escape
			&& zones[position] is LexicalZone.Code or LexicalZone.Delimiter
			&& position + _SINGLE_CHARACTER_LENGTH < end;
	}

	/// <summary>Skips blanks, and the escape pairs that hold a line feed, which the shell removes and which are nothing beside a blank.</summary>
	private int SkipBlanks(string text, LexicalZone[] zones, int position, int end)
	{
		int cursor = position;

		while (cursor < end && (IsABlankAt(text, zones, cursor) || IsARemovedPairAt(text, zones, cursor, end)))
		{
			cursor += IsABlankAt(text, zones, cursor) ? _SINGLE_CHARACTER_LENGTH : _ESCAPE_PAIR_LENGTH;
		}

		return cursor;
	}

	private bool IsARemovedPairAt(string text, LexicalZone[] zones, int position, int end)
	{
		return IsAnEscapePairAt(text, zones, position, end) && text[position + _SINGLE_CHARACTER_LENGTH] == _LINE_FEED;
	}

	/// <summary>A declared blank in code. A blank inside a string is part of its word.</summary>
	private bool IsABlankAt(string text, LexicalZone[] zones, int position)
	{
		return zones[position] is LexicalZone.Code or LexicalZone.Delimiter && IsBlank(text[position]);
	}

	private CommandWords TakeOutTheRedirections(string text, List<Word> raw)
	{
		List<Word> words = [];
		List<string> targets = [];

		for (int index = 0; index < raw.Count; index++)
		{
			ReadOnlySpan<char> word = raw[index].SpanIn(text);
			Redirection redirection = MeasureTheRedirection(word);

			if (!redirection.IsARedirection)
			{
				words.Add(raw[index]);
			}
			else
			{
				string? target = redirection.TargetIsAttached
					? word[redirection.OperatorEnd..].ToString()
					: redirection.ExpectsATarget && index + _SINGLE_CHARACTER_LENGTH < raw.Count
						? raw[index + _SINGLE_CHARACTER_LENGTH].SpanIn(text).ToString()
						: null;

				index += redirection.ExpectsATarget && !redirection.TargetIsAttached && target is not null ? _SINGLE_CHARACTER_LENGTH : 0;

				if (redirection.IsAnOutputToAFile && target is not null)
				{
					targets.Add(target);
				}
			}
		}

		return new CommandWords(StripTheLeadingLicensingWords(text, words), targets);
	}

	private List<Word> StripTheLeadingLicensingWords(string text, List<Word> words)
	{
		int skipped = 0;

		while (skipped < words.Count
			&& (IsOneOf(words[skipped].SpanIn(text), _declaration.LicensingWords) || IsAssignment(words[skipped].SpanIn(text))))
		{
			skipped++;
		}

		return words.GetRange(skipped, words.Count - skipped);
	}

	/// <summary>
	/// The shape of a word that is a redirection: optional digits, a run of redirection characters, optionally one
	/// redirection modifier, then the target attached to it. A word of only those characters takes the next word as
	/// its target, unless its modifier is the descriptor duplication.
	/// </summary>
	private Redirection MeasureTheRedirection(ReadOnlySpan<char> word)
	{
		int digits = 0;

		while (digits < word.Length && char.IsAsciiDigit(word[digits]))
		{
			digits++;
		}

		int cursor = digits;

		while (cursor < word.Length && _declaration.RedirectionCharacters.Contains(word[cursor]))
		{
			cursor++;
		}

		bool isARedirection = cursor > digits;
		ReadOnlySpan<char> operatorText = word[digits..cursor];
		bool hasAModifier = isARedirection && cursor < word.Length && _declaration.RedirectionModifierCharacters.Contains(word[cursor]);
		int operatorEnd = hasAModifier ? cursor + _SINGLE_CHARACTER_LENGTH : cursor;
		bool endsInADuplication = hasAModifier && word[cursor] == _declaration.DescriptorDuplicationCharacter;
		bool descriptorIsStandardOutput = digits == 0 || word[..digits].Equals(_declaration.StandardOutputDescriptor, StringComparison.Ordinal);

		return new Redirection(
			isARedirection,
			operatorEnd,
			isARedirection && operatorEnd < word.Length && !endsInADuplication,
			isARedirection && operatorEnd == word.Length && !endsInADuplication,
			isARedirection && !hasAModifier && descriptorIsStandardOutput && IsOneOf(operatorText, _declaration.OutputRedirections));
	}

	/// <summary>
	/// The output targets of the redirections among the words of the first stage that follows the opening on its line,
	/// read with the quotes and escapes of the declaration: a quoted or escaped word is one word whatever it holds, so a
	/// redirection character inside a quoted argument is no redirection.
	/// </summary>
	private List<string> ReadTheOutputTargets(string text, List<Word> stage)
	{
		List<string> targets = [];

		for (int index = 0; index < stage.Count; index++)
		{
			string word = stage[index].SpanIn(text).ToString();
			Redirection redirection = MeasureTheRedirection(word);

			if (redirection.IsAnOutputToAFile && redirection.TargetIsAttached)
			{
				targets.Add(word[redirection.OperatorEnd..]);
			}
			else if (redirection.IsAnOutputToAFile && redirection.ExpectsATarget && index + _SINGLE_CHARACTER_LENGTH < stage.Count)
			{
				targets.Add(stage[index + _SINGLE_CHARACTER_LENGTH].SpanIn(text).ToString());
			}
		}

		return targets;
	}

	/// <summary>
	/// The words that follow the opening on its line, up to a command-ending operator or comment, split into the stages of
	/// a pipeline at the declared pipe operators (a word that begins with one starts the next stage, and the rest of that
	/// word is its first word). The first stage holds the opening command's own words, the last the command the body is
	/// piped to. Only the opening's own line is read.
	/// </summary>
	private List<List<Word>> SplitTheRestOfTheLineIntoStages(string text, int openingEnd)
	{
		int lineFeed = text.IndexOf(_LINE_FEED, openingEnd);
		int lineEnd = Math.Min(lineFeed < 0 ? text.Length : lineFeed, openingEnd + LexicalEnginePolicy.EXECUTED_STRING_LOOKBACK_CHARACTERS);
		List<List<Word>> stages = [[]];
		bool ended = false;

		foreach (Word word in SplitTheRestOfTheLine(text, openingEnd, lineEnd))
		{
			ReadOnlySpan<char> span = word.SpanIn(text);
			string? pipe = FindThePipeOperatorAtTheStart(span);

			ended = ended || EndsTheCommand(span, pipe is not null);

			if (!ended && pipe is not null)
			{
				stages.Add([]);

				if (span.Length > pipe.Length)
				{
					stages[^1].Add(new Word(word.Start + pipe.Length, word.Length - pipe.Length));
				}
			}
			else if (!ended)
			{
				stages[^1].Add(word);
			}
		}

		return stages;
	}

	private string? FindThePipeOperatorAtTheStart(ReadOnlySpan<char> word)
	{
		string? found = null;

		foreach (string pipe in _declaration.PipeOperators)
		{
			found = word.StartsWith(pipe, StringComparison.Ordinal) && (found is null || pipe.Length > found.Length) ? pipe : found;
		}

		return found;
	}

	private List<Word> SplitTheRestOfTheLine(string text, int start, int end)
	{
		List<Word> words = [];
		int position = start;

		while (position < end && words.Count < LexicalEnginePolicy.EXECUTED_STRING_MAXIMUM_COMMAND_WORDS)
		{
			while (position < end && IsBlank(text[position]))
			{
				position++;
			}

			int wordStart = position;

			while (position < end && !IsBlank(text[position]))
			{
				position = StepOverACharacterOfTheLine(text, position, end);
			}

			if (position > wordStart)
			{
				words.Add(new Word(wordStart, Math.Min(position, end) - wordStart));
			}
		}

		return words;
	}

	/// <summary>One character of a word on the rest of the line, or a whole escape pair, or a whole quoted run that closes on its own quote.</summary>
	private int StepOverACharacterOfTheLine(string text, int position, int end)
	{
		char character = text[position];
		int next = position + _SINGLE_CHARACTER_LENGTH;

		if (_escape is char escape && character == escape)
		{
			next = position + _ESCAPE_PAIR_LENGTH;
		}
		else if (_quoteCharacters.Contains(character))
		{
			bool honoursTheEscape = _escape is not null && _declaration.ReadableBodies.Any(body => body.QuoteOpening == character.ToString() && body.EscapableCharacters.Length > 0);

			next = position + _SINGLE_CHARACTER_LENGTH;

			while (next < end && text[next] != character)
			{
				next += honoursTheEscape && text[next] == _escape ? _ESCAPE_PAIR_LENGTH : _SINGLE_CHARACTER_LENGTH;
			}

			next += _SINGLE_CHARACTER_LENGTH;
		}

		return next;
	}

	/// <summary>
	/// Whether a word ends the words of the command: a list operator or a comment, or a pair such as the or-list. A word
	/// that begins with a pipe operator is a stage break, not an end, unless a declared pair begins it.
	/// </summary>
	private bool EndsTheCommand(ReadOnlySpan<char> word, bool beginsWithAPipe)
	{
		bool beginsAPair = false;

		foreach (string pair in _declaration.ListOperatorPairs)
		{
			beginsAPair = beginsAPair || word.StartsWith(pair, StringComparison.Ordinal);
		}

		bool beginsAListOperator = _listOperatorCharacters.Contains(word[_FIRST_INDEX]) && !beginsWithAPipe;

		return beginsAPair || beginsAListOperator || word[_FIRST_INDEX] == _declaration.CommentStart;
	}

	/// <summary>What the command whose words start at the index does with the string or the standard input that follows them.</summary>
	private CommandReading ReadTheCommand(string text, List<Word> words, int index)
	{
		int cursor = SkipThePrivilegeWords(text, words, index);
		CommandReading reading = CommandReading.None;

		if (cursor < words.Count)
		{
			ReadOnlySpan<char> word = words[cursor].SpanIn(text);

			if (IsACommandNamed(word, _declaration.EvalWords))
			{
				reading = CommandReading.RunsTheNextString;
			}
			else if (IsACommandNamed(word, _declaration.ShellWords))
			{
				reading = ReadAShell(text, words, cursor + _SINGLE_CHARACTER_LENGTH);
			}
			else if (IsACommandNamed(word, _declaration.CommandStringWords))
			{
				reading = words.Count > cursor + _SINGLE_CHARACTER_LENGTH
					&& IsAStringOption(words[^1].SpanIn(text))
					&& !HasTheEndOfOptionsBeforeTheLast(text, words, cursor)
						? CommandReading.RunsTheNextString
						: CommandReading.None;
			}
			else if (IsACommandNamed(word, _declaration.RemoteShellWords))
			{
				reading = ReadARemoteShell(text, words, cursor + _SINGLE_CHARACTER_LENGTH);
			}
		}

		return reading;
	}

	private bool HasTheEndOfOptionsBeforeTheLast(string text, List<Word> words, int commandIndex)
	{
		return words.Skip(commandIndex + _SINGLE_CHARACTER_LENGTH).SkipLast(_SINGLE_CHARACTER_LENGTH).Any(word => IsTheEndOfOptions(word.SpanIn(text)));
	}

	/// <summary>Skips the privilege words of the command, with the options of each and the assignments that follow.</summary>
	private int SkipThePrivilegeWords(string text, List<Word> words, int index)
	{
		int cursor = index;

		while (cursor < words.Count && IsACommandNamed(words[cursor].SpanIn(text), _declaration.PrivilegeWords))
		{
			cursor++;

			bool optionsEnded = false;

			while (cursor < words.Count && !optionsEnded && (IsOption(words[cursor].SpanIn(text)) || IsAssignment(words[cursor].SpanIn(text))))
			{
				optionsEnded = IsTheEndOfOptions(words[cursor].SpanIn(text));
				cursor += IsOneOf(words[cursor].SpanIn(text), _declaration.PrivilegeOptionsWithArgument) ? _OPTION_AND_ARGUMENT_LENGTH : _SINGLE_CHARACTER_LENGTH;
			}
		}

		return cursor;
	}

	/// <summary>
	/// A shell whose words from the index to the end are options runs the next string if the last of them is the string
	/// option, and reads standard input as a script if none is; a word that is not an option is a script file, which
	/// leaves standard input to the script, unless the standard-input option came before it.
	/// </summary>
	private CommandReading ReadAShell(string text, List<Word> words, int from)
	{
		int cursor = from;
		bool stringOptionSeen = false;
		bool lastWasTheStringOption = false;
		bool standardInputOptionSeen = false;
		bool readsAScriptFile = false;
		bool optionsEnded = false;

		while (cursor < words.Count && !readsAScriptFile)
		{
			ReadOnlySpan<char> word = words[cursor].SpanIn(text);
			bool isAnOption = !optionsEnded && IsOption(word);
			bool takesAnArgument = isAnOption && IsOneOf(word, _declaration.ShellOptionsWithArgument);

			optionsEnded = optionsEnded || (isAnOption && IsTheEndOfOptions(word));

			lastWasTheStringOption = isAnOption && !takesAnArgument && IsAStringOption(word);
			stringOptionSeen = stringOptionSeen || lastWasTheStringOption;
			standardInputOptionSeen = standardInputOptionSeen
				|| (isAnOption && _declaration.StdinOption is string standardInputOption && word.Equals(standardInputOption, _comparison));
			readsAScriptFile = !isAnOption && !standardInputOptionSeen;
			cursor += takesAnArgument ? _OPTION_AND_ARGUMENT_LENGTH : _SINGLE_CHARACTER_LENGTH;
		}

		return readsAScriptFile || cursor > words.Count
			? CommandReading.None
			: lastWasTheStringOption
				? CommandReading.RunsTheNextString
				: stringOptionSeen ? CommandReading.None : CommandReading.ReadsStandardInputAsCode;
	}

	/// <summary>
	/// A remote shell: options (some with an argument), then the host. A string right after the host is the remote
	/// command and a here-document with no remote command is fed to the remote shell; a remote command that is itself
	/// words is read as a command of its own.
	/// </summary>
	private CommandReading ReadARemoteShell(string text, List<Word> words, int from)
	{
		int cursor = from;

		bool optionsEnded = false;

		while (cursor < words.Count && !optionsEnded && IsOption(words[cursor].SpanIn(text)))
		{
			optionsEnded = IsTheEndOfOptions(words[cursor].SpanIn(text));
			cursor += IsOneOf(words[cursor].SpanIn(text), _declaration.RemoteOptionsWithArgument) ? _OPTION_AND_ARGUMENT_LENGTH : _SINGLE_CHARACTER_LENGTH;
		}

		int hostEnd = cursor + _SINGLE_CHARACTER_LENGTH;

		return cursor >= words.Count
			? CommandReading.None
			: hostEnd == words.Count
				? CommandReading.RunsTheNextString | CommandReading.ReadsStandardInputAsCode
				: ReadTheCommand(text, words, hostEnd);
	}

	/// <summary>
	/// An option cluster that carries the string option's letter: the option introducer and letters only, one of which is
	/// the string option's (a long option holds a second introducer, which is no letter).
	/// </summary>
	private bool IsAStringOption(ReadOnlySpan<char> word)
	{
		bool isACluster = word.Length >= _MINIMUM_OPTION_CLUSTER_LENGTH && word[_FIRST_INDEX] == _declaration.StringOption[_FIRST_INDEX];
		bool holdsOnlyLetters = isACluster && word[_SINGLE_CHARACTER_LENGTH..].IndexOfAnyExcept(_letters) < 0;

		return holdsOnlyLetters && word[_SINGLE_CHARACTER_LENGTH..].IndexOf(_stringOptionLetter) >= 0;
	}

	private bool IsTheEndOfOptions(ReadOnlySpan<char> word)
	{
		return word.Equals(_declaration.EndOfOptionsWord, _comparison);
	}

	private bool IsOption(ReadOnlySpan<char> word)
	{
		return word.Length > 0 && _declaration.OptionIntroducers.Contains(word[_FIRST_INDEX]);
	}

	private bool IsAssignment(ReadOnlySpan<char> word)
	{
		int cursor = 0;

		while (cursor < word.Length && (char.IsAsciiLetterOrDigit(word[cursor]) || word[cursor] == _declaration.NameConnectorCharacter))
		{
			cursor++;
		}

		return cursor > 0 && cursor < word.Length && word[cursor] == _declaration.AssignmentOperator && !char.IsAsciiDigit(word[_FIRST_INDEX]);
	}

	/// <summary>
	/// Whether the word names one of the commands, either as the bare word or as a path that ends in it: the base name after
	/// the last declared path separator is compared, so a word that ends in the separator (a directory) names nothing.
	/// </summary>
	private bool IsACommandNamed(ReadOnlySpan<char> word, ImmutableArray<string> commands)
	{
		int separator = word.LastIndexOf(_declaration.PathSeparator);
		ReadOnlySpan<char> name = word[(separator + _SINGLE_CHARACTER_LENGTH)..];

		return name.Length > 0 && IsOneOf(name, commands);
	}

	private bool IsOneOf(ReadOnlySpan<char> word, ImmutableArray<string> words)
	{
		bool isOne = false;

		foreach (string candidate in words)
		{
			isOne = isOne || word.Equals(candidate, _comparison);
		}

		return isOne;
	}

	/// <summary>One word of a command: where it starts and how long it is, in the text it was read from.</summary>
	private readonly record struct Word(int Start, int Length)
	{
		internal ReadOnlySpan<char> SpanIn(string text)
		{
			return text.AsSpan(Start, Length);
		}
	}

	/// <summary>The words of a command, and the targets of its output redirections to a file.</summary>
	private sealed record CommandWords(List<Word> Words, List<string> OutputTargets);

	/// <summary>
	/// The reading of one word as a redirection: whether it is one, where its operator ends, whether the target is
	/// attached to it, whether the next word is its target, and whether it writes standard output to a file.
	/// </summary>
	private readonly record struct Redirection(
		bool IsARedirection,
		int OperatorEnd,
		bool TargetIsAttached,
		bool ExpectsATarget,
		bool IsAnOutputToAFile);
}
