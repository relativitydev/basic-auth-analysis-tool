using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>Which of the three recommendation routes produced a finding's recommendation.</summary>
public enum RecommendationKind
{
	/// <summary>The catalog carried a template for this pattern class, and it was rendered.</summary>
	FromTemplate = 0,

	/// <summary>A Low-level class carries no template and is routed to a human.</summary>
	ManualReview = 1,

	/// <summary>The fallback for a High or Medium class the catalog does not cover.</summary>
	Generic = 2
}

/// <summary>
/// One finding's recommendation, rendered and about to be written.
///
/// IT IS PRODUCED AT THE MOMENT OF WRITING AND HELD BY NOBODY. The artifact is
/// written streaming, and a list of these built before the write would hold every rendered
/// recommendation in memory at once. The type therefore exists for the width
/// of one <c>foreach</c> body and is never collected - which is also why
/// <see cref="Finding"/> carries no rendered recommendation field of its own.
/// </summary>
public sealed record RenderedRecommendation
{
	/// <summary>Which route produced it.</summary>
	public required RecommendationKind Kind { get; init; }

	/// <summary>Template piece 1. The generic form of the pattern class, never text from the file.</summary>
	public required string GenericSourceForm { get; init; }

	/// <summary>Template piece 2. The equivalent form under token authentication.</summary>
	public required string TargetFormUnderTokenAuthentication { get; init; }

	/// <summary>Template piece 3. Where the client credentials belong.</summary>
	public required string CredentialProvenance { get; init; }

	/// <summary>Template piece 4. How to confirm the change worked.</summary>
	public required string VerificationNote { get; init; }

	/// <summary>
	/// The engine-owned OAuth2 Client block for the finding's flow, the fifth labelled piece
	/// of the composed text. Empty on the manual-review route, which has no flow.
	/// </summary>
	public required string OAuth2ClientBlock { get; init; }

	/// <summary>
	/// The whole-text form, for the two routes that have no four pieces: manual review and the
	/// generic fallback. Empty when a template supplied the four above.
	/// </summary>
	public required string Narrative { get; init; }

	/// <summary>
	/// The fifth piece, present only when the literal-credential signal is set. Empty
	/// otherwise, and its emptiness is what a renderer tests rather than re-deriving the signal.
	/// </summary>
	public required string SecurityNote { get; init; }
}

/// <summary>
/// THE RECOMMENDATION RENDERING, PERFORMED AT WRITE TIME AND SUBSTITUTING ONLY TWO THINGS.
///
/// The problem is that a migration template is written as "before and
/// after", and the natural way to implement that is to interpolate the construct found - which is
/// what makes it useful and is also the design's highest-probability leak, requiring no error of
/// reasoning at all. So the source form is the GENERIC form of the pattern class, taken from the
/// catalog, and the only substitutions permitted anywhere in a template are the relative path and
/// the line number. There is no code path in this class that can put file text into a
/// recommendation, because the only inputs are catalog text and a
/// <see cref="Finding"/>, and <see cref="Finding"/> carries no file text by construction.
///
/// THE TEXT IS NOT COMPOSED HERE. <see cref="RecommendationComposer"/> is the single,
/// finding-free composition point for (language, pattern class, flow); this class asks it, then
/// substitutes the placeholders and attaches the security note.
///
/// THE ROUTES, IN A FIXED ORDER.
///
/// A LOW-LEVEL FINDING IS NOT LOOKED UP AT ALL. Low-level classes carry no template and are
/// routed to manual review, so the template provider is not consulted, and a
/// catalog that mistakenly registered a template against a Low class cannot cause one to be
/// rendered. Asking and ignoring the answer would leave that possible.
///
/// A HIGH OR MEDIUM FINDING WITH A TEMPLATE FOR ITS FLOW renders the template's four pieces.
///
/// A HIGH OR MEDIUM FINDING WITHOUT ONE goes to the finding's own language's declared fallback text
/// for its flow when the analyzer supplies one, and otherwise to the injected provider's generic text for
/// that flow - see <see cref="IGenericRecommendationProvider"/>.
///
/// AND THE OAUTH2 CLIENT BLOCK FOR THE FLOW RIDES ON BOTH, as a fifth labelled piece of
/// the composed text. The other additional piece, the security note, attaches to the
/// literal-credential signal rather than to the route, and says so explicitly of the generic branch:
/// the embedded-secret signal comes from the zone mask and not from recognizing the construct, so a
/// construct we failed to characterize can still carry a published secret. It names the location and
/// never the value, because a secret's value is never reported. NEITHER THE SECURITY NOTE NOR THE FLOW NOTE IS PART OF THE
/// COMPOSED TEXT.
/// </summary>
public sealed class RecommendationRenderer
{
	/// <summary>
	/// Manual review, up to the pointer. It says what the level means and what it does not mean: a Low
	/// finding is a construct the rubric could not stand behind, not a construct it cleared. The
	/// pointer that ends the text is added by <see cref="ManualReviewTextFor"/>, which names the
	/// finding's own language and, for a class that is Low itself, the class's subsection.
	/// </summary>
	public const string MANUAL_REVIEW_LEAD_TEXT =
		"Routed to manual review. This construct was recognized at low confidence, so the tool gives no migration suggestion for it and the change has not been characterized. Whoever owns this code should confirm whether it performs Basic Authentication before acting on it, and should not read the level as evidence that it does not. How to judge a match of this kind is described in ";

