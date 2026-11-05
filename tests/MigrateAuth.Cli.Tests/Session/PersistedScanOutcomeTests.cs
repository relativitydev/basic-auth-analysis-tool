using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// SECTION 2'S NUMBERS, EACH STAGE'S OWN SKIPPED-ITEM TALLIES AND THE TRUNCATION
/// COUNTER, ALL READ AS HOSTILE INPUT ON THE SAME TERMS <see cref="PersistedFindingsTests"/> ALREADY
/// APPLIES TO FINDINGS.
///
/// A document this build wrote is round-tripped first, through the real writer and the real reader,
/// which is the positive case every rejection test below is contrasted against. Every rejection case
/// after it edits one field of an otherwise well-formed document, so what is under test is the check
/// named in the test's own name.
/// </summary>
public sealed class PersistedScanOutcomeTests
{
	private const string _REPOSITORY_LABEL_PLACEHOLDER = "{repository-label}";
	private const string _VALID_LANGUAGE_NAME = "CSharp";
	private const string _CATALOG_DECLARED_LANGUAGE_NAME = "ExemplarOne";
	private const string _VALID_ENUMERATED_PATH = "src/denied.cs";

	/// <summary>
	/// A session carrying every persisted section survives a save and a load, checked through an
	/// arrange-and-save helper and two assertion helpers.
	/// </summary>
	[Fact]
	public void ScanSummarySkippedItemsAndFindingsCapOutcomeSurviveASaveAndLoad()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		LanguageCatalog catalog = ExemplarFixtureCatalog.Build();
		SessionStateStore store = new(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path));

		FindingsCapOutcome findingsCapOutcome = SaveAFullyPopulatedSession(repository, catalog, store);
		SessionStateLoad reloaded = store.Load(repository, catalog);

		AssertTheScanSectionsSurvived(reloaded);
		Assert.Equal(findingsCapOutcome, reloaded.State.FindingsCapOutcome);
	}

	/// <summary>Arranges a session carrying every persisted section, records it and saves it.</summary>
	private static FindingsCapOutcome SaveAFullyPopulatedSession(
		RepositoryFolderPath repository,
		LanguageCatalog catalog,
		SessionStateStore store)
	{
		SessionState state = SessionState.Fresh(catalog.Version, repository.Label);

		ScanSummary scanSummary = new()
		{
			FileCount = 3,
			LineCount = 42,
			CandidateFileCount = 1,
			Rows =
			[
				new LanguageInventoryRow
				{
					LanguageName = _VALID_LANGUAGE_NAME,
					FileCount = 3,
					LineCount = 42,
					CandidateFileCount = 1,
					ShareOfFilesPercentage = 12.5
				}
			]
		};

		RepositoryRelativePath deniedPath = PathOf(repository, _VALID_ENUMERATED_PATH);
		System.Collections.Immutable.ImmutableArray<SkippedItemTally> scanSkippedItems =
		[
			.. SkippedItemCategories.ALL.Select(category => new SkippedItemTally
			{
				Category = category,
				Unit = SkippedItemCategories.UnitOf(category),
				Count = category == SkippedItemCategory.UnreadableFile ? 1 : 0,
				EnumeratedPaths = category == SkippedItemCategory.UnreadableFile ? [deniedPath] : []
			})
		];

		state.RecordScanOutcome(
			["src/Credentials.cs"],
			[_CATALOG_DECLARED_LANGUAGE_NAME],
			scanSummary,
			scanSkippedItems,
			[AssessedLanguage.CSharp]);
		state.MarkCompleted(CommandId.Scan);

		FindingsCapOutcome findingsCapOutcome = new()
		{
			TotalRecognizedCount = 0,
			RetainedCount = 0,
			PerFileCapTrippedFileCount = 0,
			PerFileOmittedCount = 0,
			GlobalCapTripped = false,
			GlobalOmittedCount = 0
		};

		state.RecordAnalysisOutcome([], findingsCapOutcome, SkippedItemTallies.Empty);
		state.MarkCompleted(CommandId.Analyze);

		store.Save(repository, state);

		return findingsCapOutcome;
	}

	/// <summary>
	/// The scan summary and scan skipped-item halves. The share is RECOMPUTED on read rather
	/// than trusted from a persisted copy, so it reflects the totals above (3 files of which this
	/// row's 3 are all of them) rather than the arbitrary 12.5 written into the fixture above, which
	/// is never itself persisted.
	/// </summary>
	private static void AssertTheScanSectionsSurvived(SessionStateLoad reloaded)
	{
		Assert.True(reloaded.WasResumed);
		Assert.Equal(3, reloaded.State.ScanOutcome.FileCount);
		Assert.Equal(42, reloaded.State.ScanOutcome.LineCount);
		Assert.Equal(1, reloaded.State.ScanOutcome.CandidateFileCount);

		LanguageInventoryRow row = Assert.Single(reloaded.State.ScanOutcome.Rows);

		Assert.Equal(_VALID_LANGUAGE_NAME, row.LanguageName);
		Assert.Equal(100.0, row.ShareOfFilesPercentage);

		SkippedItemTally restoredTally = reloaded.State.ScanSkippedItems.Single(t => t.Category == SkippedItemCategory.UnreadableFile);

		Assert.Equal(1, restoredTally.Count);
		Assert.Equal([_VALID_ENUMERATED_PATH], restoredTally.EnumeratedPaths.Select(path => path.Value));

		Assert.Equal(AssessedLanguage.CSharp, Assert.Single(reloaded.State.ScanEnabledLanguages));
	}

	[Fact]
	public void AbsentScanSummarySkippedItemsAndFindingsCapOutcomeDefaultToEmptyAndAreAccepted()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText());

		Assert.True(load.WasResumed);
		Assert.Equal(ScanSummary.Empty, load.State.ScanOutcome);
		Assert.Equal(FindingsCapOutcome.Empty, load.State.FindingsCapOutcome);
		Assert.All(load.State.ScanSkippedItems, tally => Assert.Equal(0, tally.Count));
		Assert.All(load.State.AnalysisSkippedItems, tally => Assert.Equal(0, tally.Count));
	}

	/// <summary>
	/// A completed <c>scan</c> always snapshots its own summary, so a document
	/// claiming the one without the other was not written by this tool.
	/// </summary>
	[Fact]
	public void ACompletedScanWithNoPersistedScanSummaryStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStagesJson: "\"scan\"",
			scanSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson()));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, load.RejectionReason);
	}

	/// <summary>The other half of the same stage: the tallies absent, the summary present.</summary>
	[Fact]
	public void ACompletedScanWithNoPersistedScanSkippedItemsStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStagesJson: "\"scan\"",
			scanSummaryJson: SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SkippedItemTallyNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// A completed <c>analyze</c> always snapshots its own truncation counter, so
	/// a document claiming the one without the other was not written by this tool. <c>scan</c> is
	/// also completed and fully sectioned here, because <c>analyze</c> could not gate otherwise
	/// - what is under test is <c>analyze</c>'s own section alone.
	/// </summary>
	[Fact]
	public void ACompletedAnalyzeWithNoPersistedFindingsCapOutcomeStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStagesJson: "\"scan\", \"analyze\"",
			scanSummaryJson: SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON,
			scanSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			analysisSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson()));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingsCapOutcomeNotWellFormed, load.RejectionReason);
	}

	/// <summary>The other half of the same stage: the cap outcome present, the tallies absent.</summary>
	[Fact]
	public void ACompletedAnalyzeWithNoPersistedAnalysisSkippedItemsStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStagesJson: "\"scan\", \"analyze\"",
			scanSummaryJson: SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON,
			scanSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			findingsCapOutcomeJson: SessionStateDocumentSections.UntrippedFindingsCapOutcomeJson(findingsCount: 0)));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SkippedItemTallyNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// The positive complement: every section a completed <c>scan</c> and <c>analyze</c> require is
	/// present, so the document resumes despite naming both stages complete.
	/// </summary>
	[Fact]
	public void ACompletedScanAndAnalyzeWithEverySectionPresentIsAccepted()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStagesJson: "\"scan\", \"analyze\"",
			scanSummaryJson: SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON,
			scanSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			analysisSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			findingsCapOutcomeJson: SessionStateDocumentSections.UntrippedFindingsCapOutcomeJson(findingsCount: 0),
			scanEnabledLanguagesJson: SessionStateDocumentSections.EMPTY_SCAN_ENABLED_LANGUAGES_JSON,
			defaultFlowJson: SessionStateDocumentSections.DEFAULT_FLOW_JSON,
			defaultFlowOriginJson: SessionStateDocumentSections.DEFAULT_FLOW_ORIGIN_JSON,
			flowEvaluationDiagnosticsJson: SessionStateDocumentSections.EMPTY_FLOW_EVALUATION_DIAGNOSTICS_JSON));

		Assert.True(load.WasResumed);
	}

	/// <summary>
	/// The rule for the schema-5 sections: a completed <c>analyze</c> always records the
	/// default flow it decided under and the flow-signal counters, so a document that names it complete
	/// and omits either is not one this tool wrote.
	/// </summary>
	[Fact]
	public void ACompletedAnalyzeWithNoPersistedDefaultFlowStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildCompletedDocument(withDefaultFlow: false, withDiagnostics: true));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.DefaultFlowNotWellFormed, load.RejectionReason);
	}

	[Fact]
	public void ACompletedAnalyzeWithNoPersistedFlowEvaluationDiagnosticsStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildCompletedDocument(withDefaultFlow: true, withDiagnostics: false));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FlowEvaluationDiagnosticsNotWellFormed, load.RejectionReason);
	}

	[Theory]
	[InlineData("\"Sometimes\"", "\"BuiltInDefault\"")]
	[InlineData("\"clientcredentials\"", "\"BuiltInDefault\"")]
	[InlineData("\"ClientCredentials\"", "\"Somewhere\"")]
	[InlineData("\"ClientCredentials\"", "\"UserProfileFile\"")]
	[InlineData("\"ClientCredentials\"", "null")]
	[InlineData("null", "\"BuiltInDefault\"")]
	[InlineData("\"AuthorizationCode\"", "\"BuiltInDefault\"")]
	public void AMalformedDefaultFlowOrOriginStartsAFreshSession(string defaultFlowJson, string defaultFlowOriginJson)
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			defaultFlowJson: defaultFlowJson,
			defaultFlowOriginJson: defaultFlowOriginJson));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.DefaultFlowNotWellFormed, load.RejectionReason);
	}

	[Theory]
	[InlineData("{\"filesEvaluated\":-1,\"signalTripCount\":0}")]
	[InlineData("{\"filesEvaluated\":0,\"signalTripCount\":-1}")]
	[InlineData("{\"filesEvaluated\":1,\"signalTripCount\":2}")]
	[InlineData("{\"filesEvaluated\":1,\"signalTripCount\":0}")]
	[InlineData("{\"filesEvaluated\":0}")]
	public void MalformedFlowEvaluationDiagnosticsStartTheSessionFresh(string diagnosticsJson)
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(flowEvaluationDiagnosticsJson: diagnosticsJson));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FlowEvaluationDiagnosticsNotWellFormed, load.RejectionReason);
	}

	/// <summary>A candidate is exactly as attributed as it is present, and only to a language some analyzer declares.</summary>
	[Theory]
	[InlineData("")]
	[InlineData("\"ExemplarOne\", \"ExemplarOne\"")]
	[InlineData("\"NotALanguageThisCatalogDeclares\"")]
	[InlineData("null")]
	public void ACandidateWithoutAWellFormedAttributionStartsAFreshSession(string candidateLanguagesJson)
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStagesJson: "\"scan\"",
			scanSummaryJson: BuildScanSummaryJson(_CATALOG_DECLARED_LANGUAGE_NAME, fileCount: 1, candidateFileCount: 1),
			scanSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			scanEnabledLanguagesJson: SessionStateDocumentSections.EMPTY_SCAN_ENABLED_LANGUAGES_JSON,
			defaultFlowJson: SessionStateDocumentSections.DEFAULT_FLOW_JSON,
			defaultFlowOriginJson: SessionStateDocumentSections.DEFAULT_FLOW_ORIGIN_JSON,
			flowEvaluationDiagnosticsJson: SessionStateDocumentSections.EMPTY_FLOW_EVALUATION_DIAGNOSTICS_JSON,
			candidatePathsJson: "\"src/Credentials.cs\"",
			candidateLanguagesJson: candidateLanguagesJson));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CandidateAttributionNotWellFormed, load.RejectionReason);
	}

	private static string BuildCompletedDocument(bool withDefaultFlow, bool withDiagnostics)
	{
		return BuildDocumentText(
			completedStagesJson: "\"scan\", \"analyze\"",
			scanSummaryJson: SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON,
			scanSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			analysisSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			findingsCapOutcomeJson: SessionStateDocumentSections.UntrippedFindingsCapOutcomeJson(findingsCount: 0),
			scanEnabledLanguagesJson: SessionStateDocumentSections.EMPTY_SCAN_ENABLED_LANGUAGES_JSON,
			defaultFlowJson: withDefaultFlow ? SessionStateDocumentSections.DEFAULT_FLOW_JSON : null,
			defaultFlowOriginJson: withDefaultFlow ? SessionStateDocumentSections.DEFAULT_FLOW_ORIGIN_JSON : null,
			flowEvaluationDiagnosticsJson: withDiagnostics ? SessionStateDocumentSections.EMPTY_FLOW_EVALUATION_DIAGNOSTICS_JSON : null);
	}

	/// <summary>A completed <c>scan</c> always snapshots the enabled set it narrowed candidate selection with.</summary>
	[Fact]
	public void ACompletedScanWithNoPersistedScanEnabledLanguagesStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStagesJson: "\"scan\"",
			scanSummaryJson: SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON,
			scanSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson()));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanEnabledLanguagesNotWellFormed, load.RejectionReason);
	}

	[Theory]
	[InlineData("[\"NotALanguageThisBuildKnows\"]")]
	[InlineData("[\"CSharp\", \"CSharp\"]")]
	public void AMalformedScanEnabledLanguagesStartsAFreshSession(string scanEnabledLanguagesJson)
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStagesJson: "\"scan\"",
			scanSummaryJson: SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON,
			scanSkippedItemsJson: SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			scanEnabledLanguagesJson: scanEnabledLanguagesJson));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanEnabledLanguagesNotWellFormed, load.RejectionReason);
	}

	[Fact]
	public void ACatalogDeclaredLanguageNameThatIsNotOneOfTheEightIsAccepted()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			scanSummaryJson: BuildScanSummaryJson(_CATALOG_DECLARED_LANGUAGE_NAME, fileCount: 1)));

		Assert.True(load.WasResumed);
		Assert.Equal(_CATALOG_DECLARED_LANGUAGE_NAME, Assert.Single(load.State.ScanOutcome.Rows).LanguageName);
	}

	[Theory]
	[InlineData("\"fileCount\":-1,\"lineCount\":0,\"candidateFileCount\":0,\"rows\":[]", "a negative total")]
	[InlineData(
		"\"fileCount\":1,\"lineCount\":0,\"candidateFileCount\":0,\"rows\":[{\"languageName\":\"CSharp\",\"fileCount\":2,\"lineCount\":0,\"candidateFileCount\":0}]",
		"a row whose count exceeds the total it should sum into")]
	[InlineData(
		"\"fileCount\":1,\"lineCount\":0,\"candidateFileCount\":0,\"rows\":[{\"languageName\":\"NotALanguageThisBuildKnows\",\"fileCount\":1,\"lineCount\":0,\"candidateFileCount\":0}]",
		"a row naming a language outside the closed set")]
	[InlineData(
		"\"fileCount\":2,\"lineCount\":0,\"candidateFileCount\":0,\"rows\":[{\"languageName\":\"CSharp\",\"fileCount\":1,\"lineCount\":0,\"candidateFileCount\":0},{\"languageName\":\"CSharp\",\"fileCount\":1,\"lineCount\":0,\"candidateFileCount\":0}]",
		"a language named twice across rows")]
	[InlineData(
		"\"fileCount\":1,\"lineCount\":0,\"candidateFileCount\":2,\"rows\":[{\"languageName\":\"CSharp\",\"fileCount\":1,\"lineCount\":0,\"candidateFileCount\":2}]",
		"a row whose own candidate count exceeds its own file count")]
	public void AMalformedScanSummaryStartsAFreshSession(string scanSummaryBody, string reason)
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(scanSummaryJson: $"{{{scanSummaryBody}}}"));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, load.RejectionReason);
		Assert.NotEmpty(reason);
	}

	/// <summary>
	/// The scan summary's own candidate count and the persisted candidate
	/// path list are written from the same scan; a document asserting a summary of one candidate
	/// while carrying none of the paths that would back it is not one this tool produced.
	/// </summary>
	[Fact]
	public void AScanSummaryCandidateCountDisagreeingWithThePersistedCandidatePathsStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			scanSummaryJson: BuildScanSummaryJson(_VALID_LANGUAGE_NAME, fileCount: 1, candidateFileCount: 1)));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, load.RejectionReason);
	}

	[Fact]
	public void ScanSkippedItemsMissingACategoryStartsAFreshSession()
	{
		string incompleteTallies = $"[{{\"category\":\"{SkippedItemCategoryNames.NameOf(SkippedItemCategory.UnreadableFile)}\",\"count\":0,\"enumeratedPaths\":[]}}]";

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(scanSkippedItemsJson: incompleteTallies));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SkippedItemTallyNotWellFormed, load.RejectionReason);
	}

	/// <summary>
	/// Eleven entries, so the length check alone cannot explain a rejection - the last category is
	/// never mentioned and the first appears twice instead, which is what makes this specifically a
	/// duplicate-category document rather than a wrong-length one.
	/// </summary>
	[Fact]
	public void ScanSkippedItemsRepeatingACategoryStartsAFreshSession()
	{
		string oneName = SkippedItemCategoryNames.NameOf(SkippedItemCategories.ALL[0]);
		string[] entries =
		[
			.. SkippedItemCategories.ALL.Take(SkippedItemCategories.ALL.Length - 1)
				.Select(category => BuildEmptyTallyJson(SkippedItemCategoryNames.NameOf(category))),
			BuildEmptyTallyJson(oneName)
		];

		Assert.Equal(SkippedItemCategories.ALL.Length, entries.Length);

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(scanSkippedItemsJson: $"[{string.Join(",", entries)}]"));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SkippedItemTallyNotWellFormed, load.RejectionReason);
	}

	[Fact]
	public void ScanSkippedItemsCarryingAnUndeclaredCategoryNameStartsAFreshSession()
	{
		string[] entries =
		[
			.. SkippedItemCategories.ALL.Skip(1).Select(category => BuildEmptyTallyJson(SkippedItemCategoryNames.NameOf(category))),
			"{\"category\":\"exfiltratedSecrets\",\"count\":0,\"enumeratedPaths\":[]}"
		];

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(scanSkippedItemsJson: $"[{string.Join(",", entries)}]"));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SkippedItemTallyNotWellFormed, load.RejectionReason);
	}

	[Fact]
	public void ScanSkippedItemsCarryingANegativeCountStartsAFreshSession()
	{
		string tallies = BuildTalliesJsonWithOneOverride(SkippedItemCategory.UnreadableFile, "\"count\":-1,\"enumeratedPaths\":[]");

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(scanSkippedItemsJson: tallies));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SkippedItemTallyNotWellFormed, load.RejectionReason);
	}

	[Fact]
	public void ScanSkippedItemsCarryingMoreEnumeratedPathsThanItsOwnCountStartsAFreshSession()
	{
		string tallies = BuildTalliesJsonWithOneOverride(
			SkippedItemCategory.UnreadableFile,
			$"\"count\":0,\"enumeratedPaths\":[\"{_VALID_ENUMERATED_PATH}\"]");

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(scanSkippedItemsJson: tallies));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SkippedItemTallyNotWellFormed, load.RejectionReason);
	}

	[Theory]
	[InlineData("\"../outside/secrets.cs\"")]
	[InlineData("\"nested/../../outside.cs\"")]
	public void ScanSkippedItemsCarryingAnEnumeratedPathThatEscapesTheRepositoryStartsAFreshSession(string escapingPath)
	{
		string tallies = BuildTalliesJsonWithOneOverride(
			SkippedItemCategory.UnreadableFile,
			$"\"count\":1,\"enumeratedPaths\":[{escapingPath}]");

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(scanSkippedItemsJson: tallies));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SkippedItemTallyNotWellFormed, load.RejectionReason);
	}

	[Theory]
	[InlineData(
		"\"totalRecognizedCount\":-1,\"retainedCount\":0,\"perFileCapTrippedFileCount\":0,\"perFileOmittedCount\":0,\"globalCapTripped\":false,\"globalOmittedCount\":0",
		"a negative count")]
	[InlineData(
		"\"totalRecognizedCount\":1,\"retainedCount\":1,\"perFileCapTrippedFileCount\":0,\"perFileOmittedCount\":0,\"globalCapTripped\":false,\"globalOmittedCount\":0",
		"a retained count that disagrees with the persisted finding set's own length")]
	[InlineData(
		"\"totalRecognizedCount\":5,\"retainedCount\":0,\"perFileCapTrippedFileCount\":0,\"perFileOmittedCount\":0,\"globalCapTripped\":false,\"globalOmittedCount\":0",
		"a total that does not sum from the retained and omitted counts")]
	[InlineData(
		"\"totalRecognizedCount\":0,\"retainedCount\":0,\"perFileCapTrippedFileCount\":0,\"perFileOmittedCount\":0,\"globalCapTripped\":true,\"globalOmittedCount\":0",
		"a tripped flag that disagrees with a zero global omission")]
	[InlineData(
		"\"totalRecognizedCount\":2,\"retainedCount\":0,\"perFileCapTrippedFileCount\":0,\"perFileOmittedCount\":2,\"globalCapTripped\":false,\"globalOmittedCount\":0",
		"a per-file omission with no file recorded as having tripped the per-file cap")]
	public void AMalformedFindingsCapOutcomeStartsAFreshSession(string findingsCapOutcomeBody, string reason)
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(findingsCapOutcomeJson: $"{{{findingsCapOutcomeBody}}}"));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingsCapOutcomeNotWellFormed, load.RejectionReason);
		Assert.NotEmpty(reason);
	}

	private static RepositoryRelativePath PathOf(RepositoryFolderPath repository, string relativePath)
	{
		if (!RepositoryRelativePath.TryFromScannedPath(repository, relativePath, out RepositoryRelativePath path))
		{
			throw new InvalidOperationException($"The fixture could not build a relative path from '{relativePath}'.");
		}

		return path;
	}

	private static SessionStateLoad LoadWrittenDocument(string documentText)
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		string statePath = destination.Combine(SessionStateStore.BuildStateFileName(repository));
		string documentWithLabel = documentText.Replace(
			_REPOSITORY_LABEL_PLACEHOLDER,
			repository.Label,
			StringComparison.Ordinal);

		File.WriteAllText(statePath, documentWithLabel);

		return new SessionStateStore(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path))
			.Load(repository, ExemplarFixtureCatalog.Build());
	}

	private static string BuildDocumentText(
		string? scanSummaryJson = null,
		string? scanSkippedItemsJson = null,
		string? analysisSkippedItemsJson = null,
		string? findingsCapOutcomeJson = null,
		string? scanEnabledLanguagesJson = null,
		string completedStagesJson = "",
		string? defaultFlowJson = null,
		string? defaultFlowOriginJson = null,
		string? flowEvaluationDiagnosticsJson = null,
		string candidateLanguagesJson = "",
		string candidatePathsJson = "")
	{
		CatalogVersion version = LanguageCatalog.CurrentVersion;

		return $$"""
			{
				"schemaVersion": {{SessionStateStore.CURRENT_SCHEMA_VERSION}},
				"catalogVersionMajor": {{version.Major}},
				"catalogVersionMinor": {{version.Minor}},
				"catalogVersionRevision": {{version.Revision}},
				"repositoryRootLabel": "{{_REPOSITORY_LABEL_PLACEHOLDER}}",
				"completedStages": [{{completedStagesJson}}],
				"candidateRelativePaths": [{{candidatePathsJson}}],
				"findings": [],
				"scanSummary": {{scanSummaryJson ?? "null"}},
				"scanSkippedItems": {{scanSkippedItemsJson ?? "null"}},
				"analysisSkippedItems": {{analysisSkippedItemsJson ?? "null"}},
				"findingsCapOutcome": {{findingsCapOutcomeJson ?? "null"}},
				"scanEnabledLanguages": {{scanEnabledLanguagesJson ?? "null"}},
				"candidateLanguages": [{{candidateLanguagesJson}}],
				"defaultFlow": {{defaultFlowJson ?? "null"}},
				"defaultFlowOrigin": {{defaultFlowOriginJson ?? "null"}},
				"flowEvaluationDiagnostics": {{flowEvaluationDiagnosticsJson ?? "null"}}
			}
			""";
	}

	private static string BuildScanSummaryJson(string languageName, int fileCount, int candidateFileCount = 0)
	{
		return string.Create(
			CultureInfo.InvariantCulture,
			$$"""{"fileCount":{{fileCount}},"lineCount":0,"candidateFileCount":{{candidateFileCount}},"rows":[{"languageName":"{{languageName}}","fileCount":{{fileCount}},"lineCount":0,"candidateFileCount":{{candidateFileCount}}}]}""");
	}

	private static string BuildEmptyTallyJson(string categoryName)
	{
		return $$"""{"category":"{{categoryName}}","count":0,"enumeratedPaths":[]}""";
	}

	/// <summary>All eleven categories well formed and empty, except one whose body is overridden.</summary>
	private static string BuildTalliesJsonWithOneOverride(SkippedItemCategory overriddenCategory, string overriddenBody)
	{
		string[] entries =
		[
			.. SkippedItemCategories.ALL.Select(category => category == overriddenCategory
				? $$"""{"category":"{{SkippedItemCategoryNames.NameOf(category)}}",{{overriddenBody}}}"""
				: BuildEmptyTallyJson(SkippedItemCategoryNames.NameOf(category)))
		];

		return $"[{string.Join(",", entries)}]";
	}
}
