using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Scanning;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// For PowerShell, a case-insensitive preselection anchor (ordinal, ignoring case) and an
/// <see cref="RegexOptions.IgnoreCase"/> | <see cref="RegexOptions.CultureInvariant"/> expression over the
/// same literal must AGREE on ASCII text. Outside ASCII the two folds may differ, and that is not pinned.
/// </summary>
public sealed class AnchorIgnoreCaseAgreementTests
{
	private const int _ASCII_CHARACTER_COUNT = 128;
	private const string _ANCHOR_WORD = "Authorization";
	private static readonly string[] _surroundingTexts = ["", "x ", "\t", "-- "];

	[Fact]
	public void PowerShell_EverySingleAsciiCharacterAnchorAgreesWithTheIgnoreCaseExpressionOnEveryAsciiCharacter()
	{
		int disagreements = 0;

		for (int anchorCode = 0; anchorCode < _ASCII_CHARACTER_COUNT; anchorCode++)
		{
			string anchorText = ((char)anchorCode).ToString();
			ILanguageAnalyzer analyzer = BuildAnalyzer(anchorText);
			Regex expression = BuildIgnoreCaseExpression(anchorText);

			for (int textCode = 0; textCode < _ASCII_CHARACTER_COUNT; textCode++)
			{
				string text = ((char)textCode).ToString();

				disagreements += CandidatePreselection.IsCandidate(analyzer, text) == expression.IsMatch(text) ? 0 : 1;
			}
		}

		Assert.Equal(0, disagreements);
	}

	[Theory]
	[InlineData(_ANCHOR_WORD)]
	[InlineData("AUTHORIZATION")]
	[InlineData("authorization")]
	[InlineData("aUtHoRiZaTiOn")]
	[InlineData("Authorisation")]
	[InlineData("Author")]
	public void PowerShell_AWordAnchorAgreesWithTheIgnoreCaseExpressionAcrossAsciiCasings(string word)
	{
		ILanguageAnalyzer analyzer = BuildAnalyzer(_ANCHOR_WORD);
		Regex expression = BuildIgnoreCaseExpression(_ANCHOR_WORD);

		foreach (string surrounding in _surroundingTexts)
		{
			string text = surrounding + word + surrounding;

			Assert.Equal(expression.IsMatch(text), CandidatePreselection.IsCandidate(analyzer, text));
		}
	}

	private static Regex BuildIgnoreCaseExpression(string literal)
	{
		return new Regex(
			Regex.Escape(literal),
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
			TimeSpan.FromSeconds(1));
	}

	private static ILanguageAnalyzer BuildAnalyzer(string anchorText)
	{
		CandidateSelectionAndRecommendation selection = new()
		{
			PreselectionAnchors = new[]
			{
				new PreselectionAnchor { Text = anchorText, Sensitivity = CaseSensitivity.Insensitive }
			}.ToFrozenSet(),
			TemplateProvider = (_, _) => null,
			FlowSignals = ImmutableArray<FlowSignal>.Empty
		};
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.CandidateSelectionAndRecommendation).Returns(selection);

		return analyzer.Object;
	}
}
