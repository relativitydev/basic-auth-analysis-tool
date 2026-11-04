using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// TWO HERE-DOCUMENT CASES: A HOLE IN A BODY, AND THE BACKSLASH RUN OF THE TAB-STRIPPING FORM. A <c>$( )</c> in an unquoted here-document body takes no
/// body from the lines after the outer terminator, and an even run of backslashes with <c>&lt;&lt;-</c> is pinned as the scanner reads it. The code span has
/// its own file, <c>ShellCodeSpanElementTests</c>, and the dropped opening its own, <c>ShellDroppedOpeningZoneTests</c>. Every credential is a placeholder pair
/// on a reserved example host. The cases run under the test-local <see cref="ShellShapedProfile"/> and, for one shape, under the shipped `Shell` profile.
/// </summary>
public sealed class ShellHereDocumentCorrectionsTests
{
	private const string _CREDENTIAL_LINE = "curl -u user:pass https://example.test";
	private const string _FOLLOWING_STATEMENT = "after=1";

	private static readonly LexicalProfile _shipped = ShellRegisteredExpressions.Analyzer.LexicalProfile;

	// ---- A hole in the body of an unquoted here-document hands its openings no body ----

	[Theory]
	[InlineData("cat <<EOF\ntext\n$(cat <<NESTED)\nEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("cat <<EOF\ntext\n$(cat <<A <<B)\nEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("cat <<EOF\ntext\n$(cat <<A; cat <<B)\nEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("cat <<-EOF\n\ttext\n\t$(cat <<NESTED)\n\tEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("cat <<EOF\n$(cat <<NESTED)\nEOF\nplain-command\ncurl -u user:pass https://example.test\n")]
	[InlineData("cat <<EOF\ntext\n$(echo \"$(cat <<NESTED)\")\nEOF\ncurl -u user:pass https://example.test\n")]
	public void C2AnOpeningInAHoleOfAnUnquotedBodyTakesNoBodyFromTheLinesAfterTheOuterTerminator(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid, ZoneAssertions.Render(mask, source));
	}

	[Fact]
	public void C2TheOpeningInTheHoleKeepsItsDelimiterMarkingAndTheOuterTerminatorStillCloses()
	{
		const string source = "cat <<EOF\ntext\n$(cat <<NESTED)\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<NESTED", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Theory]
	[InlineData("cat <<EOF\ntext\n$(cat <<NESTED)\nEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("cat <<-EOF\n\ttext\n\t$(cat <<NESTED)\n\tEOF\ncurl -u user:pass https://example.test\n")]
	public void C2TheSameShapeUnderTheShippedProfileReadsTheLinesAfterTheOuterTerminatorAsCode(string source)
	{
		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid, ZoneAssertions.Render(mask, source));
	}

	[Theory]
	[InlineData("cat <<EOF\ntext\n$(cat <<NESTED)\nEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("cat <<-EOF\n\ttext\n\t$(cat <<NESTED)\n\tEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=\"$(cat <<NESTED)\"\nbody\nNESTED\ncurl -u user:pass https://example.test\n")]
	public void C2TheCredentialLineAfterTheOuterTerminatorOfAHereDocumentWithAHoleIsHigh(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize("#!/bin/bash\n" + source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == "SH-001" && construct.Level == ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("cat <<EOF\n$(cat <<NESTED\ninner\nNESTED\n)\nEOF\nafter=1\n", "inner\n")]
	[InlineData("cat <<EOF\n$(cat <<NESTED\nline\n)\nNESTED\n)\nEOF\nafter=1\n", "line\n)\n")]
	public void C2AHoleThatSpansLinesAndHoldsItsNestedBodyAndClosingInsideTheOuterBodyIsUnchanged(string source, string nestedBody)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, nestedBody, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid, ZoneAssertions.Render(mask, source));
	}

	[Theory]
	[InlineData("x=\"$(cat <<NESTED)\"\nbody\nNESTED\ncurl -u user:pass https://example.test\n")]
	[InlineData("echo \"a $(cat <<NESTED) b\"\nbody\nNESTED\ncurl -u user:pass https://example.test\n")]
	public void C2NeighbourControlTheSameInnerOpeningInADoubleQuotedStringStillReadsTheLinesAfterTheCommandAsItsBody(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "NESTED", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid, ZoneAssertions.Render(mask, source));
	}

	[Fact]
	public void C2NeighbourControlAHoleInCodeHandsItsBodyToTheFilesOwnQueue()
	{
		const string source = "cat <<A \"$(cat <<X)\"\nx1\nX\na1\nA\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "x1\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "a1\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- An even run of backslashes with <<- ----

	[Theory]
	[InlineData("cat <<-EOF\n\tfoo\\\\\n\tEOF\nafter=1\n", "foo\\\\\n")]
	[InlineData("cat <<-EOF\n\tfoo\\\\\\\\\n\tEOF\nafter=1\n", "foo\\\\\\\\\n")]
	public void C5AnEvenRunOfBackslashesBeforeTheLineFeedOfABodyLineContinuesNothingUnderTheTabStrippingForm(string source, string bodyLine)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, bodyLine, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C5AnOddRunOfBackslashesJoinsTheTerminatorLineUnderTheTabStrippingFormSoNothingCloses()
	{
		const string source = "cat <<-EOF\n\tfoo\\\\\\\n\tEOF\nbar\n\tEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "\tfoo\\\\\\\n\tEOF\nbar\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 2);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData(": <<-EOF \\\\\n\tbody\n\tEOF\nafter=1\n")]
	[InlineData(": <<-EOF # note \\\\\n\tbody\n\tEOF\nafter=1\n")]
	public void C5AnEvenRunOfBackslashesOnTheOpeningLineContinuesNothingUnderTheTabStrippingForm(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "\tbody\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C5AnOddRunOfBackslashesOnTheOpeningLineMovesTheBodyToTheLineThatEndsTheCommandUnderTheTabStrippingForm()
	{
		const string source = ": <<-EOF \\\n&& echo x\n\tbody\n\tEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "&& echo x", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "\tbody\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C5AQuotedDelimiterTakesAnEvenRunOfBackslashesAsTextUnderTheTabStrippingForm()
	{
		const string source = "cat <<-'EOF'\n\tfoo\\\\\n\tEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "\tfoo\\\\\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	private static ZoneMask ScanShipped(string source)
	{
		return new LexicalScanner(_shipped).Scan(source);
	}
}
