using System.Collections.Immutable;
using System.Globalization;

using MigrateAuth.Cli.Catalog;
using MigrateAuth.Cli.Configuration;
using MigrateAuth.Cli.Emission;
using MigrateAuth.Cli.Findings;
using MigrateAuth.Cli.Lexical;
using MigrateAuth.Cli.Progress;
using MigrateAuth.Cli.Reading;
using MigrateAuth.Cli.Recognition;
using MigrateAuth.Cli.Reporting;
using MigrateAuth.Cli.Scanning;
using MigrateAuth.Cli.Session;

namespace MigrateAuth.Cli.Pipeline;

/// <summary>
/// What the Analysis stage is built from. It is a context for the reason
/// <see cref="ScanStageContext"/> is one: past about four dependencies a call site stops reading as
/// an ordered list of values, and the last two - where output goes and which clock the match bound
/// and the progress cadence are measured against - exist to be substituted in a test.
/// </summary>
public sealed record AnalyzeStageContext
{
	/// <summary>The repository under assessment, which candidate paths are rebuilt against.</summary>
	public required RepositoryFolderPath Repository { get; init; }

	/// <summary>The embedded catalog, whose analyzers supply identification, profiles and rules.</summary>
	public required LanguageCatalog Catalog { get; init; }

	/// <summary>
	/// The session's effective configuration, read here for one value only:
	/// <see cref="EffectiveConfiguration.MinimumConfidenceLevel"/>, the confidence threshold, which
	/// <see cref="AnalyzeConsoleReport"/> applies to the console table so the table `analyze`
	/// prints and the report's own filtered set cannot disagree.
	/// </summary>
	public required EffectiveConfiguration Configuration { get; init; }

	/// <summary>The one door everything leaving the process passes through.</summary>
	public required EmissionPoint EmissionPoint { get; init; }

	/// <summary>
	/// The sink recognition's own skipped items accumulate in - the same instance Scan's
	/// context holds, shared across the session so both stages route through one emission point. The
	/// stage CLEARS it at the start of its own run and snapshots it onto the session state at
	/// completion, on the same terms <see cref="Scanning.ScanStageContext.SkippedItems"/> documents
	/// for Scan.
	/// </summary>
	public required SkippedItemLedger SkippedItems { get; init; }

	/// <summary>Where the stage writes.</summary>
	public required TextWriter Output { get; init; }

	/// <summary>The clock the match bound and the progress cadence are measured against.</summary>
	public required TimeProvider TimeProvider { get; init; }
}

/// <summary>
/// THE SECOND STAGE THAT DOES REAL WORK, AND IT COMPOSES RATHER THAN COMPUTES.
///
/// It runs, in order, the read, the attribution (made by Scan and reused here), the lexical
/// scan, recognition and classification, and the composition that orders and numbers, over the
/// candidate set the Scan stage persisted, and puts the result where findings live in the
/// session state. This stage is that loop and nothing else - there is no analysis decision taken
/// here, which is why there is no rule, no level and no ordering in this file.
///
/// IT ANALYZES THE CANDIDATE SET AND NEVER THE REPOSITORY. The candidate set is an output
/// of Scan that is persisted, precisely so that Analysis consumes a recorded artifact rather than
/// walking the tree a second time - which is also what makes the read-only guarantee verifiable and lets the
/// stage cascade compare concrete artifacts.
///
/// THE RECOGNIZED FILES ARE STREAMED INTO COMPOSITION, NOT COLLECTED BEFORE IT. A
/// <see cref="RecognizedFile"/> carries the file's whole normalized text, because every anchor
/// offset indexes into it; a list of them built before composing would hold every candidate file's
/// text at once, which for the largest supported repositories is the largest thing this product
/// could allocate. Streaming means a file that
/// yields no construct is released as soon as the next one is read, and only files that actually
/// contributed a construct are still reachable while the sort runs.
///
/// THE ANALYSIS STAGE, NOT REPORT, DISPLAYS THE FINDINGS TABLE, so a path and a line of an
/// analyzed file reach this console surface by way of <see cref="AnalyzeConsoleReport"/>. The
/// data invariant holds at this sink: the relative path is its one stated exemption, never the
/// matched text or the secret a warning flags beside it.
/// </summary>
public sealed class AnalyzeStage
{
	/// <summary>Said before the pass, so that a long analysis is not a silent one.</summary>
	public const string ANALYSIS_STARTED_MESSAGE_FORMAT = $"{_STAGE_NAME}: analyzing {{0:N0}} candidate files.";

