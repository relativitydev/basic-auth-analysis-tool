using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE SCANNER-LEVEL CASES OF THE HERE-DOCUMENT: the two whole-line closings, the deferred
/// body start with its queue of pending openings, the empty terminator, and the refusals of an
/// opening that is not one. Every case runs the scanner under the test-local exemplar profile, which
/// is shaped like a shell and is built by no product code. The text of each case is invented.
/// </summary>
public sealed class HereDocumentScannerTests
{
	private const char _LINE_FEED = '\n';
	private const string _PENDING_OPENING = "<<A";
	private const string _PENDING_BODY = "x\nA\n";
	private const int _BODY_LENGTH_OF_ONE_PENDING_BODY = 2;
	private const int _OPENING_START_AFTER_THE_COMMAND_NAME = 4;
	private const int _SHORTEST_OPENING_LENGTH = 2;
	private const int _EXPECTED_QUEUE_BOUND = 5000;
	private const int _EXPECTED_UNBALANCED_NESTING_COUNT = 1;
	private const string _BARE_OPENING = "<<";
	private const string _EMPTY_LINE_BODY = "\n";

	private static readonly LexicalProfile _profile = new HereDocumentExemplarAnalyzer().LexicalProfile;

	[Fact]
	public void AClosingStandingAloneOnItsLineEndsTheBodyAndTheRestIsCode()
	{
		const string source = "cat <<EOF\nline one\nEOF\nafter=1\n";

		ZoneMask mask = Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf(_LINE_FEED)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "line one\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AClosingWithATrailingBlankOrALeadingBlankDoesNotCloseAWholeLineBody()
	{
		const string source = "cat <<EOF\nEOF \n EOF\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "EOF \n EOF\nbody\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 3);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ATerminatorIsNotClosedByALongerWordOrAWordThatEndsWithIt()
	{
		const string source = "cat <<END\nENDPOINT\nxEND\nEND\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "ENDPOINT\nxEND\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "END", LexicalZone.Delimiter, occurrence: 3);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheTabStrippingFormClosesOnAClosingIndentedByTabs()
	{
		const string source = "cat <<-EOF\n\tbody\n\t\tEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<-EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "\tbody\n\t\t", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheTabStrippingFormDoesNotCloseOnAClosingIndentedBySpaces()
	{
		const string source = "cat <<-EOF\n  EOF\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "  EOF\nbody\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 2);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ThePlainFormDoesNotCloseOnAClosingIndentedByATab()
	{
		const string source = "cat <<EOF\n\tEOF\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "\tEOF\nbody\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 2);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TwoOpeningsOnOneLineReadTheirBodiesInOrderAndTheRestOfTheLineIsCode()
	{
		const string source = "cat <<A <<B | wc -l\nbody a\nA\nbody b\nB\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<A", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "<<B", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, " | wc -l", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body a\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "A", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "body b\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "B", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ACommentOnTheOpeningLineDoesNotDrainBeforeItsLineFeed()
	{
		const string source = "cat <<EOF # a note\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "# a note", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AMultiLineSubstitutionInTheFirstBodyDoesNotDisturbTheQueueOfTheSecond()
	{
		const string source = "cat <<A <<B\nx $(echo\nfoo\nB\n) y\nA\nbody b\nB\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "x ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "$(", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "echo\nfoo\nB\n", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, ")", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, " y\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "A", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "body b\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "B", LexicalZone.Delimiter, occurrence: 2);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AMultiLineSubstitutionInAQuoteOnTheOpeningLineDoesNotDrain()
	{
		const string source = "cat <<EOF \"$(echo\nfoo)\" tail\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "echo\nfoo", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ABackslashNewlineAfterAnOpeningContinuesTheCommandAndTheBodiesStartAfterTheContinuedLine()
	{
		const string source = "cat <<EOF \\\nmore args\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "more args", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnEvenRunOfBackslashesBeforeTheLineFeedDoesNotContinueTheCommand()
	{
		const string source = "cat <<EOF \\\\\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheQueueBoundIsTheLineCeilingOverTheShortestOpening()
	{
		Assert.Equal(
			ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS / LexicalScanner.SHORTEST_DEFERRED_OPENING_LENGTH_IN_CHARACTERS,
			LexicalScanner.MAXIMUM_PENDING_BODIES);
		Assert.Equal(_SHORTEST_OPENING_LENGTH, LexicalScanner.SHORTEST_DEFERRED_OPENING_LENGTH_IN_CHARACTERS);
		Assert.Equal(_EXPECTED_QUEUE_BOUND, LexicalScanner.MAXIMUM_PENDING_BODIES);
		Assert.Equal(
			LexicalScanner.SHORTEST_DEFERRED_OPENING_LENGTH_IN_CHARACTERS,
			HereDocumentExemplarPatternExpressions.BareDelimiterOpening().Match(_BARE_OPENING).Length);
		Assert.True(
			HereDocumentExemplarPatternExpressions.PlainOpening().Match(_PENDING_OPENING).Length
			>= LexicalScanner.SHORTEST_DEFERRED_OPENING_LENGTH_IN_CHARACTERS);
		Assert.True(
			(LexicalScanner.MAXIMUM_PENDING_BODIES + 1) * LexicalScanner.SHORTEST_DEFERRED_OPENING_LENGTH_IN_CHARACTERS
			> ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);
	}

	[Fact]
	public void ALineFilledToTheCeilingWithOpeningsReadsEveryBodyInOrder()
	{
		int openingCount = ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS / _PENDING_OPENING.Length;
		string source = $"{BuildLineOfOpenings(openingCount)}\n{BuildBodies(openingCount)}after\n";

		ZoneMask mask = Scan(source);

		Assert.True(source.IndexOf(_LINE_FEED) <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(openingCount * _BODY_LENGTH_OF_ONE_PENDING_BODY, CountPositionsIn(mask, LexicalZone.StringBody));
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
	}

	[Fact]
	public void ALineFilledToTheBoundWithTwoCharacterOpeningsReadsEveryBodyInOrder()
	{
		int openingCount = LexicalScanner.MAXIMUM_PENDING_BODIES;
		string source = $"{BuildBareOpenings(openingCount)}\n{BuildEmptyLines(openingCount)}after\n";

		ZoneMask mask = new LexicalScanner(BuildBareOpeningProfile()).Scan(source);

		Assert.True(source.IndexOf(_LINE_FEED) <= ReadingCeilings.MAXIMUM_LINE_LENGTH_IN_CHARACTERS);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(openingCount * _BARE_OPENING.Length, CountPositionsIn(mask, LexicalZone.Delimiter));
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
	}

	[Fact]
	public void ATwoCharacterOpeningBeyondTheQueueBoundIsRefusedAndStaysCode()
	{
		int openingCount = LexicalScanner.MAXIMUM_PENDING_BODIES;
		string source = $"{BuildBareOpenings(openingCount + 1)}\n{BuildEmptyLines(openingCount)}after\n";
		int refusedOpening = openingCount * _BARE_OPENING.Length;

		ZoneMask mask = new LexicalScanner(BuildBareOpeningProfile()).Scan(source);

		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(refusedOpening));
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(refusedOpening + 1));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(refusedOpening - 1));
		Assert.Equal(openingCount * _BARE_OPENING.Length, CountPositionsIn(mask, LexicalZone.Delimiter));
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
	}

	[Fact]
	public void OpeningsQueuedAcrossTwoPhysicalLinesByAMultiLineHoleDrainInOrder()
	{
		const string source = "cat <<A \"$(x\n)\" <<B\nbody a\nA\nbody b\nB\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "x\n", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body a\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "A", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "body b\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "B", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<''\nbody line\n\nafter=1\n")]
	[InlineData("cat <<\"\"\nbody line\n\nafter=1\n")]
	public void AnEmptyTerminatorClosesAtTheFirstEmptyLineAndTheNextLineIsCode(string source)
	{
		ZoneMask mask = Scan(source);
		int emptyLine = source.IndexOf("\n\n", StringComparison.Ordinal) + 1;

		ZoneAssertions.SpansZone(mask, source, "body line\n", LexicalZone.StringBody);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(emptyLine));
		ZoneAssertions.SpansZone(mask, source, "after=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnEmptyTerminatorWithNoBodyClosesAtTheFirstLine()
	{
		const string source = "cat <<''\n\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<''", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnEmptyTerminatorOfTheTabStrippingFormClosesAtTheFirstLineOfTabsOnly()
	{
		const string source = "cat <<-''\nbody\n\t\t\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n\t\t", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>The length-zero refusal stays for every other anchoring: here a start-of-line closing, so the opening is not one.</summary>
	[Fact]
	public void AnEmptyTerminatorIsStillRefusedUnderAnyOtherClosingAnchoring()
	{
		const string source = "cat <<''\nbody\nafter\n";
		LexicalProfile profile = HereDocumentExemplarAnalyzer.BuildProfileWith(
		[
			HereDocumentExemplarAnalyzer.BuildHereDocumentRule(
				"<<",
				HereDocumentExemplarPatternExpressions.PlainOpening(),
				PositionalAnchoring.StartOfLine,
				HereDocumentBodyStart.AfterTheOpening)
		]);

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<<", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "body\nafter", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<EOF\n")]
	[InlineData("cat <<EOF")]
	public void AnOpeningWithNoBodyBeforeTheEndOfTheFileIsAnUnterminatedLiteralAtTheOpening(string source)
	{
		ZoneMask mask = Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(_OPENING_START_AFTER_THE_COMMAND_NAME, mask.ValidThroughPosition);
	}

	[Fact]
	public void ASecondOpeningWhoseBodyNeverStartsIsUnterminatedAtItsOwnOpening()
	{
		const string source = "cat <<A <<B\nbody\nA\n";

		ZoneMask mask = Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(source.IndexOf("<<B", StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	[Fact]
	public void AnAbandonmentDuringTheDrainStopsItAtTheFirstUnterminatedBody()
	{
		const string source = "cat <<A <<B\nbody\n";

		ZoneMask mask = Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(source.IndexOf("<<A", StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	[Fact]
	public void AnOpeningInsideAHoleIsReadAndItsBodyIsReadAfterTheHolesOwnLine()
	{
		const string source = "cat <<A\n$(cat <<X\ninner\nX\n)\nA\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<X", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "inner\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "X", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, ")", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "A", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnOpeningInsideAHoleThatClosesOnTheOpeningLineHandsItsBodyToTheOutsideQueueAheadOfTheOutsideOpening()
	{
		const string source = "cat <<A \"$(cat <<X)\"\ninner\nX\nouter\nA\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<X", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "inner\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "X", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "outer\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "A", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnUnquotedTerminatorBodyInterpolatesAndAQuotedOneDoesNot()
	{
		const string unquoted = "cat <<EOF\nhello $(echo hi)\nEOF\nafter\n";
		const string quoted = "cat <<'EOF'\n$(echo hi)\nEOF\nafter\n";

		ZoneMask unquotedMask = Scan(unquoted);
		ZoneMask quotedMask = Scan(quoted);

		ZoneAssertions.SpansZone(unquotedMask, unquoted, "echo hi", LexicalZone.Code);
		ZoneAssertions.SpansZone(quotedMask, quoted, "$(echo hi)\n", LexicalZone.StringBody);
		Assert.True(unquotedMask.IsCompletelyValid);
		Assert.True(quotedMask.IsCompletelyValid);
	}

	/// <summary>
	/// A hole that counted a nested opener and never closed is read again from its start. The opening
	/// waiting for its body is the state that read must leave intact, and the body is still read after
	/// the line that held the hole.
	/// </summary>
	[Fact]
	public void ANestingRecoveryOnTheOpeningLineLeavesThePendingOpeningIntact()
	{
		const string source = "cat <<EOF \"$( ( x )\"\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		Assert.Equal(_EXPECTED_UNBALANCED_NESTING_COUNT, mask.UnbalancedNestingCount);
		Assert.Equal(LexicalAnomalyReason.UnbalancedInterpolationNesting, mask.AnomalyReason);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>
	/// The opening refusals at scanner level. Each first line holds a construct the opening refuses, by the expression's
	/// own non-match or by the predicate, and a here-document that follows it is still opened and closed.
	/// </summary>
	[Theory]
	[InlineData("cat <<<word")]
	[InlineData("echo $(( 1 <<x ))")]
	[InlineData("echo $(( 1 << x ))")]
	[InlineData("(( n <<x ))")]
	[InlineData("(( n << 2 ))")]
	[InlineData("let v=1<<n")]
	[InlineData("  let v=1<<n")]
	public void AnOpeningTheExpressionOrThePredicateRefusesOpensNoHereDocument(string refusedLine)
	{
		string source = $"{refusedLine}\ncat <<EOF\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		for (int position = 0; position < refusedLine.Length; position++)
		{
			Assert.Equal(LexicalZone.Code, mask.ZoneAt(position));
		}

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "after", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnArithmeticLineThatClosedItsParenthesesDoesNotRefuseALaterOpening()
	{
		const string source = "echo $((1+2)) <<EOF\nbody\nEOF\nafter\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	private static ZoneMask Scan(string source)
	{
		return new LexicalScanner(_profile).Scan(source);
	}

	private static string BuildLineOfOpenings(int openingCount)
	{
		return string.Concat(Enumerable.Repeat(_PENDING_OPENING, openingCount));
	}

	private static string BuildBareOpenings(int openingCount)
	{
		return string.Concat(Enumerable.Repeat(_BARE_OPENING, openingCount));
	}

	private static string BuildEmptyLines(int lineCount)
	{
		return string.Concat(Enumerable.Repeat(_EMPTY_LINE_BODY, lineCount));
	}

	/// <summary>A test-local profile whose only here-document opening is the bare delimiter with an empty terminator and no context predicate, so the openings can stand back to back.</summary>
	private static LexicalProfile BuildBareOpeningProfile()
	{
		QuoteRule rule = HereDocumentExemplarAnalyzer.BuildHereDocumentRule(
			_BARE_OPENING,
			HereDocumentExemplarPatternExpressions.BareDelimiterOpening(),
			PositionalAnchoring.WholeLine,
			HereDocumentBodyStart.AfterTheOpeningLine) with
		{
			OpeningContextPredicate = null
		};

		return HereDocumentExemplarAnalyzer.BuildProfileWith([rule]);
	}

	private static string BuildBodies(int bodyCount)
	{
		StringBuilder bodies = new(bodyCount * _PENDING_BODY.Length);

		for (int index = 0; index < bodyCount; index++)
		{
			bodies.Append(_PENDING_BODY);
		}

		return bodies.ToString();
	}

	private static int CountPositionsIn(ZoneMask mask, LexicalZone zone)
	{
		int count = 0;

		for (int position = 0; position < mask.Length; position++)
		{
			count += mask.ZoneAt(position) == zone ? 1 : 0;
		}

		return count;
	}
}
