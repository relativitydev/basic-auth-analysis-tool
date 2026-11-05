using MigrateAuth.Cli.Progress;

using Xunit;

namespace MigrateAuth.Cli.Tests.Progress;

/// <summary>
/// The required two thresholds and the two-second cadence, over a clock the test moves.
///
/// The reporter is the piece the Analysis stage reuses, so these tests are written against it
/// alone rather than through the scan: what is being pinned is that it counts items, not files.
/// </summary>
public sealed class StageProgressReporterTests
{
	private const string _STAGE_NAME = "scan";
	private const string _ITEM_LABEL = "files";
	private const int _ITEMS_PAST_THE_THRESHOLD = ProgressThresholds.PROGRESS_REPORTING_FILE_COUNT + 1;
	private const int _ITEMS_PAST_THE_WARNING = ProgressThresholds.ADVANCE_WARNING_FILE_COUNT + 1;

	private static readonly TimeSpan _oneInterval =
		TimeSpan.FromSeconds(ProgressThresholds.PROGRESS_REPORTING_INTERVAL_IN_SECONDS);

	private readonly StringWriter _output = new();
	private readonly SteppingTimeProvider _clock = new();

	[Fact]
	public void ASmallRunSaysNothingHoweverLongItTakes()
	{
		StageProgressReporter reporter = Build(ProgressThresholds.PROGRESS_REPORTING_FILE_COUNT);

		for (int item = 0; item < 10; item++)
		{
			_clock.Advance(_oneInterval);
			reporter.ItemCompleted();
		}

		Assert.Equal(string.Empty, _output.ToString());
	}

	[Fact]
	public void PastTheThresholdALineAppearsOncePerInterval()
	{
		StageProgressReporter reporter = Build(_ITEMS_PAST_THE_THRESHOLD);

		for (int item = 0; item < 3; item++)
		{
			_clock.Advance(_oneInterval);
			reporter.ItemCompleted();
		}

		Assert.Equal(3, CountLines());
	}

	[Fact]
	public void NoLineAppearsBeforeTheIntervalHasElapsed()
	{
		StageProgressReporter reporter = Build(_ITEMS_PAST_THE_THRESHOLD);

		for (int item = 0; item < 500; item++)
		{
			reporter.ItemCompleted();
		}

		Assert.Equal(string.Empty, _output.ToString());
	}

	[Fact]
	public void ProgressNamesTheDenominatorWhereThereIsOne()
	{
		StageProgressReporter reporter = Build(_ITEMS_PAST_THE_THRESHOLD);

		_clock.Advance(_oneInterval);
		reporter.ItemCompleted();

		Assert.Contains("1 of 5,001 files examined", _output.ToString(), StringComparison.Ordinal);
	}

	/// <summary>
	/// The enumeration pass is discovering the total, so it reports a count and no share. It
	/// engages on the count it has reached, which is the only figure it has.
	/// </summary>
	[Fact]
	public void ProgressWithoutATotalEngagesOnTheCountReachedSoFar()
	{
		StageProgressReporter reporter = Build(totalItemCount: null);

		for (int item = 0; item < _ITEMS_PAST_THE_THRESHOLD; item++)
		{
			_clock.Advance(_oneInterval);
			reporter.ItemCompleted();
		}

		Assert.Equal(1, CountLines());
		Assert.Contains("5,001 files found so far", _output.ToString(), StringComparison.Ordinal);
	}

	[Fact]
	public void TheAdvanceWarningFiresPastFiftyThousand()
	{
		Build(_ITEMS_PAST_THE_WARNING).WarnWhenTheWorkIsLarge();

		Assert.Contains("This will take several minutes.", _output.ToString(), StringComparison.Ordinal);
	}

	[Fact]
	public void TheAdvanceWarningIsSilentAtTheThresholdItself()
	{
		Build(ProgressThresholds.ADVANCE_WARNING_FILE_COUNT).WarnWhenTheWorkIsLarge();

		Assert.Equal(string.Empty, _output.ToString());
	}

	/// <summary>
	/// A stage that has not counted its work cannot warn about how long it will take. That is the
	/// ordering guarantee stated as a property of the type rather than of the caller.
	/// </summary>
	[Fact]
	public void TheAdvanceWarningIsSilentWithNoKnownTotal()
	{
		Build(totalItemCount: null).WarnWhenTheWorkIsLarge();

		Assert.Equal(string.Empty, _output.ToString());
	}

	/// <summary>
	/// The label is the caller's, which is the whole of the reuse claim: each stage
	/// supplies its own two words.
	/// </summary>
	[Fact]
	public void TheStageAndItemNamesComeFromTheCaller()
	{
		StageProgressReporter reporter = new(
			"analyze",
			"candidate files",
			_ITEMS_PAST_THE_THRESHOLD,
			_output,
			_clock);

		_clock.Advance(_oneInterval);
		reporter.ItemCompleted();

		Assert.Contains("analyze: 1 of 5,001 candidate files examined.", _output.ToString(), StringComparison.Ordinal);
	}

	private StageProgressReporter Build(int? totalItemCount)
	{
		return new StageProgressReporter(_STAGE_NAME, _ITEM_LABEL, totalItemCount, _output, _clock);
	}

	private int CountLines()
	{
		return _output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length;
	}
}
