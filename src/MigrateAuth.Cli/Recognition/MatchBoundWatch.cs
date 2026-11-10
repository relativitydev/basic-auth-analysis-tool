using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE MATCH BOUND, AS A COOPERATIVE TIMER THIS COMPONENT POLLS RATHER THAN AS THE PLATFORM'S
/// OWN MATCH TIMEOUT.
///
/// WHY NOT THE PLATFORM'S TIMEOUT. The platform fixes a <c>Regex</c>'s match timeout IN ITS
/// CONSTRUCTOR; there is no property, no overload and no wrapper that applies one to an instance
/// already built. An analyzer hands the expression over ALREADY CONSTRUCTED, because the build-time
/// generator needs a constant pattern and yields a method rather than data, and rebuilding the
/// expression from its pattern text to attach a timeout would give up exactly that - under
/// ahead-of-time compilation the rebuilt one is INTERPRETED.
///
/// WHAT IT IS. Elapsed time is measured against a <see cref="TimeProvider"/> and accumulated only
/// across match attempts. It is not a <c>CancellationTokenSource</c> with the bound as its due
/// time: a token-source timer schedules a callback on the thread pool, this project introduces no
/// parallelism, no thread pool use and no concurrency primitives - and the token would buy nothing
/// anyway, because the thing that needs interrupting cannot observe a token.
///
/// WHAT IS TIMED. Analyzer-expression time only, not whole-file time: a detection rule's
/// matching, <see cref="FileSymbolTable.Collect"/>'s constant-declaration expression and
/// <see cref="FileDemotionSignals"/>'s generation-marker and content-marker expressions.
/// <see cref="BeginMatchAttempt"/> and <see cref="EndMatchAttempt"/> bracket every one of those,
/// reached through the shared door <see cref="TimedRegexEvaluation"/>; see that type's own summary
/// for which phases share it and which two pipeline phases (the lexical scan and first-line
/// language attribution) remain outside it. Nothing else the engine does between attempts - the
/// malformed-match guard, classification, subsumption - is timed, because no analyzer expression
/// can make those run forever. One file has one accumulator shared across every rule and every
/// other analyzer-declared expression (<see cref="BeginFile"/>, <see cref="HasBeenExceeded"/>), and
/// a trip discards the whole file.
///
/// THE HONEST LIMIT. This bounds the SUM of every match attempt's own duration, across a rule set
/// and across the many matches of one rule. It cannot interrupt ONE pathological call to
/// <c>Match</c> from outside, because nothing can: only the platform's own timeout checks inside the
/// matching loop. So a single expression that backtracks catastrophically on a single match is
/// bounded only where the platform's timeout is also present - which is why
/// <see cref="RecognitionEngine"/> still catches that exception, rather than assuming this replaces
/// it. Both routes end in the same place: the file is stopped and counted.
///
/// THE CLOCK IS AN INPUT, WHICH IS WHAT MAKES THE BOUND TESTABLE without waiting.
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
	/// Opens the bound for one file. The bound applies at the level of the FILE - every
	/// file it stops is counted in its own skipped category - so the accumulator that is reset is
	/// the file's, not the rule's: every rule's own matching time adds to the same total.
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
	/// THE ACCUMULATOR ITSELF, READ RATHER THAN COMPARED. This exists for one caller outside the
	/// engine: <c>Tests.Calibration.MatchBoundCalibration</c>, which reads the exact figure this
	/// bound would have charged a real file against a real clock. It changes nothing
	/// about the bound's own behaviour: <see cref="HasBeenExceeded"/> still decides every trip, and
	/// this is a second, read-only view of the same field.
	/// </summary>
	internal TimeSpan AccumulatedMatchingTime => _accumulatedMatchingTime;
}
