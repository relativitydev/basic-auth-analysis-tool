namespace MigrateAuth.Cli.Writing;

/// <summary>
/// The handling header required on both shareable artifacts.
///
/// No credential value is ever emitted, and that is not the same as the artifact being safe to
/// forward. The report and the data
/// artifact are not a map of call sites: with the literal-credential signal they are
/// a precise index of WHERE a repository's embedded credentials are, and these are the documents
/// that get pasted into support tickets. The header says so, in
/// the artifact, where whoever is about to forward it will see it.
///
/// It is stated once here and rendered by each sink in its own form, because a Markdown
/// blockquote is not a JSON field and neither may be produced by concatenating the other.
/// </summary>
public static class ArtifactHandlingNotice
{
	/// <summary>The sentence itself, free of any markup, for a sink that carries it as data.</summary>
	public const string NOTICE_TEXT =
		"This document locates Basic Authentication credential material in the assessed repository. It contains no credential values, and it is a precise index of where they are. Handle and share it accordingly.";

	/// <summary>The field a structured-data sink carries the notice in.</summary>
	public const string STRUCTURED_DATA_FIELD_NAME = "handlingNotice";

	private const string _MARKDOWN_BLOCKQUOTE_PREFIX = "> **Handling.** ";

	/// <summary>
	/// The notice as the first line of a Markdown document. It is a blockquote rather than a
	/// heading so that it cannot be mistaken for one of the five report sections.
	/// </summary>
	public static string MarkdownHeader => $"{_MARKDOWN_BLOCKQUOTE_PREFIX}{NOTICE_TEXT}";
}
