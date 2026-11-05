using System.Collections.Frozen;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// EVERY CLOSED VOCABULARY THIS PRODUCT PRINTS, IN ONE PLACE AND DECLARED AS DATA.
///
/// Two rules meet here. The names are declared rather than derived from the enumeration members,
/// for the reason <see cref="AssessedLanguages"/> and <see cref="ConfidenceLevels"/> both give:
/// under ahead-of-time compilation a reflective name lookup is the failure that compiles,
/// publishes and breaks on the client's machine - and it also makes the emitted name a decision
/// that a rename cannot change by accident. And each table refuses to build unless every member
/// of its enumeration carries a name, so a category added without a name here does not start.
///
/// It sits in this namespace and not beside the enumerations because these are PRESENTATION
/// names. The enumeration is the vocabulary of the engine; how it reads in a report is the
/// report's business, and a wording change should not reach into the reading set.
/// </summary>
public static class ReportVocabulary
{
	/// <summary>
	/// The one sentence Section 2 adds when first-line detection tripped the lexical budget for
	/// at least one file: the count of files attributed by their extension alone as a result.
	/// </summary>
	public const string FIRST_LINE_FALLBACK_FORMAT =
		"Files attributed by extension because first-line detection exceeded its budget: {0:N0}.";

	/// <summary>
	/// The label for findings whose destination the construct itself establishes. A construct anchored
	/// on a credential type from the Relativity SDK packages establishes its destination, and nothing
	/// else does - in either direction.
	/// </summary>
	public const string DESTINATION_ESTABLISHED_LABEL = "established";

	/// <summary>
	/// The label for the other group: "not established" rather than "not Relativity". The engine can no
	/// more prove the negative than the positive, and a reader who believed "not Relativity" would discard
	/// a finding that was Relativity's after all.
	/// </summary>
	public const string DESTINATION_NOT_ESTABLISHED_LABEL = "not established";

	/// <summary>The value the data artifact carries, one of the declared pair.</summary>
	public const string DESTINATION_CERTAINTY_ESTABLISHED_VALUE = "established";

	/// <summary>The artifact's <c>defaultFlowOrigin</c> when no profile set the default flow.</summary>
	public const string DEFAULT_FLOW_ORIGIN_BUILT_IN_VALUE = "BuiltInDefault";

	/// <summary>The artifact's <c>defaultFlowOrigin</c> when the profile set the default flow.</summary>
	public const string DEFAULT_FLOW_ORIGIN_PROFILE_VALUE = "Profile";

	/// <summary>The other value of the pair. It is "unknown" in the data and "not established" in prose.</summary>
	public const string DESTINATION_CERTAINTY_UNKNOWN_VALUE = "unknown";

	/// <summary>
	/// How a finding's pattern class renders in the Markdown report and the console
	/// table: the catalog's <see cref="DetectionRule.DisplayName"/>, with the identifier kept
	/// visible in parentheses so a reader can still cross-reference the artifact, which continues to
	/// key on the identifier alone.
	/// </summary>
	private const string _PATTERN_CLASS_WITH_IDENTIFIER_FORMAT = "{0} ({1})";

	private static readonly FrozenDictionary<SkippedItemCategory, string> _skippedItemCategoryNames =
		BuildSkippedItemCategoryNames();

	/// <summary>
	/// A client reader's version of <see cref="_skippedItemCategoryNames"/>'s eleven entries:
	/// what the category means and, where there is one, what to do about it, rather than the engine's
	/// own vocabulary for it. The two are declared and required separately for the same structural
	/// reason - <see cref="RequireOneNamePerMember{TMember}"/> - because a category is not fully
	/// declared until both a stable identifier and a reader-facing sentence exist for it.
	/// </summary>
	private static readonly FrozenDictionary<SkippedItemCategory, string> _skippedItemCategoryDescriptions =
		BuildSkippedItemCategoryDescriptions();

	private static readonly FrozenDictionary<ConfigurationSource, string> _configurationSourceNames =
		BuildConfigurationSourceNames();

	private static readonly FrozenDictionary<ConfigurationField, string> _configurationFieldNames =
		BuildConfigurationFieldNames();

