using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;

namespace MigrateAuth.Cli.Reporting;

/// <summary>What this release did about one of the eight languages. There are exactly three states.</summary>
public enum LanguageCoverageState
{
	/// <summary>This session was configured not to assess it.</summary>
	DisabledByConfiguration = 0,

	/// <summary>This release ships no analyzer for it, so nothing was looked for.</summary>
	DeclaredUnsupported = 1,

	/// <summary>An analyzer ran, and it searched for a stated number of pattern classes.</summary>
	Supported = 2
}

/// <summary>One language's row of the coverage statement.</summary>
public sealed record LanguageCoverageRow
{
	/// <summary>The language. One of the eight, always.</summary>
	public required AssessedLanguage Language { get; init; }

	/// <summary>Its canonical name, which is the name the artifact and the report both carry.</summary>
	public required string LanguageName { get; init; }

	/// <summary>Which of the three states applies.</summary>
	public required LanguageCoverageState State { get; init; }

	/// <summary>
	/// How many pattern classes were searched for. Zero in the two states where nothing was
	/// searched, and it is zero because nothing ran rather than because the catalog is empty.
	/// </summary>
	public required int PatternClassesSearched { get; init; }

	/// <summary>
	/// What this language's analyzer declares it does not attempt to recognize,
	/// already sorted ordinally by <see cref="DeclaredUnsupportedCoverage.Construct"/> -
	/// the analyzer itself never orders, so this is the one point that does. Empty in the
	/// two states where nothing was searched, for the same reason <see cref="PatternClassesSearched"/>
	/// is: there is no analyzer to have declared anything.
	/// </summary>
	public required ImmutableArray<DeclaredUnsupportedCoverage> DeclaredUnsupportedConstructs { get; init; }
}

/// <summary>
/// The coverage statement, which leads the report.
///
/// A zero-findings result reads as "this repository is clean" unless something says otherwise, and
/// the true reading may be "this language was not assessed". This section says so, and it is
/// present even when every other section is empty.
///
/// It iterates the eight-value enumeration and never the catalog registry. A language with no
/// analyzer is absent from <see cref="LanguageCatalog.AnalyzersByLanguageName"/>, so a loop over the
/// registry would say nothing about it; driving the loop from
/// <see cref="AssessedLanguages.AllInDeclaredOrder"/> makes an unsupported language a row rather than
/// a silence.
///
/// The three states are tested in priority order. A language that is both disabled and unsupported
/// reports as disabled: that is what the session was asked to do, and it is the state the reader can
/// act on.
///
/// The section also states that the assessment is not a guarantee of absence: the catalog's own
/// declared scope bounds what was searched for.
/// </summary>
public static class CoverageStatement
{
	/// <summary>
	/// The explicit statement that the assessment is not a guarantee of absence. It says
	/// what the coverage rows mean rather than repeating them: the rows list what was searched
	/// for, and this says that what was not searched for is not thereby absent.
	/// </summary>
	public const string NOT_A_GUARANTEE_OF_ABSENCE =
		"This assessment reports what the catalog of this release searched for. It is not a guarantee of absence: a construct this catalog does not cover, in a language it does not support or was not asked to assess, produces no finding and is not evidence that none exists.";

	/// <summary>The row text for a language this session was configured not to assess.</summary>
	public const string DISABLED_BY_CONFIGURATION = "disabled by configuration";

	/// <summary>The row text for a language this release ships no analyzer for.</summary>
	public const string DECLARED_UNSUPPORTED = "declared unsupported in this release";

	/// <summary>The row text for a language that was assessed, carrying the class count.</summary>
	public const string SUPPORTED_FORMAT = "supported, {0} classes searched";

	/// <summary>
	/// One row per language, in the enumeration's declared order, always eight of them.
	/// </summary>
	public static ImmutableArray<LanguageCoverageRow> Compose(
		LanguageCatalog catalog,
		EffectiveConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		ArgumentNullException.ThrowIfNull(configuration);

		return
		[
			.. AssessedLanguages.AllInDeclaredOrder.Select(language => RowFor(language, catalog, configuration))
		];
	}

	/// <summary>The row's text, which the report prints and the artifact carries as a state name.</summary>
	public static string Describe(LanguageCoverageRow row)
	{
		ArgumentNullException.ThrowIfNull(row);

		string description = row.State switch
		{
			LanguageCoverageState.DisabledByConfiguration => DISABLED_BY_CONFIGURATION,
			LanguageCoverageState.DeclaredUnsupported => DECLARED_UNSUPPORTED,
			LanguageCoverageState.Supported => string.Format(
				CultureInfo.InvariantCulture,
				SUPPORTED_FORMAT,
				row.PatternClassesSearched),
			_ => throw new ArgumentOutOfRangeException(nameof(row), row.State, "No coverage text is declared for this state.")
		};

		return description;
	}

	private static LanguageCoverageRow RowFor(
		AssessedLanguage language,
		LanguageCatalog catalog,
		EffectiveConfiguration configuration)
	{
		string languageName = AssessedLanguages.CanonicalName(language);
		LanguageCoverageState state = LanguageCoverageState.DisabledByConfiguration;
		int patternClassesSearched = 0;
		ImmutableArray<DeclaredUnsupportedCoverage> declaredUnsupportedConstructs = [];

		if (configuration.IsEnabled(language))
		{
			state = LanguageCoverageState.DeclaredUnsupported;

			if (catalog.AnalyzersByLanguageName.TryGetValue(languageName, out ILanguageAnalyzer? analyzer))
			{
				state = LanguageCoverageState.Supported;
				patternClassesSearched = analyzer.DetectionRulesByPatternClass.Count;
				declaredUnsupportedConstructs = SortedDeclaredUnsupportedConstructs(analyzer);
			}
		}

		return new LanguageCoverageRow
		{
			Language = language,
			LanguageName = languageName,
			State = state,
			PatternClassesSearched = patternClassesSearched,
			DeclaredUnsupportedConstructs = declaredUnsupportedConstructs
		};
	}

	/// <summary>
	/// The ordinal comparison, applied to the one field the analyzer itself does not order. Two languages
	/// sharing one analyzer read the same declared set, since the coverage belongs to the analyzer's
	/// catalog and not to which of its declared names a file happened to resolve to.
	/// </summary>
	private static ImmutableArray<DeclaredUnsupportedCoverage> SortedDeclaredUnsupportedConstructs(
		ILanguageAnalyzer analyzer)
	{
		return
		[
			.. analyzer.LanguageIdentification.DeclaredUnsupportedCoverage
				.OrderBy(entry => entry.Construct, StringComparer.Ordinal)
		];
	}
}
