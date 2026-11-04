using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE MATCH BOUND, AS A COOPERATIVE TIMER THIS COMPONENT POLLS RATHER THAN AS THE PLATFORM'S
/// OWN MATCH TIMEOUT. A <c>Regex</c>'s match timeout is fixed in its constructor, and the analyzer
/// contract hands the expression over already constructed, so no timeout can be attached to it here.
///
/// Elapsed time is measured against a <see cref="TimeProvider"/> and accumulated only across
/// analyzer-expression match attempts, bracketed by <see cref="BeginMatchAttempt"/> and
/// <see cref="EndMatchAttempt"/> through the shared door <see cref="TimedRegexEvaluation"/>. No
/// timer, thread-pool callback or cancellation token is used. Nothing else the engine does between
/// attempts - the malformed-match guard, classification, subsumption - is timed. One file has one
/// accumulator (<see cref="BeginFile"/>, <see cref="HasBeenExceeded"/>) shared across every rule and
/// every other analyzer-declared expression, and a trip discards the whole file.
///
/// THE LIMIT. This bounds the SUM of every match attempt's own duration, across a rule set and
/// across the many matches of one rule. It cannot interrupt ONE call to <c>Match</c> from outside:
/// a single expression that backtracks catastrophically on a single match is bounded only where the
/// platform's own timeout is also present, which is why <see cref="RecognitionEngine"/> still catches
/// that exception. Both routes end in the same place: the file is stopped and counted.
///
/// THE CLOCK IS AN INPUT, WHICH IS WHAT MAKES THE BOUND TESTABLE without waiting on a real clock.
/// </summary>
internal sealed class MatchBoundWatch
{
	private readonly TimeProvider _timeProvider;
	private readonly TimeSpan _bound;

	private TimeSpan _accumulatedMatchingTime;
	private long _matchAttemptStartedTimestamp;

	internal MatchBoundWatch(TimeProvider timeProvider)
		: this(timeProvider, RecognitionEnginePolicy.MATCH_BOUND)
	{
	}

	internal MatchBoundWatch(TimeProvider timeProvider, TimeSpan bound)
	{
		ArgumentNullException.ThrowIfNull(timeProvider);

		_timeProvider = timeProvider;
		_bound = bound;
	}

	/// <summary>
	/// Opens the bound for one file. The accumulator that is reset is the file's, not the rule's:
	/// every rule's own matching time adds to the same total.
	/// </summary>
	internal void BeginFile()
	{
		_accumulatedMatchingTime = TimeSpan.Zero;
	}

	/// <summary>Opens timing for one call to <c>Match</c> or <c>NextMatch</c>, about to be made.</summary>
	internal void BeginMatchAttempt()
	{
		_matchAttemptStartedTimestamp = _timeProvider.GetTimestamp();
	}

	/// <summary>
	/// Closes timing for the match attempt <see cref="BeginMatchAttempt"/> opened, adding its
	/// duration to the file's accumulator. Called whether the attempt found a match or not - a rule
	/// that scans the whole file and finds nothing has still spent real time doing it.
	/// </summary>
	internal void EndMatchAttempt()
	{
		_accumulatedMatchingTime += _timeProvider.GetElapsedTime(_matchAttemptStartedTimestamp);
	}

	/// <summary>Whether this file's accumulated matching time has reached the bound. Read between match attempts, never inside one.</summary>
	internal bool HasBeenExceeded()
	{
		return _accumulatedMatchingTime >= _bound;
	}

	/// <summary>
	/// THE ACCUMULATOR ITSELF, READ RATHER THAN COMPARED, for a timing harness that needs the exact
	/// figure this bound charged a file. It changes nothing about the bound's own behaviour:
	/// <see cref="HasBeenExceeded"/> still decides every trip, and this is a second, read-only view of
	/// the same field.
	/// </summary>
	internal TimeSpan AccumulatedMatchingTime => _accumulatedMatchingTime;
}
