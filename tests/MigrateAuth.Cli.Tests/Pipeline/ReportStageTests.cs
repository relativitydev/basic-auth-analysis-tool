using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Pipeline;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE COMPOSITION, NOT THE RENDERING.
///
/// The renderer tests verify what the two renderers produce, exhaustively, over synthetic content. What
/// these tests check is that the stage assembles the RIGHT content from a real run - which is a
/// different claim and the only one these tests are responsible for. So these tests assert that each of
/// the report's five sections is fed from the component that owns it, and deliberately do not re-assert
/// the shape of the sections themselves.
/// </summary>
public sealed class ReportStageTests
{
	private const string _CREDENTIAL_FILE_NAME = "Credentials.cs";
	private const string _LOW_FILE_NAME = "Legacy.cs";
	private const string _ORDINARY_FILE_NAME = "Ordinary.cs";
	private const string _ORDINARY_CONTENT = "public sealed class Ordinary\n{\n}\n";
	private const string _STAGE_NAME_SEPARATOR = ": ";
	private const int _ONE_FINDING = 1;
	private const string _RELATIVE_DESTINATION_TEXT = "./Outputs";

	private static readonly string _credentialContent =
		$"class Credentials\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, secret);\n}}\n";

	private static readonly string _lowContent =
		$"class Legacy\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}();\n}}\n";

