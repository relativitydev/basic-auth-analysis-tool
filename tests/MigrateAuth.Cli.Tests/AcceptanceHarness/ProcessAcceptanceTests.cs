using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THE ZERO-CANDIDATE ACCEPTANCE CONDITION, THROUGH THE PROCESS, WITH THE PROCESS'S OWN EXIT CODE.
///
/// WHAT THIS ADDS TO THE PIPELINE SET, WHICH ALREADY GRADES THAT CONDITION IN PROCESS. That set composes the
/// four stages directly and asserts what each returned. It cannot assert an exit code, because the exit code
/// is a property of a session and the exit-code contract makes it last-condition-wins over that whole
/// session - a value that exists only when a process ends. It also cannot establish that
/// <c>Program</c> composes the stages the way the harness does, that the command-line parser accepts
/// the two options, that Pre-flight permits the session, that the shell's gating chain admits the
/// four commands in order, or that the state reached the destination. Every one of those sits
/// between a user and the stages, and every one of them is exercised here and nowhere else.
///
/// The transcript is asserted against the shell's own message constants rather than against typed
/// text, so a changed message changes one constant and not four tests.
/// </summary>
public sealed class ProcessAcceptanceTests : IClassFixture<ZeroCandidateProcessSession>
{
	private const string _COMPLETED_STAGES_FIELD_NAME = "completedStages";
	private const int _ARTIFACTS_PER_RUN = 2;

	private static readonly ImmutableArray<CommandId> _theFourStages =
	[
		CommandId.Scan,
		CommandId.Analyze,
		CommandId.Suggest,
		CommandId.Report
	];

	private readonly ZeroCandidateProcessSession _session;

	public ProcessAcceptanceTests(ZeroCandidateProcessSession session)
	{
		_session = session;
	}

	/// <summary>
	/// THE CLAUSE NOTHING ELSE IN THIS SUITE CAN GRADE. A repository with zero candidate files ends
	/// its session with the operating system reporting zero, which is what a script calling this tool
	/// observes and what the condition requires. Standard error is checked in the same breath: the exception
	/// boundary is the only writer to it, so anything there means the session ended on a path that
	/// happened to return the right number.
	/// </summary>
	[Fact]
	public void TheProcessExitsWithCodeZeroAndWritesNothingToStandardError()
	{
		Assert.Equal((int)ExitCode.Success, _session.Outcome.ExitCode);
		Assert.Empty(_session.Outcome.StandardError);
	}

	/// <summary>
	/// All four stages completed, said in the shell's own words. The gating chain
	/// admits each command only after its predecessor committed, so four completion lines in one
	/// transcript is also the evidence that the order held.
	/// </summary>
	[Fact]
	public void EveryOneOfTheFourStagesCompletedInsideTheProcess()
	{
		foreach (CommandId stage in _theFourStages)
		{
			Assert.Contains(
				FormatCompletion(stage),
				_session.Outcome.StandardOutput,
				StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// Two artifacts, and the Markdown one carries the report's five sections. "A valid report stating
	/// zero findings" is what the condition asks for and a report missing a section over an empty run is the
	/// documented way that requirement fails.
	/// </summary>
	[Fact]
	public void BothArtifactsWereWrittenAndTheReportCarriesAllFiveSections()
	{
		Assert.Equal(_ARTIFACTS_PER_RUN, ProducedArtifacts.NamesIn(_session.DestinationPath).Length);

		string report = ProducedArtifacts.TextOfTheOnly(
			_session.DestinationPath,
			OutputArtifactKind.MarkdownReport);

		Assert.Contains(MarkdownReportRenderer.COVERAGE_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.SCAN_SUMMARY_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.SKIPPED_ITEMS_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.FINDINGS_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.PROVENANCE_HEADING, report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The data artifact is well-formed JSON, carries the same five sections under its own field
	/// names, and its finding set is EMPTY RATHER THAN ABSENT - which is the difference between a
	/// repository that was assessed and found clean and one that was never assessed.
	/// </summary>
	[Fact]
	public void TheDataArtifactParsesWithAllFiveSectionsAndStatesZeroFindings()
	{
		using JsonDocument artifact = JsonDocument.Parse(ProducedArtifacts.TextOfTheOnly(
			_session.DestinationPath,
			OutputArtifactKind.StructuredDataArtifact));

		JsonElement root = artifact.RootElement;

		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.COVERAGE_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.SCAN_SUMMARY_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.SKIPPED_ITEMS_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.PROVENANCE_FIELD_NAME, out _));
		Assert.Equal(
			0,
			root.GetProperty(StructuredDataArtifactRenderer.FINDINGS_FIELD_NAME).GetArrayLength());
	}

	/// <summary>
	/// The state the tool persists, read back from the destination as a resuming process would find it -
	/// by the keyed hash of the repository path rather than by a name this test knows. Four completed
	/// stages in a file is the durable half of the condition's "completes all four stages"; the transcript
	/// above is the visible half, and a session that printed one without writing the other would be
	/// a session that cannot be resumed.
	/// </summary>
	[Fact]
	public void TheStateDocumentRecordsAllFourStagesAsCompleted()
	{
		using JsonDocument state = JsonDocument.Parse(
			ProducedArtifacts.ReadStateDocument(_session.DestinationPath, _session.Repository));

		string[] completedStageNames =
		[
			.. state.RootElement
				.GetProperty(_COMPLETED_STAGES_FIELD_NAME)
				.EnumerateArray()
				.Select(stage => stage.GetString())
				.OfType<string>()
		];

		string[] expectedStageNames =
		[
			.. _theFourStages.Select(stage => CommandRegistry.Describe(stage).Name)
		];

		Assert.Equal(expectedStageNames, completedStageNames);
	}

	/// <summary>
	/// THE EMPTIEST FORM OF THE CONDITION, THROUGH THE PROCESS. A repository with no files at all still
	/// completes the four stages and still leaves two artifacts, and the exit code is still zero.
	/// It runs its own process because it is a different repository, and the count-driven lines of
	/// every stage take a different branch over it.
	/// </summary>
	[Fact]
	public void ARepositoryWithNoFilesAtAllAlsoExitsWithCodeZeroAndReports()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);
		Assert.Equal(_ARTIFACTS_PER_RUN, ProducedArtifacts.NamesIn(destination.Path).Length);
		Assert.Contains(
			MarkdownReportRenderer.PROVENANCE_HEADING,
			ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.MarkdownReport),
			StringComparison.Ordinal);
	}

	private static string FormatCompletion(CommandId stage)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			SessionShell.STAGE_COMPLETED_MESSAGE_FORMAT,
			CommandRegistry.Describe(stage).Name);
	}
}
