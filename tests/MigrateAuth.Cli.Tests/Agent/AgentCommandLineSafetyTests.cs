using System.Text;

using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The allow-list a wrapper command line is held to: letters and digits of any script, a space, the
/// five punctuation characters of a Windows path, and the backtick. Everything else refuses, which covers the
/// characters the command interpreter reads specially and the look-alikes of them.
/// </summary>
public sealed class AgentCommandLineSafetyTests
{
	private const string _PLAIN_WRAPPER_PATH = @"C:\tools\agent-1.0\claude.cmd";
	private const string _PLAIN_COPY_PATH = @"D:\audit docs\ai-assist-launch-00000000000000000000000000000000\pack.md";
	private const string _ALLOWED_PUNCTUATION = "\\/:.-_` ";
	private const int _LONG_PATH_PADDING = 100;
	private const int _PINNED_COMMAND_LINE_LIMIT = 8000;

	[Theory]
	[InlineData("abcXYZ")]
	[InlineData("0123456789")]
	[InlineData("C:\\Users\\Jos\u00E9 Mu\u00F1oz\\docs")]
	[InlineData("\u4E2D\u6587\u6587\u4EF6")]
	[InlineData("\u0434\u043E\u043A\u0443\u043C\u0435\u043D\u0442")]
	[InlineData("a b")]
	[InlineData("a\\b/c:d.e-f_g")]
	[InlineData("a`b")]
	[InlineData("\uFF21\uFF41\uFF11")]
	public void TextOfAllowedCharactersIsAllowed(string text)
	{
		Assert.True(AgentCommandLineSafety.IsAllowedText(text));
	}

	[Theory]
	[InlineData("%")]
	[InlineData("!")]
	[InlineData("^")]
	[InlineData("&")]
	[InlineData("|")]
	[InlineData("<")]
	[InlineData(">")]
	[InlineData("\"")]
	[InlineData("(")]
	[InlineData(")")]
	[InlineData(",")]
	[InlineData(";")]
	[InlineData("=")]
	[InlineData("'")]
	[InlineData("*")]
	[InlineData("?")]
	[InlineData("\t")]
	[InlineData("\r")]
	[InlineData("\n")]
	[InlineData("\0")]
	public void EachCharacterTheDenyListWouldNameIsRefused(string character)
	{
		Assert.False(AgentCommandLineSafety.IsAllowedText($"a{character}b"));
	}

	[Theory]
	[InlineData("\uFF05", "fullwidth percent")]
	[InlineData("\uFF02", "fullwidth quotation mark")]
	[InlineData("\uFF06", "fullwidth ampersand")]
	[InlineData("\u02BA", "modifier letter double prime")]
	[InlineData("\u02B9", "modifier letter prime")]
	[InlineData("\u00B2", "superscript two, an other number")]
	[InlineData("\u2160", "roman numeral one, a letter number")]
	[InlineData("\u2019", "right single quotation mark")]
	[InlineData("\u201C", "left double quotation mark")]
	[InlineData("\uFF3C", "fullwidth reverse solidus")]
	public void LookAlikesOfSpecialCharactersAreRefused(string character, string description)
	{
		Assert.False(AgentCommandLineSafety.IsAllowedText($"a{character}b"), description);
	}

	[Fact]
	public void AFullwidthLetterAndDigitAreLettersAndDigitsAndAreAllowed()
	{
		Assert.True(AgentCommandLineSafety.IsAllowedText("\uFF21\uFF11"));
	}

	[Fact]
	public void AnAccentedUserNameStaysLaunchable()
	{
		Assert.True(AgentCommandLineSafety.IsAllowedText("C:\\Users\\Jos\u00C9 Mu\u00F1oz\\docs"));
	}

	[Fact]
	public void ALoneSurrogateIsRefusedAndAPairIsAllowedOnlyWhenItIsALetter()
	{
		string letterOutsideTheBasicPlane = char.ConvertFromUtf32(0x20000);
		string emoji = char.ConvertFromUtf32(0x1F600);

		Assert.False(AgentCommandLineSafety.IsAllowedText("a\uD800b"));
		Assert.False(AgentCommandLineSafety.IsAllowedText("a\uDC00b"));
		Assert.True(AgentCommandLineSafety.IsAllowedText($"a{letterOutsideTheBasicPlane}b"));
		Assert.False(AgentCommandLineSafety.IsAllowedText($"a{emoji}b"));
	}

