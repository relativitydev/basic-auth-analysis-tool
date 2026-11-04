using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE CONSOLE NOTE THAT A THRESHOLD HIDES FINDINGS NAMES THE REPOSITORY'S OWN FILE WHEN THAT IS WHERE
/// THE THRESHOLD CAME FROM, on both stages that say it: <c>analyze</c> above its table and <c>report</c>
/// before it writes.
///
/// A threshold from the user profile or the built-in default is a setting the person running the tool
/// chose or can find; one from the repository was chosen by whoever owns the repository, and a person
/// reading "showing 1 of 2" needs to know that.
/// </summary>
public sealed class ThresholdSourceConsoleTests
{
	private const string _CREDENTIAL_FILE_NAME = "Credential.cs";
	private const string _LOW_FILE_NAME = "Legacy.cs";

	private static readonly string _credentialContent =
		$"class A\n{{\n\tvoid B() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, credential);\n}}\n";

	private static readonly string _lowContent =
		$"class A\n{{\n\tvoid B() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();\n}}\n";

	[Fact]
	public void TheAnalyzeNoteNamesTheRepositoryWhenItSetTheThreshold()
	{
		string output = RenderAnalyzeNote(thresholdSetByRepository: true);

		Assert.Contains(AnalyzeConsoleReport.THRESHOLD_SET_BY_REPOSITORY_SUFFIX, output, StringComparison.Ordinal);
		Assert.Contains("Showing 1 of 2 findings at minimum confidence level High.", output, StringComparison.Ordinal);
	}

	[Fact]
	public void TheAnalyzeNoteSaysNothingOfTheRepositoryWhenSomethingElseSetTheThreshold()
	{
		string output = RenderAnalyzeNote(thresholdSetByRepository: false);

		Assert.DoesNotContain(AnalyzeConsoleReport.THRESHOLD_SET_BY_REPOSITORY_SUFFIX, output, StringComparison.Ordinal);
		Assert.Contains("Showing 1 of 2 findings", output, StringComparison.Ordinal);
	}

	[Fact]
	public void TheAnalyzeNoteSaysNothingOfTheRepositoryWhenNothingIsHidden()
	{
		using StringWriter writer = new();
		ImmutableArray<Finding> findings = [ReportHarness.FindingAt(1, _CREDENTIAL_FILE_NAME, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)];

		AnalyzeConsoleReport.Render(
			writer,
			findings,
			ReportHarness.CatalogWithCSharpOnly(),
			ConfidenceLevel.High,
			thresholdSetByRepository: true);

		Assert.DoesNotContain(AnalyzeConsoleReport.THRESHOLD_SET_BY_REPOSITORY_SUFFIX, writer.ToString(), StringComparison.Ordinal);
	}

	[Fact]
	public void TheReportStageNamesTheRepositoryWhenItSetTheThresholdAndItHidesFindings()
	{
		using PipelineHarness harness = HarnessWithThresholdFrom(ConfigurationSource.RepositoryFile);

		RunTheWorkflow(harness);

		Assert.Contains(
			string.Format(CultureInfo.InvariantCulture, ReportStage.REPORTING_MESSAGE_FORMAT, 1, 2, "High")
				+ AnalyzeConsoleReport.THRESHOLD_SET_BY_REPOSITORY_SUFFIX,
			harness.Transcript,
			StringComparison.Ordinal);
		Assert.Contains(AnalyzeConsoleReport.THRESHOLD_SET_BY_REPOSITORY_SUFFIX, TheAnalyzeLine(harness), StringComparison.Ordinal);
	}

	[Fact]
	public void TheReportStageDoesNotNameTheRepositoryWhenTheThresholdCameFromElsewhere()
	{
		using PipelineHarness harness = HarnessWithThresholdFrom(ConfigurationSource.UserProfileFile);

		RunTheWorkflow(harness);

		Assert.DoesNotContain(AnalyzeConsoleReport.THRESHOLD_SET_BY_REPOSITORY_SUFFIX, harness.Transcript, StringComparison.Ordinal);
	}

	private static string RenderAnalyzeNote(bool thresholdSetByRepository)
	{
		using StringWriter writer = new();
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, _CREDENTIAL_FILE_NAME, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(2, _LOW_FILE_NAME, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)
		];

		AnalyzeConsoleReport.Render(
			writer,
			findings,
			ReportHarness.CatalogWithCSharpOnly(),
			ConfidenceLevel.High,
			thresholdSetByRepository);

		return writer.ToString();
	}

	private static PipelineHarness HarnessWithThresholdFrom(ConfigurationSource origin)
	{
		EffectiveConfiguration baseline = ReportHarness.ConfigurationWith(ConfidenceLevel.High);
		EffectiveConfiguration configuration = baseline with
		{
			Provenance = baseline.Provenance with { MinimumConfidenceLevelOrigin = origin }
		};
		PipelineHarness harness = PipelineHarness.CreateWithConfiguration(configuration);

		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		harness.WriteSourceFile(_LOW_FILE_NAME, _lowContent);

		return harness;
	}

	private static void RunTheWorkflow(PipelineHarness harness)
	{
		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		harness.RunAndCommit(CommandId.Suggest);
		harness.RunAndCommit(CommandId.Report);
	}

	private static string TheAnalyzeLine(PipelineHarness harness)
	{
		return harness.Transcript
			.Split('\n')
			.Single(line => line.StartsWith("Showing ", StringComparison.Ordinal));
	}
}
