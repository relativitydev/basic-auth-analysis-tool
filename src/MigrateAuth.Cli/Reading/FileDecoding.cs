using System.Text;

namespace MigrateAuth.Cli.Reading;

/// <summary>Which form a file turned out to be in. Reported so that a decode can be evidenced.</summary>
public enum DetectedEncoding
{
	/// <summary>Nothing was decoded.</summary>
	None = 0,

	/// <summary>No mark, and it validated strictly as UTF-8. The overwhelming majority.</summary>
	Utf8 = 1,

	/// <summary>A UTF-8 byte order mark.</summary>
	Utf8WithByteOrderMark = 2,

	/// <summary>Two bytes per unit, low byte first. With a mark, or found by the retry.</summary>
	Utf16LittleEndian = 3,

	/// <summary>Two bytes per unit, high byte first. With a mark, or found by the retry.</summary>
	Utf16BigEndian = 4,

	/// <summary>Four bytes per unit, low byte first. Only ever with a mark.</summary>
	Utf32LittleEndian = 5,

	/// <summary>Four bytes per unit, high byte first. Only ever with a mark.</summary>
	Utf32BigEndian = 6,

	/// <summary>
	/// No mark, not valid UTF-8, not two-byte: read as Windows-1252, the last resort. Reported
	/// per file so that a decode which was a guess is never presented as a certainty.
	/// </summary>
	Windows1252 = 7
}

/// <summary>What a decode produced, or why there is nothing.</summary>
public readonly record struct DecodedContent
{
	internal DecodedContent(string text, DetectedEncoding encoding)
	{
		Text = text;
		Encoding = encoding;
	}

	/// <summary>The decoded text, before line-ending normalization.</summary>
	public string Text { get; }

	/// <summary>What it was decoded as.</summary>
	public DetectedEncoding Encoding { get; }
}

/// <summary>
/// THE DECODE, IN THE ORDER IT HAS TO HAPPEN.
///
/// Some platforms write their scripts in a two-byte encoding by default, so those files are
/// common rather than exceptional. Decoded wrongly they become garbage, the zone mask corrupts,
/// and findings vanish with no warning at all.
///
/// STRICT UTF-8 VALIDATION IS NOT A SUFFICIENT DISCRIMINATOR. A two-byte encoding holding ASCII
/// is VALID UTF-8, because NUL is a legal code point: strict validation SUCCEEDS and returns
/// text with a NUL between every character. So a decode that PRODUCES a NUL has discriminated
/// wrongly - source text does not contain one - and falls through to the next attempt. A file
/// that is genuinely UTF-8 and genuinely contains a null byte therefore falls through, fails the
/// retry, and is reported undecodable rather than read.
///
/// THE LAST RESORT IS WINDOWS-1252, AND IT IS A GUESS THAT IS REPORTED AS ONE. A file that is not
/// marked, not valid UTF-8 and not markless two-byte is treated as a legacy single-byte file in
/// code page 1252.
///
/// A SINGLE-BYTE CODE PAGE DECODES ANY BYTE SEQUENCE, so the step cannot discriminate by failing
/// and the discrimination has to be put in front of it, or the Undecodable category would
/// become unreachable and every binary file would be read as garbage. It is attempted only for
/// bytes that look like text in a single-byte code page: no null byte anywhere in the file,
/// no control character other than tab, line feed, form feed and carriage return, and none of
/// the five bytes Windows-1252 leaves undefined. A file that holds one of those is not
/// Windows-1252, and decoding it as 1252 would read it as mojibake that matches nothing, so it
/// is reported undecodable instead.
///
/// As a consequence, a UTF-8 file with one malformed sequence is read as Windows-1252, with its
/// non-ASCII characters wrong, and is reported as a Windows-1252 file; and a binary file
/// containing no null byte and no control character is read as text.
/// </summary>
public static class FileDecoding
{
	/// <summary>
	/// The first 4 KiB, or the whole file when it is smaller. Everything the retry decides is
	/// decided inside this window: it is enough to characterize a text file's shape, and bounded
	/// so that the decision costs the same on a 2 MB file as on a small one.
	/// </summary>
	public const int PROBE_WINDOW_IN_BYTES = 4096;

