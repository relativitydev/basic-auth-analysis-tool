using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Tests.Catalog;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// EVERY PUBLIC PAGE THAT LISTS THE ANALYZED LANGUAGES, OR LINKS PART A AND PART C, NAMES EVERY ANALYZER THE CATALOG REGISTERS.
///
/// A language is added to the tool by registering an analyzer, and the public pages that say which languages the tool reads
/// are written by hand in many places. This test reads the registered analyzers from the same catalog the report is built from and
/// holds each page to the full list, so that a page which still lists the languages of an earlier release fails the day a language
/// is registered. A line that names an earlier version of the tool is history and is not held to the list.
/// </summary>
public sealed class AnalyzerListConsistencyTests
{
	private const string _PART_LINK_PATTERN = "\\(([^)]*?)part-(?<part>[ac])-(?<language>[a-z]+)\\.md";
	private const string _PART_GROUP = "part";
	private const string _LANGUAGE_GROUP = "language";
	private const string _LANGUAGE_LIST_PATTERN = "C# (and|or) (for )?PowerShell|C#, (and )?PowerShell|PowerShell (and|or) (for )?C#";
	private const string _EARLIER_VERSION_PATTERN = "0\\.1\\.0-alpha|0\\.1\\.1-alpha|0\\.2\\.0-alpha|previous release|earlier release";
	private const string _WHOLE_WORD_PATTERN_FORMAT = "(?<![A-Za-z0-9.]){0}(?![A-Za-z0-9])";
	private const string _SHELL_HISTORY_PATTERN = "0\\.[123]\\.\\d+-alpha|previous release|earlier release";
	private const string _PART_A_PREFIX = "part-a-";
	private const string _PART_C_PREFIX = "part-c-";
	private const string _FENCE_MARKER = "```";
	private const string _LINK_TEMPLATE = "part-{0}-{1}.md";
	private const string _SCAN_ROW_EXCERPT_ELLIPSIS = "...";
	private const int _EXCERPT_LENGTH = 110;

