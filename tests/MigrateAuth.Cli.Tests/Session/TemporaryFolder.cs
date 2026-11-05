using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Tests.Session;

/// <summary>
/// A folder that exists for one test and is removed afterwards. The state store writes real
/// files, and a test that asserts atomic writes and restrictive permissions has to look at real
/// ones.
///
/// <see cref="Path"/> is the CANONICAL form of the folder, resolved the way the product resolves
/// every path it is handed. Where the platform's temporary folder sits behind a link -
/// macOS hands out a path under a folder that is itself a link - the spelling the platform
/// returns and the spelling the product reports are different strings for one folder, and a test
/// that builds an expected or absolute path from the first while the product answers in the
/// second would fail for a reason that has nothing to do with what it asserts.
/// </summary>
internal sealed class TemporaryFolder : IDisposable
{
	private const string _FOLDER_NAME_PREFIX = "migrate-auth-tests-";

	private TemporaryFolder(string path)
	{
		Path = path;
	}

	public string Path { get; }

	/// <summary>The platform's temporary folder in canonical form, which is where every folder here lives.</summary>
	public static string CanonicalRoot => CanonicalPath.Resolve(System.IO.Path.GetTempPath());

	public static TemporaryFolder Create()
	{
		return CreateUnder(System.IO.Path.GetTempPath());
	}

	/// <summary>
	/// A folder beneath <paramref name="parentPath"/>, which may itself be reached through a link.
	/// The folder is created first and resolved afterwards, because resolution adopts the names
	/// the filesystem holds only for components that exist.
	/// </summary>
	public static TemporaryFolder CreateUnder(string parentPath)
	{
		string path = System.IO.Path.Combine(parentPath, $"{_FOLDER_NAME_PREFIX}{Guid.NewGuid():N}");

		Directory.CreateDirectory(path);

		return new TemporaryFolder(CanonicalPath.Resolve(path));
	}

	public string Combine(string fileName)
	{
		return System.IO.Path.Combine(Path, fileName);
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(Path, recursive: true);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
