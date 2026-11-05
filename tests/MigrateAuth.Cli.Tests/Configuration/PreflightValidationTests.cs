using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Configuration;

/// <summary>
/// Pre-flight Validation: two checks with two deliberately different failure shapes,
/// plus the early containment comparison.
///
/// The asymmetry is what most of these assert. A malformed file DEGRADES because the built-in
/// defaults are a safe answer and a broken configuration must not cost the user the ability to
/// run. A configuration enabling no language REFUSES because there is no safe answer: an empty
/// scan produces a report of zero findings, and that is a claim about the client's code which is
/// indistinguishable from a clean repository.
/// </summary>
public sealed class PreflightValidationTests
{
	private const string _MALFORMED_DOCUMENT = "{ this is not json";
	private const string _NO_LANGUAGES_DOCUMENT = "{ \"enabledLanguages\": [] }";
	private const string _OUTPUT_FOLDER_NAME = "output";
	private const string _FOUND_MARKER = "was found";
	private const string _STRUCTURE_MARKER = "expected structure";

	[Fact]
	public void AMalformedConfigurationFileSaysItWasFoundAndLetsTheSessionStart()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, _MALFORMED_DOCUMENT);

		StringWriter output = new();
		PreflightVerdict verdict = Run(repositoryFolder, profileFolder, destinationFolder, output);

		Assert.True(verdict.PermitsSession);
		Assert.Equal(ExitCode.Success, verdict.ExitCode);
		Assert.Contains(_FOUND_MARKER, output.ToString(), StringComparison.Ordinal);
		Assert.Contains(_STRUCTURE_MARKER, output.ToString(), StringComparison.Ordinal);
		Assert.Contains(
			PreflightValidation.REPOSITORY_ROOT_LOCATION_LABEL,
			output.ToString(),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// Found and impossible to open is not the same mistake as found and malformed. Telling a user
	/// their file is malformed when it is merely locked sends them to edit a correct file.
	/// </summary>
	[Fact]
	public void AnUnreadableConfigurationFileDegradesAndSaysSomethingElse()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, "{}");

		StringWriter output = new();
		PreflightVerdict verdict;

		using (FileStream heldOpen = new(
			ConfigurationFile.PathIn(repositoryFolder),
			FileMode.Open,
			FileAccess.Read,
			FileShare.None))
		{
			verdict = Run(repositoryFolder, profileFolder, destinationFolder, output);
		}

		Assert.True(verdict.PermitsSession);
		Assert.Contains("could not be read", output.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain(_STRUCTURE_MARKER, output.ToString(), StringComparison.Ordinal);
	}

	/// <summary>
	/// a missing file is not an error at any level, so nothing is said. Nothing was
	/// found to be wrong, and a message about it would train the user to ignore messages.
	/// </summary>
	[Fact]
	public void NoConfigurationFileAnywhereSaysNothingAtAll()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		StringWriter output = new();
		PreflightVerdict verdict = Run(repositoryFolder, profileFolder, destinationFolder, output);

		Assert.True(verdict.PermitsSession);
		Assert.Equal(string.Empty, output.ToString());
	}

	[Fact]
	public void AConfigurationEnablingNoLanguageRefusesToStartTheSession()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, _NO_LANGUAGES_DOCUMENT);

		StringWriter output = new();
		PreflightVerdict verdict = Run(repositoryFolder, profileFolder, destinationFolder, output);

		Assert.False(verdict.PermitsSession);
		Assert.Equal(PreflightOutcome.NoLanguageEnabled, verdict.Outcome);
		Assert.Equal(ExitCode.NoLanguageEnabled, verdict.ExitCode);
		Assert.Contains(
			PreflightValidation.NO_LANGUAGE_ENABLED_MESSAGE,
			output.ToString(),
			StringComparison.Ordinal);
		Assert.Contains(
			ConfigurationResolver.CONFIGURATION_FILE_NAME,
			output.ToString(),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The refusal's exit code is carried in one constant, and this test pins its value.
	/// </summary>
	[Fact]
	public void TheNoLanguageRefusalCarriesItsOwnExitCode()
	{
		Assert.Equal(6, (int)ExitCode.NoLanguageEnabled);
	}

	/// <summary>
	/// With the default destination resolved to the working directory, running the tool
	/// from inside the repository under audit makes the two the same folder - and this is where
	/// that is discovered, in a second, rather than after the whole run.
	/// </summary>
	[Fact]
	public void ADestinationThatIsTheRepositoryIsRefusedAtPreflight()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		StringWriter output = new();
		PreflightVerdict verdict = Run(repositoryFolder, profileFolder, repositoryFolder, output);

		Assert.False(verdict.PermitsSession);
		Assert.Equal(PreflightOutcome.WriteBoundaryRefused, verdict.Outcome);
		Assert.Equal(WriteBoundary.REFUSAL_EXIT_CODE, verdict.ExitCode);
		Assert.Contains(
			WriteBoundary.REFUSAL_REMEDY_MESSAGE,
			output.ToString(),
			StringComparison.Ordinal);
	}

	[Fact]
	public void ADestinationInsideTheRepositoryIsRefusedAtPreflight()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		string nestedDestination = Path.Combine(repositoryFolder.Path, _OUTPUT_FOLDER_NAME);
		StringWriter output = new();

		PreflightVerdict verdict = PreflightValidation.Run(
			ConfigurationResolver.Resolve(RepositoryOf(repositoryFolder), null, profileFolder.Path),
			RepositoryOf(repositoryFolder),
			DocumentationDestinationFolderPath.FromSuppliedText(nestedDestination),
			output);

		Assert.False(verdict.PermitsSession);
		Assert.Equal(PreflightOutcome.WriteBoundaryRefused, verdict.Outcome);
		Assert.False(Directory.Exists(nestedDestination));
	}

	/// <summary>
	/// One implementation, two call sites. The message the early check writes is the write
	/// boundary's own verdict, not a second wording that could drift from it.
	/// </summary>
	[Fact]
	public void ThePreflightRefusalIsTheWriteBoundarysOwnVerdict()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		StringWriter output = new();

		Run(repositoryFolder, profileFolder, repositoryFolder, output);

		WriteBoundaryVerdict boundaryVerdict = WriteBoundary.Evaluate(
			RepositoryOf(repositoryFolder),
			DocumentationDestinationFolderPath.FromSuppliedText(repositoryFolder.Path));

		Assert.Contains(boundaryVerdict.RefusalMessage, output.ToString(), StringComparison.Ordinal);
	}

	[Fact]
	public void ANarrowingAndAnIgnoredRepositoryFieldAreAnnouncedRatherThanQuietlyApplied()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(
			repositoryFolder,
			"{ \"enabledLanguages\": [ \"Go\" ], \"minimumConfidenceLevel\": \"High\", \"outputPath\": \"/elsewhere\" }");

		StringWriter output = new();
		PreflightVerdict verdict = Run(repositoryFolder, profileFolder, destinationFolder, output);

		Assert.True(verdict.PermitsSession);
		Assert.Contains("sets the enabled languages to Go; it disables CSharp", output.ToString(), StringComparison.Ordinal);
		Assert.Contains(
			"raises the minimum confidence level from Low to High; Medium and Low findings are not shown.",
			output.ToString(),
			StringComparison.Ordinal);
		Assert.Contains(
			PreflightValidation.REPOSITORY_OUTPUT_PATH_IGNORED_MESSAGE,
			output.ToString(),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// No absolute path appears in anything this product emits, and a pre-flight message is
	/// no exception. The one place a path could leak here is the output path a repository wrote
	/// into its own configuration file, which is why its value is never carried.
	/// </summary>
	[Fact]
	public void NoPreflightMessageCarriesAPath()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, "{ \"outputPath\": \"/a/path/nobody/should/see\" }");

		StringWriter output = new();

		Run(repositoryFolder, profileFolder, destinationFolder, output);

		Assert.DoesNotContain("/a/path/nobody/should/see", output.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain(repositoryFolder.Path, output.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain(destinationFolder.Path, output.ToString(), StringComparison.Ordinal);
	}

	/// <summary>
	/// The checks run once and are never repeated inside the session. The mechanism is
	/// that the effective configuration is a value rather than a file - so pre-flight reaches the
	/// same verdict after the files it was read from have been deleted, which is what "session
	/// scoped constant" means in practice.
	/// </summary>
	[Fact]
	public void TheEffectiveConfigurationOutlivesTheFilesItWasReadFrom()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, _NO_LANGUAGES_DOCUMENT);

		EffectiveConfiguration configuration = ConfigurationResolver.Resolve(
			RepositoryOf(repositoryFolder),
			null,
			profileFolder.Path);

		File.Delete(ConfigurationFile.PathIn(profileFolder));

		StringWriter firstOutput = new();
		StringWriter secondOutput = new();

		PreflightVerdict firstVerdict = PreflightValidation.Run(
			configuration,
			RepositoryOf(repositoryFolder),
			DestinationOf(destinationFolder),
			firstOutput);

		PreflightVerdict secondVerdict = PreflightValidation.Run(
			configuration,
			RepositoryOf(repositoryFolder),
			DestinationOf(destinationFolder),
			secondOutput);

		Assert.Equal(firstVerdict, secondVerdict);
		Assert.Equal(PreflightOutcome.NoLanguageEnabled, secondVerdict.Outcome);
		Assert.Equal(firstOutput.ToString(), secondOutput.ToString());
	}

	/// <summary>
	/// Pre-flight is a step and not a stage. It is therefore absent from the command
	/// registry - not gated, not cascade-invalidated, not persisted as a completed stage, and not
	/// reachable from the prompt. A session either passed it or does not exist.
	/// </summary>
	[Fact]
	public void PreflightIsNotAStageAndIsNotReachableFromThePrompt()
	{
		string[] workflowCommandNames =
		[
			.. CommandRegistry.WorkflowCommandsInRunOrder.Select(command => command.Name)
		];

		Assert.Equal(["scan", "analyze", "suggest", "report", "ai-assist"], workflowCommandNames);
		Assert.False(CommandRegistry.TryResolve("preflight", out _));
	}

	/// <summary>
	/// An invalid default flow in the user profile is a warning naming that key, not a malformed
	/// file: the session proceeds on the built-in default flow, and the profile's output path still
	/// decides where the report is written.
	/// </summary>
	[Fact]
	public void AnInvalidUserProfileDefaultFlowIsAWarningAndTheProfileOutputPathStillApplies()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"defaultFlow\": \"Implicit\", \"outputPath\": \"reports\" }");

		StringWriter output = new();
		EffectiveConfiguration configuration =
			ConfigurationResolver.Resolve(RepositoryOf(repositoryFolder), null, profileFolder.Path);
		PreflightVerdict verdict = PreflightValidation.Run(
			configuration,
			RepositoryOf(repositoryFolder),
			DestinationOf(destinationFolder),
			output);

		Assert.True(verdict.PermitsSession);
		Assert.DoesNotContain(_STRUCTURE_MARKER, output.ToString(), StringComparison.Ordinal);
		Assert.Contains("defaultFlow is \"Implicit\"", output.ToString(), StringComparison.Ordinal);
		Assert.Contains(
			PreflightValidation.USER_PROFILE_LOCATION_LABEL,
			output.ToString(),
			StringComparison.Ordinal);
		Assert.Equal(CredentialFlow.ClientCredentials, configuration.DefaultFlow);
		Assert.Equal(ConfigurationSource.BuiltInDefault, configuration.Provenance.DefaultFlowOrigin);
		Assert.Equal("reports", configuration.OutputPathText);
		Assert.Equal(ConfigurationSource.UserProfileFile, configuration.Provenance.OutputPathOrigin);
	}

	/// <summary>
	/// At Pre-flight, a default flow in the repository's file is announced as ignored, and
	/// neither its value nor an unknown one costs the session anything.
	/// </summary>
	[Theory]
	[InlineData("AuthorizationCode")]
	[InlineData("Implicit")]
	public void ARepositoryDefaultFlowIsAnnouncedAsIgnoredWhateverItSays(string declaredValue)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, $"{{ \"defaultFlow\": \"{declaredValue}\" }}");

		StringWriter output = new();
		PreflightVerdict verdict = Run(repositoryFolder, profileFolder, destinationFolder, output);

		Assert.True(verdict.PermitsSession);
		Assert.Contains(
			PreflightValidation.REPOSITORY_DEFAULT_FLOW_IGNORED_MESSAGE,
			output.ToString(),
			StringComparison.Ordinal);
		Assert.DoesNotContain(declaredValue, output.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain(_STRUCTURE_MARKER, output.ToString(), StringComparison.Ordinal);
	}

	private static PreflightVerdict Run(
		TemporaryFolder repositoryFolder,
		TemporaryFolder profileFolder,
		TemporaryFolder destinationFolder,
		TextWriter outputWriter) =>
		PreflightValidation.Run(
			ConfigurationResolver.Resolve(RepositoryOf(repositoryFolder), null, profileFolder.Path),
			RepositoryOf(repositoryFolder),
			DestinationOf(destinationFolder),
			outputWriter);

	private static RepositoryFolderPath RepositoryOf(TemporaryFolder folder) =>
		RepositoryFolderPath.FromSuppliedText(folder.Path);

	private static DocumentationDestinationFolderPath DestinationOf(TemporaryFolder folder) =>
		DocumentationDestinationFolderPath.FromSuppliedText(folder.Path);
}
