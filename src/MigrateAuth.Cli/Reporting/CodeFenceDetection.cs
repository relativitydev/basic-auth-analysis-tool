namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// The fence-opening test, shared by two renderers.
///
/// <see cref="MarkdownReportRenderer"/> uses it to decide whether a template piece's label can share
/// its bullet line with the piece's first physical line, or whether the fence forces the label onto a
/// line of its own. <see cref="RecommendationRenderer"/> makes the same decision when it composes the
/// JSON artifact's `recommendation` string: a consumer that renders that string as Markdown hits the
/// same CommonMark rule - a fence marker that follows inline text on the label's own paragraph never
/// opens a block.
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
