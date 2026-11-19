using System.Globalization;

namespace MigrateAuth.WorstCase;

/// <summary>One planned file: where it goes and what it is for.</summary>
public readonly record struct WorstCaseFilePlan(long Index, string RelativePath, WorstCaseFileKind Kind);

/// <summary>
/// The shape of the worst-case tree, as a pure function of the options and a file index.
///
/// Layout: three folder levels and a file, a hundred files to a leaf folder and a hundred leaf
/// folders to a middle folder, every name zero-padded to a fixed width. Fixed-width names make the
/// ordinal path order equal the index order at any file count, which is what places the heavy files
/// first in the order the run admits findings in.
///
/// Mix, in basis points of the files after the heavy ones (a file's kind is decided by a seeded hash
/// of its index): 5% files with a few High constructs, 1% undecodable, 0.5% with an over-long line,
/// 10% of a type no analyzer claims, the rest ordinary C#. The first file after the heavy ones always
/// holds a High construct, so a tree larger than the heavy files always has one past the global cap.
///
/// Python and Go, and the tree shapes, only when asked: <see cref="WorstCaseTreeOptions.PythonSourceBasisPoints"/> and <see cref="WorstCaseTreeOptions.GoSourceBasisPoints"/> turn their shares of the ordinary files into Python and Go files on streams drawn after
/// every other share, and <see cref="WorstCaseTreeOptions.TreeShape"/> moves those files into a virtual-environment or a vendored layout; with all three at their defaults the tree is the one every earlier scenario recorded.
///
/// PowerShell, only when asked: <see cref="WorstCaseTreeOptions.PowerShellScriptBasisPoints"/> turns that share of the ordinary files into PowerShell
/// scripts, on streams of their own, so no other file's kind changes and a tree asked for no PowerShell is the same tree it always was.
/// </summary>
public static class WorstCaseTreePlanner
{
	internal const int FILES_PER_LEAF_FOLDER = 100;
	internal const int LEAF_FOLDERS_PER_MIDDLE_FOLDER = 100;
	internal const int BASIS_POINT_DENOMINATOR = 10_000;
	internal const int STREAM_KIND = 1;

