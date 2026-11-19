using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE IDENTIFICATION AND THE CONTRACT STANDING OF THE SHIPPED `Python` ANALYZER: the two extensions in an <see cref="StringComparer.OrdinalIgnoreCase"/> set and no other, the language name `Python`, a first-line
/// rule that is the analyzer's own, is case-sensitive and carries the lexical budget as its timeout, no file dialects, and the validator's verdict on every element the analyzer declares at this point.
/// </summary>
public sealed class PythonAnalyzerIdentificationTests
{
	private const string _LANGUAGE_NAME = "Python";

	private static readonly ILanguageAnalyzer _analyzer = PythonShippedAnalyzer.Instance;

	[Theory]
	[InlineData(".py")]
	[InlineData(".pyw")]
	[InlineData(".PY")]
	[InlineData(".Pyw")]
	public void TheTwoExtensionsAreOwnedWithoutCase(string extension)
	{
		Assert.Contains(extension, (ISet<string>)_analyzer.LanguageIdentification.FileExtensions);
	}

	[Theory]
	[InlineData(".pyi")]
	[InlineData(".ipynb")]
	[InlineData(".pyx")]
	[InlineData(".pyc")]
	[InlineData(".go")]
	[InlineData(".sh")]
	[InlineData("")]
	public void NoOtherExtensionIsOwned(string extension)
	{
		Assert.DoesNotContain(extension, (ISet<string>)_analyzer.LanguageIdentification.FileExtensions);
	}

	[Theory]
	[InlineData(".py")]
	[InlineData(".pyw")]
	public void TheEmbeddedCatalogHandsTheAnalyzerOverForItsExtensions(string extension)
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();

		Assert.True(catalog.TryGetAnalyzerForFileExtension(extension, out ILanguageAnalyzer? found));
		Assert.Equal([_LANGUAGE_NAME], found!.LanguageIdentification.DeclaredLanguageNames.ToArray());
	}

	[Fact]
	public void TheAnalyzerDeclaresThePythonLanguageNameAndDerivesItForEveryFile()
	{
		Assert.Equal([_LANGUAGE_NAME], _analyzer.LanguageIdentification.DeclaredLanguageNames.ToArray());
		Assert.Equal(_LANGUAGE_NAME, _analyzer.LanguageIdentification.LanguageNameDerivation(".py", null));
		Assert.Equal(_LANGUAGE_NAME, _analyzer.LanguageIdentification.LanguageNameDerivation(string.Empty, "#!/usr/bin/env python3"));
	}

	[Fact]
	public void TheFirstLineRuleIsCaseSensitiveAndCarriesTheLexicalBudgetAsItsTimeout()
	{
		Regex expression = _analyzer.LanguageIdentification.FirstLineDetectionExpression!;

		Assert.NotNull(expression);
		Assert.Equal(LexicalEnginePolicy.LEXICAL_BUDGET, expression.MatchTimeout);
		Assert.Equal(RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, expression.Options);
		Assert.DoesNotContain(
			AnalyzerContractViolationCode.FirstLineDetectionMatchTimeoutNotLexicalBudget,
			AnalyzerContractValidator.Validate(_analyzer).Select(violation => violation.Code));
	}

	[Theory]
	[InlineData("#!/usr/bin/python")]
	[InlineData("#!/usr/bin/python2")]
	[InlineData("#!/usr/bin/python3")]
	[InlineData("#!/usr/bin/python3.11")]
	[InlineData("#!/usr/bin/env python3")]
	[InlineData("#!/usr/bin/env -S python3")]
	[InlineData("#! /usr/bin/env python")]
	[InlineData("#!python3")]
	public void TheFirstLineRuleClaimsAFileWhoseShebangNamesAPythonInterpreter(string firstLine)
	{
		Assert.Matches(_analyzer.LanguageIdentification.FirstLineDetectionExpression!, firstLine);
	}

	[Theory]
	[InlineData("#!/usr/bin/python2.7")]
	[InlineData("#!/usr/bin/pypy")]
	[InlineData("#!/usr/bin/env VAR=1 python")]
	[InlineData("#!/usr/bin/python -S")]
	[InlineData("#!/usr/bin/env python -S")]
	[InlineData("#!/usr/bin/Python3")]
	[InlineData("#!/usr/bin/python3.x")]
	[InlineData("#!/usr/bin/env bash")]
	[InlineData("# /usr/bin/python3")]
	[InlineData("/usr/bin/python3")]
	public void TheFirstLineRuleDoesNotClaimTheDeclaredFormsNorWhatTheBaseTableDoesNotAttributeToPython(string firstLine)
	{
		Assert.DoesNotMatch(_analyzer.LanguageIdentification.FirstLineDetectionExpression!, firstLine);
	}

	[Fact]
	public void TheAnalyzerDeclaresNoFileDialectsNoConstructPrecedenceAndNoWrapperTracking()
	{
		Assert.Null(_analyzer.CandidateSelectionAndRecommendation.FileDialects);
		Assert.Null(_analyzer.CandidateSelectionAndRecommendation.WrapperTracking);
		Assert.Null(_analyzer.ConstructPrecedence);
	}

	[Fact]
	public void TheValidatorReportsNothingAgainstTheElementsTheAnalyzerDeclaresAtThisPoint()
	{
		AnalyzerContractViolationCode[] declaredByLaterParts =
		[
			AnalyzerContractViolationCode.DetectionRuleSetEmpty,
			AnalyzerContractViolationCode.PreselectionAnchorSetEmpty
		];

		AnalyzerContractViolationCode[] unexpected =
		[
			.. AnalyzerContractValidator.Validate(_analyzer).Select(violation => violation.Code).Except(declaredByLaterParts)
		];

		Assert.Empty(unexpected);
	}
}
