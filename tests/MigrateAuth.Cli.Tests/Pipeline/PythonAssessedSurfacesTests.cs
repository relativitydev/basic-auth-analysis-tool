using System.Globalization;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.AcceptanceHarness;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Configuration;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// `Python` IS AN ASSESSED LANGUAGE ON EVERY SURFACE THE BASE REPORTS: the console inventory, the Markdown report, the data file and the declared-coverage text read it as assessed;
/// a configuration file can disable it and then every surface reads `not assessed: disabled by configuration`; Pre-flight Validation still refuses a session when all eight languages are
/// disabled; and the launch help text, which names no language, is what it was before the analyzer was registered. The fixtures are invented placeholders.
/// </summary>
public sealed class PythonAssessedSurfacesTests
{
	private const string _PYTHON_FILE_PATH = "src/client.py";
	private const string _GO_FILE_PATH = "src/client.go";
	private const string _PYTHON_FINDING_CONTENT = "session.auth = (\"user\", \"exampleSecret\")\n";
	private const string _GO_CONTENT = "package main\n";
	private const string _PYTHON_NAME = "Python";
	private const string _DECLARED_LANGUAGE_LINE = "- Python:";
	private const string _DECLARED_ENTRY_LINE_FORMAT = "  - {0}: {1}";
	private const string _NO_LANGUAGE_ENABLED_DOCUMENT = "{ \"enabledLanguages\": [] }";
	private const string _UNKNOWN_NAME_ONLY_DOCUMENT = "{ \"enabledLanguages\": [ \"Klingon\" ] }";
	private const int _LANGUAGE_COUNT = 8;
	private const string _ASSESSED_FIELD = "assessed";
	private const string _NOT_ASSESSED_REASON_FIELD = "notAssessedReason";
	private const string _DISABLED_REASON = "disabledByConfiguration";

