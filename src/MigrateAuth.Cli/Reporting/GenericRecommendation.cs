using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// The seam between the report renderers and the provider of the generic recommendation.
///
/// Static templates are catalog content, indexed by pattern class, and rendering one is a mechanical
/// substitution this code performs. The generic recommendation is the text read for every construct
/// the catalog does not cover, and its content comes from a provider: a High or Medium finding whose
/// class has no template produces whatever the injected provider says. An interface rather than a
/// constant keeps that boundary visible in the type system.
/// </summary>
public interface IGenericRecommendationProvider
{
	/// <summary>
	/// The generic migration recommendation for a High or Medium finding whose pattern class carries
	/// no template for its flow.
	///
	/// It receives the flow and no finding: the single composition point is finding-free, so that the
	/// report, the JSON artifact, the generated Part C blocks and the User Guide all compose the same text
	/// from (language, pattern class, flow). The text may carry no text from the analyzed file, which the
	/// finding-free signature makes structural: nothing is passed in that could leak.
	/// </summary>
	string DescribeFor(CredentialFlow flow);
}

/// <summary>
/// A placeholder provider that produces a marker rather than text that could be mistaken for advice.
/// A generic recommendation that reads like a specific one invites a developer to apply a
/// transformation the tool never verified applies; nobody acts on a marker.
/// </summary>
public sealed class UnwiredGenericRecommendationProvider : IGenericRecommendationProvider
{
	/// <summary>
	/// Said in place of the generic recommendation, stating that the real text is not written yet
	/// so that a reader of a report produced by an intermediate build knows what is missing rather
	/// than being left to guess.
	/// </summary>
	public const string PLACEHOLDER_TEXT =
		"[no generic recommendation wired yet. This build recognized the construct but carries no template for its pattern class, and the text that belongs here has not been written.]";

	/// <inheritdoc />
	public string DescribeFor(CredentialFlow flow)
	{
		return PLACEHOLDER_TEXT;
	}
}
