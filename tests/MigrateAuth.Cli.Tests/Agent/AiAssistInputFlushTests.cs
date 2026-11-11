using MigrateAuth.Cli.Agent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// TYPED-AHEAD INPUT IS THROWN AWAY BEHIND THE NATIVE-CONSOLE SEAM: before the confirmation is read, so that a stray line
/// cannot answer it, and again after the child returns, so that what was typed while it ran belongs to nobody. With a fake
/// seam the two flushes are seen in order, the second one after the fake agent ran; a decline flushes once; a run that
/// offers nothing flushes never.
/// </summary>
public sealed class AiAssistInputFlushTests
{
	[Fact]
	public void ALaunchFlushesBeforeTheConfirmationReadAndAgainAfterTheChildReturns()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.Equal(2, harness.NativeConsole.Flushes.Count);
		Assert.Equal(new FlushRecord(ReadsBefore: 1, ChildHadRun: false), harness.NativeConsole.Flushes[0]);
		Assert.Equal(new FlushRecord(ReadsBefore: 2, ChildHadRun: true), harness.NativeConsole.Flushes[1]);
	}

	[Fact]
	public void ADeclineFlushesOnceBeforeTheReadAndNeverAfter()
	{
		using AiAssistHarness harness = new(["1", "n"]);

		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.Equal([new FlushRecord(ReadsBefore: 1, ChildHadRun: false)], harness.NativeConsole.Flushes);
	}

	[Fact]
	public void ARunThatOffersNothingNeverFlushes()
	{
		using AiAssistHarness harness = new(["1", "y"]);

		harness.IsTerminal = false;
		harness.InstallFake(AgentEnvironment.ClaudeCode);
		harness.Run();

		Assert.Empty(harness.NativeConsole.Flushes);
	}

	[Fact]
	public void AnAgentThatCouldNotBeStartedStillFlushesAfterTheLaunchAttempt()
	{
		using AiAssistHarness harness = new(["1", "y"]);
		string executable = harness.InstallFake(AgentEnvironment.ClaudeCode);

		harness.Input.OnRead = index =>
		{
			if (index == AiAssistHarness.CONFIRMATION_READ_INDEX)
			{
				File.Delete(executable);
			}
		};
		harness.Run();

		Assert.Equal(2, harness.NativeConsole.Flushes.Count);
	}
}
