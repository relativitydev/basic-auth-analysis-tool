using System.Text;

using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

using MigrateAuth.Cli.Tests.Writing;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// A REAL TREE ON A REAL FILESYSTEM, because every claim these tests make is about what the
/// operating system does when it is asked.
///
/// The walk's whole reason for existing is that a denial behaves differently from a success, and
/// a mocked filesystem would be a model of the behaviour under test rather than the behaviour.
/// So the fixture plants the conditions - a denied file, a denied directory, a link, an oversize
/// file, an over-long line, a two-byte encoding, an excluded folder name, a clean file - and the
/// tests reconcile what the walk reported against what was planted.
/// </summary>
internal sealed class RepositoryFixture : IDisposable
{
	internal const string CLEAN_FILE_NAME = "clean.cs";
	internal const string CARRIAGE_RETURN_FILE_NAME = "carriage-returns.cs";
	internal const string DENIED_FILE_NAME = "denied.cs";
	internal const string DENIED_DIRECTORY_NAME = "denied-directory";
	internal const string LINKED_DIRECTORY_NAME = "link-to-target";
	/// <summary>
	/// NOT "target", which is on the exclusion list - naming it that would have hidden this
	/// folder from the walk and made the link test pass for the wrong reason.
	/// </summary>
	internal const string TARGET_DIRECTORY_NAME = "link-target";
	internal const string TARGET_FILE_NAME = "reached-without-the-link.cs";
	internal const string OVERSIZE_FILE_NAME = "oversize.js";
	internal const string OVER_LONG_LINE_FILE_NAME = "minified.js";
	internal const string TWO_BYTE_FILE_NAME = "markless-two-byte.ps1";
	internal const string BINARY_FILE_NAME = "compiled.bin";
	internal const string EXCLUDED_DIRECTORY_NAME = "node_modules";
	internal const string EXCLUDED_FILE_NAME = "bundle.js";

	/// <summary>
	/// Real content in the two-byte file rather than filler, so the test can prove the text came
	/// back CORRECTLY rather than merely prove the encoding was noticed. Silent zero recall is
	/// what happens when a file is detected and then read as garbage.
	/// </summary>
	internal const string TWO_BYTE_FILE_CONTENT = "$headers = @{ Authorization = 'Basic ' + $encoded }\n";

	internal const string CLEAN_FILE_CONTENT = "public sealed class Ordinary\n{\n}\n";

	private const string _FOLDER_NAME_PREFIX = "migrate-auth-walk-";
	private const int _OVERSIZE_MARGIN_IN_BYTES = 1024;
	private const int _OVER_LONG_LINE_MARGIN_IN_CHARACTERS = 1;
	private const char _FILLER_CHARACTER = 'a';
	private const string _LINE_TERMINATOR = "\n";

	private readonly List<string> _deniedPaths = [];

	private RepositoryFixture(string rootPath)
	{
		RootPath = rootPath;
		Repository = RepositoryFolderPath.FromSuppliedText(rootPath);
		Ledger = new SkippedItemLedger();
		EmissionPoint = new EmissionPoint([Ledger]);
	}

	internal string RootPath { get; }

	internal RepositoryFolderPath Repository { get; }

	internal SkippedItemLedger Ledger { get; }

	internal EmissionPoint EmissionPoint { get; }

	internal static RepositoryFixture CreateEmpty()
	{
		return CreateEmptyUnder(Path.GetTempPath());
	}

	/// <summary>
	/// The root is held in CANONICAL form, resolved the way the product resolves the repository,
	/// so every absolute path a test builds from it is spelled as the product spells it.
	/// <paramref name="temporaryParentPath"/> may be reached through a link, which is how a test
	/// reproduces a platform whose temporary folder is one.
	/// </summary>
	internal static RepositoryFixture CreateEmptyUnder(string temporaryParentPath)
	{
		string rootPath = Path.Combine(temporaryParentPath, $"{_FOLDER_NAME_PREFIX}{Guid.NewGuid():N}");

		Directory.CreateDirectory(rootPath);

		return new RepositoryFixture(CanonicalPath.Resolve(rootPath));
	}

	/// <summary>Every planted condition, in one tree.</summary>
	internal static RepositoryFixture CreateWithEveryCondition()
	{
		RepositoryFixture fixture = CreateEmpty();

		fixture.WriteText(CLEAN_FILE_NAME, CLEAN_FILE_CONTENT);
		fixture.WriteText(CARRIAGE_RETURN_FILE_NAME, "first\r\nsecond\rthird\n");
		fixture.PlantOversizeFile();
		fixture.PlantOverLongLine();
		fixture.PlantMarklessTwoByteFile();
		fixture.PlantBinaryFile();
		fixture.PlantExcludedDirectory();
		fixture.PlantLinkedDirectory();
		fixture.PlantDeniedFile();
		fixture.PlantDeniedDirectory();

		return fixture;
	}

	internal string WriteText(string relativePath, string text)
	{
		return WriteBytes(relativePath, Encoding.UTF8.GetBytes(text));
	}

