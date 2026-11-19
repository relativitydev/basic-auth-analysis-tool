using System.Globalization;
using System.Security.Cryptography;

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

	/// <summary>
	/// The output folder as the user supplied it (<see cref="DocumentationDestinationFolderPath.ConsoleDisplayText"/>),
	/// so the file named above can be found. For the console only: never the canonical path, and never
	/// written to the report or the data artifact. Empty when nothing was written.
	/// </summary>
	public string DestinationConsoleText { get; init; } = string.Empty;

	/// <summary>Why nothing was written.</summary>
	public required ArtifactWriteRefusal Refusal { get; init; }

	/// <summary>What to say about it, naming the output folder as the user supplied it and never by its canonical path. Empty on success.</summary>
	public required string RefusalMessage { get; init; }
}

/// <summary>
/// Every file this product writes goes out through here, and each rule it applies belongs to the write boundary.
///
/// THE BOUNDARY IS CHECKED IMMEDIATELY BEFORE THE WRITE. Pre-flight already ran the same
/// primitive (the containment rule) and that does not weaken this one: the destination can be supplied per
/// invocation, and a security control's enforcement point belongs immediately before the act
/// it controls. A refusal here is exit code 4.
///
/// EVERY WRITE IS TEMPORARY-THEN-RENAME. A half-written state must read as absent, and the
/// same reasoning applies with more force to the two files a human reads: a process killed
/// during Report otherwise leaves a report truncated mid-section, and A TRUNCATED REPORT DOES
/// NOT ANNOUNCE ITSELF - it reads as complete and shorter. The temporary is created in the
/// destination folder so that the rename stays within one volume, which is what makes it
/// atomic rather than a copy.
///
/// THE COLLISION RULE IS EXCLUSIVE CREATION IN A LOOP AND NEVER CHECK-THEN-CREATE, which is a
/// race. The exclusive creation here is the rename itself: a rename that refuses to overwrite
/// fails as one operation when the name is taken, so no window exists between deciding a name
/// is free and taking it. An existing report is never overwritten and never deleted.
///
/// THERE IS NO LOCK ON THE DESTINATION FOLDER. Concurrent sessions are out of scope, and a process
/// killed while holding a lock would leave an artifact that blocks the next run.
/// </summary>
public sealed class AtomicArtifactWriter : IReportAppender
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

	/// <summary>Said when every name was taken for a prompt pack.</summary>
	public const string PACK_NAME_COLLISION_LIMIT_MESSAGE_FORMAT =
		"The output destination ({0}) already holds every name this prompt pack could take. Move or remove the existing prompt packs.";

	private const string _TEMPORARY_FILE_NAME_SUFFIX = ".writing";
	private const string _MARKDOWN_HEADER_SEPARATOR = "\n\n";
	private const string _PACK_STEM_NOT_A_FILE_NAME_MESSAGE = "A prompt pack stem must be a bare file name.";
	private const string _PROMPT_PACK_OPERATION_MESSAGE = "A prompt pack is written through WritePromptPackStreaming.";
	private const char _LINE_FEED = '\n';
	private const int _COPY_BUFFER_SIZE_BYTES = 81920;

	private readonly RepositoryFolderPath _repository;
	private readonly DocumentationDestinationFolderPath _destination;
	private readonly ReportWriteHolder? _reportHolder;
	private readonly Func<IncrementalHash>? _hashFactory;
	private readonly Action<ReportAppendStep>? _appendStepHook;

	/// <summary>
	/// Builds the writer for one repository and one destination.
	/// </summary>
	/// <param name="repository">The assessed repository, which the write boundary keeps apart from the destination.</param>
	/// <param name="destination">The documentation destination.</param>
	/// <param name="reportHolder">
	/// Optional. When given, the streaming Markdown REPORT write records the report's name, length and
	/// SHA-256 in it after a successful write; with none, the report's bytes and the write's behaviour are
	/// exactly what they are without this parameter.
	/// </param>
	/// <param name="hashFactory">Optional seam that creates the hash (SHA-256 by default), for a hasher that throws.</param>
	/// <param name="appendStepHook">
	/// Optional seam called as <see cref="AppendToReport"/> reaches the creation of its temporary, the middle of
	/// the copy and the replacement, so that a failure can be injected there. With none, nothing is called and the
	/// behaviour is unchanged.
	/// </param>
	public AtomicArtifactWriter(
		RepositoryFolderPath repository,
		DocumentationDestinationFolderPath destination,
		ReportWriteHolder? reportHolder = null,
		Func<IncrementalHash>? hashFactory = null,
		Action<ReportAppendStep>? appendStepHook = null)
	{
		_repository = repository;
		_destination = destination;
		_reportHolder = reportHolder;
		_hashFactory = hashFactory;
		_appendStepHook = appendStepHook;
	}

	/// <summary>
	/// Writes one of the two shareable artifacts. The handling header is attached here rather
	/// than left to the caller, because BOTH artifacts are required to carry it and a header
	/// a caller may forget is a header that will be forgotten. A Markdown document gets it as
	/// its first line; a structured-data document carries it as a field its own serializer
	/// places, so the body is handed on untouched.
	///
	/// THIS OVERLOAD DOES NOT RECORD THE REPORT IN THE REPORT WRITE HOLDER, even when the writer was
	/// built with one: only the streaming Markdown report write does. A report written here cannot later
	/// be appended to, and the holder keeps whatever it held. A prompt pack is not written here either.
	/// </summary>
	public ArtifactWriteResult WriteShareableArtifact(OutputArtifactKind kind, DateOnly artifactDate, string body)
	{
		ArgumentNullException.ThrowIfNull(body);
		ThrowIfPromptPack(kind);

		string content = kind is OutputArtifactKind.MarkdownReport
			? $"{ArtifactHandlingNotice.MarkdownHeader}{_MARKDOWN_HEADER_SEPARATOR}{body}"
			: body;

		return Write(
			ordinal => OutputArtifactNaming.BuildFileName(kind, artifactDate, ordinal),
			temporaryPath => RestrictiveFileCreation.WriteText(temporaryPath, content, FileMode.CreateNew));
	}

	/// <summary>
	/// The same write, with the body produced INTO the file rather than handed over as a string.
	///
	/// The artifact is written streaming, not serialized to a string in memory. The method above
	/// remains the right shape for small content, where a delegate buys nothing.
	///
	/// EVERYTHING ELSE IS THE SAME MECHANISM and deliberately so: one boundary check, one
	/// temporary in the destination folder, one handling header for Markdown, and the same
	/// exclusive-creation rename. The only difference is where the bytes come from.
	///
	/// The body delegate runs INSIDE the temporary-file window, so a failure part-way through it
	/// leaves the temporary and never a half-written report under a final name - which is the
	/// property a truncated report cannot announce for itself. An I/O failure raised
	/// by the delegate is classified like any other write failure.
	/// </summary>
	public ArtifactWriteResult WriteShareableArtifactStreaming(
		OutputArtifactKind kind,
		DateOnly artifactDate,
		Action<TextWriter> writeBody)
	{
		ArgumentNullException.ThrowIfNull(writeBody);
		ThrowIfPromptPack(kind);

		ArtifactWriteResult result = kind is OutputArtifactKind.MarkdownReport && _reportHolder is not null
			? WriteAndRecordReport(artifactDate, writeBody)
			: Write(
				ordinal => OutputArtifactNaming.BuildFileName(kind, artifactDate, ordinal),
				temporaryPath => StreamTextBody(kind, temporaryPath, writeBody));

		return result;
	}

	/// <summary>
	/// Writes a prompt pack: the same mechanism as every other artifact (write boundary, temporary in the
	/// destination, exclusive rename, restrictive creation), so a failure leaves no partial pack and never
	/// touches an existing report or data artifact. The name is <c>stem.md</c>, then <c>stem-2.md</c> and
	/// so on, with no date. The pack carries the same handling header as the report, as its first line.
	///
	/// The stem comes from the environment's compiled descriptor and never from input; a stem that is not a
	/// bare file name is refused as a programming error. A pack write never touches the report holder.
	/// On Linux the file is created <c>0600</c>; on Windows it inherits the destination folder's access
	/// control list (declared).
	/// </summary>
	public ArtifactWriteResult WritePromptPackStreaming(string stem, Action<TextWriter> writeBody)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(stem);
		ArgumentNullException.ThrowIfNull(writeBody);

		if (stem.IndexOfAny(RefusedFileNameCharacters.ALL) >= 0)
		{
			throw new ArgumentException(_PACK_STEM_NOT_A_FILE_NAME_MESSAGE, nameof(stem));
		}

		return Write(
			ordinal => OutputArtifactNaming.BuildPromptPackFileName(stem, ordinal),
			temporaryPath => StreamTextBody(OutputArtifactKind.PromptPack, temporaryPath, writeBody),
			PACK_NAME_COLLISION_LIMIT_MESSAGE_FORMAT);
	}

	/// <summary>
	/// Replaces the report this process wrote with the same bytes plus one more section, atomically and
	/// failing closed: on any refusal or failure the original report is exactly as it was.
	///
	/// The steps: (1) an empty holder refuses; (2) the write boundary is evaluated; (3) the path is the
	/// destination plus the holder's file name, and a symbolic link, a junction (<c>LinkTarget != null</c>),
	/// a directory or a different length refuses - the raw reparse-point attribute is NOT tested, because a
	/// cloud-synchronized file carries it and is an ordinary file (a recorded adaptation); (4) the original
	/// is opened read-only and copied into a restrictively created temporary through a hashing stream, and
	/// the copy's length and SHA-256 must equal the holder's; (5) a line feed is added when the last byte is
	/// not one, then the section is written as UTF-8 without a byte order mark and with "\n" newlines;
	/// (6) both files are closed, the original's length and last-write time are re-checked and the
	/// temporary replaces it (the time between that check and the rename is a declared, unlocked window);
	/// (7) the holder takes the new length and hash. The temporary is deleted on any failure. The data
	/// artifact is never opened.
	/// </summary>
	public ReportAppendResult AppendToReport(ReportWriteHolder holder, Action<TextWriter> writeSection)
	{
		ArgumentNullException.ThrowIfNull(holder);
		ArgumentNullException.ThrowIfNull(writeSection);

		WrittenReport? report = holder.Current;
		ReportAppendRefusal refusal;

		if (report is null)
		{
			refusal = ReportAppendRefusal.NotWrittenByThisProcess;
		}
		else if (!WriteBoundary.Evaluate(_repository, _destination).PermitsWriting)
		{
			refusal = ReportAppendRefusal.OutsideBoundary;
		}
		else
		{
			refusal = ReplaceReportWithAppendedCopy(holder, report, writeSection);
		}

		return new ReportAppendResult
		{
			Appended = refusal is ReportAppendRefusal.None,
			Refusal = refusal,
			FileName = report is not null && refusal is ReportAppendRefusal.None ? report.FileName : string.Empty
		};
	}

	private static void ThrowIfPromptPack(OutputArtifactKind kind)
	{
		if (kind is OutputArtifactKind.PromptPack)
		{
			throw new ArgumentOutOfRangeException(nameof(kind), _PROMPT_PACK_OPERATION_MESSAGE);
		}
	}

	private ArtifactWriteResult WriteAndRecordReport(DateOnly artifactDate, Action<TextWriter> writeBody)
	{
		ReportContentDigest? digest = null;

		ArtifactWriteResult result = Write(
			ordinal => OutputArtifactNaming.BuildFileName(OutputArtifactKind.MarkdownReport, artifactDate, ordinal),
			temporaryPath => digest = StreamRecordedReportBody(temporaryPath, writeBody, _reportHolder, _hashFactory));

		if (result.Wrote && digest is not null)
		{
			_reportHolder?.Record(result.FileNameWritten, digest.Value);
		}

		return result;
	}

	/// <summary>
	/// The report body written through a hashing stream, so its length and SHA-256 are known without
	/// reading it back. The bytes are those of <see cref="StreamTextBody"/>. A hashing failure clears the
	/// holder and returns no digest, and never fails the write.
	/// </summary>
	private static ReportContentDigest? StreamRecordedReportBody(
		string temporaryPath,
		Action<TextWriter> writeBody,
		ReportWriteHolder? holder,
		Func<IncrementalHash>? hashFactory)
	{
		using FileStream file = RestrictiveFileCreation.CreateStream(temporaryPath, FileMode.CreateNew);
		using HashingWriteStream hashing = new(file, hashFactory);
		using StreamWriter writer = RestrictiveFileCreation.CreateTextWriterOver(hashing);

		writer.Write(ArtifactHandlingNotice.MarkdownHeader);
		writer.Write(_MARKDOWN_HEADER_SEPARATOR);
		writeBody(writer);
		writer.Flush();

		ReportContentDigest? digest = hashing.TryGetDigest();

		if (digest is null)
		{
			holder?.Clear();
		}

		return digest;
	}

	private ReportAppendRefusal ReplaceReportWithAppendedCopy(
		ReportWriteHolder holder,
		WrittenReport report,
		Action<TextWriter> writeSection)
	{
		string reportPath = Path.Combine(_destination.CanonicalPath, report.FileName);
		string temporaryPath = BuildTemporaryPath();
		ReportContentDigest? newDigest = null;
		ReportAppendRefusal refusal;

		try
		{
			refusal = InspectComposedReport(reportPath, report.Length, out DateTime originalLastWriteUtc);

			if (refusal is ReportAppendRefusal.None)
			{
				refusal = WriteAppendedTemporary(reportPath, temporaryPath, report, writeSection, out newDigest);
			}

			if (refusal is ReportAppendRefusal.None)
			{
				_appendStepHook?.Invoke(ReportAppendStep.Rename);
				refusal = SwapInTemporary(reportPath, temporaryPath, report.Length, originalLastWriteUtc);
			}
		}
		catch (IOException)
		{
			refusal = ReportAppendRefusal.NotWritable;
		}
		catch (UnauthorizedAccessException)
		{
			refusal = ReportAppendRefusal.NotWritable;
		}
		catch (NotSupportedException)
		{
			refusal = ReportAppendRefusal.NotWritable;
		}
		finally
		{
			DeleteIfPresent(temporaryPath);
		}

		if (refusal is ReportAppendRefusal.None && newDigest is not null)
		{
			holder.Record(report.FileName, newDigest.Value);
		}

		return refusal;
	}

	/// <summary>
	/// Fails closed when the composed path does not sit directly in the destination folder - whatever the holder
	/// held - and otherwise inspects the report.
	/// </summary>
	private ReportAppendRefusal InspectComposedReport(string reportPath, long expectedLength, out DateTime lastWriteUtc)
	{
		string? parent = Path.GetDirectoryName(Path.GetFullPath(reportPath));
		string destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_destination.CanonicalPath));
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		ReportAppendRefusal refusal;

		if (parent is not null && string.Equals(Path.TrimEndingDirectorySeparator(parent), destination, comparison))
		{
			refusal = InspectReport(reportPath, expectedLength, out lastWriteUtc);
		}
		else
		{
			lastWriteUtc = default;
			refusal = ReportAppendRefusal.OutsideBoundary;
		}

		return refusal;
	}

	private static ReportAppendRefusal InspectReport(string reportPath, long expectedLength, out DateTime lastWriteUtc)
	{
		FileInfo info = new(reportPath);
		ReportAppendRefusal refusal = ReportAppendRefusal.None;

		lastWriteUtc = default;

		if (info.LinkTarget is not null || Directory.Exists(reportPath))
		{
			refusal = ReportAppendRefusal.ReportReplacedOrLinked;
		}
		else if (!info.Exists || info.Length != expectedLength)
		{
			refusal = ReportAppendRefusal.ReportChanged;
		}
		else
		{
			lastWriteUtc = info.LastWriteTimeUtc;
		}

		return refusal;
	}

	private ReportAppendRefusal WriteAppendedTemporary(
		string reportPath,
		string temporaryPath,
		WrittenReport report,
		Action<TextWriter> writeSection,
		out ReportContentDigest? newDigest)
	{
		using FileStream original = new(reportPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		_appendStepHook?.Invoke(ReportAppendStep.TemporaryCreation);
		using FileStream temporaryFile = RestrictiveFileCreation.CreateStream(temporaryPath, FileMode.CreateNew);
		using HashingWriteStream hashing = new(temporaryFile, _hashFactory);

		bool endsWithLineFeed = CopyOriginal(original, hashing, _appendStepHook);
		ReportAppendRefusal refusal = VerifyCopy(hashing, report);

		newDigest = null;

		if (refusal is ReportAppendRefusal.None)
		{
			newDigest = WriteSection(hashing, endsWithLineFeed, writeSection);
			refusal = newDigest is null ? ReportAppendRefusal.NotWritable : ReportAppendRefusal.None;
		}

		return refusal;
	}

	private static bool CopyOriginal(Stream original, HashingWriteStream hashing, Action<ReportAppendStep>? stepHook)
	{
		byte[] buffer = new byte[_COPY_BUFFER_SIZE_BYTES];
		bool endsWithLineFeed = true;
		int bytesRead = original.Read(buffer, 0, buffer.Length);

		while (bytesRead > 0)
		{
			hashing.Write(buffer, 0, bytesRead);
			stepHook?.Invoke(ReportAppendStep.MidCopy);
			endsWithLineFeed = buffer[bytesRead - 1] == _LINE_FEED;
			bytesRead = original.Read(buffer, 0, buffer.Length);
		}

		return endsWithLineFeed;
	}

	private static ReportAppendRefusal VerifyCopy(HashingWriteStream hashing, WrittenReport report)
	{
		ReportContentDigest? copied = hashing.TryGetDigest();
		ReportAppendRefusal refusal = ReportAppendRefusal.None;

		if (copied is null)
		{
			refusal = ReportAppendRefusal.NotWritable;
		}
		else if (copied.Value.Length != report.Length
			|| !CryptographicOperations.FixedTimeEquals(copied.Value.Sha256, report.Sha256))
		{
			refusal = ReportAppendRefusal.ReportChanged;
		}

		return refusal;
	}

	private static ReportContentDigest? WriteSection(
		HashingWriteStream hashing,
		bool endsWithLineFeed,
		Action<TextWriter> writeSection)
	{
		using StreamWriter writer = RestrictiveFileCreation.CreateTextWriterOver(hashing);

		writer.NewLine = "\n";

		if (!endsWithLineFeed)
		{
			writer.Write(_LINE_FEED);
		}

		writeSection(writer);
		writer.Flush();

		return hashing.TryGetDigest();
	}

	private static ReportAppendRefusal SwapInTemporary(
		string reportPath,
		string temporaryPath,
		long originalLength,
		DateTime originalLastWriteUtc)
	{
		ReportAppendRefusal refusal = InspectReport(reportPath, originalLength, out DateTime currentLastWriteUtc);

		if (refusal is ReportAppendRefusal.None && currentLastWriteUtc != originalLastWriteUtc)
		{
			refusal = ReportAppendRefusal.ReportChanged;
		}

		if (refusal is ReportAppendRefusal.None)
		{
			File.Move(temporaryPath, reportPath, overwrite: true);
		}

		return refusal;
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

		if (kind is OutputArtifactKind.MarkdownReport or OutputArtifactKind.PromptPack)
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

	private ArtifactWriteResult Write(
		Func<int, string> buildFileName,
		Action<string> writeTemporaryFile,
		string collisionLimitMessageFormat = NAME_COLLISION_LIMIT_MESSAGE_FORMAT)
	{
		WriteBoundaryVerdict verdict = WriteBoundary.Evaluate(_repository, _destination);
		ArtifactWriteResult result;

		if (!verdict.PermitsWriting)
		{
			result = Refused(ArtifactWriteRefusal.DestinationOverlapsRepository, verdict.RefusalMessage);
		}
		else
		{
			result = WriteUnderCollisionRule(buildFileName, writeTemporaryFile, collisionLimitMessageFormat);
		}

		return result;
	}

	private ArtifactWriteResult WriteUnderCollisionRule(
		Func<int, string> buildFileName,
		Action<string> writeTemporaryFile,
		string collisionLimitMessageFormat)
	{
		string temporaryPath = BuildTemporaryPath();
		ArtifactWriteResult result;

		try
		{
			Directory.CreateDirectory(_destination.CanonicalPath);
			writeTemporaryFile(temporaryPath);

			result = RenameToFirstFreeName(buildFileName, temporaryPath, collisionLimitMessageFormat);
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
	private ArtifactWriteResult RenameToFirstFreeName(
		Func<int, string> buildFileName,
		string temporaryPath,
		string collisionLimitMessageFormat)
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
			? Refused(ArtifactWriteRefusal.NameCollisionLimitReached, DescribeCollisionLimit(collisionLimitMessageFormat))
			: new ArtifactWriteResult
			{
				Wrote = true,
				FileNameWritten = fileNameWritten,
				DestinationConsoleText = _destination.ConsoleDisplayText,
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
			_destination.ConsoleDisplayText);

	private string DescribeCollisionLimit(string messageFormat) =>
		string.Format(
			CultureInfo.InvariantCulture,
			messageFormat,
			_destination.ConsoleDisplayText);

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
