using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The console table's five columns, and the sixth that must never appear there.
///
/// The console and the report deliberately disagree in one place: the finding model carries
/// destination certainty and the report groups its findings by it, but the console table is
/// specified column by column, so the console stays as specified. The test that
/// matters is therefore the negative one.
/// </summary>
public sealed class ConsoleFindingsTableTests
{
	[Fact]
	public void TheTableHasExactlyTheFiveColumnsTheRequirementNames()
	{
		Assert.Equal(5, ConsoleFindingsTable.COLUMN_COUNT);
		Assert.Equal(
			["ID", "File:Line", "Language", "Pattern", "Confidence"],
			[.. ConsoleFindingsTable.ColumnHeadings]);
	}

	[Fact]
	public void ARenderedRowCarriesFiveValuesAndNoMore()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		Finding finding = ReportHarness.FindingAt(
			1,
			"folder/source.cs",
			42,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			ConfidenceLevel.High);

		string row = ConsoleFindingsTable.BuildRowLine(finding, catalog);

		string[] cells = row.Split(
			ConsoleFindingsTable.COLUMN_SEPARATOR,
			StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		Assert.Equal(ConsoleFindingsTable.COLUMN_COUNT, cells.Length);
		Assert.Equal(FindingIdentifiers.Of(1), cells[0]);
		Assert.Equal("folder/source.cs:42", cells[1]);
		Assert.Equal(AssessedLanguages.CanonicalName(AssessedLanguage.CSharp), cells[2]);
		Assert.Equal(ReportVocabulary.PatternClassText(catalog, finding), cells[3]);
		Assert.Equal(ConfidenceLevels.Name(ConfidenceLevel.High), cells[4]);
	}

	/// <summary>
	/// The Pattern column carries the catalog's display name, with the
	/// identifier still discoverable in the same cell, at every one of the three confidence
	/// levels - Low is routed to manual review for its recommendation, but the
	/// naming promise does not carve out an exception for it.
	/// </summary>
	[Theory]
	[InlineData(ConfidenceLevel.Low)]
	[InlineData(ConfidenceLevel.Medium)]
	[InlineData(ConfidenceLevel.High)]
	public void TheDisplayNameRendersAtEveryConfidenceLevel(ConfidenceLevel level)
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		Finding finding = ReportHarness.FindingAt(1, "source.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, level);

		string row = ConsoleFindingsTable.BuildRowLine(finding, catalog);

		Assert.Contains(ReportHarness.DisplayNameFor(ReportHarness.TEMPLATED_PATTERN_CLASS), row, StringComparison.Ordinal);
		Assert.Contains(ReportHarness.TEMPLATED_PATTERN_CLASS, row, StringComparison.Ordinal);
	}

	/// <summary>
	/// The negative case the console specification exists for. Both findings are rendered and neither row says anything
	/// about where the request goes, in either direction.
	/// </summary>
	[Fact]
	public void TheTableNeverCarriesDestinationCertainty()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "alpha.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(
				2,
				"beta.cs",
				2,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High,
				DestinationCertainty.Unknown)
		];

		string rendered = Render(findings);

		Assert.DoesNotContain(ReportVocabulary.DESTINATION_CERTAINTY_ESTABLISHED_VALUE, rendered, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(ReportVocabulary.DESTINATION_CERTAINTY_UNKNOWN_VALUE, rendered, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(ReportVocabulary.DESTINATION_NOT_ESTABLISHED_LABEL, rendered, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void AnEmptySetSaysSoRatherThanPrintingABareHeader()
	{
		string rendered = Render([]);

		Assert.Equal(ConsoleFindingsTable.NO_FINDINGS_MESSAGE, rendered.Trim());
	}

	[Fact]
	public void TheHeaderIsWrittenOnceAndOnlyWhenThereIsARow()
	{
		string rendered = Render(
		[
			ReportHarness.FindingAt(1, "one.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(2, "two.cs", 2, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium)
		]);

		string[] lines = rendered.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		Assert.Equal(3, lines.Length);
		Assert.Equal(ConsoleFindingsTable.BuildHeaderLine(), lines[0].TrimEnd('\r'));
	}

	/// <summary>
	/// A path longer than the location column widens that column for the whole table, so every
	/// row's later columns start where the header's do instead of the long row drifting right.
	/// </summary>
	[Fact]
	public void APathLongerThanTheColumnKeepsEveryRowAligned()
	{
		string longPath = string.Join('/', Enumerable.Repeat("a-deeply-nested-folder", 4)) + "/source.cs";

		string rendered = Render(
		[
			ReportHarness.FindingAt(1, "short.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(2, longPath, 20, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium)
		]);

		string[] lines = rendered.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		string languageName = AssessedLanguages.CanonicalName(AssessedLanguage.CSharp);
		int headerLanguageColumn = lines[0].IndexOf("Language", StringComparison.Ordinal);

		Assert.True(longPath.Length > 48);
		Assert.Equal(3, lines.Length);
		Assert.Equal(headerLanguageColumn, lines[1].IndexOf(languageName, StringComparison.Ordinal));
		Assert.Equal(headerLanguageColumn, lines[2].IndexOf(languageName, StringComparison.Ordinal));
		Assert.Equal(lines[0].IndexOf("Confidence", StringComparison.Ordinal), lines[2].IndexOf("Medium", StringComparison.Ordinal));
	}

	private static string Render(ImmutableArray<Finding> findings)
	{
		using StringWriter writer = new();

		ConsoleFindingsTable.Render(writer, findings, ReportHarness.CatalogWithCSharpOnly());

		return writer.ToString();
	}
}