	/// <summary>The same line when exactly one candidate file is analyzed.</summary>
	public const string SINGLE_CANDIDATE_ANALYSIS_STARTED_MESSAGE = $"{_STAGE_NAME}: analyzing 1 candidate file.";

	/// <summary>
	/// The empty candidate set, which is not an error. A repository with no candidate files
	/// completes every stage and produces a valid report, and this is the line that case prints
	/// here.
	/// </summary>
	public const string NO_CANDIDATES_MESSAGE =
		$"{_STAGE_NAME}: the scan recorded no candidate files, so there is nothing to analyze.";

	/// <summary>The count the stage exists to produce, said distinctly from the file counts.</summary>
	public const string FINDINGS_COUNT_MESSAGE_FORMAT = "Findings: {0:N0}.";

	/// <summary>
	/// Said when a path the scan recorded is no longer there. It is a count rather than a list
	/// because the paths themselves are already carried by the skipped-item ledger, which bounds
	/// its enumeration at a hundred per category and this line does not.
	/// </summary>
	public const string MISSING_CANDIDATES_MESSAGE_FORMAT =
		"Candidate files recorded by the scan and no longer present: {0:N0}.";

	/// <summary>
	/// The truncation counter, said whenever either cap removed anything. It is the console
	/// half of the same declaration both shareable artifacts carry - never presented as complete.
	/// </summary>
	public const string FINDINGS_CAPPED_MESSAGE_FORMAT =
		"Findings capped: {0:N0} recognized construct(s) omitted ({1:N0} by the per-file cap across {2:N0} file(s), {3:N0} by the global cap).";

	/// <summary>
	/// THE RECONCILIATION LINE. Said once, at the end of the stage, only when analysis
	/// itself skipped at least one file - Scan already prints its own skip line at its own end, and
	/// this is Analysis's counterpart rather than a repeat of it. Files only: the one skipped-item
	/// category counted in directories (<see cref="Reading.SkippedItemCategory.UnlistableDirectory"/>)
	/// belongs to the walk, and the walk never runs again during <c>analyze</c>.
	/// </summary>
	public const string ANALYSIS_SKIPPED_MESSAGE_FORMAT =
		"Skipped during analysis: {0:N0} files. The report lists skipped items from both stages.";

	private const string _STAGE_NAME = "analyze";
	private const int _SINGULAR_COUNT = 1;
	private const string _ITEM_LABEL = "candidate files";
	private const char _PORTABLE_DIRECTORY_SEPARATOR = '/';

	private readonly AnalyzeStageContext _context;

	public AnalyzeStage(AnalyzeStageContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(context.Catalog);
		ArgumentNullException.ThrowIfNull(context.Configuration);
		ArgumentNullException.ThrowIfNull(context.EmissionPoint);
		ArgumentNullException.ThrowIfNull(context.SkippedItems);
		ArgumentNullException.ThrowIfNull(context.Output);
		ArgumentNullException.ThrowIfNull(context.TimeProvider);

		_context = context;
	}

