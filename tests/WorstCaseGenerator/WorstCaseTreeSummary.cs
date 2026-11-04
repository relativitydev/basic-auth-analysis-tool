namespace MigrateAuth.WorstCase;

/// <summary>What one generation wrote.</summary>
public sealed record WorstCaseTreeSummary
{
	/// <summary>How many files were written.</summary>
	public required long FilesWritten { get; init; }

	/// <summary>How many bytes were written in all.</summary>
	public required long BytesWritten { get; init; }

	/// <summary>How many files of each kind were written.</summary>
	public required IReadOnlyDictionary<WorstCaseFileKind, long> FilesByKind { get; init; }
}
