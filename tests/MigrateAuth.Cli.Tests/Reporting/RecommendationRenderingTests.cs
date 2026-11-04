using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The three recommendation routes, the security note, and the boundary these tests do not cross.
///
/// The test that matters most is the last one: a High finding whose class has no template calls
/// the injected provider rather than producing text of its own. A renderer that quietly invented
/// something plausible would satisfy every other assertion in this file while producing a
/// recommendation that reads like a specific one and invites a developer to apply a transformation
/// the tool never verified applies.
/// </summary>
public sealed class RecommendationRenderingTests
{
	[Fact]
	public void AHighFindingWithATemplateRendersItsFourPieces()
	{
		RenderedRecommendation rendered = Render(
			ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			new UnwiredGenericRecommendationProvider());

		Assert.Equal(RecommendationKind.FromTemplate, rendered.Kind);
		Assert.Equal(ReportHarness.TEMPLATE_SOURCE_FORM, rendered.GenericSourceForm);
		Assert.Equal(ReportHarness.TEMPLATE_TARGET_FORM, rendered.TargetFormUnderTokenAuthentication);
		Assert.Equal(ReportHarness.TEMPLATE_CREDENTIAL_PROVENANCE, rendered.CredentialProvenance);
		Assert.Equal(ReportHarness.TEMPLATE_VERIFICATION_NOTE, rendered.VerificationNote);
		Assert.Equal(string.Empty, rendered.Narrative);
	}

	/// <summary>
	/// The provider is not consulted at all, so a catalog that mistakenly registered a
	/// template against a Low class cannot cause one to be rendered.
	/// </summary>
	[Fact]
	public void ALowFindingGoesToManualReviewWithoutConsultingEitherSource()
	{
		RecordingGenericRecommendationProvider provider = new();

		RenderedRecommendation rendered = Render(
			ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low),
			provider);

