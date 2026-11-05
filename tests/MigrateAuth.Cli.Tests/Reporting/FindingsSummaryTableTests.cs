using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// THE FINDINGS SUMMARY AT THE TOP OF THE MARKDOWN REPORT, THE ZERO-FINDINGS SENTENCE THAT STANDS IN ITS
/// PLACE, THE COUNT OF WHAT A RAISED THRESHOLD HIDES, AND THE REASON A LOW FINDING IS LOW.
///
/// The table is an index of section 4, so the property under test is agreement: the rows are exactly the
/// findings the data artifact carries, in its order and under its identifiers. Every test that checks a
/// row checks it against what the artifact says rather than against a second list typed in the test.
/// </summary>
public sealed class FindingsSummaryTableTests
{
	private const string _HIGH_PATH = "src/High.cs";
	private const string _MEDIUM_PATH = "src/Medium.cs";
	private const string _LOW_PATH = "src/Low.cs";
	private const string _BAR_AND_BACKTICK_PATH = "src/we|rd`name.cs";
	private const string _LINE_ENDING_PATH = "src/line\nbreak.cs";
	private const string _ESCAPED_BAR_AND_BACKTICK_PATH = "src/we\\|rd\\`name.cs";
	private const string _SUMMARY_ROW_PREFIX = "| ";
	private const int _COLUMN_COUNT = 6;
	private const char _TABLE_CELL_SEPARATOR = '|';
	private const string _PYTHON_NAME = "Python";
	private const string _POWERSHELL_NAME = "PowerShell";
	private const string _SHELL_NAME = "Shell";
	private const string _VB_NET_NAME = "VBNet";
	private const string _ZERO_FINDINGS_LINE = "0 findings.";
	private const string _ZERO_FINDINGS_WITH_TWO_HIDDEN_LINE = "0 findings; 2 recognized findings are below the minimum confidence level (High) and not shown.";
	private const string _BELOW_THRESHOLD_TEXT = "below the minimum confidence level";
	private const string _NOT_SHOWN_TEXT = "not shown";
	private const string _RAISED_FROM_LOW_STATEMENT =
		"The configuration file in the repository root raises the minimum confidence level from Low to High; 2 findings (Medium and Low) are not shown.";
	private const string _RAISED_FROM_MEDIUM_STATEMENT =
		"The configuration file in the repository root raises the minimum confidence level from Medium to High; 1 findings (Medium) are not shown.";

	[Fact]
	public void TheSummarySitsRightAfterTheTitleAndAheadOfTheFiveSections()
	{
		string report = Render(ContentWithThreeFindings(ConfidenceLevel.Low));

		int title = report.IndexOf(MarkdownReportRenderer.REPORT_TITLE, StringComparison.Ordinal);
		int summary = report.IndexOf(MarkdownReportRenderer.FINDINGS_SUMMARY_HEADING, StringComparison.Ordinal);
		int coverage = report.IndexOf(MarkdownReportRenderer.COVERAGE_HEADING, StringComparison.Ordinal);

		Assert.Equal(0, title);
		Assert.InRange(summary, title + 1, coverage - 1);
	}

	[Fact]
	public void TheTableHasTheSixRequiredColumnsPlusTheFlow()
	{
		string report = Render(ContentWithThreeFindings(ConfidenceLevel.Low));

		Assert.Contains("| ID | File:Line | Language | Pattern | Confidence | Flow |", report, StringComparison.Ordinal);
		Assert.Contains("| --- | --- | --- | --- | --- | --- |", report, StringComparison.Ordinal);
	}

	[Fact]
	public void TheRowsAreExactlyTheFindingsOfTheDataArtifactInItsOrderAndUnderItsIdentifiers()
	{
		ReportContent content = ContentWithThreeFindings(ConfidenceLevel.Medium);

		string[] tableIdentifiers = [.. SummaryRowsOf(Render(content)).Select(row => CellsOf(row)[0])];
		string[] artifactIdentifiers = [.. ReadArtifact(content).Findings.Select(finding => finding.Id)];

		Assert.Equal(["001", "002"], artifactIdentifiers);
		Assert.Equal(artifactIdentifiers, tableIdentifiers);
	}

