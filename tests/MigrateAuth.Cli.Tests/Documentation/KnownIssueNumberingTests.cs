using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using Xunit;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>
/// THE KNOWN ISSUES OF THE RELEASE NOTES ARE NUMBERED FROM 1 WITHOUT A GAP, AND EVERY REFERENCE TO ONE POINTS AT THE ISSUE IT MEANT.
///
/// The notes keep one numbering across the versions: a version lists the issues it adds and says which numbers still apply, and the
/// newest version comes first, so its issues carry the highest numbers. When an issue is removed, every later issue and every reference
/// to it moves, so three things are checked: taken together the numbers run from 1 to the last with no gap and no repeat; every "known issue N", "known issues N and M", range and "item N" in the public set names a number that exists,
/// and the ranges a version states ("the entries 47 to 56", "the known issues 1 to 46 still apply") agree with the issues it holds; and the
/// references that existed before an issue was removed still name the same issue, shown by a keyword of its title.
/// </summary>
public sealed class KnownIssueNumberingTests
{
	private const string _ITEM_PATTERN = "^(?<number>\\d+)\\. \\*\\*(?<title>[^\\n]+?)\\*\\*";
	private const string _NOTES_REFERENCE_PATTERN = "\\b(?:known issues?|entries|items?)\\s+(?<list>\\d+(?:(?:,\\s*|\\s+and\\s+|\\s+to\\s+)\\d+)*)";
	private const string _GUIDE_REFERENCE_PATTERN = "\\bknown issues?\\s+(?<list>\\d+(?:(?:,\\s*|\\s+and\\s+|\\s+to\\s+)\\d+)*)";
	private const string _NUMBER_PATTERN = "\\d+";
	private const string _ENTRIES_RANGE_PATTERN = "entries (?<first>\\d+) to (?<last>\\d+) ";
	private const string _STILL_APPLY_PATTERN = "known issues 1 to (?<last>\\d+) still apply";
	private const string _SECTION_PREFIX = "### ";
	private const string _SECTION_SEPARATOR = "\n### ";
	private const string _ADDED_SECTION_PREFIX = "### Known issues added in";
	private const string _NO_ISSUE_VERSION_NAME = "a version that adds no issue";
	private const string _ADDED_SECTION_NAME = "a version that adds issues";
	private const string _NO_RANGE_TEXT = "no range";
	private const string _NO_NEW_ISSUE_SENTENCE = "No known issue is added in this release.";
	private const string _NUMBER_FORMAT = "\\b{0}\\b";
	private const int _FIRST_NUMBER = 1;
	private const string _GAP_MESSAGE = "The known issue expected as number {0} is numbered {1}: a number is missing or repeated.";
	private const string _MISSING_REFERENCE_MESSAGE = "{0}: '{1}' names known issue {2}, and the last one is {3}.";
	private const string _STATEMENT_MESSAGE = "{0}: '{1}' does not agree with the issues the version holds ({2}).";
	private const string _SELF_CHECK_NOTES = "# Notes\n\n### Known issues\n\n1. **First.** Text.\n\n3. **Third.** See known issue 7.\n";

	/// <summary>Each reference the notes held before known issue 4 was removed, as it reads now, the number it has now and a keyword of the issue's title.</summary>
	private static readonly (string Snippet, int Number, string TitleKeyword)[] _referenceEvidence =
	[
		("Start a new session. See known issue 5.", 5, "another state format or detection catalog version"),
		("and the report counts it. See known issue 8.", 8, "time limits"),
		("Known issue 7 describes this for the other languages, and it holds for Go", 7, "flow is chosen from signals"),
		("Known issue 7 describes this for the other languages, and it holds for Python", 7, "flow is chosen from signals"),
		("is started fresh. See known issue 5.", 5, "another state format or detection catalog version"),
		("from this release's.** See known issue 5.", 5, "another state format or detection catalog version"),
		("are the same; see known issue 5.", 5, "another state format or detection catalog version"),
		("is not resumed; see known issue 5.", 5, "another state format or detection catalog version"),
		("name a kind of file. See known issue 5.", 5, "another state format or detection catalog version"),
		("see known issue 3. The operating system", 3, "unsigned"),
		("For a scheme written as an identifier, see known issue 31.", 31, "credential-cache entry"),
		("#js-018) and known issue 27.", 27, "environment variable"),
		("#js-019) and known issue 27.", 27, "environment variable"),
		("with a tail, see known issue 27.", 27, "environment variable"),
		("part-a-typescript.md#js-019) and known issue 41.", 41, "settings property"),
		("See known issue 41.", 41, "settings property"),
		("is still not read; see known issue 29.", 29, "JavaScript is read as text"),
		("#cs-019) and known issue 40.", 40, "wrong level"),
		("see known issue 46. See [Part A for C#]", 46, "colon-joined pair in C#"),
		("not Basic Authentication towards RelativityOne; see known issue 18.", 18, "some VB.NET code"),
		("see known issues 14 and 15, and for TypeScript, known issue 38.", 14, "flow evidence is in another file"),
		("see known issues 14 and 15, and for TypeScript, known issue 38.", 15, "both kinds of flow evidence"),
		("see known issues 14 and 15, and for TypeScript, known issue 38.", 38, "plain `.ts` file"),
		("see known issue 57.", 57, "built in two statements"),
		("as described in item 16.", 16, "double quote"),
		("same limit; see known issue 36.", 36, "`.tsx` file"),
		("See known issue 19 and [Limits", 19, "few VB.NET forms"),
		("See known issue 7 and [Part C for TypeScript]", 7, "flow is chosen from signals"),
		("(see known issue 17)", 17, "block switched off"),
		("known issues 19, 20 and 30 and in", 19, "few VB.NET forms"),
		("known issues 19, 20 and 30 and in", 20, "Some VB.NET encoded pairs"),
		("known issues 19, 20 and 30 and in", 30, "VB.NET colon-joined pair"),
		("See known issues 19 and 20.", 19, "few VB.NET forms"),
		("See known issues 19 and 20.", 20, "Some VB.NET encoded pairs"),
		("as described in known issue 8.", 8, "time limits"),
		("lowered from 22.75.** See known issue 8.", 8, "time limits"),
		("raised from 250.** See known issue 8.", 8, "time limits"),
		("need more time than the other languages do. See known issue 8.", 8, "time limits")
	];

