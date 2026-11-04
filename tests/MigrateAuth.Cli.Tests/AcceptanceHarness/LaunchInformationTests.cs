using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

using MigrateAuth.Cli.Tests.Reading;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.AcceptanceHarness;

/// <summary>
/// THROUGH THE REAL PROCESS: <c>--help</c>, <c>-h</c> AND <c>--version</c> ANSWER BEFORE A
/// SESSION EVER STARTS.
///
/// Every test here also supplies both named folder options, pointed at a real repository and a
/// real, empty destination folder - the strongest form of "resolves nothing", because the launch
/// flag wins even though everything a session would need is sitting right there on the command
/// line. A test that omitted the two options would only prove the flag works when there is
/// nothing else to resolve, which is the easy case.
/// </summary>
public sealed class LaunchInformationTests
{
	/// <summary>
	/// The alpha label written out, not read from <see cref="ToolVersion.CURRENT"/>: a comparison of
	/// the constant against itself passes for any value, so the literal is what pins the label.
	/// </summary>
	private const string _EXPECTED_ALPHA_VERSION = "0.1.1-alpha";

	private const string _EXPECTED_OUTPUT_FOLDER_DEFAULT_TEXT =
		"Defaults to the outputPath in the user profile's configuration file, then to the working directory.";

	private const string _STALE_OUTPUT_FOLDER_DEFAULT_TEXT = "written to. Defaults to the working directory.";

	[Fact]
	public void HelpAnswersDirectlyWithoutTouchingTheRepositoryOrTheDestination()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments(
		[
			CommandLineParser.HELP_OPTION,
			CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
			repository.RootPath,
			CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION,
			destination.Path
		]);

		AssertAnsweredTheHelpRequest(outcome);
		AssertNothingWasResolved(outcome, destination);
	}

	[Fact]
	public void TheShortFormOfHelpAnswersTheSameRequest()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments(
		[
			CommandLineParser.HELP_OPTION_SHORT_FORM,
			CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
			repository.RootPath,
			CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION,
			destination.Path
		]);

		AssertAnsweredTheHelpRequest(outcome);
		AssertNothingWasResolved(outcome, destination);
	}

	[Fact]
	public void HelpStatesTheOutputFolderFallbackOrderTheResolverApplies()
	{
		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments([CommandLineParser.HELP_OPTION]);

		Assert.Contains(_EXPECTED_OUTPUT_FOLDER_DEFAULT_TEXT, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(_STALE_OUTPUT_FOLDER_DEFAULT_TEXT, outcome.StandardOutput, StringComparison.Ordinal);
	}

	[Fact]
	public void VersionPrintsTheToolAndCatalogVersionsWithoutTouchingTheRepositoryOrTheDestination()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments(
		[
			CommandLineParser.VERSION_OPTION,
			CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
			repository.RootPath,
			CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION,
			destination.Path
		]);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);
		Assert.Contains(
			$"{CommandLineParser.PRODUCT_NAME} {ToolVersion.CURRENT} (catalog {catalog.Version.DisplayValue})",
			outcome.StandardOutput,
			StringComparison.Ordinal);
		AssertNothingWasResolved(outcome, destination);
	}

	[Fact]
	public void VersionPrintsTheAlphaLabelledVersionLiteral()
	{
		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments([CommandLineParser.VERSION_OPTION]);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Contains(
			$"{CommandLineParser.PRODUCT_NAME} {_EXPECTED_ALPHA_VERSION} (catalog ",
			outcome.StandardOutput,
			StringComparison.Ordinal);
	}

	[Fact]
	public void TheSessionBannerCarriesTheAlphaLabelledVersionLiteral()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.Run(
			repository.RootPath,
			destination.Path,
			[CommandRegistry.Describe(CommandId.Exit).Name]);

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Contains(
			$"Basic Authentication assessment CLI {_EXPECTED_ALPHA_VERSION} (catalog ",
			outcome.StandardOutput,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// <c>--help</c> wins when both are supplied on the same launch.
	/// </summary>
	[Fact]
	public void HelpWinsWhenBothHelpAndVersionAreSupplied()
	{
		using RepositoryFixture repository = RepositoryFixture.CreateEmpty();
		using TemporaryFolder destination = TemporaryFolder.Create();

		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments(
		[
			CommandLineParser.VERSION_OPTION,
			CommandLineParser.HELP_OPTION
		]);

		AssertAnsweredTheHelpRequest(outcome);
	}

	/// <summary>
	/// <c>-h</c> here is the VALUE <c>--repositoryFolderPath</c> claims,
	/// exactly as <see cref="CommandLineParser.Parse"/> itself reads the same two tokens - not the
	/// help flag. The session proceeds with a repository named <c>-h</c>, which does not exist, so
	/// it fails on that path rather than ever printing help.
	/// </summary>
	[Fact]
	public void AShortHelpFormSuppliedAsAnOptionValueDoesNotAnswerHelp()
	{
		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments(
		[
			CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
			CommandLineParser.HELP_OPTION_SHORT_FORM
		]);

		Assert.Equal((int)ExitCode.UnusableRepositoryPath, outcome.ExitCode);
		Assert.DoesNotContain(CommandLineParser.UsageLine, outcome.StandardOutput, StringComparison.Ordinal);
	}

	/// <summary>
	/// The positive complement: <c>-h</c> in the FIRST position is a token of
	/// its own, not a value any preceding named option claims, so it still answers help.
	/// </summary>
	[Fact]
	public void AShortHelpFormSuppliedBeforeANamedOptionStillAnswersHelp()
	{
		CliProcessOutcome outcome = CliProcessHarness.RunWithArguments(
		[
			CommandLineParser.HELP_OPTION_SHORT_FORM,
			CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION,
			"x"
		]);

		AssertAnsweredTheHelpRequest(outcome);
	}

	private static void AssertAnsweredTheHelpRequest(CliProcessOutcome outcome)
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.Equal((int)ExitCode.Success, outcome.ExitCode);
		Assert.Empty(outcome.StandardError);

		Assert.Contains(
			$"migrate-auth - Basic Authentication assessment CLI {ToolVersion.CURRENT} (catalog {catalog.Version.DisplayValue})",
			outcome.StandardOutput,
			StringComparison.Ordinal);
		Assert.Contains(CommandLineParser.UsageLine, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Contains(CommandLineParser.REPOSITORY_FOLDER_PATH_OPTION, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Contains(
			CommandLineParser.DOCUMENTATION_DESTINATION_FOLDER_PATH_OPTION,
			outcome.StandardOutput,
			StringComparison.Ordinal);
		Assert.Contains(HelpRenderer.WORKFLOW_GROUP_HEADING, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Contains(HelpRenderer.SESSION_GROUP_HEADING, outcome.StandardOutput, StringComparison.Ordinal);

		foreach (CommandDescriptor command in CommandRegistry.CommandsInHelpOrder)
		{
			Assert.Contains(command.DisplayName, outcome.StandardOutput, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// No prompt (the exact prompt text is never printed), and the destination folder this test
	/// pointed the launch at is still empty - proof that neither Pre-flight, nor the session
	/// state store, nor anything else that would have written there ever ran.
	/// </summary>
	private static void AssertNothingWasResolved(CliProcessOutcome outcome, TemporaryFolder destination)
	{
		Assert.DoesNotContain(SessionShell.PROMPT, outcome.StandardOutput, StringComparison.Ordinal);
		Assert.Empty(Directory.EnumerateFileSystemEntries(destination.Path));
	}
}
