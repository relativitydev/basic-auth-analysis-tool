using System.Reflection;

using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The static sentence every launch starts with, and the prompt argument built from it. The sentence is a
/// compiled constant that satisfies the allow-list a wrapper command line is held to, begins with a letter
/// and never with a character an agent reads as a command, a shell escape, a file mention or a memory note.
/// The path after it is framed so that nothing inside the path can close the frame.
/// </summary>
public sealed class LaunchSentenceTests
{
	private static readonly char[] _forbiddenFirstCharacters = ['/', '!', '@', '#'];

	[Fact]
	public void TheSentenceIsACompiledConstant()
	{
		FieldInfo field = typeof(ExternalAgentLauncherBase).GetField(nameof(ExternalAgentLauncherBase.LAUNCH_SENTENCE))!;

		Assert.True(field.IsLiteral);
		Assert.Equal(typeof(string), field.FieldType);
	}

	[Fact]
	public void TheSentenceSatisfiesTheWrapperAllowList()
	{
		Assert.True(AgentCommandLineSafety.IsAllowedText(ExternalAgentLauncherBase.LAUNCH_SENTENCE));
	}

	[Fact]
	public void TheSentenceBeginsWithALetterAndNeverWithACharacterAnAgentInterprets()
	{
		char first = ExternalAgentLauncherBase.LAUNCH_SENTENCE[0];

		Assert.True(char.IsLetter(first));
		Assert.DoesNotContain(first, _forbiddenFirstCharacters);
	}

	[Fact]
	public void TheSentenceHoldsOnlyLettersSpacesAndOneColon()
	{
		string sentence = ExternalAgentLauncherBase.LAUNCH_SENTENCE;

		Assert.All(sentence, character => Assert.True(char.IsAsciiLetter(character) || character is ' ' or ':'));
		Assert.Equal(1, sentence.Count(character => character == ':'));
	}

	[Fact]
	public void TheSentenceNamesNoPathAndNoFlag()
	{
		string sentence = ExternalAgentLauncherBase.LAUNCH_SENTENCE;

		Assert.DoesNotContain("-", sentence, StringComparison.Ordinal);
		Assert.DoesNotContain("\\", sentence, StringComparison.Ordinal);
		Assert.DoesNotContain("/", sentence, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePromptArgumentIsTheSentenceASpaceAndThePathInACodeSpan()
	{
		string path = @"C:\audit docs\ai-assist-launch-0123456789abcdef0123456789abcdef\pack.md";

		Assert.Equal(
			$"{ExternalAgentLauncherBase.LAUNCH_SENTENCE} ` {path} `",
			ExternalAgentLauncherBase.BuildPromptArgument(path));
	}

	[Theory]
	[InlineData("a`b", "`` a`b ``")]
	[InlineData("a``b", "``` a``b ```")]
	[InlineData("a```b", "```` a```b ````")]
	[InlineData("`start", "`` `start ``")]
	public void ThePathCannotCloseItsFrame(string path, string expectedSpan)
	{
		Assert.Equal(
			$"{ExternalAgentLauncherBase.LAUNCH_SENTENCE} {expectedSpan}",
			ExternalAgentLauncherBase.BuildPromptArgument(path));
	}

	[Fact]
	public void ThePromptArgumentOfAnAllowedPathIsAllowed()
	{
		Assert.True(AgentCommandLineSafety.IsAllowedText(ExternalAgentLauncherBase.BuildPromptArgument(@"D:\docs\pack.md")));
	}

	[Fact]
	public void APathIsRequired()
	{
		Assert.Throws<ArgumentException>(() => ExternalAgentLauncherBase.BuildPromptArgument(" "));
		Assert.Throws<ArgumentNullException>(() => ExternalAgentLauncherBase.BuildPromptArgument(null!));
	}
}
