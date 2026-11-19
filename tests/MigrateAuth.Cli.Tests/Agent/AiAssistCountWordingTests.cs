using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// A count of one takes the singular in every ai-assist line that states a count, and the launch folder
/// line joins the destination and the folder with the separator the destination itself uses.
/// </summary>
public sealed class AiAssistCountWordingTests
{
	private const string _PACK_FILE_NAME = "ai-assist-claude-code.md";
	private const string _FOLDER_NAME = "ai-assist-launch-0123456789abcdef0123456789abcdef";
	private const int _NONE = 0;
	private const int _ONE = 1;
	private const int _TWO = 2;

	[Theory]
	[InlineData(_NONE, "0 entries")]
	[InlineData(_ONE, "1 entry")]
	[InlineData(_TWO, "2 entries")]
	public void AnEntryCountTakesTheSingularOnlyForOne(int count, string expected)
	{
		Assert.Equal(expected, AiAssistTexts.Entries(count));
	}

	[Theory]
	[InlineData(_ONE, _NONE, _NONE, _NONE, "ai-assist: the pack lists 1 entry: 0 ambiguous cases, 0 findings whose flow was defaulted, 0 others for confirmation.")]
	[InlineData(_ONE, _ONE, _NONE, _NONE, "ai-assist: the pack lists 1 entry: 1 ambiguous case, 0 findings whose flow was defaulted, 0 others for confirmation.")]
	[InlineData(_ONE, _NONE, _ONE, _NONE, "ai-assist: the pack lists 1 entry: 0 ambiguous cases, 1 finding whose flow was defaulted, 0 others for confirmation.")]
	[InlineData(_ONE, _NONE, _NONE, _ONE, "ai-assist: the pack lists 1 entry: 0 ambiguous cases, 0 findings whose flow was defaulted, 1 other for confirmation.")]
	[InlineData(_TWO, _TWO, _TWO, _TWO, "ai-assist: the pack lists 2 entries: 2 ambiguous cases, 2 findings whose flow was defaulted, 2 others for confirmation.")]
	[InlineData(_NONE, _NONE, _NONE, _NONE, "ai-assist: the pack lists 0 entries: 0 ambiguous cases, 0 findings whose flow was defaulted, 0 others for confirmation.")]
	public void EveryCountOfTheEntryCountLineFollowsItsOwnNumber(int total, int ambiguous, int defaulted, int confirmation, string expected)
	{
		Assert.Equal(expected, AiAssistTexts.EntryCountLine(total, ambiguous, defaulted, confirmation));
	}

	[Theory]
	[InlineData(_NONE, "this pack lists 0 entries.")]
	[InlineData(_ONE, "this pack lists 1 entry.")]
	[InlineData(_TWO, "this pack lists 2 entries.")]
	public void TheCostNoticeStatesTheCountWithItsNoun(int count, string expectedEnding)
	{
		string notice = AiAssistTexts.CostNotice(AgentEnvironment.ClaudeCode, count);

		Assert.EndsWith(expectedEnding, notice, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(_NONE, "It lists 0 entries.")]
	[InlineData(_ONE, "It lists 1 entry.")]
	[InlineData(_TWO, "It lists 2 entries.")]
	public void TheReportSectionStatesTheCountWithItsNoun(int count, string expectedSentence)
	{
		string body = AiAssistTexts.SectionLines(AgentEnvironment.ClaudeCode, _PACK_FILE_NAME, count)[^1];

		Assert.Contains(expectedSentence, body, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("./Outputs", "./Outputs/" + _FOLDER_NAME)]
	[InlineData("out/", "out/" + _FOLDER_NAME)]
	[InlineData("/home/user/out", "/home/user/out/" + _FOLDER_NAME)]
	public void ADestinationWithForwardSlashesKeepsThem(string destination, string expected)
	{
		Assert.Equal(expected, AiAssistTexts.LaunchFolderText(destination, _FOLDER_NAME));
	}

	[Fact]
	public void ADestinationWithBackslashesUsesABackslashOnWindowsOnly()
	{
		string expected = OperatingSystem.IsWindows() ? @"C:\out\" + _FOLDER_NAME : @"C:\out/" + _FOLDER_NAME;

		Assert.Equal(expected, AiAssistTexts.LaunchFolderText(@"C:\out", _FOLDER_NAME));
	}

	[Fact]
	public void ATrailingBackslashIsNotDoubledOnWindows()
	{
		string expected = OperatingSystem.IsWindows() ? @"out\" + _FOLDER_NAME : @"out\/" + _FOLDER_NAME;

		Assert.Equal(expected, AiAssistTexts.LaunchFolderText(@"out\", _FOLDER_NAME));
	}

	[Fact]
	public void AMixedDestinationUsesThePlatformSeparator()
	{
		char separator = OperatingSystem.IsWindows() ? Path.DirectorySeparatorChar : '/';
		string expected = @"C:\work/out" + separator + _FOLDER_NAME;

		Assert.Equal(expected, AiAssistTexts.LaunchFolderText(@"C:\work/out", _FOLDER_NAME));
	}

	[Fact]
	public void ADestinationWithNoSeparatorUsesThePlatformSeparator()
	{
		char separator = OperatingSystem.IsWindows() ? Path.DirectorySeparatorChar : '/';

		Assert.Equal("out" + separator + _FOLDER_NAME, AiAssistTexts.LaunchFolderText("out", _FOLDER_NAME));
	}
}
