namespace MigrateAuth.WorstCase;

/// <summary>
/// A stateless, seeded pseudo-random source. Every value is a pure function of the seed and a
/// caller-supplied index, so the tree does not depend on the order values are asked for, on the
/// framework's random implementation, or on the platform.
/// </summary>
internal static class DeterministicRandom
{
	private const ulong _INCREMENT = 0x9E3779B97F4A7C15UL;
	private const ulong _FIRST_MULTIPLIER = 0xBF58476D1CE4E5B9UL;
	private const ulong _SECOND_MULTIPLIER = 0x94D049BB133111EBUL;
	private const int _FIRST_SHIFT = 30;
	private const int _SECOND_SHIFT = 27;
	private const int _THIRD_SHIFT = 31;

	/// <summary>The mixed value for a seed, an index and a stream that keeps unrelated decisions independent.</summary>
	internal static ulong ValueOf(long seed, long index, int stream)
	{
		ulong state = unchecked((ulong)seed + (ulong)index * _INCREMENT + (ulong)stream * _FIRST_MULTIPLIER);

		state = unchecked(state + _INCREMENT);
		state = unchecked((state ^ (state >> _FIRST_SHIFT)) * _FIRST_MULTIPLIER);
		state = unchecked((state ^ (state >> _SECOND_SHIFT)) * _SECOND_MULTIPLIER);

		return state ^ (state >> _THIRD_SHIFT);
	}

	/// <summary>A value in [0, upperBound) for a seed, an index and a stream.</summary>
	internal static int Below(int upperBound, long seed, long index, int stream)
	{
		return (int)(ValueOf(seed, index, stream) % (ulong)upperBound);
	}
}
