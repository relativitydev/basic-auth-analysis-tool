using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Scanning;

using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// WHAT THE SAFE ALGORITHM COSTS, MEASURED AGAINST THE UNSAFE ONE.
///
/// The comparison is between two ways of looking for the same anchor: over the decoded string, or
/// over the byte buffer transliterated one byte per character. The DECODE ITSELF IS NOT PART OF THE
/// DELTA, because the inventory pass has to decode anyway - the line count the inventory requires is
/// derived from normalized text. What differs is only the search.
///
/// So this measures the search, both ways, over the same content, and reports alongside it how many
/// candidates each way actually finds. A saving that comes with silent zero recall in an encoding
/// is not a saving.
///
/// NOTHING HERE ASSERTS A TIMING. The numbers are this machine's; what is asserted is the recall
/// difference, which is a property of the algorithms and not of the machine.
/// </summary>
public sealed class PreselectionCostTests
{
	private const int _UTF8_FILE_COUNT = 20_000;
	private const int _TWO_BYTE_FILE_COUNT = 2_000;
	private const int _ANCHOR_INTERVAL = 50;
	private const string _ANCHOR_LINE = "var credentials = NewExemplarCredentials(user, secret);\n";
	private const string _ORDINARY_LINE = "public sealed class Ordinary { }\n";
	private const string _EXTENSION_OF_THE_REFERENCE_PROFILE = ".ex1";
	private const string _COST_FORMAT = "{0}: {1} buffers in {2:F4} s, candidates found {3}";

	private readonly ITestOutputHelper _output;

	public PreselectionCostTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void PreselectingOverDecodedTextCostsTheSearchAndBuysTheRecall()
	{
		ILanguageAnalyzer analyzer = ReferenceAnalyzer();
		List<byte[]> buffers = BuildBuffers();

		int overDecodedText = MeasureOverDecodedText(analyzer, buffers);
		int overTheByteBuffer = MeasureOverTheByteBuffer(analyzer, buffers);

		Assert.True(
			overDecodedText > overTheByteBuffer,
			"The byte-buffer search must miss the two-byte-encoded files, which is the failure DR-06 rejects it for.");

		Assert.Equal(_UTF8_FILE_COUNT / _ANCHOR_INTERVAL + _TWO_BYTE_FILE_COUNT, overDecodedText);
	}

	private static ILanguageAnalyzer ReferenceAnalyzer()
	{
		LanguageCatalog catalog = ExemplarFixtureCatalog.Build();

		_ = catalog.TryGetAnalyzerForFileExtension(
			_EXTENSION_OF_THE_REFERENCE_PROFILE,
			out ILanguageAnalyzer? analyzer);

		return analyzer!;
	}

	/// <summary>
	/// Mostly ordinary single-byte files with an anchor every fiftieth, plus a block of
	/// two-byte-encoded files that every carry one. The second block is the whole argument: it is
	/// what one encoding's worth of a repository looks like.
	/// </summary>
	private static List<byte[]> BuildBuffers()
	{
		List<byte[]> buffers = [];
		UnicodeEncoding twoByteEncoding = new(bigEndian: false, byteOrderMark: false);

		for (int file = 0; file < _UTF8_FILE_COUNT; file++)
		{
			buffers.Add(Encoding.UTF8.GetBytes(file % _ANCHOR_INTERVAL == 0 ? _ANCHOR_LINE : _ORDINARY_LINE));
		}

		for (int file = 0; file < _TWO_BYTE_FILE_COUNT; file++)
		{
			buffers.Add(twoByteEncoding.GetBytes(_ANCHOR_LINE));
		}

		return buffers;
	}

	private int MeasureOverDecodedText(ILanguageAnalyzer analyzer, List<byte[]> buffers)
	{
		List<string> decoded = [.. buffers.Select(Decode)];
		Stopwatch stopwatch = Stopwatch.StartNew();
		int candidates = decoded.Count(text => CandidatePreselection.IsCandidate(analyzer, text));

		stopwatch.Stop();
		Report("over decoded text", buffers.Count, stopwatch, candidates);

		return candidates;
	}

	/// <summary>
	/// The byte-buffer algorithm, written here and nowhere else. It exists to be measured and to be
	/// shown missing what it misses; no production path takes a byte buffer.
	/// </summary>
	private int MeasureOverTheByteBuffer(ILanguageAnalyzer analyzer, List<byte[]> buffers)
	{
		List<string> transliterated = [.. buffers.Select(Encoding.Latin1.GetString)];
		Stopwatch stopwatch = Stopwatch.StartNew();
		int candidates = transliterated.Count(text => CandidatePreselection.IsCandidate(analyzer, text));

		stopwatch.Stop();
		Report("over the byte buffer, one byte per character", buffers.Count, stopwatch, candidates);

		return candidates;
	}

	/// <summary>The decode the inventory pass performs, standing in for the reader's own.</summary>
	private static string Decode(byte[] bytes)
	{
		return bytes.Length > 1 && bytes[1] == 0
			? new UnicodeEncoding(bigEndian: false, byteOrderMark: false).GetString(bytes)
			: Encoding.UTF8.GetString(bytes);
	}

	private void Report(string label, int bufferCount, Stopwatch stopwatch, int candidates)
	{
		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			_COST_FORMAT,
			label,
			bufferCount,
			stopwatch.Elapsed.TotalSeconds,
			candidates));
	}
}
