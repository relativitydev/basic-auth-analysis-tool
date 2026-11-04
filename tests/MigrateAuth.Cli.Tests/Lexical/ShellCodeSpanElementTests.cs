using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Catalog;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// A HERE-DOCUMENT OPENED INSIDE A BACKTICK SUBSTITUTION. The quoted form is behaviour-neutral and pinned; the unquoted one-line form is read by the
/// code-span element of the lexical vocabulary, under which a here-document opened in the span has an empty body and the lines after the span are code.
/// Every credential is a placeholder pair on a reserved example host, and the cases run under the shipped `Shell` profile.
/// </summary>
public sealed class ShellCodeSpanElementTests
{
	private const string _CREDENTIAL_LINE = "curl -u user:pass https://example.test";
	private const string _FOLLOWING_STATEMENT = "after=1";
	private const string _CREDENTIAL_CLASS = "SH-001";
	private const string _BACKTICK = "`";
	private const string _ESCAPED_BACKTICK = "\\`";
	private const int _LINE_LENGTH_CEILING = 10000;
	private const int _OPENING_LENGTH = 1;

	private static readonly LexicalProfile _shipped = ShellRegisteredExpressions.Analyzer.LexicalProfile;

	// ---- The unquoted one-line form: a code span opens a here-document that has no body ----

