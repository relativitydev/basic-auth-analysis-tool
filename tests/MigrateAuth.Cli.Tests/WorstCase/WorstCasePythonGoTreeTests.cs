using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.WorstCase;

using Xunit;

namespace MigrateAuth.Cli.Tests.WorstCase;

/// <summary>
/// THE PYTHON AND GO SHARES OF THE WORST-CASE GENERATOR AND ITS TWO TREE SHAPES: a Python share (sources, type stubs and extensionless scripts with a shebang), a Go share (present and unassessed on a line that
/// registers no Go analyzer), a virtual-environment-shaped tree and a vendored tree. Every earlier tree stays byte-identical when the new options are left at zero, the new shares replace only ordinary files, each shape
/// places the Python and Go files where it says, the product's walk reaches every file of a shape, and the same options always produce the same tree.
/// </summary>
public sealed class WorstCasePythonGoTreeTests
{
	private const int _SHAPE_FILE_COUNT = 3_000;
	private const int _SEED = 11;
	private const int _OTHER_SEED = 12;
	private const int _BASIS_POINTS = 1_000;
	private const int _WHOLE_SHARE_BASIS_POINTS = 10_000;
	private const int _BASIS_POINTS_ABOVE_THE_WHOLE = 10_001;
	private const string _PYTHON_LANGUAGE_NAME = "Python";
	private const string _PYTHON_SHEBANG_LINE = "#!/usr/bin/env python3";
	private const string _PYTHON_SOURCE_EXTENSION = ".py";
	private const string _PYTHON_STUB_EXTENSION = ".pyi";
	private const string _GO_SOURCE_EXTENSION = ".go";
	private const string _GO_PACKAGE_LINE = "package ordinary";
	private const string _VIRTUAL_ENVIRONMENT_LIBRARY_PREFIX = ".venv/lib/python3.12/site-packages/";
	private const string _VIRTUAL_ENVIRONMENT_SCRIPTS_PREFIX = ".venv/bin/";
	private const string _VIRTUAL_ENVIRONMENT_FOLDER_PREFIX = ".venv/";
	private const string _THIRD_PARTY_PYTHON_PREFIX = "third_party/python/";
	private const string _VENDORED_GO_PREFIX = "vendor/modules.example/";
	private const string _PLAIN_TOP_FOLDER_PREFIX = "t";
	private const string _PATH_SEPARATOR = "/";
	private const char _PATH_SEPARATOR_CHARACTER = '/';
	private const char _WINDOWS_PATH_SEPARATOR_CHARACTER = '\\';
	private const string _HASH_SEPARATOR = ":";
	private const int _MAJORITY_DIVISOR = 2;
	private const char _LINE_FEED = '\n';

	[Fact]
	public void ATreeAskedForNoPythonAndNoGoHoldsNoFileOfEitherKind()
	{
		WorstCaseTreeOptions options = EarlierSharesOptions();

		Assert.DoesNotContain(PlansOf(options), plan => IsPython(plan.Kind) || plan.Kind == WorstCaseFileKind.GoSource);
	}

