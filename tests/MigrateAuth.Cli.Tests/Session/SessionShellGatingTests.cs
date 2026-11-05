using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// GATING AND CASCADE AT THE PROMPT: AN OUT-OF-ORDER COMMAND NAMES THE FIRST STAGE NOT YET COMPLETED, AND
/// THE INVALIDATION NOTICE AGREES IN NUMBER WITH THE STAGES IT NAMES.
///
/// The stage runner below completes every stage without doing any work, so what is exercised is the
/// shell's gate and its messages and nothing else.
/// </summary>
public sealed class SessionShellGatingTests
{
	private const string _COMMAND_SEPARATOR = "\n";
	private const string _SINGLE_INVALIDATION_LINE = "Invalidated: analyze. Re-run it before the report reflects the new results.";
	private const string _SEVERAL_INVALIDATION_LINE = "Invalidated: analyze, suggest. Re-run them before the report reflects the new results.";

	[Fact]
	public void ReportInAFreshSessionNamesScan()
	{
		string transcript = RunSession("report");

		Assert.Contains("Run 'scan' first.", transcript, StringComparison.Ordinal);
		Assert.DoesNotContain("Run 'suggest' first.", transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void ReportAfterScanOnlyNamesAnalyze()
	{
		string transcript = RunSession("scan", "report");

		Assert.Contains("Run 'analyze' first.", transcript, StringComparison.Ordinal);
		Assert.DoesNotContain("Run 'suggest' first.", transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void SuggestAfterScanOnlyNamesAnalyze()
	{
		string transcript = RunSession("scan", "suggest");

		Assert.Contains("Run 'analyze' first.", transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void ReportAfterAScanThatInvalidatedTheLaterStagesNamesAnalyze()
	{
		string transcript = RunSession("scan", "analyze", "suggest", "scan", "report");

		Assert.Contains("Run 'analyze' first.", transcript, StringComparison.Ordinal);
		Assert.DoesNotContain("Run 'suggest' first.", transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void ReportAfterEveryEarlierStageCompletedIsNotGated()
	{
		string transcript = RunSession("scan", "analyze", "suggest", "report");

		Assert.DoesNotContain("first.", transcript, StringComparison.Ordinal);
		Assert.Contains("report: completed.", transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void OneInvalidatedStageIsReferredToAsIt()
	{
		string transcript = RunSession("scan", "analyze", "scan");

		Assert.Contains(_SINGLE_INVALIDATION_LINE, transcript, StringComparison.Ordinal);
		Assert.DoesNotContain("Re-run them", transcript, StringComparison.Ordinal);
	}

	[Fact]
	public void SeveralInvalidatedStagesAreReferredToAsThem()
	{
		string transcript = RunSession("scan", "analyze", "suggest", "scan");

		Assert.Contains(_SEVERAL_INVALIDATION_LINE, transcript, StringComparison.Ordinal);
		Assert.DoesNotContain("Re-run it", transcript, StringComparison.Ordinal);
	}

	private static string RunSession(params string[] commands)
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		SessionStateStore store = new(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path));
		SessionState state = SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label);
		StringWriter output = new();
		string input = string.Join(_COMMAND_SEPARATOR, [.. commands, CommandRegistry.Describe(CommandId.Exit).Name, string.Empty]);
		SessionShell shell = new(
			repository,
			state,
			store,
			new CompletingStageRunner(),
			new SessionConsole(new StringReader(input), output));

		shell.Run();

		return output.ToString();
	}

	private sealed class CompletingStageRunner : IWorkflowStageRunner
	{
		public StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken)
		{
			return StageRunOutcome.Completed;
		}
	}
}
