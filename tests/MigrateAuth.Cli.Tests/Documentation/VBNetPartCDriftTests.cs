namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The Part A and Part C checks of <see cref="PartCDriftTests"/>, run for VB.NET: every generated block of
/// <c>part-c-vbnet.md</c> equals what the recommendation composer produces from the embedded VBNet catalog.
/// </summary>
public sealed class VBNetPartCDriftTests : PartCDriftTests
{
	public VBNetPartCDriftTests()
		: base(DocumentedLanguage.VBNet)
	{
	}
}
