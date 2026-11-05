using Xunit;

namespace MigrateAuth.Cli.Tests;

/// <summary>
/// The exit codes are a published contract: scripts branch on the number, so a value that
/// silently moves breaks a caller that never recompiles. These assertions exist to fail
/// the build if a number is ever reassigned.
/// </summary>
public sealed class ExitCodeTests
{
	private const int _EXPECTED_EXIT_CODE_COUNT = 7;

	/// <summary>
	/// Codes 0 to 5 are the documented table. Code 6 is the refusal none of the five can carry
	/// without assigning a new meaning to a published number.
	/// </summary>
	[Theory]
	[InlineData(ExitCode.Success, 0)]
	[InlineData(ExitCode.UnusableRepositoryPath, 1)]
	[InlineData(ExitCode.SequentialGatingViolation, 2)]
	[InlineData(ExitCode.UnrecognizedCommand, 3)]
	[InlineData(ExitCode.OutputDestinationNotWritable, 4)]
	[InlineData(ExitCode.UnexpectedInternalError, 5)]
	[InlineData(ExitCode.NoLanguageEnabled, 6)]
	public void ExitCodeHasTheStableNumberItsContractPublishes(ExitCode exitCode, int expectedNumber)
	{
		int actualNumber = (int)exitCode;

		Assert.Equal(expectedNumber, actualNumber);
	}

	[Fact]
	public void EnumerationDefinesEveryExitCodeAndNoOthers()
	{
		ExitCode[] definedExitCodes = Enum.GetValues<ExitCode>();

		Assert.Equal(_EXPECTED_EXIT_CODE_COUNT, definedExitCodes.Length);
	}
}
