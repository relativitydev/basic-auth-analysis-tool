using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Configuration;

/// <summary>
/// The resolution chain.
///
/// THE CHAIN. Enabled languages and the confidence threshold resolve from the file in the
/// scanned repository's root if present, otherwise the file in the user's profile folder,
/// otherwise the values built into the binary. A valid value in the repository's file REPLACES
/// the one the next source would have supplied, whichever way it moves: a repository that says
/// which languages apply to it, or at which level its findings matter, is the better authority on
/// its own assessment. The absence of any file at any level is not an error and produces no
/// message - it is the ordinary case.
///
/// WHAT KEEPS THAT FROM BEING A WAY TO HIDE FINDINGS IS THAT IT IS NEVER SILENT. Relative to what
/// would have applied without the repository's file, the resolver records the languages the
/// repository disabled, the languages it enabled that were not, and the threshold it changed, in
/// <see cref="RepositoryConfigurationEffect"/>. Pre-flight says it at start-up and the report and
/// the data artifact carry it, so a client can see when their repository narrowed its own
/// assessment.
///
/// TWO FIELDS ARE NEVER READ FROM THE REPOSITORY AT ALL. The output path and the default flow are
/// taken from the command line, the user profile and the built-in default only, on the principle
/// that the audited object must not govern the auditor. A value of either in the repository's
/// file is listed as ignored, with no value.
///
/// ONCE, AND NEVER AGAIN. Pre-flight Validation runs this before <c>scan</c> becomes available and does not
/// repeat it per stage. The result is a session-scoped constant, which is the property the provenance
/// record depends on.
/// </summary>
public static class ConfigurationResolver
{
	/// <summary>
	/// The file name the tool fixes. The same name at both locations, which is what lets one
	/// reader serve the repository root and the user profile.
	/// </summary>
	public const string CONFIGURATION_FILE_NAME = ".migrate-auth.json";

