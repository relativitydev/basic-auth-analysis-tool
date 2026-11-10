using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Lexical;

using Xunit;

namespace MigrateAuth.Cli.Tests.Lexical;

/// <summary>
/// The rule index is internal to the product, so its behaviour is asserted through the scanner
/// that consults it: which characters can open a construct, under which case sensitivity, and in
/// which order the candidates for one character are tried.
/// </summary>
public sealed class LexicalRuleIndexBucketingTests
{
	private const string _PROFILE_NAME = "BucketingProfile";
	private const string _IDENTIFIER_PATTERN = "[A-Za-z_][A-Za-z0-9_]*";
	private const string _WORD_OPENING = "REM";
	private const string _WIDE_OPENING = "«";
	private const string _WIDE_UPPER_CASE_OPENING = "É";
	private const string _SHORT_LINE_OPENING = "#";
	private const string _LONG_BLOCK_OPENING = "#[";
	private const string _LONG_BLOCK_CLOSING = "]#";

	[Theory]
	[InlineData("REM note", "REM note")]
	[InlineData("rem note", "rem note")]
	[InlineData("Rem note", "Rem note")]
	public void AnInsensitiveWordOpenerIsFoundWhateverTheCaseOfItsFirstLetter(string line, string expectedComment)
	{
		string source = $"code\n{line}\nmore";
		LexicalProfile profile = BuildProfile(
			keywordCase: CaseSensitivity.Insensitive,
			delimiterCase: CaseSensitivity.Sensitive,
			lineComments: [new LineCommentRule { Opening = _WORD_OPENING, RequiresWordBoundary = true }]);

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		ZoneAssertions.SpansZone(mask, source, expectedComment, LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "more", LexicalZone.Code);
	}

	[Fact]
	public void ASensitiveWordOpenerIsNotFoundUnderAnotherCase()
	{
		const string source = "code\nrem note\nmore";
		LexicalProfile profile = BuildProfile(
			keywordCase: CaseSensitivity.Sensitive,
			delimiterCase: CaseSensitivity.Sensitive,
			lineComments: [new LineCommentRule { Opening = _WORD_OPENING, RequiresWordBoundary = true }]);

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Comment);
	}

	[Fact]
	public void AnOpenerBeyondTheDirectRangeOfCharactersIsFound()
	{
		const string source = "code « note\nmore";
		LexicalProfile profile = BuildProfile(
			keywordCase: CaseSensitivity.Sensitive,
			delimiterCase: CaseSensitivity.Sensitive,
			lineComments: [new LineCommentRule { Opening = _WIDE_OPENING }]);

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		ZoneAssertions.SpansZone(mask, source, "« note", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "more", LexicalZone.Code);
	}

	[Fact]
	public void AnInsensitiveWideOpenerIsFoundUnderItsOtherCase()
	{
		const string source = "code é note\nmore";
		LexicalProfile profile = BuildProfile(
			keywordCase: CaseSensitivity.Sensitive,
			delimiterCase: CaseSensitivity.Insensitive,
			lineComments: [new LineCommentRule { Opening = _WIDE_UPPER_CASE_OPENING }]);

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		ZoneAssertions.SpansZone(mask, source, "é note", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, "more", LexicalZone.Code);
	}

	[Fact]
	public void ACharacterNoRuleOpensWithStaysCode()
	{
		const string source = "plain text\nwith lines";
		LexicalProfile profile = BuildProfile(
			keywordCase: CaseSensitivity.Sensitive,
			delimiterCase: CaseSensitivity.Sensitive,
			lineComments: [new LineCommentRule { Opening = _SHORT_LINE_OPENING }]);

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		ZoneAssertions.NoPositionCarries(mask, source, LexicalZone.Comment);
		ZoneAssertions.CoversEveryPositionExactlyOnce(mask, source);
	}

	[Fact]
	public void TheLongestOpeningSharingACharacterIsTriedFirst()
	{
		const string source = "a #[ block ]# b\nc # line";
		LexicalProfile profile = BuildProfile(
			keywordCase: CaseSensitivity.Sensitive,
			delimiterCase: CaseSensitivity.Sensitive,
			lineComments: [new LineCommentRule { Opening = _SHORT_LINE_OPENING }],
			blockComments: [new BlockCommentRule { Opening = _LONG_BLOCK_OPENING, Closing = _LONG_BLOCK_CLOSING }]);

		ZoneMask mask = new LexicalScanner(profile).Scan(source);

		ZoneAssertions.SpansZone(mask, source, "#[ block ]#", LexicalZone.Comment);
		ZoneAssertions.SpansZone(mask, source, " b\nc ", LexicalZone.Code);
		ZoneAssertions.SpansZone(mask, source, "# line", LexicalZone.Comment);
	}

	private static LexicalProfile BuildProfile(
		CaseSensitivity keywordCase,
		CaseSensitivity delimiterCase,
		ImmutableArray<LineCommentRule> lineComments,
		ImmutableArray<BlockCommentRule> blockComments = default)
	{
		LexicalProfile profile = new()
		{
			ProfileName = _PROFILE_NAME,
			ConstantDeclarationExpression = new Regex(_IDENTIFIER_PATTERN, RegexOptions.CultureInvariant),
			KeywordCaseSensitivity = keywordCase,
			DelimiterCaseSensitivity = delimiterCase,
			LineCommentRules = lineComments,
			BlockCommentRules = blockComments.IsDefault ? ImmutableArray<BlockCommentRule>.Empty : blockComments,
			QuoteRules = ImmutableArray<QuoteRule>.Empty
		};

		return profile;
	}
}
