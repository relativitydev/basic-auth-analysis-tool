using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.Agent;

/// <summary>
/// THE SEAMS ARE CLOSED IN THE SHIPPED PRODUCT, read from its source: the launcher switch, the search path provider, the
/// stage runner and the native console are constructed in the entry point and nowhere else; the one call that appends to
/// the report is made from the stage and nowhere else; none of the four assessment stages knows any of them; and nothing
/// the product reads (an argument, an environment variable, a configuration key) can open a seam. The test-only variable of
/// the stage timing comparison is read by the pipeline test harness alone and never by the product.
/// </summary>
public sealed class ProductionSeamSourceTests
{
	private const string _LAUNCHER_SWITCH_FILE = "AgentLauncherSwitch.cs";
	private const string _STAGE_RUNNER_FILE = "AiAssistStageRunner.cs";
	private const string _CONSOLE_MODE_GUARD_FILE = "ConsoleModeGuard.cs";
	private const string _TEST_VARIABLE_NAME = "MIGRATEAUTH_TEST_REPORT_HOLDER";
	private const string _PRODUCT_VARIABLE_PREFIX = "MIGRATEAUTH_";
	private const string _PIPELINE_HARNESS_FILE = "PipelineHarness.cs";
	private const string _THIS_FILE = "ProductionSeamSourceTests.cs";
	private const string _TERMINAL_PREDICATE_TEXT = "IsInteractiveTerminal = () => !Console.IsInputRedirected && !Console.IsOutputRedirected";
	private const string _ENVIRONMENT_READ_TEXT = "GetEnvironmentVariable";
	private const string _FOLDERCATALOG = "Catalog";
	private const string _FOLDERCONFIGURATION = "Configuration";
	private const string _FOLDEREMISSION = "Emission";
	private const string _FOLDERFINDINGS = "Findings";
	private const string _FOLDERLEXICAL = "Lexical";
	private const string _FOLDERPIPELINE = "Pipeline";
	private const string _FOLDERPROGRESS = "Progress";
	private const string _FOLDERREADING = "Reading";
	private const string _FOLDERRECOGNITION = "Recognition";
	private const string _FOLDERREPORTING = "Reporting";
	private const string _FOLDERSCANNING = "Scanning";
	private const string _SEAMAGENT_LAUNCHER_SWITCH = "AgentLauncherSwitch";
	private const string _SEAMI_EXTERNAL_AGENT_LAUNCHER = "IExternalAgentLauncher";
	private const string _SEAMI_AGENT_SEARCH_PATH_PROVIDER = "IAgentSearchPathProvider";
	private const string _SEAMPATH_ENVIRONMENT_SEARCH_PATH_PROVIDER = "PathEnvironmentSearchPathProvider";
	private const string _SEAMAI_ASSIST_STAGE_RUNNER = "AiAssistStageRunner";
	private const string _SEAMAI_ASSIST_STAGE_CONTEXT = "AiAssistStageContext";
	private const string _SEAMI_REPORT_APPENDER = "IReportAppender";
	private const string _SEAMAPPEND_TO_REPORT = "AppendToReport";
	private const string _SEAMI_NATIVE_CONSOLE = "INativeConsole";
	private const string _SEAMWINDOWS_NATIVE_CONSOLE = "WindowsNativeConsole";
	private const string _SEAMNO_NATIVE_CONSOLE = "NoNativeConsole";

	private static readonly string[] _featureFourOneFolders =
	[
		_FOLDERCATALOG,
		_FOLDERCONFIGURATION,
		_FOLDEREMISSION,
		_FOLDERFINDINGS,
		_FOLDERLEXICAL,
		_FOLDERPIPELINE,
		_FOLDERPROGRESS,
		_FOLDERREADING,
		_FOLDERRECOGNITION,
		_FOLDERREPORTING,
		_FOLDERSCANNING
	];

	private static readonly string[] _seamNames =
	[
		_SEAMAGENT_LAUNCHER_SWITCH,
		_SEAMI_EXTERNAL_AGENT_LAUNCHER,
		_SEAMI_AGENT_SEARCH_PATH_PROVIDER,
		_SEAMPATH_ENVIRONMENT_SEARCH_PATH_PROVIDER,
		_SEAMAI_ASSIST_STAGE_RUNNER,
		_SEAMAI_ASSIST_STAGE_CONTEXT,
		_SEAMI_REPORT_APPENDER,
		_SEAMAPPEND_TO_REPORT,
		_SEAMI_NATIVE_CONSOLE,
		_SEAMWINDOWS_NATIVE_CONSOLE,
		_SEAMNO_NATIVE_CONSOLE
	];

	[Fact]
	public void TheLauncherSwitchIsCalledOnlyFromTheEntryPoint()
	{
		Assert.Equal([ProductSourceFiles.PROGRAM_FILE], FilesMatching(new Regex(@"\bAgentLauncherSwitch\s*\.\s*Create\s*\(", RegexOptions.CultureInvariant)));
	}

