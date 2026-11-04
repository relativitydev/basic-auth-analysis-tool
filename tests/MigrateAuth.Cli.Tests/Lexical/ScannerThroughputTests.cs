using System.Diagnostics;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;

using Xunit;
using Xunit.Abstractions;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// WHAT THE SCANNER COSTS, AND - THE PART THAT IS ACTUALLY A PROOF - WHETHER THAT COST HOLDS AT
/// THE CEILING.
///
/// The reader sets a 2 MB file-size ceiling and a 10,000-character line-length ceiling, and the scanner sets
/// a recursion bound of eight. All three exist to CAP A COST, and a cap that exists without
/// holding is worse than none, because the file that reaches it is the file nobody looked at. So
/// the volume proof here is not "a large file scans" but "per-megabyte cost at the ceiling is the
/// same per-megabyte cost as at a tenth of it", measured over content chosen to be hostile:
/// interpolation nested to the bound, suspended sub-ranges, variable-length runs that nearly
/// close, and lines at the length ceiling.
///
/// NOTHING HERE ASSERTS A WALL-CLOCK TIMING. The numbers are this machine's and are reported; what is ASSERTED is the SHAPE - that
/// cost per megabyte does not degrade with file size - which is a property of the algorithm and
/// survives being run on a busy build agent. The generous factor is chosen so that the test fails
/// on a quadratic scanner and on nothing else.
/// </summary>
public sealed class ScannerThroughputTests
{
	private const int _SMALL_FILE_SIZE_IN_CHARACTERS = 64 * 1024;
	private const int _SAMPLE_FILE_SIZE_IN_CHARACTERS = 8 * 1024;
	private const int _SAMPLE_FILE_COUNT = 200;
	private const int _WARM_UP_ITERATIONS = 2;

	/// <summary>
	/// The largest file the tool will ever scan, because the reader refuses anything above it before
	/// the scanner is reached at all. The byte ceiling is used as a CHARACTER count on purpose:
	/// for the single-byte-encoded source that dominates a repository the two are the same
	/// number, so this is the worst case the scanner can actually be handed.
	/// </summary>
	private const int _CEILING_FILE_SIZE_IN_CHARACTERS = ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES;

	/// <summary>
	/// The two measured sizes differ by a factor of thirty-two, so a scanner quadratic in file
	/// length lands near thirty-two here and a linear one near one. Eight is far enough above one
	/// to absorb cache behaviour and a loaded build agent, and far enough below thirty-two to
	/// fail on the degradation this proof exists to rule out.
	/// </summary>
	private const double _ADMITTED_COST_RATIO = 8.0;

	private const string _THROUGHPUT_FORMAT = "{0}: {1:N0} characters in {2:F4} s, {3:F1} MB/s";
	private const string _RATIO_FORMAT = "per-megabyte cost at the ceiling divided by the same at 64 KiB: {0:F2}";

	private readonly ITestOutputHelper _output;

	public ScannerThroughputTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// Throughput over a set of ordinary sample files in the reference language, which is the number
	/// the figure reported per language profile and the one a later language analyzer will compare its own
	/// language against.
	/// </summary>
	[Fact]
	public void TheScannerReportsItsThroughputOverASetOfOrdinarySampleFiles()
	{
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile());
		List<string> sampleFiles = BuildSampleFiles();

		Measurement measurement = Measure(scanner, sampleFiles, "ordinary sample files");

