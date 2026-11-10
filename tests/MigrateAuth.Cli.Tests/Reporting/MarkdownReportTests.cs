using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The report's shape, checked on the empty run.
///
/// A repository with zero candidate files must produce a VALID report stating zero findings, and
/// validity is unassertable against a shape nobody
/// specified. These tests are that shape - five sections, in order, present when empty - plus the
/// two rules the report design stresses most: the groups are named for what is
/// known, and the second group is never "not Relativity".
/// </summary>
public sealed class MarkdownReportTests
{
	[Fact]
	public void AnEmptyRunProducesAllFiveSectionsWithCoverageFirst()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		int coverage = IndexOf(report, MarkdownReportRenderer.COVERAGE_HEADING);
		int scanSummary = IndexOf(report, MarkdownReportRenderer.SCAN_SUMMARY_HEADING);
		int skippedItems = IndexOf(report, MarkdownReportRenderer.SKIPPED_ITEMS_HEADING);
		int findings = IndexOf(report, MarkdownReportRenderer.FINDINGS_HEADING);
		int provenance = IndexOf(report, MarkdownReportRenderer.PROVENANCE_HEADING);

		Assert.True(coverage < scanSummary);
		Assert.True(scanSummary < skippedItems);
		Assert.True(skippedItems < findings);
		Assert.True(findings < provenance);
	}

	/// <summary>
	/// The half of the shape that carries the decision: a missing "skipped items" heading and a
	/// "skipped items: none" heading say opposite things to a reader.
	/// </summary>
	[Fact]
	public void TheSkippedItemsSectionStatesZeroRatherThanBeingAbsent()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Contains(MarkdownReportRenderer.SKIPPED_ITEMS_HEADING, report, StringComparison.Ordinal);
		Assert.Contains("Skipped files: 0.", report, StringComparison.Ordinal);
		Assert.Contains("Directories whose contents could not be listed: 0.", report, StringComparison.Ordinal);
	}

	[Fact]
	public void AnEmptyRunStatesZeroFindingsUnderBothGroups()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Contains(MarkdownReportRenderer.ESTABLISHED_GROUP_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.NOT_ESTABLISHED_GROUP_HEADING, report, StringComparison.Ordinal);
		Assert.Equal(2, CountOccurrences(report, MarkdownReportRenderer.NO_FINDINGS_IN_GROUP));
	}

	[Fact]
	public void TheCoverageSectionNamesAllEightLanguagesIncludingTheUnsupportedOnes()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low, AssessedLanguage.Shell)));

		foreach (AssessedLanguage language in AssessedLanguages.AllInDeclaredOrder)
		{
			Assert.Contains(AssessedLanguages.CanonicalName(language), report, StringComparison.Ordinal);
		}

		Assert.Contains(CoverageStatement.DECLARED_UNSUPPORTED, report, StringComparison.Ordinal);
		Assert.Contains(CoverageStatement.DISABLED_BY_CONFIGURATION, report, StringComparison.Ordinal);
		Assert.Contains("supported, 2 classes searched", report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The finding model and the report structure both insist on this, and the latter states it as a
	/// prohibition: the second group is "not established" and must never be rendered as "not Relativity", because the engine
	/// establishes nothing about those findings in either direction.
	/// </summary>
	[Fact]
	public void TwoFindingsOfDifferentCertaintyLandInDifferentGroupsNamedForWhatIsKnown()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "established.cs", 10, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(
				2,
				"unknown.cs",
				20,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High,
				DestinationCertainty.Unknown)
		];

		string report = Render(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings));

		string findingsSection = report[IndexOf(report, MarkdownReportRenderer.FINDINGS_HEADING)..];
		int establishedGroup = IndexOf(findingsSection, MarkdownReportRenderer.ESTABLISHED_GROUP_HEADING);
		int notEstablishedGroup = IndexOf(findingsSection, MarkdownReportRenderer.NOT_ESTABLISHED_GROUP_HEADING);

		Assert.InRange(IndexOf(findingsSection, "established.cs"), establishedGroup, notEstablishedGroup);
		Assert.True(IndexOf(findingsSection, "unknown.cs") > notEstablishedGroup);
		Assert.Contains(ReportVocabulary.DESTINATION_ESTABLISHED_LABEL, report, StringComparison.OrdinalIgnoreCase);
		Assert.Contains(ReportVocabulary.DESTINATION_NOT_ESTABLISHED_LABEL, report, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("not Relativity", report, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// The threshold filter removes rows and renumbering what is left is forbidden, so the filtered
	/// report shows a gap in its sequence. The gap is the declared cost of a stable identifier.
	/// </summary>
	[Fact]
	public void TheThresholdHidesALowFindingAndLeavesTheOtherIdentifiersUntouched()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "low.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low),
			ReportHarness.FindingAt(2, "high.cs", 2, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		string report = Render(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Medium),
			findings));

		Assert.DoesNotContain("low.cs", report, StringComparison.Ordinal);
		Assert.Contains("high.cs", report, StringComparison.Ordinal);
		Assert.Contains(FindingIdentifiers.Of(2), report, StringComparison.Ordinal);
		Assert.Contains("Findings reported: 1 of 2 recognized.", report, StringComparison.Ordinal);
	}

	[Fact]
	public void ALiteralCredentialSignalAddsTheSecurityPieceAndItsAbsenceDoesNot()
	{
		string withSignal = RenderOne(ConfidenceLevel.High, ReportHarness.TEMPLATED_PATTERN_CLASS, literalCredentialSignal: true);
		string withoutSignal = RenderOne(ConfidenceLevel.High, ReportHarness.TEMPLATED_PATTERN_CLASS, literalCredentialSignal: false);

		Assert.Contains(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, withSignal, StringComparison.Ordinal);
		Assert.DoesNotContain(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, withoutSignal, StringComparison.Ordinal);
	}

	[Fact]
	public void ATemplatedFindingRendersTheTemplatesFourPieces()
	{
		string report = RenderOne(ConfidenceLevel.High, ReportHarness.TEMPLATED_PATTERN_CLASS, literalCredentialSignal: false);

		Assert.Contains(ReportHarness.TEMPLATE_SOURCE_FORM, report, StringComparison.Ordinal);
		Assert.Contains(ReportHarness.TEMPLATE_TARGET_FORM, report, StringComparison.Ordinal);
		Assert.Contains(ReportHarness.TEMPLATE_CREDENTIAL_PROVENANCE, report, StringComparison.Ordinal);
		Assert.Contains(ReportHarness.TEMPLATE_VERIFICATION_NOTE, report, StringComparison.Ordinal);
	}

	[Fact]
	public void ALowFindingIsRoutedToManualReviewRatherThanToATemplate()
	{
		string report = RenderOne(ConfidenceLevel.Low, ReportHarness.TEMPLATED_PATTERN_CLASS, literalCredentialSignal: false);

		Assert.Contains(RecommendationRenderer.MANUAL_REVIEW_LEAD_TEXT, report, StringComparison.Ordinal);
		Assert.Contains("\"Judging a Low-confidence match\" in Part C for C# of the Migration Documentation.", report, StringComparison.Ordinal);
		Assert.DoesNotContain(ReportHarness.TEMPLATE_SOURCE_FORM, report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The naming promise holds at every level, including Low - the finding
	/// heading carries the display name whether or not `RecommendationRenderer.Route` ever
	/// consults a template for it, and the identifier stays visible beside it.
	/// </summary>
	[Theory]
	[InlineData(ConfidenceLevel.Low)]
	[InlineData(ConfidenceLevel.Medium)]
	[InlineData(ConfidenceLevel.High)]
	public void TheDisplayNameRendersInTheFindingHeadingAtEveryConfidenceLevel(ConfidenceLevel level)
	{
		string report = RenderOne(level, ReportHarness.TEMPLATED_PATTERN_CLASS, literalCredentialSignal: false);

		Assert.Contains(ReportHarness.DisplayNameFor(ReportHarness.TEMPLATED_PATTERN_CLASS), report, StringComparison.Ordinal);
		Assert.Contains(ReportHarness.TEMPLATED_PATTERN_CLASS, report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The coverage section's third piece: a supported language's declared
	/// unsupported constructs appear ordinally by <c>Construct</c>, not in declaration order - see
	/// <see cref="ReportHarness.LOWERCASE_FIRST_CONSTRUCT"/> for why the two disagree here.
	/// </summary>
	[Fact]
	public void TheCoverageSectionListsDeclaredUnsupportedConstructsInOrdinalOrder()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithDeclaredUnsupportedCoverage(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Contains(MarkdownReportRenderer.DECLARED_UNSUPPORTED_CONSTRUCTS_HEADING, report, StringComparison.Ordinal);

		int uppercaseIndex = IndexOf(report, ReportHarness.UPPERCASE_FIRST_CONSTRUCT);
		int lowercaseIndex = IndexOf(report, ReportHarness.LOWERCASE_FIRST_CONSTRUCT);

		Assert.True(uppercaseIndex < lowercaseIndex);
	}

	/// <summary>The zero case states itself explicitly, on the report's own "present when empty" convention.</summary>
	[Fact]
	public void TheCoverageSectionSaysNoneDeclaredWhenNothingIs()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Contains(MarkdownReportRenderer.DECLARED_UNSUPPORTED_CONSTRUCTS_HEADING, report, StringComparison.Ordinal);
		Assert.Contains(MarkdownReportRenderer.NO_DECLARED_UNSUPPORTED_CONSTRUCTS, report, StringComparison.Ordinal);
	}

	/// <summary>The alpha label written out, so the assertion does not compare the constant with itself.</summary>
	private const string _EXPECTED_ALPHA_VERSION = "0.4.0-alpha";

	[Fact]
	public void TheProvenanceCarriesTheAlphaLabelledToolVersionLiteral()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Medium)));

		Assert.Contains(_EXPECTED_ALPHA_VERSION, report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The provenance holds the tool version, the catalog version, the scanned path AS A LABEL and the
	/// applied filters. The label matters: no absolute path is permitted in
	/// anything the product emits, and the repository root is the one path with no relative form.
	/// </summary>
	[Fact]
	public void TheProvenanceCarriesBothVersionsAndTheRepositoryAsALabel()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Medium)));

		Assert.Contains(ToolVersion.CURRENT, report, StringComparison.Ordinal);
		Assert.Contains(LanguageCatalog.CurrentVersion.DisplayValue, report, StringComparison.Ordinal);
		Assert.Contains(ReportHarness.REPOSITORY_LABEL, report, StringComparison.Ordinal);
		Assert.Contains(ConfidenceLevels.Name(ConfidenceLevel.Medium), report, StringComparison.Ordinal);
		Assert.DoesNotContain(Path.GetTempPath(), report, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// A repository that narrowed its own assessment says so in the report's provenance, in the
	/// same words Pre-flight used and without any text from its file.
	/// </summary>
	[Fact]
	public void TheProvenanceStatesWhatTheRepositoryFileDisabledAndRaised()
	{
		RepositoryConfigurationEffect effect = new()
		{
			LanguagesSetByRepository = [AssessedLanguage.Python],
			DisabledLanguages = [AssessedLanguage.CSharp],
			ThresholdChange = new ConfidenceLevelChange { Previous = ConfidenceLevel.Low, Current = ConfidenceLevel.High }
		};
		EffectiveConfiguration baseline = ReportHarness.ConfigurationWith(ConfidenceLevel.High);
		EffectiveConfiguration configuration = baseline with { Provenance = baseline.Provenance with { RepositoryEffect = effect } };

		string report = Render(ReportHarness.EmptyContent(ReportHarness.CatalogWithCSharpOnly(), configuration));

		Assert.Contains("Repository configuration applied", report, StringComparison.Ordinal);
		Assert.Contains("it disables CSharp", report, StringComparison.Ordinal);
		Assert.Contains("raises the minimum confidence level from Low to High", report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The provenance's language row is labeled "Languages enabled":
	/// it reflects <see cref="Configuration.LanguageEnablement"/>, which is not the same fact as
	/// the scan section's own "Assessed" column.
	/// </summary>
	[Fact]
	public void TheProvenanceLabelsTheLanguageRowEnabledRatherThanAssessed()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Contains("- Languages enabled: ", report, StringComparison.Ordinal);
		Assert.DoesNotContain("Languages assessed", report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The zero case, extended to the findings cap: a run that capped nothing says so explicitly, on the
	/// same "state zero" convention the skipped-items section already follows.
	/// </summary>
	[Fact]
	public void AnEmptyRunStatesTheFindingsCapWasNotTripped()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.Contains(MarkdownReportRenderer.FINDINGS_NOT_CAPPED, report, StringComparison.Ordinal);
		Assert.DoesNotContain("TRUNCATED", report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The cap invariant, over the Markdown report: a truncated result is declared truncated and
	/// never presented as complete, with the omission counts a reader needs to judge the hole.
	/// </summary>
	[Fact]
	public void ATruncatedRunDeclaresItselfTruncatedWithTheOmissionCounts()
	{
		FindingsCapOutcome capOutcome = new()
		{
			TotalRecognizedCount = 2_500,
			RetainedCount = 2_000,
			PerFileCapTrippedFileCount = 1,
			PerFileOmittedCount = 500,
			GlobalCapTripped = false,
			GlobalOmittedCount = 0
		};

		string report = Render(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			[],
			capOutcome));

		Assert.Contains("TRUNCATED", report, StringComparison.Ordinal);
		Assert.Contains("500", report, StringComparison.Ordinal);
		Assert.Contains("2,500", report, StringComparison.Ordinal);
		Assert.DoesNotContain(MarkdownReportRenderer.FINDINGS_NOT_CAPPED, report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The finding groups this line describes render AFTER it, never before.
	/// </summary>
	[Fact]
	public void TheNotCappedSentenceNamesTheFindingsThatFollowItRatherThanPrecedeIt()
	{
		Assert.Contains("below", MarkdownReportRenderer.FINDINGS_NOT_CAPPED, StringComparison.Ordinal);
		Assert.DoesNotContain("above", MarkdownReportRenderer.FINDINGS_NOT_CAPPED, StringComparison.Ordinal);
	}

	/// <summary>
	/// When truncated, "recognized" names the CAP OUTCOME'S true total rather than the
	/// findings array's own length, which is the RETAINED count once anything has been capped -
	/// the same number the line above it already reports under a different label.
	/// </summary>
	[Fact]
	public void ATruncatedRunNamesTheTrueRecognizedTotalRatherThanTheRetainedCount()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "one.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(2, "two.cs", 2, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		FindingsCapOutcome capOutcome = new()
		{
			TotalRecognizedCount = 5,
			RetainedCount = 2,
			PerFileCapTrippedFileCount = 1,
			PerFileOmittedCount = 3,
			GlobalCapTripped = false,
			GlobalOmittedCount = 0
		};

		string report = Render(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings,
			capOutcome));

		Assert.Contains(
			"Findings reported: 2 of 2 kept after the findings caps, out of 5 recognized.",
			report,
			StringComparison.Ordinal);
	}

	/// <summary>The client description, not the engine's own stable identifier.</summary>
	[Fact]
	public void TheSkippedItemsTableReadsInAClientsTermsForEveryCategory()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		foreach (SkippedItemCategory category in SkippedItemCategories.ALL)
		{
			Assert.Contains(ReportVocabulary.DescriptionOf(category), report, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// Identical content must produce identical output, and a writer left on its
	/// platform default would emit CRLF on one operating system and LF on another - a difference
	/// a repeat-run check cannot see, because it runs twice on one machine.
	/// </summary>
	[Fact]
	public void TheReportIsWrittenWithPortableLineEndingsOnEveryPlatform()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));

		Assert.DoesNotContain('\r', report);
	}

	/// <summary>
	/// The scan section's "Assessed" column and
	/// the after-table sentence, over a hand-built <see cref="ScanSummary"/>: a row for the catalog's
	/// own `CSharp` analyzer reads "yes", a row for `VBNet` - which
	/// <see cref="ReportHarness.CatalogWithCSharpOnly"/> ships no analyzer for - reads "no", a row for
	/// <see cref="LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME"/> reads the third, distinct cell
	/// rather than either "yes" or "no", and the after-table sentence still names only the one
	/// unassessed file - the unrecognized bucket's files are never a language this release "does not
	/// assess", because they were never searched at all.
	/// </summary>
	[Fact]
	public void TheScanSummaryTableMarksThreeAssessmentStatesDistinctly()
	{
		LanguageInventoryRow assessedRow = new()
		{
			LanguageName = AssessedLanguages.CanonicalName(AssessedLanguage.CSharp),
			FileCount = 1,
			LineCount = 10,
			CandidateFileCount = 1,
			ShareOfFilesPercentage = 50.0
		};

		LanguageInventoryRow unassessedRow = new()
		{
			LanguageName = AssessedLanguages.CanonicalName(AssessedLanguage.VBNet),
			FileCount = 1,
			LineCount = 5,
			CandidateFileCount = 0,
			ShareOfFilesPercentage = 50.0
		};

		LanguageInventoryRow unrecognizedRow = new()
		{
			LanguageName = LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME,
			FileCount = 1,
			LineCount = 3,
			CandidateFileCount = 0,
			ShareOfFilesPercentage = 0.0
		};

		ReportContent content = ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)) with
		{
			ScanSummary = new ScanSummary
			{
				FileCount = 3,
				LineCount = 18,
				CandidateFileCount = 1,
				Rows = [assessedRow, unassessedRow, unrecognizedRow]
			}
		};

		string report = Render(content);

		Assert.Contains(
			$"| {AssessedLanguages.CanonicalName(AssessedLanguage.CSharp)} | 1 | 10 | 1 | 50.0 % | {MarkdownReportRenderer.ASSESSED_YES} |",
			report,
			StringComparison.Ordinal);
		Assert.Contains(
			$"| {AssessedLanguages.CanonicalName(AssessedLanguage.VBNet)} | 1 | 5 | 0 | 50.0 % | {MarkdownReportRenderer.ASSESSED_NO} |",
			report,
			StringComparison.Ordinal);
		Assert.Contains(
			$"| {LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME} | 1 | 3 | 0 | 0.0 % | {MarkdownReportRenderer.ASSESSED_NOT_APPLICABLE} |",
			report,
			StringComparison.Ordinal);

		string expectedSummary = string.Format(
			CultureInfo.InvariantCulture,
			MarkdownReportRenderer.UNASSESSED_LANGUAGES_SUMMARY_FORMAT,
			1,
			3);

		Assert.Contains(expectedSummary, report, StringComparison.Ordinal);
	}

	/// <summary>
	/// A Markdown renderer reads a line placed directly against a table's last row as one more row.
	/// Section 2 puts sentences after its inventory table (the unassessed-languages summary and the
	/// stale-scan note), so every line after a table row must be another row or a blank line.
	/// </summary>
	[Fact]
	public void NoParagraphFollowsATableRowWithoutABlankLine()
	{
		LanguageInventoryRow assessedRow = new()
		{
			LanguageName = AssessedLanguages.CanonicalName(AssessedLanguage.CSharp),
			FileCount = 1,
			LineCount = 10,
			CandidateFileCount = 1,
			ShareOfFilesPercentage = 50.0
		};

		LanguageInventoryRow unassessedRow = new()
		{
			LanguageName = AssessedLanguages.CanonicalName(AssessedLanguage.Go),
			FileCount = 1,
			LineCount = 5,
			CandidateFileCount = 0,
			ShareOfFilesPercentage = 25.0
		};

		LanguageInventoryRow disabledRow = new()
		{
			LanguageName = AssessedLanguages.CanonicalName(AssessedLanguage.VBNet),
			FileCount = 1,
			LineCount = 5,
			CandidateFileCount = 0,
			ShareOfFilesPercentage = 25.0
		};

		ReportContent content = ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)) with
		{
			ScanSummary = new ScanSummary
			{
				FileCount = 4,
				LineCount = 25,
				CandidateFileCount = 1,
				Rows = [assessedRow, unassessedRow, disabledRow]
			},
			ScanEnabledLanguages = [.. AssessedLanguages.AllInDeclaredOrder.Where(language => language != AssessedLanguage.VBNet)]
		};

		string report = Render(content);

		Assert.True(content.ScanEnabledLanguagesDifferFromSession);
		Assert.Contains(MarkdownReportRenderer.SCAN_ENABLED_LANGUAGES_STALE_MESSAGE, report, StringComparison.Ordinal);
		Assert.Contains(
			string.Format(CultureInfo.InvariantCulture, MarkdownReportRenderer.UNASSESSED_LANGUAGES_SUMMARY_FORMAT, 1, 4),
			report,
			StringComparison.Ordinal);
		Assert.Contains(
			string.Format(CultureInfo.InvariantCulture, MarkdownReportRenderer.DISABLED_LANGUAGES_SUMMARY_FORMAT, 1, 4),
			report,
			StringComparison.Ordinal);

		string[] lines = report.Split('\n');

		for (int position = 0; position < lines.Length - 1; position++)
		{
			if (lines[position].StartsWith('|'))
			{
				Assert.True(
					lines[position + 1].StartsWith('|') || lines[position + 1].Length == 0,
					$"The line after the table row '{lines[position]}' is '{lines[position + 1]}'.");
			}
		}
	}

	private static string RenderOne(ConfidenceLevel level, string patternClass, bool literalCredentialSignal)
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(
				1,
				"source.cs",
				7,
				patternClass,
				level,
				DestinationCertainty.Established,
				literalCredentialSignal)
		];

		return Render(ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings));
	}

	private static string Render(ReportContent content)
	{
		using StringWriter writer = new();

		MarkdownReportRenderer.Render(writer, content, new UnwiredGenericRecommendationProvider());

		return writer.ToString();
	}

	private static int IndexOf(string report, string value)
	{
		int index = report.IndexOf(value, StringComparison.Ordinal);

		Assert.True(index >= 0, $"The report does not contain '{value}'.");

		return index;
	}

	private static int CountOccurrences(string report, string value)
	{
		int count = 0;
		int index = report.IndexOf(value, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = report.IndexOf(value, index + value.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
