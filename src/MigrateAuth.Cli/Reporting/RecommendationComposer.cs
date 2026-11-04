using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE SINGLE, FINDING-FREE POINT WHERE A RECOMMENDATION IS COMPOSED.
///
/// A recommendation is a function of three things - the language, the pattern class and the flow -
/// and of nothing about any one finding. The Markdown report, the JSON artifact, the generated Part C
/// blocks and the User Guide's OAuth2 Client row all compose through <see cref="Compose"/>, so the
/// four cannot say different things about the same (language, class, flow), and a drift test can call
/// the composer directly rather than render a report to find out what it would have said.
/// <see cref="RecommendationRenderer"/> delegates here and only substitutes the finding's relative path
/// and line number into the placeholders; it decides nothing about what is said.
///
/// WHAT IS COMPOSED, IN ORDER. A catalog template for the (class, flow) contributes four pieces;
/// where the catalog has none, the language's own declared text for the flow and then the
/// shared engine's generic text for the flow contribute one narrative. EITHER WAY THE ENGINE-OWNED OAUTH2 CLIENT
/// BLOCK FOR THE FLOW IS THE FIFTH PIECE, labelled <see cref="OAUTH2_CLIENT_LABEL"/>: a construct the
/// catalog failed to characterize still needs an OAuth2 Client, and the block does not depend on
/// characterizing it. The security note and the flow note are NOT part of the composed text; they ride
/// beside it.
///
/// A LOW-CONFIDENCE FINDING IS NOT COMPOSED. It has no flow and no template and is routed to manual
/// review before this class is asked anything; asking here would invite a caller to compose text for a
/// finding that must never have one.
/// </summary>
public static class RecommendationComposer
{
	/// <summary>
	/// The fifth piece's label, beside the four template pieces (<see cref="RecommendationRenderer.SOURCE_FORM_LABEL"/>,
	/// <see cref="RecommendationRenderer.TARGET_FORM_LABEL"/>, <see cref="RecommendationRenderer.CREDENTIALS_LABEL"/>
	/// and <see cref="RecommendationRenderer.VERIFICATION_LABEL"/>). The block's own first line follows it.
	/// </summary>
	public const string OAUTH2_CLIENT_LABEL = "OAuth2 Client";

	private const string _LABELED_LINE_FORMAT = "{0}: {1}";

	/// <summary>
	/// The label alone on its line, for a piece whose own first physical line opens a code fence: a
	/// fence marker that followed inline text would not open a fenced block under CommonMark, so the
	/// label stands alone and the fence opens the next line exactly as it does in the template text.
	/// </summary>
	private const string _LABEL_ONLY_LINE_FORMAT = "{0}:\n{1}";

	private const char _FIRST_LINE_BREAK = '\n';

	/// <summary>
	/// Composes the recommendation for one (language, pattern class, flow), with the placeholders
	/// unsubstituted and no security note. The result is the same for every finding of that triple.
	/// </summary>
	public static RenderedRecommendation Compose(
		LanguageCatalog catalog,
		IGenericRecommendationProvider genericRecommendations,
		AssessedLanguage language,
		string patternClassIdentifier,
		CredentialFlow flow)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		ArgumentNullException.ThrowIfNull(genericRecommendations);
		ArgumentException.ThrowIfNullOrWhiteSpace(patternClassIdentifier);

		RecommendationTemplate? template = FindTemplate(catalog, language, patternClassIdentifier, flow);
		string oauth2ClientBlock = OAuth2ClientConfigurationBlocks.For(flow);
		RenderedRecommendation composed;

		if (template is null)
		{
			string narrative = LanguageFallbackTextFor(catalog, language, flow) ?? genericRecommendations.DescribeFor(flow);

			composed = new RenderedRecommendation
			{
				Kind = RecommendationKind.Generic,
				GenericSourceForm = string.Empty,
				TargetFormUnderTokenAuthentication = string.Empty,
				CredentialProvenance = string.Empty,
				VerificationNote = string.Empty,
				Narrative = narrative,
				OAuth2ClientBlock = oauth2ClientBlock,
				SecurityNote = string.Empty
			};
		}
		else
		{
			composed = new RenderedRecommendation
			{
				Kind = RecommendationKind.FromTemplate,
				GenericSourceForm = template.GenericSourceForm,
				TargetFormUnderTokenAuthentication = template.TargetFormUnderTokenAuthentication,
				CredentialProvenance = template.CredentialProvenance,
				VerificationNote = template.VerificationNote,
				Narrative = string.Empty,
				OAuth2ClientBlock = oauth2ClientBlock,
				SecurityNote = string.Empty
			};
		}

