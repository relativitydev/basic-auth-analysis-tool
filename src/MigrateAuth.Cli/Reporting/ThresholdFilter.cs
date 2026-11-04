using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;

namespace MigrateAuth.Cli.Reporting;

/// <summary>
/// THE CONFIGURED THRESHOLD, APPLIED HERE AND NOWHERE EARLIER.
///
/// The rubric assigns the same level to the same construct whatever the configuration says, and
/// configuration decides only WHICH ASSIGNED LEVELS APPEAR. That is why this component exists at
/// the render boundary rather than inside recognition, inside classification, or inside
/// composition - <see cref="FindingComposition"/> does not read the configuration at all.
///
/// IT REMOVES ROWS AND NEVER RENUMBERS THEM. Identifiers are assigned over the FULL ordered set
/// before anything is filtered, so a filtered report shows gaps in its sequence. Renumbering would
/// make a finding's identifier a function of the reader's configuration file and move the key a
/// comparison runs against. Nothing here touches
/// <see cref="Finding.Identifier"/> or <see cref="Finding.SequentialIndex"/>, and there is no
/// overload that could.
///
/// IT IS LAZY, AND THAT IS NOT A MICRO-OPTIMIZATION. The renderers stream: a filtered set
/// materialized into a second array before writing would hold a second copy of up to a hundred
/// thousand findings at the exact moment the write is meant to stay incremental. The
/// sequence is enumerated once per group and never collected.
/// </summary>
public static class ThresholdFilter
{
	/// <summary>
	/// The findings that appear in output: those whose assigned level is at least the configured
	/// minimum. The comparison is on the enumeration's own order, where Low is the widest.
	/// </summary>
	public static IEnumerable<Finding> Apply(ImmutableArray<Finding> findings, ConfidenceLevel minimumLevel)
	{
		return findings.Where(finding => IsReported(finding, minimumLevel));
	}

	/// <summary>
	/// Whether one finding appears in output at the configured minimum - the single place the
	/// comparison is written, so a caller that tests findings one at a time cannot come out
	/// differently from <see cref="Apply"/>.
	/// </summary>
	public static bool IsReported(Finding finding, ConfidenceLevel minimumLevel)
	{
		return finding.Level >= minimumLevel;
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
	/// How many findings the configured minimum hides although they carry a literal credential: the ones
	/// the console warns about and the report does not list. It counts the complement of
	/// <see cref="IsReported"/>, so it cannot disagree with what the renderers write.
	/// </summary>
	public static int CountHiddenWithLiteralCredential(ImmutableArray<Finding> findings, ConfidenceLevel minimumLevel)
	{
		return findings.Count(finding => finding.LiteralCredentialSignal && !IsReported(finding, minimumLevel));
	}

	/// <summary>
	/// One destination-certainty group of the reported set, in the report's total order.
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