	/// <summary>
	/// Resolves against the running environment: the user's profile folder is where the
	/// second file lives. The working directory is deliberately NOT read here - the
	/// fallback for an unsupplied output path belongs to <see cref="SessionStartup"/>, which
	/// already captures it once at launch, and deriving it twice would let the two disagree.
	/// </summary>
	public static EffectiveConfiguration Resolve(
		RepositoryFolderPath repositoryFolder,
		CommandLineParseResult parsedCommandLine)
	{
		ArgumentNullException.ThrowIfNull(parsedCommandLine);

		return Resolve(
			repositoryFolder,
			parsedCommandLine.DocumentationDestinationFolderPathText,
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
	}

	/// <summary>
	/// The chain itself, with both locations supplied. Touches the filesystem only to read the
	/// two files and decides nothing about them beyond what the chain states.
	/// </summary>
	public static EffectiveConfiguration Resolve(
		RepositoryFolderPath repositoryFolder,
		string? commandLineOutputPathText,
		string userProfileFolderPath)
	{
		ConfigurationFileRead repositoryRead = ConfigurationFileReader.ReadFrom(
			repositoryFolder.CanonicalPath,
			ConfigurationFileLocation.RepositoryRoot);
		ConfigurationFileRead profileRead = ConfigurationFileReader.ReadFrom(
			userProfileFolderPath,
			ConfigurationFileLocation.UserProfile);

		ImmutableArray<IgnoredRepositoryConfigurationField>.Builder ignoredFields =
			ImmutableArray.CreateBuilder<IgnoredRepositoryConfigurationField>();

		ResolvedLanguages languages = ResolveLanguages(profileRead.Document, repositoryRead.Document);
		ResolvedThreshold threshold = ResolveThreshold(profileRead.Document, repositoryRead.Document);
		ResolvedOutputPath outputPath = ResolveOutputPath(
			commandLineOutputPathText,
			profileRead.Document,
			repositoryRead.Document,
			ignoredFields);
		ResolvedDefaultFlow defaultFlow = ResolveDefaultFlow(profileRead.Document, repositoryRead.Document, ignoredFields);

		return new EffectiveConfiguration
		{
			LanguageEnablement = languages.Enablement,
			MinimumConfidenceLevel = threshold.Level,
			OutputPathText = outputPath.Text,
			DefaultFlow = defaultFlow.Flow,
			Provenance = new ConfigurationProvenance
			{
				EnabledLanguagesOrigin = languages.Origin,
				MinimumConfidenceLevelOrigin = threshold.Origin,
				OutputPathOrigin = outputPath.Origin,
				DefaultFlowOrigin = defaultFlow.Origin,
				LanguageEnablement = languages.Enablement,
				IgnoredRepositoryFields = ignoredFields.ToImmutable(),
				RepositoryEffect = BuildRepositoryEffect(languages, threshold),
				ConfigurationFiles =
				[
					BuildFileState(ConfigurationFileLocation.RepositoryRoot, repositoryRead.Condition),
					BuildFileState(ConfigurationFileLocation.UserProfile, profileRead.Condition)
				]
			},
			FileDiagnostics = [.. repositoryRead.Diagnostics, .. profileRead.Diagnostics]
		};
	}

	private static RepositoryConfigurationEffect BuildRepositoryEffect(
		ResolvedLanguages languages,
		ResolvedThreshold threshold) =>
		new()
		{
			LanguagesSetByRepository = languages.SetByRepository,
			DisabledLanguages = languages.Disabled,
			EnabledLanguages = languages.Enabled,
			ThresholdChange = threshold.Change
		};

	/// <summary>
	/// The repository's list, when it has one, REPLACES the profile's or the built-in default; the
	/// difference between the two is recorded as the languages the repository disabled and the
	/// ones it enabled. An unknown language name in the repository's list is skipped, so a list
	/// naming only unknown languages replaces the set with an empty one, and pre-flight refuses it.
	/// </summary>
	private static ResolvedLanguages ResolveLanguages(
		ConfigurationDocument? profile,
		ConfigurationDocument? repository)
	{
		HashSet<AssessedLanguage>? profileLanguages = ReadLanguageSet(profile);
		HashSet<AssessedLanguage>? repositoryLanguages = ReadLanguageSet(repository);

		HashSet<AssessedLanguage> baseline = profileLanguages ?? [.. AssessedLanguages.AllInDeclaredOrder];
		ConfigurationSource baselineOrigin = profileLanguages is null
			? ConfigurationSource.BuiltInDefault
			: ConfigurationSource.UserProfileFile;

		ResolvedLanguages resolved = new(
			BuildEnablement(baseline),
			baselineOrigin,
			SetByRepository: [],
			Disabled: [],
			Enabled: []);

		if (repositoryLanguages is not null)
		{
			resolved = new ResolvedLanguages(
				BuildEnablement(repositoryLanguages),
				ConfigurationSource.RepositoryFile,
				SetByRepository: [.. repositoryLanguages.Order()],
				Disabled: [.. baseline.Except(repositoryLanguages).Order()],
				Enabled: [.. repositoryLanguages.Except(baseline).Order()]);
		}

		return resolved;
	}

	/// <summary>
	/// The repository's level, when it names a valid one, replaces the profile's or the built-in
	/// default in either direction. A different level is recorded with the level it replaced, so
	/// that a raise - which hides findings - can be said and counted where it matters.
	/// </summary>
	private static ResolvedThreshold ResolveThreshold(
		ConfigurationDocument? profile,
		ConfigurationDocument? repository)
	{
		ConfidenceLevel baseline = ConfidenceLevels.WIDEST;
		ConfigurationSource baselineOrigin = ConfigurationSource.BuiltInDefault;

		if (ConfidenceLevels.TryParse(profile?.MinimumConfidenceLevel, out ConfidenceLevel profileLevel))
		{
			baseline = profileLevel;
			baselineOrigin = ConfigurationSource.UserProfileFile;
		}

		ResolvedThreshold resolved = new(baseline, baselineOrigin, Change: null);

		if (ConfidenceLevels.TryParse(repository?.MinimumConfidenceLevel, out ConfidenceLevel repositoryLevel))
		{
			resolved = new ResolvedThreshold(
				repositoryLevel,
				ConfigurationSource.RepositoryFile,
				repositoryLevel == baseline
					? null
					: new ConfidenceLevelChange { Previous = baseline, Current = repositoryLevel });
		}

		return resolved;
	}

	/// <summary>
	/// The command line, then the user profile, then nothing - and the repository's own value is
	/// never one of the candidates. It is recorded as ignored when present so that a client can
	/// see the tool declined to be aimed by the thing it is auditing, and the value itself is NOT
	/// recorded, because a path out of the repository is what must never be emitted.
	/// </summary>
	private static ResolvedOutputPath ResolveOutputPath(
		string? commandLineOutputPathText,
		ConfigurationDocument? profile,
		ConfigurationDocument? repository,
		ImmutableArray<IgnoredRepositoryConfigurationField>.Builder ignoredFields)
	{
		if (!string.IsNullOrWhiteSpace(repository?.OutputPath))
		{
			ignoredFields.Add(BuildIgnoredField(
				ConfigurationField.OutputPath,
				ConfigurationFieldIgnoredReason.NotReadFromTheRepository,
				string.Empty));
		}

		ResolvedOutputPath resolved;

		if (!string.IsNullOrWhiteSpace(commandLineOutputPathText))
		{
			resolved = new ResolvedOutputPath(commandLineOutputPathText, ConfigurationSource.CommandLine);
		}
		else if (!string.IsNullOrWhiteSpace(profile?.OutputPath))
		{
			resolved = new ResolvedOutputPath(profile.OutputPath, ConfigurationSource.UserProfileFile);
		}
		else
		{
			resolved = new ResolvedOutputPath(null, ConfigurationSource.WorkingDirectoryAtLaunch);
		}

		return resolved;
	}

	/// <summary>
	/// The user profile, then the built-in default - and the repository's own value is never one of
	/// the candidates, on the output path's shape. It is recorded as ignored when present, whatever
	/// it says: the value is not validated on this path and is not recorded, so the
	/// repository can neither aim the assessment nor malform its own file with it.
	/// </summary>
	private static ResolvedDefaultFlow ResolveDefaultFlow(
		ConfigurationDocument? profile,
		ConfigurationDocument? repository,
		ImmutableArray<IgnoredRepositoryConfigurationField>.Builder ignoredFields)
	{
		if (!string.IsNullOrWhiteSpace(repository?.DefaultFlow))
		{
			ignoredFields.Add(BuildIgnoredField(
				ConfigurationField.DefaultFlow,
				ConfigurationFieldIgnoredReason.NotReadFromTheRepository,
				string.Empty));
		}

		ResolvedDefaultFlow resolved = new(CredentialFlows.BUILT_IN_DEFAULT, ConfigurationSource.BuiltInDefault);

		if (CredentialFlows.TryParse(profile?.DefaultFlow, out CredentialFlow profileFlow))
		{
			resolved = new ResolvedDefaultFlow(profileFlow, ConfigurationSource.UserProfileFile);
		}

		return resolved;
	}

	/// <summary>
	/// Null when the file says nothing about languages, which falls through to the next source.
	/// An empty set is a different answer and is kept: it is a configuration that enables none,
	/// and Pre-flight Validation's language check exists precisely for it.
	/// </summary>
	private static HashSet<AssessedLanguage>? ReadLanguageSet(ConfigurationDocument? document)
	{
		HashSet<AssessedLanguage>? languages = null;

		if (document?.EnabledLanguages is string[] declaredNames)
		{
			languages = [];

			foreach (string declaredName in declaredNames)
			{
				if (AssessedLanguages.TryParse(declaredName, out AssessedLanguage language))
				{
					languages.Add(language);
				}
			}
		}

		return languages;
	}

	private static ImmutableArray<LanguageEnablement> BuildEnablement(HashSet<AssessedLanguage> enabledLanguages) =>
	[
		.. AssessedLanguages.AllInDeclaredOrder.Select(language => new LanguageEnablement
		{
			Language = language,
			IsEnabled = enabledLanguages.Contains(language)
		})
	];

	private static IgnoredRepositoryConfigurationField BuildIgnoredField(
		ConfigurationField field,
		ConfigurationFieldIgnoredReason reason,
		string wouldHaveBeen) =>
		new()
		{
			Field = field,
			Reason = reason,
			WouldHaveBeen = wouldHaveBeen
		};

	private static ConfigurationFileState BuildFileState(
		ConfigurationFileLocation location,
		ConfigurationFileCondition condition) =>
		new()
		{
			Location = location,
			Condition = condition
		};

	/// <summary>
	/// The enabled-language outcome, where it came from, and what the repository's list changed
	/// against the outcome it would otherwise have had.
	/// </summary>
	private readonly record struct ResolvedLanguages(
		ImmutableArray<LanguageEnablement> Enablement,
		ConfigurationSource Origin,
		ImmutableArray<AssessedLanguage> SetByRepository,
		ImmutableArray<AssessedLanguage> Disabled,
		ImmutableArray<AssessedLanguage> Enabled);

	/// <summary>The threshold outcome, where it came from, and the change the repository made to it, if any.</summary>
	private readonly record struct ResolvedThreshold(
		ConfidenceLevel Level,
		ConfigurationSource Origin,
		ConfidenceLevelChange? Change);

	/// <summary>The output-path outcome and where it came from. Null text is the working-directory fallback.</summary>
	private readonly record struct ResolvedOutputPath(string? Text, ConfigurationSource Origin);

	/// <summary>The default-flow outcome and where it came from.</summary>
	private readonly record struct ResolvedDefaultFlow(CredentialFlow Flow, ConfigurationSource Origin);
}
