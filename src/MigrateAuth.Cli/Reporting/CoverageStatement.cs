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
/// THE COVERAGE STATEMENT, WHICH LEADS THE REPORT BECAUSE OF WHAT IT SAYS WHEN IT IS EMPTY.
///
/// A zero-findings result reads as "this repository is clean" unless something says otherwise,
/// and the true reading is often "we did not look at your language". This section is the thing
/// that says otherwise, which is why the report puts it first and why it is present even -
/// especially - when every other section is empty.
///
/// IT ITERATES THE EIGHT-VALUE ENUMERATION AND NEVER THE CATALOG REGISTRY. A language with no
/// analyzer is simply ABSENT from <see cref="LanguageCatalog.AnalyzersByLanguageName"/>, so a loop
/// over the registry would emit nothing for it and the report would say nothing about it. Driving
/// the loop from <see cref="AssessedLanguages.AllInDeclaredOrder"/> makes an unsupported language
/// a ROW rather than a silence.
///
/// THE THREE STATES ARE TESTED IN PRIORITY ORDER, and the order matters. A language that is both
/// disabled and unsupported reports as disabled: that is what the session was asked to do, and it
/// is the state the reader can act on. Reporting it as unsupported would attribute a
/// configuration choice to the product's coverage.
///
/// THE STATEMENT THAT THE ASSESSMENT IS NOT A GUARANTEE OF ABSENCE IS PART OF THE SECTION, not a
/// footnote to it.
/// </summary>
public static class CoverageStatement
{
	/// <summary>
	/// The statement the report must make in explicit language, and the reason for it. It says
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
	/// One row per language, in the enumeration's declared order, always eight of them, for the
	/// languages the configuration enables.
	/// </summary>
	public static ImmutableArray<LanguageCoverageRow> Compose(
		LanguageCatalog catalog,
		EffectiveConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		return Compose(catalog, configuration.EnabledLanguageSet);
	}

	/// <summary>
	/// One row per language, in the enumeration's declared order, always eight of them, for the given
	/// set of assessed languages. The report passes the set the scan actually used, so that the
	/// statement describes what was looked at even when the configuration has changed since.
	/// </summary>
	public static ImmutableArray<LanguageCoverageRow> Compose(
		LanguageCatalog catalog,
		IReadOnlySet<AssessedLanguage> assessedLanguages)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		ArgumentNullException.ThrowIfNull(assessedLanguages);

		return
		[
			.. AssessedLanguages.AllInDeclaredOrder.Select(language => RowFor(language, catalog, assessedLanguages))
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
		IReadOnlySet<AssessedLanguage> assessedLanguages)
	{
		string languageName = AssessedLanguages.CanonicalName(language);
		LanguageCoverageState state = LanguageCoverageState.DisabledByConfiguration;
		int patternClassesSearched = 0;
		ImmutableArray<DeclaredUnsupportedCoverage> declaredUnsupportedConstructs = [];

		if (assessedLanguages.Contains(language))
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
	/// Ordinal comparison, applied to the one field of this contract the analyzer itself is
	/// forbidden to order. Two languages sharing one analyzer (the relation between
	/// languages and analyzers is not one-to-one) read the same declared set, since the coverage belongs to the
	/// analyzer's catalog and not to which of its declared names a file happened to resolve to.
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
