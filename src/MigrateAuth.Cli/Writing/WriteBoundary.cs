using System.Globalization;

using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Writing;

/// <summary>How two canonical paths sit relative to one another. Four cases and no more.</summary>
public enum PathOverlap
{
	/// <summary>Neither contains the other. The only outcome that permits a write.</summary>
	Separate = 0,

	/// <summary>The two paths are the same folder.</summary>
	SamePath = 1,

	/// <summary>The destination sits inside the repository being assessed.</summary>
	DestinationInsideRepository = 2,

	/// <summary>The repository being assessed sits inside the destination.</summary>
	RepositoryInsideDestination = 3
}

/// <summary>
/// What the boundary decided, and what to say when it refused. The message names both
/// locations by label, because no absolute path appears in anything the product emits -
/// a refusal message included.
/// </summary>
public sealed record WriteBoundaryVerdict
{
	/// <summary>How the two paths sit relative to one another.</summary>
	public required PathOverlap Overlap { get; init; }

	/// <summary>Empty when the boundary permitted the write.</summary>
	public required string RefusalMessage { get; init; }

	/// <summary>Only <see cref="PathOverlap.Separate"/> permits writing.</summary>
	public bool PermitsWriting => Overlap is PathOverlap.Separate;
}

/// <summary>
/// The write boundary: the tool never writes inside the repository it audits, and never
/// audits a repository that sits inside where it writes.
///
/// One implementation, three call sites. The comparison runs at Pre-flight
/// Validation as well as immediately before each write, and it is one primitive
/// rather than two because the early check exists only to spare the user a whole run - it is
/// not a second boundary and must not be able to disagree with the first. The third call site
/// is the read path: the state file is untrusted input, a candidate path escaping the
/// repository root is schema-valid, and analysis would otherwise open it. Nothing here touches
/// write machinery, so pre-flight can call it before any stage has run, against a destination
/// that is known but not yet written to.
///
/// The comparison is case-insensitive on every platform. Case sensitivity is a property of the
/// volume, not of the operating system: macOS is case-insensitive by default and case-sensitive on
/// some volumes, Linux filesystems vary, and Windows is case-insensitive but case-preserving. Nothing
/// derivable from the running platform answers the question, and probing the volume at runtime would
/// make a security boundary depend on a filesystem round trip. So the fold is applied everywhere,
/// using ordinal case folding against the invariant culture, which keeps the comparison itself
/// platform-independent - no platform API participates in it.
///
/// The asymmetry is the safe direction. On a case-sensitive volume, two paths differing only
/// in case are genuinely different, so folding them together can refuse a legitimate write -
/// visible, recoverable, and reported with exit code 4. Comparing case-sensitively on a
/// case-insensitive volume would permit a write inside the audited repository - silent, and
/// the single thing this boundary exists to stop.
///
/// And it is not a string prefix test. A prefix comparison fails on case differences, on
/// sibling folders with overlapping names, on traversals, on short names, on drives mapped to
/// shares, and on directory links resolving inside the tree. Both paths are canonicalized with
/// link resolution and then compared segment by segment.
/// </summary>
public static class WriteBoundary
{
	/// <summary>Every refusal here is the same failure, and the exit-code contract numbers it four.</summary>
	public const ExitCode REFUSAL_EXIT_CODE = ExitCode.OutputDestinationNotWritable;

	/// <summary>Said when the destination and the repository are one folder.</summary>
	public const string DESTINATION_COINCIDES_MESSAGE_FORMAT =
		"The output destination is the repository being assessed ({0}). This tool never writes inside the repository it audits.";

	/// <summary>Said when the destination sits under the repository.</summary>
	public const string DESTINATION_INSIDE_REPOSITORY_MESSAGE_FORMAT =
		"The output destination ({0}) is inside the repository being assessed ({1}). This tool never writes inside the repository it audits.";

	/// <summary>Said when the repository sits under the destination.</summary>
	public const string REPOSITORY_INSIDE_DESTINATION_MESSAGE_FORMAT =
		"The repository being assessed ({0}) is inside the output destination ({1}). This tool never writes inside the repository it audits.";

	/// <summary>
	/// What to do about it. Split from the refusal itself because all three refusals share it,
	/// and because the early check says what to do rather than only what is wrong - the collision is
	/// what happens when the tool is run from inside the repository, which is the most natural
	/// invocation there is.
	/// </summary>
	public const string REFUSAL_REMEDY_MESSAGE =
		"Run it from another folder, or supply --documentationDestinationFolderPath with a destination outside the repository.";

