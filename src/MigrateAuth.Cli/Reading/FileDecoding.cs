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
	Utf32BigEndian = 6
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
/// The administration shell of one of the three platforms writes its scripts in a two-byte
/// encoding BY DEFAULT, so for that language those files are the norm rather than the exception.
/// Decoded wrongly they become garbage, the zone mask corrupts, and findings vanish with no
/// warning at all.
///
/// STRICT UTF-8 VALIDATION IS NOT A SUFFICIENT DISCRIMINATOR. A two-byte encoding holding ASCII
/// is VALID UTF-8, because NUL is a legal code point: strict validation SUCCEEDS and returns text
/// with a NUL between every character.
///
/// So a decode that PRODUCES a NUL has discriminated wrongly - source text does not contain one
/// - and falls through to the next attempt. That single rule is what makes the ladder below
/// work, and it also creates an edge case: a file that is genuinely UTF-8 and genuinely contains
/// a null byte falls through, fails the retry, and is reported undecodable rather than read. A
/// file with a null in it is either not source or not the encoding it claims, and both are
/// better counted than read wrongly.
///
/// LEGACY CODE PAGES ARE OUT OF SCOPE; undecodable files are counted, so their volume is
/// visible.
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
	/// The ladder. A byte order mark settles the question outright; without one, strict UTF-8
	/// answers it unless it produces a null; and a null sends the file to the two-byte retry,
	/// which either identifies the byte order or gives up.
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
			decoded = TryDecodeAsUtf8(bytes, out content) || TryDecodeAsTwoByte(bytes, out content);
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
