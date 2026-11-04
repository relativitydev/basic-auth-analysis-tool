using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Configuration;

/// <summary>
/// What Pre-flight says when the repository's own configuration file narrows, widens or breaks
/// its assessment - and that it says it at all.
///
/// A repository file may narrow its own assessment, so the guard against that being a way to hide
/// findings is that it is stated: what the repository asked for, in the product's own words, and
/// what that did. The second half is why a malformed file gets a literal message and then
/// one line naming the problem, and why a key or language this version does not know is a
/// warning rather than a refusal.
/// </summary>
public sealed class RepositoryConfigurationNarrowingTests
{
	private const string _DETAIL_PREFIX = "Detail:";
	private const string _WARNING_PREFIX = "Warning:";
	private const string _REPOSITORY_MALFORMED_MESSAGE =
		"The configuration file in the repository root was found but does not have the expected structure. The built-in default configuration is used instead.";
	private const string _PROFILE_MALFORMED_MESSAGE =
		"The configuration file in the user profile folder was found but does not have the expected structure. The built-in default configuration is used instead.";
	private const string _REPOSITORY_MALFORMED_BESIDE_PROFILE_MESSAGE =
		"The configuration file in the repository root was found but does not have the expected structure. Each setting is taken from the next source: the configuration file in the user profile folder where it sets one, the built-in default otherwise.";
	private const string _PROFILE_MALFORMED_BESIDE_REPOSITORY_MESSAGE =
		"The configuration file in the user profile folder was found but does not have the expected structure. The built-in defaults take its place for every setting the configuration file in the repository root does not set.";
	private const string _REPOSITORY_UNREADABLE_BESIDE_PROFILE_MESSAGE =
		"The configuration file in the repository root was found and could not be read. Each setting is taken from the next source: the configuration file in the user profile folder where it sets one, the built-in default otherwise.";
	private const string _REPOSITORY_TOO_LARGE_MESSAGE =
		"The configuration file in the repository root was found but is larger than the 262,144 bytes (256 KiB) this tool reads, so it was not read. The built-in default configuration is used instead.";
	private const string _VALID_PROFILE_DOCUMENT = "{ \"minimumConfidenceLevel\": \"High\" }";
	private const string _VALID_REPOSITORY_DOCUMENT = "{ \"minimumConfidenceLevel\": \"Medium\" }";
	private const int _OVERSIZED_FILE_LENGTH = 262145;
	private const int _REFUSAL_EXIT_CODE = 6;

	[Fact]
	public void ARepositoryLanguageListNamesWhatItSetsAndWhatItDisables()
	{
		string output = RunWith(repositoryContents: "{ \"enabledLanguages\": [ \"Python\" ] }").Output;

		Assert.Contains(
			"The configuration file in the repository root sets the enabled languages to Python; it disables CSharp, VBNet, JavaScript, TypeScript, Go, PowerShell, Shell for this assessment.",
			output,
			StringComparison.Ordinal);
	}

	[Fact]
	public void ARepositoryThresholdRaiseNamesBothLevelsAndTheLevelsThatAreNoLongerShown()
	{
		string output = RunWith(repositoryContents: "{ \"minimumConfidenceLevel\": \"High\" }").Output;

		Assert.Contains(
			"The configuration file in the repository root raises the minimum confidence level from Low to High; Medium and Low findings are not shown.",
			output,
			StringComparison.Ordinal);
	}

	[Fact]
	public void ARepositoryThresholdRaiseByOneLevelNamesOnlyThatLevel()
	{
		string output = RunWith(
			profileContents: "{ \"minimumConfidenceLevel\": \"Medium\" }",
			repositoryContents: "{ \"minimumConfidenceLevel\": \"High\" }").Output;

		Assert.Contains("from Medium to High; Medium findings are not shown.", output, StringComparison.Ordinal);
	}

	[Fact]
	public void ARepositoryThresholdLoweringIsStatedPlainly()
	{
		string output = RunWith(
			profileContents: "{ \"minimumConfidenceLevel\": \"High\" }",
			repositoryContents: "{ \"minimumConfidenceLevel\": \"Low\" }").Output;

		Assert.Contains(
			"The configuration file in the repository root lowers the minimum confidence level from High to Low; Medium and Low findings are now shown.",
			output,
			StringComparison.Ordinal);
	}

