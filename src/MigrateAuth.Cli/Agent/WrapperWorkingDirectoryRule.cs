namespace MigrateAuth.Cli.Agent;

/// <summary>
/// WHERE A WINDOWS SCRIPT WRAPPER MAY START, defined positively.
///
/// The command interpreter does not accept a network path as its current directory: it prints that such
/// paths are not supported and runs in the Windows folder instead, so an agent started through it would
/// review the wrong folder. A wrapper launch is therefore offered only when the working directory is a
/// local drive-letter path within the interpreter's length limit: with a leading <c>\\?\</c> removed when
/// a drive letter follows it, the path starts with a letter, a colon and a backslash, and is no longer than
/// <see cref="MAXIMUM_CURRENT_DIRECTORY_LENGTH"/> characters. Every other form (a share, the long-path form of
/// a share, a volume name, a path that is too long) is refused. A direct program launch is not affected.
/// </summary>
public static class WrapperWorkingDirectoryRule
{
	/// <summary>
	/// The longest working directory, in characters, for which the command interpreter starts and reports
	/// the folder it was given. Measured with the interpreter's own current-directory command: 258
	/// characters starts and is reported unchanged, and 259 is refused by process creation itself.
	/// </summary>
	public const int MAXIMUM_CURRENT_DIRECTORY_LENGTH = 258;

	private const string _LONG_PATH_PREFIX = @"\\?\";
	private const int _DRIVE_PREFIX_LENGTH = 3;
	private const int _DRIVE_LETTER_POSITION = 0;
	private const int _DRIVE_COLON_POSITION = 1;
	private const int _DRIVE_SEPARATOR_POSITION = 2;
	private const char _DRIVE_COLON = ':';
	private const char _DRIVE_SEPARATOR = '\\';

	/// <summary>Whether a wrapper may be started with <paramref name="workingDirectory"/> as its working directory.</summary>
	public static bool AllowsWrapperLaunch(string workingDirectory)
	{
		ArgumentNullException.ThrowIfNull(workingDirectory);

		string path = StripLongPathPrefix(workingDirectory);

		return path.Length >= _DRIVE_PREFIX_LENGTH
			&& path.Length <= MAXIMUM_CURRENT_DIRECTORY_LENGTH
			&& char.IsAsciiLetter(path[_DRIVE_LETTER_POSITION])
			&& path[_DRIVE_COLON_POSITION] == _DRIVE_COLON
			&& path[_DRIVE_SEPARATOR_POSITION] == _DRIVE_SEPARATOR;
	}

	private static string StripLongPathPrefix(string workingDirectory)
	{
		string remainder = workingDirectory.StartsWith(_LONG_PATH_PREFIX, StringComparison.Ordinal)
			? workingDirectory[_LONG_PATH_PREFIX.Length..]
			: workingDirectory;
		bool driveLetterFollows = remainder.Length >= _DRIVE_PREFIX_LENGTH
			&& char.IsAsciiLetter(remainder[_DRIVE_LETTER_POSITION])
			&& remainder[_DRIVE_COLON_POSITION] == _DRIVE_COLON;

		return remainder.Length != workingDirectory.Length && !driveLetterFollows
			? workingDirectory
			: remainder;
	}
}
