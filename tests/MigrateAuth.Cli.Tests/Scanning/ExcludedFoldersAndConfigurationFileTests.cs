using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// THE FOLDERS THE WALK DOES NOT ENTER, MADE VISIBLE; AND THE TOOL'S OWN CONFIGURATION FILE, LEFT OUT
/// OF WHAT IT ASSESSES.
///
/// An excluded folder is a policy of the tool and not a hole it could not avoid, so the first group of
/// tests is as much about what it is NOT - a skipped item - as about the count and the list. No test
/// here depends on which names are excluded beyond two of them: the tests create folders of those names.
/// </summary>
public sealed class ExcludedFoldersAndConfigurationFileTests
{
	private const string _ORDINARY_FILE_NAME = "ordinary.ex1";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";
	private const string _EXCLUDED_AT_ROOT = "node_modules";
	private const string _EXCLUDED_NESTED = "src/obj";
	private const string _EXCLUDED_DEEP = "a/b/obj";
	private const string _EXCLUDED_FILE_NAME = "inside.ex1";
	private const string _PACKAGE_FOLDER_FORMAT = "pkg-{0:D3}/obj";
	private const int _MORE_THAN_THE_CEILING = ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY + 5;
	private const string _CONFIGURATION_FILE_NAME = ConfigurationResolver.CONFIGURATION_FILE_NAME;
	private const string _NESTED_CONFIGURATION_FILE_PATH = "sub/.migrate-auth.json";
	private const string _CONFIGURATION_CONTENT = "{ \"minimumConfidenceLevel\": \"High\" }\n";
	private const string _UNRECOGNIZED_LANGUAGE_NAME = LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME;

	[Fact]
	public void EveryExcludedFolderIsCountedAndListedByItsRelativePathInOrdinalOrder()
	{
		using RepositoryFixture fixture = FixtureWithThreeExcludedFolders();
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);

		_ = walk.Enumerate().ToList();
		ExcludedFolderSummary excluded = walk.ExcludedFolders();

