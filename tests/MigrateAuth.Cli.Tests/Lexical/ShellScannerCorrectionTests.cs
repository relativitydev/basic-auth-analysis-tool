using System.Text;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Tests.Fixtures;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// FOUR LEXICAL-SCANNER BEHAVIOURS OF THE SHELL, one test group each: a here-document inside <c>"$( … )"</c> (per-region pending
/// queues), a <c>case</c> arm's <c>)</c> inside <c>"$( … )"</c> (the arm block), a partially quoted terminator (the effective
/// word) and multi-line arithmetic (the arithmetic context). They run under the test-local
/// <see cref="ShellShapedProfile"/>, which declares the features; the cases marked as a contrast run under the
/// here-document exemplar, which does not, to show what a profile without the declarations still gets.
/// The forms are common shell idioms (a payload built with <c>$(cat &lt;&lt;TAG … TAG)</c>, a
/// <c>case "$opt" in</c> option loop, <c>$(( n - 1 ))</c> expressions) and their neighbours.
/// </summary>
public sealed class ShellScannerCorrectionTests
{
	internal const string FOLLOWING_STATEMENT = "after=1";
	private const string _EXPANSION_IN_A_BODY = "$(echo hi)\n";
	private const int _EXPECTED_ONE_RECOVERY = 1;
	private const int _LINE_OF_OPENINGS_PAST_THE_BOUND = LexicalScanner.MAXIMUM_PENDING_BODIES + 1;
	private const string _OPENING_WITH_BLANK = "<<A ";
	private const string _BODY_LINE_CLOSING_ITSELF = "A\n";
	internal const int SHORT_WORD_PARTS = 1500;
	internal const int LENGTH_FACTOR = 8;
	internal const int OPENING_START = 4;
	internal const int PATHOLOGICAL_UNITS = 10;
	private const int _CHARACTERS_PER_UNIT = 10000;
	private const int _LICENSING_WORD_LENGTH = 5;
	private const int _UNCLOSED_LINE_LENGTH = 4;
	private const string _EXTENT_ENDING_IN_THE_ESCAPE = @"[a-z]+\\?";
	private const int _AGREEMENT_SEED = 173;
	private const int _UNWRAPPED_ONE_IN = 4;
	private const string _WRAPPER_OPENING = "x=$(( ";
	private const string _WRAPPER_CLOSING = " ))";
	private const int _MINIMUM_BRACKET_ARITHMETIC_SAMPLES = 60;
	private const int _MINIMUM_KEYWORD_ARITHMETIC_SAMPLES = 200;
	private const int _MINIMUM_SUBSTITUTION_ARITHMETIC_SAMPLES = 300;
	private const char _OPENING_BRACKET = '(';
	private const char _CLOSING_BRACKET = ')';
	private const int _OPENING_RUN_LENGTH = 2;
	private const int _CLOSING_RUN_LENGTH = 2;
	private const string _TAB_STRIPPING_OPENING = "<<-";
	private const string _LEADING_TAB = "\t";
	private const int _NO_END = -1;
	private const int _AGREEMENT_SAMPLES = 1000;
	private const int _KEYWORD_AGREEMENT_SAMPLES = 4000;
	private const int _SUBSTITUTION_AGREEMENT_SAMPLES = 6000;
	private const int _AGREEMENT_MINIMUM_LENGTH = 6;
	private const int _AGREEMENT_MAXIMUM_LENGTH = 20;
	private const string _UNBALANCED_HOLE_LINE = "a=\"id $(echo (b ) end\"\n";

	private static readonly string[] _bracketTokens = ["(", "(", "(", "(", ")", ")", ")", " ", "x", "\n"];
	private static readonly string[] _substitutionTokens = [.. _bracketTokens, "$(", "$(", "$(", "case ", "esac ", "a) ", "b) ", ";; ", "$(case ", "$(esac) ", ";; esac) ", "$(( ", "(x)", "$(x)", "$(case a in a) x;; esac)", "(( x ))"];
	private static readonly string[] _keywordTokens = [.. _bracketTokens, "case ", "esac ", "a) ", ";; ", "then ", "time -p "];

	// ---- A here-document inside "$( … )" ----