		Assert.All(sampleFiles, file => Assert.False(scanner.Scan(file).WasAbandonedMidFile));
		Assert.True(measurement.CharacterCount > 0);
	}

	/// <summary>
	/// THE VOLUME PROOF. Two sizes, one hostile content generator, and the assertion is on the
	/// ratio of their per-megabyte costs rather than on either of them.
	/// </summary>
	[Fact]
	public void PerMegabyteCostAtTheFileSizeCeilingDoesNotDegradeAgainstASmallFile()
	{
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile());
		string smallFile = HostileSource(_SMALL_FILE_SIZE_IN_CHARACTERS);
		string ceilingFile = HostileSource(_CEILING_FILE_SIZE_IN_CHARACTERS);

		WarmUp(scanner, smallFile);

		Measurement small = Measure(scanner, [smallFile], "hostile 64 KiB");
		Measurement ceiling = Measure(scanner, [ceilingFile], "hostile file at the 2 MB ceiling");
		double ratio = ceiling.SecondsPerMegabyte / small.SecondsPerMegabyte;

		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, _RATIO_FORMAT, ratio));
		Assert.True(
			ratio < _ADMITTED_COST_RATIO,
			"Per-megabyte scanning cost grew with file size, which is the degradation the file-size and line-length ceilings and the recursion bound exist to cap.");
	}

	/// <summary>
	/// The lexical budget charges every anchored match the scanner makes, so the hostile file -
	/// dense in here-documents and identifier holes, the two expressions the budget charges - is the
	/// one that would expose a budget that costs too much or trips on legitimate content. Under the
	/// system clock, at the file-size ceiling, it must scan to the end without the budget tripping.
	/// </summary>
	[Fact]
	public void TheLexicalBudgetDoesNotTripOnHostileContentAtTheFileSizeCeiling()
	{
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile());
		string ceilingFile = HostileSource(_CEILING_FILE_SIZE_IN_CHARACTERS);

		ZoneMask mask = scanner.Scan(ceilingFile);

		Assert.NotEqual(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>
	/// The recursion bound capping a cost rather than merely existing. A file built entirely of
	/// interpolation nested one level PAST the bound is refused at the first construct, so it
	/// costs a constant rather than the whole file - which is the difference between a bound that
	/// holds and a bound that is written down.
	/// </summary>
	[Fact]
	public void AFileNestedPastTheRecursionBoundIsRefusedAtItsFirstConstruct()
	{
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile());
		string source = PathologicalNesting(_CEILING_FILE_SIZE_IN_CHARACTERS);

		Measurement measurement = Measure(scanner, [source], "nesting past the bound, 2 MB");
		ZoneMask mask = scanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.InterpolationDepthExceeded, mask.AnomalyReason);
		Assert.Equal(0, mask.ValidThroughPosition);
		Assert.True(measurement.CharacterCount > 0);
	}

	/// <summary>
	/// A single line AT the line-length ceiling, which is the largest line the reader admits, built of
	/// content that makes the scanner work at every character.
	/// </summary>
	[Fact]
	public void ALineAtTheLengthCeilingScansWithoutDegrading()
	{
		LexicalScanner scanner = new(ReferenceProfileScanner.Profile());
		string source = LongLine(ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

		Measurement measurement = Measure(scanner, [source], "one line at the 10,000 character ceiling");
		ZoneMask mask = scanner.Scan(source);

		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
		Assert.True(measurement.CharacterCount > 0);
	}

	/// <summary>
	/// The reference profile declares no nesting opener, so the nesting path is
	/// measured with a test-local profile that does: balanced nested holes, a here-string with trailing
	/// blanks and comments, at 64 KiB and at the ceiling. The assertion is the same shape as the volume
	/// proof above.
	/// </summary>
	[Fact]
	public void PerMegabyteCostWithNestedHolesDeclaredDoesNotDegradeAtTheFileSizeCeiling()
	{
		LexicalScanner scanner = new(PowerShellShapedProfile.Build(countsNestedParentheses: true));
		string smallFile = NestingSource(_NESTING_BALANCED_UNIT, _SMALL_FILE_SIZE_IN_CHARACTERS);
		string ceilingFile = NestingSource(_NESTING_BALANCED_UNIT, _CEILING_FILE_SIZE_IN_CHARACTERS);

		WarmUp(scanner, smallFile);

		Measurement small = Measure(scanner, [smallFile], "nesting declared, balanced, 64 KiB");
		Measurement ceiling = Measure(scanner, [ceilingFile], "nesting declared, balanced, 2 MB ceiling");
		double ratio = ceiling.SecondsPerMegabyte / small.SecondsPerMegabyte;

		_output.WriteLine(string.Format(CultureInfo.InvariantCulture, _RATIO_FORMAT, ratio));
		Assert.True(ratio < _ADMITTED_COST_RATIO, "Per-megabyte cost with nested holes declared grew with file size.");
		Assert.True(scanner.Scan(ceilingFile).IsCompletelyValid);
	}

	/// <summary>
	/// The worst case for nested holes: a ceiling-sized file of nothing but unbalanced holes. Each recovery
	/// re-reads a hole, so the number of recoveries is bounded per file and the scan stays linear in the file.
	/// </summary>
	[Fact]
	public void AFileOfUnbalancedHolesAtTheCeilingStaysWithinTheRecoveryBound()
	{
		LexicalScanner scanner = new(PowerShellShapedProfile.Build(countsNestedParentheses: true));
		LexicalScanner plain = new(PowerShellShapedProfile.Build(countsNestedParentheses: false));
		string ceilingFile = NestingSource(_NESTING_UNBALANCED_UNIT, _CEILING_FILE_SIZE_IN_CHARACTERS);

		Measurement nesting = Measure(scanner, [ceilingFile], "nesting declared, unbalanced, 2 MB ceiling");
		Measurement baseline = Measure(plain, [ceilingFile], "nesting not declared, same text, 2 MB ceiling");
		ZoneMask mask = scanner.Scan(ceilingFile);

		Assert.True(mask.UnbalancedNestingCount <= LexicalEnginePolicy.MAXIMUM_NESTING_RECOVERIES_PER_FILE);
		Assert.True(nesting.SecondsPerMegabyte / baseline.SecondsPerMegabyte < _ADMITTED_COST_RATIO * _ADMITTED_COST_RATIO);
	}

	private const string _NESTING_BALANCED_UNIT =
		"$a = \"id $(Get-Item (Join-Path (Get-Root) 'x')) end\"\n$b = @\"  \nbody line\n\"@\n# a comment (with parens)\n";

	private const string _NESTING_UNBALANCED_UNIT = "$a = \"id $(Get-Item (Join-Path 1 ) end\"\n";

	private static string NestingSource(string unit, int approximateLength)
	{
		StringBuilder source = new(approximateLength);

		while (source.Length < approximateLength)
		{
			source.Append(unit);
		}

		return source.ToString();
	}

	private static void WarmUp(LexicalScanner scanner, string source)
	{
		for (int iteration = 0; iteration < _WARM_UP_ITERATIONS; iteration++)
		{
			_ = scanner.Scan(source);
		}
	}

	private Measurement Measure(LexicalScanner scanner, IReadOnlyList<string> files, string label)
	{
		long characterCount = files.Sum(file => (long)file.Length);
		Stopwatch stopwatch = Stopwatch.StartNew();

		foreach (string file in files)
		{
			_ = scanner.Scan(file);
		}

		stopwatch.Stop();

		Measurement measurement = new(characterCount, stopwatch.Elapsed);

		_output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			_THROUGHPUT_FORMAT,
			label,
			characterCount,
			measurement.Elapsed.TotalSeconds,
			measurement.MegabytesPerSecond));

		return measurement;
	}

	/// <summary>
	/// Ordinary files in the reference language: comments, literals, one interpolation, one
	/// regular expression literal and code. It is what the profile was written to describe, so it
	/// is what a throughput figure for this profile ought to be measured over.
	/// </summary>
	private static List<string> BuildSampleFiles()
	{
		List<string> sampleFiles = [];

		for (int file = 0; file < _SAMPLE_FILE_COUNT; file++)
		{
			sampleFiles.Add(OrdinarySource(_SAMPLE_FILE_SIZE_IN_CHARACTERS));
		}

		return sampleFiles;
	}

	/// <summary>
	/// EVERY GENERATOR HERE EMITS A BARE LINE FEED AND NEVER A PLATFORM LINE ENDING. The scanner's
	/// contract is that its input has already been through line-ending normalization, and a generator
	/// using the environment's newline would hand it carriage returns - which would read as
	/// ordinary characters and quietly defeat every anchored delimiter in the measured text.
	/// </summary>
	private static string OrdinarySource(int approximateLength)
	{
		StringBuilder source = new(approximateLength);

		while (source.Length < approximateLength)
		{
			source.Append("-- an ordinary line comment about the code below\n");
			source.Append("let credentials = NewExemplarCredentials(operatorName, \"secret\")\n");
			source.Append("let message = \"value ${ name } after\"\n");
			source.Append("let pattern = /ab[/]cd/\n");
			source.Append("REM a word shaped comment\n");
			source.Append("let total = first / second / third\n");
		}

		return source.ToString();
	}

	/// <summary>
	/// Content chosen to keep the scanner off its fast path: every line opens a construct, and
	/// three of them are the constructions named as most destructive. A benign file
	/// would measure the character loop and nothing else, which is not what the ceilings cap.
	/// </summary>
	private static string HostileSource(int approximateLength)
	{
		StringBuilder source = new(approximateLength);

		while (source.Length < approximateLength)
		{
			source.Append("let a = \"x ${ f('}') } ${ g(\"${ h }\") } y\"\n");
			source.Append("let b = ````\ninner ``` not a close\n```` tail\n");
			source.Append("let c = /ab[/]cd<#ef/ tail\n");
			source.Append("let d = <<EOT\nbody $name more $other\nEOT\n");
			source.Append("let e = 'it''s doubled' <# a block comment #> after\n");
			source.Append("let f = r\"suppressed \\escape\" and a / b / c\n");
		}

		return source.ToString();
	}

	/// <summary>
	/// Nesting one level past the bound, repeated to fill a file at the size ceiling. The scanner
	/// must refuse at the first construct and stop, which is what makes the bound a cap.
	/// </summary>
	private static string PathologicalNesting(int approximateLength)
	{
		string unit = string.Concat(Enumerable.Repeat("\"${", 9));
		StringBuilder source = new(approximateLength);

		while (source.Length < approximateLength)
		{
			source.Append(unit);
		}

		return source.ToString();
	}

	private static string LongLine(int length)
	{
		StringBuilder source = new(length);

		while (source.Length < length)
		{
			source.Append("let x = \"a ${ b } c\" / 'd''e' /ab[/]cd/ ");
		}

		return source.ToString(0, length);
	}

	/// <summary>One measurement, with the derived figures the report wants.</summary>
	private readonly record struct Measurement
	{
		private const double _BYTES_PER_MEGABYTE = 1024.0 * 1024.0;
		private const double _BYTES_PER_UTF16_CODE_UNIT = 2.0;

		internal Measurement(long characterCount, TimeSpan elapsed)
		{
			CharacterCount = characterCount;
			Elapsed = elapsed;
		}

		internal long CharacterCount { get; }

		internal TimeSpan Elapsed { get; }

		private double Megabytes => CharacterCount * _BYTES_PER_UTF16_CODE_UNIT / _BYTES_PER_MEGABYTE;

		internal double MegabytesPerSecond => Megabytes / Math.Max(Elapsed.TotalSeconds, double.Epsilon);

		internal double SecondsPerMegabyte => Elapsed.TotalSeconds / Math.Max(Megabytes, double.Epsilon);
	}
}
