namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// The Part A and Part C checks of <see cref="PartCDriftTests"/>, run for PowerShell: every generated block of
/// <c>part-c-powershell.md</c> equals what the recommendation composer produces from the embedded PowerShell catalog.
/// </summary>
public sealed class PowerShellPartCDriftTests : PartCDriftTests
{
	public PowerShellPartCDriftTests()
		: base(DocumentedLanguage.PowerShell)
	{
	}
}
