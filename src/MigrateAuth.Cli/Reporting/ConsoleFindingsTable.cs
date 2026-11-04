using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// The console table: five columns, and never a sixth.
///
/// Destination certainty is carried by the finding model, the report and the artifact, but not by the
/// console table, which keeps its five columns. Where a reader needs the distinction, the report is
/// where it lives. This is a separate renderer rather than a narrow view of the report's, so the
/// console's column set cannot change through a shared option.
///
/// The path is escaped for a terminal, which is a different escaping from the report's. Control
/// and formatting characters are removed rather than escaped, because they cannot be escaped into
/// harmlessness - a file name carrying a right-to-left override renders as a different name than
/// it is without containing a single control character.
///
/// The Pattern column renders <see cref="ReportVocabulary.PatternClassText"/>'s "name (identifier)"
/// form, so the identifier stays discoverable in the same cell and the column count stays five.
///
/// <see cref="Render"/> renders whatever finding sequence it is handed; <see cref="AnalyzeConsoleReport"/>
/// applies the confidence threshold and the row cap before calling it.
/// </summary>
public static class ConsoleFindingsTable
{
	/// <summary>The column count, asserted against the headings below rather than trusted.</summary>
	public const int COLUMN_COUNT = 5;

	/// <summary>
	/// The row bound <see cref="AnalyzeConsoleReport"/> applies to this table, so that a
	/// large finding set does not turn one `analyze` run into an unreadable console scroll. The
	/// report carries every row without a bound - it is the artifact a reader consults for the full set.
	/// </summary>
	public const int MAXIMUM_CONSOLE_TABLE_ROWS = 200;

	/// <summary>Said in place of the table when the reported set is empty.</summary>
	public const string NO_FINDINGS_MESSAGE = "No findings to report.";

	/// <summary>The separator between columns, which is also what makes the count countable.</summary>
	public const string COLUMN_SEPARATOR = "  ";

	private const string _LOCATION_FORMAT = "{0}:{1}";
	private const int _IDENTIFIER_COLUMN_WIDTH = 6;
	private const int _LOCATION_COLUMN_WIDTH = 48;
	private const int _LANGUAGE_COLUMN_WIDTH = 12;

	/// <summary>
	/// The Pattern column's padding width, sized for <see cref="ReportVocabulary.PatternClassText"/>'s
	/// "name (identifier)" form. Padding only, not a bound on content - a longer name still renders in
	/// full, just without column alignment past this width.
	/// </summary>
	private const int _PATTERN_COLUMN_WIDTH = 40;

	private static readonly ImmutableArray<string> _columnHeadings = BuildValidatedColumnHeadings();

	/// <summary>
	/// The five columns, in order: ID, <c>File:Line</c>, Language, Pattern, Confidence. There is no
	/// sixth, and the table refuses to build if this list ever grows one.
	/// </summary>
	public static ImmutableArray<string> ColumnHeadings => _columnHeadings;

	/// <summary>
	/// Writes the table. The findings arrive already filtered and ordered - the caller applies the
	/// confidence threshold, because the console shows what the report shows. The catalog is what the
	/// Pattern column's display name is looked up from.
	/// </summary>
	public static void Render(TextWriter writer, IEnumerable<Finding> findings, LanguageCatalog catalog)
	{
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(findings);
		ArgumentNullException.ThrowIfNull(catalog);

		bool wroteAny = false;

		foreach (Finding finding in findings)
		{
			if (!wroteAny)
			{
				writer.WriteLine(BuildHeaderLine());
			}

			writer.WriteLine(BuildRowLine(finding, catalog));
			wroteAny = true;
		}

		if (!wroteAny)
		{
			writer.WriteLine(NO_FINDINGS_MESSAGE);
		}
	}

	/// <summary>The header line, exposed so that the column rule is assertable without a render.</summary>
	public static string BuildHeaderLine()
	{
		return Compose(
			_columnHeadings[0],
			_columnHeadings[1],
			_columnHeadings[2],
			_columnHeadings[3],
			_columnHeadings[4]);
	}

	/// <summary>
	/// One finding's row, carrying the five values and nothing else about it. The catalog is what
	/// the Pattern column's display name is looked up from; it is not itself a
	/// sixth value.
	/// </summary>
	public static string BuildRowLine(Finding finding, LanguageCatalog catalog)
	{
		ArgumentNullException.ThrowIfNull(catalog);

		string location = string.Format(
			CultureInfo.InvariantCulture,
			_LOCATION_FORMAT,
			EmittedPathText.ForDestination(finding.Path, EmissionDestination.Terminal),
			finding.Line);

		return Compose(
			finding.Identifier,
			location,
			AssessedLanguages.CanonicalName(finding.Language),
			ReportVocabulary.PatternClassText(catalog, finding),
			ConfidenceLevels.Name(finding.Level));
	}

	private static string Compose(string identifier, string location, string language, string pattern, string confidence)
	{
		return
			$"{identifier.PadRight(_IDENTIFIER_COLUMN_WIDTH)}{COLUMN_SEPARATOR}" +
			$"{location.PadRight(_LOCATION_COLUMN_WIDTH)}{COLUMN_SEPARATOR}" +
			$"{language.PadRight(_LANGUAGE_COLUMN_WIDTH)}{COLUMN_SEPARATOR}" +
			$"{pattern.PadRight(_PATTERN_COLUMN_WIDTH)}{COLUMN_SEPARATOR}" +
			$"{confidence}";
	}

	/// <summary>
	/// The list, checked against the fixed column count. A sixth column added here does
	/// not start the product, which is the structural form of "never the sixth".
	/// </summary>
	private static ImmutableArray<string> BuildValidatedColumnHeadings()
	{
		ImmutableArray<string> headings = ["ID", "File:Line", "Language", "Pattern", "Confidence"];

		if (headings.Length != COLUMN_COUNT)
		{
			throw new InvalidOperationException(
				$"The console table fixes {COLUMN_COUNT} console columns and {headings.Length} are declared.");
		}

		return headings;
	}
}
