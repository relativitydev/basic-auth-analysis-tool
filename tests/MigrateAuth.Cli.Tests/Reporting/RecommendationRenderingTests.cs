using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Documentation;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The three recommendation routes, the fifth piece, and the boundary these tests do not cross.
///
/// The test that matters most is the last one: a High finding whose class has no template calls
/// the injected provider rather than producing text of its own. A renderer that quietly invented something
/// plausible would satisfy every other assertion in this file while producing a recommendation
/// that reads like a specific one and invites a developer to apply a transformation the tool
/// never verified applies.
/// </summary>
public sealed class RecommendationRenderingTests
{
	private const string _LOW_CONFIDENCE_SECTION_TITLE = "Judging a Low-confidence match";
	private const string _LOW_CONFIDENCE_SECTION_HEADING = "## " + _LOW_CONFIDENCE_SECTION_TITLE;
	private const string _MANUAL_REVIEW_LEAD =
		"Routed to manual review. This construct was recognized at low confidence, so the tool gives no migration suggestion for it and the change has not been characterized. Whoever owns this code should confirm whether it performs Basic Authentication before acting on it, and should not read the level as evidence that it does not. How to judge a match of this kind is described in ";
	private const string _CSHARP_GENERAL_POINTER =
		"\"Judging a Low-confidence match\" in Part C for C# of the Migration Documentation.";

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
		Assert.Equal(_MANUAL_REVIEW_LEAD + _CSHARP_GENERAL_POINTER, rendered.Narrative);
		Assert.Equal(string.Empty, rendered.GenericSourceForm);
		Assert.Empty(provider.AskedAbout);
	}

	/// <summary>
	/// A Low finding points the reader at the guidance on judging it, and the section it names exists in Part C
	/// under exactly that title, so the pointer cannot name a heading the documentation does not have.
	/// </summary>
	[Fact]
	public void TheManualReviewTextPointsToTheLowConfidenceSectionOfPartC()
	{
		string partC = File.ReadAllText(PublicDocuments.PartCPath());

		Assert.Contains(_LOW_CONFIDENCE_SECTION_TITLE, _CSHARP_GENERAL_POINTER, StringComparison.Ordinal);
		Assert.Contains(_LOW_CONFIDENCE_SECTION_HEADING, partC, StringComparison.Ordinal);
		Assert.Equal(_LOW_CONFIDENCE_SECTION_TITLE, RecommendationRenderer.LOW_CONFIDENCE_SECTION_TITLE);
	}

	/// <summary>
	/// The pointer names the Part C of the finding's own language, so a PowerShell finding is not sent to
	/// the C# document, and the section it names is in that language's Part C.
	/// </summary>
	[Theory]
	[InlineData(AssessedLanguage.CSharp)]
	[InlineData(AssessedLanguage.PowerShell)]
	public void TheManualReviewPointerNamesPartCForTheFindingsLanguage(AssessedLanguage language)
	{
		DocumentedLanguage documented = DocumentedLanguageOf(language);
		Finding finding = ReportHarness.FindingAt(1, "source", 5, documented.UndeclaredPatternClass, ConfidenceLevel.Low, language: language);

		string text = RecommendationRenderer.ManualReviewTextFor(CSharpRenderedTexts.Catalog(), finding);

		Assert.Equal(
			_MANUAL_REVIEW_LEAD + $"\"Judging a Low-confidence match\" in Part C for {documented.DisplayName} of the Migration Documentation.",
			text);
		Assert.Contains(_LOW_CONFIDENCE_SECTION_HEADING, File.ReadAllText(documented.PartCPath()), StringComparison.Ordinal);
	}

	/// <summary>
	/// A class that is Low by definition names its own subsection of Part C, and every such class of both
	/// embedded catalogs has that subsection in its language's Part C under exactly the heading the pointer
	/// names, so the reader lands on the paragraph that judges the class and no pointer names a missing one.
	/// </summary>
	[Theory]
	[InlineData(AssessedLanguage.CSharp, "CS-011")]
	[InlineData(AssessedLanguage.CSharp, "CS-019")]
	[InlineData(AssessedLanguage.PowerShell, "PS-012")]
	[InlineData(AssessedLanguage.PowerShell, "PS-013")]
	[InlineData(AssessedLanguage.PowerShell, "PS-014")]
	public void ALowByDefinitionClassPointsToItsOwnSubsectionOfItsLanguagesPartC(AssessedLanguage language, string identifier)
	{
		DocumentedLanguage documented = DocumentedLanguageOf(language);
		Finding finding = ReportHarness.FindingAt(
			1, "source", 5, identifier, ConfidenceLevel.Low, language: language, lowReasons: LowConfidenceReasons.PatternClass);
		string heading = $"{identifier} · {documented.Analyzer().DetectionRulesByPatternClass[identifier].DisplayName}";

		string text = RecommendationRenderer.ManualReviewTextFor(CSharpRenderedTexts.Catalog(), finding);

		Assert.Equal(
			_MANUAL_REVIEW_LEAD + $"\"{heading}\" under \"Judging a Low-confidence match\" in Part C for {documented.DisplayName} of the Migration Documentation.",
			text);
		Assert.Contains($"### {heading}\n", File.ReadAllText(documented.PartCPath()).ReplaceLineEndings("\n"), StringComparison.Ordinal);
	}

	/// <summary>
	/// A Low finding whose reason is a demotion, not its class, points to the section as a whole: the cause
	/// subsections of Part C are by cause, and the class has no paragraph of its own to name.
	/// </summary>
	[Fact]
	public void ADemotedLowFindingPointsToTheSectionAndNotToItsClassSubsection()
	{
		Finding finding = ReportHarness.FindingAt(
			1, "source", 5, "PS-012", ConfidenceLevel.Low, language: AssessedLanguage.PowerShell, lowReasons: LowConfidenceReasons.DemotedFile);

		string text = RecommendationRenderer.ManualReviewTextFor(CSharpRenderedTexts.Catalog(), finding);

		Assert.Equal(
			_MANUAL_REVIEW_LEAD + "\"Judging a Low-confidence match\" in Part C for PowerShell of the Migration Documentation.",
			text);
	}

	/// <summary>A VBNet Low finding's pointer names Part C for "VB.NET", the name its Part C is titled by, and not the enumeration value.</summary>
	[Fact]
	public void AVBNetLowFindingsManualReviewPointerNamesPartCForVBDotNet()
	{
		Finding finding = ReportHarness.FindingAt(
			1, "source", 5, "VB-001", ConfidenceLevel.Low, language: AssessedLanguage.VBNet, lowReasons: LowConfidenceReasons.DemotedFile);

		string text = RecommendationRenderer.ManualReviewTextFor(CSharpRenderedTexts.Catalog(), finding);

		Assert.Equal(
			_MANUAL_REVIEW_LEAD + "\"Judging a Low-confidence match\" in Part C for VB.NET of the Migration Documentation.",
			text);
	}

	/// <summary>The Part C of VBNet is titled by the display name "VB.NET", the same name the pointer carries.</summary>
	[Fact]
	public void ThePartCDocumentOfVBNetIsTitledByTheVBDotNetDisplayName()
	{
		Finding finding = ReportHarness.FindingAt(
			1, "source", 5, "VB-001", ConfidenceLevel.Low, language: AssessedLanguage.VBNet);

		string text = RecommendationRenderer.ManualReviewTextFor(CSharpRenderedTexts.Catalog(), finding);

		Assert.Contains("in Part C for VB.NET of the Migration Documentation.", text, StringComparison.Ordinal);
		Assert.DoesNotContain("Part C for VBNet", text, StringComparison.Ordinal);
	}

	private static DocumentedLanguage DocumentedLanguageOf(AssessedLanguage language)
	{
		return language == AssessedLanguage.CSharp ? DocumentedLanguage.CSharp : DocumentedLanguage.PowerShell;
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
	/// fallback text, the generic route renders THAT text and never reaches the injected default
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
	/// recommendation would be taken for one.
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
	/// Only the relative path and the line number are permitted. A template asking for
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
	/// The JSON artifact's `recommendation` string
	/// follows the same fence-first-line rule <see cref="MarkdownReportRenderer"/>
	/// applies: a field whose own first physical line opens a code fence writes its label
	/// alone, one line break ahead of the value, rather than sharing the value's first line - the
	/// same CommonMark trap one step removed, for a consumer that renders this JSON string as
	/// Markdown.
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