	/// <summary>
	/// The title of the section of Part C that explains how to judge a Low-confidence match, as the
	/// documentation titles it. Every language's Part C carries it, and every class that is Low itself
	/// has a subsection of its own under it, headed by the class identifier and display name.
	/// </summary>
	public const string LOW_CONFIDENCE_SECTION_TITLE = "Judging a Low-confidence match";

	/// <summary>
	/// What follows the language name in the pointer: the migration documentation Part C belongs to. A
	/// name and no link, on the same terms as <see cref="SecretManagementPointer"/>: the tool is offline
	/// by design and cannot check where the document is delivered, so there is no path to go dead.
	/// </summary>
	public const string LOW_CONFIDENCE_GUIDANCE_DOCUMENT_SUFFIX = " of the Migration Documentation";

	private const string _CLASS_SUBSECTION_FORMAT = "\"{0} \u00b7 {1}\" under ";
	private const string _SECTION_IN_PART_C_FORMAT = "\"{0}\" in Part C for {1}{2}.";
	private const string _CSHARP_DISPLAY_NAME = "C#";
	private const string _VBNET_DISPLAY_NAME = "VB.NET";

	/// <summary>
	/// The names the Part C documents carry in their titles where they differ from the canonical language
	/// name; a language not listed is titled by its canonical name.
	/// </summary>
	private static readonly Dictionary<AssessedLanguage, string> _documentTitleNamesByLanguage = new()
	{
		[AssessedLanguage.CSharp] = _CSHARP_DISPLAY_NAME,
		[AssessedLanguage.VBNet] = _VBNET_DISPLAY_NAME
	};

	/// <summary>
	/// The manual-review text for a finding. The pointer names Part C for the finding's language, and
	/// when the class is Low by definition and the catalog declares it, the class's own subsection -
	/// the heading Part C carries for it - so the reader lands on the paragraph that judges that class.
	/// </summary>
	public static string ManualReviewTextFor(LanguageCatalog catalog, Finding finding)
	{
		ArgumentNullException.ThrowIfNull(catalog);

		string languageName = AssessedLanguages.CanonicalName(finding.Language);
		string classSubsection = string.Empty;

		if (finding.LowReasons.HasFlag(LowConfidenceReasons.PatternClass)
			&& catalog.TryGetDetectionRule(languageName, finding.PatternClass.Value, out DetectionRule? rule)
			&& rule is not null)
		{
			classSubsection = string.Format(CultureInfo.InvariantCulture, _CLASS_SUBSECTION_FORMAT, finding.PatternClass.Value, rule.DisplayName);
		}

		string documentLanguageName = _documentTitleNamesByLanguage.GetValueOrDefault(finding.Language, languageName);

		return MANUAL_REVIEW_LEAD_TEXT
			+ classSubsection
			+ string.Format(CultureInfo.InvariantCulture, _SECTION_IN_PART_C_FORMAT, LOW_CONFIDENCE_SECTION_TITLE, documentLanguageName, LOW_CONFIDENCE_GUIDANCE_DOCUMENT_SUFFIX);
	}