	/// <summary>
	/// The stage body. The pass is built fresh on every run - the lexical scanner caches one
	/// scanner per profile and the recognition engine holds the match bound's stopwatch, and
	/// neither may carry state from a previous run into this one.
	///
	/// The ledger is cleared before the pass starts, on the same terms
	/// <see cref="Scanning.ScanStage.Run"/> already documents for Scan - a second Analyze run in the
	/// same process must not add this run's skips onto an earlier run's.
	/// </summary>
	public StageRunOutcome Run(SessionState state, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(state);

		_context.SkippedItems.Clear();

		AnalysisPass pass = new(_context);
		StageProgressReporter progress = new(
			_STAGE_NAME,
			_ITEM_LABEL,
			state.CandidateRelativePaths.Count,
			_context.Output,
			_context.TimeProvider);

		AnnounceTheWorkAhead(state, progress);

		ComposedFindingSet recognized = FindingComposition.ComposeWithCapOutcome(
			pass.RecognizeEveryCandidate(
				state.CandidateRelativePaths,
				state.CandidateLanguageNames,
				progress,
				cancellationToken));

		// Composition reads no configuration, so the flow is assigned here, once the set is
		// complete and before anything records or displays it. The default it is assigned under
		// is recorded with the findings, and is what a resumed session reports.
		EffectiveDefaultFlow defaultFlow = new(
			_context.Configuration.DefaultFlow,
			_context.Configuration.Provenance.DefaultFlowOrigin);
		ComposedFindingSet composed = recognized with
		{
			Findings = FlowAssignment.AssignAll(recognized.Findings, defaultFlow.Flow)
		};

		StageRunOutcome outcome;

		if (pass.WasCancelled)
		{
			// An interrupted stage leaves nothing partial behind. The composed set is
			// discarded rather than recorded, so the state still holds whatever the last COMPLETED
			// analysis produced - and the identifiers in it were assigned over that run's full set
			// rather than over however far this one happened to get.
			outcome = StageRunOutcome.Cancelled;
		}
		else
		{
			state.RecordAnalysisOutcome(
				composed.Findings,
				composed.CapOutcome,
				_context.SkippedItems.Tallies(),
				pass.FlowEvaluationDiagnostics,
				defaultFlow);
			ReportOutcome(composed, pass);
			outcome = StageRunOutcome.Completed;
		}

		return outcome;
	}

	private void AnnounceTheWorkAhead(SessionState state, StageProgressReporter progress)
	{
		SessionStartup.ReportScanConfigurationChange(state, _context.Configuration, _context.Output);

		if (state.CandidateRelativePaths.Count == 0)
		{
			_context.Output.WriteLine(NO_CANDIDATES_MESSAGE);
		}
		else
		{
			WriteFormatted(
				state.CandidateRelativePaths.Count == _SINGULAR_COUNT
					? SINGLE_CANDIDATE_ANALYSIS_STARTED_MESSAGE
					: ANALYSIS_STARTED_MESSAGE_FORMAT,
				state.CandidateRelativePaths.Count);
		}

		progress.WarnWhenTheWorkIsLarge();
	}

	private void ReportOutcome(ComposedFindingSet composed, AnalysisPass pass)
	{
		WriteFormatted(FINDINGS_COUNT_MESSAGE_FORMAT, composed.Findings.Length);

		if (pass.MissingCandidateCount > 0)
		{
			WriteFormatted(MISSING_CANDIDATES_MESSAGE_FORMAT, pass.MissingCandidateCount);
		}

		if (composed.CapOutcome.IsTruncated)
		{
			WriteFormattedCapOutcome(composed.CapOutcome);
		}

		if (_context.SkippedItems.SkippedFiles > 0)
		{
			WriteFormatted(ANALYSIS_SKIPPED_MESSAGE_FORMAT, _context.SkippedItems.SkippedFiles);
		}

		AnalyzeConsoleReport.Render(
			_context.Output,
			composed.Findings,
			_context.Catalog,
			_context.Configuration.MinimumConfidenceLevel,
			_context.Configuration.Provenance.MinimumConfidenceLevelOrigin == ConfigurationSource.RepositoryFile);
	}

	private void WriteFormattedCapOutcome(FindingsCapOutcome capOutcome)
	{
		_context.Output.WriteLine(string.Format(
			CultureInfo.InvariantCulture,
			FINDINGS_CAPPED_MESSAGE_FORMAT,
			capOutcome.TotalOmittedCount,
			capOutcome.PerFileOmittedCount,
			capOutcome.PerFileCapTrippedFileCount,
			capOutcome.GlobalOmittedCount));
	}

	private void WriteFormatted(string messageFormat, int count)
	{
		_context.Output.WriteLine(string.Format(CultureInfo.InvariantCulture, messageFormat, count));
	}

	/// <summary>
	/// ONE RUN'S PASS OVER THE CANDIDATE SET. It is a class rather than a set of methods on the
	/// stage because it owns per-run mutable state - the scanner cache, the match bound's stopwatch
	/// and the two counts - and the stage itself is built once by the session and run many times.
	/// </summary>
	private sealed class AnalysisPass
	{
		private readonly AnalyzeStageContext _context;
		private readonly RepositoryFileReader _reader;
		private readonly FileLexicalScanner _scanner;
		private readonly RecognitionEngine _engine;

