using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE REFUSALS, THE LET SHIFT AND THE COMMENT PREDECESSORS OF THE SHIPPED `Shell` PROFILE, scanned on the shipped profile and not on the test-local one: the
/// here-string and the two arithmetic contexts open no here-document, each followed by a here-document construct that is still recognized; an unquoted <c>let</c>
/// shift opens one; and a hash directly after an angle bracket is code in every dialect.
/// </summary>
public sealed class ShellShippedProfileTests
{
	private const string _FOLLOWING_STATEMENT = "after=1";

	private static readonly LexicalProfile _shipped = ShellRegisteredExpressions.Analyzer.LexicalProfile;

	private static ZoneMask Scan(string source)
	{
		return new LexicalScanner(_shipped).Scan(source);
	}

	[Theory]
	[InlineData("cat <<<word")]
	[InlineData("cat <<<\"a b\"")]
	[InlineData("echo $(( 1 << 3 ))")]
	[InlineData("echo $((1<<x))")]
	[InlineData("echo \"$(( 1 <<x ))\"")]
	[InlineData("(( n = 1 << 2 ))")]
	[InlineData("(( n <<x ))")]
	public void AnOpeningTheExpressionOrThePredicateRefusesOpensNoHereDocumentAndTheNextOneIsStillRecognized(string refusedLine)
	{
		string source = $"{refusedLine}\ncat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<<EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<<", StringComparison.Ordinal)));
		Assert.Equal(LexicalZone.Code, mask.ZoneAt(source.IndexOf("<<", StringComparison.Ordinal) + 1));
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("cat <<<word", "<<-")]
	[InlineData("echo $(( 1 << 3 ))", "<<-")]
	[InlineData("(( n = 1 << 2 ))", "<<-")]
	public void ARefusalIsFollowedByATabStrippingConstructThatIsStillRecognized(string refusedLine, string opening)
	{
		string source = $"{refusedLine}\ncat {opening}EOF\n\tbody\n\tEOF\nafter=1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, opening + "EOF", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "\tbody\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AMultiLineArithmeticContextRefusesAnOpeningOnALaterLineAndTheHereDocumentAfterItIsRecognized()
	{
		const string source = "(( n = 1\n<< 3 ))\ncat <<EOF\nbody\nEOF\nafter=1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<< 3 ))", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("let v=1<<3\necho $v\n3\nafter=1\n", "<<3")]
	[InlineData("let v = 1 << 3\necho $v\n3\nafter=1\n", "<< 3")]
	[InlineData("  let v=1<<3\necho $v\n3\nafter=1\n", "<<3")]
	public void AnUnquotedLetShiftOpensAHereDocumentAsEveryShellWithLetReadsIt(string source, string expectedOpening)
	{
		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedOpening, LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "echo $v\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("let \"w = 1 << 3\"\necho $w\nafter=1\n")]
	[InlineData("let 'w=1<<3'\necho $w\nafter=1\n")]
	[InlineData("let w=1\\<\\<3\necho $w\nafter=1\n")]
	public void AQuotedOrEscapedLetShiftOpensNoHereDocument(string source)
	{
		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "echo $w\nafter=1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("echo x ># word\nafter=1\n", "># word")]
	[InlineData("echo x <# word\nafter=1\n", "<# word")]
	[InlineData("echo x >#word\nafter=1\n", ">#word")]
	[InlineData("echo x <#word\nafter=1\n", "<#word")]
	[InlineData("cat<#w\nafter=1\n", "<#w")]
	[InlineData("echo >#w;echo y<#z\nafter=1\n", "echo y<#z")]
	public void AHashDirectlyAfterAnAngleBracketIsCodeInEveryDialect(string source, string expectedCode)
	{
		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedCode, LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Comment);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("echo x # note ><\nafter=1\n", "# note ><")]
	[InlineData("echo x;# note\nafter=1\n", "# note")]
	[InlineData("echo x |# note\nafter=1\n", "# note")]
	[InlineData("cat > f # note\nafter=1\n", "# note")]
	[InlineData("( # note\nafter=1\n", "# note")]
	public void AHashAfterABlankOrAWordSeparatorStillOpensAComment(string source, string expectedComment)
	{
		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedComment, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheShippedProfileReadsABashStringExecutedAsCode()
	{
		const string source = "bash -c 'curl -u user:pw http://h.test/'\nafter=1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "curl -u user:pw http://h.test/", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, _FOLLOWING_STATEMENT, LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheShippedProfileLeavesAStringThatNothingExecutesAsStringBody()
	{
		const string source = "echo 'curl -u user:pw http://h.test/'\nafter=1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "curl -u user:pw http://h.test/", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}
}
