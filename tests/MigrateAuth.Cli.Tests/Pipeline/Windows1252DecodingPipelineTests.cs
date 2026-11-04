using System.Text;
using System.Text.Json;

using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// A FILE IN A LEGACY SINGLE-BYTE CODE PAGE, THROUGH THE REAL PIPELINE: it is read as Windows-1252
/// rather than skipped, it produces its finding at a column counted over the decoded text, the
/// encoding it was read as is visible in the report and the data artifact, and the state file keeps
/// that across a resume - while a UTF-8 file, a markless UTF-16 file and a binary file are decoded or
/// skipped by the decode's other rules.
/// </summary>
public sealed class Windows1252DecodingPipelineTests
{
	private const string _LEGACY_FILE = "src/Legacy.cs";
	private const string _UTF8_FILE = "src/Utf8.cs";
	private const string _UTF16_FILE = "src/Utf16.cs";
	private const string _BINARY_FILE = "src/Binary.cs";
	private const int _WINDOWS_1252_CODE_PAGE = 1252;
	private const string _ENCODING_NAME = "Windows-1252";
	private const string _REPORT_LINE_PREFIX = "Files read as Windows-1252, a legacy code page: ";
	private const int _FIRST_COLUMN = 1;
	private const byte _ELF_MAGIC_FIRST_BYTE = 0x7F;
	private const byte _E_ACUTE_IN_WINDOWS_1252 = 0xE9;
	private const byte _NULL_BYTE = 0x00;

	private static readonly string _line =
		$"\tstring author = \"José Peña\"; {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, credential);";

	private static readonly string _content = $"class A\n{{\n{_line}\n}}\n";

	[Fact]
	public void AWindows1252FileProducesAFindingAtAColumnCountedOverTheDecodedText()
	{
		using PipelineHarness harness = RunTheWorkflowOverTheMixedTree();

		ArtifactDocument artifact = ReadArtifact(harness);
		ArtifactFindingDocument legacy = artifact.Findings.Single(finding => finding.FilePath == _LEGACY_FILE);
		ArtifactFindingDocument utf8 = artifact.Findings.Single(finding => finding.FilePath == _UTF8_FILE);

		Assert.Equal(_line.IndexOf(PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT, StringComparison.Ordinal) + _FIRST_COLUMN, legacy.Column);
		Assert.Equal(utf8.Column, legacy.Column);
		Assert.Equal(utf8.Line, legacy.Line);
	}

	[Fact]
	public void TheOtherEncodingsAreReadAsBeforeAndTheBinaryFileIsStillSkipped()
	{
		using PipelineHarness harness = RunTheWorkflowOverTheMixedTree();

		ArtifactDocument artifact = ReadArtifact(harness);

		Assert.Equal(
			[_LEGACY_FILE, _UTF16_FILE, _UTF8_FILE],
			artifact.Findings.Select(finding => finding.FilePath).Order(StringComparer.Ordinal));
		ArtifactSkippedItemDocument skipped = Assert.Single(artifact.SkippedItems, item => item.Count > 0);

		Assert.Equal(ReportVocabulary.NameOf(SkippedItemCategory.Undecodable), skipped.Category);
		Assert.Equal(1, skipped.Count);
		Assert.Equal([_BINARY_FILE], skipped.Paths);
	}

	[Fact]
	public void TheEncodingIsVisibleInTheReportAndTheDataArtifactAndTheFileIsNotASkippedItem()
	{
		using PipelineHarness harness = RunTheWorkflowOverTheMixedTree();

		string report = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		ArtifactLegacyEncodedFilesDocument legacyEncoded = ReadArtifact(harness).ScanSummary.LegacyEncodedFiles;

		Assert.Equal(_ENCODING_NAME, legacyEncoded.Encoding);
		Assert.Equal(1, legacyEncoded.Count);
		Assert.Equal([_LEGACY_FILE], legacyEncoded.Paths);
		Assert.Equal(0, legacyEncoded.OmittedPathCount);
		Assert.Contains(_REPORT_LINE_PREFIX + "1.", report, StringComparison.Ordinal);
		Assert.Contains("- " + _LEGACY_FILE, report, StringComparison.Ordinal);
		Assert.Contains("Skipped files: 1.", report, StringComparison.Ordinal);
	}

	[Fact]
	public void ARepositoryWithoutAWindows1252FileStatesZeroInTheReportAndTheArtifact()
	{
		using PipelineHarness harness = PipelineHarness.Create();

		harness.WriteSourceFile(_UTF8_FILE, _content);
		harness.RunTheWholeWorkflow();

		ArtifactLegacyEncodedFilesDocument legacyEncoded = ReadArtifact(harness).ScanSummary.LegacyEncodedFiles;

		Assert.Equal(0, legacyEncoded.Count);
		Assert.Empty(legacyEncoded.Paths);
		Assert.Contains(
			_REPORT_LINE_PREFIX + "0.",
			harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport),
			StringComparison.Ordinal);
	}

	[Fact]
	public void TheWindows1252FilesSurviveAResume()
	{
		using PipelineHarness harness = RunTheWorkflowOverTheMixedTree();

		SessionStateLoad reloaded = harness.ReloadState();

		Assert.True(reloaded.WasResumed);
		Assert.Equal(1, reloaded.State.ScanOutcome.LegacyEncodedFiles.OccurrenceCount);
		Assert.Equal(
			[_LEGACY_FILE],
			reloaded.State.ScanOutcome.LegacyEncodedFiles.Paths.Select(path => path.Value));
	}

	private static PipelineHarness RunTheWorkflowOverTheMixedTree()
	{
		PipelineHarness harness = PipelineHarness.Create();
		Encoding windows1252 = CodePagesEncodingProvider.Instance.GetEncoding(_WINDOWS_1252_CODE_PAGE)!;

		harness.WriteSourceBytes(_LEGACY_FILE, windows1252.GetBytes(_content));
		harness.WriteSourceFile(_UTF8_FILE, _content);
		harness.WriteSourceBytes(_UTF16_FILE, new UnicodeEncoding(bigEndian: false, byteOrderMark: false).GetBytes(_content));
		harness.WriteSourceBytes(
			_BINARY_FILE,
			[_ELF_MAGIC_FIRST_BYTE, _E_ACUTE_IN_WINDOWS_1252, _NULL_BYTE, _E_ACUTE_IN_WINDOWS_1252, _NULL_BYTE, _NULL_BYTE]);
		harness.RunTheWholeWorkflow();

		return harness;
	}

	private static ArtifactDocument ReadArtifact(PipelineHarness harness)
	{
		ArtifactDocument? document = JsonSerializer.Deserialize(
			harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact),
			ReportJsonContext.Default.ArtifactDocument);

		return document ?? throw new InvalidOperationException("The data artifact did not deserialize.");
	}
}
