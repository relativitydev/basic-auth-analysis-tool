using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE UNTERMINATED-CONSTRUCT POLICY, THE RAW NEWLINE, AND THE ONE ROUTE EITHER OF THEM
/// TAKES TO A REPORT.
///
/// The two are deliberately not the same shape and this file is where that distinction is pinned.
/// An unterminated construct leaves the mask CORRECT UP TO the opening position and undefined
/// after it, so the file is partially covered and is counted under the ninth skipped-item category. A raw
/// newline inside a literal that does not admit one is also an anomaly in the profile vocabulary's words, but the
/// scanner recovers at a line boundary it knows the position of, the file is covered end to end,
/// and counting it as a skipped item would put a fully analyzed file into the number a reader
/// uses to size the holes in the coverage.
/// </summary>
public sealed class LexicalAnomalyTests
{
	/// <summary>
	/// The mask below the boundary is not thrown away. the scanner policy is explicit that the findings
	/// established up to the opening are emitted, which is only possible if the zones below it
	/// survive - so this asserts the surviving zones as well as the boundary.
	/// </summary>
	[Fact]
	public void AnUnterminatedLiteralBoundsTheMaskAtItsOwnOpeningAndKeepsWhatCameBefore()
	{
		const string source = "let s = \"abc";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);
		int openingQuote = source.IndexOf('"');

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(openingQuote, mask.ValidThroughPosition);
		ZoneAssertions.SpansZone(mask, source, "let s = ", LexicalZone.Code);
		Assert.True(mask.IsTrustworthyAt(openingQuote - 1));
		Assert.False(mask.IsTrustworthyAt(openingQuote));
	}

	[Fact]
	public void AnUnterminatedBlockCommentBoundsTheMaskAtItsOwnOpening()
	{
		const string source = "let a = 1 <# never closed";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);
		int opening = source.IndexOf("<#", StringComparison.Ordinal);

		Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		Assert.Equal(opening, mask.ValidThroughPosition);
		ZoneAssertions.SpansZone(mask, source, "let a = 1 ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "<# never closed", LexicalZone.Comment);
	}

	/// <summary>A captured terminator that never appears is the same policy on a different rule.</summary>
	[Fact]
	public void AHereDocumentWhoseTerminatorNeverAppearsIsAnUnterminatedLiteral()
	{
		const string source = "let d = <<EOT\nbody that never ends\n";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(source.IndexOf("<<", StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	/// <summary>
	/// THE UNDEFINED REGION IS FILLED IN THE DEMOTING DIRECTION. Every position still carries
	/// exactly one zone above the boundary, and the value written there is a demoting one, so a
	/// consumer that forgets to read the boundary errs towards losing a finding rather than
	/// towards promoting one it never had grounds for.
	/// </summary>
	[Fact]
	public void TheRegionAboveTheBoundaryStillCarriesOneZoneAndItIsTheDemotingOne()
	{
		const string source = "let s = \"abc";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
		Assert.False(LexicalZones.CountsAsCode(mask.ZoneAt(mask.Length - 1)));
	}

	/// <summary>
	/// The profile's three-valued newline policy, in its <c>Forbidden</c> value. The literal ends at
	/// the newline, the anomaly is counted, and the file is still covered end to end.
	/// </summary>
	[Fact]
	public void ARawNewlineEndsALiteralThatForbidsOneAndTheScanStillCoversTheWholeFile()
	{
		const string source = "let q = 'unclosed\nlet b = 2";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.NewlineInsideALiteral, mask.AnomalyReason);
		Assert.False(mask.WasAbandonedMidFile);
		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		ZoneAssertions.SpansZone(mask, normalized, "let b = 2", LexicalZone.Code);
	}

	/// <summary>
	/// The profile's <c>EscapedContinues</c> value, which is the one a boolean cannot express. Read
	/// wrongly, the literal's CLOSING quote becomes an OPENING one and the whole rest
	/// of the line turns into string body - so the assertion that carries this case is the one
	/// about the trailing call.
	/// </summary>
	[Fact]
	public void AnEscapedNewlineContinuesALiteralWhoseRuleSaysItDoes()
	{
		const string source = "var aa = \"xx\\\nyy\"; bb();";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		Assert.Equal(0, mask.NewlineTerminatedLiteralCount);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		ZoneAssertions.SpansZone(mask, normalized, "xx\\\nyy", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, normalized, "; bb();", LexicalZone.Code);
	}

	/// <summary>
	/// And the contrast that makes the three values three rather than two: the same escape before
	/// the same newline, under a rule that forbids continuation, does NOT continue the literal.
	/// </summary>
	[Fact]
	public void AnEscapedNewlineDoesNotContinueALiteralWhoseRuleForbidsIt()
	{
		const string source = "let p = /ab\\\ncd/ tail";

		string normalized = ReferenceProfileScanner.Normalize(source);
		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		Assert.False(mask.WasAbandonedMidFile);
		ZoneAssertions.SpansZone(mask, normalized, "cd/ tail", LexicalZone.Code);
	}

	/// <summary>
	/// THE SCANNER NEVER FILTERS. A file that is nothing but a comment is masked end to end
	/// rather than declined, because demotion lowers what is found in a comment and cannot demote
	/// what was never scanned.
	/// </summary>
	[Fact]
	public void AFileThatIsEntirelyACommentIsStillMaskedRatherThanDeclined()
	{
		const string source = "-- everything on this line is a comment";

		ZoneMask mask = ReferenceProfileScanner.Scan(source);

		ZoneAssertions.SpansZone(mask, source, source, LexicalZone.Comment);
		Assert.False(mask.WasAbandonedMidFile);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
	}

	/// <summary>
	/// The emptiest file there is, which is the shape of a repository with nothing to find, at the scanner's level:
	/// nothing to find is not an error and produces no anomaly.
	/// </summary>
	[Fact]
	public void AnEmptyFileProducesAnEmptyValidMask()
	{
		ZoneMask mask = ReferenceProfileScanner.Scan(string.Empty);

		Assert.Equal(0, mask.Length);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
	}

	/// <summary>
	/// THE SKIP GOES THROUGH THE SAME DOOR AS EVERY OTHER SKIP. The skipped-item catalogue declares the ninth category
	/// and leaves it for the scanner to raise, and raising it anywhere but the emission point would be
	/// a second reporting path for a hole in the coverage.
	/// </summary>
	[Fact]
	public void AnAbandonedScanIsReportedAsTheNinthSkippedItemCategory()
	{
		SkippedItemLedger ledger = new();
		FileLexicalScanner scanner = new(new EmissionPoint([ledger]));

		ZoneMask mask = scanner.Scan(ReferenceProfileScanner.Profile(), "let s = \"abc", default);

		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(1, ledger.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
		Assert.Equal(1, ledger.SkippedFiles);
	}

	/// <summary>
	/// And a recovered literal is not reported, because the file was covered. A category whose
	/// own words are "abandoned mid-file, leaving it partially covered" must not count a file
	/// that was neither.
	/// </summary>
	[Fact]
	public void ARecoveredLiteralIsCountedOnTheMaskAndNotReportedAsASkippedItem()
	{
		SkippedItemLedger ledger = new();
		FileLexicalScanner scanner = new(new EmissionPoint([ledger]));

		ZoneMask mask = scanner.Scan(ReferenceProfileScanner.Profile(), "let q = 'unclosed\nlet b = 2", default);

		Assert.Equal(1, mask.NewlineTerminatedLiteralCount);
		Assert.Equal(0, ledger.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
		Assert.False(ledger.RecordedAnything);
	}

	[Fact]
	public void ACleanScanReportsNothingAtAll()
	{
		SkippedItemLedger ledger = new();
		FileLexicalScanner scanner = new(new EmissionPoint([ledger]));

		ZoneMask mask = scanner.Scan(ReferenceProfileScanner.Profile(), "let a = 1 -- fine\n", default);

		Assert.True(mask.IsCompletelyValid);
		Assert.False(ledger.RecordedAnything);
	}

	/// <summary>
	/// The recursion bound, reached through the wired scanner rather than the bare one, so that
	/// the whole path from a refused descent to a reported category is covered once.
	/// </summary>
	[Fact]
	public void ADepthAnomalyReachesTheSameCategoryAsAnUnterminatedConstruct()
	{
		SkippedItemLedger ledger = new();
		FileLexicalScanner scanner = new(new EmissionPoint([ledger]));
		string nesting = string.Concat(Enumerable.Repeat("\"${", 9));
		string source = $"{nesting}x";

		ZoneMask mask = scanner.Scan(ReferenceProfileScanner.Profile(), source, default);

		Assert.Equal(LexicalAnomalyReason.InterpolationDepthExceeded, mask.AnomalyReason);
		Assert.Equal(1, ledger.TallyOf(SkippedItemCategory.LexicalAnomaly).Count);
	}
}
