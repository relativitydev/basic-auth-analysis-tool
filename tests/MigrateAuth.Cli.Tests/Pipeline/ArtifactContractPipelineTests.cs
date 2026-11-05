using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THE JSON FINDINGS ARTIFACT AGAINST THE ARTIFACT CONTRACT, THROUGH THE REAL PIPELINE.
///
/// Every other artifact test builds an <see cref="ArtifactFindingDocument"/> from hand-placed
/// findings - the right fixture for rendering, and the wrong one for a contract test, because it
/// never proves the field the real <c>RecognitionEngine</c> assigns is the field this artifact
/// carries. This class runs the four real stages over <see cref="PipelineLanguageAnalyzer"/>, whose
/// three classes reach the three recommendation routes, and grades the written artifact
/// field by field against the artifact contract and its declared extensions.
/// </summary>
public sealed class ArtifactContractPipelineTests
{
	private const string _SOURCE_FILE_NAME = "ArtifactContract.cs";
	private const string _LITERAL_CREDENTIAL_TEXT = "hardcoded-secret";
	private const string _LIST_ITEM_CONTENT_INDENT = "  ";

	private static readonly string _sourceContent = string.Join(
		'\n',
		"class ArtifactContractFixture",
		"{",
		$"\tvoid Templated() => {PipelineLanguageAnalyzer.TEMPLATED_CONSTRUCT}(user, credential);",
		$"\tvoid Untemplated() => {PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();",
		$"\tvoid Low() => {PipelineLanguageAnalyzer.LOW_CONSTRUCT}(\"{_LITERAL_CREDENTIAL_TEXT}\");",
		"}",
		string.Empty);

	private static readonly Regex _threeDigitIdentifier = new(@"^\d{3}$", RegexOptions.CultureInvariant);

	private static readonly ImmutableHashSet<string> _allowedConfidenceNames =
	[
		ConfidenceLevels.Name(ConfidenceLevel.High),
		ConfidenceLevels.Name(ConfidenceLevel.Medium),
		ConfidenceLevels.Name(ConfidenceLevel.Low)
	];

