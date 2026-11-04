using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Pipeline;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.WorstCase;

using Xunit;

namespace MigrateAuth.Cli.Tests.WorstCase;

/// <summary>
/// THE WORST-CASE GENERATOR: deterministic from its seed, written outside
/// any repository, shaped so the High constructs run past the global findings cap, and covering the
/// skipped-file categories. The scenario sizes (50,000 and 500,000 files) are exercised by planning,
/// which is a pure function; the writing of a tree is exercised at a few thousand files.
/// </summary>
public sealed class WorstCaseTreeGeneratorTests
{
	private const int _SMALL_FILE_COUNT = 300;
	private const int _SHAPE_FILE_COUNT = 3_000;
	private const int _OVERSIZE_FILE_COUNT = 3;
	private const int _SCENARIO_LARGE_FILE_COUNT = 500_000;
	private const int _SEED = 11;
	private const int _OTHER_SEED = 12;
	private const int _PROGRESS_BLOCK_FOR_TEST = 100;
	private const string _GENERATED_FOLDER_NAME = "generated";
	private const string _POWERSHELL_LANGUAGE_NAME = "PowerShell";
	private const string _POWERSHELL_SHEBANG_LINE = "#!/usr/bin/env pwsh";
	private const string _POWERSHELL_SCRIPT_EXTENSION = ".ps1";
	private const int _POWERSHELL_BASIS_POINTS = 1_000;
	private const char _LINE_FEED = '\n';

