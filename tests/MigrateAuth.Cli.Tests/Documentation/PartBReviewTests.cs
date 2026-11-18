using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// PART B IS REACHED FROM THE PART A AND THE PART C OF EVERY LANGUAGE, AND WHAT IT SAYS OF THE SCAN-TIME WARNING IS WHAT THE TOOL PRINTS.
///
/// The check over the pointers reads every Part A and every Part C file that exists, so the files of a language join it the day they are written; it also
/// requires the files of every documented language. A pointer resolves when the file it names exists beside the page and, when it names an anchor,
/// when Part B has a heading for it. The check over the warning builds the sentence from the format the tool prints, with the same placeholders
/// Part B shows, and holds Part B to it, so the page and the console cannot say different things about what the warning says and whom it points to.
/// </summary>
public sealed class PartBReviewTests
{
	private const string _PART_B_FILE_NAME = "part-b-secret-management.md";
	private const string _PART_A_SEARCH_PATTERN = "part-a-*.md";
	private const string _PART_C_SEARCH_PATTERN = "part-c-*.md";
	private const string _LINK_PATTERN = "\\]\\((?<target>[^)\\s]*part-b-secret-management\\.md)(?:#(?<anchor>[^)\\s]+))?\\)";
	private const string _HEADING_PATTERN = "^#{1,6}\\s+(?<text>.+?)\\s*#*\\s*$";
	private const string _SLUG_REMOVED_CHARACTERS_PATTERN = "[^\\p{L}\\p{N}\\p{M} _-]";
	private const string _PLACEHOLDER_PATTERN = "\\{\\d+\\}";
	private const string _FINDING_IDENTIFIER_PLACEHOLDER = "<finding-identifier>";
	private const string _LOCATION_PLACEHOLDER = "<path>:<line>";
	private const string _PATTERN_CLASS_PLACEHOLDER = "<pattern-class>";
	private const string _STILL_APPEARS_PHRASE = "its warning still appears";
	private const string _TITLE_PREFIX = "# ";
	private const string _HYPHEN = "-";
	private const string _SPACE = " ";
	private const string _CRLF = "\r\n";
	private const string _LF = "\n";
	private const char _LINE_BREAK = '\n';
	private const char _CARRIAGE_RETURN = '\r';
	private const int _EXPECTED_PLACEHOLDER_COUNT = 3;