	private const int _HIGH_TAIL_THRESHOLD = 500;
	private const int _UNDECODABLE_THRESHOLD = 600;
	private const int _OVER_LONG_LINE_THRESHOLD = 650;
	private const int _NON_CANDIDATE_THRESHOLD = 1_650;
	private const string _TOP_FOLDER_FORMAT = "t{0:D3}";
	private const string _MIDDLE_FOLDER_FORMAT = "m{0:D2}";
	private const string _LEAF_FOLDER_FORMAT = "s{0:D2}";
	private const string _FILE_FORMAT = "f{0:D7}{1}";
	private const string _PATH_SEPARATOR = "/";
	private const string _CANDIDATE_EXTENSION = ".cs";
	private const int _NON_CANDIDATE_EXTENSION_COUNT = 4;
	private const int _STREAM_EXTENSION = 2;
	private const int _STREAM_POWERSHELL_SHARE = 6;
	private const int _STREAM_POWERSHELL_FORM = 7;
	private const int _POWERSHELL_FORM_COUNT = 2;
	private const string _POWERSHELL_SCRIPT_EXTENSION = ".ps1";
	private const int _STREAM_VBNET_SHARE = 8;
	private const string _VBNET_SOURCE_EXTENSION = ".vb";
	private const int _STREAM_SHELL_SHARE = 9;
	private const int _STREAM_SHELL_FORM = 10;
	private const int _SHELL_FORM_COUNT = 2;
	private const string _SHELL_SCRIPT_EXTENSION = ".sh";
	private const int _STREAM_JAVASCRIPT_SHARE = 11;
	private const int _STREAM_JAVASCRIPT_FORM = 12;
	private const int _JAVASCRIPT_FORM_COUNT = 3;
	private const int _JAVASCRIPT_PLAIN_FORM = 0;
	private const int _JAVASCRIPT_JSX_FORM = 1;
	private const string _JAVASCRIPT_SOURCE_EXTENSION = ".js";
	private const string _JAVASCRIPT_JSX_EXTENSION = ".jsx";
	private const int _STREAM_TYPESCRIPT_SHARE = 13;
	private const int _STREAM_TYPESCRIPT_FORM = 14;
	private const string _TYPESCRIPT_SOURCE_EXTENSION = ".ts";
	private const string _TYPESCRIPT_TSX_EXTENSION = ".tsx";
	private const string _TYPESCRIPT_MODULE_EXTENSION = ".mts";
	private const string _TYPESCRIPT_COMMONJS_EXTENSION = ".cts";
	private const string _TYPESCRIPT_DECLARATION_EXTENSION = ".d.ts";
	private const string _TYPESCRIPT_MODULE_DECLARATION_EXTENSION = ".d.mts";
	private const string _TYPESCRIPT_COMMONJS_DECLARATION_EXTENSION = ".d.cts";
	private const string _NO_EXTENSION = "";
	private const int _STREAM_PYTHON_SHARE = 15;
	private const int _STREAM_PYTHON_FORM = 16;
	private const int _STREAM_GO_SHARE = 17;
	private const int _PYTHON_FORM_DENOMINATOR = 10;
	private const int _PYTHON_SHEBANG_FORM_LIMIT = 1;
	private const int _PYTHON_STUB_FORM_LIMIT = 3;
	private const string _PYTHON_SOURCE_EXTENSION = ".py";
	private const string _PYTHON_STUB_EXTENSION = ".pyi";
	private const string _GO_SOURCE_EXTENSION = ".go";
	private const string _VIRTUAL_ENVIRONMENT_FOLDER = ".venv";
	private const string _VIRTUAL_ENVIRONMENT_SCRIPTS_FOLDER = "bin";
	private const string _VIRTUAL_ENVIRONMENT_LIBRARY_FOLDER = "lib";
	private const string _VIRTUAL_ENVIRONMENT_INTERPRETER_FOLDER = "python3.12";
	private const string _VIRTUAL_ENVIRONMENT_PACKAGES_FOLDER = "site-packages";
	private const string _THIRD_PARTY_FOLDER = "third_party";
	private const string _THIRD_PARTY_PYTHON_FOLDER = "python";
	private const string _VENDOR_FOLDER = "vendor";
	private const string _VENDORED_GO_MODULE_HOST_FOLDER = "modules.example";
	private const string _PACKAGE_FOLDER_FORMAT = "pkg{0:D3}";
	private const string _MODULE_FOLDER_FORMAT = "mod{0:D2}";
	private const string _GO_MODULE_FOLDER_FORMAT = "lib{0:D3}";
	private const string _GO_PACKAGE_FOLDER_FORMAT = "pkg{0:D2}";

	private static readonly WorstCaseFileKind[] _TypeScriptKinds =
	[
		WorstCaseFileKind.TypeScriptSource,
		WorstCaseFileKind.TypeScriptTsxSource,
		WorstCaseFileKind.TypeScriptModuleSource,
		WorstCaseFileKind.TypeScriptCommonJsSource,
		WorstCaseFileKind.TypeScriptDeclarationFile,
		WorstCaseFileKind.TypeScriptModuleDeclarationFile,
		WorstCaseFileKind.TypeScriptCommonJsDeclarationFile
	];

	private static readonly string[] _NonCandidateExtensions = [".txt", ".md", ".json", ".xml"];

	/// <summary>The plan for one file of a tree described by the options.</summary>
	public static WorstCaseFilePlan PlanFile(WorstCaseTreeOptions options, long index)
	{
		WorstCaseFileKind kind = KindOf(options, index);
		string extension = ExtensionOf(options, index, kind);

		return new WorstCaseFilePlan(index, RelativePathOf(options.TreeShape, index, kind, extension), kind);
	}

