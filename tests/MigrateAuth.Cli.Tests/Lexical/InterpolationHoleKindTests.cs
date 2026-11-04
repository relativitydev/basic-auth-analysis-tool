using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE TWO INTERPOLATION HOLE KINDS, AND THE PROOF THAT THEY COMPOSE WITH ONE DESCENT RATHER THAN TWO.
///
/// The reference profile carries both: the bracketed kind, and the identifier-extent kind (a sigil
/// and an identifier, with no closing token). Its here-document is the identifier-extent case, and it
/// is the same rule that carries the captured terminator, which decides per occurrence whether the
/// body interpolates.
/// </summary>
public sealed class InterpolationHoleKindTests
{
	private const string _BRACKETED_PROFILE_NAME = "BracketedAndIdentifierExtent";
	private const string _BRACKETED_LITERAL_QUOTE = "\"";
	private const string _IDENTIFIER_EXTENT_LITERAL_QUOTE = "`";
	private const string _BRACKETED_HOLE_OPENING = "${";
	private const string _BRACKETED_HOLE_CLOSING = "}";
	private const string _IDENTIFIER_HOLE_SIGIL = "$";
	private const string _IDENTIFIER_PATTERN = "[A-Za-z_][A-Za-z0-9_]*";
	private const string _NESTED_HOLE_INTERIOR = "`$name`";
	private const string _INNERMOST_HOLE_INTERIOR = "x";

	/// <summary>
	/// Eight is the recursion bound and it is the engine's, not a profile's. Nine is one past it, and
	/// the anomaly arrives from the explicit counter rather than from the stack - nine frames of
	/// recursive descent is nothing, which is exactly the point: a test that proved the bound by
	/// actually overflowing would prove the opposite of what it claims.
	/// </summary>
	private const int _DEEPEST_ADMITTED_NESTING = 8;
	private const int _FIRST_REFUSED_NESTING = 9;

