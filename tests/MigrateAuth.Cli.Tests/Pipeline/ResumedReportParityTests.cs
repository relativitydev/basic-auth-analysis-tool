using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Progress;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE ACCEPTANCE TEST: A REPORT RESUMED IN A SECOND PROCESS MUST NOT DIFFER FROM ONE A
/// SINGLE PROCESS PRODUCED WITHOUT EVER PERSISTING AND RELOADING ANYTHING.
///
/// <see cref="PipelineHarness"/> is deliberately not reused here - it owns one destination and one
/// composition for the life of the harness, and what these tests need is TWO independent
/// compositions, built the way <c>Program.BuildStageRunner</c> builds them, sharing nothing but the
/// repository on disk. That is what "a second process" means for a set of stages that are otherwise
/// pure functions of the repository and the session state: a fresh <see cref="SkippedItemLedger"/>,
/// a fresh <see cref="EmissionPoint"/>, and - for the resumed half - a session state read back
/// through <see cref="SessionStateStore"/> rather than the one still held in memory.
/// </summary>
public sealed class ResumedReportParityTests
{
	private const string _CREDENTIAL_FILE_NAME = "Credentials.cs";
	private const string _UNREADABLE_FILE_NAME = "Unreadable.cs";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";

	private static readonly string _credentialContent =
		$"class Credentials\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, secret);\n}}\n";

	/// <summary>
	/// <c>scan</c> and <c>analyze</c> in one process, then <c>suggest</c> and <c>report</c> in a second,
	/// freshly started one. The resumed report, Section 2's numbers and both stages' skipped-item tallies
	/// included, is byte for byte the report a single, uninterrupted process writes over the identical repository.
	/// </summary>
	[Fact]
	public void AReportResumedInASecondProcessMatchesASingleProcessReportByteForByte()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText(_CREDENTIAL_FILE_NAME, _credentialContent);
		fixture.Deny(fixture.WriteText(_UNREADABLE_FILE_NAME, _ORDINARY_CONTENT));

		RepositoryFolderPath repository = fixture.Repository;
		LanguageCatalog catalog = PipelineLanguageAnalyzer.BuildCatalog();
		EffectiveConfiguration configuration = PipelineHarness.BuildConfiguration(ConfidenceLevels.WIDEST);

		using TemporaryFolder destinationSingleProcess = TemporaryFolder.Create();
		using TemporaryFolder destinationTwoProcess = TemporaryFolder.Create();

		(string singleProcessMarkdown, string singleProcessJson) = RunSingleUninterruptedProcess(
			repository,
			catalog,
			configuration,
			destinationSingleProcess);

		(string resumedMarkdown, string resumedJson) = RunAcrossTwoProcesses(
			repository,
			catalog,
			configuration,
			destinationTwoProcess);

