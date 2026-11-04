using System.Globalization;

using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Writing;

/// <summary>Why a write did not happen. A code rather than prose, as elsewhere.</summary>
public enum ArtifactWriteRefusal
{
	/// <summary>The write happened.</summary>
	None = 0,

	/// <summary>The write boundary refused: destination and repository overlap.</summary>
	DestinationOverlapsRepository = 1,

	/// <summary>The destination could not be created or written to.</summary>
	DestinationNotWritable = 2,

	/// <summary>Every name the collision rule is willing to try was already taken.</summary>
	NameCollisionLimitReached = 3
}

/// <summary>
/// What a write produced. The file name is carried so the tool can state the name it wrote,
/// which it can only do after the collision rule has settled it.
/// </summary>
public sealed record ArtifactWriteResult
{
	/// <summary>Whether a file exists as a result of this call.</summary>
	public required bool Wrote { get; init; }

	/// <summary>The file name written. Empty when nothing was. Never a path.</summary>
	public required string FileNameWritten { get; init; }

	/// <summary>Why nothing was written.</summary>
	public required ArtifactWriteRefusal Refusal { get; init; }

	/// <summary>What to say about it, by label and never by path. Empty on success.</summary>
	public required string RefusalMessage { get; init; }
}

/// <summary>
/// Every file this product writes goes out through here.
///
/// The boundary is checked immediately before the write. Pre-flight already ran the same
/// primitive and that does not weaken this one: the destination can be supplied per invocation, and
/// a security control's enforcement point belongs immediately before the act it controls. A refusal
/// here is exit code 4.
///
/// Every write is temporary-then-rename. A half-written state must read as absent, and the
/// same reasoning applies with more force to the two files a human reads: a process killed
/// during Report otherwise leaves a report truncated mid-section, and a truncated report does
/// not announce itself - it reads as complete and shorter. The temporary is created in the
/// destination folder so that the rename stays within one volume, which is what makes it
/// atomic rather than a copy.
///
/// The collision rule is exclusive creation in a loop and never check-then-create, which is a
/// race. The exclusive creation here is the rename itself: a rename that refuses to overwrite
/// fails as one operation when the name is taken, so no window exists between deciding a name
/// is free and taking it. An existing report is never overwritten and never deleted.
///
/// There is no lock on the destination folder. Concurrent sessions are out of scope, and a process
/// killed while holding a lock would leave an artifact that blocks the next run.
/// </summary>
public sealed class AtomicArtifactWriter
{
	/// <summary>
	/// How many names the collision rule will try before giving up, so the loop against the
	/// filesystem is bounded.
	/// </summary>
	public const int MAXIMUM_NAME_ORDINAL = 1000;

	/// <summary>Said when every name was taken.</summary>
	public const string NAME_COLLISION_LIMIT_MESSAGE_FORMAT =
		"The output destination ({0}) already holds every name today's report could take. Move or remove the existing reports.";

	/// <summary>Said when the destination refused the write for any other reason.</summary>
	public const string DESTINATION_NOT_WRITABLE_MESSAGE_FORMAT =
		"The output destination ({0}) could not be written to.";

	private const string _TEMPORARY_FILE_NAME_SUFFIX = ".writing";
	private const string _MARKDOWN_HEADER_SEPARATOR = "\n\n";

	private readonly RepositoryFolderPath _repository;
	private readonly DocumentationDestinationFolderPath _destination;

	public AtomicArtifactWriter(RepositoryFolderPath repository, DocumentationDestinationFolderPath destination)
	{
		_repository = repository;
		_destination = destination;
	}

	/// <summary>
	/// Writes one of the two shareable artifacts. The handling header is attached here rather
	/// than left to the caller, because both artifacts must carry it and a header a caller may forget
	/// is a header that will be forgotten. A Markdown document gets it as its first line; a
	/// structured-data document carries it as a field its own serializer places, so the body is handed
	/// on untouched.
	/// </summary>
	public ArtifactWriteResult WriteShareableArtifact(OutputArtifactKind kind, DateOnly artifactDate, string body)
	{
		ArgumentNullException.ThrowIfNull(body);

		string content = kind is OutputArtifactKind.MarkdownReport
			? $"{ArtifactHandlingNotice.MarkdownHeader}{_MARKDOWN_HEADER_SEPARATOR}{body}"
			: body;

		return Write(
			ordinal => OutputArtifactNaming.BuildFileName(kind, artifactDate, ordinal),
			temporaryPath => RestrictiveFileCreation.WriteText(temporaryPath, content, FileMode.CreateNew));
	}

	/// <summary>
	/// The same write, with the body produced into the file rather than handed over as a string, so the
	/// artifact is written streaming rather than serialized to a string in memory. The string overload
	/// above remains the right shape for small content, where a delegate buys nothing.
	///
	/// Everything else is the same mechanism: one boundary check, one temporary in the destination
	/// folder, one handling header for Markdown, and the same exclusive-creation rename. The only
	/// difference is where the bytes come from.
	///
	/// The body delegate runs inside the temporary-file window, so a failure part-way through it
	/// leaves the temporary and never a half-written report under a final name. An I/O failure raised
	/// by the delegate is classified like any other write failure.
	/// </summary>
	public ArtifactWriteResult WriteShareableArtifactStreaming(
		OutputArtifactKind kind,
		DateOnly artifactDate,
		Action<TextWriter> writeBody)
	{
		ArgumentNullException.ThrowIfNull(writeBody);

		return Write(
			ordinal => OutputArtifactNaming.BuildFileName(kind, artifactDate, ordinal),
			temporaryPath => StreamTextBody(kind, temporaryPath, writeBody));
	}

