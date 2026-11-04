using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Tests.Recognition;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// NO PUBLIC PAGE PLACES TEST CODE AT LOW.
///
/// A finding in a file that only the test kind marks is reported at Medium at most, and a file is Low only when another kind fires with it (a mock, a sample,
/// vendored or generated code). This check reads every page of the public set sentence by
/// sentence and reports a sentence that says test code is Low without saying Medium and without naming one of the other kinds. A sentence that says test code is
/// <em>not</em> Low is not a violation. Fenced blocks, inline code and quoted text are left out: the quoted text is the tool's own wording, which the pages
/// quote, and the code is a folder or a file name.
/// </summary>
public sealed class PublicPagesTestCodeLevelTests
{
	private const string _FENCE_MARKER = "```";
	private const string _CODE_SPAN_PATTERN = "`[^`\\n]*`";
	private const string _QUOTED_TEXT_PATTERN = "\"[^\"\\n]*\"";
	private const string _SENTENCE_BREAK_PATTERN = "(?<=[.!?])[*)]*\\s+";
	private const string _TEST_TERM_PATTERN = "\\btests?\\b";
	private const string _LOW_PLACEMENT_PATTERN = "\\bLow\\b";
	private const string _NEGATION_PATTERN = "\\b(not|never|no longer)\\b";
	private const string _MEDIUM_PATTERN = "\\bMedium\\b";
	private const string _OTHER_KIND_PATTERN = "\\b(mocks?|fakes?|stubs?|samples?|examples?|vendored|generated|deployment|another kind|other kind)\\b";
	private const string _FOUND_MESSAGE = "{0}: a sentence places test code at Low: {1}";
	private const string _MEDIUM_AT_MOST_PHRASE = "Medium at most";
	private const string _DEMOTED_TO_MEDIUM_PHRASE = "demoted to Medium and no lower";
	private const string _TEST_CODE_NOT_LOW_PHRASE = "**Test code is not Low.**";
	private const string _MOCK_IS_LOW_PHRASE = "**A mock is Low.**";
	private const string _EXCERPT_ELLIPSIS = "...";
	private const int _EXCERPT_LENGTH = 140;

