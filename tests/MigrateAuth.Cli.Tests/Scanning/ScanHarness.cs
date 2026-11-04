using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Progress;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Fixtures;
using MigrateAuth.Cli.Tests.Progress;
using MigrateAuth.Cli.Tests.Reading;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// A REAL TREE, A REAL CATALOG AND A CLOCK THE TEST MOVES.
///
/// The tree is the walk tests' fixture rather than a second one, because the conditions this stage
/// has to survive - a denied file, an oversize file, a two-byte encoding, a link - are the ones
/// that fixture already plants, and a scan tested against a tidier tree would be tested against a
/// repository nobody has.
///
/// The catalog is <see cref="ExemplarFixtureCatalog"/>: the reference fixture analyzer followed by
/// the embedded analyzers. These tests work against whatever the catalog holds, and a stage that
/// behaved differently for a real language than for the reference one would be a stage with a
/// language in it.
/// </summary>
internal sealed class ScanHarness : IDisposable
{
	private const string _REPOSITORY_ROOT_LABEL = "harness-repository";
	private const string _PATH_REFUSED_MESSAGE_FORMAT =
		"The harness could not build a repository-relative path: '{0}' is not inside the repository root '{1}'.";

	private ScanHarness(RepositoryFixture fixture, EffectiveConfiguration? configuration = null)
	{
		Fixture = fixture;
		Catalog = ExemplarFixtureCatalog.Build();
		Configuration = configuration ?? BuildAllLanguagesEnabledConfiguration();
		Output = new OrderingWitnessWriter(fixture.Ledger);
		Clock = new SteppingTimeProvider();
		State = SessionState.Fresh(Catalog.Version, _REPOSITORY_ROOT_LABEL);
	}

	internal RepositoryFixture Fixture { get; }

	internal LanguageCatalog Catalog { get; }

	internal EffectiveConfiguration Configuration { get; }

	internal OrderingWitnessWriter Output { get; }

	internal SteppingTimeProvider Clock { get; }

	internal SessionState State { get; }

	internal static ScanHarness CreateEmpty()
	{
		return new ScanHarness(RepositoryFixture.CreateEmpty());
	}

	internal static ScanHarness CreateWithEveryCondition()
	{
		return new ScanHarness(RepositoryFixture.CreateWithEveryCondition());
	}

	/// <summary>An empty tree, with the caller's own configuration rather than the all-enabled default.</summary>
	internal static ScanHarness CreateEmptyWithConfiguration(EffectiveConfiguration configuration)
	{
		return new ScanHarness(RepositoryFixture.CreateEmpty(), configuration);
	}

	/// <summary>The stage as the session builds it, over this harness's tree.</summary>
	internal ScanStage BuildStage()
	{
		return new ScanStage(new ScanStageContext
		{
			Repository = Fixture.Repository,
			Catalog = Catalog,
			Configuration = Configuration,
			EmissionPoint = Fixture.EmissionPoint,
			SkippedItems = Fixture.Ledger,
			Output = Output,
			TimeProvider = Clock
		});
	}

	/// <summary>The two passes on their own, for the tests that are about the passes.</summary>
	internal RepositoryInventoryScan BuildScan()
	{
		return new RepositoryInventoryScan(Fixture.Repository, Catalog, Fixture.EmissionPoint, Configuration.EnabledLanguageSet);
	}

	/// <summary>Every one of the eight languages enabled.</summary>
	private static EffectiveConfiguration BuildAllLanguagesEnabledConfiguration()
	{
		ImmutableArray<LanguageEnablement> enablement =
		[
			.. AssessedLanguages.AllInDeclaredOrder.Select(language => new LanguageEnablement
			{
				Language = language,
				IsEnabled = true
			})
		];

		return new EffectiveConfiguration
		{
			LanguageEnablement = enablement,
			MinimumConfidenceLevel = ConfidenceLevels.WIDEST,
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
	/// A progress reporter for the tests that drive the two passes directly. The small totals
	/// those tests use are below the progress threshold, so it engages and writes nothing - which is
	/// the behaviour under test in the reporter's own file and merely absent noise here.
	/// </summary>
	internal StageProgressReporter BuildProgress(int? totalItemCount)
	{
		return new StageProgressReporter("scan", "files", totalItemCount, Output, Clock);
	}

	/// <summary>A repository-relative path of this tree, typed the one way that type can be made.</summary>
	internal RepositoryRelativePath PathOf(string relativePath)
	{
		string absolutePath = Path.Combine(Fixture.RootPath, relativePath);

		if (!RepositoryRelativePath.TryFromScannedPath(Fixture.Repository, absolutePath, out RepositoryRelativePath path))
		{
			throw new InvalidOperationException(string.Format(
				CultureInfo.InvariantCulture,
				_PATH_REFUSED_MESSAGE_FORMAT,
				absolutePath,
				Fixture.Repository.CanonicalPath));
		}

		return path;
	}

	public void Dispose()
	{
		Fixture.Dispose();
	}
}

/// <summary>
/// THE ORDERING PROOF, AND IT IS A WRITER RATHER THAN A TIMESTAMP.
///
/// The claim is that the advance warning is written before the inventory pass opens a single file.
/// Timing it would prove nothing on a fast machine; asserting the line's position in the
/// transcript would prove only that the lines are in that order in the code.
///
/// So every line written is recorded together with how many files the reader had REFUSED at that
/// moment. The four categories counted are the ones only the reader can raise - unreadable,
/// undecodable, oversize and over-long line - and the reader is the only thing that opens a file.
/// The walk's own categories are deliberately not counted: a link it declined to follow and a
/// directory it could not list are recorded during the enumeration pass, which opens nothing, and
/// counting them would make this witness report an open that never happened.
///
/// A line whose witness reads zero, over a tree that contains a file the reader cannot read, is a
/// line that was written before any file was opened.
/// </summary>
internal sealed class OrderingWitnessWriter : StringWriter
{
	private static readonly SkippedItemCategory[] _categoriesOnlyTheReaderRaises =
	[
		SkippedItemCategory.UnreadableFile,
		SkippedItemCategory.Undecodable,
		SkippedItemCategory.OversizeFile,
		SkippedItemCategory.OverLongLine
	];

	private readonly SkippedItemLedger _ledger;
	private readonly List<WitnessedLine> _witnessedLines = [];

	internal OrderingWitnessWriter(SkippedItemLedger ledger)
	{
		_ledger = ledger;
	}

	internal IReadOnlyList<WitnessedLine> WitnessedLines => _witnessedLines;

	/// <summary>How many files the reader has opened and refused so far.</summary>
	internal int FilesTheReaderRefused =>
		_categoriesOnlyTheReaderRaises.Sum(category => _ledger.TallyOf(category).Count);

	public override void WriteLine(string? value)
	{
		_witnessedLines.Add(new WitnessedLine(value ?? string.Empty, FilesTheReaderRefused));

		base.WriteLine(value);
	}

	/// <summary>Whether a line containing this text was written before the reader opened any file.</summary>
	internal bool WasWrittenBeforeAnyFileWasOpened(string lineFragment)
	{
		return _witnessedLines.Any(line =>
			line.Text.Contains(lineFragment, StringComparison.Ordinal) && line.FilesRefusedSoFar == 0);
	}
}

/// <summary>One written line, and what the reader had refused when it was written.</summary>
internal readonly record struct WitnessedLine(string Text, int FilesRefusedSoFar);