		return composed;
	}

	/// <summary>
	/// Composes with the shared engine's own generic provider, for the callers that have no reason to inject
	/// another: the generated Part C blocks and the User Guide.
	/// </summary>
	public static RenderedRecommendation Compose(
		LanguageCatalog catalog,
		AssessedLanguage language,
		string patternClassIdentifier,
		CredentialFlow flow)
	{
		return Compose(catalog, new GenericRecommendationProvider(), language, patternClassIdentifier, flow);
	}

	/// <summary>
	/// Which of the two composed routes a High or Medium (class, flow) takes, without producing
	/// any text - the answer the Suggest stage counts. The generic provider is not consulted.
	/// </summary>
	public static RecommendationKind RouteOf(
		LanguageCatalog catalog,
		AssessedLanguage language,
		string patternClassIdentifier,
		CredentialFlow flow)
	{
		ArgumentNullException.ThrowIfNull(catalog);

		return FindTemplate(catalog, language, patternClassIdentifier, flow) is null
			? RecommendationKind.Generic
			: RecommendationKind.FromTemplate;
	}

	/// <summary>
	/// THE FINDING RECORD'S `recommendation`, joined from the composed pieces in one place so the Markdown
	/// report's list items and the artifact's string are built from the same labels and cannot drift.
	///
	/// NULL FOR <see cref="RecommendationKind.ManualReview"/>, WHICH IS EXACTLY LOW: the one expression
	/// of the manual-review signal in the artifact. The template route joins the four
	/// labelled pieces and the fifth on <see cref="RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR"/>;
	/// the generic route joins its narrative and the fifth. The security note and the flow note never
	/// appear here.
	/// </summary>
	public static string? ComposeArtifactText(RenderedRecommendation rendered)
	{
		ArgumentNullException.ThrowIfNull(rendered);

		string? text = rendered.Kind switch
		{
			RecommendationKind.ManualReview => null,
			RecommendationKind.Generic => JoinWithClientBlock(rendered, rendered.Narrative),
			_ => JoinWithClientBlock(rendered, ComposeTemplateText(rendered))
		};

		return text;
	}

	private static string JoinWithClientBlock(RenderedRecommendation rendered, string leadingText)
	{
		return string.Join(
			RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR,
			leadingText,
			LabeledLine(OAUTH2_CLIENT_LABEL, rendered.OAuth2ClientBlock));
	}

	private static string ComposeTemplateText(RenderedRecommendation rendered)
	{
		return string.Join(
			RecommendationRenderer.ARTIFACT_TEXT_PARAGRAPH_SEPARATOR,
			LabeledLine(RecommendationRenderer.SOURCE_FORM_LABEL, rendered.GenericSourceForm),
			LabeledLine(RecommendationRenderer.TARGET_FORM_LABEL, rendered.TargetFormUnderTokenAuthentication),
			LabeledLine(RecommendationRenderer.CREDENTIALS_LABEL, rendered.CredentialProvenance),
			LabeledLine(RecommendationRenderer.VERIFICATION_LABEL, rendered.VerificationNote));
	}

	/// <summary>
	/// One labelled piece. When the piece's own first physical line opens a code fence the label is
	/// written alone and the value - fence included - starts the next line;
	/// <see cref="CodeFenceDetection"/> is the same predicate the Markdown renderer tests, so the two
	/// renderers cannot decide this differently for the same text.
	/// </summary>
	private static string LabeledLine(string label, string value)
	{
		string format = CodeFenceDetection.OpensFence(FirstLineOf(value)) ? _LABEL_ONLY_LINE_FORMAT : _LABELED_LINE_FORMAT;

		return string.Format(CultureInfo.InvariantCulture, format, label, value);
	}

	private static string FirstLineOf(string value)
	{
		int breakIndex = value.IndexOf(_FIRST_LINE_BREAK);

		return breakIndex < 0 ? value : value[..breakIndex];
	}

	/// <summary>
	/// The template for one (class, flow) from the analyzer that owns the language. The lookup is by
	/// canonical language name, the key an analyzer is registered under; a language with
	/// no analyzer yields no template, the same answer as a class with no entry for the flow.
	/// </summary>
	private static RecommendationTemplate? FindTemplate(
		LanguageCatalog catalog,
		AssessedLanguage language,
		string patternClassIdentifier,
		CredentialFlow flow)
	{
		RecommendationTemplate? template = null;

		if (catalog.AnalyzersByLanguageName.TryGetValue(AssessedLanguages.CanonicalName(language), out ILanguageAnalyzer? analyzer))
		{
			template = analyzer.CandidateSelectionAndRecommendation.TemplateProvider(patternClassIdentifier, flow);
		}

		return template;
	}

	/// <summary>
	/// The language's own declared generic text for one flow, or null where it declares none for it -
	/// which is the signal to fall through to the shared engine's text for that flow.
	/// </summary>
	private static string? LanguageFallbackTextFor(LanguageCatalog catalog, AssessedLanguage language, CredentialFlow flow)
	{
		string? fallbackText = null;

		if (catalog.AnalyzersByLanguageName.TryGetValue(AssessedLanguages.CanonicalName(language), out ILanguageAnalyzer? analyzer))
		{
			analyzer.CandidateSelectionAndRecommendation.LanguageFallbackTextByFlow?.TryGetValue(flow, out fallbackText);
		}

		return fallbackText;
	}
}
