using System.Globalization;
using System.Text;

namespace MigrateAuth.WorstCase;

/// <summary>
/// The invented bytes of each kind of file. Every name is generic and every line is a pure function
/// of the seed and the file index; nothing is taken from any real code. Lines end in a single line
/// feed so the bytes are the same on every platform.
/// </summary>
public static class WorstCaseFileContent
{
	/// <summary>The product's file-size ceiling in bytes, restated because the tool references nothing from it; a test holds the two equal.</summary>
	public const int PRODUCT_MAXIMUM_FILE_SIZE_IN_BYTES = 2 * 1024 * 1024;

	/// <summary>The product's line-length ceiling in characters, restated for the same reason.</summary>
	public const int PRODUCT_MAXIMUM_LINE_LENGTH_IN_CHARACTERS = 10_000;

	private const int _HEAVY_CONSTRUCTS_ABOVE_PER_FILE_CAP = 100;
	private const int _STREAM_SHAPE = 3;
	private const int _STREAM_SIZE = 4;
	private const int _STREAM_NAME = 5;
	private const int _STREAM_PYTHON_CONSTRUCT_SHARE = 18;
	private const int _STREAM_PYTHON_CONSTRUCT_COUNT = 19;
	private const int _PYTHON_CONSTRUCT_MAXIMUM_COUNT = 3;
	private const int _STREAM_GO_CONSTRUCT_SHARE = 20;
	private const int _STREAM_GO_CONSTRUCT_COUNT = 21;
	private const int _GO_CONSTRUCT_MAXIMUM_COUNT = 3;

	/// <summary>
	/// How many of every ten thousand Go sources carry constructs the shipped analyzer recognizes: five hundred, one file in twenty, as for Python. Each such file holds one to three,
	/// so a generated Go file adds a modest number of findings and never reaches the per-file cap.
	/// </summary>
	public const int GO_CONSTRUCT_BASIS_POINTS = 500;

