namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE THREE VALUES OF A CONDITIONAL-COMPILATION CONDITION, in Kleene's three-valued logic. <see cref="Unknown"/> is the
/// value of a symbol the build can define outside the file, and of every condition that does not parse: it is never
/// treated as false, because only a definite false may switch a branch off.
/// </summary>
public enum ConditionValue
{
	/// <summary>The condition is definitely false.</summary>
	False = 0,

	/// <summary>The condition is definitely true.</summary>
	True = 1,

	/// <summary>The file alone cannot say.</summary>
	Unknown = 2
}
