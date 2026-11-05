using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SCANNER VERIFICATION SUITE.
///
/// It covers the constructions known to break a naive scanner, including a variable-length raw
/// literal containing a shorter quote run, a closing brace inside a nested literal inside an
/// interpolation hole, a regular expression literal containing the block comment opener, a doubled
/// close consumed one character at a time, and a character class truncating a regular expression
/// literal.
///
/// EVERY CASE RUNS AGAINST THE REFERENCE PROFILE, which exercises every lexical-profile field at once.
/// A case that needed a profile written for it would be testing the case, not the contract.
/// </summary>
public sealed class LexicalScannerVerificationTests
{
	[Fact]
	public void ALineCommentRunsToTheEndOfItsLineAndTheNextLineIsCodeAgain()
	{
		const string source = "let a = 1 -- a line comment\nlet b = 2";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "-- a line comment", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "let a = 1 ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "let b = 2", LexicalZone.Code);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	/// <summary>The positional form of the context predicate, in its passing direction.</summary>
	[Fact]
	public void AWordShapedCommentOpenerOpensACommentWhereItStandsAsAWholeWord()
	{
		const string source = "REM a comment opened by a word\nlet b = 2";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "REM a comment opened by a word", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "let b = 2", LexicalZone.Code);
	}

	/// <summary>
	/// And in its failing direction, which is the reason the field exists: without the word
	/// boundary an identifier that merely begins with those three letters swallows the line.
	/// </summary>
	[Fact]
	public void AnIdentifierThatMerelyBeginsWithACommentOpenerDoesNotOpenAComment()
	{
		const string source = "let x = REMOVE(candidate)";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, source, LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Comment);
	}

	/// <summary>
	/// Case sensitivity, in keywords. Without it the lower-case form of the opener is
	/// missed and the rest of the line is read as code.
	/// </summary>
	[Fact]
	public void ACaseInsensitiveProfileOpensACommentOnTheLowerCaseFormOfItsKeywordOpener()
	{
		const string source = "rem lower case opener\nlet b = 2";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "rem lower case opener", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "let b = 2", LexicalZone.Code);
	}

	[Fact]
	public void ABlockCommentCoversItsOwnMarkersAndSpansLines()
	{
		const string source = "let a = 1 <# still\ninside #> let b = 2";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, ReferenceProfileScanner.Normalize(source), "<# still\ninside #>", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, " let b = 2", LexicalZone.Code);
	}

	/// <summary>
	/// The doubled close is consumed whole; consuming one character lets the second close the
	/// literal and inverts the mask for the rest of the line, which is why the assertion that matters
	/// here is the one about the trailing code.
	/// </summary>
	[Fact]
	public void ADoubledClosingIsConsumedAsAPairAndDoesNotCloseTheLiteral()
	{
		const string source = "let q = 'it''s doubled' after";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "it''s doubled", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " after", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "'", LexicalZone.Delimiter);
	}

	/// <summary>
	/// Why the <see cref="LexicalZone.Delimiter"/> zone exists, stated as a test: the
	/// quote that opens a literal is part of the syntactic expression, so a rule anchored there
	/// is anchored in code.
	/// </summary>
	[Fact]
	public void TheDelimitersOfALiteralAreDelimiterAndCountAsCodeWhileItsBodyDoesNot()
	{
		const string source = "let header = \"Authorization\";";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);
		int openingQuote = source.IndexOf('"');

		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(openingQuote));
		Assert.True(LexicalZones.CountsAsCode(mask.ZoneAt(openingQuote)));
		ZoneAssertions.SpansZone(mask, source, "Authorization", LexicalZone.StringBody);
		Assert.False(LexicalZones.CountsAsCode(LexicalZone.StringBody));
		Assert.False(LexicalZones.CountsAsCode(LexicalZone.Comment));
	}

	[Fact]
	public void AnEscapedClosingQuoteDoesNotCloseTheLiteral()
	{
		const string source = "let s = \"a\\\"b\" tail";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "a\\\"b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
	}

	/// <summary>The interior of an interpolation is code, and its brackets are delimiters.</summary>
	[Fact]
	public void TheInteriorOfABracketedInterpolationHoleIsCode()
	{
		const string source = "let m = \"x ${ name } y\" tail";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "${", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, " name ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "}", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "x ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " y", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
	}

	/// <summary>
	/// A closing brace inside a literal nested in an interpolation hole: the brace is body text, so
	/// the hole is still open when it is met.
	/// </summary>
	[Fact]
	public void AClosingBraceInsideANestedLiteralInsideAHoleDoesNotCloseTheHole()
	{
		const string source = "let m = \"a ${ f('}') } b\" end";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "}", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "}", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, " b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " end", LexicalZone.Code);
		Assert.False(mask.WasAbandonedMidFile);
	}

	/// <summary>The literal prefix: the same quote pair with its escape suppressed.</summary>
	[Fact]
	public void ALiteralPrefixSuppressesTheEscapeSoTheFollowingQuoteCloses()
	{
		const string source = "let r = r\"a\\\" tail";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "r\"", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "a\\", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		Assert.False(mask.WasAbandonedMidFile);
	}

	/// <summary>Case sensitivity, in delimiters, reached through the prefix's own rule.</summary>
	[Fact]
	public void ACaseInsensitiveLiteralPrefixIsRecognizedInEitherCase()
	{
		const string source = "let r = R\"a\\\" tail";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "R\"", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
	}

	/// <summary>
	/// A variable-length raw literal. The closing has to reach the length the OPENING matched, not
	/// the length the rule declares, or a shorter run inside the body closes the literal and inverts
	/// the mask's polarity for everything after it.
	/// </summary>
	[Fact]
	public void AVariableLengthLiteralIsNotClosedByAShorterRunInsideIt()
	{
		const string source = "let b = ````\ninner ``` still body\n```` tail";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, normalized, "inner ``` still body", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, normalized, "````", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, normalized, " tail", LexicalZone.Code);
		Assert.False(mask.WasAbandonedMidFile);
	}

	/// <summary>The third newline value: newlines as content, in the raw literal.</summary>
	[Fact]
	public void ALiteralWhoseRuleAdmitsNewlinesCarriesThemAsContent()
	{
		const string source = "let b = ```\nline one\nline two\n``` tail";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, normalized, "\nline one\nline two\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, normalized, " tail", LexicalZone.Code);
		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
	}

	/// <summary>
	/// The delimiter context predicate in its refusing direction: division is not a regular
	/// expression literal, and without the predicate the whole rest of the line becomes body.
	/// </summary>
	[Fact]
	public void ADelimiterContextPredicateRefusesAnOpeningWherePrecedingCodeEndsAValue()
	{
		const string source = "let d = a / b / c;";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, source, LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.StringBody);
	}

	[Fact]
	public void ADelimiterContextPredicateAdmitsAnOpeningWherePrecedingCodeCannotEndAValue()
	{
		const string source = "let p = /abc/ tail";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "abc", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
	}

	/// <summary>
	/// A character class is a suspended sub-range: the literal's closing delimiter does not apply
	/// inside it. Without the field the literal truncates at the slash in the class and every zone
	/// after it on the line is wrong.
	/// </summary>
	[Fact]
	public void ASuspendedSubRangeStopsTheClosingDelimiterFromApplyingInsideIt()
	{
		const string source = "let p = /ab[/]cd/ tail";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "ab[/]cd", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		Assert.False(mask.WasAbandonedMidFile);
	}

	/// <summary>
	/// The character class, checked at its closing: with the sub-range honoured the literal ends at the
	/// LAST slash, so the text between the two candidate closings is body rather than code. A
	/// scanner that truncated early would report that stretch as code and this assertion is what
	/// distinguishes the two readings.
	/// </summary>
	[Fact]
	public void ATruncatedRegularExpressionLiteralWouldShowUpAsCodeBetweenTheTwoCandidateClosings()
	{
		const string source = "let p = /ab[/]cd/ tail";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "]cd", LexicalZone.StringBody);
	}

	/// <summary>A regular expression literal containing the block comment opener opens no comment.</summary>
	[Fact]
	public void ARegularExpressionLiteralContainingTheBlockCommentOpenerOpensNoComment()
	{
		const string source = "let p = /a<#b/ tail";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "a<#b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Comment);
	}

	/// <summary>
	/// The cross-collection half of the longest-opening rule. A block comment opener and a
	/// here-document opener share a first character and live in different rule collections, which
	/// the profile cannot order against each other; both are recognized in one file here.
	/// </summary>
	[Fact]
	public void ACommentOpenerAndAQuoteOpenerSharingAFirstCharacterAreBothRecognized()
	{
		const string source = "x <# a #> y <<EOT\nbody\nEOT\nz";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, normalized, "<# a #>", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, normalized, "<<EOT", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, normalized, "body", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, normalized, "\nz", LexicalZone.Code);
	}
}
