using System.Globalization;

namespace MigrateAuth.Cli.Progress;

/// <summary>
/// ONGOING PROGRESS AND THE ADVANCE WARNING, FOR ANY STAGE THAT WORKS THROUGH A COUNTABLE SET.
///
/// This class names no stage, no file, no language and no candidate. It is given a stage name and
/// an item label as text, and a count of items, so any stage constructs one with its own two words
/// and calls the same two methods, without this file changing and without a base type to inherit.
///
/// IT IS SILENT BELOW THE THRESHOLD: progress is reported past 5,000 items, and a small repository
/// that printed a progress line would bury the inventory it is supposed to be reading.
///
/// DETERMINISM IS NOT PREDICATED ON THIS OUTPUT. How many progress lines appear depends
/// on how fast the machine is, so nothing derived from them may reach a report - and nothing here
/// returns a value to a caller that could.
/// </summary>
public sealed class StageProgressReporter
{
	/// <summary>The advance warning. Fired once, before the work it warns about begins.</summary>
	public const string ADVANCE_WARNING_FORMAT = "{0}: {1} {2} to examine. This will take several minutes.";

	/// <summary>Ongoing progress where the total is known.</summary>
	public const string PROGRESS_WITH_TOTAL_FORMAT = "{0}: {1} of {2} {3} examined.";

	/// <summary>
	/// Ongoing progress where it is not. The enumeration pass is discovering the total, so it
	/// cannot report a share of it, and a fabricated denominator would be worse than none.
	/// </summary>
	public const string PROGRESS_WITHOUT_TOTAL_FORMAT = "{0}: {1} {2} found so far.";

	private const string _ITEM_COUNT_FORMAT = "N0";

	private readonly string _stageName;
	private readonly string _itemLabel;
	private readonly int? _totalItemCount;
	private readonly TextWriter _output;
	private readonly TimeProvider _timeProvider;
	private readonly TimeSpan _reportingInterval =
		TimeSpan.FromSeconds(ProgressThresholds.PROGRESS_REPORTING_INTERVAL_IN_SECONDS);

	private long _lastReportTimestamp;
	private int _completedItemCount;

	/// <param name="stageName">The stage this reports for, in the user's vocabulary.</param>
	/// <param name="itemLabel">What is being counted, plural, in the user's vocabulary.</param>
	/// <param name="totalItemCount">How many there are, or null where that is not yet known.</param>
	/// <param name="output">Where progress is written.</param>
	/// <param name="timeProvider">The clock the two-second cadence is measured against.</param>
	public StageProgressReporter(
		string stageName,
		string itemLabel,
		int? totalItemCount,
		TextWriter output,
		TimeProvider timeProvider)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(stageName);
		ArgumentException.ThrowIfNullOrWhiteSpace(itemLabel);
		ArgumentNullException.ThrowIfNull(output);
		ArgumentNullException.ThrowIfNull(timeProvider);

		_stageName = stageName;
		_itemLabel = itemLabel;
		_totalItemCount = totalItemCount;
		_output = output;
		_timeProvider = timeProvider;
		_lastReportTimestamp = timeProvider.GetTimestamp();
	}

	/// <summary>
	/// The advance warning, which fires only where the total is already known - and that is
	/// the ordering guarantee rather than a limitation of this class. A stage that has not
	/// counted its work cannot warn about how long it will take, which is why the scan
	/// counts first and reads afterwards.
	/// </summary>
	public void WarnWhenTheWorkIsLarge()
	{
		if (_totalItemCount is int totalItemCount && totalItemCount > ProgressThresholds.ADVANCE_WARNING_FILE_COUNT)
		{
			_output.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				ADVANCE_WARNING_FORMAT,
				_stageName,
				totalItemCount.ToString(_ITEM_COUNT_FORMAT, CultureInfo.InvariantCulture),
				_itemLabel));
		}
	}

	/// <summary>
	/// One item done. Writes a line at most every two seconds, and only once the work is past the
	/// progress threshold - which is decided from the total where one is known and from the count
	/// reached so far where it is not.
	/// </summary>
	public void ItemCompleted()
	{
		_completedItemCount++;

		if (HasEngaged() && HasTheIntervalElapsed())
		{
			WriteProgress();
		}
	}

	private bool HasEngaged()
	{
		int engagementCount = _totalItemCount ?? _completedItemCount;

		return engagementCount > ProgressThresholds.PROGRESS_REPORTING_FILE_COUNT;
	}

	/// <summary>
	/// Elapsed time against the last line written, and the stamp moves only when a line is
	/// actually written. Reading the clock once per item is the cost of the cadence, and it is a
	/// timestamp read rather than a wall-clock conversion for that reason.
	/// </summary>
	private bool HasTheIntervalElapsed()
	{
		bool elapsed = _timeProvider.GetElapsedTime(_lastReportTimestamp) >= _reportingInterval;

		if (elapsed)
		{
			_lastReportTimestamp = _timeProvider.GetTimestamp();
		}

		return elapsed;
	}

	private void WriteProgress()
	{
		string completedText = _completedItemCount.ToString(_ITEM_COUNT_FORMAT, CultureInfo.InvariantCulture);

		string line = _totalItemCount is int totalItemCount
			? string.Format(
				CultureInfo.InvariantCulture,
				PROGRESS_WITH_TOTAL_FORMAT,
				_stageName,
				completedText,
				totalItemCount.ToString(_ITEM_COUNT_FORMAT, CultureInfo.InvariantCulture),
				_itemLabel)
			: string.Format(
				CultureInfo.InvariantCulture,
				PROGRESS_WITHOUT_TOTAL_FORMAT,
				_stageName,
				completedText,
				_itemLabel);

		_output.WriteLine(line);
	}
}