		internal AnalysisPass(AnalyzeStageContext context)
		{
			_context = context;
			_reader = new RepositoryFileReader(context.EmissionPoint);
			_scanner = new FileLexicalScanner(context.EmissionPoint, context.TimeProvider);
			_engine = new RecognitionEngine(context.EmissionPoint, context.TimeProvider);
		}

		/// <summary>The engine's flow-signal counters for this pass, read once the pass is complete.</summary>
		internal FlowEvaluationDiagnostics FlowEvaluationDiagnostics => new()
		{
			FilesEvaluated = _engine.FilesEvaluatedForFlowSignals,
			SignalTripCount = _engine.FlowSignalTripCount
		};

		/// <summary>Whether the user interrupted the pass. A cancelled analysis is not an analysis.</summary>
		internal bool WasCancelled { get; private set; }

		/// <summary>How many recorded candidates were no longer there when this pass opened them.</summary>
		internal int MissingCandidateCount { get; private set; }

		/// <summary>
		/// Every candidate that yielded at least one construct, one at a time.
		///
		/// A FILE THAT YIELDED NOTHING IS NOT RETURNED AT ALL, and that is not a filter on the
		/// findings - a file with no recognized construct contributes nothing to composition by
		/// definition. What it buys is that the file's text becomes collectable the moment the loop
		/// moves on, which for the large majority of candidates is immediately.
		/// </summary>
		internal IEnumerable<RecognizedFile> RecognizeEveryCandidate(
			IReadOnlyList<string> candidateRelativePaths,
			IReadOnlyList<string> candidateLanguageNames,
			StageProgressReporter progress,
			CancellationToken cancellationToken)
		{
			for (int candidateIndex = 0; candidateIndex < candidateRelativePaths.Count; candidateIndex++)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					WasCancelled = true;
					break;
				}

				if (TryRecognizeOneCandidate(
					candidateRelativePaths[candidateIndex],
					candidateLanguageNames[candidateIndex],
					out RecognizedFile? recognizedFile))
				{
					yield return recognizedFile!;
				}

