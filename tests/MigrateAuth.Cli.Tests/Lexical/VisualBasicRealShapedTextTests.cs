using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// THE LEXICAL-PROFILE VERIFICATION OF THE `VBNet` PROFILE ON REAL-SHAPED TEXT. The text is invented and
/// generic, and the profile is test-local (<see cref="VisualBasicShapedProfile"/>): nothing here reads a shipped
/// catalog. Each field of the profile runs here, with these cases: a multi-line literal holding the word the tool looks for, the typographic
/// quote pairs and apostrophes, `REM` in every position it can stand, date literals, preprocessor lines, the doubled quote at the start, the middle and
/// the end of a literal, an interpolation hole with a literal inside it, a continuation that carries a comment, and the case-insensitive delimiter.
///
/// WHAT IS CHARACTERIZED, NOT VERIFIED. Three behaviours are the profile's declared limits and are asserted as they are, so the damage is stated and cannot change
/// unnoticed: the doubled interpolation opener (literal text under the shipped flag), the prose of an XML literal, and a disabled `#If` branch.
/// </summary>
public sealed class VisualBasicRealShapedTextTests
{
	private const string _CONTINUATION_SOURCE = "Dim s As String = \"a\" & _ ' comment after the underscore\n\t\"b\"\nDim t As Integer = 1\n";

