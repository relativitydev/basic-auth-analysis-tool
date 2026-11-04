namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The Part A and Part C checks of <see cref="PartCDriftTests"/>, run for Shell: every generated block of
/// <c>part-c-shell.md</c> equals what the recommendation composer produces from the embedded Shell catalog.
/// </summary>
public sealed class ShellPartCDriftTests : PartCDriftTests
{
	public ShellPartCDriftTests()
		: base(DocumentedLanguage.Shell)
	{
	}
}
