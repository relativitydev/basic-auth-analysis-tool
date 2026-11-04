using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE IDENTIFICATION, THE DIALECTS AND THE CONTRACT STANDING OF THE SHIPPED `Shell` ANALYZER: the four extensions in an
/// <see cref="StringComparer.OrdinalIgnoreCase"/> set, the language name `Shell`, a first-line rule that carries the lexical budget as its timeout,
/// the declared dialect map, and the validator's verdict on every element the analyzer declares.
/// </summary>
public sealed class ShellAnalyzerIdentificationTests
{
	private const string _LANGUAGE_NAME = "Shell";

	private static readonly ILanguageAnalyzer _analyzer = ShellRegisteredExpressions.Analyzer;

	[Theory]
	[InlineData(".sh")]
	[InlineData(".bash")]
	[InlineData(".zsh")]
	[InlineData(".ksh")]
	[InlineData(".SH")]
	[InlineData(".Bash")]
	[InlineData(".ZSH")]
	[InlineData(".KsH")]
	public void TheFourExtensionsAreOwnedWithoutCase(string extension)
	{
		Assert.Contains(extension, (ISet<string>)_analyzer.LanguageIdentification.FileExtensions);
	}

	[Theory]
	[InlineData(".bats")]
	[InlineData(".fish")]
	[InlineData(".csh")]
	[InlineData(".bat")]
	[InlineData(".ps1")]
	[InlineData(".bashrc")]
	[InlineData("")]
	public void NoOtherExtensionIsOwned(string extension)
	{
		Assert.DoesNotContain(extension, (ISet<string>)_analyzer.LanguageIdentification.FileExtensions);
	}

	[Fact]
	public void TheAnalyzerDeclaresTheShellLanguageNameAndDerivesItForEveryFile()
	{
		Assert.Equal([_LANGUAGE_NAME], _analyzer.LanguageIdentification.DeclaredLanguageNames.ToArray());
		Assert.Equal(_LANGUAGE_NAME, _analyzer.LanguageIdentification.LanguageNameDerivation(".sh", null));
		Assert.Equal(_LANGUAGE_NAME, _analyzer.LanguageIdentification.LanguageNameDerivation(string.Empty, "#!/bin/bash"));
		Assert.Equal(_LANGUAGE_NAME, _analyzer.LanguageIdentification.LanguageNameDerivation(".ksh", "#!/bin/ksh"));
	}

	[Fact]
	public void TheFirstLineRuleCarriesTheLexicalBudgetAsItsTimeout()
	{
		Assert.NotNull(_analyzer.LanguageIdentification.FirstLineDetectionExpression);
		Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, _analyzer.LanguageIdentification.FirstLineDetectionExpression!.MatchTimeout);
		Assert.DoesNotContain(
			AnalyzerContractViolationCode.FirstLineDetectionMatchTimeoutNotLexicalBudget,
			AnalyzerContractValidator.Validate(_analyzer).Select(violation => violation.Code));
	}

	[Theory]
	[InlineData("#!/bin/sh")]
	[InlineData("#!/bin/bash")]
	[InlineData("#!/usr/bin/env zsh")]
	[InlineData("#!/usr/bin/env -S ksh -e")]
	public void TheFirstLineRuleClaimsAFileWhoseShebangNamesAShell(string firstLine)
	{
		Assert.Matches(_analyzer.LanguageIdentification.FirstLineDetectionExpression!, firstLine);
	}

	[Theory]
	[InlineData("#!/bin/dash")]
	[InlineData("#!/bin/ash")]
	[InlineData("#!/usr/bin/env python3")]
	[InlineData("#!/usr/bin/env pwsh")]
	[InlineData("# /bin/sh")]
	[InlineData("/bin/sh")]
	public void TheFirstLineRuleDoesNotClaimWhatTheBaseDoesNotAttributeToShell(string firstLine)
	{
		Assert.DoesNotMatch(_analyzer.LanguageIdentification.FirstLineDetectionExpression!, firstLine);
	}

	[Fact]
	public void TheDialectsAreTheDeclaredOnes()
	{
		FileDialects dialects = _analyzer.CandidateSelectionAndRecommendation.FileDialects!;

		Assert.NotNull(dialects);
		Assert.Equal("bash", dialects.DefaultDialect);
		Assert.Equal("bash", dialects.ShebangInterpreters["bash"]);
		Assert.Equal("bash", dialects.ShebangInterpreters["sh"]);
		Assert.Equal("zsh", dialects.ShebangInterpreters["zsh"]);
		Assert.Equal("ksh93", dialects.ShebangInterpreters["ksh"]);
		Assert.Equal("mksh", dialects.ShebangInterpreters["mksh"]);
		Assert.Equal("zsh", dialects.ExtensionDialects[".zsh"]);
		Assert.Equal("ksh93", dialects.ExtensionDialects[".ksh"]);
		Assert.False(dialects.ExtensionDialects.ContainsKey(".sh"));
		Assert.False(dialects.ExtensionDialects.ContainsKey(".bash"));
	}

	[Fact]
	public void TheValidatorReportsNothingAgainstTheElementsDeclaredSoFar()
	{
		AnalyzerContractViolationCode[] notYetDeclared =
		[
			AnalyzerContractViolationCode.DetectionRuleSetEmpty,
			AnalyzerContractViolationCode.PreselectionAnchorSetEmpty
		];

		IEnumerable<AnalyzerContractViolation> unexpected = AnalyzerContractValidator.Validate(_analyzer).Where(violation => !notYetDeclared.Contains(violation.Code));

		Assert.Empty(unexpected);
	}
}
