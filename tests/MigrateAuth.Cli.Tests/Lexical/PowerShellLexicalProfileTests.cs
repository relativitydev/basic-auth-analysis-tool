using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE REAL `PowerShell` LEXICAL PROFILE, read from the embedded catalog and run through the real scanner.
/// Every script is invented, generic text.
/// </summary>
public sealed class PowerShellLexicalProfileTests
{
	private const string _LANGUAGE_NAME = "PowerShell";

	[Fact]
	public void AHereStringCloserThatIsNotAtTheStartOfALineDoesNotCloseTheString()
	{
		const string source = "$h = @\"\nbody\n  \"@\nstill body\n\"@\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "@\"", LexicalZone.Delimiter);
		ZoneAssertions.SpansZone(mask, source, "\nbody\n  \"@\nstill body\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "\"@", LexicalZone.Delimiter, occurrence: 1);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Theory]
	[InlineData("$h = @\"   \nbody\n\"@\n$after = 1\n")]
	[InlineData("$h = @'\t\nbody 'quoted'\n'@\n$after = 1\n")]
	public void AHereStringOpenerFollowedOnlyByBlanksOpensTheString(string source)
	{
		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "body", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnOpenerFollowedByTextIsAnOrdinaryQuoteAndNotAHereString()
	{
		const string source = "$h = @\"text\" + 'x'\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "text", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AHashInsideAnUnquotedArgumentOrAfterAWordCharacterIsNotAComment()
	{
		const string source = "Write-Host tag#1 value\n$x = $y#z\n# a real comment\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "tag#1 value", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "$y#z", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "# a real comment", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
	}

	[Theory]
	[InlineData("$total = 1 # after a space")]
	[InlineData("$list = @(1, 2);# right after a semicolon")]
	[InlineData("Get-Item $p |# after a pipe")]
	[InlineData("$a = 1,# after a comma")]
	[InlineData("# at the start of the text")]
	public void AHashAtTheStartOfAStatementOrAfterAnOperatorOrWhitespaceOpensAComment(string source)
	{
		ZoneMask mask = Scan(source + "\n");

		ZoneAssertions.SpansZone(mask, source, source[source.IndexOf('#', StringComparison.Ordinal)..], LexicalZone.Comment);
	}

	[Fact]
	public void ABacktickEscapedHashOrQuoteInCodeOpensNothing()
	{
		const string source = "Write-Host `# not a comment\nWrite-Host `\"not a string`\" tail\n$z = ``\"abc\"\n$end = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "`# not a comment", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "`\"not a string`\" tail", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "$end = 1", LexicalZone.Code);
	}

	[Fact]
	public void ANestedSubExpressionInAnExpandableStringIsOneHoleWithItsParenthesesBalanced()
	{
		const string source = "$s = \"id $($c.GetNetworkCredential().Password) done $(Get-Item (Join-Path 'a' 'b')) end\"\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "$c.GetNetworkCredential().Password", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "Get-Item (Join-Path ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, " done ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " end", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
		Assert.Equal(0, mask.UnbalancedNestingCount);
	}

	[Fact]
	public void ABareVariableInAnExpandableStringIsStringBodyAndNotAHole()
	{
		const string source = "$s = \"value $name and ${other} end\"\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "value $name and ${other} end", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
	}

	[Fact]
	public void TheBasicParsingSwitchOfAWebCmdletIsCodeAndOpensNothing()
	{
		const string source = "Invoke-WebRequest -Uri $uri -UseBasicParsing\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "Invoke-WebRequest -Uri $uri -UseBasicParsing", LexicalZone.Code);
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.StringBody);
		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Comment);
	}

	[Fact]
	public void ATypographicQuotePairIsAStringAndItsContentsOpenNothing()
	{
		const string source = "$d = “text with ' and # inside”\n$s = ‘literal \"inner\"’\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "text with ' and # inside", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "literal \"inner\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ADoubledClosingQuoteIsAnEscapeInBothOrdinaryKinds()
	{
		const string source = "$a = 'it''s'\n$b = \"say \"\"hi\"\"\"\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "it''s", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "say \"\"hi\"\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
	}

	[Fact]
	public void ABlockCommentIsACommentAcrossLinesAndCloseOnItsOwnClosing()
	{
		const string source = "Get-Item <# inline #> -Path $p\n<#\n.EXAMPLE\n Invoke-RestMethod -Authentication Basic\n#>\n$after = 1\n";

		ZoneMask mask = Scan(source);

		ZoneAssertions.SpansZone(mask, source, "<# inline #>", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, " -Path $p", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "Invoke-RestMethod -Authentication Basic", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "$after = 1", LexicalZone.Code);
	}

	[Fact]
	public void AnUnterminatedBlockCommentIsADeclaredAnomalyAndStopsTheScan()
	{
		const string source = "$a = 1\n<# never closed\n$b = 2\n";

		ZoneMask mask = Scan(source);

		Assert.Equal(LexicalAnomalyReason.UnterminatedBlockComment, mask.AnomalyReason);
		Assert.True(mask.WasAbandonedMidFile);
	}

	[Fact]
	public void TheProfileDeclaresItsDelimitersKeywordsAndNamesAsCaseInsensitive()
	{
		LexicalProfile profile = ReadProfile();

		Assert.Equal(CaseSensitivity.Insensitive, profile.KeywordCaseSensitivity);
		Assert.Equal(CaseSensitivity.Insensitive, profile.DelimiterCaseSensitivity);
		Assert.Equal(IdentifierCaseSensitivityMode.Insensitive, profile.IdentifierCaseSensitivity);
	}

	[Fact]
	public void TheDoubleQuotedRuleCountsNestedParenthesesAndTheHereStringsAreAnchoredByPosition()
	{
		LexicalProfile profile = ReadProfile();
		QuoteRule doubleQuoted = profile.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "\"");
		QuoteRule expandableHereString = profile.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "@\"");
		QuoteRule literalHereString = profile.QuoteRulesInEngineOrder.Single(rule => rule.Opening == "@'");

		Assert.Equal("(", doubleQuoted.InterpolationNestingOpening);
		Assert.Equal("$(", doubleQuoted.InterpolationOpening);
		Assert.Equal(PositionalAnchoring.EndOfLineAfterOptionalBlanks, expandableHereString.OpeningAnchoring);
		Assert.Equal(PositionalAnchoring.StartOfLine, expandableHereString.ClosingAnchoring);
		Assert.Equal(PositionalAnchoring.EndOfLineAfterOptionalBlanks, literalHereString.OpeningAnchoring);
		Assert.Equal(PositionalAnchoring.StartOfLine, literalHereString.ClosingAnchoring);
		Assert.Null(expandableHereString.InterpolationOpening);
	}

	[Fact]
	public void TheShippedConstantDeclarationReadsTheBareNameOfEverySpellingOfAVariable()
	{
		LexicalProfile profile = ReadProfile();
		const string source = "$a = 'one'\n${b} = 'two'\n$script:c = 'three'\n[string]$d = \"four\"\n";

		System.Text.RegularExpressions.MatchCollection matches = profile.ConstantDeclarationExpression.Matches(source);

		Assert.Equal(["a", "b", "c", "d"], matches.Select(match => match.Groups["name"].Value));
		Assert.Equal(["one", "two", "three", "four"], matches.Select(match => match.Groups["value"].Value));
	}

	[Fact]
	public void AParameterDefaultAndAConcatenationAreNotConstantDeclarations()
	{
		LexicalProfile profile = ReadProfile();
		const string source = "function F($scheme = 'Basic', $other = 'x') { }\nparam(\n\t[string]$kind = 'Basic',\n\t[string]$last = 'y'\n)\n$joined = 'Basic' + $suffix\n$n = \"has $interpolation\"\n";

		System.Text.RegularExpressions.MatchCollection matches = profile.ConstantDeclarationExpression.Matches(source);

		Assert.Empty(matches);
	}

	private static LexicalProfile ReadProfile()
	{
		return LanguageCatalogRegistry.BuildEmbeddedCatalog().AnalyzersByLanguageName[_LANGUAGE_NAME].LexicalProfile;
	}

	private static ZoneMask Scan(string source)
	{
		return new LexicalScanner(ReadProfile()).Scan(source);
	}
}
