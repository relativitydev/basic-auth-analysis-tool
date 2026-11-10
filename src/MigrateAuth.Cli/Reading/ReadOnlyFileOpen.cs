namespace MigrateAuth.Cli.Reading;

/// <summary>How an attempt to open a file for reading ended.</summary>
public enum FileOpenResult
{
	/// <summary>The file is open and its stream is readable.</summary>
	Opened = 0,

	/// <summary>
	/// The open was refused. Whether by an access control list, a file mode, a mandatory lock or
	/// anything else is NOT reported and NOT inquired into - the outcome is that this file cannot
	/// be read, which is the whole of what the report needs.
	/// </summary>
	Unreadable = 1,

	/// <summary>
	/// The file is not there. It is kept apart from <see cref="Unreadable"/> because the two mean
	/// different things to different callers, and the CALLER assigns the skipped-item category:
	/// during the walk a file that vanished between enumeration and opening is a hole like any
	/// other, while in Analysis a candidate path recorded during Scan that is no longer there is
	/// MissingCandidate, which <c>Pipeline/AnalyzeStage.cs</c> raises. Deciding
	/// that here would force this reader to raise a category it does not own.
	/// </summary>
	Missing = 2,

	/// <summary>The platform refused the path for its length.</summary>
	PathTooLong = 3
}

/// <summary>
/// THE ONLY PLACE THIS PRODUCT OPENS A FILE IT DID NOT WRITE.
///
/// It exists as one function for the same reason the write boundary does: the two properties
/// below have to hold at every read in the product, and a rule honoured from memory at each call
/// site is a rule that gets forgotten once.
///
/// READ ACCESS, AND REQUESTING WRITE ACCESS ANYWHERE IS A DEFECT. The tool never modifies the
/// repository it audits, and asking the operating system for a right it will not use is the
/// difference between a promise and a permission.
///
/// SHARING THAT PERMITS ANOTHER PROCESS TO WRITE AND TO DELETE, which is the subtler half and is
/// equally forbidden to tighten. A restrictive share mode LOCKS A DEVELOPER OUT OF THEIR OWN
/// FILE while this tool reads it - their editor cannot save, their version control client cannot
/// switch branches - and that is interference with the audited repository even though not one
/// byte is written to it. The boundary is about interference, not about bytes.
///
/// The consequence of permitting it is accepted rather than overlooked: a file being written
/// while it is read can be read torn. A torn read produces a finding on a line that was never
/// committed, or misses one, in a repository somebody is editing DURING an assessment - against
/// locking out the editor of every file in a half-million-file walk, which is the worse of the
/// two by a distance.
/// </summary>
public static class ReadOnlyFileOpen
{
	/// <summary>Read. Never write, never read-write, at any call site.</summary>
	public const FileAccess PERMITTED_ACCESS = FileAccess.Read;

	/// <summary>
	/// Another process may write to this file and may delete it while we hold it open. This is
	/// the least exclusive share mode there is, and the value is a constant so that a test can
	/// assert on it rather than on a comment.
	/// </summary>
	public const FileShare PERMITTED_SHARE = FileShare.ReadWrite | FileShare.Delete;

	/// <summary>
	/// Attempts the open and reports what happened. NOTHING IS INSPECTED BEFOREHAND: no
	/// permission is computed, no existence is checked. A check before the open would be a race
	/// window in every case and wrong in some, and the attempt reports the outcome directly.
	/// </summary>
	public static FileOpenResult Attempt(string absolutePath, out FileStream? stream)
	{
		FileOpenResult result;

		stream = null;

		try
		{
			stream = new FileStream(absolutePath, BuildOptions());
			result = FileOpenResult.Opened;
		}
		catch (FileNotFoundException)
		{
			result = FileOpenResult.Missing;
		}
		catch (DirectoryNotFoundException)
		{
			result = FileOpenResult.Missing;
		}
		catch (PathTooLongException)
		{
			result = FileOpenResult.PathTooLong;
		}
		catch (UnauthorizedAccessException)
		{
			result = FileOpenResult.Unreadable;
		}
		catch (IOException)
		{
			result = FileOpenResult.Unreadable;
		}
		catch (NotSupportedException)
		{
			result = FileOpenResult.Unreadable;
		}
		catch (ArgumentException)
		{
			result = FileOpenResult.Unreadable;
		}

		return result;
	}

	private static FileStreamOptions BuildOptions()
	{
		return new FileStreamOptions
		{
			Mode = FileMode.Open,
			Access = PERMITTED_ACCESS,
			Share = PERMITTED_SHARE,
			Options = FileOptions.SequentialScan
		};
	}
}
