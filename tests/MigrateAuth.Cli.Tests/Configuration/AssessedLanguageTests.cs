using MigrateAuth.Cli.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Configuration;

/// <summary>
/// The eight-value language enumeration and the three-value confidence scale, which are the two
/// closed vocabularies a configuration file is written in.
///
/// The count is asserted rather than assumed. A recall floor is set per value here
/// and the report's coverage statement iterates these values rather than the catalog registry, so a
/// language quietly missing from the enumeration is a language no report ever mentions - which is
/// exactly the failure the coverage statement exists to prevent.
/// </summary>
public sealed class AssessedLanguageTests
{
	private const string _UNKNOWN_LANGUAGE_NAME = "Fortran";
	private const string _UNKNOWN_LEVEL_NAME = "Critical";

	[Fact]
	public void TheEnumerationDeclaresTheEightLanguagesTheProductAssesses()
	{
		string[] canonicalNames =
		[
			.. AssessedLanguages.AllInDeclaredOrder.Select(AssessedLanguages.CanonicalName)
		];

		Assert.Equal(AssessedLanguages.SUPPORTED_LANGUAGE_COUNT, canonicalNames.Length);
		Assert.Equal(
			["CSharp", "VBNet", "JavaScript", "TypeScript", "Python", "Go", "PowerShell", "Shell"],
			canonicalNames);
	}

	[Fact]
	public void EveryDeclaredLanguageIsPresentExactlyOnce()
	{
		Assert.Equal(
			AssessedLanguages.AllInDeclaredOrder.Length,
			AssessedLanguages.AllInDeclaredOrder.Distinct().Count());
	}

	[Theory]
	[InlineData("CSharp", AssessedLanguage.CSharp)]
	[InlineData("csharp", AssessedLanguage.CSharp)]
	[InlineData("  PowerShell  ", AssessedLanguage.PowerShell)]
	[InlineData("VBNET", AssessedLanguage.VBNet)]
	public void ACanonicalNameParsesBackToItsLanguage(string writtenName, AssessedLanguage expected)
	{
		Assert.True(AssessedLanguages.TryParse(writtenName, out AssessedLanguage parsed));
		Assert.Equal(expected, parsed);
	}

	/// <summary>
	/// A name this build does not know is not a failure. The configuration read tolerates version
	/// differences, and a language a later release adds is exactly that.
	/// </summary>
	[Fact]
	public void AnUnknownLanguageNameDoesNotParseAndIsNotAnError()
	{
		Assert.False(AssessedLanguages.TryParse(_UNKNOWN_LANGUAGE_NAME, out _));
		Assert.False(AssessedLanguages.TryParse(string.Empty, out _));
	}

	[Fact]
	public void TheWidestThresholdIsTheOneThatHidesNothing()
	{
		Assert.Equal(ConfidenceLevel.Low, ConfidenceLevels.WIDEST);
	}

	[Theory]
	[InlineData(ConfidenceLevel.High, ConfidenceLevel.Low, ConfidenceLevel.Low)]
	[InlineData(ConfidenceLevel.Medium, ConfidenceLevel.High, ConfidenceLevel.Medium)]
	[InlineData(ConfidenceLevel.Medium, ConfidenceLevel.Medium, ConfidenceLevel.Medium)]
	public void TheWidestOfTwoThresholdsIsTheOneThatReportsMore(
		ConfidenceLevel first,
		ConfidenceLevel second,
		ConfidenceLevel expected)
	{
		Assert.Equal(expected, ConfidenceLevels.Widest(first, second));
	}

	[Fact]
	public void EveryConfidenceLevelParsesBackFromItsName()
	{
		foreach (ConfidenceLevel level in Enum.GetValues<ConfidenceLevel>())
		{
			Assert.True(ConfidenceLevels.TryParse(ConfidenceLevels.Name(level), out ConfidenceLevel parsed));
			Assert.Equal(level, parsed);
		}
	}

	/// <summary>
	/// The contrast with a language name. The rubric defines three levels and that vocabulary does
	/// not grow with releases, so a value outside it cannot be honoured or safely guessed.
	/// </summary>
	[Fact]
	public void AnUnknownConfidenceLevelNameDoesNotParse()
	{
		Assert.False(ConfidenceLevels.TryParse(_UNKNOWN_LEVEL_NAME, out _));
	}
}
