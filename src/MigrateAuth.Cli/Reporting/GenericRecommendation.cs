using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE SEAM BETWEEN THIS CODE AND WHATEVER OWNS THE GENERIC RECOMMENDATION.
///
/// The STATIC TEMPLATES are catalog content, indexed by pattern class, and rendering one is a
/// mechanical substitution this code performs. The GENERIC RECOMMENDATION is the text a client
/// reads for every pattern the catalog does not cover, and it is supplied by a provider.
///
/// So this code calls a provider and does not implement one: a High or Medium finding whose class
/// has no template produces whatever the injected provider says and nothing this file invented.
///
/// WHY AN INTERFACE AND NOT A CONSTANT. A constant here would have to hold text, and the only
/// text available to hold would be text this code made up - which is the outcome the split exists
/// to avoid. An interface holds no text at all, so the boundary is visible in the type system
/// rather than maintained by a comment.
/// </summary>
public interface IGenericRecommendationProvider
{
	/// <summary>
	/// The generic migration recommendation for a High or Medium finding whose pattern class carries
	/// no template for its flow.
	///
	/// It receives the flow and no finding: the single composition point is finding-free,
	/// so that the report, the JSON artifact, the generated Part C blocks and the User Guide all compose
	/// the same text from (language, pattern class, flow). The text may carry no text from the
	/// analyzed file, which the finding-free signature makes structural: nothing is passed in that
	/// could leak.
	/// </summary>
	string DescribeFor(CredentialFlow flow);
}
