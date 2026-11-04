using MigrateAuth.Cli.Catalog;

namespace MigrateAuth.Cli.Recognition;

/// <summary>
/// THE CONFIDENCE COMPOSITION, MADE INTO A TYPE SO THAT A DECREMENT CHAIN CANNOT BE WRITTEN.
///
/// The rule is "the assigned level is the MINIMUM of the candidate levels, and no signal ever
/// raises one". Composing the same signals as SUCCESSIVE DEMOTIONS would let a class with base
/// Medium that is also not self-sufficient and also receives an indeterminate verdict land at LOW,
/// out of the High-or-Medium set and without a recommendation.
///
/// THE TWO COMPOSITIONS DIFFER ONLY IN THE SHAPE OF THE CODE THAT WRITES THEM, which is why this
/// is a type and not a helper method. A running `level` variable that four branches may lower is
/// a decrement chain whether or not its author meant one, and it reads as correct; there is no
/// variable here to lower. Signals are PROPOSED, the set is complete before anything is computed
/// from it, and the level is taken exactly once, at the end, by <see cref="Minimum"/>.
///
/// It is not a set in the mathematical sense and does not deduplicate. Two signals proposing
/// Medium is not the same event twice - it is two independent reasons - and under a minimum the
/// multiplicity costs nothing, while removing it would be the first step back towards counting.
/// </summary>
internal sealed class CandidateLevelSet
{
	private const string _NO_CANDIDATE_MESSAGE =
		"A finding is classified from at least the pattern class's own base level.";

	private readonly List<ConfidenceLevel> _candidates = [];

	/// <summary>How many signals proposed a level. Its value is diagnostic, never an input.</summary>
	internal int Count => _candidates.Count;

	/// <summary>One signal's proposal. Order of arrival cannot matter to a minimum, and does not.</summary>
	internal void Propose(ConfidenceLevel level)
	{
		_candidates.Add(level);
	}

	/// <summary>
	/// Proposes a level only where the signal is present, which keeps the caller free of the
	/// `if` around every proposal.
	/// </summary>
	internal void ProposeWhen(bool signalIsPresent, ConfidenceLevel level)
	{
		if (signalIsPresent)
		{
			Propose(level);
		}
	}

	/// <summary>
	/// The assigned level: the lowest proposal, taken once. Refuses an empty set rather than
	/// inventing a level for it - every path into classification proposes the base level first,
	/// so an empty set here would mean a finding nobody classified.
	/// </summary>
	internal ConfidenceLevel Minimum()
	{
		if (_candidates.Count == 0)
		{
			throw new InvalidOperationException(_NO_CANDIDATE_MESSAGE);
		}

		ConfidenceLevel minimum = _candidates[0];

		foreach (ConfidenceLevel candidate in _candidates)
		{
			if (candidate < minimum)
			{
				minimum = candidate;
			}
		}

		return minimum;
	}
}
