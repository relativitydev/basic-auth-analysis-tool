using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// The console names the output folder as the user supplied it and never by its
/// canonical path, and a leading <c>~</c> in the profile file's output path is expanded through an injected home
/// provider. Every test here is platform-neutral, so the same file runs on Windows and on Linux, and none of them
/// changes the process environment or the working folder.
/// </summary>
public sealed class DestinationConsoleTextTests
{
	private const string _OUTPUTS_FOLDER_NAME = "Outputs";
	private const string _TILDE_FOLDER_NAME = "~";
	private const string _PROFILE_FILE_NAME = ConfigurationResolver.CONFIGURATION_FILE_NAME;
	private const string _PROFILE_WITH_FORWARD_SLASH_TILDE = "{ \"outputPath\": \"~/Outputs\" }";
	private const string _PROFILE_WITH_BACKSLASH_TILDE = "{ \"outputPath\": \"~\\\\Outputs\" }";
	private const string _ESCAPE_CHARACTER_TEXT = "Out\u001b[31mputs";

	[Theory]
	[InlineData("Outputs", "./Outputs")]
	[InlineData("Outputs/inner", "./Outputs/inner")]
	[InlineData("Outputs\\inner", "./Outputs\\inner")]
	[InlineData("./Outputs", "./Outputs")]
	[InlineData(".\\Outputs", ".\\Outputs")]
	[InlineData("../Outputs", "../Outputs")]
	[InlineData("..\\Outputs", "..\\Outputs")]
	[InlineData(".", ".")]
	[InlineData("..", "..")]
	public void ARelativePathIsShownRelativeToTheWorkingFolderInTheUsersOwnSeparators(string suppliedText, string expectedText)
	{
		Assert.Equal(expectedText, DocumentationDestinationFolderPath.DeriveConsoleDisplayText(suppliedText));
	}

	[Fact]
	public void AnAbsolutePathIsShownAsSupplied()
	{
		string absolutePath = Path.Combine(Path.GetTempPath(), _OUTPUTS_FOLDER_NAME);

		DocumentationDestinationFolderPath destination = DocumentationDestinationFolderPath.FromSuppliedText(absolutePath);

		Assert.Equal(absolutePath, destination.ConsoleDisplayText);
	}