				progress.ItemCompleted();
			}
		}

		/// <summary>
		/// One candidate, from a recorded path to its recognized constructs. False means this
		/// candidate contributes nothing, for one of five reasons - four of which have already been
		/// reported through the emission point by the component that established them, and the
		/// fifth of which is reported here because this stage is the only one that can see it.
		/// </summary>
		private bool TryRecognizeOneCandidate(
			string candidateRelativePath,
			string attributedLanguageName,
			out RecognizedFile? recognizedFile)
		{
			recognizedFile = null;

			string absolutePath = RebuildAbsolutePath(candidateRelativePath);
			bool recognized = false;

			if (!RepositoryRelativePath.TryFromScannedPath(
				_context.Repository,
				absolutePath,
				out RepositoryRelativePath path)
				|| !File.Exists(absolutePath))
			{
				ReportTheCandidateAsMissing(path);
			}
			else
			{
				recognized = TryRecognizeOnePresentCandidate(path, absolutePath, attributedLanguageName, out recognizedFile);
			}

			return recognized;
		}

		/// <summary>
		/// The read, the resolution of Scan's attribution, the scan and the recognition, in the one
		/// order that works. Each of the four can decline the file and each declines it for a
		/// different reason, so they are tested in sequence rather than combined.
		/// </summary>
		private bool TryRecognizeOnePresentCandidate(
			RepositoryRelativePath path,
			string absolutePath,
			string attributedLanguageName,
			out RecognizedFile? recognizedFile)
		{
			recognizedFile = null;

			FileReadResult read = _reader.Read(new WalkedFile(path, absolutePath));
			bool recognized = false;

			if (read.WasRead && TryResolveAttribution(attributedLanguageName, out AttributedAnalyzer attributed))
			{
				ZoneMask mask = _scanner.Scan(attributed.Analyzer.LexicalProfile, read.NormalizedText, path);

				RecognitionResult result = _engine.Recognize(new FileRecognitionContext
				{
					Analyzer = attributed.Analyzer,
					NormalizedText = read.NormalizedText,
					Mask = mask,
					Path = path
				});

				recognized = result.Constructs.Length > 0;

				if (recognized)
				{
					recognizedFile = new RecognizedFile
					{
						Path = path,
						Language = attributed.Language,
						NormalizedText = read.NormalizedText,
						Analyzer = attributed.Analyzer,
						Constructs = result.Constructs,
						FlowEvidence = result.FlowEvidence,
						FlowEvaluationTripped = result.FlowEvaluationTripped
					};
				}
			}

			return recognized;
		}

		/// <summary>
		/// The file's analyzer and the enumeration value a finding must carry, both resolved from the
		/// language name Scan's first-line attribution persisted for the candidate. Nothing
		/// here reads the file's first line or attributes it: Analysis charges the lexical scan only
		/// (never the classification), and a file cannot be attributed to one language in Scan and another here.
		///
		/// BOTH ARE REQUIRED AND THE SECOND IS THE ONE THAT CAN FAIL ON A CORRECT CATALOG. The analyzer
		/// contract lets an analyzer derive any name it likes, while
		/// <see cref="Finding.Language"/> is one of the eight languages - so a catalog holding
		/// an analyzer for a language outside that enumeration attributes the file correctly and
		/// still has nowhere to put it in a finding. That is declared unsupported coverage rather
		/// than a hole in the walk: the coverage statement already names every one of the eight and
		/// its status, and no skipped item is raised here because no file was skipped - it was read,
		/// counted in the inventory, and belongs to a language this product does not report on.
		/// </summary>
		private bool TryResolveAttribution(string attributedLanguageName, out AttributedAnalyzer attributed)
		{
			attributed = default;

			bool attributedToAnAssessedLanguage = false;

			if (_context.Catalog.AnalyzersByLanguageName.TryGetValue(attributedLanguageName, out ILanguageAnalyzer? analyzer)
				&& AssessedLanguages.TryParse(attributedLanguageName, out AssessedLanguage language))
			{
				attributed = new AttributedAnalyzer(analyzer, language);
				attributedToAnAssessedLanguage = true;
			}

			return attributedToAnAssessedLanguage;
		}

		/// <summary>
		/// THE SKIPPED-ITEM CATEGORY FOR A RECORDED CANDIDATE THAT IS NO LONGER THERE, RAISED HERE
		/// BECAUSE THIS IS THE ONLY STAGE THAT CAN RAISE IT.
		///
		/// <see cref="RepositoryFileReader"/> declines to raise it in its own words: the category
		/// "exists for a path recorded in Scan that Analysis later opens, a situation the persisted
		/// candidate set creates and the inventory owns", and a reader raising it would be inventing an
		/// occasion for a category it does not own. This is that occasion. Without it a candidate
		/// deleted between <c>scan</c> and <c>analyze</c> would vanish from the assessment with
		/// nothing counting it, which is precisely the silent hole in coverage the skipped-item
		/// enumeration exists to prevent.
		///
		/// The path may be undeclared, when the recorded text could not be expressed relative to
		/// the root at all. The ledger handles that by construction - it counts every item and
		/// enumerates only the ones that can be named - so an unnameable hole is still a counted
		/// hole rather than a dropped one.
		/// </summary>
		private void ReportTheCandidateAsMissing(RepositoryRelativePath path)
		{
			MissingCandidateCount++;

			_context.EmissionPoint.Emit(new EmittedFact
			{
				Category = EmissionCategory.SkippedItem,
				SkippedItem = SkippedItemCategory.MissingCandidate,
				Path = path
			});
		}

		/// <summary>
		/// The recorded path made openable. The state file and the candidate list both carry the
		/// portable separator, so it is put back to the platform's before the combine - on a
		/// platform where the two are the same character this changes nothing, and on the one where
		/// they differ it is the difference between a path that opens and one that does not. It is
		/// the same derivation <see cref="RepositoryInventoryScan"/> uses, for the same reason.
		/// </summary>
		private string RebuildAbsolutePath(string candidateRelativePath)
		{
			return Path.Combine(
				_context.Repository.CanonicalPath,
				candidateRelativePath.Replace(_PORTABLE_DIRECTORY_SEPARATOR, Path.DirectorySeparatorChar));
		}

		/// <summary>
		/// An analyzer paired with the enumeration value its files are reported under. The two
		/// travel together for the reason <see cref="AttributedLanguage"/> gives: deriving them
		/// separately is how a file gets analyzed with one language's rules and reported under
		/// another's name.
		/// </summary>
		private readonly record struct AttributedAnalyzer(ILanguageAnalyzer Analyzer, AssessedLanguage Language);
	}
}