	private static string ExtensionOf(WorstCaseTreeOptions options, long index, WorstCaseFileKind kind)
	{
		return kind switch
		{
			WorstCaseFileKind.NonCandidate => _NonCandidateExtensions[DeterministicRandom.Below(_NON_CANDIDATE_EXTENSION_COUNT, options.Seed, index, _STREAM_EXTENSION)],
			WorstCaseFileKind.PowerShellScript => _POWERSHELL_SCRIPT_EXTENSION,
			WorstCaseFileKind.PowerShellShebangScript => _NO_EXTENSION,
			WorstCaseFileKind.VBNetSource => _VBNET_SOURCE_EXTENSION,
			WorstCaseFileKind.ShellScript => _SHELL_SCRIPT_EXTENSION,
			WorstCaseFileKind.ShellShebangScript => _NO_EXTENSION,
			WorstCaseFileKind.JavaScriptSource => _JAVASCRIPT_SOURCE_EXTENSION,
			WorstCaseFileKind.JavaScriptJsxSource => _JAVASCRIPT_JSX_EXTENSION,
			WorstCaseFileKind.JavaScriptNodeScript => _NO_EXTENSION,
			WorstCaseFileKind.TypeScriptSource => _TYPESCRIPT_SOURCE_EXTENSION,
			WorstCaseFileKind.TypeScriptTsxSource => _TYPESCRIPT_TSX_EXTENSION,
			WorstCaseFileKind.TypeScriptModuleSource => _TYPESCRIPT_MODULE_EXTENSION,
			WorstCaseFileKind.TypeScriptCommonJsSource => _TYPESCRIPT_COMMONJS_EXTENSION,
			WorstCaseFileKind.TypeScriptDeclarationFile => _TYPESCRIPT_DECLARATION_EXTENSION,
			WorstCaseFileKind.TypeScriptModuleDeclarationFile => _TYPESCRIPT_MODULE_DECLARATION_EXTENSION,
			WorstCaseFileKind.TypeScriptCommonJsDeclarationFile => _TYPESCRIPT_COMMONJS_DECLARATION_EXTENSION,
			WorstCaseFileKind.PythonSource => _PYTHON_SOURCE_EXTENSION,
			WorstCaseFileKind.PythonStubFile => _PYTHON_STUB_EXTENSION,
			WorstCaseFileKind.PythonShebangScript => _NO_EXTENSION,
			WorstCaseFileKind.GoSource => _GO_SOURCE_EXTENSION,
			_ => _CANDIDATE_EXTENSION
		};
	}

	/// <summary>The kind of the file at an index.</summary>
	public static WorstCaseFileKind KindOf(WorstCaseTreeOptions options, long index)
	{
		long afterHeavy = index - options.HeavyFileCount;
		WorstCaseFileKind kind;

		if (afterHeavy < 0)
		{
			kind = WorstCaseFileKind.Heavy;
		}
		else if (IsOversizeSlot(options, afterHeavy))
		{
			kind = WorstCaseFileKind.Oversize;
		}
		else if (afterHeavy == 0)
		{
			kind = WorstCaseFileKind.HighTail;
		}
		else
		{
			kind = KindByBasisPoints(DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, STREAM_KIND));
		}

