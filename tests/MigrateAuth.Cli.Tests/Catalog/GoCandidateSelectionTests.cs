using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Reporting;
using MigrateAuth.Cli.Tests.Scanning;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// WHAT THE ENGINE DOES WITH THE `Go` ANALYZER'S PRESELECTION ANCHORS, AND HOW `Go` IS REGISTERED.
///
/// A file with no anchor is never analyzed, so every class must bring its own anchor: a fixture of each class is a candidate, and a file that holds each of the six anchors alone, and nothing else
/// that is an anchor, is a candidate too. The words a flow signal or a class reads but that no class needs to be admitted by (<c>Basic</c>, <c>EncodeToString</c>, <c>Getenv</c>) are not anchors, so a
/// file that holds only them is not. The second part states the registration: the language is assessed, the report and the console say so, and the language can be disabled by configuration.
/// </summary>
public sealed class GoCandidateSelectionTests
{
	private const int _CLASS_COUNT = 13;

	private const string _GO_FILE_NAME = "main.go";
	private const string _ANCHOR_ALONE_TEMPLATE = "package sample\n\n// {0}\n";

	[Fact]
	public void AFixtureOfEveryClassIsACandidateThroughAnAnchor()
	{
		ILanguageAnalyzer analyzer = GoRegisteredExpressions.Analyzer;

		Assert.All(GoAnalyzerTests.ClassFixtures(), fixture => Assert.True(CandidatePreselection.IsCandidate(analyzer, fixture.Source), fixture.PatternClass));
	}

	[Theory]
	[InlineData("SetBasicAuth")]
	[InlineData("Authorization")]
	[InlineData("UserPassword")]
	[InlineData("StdEncoding")]
	[InlineData("://")]
	[InlineData("RoundTrip")]
	public void AFileThatHoldsOneAnchorAndNoOtherIsACandidate(string anchor)
	{
		string source = string.Format(System.Globalization.CultureInfo.InvariantCulture, _ANCHOR_ALONE_TEMPLATE, anchor);

		Assert.True(CandidatePreselection.IsCandidate(GoRegisteredExpressions.Analyzer, source));
	}

	[Theory]
	[InlineData("authorization")]
	[InlineData("AUTHORIZATION")]
	[InlineData("setbasicauth")]
	[InlineData("userpassword")]
	[InlineData("stdencoding")]
	[InlineData("roundtrip")]
	[InlineData("RawStdEncoding")]
	public void AnAnchorIsFoundWithoutRegardToCaseAndInsideALongerWord(string anchor)
	{
		string source = string.Format(System.Globalization.CultureInfo.InvariantCulture, _ANCHOR_ALONE_TEMPLATE, anchor);

		Assert.True(CandidatePreselection.IsCandidate(GoRegisteredExpressions.Analyzer, source));
	}

	[Theory]
	[InlineData("package sample\n\nfunc main() {\n\tfmt.Println(\"hello\")\n}\n")]
	[InlineData("package sample\n\nvar scheme = \"Basic\"\n")]
	[InlineData("package sample\n\nvar text = base64.URLEncoding.EncodeToString(raw)\n")]
	[InlineData("package sample\n\nvar port = os.Getenv(\"PORT\")\n")]
	[InlineData("package sample\n\nvar password, err = term.ReadPassword(fd)\n")]
	[InlineData("package sample\n\nvar value = request.FormValue(\"password\")\n")]
	[InlineData("package sample\n\nvar user = request.BasicAuth\n")]
	[InlineData("")]
	public void AFileThatNamesNoAnchorIsNotACandidate(string source)
	{
		Assert.False(CandidatePreselection.IsCandidate(GoRegisteredExpressions.Analyzer, source));
	}

	// ------------------------------------------------------------------------------------------
	// Go is assessed, shown by the console and the coverage, and can be disabled
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void GoIsAssessedByTheEmbeddedCatalogAndDisabledByConfigurationWhenTheConfigurationSaysSo()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		HashSet<AssessedLanguage> allEnabled = [.. AssessedLanguages.AllInDeclaredOrder];
		HashSet<AssessedLanguage> goDisabled = [.. AssessedLanguages.AllInDeclaredOrder.Where(language => language != AssessedLanguage.Go)];
		string goName = AssessedLanguages.CanonicalName(AssessedLanguage.Go);

		Assert.Equal(LanguageAssessmentState.Assessed, LanguageAssessment.StateOf(catalog, goName, allEnabled));
		Assert.Equal(LanguageAssessmentState.DisabledByConfiguration, LanguageAssessment.StateOf(catalog, goName, goDisabled));
	}

	[Fact]
	public void TheCoverageStatementNamesGoAsSupportedWithItsThirteenClassesAndAsDisabledWhenItIsDisabled()
	{
		LanguageCatalog catalog = LanguageCatalogRegistry.BuildEmbeddedCatalog();
		ImmutableArray<LanguageCoverageRow> enabled = CoverageStatement.Compose(catalog, ReportHarness.ConfigurationWith(ConfidenceLevel.Low));
		ImmutableArray<LanguageCoverageRow> disabled = CoverageStatement.Compose(catalog, ReportHarness.ConfigurationWith(ConfidenceLevel.Low, AssessedLanguage.Go));
		LanguageCoverageRow supported = enabled.Single(row => row.Language == AssessedLanguage.Go);
		LanguageCoverageRow notSearched = disabled.Single(row => row.Language == AssessedLanguage.Go);

		Assert.Equal(LanguageCoverageState.Supported, supported.State);
		Assert.Equal(_CLASS_COUNT, supported.PatternClassesSearched);
		Assert.Equal(46, supported.DeclaredUnsupportedConstructs.Length);
		Assert.Equal(LanguageCoverageState.DisabledByConfiguration, notSearched.State);
	}

	[Fact]
	public void TheConsoleInventoryMarksGoAsAssessedOnThisChain()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_GO_FILE_NAME, "package sample\n\n// SetBasicAuth marker\n");

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		string goLine = harness.Output.ToString().Split('\n').Single(line => line.TrimStart().StartsWith(AssessedLanguages.CanonicalName(AssessedLanguage.Go), StringComparison.Ordinal));

		Assert.DoesNotContain(ScanStage.UNASSESSED_ROW_SUFFIX, goLine, StringComparison.Ordinal);
	}

	[Fact]
	public void AGoFileWithAnAnchorBecomesACandidateAndAGoFileWithNoAnchorDoesNot()
	{
		using ScanHarness harness = ScanHarness.CreateEmpty();
		harness.Fixture.WriteText(_GO_FILE_NAME, "package sample\n\n// SetBasicAuth marker\n");
		harness.Fixture.WriteText("plain.go", "package sample\n\nfunc plain() {}\n");

		harness.BuildStage().Run(harness.State, CancellationToken.None);

		Assert.Equal([_GO_FILE_NAME], harness.State.CandidateRelativePaths);
	}
}
