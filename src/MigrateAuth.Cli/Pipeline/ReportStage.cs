using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Session;
using MigrateAuth.Cli.Writing;

namespace MigrateAuth.Cli.Pipeline;

/// <summary>
/// What the Report stage is built from: only what belongs to the WRITE - the catalog, the
/// configuration, the label, the artifact boundary, the fallback provider, the console and the
/// clock. The report's five sections draw on every earlier stage at once; they do it by way of
/// <see cref="SessionState"/>, which <see cref="ReportStage.Run"/> receives directly, rather than by
/// way of a session-scoped object threaded through this record.
/// </summary>
public sealed record ReportStageContext
{
	/// <summary>The embedded catalog: its version, its analyzers, and its templates.</summary>
	public required LanguageCatalog Catalog { get; init; }

	/// <summary>The session's effective configuration, which supplies the threshold and Section 5.</summary>
	public required EffectiveConfiguration Configuration { get; init; }

	/// <summary>
	/// The audited repository AS A LABEL. It is a string rather than a
	/// <see cref="RepositoryFolderPath"/> because that type also carries the canonical path, and
	/// no absolute path is permitted in anything the product emits - the root being the one path
	/// with no relative form. A stage that held the folder could print it.
	/// </summary>
	public required string RepositoryLabel { get; init; }

	/// <summary>Where both artifacts are written, within the boundary it already enforces.</summary>
	public required AtomicArtifactWriter ArtifactWriter { get; init; }

	/// <summary>The fallback text, for the findings whose class the catalog does not cover.</summary>
	public required IGenericRecommendationProvider GenericRecommendations { get; init; }

	/// <summary>Where the stage writes its console surface.</summary>
	public required TextWriter Output { get; init; }

	/// <summary>
	/// The clock the artifact's dated file name is taken from. A clock-derived value is kept out of
	/// the CONTENT of the report; only the file name carries one.
	/// </summary>
	public required TimeProvider TimeProvider { get; init; }
}

/// <summary>
/// THE LAST WIRED STAGE, AND IT DECIDES NOTHING ABOUT WHAT A REPORT SAYS.
///
/// The report's five sections, the parallel data artifact, the confidence threshold, the
/// recommendation routing and the streaming write live elsewhere. This stage assembles one
/// <see cref="ReportContent"/> from the session and hands it to both renderers: it reads, it
/// composes one value, it calls two renderers through one writer, and it prints what it wrote.
///
/// THE FINDING SET IT PASSES ON IS THE COMPLETE ONE. <see cref="ReportContent"/> requires it and
/// says why: the filter runs at RENDER time over the full set, so the identifiers in the report
/// are the ones assigned over the whole. Filtering here would make a finding's identifier a
/// function of a configuration file. The threshold reaches the renderers through the configuration
/// on the content, which is the only route it has.
///
/// BOTH ARTIFACTS ARE WRITTEN FROM ONE CONTENT VALUE, which is what makes "the same five sections"
/// a property of the code rather than a thing a maintainer keeps true.
///
/// AND A REFUSED WRITE IS A FAILED STAGE, NEVER A SILENT ONE. The write boundary can refuse - a
/// destination that stopped being writable mid-session, or a thousand same-named files - and the session
/// sends that to the prompt with the session intact rather than terminating: the completed stages
/// are still valid and the user can fix a permission in seconds. What it must not do is report
/// completion, because <c>report</c> completing means a report exists.
/// </summary>
public sealed class ReportStage
{
	/// <summary>
	/// Said before the write, naming how many of the full set the threshold admits. It is the
	/// stage's first line, so it opens with the stage name as the first line of every other stage
	/// does.
	/// </summary>
	public const string REPORTING_MESSAGE_FORMAT =
		_STAGE_NAME + ": reporting {0:N0} of {1:N0} findings at minimum confidence level {2}.";

	/// <summary>
	/// Said for each file written, naming it and the output folder it is in so the user can find it.
	/// The folder is named as the user supplied it: relative to the working folder when it
	/// is the default or relative, for example <c>./Outputs</c>, and never by its canonical path.
	/// </summary>
	public const string ARTIFACT_WRITTEN_MESSAGE_FORMAT = "Written: {0} (in the output folder {1}).";

