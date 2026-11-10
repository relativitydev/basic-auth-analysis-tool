using System.Globalization;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Configuration;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// A CONFIGURATION THAT DISABLES A LANGUAGE NARROWS SCAN'S OWN CANDIDATE SELECTION, AND
/// EVERY ONE OF THE THREE REPORTING SURFACES SAYS SO - THROUGH THE REAL RESOLUTION CHAIN, NOT A
/// HAND-BUILT <see cref="EffectiveConfiguration"/>.
///
/// <see cref="ConfigurationResolver.Resolve(Session.RepositoryFolderPath, string?, string)"/> is the
/// one seam every fixture here goes through, exactly as <c>ConfigurationResolverTests</c> already
/// does - a real <c>.migrate-auth.json</c> is written into a temporary user-profile folder, and the
/// repository side of the chain is a second, unrelated temporary folder that carries no file of its
/// own, so the resolver's "absent" path is what runs for it. Neither temporary folder is the real
/// user profile.
/// </summary>
public sealed class DisabledLanguageConfigurationTests
{
	private const string _CSHARP_FILE_NAME = "HighFinding.cs";
	private const string _PYTHON_FILE_NAME = "script.py";
	private const string _DIFFERS_FROM_SESSION_FIELD = "enabledLanguagesDifferFromSession";

	/// <summary>The C# recommendation route tests' own High-finding fixture, reused verbatim: synthetic, with generic names.</summary>
	private const string _csharpHighFindingContent =
		"class HighFindingFixture\n{\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	private const string _pythonContent = "print('hello')\n";

