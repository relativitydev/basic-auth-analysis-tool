using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Catalog;

/// <summary>
/// THE EXTENSIONLESS FILES AND THE CANDIDATE COUNT OF THE SHIPPED `Python` ANALYZER.
///
/// An extensionless file is attributed only through the analyzer's own first-line rule, so a base-table hit alone never yields the analyzer: the declared forms (`python2.{digits}`, `pypy`, `env VAR=1 python`, `python -S`)
/// and a file with no first line are counted and not assessed. An accepted file is then read like any other: a High construct in it is a finding. The candidate count is the catalog's fixed set of eight files, of which five
/// are candidates: a file with no anchor is never analyzed, and the breadth of the anchor `auth` (the word `author` admits a file) is the declared cost.
/// </summary>
public sealed class PythonExtensionlessAndCandidateTests
{
	private const string _LANGUAGE_NAME = "Python";
	private const string _HIGH_CONSTRUCT = "response = requests.get(url, auth=HTTPBasicAuth(\"user\", \"secret\"))\n";
	private const int _FIXTURE_FILE_COUNT = 8;
	private const int _EXPECTED_CANDIDATE_COUNT = 5;

	// ------------------------------------------------------------------------------------------
	// Extensionless files
	// ------------------------------------------------------------------------------------------

	[Theory]
	[InlineData("#!/usr/bin/python")]
	[InlineData("#!/usr/bin/python2")]
	[InlineData("#!/usr/bin/python3")]
	[InlineData("#!/usr/bin/python3.11")]
	[InlineData("#!/usr/bin/env python3")]
	[InlineData("#!/usr/bin/env -S python3")]
	[InlineData("#! /usr/bin/env python")]
	[InlineData("#!python3")]
	public void AnExtensionlessFileWhoseFirstLineNamesAPythonInterpreterIsAttributedToThePythonAnalyzer(string firstLine)
	{
		AttributedLanguage attribution = Attribute("bin/run", firstLine);

		Assert.True(attribution.IsRecognized, firstLine);
		Assert.Equal(_LANGUAGE_NAME, attribution.LanguageName);
		Assert.Contains(_LANGUAGE_NAME, (ISet<string>)attribution.Analyzer!.LanguageIdentification.DeclaredLanguageNames);
	}

	[Theory]
	[InlineData("#!/usr/bin/python2.7")]
	[InlineData("#!/usr/bin/pypy")]
	[InlineData("#!/usr/bin/pypy3")]
	[InlineData("#!/usr/bin/env VAR=1 python3")]
	[InlineData("#!/usr/bin/python -S")]
	[InlineData("#!/usr/bin/env python -S")]
	[InlineData("#!/usr/bin/Python3")]
	[InlineData("#!/usr/bin/python3.x")]
	[InlineData("# python3")]
	[InlineData("import python3")]
	[InlineData("")]
	public void AnExtensionlessFileWithADeclaredOrUnattributedFirstLineIsNotAssessedAndNeverByThePythonAnalyzer(string firstLine)
	{
		AttributedLanguage attribution = Attribute("bin/run", firstLine);

		Assert.False(attribution.IsRecognized, firstLine);
		Assert.Null(attribution.Analyzer);
	}

	[Fact]
	public void AnExtensionlessFileWithNoFirstLineAtAllIsNotAssessed()
	{
		AttributedLanguage attribution = Attribute("bin/run", firstLine: null);

		Assert.False(attribution.IsRecognized);
		Assert.Equal(LanguageAttribution.UNRECOGNIZED_LANGUAGE_NAME, attribution.LanguageName);
	}

	[Theory]
	[InlineData("#!/bin/bash", "Shell")]
	[InlineData("#!/usr/bin/env sh", "Shell")]
	[InlineData("#!/usr/bin/env node", "JavaScript")]
	[InlineData("#!/usr/bin/env pwsh", "PowerShell")]
	public void TheShebangOfAnotherAnalyzedLanguageIsStillAttributedToItsOwnAnalyzer(string firstLine, string languageName)
	{
		AttributedLanguage attribution = Attribute("bin/run", firstLine);

		Assert.True(attribution.IsRecognized, firstLine);
		Assert.Equal(languageName, attribution.LanguageName);
		Assert.NotEqual(_LANGUAGE_NAME, attribution.LanguageName);
	}

	[Theory]
	[InlineData("src/client.py")]
	[InlineData("src/client.pyw")]
	[InlineData("src/CLIENT.PY")]
	public void AFileWithAPythonExtensionIsAttributedWhateverItsFirstLineSays(string path)
	{
		Assert.Equal(_LANGUAGE_NAME, Attribute(path, "#!/bin/sh").LanguageName);
		Assert.Equal(_LANGUAGE_NAME, Attribute(path, firstLine: null).LanguageName);
		Assert.True(Attribute(path, "#!/bin/sh").IsRecognized);
	}