	/// <summary>Partial overlap is worded as an enabling and a disabling, not as a disabling alone.</summary>
	[Fact]
	public void APartialOverlapIsWordedAsAnEnablingAndADisabling()
	{
		string output = RunWith(
			profileContents: "{ \"enabledLanguages\": [ \"Python\" ] }",
			repositoryContents: "{ \"enabledLanguages\": [ \"CSharp\" ] }").Output;

		Assert.Contains(
			"The configuration file in the repository root sets the enabled languages to CSharp: it enables CSharp and disables Python.",
			output,
			StringComparison.Ordinal);
	}

	[Fact]
	public void ARepositoryLanguageListThatOnlyAddsIsWordedAsAnEnabling()
	{
		string output = RunWith(
			profileContents: "{ \"enabledLanguages\": [ \"CSharp\" ] }",
			repositoryContents: "{ \"enabledLanguages\": [ \"CSharp\", \"Go\" ] }").Output;

		Assert.Contains("sets the enabled languages to CSharp, Go; it enables Go for this assessment.", output, StringComparison.Ordinal);
	}

	[Fact]
	public void ARepositoryFileThatChangesNothingSaysNothing()
	{
		string output = RunWith(
			profileContents: "{ \"enabledLanguages\": [ \"Go\" ] }",
			repositoryContents: "{ \"enabledLanguages\": [ \"Go\" ], \"minimumConfidenceLevel\": \"Low\" }").Output;

		Assert.Equal(string.Empty, output);
	}

	[Theory]
	[InlineData(null, "{ \"enabledLanguages\": [] }")]
	[InlineData("{ \"enabledLanguages\": [] }", null)]
	[InlineData("{ \"enabledLanguages\": [ \"Go\" ] }", "{ \"enabledLanguages\": [] }")]
	public void AnEffectiveSetOfNoLanguagesRefusesTheSessionWhereverItCameFrom(string? profileContents, string? repositoryContents)
	{
		PreflightRun run = RunWith(profileContents, repositoryContents);

		Assert.False(run.Verdict.PermitsSession);
		Assert.Equal(PreflightOutcome.NoLanguageEnabled, run.Verdict.Outcome);
		Assert.Equal(_REFUSAL_EXIT_CODE, (int)run.Verdict.ExitCode);
		Assert.Contains(PreflightValidation.NO_LANGUAGE_ENABLED_MESSAGE, run.Output, StringComparison.Ordinal);
	}

	[Fact]
	public void AMalformedRepositoryFileBesideAnAppliedProfileSaysTheProfileSuppliesTheSettings()
	{
		PreflightRun run = RunWith(profileContents: _VALID_PROFILE_DOCUMENT, repositoryContents: "{ not json");

		Assert.Equal(_REPOSITORY_MALFORMED_BESIDE_PROFILE_MESSAGE, FirstLineOf(run.Output));
		Assert.Equal(MigrateAuth.Cli.Catalog.ConfidenceLevel.High, run.Configuration.MinimumConfidenceLevel);
	}

	[Fact]
	public void AMalformedProfileBesideAnAppliedRepositoryFileDoesNotClaimTheDefaultsReplaceEverything()
	{
		PreflightRun run = RunWith(profileContents: "{ not json", repositoryContents: _VALID_REPOSITORY_DOCUMENT);

		Assert.Equal(_PROFILE_MALFORMED_BESIDE_REPOSITORY_MESSAGE, FirstLineOf(run.Output));
		Assert.Equal(MigrateAuth.Cli.Catalog.ConfidenceLevel.Medium, run.Configuration.MinimumConfidenceLevel);
	}

	[Fact]
	public void TwoMalformedFilesStillSayTheBuiltInDefaultsApply()
	{
		string[] lines = LinesOf(RunWith(profileContents: "{ not json", repositoryContents: "{ not json").Output);

		Assert.Contains(_REPOSITORY_MALFORMED_MESSAGE, lines);
		Assert.Contains(_PROFILE_MALFORMED_MESSAGE, lines);
	}

