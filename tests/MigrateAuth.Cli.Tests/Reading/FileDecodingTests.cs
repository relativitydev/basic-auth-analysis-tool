using System.Text;

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

	[Fact]
	public void InvalidUtf8ThatIsNotTwoByteTextIsUndecodable()
	{
		byte[] bytes = [0xC3, 0x28, 0xA0, 0xA1, 0xE2, 0x28, 0xA1, 0xF0];

		Assert.False(FileDecoding.TryDecode(bytes, out DecodedContent _));
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
