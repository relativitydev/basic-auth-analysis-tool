using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// THROUGH THE REAL EMBEDDED CATALOG - not <see cref="PipelineLanguageAnalyzer"/>, because
/// what is under test is whether the real `CSharp` analyzer's sixteen catalog templates
/// reach both real artifacts through the real four-stage pipeline, exactly as
/// <see cref="DisplayNameContractPipelineTests"/> proves for a Low finding's display name and
/// <see cref="ArtifactContractPipelineTests"/> proves for the synthetic analyzer's three routes.
///
/// One High finding (CS-001) exercises the template route; one Low finding (CS-019) exercises
/// the manual-review route. Both fixtures are synthetic, written from the catalog's own prose with
/// generic names.
/// </summary>
public sealed class CSharpRecommendationRoutePipelineTests
{
	private const string _HIGH_FILE_NAME = "HighFinding.cs";
	private const string _LOW_FILE_NAME = "LowFinding.cs";
	private const string _HIGH_PATTERN_CLASS = "CS-001";
	private const string _LOW_PATTERN_CLASS = "CS-019";

	private const string _highContent =
		"class HighFindingFixture\n{\n\tvoid Build()\n\t{\n\t\tvar credentials = new UsernamePasswordCredentials(options.UserName, options.Password);\n\t}\n}\n";

	private const string _lowContent =
		"class LowFindingFixture\n{\n\tvoid Build()\n\t{\n\t\tvar sink = new SmtpClient(host, port);\n\t\tsink.Credentials = new NetworkCredential(settings.UserName, settings.Credentials.Secret);\n\t}\n}\n";

