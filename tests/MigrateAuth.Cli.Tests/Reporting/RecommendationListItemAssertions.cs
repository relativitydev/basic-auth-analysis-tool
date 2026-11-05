using System.Globalization;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The CommonMark structural check for a recommendation list item, shared by the
/// reporting-level synthetic-template test and the pipeline-level real-CS-001 test.
///
/// IT DOES TWO THINGS AT ONCE. It walks the item's continuation
/// lines and asserts each one is indented to exactly <see cref="ContentIndent"/> - no fence
/// marker anywhere else, no line escaping to column 0 while still part of the item, since a line
/// that failed either of those would not satisfy the loop's own <c>StartsWith</c> condition and
/// would end the item early. And it hands back the field's own text with that indentation
/// removed, so the caller proves the content survived untouched by asserting the reconstruction
/// equals the template verbatim - which also catches an indentation width one column too wide,
/// something the loop condition alone would miss.
/// </summary>
internal static class RecommendationListItemAssertions
{
	/// <summary>
	/// CommonMark's content column for this report's `"- "` marker: marker width 1 plus the one
	/// space after it. Mirrors the private constant of the same value in
	/// <c>MarkdownReportRenderer</c> - duplicated rather than exposed, because a test asserting
	/// against the production type's own constant would not catch that constant itself drifting
	/// from what CommonMark requires.
	/// </summary>
	internal const string ContentIndent = "  ";

	private const string _LABELED_ROW_FORMAT = "- {0}: ";
	private const string _LABEL_ONLY_FORMAT = "- {0}:";
	private const string _UNLABELED_BULLET_PREFIX = "- ";
	private const string _BACKTICK_FENCE_MARKER = "```";
	private const string _TILDE_FENCE_MARKER = "~~~";

	/// <summary>
	/// One of the four labeled recommendation pieces, starting at <paramref name="bulletLineIndex"/>. Reads
	/// either of its two forms: the label sharing its bullet with the value's first line, or -
	/// when that first line opens a fence - the label alone, with the value starting on the item's
	/// first continuation line.
	/// </summary>
	internal static (string Value, int EndExclusive) ReadLabeledListItem(string[] lines, int bulletLineIndex, string label)
	{
		string labeledRowPrefix = string.Format(CultureInfo.InvariantCulture, _LABELED_ROW_FORMAT, label);
		string labelOnlyLine = string.Format(CultureInfo.InvariantCulture, _LABEL_ONLY_FORMAT, label);

		List<string> valueLines = [];
		int cursor = bulletLineIndex + 1;

		if (!string.Equals(lines[bulletLineIndex], labelOnlyLine, StringComparison.Ordinal))
		{
			Assert.StartsWith(labeledRowPrefix, lines[bulletLineIndex], StringComparison.Ordinal);
			valueLines.Add(lines[bulletLineIndex][labeledRowPrefix.Length..]);
		}

		cursor = ConsumeContinuationLines(lines, cursor, valueLines);

		return (string.Join('\n', valueLines), cursor);
	}

	/// <summary>The generic narrative or the security note: one bullet, no label, starting at <paramref name="bulletLineIndex"/>.</summary>
	internal static (string Value, int EndExclusive) ReadUnlabeledListItem(string[] lines, int bulletLineIndex)
	{
		Assert.StartsWith(_UNLABELED_BULLET_PREFIX, lines[bulletLineIndex], StringComparison.Ordinal);

		List<string> valueLines = [lines[bulletLineIndex][_UNLABELED_BULLET_PREFIX.Length..]];
		int cursor = ConsumeContinuationLines(lines, bulletLineIndex + 1, valueLines);

		return (string.Join('\n', valueLines), cursor);
	}

	private static int ConsumeContinuationLines(string[] lines, int startIndex, List<string> valueLines)
	{
		int cursor = startIndex;

		while (cursor < lines.Length && lines[cursor].StartsWith(ContentIndent, StringComparison.Ordinal))
		{
			AssertFenceMarkerNotMisindented(lines[cursor]);
			valueLines.Add(lines[cursor][ContentIndent.Length..]);
			cursor++;
		}

		return cursor;
	}

	/// <summary>
	/// If this line's own content, once the content column is set aside, opens a fence, the marker
	/// must sit at exactly that column - one column short is already impossible, since the
	/// enclosing loop's own <c>StartsWith</c> check would have ended the item first; one column
	/// over is what this catches, which a plain text-equality check downstream could miss if a
	/// template line happened to carry its own leading space.
	/// </summary>
	private static void AssertFenceMarkerNotMisindented(string line)
	{
		string withoutContentColumn = line[ContentIndent.Length..];

		if (OpensFence(withoutContentColumn))
		{
			Assert.False(line.StartsWith(ContentIndent + " ", StringComparison.Ordinal));
		}
	}

	private static bool OpensFence(string line)
	{
		return line.StartsWith(_BACKTICK_FENCE_MARKER, StringComparison.Ordinal)
			|| line.StartsWith(_TILDE_FENCE_MARKER, StringComparison.Ordinal);
	}
}