	/// <summary>The two references of the limits page to the Go helper issue, as they read now.</summary>
	private static readonly (string Snippet, int Number, string TitleKeyword)[] _limitsReferenceEvidence =
	[
		("declared once in the same file (known issue 62)", 62, "call to a helper that builds the Basic value"),
		("See known issue 62 in the", 62, "call to a helper that builds the Basic value")
	];

	private static readonly Regex _item = new(_ITEM_PATTERN, RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
	private static readonly Regex _notesReference = new(_NOTES_REFERENCE_PATTERN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
	private static readonly Regex _guideReference = new(_GUIDE_REFERENCE_PATTERN, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
	private static readonly Regex _entriesRange = new(_ENTRIES_RANGE_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _stillApply = new(_STILL_APPLY_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

	/// <summary>The numbered known issues of the release notes run from 1 in order, with no gap and no repeat.</summary>
	[Fact]
	public void TheKnownIssuesAreNumberedFromOneWithNoGap()
	{
		string notes = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_RELEASE_NOTES);
		ImmutableArray<(int Number, string Title)> items = ItemsOf(notes);

		Assert.NotEmpty(items);
		Assert.Empty(SequenceProblems(items));
	}

	/// <summary>Every reference to a known issue in the User Guide and the README names a number that exists.</summary>
	[Fact]
	public void EveryReferenceToAKnownIssueNamesAnIssueThatExists()
	{
		string notes = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_RELEASE_NOTES);
		int last = ItemsOf(notes).Length;
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		problems.AddRange(ReferenceProblems(nameof(PublicDocuments.ROW_RELEASE_NOTES), notes, _notesReference, last));

		foreach (string path in PublicDocuments.UserGuidePaths().Append(PublicDocuments.ReadmePath()).Where(candidate => candidate != PublicDocuments.UserGuideRowPath(PublicDocuments.ROW_RELEASE_NOTES)))
		{
			problems.AddRange(ReferenceProblems(Path.GetFileName(path), File.ReadAllText(path), _guideReference, last));
		}

		Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>The ranges a version states agree with the issues it holds: the entries it adds, the numbers that still apply, and the total when it adds none.</summary>
	[Fact]
	public void TheRangesEachVersionStatesAgreeWithTheIssuesItHolds()
	{
		string notes = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_RELEASE_NOTES);
		int last = ItemsOf(notes).Length;
		ImmutableArray<string> problems = StatementProblems(notes, last);

		Assert.True(problems.Length == 0, string.Join(Environment.NewLine, problems));
	}

	/// <summary>Each reference that existed before known issue 4 was removed still names the issue it named, shown by a keyword of that issue's title.</summary>
	[Fact]
	public void EveryReferenceThatExistedBeforeTheRemovalStillNamesTheSameIssue()
	{
		string notes = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_RELEASE_NOTES);
		string limits = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_LIMITS);
		ImmutableArray<(int Number, string Title)> items = ItemsOf(notes);

		Assert.All(_referenceEvidence, evidence => AssertReferenceNamesTheIssue(notes, items, evidence));
		Assert.All(_limitsReferenceEvidence, evidence => AssertReferenceNamesTheIssue(limits, items, evidence));
	}

	/// <summary>The removed issue and its pending-item table are gone, and nothing links to the removed anchor.</summary>
	[Fact]
	public void TheRemovedIssueAndItsPendingItemTableAreGone()
	{
		string notes = PublicDocuments.ReadUserGuideRow(PublicDocuments.ROW_RELEASE_NOTES);

		Assert.DoesNotContain("continuous integration and delivery", notes, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("#whats-next", notes, StringComparison.Ordinal);
		Assert.DoesNotContain("## What's next", notes, StringComparison.Ordinal);
	}

	/// <summary>The check reports a gap in the numbers and a reference to a number that does not exist, shown on synthetic notes.</summary>
	[Fact]
	public void TheCheckReportsAGapAndAReferenceToAMissingNumber()
	{
		ImmutableArray<(int Number, string Title)> items = ItemsOf(_SELF_CHECK_NOTES);

		Assert.Single(SequenceProblems(items));
		Assert.Single(ReferenceProblems("synthetic", _SELF_CHECK_NOTES, _notesReference, items.Length));
	}

	private static void AssertReferenceNamesTheIssue(string text, ImmutableArray<(int Number, string Title)> items, (string Snippet, int Number, string TitleKeyword) evidence)
	{
		Assert.Contains(evidence.Snippet, text, StringComparison.Ordinal);
		Assert.Matches(string.Format(CultureInfo.InvariantCulture, _NUMBER_FORMAT, evidence.Number), evidence.Snippet);
		Assert.Contains(evidence.TitleKeyword, items.Single(item => item.Number == evidence.Number).Title, StringComparison.Ordinal);
	}

	private static ImmutableArray<(int Number, string Title)> ItemsOf(string notes)
	{
		return
		[
			.. _item.Matches(notes).Select(match => (
				int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture),
				match.Groups["title"].Value))
		];
	}

	private static ImmutableArray<string> SequenceProblems(ImmutableArray<(int Number, string Title)> items)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		int[] ordered = [.. items.Select(item => item.Number).Order()];

		for (int index = 0; index < ordered.Length; index++)
		{
			int expected = _FIRST_NUMBER + index;

			if (ordered[index] != expected)
			{
				problems.Add(string.Format(CultureInfo.InvariantCulture, _GAP_MESSAGE, expected, ordered[index]));
			}
		}

		return problems.ToImmutable();
	}

