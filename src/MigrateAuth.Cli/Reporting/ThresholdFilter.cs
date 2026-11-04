using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// The confidence threshold, applied here and nowhere earlier.
///
/// The threshold does not enter the rubric: the rubric assigns the same level to the same construct
/// whatever the configuration says, and configuration decides only which assigned levels appear. That
/// is why this component sits at the render boundary rather than inside recognition, classification
/// or composition - <see cref="FindingComposition"/> does not read the configuration at all.
///
/// It removes rows and never renumbers them. Identifiers are assigned over the full ordered set
/// before anything is filtered, so a filtered report shows gaps in its sequence; a finding's
/// identifier is never a function of the reader's configuration file. Nothing here touches
/// <see cref="Finding.Identifier"/> or <see cref="Finding.SequentialIndex"/>, and there is no
/// overload that could.
///
/// It is lazy. The renderers stream, and a filtered set materialized into a second array before
/// writing would hold a second copy of every finding. The sequence is enumerated once per group and
/// never collected.
/// </summary>
public static class ThresholdFilter
{
	/// <summary>
	/// The findings that appear in output: those whose assigned level is at least the configured
	/// minimum. The comparison is on the enumeration's own order, where Low is the widest.
	/// </summary>
	public static IEnumerable<Finding> Apply(ImmutableArray<Finding> findings, ConfidenceLevel minimumLevel)
	{
		return findings.Where(finding => finding.Level >= minimumLevel);
	}

	/// <summary>
	/// How many of the full set appear. It counts the same sequence the renderers write, so the
	/// stated total and the rendered rows cannot disagree.
	/// </summary>
	public static int CountReported(ImmutableArray<Finding> findings, ConfidenceLevel minimumLevel)
	{
		return Apply(findings, minimumLevel).Count();
	}

	/// <summary>
	/// How many findings a raised threshold hides that the level it replaced would have shown: those
	/// between the two. It is the difference of two <see cref="CountReported"/> results, so it counts
	/// what the renderers write and no second filter exists to disagree with them. Zero for a change
	/// that lowers the threshold, which hides nothing.
	/// </summary>
	public static int CountHiddenByRaise(ImmutableArray<Finding> findings, ConfidenceLevelChange change)
	{
		ArgumentNullException.ThrowIfNull(change);

		return change.IsRaise
			? CountReported(findings, change.Previous) - CountReported(findings, change.Current)
			: 0;
	}

	/// <summary>
	/// One destination-certainty group of the reported set, in the total finding order.
	/// Filtering never reorders: the sequence is walked, not sorted.
	/// </summary>
	public static IEnumerable<Finding> InGroup(
		ImmutableArray<Finding> findings,
		ConfidenceLevel minimumLevel,
		DestinationCertainty certainty)
	{
		return Apply(findings, minimumLevel).Where(finding => finding.DestinationCertainty == certainty);
	}
}
