using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Tests.Session;

using Xunit;

namespace MigrateAuth.Cli.Tests.Configuration;

/// <summary>
/// The resolution chain: the repository's file, then the user profile, then the
/// built-in default, with a valid value in an earlier one replacing the later ones.
///
/// A repository may narrow its own assessment under that order, which is why the tests that
/// matter most here assert that a narrowing is RECORDED - the languages it disabled, the ones it
/// enabled, the threshold it raised - and not merely applied. The output path and the default flow
/// are the exceptions: neither is ever read from the repository's file.
/// </summary>
public sealed class ConfigurationResolverTests
{
	private const string _EMPTY_DOCUMENT = "{}";
	private const string _UNRELATED_FILE_NAME = "readme.txt";
	private const string _SUPPLIED_OUTPUT_PATH = "/somewhere/else/output";
	private const string _COMMAND_LINE_OUTPUT_PATH = "/from/the/command/line";
	private const int _CONFIGURATION_FIELD_COUNT = 4;
	private const string _ENABLED_LANGUAGES_KEY = "enabledLanguages";
	private const string _MINIMUM_CONFIDENCE_LEVEL_KEY = "minimumConfidenceLevel";
	private const string _OUTPUT_PATH_KEY = "outputPath";
	private const string _DEFAULT_FLOW_KEY = "defaultFlow";

