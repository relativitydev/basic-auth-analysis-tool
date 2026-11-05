using MigrateAuth.Cli.Emission;

namespace MigrateAuth.Cli.Reading;

/// <summary>
/// What reading one file produced: its text, or the category under which it was skipped. Exactly
/// one of the two, and <see cref="WasRead"/> says which.
/// </summary>
public sealed record FileReadResult
{
	/// <summary>Why it was skipped, or absent when it was read.</summary>
	public required SkippedItemCategory? SkippedAs { get; init; }

	/// <summary>The decoded text with line endings normalized. Empty when the file was skipped.</summary>
	public string NormalizedText { get; init; } = string.Empty;

	/// <summary>What it was decoded as, which is what makes a decode evidenced rather than assumed.</summary>
	public DetectedEncoding Encoding { get; init; }

	/// <summary>Its line count and longest line. Zero when the file was skipped.</summary>
	public TextMeasurement Measurement { get; init; }

	/// <summary>Whether there is text to look at.</summary>
	public bool WasRead => SkippedAs is null;
}

/// <summary>
/// ONE FILE, FROM AN UNOPENED PATH TO NORMALIZED TEXT OR A CATEGORIZED HOLE.
///
/// Five things can stop a file being read and each one is a different category, so the order
/// they are tested in is part of the design rather than an implementation detail:
///
/// 1. THE OPEN, first, because it is how a denial is discovered - by attempting, never by
///    inspecting. Nothing is checked about the file before this line.
/// 2. THE SIZE CEILING, immediately after the open and before a single byte is read. Checking it
///    on the OPEN STREAM rather than on a separate probe of the filesystem is deliberate twice
///    over: it costs no extra call in a walk of the largest supported size, and it closes the race a check-then-open
///    would leave - the length that is tested is the length of the file that is held.
/// 3. THE DECODE, which is where a two-byte-encoded file is either read correctly or declared
///    undecodable, and never silently read as garbage that matches nothing.
/// 4. NORMALIZATION, before any content-derived number exists, because the same commit has
///    different bytes on different platforms and every such number would otherwise differ.
/// 5. THE LINE-LENGTH CEILING, during the counting pass, because a line's length is not knowable
///    before the text is decoded and normalized - which is exactly why it cannot be enforced
///    alongside the size ceiling.
///
/// WHAT IT DOES NOT DO: decide whether the file was worth reading. Preselection anchors, the
/// per-language inventory and the candidate set belong to inventory selection and are a later stage's; this one
/// hands back text and lets that stage decide.
/// </summary>
public sealed class RepositoryFileReader
{
	private readonly EmissionPoint _emissionPoint;

	public RepositoryFileReader(EmissionPoint emissionPoint)
	{
		ArgumentNullException.ThrowIfNull(emissionPoint);

		_emissionPoint = emissionPoint;
	}

	/// <summary>
	/// Reads one walked file, recording a skipped item when it cannot be read. The skip goes
	/// through the emission point like everything else that leaves this process, so there is no
	/// second route by which a hole could reach a report - or fail to.
	/// </summary>
	public FileReadResult Read(WalkedFile file)
	{
		FileReadResult result = Interpret(file.AbsolutePath);

		if (result.SkippedAs is SkippedItemCategory category)
		{
			_emissionPoint.Emit(new EmittedFact
			{
				Category = EmissionCategory.SkippedItem,
				SkippedItem = category,
				Path = file.Path
			});
		}

		return result;
	}

	private static FileReadResult Interpret(string absolutePath)
	{
		FileOpenResult opened = ReadOnlyFileOpen.Attempt(absolutePath, out FileStream? stream);
		FileReadResult result;

		using (stream)
		{
			result = opened is FileOpenResult.Opened && stream is not null
				? ReadOpened(stream)
				: SkippedAs(CategorizeOpenFailure(opened));
		}

		return result;
	}

	/// <summary>
	/// THE MAPPING FROM AN OPEN FAILURE TO ONE OF THE ELEVEN, and the one line in it worth arguing
	/// about is the missing file.
	///
	/// A file that vanished between the walk enumerating it and this method opening it is
	/// recorded as UNREADABLE, which is literally what happened: it could not be opened for
	/// reading. It is NOT recorded as MissingCandidate. That category exists for a path recorded
	/// in Scan that Analysis later opens, a situation the persisted candidate set creates and
	/// inventory selection owns, and raising it here would mean this reader inventing an occasion for a category
	/// it does not own. The open attempt reports the two cases apart, so the later stage maps its
	/// own without changing anything here.
	/// </summary>
	private static SkippedItemCategory CategorizeOpenFailure(FileOpenResult opened)
	{
		return opened is FileOpenResult.PathTooLong
			? SkippedItemCategory.PathTooLong
			: SkippedItemCategory.UnreadableFile;
	}

	/// <summary>
	/// The ceiling, then the bytes. A read that fails part way - a disconnected share, a file
	/// truncated underneath us by the writer the share mode deliberately permits - is an
	/// unreadable file and not an exception that ends the walk.
	/// </summary>
	private static FileReadResult ReadOpened(FileStream stream)
	{
		FileReadResult result;

		try
		{
			if (stream.Length > ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES)
			{
				result = SkippedAs(SkippedItemCategory.OversizeFile);
			}
			else
			{
				byte[] bytes = new byte[(int)stream.Length];

				stream.ReadExactly(bytes);
				result = DecodeAndMeasure(bytes);
			}
		}
		catch (IOException)
		{
			result = SkippedAs(SkippedItemCategory.UnreadableFile);
		}
		catch (UnauthorizedAccessException)
		{
			result = SkippedAs(SkippedItemCategory.UnreadableFile);
		}
		catch (NotSupportedException)
		{
			result = SkippedAs(SkippedItemCategory.UnreadableFile);
		}

		return result;
	}

	private static FileReadResult DecodeAndMeasure(byte[] bytes)
	{
		FileReadResult result;

		if (!FileDecoding.TryDecode(bytes, out DecodedContent content))
		{
			result = SkippedAs(SkippedItemCategory.Undecodable);
		}
		else
		{
			string normalizedText = TextNormalization.Normalize(content.Text);

			result = TextNormalization.TryMeasure(normalizedText, out TextMeasurement measurement)
				? new FileReadResult
				{
					SkippedAs = null,
					NormalizedText = normalizedText,
					Encoding = content.Encoding,
					Measurement = measurement
				}
				: SkippedAs(SkippedItemCategory.OverLongLine);
		}

		return result;
	}

	private static FileReadResult SkippedAs(SkippedItemCategory category)
	{
		return new FileReadResult { SkippedAs = category };
	}
}