	/// <summary>
	/// How many of every ten thousand Python sources and shebang scripts carry constructs the shipped analyzer recognizes: five hundred, one file in twenty. Each such file holds one to three,
	/// so a generated Python file adds a modest number of findings and never reaches the per-file cap. Stubs never carry any: no analyzer claims <c>.pyi</c>.
	/// </summary>
	public const int PYTHON_CONSTRUCT_BASIS_POINTS = 500;
	private const int _HIGH_TAIL_MAXIMUM_CONSTRUCTS = 3;
	private const int _HIGH_TAIL_VARIANT_COUNT = 3;
	private const int _SIZE_DENOMINATOR = 1_000;
	private const int _MEDIUM_SIZE_THRESHOLD = 105;
	private const int _LARGE_SIZE_THRESHOLD = 5;
	private const int _SMALL_METHOD_COUNT = 4;
	private const int _MEDIUM_METHOD_COUNT = 30;
	private const int _LARGE_METHOD_COUNT = 300;
	private const int _ANCHOR_SHARE_DENOMINATOR = 10;
	private const int _ANCHOR_SHARE_NUMERATOR = 3;
	private const int _OVERSIZE_MARGIN_IN_BYTES = 1_024;
	private const int _OVERSIZE_LINE_LENGTH = 1_023;
	private const int _INTERACTIVE_VARIANT = 1;
	private const int _FIXED_VARIANT = 2;
	private const char _FILLER_CHARACTER = 'a';
	private const string _LINE_END = "\n";
	private const string _POWERSHELL_SHEBANG_LINE = "#!/usr/bin/env pwsh\n";
	private const string _VBNET_ANCHOR_LINE = "\tPrivate ReadOnly credentialSource As String = \"configured elsewhere\"\n";
	private const string _VBNET_FUNCTION_FORMAT = "\n\tFunction AddAmount{0}(amount As Integer) As Integer\n\t\ttotal += amount * {0}\n\t\tReturn total\n\tEnd Function\n";
	private const string _SHELL_SHEBANG_LINE = "#!/usr/bin/env bash\n";
	private const string _SHELL_ANCHOR_LINE = "curl --silent --fail \"https://status.invalid/health\" > /dev/null\n";
	private const string _SHELL_FUNCTION_FORMAT = "\nadd_amount{0}() {{\n\tlocal amount=\"$1\"\n\ttotal=$((total + amount * {0}))\n\techo \"$total\"\n}}\n";
	private const string _JAVASCRIPT_HASHBANG_LINE = "#!/usr/bin/env node\n";
	private const string _JAVASCRIPT_ANCHOR_LINE = "fetch(\"https://status.invalid/health\");\n";
	private const string _JAVASCRIPT_COMMENT_PREFIX = "// Ordinary arithmetic for ";
	private const string _JAVASCRIPT_TOTAL_DECLARATION = "let total = 0;";
	private const string _JAVASCRIPT_FUNCTION_FORMAT = "\nfunction addAmount{0}(amount) {{\n\ttotal += amount * {0};\n\treturn total;\n}}\n";
	private const string _TYPESCRIPT_ANCHOR_LINE = "fetch(\"https://status.invalid/health\");\n";
	private const string _TYPESCRIPT_COMMENT_PREFIX = "// Ordinary typed arithmetic for ";
	private const string _TYPESCRIPT_TOTAL_DECLARATION = "let total: number = 0;";
	private const string _TYPESCRIPT_FUNCTION_FORMAT = "\nfunction addAmount{0}(amount: number): number {{\n\ttotal += amount * {0};\n\treturn total;\n}}\n";
	private const string _TYPESCRIPT_TSX_FUNCTION_FORMAT = "\nfunction Amount{0}(props: {{ amount: number }}): JSX.Element {{\n\treturn <span title=\"{0}\">{{props.amount * {0}}}</span>;\n}}\n";
	private const string _TYPESCRIPT_DECLARATION_FORMAT = "\nexport declare function addAmount{0}(amount: number): number;\n";
	private const string _TYPESCRIPT_DECLARATION_ANCHOR_FORMAT = "export declare const endpoint{0}: \"https://status.invalid/health{0}\";\n";
	private const string _TYPESCRIPT_DECLARATION_HEADER = "export {};\n";
	private const string _PYTHON_SHEBANG_LINE = "#!/usr/bin/env python3\n";
	private const string _PYTHON_COMMENT_PREFIX = "# Ordinary arithmetic for ";
	private const string _PYTHON_TOTAL_DECLARATION = "total = 0";
	private const string _PYTHON_ANCHOR_LINE = "ENDPOINT = \"https://status.invalid/health\"\n";
	private const string _PYTHON_FUNCTION_FORMAT = "\n\ndef add_amount{0}(amount):\n\tglobal total\n\ttotal += amount * {0}\n\treturn total\n";
	private const string _PYTHON_CONSTRUCT_FORMAT = "session_{0} = HTTPBasicAuth(account_a, account_b)\n";
	private const string _PYTHON_STUB_HEADER = "total: int\n";
	private const string _PYTHON_STUB_ANCHOR_LINE = "ENDPOINT: str\n";
	private const string _PYTHON_STUB_FUNCTION_FORMAT = "\ndef add_amount{0}(amount: int) -> int: ...\n";
	private const string _GO_PACKAGE_LINE = "package ordinary\n";
	private const string _GO_TOTAL_DECLARATION = "var total int";
	private const string _GO_ANCHOR_LINE = "const endpoint = \"https://status.invalid/health\"\n";
	private const string _GO_COMMENT_PREFIX = "// Ordinary arithmetic for ";
	private const string _GO_FUNCTION_FORMAT = "\nfunc AddAmount{0}(amount int) int {{\n\ttotal += amount * {0}\n\treturn total\n}}\n";
	private const string _GO_CONSTRUCT_FORMAT = "\nfunc Authorize{0}(request *http.Request, accountA string, accountB string) {{\n\trequest.SetBasicAuth(accountA, accountB)\n}}\n";
	private const string _POWERSHELL_ANCHOR_LINE = "$credentialSource = 'configured elsewhere'\n";
	private const string _POWERSHELL_FUNCTION_FORMAT = "\nfunction Add-Amount{0}\n{{\n\tparam([int]$Amount)\n\t$script:total += $Amount * {0}\n\treturn $script:total\n}}\n";