	/// <summary>
	/// The fifth piece. The migration and the exposure have different urgencies - the
	/// migration can be scheduled and the exposure cannot - so it is stated as its own action
	/// rather than folded into the advice about the scheme.
	/// </summary>
	public const string LITERAL_CREDENTIAL_SECURITY_TEXT =
		"Security, separate from the migration: the credential at this location is embedded in source and must be treated as compromised and rotated now. The repository history has published it for as long as the code has existed, so deleting the line does not undo the exposure. Its replacement belongs in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source.";

	/// <summary>
	/// The one token a template may use to ask for the finding's relative path. Only the
	/// relative path and the line number are permitted as substitutions, and naming them as explicit
	/// tokens is what keeps "only" checkable: a template with no token gets no substitution, and
	/// no other token is recognized at all.
	/// </summary>
	public const string RELATIVE_PATH_PLACEHOLDER = "{path}";

	/// <summary>The one token a template may use to ask for the finding's line number.</summary>
	public const string LINE_PLACEHOLDER = "{line}";

	/// <summary>
	/// Template piece 1's label, shared with <see cref="MarkdownReportRenderer"/> so that its bullet
	/// row and the artifact's `recommendation` string are built from one declared
	/// label rather than two copies that can drift apart.
	/// </summary>
	public const string SOURCE_FORM_LABEL = "Source form";

	/// <summary>Template piece 2's label.</summary>
	public const string TARGET_FORM_LABEL = "Under token authentication";

	/// <summary>Template piece 3's label.</summary>
	public const string CREDENTIALS_LABEL = "Credentials";

	/// <summary>Template piece 4's label.</summary>
	public const string VERIFICATION_LABEL = "Verification";

	/// <summary>
	/// What separates the four template pieces in the artifact's `recommendation` string: a
	/// BLANK LINE, not the single line break <see cref="MarkdownReportRenderer"/> leaves
	/// between the four list items it renders for a template route.
	///
	/// THE LABEL SET THIS SEPARATOR JOINS ALSO HAS A FIFTH PIECE: a fifth labelled piece,
	/// <see cref="RecommendationComposer.OAUTH2_CLIENT_LABEL"/>, follows the four on the template route
	/// and follows the narrative on the generic route. The flow note and the security note are not in
	/// the joined text on either route.
	///
	/// A template piece can itself hold several paragraphs and a fenced code block, each already
	/// separated by this same "\n\n" inside the template's own text. What tells a piece boundary
	/// from an internal paragraph boundary is that only a piece boundary is followed by one of the
	/// four declared `Label: ` prefixes, and an internal paragraph never is.
	/// </summary>
	public const string ARTIFACT_TEXT_PARAGRAPH_SEPARATOR = "\n\n";

	private readonly LanguageCatalog _catalog;
	private readonly IGenericRecommendationProvider _genericRecommendations;

	public RecommendationRenderer(LanguageCatalog catalog, IGenericRecommendationProvider genericRecommendations)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		ArgumentNullException.ThrowIfNull(genericRecommendations);