	[Fact]
	public void ARelativePathIsShownRelativeAndTheCanonicalPathIsNotThePrintedForm()
	{
		DocumentationDestinationFolderPath destination = DocumentationDestinationFolderPath.FromSuppliedText(_OUTPUTS_FOLDER_NAME);

		Assert.Equal($"./{_OUTPUTS_FOLDER_NAME}", destination.ConsoleDisplayText);
		Assert.True(Path.IsPathRooted(destination.CanonicalPath));
		Assert.DoesNotContain(destination.CanonicalPath, destination.ConsoleDisplayText, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(_OUTPUTS_FOLDER_NAME, destination.Label);
	}

	[Fact]
	public void TheDisplayTextCarriesNothingThatCouldActOnATerminal()
	{
		DocumentationDestinationFolderPath destination = DocumentationDestinationFolderPath.FromSuppliedText(_ESCAPE_CHARACTER_TEXT);

		Assert.DoesNotContain('\u001b', destination.ConsoleDisplayText);
		Assert.StartsWith("./Out", destination.ConsoleDisplayText, StringComparison.Ordinal);
	}

	[Fact]
	public void TheDefaultDestinationIsTheWorkingFolderAndIsShownAsSuch()
	{
		EffectiveConfiguration configuration = ConfigurationWith(null, ConfigurationSource.WorkingDirectoryAtLaunch);
		using StringWriter output = new();

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(configuration, output, out DocumentationDestinationFolderPath destination);

		Assert.True(canonicalized);
		Assert.Equal(".", destination.ConsoleDisplayText);
		Assert.Equal(CanonicalPath.Resolve(Directory.GetCurrentDirectory()), destination.CanonicalPath);
		Assert.Equal(string.Empty, output.ToString());
	}

	[Fact]
	public void ARelativeCommandLineValueIsShownRelativeToTheWorkingFolder()
	{
		EffectiveConfiguration configuration = ConfigurationWith(_OUTPUTS_FOLDER_NAME, ConfigurationSource.CommandLine);
		using StringWriter output = new();

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(configuration, output, out DocumentationDestinationFolderPath destination);

		Assert.True(canonicalized);
		Assert.Equal($"./{_OUTPUTS_FOLDER_NAME}", destination.ConsoleDisplayText);
		Assert.Equal(CanonicalPath.Resolve(_OUTPUTS_FOLDER_NAME), destination.CanonicalPath);
	}

	/// <summary>The command-line value is the shell's to expand; a tilde that reaches the tool there is a folder name.</summary>
	[Fact]
	public void ATildeOnTheCommandLineIsNotExpandedAndTheHomeIsNeverAsked()
	{
		EffectiveConfiguration configuration = ConfigurationWith($"~/{_OUTPUTS_FOLDER_NAME}", ConfigurationSource.CommandLine);
		using StringWriter output = new();
		int homeRequests = 0;

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(
			configuration,
			output,
			out DocumentationDestinationFolderPath destination,
			() =>
			{
				homeRequests++;

				return Path.GetTempPath();
			});

		Assert.True(canonicalized);
		Assert.Equal(0, homeRequests);
		Assert.Equal($"./~/{_OUTPUTS_FOLDER_NAME}", destination.ConsoleDisplayText);
		Assert.Equal(
			CanonicalPath.Resolve(Path.Combine(Directory.GetCurrentDirectory(), _TILDE_FOLDER_NAME, _OUTPUTS_FOLDER_NAME)),
			destination.CanonicalPath);
	}

	[Theory]
	[InlineData("~/Outputs")]
	[InlineData("~\\Outputs")]
	public void ATildeFolderFromTheProfileIsExpandedAndEchoedAsSupplied(string configuredText)
	{
		using TemporaryFolder homeFolder = TemporaryFolder.Create();
		EffectiveConfiguration configuration = ConfigurationWith(configuredText, ConfigurationSource.UserProfileFile);
		using StringWriter output = new();

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(
			configuration,
			output,
			out DocumentationDestinationFolderPath destination,
			() => homeFolder.Path);

		Assert.True(canonicalized);
		Assert.Equal(CanonicalPath.Resolve(Path.Combine(homeFolder.Path, _OUTPUTS_FOLDER_NAME)), destination.CanonicalPath);
		Assert.Equal(configuredText, destination.ConsoleDisplayText);
		Assert.Equal(string.Empty, output.ToString());
	}

	[Fact]
	public void ABareTildeFromTheProfileIsTheHomeFolderItself()
	{
		using TemporaryFolder homeFolder = TemporaryFolder.Create();
		EffectiveConfiguration configuration = ConfigurationWith(_TILDE_FOLDER_NAME, ConfigurationSource.UserProfileFile);
		using StringWriter output = new();

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(
			configuration,
			output,
			out DocumentationDestinationFolderPath destination,
			() => homeFolder.Path);

		Assert.True(canonicalized);
		Assert.Equal(homeFolder.Path, destination.CanonicalPath);
		Assert.Equal(_TILDE_FOLDER_NAME, destination.ConsoleDisplayText);
	}

	[Theory]
	[InlineData("~alice/Outputs")]
	[InlineData("~alice")]
	public void AnotherUsersHomeFormIsRefusedWithAMessageAndNothingIsCanonicalized(string configuredText)
	{
		EffectiveConfiguration configuration = ConfigurationWith(configuredText, ConfigurationSource.UserProfileFile);
		using StringWriter output = new();

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(
			configuration,
			output,
			out DocumentationDestinationFolderPath destination,
			() => Path.GetTempPath());

		Assert.False(canonicalized);
		Assert.Equal(default, destination);
		Assert.Equal(
			string.Format(CultureInfo.InvariantCulture, SessionStartup.DESTINATION_OTHER_USER_HOME_NOT_SUPPORTED_MESSAGE_FORMAT, configuredText)
				+ Environment.NewLine,
			output.ToString());
	}

	[Theory]
	[InlineData("")]
	[InlineData("  ")]
	public void AHomeFolderThatCannotBeResolvedIsRefusedWithAMessage(string unresolvedHome)
	{
		EffectiveConfiguration configuration = ConfigurationWith("~/Outputs", ConfigurationSource.UserProfileFile);
		using StringWriter output = new();

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(
			configuration,
			output,
			out DocumentationDestinationFolderPath destination,
			() => unresolvedHome);

		Assert.False(canonicalized);
		Assert.Equal(default, destination);
		Assert.Equal(
			string.Format(CultureInfo.InvariantCulture, SessionStartup.DESTINATION_HOME_FOLDER_UNRESOLVED_MESSAGE_FORMAT, "~/Outputs")
				+ Environment.NewLine,
			output.ToString());
	}

	/// <summary>Both refusals end the run with the existing exit code for an output destination that cannot be written to.</summary>
	[Fact]
	public void BothRefusalsEndTheRunWithExitCodeFour()
	{
		Assert.Equal(4, (int)ExitCode.OutputDestinationNotWritable);
		Assert.Equal(ExitCode.OutputDestinationNotWritable, WriteBoundary.REFUSAL_EXIT_CODE);
	}

	/// <summary>
	/// The whole path from a real profile file: the file's value is read, expanded against the injected home,
	/// checked by Pre-flight, and written to - and the working folder never gains a folder named <c>~</c>.
	/// </summary>
	[Theory]
	[InlineData(_PROFILE_WITH_FORWARD_SLASH_TILDE)]
	[InlineData(_PROFILE_WITH_BACKSLASH_TILDE)]
	public void NoFolderNamedTildeIsCreatedWhenTheProfileUsesATildeOutputPath(string profileDocument)
	{
		using TemporaryFolder homeFolder = TemporaryFolder.Create();
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		File.WriteAllText(homeFolder.Combine(_PROFILE_FILE_NAME), profileDocument);
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		EffectiveConfiguration configuration = ConfigurationResolver.Resolve(repository, null, homeFolder.Path);
		using StringWriter output = new();

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(
			configuration,
			output,
			out DocumentationDestinationFolderPath destination,
			() => homeFolder.Path);
		PreflightVerdict verdict = PreflightValidation.Run(configuration, repository, destination, output);
		bool writable = SessionStartup.TryEstablishDestinationIsWritable(destination, output);

		Assert.Equal(ConfigurationSource.UserProfileFile, configuration.Provenance.OutputPathOrigin);
		Assert.True(canonicalized);
		Assert.True(verdict.PermitsSession);
		Assert.True(writable);
		Assert.True(Directory.Exists(homeFolder.Combine(_OUTPUTS_FOLDER_NAME)));
		Assert.False(Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), _TILDE_FOLDER_NAME)));
		Assert.False(Directory.Exists(homeFolder.Combine(_TILDE_FOLDER_NAME)));
		Assert.False(Directory.Exists(repositoryFolder.Combine(_TILDE_FOLDER_NAME)));
	}

	/// <summary>Expansion comes before canonicalization, so the containment rule is applied to the expanded folder.</summary>
	[Theory]
	[InlineData("~", PathOverlap.SamePath)]
	[InlineData("~/inner", PathOverlap.DestinationInsideRepository)]
	public void TheContainmentRuleSeesTheExpandedFolderAndRefusesIt(string configuredText, PathOverlap expectedOverlap)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		EffectiveConfiguration configuration = ConfigurationWith(configuredText, ConfigurationSource.UserProfileFile);
		using StringWriter output = new();

		bool canonicalized = SessionStartup.TryCanonicalizeDestinationFolder(
			configuration,
			output,
			out DocumentationDestinationFolderPath destination,
			() => repositoryFolder.Path);
		PreflightVerdict verdict = PreflightValidation.Run(configuration, repository, destination, output);

		Assert.True(canonicalized);
		Assert.Equal(expectedOverlap, WriteBoundary.Evaluate(repository, destination).Overlap);
		Assert.False(verdict.PermitsSession);
		Assert.Equal(PreflightOutcome.WriteBoundaryRefused, verdict.Outcome);
		Assert.Equal(ExitCode.OutputDestinationNotWritable, verdict.ExitCode);
		Assert.Contains(configuredText, output.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain(destination.CanonicalPath, output.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The exit-4 text of startup names the folder as supplied, here a relative one, and not its label or canonical path.</summary>
	[Fact]
	public void TheCannotBeWrittenToTextEchoesTheSuppliedFolder()
	{
		using TemporaryFolder blockingFolder = TemporaryFolder.Create();
		string blockingFile = blockingFolder.Combine("blocker.txt");
		File.WriteAllText(blockingFile, "placeholder");
		DocumentationDestinationFolderPath destination = DocumentationDestinationFolderPath.FromResolvedText(blockingFile, "./Outputs");
		using StringWriter output = new();

		bool writable = SessionStartup.TryEstablishDestinationIsWritable(destination, output);

		Assert.False(writable);
		Assert.Equal(
			string.Format(CultureInfo.InvariantCulture, SessionStartup.DESTINATION_FOLDER_NOT_WRITABLE_MESSAGE_FORMAT, "./Outputs")
				+ Environment.NewLine,
			output.ToString());
		Assert.DoesNotContain(blockingFile, output.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void TheCannotBeWrittenToTextEchoesAnAbsoluteSuppliedFolderAsSupplied()
	{
		using TemporaryFolder blockingFolder = TemporaryFolder.Create();
		string blockingFile = blockingFolder.Combine("blocker.txt");
		File.WriteAllText(blockingFile, "placeholder");
		DocumentationDestinationFolderPath destination = DocumentationDestinationFolderPath.FromSuppliedText(blockingFile);
		using StringWriter output = new();

		bool writable = SessionStartup.TryEstablishDestinationIsWritable(destination, output);

		Assert.False(writable);
		Assert.Contains(blockingFile, output.ToString(), StringComparison.Ordinal);
	}

	private static EffectiveConfiguration ConfigurationWith(string? outputPathText, ConfigurationSource origin)
	{
		EffectiveConfiguration baseline = ReportHarness.ConfigurationWith(ConfidenceLevel.Low);

		return baseline with
		{
			OutputPathText = outputPathText,
			Provenance = baseline.Provenance with { OutputPathOrigin = origin }
		};
	}
}
