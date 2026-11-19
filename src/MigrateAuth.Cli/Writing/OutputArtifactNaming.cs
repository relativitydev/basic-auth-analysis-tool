using System.Globalization;

namespace MigrateAuth.Cli.Writing;

/// <summary>
/// The files this product writes. The first two, which the Report stage produces, share one dated naming
/// rule; the prompt pack is named from a compiled stem and carries no date.
/// </summary>
public enum OutputArtifactKind
{
	/// <summary>The consolidated Markdown report.</summary>
	MarkdownReport = 0,

	/// <summary>The machine-readable findings artifact.</summary>
	StructuredDataArtifact = 1,

	/// <summary>The AI-assisted review prompt pack, named through <see cref="OutputArtifactNaming.BuildPromptPackFileName"/>.</summary>
	PromptPack = 2
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

	/// <summary>
	/// The name of a prompt pack for an ordinal: the first is <c>stem.md</c> and the next ones carry
	/// <c>-2</c>, <c>-3</c> and so on. There is no date, so a second run for the same environment on any
	/// day takes the next suffix. The stem is a compiled constant of the environment descriptor, never
	/// input, and is not validated here beyond being present.
	/// </summary>
	public static string BuildPromptPackFileName(string stem, int ordinal)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(stem);
		ArgumentOutOfRangeException.ThrowIfLessThan(ordinal, UNSUFFIXED_ORDINAL);

		string ordinalPart = ordinal == UNSUFFIXED_ORDINAL
			? string.Empty
			: $"-{ordinal.ToString(CultureInfo.InvariantCulture)}";

		return $"{stem}{ordinalPart}{MARKDOWN_FILE_EXTENSION}";
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
