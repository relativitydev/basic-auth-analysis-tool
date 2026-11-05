using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// WHY A LOW FINDING IS LOW, AND THE FOLDERS THE WALK DID NOT ENTER, READ BACK AS HOSTILE INPUT.
///
/// Both travel in the state file so that a report produced after a resume says what the one produced
/// straight after the scan would have. The first test is the round trip through the real writer and
/// reader; every other test edits one value of an otherwise well-formed file, so what fails is the
/// check named in the test's own name.
/// </summary>
public sealed class PersistedLowReasonAndExcludedFolderTests
{
	private const string _LOW_FILE = "src/Low.cs";
	private const string _EXCLUDED_FIRST = "bin";
	private const string _EXCLUDED_SECOND = "src/obj";
	private const string _REASON_VALUE = "commentOrStringLiteral";
	private const string _LEGACY_ENCODED_FILE = "src/Legacy.cs";
	private const string _LEGACY_COUNT_FIELD = "\"legacyEncodedFileCount\":1";
	private const string _LEGACY_COUNT_ZERO = "\"legacyEncodedFileCount\":0";
	private const string _LEGACY_COUNT_ABOVE_THE_FILE_COUNT = "\"legacyEncodedFileCount\":2";
	private const string _LEGACY_COUNT_NEGATIVE = "\"legacyEncodedFileCount\":-1";
	private const string _EXCLUDED_COUNT_FIELD = "\"excludedFolderCount\":2";
	private const string _EXCLUDED_COUNT_TOO_SMALL = "\"excludedFolderCount\":1";
	private const string _EXCLUDED_COUNT_NEGATIVE = "\"excludedFolderCount\":-1";
	private const string _CONFIDENCE_LOW = "\"confidenceLevel\":\"Low\"";
	private const string _CONFIDENCE_HIGH = "\"confidenceLevel\":\"High\"";
	private const string _UNKNOWN_REASON_VALUE = "somethingTheEngineNeverWrote";
	private const string _RETIRED_REASON_VALUE = "demotedFile";
	private const string _SCHEMA_VERSION_NINE_FIELD = "\"schemaVersion\":9";
	private const string _ESCAPING_EXCLUDED_PATH = "../outside";
	private const string _CATALOG_MAJOR_OF_ANOTHER_CATALOG = "\"catalogVersionMajor\":9999";

	[Fact]
	public void TheReasonAndTheExcludedFoldersSurviveASaveAndLoad()
	{
		using SavedSession saved = SavedSession.Create();

		SessionStateLoad reloaded = saved.Load();

		Assert.True(reloaded.WasResumed);
		Assert.Equal(LowConfidenceReasons.CommentOrStringLiteral, Assert.Single(reloaded.State.Findings).LowReasons);
		Assert.Equal(2, reloaded.State.ScanOutcome.ExcludedFolders.OccurrenceCount);
		Assert.Equal(
			[_EXCLUDED_FIRST, _EXCLUDED_SECOND],
			reloaded.State.ScanOutcome.ExcludedFolders.Paths.Select(path => path.Value));
	}

	[Fact]
	public void TheWindows1252FilesSurviveASaveAndLoad()
	{
		using SavedSession saved = SavedSession.Create();

		SessionStateLoad reloaded = saved.Load();

		Assert.True(reloaded.WasResumed);
		Assert.Equal(1, reloaded.State.ScanOutcome.LegacyEncodedFiles.OccurrenceCount);
		Assert.Equal([_LEGACY_ENCODED_FILE], reloaded.State.ScanOutcome.LegacyEncodedFiles.Paths.Select(path => path.Value));
	}

	[Fact]
	public void AWindows1252CountAboveTheFileCountRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(_LEGACY_COUNT_FIELD, _LEGACY_COUNT_ABOVE_THE_FILE_COUNT);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void MoreListedWindows1252FilesThanTheCountRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(_LEGACY_COUNT_FIELD, _LEGACY_COUNT_ZERO);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void ANegativeWindows1252CountRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(_LEGACY_COUNT_FIELD, _LEGACY_COUNT_NEGATIVE);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void AWindows1252PathThatEscapesTheRepositoryRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit($"\"{_LEGACY_ENCODED_FILE}\"", $"\"{_ESCAPING_EXCLUDED_PATH}\"");

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void TheFileIsWrittenAtSchemaVersionNine()
	{
		using SavedSession saved = SavedSession.Create();

		Assert.Equal(9, SessionStateStore.CURRENT_SCHEMA_VERSION);
		Assert.Contains("\"schemaVersion\":9", saved.Text(), StringComparison.Ordinal);
	}

