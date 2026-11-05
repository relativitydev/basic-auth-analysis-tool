namespace MigrateAuth.Cli.Tests.Progress;

/// <summary>
/// A CLOCK THE TEST MOVES, because the alternative is a test that sleeps.
///
/// The cadence under test is two seconds. A test that proved it by waiting would take longer than
/// every other test in this suite combined, and would still be flaky on a busy machine - the
/// failure mode of timing tests is that they are deleted. This provider makes the interval an
/// input instead: nothing waits, and the assertion is about what the reporter does with the time
/// it is told about rather than about how fast the machine is.
/// </summary>
internal sealed class SteppingTimeProvider : TimeProvider
{
	private long _timestamp;

	public override long TimestampFrequency => TimeSpan.TicksPerSecond;

	public override long GetTimestamp()
	{
		return _timestamp;
	}

	internal void Advance(TimeSpan interval)
	{
		_timestamp += interval.Ticks;
	}
}
