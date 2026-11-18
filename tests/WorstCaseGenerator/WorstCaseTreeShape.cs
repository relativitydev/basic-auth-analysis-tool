namespace MigrateAuth.WorstCase;

/// <summary>
/// Where the files of the languages a repository vendors are placed. The default places every file in the plain three-level layout, so a tree asked for nothing else is the tree the earlier scenarios
/// recorded, byte for byte; the other shapes move only the files of the kinds they name and leave every other file where it was.
/// </summary>
public enum WorstCaseTreeShape
{
	/// <summary>Every file stands in the plain three-level layout.</summary>
	Standard = 0,

	/// <summary>
	/// The Python files stand where a virtual environment keeps them: the sources and stubs under <c>.venv/lib/python3.12/site-packages/&lt;package&gt;/&lt;module&gt;</c>, and the extensionless scripts with a
	/// <c>python</c> shebang under <c>.venv/bin</c>. The product walks a virtual-environment folder, so these files are read.
	/// </summary>
	VirtualEnvironment = 1,

	/// <summary>
	/// A vendored tree: the Python files under <c>third_party/python/&lt;package&gt;/&lt;module&gt;</c> and the Go files in the layout of a vendored Go module,
	/// <c>vendor/&lt;module path&gt;/&lt;package&gt;</c>.
	/// </summary>
	Vendored = 2
}