	/// <summary>
	/// The captured terminator decides per occurrence, and an unquoted terminator means the
	/// body interpolates. The identifier after the sigil is CODE, which is the whole point of the
	/// second hole kind.
	/// </summary>
	[Fact]
	public void AnIdentifierExtentHoleMakesItsIdentifierCodeInsideALiteralBody()
	{
		const string source = "let d = <<EOT\nbody $name more\nEOT\ntail";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, normalized, "<<EOT", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, normalized, "$", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, normalized, "name", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, normalized, " more", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, normalized, "\ntail", LexicalZone.Code);
		Assert.False(mask.WasAbandonedMidFile);
	}

	/// <summary>
	/// The other half of the per-occurrence decision: a QUOTED terminator means this occurrence
	/// does not interpolate, so the identical sigil is body text. Neither the rule nor the
	/// scanner changed between this case and the one above - only the occurrence did.
	/// </summary>
	[Fact]
	public void TheSameRuleDoesNotInterpolateWhereTheOccurrenceQuotedItsTerminator()
	{
		const string source = "let d = <<'EOT'\nbody $name more\nEOT\ntail";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, normalized, "<<'EOT'", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, normalized, "$name more", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, normalized, "\ntail", LexicalZone.Code);
	}

	/// <summary>
	/// A sigil the identifier expression does not match AT is not a hole at all. Without the
	/// anchoring the expression would find an identifier later on the line and open a hole over
	/// text that has nothing to do with it.
	/// </summary>
	[Fact]
	public void ASigilNotFollowedByAnIdentifierOpensNoHole()
	{
		const string source = "let d = <<EOT\nprice $ 5 then name\nEOT\ntail";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, normalized, "price $ 5 then name", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, normalized, "\ntail", LexicalZone.Code);
	}

	/// <summary>Positional anchoring, on a captured terminator's closing.</summary>
	[Fact]
	public void ACapturedTerminatorThatIsNotAtTheStartOfALineDoesNotCloseTheLiteral()
	{
		const string source = "let d = <<EOT\nxEOT still body\nEOT\ntail";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, normalized, "xEOT still body", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, normalized, "\ntail", LexicalZone.Code);
	}

	/// <summary>
	/// Positional anchoring, on the opening. Without it the here-string of an
	/// administration shell closes early, and here the mirror case: an opening that does not end
	/// its line opens nothing, so the whole line stays code.
	/// </summary>
	[Fact]
	public void AnOpeningThatDoesNotEndItsLineOpensNoHereDocument()
	{
		const string source = "let d = <<EOT trailing\nordinary code\n";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, normalized, "let d = <<EOT trailing", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, normalized, LexicalZone.StringBody);
		Assert.False(mask.WasAbandonedMidFile);
	}

	/// <summary>
	/// THE BOUND HOLDS AT EIGHT. Nesting to exactly the bound is legitimate code as far as the
	/// engine is concerned and produces no anomaly, which is the half of the bound that a test
	/// asserting only the refusal would leave unproven - a scanner that refused everything would
	/// pass that one.
	/// </summary>
	[Fact]
	public void NestingBracketedHolesToTheBoundIsNotAnAnomaly()
	{
		string source = NestedBracketedHoles(_DEEPEST_ADMITTED_NESTING);

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.False(mask.WasAbandonedMidFile);
		ZoneAssertions.SpansZone(mask, source, _INNERMOST_HOLE_INTERIOR, LexicalZone.Code);
	}

	/// <summary>
	/// AND REFUSES AT NINE, BEFORE THE DESCENT AND NOT BY MAKING IT. Nine frames is trivial
	/// stack; what produces the anomaly is the counter checked ahead of the call. The boundary it
	/// declares is the outermost construct's opening, because everything from there on was being
	/// read under an assumption the scanner has just given up.
	/// </summary>
	[Fact]
	public void NestingBracketedHolesPastTheBoundIsAnAnomalyAndNeverAProcessFailure()
	{
		string source = NestedBracketedHoles(_FIRST_REFUSED_NESTING);

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.InterpolationDepthExceeded, mask.AnomalyReason);
		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(0, mask.ValidThroughPosition);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	/// <summary>
	/// BOTH KINDS COUNT AGAINST ONE COUNTER, which is what "the recursion bound counts both"
	/// means operationally. Seven bracketed levels leave room for an identifier-extent hole at
	/// the eighth, and its identifier is code.
	/// </summary>
	[Fact]
	public void AnIdentifierExtentHoleAtTheBoundIsAdmittedAndItsIdentifierIsCode()
	{
		string source = BracketedHolesAroundAnIdentifierExtentHole(_DEEPEST_ADMITTED_NESTING - 1);

		ZoneMask mask = ScanWithBothHoleKinds(source);

		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		ZoneAssertions.SpansZone(mask, source, "name", LexicalZone.Code);
	}

	/// <summary>
	/// And one level deeper the identifier-extent hole is the descent that gets refused. The
	/// anomaly is raised on the identifier-extent path, which is the only way to show that path
	/// checks the bound at all - an identifier cannot contain a literal, so identifier-extent
	/// holes cannot nest inside each other and the mixed case is the only reachable one.
	/// </summary>
	[Fact]
	public void AnIdentifierExtentHolePastTheBoundIsRefusedOnItsOwnPath()
	{
		string source = BracketedHolesAroundAnIdentifierExtentHole(_DEEPEST_ADMITTED_NESTING);

		ZoneMask mask = ScanWithBothHoleKinds(source);

		Assert.Equal(LexicalAnomalyReason.InterpolationDepthExceeded, mask.AnomalyReason);
		Assert.True(mask.WasAbandonedMidFile);
	}

	/// <summary>
	/// A profile carrying one rule of each hole kind. It exists because the reference profile
	/// cannot express the mixed nesting above: its identifier-extent rule is a here-document,
	/// whose opening must end a line, so it cannot sit inside a bracketed hole on one line.
	/// </summary>
	private static ZoneMask ScanWithBothHoleKinds(string source)
	{
		LexicalProfile profile = new()
		{
			ProfileName = _BRACKETED_PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_IDENTIFIER_PATTERN, RegexOptions.CultureInvariant),
			LineCommentRules = ImmutableArray<LineCommentRule>.Empty,
			BlockCommentRules = ImmutableArray<BlockCommentRule>.Empty,
			QuoteRules =
			[
				new QuoteRule
				{
					Opening = _BRACKETED_LITERAL_QUOTE,
					Closing = _BRACKETED_LITERAL_QUOTE,
					NewlinePolicy = NewlinePolicy.Allowed,
					InterpolationOpening = _BRACKETED_HOLE_OPENING,
					InterpolationClosing = _BRACKETED_HOLE_CLOSING
				},
				new QuoteRule
				{
					Opening = _IDENTIFIER_EXTENT_LITERAL_QUOTE,
					Closing = _IDENTIFIER_EXTENT_LITERAL_QUOTE,
					NewlinePolicy = NewlinePolicy.Allowed,
					InterpolationOpening = _IDENTIFIER_HOLE_SIGIL,
					InterpolationIdentifierExtent = new Regex(_IDENTIFIER_PATTERN, RegexOptions.CultureInvariant)
				}
			]
		};

		return new LexicalScanner(profile).Scan(source);
	}

	/// <summary>
	/// A literal opening a hole opening a literal, repeated. Each repetition costs exactly one
	/// against the bound, because the bound counts holes and a nested literal is only ever
	/// reachable through one.
	/// </summary>
	private static string NestedBracketedHoles(int depth)
	{
		StringBuilder source = new();

		for (int level = 0; level < depth; level++)
		{
			source.Append(_BRACKETED_LITERAL_QUOTE).Append(_BRACKETED_HOLE_OPENING);
		}

		source.Append(_INNERMOST_HOLE_INTERIOR);

		for (int level = 0; level < depth; level++)
		{
			source.Append(_BRACKETED_HOLE_CLOSING).Append(_BRACKETED_LITERAL_QUOTE);
		}

		return source.ToString();
	}

	private static string BracketedHolesAroundAnIdentifierExtentHole(int bracketedDepth)
	{
		StringBuilder source = new();

		for (int level = 0; level < bracketedDepth; level++)
		{
			source.Append(_BRACKETED_LITERAL_QUOTE).Append(_BRACKETED_HOLE_OPENING);
		}

		source.Append(_NESTED_HOLE_INTERIOR);

		for (int level = 0; level < bracketedDepth; level++)
		{
			source.Append(_BRACKETED_HOLE_CLOSING).Append(_BRACKETED_LITERAL_QUOTE);
		}

		return source.ToString();
	}
}
