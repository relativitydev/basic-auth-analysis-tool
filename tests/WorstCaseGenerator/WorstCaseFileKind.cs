namespace MigrateAuth.WorstCase;

/// <summary>What a planned file is for. Every file of the tree has exactly one.</summary>
public enum WorstCaseFileKind
{
	/// <summary>A C# file holding more High constructs than the per-file findings cap admits; these lead the path order and exhaust the global budget.</summary>
	Heavy = 0,

	/// <summary>A C# file holding a few High constructs, some carrying an interactive or a fixed-credential signal.</summary>
	HighTail = 1,

	/// <summary>A C# file of ordinary code with no construct; a share of these still mention a candidate anchor word.</summary>
	Filler = 2,

	/// <summary>A file of a type no analyzer claims, so it is enumerated and never read as a candidate.</summary>
	NonCandidate = 3,

	/// <summary>A C# file that is not valid UTF-8, skipped as undecodable.</summary>
	Undecodable = 4,

	/// <summary>A C# file with one line above the line-length ceiling, skipped as a whole.</summary>
	OverLongLine = 5,

	/// <summary>A C# file above the file-size ceiling, skipped without being read.</summary>
	Oversize = 6,

	/// <summary>A PowerShell script with the <c>.ps1</c> extension, ordinary code, a share of which mentions a candidate anchor word. Only a tree asked for a PowerShell share holds any.</summary>
	PowerShellScript = 7,

	/// <summary>An extensionless PowerShell script whose first line is a <c>pwsh</c> shebang, so only its first line attributes it. Only a tree asked for a PowerShell share holds any.</summary>
	PowerShellShebangScript = 8
}
