using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Agent;

/// <summary>The three sections of the pack, in the order they are written.</summary>
public enum PromptPackSection
{
	/// <summary>Manual-review and uncharacterized cases, where the agent has the most to establish.</summary>
	AmbiguousCases = 0,

	/// <summary>Findings whose flow no signal decided and which a template covers.</summary>
	DefaultedFlow = 1,

	/// <summary>Everything else: optional and skippable.</summary>
	ForConfirmation = 2
}

/// <summary>
/// WHICH FINDINGS THE PACK LISTS, IN WHICH SECTION AND IN WHICH ORDER - computed without ever collecting them.
///
/// SCOPE IS THE FINDINGS THE REPORT LISTS (level at or above the minimum), through the same filter the report
/// renderers use, so every identifier in the pack is one the report shows.
///
/// A FINDING BELONGS TO THE FIRST SECTION IT MATCHES: manual-review and uncharacterized routes first, then a
/// defaulted flow, then the rest. ORDER inside a section is the report's: the established-destination group
/// and then the unknown group, array order inside each.
///
/// THE FINDINGS ARE WALKED, NEVER COLLECTED. The set can hold up to the global findings cap, and a second
/// copy of it at the moment a streamed write is meant to stay incremental is what the memory bound forbids.
/// One counting pass in the constructor records the counts and which instruction kinds each section holds;
/// each section is then a lazy pass over the same source that the writer enumerates once.
/// </summary>
public sealed class PromptPackScope
{
	private const int _SECTION_COUNT = 3;
	private const int _NO_KINDS = 0;

	private readonly ImmutableArray<Finding> _findings;
	private readonly LanguageCatalog _catalog;
	private readonly int[] _sectionCounts = new int[_SECTION_COUNT];
	private readonly int[] _kindMasks = new int[_SECTION_COUNT];

	/// <summary>Counts the findings the report lists, in one pass.</summary>
	public PromptPackScope(ImmutableArray<Finding> findings, ConfidenceLevel minimumLevel, LanguageCatalog catalog)
	{
		ArgumentNullException.ThrowIfNull(catalog);

		_findings = findings;
		_catalog = catalog;
		MinimumLevel = minimumLevel;

		foreach (Finding finding in InReportOrder())
		{
			(RecommendationKind kind, PromptPackSection section) = Classify(finding);

			_sectionCounts[(int)section]++;
			_kindMasks[(int)section] |= KindBit(kind);
		}
	}

	/// <summary>The report's minimum confidence level, which the pack states.</summary>
	public ConfidenceLevel MinimumLevel { get; }

	/// <summary>The number of entries in all three sections.</summary>
	public int TotalCount => _sectionCounts.Sum();

	/// <summary>The number of entries in <paramref name="section"/>.</summary>
	public int CountOf(PromptPackSection section)
	{
		return _sectionCounts[(int)section];
	}

	/// <summary>Whether <paramref name="section"/> holds at least one entry of the given instruction kind.</summary>
	public bool ContainsKind(PromptPackSection section, RecommendationKind kind)
	{
		return (_kindMasks[(int)section] & KindBit(kind)) != _NO_KINDS;
	}

	/// <summary>The entries of <paramref name="section"/> in the report's order, produced lazily.</summary>
	public IEnumerable<PromptPackFindingEntry> EntriesOf(PromptPackSection section)
	{
		foreach (Finding finding in InReportOrder())
		{
			(RecommendationKind kind, PromptPackSection findingSection) = Classify(finding);

			if (findingSection == section)
			{
				yield return BuildEntry(finding, kind);
			}
		}
	}

	private IEnumerable<Finding> InReportOrder()
	{
		return ThresholdFilter.InGroup(_findings, MinimumLevel, DestinationCertainty.Established)
			.Concat(ThresholdFilter.InGroup(_findings, MinimumLevel, DestinationCertainty.Unknown));
	}

	private (RecommendationKind Kind, PromptPackSection Section) Classify(Finding finding)
	{
		RecommendationKind kind;
		PromptPackSection section;

		if (finding.Level == ConfidenceLevel.Low)
		{
			kind = RecommendationKind.ManualReview;
			section = PromptPackSection.AmbiguousCases;
		}
		else
		{
			CredentialFlow flow = finding.Flow
				?? throw new InvalidOperationException("A High or Medium finding carries its flow once composed.");

			kind = RecommendationComposer.RouteOf(_catalog, finding.Language, finding.PatternClass.Value, flow);
			section = SectionOf(kind, finding.FlowDecision == FlowDecision.Default);
		}

		return (kind, section);
	}

	private static PromptPackSection SectionOf(RecommendationKind kind, bool flowWasDefaulted)
	{
		PromptPackSection section;

		if (kind != RecommendationKind.FromTemplate)
		{
			section = PromptPackSection.AmbiguousCases;
		}
		else
		{
			section = flowWasDefaulted ? PromptPackSection.DefaultedFlow : PromptPackSection.ForConfirmation;
		}

		return section;
	}

	private static PromptPackFindingEntry BuildEntry(Finding finding, RecommendationKind kind)
	{
		bool isLow = finding.Level == ConfidenceLevel.Low;

		return new PromptPackFindingEntry(
			finding.PatternClass,
			finding.Path,
			finding.Line,
			finding.Level,
			kind,
			finding.SequentialIndex,
			isLow ? null : finding.Flow,
			!isLow && finding.FlowDecision == FlowDecision.Default);
	}

	private static int KindBit(RecommendationKind kind)
	{
		return 1 << (int)kind;
	}
}
