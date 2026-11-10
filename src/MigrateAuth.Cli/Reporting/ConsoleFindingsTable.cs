using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE CONSOLE TABLE: FIVE COLUMNS, AND NEVER THE SIXTH.
///
/// The console and the report deliberately differ by one column. Destination certainty is carried
/// on the finding as a declared extension, and the REPORT groups by it; the console shows the five
/// columns, and the report and the artifact carry the sixth. Where a reader needs the distinction,
/// the report is where it lives.
///
/// THAT IS WHY THIS IS A SEPARATE RENDERER RATHER THAN A NARROW VIEW OF THE REPORT'S. A shared
/// renderer parameterized by "include the certainty" would be one boolean away from putting it on
/// the console.
///
/// THE PATH IS ESCAPED FOR A TERMINAL, which is a different escaping from the report's. Control
/// and formatting characters are removed rather than escaped, because they cannot be escaped into
/// harmlessness - a file name carrying a right-to-left override renders as a different name than
/// it is without containing a single control character.
///
/// THE PATTERN COLUMN RENDERS THE DISPLAY NAME, NOT ONLY THE IDENTIFIER: it uses
/// <see cref="ReportVocabulary.PatternClassText"/>'s "name (identifier)" form, so the identifier is
/// still discoverable in the same cell and the column count this type asserts stays five.
///
/// <see cref="Render"/> takes no side on which stage calls it - it renders whatever finding
/// sequence it is handed - and <see cref="AnalyzeConsoleReport"/> is what applies the configured
/// threshold and the row cap before calling it.
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
	/// The minimum width of the Pattern column. The cell carries
	/// <see cref="ReportVocabulary.PatternClassText"/>'s "name (identifier)" form. Padding only, not a
	/// bound on content - a longer name still renders in full, and widens the column for the whole table.
	/// </summary>
	private const int _PATTERN_COLUMN_WIDTH = 40;

	private static readonly ImmutableArray<string> _columnHeadings = BuildValidatedColumnHeadings();

	/// <summary>The minimum width of each padded column, in column order: ID, File:Line, Language, Pattern.</summary>
	private static readonly ImmutableArray<int> _minimumColumnWidths =
		[_IDENTIFIER_COLUMN_WIDTH, _LOCATION_COLUMN_WIDTH, _LANGUAGE_COLUMN_WIDTH, _PATTERN_COLUMN_WIDTH];

	/// <summary>
	/// The five columns, in order: ID, <c>File:Line</c>, Language, Pattern, Confidence. There is no
	/// sixth, and the table refuses to build if this list ever grows one.
	/// </summary>
	public static ImmutableArray<string> ColumnHeadings => _columnHeadings;

	/// <summary>
	/// Writes the table. The findings arrive already filtered and ordered - the caller applies
	/// the configured threshold, because the console shows what the report shows. The catalog is what the
	/// Pattern column's display name is looked up from.
	///
	/// EACH PADDED COLUMN IS AS WIDE AS ITS LONGEST CELL, and never narrower than its declared
	/// minimum, so a path or a pattern name longer than the minimum widens the column for the whole
	/// table instead of pushing its own row out of line. The rows are therefore all built before the
	/// first is written; the caller bounds them (<see cref="MAXIMUM_CONSOLE_TABLE_ROWS"/>).
	/// </summary>
	public static void Render(TextWriter writer, IEnumerable<Finding> findings, LanguageCatalog catalog)
	{
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(findings);
		ArgumentNullException.ThrowIfNull(catalog);

		ImmutableArray<ImmutableArray<string>> rows = [.. findings.Select(finding => BuildRowCells(finding, catalog))];

		if (rows.IsEmpty)
		{
			writer.WriteLine(NO_FINDINGS_MESSAGE);
		}
		else
		{
			int[] widths = MeasureColumns(rows);

			writer.WriteLine(Compose(_columnHeadings, widths));

			foreach (ImmutableArray<string> row in rows)
			{
				writer.WriteLine(Compose(row, widths));
			}
		}
	}

	/// <summary>The header line at the minimum widths, exposed so that the column rule is assertable without a render.</summary>
	public static string BuildHeaderLine()
	{
		return Compose(_columnHeadings, _minimumColumnWidths);
	}

	/// <summary>
	/// The header line <see cref="Render"/> writes over these findings: the same columns, widened
	/// to the longest cell exactly as the rows are.
	/// </summary>
	public static string BuildHeaderLine(IEnumerable<Finding> findings, LanguageCatalog catalog)
	{
		ArgumentNullException.ThrowIfNull(findings);

		ImmutableArray<ImmutableArray<string>> rows = [.. findings.Select(finding => BuildRowCells(finding, catalog))];

		return Compose(_columnHeadings, MeasureColumns(rows));
	}

	/// <summary>
	/// One finding's row at the minimum widths, carrying the five values and nothing else about it.
	/// The catalog is what the Pattern column's display name is looked up from; it is not itself a
	/// sixth value.
	/// </summary>
	public static string BuildRowLine(Finding finding, LanguageCatalog catalog)
	{
		return Compose(BuildRowCells(finding, catalog), _minimumColumnWidths);
	}

	private static ImmutableArray<string> BuildRowCells(Finding finding, LanguageCatalog catalog)
	{
		ArgumentNullException.ThrowIfNull(catalog);

		string location = string.Format(
			CultureInfo.InvariantCulture,
			_LOCATION_FORMAT,
			EmittedPathText.ForDestination(finding.Path, EmissionDestination.Terminal),
			finding.Line);

		return
		[
			finding.Identifier,
			location,
			AssessedLanguages.CanonicalName(finding.Language),
			ReportVocabulary.PatternClassText(catalog, finding),
			ConfidenceLevels.Name(finding.Level)
		];
	}

	/// <summary>The width of each padded column: its minimum, or its longest cell where that is longer.</summary>
	private static int[] MeasureColumns(ImmutableArray<ImmutableArray<string>> rows)
	{
		int[] widths = [.. _minimumColumnWidths];

		foreach (ImmutableArray<string> row in rows)
		{
			for (int column = 0; column < widths.Length; column++)
			{
				widths[column] = Math.Max(widths[column], row[column].Length);
			}
		}

		return widths;
	}

	/// <summary>Pads every column but the last, which is not padded so a line carries no trailing blanks.</summary>
	private static string Compose(ImmutableArray<string> cells, IReadOnlyList<int> widths)
	{
		StringBuilder line = new();

		for (int column = 0; column < widths.Count; column++)
		{
			line.Append(cells[column].PadRight(widths[column])).Append(COLUMN_SEPARATOR);
		}

		return line.Append(cells[widths.Count]).ToString();
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
