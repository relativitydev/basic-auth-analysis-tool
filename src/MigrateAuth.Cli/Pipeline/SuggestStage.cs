using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Pipeline;

/// <summary>What the Suggest stage is built from. It reads two things and writes one.</summary>
public sealed record SuggestStageContext
{
	/// <summary>The embedded catalog, whose analyzers carry the templates the routing selects.</summary>
	public required LanguageCatalog Catalog { get; init; }

	/// <summary>
	/// The generic-recommendation provider. It is held so that the renderer this stage routes
	/// through is the one the Report stage will use, and it is never ASKED anything here - see
	/// <see cref="RecommendationRenderer.Route(Finding)"/> for why the routing deliberately does
	/// not consult it.
	/// </summary>
	public required IGenericRecommendationProvider GenericRecommendations { get; init; }

	/// <summary>
	/// The session's effective configuration, which supplies the minimum confidence level. Every count this stage prints is taken over the reported set,
	/// the findings that remain after that filter, as the report already does.
	/// </summary>
	public required EffectiveConfiguration Configuration { get; init; }

	/// <summary>Where the stage writes.</summary>
	public required TextWriter Output { get; init; }
}

/// <summary>
/// THE STAGE THAT DECIDES WHICH RECOMMENDATION EACH FINDING WILL GET, AND PRODUCES NONE OF THEM.
///
/// A rendered recommendation is produced at the moment of writing and held by nobody, and
/// <see cref="Finding"/> carries no recommendation field at all, because the pattern class IS the
/// template reference that the template catalog indexes by. Attaching a recommendation therefore
/// cannot mean storing one.
///
/// SO ATTACHING IS RESOLVING, NOT RENDERING. This stage runs the routing over every finding the
/// analysis recorded - a Low finding to manual review, a High or Medium one to its catalog template
/// or to the generic fallback where the catalog has no entry - and reports how many took each
/// route. It writes nothing to the session state, holds no text between runs, and does not call
/// <see cref="RecommendationRenderer.Render"/> at all. Report re-derives the same route at write
/// time, which cannot disagree with what was printed here because both go through the one
/// <see cref="RecommendationRenderer.Route(Finding)"/>.
///
/// WHAT THE USER GETS FOR IT IS THE THING A REPORT WOULD NOT SHOW THEM IN TIME. A catalog whose
/// templates do not cover what was found produces a large generic count HERE, one command before
/// the report is written, rather than being discovered by reading the finished document - which is
/// the failure the fallback exists to make survivable and this line makes visible.
///
/// AND IT IS HONEST ABOUT BEING A THIN STAGE. It gates <c>report</c> because the chain
/// puts it there, and the counts are what it has to say. An empty stage that claimed more would be
/// worse than a thin one that says exactly what it did.
/// </summary>
public sealed class SuggestStage
{
	/// <summary>
	/// Said when the analysis found nothing. The empty repository reaches this line, and it says the
	/// finding set was empty rather than that the stage was skipped - those are different claims
	/// and the gating chain ran this stage either way.
	/// </summary>
	public const string NO_FINDINGS_MESSAGE =
		$"{_STAGE_NAME}: the analysis recorded no findings, so there is nothing to recommend.";

	/// <summary>
	/// The three routes and their counts, on one line. They are reported together because what a
	/// reader needs is the PROPORTION - a generic count beside a template count is a statement
	/// about how well the catalog covered this repository, and either number alone is not. It is
	/// the stage's first line, so it opens with the stage name as the first line of every other
	/// stage does.
	/// </summary>
	public const string ROUTE_COUNTS_MESSAGE_FORMAT =
		_STAGE_NAME + ": recommendations resolved: {0:N0} from a catalog template, {1:N0} generic, {2:N0} routed to manual review.";

	/// <summary>
	/// Said only when the generic route fired, because it is the one that means the catalog did not
	/// cover something. It is what a client reads for every pattern the catalog did not anticipate.
	/// </summary>
	public const string GENERIC_COVERAGE_NOTICE =
		"Some findings have no catalog template for their pattern class and flow and will carry the generic migration recommendation.";

	/// <summary>
	/// The most finding rows the console prints. A repository at the findings cap has
	/// tens of thousands of reported findings and a console that printed a row for each would be
	/// unreadable; past this many the rest are summarized on one line and the report lists every one.
	/// One hundred is what a reader can scan in a terminal without paging.
	/// </summary>
	public const int SUGGEST_CONSOLE_FINDING_ROW_LIMIT = 100;

	/// <summary>Heads the flow-note legend, which gives each note that occurs exactly once.</summary>
	public const string FLOW_NOTES_LEGEND_HEADING = "Flow notes:";

