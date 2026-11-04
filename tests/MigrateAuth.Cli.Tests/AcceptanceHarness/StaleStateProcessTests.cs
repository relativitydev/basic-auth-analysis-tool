using System.Globalization;

using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THROUGH TWO REAL PROCESSES: A RESUMED SESSION NEITHER DETECTS NOR WARNS
/// ABOUT A REPOSITORY THAT CHANGED AFTER <c>scan</c>, AND ITS RESULTS DESCRIBE THE INVENTORY AS
/// SCAN LEFT IT.
///
/// Analysis consumes the candidate set the Scan stage persisted rather than walking the tree again,
/// so a candidate file added after <c>scan</c> is simply not in it. The first process scans a
/// repository holding one candidate file and exits. The repository is then modified from outside the
/// tool: a second file carrying the same construct is added. The second process, launched against
/// the same destination, runs <c>analyze</c> with no <c>scan</c> before it.
/// </summary>
public sealed class StaleStateProcessTests
{
	private const string _ORIGINAL_FILE_NAME = "Credentials.cs";

	private const string _ADDED_FILE_NAME = "AddedAfterScan.cs";

	private const string _CANDIDATE_FILE_CONTENT =
		"public sealed class CredentialFactory\n{\n\tprivate readonly string userName;\n\tprivate readonly string password;\n\n\tprivate void Build()\n\t{\n\t\tCredentials credentials = new UsernamePasswordCredentials(userName, password);\n\t}\n}\n";

	private const int _INVENTORIED_CANDIDATE_COUNT = 1;

	private static readonly string[] _changeOrStalenessWording =
	[
		"changed",
		"modified",
		"stale",
		"outdated",
		"out of date",
		"rescan"
	];

	[Fact]
	public void AnalyzeAfterAResumeReflectsTheInventoryScanRecordedAndSaysNothingAboutTheChange()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		repository.WriteText(_ORIGINAL_FILE_NAME, _CANDIDATE_FILE_CONTENT);

		CliProcessOutcome scanProcess = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			[
				CommandRegistry.Describe(CommandId.Scan).Name,
				CommandRegistry.Describe(CommandId.Exit).Name
			]);

		Assert.Equal((int)ExitCode.Success, scanProcess.ExitCode);

		// The repository is modified outside the tool, between the two processes.
		repository.WriteText(_ADDED_FILE_NAME, _CANDIDATE_FILE_CONTENT);

		CliProcessOutcome analyzeProcess = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			[
				CommandRegistry.Describe(CommandId.Analyze).Name,
				CommandRegistry.Describe(CommandId.Exit).Name
			]);

		AssertNoChangeWasDetectedOrReported(analyzeProcess);
		AssertScanWasNotRequiredAgain(analyzeProcess);
		AssertTheResultsDescribeThePreModificationInventory(analyzeProcess);
	}

	private static void AssertNoChangeWasDetectedOrReported(CliProcessOutcome analyzeProcess)
	{
		Assert.Equal((int)ExitCode.Success, analyzeProcess.ExitCode);
		Assert.Empty(analyzeProcess.StandardError);

		foreach (string wording in _changeOrStalenessWording)
		{
			Assert.DoesNotContain(wording, analyzeProcess.StandardOutput, StringComparison.OrdinalIgnoreCase);
		}
	}

	private static void AssertScanWasNotRequiredAgain(CliProcessOutcome analyzeProcess)
	{
		string analyzeStageName = CommandRegistry.Describe(CommandId.Analyze).Name;

		Assert.DoesNotContain(
			CommandRegistry.FormatGatingMessage(CommandId.Scan),
			analyzeProcess.StandardOutput,
			StringComparison.Ordinal);

		Assert.Contains(
			string.Format(CultureInfo.InvariantCulture, SessionShell.STAGE_COMPLETED_MESSAGE_FORMAT, analyzeStageName),
			analyzeProcess.StandardOutput,
			StringComparison.Ordinal);
	}

	private static void AssertTheResultsDescribeThePreModificationInventory(CliProcessOutcome analyzeProcess)
	{
		Assert.Contains(
			string.Format(
				CultureInfo.InvariantCulture,
				AnalyzeStage.ANALYSIS_STARTED_MESSAGE_FORMAT,
				_INVENTORIED_CANDIDATE_COUNT),
			analyzeProcess.StandardOutput,
			StringComparison.Ordinal);

		Assert.Contains(
			string.Format(
				CultureInfo.InvariantCulture,
				AnalyzeStage.FINDINGS_COUNT_MESSAGE_FORMAT,
				_INVENTORIED_CANDIDATE_COUNT),
			analyzeProcess.StandardOutput,
			StringComparison.Ordinal);

		Assert.DoesNotContain(_ADDED_FILE_NAME, analyzeProcess.StandardOutput, StringComparison.Ordinal);
	}
}
