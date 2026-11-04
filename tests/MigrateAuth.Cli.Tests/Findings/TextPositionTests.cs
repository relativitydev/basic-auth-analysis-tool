using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Findings;

/// <summary>
/// THE OFFSET-TO-POSITION CONVERSION, AGAINST A FILE COUNTED BY HAND.
///
/// Every expectation here is derived from a text whose lines have DIFFERENT lengths, one of which
/// is empty. Equal-length lines would let an implementation that divided by a line width pass, and
/// a text with no empty line would let one that treated consecutive line feeds as a single break
/// pass - both of which are the ways this calculation is normally got wrong.
/// </summary>
public sealed class TextPositionTests
{
	/// <summary>
	/// Four lines of deliberately unequal length, the third of them empty. Offsets, counted:
	///
	/// <code>
	/// 0..6    "alpha\n"      line 1, the line feed at offset 5
	/// 6..15   "bravoes\n"    line 2, the line feed at offset 13
	/// 14..15  "\n"           line 3, empty, the line feed at offset 14
	/// 15..19  "echo"         line 4, ending at offset 19, which is the text's length
	/// </code>
	/// </summary>
	private const string _FOUR_LINES_OF_UNEQUAL_LENGTH = "alpha\nbravoes\n\necho";

	[Theory]
	[InlineData(0, 1, 1)]
	[InlineData(4, 1, 5)]
	[InlineData(6, 2, 1)]
	[InlineData(12, 2, 7)]
	[InlineData(15, 4, 1)]
	[InlineData(18, 4, 4)]
	public void TheLineAndColumnAreCountedFromOne(int offset, int expectedLine, int expectedColumn)
	{
		TextPosition position = TextPositions.LineAndColumnOf(_FOUR_LINES_OF_UNEQUAL_LENGTH, offset);

		Assert.Equal(expectedLine, position.Line);
		Assert.Equal(expectedColumn, position.Column);
	}

	/// <summary>
	/// AN OFFSET LANDING EXACTLY ON A LINE FEED BELONGS TO THE LINE THAT FEED ENDS. The line feed
	/// at offset 5 terminates line 1 and is its sixth character; reading it as the first character
	/// of line 2 would be off by one in the direction a reader notices, since it would report a
	/// line the construct is not on.
	/// </summary>
	[Fact]
	public void AnOffsetOnALineFeedBelongsToTheLineItEnds()
	{
		TextPosition position = TextPositions.LineAndColumnOf(_FOUR_LINES_OF_UNEQUAL_LENGTH, offset: 5);

		Assert.Equal(1, position.Line);
		Assert.Equal(6, position.Column);
	}

	/// <summary>
	/// The empty line is a line. Its own line feed sits at offset 14 and is the only character it
	/// has, so column one of line three is where a converter that collapsed consecutive line feeds
	/// would instead report line four.
	/// </summary>
	[Fact]
	public void AnEmptyLineIsCountedLikeAnyOther()
	{
		TextPosition position = TextPositions.LineAndColumnOf(_FOUR_LINES_OF_UNEQUAL_LENGTH, offset: 14);

		Assert.Equal(3, position.Line);
		Assert.Equal(1, position.Column);
	}

	/// <summary>The position one past the last character is a legitimate offset, not a defect.</summary>
	[Fact]
	public void TheOffsetMayEqualTheTextsLength()
	{
		TextPosition position = TextPositions.LineAndColumnOf(
			_FOUR_LINES_OF_UNEQUAL_LENGTH,
			_FOUR_LINES_OF_UNEQUAL_LENGTH.Length);

		Assert.Equal(4, position.Line);
		Assert.Equal(5, position.Column);
	}

	/// <summary>Empty text has one line and one column, which is where a reader's cursor would be.</summary>
	[Fact]
	public void EmptyTextPlacesTheOnlyOffsetAtTheOrigin()
	{
		TextPosition position = TextPositions.LineAndColumnOf(string.Empty, offset: 0);

		Assert.Equal(TextPositions.FIRST_LINE, position.Line);
		Assert.Equal(TextPositions.FIRST_COLUMN, position.Column);
	}

	/// <summary>
	/// THE CONVERSION IS SOUND ONLY BECAUSE LINE ENDINGS WERE NORMALIZED FIRST, and this pins the
	/// dependency rather than trusting it. The same source carrying the other platform's line
	/// endings must produce the same line and column once normalized - which is the whole reason
	/// normalization happens before any content-derived calculation, since the version control
	/// client converts line endings by default on one of the three platforms and THE SAME COMMIT
	/// THEREFORE HAS DIFFERENT BYTES depending on where it was checked out.
	/// </summary>
	[Fact]
	public void TheSameSourceUnderEitherLineEndingConventionProducesOnePosition()
	{
		const string lineFeedSource = "alpha\nbravo\ncharlie";
		const string carriageReturnSource = "alpha\r\nbravo\r\ncharlie";
		const string anchorText = "charlie";

		string normalizedLineFeedSource = TextNormalization.Normalize(lineFeedSource);
		string normalizedCarriageReturnSource = TextNormalization.Normalize(carriageReturnSource);

		TextPosition fromLineFeeds = TextPositions.LineAndColumnOf(
			normalizedLineFeedSource,
			normalizedLineFeedSource.IndexOf(anchorText, StringComparison.Ordinal));
		TextPosition fromCarriageReturns = TextPositions.LineAndColumnOf(
			normalizedCarriageReturnSource,
			normalizedCarriageReturnSource.IndexOf(anchorText, StringComparison.Ordinal));

		Assert.Equal(3, fromLineFeeds.Line);
		Assert.Equal(1, fromLineFeeds.Column);
		Assert.Equal(fromLineFeeds, fromCarriageReturns);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(20)]
	public void AnOffsetOutsideTheTextIsRefused(int offset)
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => TextPositions.LineAndColumnOf(_FOUR_LINES_OF_UNEQUAL_LENGTH, offset));
	}
}