	[Fact]
	public void AnUnreadableRepositoryFileBesideAnAppliedProfileUsesTheSameConsequence()
	{
		string message = string.Format(
			System.Globalization.CultureInfo.InvariantCulture,
			PreflightValidation.CONFIGURATION_FILE_UNREADABLE_MESSAGE_FORMAT,
			PreflightValidation.REPOSITORY_ROOT_LOCATION_LABEL,
			PreflightValidation.DEGRADATION_TO_USER_PROFILE_MESSAGE);

		Assert.Equal(_REPOSITORY_UNREADABLE_BESIDE_PROFILE_MESSAGE, message);
	}

	[Theory]
	[InlineData(null, "{ \"enabledLanguages\": [] }", "in the repository root")]
	[InlineData("{ \"enabledLanguages\": [] }", null, "in the user profile folder")]
	[InlineData("{ \"enabledLanguages\": [ \"Go\" ] }", "{ \"enabledLanguages\": [] }", "in the repository root")]
	[InlineData("{ \"enabledLanguages\": [] }", "{ \"minimumConfidenceLevel\": \"High\" }", "in the user profile folder")]
	public void TheNoLanguageRefusalNamesTheFileToFix(string? profileContents, string? repositoryContents, string expectedLocation)
	{
		PreflightRun run = RunWith(profileContents, repositoryContents);

		Assert.Contains(
			$"Enable at least one language in .migrate-auth.json {expectedLocation} before starting a session.",
			run.Output,
			StringComparison.Ordinal);
	}

	[Fact]
	public void ARepositoryListOfOnlyUnknownLanguagesRefusesTheSessionAndWarnsAboutTheName()
	{
		PreflightRun run = RunWith(repositoryContents: "{ \"enabledLanguages\": [ \"Rust\" ] }");

		Assert.Equal(PreflightOutcome.NoLanguageEnabled, run.Verdict.Outcome);
		Assert.Contains("names the language \"Rust\", which this version does not know", run.Output, StringComparison.Ordinal);
	}

	[Fact]
	public void AListOfOnlyUnknownLanguagesSaysEveryNameWasUnknownNamesThemAndListsTheAcceptedNames()
	{
		string[] lines = LinesOf(RunWith(repositoryContents: "{ \"enabledLanguages\": [ \"Klingon\", \"Rust\" ] }").Output);

		Assert.Contains(
			"Every language the enabled languages list names is unknown to this version (\"Klingon\", \"Rust\"), so no language is enabled. The accepted names are CSharp, VBNet, JavaScript, TypeScript, Python, Go, PowerShell, Shell.",
			lines);
		Assert.Contains(PreflightValidation.NO_LANGUAGE_ENABLED_MESSAGE, lines);
	}

	[Fact]
	public void AnEmptyListSaysNothingAboutUnknownNames()
	{
		PreflightRun run = RunWith(repositoryContents: "{ \"enabledLanguages\": [] }");

		Assert.Equal(PreflightOutcome.NoLanguageEnabled, run.Verdict.Outcome);
		Assert.DoesNotContain("unknown to this version", run.Output, StringComparison.Ordinal);
	}

	[Fact]
	public void TheMalformedFileMessagesAreVerbatimForBothLocations()
	{
		PreflightRun repositoryRun = RunWith(repositoryContents: "{ not json");
		PreflightRun profileRun = RunWith(profileContents: "{ not json");

		Assert.Equal(_REPOSITORY_MALFORMED_MESSAGE, FirstLineOf(repositoryRun.Output));
		Assert.Equal(_PROFILE_MALFORMED_MESSAGE, FirstLineOf(profileRun.Output));
	}

	[Fact]
	public void AMalformedFileGetsTheLiteralMessageThenExactlyOneDetailLine()
	{
		string[] lines = LinesOf(RunWith(repositoryContents: "{ not json").Output);

		Assert.Equal(_REPOSITORY_MALFORMED_MESSAGE, lines[0]);
		Assert.StartsWith(_DETAIL_PREFIX, lines[1], StringComparison.Ordinal);
		Assert.Equal(2, lines.Length);
	}

	[Fact]
	public void InvalidJsonNamesTheLineAndPositionTheParserStoppedAt()
	{
		string[] lines = LinesOf(RunWith(repositoryContents: "{\n\t\"enabledLanguages\": [],\n\tnot json\n}").Output);

		Assert.Matches("^Detail: the file is not valid JSON; the parser stopped at line 3, position \\d+\\.$", lines[1]);
	}

