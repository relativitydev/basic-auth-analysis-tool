using System.Globalization;
using System.Text.Json;

using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THROUGH THE REAL PROCESS: <c>ai-assist</c> PRINTS A CLIENT-FACING LINE AND
/// COMMITS NOTHING.
///
/// <c>Pipeline.PipelineWiringTests</c> already proves the stage runner itself returns
/// <see cref="StageRunOutcome.NotAvailable"/> and prints the right text. What that suite cannot
/// prove is what <see cref="SessionShell"/> does with that outcome, because its harness calls the
/// runner chain directly and marks completion itself rather than going through the shell's own
/// <c>ExecuteStage</c> switch. This suite runs the real process instead, so every assertion here
/// is about the shell's own behaviour: no completion line, no "did not finish" line, no
/// "interrupted" line, and a state document that never names <c>ai-assist</c> as completed.
/// </summary>
public sealed class AiAssistNotAvailableProcessTests
{
	private const string _COMPLETED_STAGES_FIELD_NAME = "completedStages";

	private static readonly string[] _fourStagesThenAiAssistThenExit =
	[
		CommandRegistry.Describe(CommandId.Scan).Name,
		CommandRegistry.Describe(CommandId.Analyze).Name,
		CommandRegistry.Describe(CommandId.Suggest).Name,
		CommandRegistry.Describe(CommandId.Report).Name,
		CommandRegistry.Describe(CommandId.AiAssist).Name,
		CommandRegistry.Describe(CommandId.Exit).Name
	];

	/// <summary>
	/// The exit code is still zero - <c>ai-assist</c> is not an error - and the transcript carries
	/// the client-facing text and none of the three lines <see cref="SessionShell"/> prints
	/// for its own three outcomes, because none of those three is what happened here.
	/// </summary>
	[Fact]
	public void AiAssistPrintsTheClientFacingTextAndNoneOfTheShellsThreeOutcomeLines()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			_fourStagesThenAiAssistThenExit);

		string aiAssistStageName = CommandRegistry.Describe(CommandId.AiAssist).Name;

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		Assert.Contains(
			FormatMessage(AiAssistNotAvailableStageRunner.AI_ASSIST_NOT_AVAILABLE_MESSAGE_FORMAT, aiAssistStageName),
			outcome.StandardOutput,
			StringComparison.Ordinal);

		Assert.DoesNotContain(
			FormatMessage(SessionShell.STAGE_COMPLETED_MESSAGE_FORMAT, aiAssistStageName),
			outcome.StandardOutput,
			StringComparison.Ordinal);

		Assert.DoesNotContain(
			FormatMessage(SessionShell.STAGE_FAILED_MESSAGE_FORMAT, aiAssistStageName),
			outcome.StandardOutput,
			StringComparison.Ordinal);

		Assert.DoesNotContain(
			FormatMessage(SessionShell.STAGE_CANCELLED_MESSAGE_FORMAT, aiAssistStageName),
			outcome.StandardOutput,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The persisted half of the same claim: a stage <see cref="SessionShell"/> never committed
	/// is a stage the state document never names, so a process resuming this session still finds
	/// <c>ai-assist</c> gated behind <c>report</c> rather than satisfied.
	/// </summary>
	[Fact]
	public void AiAssistIsNotRecordedAsACompletedStageInThePersistedState()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessHarness.Run(repository.RootPath, destination.Path, _fourStagesThenAiAssistThenExit);

		using JsonDocument state = JsonDocument.Parse(
			ProducedArtifacts.ReadStateDocument(destination.Path, repository.Repository));

		string[] completedStageNames =
		[
			.. state.RootElement
				.GetProperty(_COMPLETED_STAGES_FIELD_NAME)
				.EnumerateArray()
				.Select(stage => stage.GetString())
				.OfType<string>()
		];

		Assert.DoesNotContain(CommandRegistry.Describe(CommandId.AiAssist).Name, completedStageNames);
		Assert.Equal(4, completedStageNames.Length);
	}

	/// <summary>`ai-assist` appears in `help` with its own description.</summary>
	[Fact]
	public void HelpStillListsAiAssistWithTheNotAvailableDescription()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.Run(repository.RootPath, destination.Path, ["help", "exit"]);

		Assert.Contains(
			CommandRegistry.Describe(CommandId.AiAssist).Description,
			outcome.StandardOutput,
			StringComparison.Ordinal);
	}

	private static string FormatMessage(string messageFormat, string stageName)
	{
		return string.Format(CultureInfo.InvariantCulture, messageFormat, stageName);
	}
}