	/// <summary>Said when the boundary refused a write. The refusal's own message follows it.</summary>
	public const string ARTIFACT_NOT_WRITTEN_MESSAGE = $"{_STAGE_NAME}: the report could not be written.";

	/// <summary>
	/// Said once, before the write, and only when neither resume nor <c>analyze</c> said it already, when the enabled-language set Scan's own last completed run
	/// used differs from this session's current configuration - the console twin of
	/// <see cref="Reporting.MarkdownReportRenderer.SCAN_ENABLED_LANGUAGES_STALE_MESSAGE"/>, kept apart
	/// from it on the same terms every other duplicated-by-design console/report pair in this product
	/// is: each surface owns its own vocabulary, and the text is kept identical rather than shared.
	/// </summary>
	public const string SCAN_ENABLED_LANGUAGES_STALE_MESSAGE =
		"The scan used a different set of enabled languages than this session; re-run 'scan' to apply the current configuration.";

	private const string _STAGE_NAME = "report";

	private readonly ReportStageContext _context;

	public ReportStage(ReportStageContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(context.Catalog);
		ArgumentNullException.ThrowIfNull(context.Configuration);
		ArgumentNullException.ThrowIfNull(context.RepositoryLabel);
		ArgumentNullException.ThrowIfNull(context.ArtifactWriter);
		ArgumentNullException.ThrowIfNull(context.GenericRecommendations);
		ArgumentNullException.ThrowIfNull(context.Output);
		ArgumentNullException.ThrowIfNull(context.TimeProvider);

		_context = context;
	}

	/// <summary>
	/// The stage body. Cancellation is honoured BEFORE the first write and not between the two:
	/// a session that produced a Markdown report and no data artifact is a worse outcome than one
	/// that produced both after the user asked it to stop, and each write is individually atomic
	/// anyway - the temporary is renamed into place or it is not.
	/// </summary>
	public StageRunOutcome Run(SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		StageRunOutcome outcome;

		if (cancellationToken.IsCancellationRequested)
		{
			outcome = StageRunOutcome.Cancelled;
		}
		else
		{
			DateOnly runDate = DateOnly.FromDateTime(_context.TimeProvider.GetLocalNow().DateTime);

			outcome = WriteBothArtifacts(state, BuildContent(state, runDate), runDate);
		}

		return outcome;
	}

	/// <summary>
	/// Everything the two renderers read, assembled in one place. Every section but coverage and
	/// provenance comes from <paramref name="state"/>: Section 2 from
	/// <see cref="SessionState.ScanOutcome"/>, Section 3 from
	/// <see cref="SessionState.MergedSkippedItems"/> and Section 4's truncation counter from
	/// <see cref="SessionState.FindingsCapOutcome"/> - which is what makes all three, like the
	/// findings themselves, survive a session resumed in a process that never scanned or analyzed
	/// anything.
	///
	/// The findings carry the flows Analysis decided, persisted with them, and the default flow
	/// and origin the provenance names are the ones Analysis decided under - <see cref="SessionState.DefaultFlow"/> -
	/// and not the current configuration's, so a report from a resumed session describes the decisions
	/// it prints.
	///
	/// <paramref name="runDate"/> is the one clock read <see cref="Run"/> takes for the whole
	/// stage, so the date named in the content and the date named in the artifacts' file names below
	/// are the same value rather than two reads that could straddle midnight differently.
	/// </summary>
	private ReportContent BuildContent(SessionState state, DateOnly runDate)
	{
		return new ReportContent
		{
			Findings = state.Findings,
			Configuration = WithTheDefaultFlowAnalysisDecidedUnder(_context.Configuration, state.DefaultFlow),
			Catalog = _context.Catalog,
			ScanSummary = state.ScanOutcome,
			SkippedItems = state.MergedSkippedItems(),
			FindingsCapOutcome = state.FindingsCapOutcome,
			RepositoryLabel = _context.RepositoryLabel,
			RunDate = runDate,
			ScanEnabledLanguages = state.ScanEnabledLanguages
		};
	}

