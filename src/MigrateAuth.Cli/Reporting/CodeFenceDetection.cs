namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE FENCE-OPENING TEST, FACTORED OUT SO TWO RENDERERS SHARE ONE PREDICATE.
///
/// <see cref="MarkdownReportRenderer"/> uses it to decide whether a template piece's label can
/// share its bullet line with the piece's first physical line, or whether the fence forces the label
/// onto a line of its own. <see cref="RecommendationRenderer"/> makes the same decision for the same
/// reason when it composes the JSON artifact's `recommendation` string: a piece whose first line opens a fence cannot share its label's line
/// either, because a consumer that renders that JSON string as Markdown hits the identical CommonMark
/// trap - the fence marker would follow inline text on the label's own paragraph and never open a
/// block. This type is the one place that holds the two fence markers and the test against them, so
/// the two renderers cannot drift.
/// </summary>
internal static class CodeFenceDetection
{
	/// <summary>CommonMark's fenced code block opened with three backticks.</summary>
	public const string BACKTICK_FENCE_MARKER = "```";

	/// <summary>CommonMark's fenced code block opened with three tildes.</summary>
	public const string TILDE_FENCE_MARKER = "~~~";

	/// <summary>Whether a physical line opens a fenced code block under CommonMark's two fence characters.</summary>
	public static bool OpensFence(string line)
	{
		return line.StartsWith(BACKTICK_FENCE_MARKER, StringComparison.Ordinal)
			|| line.StartsWith(TILDE_FENCE_MARKER, StringComparison.Ordinal);
	}
}