	private const string _INTERACTIVE_SIGNAL_LINES =
		"\t\tConsole.Write(\"Enter your password: \");\n\t\tstring secret = Console.ReadLine();\n";

	private const string _FIXED_SIGNAL_LINE = "\t\tstring secret = Environment.GetEnvironmentVariable(\"SERVICE_PWD\");\n";
	private const string _NO_SIGNAL_LINE = "\t\tstring secret = string.Empty;\n";

	private static readonly UTF8Encoding _Utf8WithoutMark = new(false);
	/// <summary>
	/// Invalid UTF-8 that the Windows-1252 last resort also refuses: the last byte, 0x81, is one of the five
	/// that code page leaves undefined, so the file is still counted as undecodable rather than read.
	/// </summary>
	private static readonly byte[] _InvalidUtf8Bytes = [0xC3, 0x28, 0xA0, 0xA1, 0x81];

	private static readonly string[] _TypeNames =
		["Widget", "Gadget", "Ledger", "Parcel", "Beacon", "Harbor", "Meadow", "Lantern"];

	/// <summary>The bytes of one planned file.</summary>
	public static byte[] BytesOf(WorstCaseTreeOptions options, WorstCaseFilePlan plan)
	{
		string typeName = _TypeNames[DeterministicRandom.Below(_TypeNames.Length, options.Seed, plan.Index, _STREAM_NAME)]
			+ plan.Index.ToString(CultureInfo.InvariantCulture);

		return plan.Kind switch
		{
			WorstCaseFileKind.Heavy => Encode(HeavyText(typeName, options.PerFileFindingsCap + _HEAVY_CONSTRUCTS_ABOVE_PER_FILE_CAP)),
			WorstCaseFileKind.HighTail => Encode(HighTailText(options, plan.Index, typeName)),
			WorstCaseFileKind.Undecodable => UndecodableBytes(options, plan.Index, typeName),
			WorstCaseFileKind.OverLongLine => Encode(OverLongLineText(typeName)),
			WorstCaseFileKind.Oversize => OversizeBytes(),
			WorstCaseFileKind.NonCandidate => Encode(NonCandidateText(plan.Index)),
			WorstCaseFileKind.PowerShellScript => Encode(PowerShellText(options, plan.Index, typeName, shebang: false)),
			WorstCaseFileKind.PowerShellShebangScript => Encode(PowerShellText(options, plan.Index, typeName, shebang: true)),
			WorstCaseFileKind.VBNetSource => Encode(VBNetText(options, plan.Index, typeName)),
			WorstCaseFileKind.ShellScript => Encode(ShellText(options, plan.Index, typeName, shebang: false)),
			WorstCaseFileKind.ShellShebangScript => Encode(ShellText(options, plan.Index, typeName, shebang: true)),
			WorstCaseFileKind.JavaScriptSource or WorstCaseFileKind.JavaScriptJsxSource => Encode(JavaScriptText(options, plan.Index, typeName, hashbang: false)),
			WorstCaseFileKind.JavaScriptNodeScript => Encode(JavaScriptText(options, plan.Index, typeName, hashbang: true)),
			WorstCaseFileKind.TypeScriptSource or WorstCaseFileKind.TypeScriptModuleSource or WorstCaseFileKind.TypeScriptCommonJsSource => Encode(TypeScriptText(options, plan.Index, typeName, isTsx: false)),
			WorstCaseFileKind.TypeScriptTsxSource => Encode(TypeScriptText(options, plan.Index, typeName, isTsx: true)),
			WorstCaseFileKind.TypeScriptDeclarationFile or WorstCaseFileKind.TypeScriptModuleDeclarationFile or WorstCaseFileKind.TypeScriptCommonJsDeclarationFile => Encode(TypeScriptDeclarationText(options, plan.Index)),
			WorstCaseFileKind.PythonSource => Encode(PythonText(options, plan.Index, typeName, shebang: false)),
			WorstCaseFileKind.PythonShebangScript => Encode(PythonText(options, plan.Index, typeName, shebang: true)),
			WorstCaseFileKind.PythonStubFile => Encode(PythonStubText(options, plan.Index)),
			WorstCaseFileKind.GoSource => Encode(GoText(options, plan.Index, typeName)),
			_ => Encode(FillerText(options, plan.Index, typeName))
		};
	}