	/// <summary>One legend line: the note's key, then the note's full text.</summary>
	public const string FLOW_NOTE_LEGEND_LINE_FORMAT = "  {0} {1}";

	/// <summary>Heads the finding rows.</summary>
	public const string FINDING_ROWS_HEADING = "Findings:";

	/// <summary>One finding row: identifier, flow, decision, and the key of the flow note it carries.</summary>
	public const string FINDING_ROW_FORMAT = "  {0}  {1}  {2}  {3}";

	/// <summary>
	/// The single line said past <see cref="SUGGEST_CONSOLE_FINDING_ROW_LIMIT"/>: how many reported
	/// findings have no row, then that count per (decision, flow) with the pair's note key, and where
	/// the complete list is.
	/// </summary>
	public const string FINDING_ROWS_SUMMARY_FORMAT =
		"... {0:N0} further findings have no row here: {1}. The report lists every finding.";

	/// <summary>One (decision, flow) entry of <see cref="FINDING_ROWS_SUMMARY_FORMAT"/>.</summary>
	public const string FINDING_ROWS_SUMMARY_ENTRY_FORMAT = "{0:N0} {1} {2} {3}";

	private const string _SUMMARY_ENTRY_SEPARATOR = "; ";

	private const string _STAGE_NAME = "suggest";

	private readonly SuggestStageContext _context;

	public SuggestStage(SuggestStageContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(context.Catalog);
		ArgumentNullException.ThrowIfNull(context.GenericRecommendations);
		ArgumentNullException.ThrowIfNull(context.Configuration);
		ArgumentNullException.ThrowIfNull(context.Output);

		_context = context;
	}

	/// <summary>
	/// The stage body. The renderer is built per run and goes out of scope with it, which is the
	/// structural half of "retains nothing": there is no field on this class that a rendered
	/// recommendation could be assigned to. What survives the loop is a handful of integers and at
	/// most <see cref="SUGGEST_CONSOLE_FINDING_ROW_LIMIT"/> short rows.
	///
	/// EVERYTHING IS COUNTED OVER THE REPORTED SET: the findings at or above the
	/// configured minimum confidence level, the same sequence the report walks. The route counts and
	/// the per-flow counts therefore describe the report this session will write.
	/// </summary>
	public StageRunOutcome Run(SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		RecommendationRenderer recommendations = new(_context.Catalog, _context.GenericRecommendations);
		SuggestTally tally = new();
		bool wasCancelled = false;

		foreach (Finding finding in ThresholdFilter.Apply(state.Findings, _context.Configuration.MinimumConfidenceLevel))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				wasCancelled = true;
				break;
			}

