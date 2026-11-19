namespace MigrateAuth.Cli.Writing;

/// <summary>The length and SHA-256 of the bytes of one file, taken while those bytes were written.</summary>
/// <param name="Length">The number of bytes written.</param>
/// <param name="Sha256">The SHA-256 of exactly those bytes.</param>
public readonly record struct ReportContentDigest(long Length, byte[] Sha256);

/// <summary>
/// The Markdown report this process wrote: its bare file name, its length and the SHA-256 of its bytes.
/// Never a path - the folder comes from the destination the writer was built with.
/// </summary>
/// <param name="FileName">The bare file name the collision rule settled.</param>
/// <param name="Length">The length in bytes of the report as written.</param>
/// <param name="Sha256">The SHA-256 of the report as written.</param>
public sealed record WrittenReport(string FileName, long Length, byte[] Sha256);

/// <summary>
/// The in-process record of the LAST MARKDOWN REPORT THIS PROCESS WROTE. It exists so that a later step
/// can replace that report with itself plus one more section and prove, from length and SHA-256, that the
/// bytes it is about to copy are the bytes this process wrote. Nothing is persisted, and a process that did
/// not write a report has an empty holder, which the append refuses.
///
/// Only the writer's Markdown REPORT write records here: a prompt pack, the data artifact and the string
/// overload never touch it. A failed report write leaves the previous value; a failed recording clears it,
/// so a stale value can never be compared against a newer report.
/// </summary>
public sealed class ReportWriteHolder
{
	private const string _PARENT_SEGMENT = "..";
	private const string _NOT_A_BARE_FILE_NAME_MESSAGE = "A recorded report name must be a bare file name.";

	/// <summary>The last report this process wrote, or <c>null</c> when none is known.</summary>
	public WrittenReport? Current { get; private set; }

	/// <summary>
	/// Whether the value was cleared after a report had been written and not recorded since: the report exists
	/// but this process could not record it, which the stage says in its own words, apart from a run that wrote
	/// no report at all.
	/// </summary>
	public bool RecordingWasLost { get; private set; }

	/// <summary>
	/// Replaces the value with the report just written under <paramref name="fileName"/>. The name must be a bare
	/// file name: a rooted name, one with a directory separator or <c>..</c>, or one with a character a file name
	/// cannot hold is refused with an <see cref="ArgumentException"/>, because it is later joined to the destination.
	/// </summary>
	public void Record(string fileName, ReportContentDigest digest)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
		ArgumentNullException.ThrowIfNull(digest.Sha256);

		if (!IsBareFileName(fileName))
		{
			throw new ArgumentException(_NOT_A_BARE_FILE_NAME_MESSAGE, nameof(fileName));
		}

		Current = new WrittenReport(fileName, digest.Length, digest.Sha256);
		RecordingWasLost = false;
	}

	private static bool IsBareFileName(string fileName) =>
		!Path.IsPathRooted(fileName)
		&& !fileName.Contains(_PARENT_SEGMENT, StringComparison.Ordinal)
		&& fileName.IndexOfAny(RefusedFileNameCharacters.ALL) < 0;

	/// <summary>Forgets the value, so that the next append is refused rather than compared against an older report.</summary>
	public void Clear()
	{
		Current = null;
		RecordingWasLost = true;
	}
}

/// <summary>Why a report append did not happen. A code rather than prose, as elsewhere.</summary>
public enum ReportAppendRefusal
{
	/// <summary>The section was appended.</summary>
	None = 0,

	/// <summary>The holder is empty: this process wrote no report, or could not record it.</summary>
	NotWrittenByThisProcess = 1,

	/// <summary>The write boundary refused: destination and repository overlap.</summary>
	OutsideBoundary = 2,

	/// <summary>The report is now a symbolic link, a junction or a directory.</summary>
	ReportReplacedOrLinked = 3,

	/// <summary>The report's length, hash or last-write time differs from what this process wrote.</summary>
	ReportChanged = 4,

	/// <summary>The temporary could not be created or written, or the replacement failed.</summary>
	NotWritable = 5
}

/// <summary>The points of <see cref="AtomicArtifactWriter.AppendToReport"/> at which a failure can be injected.</summary>
public enum ReportAppendStep
{
	/// <summary>Before the temporary file is created.</summary>
	TemporaryCreation = 0,

	/// <summary>After each block of the original has been copied.</summary>
	MidCopy = 1,

	/// <summary>Before the temporary replaces the report.</summary>
	Rename = 2
}

/// <summary>What an append produced. The original report is untouched whenever <see cref="Appended"/> is false.</summary>
public sealed record ReportAppendResult
{
	/// <summary>Whether the report now carries the new section.</summary>
	public required bool Appended { get; init; }

	/// <summary>Why nothing was appended.</summary>
	public required ReportAppendRefusal Refusal { get; init; }

	/// <summary>The bare file name of the report, empty when nothing was appended.</summary>
	public required string FileName { get; init; }
}