	/// <summary>
	/// Nulls must be at least a quarter of the window. ASCII in a two-byte encoding is close to
	/// half, and a quarter leaves room for text that is not ASCII.
	/// </summary>
	private const int _NULL_DENSITY_DIVISOR = 4;

	/// <summary>
	/// Fifteen sixteenths of the nulls must share one parity. Both thresholds have to hold
	/// together: DENSITY WITHOUT PARITY IS A BINARY FILE, and PARITY WITHOUT DENSITY is a handful
	/// of stray nulls agreeing by chance.
	/// </summary>
	private const int _PARITY_MAJORITY_NUMERATOR = 15;

	private const int _PARITY_MAJORITY_DENOMINATOR = 16;

	private const int _TWO_BYTE_UNIT_LENGTH = 2;

	private const char _NULL_CHARACTER = '\0';

	private const int _WINDOWS_1252_CODE_PAGE = 1252;

	private const byte _FIRST_PRINTABLE_BYTE = 0x20;
	private const byte _TAB_BYTE = 0x09;
	private const byte _LINE_FEED_BYTE = 0x0A;
	private const byte _FORM_FEED_BYTE = 0x0C;
	private const byte _CARRIAGE_RETURN_BYTE = 0x0D;
	private const byte _DELETE_BYTE = 0x7F;

	/// <summary>The five bytes Windows-1252 leaves undefined.</summary>
	private static readonly byte[] _windows1252UndefinedBytes = [0x81, 0x8D, 0x8F, 0x90, 0x9D];

	private static readonly byte[] _utf8Mark = [0xEF, 0xBB, 0xBF];
	private static readonly byte[] _utf32LittleEndianMark = [0xFF, 0xFE, 0x00, 0x00];
	private static readonly byte[] _utf32BigEndianMark = [0x00, 0x00, 0xFE, 0xFF];
	private static readonly byte[] _utf16LittleEndianMark = [0xFF, 0xFE];
	private static readonly byte[] _utf16BigEndianMark = [0xFE, 0xFF];