	[Fact]
	public void ADisabledCSharpYieldsZeroCandidatesAndZeroFindingsAndIsMarkedDisabledOnAllThreeSurfaces()
	{
		using PipelineHarness harness = HarnessDisabling(AssessedLanguage.CSharp);
		harness.WriteSourceFile(_CSHARP_FILE_NAME, _csharpHighFindingContent);

		harness.RunTheWholeWorkflow();

		Assert.Empty(harness.State.CandidateRelativePaths);
		Assert.Empty(harness.State.Findings);

		Assert.Contains(ScanStage.DISABLED_ROW_SUFFIX, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(
			Formatted(ScanStage.DISABLED_LANGUAGES_SUMMARY_FORMAT, 1, 1),
			harness.Transcript,
			StringComparison.Ordinal);

		string markdown = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Contains(MarkdownReportRenderer.ASSESSED_DISABLED, markdown, StringComparison.Ordinal);
		Assert.Contains(CoverageStatement.DISABLED_BY_CONFIGURATION, markdown, StringComparison.Ordinal);

		JsonElement cSharpRow = InventoryRow(harness, AssessedLanguage.CSharp);

		Assert.False(cSharpRow.GetProperty("assessed").GetBoolean());
		Assert.Equal("disabledByConfiguration", cSharpRow.GetProperty("notAssessedReason").GetString());
	}

	/// <summary>
	/// C# enabled, everything else - Python among it - disabled by the same profile file: C#'s
	/// finding is unaffected, and Python's inventory row - which carries no analyzer either, but for
	/// a different reason - reads disabled rather than "no analyzer in this release".
	/// </summary>
	[Fact]
	public void AMixedConfigurationKeepsCSharpFindingsAndMarksPythonDisabledRatherThanUnanalyzed()
	{
		using PipelineHarness harness = HarnessEnablingOnly(AssessedLanguage.CSharp);
		harness.WriteSourceFile(_CSHARP_FILE_NAME, _csharpHighFindingContent);
		harness.WriteSourceFile(_PYTHON_FILE_NAME, _pythonContent);

		harness.RunTheWholeWorkflow();

		Assert.Equal([_CSHARP_FILE_NAME], harness.State.CandidateRelativePaths);
		Assert.Single(harness.State.Findings);

		JsonElement pythonRow = InventoryRow(harness, AssessedLanguage.Python);

		Assert.False(pythonRow.GetProperty("assessed").GetBoolean());
		Assert.Equal("disabledByConfiguration", pythonRow.GetProperty("notAssessedReason").GetString());

		string markdown = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.EndsWith(
			$"| {MarkdownReportRenderer.ASSESSED_DISABLED} |",
			MarkdownTableRow(markdown, AssessedLanguage.Python),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The resume-parity acceptance test, repeated here under a configuration that is NOT
	/// the all-enabled default - the property the configuration must not have disturbed.
	/// </summary>
	[Fact]
	public void ResumingAcrossTwoProcessesStaysByteIdenticalUnderANonDefaultEnabledSet()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText(_CSHARP_FILE_NAME, _csharpHighFindingContent);

		RepositoryFolderPath repository = fixture.Repository;
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		EffectiveConfiguration configuration = ResolveConfigurationEnablingOnly(AssessedLanguage.CSharp);

		using TemporaryFolder singleProcessDestination = TemporaryFolder.Create();
		using TemporaryFolder twoProcessDestination = TemporaryFolder.Create();

		(string singleMarkdown, string singleJson) = RunSingleUninterruptedProcess(
			repository,
			catalog,
			configuration,
			singleProcessDestination);
		(string resumedMarkdown, string resumedJson) = RunAcrossTwoProcesses(
			repository,
			catalog,
			configuration,
			twoProcessDestination);

		Assert.Equal(singleMarkdown, resumedMarkdown);
		Assert.Equal(singleJson, resumedJson);
	}

	/// <summary>
	/// A session resumed under a configuration DIFFERENT from the one Scan actually
	/// used states so, on the console and in Section 2, and Section 2's per-row state still reflects
	/// the scan-time set rather than the newly current one.
	/// </summary>
	[Fact]
	public void AConfigurationChangedBetweenSessionsAfterScanStatesSoAndSectionTwoReflectsTheScanTimeSet()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText(_CSHARP_FILE_NAME, _csharpHighFindingContent);
		fixture.WriteText(_PYTHON_FILE_NAME, _pythonContent);

		RepositoryFolderPath repository = fixture.Repository;
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		using TemporaryFolder destination = TemporaryFolder.Create();
		DocumentationDestinationFolderPath destinationFolder =
			DocumentationDestinationFolderPath.FromSuppliedText(destination.Path);

		EffectiveConfiguration scanTimeConfiguration = ResolveConfigurationEnablingOnly(
			AssessedLanguage.CSharp,
			AssessedLanguage.Python);
		StageSet processOne = BuildStages(repository, destinationFolder, catalog, scanTimeConfiguration);
		SessionStateStore store = new(destinationFolder);
		SessionState processOneState = SessionState.Fresh(catalog.Version, repository.Label);

		Assert.Equal(StageRunOutcome.Completed, processOne.Scan.Run(processOneState, CancellationToken.None));
		processOneState.MarkCompleted(CommandId.Scan);
		Assert.Equal(StageRunOutcome.Completed, processOne.Analyze.Run(processOneState, CancellationToken.None));
		processOneState.MarkCompleted(CommandId.Analyze);
		store.Save(repository, processOneState);

		EffectiveConfiguration laterConfiguration = ResolveConfigurationEnablingOnly(AssessedLanguage.CSharp);
		SessionStateLoad reloaded = store.Load(repository, catalog);
		StageSet processTwo = BuildStages(repository, destinationFolder, catalog, laterConfiguration);

		Assert.Equal(StageRunOutcome.Completed, processTwo.Report.Run(reloaded.State, CancellationToken.None));

		Assert.Contains(
			ReportStage.SCAN_ENABLED_LANGUAGES_STALE_MESSAGE,
			processTwo.Output.ToString(),
			StringComparison.Ordinal);

		string markdown = File.ReadAllText(WrittenArtifactPath(destination, OutputArtifactNaming.MARKDOWN_FILE_EXTENSION));

		Assert.Contains(MarkdownReportRenderer.SCAN_ENABLED_LANGUAGES_STALE_MESSAGE, markdown, StringComparison.Ordinal);

		using JsonDocument artifact = JsonDocument.Parse(
			File.ReadAllText(WrittenArtifactPath(destination, OutputArtifactNaming.STRUCTURED_DATA_FILE_EXTENSION)));

		Assert.True(ScanSummaryOf(artifact).GetProperty(_DIFFERS_FROM_SESSION_FIELD).GetBoolean());
		Assert.EndsWith(
			$"| {MarkdownReportRenderer.ASSESSED_NO} |",
			MarkdownTableRow(markdown, AssessedLanguage.Python),
			StringComparison.Ordinal);
	}

	/// <summary>A session whose scan and report ran under the same configuration carries the flag as false.</summary>
	[Fact]
	public void AScanAndAReportUnderTheSameConfigurationCarryTheStaleScanFlagAsFalse()
	{
		using PipelineHarness harness = HarnessDisabling(AssessedLanguage.Python);
		harness.WriteSourceFile(_CSHARP_FILE_NAME, _csharpHighFindingContent);

		harness.RunTheWholeWorkflow();

		using JsonDocument artifact = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));