	[Fact]
	public void TheGeneratorRestatesTheProductCapsAndCeilingsItIsSizedAgainst()
	{
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_GLOBAL_FINDINGS_CAP, WorstCaseTreeOptions.PRODUCT_GLOBAL_FINDINGS_CAP);
		Assert.Equal(FindingsCapPolicy.PROVISIONAL_PER_FILE_FINDINGS_CAP, WorstCaseTreeOptions.PRODUCT_PER_FILE_FINDINGS_CAP);
		Assert.Equal(ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES, WorstCaseFileContent.PRODUCT_MAXIMUM_FILE_SIZE_IN_BYTES);
		Assert.Equal(ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS, WorstCaseFileContent.PRODUCT_MAXIMUM_LINE_LENGTH_IN_CHARACTERS);
	}

	[Fact]
	public void TheSameSeedAndCountGiveAByteIdenticalTree()
	{
		using TemporaryFolder first = TemporaryFolder.Create();
		using TemporaryFolder second = TemporaryFolder.Create();

		WorstCaseTreeGenerator.Generate(OptionsFor(first, _SMALL_FILE_COUNT, _SEED));
		WorstCaseTreeGenerator.Generate(OptionsFor(second, _SMALL_FILE_COUNT, _SEED));

		Assert.Equal(SnapshotOf(first.Path), SnapshotOf(second.Path));
	}

	[Fact]
	public void ADifferentSeedGivesADifferentTree()
	{
		using TemporaryFolder first = TemporaryFolder.Create();
		using TemporaryFolder second = TemporaryFolder.Create();

		WorstCaseTreeGenerator.Generate(OptionsFor(first, _SMALL_FILE_COUNT, _SEED));
		WorstCaseTreeGenerator.Generate(OptionsFor(second, _SMALL_FILE_COUNT, _OTHER_SEED));

		Assert.NotEqual(SnapshotOf(first.Path), SnapshotOf(second.Path));
	}

	[Fact]
	public void ATargetInsideTheRepositoryIsRefusedAndNothingIsWritten()
	{
		string repositoryRoot = RepositoryBoundary.FindRepositoryRoot(AppContext.BaseDirectory)!;
		string target = Path.Combine(repositoryRoot, _GENERATED_FOLDER_NAME);

		Assert.Throws<ArgumentException>(() => WorstCaseTreeGenerator.Generate(new WorstCaseTreeOptions { OutputRoot = target, FileCount = _SMALL_FILE_COUNT }));
		Assert.False(Directory.Exists(target));
	}

	[Fact]
	public void ANonEmptyTargetIsRefused()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();

		File.WriteAllText(folder.Combine("existing.txt"), "x");

		Assert.Throws<IOException>(() => WorstCaseTreeGenerator.Generate(OptionsFor(folder, _SMALL_FILE_COUNT, _SEED)));
	}

	[Fact]
	public void ProgressIsReportedOnceForEveryBlockOfFiles()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		List<string> lines = [];
		WorstCaseTreeOptions options = OptionsFor(folder, _SMALL_FILE_COUNT, _SEED) with
		{
			ProgressBlockSize = _PROGRESS_BLOCK_FOR_TEST,
			ReportProgress = lines.Add
		};

		WorstCaseTreeGenerator.Generate(options);

		Assert.Equal(options.FileCount / _PROGRESS_BLOCK_FOR_TEST, lines.Count);
		Assert.Equal($"Wrote {_PROGRESS_BLOCK_FOR_TEST:N0} of {options.FileCount:N0} files", lines[0]);
		Assert.Equal(50_000, WorstCaseTreeOptions.SCENARIO_PROGRESS_BLOCK_SIZE);
	}

	[Fact]
	public void PathOrderEqualsIndexOrderAtHalfAMillionFilesSoTheHeavyFilesComeFirst()
	{
		WorstCaseTreeOptions options = new() { Seed = _SEED, OutputRoot = string.Empty, FileCount = _SCENARIO_LARGE_FILE_COUNT };
		string previous = string.Empty;
		int heavyCount = 0;

		for (int index = 0; index < _SCENARIO_LARGE_FILE_COUNT; index++)
		{
			WorstCaseFilePlan plan = WorstCaseTreePlanner.PlanFile(options, index);

			Assert.True(string.CompareOrdinal(plan.RelativePath, previous) > 0);
			previous = plan.RelativePath;
			heavyCount += plan.Kind == WorstCaseFileKind.Heavy ? 1 : 0;
		}

		Assert.Equal(options.HeavyFileCount, heavyCount);
		Assert.Equal(WorstCaseFileKind.HighTail, WorstCaseTreePlanner.PlanFile(options, options.HeavyFileCount).Kind);
		Assert.True(options.HeavyFileCount * options.PerFileFindingsCap > options.GlobalFindingsCap);
	}

	[Fact]
	public void TheTreeHoldsEveryKindAndTheSkippedFilesBreakTheirCeilings()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with
		{
			OversizeFileCount = _OVERSIZE_FILE_COUNT,
			PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS
		};

		WorstCaseTreeSummary summary = WorstCaseTreeGenerator.Generate(options);
		List<WorstCaseFilePlan> plans = Enumerable.Range(0, options.FileCount).Select(index => WorstCaseTreePlanner.PlanFile(options, index)).ToList();

		Assert.Equal(options.FileCount, summary.FilesWritten);
		Assert.All(Enum.GetValues<WorstCaseFileKind>(), kind => Assert.True(summary.FilesByKind.GetValueOrDefault(kind) > 0, kind.ToString()));
		Assert.Equal(_OVERSIZE_FILE_COUNT, summary.FilesByKind[WorstCaseFileKind.Oversize]);
		Assert.All(PathsOf(plans, WorstCaseFileKind.Oversize), path => Assert.True(new FileInfo(Path.Combine(folder.Path, path)).Length > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES));
		Assert.All(PathsOf(plans, WorstCaseFileKind.OverLongLine), path => Assert.Contains(File.ReadLines(Path.Combine(folder.Path, path)), line => line.Length > ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS));
		Assert.All(PathsOf(plans, WorstCaseFileKind.Undecodable), path => Assert.Throws<DecoderFallbackException>(() => new UTF8Encoding(false, true).GetString(File.ReadAllBytes(Path.Combine(folder.Path, path)))));
	}

	[Fact]
	public void ATreeAskedForNoPowerShellHoldsNoPowerShellFile()
	{
		WorstCaseTreeOptions options = new() { Seed = _SEED, OutputRoot = string.Empty, FileCount = _SHAPE_FILE_COUNT };

		IEnumerable<WorstCaseFileKind> kinds = Enumerable.Range(0, _SHAPE_FILE_COUNT).Select(index => WorstCaseTreePlanner.KindOf(options, index));

		Assert.DoesNotContain(kinds, kind => kind is WorstCaseFileKind.PowerShellScript or WorstCaseFileKind.PowerShellShebangScript);
	}

	[Fact]
	public void ThePowerShellShareReplacesOrdinaryFilesOnlyAndKeepsEveryOtherFileAsItWas()
	{
		WorstCaseTreeOptions without = new() { Seed = _SEED, OutputRoot = string.Empty, FileCount = _SHAPE_FILE_COUNT };
		WorstCaseTreeOptions with = without with { PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS };
		List<(WorstCaseFileKind Before, WorstCaseFileKind After)> pairs =
		[
			.. Enumerable.Range(0, _SHAPE_FILE_COUNT).Select(index => (WorstCaseTreePlanner.KindOf(without, index), WorstCaseTreePlanner.KindOf(with, index)))
		];

		Assert.All(pairs.Where(pair => pair.Before != pair.After), pair => Assert.Equal(WorstCaseFileKind.Filler, pair.Before));
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.PowerShellScript);
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.PowerShellShebangScript);
	}

	[Fact]
	public void TheShebangFormIsExtensionlessAndItsFirstLineAloneAttributesItToPowerShell()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS };
		List<WorstCaseFilePlan> plans = Enumerable.Range(0, options.FileCount).Select(index => WorstCaseTreePlanner.PlanFile(options, index)).ToList();
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);

		WorstCaseTreeGenerator.Generate(options);

		Assert.All(PathsOf(plans, WorstCaseFileKind.PowerShellScript), path => Assert.EndsWith(_POWERSHELL_SCRIPT_EXTENSION, path, StringComparison.Ordinal));
		Assert.All(PathsOf(plans, WorstCaseFileKind.PowerShellShebangScript), path =>
		{
			string firstLine = File.ReadAllText(Path.Combine(folder.Path, path)).Split(_LINE_FEED)[0];

			Assert.Equal(string.Empty, Path.GetExtension(path));
			Assert.Equal(_POWERSHELL_SHEBANG_LINE, firstLine);
			Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, path, out RepositoryRelativePath relativePath));
			Assert.Equal(_POWERSHELL_LANGUAGE_NAME, attribution.Attribute(relativePath, firstLine).LanguageName);
		});
	}

	[Fact]
	public void TheAnalyzerRecordsExactlyTheSkippedFilesThatWereCreated()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		WorstCaseTreeOptions seeded = new() { Seed = _SEED, OutputRoot = harness.RepositoryRootPath, FileCount = 0 };
		WorstCaseTreeOptions options = seeded with
		{
			FileCount = _SHAPE_FILE_COUNT,
			OversizeFileCount = _OVERSIZE_FILE_COUNT
		};
		List<WorstCaseFilePlan> plans = Enumerable.Range(0, _SHAPE_FILE_COUNT).Select(index => WorstCaseTreePlanner.PlanFile(options, index)).ToList();

		WorstCaseTreeGenerator.Generate(options);
		harness.RunAndCommit(CommandId.Scan);
		harness.Run(CommandId.Analyze);

		Assert.Equal(PathsOf(plans, WorstCaseFileKind.Oversize).Count, SkippedCountOf(harness, SkippedItemCategory.OversizeFile));
		Assert.Equal(PathsOf(plans, WorstCaseFileKind.Undecodable).Count, SkippedCountOf(harness, SkippedItemCategory.Undecodable));
		Assert.Equal(PathsOf(plans, WorstCaseFileKind.OverLongLine).Count, SkippedCountOf(harness, SkippedItemCategory.OverLongLine));
	}

	private static WorstCaseTreeOptions OptionsFor(TemporaryFolder folder, int fileCount, long seed)
	{
		return new WorstCaseTreeOptions
		{
			Seed = seed,
			OutputRoot = folder.Path,
			FileCount = fileCount + new WorstCaseTreeOptions { OutputRoot = string.Empty, FileCount = 0 }.HeavyFileCount
		};
	}

	private static int SkippedCountOf(PipelineHarness harness, SkippedItemCategory category)
	{
		return harness.State.MergedSkippedItems().Single(tally => tally.Category == category).Count;
	}

	private static List<string> PathsOf(List<WorstCaseFilePlan> plans, WorstCaseFileKind kind)
	{
		return plans.Where(plan => plan.Kind == kind).Select(plan => plan.RelativePath).ToList();
	}

	/// <summary>Every file's relative path and a hash of its bytes, in path order.</summary>
	private static List<string> SnapshotOf(string root)
	{
		return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
			.Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
			.OrderBy(path => path, StringComparer.Ordinal)
			.Select(path => path + ":" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(root, path)))))
			.ToList();
	}
}
