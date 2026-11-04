using System.Collections.Immutable;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;

namespace MigrateAuth.Cli.Session;

/// <summary>
/// The bounded session state: the minimum the gating needs, and nothing else - which stages
/// completed, the candidate set, and findings without rendered recommendations.
///
/// <see cref="Finding"/> carries no rendered recommendation by construction - it holds a pattern
/// class, which the template catalog is indexed by, and the rendered text is produced at write time
/// and held by nobody. So this state stores the type as it is rather than a filtered projection of it.
///
/// The scan summary, each stage's own skipped-item tallies and the findings-cap outcome are recorded
/// here too, as supporting detail of the same stage outputs, so a report produced in a resumed process
/// reads them as the scan and the analysis left them.
///
/// Resumption granularity is the stage. There is no checkpoint inside a stage: a stage
/// commits on completion and an interrupted one leaves nothing behind.
/// </summary>
public sealed class SessionState
{
	private readonly HashSet<CommandId> _completedWorkflowStages = [];
	private readonly List<string> _candidateRelativePaths = [];
	private readonly List<string> _candidateLanguageNames = [];

	private ImmutableArray<Finding> _findings = [];

	private SessionState(CatalogVersion catalogVersion, string repositoryRootLabel)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRootLabel);

		CatalogVersion = catalogVersion;
		RepositoryRootLabel = repositoryRootLabel;
	}

	/// <summary>
	/// The catalog version that produced this state. A state produced under a different
	/// catalog version starts a fresh session, because a report declaring the new version over
	/// findings produced with the previous one would misstate its own provenance.
	/// </summary>
	public CatalogVersion CatalogVersion { get; }

	/// <summary>
	/// The audited repository as a label rather than as a traversable path, so that a human can
	/// tell which state belongs to which checkout without the file carrying a route to it.
	/// </summary>
	public string RepositoryRootLabel { get; }

	/// <summary>
	/// The candidate set Scan produces and Analysis consumes. Declared here and
	/// filled by the repository walk; every entry is a path relative to the
	/// repository root.
	/// </summary>
	public IReadOnlyList<string> CandidateRelativePaths => _candidateRelativePaths;

	/// <summary>
	/// The complete, ordered, numbered finding set Analysis produces and Report consumes.
	///
	/// It is the full set and never the filtered one. The confidence threshold runs at render time over
	/// this array, so what is persisted is what the rubric assigned rather than what the reader's
	/// configuration currently admits - otherwise a session resumed under a different threshold
	/// would lose findings that were filtered out of the state file.
	/// </summary>
	public ImmutableArray<Finding> Findings => _findings;

	/// <summary>
	/// Section 2's numbers, as Scan last left them: <see cref="ScanSummary.Empty"/> until
	/// Scan has completed at least once in this session or an earlier one.
	/// </summary>
	public ScanSummary ScanOutcome { get; private set; } = ScanSummary.Empty;

	/// <summary>
	/// The Scan stage's OWN skipped-item tallies from its last completed run - never
	/// Analysis's. All eleven categories are always present; a category nothing tripped carries a
	/// zero count rather than being absent, on the same terms <see cref="SkippedItemLedger.Tallies"/>
	/// already holds within one run.
	/// </summary>
	public ImmutableArray<SkippedItemTally> ScanSkippedItems { get; private set; } = SkippedItemTallies.Empty;

	/// <summary>
	/// The Analysis stage's OWN skipped-item tallies from its last completed run - never
	/// Scan's. Kept separate from <see cref="ScanSkippedItems"/> rather than merged at the point of
	/// recording, because each stage snapshots only what it itself accumulated and Report is the one
	/// reader that needs the two combined - see <see cref="MergedSkippedItems"/>.
	/// </summary>
	public ImmutableArray<SkippedItemTally> AnalysisSkippedItems { get; private set; } = SkippedItemTallies.Empty;

	/// <summary>
	/// The enabled-language set Scan's own last completed run actually used to narrow
	/// candidate selection - empty until Scan has completed at least once in this session or an
	/// earlier one. Persisted (schema version 4) so that Section 2's assessed/disabled state, when
	/// rendered from a resumed session, still reflects what the scan itself saw rather than being
	/// reinterpreted under a configuration that may have changed since.
	/// </summary>
	public ImmutableArray<AssessedLanguage> ScanEnabledLanguages { get; private set; } = [];

	/// <summary>
	/// The truncation counter, as Analysis last left it: <see cref="FindingsCapOutcome.Empty"/>
	/// until Analysis has completed at least once in this session or an earlier one.
	/// </summary>
	public FindingsCapOutcome FindingsCapOutcome { get; private set; } = FindingsCapOutcome.Empty;

	/// <summary>
	/// The flow-signal evaluation counters as Analysis last left them:
	/// <see cref="FlowEvaluationDiagnostics.Empty"/> until an analysis has completed in this session
	/// or an earlier one. Persisted, so that the figures a report carries survive a resume.
	/// </summary>
	public FlowEvaluationDiagnostics FlowEvaluationDiagnostics { get; private set; } = FlowEvaluationDiagnostics.Empty;

	/// <summary>
	/// The default flow Analysis applied when it decided the flows, and where it came from:
	/// the built-in default until an analysis has completed. Persisted, so that what a resumed session
	/// reports as its default is the one the persisted decisions were made under.
	/// </summary>
	public EffectiveDefaultFlow DefaultFlow { get; private set; } = EffectiveDefaultFlow.BuiltIn;

	/// <summary>
	/// The language name Scan's first-line attribution gave each candidate, parallel to
	/// <see cref="CandidateRelativePaths"/> and the same length. Analysis reuses it and does not
	/// attribute a file a second time, so a lexical-budget trip in one stage cannot attribute a file
	/// to one language in Scan and another in Analysis.
	/// </summary>
	public IReadOnlyList<string> CandidateLanguageNames => _candidateLanguageNames;

	/// <summary>A session with nothing completed.</summary>
	public static SessionState Fresh(CatalogVersion catalogVersion, string repositoryRootLabel) =>
		new(catalogVersion, repositoryRootLabel);

	/// <summary>
	/// A session restored from a validated state document. The caller is the state store, which
	/// has already rejected anything the schema does not admit.
	/// </summary>
	public static SessionState Resumed(
		CatalogVersion catalogVersion,
		string repositoryRootLabel,
		IEnumerable<CommandDescriptor> completedWorkflowStages,
		IEnumerable<string> candidateRelativePaths,
		IEnumerable<string> candidateLanguageNames,
		ImmutableArray<Finding> findings,
		ScanSummary scanOutcome,
		ImmutableArray<SkippedItemTally> scanSkippedItems,
		ImmutableArray<SkippedItemTally> analysisSkippedItems,
		FindingsCapOutcome findingsCapOutcome,
		ImmutableArray<AssessedLanguage> scanEnabledLanguages,
		EffectiveDefaultFlow defaultFlow,
		FlowEvaluationDiagnostics flowEvaluationDiagnostics)
	{
		ArgumentNullException.ThrowIfNull(completedWorkflowStages);
		ArgumentNullException.ThrowIfNull(candidateRelativePaths);
		ArgumentNullException.ThrowIfNull(candidateLanguageNames);
		ArgumentNullException.ThrowIfNull(scanOutcome);
		ArgumentNullException.ThrowIfNull(findingsCapOutcome);
		ArgumentNullException.ThrowIfNull(flowEvaluationDiagnostics);

		SessionState state = new(catalogVersion, repositoryRootLabel);

		foreach (CommandDescriptor completedStage in completedWorkflowStages)
		{
			state._completedWorkflowStages.Add(completedStage.Id);
		}

		state._candidateRelativePaths.AddRange(candidateRelativePaths);
		state._candidateLanguageNames.AddRange(candidateLanguageNames);
		state._findings = findings;
		state.ScanOutcome = scanOutcome;
		state.ScanSkippedItems = scanSkippedItems;
		state.AnalysisSkippedItems = analysisSkippedItems;
		state.FindingsCapOutcome = findingsCapOutcome;
		state.ScanEnabledLanguages = scanEnabledLanguages;
		state.DefaultFlow = defaultFlow;
		state.FlowEvaluationDiagnostics = flowEvaluationDiagnostics;

		return state;
	}

	/// <summary>
	/// Every output Scan produced this run: the candidate set, Section 2's numbers, Scan's own
	/// skipped-item tallies and the enabled-language set it narrowed candidate selection with, recorded
	/// together because they are one stage's one completed run.
	///
	/// It replaces rather than appends. An interrupted Scan leaves nothing partial behind and re-runs
	/// from its start, so a second run must produce the outputs of that run and not the union of two.
	/// Appending would also double the candidate set on every ordinary re-scan of an unchanged
	/// repository.
	///
	/// The caller is the Scan stage, which has already established that every candidate path is
	/// relative to the repository root - the walk produces no other kind - that
	/// <paramref name="scanSkippedItems"/> is its own ledger's snapshot, taken after clearing the
	/// ledger at the start of this same run, and that <paramref name="scanEnabledLanguages"/> is the
	/// same effective configuration's enabled set that this same run's candidate selection read.
	/// </summary>
	public void RecordScanOutcome(
		IReadOnlyList<string> candidateRelativePaths,
		IReadOnlyList<string> candidateLanguageNames,
		ScanSummary scanSummary,
		ImmutableArray<SkippedItemTally> scanSkippedItems,
		ImmutableArray<AssessedLanguage> scanEnabledLanguages)
	{
		ArgumentNullException.ThrowIfNull(candidateRelativePaths);
		ArgumentNullException.ThrowIfNull(candidateLanguageNames);
		ArgumentNullException.ThrowIfNull(scanSummary);

		if (candidateLanguageNames.Count != candidateRelativePaths.Count)
		{
			throw new ArgumentException(
				"Every candidate carries exactly one attributed language name (DR-27).",
				nameof(candidateLanguageNames));
		}

		_candidateRelativePaths.Clear();
		_candidateRelativePaths.AddRange(candidateRelativePaths);
		_candidateLanguageNames.Clear();
		_candidateLanguageNames.AddRange(candidateLanguageNames);
		ScanOutcome = scanSummary;
		ScanSkippedItems = scanSkippedItems;
		ScanEnabledLanguages = scanEnabledLanguages;
	}

	/// <summary>
	/// Every output Analysis produced this run: the finding set, the truncation counter and Analysis's
	/// own skipped-item tallies, recorded together for the reason <see cref="RecordScanOutcome"/> already
	/// gives for Scan's outputs.
	///
	/// It replaces rather than appends. An interrupted <c>analyze</c> leaves nothing partial behind and
	/// re-runs from its start, so a second run must produce the outputs of that run and not the union of
	/// two. A union would also carry two findings with the same identifier, since identifiers are
	/// assigned over the whole set.
	///
	/// The caller is the Analysis stage, which has already composed <paramref name="findings"/>
	/// through <see cref="FindingComposition"/> - so the array is ordered and numbered before it arrives,
	/// and nothing here re-orders or re-numbers it - and whose <paramref name="analysisSkippedItems"/> is
	/// its own ledger's snapshot, on the same terms <see cref="RecordScanOutcome"/> documents for Scan's.
	/// </summary>
	public void RecordAnalysisOutcome(
		ImmutableArray<Finding> findings,
		FindingsCapOutcome findingsCapOutcome,
		ImmutableArray<SkippedItemTally> analysisSkippedItems,
		FlowEvaluationDiagnostics? flowEvaluationDiagnostics = null,
		EffectiveDefaultFlow? defaultFlow = null)
	{
		ArgumentNullException.ThrowIfNull(findingsCapOutcome);

		_findings = findings;
		FindingsCapOutcome = findingsCapOutcome;
		AnalysisSkippedItems = analysisSkippedItems;
		FlowEvaluationDiagnostics = flowEvaluationDiagnostics ?? FlowEvaluationDiagnostics.Empty;
		DefaultFlow = defaultFlow ?? EffectiveDefaultFlow.BuiltIn;
	}

	/// <summary>
	/// WHAT REPORT ACTUALLY READS FOR SECTION 3: the two stages' tallies combined, per
	/// category - counts added, enumerated paths Scan's own followed by Analysis's own, truncated to
	/// the same ceiling one ledger's own accumulation already enforces within a single run. See
	/// <see cref="SkippedItemTallies.Merge"/> for why this reproduces a single, never-cleared
	/// ledger's own tallies for an ordinary scan-then-analyze run.
	/// </summary>
	public ImmutableArray<SkippedItemTally> MergedSkippedItems() =>
		SkippedItemTallies.Merge(ScanSkippedItems, AnalysisSkippedItems);

	/// <summary>Whether a workflow stage has completed in this session or an earlier one.</summary>
	public bool IsCompleted(CommandId workflowCommand) =>
		_completedWorkflowStages.Contains(workflowCommand);

	/// <summary>The completed stages, in run order, for display and for persistence.</summary>
	public ImmutableArray<CommandDescriptor> CompletedStagesInRunOrder() =>
	[
		.. CommandRegistry.WorkflowCommandsInRunOrder.Where(stage => _completedWorkflowStages.Contains(stage.Id))
	];

	/// <summary>
	/// the gate. Returns the first stage, in run order, that precedes the command and
	/// has not completed, so the caller can name the stage the user must run next - which is not
	/// always the command's immediate predecessor, since that one cannot run either while an
	/// earlier stage is missing.
	/// </summary>
	public bool TryFindUnmetPredecessor(CommandId workflowCommand, out CommandDescriptor? unmetPredecessor)
	{
		CommandDescriptor descriptor = CommandRegistry.Describe(workflowCommand);

		unmetPredecessor = CommandRegistry.WorkflowCommandsInRunOrder
			.Where(stage => stage.OrderWithinGroup < descriptor.OrderWithinGroup)
			.FirstOrDefault(stage => !_completedWorkflowStages.Contains(stage.Id));

		return unmetPredecessor is not null;
	}

	/// <summary>
	/// the cascade. Marks a stage completed and clears every downstream stage,
	/// returning the ones that had in fact completed so the caller can name them. Invalidation
	/// touches this state only: a report already written to disk is left where it is.
	/// </summary>
	public ImmutableArray<CommandDescriptor> MarkCompleted(CommandId workflowCommand)
	{
		ImmutableArray<CommandDescriptor>.Builder invalidatedStages = ImmutableArray.CreateBuilder<CommandDescriptor>();

		foreach (CommandDescriptor downstreamStage in CommandRegistry.WorkflowCommandsAfter(workflowCommand))
		{
			if (_completedWorkflowStages.Remove(downstreamStage.Id))
			{
				invalidatedStages.Add(downstreamStage);
			}
		}

		_completedWorkflowStages.Add(workflowCommand);

		return invalidatedStages.ToImmutable();
	}

	/// <summary>
	/// The path a cancelled or failed stage takes: the session stays intact, the stage
	/// is not completed, and nothing upstream is invalidated.
	/// </summary>
	public void MarkNotCompleted(CommandId workflowCommand)
	{
		CommandDescriptor descriptor = CommandRegistry.Describe(workflowCommand);

		if (descriptor.Group is not CommandGroup.Workflow)
		{
			throw new ArgumentOutOfRangeException(
				nameof(workflowCommand),
				workflowCommand,
				"Only a workflow command has a completion state.");
		}

		_completedWorkflowStages.Remove(workflowCommand);
	}
}