	[Theory]
	[InlineData("bin/run", "#!/usr/bin/env python3")]
	[InlineData("bin/deploy", "#!/usr/bin/python")]
	[InlineData("scripts/job", "#!/usr/bin/env -S python3")]
	public void AHighConstructInAnExtensionlessPythonScriptIsAFindingAtHigh(string path, string firstLine)
	{
		string source = firstLine + "\n" + _HIGH_CONSTRUCT;
		RecognizedConstruct construct = Assert.Single(new PythonRecognitionHarness().Recognize(source, path));

		Assert.Equal("PY-001", construct.PatternClassIdentifier);
		Assert.Equal(ConfidenceLevel.High, construct.Level);
	}

	[Fact]
	public void TheShebangLineAndAFirstLineHashDoNotHideOrCreateAFindingInAnExtensionlessScript()
	{
		PythonRecognitionHarness harness = new();

		Assert.Empty(harness.Recognize("#!/usr/bin/env python3\nvalue = 1\n", "bin/run"));
		Assert.Single(harness.Recognize("#!/usr/bin/env python3\n# a comment\nsession.auth = (\"user\", \"secret\")\n", "bin/run"));
	}

	// ------------------------------------------------------------------------------------------
	// The candidate count on fixtures
	// ------------------------------------------------------------------------------------------

	[Fact]
	public void FiveOfTheEightFixtureFilesAreCandidatesAndTheOtherThreeAreNot()
	{
		ImmutableArray<(string Path, string Text, bool Candidate)> fixtures = CandidateFixtures();
		ImmutableArray<string> candidates = [.. fixtures.Where(fixture => IsCandidate(fixture.Path, fixture.Text)).Select(fixture => fixture.Path)];

		Assert.Equal(_FIXTURE_FILE_COUNT, fixtures.Length);
		Assert.Equal(_EXPECTED_CANDIDATE_COUNT, candidates.Length);
		Assert.Equal(fixtures.Where(fixture => fixture.Candidate).Select(fixture => fixture.Path).ToArray(), candidates.ToArray());
	}

	[Fact]
	public void AFixtureOfEveryClassIsACandidateThroughAnAnchor()
	{
		Assert.All(PythonAnalyzerTests.ClassFixtures(), fixture =>
			Assert.True(CandidatePreselection.IsCandidate(PythonShippedAnalyzer.Instance, fixture.Source), fixture.PatternClass));
	}

	[Theory]
	[InlineData("AUTH = 1\n")]
	[InlineData("value = base64.b64encode(data)\n")]
	[InlineData("value = base64.encodebytes(data)\n")]
	[InlineData("manager.add_password(a, b, c, d)\n")]
	[InlineData("url = \"https://example.test/\"\n")]
	public void EachOfTheFiveAnchorsAloneMakesAFileACandidateWithoutRegardToCase(string source)
	{
		Assert.True(CandidatePreselection.IsCandidate(PythonShippedAnalyzer.Instance, source));
	}

	[Fact]
	public void BasicAloneIsNotAnAnchorAndAFileThatNamesNoAnchorIsNotACandidate()
	{
		Assert.False(CandidatePreselection.IsCandidate(PythonShippedAnalyzer.Instance, "value = \"Basic\"\nprint(value)\n"));
		Assert.False(CandidatePreselection.IsCandidate(PythonShippedAnalyzer.Instance, "import base64\nvalue = base64.b64decode(data)\n"));
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	/// <summary>The fixed set of eight files of the catalog: the path, the text and whether the file is a candidate.</summary>
	private static ImmutableArray<(string Path, string Text, bool Candidate)> CandidateFixtures()
	{
		return
		[
			("client.py", "response = session.get(url, auth=credentials)\n", true),
			("plain.py", "total = first + second\nprint(total)\n", false),
			("encoder.py", "value = base64.b64encode(raw)\n", true),
			("urls.py", "BASE = \"https://example.test/\"\n", true),
			("author.py", "author = \"a name\"\n", true),
			("notes.txt", "auth notes\n", false),
			("run", "#!/usr/bin/env python3\nauth = None\n", true),
			("run2", "auth = None\n", false)
		];
	}

	private static bool IsCandidate(string path, string text)
	{
		string firstLine = text.Split('\n')[0];
		AttributedLanguage attribution = Attribute(path, firstLine);

		return attribution.IsRecognized && attribution.Analyzer!.LanguageIdentification.DeclaredLanguageNames.Contains(_LANGUAGE_NAME) && CandidatePreselection.IsCandidate(attribution.Analyzer, text);
	}

	private static AttributedLanguage Attribute(string relativePath, string? firstLine)
	{
		PythonRecognitionHarness harness = new();
		LanguageAttribution attribution = new(LanguageCatalogRegistry.BuildEmbeddedCatalog());

		return attribution.Attribute(harness.PathOf(relativePath), firstLine);
	}
}
