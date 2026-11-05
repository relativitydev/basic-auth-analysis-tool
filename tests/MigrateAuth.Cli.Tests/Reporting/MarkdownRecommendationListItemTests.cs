using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// Two things are pinned: a single-line field renders as one row, byte for byte, and a
/// multi-paragraph, fenced field renders with every continuation line - a blank
/// paragraph break, a fence marker, a fence's own body, a nested list - indented to the item's
/// content column rather than escaping to column 0.
///
/// THE SYNTHETIC TEMPLATE BELOW IS DELIBERATELY NOT ANY SHIPPED CATALOG TEXT: it exists only
/// to put every shape the report renderer has to handle - a fence-opening first line with a
/// tab-indented body line, a mid-field fence between two ordinary paragraphs, and a nested bullet
/// list - inside one rendering pass, which no single real `CSharp` template happens to combine.
/// </summary>
public sealed class MarkdownRecommendationListItemTests
{
	private const string _PATTERN_CLASS = "TST-901";

	/// <summary>The trap: the field's own first physical line opens a fence, and its body carries a tab.</summary>
	private const string _SOURCE_FORM =
		"```csharp\n\tvar credential = ExistingCredential();\nUseIt(credential);\n```";

	/// <summary>Three paragraphs, with a fenced block as the middle one.</summary>
	private const string _TARGET_FORM =
		"First paragraph, before any fence.\n\n```csharp\nvar token = AccessToken();\n```\n\nFinal paragraph, after the fence.";

	/// <summary>A single-line field, rendered alongside the multi-line ones in the same pass.</summary>
	private const string _CREDENTIAL_PROVENANCE = "A single-line credential provenance statement.";

	/// <summary>A nested Markdown list, on `CS-008`'s own real shape (restated generically here).</summary>
	private const string _VERIFICATION_NOTE =
		"Identify the consumer first.\n\n- first nested option\n- second nested option";

	[Fact]
	public void EveryContinuationLineOfAMultiParagraphFencedFieldIsIndentedToTheContentColumn()
	{
		string[] lines = RenderReportWithSyntheticTemplate();

		AssertSourceFormKeepsTheFenceAndTheTabIntact(lines);
		AssertTargetFormKeepsBothParagraphBreaksAndTheMidFieldFence(lines);
		AssertASingleLineFieldAmongMultiLineOnesIsUnaffected(lines);
		AssertVerificationNoteKeepsItsNestedList(lines);
	}

	/// <summary>
	/// The fence-first-line rule: the label alone on the bullet, the fence as the first
	/// continuation line, the tab-indented body line underneath it with only the content-column
	/// indent added ahead of the tab it already carries.
	/// </summary>
	private static void AssertSourceFormKeepsTheFenceAndTheTabIntact(string[] lines)
	{
		int bulletLineIndex = FindLabeledBulletLineIndex(lines, RecommendationRenderer.SOURCE_FORM_LABEL);

		Assert.Equal($"- {RecommendationRenderer.SOURCE_FORM_LABEL}:", lines[bulletLineIndex]);
		Assert.Equal("  ```csharp", lines[bulletLineIndex + 1]);
		Assert.Equal("  \tvar credential = ExistingCredential();", lines[bulletLineIndex + 2]);
		Assert.Equal("  UseIt(credential);", lines[bulletLineIndex + 3]);
		Assert.Equal("  ```", lines[bulletLineIndex + 4]);

		(string value, _) = RecommendationListItemAssertions.ReadLabeledListItem(
			lines,
			bulletLineIndex,
			RecommendationRenderer.SOURCE_FORM_LABEL);

		Assert.Equal(_SOURCE_FORM, value);
	}

	/// <summary>
	/// The ordinary case: the label shares its bullet with the first paragraph's own first line.
	/// The paragraph break before and after the mid-field fence is a continuation line holding
	/// nothing but the content-column indent - the chosen way to keep a blank line part of the
	/// same list item under CommonMark's own definition of blank (spaces alone still count).
	/// </summary>
	private static void AssertTargetFormKeepsBothParagraphBreaksAndTheMidFieldFence(string[] lines)
	{
		int bulletLineIndex = FindLabeledBulletLineIndex(lines, RecommendationRenderer.TARGET_FORM_LABEL);

		Assert.Equal($"- {RecommendationRenderer.TARGET_FORM_LABEL}: First paragraph, before any fence.", lines[bulletLineIndex]);
		Assert.Equal("  ", lines[bulletLineIndex + 1]);
		Assert.Equal("  ```csharp", lines[bulletLineIndex + 2]);
		Assert.Equal("  var token = AccessToken();", lines[bulletLineIndex + 3]);
		Assert.Equal("  ```", lines[bulletLineIndex + 4]);
		Assert.Equal("  ", lines[bulletLineIndex + 5]);
		Assert.Equal("  Final paragraph, after the fence.", lines[bulletLineIndex + 6]);

		(string value, _) = RecommendationListItemAssertions.ReadLabeledListItem(
			lines,
			bulletLineIndex,
			RecommendationRenderer.TARGET_FORM_LABEL);

		Assert.Equal(_TARGET_FORM, value);
	}

	/// <summary>A single-line field between two multi-line ones renders as one row.</summary>
	private static void AssertASingleLineFieldAmongMultiLineOnesIsUnaffected(string[] lines)
	{
		int bulletLineIndex = FindLabeledBulletLineIndex(lines, RecommendationRenderer.CREDENTIALS_LABEL);

		Assert.Equal($"- {RecommendationRenderer.CREDENTIALS_LABEL}: {_CREDENTIAL_PROVENANCE}", lines[bulletLineIndex]);
		Assert.False(lines[bulletLineIndex + 1].StartsWith(RecommendationListItemAssertions.ContentIndent, StringComparison.Ordinal));
	}

