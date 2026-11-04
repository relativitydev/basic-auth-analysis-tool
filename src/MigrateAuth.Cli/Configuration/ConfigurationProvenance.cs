using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Configuration;

/// <summary>Where an effective configuration value actually came from.</summary>
public enum ConfigurationSource
{
	/// <summary>Nothing supplied a value, so the value compiled into the binary applies.</summary>
	BuiltInDefault = 0,

	/// <summary>The <c>.migrate-auth.json</c> in the user's profile folder.</summary>
	UserProfileFile = 1,

	/// <summary>
	/// The <c>.migrate-auth.json</c> in the root of the repository under assessment. It comes
	/// first in the chain, so a valid <c>enabledLanguages</c> or
	/// <c>minimumConfidenceLevel</c> in it replaces the value the user profile or the built-in
	/// default would have supplied - narrowing included, which is then recorded in
	/// <see cref="RepositoryConfigurationEffect"/> rather than left implicit.
	/// </summary>
	RepositoryFile = 2,

	/// <summary>A named option on the command line.</summary>
	CommandLine = 3,

	/// <summary>
	/// The working directory captured at launch. The fallback for the output destination when
	/// neither the command line nor the user profile supplies one. It is a source rather than a
	/// fourth policy, which is why the repository still cannot govern where its audit lands.
	/// </summary>
	WorkingDirectoryAtLaunch = 4
}

/// <summary>The fields the configuration file carries: the three (languages, thresholds, output paths), and the default flow.</summary>
public enum ConfigurationField
{
	/// <summary>Which of the eight languages are assessed.</summary>
	EnabledLanguages = 0,

	/// <summary>The lowest confidence level that appears in output.</summary>
	MinimumConfidenceLevel = 1,

	/// <summary>Where the report and the data artifact are written.</summary>
	OutputPath = 2,

	/// <summary>The flow a finding is assigned when no flow signal decided it.</summary>
	DefaultFlow = 3
}

/// <summary>Why a field of the repository's own configuration file had no effect.</summary>
public enum ConfigurationFieldIgnoredReason
{
	/// <summary>
	/// It is never read from the repository at all. The output path and the default flow are
	/// removed from the repository-resolved file outright, so the audited object cannot govern the
	/// auditor and the state file is never written inside the scanned repository.
	/// </summary>
	NotReadFromTheRepository = 0
}

/// <summary>Which of the two files a condition belongs to.</summary>
public enum ConfigurationFileLocation
{
	/// <summary>The root of the repository under assessment. Read first.</summary>
	RepositoryRoot = 0,

	/// <summary>The user's profile folder. Read second.</summary>
	UserProfile = 1
}

/// <summary>What was found at one of the two locations.</summary>
public enum ConfigurationFileCondition
{
	/// <summary>No file there. Not an error at any level and says nothing.</summary>
	Absent = 0,

	/// <summary>A file was read and its contents applied.</summary>
	Accepted = 1,

	/// <summary>Found, and not the expected structure. Degrades rather than failing.</summary>
	Malformed = 2,

	/// <summary>Found and impossible to open. Degrades identically, and says something different.</summary>
	Unreadable = 3
}

/// <summary>
/// One field of the repository's configuration file that had no effect, and what it asked for.
///
/// IT CARRIES NO TEXT FROM THE REPOSITORY. <see cref="WouldHaveBeen"/> is composed from this
/// product's own closed vocabularies - language canonical names and confidence level names - and
/// never from the file's raw contents, because this record is rendered into a report a client
/// receives. The two fields that can be ignored, the
/// output path and the default flow, carry no value at all: a path from the audited repository is
/// precisely what may not be emitted, and the flow is never inspected there.
/// </summary>
public sealed record IgnoredRepositoryConfigurationField
{
	/// <summary>Which configuration field.</summary>
	public required ConfigurationField Field { get; init; }

	/// <summary>Why it had no effect.</summary>
	public required ConfigurationFieldIgnoredReason Reason { get; init; }

	/// <summary>
	/// What the repository asked for, in this product's own vocabulary. Empty for the output
	/// path and the default flow, whose values may not be emitted.
	/// </summary>
	public required string WouldHaveBeen { get; init; }
}

/// <summary>One of the two locations and the condition its file was in.</summary>
public sealed record ConfigurationFileState
{
	/// <summary>Which file.</summary>
	public required ConfigurationFileLocation Location { get; init; }

	/// <summary>What was found there.</summary>
	public required ConfigurationFileCondition Condition { get; init; }

	/// <summary>Whether this condition is one the session is required to announce.</summary>
	public bool RequiresDegradationNotice =>
		Condition is ConfigurationFileCondition.Malformed or ConfigurationFileCondition.Unreadable;
}

/// <summary>A change of the confidence threshold that the repository's file made, from what would otherwise have applied.</summary>
public sealed record ConfidenceLevelChange
{
	/// <summary>The level that would have applied without the repository's file.</summary>
	public required ConfidenceLevel Previous { get; init; }