	[Fact]
	public void O521AHereDocumentInsideAQuotedSubstitutionIsReadAndItsBodyIsStringBody()
	{
		const string source = "x=\"$(cat <<EOF\ninner\nEOF\n)\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "inner\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, ")", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521ThePayloadFormOfTheCensusIsReadInsideAndOutsideQuotes()
	{
		const string unquoted = "payload=$(cat <<JSON\n{\"request\":{\"condition\":\"$cond\"},\"start\":1}\nJSON\n)\nwget --header='X: -' \"$url\"\n";
		const string quoted = "payload=\"$(cat <<JSON\n{\"request\":{\"condition\":\"$cond\"},\"start\":1}\nJSON\n)\"\nwget --header='X: -' \"$url\"\n";

		ZoneMask unquotedMask = ShellShapedProfile.Scan(unquoted);
		ZoneMask quotedMask = ShellShapedProfile.Scan(quoted);

		ZoneAssertions.SpansZone(unquotedMask, unquoted, "{\"request\":{\"condition\":\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(quotedMask, quoted, "{\"request\":{\"condition\":\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(quotedMask, quoted, "wget --header=", LexicalZone.Code);
		ZoneAssertions.SpansZone(quotedMask, quoted, "X: -", LexicalZone.StringBody);
		Assert.True(unquotedMask.IsCompletelyValid);
		Assert.True(quotedMask.IsCompletelyValid);
	}

	[Fact]
	public void O521TheHoleContinuesAfterItsHereDocumentAndALineHoldingTheClosingParenthesisIsBody()
	{
		const string source = "x=\"$(cat <<EOF\n)\nEOF\necho done)\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, ")\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "echo done", LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf("done)", StringComparison.Ordinal) + "done".Length));
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521SeveralOpeningsOnTheHoleLineReadTheirBodiesInOrder()
	{
		const string source = "x=\"$(cat <<A <<B\nfirst\nA\nsecond\nB\n)\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "first\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "second\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "B", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521ABackslashNewlineAfterTheOpeningMovesTheBodyToTheLineThatEndsTheCommand()
	{
		const string source = "x=\"$(cat <<EOF \\\n&& echo x\nbody\nEOF\n)\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "&& echo x", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521NestedSubstitutionsEachDrainTheirOwnQueue()
	{
		const string source = "x=\"$(echo \"$(cat <<EOF\ninner\nEOF\n)\")\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "inner\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521TheFilesOwnOpeningAndTheHolesOpeningDoNotTakeEachOthersBodies()
	{
		const string source = "cat <<A \"$(cat <<B\nbodyB\nB\n)\"\nbodyA\nA\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "bodyB\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "bodyA\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "A", LexicalZone.Delimiter, occurrence: 2);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("x=\"$(cat <<EOF)\"\nbody\nEOF\nafter=1\n")]
	[InlineData("x=\"$(cat <<EOF)\" # tail\nbody\nEOF\nafter=1\n")]
	[InlineData("echo \"a $(cat <<EOF) b\"\nbody\nEOF\nafter=1\n")]
	[InlineData("x=\"$(echo \"$(cat <<EOF)\")\"\nbody\nEOF\nafter=1\n")]
	[InlineData("x=\"$(echo \"$(cat <<EOF)\"\nbody\nEOF\n)\"\nafter=1\n")]
	public void O521AnOpeningWhoseHoleClosesOnItsLineReadsItsBodyFromTheLinesAfterTheCommandAsBashDoes(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<A \"$(cat <<X)\"\nx1\nX\na1\nA\nafter=1\n")]
	[InlineData("cat \"$(cat <<X)\" <<A\nx1\nX\na1\nA\nafter=1\n")]
	[InlineData("cat <<A <<B \"$(cat <<X)\"\nx1\nX\na1\nA\nb1\nB\nafter=1\n")]
	public void O521ABodyHandedOverByAHoleIsReadBeforeTheBodiesOfTheOpeningsPendingInTheHoldingRegion(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "x1\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "X", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "a1\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "A", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat \"$(cat <<X)\" \"$(cat <<Y)\" <<A\nx1\nX\ny1\nY\na1\nA\nafter=1\n")]
	[InlineData("cat \"$(cat <<X; cat <<Y)\" <<A\nx1\nX\ny1\nY\na1\nA\nafter=1\n")]
	public void O521BodiesHandedOverByHolesAreReadInTheOrderTheHolesClosedAndTheirOpeningsAppeared(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "x1\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "y1\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "a1\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "Y", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "A", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521TheQueueOfTheHoldingRegionBoundsTheBodiesHandedOverByHoles()
	{
		const string segment = "\"$(cat <<A)\"";
		string head = string.Join("\\\n", Enumerable.Repeat(segment, LexicalScanner.MAXIMUM_PENDING_BODIES + 1));
		string bodies = string.Concat(Enumerable.Repeat(_BODY_LINE_CLOSING_ITSELF, LexicalScanner.MAXIMUM_PENDING_BODIES));
		string source = $"cat {head}\n{bodies}after=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521AnUnterminatedHereDocumentInsideAHoleAbandonsAtItsOpening()
	{
		const string source = "x=\"$(cat <<EOF\nnever closed\n)\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(source.IndexOf("<<EOF", StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	[Fact]
	public void O521AHoleReadAfterANormalizedCarriageReturnLineFeedReadsTheSameBody()
	{
		string source = TextNormalization.Normalize("x=\"$(cat <<EOF\r\ninner\r\nEOF\r\n)\"\r\nafter=1\r\n");

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "inner\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521TheQueueOfARegionIsBoundedAndTheOpeningBeyondItStaysCode()
	{
		string openings = string.Concat(Enumerable.Repeat(_OPENING_WITH_BLANK, _LINE_OF_OPENINGS_PAST_THE_BOUND));
		string bodies = string.Concat(Enumerable.Repeat(_BODY_LINE_CLOSING_ITSELF, LexicalScanner.MAXIMUM_PENDING_BODIES));
		string source = $"x=\"$(cat {openings}\n{bodies})\"\nafter=1\n";
		int refusedOpening = source.IndexOf(_OPENING_WITH_BLANK, StringComparison.Ordinal) + (LexicalScanner.MAXIMUM_PENDING_BODIES * _OPENING_WITH_BLANK.Length);

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(refusedOpening));
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(refusedOpening + 1));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(refusedOpening - _OPENING_WITH_BLANK.Length));
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521ARecoveryThatRereadsTheHoleStartsFromAnEmptyQueue()
	{
		const string source = "x=\"$( ( cat <<EOF\nbody\nEOF\n)\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(_EXPECTED_ONE_RECOVERY, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	// ---- A case arm's ")" inside "$( … )" ----

	[Fact]
	public void O522ACaseArmInsideAQuotedSubstitutionDoesNotCloseTheHole()
	{
		const string source = "echo \"$(case a in a) echo hit;; esac)\" # tail comment\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheHoleInteriorIsCode(mask, source);
		ZoneAssertions.SpansZone(mask, source, "case a in a) echo hit;; esac", LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf("esac)", StringComparison.Ordinal) + "esac".Length));
		ZoneAssertions.SpansZone(mask, source, "# tail comment", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("x=\"$(\ncase $v in\n  a) echo 1 ;;\n  *) echo 2 ;;\nesac\n)\" # c\n")]
	[InlineData("x=\"$(case a in (a) echo ;; esac)\" # c\n")]
	[InlineData("x=\"$(case a in a) (echo x) ;; esac)\" # c\n")]
	[InlineData("x=\"$(case a in a) echo $(date) ;; b|c) echo no ;; esac)\" # c\n")]
	[InlineData("x=\"$(if y; then case a in a) echo ;; esac; fi)\" # c\n")]
	[InlineData("x=\"$(echo q | case $v in a) echo ;; esac)\" # c\n")]
	[InlineData("x=\"$(case a in a) case b in b) echo ;; esac ;; esac)\" # c\n")]
	public void O522ArmsInEveryNeighbouringShapeKeepTheHoleOpenUntilItsOwnClosing(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheHoleInteriorIsCode(mask, source);
		ZoneAssertions.SpansZone(mask, source, "# c", LexicalZone.Comment);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O522ACaseWordAmongTheArgumentsOpensNoBlock()
	{
		const string source = "echo \"$(echo case)\" # c\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "# c", LexicalZone.Comment);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O522ABlockThatNeverClosesFallsBackToTheFirstClosingWithOneRecordedRecovery()
	{
		const string source = "x=\"$(case a in a) echo)\" y\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(_EXPECTED_ONE_RECOVERY, mask.UnbalancedNestingCount);
		Assert.Equal(LexicalAnomalyReason.UnbalancedInterpolationNesting, mask.AnomalyReason);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void O522ACaseCommandAtTheTopLevelReadsItsArmsAsCodeAsBefore()
	{
		const string source = "case $opt in\n\tp) package=$OPTARG ;;\n\t*) usage ;;\nesac\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.StringBody);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("p)", StringComparison.Ordinal) + 1));
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O522AProfileWithoutTheArmBlockStillClosesTheHoleAtTheArm()
	{
		const string source = "echo \"$(case a in a) echo hit;; esac)\" tail\nafter=1\n";

		ZoneMask mask = new LexicalScanner(new HereDocumentExemplarAnalyzer().LexicalProfile).Scan(source);

		ZoneAssertions.SpansZone(mask, source, "case a in a", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " echo hit;; esac)", LexicalZone.StringBody);
	}

	[Theory]
	[InlineData("x=\"$(cat <<-EOF)\"\n\tbody\n\tEOF\nafter=1\n")]
	[InlineData("x=\"$(cat <<EO\\\nF)\"\nbody\nEOF\nafter=1\n")]
	public void O521ATabStrippingOrContinuedOpeningInAHoleThatClosesOnItsLineReadsItsBodyAfterTheCommand(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O521ABodyHandedOverAheadOfAnEarlierOpeningLeavesTheMaskSoundUpToTheEarlierOpeningWhenTheFileEnds()
	{
		const string source = "before=1\ncat <<A \"$(cat <<X)\"\nx1\nX\na1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(source.IndexOf("<<A", StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	// ---- A partially quoted terminator ----

	[Fact]
	public void O523APartiallyQuotedTerminatorIsUnquotedToItsEffectiveWordAndIsQuoted()
	{
		const string source = "cat <<E\"O\"F\n$(echo hi)\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<E\"O\"F", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, _EXPANSION_IN_A_BODY, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<E\"O\"F\n")]
	[InlineData("cat <<'E'OF\n")]
	[InlineData("cat <<E\\OF\n")]
	[InlineData("cat <<\\EOF\n")]
	[InlineData("cat <<\"EO\"F\n")]
	[InlineData("cat <<EO''F\n")]
	[InlineData("cat <<'E'\"O\"\\F\n")]
	[InlineData("cat <<-E\"O\"F\n")]
	public void O523EveryWayOfQuotingPartOfTheWordMakesTheBodyQuotedUnderTheEffectiveTerminator(string openingLine)
	{
		string source = $"{openingLine}$(echo hi)\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _EXPANSION_IN_A_BODY, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523ALineHoldingTheWordAsWrittenDoesNotCloseTheBody()
	{
		const string source = "cat <<E\"O\"F\nE\"O\"F\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(source.IndexOf("\nE\"O\"F\n", StringComparison.Ordinal) + 1));
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Theory]
	[InlineData("cat <<EOF-1\nbody\nEOF-1\nafter=1\n")]
	[InlineData("cat <<END.TXT\nbody\nEND.TXT\nafter=1\n")]
	[InlineData("cat <<_X_\nbody\n_X_\nafter=1\n")]
	[InlineData("cat << EOF\nbody\nEOF\nafter=1\n")]
	[InlineData("cat <<EOF>out\nbody\nEOF\nafter=1\n")]
	[InlineData("cat <<EOF;echo x\nbody\nEOF\nafter=1\n")]
	[InlineData("cat <<-EOF\n\tbody\n\tEOF\nafter=1\n")]
	[InlineData("cat <<$v\nbody\n$v\nafter=1\n")]
	public void O523TheWordMayHoldDashDotAndUnderscoreAndEndsAtABlankOrAMetacharacter(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523AnUnquotedWordKeepsAnInterpolatingBody()
	{
		const string source = "cat <<EOF\n$(echo hi)\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "echo hi", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523APartiallyQuotedTerminatorNeverClosedIsUnterminatedAndBounded()
	{
		const string source = "cat <<E\"O\"F\nnever closed\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(source.IndexOf("<<E", StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	[Theory]
	[InlineData("cat <<E\"O\"F\n")]
	[InlineData("cat <<'E'OF x\n")]
	[InlineData("cat <<EOF>out\n")]
	[InlineData("cat <<EO\\\nF\n")]
	[InlineData("cat <<-'EOF'\n")]
	[InlineData("cat <<''\n")]
	[InlineData("cat <<E\"O\n")]
	[InlineData("cat <<$'EOF'\n")]
	[InlineData("cat <<<word\n")]
	[InlineData("x=$(( 1<<3 ))\n")]
	[InlineData("cat << \"E O\" F\n")]
	[InlineData("cat <<\"E\\\\F\"\n")]
	[InlineData("cat <<\"E\\\"F\"\n")]
	[InlineData("cat <<\"E\\\"\n")]
	[InlineData("cat <<$\"E\\$F\"x\n")]
	[InlineData("cat <<$'E\\x4fF'\n")]
	[InlineData("cat <<$'E\\'F'x\n")]
	[InlineData("cat <<A\"E\\\\\"B'C'\\D\n")]
	[InlineData("cat <<E\\\nF\n")]
	[InlineData("cat <<-E\\\nF\n")]
	[InlineData("cat <<\\\n E\n")]
	[InlineData("cat <<'E'\\\n'F'\n")]
	[InlineData("cat <<\"E\\\nF\"\n")]
	[InlineData("cat <<E\\\n")]
	[InlineData("cat <<E\\\n\nbody\n")]
	[InlineData("cat <<E\\\nF\\")]
	[InlineData("cat <<\\\n")]
	[InlineData("cat <<E\\\n\\\nF\n")]
	[InlineData("cat <<$\"E\\\nF\"x\n")]
	public void O523TheGeneratedAndTheInterpretedFormOfEachOpeningExpressionAgree(string text)
	{
		Regex[] generatedForms = [ShellShapedProfile.PlainOpening(), ShellShapedProfile.TabStrippingOpening()];

		foreach (Regex generated in generatedForms)
		{
			Regex interpreted = new(generated.ToString(), generated.Options, Regex.InfiniteMatchTimeout);

			for (int start = 0; start < text.Length; start++)
			{
				Assert.Equal(Summarize(interpreted, text, start), Summarize(generated, text, start));
			}
		}
	}

	[Fact]
	public void O523TheFailingWordIsNotAMatchAtAnyLength()
	{
		Assert.All(
			new[] { SHORT_WORD_PARTS, SHORT_WORD_PARTS * LENGTH_FACTOR },
			parts => Assert.False(ShellShapedProfile.PlainOpening().Match(BuildAWordThatNeverEnds(parts), OPENING_START).Success));
	}

	internal static string BuildAWordThatNeverEnds(int parts)
	{
		return $"cat <<{string.Concat(Enumerable.Repeat("a\"b\"\\\n", parts))}\\";
	}

	// ---- A terminator word continued across a backslash-newline ----

	public static TheoryData<string, string> ContinuedOpenings => new()
	{
		{ "<<E\\\nF", "EF" },
		{ "<<-E\\\nF", "EF" },
		{ "<<E\\\nF\\\nG\\\nH", "EFGH" },
		{ "<<\\\nE", "E" },
		{ "<<\\\n E", "E" },
		{ "<<'E'\\\n'F'", "EF" },
		{ "<<-'E'\\\n'F'", "EF" },
		{ "<<\"E\"\\\nF", "EF" },
		{ "<<\"E\\\nF\"", "EF" },
		{ "<<E\"O\"\\\nF", "EOF" },
		{ "<<-E\"O\"\\\nF", "EOF" },
		{ "<<E\\\n\\\nF", "EF" },
		{ "<<-\\\nE", "E" },
		{ "<<-\\\n E", "E" },
		{ "<<-E\\\n\\\nF", "EF" },
		{ "<<-E\\\nF\\\nG\\\nH", "EFGH" },
		{ "<<-\"E\\\nF\"", "EF" }
	};

	[Theory]
	[MemberData(nameof(ContinuedOpenings))]
	public void O523ABackslashNewlineInsideTheWordContinuesItAtTopLevel(string opening, string closingWord)
	{
		string source = $"cat {opening}\nbody\n{ClosingLineOf(opening, closingWord)}\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheContinuedHereDocumentIsRead(mask, source, opening, closingWord);
		AssertTheZoneOfRange(mask, source, new ExpectedRange(0, source.IndexOf(opening, StringComparison.Ordinal), LexicalZone.Code));
	}

	[Theory]
	[MemberData(nameof(ContinuedOpenings))]
	public void O523ABackslashNewlineInsideTheWordContinuesItInsideAHole(string opening, string closingWord)
	{
		const string holeHead = "x=\"$(";
		string source = $"{holeHead}cat {opening}\nbody\n{ClosingLineOf(opening, closingWord)}\n)\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheContinuedHereDocumentIsRead(mask, source, opening, closingWord);
		AssertTheZoneOfRange(mask, source, new ExpectedRange(holeHead.Length, source.IndexOf(opening, StringComparison.Ordinal) - holeHead.Length, LexicalZone.Code));
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	[Theory]
	[MemberData(nameof(ContinuedOpenings))]
	public void O523ABackslashNewlineInsideTheWordContinuesItInAnArmInsideAHole(string opening, string closingWord)
	{
		const string holeHead = "x=\"$(";
		const string armHead = "case a in a) cat ";
		const string armTail = ";; esac";
		string source = $"{holeHead}{armHead}{opening}\nbody\n{ClosingLineOf(opening, closingWord)}\n{armTail})\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheContinuedHereDocumentIsRead(mask, source, opening, closingWord);
		AssertTheZoneOfRange(mask, source, new ExpectedRange(holeHead.Length, armHead.Length, LexicalZone.Code));
		ZoneAssertions.SpansZone(mask, source, armTail, LexicalZone.Code);
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	[Fact]
	public void O523TheOpeningLineIsTheOneThatEndsTheCommandAfterTheContinuation()
	{
		const string source = "cat <<E\\\nF && echo x\nbody\nEF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<E\\\nF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "&& echo x", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523AContinuationAfterTheContinuedWordStillMovesTheBodyToTheLineThatEndsTheCommand()
	{
		const string source = "cat <<E\\\nF \\\n&& echo x\nbody\nEF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "&& echo x", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523AContinuationFollowedDirectlyByABlankLineEndsTheOpeningLineAtTheBlankLine()
	{
		const string source = "cat <<E\\\n\nbody\nE\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<E\\\n", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523ACarriageReturnNormalizedContinuationReadsTheSameWord()
	{
		string source = TextNormalization.Normalize("cat <<E\\\r\nF\r\nbody\r\nEF\r\nafter=1\r\n");

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523AContinuationThatEndsTheTextLeavesTheBodyUnterminatedAndBounded()
	{
		const string source = "cat <<E\\\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<E\\\n", LexicalZone.Delimiter);
		Assert.Equal(source.Length, mask.Length);
	}

	[Fact]
	public void O523ATerminatorContinuedAcrossABackslashNewlineOnABodyLineClosesTheBodyAsBashDoes()
	{
		const string source = "cat <<E\\\nF\nE\\\nF\nEF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "E\\\nF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "EF", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<EOF\n\\\nEOF\nafter=1\n", "EOF", 1)]
	[InlineData("cat <<EOF\nEOF\\\n\nafter=1\n", "EOF\\\n", 0)]
	[InlineData("cat <<EOF\nE\\\nO\\\nF\nafter=1\n", "E\\\nO\\\nF", 0)]
	[InlineData("cat <<-EOF\n\t\\\n\t\tEOF\nafter=1\n", "EOF", 1)]
	[InlineData("cat <<-EOF\n\tE\\\nOF\nafter=1\n", "E\\\nOF", 0)]
	public void O523AnUnquotedBodyClosesOnALogicalLineThatItsContinuationsMakeTheTerminator(string source, string closing, int occurrence)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, closing, LexicalZone.Delimiter, occurrence);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523AQuotedBodyTakesNoContinuationsSoAContinuedTerminatorLineClosesNothing()
	{
		const string source = "cat <<'EF'\nE\\\nF\nEF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "E\\\nF\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- A comment's backslash, a backslash before the terminator line, a raw line feed in the word ----

	[Theory]
	[InlineData("cat <<EOF # c\\\nbody\nEOF\nafter=1\n")]
	[InlineData("cat <<-EOF # c\\\nbody\nEOF\nafter=1\n")]
	[InlineData("cat <<'EOF' # c\\\nbody\nEOF\nafter=1\n")]
	[InlineData("cat <<EOF # c\\\\\\\nbody\nEOF\nafter=1\n")]
	[InlineData("x=\"$(cat <<EOF # c\\\nbody\nEOF\n)\"\nafter=1\n")]
	public void R10ACommentEndingInABackslashOnTheOpeningLineDoesNotContinueTheCommand(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void R10ABackslashThatIsNotInACommentStillContinuesTheCommand()
	{
		const string source = "cat <<EOF \\#c\\\n&& echo x\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "&& echo x", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<EOF\nfoo\\\nEOF\nbar\nEOF\nafter=1\n", "foo\\\nEOF\nbar\n")]
	[InlineData("cat <<-EOF\n\tfoo\\\n\tEOF\n\tbar\n\tEOF\nafter=1\n", "\tfoo\\\n\tEOF\n\tbar\n")]
	public void R10AnUnquotedBodyLineEndingInABackslashJoinsTheTerminatorLineAndSoClosesNothing(string source, string body)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, body, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 2);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<'EOF'\nfoo\\\nEOF\nafter=1\n", "foo\\\n")]
	[InlineData("cat <<-'EOF'\n\tfoo\\\n\tEOF\nafter=1\n", "\tfoo\\\n\t")]
	[InlineData("cat <<\\EOF\nfoo\\\nEOF\nafter=1\n", "foo\\\n")]
	public void R10AQuotedBodyLineEndingInABackslashIsTextAndTheTerminatorLineStillCloses(string source, string body)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, body, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void R10AnEvenRunOfBackslashesBeforeTheLineFeedContinuesNothing()
	{
		const string source = "cat <<EOF\nfoo\\\\\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "foo\\\\\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<'E\nF'\nline\nE\nF\nafter=1\n")]
	[InlineData("cat <<\"E\nF\"\nline\nE\nF\nafter=1\n")]
	[InlineData("cat <<-'E\nF'\n\tline\n\tE\n\tF\nafter=1\n")]
	[InlineData("cat <<$'E\\\nF'\nline\nE\nF\nafter=1\n")]
	[InlineData("x=\"$(cat <<'E\nF'\nline\nE\nF\n)\"\nafter=1\n")]
	public void R10ATerminatorWordQuotedAcrossARawLineFeedIsReadAndClosesOnNoLine(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(source.IndexOf("<<", StringComparison.Ordinal), mask.ValidThroughPosition);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf("<<", StringComparison.Ordinal)));
	}

	[Fact]
	public void R10ATerminatorWordQuotedAcrossARawLineFeedKeepsTheStatementsBeforeItSound()
	{
		const string source = "before=1\ncat <<'E\nF'\nline\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "before=1", LexicalZone.Code);
		Assert.Equal(source.IndexOf("<<", StringComparison.Ordinal), mask.ValidThroughPosition);
	}

	private static string ClosingLineOf(string opening, string closingWord)
	{
		return opening.StartsWith(_TAB_STRIPPING_OPENING, StringComparison.Ordinal) ? $"{_LEADING_TAB}{closingWord}" : closingWord;
	}

	private static void AssertTheContinuedHereDocumentIsRead(ZoneMask mask, string source, string opening, string closingWord)
	{
		string closingLine = ClosingLineOf(opening, closingWord);
		int closingLineStart = source.IndexOf($"\n{closingLine}\n", source.IndexOf(opening, StringComparison.Ordinal), StringComparison.Ordinal) + 1;

		ZoneAssertions.SpansZone(mask, source, opening, LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		AssertTheZoneOfRange(mask, source, new ExpectedRange(closingLineStart + closingLine.Length - closingWord.Length, closingWord.Length, LexicalZone.Delimiter));
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	private static void AssertTheZoneOfRange(ZoneMask mask, string source, ExpectedRange expected)
	{
		Assert.True(mask.Zones.Slice(expected.Start, expected.Length).ToArray().All(actual => actual == expected.Zone), ZoneAssertions.Render(mask, source));
	}

	/// <summary>A run of positions that must all carry one zone.</summary>
	private readonly record struct ExpectedRange(int Start, int Length, LexicalZone Zone);

	// ---- Multi-line arithmetic ----

	[Fact]
	public void O524AMultiLineArithmeticExpressionClosesAndNothingOnItsContinuationLineOpens()
	{
		const string source = "echo $(( 1 +\n  2 << 3 ))\ncat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 3", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("(( n =\n 1 << 2 ))\n")]
	[InlineData("x=\"$(( 1 +\n 2 << 3 ))\"\n")]
	[InlineData("(( (a << 1) +\n (b << 2) ))\n")]
	[InlineData("echo $(( n - 1 ))\nx=$((\n1 <<x\n))\n")]
	public void O524TheShiftOnAContinuationLineIsAnOperatorInEveryNeighbouringShape(string head)
	{
		string source = $"{head}cat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524ACommandSubstitutionInsideArithmeticIsACommandContextAgain()
	{
		const string source = "x=$(( 1 + $(cat <<EOF\nbody\nEOF\n) ))\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524ADoubleParenthesisInALiteralIsNotCounted()
	{
		const string source = "echo \"((\" <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524ADoubleParenthesisThatNeverClosesStartsNoContextAndSwallowsNothingAfterIt()
	{
		const string source = "(( x\ncat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524AnExpressionThatClosedOnALaterLineDoesNotRefuseTheOpeningAfterIt()
	{
		const string source = "echo $((1 +\n2)) <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524AProfileWithoutTheArithmeticContextReadsTheContinuationShiftAsAnOpeningAndAbandonsTheTail()
	{
		const string source = "echo $(( 1 +\n 2 <<x ))\nafter=1\n";

		ZoneMask mask = new LexicalScanner(new HereDocumentExemplarAnalyzer().LexicalProfile).Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
	}

	[Theory]
	[InlineData("((cat <<EOF\nbody\nEOF\n) | wc)\nafter=1\n")]
	[InlineData("x=\"$((cat <<EOF\nbody\nEOF\n) | wc)\"\nafter=1\n")]
	[InlineData("(( (cat <<EOF\nbody\nEOF\n) ) )\nafter=1\n")]
	public void O524ADoubleParenthesisThatIsTwoNestedSubshellsStartsNoContextAndItsHereDocumentOpens(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("(( x = \")\" +\n 2 << 3 ))\n")]
	[InlineData("(( x = 'a)' +\n 2 << 3 ))\n")]
	[InlineData("(( x = \\) +\n 2 << 3 ))\n")]
	public void O524AnEscapedOrQuotedParenthesisDoesNotEndTheExpression(string head)
	{
		string source = $"{head}cat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 3", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524ACommandSubstitutionThatClosesAfterAnArmInsideArithmeticReturnsToArithmetic()
	{
		const string source = "x=$(( 1 + $(case a in a) echo 1 ;; esac) << 2 ))\ncat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 2", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524ABracketInsideAMultiCharacterNestingOpeningStartsTheArithmeticContext()
	{
		const string source = "x=\"@[ @[[ 1 << 2 ]]@ <<EOF\nbody\nEOF\n]@\"\nafter=1\n";

		ZoneMask mask = new LexicalScanner(BracketTokenProfile.Build()).Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 2", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524ABracketInsideAMultiCharacterClosingTokenEndsTheCommandContext()
	{
		const string source = "x=\"@[ @[[ 1 + @[ x ]@ << 2 ]]@ ]@\"\nafter=1\n";

		ZoneMask mask = new LexicalScanner(BracketTokenProfile.Build()).Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 2", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- The nesting of arm blocks and the reading of command positions ----

	[Theory]
	[InlineData("x=\"$(case a in a) $(esac) ;; b) x;; esac)\" # c\n")]
	[InlineData("x=\"$(case a in a) $(case b in b) x;; esac) ;; c) y;; esac)\" # c\n")]
	[InlineData("x=\"$(case a in a) echo $(echo esac) ;; b) x;; esac)\" # c\n")]
	public void O522AClosingKeywordInANestedSubstitutionEndsNoOuterBlock(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheHoleInteriorIsCode(mask, source);
		ZoneAssertions.SpansZone(mask, source, "# c", LexicalZone.Comment);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("x=\"$(! case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$({ case a in a) echo;; esac; })\" # c\n")]
	[InlineData("x=\"$(true && case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(true || case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(time case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(coproc case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(while case a in a) true;; esac; do break; done)\" # c\n")]
	[InlineData("x=\"$(f() case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(case a in a) echo esac;; esac)\" # c\n")]
	[InlineData("x=\"$(case a in a) echo;;& b) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(true; then case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(! time case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(if case a in a) true;; esac; then :; fi)\" # c\n")]
	[InlineData("x=\"$({ ! case a in a) x;; esac; })\" # c\n")]
	[InlineData("x=\"$(true; then then case a in a) x;; esac)\" # c\n")]
	public void O522AReservedWordIsReadAtEveryCommandPositionTheGrammarHasAndAfterAChainOfLicensingWords(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheHoleInteriorIsCode(mask, source);
		ZoneAssertions.SpansZone(mask, source, "# c", LexicalZone.Comment);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("x=\"$(echo $(date) case a in a) x)\"\n", " x)")]
	[InlineData("x=\"$(a=1 case a in a) x)\"\n", " x)")]
	[InlineData("x=\"$(echo case a in a) x)\"\n", " x)")]
	public void O522AWordAfterAPlainWordOrAnAssignmentOrASubstitutionOpensNoBlock(string source, string expectedStringBody)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedStringBody, LexicalZone.StringBody);
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	// ---- ANSI-C quoted and translated terminator parts ----

	[Theory]
	[InlineData("cat <<$'EOF'\n$(echo hi)\nEOF\nafter=1\n")]
	[InlineData("cat <<$'E\\x4fF'\n$(echo hi)\nEOF\nafter=1\n")]
	[InlineData("cat <<$'\\105OF'\n$(echo hi)\nEOF\nafter=1\n")]
	[InlineData("cat <<$'E\\117F'\n$(echo hi)\nEOF\nafter=1\n")]
	[InlineData("cat <<E$'\\x4f'F\n$(echo hi)\nEOF\nafter=1\n")]
	[InlineData("cat <<$\"EOF\"\n$(echo hi)\nEOF\nafter=1\n")]
	[InlineData("cat <<$'\\u0045OF'\n$(echo hi)\nEOF\nafter=1\n")]
	public void O523AnAnsiCQuotedOrTranslatedPartIsDecodedIntoTheEffectiveWordAndIsQuoted(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _EXPANSION_IN_A_BODY, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523AnAnsiCQuotedTerminatorIsNotClosedByTheLineAsWritten()
	{
		const string source = "cat <<$'E\\x4fF'\n$'E\\x4fF'\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(source.IndexOf("\n$'E", StringComparison.Ordinal) + 1));
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("a\\nb", "a\nb")]
	[InlineData("\\x41\\x4a", "AJ")]
	[InlineData("\\101\\60", "A0")]
	[InlineData("\\u0041\\U00000042", "AB")]
	[InlineData("\\e\\E", "\u001B\u001B")]
	[InlineData("\\a\\b\\f\\r\\t\\v", "\a\b\f\r\t\v")]
	[InlineData("\\\\\\'\\\"\\?", "\\'\"?")]
	[InlineData("\\cA\\c?", "\u0001\u007F")]
	[InlineData("\\q\\x", "\\q\\x")]
	[InlineData("ab\\0cd", "ab")]
	[InlineData("trailing\\", "trailing\\")]
	public void O523TheAnsiCEscapesDecodeAsAShellWithAnsiCQuotingReadsThem(string content, string expected)
	{
		Assert.Equal(expected, AnsiCEscapes.Decode(content));
	}

	[Theory]
	[InlineData("x=\"$(echo then case a in a) x)\"\n")]
	[InlineData("x=\"$(echo do case a in a) x)\"\n")]
	[InlineData("x=\"$(x=then case a in a) x)\"\n")]
	[InlineData("x=\"$(echo ! case a in a) x)\"\n")]
	[InlineData("x=\"$(echo { case a in a) x)\"\n")]
	[InlineData("x=\"$(echo time case a in a) x)\"\n")]
	[InlineData("x=\"$(echo coproc case a in a) x)\"\n")]
	[InlineData("x=\"$(echo then then case a in a) x)\"\n")]
	[InlineData("x=\"$(echo; echo then case a in a) x)\"\n")]
	public void O522AWordSpelledLikeALicensingWordAmongTheArgumentsLicensesNothing(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, " x)", LexicalZone.StringBody);
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	[Theory]
	[InlineData("cat <<\"E\\\\F\"\n", "E\\F")]
	[InlineData("cat <<\"E\\\"F\"\n", "E\"F")]
	[InlineData("cat <<\"E\\$F\"\n", "E$F")]
	[InlineData("cat <<\"E\\`F\"\n", "E`F")]
	[InlineData("cat <<\"E\\xF\"\n", "E\\xF")]
	[InlineData("cat <<$\"E\\\"F\"\n", "E\"F")]
	[InlineData("cat <<-\"E\\\\F\"\n", "E\\F")]
	[InlineData("cat <<A\"E\\\\\"B\n", "AE\\B")]
	public void O523AnEscapeInADoubleQuotedTerminatorPartIsRemovedByTheDoubleQuoteRuleAndTheWordIsQuoted(string openingLine, string effectiveWord)
	{
		string source = $"{openingLine}$(echo hi)\n{effectiveWord}\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _EXPANSION_IN_A_BODY, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O523ADoubleQuotedTerminatorIsNotClosedByTheLineAsWritten()
	{
		const string source = "cat <<\"E\\\"F\"\nE\\\"F\nE\"F\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(source.IndexOf("\nE\\\"F\n", StringComparison.Ordinal) + 1));
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("a\\\\b", "a\\b")]
	[InlineData("a\\\"b", "a\"b")]
	[InlineData("a\\$b\\`c", "a$b`c")]
	[InlineData("a\\xb\\", "a\\xb\\")]
	[InlineData("a\\\nb", "ab")]
	[InlineData("a\\\n", "a")]
	[InlineData("a\\\\\nb", "a\\\nb")]
	[InlineData("", "")]
	public void O523TheDoubleQuoteRuleRemovesTheBackslashBeforeFourCharactersAndTheContinuationBeforeALineFeed(string content, string expected)
	{
		Assert.Equal(expected, DoubleQuoteEscapes.Decode(content));
	}

	[Theory]
	[InlineData("\\U00110000", "\\U00110000")]
	[InlineData("\\UFFFF0041", "\\UFFFF0041")]
	[InlineData("\\uD800", "\\uD800")]
	[InlineData("\\uDFFFz", "\\uDFFFz")]
	[InlineData("\\U0001F600", "\U0001F600")]
	[InlineData("\\U0010FFFF", "\U0010FFFF")]
	[InlineData("ab\\u0000cd", "ab")]
	[InlineData("ab\\x00cd", "ab")]
	[InlineData("ab\\c@cd", "ab")]
	[InlineData("ab\\400cd", "ab")]
	[InlineData("\\777", "ÿ")]
	public void O523AnAnsiCValueThatIsNotAScalarIsKeptAsWrittenAndANulEndsTheWord(string content, string expected)
	{
		Assert.Equal(expected, AnsiCEscapes.Decode(content));
	}

	[Fact]
	public void O523ANulInAnAnsiCQuotedTerminatorEndsTheWordAtTheNul()
	{
		const string source = "cat <<$'EO\\x00F'\nbody\nEO\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- the arithmetic read-ahead reads quoted runs by the rule's own escapes and is exact ----

	[Theory]
	[InlineData("x=$(( 1 +\n$'a\\')' << 2 ))\ncat <<EOF\nbody\nEOF\nafter=1\n")]
	[InlineData("x=$(( 1 +\n$\"a\\\")\" << 2 ))\ncat <<EOF\nbody\nEOF\nafter=1\n")]
	[InlineData("x=$(( 1 +\n\"a\\\")\" << 2 ))\ncat <<EOF\nbody\nEOF\nafter=1\n")]
	[InlineData("x=$(( 1 +\n'a)' << 2 ))\ncat <<EOF\nbody\nEOF\nafter=1\n")]
	[InlineData("x=$(( 1 + $(echo # )\n) << 2 ))\ncat <<EOF\nbody\nEOF\nafter=1\n")]
	public void O524AQuotedRunOrACommentHoldingAClosingBracketIsSkippedByTheRulesOwnEscapes(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 2", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524ABacktickRunHoldingTheClosingRunIsSkippedByTheProfilesBacktickRule()
	{
		const string source = "x=[[ 1 +\n`]]` << 2 ]]\ncat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = new LexicalScanner(BracketTokenProfile.Build()).Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<< 2", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524TheArithmeticReadAheadAgreesWithTheStraightforwardReadOnRandomBracketText()
	{
		int refused = CountTheArithmeticSamplesAgreeingWithTheOracle(_bracketTokens, _AGREEMENT_SAMPLES, IsInsideAStraightforwardArithmeticRead);
		Assert.True(refused >= _MINIMUM_BRACKET_ARITHMETIC_SAMPLES, $"{refused} samples were refused, at least {_MINIMUM_BRACKET_ARITHMETIC_SAMPLES} are wanted");
	}

	/// <summary>
	/// The keywords of the arm block, and the pattern and arm terminator that go with them, among the brackets: a
	/// keyword standing directly inside a double parenthesis or inside a plain parenthesis opens no block, so the
	/// straightforward read, which knows no keyword, must still agree with the scanner, whose read-ahead is shared by
	/// every run it passes.
	/// </summary>
	[Fact]
	public void O524TheArithmeticReadAheadAgreesWithTheStraightforwardReadWhenKeywordsStandAmongTheBrackets()
	{
		int refused = CountTheArithmeticSamplesAgreeingWithTheOracle(_keywordTokens, _KEYWORD_AGREEMENT_SAMPLES, IsInsideAStraightforwardArithmeticRead);
		Assert.True(refused >= _MINIMUM_KEYWORD_ARITHMETIC_SAMPLES, $"{refused} samples were refused, at least {_MINIMUM_KEYWORD_ARITHMETIC_SAMPLES} are wanted");
	}

	/// <summary>
	/// Command substitutions, their arm blocks and keywords among the brackets: the oracle is the straightforward reading of
	/// <see cref="NaiveArithmeticReading"/>, which tracks the substitution frames inside arithmetic and the depth at which an
	/// arm ends, and which the scanner must agree with on every text, seeded and fast.
	/// </summary>
	[Fact]
	public void O524TheArithmeticReadAheadAgreesWithTheStraightforwardReadWhenSubstitutionsAndArmBlocksStandAmongTheBrackets()
	{
		int refused = CountTheArithmeticSamplesAgreeingWithTheOracle(_substitutionTokens, _SUBSTITUTION_AGREEMENT_SAMPLES, NaiveArithmeticReading.IsInsideArithmetic);
		Assert.True(refused >= _MINIMUM_SUBSTITUTION_ARITHMETIC_SAMPLES, $"{refused} samples were refused, at least {_MINIMUM_SUBSTITUTION_ARITHMETIC_SAMPLES} are wanted");
	}

	/// <summary>
	/// Asserts, for each seeded random text, that the opening placed in it is refused exactly where the oracle says it
	/// is inside arithmetic, and returns how many samples the oracle expected to be refused, so a test whose generator
	/// stopped producing arithmetic cannot pass by agreeing on nothing. Three texts in four are wrapped in a double
	/// parenthesis so that the generator reaches the inside of an arithmetic expression often.
	/// </summary>
	private static int CountTheArithmeticSamplesAgreeingWithTheOracle(string[] tokens, int samples, Func<string, int, bool> isInsideArithmetic)
	{
		Random random = new(_AGREEMENT_SEED);
		int refused = 0;

		for (int sample = 0; sample < samples; sample++)
		{
			string generated = BuildARandomBracketText(random, tokens);
			string body = random.Next(_UNWRAPPED_ONE_IN) != 0 ? $"{_WRAPPER_OPENING}{generated}{_WRAPPER_CLOSING}" : generated;
			int candidate = random.Next(body.Length + 1);
			string source = $"{body[..candidate]}<<x{body[candidate..]}";
			bool inside = isInsideArithmetic(source, candidate);
			LexicalZone expected = inside ? LexicalZone.Code : LexicalZone.Delimiter;

			refused += inside ? 1 : 0;

			ZoneMask mask = ShellShapedProfile.Scan(source);

			Assert.True(mask.ZoneAt(candidate) == expected, $"sample {sample}: the opening at {candidate} of {source.Replace("\n", "\\n", StringComparison.Ordinal)} is {mask.ZoneAt(candidate)}, expected {expected}");
		}

		return refused;
	}

	private static string BuildARandomBracketText(Random random, string[] tokens)
	{
		int count = random.Next(_AGREEMENT_MINIMUM_LENGTH, _AGREEMENT_MAXIMUM_LENGTH);
		StringBuilder text = new();

		for (int index = 0; index < count; index++)
		{
			_ = text.Append(tokens[random.Next(tokens.Length)]);
		}

		return text.ToString();
	}

	/// <summary>
	/// The straightforward read, written without any memory of what it has read: left to right, a run of two or more
	/// opening brackets that is not inside an expression already found starts one where the first closing bracket
	/// met with none open is followed by a second.
	/// </summary>
	private static bool IsInsideAStraightforwardArithmeticRead(string text, int position)
	{
		int cursor = 0;
		bool inside = false;

		while (cursor < text.Length && !inside)
		{
			int end = text[cursor] == _OPENING_BRACKET && cursor + 1 < text.Length && text[cursor + 1] == _OPENING_BRACKET
				? StraightforwardArithmeticEnd(text, cursor + _OPENING_RUN_LENGTH)
				: _NO_END;

			inside = end > 0 && position >= cursor && position < end;
			cursor = end > 0 && !inside ? end : cursor + 1;
		}

		return inside;
	}

	private static int StraightforwardArithmeticEnd(string text, int from)
	{
		int depth = 0;
		int end = _NO_END;
		bool decided = false;

		for (int index = from; index < text.Length && !decided; index++)
		{
			if (text[index] == _OPENING_BRACKET)
			{
				depth++;
			}
			else if (text[index] == _CLOSING_BRACKET && depth > 0)
			{
				depth--;
			}
			else if (text[index] == _CLOSING_BRACKET)
			{
				end = index + 1 < text.Length && text[index + 1] == _CLOSING_BRACKET ? index + _CLOSING_RUN_LENGTH : _NO_END;
				decided = true;
			}
		}

		return end;
	}

	// ---- reserved words are read in command contexts only, option words and the recovery bound ----

	[Theory]
	[InlineData("x=\"$(time -p case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(time -p -x case a in a) echo;; esac)\" # c\n")]
	[InlineData("x=\"$(! time -p case a in a) echo;; esac)\" # c\n")]
	public void O522TheOptionWordsOfTimeKeepTheCommandPositionOfTheKeywordAfterThem(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "# c", LexicalZone.Comment);
		AssertTheHoleInteriorIsCode(mask, source);
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	[Theory]
	[InlineData("x=\"$(echo time -p case a in a) x)\"\n")]
	[InlineData("x=\"$(time x -p case a in a) x)\"\n")]
	[InlineData("x=\"$(then -p case a in a) x)\"\n")]
	[InlineData("x=\"$(echo; -p case a in a) x)\"\n")]
	public void O522AnOptionWordKeepsTheCommandPositionOnlyAfterAKeywordThatTakesOptions(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, " x)", LexicalZone.StringBody);
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	[Theory]
	[InlineData("x=\"$(( case * 2 ))\" # c\n")]
	[InlineData("x=\"$(( (case) * 2 ))\" # c\n")]
	[InlineData("x=\"$(( $(case a in a) echo;; esac) + 1 ))\" # c\n")]
	[InlineData("x=\"$(( $((case)) + (esac) ))\" # c\n")]
	public void O522AReservedWordIsReadInACommandContextInsideArithmeticAndNotInTheArithmeticItself(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "# c", LexicalZone.Comment);
		AssertTheHoleInteriorIsCode(mask, source);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O522AnArmBlockIsStillReadAfterTheRecoveryBoundIsSpent()
	{
		string source = $"{string.Concat(Enumerable.Repeat(_UNBALANCED_HOLE_LINE, LexicalEnginePolicy.MAXIMUM_NESTING_RECOVERIES_PER_FILE))}x=\"$(case a in a) echo hit;; esac)\" # c\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalEnginePolicy.MAXIMUM_NESTING_RECOVERIES_PER_FILE, mask.UnbalancedNestingCount);
		ZoneAssertions.SpansZone(mask, source, "case a in a) echo hit;; esac", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "# c", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O522AnUnbalancedHoleAfterTheRecoveryBoundIsAbandonedAsUnterminatedAndNotReadByItsFirstClosing()
	{
		string source = $"{string.Concat(Enumerable.Repeat(_UNBALANCED_HOLE_LINE, LexicalEnginePolicy.MAXIMUM_NESTING_RECOVERIES_PER_FILE + 1))}after=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.True(mask.WasAbandonedMidFile);
	}

	// ---- a closing keyword inside plain parentheses stands at a deeper depth than the block it would close ----

	[Theory]
	[InlineData("x=\"$(case a in a) ( esac ) x) ;; esac)\"\necho after\n")]
	[InlineData("x=\"$(case a in a) echo ;; (esac) ;; esac)\"\necho after\n")]
	[InlineData("x=\"$(case a in a) ( case b in b) echo ;; esac ) ;; c) y ;; esac)\"\necho after\n")]
	[InlineData("x=\"$(case a in a) ( ( esac ) ) x) ;; esac)\"\necho after\n")]
	public void O522AClosingKeywordInsidePlainParenthesesAtADeeperDepthEndsNoBlockOpenedOutsideThem(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheHoleInteriorIsCode(mask, source);
		ZoneAssertions.SpansZone(mask, source, "echo after", LexicalZone.Code);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- the code escape: a character after the escape is text in code, for the walk and the read-ahead alike ----

	[Theory]
	[InlineData("x=\"$((echo \\) ) )\" # c\nafter=1\n")]
	[InlineData("x=\"$(echo a \\) b)\" # c\nafter=1\n")]
	[InlineData("x=\"$(( \\) ))\" # c\nafter=1\n")]
	[InlineData("x=\"$(echo \\( a)\" # c\nafter=1\n")]
	[InlineData("x=\"$(echo \\\\)\" # c\nafter=1\n")]
	public void O524AnEscapedBracketInCodeInsideAHoleIsTextAndEndsNothing(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		AssertTheHoleInteriorIsCode(mask, source);
		ZoneAssertions.SpansZone(mask, source, "# c", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.Equal(0, mask.UnbalancedNestingCount);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("echo a \\) b\nafter=1\n")]
	[InlineData("echo a \\# b\nafter=1\n")]
	[InlineData("echo \\\" not a string\nafter=1\n")]
	[InlineData("echo \\' not a string\nafter=1\n")]
	[InlineData("echo a\\\nb\nafter=1\n")]
	public void O524AnEscapedCharacterInTheFilesOwnCodeOpensNothing(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.StringBody);
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Comment);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524AnEscapedAngleBracketOpensNoHereDocument()
	{
		const string source = "cat \\<<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "<<EOF\nbody\nEOF\nafter=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("echo a\\")]
	[InlineData("\\")]
	[InlineData("x=\"$(echo \\")]
	[InlineData("x=$(( 1 +\\")]
	public void O524ALoneEscapeAtTheEndOfTheTextIsReadWithoutLeavingTheText(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(source.Length, mask.Length);
	}

	[Fact]
	public void O524AnEscapeThatEndsAnIdentifierExtentRegionDoesNotConsumeTheCharacterAfterTheRegion()
	{
		const string source = "x=\"@ab\\\" z\nafter=1\n";
		LexicalProfile profile = new()
		{
			ProfileName = nameof(ShellScannerCorrectionTests),
			ConstantDeclarationExpression = HereDocumentExemplarPatternExpressions.ConstantDeclaration(),
			LineCommentRules = [],
			BlockCommentRules = [],
			QuoteRules =
			[
				new QuoteRule
				{
					Opening = "\"",
					Closing = "\"",
					NewlinePolicy = NewlinePolicy.Allowed,
					InterpolationOpening = "@",
					InterpolationIdentifierExtent = new Regex(_EXTENT_ENDING_IN_THE_ESCAPE, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
				}
			],
			CodeEscapeCharacter = '\\'
		};

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		ZoneAssertions.SpansZone(mask, source, " z\nafter=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524AnEscapedLineFeedAfterAHereDocumentOpeningStillMovesTheBodyToTheLineThatEndsTheCommand()
	{
		const string source = "cat <<EOF \\\n&& echo x\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "&& echo x", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void O524AProfileWithoutACodeEscapeReadsABackslashInCodeAsAnOrdinaryCharacter()
	{
		const string source = "x=\"$(echo a \\) b)\"\nafter=1\n";

		ZoneMask mask = new LexicalScanner(new HereDocumentExemplarAnalyzer().LexicalProfile).Scan(source);

		ZoneAssertions.SpansZone(mask, source, " b)", LexicalZone.StringBody);
	}

	// ---- a closing run longer than the closing is not part of the expression ----

	[Theory]
	[InlineData("x=$(( $( $(( 1 ))) <<EOF )) y\nafter=1\n")]
	[InlineData("x=$(( $( $(( $( $(( 1 ))) ))) <<EOF )) y\nafter=1\n")]
	public void O524AClosingRunLongerThanTheClosingEndsTheExpressionAfterTheClosingOnly(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- an arm's closing bracket at its own depth, inside a command substitution inside arithmetic ----

	[Theory]
	[InlineData("x=$(( $(case a in a) $(esac) b) c ;; esac) << 2 ))\nafter=1\n", "<< 2")]
	[InlineData("x=$(( $(case a in a) echo ;; b) echo ;; esac) << 2 ))\nafter=1\n", "<< 2")]
	[InlineData("x=$(( 1 + $(case a in a) $(case b in b) $(esac) c) d ;; esac) e ;; esac) << 2 ))\nafter=1\n", "<< 2")]
	[InlineData("x=$(( $(case a in a) $(esac) b) c ;; esac) ))\ny=$(( 1 << 3 ))\nafter=1\n", "<< 3")]
	public void O524AnArmClosingBracketAtItsOwnDepthInsideASubstitutionInsideArithmeticEndsNothing(string source, string shift)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf(shift, StringComparison.Ordinal)));
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Delimiter);
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- the walk is linear in the text on the shapes that could make it quadratic ----

	[Theory]
	[InlineData(PathologicalShape.RunsOfOpeningBrackets)]
	[InlineData(PathologicalShape.BlanksBeforeAKeyword)]
	[InlineData(PathologicalShape.OneLongWordBeforeAKeyword)]
	[InlineData(PathologicalShape.ALongChainOfLicensingWords)]
	[InlineData(PathologicalShape.ManyUnclosedDoubleParentheses)]
	public void O524AnyPathologicalShapeIsStillReadToTheEnd(PathologicalShape shape)
	{
		string source = BuildThePathologicalText(shape, PATHOLOGICAL_UNITS);

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.Equal(source.Length, mask.ValidThroughPosition);
	}

	public enum PathologicalShape
	{
		RunsOfOpeningBrackets,
		BlanksBeforeAKeyword,
		OneLongWordBeforeAKeyword,
		ALongChainOfLicensingWords,
		ManyUnclosedDoubleParentheses
	}

	internal static string BuildThePathologicalText(PathologicalShape shape, int units)
	{
		const string keywordTail = "case a in a) x;; esac)\"\n";

		return shape switch
		{
			PathologicalShape.RunsOfOpeningBrackets => $"{new string(_OPENING_BRACKET, units * _CHARACTERS_PER_UNIT)}\n",
			PathologicalShape.BlanksBeforeAKeyword => $"x=\"$({new string(' ', units * _CHARACTERS_PER_UNIT)}{keywordTail}",
			PathologicalShape.OneLongWordBeforeAKeyword => $"x=\"$({new string('a', units * _CHARACTERS_PER_UNIT)} {keywordTail}",
			PathologicalShape.ALongChainOfLicensingWords => $"x=\"$(true; {string.Concat(Enumerable.Repeat("then ", units * _CHARACTERS_PER_UNIT / _LICENSING_WORD_LENGTH))}{keywordTail}",
			_ => string.Concat(Enumerable.Repeat("((x\n", units * _CHARACTERS_PER_UNIT / _UNCLOSED_LINE_LENGTH))
		};
	}

	/// <summary>
	/// Every character between the opening of the first hole and the closing of the last is code. A hole that closed
	/// early leaves the rest of its interior as string body, so this fails where a comment after the hole would not.
	/// </summary>
	private static void AssertTheHoleInteriorIsCode(ZoneMask mask, string source)
	{
		const string holeOpening = "\"$(";
		int start = source.IndexOf(holeOpening, StringComparison.Ordinal) + holeOpening.Length;
		int end = source.LastIndexOf(")\"", StringComparison.Ordinal);

		for (int position = start; position < end; position++)
		{
			Assert.True(mask.ZoneAt(position) == LexicalZone.Code, $"position {position} ({source[position]}) is {mask.ZoneAt(position)}");
		}
	}

	private static string Summarize(Regex expression, string text, int start)
	{
		Match match = expression.Match(text, start);
		string terminator = string.Concat(match.Groups["terminator"].Captures.Select(capture => capture.Value));
		string ansiC = string.Join(',', match.Groups["ansic"].Captures.Select(capture => $"{capture.Index}:{capture.Length}"));
		string doubleQuoted = string.Join(',', match.Groups["dquoted"].Captures.Select(capture => $"{capture.Index}:{capture.Length}"));

		return match.Success && match.Index == start
			? $"{match.Index}|{match.Length}|{terminator}|{match.Groups["quoted"].Success}|{ansiC}|{doubleQuoted}"
			: "no match";
	}
}
