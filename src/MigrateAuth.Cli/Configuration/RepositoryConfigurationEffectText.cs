using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Configuration;

/// <summary>
/// The sentences that say what the repository's own configuration file changed, worded once so
/// that Pre-flight, the Markdown report and any later renderer state it identically.
///
/// Each sentence describes what the repository ASKED for and then what that did to the
/// assessment, in this product's own vocabulary only - canonical language names and level names.
/// A sentence never contains text taken from the file, which is what lets the report carry it.
/// The number of findings a raised threshold hides is known only when the report is built, so
/// <see cref="ThresholdStatementFor"/> stops before it and the report appends its own count.
/// </summary>
public static class RepositoryConfigurationEffectText
{
	/// <summary>The enabled-language sentence when the repository only removed languages.</summary>
	public const string LANGUAGES_DISABLED_MESSAGE_FORMAT =
		"The configuration file in the repository root sets the enabled languages to {0}; it disables {1} for this assessment.";

	/// <summary>The enabled-language sentence when the repository only added languages.</summary>
	public const string LANGUAGES_ENABLED_MESSAGE_FORMAT =
		"The configuration file in the repository root sets the enabled languages to {0}; it enables {1} for this assessment.";

	/// <summary>The enabled-language sentence when the repository both added and removed languages.</summary>
	public const string LANGUAGES_ENABLED_AND_DISABLED_MESSAGE_FORMAT =
		"The configuration file in the repository root sets the enabled languages to {0}: it enables {1} and disables {2}.";

	/// <summary>The threshold sentence when the repository raised it.</summary>
	public const string THRESHOLD_RAISED_MESSAGE_FORMAT =
		"The configuration file in the repository root raises the minimum confidence level from {0} to {1}; {2} findings are not shown.";

	/// <summary>
	/// The threshold sentence when the repository raised it and the report knows how many findings that
	/// hides: the source, the levels, and the number.
	/// </summary>
	public const string THRESHOLD_RAISED_WITH_COUNT_MESSAGE_FORMAT =
		"The configuration file in the repository root raises the minimum confidence level from {0} to {1}; {2:N0} findings ({3}) are not shown.";

	/// <summary>The threshold sentence when the repository lowered it.</summary>
	public const string THRESHOLD_LOWERED_MESSAGE_FORMAT =
		"The configuration file in the repository root lowers the minimum confidence level from {0} to {1}; {2} findings are now shown.";

	/// <summary>What a language list reads as when it holds no language.</summary>
	public const string NO_LANGUAGES_TEXT = "none";

	private const string _LANGUAGE_SEPARATOR = ", ";
	private const string _LEVEL_SEPARATOR = " and ";

	/// <summary>
	/// Every sentence the effect calls for, languages first and threshold second. Empty when the
	/// repository's file changed nothing, which is the ordinary case. When
	/// <paramref name="hiddenFindingCount"/> is given, a raised threshold's sentence states it; Pre-flight
	/// runs before there are findings and gives none.
	/// </summary>
	public static ImmutableArray<string> StatementsFor(RepositoryConfigurationEffect effect, int? hiddenFindingCount = null)
	{
		ArgumentNullException.ThrowIfNull(effect);

		ImmutableArray<string>.Builder statements = ImmutableArray.CreateBuilder<string>();

		if (effect.ChangesLanguages)
		{
			statements.Add(LanguageStatementFor(effect));
		}

		if (effect.ThresholdChange is ConfidenceLevelChange thresholdChange)
		{
			statements.Add(
				hiddenFindingCount is int hiddenCount && thresholdChange.IsRaise
					? ThresholdStatementFor(thresholdChange, hiddenCount)
					: ThresholdStatementFor(thresholdChange));
		}

		return statements.ToImmutable();
	}

	/// <summary>The sentence for a change of the enabled languages: what the repository set, then what that enabled and disabled.</summary>
	public static string LanguageStatementFor(RepositoryConfigurationEffect effect)
	{
		ArgumentNullException.ThrowIfNull(effect);

		string setTo = effect.LanguagesSetByRepository.IsEmpty
			? NO_LANGUAGES_TEXT
			: NamesOf(effect.LanguagesSetByRepository);

		string statement;

		if (effect.EnabledLanguages.IsEmpty)
		{
			statement = Format(LANGUAGES_DISABLED_MESSAGE_FORMAT, setTo, NamesOf(effect.DisabledLanguages));
		}
		else if (effect.DisabledLanguages.IsEmpty)
		{
			statement = Format(LANGUAGES_ENABLED_MESSAGE_FORMAT, setTo, NamesOf(effect.EnabledLanguages));
		}
		else
		{
			statement = Format(
				LANGUAGES_ENABLED_AND_DISABLED_MESSAGE_FORMAT,
				setTo,
				NamesOf(effect.EnabledLanguages),
				NamesOf(effect.DisabledLanguages));
		}

		return statement;
	}

	/// <summary>
	/// The sentence for a change of the threshold, ending with the levels whose visibility
	/// changed - highest first. It carries no count of findings; see the type's remarks.
	/// </summary>
	public static string ThresholdStatementFor(ConfidenceLevelChange change)
	{
		ArgumentNullException.ThrowIfNull(change);

		return Format(
			change.IsRaise ? THRESHOLD_RAISED_MESSAGE_FORMAT : THRESHOLD_LOWERED_MESSAGE_FORMAT,
			ConfidenceLevels.Name(change.Previous),
			ConfidenceLevels.Name(change.Current),
			ChangedLevelsOf(change));
	}

	/// <summary>
	/// The sentence for a raised threshold, with the number of findings the raise hides: the ones the
	/// previous level would have shown and the current one does not. The number is computed by
	/// <c>ThresholdFilter</c> over the same set the report lists, so the sentence and the rows agree.
	/// </summary>
	public static string ThresholdStatementFor(ConfidenceLevelChange change, int hiddenFindingCount)
	{
		ArgumentNullException.ThrowIfNull(change);

		return Format(
			THRESHOLD_RAISED_WITH_COUNT_MESSAGE_FORMAT,
			ConfidenceLevels.Name(change.Previous),
			ConfidenceLevels.Name(change.Current),
			hiddenFindingCount,
			ChangedLevelsOf(change));
	}

	private static string ChangedLevelsOf(ConfidenceLevelChange change)
	{
		ConfidenceLevel lowest = ConfidenceLevels.Widest(change.Previous, change.Current);
		ConfidenceLevel highest = change.IsRaise ? change.Current : change.Previous;

		return string.Join(
			_LEVEL_SEPARATOR,
			Enum.GetValues<ConfidenceLevel>()
				.Where(level => level >= lowest && level < highest)
				.OrderDescending()
				.Select(ConfidenceLevels.Name));
	}

	private static string NamesOf(ImmutableArray<AssessedLanguage> languages) =>
		string.Join(_LANGUAGE_SEPARATOR, languages.Select(AssessedLanguages.CanonicalName));

	private static string Format(string messageFormat, params object[] arguments) =>
		string.Format(CultureInfo.InvariantCulture, messageFormat, arguments);
}