	private static byte[] Encode(string text)
	{
		return _Utf8WithoutMark.GetBytes(text);
	}

	private static string HeavyText(string typeName, int constructCount)
	{
		StringBuilder builder = new();

		builder.Append("public sealed class ").Append(typeName).Append("\n{\n");
		builder.Append("\tprivate readonly string userName;\n\tprivate readonly string password;\n\n\tprivate void Build()\n\t{\n");

		for (int number = 0; number < constructCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, "\t\tCredentials credentials{0:D5} = new UsernamePasswordCredentials(userName, password);\n", number));
		}

		builder.Append("\t}\n}\n");

		return builder.ToString();
	}

	private static string HighTailText(WorstCaseTreeOptions options, long index, string typeName)
	{
		StringBuilder builder = new();
		int constructCount = 1 + DeterministicRandom.Below(_HIGH_TAIL_MAXIMUM_CONSTRUCTS, options.Seed, index, _STREAM_SHAPE);
		int variant = DeterministicRandom.Below(_HIGH_TAIL_VARIANT_COUNT, options.Seed, index, _STREAM_SIZE);

		builder.Append("public sealed class ").Append(typeName).Append("\n{\n");
		builder.Append("\tprivate readonly string userName;\n\n\tprivate void Build()\n\t{\n");
		builder.Append(HighTailSignalLine(variant));

		for (int number = 0; number < constructCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, "\t\tCredentials credentials{0} = new UsernamePasswordCredentials(userName, secret);\n", number));
		}

		builder.Append("\t}\n}\n");

		return builder.ToString();
	}

	/// <summary>Variant 0 carries no signal; 1 an interactive-user one; 2 a fixed-credential one.</summary>
	private static string HighTailSignalLine(int variant)
	{
		return variant switch
		{
			_INTERACTIVE_VARIANT => _INTERACTIVE_SIGNAL_LINES,
			_FIXED_VARIANT => _FIXED_SIGNAL_LINE,
			_ => _NO_SIGNAL_LINE
		};
	}

	private static string FillerText(WorstCaseTreeOptions options, long index, string typeName)
	{
		StringBuilder builder = new();
		int sizeRoll = DeterministicRandom.Below(_SIZE_DENOMINATOR, options.Seed, index, _STREAM_SIZE);
		int methodCount = _SMALL_METHOD_COUNT;
		bool mentionsAnchor = DeterministicRandom.Below(_ANCHOR_SHARE_DENOMINATOR, options.Seed, index, _STREAM_SHAPE) < _ANCHOR_SHARE_NUMERATOR;

		if (sizeRoll < _LARGE_SIZE_THRESHOLD)
		{
			methodCount = _LARGE_METHOD_COUNT;
		}
		else if (sizeRoll < _MEDIUM_SIZE_THRESHOLD)
		{
			methodCount = _MEDIUM_METHOD_COUNT;
		}

		builder.Append("public sealed class ").Append(typeName).Append("\n{\n");
		builder.Append("\tprivate int total;\n");

		if (mentionsAnchor)
		{
			builder.Append("\tprivate readonly ICredentialSource source;\n");
		}

		for (int number = 0; number < methodCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, "\n\tpublic int Add{0}(int amount)\n\t{{\n\t\ttotal += amount * {0};\n\t\treturn total;\n\t}}\n", number));
		}

		builder.Append("}\n");

		return builder.ToString();
	}

	/// <summary>
	/// Ordinary PowerShell with no construct, in the same small, medium and large mix as the C# filler; a share of the files still mention a candidate
	/// anchor word. The shebang form opens with the first line that attributes an extensionless file.
	/// </summary>
	private static string PowerShellText(WorstCaseTreeOptions options, long index, string functionName, bool shebang)
	{
		StringBuilder builder = new();
		int sizeRoll = DeterministicRandom.Below(_SIZE_DENOMINATOR, options.Seed, index, _STREAM_SIZE);
		int functionCount = _SMALL_METHOD_COUNT;
		bool mentionsAnchor = DeterministicRandom.Below(_ANCHOR_SHARE_DENOMINATOR, options.Seed, index, _STREAM_SHAPE) < _ANCHOR_SHARE_NUMERATOR;

		if (sizeRoll < _LARGE_SIZE_THRESHOLD)
		{
			functionCount = _LARGE_METHOD_COUNT;
		}
		else if (sizeRoll < _MEDIUM_SIZE_THRESHOLD)
		{
			functionCount = _MEDIUM_METHOD_COUNT;
		}

		builder.Append(shebang ? _POWERSHELL_SHEBANG_LINE : string.Empty);
		builder.Append("# Ordinary arithmetic for ").Append(functionName).Append(_LINE_END);
		builder.Append("$total = 0").Append(_LINE_END);
		builder.Append(mentionsAnchor ? _POWERSHELL_ANCHOR_LINE : string.Empty);

		for (int number = 0; number < functionCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _POWERSHELL_FUNCTION_FORMAT, number));
		}

		return builder.ToString();
	}

	/// <summary>
	/// Ordinary Visual Basic with no construct, in the same small, medium and large mix as the C# filler; a share of the files still mention a candidate
	/// anchor word.
	/// </summary>
	private static string VBNetText(WorstCaseTreeOptions options, long index, string moduleName)
	{
		StringBuilder builder = new();
		int sizeRoll = DeterministicRandom.Below(_SIZE_DENOMINATOR, options.Seed, index, _STREAM_SIZE);
		int functionCount = _SMALL_METHOD_COUNT;
		bool mentionsAnchor = DeterministicRandom.Below(_ANCHOR_SHARE_DENOMINATOR, options.Seed, index, _STREAM_SHAPE) < _ANCHOR_SHARE_NUMERATOR;

		if (sizeRoll < _LARGE_SIZE_THRESHOLD)
		{
			functionCount = _LARGE_METHOD_COUNT;
		}
		else if (sizeRoll < _MEDIUM_SIZE_THRESHOLD)
		{
			functionCount = _MEDIUM_METHOD_COUNT;
		}

		builder.Append("' Ordinary arithmetic for ").Append(moduleName).Append(_LINE_END);
		builder.Append("Public Module ").Append(moduleName).Append(_LINE_END);
		builder.Append("\tPrivate total As Integer = 0").Append(_LINE_END);
		builder.Append(mentionsAnchor ? _VBNET_ANCHOR_LINE : string.Empty);

		for (int number = 0; number < functionCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _VBNET_FUNCTION_FORMAT, number));
		}

		builder.Append("End Module").Append(_LINE_END);

		return builder.ToString();
	}

	/// <summary>
	/// Ordinary Shell with no construct, in the same small, medium and large mix as the C# filler; a share of the files still carry one ordinary <c>curl</c> call, a content-only preselection anchor of the shipped analyzer.
	/// The shebang form starts with a <c>bash</c> shebang and the plain form starts with a comment.
	/// </summary>
	private static string ShellText(WorstCaseTreeOptions options, long index, string functionName, bool shebang)
	{
		StringBuilder builder = new();
		int sizeRoll = DeterministicRandom.Below(_SIZE_DENOMINATOR, options.Seed, index, _STREAM_SIZE);
		int functionCount = _SMALL_METHOD_COUNT;
		bool mentionsAnchor = DeterministicRandom.Below(_ANCHOR_SHARE_DENOMINATOR, options.Seed, index, _STREAM_SHAPE) < _ANCHOR_SHARE_NUMERATOR;

		if (sizeRoll < _LARGE_SIZE_THRESHOLD)
		{
			functionCount = _LARGE_METHOD_COUNT;
		}
		else if (sizeRoll < _MEDIUM_SIZE_THRESHOLD)
		{
			functionCount = _MEDIUM_METHOD_COUNT;
		}

		builder.Append(shebang ? _SHELL_SHEBANG_LINE : string.Empty);
		builder.Append("# Ordinary arithmetic for ").Append(functionName).Append(_LINE_END);
		builder.Append("total=0").Append(_LINE_END);
		builder.Append(mentionsAnchor ? _SHELL_ANCHOR_LINE : string.Empty);

		for (int number = 0; number < functionCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _SHELL_FUNCTION_FORMAT, number));
		}

		return builder.ToString();
	}

	/// <summary>
	/// Ordinary JavaScript with no construct, in the same small, medium and large mix as the C# filler; a share of the files still carry one ordinary <c>fetch</c> call to a URL with no credential, a content-only preselection anchor
	/// (<c>://</c>) of the shipped analyzer. The hashbang form starts with a <c>node</c> hashbang, which is the only thing that attributes an extensionless file.
	/// </summary>
	private static string JavaScriptText(WorstCaseTreeOptions options, long index, string functionName, bool hashbang)
	{
		StringBuilder builder = new();
		int sizeRoll = DeterministicRandom.Below(_SIZE_DENOMINATOR, options.Seed, index, _STREAM_SIZE);
		int functionCount = _SMALL_METHOD_COUNT;
		bool mentionsAnchor = DeterministicRandom.Below(_ANCHOR_SHARE_DENOMINATOR, options.Seed, index, _STREAM_SHAPE) < _ANCHOR_SHARE_NUMERATOR;

		if (sizeRoll < _LARGE_SIZE_THRESHOLD)
		{
			functionCount = _LARGE_METHOD_COUNT;
		}
		else if (sizeRoll < _MEDIUM_SIZE_THRESHOLD)
		{
			functionCount = _MEDIUM_METHOD_COUNT;
		}

		builder.Append(hashbang ? _JAVASCRIPT_HASHBANG_LINE : string.Empty);
		builder.Append(_JAVASCRIPT_COMMENT_PREFIX).Append(functionName).Append(_LINE_END);
		builder.Append(_JAVASCRIPT_TOTAL_DECLARATION).Append(_LINE_END);
		builder.Append(mentionsAnchor ? _JAVASCRIPT_ANCHOR_LINE : string.Empty);

		for (int number = 0; number < functionCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _JAVASCRIPT_FUNCTION_FORMAT, number));
		}

		return builder.ToString();
	}

	private static int FunctionCountOf(WorstCaseTreeOptions options, long index)
	{
		int sizeRoll = DeterministicRandom.Below(_SIZE_DENOMINATOR, options.Seed, index, _STREAM_SIZE);
		int functionCount = _SMALL_METHOD_COUNT;

		if (sizeRoll < _LARGE_SIZE_THRESHOLD)
		{
			functionCount = _LARGE_METHOD_COUNT;
		}
		else if (sizeRoll < _MEDIUM_SIZE_THRESHOLD)
		{
			functionCount = _MEDIUM_METHOD_COUNT;
		}

		return functionCount;
	}

	private static bool MentionsAnchor(WorstCaseTreeOptions options, long index)
	{
		return DeterministicRandom.Below(_ANCHOR_SHARE_DENOMINATOR, options.Seed, index, _STREAM_SHAPE) < _ANCHOR_SHARE_NUMERATOR;
	}

	/// <summary>
	/// Ordinary typed TypeScript with no construct, in the same small, medium and large mix as the C# filler; a share of the files still carry one ordinary <c>fetch</c> call to a URL with no credential,
	/// a content-only preselection anchor (<c>://</c>). The <c>.tsx</c> form holds element templates instead of plain functions.
	/// </summary>
	private static string TypeScriptText(WorstCaseTreeOptions options, long index, string functionName, bool isTsx)
	{
		StringBuilder builder = new();
		int functionCount = FunctionCountOf(options, index);
		string functionFormat = isTsx ? _TYPESCRIPT_TSX_FUNCTION_FORMAT : _TYPESCRIPT_FUNCTION_FORMAT;

		builder.Append(_TYPESCRIPT_COMMENT_PREFIX).Append(functionName).Append(_LINE_END);
		builder.Append(_TYPESCRIPT_TOTAL_DECLARATION).Append(_LINE_END);
		builder.Append(MentionsAnchor(options, index) ? _TYPESCRIPT_ANCHOR_LINE : string.Empty);

		for (int number = 0; number < functionCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, functionFormat, number));
		}

		return builder.ToString();
	}

	/// <summary>
	/// A declaration file of ambient function declarations in the same small, medium and large mix. A file that mentions an anchor word is anchor-dense: every declaration is followed by a
	/// string-literal constant holding a URL, so the preselection anchor occurs once per declaration.
	/// </summary>
	private static string TypeScriptDeclarationText(WorstCaseTreeOptions options, long index)
	{
		StringBuilder builder = new();
		int declarationCount = FunctionCountOf(options, index);
		bool anchorDense = MentionsAnchor(options, index);

		builder.Append(_TYPESCRIPT_DECLARATION_HEADER);

		for (int number = 0; number < declarationCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _TYPESCRIPT_DECLARATION_FORMAT, number));
			builder.Append(anchorDense ? string.Format(CultureInfo.InvariantCulture, _TYPESCRIPT_DECLARATION_ANCHOR_FORMAT, number) : string.Empty);
		}

		return builder.ToString();
	}

	/// <summary>
	/// Ordinary Python, one file in twenty with one to three recognized constructs (see <see cref="PythonConstructCountOf"/>), in the same small, medium and large mix as the C# filler; a share of the files still carry one assignment of a URL with no credential, a content-only preselection anchor
	/// (<c>://</c>) of the shipped analyzer. The shebang form starts with a <c>python3</c> shebang, which is the only thing that attributes an extensionless file. Indentation is one tab, so the text is valid Python.
	/// </summary>
	private static string PythonText(WorstCaseTreeOptions options, long index, string moduleName, bool shebang)
	{
		StringBuilder builder = new();
		int functionCount = FunctionCountOf(options, index);

		builder.Append(shebang ? _PYTHON_SHEBANG_LINE : string.Empty);
		builder.Append(_PYTHON_COMMENT_PREFIX).Append(moduleName).Append(_LINE_END);
		builder.Append(_PYTHON_TOTAL_DECLARATION).Append(_LINE_END);
		builder.Append(MentionsAnchor(options, index) ? _PYTHON_ANCHOR_LINE : string.Empty);

		for (int number = 0; number < PythonConstructCountOf(options, index); number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _PYTHON_CONSTRUCT_FORMAT, number));
		}

		for (int number = 0; number < functionCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _PYTHON_FUNCTION_FORMAT, number));
		}

		return builder.ToString();
	}

	/// <summary>
	/// How many constructs a generated Python source or shebang script holds: none for most files, and for <see cref="PYTHON_CONSTRUCT_BASIS_POINTS"/> of every ten thousand one to three, each a call of the Basic
	/// helper class with held names, a High class of the shipped analyzer. Drawn on streams of their own, so no other file and no other draw changes.
	/// </summary>
	public static int PythonConstructCountOf(WorstCaseTreeOptions options, long index)
	{
		bool carriesConstructs = DeterministicRandom.Below(WorstCaseTreePlanner.BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_PYTHON_CONSTRUCT_SHARE) < PYTHON_CONSTRUCT_BASIS_POINTS;

		return carriesConstructs ? 1 + DeterministicRandom.Below(_PYTHON_CONSTRUCT_MAXIMUM_COUNT, options.Seed, index, _STREAM_PYTHON_CONSTRUCT_COUNT) : 0;
	}

	/// <summary>A type stub: the signatures of the functions of a module, in the same small, medium and large mix. No analyzer claims the <c>.pyi</c> extension, so the file is enumerated and never read as a candidate.</summary>
	private static string PythonStubText(WorstCaseTreeOptions options, long index)
	{
		StringBuilder builder = new();
		int functionCount = FunctionCountOf(options, index);

		builder.Append(_PYTHON_STUB_HEADER);
		builder.Append(MentionsAnchor(options, index) ? _PYTHON_STUB_ANCHOR_LINE : string.Empty);

		for (int number = 0; number < functionCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _PYTHON_STUB_FUNCTION_FORMAT, number));
		}

		return builder.ToString();
	}

	/// <summary>
	/// Ordinary Go, one file in twenty with one to three recognized constructs (see <see cref="GoConstructCountOf"/>), in the same small, medium and large mix; a share of the files still carry one constant holding a URL with no credential,
	/// a content-only preselection anchor of the shipped analyzer. The recognized forms are one function each, so the text is the shape of Go the analyzer reads.
	/// </summary>
	private static string GoText(WorstCaseTreeOptions options, long index, string functionName)
	{
		StringBuilder builder = new();
		int functionCount = FunctionCountOf(options, index);

		builder.Append(_GO_COMMENT_PREFIX).Append(functionName).Append(_LINE_END);
		builder.Append(_GO_PACKAGE_LINE).Append(_LINE_END);
		builder.Append(_GO_TOTAL_DECLARATION).Append(_LINE_END);
		builder.Append(MentionsAnchor(options, index) ? _GO_ANCHOR_LINE : string.Empty);

		for (int number = 0; number < GoConstructCountOf(options, index); number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _GO_CONSTRUCT_FORMAT, number));
		}

		for (int number = 0; number < functionCount; number++)
		{
			builder.Append(string.Format(CultureInfo.InvariantCulture, _GO_FUNCTION_FORMAT, number));
		}

		return builder.ToString();
	}

	/// <summary>
	/// How many constructs a generated Go source holds: none for most files, and for <see cref="GO_CONSTRUCT_BASIS_POINTS"/> of every ten thousand one to three, each a call of the request method that sets Basic
	/// authentication with held names, a High class (<c>GO-001</c>) of the shipped analyzer. Drawn on streams of their own, so no other file and no other draw changes.
	/// </summary>
	public static int GoConstructCountOf(WorstCaseTreeOptions options, long index)
	{
		bool carriesConstructs = DeterministicRandom.Below(WorstCaseTreePlanner.BASIS_POINT_DENOMINATOR, options.Seed, index, _STREAM_GO_CONSTRUCT_SHARE) < GO_CONSTRUCT_BASIS_POINTS;

		return carriesConstructs ? 1 + DeterministicRandom.Below(_GO_CONSTRUCT_MAXIMUM_COUNT, options.Seed, index, _STREAM_GO_CONSTRUCT_COUNT) : 0;
	}

	private static byte[] UndecodableBytes(WorstCaseTreeOptions options, long index, string typeName)
	{
		byte[] head = Encode(FillerText(options, index, typeName) + "// ");
		byte[] tail = Encode(_LINE_END);
		byte[] bytes = new byte[head.Length + _InvalidUtf8Bytes.Length + tail.Length];

		head.CopyTo(bytes, 0);
		_InvalidUtf8Bytes.CopyTo(bytes, head.Length);
		tail.CopyTo(bytes, head.Length + _InvalidUtf8Bytes.Length);

		return bytes;
	}

	private static string OverLongLineText(string typeName)
	{
		string longLine = "//" + new string(_FILLER_CHARACTER, PRODUCT_MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

		return longLine + _LINE_END + "public sealed class " + typeName + "\n{\n}\n";
	}

	private static byte[] OversizeBytes()
	{
		byte[] line = Encode("//" + new string(_FILLER_CHARACTER, _OVERSIZE_LINE_LENGTH - 2) + _LINE_END);
		int lineCount = ((PRODUCT_MAXIMUM_FILE_SIZE_IN_BYTES + _OVERSIZE_MARGIN_IN_BYTES) / line.Length) + 1;
		byte[] bytes = new byte[lineCount * line.Length];

		for (int number = 0; number < lineCount; number++)
		{
			line.CopyTo(bytes, number * line.Length);
		}

		return bytes;
	}

	private static string NonCandidateText(long index)
	{
		return string.Format(CultureInfo.InvariantCulture, "Notes for item {0}.\nNothing here is code.\n", index);
	}
}