	/// <summary>
	/// The boundary. Called at Pre-flight Validation against the destination then in effect,
	/// and again immediately before each write, which stays the enforcement point.
	/// </summary>
	public static WriteBoundaryVerdict Evaluate(
		RepositoryFolderPath repository,
		DocumentationDestinationFolderPath destination)
	{
		PathOverlap overlap = ComparePaths(repository.CanonicalPath, destination.CanonicalPath);

		return new WriteBoundaryVerdict
		{
			Overlap = overlap,
			RefusalMessage = DescribeRefusal(overlap, repository.Label, destination.Label)
		};
	}

	/// <summary>
	/// The comparison itself, over two paths that are canonicalized here so that a caller
	/// holding raw text cannot accidentally compare unresolved forms.
	/// </summary>
	public static PathOverlap ComparePaths(string repositoryPath, string destinationPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

		string[] repositorySegments = CanonicalPath.SplitIntoSegments(CanonicalPath.Resolve(repositoryPath));
		string[] destinationSegments = CanonicalPath.SplitIntoSegments(CanonicalPath.Resolve(destinationPath));

		return CompareSegments(repositorySegments, destinationSegments);
	}

	/// <summary>
	/// The read path. Every candidate is put through the same primitive before anything is
	/// opened, and the outcome it must produce is the one that refuses a write: a path this
	/// tool may read is exactly a path contained in the repository being assessed.
	///
	/// The candidate is normalized but not link-resolved. The walk does not follow links, so a link
	/// inside the repository is already a skipped item rather than something this check must catch; and
	/// a stat per candidate would put a filesystem round trip in the inner loop of a stage that may run
	/// over half a million files.
	/// </summary>
	public static bool PermitsReading(RepositoryFolderPath repository, string candidatePath)
	{
		bool permitted = false;

		if (!string.IsNullOrWhiteSpace(candidatePath))
		{
			try
			{
				string absoluteCandidate = Path.GetFullPath(candidatePath, repository.CanonicalPath);
				string[] repositorySegments = CanonicalPath.SplitIntoSegments(repository.CanonicalPath);
				string[] candidateSegments = CanonicalPath.SplitIntoSegments(absoluteCandidate);

				permitted = CompareSegments(repositorySegments, candidateSegments)
					is PathOverlap.DestinationInsideRepository;
			}
			catch (ArgumentException)
			{
				permitted = false;
			}
			catch (NotSupportedException)
			{
				permitted = false;
			}
			catch (PathTooLongException)
			{
				permitted = false;
			}
			catch (IOException)
			{
				permitted = false;
			}
		}

		return permitted;
	}

	/// <summary>
	/// Segment by segment, folded case, ordinal. <see cref="StringComparer.OrdinalIgnoreCase"/>
	/// is the invariant culture's ordinal case folding: it consults no locale data and no
	/// platform API, so the same two paths compare the same way on every machine.
	/// </summary>
	private static PathOverlap CompareSegments(string[] repositorySegments, string[] candidateSegments)
	{
		int sharedLength = Math.Min(repositorySegments.Length, candidateSegments.Length);
		bool sharesPrefix = true;

		for (int position = 0; position < sharedLength && sharesPrefix; position++)
		{
			sharesPrefix = StringComparer.OrdinalIgnoreCase.Equals(
				repositorySegments[position],
				candidateSegments[position]);
		}

		PathOverlap overlap = PathOverlap.Separate;

		if (sharesPrefix && repositorySegments.Length == candidateSegments.Length)
		{
			overlap = PathOverlap.SamePath;
		}
		else if (sharesPrefix && repositorySegments.Length < candidateSegments.Length)
		{
			overlap = PathOverlap.DestinationInsideRepository;
		}
		else if (sharesPrefix)
		{
			overlap = PathOverlap.RepositoryInsideDestination;
		}

		return overlap;
	}

	private static string DescribeRefusal(PathOverlap overlap, string repositoryLabel, string destinationLabel)
	{
		string refusalMessage = overlap switch
		{
			PathOverlap.SamePath => Format(DESTINATION_COINCIDES_MESSAGE_FORMAT, repositoryLabel, destinationLabel),
			PathOverlap.DestinationInsideRepository =>
				Format(DESTINATION_INSIDE_REPOSITORY_MESSAGE_FORMAT, destinationLabel, repositoryLabel),
			PathOverlap.RepositoryInsideDestination =>
				Format(REPOSITORY_INSIDE_DESTINATION_MESSAGE_FORMAT, repositoryLabel, destinationLabel),
			_ => string.Empty
		};

		return refusalMessage.Length == 0 ? string.Empty : $"{refusalMessage} {REFUSAL_REMEDY_MESSAGE}";
	}

	private static string Format(string messageFormat, string firstLabel, string secondLabel) =>
		string.Format(CultureInfo.InvariantCulture, messageFormat, firstLabel, secondLabel);
}