	[Fact]
	public void ATopLevelValueThatIsNotAnObjectIsNamed()
	{
		string[] lines = LinesOf(RunWith(repositoryContents: "[ \"an array\" ]").Output);

		Assert.Equal(ConfigurationDiagnosticText.NOT_AN_OBJECT_MESSAGE, lines[1]);
	}

	/// <summary>
	/// A known key with a value this version cannot use is a warning, not a malformed file: the
	/// warning names the key and the value, and says the rest of the file is applied.
	/// </summary>
	[Fact]
	public void AnUnknownConfidenceLevelIsAWarningNamingTheKeyAndTheValue()
	{
		string[] lines = LinesOf(RunWith(repositoryContents: "{ \"minimumConfidenceLevel\": \"Critical\" }").Output);

		Assert.Equal(
			"Warning: in the configuration file in the repository root, minimumConfidenceLevel is \"Critical\"; it must be Low, Medium or High. That key is ignored and the rest of the file is applied.",
			Assert.Single(lines));
	}

	[Fact]
	public void ALanguageListThatIsNotAnArrayIsAWarningNamingTheKey()
	{
		string[] lines = LinesOf(RunWith(repositoryContents: "{ \"enabledLanguages\": 7 }").Output);

		Assert.Equal(
			"Warning: in the configuration file in the repository root, enabledLanguages must be an array of language names; that key is ignored and the rest of the file is applied.",
			Assert.Single(lines));
	}

	[Fact]
	public void ABlankLanguageEntryIsAWarningNamingTheKey()
	{
		string[] lines = LinesOf(RunWith(repositoryContents: "{ \"enabledLanguages\": [ \"\" ] }").Output);

		Assert.Equal(
			"Warning: in the configuration file in the repository root, enabledLanguages must be an array of language names; that key is ignored and the rest of the file is applied.",
			Assert.Single(lines));
	}

	[Fact]
	public void AnInvalidProfileDefaultFlowIsAWarningAndNotAMalformedFile()
	{
		string output = RunWith(profileContents: "{ \"defaultFlow\": \"Client Credentials\", \"outputPath\": \"reports\" }").Output;

		Assert.Equal(
			"Warning: in the configuration file in the user profile folder, defaultFlow is \"Client Credentials\"; it must be ClientCredentials or AuthorizationCode. That key is ignored and the rest of the file is applied.",
			Assert.Single(LinesOf(output)));
		Assert.DoesNotContain(_PROFILE_MALFORMED_MESSAGE, output, StringComparison.Ordinal);
	}

	[Fact]
	public void AnOversizedFileIsReportedWithTheSizeLimitAndNotAsAnUnexpectedStructure()
	{
		string[] lines = LinesOf(RunWith(repositoryContents: new string(' ', _OVERSIZED_FILE_LENGTH)).Output);

		Assert.Equal(_REPOSITORY_TOO_LARGE_MESSAGE, Assert.Single(lines));
		Assert.DoesNotContain(lines, line => line.StartsWith("Detail:", StringComparison.Ordinal));
	}

	[Fact]
	public void AnOversizedProfileFileIsReportedWithTheSizeLimitAndTheConsequenceForTheRepositoryFile()
	{
		string[] lines = LinesOf(RunWith(profileContents: new string(' ', _OVERSIZED_FILE_LENGTH), repositoryContents: _VALID_REPOSITORY_DOCUMENT).Output);

		Assert.Equal(
			"The configuration file in the user profile folder was found but is larger than the 262,144 bytes (256 KiB) this tool reads, so it was not read. The built-in defaults take its place for every setting the configuration file in the repository root does not set.",
			lines[0]);
		Assert.DoesNotContain(lines, line => line.Contains("expected structure", StringComparison.Ordinal));
		Assert.DoesNotContain(lines, line => line.StartsWith("Detail:", StringComparison.Ordinal));
	}