	/// <summary>
	/// Every <see cref="SessionStateRejectionReason"/> a discarded state can
	/// actually carry - <see cref="SessionStateRejectionReason.None"/> and
	/// <see cref="SessionStateRejectionReason.FileAbsent"/> are excluded from the completeness check
	/// itself, not merely unused, because neither is ever a discard: <c>None</c> means the state was
	/// accepted, and <c>FileAbsent</c> is the ordinary first run. Computed from the enumeration rather
	/// than listed twice, so a rejection reason added later and left without text fails here before it
	/// can ever reach a client as a raw enum name.
	/// </summary>
	private static readonly SessionStateRejectionReason[] _rejectionReasonsRequiringClientText =
	[
		.. Enum.GetValues<SessionStateRejectionReason>()
			.Where(reason => reason is not (SessionStateRejectionReason.None or SessionStateRejectionReason.FileAbsent))
	];

	private static readonly FrozenDictionary<SessionStateRejectionReason, string> _sessionStateRejectionReasonDescriptions =
		BuildSessionStateRejectionReasonDescriptions();

	/// <summary>The group label for a finding's destination certainty, in prose.</summary>
	public static string LabelOf(DestinationCertainty certainty)
	{
		return certainty is DestinationCertainty.Established
			? DESTINATION_ESTABLISHED_LABEL
			: DESTINATION_NOT_ESTABLISHED_LABEL;
	}

	/// <summary>The value a structured consumer reads, one of the declared pair.</summary>
	public static string ValueOf(DestinationCertainty certainty)
	{
		return certainty is DestinationCertainty.Established
			? DESTINATION_CERTAINTY_ESTABLISHED_VALUE
			: DESTINATION_CERTAINTY_UNKNOWN_VALUE;
	}

	/// <summary>
	/// What a reader sees where the report or the console table names a finding's pattern class: the
	/// catalog's display name, looked up by identifier. The identifier rides alongside it in
	/// parentheses, so a reader can still cross-reference the JSON artifact, which keys every finding on
	/// the identifier alone.
	///
	/// Falls back to the bare identifier when the catalog carries no rule for it - a finding the
	/// engine produced always has one, but a caller holding a stale catalog reference should not
	/// throw over a display string.
	/// </summary>
	public static string PatternClassText(LanguageCatalog catalog, Finding finding)
	{
		ArgumentNullException.ThrowIfNull(catalog);

		string identifier = finding.PatternClass.Value;
		string languageName = AssessedLanguages.CanonicalName(finding.Language);
		string text = identifier;

		if (catalog.TryGetDetectionRule(languageName, identifier, out DetectionRule? rule) && rule is not null)
		{
			text = string.Format(CultureInfo.InvariantCulture, _PATTERN_CLASS_WITH_IDENTIFIER_FORMAT, rule.DisplayName, identifier);
		}

		return text;
	}

	/// <summary>
	/// What a skipped-item tally counts. It is printed beside every count because adding a directory of
	/// unknown contents into a file count would report a hole of unknown size as a hole of size one.
	/// </summary>
	public static string NameOf(SkippedItemUnit unit)
	{
		return unit is SkippedItemUnit.Directories ? "directories" : "files";
	}

	/// <summary>
	/// What a client reads in place of the raw <see cref="SessionStateRejectionReason"/>
	/// name in <see cref="Session.SessionStartup.SESSION_STATE_DISCARDED_MESSAGE_FORMAT"/>. Never
	/// called for <see cref="SessionStateRejectionReason.None"/> or
	/// <see cref="SessionStateRejectionReason.FileAbsent"/> - <c>SessionStartup.ReportSessionStart</c>
	/// never reaches this method for either, so both are absent from the table by declaration rather
	/// than merely by return value.
	/// </summary>
	public static string DescriptionOf(SessionStateRejectionReason reason)
	{
		if (!_sessionStateRejectionReasonDescriptions.TryGetValue(reason, out string? description))
		{
			throw new ArgumentOutOfRangeException(
				nameof(reason),
				reason,
				"No client description is declared for this rejection reason.");
		}

		return description;
	}

	/// <summary>
	/// Which of the three routes produced a recommendation. Declared as data for the same
	/// reason as everything else here, and additionally because the enumeration's member names are
	/// implementation vocabulary while these are what a consumer of the artifact reads.
	/// </summary>
	public static string NameOf(RecommendationKind kind)
	{
		string name = kind switch
		{
			RecommendationKind.FromTemplate => "template",
			RecommendationKind.ManualReview => "manualReview",
			RecommendationKind.Generic => "generic",
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No report name is declared for this kind.")
		};

		return name;
	}

