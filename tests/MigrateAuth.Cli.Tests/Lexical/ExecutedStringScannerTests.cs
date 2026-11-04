using System.Diagnostics;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE EXECUTED-STRING CONTEXT: a string, a here-document body, a backtick or a parameter word that a
/// shell runs as a command is read again as code, so a covered construct inside it is a code position. The tests run
/// under the test-local <see cref="ShellShapedProfile"/> with <see cref="ShellExecutedStringsDeclaration"/>; the control
/// of each positive case is the same text under the profile that does not declare the context, where the construct is
/// string body.
/// </summary>
public sealed class ExecutedStringScannerTests
{
	private const string _CONSTRUCT = "curl -u user:pass";
	private const string _HEREDOC_BODY = "curl -u user:pass host\n";
	private const int _ONE_MORE_THAN_THE_BOUND = LexicalEnginePolicy.MAXIMUM_EXECUTED_STRING_NESTING_DEPTH + 1;
	private const int _STATEMENT_LINES = 40000;
	private const int _NESTING_FAR_BEYOND_THE_BOUND = 14;
	private const int _LONG_BODY_STATEMENTS = 60000;
	private const int _BACKTICK_PAIRS = 60000;
	private const int _UNCLOSED_OPENERS = 60000;
	private const int _WORDS_BEYOND_THE_LOOK_BACK = 100;
	private const int _WORDS_WELL_INSIDE_THE_LOOK_BACK = 20;
	private const int _WORDS_BEFORE_THE_OPTION_AT_THE_CAP = LexicalEnginePolicy.EXECUTED_STRING_MAXIMUM_COMMAND_WORDS - 2;
	private const string _OPTION_WORD = " -x";
	private const int _UNRELATED_OPENINGS = 80000;

	private static readonly TimeSpan _boundedCeiling = TimeSpan.FromSeconds(10);

	// ---- strings that a command runs ----

