using System.Text;

using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// THE DECODE, AGAINST THE NUMBERS THAT DEFINE IT RATHER THAN AGAINST A PARAPHRASE OF THEM.
///
/// The claim the acceptance criterion makes is narrow and absolute: a two-byte-encoded file is
/// either DECODED CORRECTLY or reported UNDECODABLE, and never silently read as garbage that
/// matches nothing. Both halves are asserted here, and the first half is asserted on real
/// content - a file that was detected as two-byte and then decoded wrongly would pass a test
/// that only checked the detection.
/// </summary>
public sealed class FileDecodingTests
{
	private const string _CREDENTIAL_SHAPED_LINE = "$headers = @{ Authorization = 'Basic ' + $encoded }";
	private const string _ASCII_LINE = "using System;";
	private const string _NON_ASCII_LINE = "// año, ĉiu, 日本語";
	private const int _EXPECTED_PROBE_WINDOW_IN_BYTES = 4096;
	private const int _PROBE_SAMPLE_LENGTH_IN_BYTES = 512;
	private const int _EVERY_THIRD_BYTE = 3;
	private const byte _PRINTABLE_BYTE = 0x41;
	private const byte _HIGH_ASCII_BYTE = 0x7F;
	private const int _WINDOWS_1252_CODE_PAGE = 1252;
	private const string _ACCENTED_COMMENT_LINE = "// Autor: José Peña\n$headers = @{ Authorization = 'Basic ' + $encoded }";
	private const string _SMART_QUOTES_LINE = "// \u201Cquoted\u201D \u20AC5 \u2013 done";
	private const byte _NULL_BYTE = 0x00;
	private const byte _START_OF_HEADING_BYTE = 0x01;
	private const byte _ESCAPE_BYTE = 0x1B;
	private const byte _TAB_BYTE = 0x09;
	private const byte _FORM_FEED_BYTE = 0x0C;
	private const byte _DELETE_BYTE = 0x7F;
	private const byte _E_ACUTE_IN_WINDOWS_1252 = 0xE9;

	private static readonly Encoding _windows1252 = CodePagesEncodingProvider.Instance.GetEncoding(_WINDOWS_1252_CODE_PAGE)!;

	[Fact]
	public void PlainUtf8DecodesAsItself()
	{
		Assert.True(FileDecoding.TryDecode(Encoding.UTF8.GetBytes(_CREDENTIAL_SHAPED_LINE), out DecodedContent content));
		Assert.Equal(_CREDENTIAL_SHAPED_LINE, content.Text);
		Assert.Equal(DetectedEncoding.Utf8, content.Encoding);
	}

	[Fact]
	public void NonAsciiUtf8SurvivesIntact()
	{
		Assert.True(FileDecoding.TryDecode(Encoding.UTF8.GetBytes(_NON_ASCII_LINE), out DecodedContent content));
		Assert.Equal(_NON_ASCII_LINE, content.Text);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AMarkedTwoByteFileDecodesByItsMark(bool bigEndian)
	{
		byte[] bytes = new UnicodeEncoding(bigEndian, byteOrderMark: true).GetPreamble()
			.Concat(new UnicodeEncoding(bigEndian, byteOrderMark: false).GetBytes(_CREDENTIAL_SHAPED_LINE))
			.ToArray();

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(_CREDENTIAL_SHAPED_LINE, content.Text);
		Assert.Equal(
			bigEndian ? DetectedEncoding.Utf16BigEndian : DetectedEncoding.Utf16LittleEndian,
			content.Encoding);
	}

	/// <summary>
	/// THE CASE THE WHOLE RETRY EXISTS FOR, and the one strict UTF-8 validation silently accepts:
	/// a markless two-byte file holding ASCII is VALID UTF-8, so validation succeeds and returns
	/// text with a null between every character. Decoded that way it matches nothing, no counter
	/// fires, and recall goes to zero invisibly.
	/// </summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AMarklessTwoByteFileIsFoundByParityAndDecodedCorrectly(bool bigEndian)
	{
		byte[] bytes = new UnicodeEncoding(bigEndian, byteOrderMark: false).GetBytes(_CREDENTIAL_SHAPED_LINE);

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(_CREDENTIAL_SHAPED_LINE, content.Text);
		Assert.Equal(
			bigEndian ? DetectedEncoding.Utf16BigEndian : DetectedEncoding.Utf16LittleEndian,
			content.Encoding);
		Assert.DoesNotContain("\0", content.Text, StringComparison.Ordinal);
	}

	/// <summary>Strict UTF-8 accepts these bytes, and the decode must not.</summary>
	[Fact]
	public void StrictUtf8WouldHaveAcceptedTheMarklessTwoByteFileWeRefused()
	{
		byte[] bytes = new UnicodeEncoding(bigEndian: false, byteOrderMark: false).GetBytes(_ASCII_LINE);
		string asUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);

		Assert.Contains("\0", asUtf8, StringComparison.Ordinal);
		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(DetectedEncoding.Utf16LittleEndian, content.Encoding);
	}

