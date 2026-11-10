using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Reading;

/// <summary>
/// THE WALK WRITTEN BY HAND, DIRECTORY BY DIRECTORY, EACH WITH ITS OWN FAILURE HANDLING.
///
/// RECURSIVE ENUMERATION IS NOT USED. The framework's recursive overload raises on the first
/// unreadable directory and, because enumeration is lazy, that ABORTS THE ENTIRE WALK. The option
/// that suppresses the exception instead skips inaccessible entries SILENTLY: no exception, no
/// counter, no trace. Neither can produce a categorized count of skipped items.
///
/// So this class holds its own stack of directories still to visit, and takes one directory's
/// entries at a time. A denial is then a local event: it is caught where it happened, recorded
/// as ONE DIRECTORY OF UNKNOWN CONTENTS, and the stack still holds every other directory, so the
/// walk continues into the parts of the tree that remain reachable. That is the whole mechanism
/// behind "a denial never aborts the walk and never disappears silently".
///
/// IT STREAMS. Files are yielded as they are found, and the only thing materialized at any
/// moment is the entry list of the single directory being processed plus the stack of
/// directories still pending - neither of which grows with the size of the repository in the way
/// a list of every file would.
///
/// WHAT IT DOES NOT DO: decide what a candidate is, count lines, open anything. It reaches
/// files, and the reader opens them.
/// </summary>
public sealed class RepositoryWalk
{
	private readonly RepositoryFolderPath _repository;
	private readonly EmissionPoint _emissionPoint;
	private readonly List<RepositoryRelativePath> _namedExcludedFolders = [];
	private int _excludedFolderCount;

	public RepositoryWalk(RepositoryFolderPath repository, EmissionPoint emissionPoint)
	{
		ArgumentNullException.ThrowIfNull(emissionPoint);

		_repository = repository;
		_emissionPoint = emissionPoint;
	}

	/// <summary>
	/// Every file under the root, in an order fixed by the stack and by the platform's own
	/// enumeration. Ordering of the OUTPUT is applied where output is
	/// produced, not here - a walk that sorted would have to hold the tree to do it.
	/// </summary>
	public IEnumerable<WalkedFile> Enumerate()
	{
		_namedExcludedFolders.Clear();
		_excludedFolderCount = 0;

		Stack<string> pendingDirectories = new();
		pendingDirectories.Push(_repository.CanonicalPath);

		while (pendingDirectories.Count > 0)
		{
			string directoryPath = pendingDirectories.Pop();

			foreach (WalkedFile file in VisitDirectory(directoryPath, pendingDirectories))
			{
				yield return file;
			}
		}
	}

	/// <summary>
	/// The folders the last enumeration met and did not enter: a count of every one and a bounded,
	/// ordered list. An excluded folder is a policy of the tool and never a skipped item, so it is
	/// kept here and reaches no emission sink. Complete only once <see cref="Enumerate"/> has run to
	/// its end.
	/// </summary>
	public ExcludedFolderSummary ExcludedFolders()
	{
		return ExcludedFolderSummary.From(_excludedFolderCount, _namedExcludedFolders);
	}

	/// <summary>
	/// One directory: list it, classify what it holds, push the subdirectories worth visiting,
	/// and hand back its files. A failure to list it is recorded here and reaches no caller,
	/// which is what keeps it from ending the walk.
	/// </summary>
	private List<WalkedFile> VisitDirectory(string directoryPath, Stack<string> pendingDirectories)
	{
		List<WalkedFile> files = [];
		DirectoryListing listing = ListEntries(directoryPath, out FileSystemInfo[] entries);

		if (listing is DirectoryListing.Listed)
		{
			foreach (FileSystemInfo entry in entries)
			{
				Classify(entry, files, pendingDirectories);
			}
		}
		else
		{
			Record(
				directoryPath,
				listing is DirectoryListing.PathTooLong
					? SkippedItemCategory.PathTooLong
					: SkippedItemCategory.UnlistableDirectory);
		}

		return files;
	}

