using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// Where a Windows script wrapper may start, as a pure rule over text: a drive-letter path within the
/// interpreter's measured limit. Every other form of a working directory withholds a wrapper launch.
/// </summary>
public sealed class WrapperWorkingDirectoryRuleTests
{
	private const int _MEASURED_LIMIT = 258;
	private const string _DRIVE_ROOT = @"C:\";

	[Theory]
	[InlineData(@"C:\")]
	[InlineData(@"C:\repo")]
	[InlineData(@"D:\work\audits\repo with spaces")]
	[InlineData(@"z:\lower case letter")]
	[InlineData(@"\\?\C:\repo")]
	[InlineData(@"\\?\D:\")]
	public void ADriveLetterPathIsAllowed(string workingDirectory)
	{
		Assert.True(WrapperWorkingDirectoryRule.AllowsWrapperLaunch(workingDirectory));
	}

	[Theory]
	[InlineData(@"\\server\share\repo")]
	[InlineData(@"\\localhost\C$\Windows\Temp")]
	[InlineData(@"\\?\UNC\server\share\repo")]
	[InlineData(@"\\?\Volume{01234567-89ab-cdef-0123-456789abcdef}\repo")]
	[InlineData(@"\\.\C:\repo")]
	[InlineData(@"repo")]
	[InlineData(@"..\repo")]
	[InlineData(@"\repo")]
	[InlineData(@"C:repo")]
	[InlineData(@"C:")]
	[InlineData(@"1:\repo")]
	[InlineData(@"")]
	public void EveryOtherFormIsRefused(string workingDirectory)
	{
		Assert.False(WrapperWorkingDirectoryRule.AllowsWrapperLaunch(workingDirectory));
	}

	[Fact]
	public void ThePathLengthLimitIsTheMeasuredConstant()
	{
		Assert.Equal(_MEASURED_LIMIT, WrapperWorkingDirectoryRule.MAXIMUM_CURRENT_DIRECTORY_LENGTH);
	}

	[Fact]
	public void APathOfExactlyTheLimitIsAllowedAndOneLongerIsRefused()
	{
		string atTheLimit = $"{_DRIVE_ROOT}{new string('a', _MEASURED_LIMIT - _DRIVE_ROOT.Length)}";

		Assert.Equal(_MEASURED_LIMIT, atTheLimit.Length);
		Assert.True(WrapperWorkingDirectoryRule.AllowsWrapperLaunch(atTheLimit));
		Assert.False(WrapperWorkingDirectoryRule.AllowsWrapperLaunch($"{atTheLimit}a"));
	}

	[Fact]
	public void TheLongPathPrefixIsNotCountedTowardsTheLimit()
	{
		string atTheLimit = $"{_DRIVE_ROOT}{new string('a', _MEASURED_LIMIT - _DRIVE_ROOT.Length)}";

		Assert.True(WrapperWorkingDirectoryRule.AllowsWrapperLaunch($@"\\?\{atTheLimit}"));
		Assert.False(WrapperWorkingDirectoryRule.AllowsWrapperLaunch($@"\\?\{atTheLimit}a"));
	}

	[Fact]
	public void ANullPathIsAProgrammingError()
	{
		Assert.Throws<ArgumentNullException>(() => WrapperWorkingDirectoryRule.AllowsWrapperLaunch(null!));
	}
}