	private static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	private static readonly UnicodeEncoding _strictUtf16LittleEndian =
		new(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

	private static readonly UnicodeEncoding _strictUtf16BigEndian =
		new(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);

	private static readonly UTF32Encoding _strictUtf32LittleEndian =
		new(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true);

	private static readonly UTF32Encoding _strictUtf32BigEndian =
		new(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true);

	/// <summary>
	/// Windows-1252 from the code page provider that ships in the shared framework - no package.
	/// Invariant globalization does not affect it: the provider carries its own tables,
	/// and Native AOT keeps them because the type is referenced here directly. The fallbacks throw,
	/// so that a byte the table cannot map is undecodable and never a replacement character.
	/// </summary>
	private static readonly Encoding _strictWindows1252 = CreateStrictWindows1252();

	/// <summary>
	/// The ladder. A byte order mark settles the question outright; without one, strict UTF-8
	/// answers it unless it produces a null; a null or an invalid sequence sends the file to the
	/// two-byte retry, which either identifies the byte order or gives up; and a file still
	/// unplaced gets the Windows-1252 last resort if, and only if, it looks like single-byte text.
	/// </summary>
	public static bool TryDecode(byte[] bytes, out DecodedContent content)
	{
		ArgumentNullException.ThrowIfNull(bytes);

		bool decoded;

		if (TryIdentifyMark(bytes, out MarkedForm mark))
		{
			decoded = TryDecodeWith(bytes, mark.MarkLength, mark.Encoding, mark.Detected, out content);
		}
		else
		{
			decoded = TryDecodeAsUtf8(bytes, out content)
				|| TryDecodeAsTwoByte(bytes, out content)
				|| TryDecodeAsWindows1252(bytes, out content);
		}

		return decoded;
	}

	/// <summary>
	/// A mark settles both the form and the byte order, so nothing is inferred and NOTHING FALLS
	/// THROUGH: a marked file whose body does not validate under the form its own mark declares
	/// is undecodable, not a candidate for the retry. Guessing past a declaration is how a file
	/// gets read as something it said it was not.
	///
	/// The four-byte marks are tested first because the little-endian one BEGINS WITH the
	/// two-byte little-endian mark, and testing the shorter one first would decode every UTF-32
	/// file as UTF-16 starting with a null character.
	/// </summary>
	private static bool TryIdentifyMark(byte[] bytes, out MarkedForm mark)
	{
		bool marked = true;

		if (StartsWith(bytes, _utf32LittleEndianMark))
		{
			mark = new MarkedForm(_utf32LittleEndianMark.Length, _strictUtf32LittleEndian, DetectedEncoding.Utf32LittleEndian);
		}
		else if (StartsWith(bytes, _utf32BigEndianMark))
		{
			mark = new MarkedForm(_utf32BigEndianMark.Length, _strictUtf32BigEndian, DetectedEncoding.Utf32BigEndian);
		}
		else if (StartsWith(bytes, _utf8Mark))
		{
			mark = new MarkedForm(_utf8Mark.Length, _strictUtf8, DetectedEncoding.Utf8WithByteOrderMark);
		}
		else if (StartsWith(bytes, _utf16LittleEndianMark))
		{
			mark = new MarkedForm(_utf16LittleEndianMark.Length, _strictUtf16LittleEndian, DetectedEncoding.Utf16LittleEndian);
		}
		else if (StartsWith(bytes, _utf16BigEndianMark))
		{
			mark = new MarkedForm(_utf16BigEndianMark.Length, _strictUtf16BigEndian, DetectedEncoding.Utf16BigEndian);
		}
		else
		{
			mark = default;
			marked = false;
		}

		return marked;
	}

	/// <summary>
	/// Strict UTF-8, refused on a null in the result. The null test is the binary probe and the
	/// two-byte discriminator at once: under UTF-8 any null is a byte the encoding permits and
	/// source text does not, so it is evidence either of a binary file or of the markless
	/// two-byte form, and both of those go to the retry rather than being read as text.
	/// </summary>
	private static bool TryDecodeAsUtf8(byte[] bytes, out DecodedContent content)
	{
		bool decoded = false;
		content = default;

		if (!WindowContainsNull(bytes))
		{
			try
			{
				string text = _strictUtf8.GetString(bytes);

				decoded = !text.Contains(_NULL_CHARACTER, StringComparison.Ordinal);
				content = decoded ? new DecodedContent(text, DetectedEncoding.Utf8) : default;
			}
			catch (DecoderFallbackException)
			{
				decoded = false;
			}
		}

		return decoded;
	}

	/// <summary>
	/// THE MARKLESS TWO-BYTE RETRY, with a fixed window, fixed thresholds and a fixed rule for telling
	/// the two byte orders apart.
	///
	/// An odd byte count disqualifies the file outright: a two-byte encoding cannot produce one,
	/// and checking it first costs nothing. Byte order comes from the PARITY of the null
	/// positions, which is unambiguous for predominantly ASCII text - the little-endian form puts
	/// its nulls at ODD offsets and the big-endian form at EVEN ones.
	/// </summary>
	private static bool TryDecodeAsTwoByte(byte[] bytes, out DecodedContent content)
	{
		bool decoded = false;
		content = default;

		if (LooksLikeTwoByteText(bytes, out bool bigEndian))
		{
			decoded = TryDecodeWith(
				bytes,
				0,
				bigEndian ? _strictUtf16BigEndian : _strictUtf16LittleEndian,
				bigEndian ? DetectedEncoding.Utf16BigEndian : DetectedEncoding.Utf16LittleEndian,
				out content);
		}

		return decoded;
	}

	/// <summary>
	/// THE LAST RESORT. Only bytes that look like single-byte text reach the decoder, because the
	/// decoder itself accepts everything; see the class comment for each refusal and its reason.
	/// </summary>
	private static bool TryDecodeAsWindows1252(byte[] bytes, out DecodedContent content)
	{
		bool decoded = false;
		content = default;

		if (LooksLikeSingleByteText(bytes))
		{
			decoded = TryDecodeWith(bytes, 0, _strictWindows1252, DetectedEncoding.Windows1252, out content);
		}

		return decoded;
	}

	/// <summary>
	/// Over the WHOLE file rather than the probe window: a null or a control byte anywhere is
	/// evidence that this is not text, and the scan is a single pass over bytes already in memory.
	/// </summary>
	private static bool LooksLikeSingleByteText(byte[] bytes)
	{
		bool looksLikeText = true;

		foreach (byte value in bytes)
		{
			if (IsRefusedBySingleByteStep(value))
			{
				looksLikeText = false;
				break;
			}
		}

		return looksLikeText;
	}

	private static bool IsRefusedBySingleByteStep(byte value)
	{
		bool isAllowedControl = value is _TAB_BYTE or _LINE_FEED_BYTE or _FORM_FEED_BYTE or _CARRIAGE_RETURN_BYTE;
		bool isControl = value < _FIRST_PRINTABLE_BYTE || value == _DELETE_BYTE;

		return (isControl && !isAllowedControl) || _windows1252UndefinedBytes.Contains(value);
	}

	private static Encoding CreateStrictWindows1252()
	{
		return CodePagesEncodingProvider.Instance.GetEncoding(
			_WINDOWS_1252_CODE_PAGE,
			EncoderFallback.ExceptionFallback,
			DecoderFallback.ExceptionFallback)
			?? throw new InvalidOperationException("The Windows-1252 code page is not available from the framework's code page provider.");
	}

	private static bool LooksLikeTwoByteText(byte[] bytes, out bool bigEndian)
	{
		int windowLength = Math.Min(bytes.Length, PROBE_WINDOW_IN_BYTES);
		int nullsAtEvenOffsets = 0;
		int nullsAtOddOffsets = 0;

		for (int offset = 0; offset < windowLength; offset++)
		{
			if (bytes[offset] == 0)
			{
				CountNull(offset, ref nullsAtEvenOffsets, ref nullsAtOddOffsets);
			}
		}

		int totalNulls = nullsAtEvenOffsets + nullsAtOddOffsets;
		int dominantParity = Math.Max(nullsAtEvenOffsets, nullsAtOddOffsets);
		bool denseEnough = totalNulls > 0 && totalNulls * _NULL_DENSITY_DIVISOR >= windowLength;
		bool parityAgrees =
			dominantParity * _PARITY_MAJORITY_DENOMINATOR >= totalNulls * _PARITY_MAJORITY_NUMERATOR;

		bigEndian = nullsAtEvenOffsets >= nullsAtOddOffsets;

		return bytes.Length % _TWO_BYTE_UNIT_LENGTH == 0 && denseEnough && parityAgrees;
	}

	private static void CountNull(int offset, ref int nullsAtEvenOffsets, ref int nullsAtOddOffsets)
	{
		if (offset % _TWO_BYTE_UNIT_LENGTH == 0)
		{
			nullsAtEvenOffsets++;
		}
		else
		{
			nullsAtOddOffsets++;
		}
	}

	/// <summary>
	/// The binary probe, which is a shortcut rather than a separate rule: a null inside the
	/// window means strict UTF-8 cannot be the right answer, and finding that out here avoids
	/// decoding two megabytes to discover it. A null PAST the window is caught by the test on the
	/// decoded text, so the two together cover the file.
	/// </summary>
	private static bool WindowContainsNull(byte[] bytes)
	{
		int windowLength = Math.Min(bytes.Length, PROBE_WINDOW_IN_BYTES);

		return bytes.AsSpan(0, windowLength).IndexOf((byte)0) >= 0;
	}

	private static bool TryDecodeWith(
		byte[] bytes,
		int markLength,
		Encoding encoding,
		DetectedEncoding detected,
		out DecodedContent content)
	{
		bool decoded;

		try
		{
			string text = encoding.GetString(bytes, markLength, bytes.Length - markLength);

			content = new DecodedContent(text, detected);
			decoded = true;
		}
		catch (DecoderFallbackException)
		{
			content = default;
			decoded = false;
		}
		catch (ArgumentException)
		{
			content = default;
			decoded = false;
		}

		return decoded;
	}

	private static bool StartsWith(byte[] bytes, byte[] mark)
	{
		return bytes.Length >= mark.Length && bytes.AsSpan(0, mark.Length).SequenceEqual(mark);
	}

	/// <summary>What a byte order mark declared the file to be.</summary>
	private readonly record struct MarkedForm
	{
		internal MarkedForm(int markLength, Encoding encoding, DetectedEncoding detected)
		{
			MarkLength = markLength;
			Encoding = encoding;
			Detected = detected;
		}

		internal int MarkLength { get; }

		internal Encoding Encoding { get; }

		internal DetectedEncoding Detected { get; }
	}
}
