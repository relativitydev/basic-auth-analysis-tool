using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// NO CURRENT PAGE SAYS THAT A LANGUAGE OF THE PLAN IS UNDELIVERED, AND EVERY ONE NAMES ALL EIGHT AS ANALYZED.
///
/// With every analyzer registered, a page that still says a language is not in this release, is pending, or that enabling it changes
/// nothing yet states something the tool no longer does. The pages read are the README, the User Guide and the current sections of the
/// release notes: the first section after the introduction. A section of an earlier version is history, states
/// the languages of its own release and is not rewritten, so it is not read here. The search is over what the page says, not over the
/// source of the tool: the text the tool prints for a language with no analyzer is a state that no language is in at present.
/// </summary>
public sealed class CurrentPagesLanguageStatementTests
{
	private const string _SECTION_HEADING_PREFIX = "## ";
	private const int _FIRST_SECTION_COUNT = 1;
	private const char _LINE_BREAK = '\n';
	private const string _GO_WORD_PATTERN = "(?<![A-Za-z0-9.])Go(?![A-Za-z0-9])";
	private const string _SHELL_WORD_PATTERN = "(?<![A-Za-z0-9.])Shell(?![A-Za-z0-9])";
	private const string _GO_AFTER_PYTHON_PATTERN = "Python(, | and )Go";
	private const string _SYNTHETIC_STALE_TEXT = "Go is not in this release. Enabling `Go` changes nothing yet.";
	private const string _SYNTHETIC_CLEAN_TEXT = "This release analyzes C#, PowerShell, VB.NET, Shell, JavaScript, TypeScript, Python and Go.";
	private const string _SYNTHETIC_NOTES = "# Notes\n\nIntro.\n\n## Current\n\nGo is analyzed.\n\n## Version 0.1.0-alpha\n\nGo is not in this release.\n";

	private static readonly string[] _undeliveredStatements =
	[
		"is not in this release",
		"no analyzer in this release",
		"changes nothing yet",
		"not delivered yet",
		"not yet delivered",
		"Go is pending",
		"language is pending",
		"have an analyzer in this release"
	];

	private static readonly string[] _languageDisplayNames = ["C#", "PowerShell", "VB.NET", "JavaScript", "TypeScript", "Python"];

	private static readonly Regex _goWord = new(_GO_WORD_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _shellWord = new(_SHELL_WORD_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _goAfterPython = new(_GO_AFTER_PYTHON_PATTERN, RegexOptions.CultureInvariant);

	/// <summary>No current page says that a language is not in this release, is pending, or changes nothing when enabled.</summary>
	[Fact]
	public void NoCurrentPageSaysALanguageIsUndelivered()
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach ((string name, string text) in CurrentPageTexts())
		{
			problems.AddRange(UndeliveredStatementProblems(name, text));
		}

		Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The README, the first section of the release notes and the limits page each name all eight languages, Go among them, as analyzed.</summary>
	[Fact]
	public void TheReadmeTheCurrentReleaseNotesAndTheLimitsNameAllEightLanguages()
	{
		string releaseNotes = CurrentReleaseNotesText(PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_RELEASE_NOTES));
		string[] texts =
		[
			File.ReadAllText(PublicDocuments.ReadmePath()),
			releaseNotes,
			PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_LIMITS),
			PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_USING_THE_TOOL)
		];

		foreach (string text in texts)
		{
			Assert.All(_languageDisplayNames, name => Assert.Contains(name, text, StringComparison.Ordinal));
			Assert.Matches(_shellWord, text);
			Assert.Matches(_goAfterPython, text);
		}
	}

	/// <summary>The check reports a stale sentence and passes a current one, and the current sections exclude the section of an earlier version, shown on synthetic text.</summary>
	[Fact]
	public void TheCheckReportsAStaleSentenceAndReadsOnlyTheCurrentSections()
	{
		string current = CurrentReleaseNotesText(_SYNTHETIC_NOTES);

		Assert.NotEmpty(UndeliveredStatementProblems("stale", _SYNTHETIC_STALE_TEXT));
		Assert.Empty(UndeliveredStatementProblems("clean", _SYNTHETIC_CLEAN_TEXT));
		Assert.Contains("Go is analyzed.", current, StringComparison.Ordinal);
		Assert.DoesNotContain("is not in this release", current, StringComparison.Ordinal);
		Assert.Matches(_goWord, _SYNTHETIC_CLEAN_TEXT);
	}

	private static ImmutableArray<(string Name, string Text)> CurrentPageTexts()
	{
		ImmutableArray<(string Name, string Text)>.Builder pages = ImmutableArray.CreateBuilder<(string Name, string Text)>();

		pages.Add((Path.GetFileName(PublicDocuments.ReadmePath()), File.ReadAllText(PublicDocuments.ReadmePath())));

		foreach (string path in PublicDocuments.UserGuidePaths())
		{
			string text = File.ReadAllText(path);
			bool isReleaseNotes = string.Equals(path, PublicDocuments.UserGuideRowPath(PublicDocuments.ROW_RELEASE_NOTES), StringComparison.Ordinal);

			pages.Add((Path.GetFileName(path), isReleaseNotes ? CurrentReleaseNotesText(text) : text));
		}

		return pages.ToImmutable();
	}

	/// <summary>The first section after the introduction: what the release notes say of the current release.</summary>
	private static string CurrentReleaseNotesText(string notes)
	{
		string[] lines = notes.Split(_LINE_BREAK);
		ImmutableArray<string>.Builder current = ImmutableArray.CreateBuilder<string>();
		int sectionsSeen = 0;
		bool inCurrent = false;

		foreach (string line in lines)
		{
			if (line.StartsWith(_SECTION_HEADING_PREFIX, StringComparison.Ordinal))
			{
				sectionsSeen++;
				inCurrent = sectionsSeen == _FIRST_SECTION_COUNT;
			}

			if (inCurrent)
			{
				current.Add(line);
			}
		}

		return string.Join(_LINE_BREAK, current);
	}

	private static ImmutableArray<string> UndeliveredStatementProblems(string pageName, string text)
	{
		return
		[
			.. _undeliveredStatements
				.Where(statement => text.Contains(statement, StringComparison.OrdinalIgnoreCase))
				.Select(statement => $"{pageName} says '{statement}'.")
		];
	}
}
