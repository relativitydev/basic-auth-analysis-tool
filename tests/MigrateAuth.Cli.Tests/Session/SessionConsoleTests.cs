using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// A byte-order mark on standard input must not turn the first typed command into an
/// unrecognized one. <c>string.Trim()</c> does not remove U+FEFF - it is a Unicode FORMAT
/// character, not whitespace - so the stripping has to be deliberate rather than incidental.
/// </summary>
public sealed class SessionConsoleTests
{
	private const char _BYTE_ORDER_MARK = (char)0xFEFF;

	[Fact]
	public void ALeadingByteOrderMarkIsStrippedFromTheFirstLine()
	{
		using StringWriter output = new();
		using StringReader input = new($"{_BYTE_ORDER_MARK}scan\nexit\n");
		SessionConsole console = new(input, output);

		string? firstLine = console.ReadLine();

		Assert.Equal("scan", firstLine);
	}

	[Fact]
	public void ALeadingByteOrderMarkIsStrippedFromAnyLineThatCarriesOne()
	{
		using StringWriter output = new();
		using StringReader input = new($"scan\n{_BYTE_ORDER_MARK}analyze\n");
		SessionConsole console = new(input, output);

		console.ReadLine();
		string? secondLine = console.ReadLine();

		Assert.Equal("analyze", secondLine);
	}

	[Fact]
	public void ALineWithNoByteOrderMarkIsReturnedUnchanged()
	{
		using StringWriter output = new();
		using StringReader input = new("scan\n");
		SessionConsole console = new(input, output);

		string? firstLine = console.ReadLine();

		Assert.Equal("scan", firstLine);
	}

	[Fact]
	public void AMarkThatIsNotTheFirstCharacterIsLeftInPlace()
	{
		using StringWriter output = new();
		using StringReader input = new($"sc{_BYTE_ORDER_MARK}an\n");
		SessionConsole console = new(input, output);

		string? firstLine = console.ReadLine();

		Assert.Equal($"sc{_BYTE_ORDER_MARK}an", firstLine);
	}

	[Fact]
	public void TheEndOfInputIsStillReportedAsNull()
	{
		using StringWriter output = new();
		using StringReader input = new(string.Empty);
		SessionConsole console = new(input, output);

		Assert.Null(console.ReadLine());
	}
}