	/// <summary>
	/// The category's stable identifier, exactly as the data artifact's `category` field carries it.
	/// It never changes for readability - <see cref="DescriptionOf"/> is where a client reads what the
	/// category means.
	/// </summary>
	public static string NameOf(SkippedItemCategory category)
	{
		if (!_skippedItemCategoryNames.TryGetValue(category, out string? name))
		{
			throw new ArgumentOutOfRangeException(nameof(category), category, "No report name is declared for this category.");
		}

		return name;
	}

	/// <summary>
	/// What the category means for a client reading the report, in the same terms the
	/// Markdown Section 3 table and the artifact's additive `description` field both use. Never the
	/// value a consumer keys on - see <see cref="NameOf(SkippedItemCategory)"/> for that.
	/// </summary>
	public static string DescriptionOf(SkippedItemCategory category)
	{
		if (!_skippedItemCategoryDescriptions.TryGetValue(category, out string? description))
		{
			throw new ArgumentOutOfRangeException(nameof(category), category, "No client description is declared for this category.");
		}

		return description;
	}

	/// <summary>Where an effective configuration value came from, as the provenance record states it.</summary>
	public static string NameOf(ConfigurationSource source)
	{
		if (!_configurationSourceNames.TryGetValue(source, out string? name))
		{
			throw new ArgumentOutOfRangeException(nameof(source), source, "No report name is declared for this source.");
		}

		return name;
	}

	/// <summary>
	/// The value a structured consumer reads for where the effective default flow came from: a
	/// closed pair, <c>BuiltInDefault</c> or <c>Profile</c>, rather than the prose
	/// <see cref="NameOf(ConfigurationSource)"/> gives the other origins. The repository's file and the
	/// command line are never a source of this field.
	/// </summary>
	public static string DefaultFlowOriginValueOf(ConfigurationSource source)
	{
		string value = source switch
		{
			ConfigurationSource.BuiltInDefault => DEFAULT_FLOW_ORIGIN_BUILT_IN_VALUE,
			ConfigurationSource.UserProfileFile => DEFAULT_FLOW_ORIGIN_PROFILE_VALUE,
			_ => throw new ArgumentOutOfRangeException(nameof(source), source, "The default flow has no such source.")
		};

		return value;
	}

	/// <summary>
	/// The reverse of <see cref="DefaultFlowOriginValueOf"/>, for the session state file: exactly one
	/// of the two declared values, compared ordinally, and nothing else.
	/// </summary>
	public static bool TryParseDefaultFlowOrigin(string? value, out ConfigurationSource source)
	{
		bool parsed = true;

		if (string.Equals(value, DEFAULT_FLOW_ORIGIN_BUILT_IN_VALUE, StringComparison.Ordinal))
		{
			source = ConfigurationSource.BuiltInDefault;
		}
		else if (string.Equals(value, DEFAULT_FLOW_ORIGIN_PROFILE_VALUE, StringComparison.Ordinal))
		{
			source = ConfigurationSource.UserProfileFile;
		}
		else
		{
			source = ConfigurationSource.BuiltInDefault;
			parsed = false;
		}

		return parsed;
	}

	/// <summary>Which configuration field.</summary>
	public static string NameOf(ConfigurationField field)
	{
		if (!_configurationFieldNames.TryGetValue(field, out string? name))
		{
			throw new ArgumentOutOfRangeException(nameof(field), field, "No report name is declared for this field.");
		}

		return name;
	}