	/// <summary>The level the repository's file set, which is the one in effect.</summary>
	public required ConfidenceLevel Current { get; init; }

	/// <summary>Whether the change hides findings the previous level would have shown.</summary>
	public bool IsRaise => Current > Previous;
}

/// <summary>
/// What the repository's own file changed, measured against what would have applied without it -
/// the user profile's value where there is one, the built-in default otherwise.
///
/// A repository file may narrow its own assessment, and the chain puts it first
/// for that reason. What keeps that from being a way to hide findings is that the narrowing is
/// never silent: this record is what Pre-flight says at start-up and what the report and the data
/// artifact carry. The number of findings a raised threshold hides is known only when the report
/// is built, so it is not stored here; <see cref="ThresholdChange"/> holds what a report needs to
/// count it.
///
/// IT CARRIES ONLY THIS PRODUCT'S CLOSED VOCABULARIES - language enumeration members and level
/// enumeration members - and never text from the file. A language
/// name this build does not know is therefore absent from it.
/// </summary>
public sealed record RepositoryConfigurationEffect
{
	/// <summary>The record for a repository file that changed nothing, or that is not there.</summary>
	public static RepositoryConfigurationEffect None { get; } = new();

	/// <summary>
	/// The languages the repository's file named, in the enumeration's declared order; empty when
	/// it named none or said nothing about languages.
	/// </summary>
	public ImmutableArray<AssessedLanguage> LanguagesSetByRepository { get; init; } = [];

	/// <summary>Languages that would have been enabled without the repository's file and are not.</summary>
	public ImmutableArray<AssessedLanguage> DisabledLanguages { get; init; } = [];

	/// <summary>Languages the repository's file enabled that would not otherwise have been.</summary>
	public ImmutableArray<AssessedLanguage> EnabledLanguages { get; init; } = [];

	/// <summary>The threshold change the repository's file made, or null when it made none.</summary>
	public ConfidenceLevelChange? ThresholdChange { get; init; }

	/// <summary>Whether the repository's file changed which languages are enabled.</summary>
	public bool ChangesLanguages => DisabledLanguages.Length > 0 || EnabledLanguages.Length > 0;

	/// <summary>Whether the repository's file made the assessment cover less than it would have.</summary>
	public bool NarrowsReporting => DisabledLanguages.Length > 0 || ThresholdChange?.IsRaise == true;
}

/// <summary>Whether one language is assessed in this session.</summary>
public sealed record LanguageEnablement
{
	/// <summary>The language.</summary>
	public required AssessedLanguage Language { get; init; }

	/// <summary>Whether this session assesses it.</summary>
	public required bool IsEnabled { get; init; }
}

/// <summary>
/// The provenance record for the configuration: for each of the three fields,
/// where the effective value came from, what the repository's own file changed, and everything it
/// asked for that did not happen.
///
/// IT IS DATA HERE AND RENDERED LATER, in the report and the data artifact. It is meaningful
/// because the effective configuration is a SESSION-SCOPED CONSTANT - resolved once
/// before the prompt exists, never re-read per stage. A provenance record of something re-read
/// per stage would describe the stage that happened to read it rather than the session.
///
/// <see cref="LanguageEnablement"/> covers all eight values of the enumeration and not only the
/// enabled ones, because a result with zero findings in a language must be
/// distinguishable from a result with that language disabled. A list of enabled languages alone
/// cannot express the second.
/// </summary>
public sealed record ConfigurationProvenance
{
	/// <summary>Where the effective set of enabled languages came from.</summary>
	public required ConfigurationSource EnabledLanguagesOrigin { get; init; }

	/// <summary>Where the effective confidence threshold came from.</summary>
	public required ConfigurationSource MinimumConfidenceLevelOrigin { get; init; }

	/// <summary>Where the effective output destination came from.</summary>
	public required ConfigurationSource OutputPathOrigin { get; init; }

	/// <summary>
	/// Where the effective default flow came from: <see cref="ConfigurationSource.UserProfileFile"/>
	/// where the profile sets it, <see cref="ConfigurationSource.BuiltInDefault"/> otherwise. The
	/// repository's file is never a source, only an entry in <see cref="IgnoredRepositoryFields"/>.
	/// </summary>
	public ConfigurationSource DefaultFlowOrigin { get; init; } = ConfigurationSource.BuiltInDefault;

	/// <summary>Every language, enabled or not, in the enumeration's declared order.</summary>
	public required ImmutableArray<LanguageEnablement> LanguageEnablement { get; init; }

	/// <summary>What the repository's file asked for and did not get. Empty is the ordinary case.</summary>
	public required ImmutableArray<IgnoredRepositoryConfigurationField> IgnoredRepositoryFields { get; init; }

	/// <summary>What the repository's file changed, narrowing included. Empty is the ordinary case.</summary>
	public RepositoryConfigurationEffect RepositoryEffect { get; init; } = RepositoryConfigurationEffect.None;

	/// <summary>The condition of each of the two files, including the absent ones.</summary>
	public required ImmutableArray<ConfigurationFileState> ConfigurationFiles { get; init; }
}