		Assert.False(ScanSummaryOf(artifact).GetProperty(_DIFFERS_FROM_SESSION_FIELD).GetBoolean());
		Assert.DoesNotContain(
			MarkdownReportRenderer.SCAN_ENABLED_LANGUAGES_STALE_MESSAGE,
			harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport),
			StringComparison.Ordinal);
	}

	private static PipelineHarness HarnessDisabling(params AssessedLanguage[] disabledLanguages)
	{
		string[] enabledNames =
		[
			.. AssessedLanguages.AllInDeclaredOrder
				.Where(language => !disabledLanguages.Contains(language))
				.Select(AssessedLanguages.CanonicalName)
		];

		return HarnessWithProfile(enabledNames);
	}

	private static PipelineHarness HarnessEnablingOnly(params AssessedLanguage[] enabledLanguages)
	{
		return HarnessWithProfile([.. enabledLanguages.Select(AssessedLanguages.CanonicalName)]);
	}

	private static PipelineHarness HarnessWithProfile(string[] enabledLanguageNames)
	{
		EffectiveConfiguration configuration = ResolveConfigurationFromProfile(enabledLanguageNames);

		return PipelineHarness.CreateOverTheEmbeddedCatalogWithConfiguration(configuration);
	}

	private static EffectiveConfiguration ResolveConfigurationEnablingOnly(params AssessedLanguage[] enabledLanguages)
	{
		return ResolveConfigurationFromProfile([.. enabledLanguages.Select(AssessedLanguages.CanonicalName)]);
	}

	/// <summary>
	/// The workspace's one authorized seam: a real profile file, read by the real resolver, against a
	/// repository-side folder that carries none of its own.
	/// </summary>
	private static EffectiveConfiguration ResolveConfigurationFromProfile(string[] enabledLanguageNames)
	{
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder throwawayRepositoryFolder = TemporaryFolder.Create();

		string quotedNames = string.Join(", ", enabledLanguageNames.Select(name => $"\"{name}\""));

		ConfigurationFile.WriteInto(profileFolder, $$"""{ "enabledLanguages": [{{quotedNames}}] }""");

		RepositoryFolderPath throwawayRepository = RepositoryFolderPath.FromSuppliedText(throwawayRepositoryFolder.Path);

		return ConfigurationResolver.Resolve(throwawayRepository, null, profileFolder.Path);
	}

	private static JsonElement ScanSummaryOf(JsonDocument artifact)
	{
		return artifact.RootElement.GetProperty("scanSummary");
	}

	private static JsonElement InventoryRow(PipelineHarness harness, AssessedLanguage language)
	{
		using JsonDocument document = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));

		return document.RootElement
			.Clone()
			.GetProperty("scanSummary")
			.GetProperty("languages")
			.EnumerateArray()
			.Single(row => row.GetProperty("language").GetString() == AssessedLanguages.CanonicalName(language));
	}

	private static string Formatted(string format, int first, int second)
	{
		return string.Format(CultureInfo.InvariantCulture, format, first, second);
	}

	/// <summary>
	/// SECTION 2'S OWN TABLE ROW FOR ONE LANGUAGE, found by its leading cell rather than by an exact
	/// reconstruction of every column - which is what keeps this fixture from having to predict a
	/// file's own line count and share percentage. Section 1's coverage table carries a two-cell row
	/// for the same language under the same leading cell text, so the search is confined to the text
	/// between Section 2's own heading and Section 3's, never the whole document.
	/// </summary>
	private static string MarkdownTableRow(string markdown, AssessedLanguage language)
	{
		int sectionTwoStart = markdown.IndexOf(MarkdownReportRenderer.SCAN_SUMMARY_HEADING, StringComparison.Ordinal);
		int sectionTwoEnd = markdown.IndexOf(MarkdownReportRenderer.SKIPPED_ITEMS_HEADING, StringComparison.Ordinal);
		string sectionTwo = markdown[sectionTwoStart..sectionTwoEnd];
		string rowPrefix = $"| {AssessedLanguages.CanonicalName(language)} |";

		return sectionTwo
			.Split('\n')
			.Single(line => line.StartsWith(rowPrefix, StringComparison.Ordinal));
	}

	private static string WrittenArtifactPath(TemporaryFolder destination, string extension)
	{
		string[] writtenFiles = Directory.GetFiles(destination.Path, OutputArtifactNaming.FILE_NAME_STEM + "*");

		return Array.Find(writtenFiles, path => path.EndsWith(extension, StringComparison.Ordinal))!;
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
		StageSet stages = BuildStages(repository, destinationFolder, catalog, configuration);
		SessionState state = SessionState.Fresh(catalog.Version, repository.Label);

		Assert.Equal(StageRunOutcome.Completed, stages.Scan.Run(state, CancellationToken.None));
		Assert.Equal(StageRunOutcome.Completed, stages.Analyze.Run(state, CancellationToken.None));
		Assert.Equal(StageRunOutcome.Completed, stages.Suggest.Run(state, CancellationToken.None));
		Assert.Equal(StageRunOutcome.Completed, stages.Report.Run(state, CancellationToken.None));

		return ReadBothArtifacts(destination);
	}

	private static (string Markdown, string Json) RunAcrossTwoProcesses(
		RepositoryFolderPath repository,
		LanguageCatalog catalog,
		EffectiveConfiguration configuration,
		TemporaryFolder destination)
	{
		DocumentationDestinationFolderPath destinationFolder =
			DocumentationDestinationFolderPath.FromSuppliedText(destination.Path);

		StageSet processOne = BuildStages(repository, destinationFolder, catalog, configuration);
		SessionStateStore processOneStore = new(destinationFolder);
		SessionState processOneState = SessionState.Fresh(catalog.Version, repository.Label);

		Assert.Equal(StageRunOutcome.Completed, processOne.Scan.Run(processOneState, CancellationToken.None));
		processOneState.MarkCompleted(CommandId.Scan);
		processOneStore.Save(repository, processOneState);

		Assert.Equal(StageRunOutcome.Completed, processOne.Analyze.Run(processOneState, CancellationToken.None));
		processOneState.MarkCompleted(CommandId.Analyze);
		processOneStore.Save(repository, processOneState);

		SessionStateStore processTwoStore = new(destinationFolder);
		SessionStateLoad reloaded = processTwoStore.Load(repository, catalog);

		Assert.True(reloaded.WasResumed);

		StageSet processTwo = BuildStages(repository, destinationFolder, catalog, configuration);

		Assert.Equal(StageRunOutcome.Completed, processTwo.Suggest.Run(reloaded.State, CancellationToken.None));
		Assert.Equal(StageRunOutcome.Completed, processTwo.Report.Run(reloaded.State, CancellationToken.None));

		return ReadBothArtifacts(destination);
	}

	private static (string Markdown, string Json) ReadBothArtifacts(TemporaryFolder destination)
	{
		string markdown = File.ReadAllText(WrittenArtifactPath(destination, OutputArtifactNaming.MARKDOWN_FILE_EXTENSION));
		string json = File.ReadAllText(WrittenArtifactPath(destination, OutputArtifactNaming.STRUCTURED_DATA_FILE_EXTENSION));

		return (markdown, json);
	}

	/// <summary>
	/// The decorator-free composition, built exactly as <c>Program.BuildStageRunner</c> builds it -
	/// see <c>ResumedReportParityTests</c>'s own twin of this helper for why it is not shared: two
	/// independent compositions sharing nothing but the repository on disk is what "a second process"
	/// means here.
	/// </summary>
	private static StageSet BuildStages(
		RepositoryFolderPath repository,
		DocumentationDestinationFolderPath destination,
		LanguageCatalog catalog,
		EffectiveConfiguration configuration)
	{
		SkippedItemLedger skippedItems = new();
		EmissionPoint emissionPoint = new([skippedItems]);
		IGenericRecommendationProvider genericRecommendations = new GenericRecommendationProvider();
		StringWriter output = new();

		ScanStage scan = new(new ScanStageContext
		{
			Repository = repository,
			Catalog = catalog,
			Configuration = configuration,
			EmissionPoint = emissionPoint,
			SkippedItems = skippedItems,
			Output = output,
			TimeProvider = TimeProvider.System
		});

		AnalyzeStage analyze = new(new AnalyzeStageContext
		{
			Repository = repository,
			Catalog = catalog,
			Configuration = configuration,
			EmissionPoint = emissionPoint,
			SkippedItems = skippedItems,
			Output = output,
			TimeProvider = TimeProvider.System
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
			TimeProvider = TimeProvider.System
		});

		return new StageSet(scan, analyze, suggest, report, output);
	}

	private readonly record struct StageSet(
		ScanStage Scan,
		AnalyzeStage Analyze,
		SuggestStage Suggest,
		ReportStage Report,
		StringWriter Output);
}