	[Fact]
	public void WithNoConfigurationFileAnywhereEveryLanguageIsEnabledAtTheWidestThreshold()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, configuration.EnabledLanguageCount);
		Assert.Equal(ConfidenceLevels.WIDEST, configuration.MinimumConfidenceLevel);
		Assert.Equal(ConfigurationSource.BuiltInDefault, configuration.Provenance.EnabledLanguagesOrigin);
		Assert.Equal(ConfigurationSource.BuiltInDefault, configuration.Provenance.MinimumConfidenceLevelOrigin);
		Assert.Empty(configuration.Provenance.IgnoredRepositoryFields);
	}

	/// <summary>
	/// the absence of any configuration file is not an error. A folder holding
	/// unrelated files resolves identically to an empty one, and neither reports anything.
	/// </summary>
	[Fact]
	public void AMissingConfigurationFileIsIndistinguishableFromNoConfigurationAtAll()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();
		using TemporaryFolder emptyRepositoryFolder = TemporaryFolder.Create();

		File.WriteAllText(repositoryFolder.Combine(_UNRELATED_FILE_NAME), _EMPTY_DOCUMENT);

		EffectiveConfiguration withUnrelatedFile = Resolve(repositoryFolder, profileFolder);
		EffectiveConfiguration withNothing = Resolve(emptyRepositoryFolder, profileFolder);

		LanguageEnablement[] expectedEnablement = [.. withNothing.LanguageEnablement];

		Assert.Equal(expectedEnablement, withUnrelatedFile.LanguageEnablement.ToArray());
		Assert.Equal(withNothing.MinimumConfidenceLevel, withUnrelatedFile.MinimumConfidenceLevel);
		Assert.All(
			withUnrelatedFile.Provenance.ConfigurationFiles,
			fileState => Assert.Equal(ConfigurationFileCondition.Absent, fileState.Condition));
	}

	[Theory]
	[InlineData("{ not json at all")]
	[InlineData("[ \"an array rather than an object\" ]")]
	public void AMalformedRepositoryFileDegradesToTheBuiltInDefaults(string contents)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, contents);

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Malformed, ConditionOf(configuration, ConfigurationFileLocation.RepositoryRoot));
		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, configuration.EnabledLanguageCount);
		Assert.Equal(ConfidenceLevels.WIDEST, configuration.MinimumConfidenceLevel);
	}

	[Fact]
	public void AMalformedUserProfileFileDegradesToTheBuiltInDefaults()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"enabledLanguages\": ");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Malformed, ConditionOf(configuration, ConfigurationFileLocation.UserProfile));
		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, configuration.EnabledLanguageCount);
	}

	/// <summary>
	/// A key holding a value this version cannot use is ignored on its own, as if it were absent,
	/// and the file's other keys still apply - here the threshold the repository file sets.
	/// </summary>
	[Theory]
	[InlineData("{ \"enabledLanguages\": 7, \"minimumConfidenceLevel\": \"High\" }")]
	[InlineData("{ \"enabledLanguages\": [ \"\" ], \"minimumConfidenceLevel\": \"High\" }")]
	[InlineData("{ \"enabledLanguages\": [ \"CSharp\", null ], \"minimumConfidenceLevel\": \"High\" }")]
	public void AnUnusableRepositoryLanguageListIsIgnoredAloneAndTheThresholdStillApplies(string contents)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, contents);

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Accepted, ConditionOf(configuration, ConfigurationFileLocation.RepositoryRoot));
		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, configuration.EnabledLanguageCount);
		Assert.Equal(ConfigurationSource.BuiltInDefault, configuration.Provenance.EnabledLanguagesOrigin);
		Assert.Equal(ConfidenceLevel.High, configuration.MinimumConfidenceLevel);
		Assert.Equal(ConfigurationSource.RepositoryFile, configuration.Provenance.MinimumConfidenceLevelOrigin);
		Assert.Equal(
			_ENABLED_LANGUAGES_KEY,
			Assert.Single(configuration.FileDiagnostics, IsInvalidValue).Subject);
	}

	/// <summary>
	/// An unknown level in the repository's file is ignored on its own: the threshold falls through
	/// to the profile's, as an absent one does, and the repository's language list still applies.
	/// </summary>
	[Theory]
	[InlineData("\"Critical\"")]
	[InlineData("3")]
	public void AnUnknownRepositoryLevelFallsThroughToTheProfileAndTheLanguageListStillApplies(string declaredLevel)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"minimumConfidenceLevel\": \"Medium\" }");
		ConfigurationFile.WriteInto(
			repositoryFolder,
			$"{{ \"minimumConfidenceLevel\": {declaredLevel}, \"enabledLanguages\": [ \"CSharp\" ] }}");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Accepted, ConditionOf(configuration, ConfigurationFileLocation.RepositoryRoot));
		Assert.Equal(ConfidenceLevel.Medium, configuration.MinimumConfidenceLevel);
		Assert.Equal(ConfigurationSource.UserProfileFile, configuration.Provenance.MinimumConfidenceLevelOrigin);
		Assert.Equal(1, configuration.EnabledLanguageCount);
		Assert.True(configuration.IsEnabled(AssessedLanguage.CSharp));
		Assert.Equal(
			_MINIMUM_CONFIDENCE_LEVEL_KEY,
			Assert.Single(configuration.FileDiagnostics, IsInvalidValue).Subject);
	}

	/// <summary>A key that appears twice keeps its last value, as the JSON deserializer does.</summary>
	[Fact]
	public void ARepeatedKeyKeepsItsLastValueAndAnEarlierInvalidOneRaisesNothing()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(
			profileFolder,
			"{ \"minimumConfidenceLevel\": \"Critical\", \"minimumConfidenceLevel\": \"High\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfidenceLevel.High, configuration.MinimumConfidenceLevel);
		Assert.DoesNotContain(configuration.FileDiagnostics, IsInvalidValue);
	}

	/// <summary>
	/// A field a later release adds is ignored rather than making the file malformed, and so is a
	/// language name this build does not know. That is the configuration read's version tolerance.
	/// </summary>
	[Fact]
	public void AnUnknownFieldAndAnUnknownLanguageAreToleratedRatherThanMalformed()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(
			profileFolder,
			"{ \"enabledLanguages\": [ \"CSharp\", \"Fortran\" ], \"aFieldFromALaterRelease\": true }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Accepted, ConditionOf(configuration, ConfigurationFileLocation.UserProfile));
		Assert.True(configuration.IsEnabled(AssessedLanguage.CSharp));
		Assert.Equal(1, configuration.EnabledLanguageCount);
	}

	/// <summary>The user profile narrows freely. It is the user's own machine, not the audited object.</summary>
	[Fact]
	public void TheUserProfileMayNarrowWhatIsReported()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(
			profileFolder,
			"{ \"enabledLanguages\": [ \"CSharp\" ], \"minimumConfidenceLevel\": \"High\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(1, configuration.EnabledLanguageCount);
		Assert.Equal(ConfidenceLevel.High, configuration.MinimumConfidenceLevel);
		Assert.Equal(ConfigurationSource.UserProfileFile, configuration.Provenance.EnabledLanguagesOrigin);
		Assert.Equal(ConfigurationSource.UserProfileFile, configuration.Provenance.MinimumConfidenceLevelOrigin);
		Assert.Empty(configuration.Provenance.IgnoredRepositoryFields);
	}

	/// <summary>
	/// The resolution order: the repository's list replaces the built-in default, and what it
	/// disabled is recorded rather than left to be inferred from the set that remains.
	/// </summary>
	[Fact]
	public void TheRepositoryFileReplacesTheEnabledLanguagesAndRecordsWhatItDisabled()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, "{ \"enabledLanguages\": [ \"Python\" ] }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(1, configuration.EnabledLanguageCount);
		Assert.True(configuration.IsEnabled(AssessedLanguage.Python));
		Assert.Equal(ConfigurationSource.RepositoryFile, configuration.Provenance.EnabledLanguagesOrigin);
		Assert.Empty(configuration.Provenance.IgnoredRepositoryFields);

		RepositoryConfigurationEffect effect = configuration.Provenance.RepositoryEffect;

		Assert.Equal([AssessedLanguage.Python], effect.LanguagesSetByRepository.ToArray());
		Assert.Empty(effect.EnabledLanguages);
		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT - 1, effect.DisabledLanguages.Length);
		Assert.DoesNotContain(AssessedLanguage.Python, effect.DisabledLanguages);
		Assert.True(effect.NarrowsReporting);
	}

	/// <summary>The repository's list replaces the user profile's, and the overlap is recorded as both an enabling and a disabling.</summary>
	[Fact]
	public void TheRepositoryFileReplacesTheProfileLanguagesAndRecordsWhatItEnabledAndDisabled()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"enabledLanguages\": [ \"Python\" ] }");
		ConfigurationFile.WriteInto(repositoryFolder, "{ \"enabledLanguages\": [ \"CSharp\" ] }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);
		RepositoryConfigurationEffect effect = configuration.Provenance.RepositoryEffect;

		Assert.Equal([AssessedLanguage.CSharp], configuration.EnabledLanguagesInDeclaredOrder.ToArray());
		Assert.Equal([AssessedLanguage.CSharp], effect.EnabledLanguages.ToArray());
		Assert.Equal([AssessedLanguage.Python], effect.DisabledLanguages.ToArray());
		Assert.Equal(ConfigurationSource.RepositoryFile, configuration.Provenance.EnabledLanguagesOrigin);
	}

	/// <summary>A repository list that matches what already applied changes nothing worth saying, but is still the source.</summary>
	[Fact]
	public void ARepositoryLanguageListEqualToTheProfileListIsTheSourceAndChangesNothing()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"enabledLanguages\": [ \"Go\" ] }");
		ConfigurationFile.WriteInto(repositoryFolder, "{ \"enabledLanguages\": [ \"Go\" ] }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationSource.RepositoryFile, configuration.Provenance.EnabledLanguagesOrigin);
		Assert.False(configuration.Provenance.RepositoryEffect.ChangesLanguages);
		Assert.Empty(RepositoryConfigurationEffectText.StatementsFor(configuration.Provenance.RepositoryEffect));
	}

	/// <summary>A repository list that adds a language to the profile's is applied and recorded as an enabling only.</summary>
	[Fact]
	public void TheRepositoryFileMayEnableALanguageNothingElseEnabled()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"enabledLanguages\": [ \"CSharp\" ] }");
		ConfigurationFile.WriteInto(repositoryFolder, "{ \"enabledLanguages\": [ \"CSharp\", \"Go\" ] }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.True(configuration.IsEnabled(AssessedLanguage.Go));
		Assert.True(configuration.IsEnabled(AssessedLanguage.CSharp));
		Assert.Equal(ConfigurationSource.RepositoryFile, configuration.Provenance.EnabledLanguagesOrigin);
		Assert.Empty(configuration.Provenance.IgnoredRepositoryFields);
		Assert.Equal([AssessedLanguage.Go], configuration.Provenance.RepositoryEffect.EnabledLanguages.ToArray());
		Assert.Empty(configuration.Provenance.RepositoryEffect.DisabledLanguages);
		Assert.False(configuration.Provenance.RepositoryEffect.NarrowsReporting);
	}

	/// <summary>
	/// A raised threshold hides findings, so the repository's level is applied and the change is
	/// recorded with the level it replaced, which is what lets the report count what it hides.
	/// </summary>
	[Fact]
	public void TheRepositoryFileRaisesTheThresholdAndRecordsFromWhichLevel()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, "{ \"minimumConfidenceLevel\": \"High\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfidenceLevel.High, configuration.MinimumConfidenceLevel);
		Assert.Equal(ConfigurationSource.RepositoryFile, configuration.Provenance.MinimumConfidenceLevelOrigin);
		Assert.Empty(configuration.Provenance.IgnoredRepositoryFields);

		ConfidenceLevelChange change = Assert.IsType<ConfidenceLevelChange>(configuration.Provenance.RepositoryEffect.ThresholdChange);

		Assert.Equal(ConfidenceLevel.Low, change.Previous);
		Assert.Equal(ConfidenceLevel.High, change.Current);
		Assert.True(change.IsRaise);
		Assert.True(configuration.Provenance.RepositoryEffect.NarrowsReporting);
	}

	/// <summary>The profile's level, not the built-in one, is what the repository's level is compared with.</summary>
	[Fact]
	public void TheRepositoryThresholdIsComparedWithTheProfileThresholdWhenThereIsOne()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"minimumConfidenceLevel\": \"Medium\" }");
		ConfigurationFile.WriteInto(repositoryFolder, "{ \"minimumConfidenceLevel\": \"High\" }");

		ConfidenceLevelChange change = Assert.IsType<ConfidenceLevelChange>(
			Resolve(repositoryFolder, profileFolder).Provenance.RepositoryEffect.ThresholdChange);

		Assert.Equal(ConfidenceLevel.Medium, change.Previous);
		Assert.Equal(ConfidenceLevel.High, change.Current);
	}

	/// <summary>A repository level equal to the one already in effect is the source and records no change.</summary>
	[Fact]
	public void ARepositoryThresholdEqualToTheEffectiveOneRecordsNoChange()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, "{ \"minimumConfidenceLevel\": \"Low\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Null(configuration.Provenance.RepositoryEffect.ThresholdChange);
		Assert.Equal(ConfigurationSource.RepositoryFile, configuration.Provenance.MinimumConfidenceLevelOrigin);
	}

	[Fact]
	public void TheRepositoryFileMayLowerTheThreshold()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"minimumConfidenceLevel\": \"High\" }");
		ConfigurationFile.WriteInto(repositoryFolder, "{ \"minimumConfidenceLevel\": \"Low\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfidenceLevel.Low, configuration.MinimumConfidenceLevel);
		Assert.Equal(ConfigurationSource.RepositoryFile, configuration.Provenance.MinimumConfidenceLevelOrigin);
		Assert.Empty(configuration.Provenance.IgnoredRepositoryFields);
		Assert.False(configuration.Provenance.RepositoryEffect.ThresholdChange!.IsRaise);
		Assert.False(configuration.Provenance.RepositoryEffect.NarrowsReporting);
	}

	/// <summary>
	/// The output path is not read from the repository at all - not narrowed, not widened, not
	/// read. And the value is not recorded either, because a path out of the audited repository is
	/// what the data invariant forbids emitting.
	/// </summary>
	[Fact]
	public void TheRepositoryFileOutputPathIsNeverReadAndItsValueIsNeverRecorded()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, "{ \"outputPath\": \"/inside/the/repository\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Null(configuration.OutputPathText);
		Assert.Equal(ConfigurationSource.WorkingDirectoryAtLaunch, configuration.Provenance.OutputPathOrigin);

		IgnoredRepositoryConfigurationField ignoredField =
			Assert.Single(configuration.Provenance.IgnoredRepositoryFields);

		Assert.Equal(ConfigurationField.OutputPath, ignoredField.Field);
		Assert.Equal(ConfigurationFieldIgnoredReason.NotReadFromTheRepository, ignoredField.Reason);
		Assert.Equal(string.Empty, ignoredField.WouldHaveBeen);
	}

	[Fact]
	public void TheUserProfileOutputPathIsReadAndTheCommandLineOverridesIt()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, $"{{ \"outputPath\": \"{_SUPPLIED_OUTPUT_PATH}\" }}");

		EffectiveConfiguration fromProfile = Resolve(repositoryFolder, profileFolder);
		EffectiveConfiguration fromCommandLine = ConfigurationResolver.Resolve(
			RepositoryOf(repositoryFolder),
			_COMMAND_LINE_OUTPUT_PATH,
			profileFolder.Path);

		Assert.Equal(_SUPPLIED_OUTPUT_PATH, fromProfile.OutputPathText);
		Assert.Equal(ConfigurationSource.UserProfileFile, fromProfile.Provenance.OutputPathOrigin);
		Assert.Equal(_COMMAND_LINE_OUTPUT_PATH, fromCommandLine.OutputPathText);
		Assert.Equal(ConfigurationSource.CommandLine, fromCommandLine.Provenance.OutputPathOrigin);
	}

	/// <summary>
	/// An empty array is a different answer from an absent field. In the user's own profile it
	/// means "none", which is the configuration the tool refuses to start a session on.
	/// </summary>
	[Fact]
	public void AnEmptyLanguageArrayInTheUserProfileDisablesEveryLanguage()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"enabledLanguages\": [] }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.False(configuration.HasAnyEnabledLanguage);
		Assert.Equal(0, configuration.EnabledLanguageCount);
	}

	/// <summary>The same array in the repository's own file disables every language too, and records it.</summary>
	[Fact]
	public void AnEmptyLanguageArrayInTheRepositoryDisablesEveryLanguageAndRecordsIt()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, "{ \"enabledLanguages\": [] }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.False(configuration.HasAnyEnabledLanguage);
		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, configuration.Provenance.RepositoryEffect.DisabledLanguages.Length);
		Assert.Empty(configuration.Provenance.RepositoryEffect.LanguagesSetByRepository);
	}

	/// <summary>An empty repository list overrides a non-empty profile list.</summary>
	[Fact]
	public void AnEmptyLanguageArrayInTheRepositoryOverridesANonEmptyProfileList()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"enabledLanguages\": [ \"CSharp\", \"Go\" ] }");
		ConfigurationFile.WriteInto(repositoryFolder, "{ \"enabledLanguages\": [] }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.False(configuration.HasAnyEnabledLanguage);
		Assert.Equal([AssessedLanguage.CSharp, AssessedLanguage.Go], configuration.Provenance.RepositoryEffect.DisabledLanguages.ToArray());
	}

	/// <summary>
	/// A result with zero findings in a language must be distinguishable from a result with
	/// that language disabled. The provenance therefore carries all eight with a flag, not a list
	/// of the enabled ones - a list cannot express the second.
	/// </summary>
	[Fact]
	public void TheProvenanceCarriesAllEightLanguagesEnabledOrNot()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"enabledLanguages\": [ \"CSharp\" ] }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		AssessedLanguage[] coveredLanguages =
		[
			.. configuration.Provenance.LanguageEnablement.Select(enablement => enablement.Language)
		];

		Assert.Equal(
			AssessedLanguages.SUPPORTED_LANGUAGE_COUNT,
			configuration.Provenance.LanguageEnablement.Length);
		AssessedLanguage[] expectedLanguages = [.. AssessedLanguages.AllInDeclaredOrder];

		Assert.Equal(expectedLanguages, coveredLanguages);
		Assert.Contains(
			configuration.Provenance.LanguageEnablement,
			enablement => enablement.Language is AssessedLanguage.Go && !enablement.IsEnabled);
	}

	// ---- defaultFlow ----

	[Fact]
	public void WithNoDefaultFlowAnywhereTheDefaultIsClientCredentialsFromTheBuiltInDefault()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(CredentialFlow.ClientCredentials, configuration.DefaultFlow);
		Assert.Equal(ConfigurationSource.BuiltInDefault, configuration.Provenance.DefaultFlowOrigin);
		Assert.Empty(configuration.Provenance.IgnoredRepositoryFields);
	}

	[Theory]
	[InlineData("AuthorizationCode", CredentialFlow.AuthorizationCode)]
	[InlineData("authorizationcode", CredentialFlow.AuthorizationCode)]
	[InlineData("ClientCredentials", CredentialFlow.ClientCredentials)]
	public void TheUserProfileDefaultFlowIsReadAndItsOriginIsTheProfile(string declaredValue, CredentialFlow expectedFlow)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, $"{{ \"defaultFlow\": \"{declaredValue}\" }}");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(expectedFlow, configuration.DefaultFlow);
		Assert.Equal(ConfigurationSource.UserProfileFile, configuration.Provenance.DefaultFlowOrigin);
	}

	/// <summary>the fourth field, beside the output path, on the same shape.</summary>
	[Fact]
	public void TheRepositoryFileDefaultFlowIsNeverReadAndItsValueIsNeverRecorded()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(repositoryFolder, "{ \"defaultFlow\": \"AuthorizationCode\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(CredentialFlow.ClientCredentials, configuration.DefaultFlow);
		Assert.Equal(ConfigurationSource.BuiltInDefault, configuration.Provenance.DefaultFlowOrigin);

		IgnoredRepositoryConfigurationField ignoredField =
			Assert.Single(configuration.Provenance.IgnoredRepositoryFields);

		Assert.Equal(ConfigurationField.DefaultFlow, ignoredField.Field);
		Assert.Equal(ConfigurationFieldIgnoredReason.NotReadFromTheRepository, ignoredField.Reason);
		Assert.Equal(string.Empty, ignoredField.WouldHaveBeen);
	}

	[Fact]
	public void ConfigurationFieldHasTheOutputPathAndTheDefaultFlowAsSeparateMembers()
	{
		ConfigurationField[] members = Enum.GetValues<ConfigurationField>();

		Assert.Equal(_CONFIGURATION_FIELD_COUNT, members.Length);
		Assert.Contains(ConfigurationField.OutputPath, members);
		Assert.Contains(ConfigurationField.DefaultFlow, members);
	}

	[Fact]
	public void TheRepositoryFileNeverOverridesTheUserProfileDefaultFlow()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"defaultFlow\": \"AuthorizationCode\" }");
		ConfigurationFile.WriteInto(repositoryFolder, "{ \"defaultFlow\": \"ClientCredentials\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(CredentialFlow.AuthorizationCode, configuration.DefaultFlow);
		Assert.Equal(ConfigurationSource.UserProfileFile, configuration.Provenance.DefaultFlowOrigin);
		Assert.Equal(ConfigurationField.DefaultFlow, Assert.Single(configuration.Provenance.IgnoredRepositoryFields).Field);
	}

	/// <summary>
	/// Path one: a value the repository's file carries is recorded as present and never
	/// validated, so an unknown value neither malforms that file nor costs it the fields
	/// it may still carry.
	/// </summary>
	[Fact]
	public void AnUnknownRepositoryDefaultFlowDoesNotMalformTheRepositoryFileOrDropItsOtherFields()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"enabledLanguages\": [ \"CSharp\" ] }");
		ConfigurationFile.WriteInto(
			repositoryFolder,
			"{ \"defaultFlow\": \"Implicit\", \"enabledLanguages\": [ \"Go\" ], \"minimumConfidenceLevel\": \"Low\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Accepted, ConditionOf(configuration, ConfigurationFileLocation.RepositoryRoot));
		Assert.True(configuration.IsEnabled(AssessedLanguage.Go));
		Assert.False(configuration.IsEnabled(AssessedLanguage.CSharp));
		Assert.Equal(CredentialFlow.ClientCredentials, configuration.DefaultFlow);
		Assert.Contains(
			configuration.Provenance.IgnoredRepositoryFields,
			ignored => ignored.Field is ConfigurationField.DefaultFlow
				&& ignored.Reason is ConfigurationFieldIgnoredReason.NotReadFromTheRepository);
	}

	/// <summary>
	/// Path two: on the profile read path an unknown value is validated, and only that key is
	/// ignored. The built-in default flow applies, and the profile's other keys - the output path
	/// above all, which decides where the report is written - still apply.
	/// </summary>
	[Theory]
	[InlineData("\"Implicit\"")]
	[InlineData("\"ResourceOwner\"")]
	[InlineData("\"Code\"")]
	[InlineData("\"Client Credentials\"")]
	[InlineData("true")]
	public void AnUnknownUserProfileDefaultFlowIsIgnoredAloneAndTheOutputPathStillApplies(string declaredValue)
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(
			profileFolder,
			$"{{ \"defaultFlow\": {declaredValue}, \"minimumConfidenceLevel\": \"High\", \"outputPath\": \"{_SUPPLIED_OUTPUT_PATH}\" }}");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Accepted, ConditionOf(configuration, ConfigurationFileLocation.UserProfile));
		Assert.Equal(CredentialFlow.ClientCredentials, configuration.DefaultFlow);
		Assert.Equal(ConfigurationSource.BuiltInDefault, configuration.Provenance.DefaultFlowOrigin);
		Assert.Equal(_SUPPLIED_OUTPUT_PATH, configuration.OutputPathText);
		Assert.Equal(ConfigurationSource.UserProfileFile, configuration.Provenance.OutputPathOrigin);
		Assert.Equal(ConfidenceLevel.High, configuration.MinimumConfidenceLevel);
		Assert.Equal(
			_DEFAULT_FLOW_KEY,
			Assert.Single(configuration.FileDiagnostics, IsInvalidValue).Subject);
	}

	/// <summary>An output path in the profile that is not text is ignored alone, and the working-directory fallback applies.</summary>
	[Fact]
	public void AUserProfileOutputPathThatIsNotTextIsIgnoredAloneAndTheDefaultFlowStillApplies()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolder, "{ \"outputPath\": 42, \"defaultFlow\": \"AuthorizationCode\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Accepted, ConditionOf(configuration, ConfigurationFileLocation.UserProfile));
		Assert.Null(configuration.OutputPathText);
		Assert.Equal(ConfigurationSource.WorkingDirectoryAtLaunch, configuration.Provenance.OutputPathOrigin);
		Assert.Equal(CredentialFlow.AuthorizationCode, configuration.DefaultFlow);
		Assert.Equal(
			_OUTPUT_PATH_KEY,
			Assert.Single(configuration.FileDiagnostics, IsInvalidValue).Subject);
	}

	/// <summary>
	/// Path one, for a value that is not even text: the two keys never read from the
	/// repository are recorded as present and ignored, never validated, and cost the file nothing.
	/// </summary>
	[Fact]
	public void RepositoryProfileOnlyKeysThatAreNotTextAreRecordedAsIgnoredAndNotValidated()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolder = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(
			repositoryFolder,
			"{ \"defaultFlow\": 7, \"outputPath\": [ \"x\" ], \"minimumConfidenceLevel\": \"Medium\" }");

		EffectiveConfiguration configuration = Resolve(repositoryFolder, profileFolder);

		Assert.Equal(ConfigurationFileCondition.Accepted, ConditionOf(configuration, ConfigurationFileLocation.RepositoryRoot));
		Assert.Equal(ConfidenceLevel.Medium, configuration.MinimumConfidenceLevel);
		Assert.Equal(
			[ConfigurationField.OutputPath, ConfigurationField.DefaultFlow],
			configuration.Provenance.IgnoredRepositoryFields.Select(ignored => ignored.Field).Order());
		Assert.DoesNotContain(configuration.FileDiagnostics, IsInvalidValue);
	}

	/// <summary>
	/// On Windows the profile's isolation is proved in-process. The profile folder
	/// is an argument, so an empty folder is a profile that holds nothing whatever the machine holds,
	/// and only the folder passed decides the outcome. No test-only override exists to do it.
	/// </summary>
	[Fact]
	public void TheProfileFolderArgumentAloneDecidesTheDefaultFlow()
	{
		using TemporaryFolder repositoryFolder = TemporaryFolder.Create();
		using TemporaryFolder emptyProfileFolder = TemporaryFolder.Create();
		using TemporaryFolder profileFolderWithAFlow = TemporaryFolder.Create();

		ConfigurationFile.WriteInto(profileFolderWithAFlow, "{ \"defaultFlow\": \"AuthorizationCode\" }");

		EffectiveConfiguration isolated = Resolve(repositoryFolder, emptyProfileFolder);
		EffectiveConfiguration configured = Resolve(repositoryFolder, profileFolderWithAFlow);

		Assert.Equal(ConfigurationSource.BuiltInDefault, isolated.Provenance.DefaultFlowOrigin);
		Assert.Equal(CredentialFlow.ClientCredentials, isolated.DefaultFlow);
		Assert.Equal(ConfigurationSource.UserProfileFile, configured.Provenance.DefaultFlowOrigin);
		Assert.Equal(CredentialFlow.AuthorizationCode, configured.DefaultFlow);
		Assert.All(
			isolated.Provenance.ConfigurationFiles,
			fileState => Assert.Equal(ConfigurationFileCondition.Absent, fileState.Condition));
	}

	private static EffectiveConfiguration Resolve(
		TemporaryFolder repositoryFolder,
		TemporaryFolder profileFolder) =>
		ConfigurationResolver.Resolve(RepositoryOf(repositoryFolder), null, profileFolder.Path);

	private static RepositoryFolderPath RepositoryOf(TemporaryFolder folder) =>
		RepositoryFolderPath.FromSuppliedText(folder.Path);

	private static bool IsInvalidValue(ConfigurationFileDiagnostic diagnostic) =>
		diagnostic.Kind is ConfigurationDiagnosticKind.InvalidValue;

	private static ConfigurationFileCondition ConditionOf(
		EffectiveConfiguration configuration,
		ConfigurationFileLocation location) =>
		configuration.Provenance.ConfigurationFiles
			.Single(fileState => fileState.Location == location)
			.Condition;
}