	[Theory]
	[InlineData("x=`cat <<EOF`\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=`cat <<EOF`\nbody-line\nEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=`cat <<'EOF'`\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=`cat <<-EOF`\n\tbody-line\n\tEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("y=$(echo `cat <<EOF`)\ncurl -u user:pass https://example.test\n")]
	[InlineData("z=\"$(echo `cat <<EOF`)\"\ncurl -u user:pass https://example.test\n")]
	public void C1AnUnquotedOneLineBacktickSpanThatOpensAHereDocumentGivesItNoBodyAndTheLinesAfterItAreCode(string source)
	{
		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid, ZoneAssertions.Render(mask, source));
	}

	[Fact]
	public void C1TheTerminatorLineOfAHereDocumentWithNoBodyRunsAsCode()
	{
		const string source = "x=`cat <<EOF`\nbody-line\nEOF\ncurl -u user:pass https://example.test\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, "body-line\nEOF\n", LexicalZone.Code);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
	}

	[Fact]
	public void C1TheTwoBackticksAreDelimitersAndTheDroppedOpeningStaysCode()
	{
		const string source = "x=`cat <<EOF`\nafter=1\n";

		ZoneMask mask = ScanShipped(source);

		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf(_BACKTICK, StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.LastIndexOf(_BACKTICK, StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, "cat <<EOF", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
	}

	[Fact]
	public void C1ASpanThatHoldsOnlyCommandsAndAClosingQuoteKeepsItsContentAsCode()
	{
		const string source = "x=`echo \"a b\" $(date)`\nafter=1\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, "echo ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "a b", LexicalZone.StringBody);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf(_BACKTICK, StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C1AnEscapedBacktickInsideTheSpanIsContentAndDoesNotCloseIt()
	{
		string source = "x=`echo " + _ESCAPED_BACKTICK + "cat <<EOF" + _ESCAPED_BACKTICK + "`\n" + _CREDENTIAL_LINE + "\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.LastIndexOf(_BACKTICK, StringComparison.Ordinal)));
		Assert.True(mask.IsCompletelyValid, ZoneAssertions.Render(mask, source));
	}

	// ---- What the code span leaves unchanged ----

	[Fact]
	public void C1TheQuotedFormIsStillStringBodyAndTheLineAfterItIsCode()
	{
		const string source = "x=\"`cat <<EOF`\"\ncurl -u user:pass https://example.test\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, "cat <<EOF", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid, ZoneAssertions.Render(mask, source));
	}

	[Fact]
	public void C1ASpanWithNoHereDocumentAndACredentialInItKeepsTheCredentialAsCode()
	{
		const string source = "x=`curl -u user:pass https://example.test`\nafter=1\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C1ASpanThatSpansLinesKeepsItsHereDocumentBodyAsBody()
	{
		const string source = "x=`cat <<EOF\ncurl -u user:pass https://example.test\nEOF\n`\nafter=1\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C1ABacktickInTheBodyOfAnUnquotedHereDocumentIsTextOfTheBody()
	{
		const string source = "cat <<EOF\nx `cat <<NESTED`\nEOF\nafter=1\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, "x `cat <<NESTED`\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C1ABacktickInACommentIsAComment()
	{
		const string source = "# `cat <<EOF`\n" + _CREDENTIAL_LINE + "\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, "# `cat <<EOF`", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
	}

	// ---- The edge cases of the code span ----

	[Fact]
	public void C1OpenPoint1ACommentOpenerInsideAOneLineSpanCommentsOutTheRestOfTheContentOnlyAsBashDoes()
	{
		const string source = "x=`echo a # b`\n" + _CREDENTIAL_LINE + "\n";

		ZoneMask mask = ScanShipped(source);

		ZoneAssertions.SpansZone(mask, source, "# b", LexicalZone.Comment);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf('\n') - _OPENING_LENGTH));
		ZoneAssertions.SpansZone(mask, source, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void C1OpenPoint2ASpanWithAnUnbalancedQuoteIsLeftAsItWasAndRecordsNoAnomalyOfItsOwn()
	{
		const string source = "x=`echo \"abc`\n" + _CREDENTIAL_LINE + "\n\"\nafter=1\n";

		ZoneMask withTheElement = ScanShipped(source);
		ZoneMask withoutTheElement = ScanTheReferenceWithoutSpans(source);

		Assert.Equal(withoutTheElement.Zones.ToArray(), withTheElement.Zones.ToArray());
		Assert.Equal(withoutTheElement.AnomalyReason, withTheElement.AnomalyReason);
	}

	[Fact]
	public void C1OpenPoint3ABacktickAfterAnOddRunOfBackslashesOpensNoSpanAndAnEvenRunDoes()
	{
		string odd = "echo \\" + _BACKTICK + "x\n" + _CREDENTIAL_LINE + "\n";
		string even = "echo \\\\" + _BACKTICK + "x" + _BACKTICK + "\n" + _CREDENTIAL_LINE + "\n";

		ZoneMask oddMask = ScanShipped(odd);
		ZoneMask evenMask = ScanShipped(even);

		Assert.Equal(ScanTheReferenceWithoutSpans(odd).Zones.ToArray(), oddMask.Zones.ToArray());
		Assert.Equal(LexicalZone.Delimiter, evenMask.ZoneAt(even.IndexOf(_BACKTICK, StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.Delimiter, evenMask.ZoneAt(even.LastIndexOf(_BACKTICK, StringComparison.Ordinal)));
	}

	[Fact]
	public void C1OpenPoint4TheClosingSearchStopsAtTheLineLengthCeiling()
	{
		string inside = _BACKTICK + "echo " + new string('a', _LINE_LENGTH_CEILING - "`echo `".Length - 1) + _BACKTICK + "\nafter=1\n";
		string outside = _BACKTICK + "echo " + new string('a', _LINE_LENGTH_CEILING) + _BACKTICK + "\nafter=1\n";

		ZoneMask insideMask = ScanShipped(inside);
		ZoneMask outsideMask = ScanShipped(outside);

		Assert.Equal(LexicalZone.Delimiter, insideMask.ZoneAt(inside.IndexOf("\n", StringComparison.Ordinal) - _OPENING_LENGTH));
		Assert.Equal(LexicalZone.Code, outsideMask.ZoneAt(0));
		Assert.Equal(ScanTheReferenceWithoutSpans(outside).Zones.ToArray(), outsideMask.Zones.ToArray());
	}

	[Fact]
	public void C1OpenPoint5ASpanInsideAHoleAndInsideANestedExecutedScanIsRead()
	{
		const string inAHole = "z=\"$(echo `cat <<EOF`)\"\ncurl -u user:pass https://example.test\n";
		const string inAnExecutedString = "sh -c 'echo `cat <<EOF`'\ncurl -u user:pass https://example.test\n";

		ZoneMask holeMask = ScanShipped(inAHole);
		ZoneMask executedMask = ScanShipped(inAnExecutedString);

		ZoneAssertions.SpansZone(holeMask, inAHole, "cat <<EOF", LexicalZone.Code);
		ZoneAssertions.SpansZone(executedMask, inAnExecutedString, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, executedMask.ZoneAt(inAnExecutedString.IndexOf(_BACKTICK, StringComparison.Ordinal)));
		Assert.True(executedMask.IsCompletelyValid, ZoneAssertions.Render(executedMask, inAnExecutedString));
	}

	[Fact]
	public void C1OpenPoint6TheSameSpanReadAsAStringBodysInlineContextAndInCodeAgreesOnTheLinesAfterIt()
	{
		const string inAString = "x=\"`cat <<EOF`\"\ncurl -u user:pass https://example.test\n";
		const string inCode = "x=`cat <<EOF`\ncurl -u user:pass https://example.test\n";

		ZoneMask stringMask = ScanShipped(inAString);
		ZoneMask codeMask = ScanShipped(inCode);

		ZoneAssertions.SpansZone(stringMask, inAString, _CREDENTIAL_LINE, LexicalZone.Code);
		ZoneAssertions.SpansZone(codeMask, inCode, _CREDENTIAL_LINE, LexicalZone.Code);
		Assert.Equal(stringMask.IsCompletelyValid, codeMask.IsCompletelyValid);
	}

	// ---- The finding on the line the shells run: the level of its own, not Low ----

	[Theory]
	[InlineData("x=`cat <<EOF`\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=`cat <<'EOF'`\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=`cat <<-EOF`\n\tbody-line\n\tEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=`cat <<EOF`\nbody-line\nEOF\ncurl -u user:pass https://example.test\n")]
	[InlineData("y=$(echo `cat <<EOF`)\ncurl -u user:pass https://example.test\n")]
	[InlineData("z=\"$(echo `cat <<EOF`)\"\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=\"`cat <<EOF`\"\ncurl -u user:pass https://example.test\n")]
	[InlineData("x=`curl -u user:pass https://example.test`\n")]
	public void ACredentialAfterAOneLineSpanThatOpensAHereDocumentIsHighAndNotLow(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize("#!/bin/bash\n" + source);

		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _CREDENTIAL_CLASS && construct.Level == ConfidenceLevel.High);
	}

	[Theory]
	[InlineData("x=`cat <<EOF\ncurl -u user:pass https://example.test\nEOF\n`\n")]
	[InlineData("cat <<EOF\nx `cat <<NESTED`\ncurl -u user:pass https://example.test\nEOF\n")]
	public void ACredentialInsideTheBodyOfAHereDocumentThatSpansLinesOrThatHoldsABacktickStaysLow(string source)
	{
		ImmutableArray<RecognizedConstruct> constructs = new ShellRecognitionHarness().Recognize("#!/bin/bash\n" + source);

		Assert.DoesNotContain(constructs, construct => construct.PatternClassIdentifier == _CREDENTIAL_CLASS && construct.Level == ConfidenceLevel.High);
		Assert.Contains(constructs, construct => construct.PatternClassIdentifier == _CREDENTIAL_CLASS && construct.Level == ConfidenceLevel.Low);
	}

	private static ZoneMask ScanShipped(string source)
	{
		return new LexicalScanner(_shipped).Scan(source);
	}

	private static ZoneMask ScanTheReferenceWithoutSpans(string source)
	{
		return new LexicalScanner(ShellShapedProfile.BuildWithExecutedStrings()).Scan(source);
	}
}
