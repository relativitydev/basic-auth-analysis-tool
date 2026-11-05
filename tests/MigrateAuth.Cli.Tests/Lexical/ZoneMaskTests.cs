using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// The mask's public surface: one zone per position, the boundary the scanner vouches for, and
/// the difference between an abandoned scan and a recovered one.
/// </summary>
public sealed class ZoneMaskTests
{
	private const string _PROFILE_NAME = "ZoneMaskProfile";
	private const string _IDENTIFIER_PATTERN = "[A-Za-z_][A-Za-z0-9_]*";
	private const string _LINE_COMMENT_OPENING = "//";
	private const string _BLOCK_COMMENT_OPENING = "/*";
	private const string _BLOCK_COMMENT_CLOSING = "*/";
	private const string _QUOTE = "\"";

	[Fact]
	public void ACompleteScanIsValidThroughItsWholeLength()
	{
		const string source = "a // note\nb";

		ZoneMask mask = Scan(source, NewlinePolicy.Forbidden);

		Assert.Equal(source.Length, mask.Length);
		Assert.Equal(source.Length, mask.ValidThroughPosition);
		Assert.Equal(source.Length, mask.Zones.Length);
		Assert.True(mask.IsCompletelyValid);
		Assert.False(mask.WasAbandonedMidFile);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
	}

	[Fact]
	public void ZoneAtReportsTheZoneOfEachPosition()
	{
		const string source = "a // note\nb";

		ZoneMask mask = Scan(source, NewlinePolicy.Forbidden);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(0));
		Assert.Equal(LexicalZone.Comment, mask.ZoneAt(source.IndexOf(_LINE_COMMENT_OPENING, StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.Length - 1));
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(7)]
	[InlineData(int.MaxValue)]
	public void ZoneAtRefusesAPositionOutsideTheMaskedText(int position)
	{
		ZoneMask mask = Scan("a // no", NewlinePolicy.Forbidden);

		Assert.Throws<ArgumentOutOfRangeException>(() => mask.ZoneAt(position));
	}

	[Fact]
	public void AnEmptyTextHasAnEmptyValidMask()
	{
		ZoneMask mask = Scan(string.Empty, NewlinePolicy.Forbidden);

		Assert.Equal(0, mask.Length);
		Assert.True(mask.IsCompletelyValid);
		Assert.False(mask.IsTrustworthyAt(0));
	}

	[Fact]
	public void AnUnterminatedBlockCommentIsTrustworthyOnlyBeforeItsOpening()
	{
		const string source = "code /* never closed";
		int openingPosition = source.IndexOf(_BLOCK_COMMENT_OPENING, StringComparison.Ordinal);

		ZoneMask mask = Scan(source, NewlinePolicy.Forbidden);

		Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		Assert.Equal(openingPosition, mask.ValidThroughPosition);
		Assert.False(mask.IsCompletelyValid);
		Assert.True(mask.WasAbandonedMidFile);
		Assert.True(mask.IsTrustworthyAt(openingPosition - 1));
		Assert.False(mask.IsTrustworthyAt(openingPosition));
		Assert.False(mask.IsTrustworthyAt(source.Length - 1));
		Assert.False(mask.IsTrustworthyAt(-1));
	}

	[Fact]
	public void AnAbandonedScanStillAnswersEveryPositionAndAnalyzesTheWholeText()
	{
		const string source = "code /* never closed";

		ZoneMask mask = Scan(source, NewlinePolicy.Forbidden);

		for (int position = 0; position < source.Length; position++)
		{
			Assert.True(mask.IsAnalyzableAt(position), $"position {position}");
		}

		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	[Fact]
	public void ARawNewlineInsideAForbiddingLiteralIsRecoveredAndNotAbandoned()
	{
		const string source = "a \"abc\nb";

		ZoneMask mask = Scan(source, NewlinePolicy.Forbidden);

		Assert.Equal(LexicalAnomalyReason.NewlineInsideALiteral, mask.AnomalyReason);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		Assert.True(mask.IsCompletelyValid);
		Assert.False(mask.WasAbandonedMidFile);
		Assert.Equal(source.Length, mask.ValidThroughPosition);
	}

	private static ZoneMask Scan(string source, NewlinePolicy newlinePolicy)
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_IDENTIFIER_PATTERN, RegexOptions.CultureInvariant),
			LineCommentRules = [new LineCommentRule { Opening = _LINE_COMMENT_OPENING }],
			BlockCommentRules = [new BlockCommentRule { Opening = _BLOCK_COMMENT_OPENING, Closing = _BLOCK_COMMENT_CLOSING }],
			QuoteRules =
			[
				new QuoteRule
				{
					Opening = _QUOTE,
					Closing = _QUOTE,
					NewlinePolicy = newlinePolicy,
					DoubledClosingEscapes = false,
					DelimiterLength = DelimiterLength.Fixed
				}
			]
		};

		return new LexicalScanner(profile).Scan(source);
	}
}