	private static EffectiveConfiguration WithTheDefaultFlowAnalysisDecidedUnder(
		EffectiveConfiguration configuration,
		EffectiveDefaultFlow decidedUnder)
	{
		return configuration with
		{
			DefaultFlow = decidedUnder.Flow,
			Provenance = configuration.Provenance with { DefaultFlowOrigin = decidedUnder.Origin }
		};
	}

	private StageRunOutcome WriteBothArtifacts(SessionState state, ReportContent content, DateOnly artifactDate)
	{
		AnnounceTheReportedSet(content);
		AnnounceScanEnabledLanguagesStalenessIfAny(state, content);

		ArtifactWriteResult markdown = _context.ArtifactWriter.WriteShareableArtifactStreaming(
			OutputArtifactKind.MarkdownReport,
			artifactDate,
			writer => MarkdownReportRenderer.Render(writer, content, _context.GenericRecommendations));

		StageRunOutcome outcome;

		if (!ReportTheWrite(markdown))
		{
			outcome = StageRunOutcome.Failed;
		}
		else
		{
			ArtifactWriteResult structuredData = _context.ArtifactWriter.WriteStructuredDataArtifactStreaming(
				artifactDate,
				stream => StructuredDataArtifactRenderer.Render(stream, content, _context.GenericRecommendations));

			outcome = ReportTheWrite(structuredData) ? StageRunOutcome.Completed : StageRunOutcome.Failed;
		}

		return outcome;
	}

	/// <summary>
	/// Compares the persisted, scan-time enabled set against this session's current
	/// configuration and says so when they differ - both derived through
	/// <see cref="EffectiveConfiguration.EnabledLanguagesInDeclaredOrder"/>, which is what makes a
	/// sequence comparison sufficient rather than a set comparison: both sides are already built by
	/// filtering the same declared order.
	/// </summary>
	private void AnnounceScanEnabledLanguagesStalenessIfAny(SessionState state, ReportContent content)
	{
		if (content.ScanEnabledLanguagesDifferFromSession && state.TryMarkScanConfigurationChangeAnnounced())
		{
			_context.Output.WriteLine(SCAN_ENABLED_LANGUAGES_STALE_MESSAGE);
		}
	}

	/// <summary>
	/// The counts a reader needs: how many of the full set are reported, and at what
	/// threshold. It is said before the write rather than after, so that a refused write leaves the
	/// user knowing what the report WOULD have contained.
	/// </summary>
	private void AnnounceTheReportedSet(ReportContent content)
	{
		int reportedCount = ThresholdFilter.CountReported(content.Findings, content.Configuration.MinimumConfidenceLevel);
		string announcement = string.Format(
			CultureInfo.InvariantCulture,
			REPORTING_MESSAGE_FORMAT,
			reportedCount,
			content.Findings.Length,
			ConfidenceLevels.Name(content.Configuration.MinimumConfidenceLevel));
		bool hiddenByTheRepository =
			reportedCount < content.Findings.Length
			&& content.Configuration.Provenance.MinimumConfidenceLevelOrigin == ConfigurationSource.RepositoryFile;

		_context.Output.WriteLine(hiddenByTheRepository ? $"{announcement}{AnalyzeConsoleReport.THRESHOLD_SET_BY_REPOSITORY_SUFFIX}" : announcement);
	}

	private bool ReportTheWrite(ArtifactWriteResult result)
	{
		if (result.Wrote)
		{
			_context.Output.WriteLine(string.Format(
				CultureInfo.InvariantCulture,
				ARTIFACT_WRITTEN_MESSAGE_FORMAT,
				result.FileNameWritten,
				result.DestinationConsoleText));
		}
		else
		{
			_context.Output.WriteLine(ARTIFACT_NOT_WRITTEN_MESSAGE);
			_context.Output.WriteLine(result.RefusalMessage);
		}

		return result.Wrote;
	}
}
