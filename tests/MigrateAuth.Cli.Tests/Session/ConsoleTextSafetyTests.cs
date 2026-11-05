using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// Text from a repository is untrusted on the way out: anything that can act on a renderer
/// rather than appear in it is removed, and everything else is left exactly as it was.
/// </summary>
public sealed class ConsoleTextSafetyTests
{
	private const string _ORDINARY_TEXT = "src/Client/HttpFactory.cs";
	private const char _ESCAPE_CHARACTER = '\u001B';
	private const char _NEXT_LINE_CONTROL = '\u0085';
	private const char _RIGHT_TO_LEFT_OVERRIDE = '‮';
	private const char _ZERO_WIDTH_JOINER = '‍';

	[Fact]
	public void OrdinaryTextIsReturnedUnchanged()
	{
		Assert.Equal(_ORDINARY_TEXT, ConsoleTextSafety.StripUnsafeCharacters(_ORDINARY_TEXT));
	}

	[Fact]
	public void EmptyTextIsReturnedEmpty()
	{
		Assert.Equal(string.Empty, ConsoleTextSafety.StripUnsafeCharacters(string.Empty));
	}

	[Fact]
	public void NonAsciiLettersAreKept()
	{
		const string text = "carpeta/éñü.cs";

		Assert.Equal(text, ConsoleTextSafety.StripUnsafeCharacters(text));
	}

	[Theory]
	[InlineData('\u001B')]
	[InlineData('\r')]
	[InlineData('\n')]
	[InlineData('\t')]
	[InlineData('\u0000')]
	[InlineData('\u0085')]
	public void ControlCharactersAreRemoved(char control)
	{
		string text = $"a{control}b";

		Assert.Equal("ab", ConsoleTextSafety.StripUnsafeCharacters(text));
	}

	[Theory]
	[InlineData('‮')]
	[InlineData('‍')]
	[InlineData('​')]
	public void FormattingCharactersAreRemovedAlthoughTheyAreNotControlCharacters(char formatting)
	{
		string text = $"a{formatting}b";

		Assert.Equal("ab", ConsoleTextSafety.StripUnsafeCharacters(text));
	}

	[Fact]
	public void AnEscapeSequenceLosesItsLeadingCharacterAndCannotRepaintTheTerminal()
	{
		string text = $"{_ESCAPE_CHARACTER}[2Jname";

		string safeText = ConsoleTextSafety.StripUnsafeCharacters(text);

		Assert.DoesNotContain(_ESCAPE_CHARACTER, safeText);
		Assert.Equal("[2Jname", safeText);
	}

	[Fact]
	public void SeveralDirectivesInOneNameAreAllRemoved()
	{
		string text = $"{_RIGHT_TO_LEFT_OVERRIDE}gnp.{_ZERO_WIDTH_JOINER}x{_NEXT_LINE_CONTROL}e";

		Assert.Equal("gnp.xe", ConsoleTextSafety.StripUnsafeCharacters(text));
	}

	[Fact]
	public void NullTextIsRefused()
	{
		Assert.Throws<ArgumentNullException>(() => ConsoleTextSafety.StripUnsafeCharacters(null!));
	}
}
