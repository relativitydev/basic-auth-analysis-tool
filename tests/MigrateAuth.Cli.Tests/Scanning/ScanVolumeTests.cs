using MigrateAuth.Cli.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// THE VOLUME PROOF FOR THE TERM THAT GROWS WITH THE REPOSITORY.
///
/// The candidate set is the one thing in this design that grows with the repository
/// and it is deliberately NOT capped: capping it means silently declining to analyze
/// files, which is a worse failure than the memory it saves. A claim like that is only worth
/// anything if the uncapped case has been run, so this builds a tree in which EVERY file carries
/// an anchor and reconciles the candidate count against the number of files created.
///
/// The worst case is this one: every file, 100 %.
///
/// THE MEASUREMENT lives in ScanVolumeTimingTests, which prints this machine's number. Its assertion is against the required
/// floor of 5,000 files a minute and never against a reference figure, because a test that fails
/// when a laptop is busy is a test that gets deleted.
/// </summary>
public sealed class ScanVolumeTests
{
	internal const int FOLDER_COUNT = 60;
	internal const int FILES_PER_FOLDER = 50;
	internal const int TREE_FILE_COUNT = FOLDER_COUNT * FILES_PER_FOLDER;
	private const string _CANDIDATE_CONTENT = "var credentials = NewExemplarCredentials(user, secret);\n";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";
	private const string _REPOSITORY_ROOT_LABEL = "volume-repository";

	/// <summary>
	/// The whole stage at volume, through the state it writes to. Half a million paths is the
	/// worst case this term is declared against; three thousand is what a test can create in a
	/// second, and what it establishes is that nothing between the walk and the state loses or
	/// duplicates one.
	/// </summary>
	[Fact]
	public void TheStageCarriesEveryCandidateThroughToTheSessionState()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();

		CreateTree(harness, everyFileCarriesAnAnchor: true);

		SessionState state = SessionState.Fresh(harness.Catalog.Version, _REPOSITORY_ROOT_LABEL);
		StageRunOutcome outcome = harness.BuildStage().Run(state, CancellationToken.None);

		Assert.Equal(StageRunOutcome.Completed, outcome);
		Assert.Equal(TREE_FILE_COUNT, state.CandidateRelativePaths.Count);
	}

	internal static void CreateTree(ScanHarness harness, bool everyFileCarriesAnAnchor)
	{
		string content = everyFileCarriesAnAnchor ? _CANDIDATE_CONTENT : _ORDINARY_CONTENT;

		for (int folder = 0; folder < FOLDER_COUNT; folder++)
		{
			for (int file = 0; file < FILES_PER_FOLDER; file++)
			{
				harness.Fixture.WriteText(
					Path.Combine($"module-{folder:D3}", "source", $"file-{file:D3}.ex1"),
					content);
			}
		}
	}
}
