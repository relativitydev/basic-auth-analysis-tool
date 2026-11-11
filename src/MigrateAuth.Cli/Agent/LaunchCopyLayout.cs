namespace MigrateAuth.Cli.Agent;

/// <summary>
/// WHERE THE LAUNCH COPY OF THE PROMPT PACK LIVES, AND WHAT ITS PATH WILL LOOK LIKE BEFORE IT EXISTS.
///
/// The launch copy goes in its own folder under the documentation destination, named
/// <c>ai-assist-launch-</c> followed by 32 hexadecimal digits. The digits are the only part that varies,
/// and they have a fixed length and a fixed alphabet, so the path a copy WILL have and the path it DOES
/// have contain exactly the same set of characters apart from those digits. That is what lets the check
/// for control characters and for the wrapper character allow-list run before the user is offered the
/// launch, on the predicted path, and hold for the real one.
///
/// The folder is never removed by the tool.
/// </summary>
public static class LaunchCopyLayout
{
	/// <summary>The fixed start of every launch folder name.</summary>
	public const string FOLDER_NAME_PREFIX = "ai-assist-launch-";

	/// <summary>How many hexadecimal digits follow the prefix.</summary>
	public const int IDENTIFIER_LENGTH = 32;

	/// <summary>The identifier used in a predicted path: all digits are the same, and they are valid hexadecimal.</summary>
	private const char _PREDICTED_IDENTIFIER_DIGIT = '0';

	private const string _IDENTIFIER_FORMAT = "N";

	private const UnixFileMode _OWNER_ONLY_FOLDER_MODE =
		UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

	/// <summary>A new, unique launch folder name.</summary>
	public static string NewFolderName()
	{
		return $"{FOLDER_NAME_PREFIX}{Guid.NewGuid().ToString(_IDENTIFIER_FORMAT)}";
	}

	/// <summary>
	/// The launch folder path a copy under <paramref name="destinationFolderPath"/> will have, with a
	/// placeholder identifier of the same length and alphabet as a real one.
	/// </summary>
	public static string PredictFolderPath(string destinationFolderPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationFolderPath);

		return Path.Combine(
			destinationFolderPath,
			$"{FOLDER_NAME_PREFIX}{new string(_PREDICTED_IDENTIFIER_DIGIT, IDENTIFIER_LENGTH)}");
	}

	/// <summary>The path of the copy of a pack with the given file name, as it will be.</summary>
	public static string PredictCopyPath(string destinationFolderPath, string packFileName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(packFileName);

		return Path.Combine(PredictFolderPath(destinationFolderPath), packFileName);
	}

	/// <summary>
	/// Whether <paramref name="folderName"/> has the shape of a launch folder name: the prefix and then
	/// exactly the identifier length of lower-case hexadecimal digits.
	/// </summary>
	public static bool HasLaunchFolderShape(string folderName)
	{
		bool hasShape = folderName.Length == FOLDER_NAME_PREFIX.Length + IDENTIFIER_LENGTH
			&& folderName.StartsWith(FOLDER_NAME_PREFIX, StringComparison.Ordinal);

		for (int position = FOLDER_NAME_PREFIX.Length; hasShape && position < folderName.Length; position++)
		{
			hasShape = char.IsAsciiHexDigitLower(folderName[position]);
		}

		return hasShape;
	}

	/// <summary>
	/// Creates a new launch folder under the destination and returns its path. On Linux the folder is
	/// readable and writable by its owner only.
	/// </summary>
	public static string NewFolder(string destinationFolderPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationFolderPath);

		string folderPath = Path.Combine(destinationFolderPath, NewFolderName());

		if (OperatingSystem.IsWindows())
		{
			Directory.CreateDirectory(folderPath);
		}
		else
		{
			Directory.CreateDirectory(folderPath, _OWNER_ONLY_FOLDER_MODE);
		}

		return folderPath;
	}
}
