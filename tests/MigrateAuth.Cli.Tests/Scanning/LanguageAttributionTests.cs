using System.Collections.Frozen;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Scanning;

using MigrateAuth.Cli.Tests.Recognition;

using Moq;

using Xunit;

namespace MigrateAuth.Cli.Tests.Scanning;

/// <summary>
/// the detection, over the reference profile's own two extensions and its first-line
/// rule. The names asserted are the catalog's, not this file's invention - which is the point:
/// nothing here would change if the profile were a real language.
/// </summary>
public sealed class LanguageAttributionTests
{
	private const string _FIRST_LANGUAGE_NAME = "ExemplarOne";
	private const string _SECOND_LANGUAGE_NAME = "ExemplarTwo";
	private const string _FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE = "#!/usr/bin/env exemplar";
	private const double _FRACTION_OF_THE_BUDGET_A_SINGLE_ATTEMPT_MAY_SPEND = 0.6;
	private const int _ONE_MILLISECOND_TIMEOUT = 1;
	private const int _CATASTROPHIC_REPETITION_COUNT = 60;
	private const string _CATASTROPHIC_FIRST_LINE_PATTERN = "^(?:a|aa)+$";
	private const string _CATASTROPHIC_LANGUAGE_NAME = "CatastrophicProbe";

	[Fact]
	public void TheExtensionAttributesTheFile()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(harness.PathOf("source.ex1"), firstLine: null);

