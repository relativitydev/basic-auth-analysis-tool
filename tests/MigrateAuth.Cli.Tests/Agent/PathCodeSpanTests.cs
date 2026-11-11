using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// The code span framing of a path: the delimiter is one backtick longer than the longest run of backticks
/// inside the path, with one space inside each end and nothing escaped.
/// </summary>
public sealed class PathCodeSpanTests
{
	[Theory]
	[InlineData("", 0)]
	[InlineData("no ticks", 0)]
	[InlineData("a`b", 1)]
	[InlineData("a``b`c", 2)]
	[InlineData("```", 3)]
	[InlineData("`a``b```c````", 4)]
	public void TheLongestRunIsFound(string text, int expectedRun)
	{
		Assert.Equal(expectedRun, PathCodeSpan.LongestBacktickRun(text));
	}

	[Theory]
	[InlineData("C:\\work\\pack.md", "` C:\\work\\pack.md `")]
	[InlineData("/home/user/pack.md", "` /home/user/pack.md `")]
	[InlineData("/a`b/pack.md", "`` /a`b/pack.md ``")]
	[InlineData("/a``b/pack.md", "``` /a``b/pack.md ```")]
	[InlineData("/a```b/pack.md", "```` /a```b/pack.md ````")]
	[InlineData("`edge", "`` `edge ``")]
	[InlineData("edge`", "`` edge` ``")]
	public void ThePathIsFramedWithAOneLongerDelimiterAndAPaddingSpace(string path, string expectedFrame)
	{
		Assert.Equal(expectedFrame, PathCodeSpan.Frame(path));
	}

	[Theory]
	[InlineData("/a`b/pack.md")]
	[InlineData("/a``b```c/pack.md")]
	[InlineData("```")]
	public void NoRunInsideThePathCanCloseTheSpan(string path)
	{
		string frame = PathCodeSpan.Frame(path);
		int delimiterLength = PathCodeSpan.LongestBacktickRun(path) + 1;
		string interior = frame[delimiterLength..^delimiterLength];

		Assert.True(PathCodeSpan.LongestBacktickRun(interior) < delimiterLength);
		Assert.Equal(delimiterLength, frame.TakeWhile(character => character == '`').Count());
	}

	[Fact]
	public void BackslashesAreNotEscaped()
	{
		string framed = PathCodeSpan.Frame("C:\\a\\b");

		Assert.Contains("C:\\a\\b", framed, StringComparison.Ordinal);
		Assert.DoesNotContain("\\\\", framed, StringComparison.Ordinal);
	}

	[Fact]
	public void ANullPathIsRefused()
	{
		Assert.Throws<ArgumentNullException>(() => PathCodeSpan.Frame(null!));
		Assert.Throws<ArgumentNullException>(() => PathCodeSpan.LongestBacktickRun(null!));
	}
}