	/// <summary>
	/// The streaming write for the data artifact, one layer lower.
	///
	/// It is a separate method rather than an overload of the one above for two reasons. Its
	/// serializer writes UTF-8 bytes directly, so a character writer in between would decode and
	/// re-encode every byte of the largest file this product produces. And the artifact's
	/// handling notice is a FIELD its own serializer places rather than text prepended to the
	/// bytes, so there is no header to write here - naming the kind in the method removes the
	/// invalid combination instead of validating it.
	/// </summary>
	public ArtifactWriteResult WriteStructuredDataArtifactStreaming(DateOnly artifactDate, Action<Stream> writeBody)
	{
		ArgumentNullException.ThrowIfNull(writeBody);

		return Write(
			ordinal => OutputArtifactNaming.BuildFileName(OutputArtifactKind.StructuredDataArtifact, artifactDate, ordinal),
			temporaryPath => StreamByteBody(temporaryPath, writeBody));
	}

	private static void StreamTextBody(OutputArtifactKind kind, string temporaryPath, Action<TextWriter> writeBody)
	{
		using StreamWriter writer = RestrictiveFileCreation.CreateTextWriter(temporaryPath, FileMode.CreateNew);

		if (kind is OutputArtifactKind.MarkdownReport)
		{
			writer.Write(ArtifactHandlingNotice.MarkdownHeader);
			writer.Write(_MARKDOWN_HEADER_SEPARATOR);
		}

		writeBody(writer);
	}

	private static void StreamByteBody(string temporaryPath, Action<Stream> writeBody)
	{
		using FileStream stream = RestrictiveFileCreation.CreateStream(temporaryPath, FileMode.CreateNew);

		writeBody(stream);
	}

	private ArtifactWriteResult Write(Func<int, string> buildFileName, Action<string> writeTemporaryFile)
	{
		WriteBoundaryVerdict verdict = WriteBoundary.Evaluate(_repository, _destination);
		ArtifactWriteResult result;

		if (!verdict.PermitsWriting)
		{
			result = Refused(ArtifactWriteRefusal.DestinationOverlapsRepository, verdict.RefusalMessage);
		}
		else
		{
			result = WriteUnderCollisionRule(buildFileName, writeTemporaryFile);
		}

		return result;
	}

	private ArtifactWriteResult WriteUnderCollisionRule(Func<int, string> buildFileName, Action<string> writeTemporaryFile)
	{
		string temporaryPath = BuildTemporaryPath();
		ArtifactWriteResult result;

		try
		{
			Directory.CreateDirectory(_destination.CanonicalPath);
			writeTemporaryFile(temporaryPath);

			result = RenameToFirstFreeName(buildFileName, temporaryPath);
		}
		catch (IOException)
		{
			result = Refused(ArtifactWriteRefusal.DestinationNotWritable, DescribeNotWritable());
		}
		catch (UnauthorizedAccessException)
		{
			result = Refused(ArtifactWriteRefusal.DestinationNotWritable, DescribeNotWritable());
		}
		catch (NotSupportedException)
		{
			result = Refused(ArtifactWriteRefusal.DestinationNotWritable, DescribeNotWritable());
		}
		finally
		{
			DeleteIfPresent(temporaryPath);
		}

		return result;
	}

	/// <summary>
	/// The collision loop. The rename is the exclusive creation: it refuses to overwrite, so
	/// when the name is taken it fails as a single operation and no window exists in which
	/// another process could take a name this one had already decided was free.
	///
	/// The exception filter inspects the destination AFTER the failure to tell a taken name
	/// from a broken destination. That is not check-then-create - nothing is decided by the
	/// check, it only classifies a failure that has already happened.
	/// </summary>
	private ArtifactWriteResult RenameToFirstFreeName(Func<int, string> buildFileName, string temporaryPath)
	{
		string fileNameWritten = string.Empty;

		for (int ordinal = OutputArtifactNaming.UNSUFFIXED_ORDINAL;
			ordinal <= MAXIMUM_NAME_ORDINAL && fileNameWritten.Length == 0;
			ordinal++)
		{
			string fileName = buildFileName(ordinal);
			string finalPath = Path.Combine(_destination.CanonicalPath, fileName);

			try
			{
				File.Move(temporaryPath, finalPath, overwrite: false);

				fileNameWritten = fileName;
			}
			catch (IOException) when (File.Exists(finalPath))
			{
			}
		}

		ArtifactWriteResult result = fileNameWritten.Length == 0
			? Refused(ArtifactWriteRefusal.NameCollisionLimitReached, DescribeCollisionLimit())
			: new ArtifactWriteResult
			{
				Wrote = true,
				FileNameWritten = fileNameWritten,
				Refusal = ArtifactWriteRefusal.None,
				RefusalMessage = string.Empty
			};

		return result;
	}

	private string BuildTemporaryPath()
	{
		string temporaryName = $"{OutputArtifactNaming.FILE_NAME_STEM}.{Guid.NewGuid():N}{_TEMPORARY_FILE_NAME_SUFFIX}";

		return Path.Combine(_destination.CanonicalPath, temporaryName);
	}

	private string DescribeNotWritable() =>
		string.Format(
			CultureInfo.InvariantCulture,
			DESTINATION_NOT_WRITABLE_MESSAGE_FORMAT,
			_destination.Label);

	private string DescribeCollisionLimit() =>
		string.Format(
			CultureInfo.InvariantCulture,
			NAME_COLLISION_LIMIT_MESSAGE_FORMAT,
			_destination.Label);

	private static ArtifactWriteResult Refused(ArtifactWriteRefusal refusal, string refusalMessage) =>
		new()
		{
			Wrote = false,
			FileNameWritten = string.Empty,
			Refusal = refusal,
			RefusalMessage = refusalMessage
		};

	private static void DeleteIfPresent(string temporaryPath)
	{
		try
		{
			File.Delete(temporaryPath);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