	private static readonly Regex _codeSpan = new(_CODE_SPAN_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _quotedText = new(_QUOTED_TEXT_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _sentenceBreak = new(_SENTENCE_BREAK_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _testTerm = new(_TEST_TERM_PATTERN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static readonly Regex _lowPlacement = new(_LOW_PLACEMENT_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _negation = new(_NEGATION_PATTERN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static readonly Regex _medium = new(_MEDIUM_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _otherKind = new(_OTHER_KIND_PATTERN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

	/// <summary>No page of the public set, the migration documentation, the User Guide and the README included, places test code at Low.</summary>
	[Fact]
	public void NoPublicPagePlacesTestCodeAtLow()
	{
		string[] paths = PublicDocuments.AllPublicMarkdownPaths();

		string[] problems = [.. paths.SelectMany(path => ProblemsIn(Path.GetRelativePath(PublicDocuments.PublicFolder(), path), File.ReadAllText(path)))];

		Assert.NotEmpty(paths);
		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The check fails on a sentence that places test code at Low, and passes the sentences the pages are right to write, shown on synthetic text.</summary>
	[Fact]
	public void TheCheckReportsASentenceThatPlacesTestCodeAtLowAndPassesTheRightOnes()
	{
		string[] offenders =
		[
			"A match in a test file is reported at Low, with no recommendation.",
			"| **Low** | The match is in a comment or in a test fixture. |",
			"A match is demoted to Low when it sits in a file the tool recognizes as test code."
		];
		string[] rightOnes =
		[
			"A match in a test file is reported at Medium at most, with the recommendation of its class.",
			"**Test code is not Low.** A match in a test file is reported at Medium.",
			"A finding in test code is not reported at Low.",
			"A file that is test code and also a mock is Low, and the report still marks it as test code.",
			"A match in a file the tool recognizes as a mock is reported at Low.",
			"The reason reads \"the file's name marks it as a test, sample or generated file\" and the finding is Low."
		];

		Assert.All(offenders, offender => Assert.NotEmpty(ProblemsIn("synthetic", offender)));
		Assert.All(rightOnes, right => Assert.Empty(ProblemsIn("synthetic", right)));
	}

	/// <summary>Part A of C#, PowerShell and VB.NET says that test code is Medium at most, and so does Part A of Shell.</summary>
	[Fact]
	public void EveryPartAStatesThatTestCodeIsDemotedToMediumAndNoLower()
	{
		string[] withoutStatement =
		[
			.. DocumentedLanguage.All
				.Where(language => !File.ReadAllText(language.PartAPath()).Contains(_MEDIUM_AT_MOST_PHRASE, StringComparison.Ordinal)
					|| !File.ReadAllText(language.PartAPath()).Contains(_DEMOTED_TO_MEDIUM_PHRASE, StringComparison.Ordinal))
				.Select(language => language.PartAFileName)
		];

		Assert.True(withoutStatement.Length == 0, $"Part A does not say that test code is demoted to Medium and no lower: {string.Join(", ", withoutStatement)}");
	}

	/// <summary>The test-code section of every Part C says that test code is not Low and that a mock is.</summary>
	[Fact]
	public void EveryPartCSaysThatTestCodeIsNotLowAndThatAMockIs()
	{
		string[] withoutStatement =
		[
			.. DocumentedLanguage.All
				.Where(language =>
				{
					string partC = File.ReadAllText(language.PartCPath());

					return !partC.Contains(_TEST_CODE_NOT_LOW_PHRASE, StringComparison.Ordinal)
						|| !partC.Contains(_MOCK_IS_LOW_PHRASE, StringComparison.Ordinal);
				})
				.Select(language => language.PartCFileName)
		];

		Assert.True(withoutStatement.Length == 0, $"Part C does not say that test code is not Low and that a mock is: {string.Join(", ", withoutStatement)}");
	}

	/// <summary>
	/// What Part A and Part C say, run through the analyzers: in each of the four languages a High-shaped construct in a test folder is reported at Medium and marked as
	/// test code, the same construct in a mock is Low and not marked, and the same construct in an ordinary file is High.
	/// </summary>
	[Fact]
	public void TestCodeIsMediumAndMarkedAndAMockIsLowInEveryLanguageThePagesDescribeItSo()
	{
		foreach (LanguageProbe probe in Probes())
		{
			ImmutableArray<RecognizedConstruct> ordinary = probe.Recognize(probe.HighSource, null);
			ImmutableArray<RecognizedConstruct> inTest = probe.Recognize(probe.HighSource, probe.TestPath);
			ImmutableArray<RecognizedConstruct> inMock = probe.Recognize(probe.HighSource, probe.MockPath);

			Assert.True(ordinary.Any(construct => construct.Level == ConfidenceLevel.High && !construct.TestCode), $"{probe.Language}: the ordinary file does not read at High.");
			Assert.NotEmpty(inTest);
			Assert.All(inTest, construct => Assert.Equal(ConfidenceLevel.Medium, construct.Level));
			Assert.All(inTest, construct => Assert.True(construct.TestCode, $"{probe.Language}: the test finding is not marked as test code."));
			Assert.NotEmpty(inMock);
			Assert.All(inMock, construct => Assert.Equal(ConfidenceLevel.Low, construct.Level));
			Assert.All(inMock, construct => Assert.False(construct.TestCode, $"{probe.Language}: the mock finding is marked as test code."));
		}
	}

	private static ImmutableArray<LanguageProbe> Probes()
	{
		CSharpRecognitionHarness csharp = new();
		PowerShellRecognitionHarness powerShell = new();
		VBNetRecognitionHarness vbNet = new();
		ShellRecognitionHarness shell = new();

		return
		[
			new LanguageProbe(
				"C#",
				(source, path) => path is null ? csharp.Recognize(source) : csharp.Recognize(source, path),
				"Credentials credentials = new UsernamePasswordCredentials(\"<user>\", \"<password>\");\n",
				"tests/ClientTests.cs",
				"FakeClient.cs"),
			new LanguageProbe(
				"PowerShell",
				(source, path) => path is null ? powerShell.Recognize(source) : powerShell.Recognize(source, path),
				"$credentials = New-Object Vendor.Sdk.UsernamePasswordCredentials('<user>', '<password>')\n",
				"tests/Run.ps1",
				"mocks/Run.ps1"),
			new LanguageProbe(
				"VB.NET",
				(source, path) => path is null ? vbNet.Recognize(source) : vbNet.Recognize(source, path),
				"Dim c As New UsernamePasswordCredentials(userName, password)\n",
				"tests/Run.vb",
				"FakeRun.vb"),
			new LanguageProbe(
				"Shell",
				(source, path) => path is null ? shell.Recognize(source) : shell.Recognize(source, path),
				"curl -sS -u admin:example \"$HOST/api\"\n",
				"tests/run.sh",
				"mocks/run.sh")
		];
	}

	/// <summary>One language of the probe: how to recognize its text, a High-shaped construct, a test path and a mock path.</summary>
	private sealed record LanguageProbe(string Language, Func<string, string?, ImmutableArray<RecognizedConstruct>> Recognize, string HighSource, string TestPath, string MockPath);

	private static ImmutableArray<string> ProblemsIn(string name, string text)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach (string line in WithoutFences(text))
		{
			string plain = _quotedText.Replace(_codeSpan.Replace(line, string.Empty), string.Empty);

			foreach (string sentence in _sentenceBreak.Split(plain))
			{
				if (_testTerm.IsMatch(sentence)
					&& _lowPlacement.IsMatch(sentence)
					&& !_negation.IsMatch(sentence)
					&& !_medium.IsMatch(sentence)
					&& !_otherKind.IsMatch(sentence))
				{
					problems.Add(string.Format(CultureInfo.InvariantCulture, _FOUND_MESSAGE, name, Excerpt(sentence)));
				}
			}
		}

		return problems.ToImmutable();
	}

	private static IEnumerable<string> WithoutFences(string text)
	{
		bool insideFence = false;

		foreach (string line in text.Split('\n'))
		{
			bool isFenceLine = line.TrimStart().StartsWith(_FENCE_MARKER, StringComparison.Ordinal);

			insideFence ^= isFenceLine;

			if (!insideFence && !isFenceLine)
			{
				yield return line;
			}
		}
	}

	private static string Excerpt(string sentence)
	{
		string trimmed = sentence.Trim();

		return trimmed.Length <= _EXCERPT_LENGTH ? trimmed : $"{trimmed[.._EXCERPT_LENGTH]}{_EXCERPT_ELLIPSIS}";
	}
}
