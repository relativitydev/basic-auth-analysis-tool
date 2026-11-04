using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Progress;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE FOUR STAGES AS THE SESSION ACTUALLY BUILDS THEM, OVER A REAL TREE AND A REAL DESTINATION.
///
/// This harness is deliberately the composition from <c>Program</c> rather than a simpler one. What
/// these tests exercise IS the composition - the stages themselves are thin over components that were
/// already tested in isolation - so a harness that wired them differently would be testing an
/// arrangement that ships nowhere. The runner it exposes is the same decorator chain, in the same
/// order, over the same <c>AiAssistNotAvailableStageRunner</c>.
///
/// THE SESSION-SCOPED OBJECT IS SHARED HERE EXACTLY AS IT IS IN THE PRODUCT, and that is the
/// property most of these tests turn on: one skipped-item ledger, built once, cleared by each of
/// Scan and Analyze at the start of its own run, and snapshotted onto <c>SessionState</c> at
/// completion. A harness that built a fresh ledger per stage would hide that clearing
/// behaviour entirely.
/// </summary>
internal sealed class PipelineHarness : IDisposable
{
	private readonly RepositoryFixture _fixture;
	private readonly TemporaryFolder _destination;

	private PipelineHarness(
		RepositoryFixture fixture,
		LanguageCatalog catalog,
		ConfidenceLevel minimumLevel,
		TimeProvider clock,
		EffectiveConfiguration? configuration = null,
		string? destinationConsoleDisplayText = null)
	{
		_fixture = fixture;
		_destination = TemporaryFolder.Create();

		Catalog = catalog;
		Configuration = configuration ?? BuildConfiguration(minimumLevel);
		Output = new StringWriter();
		Clock = clock;
		Destination = destinationConsoleDisplayText is null
			? DocumentationDestinationFolderPath.FromSuppliedText(_destination.Path)
			: DocumentationDestinationFolderPath.FromResolvedText(_destination.Path, destinationConsoleDisplayText);
		StateStore = new SessionStateStore(Destination);
		State = SessionState.Fresh(catalog.Version, fixture.Repository.Label);
		Runner = BuildRunner();
	}

	internal LanguageCatalog Catalog { get; }

	internal EffectiveConfiguration Configuration { get; }

	internal StringWriter Output { get; }

	/// <summary>
	/// The clock the stages are built with. It is a stepping fake for every test that is about
	/// behaviour - nothing waits, and the progress cadence never engages by accident - and the
	/// system clock for the one caller that is measuring, where a clock the product does not ship
	/// with would leave the per-item clock read out of the figure.
	/// </summary>
	internal TimeProvider Clock { get; }

	internal DocumentationDestinationFolderPath Destination { get; }

	internal SessionStateStore StateStore { get; }

	internal SessionState State { get; }

	internal IWorkflowStageRunner Runner { get; }

	internal RepositoryFolderPath Repository => _fixture.Repository;

	/// <summary>The repository's root on disk, for a test that asks the platform what it returns.</summary>
	internal string RepositoryRootPath => _fixture.RootPath;

	internal SkippedItemLedger SkippedItems => _fixture.Ledger;

	internal string Transcript => Output.ToString();