	[Fact]
	public void ThePythonAndGoSharesReplaceOrdinaryFilesOnlyAndKeepEveryOtherFileAsItWas()
	{
		WorstCaseTreeOptions without = EarlierSharesOptions();
		WorstCaseTreeOptions with = without with { PythonSourceBasisPoints = _BASIS_POINTS, GoSourceBasisPoints = _BASIS_POINTS };
		List<(WorstCaseFileKind Before, WorstCaseFileKind After)> pairs =
		[
			.. Enumerable.Range(0, without.FileCount).Select(index => (WorstCaseTreePlanner.KindOf(without, index), WorstCaseTreePlanner.KindOf(with, index)))
		];

		Assert.All(pairs.Where(pair => pair.Before != pair.After), pair => Assert.Equal(WorstCaseFileKind.Filler, pair.Before));
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.PythonSource);
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.PythonStubFile);
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.PythonShebangScript);
		Assert.Contains(pairs, pair => pair.After == WorstCaseFileKind.GoSource);
	}

	[Fact]
	public void TheEarlierSharesAndTheDefaultLayoutGiveTheSameKindAndPathWhateverTheNewOptionsAreWhenTheyAreZero()
	{
		WorstCaseTreeOptions baseline = EarlierSharesOptions();
		WorstCaseTreeOptions explicitZero = baseline with { PythonSourceBasisPoints = 0, GoSourceBasisPoints = 0, TreeShape = WorstCaseTreeShape.Standard };
		WorstCaseTreeOptions venvWithNoShare = baseline with { TreeShape = WorstCaseTreeShape.VirtualEnvironment };
		WorstCaseTreeOptions vendoredWithNoShare = baseline with { TreeShape = WorstCaseTreeShape.Vendored };

		Assert.Equal(PlansOf(baseline), PlansOf(explicitZero));
		Assert.Equal(PlansOf(baseline), PlansOf(venvWithNoShare));
		Assert.Equal(PlansOf(baseline), PlansOf(vendoredWithNoShare));
	}

	[Fact]
	public void ATreeWithNoPythonAndNoGoShareIsByteIdenticalWhetherTheNewOptionsAreLeftOutOrSetToZero()
	{
		using TemporaryFolder first = TemporaryFolder.Create();
		using TemporaryFolder second = TemporaryFolder.Create();
		WorstCaseTreeOptions baseline = EarlierSharesOptions() with { OutputRoot = first.Path };
		WorstCaseTreeOptions explicitZero = baseline with { OutputRoot = second.Path, PythonSourceBasisPoints = 0, GoSourceBasisPoints = 0, TreeShape = WorstCaseTreeShape.Standard };

		WorstCaseTreeGenerator.Generate(baseline);
		WorstCaseTreeGenerator.Generate(explicitZero);

		List<string> firstFiles = RelativeFilesOf(first.Path);

		Assert.Equal(firstFiles, RelativeFilesOf(second.Path));
		Assert.All(firstFiles, relative => Assert.Equal(File.ReadAllBytes(Path.Combine(first.Path, relative)), File.ReadAllBytes(Path.Combine(second.Path, relative))));
		Assert.DoesNotContain(firstFiles, relative => relative.EndsWith(_PYTHON_SOURCE_EXTENSION, StringComparison.Ordinal) || relative.EndsWith(_GO_SOURCE_EXTENSION, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(WorstCaseTreeShape.Standard)]
	[InlineData(WorstCaseTreeShape.VirtualEnvironment)]
	[InlineData(WorstCaseTreeShape.Vendored)]
	public void TheSameOptionsGiveAByteIdenticalTreeAndAnotherSeedGivesAnotherOne(WorstCaseTreeShape shape)
	{
		using TemporaryFolder first = TemporaryFolder.Create();
		using TemporaryFolder second = TemporaryFolder.Create();
		using TemporaryFolder other = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(shape) with { OutputRoot = first.Path };

		WorstCaseTreeGenerator.Generate(options);
		WorstCaseTreeGenerator.Generate(options with { OutputRoot = second.Path });
		WorstCaseTreeGenerator.Generate(options with { OutputRoot = other.Path, Seed = _OTHER_SEED });

		Assert.Equal(SnapshotOf(first.Path), SnapshotOf(second.Path));
		Assert.NotEqual(SnapshotOf(first.Path), SnapshotOf(other.Path));
	}

	[Fact]
	public void ThePythonFormsAreSourcesStubsAndExtensionlessScriptsWhoseFirstLineAloneAttributesThemToPython()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(WorstCaseTreeShape.Standard) with { OutputRoot = folder.Path };
		List<WorstCaseFilePlan> plans = PlansOf(options);
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(folder.Path);

		WorstCaseTreeGenerator.Generate(options);

		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.PythonSource));
		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.PythonStubFile));
		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.PythonShebangScript));
		Assert.All(PathsOf(plans, WorstCaseFileKind.PythonSource), path => AssertAttributedToPython(attribution, repository, folder.Path, path, _PYTHON_SOURCE_EXTENSION));
		Assert.All(PathsOf(plans, WorstCaseFileKind.PythonStubFile), path => Assert.EndsWith(_PYTHON_STUB_EXTENSION, path, StringComparison.Ordinal));
		Assert.All(PathsOf(plans, WorstCaseFileKind.PythonShebangScript), path =>
		{
			string firstLine = File.ReadAllText(Path.Combine(folder.Path, path)).Split(_LINE_FEED)[0];

			Assert.Equal(string.Empty, Path.GetExtension(path));
			Assert.Equal(_PYTHON_SHEBANG_LINE, firstLine);
			AssertAttributedToPython(attribution, repository, folder.Path, path, string.Empty);
		});
		Assert.False(BaseLanguageDetection.TryGetByExtension(_PYTHON_STUB_EXTENSION, out _));
	}

	[Fact]
	public void AShareOfThePythonSourcesIsPreselectedByTheShippedPythonAnchorsAndTheRestIsNot()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(WorstCaseTreeShape.Standard) with { OutputRoot = folder.Path };
		List<WorstCaseFilePlan> plans = PlansOf(options);
		ILanguageAnalyzer pythonAnalyzer = LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_PYTHON_LANGUAGE_NAME];

		WorstCaseTreeGenerator.Generate(options);

		List<bool> preselected = PathsOf(plans, WorstCaseFileKind.PythonSource)
			.Select(path => CandidatePreselection.IsCandidate(pythonAnalyzer, File.ReadAllText(Path.Combine(folder.Path, path))))
			.ToList();

		Assert.Contains(true, preselected);
		Assert.Contains(false, preselected);
	}

	[Fact]
	public void GoFilesExistHaveTheGoExtensionAndAGoPackageAndAreAttributedToGoOnlyByTheBaseTable()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(WorstCaseTreeShape.Standard) with { OutputRoot = folder.Path };
		List<string> goPaths = PathsOf(PlansOf(options), WorstCaseFileKind.GoSource);

		WorstCaseTreeGenerator.Generate(options);

		Assert.NotEmpty(goPaths);
		Assert.All(goPaths, path => Assert.EndsWith(_GO_SOURCE_EXTENSION, path, StringComparison.Ordinal));
		Assert.All(goPaths, path => Assert.Contains(_GO_PACKAGE_LINE, File.ReadAllLines(Path.Combine(folder.Path, path))));
		Assert.True(BaseLanguageDetection.TryGetByExtension(_GO_SOURCE_EXTENSION, out AssessedLanguage language));
		Assert.Equal(AssessedLanguage.Go, language);
	}

	[Fact]
	public void TheVirtualEnvironmentShapeStandsTheSourcesAndStubsInSitePackagesAndTheScriptsInBinAndLeavesEveryOtherFileInThePlainLayout()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(WorstCaseTreeShape.VirtualEnvironment) with { OutputRoot = folder.Path };
		List<WorstCaseFilePlan> plans = PlansOf(options);

		WorstCaseTreeGenerator.Generate(options);

		Assert.All(PathsOf(plans, WorstCaseFileKind.PythonSource), path => Assert.StartsWith(_VIRTUAL_ENVIRONMENT_LIBRARY_PREFIX, path, StringComparison.Ordinal));
		Assert.All(PathsOf(plans, WorstCaseFileKind.PythonStubFile), path => Assert.StartsWith(_VIRTUAL_ENVIRONMENT_LIBRARY_PREFIX, path, StringComparison.Ordinal));
		Assert.All(PathsOf(plans, WorstCaseFileKind.PythonShebangScript), path => Assert.StartsWith(_VIRTUAL_ENVIRONMENT_SCRIPTS_PREFIX, path, StringComparison.Ordinal));
		Assert.All(plans.Where(plan => !IsPython(plan.Kind)), plan => Assert.StartsWith(_PLAIN_TOP_FOLDER_PREFIX, plan.RelativePath, StringComparison.Ordinal));
		Assert.All(plans, plan => Assert.True(File.Exists(Path.Combine(folder.Path, plan.RelativePath)), plan.RelativePath));
	}

	[Fact]
	public void ThePythonShareDominatesAVirtualEnvironmentTreeAskedForADominantShare()
	{
		WorstCaseTreeOptions options = new()
		{
			Seed = _SEED,
			OutputRoot = string.Empty,
			FileCount = _SHAPE_FILE_COUNT,
			PythonSourceBasisPoints = _WHOLE_SHARE_BASIS_POINTS,
			TreeShape = WorstCaseTreeShape.VirtualEnvironment
		};
		List<WorstCaseFilePlan> plans = PlansOf(options);
		int pythonFiles = plans.Count(plan => IsPython(plan.Kind));
		int filesUnderTheEnvironment = plans.Count(plan => plan.RelativePath.StartsWith(_VIRTUAL_ENVIRONMENT_FOLDER_PREFIX, StringComparison.Ordinal));

		Assert.Equal(pythonFiles, filesUnderTheEnvironment);
		Assert.True(pythonFiles > plans.Count / _MAJORITY_DIVISOR, $"{pythonFiles} Python files of {plans.Count}");
	}

	[Fact]
	public void TheVendoredShapeStandsPythonUnderThirdPartyAndGoInTheLayoutOfAVendoredModuleAndLeavesEveryOtherFileInThePlainLayout()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(WorstCaseTreeShape.Vendored) with { OutputRoot = folder.Path };
		List<WorstCaseFilePlan> plans = PlansOf(options);

		WorstCaseTreeGenerator.Generate(options);

		Assert.All(plans.Where(plan => IsPython(plan.Kind)), plan => Assert.StartsWith(_THIRD_PARTY_PYTHON_PREFIX, plan.RelativePath, StringComparison.Ordinal));
		Assert.NotEmpty(PathsOf(plans, WorstCaseFileKind.GoSource));
		Assert.All(PathsOf(plans, WorstCaseFileKind.GoSource), path => Assert.StartsWith(_VENDORED_GO_PREFIX, path, StringComparison.Ordinal));
		Assert.All(plans.Where(plan => !IsPython(plan.Kind) && plan.Kind != WorstCaseFileKind.GoSource), plan => Assert.StartsWith(_PLAIN_TOP_FOLDER_PREFIX, plan.RelativePath, StringComparison.Ordinal));
		Assert.All(plans, plan => Assert.True(File.Exists(Path.Combine(folder.Path, plan.RelativePath)), plan.RelativePath));
	}

	[Theory]
	[InlineData(WorstCaseTreeShape.VirtualEnvironment)]
	[InlineData(WorstCaseTreeShape.Vendored)]
	public void TheProductWalkReachesEveryFileOfAShapedTreeAndExcludesNoFolderOfIt(WorstCaseTreeShape shape)
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(shape) with { OutputRoot = folder.Path };
		List<WorstCaseFilePlan> plans = PlansOf(options);

		WorstCaseTreeGenerator.Generate(options);

		RepositoryWalk walk = new(RepositoryFolderPath.FromSuppliedText(folder.Path), new EmissionPoint([new SkippedItemLedger()]));
		List<string> walked = [.. walk.Enumerate().Select(file => file.Path.Value).Order(StringComparer.Ordinal)];

		Assert.Equal([.. plans.Select(plan => plan.RelativePath).Order(StringComparer.Ordinal)], walked);
		Assert.All(plans.SelectMany(plan => plan.RelativePath.Split(_PATH_SEPARATOR)[..^1]).Distinct(StringComparer.Ordinal), segment => Assert.False(WalkExclusions.IsExcluded(segment), segment));
	}

	[Fact]
	public void APythonShareAboveTheWholeIsRefusedAndNothingIsWritten()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(WorstCaseTreeShape.Standard) with { OutputRoot = folder.Path, PythonSourceBasisPoints = _BASIS_POINTS_ABOVE_THE_WHOLE };

		Assert.Throws<ArgumentException>(() => WorstCaseTreeGenerator.Generate(options));
		Assert.Empty(Directory.EnumerateFileSystemEntries(folder.Path));
	}

	[Fact]
	public void ANegativeGoShareIsRefused()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = ShapedOptions(WorstCaseTreeShape.Standard) with { OutputRoot = folder.Path, GoSourceBasisPoints = -1 };

		Assert.Throws<ArgumentException>(() => WorstCaseTreeGenerator.Generate(options));
	}

	private static WorstCaseTreeOptions EarlierSharesOptions()
	{
		return new WorstCaseTreeOptions
		{
			Seed = _SEED,
			OutputRoot = string.Empty,
			FileCount = _SHAPE_FILE_COUNT,
			PowerShellScriptBasisPoints = _BASIS_POINTS,
			VBNetSourceBasisPoints = _BASIS_POINTS,
			ShellScriptBasisPoints = _BASIS_POINTS,
			JavaScriptSourceBasisPoints = _BASIS_POINTS,
			TypeScriptSourceBasisPoints = _BASIS_POINTS
		};
	}

	private static WorstCaseTreeOptions ShapedOptions(WorstCaseTreeShape shape)
	{
		return EarlierSharesOptions() with { PythonSourceBasisPoints = _BASIS_POINTS, GoSourceBasisPoints = _BASIS_POINTS, TreeShape = shape };
	}

	private static bool IsPython(WorstCaseFileKind kind)
	{
		return kind is WorstCaseFileKind.PythonSource or WorstCaseFileKind.PythonStubFile or WorstCaseFileKind.PythonShebangScript;
	}

	private static void AssertAttributedToPython(LanguageAttribution attribution, RepositoryFolderPath repository, string folderPath, string path, string expectedExtension)
	{
		string firstLine = File.ReadAllText(Path.Combine(folderPath, path)).Split(_LINE_FEED)[0];

		Assert.Equal(expectedExtension, Path.GetExtension(path));
		Assert.True(RepositoryRelativePath.TryFromScannedPath(repository, path, out RepositoryRelativePath relativePath));
		Assert.Equal(_PYTHON_LANGUAGE_NAME, attribution.Attribute(relativePath, firstLine).LanguageName);
	}

	private static List<WorstCaseFilePlan> PlansOf(WorstCaseTreeOptions options)
	{
		return [.. Enumerable.Range(0, options.FileCount).Select(index => WorstCaseTreePlanner.PlanFile(options, index))];
	}

	private static List<string> PathsOf(List<WorstCaseFilePlan> plans, WorstCaseFileKind kind)
	{
		return [.. plans.Where(plan => plan.Kind == kind).Select(plan => plan.RelativePath)];
	}

	private static List<string> RelativeFilesOf(string folderPath)
	{
		return [.. Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(folderPath, path)).Order(StringComparer.Ordinal)];
	}

	/// <summary>Every file's relative path and a hash of its bytes, in path order.</summary>
	private static List<string> SnapshotOf(string root)
	{
		return
		[
			.. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
				.Select(path => Path.GetRelativePath(root, path).Replace(_WINDOWS_PATH_SEPARATOR_CHARACTER, _PATH_SEPARATOR_CHARACTER))
				.OrderBy(path => path, StringComparer.Ordinal)
				.Select(path => $"{path}{_HASH_SEPARATOR}{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(root, path))))}")
		];
	}
}
