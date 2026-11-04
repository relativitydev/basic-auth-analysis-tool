using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE FOUR STATES A ROW OF THE LANGUAGE INVENTORY CAN BE IN, RELATIVE TO
/// WHAT THE RUNNING CATALOG ACTUALLY SEARCHED FOR AND WHETHER THIS SESSION WAS CONFIGURED TO.
/// </summary>
public enum LanguageAssessmentState
{
	/// <summary>The row names one of the eight <see cref="AssessedLanguages"/> and a registered analyzer covers it.</summary>
	Assessed = 0,

	/// <summary>The row names one of the eight but no registered analyzer covers it in this build.</summary>
	NotAssessed = 1,

	/// <summary>
	/// The row is <see cref="Scanning.LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME"/>: no language
	/// was detected at all, so the file was never searched and the question of coverage does not
	/// apply to it.
	/// </summary>
	NoLanguageDetected = 2,

	/// <summary>
	/// The row names one of the eight, but this session's configuration disabled it - so the
	/// question is never reached even where a registered analyzer exists. Takes precedence over
	/// <see cref="NotAssessed"/>: a disabled language reports as disabled regardless of whether this
	/// build ships an analyzer for it, because that is the state a reader can act on.
	/// </summary>
	DisabledByConfiguration = 3
}

/// <summary>
/// WHETHER A LANGUAGE THE LANGUAGE INVENTORY NAMES HAS A REGISTERED ANALYZER BEHIND IT,
/// DERIVED FROM THE CATALOG AT RENDER TIME AND NEVER PERSISTED.
///
/// <see cref="Scanning.BaseLanguageDetection"/> lets the inventory name all eight languages before
/// every one of them ships an analyzer, and a reader of that inventory needs to be able to tell
/// which rows the running catalog actually searched from which rows it only counted. This is that
/// distinction, read straight off <see cref="LanguageCatalog.AnalyzersByLanguageName"/> rather than
/// carried on <see cref="Scanning.LanguageInventoryRow"/> itself - the row is produced once by the
/// Scan stage and may be read back from a resumed session's state long after that stage ran, while
/// the catalog a later stage builds is always the one embedded in THAT run's binary. Computing this
/// at render time is what keeps a resumed report identical to an uninterrupted one without the
/// session-state schema having to carry a field that would go stale the moment a later build ships
/// the missing analyzer.
///
/// THE UNRECOGNIZED BUCKET IS ITS OWN STATE, NEVER "ASSESSED". `LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME`
/// is not one of <see cref="AssessedLanguages"/>'s eight names and never will be - it is what
/// the inventory calls a file matching neither an analyzer's rule nor the shared engine's own table, not a
/// ninth language this release forgot to build. Those files were never searched at all, which is a
/// different fact from "a detected language this release does not assess" - marking either one as
/// the other misattributes an absence of any match to an absence of coverage, or the reverse, which
/// is exactly the confusion the coverage section's own three-state split (disabled, declared unsupported,
/// supported) already refuses to make for the coverage section. This is the same refusal applied to
/// the inventory, with <see cref="LanguageAssessmentState.NoLanguageDetected"/> as its own third
/// state rather than folded into either of the other two.
/// </summary>
public static class LanguageAssessment
{
	/// <summary>
	/// Which of the four <see cref="LanguageAssessmentState"/> values this inventory row is in.
	///
	/// THE PRECEDENCE: no language detected outranks everything else, because the row was never
	/// searched at all; a disabled language then outranks a missing analyzer, because disabling is
	/// this session's own choice and the reader needs to see that rather than a coverage gap the
	/// choice made moot.
	/// </summary>
	public static LanguageAssessmentState StateOf(
		LanguageCatalog catalog,
		string languageName,
		IReadOnlySet<AssessedLanguage> enabledLanguages)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		ArgumentNullException.ThrowIfNull(languageName);
		ArgumentNullException.ThrowIfNull(enabledLanguages);

		LanguageAssessmentState state;

		if (!AssessedLanguages.TryParse(languageName, out AssessedLanguage language))
		{
			state = LanguageAssessmentState.NoLanguageDetected;
		}
		else if (!enabledLanguages.Contains(language))
		{
			state = LanguageAssessmentState.DisabledByConfiguration;
		}
		else if (catalog.AnalyzersByLanguageName.ContainsKey(languageName))
		{
			state = LanguageAssessmentState.Assessed;
		}
		else
		{
			state = LanguageAssessmentState.NotAssessed;
		}

		return state;
	}
}
