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
	private const string _PART_A_PREFIX = "part-a-";
	private const string _PART_C_PREFIX = "part-c-";
	private const string _FENCE_MARKER = "```";
	private const string _LINK_TEMPLATE = "part-{0}-{1}.md";
	private const string _SCAN_ROW_EXCERPT_ELLIPSIS = "...";
	private const int _EXCERPT_LENGTH = 110;

	private static readonly Regex _partLink = new(_PART_LINK_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _languageList = new(_LANGUAGE_LIST_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _earlierVersion = new(_EARLIER_VERSION_PATTERN, RegexOptions.CultureInvariant);

	/// <summary>The languages the documentation covers are exactly the languages the catalog registers an analyzer for.</summary>
	[Fact]
	public void TheDocumentedLanguagesAreExactlyTheRegisteredAnalyzers()
	{
		string[] registered = [.. CSharpRenderedTexts.Catalog().AnalyzersByLanguageName.Keys.Order(StringComparer.Ordinal)];
		string[] documented = [.. DocumentedLanguage.All.Select(language => language.LanguageName).Order(StringComparer.Ordinal)];

		Assert.Equal(registered, documented);
		Assert.Contains("VB.NET", DocumentedLanguage.All.Select(language => language.DisplayName));
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
		string current = "This release analyzes C#, PowerShell and VB.NET.\n\n[Part A](part-a-csharp.md), [Part A](part-a-powershell.md), [Part A](part-a-vbnet.md).";

		Assert.NotEmpty(LanguageListProblems("stale", stale));
		Assert.NotEmpty(MissingLinks("stale", stale, "a"));
		Assert.Empty(LanguageListProblems("history", history));
		Assert.Empty(LanguageListProblems("current", current));
		Assert.Empty(MissingLinks("current", current, "a"));
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
			.. DocumentedLanguage.All
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
				string[] missing = [.. DocumentedLanguage.All.Select(language => language.DisplayName).Where(name => !line.Contains(name, StringComparison.Ordinal))];

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