	[Fact]
	public void ARowCarriesTheLocationTheLanguageThePatternTheConfidenceAndTheFlow()
	{
		string[] cells = CellsOf(SummaryRowsOf(Render(ContentWithThreeFindings(ConfidenceLevel.Low))).First());

		Assert.Equal(_COLUMN_COUNT, cells.Length);
		Assert.Equal("001", cells[0]);
		Assert.Equal($"src/High.cs:10", cells[1].Replace("\\", string.Empty, StringComparison.Ordinal));
		Assert.Equal("CSharp", cells[2]);
		Assert.Contains(ReportHarness.TEMPLATED_PATTERN_CLASS, cells[3], StringComparison.Ordinal);
		Assert.Equal("High", cells[4]);
		Assert.Equal("ClientCredentials", cells[5]);
	}

	[Fact]
	public void ALowFindingsFlowCellSaysNone()
	{
		string[] cells = CellsOf(SummaryRowsOf(Render(ContentWithThreeFindings(ConfidenceLevel.Low))).Last());

		Assert.Equal("Low", cells[4]);
		Assert.Equal("none", cells[5]);
	}

	[Fact]
	public void ABarAndABacktickInAPathCannotEndTheCellOrOpenACodeSpan()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, _BAR_AND_BACKTICK_PATH, 3, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		string row = SummaryRowsOf(Render(ContentOf(findings, ConfidenceLevel.Low))).Single();

