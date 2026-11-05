using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// EVERY ROW OF THE SHELL DECLARATION TABLE, RUN ON REAL-SHAPED TEXT. The text of each case is invented and
/// generic, written as shell scripts are written, and held inline: nothing here reads any file from a repository. The profile is
/// test-local (<see cref="ShellShapedProfile"/>), so a case that fails is
/// a finding about the declaration or about the scanner, never about the fixture. Each case names, in its
/// summary, the row it runs.
/// </summary>
public sealed partial class ShellRealShapedTextTests
{
	private const string _FOLLOWING_STATEMENT = "after=1";

	[GeneratedRegex(
		@"^#![ \t]*(?:[^\s/]*/)*(?:env[ \t]+(?:-\S*[ \t]+)*)?(?:bash|sh|zsh|ksh)(?:[ \t]|$)",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
	private static partial Regex FirstLineExpression();

	/// <summary>Row: the <c>#</c> comment. A hash opens a comment after a blank, a line feed, <c>; &amp; | ( )</c> and at the start of the text; the <c>#!</c> line is one.</summary>
	[Fact]
	public void AHashOpensACommentAtTheStartOfAWordAndTheShebangLineIsAComment()
	{
		const string source =
			"#!/bin/sh\n"
			+ "# leading note\n"
			+ "total=1 # trailing note\n"
			+ "echo a;# after semicolon\n"
			+ "echo b&# after ampersand\n"
			+ "echo c |# after pipe\n"
			+ "cat\n"
			+ "( # after open parenthesis\n"
			+ "echo d )\n"
			+ "case x in x) # after close parenthesis\n"
			+ "echo e;; esac\n"
			+ "end=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "#!/bin/sh", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "# leading note", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "# trailing note", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "# after semicolon", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "# after ampersand", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "# after pipe", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "# after open parenthesis", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "# after close parenthesis", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "total=1 ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "echo d )", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "echo e;; esac", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "end=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: the <c>#</c> comment, negative side. <c>${#v}</c>, <c>$#</c>, <c>a#b</c>, <c>16#ff</c>, <c>${v#p}</c>, <c>${v##p}</c>, <c>${v%p}</c> and an escaped hash do not open one.</summary>
	[Fact]
	public void AHashInsideAWordOrAnExpansionIsCode()
	{
		const string source =
			"v=abc\n"
			+ "echo a#b ${#v} $# x#y $((16#ff)) ${v#a} ${v##a*} ${v%c} \\#escaped # real note\n"
			+ "end=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "echo a#b ${#v} $# x#y $((16#ff)) ${v#a} ${v##a*} ${v%c} \\#escaped ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "# real note", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "end=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: the <c>#</c> comment. A hash inside any quote form is string body, never a comment.</summary>
	[Fact]
	public void AHashInsideAnyQuoteFormIsStringBody()
	{
		const string source = "echo 'a # b' \"c # d\" $'e # f' tail # note\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "a # b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "c # d", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "e # f", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "# note", LexicalZone.Comment);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: the zsh <c>(#i)</c> glob qualifier. A hash directly after an open parenthesis that closes in the same word is a qualifier, not a comment, so the rest of the line is code; the profile carries no extended-glob switch, so it reads the qualifier whether or not zsh has the option on.</summary>
	[Theory]
	[InlineData("echo (#i)abc.txt tail\nend=1\n", "echo (#i)abc.txt tail\n")]
	[InlineData("[[ ABC == (#i)abc ]] && echo hit\nend=1\n", "[[ ABC == (#i)abc ]] && echo hit\n")]
	[InlineData("echo *.txt(#qN) tail\nend=1\n", "echo *.txt(#qN) tail\n")]
	[InlineData("echo (#a1)abc (#s)abc(#e) tail\nend=1\n", "echo (#a1)abc (#s)abc(#e) tail\n")]
	public void TheZshGlobQualifierIsCodeAndTheRestOfTheLineIsCode(string source, string expectedCode)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedCode, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "end=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: a hash after an open parenthesis that is not a glob qualifier still opens a comment: at command start in a subshell, with a blank after the hash, or with no closing parenthesis in the word.</summary>
	[Theory]
	[InlineData("( # note\necho sub )\nend=1\n", "# note")]
	[InlineData("(#note\necho sub\n)\nend=1\n", "#note")]
	[InlineData("echo ( # note\nend=1\n", "# note")]
	[InlineData("a=( # list\nx y )\nend=1\n", "# list")]
	public void AHashAfterAnOpenParenthesisThatIsNotAGlobQualifierOpensAComment(string source, string expectedComment)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedComment, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "end=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: the hash after a backslash-escaped blank or metacharacter. The shell keeps it in the word, so it opens no comment and the text through the hash's line is code. A backslash-line-feed after a word joins the next line into the same word.</summary>
	[Theory]
	[InlineData("echo a\\ #b\nend=1\n", "echo a\\ #b\n")]
	[InlineData("echo a\\\t#b\nend=1\n", "echo a\\\t#b\n")]
	[InlineData("echo a\\\\\\ #b\nend=1\n", "echo a\\\\\\ #b\n")]
	[InlineData("echo a\\;#b\nend=1\n", "echo a\\;#b\n")]
	[InlineData("echo a\\&#b\nend=1\n", "echo a\\&#b\n")]
	[InlineData("echo a\\|#b\nend=1\n", "echo a\\|#b\n")]
	[InlineData("echo a\\(\\)#b\nend=1\n", "echo a\\(\\)#b\n")]
	[InlineData("echo a\\<#b\\>#c\nend=1\n", "echo a\\<#b\\>#c\n")]
	[InlineData("echo a\\\n#b\nend=1\n", "echo a\\\n#b\n")]
	public void AHashAfterAnEscapedBlankOrMetacharacterStaysInTheWord(string source, string expectedCode)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedCode, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "end=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: the hash after a blank that an even run of backslashes leaves unescaped, or after a blank before a backslash-line-feed, opens a comment, as a shell reads it.</summary>
	[Theory]
	[InlineData("echo a\\\\ #b\nend=1\n", "#b")]
	[InlineData("echo a \\\n#b\nend=1\n", "#b")]
	[InlineData("echo a \\\n #b\nend=1\n", "#b")]
	[InlineData("echo a\\\\\n#b\nend=1\n", "#b")]
	public void AHashAfterAnUnescapedBlankStillOpensAComment(string source, string expectedComment)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedComment, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "end=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>'…'</c>. No escape character at all: a backslash before the closing quote closes it; newlines are content.</summary>
	[Fact]
	public void ASingleQuotedStringHasNoEscapeAndMayHoldNewlines()
	{
		const string source = "echo 'a\\' b\nmsg='line one\nline two'\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "a\\", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " b\nmsg=", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "line one\nline two", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>'…'</c>, the predicate. A quote after an odd run of backslashes is escaped text; after an even run it opens.</summary>
	[Fact]
	public void AQuoteAfterAnOddRunOfBackslashesOpensNothingAndAfterAnEvenRunOpens()
	{
		const string source = "echo it\\'s fine\necho \\\\'a b' tail\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "echo it\\'s fine", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "a b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>'…'</c> and <c>"…"</c>. A doubled quote is two adjacent literals, and a dollar sign before a double quote is code followed by an ordinary double-quoted string.</summary>
	[Fact]
	public void ADoubledQuoteIsTwoAdjacentLiteralsAndADollarBeforeADoubleQuoteIsCode()
	{
		const string source = "echo 'a''b' $\"c d\" tail\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "a", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "b", LexicalZone.StringBody);
		AssertZoneAt(mask, source, "$\"c d\"", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "c d", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>$'…'</c>. The ANSI-C prefix restores the backslash escape inside single quotes.</summary>
	[Fact]
	public void AnAnsiCQuotedStringHonoursTheBackslashEscape()
	{
		const string source = "echo $'it\\'s ok' tail\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "$'", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "it\\'s ok", LexicalZone.StringBody);
		Assert.Equal(LexicalZone.Delimiter, mask.ZoneAt(source.IndexOf("' tail", StringComparison.Ordinal)));
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>"…"</c>. Backslash escapes the quote, a <c>$( )</c> hole nests and holds code, <c>$(( ))</c> nests inside it, and <c>${…}</c> and backticks stay body (declared).</summary>
	[Fact]
	public void ADoubleQuotedStringEscapesNestsSubstitutionAndKeepsOtherExpansionsAsBody()
	{
		const string source =
			"echo \"a \\\"quoted\\\" word\" tail\n"
			+ "echo \"$(echo \"x y\")\" \"$((1 + (2 * 3)))\" \"${v:-#not} `echo hi`\" end\n"
			+ "msg=\"first\nsecond\"\n"
			+ "after=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "a \\\"quoted\\\" word", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		AssertZoneAt(mask, source, "echo \"x y\"", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "x y", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "1 + (2 * 3)", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "${v:-#not} `echo hi`", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " end\nmsg=", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "first\nsecond", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: backticks in code. Not a quote rule: the backticks and what they hold are code, and a string inside them is read as a string.</summary>
	[Fact]
	public void BackticksInCodeAreCodeAndAStringInsideThemIsStillAString()
	{
		const string source = "now=`date +%s`\nx=`echo \"a#b\"`\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "now=`date +%s`", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "x=`echo ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "a#b", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "`\nafter=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: backslash-newline. Not lexical: a continued command is code across the break, and inside double quotes the break is body.</summary>
	[Fact]
	public void ABackslashNewlineIsNotLexicalAndAContinuedCommandIsCodeAcrossTheBreak()
	{
		const string source = "curl -s -u \\\n\tuser \\\n\turl\necho \"a\\\nb\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "curl -s -u \\\n\tuser \\\n\turl\necho ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "a\\\nb", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>&lt;&lt;</c> here-document. A closing alone on its line ends the body; an unquoted terminator expands, so a substitution inside it is code.</summary>
	[Fact]
	public void AnUnquotedHereDocumentEndsOnAClosingAloneOnItsLineAndItsSubstitutionIsCode()
	{
		const string source = "cat <<EOF\nDear user,\nyour name is $(whoami).\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "Dear user,\nyour name is ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "whoami", LexicalZone.Code);
		AssertZoneAt(mask, source, "EOF\nafter", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>&lt;&lt;</c> here-document, quoted terminators. Any quoting of the terminator (single, double, backslash-led) stops expansion, so the whole body is string body.</summary>
	[Theory]
	[InlineData("<<EOF", false)]
	[InlineData("<<'EOF'", true)]
	[InlineData("<<\"EOF\"", true)]
	[InlineData("<<\\EOF", true)]
	[InlineData("<< 'EOF'", true)]
	public void AQuotedTerminatorStopsExpansionInTheBody(string opening, bool bodyIsLiteral)
	{
		string source = $"cat {opening}\nvalue $(echo hi) $v\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, opening, LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "value ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "echo hi", bodyIsLiteral ? LexicalZone.StringBody : LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>&lt;&lt;</c> here-document, the terminator word. Hyphens, dots, underscores and a quoted word with a blank are terminators; the closing is that whole word and nothing shorter.</summary>
	[Theory]
	[InlineData("<<END-OF.DATA_1", "END-OF.DATA_1")]
	[InlineData("<<_EOF_", "_EOF_")]
	[InlineData("<<'END OF'", "END OF")]
	[InlineData("<<\"EOF.sql\"", "EOF.sql")]
	public void ATerminatorWordIsReadAsTheShellReadsIt(string opening, string closing)
	{
		string source = $"cat {opening}\nbody\n{closing[..(closing.Length - 1)]}\nmore\n{closing}\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "more\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, closing, LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>&lt;&lt;</c> here-document. Blanks may stand between the operator and the word; the tab-stripping form takes them too.</summary>
	[Fact]
	public void BlanksBetweenTheOperatorAndTheWordStillOpenAHereDocument()
	{
		const string source = "cat << EOF\nplain\nEOF\ncat <<-   EOF\n\tstripped\n\tEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "plain\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "\tstripped\n\t", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>&lt;&lt;</c> here-document, closing near-misses. A trailing blank, a leading blank or tab, a longer word and a word ending in the terminator do not close the plain form.</summary>
	[Fact]
	public void OnlyAClosingAloneOnItsLineEndsAPlainHereDocument()
	{
		const string source = "cat <<EOF\nEOF \n EOF\n\tEOF\nEOFX\nxEOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "EOF \n EOF\n\tEOF\nEOFX\nxEOF\nbody\n", LexicalZone.StringBody);
		AssertZoneAt(mask, source, "EOF\nafter", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: <c>&lt;&lt;-</c> here-document. A closing indented by tabs closes it; one indented by spaces does not; the plain form is not closed by a tab.</summary>
	[Fact]
	public void TheTabStrippingFormClosesOnTabsOnly()
	{
		const string stripped = "cat <<-EOF\n\tbody\n\t\tEOF\nafter=1\n";
		const string spaced = "cat <<-EOF\n  EOF\nbody\nEOF\nafter=1\n";

		ZoneMask strippedMask = ShellShapedProfile.Scan(stripped);
		ZoneMask spacedMask = ShellShapedProfile.Scan(spaced);

		ZoneAssertions.SpansZone(strippedMask, stripped, "\tbody\n\t\t", LexicalZone.StringBody);
		AssertZoneAt(strippedMask, stripped, "EOF\nafter", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(strippedMask, stripped, _FOLLOWING_STATEMENT, LexicalZone.Code);
		ZoneAssertions.SpansZone(spacedMask, spaced, "  EOF\nbody\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(spacedMask, spaced, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(strippedMask.IsCompletelyValid);
		Assert.True(spacedMask.IsCompletelyValid);
	}

	/// <summary>Row: <c>&lt;&lt;''</c> and <c>&lt;&lt;""</c>, the empty terminator. It closes at the first empty line and the following line is code; the tab-stripping form closes at a line of tabs only.</summary>
	[Theory]
	[InlineData("cat <<''\nline one\n\nafter=1\n", "line one\n")]
	[InlineData("cat <<\"\"\nline one\n\nafter=1\n", "line one\n")]
	[InlineData("cat <<-''\nline one\n\t\t\nafter=1\n", "line one\n")]
	[InlineData("cat <<''\n\nafter=1\n", "")]
	public void AnEmptyTerminatorClosesAtTheFirstEmptyLine(string source, string expectedBody)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		if (expectedBody.Length > 0)
		{
			ZoneAssertions.SpansZone(mask, source, expectedBody, LexicalZone.StringBody);
		}

		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: the rest of the opening line. It is code, so a command that follows the terminator is read as code and its quotes as strings.</summary>
	[Fact]
	public void TheRestOfTheOpeningLineIsCode()
	{
		const string source = "cat <<EOF | curl -s -u \"$CRED\" --data @- \"$URL\"\n{\"a\":1}\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, " | curl -s -u ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "$CRED", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " --data @- ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "$URL", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "{\"a\":1}\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: the rest of the opening line, after a metacharacter. The terminator word ends at <c>&gt; | ; &amp;</c>, and what follows is code.</summary>
	[Theory]
	[InlineData(">out.txt")]
	[InlineData("|wc -l")]
	[InlineData(";echo semi")]
	[InlineData("&&echo ok")]
	[InlineData(" # a note")]
	public void TheTerminatorWordEndsAtAMetacharacterAndTheRestIsCode(string rest)
	{
		string source = $"cat <<EOF{rest}\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: several openings on one line. Their bodies are read in order, and the first body's multi-line substitution does not disturb the second.</summary>
	[Fact]
	public void SeveralOpeningsOnOneLineReadTheirBodiesInOrder()
	{
		const string source = "{ cat <&3; cat; } 3<<A <<'B'\nbody a $(\necho one\n)\nA\nbody b $(echo two)\nB\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<A", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "<<'B'", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body a ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "echo one", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body b $(echo two)\n", LexicalZone.StringBody);
		AssertZoneAt(mask, source, "B\nafter", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: backslash-newline after an opening. It does not drain: the body starts after the line that ends the continued command; an even run of backslashes does not continue it.</summary>
	[Fact]
	public void ABackslashNewlineAfterAnOpeningMovesTheBodyToTheLineThatEndsTheCommand()
	{
		const string continued = "cat <<EOF \\\n| tr a-z A-Z\nhello\nEOF\nafter=1\n";
		const string evenRun = "cat <<EOF \\\\\nhello\nEOF\nafter=1\n";

		ZoneMask continuedMask = ShellShapedProfile.Scan(continued);
		ZoneMask evenRunMask = ShellShapedProfile.Scan(evenRun);

		ZoneAssertions.SpansZone(continuedMask, continued, "| tr a-z A-Z", LexicalZone.Code);
		ZoneAssertions.SpansZone(continuedMask, continued, "hello\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(continuedMask, continued, _FOLLOWING_STATEMENT, LexicalZone.Code);
		ZoneAssertions.SpansZone(evenRunMask, evenRun, "hello\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(evenRunMask, evenRun, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(continuedMask.IsCompletelyValid);
		Assert.True(evenRunMask.IsCompletelyValid);
	}

	/// <summary>Row: <c>&lt;&lt;&lt;</c> here-string, and <c>&lt;&lt;</c> in <c>$(( ))</c> and <c>(( ))</c>. None opens a here-document; a real one on the next line still does; an unquoted <c>let</c> shift is the separate row below.</summary>
	[Theory]
	[InlineData("cat <<<word")]
	[InlineData("cat <<<\"a b\"")]
	[InlineData("echo $(( 1 << 3 ))")]
	[InlineData("echo $((1<<x))")]
	[InlineData("echo \"$(( 1 <<x ))\"")]
	[InlineData("(( n = 1 << 2 ))")]
	[InlineData("(( n <<x ))")]
	public void AnOpeningTheExpressionOrThePredicateRefusesOpensNoHereDocument(string refusedLine)
	{
		string source = $"{refusedLine}\ncat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<<", StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<<", StringComparison.Ordinal) + 1));
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: arithmetic context, closed. An arithmetic expression that closed its parentheses does not refuse a later opening on the line.</summary>
	[Fact]
	public void AnArithmeticExpressionThatClosedDoesNotRefuseALaterOpening()
	{
		const string source = "echo $((1+2)) <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: an unquoted <c>let</c> with a shift. Every shell that has <c>let</c> (<c>bash</c>, <c>zsh</c>, <c>ksh93</c>, <c>mksh</c>) reads <c>&lt;&lt;3</c> as a here-document opening whose terminator is <c>3</c>, so the lines after it are body up to a closing <c>3</c> and the profile reads them so; blanks around the operator do not change it.</summary>
	[Theory]
	[InlineData("let v=1<<3\necho $v\n3\nafter=1\n", "<<3")]
	[InlineData("let v = 1 << 3\necho $v\n3\nafter=1\n", "<< 3")]
	[InlineData("  let v=1<<3\necho $v\n3\nafter=1\n", "<<3")]
	public void AnUnquotedLetShiftOpensAHereDocumentAsEveryShellWithLetReadsIt(string source, string expectedOpening)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedOpening, LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "echo $v\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: an unquoted <c>let</c> shift with no closing <c>3</c>. A shell warns or fails ("delimited by end-of-file", "here document unclosed"); the scanner abandons the tail as an unterminated literal, the same reading.</summary>
	[Fact]
	public void AnUnquotedLetShiftWithNoClosingLeavesTheTailUnterminated()
	{
		const string source = "let v=1<<3\necho $v\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		Assert.Equal(source.IndexOf("<<3", StringComparison.Ordinal), mask.ValidThroughPosition);
		Assert.True(mask.WasAbandonedMidFile);
		ZoneAssertions.SpansZone(mask, source, "let v=1", LexicalZone.Code);
	}

	/// <summary>Row: a quoted <c>let</c> expression. The shift is inside a string, so nothing opens.</summary>
	[Theory]
	[InlineData("let \"w = 1 << 3\"\necho $w\nafter=1\n")]
	[InlineData("let 'w=1<<3'\necho $w\nafter=1\n")]
	[InlineData("let w=1\\<\\<3\necho $w\nafter=1\n")]
	public void AQuotedOrEscapedLetShiftOpensNoHereDocument(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "echo $w\nafter=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: a partially quoted terminator. It is an opening whose terminator is the word with its quotes removed, quoted: the body is body.</summary>
	[Fact]
	public void APartiallyQuotedTerminatorIsAnOpeningWhoseBodyIsBody()
	{
		const string source = "cat <<E\"O\"F\nvalue\nEOF\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<E\"O\"F", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "value\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: a here-document inside a <c>$( )</c> in code. It opens at depth zero, so its body is read as body and the closing parenthesis after it is code.</summary>
	[Fact]
	public void AHereDocumentInsideAnUnquotedSubstitutionIsRead()
	{
		const string source = "x=$(cat <<EOF\ninner $v\nEOF\n)\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "inner ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "\n)\nafter=1", LexicalZone.Code, occurrence: 0);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: a here-document inside <c>"$( … )"</c>. It waits in the hole's own queue and its body is read as body, and the closing parenthesis after it closes the hole.</summary>
	[Fact]
	public void AHereDocumentInsideAQuotedSubstitutionIsReadAndItsBodyIsBody()
	{
		const string source = "x=\"$(cat <<EOF\ninner\nEOF\n)\"\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "inner\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, ")", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: a <c>case</c> arm inside <c>"$( … )"</c>. Its <c>)</c> ends the arm's pattern and not the hole: the construct after the hole is read in its own zone.</summary>
	[Fact]
	public void ACaseArmInsideAQuotedSubstitutionDoesNotCloseTheHole()
	{
		const string source = "echo \"$(case a in a) echo hit;; esac)\" tail\nafter=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "case a in a) echo hit;; esac", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " tail", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Rows: a here-document opening inside a comment, inside quotes and inside another here-document's body is not an opening.</summary>
	[Fact]
	public void AnOpeningInACommentInQuotesOrInAnotherBodyOpensNothing()
	{
		const string source =
			"echo start # cat <<EOF\n"
			+ "echo '<<EOF' \"<<EOF\"\n"
			+ "cat <<A\n<<B\nA\n"
			+ "after=1\n";

		ZoneMask mask = ShellShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "# cat <<EOF", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.StringBody, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.StringBody, occurrence: 2);
		ZoneAssertions.SpansZone(mask, source, "<<B\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	/// <summary>Row: end of file with a body pending. The scanner abandons the tail as an unterminated literal, the existing terminal semantics.</summary>
	[Theory]
	[InlineData("cat <<EOF\nnever closed\n")]
	[InlineData("cat <<EOF")]
	public void AnOpeningWithNoClosingBeforeTheEndOfTheFileIsUnterminated(string source)
	{
		ZoneMask mask = ShellShapedProfile.Scan(source);

		Assert.False(mask.IsCompletelyValid);
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
	}

	/// <summary>Row: case. Keywords, delimiters and identifiers are case-sensitive: a closing in the wrong case does not close, and two names differing in case are two declarations.</summary>
	[Fact]
	public void KeywordsDelimitersAndIdentifiersAreCaseSensitive()
	{
		const string source = "cat <<EOF\neof\nEOF\nToken=a\ntoken=b\n";
		LexicalProfile profile = ShellShapedProfile.Build();

		ZoneMask mask = ShellShapedProfile.Scan(source);
		FileSymbolTable symbols = FileSymbolTable.Collect(profile, source, mask);

		Assert.Equal(CaseSensitivity.Sensitive, profile.KeywordCaseSensitivity);
		Assert.Equal(CaseSensitivity.Sensitive, profile.DelimiterCaseSensitivity);
		Assert.Equal(IdentifierCaseSensitivityMode.Sensitive, profile.IdentifierCaseSensitivity);
		ZoneAssertions.SpansZone(mask, source, "eof\n", LexicalZone.StringBody);
		Assert.Equal(1, symbols.DeclarationCountOf("Token"));
		Assert.Equal(1, symbols.DeclarationCountOf("token"));
	}

	/// <summary>Row: constant declaration. A statement-start <c>NAME=literal</c>, with the optional keyword and flags, is one declaration per assignment; a reassigned name is indeterminate; a value with an expansion, an assignment that is not at statement start, and one inside a body are not declarations.</summary>
	[Fact]
	public void AStatementStartAssignmentOfALiteralIsAConstantDeclarationAndAReassignedNameIsIndeterminate()
	{
		const string source =
			"A=1\n"
			+ "readonly B=two\n"
			+ "export C='three four'\n"
			+ "declare -r E=\"five\"\n"
			+ "typeset -x F=6\n"
			+ "G=1\n"
			+ "G=2\n"
			+ "H=$HOME\n"
			+ "I=$(cmd)\n"
			+ "echo J=1\n"
			+ "cat <<EOF\n"
			+ "L=1\n"
			+ "EOF\n";
		const int afterEveryDeclaration = int.MaxValue;

		FileSymbolTable symbols = FileSymbolTable.Collect(ShellShapedProfile.Build(), source, ShellShapedProfile.Scan(source));

		Assert.True(symbols.TryResolveUnambiguously("B", afterEveryDeclaration, out string constantValue));
		Assert.Equal("two", constantValue);
		Assert.True(symbols.TryResolveUnambiguously("C", afterEveryDeclaration, out string quotedValue));
		Assert.Equal("three four", quotedValue);
		Assert.True(symbols.TryResolveUnambiguously("E", afterEveryDeclaration, out string doubleQuotedValue));
		Assert.Equal("five", doubleQuotedValue);
		Assert.Equal(1, symbols.DeclarationCountOf("A"));
		Assert.Equal(1, symbols.DeclarationCountOf("F"));
		Assert.Equal(2, symbols.DeclarationCountOf("G"));
		Assert.False(symbols.TryResolveUnambiguously("G", afterEveryDeclaration, out _));
		Assert.Equal(0, symbols.DeclarationCountOf("H"));
		Assert.Equal(0, symbols.DeclarationCountOf("I"));
		Assert.Equal(0, symbols.DeclarationCountOf("J"));
		Assert.Equal(0, symbols.DeclarationCountOf("L"));
	}

	/// <summary>Row: the first line. A test-local expression of the shape the analyzer will declare, set against the base shebang table over the shapes real scripts use; separators beyond blank and tab are the parity test's of the analyzer set.</summary>
	[Theory]
	[InlineData("#!/bin/sh")]
	[InlineData("#!/bin/bash")]
	[InlineData("#!/usr/bin/env bash")]
	[InlineData("#!/usr/bin/env -S bash -e")]
	[InlineData("#!/usr/bin/env -i -S zsh")]
	[InlineData("#! /bin/ksh")]
	[InlineData("#!/bin/sh -e")]
	[InlineData("#!/bin/sh\t-e")]
	[InlineData("#!bash")]
	[InlineData("#!/bin/dash")]
	[InlineData("#!/bin/bashful")]
	[InlineData("#!/usr/bin/env")]
	[InlineData("#!/usr/bin/env python3")]
	[InlineData("#!/usr/bin/env pwsh")]
	[InlineData("#!/usr/bin/env FOO=1 bash")]
	[InlineData("# /bin/sh")]
	[InlineData("/bin/sh")]
	public void TheFirstLineExpressionAgreesWithTheBaseShebangTable(string firstLine)
	{
		bool baseSaysShell = BaseLanguageDetection.TryGetByShebangFirstLine(firstLine, out AssessedLanguage language)
			&& language == AssessedLanguage.Shell;

		Assert.Equal(baseSaysShell, FirstLineExpression().IsMatch(firstLine));
	}

	private static void AssertZoneAt(ZoneMask mask, string source, string anchor, LexicalZone zone)
	{
		int position = source.IndexOf(anchor, StringComparison.Ordinal);

		Assert.True(position >= 0, $"The text does not contain the anchor {anchor}.");
		Assert.True(zone == mask.ZoneAt(position), ZoneAssertions.Render(mask, source));
	}
}
