using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>Which of the three routes produced a finding's recommendation.</summary>
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
/// It is produced at the moment of writing and held by nobody, so that the artifact is written
/// streaming. The type exists for the width of one <c>foreach</c> body and is never collected - which
/// is also why <see cref="Finding"/> carries no rendered recommendation field of its own.
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
	/// The base-owned OAuth2 Client block for the finding's flow, the fifth labelled piece
	/// of the composed text. Empty on the manual-review route, which has no flow.
	/// </summary>
	public required string OAuth2ClientBlock { get; init; }

	/// <summary>
	/// The whole-text form, for the two routes that have no four pieces: manual review and the
	/// generic fallback. Empty when a template supplied the four above.
	/// </summary>
	public required string Narrative { get; init; }

	/// <summary>
	/// The security note, present only when the literal-credential signal is set. Empty
	/// otherwise, and its emptiness is what a renderer tests rather than re-deriving the signal.
	/// </summary>
	public required string SecurityNote { get; init; }
}

/// <summary>
/// Recommendation rendering, performed at write time and substituting only two things.
///
/// A migration template is written as "before and after", and interpolating the construct found would
/// put file text into the report. So the source form is the generic form of the pattern class, taken
/// from the catalog, and the only substitutions permitted anywhere in a template are the relative path
/// and the line number. There is no code path in this class that can put file text into a
/// recommendation, because the only inputs are catalog text and a <see cref="Finding"/>, and
/// <see cref="Finding"/> carries no file text by construction.
///
/// The text is not composed here. <see cref="RecommendationComposer"/> is the single, finding-free
/// composition point for (language, pattern class, flow); this class asks it, then substitutes the
/// placeholders and attaches the security note.
///
/// The routes, in order.
///
/// A Low-level finding is not looked up at all. Low-level classes carry no template and are routed to
/// manual review, so the template provider is not consulted, and a catalog that mistakenly registered
/// a template against a Low class cannot cause one to be rendered.
///
/// A High or Medium finding with a template for its flow renders the template's four pieces.
///
/// A High or Medium finding without one goes to the finding's own language's declared fallback text
/// for its flow when the analyzer supplies one, and otherwise to the injected provider's base text for
/// that flow - see <see cref="IGenericRecommendationProvider"/>.
///
/// The OAuth2 Client block for the flow rides on both, as a fifth labelled piece of the composed text.
/// The security note attaches to the literal-credential signal rather than to the route, including on
/// the generic branch: the embedded-secret signal comes from the zone mask and not from recognizing
/// the construct. It names the location and never the value. Neither the security note nor the flow
/// note is part of the composed text.
/// </summary>
public sealed class RecommendationRenderer
{
	/// <summary>
	/// Manual review. It says what the level means and what it does not mean: a Low
	/// finding is a construct the rubric could not stand behind, not a construct it cleared.
	/// </summary>
	public const string MANUAL_REVIEW_TEXT =
		"Routed to manual review. This construct was recognized at low confidence, so the catalog carries no migration template for it and the change has not been characterized. Whoever owns this code should confirm whether it performs Basic Authentication before acting on it, and should not read the level as evidence that it does not.";

	/// <summary>
	/// The security note. The migration and the exposure have different urgencies - the
	/// migration can be scheduled and the exposure cannot - so it is stated as its own action
	/// rather than folded into the advice about the scheme.
	/// </summary>
	public const string LITERAL_CREDENTIAL_SECURITY_TEXT =
		"Security, separate from the migration: the credential at this location is embedded in source and must be treated as compromised and rotated now. The repository history has published it for as long as the code has existed, so deleting the line does not undo the exposure. Its replacement belongs in the process environment or a secret store, per " + SecretManagementPointer.PART_B_NAME + ", and never in source.";

	/// <summary>
	/// The one token a template may use to ask for the finding's relative path. The relative path and
	/// the line number are the only substitutions, and naming them as explicit tokens is what keeps
	/// "only" checkable: a template with no token gets no substitution, and no other token is recognized
	/// at all.
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
	/// What separates the labelled pieces in the artifact's `recommendation` string: a blank line, not
	/// the single line break <see cref="MarkdownReportRenderer"/> leaves between the list items it renders
	/// for a template route.
	///
	/// On the template route the four template pieces are followed by a fifth,
	/// <see cref="RecommendationComposer.OAUTH2_CLIENT_LABEL"/>; on the generic route the fifth follows the
	/// narrative. The flow note and the security note are not in the joined text on either route.
	///
	/// A template piece can itself hold several paragraphs and a fenced code block, each separated by this
	/// same "\n\n" inside the template's own text. What tells a piece boundary from an internal paragraph
	/// break is that only a piece boundary is followed by one of the declared `Label: ` prefixes.
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
	/// destination decides how a substituted path is escaped: a path is untrusted text on the way out,
	/// and a template that substitutes one inherits that.
	///
	/// The text is composed by <see cref="RecommendationComposer"/>; this method substitutes the two
	/// permitted placeholders into it and attaches the security note - it decides nothing about what is
	/// said.
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
	/// Which of the three routes a finding takes, without rendering it.
	///
	/// The Suggest stage needs the routing and must not produce the text: recommendations are rendered at
	/// write time and held by nobody. So the routing is exposed and the rendering is not.
	///
	/// A Low-level finding is not looked up at all: Low-level classes carry no template and are routed to
	/// manual review. A High or Medium finding is routed by <see cref="RecommendationComposer.RouteOf"/> on
	/// its (class, flow), the same lookup <see cref="Render"/> composes from, so <c>suggest</c> and the
	/// report cannot disagree. The generic provider is not consulted.
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

	private static RenderedRecommendation BuildManualReview(Finding finding)
	{
		return new RenderedRecommendation
		{
			Kind = RecommendationKind.ManualReview,
			GenericSourceForm = string.Empty,
			TargetFormUnderTokenAuthentication = string.Empty,
			CredentialProvenance = string.Empty,
			VerificationNote = string.Empty,
			Narrative = MANUAL_REVIEW_TEXT,
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