	[Fact]
	public void TheSearchPathProviderIsConstructedOnlyInTheEntryPoint()
	{
		Assert.Equal([ProductSourceFiles.PROGRAM_FILE], FilesMatching(new Regex(@"\bnew\s+PathEnvironmentSearchPathProvider\s*\(", RegexOptions.CultureInvariant)));
	}

	[Fact]
	public void TheAiAssistRunnerIsConstructedOnlyInTheEntryPoint()
	{
		Assert.Equal([ProductSourceFiles.PROGRAM_FILE], FilesMatching(new Regex(@"\bnew\s+AiAssistStageRunner\s*\(", RegexOptions.CultureInvariant)));
	}

	/// <summary>
	/// The stage's native console is built in the entry point. The one other construction is the console-mode guard's own
	/// factory, which the launchers use to save and restore the console modes around a launch; it is not given to the stage
	/// and is named here so that a third site would fail this test.
	/// </summary>
	[Fact]
	public void TheNativeConsoleIsBuiltOnlyInTheEntryPointAndTheConsoleModeGuardsFactory()
	{
		Assert.Equal([_CONSOLE_MODE_GUARD_FILE, ProductSourceFiles.PROGRAM_FILE], FilesMatching(new Regex(@"\bnew\s+(WindowsNativeConsole|NoNativeConsole)\s*\(", RegexOptions.CultureInvariant)));
	}

	/// <summary>The append is defined in the writer and called from the stage, and from no other file.</summary>
	[Fact]
	public void TheReportIsAppendedToOnlyFromTheStage()
	{
		Assert.Equal([_STAGE_RUNNER_FILE], FilesMatching(new Regex(@"\.\s*AppendToReport\s*\(", RegexOptions.CultureInvariant)));
	}

	[Fact]
	public void NoAssessmentStageReferencesAnySeamOrTheStage()
	{
		foreach (string path in ProductSourceFiles.All().Where(path => _featureFourOneFolders.Contains(ProductSourceFiles.TopFolderOf(path))))
		{
			string source = ProductSourceFiles.Read(path);

			foreach (string seamName in _seamNames)
			{
				Assert.False(source.Contains(seamName, StringComparison.Ordinal), $"{ProductSourceFiles.RelativePathOf(path)} names {seamName}.");
			}
		}
	}

	/// <summary>The shipped values are written out in the entry point: the real terminal predicate and no test budget.</summary>
	[Fact]
	public void TheEntryPointPassesTheShippedValuesAndReadsNothingToChangeASeam()
	{
		string source = ProductSourceFiles.Read(Path.Combine(ProductSourceFiles.ProductRoot, ProductSourceFiles.PROGRAM_FILE));

		Assert.Matches(new Regex(@"AgentLauncherSwitch\s*\.\s*Create\([^;]*testBudget:\s*null\s*\)", RegexOptions.CultureInvariant), source);
		Assert.Contains(_TERMINAL_PREDICATE_TEXT, source, StringComparison.Ordinal);
		Assert.DoesNotContain(_ENVIRONMENT_READ_TEXT, source, StringComparison.Ordinal);
		Assert.DoesNotContain(_PRODUCT_VARIABLE_PREFIX, source, StringComparison.Ordinal);
	}

	[Fact]
	public void NoProductFileReadsAProductVariableOrTheTestVariableOfTheTimingComparison()
	{
		string[] offenders =
		[
			.. ProductSourceFiles.All()
				.Where(path => ProductSourceFiles.Read(path).Contains(_PRODUCT_VARIABLE_PREFIX, StringComparison.Ordinal))
				.Select(ProductSourceFiles.RelativePathOf)
		];

		Assert.Empty(offenders);
	}

	[Fact]
	public void TheTestVariableOfTheTimingComparisonIsReadByThePipelineHarnessAlone()
	{
		string[] files =
		[
			.. ProductSourceFiles.AllTests()
				.Where(path => File.ReadAllText(path).Contains(_TEST_VARIABLE_NAME, StringComparison.Ordinal))
				.Select(path => Path.GetFileName(path))
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal([_PIPELINE_HARNESS_FILE, _THIS_FILE], files);
	}

	/// <summary>The product files, with the folder prefix, that match the pattern outside the file that defines the seam.</summary>
	private static string[] FilesMatching(Regex pattern)
	{
		return
		[
			.. ProductSourceFiles.All()
				.Where(path => Path.GetFileName(path) != _LAUNCHER_SWITCH_FILE)
				.Where(path => pattern.IsMatch(ProductSourceFiles.Read(path)))
				.Select(path => Path.GetFileName(path))
				.Order(StringComparer.Ordinal)
		];
	}
}