		Assert.Equal(RecommendationKind.ManualReview, rendered.Kind);
		Assert.Equal(RecommendationRenderer.MANUAL_REVIEW_TEXT, rendered.Narrative);
		Assert.Equal(string.Empty, rendered.GenericSourceForm);
		Assert.Empty(provider.AskedAbout);
	}

	/// <summary>
	/// The boundary between these tests and the ones that own the generic text. What is asserted
	/// is that the extension point was CALLED - not that some particular sentence came back.
	/// </summary>
	[Fact]
	public void AHighFindingWithNoTemplateCallsTheExtensionPointRatherThanInventingText()
	{
		RecordingGenericRecommendationProvider provider = new();

		RenderedRecommendation rendered = Render(
			ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.UNTEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			provider);

		Assert.Equal(RecommendationKind.Generic, rendered.Kind);
		Assert.Equal(RecordingGenericRecommendationProvider.TEXT, rendered.Narrative);
		Assert.Equal([CredentialFlow.ClientCredentials], provider.AskedAbout);
	}

	/// <summary>
	/// The language's own fallback text supersedes the generic one. When the finding's own language declares a
	/// fallback text, the generic route renders THAT text and never reaches the injected base
	/// provider at all - the language's own text takes precedence, not merely a place beside it.
	/// </summary>
	[Fact]
	public void AHighFindingWithNoTemplateInALanguageThatDeclaresAFallbackTextRendersItInsteadOfTheBaseProvider()
	{
		RecordingGenericRecommendationProvider provider = new();

		RenderedRecommendation rendered = new RecommendationRenderer(ReportHarness.CatalogWithLanguageFallbackText(), provider)
			.Render(
				ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.UNTEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
				EmissionDestination.Markdown);

		Assert.Equal(RecommendationKind.Generic, rendered.Kind);
		Assert.Equal(ReportHarness.LANGUAGE_FALLBACK_TEXT, rendered.Narrative);
		Assert.Empty(provider.AskedAbout);
	}

	[Fact]
	public void AMediumFindingInALanguageWithNoAnalyzerAlsoReachesTheExtensionPoint()
	{
		RecordingGenericRecommendationProvider provider = new();

		RenderedRecommendation rendered = new RecommendationRenderer(ReportHarness.CatalogWithNoAnalyzers(), provider)
			.Render(
				ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium),
				EmissionDestination.Markdown);

		Assert.Equal(RecommendationKind.Generic, rendered.Kind);
		Assert.Single(provider.AskedAbout);
	}

	/// <summary>
	/// The provider ships a marker rather than advice, because a placeholder that reads like a
	/// recommendation is the failure mode the generic route exists to avoid.
	/// </summary>
	[Fact]
	public void TheShippedProviderProducesAMarkerRatherThanAdvice()
	{
		Assert.StartsWith("[", UnwiredGenericRecommendationProvider.PLACEHOLDER_TEXT, StringComparison.Ordinal);
		Assert.Contains("wired yet", UnwiredGenericRecommendationProvider.PLACEHOLDER_TEXT, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(RecommendationKind.FromTemplate, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)]
	[InlineData(RecommendationKind.Generic, ReportHarness.UNTEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)]
	[InlineData(RecommendationKind.ManualReview, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low)]
	public void TheSecurityPieceRidesOnTheSignalAndNotOnTheRoute(
		RecommendationKind expectedKind,
		string patternClass,
		ConfidenceLevel level)
	{
		RenderedRecommendation withSignal = Render(
			ReportHarness.FindingAt(
				1,
				"source.cs",
				5,
				patternClass,
				level,
				DestinationCertainty.Established,
				literalCredentialSignal: true),
			new UnwiredGenericRecommendationProvider());

		RenderedRecommendation withoutSignal = Render(
			ReportHarness.FindingAt(1, "source.cs", 5, patternClass, level),
			new UnwiredGenericRecommendationProvider());

		Assert.Equal(expectedKind, withSignal.Kind);
		Assert.Equal(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, withSignal.SecurityNote);
		Assert.Equal(string.Empty, withoutSignal.SecurityNote);
	}

	/// <summary>
	/// Only the relative path and the line number are substituted. A template asking for
	/// either gets it; a template asking for anything else gets the text it wrote, unsubstituted,
	/// because there is no mechanism for a third token.
	/// </summary>
	[Fact]
	public void OnlyThePathAndTheLineAreSubstituted()
	{
		LanguageCatalog catalog = CatalogWithTemplate(
			$"at {RecommendationRenderer.RELATIVE_PATH_PLACEHOLDER} line {RecommendationRenderer.LINE_PLACEHOLDER}, and {{credential}}");

		RenderedRecommendation rendered = new RecommendationRenderer(catalog, new UnwiredGenericRecommendationProvider())
			.Render(
				ReportHarness.FindingAt(1, "folder/source.cs", 42, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
				EmissionDestination.Markdown);

		Assert.Equal("at folder/source.cs line 42, and {credential}", rendered.GenericSourceForm);
	}

	/// <summary>
	/// The JSON artifact's `recommendation` string follows the same fence-first-line rule
	/// <see cref="MarkdownReportRenderer"/> applies: a field whose own first physical line opens a code
	/// fence writes its label alone, one line break ahead of the value, rather than sharing the value's
	/// first line - the same CommonMark trap one step removed, for a consumer that renders this JSON
	/// string as Markdown.
	/// </summary>
	[Fact]
	public void ComposeArtifactTextWritesTheLabelAloneWhenAFieldOpensWithAFence()
	{
		const string fencedSourceForm = "```csharp\nvar credentials = new UsernamePasswordCredentials(user, pass);\n```";

		LanguageCatalog catalog = CatalogWithTemplate(fencedSourceForm);

		RenderedRecommendation rendered = new RecommendationRenderer(catalog, new UnwiredGenericRecommendationProvider())
			.Render(
				ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
				EmissionDestination.StructuredData);

		string? artifactText = RecommendationRenderer.ComposeArtifactText(rendered);

		Assert.NotNull(artifactText);
		Assert.StartsWith(
			$"{RecommendationRenderer.SOURCE_FORM_LABEL}:\n{fencedSourceForm}",
			artifactText,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The ordinary case: a field whose own first physical line is ordinary text shares its
	/// label's line with the value.
	/// </summary>
	[Fact]
	public void ComposeArtifactTextKeepsTheLabelAndValueOnOneLineWhenNoFieldOpensWithAFence()
	{
		RenderedRecommendation rendered = Render(
			ReportHarness.FindingAt(1, "source.cs", 5, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High),
			new UnwiredGenericRecommendationProvider());

		string? artifactText = RecommendationRenderer.ComposeArtifactText(rendered);

		Assert.NotNull(artifactText);
		Assert.StartsWith(
			$"{RecommendationRenderer.SOURCE_FORM_LABEL}: {ReportHarness.TEMPLATE_SOURCE_FORM}",
			artifactText,
			StringComparison.Ordinal);
	}

	private static RenderedRecommendation Render(Finding finding, IGenericRecommendationProvider provider)
	{
		return new RecommendationRenderer(ReportHarness.CatalogWithCSharpOnly(), provider)
			.Render(finding, EmissionDestination.Markdown);
	}

	private static LanguageCatalog CatalogWithTemplate(string sourceForm)
	{
		TestLanguageAnalyzer analyzer = new(
			AssessedLanguages.CanonicalName(AssessedLanguage.CSharp),
			".cs",
			[ReportHarness.TEMPLATED_PATTERN_CLASS],
			new Dictionary<string, RecommendationTemplate>(StringComparer.Ordinal)
			{
				[ReportHarness.TEMPLATED_PATTERN_CLASS] = new RecommendationTemplate
				{
					GenericSourceForm = sourceForm,
					TargetFormUnderTokenAuthentication = ReportHarness.TEMPLATE_TARGET_FORM,
					CredentialProvenance = ReportHarness.TEMPLATE_CREDENTIAL_PROVENANCE,
					VerificationNote = ReportHarness.TEMPLATE_VERIFICATION_NOTE
				}
			});

		return new LanguageCatalog(LanguageCatalog.CurrentVersion, [analyzer]);
	}
}