		return kind == WorstCaseFileKind.Filler ? GoInsteadOfFiller(options, index, PythonInsteadOfFiller(options, index, TypeScriptInsteadOfFiller(options, index, JavaScriptInsteadOfFiller(options, index, ShellInsteadOfFiller(options, index, VBNetInsteadOfFiller(options, index, PowerShellInsteadOfFiller(options, index))))))) : kind;
	}

	/// <summary>A file still ordinary after the Python share is a Go source for the share the options ask for, drawn on a stream of its own after the Python share.</summary>
	private static WorstCaseFileKind GoInsteadOfFiller(WorstCaseTreeOptions options, long index, WorstCaseFileKind kind)
	{
		bool isGo = kind == WorstCaseFileKind.Filler
			&& DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_GO_SHARE) < options.GoSourceBasisPoints;

		return isGo ? WorstCaseFileKind.GoSource : kind;
	}

	/// <summary>A file still ordinary after the TypeScript share is a Python file for the share the options ask for, and its form (a source, a type stub or an extensionless script) is decided on a stream of its own.</summary>
	private static WorstCaseFileKind PythonInsteadOfFiller(WorstCaseTreeOptions options, long index, WorstCaseFileKind kind)
	{
		WorstCaseFileKind result = kind;

		if (kind == WorstCaseFileKind.Filler
			&& DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_PYTHON_SHARE) < options.PythonSourceBasisPoints)
		{
			result = DeterministicRandom.Below(_PYTHON_FORM_DENOMINATOR, options.Seed, index, _STREAM_PYTHON_FORM) switch
			{
				< _PYTHON_SHEBANG_FORM_LIMIT => WorstCaseFileKind.PythonShebangScript,
				< _PYTHON_STUB_FORM_LIMIT => WorstCaseFileKind.PythonStubFile,
				_ => WorstCaseFileKind.PythonSource
			};
		}

		return result;
	}

	/// <summary>A file still ordinary after the JavaScript share is a TypeScript file for the share the options ask for, and its form (one of seven extensions) is decided on a stream of its own.</summary>
	private static WorstCaseFileKind TypeScriptInsteadOfFiller(WorstCaseTreeOptions options, long index, WorstCaseFileKind kind)
	{
		WorstCaseFileKind result = kind;

		if (kind == WorstCaseFileKind.Filler
			&& DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_TYPESCRIPT_SHARE) < options.TypeScriptSourceBasisPoints)
		{
			result = _TypeScriptKinds[DeterministicRandom.Below(_TypeScriptKinds.Length, options.Seed, index, _STREAM_TYPESCRIPT_FORM)];
		}

		return result;
	}

	/// <summary>A file still ordinary after the Shell share is a JavaScript source for the share the options ask for, and its form (<c>.js</c>, <c>.jsx</c> or an extensionless <c>node</c> script) is decided on a stream of its own.</summary>
	private static WorstCaseFileKind JavaScriptInsteadOfFiller(WorstCaseTreeOptions options, long index, WorstCaseFileKind kind)
	{
		WorstCaseFileKind result = kind;

		if (kind == WorstCaseFileKind.Filler
			&& DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_JAVASCRIPT_SHARE) < options.JavaScriptSourceBasisPoints)
		{
			result = DeterministicRandom.Below(_JAVASCRIPT_FORM_COUNT, options.Seed, index, _STREAM_JAVASCRIPT_FORM) switch
			{
				_JAVASCRIPT_PLAIN_FORM => WorstCaseFileKind.JavaScriptSource,
				_JAVASCRIPT_JSX_FORM => WorstCaseFileKind.JavaScriptJsxSource,
				_ => WorstCaseFileKind.JavaScriptNodeScript
			};
		}

		return result;
	}

	/// <summary>A file still ordinary after the Visual Basic share is a Shell script for the share the options ask for, and its form is decided on a stream of its own.</summary>
	private static WorstCaseFileKind ShellInsteadOfFiller(WorstCaseTreeOptions options, long index, WorstCaseFileKind kind)
	{
		WorstCaseFileKind result = kind;

		if (kind == WorstCaseFileKind.Filler
			&& DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_SHELL_SHARE) < options.ShellScriptBasisPoints)
		{
			result = DeterministicRandom.Below(_SHELL_FORM_COUNT, options.Seed, index, _STREAM_SHELL_FORM) == 0
				? WorstCaseFileKind.ShellScript
				: WorstCaseFileKind.ShellShebangScript;
		}

		return result;
	}

	/// <summary>A file still ordinary after the PowerShell share is a Visual Basic source for the share the options ask for.</summary>
	private static WorstCaseFileKind VBNetInsteadOfFiller(WorstCaseTreeOptions options, long index, WorstCaseFileKind kind)
	{
		bool isVBNet = kind == WorstCaseFileKind.Filler
			&& DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_VBNET_SHARE) < options.VBNetSourceBasisPoints;

		return isVBNet ? WorstCaseFileKind.VBNetSource : kind;
	}

	/// <summary>An ordinary file is a PowerShell script for the share the options ask for, and its form is decided on a stream of its own.</summary>
	private static WorstCaseFileKind PowerShellInsteadOfFiller(WorstCaseTreeOptions options, long index)
	{
		WorstCaseFileKind kind = WorstCaseFileKind.Filler;

		if (DeterministicRandom.Below(BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_POWERSHELL_SHARE) < options.PowerShellScriptBasisPoints)
		{
			kind = DeterministicRandom.Below(_POWERSHELL_FORM_COUNT, options.Seed, index, _STREAM_POWERSHELL_FORM) == 0
				? WorstCaseFileKind.PowerShellScript
				: WorstCaseFileKind.PowerShellShebangScript;
		}

		return kind;
	}

	private static WorstCaseFileKind KindByBasisPoints(int basisPoints)
	{
		return basisPoints switch
		{
			< _HIGH_TAIL_THRESHOLD => WorstCaseFileKind.HighTail,
			< _UNDECODABLE_THRESHOLD => WorstCaseFileKind.Undecodable,
			< _OVER_LONG_LINE_THRESHOLD => WorstCaseFileKind.OverLongLine,
			< _NON_CANDIDATE_THRESHOLD => WorstCaseFileKind.NonCandidate,
			_ => WorstCaseFileKind.Filler
		};
	}

	/// <summary>Oversize files sit at evenly spaced slots after the heavy files, never the first one, so their count is exact.</summary>
	private static bool IsOversizeSlot(WorstCaseTreeOptions options, long afterHeavy)
	{
		bool isSlot = false;

		if (options.OversizeFileCount > 0)
		{
			long stride = (options.FileCount - options.HeavyFileCount) / options.OversizeFileCount;

			isSlot = stride > 1
				&& afterHeavy / stride < options.OversizeFileCount
				&& afterHeavy % stride == stride / 2;
		}

		return isSlot;
	}

	private static string RelativePathOf(WorstCaseTreeShape shape, long index, WorstCaseFileKind kind, string extension)
	{
		long leaf = index / FILES_PER_LEAF_FOLDER;
		long middle = leaf / LEAF_FOLDERS_PER_MIDDLE_FOLDER;
		string fileName = string.Format(CultureInfo.InvariantCulture, _FILE_FORMAT, index, extension);

		return string.Join(_PATH_SEPARATOR, [.. FoldersOf(shape, kind, leaf, middle), fileName]);
	}

	/// <summary>The folders a file stands in: the plain three levels, or, for the files of a language a shape moves, the layout of that shape.</summary>
	private static string[] FoldersOf(WorstCaseTreeShape shape, WorstCaseFileKind kind, long leaf, long middle)
	{
		bool isPython = kind is WorstCaseFileKind.PythonSource or WorstCaseFileKind.PythonStubFile or WorstCaseFileKind.PythonShebangScript;
		string[] folders = PlainFoldersOf(leaf, middle);

		if (shape == WorstCaseTreeShape.VirtualEnvironment && isPython)
		{
			folders = VirtualEnvironmentFoldersOf(kind, leaf, middle);
		}
		else if (shape == WorstCaseTreeShape.Vendored && isPython)
		{
			folders = [_THIRD_PARTY_FOLDER, _THIRD_PARTY_PYTHON_FOLDER, PackageFolderOf(middle), ModuleFolderOf(leaf)];
		}
		else if (shape == WorstCaseTreeShape.Vendored && kind == WorstCaseFileKind.GoSource)
		{
			folders =
			[
				_VENDOR_FOLDER,
				_VENDORED_GO_MODULE_HOST_FOLDER,
				string.Format(CultureInfo.InvariantCulture, _GO_MODULE_FOLDER_FORMAT, middle),
				string.Format(CultureInfo.InvariantCulture, _GO_PACKAGE_FOLDER_FORMAT, leaf % LEAF_FOLDERS_PER_MIDDLE_FOLDER)
			];
		}

		return folders;
	}

	/// <summary>The scripts of a virtual environment stand in its flat <c>bin</c> folder; its sources and stubs in a package of <c>site-packages</c>.</summary>
	private static string[] VirtualEnvironmentFoldersOf(WorstCaseFileKind kind, long leaf, long middle)
	{
		return kind == WorstCaseFileKind.PythonShebangScript
			? [_VIRTUAL_ENVIRONMENT_FOLDER, _VIRTUAL_ENVIRONMENT_SCRIPTS_FOLDER]
			: [_VIRTUAL_ENVIRONMENT_FOLDER, _VIRTUAL_ENVIRONMENT_LIBRARY_FOLDER, _VIRTUAL_ENVIRONMENT_INTERPRETER_FOLDER, _VIRTUAL_ENVIRONMENT_PACKAGES_FOLDER, PackageFolderOf(middle), ModuleFolderOf(leaf)];
	}

	private static string PackageFolderOf(long middle)
	{
		return string.Format(CultureInfo.InvariantCulture, _PACKAGE_FOLDER_FORMAT, middle);
	}

	private static string ModuleFolderOf(long leaf)
	{
		return string.Format(CultureInfo.InvariantCulture, _MODULE_FOLDER_FORMAT, leaf % LEAF_FOLDERS_PER_MIDDLE_FOLDER);
	}

	private static string[] PlainFoldersOf(long leaf, long middle)
	{
		long top = middle / LEAF_FOLDERS_PER_MIDDLE_FOLDER;

		return
		[
			string.Format(CultureInfo.InvariantCulture, _TOP_FOLDER_FORMAT, top),
			string.Format(CultureInfo.InvariantCulture, _MIDDLE_FOLDER_FORMAT, middle % LEAF_FOLDERS_PER_MIDDLE_FOLDER),
			string.Format(CultureInfo.InvariantCulture, _LEAF_FOLDER_FORMAT, leaf % LEAF_FOLDERS_PER_MIDDLE_FOLDER)
		];
	}
}
