using System.Globalization;

namespace MigrateAuth.Cli.Writing;

/// <summary>The two files the Report stage produces, which share one naming rule.</summary>
public enum OutputArtifactKind
{
	/// <summary>The consolidated Markdown report.</summary>
	MarkdownReport = 0,

	/// <summary>The machine-readable findings artifact.</summary>
	StructuredDataArtifact = 1
}

/// <summary>
/// the filename pattern, and the statement that the artifact
/// follows the same pattern and the same collision rule with a different extension. The rule
/// lives here rather than inside the writer so that a caller can say what a run WOULD produce
/// without producing it, and so that the collision loop has nothing to invent.
///
/// The date is formatted against the invariant culture.
/// </summary>
public static class OutputArtifactNaming
{
	/// <summary>The stem every produced file name starts with.</summary>
	public const string FILE_NAME_STEM = "basic-auth-migration";

	/// <summary>The Markdown report's extension.</summary>
	public const string MARKDOWN_FILE_EXTENSION = ".md";

	/// <summary>The data artifact's extension.</summary>
	public const string STRUCTURED_DATA_FILE_EXTENSION = ".json";

	/// <summary>The first name tried, which carries no numeric suffix.</summary>
	public const int UNSUFFIXED_ORDINAL = 1;

	/// <summary>The lowest suffix a collision may produce, which the naming rule fixes at two.</summary>
	public const int FIRST_COLLISION_SUFFIX = 2;

	/// <summary>
	/// The one declaration of the date format this file name, the
	/// Markdown provenance and the data artifact's `provenance.runDate` field all use - see
	/// <see cref="Reporting.MarkdownReportRenderer"/> and <see cref="Reporting.StructuredDataArtifactRenderer"/>,
	/// which both read this constant rather than carrying their own copy of it.
	/// </summary>
	public const string DATE_FORMAT = "yyyy-MM-dd";

	/// <summary>
	/// The name for an ordinal: the first is <c>basic-auth-migration-YYYY-MM-DD.md</c> and the
	/// next ones carry <c>-2</c>, <c>-3</c> and so on.
	/// </summary>
	public static string BuildFileName(OutputArtifactKind kind, DateOnly artifactDate, int ordinal)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(ordinal, UNSUFFIXED_ORDINAL);

		string datePart = artifactDate.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
		string ordinalPart = ordinal == UNSUFFIXED_ORDINAL
			? string.Empty
			: $"-{ordinal.ToString(CultureInfo.InvariantCulture)}";

		return $"{FILE_NAME_STEM}-{datePart}{ordinalPart}{DescribeExtension(kind)}";
	}

	private static string DescribeExtension(OutputArtifactKind kind)
	{
		string extension = kind switch
		{
			OutputArtifactKind.MarkdownReport => MARKDOWN_FILE_EXTENSION,
			OutputArtifactKind.StructuredDataArtifact => STRUCTURED_DATA_FILE_EXTENSION,
			_ => throw new ArgumentOutOfRangeException(nameof(kind))
		};

		return extension;
	}
}
