using MigrateAuth.Cli.Agent;
using MigrateAuth.Cli.Session;

using MigrateAuth.FakeAgent;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE STAGE READS THE SESSION STATE AND NEVER WRITES IT: the persisted state file's bytes and the completed stages are
/// identical before and after, whatever the run did (cancelled, generated only, declined, launched, interrupted).
/// </summary>
public sealed class AiAssistSessionStateTests
{
	private const string _SECOND_RUN = "second run";

	private static readonly CommandId[] _fourStages = [CommandId.Scan, CommandId.Analyze, CommandId.Suggest, CommandId.Report];

	public static TheoryData<string, string[]> Scenarios => new()
	{
		{ "cancelled selection", [string.Empty] },
		{ "generate only", ["1"] },
		{ "declined", ["1", "n"] },
		{ "launched", ["1", "y"] },
		{ _SECOND_RUN, ["1", "y", "2", "n"] }
	};

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void TheStateFileBytesAndTheCompletedStagesAreIdenticalBeforeAndAfter(string scenario, string[] inputs)
	{
		using AiAssistHarness harness = new(inputs);

		foreach (CommandId stage in _fourStages)
		{
			harness.State.MarkCompleted(stage);
		}

		new SessionStateStore(harness.Destination).Save(harness.RepositoryPath, harness.State);

		string statePath = harness.DestinationFolder.Combine(SessionStateStore.BuildStateFileName(harness.RepositoryPath));
		byte[] bytesBefore = File.ReadAllBytes(statePath);
		DateTime writtenBefore = File.GetLastWriteTimeUtc(statePath);
		CommandId[] completedBefore = [.. harness.State.CompletedStagesInRunOrder().Select(stage => stage.Id)];

		harness.InstallFake(AgentEnvironment.ClaudeCode, new FakeAgentBehavior());
		harness.InstallFake(AgentEnvironment.OpenAiCodex, new FakeAgentBehavior());

		int runs = scenario == _SECOND_RUN ? 2 : 1;

		for (int run = 0; run < runs; run++)
		{
			Assert.Equal(StageRunOutcome.SelfReported, harness.Run());
		}

		Assert.Equal(bytesBefore, File.ReadAllBytes(statePath));
		Assert.Equal(writtenBefore, File.GetLastWriteTimeUtc(statePath));
		Assert.Equal(completedBefore, [.. harness.State.CompletedStagesInRunOrder().Select(stage => stage.Id)]);
		Assert.Equal(4, completedBefore.Length);
		Assert.False(harness.State.IsCompleted(CommandId.AiAssist));
	}

	[Fact]
	public void ARunReadsTheFindingsOfTheStateAndNothingElseOfIt()
	{
		using AiAssistHarness harness = new(["1"]);

		harness.IsTerminal = false;
		harness.Run();

		string pack = File.ReadAllText(harness.DestinationFolder.Combine(AiAssistHarness.CLAUDE_PACK_FILE_NAME));

		Assert.Contains("Entries: 6. Section 1: 3. Section 2: 1. Section 3: 2.", pack, StringComparison.Ordinal);
	}
}
