using System.Collections.Immutable;

namespace MigrateAuth.Cli.Scanning;

/// <summary>One language's row of the inventory.</summary>
public sealed record LanguageInventoryRow
{
	/// <summary>The detected language, or the unrecognized bucket the inventory requires.</summary>
	public required string LanguageName { get; init; }

	/// <summary>Every file attributed to this language, including the ones that could not be read.</summary>
	public required int FileCount { get; init; }

	/// <summary>Lines, counted after normalization. A file that was skipped contributes none.</summary>
	public required long LineCount { get; init; }

	/// <summary>How many of those files are candidates, as the inventory defines it.</summary>
	public required int CandidateFileCount { get; init; }

	/// <summary>
	/// Share of the enumerated file count, as a percentage. The denominator is FILES rather than
	/// lines, and the console header says so - see the class below for why the choice is stated
	/// rather than assumed.
	/// </summary>
	public required double ShareOfFilesPercentage { get; init; }
}

/// <summary>
/// The inventory, aggregated on the fly and never per file.
///
/// It holds running totals keyed by language name, and nothing else; no per-file record is retained.
/// What this holds grows with the number of distinct languages, which is at most the catalog's own
/// size plus one - so it is bounded by the binary rather than by the repository.
///
/// The share's denominator is files. A share of lines would silently exclude every file that could
/// not be read, since a skipped file has no line count; a share of files has a denominator that is
/// exact for every enumerated file. The console header names the denominator.
/// </summary>
public sealed class LanguageInventory
{
	private const double _PERCENTAGE_SCALE = 100.0;

	private readonly Dictionary<string, LanguageTally> _talliesByLanguageName = new(StringComparer.Ordinal);

	/// <summary>Every file the inventory pass reached, read or skipped.</summary>
	public int TotalFileCount { get; private set; }

	/// <summary>Every line counted, across all languages.</summary>
	public long TotalLineCount { get; private set; }

	/// <summary>the count, distinct from the total file count.</summary>
	public int TotalCandidateFileCount { get; private set; }

	/// <summary>
	/// How many files were attributed by their extension because first-line detection tripped the
	/// lexical budget. A file counted here is still counted once in its row, under whatever language its
	/// extension gave it.
	/// </summary>
	public int FirstLineFallbackCount { get; private set; }

	/// <summary>
	/// One file's contribution. Everything this stage retains about a file passes through here
	/// and nothing survives the call.
	/// </summary>
	public void RecordFile(string languageName, long lineCount, bool isCandidate)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(languageName);
		ArgumentOutOfRangeException.ThrowIfNegative(lineCount);

		if (!_talliesByLanguageName.TryGetValue(languageName, out LanguageTally? tally))
		{
			tally = new LanguageTally();
			_talliesByLanguageName[languageName] = tally;
		}

		tally.Record(lineCount, isCandidate);

		TotalFileCount++;
		TotalLineCount += lineCount;
		TotalCandidateFileCount += isCandidate ? 1 : 0;
	}

	/// <summary>
	/// Counts one file whose first-line detection tripped the lexical budget. Separate from
	/// <see cref="RecordFile"/> because the fallback describes how the file was attributed, not what
	/// it contributes to a row.
	/// </summary>
	public void RecordFirstLineFallback()
	{
		FirstLineFallbackCount++;
	}

	/// <summary>
	/// The rows, ordered ordinally by language name, so the same repository produces the same output on
	/// every platform; a dictionary's enumeration order does not. There is no special case putting the
	/// unrecognized bucket last.
	/// </summary>
	public ImmutableArray<LanguageInventoryRow> RowsInReportOrder()
	{
		return
		[
			.. _talliesByLanguageName
				.OrderBy(tally => tally.Key, StringComparer.Ordinal)
				.Select(tally => BuildRow(tally.Key, tally.Value))
		];
	}

	private LanguageInventoryRow BuildRow(string languageName, LanguageTally tally)
	{
		return new LanguageInventoryRow
		{
			LanguageName = languageName,
			FileCount = tally.FileCount,
			LineCount = tally.LineCount,
			CandidateFileCount = tally.CandidateFileCount,
			ShareOfFilesPercentage = TotalFileCount == 0
				? 0.0
				: tally.FileCount * _PERCENTAGE_SCALE / TotalFileCount
		};
	}

	/// <summary>One language's running totals. Three integers, and never a path or a line of text.</summary>
	private sealed class LanguageTally
	{
		internal int FileCount { get; private set; }

		internal long LineCount { get; private set; }

		internal int CandidateFileCount { get; private set; }

		internal void Record(long lineCount, bool isCandidate)
		{
			FileCount++;
			LineCount += lineCount;
			CandidateFileCount += isCandidate ? 1 : 0;
		}
	}
}
