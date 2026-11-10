using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Catalog.Languages;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The four states of a language inventory row and the precedence between them: no language
/// detected, then disabled by configuration, then whether a registered analyzer covers it.
/// </summary>
public sealed class LanguageAssessmentTests
{
	private const string _COVERED_LANGUAGE_NAME = "CSharp";
	private const string _UNCOVERED_LANGUAGE_NAME = "Go";

	private static readonly LanguageCatalog _catalog =
		new(LanguageCatalog.CurrentVersion, EmbeddedLanguageAnalyzers.CreateAll());

	private static readonly LanguageCatalog _emptyCatalog = new(LanguageCatalog.CurrentVersion, []);

	[Fact]
	public void ALanguageWithARegisteredAnalyzerIsAssessed()
	{
		LanguageAssessmentState state = LanguageAssessment.StateOf(_catalog, _COVERED_LANGUAGE_NAME, AllLanguagesEnabled());

		Assert.Equal(LanguageAssessmentState.Assessed, state);
	}

	[Fact]
	public void ALanguageWithoutARegisteredAnalyzerIsNotAssessed()
	{
		LanguageAssessmentState state = LanguageAssessment.StateOf(_emptyCatalog, _COVERED_LANGUAGE_NAME, AllLanguagesEnabled());

		Assert.Equal(LanguageAssessmentState.NotAssessed, state);
	}

	[Fact]
	public void AnEnabledLanguageNoAnalyzerCoversIsNotAssessed()
	{
		Assert.False(_catalog.AnalyzersByLanguageName.ContainsKey(_UNCOVERED_LANGUAGE_NAME));

		LanguageAssessmentState state = LanguageAssessment.StateOf(_catalog, _UNCOVERED_LANGUAGE_NAME, AllLanguagesEnabled());

		Assert.Equal(LanguageAssessmentState.NotAssessed, state);
	}

	[Fact]
	public void ADisabledLanguageReportsAsDisabledEvenWhenAnAnalyzerCoversIt()
	{
		HashSet<AssessedLanguage> enabled = AllLanguagesEnabled();
		enabled.Remove(AssessedLanguage.CSharp);

		LanguageAssessmentState state = LanguageAssessment.StateOf(_catalog, _COVERED_LANGUAGE_NAME, enabled);

		Assert.Equal(LanguageAssessmentState.DisabledByConfiguration, state);
	}

	[Fact]
	public void ADisabledLanguageWithoutAnAnalyzerStillReportsAsDisabled()
	{
		HashSet<AssessedLanguage> enabled = [];

		LanguageAssessmentState state = LanguageAssessment.StateOf(_emptyCatalog, _UNCOVERED_LANGUAGE_NAME, enabled);

		Assert.Equal(LanguageAssessmentState.DisabledByConfiguration, state);
	}

	[Fact]
	public void TheUnrecognizedBucketIsItsOwnStateRegardlessOfConfiguration()
	{
		HashSet<AssessedLanguage> noneEnabled = [];

		Assert.Equal(
			LanguageAssessmentState.NoLanguageDetected,
			LanguageAssessment.StateOf(_catalog, LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, AllLanguagesEnabled()));
		Assert.Equal(
			LanguageAssessmentState.NoLanguageDetected,
			LanguageAssessment.StateOf(_catalog, LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, noneEnabled));
	}

	[Fact]
	public void ANullArgumentIsRefused()
	{
		HashSet<AssessedLanguage> enabled = AllLanguagesEnabled();

		Assert.Throws<ArgumentNullException>(() => LanguageAssessment.StateOf(null!, _COVERED_LANGUAGE_NAME, enabled));
		Assert.Throws<ArgumentNullException>(() => LanguageAssessment.StateOf(_catalog, null!, enabled));
		Assert.Throws<ArgumentNullException>(() => LanguageAssessment.StateOf(_catalog, _COVERED_LANGUAGE_NAME, null!));
	}

	private static HashSet<AssessedLanguage> AllLanguagesEnabled()
	{
		return [.. AssessedLanguages.AllInDeclaredOrder];
	}
}