		Assert.Equal(_FIRST_LANGUAGE_NAME, attributed.LanguageName);
		Assert.True(attributed.IsRecognized);
	}

	[Fact]
	public void TheSecondExtensionAttributesTheSecondLanguageOfTheSameProfile()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(harness.PathOf("source.ex2"), firstLine: null);

		Assert.Equal(_SECOND_LANGUAGE_NAME, attributed.LanguageName);
	}

	/// <summary>
	/// The first line wins over the extension, which is what the detection rule asks for and what the
	/// analyzer's own derivation decides - the engine hands it the first line and takes the answer.
	/// </summary>
	[Fact]
	public void TheFirstLineOverridesTheExtensionWhereTheAnalyzerSaysItDoes()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("source.ex2"),
			_FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE);

		Assert.Equal(_FIRST_LANGUAGE_NAME, attributed.LanguageName);
	}

	[Fact]
	public void AFileWithNoExtensionIsAttributedByItsFirstLine()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("build-script"),
			_FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE);

		Assert.Equal(_FIRST_LANGUAGE_NAME, attributed.LanguageName);
	}

	[Fact]
	public void AFileWithNoExtensionAndNoMatchingFirstLineIsUnrecognized()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(harness.PathOf("LICENSE"), "Copyright notice");

		Assert.Equal(LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, attributed.LanguageName);
		Assert.False(attributed.IsRecognized);
	}

	/// <summary>
	/// The detection rule scopes the first-line fallback to files WITHOUT an extension. A file whose
	/// extension no analyzer claims is unrecognized however its first line reads, and widening
	/// that would let a first line claim a file whose extension already answered the question.
	/// </summary>
	[Fact]
	public void AnUnclaimedExtensionIsUnrecognizedEvenWithAMatchingFirstLine()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("notes.txt"),
			_FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE);

		Assert.Equal(LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, attributed.LanguageName);
	}

	/// <summary>
	/// An unrecognized file carries no analyzer, which is how the exclusion from candidate
	/// selection is enforced: there are no anchors to test it against.
	/// </summary>
	[Fact]
	public void AnUnrecognizedFileCarriesNoAnalyzer()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(harness.PathOf("notes.txt"), firstLine: null);

		Assert.Null(attributed.Analyzer);
	}

	/// <summary>
	/// The extension comparison is the catalog's, and the catalog indexes extensions without case.
	/// A repository written on a case-insensitive volume must not inventory differently on a
	/// case-sensitive one.
	/// </summary>
	[Fact]
	public void TheExtensionIsMatchedWithoutCase()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(harness.PathOf("SOURCE.EX1"), firstLine: null);

		Assert.True(attributed.IsRecognized);
	}

	/// <summary>
	/// A language the detection rule claims by extension alone, with no analyzer to have registered
	/// it: the shared engine's own table names it in the inventory, and it still carries no analyzer, so it
	/// is excluded from candidate selection exactly like an unrecognized file.
	/// </summary>
	[Fact]
	public void ABaseTableExtensionNamesTheLanguageWithNoAnalyzer()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(harness.PathOf("script.py"), firstLine: null);

		Assert.Equal(AssessedLanguages.CanonicalName(AssessedLanguage.Python), attributed.LanguageName);
		Assert.False(attributed.IsRecognized);
		Assert.Null(attributed.Analyzer);
	}

	/// <summary>An extensionless file's shebang, read through the shared engine's own table.</summary>
	[Fact]
	public void ABaseTableShebangNamesAnExtensionlessFile()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("build-script"),
			"#!/usr/bin/env bash");

		Assert.Equal(AssessedLanguages.CanonicalName(AssessedLanguage.Shell), attributed.LanguageName);
		Assert.False(attributed.IsRecognized);
	}

	/// <summary>
	/// The detection rule scopes the shebang fallback to files with NO extension, exactly as it
	/// already scopes a registered analyzer's own first-line rule - a `.py` file is Python by its
	/// extension however its first line reads, and a Shell shebang inside it never overrides that.
	/// </summary>
	[Fact]
	public void AKnownExtensionIsNeverOverriddenByTheBaseShebangTable()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("script.py"),
			"#!/usr/bin/env bash");

		Assert.Equal(AssessedLanguages.CanonicalName(AssessedLanguage.Python), attributed.LanguageName);
	}

	/// <summary>
	/// The shared engine's table is consulted only once no registered analyzer claims the extension -
	/// C# is a real analyzer in the embedded catalog, so a `.cs` file is attributed through it and
	/// never reaches <see cref="Scanning.BaseLanguageDetection"/>'s own `.cs` entry.
	/// </summary>
	[Fact]
	public void ARegisteredAnalyzersExtensionStillWinsOverTheBaseTable()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(harness.PathOf("Program.cs"), firstLine: null);

		Assert.Equal(AssessedLanguages.CanonicalName(AssessedLanguage.CSharp), attributed.LanguageName);
		Assert.True(attributed.IsRecognized);
		Assert.NotNull(attributed.Analyzer);
	}

	/// <summary>An extension the shared engine's table does not declare either stays unrecognized.</summary>
	[Fact]
	public void AnExtensionOutsideBothTablesStaysUnrecognized()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog);

		AttributedLanguage attributed = attribution.Attribute(harness.PathOf("notes.rb"), firstLine: null);

		Assert.Equal(LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, attributed.LanguageName);
	}

	/// <summary>
	/// A first line that spends the lexical budget is treated as not recognized by its first
	/// line: the file is attributed by its extension - here none, so it falls to unrecognized - and the
	/// fallback is carried on the attribution so the inventory can count it.
	/// </summary>
	[Fact]
	public void AFirstLineThatSpendsTheBudgetFallsBackToTheExtension()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog, new AdvancingTimeProvider(LexicalEnginePolicy.LEXICAL_BUDGET));

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("build-script"),
			_FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE);

		Assert.Equal(LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, attributed.LanguageName);
		Assert.False(attributed.IsRecognized);
		Assert.True(attributed.FirstLineBudgetExceeded);
	}

	[Fact]
	public void AFirstLineWithinTheBudgetAttributesTheFileAndIsNotAFallback()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog, new FrozenTimeProvider());

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("build-script"),
			_FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE);

		Assert.Equal(_FIRST_LANGUAGE_NAME, attributed.LanguageName);
		Assert.False(attributed.FirstLineBudgetExceeded);
	}

	/// <summary>A file with an extension never has its first line evaluated, so the budget is never charged for it.</summary>
	[Fact]
	public void AFileWithAnExtensionIsNeverChargedAgainstTheFirstLineBudget()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageAttribution attribution = new(harness.Catalog, new AdvancingTimeProvider(LexicalEnginePolicy.LEXICAL_BUDGET));

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("source.ex2"),
			_FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE);

		Assert.Equal(_FIRST_LANGUAGE_NAME, attributed.LanguageName);
		Assert.False(attributed.FirstLineBudgetExceeded);
	}

	/// <summary>The budget is one per file: the time one file spent does not count against the next.</summary>
	[Fact]
	public void TheFirstLineBudgetIsPerFileAndNotPerAttribution()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		TimeSpan step = LexicalEnginePolicy.LEXICAL_BUDGET * _FRACTION_OF_THE_BUDGET_A_SINGLE_ATTEMPT_MAY_SPEND;
		LanguageAttribution attribution = new(harness.Catalog, new AdvancingTimeProvider(step));

		AttributedLanguage first = attribution.Attribute(harness.PathOf("first-script"), _FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE);
		AttributedLanguage second = attribution.Attribute(harness.PathOf("second-script"), _FIRST_LINE_CLAIMING_THE_FIRST_LANGUAGE);

		Assert.Equal(_FIRST_LANGUAGE_NAME, first.LanguageName);
		Assert.Equal(_FIRST_LANGUAGE_NAME, second.LanguageName);
		Assert.False(first.FirstLineBudgetExceeded);
		Assert.False(second.FirstLineBudgetExceeded);
	}

	/// <summary>
	/// A first-line expression that exceeds its own regex timeout is a trip as well, under a clock
	/// that never moves - so nothing but the timeout can be what tripped it.
	/// </summary>
	[Fact]
	public void AFirstLineExpressionThatExceedsItsOwnTimeoutIsATrip()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		LanguageCatalog catalog = new(LanguageCatalog.CurrentVersion, [BuildAnalyzerWithACatastrophicFirstLineExpression()]);
		LanguageAttribution attribution = new(catalog, new FrozenTimeProvider());
		string catastrophicFirstLine = new string('a', _CATASTROPHIC_REPETITION_COUNT) + "!";

		AttributedLanguage attributed = attribution.Attribute(
			harness.PathOf("catastrophic-script"),
			catastrophicFirstLine);

		Assert.Equal(LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, attributed.LanguageName);
		Assert.True(attributed.FirstLineBudgetExceeded);
	}

	private static ILanguageAnalyzer BuildAnalyzerWithACatastrophicFirstLineExpression()
	{
		Mock<ILanguageAnalyzer> analyzer = new();

		analyzer.SetupGet(supplied => supplied.LanguageIdentification).Returns(new LanguageIdentification
		{
			FileExtensions = new[] { ".catastrophic" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
			DeclaredLanguageNames = new[] { _CATASTROPHIC_LANGUAGE_NAME }.ToFrozenSet(StringComparer.Ordinal),
			FirstLineDetectionExpression = new Regex(
				_CATASTROPHIC_FIRST_LINE_PATTERN,
				RegexOptions.None,
				TimeSpan.FromMilliseconds(_ONE_MILLISECOND_TIMEOUT)),
			LanguageNameDerivation = (fileExtension, firstLine) => _CATASTROPHIC_LANGUAGE_NAME,
			DeclaredUnsupportedCoverage = FrozenSet<DeclaredUnsupportedCoverage>.Empty
		});

		return analyzer.Object;
	}
}
