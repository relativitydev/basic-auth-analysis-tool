using System.Globalization;
using System.Text;

namespace MigrateAuth.Cli.Agent;

/// <summary>
/// WHAT MAY BE PUT IN A COMMAND LINE THAT THE WINDOWS COMMAND INTERPRETER READS, decided by an allow-list.
///
/// A script wrapper runs as <c>cmd.exe /d /s /c "..."</c>, and the interpreter gives meaning to many
/// characters a deny-list would have to name one by one, and to look-alikes of them. So the rule is the
/// other way round: every character of the wrapper's path and of the launch copy's path (the variable
/// components, not the built line, whose own quotes are structural) must be a letter or a decimal digit of any
/// script, a space, one of <c>\ / : . - _</c>, or a backtick. Modifier letters, letter numbers and other
/// numbers are not in the list, because some of them look like quote characters. Anything else refuses the
/// launch through a wrapper. Accented letters in a user name stay launchable.
///
/// A command line longer than <see cref="MAXIMUM_WRAPPER_COMMAND_LINE_LENGTH"/> characters is refused too.
///
/// The static sentence of the launch is a compiled constant tested against this same rule.
/// </summary>
public static class AgentCommandLineSafety
{
	/// <summary>The longest wrapper command line, in characters, the launch accepts.</summary>
	public const int MAXIMUM_WRAPPER_COMMAND_LINE_LENGTH = 8000;

	private const char _SPACE = ' ';
	private const char _QUOTE = '"';
	private const string _INTERPRETER_SWITCHES = "/d /s /c ";
	private const string _ALLOWED_PUNCTUATION = "\\/:.-_`";

	/// <summary>Whether <paramref name="character"/> is on the allow-list.</summary>
	public static bool IsAllowedCharacter(Rune character)
	{
		UnicodeCategory category = Rune.GetUnicodeCategory(character);
		bool allowedCategory = category is UnicodeCategory.UppercaseLetter
			or UnicodeCategory.LowercaseLetter
			or UnicodeCategory.TitlecaseLetter
			or UnicodeCategory.OtherLetter
			or UnicodeCategory.DecimalDigitNumber;

		return allowedCategory
			|| character.Value == _SPACE
			|| (character.IsAscii && _ALLOWED_PUNCTUATION.Contains((char)character.Value));
	}

	/// <summary>
	/// Whether every character of <paramref name="text"/> is on the allow-list. An unpaired surrogate
	/// enumerates as the replacement character, which is not on it, so it is refused.
	/// </summary>
	public static bool IsAllowedText(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		bool allowed = true;

		foreach (Rune character in text.EnumerateRunes())
		{
			allowed &= IsAllowedCharacter(character);
		}

		return allowed;
	}

	/// <summary>Whether <paramref name="commandLine"/> is no longer than the limit.</summary>
	public static bool IsWithinLengthLimit(string commandLine)
	{
		ArgumentNullException.ThrowIfNull(commandLine);

		return commandLine.Length <= MAXIMUM_WRAPPER_COMMAND_LINE_LENGTH;
	}

	/// <summary>
	/// The command line the interpreter receives for <paramref name="wrapperPath"/> and its arguments:
	/// the switches, then the whole command in one pair of quotes, with the wrapper and each argument in
	/// quotes of their own. The interpreter's own <c>/s</c> removes the outer pair.
	/// </summary>
	public static string BuildWrapperCommandLine(string wrapperPath, IReadOnlyList<string> arguments)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(wrapperPath);
		ArgumentNullException.ThrowIfNull(arguments);

		StringBuilder commandLine = new(_INTERPRETER_SWITCHES);

		commandLine.Append(_QUOTE).Append(_QUOTE).Append(wrapperPath).Append(_QUOTE);

		foreach (string argument in arguments)
		{
			commandLine.Append(_SPACE).Append(_QUOTE).Append(argument).Append(_QUOTE);
		}

		return commandLine.Append(_QUOTE).ToString();
	}

	/// <summary>
	/// Whether a launch through a wrapper is safe: the wrapper path and the copy path are on the allow-list
	/// and the command line built from them is within the limit.
	/// </summary>
	public static bool IsWrapperLaunchSafe(string wrapperPath, string copyPath, IReadOnlyList<string> arguments)
	{
		ArgumentNullException.ThrowIfNull(wrapperPath);
		ArgumentNullException.ThrowIfNull(copyPath);

		return IsAllowedText(wrapperPath)
			&& IsAllowedText(copyPath)
			&& IsWithinLengthLimit(BuildWrapperCommandLine(wrapperPath, arguments));
	}
}