	/// <summary>
	/// The docs say keys the tool does not know are ignored, so a misspelt key does not stop the
	/// file being applied - but it is named, or the misspelling goes unnoticed.
	/// </summary>
	[Fact]
	public void AnUnknownKeyIsAWarningAndTheRestOfTheFileStillApplies()
	{
		PreflightRun run = RunWith(repositoryContents: "{ \"minimumConfidenceLevl\": \"High\", \"enabledLanguages\": [ \"Go\" ] }");

		Assert.True(run.Verdict.PermitsSession);
		Assert.Contains(
			"Warning: the configuration file in the repository root has the key \"minimumConfidenceLevl\", which this version does not use; it is ignored.",
			run.Output,
			StringComparison.Ordinal);
		Assert.Contains("sets the enabled languages to Go", run.Output, StringComparison.Ordinal);
		Assert.DoesNotContain("raises the minimum confidence level", run.Output, StringComparison.Ordinal);
		Assert.DoesNotContain(_DETAIL_PREFIX, run.Output, StringComparison.Ordinal);
	}

	[Fact]
	public void AnUnknownLanguageIsAWarningAndTheKnownOnesStillApply()
	{
		PreflightRun run = RunWith(profileContents: "{ \"enabledLanguages\": [ \"Rust\", \"Go\" ] }");

		Assert.True(run.Verdict.PermitsSession);
		Assert.Contains(
			"Warning: the configuration file in the user profile folder names the language \"Rust\", which this version does not know; it is ignored.",
			run.Output,
			StringComparison.Ordinal);
		Assert.True(run.Configuration.IsEnabled(MigrateAuth.Cli.Catalog.AssessedLanguage.Go));
	}

	/// <summary>A name typed in the file reaches the console only, and the record the report renders never holds it.</summary>
	[Fact]
	public void ATypedNameNeverReachesTheRecordTheReportRenders()
	{
		PreflightRun run = RunWith(repositoryContents: "{ \"enabledLanguages\": [ \"Rust\", \"Python\" ], \"aTypedKey\": 1 }");

		string[] rendered =
		[
			.. RepositoryConfigurationEffectText.StatementsFor(run.Configuration.Provenance.RepositoryEffect)
		];

		Assert.NotEmpty(rendered);
		Assert.All(rendered, statement => Assert.DoesNotContain("Rust", statement, StringComparison.Ordinal));
		Assert.All(rendered, statement => Assert.DoesNotContain("aTypedKey", statement, StringComparison.Ordinal));
		Assert.Equal(2, run.Configuration.FileDiagnostics.Length);
	}

	[Fact]
	public void ATypedNameIsStrippedOfCharactersThatCanActOnATerminal()
	{
		PreflightRun run = RunWith(repositoryContents: "{ \"bad\\u001b[31mkey\": 1 }");

		Assert.DoesNotContain('\u001b', run.Output);
		Assert.Contains("\"bad[31mkey\"", run.Output, StringComparison.Ordinal);
	}

	[Fact]
	public void ManyUnknownKeysDoNotFloodTheConsole()
	{
		string keys = string.Join(", ", Enumerable.Range(0, 200).Select(index => $"\"key{index}\": 1"));

		string[] warnings = [.. LinesOf(RunWith(repositoryContents: $"{{ {keys} }}").Output).Where(line => line.StartsWith(_WARNING_PREFIX, StringComparison.Ordinal))];

		Assert.Equal(10, warnings.Length);
	}

	private static PreflightRun RunWith(string? profileContents = null, string? repositoryContents = null)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder destinationFolder = TemporaryFolder.Create();

		if (profileContents is not null)
		{
			ConfigurationFile.WriteInto(profileFolder, profileContents);
		}

		if (repositoryContents is not null)
		{
			ConfigurationFile.WriteInto(repositoryFolder, repositoryContents);
		}

		RepositoryFolderPath repository = RepositoryFolderPath.FromSuppliedText(repositoryFolder.Path);
		EffectiveConfiguration configuration = ConfigurationResolver.Resolve(repository, null, profileFolder.Path);
		StringWriter output = new();
		PreflightVerdict verdict = PreflightValidation.Run(
			configuration,
			repository,
			DocumentationDestinationFolderPath.FromSuppliedText(destinationFolder.Path),
			output);

		return new PreflightRun(verdict, output.ToString(), configuration);
	}

	private static string[] LinesOf(string output) =>
		output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

	private static string FirstLineOf(string output) => LinesOf(output)[0];

	private sealed record PreflightRun(PreflightVerdict Verdict, string Output, EffectiveConfiguration Configuration);
}