		Assert.Equal(3, excluded.OccurrenceCount);
		Assert.Equal([_EXCLUDED_DEEP, _EXCLUDED_AT_ROOT, _EXCLUDED_NESTED], excluded.Paths.Select(path => path.Value));
		Assert.Equal(0, excluded.OmittedPathCount);
	}

	[Fact]
	public void AnExcludedFolderIsNotASkippedItem()
	{
		using RepositoryFixture fixture = FixtureWithThreeExcludedFolders();
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);

		_ = walk.Enumerate().ToList();

		Assert.Equal(0, fixture.Ledger.SkippedFiles);
		Assert.Equal(0, fixture.Ledger.DirectoriesOfUnknownContents);
	}

	[Fact]
	public void TheListIsBoundedAtTheSkippedItemCeilingAndTheCountStaysWhole()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		for (int index = 0; index < _MORE_THAN_THE_CEILING; index++)
		{
			fixture.CreateFolder(string.Format(System.Globalization.CultureInfo.InvariantCulture, _PACKAGE_FOLDER_FORMAT, index));
		}

		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);

		_ = walk.Enumerate().ToList();
		ExcludedFolderSummary excluded = walk.ExcludedFolders();

		Assert.Equal(_MORE_THAN_THE_CEILING, excluded.OccurrenceCount);
		Assert.Equal(ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY, excluded.Paths.Length);
		Assert.Equal(_MORE_THAN_THE_CEILING - ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY, excluded.OmittedPathCount);
	}

	[Fact]
	public void ASecondEnumerationStartsTheCountAgain()
	{
		using RepositoryFixture fixture = FixtureWithThreeExcludedFolders();
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);

		_ = walk.Enumerate().ToList();
		_ = walk.Enumerate().ToList();

		Assert.Equal(3, walk.ExcludedFolders().OccurrenceCount);
	}

	[Fact]
	public void ARepositoryWithNoExcludedFolderReportsZero()
	{
		using RepositoryFixture fixture = RepositoryFixture.CreateEmpty();
		fixture.WriteText(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);
		RepositoryWalk walk = new(fixture.Repository, fixture.EmissionPoint);

		_ = walk.Enumerate().ToList();

		Assert.Equal(ExcludedFolderSummary.None, walk.ExcludedFolders());
	}

	[Fact]
	public void TheScanStagePutsTheExcludedFoldersOnTheSessionStateAndSaysHowManyOnTheConsole()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText($"{_EXCLUDED_AT_ROOT}/{_EXCLUDED_FILE_NAME}", _ORDINARY_CONTENT);
		harness.Fixture.WriteText($"{_EXCLUDED_NESTED}/{_EXCLUDED_FILE_NAME}", _ORDINARY_CONTENT);

		StageRunOutcome outcome = harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Equal(2, harness.State.ScanOutcome.ExcludedFolders.OccurrenceCount);
		Assert.Equal([_EXCLUDED_AT_ROOT, _EXCLUDED_NESTED], harness.State.ScanOutcome.ExcludedFolders.Paths.Select(path => path.Value));
		Assert.Contains(
			string.Format(System.Globalization.CultureInfo.InvariantCulture, ScanStage.EXCLUDED_FOLDERS_MESSAGE_FORMAT, 2),
			harness.Output.ToString(),
			StringComparison.Ordinal);
		Assert.Equal(1, harness.State.ScanOutcome.FileCount);
	}

	[Fact]
	public void TheScanConsoleSaysNothingAboutExcludedFoldersWhenThereWereNone()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.DoesNotContain("Folders excluded", harness.Output.ToString(), StringComparison.Ordinal);
		Assert.Equal(0, harness.State.ScanOutcome.ExcludedFolders.OccurrenceCount);
	}

	[Fact]
	public void TheRepositorysOwnConfigurationFileAtTheRootIsNotCountedInTheInventory()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_CONFIGURATION_FILE_NAME, _CONFIGURATION_CONTENT);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Equal(1, harness.State.ScanOutcome.FileCount);
		Assert.DoesNotContain(
			harness.State.ScanOutcome.Rows,
			row => row.LanguageName == _UNRECOGNIZED_LANGUAGE_NAME);
	}

	[Fact]
	public void ANestedFileOfThatNameIsAnOrdinaryFileAndIsCounted()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);
		harness.Fixture.WriteText(_CONFIGURATION_FILE_NAME, _CONFIGURATION_CONTENT);
		harness.Fixture.WriteText(_NESTED_CONFIGURATION_FILE_PATH, _CONFIGURATION_CONTENT);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Equal(2, harness.State.ScanOutcome.FileCount);
		Assert.Equal(
			1,
			harness.State.ScanOutcome.Rows.Single(row => row.LanguageName == _UNRECOGNIZED_LANGUAGE_NAME).FileCount);
	}

	[Fact]
	public void TheConfigurationFileIsNeverACandidate()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_CONFIGURATION_FILE_NAME, _CONFIGURATION_CONTENT);

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Empty(harness.State.CandidateRelativePaths);
		Assert.Equal(0, harness.State.ScanOutcome.FileCount);
	}

	private static RepositoryFixture FixtureWithThreeExcludedFolders()
	{
		RepositoryFixture fixture = RepositoryFixture.CreateEmpty();

		fixture.WriteText(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);
		fixture.WriteText($"{_EXCLUDED_AT_ROOT}/{_EXCLUDED_FILE_NAME}", _ORDINARY_CONTENT);
		fixture.WriteText($"{_EXCLUDED_NESTED}/{_EXCLUDED_FILE_NAME}", _ORDINARY_CONTENT);
		fixture.WriteText($"{_EXCLUDED_DEEP}/{_EXCLUDED_FILE_NAME}", _ORDINARY_CONTENT);

		return fixture;
	}
}