	[Fact]
	public void AUtf8MarkIsRecognizedAndStripped()
	{
		byte[] bytes = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(_ASCII_LINE)];

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(_ASCII_LINE, content.Text);
		Assert.Equal(DetectedEncoding.Utf8WithByteOrderMark, content.Encoding);
	}

	/// <summary>
	/// The four-byte little-endian mark BEGINS WITH the two-byte little-endian mark, so a decoder
	/// testing the shorter one first reads every four-byte file as two-byte text starting with a
	/// null character - which would then be reported undecodable, silently losing the file.
	/// </summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AFourByteMarkIsNotMistakenForATwoByteOne(bool bigEndian)
	{
		UTF32Encoding encoding = new(bigEndian, byteOrderMark: true);
		byte[] bytes = [.. encoding.GetPreamble(), .. new UTF32Encoding(bigEndian, byteOrderMark: false).GetBytes(_ASCII_LINE)];

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(_ASCII_LINE, content.Text);
		Assert.Equal(
			bigEndian ? DetectedEncoding.Utf32BigEndian : DetectedEncoding.Utf32LittleEndian,
			content.Encoding);
	}

	/// <summary>An odd byte count disqualifies the file outright: no two-byte encoding produces one.</summary>
	[Fact]
	public void AnOddByteCountDisqualifiesTheTwoByteRetry()
	{
		byte[] bytes = [.. new UnicodeEncoding(false, false).GetBytes(_ASCII_LINE), _PRINTABLE_BYTE];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	/// <summary>
	/// DENSITY WITHOUT PARITY IS A BINARY FILE. Nulls are well past a quarter of the window here
	/// and they fall on both parities, which is exactly what a compiled artifact looks like and
	/// exactly what no two-byte text file looks like.
	/// </summary>
	[Fact]
	public void DenseNullsOfNoSingleParityAreRefused()
	{
		byte[] bytes = new byte[_PROBE_SAMPLE_LENGTH_IN_BYTES];

		for (int offset = 0; offset < bytes.Length; offset += _EVERY_THIRD_BYTE)
		{
			bytes[offset] = _HIGH_ASCII_BYTE;
		}

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	/// <summary>
	/// PARITY WITHOUT DENSITY IS A HANDFUL OF STRAY NULLS AGREEING BY CHANCE. Every null here is
	/// at an odd offset, which is the little-endian signature, but there are far too few of them
	/// for the file to be text in that encoding.
	/// </summary>
	[Fact]
	public void SparseNullsOfOneParityAreRefused()
	{
		byte[] bytes = new byte[_PROBE_SAMPLE_LENGTH_IN_BYTES];

		Array.Fill(bytes, _PRINTABLE_BYTE);
		bytes[1] = 0;
		bytes[3] = 0;

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	/// <summary>
	/// THE EDGE CASE THE REASONING ITSELF CREATES, and it is the intended outcome rather than a
	/// defect: a file that is valid UTF-8 and genuinely contains a null falls through the null
	/// test, fails the retry's density threshold, and is REPORTED rather than read. Source text
	/// does not contain a null, so a file that does is either not source or not the encoding it
	/// claims, and both are better counted than read wrongly.
	/// </summary>
	[Fact]
	public void ValidUtf8HoldingOneNullIsReportedRatherThanRead()
	{
		byte[] ascii = Encoding.UTF8.GetBytes(_ASCII_LINE);
		byte[] bytes = [.. ascii, 0x00, .. ascii, _PRINTABLE_BYTE];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	/// <summary>
	/// A null PAST the probe window is caught by the test on the decoded text rather than by the
	/// probe, which is why the two exist together: the probe is a shortcut over the first four
	/// kibibytes and the text test covers the rest of the file.
	/// </summary>
	[Fact]
	public void ANullPastTheProbeWindowIsStillCaught()
	{
		byte[] filler = Encoding.UTF8.GetBytes(new string('a', FileDecoding.PROBE_WINDOW_IN_BYTES * 2));

		byte[] bytes = [.. filler, 0x00, _PRINTABLE_BYTE, .. filler];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	/// <summary>
	/// The last resort's case: bytes that are not valid UTF-8, hold
	/// no null, no control byte and none of the five undefined bytes. Windows-1252 decodes every byte
	/// it defines, so this is read - and reported as Windows-1252, never as UTF-8.
	/// </summary>
	[Fact]
	public void InvalidUtf8ThatLooksLikeSingleByteTextIsReadAsWindows1252()
	{
		byte[] bytes = [0xC3, 0x28, 0xA0, 0xA1, 0xE2, 0x28, 0xA1, 0xF0];

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(DetectedEncoding.Windows1252, content.Encoding);
		Assert.Equal(_windows1252.GetString(bytes), content.Text);
	}

	[Fact]
	public void AWindows1252FileWithAccentedCommentsIsDecodedAndReportedAsWindows1252()
	{
		byte[] bytes = _windows1252.GetBytes(_ACCENTED_COMMENT_LINE);

		Assert.Throws<DecoderFallbackException>(() => new UTF8Encoding(false, true).GetString(bytes));
		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(_ACCENTED_COMMENT_LINE, content.Text);
		Assert.Equal(DetectedEncoding.Windows1252, content.Encoding);
	}

	/// <summary>The 0x80 to 0x9F range is where Windows-1252 differs from Latin-1: typographic characters, not controls.</summary>
	[Fact]
	public void TheTypographicRangeOfWindows1252DecodesToItsCharactersAndNotToControls()
	{
		byte[] bytes = _windows1252.GetBytes(_SMART_QUOTES_LINE);

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(_SMART_QUOTES_LINE, content.Text);
		Assert.Equal(DetectedEncoding.Windows1252, content.Encoding);
	}

	/// <summary>
	/// THE DECISION ON THE FIVE UNDEFINED BYTES, pinned: a file holding one is not Windows-1252, so it
	/// is undecodable and counted, rather than read as mojibake with a control character in its place.
	/// </summary>
	[Theory]
	[InlineData((byte)0x81)]
	[InlineData((byte)0x8D)]
	[InlineData((byte)0x8F)]
	[InlineData((byte)0x90)]
	[InlineData((byte)0x9D)]
	public void AByteWindows1252LeavesUndefinedMakesTheFileUndecodable(byte undefinedByte)
	{
		byte[] bytes = [.. Encoding.ASCII.GetBytes("// Autor: Jos"), _E_ACUTE_IN_WINDOWS_1252, undefinedByte, .. Encoding.ASCII.GetBytes("\n")];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	/// <summary>A single-byte code page decodes any byte, so the NUL rule has to run in front of it, over the whole file.</summary>
	[Fact]
	public void ANullByteAnywhereKeepsASingleByteFileFromBeingRead()
	{
		byte[] filler = _windows1252.GetBytes(new string('a', FileDecoding.PROBE_WINDOW_IN_BYTES * 2));
		byte[] bytes = [_E_ACUTE_IN_WINDOWS_1252, .. filler, _NULL_BYTE, .. filler];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	[Theory]
	[InlineData(_START_OF_HEADING_BYTE)]
	[InlineData(_ESCAPE_BYTE)]
	[InlineData(_DELETE_BYTE)]
	public void ANonTextControlByteKeepsASingleByteFileFromBeingRead(byte controlByte)
	{
		byte[] bytes = [.. _windows1252.GetBytes("// Peña\n"), controlByte, .. _windows1252.GetBytes("x")];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	[Fact]
	public void TabsAndFormFeedsDoNotKeepASingleByteFileFromBeingRead()
	{
		byte[] bytes = [.. _windows1252.GetBytes("// Peña"), _TAB_BYTE, _FORM_FEED_BYTE, .. _windows1252.GetBytes("\r\n")];

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(DetectedEncoding.Windows1252, content.Encoding);
	}

	[Fact]
	public void ABinaryFileWithHighBytesIsStillUndecodable()
	{
		byte[] bytes = [0x7F, 0x45, 0x4C, 0x46, 0x02, 0x01, 0x01, 0x00, 0xE9, 0xFF, 0x00, 0x00, 0xC3, 0x28];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	/// <summary>The earlier rungs keep what they had: valid UTF-8 with accents is UTF-8, never Windows-1252.</summary>
	[Fact]
	public void ValidNonAsciiUtf8IsStillUtf8AndNotWindows1252()
	{
		Assert.True(FileDecoding.TryDecode(Encoding.UTF8.GetBytes(_ACCENTED_COMMENT_LINE), out DecodedContent content));
		Assert.Equal(_ACCENTED_COMMENT_LINE, content.Text);
		Assert.Equal(DetectedEncoding.Utf8, content.Encoding);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AMarklessTwoByteFileWithAccentsIsStillTwoByteAndNotWindows1252(bool bigEndian)
	{
		byte[] bytes = new UnicodeEncoding(bigEndian, byteOrderMark: false).GetBytes(_ACCENTED_COMMENT_LINE);

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(_ACCENTED_COMMENT_LINE, content.Text);
		Assert.Equal(
			bigEndian ? DetectedEncoding.Utf16BigEndian : DetectedEncoding.Utf16LittleEndian,
			content.Encoding);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AMarkedFourByteFileWithAccentsIsStillFourByte(bool bigEndian)
	{
		UTF32Encoding encoding = new(bigEndian, byteOrderMark: true);
		byte[] bytes = [.. encoding.GetPreamble(), .. encoding.GetBytes(_ACCENTED_COMMENT_LINE)];

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));
		Assert.Equal(_ACCENTED_COMMENT_LINE, content.Text);
		Assert.Equal(
			bigEndian ? DetectedEncoding.Utf32BigEndian : DetectedEncoding.Utf32LittleEndian,
			content.Encoding);
	}

	/// <summary>A mark settles the question: a marked file whose body is single-byte text is not retried as Windows-1252.</summary>
	[Fact]
	public void AMarkedFileIsNeverFallenThroughToWindows1252()
	{
		byte[] bytes = [.. Encoding.UTF8.GetPreamble(), _E_ACUTE_IN_WINDOWS_1252, 0x41];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	[Fact]
	public void TheDecodeReportsPositionsOverTheDecodedTextAfterAccentedCharacters()
	{
		const string line = "// José Peña, then the construct: Basic";
		byte[] bytes = _windows1252.GetBytes(line);

		Assert.True(FileDecoding.TryDecode(bytes, out DecodedContent content));

		int offset = content.Text.IndexOf("Basic", StringComparison.Ordinal);
		TextPosition position = TextPositions.LineAndColumnOf(content.Text, offset);

		Assert.Equal(DetectedEncoding.Windows1252, content.Encoding);
		Assert.Equal(TextPositions.FIRST_LINE, position.Line);
		Assert.Equal(line.IndexOf("Basic", StringComparison.Ordinal) + TextPositions.FIRST_COLUMN, position.Column);
	}

	[Fact]
	public void AnEmptyFileDecodesToEmptyTextRatherThanFailing()
	{
		Assert.True(FileDecoding.TryDecode([], out DecodedContent content));
		Assert.Equal(string.Empty, content.Text);
		Assert.Equal(DetectedEncoding.Utf8, content.Encoding);
	}

	/// <summary>
	/// A mark settles the question and nothing falls through it: these bytes declare themselves
	/// two-byte little-endian and then hold an unpaired trailing byte, which is undecodable
	/// rather than an invitation to guess again.
	/// </summary>
	[Fact]
	public void AMarkedFileThatDoesNotValidateUnderItsOwnMarkIsUndecodable()
	{
		byte[] bytes = [0xFF, 0xFE, 0x41, 0x00, 0x42];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
	}

	[Fact]
	public void TheProbeWindowIsFourKibibytes()
	{
		Assert.Equal(_EXPECTED_PROBE_WINDOW_IN_BYTES, FileDecoding.PROBE_WINDOW_IN_BYTES);
	}
}
