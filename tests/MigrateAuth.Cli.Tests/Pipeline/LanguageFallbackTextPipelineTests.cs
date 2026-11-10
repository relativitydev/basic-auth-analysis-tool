using System.Text.Json;

using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Pipeline;

/// <summary>
/// The generic recommendation route
/// reads the finding's own language's declared fallback text when the analyzer that owns it
/// supplies one, through the REAL renderer and the real four-stage pipeline - not a synthetic
/// <see cref="RenderedRecommendation"/>, because what is under test is whether the declared text
/// actually reaches both shareable artifacts rather than whether the renderer's private routing
/// logic can be made to return it.
///
/// <see cref="PipelineLanguageAnalyzer.UNTEMPLATED_PATTERN_CLASS"/> is exactly the third route:
/// a High-confidence class the fixture analyzer declares no template for. Declaring a language
/// fallback text on that same analyzer, and reading it back from both artifacts, is the whole of
/// what this class proves.
/// </summary>
public sealed class LanguageFallbackTextPipelineTests
{
	private const string _SOURCE_FILE_NAME = "LanguageFallback.cs";

	private static readonly string _sourceContent = string.Join(
		'\n',
		"class LanguageFallbackFixture",
		"{",
		$"\tvoid Untemplated() => {PipelineLanguageAnalyzer.UNTEMPLATED_CONSTRUCT}();",
		"}",
		string.Empty);

	/// <summary>
	/// The declared text renders in place of the shared engine's language-neutral one, in both the Markdown
	/// report and the JSON artifact's `recommendation` field.
	/// </summary>
	[Fact]
	public void AHighFindingWithNoTemplateRendersTheAnalyzersDeclaredFallbackTextInBothArtifacts()
	{
		using PipelineHarness harness = PipelineHarness.Create(languageFallbackText: ReportHarness.LANGUAGE_FALLBACK_TEXT);
		harness.WriteSourceFile(_SOURCE_FILE_NAME, _sourceContent);

		harness.RunTheWholeWorkflow();

		string reportText = harness.ReadWrittenArtifact(OutputArtifactKind.MarkdownReport);
		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		Assert.Contains(ReportHarness.LANGUAGE_FALLBACK_TEXT, reportText, StringComparison.Ordinal);
		Assert.DoesNotContain(GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT, reportText, StringComparison.Ordinal);

		ArtifactFindingDocument finding = FindByPatternClass(artifactText, PipelineLanguageAnalyzer.UNTEMPLATED_PATTERN_CLASS);

		Assert.StartsWith(ReportHarness.LANGUAGE_FALLBACK_TEXT, finding.Recommendation, StringComparison.Ordinal);
		Assert.Contains(RecommendationComposer.OAUTH2_CLIENT_LABEL, finding.Recommendation, StringComparison.Ordinal);
	}

	/// <summary>
	/// The unchanged case: an analyzer that declares no fallback text still reaches the shared engine's
	/// Client Credentials <see cref="GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT"/>.
	/// </summary>
	[Fact]
	public void AHighFindingWithNoTemplateAndNoDeclaredFallbackStillRendersTheBaseNeutralText()
	{
		using PipelineHarness harness = PipelineHarness.Create();
		harness.WriteSourceFile(_SOURCE_FILE_NAME, _sourceContent);

		harness.RunTheWholeWorkflow();

		string artifactText = harness.ReadWrittenArtifact(OutputArtifactKind.StructuredDataArtifact);

		ArtifactFindingDocument finding = FindByPatternClass(artifactText, PipelineLanguageAnalyzer.UNTEMPLATED_PATTERN_CLASS);

		Assert.StartsWith(GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT, finding.Recommendation, StringComparison.Ordinal);
	}

	private static ArtifactFindingDocument FindByPatternClass(string artifactText, string patternClassIdentifier)
	{
		ArtifactDocument? document = JsonSerializer.Deserialize(artifactText, ReportJsonContext.Default.ArtifactDocument);

		return document!.Findings.Single(finding => finding.PatternClass == patternClassIdentifier);
	}
}
