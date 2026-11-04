using MigrateAuth.Cli.Tests.Session;
using MigrateAuth.Cli.Writing;

using Xunit;

namespace MigrateAuth.Cli.Tests.Writing;

/// <summary>
/// The one place a produced file is created: UTF-8 without a byte order mark, never shared
/// while it is being written, honouring the creation mode, and owner-only wherever the platform
/// expresses permissions as a file mode.
/// </summary>
public sealed class RestrictiveFileCreationTests
{
	private const string _FILE_NAME = "artifact.txt";
	private const string _BODY = "line one\nline two with é\n";
	private const string _REPLACEMENT_BODY = "replaced\n";

	[Fact]
	public void WriteTextCreatesTheFileWithTheTextAsUtf8WithoutAByteOrderMark()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		string path = folder.Combine(_FILE_NAME);

		RestrictiveFileCreation.WriteText(path, _BODY, FileMode.CreateNew);

		byte[] bytes = File.ReadAllBytes(path);
		Assert.Equal(_BODY, System.Text.Encoding.UTF8.GetString(bytes));
		Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(_BODY), bytes.Length);
		Assert.False(bytes.AsSpan().StartsWith(System.Text.Encoding.UTF8.Preamble));
	}

	[Fact]
	public void CreateNewRefusesToOverwriteAnExistingFile()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		string path = folder.Combine(_FILE_NAME);
		RestrictiveFileCreation.WriteText(path, _BODY, FileMode.CreateNew);

		Assert.Throws<IOException>(() => RestrictiveFileCreation.WriteText(path, _REPLACEMENT_BODY, FileMode.CreateNew));

		Assert.Equal(_BODY, File.ReadAllText(path));
	}

	[Fact]
	public void CreateReplacesAnExistingFile()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		string path = folder.Combine(_FILE_NAME);
		RestrictiveFileCreation.WriteText(path, _BODY, FileMode.CreateNew);

		RestrictiveFileCreation.WriteText(path, _REPLACEMENT_BODY, FileMode.Create);

		Assert.Equal(_REPLACEMENT_BODY, File.ReadAllText(path));
	}

	[Fact]
	public void AWriterOpenedOverAFileHoldsItExclusivelyUntilDisposed()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		string path = folder.Combine(_FILE_NAME);

		using (StreamWriter writer = RestrictiveFileCreation.CreateTextWriter(path, FileMode.CreateNew))
		{
			writer.Write(_BODY);
			writer.Flush();

			Assert.Throws<IOException>(() => File.OpenRead(path));
		}

		Assert.Equal(_BODY, File.ReadAllText(path));
	}

	[Fact]
	public void AStreamOpenedOverAFileWritesRawBytesAndHoldsItExclusivelyUntilDisposed()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		string path = folder.Combine(_FILE_NAME);
		byte[] payload = [1, 2, 3, 4];

		using (FileStream stream = RestrictiveFileCreation.CreateStream(path, FileMode.CreateNew))
		{
			stream.Write(payload);
			stream.Flush();

			Assert.False(stream.CanRead);
			Assert.Throws<IOException>(() => File.OpenRead(path));
		}

		Assert.Equal(payload, File.ReadAllBytes(path));
	}

	[Fact]
	public void WhereThePlatformExpressesFileModesTheFileIsOwnerReadWriteOnly()
	{
		using TemporaryFolder folder = TemporaryFolder.Create();
		string path = folder.Combine(_FILE_NAME);

		RestrictiveFileCreation.WriteText(path, _BODY, FileMode.CreateNew);

		Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, RestrictiveFileCreation.RESTRICTIVE_UNIX_FILE_MODE);

		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(RestrictiveFileCreation.RESTRICTIVE_UNIX_FILE_MODE, File.GetUnixFileMode(path));
		}
	}

	[Fact]
	public void ABlankPathIsRefused()
	{
		Assert.Throws<ArgumentException>(() => RestrictiveFileCreation.WriteText(" ", _BODY, FileMode.CreateNew));
		Assert.Throws<ArgumentException>(() => RestrictiveFileCreation.CreateTextWriter(string.Empty, FileMode.CreateNew));
		Assert.Throws<ArgumentException>(() => RestrictiveFileCreation.CreateStream(" ", FileMode.CreateNew));
		Assert.Throws<ArgumentNullException>(() => RestrictiveFileCreation.WriteText(_FILE_NAME, null!, FileMode.CreateNew));
	}
}
