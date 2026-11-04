using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// WHICH KINDS OF DEMOTION SIGNAL FIRED ON A FILE.
///
/// Every signal is evaluated, so this is the complete set of kinds the file's name, folders and
/// content declare. The engine reads the level and the reasons from it: Test alone proposes Medium,
/// any other kind proposes Low.
/// </summary>
internal readonly record struct FileDemotion
{
	/// <summary>No signal fired, or the evaluation did not finish and its answer is not used.</summary>
	internal static FileDemotion None { get; } = new();

	/// <summary>The kinds of every signal that fired on the file.</summary>
	internal DemotionKinds FiredKinds { get; init; }
}