	internal string WriteBytes(string relativePath, byte[] bytes)
	{
		string absolutePath = Path.Combine(RootPath, relativePath);

		Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
		File.WriteAllBytes(absolutePath, bytes);

		return absolutePath;
	}

	internal string CreateFolder(string relativePath)
	{
		string absolutePath = Path.Combine(RootPath, relativePath);

		Directory.CreateDirectory(absolutePath);

		return absolutePath;
	}

	/// <summary>
	/// Takes read access away and confirms it went. A fixture whose precondition quietly failed
	/// is a test that reports success for an unexercised claim.
	/// </summary>
	internal void Deny(string absolutePath)
	{
		bool isDirectory = Directory.Exists(absolutePath);

		AccessDenial.Apply(absolutePath);
		_deniedPaths.Add(absolutePath);
		AccessDenial.ConfirmUnreadable(
			isDirectory ? () => CanListDirectory(absolutePath) : () => CanOpenFile(absolutePath));
	}

	/// <summary>Walks the fixture and reads every file it reaches, the way a stage would.</summary>
	internal List<FileReadResult> WalkAndRead()
	{
		RepositoryWalk walk = new(Repository, EmissionPoint);
		RepositoryFileReader reader = new(EmissionPoint);

		return [.. walk.Enumerate().Select(reader.Read)];
	}

	/// <summary>The repository-relative paths the walk reached, in a stable order.</summary>
	internal List<string> WalkedPaths()
	{
		RepositoryWalk walk = new(Repository, EmissionPoint);

		return [.. walk.Enumerate().Select(file => file.Path.Value).Order(StringComparer.Ordinal)];
	}

	public void Dispose()
	{
		foreach (string deniedPath in _deniedPaths)
		{
			TryRevoke(deniedPath);
		}

		try
		{
			Directory.Delete(RootPath, recursive: true);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	private static void TryRevoke(string deniedPath)
	{
		try
		{
			AccessDenial.Revoke(deniedPath);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	private void PlantOversizeFile()
	{
		WriteBytes(
			OVERSIZE_FILE_NAME,
			new byte[ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES + _OVERSIZE_MARGIN_IN_BYTES]);
	}

	/// <summary>
	/// A small file whose longest line is one character past the ceiling, and the over-long line
	/// is the LAST one - the ceiling has to catch a file it only breaches near the end, which is
	/// the case a check that gave up early would miss.
	/// </summary>
	private void PlantOverLongLine()
	{
		string overLongLine = new(
			_FILLER_CHARACTER,
			ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS + _OVER_LONG_LINE_MARGIN_IN_CHARACTERS);

		WriteText(OVER_LONG_LINE_FILE_NAME, $"short{_LINE_TERMINATOR}{overLongLine}{_LINE_TERMINATOR}");
	}

	/// <summary>
	/// Two bytes per character, low byte first, and NO byte order mark - which is the case the
	/// retry exists for and the case strict UTF-8 silently accepts.
	/// </summary>
	private void PlantMarklessTwoByteFile()
	{
		WriteBytes(TWO_BYTE_FILE_NAME, new UnicodeEncoding(bigEndian: false, byteOrderMark: false)
			.GetBytes(TWO_BYTE_FILE_CONTENT));
	}

	/// <summary>Nulls in a shape no two-byte encoding produces: dense, but of no single parity.</summary>
	private void PlantBinaryFile()
	{
		byte[] bytes = new byte[512];

		for (int offset = 0; offset < bytes.Length; offset += 3)
		{
			bytes[offset] = 0x7F;
		}

		WriteBytes(BINARY_FILE_NAME, bytes);
	}

	private void PlantExcludedDirectory()
	{
		WriteText(Path.Combine(EXCLUDED_DIRECTORY_NAME, EXCLUDED_FILE_NAME), "var generated = 1;\n");
	}

	private void PlantLinkedDirectory()
	{
		WriteText(Path.Combine(TARGET_DIRECTORY_NAME, TARGET_FILE_NAME), CLEAN_FILE_CONTENT);
		DirectoryLink.Create(Path.Combine(RootPath, LINKED_DIRECTORY_NAME), Path.Combine(RootPath, TARGET_DIRECTORY_NAME));
	}

	private void PlantDeniedFile()
	{
		Deny(WriteText(DENIED_FILE_NAME, CLEAN_FILE_CONTENT));
	}

	private void PlantDeniedDirectory()
	{
		WriteText(Path.Combine(DENIED_DIRECTORY_NAME, CLEAN_FILE_NAME), CLEAN_FILE_CONTENT);
		Deny(Path.Combine(RootPath, DENIED_DIRECTORY_NAME));
	}

	private static bool CanOpenFile(string absolutePath)
	{
		bool opened = ReadOnlyFileOpen.Attempt(absolutePath, out FileStream? stream) is FileOpenResult.Opened;

		stream?.Dispose();

		return opened;
	}

	private static bool CanListDirectory(string absolutePath)
	{
		bool listed;

		try
		{
			_ = new DirectoryInfo(absolutePath).GetFileSystemInfos();
			listed = true;
		}
		catch (UnauthorizedAccessException)
		{
			listed = false;
		}
		catch (IOException)
		{
			listed = false;
		}

		return listed;
	}
}
