using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reading;

/// <summary>
/// NORMALIZATION FIRST, THEN EVERY NUMBER DERIVED FROM THE TEXT.
///
/// The version control client rewrites line endings on checkout on one of the three platforms,
/// so the same commit has different bytes on different machines. Any number taken from the raw
/// bytes therefore differs for a repository that is identical, and deterministic output cannot hold. These tests
/// pin the three conventions to the same answer, which is what makes that true.
/// </summary>
public sealed class TextNormalizationTests
{
	private const int _EXPECTED_LINE_COUNT = 3;
	private const int _LONGEST_EXPECTED_LINE_LENGTH = 6;
	private const string _ASTRAL_CHARACTER = "\U0001F600";
	private const int _ASTRAL_CHARACTER_CODE_UNITS = 2;

	/// <summary>
	/// Written as code points rather than as literals because they ARE line terminators to the
	/// compiler: pasted into a source file they end the line the string is on.
	/// </summary>
	private const char _UNICODE_LINE_SEPARATOR = (char)0x2028;

	private const char _UNICODE_PARAGRAPH_SEPARATOR = (char)0x2029;

	[Theory]
	[InlineData("first\nsecond\nthird\n")]
	[InlineData("first\r\nsecond\r\nthird\r\n")]
	[InlineData("first\rsecond\rthird\r")]
	[InlineData("first\nsecond\r\nthird\r")]
	public void EveryLineEndingConventionCountsTheSameLines(string text)
	{
		string normalizedText = TextNormalization.Normalize(text);

		Assert.True(TextNormalization.TryMeasure(normalizedText, out TextMeasurement measurement));
		Assert.Equal(_EXPECTED_LINE_COUNT, measurement.LineCount);
		Assert.Equal(_LONGEST_EXPECTED_LINE_LENGTH, measurement.LongestLineLength);
		Assert.Equal("first\nsecond\nthird\n", normalizedText);
	}

	[Fact]
	public void AFinalLineWithNoTerminatorStillCounts()
	{
		Assert.True(TextNormalization.TryMeasure("first\nsecond", out TextMeasurement measurement));
		Assert.Equal(2, measurement.LineCount);
	}

	[Fact]
	public void AnEmptyFileHasNoLines()
	{
		Assert.True(TextNormalization.TryMeasure(string.Empty, out TextMeasurement measurement));
		Assert.Equal(0, measurement.LineCount);
		Assert.Equal(0, measurement.LongestLineLength);
	}

	/// <summary>
	/// A DECLARED CHOICE RATHER THAN AN OVERSIGHT. The Unicode line and paragraph separators do
	/// not end a line, and the reason to pin it in a test is that reversing it would silently
	/// move the line number every finding in such a file carries.
	/// </summary>
	[Fact]
	public void UnicodeLineAndParagraphSeparatorsDoNotEndALine()
	{
		string text = $"first{_UNICODE_LINE_SEPARATOR}second{_UNICODE_PARAGRAPH_SEPARATOR}third";

		Assert.True(TextNormalization.TryMeasure(TextNormalization.Normalize(text), out TextMeasurement measurement));
		Assert.Equal(1, measurement.LineCount);
	}

	/// <summary>
	/// Positions are counted in UTF-16 code units, declared because a character outside the basic
	/// multilingual plane occupies two of them and would measure - and later sort - differently
	/// under any other unit.
	/// </summary>
	[Fact]
	public void PositionsAreCountedInUtf16CodeUnits()
	{
		Assert.True(TextNormalization.TryMeasure(_ASTRAL_CHARACTER, out TextMeasurement measurement));
		Assert.Equal(_ASTRAL_CHARACTER_CODE_UNITS, measurement.LongestLineLength);
	}

	[Fact]
	public void ALineAtTheCeilingIsAccepted()
	{
		string line = new('a', ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);

		Assert.True(TextNormalization.TryMeasure(line, out TextMeasurement measurement));
		Assert.Equal(ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS, measurement.LongestLineLength);
	}

	/// <summary>One character past the ceiling, and the ceiling is breached by the LAST line.</summary>
	[Fact]
	public void ALineOneCharacterPastTheCeilingIsRefusedWhereverItIs()
	{
		string overLongLine = new('a', ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS + 1);

		Assert.False(TextNormalization.TryMeasure($"short\n{overLongLine}", out TextMeasurement _));
		Assert.False(TextNormalization.TryMeasure($"{overLongLine}\nshort", out TextMeasurement _));
	}

	[Fact]
	public void TextWithoutCarriageReturnsIsNotRebuilt()
	{
		string text = "first\nsecond\n";

		Assert.Same(text, TextNormalization.Normalize(text));
	}

	[Fact]
	public void TheCeilingsAreTheOnesTheDesignFixed()
	{
		Assert.Equal(2 * 1024 * 1024, ReadingCeilings.MAXIMUM_FILE_SIZE_IN_BYTES);
		Assert.Equal(10_000, ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);
		Assert.Equal(100, ReadingCeilings.ENUMERATED_SKIPPED_PATHS_PER_CATEGORY);
	}
}
