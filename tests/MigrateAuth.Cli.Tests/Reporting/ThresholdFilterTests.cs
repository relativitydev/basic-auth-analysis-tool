using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

using Xunit;

namespace MigrateAuth.Cli.Tests.Reporting;

/// <summary>
/// The confidence filter: it decides what appears and never what was assigned, and it does not
/// renumber what survives it.
///
/// The renumbering assertion is the one worth having. Numbering after the filter would make a
/// finding's identifier a function of the reader's configuration file, which breaks the stable
/// identity and moves the key the recall measure grades against; a gapped sequence is the lesser
/// cost.
/// </summary>
public sealed class ThresholdFilterTests
{
	[Fact]
	public void TheWidestThresholdRemovesNothing()
	{
		ImmutableArray<Finding> findings = BuildThreeLevels();

		Assert.Equal(3, ThresholdFilter.CountReported(findings, ConfidenceLevels.WIDEST));
	}

	[Fact]
	public void AMediumThresholdHidesTheLowFinding()
	{
		ImmutableArray<Finding> findings = BuildThreeLevels();

		Finding[] reported = [.. ThresholdFilter.Apply(findings, ConfidenceLevel.Medium)];

		Assert.Equal(2, reported.Length);
		Assert.DoesNotContain(reported, finding => finding.Level == ConfidenceLevel.Low);
	}

	[Fact]
	public void AFindingThatSurvivesKeepsTheIdentifierAndIndexItWasAssigned()
	{
		ImmutableArray<Finding> findings = BuildThreeLevels();

		Finding[] reported = [.. ThresholdFilter.Apply(findings, ConfidenceLevel.High)];
		Finding surviving = Assert.Single(reported);

		Assert.Equal(FindingIdentifiers.Of(3), surviving.Identifier);
		Assert.Equal(3, surviving.SequentialIndex);
	}

	[Fact]
	public void TheFilterPreservesTheOrderCompositionAssigned()
	{
		ImmutableArray<Finding> findings = BuildThreeLevels();

		string[] reportedIdentifiers = [.. ThresholdFilter.Apply(findings, ConfidenceLevel.Medium).Select(finding => finding.Identifier)];

		Assert.Equal([FindingIdentifiers.Of(2), FindingIdentifiers.Of(3)], reportedIdentifiers);
	}

	[Fact]
	public void AGroupCarriesOnlyItsOwnCertaintyAndOnlyWhatTheThresholdReports()
	{
		ImmutableArray<Finding> findings =
		[
			ReportHarness.FindingAt(
				1,
				"low-unknown.cs",
				1,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.Low,
				DestinationCertainty.Unknown),
			ReportHarness.FindingAt(
				2,
				"high-unknown.cs",
				2,
				ReportHarness.TEMPLATED_PATTERN_CLASS,
				ConfidenceLevel.High,
				DestinationCertainty.Unknown),
			ReportHarness.FindingAt(3, "high-established.cs", 3, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];

		Finding[] unknownGroup = [.. ThresholdFilter.InGroup(findings, ConfidenceLevel.Medium, DestinationCertainty.Unknown)];
		Finding[] establishedGroup = [.. ThresholdFilter.InGroup(findings, ConfidenceLevel.Medium, DestinationCertainty.Established)];

		Assert.Equal(FindingIdentifiers.Of(2), Assert.Single(unknownGroup).Identifier);
		Assert.Equal(FindingIdentifiers.Of(3), Assert.Single(establishedGroup).Identifier);
	}

	private static ImmutableArray<Finding> BuildThreeLevels()
	{
		return
		[
			ReportHarness.FindingAt(1, "low.cs", 1, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Low),
			ReportHarness.FindingAt(2, "medium.cs", 2, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.Medium),
			ReportHarness.FindingAt(3, "high.cs", 3, ReportHarness.TEMPLATED_PATTERN_CLASS, ConfidenceLevel.High)
		];
	}
}
