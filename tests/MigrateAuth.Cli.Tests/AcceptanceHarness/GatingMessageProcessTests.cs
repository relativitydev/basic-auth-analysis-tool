using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THROUGH THE REAL PROCESS: A STAGE ENTERED OUT OF ORDER PRINTS THE GATING
/// LITERAL VERBATIM, NAMING THE MISSING STAGE.
/// </summary>
public sealed class GatingMessageProcessTests
{
	private const string _EXPECTED_GATING_MESSAGE = "Run 'analyze' first.";

	[Fact]
	public void SuggestBeforeAnalyzePrintsTheGatingLiteralNamingAnalyze()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			[
				CommandRegistry.Describe(CommandId.Scan).Name,
				CommandRegistry.Describe(CommandId.Suggest).Name,
				CommandRegistry.Describe(CommandId.Exit).Name
			]);

		Assert.Contains(_EXPECTED_GATING_MESSAGE, outcome.StandardOutput, StringComparison.Ordinal);
	}
}