	[Fact]
	public void EveryFindingSatisfiesTheSectionThreeOneContractFieldByField()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_SOURCE_FILE_NAME, _sourceContent);

		harness.RunTheWholeWorkflow();

		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);
		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);

		using JsonDocument raw = JsonDocument.Parse(artifactText);
		ArtifactDocument? document = JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);
		Assert.Equal(3, document.Findings.Length);

		AssertEnvelopeKeysArePresent(raw.RootElement);

		foreach (ArtifactFindingDocument finding in document.Findings)
		{
			AssertContractShape(finding);
		}

		AssertOldNamesAreAbsent(raw.RootElement);

		ArtifactFindingDocument templated = FindByPatternClass(document.Findings, PipelineLanguageAnalyzer.TEMPLATED_PATTERN_CLASS);
		ArtifactFindingDocument untemplated = FindByPatternClass(document.Findings, PipelineLanguageAnalyzer.UNTEMPLATED_PATTERN_CLASS);
		ArtifactFindingDocument low = FindByPatternClass(document.Findings, PipelineLanguageAnalyzer.LOW_PATTERN_CLASS);

		AssertHighRecommendationMatchesTheReport(templated, reportText);
		AssertHighRecommendationMatchesTheReport(untemplated, reportText);
		AssertLowFindingCarriesNoRecommendationAndASecurityNote(low);
	}

	/// <summary>The contract's seven fields, and the extension fields beside them.</summary>
	private static void AssertContractShape(ArtifactFindingDocument finding)
	{
		Assert.Matches(_threeDigitIdentifier, finding.Id);
		Assert.DoesNotContain('\\', finding.FilePath);
		Assert.False(finding.FilePath.StartsWith('/'));
		Assert.True(finding.Line >= FindingIdentifiers.FIRST_INDEX);
		Assert.Equal(AssessedLanguages.CanonicalName(AssessedLanguage.CSharp), finding.Language);
		Assert.False(string.IsNullOrEmpty(finding.PatternClass));
		Assert.Contains(finding.Confidence, _allowedConfidenceNames);

		bool isLow = finding.Confidence == ConfidenceLevels.Name(ConfidenceLevel.Low);

		Assert.Equal(isLow, finding.Recommendation is null);
	}

	private static void AssertEnvelopeKeysArePresent(JsonElement root)
	{
		Assert.True(root.TryGetProperty(ArtifactHandlingNotice.STRUCTURED_DATA_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.COVERAGE_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.SCAN_SUMMARY_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.SKIPPED_ITEMS_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.FINDINGS_CAP_FIELD_NAME, out _));
		Assert.True(root.TryGetProperty(StructuredDataArtifactRenderer.PROVENANCE_FIELD_NAME, out _));

		JsonElement finding = root.GetProperty(StructuredDataArtifactRenderer.FINDINGS_FIELD_NAME)[0];

		Assert.True(finding.TryGetProperty("sequentialIndex", out _));
		Assert.True(finding.TryGetProperty("destinationCertainty", out _));
		Assert.True(finding.TryGetProperty("literalCredentialSignal", out _));
		Assert.True(finding.TryGetProperty("fingerprint", out _));
		Assert.True(finding.TryGetProperty("securityNote", out _));
	}

	/// <summary>Three names that are not part of the contract. None may appear on a finding.</summary>
	private static void AssertOldNamesAreAbsent(JsonElement root)
	{
		JsonElement finding = root.GetProperty(StructuredDataArtifactRenderer.FINDINGS_FIELD_NAME)[0];

		Assert.False(finding.TryGetProperty("identifier", out _));
		Assert.False(finding.TryGetProperty("path", out _));
		Assert.False(finding.TryGetProperty("confidenceLevel", out _));
	}

	private static ArtifactFindingDocument FindByPatternClass(
		ArtifactFindingDocument[] findings,
		string patternClassIdentifier)
	{
		return findings.Single(finding => finding.PatternClass == patternClassIdentifier);
	}

	/// <summary>
	/// The contract's `recommendation` for a High finding must equal the text
	/// <see cref="MarkdownReportRenderer"/> rendered for the same finding, which is what the
	/// shared-source requirement is for. The Markdown bullet rows are read back rather than
	/// re-derived from catalog literals, so this proves the two never drifted rather than proving
	/// two copies of one literal agree with each other.
	/// </summary>
	private static void AssertHighRecommendationMatchesTheReport(ArtifactFindingDocument finding, string reportText)
	{
		string expected = string.Join(
			RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR,
			ReadRecommendationLines(reportText, finding.Id));

		Assert.NotNull(finding.Recommendation);
		Assert.Equal(expected, finding.Recommendation);
	}

	private static void AssertLowFindingCarriesNoRecommendationAndASecurityNote(ArtifactFindingDocument low)
	{
		Assert.Null(low.Recommendation);
		Assert.Equal(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, low.SecurityNote);
	}

	/// <summary>
	/// The recommendation list items under one finding's heading, each as the text the artifact joins:
	/// the item's own row with the leading `"- "` stripped, followed by its continuation lines with the
	/// list-item indent stripped (the OAuth2 Client block is a piece whose value spans several lines).
	/// The flow and its note are list items beside the finding and outside the recommendation text, so
	/// they are skipped here.
	/// </summary>
	private static ImmutableArray<string> ReadRecommendationLines(string reportText, string findingId)
	{
		string headingPrefix = $"#### {findingId} -";
		string[] lines = reportText.Split('\n');
		int headingIndex = Array.FindIndex(lines, line => line.StartsWith(headingPrefix, StringComparison.Ordinal));

		Assert.True(headingIndex >= 0);

		List<string> recommendationItems = [];
		int cursor = headingIndex + 2;

		while (cursor < lines.Length && lines[cursor].Length > 0)
		{
			if (lines[cursor].StartsWith(_LIST_ITEM_CONTENT_INDENT, StringComparison.Ordinal))
			{
				recommendationItems[^1] += "\n" + lines[cursor][_LIST_ITEM_CONTENT_INDENT.Length..];
			}
			else
			{
				Assert.StartsWith("- ", lines[cursor], StringComparison.Ordinal);

				if (!IsFlowItem(lines[cursor]))
				{
					recommendationItems.Add(lines[cursor][2..]);
				}
			}

			cursor++;
		}

		return [.. recommendationItems];
	}

	private static bool IsFlowItem(string line)
	{
		return line.StartsWith("- Flow: ", StringComparison.Ordinal) || line.StartsWith("- Flow note: ", StringComparison.Ordinal);
	}
}
