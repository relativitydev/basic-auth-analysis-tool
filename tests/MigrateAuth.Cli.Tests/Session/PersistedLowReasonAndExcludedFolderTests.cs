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
	private const string _EXCLUDED_COUNT_FIELD = "\"excludedFolderCount\":2";
	private const string _EXCLUDED_COUNT_TOO_SMALL = "\"excludedFolderCount\":1";
	private const string _EXCLUDED_COUNT_NEGATIVE = "\"excludedFolderCount\":-1";
	private const string _CONFIDENCE_LOW = "\"confidenceLevel\":\"Low\"";
	private const string _CONFIDENCE_HIGH = "\"confidenceLevel\":\"High\"";
	private const string _UNKNOWN_REASON_VALUE = "somethingTheEngineNeverWrote";
	private const string _ESCAPING_EXCLUDED_PATH = "../outside";

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
	public void TheFileIsWrittenAtSchemaVersionSeven()
	{
		using SavedSession saved = SavedSession.Create();

		Assert.Equal(7, SessionStateStore.CURRENT_SCHEMA_VERSION);
		Assert.Contains("\"schemaVersion\":7", saved.Text(), StringComparison.Ordinal);
	}

	[Fact]
	public void AFileOfTheEarlierSchemaStartsAFreshSession()
	{
		using SavedSession saved = SavedSession.Create();
		saved.Edit("\"schemaVersion\":7", "\"schemaVersion\":6");

		SessionStateLoad reloaded = saved.Load();

		Assert.False(reloaded.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SchemaVersionMismatch, reloaded.RejectionReason);
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

		internal static SavedSession Create()
		{
			SavedSession saved = new(TemporaryFolder.Create(), TemporaryFolder.Create());

			saved.Save();

			return saved;
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

		private void Save()
		{
			SessionState state = SessionState.Fresh(_catalog.Version, _repository.Label);
			Finding lowFinding = ReportHarness.FindingAt(
				1,
				_LOW_FILE,
				3,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.Low,
				lowReasons: LowConfidenceReasons.CommentOrStringLiteral);

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
