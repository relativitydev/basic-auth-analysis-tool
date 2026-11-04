using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The recommendation is composed per (language, pattern class, flow) at one
/// finding-free point, the template provider is asked per flow, and the generic route is per flow at
/// both the language and the base level.
/// </summary>
public sealed class RecommendationComposerTests
{
	private const string _CLIENT_CREDENTIALS_LANGUAGE_TEXT = "the fixture language's Client Credentials generic text";
	private const string _AUTHORIZATION_CODE_LANGUAGE_TEXT = "the fixture language's Authorization Code generic text";
	private const string _SOURCE_PATH = "source.cs";

	/// <summary>
	/// A fixture analyzer with a Client Credentials template and no Authorization Code template
	/// for one class renders the template for a Client Credentials finding and the Authorization Code
	/// generic text for an Authorization Code finding.
	/// </summary>
	[Fact]
	public void ATemplateForOneFlowRendersForThatFlowAndTheOtherFlowFallsToItsGenericText()
	{
		RecommendationRenderer renderer = new(ReportHarness.CatalogWithCSharpOnly(), new GenericRecommendationProvider());

		RenderedRecommendation forClientCredentials = renderer.Render(
			TemplatedFinding(CredentialFlow.ClientCredentials),
			EmissionDestination.Markdown);
		RenderedRecommendation forAuthorizationCode = renderer.Render(
			TemplatedFinding(CredentialFlow.AuthorizationCode),
			EmissionDestination.Markdown);

		Assert.Equal(RecommendationKind.FromTemplate, forClientCredentials.Kind);
		Assert.Equal(ReportHarness.TEMPLATE_SOURCE_FORM, forClientCredentials.GenericSourceForm);
		Assert.Equal(OAuth2ClientConfigurationBlocks.For(CredentialFlow.ClientCredentials), forClientCredentials.OAuth2ClientBlock);

		Assert.Equal(RecommendationKind.Generic, forAuthorizationCode.Kind);
		Assert.Equal(GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT, forAuthorizationCode.Narrative);
		Assert.Equal(OAuth2ClientConfigurationBlocks.For(CredentialFlow.AuthorizationCode), forAuthorizationCode.OAuth2ClientBlock);
		Assert.Equal(RecommendationKind.FromTemplate, renderer.Route(TemplatedFinding(CredentialFlow.ClientCredentials)));
		Assert.Equal(RecommendationKind.Generic, renderer.Route(TemplatedFinding(CredentialFlow.AuthorizationCode)));
	}

	/// <summary>
	/// A fixture analyzer registers in-process through the public contract with no change outside
	/// `Catalog/Languages/`: the reference fixture answers the per-flow provider and is contract-clean,
	/// beside the embedded analyzer.
	/// </summary>
	[Fact]
	public void AFixtureAnalyzerRegistersThroughThePublicContractAndAnswersPerFlow()
	{
		LanguageCatalog catalog = ExemplarFixtureCatalog.Build();
		ILanguageAnalyzer fixture = catalog.Analyzers.Single(analyzer => analyzer is ExemplarFixtureAnalyzer);
		string templatedClass = fixture.DetectionRulesByPatternClass.Keys
			.First(identifier => fixture.CandidateSelectionAndRecommendation.TemplateProvider(identifier, CredentialFlow.ClientCredentials) is not null);

		Assert.Empty(AnalyzerContractValidator.Validate(fixture));
		Assert.NotNull(fixture.CandidateSelectionAndRecommendation.TemplateProvider(templatedClass, CredentialFlow.ClientCredentials));
		Assert.Null(fixture.CandidateSelectionAndRecommendation.TemplateProvider(templatedClass, CredentialFlow.AuthorizationCode));
		Assert.Contains(catalog.AnalyzersByLanguageName.Keys, name => name == "CSharp");
	}

	/// <summary>
	/// Markdown and JSON. A High finding with no template renders the language's per-flow text
	/// when the language declares one for the flow, and the base's text for the flow when it does not.
	/// </summary>
	[Fact]
	public void AnUntemplatedFindingRendersTheLanguagesPerFlowTextWhenDeclaredAndTheBasesWhenNot()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithPerFlowFallbackText(_CLIENT_CREDENTIALS_LANGUAGE_TEXT, null);
		ImmutableArray<Finding> findings =
		[
			UntemplatedFinding(1, CredentialFlow.ClientCredentials),
			UntemplatedFinding(2, CredentialFlow.AuthorizationCode)
		];

		ReportContent content = ReportHarness.ContentWith(catalog, ReportHarness.ConfigurationWith(ConfidenceLevel.Low), findings);
		string report = RenderMarkdown(content);
		ArtifactFindingDocument[] artifact = RenderArtifact(content);