	[Fact]
	public void BothArtifactsAreWritten()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		Assert.Equal(2, harness.WrittenArtifactNames().Length);
		Assert.Contains(OutputArtifactNaming.MARKDOWN_FILE_EXTENSION, harness.Transcript, StringComparison.Ordinal);
		Assert.Contains(OutputArtifactNaming.STRUCTURED_DATA_FILE_EXTENSION, harness.Transcript, StringComparison.Ordinal);
	}

	/// <summary>
	/// Each "Written" line names the output folder as well as the file, so the user can find it.
	/// The folder is named as the user supplied it (here an absolute path, so the same text).
	/// </summary>
	[Fact]
	public void EachWrittenLineNamesTheFileAndTheOutputFolder()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		foreach (string fileName in harness.WrittenArtifactNames())
		{
			Assert.Contains(
				string.Format(
					System.Globalization.CultureInfo.InvariantCulture,
					ReportStage.ARTIFACT_WRITTEN_MESSAGE_FORMAT,
					fileName,
					harness.Destination.ConsoleDisplayText),
				harness.Transcript,
				StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// A relative or default destination is shown relative to the working folder, and the
	/// canonical resolved path is never printed - the console transcript carries only the text the user supplied.
	/// </summary>
	[Fact]
	public void TheWrittenLineEchoesTheSuppliedRelativeFolderAndNeverTheCanonicalPath()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithDestinationShownAs(_RELATIVE_DESTINATION_TEXT);
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		foreach (string fileName in harness.WrittenArtifactNames())
		{
			Assert.Contains(
				$"Written: {fileName} (in the output folder {_RELATIVE_DESTINATION_TEXT}).",
				harness.Transcript,
				StringComparison.Ordinal);
		}

		Assert.DoesNotContain(harness.Destination.CanonicalPath, harness.Transcript, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The report and the artifact stay label-only: neither names the output folder, in any form.</summary>
	[Fact]
	public void NeitherArtifactNamesTheOutputFolder()
	{
		using PipelineHarness harness = PipelineHarness.CreateWithDestinationShownAs(_RELATIVE_DESTINATION_TEXT);
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		string artifact = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		Assert.DoesNotContain(harness.Destination.CanonicalPath, report, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(harness.Destination.CanonicalPath, artifact, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(_RELATIVE_DESTINATION_TEXT, report, StringComparison.Ordinal);
		Assert.DoesNotContain(_RELATIVE_DESTINATION_TEXT, artifact, StringComparison.Ordinal);
	}

	/// <summary>
	/// SECTION 2 COMES FROM THE SCAN, ACROSS A COMMAND BOUNDARY. A report rendered from
	/// <c>ScanSummary.Empty</c> would read exactly like a repository that really is empty, so the
	/// assertion is that the report carries the scan's real numbers.
	/// </summary>
	[Fact]
	public void SectionTwoCarriesTheNumbersTheScanProduced()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		harness.WriteSourceFile(_ORDINARY_FILE_NAME, _ORDINARY_CONTENT);

		harness.RunTheWholeWorkflow();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Contains("Files enumerated: 2.", report, StringComparison.Ordinal);
		Assert.Contains("Candidate files: 1.", report, StringComparison.Ordinal);
		Assert.Equal(2, harness.State.ScanOutcome.FileCount);
		Assert.Equal(1, harness.State.ScanOutcome.CandidateFileCount);
	}

	/// <summary>
	/// SECTION 3 COMES FROM THE LEDGER, which has been accumulating since the walk. It survives
	/// across commands because the session owns it, and this is the check that the report reads the
	/// same instance rather than a fresh one.
	/// </summary>
	[Fact]
	public void SectionThreeCarriesTheHolesTheRunAccumulated()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.DeleteSourceFile(_CREDENTIAL_FILE_NAME);
		harness.RunAndCommit(CommandId.Analyze);
		harness.RunAndCommit(CommandId.Suggest);
		harness.RunAndCommit(CommandId.Report);

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Contains(
			ReportVocabulary.DescriptionOf(SkippedItemCategory.MissingCandidate),
			report,
			StringComparison.Ordinal);
		Assert.Contains("Skipped files: 1.", report, StringComparison.Ordinal);
	}

	/// <summary>SECTION 4 COMES FROM THE SESSION STATE, which is where the data invariant put the findings.</summary>
	[Fact]
	public void SectionFourCarriesTheFindingsTheAnalysisRecorded()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		Assert.Contains(PipelineLanguageAnalyzer.TEMPLATED_PATTERN_CLASS, report, StringComparison.Ordinal);
		Assert.Contains(_CREDENTIAL_FILE_NAME, report, StringComparison.Ordinal);
		Assert.Contains("Findings reported: 1 of 1", report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The recommendation routing survives the round trip: a class the catalog templates renders its template's
	/// own words in the report. The rendering itself is covered by the renderer tests; what is
	/// checked here is that the stage handed the renderer the catalog those templates live in.
	/// </summary>
	[Fact]
	public void ATemplatedFindingCarriesItsTemplateIntoTheReport()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		Assert.Contains(
			"a bearer token obtained from the identity endpoint",
			harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// THE CONFIDENCE THRESHOLD, APPLIED AT RENDER TIME AND NOT BEFORE. The full set is persisted and the
	/// filtered set is reported, so the state still holds the Low finding the report omits - which
	/// is what keeps a finding's identifier independent of the reader's configuration.
	/// </summary>
	[Fact]
	public void TheThresholdFiltersTheReportAndNotTheRecordedSet()
	{
		using PipelineHarness harness = PipelineHarness.Create(ConfidenceLevel.High);
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);
		harness.WriteSourceFile(_LOW_FILE_NAME, _lowContent);

		harness.RunTheWholeWorkflow();

		Assert.Equal(2, harness.State.Findings.Length);
		Assert.Contains(
			"Findings reported: 1 of 2",
			harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The data invariant at the artifact boundary: the repository travels as a label and the
	/// canonical path appears nowhere in either file. The root is the one path with no relative
	/// form, so it is the one this check has to make.
	/// </summary>
	[Fact]
	public void NeitherArtifactCarriesTheRepositoryPath()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunTheWholeWorkflow();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		string artifact = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		Assert.DoesNotContain(harness.Repository.CanonicalPath, report, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(harness.Repository.CanonicalPath, artifact, StringComparison.OrdinalIgnoreCase);
		Assert.Contains(harness.Repository.Label, report, StringComparison.Ordinal);
	}

	/// <summary>
	/// The data invariant again, at the sink that matters most: no text from an analyzed file reaches an
	/// artifact. The fixture places a distinctive literal credential in the source, and neither
	/// output may contain it - a template renders the GENERIC form of the class, never what was
	/// found.
	/// </summary>
	[Fact]
	public void NeitherArtifactCarriesTextFromTheAnalyzedFile()
	{
		const string placedSecret = "the-placed-literal-secret";

		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(
			_CREDENTIAL_FILE_NAME,
			$"class Credentials\n{{\n\tvoid Build() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, \"{placedSecret}\");\n}}\n");

		harness.RunTheWholeWorkflow();

		Assert.DoesNotContain(
			placedSecret,
			harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport),
			StringComparison.Ordinal);
		Assert.DoesNotContain(
			placedSecret,
			harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The table belongs to `analyze`, the stage that displays it. This is the negative half -
	/// `report`'s OWN console output, isolated from
	/// what `scan`, `analyze` and `suggest` already printed before it ran, carries neither the
	/// table's header nor a pattern class identifier.
	/// </summary>
	[Fact]
	public void ReportNoLongerPrintsTheFindingsTableOnItsOwnConsoleSurface()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		harness.RunAndCommit(CommandId.Suggest);

		int transcriptLengthBeforeReport = harness.Transcript.Length;

		harness.RunAndCommit(CommandId.Report);

		string reportOnlyOutput = harness.Transcript[transcriptLengthBeforeReport..];

		Assert.DoesNotContain(ConsoleFindingsTable.ColumnHeadings[1], reportOnlyOutput, StringComparison.Ordinal);
		Assert.DoesNotContain(PipelineLanguageAnalyzer.TEMPLATED_PATTERN_CLASS, reportOnlyOutput, StringComparison.Ordinal);
	}

	/// <summary>
	/// `report`'s first console line opens with its stage name, as the first line of `scan`,
	/// `analyze` and `suggest` does, and it is the line that names the reported set.
	/// </summary>
	[Fact]
	public void TheFirstLineOfTheReportStageOpensWithTheStageName()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		harness.RunAndCommit(CommandId.Suggest);

		int transcriptLengthBeforeReport = harness.Transcript.Length;

		harness.RunAndCommit(CommandId.Report);

		string firstReportLine = FirstLineOf(harness.Transcript[transcriptLengthBeforeReport..]);
		string expectedLine = string.Format(
			CultureInfo.InvariantCulture,
			ReportStage.REPORTING_MESSAGE_FORMAT,
			_ONE_FINDING,
			_ONE_FINDING,
			ConfidenceLevels.Name(ConfidenceLevels.WIDEST));

		Assert.StartsWith(
			CommandRegistry.Describe(CommandId.Report).Name + _STAGE_NAME_SEPARATOR,
			firstReportLine,
			StringComparison.Ordinal);
		Assert.Equal(expectedLine, firstReportLine);
	}

	/// <summary>An interrupted stage writes nothing and does not complete.</summary>
	[Fact]
	public void ACancelledReportWritesNothing()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_CREDENTIAL_FILE_NAME, _credentialContent);

		harness.RunAndCommit(CommandId.Scan);
		harness.RunAndCommit(CommandId.Analyze);
		harness.RunAndCommit(CommandId.Suggest);

		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();

		Assert.Equal(StageRunOutcome.Cancelled, harness.Run(CommandId.Report, cancellation.Token));
		Assert.Empty(harness.WrittenArtifactNames());
	}

	private static string FirstLineOf(string output)
	{
		return output.ReplaceLineEndings().Split(Environment.NewLine)[0];
	}
}
