using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THROUGH THE REAL PROCESS: A BYTE-ORDER MARK ON STANDARD INPUT DOES NOT SINK THE FIRST
/// COMMAND.
///
/// Windows PowerShell writes a UTF-8 byte-order mark at the start of a piped stream by default,
/// so the first line a redirected session reads can arrive as the mark's bytes followed by
/// the command the user typed. <c>string.Trim()</c> does not remove a mark once it has become a
/// character, so the mark must be handled before the command is resolved.
///
/// TWO TESTS, ONE PER LINE OF DEFENCE.
///
/// <see cref="TheDefaultCodePageStillConsumesTheMarkAndRunsHelp"/> sends the raw bytes on this
/// machine's own default console code page, with no <c>chcp</c> anywhere in the launch. It is what
/// <see cref="Program"/>'s redirected byte-level reader (<c>detectEncodingFromByteOrderMarks</c>)
/// has to pass on its own: that reader consumes the mark from the stream's raw bytes before the
/// console's code page can turn it into three unrelated glyphs.
///
/// <see cref="AByteOrderMarkAlreadyDecodedAsUtf8StillRunsHelp"/> uses a code page that already
/// decodes the mark as the single character U+FEFF - forced here via
/// <see cref="CliProcessHarness.RunWithUtf8CodePage"/> - where <see cref="SessionConsole"/>'s
/// own leading-mark strip is what catches it. That strip covers the case the byte-level reader
/// does not: an interactive session on <see cref="Console.In"/>, which this suite cannot launch a
/// real console to test but which the unit-level <c>SessionConsoleTests</c> covers directly.
/// </summary>
public sealed class StandardInputByteOrderMarkTests
{
	private const char _BYTE_ORDER_MARK = (char)0xFEFF;

	[Fact]
	public void TheDefaultCodePageStillConsumesTheMarkAndRunsHelp()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			[$"{_BYTE_ORDER_MARK}help", "exit"]);

		AssertHelpRanRatherThanBeingReportedUnrecognized(outcome);
	}

	[Fact]
	public void AByteOrderMarkAlreadyDecodedAsUtf8StillRunsHelp()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.RunWithUtf8CodePage(
			repository.RootPath,
			destination.Path,
			[$"{_BYTE_ORDER_MARK}help", "exit"]);

		AssertHelpRanRatherThanBeingReportedUnrecognized(outcome);
	}

	private static void AssertHelpRanRatherThanBeingReportedUnrecognized(CliProcessOutcome outcome)
	{
		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		Assert.Contains(HelpRenderer.WORKFLOW_GROUP_HEADING, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(
			CommandRegistry.FormatNotRecognizedMessage("help"),
			outcome.StandardOutput,
			StringComparison.Ordinal);
		Assert.DoesNotContain(_BYTE_ORDER_MARK.ToString(), outcome.StandardOutput, StringComparison.Ordinal);
	}
}