	/// <summary>A nested bullet list inside the field renders as a nested list under CommonMark, at the same content column.</summary>
	private static void AssertVerificationNoteKeepsItsNestedList(string[] lines)
	{
		int bulletLineIndex = FindLabeledBulletLineIndex(lines, RecommendationRenderer.VERIFICATION_LABEL);

		Assert.Equal($"- {RecommendationRenderer.VERIFICATION_LABEL}: Identify the consumer first.", lines[bulletLineIndex]);
		Assert.Equal("  ", lines[bulletLineIndex + 1]);
		Assert.Equal("  - first nested option", lines[bulletLineIndex + 2]);
		Assert.Equal("  - second nested option", lines[bulletLineIndex + 3]);

		(string value, _) = RecommendationListItemAssertions.ReadLabeledListItem(
			lines,
			bulletLineIndex,
			RecommendationRenderer.VERIFICATION_LABEL);

		Assert.Equal(_VERIFICATION_NOTE, value);
	}

	/// <summary>
	/// A single-line template - <see cref="ReportHarness.TEMPLATED_PATTERN_CLASS"/>'s
	/// four fields never carry an embedded newline - renders in Markdown as one `"- Label: value"` row
	/// per piece, byte for byte. In JSON, `recommendation`'s four pieces join on a blank line, which for
	/// single-line pieces means one empty line between each `"Label: value"` entry.
	/// </summary>
	[Fact]
	public void ASingleLineTemplateRendersUnchangedInMarkdownWhileJsonGainsTheBlankLineSeparator()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "source.cs", 9, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		ReportContent content = ReportHarness.ContentWith(
			ReportHarness.CatalogWithCSharpOnly(),
			ReportHarness.ConfigurationWith(ConfidenceLevel.Low),
			findings);

		string report = RenderMarkdown(content);

		Assert.Contains($"- {RecommendationRenderer.SOURCE_FORM_LABEL}: {ReportHarness.TEMPLATE_SOURCE_FORM}", report, StringComparison.Ordinal);
		Assert.Contains($"- {RecommendationRenderer.TARGET_FORM_LABEL}: {ReportHarness.TEMPLATE_TARGET_FORM}", report, StringComparison.Ordinal);
		Assert.Contains($"- {RecommendationRenderer.CREDENTIALS_LABEL}: {ReportHarness.TEMPLATE_CREDENTIAL_PROVENANCE}", report, StringComparison.Ordinal);
		Assert.Contains($"- {RecommendationRenderer.VERIFICATION_LABEL}: {ReportHarness.TEMPLATE_VERIFICATION_NOTE}", report, StringComparison.Ordinal);

		ArtifactFindingDocument finding = RenderAndReadArtifact(content);
		string expectedJsonRecommendation = string.Join(
			RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR,
			$"{RecommendationRenderer.SOURCE_FORM_LABEL}: {ReportHarness.TEMPLATE_SOURCE_FORM}",
			$"{RecommendationRenderer.TARGET_FORM_LABEL}: {ReportHarness.TEMPLATE_TARGET_FORM}",
			$"{RecommendationRenderer.CREDENTIALS_LABEL}: {ReportHarness.TEMPLATE_CREDENTIAL_PROVENANCE}",
			$"{RecommendationRenderer.VERIFICATION_LABEL}: {ReportHarness.TEMPLATE_VERIFICATION_NOTE}",
			$"{RecommendationComposer.OAUTH2_CLIENT_LABEL}: {OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials)}");

		Assert.Equal("\n\n", RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR);
		Assert.Equal(expectedJsonRecommendation, finding.Recommendation);
	}

	private static string RenderMarkdown(ReportContent content)
	{
		using StringWriter writer = new();

		MarkdownReportRenderer.Render(writer, content, new UnwiredGenericRecommendationProvider());

		return writer.ToString();
	}

	private static ArtifactFindingDocument RenderAndReadArtifact(ReportContent content)
	{
		using MemoryStream stream = new();

		StructuredDataArtifactRenderer.Render(stream, content, new UnwiredGenericRecommendationProvider());

		string artifactText = Encoding.UTF8.GetString(stream.ToArray());
		ArtifactDocument? document = JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);

		return document.Findings.Single();
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

	private static string[] RenderReportWithSyntheticTemplate()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(1, "source.cs", 7, _PATTERN_CLASS, ConfidenceLevel.High)
		];

		ReportContent content = ReportHarness.ContentWith(CatalogWithSyntheticTemplate(), ReportHarness.ConfigurationWith(ConfidenceLevel.Low), findings);

		using StringWriter writer = new();

		MarkdownReportRenderer.Render(writer, content, new UnwiredGenericRecommendationProvider());

		return writer.ToString().Split('\n');
	}

	private static LanguageCatalog CatalogWithSyntheticTemplate()
	{
		TestLanguageAnalyzer analyzer = new(
			AssessedLanguages.CanonicalName(AssessedLanguage.CSharp),
			".cs",
			[_PATTERN_CLASS],
			new Dictionary<string, RecommendationTemplate>(StringComparer.Ordinal)
			{
				[_PATTERN_CLASS] = new RecommendationTemplate
				{
					GenericSourceForm = _SOURCE_FORM,
					TargetFormUnderTokenAuthentication = _TARGET_FORM,
					CredentialProvenance = _CREDENTIAL_PROVENANCE,
					VerificationNote = _VERIFICATION_NOTE
				}
			});

		return new LanguageCatalog(LanguageCatalog.CurrentVersion, [analyzer]);
	}
}
