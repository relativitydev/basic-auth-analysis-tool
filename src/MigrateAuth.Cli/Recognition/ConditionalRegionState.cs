namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE THREE STATES OF A CONDITIONAL-COMPILATION BRANCH. Only <see cref="Dead"/> proposes Low: a branch that may or may
/// not be taken is as reportable as the code outside any directive.
/// </summary>
public enum ConditionalRegionState
{
	/// <summary>The branch is definitely taken.</summary>
	Live = 0,

	/// <summary>The branch may or may not be taken.</summary>
	Maybe = 1,

	/// <summary>The branch is definitely not taken, by the file's own symbols.</summary>
	Dead = 2
}
