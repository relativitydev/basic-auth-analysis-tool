using System.Security.Cryptography;
using System.Text;

using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE PACK'S BYTES DO NOT DEPEND ON WHEN, WHERE OR ON WHICH PLATFORM IT IS WRITTEN. Two writes into different
/// folders are equal byte for byte, no absolute path or temporary folder name appears, and the bytes over the fixed
/// fixture equal a hash committed in this file. The same hash holds on Windows and Linux, because the text is built from
/// constants with "\n" line endings and the invariant culture. A change to any pack text changes the hash on purpose:
/// the new value is recorded here in the same change.
/// </summary>
public sealed class PromptPackDeterminismTests
{
	private const byte _BYTE_ORDER_MARK_FIRST_BYTE = 0xEF;
	private const string _EXPECTED_CLAUDE_CODE_PACK_HASH = "75E87BE54296C02A5A7184358122140FF6AB04D82BD375DB01D3BCD6B024D0EB";

	[Fact]
	public void TwoWritesOfTheSamePackAreByteIdentical()
	{
		byte[] first = PromptPackFixture.WritePackFile(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());
		byte[] second = PromptPackFixture.WritePackFile(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());

		Assert.Equal(first, second);
	}

	[Fact]
	public void ThePackOverTheFixedFixtureEqualsTheCommittedHash()
	{
		byte[] bytes = PromptPackFixture.WritePackFile(AgentEnvironment.ClaudeCode, PromptPackFixture.Scope());

		string actualHash = Convert.ToHexString(SHA256.HashData(bytes));

		Assert.True(
			string.Equals(_EXPECTED_CLAUDE_CODE_PACK_HASH, actualHash, StringComparison.Ordinal),
			$"The pack hash is {actualHash}; the committed value is {_EXPECTED_CLAUDE_CODE_PACK_HASH}.");
	}

	[Fact]
	public void ThePackNamesNoAbsolutePathAndNoTemporaryFolder()
	{
		string text = Encoding.UTF8.GetString(PromptPackFixture.WritePackFile(AgentEnvironment.GitHubCopilotCli, PromptPackFixture.Scope()));

		Assert.DoesNotContain(Path.GetTempPath(), text, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("migrate-auth-tests-", text, StringComparison.Ordinal);
		Assert.DoesNotContain(Environment.UserName, text, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(":\\", text, StringComparison.Ordinal);
		Assert.DoesNotContain("\r", text, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePackHoldsNoBomAndEndsWithALineFeed()
	{
		byte[] bytes = PromptPackFixture.WritePackFile(AgentEnvironment.OpenAiCodex, PromptPackFixture.Scope());

		Assert.NotEqual(_BYTE_ORDER_MARK_FIRST_BYTE, bytes[0]);
		Assert.Equal((byte)AiAssistHarness.LINE_FEED_CHARACTER, bytes[^1]);
	}
}
