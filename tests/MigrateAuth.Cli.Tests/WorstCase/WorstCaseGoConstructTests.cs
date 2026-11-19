using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.WorstCase;

using Xunit;

namespace MigrateAuth.Cli.Tests.WorstCase;

/// <summary>
/// THE GENERATED GO FILES CARRY CONSTRUCTS THE SHIPPED ANALYZER RECOGNIZES. Without them a worst-case tree holds Go candidates and no Go finding, and the volume proof never exercises the Go analyzer: the ordinary
/// text holds only a URL with no credential, which preselects a file and matches no class. One source in twenty now holds one to three calls of the request method that sets Basic authentication with held names, a High
/// class (<c>GO-001</c>) of the shipped analyzer. This class runs generated files of the standard and vendored shapes through the shipped analyzer's recognition and checks the count, the class and the level each path
/// reads, and that no file of another kind changes when the Go share is asked for.
/// </summary>
public sealed class WorstCaseGoConstructTests
{
	private const int _FILE_COUNT = 3_000;
	private const int _SEED = 11;
	private const int _WHOLE_SHARE_BASIS_POINTS = 10_000;
	private const int _PYTHON_BASIS_POINTS = 1_000;
	private const int _MAXIMUM_CONSTRUCTS_PER_FILE = 3;
	private const string _HIGH_CLASS = "GO-001";
	private const string _VENDORED_FOLDER_PREFIX = "vendor/";
	private const int _MINIMUM_FILES_WITH_CONSTRUCTS = 20;
	private const int _SHARE_CEILING_DIVISOR = 5;

	[Theory]
	[InlineData(WorstCaseTreeShape.Standard)]
	[InlineData(WorstCaseTreeShape.Vendored)]
	public void EveryGeneratedGoSourceIsRecognizedByTheShippedAnalyzerAsManyTimesAsPlannedAndAtTheLevelItsPathReads(WorstCaseTreeShape shape)
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder.Path, shape);
		List<WorstCaseFilePlan> plans = [.. GoSourcePlans(options)];
		GoRecognitionHarness harness = new();
		int filesWithConstructs = 0;

		WorstCaseTreeGenerator.Generate(options);

		foreach (WorstCaseFilePlan plan in plans)
		{
			int planned = WorstCaseFileContent.GoConstructCountOf(options, plan.Index);
			ImmutableArray<RecognizedConstruct> recognized = harness.Recognize(File.ReadAllText(Path.Combine(folder.Path, plan.RelativePath)), plan.RelativePath);

			Assert.InRange(planned, 0, _MAXIMUM_CONSTRUCTS_PER_FILE);
			Assert.Equal(planned, recognized.Length);
			Assert.All(recognized, construct => Assert.Equal(_HIGH_CLASS, construct.PatternClassIdentifier));
			Assert.All(recognized, construct => Assert.Equal(ExpectedLevelOf(plan.RelativePath), construct.Level));
			filesWithConstructs += planned > 0 ? 1 : 0;
		}

		Assert.True(filesWithConstructs >= _MINIMUM_FILES_WITH_CONSTRUCTS, $"{filesWithConstructs} files with constructs of {plans.Count}");
		Assert.True(filesWithConstructs < plans.Count / _SHARE_CEILING_DIVISOR, $"{filesWithConstructs} files with constructs of {plans.Count}");
	}

	[Fact]
	public void AGeneratedGoFileThatIsPlannedWithoutConstructsHasNoneToRecognize()
	{
		WorstCaseTreeOptions options = OptionsFor(string.Empty, WorstCaseTreeShape.Standard);
		WorstCaseFilePlan plan = GoSourcePlans(options).First(candidate => WorstCaseFileContent.GoConstructCountOf(options, candidate.Index) == 0);

		string text = System.Text.Encoding.UTF8.GetString(WorstCaseFileContent.BytesOf(options, plan));

		Assert.Empty(new GoRecognitionHarness().Recognize(text, plan.RelativePath));
	}

	[Fact]
	public void TheGoShareChangesNoFileOfAnyOtherKindAndNoPythonFileInParticular()
	{
		WorstCaseTreeOptions without = OptionsFor(string.Empty, WorstCaseTreeShape.Standard) with { GoSourceBasisPoints = 0, PythonSourceBasisPoints = _PYTHON_BASIS_POINTS };
		WorstCaseTreeOptions with = without with { GoSourceBasisPoints = _WHOLE_SHARE_BASIS_POINTS };
		List<WorstCaseFilePlan> otherPlans = [.. Enumerable.Range(0, without.FileCount).Select(index => WorstCaseTreePlanner.PlanFile(with, index)).Where(plan => plan.Kind != WorstCaseFileKind.GoSource)];

		Assert.Contains(otherPlans, plan => plan.Kind is WorstCaseFileKind.PythonSource or WorstCaseFileKind.PythonShebangScript or WorstCaseFileKind.PythonStubFile);
		Assert.All(otherPlans, plan => Assert.Equal(WorstCaseFileContent.BytesOf(without, WorstCaseTreePlanner.PlanFile(without, plan.Index)), WorstCaseFileContent.BytesOf(with, plan)));
	}

	private static ConfidenceLevel ExpectedLevelOf(string relativePath)
	{
		return relativePath.StartsWith(_VENDORED_FOLDER_PREFIX, StringComparison.Ordinal) ? ConfidenceLevel.Low : ConfidenceLevel.High;
	}

	private static WorstCaseTreeOptions OptionsFor(string outputRoot, WorstCaseTreeShape shape)
	{
		return new WorstCaseTreeOptions
		{
			Seed = _SEED,
			OutputRoot = outputRoot,
			FileCount = _FILE_COUNT,
			GoSourceBasisPoints = _WHOLE_SHARE_BASIS_POINTS,
			TreeShape = shape
		};
	}

	private static IEnumerable<WorstCaseFilePlan> GoSourcePlans(WorstCaseTreeOptions options)
	{
		return Enumerable.Range(0, options.FileCount)
			.Select(index => WorstCaseTreePlanner.PlanFile(options, index))
			.Where(plan => plan.Kind == WorstCaseFileKind.GoSource);
	}
}