	private static readonly Regex _partLink = new(_PART_LINK_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _languageList = new(_LANGUAGE_LIST_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _earlierVersion = new(_EARLIER_VERSION_PATTERN, RegexOptions.CultureInvariant);

	private const string _PRE_RELEASE_SUFFIX = "-alpha";

	/// <summary>
	/// The languages whose analyzer is registered and whose Part A and Part C are not written yet. The list is not a way round the rule: documented plus pending must
	/// equal the registered analyzers, the two lists are disjoint, and a non-empty list is refused in any build that is not a pre-release.
	/// </summary>
	private static readonly string[] _registeredDocumentationPending = [];

	/// <summary>
	/// The languages whose Part A and Part C are written and whose public pages (the README, the release notes, the limits, the index and
	/// the OAuth 2.0 essentials) do not name them yet. The list is not a way round the
	/// rule: it holds only languages that are documented, and a non-empty list is refused in any build that is not a pre-release.
	/// </summary>
	private static readonly string[] _publicPagesPending = [];

	/// <summary>The languages the documentation covers, plus the interim pending list, are exactly the languages the catalog registers an analyzer for.</summary>
	[Fact]
	public void TheDocumentedLanguagesPlusThePendingOnesAreExactlyTheRegisteredAnalyzers()
	{
		string[] registered = [.. CSharpRenderedTexts.Catalog().AnalyzersByLanguageName.Keys.Order(StringComparer.Ordinal)];
		string[] documented = [.. DocumentedLanguage.All.Select(language => language.LanguageName)];
		string[] covered = [.. documented.Concat(_registeredDocumentationPending).Order(StringComparer.Ordinal)];

		Assert.Equal(registered, covered);
		Assert.Empty(documented.Intersect(_registeredDocumentationPending));
		Assert.Empty(_publicPagesPending.Except(documented));
		Assert.Contains("VB.NET", DocumentedLanguage.All.Select(language => language.DisplayName));
	}

	/// <summary>A registered language may wait for its documentation only in a pre-release build; the release must flip this list to empty.</summary>
	[Fact]
	public void ThePendingDocumentationListIsEmptyUnlessTheToolIsAPreReleaseBuild()
	{
		bool preRelease = MigrateAuth.Cli.Reporting.ToolVersion.CURRENT.EndsWith(_PRE_RELEASE_SUFFIX, StringComparison.Ordinal);

		Assert.True(preRelease || (_registeredDocumentationPending.Length == 0 && _publicPagesPending.Length == 0), "A release build may not carry a registered language without its documentation.");
	}

	/// <summary>A page that links Part A or Part C of one language links Part A or Part C of every registered analyzer.</summary>
	[Fact]
	public void EveryPageThatLinksPartAOrPartCLinksThemForEveryRegisteredAnalyzer()
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach (string path in PublicDocuments.AllPublicMarkdownPaths().Where(candidate => !IsAPerLanguagePart(candidate)))
		{
			string text = File.ReadAllText(path);
			HashSet<string> parts = [.. _partLink.Matches(text).Select(match => match.Groups[_PART_GROUP].Value)];

			foreach (string part in parts)
			{
				problems.AddRange(MissingLinks(Path.GetFileName(path), text, part));
			}
		}

		Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>A line that lists the analyzed languages names every registered analyzer, unless it names an earlier version of the tool.</summary>
	[Fact]
	public void EveryLineThatListsTheAnalyzedLanguagesNamesEveryRegisteredAnalyzer()
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach (string path in PublicDocuments.AllPublicMarkdownPaths())
		{
			problems.AddRange(LanguageListProblems(Path.GetFileName(path), File.ReadAllText(path)));
		}

		Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The two checks fail on a page written for the languages of an earlier release, shown on synthetic text.</summary>
	[Fact]
	public void TheChecksReportAPageThatStillListsOnlyTheEarlierLanguages()
	{
		string stale = "This release analyzes C# and PowerShell.\n\nSee [Part A](part-a-csharp.md) and [Part A](part-a-powershell.md).";
		string history = "The release 0.2.0-alpha analyzed C# and PowerShell.";
		string current = "This release analyzes C#, PowerShell, VB.NET, Shell and JavaScript.\n\n[Part A](part-a-csharp.md), [Part A](part-a-powershell.md), [Part A](part-a-vbnet.md), [Part A](part-a-shell.md), [Part A](part-a-javascript.md).";

		Assert.NotEmpty(LanguageListProblems("stale", stale));
		Assert.NotEmpty(MissingLinks("stale", stale, "a"));
		Assert.Empty(LanguageListProblems("history", history));
		Assert.Empty(LanguageListProblems("current", current));
		Assert.Empty(MissingLinks("current", current, "a"));
	}

	/// <summary>
	/// A line that lists the analyzed languages names Shell as a whole word. The other check compares a language's name with the line as a substring, and the name of
	/// Shell is a substring of the name of PowerShell, so a line that lists PowerShell passes it for Shell without naming Shell. A line that names an earlier release, 0.1.x
	/// to 0.3.x, is history and is not held to the list.
	/// </summary>
	[Fact]
	public void EveryLineThatListsTheAnalyzedLanguagesNamesShellAsAWholeWord()
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach (string path in PublicDocuments.AllPublicMarkdownPaths())
		{
			problems.AddRange(ShellWholeWordProblems(Path.GetFileName(path), File.ReadAllText(path)));
		}

		Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The whole-word check fails on a line that lists PowerShell and not Shell, passes history and a complete list, shown on synthetic text.</summary>
	[Fact]
	public void TheWholeWordCheckReportsALineThatListsPowerShellAndNotShell()
	{
		string stale = "This release analyzes C#, PowerShell and VB.NET.";
		string history = "The release 0.3.0-alpha analyzed C#, PowerShell and VB.NET.";
		string current = "This release analyzes C#, PowerShell, VB.NET and Shell.";
		string insidePowerShell = "This release analyzes C# and PowerShell.";

		Assert.NotEmpty(ShellWholeWordProblems("stale", stale));
		Assert.NotEmpty(ShellWholeWordProblems("inside", insidePowerShell));
		Assert.Empty(ShellWholeWordProblems("history", history));
		Assert.Empty(ShellWholeWordProblems("current", current));
	}

	private static ImmutableArray<string> ShellWholeWordProblems(string pageName, string text)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		Regex wholeWord = new(string.Format(CultureInfo.InvariantCulture, _WHOLE_WORD_PATTERN_FORMAT, Regex.Escape(DocumentedLanguage.Shell.DisplayName)), RegexOptions.CultureInvariant);
		Regex history = new(_SHELL_HISTORY_PATTERN, RegexOptions.CultureInvariant);
		bool insideFence = false;

		foreach (string line in text.Split('\n'))
		{
			insideFence ^= line.TrimStart().StartsWith(_FENCE_MARKER, StringComparison.Ordinal);

			if (!insideFence && _languageList.IsMatch(line) && !history.IsMatch(line) && !wholeWord.IsMatch(line))
			{
				problems.Add($"{pageName} lists the analyzed languages without naming {DocumentedLanguage.Shell.DisplayName} as a word: {Excerpt(line)}");
			}
		}

		return problems.ToImmutable();
	}

	private static IEnumerable<DocumentedLanguage> PageLanguages()
	{
		return DocumentedLanguage.All.Where(language => !_publicPagesPending.Contains(language.LanguageName));
	}

	private static bool IsAPerLanguagePart(string path)
	{
		string name = Path.GetFileName(path);

		return name.StartsWith(_PART_A_PREFIX, StringComparison.Ordinal) || name.StartsWith(_PART_C_PREFIX, StringComparison.Ordinal);
	}

	private static ImmutableArray<string> MissingLinks(string pageName, string text, string part)
	{
		return
		[
			.. PageLanguages()
				.Select(language => string.Format(CultureInfo.InvariantCulture, _LINK_TEMPLATE, part, language.LanguageName.ToLowerInvariant()))
				.Where(link => !text.Contains(link, StringComparison.Ordinal))
				.Select(link => $"{pageName} links Part {part.ToUpperInvariant()} of some language but not {link}.")
		];
	}

	private static ImmutableArray<string> LanguageListProblems(string pageName, string text)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		bool insideFence = false;

		foreach (string line in text.Split('\n'))
		{
			insideFence ^= line.TrimStart().StartsWith(_FENCE_MARKER, StringComparison.Ordinal);

			if (!insideFence && _languageList.IsMatch(line) && !_earlierVersion.IsMatch(line))
			{
				string[] missing = [.. PageLanguages().Select(language => language.DisplayName).Where(name => !line.Contains(name, StringComparison.Ordinal))];

				if (missing.Length > 0)
				{
					problems.Add($"{pageName} lists the analyzed languages without {string.Join(", ", missing)}: {Excerpt(line)}");
				}
			}
		}

		return problems.ToImmutable();
	}

	private static string Excerpt(string line)
	{
		string trimmed = line.Trim();

		return trimmed.Length <= _EXCERPT_LENGTH ? trimmed : string.Concat(trimmed.AsSpan(0, _EXCERPT_LENGTH), _SCAN_ROW_EXCERPT_ELLIPSIS);
	}
}
