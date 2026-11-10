using System.Collections.Frozen;
using System.Collections.Immutable;

using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Catalog;

/// <summary>
/// A preselection anchor. Each anchor carries its own case rule, because the lexical profile's case
/// field is scoped to keywords and delimiters rather than to anchors, and two of the eight
/// languages are case-insensitive.
/// </summary>
public sealed record PreselectionAnchor
{
	public required string Text { get; init; }

	public CaseSensitivity Sensitivity { get; init; } = CaseSensitivity.Sensitive;
}

/// <summary>
/// A static recommendation template. Every template requires four pieces, and none of
/// them is ever interpolated with repository content: the source form is the generic form of
/// the pattern class, taken from the catalog, never text extracted from the file.
/// </summary>
public sealed record RecommendationTemplate
{
	public required string GenericSourceForm { get; init; }

	public required string TargetFormUnderTokenAuthentication { get; init; }

	public required string CredentialProvenance { get; init; }

	public required string VerificationNote { get; init; }
}

/// <summary>
/// Contract element 5, second half, indexed by pattern class AND by the OAuth 2.0 flow the finding
/// targets. It returns null where the class carries no template for that flow - a Low
/// class is routed to manual review, and a High or Medium class with no entry for the finding's flow
/// falls back to the language's declared text for that flow and then to the base's generic text for
/// it.
/// </summary>
public delegate RecommendationTemplate? RecommendationTemplateProvider(string patternClassIdentifier, CredentialFlow flow);

/// <summary>
/// Contract element 5. Preselection anchors for candidate selection, and a template provider for
/// recommendations indexed by class. One element of the contract carrying both, because the
/// contract has six top-level elements and these two are one of them.
///
/// The anchors are an unordered set. An analyzer cannot declare that one anchor is tried
/// before another, because the order in which the engine looks is not an analyzer's decision.
/// </summary>
public sealed class CandidateSelectionAndRecommendation
{
	public required FrozenSet<PreselectionAnchor> PreselectionAnchors { get; init; }

	public required RecommendationTemplateProvider TemplateProvider { get; init; }

	/// <summary>
	/// The language's flow signals, declared here so that the contract stays at six
	/// top-level elements. Empty where the language declares none. Evaluated by the engine alone, per file.
	/// </summary>
	public required ImmutableArray<FlowSignal> FlowSignals { get; init; }

	/// <summary>
	/// OPTIONAL: this
	/// language's own text for the generic recommendation route, per flow, read in place of the base's text for
	/// that flow (<see cref="Reporting.GenericRecommendationProvider.CLIENT_CREDENTIALS_GENERIC_TEXT"/>
	/// or <see cref="Reporting.GenericRecommendationProvider.AUTHORIZATION_CODE_GENERIC_TEXT"/>) when a
	/// High or Medium finding of this language has no template for its flow. Null when the analyzer
	/// declares none, and a flow absent from a declared dictionary keeps the base's text for that flow.
	///
	/// A DICTIONARY KEYED BY FLOW, not two properties, so that a third flow added to
	/// <see cref="CredentialFlow"/> is a key rather than a change to the contract's surface.
	/// <see cref="AnalyzerContractValidator"/> rejects a declared value that is empty or
	/// whitespace-only.
	/// </summary>
	public FrozenDictionary<CredentialFlow, string>? LanguageFallbackTextByFlow { get; init; }
}
