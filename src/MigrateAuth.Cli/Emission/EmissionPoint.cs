using System.Collections.Immutable;

using MigrateAuth.Cli.Reading;

namespace MigrateAuth.Cli.Emission;

/// <summary>
/// One of the eleven places information leaves this process: the console, the report, the data
/// artifact, the state file, the diagnostic log, an exception message, the prompt pack, and
/// whatever is added later.
///
/// A sink receives an <see cref="EmittedFact"/> and nothing else. That is the whole contract,
/// and it is what makes the count of sinks stop mattering: a twelfth one added next year
/// inherits the invariant by being unable to express a violation of it, rather than by its
/// author remembering a rule.
/// </summary>
public interface IEmissionSink
{
	/// <summary>Where this sink writes, which decides how it must escape a path.</summary>
	EmissionDestination Destination { get; }

	/// <summary>Takes one fact. There is no overload that takes text.</summary>
	void Receive(EmittedFact fact);
}

/// <summary>
/// THE CHOKEPOINT. Everything leaving the process passes through <see cref="Emit"/>, whose
/// parameter is <see cref="EmittedFact"/> and whose parameter list is therefore the permitted
/// fields and nothing else: relative path, line number, rule identifier, category, level.
///
/// The value of concentrating it here is not that it is impossible to go around - it is not.
/// It is that going around it looks like going around it. A developer who
/// wants the matched line in the report has to change a signature two dozen call sites depend
/// on, in a file whose entire purpose is stated at the top of it, and that shows up in a diff
/// as a change to the data invariant rather than as an improvement to the report.
///
/// The fan-out is deliberately dumb. This class decides nothing about presentation, ordering or
/// filtering - those belong to the sinks and to the decisions that own them. What it owns is
/// that there is exactly one door.
/// </summary>
public sealed class EmissionPoint
{
	private const string _SINK_NULL_MESSAGE = "A sink may not be null.";
	private const string _CATEGORY_ABSENT_MESSAGE = "An emitted fact must declare its category.";
	private const string _SKIP_REASON_ABSENT_MESSAGE =
		"A skipped item must declare which of the eleven skipped-item categories it was skipped under.";
	private const string _SKIP_REASON_UNEXPECTED_MESSAGE =
		"Only a skipped item carries a skipped-item category.";

	private readonly ImmutableArray<IEmissionSink> _sinks;

	public EmissionPoint(IEnumerable<IEmissionSink> sinks)
	{
		ArgumentNullException.ThrowIfNull(sinks);

		_sinks = [.. sinks];

		if (_sinks.Contains(null!))
		{
			throw new ArgumentException(_SINK_NULL_MESSAGE, nameof(sinks));
		}
	}

	/// <summary>The sinks this point feeds, in the order they were registered.</summary>
	public ImmutableArray<IEmissionSink> Sinks => _sinks;

	/// <summary>
	/// THE EMISSION POINT'S SIGNATURE. Its one parameter carries the five permitted fields, and
	/// carrying text from the analyzed file means adding a sixth - which is a change to this
	/// line and to <see cref="EmittedFact"/>, and is the thing a reviewer is looking for.
	/// </summary>
	public void Emit(EmittedFact fact)
	{
		ArgumentNullException.ThrowIfNull(fact);

		if (fact.Category is EmissionCategory.None)
		{
			throw new ArgumentException(_CATEGORY_ABSENT_MESSAGE, nameof(fact));
		}

		RefuseAMismatchedSkipReason(fact);

		foreach (IEmissionSink sink in _sinks)
		{
			sink.Receive(fact);
		}
	}

	/// <summary>
	/// The skip reason and the skipped-item category travel together or not at all. A skipped
	/// item without a reason is exactly the aggregate count the walk refuses to report - it cannot tell a
	/// permissions problem from a coverage problem - and a reason on anything else is a category
	/// leaking out of the walk into facts that are not skips.
	/// </summary>
	private static void RefuseAMismatchedSkipReason(EmittedFact fact)
	{
		bool declaresASkip = fact.Category is EmissionCategory.SkippedItem;
		bool carriesAReason = fact.SkippedItem is not null and not SkippedItemCategory.None;

		if (declaresASkip && !carriesAReason)
		{
			throw new ArgumentException(_SKIP_REASON_ABSENT_MESSAGE, nameof(fact));
		}

		if (!declaresASkip && fact.SkippedItem is not null)
		{
			throw new ArgumentException(_SKIP_REASON_UNEXPECTED_MESSAGE, nameof(fact));
		}
	}
}
