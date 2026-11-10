namespace MigrateAuth.WorstCase;

/// <summary>
/// The parameters of one generated tree. The same options always produce a byte-identical tree.
/// </summary>
public sealed record WorstCaseTreeOptions
{
	/// <summary>The smallest tree the worst-case scenario names: the advance duration warning engages above 50,000 files.</summary>
	public const int MINIMUM_SCENARIO_FILE_COUNT = 50_000;

	/// <summary>The global findings cap of the product, restated here because the tool references nothing from it; a test holds the two equal.</summary>
	public const int PRODUCT_GLOBAL_FINDINGS_CAP = 50_000;

	/// <summary>The per-file findings cap of the product, restated for the same reason.</summary>
	public const int PRODUCT_PER_FILE_FINDINGS_CAP = 2_000;

	/// <summary>The names retained per skipped-file category; the oversize count of a scenario run exceeds it.</summary>
	public const int RETAINED_NAMES_PER_CATEGORY = 100;

	/// <summary>The seed a run uses when none is given.</summary>
	public const long DEFAULT_SEED = 20260929;

	/// <summary>The seed that fixes every choice of the tree.</summary>
	public long Seed { get; init; } = DEFAULT_SEED;

	/// <summary>How many files the tree holds; at least the heavy files plus one.</summary>
	public required int FileCount { get; init; }

	/// <summary>The folder the tree is written to. It must not lie inside a repository and must be empty or absent.</summary>
	public required string OutputRoot { get; init; }

	/// <summary>The global cap the heavy files are sized against.</summary>
	public int GlobalFindingsCap { get; init; } = PRODUCT_GLOBAL_FINDINGS_CAP;

	/// <summary>The per-file cap the heavy files are sized against.</summary>
	public int PerFileFindingsCap { get; init; } = PRODUCT_PER_FILE_FINDINGS_CAP;

	/// <summary>How many files above the file-size ceiling are written; each costs more than 2 MiB of disk.</summary>
	public int OversizeFileCount { get; init; }

	/// <summary>
	/// How many of every ten thousand otherwise-ordinary files are PowerShell scripts instead, half with the <c>.ps1</c> extension and half
	/// extensionless with a <c>pwsh</c> shebang. Zero, the default, writes no PowerShell file, so a tree asked for nothing else is
	/// byte for byte the tree without this share.
	/// </summary>
	public int PowerShellScriptBasisPoints { get; init; }

	/// <summary>
	/// How many of every ten thousand otherwise-ordinary files are Visual Basic sources (<c>.vb</c>) instead, drawn on a stream of their own after the PowerShell share, so
	/// every other file keeps its kind. Zero, the default, writes no Visual Basic file, so a tree asked for nothing else is byte for byte the tree without this share.
	/// </summary>
	public int VBNetSourceBasisPoints { get; init; }

	/// <summary>
	/// How many of every ten thousand otherwise-ordinary files are Shell scripts instead, half with the <c>.sh</c> extension and half extensionless with a <c>bash</c>
	/// shebang, drawn on streams of their own after the Visual Basic share, so every other file keeps its kind. Zero, the default, writes no Shell file, so a tree asked
	/// for nothing else is byte for byte the tree without this share.
	/// </summary>
	public int ShellScriptBasisPoints { get; init; }

	/// <summary>
	/// How many of every ten thousand otherwise-ordinary files are JavaScript sources instead, a third with the <c>.js</c> extension, a third with <c>.jsx</c> and a third
	/// extensionless with a <c>node</c> hashbang, drawn on streams of their own after the Shell share, so every other file keeps its kind. Zero, the default, writes no JavaScript file,
	/// so a tree asked for nothing else is byte for byte the tree without this share.
	/// </summary>
	public int JavaScriptSourceBasisPoints { get; init; }

	/// <summary>The scenario's progress cadence: one line per 50,000 files.</summary>
	public const int SCENARIO_PROGRESS_BLOCK_SIZE = 50_000;

	/// <summary>Called with a progress line after every block of files.</summary>
	public Action<string>? ReportProgress { get; init; }

	/// <summary>How many files make up one progress block.</summary>
	public int ProgressBlockSize { get; init; } = SCENARIO_PROGRESS_BLOCK_SIZE;

	/// <summary>How many heavy files lead the tree: enough that their capped findings pass the global cap.</summary>
	public int HeavyFileCount => (GlobalFindingsCap / PerFileFindingsCap) + 1;
}
