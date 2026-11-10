using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// The anchoring value "end of line after optional blanks" for a here-string
/// opener, with the closer still anchored to the start of the line, and the typographic quote pairs
/// expressed as ordinary quote rules.
///
/// The text below is invented and shaped like PowerShell scripts.
/// </summary>
public sealed class HereStringAnchoringTests
{
	private const string _PLAIN_OPENER = "$h = @\"\nline one\nline two\n\"@\n$after = 1\n";
	private const string _OPENER_WITH_SPACES = "$h = @\"   \nline one\n\"@\n$after = 1\n";
	private const string _OPENER_WITH_TABS = "$h = @\"\t\t\nline one\n\"@\n$after = 1\n";
	private const string _OPENER_WITH_MIXED_BLANKS = "$h = @'  \t \nline 'one'\n'@\n$after = 1\n";
	private const string _OPENER_FOLLOWED_BY_TEXT = "$h = @\" text\nstill\n\"@\n$after = 1\n";
	private const string _INDENTED_CLOSER = "$h = @\"\nline one\n  \"@\nline two\n\"@\n$after = 1\n";
	private const string _TYPOGRAPHIC_SOURCE = "$d = “double $x \"inner\"”\n$s = ‘single \"inner\"’\n$after = 1\n";

	[Fact]
	public void AnOpenerAtTheEndOfTheLineIsAccepted()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_PLAIN_OPENER, countsNestedParentheses: false);

		ZoneAssertions.SpansZone(mask, _PLAIN_OPENER, "@\"", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _PLAIN_OPENER, "\nline one\nline two\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _PLAIN_OPENER, "\"@", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _PLAIN_OPENER, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData(_OPENER_WITH_SPACES)]
	[InlineData(_OPENER_WITH_TABS)]
	public void TrailingSpacesOrTabsAfterTheOpenerAreAccepted(string source)
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(source, countsNestedParentheses: false);

		ZoneAssertions.SpansZone(mask, source, "@\"", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "line one", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "\"@", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheLiteralFormAcceptsMixedTrailingBlanks()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_OPENER_WITH_MIXED_BLANKS, countsNestedParentheses: false);

		ZoneAssertions.SpansZone(mask, _OPENER_WITH_MIXED_BLANKS, "@'", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _OPENER_WITH_MIXED_BLANKS, "line 'one'", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _OPENER_WITH_MIXED_BLANKS, "'@", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _OPENER_WITH_MIXED_BLANKS, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Non-blank text after the opener makes it an ordinary quote, so the at sign is code and the string ends at the next quote.</summary>
	[Fact]
	public void NonBlankTextAfterTheOpenerIsNotAnOpener()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_OPENER_FOLLOWED_BY_TEXT, countsNestedParentheses: false);
		int atSign = _OPENER_FOLLOWED_BY_TEXT.IndexOf('@', StringComparison.Ordinal);
		int closerAtSign = _OPENER_FOLLOWED_BY_TEXT.LastIndexOf('@');

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(atSign));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(atSign + 1));
		ZoneAssertions.SpansZone(mask, _OPENER_FOLLOWED_BY_TEXT, " text\nstill\n", LexicalZone.StringBody);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(closerAtSign));
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>The closer stays anchored to the start of the line: an indented one is body text and the literal ends at the next column-zero closer.</summary>
	[Fact]
	public void AnIndentedCloserDoesNotEndTheHereString()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_INDENTED_CLOSER, countsNestedParentheses: false);

		ZoneAssertions.SpansZone(mask, _INDENTED_CLOSER, "\nline one\n  \"@\nline two\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _INDENTED_CLOSER, "\"@", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, _INDENTED_CLOSER, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>An opener followed only by blanks up to the end of the text is still an opener, and the unclosed literal keeps the unterminated-construct policy.</summary>
	[Fact]
	public void ABlankRunOnlyCountsUpToTheEndOfTheText()
	{
		const string source = "$h = @\"   ";

		ZoneMask mask = PowerShellShapedProfile.Scan(source, countsNestedParentheses: false);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(source.IndexOf('@', StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	[Fact]
	public void TheVocabularyExpressesTheTypographicQuotePairs()
	{
		ZoneMask mask = PowerShellShapedProfile.Scan(_TYPOGRAPHIC_SOURCE, countsNestedParentheses: false);

		ZoneAssertions.SpansZone(mask, _TYPOGRAPHIC_SOURCE, "“", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _TYPOGRAPHIC_SOURCE, "double $x \"inner\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TYPOGRAPHIC_SOURCE, "”", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _TYPOGRAPHIC_SOURCE, "‘", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _TYPOGRAPHIC_SOURCE, "single \"inner\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _TYPOGRAPHIC_SOURCE, "’", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, _TYPOGRAPHIC_SOURCE, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}
}
