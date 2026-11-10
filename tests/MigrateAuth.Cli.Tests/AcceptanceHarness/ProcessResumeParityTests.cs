using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// <see cref="ResumedReportParityTests"/> PROVES RESUME PARITY ACROSS TWO
/// IN-PROCESS COMPOSITIONS SHARING NOTHING BUT THE REPOSITORY; THIS CLASS PROVES THE SAME CLAIM
/// ACROSS TWO ACTUAL OPERATING-SYSTEM PROCESSES, THROUGH <see cref="CliProcessHarness"/>.
///
/// Process one runs <c>scan</c>, <c>analyze</c>, <c>exit</c> against a destination folder; process
/// two, launched fresh afterwards, runs <c>suggest</c>, <c>report</c>, <c>exit</c> against that same
/// destination - nothing survives between the two processes except what
/// <see cref="SessionStateStore"/> wrote to disk. A third, single, uninterrupted process runs all
/// four stages then <c>exit</c> against its own destination. The two reports must be byte for byte
/// identical: a resumed process is not permitted to see anything a single process would not have.
///
/// THE FIXTURE PLANTS THREE REAL FINDINGS through the embedded <c>CSharp</c> analyzer and its own flow
/// signals - one decided by an interactive-user signal, one by a fixed-credential signal and one
/// defaulted for lack of any - so the comparison exercises a report holding both flows, both signal
/// notes and the default note, and not only the empty-repository case <see cref="ProcessAcceptanceTests"/>
/// already covers.
/// </summary>
public sealed class ProcessResumeParityTests
{
	private const string _INTERACTIVE_FILE_NAME = "Interactive.cs";
	private const string _FIXED_FILE_NAME = "Fixed.cs";
	private const string _DEFAULTED_FILE_NAME = "Defaulted.cs";
	private const int _FILE_COUNT_WITH_A_FINDING = 3;
	private const string _FINDINGS_FIELD = "findings";
	private const string _FLOW_FIELD = "flow";
	private const string _FLOW_DECISION_FIELD = "flowDecision";
	private const string _FLOW_NOTE_FIELD = "flowNote";
	private const string _FILE_PATH_FIELD = "filePath";
	private const string _ROWS_HEADING_LINE = "Findings:";
	private const string _LEGEND_HEADING_LINE = "Flow notes:";
	private const string _FLOW_NOTE_REPORT_LINE_FORMAT = "- Flow note: {0}";
	private const string _ROW_INDENTATION = "  ";

	private static readonly ImmutableArray<FlowSignal> _bothKindsOfSignal =
		[FlowSignalFixtures.InteractiveCodeOnly, FlowSignalFixtures.FixedCodeOnly];

	/// <summary>
	/// The four stages, split at the same seam a real user would resume at, against everything
	/// <see cref="CliProcessHarness"/> can observe of a real process: standard output, the exit
	/// code, and the two artifacts left on disk.
	/// </summary>
	[Fact]
	public void AReportResumedAcrossTwoRealProcessesMatchesASingleUninterruptedProcessByteForByte()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder twoProcessDestination = TemporaryFolder.Create();
		using TemporaryFolder singleProcessDestination = TemporaryFolder.Create();

		PlantTheFixtureRepository(repository, CSharpFlowFixtureRepository.AllFiles);

		RunTheFirstHalfThenTheSecondHalfInSeparateProcesses(repository.RootPath, twoProcessDestination.Path);
		RunAllFourStagesInOneProcess(repository.RootPath, singleProcessDestination.Path);

