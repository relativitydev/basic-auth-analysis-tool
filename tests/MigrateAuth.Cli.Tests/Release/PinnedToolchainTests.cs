using System.Text.Json;
using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.Release;

/// <summary>
/// THE .NET SDK IS PINNED IN global.json, AND THE NATIVE AOT COMPILER PACKAGE THE PINNED SDK BRINGS IS NAMED ONCE
/// IN EACH RELEASE SCRIPT.
///
/// The two release scripts must agree on that package version, and the pin file must say what the release
/// procedure relies on: one SDK, no roll forward beyond a patch, no pre-release.
/// </summary>
public sealed class PinnedToolchainTests
{
	private const string _SOLUTION_FILE_PATTERN = "*.sln";
	private const string _GLOBAL_JSON_RELATIVE_PATH = "global.json";
	private const string _POWERSHELL_SCRIPT_RELATIVE_PATH = "scripts/build-release.ps1";
	private const string _BASH_SCRIPT_RELATIVE_PATH = "scripts/build-release.sh";
	private const string _SDK_PROPERTY = "sdk";
	private const string _VERSION_PROPERTY = "version";
	private const string _ROLL_FORWARD_PROPERTY = "rollForward";
	private const string _ALLOW_PRERELEASE_PROPERTY = "allowPrerelease";
	private const string _PINNED_SDK_VERSION = "10.0.403";
	private const string _PINNED_ROLL_FORWARD = "patch";
	private const string _EXPECTED_COMPILER_VERSION = "10.0.14";
	private const string _BASH_COMPILER_VERSION_PATTERN = "EXPECTED_ILCOMPILER_VERSION=\"([^\"]+)\"";
	private const string _POWERSHELL_COMPILER_VERSION_PATTERN = "\\$ExpectedIlCompilerVersion = '([^']+)'";
	private const char _CARRIAGE_RETURN = '\r';
	private const char _SPACE = ' ';
	private const char _LINE_FEED = '\n';
	private static readonly byte[] _Utf8ByteOrderMark = [0xEF, 0xBB, 0xBF];

	[Fact]
	public void TheGlobalJsonPinsTheSdkWithPatchRollForwardAndNoPreRelease()
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(GlobalJsonPath()));
		JsonElement sdk = document.RootElement.GetProperty(_SDK_PROPERTY);

		Assert.Equal(_PINNED_SDK_VERSION, sdk.GetProperty(_VERSION_PROPERTY).GetString());
		Assert.Equal(_PINNED_ROLL_FORWARD, sdk.GetProperty(_ROLL_FORWARD_PROPERTY).GetString());
		Assert.False(sdk.GetProperty(_ALLOW_PRERELEASE_PROPERTY).GetBoolean());
	}

	[Fact]
	public void TheGlobalJsonHasNoByteOrderMarkNoCarriageReturnAndNoSpaceIndentation()
	{
		byte[] globalJsonBytes = File.ReadAllBytes(GlobalJsonPath());
		string globalJsonText = File.ReadAllText(GlobalJsonPath());

		Assert.False(globalJsonBytes.AsSpan().StartsWith(_Utf8ByteOrderMark), "The pin file starts with a byte-order mark.");
		Assert.DoesNotContain(_CARRIAGE_RETURN, globalJsonText);
		Assert.DoesNotContain($"{_LINE_FEED}{_SPACE}", globalJsonText, StringComparison.Ordinal);
	}

	[Fact]
	public void BothReleaseScriptsNameTheSameCompilerVersionAndItIsTheOneTheSdkBrings()
	{
		string bashVersion = ReadFirstGroup(_BASH_SCRIPT_RELATIVE_PATH, _BASH_COMPILER_VERSION_PATTERN);
		string powerShellVersion = ReadFirstGroup(_POWERSHELL_SCRIPT_RELATIVE_PATH, _POWERSHELL_COMPILER_VERSION_PATTERN);

		Assert.Equal(bashVersion, powerShellVersion);
		Assert.Equal(_EXPECTED_COMPILER_VERSION, bashVersion);
	}

	private static string ReadFirstGroup(string scriptRelativePath, string pattern)
	{
		string scriptText = File.ReadAllText(Path.Combine(LocateRepositoryRoot(), scriptRelativePath));
		Match match = Regex.Match(scriptText, pattern);

		Assert.True(match.Success, $"{scriptRelativePath} does not declare the compiler version.");

		return match.Groups[1].Value;
	}

	private static string GlobalJsonPath()
	{
		return Path.Combine(LocateRepositoryRoot(), _GLOBAL_JSON_RELATIVE_PATH);
	}

	private static string LocateRepositoryRoot()
	{
		DirectoryInfo? candidate = new(AppContext.BaseDirectory);

		while (candidate is not null && candidate.GetFiles(_SOLUTION_FILE_PATTERN).Length == 0)
		{
			candidate = candidate.Parent;
		}

		return candidate?.FullName
			?? throw new DirectoryNotFoundException("No folder holding a solution file sits above the test assembly.");
	}
}
