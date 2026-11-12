using System.Text;

namespace MigrateAuth.Cli.Writing;

/// <summary>
/// The one place a file this product writes is created, so that the state file, the report and
/// the data artifact cannot drift apart on how restrictively they are permissioned.
///
/// The two shareable artifacts carry the same permissions as the state file, because with the
/// literal-credential signal they are a precise index of where a repository's embedded
/// credentials are.
///
/// Restrictive permissions are applied where the platform expresses them as a file mode. On
/// Windows nothing is applied and the file inherits the destination folder's access control
/// list, so a destination others can read yields artifacts others can read, while they stay
/// user-only everywhere else.
/// </summary>
public static class RestrictiveFileCreation
{
	/// <summary>Owner read and write, and nothing for anybody else.</summary>
	public const UnixFileMode RESTRICTIVE_UNIX_FILE_MODE = UnixFileMode.UserRead | UnixFileMode.UserWrite;

	/// <summary>Owner read, write and traverse, and nothing for anybody else.</summary>
	public const UnixFileMode RESTRICTIVE_UNIX_DIRECTORY_MODE =
		UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

	private static readonly UTF8Encoding _utf8WithoutByteOrderMark = new(encoderShouldEmitUTF8Identifier: false);

	/// <summary>
	/// Writes text to a file created under the supplied mode with restrictive permissions,
	/// UTF-8 and no byte order mark.
	///
	/// IT REQUIRES THE WHOLE TEXT UP FRONT, which is correct for small content and wrong for the
	/// report and the data artifact. Those two go through the two openers below instead.
	/// </summary>
	public static void WriteText(string path, string text, FileMode creationMode)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		ArgumentNullException.ThrowIfNull(text);

		using StreamWriter writer = CreateTextWriter(path, creationMode);

		writer.Write(text);
	}

	/// <summary>
	/// Opens a writer over a file created under the supplied mode with the SAME permissions,
	/// encoding and sharing rules as <see cref="WriteText"/>, and without demanding the content
	/// in advance.
	///
	/// It exists because the artifact is written streaming rather than serialized to a string
	/// first. It is the writer <see cref="WriteText"/> constructs internally, exposed so that a
	/// caller can write into it incrementally. The caller owns the returned writer and disposes it.
	/// </summary>
	public static StreamWriter CreateTextWriter(string path, FileMode creationMode)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		return new StreamWriter(path, _utf8WithoutByteOrderMark, BuildWriteOptions(creationMode));
	}

	/// <summary>
	/// The same opener one layer lower, for a consumer that encodes its own bytes.
	///
	/// The data artifact is that consumer: its serializer writes UTF-8 bytes directly and
	/// wrapping it in a character writer would decode and re-encode every byte of a file whose
	/// whole point is that it is large. The permissions, the creation mode and the sharing rule
	/// are the ones above, which is what keeps the two artifacts from drifting apart on how
	/// restrictively they are created.
	/// </summary>
	public static FileStream CreateStream(string path, FileMode creationMode)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		return new FileStream(path, BuildWriteOptions(creationMode));
	}

	/// <summary>
	/// A character writer over a stream the caller already opened, with the encoding every text artifact
	/// here uses (UTF-8, no byte order mark) and the default newline, so the bytes are the ones
	/// <see cref="CreateTextWriter"/> would produce. It exists so a hashing stream can sit between the
	/// writer and the file. The caller owns the returned writer, which disposes the stream.
	/// </summary>
	public static StreamWriter CreateTextWriterOver(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream);

		return new StreamWriter(stream, _utf8WithoutByteOrderMark);
	}

	/// <summary>
	/// Creates a folder, with owner-only access (<c>0700</c>) where the platform expresses it as a mode.
	/// On Windows nothing is applied and the folder inherits its parent's access control list, the same
	/// declared divergence as for files. An existing folder is left as it is.
	/// </summary>
	public static DirectoryInfo CreateDirectory(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		DirectoryInfo created = OperatingSystem.IsWindows()
			? Directory.CreateDirectory(path)
			: Directory.CreateDirectory(path, RESTRICTIVE_UNIX_DIRECTORY_MODE);

		return created;
	}

	private static FileStreamOptions BuildWriteOptions(FileMode creationMode)
	{
		FileStreamOptions writeOptions = new()
		{
			Mode = creationMode,
			Access = FileAccess.Write,
			Share = FileShare.None
		};

		if (!OperatingSystem.IsWindows())
		{
			writeOptions.UnixCreateMode = RESTRICTIVE_UNIX_FILE_MODE;
		}

		return writeOptions;
	}
}
