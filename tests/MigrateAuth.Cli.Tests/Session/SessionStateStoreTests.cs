using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// The state file is written atomically with restrictive
/// permissions, named by a keyed hash of the repository path, carries the root as a label
/// rather than as a path, and is read back as untrusted input where any anomaly starts a fresh
/// session instead of failing.
///
/// The hostile cases matter more than the happy one. The missing-file rule covers a missing or unreadable
/// file; the readable-and-hostile file is what would otherwise let someone inject fabricated
/// state into a client's report.
/// </summary>
public sealed class SessionStateStoreTests
{
	private const string _STATE_FILE_SEARCH_PATTERN = "migrate-auth.session.*";
	private const string _CANDIDATE_LANGUAGE_NAME = "CSharp";

	/// <summary>Headroom above the small file that is not enough for the large one, in bytes.</summary>
	private const int _CEILING_MARGIN_IN_BYTES = 2_048;

	private const int _MANY_CANDIDATES = 500;

	/// <summary>An oversize description that is not true, which the store must no longer emit.</summary>
	private const string _RETIRED_OVERSIZE_TEXT = "larger than any session this tool writes";
	/// <summary>
	/// The schema version is a placeholder rather than a literal, so that these fixtures follow
	/// <see cref="SessionStateStore.CURRENT_SCHEMA_VERSION"/> when a later version adds a section. A
	/// literal here would make every hostile-document test start failing with
	/// <see cref="SessionStateRejectionReason.SchemaVersionMismatch"/> on the version bump, hiding
	/// the check each one is actually about behind a version mismatch none of them are testing.
	/// </summary>
	/// <summary>
	/// The fields at the bottom are here so that a fixture
	/// naming <c>scan</c> or <c>analyze</c> complete stays resumable - the read path refuses a
	/// completed stage whose own section is absent - while every fixture that names neither still
	/// gets a document these zero-valued, well-formed sections cannot reject on their own account.
	/// <c>scanEnabledLanguages</c>'s own empty array is well formed regardless of whether <c>scan</c>
	/// is named complete, for the same reason - see <see cref="SessionStateDocumentSections.EMPTY_SCAN_ENABLED_LANGUAGES_JSON"/>.
	/// </summary>
	private const string _WELL_FORMED_STATE_DOCUMENT_FORMAT = """
		{{
			"schemaVersion": {6},
			"catalogVersionMajor": {0},
			"catalogVersionMinor": {1},
			"catalogVersionRevision": {2},
			"repositoryRootLabel": "{3}",
			"completedStages": [{4}],
			"candidateRelativePaths": [{5}],
			"findings": [{7}],
			"scanSummary": {8},
			"scanSkippedItems": {9},
			"analysisSkippedItems": {9},
			"findingsCapOutcome": {10},
			"scanEnabledLanguages": [],
			"candidateLanguages": [{11}],
			"defaultFlow": {12},
			"defaultFlowOrigin": {13},
			"flowEvaluationDiagnostics": {14}
		}}
		""";