	[Fact]
	public void AMultiLineLiteralHoldingTheSchemeWordIsStringBodyAndTheCodeAfterItIsCode()
	{
		string source = string.Join(
			"\n",
			"Dim banner As String = \"Usage:",
			"  tool --user <name>   sends an Authorization: Basic header",
			"  second line with a colon : and a quote \"\" inside",
			"\"",
			"Dim after As Integer = 1",
			string.Empty);
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "Usage:\n  tool --user <name>   sends an Authorization: Basic header\n  second line with a colon : and a quote \"\" inside\n", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "Dim after As Integer = 1", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "Basic", LexicalZone.StringBody);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TypographicQuotedLiteralsAreStringBodyWithTheirDoubledClosersAndOpeners()
	{
		string source = string.Join(
			"\n",
			"Dim a As String = “Basic abc”",
			"Dim b As String = “it””s fine”",
			"Dim c As String = ”second pair”",
			"Dim d As Integer = 1",
			string.Empty);
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "Basic abc", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "it””s fine", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "second pair", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "Dim d As Integer = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void RemOpensACommentInEveryPositionTheCompilerProbesSettled()
	{
		string source = string.Join(
			"\n",
			"REM comment at the start of a line",
			"Dim a As Integer = 1 : REM comment after a colon",
			"If a = 1 Then Rem comment after Then",
			"Dim b As Integer = 2 Rem comment after a blank with no colon",
			"Dim c = o.Rem hidden after a member dot",
			"Dim d = o.[Rem]",
			"Dim Remark As Integer = 1",
			"Dim xRem As Integer = Remark",
			"rem lower case",
			"Dim e As Integer = 1",
			string.Empty);
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "REM comment at the start of a line", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "REM comment after a colon", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "Rem comment after Then", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "Rem comment after a blank with no colon", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "Rem hidden after a member dot", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "Dim d = o.[Rem]", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "Dim Remark As Integer = 1", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "Dim xRem As Integer = Remark", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "rem lower case", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "Dim e As Integer = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheApostropheAndTheTypographicApostrophesOpenCommentsAndAnApostropheInAStringDoesNot()
	{
		string source = string.Join(
			"\n",
			"Dim a As Integer = 1 ' ordinary comment",
			"‘ left typographic apostrophe comment",
			"’ right typographic apostrophe comment",
			"Dim s As String = \"it's not a comment\" ' but this is",
			"Dim b As Integer = 2",
			string.Empty);
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "' ordinary comment", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "‘ left typographic apostrophe comment", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "’ right typographic apostrophe comment", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "it's not a comment", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "' but this is", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "Dim b As Integer = 2", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void DateLiteralsAndPreprocessorLinesAreCodeAndTheirStringsAndCommentsAreRead()
	{
		string source = string.Join(
			"\n",
			"#Const LEGACY = False",
			"#Region \"title's here\"",
			"Dim a As Date = #1/15/2004 11:30:00 PM#",
			"Dim b As Date = #2021-06-30# : Dim s As String = \"#not a date#\"",
			"#If LEGACY Then ' trailing comment after Then",
			"Dim c As Integer = 3",
			"#End If",
			"#End Region",
			"Dim d As Integer = 4",
			string.Empty);
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "#Const LEGACY = False", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "title's here", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "#1/15/2004 11:30:00 PM#", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "#2021-06-30#", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "#not a date#", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "' trailing comment after Then", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "Dim d As Integer = 4", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void ADoubledQuoteAtTheStartTheMiddleAndTheEndOfALiteralIsConsumedAsAPair()
	{
		string source = string.Join(
			"\n",
			"Dim a As String = \"\"\"start\"",
			"Dim b As String = \"mid\"\"dle\"",
			"Dim c As String = \"end\"\"\"",
			"Dim d As String = \"\"\"\"",
			"Dim e As Integer = 1",
			string.Empty);
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "\"\"start", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "mid\"\"dle", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "end\"\"", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "Dim e As Integer = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AnInterpolatedLiteralReadsItsHoleAsCodeWithAStringAndAFormatInsideIt()
	{
		string source = string.Join(
			"\n",
			"Dim s As String = $\"on {#6/1/2024#:yyyy-MM-dd} the {If(ok, \"yes\", \"no\")} value\"",
			"Dim t As Integer = 1",
			string.Empty);
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "on ", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "#6/1/2024#:yyyy-MM-dd", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "If(ok, ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "yes", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, " value", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "Dim t As Integer = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void AContinuationThatCarriesACommentDoesNotChangeTheMaskOfTheContinuedStatement()
	{
		ZoneMask mask = VisualBasicShapedProfile.Scan(_CONTINUATION_SOURCE);

		ZoneAssertions.SpansZone(mask, _CONTINUATION_SOURCE, "' comment after the underscore", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, _CONTINUATION_SOURCE, "a", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, _CONTINUATION_SOURCE, "b", LexicalZone.StringBody, occurrence: 0);
		ZoneAssertions.SpansZone(mask, _CONTINUATION_SOURCE, "Dim t As Integer = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void TheThreeCaseFieldsAreInsensitiveSoEveryCaseOfRemOpensTheSameComment()
	{
		string[] spellings = ["REM", "Rem", "rem", "rEm"];

		foreach (string spelling in spellings)
		{
			string source = spelling + " comment\nDim a As Integer = 1\n";
			ZoneMask mask = VisualBasicShapedProfile.Scan(source);

			ZoneAssertions.SpansZone(mask, source, spelling + " comment", LexicalZone.Comment);
			ZoneAssertions.SpansZone(mask, source, "Dim a As Integer = 1", LexicalZone.Code);
		}
	}

	// ------------------------------------------------------------------------------------------
	// The declared limits, asserted as they are
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void TheDoubledInterpolationOpenerIsLiteralTextAndTheFileStaysValid()
	{
		string source = "Dim s As String = $\"{{x}}\"\nDim t As Integer = 1\n";
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "{{x}}", LexicalZone.StringBody);
		ZoneAssertions.SpansZone(mask, source, "Dim t As Integer = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void CharacterizationAnOddQuoteInXmlLiteralProseOpensALiteralThatRunsToTheEndOfTheFile()
	{
		string source = "Dim x = <note>an odd \" quote in prose</note>\nDim after As Integer = 1\n";
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
		ZoneAssertions.SpansZone(mask, source, "Dim x = <note>an odd ", LexicalZone.Code);
	}

	[Fact]
	public void CharacterizationAnApostropheInXmlLiteralProseOpensACommentToTheEndOfThatLine()
	{
		string source = "Dim x = <note>it's prose</note>\nDim after As Integer = 1\n";
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		ZoneAssertions.SpansZone(mask, source, "'s prose</note>", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "Dim after As Integer = 1", LexicalZone.Code);
		Assert.True(mask.IsCompletelyValid);
	}

	[Fact]
	public void CharacterizationAStrayQuoteInADisabledPreprocessorBranchAbandonsTheFileTail()
	{
		string source = "#If False Then\nDim s As String = \"stray\n#End If\nDim after As Integer = 1\n";
		ZoneMask mask = VisualBasicShapedProfile.Scan(source);

		Assert.True(mask.WasAbandonedMidFile);
		Assert.Equal(LexicalAnomalyReason.UnterminatedLiteral, mask.AnomalyReason);
	}
}
