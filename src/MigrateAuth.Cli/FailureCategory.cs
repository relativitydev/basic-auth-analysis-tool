namespace MigrateAuth.Cli;

/// <summary>
/// The coarse classification the session exception boundary reports alongside the
/// exception type. It exists so that a failure line says what kind of thing went wrong
/// without quoting the framework's own message, which cannot be emitted because framework
/// I/O messages carry an absolute path by construction.
/// </summary>
public enum FailureCategory
{
	/// <summary>No category could be established for this exception type.</summary>
	Unknown = 0,

	/// <summary>Reading, traversing or opening a filesystem entry failed.</summary>
	FileAccess = 1,

	/// <summary>The operation was cancelled before it completed.</summary>
	Cancellation = 2,

	/// <summary>A value supplied to the operation was absent, malformed or out of range.</summary>
	InvalidInput = 3,

	/// <summary>The operation was attempted against a state that does not permit it.</summary>
	InvalidState = 4,

	/// <summary>A resource the process depends on was exhausted.</summary>
	ResourceExhaustion = 5
}
