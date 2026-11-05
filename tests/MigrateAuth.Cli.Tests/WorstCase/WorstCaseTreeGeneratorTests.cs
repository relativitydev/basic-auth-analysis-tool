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
	private const int _VBNET_BASIS_POINTS = 1_000;
	private const int _SHELL_BASIS_POINTS = 1_000;
	private const string _SHELL_LANGUAGE_NAME = "Shell";
	private const string _SHELL_SHEBANG_LINE = "#!/usr/bin/env bash";
	private const string _SHELL_SCRIPT_EXTENSION = ".sh";
	private const int _JAVASCRIPT_BASIS_POINTS = 1_000;
	private const string _JAVASCRIPT_LANGUAGE_NAME = "JavaScript";
	private const string _JAVASCRIPT_HASHBANG_LINE = "#!/usr/bin/env node";
	private const string _JAVASCRIPT_SOURCE_EXTENSION = ".js";
	private const string _JAVASCRIPT_JSX_EXTENSION = ".jsx";
	private const int _TYPESCRIPT_BASIS_POINTS = 1_000;
	private const int _TYPESCRIPT_SHARE_CEILING_DIVISOR = 4;
	private const int _BASIS_POINTS_ABOVE_THE_WHOLE = 10_001;
	private const string _TYPESCRIPT_LANGUAGE_NAME = "TypeScript";
	private const string _DECLARATION_FILE_MARKER = ".d.";
	private const string _TYPESCRIPT_SOURCE_EXTENSION = ".ts";
	private const string _TYPESCRIPT_JSX_EXTENSION = ".tsx";
	private const string _URL_SCHEME_SEPARATOR = "://";
	private const int _EXPECTED_SCENARIO_PROGRESS_BLOCK_SIZE = 50_000;
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
		Assert.Equal(_EXPECTED_SCENARIO_PROGRESS_BLOCK_SIZE,WorstCaseTreeOptions.SCENARIO_PROGRESS_BLOCK_SIZE);
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
			PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS,
			VBNetSourceBasisPoints = _POWERSHELL_BASIS_POINTS,
			ShellScriptBasisPoints = _SHELL_BASIS_POINTS,
			JavaScriptSourceBasisPoints = _JAVASCRIPT_BASIS_POINTS,
			TypeScriptSourceBasisPoints = _TYPESCRIPT_BASIS_POINTS
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
	public void ATreeAskedForNoVBNetHoldsNoVisualBasicFileAndTheVBNetShareReplacesOrdinaryFilesOnly()
	{
		WorstCaseTreeOptions without = new() { Seed = _SEED, OutputRoot = string.Empty, FileCount = _SHAPE_FILE_COUNT, PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS };
		WorstCaseTreeOptions with = without with { VBNetSourceBasisPoints = _POWERSHELL_BASIS_POINTS };
		List<(WorstCaseFileKind Before, WorstCaseFileKind After)> pairs =
		[
			.. Enumerable.Range(0, _SHAPE_FILE_COUNT).Select(index => (WorstCaseTreePlanner.KindOf(without, index), WorstCaseTreePlanner.KindOf(with, index)))
		];

		Assert.DoesNotContain(pairs, pair => pair.Before == WorstCaseFileKind.VBNetSource);
		Assert.All(pairs.Where(pair => pair.Before != pair.After), pair => Assert.Equal((WorstCaseFileKind.Filler, WorstCaseFileKind.VBNetSource), pair));
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.VBNetSource);
	}

	[Fact]
	public void AVisualBasicFileHasTheVbExtensionAndIsAttributedToVBNet()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { VBNetSourceBasisPoints = _POWERSHELL_BASIS_POINTS };
		List<WorstCaseFilePlan> plans = Enumerable.Range(0, options.FileCount).Select(index => WorstCaseTreePlanner.PlanFile(options, index)).ToList();
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);

		WorstCaseTreeGenerator.Generate(options);

		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.VBNetSource));
		Assert.All(PathsOf(plans, WorstCaseFileKind.VBNetSource), path =>
		{
			Assert.EndsWith(".vb", path, StringComparison.Ordinal);
			Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, path, out RepositoryRelativePath relativePath));
			Assert.Equal("VBNet", attribution.Attribute(relativePath, File.ReadAllText(Path.Combine(folder.Path, path)).Split(_LINE_FEED)[0]).LanguageName);
		});
	}

	[Fact]
	public void ATreeAskedForNoShellHoldsNoShellFileAndTheShellShareReplacesOrdinaryFilesOnly()
	{
		WorstCaseTreeOptions withoutShellShare = new() { Seed = _SEED, OutputRoot = string.Empty, FileCount = _SHAPE_FILE_COUNT, PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS, VBNetSourceBasisPoints = _VBNET_BASIS_POINTS };
		WorstCaseTreeOptions withShellShare = withoutShellShare with { ShellScriptBasisPoints = _SHELL_BASIS_POINTS };
		List<(WorstCaseFileKind Before, WorstCaseFileKind After)> pairs = KindPairsOf(withoutShellShare, withShellShare);

		Assert.DoesNotContain(pairs, pair => pair.Before is WorstCaseFileKind.ShellScript or WorstCaseFileKind.ShellShebangScript);
		Assert.All(pairs.Where(pair => pair.Before != pair.After), pair => Assert.Equal(WorstCaseFileKind.Filler, pair.Before));
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.ShellScript);
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.ShellShebangScript);
	}

	[Fact]
	public void ATreeWithNoShellShareIsByteIdenticalWhetherTheShellShareIsLeftOutOrSetToZero()
	{
		using TemporaryFolder first = TemporaryFolder.Create();
		using TemporaryFolder second = TemporaryFolder.Create();
		WorstCaseTreeOptions baseline = new() { Seed = _SEED, OutputRoot = first.Path, FileCount = _SHAPE_FILE_COUNT, PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS, VBNetSourceBasisPoints = _VBNET_BASIS_POINTS };
		WorstCaseTreeOptions explicitZero = baseline with { OutputRoot = second.Path, ShellScriptBasisPoints = 0 };

		WorstCaseTreeGenerator.Generate(baseline);
		WorstCaseTreeGenerator.Generate(explicitZero);

		List<string> firstFiles = RelativeFilesOf(first.Path);
		List<string> secondFiles = RelativeFilesOf(second.Path);

		Assert.Equal(firstFiles, secondFiles);
		Assert.All(firstFiles, relative => Assert.Equal(File.ReadAllBytes(Path.Combine(first.Path, relative)), File.ReadAllBytes(Path.Combine(second.Path, relative))));
		Assert.DoesNotContain(firstFiles, relative => relative.EndsWith(_SHELL_SCRIPT_EXTENSION, StringComparison.Ordinal));
	}

	[Fact]
	public void AShellFileIsAttributedToShellByItsExtensionOrByItsFirstLineAlone()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { ShellScriptBasisPoints = _SHELL_BASIS_POINTS };
		List<WorstCaseFilePlan> plans = PlansOf(options);
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);

		WorstCaseTreeGenerator.Generate(options);

		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.ShellScript));
		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.ShellShebangScript));
		Assert.All(PathsOf(plans, WorstCaseFileKind.ShellScript), path =>
		{
			(RepositoryRelativePath relativePath, string firstLine) = ResolveScannedFile(repository, folder.Path, path);

			Assert.EndsWith(_SHELL_SCRIPT_EXTENSION, path, StringComparison.Ordinal);
			Assert.Equal(_SHELL_LANGUAGE_NAME, attribution.Attribute(relativePath, firstLine).LanguageName);
		});
		Assert.All(PathsOf(plans, WorstCaseFileKind.ShellShebangScript), path =>
		{
			(RepositoryRelativePath relativePath, string firstLine) = ResolveScannedFile(repository, folder.Path, path);

			Assert.Equal(string.Empty, Path.GetExtension(path));
			Assert.Equal(_SHELL_SHEBANG_LINE, firstLine);
			Assert.Equal(_SHELL_LANGUAGE_NAME, attribution.Attribute(relativePath, firstLine).LanguageName);
		});
	}

	[Fact]
	public void AShareOfTheShellFilesIsPreselectedByTheShippedShellAnchorsAndTheRestIsNot()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { ShellScriptBasisPoints = _SHELL_BASIS_POINTS };
		List<WorstCaseFilePlan> plans = PlansOf(options);
		ILanguageAnalyzer shellAnalyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_SHELL_LANGUAGE_NAME];
		WorstCaseTreeGenerator.Generate(options);

		List<bool> preselected = PathsOf(plans, WorstCaseFileKind.ShellScript)
			.Concat(PathsOf(plans, WorstCaseFileKind.ShellShebangScript))
			.Select(path => CandidatePreselection.IsCandidate(shellAnalyzer, File.ReadAllText(Path.Combine(folder.Path, path))))
			.ToList();

		Assert.Contains(true, preselected);
		Assert.Contains(false, preselected);
	}

	[Fact]
	public void ATreeAskedForNoJavaScriptHoldsNoJavaScriptFileAndTheJavaScriptShareReplacesOrdinaryFilesOnly()
	{
		WorstCaseTreeOptions withoutShare = new() { Seed = _SEED, OutputRoot = string.Empty, FileCount = _SHAPE_FILE_COUNT, PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS, VBNetSourceBasisPoints = _VBNET_BASIS_POINTS, ShellScriptBasisPoints = _SHELL_BASIS_POINTS };
		WorstCaseTreeOptions withShare = withoutShare with { JavaScriptSourceBasisPoints = _JAVASCRIPT_BASIS_POINTS };
		List<(WorstCaseFileKind Before, WorstCaseFileKind After)> pairs = KindPairsOf(withoutShare, withShare);

		Assert.DoesNotContain(pairs, pair => pair.Before is WorstCaseFileKind.JavaScriptSource or WorstCaseFileKind.JavaScriptJsxSource or WorstCaseFileKind.JavaScriptNodeScript);
		Assert.All(pairs.Where(pair => pair.Before != pair.After), pair => Assert.Equal(WorstCaseFileKind.Filler, pair.Before));
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.JavaScriptSource);
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.JavaScriptJsxSource);
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.JavaScriptNodeScript);
	}

	[Fact]
	public void ATreeWithNoJavaScriptShareIsByteIdenticalWhetherTheJavaScriptShareIsLeftOutOrSetToZero()
	{
		using TemporaryFolder first = TemporaryFolder.Create();
		using TemporaryFolder second = TemporaryFolder.Create();
		WorstCaseTreeOptions baseline = new() { Seed = _SEED, OutputRoot = first.Path, FileCount = _SHAPE_FILE_COUNT, PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS, VBNetSourceBasisPoints = _VBNET_BASIS_POINTS, ShellScriptBasisPoints = _SHELL_BASIS_POINTS };
		WorstCaseTreeOptions explicitZero = baseline with { OutputRoot = second.Path, JavaScriptSourceBasisPoints = 0 };

		WorstCaseTreeGenerator.Generate(baseline);
		WorstCaseTreeGenerator.Generate(explicitZero);

		List<string> firstFiles = RelativeFilesOf(first.Path);
		List<string> secondFiles = RelativeFilesOf(second.Path);

		Assert.Equal(firstFiles, secondFiles);
		Assert.All(firstFiles, relative => Assert.Equal(File.ReadAllBytes(Path.Combine(first.Path, relative)), File.ReadAllBytes(Path.Combine(second.Path, relative))));
		Assert.DoesNotContain(firstFiles, relative => relative.EndsWith(_JAVASCRIPT_SOURCE_EXTENSION, StringComparison.Ordinal) || relative.EndsWith(_JAVASCRIPT_JSX_EXTENSION, StringComparison.Ordinal));
	}

	[Fact]
	public void AJavaScriptFileIsAttributedToJavaScriptByItsExtensionOrByItsFirstLineAlone()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { JavaScriptSourceBasisPoints = _JAVASCRIPT_BASIS_POINTS };
		List<WorstCaseFilePlan> plans = PlansOf(options);
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);

		WorstCaseTreeGenerator.Generate(options);

		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.JavaScriptSource));
		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.JavaScriptJsxSource));
		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.JavaScriptNodeScript));
		Assert.All(PathsOf(plans, WorstCaseFileKind.JavaScriptSource), path => AssertAttributedToJavaScript(attribution, repository, folder.Path, path, _JAVASCRIPT_SOURCE_EXTENSION));
		Assert.All(PathsOf(plans, WorstCaseFileKind.JavaScriptJsxSource), path => AssertAttributedToJavaScript(attribution, repository, folder.Path, path, _JAVASCRIPT_JSX_EXTENSION));
		Assert.All(PathsOf(plans, WorstCaseFileKind.JavaScriptNodeScript), path =>
		{
			(_, string firstLine) = ResolveScannedFile(repository, folder.Path, path);

			Assert.Equal(string.Empty, Path.GetExtension(path));
			Assert.Equal(_JAVASCRIPT_HASHBANG_LINE, firstLine);
			AssertAttributedToJavaScript(attribution, repository, folder.Path, path, string.Empty);
		});
	}

	[Fact]
	public void AShareOfTheJavaScriptFilesIsPreselectedByTheShippedJavaScriptAnchorsAndTheRestIsNot()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { JavaScriptSourceBasisPoints = _JAVASCRIPT_BASIS_POINTS };
		List<WorstCaseFilePlan> plans = PlansOf(options);
		ILanguageAnalyzer javaScriptAnalyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_JAVASCRIPT_LANGUAGE_NAME];

		WorstCaseTreeGenerator.Generate(options);

		List<bool> preselected = PathsOf(plans, WorstCaseFileKind.JavaScriptSource)
			.Concat(PathsOf(plans, WorstCaseFileKind.JavaScriptJsxSource))
			.Concat(PathsOf(plans, WorstCaseFileKind.JavaScriptNodeScript))
			.Select(path => CandidatePreselection.IsCandidate(javaScriptAnalyzer, File.ReadAllText(Path.Combine(folder.Path, path))))
			.ToList();

		Assert.Contains(true, preselected);
		Assert.Contains(false, preselected);
	}

	[Fact]
	public void ATreeAskedForNoTypeScriptHoldsNoTypeScriptFileAndTheTypeScriptShareReplacesOrdinaryFilesOnly()
	{
		WorstCaseTreeOptions withoutShare = new() { Seed = _SEED, OutputRoot = string.Empty, FileCount = _SHAPE_FILE_COUNT, PowerShellScriptBasisPoints = _POWERSHELL_BASIS_POINTS, VBNetSourceBasisPoints = _VBNET_BASIS_POINTS, ShellScriptBasisPoints = _SHELL_BASIS_POINTS, JavaScriptSourceBasisPoints = _JAVASCRIPT_BASIS_POINTS };
		WorstCaseTreeOptions withShare = withoutShare with { TypeScriptSourceBasisPoints = _TYPESCRIPT_BASIS_POINTS };
		List<(WorstCaseFileKind Before, WorstCaseFileKind After)> pairs = KindPairsOf(withoutShare, withShare);

		Assert.DoesNotContain(pairs, pair => IsTypeScriptKind(pair.Before));
		Assert.All(pairs.Where(pair => pair.Before != pair.After), pair => Assert.Equal(WorstCaseFileKind.Filler, pair.Before));
		Assert.All(TypeScriptKinds(), kind => Assert.Contains(pairs, pair => pair.After == kind));
	}

	[Fact]
	public void TheTypeScriptShareIsRoughlyTheBasisPointsAskedForAndTheKindsStillSumToTheTotal()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { TypeScriptSourceBasisPoints = _TYPESCRIPT_BASIS_POINTS };

		WorstCaseTreeSummary summary = WorstCaseTreeGenerator.Generate(options);
		long typeScriptFiles = TypeScriptKinds().Sum(kind => summary.FilesByKind.GetValueOrDefault(kind));

		Assert.Equal(options.FileCount, summary.FilesByKind.Values.Sum());
		Assert.InRange(typeScriptFiles, 1, options.FileCount / _TYPESCRIPT_SHARE_CEILING_DIVISOR);
	}

	[Fact]
	public void ATreeWithNoTypeScriptShareIsByteIdenticalWhetherTheTypeScriptShareIsLeftOutOrSetToZero()
	{
		using TemporaryFolder first = TemporaryFolder.Create();
		using TemporaryFolder second = TemporaryFolder.Create();
		WorstCaseTreeOptions baseline = OptionsFor(first, _SHAPE_FILE_COUNT, _SEED) with { JavaScriptSourceBasisPoints = _JAVASCRIPT_BASIS_POINTS };
		WorstCaseTreeOptions explicitZero = baseline with { OutputRoot = second.Path, TypeScriptSourceBasisPoints = 0 };

		WorstCaseTreeGenerator.Generate(baseline);
		WorstCaseTreeGenerator.Generate(explicitZero);

		List<string> firstFiles = RelativeFilesOf(first.Path);

		Assert.Equal(firstFiles, RelativeFilesOf(second.Path));
		Assert.All(firstFiles, relative => Assert.Equal(File.ReadAllBytes(Path.Combine(first.Path, relative)), File.ReadAllBytes(Path.Combine(second.Path, relative))));
		Assert.DoesNotContain(firstFiles, relative => relative.EndsWith(_TYPESCRIPT_SOURCE_EXTENSION, StringComparison.Ordinal) || relative.EndsWith(_TYPESCRIPT_JSX_EXTENSION, StringComparison.Ordinal));
	}

	[Fact]
	public void ATypeScriptShareAboveTheWholeIsRefusedAndNothingIsWritten()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SMALL_FILE_COUNT, _SEED) with { TypeScriptSourceBasisPoints = _BASIS_POINTS_ABOVE_THE_WHOLE };

		Assert.Throws<ArgumentException>(() => WorstCaseTreeGenerator.Generate(options));
		Assert.Empty(RelativeFilesOf(folder.Path));
	}

	[Fact]
	public void ANegativeTypeScriptShareIsRefused()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SMALL_FILE_COUNT, _SEED) with { TypeScriptSourceBasisPoints = -1 };

		Assert.Throws<ArgumentException>(() => WorstCaseTreeGenerator.Generate(options));
	}

	[Fact]
	public void ATypeScriptTreeInsideTheRepositoryIsRefusedAndNothingIsWritten()
	{
		string repositoryRoot = RepositoryBoundary.FindRepositoryRoot(AppContext.BaseDirectory)!;
		string target = Path.Combine(repositoryRoot, _GENERATED_FOLDER_NAME);

		Assert.Throws<ArgumentException>(() => WorstCaseTreeGenerator.Generate(new WorstCaseTreeOptions { OutputRoot = target, FileCount = _SMALL_FILE_COUNT, TypeScriptSourceBasisPoints = _TYPESCRIPT_BASIS_POINTS }));
		Assert.False(Directory.Exists(target));
	}

	[Fact]
	public void ATypeScriptFileIsAttributedToTypeScriptByItsExtensionAndDeclarationFilesKeepTheirDoubleExtension()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { TypeScriptSourceBasisPoints = _TYPESCRIPT_BASIS_POINTS };
		List<WorstCaseFilePlan> plans = PlansOf(options);
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);

		WorstCaseTreeGenerator.Generate(options);

		Assert.All(TypeScriptKinds(), kind => Assert.NotEmpty(PathsOf(plans, kind)));
		Assert.All(plans.Where(plan => IsTypeScriptKind(plan.Kind)), plan =>
		{
			(RepositoryRelativePath relativePath, string firstLine) = ResolveScannedFile(repository, folder.Path, plan.RelativePath);

			Assert.Equal(_TYPESCRIPT_LANGUAGE_NAME, attribution.Attribute(relativePath, firstLine).LanguageName);
			Assert.Equal(plan.Kind is WorstCaseFileKind.TypeScriptDeclarationFile or WorstCaseFileKind.TypeScriptModuleDeclarationFile or WorstCaseFileKind.TypeScriptCommonJsDeclarationFile, plan.RelativePath.Contains(_DECLARATION_FILE_MARKER, StringComparison.Ordinal));
		});
	}

	[Fact]
	public void AShareOfTheTypeScriptFilesIsPreselectedAndAnchorDenseDeclarationFilesHoldManyAnchors()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder, _SHAPE_FILE_COUNT, _SEED) with { TypeScriptSourceBasisPoints = _TYPESCRIPT_BASIS_POINTS };
		List<WorstCaseFilePlan> plans = PlansOf(options);
		ILanguageAnalyzer typeScriptAnalyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_TYPESCRIPT_LANGUAGE_NAME];

		WorstCaseTreeGenerator.Generate(options);

		List<bool> preselected = plans.Where(plan => IsTypeScriptKind(plan.Kind))
			.Select(plan => CandidatePreselection.IsCandidate(typeScriptAnalyzer, File.ReadAllText(Path.Combine(folder.Path, plan.RelativePath))))
			.ToList();
		int mostAnchorsInADeclarationFile = PathsOf(plans, WorstCaseFileKind.TypeScriptDeclarationFile)
			.Max(path => File.ReadAllText(Path.Combine(folder.Path, path)).Split(_URL_SCHEME_SEPARATOR).Length - 1);

		Assert.Contains(true, preselected);
		Assert.Contains(false, preselected);
		Assert.True(mostAnchorsInADeclarationFile > 1);
	}

	private static IEnumerable<WorstCaseFileKind> TypeScriptKinds()
	{
		return Enum.GetValues<WorstCaseFileKind>().Where(IsTypeScriptKind);
	}

	private static bool IsTypeScriptKind(WorstCaseFileKind kind)
	{
		return kind >= WorstCaseFileKind.TypeScriptSource;
	}

	private static void AssertAttributedToJavaScript(LanguageAttribution attribution, RepositoryFolderPath repository, string folderPath, string path, string expectedExtension)
	{
		(RepositoryRelativePath relativePath, string firstLine) = ResolveScannedFile(repository, folderPath, path);

		Assert.Equal(expectedExtension, Path.GetExtension(path));
		Assert.Equal(_JAVASCRIPT_LANGUAGE_NAME, attribution.Attribute(relativePath, firstLine).LanguageName);
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

	private static List<string> RelativeFilesOf(string folderPath)
	{
		return Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(folderPath, path)).Order(StringComparer.Ordinal).ToList();
	}

	private static (RepositoryRelativePath RelativePath, string FirstLine) ResolveScannedFile(RepositoryFolderPath repository, string folderPath, string path)
	{
		string firstLine = File.ReadAllText(Path.Combine(folderPath, path)).Split(_LINE_FEED)[0];

		Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, path, out RepositoryRelativePath relativePath));

		return (relativePath, firstLine);
	}

	private static List<WorstCaseFilePlan> PlansOf(WorstCaseTreeOptions options)
	{
		return Enumerable.Range(0, options.FileCount).Select(index => WorstCaseTreePlanner.PlanFile(options, index)).ToList();
	}

	private static List<(WorstCaseFileKind Before, WorstCaseFileKind After)> KindPairsOf(WorstCaseTreeOptions optionsWithoutShellShare, WorstCaseTreeOptions optionsWithShellShare)
	{
		return [.. Enumerable.Range(0, optionsWithoutShellShare.FileCount).Select(index => (WorstCaseTreePlanner.KindOf(optionsWithoutShellShare, index), WorstCaseTreePlanner.KindOf(optionsWithShellShare, index)))];
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