		Assert.Contains(_ESCAPED_BAR_AND_BACKTICK_PATH, row, StringComparison.Ordinal);
		Assert.Equal(_COLUMN_COUNT, CellsOf(row).Length);
	}

	[Fact]
	public void ALineEndingInAPathCannotEndTheRow()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, _LINE_ENDING_PATH, 3, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		string report = Render(ContentOf(findings, ConfidenceLevel.Low));
		string[] rows = SummaryRowsOf(report);

		Assert.Single(rows);
		Assert.Equal(_COLUMN_COUNT, CellsOf(rows[0]).Length);
	}

	[Fact]
	public void WithNothingToListTheTableIsReplacedByASingleLine()
	{
		string report = Render(ReportHarness.EmptyContent(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low)));
		string summary = SectionBetween(report, MarkdownReportRenderer.FINDINGS_SUMMARY_HEADING, MarkdownReportRenderer.COVERAGE_HEADING);

		Assert.Contains("0 findings.", summary, StringComparison.Ordinal);
		Assert.DoesNotContain("| ID |", summary, StringComparison.Ordinal);
		Assert.DoesNotContain("| --- |", summary, StringComparison.Ordinal);
	}

	[Fact]
	public void TheZeroFindingsLineNamesTheLanguagesThisReleaseDoesNotAssess()
	{
		ReportContent content = EmptyContentWithRows(
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			RowOf(_PYTHON_NAME, 1),
			RowOf(_POWERSHELL_NAME, 1),
			RowOf(_SHELL_NAME, 1));

		string report = Render(content);
		string summary = SectionBetween(report, MarkdownReportRenderer.FINDINGS_SUMMARY_HEADING, MarkdownReportRenderer.COVERAGE_HEADING);
		string findings = SectionBetween(report, MarkdownReportRenderer.FINDINGS_HEADING, MarkdownReportRenderer.ESTABLISHED_GROUP_HEADING);
		const string expected = "0 findings; 3 files in languages this release does not assess (Python, PowerShell, Shell).";

		Assert.Contains(expected, summary, StringComparison.Ordinal);
		Assert.Contains(expected, findings, StringComparison.Ordinal);
	}

	[Fact]
	public void ASingleFileIsNotCalledFiles()
	{
		string summary = SummaryOf(EmptyContentWithRows(ReportHarness.ConfigurationWith(ConfidenceLevel.Low), RowOf(_PYTHON_NAME, 1)));

		Assert.Contains("1 file in languages this release does not assess (Python).", summary, StringComparison.Ordinal);
	}

	[Fact]
	public void LanguagesTheRepositorysFileDisabledAreSaidAsTheRepositorysFile()
	{
		EffectiveConfiguration baseline = ReportHarness.ConfigurationWith(ConfidenceLevel.Low, AssessedLanguage.VBNet);
		EffectiveConfiguration configuration = baseline with
		{
			Provenance = baseline.Provenance with { EnabledLanguagesOrigin = ConfigurationSource.RepositoryFile }
		};

		string summary = SummaryOf(EmptyContentWithRows(configuration, RowOf(_VB_NET_NAME, 2)));

		Assert.Contains(
			"0 findings; 2 files in languages disabled by the repository's configuration file (VBNet).",
			summary,
			StringComparison.Ordinal);
	}

	[Fact]
	public void LanguagesDisabledByAnyOtherSourceAreSaidAsConfiguration()
	{
		EffectiveConfiguration configuration = ReportHarness.ConfigurationWith(ConfidenceLevel.Low, AssessedLanguage.VBNet);

		string summary = SummaryOf(EmptyContentWithRows(configuration, RowOf(_VB_NET_NAME, 2)));

		Assert.Contains("0 findings; 2 files in languages disabled by configuration (VBNet).", summary, StringComparison.Ordinal);
		Assert.DoesNotContain("repository's configuration file", summary, StringComparison.Ordinal);
	}

	[Fact]
	public void ALanguageTheConfigurationDisabledIsNamedUnderTheTableWhenThereAreFindings()
	{
		EffectiveConfiguration configuration = ReportHarness.ConfigurationWith(ConfidenceLevel.Low, AssessedLanguage.VBNet);
		ReportContent content = ContentWithThreeFindings(ConfidenceLevel.Low) with
		{
			Configuration = configuration,
			ScanEnabledLanguages = configuration.EnabledLanguagesInDeclaredOrder,
			ScanSummary = new ScanSummary
			{
				FileCount = 2,
				LineCount = 2,
				CandidateFileCount = 0,
				Rows = [RowOf(_VB_NET_NAME, 2)]
			}
		};

		string summary = SummaryOf(content);

		Assert.Contains("Not assessed, because the configuration disabled VBNet.", summary, StringComparison.Ordinal);
		Assert.True(
			summary.IndexOf("Not assessed, because", StringComparison.Ordinal) > summary.IndexOf("src/Low.cs", StringComparison.Ordinal),
			"The note follows the table.");
	}

	[Fact]
	public void NoDisabledLanguageNoteIsWrittenWhenNoLanguageIsDisabled()
	{
		string summary = SummaryOf(ContentWithThreeFindings(ConfidenceLevel.Low));

		Assert.DoesNotContain("Not assessed, because", summary, StringComparison.Ordinal);
	}

	[Fact]
	public void FindingsWithALiteralCredentialThatTheThresholdHidesAreSaidInTheSummary()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, _HIGH_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			ReportHarness.FindingAt(2, _LOW_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low, literalCredentialSignal: true),
			ReportHarness.FindingAt(3, _MEDIUM_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium, literalCredentialSignal: true)
		];

		Assert.Equal(2, ThresholdFilter.CountHiddenWithLiteralCredential(findings, ConfidenceLevel.High));

		string summary = SummaryOf(ContentOf(findings, ConfidenceLevel.High));

		Assert.Contains(
			"Of the findings not shown at the minimum confidence level (High), 2 carry a literal credential. The console warned about them. Lower the minimum confidence level to list them in this report.",
			summary,
			StringComparison.Ordinal);
	}

	[Fact]
	public void NoLiteralCredentialStatementIsWrittenWhenNoneIsHidden()
	{
		string summary = SummaryOf(ContentWithThreeFindings(ConfidenceLevel.Medium));

		Assert.DoesNotContain("carry a literal credential", summary, StringComparison.Ordinal);
		Assert.DoesNotContain("carries a literal credential", summary, StringComparison.Ordinal);
	}

	[Fact]
	public void FindingsTheThresholdHidesAreNamedWhenNothingIsShown()
	{
		ReportContent content = ContentWithThreeFindings(ConfidenceLevel.High) with
		{
			Findings =
			[
				ReportHarness.FindingAt(1, _LOW_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low),
				ReportHarness.FindingAt(2, _MEDIUM_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium)
			]
		};

		string summary = SummaryOf(content);

		Assert.Contains("0 findings; 2 recognized findings are below the minimum confidence level (High) and not shown.", summary, StringComparison.Ordinal);
	}

	[Fact]
	public void ARaisedThresholdStatesItsSourceAndHowManyFindingsItHides()
	{
		ReportContent content = RaisedByTheRepository(ContentWithThreeFindings(ConfidenceLevel.High));

		string report = Render(content);
		string summary = SectionBetween(report, MarkdownReportRenderer.FINDINGS_SUMMARY_HEADING, MarkdownReportRenderer.COVERAGE_HEADING);
		string provenance = report[report.IndexOf(MarkdownReportRenderer.PROVENANCE_HEADING, StringComparison.Ordinal)..];
		const string expected =
			"The configuration file in the repository root raises the minimum confidence level from Low to High; 2 findings (Medium and Low) are not shown.";

		Assert.Contains(expected, summary.Replace("\\", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
		Assert.Contains(expected, provenance.Replace("\\", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
	}

	/// <summary>
	/// With nothing shown because the repository's own file raised the threshold over every finding, the
	/// summary says how many are hidden once, in the raised-threshold statement, and not a second time in
	/// the zero-findings line just above it. Section 4 keeps the full zero-findings line, where nothing
	/// else says it.
	/// </summary>
	[Fact]
	public void ARaiseThatHidesEveryFindingIsCountedOnceInTheSummary()
	{
		ReportContent content = RaisedByTheRepository(ContentOf(LowAndMediumFindings(), ConfidenceLevel.High));

		string report = Render(content);
		string summary = SectionBetween(report, MarkdownReportRenderer.FINDINGS_SUMMARY_HEADING, MarkdownReportRenderer.COVERAGE_HEADING)
			.Replace("\\", string.Empty, StringComparison.Ordinal);
		string findingsSection = SectionBetween(report, MarkdownReportRenderer.FINDINGS_HEADING, MarkdownReportRenderer.PROVENANCE_HEADING);

		Assert.Contains(_ZERO_FINDINGS_LINE, summary.Split('\n').Select(line => line.TrimEnd('\r')));
		Assert.DoesNotContain(_BELOW_THRESHOLD_TEXT, summary, StringComparison.Ordinal);
		Assert.Equal(1, CountOccurrences(summary, _NOT_SHOWN_TEXT));
		Assert.Contains(_RAISED_FROM_LOW_STATEMENT, summary, StringComparison.Ordinal);
		Assert.Contains(_ZERO_FINDINGS_WITH_TWO_HIDDEN_LINE, findingsSection, StringComparison.Ordinal);
	}

	/// <summary>
	/// When a level set elsewhere already hid some findings and the repository's raise hid the rest, the
	/// two counts are different facts, so the summary keeps both.
	/// </summary>
	[Fact]
	public void ARaiseThatHidesOnlySomeOfTheHiddenFindingsKeepsBothCounts()
	{
		ReportContent content = RaisedByTheRepository(ContentOf(LowAndMediumFindings(), ConfidenceLevel.High), ConfidenceLevel.Medium);

		string summary = SummaryOf(content).Replace("\\", string.Empty, StringComparison.Ordinal);

		Assert.Contains(_ZERO_FINDINGS_WITH_TWO_HIDDEN_LINE, summary, StringComparison.Ordinal);
		Assert.Contains(_RAISED_FROM_MEDIUM_STATEMENT, summary, StringComparison.Ordinal);
	}

	[Fact]
	public void TheArtifactCarriesTheSameHiddenCountAsTheReport()
	{
		ReportContent content = RaisedByTheRepository(ContentWithThreeFindings(ConfidenceLevel.High));

		ArtifactThresholdChangeDocument change = ReadArtifact(content).Provenance.RepositoryConfigurationEffect.ThresholdChange!;

		Assert.Equal(2, change.HiddenFindingCount);
		Assert.Equal("Raised", change.Direction);
	}

	[Fact]
	public void TheHiddenCountIsTheFindingsBetweenThePreviousAndTheCurrentLevelOnly()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, _LOW_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low),
			ReportHarness.FindingAt(2, _MEDIUM_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium),
			ReportHarness.FindingAt(3, _HIGH_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];
		ConfidenceLevelChange fromMediumToHigh = new() { Previous = ConfidenceLevel.Medium, Current = ConfidenceLevel.High };

		Assert.Equal(1, ThresholdFilter.CountHiddenByRaise(findings, fromMediumToHigh));
		Assert.Equal(2, ThresholdFilter.CountHiddenByRaise(findings, new ConfidenceLevelChange { Previous = ConfidenceLevel.Low, Current = ConfidenceLevel.High }));
		Assert.Equal(0, ThresholdFilter.CountHiddenByRaise(findings, new ConfidenceLevelChange { Previous = ConfidenceLevel.High, Current = ConfidenceLevel.Low }));
	}

	[Fact]
	public void ALoweredThresholdHidesNothingAndSaysSo()
	{
		EffectiveConfiguration baseline = ReportHarness.ConfigurationWith(ConfidenceLevel.Low);
		EffectiveConfiguration configuration = baseline with
		{
			Provenance = baseline.Provenance with
			{
				RepositoryEffect = new RepositoryConfigurationEffect
				{
					ThresholdChange = new ConfidenceLevelChange { Previous = ConfidenceLevel.High, Current = ConfidenceLevel.Low }
				}
			}
		};
		ReportContent content = ContentWithThreeFindings(ConfidenceLevel.Low) with { Configuration = configuration };

		Assert.Equal(0, ReadArtifact(content).Provenance.RepositoryConfigurationEffect.ThresholdChange!.HiddenFindingCount);
		Assert.DoesNotContain("are not shown", SummaryOf(content), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(LowConfidenceReasons.PatternClass, "this pattern class is Low by definition")]
	[InlineData(LowConfidenceReasons.CommentOrStringLiteral, "the match sits in a comment or a string literal")]
	[InlineData(LowConfidenceReasons.DemotedFile, "the file's name, folder or content marks it as a test, sample, generated or deployment file")]
	[InlineData(LowConfidenceReasons.DemotedVendoredFile, "the file sits in a vendor or third-party folder, so it is code the repository carries rather than its own")]
	public void ALowFindingStatesTheReasonItIsLow(LowConfidenceReasons reason, string expectedText)
	{
		ReportContent content = ContentOf(
			[ReportHarness.FindingAt(1, _LOW_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low, lowReasons: reason)],
			ConfidenceLevel.Low);

		string report = Render(content);

		Assert.Contains($"- Low because: {expectedText}", report, StringComparison.Ordinal);
		Assert.Equal([LowReasonVocabulary.ValuesOf(reason).Single()], ReadArtifact(content).Findings.Single().LowReasons!);
	}

	[Fact]
	public void ADemotedPowerShellFindingDoesNotMentionADeploymentFile()
	{
		ReportContent content = ContentOf(
			[
				ReportHarness.FindingAt(
					1,
					_LOW_PATH,
					1,
					ReportHarness.TEMPLATED_PATTERN_CLASS,
					ConfidenceLevel.Low,
					language: AssessedLanguage.PowerShell,
					lowReasons: LowConfidenceReasons.DemotedFile)
			],
			ConfidenceLevel.Low);

		string report = Render(content);

		Assert.Contains("- Low because: the file's name, folder or content marks it as a test, sample or generated file", report, StringComparison.Ordinal);
		Assert.DoesNotContain("deployment file", report, StringComparison.Ordinal);
	}

	[Fact]
	public void SeveralReasonsAreAllStated()
	{
		LowConfidenceReasons both = LowConfidenceReasons.CommentOrStringLiteral | LowConfidenceReasons.DemotedFile;
		ReportContent content = ContentOf(
			[ReportHarness.FindingAt(1, _LOW_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low, lowReasons: both)],
			ConfidenceLevel.Low);

		string report = Render(content);

		Assert.Contains("comment or a string literal, so it is not executable code; the file's name", report, StringComparison.Ordinal);
		Assert.Equal(["commentOrStringLiteral", "demotedFile"], ReadArtifact(content).Findings.Single().LowReasons!);
	}

	[Fact]
	public void ALowFindingWithNoRecordedReasonSaysSoRatherThanGuessing()
	{
		ReportContent content = ContentOf(
			[ReportHarness.FindingAt(1, _LOW_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)],
			ConfidenceLevel.Low);

		Assert.Contains("- Low because: the reason was not recorded", Render(content), StringComparison.Ordinal);
		Assert.Empty(ReadArtifact(content).Findings.Single().LowReasons!);
	}

	[Fact]
	public void AFindingThatIsNotLowStatesNoReasonAndItsArtifactFieldIsNull()
	{
		ReportContent content = ContentWithThreeFindings(ConfidenceLevel.Low);

		ArtifactFindingDocument high = ReadArtifact(content).Findings.First();

		Assert.Null(high.LowReasons);
		Assert.Single(Render(content).Split("Low because", StringSplitOptions.None).Skip(1));
	}

	[Fact]
	public void EveryReportedFindingGetsARowInIdentifierOrder()
	{
		ImmutableArray<Finding> findings =
		[
			.. Enumerable.Range(1, 5).Select(index => ReportHarness.FindingAt(
				index,
				$"src/file{index}.cs",
				index,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High))
		];

		string[] rows = SummaryRowsOf(Render(ContentOf(findings, ConfidenceLevel.Low)));

		Assert.Equal(5, rows.Length);
		Assert.Equal(findings.Select(finding => finding.Identifier), rows.Select(row => CellsOf(row)[0]));
	}

	private static ReportContent ContentWithThreeFindings(ConfidenceLevel minimumLevel)
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(
				1,
				_HIGH_PATH,
				10,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High,
				flow: CredentialFlow.ClientCredentials,
				flowDecision: FlowDecision.Default),
			ReportHarness.FindingAt(
				2,
				_MEDIUM_PATH,
				20,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.Medium,
				flow: CredentialFlow.AuthorizationCode,
				flowDecision: FlowDecision.InteractiveSignal),
			ReportHarness.FindingAt(3, _LOW_PATH, 30, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)
		];

		return ContentOf(findings, minimumLevel);
	}

	private static ReportContent ContentOf(ImmutableArray<Finding> findings, ConfidenceLevel minimumLevel)
	{
		return ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(minimumLevel),
			findings);
	}

	private static ImmutableArray<Finding> LowAndMediumFindings()
	{
		return
		[
			ReportHarness.FindingAt(1, _LOW_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low),
			ReportHarness.FindingAt(2, _MEDIUM_PATH, 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium)
		];
	}

	private static int CountOccurrences(string text, string fragment)
	{
		int count = 0;
		int index = text.IndexOf(fragment, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = text.IndexOf(fragment, index + fragment.Length, StringComparison.Ordinal);
		}

		return count;
	}

	private static ReportContent RaisedByTheRepository(ReportContent content)
	{
		return RaisedByTheRepository(content, ConfidenceLevel.Low);
	}

	private static ReportContent RaisedByTheRepository(ReportContent content, ConfidenceLevel previousLevel)
	{
		return content with
		{
			Configuration = content.Configuration with
			{
				Provenance = content.Configuration.Provenance with
				{
					MinimumConfidenceLevelOrigin = ConfigurationSource.RepositoryFile,
					RepositoryEffect = new RepositoryConfigurationEffect
					{
						ThresholdChange = new ConfidenceLevelChange { Previous = previousLevel, Current = ConfidenceLevel.High }
					}
				}
			}
		};
	}

	private static LanguageInventoryRow RowOf(string languageName, int fileCount)
	{
		return new LanguageInventoryRow
		{
			LanguageName = languageName,
			FileCount = fileCount,
			LineCount = fileCount,
			CandidateFileCount = 0,
			ShareOfFilesPercentage = 0.0
		};
	}

	private static ReportContent EmptyContentWithRows(EffectiveConfiguration configuration, params LanguageInventoryRow[] rows)
	{
		return ReportHarness.EmptyContent(ReportHarness.CatalogWithCSharpOnly(), configuration) with
		{
			ScanSummary = new ScanSummary
			{
				FileCount = rows.Sum(row => row.FileCount),
				LineCount = rows.Sum(row => row.LineCount),
				CandidateFileCount = 0,
				Rows = [.. rows]
			}
		};
	}

	private static string SummaryOf(ReportContent content)
	{
		return SectionBetween(Render(content), MarkdownReportRenderer.FINDINGS_SUMMARY_HEADING, MarkdownReportRenderer.COVERAGE_HEADING);
	}

	private static string SectionBetween(string report, string startHeading, string endHeading)
	{
		int start = report.IndexOf(startHeading, StringComparison.Ordinal);
		int end = report.IndexOf(endHeading, start, StringComparison.Ordinal);

		Assert.True(start >= 0 && end > start, $"The report has no section from '{startHeading}' to '{endHeading}'.");

		return report[start..end];
	}

	private static string[] SummaryRowsOf(string report)
	{
		string summary = SectionBetween(report, MarkdownReportRenderer.FINDINGS_SUMMARY_HEADING, MarkdownReportRenderer.COVERAGE_HEADING);

		return
		[
			.. summary
				.Split('\n')
				.Where(line => line.StartsWith(_SUMMARY_ROW_PREFIX, StringComparison.Ordinal) && char.IsAsciiDigit(line[_SUMMARY_ROW_PREFIX.Length]))
		];
	}

	/// <summary>The cells of a row, split on the bars a cell has not escaped.</summary>
	private static string[] CellsOf(string row)
	{
		List<string> cells = [];
		StringBuilder current = new();

		for (int position = 1; position < row.Length; position++)
		{
			char character = row[position];
			bool escaped = position > 0 && row[position - 1] == '\\';

			if (character == _TABLE_CELL_SEPARATOR && !escaped)
			{
				cells.Add(current.ToString().Trim());
				current.Clear();
			}
			else
			{
				current.Append(character);
			}
		}

		return [.. cells];
	}

	private static ArtifactDocument ReadArtifact(ReportContent content)
	{
		using MemoryStream stream = new();

		StructuredDataArtifactRenderer.Render(stream, content, new UnwiredGenericRecommendationProvider());

		ArtifactDocument? document = JsonSerializer.Deserialize(
			Encoding.UTF8.GetString(stream.ToArray()),
			ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);

		return document;
	}

	private static string Render(ReportContent content)
	{
		using StringWriter writer = new();

		MarkdownReportRenderer.Render(writer, content, new UnwiredGenericRecommendationProvider());

		return writer.ToString();
	}
}