		_catalog = catalog;
		_genericRecommendations = genericRecommendations;
	}

	/// <summary>
	/// Renders one finding's recommendation for the destination it is about to be written to. The
	/// destination decides how a substituted path is escaped, because a path
	/// is untrusted text on the way out, and a template that substitutes one inherits that.
	///
	/// THE TEXT IS COMPOSED BY <see cref="RecommendationComposer"/> and this method
	/// substitutes the two permitted placeholders into it and attaches the security note - it decides
	/// nothing about what is said.
	/// </summary>
	public RenderedRecommendation Render(Finding finding, EmissionDestination destination)
	{
		RenderedRecommendation rendered;

		if (finding.Level is ConfidenceLevel.Low)
		{
			rendered = BuildManualReview(finding);
		}
		else
		{
			RenderedRecommendation composed = RecommendationComposer.Compose(
				_catalog,
				_genericRecommendations,
				finding.Language,
				finding.PatternClass.Value,
				FlowOf(finding));

			rendered = SubstituteInto(composed, finding, destination);
		}

		return rendered;
	}

	/// <summary>
	/// WHICH OF THE THREE ROUTES A FINDING TAKES, WITHOUT RENDERING IT.
	///
	/// The Suggest stage needs the routing and must not produce the text: recommendations are
	/// rendered at write time and held by nobody, and a stage that rendered
	/// them one command early would either retain them all in memory or throw the
	/// work away. So the routing is exposed and the rendering is not.
	///
	/// A LOW-LEVEL FINDING IS NOT LOOKED UP AT ALL, because low-level
	/// classes carry no template and are routed to manual review. A High or Medium
	/// finding is routed by <see cref="RecommendationComposer.RouteOf"/> on its (class, flow), the same
	/// lookup <see cref="Render"/> composes from, so <c>suggest</c> and the report cannot disagree.
	/// The generic provider is not consulted: asking it a question whose answer is discarded would be
	/// asking it to do work for a stage that prints a count.
	/// </summary>
	public RecommendationKind Route(Finding finding)
	{
		return finding.Level is ConfidenceLevel.Low
			? RecommendationKind.ManualReview
			: RecommendationComposer.RouteOf(_catalog, finding.Language, finding.PatternClass.Value, FlowOf(finding));
	}

	/// <summary>
	/// The flow a High or Medium finding targets. Analysis assigns one to every such finding; a
	/// finding that has not been through assignment - a hand-built one, or one read from a state that
	/// predates the field - takes the built-in default rather than having no recommendation.
	/// </summary>
	private static CredentialFlow FlowOf(Finding finding)
	{
		return finding.Flow ?? CredentialFlows.BUILT_IN_DEFAULT;
	}

	private RenderedRecommendation BuildManualReview(Finding finding)
	{
		return new RenderedRecommendation
		{
			Kind = RecommendationKind.ManualReview,
			GenericSourceForm = string.Empty,
			TargetFormUnderTokenAuthentication = string.Empty,
			CredentialProvenance = string.Empty,
			VerificationNote = string.Empty,
			Narrative = ManualReviewTextFor(_catalog, finding),
			OAuth2ClientBlock = string.Empty,
			SecurityNote = SecurityNoteFor(finding)
		};
	}

	private static RenderedRecommendation SubstituteInto(
		RenderedRecommendation composed,
		Finding finding,
		EmissionDestination destination)
	{
		return composed with
		{
			GenericSourceForm = Substitute(composed.GenericSourceForm, finding, destination),
			TargetFormUnderTokenAuthentication = Substitute(composed.TargetFormUnderTokenAuthentication, finding, destination),
			CredentialProvenance = Substitute(composed.CredentialProvenance, finding, destination),
			VerificationNote = Substitute(composed.VerificationNote, finding, destination),
			SecurityNote = SecurityNoteFor(finding)
		};
	}

	private static string SecurityNoteFor(Finding finding)
	{
		return finding.LiteralCredentialSignal ? LITERAL_CREDENTIAL_SECURITY_TEXT : string.Empty;
	}

	/// <summary>
	/// The two permitted substitutions, and no mechanism for a third. A template that carries
	/// neither token comes through unchanged, which is the ordinary case.
	/// </summary>
	private static string Substitute(string templateText, Finding finding, EmissionDestination destination)
	{
		string pathText = EmittedPathText.ForDestination(finding.Path, destination);
		string lineText = finding.Line.ToString(CultureInfo.InvariantCulture);

		return templateText
			.Replace(RELATIVE_PATH_PLACEHOLDER, pathText, StringComparison.Ordinal)
			.Replace(LINE_PLACEHOLDER, lineText, StringComparison.Ordinal);
	}

	/// <summary>
	/// THE FINDING RECORD'S `recommendation`. Composed by <see cref="RecommendationComposer.ComposeArtifactText"/>,
	/// the one place the labelled pieces are joined; kept here so that a caller holding a renderer and
	/// a rendered recommendation has the joined text at hand. Null for manual review, and neither the
	/// security note nor the flow note is ever part of it.
	/// </summary>
	public static string? ComposeArtifactText(RenderedRecommendation rendered)
	{
		return RecommendationComposer.ComposeArtifactText(rendered);
	}
}