	[Theory]
	[InlineData("eval \"curl -u user:pass host\"\n")]
	[InlineData("eval 'curl -u user:pass host'\n")]
	[InlineData("bash -c 'curl -u user:pass host'\n")]
	[InlineData("bash -c \"curl -u user:pass host\"\n")]
	[InlineData("sh -c \"curl -u user:pass host\"\n")]
	[InlineData("zsh -c 'curl -u user:pass host'\n")]
	[InlineData("ksh -c 'curl -u user:pass host'\n")]
	[InlineData("dash -c 'curl -u user:pass host'\n")]
	[InlineData("mksh -c 'curl -u user:pass host'\n")]
	[InlineData("bash -lc 'curl -u user:pass host'\n")]
	[InlineData("bash --norc -c 'curl -u user:pass host'\n")]
	[InlineData("bash -o pipefail -c 'curl -u user:pass host'\n")]
	[InlineData("sudo bash -c 'curl -u user:pass host'\n")]
	[InlineData("sudo -u deploy bash -c 'curl -u user:pass host'\n")]
	[InlineData("VAR=1 bash -c 'curl -u user:pass host'\n")]
	[InlineData("if true; then bash -c 'curl -u user:pass host'; fi\n")]
	[InlineData("a && bash -c 'curl -u user:pass host'\n")]
	[InlineData("x=$(bash -c 'curl -u user:pass host')\n")]
	[InlineData("bash \\\n  -c 'curl -u user:pass host'\n")]
	[InlineData("ssh host \"curl -u user:pass host\"\n")]
	[InlineData("ssh -p 22 -o StrictHostKeyChecking=no user@host 'curl -u user:pass host'\n")]
	[InlineData("ssh -t host sudo bash -c \"curl -u user:pass host\"\n")]
	[InlineData("su -c \"curl -u user:pass host\"\n")]
	[InlineData("su - svc -c 'curl -u user:pass host'\n")]
	[InlineData("sudo su -c 'curl -u user:pass host'\n")]
	public void AStringACommandRunsIsReadAsCodeAndTheConstructInsideItIsCode(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);
		ZoneMask control = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		ZoneAssertions.SpansZone(control, source, _CONSTRUCT, LexicalZone.StringBody);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheQuotesStayDelimitersWhenTheirBodyIsReadAsCode()
	{
		const string source = "bash -c 'curl -u user:pass host'\n";
		int opening = source.IndexOf('\'');
		int closing = source.LastIndexOf('\'');

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(opening));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(closing));
	}

	[Theory]
	[InlineData("echo \"curl -u user:pass host\"\n")]
	[InlineData("echo bash -c \"curl -u user:pass host\"\n")]
	[InlineData("bash script.sh -c \"curl -u user:pass host\"\n")]
	[InlineData("bash -c script \"curl -u user:pass host\"\n")]
	[InlineData("ssh host \"echo\" \"curl -u user:pass host\"\n")]
	[InlineData("ssh host curl \"curl -u user:pass host\"\n")]
	[InlineData("x=\"bash -c \"'curl -u user:pass host'\n")]
	[InlineData("su \"curl -u user:pass host\"\n")]
	[InlineData("bash -c\"curl -u user:pass host\"\n")]
	[InlineData("echo 'a;bash -c' 'curl -u user:pass host'\n")]
	[InlineData("printf '%s' \"curl -u user:pass host\"\n")]
	public void AStringNoCommandRunsStaysStringBody(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnExecutedStringInACommentIsStillAComment()
	{
		const string source = "# eval \"curl -u user:pass host\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Comment);
	}

	// ---- backticks and parameter words inside a double-quoted string ----

	[Theory]
	[InlineData("echo \"out: `curl -u user:pass host`\"\n")]
	[InlineData("echo \"${V:-curl -u user:pass host}\"\n")]
	[InlineData("echo \"${V:=curl -u user:pass host}\"\n")]
	[InlineData("echo \"${V+curl -u user:pass host}\"\n")]
	[InlineData("echo \"`echo \\`curl -u user:pass host\\``\"\n")]
	[InlineData("echo \"a `date` b ${V:-curl -u user:pass host} c\"\n")]
	public void ABacktickOrAParameterWordInsideADoubleQuotedStringIsReadAsCode(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);
		ZoneMask control = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		ZoneAssertions.SpansZone(control, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheTextAroundABacktickAndAParameterWordStaysStringBody()
	{
		const string source = "echo \"out: `date` and ${V:-curl -u user:pass host} done\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "out: ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " and ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " done", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "date", LexicalZone.Code);
	}

	[Theory]
	[InlineData("echo \"${V}: curl -u user:pass host\"\n")]
	[InlineData("echo \"${V:-}curl -u user:pass host\"\n")]
	[InlineData("echo \"a `curl -u user:pass host\"\nnext\n")]
	[InlineData("echo \"${#V} curl -u user:pass host\"\n")]
	public void AnInlineConstructWithNoWordOrNoClosingLeavesTheStringAsItWas(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	[Fact]
	public void ADoubleQuoteInsideBackticksNeitherEndsTheStringNorPairsWithALaterOne()
	{
		const string source = "echo \"a `echo \"b\"` c\" tail\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, " c", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- a here-document fed to a shell ----

	[Theory]
	[InlineData("bash <<EOF\n")]
	[InlineData("sh <<EOF\n")]
	[InlineData("zsh <<EOF\n")]
	[InlineData("ksh <<EOF\n")]
	[InlineData("dash <<EOF\n")]
	[InlineData("mksh <<EOF\n")]
	[InlineData("bash -s <<EOF\n")]
	[InlineData("sh -s <<EOF\n")]
	[InlineData("zsh -s <<EOF\n")]
	[InlineData("ksh -s <<EOF\n")]
	[InlineData("dash -s <<EOF\n")]
	[InlineData("mksh -s <<EOF\n")]
	[InlineData("bash -s -- one two <<EOF\n")]
	[InlineData("bash - <<EOF\n")]
	[InlineData("sudo bash <<EOF\n")]
	[InlineData("sudo -u deploy bash <<EOF\n")]
	[InlineData("ssh host <<EOF\n")]
	[InlineData("ssh -p 22 -i key user@host <<EOF\n")]
	[InlineData("ssh host bash -s <<EOF\n")]
	[InlineData("ssh host sudo -u deploy bash -s <<EOF\n")]
	[InlineData("bash <<'EOF'\n")]
	[InlineData("bash <<\"EOF\"\n")]
	[InlineData("bash > log.txt <<EOF\n")]
	[InlineData("bash 2>&1 <<EOF\n")]
	[InlineData("if true; then bash <<EOF\n")]
	public void AHereDocumentFedToAShellIsReadAsCode(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);
		ZoneMask control = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		ZoneAssertions.SpansZone(control, source, _CONSTRUCT, LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "after=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ATabStrippingHereDocumentFedToAShellIsReadAsCode()
	{
		const string source = "bash <<-EOF\n\tcurl -u user:pass host\n\tEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AHereDocumentFedToAShellWithARedirectionAfterTheOpeningIsReadAsCode()
	{
		const string source = "bash <<EOF > log.txt\ncurl -u user:pass host\nEOF\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	[Theory]
	[InlineData("cat <<EOF\n")]
	[InlineData("cat > notes.txt <<EOF\n")]
	[InlineData("cat >> out.log <<EOF\n")]
	[InlineData("cat <<EOF > notes.txt\n")]
	[InlineData("python3 <<EOF\n")]
	[InlineData("mysql -u root <<EOF\n")]
	[InlineData("bash script.sh <<EOF\n")]
	[InlineData("bash -c 'cat' <<EOF\n")]
	[InlineData("ssh host cat <<EOF\n")]
	[InlineData("su - user <<EOF\n")]
	[InlineData("sudo cat <<EOF\n")]
	[InlineData("bash -x script.sh <<EOF\n")]
	[InlineData("echo bash <<EOF\n")]
	public void AHereDocumentFedToAnythingElseStaysStringBody(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	// ---- a script written by a script ----

	[Theory]
	[InlineData("cat > run.sh <<EOF\n")]
	[InlineData("cat >> run.bash <<EOF\n")]
	[InlineData("cat >run.zsh <<EOF\n")]
	[InlineData("cat <<EOF > run.sh\n")]
	[InlineData("cat <<EOF >run.ksh\n")]
	[InlineData("cat > \"run.sh\" <<EOF\n")]
	[InlineData("cat > /tmp/deploy/run.dash <<EOF\n")]
	[InlineData("cat > RUN.SH <<EOF\n")]
	[InlineData("cat <<'EOF' > run.sh\n")]
	public void AHereDocumentWrittenToAFileWithAShellExtensionIsReadAsCode(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat > notes.txt <<EOF\n#!/bin/sh\n")]
	[InlineData("cat > notes.txt <<EOF\n#!/bin/bash -e\n")]
	[InlineData("cat > notes.txt <<EOF\n#!/usr/bin/env bash\n")]
	[InlineData("cat > notes.txt <<EOF\n#!/usr/bin/env -S bash -e\n")]
	[InlineData("cat > notes.txt <<EOF\n#! /bin/zsh\n")]
	[InlineData("cat <<EOF\n#!/bin/dash\n")]
	[InlineData("cat > data <<'EOF'\n#!/bin/ksh\n")]
	public void AHereDocumentWhoseFirstLineIsAShellShebangIsReadAsCode(string openingLineAndShebang)
	{
		string source = openingLineAndShebang + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "#!", LexicalZone.Comment);
	}

	[Theory]
	[InlineData("cat > notes.txt <<EOF\n#!/usr/bin/env python3\n")]
	[InlineData("cat > notes.txt <<EOF\n#!/usr/bin/perl\n")]
	[InlineData("cat > notes.txt <<EOF\n\n#!/bin/sh\n")]
	[InlineData("cat > notes.txt <<EOF\n # !/bin/sh\n")]
	[InlineData("cat > notes.txt <<EOF\n#!\n")]
	[InlineData("cat > notes.txt <<EOF\nnot a shebang\n")]
	[InlineData("cat > notes.shx <<EOF\n")]
	[InlineData("cat > sh <<EOF\n")]
	[InlineData("cat > .sh <<EOF\n")]
	public void AHereDocumentWithNeitherAScriptTargetNorAShellShebangStaysStringBody(string openingLineAndFirstLine)
	{
		string source = openingLineAndFirstLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	// ---- redirection characters inside the arguments of the opener line ----

	[Theory]
	[InlineData("cat <<EOF \"a > run.sh\"\n")]
	[InlineData("cat <<EOF 'a > run.sh'\n")]
	[InlineData("cat <<EOF a\\ \\>\\ run.sh\n")]
	[InlineData("cat <<EOF \">\" run.sh\n")]
	[InlineData("cat <<EOF \"a \\\" > run.sh\"\n")]
	[InlineData("cat <<EOF '>' > notes.txt\n")]
	public void ARedirectionCharacterInsideAQuotedOrEscapedArgumentOfTheOpenerLineIsNoRedirection(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	[Theory]
	[InlineData("cat <<EOF > run.sh\n")]
	[InlineData("cat <<EOF \"a\" > run.sh\n")]
	[InlineData("cat <<EOF 'a > b' >run.sh\n")]
	[InlineData("cat <<EOF > \"my file.sh\"\n")]
	[InlineData("cat <<EOF > run.sh; echo done\n")]
	public void ARealUnquotedTargetAfterTheOpenerStillReadsTheBodyAsCode(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	// ---- an escaped line feed is removed by the shell, it is not a blank ----

	[Theory]
	[InlineData("sudo\\\nbash -c \"curl -u user:pass host\"\n")]
	[InlineData("bash -c\\\n\"curl -u user:pass host\"\n")]
	[InlineData("ba\\\nsh -c 'curl -u user:pass host'\n")]
	[InlineData("ssh host\\\n\"curl -u user:pass host\"\n")]
	public void AnEscapedLineFeedBetweenCharactersJoinsThemIntoOneWordAndReadsNothing(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	[Theory]
	[InlineData("ssh host \\\n\"curl -u user:pass host\"\n")]
	[InlineData("bash\\\n -c 'curl -u user:pass host'\n")]
	[InlineData("bash \\\n-c 'curl -u user:pass host'\n")]
	[InlineData("bash -c \\\n'curl -u user:pass host'\n")]
	public void AnEscapedLineFeedBesideABlankIsRemovedAndTheWordsStaySeparate(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	// ---- an escaped character is never a blank or a separator ----

	[Theory]
	[InlineData("ssh host\\ \"curl -u user:pass host\"\n")]
	[InlineData("eval\\ \"curl -u user:pass host\"\n")]
	[InlineData("bash -c\\ \"curl -u user:pass host\"\n")]
	[InlineData("bash -c\\ 'curl -u user:pass host'\n")]
	[InlineData("su -c\\ \"curl -u user:pass host\"\n")]
	[InlineData("echo x\\; bash -c 'curl -u user:pass host'\n")]
	[InlineData("echo x\\| bash -c 'curl -u user:pass host'\n")]
	[InlineData("echo x\\& bash -c 'curl -u user:pass host'\n")]
	[InlineData("echo x\\( bash -c 'curl -u user:pass host'\n")]
	public void AnEscapedBlankOrSeparatorBeforeAStringReadsNothing(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<EOF a\\ > run.sh\n")]
	[InlineData("cat <<EOF a\\> run.sh\n")]
	[InlineData("cat <<EOF \\| bash\n")]
	[InlineData("cat <<EOF a\\| bash\n")]
	public void AnEscapedBlankOrOperatorAfterTheOpenerIsPartOfAWordAndReadsNothing(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	// ---- the end-of-options word ends option parsing ----

	[Theory]
	[InlineData("bash -- -c 'curl -u user:pass host'\n")]
	[InlineData("sudo -- bash -- -c 'curl -u user:pass host'\n")]
	[InlineData("su -- -c 'curl -u user:pass host'\n")]
	[InlineData("bash -o pipefail -- -c 'curl -u user:pass host'\n")]
	public void AStringOptionAfterTheEndOfOptionsIsAnArgumentAndReadsNothing(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("sudo -- bash -c 'curl -u user:pass host'\n")]
	[InlineData("sudo -u deploy -- bash -c 'curl -u user:pass host'\n")]
	[InlineData("ssh -- host 'curl -u user:pass host'\n")]
	[InlineData("ssh -p 22 -- host 'curl -u user:pass host'\n")]
	public void TheEndOfOptionsBeforeTheCommandOrTheHostStillReadsTheString(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	[Theory]
	[InlineData("bash -s -- -c <<EOF\n")]
	[InlineData("sudo -- bash <<EOF\n")]
	[InlineData("ssh -- host <<EOF\n")]
	public void AHereDocumentFedToAShellAfterTheEndOfOptionsIsReadAsCode(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	[Theory]
	[InlineData("bash -- script.sh <<EOF\n")]
	[InlineData("bash -- -s <<EOF\n")]
	public void AHereDocumentFedToAScriptFileAfterTheEndOfOptionsStaysStringBody(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	// ---- quotes inside the word of a parameter expansion ----

	[Fact]
	public void AnUnbalancedQuoteInsideAParameterWordMakesItUnreadableAndTheStringKeepsItsZones()
	{
		const string source = "echo \"${V:-\"}\" ; curl -u user:pass host\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);
		ZoneMask control = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.Equal(control.Zones.ToArray(), mask.Zones.ToArray());
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ABalancedQuoteInsideAParameterWordIsPartOfTheWordAndTheStringEndsAfterIt()
	{
		const string source = "echo \"${V:-\"a b\"}\" tail\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "a b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AClosingBraceInsideAQuotedRunOfAParameterWordDoesNotCloseIt()
	{
		const string source = "echo \"${V:-'}' curl -u user:pass host}\" tail\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
	}

	[Fact]
	public void ANestedParameterWordInsideAParameterWordIsReadAsCode()
	{
		const string source = "echo \"${V:-${W:-curl -u user:pass host}}\" tail\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
	}

	// ---- a here-document piped into a shell on its own line ----

	[Theory]
	[InlineData("cat <<EOF | bash\n")]
	[InlineData("cat <<EOF | sudo bash\n")]
	[InlineData("cat <<EOF | sudo -u deploy bash -s\n")]
	[InlineData("cat <<EOF | sh -s\n")]
	[InlineData("cat <<EOF | ssh host\n")]
	[InlineData("cat <<EOF | ssh -p 22 host bash -s\n")]
	[InlineData("cat <<EOF | bash -s -- one two\n")]
	[InlineData("cat <<EOF | bash -\n")]
	[InlineData("cat <<EOF | tr a b | bash\n")]
	[InlineData("cat <<EOF |& bash\n")]
	[InlineData("cat <<EOF |bash\n")]
	[InlineData("cat <<EOF | bash > log.txt\n")]
	[InlineData("cat <<'EOF' | bash\n")]
	[InlineData("cat > data.txt <<EOF | bash\n")]
	public void AHereDocumentPipedIntoAShellOnTheOpenerLineIsReadAsCode(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<EOF | grep x\n")]
	[InlineData("cat <<EOF | tee run.sh\n")]
	[InlineData("cat <<EOF | bash -c 'cat'\n")]
	[InlineData("cat <<EOF | bash script.sh\n")]
	[InlineData("cat <<EOF | bash | grep x\n")]
	[InlineData("cat <<EOF || bash\n")]
	[InlineData("cat <<EOF ; bash\n")]
	[InlineData("cat <<EOF && bash\n")]
	[InlineData("cat <<EOF # | bash\n")]
	[InlineData("cat <<EOF '| bash'\n")]
	public void AHereDocumentNotPipedIntoAShellStaysStringBody(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	[Fact]
	public void AContinuedPipeToAShellOnTheOpenerLineIsAKnownLimitAndStaysStringBody()
	{
		const string source = "cat <<EOF | \\\nbash\ncurl -u user:pass host\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	// ---- a command given by a path is the command it names ----

	[Theory]
	[InlineData("/bin/sh -c 'curl -u user:pass host'\n")]
	[InlineData("/usr/bin/sudo bash -c 'curl -u user:pass host'\n")]
	[InlineData("sudo /bin/bash -c 'curl -u user:pass host'\n")]
	[InlineData("/usr/bin/sudo -u deploy /bin/bash -c 'curl -u user:pass host'\n")]
	[InlineData("/usr/bin/ssh host \"curl -u user:pass host\"\n")]
	[InlineData("/usr/bin/ssh host /bin/sh -c 'curl -u user:pass host'\n")]
	[InlineData("/bin/su -c 'curl -u user:pass host'\n")]
	[InlineData("./bash -c 'curl -u user:pass host'\n")]
	public void ACommandGivenByAPathIsReadAsTheCommandItNames(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	[Theory]
	[InlineData("/bin/bash <<EOF\n")]
	[InlineData("/usr/bin/sudo /bin/sh -s <<EOF\n")]
	[InlineData("/usr/bin/ssh host <<EOF\n")]
	[InlineData("cat <<EOF | /bin/bash\n")]
	[InlineData("cat <<EOF | /usr/bin/sudo /bin/bash -s\n")]
	public void AHereDocumentFedToACommandGivenByAPathIsReadAsCode(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	[Theory]
	[InlineData("/usr/bin/env bash -c 'curl -u user:pass host'\n")]
	[InlineData("/bin/ -c 'curl -u user:pass host'\n")]
	[InlineData("/bin/bash/ -c 'curl -u user:pass host'\n")]
	[InlineData("/bin/cat -c 'curl -u user:pass host'\n")]
	[InlineData("/bin/echo bash -c 'curl -u user:pass host'\n")]
	[InlineData("/usr/bin/sudo /bin/cat -c 'curl -u user:pass host'\n")]
	[InlineData("/bin/bash script.sh -c 'curl -u user:pass host'\n")]
	[InlineData("/usr/bin/ssh host /bin/echo 'curl -u user:pass host'\n")]
	public void AWordEndingInTheSeparatorOrNamingAnotherCommandStaysUnread(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	[Theory]
	[InlineData("/bin/cat <<EOF\n")]
	[InlineData("/bin/ <<EOF\n")]
	[InlineData("/bin/bash/ <<EOF\n")]
	[InlineData("/bin/cat <<EOF | /bin/grep x\n")]
	public void AHereDocumentFedToAPathThatIsNotAShellStaysStringBody(string openingLine)
	{
		string source = openingLine + _HEREDOC_BODY + "EOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	// ---- inline escapes and single quotes follow bash ----

	[Fact]
	public void AnEscapedDoubleQuoteInsideBackticksIsRemovedSoItsQuotedRunHidesTheCommentCharacter()
	{
		const string source = "echo \"`echo \\\"x #\\\" ; curl -u user:pass host`\" tail\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "x #", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ABackslashNewlineInsideASingleQuotedBodyStaysInTheBody()
	{
		const string source = "bash -c 'echo a\\\nb; curl -u user:pass host'\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "echo a", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "\\\nb", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "after=1", LexicalZone.Code);
	}

	[Fact]
	public void APipeOnALaterLineThanTheOpeningIsNotRead()
	{
		const string source = "cat <<EOF\ncurl -u user:pass host\nEOF\n| bash\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
	}

	// ---- the look-back and word bounds ----

	[Fact]
	public void ACommandWhoseWordsBeginBeyondTheLookBackStaysAStringZoneWithNoAnomaly()
	{
		string source = "bash" + string.Concat(Enumerable.Repeat(_OPTION_WORD, _WORDS_BEYOND_THE_LOOK_BACK)) + " -c 'curl -u user:pass host'\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		Assert.True(source.IndexOf('\'') > LexicalEnginePolicy.EXECUTED_STRING_LOOKBACK_CHARACTERS);
		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ACommandWithinTheLookBackIsRead()
	{
		string source = "bash" + string.Concat(Enumerable.Repeat(_OPTION_WORD, _WORDS_WELL_INSIDE_THE_LOOK_BACK)) + " -c 'curl -u user:pass host'\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	[Fact]
	public void ACommandWithMoreWordsThanTheCapStaysAStringZoneWithNoAnomaly()
	{
		string source = "bash" + string.Concat(Enumerable.Repeat(_OPTION_WORD, _WORDS_BEFORE_THE_OPTION_AT_THE_CAP + 1)) + " -c 'curl -u user:pass host'\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		Assert.True(source.IndexOf('\'') < LexicalEnginePolicy.EXECUTED_STRING_LOOKBACK_CHARACTERS);
		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ACommandWithExactlyTheCapOfWordsIsRead()
	{
		string source = "bash" + string.Concat(Enumerable.Repeat(_OPTION_WORD, _WORDS_BEFORE_THE_OPTION_AT_THE_CAP)) + " -c 'curl -u user:pass host'\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	// ---- the body is the command the shell runs ----

	[Fact]
	public void AnEscapedQuoteInADoubleQuotedBodyIsRemovedAndTheStringItOpensIsRead()
	{
		const string source = "bash -c \"curl -u \\\"user:pass\\\" host\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "curl -u ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "\\", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, source, "user:pass", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " host", LexicalZone.Code);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf("\\\"user", StringComparison.Ordinal) + 1));
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnEscapedDollarInADoubleQuotedBodyIsRemovedAndTheVariableIsCode()
	{
		const string source = "bash -c \"curl -u \\$USER:pass host\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "curl -u ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "$USER:pass host", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "\\", LexicalZone.StringBody, occurrence: 0);
	}

	[Fact]
	public void ABackslashThatEscapesNothingInADoubleQuotedBodyStaysAndIsCode()
	{
		const string source = "bash -c \"curl -u a\\bc:pass host\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "curl -u ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " host", LexicalZone.Code);
	}

	[Fact]
	public void ASingleQuotedBodyIsLiteralAndItsBackslashesAreNotRemoved()
	{
		const string source = "bash -c 'curl -u a\\\"b:pass host'\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "curl -u a", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "after=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ABackslashNewlinePairInADoubleQuotedBodyIsRemovedAndKeepsTheStringBodyZone()
	{
		const string source = "bash -c \"curl -u \\\nuser:pass host\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "curl -u ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "user:pass host", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "\\\n", LexicalZone.StringBody);
	}

	[Fact]
	public void EveryPositionOfAnEscapedBodyMapsBackToTheCharacterThatStandsThere()
	{
		const string source = "bash -c \"echo \\\"a\\\" \\$b `x` ; curl -u \\\\ok:pass host\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);
		int construct = source.IndexOf("curl", StringComparison.Ordinal);

		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
		Assert.All(Enumerable.Range(construct, "curl -u ".Length), position => Assert.Equal(LexicalZone.Code, mask.ZoneAt(position)));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf('"')));
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.LastIndexOf('"')));
	}

	[Fact]
	public void AHereDocumentBodyThatHoldsAStringAndACommentReadsThemAsInCode()
	{
		const string source = "bash <<EOF\necho 'a;b' # tail\ncurl -u user:pass host\nEOF\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "a;b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "# tail", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	[Fact]
	public void AnUnquotedHereDocumentBodyRemovesTheBackslashBeforeADollar()
	{
		const string source = "bash <<EOF\ncurl -u \\$U:pass host\nEOF\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "$U:pass host", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "\\", LexicalZone.StringBody, occurrence: 0);
	}

	// ---- nesting ----

	[Fact]
	public void AStringInsideAStringThatRunsAsACommandIsReadAsCodeAtEachLevel()
	{
		const string source = "ssh host \"bash -c 'curl -u user:pass host'\"\n";

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, "bash -c ", LexicalZone.Code, occurrence: 0);
		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
	}

	[Fact]
	public void NestedStringsAreReadUpToTheNestingBound()
	{
		string source = BuildNestedStrings(LexicalEnginePolicy.MAXIMUM_EXECUTED_STRING_NESTING_DEPTH);

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
	}

	[Fact]
	public void AStringNestedBeyondTheBoundStaysAStringZoneWithNoAnomaly()
	{
		string source = BuildNestedStrings(_ONE_MORE_THAN_THE_BOUND);

		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("bash", StringComparison.Ordinal) + 1));
	}

	// ---- what cannot be read soundly stays a string zone, with no anomaly ----

	[Theory]
	[InlineData("eval \"echo 'curl -u user:pass host\"\n")]
	[InlineData("eval \"echo \\\"curl -u user:pass host\"\n")]
	[InlineData("eval \"cat <<EOF; curl -u user:pass host\"\n")]
	[InlineData("bash -c 'cat <<EOF\ncurl -u user:pass host\n'\n")]
	public void ABodyThatCannotBeReadSoundlyStaysAStringZoneAndRecordsNoAnomaly(string source)
	{
		ZoneMask mask = ShellShapedProfile.ScanWithExecutedStrings(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ABodyThatTripsTheLexicalBudgetAbandonsTheFileAtTheOutermostOpening()
	{
		const string source = "x=1\neval \"cat <<EOF\ncurl -u user:pass host\nEOF\n\"\nafter=1\n";
		LexicalScanner scanner = new(ShellShapedProfile.BuildWithExecutedStrings(), new AdvancingTimeProvider(LexicalEnginePolicy.LEXICAL_BUDGET));

		ZoneMask mask = scanner.Scan(source);

		Assert.Equal(LexicalAnomalyReason.LexicalBudgetExceeded, mask.AnomalyReason);
		Assert.True(mask.ValidThroughPosition <= source.IndexOf("eval", StringComparison.Ordinal) + "eval ".Length);
		Assert.False(mask.IsAnalyzableAt(source.Length - 1));
	}

	[Fact]
	public void AProfileThatDoesNotDeclareTheContextReadsEveryContextAsItDidBefore()
	{
		const string source = "eval \"curl -u user:pass host\"\nbash <<EOF\ncurl -u user:pass host\nEOF\necho \"`curl -u user:pass host`\"\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);
		int[] constructs = [.. Enumerable.Range(0, mask.Length).Where(position => source.AsSpan(position).StartsWith(_CONSTRUCT.AsSpan()))];

		Assert.Equal(3, constructs.Length);
		Assert.All(constructs, position => Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(position)));
	}

	// ---- bounds ----

	[Fact]
	public void ManyExecutedStringsOnManyLinesAreReadInBoundedTime()
	{
		StringBuilder builder = new();

		for (int line = 0; line < _STATEMENT_LINES; line++)
		{
			builder.Append("eval \"curl -u user:pass host").Append(line).Append("\"\n");
		}

		string source = builder.ToString();
		ZoneMask mask = AssertBounded(source);

		ZoneAssertions.SpansZone(mask, source, "curl -u user:pass host0", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "curl -u user:pass host" + (_STATEMENT_LINES - 1), LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void OneVeryLongExecutedBodyIsReadInBoundedTime()
	{
		StringBuilder builder = new("bash -c '");

		for (int statement = 0; statement < _LONG_BODY_STATEMENTS; statement++)
		{
			builder.Append("curl -u user:pass host; ");
		}

		builder.Append("'\n");

		string source = builder.ToString();
		ZoneMask mask = AssertBounded(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.LastIndexOf("host", StringComparison.Ordinal)));
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void StringsNestedFarBeyondTheBoundAreReadInBoundedTimeAndTheInnermostStaysAString()
	{
		string source = BuildNestedStrings(_NESTING_FAR_BEYOND_THE_BOUND);

		ZoneMask mask = AssertBounded(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.StringBody);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ManyOpeningsWithACommandPositionBeforeEachAreReadInBoundedTimeAndReadNothing()
	{
		StringBuilder builder = new();

		for (int line = 0; line < _UNRELATED_OPENINGS; line++)
		{
			builder.Append("a; '' \"\" '' \"\" '' \"\"\n");
		}

		ZoneMask mask = AssertBounded(builder.ToString());

		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
	}

	[Fact]
	public void OneLineOfOpeningsWithNoCommandPositionIsReadInBoundedTimeAndReadNothing()
	{
		ZoneMask mask = AssertBounded("a" + string.Concat(Enumerable.Repeat(" ''", _UNRELATED_OPENINGS)) + "\n");

		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.None, mask.AnomalyReason);
	}

	[Fact]
	public void ManyBackticksInsideOneDoubleQuotedStringAreReadInBoundedTime()
	{
		StringBuilder builder = new("echo \"");

		for (int pair = 0; pair < _BACKTICK_PAIRS; pair++)
		{
			builder.Append("`curl -u user:pass host` ");
		}

		builder.Append("\"\n");

		string source = builder.ToString();
		ZoneMask mask = AssertBounded(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.LastIndexOf("host", StringComparison.Ordinal)));
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ABacktickThatNeverClosesIsSearchedForInBoundedTimeAndTheStringKeepsItsZones()
	{
		string source = "echo \"a`" + string.Concat(Enumerable.Repeat("b \\` ", _BACKTICK_PAIRS)) + "\"\n";

		ZoneMask mask = AssertBounded(source);

		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(source.IndexOf('b')));
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ManyParameterWordsInsideOneDoubleQuotedStringAreReadInBoundedTime()
	{
		StringBuilder builder = new("echo \"");

		for (int pair = 0; pair < _BACKTICK_PAIRS; pair++)
		{
			builder.Append("${V:-curl -u user:pass host} ");
		}

		builder.Append("\"\n");

		string source = builder.ToString();
		ZoneMask mask = AssertBounded(source);

		ZoneAssertions.SpansZone(mask, source, _CONSTRUCT, LexicalZone.Code);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.LastIndexOf("host", StringComparison.Ordinal)));
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ManyParameterOpenersThatNeverCloseAreSearchedForInBoundedTimeAndTheStringKeepsItsZones()
	{
		string source = "echo \"" + string.Concat(Enumerable.Repeat("${V:-a ", _UNCLOSED_OPENERS)) + "\"\n";

		ZoneMask mask = AssertBounded(source);

		Assert.Equal(LexicalZone.StringBody, mask.ZoneAt(source.IndexOf("a ", StringComparison.Ordinal)));
		Assert.True(mask.IsCompletelyValid);
	}

	private static ZoneMask AssertBounded(string source)
	{
		LexicalScanner scanner = new(ShellShapedProfile.BuildWithExecutedStrings());
		Stopwatch stopwatch = Stopwatch.StartNew();

		ZoneMask mask = scanner.Scan(source);

		Assert.True(stopwatch.Elapsed < _boundedCeiling, $"the scan took {stopwatch.Elapsed.TotalSeconds:F1} s");
		Assert.Equal(source.Length, mask.Length);

		return mask;
	}

	/// <summary>
	/// The text of <paramref name="strings"/> nested strings, the innermost holding the construct: each level is
	/// <c>bash -c "…"</c> around the one inside, whose backslashes and double quotes are escaped as a shell requires,
	/// so the body of each level is the command text of the level inside it with its escapes removed.
	/// </summary>
	private static string BuildNestedStrings(int strings)
	{
		string command = _CONSTRUCT + " host";

		for (int level = 0; level < strings; level++)
		{
			command = "bash -c \"" + command.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
		}

		return command + "\n";
	}
}