	[Fact]
	public void AFileOfTheEarlierSchemaStartsAFreshSession()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit("\"schemaVersion\":9", "\"schemaVersion\":8");

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SchemaVersionMismatch, reloaded.RejectionReason);
	}

	/// <summary>
	/// A session written at schema 8 and under an earlier catalog is refused for its schema and not for its
	/// catalog: the schema is looked at first, so the reason shown is the one that tells the user the file is of another shape.
	/// </summary>
	[Fact]
	public void ASchemaEightFileUnderAnotherCatalogIsRefusedBySchemaBeforeTheCatalogIsChecked()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit("\"schemaVersion\":9", "\"schemaVersion\":8");
		saved.Edit(saved.CatalogMajorField(), _CATALOG_MAJOR_OF_ANOTHER_CATALOG);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SchemaVersionMismatch, reloaded.RejectionReason);
	}

	[Fact]
	public void ASchemaNineFileUnderAnotherCatalogIsStillRefusedByTheCatalog()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(saved.CatalogMajorField(), _CATALOG_MAJOR_OF_ANOTHER_CATALOG);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CatalogVersionMismatch, reloaded.RejectionReason);
	}

	[Theory]
	[InlineData(LowConfidenceReasons.DemotedMockFile, "demotedMockFile")]
	[InlineData(LowConfidenceReasons.DemotedSampleFile, "demotedSampleFile")]
	[InlineData(LowConfidenceReasons.DemotedGeneratedFile, "demotedGeneratedFile")]
	[InlineData(LowConfidenceReasons.DemotedDeploymentFolder, "demotedDeploymentFolder")]
	[InlineData(LowConfidenceReasons.DemotedVendoredFile, "demotedVendoredFile")]
	[InlineData(LowConfidenceReasons.NeutralOperandPair, "neutralOperandPair")]
	[InlineData(LowConfidenceReasons.InactivePreprocessorRegion, "inactivePreprocessorRegion")]
	[InlineData(LowConfidenceReasons.NeutralOperandPair | LowConfidenceReasons.InactivePreprocessorRegion | LowConfidenceReasons.PatternClass, "neutralOperandPair")]
	public void EachDemotionReasonSurvivesASaveAndLoadUnderItsOwnValue(LowConfidenceReasons reason, string value)
	{
		using SavedSession saved = SavedSession.Create(reason);

		SessionStateLoad reloaded = saved.Load();

		Assert.Contains($"\"{value}\"", saved.Text(), StringComparison.Ordinal);
		Assert.True(reloaded.WasResumed);
		Assert.Equal(reason, Assert.Single(reloaded.State.Findings).LowReasons);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void TheTestCodeMarkerSurvivesASaveAndLoadAtSchemaNine(bool testCode)
	{
		using SavedSession saved = SavedSession.Create(testCode: testCode);

		SessionStateLoad reloaded = saved.Load();

		Assert.Contains($"\"testCode\":{(testCode ? "true" : "false")}", saved.Text(), StringComparison.Ordinal);
		Assert.True(reloaded.WasResumed);
		Assert.Equal(testCode, Assert.Single(reloaded.State.Findings).TestCode);
	}

	[Fact]
	public void AFindingWithoutTheTestCodeMarkerRefusesTheDocumentUnderSchemaNine()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit("\"testCode\":false,", string.Empty);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void ANullTestCodeMarkerRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit("\"testCode\":false", "\"testCode\":null");

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, reloaded.RejectionReason);
	}

	/// <summary>A value outside the closed vocabulary is refused at schema 9, and the file is not read.</summary>
	[Fact]
	public void TheRetiredDemotedFileValueRefusesTheDocumentAndTheSchemaStaysNine()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(_REASON_VALUE, _RETIRED_REASON_VALUE);

		SessionStateLoad reloaded = saved.Load();

		Assert.Contains(_SCHEMA_VERSION_NINE_FIELD, saved.Text(), StringComparison.Ordinal);
		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void AReasonOutsideTheClosedVocabularyRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(_REASON_VALUE, _UNKNOWN_REASON_VALUE);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void AReasonBesideAFindingThatIsNotLowRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(_CONFIDENCE_LOW, _CONFIDENCE_HIGH);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FindingNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void MoreListedFoldersThanTheCountRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(_EXCLUDED_COUNT_FIELD, _EXCLUDED_COUNT_TOO_SMALL);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void ANegativeExcludedCountRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit(_EXCLUDED_COUNT_FIELD, _EXCLUDED_COUNT_NEGATIVE);

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, reloaded.RejectionReason);
	}

	[Fact]
	public void AnExcludedFolderPathThatEscapesTheRepositoryRefusesTheDocument()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit($"\"{_EXCLUDED_FIRST}\"", $"\"{_ESCAPING_EXCLUDED_PATH}\"");

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.ScanSummaryNotWellFormed, reloaded.RejectionReason);
	}

	/// <summary>A session saved to a temporary folder, ready to be read back or edited first.</summary>
	private sealed class SavedSession : IDisposable
	{
		private readonly TemporaryFolder _destination;
		private readonly TemporaryFolder _repositoryFolder;
		private readonly RepositoryFolderPath _repository;
		private readonly LanguageCatalog _catalog;

		private SavedSession(TemporaryFolder destination, TemporaryFolder repositoryFolder)
		{
			_destination = destination;
			_repositoryFolder = repositoryFolder;
			_repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
			_catalog = ExemplarFixtureCatalog.Build();
		}

		private string StatePath => _destination.Combine(SessionStateStore.BuildStateFileName(_repository));

		internal static SavedSession Create(
			LowConfidenceReasons reasons = LowConfidenceReasons.CommentOrStringLiteral,
			bool testCode = false)
		{
			SavedSession saved = new(TemporaryFolder.Create(), TemporaryFolder.Create());

			saved.Save(reasons, testCode);

			return saved;
		}

		/// <summary>The catalog major version as the file writes it, so that a test can name another one.</summary>
		internal string CatalogMajorField()
		{
			return $"\"catalogVersionMajor\":{_catalog.Version.Major}";
		}

		internal string Text()
		{
			return File.ReadAllText(StatePath);
		}

		internal void Edit(string oldText, string newText)
		{
			string text = Text();

			Assert.Contains(oldText, text, StringComparison.Ordinal);
			File.WriteAllText(StatePath, text.Replace(oldText, newText, StringComparison.Ordinal));
		}

		internal SessionStateLoad Load()
		{
			return new SessionStateStore(DocumentationDestinationFolderPath.FromSuppliedText(_destination.Path))
				.Load(_repository, _catalog);
		}

		public void Dispose()
		{
			_destination.Dispose();
			_repositoryFolder.Dispose();
		}

		private void Save(LowConfidenceReasons reasons, bool testCode)
		{
			SessionState state = SessionState.Fresh(_catalog.Version, _repository.Label);
			Finding lowFinding = ReportHarness.FindingAt(
				1,
				_LOW_FILE,
				3,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.Low,
				lowReasons: reasons,
				testCode: testCode);

			state.RecordScanOutcome(
				[],
				[],
				new ScanSummary
				{
					FileCount = 1,
					LineCount = 4,
					CandidateFileCount = 0,
					Rows =
					[
						new LanguageInventoryRow
						{
							LanguageName = AssessedLanguages.CanonicalName(AssessedLanguage.CSharp),
							FileCount = 1,
							LineCount = 4,
							CandidateFileCount = 0,
							ShareOfFilesPercentage = 100.0
						}
					],
					ExcludedFolders = new ExcludedFolderSummary
					{
						OccurrenceCount = 2,
						Paths = [PathOf(_EXCLUDED_FIRST), PathOf(_EXCLUDED_SECOND)]
					},
					LegacyEncodedFiles = new LegacyEncodedFileSummary
					{
						OccurrenceCount = 1,
						Paths = [PathOf(_LEGACY_ENCODED_FILE)]
					}
				},
				SkippedItemTallies.Empty,
				[AssessedLanguage.CSharp]);
			state.MarkCompleted(CommandId.Scan);
			state.RecordAnalysisOutcome(
				[lowFinding],
				new FindingsCapOutcome
				{
					TotalRecognizedCount = 1,
					RetainedCount = 1,
					PerFileCapTrippedFileCount = 0,
					PerFileOmittedCount = 0,
					GlobalCapTripped = false,
					GlobalOmittedCount = 0
				},
				SkippedItemTallies.Empty);
			state.MarkCompleted(CommandId.Analyze);

			new SessionStateStore(DocumentationDestinationFolderPath.FromSuppliedText(_destination.Path)).Save(_repository, state);
		}

		private RepositoryRelativePath PathOf(string relativePath)
		{
			string absolutePath = Path.Combine(_repositoryFolder.Path, relativePath);

			if (!RepositoryRelativePath.TryFromScannedPath(_repository, absolutePath, out RepositoryRelativePath path))
			{
				throw new InvalidOperationException(
					$"The fixture could not build a relative path: '{absolutePath}' is not inside the repository root '{_repository.CanonicalPath}'.");
			}

			return path;
		}
	}
}
