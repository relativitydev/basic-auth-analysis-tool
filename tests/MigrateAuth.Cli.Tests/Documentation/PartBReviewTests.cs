using System.Collections.Immutable;
using System.Text.RegularExpressions;

using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// PART B IS REACHED FROM THE PART A AND THE PART C OF EVERY LANGUAGE, ITS TITLE IS THE NAME THE TOOL PRINTS, AND IT STANDS ON ITS OWN.
///
/// The check over the pointers reads every Part A and every Part C file that exists, so the files of a language join it the day they are written; it also
/// requires the files of every documented language. A pointer resolves when the file it names exists beside the page and, when it names an anchor,
/// when Part B has a heading for it. The check over the title holds Part B's first line to the name the tool prints when it points a reader here.
/// Part B is a security guide for a developer who does not run the tool, so the check over its text holds it to naming neither the tool, nor its
/// configuration, nor its report; what the scan-time warning says is pinned where that warning is described, in the User Guide.
/// </summary>
public sealed class PartBReviewTests
{
	private const string _PART_B_FILE_NAME = "part-b-secret-management.md";
	private const string _PART_A_SEARCH_PATTERN = "part-a-*.md";
	private const string _PART_C_SEARCH_PATTERN = "part-c-*.md";
	private const string _LINK_PATTERN = "\\]\\((?<target>[^)\\s]*part-b-secret-management\\.md)(?:#(?<anchor>[^)\\s]+))?\\)";
	private const string _HEADING_PATTERN = "^#{1,6}\\s+(?<text>.+?)\\s*#*\\s*$";
	private const string _SLUG_REMOVED_CHARACTERS_PATTERN = "[^\\p{L}\\p{N}\\p{M} _-]";
	private const string _TITLE_PREFIX = "# ";
	private const string _HYPHEN = "-";
	private const string _SPACE = " ";
	private const string _CRLF = "\r\n";
	private const string _LF = "\n";
	private const char _LINE_BREAK = '\n';
	private const char _CARRIAGE_RETURN = '\r';

	private static readonly Regex _link = new(_LINK_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _heading = new(_HEADING_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _slugRemovedCharacters = new(_SLUG_REMOVED_CHARACTERS_PATTERN, RegexOptions.CultureInvariant);

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

	/// <summary>Part B's title is the name the tool prints when it points a reader to Part B.</summary>
	[Fact]
	public void PartBTitleIsTheNameTheToolPointsTo()
	{
		string partB = File.ReadAllText(PartBPath()).Replace(_CRLF, _LF, StringComparison.Ordinal);

		Assert.StartsWith($"{_TITLE_PREFIX}{SecretManagementPointer.PART_B_NAME}{_LF}", partB, StringComparison.Ordinal);
	}

	/// <summary>Part B names neither the tool, nor its configuration, nor its report, and it does not describe the scan-time warning.</summary>
	[Theory]
	[InlineData("migrate-auth")]
	[InlineData("defaultFlow")]
	[InlineData("the tool")]
	[InlineData("the analysis")]
	[InlineData("the report")]
	[InlineData("the scan")]
	[InlineData("appears to be written into the source code")]
	public void PartBNamesNeitherTheToolNorItsConfigurationNorItsReport(string forbiddenText)
	{
		string partB = File.ReadAllText(PartBPath());

		Assert.DoesNotContain(forbiddenText, partB, StringComparison.OrdinalIgnoreCase);
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
