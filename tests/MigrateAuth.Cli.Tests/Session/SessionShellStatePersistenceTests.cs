using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// AT THE PROMPT: A STAGE THAT COMPLETED BUT WHOSE STATE WOULD NOT FIT THE CEILING TELLS THE USER
/// SO, IN WORDS THAT STATE THE CEILING, AND THE SESSION CARRIES ON.
///
/// The stage runner below stands in for a Scan that found more candidates than the ceiling admits;
/// the store is given a ceiling small enough for a handful of paths to pass it. Everything else is
/// the shell as the product builds it.
/// </summary>
public sealed class SessionShellStatePersistenceTests
{
	private const int _CEILING_IN_BYTES = 4_096;
	private const int _CANDIDATE_COUNT = 200;
	private const string _LANGUAGE_NAME = "CSharp";

	[Fact]
	public void AStageWhoseStateWouldPassTheCeilingSaysSoNamingTheCeilingAndTheSessionContinues()
	{
		using TemporaryFolder destination = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		SessionStateStore store = new(DocumentationDestinationFolderPath.FromSuppliedText(destination.Path), _CEILING_IN_BYTES);
		SessionState state = SessionState.Fresh(LanguageCatalog.CurrentVersion, repository.Label);
		StringWriter output = new();
		SessionShell shell = new(
			repository,
			state,
			store,
			new ScanFindingManyCandidatesRunner(),
			new SessionConsole(new StringReader("scan\nexit\n"), output));

		ExitCode exitCode = shell.Run();
		string transcript = output.ToString();

		Assert.Equal(ExitCode.Success, exitCode);
		Assert.Contains(SessionStateStore.DescribeCeiling(_CEILING_IN_BYTES), transcript, StringComparison.Ordinal);
		Assert.DoesNotContain(SessionShell.STATE_NOT_PERSISTED_MESSAGE, transcript, StringComparison.Ordinal);
		Assert.Empty(Directory.GetFiles(destination.Path));
		Assert.True(state.IsCompleted(CommandId.Scan));
	}

	private sealed class ScanFindingManyCandidatesRunner : IWorkflowStageRunner
	{
		public StageRunOutcome Run(CommandId workflowCommand, SessionState state, CancellationToken cancellationToken)
		{
			string[] candidatePaths = [.. Enumerable.Range(0, _CANDIDATE_COUNT).Select(index => $"src/module-{index:D6}/Credentials.cs")];

			state.RecordScanOutcome(
				candidatePaths,
				[.. candidatePaths.Select(_ => _LANGUAGE_NAME)],
				ScanSummary.Empty,
				SkippedItemTallies.Empty,
				ImmutableArray<AssessedLanguage>.Empty);

			return StageRunOutcome.Completed;
		}
	}
}