	[Fact]
	public void MissingStateFileStartsAFreshSession()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		SessionStateLoad load = BuildStore(destination).Load(BuildRepository(repositoryFolder), BuildCatalog());

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FileAbsent, load.RejectionReason);
		Assert.Empty(load.State.CompletedStagesInRunOrder());
	}

	[Fact]
	public void SavedStageCompletionSurvivesAReload()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);
		SessionStateStore store = BuildStore(destination);
		SessionState state = SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label);

		state.MarkCompleted(CommandId.Scan);
		state.MarkCompleted(CommandId.Analyze);
		store.Save(repository, state);

		SessionStateLoad load = store.Load(repository, BuildCatalog());

		Assert.True(load.WasResumed);
		Assert.Equal([CommandId.Scan, CommandId.Analyze], load.State.CompletedStagesInRunOrder().Select(stage => stage.Id));
	}

	[Fact]
	public void ResumedSessionEnforcesGatingAgainstTheStagesItRetained()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);
		SessionStateStore store = BuildStore(destination);
		SessionState state = SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label);

		state.MarkCompleted(CommandId.Scan);
		store.Save(repository, state);

		SessionState resumed = store.Load(repository, BuildCatalog()).State;

		Assert.False(resumed.TryFindUnmetPredecessor(CommandId.Analyze, out _));
		Assert.True(resumed.TryFindUnmetPredecessor(CommandId.Suggest, out _));
	}

	[Fact]
	public void WritingTheStateLeavesNoTemporaryFileBehind()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);

		BuildStore(destination).Save(repository, SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label));

		string[] writtenFiles = Directory.GetFiles(destination.Path);

		Assert.Single(writtenFiles);
		Assert.EndsWith(SessionStateStore.STATE_FILE_NAME_SUFFIX, writtenFiles[0], StringComparison.Ordinal);
	}

	[Fact]
	public void StateFileNameIsTheSameForTheSameRepositoryOnEveryRun()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string firstName = SessionStateStore.BuildStateFileName(BuildRepository(repositoryFolder));
		string secondName = SessionStateStore.BuildStateFileName(BuildRepository(repositoryFolder));

		Assert.Equal(firstName, secondName);
	}

	[Fact]
	public void StateFileNameDiffersBetweenRepositories()
	{
		using TemporaryFolder firstRepository = TemporaryFolder.Create();
		using TemporaryFolder secondRepository = TemporaryFolder.Create();

		string firstName = SessionStateStore.BuildStateFileName(BuildRepository(firstRepository));
		string secondName = SessionStateStore.BuildStateFileName(BuildRepository(secondRepository));

		Assert.NotEqual(firstName, secondName);
	}

	[Fact]
	public void StateFileNameIgnoresATrailingSeparatorAndTheCaseOfThePath()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		string plainName = SessionStateStore.BuildStateFileName(BuildRepository(repositoryFolder));
		string trailingSeparatorName = SessionStateStore.BuildStateFileName(
			RepositoryFolderPath.FromSuppliedText($"{repositoryFolder.Path}{Path.DirectorySeparatorChar}"));
		string upperCaseName = SessionStateStore.BuildStateFileName(
			RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path.ToUpperInvariant()));

		Assert.Equal(plainName, trailingSeparatorName);
		Assert.Equal(plainName, upperCaseName);
	}

	/// <summary>
	/// The name carries a hash of the path and never the path. A folder name appearing in it
	/// would put a fragment of the client's directory layout into a file name.
	/// </summary>
	[Fact]
	public void StateFileNameCarriesNoFragmentOfTheRepositoryPath()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);
		string stateFileName = SessionStateStore.BuildStateFileName(repository);

		Assert.DoesNotContain(repository.Label, stateFileName, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void SavedStateRecordsTheRepositoryRootAsALabelAndNotAsAPath()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);

		BuildStore(destination).Save(repository, SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label));

		string documentText = File.ReadAllText(Directory.GetFiles(destination.Path, _STATE_FILE_SEARCH_PATTERN)[0]);

		Assert.Contains(repository.Label, documentText, StringComparison.Ordinal);
		Assert.DoesNotContain(repository.CanonicalPath, documentText, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void StateFileIsCreatedWithRestrictiveUserPermissionsOnUnix()
	{
		if (OperatingSystem.IsWindows())
		{
			return;
		}

		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);

		BuildStore(destination).Save(repository, SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label));

		UnixFileMode fileMode = File.GetUnixFileMode(Directory.GetFiles(destination.Path, _STATE_FILE_SEARCH_PATTERN)[0]);

		Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, fileMode);
	}

	[Theory]
	[InlineData("not json at all", SessionStateRejectionReason.DocumentMalformed)]
	[InlineData("{ \"schemaVersion\": 99 }", SessionStateRejectionReason.SchemaVersionMismatch)]
	public void MalformedOrForeignDocumentStartsAFreshSession(string documentText, SessionStateRejectionReason expectedReason)
	{
		SessionStateLoad load = LoadWrittenDocument(documentText);

		Assert.False(load.WasResumed);
		Assert.Equal(expectedReason, load.RejectionReason);
	}

	/// <summary>
	/// A document at the version this build writes gets past the schema check and is rejected on
	/// the next one, which is what shows the schema check is a gate rather than the only check.
	/// </summary>
	[Fact]
	public void DocumentAtTheCurrentSchemaVersionIsJudgedOnItsContents()
	{
		SessionStateLoad load = LoadWrittenDocument(
			$"{{ \"schemaVersion\": {SessionStateStore.CURRENT_SCHEMA_VERSION} }}");

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CatalogVersionMismatch, load.RejectionReason);
	}

	/// <summary>
	/// A document written by an older schema without persisted findings must start a fresh session
	/// rather than read as a current document with no findings. The two are different claims - an
	/// analysis that produced nothing versus an analysis whose results this build cannot see - and a
	/// report must never present the second as the first.
	/// </summary>
	[Fact]
	public void DocumentAtThePreviousSchemaVersionStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStageNames: "\"scan\"",
			schemaVersion: SessionStateStore.CURRENT_SCHEMA_VERSION - 1));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SchemaVersionMismatch, load.RejectionReason);
		Assert.Empty(load.State.CompletedStagesInRunOrder());
	}

	/// <summary>Another catalog version is another session.</summary>
	[Fact]
	public void DocumentWrittenUnderAnotherCatalogVersionStartsAFreshSession()
	{
		CatalogVersion otherVersion = new(
			LanguageCatalog.CurrentVersion.Major,
			LanguageCatalog.CurrentVersion.Minor,
			LanguageCatalog.CurrentVersion.Revision + 1);

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(otherVersion, completedStageNames: "\"scan\""));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CatalogVersionMismatch, load.RejectionReason);
		Assert.Empty(load.State.CompletedStagesInRunOrder());
	}

	/// <summary>
	/// Over an actual version boundary rather than a synthetic offset: a session persisted under
	/// catalog version 1.0.0 starts fresh once the catalog carries
	/// <see cref="LanguageCatalog.CurrentVersion"/>.
	/// </summary>
	[Fact]
	public void DocumentPersistedUnderTheBaseVersionOneZeroZeroStartsAFreshSessionUnderTheBumpedVersion()
	{
		CatalogVersion previousVersion = new(1, 0, 0);

		Assert.NotEqual(previousVersion, LanguageCatalog.CurrentVersion);

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(previousVersion, completedStageNames: "\"scan\""));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CatalogVersionMismatch, load.RejectionReason);
		Assert.Empty(load.State.CompletedStagesInRunOrder());
	}

	[Fact]
	public void DocumentCarryingAMemberTheSchemaDoesNotDeclareStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument($$"""
			{
				"schemaVersion": {{SessionStateStore.CURRENT_SCHEMA_VERSION}},
				"fabricatedFindings": ["anything at all"]
			}
			""");

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.DocumentMalformed, load.RejectionReason);
	}

	[Fact]
	public void DocumentNamingAStageThisBuildDoesNotKnowStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(completedStageNames: "\"scan\", \"exfiltrate\""));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CompletedStageUnknown, load.RejectionReason);
	}

	[Fact]
	public void DocumentNamingASessionCommandAsACompletedStageStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(completedStageNames: "\"help\""));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CompletedStageUnknown, load.RejectionReason);
	}

	[Fact]
	public void DocumentRepeatingAStageStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(completedStageNames: "\"scan\", \"scan\""));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CompletedStageUnknown, load.RejectionReason);
	}

	/// <summary>
	/// A completed <c>report</c> with no completed <c>scan</c> is not something a session can
	/// produce, so it is not something the tool will read back.
	/// </summary>
	[Fact]
	public void DocumentWhoseCompletedStagesSkipTheChainStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(completedStageNames: "\"report\""));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CompletedStagesNotPrefixClosed, load.RejectionReason);
	}

	[Theory]
	[InlineData("\"../outside/secrets.cs\"")]
	[InlineData("\"nested/../../outside.cs\"")]
	[InlineData("\"\"")]
	public void DocumentCarryingACandidatePathThatEscapesTheRepositoryStartsAFreshSession(string candidatePaths)
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(candidateRelativePaths: candidatePaths));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.CandidatePathNotContained, load.RejectionReason);
	}

	/// <summary>
	/// The scan summary's own candidate count must equal the persisted
	/// candidate path list's length, so this fixture's summary carries one candidate rather than
	/// the all-zero default - the two are written from the same scan and a document where they
	/// disagree is rejected by <see cref="SessionStateRejectionReason.ScanSummaryNotWellFormed"/>.
	/// </summary>
	[Fact]
	public void DocumentCarryingAContainedCandidatePathIsAccepted()
	{
		const string oneCandidateScanSummaryJson =
			"""{"fileCount":1,"lineCount":0,"candidateFileCount":1,"rows":[{"languageName":"CSharp","fileCount":1,"lineCount":0,"candidateFileCount":1}]}""";

		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(
			completedStageNames: "\"scan\"",
			candidateRelativePaths: "\"src/Login.cs\"",
			scanSummaryJson: oneCandidateScanSummaryJson,
			candidateLanguages: "\"CSharp\""));

		Assert.True(load.WasResumed);
		Assert.Equal(["src/Login.cs"], load.State.CandidateRelativePaths);
	}

	/// <summary>
	/// A moved or renamed repository starts fresh. The tool cannot distinguish a moved checkout
	/// from a different one, and the label disagreeing with the name's hash says one of the two
	/// happened.
	/// </summary>
	[Fact]
	public void DocumentNamingAnotherCheckoutStartsAFreshSession()
	{
		SessionStateLoad load = LoadWrittenDocument(BuildDocumentText(repositoryRootLabel: "some-other-checkout"));

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.RepositoryLabelMismatch, load.RejectionReason);
	}

	/// <summary>
	/// A state file written by the previous schema is rejected through the schema
	/// mismatch path, the discard message shown at startup states the reason, and the session that
	/// starts is a fresh one with nothing completed.
	/// </summary>
	[Fact]
	public void ASchemaFourStateFileIsRejectedItsReasonIsShownAndTheSessionStartsFresh()
	{
		const int previousSchemaVersion = 4;

		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);
		string schemaFourDocument = BuildDocumentText(completedStageNames: "\"scan\"", schemaVersion: previousSchemaVersion)
			.Replace(_REPOSITORY_LABEL_PLACEHOLDER, repository.Label, StringComparison.Ordinal);

		File.WriteAllText(destination.Combine(SessionStateStore.BuildStateFileName(repository)), schemaFourDocument);

		SessionStateLoad load = BuildStore(destination).Load(repository, BuildCatalog());
		StringWriter console = new();

		SessionStartup.ReportSessionStart(load, repository, console);

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.SchemaVersionMismatch, load.RejectionReason);
		Assert.Empty(load.State.CompletedStagesInRunOrder());
		Assert.Contains(
			string.Format(
				System.Globalization.CultureInfo.InvariantCulture,
				SessionStartup.SESSION_STATE_DISCARDED_MESSAGE_FORMAT,
				ReportVocabulary.DescriptionOf(SessionStateRejectionReason.SchemaVersionMismatch)),
			console.ToString(),
			StringComparison.Ordinal);
	}

	/// <summary>The state is written unindented: no line breaks and no run of indentation.</summary>
	[Fact]
	public void TheStateFileIsWrittenUnindented()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);

		BuildStore(destination).Save(repository, BuildStateWithCandidates(repository, candidateCount: 3));

		string writtenText = File.ReadAllText(destination.Combine(SessionStateStore.BuildStateFileName(repository)));

		Assert.DoesNotContain('\n', writtenText);
		Assert.DoesNotContain('\t', writtenText);
		Assert.DoesNotContain("  ", writtenText, StringComparison.Ordinal);
		Assert.StartsWith("{\"schemaVersion\":", writtenText, StringComparison.Ordinal);
	}

	/// <summary>
	/// A save that would pass the ceiling is refused with a message that states the
	/// ceiling, the previous state file is byte for byte what it was, and no temporary file is left
	/// beside it - so a resume restarts from the last stage that saved.
	/// </summary>
	[Fact]
	public void AWriteThatWouldExceedTheCeilingIsRefusedNamingTheCeilingAndLeavesThePreviousFileByteIdentical()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);
		string statePath = destination.Combine(SessionStateStore.BuildStateFileName(repository));
		DocumentationDestinationFolderPath destinationFolder = DocumentationDestinationFolderPath.FromSuppliedText(destination.Path);

		BuildStore(destination).Save(repository, BuildStateWithCandidates(repository, candidateCount: 2));

		byte[] previousBytes = File.ReadAllBytes(statePath);
		long ceilingJustAboveThePreviousFile = previousBytes.Length + _CEILING_MARGIN_IN_BYTES;
		SessionStateStore constrainedStore = new(destinationFolder, ceilingJustAboveThePreviousFile);

		SessionStateTooLargeException refusal = Assert.Throws<SessionStateTooLargeException>(
			() => constrainedStore.Save(repository, BuildStateWithCandidates(repository, candidateCount: _MANY_CANDIDATES)));

		Assert.Contains(SessionStateStore.DescribeCeiling(ceilingJustAboveThePreviousFile), refusal.Message, StringComparison.Ordinal);
		Assert.Equal(previousBytes, File.ReadAllBytes(statePath));
		Assert.Single(Directory.GetFiles(destination.Path));
	}

	/// <summary>The refusal leaves the state that did fit loadable: the last stage that saved is what a resume restarts from.</summary>
	[Fact]
	public void AfterARefusedWriteTheLastSavedStateStillLoads()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);
		SessionState smallState = BuildStateWithCandidates(repository, candidateCount: 2);

		BuildStore(destination).Save(repository, smallState);

		long ceiling = new FileInfo(destination.Combine(SessionStateStore.BuildStateFileName(repository))).Length + _CEILING_MARGIN_IN_BYTES;
		SessionStateStore constrainedStore = new(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path), ceiling);

		Assert.Throws<SessionStateTooLargeException>(
			() => constrainedStore.Save(repository, BuildStateWithCandidates(repository, candidateCount: _MANY_CANDIDATES)));

		SessionStateLoad load = constrainedStore.Load(repository, BuildCatalog());

		Assert.True(load.WasResumed);
		Assert.Equal(smallState.CandidateRelativePaths, load.State.CandidateRelativePaths);
	}

	/// <summary>A file above the ceiling is not read at all, and says why in words that state the ceiling.</summary>
	[Fact]
	public void AFileLargerThanTheCeilingIsNotReadAndItsDescriptionStatesTheCeiling()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);

		BuildStore(destination).Save(repository, BuildStateWithCandidates(repository, candidateCount: _MANY_CANDIDATES));

		SessionStateStore smallCeilingStore = new(
			DocumentationDestinationFolderPath.FromSuppliedText(destination.Path),
			_CEILING_MARGIN_IN_BYTES);
		SessionStateLoad load = smallCeilingStore.Load(repository, BuildCatalog());
		string description = ReportVocabulary.DescriptionOf(SessionStateRejectionReason.FileTooLarge);

		Assert.False(load.WasResumed);
		Assert.Equal(SessionStateRejectionReason.FileTooLarge, load.RejectionReason);
		Assert.Contains(
			SessionStateStore.DescribeCeiling(SessionStateStore.MAXIMUM_STATE_FILE_LENGTH_IN_BYTES),
			description,
			StringComparison.Ordinal);
		Assert.DoesNotContain(_RETIRED_OVERSIZE_TEXT, description, StringComparison.Ordinal);
	}

	private static SessionState BuildStateWithCandidates(RepositoryFolderPath repository, int candidateCount)
	{
		SessionState state = SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label);
		string[] candidatePaths = [.. Enumerable.Range(0, candidateCount).Select(index => $"src/module-{index:D6}/Credentials.cs")];

		state.RecordScanOutcome(
			candidatePaths,
			[.. candidatePaths.Select(_ => _CANDIDATE_LANGUAGE_NAME)],
			BuildScanSummary(candidateCount),
			MigrateAuth.Cli.Reading.SkippedItemTallies.Empty,
			[]);
		state.MarkCompleted(CommandId.Scan);

		return state;
	}

	/// <summary>A scan summary that agrees with a candidate set of the given size, as a completed scan's always does.</summary>
	private static ScanSummary BuildScanSummary(int candidateCount)
	{
		return new ScanSummary
		{
			FileCount = candidateCount,
			LineCount = 0,
			CandidateFileCount = candidateCount,
			Rows =
			[
				new MigrateAuth.Cli.Scanning.LanguageInventoryRow
				{
					LanguageName = _CANDIDATE_LANGUAGE_NAME,
					FileCount = candidateCount,
					LineCount = 0,
					CandidateFileCount = candidateCount,
					ShareOfFilesPercentage = 100.0
				}
			]
		};
	}

	private static SessionStateLoad LoadWrittenDocument(string documentText)
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = BuildRepository(repositoryFolder);
		string statePath = destination.Combine(SessionStateStore.BuildStateFileName(repository));
		string documentWithLabel = documentText.Replace(
			_REPOSITORY_LABEL_PLACEHOLDER,
			repository.Label,
			StringComparison.Ordinal);

		File.WriteAllText(statePath, documentWithLabel);

		return BuildStore(destination).Load(repository, BuildCatalog());
	}

	private static string BuildDocumentText(
		CatalogVersion? catalogVersion = null,
		string repositoryRootLabel = _REPOSITORY_LABEL_PLACEHOLDER,
		string completedStageNames = "",
		string candidateRelativePaths = "",
		string findings = "",
		int? schemaVersion = null,
		string? scanSummaryJson = null,
		string candidateLanguages = "")
	{
		CatalogVersion version = catalogVersion ?? LanguageCatalog.CurrentVersion;

		return string.Format(
			System.Globalization.CultureInfo.InvariantCulture,
			_WELL_FORMED_STATE_DOCUMENT_FORMAT,
			version.Major,
			version.Minor,
			version.Revision,
			repositoryRootLabel,
			completedStageNames,
			candidateRelativePaths,
			schemaVersion ?? SessionStateStore.CURRENT_SCHEMA_VERSION,
			findings,
			scanSummaryJson ?? SessionStateDocumentSections.EMPTY_SCAN_SUMMARY_JSON,
			SessionStateDocumentSections.EmptySkippedItemTalliesJson(),
			SessionStateDocumentSections.UntrippedFindingsCapOutcomeJson(findingsCount: 0),
			candidateLanguages,
			SessionStateDocumentSections.DEFAULT_FLOW_JSON,
			SessionStateDocumentSections.DEFAULT_FLOW_ORIGIN_JSON,
			SessionStateDocumentSections.EMPTY_FLOW_EVALUATION_DIAGNOSTICS_JSON);
	}

	private static SessionStateStore BuildStore(TemporaryFolder destination)
	{
		return new SessionStateStore(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path));
	}

	/// <summary>
	/// <c>Load</c> takes the catalog rather than a bare version, because a resumed scan
	/// summary's language names are checked against what this catalog can declare. The embedded one
	/// carries <see cref="LanguageCatalog.CurrentVersion"/>, exactly what these fixtures' documents
	/// already assert against.
	/// </summary>
	private static LanguageCatalog BuildCatalog()
	{
		return LanguageCatalogRegistry.BuildEmbeddedCatalog();
	}

	private static RepositoryFolderPath BuildRepository(TemporaryFolder repositoryFolder)
	{
		return RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
	}

	private const string _REPOSITORY_LABEL_PLACEHOLDER = "{repository-label}";
}
