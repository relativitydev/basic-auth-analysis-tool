using MigrateAuth.Cli.Findings;

// This stand-in lives in the production namespace so that the many fixtures which render a
// report without caring about the generic recommendation's text keep reading it by the same name.
namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// A test double for <see cref="IGenericRecommendationProvider"/>: it produces a marker that
/// cannot be mistaken for advice, for fixtures that need a provider but do not assert on the
/// generic recommendation's real text. The shipped provider is <see cref="GenericRecommendationProvider"/>.
/// </summary>
public sealed class UnwiredGenericRecommendationProvider : IGenericRecommendationProvider
{
	/// <summary>Said in place of the generic recommendation.</summary>
	public const string PLACEHOLDER_TEXT =
		"[no generic recommendation wired yet. This build recognized the construct but carries no template for its pattern class, and the text that belongs here has not been written.]";

	/// <inheritdoc />
	public string DescribeFor(CredentialFlow flow)
	{
		return PLACEHOLDER_TEXT;
	}
}