	/// <summary>
	/// THE TEMPLATE ROUTE, over the real CS-001 template, which is multi-paragraph and carries a
	/// fenced code block. All four fields reach the Markdown report as their own
	/// well-formed list item - every continuation line indented to the content column, verified
	/// structurally and by exact reconstruction rather than by a raw substring search, which a
	/// continuation line's own added indentation would break (no `{path}` or `{line}` token
	/// appears in any of the four fields, so substitution leaves them unchanged and the
	/// reconstructed value is compared to the template's own text with nothing else in play). The
	/// JSON artifact's `recommendation` carries the same four pieces joined the way
	/// <see cref="RecommendationRenderer.ComposeArtifactText"/> declares - a blank line between
	/// pieces, each piece's own internal paragraphs and fence intact - and CS-001's own source form
	/// is the fence-first-line case: its label is written alone, since the field's own text
	/// opens with a fence, the same shape the Markdown report already uses for it.
	/// </summary>
	[Fact]
	public void AHighCSharpFindingRendersViaTheTemplateRouteInBothArtifacts()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_HIGH_FILE_NAME, _highContent);

		harness.RunTheWholeWorkflow();

		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		RecommendationTemplate template = TemplateFor(_HIGH_PATTERN_CLASS);
		string[] reportLines = reportText.Split('\n');

		AssertFieldReconstructsExactly(reportLines, RecommendationRenderer.SOURCE_FORM_LABEL, template.GenericSourceForm);
		AssertFieldReconstructsExactly(reportLines, RecommendationRenderer.TARGET_FORM_LABEL, template.TargetFormUnderTokenAuthentication);
		AssertFieldReconstructsExactly(reportLines, RecommendationRenderer.CREDENTIALS_LABEL, template.CredentialProvenance);
		AssertFieldReconstructsExactly(reportLines, RecommendationRenderer.VERIFICATION_LABEL, template.VerificationNote);

		ArtifactFindingDocument finding = FindByPatternClass(artifactText, _HIGH_PATTERN_CLASS);
		string expectedRecommendation = ComposeExpectedTemplateArtifactText(template);

		Assert.Equal(expectedRecommendation, finding.Recommendation);
	}

	/// <summary>
	/// THE MANUAL-REVIEW ROUTE: CS-019 is base Low, so `recommendation` is null in the artifact
	/// (the one expression of the manual-review signal) and the Markdown report carries
	/// <see cref="RecommendationRenderer.MANUAL_REVIEW_TEXT"/> rather than any catalog
	/// template's text - CS-019 has none, by design.
	/// </summary>
	[Fact]
	public void ALowCSharpFindingRendersViaManualReviewInBothArtifacts()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_LOW_FILE_NAME, _lowContent);

		harness.RunTheWholeWorkflow();

		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		Assert.Contains(RecommendationRenderer.MANUAL_REVIEW_TEXT, reportText, StringComparison.Ordinal);

		ArtifactFindingDocument finding = FindByPatternClass(artifactText, _LOW_PATTERN_CLASS);

		Assert.Null(finding.Recommendation);
	}

	/// <summary>
	/// The fence-first-line trap, proved against the real CS-001 template rather than a
	/// synthetic one: its source form's own first physical line opens a code fence, so
	/// <c>"- Source form: ```csharp"</c> is never written - that would not be a fenced block under
	/// CommonMark, since the marker would follow inline text on the bullet's own paragraph. The
	/// label is written alone on the bullet, and the fence itself becomes the item's first
	/// continuation line, indented to the content column exactly like every other line the field
	/// carries.
	/// </summary>
	[Fact]
	public void ASourceFormThatOpensWithAFenceWritesTheLabelAloneAndKeepsTheFenceInsideTheItem()
	{
		using PipelineHarness harness = PipelineHarness.CreateOverTheEmbeddedCatalog();
		harness.WriteSourceFile(_HIGH_FILE_NAME, _highContent);

		harness.RunTheWholeWorkflow();

		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		string[] lines = reportText.Split('\n');

		int bulletLineIndex = Array.FindIndex(
			lines,
			line => line == $"- {RecommendationRenderer.SOURCE_FORM_LABEL}:");

		Assert.True(bulletLineIndex >= 0);
		Assert.Equal("  ```csharp", lines[bulletLineIndex + 1]);

		(string value, _) = RecommendationListItemAssertions.ReadLabeledListItem(
			lines,
			bulletLineIndex,
			RecommendationRenderer.SOURCE_FORM_LABEL);

		RecommendationTemplate template = TemplateFor(_HIGH_PATTERN_CLASS);

		Assert.Equal(template.GenericSourceForm, value);
	}

	/// <summary>
	/// One field's list item, read back with <see cref="RecommendationListItemAssertions"/> - which
	/// asserts every continuation line's indentation and fence-column placement on the way - and
	/// compared to the template's own text, proving the field's content and its internal paragraph
	/// and fence breaks survived rendering unchanged.
	/// </summary>
	private static void AssertFieldReconstructsExactly(string[] reportLines, string label, string expectedValue)
	{
		int bulletLineIndex = FindLabeledBulletLineIndex(reportLines, label);

		(string reconstructedValue, _) = RecommendationListItemAssertions.ReadLabeledListItem(reportLines, bulletLineIndex, label);

		Assert.Equal(expectedValue, reconstructedValue);
	}

	private static int FindLabeledBulletLineIndex(string[] lines, string label)
	{
		string labelOnlyLine = $"- {label}:";
		string labeledRowPrefix = $"- {label}: ";

		int index = Array.FindIndex(
			lines,
			line => line == labelOnlyLine || line.StartsWith(labeledRowPrefix, StringComparison.Ordinal));

		Assert.True(index >= 0, $"No list item found for label '{label}'.");

		return index;
	}

	private static RecommendationTemplate TemplateFor(string patternClassIdentifier)
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		ILanguageAnalyzer analyzer = catalog.AnalyzersByLanguageName["CSharp"];

		return analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, CredentialFlow.ClientCredentials)!;
	}

	private static ArtifactFindingDocument FindByPatternClass(string artifactText, string patternClassIdentifier)
	{
		ArtifactDocument? document = JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument);

		return document!.Findings.Single(finding => finding.PatternClass == patternClassIdentifier);
	}

	/// <summary>
	/// Matches <see cref="RecommendationRenderer.ComposeArtifactText"/>'s template branch: the four
	/// fields, each preceded by its own declared label, joined on
	/// <see cref="RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR"/>. CS-001's own source
	/// form opens with a fence, so the label is written alone on a line, with the value - fence
	/// included - starting the next one; the other three fields do not, and keep the label and the
	/// value on one line.
	/// </summary>
	private static string ComposeExpectedTemplateArtifactText(RecommendationTemplate template)
	{
		return string.Join(
			RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR,
			$"{RecommendationRenderer.SOURCE_FORM_LABEL}:\n{template.GenericSourceForm}",
			$"{RecommendationRenderer.TARGET_FORM_LABEL}: {template.TargetFormUnderTokenAuthentication}",
			$"{RecommendationRenderer.CREDENTIALS_LABEL}: {template.CredentialProvenance}",
			$"{RecommendationRenderer.VERIFICATION_LABEL}: {template.VerificationNote}",
			$"{RecommendationComposer.OAUTH2_CLIENT_LABEL}: {OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials)}");
	}
}
