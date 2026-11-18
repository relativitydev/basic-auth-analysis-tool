using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;
using MigrateAuth.Cli.Tests.Session;

using MigrateAuth.WorstCase;

using Xunit;

namespace MigrateAuth.Cli.Tests.WorstCase;

/// <summary>
/// THE GENERATED PYTHON FILES CARRY CONSTRUCTS THE SHIPPED ANALYZER RECOGNIZES. Without them a worst-case tree holds Python candidates and no Python finding, and the volume proof never exercises the Python analyzer: the
/// ordinary text holds only a URL with no credential, which preselects a file and matches no class. One source or shebang script in twenty now holds one to three calls of the Basic helper class with held names, a High class
/// (<c>PY-001</c>) of the shipped analyzer. This class runs generated files of the virtual-environment and vendored shapes through the shipped analyzer's recognition and checks the count, the class and the level each path reads.
/// </summary>
public sealed class WorstCasePythonConstructTests
{
	private const int _FILE_COUNT = 3_000;
	private const int _SEED = 11;
	private const int _WHOLE_SHARE_BASIS_POINTS = 10_000;
	private const int _MAXIMUM_CONSTRUCTS_PER_FILE = 3;
	private const string _HIGH_CLASS = "PY-001";
	private const string _VIRTUAL_ENVIRONMENT_FOLDER_PREFIX = ".venv/";
	private const int _MINIMUM_FILES_WITH_CONSTRUCTS = 20;
	private const int _SHARE_CEILING_DIVISOR = 5;

	[Theory]
	[InlineData(WorstCaseTreeShape.Standard)]
	[InlineData(WorstCaseTreeShape.VirtualEnvironment)]
	[InlineData(WorstCaseTreeShape.Vendored)]
	public void EveryGeneratedPythonSourceAndScriptIsRecognizedByTheShippedAnalyzerAsManyTimesAsPlannedAndAtTheLevelItsPathReads(WorstCaseTreeShape shape)
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		WorstCaseTreeOptions options = OptionsFor(folder.Path, shape);
		List<WorstCaseFilePlan> plans = [.. PythonSourcePlans(options)];
		PythonRecognitionHarness harness = new();
		int filesWithConstructs = 0;

		WorstCaseTreeGenerator.Generate(options);

		foreach (WorstCaseFilePlan plan in plans)
		{
			int planned = WorstCaseFileContent.PythonConstructCountOf(options, plan.Index);
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
	public void TheConstructsAreFoundInBothTheSourceAndTheShebangFormOfEveryShape()
	{
		WorstCaseTreeOptions options = OptionsFor(string.Empty, WorstCaseTreeShape.VirtualEnvironment);
		List<WorstCaseFilePlan> plans = [.. PythonSourcePlans(options)];

		Assert.Contains(plans, plan => plan.Kind == WorstCaseFileKind.PythonSource && WorstCaseFileContent.PythonConstructCountOf(options, plan.Index) > 0);
		Assert.Contains(plans, plan => plan.Kind == WorstCaseFileKind.PythonShebangScript && WorstCaseFileContent.PythonConstructCountOf(options, plan.Index) > 0);
	}

	[Fact]
	public void AGeneratedPythonFileThatIsPlannedWithoutConstructsHasNoneToRecognize()
	{
		WorstCaseTreeOptions options = OptionsFor(string.Empty, WorstCaseTreeShape.Standard);
		WorstCaseFilePlan plan = PythonSourcePlans(options).First(candidate => WorstCaseFileContent.PythonConstructCountOf(options, candidate.Index) == 0);

		string text = System.Text.Encoding.UTF8.GetString(WorstCaseFileContent.BytesOf(options, plan));

		Assert.Empty(new PythonRecognitionHarness().Recognize(text, plan.RelativePath));
	}

	private static ConfidenceLevel ExpectedLevelOf(string relativePath)
	{
		return relativePath.StartsWith(_VIRTUAL_ENVIRONMENT_FOLDER_PREFIX, StringComparison.Ordinal) ? ConfidenceLevel.Low : ConfidenceLevel.High;
	}

	private static WorstCaseTreeOptions OptionsFor(string outputRoot, WorstCaseTreeShape shape)
	{
		return new WorstCaseTreeOptions
		{
			Seed = _SEED,
			OutputRoot = outputRoot,
			FileCount = _FILE_COUNT,
			PythonSourceBasisPoints = _WHOLE_SHARE_BASIS_POINTS,
			TreeShape = shape
		};
	}

	private static IEnumerable<WorstCaseFilePlan> PythonSourcePlans(WorstCaseTreeOptions options)
	{
		return Enumerable.Range(0, options.FileCount)
			.Select(index => WorstCaseTreePlanner.PlanFile(options, index))
			.Where(plan => plan.Kind is WorstCaseFileKind.PythonSource or WorstCaseFileKind.PythonShebangScript);
	}
}