		Assert.Contains(_CLIENT_CREDENTIALS_LANGUAGE_TEXT, report, StringComparison.Ordinal);
		Assert.Contains(GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT, report, StringComparison.Ordinal);
		Assert.DoesNotContain(GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT, report, StringComparison.Ordinal);
		Assert.StartsWith(_CLIENT_CREDENTIALS_LANGUAGE_TEXT, artifact[0].Recommendation, StringComparison.Ordinal);
		Assert.StartsWith(GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT, artifact[1].Recommendation, StringComparison.Ordinal);
	}

	/// <summary>A language declaring the Authorization Code text is read for that flow in both artifacts.</summary>
	[Fact]
	public void ALanguageDeclaringBothFlowsTextsIsReadPerFlowInBothArtifacts()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithPerFlowFallbackText(_CLIENT_CREDENTIALS_LANGUAGE_TEXT, _AUTHORIZATION_CODE_LANGUAGE_TEXT);
		ImmutableArray<Finding> findings =
		[
			UntemplatedFinding(1, CredentialFlow.ClientCredentials),
			UntemplatedFinding(2, CredentialFlow.AuthorizationCode)
		];

		ReportContent content = ReportHarness.ContentWith(catalog, ReportHarness.ConfigurationWith(ConfidenceLevel.Low), findings);
		string report = RenderMarkdown(content);
		ArtifactFindingDocument[] artifact = RenderArtifact(content);

		Assert.Contains(_CLIENT_CREDENTIALS_LANGUAGE_TEXT, report, StringComparison.Ordinal);
		Assert.Contains(_AUTHORIZATION_CODE_LANGUAGE_TEXT, report, StringComparison.Ordinal);
		Assert.DoesNotContain(GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT, report, StringComparison.Ordinal);
		Assert.StartsWith(_AUTHORIZATION_CODE_LANGUAGE_TEXT, artifact[1].Recommendation, StringComparison.Ordinal);
	}

	/// <summary>
	/// Neither the flow note nor the security note is part of the composed `recommendation`, on
	/// either route and for either flow; the fifth piece is, and the notes ride beside it.
	/// </summary>
	[Theory]
	[InlineData(ReportHarness.TEMPLATED_PATTERN_CLASS, CredentialFlow.ClientCredentials)]
	[InlineData(ReportHarness.UNTEMPLATED_PATTERN_CLASS, CredentialFlow.ClientCredentials)]
	[InlineData(ReportHarness.UNTEMPLATED_PATTERN_CLASS, CredentialFlow.AuthorizationCode)]
	public void NeitherTheFlowNoteNorTheSecurityNoteIsPartOfTheComposedText(string patternClass, CredentialFlow flow)
	{
		Finding finding = ReportHarness.FindingAt(
			1,
			_SOURCE_PATH,
			3,
			patternClass,
			ConfidenceLevel.High,
			literalCredentialSignal: true,
			flow: flow,
			flowDecision: FlowDecision.Default);
		RecommendationRenderer renderer = new(ReportHarness.CatalogWithCSharpOnly(), new GenericRecommendationProvider());

		RenderedRecommendation rendered = renderer.Render(finding, EmissionDestination.StructuredData);
		string? composed = RecommendationRenderer.ComposeArtifactText(rendered);

		Assert.NotNull(composed);
		Assert.Contains(RecommendationComposer.OAUTH2_CLIENT_LABEL, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, composed, StringComparison.Ordinal);
		Assert.DoesNotContain(FlowNotes.NoteFor(FlowDecision.Default, flow), composed, StringComparison.Ordinal);
		Assert.Equal(RecommendationRenderer.LITERAL_CREDENTIAL_SECURITY_TEXT, rendered.SecurityNote);
	}

	/// <summary>The composer is finding-free: the same (language, class, flow) composes the same text.</summary>
	[Fact]
	public void TheComposerReturnsTheSameCompositionForTheSameLanguageClassAndFlow()
	{
		LanguageCatalog catalog = ReportHarness.CatalogWithCSharpOnly();

		RenderedRecommendation first = RecommendationComposer.Compose(catalog, AssessedLanguage.CSharp, ReportHarness.TEMPLATED_PATTERN_CLASS, CredentialFlow.ClientCredentials);
		RenderedRecommendation second = RecommendationComposer.Compose(catalog, AssessedLanguage.CSharp, ReportHarness.TEMPLATED_PATTERN_CLASS, CredentialFlow.ClientCredentials);

		Assert.Equal(first, second);
		Assert.Equal(string.Empty, first.SecurityNote);
		Assert.Equal(
			RecommendationComposer.ComposeArtifactText(first),
			RecommendationComposer.ComposeArtifactText(second));
	}

	private static Finding TemplatedFinding(CredentialFlow flow)
	{
		return ReportHarness.FindingAt(
			1,
			_SOURCE_PATH,
			3,
			ReportHarness.TEMPLATED_PATTERN_CLASS,
			ConfidenceLevel.High,
			flow: flow,
			flowDecision: FlowDecision.Default);
	}

	private static Finding UntemplatedFinding(int sequentialIndex, CredentialFlow flow)
	{
		return ReportHarness.FindingAt(
			sequentialIndex,
			_SOURCE_PATH,
			sequentialIndex,
			ReportHarness.UNTEMPLATED_PATTERN_CLASS,
			ConfidenceLevel.High,
			flow: flow,
			flowDecision: FlowDecision.Default);
	}

	private static string RenderMarkdown(ReportContent content)
	{
		using StringWriter writer = new();

		MarkdownReportRenderer.Render(writer, content, new GenericRecommendationProvider());

		return writer.ToString();
	}

	private static ArtifactFindingDocument[] RenderArtifact(ReportContent content)
	{
		using MemoryStream stream = new();

		StructuredDataArtifactRenderer.Render(stream, content, new GenericRecommendationProvider());

		ArtifactDocument? document = JsonSerializer.Deserialize(
			Encoding.UTF8.GetString(stream.ToArray()),
			ReportJsonContext.Default.ArtifactDocument);

		Assert.NotNull(document);

		return document.Findings;
	}
}
