using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The coverage statement, and the one property that makes it worth having: it names a
/// language nobody has built an analyzer for.
///
/// A loop over the catalog registry passes every other test in this file and fails this one
/// silently - the unsupported language contributes no entry, so the report says nothing about it
/// and a clean result reads as "you are clean" when it means "we did not look". So the rows are
/// counted against the enumeration rather than against the catalog.
/// </summary>
public sealed class CoverageStatementTests
{
	[Fact]
	public void EveryOneOfTheEightLanguagesGetsARowWhateverTheCatalogHolds()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low));

		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, rows.Length);
		Assert.Equal(
			[.. AssessedLanguages.AllInDeclaredOrder],
			[.. rows.Select(row => row.Language)]);
	}

	[Fact]
	public void ALanguageWithNoAnalyzerIsNamedAsUnsupportedRatherThanOmitted()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low));

		LanguageCoverageRow goRow = rows.Single(row => row.Language == AssessedLanguage.Go);

		Assert.Equal(LanguageCoverageState.DeclaredUnsupported, goRow.State);
		Assert.Equal(CoverageStatement.DECLARED_UNSUPPORTED, CoverageStatement.Describe(goRow));
		Assert.Equal(0, goRow.PatternClassesSearched);
	}

	[Fact]
	public void ALanguageWithAnAnalyzerCarriesTheNumberOfClassesItSearchedFor()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			ReportHarness.CatalogWithTwoLanguages(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low));

		LanguageCoverageRow cSharpRow = rows.Single(row => row.Language == AssessedLanguage.CSharp);
		LanguageCoverageRow powerShellRow = rows.Single(row => row.Language == AssessedLanguage.PowerShell);

		Assert.Equal(LanguageCoverageState.Supported, cSharpRow.State);
		Assert.Equal(2, cSharpRow.PatternClassesSearched);
		Assert.Equal("supported, 2 classes searched", CoverageStatement.Describe(cSharpRow));
		Assert.Equal(1, powerShellRow.PatternClassesSearched);
	}

	/// <summary>
	/// A language with zero findings must be distinguishable from one that was never
	/// assessed, and this is the row that carries the difference.
	/// </summary>
	[Fact]
	public void ADisabledLanguageSaysSoRatherThanReportingItsCatalog()
	{
		EffectiveConfiguration configuration = ReportHarness.ConfigurationWith(
			ConfidenceLevel.Low,
			AssessedLanguage.CSharp);

		LanguageCoverageRow cSharpRow = CoverageStatement
			.Compose(ReportHarness.CatalogWithCSharpOnly(), configuration)
			.Single(row => row.Language == AssessedLanguage.CSharp);

		Assert.Equal(LanguageCoverageState.DisabledByConfiguration, cSharpRow.State);
		Assert.Equal(CoverageStatement.DISABLED_BY_CONFIGURATION, CoverageStatement.Describe(cSharpRow));
	}

	/// <summary>
	/// A language that is both disabled and unsupported reports as disabled: that is what the
	/// session was asked to do, and it is the state the reader can act on.
	/// </summary>
	[Fact]
	public void DisabledOutranksUnsupportedWhenBothApply()
	{
		EffectiveConfiguration configuration = ReportHarness.ConfigurationWith(
			ConfidenceLevel.Low,
			AssessedLanguage.Go);

		LanguageCoverageRow goRow = CoverageStatement
			.Compose(ReportHarness.CatalogWithCSharpOnly(), configuration)
			.Single(row => row.Language == AssessedLanguage.Go);

		Assert.Equal(LanguageCoverageState.DisabledByConfiguration, goRow.State);
	}

	[Fact]
	public void ACatalogWithNoAnalyzersStillProducesEightRows()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			ReportHarness.CatalogWithNoAnalyzers(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low));

		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, rows.Length);
		Assert.All(rows, row => Assert.Equal(LanguageCoverageState.DeclaredUnsupported, row.State));
	}

	[Fact]
	public void TheStatementSaysTheAssessmentIsNotAGuaranteeOfAbsence()
	{
		Assert.Contains("not a guarantee of absence", CoverageStatement.NOT_A_GUARANTEE_OF_ABSENCE, StringComparison.Ordinal);
	}

	/// <summary>
	/// Declared second, in source, but ordinal comparison places every
	/// upper-case letter before every lower-case one - so the analyzer's declaration order and the
	/// row's own order disagree, which is what proves a sort actually ran rather than the set's
	/// enumeration order surviving unexamined.
	/// </summary>
	[Fact]
	public void ADeclaredUnsupportedLanguagesConstructsAreSortedOrdinallyByConstruct()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			ReportHarness.CatalogWithDeclaredUnsupportedCoverage(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low));

		LanguageCoverageRow cSharpRow = rows.Single(row => row.Language == AssessedLanguage.CSharp);

		Assert.Equal(2, cSharpRow.DeclaredUnsupportedConstructs.Length);
		Assert.Equal(ReportHarness.UPPERCASE_FIRST_CONSTRUCT, cSharpRow.DeclaredUnsupportedConstructs[0].Construct);
		Assert.Equal(ReportHarness.LOWERCASE_FIRST_CONSTRUCT, cSharpRow.DeclaredUnsupportedConstructs[1].Construct);
	}

	/// <summary>An unsupported or disabled language carries no declared-unsupported entries: there is no analyzer to have declared any.</summary>
	[Fact]
	public void ALanguageWithNoAnalyzerDeclaresNoUnsupportedConstructs()
	{
		ImmutableArray<LanguageCoverageRow> rows = CoverageStatement.Compose(
			ReportHarness.CatalogWithDeclaredUnsupportedCoverage(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low));

		LanguageCoverageRow goRow = rows.Single(row => row.Language == AssessedLanguage.Go);

		Assert.Empty(goRow.DeclaredUnsupportedConstructs);
	}
}