	[Fact]
	public void EveryAsciiCharacterIsDecidedByTheListAndNothingElse()
	{
		for (int code = 0; code <= sbyte.MaxValue; code++)
		{
			char character = (char)code;
			bool expected = char.IsAsciiLetterOrDigit(character) || _ALLOWED_PUNCTUATION.Contains(character);

			Assert.Equal(expected, AgentCommandLineSafety.IsAllowedCharacter(new Rune(character)));
		}
	}

	[Fact]
	public void TheLengthLimitIsAtEightThousandCharacters()
	{
		int limit = AgentCommandLineSafety.MAXIMUM_WRAPPER_COMMAND_LINE_LENGTH;

		Assert.Equal(_PINNED_COMMAND_LINE_LIMIT, limit);
		Assert.True(AgentCommandLineSafety.IsWithinLengthLimit(new string('a', limit)));
		Assert.False(AgentCommandLineSafety.IsWithinLengthLimit(new string('a', limit + 1)));
	}

	[Fact]
	public void ACommandLineLongerThanTheLimitIsRefusedEvenWhenEveryCharacterIsAllowed()
	{
		string longCopyPath = $@"D:\{new string('a', AgentCommandLineSafety.MAXIMUM_WRAPPER_COMMAND_LINE_LENGTH + _LONG_PATH_PADDING)}\pack.md";

		Assert.True(AgentCommandLineSafety.IsAllowedText(longCopyPath));
		Assert.False(IsLaunchSafe(longCopyPath));
	}

	[Fact]
	public void ACommandLineOfExactlyTheLimitIsAcceptedAndOneMoreCharacterIsRefused()
	{
		int baseLength = AgentCommandLineSafety.BuildWrapperCommandLine(
			_PLAIN_WRAPPER_PATH,
			[ExternalAgentLauncherBase.BuildPromptArgument(_PLAIN_COPY_PATH)]).Length;
		string exactCopyPath = $"{_PLAIN_COPY_PATH}{new string('a', AgentCommandLineSafety.MAXIMUM_WRAPPER_COMMAND_LINE_LENGTH - baseLength)}";

		Assert.True(IsLaunchSafe(exactCopyPath));
		Assert.False(IsLaunchSafe($"{exactCopyPath}a"));
	}

	[Fact]
	public void AWrapperLaunchIsSafeOnlyWhenBothPathsAreOnTheList()
	{
		IReadOnlyList<string> arguments = [ExternalAgentLauncherBase.BuildPromptArgument(_PLAIN_COPY_PATH)];

		Assert.True(AgentCommandLineSafety.IsWrapperLaunchSafe(_PLAIN_WRAPPER_PATH, _PLAIN_COPY_PATH, arguments));
		Assert.False(AgentCommandLineSafety.IsWrapperLaunchSafe(@"C:\tools\a&b\claude.cmd", _PLAIN_COPY_PATH, arguments));
		Assert.False(AgentCommandLineSafety.IsWrapperLaunchSafe(_PLAIN_WRAPPER_PATH, @"D:\audit 100%\pack.md", arguments));
	}

	[Fact]
	public void TheWrapperCommandLineHasTheSwitchesAndOneOuterPairOfQuotes()
	{
		string line = AgentCommandLineSafety.BuildWrapperCommandLine(_PLAIN_WRAPPER_PATH, ["-i", "two words"]);

		Assert.Equal($"/d /s /c \"\"{_PLAIN_WRAPPER_PATH}\" \"-i\" \"two words\"\"", line);
	}

	[Fact]
	public void TheSentenceAndTheFramedPathOfAnAllowedCopyAreAllowed()
	{
		Assert.True(AgentCommandLineSafety.IsAllowedText(ExternalAgentLauncherBase.BuildPromptArgument(_PLAIN_COPY_PATH)));
	}

	private static bool IsLaunchSafe(string copyPath)
	{
		return AgentCommandLineSafety.IsWrapperLaunchSafe(
			_PLAIN_WRAPPER_PATH,
			copyPath,
			[ExternalAgentLauncherBase.BuildPromptArgument(copyPath)]);
	}
}
