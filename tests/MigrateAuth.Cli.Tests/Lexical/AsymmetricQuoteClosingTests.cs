using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// A FIXED-LENGTH RULE WHOSE OPENING AND CLOSING ARE NOT THE SAME LENGTH.
///
/// <see cref="DelimiterLength.Fixed"/> means the closing is a constant of the rule, not a run
/// that has to reach the opening's own length - that requirement belongs to
/// <see cref="DelimiterLength.VariableWithAtLeastEqualClosing"/> alone. A fixed rule whose opening runs
/// longer than its closing - a two-character opening closed by one character, the shape of a raw-literal
/// prefix over an ordinary quote - closes at its own closing; measured against the opening's length,
/// no candidate closing run would ever be long enough and the scan would read to the end of the file as
/// <see cref="LexicalAnomalyReason.UnterminatedLiteral"/>.
/// </summary>
public sealed class AsymmetricQuoteClosingTests
{
	private const string _PROFILE_NAME = "AsymmetricFixedClosing";
	private const string _ASYMMETRIC_OPENING = "@\"";
	private const string _ASYMMETRIC_CLOSING = "\"";
	private const string _IDENTIFIER_PATTERN = "[A-Za-z_][A-Za-z0-9_]*";

	/// <summary>
	/// THE BASIC SHAPE. A two-character opening closed by its one-character closing, with
	/// nothing else in the rule to confuse the reading - if this alone fails, the fault is the
	/// minimum closing length and not something the doubled close or the newline policy add.
	/// </summary>
	[Fact]
	public void ATwoCharacterOpeningClosesOnItsOneCharacterClosing()
	{
		const string source = "let s = @\"abc\" tail";

		ZoneMask mask = Scan(source, NewlinePolicy.Forbidden, doubledClosingEscapes: false);

		ZoneAssertions.SpansZone(mask, source, "@\"", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "abc", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "\"", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.False(mask.WasAbandonedMidFile);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>
	/// The same asymmetric rule with a doubled-quote escape declared. The doubled closing inside
	/// the body is consumed as a pair of body text - the doubled-close policy - and it is
	/// the SINGLE closing afterward that ends the literal, which only happens at all once the
	/// minimum closing length is read off the rule's closing rather than off the opening.
	/// </summary>
	[Fact]
	public void ADoubledClosingInsideAnAsymmetricLiteralIsBodyTextNotAClosing()
	{
		const string source = "let s = @\"a\"\"b\" tail";

		ZoneMask mask = Scan(source, NewlinePolicy.Forbidden, doubledClosingEscapes: true);

		ZoneAssertions.SpansZone(mask, source, "@\"", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "a\"\"b", LexicalZone.StringBody);

		// The three earlier quote characters are the opening delimiter's own and the doubled
		// pair's two body characters (occurrences 0..2); the fourth is the closing delimiter.
		ZoneAssertions.SpansZone(mask, source, "\"", LexicalZone.Delimiter, occurrence: 3);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.False(mask.WasAbandonedMidFile);
	}

	/// <summary>
	/// An asymmetric literal whose rule admits raw newlines, so the construct spans lines before
	/// its one-character closing is met. The code that follows the closing is code, which is the
	/// same trailing-code assertion every case in this file carries and is what a mask inverted by
	/// a wrong minimum closing length would fail.
	/// </summary>
	[Fact]
	public void AnAsymmetricLiteralSpanningNewlinesIsFollowedByCode()
	{
		const string source = "let s = @\"line one\nline two\" tail";

		ZoneMask mask = Scan(source, NewlinePolicy.Allowed, doubledClosingEscapes: false);

		ZoneAssertions.SpansZone(mask, source, "@\"", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "line one\nline two", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "\"", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.False(mask.WasAbandonedMidFile);
	}

	private static ZoneMask Scan(string source, NewlinePolicy newlinePolicy, bool doubledClosingEscapes)
	{
		LexicalProfile profile = BuildProfile(newlinePolicy, doubledClosingEscapes);

		return new LexicalScanner(profile).Scan(source);
	}

	private static LexicalProfile BuildProfile(NewlinePolicy newlinePolicy, bool doubledClosingEscapes)
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_IDENTIFIER_PATTERN, RegexOptions.CultureInvariant),
			LineCommentRules = ImmutableArray<LineCommentRule>.Empty,
			BlockCommentRules = ImmutableArray<BlockCommentRule>.Empty,
			QuoteRules =
			[
				new QuoteRule
				{
					Opening = _ASYMMETRIC_OPENING,
					Closing = _ASYMMETRIC_CLOSING,
					NewlinePolicy = newlinePolicy,
					DoubledClosingEscapes = doubledClosingEscapes,
					DelimiterLength = DelimiterLength.Fixed
				}
			]
		};

		return profile;
	}
}