	/// <summary>Why a field of the repository's own file had no effect: it is never read from there.</summary>
	public static string NameOf(ConfigurationFieldIgnoredReason reason)
	{
		return reason switch
		{
			ConfigurationFieldIgnoredReason.NotReadFromTheRepository => "never read from the repository under assessment",
			_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "No report name is declared for this reason.")
		};
	}

	/// <summary>The direction of a threshold change made by the repository's own file: <c>Raised</c> or <c>Lowered</c>.</summary>
	public static string DirectionOf(ConfidenceLevelChange change)
	{
		ArgumentNullException.ThrowIfNull(change);

		return change.IsRaise ? "Raised" : "Lowered";
	}

	/// <summary>Which of the two configuration file locations.</summary>
	public static string NameOf(ConfigurationFileLocation location)
	{
		return location is ConfigurationFileLocation.RepositoryRoot
			? "repository root"
			: "user profile";
	}

	/// <summary>What was found at a configuration file location.</summary>
	public static string NameOf(ConfigurationFileCondition condition)
	{
		string name = condition switch
		{
			ConfigurationFileCondition.Absent => "absent",
			ConfigurationFileCondition.Accepted => "read and applied",
			ConfigurationFileCondition.Malformed => "found and malformed, so built-in defaults applied",
			ConfigurationFileCondition.Unreadable => "found and unreadable, so built-in defaults applied",
			_ => throw new ArgumentOutOfRangeException(nameof(condition), condition, "No report name is declared for this condition.")
		};

		return name;
	}

	private static FrozenDictionary<SkippedItemCategory, string> BuildSkippedItemCategoryNames()
	{
		Dictionary<SkippedItemCategory, string> names = new()
		{
			[SkippedItemCategory.UnreadableFile] = "could not be opened for reading",
			[SkippedItemCategory.UnlistableDirectory] = "directory whose contents could not be listed",
			[SkippedItemCategory.LinkNotFollowed] = "link, junction or reparse point, which is never followed",
			[SkippedItemCategory.Undecodable] = "could not be decoded as text",
			[SkippedItemCategory.OversizeFile] = "above the file size ceiling",
			[SkippedItemCategory.OverLongLine] = "carries a line above the line length ceiling",
			[SkippedItemCategory.PathTooLong] = "path refused by the platform for its length",
			[SkippedItemCategory.LexicalAnomaly] = "lexical scan abandoned part-way through the file",
			[SkippedItemCategory.MatchBoundExceeded] = "matching stopped before every rule had run",
			[SkippedItemCategory.MalformedMatch] = "a rule's match could not be trusted and was dropped",
			[SkippedItemCategory.MissingCandidate] = "recorded during the scan and no longer present"
		};

		RequireOneNamePerMember(names, SkippedItemCategories.ALL);

		return names.ToFrozenDictionary();
	}

	/// <summary>
	/// Plural category labels, matching the count row each description sits beside in Section 3's
	/// table - a table of counts is a table of categories. Declared
	/// beside <see cref="BuildSkippedItemCategoryNames"/> rather than derived from it, because the two
	/// answer different questions and a wording change to one must never silently change the other.
	/// <see cref="SkippedItemCategory.MalformedMatch"/> says "at least one": a file in this category
	/// may have had more than one match discarded, and the label never implies exactly one.
	/// </summary>
	private static FrozenDictionary<SkippedItemCategory, string> BuildSkippedItemCategoryDescriptions()
	{
		Dictionary<SkippedItemCategory, string> descriptions = new()
		{
			[SkippedItemCategory.UnreadableFile] =
				"Files that could not be opened",
			[SkippedItemCategory.UnlistableDirectory] =
				"Directories whose contents could not be listed",
			[SkippedItemCategory.LinkNotFollowed] =
				"Links to other locations, which are not followed",
			[SkippedItemCategory.Undecodable] =
				"Files that could not be read as text",
			[SkippedItemCategory.OversizeFile] =
				"Files larger than this release analyzes",
			[SkippedItemCategory.OverLongLine] =
				"Files with a line longer than this release analyzes",
			[SkippedItemCategory.PathTooLong] =
				"Files whose path is too long for the operating system to open",
			[SkippedItemCategory.LexicalAnomaly] =
				"Files only partly readable as code, so not analyzed in full",
			[SkippedItemCategory.MatchBoundExceeded] =
				"Files whose analysis took too long and was stopped; their findings are not reported",
			[SkippedItemCategory.MalformedMatch] =
				"Files where at least one unreliable match was discarded; their other findings are reported",
			[SkippedItemCategory.MissingCandidate] =
				"Files found during the scan and no longer present when analyzed"
		};

		RequireOneNamePerMember(descriptions, SkippedItemCategories.ALL);

		return descriptions.ToFrozenDictionary();
	}

	private static FrozenDictionary<ConfigurationSource, string> BuildConfigurationSourceNames()
	{
		Dictionary<ConfigurationSource, string> names = new()
		{
			[ConfigurationSource.BuiltInDefault] = "built-in default",
			[ConfigurationSource.UserProfileFile] = "the configuration file in the user profile",
			[ConfigurationSource.RepositoryFile] = "the configuration file in the repository",
			[ConfigurationSource.CommandLine] = "the command line",
			[ConfigurationSource.WorkingDirectoryAtLaunch] = "the working directory captured at launch"
		};

		RequireOneNamePerMember(names, Enum.GetValues<ConfigurationSource>());

		return names.ToFrozenDictionary();
	}

	private static FrozenDictionary<ConfigurationField, string> BuildConfigurationFieldNames()
	{
		Dictionary<ConfigurationField, string> names = new()
		{
			[ConfigurationField.EnabledLanguages] = "enabled languages",
			[ConfigurationField.MinimumConfidenceLevel] = "minimum confidence level",
			[ConfigurationField.OutputPath] = "output path",
			[ConfigurationField.DefaultFlow] = "default flow"
		};

		RequireOneNamePerMember(names, Enum.GetValues<ConfigurationField>());

		return names.ToFrozenDictionary();
	}

	/// <summary>
	/// The eight structural reasons - a completed stage's
	/// own document sections not matching what only this tool's engine could have produced - all read
	/// the same to a client: something changed the file outside the tool, or it is damaged. The
	/// distinction among the eight is real and is what the code itself carries for diagnosis; it is
	/// not a distinction a client reading a discard message needs.
	/// </summary>
	private static FrozenDictionary<SessionStateRejectionReason, string> BuildSessionStateRejectionReasonDescriptions()
	{
		const string structurallyDamagedDescription = "it was changed outside the tool or is damaged";

		Dictionary<SessionStateRejectionReason, string> descriptions = new()
		{
			[SessionStateRejectionReason.FileUnreadable] = "it could not be read",
			[SessionStateRejectionReason.FileTooLarge] =
				$"it is larger than the {SessionStateStore.DescribeCeiling(SessionStateStore.MAXIMUM_STATE_FILE_LENGTH_IN_BYTES)} limit for a session state file",
			[SessionStateRejectionReason.DocumentMalformed] = "it is not in the expected format",
			[SessionStateRejectionReason.SchemaVersionMismatch] = "it was written by a different version of the tool",
			[SessionStateRejectionReason.CatalogVersionMismatch] = "it was written with a different detection catalog version",
			[SessionStateRejectionReason.RepositoryLabelMismatch] = "it belongs to a different repository",
			[SessionStateRejectionReason.CompletedStageUnknown] = structurallyDamagedDescription,
			[SessionStateRejectionReason.CompletedStagesNotPrefixClosed] = structurallyDamagedDescription,
			[SessionStateRejectionReason.CandidatePathNotContained] = structurallyDamagedDescription,
			[SessionStateRejectionReason.FindingNotWellFormed] = structurallyDamagedDescription,
			[SessionStateRejectionReason.ScanSummaryNotWellFormed] = structurallyDamagedDescription,
			[SessionStateRejectionReason.SkippedItemTallyNotWellFormed] = structurallyDamagedDescription,
			[SessionStateRejectionReason.FindingsCapOutcomeNotWellFormed] = structurallyDamagedDescription,
			[SessionStateRejectionReason.ScanEnabledLanguagesNotWellFormed] = structurallyDamagedDescription,
			[SessionStateRejectionReason.CandidateAttributionNotWellFormed] = structurallyDamagedDescription,
			[SessionStateRejectionReason.DefaultFlowNotWellFormed] = structurallyDamagedDescription,
			[SessionStateRejectionReason.FlowEvaluationDiagnosticsNotWellFormed] = structurallyDamagedDescription
		};

		RequireOneNamePerMember(descriptions, _rejectionReasonsRequiringClientText);

		return descriptions.ToFrozenDictionary();
	}

	/// <summary>
	/// The structural guarantee: a member with no name does not start. It is the same one the
	/// command registry and the language name table hold over their own vocabularies, and it is
	/// what turns "remember to add a name" into a thing the build says rather than a thing a
	/// report omits.
	/// </summary>
	private static void RequireOneNamePerMember<TMember>(
		Dictionary<TMember, string> names,
		IReadOnlyCollection<TMember> members)
		where TMember : notnull
	{
		foreach (TMember member in members)
		{
			if (!names.ContainsKey(member))
			{
				throw new InvalidOperationException($"Report vocabulary member {member} carries no name.");
			}
		}
	}
}
