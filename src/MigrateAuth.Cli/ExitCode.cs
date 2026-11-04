namespace MigrateAuth.Cli;

/// <summary>
/// The complete process exit-code enumeration. These values are stable across releases:
/// callers and scripts depend on them, so a number is never reassigned to a new meaning.
/// </summary>
public enum ExitCode
{
	/// <summary>
	/// The session completed successfully. A completed run that produced zero findings is
	/// a success and reports this code.
	/// </summary>
	Success = 0,

	/// <summary>
	/// The repository path is unusable: it does not exist, is not a folder, or is entirely
	/// inaccessible.
	/// </summary>
	UnusableRepositoryPath = 1,

	/// <summary>
	/// A stage was invoked before its prerequisite stage had completed.
	/// </summary>
	SequentialGatingViolation = 2,

	/// <summary>
	/// The command entered is not recognized.
	/// </summary>
	UnrecognizedCommand = 3,

	/// <summary>
	/// The output destination cannot be written to.
	/// </summary>
	OutputDestinationNotWritable = 4,

	/// <summary>
	/// An unexpected internal error occurred. Produced by the session exception boundary
	/// for every failure no other code describes.
	/// </summary>
	UnexpectedInternalError = 5,

	/// <summary>
	/// Pre-flight Validation refused to start a session because the effective configuration
	/// enables none of the eight languages.
	/// </summary>
	NoLanguageEnabled = 6
}