		Assert.Equal(singleProcessMarkdown, resumedMarkdown);
		Assert.Equal(singleProcessJson, resumedJson);
		Assert.DoesNotContain("Files enumerated: 0", resumedMarkdown, StringComparison.Ordinal);
		Assert.DoesNotContain(MarkdownReportRenderer.NO_FILES_ENUMERATED, resumedMarkdown, StringComparison.Ordinal);
	}

	/// <summary>The comparator: every stage against one composition, one session state, never persisted.</summary>
	private static (string Markdown, string Json) RunSingleUninterruptedProcess(
		RepositoryFolderPath repository,
		LanguageCatalog catalog,
		EffectiveConfiguration configuration,
		TemporaryFolder destination)
	{
		DocumentationDestinationFolderPath destinationFolder =
			DocumentationDestinationFolderPath.FromSuppliedText(destination.Path);
		StageSet stages = BuildStages(repository, destinationFolder, catalog, configuration, new StringWriter());
		SessionState state = SessionState.Fresh(catalog.Version, repository.Label);

		Assert.Equal(StageRunOutcome.Completed, stages.Scan.Run(state, CancellationToken.None));
		Assert.Equal(StageRunOutcome.Completed, stages.Analyze.Run(state, CancellationToken.None));
		Assert.Equal(StageRunOutcome.Completed, stages.Suggest.Run(state, CancellationToken.None));
		Assert.Equal(StageRunOutcome.Completed, stages.Report.Run(state, CancellationToken.None));

		return ReadBothArtifacts(destination);
	}

	/// <summary>
	/// Process 1 scans and analyzes and persists after each; process 2 is built from nothing but the
	/// destination on disk, reloads through the store, and runs only <c>suggest</c> and <c>report</c>.
	/// </summary>
	private static (string Markdown, string Json) RunAcrossTwoProcesses(
		RepositoryFolderPath repository,
		LanguageCatalog catalog,
		EffectiveConfiguration configuration,
		TemporaryFolder destination)
	{
		DocumentationDestinationFolderPath destinationFolder =
			DocumentationDestinationFolderPath.FromSuppliedText(destination.Path);

		StageSet processOne = BuildStages(repository, destinationFolder, catalog, configuration, new StringWriter());
		SessionStateStore processOneStore = new(destinationFolder);
		SessionState processOneState = SessionState.Fresh(catalog.Version, repository.Label);

		Assert.Equal(StageRunOutcome.Completed, processOne.Scan.Run(processOneState, CancellationToken.None));
		processOneState.MarkCompleted(CommandId.Scan);
		processOneStore.Save(repository, processOneState);

		Assert.Equal(StageRunOutcome.Completed, processOne.Analyze.Run(processOneState, CancellationToken.None));
		processOneState.MarkCompleted(CommandId.Analyze);
		processOneStore.Save(repository, processOneState);

		// Process 2 starts here: nothing above this line is reachable from it.
		SessionStateStore processTwoStore = new(destinationFolder);
		SessionStateLoad reloaded = processTwoStore.Load(repository, catalog);

		Assert.True(reloaded.WasResumed);

		StageSet processTwo = BuildStages(repository, destinationFolder, catalog, configuration, new StringWriter());

		Assert.Equal(StageRunOutcome.Completed, processTwo.Suggest.Run(reloaded.State, CancellationToken.None));
		Assert.Equal(StageRunOutcome.Completed, processTwo.Report.Run(reloaded.State, CancellationToken.None));

		return ReadBothArtifacts(destination);
	}

	/// <summary>
	/// Selects by the artifact's own file-name stem as well as its extension, because the session
	/// state file is also JSON and sits in the same destination folder, and directory enumeration order
	/// differs across file systems.
	/// </summary>
	private static (string Markdown, string Json) ReadBothArtifacts(TemporaryFolder destination)
	{
		string[] writtenFiles = Directory.GetFiles(destination.Path, OutputArtifactNaming.FILE_NAME_STEM + "*");

		string markdown = File.ReadAllText(Array.Find(
			writtenFiles,
			path => path.EndsWith(OutputArtifactNaming.MARKDOWN_FILE_EXTENSION, StringComparison.Ordinal))!);
		string json = File.ReadAllText(Array.Find(
			writtenFiles,
			path => path.EndsWith(OutputArtifactNaming.STRUCTURED_DATA_FILE_EXTENSION, StringComparison.Ordinal))!);

		return (markdown, json);
	}

	/// <summary>
	/// The decorator-free composition - just the four stages, built exactly as
	/// <c>Program.BuildStageRunner</c> builds them: one ledger, one emission point, shared by Scan
	/// and Analyze only, and a Report stage that reads nothing but the session state it is handed.
	/// A fresh call is "a fresh process" for every purpose this test cares about.
	/// </summary>
	private static StageSet BuildStages(
		RepositoryFolderPath repository,
		DocumentationDestinationFolderPath destination,
		LanguageCatalog catalog,
		EffectiveConfiguration configuration,
		TextWriter output)
	{
		SkippedItemLedger skippedItems = new();
		EmissionPoint emissionPoint = new([skippedItems]);
		IGenericRecommendationProvider genericRecommendations = new GenericRecommendationProvider();
		SteppingTimeProvider clock = new();

		ScanStage scan = new(new ScanStageContext
		{
			Repository = repository,
			Catalog = catalog,
			Configuration = configuration,
			EmissionPoint = emissionPoint,
			SkippedItems = skippedItems,
			Output = output,
			TimeProvider = clock
		});

		AnalyzeStage analyze = new(new AnalyzeStageContext
		{
			Repository = repository,
			Catalog = catalog,
			Configuration = configuration,
			EmissionPoint = emissionPoint,
			SkippedItems = skippedItems,
			Output = output,
			TimeProvider = clock
		});

		SuggestStage suggest = new(new SuggestStageContext
		{
			Catalog = catalog,
			GenericRecommendations = genericRecommendations,
			Configuration = configuration,
			Output = output
		});

		ReportStage report = new(new ReportStageContext
		{
			Catalog = catalog,
			Configuration = configuration,
			RepositoryLabel = repository.Label,
			ArtifactWriter = new AtomicArtifactWriter(repository, destination),
			GenericRecommendations = genericRecommendations,
			Output = output,
			TimeProvider = clock
		});

		return new StageSet(scan, analyze, suggest, report);
	}

	private readonly record struct StageSet(ScanStage Scan, AnalyzeStage Analyze, SuggestStage Suggest, ReportStage Report);
}