	/// <summary>
	/// DENIAL IS DISCOVERED BY ATTEMPTING THE OPERATION, NEVER BY INSPECTING PERMISSIONS. No
	/// access control list is read, no file mode is read, and no platform permission API is
	/// called - here or anywhere else in the walk. Computing an effective permission on Windows
	/// means resolving group membership and inherited deny entries and is commonly got wrong;
	/// between checking and listing there is a window in which the answer changes; a platform
	/// permission API would be a platform-conditional dependency inside a binary required to be
	/// single and ahead-of-time compiled; and the outcome that matters - did the listing succeed
	/// - is exactly what attempting reports.
	///
	/// The entries of one directory are materialized rather than streamed, because a lazy
	/// enumerator can raise PART WAY THROUGH a directory, and half a directory silently returned
	/// is the failure this whole class exists to avoid. The cost is bounded by one directory.
	/// </summary>
	private static DirectoryListing ListEntries(string directoryPath, out FileSystemInfo[] entries)
	{
		DirectoryListing listing;

		try
		{
			entries = new DirectoryInfo(directoryPath).GetFileSystemInfos();
			listing = DirectoryListing.Listed;
		}
		catch (PathTooLongException)
		{
			entries = [];
			listing = DirectoryListing.PathTooLong;
		}
		catch (UnauthorizedAccessException)
		{
			entries = [];
			listing = DirectoryListing.Unlistable;
		}
		catch (DirectoryNotFoundException)
		{
			entries = [];
			listing = DirectoryListing.Unlistable;
		}
		catch (IOException)
		{
			entries = [];
			listing = DirectoryListing.Unlistable;
		}

		return listing;
	}

	/// <summary>
	/// What one entry is, in the order the tests have to be applied: a link before anything else,
	/// because a link to a directory is still a directory and would otherwise be descended into.
	/// </summary>
	private void Classify(FileSystemInfo entry, List<WalkedFile> files, Stack<string> pendingDirectories)
	{
		if (IsLink(entry))
		{
			Record(entry.FullName, SkippedItemCategory.LinkNotFollowed);
		}
		else if (entry is DirectoryInfo directory)
		{
			if (WalkExclusions.IsExcluded(directory.Name))
			{
				RecordExcludedFolder(directory.FullName);
			}
			else
			{
				pendingDirectories.Push(directory.FullName);
			}
		}
		else
		{
			Admit(entry.FullName, files);
		}
	}

	/// <summary>
	/// Counts an excluded folder and keeps its relative path. A path the platform will not express
	/// relative to the root is counted without being named, as a skipped item's is.
	/// </summary>
	private void RecordExcludedFolder(string absolutePath)
	{
		_excludedFolderCount++;

		if (RepositoryRelativePath.TryFromScannedPath(_repository, absolutePath, out RepositoryRelativePath path))
		{
			_namedExcludedFolders.Add(path);
		}
	}

	/// <summary>
	/// DIRECTORY LINKS ARE NEVER FOLLOWED - no symbolic link, no junction, no reparse point -
	/// and any one of three reasons would be enough. A link pointing at an ancestor makes the
	/// walk non-terminating. A link pointing outside the tree would make the tool read and report
	/// material outside the audited scope. And a path outside the tree cannot be expressed as
	/// repository-relative, so the report would have to emit a traversal or an absolute path.
	///
	/// The test is the reparse-point attribute, read from the value the enumeration already
	/// returned rather than from a fresh probe, and it is applied to FILE entries as well as
	/// directory ones: the second and third reasons above apply verbatim to a
	/// file link, whose target is equally free to sit outside the tree. Not following it is the
	/// safe direction, and it is counted rather than dropped.
	/// </summary>
	private static bool IsLink(FileSystemInfo entry)
	{
		return (entry.Attributes & FileAttributes.ReparsePoint) != 0;
	}

	private void Admit(string absolutePath, List<WalkedFile> files)
	{
		if (RepositoryRelativePath.TryFromScannedPath(_repository, absolutePath, out RepositoryRelativePath path))
		{
			files.Add(new WalkedFile(path, absolutePath));
		}
		else
		{
			Record(absolutePath, SkippedItemCategory.PathTooLong);
		}
	}

	/// <summary>
	/// THE DENIAL'S SHAPE, AND IT OBEYS THE DATA INVARIANT. The path is emitted relative to the
	/// scanned root like every other path, and no framework exception text travels with it -
	/// which on at least one platform embeds the absolute path by construction. Nothing here
	/// formats a message: the record is a category and a path, and how it is said belongs to the
	/// report.
	///
	/// A path that cannot be expressed relative to the root is recorded WITHOUT one rather than
	/// dropped. Inside a walk that follows no link, the only thing that gets here is a path the
	/// platform will not canonicalize, and the count is the part that must not lie.
	/// </summary>
	private void Record(string absolutePath, SkippedItemCategory category)
	{
		bool named = RepositoryRelativePath.TryFromScannedPath(
			_repository,
			absolutePath,
			out RepositoryRelativePath path);

		_emissionPoint.Emit(new EmittedFact
		{
			Category = EmissionCategory.SkippedItem,
			SkippedItem = category,
			Path = named ? path : default
		});
	}

	/// <summary>How listing one directory ended. Three outcomes, two of them skipped items.</summary>
	private enum DirectoryListing
	{
		Listed = 0,
		Unlistable = 1,
		PathTooLong = 2
	}
}