		AssertTheArtifactsAreByteIdentical(twoProcessDestination.Path, singleProcessDestination.Path);
		AssertTheReportHoldsBothFlowsAndAllThreeDecisions(twoProcessDestination.Path);
	}

	/// <summary>
	/// OVER THE REAL `CSharp` ANALYZER AND A REAL PROCESS. A repository with one file
	/// holding an interactive-user signal and one holding none is run through all four stages: the first
	/// finding carries the "confirm the flow" note and the second the default note in the report, and in
	/// the Suggest console the legend carries both notes and each row ends with its own note's key.
	/// </summary>
	[Fact]
	public void AnInteractiveFileAndANoSignalFileShowBothFlowsAndNotesInTheReportAndTheSuggestConsoleOfARealProcess()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		PlantTheFixtureRepository(repository, CSharpFlowFixtureRepository.InteractiveAndPlainFiles);

		RunAllFourStagesInOneProcess(repository.RootPath, destination.Path, out CliProcessOutcome outcome);

		string report = ProducedArtifacts.TextOfTheOnly(destination.Path, OutputArtifactKind.MarkdownReport);
		JsonElement[] findings = ReadFindings(destination.Path);

		Assert.Equal(2, findings.Length);
		AssertFinding(findings[0], CSharpFlowFixtureRepository.INTERACTIVE_FILE_NAME, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal);
		AssertFinding(findings[1], CSharpFlowFixtureRepository.PLAIN_FILE_NAME, CredentialFlow.ClientCredentials, FlowDecision.Default);

		string confirmNoteLine = string.Format(CultureInfo.InvariantCulture, _FLOW_NOTE_REPORT_LINE_FORMAT, FlowNotes.INTERACTIVE_SIGNAL_AUTHORIZATION_CODE_NOTE);
		string defaultNoteLine = string.Format(CultureInfo.InvariantCulture, _FLOW_NOTE_REPORT_LINE_FORMAT, FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE);

		Assert.Contains(confirmNoteLine, report, StringComparison.Ordinal);
		Assert.Contains(defaultNoteLine, report, StringComparison.Ordinal);
		Assert.True(report.IndexOf(confirmNoteLine, StringComparison.Ordinal) < report.IndexOf(defaultNoteLine, StringComparison.Ordinal));

		AssertTheConsoleLegendAndRows(
			outcome.StandardOutput,
			[
				(FlowDecision.InteractiveSignal, CredentialFlow.AuthorizationCode),
				(FlowDecision.Default, CredentialFlow.ClientCredentials)
			]);
	}

	/// <summary>
	/// IN-PROCESS FORM. A session holding an interactive, a fixed and a defaulted
	/// finding is saved and loaded, and the restored session carries the same flows, decisions, notes
	/// and counts, the same default flow with its origin, and the same flow-signal counters. The
	/// two-process form of the claim is the test above; this one runs over a fixture in the test
	/// project so it can plant every kind of evidence.
	/// </summary>
	[Fact]
	public void AnInteractiveAFixedAndADefaultedFindingKeepTheirFlowsNotesAndCountsAcrossASaveAndALoad()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal, CredentialFlow.AuthorizationCode);

		PlantOneFilePerKindOfEvidence(harness);
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		SessionStateLoad reloaded = harness.ReloadState();

		Assert.True(reloaded.WasResumed);
		Assert.Equal(
			[FlowDecision.InteractiveSignal, FlowDecision.FixedSignal, FlowDecision.Default],
			harness.State.Findings.Select(finding => finding.FlowDecision!.Value).Order());
		Assert.Equal(DescribeTheFlows(harness.State), DescribeTheFlows(reloaded.State));
		Assert.Equal(harness.State.Findings.AsEnumerable(), reloaded.State.Findings.AsEnumerable());
		Assert.Equal(new EffectiveDefaultFlow(CredentialFlow.AuthorizationCode, ConfigurationSource.UserProfileFile), reloaded.State.DefaultFlow);
		Assert.Equal(harness.State.FlowEvaluationDiagnostics, reloaded.State.FlowEvaluationDiagnostics);
		Assert.Equal(_FILE_COUNT_WITH_A_FINDING, reloaded.State.FlowEvaluationDiagnostics.FilesEvaluated);
	}

	/// <summary>
	/// IN-PROCESS FORM. The default flow changes between Analyze and a resumed Suggest:
	/// the decided flows do not. The findings keep the flow Analyze decided, and the report a resumed
	/// session writes names the default and the origin Analyze decided under, and not the
	/// configuration of the process that resumed.
	/// </summary>
	[Fact]
	public void ADefaultFlowChangedBetweenAnalyzeAndAResumedSuggestDoesNotChangeTheDecidedFlows()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithFlowSignals(_bothKindsOfSignal, CredentialFlow.AuthorizationCode);

		PlantOneFilePerKindOfEvidence(harness);
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		string flowsAtAnalyze = DescribeTheFlows(harness.State);
		SessionState resumed = harness.ReloadState().State;
		EffectiveConfiguration builtInDefaultConfiguration = PipelineHarness.BuildConfiguration(ConfidenceLevels.WIDEST);

		Assert.Equal(CredentialFlows.BUILT_IN_DEFAULT, builtInDefaultConfiguration.DefaultFlow);
		Assert.Equal(StageRunOutcome.Completed, RunSuggestThenReport(harness, resumed, builtInDefaultConfiguration, new StringWriter()));

		Assert.Equal(flowsAtAnalyze, DescribeTheFlows(resumed));

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		JsonElement provenance = artifact.RootElement.GetProperty("provenance");

		Assert.Equal(CredentialFlows.AUTHORIZATION_CODE_NAME, provenance.GetProperty("defaultFlow").GetString());
		Assert.Equal(ReportVocabulary.DefaultFlowOriginValueOf(ConfigurationSource.UserProfileFile), provenance.GetProperty("defaultFlowOrigin").GetString());
		Assert.Equal(
			[
				CredentialFlows.AUTHORIZATION_CODE_NAME,
				CredentialFlows.CLIENT_CREDENTIALS_NAME,
				CredentialFlows.AUTHORIZATION_CODE_NAME
			],
			artifact.RootElement.GetProperty("findings").EnumerateArray().Select(finding => finding.GetProperty("flow").GetString()));
	}

	/// <summary>
	/// OVER THE REAL `CSharp` ANALYZER. Analyze runs under a profile whose <c>defaultFlow</c> is
	/// Authorization Code; the session is then resumed under the built-in default, Client Credentials,
	/// for Suggest and Report. The resumed session renders the flows and notes Analysis decided - the
	/// legend and rows of its console, and the provenance of its artifact - and not the configuration
	/// of the composition that resumed. A profile file cannot be redirected for a child process on every
	/// platform, so the two halves are two independent stage compositions over the same destination.
	/// </summary>
	[Fact]
	public void ADefaultFlowChangedBetweenAnalyzeAndAResumedSuggestRendersTheFlowsAndNotesAnalysisDecidedOverTheRealCSharpAnalyzer()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalogWithConfiguration(
			ConfigurationWithProfileDefault(CredentialFlow.AuthorizationCode));

		PlantTheFixtureRepository(harness, CSharpFlowFixtureRepository.AllFiles);
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);

		string flowsAtAnalyze = DescribeTheFlows(harness.State);
		SessionState resumed = harness.ReloadState().State;
		EffectiveConfiguration resumingConfiguration = PipelineHarness.BuildConfiguration(ConfidenceLevels.WIDEST);
		StringWriter resumedConsole = new();

		Assert.Equal(CredentialFlows.BUILT_IN_DEFAULT, resumingConfiguration.DefaultFlow);
		Assert.Equal(StageRunOutcome.Completed, RunSuggestThenReport(harness, resumed, resumingConfiguration, resumedConsole));
		Assert.Equal(flowsAtAnalyze, DescribeTheFlows(resumed));

		AssertTheConsoleLegendAndRows(
			resumedConsole.ToString(),
			[
				(FlowDecision.FixedSignal, CredentialFlow.ClientCredentials),
				(FlowDecision.InteractiveSignal, CredentialFlow.AuthorizationCode),
				(FlowDecision.Default, CredentialFlow.AuthorizationCode)
			]);
		Assert.DoesNotContain(FlowNotes.DEFAULT_CLIENT_CREDENTIALS_NOTE, resumedConsole.ToString(), StringComparison.Ordinal);

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));
		JsonElement provenance = artifact.RootElement.GetProperty("provenance");

		Assert.Equal(CredentialFlows.AUTHORIZATION_CODE_NAME, provenance.GetProperty("defaultFlow").GetString());
		Assert.Equal(ReportVocabulary.DefaultFlowOriginValueOf(ConfigurationSource.UserProfileFile), provenance.GetProperty("defaultFlowOrigin").GetString());
	}

	private static EffectiveConfiguration ConfigurationWithProfileDefault(CredentialFlow defaultFlow)
	{
		EffectiveConfiguration baseline = PipelineHarness.BuildConfiguration(ConfidenceLevels.WIDEST);

		return baseline with
		{
			DefaultFlow = defaultFlow,
			Provenance = baseline.Provenance with { DefaultFlowOrigin = ConfigurationSource.UserProfileFile }
		};
	}

	private static void PlantTheFixtureRepository(
		PipelineHarness harness,
		ImmutableArray<(string RelativePath, string Content)> files)
	{
		foreach ((string relativePath, string content) in files)
		{
			harness.WriteSourceFile(relativePath, content);
		}
	}

	private static void PlantTheFixtureRepository(
		RepositoryFixture repository,
		ImmutableArray<(string RelativePath, string Content)> files)
	{
		foreach ((string relativePath, string content) in files)
		{
			repository.WriteText(relativePath, content);
		}
	}

	private static JsonElement[] ReadFindings(string destinationFolderPath)
	{
		using JsonDocument artifact = JsonDocument.Parse(
			ProducedArtifacts.TextOfTheOnly(destinationFolderPath, OutputArtifactKind.StructuredDataArtifact));

		return [.. artifact.RootElement.GetProperty(_FINDINGS_FIELD).EnumerateArray().Select(finding => finding.Clone())];
	}

	private static void AssertFinding(JsonElement finding, string fileName, CredentialFlow flow, FlowDecision decision)
	{
		Assert.EndsWith(fileName, finding.GetProperty(_FILE_PATH_FIELD).GetString(), StringComparison.Ordinal);
		Assert.Equal(CredentialFlows.Name(flow), finding.GetProperty(_FLOW_FIELD).GetString());
		Assert.Equal(CredentialFlows.Name(decision), finding.GetProperty(_FLOW_DECISION_FIELD).GetString());
		Assert.Equal(FlowNotes.NoteFor(decision, flow), finding.GetProperty(_FLOW_NOTE_FIELD).GetString());
	}

	private static void AssertTheReportHoldsBothFlowsAndAllThreeDecisions(string destinationFolderPath)
	{
		JsonElement[] findings = ReadFindings(destinationFolderPath);

		Assert.Equal(_FILE_COUNT_WITH_A_FINDING, findings.Length);
		AssertFinding(findings[0], CSharpFlowFixtureRepository.FIXED_FILE_NAME, CredentialFlow.ClientCredentials, FlowDecision.FixedSignal);
		AssertFinding(findings[1], CSharpFlowFixtureRepository.INTERACTIVE_FILE_NAME, CredentialFlow.AuthorizationCode, FlowDecision.InteractiveSignal);
		AssertFinding(findings[2], CSharpFlowFixtureRepository.PLAIN_FILE_NAME, CredentialFlow.ClientCredentials, FlowDecision.Default);
	}

	/// <summary>
	/// The legend gives each expected note once, under its own key, and no other note; the rows, in the
	/// order given, each end with the key of their own note.
	/// </summary>
	private static void AssertTheConsoleLegendAndRows(
		string console,
		ImmutableArray<(FlowDecision Decision, CredentialFlow Flow)> expectedRows)
	{
		string[] lines = console.Split(Environment.NewLine);
		string[] rows = lines
			.SkipWhile(line => line != _ROWS_HEADING_LINE)
			.Skip(1)
			.TakeWhile(line => line.StartsWith(_ROW_INDENTATION, StringComparison.Ordinal))
			.ToArray();
		HashSet<string> expectedKeys = [.. expectedRows.Select(row => FlowNoteKeys.KeyFor(row.Decision, row.Flow))];

		Assert.Contains(_LEGEND_HEADING_LINE, lines);
		Assert.Equal(expectedRows.Length, rows.Length);

		foreach (FlowNoteLegendEntry entry in FlowNoteKeys.AllEntries)
		{
			bool expected = expectedKeys.Contains(entry.Key);

			Assert.Equal(expected, lines.Contains($"{_ROW_INDENTATION}{entry.Key} {entry.Note}"));
		}

		for (int index = 0; index < rows.Length; index++)
		{
			Assert.EndsWith(FlowNoteKeys.KeyFor(expectedRows[index].Decision, expectedRows[index].Flow), rows[index], StringComparison.Ordinal);
		}
	}

	private static StageRunOutcome RunSuggestThenReport(
		PipelineHarness harness,
		SessionState resumed,
		EffectiveConfiguration resumingConfiguration,
		TextWriter suggestOutput)
	{
		IGenericRecommendationProvider genericRecommendations = new GenericRecommendationProvider();
		SuggestStage suggest = new(new SuggestStageContext
		{
			Catalog = harness.Catalog,
			GenericRecommendations = genericRecommendations,
			Configuration = resumingConfiguration,
			Output = suggestOutput
		});
		ReportStage report = new(new ReportStageContext
		{
			Catalog = harness.Catalog,
			Configuration = resumingConfiguration,
			RepositoryLabel = harness.Repository.Label,
			ArtifactWriter = new AtomicArtifactWriter(harness.Repository, harness.Destination),
			GenericRecommendations = genericRecommendations,
			Output = new StringWriter(),
			TimeProvider = harness.Clock
		});
		StageRunOutcome suggested = suggest.Run(resumed, CancellationToken.None);

		return suggested is StageRunOutcome.Completed
			? report.Run(resumed, CancellationToken.None)
			: suggested;
	}

	/// <summary>Each finding's identifier, flow, decision and the note derived from the pair - the note is never persisted.</summary>
	private static string DescribeTheFlows(SessionState state)
	{
		return string.Join(
			"|",
			state.Findings.Select(finding => string.Join(
				",",
				finding.Identifier,
				finding.Flow,
				finding.FlowDecision,
				FlowNotes.NoteFor(finding.FlowDecision!.Value, finding.Flow!.Value))));
	}

	private static void PlantOneFilePerKindOfEvidence(PipelineHarness harness)
	{
		harness.WriteSourceFile(_INTERACTIVE_FILE_NAME, SourceWith(FlowSignalFixtures.INTERACTIVE_TOKEN));
		harness.WriteSourceFile(_FIXED_FILE_NAME, SourceWith(FlowSignalFixtures.FIXED_TOKEN));
		harness.WriteSourceFile(_DEFAULTED_FILE_NAME, SourceWith(token: null));
	}

	private static string SourceWith(string? token)
	{
		string tokenLine = token is null ? string.Empty : $"\tvoid Signal() => {token}();\n";

		return $"class Sample\n{{\n{tokenLine}\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"a-literal-secret\");\n}}\n";
	}

	private static void RunTheFirstHalfThenTheSecondHalfInSeparateProcesses(
		string repositoryFolderPath,
		string destinationFolderPath)
	{
		CliProcessOutcome processOne = CliProcessHarness.Run(
			repositoryFolderPath,
			destinationFolderPath,
			[
				CommandRegistry.Describe(CommandId.Scan).Name,
				CommandRegistry.Describe(CommandId.Analyze).Name,
				CommandRegistry.Describe(CommandId.Exit).Name
			]);

		Assert.Equal((int)ExitCode.Success, processOne.ExitCode);
		Assert.Empty(processOne.StandardError);

		// Process two starts here: nothing above this line is reachable from it - a fresh launch,
		// pointed at the same destination, with no in-memory state carried over.
		CliProcessOutcome processTwo = CliProcessHarness.Run(
			repositoryFolderPath,
			destinationFolderPath,
			[
				CommandRegistry.Describe(CommandId.Suggest).Name,
				CommandRegistry.Describe(CommandId.Report).Name,
				CommandRegistry.Describe(CommandId.Exit).Name
			]);

		Assert.Equal((int)ExitCode.Success, processTwo.ExitCode);
		Assert.Empty(processTwo.StandardError);
	}

	private static void RunAllFourStagesInOneProcess(string repositoryFolderPath, string destinationFolderPath)
	{
		RunAllFourStagesInOneProcess(repositoryFolderPath, destinationFolderPath, out _);
	}

	private static void RunAllFourStagesInOneProcess(
		string repositoryFolderPath,
		string destinationFolderPath,
		out CliProcessOutcome outcome)
	{
		outcome = CliProcessHarness.Run(
			repositoryFolderPath,
			destinationFolderPath,
			CliProcessHarness.TheFourStagesThenExit);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);
	}

	/// <summary>
	/// Selected by <see cref="ProducedArtifacts"/>, which filters on <see cref="OutputArtifactNaming.FILE_NAME_STEM"/>
	/// before the extension - the session state file carries the <c>.json</c> extension too, under
	/// its own, different stem, so it is never mistaken for the structured data artifact here.
	/// </summary>
	private static void AssertTheArtifactsAreByteIdentical(string twoProcessDestination, string singleProcessDestination)
	{
		byte[] twoProcessMarkdown = ProducedArtifacts.BytesOfTheOnly(twoProcessDestination, OutputArtifactKind.MarkdownReport);
		byte[] singleProcessMarkdown = ProducedArtifacts.BytesOfTheOnly(singleProcessDestination, OutputArtifactKind.MarkdownReport);
		byte[] twoProcessJson = ProducedArtifacts.BytesOfTheOnly(twoProcessDestination, OutputArtifactKind.StructuredDataArtifact);
		byte[] singleProcessJson = ProducedArtifacts.BytesOfTheOnly(singleProcessDestination, OutputArtifactKind.StructuredDataArtifact);

		Assert.Equal(singleProcessMarkdown, twoProcessMarkdown);
		Assert.Equal(singleProcessJson, twoProcessJson);
	}
}
