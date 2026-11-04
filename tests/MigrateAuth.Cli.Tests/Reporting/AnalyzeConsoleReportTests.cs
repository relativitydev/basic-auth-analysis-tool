using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The console surface in isolation, over hand-built findings rather than the real pipeline -
/// <see cref="Pipeline.AnalyzeStageTests"/> covers the same behaviour end to end, and what
/// this class adds is exactness the fixture language cannot give it: a Medium finding, and finding
/// counts past <see cref="ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS"/> without writing a
/// source file for each one.
/// </summary>
public sealed class AnalyzeConsoleReportTests
{
	[Fact]
	public void TheConfidenceTotalsCountEveryLevelIncludingMedium()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "a.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(2, "b.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium),
			ReportHarness.FindingAt(3, "c.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)
		];

		string rendered = Render(findings, catalog, ConfidenceLevels.WIDEST);

		Assert.Contains(
			string.Format(CultureInfo.InvariantCulture, AnalyzeConsoleReport.FINDINGS_BY_CONFIDENCE_MESSAGE_FORMAT, 1, 1, 1),
			rendered,
			StringComparison.Ordinal);
	}

	[Fact]
	public void AnEmptySetPrintsOnlyTheZeroTotalsAndNothingElse()
	{
		string rendered = Render([], ReportHarness.CatalogWithCSharpOnly(), ConfidenceLevels.WIDEST);

		Assert.Equal(
			string.Format(CultureInfo.InvariantCulture, AnalyzeConsoleReport.FINDINGS_BY_CONFIDENCE_MESSAGE_FORMAT, 0, 0, 0),
			rendered.Trim());
	}

	[Fact]
	public void TheThresholdNoteAppearsOnlyWhenTheThresholdHidesAFinding()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "a.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(2, "b.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)
		];

		string renderedAtHigh = Render(findings, catalog, ConfidenceLevel.High);
		string renderedAtLow = Render(findings, catalog, ConfidenceLevel.Low);

		Assert.Contains(
			string.Format(
				CultureInfo.InvariantCulture,
				AnalyzeConsoleReport.THRESHOLD_HIDES_FINDINGS_MESSAGE_FORMAT,
				1,
				2,
				ConfidenceLevels.Name(ConfidenceLevel.High)),
			renderedAtHigh,
			StringComparison.Ordinal);
		Assert.DoesNotContain("Showing", renderedAtLow, StringComparison.Ordinal);
	}

	[Fact]
	public void ALiteralCredentialFindingProducesExactlyOneWarningLineNamingTheGuide()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		Finding finding = ReportHarness.FindingAt(
			1,
			"a.cs",
			1,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			ConfidenceLevel.High,
			literalCredentialSignal: true);

		string rendered = Render([finding], catalog, ConfidenceLevels.WIDEST);

		Assert.Contains(
			string.Format(
				CultureInfo.InvariantCulture,
				AnalyzeConsoleReport.LITERAL_CREDENTIAL_WARNING_MESSAGE_FORMAT,
				finding.Identifier,
				"a.cs:1",
				ReportVocabulary.PatternClassText(catalog, finding)),
			rendered,
			StringComparison.Ordinal);
	}

	[Fact]
	public void AFindingWithNoLiteralCredentialSignalProducesNoWarningLine()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		Finding finding = ReportHarness.FindingAt(
			1,
			"a.cs",
			1,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			ConfidenceLevel.High,
			literalCredentialSignal: false);

		string rendered = Render([finding], catalog, ConfidenceLevels.WIDEST);

		Assert.DoesNotContain("Warning:", rendered, StringComparison.Ordinal);
	}

	/// <summary>
	/// The declared row cap, exercised exactly: 205 findings, all reported, produce precisely
	/// 200 table rows and a pointer line naming the other 5 - the arithmetic
	/// <see cref="Pipeline.AnalyzeStageTests"/> proves once more through the real pipeline, where
	/// exactness is harder to isolate from a fixture language's own vocabulary.
	/// </summary>
	[Fact]
	public void TheTableCapsAtTheDeclaredBoundAndNamesTheRemainder()
	{
		const int overflowCount = 5;

		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		ImmutableArray<Finding> findings = BuildManyHighFindings(
			ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS + overflowCount);

		string rendered = Render(findings, catalog, ConfidenceLevels.WIDEST);

		int rowCount = rendered
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Count(line => line.Contains(ReportHarness.TEMPLATED_PATTERN_CLASS, StringComparison.Ordinal));

		Assert.Equal(ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS, rowCount);
		Assert.Contains(
			string.Format(CultureInfo.InvariantCulture, AnalyzeConsoleReport.FURTHER_FINDINGS_NOT_SHOWN_MESSAGE_FORMAT, overflowCount),
			rendered,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The warning list's own cap, independent of the table's - every finding here is a credential
	/// one, all reported, so every omitted warning is in the report.
	/// </summary>
	[Fact]
	public void TheWarningListCapsAtTheDeclaredBoundAndNamesTheRemainder()
	{
		const int overflowCount = 5;

		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		ImmutableArray<Finding> findings = BuildManyHighFindings(
			ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS + overflowCount,
			literalCredentialSignal: true);

		string rendered = Render(findings, catalog, ConfidenceLevels.WIDEST);

		int warningLineCount = rendered
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Count(line => line.StartsWith("Warning:", StringComparison.Ordinal));

		Assert.Equal(ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS, warningLineCount);
		Assert.Contains(
			string.Format(
				CultureInfo.InvariantCulture,
				AnalyzeConsoleReport.FURTHER_LITERAL_CREDENTIAL_WARNINGS_MESSAGE_FORMAT,
				overflowCount,
				overflowCount),
			rendered,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// At a threshold above Low, a Low literal-credential finding is
	/// still warned about - the warning names no threshold - but the warning says the report does
	/// not list it, so "the report lists them all" is never claimed of a set the report does not hold.
	/// </summary>
	[Fact]
	public void ALowFindingsWarningNamesItselfAsBelowTheReportingThreshold()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		Finding lowFinding = ReportHarness.FindingAt(
			1,
			"a.cs",
			1,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			ConfidenceLevel.Low,
			literalCredentialSignal: true);
		Finding mediumFinding = ReportHarness.FindingAt(
			2,
			"b.cs",
			1,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			ConfidenceLevel.Medium,
			literalCredentialSignal: true);

		string rendered = Render([lowFinding, mediumFinding], catalog, ConfidenceLevel.Medium);

		string[] warningLines = rendered
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Select(line => line.TrimEnd('\r'))
			.Where(line => line.StartsWith("Warning:", StringComparison.Ordinal))
			.ToArray();

		Assert.Equal(2, warningLines.Length);
		Assert.Contains(
			warningLines,
			line => line.Contains(lowFinding.Identifier, StringComparison.Ordinal)
				&& line.EndsWith(AnalyzeConsoleReport.BELOW_REPORTING_THRESHOLD_SUFFIX, StringComparison.Ordinal));
		Assert.Contains(
			warningLines,
			line => line.Contains(mediumFinding.Identifier, StringComparison.Ordinal)
				&& !line.Contains(AnalyzeConsoleReport.BELOW_REPORTING_THRESHOLD_SUFFIX, StringComparison.Ordinal));
	}

	/// <summary>
	/// Past the row cap, the pointer's second number is how many of
	/// the omitted warnings are actually in the report - here three of the five omitted findings are
	/// Low, below the Medium threshold in effect, so only two of the five are in the report.
	/// </summary>
	[Fact]
	public void TheWarningOverflowPointerCountsOnlyTheOmittedWarningsTheReportActuallyLists()
	{
		const int overflowCount = 5;
		const int overflowBelowThresholdCount = 3;
		const int overflowInReportCount = overflowCount - overflowBelowThresholdCount;

		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();
		ImmutableArray<Finding> findings = BuildManyCredentialFindingsWithTrailingLowLevelFindings(
			ConsoleFindingsTable.MAXIMUM_CONSOLE_TABLE_ROWS + overflowCount,
			overflowBelowThresholdCount);

		string rendered = Render(findings, catalog, ConfidenceLevel.Medium);

		Assert.Contains(
			string.Format(
				CultureInfo.InvariantCulture,
				AnalyzeConsoleReport.FURTHER_LITERAL_CREDENTIAL_WARNINGS_MESSAGE_FORMAT,
				overflowCount,
				overflowInReportCount),
			rendered,
			StringComparison.Ordinal);
	}

	private static ImmutableArray<Finding> BuildManyHighFindings(int count, bool literalCredentialSignal = false)
	{
		return
		[
			.. Enumerable.Range(1, count).Select(index => ReportHarness.FindingAt(
				index,
				$"file-{index}.cs",
				1,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High,
				literalCredentialSignal: literalCredentialSignal))
		];
	}

	/// <summary>
	/// <paramref name="trailingLowLevelCount"/> findings at the END of the array are Low; the rest
	/// are High. <see cref="AnalyzeConsoleReport"/> never reorders, so the trailing Low findings are
	/// exactly the ones the row cap omits when <paramref name="totalCount"/> exceeds the cap by more
	/// than <paramref name="trailingLowLevelCount"/>.
	/// </summary>
	private static ImmutableArray<Finding> BuildManyCredentialFindingsWithTrailingLowLevelFindings(
		int totalCount,
		int trailingLowLevelCount)
	{
		return
		[
			.. Enumerable.Range(1, totalCount).Select(index => ReportHarness.FindingAt(
				index,
				$"file-{index}.cs",
				1,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				index > totalCount - trailingLowLevelCount ? ConfidenceLevel.Low : ConfidenceLevel.High,
				literalCredentialSignal: true))
		];
	}

	private static string Render(ImmutableArray<Finding> findings, LanguageCatalog catalog, ConfidenceLevel minimumConfidenceLevel)
	{
		using StringWriter writer = new();

		AnalyzeConsoleReport.Render(writer, findings, catalog, minimumConfidenceLevel);

		return writer.ToString();
	}
}