	[Fact]
	public void APythonFileIsAssessedOnTheConsoleInventoryTheMarkdownReportAndTheDataFile()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_PYTHON_FILE_PATH, _PYTHON_FINDING_CONTENT);

		harness.RunTheWholeWorkflow();

		string[] consoleRows = [.. harness.Transcript.Split('\n').Where(line => line.Contains(_PYTHON_NAME, StringComparison.Ordinal))];

		Assert.NotEmpty(consoleRows);
		Assert.All(consoleRows, row =>
		{
			Assert.DoesNotContain(ScanStage.UNASSESSED_ROW_SUFFIX.Trim(), row, StringComparison.Ordinal);
			Assert.DoesNotContain(ScanStage.DISABLED_ROW_SUFFIX.Trim(), row, StringComparison.Ordinal);
		});
		Assert.Equal([_PYTHON_FILE_PATH], harness.State.CandidateRelativePaths);
		Assert.Single(harness.State.Findings);

		JsonElement pythonRow = InventoryRow(harness);

		Assert.True(pythonRow.GetProperty(_ASSESSED_FIELD).GetBoolean());
		Assert.False(pythonRow.TryGetProperty(_NOT_ASSESSED_REASON_FIELD, out JsonElement reason) && reason.ValueKind == JsonValueKind.String);
		Assert.EndsWith($"| {MarkdownReportRenderer.ASSESSED_YES} |", MarkdownTableRow(harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport)), StringComparison.Ordinal);
	}

	[Fact]
	public void TheDeclaredCoverageTextOfPythonIsInTheMarkdownReportAndTheDataFileWhenPythonIsAssessed()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_PYTHON_FILE_PATH, _PYTHON_FINDING_CONTENT);

		harness.RunTheWholeWorkflow();

		DeclaredUnsupportedCoverage[] declared = [.. PythonShippedAnalyzer.Instance.LanguageIdentification.DeclaredUnsupportedCoverage];
		string markdown = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		string structuredData = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		Assert.NotEmpty(declared);
		Assert.Contains(_DECLARED_LANGUAGE_LINE, markdown, StringComparison.Ordinal);

		foreach (DeclaredUnsupportedCoverage entry in declared)
		{
			Assert.Contains(string.Format(CultureInfo.InvariantCulture, _DECLARED_ENTRY_LINE_FORMAT, entry.Construct, entry.Reason), markdown, StringComparison.Ordinal);
			Assert.Contains(JsonEncodedText.Encode(entry.Construct).ToString(), structuredData, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void PythonDisabledInTheConfigurationFileReadsNotAssessedDisabledByConfigurationOnEverySurface()
	{
		using PipelineHarness harness = HarnessEnablingEveryLanguageExcept(AssessedLanguage.Python);
		harness.WriteSourceFile(_PYTHON_FILE_PATH, _PYTHON_FINDING_CONTENT);

		harness.RunTheWholeWorkflow();

		Assert.Empty(harness.State.CandidateRelativePaths);
		Assert.Empty(harness.State.Findings);
		Assert.Contains(ScanStage.DISABLED_ROW_SUFFIX, harness.Transcript, StringComparison.Ordinal);

		JsonElement pythonRow = InventoryRow(harness);

		Assert.False(pythonRow.GetProperty(_ASSESSED_FIELD).GetBoolean());
		Assert.Equal(_DISABLED_REASON, pythonRow.GetProperty(_NOT_ASSESSED_REASON_FIELD).GetString());

		string markdown = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.EndsWith($"| {MarkdownReportRenderer.ASSESSED_DISABLED} |", MarkdownTableRow(markdown), StringComparison.Ordinal);
		Assert.Contains(CoverageStatement.DISABLED_BY_CONFIGURATION, markdown, StringComparison.Ordinal);
	}

	[Fact]
	public void GoIsAssessedLikePythonAndItsFileWithoutAnAnchorIsNotACandidate()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_PYTHON_FILE_PATH, _PYTHON_FINDING_CONTENT);
		harness.WriteSourceFile(_GO_FILE_PATH, _GO_CONTENT);

		harness.RunTheWholeWorkflow();

		string[] goRows = [.. harness.Transcript.Split('\n').Where(line => line.Contains(AssessedLanguages.CanonicalName(AssessedLanguage.Go), StringComparison.Ordinal))];

		Assert.NotEmpty(goRows);
		Assert.All(goRows, row => Assert.DoesNotContain(ScanStage.UNASSESSED_ROW_SUFFIX.Trim(), row, StringComparison.Ordinal));
		Assert.Equal([_PYTHON_FILE_PATH], harness.State.CandidateRelativePaths);
	}

	[Theory]
	[InlineData(_NO_LANGUAGE_ENABLED_DOCUMENT)]
	[InlineData(_UNKNOWN_NAME_ONLY_DOCUMENT)]
	public void PreflightStillRefusesTheSessionWhenAllEightLanguagesAreDisabled(string document)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, document);

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		StringWriter output = new();
		PreflightVerdict verdict = PreflightValidation.Run(
			ConfigurationResolver.Resolve(repository, null, profileFolder.Path),
			repository,
			DocumentationDestinationFolderPath.FromSuppliedText(destinationFolder.Path),
			output);

		Assert.Equal(_LANGUAGE_COUNT, AssessedLanguages.AllInDeclaredOrder.Length);
		Assert.False(verdict.PermitsSession);
		Assert.Equal(PreflightOutcome.NoLanguageEnabled, verdict.Outcome);
		Assert.Equal(ExitCode.NoLanguageEnabled, verdict.ExitCode);
		Assert.Contains(PreflightValidation.NO_LANGUAGE_ENABLED_MESSAGE, output.ToString(), StringComparison.Ordinal);
	}

	[Fact]
	public void TheLaunchHelpTextNamesNoLanguageSoRegisteringPythonLeavesItUnchanged()
	{
		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments([CommandLineParser.HELP_OPTION]);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.NotEmpty(outcome.StandardOutput);
		Assert.All(
			AssessedLanguages.AllInDeclaredOrder,
			language => Assert.DoesNotContain(AssessedLanguages.CanonicalName(language), outcome.StandardOutput, StringComparison.Ordinal));
	}

	private static PipelineHarness HarnessEnablingEveryLanguageExcept(AssessedLanguage disabledLanguage)
	{
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder throwawayRepositoryFolder = TemporaryFolder.Create();

		string quotedNames = string.Join(
			", ",
			AssessedLanguages.AllInDeclaredOrder.Where(language => language != disabledLanguage).Select(language => $"\"{AssessedLanguages.CanonicalName(language)}\""));

		ConfigurationFile.WriteInto(profileFolder, $$"""{ "enabledLanguages": [{{quotedNames}}] }""");

		EffectiveConfiguration configuration = ConfigurationResolver.Resolve(
			RepositoryFolderPath.FromSuppliedText(throwawayRepositoryFolder.Path),
			null,
			profileFolder.Path);

		return PipelineHarness.CreateOverTheEmbeddedCatalogWithConfiguration(configuration);
	}

	private static JsonElement InventoryRow(PipelineHarness harness)
	{
		using JsonDocument document = JsonDocument.Parse(harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact));

		return document.RootElement
			.Clone()
			.GetProperty("scanSummary")
			.GetProperty("languages")
			.EnumerateArray()
			.Single(row => row.GetProperty("language").GetString() == _PYTHON_NAME);
	}

	/// <summary>The language's own row of Section 2, found by its leading cell between Section 2's heading and Section 3's.</summary>
	private static string MarkdownTableRow(string markdown)
	{
		int sectionTwoStart = markdown.IndexOf(MarkdownReportRenderer.SCAN_SUMMARY_HEADING, StringComparison.Ordinal);
		int sectionTwoEnd = markdown.IndexOf(MarkdownReportRenderer.SKIPPED_ITEMS_HEADING, StringComparison.Ordinal);
		string rowPrefix = $"| {_PYTHON_NAME} |";

		return markdown[sectionTwoStart..sectionTwoEnd]
			.Split('\n')
			.Single(line => line.StartsWith(rowPrefix, StringComparison.Ordinal));
	}
}