	private static ImmutableArray<string> ReferenceProblems(string name, string text, Regex pattern, int last)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();

		foreach (Match reference in pattern.Matches(text))
		{
			foreach (Match number in Regex.Matches(reference.Groups["list"].Value, _NUMBER_PATTERN))
			{
				int value = int.Parse(number.Value, CultureInfo.InvariantCulture);

				if (value < _FIRST_NUMBER || value > last)
				{
					problems.Add(string.Format(CultureInfo.InvariantCulture, _MISSING_REFERENCE_MESSAGE, name, reference.Value, value, last));
				}
			}
		}

		return problems.ToImmutable();
	}

	private static ImmutableArray<string> StatementProblems(string notes, int last)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		string[] sections = notes.Split(_SECTION_SEPARATOR, StringSplitOptions.None);

		foreach (string section in sections.Where(candidate => $"{_SECTION_PREFIX}{candidate}".StartsWith(_ADDED_SECTION_PREFIX, StringComparison.Ordinal)))
		{
			problems.AddRange(AddedSectionProblems(section));
		}

		foreach (string section in sections.Where(candidate => candidate.Contains(_NO_NEW_ISSUE_SENTENCE, StringComparison.Ordinal)))
		{
			Match stillApply = _stillApply.Match(section);

			if (!stillApply.Success || int.Parse(stillApply.Groups["last"].Value, CultureInfo.InvariantCulture) != last)
			{
				problems.Add(string.Format(CultureInfo.InvariantCulture, _STATEMENT_MESSAGE, _NO_ISSUE_VERSION_NAME, stillApply.Value, last));
			}
		}

		return problems.ToImmutable();
	}

	private static ImmutableArray<string> AddedSectionProblems(string section)
	{
		ImmutableArray<string>.Builder problems = ImmutableArray.CreateBuilder<string>();
		ImmutableArray<(int Number, string Title)> held = ItemsOf(section);
		Match added = _entriesRange.Match(section);
		Match stillApply = _stillApply.Match(section);

		if (held.IsEmpty || !added.Success || !stillApply.Success)
		{
			problems.Add(string.Format(CultureInfo.InvariantCulture, _STATEMENT_MESSAGE, _ADDED_SECTION_NAME, _NO_RANGE_TEXT, held.Length));
		}
		else
		{
			int firstHeld = held[0].Number;
			int lastHeld = held[^1].Number;

			if (int.Parse(added.Groups["first"].Value, CultureInfo.InvariantCulture) != firstHeld
				|| int.Parse(added.Groups["last"].Value, CultureInfo.InvariantCulture) != lastHeld
				|| int.Parse(stillApply.Groups["last"].Value, CultureInfo.InvariantCulture) != firstHeld - 1)
			{
				problems.Add(string.Format(CultureInfo.InvariantCulture, _STATEMENT_MESSAGE, _ADDED_SECTION_NAME, $"{added.Value}{stillApply.Value}",$"{firstHeld} to {lastHeld}"));
			}
		}

		return problems.ToImmutable();
	}
}
