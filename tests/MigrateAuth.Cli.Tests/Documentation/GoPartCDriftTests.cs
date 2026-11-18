namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The Part A and Part C checks of <see cref="PartCDriftTests"/>, run for Go: every generated block of <c>part-c-go.md</c>, the generic blocks of
/// each flow included, equals what the recommendation composer produces from the embedded Go catalog. The lead sentence of each class entry is written by
/// hand outside the markers, so the regeneration never touches it.
/// </summary>
public sealed class GoPartCDriftTests : PartCDriftTests
{
	public GoPartCDriftTests()
		: base(DocumentedLanguage.Go)
	{
	}
}
