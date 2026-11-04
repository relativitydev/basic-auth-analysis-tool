namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// WHAT MADE A FILE'S DEMOTION SIGNALS FIRE, SPLIT ONLY AS FAR AS THE REPORT'S REASON NEEDS.
///
/// A folder the catalog declares as vendored code is told apart from every other signal, because the
/// report names the two differently. Nothing here changes the level: a file is demoted when either
/// half is set.
/// </summary>
internal readonly record struct FileDemotion
{
	/// <summary>No signal fired, or the evaluation did not finish and its answer is not used.</summary>
	internal static FileDemotion None { get; } = new();

	/// <summary>A folder signal declared as naming vendored code fired.</summary>
	internal bool FiresOnVendoredFolder { get; init; }

	/// <summary>Any other demotion signal fired: a file name, another folder, a generation or content marker.</summary>
	internal bool FiresOtherwise { get; init; }

	internal bool IsDemoted => FiresOnVendoredFolder || FiresOtherwise;
}