	/// <summary>
	/// A harness over the pipeline analyzer, which declares a real language name.
	/// <paramref name="languageFallbackText"/> is optional and defaults to null, which keeps the harness
	/// exercising the shared engine's language-neutral fallback.
	/// </summary>
	internal static PipelineHarness Create(
		ConfidenceLevel minimumLevel = ConfidenceLevels.WIDEST,
		string? languageFallbackText = null)
	{
		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			PipelineLanguageAnalyzer.BuildCatalog(languageFallbackText),
			minimumLevel,
			new SteppingTimeProvider());
	}

	/// <summary>
	/// A harness whose destination is shown on the console as <paramref name="destinationConsoleDisplayText"/>
	/// (for example <c>./Outputs</c>) while it is written to a real temporary folder, so a test can tell the text
	/// the user supplied from the canonical path.
	/// </summary>
	internal static PipelineHarness CreateWithDestinationShownAs(string destinationConsoleDisplayText)
	{
		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			PipelineLanguageAnalyzer.BuildCatalog(null),
			ConfidenceLevels.WIDEST,
			new SteppingTimeProvider(),
			destinationConsoleDisplayText: destinationConsoleDisplayText);
	}

	/// <summary>
	/// A harness over the pipeline analyzer with a caller-supplied configuration, for the tests that
	/// are about where a setting came from rather than about what it was.
	/// </summary>
	internal static PipelineHarness CreateWithConfiguration(EffectiveConfiguration configuration)
	{
		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			PipelineLanguageAnalyzer.BuildCatalog(null),
			configuration.MinimumConfidenceLevel,
			new SteppingTimeProvider(),
			configuration);
	}

	/// <summary>
	/// A harness over <see cref="ExemplarFixtureCatalog"/>, for the tests that are about a language
	/// the assessment does not know: the reference analyzer's names are not among the eight.
	/// </summary>
	internal static PipelineHarness CreateOverTheExemplarFixtureCatalog()
	{
		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			ExemplarFixtureCatalog.Build(),
			ConfidenceLevels.WIDEST,
			new SteppingTimeProvider());
	}

	/// <summary>
	/// A harness over a caller-supplied catalog and clock, for the tests that force the lexical budget
	/// to trip by making every read of the clock advance it.
	/// </summary>
	internal static PipelineHarness CreateOverCatalog(LanguageCatalog catalog, TimeProvider clock)
	{
		return new PipelineHarness(RepositoryFixture.CreateEmpty(), catalog, ConfidenceLevels.WIDEST, clock);
	}

	/// <summary>
	/// A harness over the EMBEDDED catalog, for the tests that are about what this build actually
	/// ships rather than about what a language analyzer will do.
	/// </summary>
	internal static PipelineHarness CreateOverTheEmbeddedCatalog()
	{
		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			LanguageCatalogRegistry.BuildEmbeddedCatalog(),
			ConfidenceLevels.WIDEST,
			new SteppingTimeProvider());
	}

	/// <summary>
	/// A harness over the EMBEDDED catalog with a caller-supplied configuration, for the tests
	/// that are about candidate selection and the assessed/disabled state under a
	/// configuration that disables one or more of the eight languages. The embedded catalog declares its
	/// analyzers under the real <c>AssessedLanguage</c> canonical names - "CSharp" among them - which
	/// is what a configuration's <c>LanguageEnablement</c> can actually narrow.
	/// </summary>
	internal static PipelineHarness CreateOverTheEmbeddedCatalogWithConfiguration(EffectiveConfiguration configuration)
	{
		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			LanguageCatalogRegistry.BuildEmbeddedCatalog(),
			ConfidenceLevels.WIDEST,
			new SteppingTimeProvider(),
			configuration);
	}

	/// <summary>
	/// A harness built with the clock the product ships with, for MEASUREMENT rather than for
	/// behaviour. Every other caller wants the stepping fake; a caller timing a stage wants the
	/// per-item clock read, the progress cadence and the match bound all costing what they
	/// actually cost in a session.
	/// </summary>
	internal static PipelineHarness CreateOverTheSystemClock()
	{
		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			PipelineLanguageAnalyzer.BuildCatalog(),
			ConfidenceLevels.WIDEST,
			TimeProvider.System);
	}

	/// <summary>
	/// A harness on the system clock over the pipeline analyzer declaring the supplied flow signals,
	/// for the volume proof of flow-signal evaluation.
	/// </summary>
	internal static PipelineHarness CreateOverTheSystemClockWithFlowSignals(ImmutableArray<FlowSignal> flowSignals)
	{
		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			PipelineLanguageAnalyzer.BuildCatalog(flowSignals: flowSignals),
			ConfidenceLevels.WIDEST,
			TimeProvider.System);
	}

	/// <summary>
	/// A harness over the pipeline analyzer declaring the supplied flow signals, under a
	/// configuration whose effective default flow is the one supplied. The origin is the profile
	/// where a default other than the built-in one was asked for, and the built-in default otherwise.
	/// </summary>
	internal static PipelineHarness CreateWithFlowSignals(
		ImmutableArray<FlowSignal> flowSignals,
		CredentialFlow defaultFlow = CredentialFlows.BUILT_IN_DEFAULT)
	{
		EffectiveConfiguration baseline = BuildConfiguration(ConfidenceLevels.WIDEST);
		ConfigurationSource origin = defaultFlow == CredentialFlows.BUILT_IN_DEFAULT
			? ConfigurationSource.BuiltInDefault
			: ConfigurationSource.UserProfileFile;

		EffectiveConfiguration configuration = baseline with
		{
			DefaultFlow = defaultFlow,
			Provenance = baseline.Provenance with { DefaultFlowOrigin = origin }
		};

		return new PipelineHarness(
			RepositoryFixture.CreateEmpty(),
			PipelineLanguageAnalyzer.BuildCatalog(flowSignals: flowSignals),
			ConfidenceLevels.WIDEST,
			new SteppingTimeProvider(),
			configuration);
	}

	/// <summary>Writes a source file into the tree under assessment.</summary>
	internal void WriteSourceFile(string relativePath, string text)
	{
		_fixture.WriteText(relativePath, text);
	}

	/// <summary>Writes a source file as the exact bytes given, for a file in an encoding the text writer does not produce.</summary>
	internal void WriteSourceBytes(string relativePath, byte[] bytes)
	{
		_fixture.WriteBytes(relativePath, bytes);
	}

	/// <summary>Deletes a file from the tree, for the candidate that vanishes between two stages.</summary>
	internal void DeleteSourceFile(string relativePath)
	{
		File.Delete(Path.Combine(_fixture.RootPath, relativePath));
	}

	/// <summary>One stage, through the runner chain the product builds.</summary>
	internal StageRunOutcome Run(CommandId workflowCommand, CancellationToken cancellationToken = default)
	{
		return Runner.Run(workflowCommand, State, cancellationToken);
	}

	/// <summary>
	/// One stage, with its completion recorded the way <c>SessionShell</c> records it. The shell
	/// owns gating and the cascade, so a harness that ran stages without marking them would let a
	/// test pass over a chain the session would have refused.
	/// </summary>
	internal StageRunOutcome RunAndCommit(CommandId workflowCommand, CancellationToken cancellationToken = default)
	{
		StageRunOutcome outcome = Run(workflowCommand, cancellationToken);

		if (outcome is StageRunOutcome.Completed)
		{
			State.MarkCompleted(workflowCommand);
			StateStore.Save(Repository, State);
		}
		else
		{
			State.MarkNotCompleted(workflowCommand);
		}

		return outcome;
	}

	/// <summary>The whole workflow, in the order the stage chain fixes.</summary>
	internal ImmutableArray<StageRunOutcome> RunTheWholeWorkflow()
	{
		return
		[
			RunAndCommit(CommandId.Scan),
			RunAndCommit(CommandId.Analyze),
			RunAndCommit(CommandId.Suggest),
			RunAndCommit(CommandId.Report)
		];
	}

	/// <summary>The artifact file names the destination holds, in a stable order.</summary>
	internal ImmutableArray<string> WrittenArtifactNames()
	{
		return
		[
			.. Directory.EnumerateFiles(_destination.Path)
				.Select(Path.GetFileName)
				.OfType<string>()
				.Where(fileName => fileName.StartsWith(OutputArtifactNaming.FILE_NAME_STEM, StringComparison.Ordinal))
				.Order(StringComparer.Ordinal)
		];
	}

	/// <summary>The text of the one artifact of this kind the destination holds.</summary>
	internal string ReadWrittenArtifact(OutputArtifactKind kind)
	{
		string extension = kind is OutputArtifactKind.MarkdownReport
			? OutputArtifactNaming.MARKDOWN_FILE_EXTENSION
			: OutputArtifactNaming.STRUCTURED_DATA_FILE_EXTENSION;

		string fileName = WrittenArtifactNames().Single(name =>
			name.EndsWith(extension, StringComparison.Ordinal));

		return File.ReadAllText(_destination.Combine(fileName));
	}

	/// <summary>The session state as a fresh process would read it back.</summary>
	internal SessionStateLoad ReloadState()
	{
		return StateStore.Load(Repository, Catalog);
	}

	/// <summary>The state file's own text, for the tests that are about what the session state persists.</summary>
	internal string ReadWrittenStateDocument()
	{
		return File.ReadAllText(_destination.Combine(SessionStateStore.BuildStateFileName(Repository)));
	}

	public void Dispose()
	{
		Output.Dispose();
		_destination.Dispose();
		_fixture.Dispose();
	}

	/// <summary>
	/// Not private: The resumed-report parity test builds a second, independent stage
	/// composition over the same repository and reuses this rather than duplicating it.
	/// </summary>
	internal static EffectiveConfiguration BuildConfiguration(ConfidenceLevel minimumLevel)
	{
		return BuildConfiguration(minimumLevel, disabledLanguages: []);
	}

	/// <summary>
	/// Every language enabled except the ones named - the pipeline-harness twin of
	/// <c>ReportHarness.ConfigurationWith</c>, kept apart from it because the two harnesses build
	/// configurations for different purposes (a real pipeline run versus a hand-built report) and
	/// neither should have to reach into the other's test namespace for a fixture this small.
	/// </summary>
	internal static EffectiveConfiguration BuildConfiguration(
		ConfidenceLevel minimumLevel,
		params AssessedLanguage[] disabledLanguages)
	{
		ImmutableArray<LanguageEnablement> enablement =
		[
			.. AssessedLanguages.AllInDeclaredOrder.Select(language => new LanguageEnablement
			{
				Language = language,
				IsEnabled = !disabledLanguages.Contains(language)
			})
		];

		return new EffectiveConfiguration
		{
			LanguageEnablement = enablement,
			MinimumConfidenceLevel = minimumLevel,
			OutputPathText = null,
			Provenance = new ConfigurationProvenance
			{
				EnabledLanguagesOrigin = ConfigurationSource.BuiltInDefault,
				MinimumConfidenceLevelOrigin = ConfigurationSource.BuiltInDefault,
				OutputPathOrigin = ConfigurationSource.WorkingDirectoryAtLaunch,
				LanguageEnablement = enablement,
				IgnoredRepositoryFields = [],
				ConfigurationFiles =
				[
					new ConfigurationFileState
					{
						Location = ConfigurationFileLocation.RepositoryRoot,
						Condition = ConfigurationFileCondition.Absent
					},
					new ConfigurationFileState
					{
						Location = ConfigurationFileLocation.UserProfile,
						Condition = ConfigurationFileCondition.Absent
					}
				]
			}
		};
	}

	/// <summary>
	/// The decorator chain, built inside out exactly as <c>Program.BuildStageRunner</c> builds it -
	/// the empty runner at the bottom, then scan, analyze, suggest and report.
	/// </summary>
	private IWorkflowStageRunner BuildRunner()
	{
		IGenericRecommendationProvider genericRecommendations = new GenericRecommendationProvider();

		ScanStage scanStage = new(new ScanStageContext
		{
			Repository = Repository,
			Catalog = Catalog,
			Configuration = Configuration,
			EmissionPoint = _fixture.EmissionPoint,
			SkippedItems = SkippedItems,
			Output = Output,
			TimeProvider = Clock
		});

		AnalyzeStage analyzeStage = new(new AnalyzeStageContext
		{
			Repository = Repository,
			Catalog = Catalog,
			Configuration = Configuration,
			EmissionPoint = _fixture.EmissionPoint,
			SkippedItems = SkippedItems,
			Output = Output,
			TimeProvider = Clock
		});

		SuggestStage suggestStage = new(new SuggestStageContext
		{
			Catalog = Catalog,
			GenericRecommendations = genericRecommendations,
			Configuration = Configuration,
			Output = Output
		});

		ReportStage reportStage = new(new ReportStageContext
		{
			Catalog = Catalog,
			Configuration = Configuration,
			RepositoryLabel = Repository.Label,
			ArtifactWriter = new AtomicArtifactWriter(Repository, Destination),
			GenericRecommendations = genericRecommendations,
			Output = Output,
			TimeProvider = Clock
		});

		return new ReportWiredStageRunner(
			reportStage,
			new SuggestWiredStageRunner(
				suggestStage,
				new AnalyzeWiredStageRunner(
					analyzeStage,
					new ScanWiredStageRunner(scanStage, new AiAssistNotAvailableStageRunner(Output)))));
	}
}