			tally.Record(finding, recommendations.Route(finding));
		}

		StageRunOutcome outcome;

		if (wasCancelled)
		{
			outcome = StageRunOutcome.Cancelled;
		}
		else
		{
			ReportRoutes(state, tally);
			outcome = StageRunOutcome.Completed;
		}

		return outcome;
	}

	private void ReportRoutes(SessionState state, SuggestTally tally)
	{
		if (state.Findings.Length == 0)
		{
			_context.Output.WriteLine(NO_FINDINGS_MESSAGE);
		}
		else
		{
			_context.Output.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				ROUTE_COUNTS_MESSAGE_FORMAT,
				tally.FromTemplate,
				tally.Generic,
				tally.ManualReview));

			if (tally.Generic > 0)
			{
				_context.Output.WriteLine(GENERIC_COVERAGE_NOTICE);
			}

			_context.Output.WriteLine(tally.FlowCounts.Describe());
			WriteLegend(tally);
			WriteRows(tally);
		}
	}

	/// <summary>
	/// The legend: each flow note that occurs among the reported findings, once, under its key, in key
	/// order. A note no reported finding carries is not printed - the legend explains the rows and the
	/// summary line, and a note nothing refers to explains nothing.
	/// </summary>
	private void WriteLegend(SuggestTally tally)
	{
		bool wroteHeading = false;

		foreach (FlowNoteLegendEntry entry in FlowNoteKeys.AllEntries)
		{
			if (tally.OccursInReportedSet(entry.Key))
			{
				if (!wroteHeading)
				{
					_context.Output.WriteLine(FLOW_NOTES_LEGEND_HEADING);
					wroteHeading = true;
				}

				_context.Output.WriteLine(string.Format(
					CultureInfo.InvariantCulture,
					FLOW_NOTE_LEGEND_LINE_FORMAT,
					entry.Key,
					entry.Note));
			}
		}
	}

	private void WriteRows(SuggestTally tally)
	{
		if (tally.Rows.Count > 0)
		{
			_context.Output.WriteLine(FINDING_ROWS_HEADING);
		}

		foreach (SuggestRow row in tally.Rows)
		{
			_context.Output.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				FINDING_ROW_FORMAT,
				row.Identifier,
				CredentialFlows.Name(row.Flow),
				CredentialFlows.Name(row.Decision),
				FlowNoteKeys.KeyFor(row.Decision, row.Flow)));
		}

		if (tally.UnrowedCount > 0)
		{
			_context.Output.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				FINDING_ROWS_SUMMARY_FORMAT,
				tally.UnrowedCount,
				DescribeUnrowed(tally)));
		}
	}

	/// <summary>
	/// The unrowed count per (decision, flow), in the fixed order decision-then-flow enumeration order
	/// gives, so the same state always prints the same line.
	/// </summary>
	private static string DescribeUnrowed(SuggestTally tally)
	{
		List<string> entries = [];

		foreach (FlowDecision decision in Enum.GetValues<FlowDecision>())
		{
			foreach (CredentialFlow flow in Enum.GetValues<CredentialFlow>())
			{
				int unrowed = tally.UnrowedCountFor(decision, flow);

				if (unrowed > 0)
				{
					entries.Add(string.Format(
						CultureInfo.InvariantCulture,
						FINDING_ROWS_SUMMARY_ENTRY_FORMAT,
						unrowed,
						CredentialFlows.Name(flow),
						CredentialFlows.Name(decision),
						FlowNoteKeys.KeyFor(decision, flow)));
				}
			}
		}

		return string.Join(_SUMMARY_ENTRY_SEPARATOR, entries);
	}

	/// <summary>One printed finding row: no path, no text from the file.</summary>
	private readonly record struct SuggestRow(string Identifier, CredentialFlow Flow, FlowDecision Decision);

	/// <summary>
	/// The whole of what this stage retains: one counter per route, the per-flow counts, a small matrix
	/// of counts per (decision, flow), and at most <see cref="SUGGEST_CONSOLE_FINDING_ROW_LIMIT"/> rows.
	/// It is a private nested type so that nothing outside can hold one, and its size is bounded no
	/// matter how large the finding set is.
	/// </summary>
	private sealed class SuggestTally
	{
		private readonly int[,] _reportedByDecisionAndFlow =
			new int[Enum.GetValues<FlowDecision>().Length, Enum.GetValues<CredentialFlow>().Length];

		private readonly int[,] _rowedByDecisionAndFlow =
			new int[Enum.GetValues<FlowDecision>().Length, Enum.GetValues<CredentialFlow>().Length];

		private FlowCounts _flowCounts;

		internal int FromTemplate { get; private set; }

		internal int Generic { get; private set; }

		internal int ManualReview { get; private set; }

		internal List<SuggestRow> Rows { get; } = [];

		internal int UnrowedCount { get; private set; }

		internal FlowCounts FlowCounts => _flowCounts;

		internal void Record(Finding finding, RecommendationKind kind)
		{
			RecordRoute(kind);

			if (finding.Flow is CredentialFlow flow && finding.FlowDecision is FlowDecision decision)
			{
				RecordFlow(finding, flow, decision);
			}
		}

		internal bool OccursInReportedSet(string noteKey)
		{
			bool occurs = false;

			foreach (FlowDecision decision in Enum.GetValues<FlowDecision>())
			{
				foreach (CredentialFlow flow in Enum.GetValues<CredentialFlow>())
				{
					occurs |= _reportedByDecisionAndFlow[(int)decision, (int)flow] > 0
						&& string.Equals(FlowNoteKeys.KeyFor(decision, flow), noteKey, StringComparison.Ordinal);
				}
			}

			return occurs;
		}

		internal int UnrowedCountFor(FlowDecision decision, CredentialFlow flow)
		{
			return _reportedByDecisionAndFlow[(int)decision, (int)flow] - _rowedByDecisionAndFlow[(int)decision, (int)flow];
		}

		private void RecordRoute(RecommendationKind kind)
		{
			switch (kind)
			{
				case RecommendationKind.FromTemplate:
					FromTemplate++;
					break;

				case RecommendationKind.Generic:
					Generic++;
					break;

				default:
					ManualReview++;
					break;
			}
		}

		private void RecordFlow(Finding finding, CredentialFlow flow, FlowDecision decision)
		{
			_flowCounts = _flowCounts.Including(finding);
			_reportedByDecisionAndFlow[(int)decision, (int)flow]++;

			if (Rows.Count < SUGGEST_CONSOLE_FINDING_ROW_LIMIT)
			{
				Rows.Add(new SuggestRow(finding.Identifier, flow, decision));
				_rowedByDecisionAndFlow[(int)decision, (int)flow]++;
			}
			else
			{
				UnrowedCount++;
			}
		}
	}
}