	private static readonly Regex _link = new(_LINK_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _heading = new(_HEADING_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _slugRemovedCharacters = new(_SLUG_REMOVED_CHARACTERS_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _placeholder = new(_PLACEHOLDER_PATTERN, RegexOptions.CultureInvariant);

	/// <summary>Every Part A and every Part C that exists points at Part B, and each pointer resolves; a Part A or Part C written later is read the same way.</summary>
	[Fact]
	public void EveryPartAAndPartCPointsAtPartBAndThePointerResolves()
	{
		string[] paths = PartAAndPartCPaths();
		string[] problems = [.. paths.SelectMany(path => PointerProblemsOf(path))];

		Assert.True(!problems.Any(), string.Join(Environment.NewLine, problems));
		Assert.All(
			DocumentedLanguage.All,
			language =>
			{
				Assert.Contains(paths, path => string.Equals(Path.GetFileName(path), language.PartAFileName, StringComparison.Ordinal));
				Assert.Contains(paths, path => string.Equals(Path.GetFileName(path), language.PartCFileName, StringComparison.Ordinal));
			});
	}

	/// <summary>The check fails on a page with no pointer and on a pointer to a missing file or a missing anchor, shown on synthetic text.</summary>
	[Fact]
	public void ThePointerCheckReportsAPageWithNoPointerAndAPointerThatDoesNotResolve()
	{
		string partB = File.ReadAllText(PartBPath());
		string folder = PublicDocuments.MigrationFolder();

		Assert.Empty(PointerProblemsIn("good.md", "See [Part B](part-b-secret-management.md#hardcoded-credentials).", folder, partB));
		Assert.NotEmpty(PointerProblemsIn("none.md", "No pointer here.", folder, partB));
		Assert.NotEmpty(PointerProblemsIn("anchor.md", "See [Part B](part-b-secret-management.md#no-such-heading).", folder, partB));
		Assert.NotEmpty(PointerProblemsIn("file.md", "See [Part B](missing/part-b-secret-management.md).", folder, partB));
	}

	/// <summary>Part B shows the warning the tool prints while it analyzes, word for word, with the same three placeholders, and its title is the name the warning points to.</summary>
	[Fact]
	public void PartBShowsTheShippedScanTimeWarningWordForWord()
	{
		string partB = File.ReadAllText(PartBPath()).Replace(_CRLF, _LF, StringComparison.Ordinal);
		string shipped = string.Format(
			CultureInfo.InvariantCulture,
			AnalyzeConsoleReport.LITERAL_CREDENTIAL_WARNING_MESSAGE_FORMAT,
			_FINDING_IDENTIFIER_PLACEHOLDER,
			_LOCATION_PLACEHOLDER,
			_PATTERN_CLASS_PLACEHOLDER);
		int distinctPlaceholders = _placeholder.Matches(AnalyzeConsoleReport.LITERAL_CREDENTIAL_WARNING_MESSAGE_FORMAT).Select(match => match.Value).Distinct().Count();

		Assert.Contains(shipped, partB, StringComparison.Ordinal);
		Assert.StartsWith($"{_TITLE_PREFIX}{SecretManagementPointer.PART_B_NAME}", partB, StringComparison.Ordinal);
		Assert.Equal(_EXPECTED_PLACEHOLDER_COUNT, distinctPlaceholders);
	}

	/// <summary>Part B says that a warning for a finding below the configured threshold still appears and says so, and the tool appends exactly that to such a warning.</summary>
	[Fact]
	public void PartBSaysWhatTheShippedWarningDoesForAFindingBelowTheThreshold()
	{
		string partB = File.ReadAllText(PartBPath());
		string expectedStatement = $"{_STILL_APPEARS_PHRASE}, followed by `{AnalyzeConsoleReport.BELOW_REPORTING_THRESHOLD_SUFFIX.Trim()}`";

		Assert.Contains(expectedStatement, partB, StringComparison.Ordinal);
	}

	private static string PartBPath()
	{
		return Path.Combine(PublicDocuments.MigrationFolder(), _PART_B_FILE_NAME);
	}

	private static string[] PartAAndPartCPaths()
	{
		return
		[
			.. Directory.EnumerateFiles(PublicDocuments.MigrationFolder(), _PART_A_SEARCH_PATTERN),
			.. Directory.EnumerateFiles(PublicDocuments.MigrationFolder(), _PART_C_SEARCH_PATTERN)
		];
	}

	private static ImmutableArray<string> PointerProblemsOf(string path)
	{
		return PointerProblemsIn(Path.GetFileName(path), File.ReadAllText(path), Path.GetDirectoryName(path)!, File.ReadAllText(PartBPath()));
	}

	private static ImmutableArray<string> PointerProblemsIn(string pageName, string text, string pageFolder, string partB)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		MatchCollection pointers = _link.Matches(text);

		if (!pointers.Any())
		{
			problems.Add($"{pageName} has no link to Part B.");
		}

		foreach (Match pointer in pointers)
		{
			string target = pointer.Groups["target"].Value;
			string anchor = pointer.Groups["anchor"].Value;

			if (!File.Exists(Path.GetFullPath(Path.Combine(pageFolder, target))))
			{
				problems.Add($"{pageName}: the link '{target}' names no file.");
			}
			else if (!string.IsNullOrEmpty(anchor) && !AnchorsOf(partB).Contains(anchor))
			{
				problems.Add($"{pageName}: the link '{target}#{anchor}' names an anchor that Part B does not have.");
			}
		}

		return problems.ToImmutable();
	}

	private static HashSet<string> AnchorsOf(string text)
	{
		HashSet<string> anchors = [];

		foreach (string line in text.Split(_LINE_BREAK))
		{
			Match heading = _heading.Match(line.TrimEnd(_CARRIAGE_RETURN));

			if (heading.Success)
			{
				string withoutPunctuation = _slugRemovedCharacters.Replace(heading.Groups["text"].Value.ToLowerInvariant(), string.Empty);

				anchors.Add(withoutPunctuation.Replace(_SPACE, _HYPHEN, StringComparison.Ordinal));
			}
		}

		return anchors;
	}
}
