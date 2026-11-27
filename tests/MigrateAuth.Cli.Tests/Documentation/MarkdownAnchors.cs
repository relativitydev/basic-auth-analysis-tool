using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MigrateAuth.Cli.Tests.Documentation;

/// <summary>One heading of a Markdown document: its level, its text as written, and the anchor GitHub gives it.</summary>
internal sealed record MarkdownHeading(int Level, string Text, string Anchor);

/// <summary>
/// THE ANCHORS OF A MARKDOWN DOCUMENT, COMPUTED THE WAY GITHUB COMPUTES THEM, AND THE TABLE OF CONTENTS
/// BUILT FROM THEM. The link test and the generator of the tables of contents of Part C both call this class,
/// so a table of contents and the check of its links cannot disagree.
///
/// The rule for a heading: the text is lower-cased, every character that is not a letter, a digit, a mark, a
/// space, a hyphen or an underscore is deleted, and each space becomes a hyphen. A heading that contains an inline
/// link <c>[text](url)</c> is slugged from the link text only. A heading that repeats an earlier one, of any
/// level, takes the suffix <c>-1</c>, <c>-2</c> and so on in document order. Closing <c>#</c> characters are not
/// part of the text. A line inside a fenced block (<c>```</c> or <c>~~~</c>) is not a heading. An explicit
/// <c>&lt;a id="..."&gt;</c> or <c>&lt;a name="..."&gt;</c> is an anchor too.
/// </summary>
internal static class MarkdownAnchors
{
	private const string _HEADING_PATTERN = "^ {0,3}(?<marks>#{1,6})[ \\t]+(?<text>.+?)(?:[ \\t]+#+)?[ \\t]*$";
	private const string _EXPLICIT_ANCHOR_PATTERN = "<a\\s+(?:id|name)=[\"'](?<id>[^\"']+)[\"']";
	private const string _INLINE_LINK_IN_HEADING_PATTERN = "\\[(?<text>[^\\]]*)\\]\\([^)]*\\)";
	private const string _SLUG_REMOVED_CHARACTERS_PATTERN = "[^\\p{L}\\p{N}\\p{M} _-]";
	private const string _FENCE_PATTERN = "^[ \\t]*(?<run>`{3,}|~{3,})(?<rest>.*)$";
	private const string _HYPHEN = "-";
	private const string _SPACE = " ";
	private const char _LINE_BREAK = '\n';
	private const char _CARRIAGE_RETURN = '\r';
	private const char _BACKTICK = '`';
	/// <summary>The highest heading level a table of contents lists.</summary>
	public const int TABLE_OF_CONTENTS_TOP_LEVEL = 2;

	/// <summary>The lowest heading level a table of contents lists.</summary>
	public const int TABLE_OF_CONTENTS_BOTTOM_LEVEL = 3;

	private const string _TABLE_OF_CONTENTS_ENTRY_FORMAT = "{0}- [{1}](#{2})";
	private const string _TABLE_OF_CONTENTS_INDENT = "\t";

	private static readonly Regex _heading = new(_HEADING_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _explicitAnchor = new(_EXPLICIT_ANCHOR_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _inlineLinkInHeading = new(_INLINE_LINK_IN_HEADING_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
	private static readonly Regex _slugRemovedCharacters = new(_SLUG_REMOVED_CHARACTERS_PATTERN, RegexOptions.CultureInvariant);
	private static readonly Regex _fence = new(_FENCE_PATTERN, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

	/// <summary>The text with every fenced block, its fence lines included, removed.</summary>
	public static string OutsideFences(string text)
	{
		return string.Join(_LINE_BREAK, LinesOutsideFences(text));
	}

	/// <summary>The headings of the text, outside fenced blocks, in document order, each with its GitHub anchor.</summary>
	public static ImmutableArray<MarkdownHeading> HeadingsOf(string text)
	{
		ImmutableArray<MarkdownHeading>.Builder headings = ImmutableArray.CreateBuilder<MarkdownHeading>();
		Dictionary<string, int> repeats = [];

		foreach (string line in LinesOutsideFences(text))
		{
			Match heading = _heading.Match(line);

			if (heading.Success)
			{
				string headingText = heading.Groups["text"].Value;

				headings.Add(new MarkdownHeading(heading.Groups["marks"].Length, headingText, UniqueSlug(Slug(headingText), repeats)));
			}
		}

		return headings.ToImmutable();
	}

	/// <summary>Every anchor a link into the text can name: the anchors of its headings and its explicit anchors.</summary>
	public static HashSet<string> AnchorsOf(string text)
	{
		HashSet<string> anchors = [.. HeadingsOf(text).Select(heading => heading.Anchor)];

		foreach (Match anchor in _explicitAnchor.Matches(OutsideFences(text)))
		{
			anchors.Add(anchor.Groups["id"].Value);
		}

		return anchors;
	}

	/// <summary>The slug of a heading's text, before any numeric suffix.</summary>
	public static string Slug(string headingText)
	{
		string textOnly = _inlineLinkInHeading.Replace(headingText, match => match.Groups["text"].Value);
		string withoutMarkup = _slugRemovedCharacters.Replace(textOnly.ToLowerInvariant(), string.Empty);

		return withoutMarkup.Replace(_SPACE, _HYPHEN, StringComparison.Ordinal);
	}

	/// <summary>
	/// The table of contents of the text: every level 2 heading as a list item and under it its level 3 headings
	/// indented one tab, each linking to its anchor. Deeper headings are not listed. No trailing line break.
	/// </summary>
	public static string TableOfContentsOf(string text)
	{
		IEnumerable<string> entries = HeadingsOf(text)
			.Where(heading => heading.Level is >= TABLE_OF_CONTENTS_TOP_LEVEL and <= TABLE_OF_CONTENTS_BOTTOM_LEVEL)
			.Select(heading => string.Format(
				CultureInfo.InvariantCulture,
				_TABLE_OF_CONTENTS_ENTRY_FORMAT,
				heading.Level == TABLE_OF_CONTENTS_TOP_LEVEL ? string.Empty : _TABLE_OF_CONTENTS_INDENT,
				heading.Text,
				heading.Anchor));

		return string.Join(_LINE_BREAK, entries);
	}

	private static List<string> LinesOutsideFences(string text)
	{
		List<string> kept = [];
		string? openRun = null;

		foreach (string line in text.Split(_LINE_BREAK).Select(candidate => candidate.TrimEnd(_CARRIAGE_RETURN)))
		{
			Match fence = _fence.Match(line);

			if (openRun is null && fence.Success && OpensFence(fence))
			{
				openRun = fence.Groups["run"].Value;
			}
			else if (openRun is not null)
			{
				openRun = ClosesFence(fence, openRun) ? null : openRun;
			}
			else
			{
				kept.Add(line);
			}
		}

		return kept;
	}

	private static bool OpensFence(Match fence)
	{
		return fence.Groups["run"].Value[0] != _BACKTICK || !fence.Groups["rest"].Value.Contains(_BACKTICK, StringComparison.Ordinal);
	}

	private static bool ClosesFence(Match fence, string openRun)
	{
		return fence.Success
			&& fence.Groups["run"].Value[0] == openRun[0]
			&& fence.Groups["run"].Length >= openRun.Length
			&& string.IsNullOrWhiteSpace(fence.Groups["rest"].Value);
	}

	private static string UniqueSlug(string slug, Dictionary<string, int> repeats)
	{
		repeats.TryGetValue(slug, out int seen);
		repeats[slug] = seen + 1;

		return seen == 0 ? slug : $"{slug}{_HYPHEN}{seen.ToString(CultureInfo.InvariantCulture)}";
	}
}
